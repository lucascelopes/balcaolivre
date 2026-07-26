// Legacy dialogs remain available for data portability and theme recovery.
// ignore_for_file: unused_element, unused_element_parameter

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/ui.dart';
import '../../domain/models/agenda_settings.dart';
import '../../services/via_cep_service.dart';
import '../establishment/editor_dialogs.dart';
import '../instagram/instagram_settings_dialog.dart';
import '../payments/mercado_pago_settings_dialog.dart';
import '../whatsapp/whatsapp_panel.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final settings = controller.data.settings;
        return LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 650;
            return SingleChildScrollView(
              padding: EdgeInsets.fromLTRB(
                compact ? 14 : 22,
                compact ? 16 : 20,
                compact ? 14 : 22,
                28,
              ),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 1380),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        'Configurações',
                        style: TextStyle(
                          color: AgendaThemeTokens.of(context).ink,
                          fontSize: compact ? 25 : 29,
                          fontWeight: FontWeight.w800,
                          height: 1.08,
                        ),
                      ),
                      const SizedBox(height: 5),
                      Text(
                        'Gerencie as preferências do sistema, integrações e configurações do seu negócio.',
                        style: TextStyle(
                          color: AgendaThemeTokens.of(context).muted,
                          fontSize: 13,
                        ),
                      ),
                      const SizedBox(height: 18),
                      _BusinessSummary(
                        settings: settings,
                        onEdit: () => _editBusiness(context),
                      ),
                      const SizedBox(height: 14),
                      _settingsColumns(context, settings),
                      const SizedBox(height: 14),
                      _SettingsAccessFooter(
                        onEdit: () => _editBusiness(context),
                        onExit: () => _exitSystem(context),
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        );
      },
    );
  }

  Widget _settingsColumns(BuildContext context, AgendaSettings settings) {
    final registrations = _RegistrationSettings(
      settings: settings,
      serviceCount: controller.data.services.length,
      professionalCount: controller.data.professionals.length,
      productCount: controller.data.products.length,
      onHours: () => _editHours(context),
      onServices: () => _createService(context),
      onProfessionals: () => _createProfessional(context),
      onProducts: () =>
          showProductManagerDialog(context, controller: controller),
    );
    final integrations = _IntegrationsAndAccess(
      settings: settings,
      onWhatsApp: () => showAgendaWhatsAppPanel(context, controller),
      onInstagram: () => showInstagramSettingsDialog(context, controller),
      onMercadoPago: () => showMercadoPagoSettingsDialog(context, controller),
    );

    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 900) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [registrations, const SizedBox(height: 12), integrations],
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(flex: 108, child: registrations),
            const SizedBox(width: 18),
            Expanded(flex: 100, child: integrations),
          ],
        );
      },
    );
  }

  Future<void> _createService(BuildContext context) async {
    final saved = await showServiceEditorDialog(
      context,
      controller: controller,
    );
    if (saved && context.mounted) {
      _showMessage(context, 'Serviço criado.');
    }
  }

  Future<void> _createProfessional(BuildContext context) async {
    final saved = await showProfessionalEditorDialog(
      context,
      controller: controller,
    );
    if (saved && context.mounted) {
      _showMessage(context, 'Profissional criado.');
    }
  }

  Future<void> _editBusiness(BuildContext context) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => _BusinessDataDialog(controller: controller),
    );
    if (saved != true || !context.mounted) return;
    _showMessage(context, 'Dados do negócio atualizados.');
  }

  Future<void> _editTheme(BuildContext context) async {
    final selected = await showDialog<String>(
      context: context,
      builder: (_) =>
          _ThemeDialog(selectedId: controller.data.settings.themeId),
    );
    if (selected == null) return;
    await controller.updateSettings((settings) => settings.themeId = selected);
    if (!context.mounted) return;
    _showMessage(context, 'Tema aplicado ao sistema.');
  }

  Future<void> _editHours(BuildContext context) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => _BusinessHoursDialog(controller: controller),
    );
    if (saved != true || !context.mounted) return;
    _showMessage(context, 'Horários e recursos atualizados.');
  }

  Future<void> _restartOnboarding(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Refazer configuração inicial?'),
        content: const Text(
          'O assistente será aberto novamente. Seus clientes, serviços e agendamentos não serão apagados.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          ElevatedButton.icon(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            icon: const Icon(Icons.restart_alt_rounded, size: 18),
            label: const Text('Refazer agora'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    await controller.restartOnboarding();
    if (!context.mounted) return;
    _showMessage(context, 'Assistente de configuração reiniciado.');
  }

  Future<void> _exitSystem(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (_) =>
          _ExitSystemDialog(authenticated: controller.hasAuthenticatedSession),
    );
    if (confirmed != true) return;
    try {
      await controller.logoutOrExit();
    } catch (_) {
      if (!context.mounted) return;
      _showMessage(
        context,
        'Não foi possível sair. Seus dados foram mantidos.',
      );
    }
  }

  Future<void> _copyBackup(BuildContext context) async {
    await Clipboard.setData(ClipboardData(text: controller.exportJson()));
    if (!context.mounted) return;
    _showMessage(context, 'Backup JSON copiado.');
  }

  Future<void> _importBackup(BuildContext context) async {
    final imported = await showDialog<bool>(
      context: context,
      builder: (_) => _ImportBackupDialog(controller: controller),
    );
    if (imported != true || !context.mounted) return;
    _showMessage(context, 'Backup importado com sucesso.');
  }

  void _showMessage(BuildContext context, String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  static String _businessLine(AgendaSettings settings) => [
    settings.businessName,
    settings.businessSegment,
    settings.businessPhone,
  ].where((item) => item.trim().isNotEmpty).join(' • ');
}

class _BusinessSummary extends StatelessWidget {
  const _BusinessSummary({required this.settings, required this.onEdit});

  final AgendaSettings settings;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final title = Row(
            children: [
              AgendaIconBadge(
                Icons.storefront_rounded,
                size: 44,
                iconSize: 21,
                color: tokens.ink,
                background: const Color(0xFFEEF1F4),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Dados do negócio',
                  style: TextStyle(
                    color: tokens.ink,
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          );
          final name = _BusinessSummaryValue(
            label: 'Nome do negócio',
            value: settings.businessName.trim().isEmpty
                ? 'Não informado'
                : settings.businessName,
          );
          final segment = _BusinessSummaryValue(
            label: 'Segmento',
            value: settings.businessSegment.trim().isEmpty
                ? 'Segmento não informado'
                : settings.businessSegment,
            divided: constraints.maxWidth >= 720,
          );
          final edit = OutlinedButton.icon(
            key: const Key('business-edit-open'),
            onPressed: onEdit,
            icon: const Icon(Icons.edit_outlined, size: 16),
            label: const Text('Editar dados'),
          );
          if (constraints.maxWidth < 720) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                title,
                const SizedBox(height: 14),
                name,
                const SizedBox(height: 10),
                segment,
                const SizedBox(height: 14),
                edit,
              ],
            );
          }
          return Row(
            children: [
              Expanded(flex: 11, child: title),
              Expanded(flex: 8, child: name),
              Expanded(flex: 7, child: segment),
              const SizedBox(width: 18),
              edit,
            ],
          );
        },
      ),
    );
  }
}

class _BusinessSummaryValue extends StatelessWidget {
  const _BusinessSummaryValue({
    required this.label,
    required this.value,
    this.divided = false,
  });

  final String label;
  final String value;
  final bool divided;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: EdgeInsets.only(left: divided ? 20 : 0),
      decoration: divided
          ? BoxDecoration(
              border: Border(left: BorderSide(color: t.line)),
            )
          : null,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: TextStyle(color: t.muted, fontSize: 11.5)),
          const SizedBox(height: 3),
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: t.ink,
              fontSize: 14,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _RegistrationSettings extends StatelessWidget {
  const _RegistrationSettings({
    required this.settings,
    required this.serviceCount,
    required this.professionalCount,
    required this.productCount,
    required this.onHours,
    required this.onServices,
    required this.onProfessionals,
    required this.onProducts,
  });

  final AgendaSettings settings;
  final int serviceCount;
  final int professionalCount;
  final int productCount;
  final VoidCallback onHours;
  final VoidCallback onServices;
  final VoidCallback onProfessionals;
  final VoidCallback onProducts;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 16, 20, 8),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Agenda e operação',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
                ),
                SizedBox(height: 3),
                Text(
                  'Configure o que aparece no atendimento e na rotina da agenda.',
                  style: TextStyle(fontSize: 12),
                ),
              ],
            ),
          ),
          _SettingsOperationRow(
            icon: Icons.sell_rounded,
            title: 'Serviços',
            value:
                '$serviceCount serviço${serviceCount == 1 ? '' : 's'} cadastrado${serviceCount == 1 ? '' : 's'}',
            detail: 'Valores, duração e itens oferecidos.',
            actionLabel: 'Criar serviço',
            onPressed: onServices,
          ),
          Divider(height: 1, color: tokens.line),
          _SettingsOperationRow(
            icon: Icons.groups_2_rounded,
            title: 'Profissionais',
            value:
                '$professionalCount profissional${professionalCount == 1 ? '' : 'is'} cadastrado${professionalCount == 1 ? '' : 's'}',
            detail: 'Equipe, funções e responsáveis pela agenda.',
            actionLabel: 'Criar profissional',
            onPressed: onProfessionals,
          ),
          Divider(height: 1, color: tokens.line),
          _SettingsOperationRow(
            icon: Icons.inventory_2_outlined,
            title: 'Produtos',
            value:
                '$productCount produto${productCount == 1 ? '' : 's'} cadastrado${productCount == 1 ? '' : 's'}',
            detail: 'Estoque, categoria e preço de venda no balcão.',
            actionLabel: 'Gerenciar produtos',
            onPressed: onProducts,
          ),
          Divider(height: 1, color: tokens.line),
          _SettingsOperationRow(
            icon: Icons.calendar_month_rounded,
            title: 'Horários',
            value: _scheduleSummary(settings),
            detail: 'Dias, horários, intervalos e regras da agenda.',
            actionLabel: 'Configurar horários',
            onPressed: onHours,
          ),
        ],
      ),
    );
  }

  static String _scheduleSummary(AgendaSettings settings) {
    const dayLabels = <int, String>{
      0: 'dom',
      1: 'seg',
      2: 'ter',
      3: 'qua',
      4: 'qui',
      5: 'sex',
      6: 'sáb',
    };
    final days = [...settings.workdays]..sort();
    final dayLine = days.length == 6 && !days.contains(0)
        ? 'Seg a sáb'
        : days.map((day) => dayLabels[day]).whereType<String>().join(', ');
    return '$dayLine • ${_hourLabel(settings.workdayStartHour)} às ${_hourLabel(settings.workdayEndHour)}';
  }
}

class _SettingsOperationRow extends StatelessWidget {
  const _SettingsOperationRow({
    required this.icon,
    required this.title,
    required this.value,
    required this.detail,
    required this.actionLabel,
    required this.onPressed,
  });

  final IconData icon;
  final String title;
  final String value;
  final String detail;
  final String actionLabel;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 11),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final identity = Row(
            children: [
              AgendaIconBadge(
                icon,
                size: 40,
                iconSize: 20,
                color: t.ink,
                background: const Color(0xFFEEF1F4),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 15.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      value,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.ink, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
            ],
          );
          final action = ElevatedButton.icon(
            onPressed: onPressed,
            style: ElevatedButton.styleFrom(minimumSize: const Size(150, 40)),
            icon: const Icon(Icons.add_rounded, size: 16),
            label: Text(actionLabel),
          );
          if (constraints.maxWidth < 400) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                identity,
                const SizedBox(height: 8),
                Text(detail, style: TextStyle(color: t.muted, fontSize: 11.5)),
                const SizedBox(height: 10),
                action,
              ],
            );
          }
          return Row(
            children: [
              Expanded(flex: 12, child: identity),
              const SizedBox(width: 10),
              Expanded(
                flex: 8,
                child: Text(
                  detail,
                  style: TextStyle(color: t.muted, fontSize: 11.5),
                ),
              ),
              const SizedBox(width: 12),
              action,
            ],
          );
        },
      ),
    );
  }
}

class _IntegrationsAndAccess extends StatelessWidget {
  const _IntegrationsAndAccess({
    required this.settings,
    required this.onWhatsApp,
    required this.onInstagram,
    required this.onMercadoPago,
  });

  final AgendaSettings settings;
  final VoidCallback onWhatsApp;
  final VoidCallback onInstagram;
  final VoidCallback onMercadoPago;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 14, 20, 6),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Integrações',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
                ),
                SizedBox(height: 3),
                Text(
                  'Conexões e opções importantes do sistema.',
                  style: TextStyle(fontSize: 12),
                ),
              ],
            ),
          ),
          _SettingsIntegrationRow(
            icon: Icons.chat_bubble_outline_rounded,
            iconColor: const Color(0xFF16A34A),
            iconBackground: const Color(0xFFDCFCE7),
            title: 'WhatsApp automático',
            status: settings.whatsAppLinked ? 'Conectado' : 'Não linkado',
            detail: settings.whatsAppLinked
                ? 'Conectado como ${settings.whatsAppConnectedName.isEmpty ? settings.whatsAppStorePhone : settings.whatsAppConnectedName}'
                : 'Confirme horários e chame clientes pelo app.',
            active: settings.whatsAppLinked,
            primaryLabel: settings.whatsAppLinked
                ? 'Gerenciar'
                : 'Linkar WhatsApp',
            secondaryLabel: 'Abrir painel',
            onPrimary: onWhatsApp,
            onSecondary: onWhatsApp,
          ),
          Divider(height: 1, color: tokens.line),
          _SettingsIntegrationRow(
            icon: Icons.camera_alt_outlined,
            iconColor: const Color(0xFFC13584),
            iconBackground: const Color(0xFFFCE7F3),
            title: 'Instagram profissional',
            status: settings.instagramLinked
                ? 'Conectado'
                : settings.instagramState == 'aguardando_oauth'
                ? 'Aguardando autorização'
                : 'Não conectado',
            detail: settings.instagramLinked
                ? (settings.instagramDisplayName.trim().isEmpty
                      ? '@${settings.instagramUsername}'
                      : settings.instagramDisplayName)
                : settings.instagramLastError.trim().isEmpty
                ? 'Conecte uma conta profissional Business ou Creator.'
                : settings.instagramLastError,
            active: settings.instagramLinked,
            primaryLabel: settings.instagramLinked ? 'Gerenciar' : 'Conectar',
            secondaryLabel: settings.instagramLinked
                ? 'Abrir Direct'
                : 'Ver status',
            onPrimary: onInstagram,
            onSecondary: onInstagram,
          ),
          Divider(height: 1, color: tokens.line),
          _SettingsIntegrationRow(
            icon: Icons.credit_card_outlined,
            iconColor: tokens.ink,
            iconBackground: const Color(0xFFEEF1F4),
            title: 'Mercado Pago',
            status: settings.mercadoPagoConnected ? 'Conectado' : 'Desativado',
            detail: settings.mercadoPagoConnected
                ? 'Terminal ${settings.mercadoPagoDefaultTerminalLabel.isEmpty ? settings.mercadoPagoDefaultTerminalId : settings.mercadoPagoDefaultTerminalLabel}'
                : 'Ative cartão na maquininha.',
            active: settings.mercadoPagoConnected,
            primaryLabel: settings.mercadoPagoConnected
                ? 'Gerenciar'
                : 'Configurar',
            primaryOutlined: true,
            onPrimary: onMercadoPago,
          ),
        ],
      ),
    );
  }
}

class _SettingsIntegrationRow extends StatelessWidget {
  const _SettingsIntegrationRow({
    required this.icon,
    required this.iconColor,
    required this.iconBackground,
    required this.title,
    required this.status,
    required this.detail,
    required this.active,
    required this.primaryLabel,
    required this.onPrimary,
    this.primaryOutlined = false,
    this.secondaryLabel,
    this.onSecondary,
  });

  final IconData icon;
  final Color iconColor;
  final Color iconBackground;
  final String title;
  final String status;
  final String detail;
  final bool active;
  final String primaryLabel;
  final VoidCallback onPrimary;
  final bool primaryOutlined;
  final String? secondaryLabel;
  final VoidCallback? onSecondary;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 7),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final identity = Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AgendaIconBadge(
                icon,
                size: 40,
                iconSize: 21,
                color: iconColor,
                background: iconBackground,
              ),
              const SizedBox(width: 7),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 15.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 1),
                    Text(
                      status,
                      style: TextStyle(
                        color: active ? const Color(0xFF15803D) : t.muted,
                        fontSize: 11.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      detail,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.muted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
            ],
          );
          final primaryAction = primaryOutlined
              ? OutlinedButton(
                  onPressed: onPrimary,
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size(132, 34),
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                  ),
                  child: Text(primaryLabel),
                )
              : ElevatedButton(
                  onPressed: onPrimary,
                  style: ElevatedButton.styleFrom(
                    minimumSize: const Size(132, 34),
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                  ),
                  child: Text(primaryLabel),
                );
          final actions = Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              primaryAction,
              if (secondaryLabel != null && onSecondary != null) ...[
                const SizedBox(height: 4),
                OutlinedButton(
                  onPressed: onSecondary,
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size(132, 34),
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                  ),
                  child: Text(secondaryLabel!),
                ),
              ],
            ],
          );
          if (constraints.maxWidth < 380) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [identity, const SizedBox(height: 10), actions],
            );
          }
          return Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Expanded(child: identity),
              const SizedBox(width: 12),
              SizedBox(width: 132, child: actions),
            ],
          );
        },
      ),
    );
  }
}

class _SettingsAccessFooter extends StatelessWidget {
  const _SettingsAccessFooter({required this.onEdit, required this.onExit});

  final VoidCallback onEdit;
  final VoidCallback onExit;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final registration = _SettingsFooterAction(
      icon: Icons.admin_panel_settings_outlined,
      title: 'Cadastro inicial',
      subtitle: 'Revise setor, dados e senha.',
      actionLabel: 'Editar',
      onPressed: onEdit,
    );
    final exit = _SettingsFooterAction(
      key: const Key('settings-exit-action'),
      buttonKey: const Key('settings-exit-action-button'),
      icon: Icons.logout_rounded,
      title: 'Sair desta conta',
      subtitle: 'Seus dados permanecem salvos nesta conta.',
      actionLabel: 'Sair',
      iconColor: const Color(0xFFDC2626),
      iconBackground: const Color(0xFFFEE2E2),
      destructive: true,
      reserveFabClearance: true,
      onPressed: onExit,
    );
    return AgendaPanel(
      radius: 16,
      padding: EdgeInsets.zero,
      child: LayoutBuilder(
        builder: (context, constraints) {
          if (constraints.maxWidth < 720) {
            return Column(
              children: [
                registration,
                Divider(height: 1, color: t.line),
                exit,
              ],
            );
          }
          return IntrinsicHeight(
            child: Row(
              children: [
                Expanded(child: registration),
                VerticalDivider(
                  width: 1,
                  color: t.line,
                  indent: 12,
                  endIndent: 12,
                ),
                Expanded(child: exit),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _SettingsFooterAction extends StatelessWidget {
  const _SettingsFooterAction({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.actionLabel,
    required this.onPressed,
    this.buttonKey,
    this.iconColor,
    this.iconBackground,
    this.destructive = false,
    this.reserveFabClearance = false,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String actionLabel;
  final VoidCallback onPressed;
  final Key? buttonKey;
  final Color? iconColor;
  final Color? iconBackground;
  final bool destructive;
  final bool reserveFabClearance;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final trailingClearance =
        reserveFabClearance && MediaQuery.sizeOf(context).width >= 760
        ? 72.0
        : 0.0;
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 12, 20 + trailingClearance, 12),
      child: Row(
        children: [
          AgendaIconBadge(
            icon,
            size: 40,
            iconSize: 20,
            color: iconColor ?? t.ink,
            background: iconBackground ?? const Color(0xFFEEF1F4),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  subtitle,
                  style: TextStyle(color: t.muted, fontSize: 11.5),
                ),
              ],
            ),
          ),
          const SizedBox(width: 14),
          OutlinedButton(
            key: buttonKey,
            onPressed: onPressed,
            style: destructive ? _exitButtonStyle() : null,
            child: Text(actionLabel),
          ),
        ],
      ),
    );
  }
}

class _ImportBackupDialog extends StatefulWidget {
  const _ImportBackupDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_ImportBackupDialog> createState() => _ImportBackupDialogState();
}

class _ImportBackupDialogState extends State<_ImportBackupDialog> {
  final _json = TextEditingController();
  String? _error;
  bool _importing = false;

  @override
  void dispose() {
    _json.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Dialog(
      insetPadding: const EdgeInsets.all(20),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 680, maxHeight: 650),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const AgendaSectionTitle(
                title: 'Importar backup JSON',
                subtitle:
                    'Cole o conteúdo de agenda-data.json. IDs, datas e cadastros serão preservados.',
                icon: Icons.upload_file_rounded,
              ),
              const SizedBox(height: 16),
              Flexible(
                child: TextField(
                  controller: _json,
                  minLines: 9,
                  maxLines: 18,
                  style: const TextStyle(
                    fontFamily: 'monospace',
                    fontSize: 11.5,
                  ),
                  decoration: InputDecoration(
                    hintText: '{\n  "Settings": { ... }\n}',
                    alignLabelWithHint: true,
                    errorText: _error,
                  ),
                ),
              ),
              const SizedBox(height: 12),
              AgendaPanel(
                color: tokens.yellowSoft,
                padding: const EdgeInsets.all(11),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      Icons.info_outline_rounded,
                      color: tokens.accentDark,
                      size: 18,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'A importação substitui os dados atuais. Copie um backup antes se quiser poder voltar.',
                        style: TextStyle(
                          color: tokens.ink,
                          fontSize: 11.5,
                          height: 1.35,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 14),
              Row(
                children: [
                  OutlinedButton.icon(
                    onPressed: _importing ? null : _paste,
                    icon: const Icon(Icons.content_paste_rounded, size: 17),
                    label: const Text('Colar'),
                  ),
                  const Spacer(),
                  TextButton(
                    onPressed: _importing
                        ? null
                        : () => Navigator.of(context).pop(false),
                    child: const Text('Cancelar'),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton.icon(
                    onPressed: _importing ? null : _import,
                    icon: _importing
                        ? const SizedBox(
                            width: 15,
                            height: 15,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(Icons.check_rounded, size: 17),
                    label: const Text('Importar'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _paste() async {
    final value = await Clipboard.getData(Clipboard.kTextPlain);
    if (!mounted || value?.text == null) return;
    setState(() {
      _json.text = value!.text!;
      _error = null;
    });
  }

  Future<void> _import() async {
    if (_json.text.trim().isEmpty) {
      setState(() => _error = 'Cole o conteúdo do backup antes de importar.');
      return;
    }
    setState(() {
      _importing = true;
      _error = null;
    });
    final error = await widget.controller.importJson(_json.text);
    if (!mounted) return;
    if (error != null) {
      setState(() {
        _importing = false;
        _error = error;
      });
      return;
    }
    Navigator.of(context).pop(true);
  }
}

class _SettingsRow extends StatelessWidget {
  const _SettingsRow({
    required this.icon,
    required this.title,
    required this.value,
    required this.detail,
    required this.actionLabel,
    required this.onPressed,
    this.tone,
    this.softTone,
  });

  final IconData icon;
  final String title;
  final String value;
  final String detail;
  final String actionLabel;
  final VoidCallback onPressed;
  final Color? tone;
  final Color? softTone;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 13),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final information = Row(
            children: [
              AgendaIconBadge(
                icon,
                size: 42,
                iconSize: 20,
                color: tone,
                background: softTone,
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: TextStyle(
                        color: tokens.ink,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      value,
                      style: TextStyle(
                        color: tokens.accent,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      detail,
                      style: TextStyle(color: tokens.muted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
            ],
          );
          if (constraints.maxWidth < 570) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                information,
                const SizedBox(height: 10),
                OutlinedButton(onPressed: onPressed, child: Text(actionLabel)),
              ],
            );
          }
          return Row(
            children: [
              Expanded(child: information),
              const SizedBox(width: 12),
              OutlinedButton(onPressed: onPressed, child: Text(actionLabel)),
            ],
          );
        },
      ),
    );
  }
}

class _IntegrationTile extends StatelessWidget {
  const _IntegrationTile({
    required this.icon,
    required this.title,
    required this.detail,
    required this.active,
    required this.activeLabel,
  });

  final IconData icon;
  final String title;
  final String detail;
  final bool active;
  final String activeLabel;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    final tone = active ? const Color(0xFF16A34A) : tokens.accent;
    final soft = active ? const Color(0xFFDCFCE7) : tokens.accentSoft;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AgendaIconBadge(icon, color: tone, background: soft, size: 42),
          const SizedBox(width: 11),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        title,
                        style: TextStyle(
                          color: tokens.ink,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    AgendaPill(
                      label: activeLabel,
                      color: soft,
                      textColor: tone,
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  detail,
                  style: TextStyle(
                    color: tokens.muted,
                    fontSize: 11.5,
                    height: 1.35,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _AccessAction extends StatelessWidget {
  const _AccessAction({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return ListTile(
      onTap: onTap,
      contentPadding: const EdgeInsets.symmetric(horizontal: 17, vertical: 5),
      leading: AgendaIconBadge(
        icon,
        size: 42,
        color: tokens.accent,
        background: tokens.accentSoft,
      ),
      title: Text(
        title,
        style: TextStyle(color: tokens.ink, fontWeight: FontWeight.w800),
      ),
      subtitle: Text(subtitle),
      trailing: const Icon(Icons.chevron_right_rounded),
    );
  }
}

class _ExitSystemAction extends StatelessWidget {
  const _ExitSystemAction({super.key, required this.onExit});

  final VoidCallback onExit;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 12),
      decoration: const BoxDecoration(
        color: Color(0xFFFFF7F7),
        borderRadius: BorderRadius.vertical(bottom: Radius.circular(7)),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final information = Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              const AgendaIconBadge(
                Icons.logout_rounded,
                size: 38,
                iconSize: 19,
                color: Color(0xFFDC2626),
                background: Color(0xFFFEE2E2),
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Sair desta conta',
                      style: TextStyle(
                        color: tokens.ink,
                        fontSize: 16.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Seus dados permanecem salvos nesta conta.',
                      style: TextStyle(color: tokens.muted, fontSize: 12.5),
                    ),
                  ],
                ),
              ),
            ],
          );
          final compact = constraints.maxWidth < 340;
          final button = SizedBox(
            width: compact ? double.infinity : 136,
            height: compact ? 44 : 34,
            child: OutlinedButton(
              key: const Key('settings-exit-action-button'),
              onPressed: onExit,
              style: _exitButtonStyle(
                minimumSize: Size(compact ? 0 : 136, compact ? 44 : 34),
              ),
              child: const Text('Sair'),
            ),
          );
          if (compact) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [information, const SizedBox(height: 12), button],
            );
          }
          return Row(
            children: [
              Expanded(child: information),
              const SizedBox(width: 12),
              button,
            ],
          );
        },
      ),
    );
  }
}

class _ExitSystemDialog extends StatelessWidget {
  const _ExitSystemDialog({this.authenticated = false});

  final bool authenticated;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Dialog(
      backgroundColor: tokens.panel,
      elevation: 14,
      shadowColor: Colors.black.withValues(alpha: .18),
      insetPadding: const EdgeInsets.all(16),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(color: tokens.line),
      ),
      child: SizedBox(
        key: const Key('settings-exit-dialog'),
        width: 500,
        child: Padding(
          padding: const EdgeInsets.all(22),
          child: LayoutBuilder(
            builder: (context, _) {
              return Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Flexible(
                    fit: FlexFit.loose,
                    child: SingleChildScrollView(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const AgendaIconBadge(
                                Icons.logout_rounded,
                                size: 44,
                                iconSize: 24,
                                color: Color(0xFFDC2626),
                                background: Color(0xFFFEE2E2),
                              ),
                              const SizedBox(width: 16),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      authenticated
                                          ? 'Sair da conta?'
                                          : 'Sair desta conta?',
                                      style: TextStyle(
                                        color: tokens.ink,
                                        fontSize: 22,
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      authenticated
                                          ? 'Você precisará entrar novamente neste navegador.'
                                          : 'Você vai voltar para a configuração inicial.',
                                      style: TextStyle(
                                        color: tokens.muted,
                                        fontSize: 13,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 18),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 14,
                              vertical: 12,
                            ),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFFF9F4),
                              border: Border.all(color: tokens.line),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  authenticated
                                      ? 'Sua agenda continuará salva na conta.'
                                      : 'Clientes, serviços, agenda e financeiro continuam salvos.',
                                  style: TextStyle(
                                    color: tokens.ink,
                                    fontSize: 14,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  authenticated
                                      ? 'A sessão será encerrada; os dados desta conta não serão apagados.'
                                      : 'Dados do negócio, horários e Mercado Pago serão redefinidos.',
                                  style: TextStyle(
                                    color: tokens.muted,
                                    fontSize: 12.5,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 18),
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final compact = constraints.maxWidth < 270;
                      final cancel = SizedBox(
                        width: compact ? double.infinity : 128,
                        height: 40,
                        child: OutlinedButton(
                          key: const Key('settings-exit-cancel'),
                          onPressed: () => Navigator.of(context).pop(false),
                          child: const Text('Cancelar'),
                        ),
                      );
                      final confirm = SizedBox(
                        width: compact ? double.infinity : 128,
                        height: 40,
                        child: OutlinedButton(
                          key: const Key('settings-exit-confirm'),
                          onPressed: () => Navigator.of(context).pop(true),
                          style: _exitButtonStyle(
                            minimumSize: const Size(128, 40),
                          ),
                          child: const Text('Sair agora'),
                        ),
                      );
                      if (compact) {
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            confirm,
                            const SizedBox(height: 8),
                            cancel,
                          ],
                        );
                      }
                      return Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [cancel, const SizedBox(width: 10), confirm],
                      );
                    },
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

ButtonStyle _exitButtonStyle({Size minimumSize = const Size(84, 40)}) =>
    OutlinedButton.styleFrom(
      foregroundColor: const Color(0xFFB91C1C),
      backgroundColor: Colors.white,
      minimumSize: minimumSize,
      side: const BorderSide(color: Color(0xFFFECACA)),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      textStyle: const TextStyle(fontWeight: FontWeight.w600),
    );

class _BusinessDataDialog extends StatefulWidget {
  const _BusinessDataDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_BusinessDataDialog> createState() => _BusinessDataDialogState();
}

class _BusinessDataDialogState extends State<_BusinessDataDialog> {
  final _formKey = GlobalKey<FormState>();
  final ViaCepService _viaCep = ViaCepService();
  late final TextEditingController _accountName;
  late final TextEditingController _email;
  late final TextEditingController _businessName;
  late final TextEditingController _segment;
  late final TextEditingController _document;
  late final TextEditingController _phone;
  late final TextEditingController _postalCode;
  late final TextEditingController _neighborhood;
  late final TextEditingController _street;
  late final TextEditingController _number;
  late final TextEditingController _complement;
  bool _lookingUpCep = false;
  bool _saving = false;
  String? _cepError;

  @override
  void initState() {
    super.initState();
    final settings = widget.controller.data.settings;
    _accountName = TextEditingController(text: settings.accountFullName);
    _email = TextEditingController(text: widget.controller.accountEmail);
    _businessName = TextEditingController(text: settings.businessName);
    _segment = TextEditingController(text: settings.businessSegment);
    _document = TextEditingController(text: settings.businessDocument);
    _phone = TextEditingController(text: settings.businessPhone);
    _postalCode = TextEditingController(text: settings.postalCode);
    _neighborhood = TextEditingController(text: settings.neighborhood);
    _street = TextEditingController(text: settings.street);
    _number = TextEditingController(text: settings.addressNumber);
    _complement = TextEditingController(text: settings.addressComplement);
  }

  @override
  void dispose() {
    _accountName.dispose();
    _email.dispose();
    _businessName.dispose();
    _segment.dispose();
    _document.dispose();
    _phone.dispose();
    _postalCode.dispose();
    _neighborhood.dispose();
    _street.dispose();
    _number.dispose();
    _complement.dispose();
    super.dispose();
  }

  Future<void> _lookupCep() async {
    setState(() {
      _lookingUpCep = true;
      _cepError = null;
    });
    try {
      final address = await _viaCep.lookup(_postalCode.text);
      if (!mounted) return;
      if (address == null) {
        setState(() => _cepError = 'CEP não encontrado.');
        return;
      }
      setState(() {
        _postalCode.text = address.formattedCep;
        _neighborhood.text = address.neighborhood;
        _street.text = address.street;
        if (_complement.text.trim().isEmpty) {
          _complement.text = address.complement;
        }
      });
    } on ViaCepException {
      if (mounted) {
        setState(() => _cepError = 'Não foi possível consultar o CEP.');
      }
    } finally {
      if (mounted) setState(() => _lookingUpCep = false);
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    await widget.controller.updateSettings((settings) {
      settings
        ..accountFullName = _accountName.text.trim()
        ..accountEmail = widget.controller.accountEmail.isEmpty
            ? _email.text.trim()
            : widget.controller.accountEmail
        ..businessName = _businessName.text.trim()
        ..businessSegment = _segment.text.trim()
        ..businessDocument = _document.text.trim()
        ..businessPhone = _phone.text.trim()
        ..postalCode = ViaCepService.normalizeCep(_postalCode.text)
        ..neighborhood = _neighborhood.text.trim()
        ..street = _street.text.trim()
        ..addressNumber = _number.text.trim()
        ..addressComplement = _complement.text.trim()
        ..businessAddress = [
          _street.text.trim(),
          _number.text.trim(),
          _neighborhood.text.trim(),
          _complement.text.trim(),
        ].where((item) => item.isNotEmpty).join(', ');
    });
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }

  List<String> get _businessSegmentOptions {
    const defaults = <String>[
      'Barbearia',
      'Salão de Beleza',
      'Centro de Estética',
      'Esmalteria',
      'Podologia',
      'Spa',
      'Clínica médica',
      'Petshop',
      'Oficina',
      'Outro segmento',
    ];
    final current = _segment.text.trim();
    if (current.isEmpty || defaults.contains(current)) return defaults;
    return <String>[current, ...defaults];
  }

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    final size = MediaQuery.sizeOf(context);
    final compact = size.width < 720;
    final availableHeight = size.height - (compact ? 16 : 32);
    final dialogHeight = availableHeight < 680 ? availableHeight : 680.0;

    return Dialog(
      insetPadding: EdgeInsets.symmetric(
        horizontal: compact ? 8 : 16,
        vertical: compact ? 8 : 16,
      ),
      backgroundColor: Colors.transparent,
      elevation: 0,
      child: Semantics(
        namesRoute: true,
        label: 'Editar dados do negócio',
        child: SizedBox(
          key: const Key('business-data-dialog'),
          width: 760,
          height: dialogHeight,
          child: Material(
            color: tokens.appBackground,
            elevation: 18,
            shadowColor: Colors.black.withValues(alpha: .18),
            surfaceTintColor: Colors.transparent,
            borderRadius: BorderRadius.circular(20),
            clipBehavior: Clip.antiAlias,
            child: Column(
              children: [
                _BusinessDialogHeader(compact: compact),
                Expanded(
                  child: SingleChildScrollView(
                    key: const Key('business-dialog-scroll'),
                    padding: EdgeInsets.fromLTRB(
                      compact ? 14 : 22,
                      compact ? 14 : 20,
                      compact ? 14 : 22,
                      compact ? 14 : 20,
                    ),
                    child: Form(
                      key: _formKey,
                      child: LayoutBuilder(
                        builder: (context, constraints) {
                          final ownerCard = _BusinessEditorCard(
                            key: const Key('business-owner-card'),
                            title: 'Responsável',
                            subtitle: 'Quem administra a agenda.',
                            icon: Icons.account_circle_outlined,
                            iconBackground: tokens.accentSoft,
                            iconColor: tokens.accent,
                            child: Column(
                              children: [
                                _BusinessDialogField(
                                  fieldKey: const Key('business-owner-field'),
                                  label: 'Nome completo',
                                  hint: 'Ex: Isabella Gomes',
                                  controller: _accountName,
                                ),
                                const SizedBox(height: 11),
                                _BusinessDialogField(
                                  fieldKey: const Key('business-phone-field'),
                                  label: 'Celular / WhatsApp',
                                  hint: 'Ex: (33) 99800-7983',
                                  controller: _phone,
                                  keyboardType: TextInputType.phone,
                                ),
                                const SizedBox(height: 11),
                                _BusinessDialogField(
                                  fieldKey: const Key('business-email-field'),
                                  label: 'E-mail',
                                  hint: 'Ex: contato@empresa.com',
                                  controller: _email,
                                  keyboardType: TextInputType.emailAddress,
                                  readOnly: widget
                                      .controller
                                      .authenticatedEmail
                                      .isNotEmpty,
                                ),
                              ],
                            ),
                          );
                          final establishmentCard = _BusinessEditorCard(
                            key: const Key('business-establishment-card'),
                            title: 'Estabelecimento',
                            subtitle: 'Dados exibidos no sistema.',
                            icon: Icons.storefront_outlined,
                            iconBackground: const Color(0xFFEAFBF2),
                            iconColor: const Color(0xFF16A34A),
                            child: Column(
                              children: [
                                _BusinessDialogField(
                                  fieldKey: const Key('business-name-field'),
                                  label: 'Nome do negócio',
                                  hint: 'Ex: Marquinho Barbearia',
                                  controller: _businessName,
                                  validator: _required,
                                ),
                                const SizedBox(height: 11),
                                _BusinessLabeledField(
                                  label: 'Segmento',
                                  child: DropdownButtonFormField<String>(
                                    key: const Key('business-segment-field'),
                                    initialValue:
                                        _businessSegmentOptions.contains(
                                          _segment.text,
                                        )
                                        ? _segment.text
                                        : null,
                                    isExpanded: true,
                                    decoration: _businessInputDecoration(
                                      context,
                                      hint: 'Selecione o segmento',
                                    ),
                                    style: const TextStyle(fontSize: 13),
                                    icon: const Icon(
                                      Icons.keyboard_arrow_down_rounded,
                                    ),
                                    items: [
                                      for (final segment
                                          in _businessSegmentOptions)
                                        DropdownMenuItem(
                                          value: segment,
                                          child: Text(
                                            segment,
                                            overflow: TextOverflow.ellipsis,
                                          ),
                                        ),
                                    ],
                                    onChanged: _saving
                                        ? null
                                        : (value) {
                                            if (value != null) {
                                              _segment.text = value;
                                            }
                                          },
                                    validator: _required,
                                  ),
                                ),
                                const SizedBox(height: 11),
                                _BusinessDialogField(
                                  fieldKey: const Key(
                                    'business-document-field',
                                  ),
                                  label: 'CPF / CNPJ',
                                  hint: 'Ex: 123.456.789-00',
                                  controller: _document,
                                ),
                              ],
                            ),
                          );
                          final locationCard = _BusinessEditorCard(
                            key: const Key('business-location-card'),
                            title: 'Localização',
                            subtitle: 'Endereço de referência do negócio.',
                            icon: Icons.location_on_outlined,
                            iconBackground: const Color(0xFFF3E8FF),
                            iconColor: const Color(0xFF7C3AED),
                            child: LayoutBuilder(
                              builder: (context, locationConstraints) {
                                final narrow =
                                    locationConstraints.maxWidth < 520;
                                final cepField = _BusinessDialogField(
                                  fieldKey: const Key('business-cep-field'),
                                  label: 'CEP',
                                  hint: 'Ex: 35032-390',
                                  controller: _postalCode,
                                  keyboardType: TextInputType.number,
                                  errorText: _cepError,
                                );
                                final lookupButton = SizedBox(
                                  height: 39,
                                  width: narrow ? double.infinity : 148,
                                  child: OutlinedButton.icon(
                                    key: const Key('business-cep-lookup'),
                                    onPressed: _lookingUpCep
                                        ? null
                                        : _lookupCep,
                                    icon: _lookingUpCep
                                        ? const SizedBox.square(
                                            dimension: 15,
                                            child: CircularProgressIndicator(
                                              strokeWidth: 2,
                                            ),
                                          )
                                        : const Icon(
                                            Icons.search_rounded,
                                            size: 17,
                                          ),
                                    label: const Text('Consultar CEP'),
                                  ),
                                );
                                return Column(
                                  children: [
                                    if (narrow) ...[
                                      cepField,
                                      const SizedBox(height: 9),
                                      lookupButton,
                                    ] else
                                      Row(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Expanded(child: cepField),
                                          const SizedBox(width: 12),
                                          Padding(
                                            padding: const EdgeInsets.only(
                                              top: 22,
                                            ),
                                            child: lookupButton,
                                          ),
                                        ],
                                      ),
                                    const SizedBox(height: 12),
                                    _BusinessDialogColumns(
                                      firstFlex: 2,
                                      secondFlex: 4,
                                      first: _BusinessDialogField(
                                        fieldKey: const Key(
                                          'business-neighborhood-field',
                                        ),
                                        label: 'Bairro',
                                        hint: 'Ex: Lourdes',
                                        controller: _neighborhood,
                                      ),
                                      second: _BusinessDialogField(
                                        fieldKey: const Key(
                                          'business-street-field',
                                        ),
                                        label: 'Logradouro',
                                        hint: 'Ex: Rua Piracicaba',
                                        controller: _street,
                                      ),
                                    ),
                                    const SizedBox(height: 12),
                                    _BusinessDialogColumns(
                                      firstFlex: 2,
                                      secondFlex: 4,
                                      first: _BusinessDialogField(
                                        fieldKey: const Key(
                                          'business-number-field',
                                        ),
                                        label: 'Número',
                                        hint: 'Ex: 123',
                                        controller: _number,
                                      ),
                                      second: _BusinessDialogField(
                                        fieldKey: const Key(
                                          'business-complement-field',
                                        ),
                                        label: 'Complemento',
                                        hint: 'Ex: Sala 2',
                                        controller: _complement,
                                      ),
                                    ),
                                  ],
                                );
                              },
                            ),
                          );

                          if (constraints.maxWidth < 680) {
                            return Column(
                              children: [
                                ownerCard,
                                const SizedBox(height: 14),
                                establishmentCard,
                                const SizedBox(height: 14),
                                locationCard,
                              ],
                            );
                          }
                          return Column(
                            children: [
                              IntrinsicHeight(
                                child: Row(
                                  crossAxisAlignment:
                                      CrossAxisAlignment.stretch,
                                  children: [
                                    Expanded(child: ownerCard),
                                    const SizedBox(width: 16),
                                    Expanded(child: establishmentCard),
                                  ],
                                ),
                              ),
                              const SizedBox(height: 14),
                              locationCard,
                            ],
                          );
                        },
                      ),
                    ),
                  ),
                ),
                _BusinessDialogFooter(
                  saving: _saving,
                  onCancel: () => Navigator.of(context).pop(false),
                  onSave: _save,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _BusinessDialogHeader extends StatelessWidget {
  const _BusinessDialogHeader({required this.compact});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('business-dialog-header'),
      padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 18),
      decoration: BoxDecoration(
        color: tokens.panel,
        border: Border(bottom: BorderSide(color: tokens.line)),
      ),
      child: Row(
        children: [
          AgendaIconBadge(
            Icons.manage_accounts_outlined,
            size: 48,
            iconSize: 25,
            color: Colors.white,
            background: tokens.accent,
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Semantics(
                  header: true,
                  child: Text(
                    'Editar dados do negócio',
                    style: TextStyle(
                      color: tokens.ink,
                      fontSize: 23,
                      fontWeight: FontWeight.w800,
                      height: 1.1,
                    ),
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Atualize os dados salvos sem refazer a configuração inicial.',
                  style: TextStyle(
                    color: tokens.muted,
                    fontSize: 12.5,
                    height: 1.25,
                  ),
                ),
              ],
            ),
          ),
          if (!compact) ...[
            const SizedBox(width: 16),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: tokens.accentSoft,
                border: Border.all(color: tokens.line),
                borderRadius: BorderRadius.circular(18),
              ),
              child: Text(
                'Sem reiniciar',
                style: TextStyle(
                  color: tokens.accent,
                  fontSize: 12,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _BusinessEditorCard extends StatelessWidget {
  const _BusinessEditorCard({
    super.key,
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.iconBackground,
    required this.iconColor,
    required this.child,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final Color iconBackground;
  final Color iconColor;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: tokens.panel,
        border: Border.all(color: const Color(0xFFEADFD6)),
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF0F172A).withValues(alpha: .06),
            blurRadius: 18,
            offset: const Offset(0, 5),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              AgendaIconBadge(
                icon,
                size: 38,
                iconSize: 20,
                background: iconBackground,
                color: iconColor,
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Semantics(
                      header: true,
                      child: Text(
                        title,
                        style: TextStyle(
                          color: tokens.ink,
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      style: TextStyle(color: tokens.muted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          child,
        ],
      ),
    );
  }
}

class _BusinessLabeledField extends StatelessWidget {
  const _BusinessLabeledField({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          label,
          style: TextStyle(
            color: tokens.muted,
            fontSize: 12,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 5),
        child,
      ],
    );
  }
}

class _BusinessDialogField extends StatelessWidget {
  const _BusinessDialogField({
    required this.fieldKey,
    required this.label,
    required this.hint,
    required this.controller,
    this.keyboardType,
    this.validator,
    this.errorText,
    this.readOnly = false,
  });

  final Key fieldKey;
  final String label;
  final String hint;
  final TextEditingController controller;
  final TextInputType? keyboardType;
  final FormFieldValidator<String>? validator;
  final String? errorText;
  final bool readOnly;

  @override
  Widget build(BuildContext context) {
    return _BusinessLabeledField(
      label: label,
      child: TextFormField(
        key: fieldKey,
        controller: controller,
        keyboardType: keyboardType,
        readOnly: readOnly,
        validator: validator,
        style: const TextStyle(fontSize: 13),
        decoration: _businessInputDecoration(
          context,
          hint: hint,
          errorText: errorText,
        ),
      ),
    );
  }
}

class _BusinessDialogColumns extends StatelessWidget {
  const _BusinessDialogColumns({
    required this.first,
    required this.second,
    this.firstFlex = 1,
    this.secondFlex = 1,
  });

  final Widget first;
  final Widget second;
  final int firstFlex;
  final int secondFlex;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 520) {
          return Column(children: [first, const SizedBox(height: 11), second]);
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(flex: firstFlex, child: first),
            const SizedBox(width: 12),
            Expanded(flex: secondFlex, child: second),
          ],
        );
      },
    );
  }
}

class _BusinessDialogFooter extends StatelessWidget {
  const _BusinessDialogFooter({
    required this.saving,
    required this.onCancel,
    required this.onSave,
  });

  final bool saving;
  final VoidCallback onCancel;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 720;
    final cancelButton = TextButton(
      key: const Key('business-dialog-cancel'),
      onPressed: saving ? null : onCancel,
      style: TextButton.styleFrom(
        minimumSize: const Size(112, 40),
        padding: EdgeInsets.symmetric(horizontal: compact ? 12 : 14),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: const Text(
        'Cancelar',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
    );
    final saveButton = ElevatedButton.icon(
      key: const Key('business-dialog-save'),
      onPressed: saving ? null : onSave,
      style: ElevatedButton.styleFrom(
        minimumSize: const Size(150, 40),
        padding: EdgeInsets.symmetric(horizontal: compact ? 14 : 16),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      icon: saving
          ? const SizedBox.square(
              dimension: 15,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.check_rounded, size: 18),
      label: Text(
        saving ? 'Salvando...' : 'Salvar alterações',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
    );
    return Container(
      key: const Key('business-dialog-footer'),
      padding: EdgeInsets.fromLTRB(
        compact ? 14 : 22,
        14,
        compact ? 14 : 22,
        compact ? 14 : 16,
      ),
      decoration: BoxDecoration(
        color: tokens.panel,
        border: Border(top: BorderSide(color: tokens.line)),
      ),
      child: compact
          ? Row(
              children: [
                SizedBox(width: 112, child: cancelButton),
                const SizedBox(width: 10),
                Expanded(child: saveButton),
              ],
            )
          : Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [cancelButton, const SizedBox(width: 10), saveButton],
            ),
    );
  }
}

InputDecoration _businessInputDecoration(
  BuildContext context, {
  required String hint,
  String? errorText,
}) {
  final tokens = AgendaThemeTokens.of(context);
  const fieldBorder = Color(0xFFD8E4F2);
  final radius = BorderRadius.circular(8);
  return InputDecoration(
    hintText: hint,
    errorText: errorText,
    isDense: true,
    filled: true,
    fillColor: tokens.panel,
    contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9.5),
    hintStyle: TextStyle(color: tokens.muted, fontSize: 13),
    errorStyle: const TextStyle(fontSize: 11),
    border: OutlineInputBorder(
      borderRadius: radius,
      borderSide: const BorderSide(color: fieldBorder),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: const BorderSide(color: fieldBorder),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: BorderSide(color: tokens.accent, width: 1.4),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: const BorderSide(color: Color(0xFFDC2626)),
    ),
    focusedErrorBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: const BorderSide(color: Color(0xFFDC2626), width: 1.4),
    ),
  );
}

class _ThemeDialog extends StatefulWidget {
  const _ThemeDialog({required this.selectedId});

  final String selectedId;

  @override
  State<_ThemeDialog> createState() => _ThemeDialogState();
}

class _ThemeDialogState extends State<_ThemeDialog> {
  late String _selectedId;

  @override
  void initState() {
    super.initState();
    _selectedId = widget.selectedId;
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      insetPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 18),
      title: const Text('Escolha o tema do negócio'),
      content: SizedBox(
        width: 780,
        height: MediaQuery.sizeOf(context).height.clamp(360, 590) * .72,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final columns = constraints.maxWidth < 460
                ? 1
                : constraints.maxWidth < 700
                ? 2
                : 3;
            return GridView.builder(
              itemCount: AgendaThemes.all.length,
              gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: columns,
                crossAxisSpacing: 10,
                mainAxisSpacing: 10,
                childAspectRatio: columns == 1 ? 2.55 : 1.55,
              ),
              itemBuilder: (context, index) {
                final theme = AgendaThemes.all[index];
                return _ThemeChoiceCard(
                  theme: theme,
                  selected: theme.id == _selectedId,
                  onTap: () => setState(() => _selectedId = theme.id),
                );
              },
            );
          },
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        ElevatedButton.icon(
          onPressed: () => Navigator.of(context).pop(_selectedId),
          icon: const Icon(Icons.palette_outlined, size: 18),
          label: const Text('Aplicar tema'),
        ),
      ],
    );
  }
}

class _ThemeChoiceCard extends StatelessWidget {
  const _ThemeChoiceCard({
    required this.theme,
    required this.selected,
    required this.onTap,
  });

  final AgendaThemeSpec theme;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = theme.tokens;
    return Material(
      color: t.appBackground,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(
          color: selected ? t.accent : t.line,
          width: selected ? 2 : 1,
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(11),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: Row(
                  children: [
                    Container(
                      width: 34,
                      decoration: BoxDecoration(
                        color: t.sidebarBackground,
                        borderRadius: BorderRadius.circular(6),
                      ),
                    ),
                    const SizedBox(width: 7),
                    Expanded(
                      child: Container(
                        padding: const EdgeInsets.all(7),
                        decoration: BoxDecoration(
                          color: t.panel,
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(color: t.line),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              width: 55,
                              height: 7,
                              decoration: BoxDecoration(
                                color: t.accent,
                                borderRadius: BorderRadius.circular(4),
                              ),
                            ),
                            const SizedBox(height: 6),
                            Container(height: 5, color: t.accentSoft),
                            const SizedBox(height: 4),
                            Container(height: 5, color: t.graySoft),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      _themeName(theme.id),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontWeight: FontWeight.w800,
                        fontSize: 12,
                      ),
                    ),
                  ),
                  if (selected)
                    Icon(Icons.check_circle_rounded, color: t.accent),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _BusinessHoursDialog extends StatefulWidget {
  const _BusinessHoursDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_BusinessHoursDialog> createState() => _BusinessHoursDialogState();
}

class _BusinessHoursDialogState extends State<_BusinessHoursDialog> {
  late int _startHour;
  late int _endHour;
  late Set<int> _workdays;
  late bool _breakEnabled;
  late int _breakStartHour;
  late int _breakEndHour;
  late final TextEditingController _resources;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final settings = widget.controller.data.settings;
    _startHour = settings.workdayStartHour;
    _endHour = settings.workdayEndHour;
    _workdays = settings.workdays.toSet();
    _breakEnabled = settings.workdayBreakEnabled;
    _breakStartHour = settings.workdayBreakStartHour;
    _breakEndHour = settings.workdayBreakEndHour;
    _resources = TextEditingController(text: settings.resources.join('\n'));
  }

  @override
  void dispose() {
    _resources.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_endHour <= _startHour) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('O fim do expediente deve ser posterior ao início.'),
        ),
      );
      return;
    }
    if (_workdays.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Selecione ao menos um dia de atendimento.'),
        ),
      );
      return;
    }
    if (_breakEnabled &&
        (_breakEndHour <= _breakStartHour ||
            _breakStartHour < _startHour ||
            _breakEndHour > _endHour)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('O intervalo deve ficar dentro do expediente.'),
        ),
      );
      return;
    }
    setState(() => _saving = true);
    final resources = _resources.text
        .split(RegExp(r'[,;\n]'))
        .map((item) => item.trim())
        .where((item) => item.isNotEmpty)
        .toSet()
        .toList();
    await widget.controller.updateSettings((settings) {
      settings
        ..workdayStartHour = _startHour
        ..workdayEndHour = _endHour
        ..workdays = (_workdays.toList()..sort())
        ..workdayBreakEnabled = _breakEnabled
        ..workdayBreakStartHour = _breakStartHour
        ..workdayBreakEndHour = _breakEndHour
        ..resources = resources;
    });
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Configurar horários'),
      content: SizedBox(
        width: 590,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Dias de atendimento',
                style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 7,
                runSpacing: 7,
                children: [
                  for (final entry in const <(int, String)>[
                    (1, 'Seg'),
                    (2, 'Ter'),
                    (3, 'Qua'),
                    (4, 'Qui'),
                    (5, 'Sex'),
                    (6, 'Sáb'),
                    (0, 'Dom'),
                  ])
                    FilterChip(
                      label: Text(entry.$2),
                      selected: _workdays.contains(entry.$1),
                      onSelected: (selected) => setState(() {
                        if (selected) {
                          _workdays.add(entry.$1);
                        } else {
                          _workdays.remove(entry.$1);
                        }
                      }),
                    ),
                ],
              ),
              const SizedBox(height: 16),
              LayoutBuilder(
                builder: (context, constraints) {
                  final start = DropdownButtonFormField<int>(
                    initialValue: _startHour,
                    decoration: const InputDecoration(labelText: 'Início'),
                    items: [
                      for (var hour = 0; hour <= 23; hour++)
                        DropdownMenuItem(
                          value: hour,
                          child: Text(_hourLabel(hour)),
                        ),
                    ],
                    onChanged: (value) {
                      if (value != null) setState(() => _startHour = value);
                    },
                  );
                  final end = DropdownButtonFormField<int>(
                    initialValue: _endHour,
                    decoration: const InputDecoration(labelText: 'Fim'),
                    items: [
                      for (var hour = 1; hour <= 24; hour++)
                        DropdownMenuItem(
                          value: hour,
                          child: Text(_hourLabel(hour)),
                        ),
                    ],
                    onChanged: (value) {
                      if (value != null) setState(() => _endHour = value);
                    },
                  );
                  if (constraints.maxWidth < 440) {
                    return Column(
                      children: [start, const SizedBox(height: 12), end],
                    );
                  }
                  return Row(
                    children: [
                      Expanded(child: start),
                      const SizedBox(width: 12),
                      Expanded(child: end),
                    ],
                  );
                },
              ),
              const SizedBox(height: 14),
              SwitchListTile.adaptive(
                contentPadding: EdgeInsets.zero,
                title: const Text('Intervalo no expediente'),
                subtitle: const Text(
                  'Bloqueia automaticamente esse período nos dias selecionados.',
                ),
                value: _breakEnabled,
                onChanged: (value) => setState(() => _breakEnabled = value),
              ),
              if (_breakEnabled) ...[
                const SizedBox(height: 8),
                LayoutBuilder(
                  builder: (context, constraints) {
                    final start = DropdownButtonFormField<int>(
                      initialValue: _breakStartHour,
                      decoration: const InputDecoration(
                        labelText: 'Início do intervalo',
                      ),
                      items: [
                        for (var hour = 0; hour <= 23; hour++)
                          DropdownMenuItem(
                            value: hour,
                            child: Text(_hourLabel(hour)),
                          ),
                      ],
                      onChanged: (value) {
                        if (value != null) {
                          setState(() => _breakStartHour = value);
                        }
                      },
                    );
                    final end = DropdownButtonFormField<int>(
                      initialValue: _breakEndHour,
                      decoration: const InputDecoration(
                        labelText: 'Fim do intervalo',
                      ),
                      items: [
                        for (var hour = 1; hour <= 24; hour++)
                          DropdownMenuItem(
                            value: hour,
                            child: Text(_hourLabel(hour)),
                          ),
                      ],
                      onChanged: (value) {
                        if (value != null) {
                          setState(() => _breakEndHour = value);
                        }
                      },
                    );
                    if (constraints.maxWidth < 440) {
                      return Column(
                        children: [start, const SizedBox(height: 12), end],
                      );
                    }
                    return Row(
                      children: [
                        Expanded(child: start),
                        const SizedBox(width: 12),
                        Expanded(child: end),
                      ],
                    );
                  },
                ),
                const SizedBox(height: 14),
              ],
              TextField(
                controller: _resources,
                minLines: 4,
                maxLines: 8,
                decoration: const InputDecoration(
                  labelText: 'Salas, mesas e cadeiras',
                  hintText: 'Informe um recurso por linha',
                  alignLabelWithHint: true,
                ),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.of(context).pop(false),
          child: const Text('Cancelar'),
        ),
        ElevatedButton.icon(
          onPressed: _saving ? null : _save,
          icon: _saving
              ? const SizedBox.square(
                  dimension: 15,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.check_rounded, size: 18),
          label: Text(_saving ? 'Salvando...' : 'Salvar horários'),
        ),
      ],
    );
  }
}

String? _required(String? value) =>
    value == null || value.trim().isEmpty ? 'Este campo é obrigatório.' : null;

String _hourLabel(int value) => '${value.toString().padLeft(2, '0')}:00';

String _themeName(String id) => switch (id) {
  '' => 'Agenda Livre',
  'salon-classic-gold' => 'Clássico dourado',
  'salon-lilac-glow' => 'Lilás glow',
  'salon-rose-luxe' => 'Rose luxe',
  'barber-midnight' => 'Preto clássico',
  'barber-emerald' => 'Verde barbearia',
  'barber-navy' => 'Azul naval',
  'medical-teal' => 'Teal clínico',
  'medical-green' => 'Verde acolhe',
  'medical-blue' => 'Azul saúde',
  'pet-coral' => 'Coral pet',
  'pet-lilac' => 'Lilás pet',
  'pet-teal' => 'Verde pet',
  'workshop-gold' => 'Mecânica ouro',
  'workshop-olive' => 'Verde automotivo',
  'workshop-graphite' => 'Grafite oficina',
  'aesthetic-lavender' => 'Lavanda wellness',
  'aesthetic-sage' => 'Sálvia natural',
  'aesthetic-coral' => 'Coral glow',
  'podology-terracotta' => 'Essencial terracota',
  'podology-mint' => 'Bem-estar verde',
  'podology-blue' => 'Azul podologia',
  'spa-aqua' => 'Água calma',
  'spa-sand' => 'Areia serena',
  'spa-forest' => 'Floresta zen',
  _ => 'Tema personalizado',
};
