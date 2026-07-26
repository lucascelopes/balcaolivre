import 'dart:async';
import 'dart:convert';
import 'dart:math';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:url_launcher/url_launcher.dart';

import '../data/repositories/shared_preferences_agenda_repository.dart';
import '../data/repositories/syncing_agenda_repository.dart';
import '../domain/models/agenda_data.dart';
import '../domain/models/agenda_settings.dart';
import '../services/agenda_account_api.dart';
import '../services/agenda_livre_license_identity.dart';
import '../services/android_device_api.dart';
import '../services/android_secure_store.dart';
import '../services/default_http_transport.dart';
import '../services/http_transport.dart';
import '../services/instagram_service.dart';
import '../services/mercado_pago_service.dart';
import 'agenda_app.dart';
import 'agenda_controller.dart';
import 'theme/agenda_theme.dart';

enum AndroidAgendaStage {
  connecting,
  active,
  subscriptionRequired,
  connectionRequired,
  provisioningRequired,
}

typedef AndroidStateClientFactory =
    AgendaAccountStateClient Function(
      AndroidSessionProvider sessionProvider,
      AndroidEntitlementCallback onEntitlement,
    );

class AgendaAndroidSessionController extends ChangeNotifier {
  AgendaAndroidSessionController({
    required SharedPreferences preferences,
    required AndroidSecureStore secureStore,
    required this.config,
    AndroidDeviceSessionApi? deviceApi,
    HttpTransport? transport,
    AndroidStateClientFactory? stateClientFactory,
    DateTime Function()? now,
  }) : _preferences = preferences,
       _secureStore = secureStore,
       _transport = transport ?? createDefaultHttpTransport(),
       _now = now ?? DateTime.now {
    _deviceApi =
        deviceApi ?? AndroidDeviceApi(config: config, transport: _transport);
    _stateClientFactory =
        stateClientFactory ??
        (sessionProvider, onEntitlement) => AndroidAgendaAccountApi(
          config: config,
          sessionProvider: sessionProvider,
          onEntitlement: onEntitlement,
          transport: _transport,
        );
  }

  static const String secureSessionKey = 'agenda_android_device_session_v1';
  static const String secureDeviceIdKey = 'agenda_android_device_id_v1';

  final SharedPreferences _preferences;
  final AndroidSecureStore _secureStore;
  final AndroidBuildConfig config;
  final HttpTransport _transport;
  final DateTime Function() _now;
  late final AndroidDeviceSessionApi _deviceApi;
  late final AndroidStateClientFactory _stateClientFactory;

  AndroidAgendaStage stage = AndroidAgendaStage.connecting;
  AndroidDeviceSession? _session;
  AgendaController? agendaController;
  String? errorMessage;
  bool offline = false;
  bool _initialized = false;
  bool _checking = false;
  Timer? _entitlementTimer;

  AndroidDeviceSession? get session => _session;
  AndroidBranding get branding =>
      _session?.branding ?? AndroidBranding(businessName: config.businessName);
  AndroidEntitlement? get entitlement => _session?.entitlement;
  bool get checking => _checking;

  Future<void> initialize() async {
    if (_initialized) return;
    _initialized = true;
    stage = AndroidAgendaStage.connecting;
    errorMessage = null;
    notifyListeners();

    if (kReleaseMode && config.apiBase.scheme != 'https') {
      _showProvisioningError(
        'Este aplicativo foi gerado com um endereço de serviço inseguro.',
      );
      return;
    }

    if (!kReleaseMode && config.devMode && !config.canProvision) {
      await _openLocalDevelopmentAgenda();
      return;
    }

    final stored = await _restoreSession();
    if (stored != null) {
      _session = stored;
      await checkEntitlement(force: true);
      return;
    }

    if (!config.canProvision) {
      _showProvisioningError(
        'Este APK não possui um pré-cadastro válido. Baixe novamente pelo link do seu estabelecimento.',
      );
      return;
    }
    await _provision();
  }

  Future<void> _provision() async {
    _checking = true;
    stage = AndroidAgendaStage.connecting;
    errorMessage = null;
    notifyListeners();
    try {
      final deviceId = await _deviceId();
      final provisioned = await _deviceApi.redeem(
        buildId: config.buildId,
        provisioningToken: config.provisioningToken,
        deviceId: deviceId,
        appVersion: config.appVersion,
        fallbackBusinessName: config.businessName,
      );
      await _acceptServerSession(provisioned);
    } on AndroidEntitlementException catch (error) {
      errorMessage = error.message;
      stage = AndroidAgendaStage.subscriptionRequired;
    } on Object catch (error) {
      _showProvisioningError(_messageFor(error));
    } finally {
      _checking = false;
      notifyListeners();
    }
  }

  Future<void> checkEntitlement({bool force = false}) async {
    if (_checking && !force) return;
    final current = _session;
    if (current == null) {
      if (!_initialized) return initialize();
      if (config.canProvision) return _provision();
      _showProvisioningError(
        'Não foi possível identificar este aparelho. Baixe novamente o APK personalizado.',
      );
      return;
    }

    _checking = true;
    errorMessage = null;
    if (agendaController == null) stage = AndroidAgendaStage.connecting;
    notifyListeners();
    try {
      final refreshed = await _deviceApi.refresh(current);
      offline = false;
      await _acceptServerSession(refreshed);
    } on AndroidEntitlementException catch (error) {
      final blocked = current.copyWith(entitlement: error.entitlement);
      await _persistSession(blocked);
      _session = blocked;
      _closeAgenda();
      stage = AndroidAgendaStage.subscriptionRequired;
      errorMessage = error.message;
    } on Object catch (error) {
      final now = _now().toUtc();
      if (current.entitlement.canUseOfflineAt(now)) {
        offline = true;
        if (agendaController == null) await _openAgenda(current);
        stage = AndroidAgendaStage.active;
        errorMessage = null;
        _scheduleEntitlementCheck(current.entitlement);
      } else {
        _closeAgenda();
        stage = AndroidAgendaStage.connectionRequired;
        errorMessage = _messageFor(error);
      }
    } finally {
      _checking = false;
      notifyListeners();
    }
  }

  Future<AndroidDeviceSession> _provideSession({
    bool forceRefresh = false,
  }) async {
    final current = _session;
    if (current == null) {
      throw const AgendaApiException(
        'android_session_missing',
        'Este aparelho ainda não foi conectado.',
        statusCode: 401,
      );
    }
    if (!forceRefresh) return current;
    final refreshed = await _deviceApi.refresh(current);
    await _acceptServerSession(refreshed, openAgenda: false);
    return refreshed;
  }

  Future<void> _acceptServerSession(
    AndroidDeviceSession value, {
    bool openAgenda = true,
  }) async {
    await _persistSession(value);
    _session = value;
    _scheduleEntitlementCheck(value.entitlement);
    if (!value.entitlement.canUseOfflineAt(_now().toUtc())) {
      _closeAgenda();
      stage = AndroidAgendaStage.subscriptionRequired;
      return;
    }
    if (openAgenda && agendaController == null) await _openAgenda(value);
    stage = AndroidAgendaStage.active;
  }

  Future<void> _applyEntitlement(AndroidEntitlement value) async {
    final current = _session;
    if (current == null) return;
    final updated = current.copyWith(entitlement: value);
    await _persistSession(updated);
    _session = updated;
    _scheduleEntitlementCheck(value);
    if (value.canUseOfflineAt(_now().toUtc())) {
      return;
    }
    _closeAgenda();
    stage = AndroidAgendaStage.subscriptionRequired;
    errorMessage =
        'Seu teste terminou. Regularize a assinatura para continuar.';
    notifyListeners();
  }

  Future<void> _openAgenda(AndroidDeviceSession value) async {
    final storageKey = _storageKeyForAccount(value.accountId);
    final local = SharedPreferencesAgendaRepository(
      _preferences,
      storageKey: storageKey,
      seedFactory: () => AgendaData(
        settings: AgendaSettings(
          businessName: value.branding.businessName,
          businessLogoPath: config.logoAsset.isNotEmpty
              ? config.logoAsset
              : value.branding.logoUrl,
          onboardingCompleted: false,
        ),
      ),
    );
    await _applyInitialBranding(local, value.branding);
    final stateApi = _stateClientFactory(_provideSession, _applyEntitlement);
    final repository = SyncingAgendaRepository(
      local: local,
      remote: stateApi,
      preferences: _preferences,
      syncMetadataKey: '$storageKey.sync',
      deviceId: value.deviceId,
      onUnauthorized: () async {
        _closeAgenda();
        stage = AndroidAgendaStage.provisioningRequired;
        errorMessage =
            'A conexão segura deste aparelho foi revogada. Baixe um novo APK ou fale com o suporte.';
        notifyListeners();
      },
    );
    late final AgendaController controller;
    final paymentLicense = AgendaLivreLicenseIdentity.forAccount(
      value.accountId,
    );
    final integrationMachineCode =
        AgendaLivreLicenseIdentity.machineCodeForAccount(value.accountId);
    final mercadoPagoService = MercadoPagoService(
      config: MercadoPagoServiceConfig(
        contextProvider: () {
          final settings = controller.data.settings;
          final accountEmail = settings.accountEmail.trim();
          final fallbackEmail =
              'device-${value.accountId.hashCode.abs()}@agendalivre.app';
          return MercadoPagoClientContext(
            licenseKey: paymentLicense,
            machineHash: value.deviceId,
            machineCode: 'AND-${value.deviceId.hashCode.abs()}',
            appVersion: config.appVersion,
            clientKind: 'android',
            localPlan: 'Agenda Livre Online',
            profile: <String, Object?>{
              'email': accountEmail.isEmpty ? fallbackEmail : accountEmail,
              'ownerName': settings.accountFullName.trim(),
              'businessName': settings.businessName.trim(),
              'businessDocument': settings.businessDocument.replaceAll(
                RegExp(r'\D'),
                '',
              ),
              'businessPhone': settings.businessPhone.replaceAll(
                RegExp(r'\D'),
                '',
              ),
              'segment': settings.businessSegment.trim(),
              'userId': value.accountId,
              'clientKind': 'agenda-livre-android',
            },
          );
        },
        headersProvider: () {
          final current = _session;
          if (current == null || current.deviceToken.trim().isEmpty) {
            return const <String, String>{};
          }
          return <String, String>{
            'Authorization': 'Device ${current.deviceToken.trim()}',
            'X-Agenda-Device-Id': current.deviceId,
          };
        },
      ),
      transport: _transport,
    );
    final instagramService = InstagramService(
      config: InstagramServiceConfig(
        contextProvider: () {
          final settings = controller.data.settings;
          return InstagramClientContext(
            licenseKey: paymentLicense,
            machineHash: integrationMachineCode,
            machineCode: integrationMachineCode,
            appVersion: config.appVersion,
            profile: <String, Object?>{
              'email': settings.accountEmail.trim(),
              'ownerName': settings.accountFullName.trim(),
              'businessName': settings.businessName.trim(),
              'userId': value.accountId,
              'clientKind': 'agenda-livre-android',
            },
          );
        },
        headersProvider: () {
          final current = _session;
          if (current == null || current.deviceToken.trim().isEmpty) {
            return const <String, String>{};
          }
          return <String, String>{
            'Authorization': 'Device ${current.deviceToken.trim()}',
            'X-Agenda-Device-Id': current.deviceId,
          };
        },
      ),
      transport: _transport,
    );
    controller = AgendaController(
      repository,
      // Android is provisioned to one account and deliberately has no logout.
      // This callback makes the existing "Sair" action close the process
      // without deleting the bound account or its cache.
      onLogout: () async => SystemNavigator.pop(),
      instagramService: instagramService,
      mercadoPagoService: mercadoPagoService,
    );
    agendaController = controller;
  }

  Future<void> _applyInitialBranding(
    SharedPreferencesAgendaRepository local,
    AndroidBranding branding,
  ) async {
    final data = await local.load();
    if (data == null) return;
    var changed = false;
    final currentName = data.settings.businessName.trim();
    if ((currentName.isEmpty || currentName == 'Balcão Livre') &&
        branding.businessName.trim().isNotEmpty) {
      data.settings.businessName = branding.businessName.trim();
      changed = true;
    }
    final persistentLogo = config.logoAsset.trim().isNotEmpty
        ? config.logoAsset.trim()
        : branding.logoUrl.trim();
    if (data.settings.businessLogoPath.trim().isEmpty &&
        persistentLogo.isNotEmpty) {
      data.settings.businessLogoPath = persistentLogo;
      changed = true;
    }
    if (changed) await local.save(data);
  }

  Future<void> _openLocalDevelopmentAgenda() async {
    final local = SharedPreferencesAgendaRepository(
      _preferences,
      storageKey: 'agenda_livre.android.dev.data.v1',
      seedFactory: () => AgendaData(
        settings: AgendaSettings(
          businessName: config.businessName,
          onboardingCompleted: false,
        ),
      ),
    );
    agendaController = AgendaController(local);
    stage = AndroidAgendaStage.active;
    offline = true;
    notifyListeners();
  }

  Future<AndroidDeviceSession?> _restoreSession() async {
    final source = await _secureStore.read(secureSessionKey);
    if (source == null) return null;
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map) throw const FormatException();
      return AndroidDeviceSession.fromJson(Map<String, dynamic>.from(decoded));
    } on Object {
      await _secureStore.delete(secureSessionKey);
      return null;
    }
  }

  Future<void> _persistSession(AndroidDeviceSession value) =>
      _secureStore.write(secureSessionKey, jsonEncode(value.toJson()));

  Future<String> _deviceId() async {
    final stored = (await _secureStore.read(secureDeviceIdKey))?.trim() ?? '';
    if (stored.isNotEmpty) return stored;
    final random = Random.secure();
    final bytes = List<int>.generate(18, (_) => random.nextInt(256));
    final generated = 'android-${base64Url.encode(bytes).replaceAll('=', '')}';
    await _secureStore.write(secureDeviceIdKey, generated);
    return generated;
  }

  void _scheduleEntitlementCheck(AndroidEntitlement value) {
    _entitlementTimer?.cancel();
    final now = _now().toUtc();
    var next = now.add(const Duration(minutes: 15));
    if (value.leaseExpiresAt.isBefore(next)) next = value.leaseExpiresAt;
    final trialEnd = value.trialEndsAt;
    if (trialEnd != null && trialEnd.isBefore(next)) next = trialEnd;
    var delay = next.difference(now);
    if (delay.isNegative || delay == Duration.zero) {
      delay = const Duration(seconds: 1);
    }
    _entitlementTimer = Timer(delay, () => unawaited(checkEntitlement()));
  }

  void _showProvisioningError(String message) {
    _closeAgenda();
    stage = AndroidAgendaStage.provisioningRequired;
    errorMessage = message;
    notifyListeners();
  }

  void _closeAgenda() {
    agendaController?.dispose();
    agendaController = null;
  }

  Future<bool> openPayment() async {
    final directUrl = entitlement?.paymentUrl.isNotEmpty == true
        ? entitlement!.paymentUrl
        : config.paymentUrl;
    if (directUrl.trim().isNotEmpty) return _openExternal(directUrl);
    final current = _session;
    if (current == null) return false;
    try {
      final random = Random.secure();
      final nonce = List<int>.generate(12, (_) => random.nextInt(256));
      final key = <String>[
        'android-checkout',
        current.deviceId,
        _now().toUtc().microsecondsSinceEpoch.toString(),
        base64Url.encode(nonce).replaceAll('=', ''),
      ].join('-');
      final checkout = await _deviceApi.createCheckout(
        current,
        idempotencyKey: key,
      );
      errorMessage = null;
      notifyListeners();
      return launchUrl(checkout, mode: LaunchMode.externalApplication);
    } on AndroidCheckoutUnavailableException catch (error) {
      errorMessage = error.message;
      notifyListeners();
      return false;
    } on Object catch (error) {
      errorMessage = _messageFor(error);
      notifyListeners();
      return false;
    }
  }

  Future<bool> openSupport() => _openExternal(
    entitlement?.supportUrl.isNotEmpty == true
        ? entitlement!.supportUrl
        : config.supportUrl,
  );

  Future<bool> _openExternal(String value) async {
    final uri = Uri.tryParse(value.trim());
    if (uri == null || uri.scheme != 'https' || uri.host.isEmpty) return false;
    return launchUrl(uri, mode: LaunchMode.externalApplication);
  }

  static String _storageKeyForAccount(String accountId) {
    final encoded = base64Url
        .encode(utf8.encode(accountId))
        .replaceAll('=', '');
    return 'agenda_livre.data.v2.$encoded';
  }

  static String _messageFor(Object error) {
    if (error is AgendaApiException) return error.message;
    if (error is HttpTransportException) {
      return 'Não foi possível confirmar a assinatura. Conecte-se à internet e tente novamente.';
    }
    return 'Não foi possível conectar este aparelho agora. Tente novamente.';
  }

  @override
  void dispose() {
    _entitlementTimer?.cancel();
    _closeAgenda();
    super.dispose();
  }
}

class AgendaLivreAndroidRoot extends StatefulWidget {
  const AgendaLivreAndroidRoot({
    super.key,
    required this.session,
    this.autoInitialize = true,
  });

  final AgendaAndroidSessionController session;
  final bool autoInitialize;

  @override
  State<AgendaLivreAndroidRoot> createState() => _AgendaLivreAndroidRootState();
}

class _AgendaLivreAndroidRootState extends State<AgendaLivreAndroidRoot>
    with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    if (widget.autoInitialize) unawaited(widget.session.initialize());
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state != AppLifecycleState.resumed) return;
    unawaited(widget.session.checkEntitlement());
    final controller = widget.session.agendaController;
    if (controller != null) unawaited(controller.refreshRemoteIfSafe());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: widget.session,
    builder: (context, _) {
      final controller = widget.session.agendaController;
      if (widget.session.stage == AndroidAgendaStage.active &&
          controller != null) {
        return AgendaLivreApp(
          controller: controller,
          homeBuilder: (context, child) => _AndroidActiveBrandOverlay(
            branding: widget.session.branding,
            config: widget.session.config,
            child: child,
          ),
        );
      }
      return _AndroidAccessApp(session: widget.session);
    },
  );
}

/// Android-only persistent brand mark. The optional home builder keeps the
/// regular Web/Windows AgendaLivreApp rendering unchanged.
class _AndroidActiveBrandOverlay extends StatelessWidget {
  const _AndroidActiveBrandOverlay({
    required this.branding,
    required this.config,
    required this.child,
  });

  final AndroidBranding branding;
  final AndroidBuildConfig config;
  final Widget child;

  @override
  Widget build(BuildContext context) => Stack(
    children: [
      Positioned.fill(child: child),
      Positioned(
        left: 10,
        bottom: 78,
        child: SafeArea(
          minimum: const EdgeInsets.only(bottom: 4),
          child: IgnorePointer(
            child: Material(
              key: const Key('android-active-brand'),
              elevation: 5,
              shadowColor: Colors.black.withValues(alpha: .18),
              borderRadius: BorderRadius.circular(16),
              clipBehavior: Clip.antiAlias,
              child: SizedBox(
                width: 202,
                height: 58,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    _BrandImage(
                      url: branding.coverUrl,
                      asset: config.coverAsset,
                      fit: BoxFit.cover,
                      fallback: const ColoredBox(color: Color(0xFF176B87)),
                    ),
                    const ColoredBox(color: Color(0x8A0F172A)),
                    Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 9,
                        vertical: 7,
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 42,
                            height: 42,
                            padding: const EdgeInsets.all(4),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            clipBehavior: Clip.antiAlias,
                            child: _BrandImage(
                              url: branding.logoUrl,
                              asset: config.logoAsset,
                              fit: BoxFit.contain,
                              fallback: const Icon(
                                Icons.calendar_month_rounded,
                                color: Color(0xFF176B87),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              branding.businessName,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 12,
                                height: 1.15,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    ],
  );
}

class _AndroidAccessApp extends StatelessWidget {
  const _AndroidAccessApp({required this.session});

  final AgendaAndroidSessionController session;

  @override
  Widget build(BuildContext context) => MaterialApp(
    title: session.branding.businessName,
    debugShowCheckedModeBanner: false,
    theme: AgendaThemes.byId('').toThemeData(),
    locale: const Locale('pt', 'BR'),
    supportedLocales: const <Locale>[Locale('pt', 'BR')],
    localizationsDelegates: const <LocalizationsDelegate<dynamic>>[
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    home: _AndroidAccessPage(session: session),
  );
}

class _AndroidAccessPage extends StatelessWidget {
  const _AndroidAccessPage({required this.session});

  final AgendaAndroidSessionController session;

  @override
  Widget build(BuildContext context) {
    final stage = session.stage;
    final connecting = stage == AndroidAgendaStage.connecting;
    final blocked = stage == AndroidAgendaStage.subscriptionRequired;
    final connection = stage == AndroidAgendaStage.connectionRequired;
    final title = connecting
        ? 'Conectando sua agenda'
        : blocked
        ? 'Assinatura necessária'
        : connection
        ? 'Confirme sua conexão'
        : 'Este APK precisa ser renovado';
    final description = connecting
        ? 'Estamos vinculando este aparelho ao pré-cadastro de ${session.branding.businessName}. Nenhum login é necessário.'
        : blocked
        ? _blockedMessage(session.entitlement)
        : connection
        ? 'A permissão offline expirou. Conecte-se à internet para confirmar a assinatura e liberar a agenda.'
        : session.errorMessage ??
              'Baixe novamente o aplicativo pelo link do seu estabelecimento.';

    return Scaffold(
      backgroundColor: const Color(0xFFF4F7FB),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 520),
              child: Card(
                key: Key('android-access-${stage.name}'),
                elevation: 0,
                color: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(24),
                  side: const BorderSide(color: Color(0xFFE4EAF2)),
                ),
                clipBehavior: Clip.antiAlias,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _AndroidBrandHeader(
                      branding: session.branding,
                      config: session.config,
                    ),
                    Padding(
                      padding: const EdgeInsets.fromLTRB(24, 24, 24, 26),
                      child: Column(
                        children: [
                          Icon(
                            connecting
                                ? Icons.sync_rounded
                                : blocked
                                ? Icons.lock_clock_rounded
                                : connection
                                ? Icons.wifi_off_rounded
                                : Icons.phonelink_erase_rounded,
                            size: 42,
                            color: blocked
                                ? const Color(0xFFB45309)
                                : const Color(0xFF176B87),
                          ),
                          const SizedBox(height: 14),
                          Text(
                            title,
                            key: const Key('android-access-title'),
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                              color: Color(0xFF172033),
                              fontSize: 23,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const SizedBox(height: 10),
                          Text(
                            description,
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                              color: Color(0xFF64748B),
                              fontSize: 14,
                              height: 1.45,
                            ),
                          ),
                          if (!connecting &&
                              session.errorMessage?.trim().isNotEmpty ==
                                  true) ...[
                            const SizedBox(height: 12),
                            Container(
                              key: const Key('android-payment-error'),
                              width: double.infinity,
                              padding: const EdgeInsets.all(11),
                              decoration: BoxDecoration(
                                color: const Color(0xFFFFF7ED),
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: const Color(0xFFFED7AA),
                                ),
                              ),
                              child: Text(
                                session.errorMessage!,
                                textAlign: TextAlign.center,
                                style: const TextStyle(
                                  color: Color(0xFF9A3412),
                                  fontSize: 12.5,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ],
                          if (connecting) ...[
                            const SizedBox(height: 22),
                            const LinearProgressIndicator(
                              key: Key('android-connecting-progress'),
                              minHeight: 6,
                              borderRadius: BorderRadius.all(
                                Radius.circular(20),
                              ),
                            ),
                          ] else ...[
                            const SizedBox(height: 22),
                            if (blocked)
                              SizedBox(
                                width: double.infinity,
                                child: FilledButton.icon(
                                  key: const Key('android-payment-button'),
                                  onPressed: () async {
                                    final opened = await session.openPayment();
                                    if (!opened && context.mounted) {
                                      _showUnavailable(context);
                                    }
                                  },
                                  icon: const Icon(Icons.payment_rounded),
                                  label: const Text('Regularizar assinatura'),
                                ),
                              ),
                            if (blocked) const SizedBox(height: 10),
                            SizedBox(
                              width: double.infinity,
                              child: OutlinedButton.icon(
                                key: const Key('android-retry-button'),
                                onPressed: session.checking
                                    ? null
                                    : () =>
                                          session.checkEntitlement(force: true),
                                icon: const Icon(Icons.refresh_rounded),
                                label: Text(
                                  blocked
                                      ? 'Já paguei, verificar novamente'
                                      : 'Tentar novamente',
                                ),
                              ),
                            ),
                            if (session.config.supportUrl.isNotEmpty ||
                                session.entitlement?.supportUrl.isNotEmpty ==
                                    true) ...[
                              const SizedBox(height: 8),
                              TextButton.icon(
                                key: const Key('android-support-button'),
                                onPressed: session.openSupport,
                                icon: const Icon(Icons.support_agent_rounded),
                                label: const Text('Falar com o suporte'),
                              ),
                            ],
                          ],
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  static String _blockedMessage(AndroidEntitlement? entitlement) {
    if (entitlement?.status == 'past_due') {
      return 'O pagamento está pendente. Regularize a assinatura para voltar a usar a agenda.';
    }
    if (entitlement?.status == 'suspended') {
      return 'O acesso deste estabelecimento está suspenso. Fale com o suporte para regularizar.';
    }
    return 'O teste de 7 dias terminou. Seus dados continuam seguros e o acesso volta assim que o pagamento for confirmado.';
  }

  static void _showUnavailable(BuildContext context) {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'O link de pagamento ainda não está disponível. Fale com o suporte.',
        ),
      ),
    );
  }
}

class _AndroidBrandHeader extends StatelessWidget {
  const _AndroidBrandHeader({required this.branding, required this.config});

  final AndroidBranding branding;
  final AndroidBuildConfig config;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 176,
    child: Stack(
      fit: StackFit.expand,
      children: [
        _BrandImage(
          url: branding.coverUrl,
          asset: config.coverAsset,
          fit: BoxFit.cover,
          fallback: const DecoratedBox(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: <Color>[Color(0xFF176B87), Color(0xFF64CCC5)],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
            ),
          ),
        ),
        const DecoratedBox(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: <Color>[Colors.transparent, Color(0xAA0F172A)],
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
            ),
          ),
        ),
        Positioned(
          left: 22,
          right: 22,
          bottom: 18,
          child: Row(
            children: [
              Container(
                width: 62,
                height: 62,
                padding: const EdgeInsets.all(7),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(18),
                  boxShadow: const <BoxShadow>[
                    BoxShadow(color: Color(0x33000000), blurRadius: 12),
                  ],
                ),
                clipBehavior: Clip.antiAlias,
                child: _BrandImage(
                  url: branding.logoUrl,
                  asset: config.logoAsset,
                  fit: BoxFit.contain,
                  fallback: const Icon(
                    Icons.calendar_month_rounded,
                    color: Color(0xFF176B87),
                    size: 34,
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Text(
                  branding.businessName,
                  key: const Key('android-business-name'),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    shadows: <Shadow>[
                      Shadow(color: Color(0x66000000), blurRadius: 5),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class _BrandImage extends StatelessWidget {
  const _BrandImage({
    required this.url,
    required this.asset,
    required this.fit,
    required this.fallback,
  });

  final String url;
  final String asset;
  final BoxFit fit;
  final Widget fallback;

  @override
  Widget build(BuildContext context) {
    final uri = Uri.tryParse(url.trim());
    if (uri != null && uri.scheme == 'https' && uri.host.isNotEmpty) {
      return Image.network(
        uri.toString(),
        fit: fit,
        errorBuilder: (_, _, _) => _assetOrFallback(),
      );
    }
    return _assetOrFallback();
  }

  Widget _assetOrFallback() {
    if (asset.trim().isEmpty) return fallback;
    return Image.asset(asset, fit: fit, errorBuilder: (_, _, _) => fallback);
  }
}
