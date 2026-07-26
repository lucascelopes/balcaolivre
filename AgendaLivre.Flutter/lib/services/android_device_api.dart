import 'dart:async';
import 'dart:convert';

import '../services/agenda_account_api.dart';
import '../services/default_http_transport.dart';
import '../services/http_transport.dart';

class AndroidBuildConfig {
  const AndroidBuildConfig({
    required this.apiBase,
    required this.buildId,
    required this.provisioningToken,
    required this.appVersion,
    required this.businessName,
    required this.logoAsset,
    required this.coverAsset,
    required this.paymentUrl,
    required this.supportUrl,
    required this.devMode,
  });

  factory AndroidBuildConfig.fromEnvironment() => AndroidBuildConfig(
    apiBase: Uri.parse(
      const String.fromEnvironment(
        'AGENDA_LIVRE_API_BASE',
        defaultValue: defaultAgendaLivreApiBase,
      ),
    ),
    buildId: const String.fromEnvironment('AGENDA_ANDROID_BUILD_ID'),
    provisioningToken: const String.fromEnvironment(
      'AGENDA_ANDROID_PROVISIONING_TOKEN',
    ),
    appVersion: const String.fromEnvironment(
      'AGENDA_ANDROID_APP_VERSION',
      defaultValue: '1.0.0',
    ),
    businessName: const String.fromEnvironment(
      'AGENDA_ANDROID_BUSINESS_NAME',
      defaultValue: 'Agenda Livre',
    ),
    logoAsset: const String.fromEnvironment(
      'AGENDA_ANDROID_LOGO_ASSET',
      defaultValue: 'assets/branding/agenda-livre-mark.png',
    ),
    coverAsset: const String.fromEnvironment('AGENDA_ANDROID_COVER_ASSET'),
    paymentUrl: const String.fromEnvironment('AGENDA_ANDROID_PAYMENT_URL'),
    supportUrl: const String.fromEnvironment('AGENDA_ANDROID_SUPPORT_URL'),
    devMode: const bool.fromEnvironment('AGENDA_ANDROID_DEV_MODE'),
  );

  final Uri apiBase;
  final String buildId;
  final String provisioningToken;
  final String appVersion;
  final String businessName;
  final String logoAsset;
  final String coverAsset;
  final String paymentUrl;
  final String supportUrl;
  final bool devMode;

  bool get canProvision =>
      buildId.trim().isNotEmpty && provisioningToken.trim().isNotEmpty;

  Uri endpoint(String path) {
    final root = apiBase.toString().replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$root${path.startsWith('/') ? path : '/$path'}');
  }
}

class AndroidBranding {
  const AndroidBranding({
    required this.businessName,
    this.logoUrl = '',
    this.coverUrl = '',
  });

  final String businessName;
  final String logoUrl;
  final String coverUrl;

  Map<String, Object?> toJson() => <String, Object?>{
    'businessName': businessName,
    'logoUrl': logoUrl,
    'coverUrl': coverUrl,
  };

  factory AndroidBranding.fromJson(
    Object? value, {
    String fallbackName = 'Agenda Livre',
  }) {
    final json = _asObject(value);
    return AndroidBranding(
      businessName: _firstString(json, const <String>[
        'businessName',
        'establishmentName',
        'name',
      ], fallback: fallbackName),
      logoUrl: _firstString(json, const <String>['logoUrl', 'iconUrl']),
      coverUrl: _firstString(json, const <String>[
        'coverUrl',
        'photoUrl',
        'heroUrl',
      ]),
    );
  }
}

class AndroidEntitlement {
  const AndroidEntitlement({
    required this.status,
    required this.canUse,
    required this.checkedAt,
    required this.leaseExpiresAt,
    this.trialStartedAt,
    this.trialEndsAt,
    this.daysRemaining = 0,
    this.paymentUrl = '',
    this.supportUrl = '',
  });

  static const Duration maximumOfflineLease = Duration(hours: 24);
  static const Duration defaultOnlineLease = Duration(minutes: 15);

  final String status;
  final bool canUse;
  final DateTime checkedAt;
  final DateTime leaseExpiresAt;
  final DateTime? trialStartedAt;
  final DateTime? trialEndsAt;
  final int daysRemaining;
  final String paymentUrl;
  final String supportUrl;

  bool canUseOfflineAt(DateTime now) {
    final instant = now.toUtc();
    if (!canUse || !instant.isBefore(leaseExpiresAt.toUtc())) return false;
    final trialEnd = trialEndsAt;
    return trialEnd == null || instant.isBefore(trialEnd.toUtc());
  }

  Map<String, Object?> toJson() => <String, Object?>{
    'status': status,
    'canUse': canUse,
    'checkedAt': checkedAt.toUtc().toIso8601String(),
    'leaseExpiresAt': leaseExpiresAt.toUtc().toIso8601String(),
    'trialStartedAt': trialStartedAt?.toUtc().toIso8601String(),
    'trialEndsAt': trialEndsAt?.toUtc().toIso8601String(),
    'daysRemaining': daysRemaining,
    'paymentUrl': paymentUrl,
    'supportUrl': supportUrl,
  };

  factory AndroidEntitlement.fromJson(
    Object? value, {
    required DateTime receivedAt,
    String fallbackPaymentUrl = '',
    String fallbackSupportUrl = '',
  }) {
    final json = _asObject(value);
    final checkedAt =
        _date(json['checkedAt']) ??
        _date(json['serverTime']) ??
        receivedAt.toUtc();
    final canUse = json['canUse'] == true || json['allowed'] == true;
    final trialEndsAt = _date(
      json['trialEndsAt'] ?? json['trialEnd'] ?? json['endsAt'],
    );
    final maximum = checkedAt.add(maximumOfflineLease);
    var lease =
        _date(json['leaseExpiresAt'] ?? json['offlineUntil']) ??
        checkedAt.add(defaultOnlineLease);
    if (lease.isAfter(maximum)) lease = maximum;
    if (trialEndsAt != null && lease.isAfter(trialEndsAt)) {
      lease = trialEndsAt;
    }
    if (!canUse) lease = checkedAt;
    final rawDays = _integer(json['daysRemaining'], fallback: -1);
    final calculatedDays = trialEndsAt == null
        ? 0
        : ((trialEndsAt.difference(checkedAt).inMinutes / 1440).ceil()).clamp(
            0,
            9999,
          );
    return AndroidEntitlement(
      status: _firstString(json, const <String>[
        'status',
      ], fallback: canUse ? 'active' : 'unknown'),
      canUse: canUse,
      checkedAt: checkedAt,
      leaseExpiresAt: lease,
      trialStartedAt: _date(json['trialStartedAt'] ?? json['startedAt']),
      trialEndsAt: trialEndsAt,
      daysRemaining: rawDays >= 0 ? rawDays : calculatedDays,
      paymentUrl: _firstString(json, const <String>[
        'paymentUrl',
        'checkoutUrl',
      ], fallback: fallbackPaymentUrl),
      supportUrl: _firstString(json, const <String>[
        'supportUrl',
      ], fallback: fallbackSupportUrl),
    );
  }
}

class AndroidDeviceSession {
  const AndroidDeviceSession({
    required this.accountId,
    required this.deviceId,
    required this.deviceToken,
    required this.branding,
    required this.entitlement,
    this.tokenExpiresAt,
  });

  final String accountId;
  final String deviceId;
  final String deviceToken;
  final DateTime? tokenExpiresAt;
  final AndroidBranding branding;
  final AndroidEntitlement entitlement;

  AndroidDeviceSession copyWith({
    String? deviceToken,
    DateTime? tokenExpiresAt,
    AndroidBranding? branding,
    AndroidEntitlement? entitlement,
  }) => AndroidDeviceSession(
    accountId: accountId,
    deviceId: deviceId,
    deviceToken: deviceToken ?? this.deviceToken,
    tokenExpiresAt: tokenExpiresAt ?? this.tokenExpiresAt,
    branding: branding ?? this.branding,
    entitlement: entitlement ?? this.entitlement,
  );

  Map<String, Object?> toJson() => <String, Object?>{
    'accountId': accountId,
    'deviceId': deviceId,
    'deviceToken': deviceToken,
    'tokenExpiresAt': tokenExpiresAt?.toUtc().toIso8601String(),
    'branding': branding.toJson(),
    'entitlement': entitlement.toJson(),
  };

  factory AndroidDeviceSession.fromJson(Map<String, dynamic> json) {
    final checkedAt = DateTime.now().toUtc();
    final accountId = _firstString(json, const <String>['accountId', 'userId']);
    final deviceId = _string(json['deviceId']);
    final deviceToken = _firstString(json, const <String>[
      'deviceToken',
      'token',
    ]);
    if (accountId.isEmpty || deviceId.isEmpty || deviceToken.isEmpty) {
      throw const FormatException('Invalid Android device session');
    }
    return AndroidDeviceSession(
      accountId: accountId,
      deviceId: deviceId,
      deviceToken: deviceToken,
      tokenExpiresAt: _date(json['tokenExpiresAt']),
      branding: AndroidBranding.fromJson(json['branding']),
      entitlement: AndroidEntitlement.fromJson(
        json['entitlement'],
        receivedAt: checkedAt,
      ),
    );
  }
}

abstract interface class AndroidDeviceSessionApi {
  Future<AndroidDeviceSession> redeem({
    required String buildId,
    required String provisioningToken,
    required String deviceId,
    required String appVersion,
    required String fallbackBusinessName,
  });

  Future<AndroidDeviceSession> refresh(AndroidDeviceSession current);

  Future<Uri> createCheckout(
    AndroidDeviceSession current, {
    required String idempotencyKey,
  });
}

class AndroidDeviceApi implements AndroidDeviceSessionApi {
  AndroidDeviceApi({
    required this.config,
    HttpTransport? transport,
    DateTime Function()? now,
  }) : _transport = transport ?? createDefaultHttpTransport(),
       _now = now ?? DateTime.now;

  final AndroidBuildConfig config;
  final HttpTransport _transport;
  final DateTime Function() _now;

  @override
  Future<AndroidDeviceSession> redeem({
    required String buildId,
    required String provisioningToken,
    required String deviceId,
    required String appVersion,
    required String fallbackBusinessName,
  }) async {
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: config.endpoint('/api/agenda/android/provision/redeem'),
        headers: const <String, String>{
          'Accept': 'application/json',
          'Content-Type': 'application/json; charset=utf-8',
        },
        body: jsonEncode(<String, Object?>{
          'buildId': buildId,
          'provisioningToken': provisioningToken,
          'deviceId': deviceId,
          'platform': 'android',
          'appVersion': appVersion,
        }),
        timeout: const Duration(seconds: 25),
      ),
    );
    if (!response.isSuccess) {
      throw _exceptionFromResponse(response, receivedAt: _now().toUtc());
    }
    return _sessionFromResponse(
      response.body,
      deviceId: deviceId,
      previous: null,
      fallbackBusinessName: fallbackBusinessName,
      receivedAt: _now().toUtc(),
    );
  }

  @override
  Future<AndroidDeviceSession> refresh(AndroidDeviceSession current) async {
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: config.endpoint('/api/agenda/android/session/refresh'),
        headers: <String, String>{
          'Accept': 'application/json',
          'Content-Type': 'application/json; charset=utf-8',
          'Authorization': 'Device ${current.deviceToken}',
        },
        body: jsonEncode(<String, Object?>{
          'deviceId': current.deviceId,
          'platform': 'android',
          'appVersion': config.appVersion,
        }),
        timeout: const Duration(seconds: 20),
      ),
    );
    final receivedAt = _now().toUtc();
    if (!response.isSuccess) {
      throw _exceptionFromResponse(response, receivedAt: receivedAt);
    }
    return _sessionFromResponse(
      response.body,
      deviceId: current.deviceId,
      previous: current,
      fallbackBusinessName: current.branding.businessName,
      receivedAt: receivedAt,
    );
  }

  @override
  Future<Uri> createCheckout(
    AndroidDeviceSession current, {
    required String idempotencyKey,
  }) async {
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: config.endpoint('/api/agenda/android/checkout'),
        headers: <String, String>{
          'Accept': 'application/json',
          'Content-Type': 'application/json; charset=utf-8',
          'Authorization': 'Device ${current.deviceToken}',
          'Idempotency-Key': idempotencyKey,
        },
        body: jsonEncode(<String, Object?>{
          'deviceId': current.deviceId,
          'idempotencyKey': idempotencyKey,
        }),
        timeout: const Duration(seconds: 20),
      ),
    );
    if (!response.isSuccess) {
      final apiError = AgendaApiException.fromResponse(response);
      if (response.statusCode == 503 &&
          apiError.code == 'checkout_not_configured') {
        throw const AndroidCheckoutUnavailableException(
          'O pagamento online ainda não foi configurado. Fale com o suporte para regularizar.',
        );
      }
      throw apiError;
    }
    final json = _decodeObject(response.body);
    final checkout = _asObject(json['checkout']);
    final url = _firstString(checkout, const <String>['url']);
    final uri = Uri.tryParse(url);
    if (uri == null || uri.scheme != 'https' || uri.host.isEmpty) {
      throw const AgendaApiException(
        'checkout_response_invalid',
        'O servidor não retornou um link de pagamento seguro.',
      );
    }
    return uri;
  }

  AndroidDeviceSession _sessionFromResponse(
    String source, {
    required String deviceId,
    required AndroidDeviceSession? previous,
    required String fallbackBusinessName,
    required DateTime receivedAt,
  }) {
    final json = _decodeObject(source);
    final device = _asObject(json['device']);
    final session = _asObject(json['session']);
    final account = _asObject(json['account']);
    final accountId = _firstString(json, const <String>[
      'accountId',
      'userId',
    ], fallback: _firstString(account, const <String>['id', 'userId']));
    final token = _firstString(
      device,
      const <String>['token', 'deviceToken'],
      fallback: _firstString(
        session,
        const <String>['token', 'deviceToken'],
        fallback: _firstString(json, const <String>[
          'deviceToken',
          'token',
        ], fallback: previous?.deviceToken ?? ''),
      ),
    );
    final resolvedAccountId = accountId.isEmpty
        ? previous?.accountId ?? ''
        : accountId;
    if (resolvedAccountId.isEmpty || token.isEmpty) {
      throw const AgendaApiException(
        'android_session_invalid',
        'O servidor não retornou uma sessão válida para este aparelho.',
      );
    }
    final rawBranding = json['branding'] ?? account['branding'];
    final branding = rawBranding == null
        ? previous?.branding ??
              AndroidBranding(businessName: fallbackBusinessName)
        : AndroidBranding.fromJson(
            rawBranding,
            fallbackName: fallbackBusinessName,
          );
    final entitlement = AndroidEntitlement.fromJson(
      json['entitlement'],
      receivedAt: receivedAt,
      fallbackPaymentUrl: config.paymentUrl,
      fallbackSupportUrl: config.supportUrl,
    );
    return AndroidDeviceSession(
      accountId: resolvedAccountId,
      deviceId: _firstString(device, const <String>[
        'id',
        'deviceId',
      ], fallback: deviceId),
      deviceToken: token,
      tokenExpiresAt:
          _date(device['expiresAt'] ?? session['expiresAt']) ??
          previous?.tokenExpiresAt,
      branding: branding,
      entitlement: entitlement,
    );
  }
}

class AndroidEntitlementException implements Exception {
  const AndroidEntitlementException(this.entitlement, this.message);

  final AndroidEntitlement entitlement;
  final String message;

  @override
  String toString() => message;
}

class AndroidCheckoutUnavailableException implements Exception {
  const AndroidCheckoutUnavailableException(this.message);

  final String message;

  @override
  String toString() => message;
}

typedef AndroidSessionProvider =
    Future<AndroidDeviceSession> Function({bool forceRefresh});
typedef AndroidEntitlementCallback =
    FutureOr<void> Function(AndroidEntitlement entitlement);

class AndroidAgendaAccountApi implements AgendaAccountStateClient {
  AndroidAgendaAccountApi({
    required AndroidBuildConfig config,
    required AndroidSessionProvider sessionProvider,
    required AndroidEntitlementCallback onEntitlement,
    HttpTransport? transport,
  }) : _config = config,
       _sessionProvider = sessionProvider,
       _onEntitlement = onEntitlement,
       _transport = transport ?? createDefaultHttpTransport();

  final AndroidBuildConfig _config;
  final AndroidSessionProvider _sessionProvider;
  final AndroidEntitlementCallback _onEntitlement;
  final HttpTransport _transport;

  @override
  Future<AgendaRemoteState> fetchState() async {
    final response = await _authorizedRequest(method: 'GET');
    return AgendaRemoteState.fromJson(_decodeObject(response.body));
  }

  @override
  Future<AgendaRemoteState> saveState({
    required int baseRevision,
    required int schemaVersion,
    required Map<String, dynamic> payload,
    required String deviceId,
  }) async {
    final response = await _authorizedRequest(
      method: 'PUT',
      body: jsonEncode(<String, Object?>{
        'baseRevision': baseRevision,
        'schemaVersion': schemaVersion,
        'payload': payload,
        'deviceId': deviceId,
      }),
    );
    if (response.statusCode == 409) {
      final json = _decodeObject(response.body);
      final remote = json['remote'];
      if (remote is Map) {
        throw AgendaRevisionConflict(
          AgendaRemoteState.fromJson(Map<String, dynamic>.from(remote)),
        );
      }
    }
    return AgendaRemoteState.fromJson(_decodeObject(response.body));
  }

  Future<ServiceHttpResponse> _authorizedRequest({
    required String method,
    String? body,
  }) async {
    Future<ServiceHttpResponse> send(AndroidDeviceSession session) =>
        _transport.send(
          ServiceHttpRequest(
            method: method,
            uri: _config.endpoint('/api/agenda/account/state'),
            headers: <String, String>{
              'Accept': 'application/json',
              if (body != null)
                'Content-Type': 'application/json; charset=utf-8',
              'Authorization': 'Device ${session.deviceToken}',
              'X-Agenda-Device-Id': session.deviceId,
            },
            body: body,
            timeout: const Duration(seconds: 20),
          ),
        );

    var session = await _sessionProvider(forceRefresh: false);
    var response = await send(session);
    if (response.statusCode == 401) {
      session = await _sessionProvider(forceRefresh: true);
      response = await send(session);
    }
    if (response.statusCode == 402) {
      final entitlement = _entitlementFromResponse(response, config: _config);
      await _onEntitlement(entitlement);
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage:
            'Seu período de teste terminou. Regularize para continuar.',
      );
    }
    if (!response.isSuccess && response.statusCode != 409) {
      throw AgendaApiException.fromResponse(response);
    }
    if (response.isSuccess) {
      final json = _decodeObject(response.body);
      if (json['entitlement'] != null) {
        await _onEntitlement(
          AndroidEntitlement.fromJson(
            json['entitlement'],
            receivedAt: DateTime.now().toUtc(),
            fallbackPaymentUrl: _config.paymentUrl,
            fallbackSupportUrl: _config.supportUrl,
          ),
        );
      }
    }
    return response;
  }
}

Object _exceptionFromResponse(
  ServiceHttpResponse response, {
  required DateTime receivedAt,
}) {
  if (response.statusCode == 402) {
    final json = _safeDecodeObject(response.body);
    final entitlement = AndroidEntitlement.fromJson(
      json['entitlement'],
      receivedAt: receivedAt,
    );
    final apiError = AgendaApiException.fromResponse(
      response,
      fallbackMessage:
          'Seu período de teste terminou. Regularize para continuar.',
    );
    return AndroidEntitlementException(entitlement, apiError.message);
  }
  return AgendaApiException.fromResponse(response);
}

AndroidEntitlement _entitlementFromResponse(
  ServiceHttpResponse response, {
  required AndroidBuildConfig config,
}) {
  final json = _safeDecodeObject(response.body);
  return AndroidEntitlement.fromJson(
    json['entitlement'],
    receivedAt: DateTime.now().toUtc(),
    fallbackPaymentUrl: config.paymentUrl,
    fallbackSupportUrl: config.supportUrl,
  );
}

Map<String, dynamic> _decodeObject(String source) {
  final decoded = jsonDecode(source);
  if (decoded is! Map) {
    throw const AgendaApiException(
      'invalid_response',
      'O servidor retornou uma resposta inválida.',
    );
  }
  return Map<String, dynamic>.from(decoded);
}

Map<String, dynamic> _safeDecodeObject(String source) {
  try {
    return _decodeObject(source);
  } on Object {
    return <String, dynamic>{};
  }
}

Map<String, dynamic> _asObject(Object? value) =>
    value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};

String _string(Object? value) => value?.toString().trim() ?? '';

String _firstString(
  Map<String, dynamic> json,
  List<String> keys, {
  String fallback = '',
}) {
  for (final key in keys) {
    final value = _string(json[key]);
    if (value.isNotEmpty) return value;
  }
  return fallback;
}

int _integer(Object? value, {int fallback = 0}) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse(_string(value)) ?? fallback;
}

DateTime? _date(Object? value) {
  final parsed = DateTime.tryParse(_string(value));
  return parsed?.toUtc();
}
