import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/web_agenda_session.dart';
import '../../services/agenda_account_api.dart';
import '../auth/agenda_auth_page.dart';

const _ink = Color(0xFF211A17);
const _muted = Color(0xFF7B6D66);
const _orange = Color(0xFFD94A14);
const _orangeSoft = Color(0xFFFFE5D8);
const _ivory = Color(0xFFFFFBF7);
const _line = Color(0xFFE9DDD5);

class AgendaCheckoutActivationPage extends StatelessWidget {
  const AgendaCheckoutActivationPage({super.key, required this.session});

  final AgendaWebSessionController session;

  Future<void> _openAuth(BuildContext context, AgendaAuthMode mode) async {
    await showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (context) => Dialog.fullscreen(
        child: _AuthDialogGate(
          session: session,
          child: Stack(
            children: [
              AgendaAuthPage(session: session, initialMode: mode),
              Positioned(
                top: 14,
                right: 14,
                child: IconButton.filledTonal(
                  tooltip: 'Voltar',
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.close_rounded),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final activation = session.checkoutActivation!;
    return Scaffold(
      backgroundColor: _ivory,
      body: SafeArea(
        child: Column(
          children: [
            const _ActivationHeader(),
            Expanded(
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final desktop = constraints.maxWidth >= 900;
                  final left = _ActivationProgress(
                    activation: activation,
                    compact: !desktop,
                  );
                  final right = _ActivationActions(
                    activation: activation,
                    compact: !desktop,
                    authenticated: session.authSession != null,
                    busy: session.busy,
                    onCreate: () => _openAuth(context, AgendaAuthMode.signUp),
                    onSignIn: () => _openAuth(context, AgendaAuthMode.signIn),
                    onWeb: session.finishCheckoutActivation,
                  );
                  if (desktop) {
                    return Row(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Expanded(child: left),
                        const VerticalDivider(width: 1, color: _line),
                        Expanded(child: right),
                      ],
                    );
                  }
                  return ListView(
                    padding: EdgeInsets.zero,
                    children: [left, const Divider(height: 1), right],
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AuthDialogGate extends StatefulWidget {
  const _AuthDialogGate({required this.session, required this.child});

  final AgendaWebSessionController session;
  final Widget child;

  @override
  State<_AuthDialogGate> createState() => _AuthDialogGateState();
}

class _AuthDialogGateState extends State<_AuthDialogGate> {
  @override
  void initState() {
    super.initState();
    widget.session.addListener(_sessionChanged);
  }

  void _sessionChanged() {
    if (!mounted || widget.session.authSession == null) return;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted && Navigator.of(context).canPop()) {
        Navigator.of(context).pop();
      }
    });
  }

  @override
  void dispose() {
    widget.session.removeListener(_sessionChanged);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

class _ActivationHeader extends StatelessWidget {
  const _ActivationHeader();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 74,
      padding: const EdgeInsets.symmetric(horizontal: 34),
      decoration: const BoxDecoration(
        color: Color(0xFFFFFDFC),
        border: Border(bottom: BorderSide(color: _line)),
      ),
      child: Row(
        children: [
          Image.asset(
            'assets/branding/agenda-livre-mark.png',
            height: 44,
            width: 110,
            fit: BoxFit.contain,
            alignment: Alignment.centerLeft,
            semanticLabel: 'Agenda Livre',
          ),
          const Spacer(),
          const Icon(Icons.lock_outline_rounded, size: 16, color: _muted),
          const SizedBox(width: 7),
          const Text(
            'Pagamento seguro',
            style: TextStyle(
              color: _muted,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class _ActivationProgress extends StatelessWidget {
  const _ActivationProgress({required this.activation, required this.compact});

  final AgendaCheckoutActivation activation;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFFFFF8F3),
      padding: compact
          ? const EdgeInsets.fromLTRB(28, 56, 28, 42)
          : const EdgeInsets.fromLTRB(72, 170, 58, 52),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 510),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 62,
              height: 62,
              decoration: BoxDecoration(
                color: const Color(0xFFFFF0E7),
                shape: BoxShape.circle,
                border: Border.all(color: const Color(0xFFFFC9AD)),
              ),
              child: Icon(
                activation.complete
                    ? Icons.check_rounded
                    : Icons.hourglass_top_rounded,
                color: _orange,
                size: 34,
              ),
            ),
            SizedBox(height: compact ? 30 : 45),
            Text(
              activation.complete
                  ? 'Pagamento\nconfirmado.'
                  : 'Confirmando seu\npagamento.',
              style: TextStyle(
                color: _ink,
                fontFamily: 'LibreBaskerville',
                fontSize: compact ? 40 : 56,
                height: 1.03,
                letterSpacing: compact ? -.7 : -1.2,
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 17),
            Text(
              activation.email.isEmpty
                  ? 'Sua assinatura está protegida pela Stripe.'
                  : 'Recibo e assinatura vinculados a ${activation.email}.',
              style: const TextStyle(color: _muted, height: 1.5, fontSize: 14),
            ),
            SizedBox(height: compact ? 48 : 75),
            _HorizontalProgress(activation: activation),
          ],
        ),
      ),
    );
  }

  static String sessionLabel(AgendaCheckoutActivation activation) {
    if (activation.checking) return 'Preparando';
    if (activation.claimed) return 'Vinculando licença';
    return 'Em andamento';
  }
}

class _HorizontalProgress extends StatelessWidget {
  const _HorizontalProgress({required this.activation});

  final AgendaCheckoutActivation activation;

  @override
  Widget build(BuildContext context) {
    final items = <(String, String, String, bool)>[
      (
        '1',
        'Pagamento',
        activation.complete ? 'Concluído' : 'Confirmando',
        true,
      ),
      (
        '2',
        'Sua conta',
        activation.ready
            ? 'Concluída'
            : _ActivationProgress.sessionLabel(activation),
        activation.complete,
      ),
      (
        '3',
        'Como acessar',
        activation.ready ? 'Escolha abaixo' : 'Próximo',
        activation.ready,
      ),
    ];
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: List<Widget>.generate(items.length, (index) {
        final item = items[index];
        final alignment = index == 0
            ? CrossAxisAlignment.start
            : index == items.length - 1
            ? CrossAxisAlignment.end
            : CrossAxisAlignment.center;
        return Expanded(
          child: Column(
            crossAxisAlignment: alignment,
            children: [
              Row(
                children: [
                  if (index > 0) const Expanded(child: Divider(color: _line)),
                  Container(
                    width: 29,
                    height: 29,
                    decoration: BoxDecoration(
                      color: item.$4 ? _orange : Colors.white,
                      shape: BoxShape.circle,
                      border: Border.all(color: item.$4 ? _orange : _line),
                    ),
                    alignment: Alignment.center,
                    child: Text(
                      item.$1,
                      style: TextStyle(
                        color: item.$4 ? Colors.white : _muted,
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  if (index < items.length - 1)
                    const Expanded(child: Divider(color: _line)),
                ],
              ),
              const SizedBox(height: 13),
              Text(
                item.$2,
                style: const TextStyle(
                  color: _ink,
                  fontSize: 13,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                item.$3,
                style: TextStyle(
                  color: item.$4 ? _orange : _muted,
                  fontSize: 11.5,
                ),
              ),
            ],
          ),
        );
      }),
    );
  }
}

class _ActivationActions extends StatelessWidget {
  const _ActivationActions({
    required this.activation,
    required this.compact,
    required this.authenticated,
    required this.busy,
    required this.onCreate,
    required this.onSignIn,
    required this.onWeb,
  });

  final AgendaCheckoutActivation activation;
  final bool compact;
  final bool authenticated;
  final bool busy;
  final VoidCallback onCreate;
  final VoidCallback onSignIn;
  final VoidCallback onWeb;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: compact
          ? const EdgeInsets.fromLTRB(28, 52, 28, 40)
          : const EdgeInsets.fromLTRB(80, 84, 80, 44),
      child: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 500),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                activation.ready
                    ? 'Sua conta está pronta.'
                    : 'Crie ou acesse sua conta',
                style: TextStyle(
                  color: _ink,
                  fontFamily: 'LibreBaskerville',
                  fontSize: compact ? 31 : 36,
                  height: 1.08,
                  letterSpacing: -.7,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(height: 11),
              Text(
                activation.ready
                    ? 'A mesma assinatura funciona na Web e no Windows.'
                    : 'A assinatura será ligada à sua conta Agenda Livre e funcionará em todos os seus dispositivos.',
                style: const TextStyle(
                  color: _muted,
                  fontSize: 14,
                  height: 1.5,
                ),
              ),
              const SizedBox(height: 43),
              if (activation.errorMessage != null) ...[
                _InlineNotice(text: activation.errorMessage!),
                const SizedBox(height: 18),
              ],
              if (activation.checking || (authenticated && !activation.ready))
                const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: CircularProgressIndicator(color: _orange),
                  ),
                )
              else if (!authenticated) ...[
                _PrimaryButton(
                  label: 'Criar minha conta',
                  icon: Icons.arrow_forward_rounded,
                  onPressed: activation.complete ? onCreate : null,
                ),
                const SizedBox(height: 15),
                _OutlineButton(
                  label: 'Já tenho conta — Entrar',
                  onPressed: activation.complete ? onSignIn : null,
                ),
              ] else ...[
                _PrimaryButton(
                  label: 'Entrar na Agenda Livre Web',
                  icon: Icons.language_rounded,
                  onPressed: onWeb,
                ),
                const SizedBox(height: 15),
                _OutlineButton(
                  label: 'Baixar aplicativo para Windows',
                  icon: Icons.desktop_windows_rounded,
                  onPressed: () => launchUrl(
                    Uri.parse(
                      'https://minhaagendalivre.com.br/agenda-livre/agenda-livre-windows-1.0.0.zip',
                    ),
                    mode: LaunchMode.externalApplication,
                  ),
                ),
              ],
              SizedBox(height: compact ? 58 : 85),
              const Row(
                children: [
                  Expanded(child: Divider(color: _line)),
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 12),
                    child: Text(
                      'DEPOIS, ESCOLHA ONDE USAR',
                      style: TextStyle(
                        color: _muted,
                        fontSize: 9,
                        letterSpacing: 1.4,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  Expanded(child: Divider(color: _line)),
                ],
              ),
              const SizedBox(height: 28),
              const Row(
                children: [
                  Expanded(
                    child: _AccessChoice(
                      icon: Icons.language_rounded,
                      title: 'Entrar pela Web',
                      detail: 'Acessar no navegador',
                    ),
                  ),
                  SizedBox(height: 112, child: VerticalDivider(color: _line)),
                  Expanded(
                    child: _AccessChoice(
                      icon: Icons.file_download_outlined,
                      title: 'Baixar para Windows',
                      detail: 'Aplicativo no computador',
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 82),
              const Divider(color: _line),
              const SizedBox(height: 15),
              const Row(
                children: [
                  Icon(Icons.verified_user_outlined, color: _muted, size: 22),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Sua assinatura já está ativa. Você pode trocar a forma de acesso quando quiser.',
                      style: TextStyle(
                        color: _muted,
                        fontSize: 11.5,
                        height: 1.35,
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class AgendaSubscriptionReminder extends StatelessWidget {
  const AgendaSubscriptionReminder({
    super.key,
    required this.session,
    required this.daysRemaining,
    required this.expired,
    required this.onClose,
  });

  final AgendaWebSessionController session;
  final int daysRemaining;
  final bool expired;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);
    final compact = media.size.width < 620;
    final remainingLabel = daysRemaining == 1
        ? '1 dia restante'
        : '$daysRemaining dias restantes';
    return Positioned.fill(
      child: Material(
        color: Colors.black.withValues(alpha: 0.42),
        child: Stack(
          children: [
            Positioned.fill(
              child: Semantics(
                button: true,
                label: 'Fechar aviso de assinatura',
                child: GestureDetector(
                  behavior: HitTestBehavior.opaque,
                  onTap: onClose,
                ),
              ),
            ),
            Align(
              alignment: compact ? Alignment.bottomCenter : Alignment.center,
              child: SafeArea(
                minimum: EdgeInsets.fromLTRB(
                  compact ? 8 : 24,
                  24,
                  compact ? 8 : 24,
                  compact ? 8 : 24,
                ),
                child: TweenAnimationBuilder<double>(
                  duration: const Duration(milliseconds: 360),
                  curve: Curves.easeOutCubic,
                  tween: Tween(begin: 0, end: 1),
                  builder: (context, value, child) => Opacity(
                    opacity: value,
                    child: Transform.translate(
                      offset: Offset(0, 34 * (1 - value)),
                      child: Transform.scale(
                        scale: 0.97 + (0.03 * value),
                        alignment: Alignment.bottomCenter,
                        child: child,
                      ),
                    ),
                  ),
                  child: Material(
                    color: Colors.white,
                    elevation: 22,
                    shadowColor: Colors.black38,
                    borderRadius: BorderRadius.circular(compact ? 26 : 30),
                    clipBehavior: Clip.antiAlias,
                    child: ConstrainedBox(
                      constraints: BoxConstraints(
                        maxWidth: 520,
                        maxHeight: media.size.height * (compact ? 0.9 : 0.84),
                      ),
                      child: SingleChildScrollView(
                        padding: EdgeInsets.fromLTRB(
                          compact ? 20 : 30,
                          compact ? 18 : 26,
                          compact ? 20 : 30,
                          compact ? 22 : 28,
                        ),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Container(
                                  width: 46,
                                  height: 46,
                                  decoration: BoxDecoration(
                                    color: _orangeSoft,
                                    borderRadius: BorderRadius.circular(15),
                                  ),
                                  alignment: Alignment.center,
                                  child: const FaIcon(
                                    FontAwesomeIcons.calendarCheck,
                                    color: _orange,
                                    size: 21,
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      const Text(
                                        'AGENDA LIVRE',
                                        style: TextStyle(
                                          color: _orange,
                                          fontSize: 11,
                                          fontWeight: FontWeight.w900,
                                          letterSpacing: 1.1,
                                        ),
                                      ),
                                      const SizedBox(height: 3),
                                      Text(
                                        expired
                                            ? 'Teste gratuito encerrado'
                                            : 'Teste grátis • $remainingLabel',
                                        key: const Key(
                                          'subscription-reminder-status',
                                        ),
                                        style: const TextStyle(
                                          color: _ink,
                                          fontWeight: FontWeight.w800,
                                          fontSize: 13,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                IconButton(
                                  key: const Key('subscription-reminder-close'),
                                  tooltip: 'Agora não',
                                  onPressed: onClose,
                                  icon: const FaIcon(
                                    FontAwesomeIcons.xmark,
                                    size: 20,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 22),
                            Text(
                              expired
                                  ? 'Assine quando quiser. Sua agenda continua acessível.'
                                  : 'Seu teste continua — faltam $remainingLabel.',
                              style: TextStyle(
                                color: _ink,
                                fontSize: compact ? 25 : 30,
                                height: 1.08,
                                fontWeight: FontWeight.w900,
                                letterSpacing: -0.7,
                              ),
                            ),
                            const SizedBox(height: 12),
                            Text(
                              expired
                                  ? 'Você pode fechar este aviso e voltar exatamente ao que estava fazendo. Quando estiver pronta, escolha um plano e conclua pela Stripe.'
                                  : 'Os 7 dias de teste não exigem cartão e nada será cobrado automaticamente. Se preferir, assine agora e não precise se preocupar em ativar depois.',
                              style: const TextStyle(
                                color: _muted,
                                fontSize: 14,
                                height: 1.45,
                              ),
                            ),
                            if (!expired) ...[
                              const SizedBox(height: 16),
                              Container(
                                padding: const EdgeInsets.all(13),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFFFF7F2),
                                  borderRadius: BorderRadius.circular(14),
                                  border: Border.all(color: _line),
                                ),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Padding(
                                      padding: EdgeInsets.only(top: 1),
                                      child: FaIcon(
                                        FontAwesomeIcons.shieldHalved,
                                        color: _orange,
                                        size: 17,
                                      ),
                                    ),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Text(
                                        'Ao abrir a Stripe, o período grátis será mantido com $remainingLabel. A cobrança começa somente depois dele.',
                                        style: const TextStyle(
                                          color: _ink,
                                          fontSize: 12.5,
                                          height: 1.35,
                                          fontWeight: FontWeight.w600,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                            const SizedBox(height: 22),
                            SizedBox(
                              width: double.infinity,
                              height: 54,
                              child: FilledButton.icon(
                                key: const Key('subscription-reminder-monthly'),
                                onPressed: session.busy
                                    ? null
                                    : () => session.renewSubscription('mensal'),
                                style: FilledButton.styleFrom(
                                  backgroundColor: _orange,
                                  foregroundColor: Colors.white,
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(16),
                                  ),
                                ),
                                icon: session.busy
                                    ? const SizedBox.square(
                                        dimension: 18,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: Colors.white,
                                        ),
                                      )
                                    : const FaIcon(
                                        FontAwesomeIcons.arrowRight,
                                        size: 16,
                                      ),
                                label: const Text(
                                  'Assinar mensal • R\$ 49,90',
                                  style: TextStyle(fontWeight: FontWeight.w800),
                                ),
                              ),
                            ),
                            const SizedBox(height: 10),
                            SizedBox(
                              width: double.infinity,
                              height: 50,
                              child: OutlinedButton(
                                key: const Key('subscription-reminder-annual'),
                                onPressed: session.busy
                                    ? null
                                    : () => session.renewSubscription('anual'),
                                style: OutlinedButton.styleFrom(
                                  foregroundColor: _ink,
                                  side: const BorderSide(color: _line),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(16),
                                  ),
                                ),
                                child: const Text(
                                  'Plano anual • R\$ 598,80',
                                  style: TextStyle(fontWeight: FontWeight.w800),
                                ),
                              ),
                            ),
                            TextButton(
                              key: const Key('subscription-reminder-later'),
                              onPressed: onClose,
                              style: TextButton.styleFrom(
                                minimumSize: const Size.fromHeight(48),
                                foregroundColor: _muted,
                              ),
                              child: const Text(
                                'Agora não, voltar para minha agenda',
                                style: TextStyle(fontWeight: FontWeight.w700),
                              ),
                            ),
                            if (session.errorMessage != null) ...[
                              const SizedBox(height: 8),
                              _InlineNotice(text: session.errorMessage!),
                            ],
                            const Center(
                              child: Text(
                                'Pagamento seguro processado pela Stripe',
                                style: TextStyle(color: _muted, fontSize: 11),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

@Deprecated('Use AgendaSubscriptionReminder inside the active Agenda app.')
class AgendaSubscriptionRenewalPage extends StatelessWidget {
  const AgendaSubscriptionRenewalPage({super.key, required this.session});

  final AgendaWebSessionController session;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _ivory,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Container(
              width: 720,
              padding: const EdgeInsets.all(42),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(24),
                border: Border.all(color: _line),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x16000000),
                    blurRadius: 40,
                    offset: Offset(0, 16),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Image.asset(
                    'assets/branding/agenda-livre-mark.png',
                    height: 48,
                    width: 120,
                    fit: BoxFit.contain,
                    alignment: Alignment.centerLeft,
                  ),
                  const SizedBox(height: 30),
                  const Icon(
                    Icons.event_busy_rounded,
                    color: _orange,
                    size: 44,
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Renove para continuar com sua agenda.',
                    style: TextStyle(
                      color: _ink,
                      fontSize: 34,
                      height: 1.08,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  const SizedBox(height: 12),
                  const Text(
                    'Seus dados continuam seguros. Escolha um plano para reativar a licença ou abra a Stripe para usar e atualizar o cartão salvo.',
                    style: TextStyle(color: _muted, height: 1.5),
                  ),
                  const SizedBox(height: 20),
                  _SavedCardSummary(
                    card: session.billingCard,
                    loaded: session.billingCardLoaded,
                  ),
                  const SizedBox(height: 28),
                  _PrimaryButton(
                    label: 'Renovar mensal — R\$ 49,90',
                    icon: Icons.arrow_forward_rounded,
                    onPressed: session.busy
                        ? null
                        : () => session.renewSubscription('mensal'),
                  ),
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: _OutlineButton(
                      label: 'Renovar anual — R\$ 598,80',
                      onPressed: session.busy
                          ? null
                          : () => session.renewSubscription('anual'),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextButton.icon(
                    onPressed: session.busy ? null : session.manageSubscription,
                    icon: const Icon(Icons.credit_card_rounded),
                    label: const Text(
                      'Ver ou atualizar cartão salvo na Stripe',
                    ),
                    style: TextButton.styleFrom(foregroundColor: _ink),
                  ),
                  if (session.errorMessage != null) ...[
                    const SizedBox(height: 12),
                    _InlineNotice(text: session.errorMessage!),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _SavedCardSummary extends StatelessWidget {
  const _SavedCardSummary({required this.card, required this.loaded});

  final AgendaBillingCard? card;
  final bool loaded;

  @override
  Widget build(BuildContext context) {
    final title = !loaded
        ? 'Consultando cartão salvo…'
        : card == null
        ? 'Nenhum cartão salvo nesta conta'
        : '${card!.displayBrand} terminado em ${card!.last4}';
    final detail = card == null
        ? 'A Stripe pedirá o cartão no Checkout seguro.'
        : card!.expMonth > 0 && card!.expYear > 0
        ? 'Validade ${card!.expMonth.toString().padLeft(2, '0')}/${card!.expYear}. Salvo e protegido pela Stripe.'
        : 'Salvo e protegido pela Stripe.';
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFAF6),
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Container(
            width: 46,
            height: 36,
            decoration: BoxDecoration(
              color: _ink,
              borderRadius: BorderRadius.circular(7),
            ),
            alignment: Alignment.center,
            child: const Icon(
              Icons.credit_card_rounded,
              color: Colors.white,
              size: 22,
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: _ink,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  detail,
                  style: const TextStyle(color: _muted, fontSize: 12),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PrimaryButton extends StatelessWidget {
  const _PrimaryButton({
    required this.label,
    required this.onPressed,
    required this.icon,
  });
  final String label;
  final VoidCallback? onPressed;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 58,
      child: FilledButton(
        onPressed: onPressed,
        style: FilledButton.styleFrom(
          backgroundColor: _orange,
          foregroundColor: Colors.white,
          disabledBackgroundColor: _orangeSoft,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
          textStyle: const TextStyle(fontWeight: FontWeight.w800),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(label),
            const SizedBox(width: 10),
            Icon(icon, size: 19),
          ],
        ),
      ),
    );
  }
}

class _OutlineButton extends StatelessWidget {
  const _OutlineButton({
    required this.label,
    required this.onPressed,
    this.icon,
  });
  final String label;
  final VoidCallback? onPressed;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 58,
      child: OutlinedButton.icon(
        onPressed: onPressed,
        icon: Icon(icon ?? Icons.login_rounded, size: 19),
        label: Text(label),
        style: OutlinedButton.styleFrom(
          foregroundColor: _orange,
          side: const BorderSide(color: _orange),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
          textStyle: const TextStyle(fontWeight: FontWeight.w800),
        ),
      ),
    );
  }
}

class _AccessChoice extends StatelessWidget {
  const _AccessChoice({
    required this.icon,
    required this.title,
    required this.detail,
  });
  final IconData icon;
  final String title;
  final String detail;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 14),
      child: Column(
        children: [
          Icon(icon, color: _orange, size: 40),
          const SizedBox(height: 12),
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: _ink,
              fontSize: 13,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            detail,
            textAlign: TextAlign.center,
            style: const TextStyle(color: _muted, fontSize: 10.5),
          ),
          const SizedBox(height: 8),
          const Icon(Icons.arrow_forward_rounded, color: _orange, size: 18),
        ],
      ),
    );
  }
}

class _InlineNotice extends StatelessWidget {
  const _InlineNotice({required this.text});
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(13),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF0E8),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFFFC7AA)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.info_outline_rounded, color: _orange, size: 19),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              text,
              style: const TextStyle(color: _ink, fontSize: 12.5, height: 1.4),
            ),
          ),
        ],
      ),
    );
  }
}
