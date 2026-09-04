import 'dart:async';
import 'dart:convert';
import 'dart:math';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:url_launcher/url_launcher.dart';

import '../data/repositories/shared_preferences_agenda_repository.dart';
import '../data/repositories/syncing_agenda_repository.dart';
import '../domain/models/agenda_data.dart';
import '../domain/models/agenda_settings.dart';
import '../services/agenda_account_api.dart';
import '../services/agenda_livre_license_identity.dart';
import '../services/auth_browser_location.dart';
import '../services/default_http_transport.dart';
import '../services/http_transport.dart';
import '../services/instagram_service.dart';
import '../services/mercado_pago_service.dart';
import 'agenda_controller.dart';

Map<String, dynamic> _decodeSessionObject(String source) {
  final decoded = jsonDecode(source);
  return decoded is Map
      ? Map<String, dynamic>.from(decoded)
      : <String, dynamic>{};
}

@visibleForTesting
Set<int> agendaTrialReminderDaysForUser(String userId) {
  final available = <int>[7, 6, 5, 4, 3, 2, 1];
  final selected = <int>{};
  var seed = userId.codeUnits.fold<int>(
    0x45D9F3B,
    (value, unit) => ((value * 33) ^ unit) & 0x7fffffff,
  );
  while (selected.length < 3 && available.isNotEmpty) {
    seed = (seed * 1103515245 + 12345) & 0x7fffffff;
    selected.add(available.removeAt(seed % available.length));
  }
  return selected;
}

String _sessionString(Object? value) => value?.toString() ?? '';

abstract interface class AgendaWebSession implements Listenable {
  bool get initializing;
  bool get busy;
  String? get errorMessage;
  String? get successMessage;
  AgendaAuthSession? get authSession;
  AgendaController? get agendaController;
  bool get passwordRecoveryPending;
  String get passwordRecoveryEmail;
  String? get pendingConfirmationEmail;

  Future<void> initialize();

  Future<void> signIn({required String email, required String password});

  Future<void> signUp({
    required String name,
    required String businessName,
    required String email,
    required String password,
  });

  Future<void> requestPasswordReset({required String email});

  Future<void> updateRecoveredPassword({required String password});

  Future<void> resendSignUpConfirmation();

  void cancelPasswordRecovery();

  Future<void> signOut();

  void clearFeedback();
}

class AgendaCheckoutActivation {
  const AgendaCheckoutActivation({
    required this.sessionId,
    this.checking = true,
    this.complete = false,
    this.claimed = false,
    this.ready = false,
    this.plan = 'mensal',
    this.email = '',
    this.errorMessage,
  });

  final String sessionId;
  final bool checking;
  final bool complete;
  final bool claimed;
  final bool ready;
  final String plan;
  final String email;
  final String? errorMessage;

  AgendaCheckoutActivation copyWith({
    bool? checking,
    bool? complete,
    bool? claimed,
    bool? ready,
    String? plan,
    String? email,
    String? errorMessage,
    bool clearError = false,
  }) => AgendaCheckoutActivation(
    sessionId: sessionId,
    checking: checking ?? this.checking,
    complete: complete ?? this.complete,
    claimed: claimed ?? this.claimed,
    ready: ready ?? this.ready,
    plan: plan ?? this.plan,
    email: email ?? this.email,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}

class AgendaWebSessionController extends ChangeNotifier
    implements AgendaWebSession {
  AgendaWebSessionController({
    required SharedPreferences preferences,
    HttpTransport? transport,
    Uri? apiBase,
    Uri Function()? authCallbackUriProvider,
    void Function(Uri)? authCallbackUriReplacer,
    Future<bool> Function(Uri)? checkoutLauncher,
  }) : _preferences = preferences {
    final resolvedTransport = transport ?? createDefaultHttpTransport();
    _transport = resolvedTransport;
    _configService = AgendaRemoteConfigService(
      preferences: preferences,
      transport: resolvedTransport,
      apiBase: apiBase,
    );
    _authService = AgendaAuthService(
      preferences: preferences,
      configService: _configService,
      transport: resolvedTransport,
    );
    _authCallbackUriProvider =
        authCallbackUriProvider ?? (() => currentAgendaAuthBrowserUri);
    _authCallbackUriReplacer =
        authCallbackUriReplacer ?? replaceAgendaAuthBrowserUri;
    _checkoutLauncher =
        checkoutLauncher ??
        ((uri) => launchUrl(uri, webOnlyWindowName: '_self'));
  }

  static const String deviceIdKey = 'agenda_livre.device_id.v1';
  static const String subscriptionReminderKeyPrefix =
      'agenda_livre.subscription_reminder.v1';

  final SharedPreferences _preferences;
  late final HttpTransport _transport;
  late final AgendaRemoteConfigService _configService;
  late final AgendaAuthService _authService;
  late final Uri Function() _authCallbackUriProvider;
  late final void Function(Uri) _authCallbackUriReplacer;
  late final Future<bool> Function(Uri) _checkoutLauncher;
  final List<AgendaController> _retiredControllers = <AgendaController>[];
  int _sessionGeneration = 0;
  int _interactiveAuthGeneration = 0;
  Future<void>? _reconcileInFlight;
  String? _activeUserId;
  AgendaPasswordRecoverySession? _passwordRecovery;
  String? _pendingConfirmationEmail;
  String? _pendingSubscriptionPlan;
  AgendaAccountApi? _activeAccountApi;
  AgendaCheckoutActivation? _checkoutActivation;
  AgendaBillingCard? billingCard;
  bool billingCardLoaded = false;
  bool localRenewalPreview = false;
  bool _subscriptionReminderDismissed = false;
  bool _disposed = false;

  @override
  bool initializing = true;

  @override
  bool busy = false;

  @override
  String? errorMessage;

  @override
  String? successMessage;

  @override
  AgendaAuthSession? get authSession => _authService.session;

  @override
  AgendaController? agendaController;

  @override
  bool get passwordRecoveryPending => _passwordRecovery != null;

  @override
  String get passwordRecoveryEmail => _passwordRecovery?.email ?? '';

  @override
  String? get pendingConfirmationEmail => _pendingConfirmationEmail;

  AgendaCheckoutActivation? get checkoutActivation => _checkoutActivation;

  int get subscriptionReminderDaysRemaining =>
      agendaController?.trialDaysRemaining ?? 0;

  bool get subscriptionReminderExpired =>
      agendaController?.needsSubscriptionRenewal == true;

  bool get shouldShowSubscriptionReminder {
    final controller = agendaController;
    final session = authSession;
    if (controller == null || session == null) return false;
    if (localRenewalPreview) return true;
    // A cobrança vencida ou o teste encerrado é um bloqueio real. Ele não pode
    // ser dispensado nem silenciado pela preferência diária dos lembretes.
    if (controller.needsSubscriptionRenewal) return true;
    if (_subscriptionReminderDismissed) return false;
    final today = _dateStamp(DateTime.now());
    if (_preferences.getString(_subscriptionReminderKey(session.userId)) ==
        today) {
      return false;
    }
    if (!controller.trialActive) return false;
    return agendaTrialReminderDaysForUser(
      session.userId,
    ).contains(controller.trialDaysRemaining);
  }

  bool _initialized = false;

  @override
  Future<void> initialize() async {
    if (_initialized) return;
    _initialized = true;
    initializing = true;
    notifyListeners();
    try {
      final callbackUri = _authCallbackUriProvider();
      _captureRenewalPreview(callbackUri);
      _captureSubscriptionIntent(callbackUri);
      _captureCheckoutActivation(callbackUri);
      if (_checkoutActivation != null) {
        await _refreshCheckoutActivation();
      }
      if (AgendaAuthService.isPasswordRecoveryCallback(callbackUri)) {
        // Remove tokens from the address bar before the first network await.
        _authCallbackUriReplacer(
          AgendaAuthService.sanitizePasswordRecoveryCallbackUri(callbackUri),
        );
        _retireCurrentController();
        await _authService.clearLocalSession();
        _passwordRecovery = await _authService.consumePasswordRecoveryCallback(
          callbackUri,
        );
        successMessage = _passwordRecovery!.email.isEmpty
            ? 'Link confirmado. Crie uma nova senha para sua conta.'
            : 'Link confirmado para ${_passwordRecovery!.email}. Crie uma nova senha.';
        return;
      }
      final restored = await _authService.restoreSession();
      if (restored != null) {
        await _openAgenda(restored);
        await _openPendingSubscriptionCheckout();
      }
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      initializing = false;
      notifyListeners();
    }
  }

  @override
  Future<void> signIn({required String email, required String password}) async {
    if (busy) return;
    await _startInteractiveOperation();
    try {
      final session = await _authService.signIn(
        email: email,
        password: password,
      );
      _passwordRecovery = null;
      _pendingConfirmationEmail = null;
      await _openAgenda(session);
      await _openPendingSubscriptionCheckout();
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  @override
  Future<void> signUp({
    required String name,
    required String businessName,
    required String email,
    required String password,
  }) async {
    if (busy) return;
    await _startInteractiveOperation();
    try {
      final result = await _authService.signUp(
        name: name,
        businessName: businessName,
        email: email,
        password: password,
      );
      if (result.emailConfirmationRequired) {
        _pendingConfirmationEmail = email.trim();
        successMessage =
            'Conta criada! Enviamos um link de confirmação para ${email.trim()}. '
            'Confirme o e-mail e depois entre na sua conta.';
      } else if (result.session != null) {
        _pendingConfirmationEmail = null;
        await _openAgenda(result.session!);
        await _openPendingSubscriptionCheckout();
      }
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  @override
  Future<void> requestPasswordReset({required String email}) async {
    if (busy) return;
    await _startInteractiveOperation();
    try {
      await _authService.requestPasswordReset(
        email: email,
        redirectTo: _authCallbackUriProvider(),
      );
      successMessage =
          'Se este e-mail estiver cadastrado, enviaremos um link para redefinir sua senha.';
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  @override
  Future<void> updateRecoveredPassword({required String password}) async {
    if (busy) return;
    final recovery = _passwordRecovery;
    if (recovery == null) {
      errorMessage =
          'Abra novamente o link de recuperação enviado para seu e-mail.';
      notifyListeners();
      return;
    }
    await _startInteractiveOperation();
    try {
      await _authService.updateRecoveredPassword(
        recovery: recovery,
        password: password,
      );
      _passwordRecovery = null;
      successMessage = 'Senha alterada com sucesso. Entre com a nova senha.';
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  @override
  Future<void> resendSignUpConfirmation() async {
    if (busy) return;
    final email = _pendingConfirmationEmail?.trim() ?? '';
    if (email.isEmpty) return;
    await _startInteractiveOperation();
    try {
      await _authService.resendSignUpConfirmation(
        email: email,
        redirectTo: _authCallbackUriProvider(),
      );
      successMessage = 'Enviamos um novo link de confirmação para $email.';
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  @override
  void cancelPasswordRecovery() {
    if (_passwordRecovery == null) return;
    _passwordRecovery = null;
    errorMessage = null;
    successMessage = null;
    notifyListeners();
  }

  @override
  Future<void> signOut() async {
    if (busy) return;
    await _startInteractiveOperation();
    _retireCurrentController();
    _passwordRecovery = null;
    _pendingConfirmationEmail = null;
    notifyListeners();
    await _authService.signOut();
    busy = false;
    successMessage = 'Você saiu da sua conta com segurança.';
    notifyListeners();
  }

  Future<void> reconcilePersistedSession() {
    if (busy || initializing) return Future<void>.value();
    if (_passwordRecovery != null) return Future<void>.value();
    final current = _reconcileInFlight;
    if (current != null) return current;
    final interactiveGeneration = _interactiveAuthGeneration;
    late final Future<void> operation;
    operation = _reconcilePersistedSession(interactiveGeneration).whenComplete(
      () {
        if (identical(_reconcileInFlight, operation)) {
          _reconcileInFlight = null;
        }
      },
    );
    _reconcileInFlight = operation;
    return operation;
  }

  Future<void> _reconcilePersistedSession(int interactiveGeneration) async {
    await _preferences.reload();
    final persisted = _persistedAuthSession();
    final activeUserId = _activeUserId;

    // If another tab logged out or selected another account, stop exposing the
    // current controller before any remote validation can yield.
    if (activeUserId != null && persisted?.userId != activeUserId) {
      _retireCurrentController();
      notifyListeners();
    }

    try {
      final restored = await _authService.restoreSession();
      if (restored == null) {
        if (agendaController != null) _retireCurrentController();
        notifyListeners();
        return;
      }
      if (_activeUserId == restored.userId && agendaController != null) {
        errorMessage = null;
        notifyListeners();
        return;
      }
      if (interactiveGeneration != _interactiveAuthGeneration) return;
      await _openAgenda(restored);
    } on Object catch (error) {
      if (agendaController != null) _retireCurrentController();
      errorMessage = _messageFor(error);
      notifyListeners();
    }
  }

  Future<void> _handleUnauthorized({
    required int generation,
    required String userId,
  }) async {
    if (!_isSessionCurrent(generation: generation, userId: userId)) return;
    // Invalidate the binding before the first await. A new login cannot be
    // retired by the remainder of this stale callback.
    busy = true;
    _retireCurrentController();
    notifyListeners();
    await _authService.clearLocalSession();
    errorMessage = 'Sua sessão expirou. Entre novamente para continuar.';
    busy = false;
    notifyListeners();
  }

  Future<void> _openAgenda(AgendaAuthSession session) async {
    _passwordRecovery = null;
    _retireCurrentController();
    final generation = _sessionGeneration;
    _activeUserId = session.userId;
    final stateApi = AgendaAccountApi(
      configService: _configService,
      authService: _authService,
      transport: _transport,
      isSessionCurrent: () =>
          _isSessionCurrent(generation: generation, userId: session.userId),
    );
    _activeAccountApi = stateApi;
    final storageKey = storageKeyForUser(session.userId);
    final local = SharedPreferencesAgendaRepository(
      _preferences,
      storageKey: storageKey,
      seedFactory: () =>
          AgendaData(settings: AgendaSettings(onboardingCompleted: false)),
    );
    final deviceId = await _deviceId();
    final repository = SyncingAgendaRepository(
      local: local,
      remote: stateApi,
      preferences: _preferences,
      syncMetadataKey: '$storageKey.sync',
      deviceId: deviceId,
      onUnauthorized: () =>
          _handleUnauthorized(generation: generation, userId: session.userId),
    );
    if (!_isSessionCurrent(generation: generation, userId: session.userId)) {
      return;
    }
    late final AgendaController controller;
    final paymentLicense = AgendaLivreLicenseIdentity.forAccount(
      session.userId,
    );
    final integrationMachineCode =
        AgendaLivreLicenseIdentity.machineCodeForAccount(session.userId);
    final mercadoPagoService = MercadoPagoService(
      config: MercadoPagoServiceConfig(
        contextProvider: () {
          final settings = controller.data.settings;
          return MercadoPagoClientContext(
            licenseKey: paymentLicense,
            machineHash: deviceId,
            machineCode: deviceId.length <= 18
                ? deviceId
                : deviceId.substring(0, 18),
            appVersion: 'AgendaLivre.Flutter.Web',
            clientKind: 'web',
            localPlan: 'Agenda Livre Online',
            profile: <String, Object?>{
              'email': controller.accountEmail,
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
              'userId': session.userId,
              'clientKind': 'agenda-livre-web',
            },
          );
        },
        accessTokenProvider: () =>
            _isSessionCurrent(generation: generation, userId: session.userId)
            ? _authService.session?.accessToken
            : null,
      ),
      transport: _transport,
    );
    final instagramService = InstagramService(
      config: InstagramServiceConfig(
        contextProvider: () {
          final settings = controller.data.settings;
          return InstagramClientContext(
            licenseKey: paymentLicense,
            // Account-scoped so Web and mobile read the same cloud link.
            machineHash: integrationMachineCode,
            machineCode: integrationMachineCode,
            appVersion: 'AgendaLivre.Flutter.Web',
            profile: <String, Object?>{
              'email': controller.accountEmail,
              'ownerName': settings.accountFullName.trim(),
              'businessName': settings.businessName.trim(),
              'userId': session.userId,
              'clientKind': 'agenda-livre-web',
            },
          );
        },
        accessTokenProvider: () =>
            _isSessionCurrent(generation: generation, userId: session.userId)
            ? _authService.session?.accessToken
            : null,
      ),
      transport: _transport,
    );
    controller = AgendaController(
      repository,
      onLogout: signOut,
      instagramService: instagramService,
      mercadoPagoService: mercadoPagoService,
      accountApi: stateApi,
      deviceId: deviceId,
      authenticatedEmail: session.email,
      professionalId: session.professionalId,
      permissionScope: session.permissionScope,
    );
    if (session.isProfessionalAccount) {
      controller.page = AgendaPage.agenda;
    }
    agendaController = controller;
    errorMessage = null;
    successMessage = null;
    notifyListeners();
    await _claimCheckoutAfterAuthentication();
    unawaited(
      _loadBillingCardSummary(
        stateApi,
        generation: generation,
        userId: session.userId,
      ),
    );
  }

  Future<void> _loadBillingCardSummary(
    AgendaAccountApi api, {
    required int generation,
    required String userId,
  }) async {
    try {
      final card = await api.getSubscriptionCardSummary();
      if (_disposed ||
          !_isSessionCurrent(generation: generation, userId: userId)) {
        return;
      }
      billingCard = card;
      billingCardLoaded = true;
      notifyListeners();
    } on Object {
      if (_disposed ||
          !_isSessionCurrent(generation: generation, userId: userId)) {
        return;
      }
      billingCard = null;
      billingCardLoaded = true;
      notifyListeners();
    }
  }

  void _captureCheckoutActivation(Uri uri) {
    final checkout = (uri.queryParameters['checkout'] ?? '').toLowerCase();
    final localPreview =
        checkout == 'preview' &&
        (uri.host == 'localhost' ||
            uri.host == '127.0.0.1' ||
            uri.host == '127.0.0.2');
    if (checkout != 'sucesso' && !localPreview) {
      return;
    }
    if (localPreview) {
      _checkoutActivation = const AgendaCheckoutActivation(
        sessionId: 'cs_preview_local',
        checking: false,
        complete: true,
        plan: 'mensal',
        email: 'is***@exemplo.com',
      );
      return;
    }
    final sessionId = (uri.queryParameters['session_id'] ?? '').trim();
    if (!RegExp(r'^cs_[A-Za-z0-9_]+$').hasMatch(sessionId)) return;
    _checkoutActivation = AgendaCheckoutActivation(sessionId: sessionId);
  }

  void _captureRenewalPreview(Uri uri) {
    localRenewalPreview =
        uri.queryParameters['renewal'] == 'preview' &&
        (uri.host == 'localhost' ||
            uri.host == '127.0.0.1' ||
            uri.host == '127.0.0.2');
    if (!localRenewalPreview) return;
    billingCard = const AgendaBillingCard(
      brand: 'visa',
      last4: '4242',
      expMonth: 11,
      expYear: 2029,
    );
    billingCardLoaded = true;
  }

  Future<void> _refreshCheckoutActivation() async {
    final activation = _checkoutActivation;
    if (activation == null) return;
    if (activation.sessionId == 'cs_preview_local') return;
    try {
      final response = await _transport.send(
        ServiceHttpRequest(
          method: 'GET',
          uri: _configService.apiBase.resolve(
            '/api/agenda/subscriptions/status?session_id=${Uri.encodeQueryComponent(activation.sessionId)}',
          ),
          headers: const <String, String>{'Accept': 'application/json'},
          timeout: const Duration(seconds: 20),
        ),
      );
      if (!response.isSuccess) {
        throw AgendaApiException.fromResponse(response);
      }
      final raw = _decodeSessionObject(response.body)['checkout'];
      final checkout = raw is Map
          ? Map<String, dynamic>.from(raw)
          : <String, dynamic>{};
      _checkoutActivation = activation.copyWith(
        checking: false,
        complete: checkout['complete'] == true,
        claimed: checkout['claimed'] == true,
        plan: _sessionString(checkout['plan']),
        email: _sessionString(checkout['email']),
        clearError: true,
      );
    } on Object catch (error) {
      _checkoutActivation = activation.copyWith(
        checking: false,
        errorMessage: _messageFor(error),
      );
    }
    notifyListeners();
  }

  Future<void> _claimCheckoutAfterAuthentication() async {
    final activation = _checkoutActivation;
    final api = _activeAccountApi;
    if (activation == null || api == null || !activation.complete) return;
    try {
      await api.claimSubscription(activation.sessionId);
      _checkoutActivation = activation.copyWith(
        checking: false,
        claimed: true,
        ready: true,
        clearError: true,
      );
      await agendaController?.refreshRemoteIfSafe();
    } on Object catch (error) {
      _checkoutActivation = activation.copyWith(
        checking: false,
        errorMessage: _messageFor(error),
      );
    }
    notifyListeners();
  }

  Future<void> renewSubscription(String plan) async {
    final api = _activeAccountApi;
    final session = _authService.session;
    if (api == null || session == null) return;
    _startOperation();
    try {
      final checkout = await api.createSubscriptionCheckout(
        plan: plan,
        idempotencyKey:
            '${session.userId}-$plan-${DateTime.now().toUtc().microsecondsSinceEpoch}',
      );
      if (!await _checkoutLauncher(checkout)) {
        throw const AgendaApiException(
          'checkout_launch_failed',
          'Não foi possível abrir o Checkout da Stripe.',
        );
      }
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  void dismissSubscriptionReminder() {
    final session = authSession;
    _subscriptionReminderDismissed = true;
    if (session != null) {
      unawaited(
        _preferences.setString(
          _subscriptionReminderKey(session.userId),
          _dateStamp(DateTime.now()),
        ),
      );
    }
    notifyListeners();
  }

  Future<void> manageSubscription() async {
    final api = _activeAccountApi;
    if (api == null) return;
    _startOperation();
    try {
      final portal = await api.createSubscriptionPortal();
      if (!await _checkoutLauncher(portal)) {
        throw const AgendaApiException(
          'portal_launch_failed',
          'Não foi possível abrir sua assinatura na Stripe.',
        );
      }
    } on Object catch (error) {
      errorMessage = _messageFor(error);
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  void finishCheckoutActivation() {
    _checkoutActivation = null;
    final currentUri = _authCallbackUriProvider();
    final query = Map<String, String>.from(currentUri.queryParameters)
      ..remove('checkout')
      ..remove('session_id');
    _authCallbackUriReplacer(
      currentUri.replace(queryParameters: query.isEmpty ? null : query),
    );
    notifyListeners();
  }

  void _captureSubscriptionIntent(Uri uri) {
    final requested = (uri.queryParameters['subscribe'] ?? '')
        .trim()
        .toLowerCase();
    if (requested == 'mensal' || requested == 'anual') {
      _pendingSubscriptionPlan = requested;
    }
  }

  Future<void> _openPendingSubscriptionCheckout() async {
    final plan = _pendingSubscriptionPlan;
    final api = _activeAccountApi;
    final session = _authService.session;
    if (plan == null || api == null || session == null) return;

    _pendingSubscriptionPlan = null;
    final currentUri = _authCallbackUriProvider();
    final query = Map<String, String>.from(currentUri.queryParameters)
      ..remove('subscribe');
    _authCallbackUriReplacer(
      Uri(
        scheme: currentUri.scheme,
        userInfo: currentUri.userInfo,
        host: currentUri.host,
        port: currentUri.hasPort ? currentUri.port : null,
        path: currentUri.path,
        queryParameters: query.isEmpty ? null : query,
        fragment: currentUri.fragment.isEmpty ? null : currentUri.fragment,
      ),
    );

    final checkout = await api.createSubscriptionCheckout(
      plan: plan,
      idempotencyKey:
          '${session.userId}-$plan-${DateTime.now().toUtc().microsecondsSinceEpoch}',
    );
    if (!await _checkoutLauncher(checkout)) {
      throw const AgendaApiException(
        'checkout_launch_failed',
        'Não foi possível abrir o Checkout do Stripe.',
      );
    }
  }

  void _retireCurrentController() {
    _sessionGeneration++;
    _activeUserId = null;
    _activeAccountApi = null;
    billingCard = null;
    billingCardLoaded = false;
    _subscriptionReminderDismissed = false;
    final current = agendaController;
    if (current != null) _retiredControllers.add(current);
    agendaController = null;
  }

  bool _isSessionCurrent({required int generation, required String userId}) =>
      _sessionGeneration == generation &&
      _activeUserId == userId &&
      _authService.session?.userId == userId;

  String _subscriptionReminderKey(String userId) =>
      '$subscriptionReminderKeyPrefix.$userId';

  static String _dateStamp(DateTime value) =>
      '${value.year.toString().padLeft(4, '0')}-'
      '${value.month.toString().padLeft(2, '0')}-'
      '${value.day.toString().padLeft(2, '0')}';

  void _startOperation() {
    busy = true;
    errorMessage = null;
    successMessage = null;
    notifyListeners();
  }

  Future<void> _startInteractiveOperation() async {
    _interactiveAuthGeneration++;
    final reconcile = _reconcileInFlight;
    _startOperation();
    if (reconcile != null) {
      try {
        await reconcile;
      } on Object {
        // The explicit login/create/logout action takes precedence.
      }
    }
  }

  @override
  void clearFeedback() {
    if (errorMessage == null && successMessage == null) return;
    errorMessage = null;
    successMessage = null;
    notifyListeners();
  }

  Future<String> _deviceId() async {
    final current = _preferences.getString(deviceIdKey)?.trim() ?? '';
    if (current.isNotEmpty) return current;
    final random = Random.secure();
    final bytes = List<int>.generate(18, (_) => random.nextInt(256));
    final generated = 'web-${base64Url.encode(bytes).replaceAll('=', '')}';
    await _preferences.setString(deviceIdKey, generated);
    return generated;
  }

  AgendaAuthSession? _persistedAuthSession() {
    final source = _preferences.getString(AgendaAuthService.sessionKey);
    if (source == null || source.trim().isEmpty) return null;
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map) return null;
      return AgendaAuthSession.fromJson(Map<String, dynamic>.from(decoded));
    } on Object {
      return null;
    }
  }

  static String storageKeyForUser(String userId) {
    final encoded = base64Url.encode(utf8.encode(userId)).replaceAll('=', '');
    return 'agenda_livre.data.v2.$encoded';
  }

  static String _messageFor(Object error) {
    if (error is AgendaApiException) return error.message;
    if (error is HttpTransportException) {
      return 'Não foi possível acessar o serviço. Verifique sua conexão.';
    }
    return 'Não foi possível concluir agora. Tente novamente.';
  }

  @override
  void dispose() {
    _disposed = true;
    agendaController?.dispose();
    for (final controller in _retiredControllers) {
      controller.dispose();
    }
    super.dispose();
  }
}
