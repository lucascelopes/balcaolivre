import 'dart:async';
import 'dart:convert';
import 'dart:math';

import 'default_http_transport.dart';
import 'http_transport.dart';

typedef MercadoPagoContextProvider =
    FutureOr<MercadoPagoClientContext> Function();
typedef MercadoPagoAccessTokenProvider = FutureOr<String?> Function();
typedef MercadoPagoHeadersProvider = FutureOr<Map<String, String>> Function();

class MercadoPagoServiceConfig {
  MercadoPagoServiceConfig({
    required this.contextProvider,
    Uri? baseUri,
    Uri? licenseActivationUri,
    this.accessTokenProvider,
    this.headersProvider,
    this.activateClient = true,
    this.timeout = const Duration(seconds: 16),
  }) : baseUri = baseUri ?? Uri.parse(defaultBaseUrl),
       licenseActivationUri =
           licenseActivationUri ?? Uri.parse(defaultLicenseActivationUrl) {
    if (!this.baseUri.hasScheme ||
        (this.baseUri.scheme != 'https' && this.baseUri.scheme != 'http')) {
      throw const MercadoPagoException(
        MercadoPagoFailure.invalidConfiguration,
        'A URL do serviço Mercado Pago é inválida.',
      );
    }
    if (!this.licenseActivationUri.hasScheme ||
        (this.licenseActivationUri.scheme != 'https' &&
            this.licenseActivationUri.scheme != 'http')) {
      throw const MercadoPagoException(
        MercadoPagoFailure.invalidConfiguration,
        'A URL de ativacao do Mercado Pago e invalida.',
      );
    }
  }

  static const String defaultBaseUrl =
      'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments';
  static const String defaultLicenseActivationUrl =
      'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/license/activate';

  final Uri baseUri;
  final Uri licenseActivationUri;
  final MercadoPagoContextProvider contextProvider;

  /// Supplies a short-lived end-user access token, such as a Supabase Auth
  /// session JWT. Never return a service-role key from a Flutter client.
  final MercadoPagoAccessTokenProvider? accessTokenProvider;

  /// Optional public/request metadata. Secrets must remain in the Edge
  /// Function and must not be supplied through this callback.
  final MercadoPagoHeadersProvider? headersProvider;
  final bool activateClient;
  final Duration timeout;
}

class MercadoPagoService {
  MercadoPagoService({required this.config, HttpTransport? transport})
    : _transport = transport ?? createDefaultHttpTransport();

  final MercadoPagoServiceConfig config;
  final HttpTransport _transport;
  bool _clientActivated = false;
  Future<_MercadoPagoHttpResponse?>? _activationInFlight;

  Future<MercadoPagoConnectResult> startConnect() async {
    final response = await _post(
      '/mercadopago/connect/start',
      eventName: 'agendalivre.mercadopago.connect',
    );
    return MercadoPagoConnectResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoConnectionStatusResult> fetchConnectionStatus() async {
    final response = await _post(
      '/mercadopago/status',
      eventName: 'agendalivre.mercadopago.status',
    );
    return MercadoPagoConnectionStatusResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoTerminalsResult> fetchTerminals() async {
    final response = await _post(
      '/mercadopago/terminals',
      eventName: 'agendalivre.mercadopago.terminals',
    );
    return MercadoPagoTerminalsResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoResult> selectTerminal({
    required String terminalId,
    String terminalLabel = '',
  }) async {
    final cleanId = terminalId.trim();
    if (cleanId.isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Informe a maquininha Mercado Pago.',
      );
    }

    final response = await _post(
      '/mercadopago/terminal/select',
      eventName: 'agendalivre.mercadopago.terminal.select',
      payload: <String, Object?>{
        'terminalId': cleanId,
        'terminalLabel': terminalLabel.trim(),
      },
    );
    return MercadoPagoResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoResult> releaseTerminal() async {
    final response = await _post(
      '/mercadopago/terminal/select',
      eventName: 'agendalivre.mercadopago.terminal.release',
      payload: const <String, Object?>{'terminalId': '', 'terminalLabel': ''},
    );
    return MercadoPagoResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoChargeResult> createPointCharge(
    MercadoPagoPointChargeRequest request,
  ) async {
    request.validate();
    final localReference = request.localReference.trim().isEmpty
        ? createLocalReference()
        : request.localReference.trim();
    final description = _clip(
      request.description,
      180,
      fallback: 'Agenda Livre',
    );
    final items = request.items.isEmpty
        ? <MercadoPagoChargeItem>[
            MercadoPagoChargeItem(
              code: 'AGENDALIVRE',
              title: 'Agenda Livre',
              quantity: 1,
              unitPriceInCents: request.amountInCents,
              description: description,
            ),
          ]
        : request.items;

    final response = await _post(
      '/mercadopago/point/charge',
      eventName: 'agendalivre.mercadopago.point.charge',
      payload: <String, Object?>{
        'amount': _formatCents(request.amountInCents),
        'method': request.method.apiValue,
        'localReference': localReference,
        'description': description,
        'terminalId': request.terminalId.trim(),
        'items': items.map((item) => item.toJson()).toList(growable: false),
      },
    );
    return MercadoPagoChargeResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoPointStatusResult> fetchPointStatus({
    String attemptId = '',
    String orderId = '',
    String localReference = '',
  }) async {
    if (attemptId.trim().isEmpty &&
        orderId.trim().isEmpty &&
        localReference.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Informe a tentativa ou referência do pagamento.',
      );
    }

    final response = await _post(
      '/mercadopago/point/status',
      eventName: 'agendalivre.mercadopago.point.status',
      payload: <String, Object?>{
        'attemptId': attemptId.trim(),
        'orderId': orderId.trim(),
        'localReference': localReference.trim(),
      },
    );
    return MercadoPagoPointStatusResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoPixChargeResult> createPixCharge(
    MercadoPagoPixChargeRequest request,
  ) async {
    request.validate();
    final localReference = request.localReference.trim().isEmpty
        ? createLocalReference()
        : request.localReference.trim();
    final description = _clip(
      request.description,
      180,
      fallback: 'Agenda Livre',
    );
    final items = request.items.isEmpty
        ? <MercadoPagoChargeItem>[
            MercadoPagoChargeItem(
              code: 'AGENDALIVRE',
              title: 'Atendimento Agenda Livre',
              quantity: 1,
              unitPriceInCents: request.amountInCents,
              description: description,
            ),
          ]
        : request.items;
    final response = await _post(
      '/mercadopago/web/charge',
      eventName: 'agendalivre.mercadopago.web.charge',
      payload: <String, Object?>{
        'amount': _formatCents(request.amountInCents),
        'method': 'PIX',
        'localReference': localReference,
        'description': description,
        'payerName': request.payerName.trim(),
        'payerEmail': request.payerEmail.trim(),
        'items': items.map((item) => item.toJson()).toList(growable: false),
      },
    );
    return MercadoPagoPixChargeResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoPointStatusResult> fetchPixStatus({
    String attemptId = '',
    String paymentId = '',
    String localReference = '',
  }) async {
    if (attemptId.trim().isEmpty &&
        paymentId.trim().isEmpty &&
        localReference.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Informe a tentativa ou referência do pagamento.',
      );
    }
    final response = await _post(
      '/mercadopago/web/status',
      eventName: 'agendalivre.mercadopago.web.status',
      payload: <String, Object?>{
        'attemptId': attemptId.trim(),
        'paymentId': paymentId.trim(),
        'localReference': localReference.trim(),
      },
    );
    return MercadoPagoPointStatusResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<MercadoPagoResult> cancelPointCharge({
    String attemptId = '',
    String orderId = '',
    String localReference = '',
  }) async {
    if (attemptId.trim().isEmpty &&
        orderId.trim().isEmpty &&
        localReference.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Informe a cobrança que deve ser cancelada.',
      );
    }
    final response = await _post(
      '/mercadopago/point/cancel',
      eventName: 'agendalivre.mercadopago.point.cancel',
      payload: <String, Object?>{
        'attemptId': attemptId.trim(),
        'orderId': orderId.trim(),
        'localReference': localReference.trim(),
      },
    );
    return MercadoPagoResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<_MercadoPagoHttpResponse> _post(
    String path, {
    required String eventName,
    Map<String, Object?> payload = const <String, Object?>{},
  }) async {
    final MercadoPagoClientContext context;
    try {
      context = await config.contextProvider();
      context.validate();
    } on MercadoPagoException {
      rethrow;
    } on Object catch (error) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidConfiguration,
        'Não foi possível obter a identificação do cliente Mercado Pago.',
        cause: error,
      );
    }

    final headers = <String, String>{'Accept': 'application/json'};
    try {
      final customHeaders = await config.headersProvider?.call();
      if (customHeaders != null) {
        headers.addAll(customHeaders);
      }
      final accessToken = (await config.accessTokenProvider?.call())?.trim();
      if (accessToken != null && accessToken.isNotEmpty) {
        headers['Authorization'] = 'Bearer $accessToken';
      }
    } on Object catch (error) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidConfiguration,
        'Não foi possível preparar a autorização do pagamento.',
        cause: error,
      );
    }
    headers['Content-Type'] = 'application/json; charset=utf-8';

    final body = <String, Object?>{
      ...context.toJson(),
      'eventName': eventName,
      ...payload,
    };
    if (config.activateClient) {
      final activationFailure = await _ensureClientActivated(context, headers);
      if (activationFailure != null) return activationFailure;
    }
    return _send(uri: _endpoint(path), headers: headers, body: body);
  }

  Future<_MercadoPagoHttpResponse?> _ensureClientActivated(
    MercadoPagoClientContext context,
    Map<String, String> headers,
  ) async {
    if (_clientActivated) return null;
    final pending = _activationInFlight;
    if (pending != null) return pending;
    final activation = _activateClient(context, headers);
    _activationInFlight = activation;
    try {
      return await activation;
    } finally {
      if (identical(_activationInFlight, activation)) {
        _activationInFlight = null;
      }
    }
  }

  Future<_MercadoPagoHttpResponse?> _activateClient(
    MercadoPagoClientContext context,
    Map<String, String> headers,
  ) async {
    final response = await _send(
      uri: config.licenseActivationUri,
      headers: headers,
      body: <String, Object?>{
        ...context.toJson(),
        'eventName': 'agendalivre.mercadopago.activate',
      },
    );
    if (_bool(response.json['ok'])) {
      _clientActivated = true;
      return null;
    }
    return response;
  }

  Future<_MercadoPagoHttpResponse> _send({
    required Uri uri,
    required Map<String, String> headers,
    required Map<String, Object?> body,
  }) async {
    final ServiceHttpResponse response;
    try {
      response = await _transport.send(
        ServiceHttpRequest(
          method: 'POST',
          uri: uri,
          headers: headers,
          body: jsonEncode(body),
          timeout: config.timeout,
        ),
      );
    } on Object catch (error) {
      throw MercadoPagoException(
        MercadoPagoFailure.network,
        'Não foi possível acessar o serviço de pagamentos.',
        cause: error,
      );
    }

    try {
      final decoded = jsonDecode(response.body);
      if (decoded is! Map) {
        throw const FormatException('A resposta não é um objeto JSON.');
      }
      final json = decoded.map((key, value) => MapEntry(key.toString(), value));
      return _MercadoPagoHttpResponse(
        statusCode: response.statusCode,
        json: json,
      );
    } on Object catch (error) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidResponse,
        'O serviço de pagamentos retornou uma resposta inválida.',
        statusCode: response.statusCode,
        cause: error,
      );
    }
  }

  Uri _endpoint(String path) {
    final root = config.baseUri.toString().replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$root/${path.replaceFirst(RegExp(r'^/+'), '')}');
  }

  static String createLocalReference({DateTime? now, Random? random}) {
    final instant = now ?? DateTime.now();
    final entropy = random ?? Random.secure();
    final suffix = List<int>.generate(4, (_) => entropy.nextInt(256))
        .map((value) => value.toRadixString(16).padLeft(2, '0'))
        .join()
        .toUpperCase();
    final timestamp =
        '${instant.year.toString().padLeft(4, '0')}'
        '${instant.month.toString().padLeft(2, '0')}'
        '${instant.day.toString().padLeft(2, '0')}'
        '${instant.hour.toString().padLeft(2, '0')}'
        '${instant.minute.toString().padLeft(2, '0')}'
        '${instant.second.toString().padLeft(2, '0')}';
    return 'AGL-$timestamp-$suffix';
  }

  static String _formatCents(int cents) {
    final reais = cents ~/ 100;
    final centavos = (cents % 100).abs().toString().padLeft(2, '0');
    return '$reais.$centavos';
  }

  static String _clip(String value, int maxLength, {required String fallback}) {
    final clean = value.trim().isEmpty ? fallback : value.trim();
    return clean.length <= maxLength ? clean : clean.substring(0, maxLength);
  }
}

class MercadoPagoClientContext {
  const MercadoPagoClientContext({
    required this.licenseKey,
    required this.machineHash,
    this.machineCode = '',
    this.appVersion = 'AgendaLivre.Flutter',
    this.clientKind = 'web',
    this.localPlan = 'Agenda Livre Online',
    this.profile = const <String, Object?>{},
  });

  final String licenseKey;
  final String machineHash;
  final String machineCode;
  final String appVersion;
  final String clientKind;
  final String localPlan;
  final Map<String, Object?> profile;

  void validate() {
    if (licenseKey.trim().isEmpty || machineHash.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.invalidConfiguration,
        'A identificação de licença e dispositivo é obrigatória.',
      );
    }
  }

  Map<String, Object?> toJson() => <String, Object?>{
    'licenseKey': licenseKey.trim(),
    'machineHash': machineHash.trim(),
    'machineCode': machineCode.trim(),
    'appVersion': appVersion.trim(),
    'clientKind': clientKind.trim(),
    'localPlan': localPlan.trim(),
    'profile': Map<String, Object?>.from(profile),
  };
}

enum MercadoPagoPointMethod {
  credit('CREDITO'),
  debit('DEBITO');

  const MercadoPagoPointMethod(this.apiValue);

  final String apiValue;
}

class MercadoPagoPointChargeRequest {
  const MercadoPagoPointChargeRequest({
    required this.amountInCents,
    required this.method,
    required this.terminalId,
    this.localReference = '',
    this.description = 'Agenda Livre',
    this.items = const <MercadoPagoChargeItem>[],
  });

  final int amountInCents;
  final MercadoPagoPointMethod method;
  final String terminalId;
  final String localReference;
  final String description;
  final List<MercadoPagoChargeItem> items;

  void validate() {
    if (amountInCents <= 0) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'O valor da cobrança deve ser maior que zero.',
      );
    }
    if (terminalId.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Selecione a maquininha Mercado Pago.',
      );
    }
    for (final item in items) {
      item.validate();
    }
  }
}

class MercadoPagoPixChargeRequest {
  const MercadoPagoPixChargeRequest({
    required this.amountInCents,
    this.localReference = '',
    this.description = 'Agenda Livre',
    this.payerName = 'Cliente',
    this.payerEmail = '',
    this.items = const <MercadoPagoChargeItem>[],
  });

  final int amountInCents;
  final String localReference;
  final String description;
  final String payerName;
  final String payerEmail;
  final List<MercadoPagoChargeItem> items;

  void validate() {
    if (amountInCents <= 0) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'O valor da cobrança deve ser maior que zero.',
      );
    }
    for (final item in items) {
      item.validate();
    }
  }
}

class MercadoPagoChargeItem {
  const MercadoPagoChargeItem({
    required this.code,
    required this.title,
    required this.quantity,
    required this.unitPriceInCents,
    this.description = '',
  });

  final String code;
  final String title;
  final int quantity;
  final int unitPriceInCents;
  final String description;

  void validate() {
    if (title.trim().isEmpty || quantity <= 0 || unitPriceInCents < 0) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Existe um item inválido na cobrança Mercado Pago.',
      );
    }
  }

  Map<String, Object?> toJson() => <String, Object?>{
    'code': code.trim(),
    'title': title.trim(),
    'quantity': quantity,
    'unitPrice': MercadoPagoService._formatCents(unitPriceInCents),
    'description': MercadoPagoService._clip(description, 180, fallback: title),
  };
}

class MercadoPagoResult {
  const MercadoPagoResult({
    required this.ok,
    this.message = '',
    this.statusCode = 0,
  });

  factory MercadoPagoResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return MercadoPagoResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
    );
  }

  final bool ok;
  final String message;
  final int statusCode;
}

class MercadoPagoConnectResult extends MercadoPagoResult {
  const MercadoPagoConnectResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.authUrl,
    this.expiresAt,
  });

  factory MercadoPagoConnectResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return MercadoPagoConnectResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      authUrl: _uri(json['authUrl']),
      expiresAt: _date(json['expiresAt']),
    );
  }

  final Uri? authUrl;
  final DateTime? expiresAt;
}

class MercadoPagoConnectionStatusResult extends MercadoPagoResult {
  const MercadoPagoConnectionStatusResult({
    required super.ok,
    super.message,
    super.statusCode,
    required this.connected,
    this.status = '',
    this.sellerUserId = '',
    this.selectedTerminalId = '',
    this.selectedTerminalLabel = '',
    this.lastSyncAt,
    this.lastError = '',
  });

  factory MercadoPagoConnectionStatusResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return MercadoPagoConnectionStatusResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      connected: _bool(json['connected']),
      status: _string(json['status']),
      sellerUserId: _string(json['sellerUserId']),
      selectedTerminalId: _string(json['selectedTerminalId']),
      selectedTerminalLabel: _string(json['selectedTerminalLabel']),
      lastSyncAt: _date(json['lastSyncAt']),
      lastError: _string(json['lastError']),
    );
  }

  final bool connected;
  final String status;
  final String sellerUserId;
  final String selectedTerminalId;
  final String selectedTerminalLabel;
  final DateTime? lastSyncAt;
  final String lastError;
}

class MercadoPagoTerminalsResult extends MercadoPagoResult {
  const MercadoPagoTerminalsResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.terminals = const <MercadoPagoTerminal>[],
    this.selectedTerminalId = '',
    this.selectedTerminalLabel = '',
  });

  factory MercadoPagoTerminalsResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    final rawTerminals = json['terminals'];
    final terminals = rawTerminals is List
        ? rawTerminals
              .whereType<Map>()
              .map(
                (item) => MercadoPagoTerminal.fromJson(
                  item.map((key, value) => MapEntry(key.toString(), value)),
                ),
              )
              .toList(growable: false)
        : const <MercadoPagoTerminal>[];
    return MercadoPagoTerminalsResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      terminals: terminals,
      selectedTerminalId: _string(json['selectedTerminalId']),
      selectedTerminalLabel: _string(json['selectedTerminalLabel']),
    );
  }

  final List<MercadoPagoTerminal> terminals;
  final String selectedTerminalId;
  final String selectedTerminalLabel;
}

class MercadoPagoTerminal {
  const MercadoPagoTerminal({
    required this.id,
    this.label = '',
    this.posId = '',
    this.storeId = '',
    this.operatingMode = '',
    this.modelCode = '',
    this.modelName = '',
    this.serial = '',
  });

  factory MercadoPagoTerminal.fromJson(Map<String, Object?> json) {
    return MercadoPagoTerminal(
      id: _string(json['id']),
      label: _string(json['label']),
      posId: _string(json['posId']),
      storeId: _string(json['storeId']),
      operatingMode: _string(json['operatingMode']),
      modelCode: _string(json['modelCode']),
      modelName: _string(json['modelName']),
      serial: _string(json['serial']),
    );
  }

  final String id;
  final String label;
  final String posId;
  final String storeId;
  final String operatingMode;
  final String modelCode;
  final String modelName;
  final String serial;

  String get display => label.trim().isEmpty ? id : label;
}

class MercadoPagoChargeResult extends MercadoPagoResult {
  const MercadoPagoChargeResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.attemptId = '',
    this.localReference = '',
    this.orderId = '',
    this.paymentId = '',
    this.status = '',
    this.statusDetail = '',
  });

  factory MercadoPagoChargeResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return MercadoPagoChargeResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      attemptId: _string(json['attemptId']),
      localReference: _string(json['localReference']),
      orderId: _string(json['orderId']),
      paymentId: _string(json['paymentId']),
      status: _string(json['status']),
      statusDetail: _string(json['statusDetail']),
    );
  }

  final String attemptId;
  final String localReference;
  final String orderId;
  final String paymentId;
  final String status;
  final String statusDetail;
}

class MercadoPagoPixChargeResult extends MercadoPagoResult {
  const MercadoPagoPixChargeResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.attemptId = '',
    this.localReference = '',
    this.paymentId = '',
    this.status = '',
    this.statusDetail = '',
    this.qrCode = '',
    this.qrCodeBase64 = '',
    this.ticketUrl = '',
    this.paymentUrl = '',
    this.expiresAt,
  });

  factory MercadoPagoPixChargeResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return MercadoPagoPixChargeResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      attemptId: _string(json['attemptId']),
      localReference: _string(json['localReference']),
      paymentId: _string(json['paymentId']),
      status: _string(json['status']),
      statusDetail: _string(json['statusDetail']),
      qrCode: _string(json['qrCode']),
      qrCodeBase64: _string(json['qrCodeBase64']),
      ticketUrl: _string(json['ticketUrl']),
      paymentUrl: _string(json['paymentUrl']),
      expiresAt: _date(json['expiresAt']),
    );
  }

  final String attemptId;
  final String localReference;
  final String paymentId;
  final String status;
  final String statusDetail;
  final String qrCode;
  final String qrCodeBase64;
  final String ticketUrl;
  final String paymentUrl;
  final DateTime? expiresAt;
}

class MercadoPagoPointStatusResult extends MercadoPagoResult {
  const MercadoPagoPointStatusResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.attemptId = '',
    this.orderId = '',
    this.paymentId = '',
    this.status = '',
    this.statusDetail = '',
    this.paid = false,
  });

  factory MercadoPagoPointStatusResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return MercadoPagoPointStatusResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      attemptId: _string(json['attemptId']),
      orderId: _string(json['orderId']),
      paymentId: _string(json['paymentId']),
      status: _string(json['status']),
      statusDetail: _string(json['statusDetail']),
      paid: _bool(json['paid']),
    );
  }

  final String attemptId;
  final String orderId;
  final String paymentId;
  final String status;
  final String statusDetail;
  final bool paid;
}

enum MercadoPagoFailure {
  invalidConfiguration,
  validation,
  network,
  invalidResponse,
}

class MercadoPagoException implements Exception {
  const MercadoPagoException(
    this.failure,
    this.message, {
    this.statusCode,
    this.cause,
  });

  final MercadoPagoFailure failure;
  final String message;
  final int? statusCode;
  final Object? cause;

  @override
  String toString() => 'MercadoPagoException(${failure.name}): $message';
}

class _MercadoPagoHttpResponse {
  const _MercadoPagoHttpResponse({
    required this.statusCode,
    required this.json,
  });

  final int statusCode;
  final Map<String, Object?> json;
}

String _string(Object? value) => value?.toString().trim() ?? '';

bool _bool(Object? value) => switch (value) {
  true => true,
  String text => text.toLowerCase() == 'true' || text == '1',
  num number => number != 0,
  _ => false,
};

DateTime? _date(Object? value) {
  final text = _string(value);
  return text.isEmpty ? null : DateTime.tryParse(text);
}

Uri? _uri(Object? value) {
  final text = _string(value);
  final uri = Uri.tryParse(text);
  return uri != null && uri.hasScheme ? uri : null;
}
