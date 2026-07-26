import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:crypto/crypto.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

import 'ifood_cloud.dart';
import 'models.dart';
import 'staff_security.dart';
import 'whatsapp_cloud.dart';

const _storageKey = 'balcao_livre_flutter_state_v1';
const _paymentsApiUrl = String.fromEnvironment(
  'BALCAO_PAYMENTS_API_URL',
  defaultValue:
      'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments',
);
const _licenseApiUrl = String.fromEnvironment(
  'BALCAO_LICENSE_API_URL',
  defaultValue: 'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/license',
);
const _appVersion = String.fromEnvironment(
  'BALCAO_APP_VERSION',
  defaultValue: 'flutter-preview',
);

class BalcaoStore extends ChangeNotifier {
  BalcaoStore({
    IFoodCloudClient? ifoodClient,
    WhatsAppCloudClient? whatsappClient,
  }) : _ifoodClient = ifoodClient ?? IFoodCloudClient(),
       _whatsappClient = whatsappClient ?? WhatsAppCloudClient();

  bool hydrated = false;
  bool loggedIn = false;
  bool authBusy = false;
  bool cashOpen = true;
  bool cashReconciliationRequired = false;
  DateTime? unreconciledCashOpenedAt;
  bool onlineStoreOpen = false;
  String licenseKey = '';
  String operatorName = 'Lucas';
  String authEmail = '';
  String authError = '';
  String syncStatus = 'Offline local';
  String publicMenuId = '';
  String publicMenuSlug = '';
  String businessName = 'Hamburgueria do Val';
  String businessLegalName = 'Hamburgueria do Val';
  String businessResponsible = 'Lucas Cesar';
  String businessDocument = '50.597.666/0001-47';
  String businessPhone = '(33) 99960-9457';
  String businessCity = 'Governador Valadares';
  String businessUf = 'MG';
  String businessAddress = 'Rua G 390, Turmalina';
  String selectedOrderId = 'mesa-000001';
  String search = '';
  String lastSync = '';
  bool whatsappConnected = false;
  bool whatsappBusy = false;
  String whatsappNumber = '';
  String whatsappSessionId = '';
  String whatsappConnectionStatus = 'DISCONNECTED';
  String whatsappMessage = 'WhatsApp nao conectado';
  String whatsappOnboardingUrl = '';
  String whatsappLastSyncAt = '';
  String securityMessage = '';
  bool cloudBackupEnabled = true;
  bool centralSyncEnabled = true;
  String lastBackupAt = '';
  String backupMessage = 'Nenhum backup gerado nesta instalacao.';
  bool pointConnected = false;
  String pointConnectionStatus = 'DISCONNECTED';
  String pointDeviceName = '';
  String pointSerial = '';
  String pointStatus = 'Mercado Pago nao configurado';
  String pointChargeMethod = 'Pix Mercado Pago';
  double pointPendingAmount = 0;
  String pointLastNsu = '';
  String pointSellerUserId = '';
  String pointTerminalId = '';
  String pointTerminalLabel = '';
  String pointLastSyncAt = '';
  String pointLastError = '';
  String pointAttemptId = '';
  String pointOrderId = '';
  String pointLocalReference = '';
  String pointConnectUrl = '';
  String windowsBridgeUrl = '';
  String windowsBridgeLocalUrl = 'http://localhost:5050';
  String windowsBridgeStatus = 'Windows bridge nao conectado';
  String windowsBridgeLastPrintAt = '';
  bool ifoodBusy = false;
  String ifoodConnectionId = '';
  String ifoodConnectionStatus = 'DISCONNECTED';
  String ifoodMerchantId = '';
  String ifoodMerchantName = '';
  String ifoodMessage = 'iFood nao conectado';
  String ifoodVerificationUrl = '';
  String ifoodUserCode = '';
  String ifoodLastSyncAt = '';
  final List<MercadoPagoTerminal> pointTerminals = [];

  final List<Product> products = [];
  final List<Order> orders = [];
  final List<Customer> customers = [];
  final List<TeamMember> teamMembers = [];
  final List<CashMovement> movements = [];
  final List<StockMovement> stockMovements = [];
  final List<String> syncQueue = [];
  StreamSubscription<List<Map<String, dynamic>>>? _menuSubscription;
  StreamSubscription<List<Map<String, dynamic>>>? _itemSubscription;
  StreamSubscription<List<Map<String, dynamic>>>? _orderSubscription;
  Timer? _syncDebounce;
  bool _syncRunning = false;
  final IFoodCloudClient _ifoodClient;
  final WhatsAppCloudClient _whatsappClient;

  List<String> get categories => [
    'LANCHES',
    'BEBIDAS',
    'PRATOS',
    'PIZZAS',
    'DELIVERY',
    'SOBREMESAS',
  ];
  bool get mercadoPagoCheckoutActive => pointReady;
  List<String> get paymentMethods => mercadoPagoCheckoutActive
      ? [
          'Dinheiro',
          'Pix Mercado Pago',
          'Debito Point',
          'Credito Point',
          'Fiado',
        ]
      : ['Dinheiro', 'Pix', 'Debito', 'Credito', 'Fiado'];

  Order? get selectedOrder {
    for (final order in orders) {
      if (order.id == selectedOrderId) return order;
    }
    return orders.where((order) => order.isOpen).firstOrNull;
  }

  List<Order> get openOrders => orders.where((order) => order.isOpen).toList();
  List<Order> get closedOrders =>
      orders.where((order) => order.status == OrderStatus.closed).toList();
  double get openTotal =>
      openOrders.fold(0, (sum, order) => sum + order.subtotal);
  double get soldToday =>
      closedOrders.fold(0, (sum, order) => sum + order.subtotal);
  double get grossProfit =>
      closedOrders.fold(0, (sum, order) => sum + order.profit);
  double get ifoodSales => orders
      .where((order) => order.kind == OrderKind.ifood)
      .fold(0, (sum, order) => sum + order.subtotal);
  double get ifoodRepasse => orders
      .where((order) => order.kind == OrderKind.ifood)
      .fold(0, (sum, order) => sum + order.ifoodRepasse);
  int get lowStockCount =>
      products.where((product) => product.stock <= product.minStock).length;
  int get pendingSyncCount => syncQueue.length;
  bool get ifoodConnected =>
      ifoodConnectionId.trim().isNotEmpty &&
      ifoodConnectionStatus.toUpperCase() == 'CONNECTED';
  bool get pointHasPending => pointPendingAmount > 0;
  bool get pointHasLicense => licenseKey.trim().isNotEmpty;
  bool get pointHasTerminal => pointTerminalId.trim().isNotEmpty;
  bool get pointReady => pointConnected && pointHasTerminal;
  String get pointStatusLabel {
    if (!pointHasLicense) return 'Conta da loja aguardando sincronizacao';
    if (!pointConnected) {
      return pointLastError.isEmpty
          ? 'Mercado Pago desconectado'
          : _friendlyPointMessage(pointLastError);
    }
    if (!pointHasTerminal) return 'Conta conectada, escolha uma Point';
    if (pointHasPending) return 'Aguardando pagamento';
    return _friendlyPointMessage(pointStatus);
  }

  String get pointTerminalDisplay {
    if (pointTerminalLabel.trim().isNotEmpty) return pointTerminalLabel.trim();
    if (pointDeviceName.trim().isNotEmpty) return pointDeviceName.trim();
    return 'Nenhuma Point selecionada';
  }

  String get printBridgeLabel {
    if (windowsBridgeLastPrintAt.isNotEmpty) {
      return 'Impresso pelo Windows $windowsBridgeLastPrintAt';
    }
    return windowsBridgeStatus;
  }

  bool isMercadoPagoMethod(String method) =>
      method.contains('Point') ||
      method.contains('Mercado Pago') ||
      method == 'Pix Mercado Pago';

  double get mercadoPagoSales => closedOrders
      .where((order) => isMercadoPagoMethod(order.paymentMethod))
      .fold(0, (sum, order) => sum + order.subtotal);
  int get mercadoPagoTransactions => closedOrders
      .where((order) => isMercadoPagoMethod(order.paymentMethod))
      .length;

  SupabaseClient get _supabase => Supabase.instance.client;

  @override
  void dispose() {
    _syncDebounce?.cancel();
    _cancelRealtime();
    _ifoodClient.dispose();
    _whatsappClient.dispose();
    super.dispose();
  }

  Future<void> hydrate() async {
    if (!hydrated) {
      _seed();
      hydrated = true;
      notifyListeners();
    }
    final SharedPreferences prefs;
    try {
      prefs = await SharedPreferences.getInstance();
    } catch (_) {
      return;
    }
    final raw = prefs.getString(_storageKey);
    if (raw == null) {
      await _save();
      notifyListeners();
      return;
    }
    final json = jsonDecode(raw) as Map<String, dynamic>;
    loggedIn = json['loggedIn'] as bool? ?? false;
    cashOpen = json['cashOpen'] as bool? ?? true;
    cashReconciliationRequired =
        json['cashReconciliationRequired'] as bool? ?? false;
    unreconciledCashOpenedAt = DateTime.tryParse(
      '${json['unreconciledCashOpenedAt'] ?? ''}',
    );
    onlineStoreOpen = json['onlineStoreOpen'] as bool? ?? false;
    licenseKey = json['licenseKey'] as String? ?? '';
    operatorName = json['operatorName'] as String? ?? 'Lucas';
    authEmail = json['authEmail'] as String? ?? '';
    syncStatus = json['syncStatus'] as String? ?? 'Offline local';
    publicMenuId = json['publicMenuId'] as String? ?? '';
    publicMenuSlug = json['publicMenuSlug'] as String? ?? '';
    businessName = json['businessName'] as String? ?? 'Hamburgueria do Val';
    businessLegalName = json['businessLegalName'] as String? ?? businessName;
    businessResponsible =
        json['businessResponsible'] as String? ?? 'Lucas Cesar';
    businessDocument =
        json['businessDocument'] as String? ?? '50.597.666/0001-47';
    businessPhone = json['businessPhone'] as String? ?? '(33) 99960-9457';
    businessCity = json['businessCity'] as String? ?? 'Governador Valadares';
    businessUf = json['businessUf'] as String? ?? 'MG';
    businessAddress =
        json['businessAddress'] as String? ?? 'Rua G 390, Turmalina';
    if (businessName == 'Balcao Livre PDV') {
      businessName = 'Hamburgueria do Val';
    }
    if (businessDocument.trim().isEmpty) {
      businessDocument = '50.597.666/0001-47';
    }
    if (businessPhone.trim().isEmpty) {
      businessPhone = '(33) 99960-9457';
    }
    selectedOrderId = json['selectedOrderId'] as String? ?? '';
    search = json['search'] as String? ?? '';
    lastSync = json['lastSync'] as String? ?? '';
    whatsappConnected = json['whatsappConnected'] as bool? ?? false;
    whatsappNumber = json['whatsappNumber'] as String? ?? '';
    whatsappSessionId = json['whatsappSessionId'] as String? ?? '';
    whatsappConnectionStatus =
        json['whatsappConnectionStatus'] as String? ??
        (whatsappConnected ? 'CONNECTED' : 'DISCONNECTED');
    whatsappMessage =
        json['whatsappMessage'] as String? ??
        (whatsappConnected ? 'WhatsApp conectado.' : 'WhatsApp nao conectado');
    whatsappOnboardingUrl = json['whatsappOnboardingUrl'] as String? ?? '';
    whatsappLastSyncAt = json['whatsappLastSyncAt'] as String? ?? '';
    securityMessage = json['securityMessage'] as String? ?? '';
    cloudBackupEnabled = json['cloudBackupEnabled'] as bool? ?? true;
    centralSyncEnabled = json['centralSyncEnabled'] as bool? ?? true;
    lastBackupAt = json['lastBackupAt'] as String? ?? '';
    backupMessage =
        json['backupMessage'] as String? ??
        'Nenhum backup gerado nesta instalacao.';
    pointConnected = json['pointConnected'] as bool? ?? false;
    pointConnectionStatus =
        json['pointConnectionStatus'] as String? ?? 'DISCONNECTED';
    pointDeviceName = json['pointDeviceName'] as String? ?? '';
    pointSerial = json['pointSerial'] as String? ?? '';
    pointStatus =
        json['pointStatus'] as String? ?? 'Mercado Pago nao configurado';
    pointChargeMethod =
        json['pointChargeMethod'] as String? ?? 'Pix Mercado Pago';
    pointPendingAmount = (json['pointPendingAmount'] as num?)?.toDouble() ?? 0;
    pointLastNsu = json['pointLastNsu'] as String? ?? '';
    pointSellerUserId = json['pointSellerUserId'] as String? ?? '';
    pointTerminalId = json['pointTerminalId'] as String? ?? '';
    pointTerminalLabel = json['pointTerminalLabel'] as String? ?? '';
    pointLastSyncAt = json['pointLastSyncAt'] as String? ?? '';
    pointLastError = json['pointLastError'] as String? ?? '';
    pointAttemptId = json['pointAttemptId'] as String? ?? '';
    pointOrderId = json['pointOrderId'] as String? ?? '';
    pointLocalReference = json['pointLocalReference'] as String? ?? '';
    pointConnectUrl = json['pointConnectUrl'] as String? ?? '';
    windowsBridgeUrl = json['windowsBridgeUrl'] as String? ?? '';
    windowsBridgeLocalUrl =
        json['windowsBridgeLocalUrl'] as String? ?? 'http://localhost:5050';
    windowsBridgeStatus =
        json['windowsBridgeStatus'] as String? ??
        'Windows bridge nao conectado';
    windowsBridgeLastPrintAt =
        json['windowsBridgeLastPrintAt'] as String? ?? '';
    ifoodConnectionId = json['ifoodConnectionId'] as String? ?? '';
    ifoodConnectionStatus =
        json['ifoodConnectionStatus'] as String? ?? 'DISCONNECTED';
    ifoodMerchantId = json['ifoodMerchantId'] as String? ?? '';
    ifoodMerchantName = json['ifoodMerchantName'] as String? ?? '';
    ifoodMessage = json['ifoodMessage'] as String? ?? 'iFood nao conectado';
    ifoodVerificationUrl = json['ifoodVerificationUrl'] as String? ?? '';
    ifoodUserCode = json['ifoodUserCode'] as String? ?? '';
    ifoodLastSyncAt = json['ifoodLastSyncAt'] as String? ?? '';
    if (pointConnected &&
        pointTerminalId.trim().isEmpty &&
        pointSerial == 'MP-BL-001') {
      pointConnected = false;
      pointConnectionStatus = 'DISCONNECTED';
      pointDeviceName = '';
      pointSerial = '';
      pointStatus = 'Mercado Pago nao configurado';
      pointLastError = 'Conexao antiga de demonstracao removida.';
    }
    products
      ..clear()
      ..addAll(
        (json['products'] as List<dynamic>? ?? []).map(
          (item) => Product.fromJson(item as Map<String, dynamic>),
        ),
      );
    orders
      ..clear()
      ..addAll(
        (json['orders'] as List<dynamic>? ?? []).map(
          (item) => Order.fromJson(item as Map<String, dynamic>),
        ),
      );
    customers
      ..clear()
      ..addAll(
        (json['customers'] as List<dynamic>? ?? []).map(
          (item) => Customer.fromJson(item as Map<String, dynamic>),
        ),
      );
    final savedTeam = json['teamMembers'] as List<dynamic>? ?? const [];
    if (savedTeam.isNotEmpty) {
      teamMembers
        ..clear()
        ..addAll(
          savedTeam.map(
            (item) => TeamMember.fromJson(item as Map<String, dynamic>),
          ),
        );
    }
    movements
      ..clear()
      ..addAll(
        (json['movements'] as List<dynamic>? ?? []).map(
          (item) => CashMovement.fromJson(item as Map<String, dynamic>),
        ),
      );
    stockMovements
      ..clear()
      ..addAll(
        (json['stockMovements'] as List<dynamic>? ?? []).map(
          (item) => StockMovement.fromJson(item as Map<String, dynamic>),
        ),
      );
    syncQueue
      ..clear()
      ..addAll((json['syncQueue'] as List<dynamic>? ?? []).cast<String>());
    if (products.isEmpty || orders.isEmpty) _seed();
    selectedOrderId = selectedOrder?.id ?? orders.first.id;
    if (loggedIn &&
        licenseKey.trim().isEmpty &&
        _supabase.auth.currentSession == null) {
      loggedIn = false;
      syncStatus = 'Entre com a conta Supabase da loja';
    }
    if (!loggedIn) {
      await connectFromWindowsBridge(notify: false);
    }
    hydrated = true;
    notifyListeners();
    if (loggedIn) {
      unawaited(resumeSupabaseSync());
    }
  }

  Future<void> login(String login, String password) async {
    final cleanLogin = login.trim().toLowerCase();
    final cleanPassword = password.trim();
    if (cleanLogin.isEmpty || cleanPassword.isEmpty) {
      authError = 'Informe e-mail e senha da conta Supabase.';
      notifyListeners();
      return;
    }

    authBusy = true;
    authError = '';
    syncStatus = 'Entrando na conta Supabase...';
    notifyListeners();
    try {
      await _supabase.auth.signInWithPassword(
        email: cleanLogin,
        password: cleanPassword,
      );
      authEmail = cleanLogin;
      await _loadLicenseForAuthenticatedUser();
      loggedIn = licenseKey.isNotEmpty;
      _enqueue('supabase_login');
      await resumeSupabaseSync();
      authBusy = false;
      await _saveAndNotify(pushSync: false);
    } catch (error) {
      try {
        if (_supabase.auth.currentSession != null) {
          await _supabase.auth.signOut();
        }
      } catch (_) {
        // Ignore sign-out cleanup errors after a refused login.
      }
      loggedIn = false;
      authBusy = false;
      authError = _friendlyAuthError(error);
      syncStatus = 'Login recusado';
      await _cancelRealtime();
      await _saveAndNotify(pushSync: false);
    }
  }

  Future<void> logout() async {
    await _cancelRealtime();
    if (_supabase.auth.currentSession != null) {
      await _supabase.auth.signOut();
    }
    loggedIn = false;
    syncStatus = 'Offline local';
    await _saveAndNotify(pushSync: false);
  }

  Future<bool> connectFromWindowsBridge({bool notify = true}) async {
    if (authBusy) return false;
    authBusy = true;
    authError = '';
    if (notify) notifyListeners();
    final response = await _getWindowsBridge('/api/mobile/status');
    authBusy = false;
    if (!response.ok) {
      if (notify) {
        authError = 'PDV Windows nao encontrado nesta rede.';
        notifyListeners();
      }
      return false;
    }

    _applyWindowsBridgeStatus(response.data);
    loggedIn = true;
    syncStatus = 'Conectado ao PDV Windows';
    lastSync = _time(DateTime.now());
    _enqueue('windows_bridge_login');
    if (notify) {
      await _saveAndNotify(pushSync: false);
    } else {
      await _save();
    }
    if (licenseKey.trim().isNotEmpty) {
      unawaited(resumeSupabaseSync());
    }
    return true;
  }

  Future<void> resumeSupabaseSync() async {
    if (licenseKey.trim().isEmpty) {
      if (_supabase.auth.currentSession != null) {
        await _loadLicenseForAuthenticatedUser();
      }
    }
    if (licenseKey.trim().isEmpty) return;
    syncStatus = 'Conectando tempo real...';
    notifyListeners();
    try {
      await _loadRemoteStoreOnce();
      await _startRealtime();
    } catch (error) {
      syncStatus = 'Sync por gateway ligado: ${_friendlyAuthError(error)}';
    }
    await _postMobileBootstrap();
    if (!syncStatus.toLowerCase().contains('erro')) {
      syncStatus = 'Tempo real ligado';
    }
    lastSync = _time(DateTime.now());
    await _saveAndNotify(pushSync: false);
  }

  Future<void> _loadLicenseForAuthenticatedUser() async {
    final user = _supabase.auth.currentUser;
    final email = (user?.email ?? authEmail).trim().toLowerCase();
    if (email.isEmpty) {
      throw const AuthException('Sessao sem email.');
    }

    final rows = await _supabase
        .from('bv_licenses')
        .select(
          'key,status,plan,customer_name,email,business_name,cnpj,phone,city,state,profile,expires_at,updated_at',
        )
        .eq('email', email)
        .order('updated_at', ascending: false)
        .limit(1);
    final list = _rows(rows);
    if (list.isEmpty) {
      throw AuthException(
        'Nenhuma licenca encontrada para $email. Cadastre esse email na licenca do cliente.',
      );
    }

    final row = list.first;
    final status = _text(row['status']).toUpperCase();
    final expiresAt = DateTime.tryParse(_text(row['expires_at']));
    if (status == 'BLOQUEADA') {
      throw const AuthException('Licenca bloqueada no painel.');
    }
    if (expiresAt != null && expiresAt.isBefore(DateTime.now())) {
      throw const AuthException('Licenca expirada. Renove para entrar.');
    }

    final profile = _map(row['profile']);
    licenseKey = _text(row['key']).toUpperCase();
    authEmail = email;
    businessName = _firstText([
      row['business_name'],
      profile['businessName'],
      profile['legalName'],
      row['customer_name'],
      businessName,
    ]);
    businessDocument = _firstText([
      row['cnpj'],
      profile['cnpj'],
      profile['businessDocument'],
      businessDocument,
    ]);
    businessPhone = _firstText([
      row['phone'],
      profile['phone'],
      profile['businessPhone'],
      businessPhone,
    ]);
    onlineStoreOpen = true;
  }

  Future<void> _loadRemoteStoreOnce() async {
    await _loadMenuOnce();
    if (publicMenuId.isNotEmpty) {
      await _loadMenuItemsOnce();
    }
    await _loadPublicOrdersOnce();
  }

  Future<void> _loadMenuOnce() async {
    final rows = await _supabase
        .from('bv_public_menus')
        .select(
          'id,store_id,slug,name,phone,address,city,state,store_open,updated_at',
        )
        .eq('store_id', licenseKey)
        .order('updated_at', ascending: false)
        .limit(1);
    final list = _rows(rows);
    if (list.isEmpty) return;
    _applyMenuRows(list);
  }

  Future<void> _loadMenuItemsOnce() async {
    final rows = await _supabase
        .from('bv_public_menu_items')
        .select(
          'id,code,name,category,price,stock_quantity,is_in_stock,is_active,sort_order,updated_at',
        )
        .eq('menu_id', publicMenuId)
        .order('sort_order', ascending: true);
    final list = _rows(rows);
    _applyProductRows(list);
  }

  Future<void> _loadPublicOrdersOnce() async {
    final rows = await _supabase
        .from('bv_public_orders')
        .select(
          'id,slug,source,status,customer_name,customer_phone,order_type,table_label,address,district,reference,notes,subtotal,delivery_fee,total,items,pdv_order_id,created_at,updated_at',
        )
        .eq('store_id', licenseKey)
        .order('created_at', ascending: false)
        .limit(80);
    final list = _rows(rows);
    _applyPublicOrderRows(list);
  }

  Future<void> _startRealtime() async {
    await _cancelRealtime();
    _menuSubscription = _supabase
        .from('bv_public_menus')
        .stream(primaryKey: ['id'])
        .eq('store_id', licenseKey)
        .listen((rows) async {
          _applyMenuRows(rows);
          await _saveAndNotify(pushSync: false);
          if (publicMenuId.isNotEmpty) {
            await _restartItemRealtime();
          }
        }, onError: _handleRealtimeError);

    await _restartItemRealtime();

    _orderSubscription = _supabase
        .from('bv_public_orders')
        .stream(primaryKey: ['id'])
        .eq('store_id', licenseKey)
        .listen((rows) async {
          _applyPublicOrderRows(rows);
          await _saveAndNotify(pushSync: false);
        }, onError: _handleRealtimeError);
  }

  Future<void> _restartItemRealtime() async {
    await _itemSubscription?.cancel();
    _itemSubscription = null;
    if (publicMenuId.isEmpty) return;
    _itemSubscription = _supabase
        .from('bv_public_menu_items')
        .stream(primaryKey: ['id'])
        .eq('menu_id', publicMenuId)
        .listen((rows) async {
          _applyProductRows(rows);
          await _saveAndNotify(pushSync: false);
        }, onError: _handleRealtimeError);
  }

  Future<void> _cancelRealtime() async {
    await _menuSubscription?.cancel();
    await _itemSubscription?.cancel();
    await _orderSubscription?.cancel();
    _menuSubscription = null;
    _itemSubscription = null;
    _orderSubscription = null;
  }

  void _handleRealtimeError(Object error) {
    syncStatus = 'Tempo real com erro: ${_friendlyAuthError(error)}';
    notifyListeners();
  }

  void _applyMenuRows(List<Map<String, dynamic>> rows) {
    if (rows.isEmpty) return;
    rows.sort(
      (a, b) => _text(b['updated_at']).compareTo(_text(a['updated_at'])),
    );
    final row = rows.first;
    publicMenuId = _text(row['id']);
    publicMenuSlug = _text(row['slug']);
    businessName = _firstText([row['name'], businessName]);
    businessPhone = _firstText([row['phone'], businessPhone]);
    onlineStoreOpen = row['store_open'] != false;
  }

  void _applyProductRows(List<Map<String, dynamic>> rows) {
    final remote = rows.map(_productFromMenuRow).toList();
    products
      ..clear()
      ..addAll(remote.where((product) => product.active));
  }

  Product _productFromMenuRow(Map<String, dynamic> row) {
    final price = _number(row['price']);
    return Product(
      id: _text(row['id']).isEmpty
          ? _firstText([row['code'], row['name'], _id('prd')])
          : _text(row['id']),
      code: _firstText([row['code'], row['id']]),
      name: _firstText([row['name'], 'Produto']),
      category: _firstText([row['category'], 'Cardapio']).toUpperCase(),
      price: price,
      cost: 0,
      stock: _number(row['stock_quantity']).round(),
      minStock: 0,
      active: row['is_active'] != false,
    );
  }

  void _applyPublicOrderRows(List<Map<String, dynamic>> rows) {
    orders.removeWhere((order) => order.id.startsWith('remote-'));
    final remoteOrders = rows.map(_orderFromPublicRow).toList();
    orders.insertAll(0, remoteOrders);
    selectedOrderId =
        selectedOrder?.id ?? (orders.isEmpty ? '' : orders.first.id);
  }

  Order _orderFromPublicRow(Map<String, dynamic> row) {
    final id = _text(row['id']);
    final source = _text(row['source']).toUpperCase();
    final orderType = _text(row['order_type']).toUpperCase();
    final status = _publicStatusToOrderStatus(_text(row['status']));
    final kind = source.contains('IFOOD')
        ? OrderKind.ifood
        : orderType == 'DELIVERY'
        ? OrderKind.delivery
        : orderType == 'PICKUP'
        ? OrderKind.counter
        : OrderKind.table;
    final createdAt =
        DateTime.tryParse(_text(row['created_at'])) ?? DateTime.now();
    final order = Order(
      id: 'remote-$id',
      number: _firstText([
        row['pdv_order_id'],
        orderType == 'TABLE' ? row['table_label'] : null,
        id.length >= 5 ? id.substring(0, 5).toUpperCase() : id,
      ]),
      kind: kind,
      status: status,
      createdAt: createdAt,
      customerName: _firstText([
        row['customer_name'],
        row['table_label'],
        _kindName(kind),
      ]),
      address: [
        _text(row['address']),
        _text(row['district']),
        _text(row['reference']),
      ].where((part) => part.isNotEmpty).join(' - '),
      ifoodRepasse: kind == OrderKind.ifood ? _number(row['total']) * 0.88 : 0,
    );
    final items = _list(row['items']);
    for (final item in items) {
      final map = _map(item);
      final quantity = _number(map['quantity']).round().clamp(1, 999);
      final price = _number(map['price']);
      order.items.add(
        OrderItem(
          productId: _firstText([map['productId'], map['code'], map['name']]),
          code: _firstText([map['code'], '']),
          name: _firstText([map['name'], 'Item']),
          quantity: quantity,
          price: price,
          cost: 0,
        ),
      );
    }
    if (order.items.isEmpty && _number(row['total']) > 0) {
      order.items.add(
        OrderItem(
          productId: 'remote-total-$id',
          code: '',
          name: 'Pedido online',
          quantity: 1,
          price: _number(row['total']),
          cost: 0,
        ),
      );
    }
    return order;
  }

  OrderStatus _publicStatusToOrderStatus(String value) {
    final status = value.toUpperCase();
    if (status.contains('CANCEL')) return OrderStatus.canceled;
    if (status.contains('FINAL') ||
        status.contains('ENTREG') ||
        status.contains('FECH')) {
      return OrderStatus.closed;
    }
    if (status.contains('DESPACH') || status.contains('ROTA')) {
      return OrderStatus.dispatched;
    }
    if (status.contains('PREPAR') ||
        status.contains('RECEB') ||
        status.contains('IMPORT')) {
      return OrderStatus.preparing;
    }
    return OrderStatus.open;
  }

  Future<void> _postMobileBootstrap() async {
    final response = await _postLicense('/mobile/bootstrap', {
      ..._baseLicensePayload('mobile.bootstrap'),
    });
    if (response.ok) {
      final snapshot = _map(response.data['snapshot']);
      if (snapshot.isNotEmpty) {
        _applyMobileSnapshot(snapshot);
      }
      syncStatus = 'Tempo real ligado';
      return;
    }
    syncStatus = response.message;
  }

  void _applyMobileSnapshot(Map<String, dynamic> snapshot) {
    final settings = _map(snapshot['settings']);
    final profile = _map(snapshot['profile']);
    businessName = _firstText([
      profile['businessName'],
      profile['legalName'],
      profile['ownerName'],
      businessName,
    ]);
    businessLegalName = _firstText([
      profile['legalName'],
      profile['businessLegalName'],
      businessLegalName,
      businessName,
    ]);
    businessResponsible = _firstText([
      profile['ownerName'],
      profile['responsible'],
      profile['businessResponsible'],
      businessResponsible,
    ]);
    businessDocument = _firstText([
      profile['businessDocument'],
      profile['cnpj'],
      businessDocument,
    ]);
    businessPhone = _firstText([
      profile['businessPhone'],
      profile['phone'],
      businessPhone,
    ]);
    businessCity = _firstText([profile['city'], businessCity]);
    businessUf = _firstText([profile['uf'], businessUf]).toUpperCase();
    businessAddress = _firstText([profile['address'], businessAddress]);
    authEmail = _firstText([profile['email'], authEmail]).toLowerCase();
    if (settings.containsKey('cashOpen')) {
      cashOpen = _bool(settings['cashOpen']);
    }
    if (settings.containsKey('cashReconciliationRequired')) {
      cashReconciliationRequired = _bool(
        settings['cashReconciliationRequired'],
      );
    }
    if (settings.containsKey('unreconciledCashOpenedAt')) {
      unreconciledCashOpenedAt = DateTime.tryParse(
        _text(settings['unreconciledCashOpenedAt']),
      );
    }
    ifoodConnectionId = _firstText([
      settings['ifoodConnectionId'],
      ifoodConnectionId,
    ]);
    ifoodConnectionStatus = _firstText([
      settings['ifoodConnectionStatus'],
      ifoodConnectionStatus,
    ]).toUpperCase();
    ifoodMerchantId = _firstText([
      settings['ifoodMerchantId'],
      ifoodMerchantId,
    ]);
    ifoodMerchantName = _firstText([
      settings['ifoodMerchantName'],
      ifoodMerchantName,
    ]);
    ifoodLastSyncAt = _firstText([
      settings['ifoodLastSyncAt'],
      ifoodLastSyncAt,
    ]);
    final bridgeUrl = _normalizeBridgeBaseUrl(
      _firstText([
        settings['windowsBridgeUrl'],
        settings['waiterBridgeNetworkUrl'],
      ]),
    );
    final bridgeLocalUrl = _normalizeBridgeBaseUrl(
      _firstText([
        settings['windowsBridgeLocalUrl'],
        settings['waiterBridgeLocalUrl'],
      ]),
    );
    if (bridgeUrl.isNotEmpty) windowsBridgeUrl = bridgeUrl;
    if (bridgeLocalUrl.isNotEmpty) windowsBridgeLocalUrl = bridgeLocalUrl;
    if (windowsBridgeUrl.isNotEmpty || windowsBridgeLocalUrl.isNotEmpty) {
      windowsBridgeStatus = 'Bridge Windows pronto para imprimir';
    }
    lastSync = _firstText([settings['lastSyncAt'], lastSync]);

    final productRows = _list(snapshot['products']).map(_map).toList();
    if (productRows.isNotEmpty) {
      products
        ..clear()
        ..addAll(productRows.map(_productFromSnapshot));
    }

    final orderRows = _list(snapshot['orders']).map(_map).toList();
    if (orderRows.isNotEmpty) {
      orders
        ..clear()
        ..addAll(orderRows.map(_orderFromSnapshot));
      selectedOrderId = selectedOrder?.id ?? orders.first.id;
    }

    final customerRows = _list(snapshot['customers']).map(_map).toList();
    if (customerRows.isNotEmpty) {
      customers
        ..clear()
        ..addAll(customerRows.map(_customerFromSnapshot));
    }

    final teamRows = _list(
      snapshot['teamMembers'] ?? snapshot['users'],
    ).map(_map).toList();
    if (teamRows.isNotEmpty) {
      teamMembers
        ..clear()
        ..addAll(teamRows.map(TeamMember.fromJson));
    }

    final movementRows = _list(
      snapshot['cashMovements'] ?? snapshot['movements'],
    ).map(_map).toList();
    if (movementRows.isNotEmpty) {
      movements
        ..clear()
        ..addAll(movementRows.map(_cashMovementFromSnapshot));
    }
    final stockMovementRows = _list(
      snapshot['stockMovements'],
    ).map(_map).toList();
    if (stockMovementRows.isNotEmpty) {
      stockMovements
        ..clear()
        ..addAll(
          stockMovementRows.map(
            (row) => StockMovement(
              id: _firstText([row['id'], _id('stk')]),
              productId: _firstText([row['productId'], row['productCode']]),
              type: _firstText([row['type'], 'AJUSTE']).toUpperCase(),
              quantity: _number(row['quantity']).round(),
              note: _firstText([row['note'], row['reason']]),
              createdAt:
                  DateTime.tryParse(_text(row['createdAt'])) ?? DateTime.now(),
            ),
          ),
        );
    }
  }

  void _applyWindowsBridgeStatus(Map<String, dynamic> data) {
    final session = _map(data['session']);
    final state = _map(data['state']);

    licenseKey = _firstText([
      session['licenseKey'],
      session['license'],
      data['licenseKey'],
      licenseKey,
    ]).toUpperCase();
    authEmail = _firstText([
      session['loginEmail'],
      session['email'],
      authEmail,
    ]).toLowerCase();
    operatorName = _firstText([
      session['operatorName'],
      session['responsible'],
      operatorName,
    ]);
    businessName = _firstText([
      session['businessName'],
      state['restaurantName'],
      businessName,
    ]);
    businessLegalName = _firstText([
      session['legalName'],
      session['businessLegalName'],
      businessLegalName,
      businessName,
    ]);
    businessResponsible = _firstText([
      session['responsible'],
      session['operatorName'],
      businessResponsible,
    ]);
    businessDocument = _firstText([
      session['document'],
      session['cnpj'],
      businessDocument,
    ]);
    businessPhone = _firstText([session['phone'], businessPhone]);
    businessCity = _firstText([session['city'], businessCity]);
    businessUf = _firstText([session['uf'], businessUf]).toUpperCase();
    businessAddress = _firstText([session['address'], businessAddress]);
    if (session.containsKey('cashOpen')) {
      cashOpen = _bool(session['cashOpen']);
    }
    if (session.containsKey('cashReconciliationRequired')) {
      cashReconciliationRequired = _bool(session['cashReconciliationRequired']);
    }
    if (session.containsKey('unreconciledCashOpenedAt')) {
      unreconciledCashOpenedAt = DateTime.tryParse(
        _text(session['unreconciledCashOpenedAt']),
      );
    }
    if (session.containsKey('onlineStoreOpen')) {
      onlineStoreOpen = _bool(session['onlineStoreOpen']);
    } else {
      onlineStoreOpen = true;
    }

    final localUrl = _normalizeBridgeBaseUrl(data['localUrl']);
    final networkUrl = _normalizeBridgeBaseUrl(data['networkUrl']);
    if (localUrl.isNotEmpty) windowsBridgeLocalUrl = localUrl;
    if (networkUrl.isNotEmpty) windowsBridgeUrl = networkUrl;
    windowsBridgeStatus = 'PDV Windows conectado';

    final productRows = _list(state['products']).map(_map).toList();
    if (productRows.isNotEmpty) {
      products
        ..clear()
        ..addAll(productRows.map(_productFromSnapshot));
    }
    final boardRows = _list(state['boards']).map(_map).toList();
    if (boardRows.isNotEmpty) {
      orders
        ..clear()
        ..addAll(boardRows.map(_orderFromSnapshot));
      selectedOrderId = selectedOrder?.id ?? orders.first.id;
    }
  }

  Product _productFromSnapshot(Map<String, dynamic> row) {
    final code = _firstText([row['code'], row['id'], _id('prd')]);
    return Product(
      id: _firstText([row['id'], code]),
      code: code,
      name: _firstText([row['name'], 'Produto']),
      category: _firstText([
        row['category'],
        row['group'],
        'Cardapio',
      ]).toUpperCase(),
      price: _number(row['price'] ?? row['unitPrice']),
      cost: _number(row['cost'] ?? row['costPrice']),
      stock: _number(row['stock'] ?? row['stockQuantity']).round(),
      minStock: _number(row['minStock'] ?? row['minimumStock']).round(),
      unit: _firstText([
        row['unit'],
        row['unitOfMeasure'],
        row['measureUnit'],
        'un',
      ]).toLowerCase(),
      imageData: _firstText([
        row['imageData'],
        row['imageUrl'],
        row['photoUrl'],
      ]),
      active: row['active'] != false,
    );
  }

  Order _orderFromSnapshot(Map<String, dynamic> row) {
    final id = _firstText([row['id'], row['number'], _id('ord')]);
    final order = Order(
      id: id,
      number: _firstText([row['number'], id]),
      kind: _orderKindFromText(row['kind']),
      status: _orderStatusFromText(row['status']),
      createdAt:
          DateTime.tryParse(_text(row['createdAt'] ?? row['serverTime'])) ??
          DateTime.now(),
      customerName: _firstText([
        row['customerName'],
        row['tableLabel'],
        row['title'],
      ]),
      waiter: _firstText([row['waiter'], '1']),
      address: _firstText([row['address'], row['district']]),
      paymentMethod: _text(row['paymentMethod']),
      ifoodRepasse: _number(row['ifoodRepasse']),
      coverCharge: _number(row['coverCharge'] ?? row['couvert']),
      servicePercent: _number(row['servicePercent'] ?? row['waiterPercent']),
    );
    final rawItems = _list(row['items']);
    final items = (rawItems.isEmpty ? _list(row['lines']) : rawItems).map(_map);
    for (final item in items) {
      order.items.add(
        OrderItem(
          productId: _firstText([
            item['productId'],
            item['code'],
            item['name'],
          ]),
          code: _firstText([item['code'], item['productCode']]),
          name: _firstText([item['name'], 'Item']),
          quantity: _number(item['quantity']).round().clamp(1, 999),
          price: _number(item['price'] ?? item['unitPrice']),
          cost: _number(item['cost'] ?? item['costPrice']),
        ),
      );
    }
    return order;
  }

  Customer _customerFromSnapshot(Map<String, dynamic> row) {
    return Customer(
      id: _firstText([row['id'], row['phone'], row['name'], _id('cus')]),
      name: _firstText([row['name'], 'Cliente']),
      phone: _text(row['phone']),
      document: _firstText([row['document'], row['cpf'], row['cnpj']]),
      address: [
        _text(row['address']),
        _text(row['district']),
      ].where((part) => part.isNotEmpty).join(' - '),
      points: _number(row['points']).round(),
      cashback: _number(row['cashback'] ?? row['cashbackBalance']),
      lastPurchaseAt: DateTime.tryParse(_text(row['lastPurchaseAt'])),
    );
  }

  CashMovement _cashMovementFromSnapshot(Map<String, dynamic> row) {
    return CashMovement(
      id: _firstText([row['id'], _id('mov')]),
      type: _firstText([row['type'], 'Movimento']),
      amount: _number(row['amount']),
      note: _firstText([row['note'], row['description']]),
      createdAt:
          DateTime.tryParse(_firstText([row['createdAt'], row['when']])) ??
          DateTime.now(),
    );
  }

  OrderKind _orderKindFromText(Object? value) {
    final text = _text(value).toLowerCase();
    if (text.contains('ifood')) return OrderKind.ifood;
    if (text.contains('delivery')) return OrderKind.delivery;
    if (text.contains('balcao') || text.contains('counter')) {
      return OrderKind.counter;
    }
    return OrderKind.table;
  }

  OrderStatus _orderStatusFromText(Object? value) {
    final text = _text(value).toLowerCase();
    if (text.contains('cancel')) return OrderStatus.canceled;
    if (text.contains('fech') ||
        text.contains('final') ||
        text.contains('entreg')) {
      return OrderStatus.closed;
    }
    if (text.contains('rota') || text.contains('despach')) {
      return OrderStatus.dispatched;
    }
    if (text.contains('prepar') || text.contains('novo')) {
      return OrderStatus.preparing;
    }
    return OrderStatus.open;
  }

  Future<void> setSearch(String value) async {
    search = value;
    notifyListeners();
  }

  List<Product> filteredProducts() {
    final q = search.trim().toLowerCase();
    if (q.isEmpty) return products.where((product) => product.active).toList();
    return products
        .where(
          (product) =>
              product.active &&
              (product.name.toLowerCase().contains(q) ||
                  product.code.toLowerCase().contains(q) ||
                  product.category.toLowerCase().contains(q)),
        )
        .toList();
  }

  Future<void> toggleCash() async {
    if (!cashOpen && cashReconciliationRequired) return;
    cashOpen = !cashOpen;
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: cashOpen ? 'ABERTURA' : 'FECHAMENTO',
        amount: 0,
        note: cashOpen
            ? 'Caixa aberto pelo mobile'
            : 'Caixa fechado pelo mobile',
        createdAt: DateTime.now(),
      ),
    );
    _enqueue(cashOpen ? 'cash_opened' : 'cash_closed');
    await _saveAndNotify();
  }

  Future<void> openCash({
    required double initialAmount,
    required String operator,
  }) async {
    if (cashOpen || cashReconciliationRequired) return;
    final safeAmount = initialAmount.isFinite
        ? initialAmount.clamp(0, double.maxFinite).toDouble()
        : 0.0;
    final safeOperator = operator.trim().isEmpty
        ? operatorName
        : operator.trim();
    cashOpen = true;
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: 'ABERTURA',
        amount: safeAmount,
        note:
            'Dinheiro vivo inicial. Caixa aberto por ${safeOperator.isEmpty ? 'OPERADOR' : safeOperator}.',
        createdAt: DateTime.now(),
      ),
    );
    _enqueue('cash_opened');
    await _saveAndNotify();
  }

  Future<String?> reconcilePreviousCash(
    String operator,
    String password,
  ) async {
    final cleanOperator = operator.trim();
    final cleanPassword = password.trim();
    if (cleanOperator.isEmpty || cleanPassword.isEmpty) {
      return 'Informe o operador e a senha do gerente.';
    }
    if (!cashReconciliationRequired) return null;

    final bridge = await _postWindowsBridge('/api/mobile/cash/reconcile', {
      'operator': cleanOperator,
      'password': cleanPassword,
    });
    if (bridge.ok) {
      await _completeCashReconciliation(
        bridge.message.isEmpty
            ? 'Caixa anterior reconciliado.'
            : bridge.message,
      );
      return null;
    }

    final email = authEmail.trim().toLowerCase();
    final normalizedOperator = cleanOperator.toLowerCase();
    final operatorMatches =
        normalizedOperator == email ||
        normalizedOperator == operatorName.trim().toLowerCase();
    if (email.isNotEmpty && operatorMatches) {
      try {
        final response = await _supabase.auth.signInWithPassword(
          email: email,
          password: cleanPassword,
        );
        if (response.user != null) {
          await _completeCashReconciliation(
            'Caixa anterior conferido e fechado por $cleanOperator.',
          );
          return null;
        }
      } catch (error) {
        final authMessage = _friendlyAuthError(error);
        if (authMessage.trim().isNotEmpty) return authMessage;
      }
    }

    return bridge.message.isEmpty
        ? 'Nao foi possivel validar o gerente no PDV Windows.'
        : bridge.message;
  }

  Future<void> _completeCashReconciliation(String note) async {
    cashReconciliationRequired = false;
    unreconciledCashOpenedAt = null;
    cashOpen = false;
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: 'FECHAMENTO',
        amount: 0,
        note: note,
        createdAt: DateTime.now(),
      ),
    );
    _enqueue('cash_reconciled');
    await _saveAndNotify(pushSync: false);
  }

  Future<void> addMovement(String type, double amount, String note) async {
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: type,
        amount: amount,
        note: note,
        createdAt: DateTime.now(),
      ),
    );
    _enqueue('cash_movement');
    await _saveAndNotify();
  }

  Future<void> selectOrder(String id) async {
    selectedOrderId = id;
    await _saveAndNotify();
  }

  Future<void> openOrder(
    OrderKind kind, {
    String customer = '',
    String address = '',
    String number = '',
  }) async {
    final prefix = switch (kind) {
      OrderKind.delivery => 'D',
      OrderKind.ifood => 'I',
      OrderKind.counter => 'B',
      OrderKind.table => 'M',
    };
    final requestedDigits = number.replaceAll(RegExp(r'[^0-9]'), '');
    final orderNumber = requestedDigits.isNotEmpty
        ? '$prefix${requestedDigits.padLeft(5, '0')}'
        : '$prefix${(orders.length + 1).toString().padLeft(5, '0')}';
    final order = Order(
      id: _id('ord'),
      number: orderNumber,
      kind: kind,
      status: kind == OrderKind.ifood
          ? OrderStatus.preparing
          : OrderStatus.open,
      createdAt: DateTime.now(),
      customerName: customer,
      address: address,
      ifoodRepasse: kind == OrderKind.ifood ? 0 : 0,
    );
    orders.insert(0, order);
    selectedOrderId = order.id;
    _enqueue('order_opened');
    await _saveAndNotify();
  }

  Future<void> updateSelectedOrderInfo({
    String? customerName,
    String? waiter,
  }) async {
    final order = selectedOrder;
    if (order == null || !order.isOpen) return;
    if (customerName != null) {
      order.customerName = customerName.trim();
    }
    if (waiter != null) {
      order.waiter = waiter.trim().isEmpty ? '1' : waiter.trim();
    }
    _enqueue('order_info_updated');
    await _saveAndNotify();
  }

  Future<void> updateSelectedOrderCharges({
    String? coverCharge,
    String? servicePercent,
  }) async {
    final order = selectedOrder;
    if (order == null || !order.isOpen) return;
    if (coverCharge != null) {
      order.coverCharge = _number(coverCharge).clamp(0, 99999).toDouble();
    }
    if (servicePercent != null) {
      order.servicePercent = _number(servicePercent).clamp(0, 100).toDouble();
    }
    _enqueue('order_charges_updated');
    await _saveAndNotify();
  }

  Future<void> addProduct(Product product, {int quantity = 1}) async {
    final order = selectedOrder;
    if (order == null || !order.isOpen || !cashOpen) return;
    final qty = quantity.clamp(1, 99).toInt();
    final existing = order.items
        .where((item) => item.productId == product.id)
        .firstOrNull;
    if (existing == null) {
      order.items.add(
        OrderItem(
          productId: product.id,
          code: product.code,
          name: product.name,
          quantity: qty,
          price: product.price,
          cost: product.cost,
        ),
      );
    } else {
      existing.quantity += qty;
    }
    product.stock = product.stock > qty ? product.stock - qty : 0;
    _enqueue('item_added');
    await _saveAndNotify();
  }

  Future<void> changeQty(OrderItem item, int delta) async {
    final order = selectedOrder;
    if (order == null) return;
    item.quantity += delta;
    final product = products
        .where((product) => product.id == item.productId)
        .firstOrNull;
    if (product != null) product.stock = product.stock - delta;
    if (item.quantity <= 0) order.items.remove(item);
    _enqueue('item_quantity_changed');
    await _saveAndNotify();
  }

  Future<void> closeSelected(String method) async {
    final order = selectedOrder;
    if (order == null || order.items.isEmpty || !order.isOpen) return;
    _openCashForPaymentIfNeeded();
    _closeOrder(order, method);
    if (isMercadoPagoMethod(method)) {
      pointStatus = 'Pagamento aprovado';
      pointChargeMethod = method;
      pointPendingAmount = 0;
    }
    _enqueue('sale_closed');
    selectedOrderId = openOrders.isEmpty ? order.id : openOrders.first.id;
    await _saveAndNotify();
    unawaited(
      _settleAndPrintThroughWindows(order, method).then((_) async {
        await _saveAndNotify(pushSync: false);
      }),
    );
  }

  void _openCashForPaymentIfNeeded() {
    if (cashOpen) return;
    cashOpen = true;
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: 'ABERTURA',
        amount: 0,
        note: 'Caixa aberto automaticamente ao receber venda',
        createdAt: DateTime.now(),
      ),
    );
    _enqueue('cash_auto_opened_for_payment');
  }

  Future<void> transferOrder({
    required String sourceId,
    String? targetId,
    String? targetNumber,
  }) async {
    final source = orders.where((order) => order.id == sourceId).firstOrNull;
    if (source == null || source.items.isEmpty) return;
    Order target;
    if (targetId != null) {
      final existing = orders
          .where((order) => order.id == targetId)
          .firstOrNull;
      if (existing == null || existing.id == source.id) return;
      target = existing;
    } else {
      target = Order(
        id: _id('ord'),
        number:
            targetNumber ??
            'M${(orders.length + 1).toString().padLeft(5, '0')}',
        kind:
            source.kind == OrderKind.ifood || source.kind == OrderKind.delivery
            ? OrderKind.delivery
            : OrderKind.table,
        status: OrderStatus.open,
        createdAt: DateTime.now(),
        customerName: source.customerName,
        waiter: source.waiter,
        address: source.address,
      );
      orders.insert(0, target);
    }
    for (final item in source.items) {
      final existing = target.items
          .where((targetItem) => targetItem.productId == item.productId)
          .firstOrNull;
      if (existing == null) {
        target.items.add(
          OrderItem(
            productId: item.productId,
            code: item.code,
            name: item.name,
            quantity: item.quantity,
            price: item.price,
            cost: item.cost,
          ),
        );
      } else {
        existing.quantity += item.quantity;
      }
    }
    source.items.clear();
    source.status = OrderStatus.canceled;
    source.paymentMethod = 'TRANSFERIDA';
    source.customerName = source.customerName.isEmpty
        ? 'Transferida para ${target.number}'
        : '${source.customerName} -> ${target.number}';
    selectedOrderId = target.id;
    _enqueue('order_transferred');
    await _saveAndNotify();
  }

  Future<void> settleOpenOrdersAndCloseCash() async {
    final pending = openOrders.toList();
    for (final order in pending) {
      if (order.items.isEmpty) {
        order.status = OrderStatus.canceled;
        order.paymentMethod = 'SEM MOVIMENTO';
        continue;
      }
      final method = order.kind == OrderKind.ifood
          ? 'iFood repasse'
          : 'Baixa caixa';
      _closeOrder(order, method);
    }
    cashOpen = false;
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: 'FECHAMENTO',
        amount: 0,
        note: 'Caixa fechado apos baixa automatica',
        createdAt: DateTime.now(),
      ),
    );
    _enqueue('cash_auto_settled');
    await _saveAndNotify();
  }

  void _closeOrder(Order order, String method) {
    order.status = OrderStatus.closed;
    order.paymentMethod = method;
    if (order.kind == OrderKind.ifood) {
      order.ifoodRepasse = order.subtotal * 0.88;
    }
    movements.insert(
      0,
      CashMovement(
        id: _id('mov'),
        type: 'VENDA',
        amount: order.subtotal,
        note: '${order.number} - $method',
        createdAt: DateTime.now(),
      ),
    );
    final customer = customers
        .where(
          (customer) =>
              customer.name.toLowerCase() == order.customerName.toLowerCase(),
        )
        .firstOrNull;
    if (customer != null) {
      customer.lastPurchaseAt = DateTime.now();
      customer.points += order.subtotal ~/ 10;
      customer.cashback += order.subtotal * 0.02;
    }
  }

  Future<void> _settleAndPrintThroughWindows(Order order, String method) async {
    final closeOnly = await _postWindowsBridge('/api/mobile/import', {
      'events': [_bridgeCloseEvent(order, method)],
    });
    if (_bridgeImported(closeOnly)) {
      _markWindowsPrinted('Windows baixou e imprimiu a venda');
      _enqueue('windows_bridge_settled');
      return;
    }

    if (_bridgeCanImportFull(closeOnly)) {
      final fullImport = await _postWindowsBridge('/api/mobile/import', {
        'events': _bridgeFullCloseEvents(order, method),
      });
      if (_bridgeImported(fullImport)) {
        _markWindowsPrinted('Windows recebeu, baixou e imprimiu a venda');
        _enqueue('windows_bridge_imported_and_settled');
        return;
      }
    }

    final printOnly = await _postWindowsBridge('/api/mobile/print', {
      'kind': 'receipt',
      'jobName': 'Balcao Livre ${order.number}',
      'compact': true,
      'content': _receiptText(order, method),
    });
    if (printOnly.ok) {
      _markWindowsPrinted('Impresso pelo Windows bridge');
      _enqueue('windows_bridge_printed');
      return;
    }

    final message = printOnly.message.isNotEmpty
        ? printOnly.message
        : closeOnly.message;
    windowsBridgeStatus = message.isEmpty
        ? 'Windows bridge nao encontrado para imprimir'
        : message;
    _enqueue('windows_bridge_print_failed');
  }

  Map<String, dynamic> _bridgeCloseEvent(Order order, String method) {
    return {
      'id':
          'flutter-close-${order.id}-${DateTime.now().millisecondsSinceEpoch}',
      'type': 'order.closed',
      'createdAt': DateTime.now().toIso8601String(),
      'payload': {
        'kind': _windowsKind(order.kind),
        'number': order.number,
        'boardNumber': order.number,
        'waiter': order.waiter,
        'customerName': order.customerName,
        'method': _windowsPaymentMethod(method),
        'amount': order.subtotal,
      },
    };
  }

  List<Map<String, dynamic>> _bridgeFullCloseEvents(
    Order order,
    String method,
  ) {
    final now = DateTime.now();
    final events = <Map<String, dynamic>>[
      {
        'id': 'flutter-open-${order.id}-${now.millisecondsSinceEpoch}',
        'type': 'order.opened',
        'createdAt': now.toIso8601String(),
        'payload': {
          'kind': _windowsKind(order.kind),
          'number': order.number,
          'boardNumber': order.number,
          'waiter': order.waiter,
          'customerName': order.customerName,
        },
      },
    ];
    for (var index = 0; index < order.items.length; index++) {
      final item = order.items[index];
      events.add({
        'id': 'flutter-item-${order.id}-$index-${now.millisecondsSinceEpoch}',
        'type': 'order.item_added',
        'createdAt': now.toIso8601String(),
        'payload': {
          'order': {
            'kind': _windowsKind(order.kind),
            'number': order.number,
            'boardNumber': order.number,
            'waiter': order.waiter,
          },
          'item': {'code': item.code, 'quantity': item.quantity, 'note': ''},
        },
      });
    }
    events.add(_bridgeCloseEvent(order, method));
    return events;
  }

  Future<_PaymentsResponse> _postWindowsBridge(
    String path,
    Map<String, dynamic> payload,
  ) async {
    _PaymentsResponse? lastError;
    for (final baseUrl in _windowsBridgeCandidates()) {
      final endpoint = Uri.tryParse('$baseUrl$path');
      if (endpoint == null) continue;
      try {
        final response = await http
            .post(
              endpoint,
              headers: const {'content-type': 'application/json'},
              body: jsonEncode(payload),
            )
            .timeout(const Duration(seconds: 2));
        final text = utf8.decode(response.bodyBytes);
        Map<String, dynamic> data = {};
        if (text.trim().isNotEmpty) {
          final decoded = jsonDecode(text);
          if (decoded is Map<String, dynamic>) data = decoded;
        }
        final ok =
            response.statusCode >= 200 &&
            response.statusCode < 300 &&
            data['ok'] != false;
        final result = _PaymentsResponse(
          ok: ok,
          statusCode: response.statusCode,
          data: data,
          fallbackMessage: ok
              ? ''
              : 'Windows bridge retornou ${response.statusCode}.',
        );
        if (ok || result.message.isNotEmpty) {
          windowsBridgeUrl = baseUrl;
          return result;
        }
        lastError = result;
      } catch (error) {
        lastError = _PaymentsResponse.error('Bridge indisponivel em $baseUrl');
      }
    }
    return lastError ??
        _PaymentsResponse.error('Windows bridge nao configurado.');
  }

  Future<_PaymentsResponse> _getWindowsBridge(String path) async {
    _PaymentsResponse? lastError;
    _PaymentsResponse? firstOk;
    for (final baseUrl in _windowsBridgeCandidates()) {
      final endpoint = Uri.tryParse('$baseUrl$path');
      if (endpoint == null) continue;
      try {
        final response = await http
            .get(endpoint)
            .timeout(const Duration(seconds: 2));
        final text = utf8.decode(response.bodyBytes);
        Map<String, dynamic> data = {};
        if (text.trim().isNotEmpty) {
          final decoded = jsonDecode(text);
          if (decoded is Map<String, dynamic>) data = decoded;
        }
        final ok =
            response.statusCode >= 200 &&
            response.statusCode < 300 &&
            data['ok'] != false;
        final result = _PaymentsResponse(
          ok: ok,
          statusCode: response.statusCode,
          data: data,
          fallbackMessage: ok ? '' : 'PDV Windows retornou erro.',
        );
        if (ok && path == '/api/mobile/status') {
          windowsBridgeUrl = baseUrl;
          firstOk ??= result;
          if (_map(result.data['session']).isNotEmpty) {
            return result;
          }
          continue;
        }
        if (ok || result.message.isNotEmpty) {
          windowsBridgeUrl = baseUrl;
          return result;
        }
        lastError = result;
      } catch (_) {
        lastError = _PaymentsResponse.error('PDV Windows nao encontrado.');
      }
    }
    if (firstOk != null) return firstOk;
    return lastError ?? _PaymentsResponse.error('PDV Windows nao encontrado.');
  }

  List<String> _windowsBridgeCandidates() {
    final candidates = <String>{};
    final network = _normalizeBridgeBaseUrl(windowsBridgeUrl);
    final local = _normalizeBridgeBaseUrl(windowsBridgeLocalUrl);
    if (network.isNotEmpty) candidates.add(network);
    if (local.isNotEmpty) candidates.add(local);
    final host = Uri.base.host.trim();
    if (host.isNotEmpty && host != '0.0.0.0') {
      candidates.add('http://$host:5050');
    }
    for (var port = 5050; port <= 5055; port++) {
      candidates.add('http://localhost:$port');
      candidates.add('http://127.0.0.1:$port');
    }
    return candidates.map(_normalizeBridgeBaseUrl).where((item) {
      return item.isNotEmpty;
    }).toList();
  }

  bool _bridgeImported(_PaymentsResponse response) {
    if (!response.ok) return false;
    final message = response.message.toLowerCase();
    return message.contains('importou') && !message.contains(' 0 evento');
  }

  bool _bridgeCanImportFull(_PaymentsResponse response) {
    final message = response.message.toLowerCase();
    return message.contains('mesa nao encontrada') ||
        message.contains('mesa sem itens');
  }

  void _markWindowsPrinted(String status) {
    windowsBridgeStatus = status;
    windowsBridgeLastPrintAt = _time(DateTime.now());
  }

  String _receiptText(Order order, String method) {
    final divider = '-' * 32;
    final lines = <String>[
      businessName.toUpperCase(),
      businessDocument,
      businessPhone,
      divider,
      'COMPROVANTE MOBILE',
      '${_kindName(order.kind).toUpperCase()} ${order.number}',
      if (order.customerName.trim().isNotEmpty)
        'CLIENTE: ${order.customerName.trim()}',
      'OPERADOR/GARCOM: ${order.waiter}',
      'DATA: ${DateTime.now().toLocal()}'.split('.').first,
      divider,
    ];
    for (final item in order.items) {
      lines.add('${item.quantity}x ${item.name} ${_money(item.total)}');
    }
    lines.addAll([
      divider,
      'PAGAMENTO: ${_windowsPaymentMethod(method)}',
      'TOTAL: ${_money(order.subtotal)}',
      divider,
      'Impresso pelo Balcao Livre Mobile',
      '',
    ]);
    return lines.join('\n');
  }

  String _windowsKind(OrderKind kind) {
    return switch (kind) {
      OrderKind.delivery || OrderKind.ifood => 'DELIVERY',
      OrderKind.counter => 'BALCAO',
      OrderKind.table => 'MESA',
    };
  }

  String _windowsPaymentMethod(String method) {
    final normalized = method.toUpperCase();
    if (normalized.contains('PIX')) return 'PIX';
    if (normalized.contains('DEBITO') || normalized.contains('DEBIT')) {
      return 'DEBITO';
    }
    if (normalized.contains('CREDITO') || normalized.contains('CREDIT')) {
      return 'CREDITO';
    }
    if (normalized.contains('FIADO')) return 'FIADO';
    if (normalized.contains('IFOOD')) return 'IFOOD';
    return 'DINHEIRO';
  }

  String _money(double value) =>
      'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';

  Future<void> sendSelectedToPoint(String method) async {
    final order = selectedOrder;
    if (order == null || order.items.isEmpty || !order.isOpen) return;
    _openCashForPaymentIfNeeded();
    if (!pointReady) {
      pointStatus = pointHasLicense
          ? 'Conecte o Mercado Pago e selecione uma Point antes de cobrar'
          : 'Conta da loja aguardando sincronizacao';
      pointLastError = pointStatus;
      _enqueue('mercado_pago_charge_blocked');
      await _saveAndNotify();
      return;
    }
    pointChargeMethod = method;
    pointPendingAmount = order.subtotal;
    pointStatus = 'Enviando cobranca para a Point';
    pointLastError = '';
    pointAttemptId = '';
    pointOrderId = '';
    pointLocalReference =
        'BL-${order.number}-${DateTime.now().millisecondsSinceEpoch}';
    _enqueue('mercado_pago_charge_requested');
    await _saveAndNotify();

    final response = await _postPayments('/mercadopago/point/charge', {
      ..._basePaymentsPayload('mercadopago.point.charge'),
      'amount': order.subtotal.toStringAsFixed(2),
      'method': _pointMethod(method),
      'localReference': pointLocalReference,
      'description': '$businessName - ${order.number}',
      'terminalId': pointTerminalId,
      'items': order.items
          .take(50)
          .map(
            (item) => {
              'code': item.code,
              'title': item.name,
              'quantity': item.quantity,
              'unitPrice': item.price.toStringAsFixed(2),
              'description': '${item.quantity}x ${item.name}',
            },
          )
          .toList(),
    });

    if (response.ok) {
      pointAttemptId = response.string('attemptId');
      pointOrderId = response.string('orderId');
      pointLastNsu = response.string('paymentId');
      pointStatus = response.string('message').isEmpty
          ? 'Cobranca enviada para a maquininha'
          : response.string('message');
      pointLastError = '';
      _enqueue('mercado_pago_charge_sent');
    } else {
      pointPendingAmount = 0;
      pointAttemptId = '';
      pointOrderId = '';
      pointStatus = _friendlyPointMessage(response.message);
      pointLastError = pointStatus;
      _enqueue('mercado_pago_charge_failed');
    }
    await _saveAndNotify();
  }

  Future<void> confirmPointPayment() async {
    if (!pointHasPending) return;
    final response = await _postPayments('/mercadopago/point/status', {
      ..._basePaymentsPayload('mercadopago.point.status'),
      'attemptId': pointAttemptId,
      'orderId': pointOrderId,
      'localReference': pointLocalReference,
    });
    if (!response.ok) {
      pointStatus = _friendlyPointMessage(response.message);
      pointLastError = pointStatus;
      _enqueue('mercado_pago_status_failed');
      await _saveAndNotify();
      return;
    }

    pointLastNsu = response.string('paymentId');
    pointStatus = response.boolValue('paid')
        ? 'Pagamento aprovado'
        : 'Pagamento ainda pendente na Point';
    pointLastError = '';
    _enqueue('mercado_pago_status_checked');
    if (response.boolValue('paid')) {
      await closeSelected(pointChargeMethod);
      pointPendingAmount = 0;
      pointAttemptId = '';
      pointOrderId = '';
      pointLocalReference = '';
      pointStatus = 'Point pronta para cobrar';
      await _saveAndNotify();
      return;
    }
    await _saveAndNotify();
  }

  Future<void> cancelPointCharge() async {
    pointPendingAmount = 0;
    pointAttemptId = '';
    pointOrderId = '';
    pointLocalReference = '';
    pointStatus = pointReady
        ? 'Cobranca removida do PDV; cancele na Point se apareceu la'
        : pointStatusLabel;
    _enqueue('mercado_pago_charge_canceled');
    await _saveAndNotify();
  }

  Future<void> reconnectPoint() async {
    await refreshMercadoPagoStatus(loadTerminals: true);
  }

  Future<void> startMercadoPagoConnect() async {
    if (!pointHasLicense) {
      pointStatus = 'Conta da loja aguardando sincronizacao';
      pointLastError = pointStatus;
      await _saveAndNotify();
      return;
    }
    pointStatus = 'Gerando link de conexao Mercado Pago';
    pointLastError = '';
    await _saveAndNotify();
    final response = await _postPayments('/mercadopago/connect/start', {
      ..._basePaymentsPayload('mercadopago.connect.start'),
    });
    if (response.ok) {
      pointConnectUrl = response.string('authUrl');
      pointStatus = pointConnectUrl.isEmpty
          ? 'Nao consegui gerar o link de conexao'
          : 'Abra o link de conexao Mercado Pago';
      pointLastError = pointConnectUrl.isEmpty ? pointStatus : '';
      _enqueue('mercado_pago_connect_link');
    } else {
      pointConnectUrl = '';
      pointStatus = _friendlyPointMessage(response.message);
      pointLastError = pointStatus;
      _enqueue('mercado_pago_connect_failed');
    }
    await _saveAndNotify();
  }

  Future<void> refreshMercadoPagoStatus({bool loadTerminals = false}) async {
    if (!pointHasLicense) {
      _applyDisconnectedPoint('Conta da loja aguardando sincronizacao.');
      await _saveAndNotify();
      return;
    }
    pointStatus = 'Atualizando Mercado Pago';
    pointLastError = '';
    await _saveAndNotify();
    final response = await _postPayments('/mercadopago/status', {
      ..._basePaymentsPayload('mercadopago.status'),
    });
    if (!response.ok) {
      _applyDisconnectedPoint(_friendlyPointMessage(response.message));
      _enqueue('mercado_pago_status_failed');
      await _saveAndNotify();
      return;
    }
    pointConnected = response.boolValue('connected');
    pointConnectionStatus = response.string('status').isEmpty
        ? (pointConnected ? 'CONNECTED' : 'DISCONNECTED')
        : response.string('status');
    pointSellerUserId = response.string('sellerUserId');
    pointTerminalId = response.string('selectedTerminalId');
    pointTerminalLabel = response.string('selectedTerminalLabel');
    pointDeviceName = pointTerminalDisplay == 'Nenhuma Point selecionada'
        ? ''
        : pointTerminalDisplay;
    pointSerial = pointTerminalId;
    pointLastSyncAt = response.string('lastSyncAt');
    pointLastError = _friendlyPointMessage(response.string('lastError'));
    pointStatus = pointConnected
        ? (pointHasTerminal ? 'Point pronta para cobrar' : 'Escolha uma Point')
        : 'Mercado Pago desconectado';
    _enqueue('mercado_pago_status_loaded');
    if (loadTerminals && pointConnected) {
      await loadMercadoPagoTerminals(notify: false);
    }
    await _saveAndNotify();
  }

  Future<void> loadMercadoPagoTerminals({bool notify = true}) async {
    if (!pointHasLicense) {
      _applyDisconnectedPoint('Conta da loja aguardando sincronizacao.');
      if (notify) await _saveAndNotify();
      return;
    }
    final response = await _postPayments('/mercadopago/terminals', {
      ..._basePaymentsPayload('mercadopago.terminals'),
    });
    pointTerminals
      ..clear()
      ..addAll(response.list('terminals').map(MercadoPagoTerminal.fromJson));
    if (response.ok) {
      pointTerminalId = response.string('selectedTerminalId');
      pointTerminalLabel = response.string('selectedTerminalLabel');
      pointDeviceName = pointTerminalDisplay == 'Nenhuma Point selecionada'
          ? ''
          : pointTerminalDisplay;
      pointSerial = pointTerminalId;
      pointStatus = pointHasTerminal
          ? 'Point pronta para cobrar'
          : 'Escolha uma Point Mercado Pago';
      pointLastError = '';
      _enqueue('mercado_pago_terminals_loaded');
    } else {
      pointStatus = _friendlyPointMessage(response.message);
      pointLastError = pointStatus;
      _enqueue('mercado_pago_terminals_failed');
    }
    if (notify) await _saveAndNotify();
  }

  Future<void> selectMercadoPagoTerminal(MercadoPagoTerminal terminal) async {
    if (!pointConnected) return;
    pointStatus = 'Salvando Point selecionada';
    await _saveAndNotify();
    final response = await _postPayments('/mercadopago/terminal/select', {
      ..._basePaymentsPayload('mercadopago.terminal.select'),
      'terminalId': terminal.id,
      'terminalLabel': terminal.display,
    });
    if (response.ok) {
      pointTerminalId = terminal.id;
      pointTerminalLabel = terminal.display;
      pointDeviceName = terminal.display;
      pointSerial = terminal.id;
      pointStatus = 'Point pronta para cobrar';
      pointLastError = '';
      _enqueue('mercado_pago_terminal_selected');
    } else {
      pointStatus = _friendlyPointMessage(response.message);
      pointLastError = pointStatus;
      _enqueue('mercado_pago_terminal_failed');
    }
    await _saveAndNotify();
  }

  void _applyDisconnectedPoint(String message) {
    pointConnected = false;
    pointConnectionStatus = 'DISCONNECTED';
    pointDeviceName = '';
    pointSerial = '';
    pointTerminalId = '';
    pointTerminalLabel = '';
    pointPendingAmount = 0;
    pointAttemptId = '';
    pointOrderId = '';
    pointLocalReference = '';
    pointStatus = _friendlyPointMessage(message);
    pointLastError = pointStatus;
  }

  Future<void> reopenOrder(Order order) async {
    if (order.status == OrderStatus.canceled) return;
    order.status = OrderStatus.open;
    selectedOrderId = order.id;
    _enqueue('sale_reopened');
    await _saveAndNotify();
  }

  Future<void> cancelOrder(Order order) async {
    if (order.kind == OrderKind.ifood &&
        ifoodConnected &&
        await sendIfoodOrderAction(
          order,
          'cancel',
          reason: '501 - Loja sem produto',
        )) {
      return;
    }
    order.status = OrderStatus.canceled;
    if (selectedOrderId == order.id && openOrders.isNotEmpty) {
      selectedOrderId = openOrders.first.id;
    }
    _enqueue('order_canceled');
    await _saveAndNotify();
  }

  Future<void> updateOrderStatus(Order order, OrderStatus status) async {
    if (order.kind == OrderKind.ifood && ifoodConnected) {
      final action = switch (status) {
        OrderStatus.preparing => 'confirm',
        OrderStatus.dispatched || OrderStatus.delivered => 'dispatch',
        OrderStatus.canceled => 'cancel',
        _ => '',
      };
      if (action.isNotEmpty && await sendIfoodOrderAction(order, action)) {
        return;
      }
    }
    order.status = status;
    _enqueue('order_status_changed');
    await _saveAndNotify();
  }

  Future<void> saveProduct({
    required String name,
    required String code,
    required String category,
    required double price,
    required double cost,
    required int stock,
    required int minStock,
    String unit = 'un',
    String imageData = '',
  }) async {
    products.insert(
      0,
      Product(
        id: _id('prd'),
        code: code,
        name: name,
        category: category,
        price: price,
        cost: cost,
        stock: stock,
        minStock: minStock,
        unit: unit,
        imageData: imageData,
      ),
    );
    _enqueue('product_saved');
    await _saveAndNotify();
  }

  Future<void> adjustStock(Product product, int stock) async {
    final delta = stock - product.stock;
    product.stock = stock;
    stockMovements.insert(
      0,
      StockMovement(
        id: _id('stk'),
        productId: product.id,
        type: 'AJUSTE',
        quantity: delta,
        note: 'Saldo conferido',
        createdAt: DateTime.now(),
      ),
    );
    _enqueue('stock_adjusted');
    await _saveAndNotify();
  }

  Future<void> updateProduct({
    required Product product,
    required String name,
    required String code,
    required String category,
    required double price,
    required double cost,
    required int minStock,
    required String unit,
    required String imageData,
  }) async {
    product
      ..name = name.trim()
      ..code = code.trim()
      ..category = category.trim().toUpperCase()
      ..price = price
      ..cost = cost
      ..minStock = minStock
      ..unit = unit.trim().isEmpty ? 'un' : unit.trim().toLowerCase()
      ..imageData = imageData;
    _enqueue('product_updated');
    await _saveAndNotify();
  }

  Future<void> registerStockMovement({
    required Product product,
    required int quantity,
    required bool isExit,
    required String note,
  }) async {
    if (quantity <= 0) return;
    final delta = isExit ? -quantity : quantity;
    product.stock = (product.stock + delta).clamp(0, 2147483647).toInt();
    stockMovements.insert(
      0,
      StockMovement(
        id: _id('stk'),
        productId: product.id,
        type: isExit ? 'SAÍDA' : 'ENTRADA',
        quantity: delta,
        note: note.trim().isEmpty
            ? (isExit ? 'Saída de estoque' : 'Entrada de estoque')
            : note.trim(),
        createdAt: DateTime.now(),
      ),
    );
    _enqueue(isExit ? 'stock_exit' : 'stock_entry');
    await _saveAndNotify();
  }

  Future<void> saveCustomer(String name, String phone, String address) async {
    customers.insert(
      0,
      Customer(id: _id('cus'), name: name, phone: phone, address: address),
    );
    _enqueue('customer_saved');
    await _saveAndNotify();
  }

  Future<bool> saveTeamMember({
    required String number,
    required String name,
    required String role,
    String pin = '',
  }) async {
    final cleanNumber = number.trim();
    final cleanName = name.trim().toUpperCase();
    final cleanRole = role.trim().toUpperCase();
    final cleanPin = pin.trim();
    if (cleanNumber.isEmpty || cleanName.isEmpty || cleanPin.isEmpty) {
      securityMessage = 'Informe numero, nome e senha do operador.';
      notifyListeners();
      return false;
    }
    if (teamMembers.any((member) => member.number == cleanNumber)) return false;

    final member = TeamMember(
      id: _id('team'),
      number: cleanNumber,
      name: cleanName,
      role: cleanRole.isEmpty ? 'GARCOM' : cleanRole,
      pinHash: StaffSecurity.hashPin(cleanPin),
    );
    member.normalizeRolePermissions();
    teamMembers.insert(0, member);
    securityMessage = 'Operador $cleanName cadastrado com senha protegida.';
    _enqueue('team_member_saved');
    await _saveAndNotify();
    return true;
  }

  Future<TeamMember?> authenticateTeamMember({
    required String operator,
    required String pin,
    required StaffPermission permission,
  }) async {
    final cleanOperator = operator.trim().toUpperCase();
    final cleanPin = pin.trim();
    if (cleanOperator.isEmpty || cleanPin.isEmpty) {
      securityMessage = 'Informe operador e senha.';
      notifyListeners();
      return null;
    }
    final member = teamMembers
        .where(
          (candidate) =>
              candidate.active &&
              (candidate.number.trim().toUpperCase() == cleanOperator ||
                  candidate.name.trim().toUpperCase() == cleanOperator),
        )
        .firstOrNull;
    if (member == null || !member.allows(permission)) {
      securityMessage = 'Operador, senha ou permissao invalidos.';
      notifyListeners();
      return null;
    }
    var authenticated = StaffSecurity.verifyPin(member.pinHash, cleanPin);
    if (!authenticated && member.pinHash.isEmpty) {
      authenticated =
          member.legacyPin == cleanPin ||
          (member.legacyPin.isEmpty && member.number == cleanPin);
      if (authenticated) {
        member.pinHash = StaffSecurity.hashPin(cleanPin);
        member.legacyPin = '';
        _enqueue('team_pin_migrated');
        await _saveAndNotify();
      }
    }
    if (!authenticated) {
      securityMessage = 'Operador, senha ou permissao invalidos.';
      notifyListeners();
      return null;
    }
    securityMessage = 'Autorizado por ${member.name}.';
    notifyListeners();
    return member;
  }

  Future<bool> applyDiscount({
    required String amount,
    required String reason,
    required String operator,
    required String pin,
  }) async {
    final order = selectedOrder;
    if (order == null || !order.isOpen || order.items.isEmpty) {
      securityMessage = 'Comanda sem valor para desconto.';
      notifyListeners();
      return false;
    }
    final value = _number(amount).abs();
    if (value <= 0 || value >= order.subtotal) {
      securityMessage = 'Valor de desconto invalido.';
      notifyListeners();
      return false;
    }
    final manager = await authenticateTeamMember(
      operator: operator,
      pin: pin,
      permission: StaffPermission.discount,
    );
    if (manager == null) return false;
    order.items.add(
      OrderItem(
        productId: _id('discount'),
        code: 'DESC',
        name: reason.trim().isEmpty ? 'DESCONTO' : reason.trim().toUpperCase(),
        quantity: 1,
        price: -value,
        cost: 0,
      ),
    );
    securityMessage =
        'Desconto de ${value.toStringAsFixed(2)} autorizado por ${manager.name}.';
    _enqueue('discount_applied');
    await _saveAndNotify();
    return true;
  }

  Future<void> updateBackupSettings({
    required bool cloudBackup,
    required bool centralSync,
  }) async {
    cloudBackupEnabled = cloudBackup;
    centralSyncEnabled = centralSync;
    backupMessage = 'Automacao de backup e sincronizacao atualizada.';
    _enqueue('backup_settings_saved');
    await _saveAndNotify();
  }

  Future<String?> createBackupJson({
    required String operator,
    required String pin,
  }) async {
    final authorized = await authenticateTeamMember(
      operator: operator,
      pin: pin,
      permission: StaffPermission.backup,
    );
    if (authorized == null) {
      backupMessage = securityMessage;
      notifyListeners();
      return null;
    }
    await _save();
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_storageKey);
    if (raw == null || raw.trim().isEmpty) {
      backupMessage = 'Nao foi possivel preparar os dados do backup.';
      notifyListeners();
      return null;
    }
    final payload = jsonDecode(raw);
    if (payload is! Map<String, dynamic>) {
      backupMessage = 'Estado local invalido para backup.';
      notifyListeners();
      return null;
    }
    final payloadJson = jsonEncode(payload);
    final exportedAt = DateTime.now().toUtc().toIso8601String();
    final envelope = {
      'schema': 'balcao-livre-flutter-backup',
      'version': 1,
      'exportedAt': exportedAt,
      'licenseKey': licenseKey.trim().toUpperCase(),
      'businessName': businessName,
      'authorizedBy': authorized.name,
      'checksum': sha256.convert(utf8.encode(payloadJson)).toString(),
      'payload': payload,
    };
    lastBackupAt = exportedAt;
    backupMessage = 'Backup completo gerado por ${authorized.name}.';
    _enqueue('backup_manual_created');
    await _saveAndNotify();
    return const JsonEncoder.withIndent('  ').convert(envelope);
  }

  Future<bool> restoreBackupJson({
    required String backupJson,
    required String operator,
    required String pin,
  }) async {
    final authorized = await authenticateTeamMember(
      operator: operator,
      pin: pin,
      permission: StaffPermission.backup,
    );
    if (authorized == null) {
      backupMessage = securityMessage;
      notifyListeners();
      return false;
    }
    try {
      final decoded = jsonDecode(backupJson);
      if (decoded is! Map<String, dynamic> ||
          decoded['schema'] != 'balcao-livre-flutter-backup' ||
          decoded['version'] != 1 ||
          decoded['payload'] is! Map<String, dynamic>) {
        backupMessage = 'Arquivo de backup invalido ou incompatível.';
        notifyListeners();
        return false;
      }
      final payload = decoded['payload'] as Map<String, dynamic>;
      if (payload['products'] is! List ||
          payload['orders'] is! List ||
          payload['teamMembers'] is! List) {
        backupMessage = 'Backup incompleto: faltam dados operacionais.';
        notifyListeners();
        return false;
      }
      final payloadJson = jsonEncode(payload);
      final checksum = sha256.convert(utf8.encode(payloadJson)).toString();
      if (checksum != '${decoded['checksum'] ?? ''}') {
        backupMessage = 'Backup alterado ou corrompido; restauracao bloqueada.';
        notifyListeners();
        return false;
      }
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_storageKey, payloadJson);
      await hydrate();
      lastBackupAt = DateTime.now().toUtc().toIso8601String();
      backupMessage = 'Backup restaurado por ${authorized.name}.';
      _enqueue('backup_restored');
      await _saveAndNotify();
      return true;
    } on FormatException {
      backupMessage = 'Arquivo de backup nao contem JSON valido.';
      notifyListeners();
      return false;
    }
  }

  String productsCsv() {
    String field(Object? value) {
      final text = '$value'.replaceAll('"', '""');
      return '"$text"';
    }

    final rows = <String>[
      'codigo;nome;grupo;preco_compra;preco_venda;margem_percentual;estoque',
      ...products.map(
        (product) => [
          product.code,
          product.name,
          product.category,
          product.cost.toStringAsFixed(2),
          product.price.toStringAsFixed(2),
          product.margin.toStringAsFixed(2),
          product.stock,
        ].map(field).join(';'),
      ),
    ];
    return rows.join('\r\n');
  }

  Future<void> connectIfood() async {
    if (ifoodBusy) return;
    if (licenseKey.trim().isEmpty) {
      ifoodConnectionStatus = 'ERROR';
      ifoodMessage = 'Entre na conta da loja antes de conectar o iFood.';
      notifyListeners();
      return;
    }
    ifoodBusy = true;
    ifoodMessage = 'Conectando com o iFood...';
    notifyListeners();
    try {
      final response = await _ifoodClient.startConnection(_ifoodContext());
      ifoodConnectionId = _text(response['connectionId']);
      ifoodConnectionStatus = _firstText([
        response['status'],
        ifoodConnectionId.isEmpty ? 'DISCONNECTED' : 'CONNECTED',
      ]).toUpperCase();
      ifoodMerchantId = _text(response['merchantId']);
      ifoodMerchantName = _text(response['merchantName']);
      ifoodMessage = _firstText([
        response['message'],
        ifoodConnected ? 'iFood conectado.' : 'Conexao iFood iniciada.',
      ]);
      ifoodVerificationUrl = _firstText([
        response['verificationUrlComplete'],
        response['verificationUrl'],
      ]);
      ifoodUserCode = _text(response['userCode']);
      _enqueue('ifood_connected');
      await _saveAndNotify();
    } on IFoodCloudException catch (error) {
      ifoodConnectionStatus = 'ERROR';
      ifoodMessage = error.message;
      notifyListeners();
    } finally {
      ifoodBusy = false;
      notifyListeners();
    }
  }

  Future<void> finishIfoodConnection(String authorizationCode) async {
    if (ifoodBusy || ifoodConnectionId.isEmpty) return;
    ifoodBusy = true;
    ifoodMessage = 'Finalizando autorizacao iFood...';
    notifyListeners();
    try {
      final response = await _ifoodClient.finishConnection(
        _ifoodContext(),
        connectionId: ifoodConnectionId,
        authorizationCode: authorizationCode.trim(),
      );
      ifoodConnectionStatus = 'CONNECTED';
      ifoodMerchantId = _firstText([response['merchantId'], ifoodMerchantId]);
      ifoodMerchantName = _firstText([
        response['merchantName'],
        ifoodMerchantName,
      ]);
      ifoodMessage = _firstText([response['message'], 'iFood conectado.']);
      ifoodVerificationUrl = '';
      ifoodUserCode = '';
      _enqueue('ifood_connected');
      await _saveAndNotify();
    } on IFoodCloudException catch (error) {
      ifoodConnectionStatus = 'ERROR';
      ifoodMessage = error.message;
      notifyListeners();
    } finally {
      ifoodBusy = false;
      notifyListeners();
    }
  }

  Future<int> syncIfoodOrders() async {
    if (ifoodBusy || ifoodConnectionId.isEmpty) return 0;
    ifoodBusy = true;
    ifoodMessage = 'Buscando pedidos iFood...';
    notifyListeners();
    try {
      final response = await _ifoodClient.syncOrders(
        _ifoodContext(),
        connectionId: ifoodConnectionId,
      );
      var imported = 0;
      for (final row in _rows(response['orders'])) {
        if (_upsertIfoodOrder(row)) imported++;
      }
      ifoodConnectionStatus = 'CONNECTED';
      ifoodLastSyncAt = _firstText([
        response['syncedAt'],
        DateTime.now().toIso8601String(),
      ]);
      ifoodMessage = _firstText([
        response['message'],
        '$imported pedido(s) iFood sincronizado(s).',
      ]);
      _enqueue('ifood_orders_synced');
      await _saveAndNotify();
      return imported;
    } on IFoodCloudException catch (error) {
      ifoodConnectionStatus = 'ERROR';
      ifoodMessage = error.message;
      notifyListeners();
      return 0;
    } finally {
      ifoodBusy = false;
      notifyListeners();
    }
  }

  Future<bool> sendIfoodOrderAction(
    Order order,
    String action, {
    String reason = '',
  }) async {
    if (ifoodBusy || !ifoodConnected || order.kind != OrderKind.ifood) {
      return false;
    }
    final externalId = order.id.replaceFirst(RegExp(r'^ifood-'), '');
    ifoodBusy = true;
    ifoodMessage = 'Enviando acao ao iFood...';
    notifyListeners();
    try {
      final response = await _ifoodClient.sendOrderAction(
        _ifoodContext(),
        connectionId: ifoodConnectionId,
        orderId: externalId,
        action: action,
        reason: reason,
      );
      order.status = _ifoodOrderStatus(response['status'] ?? action);
      ifoodMessage = _firstText([
        response['message'],
        'Pedido iFood atualizado.',
      ]);
      _enqueue('ifood_order_action');
      await _saveAndNotify();
      return true;
    } on IFoodCloudException catch (error) {
      ifoodMessage = error.message;
      notifyListeners();
      return false;
    } finally {
      ifoodBusy = false;
      notifyListeners();
    }
  }

  Map<String, dynamic> _ifoodContext() {
    return {
      'licenseKey': licenseKey.trim().toUpperCase(),
      'machineHash': _machineHash,
      'machineCode': _machineCode,
      'businessName': businessName,
      'legalName': businessLegalName,
      'cnpj': businessDocument,
      'phone': businessPhone,
      'address': businessAddress,
      'city': businessCity,
      'state': businessUf,
      'appVersion': _appVersion,
    };
  }

  bool _upsertIfoodOrder(Map<String, dynamic> row) {
    final externalId = _firstText([row['orderId'], row['id']]);
    if (externalId.isEmpty) return false;
    final id = 'ifood-$externalId';
    final existing = orders.where((order) => order.id == id).firstOrNull;
    final rawItems = _rows(row['items']);
    final items = rawItems.map((item) {
      final code = _firstText([
        item['code'],
        item['externalCode'],
        item['productId'],
      ]);
      final product = products
          .where(
            (candidate) =>
                candidate.id == _text(item['productId']) ||
                candidate.code == code,
          )
          .firstOrNull;
      return OrderItem(
        productId: product?.id ?? _firstText([item['productId'], code]),
        code: code,
        name: _firstText([item['name'], 'Item iFood']),
        quantity: _number(item['quantity']).round().clamp(1, 999),
        price: _number(item['unitPrice'] ?? item['price']),
        cost: product?.cost ?? 0,
      );
    }).toList();
    final status = _ifoodOrderStatus(row['status']);
    if (existing != null) {
      existing
        ..number = _firstText([row['displayId'], existing.number])
        ..status = status
        ..customerName = _firstText([
          row['customerName'],
          existing.customerName,
        ])
        ..address = [
          _text(row['address']),
          _text(row['district']),
        ].where((part) => part.isNotEmpty).join(' - ')
        ..paymentMethod = _firstText([
          row['paymentMethod'],
          row['paymentSummary'],
          existing.paymentMethod,
        ])
        ..ifoodRepasse = _number(row['total']) * 0.88
        ..servicePercent = 0;
      if (items.isNotEmpty) {
        existing.items
          ..clear()
          ..addAll(items);
      }
      return false;
    }

    final order = Order(
      id: id,
      number: _firstText([
        row['displayId'],
        'I${(orders.length + 1).toString().padLeft(5, '0')}',
      ]),
      kind: OrderKind.ifood,
      status: status,
      createdAt: DateTime.tryParse(_text(row['createdAt'])) ?? DateTime.now(),
      customerName: _firstText([row['customerName'], 'CLIENTE IFOOD']),
      address: [
        _text(row['address']),
        _text(row['district']),
      ].where((part) => part.isNotEmpty).join(' - '),
      paymentMethod: _firstText([row['paymentMethod'], row['paymentSummary']]),
      ifoodRepasse: _number(row['total']) * 0.88,
      servicePercent: 0,
      items: items,
    );
    orders.insert(0, order);
    selectedOrderId = order.id;
    return true;
  }

  OrderStatus _ifoodOrderStatus(Object? value) {
    final status = _text(value).toUpperCase();
    if (status.contains('CANCEL')) return OrderStatus.canceled;
    if (status.contains('DELIVER') || status.contains('CONCLUDE')) {
      return OrderStatus.delivered;
    }
    if (status.contains('DISPATCH') ||
        status.contains('DESPACH') ||
        status.contains('READY') ||
        status.contains('PRONTO') ||
        status.contains('PICKUP')) {
      return OrderStatus.dispatched;
    }
    if (status.contains('CONFIRM') ||
        status.contains('PREPAR') ||
        status == 'CFM') {
      return OrderStatus.preparing;
    }
    return OrderStatus.open;
  }

  Future<void> simulateIfoodOrder() async {
    final order = Order(
      id: _id('ifood'),
      number: 'I${(orders.length + 1).toString().padLeft(5, '0')}',
      kind: OrderKind.ifood,
      status: OrderStatus.preparing,
      createdAt: DateTime.now(),
      customerName: 'Cliente iFood',
      address: 'Entrega propria',
    );
    final product = products.firstWhere((product) => product.active);
    order.items.add(
      OrderItem(
        productId: product.id,
        code: product.code,
        name: product.name,
        quantity: 1,
        price: product.price,
        cost: product.cost,
      ),
    );
    product.stock = product.stock > 0 ? product.stock - 1 : 0;
    order.ifoodRepasse = order.subtotal * 0.88;
    orders.insert(0, order);
    selectedOrderId = order.id;
    _enqueue('ifood_order_imported');
    await _saveAndNotify();
  }

  Future<void> flushSync() async {
    syncQueue.clear();
    lastSync = _time(DateTime.now());
    await _saveAndNotify();
  }

  String get whatsappQrPayload => whatsappOnboardingUrl.trim();

  Future<void> connectWhatsApp(String number) async {
    if (whatsappBusy) return;
    final phone = number.trim();
    if (licenseKey.trim().isEmpty) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = 'Entre na conta da loja antes de conectar o WhatsApp.';
      notifyListeners();
      return;
    }
    if (phone.isEmpty) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = 'Informe o numero do WhatsApp da loja com DDD.';
      notifyListeners();
      return;
    }
    whatsappBusy = true;
    whatsappMessage = 'Conectando o numero da loja...';
    notifyListeners();
    try {
      final response = await _whatsappClient.activate(
        _whatsappContext('whatsapp.activate'),
        storePhone: phone,
      );
      _applyWhatsAppResponse(response, fallbackPhone: phone);
      _enqueue(
        whatsappConnected
            ? 'whatsapp_connected'
            : 'whatsapp_connection_pending',
      );
      await _saveAndNotify();
    } on WhatsAppCloudException catch (error) {
      whatsappConnected = false;
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = error.message;
      notifyListeners();
    } finally {
      whatsappBusy = false;
      notifyListeners();
    }
  }

  Future<void> refreshWhatsAppQr() async {
    if (whatsappBusy) return;
    if (licenseKey.trim().isEmpty) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = 'Entre na conta da loja antes de consultar o WhatsApp.';
      notifyListeners();
      return;
    }
    whatsappBusy = true;
    whatsappMessage = 'Consultando conexao do WhatsApp...';
    notifyListeners();
    try {
      final response = await _whatsappClient.status(
        _whatsappContext('whatsapp.status'),
        storePhone: whatsappNumber,
      );
      _applyWhatsAppResponse(response, fallbackPhone: whatsappNumber);
      whatsappLastSyncAt = DateTime.now().toIso8601String();
      _enqueue('whatsapp_status_synced');
      await _saveAndNotify();
    } on WhatsAppCloudException catch (error) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = error.message;
      notifyListeners();
    } finally {
      whatsappBusy = false;
      notifyListeners();
    }
  }

  Future<bool> sendWhatsAppMessage({
    required String customerPhone,
    required String message,
    String messageId = '',
    String customerName = '',
    String boardKind = '',
    String boardNumber = '',
    double total = 0,
  }) async {
    if (whatsappBusy || licenseKey.trim().isEmpty) return false;
    whatsappBusy = true;
    whatsappMessage = 'Enviando WhatsApp...';
    notifyListeners();
    try {
      final response = await _whatsappClient.send(
        _whatsappContext('whatsapp.send'),
        storePhone: whatsappNumber,
        customerPhone: customerPhone,
        message: message,
        messageId: messageId,
        customerName: customerName,
        boardKind: boardKind,
        boardNumber: boardNumber,
        total: total,
      );
      _applyWhatsAppResponse(response, fallbackPhone: whatsappNumber);
      _enqueue(
        whatsappConnected ? 'whatsapp_message_sent' : 'whatsapp_send_pending',
      );
      await _saveAndNotify();
      return whatsappConnected;
    } on WhatsAppCloudException catch (error) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = error.message;
      notifyListeners();
      return false;
    } finally {
      whatsappBusy = false;
      notifyListeners();
    }
  }

  Future<void> disconnectWhatsApp() async {
    if (whatsappBusy) return;
    if (licenseKey.trim().isEmpty) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage =
          'Entre na conta da loja antes de desconectar o WhatsApp.';
      notifyListeners();
      return;
    }
    whatsappBusy = true;
    whatsappMessage = 'Desconectando WhatsApp...';
    notifyListeners();
    try {
      final response = await _whatsappClient.disconnect(
        _whatsappContext('whatsapp.disconnect'),
      );
      whatsappConnected = false;
      whatsappConnectionStatus = 'DISCONNECTED';
      whatsappMessage = _firstText([
        response['message'],
        'WhatsApp desconectado.',
      ]);
      whatsappNumber = '';
      whatsappSessionId = '';
      whatsappOnboardingUrl = '';
      whatsappLastSyncAt = DateTime.now().toIso8601String();
      _enqueue('whatsapp_disconnected');
      await _saveAndNotify();
    } on WhatsAppCloudException catch (error) {
      whatsappConnectionStatus = 'ERROR';
      whatsappMessage = error.message;
      notifyListeners();
    } finally {
      whatsappBusy = false;
      notifyListeners();
    }
  }

  Map<String, dynamic> _whatsappContext(String eventName) {
    return {
      ..._baseLicensePayload(eventName),
      'localWhen': DateTime.now().toIso8601String(),
    };
  }

  void _applyWhatsAppResponse(
    Map<String, dynamic> response, {
    required String fallbackPhone,
  }) {
    final pending = response['pending'] == true;
    final connected = response['ok'] == true && !pending;
    whatsappConnected = connected;
    whatsappConnectionStatus = connected
        ? 'CONNECTED'
        : pending
        ? 'PENDING'
        : 'ERROR';
    whatsappNumber = _firstText([response['storePhone'], fallbackPhone]);
    whatsappMessage = _firstText([
      response['message'],
      connected
          ? 'WhatsApp conectado.'
          : pending
          ? 'Conexao do WhatsApp aguardando confirmacao.'
          : 'WhatsApp nao conectado.',
    ]);
    whatsappOnboardingUrl = connected ? '' : _text(response['onboardingUrl']);
    whatsappSessionId = _firstText([
      response['phoneNumberId'],
      response['wabaId'],
      whatsappSessionId,
    ]);
    whatsappLastSyncAt = DateTime.now().toIso8601String();
  }

  Future<void> updateBusinessName(String value) async {
    businessName = value.trim().isEmpty ? businessName : value.trim();
    await _saveAndNotify();
  }

  Future<void> updateBusinessProfile({
    required String name,
    required String legalName,
    required String responsible,
    required String document,
    required String phone,
    required String email,
    required String city,
    required String uf,
    required String address,
  }) async {
    businessName = name.trim().isEmpty ? businessName : name.trim();
    businessLegalName = legalName.trim().isEmpty
        ? businessLegalName
        : legalName.trim();
    businessResponsible = responsible.trim().isEmpty
        ? businessResponsible
        : responsible.trim();
    businessDocument = document.trim().isEmpty
        ? businessDocument
        : document.trim();
    businessPhone = phone.trim().isEmpty ? businessPhone : phone.trim();
    authEmail = email.trim().isEmpty ? authEmail : email.trim().toLowerCase();
    businessCity = city.trim().isEmpty ? businessCity : city.trim();
    businessUf = uf.trim().isEmpty ? businessUf : uf.trim().toUpperCase();
    businessAddress = address.trim().isEmpty ? businessAddress : address.trim();
    _enqueue('business_profile_updated');
    await _saveAndNotify();
  }

  Future<void> updateBridgeSettings({
    required String networkUrl,
    required String localUrl,
  }) async {
    windowsBridgeUrl = _normalizeBridgeBaseUrl(networkUrl);
    windowsBridgeLocalUrl = _normalizeBridgeBaseUrl(localUrl);
    if (windowsBridgeLocalUrl.isEmpty) {
      windowsBridgeLocalUrl = 'http://localhost:5050';
    }
    windowsBridgeStatus = windowsBridgeUrl.isEmpty
        ? 'Bridge Windows local configurado'
        : 'Bridge Windows pronto para imprimir';
    _enqueue('bridge_settings_updated');
    await _saveAndNotify();
  }

  Future<void> resetDemo() async {
    products.clear();
    orders.clear();
    customers.clear();
    movements.clear();
    syncQueue.clear();
    _seed();
    await _saveAndNotify();
  }

  void _seed() {
    products
      ..clear()
      ..addAll([
        Product(
          id: '000001',
          code: '000001',
          name: 'X-BURGER ARTESANAL',
          category: 'LANCHES',
          price: 28,
          cost: 13.5,
          stock: 38,
          minStock: 8,
        ),
        Product(
          id: '000002',
          code: '000002',
          name: 'X-SALADA COMPLETO',
          category: 'LANCHES',
          price: 32,
          cost: 15,
          stock: 34,
          minStock: 8,
        ),
        Product(
          id: '000003',
          code: '000003',
          name: 'BATATA FRITA MEDIA',
          category: 'LANCHES',
          price: 18,
          cost: 7.2,
          stock: 42,
          minStock: 10,
        ),
        Product(
          id: '000004',
          code: '000004',
          name: 'COCA-COLA LATA 350ML',
          category: 'BEBIDAS',
          price: 7,
          cost: 3.6,
          stock: 72,
          minStock: 18,
        ),
        Product(
          id: '000005',
          code: '000005',
          name: 'AGUA MINERAL 500ML',
          category: 'BEBIDAS',
          price: 4,
          cost: 1.7,
          stock: 96,
          minStock: 24,
        ),
        Product(
          id: '000006',
          code: '000006',
          name: 'SUCO NATURAL 400ML',
          category: 'BEBIDAS',
          price: 10,
          cost: 4.2,
          stock: 28,
          minStock: 8,
        ),
        Product(
          id: '000007',
          code: '000007',
          name: 'PRATO EXECUTIVO',
          category: 'PRATOS',
          price: 35,
          cost: 17,
          stock: 25,
          minStock: 6,
        ),
        Product(
          id: '000008',
          code: '000008',
          name: 'PARMEGIANA INDIVIDUAL',
          category: 'PRATOS',
          price: 46,
          cost: 22,
          stock: 18,
          minStock: 4,
        ),
        Product(
          id: '000009',
          code: '000009',
          name: 'PIZZA CALABRESA BROTINHO',
          category: 'PIZZAS',
          price: 36,
          cost: 16,
          stock: 20,
          minStock: 5,
        ),
        Product(
          id: '000010',
          code: '000010',
          name: 'PIZZA MUSSARELA BROTINHO',
          category: 'PIZZAS',
          price: 34,
          cost: 15,
          stock: 22,
          minStock: 5,
        ),
        Product(
          id: '000011',
          code: '000011',
          name: 'TAXA DE ENTREGA BAIRRO',
          category: 'DELIVERY',
          price: 8,
          cost: 0,
          stock: 999,
          minStock: 0,
        ),
        Product(
          id: '000012',
          code: '000012',
          name: 'BROWNIE COM SORVETE',
          category: 'SOBREMESAS',
          price: 16,
          cost: 6.8,
          stock: 16,
          minStock: 4,
        ),
      ]);
    customers
      ..clear()
      ..addAll([
        Customer(
          id: 'c1',
          name: 'Mariana Costa',
          phone: '(27) 99812-4455',
          address: 'Rua das Palmeiras, 120',
          points: 18,
          cashback: 6.5,
        ),
        Customer(
          id: 'c2',
          name: 'Ana Lima',
          phone: '(27) 99922-7788',
          address: 'Centro',
          lastPurchaseAt: DateTime.now().subtract(const Duration(days: 52)),
        ),
        Customer(
          id: 'c3',
          name: 'Rafael Lima',
          phone: '(27) 99777-2222',
          address: 'Bairro Novo',
          points: 9,
        ),
      ]);
    teamMembers
      ..clear()
      ..addAll([
        TeamMember(
          id: 'team-1',
          number: '1',
          name: operatorName.toUpperCase(),
          role: 'CAIXA',
          pinHash:
              r'PBKDF2$120000$AAECAwQFBgcICQoLDA0ODw==$MM3KlKQuwzYe1OTSQPFAJ0QREJZOQge/8zWqoROvWTA=',
          canCash: true,
          canCancel: true,
          canDiscount: true,
          canDelivery: true,
        ),
        TeamMember(
          id: 'team-2',
          number: '2',
          name: 'LUCAS CESAR',
          role: 'GERENTE',
          pinHash:
              r'PBKDF2$120000$EBESExQVFhcYGRobHB0eHw==$zV3rw3sx0cCLGcKV2G7yh8n34lgL/N96TwUnSfDAjyE=',
          canTransfer: true,
          canCancel: true,
          canDiscount: true,
          canManageProducts: true,
          canReports: true,
          canCash: true,
          canDelivery: true,
          canInventory: true,
          canKitchen: true,
          canIFood: true,
          canSettings: true,
          canBackup: true,
          canDeliveryZones: true,
          canCentralSync: true,
        ),
        TeamMember(
          id: 'team-3',
          number: '3',
          name: 'ENTREGADOR APP',
          role: 'ENTREGADOR',
          pinHash:
              r'PBKDF2$120000$ICEiIyQlJicoKSorLC0uLw==$G6nVdLtJVx+nEbSzt4AVtcedir1/SRBbKy6FtPy2IJ0=',
          canDelivery: true,
        ),
      ]);
    orders
      ..clear()
      ..addAll([
        Order(
          id: 'mesa-000001',
          number: 'M00001',
          kind: OrderKind.table,
          status: OrderStatus.open,
          createdAt: DateTime.now(),
          customerName: 'Mesa 01',
          waiter: '2',
        ),
        Order(
          id: 'mesa-000002',
          number: 'M00002',
          kind: OrderKind.table,
          status: OrderStatus.open,
          createdAt: DateTime.now(),
          customerName: 'Familia Silva',
          waiter: '1',
        ),
        Order(
          id: 'delivery-00001',
          number: 'D00001',
          kind: OrderKind.delivery,
          status: OrderStatus.preparing,
          createdAt: DateTime.now(),
          customerName: 'Cliente cardapio',
          address: 'Rua do Centro, 45',
        ),
      ]);
    orders[0].items.add(
      OrderItem(
        productId: '000001',
        code: '000001',
        name: 'X-BURGER ARTESANAL',
        quantity: 2,
        price: 28,
        cost: 13.5,
      ),
    );
    orders[1].items.add(
      OrderItem(
        productId: '000004',
        code: '000004',
        name: 'COCA-COLA LATA 350ML',
        quantity: 3,
        price: 7,
        cost: 3.6,
      ),
    );
    orders[2].items.add(
      OrderItem(
        productId: '000009',
        code: '000009',
        name: 'PIZZA CALABRESA BROTINHO',
        quantity: 1,
        price: 36,
        cost: 16,
      ),
    );
    selectedOrderId = orders.first.id;
    movements.add(
      CashMovement(
        id: 'm1',
        type: 'ABERTURA',
        amount: 0,
        note: 'Caixa aberto',
        createdAt: DateTime.now(),
      ),
    );
  }

  void _enqueue(String type) {
    syncQueue.insert(0, '${DateTime.now().toIso8601String()}|$type');
  }

  Future<void> _saveAndNotify({bool pushSync = true}) async {
    await _save();
    notifyListeners();
    if (pushSync) _scheduleMobileSync();
  }

  void _scheduleMobileSync() {
    if (!loggedIn || licenseKey.trim().isEmpty || syncQueue.isEmpty) return;
    _syncDebounce?.cancel();
    _syncDebounce = Timer(const Duration(seconds: 2), () {
      unawaited(_pushMobileSync());
    });
  }

  Future<void> _pushMobileSync() async {
    if (_syncRunning ||
        !loggedIn ||
        licenseKey.trim().isEmpty ||
        syncQueue.isEmpty) {
      return;
    }
    _syncRunning = true;
    final events = syncQueue.take(100).map((line) {
      final parts = line.split('|');
      final when = parts.isNotEmpty
          ? parts.first
          : DateTime.now().toIso8601String();
      final type = parts.length > 1
          ? parts.sublist(1).join('|')
          : 'mobile.event';
      return {
        'id': line.hashCode.toUnsigned(32).toRadixString(16),
        'type': type,
        'status': 'pending',
        'createdAt': when,
        'payload': {
          'source': 'flutter-web',
          'selectedOrderId': selectedOrderId,
          'openOrders': openOrders.length,
          'openTotal': openTotal,
        },
      };
    }).toList();
    final response = await _postLicense('/mobile/sync', {
      ..._baseLicensePayload('mobile.sync'),
      'events': events,
      'snapshot': _mobileSnapshot(),
    });
    _syncRunning = false;
    if (response.ok) {
      syncQueue.clear();
      lastSync = _time(DateTime.now());
      syncStatus = 'Tempo real ligado';
      await _save();
      notifyListeners();
    } else {
      syncStatus = response.message;
      notifyListeners();
    }
  }

  Map<String, dynamic> _baseLicensePayload(String eventName) {
    return {
      'eventName': eventName,
      'licenseKey': licenseKey.trim().toUpperCase(),
      'machineHash': _machineHash,
      'machineCode': _machineCode,
      'clientKind': 'flutter-web',
      'appVersion': _appVersion,
      'profile': {
        'email': authEmail.trim().toLowerCase(),
        'businessName': businessName,
        'legalName': businessLegalName,
        'ownerName': businessResponsible,
        'businessDocument': businessDocument,
        'cnpj': businessDocument,
        'businessPhone': businessPhone,
        'phone': businessPhone,
        'city': businessCity,
        'uf': businessUf,
        'address': businessAddress,
      },
      'settings': {
        'publicMenuId': publicMenuId,
        'publicMenuSlug': publicMenuSlug,
        'supabaseRealtime': true,
        'windowsBridgeUrl': windowsBridgeUrl,
        'windowsBridgeLocalUrl': windowsBridgeLocalUrl,
      },
      'metrics': {
        'openOrders': openOrders.length,
        'openTotal': openTotal,
        'soldToday': soldToday,
        'products': products.length,
      },
    };
  }

  Map<String, dynamic> _mobileSnapshot() {
    return {
      'settings': {
        'id': 'flutter-web',
        'storeId': licenseKey,
        'terminalId': _machineCode,
        'adminApiUrl': _licenseApiUrl,
        'windowsBridgeUrl': windowsBridgeUrl,
        'windowsBridgeLocalUrl': windowsBridgeLocalUrl,
        'printMode': 'WINDOWS_BRIDGE',
        'autoSync': 1,
        'cashOpen': cashOpen ? 1 : 0,
        'lastSyncAt': DateTime.now().toIso8601String(),
        'ifoodConnectionId': ifoodConnectionId,
        'ifoodConnectionStatus': ifoodConnectionStatus,
        'ifoodMerchantId': ifoodMerchantId,
        'ifoodMerchantName': ifoodMerchantName,
        'ifoodLastSyncAt': ifoodLastSyncAt,
      },
      'profile': {
        'email': authEmail,
        'businessName': businessName,
        'legalName': businessLegalName,
        'ownerName': businessResponsible,
        'businessDocument': businessDocument,
        'businessPhone': businessPhone,
        'city': businessCity,
        'uf': businessUf,
        'address': businessAddress,
      },
      'products': products.map((item) => item.toJson()).toList(),
      'orders': orders.map((item) => item.toJson()).toList(),
      'customers': customers.map((item) => item.toJson()).toList(),
      'teamMembers': teamMembers.map((item) => item.toJson()).toList(),
      'users': teamMembers.map((item) => item.toJson()).toList(),
      'cashMovements': movements.map((item) => item.toJson()).toList(),
      'stockMovements': stockMovements.map((item) => item.toJson()).toList(),
    };
  }

  Map<String, dynamic> _basePaymentsPayload(String eventName) {
    return {
      'eventName': eventName,
      'licenseKey': licenseKey.trim().toUpperCase(),
      'machineHash': _machineHash,
      'machineCode': _machineCode,
      'clientKind': 'flutter-web',
      'appVersion': _appVersion,
      'profile': {
        'email': authEmail.trim().toLowerCase(),
        'businessName': businessName,
        'legalName': businessLegalName,
        'ownerName': businessResponsible,
        'businessDocument': businessDocument,
        'cnpj': businessDocument,
        'businessPhone': businessPhone,
        'phone': businessPhone,
        'city': businessCity,
        'uf': businessUf,
        'address': businessAddress,
      },
      'metrics': {
        'openOrders': openOrders.length,
        'openTotal': openTotal,
        'soldToday': soldToday,
      },
    };
  }

  Future<_PaymentsResponse> _postPayments(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final endpoint = Uri.tryParse('$_paymentsApiUrl$path');
    if (endpoint == null) {
      return _PaymentsResponse.error('URL de pagamentos invalida.');
    }

    try {
      final response = await http
          .post(
            endpoint,
            headers: const {'content-type': 'application/json'},
            body: jsonEncode(payload),
          )
          .timeout(const Duration(seconds: 18));
      final text = utf8.decode(response.bodyBytes);
      Map<String, dynamic> data = {};
      if (text.trim().isNotEmpty) {
        final decoded = jsonDecode(text);
        if (decoded is Map<String, dynamic>) data = decoded;
      }
      final ok =
          response.statusCode >= 200 &&
          response.statusCode < 300 &&
          data['ok'] != false;
      return _PaymentsResponse(
        ok: ok,
        statusCode: response.statusCode,
        data: data,
        fallbackMessage: ok
            ? ''
            : 'Pagamentos online retornou ${response.statusCode}.',
      );
    } catch (error) {
      return _PaymentsResponse.error(
        'Nao consegui falar com os pagamentos agora.',
      );
    }
  }

  Future<_PaymentsResponse> _postLicense(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final endpoint = Uri.tryParse('$_licenseApiUrl$path');
    if (endpoint == null) {
      return _PaymentsResponse.error('Endereco de conexao invalido.');
    }
    final token = _supabase.auth.currentSession?.accessToken ?? '';
    try {
      final response = await http
          .post(
            endpoint,
            headers: {
              'content-type': 'application/json',
              if (token.isNotEmpty) 'authorization': 'Bearer $token',
            },
            body: jsonEncode(payload),
          )
          .timeout(const Duration(seconds: 18));
      final text = utf8.decode(response.bodyBytes);
      Map<String, dynamic> data = {};
      if (text.trim().isNotEmpty) {
        final decoded = jsonDecode(text);
        if (decoded is Map<String, dynamic>) data = decoded;
      }
      final ok =
          response.statusCode >= 200 &&
          response.statusCode < 300 &&
          data['ok'] != false;
      return _PaymentsResponse(
        ok: ok,
        statusCode: response.statusCode,
        data: data,
        fallbackMessage: ok
            ? ''
            : 'Conta da loja retornou erro ${response.statusCode}.',
      );
    } catch (error) {
      return _PaymentsResponse.error('Nao consegui sincronizar a conta agora.');
    }
  }

  String _pointMethod(String method) {
    final normalized = method.toUpperCase();
    if (normalized.contains('CREDITO')) return 'CREDITO';
    if (normalized.contains('DEBITO')) return 'DEBITO';
    if (normalized.contains('PIX')) return 'PIX';
    return 'POINT';
  }

  String get _machineCode {
    final seed = licenseKey.trim().isEmpty
        ? businessDocument
        : '${licenseKey.trim()}|$businessDocument';
    final hash = seed.codeUnits.fold<int>(
      17,
      (value, unit) => ((value * 31) + unit) & 0x7fffffff,
    );
    return 'FLUTTER-${hash.toRadixString(16).toUpperCase()}';
  }

  String get _machineHash {
    final seed = 'balcao-flutter|$_machineCode|${businessName.trim()}';
    final hash = seed.codeUnits.fold<int>(
      5381,
      (value, unit) => (((value << 5) + value) ^ unit) & 0x7fffffff,
    );
    return 'flutter-${hash.toRadixString(16)}';
  }

  String _friendlyAuthError(Object error) {
    final text = error is AuthException
        ? error.message
        : error.toString().replaceFirst('Exception: ', '');
    final lower = text.toLowerCase();
    if (lower.contains('invalid login credentials')) {
      return 'Email ou senha incorretos.';
    }
    if (lower.contains('email not confirmed')) {
      return 'Confirme o e-mail da conta Supabase antes de entrar.';
    }
    if (lower.contains('supabase has not been initialized') ||
        lower.contains('not initialized')) {
      return 'Supabase ainda esta inicializando. Tente novamente em alguns segundos.';
    }
    if (lower.contains('failed host lookup') ||
        lower.contains('network') ||
        lower.contains('connection')) {
      return 'Nao consegui conectar ao Supabase agora.';
    }
    if (lower.contains('licenca expirada')) {
      return 'Conta expirada. Renove para continuar.';
    }
    if (lower.contains('licenca bloqueada') || lower.contains('bloqueada')) {
      return 'Conta bloqueada. Fale com o suporte.';
    }
    if (lower.contains('licenca') ||
        lower.contains('chave') ||
        lower.contains('painel') ||
        lower.contains('supabase')) {
      return 'Conta da loja nao encontrada para esse login.';
    }
    return text;
  }

  String _friendlyPointMessage(Object? value) {
    final text = _text(value);
    if (text.isEmpty) return 'Mercado Pago desconectado';
    final lower = text.toLowerCase();
    if (lower.contains('supabase') ||
        lower.contains('chave') ||
        lower.contains('licenca') ||
        lower.contains('painel admin') ||
        lower.contains('painel')) {
      return 'Conta da loja ainda nao esta conectada para Mercado Pago.';
    }
    if (lower.contains('401') || lower.contains('403')) {
      return 'Conta da loja sem acesso ao Mercado Pago.';
    }
    if (lower.contains('timeout') ||
        lower.contains('network') ||
        lower.contains('falha ao falar')) {
      return 'Nao consegui falar com os pagamentos agora.';
    }
    return text;
  }

  String _firstText(List<Object?> values) {
    for (final value in values) {
      final text = _text(value);
      if (text.isNotEmpty) return text;
    }
    return '';
  }

  String _text(Object? value) => (value ?? '').toString().trim();

  String _normalizeBridgeBaseUrl(Object? value) {
    var clean = _text(value);
    if (clean.isEmpty) return '';
    while (clean.endsWith('/')) {
      clean = clean.substring(0, clean.length - 1);
    }
    if (clean.toLowerCase().endsWith('/garcom')) {
      clean = clean.substring(0, clean.length - '/garcom'.length);
    }
    if (!clean.toLowerCase().startsWith('http://') &&
        !clean.toLowerCase().startsWith('https://')) {
      clean = 'http://$clean';
    }
    return clean;
  }

  String _kindName(OrderKind kind) {
    return switch (kind) {
      OrderKind.table => 'Mesa',
      OrderKind.counter => 'Balcao',
      OrderKind.delivery => 'Delivery',
      OrderKind.ifood => 'iFood',
    };
  }

  double _number(Object? value) {
    if (value is num) return value.toDouble();
    return double.tryParse(_text(value).replaceAll(',', '.')) ?? 0;
  }

  bool _bool(Object? value) {
    if (value is bool) return value;
    if (value is num) return value != 0;
    final text = _text(value).toLowerCase();
    return ['1', 'true', 'sim', 'yes', 'on', 'aberto'].contains(text);
  }

  List<Map<String, dynamic>> _rows(Object? value) {
    if (value is List) return value.map(_map).toList();
    return [];
  }

  Map<String, dynamic> _map(Object? value) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) {
      return value.map((key, item) => MapEntry((key ?? '').toString(), item));
    }
    return {};
  }

  List<Object?> _list(Object? value) {
    if (value is List) return value;
    if (value is String && value.trim().isNotEmpty) {
      try {
        final decoded = jsonDecode(value);
        if (decoded is List) return decoded;
      } catch (_) {
        return [];
      }
    }
    return [];
  }

  Future<void> _save() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      _storageKey,
      jsonEncode({
        'loggedIn': loggedIn,
        'cashOpen': cashOpen,
        'cashReconciliationRequired': cashReconciliationRequired,
        'unreconciledCashOpenedAt': unreconciledCashOpenedAt?.toIso8601String(),
        'onlineStoreOpen': onlineStoreOpen,
        'licenseKey': licenseKey,
        'operatorName': operatorName,
        'authEmail': authEmail,
        'syncStatus': syncStatus,
        'publicMenuId': publicMenuId,
        'publicMenuSlug': publicMenuSlug,
        'businessName': businessName,
        'businessLegalName': businessLegalName,
        'businessResponsible': businessResponsible,
        'businessDocument': businessDocument,
        'businessPhone': businessPhone,
        'businessCity': businessCity,
        'businessUf': businessUf,
        'businessAddress': businessAddress,
        'selectedOrderId': selectedOrderId,
        'search': search,
        'lastSync': lastSync,
        'whatsappConnected': whatsappConnected,
        'whatsappNumber': whatsappNumber,
        'whatsappSessionId': whatsappSessionId,
        'whatsappConnectionStatus': whatsappConnectionStatus,
        'whatsappMessage': whatsappMessage,
        'whatsappOnboardingUrl': whatsappOnboardingUrl,
        'whatsappLastSyncAt': whatsappLastSyncAt,
        'securityMessage': securityMessage,
        'cloudBackupEnabled': cloudBackupEnabled,
        'centralSyncEnabled': centralSyncEnabled,
        'lastBackupAt': lastBackupAt,
        'backupMessage': backupMessage,
        'pointConnected': pointConnected,
        'pointConnectionStatus': pointConnectionStatus,
        'pointDeviceName': pointDeviceName,
        'pointSerial': pointSerial,
        'pointStatus': pointStatus,
        'pointChargeMethod': pointChargeMethod,
        'pointPendingAmount': pointPendingAmount,
        'pointLastNsu': pointLastNsu,
        'pointSellerUserId': pointSellerUserId,
        'pointTerminalId': pointTerminalId,
        'pointTerminalLabel': pointTerminalLabel,
        'pointLastSyncAt': pointLastSyncAt,
        'pointLastError': pointLastError,
        'pointAttemptId': pointAttemptId,
        'pointOrderId': pointOrderId,
        'pointLocalReference': pointLocalReference,
        'pointConnectUrl': pointConnectUrl,
        'windowsBridgeUrl': windowsBridgeUrl,
        'windowsBridgeLocalUrl': windowsBridgeLocalUrl,
        'windowsBridgeStatus': windowsBridgeStatus,
        'windowsBridgeLastPrintAt': windowsBridgeLastPrintAt,
        'ifoodConnectionId': ifoodConnectionId,
        'ifoodConnectionStatus': ifoodConnectionStatus,
        'ifoodMerchantId': ifoodMerchantId,
        'ifoodMerchantName': ifoodMerchantName,
        'ifoodMessage': ifoodMessage,
        'ifoodVerificationUrl': ifoodVerificationUrl,
        'ifoodUserCode': ifoodUserCode,
        'ifoodLastSyncAt': ifoodLastSyncAt,
        'products': products.map((item) => item.toJson()).toList(),
        'orders': orders.map((item) => item.toJson()).toList(),
        'customers': customers.map((item) => item.toJson()).toList(),
        'teamMembers': teamMembers.map((item) => item.toJson()).toList(),
        'movements': movements.map((item) => item.toJson()).toList(),
        'stockMovements': stockMovements.map((item) => item.toJson()).toList(),
        'syncQueue': syncQueue,
      }),
    );
  }

  String _id(String prefix) =>
      '$prefix-${DateTime.now().microsecondsSinceEpoch}';
  String _time(DateTime value) =>
      '${value.hour.toString().padLeft(2, '0')}:${value.minute.toString().padLeft(2, '0')}';
}

class MercadoPagoTerminal {
  const MercadoPagoTerminal({
    required this.id,
    required this.label,
    required this.posId,
    required this.storeId,
    required this.operatingMode,
  });

  final String id;
  final String label;
  final String posId;
  final String storeId;
  final String operatingMode;

  String get display => label.trim().isEmpty ? id : label;
  bool get pdvMode => operatingMode.toUpperCase() == 'PDV';

  factory MercadoPagoTerminal.fromJson(Map<String, dynamic> json) {
    return MercadoPagoTerminal(
      id: '${json['id'] ?? ''}',
      label: '${json['label'] ?? ''}',
      posId: '${json['posId'] ?? ''}',
      storeId: '${json['storeId'] ?? ''}',
      operatingMode: '${json['operatingMode'] ?? ''}',
    );
  }
}

class _PaymentsResponse {
  const _PaymentsResponse({
    required this.ok,
    required this.statusCode,
    required this.data,
    required this.fallbackMessage,
  });

  factory _PaymentsResponse.error(String message) {
    return _PaymentsResponse(
      ok: false,
      statusCode: 0,
      data: {'message': message},
      fallbackMessage: message,
    );
  }

  final bool ok;
  final int statusCode;
  final Map<String, dynamic> data;
  final String fallbackMessage;

  String get message {
    final raw = data['message'] ?? data['error'] ?? fallbackMessage;
    final text = '$raw'.trim();
    return text.isEmpty ? fallbackMessage : text;
  }

  String string(String key) {
    final value = data[key];
    return value == null ? '' : '$value'.trim();
  }

  bool boolValue(String key) => data[key] == true;

  List<Map<String, dynamic>> list(String key) {
    final value = data[key];
    if (value is! List) return const [];
    return value.whereType<Map<String, dynamic>>().toList();
  }
}

extension FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull {
    final iterator = this.iterator;
    if (iterator.moveNext()) return iterator.current;
    return null;
  }
}
