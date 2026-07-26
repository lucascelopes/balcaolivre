import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/web_agenda_session.dart';

enum _AuthMode { signIn, signUp, forgotPassword, resetPassword }

class AgendaAuthPage extends StatefulWidget {
  const AgendaAuthPage({super.key, required this.session});

  final AgendaWebSession session;

  @override
  State<AgendaAuthPage> createState() => _AgendaAuthPageState();
}

class _AgendaAuthPageState extends State<AgendaAuthPage> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _business = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _passwordConfirmation = TextEditingController();
  _AuthMode _mode = _AuthMode.signIn;
  bool _showPassword = false;
  bool _showPasswordConfirmation = false;

  _AuthMode get _effectiveMode =>
      widget.session.passwordRecoveryPending ? _AuthMode.resetPassword : _mode;

  @override
  void dispose() {
    _name.dispose();
    _business.dispose();
    _email.dispose();
    _password.dispose();
    _passwordConfirmation.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.session,
      builder: (context, _) {
        return Scaffold(
          backgroundColor: const Color(0xFFFFFCFA),
          body: LayoutBuilder(
            builder: (context, constraints) {
              final desktop = constraints.maxWidth >= 920;
              if (desktop) {
                return Row(
                  children: [
                    const Expanded(flex: 44, child: _DesktopBrandPanel()),
                    Expanded(
                      flex: 56,
                      child: _AuthFormViewport(
                        horizontalPadding: 32,
                        child: _authForm(desktop: true),
                      ),
                    ),
                  ],
                );
              }
              return SafeArea(
                child: Column(
                  children: [
                    const _MobileBrand(),
                    Expanded(
                      child: _AuthFormViewport(
                        horizontalPadding: 20,
                        child: _authForm(desktop: false),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        );
      },
    );
  }

  Widget _authForm({required bool desktop}) {
    final mode = _effectiveMode;
    final creating = mode == _AuthMode.signUp;
    final forgotPassword = mode == _AuthMode.forgotPassword;
    final resetPassword = mode == _AuthMode.resetPassword;
    return Padding(
      key: const Key('agenda-auth-card'),
      padding: EdgeInsets.symmetric(vertical: desktop ? 20 : 10),
      child: Form(
        key: _formKey,
        child: AutofillGroup(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                switch (mode) {
                  _AuthMode.signUp => 'Crie sua conta',
                  _AuthMode.forgotPassword => 'Recupere seu acesso',
                  _AuthMode.resetPassword => 'Crie uma nova senha',
                  _ => 'Bem-vindo de volta',
                },
                style: TextStyle(
                  color: const Color(0xFF090909),
                  fontSize: desktop ? 30 : 28,
                  height: 1.1,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 7),
              Text(
                switch (mode) {
                  _AuthMode.signUp =>
                    'Cadastre-se para usar a mesma agenda no Windows e na Web.',
                  _AuthMode.forgotPassword =>
                    'Informe o e-mail da sua conta para receber o link de recuperação.',
                  _AuthMode.resetPassword =>
                    'Defina uma senha nova para voltar à sua agenda com segurança.',
                  _ => 'Entre para abrir sua agenda sincronizada.',
                },
                style: const TextStyle(
                  color: Color(0xFF80675B),
                  fontSize: 13.4,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 17),
              if (forgotPassword || resetPassword)
                _backToSignInButton(resetPassword: resetPassword)
              else
                _modeSelector(),
              const SizedBox(height: 19),
              if (creating) ...[
                _field(
                  key: const Key('auth-name-field'),
                  controller: _name,
                  label: 'Nome completo',
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.name],
                  validator: (value) => _required(value, 'Informe seu nome.'),
                ),
                const SizedBox(height: 12),
                _field(
                  key: const Key('auth-business-field'),
                  controller: _business,
                  label: 'Nome do negócio',
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.organizationName],
                  validator: (value) =>
                      _required(value, 'Informe o nome do negócio.'),
                ),
                const SizedBox(height: 12),
              ],
              if (!resetPassword) ...[
                _field(
                  key: const Key('auth-email-field'),
                  controller: _email,
                  label: 'E-mail',
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: forgotPassword
                      ? TextInputAction.done
                      : TextInputAction.next,
                  autofillHints: const [AutofillHints.email],
                  validator: _validateEmail,
                  onSubmitted: forgotPassword ? (_) => _submit() : null,
                ),
                if (!forgotPassword) const SizedBox(height: 12),
              ] else if (widget.session.passwordRecoveryEmail.isNotEmpty) ...[
                _RecoveryIdentity(email: widget.session.passwordRecoveryEmail),
                const SizedBox(height: 12),
              ],
              if (!forgotPassword) ...[
                _field(
                  key: const Key('auth-password-field'),
                  controller: _password,
                  label: resetPassword ? 'Nova senha' : 'Senha',
                  obscureText: !_showPassword,
                  textInputAction: resetPassword
                      ? TextInputAction.next
                      : TextInputAction.done,
                  autofillHints: [
                    creating || resetPassword
                        ? AutofillHints.newPassword
                        : AutofillHints.password,
                  ],
                  validator: _validatePassword,
                  onSubmitted: resetPassword ? null : (_) => _submit(),
                  suffixIcon: IconButton(
                    tooltip: _showPassword ? 'Ocultar senha' : 'Mostrar senha',
                    onPressed: () =>
                        setState(() => _showPassword = !_showPassword),
                    icon: Icon(
                      _showPassword
                          ? Icons.visibility_off_outlined
                          : Icons.visibility_outlined,
                      size: 19,
                      color: const Color(0xFF706A67),
                    ),
                  ),
                ),
                if (resetPassword) ...[
                  const SizedBox(height: 12),
                  _field(
                    key: const Key('auth-password-confirmation-field'),
                    controller: _passwordConfirmation,
                    label: 'Confirme a nova senha',
                    obscureText: !_showPasswordConfirmation,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.newPassword],
                    validator: (value) => value != _password.text
                        ? 'As senhas precisam ser iguais.'
                        : null,
                    onSubmitted: (_) => _submit(),
                    suffixIcon: IconButton(
                      tooltip: _showPasswordConfirmation
                          ? 'Ocultar confirmação'
                          : 'Mostrar confirmação',
                      onPressed: () => setState(
                        () => _showPasswordConfirmation =
                            !_showPasswordConfirmation,
                      ),
                      icon: Icon(
                        _showPasswordConfirmation
                            ? Icons.visibility_off_outlined
                            : Icons.visibility_outlined,
                        size: 19,
                        color: const Color(0xFF706A67),
                      ),
                    ),
                  ),
                ],
                const SizedBox(height: 6),
                Text(
                  creating || resetPassword
                      ? 'Crie uma senha com pelo menos 6 caracteres.'
                      : 'Use a senha da sua conta.',
                  style: const TextStyle(
                    color: Color(0xFF8A6F63),
                    fontSize: 11.2,
                    height: 1.2,
                  ),
                ),
              ],
              if (mode == _AuthMode.signIn)
                Align(
                  alignment: Alignment.centerRight,
                  child: TextButton(
                    key: const Key('auth-forgot-password'),
                    onPressed: widget.session.busy
                        ? null
                        : () => _changeMode(_AuthMode.forgotPassword),
                    style: TextButton.styleFrom(
                      foregroundColor: const Color(0xFFEB4E13),
                      padding: const EdgeInsets.fromLTRB(8, 7, 0, 3),
                      minimumSize: const Size(0, 32),
                    ),
                    child: const Text(
                      'Esqueci minha senha',
                      style: TextStyle(
                        fontSize: 12.2,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ),
              if (widget.session.errorMessage != null) ...[
                const SizedBox(height: 12),
                _FeedbackBox(
                  key: const Key('auth-error-message'),
                  message: widget.session.errorMessage!,
                  success: false,
                ),
              ],
              if (widget.session.successMessage != null) ...[
                const SizedBox(height: 12),
                _FeedbackBox(
                  key: const Key('auth-success-message'),
                  message: widget.session.successMessage!,
                  success: true,
                ),
              ],
              if (!forgotPassword &&
                  !resetPassword &&
                  widget.session.pendingConfirmationEmail != null) ...[
                const SizedBox(height: 5),
                TextButton(
                  key: const Key('auth-resend-confirmation'),
                  onPressed: widget.session.busy
                      ? null
                      : widget.session.resendSignUpConfirmation,
                  style: TextButton.styleFrom(
                    foregroundColor: const Color(0xFFEB4E13),
                  ),
                  child: const Text(
                    'Reenviar e-mail de confirmação',
                    style: TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
              ],
              const SizedBox(height: 13),
              SizedBox(
                height: 48,
                child: ElevatedButton(
                  key: const Key('auth-submit'),
                  onPressed: widget.session.busy ? null : _submit,
                  style: ElevatedButton.styleFrom(
                    elevation: 0,
                    backgroundColor: const Color(0xFFFC4F0C),
                    foregroundColor: Colors.white,
                    disabledBackgroundColor: const Color(0xFFF3B18F),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(6),
                    ),
                  ),
                  child: widget.session.busy
                      ? const SizedBox(
                          width: 21,
                          height: 21,
                          child: CircularProgressIndicator(
                            strokeWidth: 2.4,
                            color: Colors.white,
                          ),
                        )
                      : Text(
                          switch (mode) {
                            _AuthMode.signUp => 'Criar minha conta',
                            _AuthMode.forgotPassword =>
                              'Enviar link de recuperação',
                            _AuthMode.resetPassword => 'Salvar nova senha',
                            _ => 'Entrar',
                          },
                          style: const TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                ),
              ),
              const SizedBox(height: 14),
              _AuthFooterNote(creating: creating),
            ],
          ),
        ),
      ),
    );
  }

  Widget _modeSelector() {
    return Container(
      height: 50,
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: const Color(0xFFF2EFED),
        borderRadius: BorderRadius.circular(13),
      ),
      child: Row(
        children: [
          Expanded(
            child: _modeButton(
              key: const Key('auth-mode-sign-in'),
              label: 'Entrar',
              selected: _effectiveMode == _AuthMode.signIn,
              onPressed: () => _changeMode(_AuthMode.signIn),
            ),
          ),
          Expanded(
            child: _modeButton(
              key: const Key('auth-mode-sign-up'),
              label: 'Criar conta',
              selected: _effectiveMode == _AuthMode.signUp,
              onPressed: () => _changeMode(_AuthMode.signUp),
            ),
          ),
        ],
      ),
    );
  }

  Widget _backToSignInButton({required bool resetPassword}) {
    return SizedBox(
      height: 50,
      child: OutlinedButton.icon(
        key: const Key('auth-back-to-sign-in'),
        onPressed: widget.session.busy
            ? null
            : () {
                if (resetPassword) widget.session.cancelPasswordRecovery();
                _changeMode(_AuthMode.signIn);
              },
        style: OutlinedButton.styleFrom(
          foregroundColor: const Color(0xFF725F56),
          side: const BorderSide(color: Color(0xFFD9D0CB)),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
        ),
        icon: const Icon(Icons.arrow_back_rounded, size: 17),
        label: const Text(
          'Voltar para entrar',
          style: TextStyle(fontSize: 13.2, fontWeight: FontWeight.w700),
        ),
      ),
    );
  }

  Widget _modeButton({
    required Key key,
    required String label,
    required bool selected,
    required VoidCallback onPressed,
  }) {
    return TextButton(
      key: key,
      onPressed: widget.session.busy ? null : onPressed,
      style: TextButton.styleFrom(
        foregroundColor: selected
            ? const Color(0xFFEB4E13)
            : const Color(0xFF725F56),
        backgroundColor: selected
            ? const Color(0xFFFFE0CF)
            : Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 13.2,
          fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
        ),
      ),
    );
  }

  Widget _field({
    required Key key,
    required TextEditingController controller,
    required String label,
    required TextInputAction textInputAction,
    required Iterable<String> autofillHints,
    String? Function(String?)? validator,
    TextInputType? keyboardType,
    bool obscureText = false,
    Widget? suffixIcon,
    ValueChanged<String>? onSubmitted,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          label,
          style: const TextStyle(
            color: Color(0xFF090909),
            fontSize: 12.8,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 6),
        TextFormField(
          key: key,
          controller: controller,
          enabled: !widget.session.busy,
          keyboardType: keyboardType,
          textInputAction: textInputAction,
          obscureText: obscureText,
          autofillHints: autofillHints,
          validator: validator,
          onFieldSubmitted: onSubmitted,
          autocorrect: false,
          enableSuggestions: !obscureText,
          inputFormatters: keyboardType == TextInputType.emailAddress
              ? <TextInputFormatter>[
                  FilteringTextInputFormatter.deny(RegExp(r'\s')),
                ]
              : null,
          decoration: InputDecoration(
            suffixIcon: suffixIcon,
            filled: true,
            fillColor: Colors.white,
            isDense: true,
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 12,
              vertical: 13,
            ),
            border: _fieldBorder(const Color(0xFFCFC6C1)),
            enabledBorder: _fieldBorder(const Color(0xFFCFC6C1)),
            focusedBorder: _fieldBorder(const Color(0xFFFC4F0C), width: 1.4),
            errorBorder: _fieldBorder(const Color(0xFFB42318)),
            focusedErrorBorder: _fieldBorder(
              const Color(0xFFB42318),
              width: 1.4,
            ),
          ),
        ),
      ],
    );
  }

  static OutlineInputBorder _fieldBorder(Color color, {double width = 1}) =>
      OutlineInputBorder(
        borderRadius: BorderRadius.circular(5),
        borderSide: BorderSide(color: color, width: width),
      );

  void _changeMode(_AuthMode mode) {
    if (_effectiveMode == mode) return;
    widget.session.clearFeedback();
    setState(() {
      _mode = mode;
      _formKey.currentState?.reset();
      _password.clear();
      _passwordConfirmation.clear();
    });
  }

  Future<void> _submit() async {
    FocusScope.of(context).unfocus();
    if (!(_formKey.currentState?.validate() ?? false)) return;
    switch (_effectiveMode) {
      case _AuthMode.signUp:
        await widget.session.signUp(
          name: _name.text,
          businessName: _business.text,
          email: _email.text,
          password: _password.text,
        );
        if (widget.session.pendingConfirmationEmail != null) {
          _password.clear();
          setState(() => _mode = _AuthMode.signIn);
        }
        return;
      case _AuthMode.forgotPassword:
        await widget.session.requestPasswordReset(email: _email.text);
        return;
      case _AuthMode.resetPassword:
        await widget.session.updateRecoveredPassword(password: _password.text);
        if (!widget.session.passwordRecoveryPending) {
          _password.clear();
          _passwordConfirmation.clear();
          setState(() => _mode = _AuthMode.signIn);
        }
        return;
      case _AuthMode.signIn:
        await widget.session.signIn(
          email: _email.text,
          password: _password.text,
        );
        return;
    }
  }

  static String? _required(String? value, String message) =>
      (value?.trim().isEmpty ?? true) ? message : null;

  static String? _validateEmail(String? value) {
    final email = value?.trim() ?? '';
    if (email.isEmpty) return 'Informe seu e-mail.';
    if (!RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(email)) {
      return 'Informe um e-mail válido.';
    }
    return null;
  }

  static String? _validatePassword(String? value) {
    if ((value ?? '').length < 6) return 'Use pelo menos 6 caracteres.';
    return null;
  }
}

class _RecoveryIdentity extends StatelessWidget {
  const _RecoveryIdentity({required this.email});

  final String email;

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('auth-recovery-identity'),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 11),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF4EE),
        border: Border.all(color: const Color(0xFFF0C7B3)),
        borderRadius: BorderRadius.circular(5),
      ),
      child: Row(
        children: [
          const Icon(
            Icons.verified_user_outlined,
            size: 18,
            color: Color(0xFFEB4E13),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              email,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Color(0xFF4B342A),
                fontSize: 12.6,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _AuthFormViewport extends StatelessWidget {
  const _AuthFormViewport({
    required this.child,
    required this.horizontalPadding,
  });

  final Widget child;
  final double horizontalPadding;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        const verticalPadding = 24.0;
        return SingleChildScrollView(
          padding: EdgeInsets.symmetric(
            horizontal: horizontalPadding,
            vertical: verticalPadding,
          ),
          child: ConstrainedBox(
            constraints: BoxConstraints(
              minHeight: (constraints.maxHeight - verticalPadding * 2).clamp(
                0,
                double.infinity,
              ),
            ),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 432),
                child: child,
              ),
            ),
          ),
        );
      },
    );
  }
}

class _DesktopBrandPanel extends StatelessWidget {
  const _DesktopBrandPanel();

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: const Color(0xFF211814),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(44, 44, 42, 42),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const _BrandLockup(compact: false),
            Expanded(
              child: Align(
                alignment: Alignment.centerLeft,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: const [
                      Text(
                        'Entre no Windows e\ncontinue de onde\nparou.',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 34,
                          height: 1.33,
                          fontWeight: FontWeight.w800,
                          letterSpacing: .1,
                        ),
                      ),
                      SizedBox(height: 20),
                      Text(
                        'A mesma conta conecta seus cadastros e sua\n'
                        'agenda, com cópia local para você continuar\n'
                        'trabalhando mesmo se a internet oscilar.',
                        style: TextStyle(
                          color: Color(0xFFF4E9E3),
                          fontSize: 14.2,
                          height: 1.7,
                        ),
                      ),
                      SizedBox(height: 25),
                      _AuthBullet(label: 'Dados separados por conta'),
                      SizedBox(height: 13),
                      _AuthBullet(label: 'Sincronização protegida'),
                      SizedBox(height: 13),
                      _AuthBullet(label: 'Trabalho offline preservado'),
                    ],
                  ),
                ),
              ),
            ),
            const Text(
              'Agenda Livre para Windows',
              style: TextStyle(color: Color(0xFFD7C4BA), fontSize: 11.5),
            ),
          ],
        ),
      ),
    );
  }
}

class _BrandLockup extends StatelessWidget {
  const _BrandLockup({required this.compact});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    final markWidth = compact ? 58.0 : 80.0;
    return Row(
      children: [
        SizedBox(
          width: markWidth,
          height: compact ? 36 : 50,
          child: Stack(
            children: [
              Positioned.fill(
                child: ColorFiltered(
                  colorFilter: const ColorFilter.mode(
                    Colors.white,
                    BlendMode.srcIn,
                  ),
                  child: Image.asset(
                    'assets/branding/agenda-livre-mark.png',
                    fit: BoxFit.contain,
                    semanticLabel: 'Agenda Livre',
                  ),
                ),
              ),
              Positioned(
                right: compact ? 0 : 1,
                bottom: compact ? 5 : 7,
                child: Icon(
                  Icons.circle,
                  size: compact ? 5 : 7,
                  color: const Color(0xFFFF5A13),
                ),
              ),
            ],
          ),
        ),
        SizedBox(width: compact ? 9 : 10),
        Expanded(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'AGENDA LIVRE',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: Colors.white,
                  fontSize: compact ? 13 : 17,
                  fontWeight: FontWeight.w900,
                  letterSpacing: .1,
                ),
              ),
              SizedBox(height: compact ? 2 : 3),
              Text(
                'Sua agenda em todos os lugares',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: const Color(0xFFF7ECE6),
                  fontSize: compact ? 8.5 : 10.5,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _MobileBrand extends StatelessWidget {
  const _MobileBrand();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      height: 82,
      padding: const EdgeInsets.symmetric(horizontal: 20),
      color: const Color(0xFF211814),
      alignment: Alignment.centerLeft,
      child: const _BrandLockup(compact: true),
    );
  }
}

class _AuthBullet extends StatelessWidget {
  const _AuthBullet({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Icon(Icons.check_rounded, size: 18, color: Color(0xFFFF5A13)),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 12.8,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
      ],
    );
  }
}

class _AuthFooterNote extends StatelessWidget {
  const _AuthFooterNote({required this.creating});

  static final Uri _privacyUri = Uri.parse(
    'https://minhaagendalivre.com.br/agenda-livre/privacidade',
  );

  final bool creating;

  @override
  Widget build(BuildContext context) {
    if (!creating) {
      return const Text(
        'Sua sessão fica protegida pela conta deste navegador.',
        textAlign: TextAlign.center,
        style: TextStyle(
          color: Color(0xFF8A6F63),
          fontSize: 10.8,
          height: 1.35,
        ),
      );
    }
    return Wrap(
      alignment: WrapAlignment.center,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        const Text(
          'Ao criar sua conta, você concorda com o armazenamento seguro dos seus dados conforme nossa ',
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Color(0xFF8A6F63),
            fontSize: 10.8,
            height: 1.35,
          ),
        ),
        InkWell(
          onTap: () => launchUrl(_privacyUri),
          child: const Text(
            'política de privacidade.',
            style: TextStyle(
              color: Color(0xFFFC4F0C),
              fontSize: 10.8,
              height: 1.35,
            ),
          ),
        ),
      ],
    );
  }
}

class _FeedbackBox extends StatelessWidget {
  const _FeedbackBox({super.key, required this.message, required this.success});

  final String message;
  final bool success;

  @override
  Widget build(BuildContext context) {
    final foreground = success
        ? const Color(0xFF166534)
        : const Color(0xFFB42318);
    final background = success
        ? const Color(0xFFF0FDF4)
        : const Color(0xFFFFF1F0);
    final border = success ? const Color(0xFFBBF7D0) : const Color(0xFFFECACA);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            success ? Icons.mark_email_read_outlined : Icons.error_outline,
            size: 19,
            color: foreground,
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              message,
              style: TextStyle(
                color: foreground,
                fontSize: 12.2,
                height: 1.35,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
