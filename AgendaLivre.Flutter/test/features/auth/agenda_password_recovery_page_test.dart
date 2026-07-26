import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/web_agenda_session.dart';
import 'package:agenda_livre/features/auth/agenda_auth_page.dart';
import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('solicita link de recuperação no layout WPF existente', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(980, 700);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final session = _RecoveryWebSession();

    await tester.pumpWidget(
      MaterialApp(home: AgendaAuthPage(session: session)),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('auth-forgot-password')));
    await tester.pumpAndSettle();

    expect(find.text('Recupere seu acesso'), findsOneWidget);
    expect(find.byKey(const Key('auth-password-field')), findsNothing);
    await tester.enterText(
      find.byKey(const Key('auth-email-field')),
      'nina@example.com',
    );
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();

    expect(session.requestedRecoveryEmail, 'nina@example.com');
    expect(find.textContaining('enviaremos um link'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('callback exige duas senhas iguais antes de redefinir', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(980, 700);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final session = _RecoveryWebSession()
      ..passwordRecoveryPending = true
      ..passwordRecoveryEmail = 'nina@example.com';

    await tester.pumpWidget(
      MaterialApp(home: AgendaAuthPage(session: session)),
    );
    await tester.pumpAndSettle();

    expect(find.text('Crie uma nova senha'), findsOneWidget);
    expect(find.byKey(const Key('auth-recovery-identity')), findsOneWidget);
    await tester.enterText(
      find.byKey(const Key('auth-password-field')),
      'novaSenha123',
    );
    await tester.enterText(
      find.byKey(const Key('auth-password-confirmation-field')),
      'senhaDiferente',
    );
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();
    expect(find.text('As senhas precisam ser iguais.'), findsOneWidget);
    expect(session.updatedPassword, isNull);

    await tester.enterText(
      find.byKey(const Key('auth-password-confirmation-field')),
      'novaSenha123',
    );
    await tester.tap(find.byKey(const Key('auth-submit')));
    await tester.pumpAndSettle();

    expect(session.updatedPassword, 'novaSenha123');
    expect(find.text('Bem-vindo de volta'), findsOneWidget);
    expect(find.textContaining('Senha alterada com sucesso'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

class _RecoveryWebSession extends ChangeNotifier implements AgendaWebSession {
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

  String? requestedRecoveryEmail;
  String? updatedPassword;

  @override
  void cancelPasswordRecovery() {
    passwordRecoveryPending = false;
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
    requestedRecoveryEmail = email;
    successMessage =
        'Se este e-mail estiver cadastrado, enviaremos um link para redefinir sua senha.';
    notifyListeners();
  }

  @override
  Future<void> resendSignUpConfirmation() async {}

  @override
  Future<void> signIn({
    required String email,
    required String password,
  }) async {}

  @override
  Future<void> signOut() async {}

  @override
  Future<void> signUp({
    required String name,
    required String businessName,
    required String email,
    required String password,
  }) async {}

  @override
  Future<void> updateRecoveredPassword({required String password}) async {
    updatedPassword = password;
    passwordRecoveryPending = false;
    passwordRecoveryEmail = '';
    successMessage = 'Senha alterada com sucesso. Entre com a nova senha.';
    notifyListeners();
  }
}
