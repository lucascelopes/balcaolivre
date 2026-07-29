import 'dart:convert';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/web_agenda_root.dart';
import 'package:agenda_livre/app/web_agenda_session.dart';
import 'package:agenda_livre/features/onboarding/onboarding_page.dart';
import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../services/fake_http_transport.dart';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  testWidgets('AuthGate mostra login quando não existe sessão Web', (
    tester,
  ) async {
    final session = _FakeWebSession();

    await tester.pumpWidget(
      AgendaLivreWebRoot(session: session, autoInitialize: false),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('agenda-auth-card')), findsOneWidget);
    expect(find.text('Bem-vindo de volta'), findsOneWidget);
    expect(find.byKey(const Key('auth-name-field')), findsNothing);
  });

  testWidgets('AuthGate renderiza o layout desktop em 1280 por 720', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 720);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final session = _FakeWebSession();

    await tester.pumpWidget(
      AgendaLivreWebRoot(session: session, autoInitialize: false),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(
      find.text('Entre no Windows e\ncontinue de onde\nparou.'),
      findsOneWidget,
    );
    expect(find.byKey(const Key('agenda-auth-card')), findsOneWidget);
    expect(find.byKey(const Key('auth-submit')), findsOneWidget);
  });

  testWidgets('AuthGate funciona no celular em 390 por 844', (tester) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final session = _FakeWebSession();

    await tester.pumpWidget(
      AgendaLivreWebRoot(session: session, autoInitialize: false),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.byKey(const Key('agenda-auth-card')), findsOneWidget);
    expect(find.byKey(const Key('auth-submit')), findsOneWidget);

    await tester.tap(find.byKey(const Key('auth-mode-sign-up')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.byKey(const Key('auth-name-field')), findsOneWidget);
    expect(find.byKey(const Key('auth-business-field')), findsOneWidget);
  });

  testWidgets('cadastro envia nome e negócio e mostra confirmação de e-mail', (
    tester,
  ) async {
    final session = _FakeWebSession();
    await tester.pumpWidget(
      AgendaLivreWebRoot(session: session, autoInitialize: false),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('auth-mode-sign-up')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('auth-name-field')),
      'Nina Souza',
    );
    await tester.enterText(
      find.byKey(const Key('auth-business-field')),
      'Studio Nina',
    );
    await tester.enterText(
      find.byKey(const Key('auth-email-field')),
      'nina@example.com',
    );
    await tester.enterText(
      find.byKey(const Key('auth-password-field')),
      'segredo123',
    );
    await tester.ensureVisible(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();

    expect(session.lastName, 'Nina Souza');
    expect(session.lastBusinessName, 'Studio Nina');
    expect(session.lastEmail, 'nina@example.com');
    expect(find.byKey(const Key('auth-success-message')), findsOneWidget);
    expect(find.textContaining('link de confirmação'), findsOneWidget);
    await tester.ensureVisible(
      find.byKey(const Key('auth-resend-confirmation')),
    );
    await tester.tap(find.byKey(const Key('auth-resend-confirmation')));
    await tester.pumpAndSettle();
    expect(session.resendConfirmationCalls, 1);
    expect(find.textContaining('novo link de confirmação'), findsOneWidget);
  });

  testWidgets('recuperação solicita link sem sair do visual de autenticação', (
    tester,
  ) async {
    final session = _FakeWebSession();
    await tester.pumpWidget(
      AgendaLivreWebRoot(session: session, autoInitialize: false),
    );
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.byKey(const Key('auth-forgot-password')));
    await tester.tap(find.byKey(const Key('auth-forgot-password')));
    await tester.pumpAndSettle();

    expect(find.text('Recupere seu acesso'), findsOneWidget);
    expect(find.byKey(const Key('auth-password-field')), findsNothing);
    expect(find.byKey(const Key('auth-back-to-sign-in')), findsOneWidget);

    await tester.enterText(
      find.byKey(const Key('auth-email-field')),
      'nina@example.com',
    );
    await tester.ensureVisible(find.byKey(const Key('auth-submit')));
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();

    expect(session.lastEmail, 'nina@example.com');
    expect(find.textContaining('enviaremos um link'), findsOneWidget);
  });

  testWidgets('callback de recuperação exige confirmação e salva nova senha', (
    tester,
  ) async {
    final session = _FakeWebSession()
      ..passwordRecoveryPending = true
      ..passwordRecoveryEmail = 'nina@example.com';
    await tester.pumpWidget(
      AgendaLivreWebRoot(session: session, autoInitialize: false),
    );
    await tester.pumpAndSettle();

    expect(find.text('Crie uma nova senha'), findsOneWidget);
    expect(find.byKey(const Key('auth-recovery-identity')), findsOneWidget);
    expect(find.text('nina@example.com'), findsOneWidget);
    expect(
      find.byKey(const Key('auth-password-confirmation-field')),
      findsOneWidget,
    );

    await tester.enterText(
      find.byKey(const Key('auth-password-field')),
      'novaSenha123',
    );
    await tester.enterText(
      find.byKey(const Key('auth-password-confirmation-field')),
      'outraSenha123',
    );
    await tester.ensureVisible(find.byKey(const Key('auth-submit')));
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();
    expect(find.text('As senhas precisam ser iguais.'), findsOneWidget);
    expect(session.lastRecoveredPassword, isNull);

    await tester.enterText(
      find.byKey(const Key('auth-password-confirmation-field')),
      'novaSenha123',
    );
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();

    expect(session.lastRecoveredPassword, 'novaSenha123');
    expect(find.text('Bem-vindo de volta'), findsOneWidget);
    expect(find.textContaining('Senha alterada com sucesso'), findsOneWidget);
  });

  testWidgets(
    'conta nova recebe sete dias e abre o questionário antes da renovação',
    (tester) async {
      final now = DateTime.now().toUtc();
      SharedPreferences.setMockInitialValues(<String, Object>{
        AgendaAuthService.sessionKey: jsonEncode(<String, Object?>{
          'userId': 'new-user',
          'email': 'nova@example.com',
          'accessToken': 'new-access',
          'refreshToken': 'new-refresh',
          'expiresAt': now.add(const Duration(hours: 1)).toIso8601String(),
          'issuer': 'https://example.supabase.co',
          'identityVerifiedAt': now.toIso8601String(),
        }),
      });
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          return _jsonResponse(<String, Object?>{
            'id': 'new-user',
            'email': 'nova@example.com',
          });
        }
        if (request.uri.path.endsWith('/api/agenda/account/state')) {
          return _jsonResponse(<String, Object?>{
            'exists': false,
            'revision': 0,
            'schemaVersion': 1,
            'payload': null,
            'updatedAt': now.toIso8601String(),
            'trial': <String, Object?>{
              'active': true,
              'daysRemaining': 7,
              'startedAt': now.toIso8601String(),
              'endsAt': now.add(const Duration(days: 7)).toIso8601String(),
            },
            'entitlement': <String, Object?>{
              'status': 'trialing',
              'canUse': true,
              'daysRemaining': 7,
              'trialStartedAt': now.toIso8601String(),
              'trialEndsAt': now.add(const Duration(days: 7)).toIso8601String(),
            },
          });
        }
        if (request.uri.path.endsWith('/api/agenda/subscriptions/summary')) {
          return _jsonResponse(<String, Object?>{'ok': true, 'card': null});
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
      );
      addTearDown(session.dispose);

      await tester.pumpWidget(AgendaLivreWebRoot(session: session));
      await tester.pumpAndSettle();

      expect(find.byType(OnboardingPage), findsOneWidget);
      expect(find.text('Renove para continuar com sua agenda.'), findsNothing);
      expect(session.agendaController?.trialStatusLabel, contains('7 dias'));
      expect(
        find.byKey(const Key('onboarding-mobile-illustration')),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
    },
  );
}

ServiceHttpResponse _jsonResponse(Map<String, Object?> body) =>
    ServiceHttpResponse(
      statusCode: 200,
      body: jsonEncode(body),
      headers: const <String, String>{'content-type': 'application/json'},
    );

class _FakeWebSession extends ChangeNotifier implements AgendaWebSession {
  @override
  bool initializing = false;

  @override
  bool busy = false;

  @override
  String? errorMessage;

  @override
  String? successMessage;

  @override
  AgendaAuthSession? authSession;

  @override
  AgendaController? agendaController;

  @override
  bool passwordRecoveryPending = false;

  @override
  String passwordRecoveryEmail = '';

  @override
  String? pendingConfirmationEmail;

  String? lastName;
  String? lastBusinessName;
  String? lastEmail;
  String? lastRecoveredPassword;
  int resendConfirmationCalls = 0;

  @override
  void cancelPasswordRecovery() {
    passwordRecoveryPending = false;
    passwordRecoveryEmail = '';
    notifyListeners();
  }

  @override
  void clearFeedback() {
    errorMessage = null;
    successMessage = null;
    notifyListeners();
  }

  @override
  Future<void> initialize() async {}

  @override
  Future<void> requestPasswordReset({required String email}) async {
    lastEmail = email;
    successMessage =
        'Se este e-mail estiver cadastrado, enviaremos um link para redefinir sua senha.';
    notifyListeners();
  }

  @override
  Future<void> resendSignUpConfirmation() async {
    resendConfirmationCalls++;
    successMessage = 'Enviamos um novo link de confirmação.';
    notifyListeners();
  }

  @override
  Future<void> signIn({required String email, required String password}) async {
    lastEmail = email;
  }

  @override
  Future<void> signOut() async {}

  @override
  Future<void> signUp({
    required String name,
    required String businessName,
    required String email,
    required String password,
  }) async {
    lastName = name;
    lastBusinessName = businessName;
    lastEmail = email;
    pendingConfirmationEmail = email;
    successMessage =
        'Conta criada! Enviamos um link de confirmação para $email.';
    notifyListeners();
  }

  @override
  Future<void> updateRecoveredPassword({required String password}) async {
    lastRecoveredPassword = password;
    passwordRecoveryPending = false;
    passwordRecoveryEmail = '';
    successMessage = 'Senha alterada com sucesso. Entre com a nova senha.';
    notifyListeners();
  }
}
