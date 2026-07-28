import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import 'default_http_transport.dart';
import 'http_transport.dart';

const String defaultAgendaLivreApiBase = 'https://app.minhaagendalivre.com.br';

class AgendaRemoteConfig {
  const AgendaRemoteConfig({
    required this.supabaseUrl,
    required this.publishableKey,
    required this.syncUri,
  });

  final Uri supabaseUrl;
  final String publishableKey;
  final Uri syncUri;

  Map<String, Object?> toJson() => <String, Object?>{
    'supabaseUrl': supabaseUrl.toString(),
    'publishableKey': publishableKey,
    'syncUrl': syncUri.toString(),
  };

  static AgendaRemoteConfig fromJson(
    Map<String, dynamic> json, {
    required Uri apiBase,
  }) {
    final supabaseUrl = _requiredString(json, 'supabaseUrl');
    final publishableKey = _firstString(json, const <String>[
      'publishableKey',
      'anonKey',
      'supabaseAnonKey',
    ]);
    final syncUrl = _firstString(json, const <String>[
      'syncUrl',
      'stateUrl',
    ], fallback: '/api/agenda/account/state');
    if (publishableKey.isEmpty) {
      throw const AgendaApiException(
        'config_invalid',
        'A configuração de acesso não informou a chave pública.',
      );
    }
    return AgendaRemoteConfig(
      supabaseUrl: _httpsUri(supabaseUrl, field: 'supabaseUrl'),
      publishableKey: publishableKey,
      syncUri: _resolveUri(apiBase, syncUrl),
    );
  }
}

class AgendaRemoteConfigService {
  AgendaRemoteConfigService({
    required SharedPreferences preferences,
    HttpTransport? transport,
    Uri? apiBase,
  }) : _preferences = preferences,
       _transport = transport ?? createDefaultHttpTransport(),
       apiBase =
           apiBase ??
           Uri.parse(
             const String.fromEnvironment(
               'AGENDA_LIVRE_API_BASE',
               defaultValue: defaultAgendaLivreApiBase,
             ),
           );

  static const String cacheKey = 'agenda_livre.remote.config.v2';

  final SharedPreferences _preferences;
  final HttpTransport _transport;
  final Uri apiBase;
  AgendaRemoteConfig? _config;

  String get scopedCacheKey {
    final environment = base64Url
        .encode(utf8.encode(_normalizedApiBase(apiBase)))
        .replaceAll('=', '');
    return '$cacheKey.$environment';
  }

  Future<AgendaRemoteConfig> load({bool refresh = false}) async {
    if (!refresh && _config != null) return _config!;

    return loadLive();
  }

  /// Loads configuration from the configured API environment.
  ///
  /// Authentication entry points use this method so a config left by another
  /// build or environment can never select their identity provider.
  Future<AgendaRemoteConfig> loadLive() async {
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'GET',
        uri: _resolveUri(apiBase, '/api/agenda/account/config'),
        headers: const <String, String>{'Accept': 'application/json'},
        timeout: const Duration(seconds: 15),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(response);
    }
    final json = _decodeObject(response.body);
    final config = AgendaRemoteConfig.fromJson(json, apiBase: apiBase);
    final saved = await _preferences.setString(
      scopedCacheKey,
      jsonEncode(<String, Object?>{
        'apiBase': _normalizedApiBase(apiBase),
        'config': config.toJson(),
      }),
    );
    if (!saved) {
      throw const AgendaApiException(
        'config_storage_failed',
        'NÃ£o foi possÃ­vel guardar a configuraÃ§Ã£o deste ambiente.',
      );
    }
    _config = config;
    return config;
  }

  /// A cached config is accepted only while restoring a session whose issuer
  /// was previously verified, and only when the cached issuer still matches.
  Future<AgendaRemoteConfig> loadForRestore(AgendaAuthSession session) async {
    try {
      return await loadLive();
    } on Object catch (liveError, liveStackTrace) {
      if (!session.hasVerifiedIdentity) {
        Error.throwWithStackTrace(liveError, liveStackTrace);
      }
      await _preferences.reload();
      final cached = _preferences.getString(scopedCacheKey);
      if (cached == null || cached.trim().isEmpty) {
        Error.throwWithStackTrace(liveError, liveStackTrace);
      }
      try {
        final envelope = _decodeObject(cached);
        if (_string(envelope['apiBase']) != _normalizedApiBase(apiBase)) {
          Error.throwWithStackTrace(liveError, liveStackTrace);
        }
        final rawConfig = envelope['config'];
        if (rawConfig is! Map) {
          Error.throwWithStackTrace(liveError, liveStackTrace);
        }
        final config = AgendaRemoteConfig.fromJson(
          Map<String, dynamic>.from(rawConfig),
          apiBase: apiBase,
        );
        if (_normalizedIssuer(config.supabaseUrl.toString()) !=
            _normalizedIssuer(session.issuer)) {
          throw const AgendaApiException(
            'session_issuer_mismatch',
            'Esta sessÃ£o pertence a outro ambiente. Entre novamente.',
            statusCode: 409,
          );
        }
        _config = config;
        return config;
      } on AgendaApiException {
        rethrow;
      } on Object {
        Error.throwWithStackTrace(liveError, liveStackTrace);
      }
    }
  }
}

class AgendaAuthSession {
  const AgendaAuthSession({
    required this.userId,
    required this.email,
    required this.accessToken,
    required this.refreshToken,
    required this.expiresAt,
    this.issuer = '',
    this.identityVerifiedAt,
  });

  final String userId;
  final String email;
  final String accessToken;
  final String refreshToken;
  final DateTime expiresAt;
  final String issuer;
  final DateTime? identityVerifiedAt;

  bool get hasVerifiedIdentity =>
      issuer.trim().isNotEmpty && identityVerifiedAt != null;

  bool get expiresSoon => expiresAt.isBefore(
    DateTime.now().toUtc().add(const Duration(minutes: 2)),
  );

  Map<String, Object?> toJson() => <String, Object?>{
    'userId': userId,
    'email': email,
    'accessToken': accessToken,
    'refreshToken': refreshToken,
    'expiresAt': expiresAt.toUtc().toIso8601String(),
    'issuer': issuer,
    'identityVerifiedAt': identityVerifiedAt?.toUtc().toIso8601String(),
  };

  static AgendaAuthSession? fromJson(Map<String, dynamic> json) {
    final userId = _string(json['userId']);
    final accessToken = _string(json['accessToken']);
    final refreshToken = _string(json['refreshToken']);
    final expiresAt = DateTime.tryParse(_string(json['expiresAt']));
    if (userId.isEmpty ||
        accessToken.isEmpty ||
        refreshToken.isEmpty ||
        expiresAt == null) {
      return null;
    }
    return AgendaAuthSession(
      userId: userId,
      email: _string(json['email']),
      accessToken: accessToken,
      refreshToken: refreshToken,
      expiresAt: expiresAt.toUtc(),
      issuer: _string(json['issuer']),
      identityVerifiedAt: DateTime.tryParse(
        _string(json['identityVerifiedAt']),
      )?.toUtc(),
    );
  }
}

class AgendaSignUpResult {
  const AgendaSignUpResult({
    required this.emailConfirmationRequired,
    this.session,
  });

  final bool emailConfirmationRequired;
  final AgendaAuthSession? session;
}

class AgendaPasswordRecoverySession {
  const AgendaPasswordRecoverySession({
    required this.userId,
    required this.email,
    required this.accessToken,
    required this.expiresAt,
    required this.issuer,
  });

  final String userId;
  final String email;
  final String accessToken;
  final DateTime expiresAt;
  final String issuer;

  bool get isExpired => !expiresAt.isAfter(DateTime.now().toUtc());
}

class AgendaAuthService {
  AgendaAuthService({
    required SharedPreferences preferences,
    required AgendaRemoteConfigService configService,
    HttpTransport? transport,
    DateTime Function()? clock,
  }) : _preferences = preferences,
       _configService = configService,
       _transport = transport ?? createDefaultHttpTransport(),
       _clock = clock ?? DateTime.now;

  static const String sessionKey = 'agenda_livre.auth.session.v2';
  static const String legacySessionKey = 'agenda_livre.auth.session.v1';

  final SharedPreferences _preferences;
  final AgendaRemoteConfigService _configService;
  final HttpTransport _transport;
  final DateTime Function() _clock;

  AgendaAuthSession? _session;
  int _authOperationEpoch = 0;
  _AgendaRefreshFlight? _refreshInFlight;

  AgendaAuthSession? get session => _session;
  bool get isAuthenticated => _session != null;

  Future<AgendaAuthSession?> restoreSession() async {
    final operation = await _beginAuthOperation();
    if (!_isOperationCurrent(operation)) return null;
    // Version 1 could retain a development login in the browser. Never adopt
    // that identity silently: the user must authenticate once against v2.
    await _preferences.remove(legacySessionKey);
    if (!_isOperationCurrent(operation)) return null;
    final stored = operation.storedSession;
    if (stored == null || stored.trim().isEmpty) {
      _session = null;
      return null;
    }
    late final AgendaAuthSession? restored;
    try {
      restored = AgendaAuthSession.fromJson(_decodeObject(stored));
    } on Object {
      await _clearCapturedSession(operation);
      return null;
    }
    if (restored == null) {
      await _clearCapturedSession(operation);
      return null;
    }

    late final AgendaRemoteConfig config;
    try {
      config = await _configService.loadForRestore(restored);
    } on Object {
      // Without a live config (or a cached config tied to this previously
      // verified issuer), do not open or mutate the persisted identity.
      await _retireOperationIfStorageChanged(operation);
      return null;
    }

    var candidate = restored;
    if (candidate.expiresSoon) {
      try {
        return await _refreshCandidate(operation, candidate, config);
      } on AgendaApiException catch (error) {
        if (error.code == 'session_superseded') return null;
        final status = error.statusCode;
        if (status != null && status >= 400 && status < 500) {
          await _clearCapturedSession(operation);
          return null;
        }
        // Network unavailable: keep the persisted identity so its isolated
        // local cache can still be opened in offline mode.
      } on HttpTransportException {
        // Same offline behavior as above.
      }
    }
    try {
      return await _validateSessionIdentity(operation, candidate, config);
    } on AgendaApiException catch (error) {
      if (error.code == 'session_superseded') return null;
      final status = error.statusCode;
      if (status == 401 || status == 403) {
        try {
          return await _refreshCandidate(operation, candidate, config);
        } on AgendaApiException catch (refreshError) {
          if (refreshError.code == 'session_superseded') return null;
          final refreshStatus = refreshError.statusCode;
          if (refreshStatus == null ||
              (refreshStatus >= 400 && refreshStatus < 500)) {
            await _clearCapturedSession(operation);
            return null;
          }
          return _adoptVerifiedOfflineSession(operation, candidate, config);
        } on HttpTransportException {
          return _adoptVerifiedOfflineSession(operation, candidate, config);
        }
      }
      if (status != null && status >= 400 && status < 500) {
        await _clearCapturedSession(operation);
        return null;
      }
      // A valid persisted login remains usable with its isolated local cache
      // while the identity endpoint is temporarily unavailable.
      return _adoptVerifiedOfflineSession(operation, candidate, config);
    } on HttpTransportException {
      return _adoptVerifiedOfflineSession(operation, candidate, config);
    }
  }

  Future<AgendaAuthSession> signIn({
    required String email,
    required String password,
  }) async {
    final operation = await _beginAuthOperation();
    final submittedEmail = _normalizeEmail(email);
    final config = await _configService.loadLive();
    _ensureOperationCurrent(operation);
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: _authUri(config, '/token?grant_type=password'),
        headers: _authHeaders(config),
        body: jsonEncode(<String, Object?>{
          'email': submittedEmail,
          'password': password,
        }),
        timeout: const Duration(seconds: 20),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage: _friendlyAuthMessage(response.statusCode),
      );
    }
    _ensureOperationCurrent(operation);
    final session = _sessionFromAuthJson(
      _decodeObject(response.body),
      issuer: config.supabaseUrl.toString(),
      identityVerifiedAt: null,
    );
    return _verifyAndPersistNewSession(
      operation,
      session,
      config,
      expectedEmail: submittedEmail,
    );
  }

  Future<AgendaSignUpResult> signUp({
    required String name,
    required String businessName,
    required String email,
    required String password,
  }) async {
    final operation = await _beginAuthOperation();
    final submittedEmail = _normalizeEmail(email);
    final config = await _configService.loadLive();
    _ensureOperationCurrent(operation);
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: _authUri(config, '/signup'),
        headers: _authHeaders(config),
        body: jsonEncode(<String, Object?>{
          'email': submittedEmail,
          'password': password,
          'data': <String, Object?>{
            'full_name': name.trim(),
            'business_name': businessName.trim(),
          },
        }),
        timeout: const Duration(seconds: 20),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage: _friendlySignUpMessage(response.statusCode),
      );
    }
    _ensureOperationCurrent(operation);

    final json = _decodeObject(response.body);
    if (_string(json['access_token']).isEmpty) {
      return const AgendaSignUpResult(emailConfirmationRequired: true);
    }
    final session = _sessionFromAuthJson(
      json,
      issuer: config.supabaseUrl.toString(),
      identityVerifiedAt: null,
    );
    final verified = await _verifyAndPersistNewSession(
      operation,
      session,
      config,
      expectedEmail: submittedEmail,
    );
    return AgendaSignUpResult(
      emailConfirmationRequired: false,
      session: verified,
    );
  }

  Future<void> requestPasswordReset({
    required String email,
    required Uri redirectTo,
  }) async {
    final config = await _configService.loadLive();
    final callbackUri = passwordRecoveryRedirectUri(redirectTo);
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: _authUri(config, '/recover').replace(
          queryParameters: <String, String>{
            'redirect_to': callbackUri.toString(),
          },
        ),
        headers: _authHeaders(config),
        body: jsonEncode(<String, Object?>{'email': email.trim()}),
        timeout: const Duration(seconds: 20),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException(
        'password_recovery_request_failed',
        _friendlyPasswordRecoveryMessage(response.statusCode),
        statusCode: response.statusCode,
      );
    }
  }

  Future<void> resendSignUpConfirmation({
    required String email,
    required Uri redirectTo,
  }) async {
    final config = await _configService.loadLive();
    final cleanRedirect = sanitizePasswordRecoveryCallbackUri(redirectTo);
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: _authUri(config, '/resend').replace(
          queryParameters: <String, String>{
            'redirect_to': cleanRedirect.toString(),
          },
        ),
        headers: _authHeaders(config),
        body: jsonEncode(<String, Object?>{
          'type': 'signup',
          'email': email.trim(),
        }),
        timeout: const Duration(seconds: 20),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException(
        'confirmation_resend_failed',
        _friendlyConfirmationResendMessage(response.statusCode),
        statusCode: response.statusCode,
      );
    }
  }

  Future<AgendaPasswordRecoverySession> consumePasswordRecoveryCallback(
    Uri callbackUri,
  ) async {
    final callback = _AgendaAuthCallback.fromUri(callbackUri);
    if (!callback.isPasswordRecovery) {
      throw const AgendaApiException(
        'password_recovery_callback_missing',
        'Abra novamente o link de recuperação enviado para seu e-mail.',
      );
    }
    if (callback.errorCode.isNotEmpty || callback.errorDescription.isNotEmpty) {
      throw AgendaApiException(
        callback.errorCode.isEmpty
            ? 'password_recovery_callback_failed'
            : callback.errorCode,
        _friendlyPasswordRecoveryCallbackError(callback.errorCode),
        statusCode: 401,
      );
    }

    final config = await _configService.loadLive();
    var accessToken = callback.accessToken;
    var expiresAt = callback.expiresAt(_clock().toUtc());
    if (callback.tokenHash.isNotEmpty) {
      final response = await _transport.send(
        ServiceHttpRequest(
          method: 'POST',
          uri: _authUri(config, '/verify'),
          headers: _authHeaders(config),
          body: jsonEncode(<String, Object?>{
            'type': 'recovery',
            'token_hash': callback.tokenHash,
          }),
          timeout: const Duration(seconds: 20),
        ),
      );
      if (!response.isSuccess) {
        throw AgendaApiException(
          'password_recovery_verification_failed',
          _friendlyPasswordRecoveryCallbackError(''),
          statusCode: response.statusCode,
        );
      }
      final verified = _decodeObject(response.body);
      accessToken = _string(verified['access_token']);
      expiresAt = _authExpiry(verified, _clock().toUtc());
    }

    if (accessToken.isEmpty) {
      final message = callback.authorizationCode.isNotEmpty
          ? 'Este link não pode ser concluído neste navegador. Solicite um novo link.'
          : 'O link de recuperação está incompleto. Solicite um novo link.';
      throw AgendaApiException('password_recovery_token_missing', message);
    }
    if (!expiresAt.isAfter(_clock().toUtc())) {
      throw const AgendaApiException(
        'password_recovery_expired',
        'Este link de recuperação expirou. Solicite um novo link.',
        statusCode: 401,
      );
    }

    late final _AgendaAuthIdentity identity;
    try {
      identity = await _identityForAccessToken(config, accessToken);
    } on AgendaApiException catch (error) {
      if (error.statusCode == 401 || error.statusCode == 403) {
        throw const AgendaApiException(
          'password_recovery_expired',
          'Este link de recuperação expirou. Solicite um novo link.',
          statusCode: 401,
        );
      }
      rethrow;
    }
    return AgendaPasswordRecoverySession(
      userId: identity.userId,
      email: identity.email,
      accessToken: accessToken,
      expiresAt: expiresAt,
      issuer: config.supabaseUrl.toString().replaceFirst(RegExp(r'/+$'), ''),
    );
  }

  Future<void> updateRecoveredPassword({
    required AgendaPasswordRecoverySession recovery,
    required String password,
  }) async {
    if (password.length < 6) {
      throw const AgendaApiException(
        'password_too_short',
        'Use uma senha com pelo menos 6 caracteres.',
      );
    }
    final config = await _configService.loadLive();
    final expectedIssuer = config.supabaseUrl.toString().replaceFirst(
      RegExp(r'/+$'),
      '',
    );
    if (recovery.issuer != expectedIssuer || recovery.isExpired) {
      throw const AgendaApiException(
        'password_recovery_expired',
        'Este link de recuperação expirou. Solicite um novo link.',
        statusCode: 401,
      );
    }
    late final _AgendaAuthIdentity identity;
    try {
      identity = await _identityForAccessToken(config, recovery.accessToken);
    } on AgendaApiException catch (error) {
      if (error.statusCode == 401 || error.statusCode == 403) {
        throw const AgendaApiException(
          'password_recovery_expired',
          'Este link de recuperação expirou. Solicite um novo link.',
          statusCode: 401,
        );
      }
      rethrow;
    }
    if (identity.userId != recovery.userId) {
      throw const AgendaApiException(
        'password_recovery_identity_mismatch',
        'Este link pertence a outra conta. Solicite um novo link.',
        statusCode: 409,
      );
    }

    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'PUT',
        uri: _authUri(config, '/user'),
        headers: <String, String>{
          ..._authHeaders(config),
          'Authorization': 'Bearer ${recovery.accessToken}',
        },
        body: jsonEncode(<String, Object?>{'password': password}),
        timeout: const Duration(seconds: 20),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException(
        'password_update_failed',
        _friendlyPasswordUpdateMessage(response.statusCode),
        statusCode: response.statusCode,
      );
    }

    try {
      await _transport.send(
        ServiceHttpRequest(
          method: 'POST',
          uri: _authUri(config, '/logout'),
          headers: <String, String>{
            ..._authHeaders(config),
            'Authorization': 'Bearer ${recovery.accessToken}',
          },
          timeout: const Duration(seconds: 12),
        ),
      );
    } on Object {
      // The password was already changed. Ending the recovery session remotely
      // is best effort; it is never persisted by this client.
    }
  }

  static bool isPasswordRecoveryCallback(Uri uri) =>
      _AgendaAuthCallback.fromUri(uri).isPasswordRecovery;

  static Uri passwordRecoveryRedirectUri(Uri uri) {
    final sanitized = sanitizePasswordRecoveryCallbackUri(uri);
    return sanitized.replace(
      queryParameters: <String, String>{
        ...sanitized.queryParameters,
        _AgendaAuthCallback.markerKey: _AgendaAuthCallback.recoveryType,
      },
    );
  }

  static Uri sanitizePasswordRecoveryCallbackUri(Uri uri) {
    final query = Map<String, String>.from(
      uri.queryParameters,
    )..removeWhere((key, _) => _AgendaAuthCallback.sensitiveKeys.contains(key));
    return Uri(
      scheme: uri.scheme,
      userInfo: uri.userInfo,
      host: uri.host,
      port: uri.hasPort ? uri.port : null,
      path: uri.path,
      queryParameters: query.isEmpty ? null : query,
    );
  }

  Future<String> accessToken({bool forceRefresh = false}) async {
    final current = _session;
    if (current == null) {
      throw const AgendaApiException(
        'unauthorized',
        'Entre novamente para continuar.',
        statusCode: 401,
      );
    }
    if (forceRefresh || current.expiresSoon) {
      return (await refreshSession()).accessToken;
    }
    return current.accessToken;
  }

  Future<AgendaAuthSession> refreshSession() {
    final current = _session;
    if (current == null || current.refreshToken.isEmpty) {
      return Future<AgendaAuthSession>.error(
        const AgendaApiException(
          'unauthorized',
          'Sua sessão expirou. Entre novamente.',
          statusCode: 401,
        ),
      );
    }

    final existing = _refreshInFlight;
    final currentFingerprint = _sessionValue(current);
    if (existing != null &&
        existing.epoch == _authOperationEpoch &&
        existing.sessionFingerprint == currentFingerprint) {
      return existing.future;
    }

    final epoch = ++_authOperationEpoch;
    late final Future<AgendaAuthSession> tracked;
    tracked = _runRefresh(epoch, current).whenComplete(() {
      if (identical(_refreshInFlight?.future, tracked)) {
        _refreshInFlight = null;
      }
    });
    _refreshInFlight = _AgendaRefreshFlight(
      epoch: epoch,
      sessionFingerprint: currentFingerprint,
      future: tracked,
    );
    return tracked;
  }

  Future<AgendaAuthSession> _runRefresh(
    int epoch,
    AgendaAuthSession current,
  ) async {
    final operation = await _captureAuthOperation(epoch);
    _ensureOperationCurrent(operation);
    if (operation.storedFingerprint !=
        _storedSessionFingerprint(_sessionValue(current))) {
      await _retireOperationIfStorageChanged(operation, force: true);
      throw _sessionSupersededException;
    }
    final config = await _configService.loadLive();
    _ensureOperationCurrent(operation);
    return _refreshCandidate(operation, current, config);
  }

  Future<AgendaAuthSession> _refreshCandidate(
    _AgendaAuthOperation operation,
    AgendaAuthSession current,
    AgendaRemoteConfig config,
  ) async {
    _ensureOperationCurrent(operation);
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'POST',
        uri: _authUri(config, '/token?grant_type=refresh_token'),
        headers: _authHeaders(config),
        body: jsonEncode(<String, Object?>{
          'refresh_token': current.refreshToken,
        }),
        timeout: const Duration(seconds: 20),
      ),
    );
    _ensureOperationCurrent(operation);
    if (!response.isSuccess) {
      final error = AgendaApiException.fromResponse(response);
      if (response.statusCode >= 400 && response.statusCode < 500) {
        await _clearCapturedSession(operation);
      }
      throw error;
    }
    final refreshed = _sessionFromAuthJson(
      _decodeObject(response.body),
      issuer: config.supabaseUrl.toString(),
      identityVerifiedAt: null,
      fallbackEmail: current.email,
      fallbackRefreshToken: current.refreshToken,
    );
    return _verifyAndPersistNewSession(
      operation,
      refreshed,
      config,
      expectedUserId: current.userId,
    );
  }

  Future<void> signOut() async {
    final current = _session;
    final operation = await _beginAuthOperation();
    await _clearCapturedSession(operation);
    try {
      if (current != null) {
        final config = await _configService.load();
        await _transport.send(
          ServiceHttpRequest(
            method: 'POST',
            uri: _authUri(config, '/logout'),
            headers: <String, String>{
              ..._authHeaders(config),
              'Authorization': 'Bearer ${current.accessToken}',
            },
            timeout: const Duration(seconds: 12),
          ),
        );
      }
    } on Object {
      // Logging out locally must never be blocked by a network outage.
    }
  }

  Future<void> clearLocalSession() async {
    final operation = await _beginAuthOperation();
    await _clearCapturedSession(operation);
  }

  Future<AgendaAuthSession> _verifyAndPersistNewSession(
    _AgendaAuthOperation operation,
    AgendaAuthSession session,
    AgendaRemoteConfig config, {
    String? expectedUserId,
    String? expectedEmail,
  }) async {
    try {
      if (expectedUserId != null && session.userId != expectedUserId) {
        throw const AgendaApiException(
          'session_identity_mismatch',
          'A sessão retornada pertence a outra conta. Entre novamente.',
          statusCode: 409,
        );
      }
      return await _validateSessionIdentity(
        operation,
        session,
        config,
        expectedEmail: expectedEmail,
      );
    } on AgendaApiException catch (error) {
      if (error.code != 'session_superseded') {
        await _clearCapturedSession(operation);
      }
      rethrow;
    } on Object {
      await _clearCapturedSession(operation);
      rethrow;
    }
  }

  Future<AgendaAuthSession?> _adoptVerifiedOfflineSession(
    _AgendaAuthOperation operation,
    AgendaAuthSession candidate,
    AgendaRemoteConfig config,
  ) async {
    if (!candidate.hasVerifiedIdentity ||
        _normalizedIssuer(candidate.issuer) !=
            _normalizedIssuer(config.supabaseUrl.toString())) {
      return null;
    }
    try {
      await _ensureStorageUnchanged(operation);
    } on AgendaApiException {
      return null;
    }
    _session = candidate;
    return candidate;
  }

  Future<AgendaAuthSession> _validateSessionIdentity(
    _AgendaAuthOperation operation,
    AgendaAuthSession session,
    AgendaRemoteConfig config, {
    String? expectedEmail,
  }) async {
    _ensureOperationCurrent(operation);
    final expectedIssuer = _normalizedIssuer(config.supabaseUrl.toString());
    final storedIssuer = _normalizedIssuer(session.issuer);
    if (storedIssuer.isNotEmpty && storedIssuer != expectedIssuer) {
      throw const AgendaApiException(
        'session_issuer_mismatch',
        'Esta sessão pertence a outro ambiente. Entre novamente.',
        statusCode: 409,
      );
    }
    final identity = await _identityForAccessToken(config, session.accessToken);
    _ensureOperationCurrent(operation);
    final userId = identity.userId;
    if (userId != session.userId) {
      throw const AgendaApiException(
        'session_identity_mismatch',
        'A conta salva não corresponde à sessão atual. Entre novamente.',
        statusCode: 409,
      );
    }
    if (expectedEmail != null &&
        _normalizeEmail(identity.email) != _normalizeEmail(expectedEmail)) {
      throw const AgendaApiException(
        'session_identity_mismatch',
        'O e-mail validado pertence a outra conta. Entre novamente.',
        statusCode: 409,
      );
    }

    final verified = AgendaAuthSession(
      userId: userId,
      email: identity.email.isEmpty ? session.email : identity.email,
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      expiresAt: session.expiresAt,
      issuer: expectedIssuer,
      identityVerifiedAt: _clock().toUtc(),
    );
    await _commitSession(operation, verified);
    return verified;
  }

  Future<_AgendaAuthOperation> _beginAuthOperation() =>
      _captureAuthOperation(++_authOperationEpoch);

  Future<_AgendaAuthOperation> _captureAuthOperation(int epoch) async {
    await _preferences.reload();
    return _AgendaAuthOperation(
      epoch: epoch,
      storedSession: _preferences.getString(sessionKey),
    );
  }

  bool _isOperationCurrent(_AgendaAuthOperation operation) =>
      operation.epoch == _authOperationEpoch;

  void _ensureOperationCurrent(_AgendaAuthOperation operation) {
    if (_isOperationCurrent(operation)) return;
    throw _sessionSupersededException;
  }

  Future<void> _ensureStorageUnchanged(_AgendaAuthOperation operation) async {
    _ensureOperationCurrent(operation);
    await _preferences.reload();
    _ensureOperationCurrent(operation);
    if (_storedSessionFingerprint(_preferences.getString(sessionKey)) ==
        operation.storedFingerprint) {
      return;
    }
    _session = null;
    _authOperationEpoch++;
    throw _sessionSupersededException;
  }

  Future<void> _commitSession(
    _AgendaAuthOperation operation,
    AgendaAuthSession session,
  ) async {
    await _ensureStorageUnchanged(operation);
    final value = _sessionValue(session);
    final saved = await _preferences.setString(sessionKey, value);
    if (!saved) {
      throw const AgendaApiException(
        'session_storage_failed',
        'Não foi possível guardar a sessão neste navegador.',
      );
    }
    if (!_isOperationCurrent(operation)) {
      await _removeStoredSessionIfEqual(value);
      throw _sessionSupersededException;
    }
    await _preferences.reload();
    if (!_isOperationCurrent(operation) ||
        _storedSessionFingerprint(_preferences.getString(sessionKey)) !=
            _storedSessionFingerprint(value)) {
      _session = null;
      if (_isOperationCurrent(operation)) _authOperationEpoch++;
      await _removeStoredSessionIfEqual(value);
      throw _sessionSupersededException;
    }
    _session = session;
  }

  Future<void> _clearCapturedSession(_AgendaAuthOperation operation) async {
    if (!_isOperationCurrent(operation)) return;
    _session = null;
    await _preferences.reload();
    if (!_isOperationCurrent(operation)) return;
    final current = _storedSessionFingerprint(
      _preferences.getString(sessionKey),
    );
    if (current == operation.storedFingerprint) {
      await _preferences.remove(sessionKey);
      return;
    }
    // Another tab selected a different identity. Keep its storage untouched,
    // and retire every operation based on this instance's stale account.
    _authOperationEpoch++;
  }

  Future<void> _retireOperationIfStorageChanged(
    _AgendaAuthOperation operation, {
    bool force = false,
  }) async {
    if (!_isOperationCurrent(operation)) return;
    await _preferences.reload();
    if (!_isOperationCurrent(operation)) return;
    final changed =
        _storedSessionFingerprint(_preferences.getString(sessionKey)) !=
        operation.storedFingerprint;
    if (changed || force) {
      _session = null;
      _authOperationEpoch++;
    }
  }

  Future<void> _removeStoredSessionIfEqual(String value) async {
    await _preferences.reload();
    if (_storedSessionFingerprint(_preferences.getString(sessionKey)) ==
        _storedSessionFingerprint(value)) {
      await _preferences.remove(sessionKey);
    }
  }

  Future<_AgendaAuthIdentity> _identityForAccessToken(
    AgendaRemoteConfig config,
    String accessToken,
  ) async {
    final response = await _transport.send(
      ServiceHttpRequest(
        method: 'GET',
        uri: _authUri(config, '/user'),
        headers: <String, String>{
          ..._authHeaders(config),
          'Authorization': 'Bearer $accessToken',
        },
        timeout: const Duration(seconds: 15),
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(response);
    }
    late final Map<String, dynamic> user;
    try {
      user = _decodeObject(response.body);
    } on Object {
      throw const AgendaApiException(
        'auth_identity_invalid',
        'Não foi possível validar esta sessão. Entre novamente.',
        statusCode: 401,
      );
    }
    final userId = _string(user['id']);
    if (userId.isEmpty) {
      throw const AgendaApiException(
        'auth_identity_invalid',
        'Não foi possível validar esta sessão. Entre novamente.',
        statusCode: 401,
      );
    }
    return _AgendaAuthIdentity(userId: userId, email: _string(user['email']));
  }

  AgendaAuthSession _sessionFromAuthJson(
    Map<String, dynamic> json, {
    required String issuer,
    required DateTime? identityVerifiedAt,
    String fallbackEmail = '',
    String fallbackRefreshToken = '',
  }) {
    final rawUser = json['user'];
    final user = rawUser is Map
        ? Map<String, dynamic>.from(rawUser)
        : <String, dynamic>{};
    final userId = _firstString(user, const <String>['id']);
    final email = _firstString(user, const <String>[
      'email',
    ], fallback: fallbackEmail);
    final accessToken = _string(json['access_token']);
    final refreshToken = _string(json['refresh_token']).isEmpty
        ? fallbackRefreshToken
        : _string(json['refresh_token']);
    final expiresAtSeconds = _integer(json['expires_at']);
    final expiresInSeconds = _integer(json['expires_in'], fallback: 3600);
    final expiresAt = expiresAtSeconds > 0
        ? DateTime.fromMillisecondsSinceEpoch(
            expiresAtSeconds * 1000,
            isUtc: true,
          )
        : _clock().toUtc().add(Duration(seconds: expiresInSeconds));

    if (userId.isEmpty || accessToken.isEmpty || refreshToken.isEmpty) {
      throw const AgendaApiException(
        'auth_response_invalid',
        'O servidor retornou uma sessão inválida.',
      );
    }
    return AgendaAuthSession(
      userId: userId,
      email: email,
      accessToken: accessToken,
      refreshToken: refreshToken,
      expiresAt: expiresAt,
      issuer: issuer,
      identityVerifiedAt: identityVerifiedAt,
    );
  }

  static Map<String, String> _authHeaders(AgendaRemoteConfig config) =>
      <String, String>{
        'Accept': 'application/json',
        'Content-Type': 'application/json; charset=utf-8',
        'apikey': config.publishableKey,
      };

  static Uri _authUri(AgendaRemoteConfig config, String path) {
    final root = config.supabaseUrl.toString().replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$root/auth/v1$path');
  }
}

class _AgendaAuthOperation {
  const _AgendaAuthOperation({
    required this.epoch,
    required this.storedSession,
  });

  final int epoch;
  final String? storedSession;

  String get storedFingerprint => _storedSessionFingerprint(storedSession);
}

class _AgendaRefreshFlight {
  const _AgendaRefreshFlight({
    required this.epoch,
    required this.sessionFingerprint,
    required this.future,
  });

  final int epoch;
  final String sessionFingerprint;
  final Future<AgendaAuthSession> future;
}

const AgendaApiException _sessionSupersededException = AgendaApiException(
  'session_superseded',
  'Esta sessão foi substituída por outra operação de acesso.',
  statusCode: 409,
);

String _sessionValue(AgendaAuthSession session) => jsonEncode(session.toJson());

String _storedSessionFingerprint(String? value) =>
    value == null ? '<no-session>' : 'json:$value';

String _normalizeEmail(String value) => value.trim().toLowerCase();

String _normalizedIssuer(String value) =>
    value.trim().replaceFirst(RegExp(r'/+$'), '');

String _normalizedApiBase(Uri value) =>
    value.toString().trim().replaceFirst(RegExp(r'/+$'), '');

class _AgendaAuthCallback {
  const _AgendaAuthCallback(this.parameters);

  static const String markerKey = 'auth_callback';
  static const String recoveryType = 'recovery';
  static const Set<String> sensitiveKeys = <String>{
    markerKey,
    'type',
    'access_token',
    'refresh_token',
    'expires_at',
    'expires_in',
    'token_type',
    'token_hash',
    'code',
    'error',
    'error_code',
    'error_description',
  };

  final Map<String, String> parameters;

  factory _AgendaAuthCallback.fromUri(Uri uri) {
    final parameters = Map<String, String>.from(uri.queryParameters);
    final fragment = uri.fragment.trim();
    if (fragment.contains('=')) {
      try {
        parameters.addAll(Uri.splitQueryString(fragment));
      } on FormatException {
        // An unrelated route fragment is not an authentication callback.
      }
    }
    return _AgendaAuthCallback(parameters);
  }

  String get type => _string(parameters['type']).toLowerCase();
  String get marker => _string(parameters[markerKey]).toLowerCase();
  String get accessToken => _string(parameters['access_token']);
  String get tokenHash => _string(parameters['token_hash']);
  String get authorizationCode => _string(parameters['code']);
  String get errorCode =>
      _firstString(parameters, const <String>['error_code', 'error']);
  String get errorDescription => _string(parameters['error_description']);

  bool get isPasswordRecovery {
    final recognized = marker == recoveryType || type == recoveryType;
    final hasCallbackPayload =
        accessToken.isNotEmpty ||
        tokenHash.isNotEmpty ||
        authorizationCode.isNotEmpty ||
        errorCode.isNotEmpty ||
        errorDescription.isNotEmpty;
    return recognized && hasCallbackPayload;
  }

  DateTime expiresAt(DateTime now) {
    final absolute = _integer(parameters['expires_at']);
    if (absolute > 0) {
      return DateTime.fromMillisecondsSinceEpoch(absolute * 1000, isUtc: true);
    }
    final seconds = _integer(parameters['expires_in'], fallback: 3600);
    return now.add(Duration(seconds: seconds));
  }
}

class _AgendaAuthIdentity {
  const _AgendaAuthIdentity({required this.userId, required this.email});

  final String userId;
  final String email;
}

class AgendaTrialStatus {
  const AgendaTrialStatus({
    this.startedAt,
    this.endsAt,
    this.active = false,
    this.daysRemaining = 0,
  });

  final DateTime? startedAt;
  final DateTime? endsAt;
  final bool active;
  final int daysRemaining;

  static AgendaTrialStatus fromJson(Object? value) {
    if (value is! Map) return const AgendaTrialStatus();
    final json = Map<String, dynamic>.from(value);
    return AgendaTrialStatus(
      startedAt: DateTime.tryParse(_string(json['startedAt'])),
      endsAt: DateTime.tryParse(_string(json['endsAt'])),
      active: json['active'] == true,
      daysRemaining: _integer(json['daysRemaining']),
    );
  }
}

class AgendaEntitlement {
  const AgendaEntitlement({
    this.status = 'pending_activation',
    this.canUse = false,
    this.trialStartedAt,
    this.trialEndsAt,
    this.daysRemaining = 0,
    this.currentPeriodEndsAt,
    this.graceEndsAt,
    this.paymentUrl = '',
    this.supportUrl = '',
  });

  final String status;
  final bool canUse;
  final DateTime? trialStartedAt;
  final DateTime? trialEndsAt;
  final int daysRemaining;
  final DateTime? currentPeriodEndsAt;
  final DateTime? graceEndsAt;
  final String paymentUrl;
  final String supportUrl;

  bool get isPaid => status == 'active' || status == 'past_due';
  bool get needsRenewal => !canUse;

  static AgendaEntitlement fromJson(Object? value) {
    if (value is! Map) return const AgendaEntitlement();
    final json = Map<String, dynamic>.from(value);
    return AgendaEntitlement(
      status: _string(json['status']).trim().toLowerCase(),
      canUse: json['canUse'] == true,
      trialStartedAt: DateTime.tryParse(_string(json['trialStartedAt'])),
      trialEndsAt: DateTime.tryParse(_string(json['trialEndsAt'])),
      daysRemaining: _integer(json['daysRemaining']),
      currentPeriodEndsAt: DateTime.tryParse(
        _string(json['currentPeriodEndsAt']),
      ),
      graceEndsAt: DateTime.tryParse(_string(json['graceEndsAt'])),
      paymentUrl: _string(json['paymentUrl']),
      supportUrl: _string(json['supportUrl']),
    );
  }
}

class AgendaBillingCard {
  const AgendaBillingCard({
    required this.brand,
    required this.last4,
    required this.expMonth,
    required this.expYear,
  });

  final String brand;
  final String last4;
  final int expMonth;
  final int expYear;

  String get displayBrand => switch (brand.toLowerCase()) {
    'visa' => 'Visa',
    'mastercard' => 'Mastercard',
    'amex' => 'American Express',
    'elo' => 'Elo',
    _ => 'Cartão',
  };
}

class AgendaRemoteState {
  const AgendaRemoteState({
    required this.exists,
    required this.revision,
    required this.schemaVersion,
    required this.payload,
    required this.updatedAt,
    required this.trial,
    this.entitlement = const AgendaEntitlement(),
  });

  final bool exists;
  final int revision;
  final int schemaVersion;
  final Map<String, dynamic>? payload;
  final DateTime? updatedAt;
  final AgendaTrialStatus trial;
  final AgendaEntitlement entitlement;

  static AgendaRemoteState fromJson(Map<String, dynamic> json) {
    final rawPayload = json['payload'];
    return AgendaRemoteState(
      exists: json['exists'] == true,
      revision: _integer(json['revision']),
      schemaVersion: _integer(json['schemaVersion'], fallback: 1),
      payload: rawPayload is Map ? Map<String, dynamic>.from(rawPayload) : null,
      updatedAt: DateTime.tryParse(_string(json['updatedAt'])),
      trial: AgendaTrialStatus.fromJson(json['trial']),
      entitlement: AgendaEntitlement.fromJson(json['entitlement']),
    );
  }
}

abstract interface class AgendaAccountStateClient {
  Future<AgendaRemoteState> fetchState();

  Future<AgendaRemoteState> saveState({
    required int baseRevision,
    required int schemaVersion,
    required Map<String, dynamic> payload,
    required String deviceId,
  });
}

class AgendaAccountApi implements AgendaAccountStateClient {
  AgendaAccountApi({
    required AgendaRemoteConfigService configService,
    required AgendaAuthService authService,
    HttpTransport? transport,
    bool Function()? isSessionCurrent,
  }) : _configService = configService,
       _authService = authService,
       _transport = transport ?? createDefaultHttpTransport(),
       _isSessionCurrent = isSessionCurrent;

  final AgendaRemoteConfigService _configService;
  final AgendaAuthService _authService;
  final HttpTransport _transport;
  final bool Function()? _isSessionCurrent;

  @override
  Future<AgendaRemoteState> fetchState() async {
    final config = await _configService.load();
    final response = await _authorizedRequest(config, method: 'GET');
    if (!response.isSuccess) throw AgendaApiException.fromResponse(response);
    return AgendaRemoteState.fromJson(_decodeObject(response.body));
  }

  @override
  Future<AgendaRemoteState> saveState({
    required int baseRevision,
    required int schemaVersion,
    required Map<String, dynamic> payload,
    required String deviceId,
  }) async {
    final config = await _configService.load();
    final response = await _authorizedRequest(
      config,
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
      final rawRemote = json['remote'];
      if (rawRemote is Map) {
        throw AgendaRevisionConflict(
          AgendaRemoteState.fromJson(Map<String, dynamic>.from(rawRemote)),
        );
      }
    }
    if (!response.isSuccess) throw AgendaApiException.fromResponse(response);
    return AgendaRemoteState.fromJson(_decodeObject(response.body));
  }

  Future<Uri> createSubscriptionCheckout({
    required String plan,
    required String idempotencyKey,
  }) async {
    final normalizedPlan = plan.trim().toLowerCase();
    if (normalizedPlan != 'mensal' && normalizedPlan != 'anual') {
      throw const AgendaApiException(
        'invalid_checkout_plan',
        'Escolha o plano mensal ou anual.',
        statusCode: 400,
      );
    }
    final config = await _configService.load();
    final response = await _authorizedRequest(
      config,
      method: 'POST',
      uri: _resolveUri(_configService.apiBase, '/api/agenda/android/checkout'),
      body: jsonEncode(<String, Object?>{
        'plan': normalizedPlan,
        'idempotencyKey': idempotencyKey,
      }),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage: 'Não foi possível abrir o pagamento seguro agora.',
      );
    }
    final rawCheckout = _decodeObject(response.body)['checkout'];
    final checkout = rawCheckout is Map
        ? Map<String, dynamic>.from(rawCheckout)
        : <String, dynamic>{};
    final url = _firstString(checkout, const <String>['url']);
    final uri = Uri.tryParse(url);
    if (uri == null || uri.scheme != 'https' || uri.host.isEmpty) {
      throw const AgendaApiException(
        'checkout_response_invalid',
        'O servidor não retornou um link seguro do Stripe.',
      );
    }
    return uri;
  }

  Future<Uri> createSubscriptionPortal() async {
    final config = await _configService.load();
    final response = await _authorizedRequest(
      config,
      method: 'POST',
      uri: _resolveUri(
        _configService.apiBase,
        '/api/agenda/subscriptions/portal',
      ),
      body: '{}',
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage: 'Não foi possível abrir sua assinatura agora.',
      );
    }
    final rawPortal = _decodeObject(response.body)['portal'];
    final portal = rawPortal is Map
        ? Map<String, dynamic>.from(rawPortal)
        : <String, dynamic>{};
    final uri = Uri.tryParse(_firstString(portal, const <String>['url']));
    if (uri == null || uri.scheme != 'https' || uri.host.isEmpty) {
      throw const AgendaApiException(
        'portal_response_invalid',
        'O servidor não retornou um link seguro da Stripe.',
      );
    }
    return uri;
  }

  Future<AgendaBillingCard?> getSubscriptionCardSummary() async {
    final config = await _configService.load();
    final response = await _authorizedRequest(
      config,
      method: 'GET',
      uri: _resolveUri(
        _configService.apiBase,
        '/api/agenda/subscriptions/summary',
      ),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage: 'Não foi possível consultar o cartão salvo agora.',
      );
    }
    final rawCard = _decodeObject(response.body)['card'];
    if (rawCard is! Map) return null;
    final card = Map<String, dynamic>.from(rawCard);
    final last4 = _string(card['last4']);
    if (!RegExp(r'^\d{4}$').hasMatch(last4)) return null;
    return AgendaBillingCard(
      brand: _string(card['brand']),
      last4: last4,
      expMonth: _integer(card['expMonth']),
      expYear: _integer(card['expYear']),
    );
  }

  Future<AgendaEntitlement> claimSubscription(String sessionId) async {
    final config = await _configService.load();
    final response = await _authorizedRequest(
      config,
      method: 'POST',
      uri: _resolveUri(
        _configService.apiBase,
        '/api/agenda/subscriptions/claim',
      ),
      body: jsonEncode(<String, String>{'sessionId': sessionId}),
    );
    if (!response.isSuccess) {
      throw AgendaApiException.fromResponse(
        response,
        fallbackMessage: 'Não foi possível ativar sua assinatura agora.',
      );
    }
    return AgendaEntitlement.fromJson(
      _decodeObject(response.body)['entitlement'],
    );
  }

  Future<ServiceHttpResponse> _authorizedRequest(
    AgendaRemoteConfig config, {
    required String method,
    Uri? uri,
    String? body,
  }) async {
    Future<ServiceHttpResponse> send(String token) => _transport.send(
      ServiceHttpRequest(
        method: method,
        uri: uri ?? config.syncUri,
        headers: <String, String>{
          'Accept': 'application/json',
          if (body != null) 'Content-Type': 'application/json; charset=utf-8',
          'Authorization': 'Bearer $token',
        },
        body: body,
        timeout: const Duration(seconds: 20),
      ),
    );

    _ensureSessionCurrent();
    final accessToken = await _authService.accessToken();
    _ensureSessionCurrent();
    var response = await send(accessToken);
    if (response.statusCode == 401) {
      // A request from a retired account must never refresh with the token of
      // the account that replaced it. Besides avoiding a stale 401 logout,
      // this prevents an old payload from being retried under another user.
      _ensureSessionCurrent();
      final refreshedToken = await _authService.accessToken(forceRefresh: true);
      _ensureSessionCurrent();
      response = await send(refreshedToken);
    }
    return response;
  }

  void _ensureSessionCurrent() {
    final isCurrent = _isSessionCurrent;
    if (isCurrent == null || isCurrent()) return;
    throw const AgendaApiException(
      'session_superseded',
      'Esta sessão não está mais ativa neste dispositivo.',
    );
  }
}

class AgendaRevisionConflict implements Exception {
  const AgendaRevisionConflict(this.remote);

  final AgendaRemoteState remote;
}

class AgendaApiException implements Exception {
  const AgendaApiException(this.code, this.message, {this.statusCode});

  final String code;
  final String message;
  final int? statusCode;

  bool get isUnauthorized => statusCode == 401 || code == 'unauthorized';

  static AgendaApiException fromResponse(
    ServiceHttpResponse response, {
    String? fallbackMessage,
  }) {
    var code = response.statusCode == 401 ? 'unauthorized' : 'request_failed';
    var message = fallbackMessage ?? 'Não foi possível concluir a operação.';
    try {
      final json = _decodeObject(response.body);
      final rawError = json['error'];
      if (rawError is Map) {
        final error = Map<String, dynamic>.from(rawError);
        code = _string(error['code']).isEmpty ? code : _string(error['code']);
        message = _string(error['message']).isEmpty
            ? message
            : _string(error['message']);
      } else {
        final responseCode = _string(json['code']);
        final responseMessage = _firstString(json, const <String>[
          'message',
          'msg',
          'error_description',
        ]);
        if (responseCode.isNotEmpty) code = responseCode;
        if (responseMessage.isNotEmpty) message = responseMessage;
      }
    } on Object {
      // Keep the friendly fallback.
    }
    return AgendaApiException(code, message, statusCode: response.statusCode);
  }

  @override
  String toString() => message;
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

String _requiredString(Map<String, dynamic> json, String key) {
  final value = _string(json[key]);
  if (value.isEmpty) {
    throw AgendaApiException(
      'config_invalid',
      'A configuração de acesso não informou $key.',
    );
  }
  return value;
}

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

String _string(Object? value) => value?.toString().trim() ?? '';

int _integer(Object? value, {int fallback = 0}) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse(_string(value)) ?? fallback;
}

Uri _httpsUri(String value, {required String field}) {
  final uri = Uri.tryParse(value);
  if (uri == null ||
      !uri.hasScheme ||
      uri.host.isEmpty ||
      uri.scheme != 'https') {
    throw AgendaApiException(
      'config_invalid',
      'A configuração $field é inválida.',
    );
  }
  return uri;
}

Uri _resolveUri(Uri base, String value) {
  final parsed = Uri.tryParse(value);
  if (parsed != null && parsed.hasScheme) return parsed;
  final root = base.toString().replaceFirst(RegExp(r'/+$'), '');
  final path = value.startsWith('/') ? value : '/$value';
  return Uri.parse('$root$path');
}

String _friendlyAuthMessage(int statusCode) => switch (statusCode) {
  400 || 401 => 'E-mail ou senha incorretos.',
  429 => 'Não foi possível entrar agora. Tente novamente.',
  _ => 'Não foi possível entrar agora. Tente novamente.',
};

String _friendlySignUpMessage(int statusCode) => switch (statusCode) {
  400 || 409 || 422 => 'Revise o e-mail e a senha informados.',
  _ => 'Não foi possível criar a conta agora. Tente novamente.',
};

String _friendlyPasswordRecoveryMessage(int statusCode) => switch (statusCode) {
  429 => 'Não foi possível enviar o link agora. Tente novamente.',
  _ => 'Não foi possível enviar o link agora. Tente novamente.',
};

String _friendlyConfirmationResendMessage(int statusCode) =>
    switch (statusCode) {
      429 => 'Não foi possível reenviar a confirmação agora.',
      404 || 405 => 'O reenvio de confirmação não está disponível agora.',
      _ => 'Não foi possível reenviar a confirmação agora.',
    };

String _friendlyPasswordUpdateMessage(int statusCode) => switch (statusCode) {
  401 || 403 => 'Este link de recuperação expirou. Solicite um novo link.',
  422 => 'A nova senha não atende aos requisitos de segurança.',
  429 => 'Não foi possível alterar a senha agora. Tente novamente.',
  _ => 'Não foi possível alterar a senha agora. Tente novamente.',
};

String _friendlyPasswordRecoveryCallbackError(String code) {
  final normalized = code.toLowerCase();
  if (normalized.contains('expired') || normalized.contains('otp_expired')) {
    return 'Este link de recuperação expirou. Solicite um novo link.';
  }
  return 'Este link de recuperação não é mais válido. Solicite um novo link.';
}

DateTime _authExpiry(Map<String, dynamic> json, DateTime now) {
  final absolute = _integer(json['expires_at']);
  if (absolute > 0) {
    return DateTime.fromMillisecondsSinceEpoch(absolute * 1000, isUtc: true);
  }
  return now.add(
    Duration(seconds: _integer(json['expires_in'], fallback: 3600)),
  );
}
