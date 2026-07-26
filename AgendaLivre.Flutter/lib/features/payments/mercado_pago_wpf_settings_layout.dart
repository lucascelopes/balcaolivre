import 'package:flutter/material.dart';

import '../../app/theme/agenda_theme.dart';
import '../../services/mercado_pago_service.dart';
import 'mercado_pago_terminal_visuals.dart';

class MercadoPagoWpfSettingsLayout extends StatelessWidget {
  const MercadoPagoWpfSettingsLayout({
    super.key,
    required this.enabled,
    required this.busy,
    required this.polling,
    required this.connected,
    required this.ready,
    required this.showSetup,
    required this.businessName,
    required this.terminalId,
    required this.terminalLabel,
    required this.terminals,
    required this.selectedTerminalId,
    required this.error,
    required this.message,
    required this.onEnabledChanged,
    required this.onConnect,
    required this.onRefresh,
    required this.onTerminalChanged,
    required this.onChangeTerminal,
    required this.onSave,
    required this.onClose,
  });

  final bool enabled;
  final bool busy;
  final bool polling;
  final bool connected;
  final bool ready;
  final bool showSetup;
  final String businessName;
  final String terminalId;
  final String terminalLabel;
  final List<MercadoPagoTerminal> terminals;
  final String? selectedTerminalId;
  final String error;
  final String message;
  final ValueChanged<bool> onEnabledChanged;
  final VoidCallback onConnect;
  final VoidCallback onRefresh;
  final ValueChanged<String?> onTerminalChanged;
  final VoidCallback onChangeTerminal;
  final VoidCallback onSave;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 720;
    final body = SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        compact ? 16 : 22,
        compact ? 16 : 14,
        compact ? 16 : 22,
        16,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (compact) ...[
            _MobileSteps(connected: connected, ready: ready),
            const SizedBox(height: 14),
          ],
          _StatusSummary(
            enabled: enabled,
            ready: ready,
            connected: connected,
            terminalLabel: terminalLabel,
            busy: busy,
            onEnabledChanged: onEnabledChanged,
          ),
          const SizedBox(height: 14),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 18),
          if (ready && !showSetup)
            _ConnectedTerminalView(
              businessName: businessName,
              terminalId: terminalId,
              terminalLabel: terminalLabel,
              terminals: terminals,
              onRefresh: onRefresh,
              onChangeTerminal: onChangeTerminal,
              busy: busy,
            )
          else
            _SetupTerminalView(
              enabled: enabled,
              connected: connected,
              ready: ready,
              busy: busy,
              polling: polling,
              terminals: terminals,
              selectedTerminalId: selectedTerminalId,
              onConnect: onConnect,
              onRefresh: onRefresh,
              onTerminalChanged: onTerminalChanged,
            ),
          if (error.isNotEmpty || message.isNotEmpty) ...[
            const SizedBox(height: 12),
            _FeedbackStrip(
              error: error.isNotEmpty,
              message: error.isNotEmpty ? error : message,
            ),
          ],
        ],
      ),
    );

    return Dialog(
      key: const Key('mercado-pago-wpf-settings-dialog'),
      insetPadding: EdgeInsets.all(compact ? 8 : 24),
      backgroundColor: Colors.transparent,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 940,
          maxHeight: compact
              ? MediaQuery.sizeOf(context).height * .92
              : MediaQuery.sizeOf(context).height * .92 < 660
              ? MediaQuery.sizeOf(context).height * .92
              : 660,
        ),
        child: RepaintBoundary(
          key: const Key('mercado-pago-wpf-settings-capture'),
          child: Material(
            color: t.panel,
            elevation: 20,
            shadowColor: Colors.black.withValues(alpha: .18),
            clipBehavior: Clip.antiAlias,
            borderRadius: BorderRadius.circular(16),
            child: Column(
              children: [
                _Header(compact: compact, onClose: busy ? null : onClose),
                Divider(height: 1, color: t.line),
                Expanded(
                  child: compact
                      ? body
                      : Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            SizedBox(
                              width: 220,
                              child: _DesktopSteps(
                                connected: connected,
                                ready: ready,
                              ),
                            ),
                            VerticalDivider(width: 1, color: t.line),
                            Expanded(child: body),
                          ],
                        ),
                ),
                Divider(height: 1, color: t.line),
                _Footer(
                  compact: compact,
                  busy: busy,
                  onCancel: onClose,
                  onSave: onSave,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.compact, required this.onClose});

  final bool compact;
  final VoidCallback? onClose;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: EdgeInsets.fromLTRB(
        compact ? 16 : 22,
        compact ? 13 : 16,
        compact ? 8 : 12,
        compact ? 13 : 16,
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(13),
            ),
            child: Icon(
              Icons.credit_card_outlined,
              color: t.accentDark,
              size: 21,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Mercado Pago',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: compact ? 19 : 22,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  'Ative a conta e escolha a Point usada nos pagamentos da agenda.',
                  maxLines: 2,
                  style: TextStyle(
                    color: t.muted,
                    fontSize: compact ? 10.5 : 11.5,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Fechar',
            onPressed: onClose,
            icon: const Icon(Icons.close_rounded, size: 21),
          ),
        ],
      ),
    );
  }
}

class _DesktopSteps extends StatelessWidget {
  const _DesktopSteps({required this.connected, required this.ready});

  final bool connected;
  final bool ready;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: const Color(0xFFFFFDFC),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 24, 16, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Configuração',
              style: TextStyle(
                color: t.ink,
                fontSize: 15,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 20),
            _SideStep(
              icon: Icons.link_rounded,
              title: 'Conta',
              subtitle: connected ? 'Conectada.' : 'Conecte sua conta.',
              complete: connected,
            ),
            const SizedBox(height: 20),
            _SideStep(
              icon: Icons.verified_user_outlined,
              title: 'Verificação',
              subtitle: connected ? 'Concluída.' : 'Verifique o acesso.',
              complete: connected,
            ),
            const SizedBox(height: 20),
            _SideStep(
              icon: Icons.credit_card_outlined,
              title: 'Maquininha',
              subtitle: ready ? 'Encontrada.' : 'Encontre uma Point.',
              complete: ready,
            ),
          ],
        ),
      ),
    );
  }
}

class _SideStep extends StatelessWidget {
  const _SideStep({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.complete,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final bool complete;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final color = complete ? const Color(0xFF159447) : t.accent;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            color: complete ? const Color(0xFFECFDF3) : t.accentSoft,
            border: Border.all(color: color.withValues(alpha: .55)),
            shape: BoxShape.circle,
          ),
          child: Icon(
            complete ? Icons.check_rounded : icon,
            color: color,
            size: 20,
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(top: 3),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  subtitle,
                  style: TextStyle(
                    color: complete ? const Color(0xFF15803D) : t.muted,
                    fontSize: 11,
                    fontWeight: complete ? FontWeight.w700 : FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _MobileSteps extends StatelessWidget {
  const _MobileSteps({required this.connected, required this.ready});

  final bool connected;
  final bool ready;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    Widget step(IconData icon, String label, bool complete) {
      final color = complete ? const Color(0xFF159447) : t.accent;
      return Expanded(
        child: Column(
          children: [
            Container(
              width: 34,
              height: 34,
              decoration: BoxDecoration(
                color: complete ? const Color(0xFFECFDF3) : t.accentSoft,
                shape: BoxShape.circle,
              ),
              child: Icon(
                complete ? Icons.check_rounded : icon,
                color: color,
                size: 18,
              ),
            ),
            const SizedBox(height: 5),
            Text(
              label,
              textAlign: TextAlign.center,
              style: TextStyle(
                color: t.ink,
                fontSize: 10.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      );
    }

    return Row(
      children: [
        step(Icons.link_rounded, 'Conta', connected),
        step(Icons.verified_user_outlined, 'Verificação', connected),
        step(Icons.credit_card_outlined, 'Maquininha', ready),
      ],
    );
  }
}

class _StatusSummary extends StatelessWidget {
  const _StatusSummary({
    required this.enabled,
    required this.ready,
    required this.connected,
    required this.terminalLabel,
    required this.busy,
    required this.onEnabledChanged,
  });

  final bool enabled;
  final bool ready;
  final bool connected;
  final String terminalLabel;
  final bool busy;
  final ValueChanged<bool> onEnabledChanged;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 620;
    final status = ready
        ? 'Pronto'
        : connected
        ? 'Falta escolher a Point'
        : 'Falta conectar';
    final statusColor = ready
        ? const Color(0xFF15803D)
        : const Color(0xFFC2410C);
    final summary = Row(
      children: [
        Container(
          width: 46,
          height: 46,
          decoration: BoxDecoration(
            color: t.accentSoft,
            borderRadius: BorderRadius.circular(13),
          ),
          child: Icon(Icons.storefront_outlined, color: t.accentDark, size: 22),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Wrap(
                spacing: 8,
                runSpacing: 5,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  Text(
                    'Mercado Pago na agenda',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 17,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: statusColor.withValues(alpha: .11),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      status,
                      style: TextStyle(
                        color: statusColor,
                        fontSize: 10.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 3),
              Text(
                ready
                    ? 'Pronto para cobrar em ${terminalLabel.trim().isEmpty ? 'sua Point' : terminalLabel}.'
                    : 'Ative para cobrar cartão na Point e registrar o pagamento só depois da aprovação.',
                style: TextStyle(color: t.muted, fontSize: 11.5),
              ),
            ],
          ),
        ),
      ],
    );
    final toggle = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          'Usar Mercado Pago',
          style: TextStyle(
            color: t.ink,
            fontSize: 12,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(width: 8),
        Switch.adaptive(
          key: const Key('mercado-pago-enabled-switch'),
          value: enabled,
          onChanged: busy ? null : onEnabledChanged,
        ),
      ],
    );
    if (compact) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          summary,
          const SizedBox(height: 10),
          Align(alignment: Alignment.centerRight, child: toggle),
        ],
      );
    }
    return Row(
      children: [
        Expanded(child: summary),
        const SizedBox(width: 20),
        toggle,
      ],
    );
  }
}

class _SetupTerminalView extends StatelessWidget {
  const _SetupTerminalView({
    required this.enabled,
    required this.connected,
    required this.ready,
    required this.busy,
    required this.polling,
    required this.terminals,
    required this.selectedTerminalId,
    required this.onConnect,
    required this.onRefresh,
    required this.onTerminalChanged,
  });

  final bool enabled;
  final bool connected;
  final bool ready;
  final bool busy;
  final bool polling;
  final List<MercadoPagoTerminal> terminals;
  final String? selectedTerminalId;
  final VoidCallback onConnect;
  final VoidCallback onRefresh;
  final ValueChanged<String?> onTerminalChanged;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final stacked = MediaQuery.sizeOf(context).width < 850;
    final steps = <Widget>[
      _ProcessStep(
        icon: Icons.link_rounded,
        title: 'Conectar conta',
        subtitle: 'Conecte sua conta do Mercado Pago à agenda.',
        complete: connected,
        button: FilledButton(
          key: const Key('mercado-pago-connect'),
          // Connecting is also the action that enables Mercado Pago. Keeping
          // this CTA disabled until the switch is enabled creates a dead end
          // for first-time setup.
          onPressed: busy ? null : onConnect,
          style: FilledButton.styleFrom(
            backgroundColor: t.accent,
            foregroundColor: Colors.black,
            minimumSize: const Size.fromHeight(42),
          ),
          child: Text(connected ? 'Reconectar' : 'Conectar'),
        ),
      ),
      _ProcessStep(
        icon: Icons.verified_user_outlined,
        title: 'Verificar conta',
        subtitle: 'Verificaremos o acesso à sua conta.',
        complete: connected,
        button: OutlinedButton(
          key: const Key('mercado-pago-refresh-status'),
          onPressed: !enabled || busy ? null : onRefresh,
          style: OutlinedButton.styleFrom(
            foregroundColor: t.ink,
            minimumSize: const Size.fromHeight(42),
            side: BorderSide(color: t.line),
          ),
          child: Text(polling ? 'Aguardando...' : 'Checar conta'),
        ),
      ),
      _ProcessStep(
        icon: Icons.credit_card_outlined,
        title: 'Encontrar maquininha',
        subtitle: 'Encontre e selecione a Point da sua loja.',
        complete: ready,
        button: OutlinedButton(
          onPressed: !enabled || !connected || busy ? null : onRefresh,
          style: OutlinedButton.styleFrom(
            foregroundColor: t.ink,
            minimumSize: const Size.fromHeight(42),
            side: BorderSide(color: t.line),
          ),
          child: const Text('Buscar Points'),
        ),
      ),
    ];
    final validSelection =
        selectedTerminalId != null &&
        terminals.any((terminal) => terminal.id == selectedTerminalId);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (stacked)
          Column(
            children: [
              for (var index = 0; index < steps.length; index++) ...[
                steps[index],
                if (index < steps.length - 1) const SizedBox(height: 14),
              ],
            ],
          )
        else
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (var index = 0; index < steps.length; index++) ...[
                Expanded(child: steps[index]),
                if (index < steps.length - 1) const SizedBox(width: 16),
              ],
            ],
          ),
        const SizedBox(height: 18),
        DropdownButtonFormField<String>(
          key: const Key('mercado-pago-terminal-select'),
          initialValue: validSelection ? selectedTerminalId : null,
          isExpanded: true,
          decoration: InputDecoration(
            labelText: 'Point da loja',
            prefixIcon: const Icon(Icons.point_of_sale_rounded),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
          ),
          hint: const Text('Selecione a Point da sua loja'),
          items: terminals
              .map(
                (terminal) => DropdownMenuItem<String>(
                  value: terminal.id,
                  child: Text(
                    terminal.display,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              )
              .toList(growable: false),
          onChanged: !enabled || !connected || busy ? null : onTerminalChanged,
        ),
        const SizedBox(height: 7),
        Text(
          connected
              ? 'Escolha a maquininha e salve a configuração.'
              : 'Conecte a conta e depois clique em Checar conta.',
          style: TextStyle(
            color: connected ? t.muted : const Color(0xFFC2410C),
            fontSize: 11.5,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 16),
        const _InfoStrip(
          title: 'Como funciona',
          message:
              'Conecte a conta, verifique o acesso e selecione a Point da loja.',
        ),
      ],
    );
  }
}

class _ProcessStep extends StatelessWidget {
  const _ProcessStep({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.complete,
    required this.button,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final bool complete;
  final Widget button;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final color = complete ? const Color(0xFF159447) : t.accent;
    return Column(
      children: [
        Container(
          width: 54,
          height: 54,
          decoration: BoxDecoration(
            color: complete ? const Color(0xFFECFDF3) : t.accentSoft,
            shape: BoxShape.circle,
          ),
          child: Icon(
            complete ? Icons.check_rounded : icon,
            color: color,
            size: 24,
          ),
        ),
        const SizedBox(height: 11),
        Text(
          title,
          textAlign: TextAlign.center,
          style: TextStyle(
            color: t.ink,
            fontSize: 14.5,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 5),
        SizedBox(
          height: 34,
          child: Text(
            subtitle,
            textAlign: TextAlign.center,
            maxLines: 2,
            style: TextStyle(color: t.muted, fontSize: 11.5),
          ),
        ),
        const SizedBox(height: 10),
        SizedBox(width: double.infinity, child: button),
      ],
    );
  }
}

class _ConnectedTerminalView extends StatelessWidget {
  const _ConnectedTerminalView({
    required this.businessName,
    required this.terminalId,
    required this.terminalLabel,
    required this.terminals,
    required this.onRefresh,
    required this.onChangeTerminal,
    required this.busy,
  });

  final String businessName;
  final String terminalId;
  final String terminalLabel;
  final List<MercadoPagoTerminal> terminals;
  final VoidCallback onRefresh;
  final VoidCallback onChangeTerminal;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final terminal = terminals.firstWhere(
      (item) => item.id == terminalId,
      orElse: () => MercadoPagoTerminal(
        id: terminalId,
        label: terminalLabel,
        operatingMode: 'PDV',
      ),
    );
    final visual = MercadoPagoTerminalVisual.resolve(
      terminalId: terminal.id,
      terminalLabel: terminal.display,
      modelCode: terminal.modelCode,
      modelName: terminal.modelName,
      serial: terminal.serial,
    );
    final stacked = MediaQuery.sizeOf(context).width < 820;
    final photo = Container(
      width: stacked ? double.infinity : 188,
      height: stacked ? 220 : 270,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFEEF8FF),
        border: Border.all(color: const Color(0xFFA9DFFF)),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        children: [
          Expanded(
            child: Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12),
              ),
              child: visual.assetPath.isEmpty
                  ? Icon(
                      Icons.point_of_sale_rounded,
                      color: t.accentDark,
                      size: 58,
                    )
                  : Image.asset(
                      visual.assetPath,
                      key: const Key('mercado-pago-settings-terminal-image'),
                      fit: BoxFit.contain,
                    ),
            ),
          ),
          const SizedBox(height: 10),
          Text(
            visual.modelCode.replaceAll('_', ' '),
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Color(0xFF0C6B94),
              fontSize: 11,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Modelo identificado pela conta',
            textAlign: TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 10.5),
          ),
        ],
      ),
    );
    final details = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Wrap(
          spacing: 10,
          runSpacing: 7,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            Text(
              visual.modelName,
              style: TextStyle(
                color: t.ink,
                fontSize: 22,
                fontWeight: FontWeight.w800,
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 5),
              decoration: BoxDecoration(
                color: const Color(0xFFDCFCE7),
                borderRadius: BorderRadius.circular(14),
              ),
              child: const Text(
                'Conectada',
                style: TextStyle(
                  color: Color(0xFF15803D),
                  fontSize: 10.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 6),
        Text(
          'Série ${visual.serial} · detectada automaticamente pelo Mercado Pago',
          style: TextStyle(
            color: t.muted,
            fontSize: 11.5,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 18),
        _DetailRow(
          icon: Icons.badge_outlined,
          label: 'Point ID',
          value: visual.serial,
        ),
        const SizedBox(height: 13),
        _DetailRow(
          icon: Icons.storefront_outlined,
          label: 'Loja',
          value: terminal.storeId.trim().isEmpty
              ? (businessName.trim().isEmpty ? 'Loja principal' : businessName)
              : terminal.storeId,
        ),
        const SizedBox(height: 13),
        const _DetailRow(
          icon: Icons.wifi_rounded,
          label: 'Status',
          value: 'Online',
          success: true,
        ),
        const SizedBox(height: 20),
        Wrap(
          spacing: 10,
          runSpacing: 10,
          children: [
            OutlinedButton(
              onPressed: busy ? null : onRefresh,
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(164, 42),
                foregroundColor: t.ink,
                side: BorderSide(color: t.line),
              ),
              child: const Text('Atualizar status'),
            ),
            OutlinedButton(
              onPressed: busy ? null : onChangeTerminal,
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(164, 42),
                foregroundColor: t.ink,
                side: BorderSide(color: t.line),
              ),
              child: const Text('Trocar maquininha'),
            ),
          ],
        ),
      ],
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Maquininha conectada',
          style: TextStyle(
            color: t.ink,
            fontSize: 18,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 16),
        if (stacked)
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [photo, const SizedBox(height: 18), details],
          )
        else
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              photo,
              const SizedBox(width: 34),
              Expanded(child: details),
            ],
          ),
        const SizedBox(height: 20),
        const _InfoStrip(
          title: '',
          message:
              'O modelo e o número de série acima vêm diretamente do terminal vinculado no Mercado Pago.',
        ),
      ],
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
    this.success = false,
  });

  final IconData icon;
  final String label;
  final String value;
  final bool success;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        Icon(icon, size: 18, color: t.muted),
        const SizedBox(width: 12),
        SizedBox(
          width: 100,
          child: Text(label, style: TextStyle(color: t.muted, fontSize: 11.5)),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              color: success ? const Color(0xFF15803D) : t.ink,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    );
  }
}

class _InfoStrip extends StatelessWidget {
  const _InfoStrip({required this.title, required this.message});

  final String title;
  final String message;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: t.accentSoft,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.info_outline_rounded, color: t.accentDark, size: 19),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (title.isNotEmpty) ...[
                  Text(
                    title,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 2),
                ],
                Text(message, style: TextStyle(color: t.muted, fontSize: 11)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _FeedbackStrip extends StatelessWidget {
  const _FeedbackStrip({required this.error, required this.message});

  final bool error;
  final String message;

  @override
  Widget build(BuildContext context) {
    final color = error ? const Color(0xFFB91C1C) : const Color(0xFF166534);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 11),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .08),
        border: Border.all(color: color.withValues(alpha: .22)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(
            error ? Icons.error_outline_rounded : Icons.check_circle_outline,
            color: color,
            size: 18,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: TextStyle(
                color: color,
                fontSize: 11.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Footer extends StatelessWidget {
  const _Footer({
    required this.compact,
    required this.busy,
    required this.onCancel,
    required this.onSave,
  });

  final bool compact;
  final bool busy;
  final VoidCallback onCancel;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final cancel = OutlinedButton(
      onPressed: busy ? null : onCancel,
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(0, 42),
        foregroundColor: t.ink,
        side: BorderSide(color: t.line),
      ),
      child: const Text('Cancelar'),
    );
    final save = FilledButton(
      key: const Key('mercado-pago-save'),
      onPressed: busy ? null : onSave,
      style: FilledButton.styleFrom(
        minimumSize: const Size(0, 42),
        backgroundColor: t.accent,
        foregroundColor: Colors.black,
      ),
      child: const Text(
        'Salvar configuração',
        maxLines: 1,
        style: TextStyle(fontSize: 12),
      ),
    );
    return Padding(
      padding: EdgeInsets.fromLTRB(
        compact ? 14 : 22,
        14,
        compact ? 14 : 22,
        16,
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.end,
        children: [
          if (compact) Expanded(flex: 2, child: cancel) else cancel,
          const SizedBox(width: 10),
          if (compact)
            Expanded(flex: 3, child: save)
          else
            SizedBox(width: 166, child: save),
        ],
      ),
    );
  }
}
