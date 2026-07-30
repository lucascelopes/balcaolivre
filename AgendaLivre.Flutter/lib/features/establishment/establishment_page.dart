import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/business_profile.dart';
import '../../core/formatters.dart';
import '../../core/motion.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import 'establishment_desktop_legacy.dart';
import 'editor_dialogs.dart';

class EstablishmentPage extends StatelessWidget {
  const EstablishmentPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final data = controller.data;
        final profile = AgendaBusinessProfile.fromSettings(data.settings);
        final now = DateTime.now();
        final monthStart = DateTime(now.year, now.month);
        final nextMonth = DateTime(now.year, now.month + 1);
        final monthAppointments = controller
            .appointmentsBetween(monthStart, nextMonth)
            .where(
              (item) => !const {
                AppointmentStatus.blocked,
                AppointmentStatus.cancelled,
                AppointmentStatus.noShow,
              }.contains(item.status),
            )
            .toList();
        final paidAppointments = monthAppointments
            .where(
              (item) =>
                  item.paymentConfirmedAt != null &&
                  !item.paymentConfirmedAt!.isBefore(monthStart) &&
                  item.paymentConfirmedAt!.isBefore(nextMonth),
            )
            .toList();
        final revenue = paidAppointments.fold<double>(
          0,
          (sum, item) => sum + item.price,
        );
        final average = paidAppointments.isEmpty
            ? 0.0
            : revenue / paidAppointments.length;

        return LayoutBuilder(
          builder: (context, constraints) {
            if (constraints.maxWidth >= 650) {
              return EstablishmentDesktopLegacyPage(controller: controller);
            }
            final mobile = constraints.maxWidth < 650;
            return SingleChildScrollView(
              key: const Key('establishment-wpf-page'),
              padding: EdgeInsets.fromLTRB(
                mobile ? 14 : 18,
                mobile ? 14 : 18,
                mobile ? 14 : 18,
                96,
              ),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 1380),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      AgendaReveal(
                        child: _EstablishmentHeader(
                          businessName: controller.businessName,
                          profile: profile,
                          onNewCustomer: () => _editCustomer(context),
                          onNewProfessional: () => _editProfessional(context),
                          onNewService: () => _editService(context),
                        ),
                      ),
                      const SizedBox(height: 12),
                      _MetricsOverview(
                        customers: data.customers.length,
                        professionals: controller.activeProfessionals.length,
                        services: controller.activeServices.length,
                        profile: profile,
                      ),
                      const SizedBox(height: 14),
                      _EstablishmentDashboard(
                        customers: data.customers,
                        professionals: data.professionals,
                        services: data.services,
                        profile: profile,
                        appointments: monthAppointments.length,
                        averageRevenue: average,
                        revenue: revenue,
                        goal: data.settings.monthlyRevenueGoal,
                        onManageCustomers: () => showCustomerManagerDialog(
                          context,
                          controller: controller,
                        ),
                        onManageProfessionals: () =>
                            showProfessionalManagerDialog(
                              context,
                              controller: controller,
                            ),
                        onManageServices: () => showServiceManagerDialog(
                          context,
                          controller: controller,
                        ),
                        onNewCustomer: () => _editCustomer(context),
                        onEditCustomer: (item) =>
                            _editCustomer(context, customer: item),
                        onEditProfessional: (item) =>
                            _editProfessional(context, professional: item),
                        onEditService: (item) =>
                            _editService(context, service: item),
                        onEditGoal: () => _editRevenueGoal(context),
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

  Future<void> _editCustomer(BuildContext context, {Customer? customer}) async {
    final saved = await showCustomerEditorDialog(
      context,
      controller: controller,
      customer: customer,
    );
    if (!saved || !context.mounted) return;
    _showSaved(
      context,
      customer == null ? 'Cliente criado.' : 'Cliente atualizado.',
    );
  }

  Future<void> _editProfessional(
    BuildContext context, {
    Professional? professional,
  }) async {
    final saved = await showProfessionalEditorDialog(
      context,
      controller: controller,
      professional: professional,
    );
    if (!saved || !context.mounted) return;
    _showSaved(
      context,
      professional == null
          ? 'Profissional criado.'
          : 'Profissional atualizado.',
    );
  }

  Future<void> _editService(
    BuildContext context, {
    ServiceItem? service,
  }) async {
    final saved = await showServiceEditorDialog(
      context,
      controller: controller,
      service: service,
    );
    if (!saved || !context.mounted) return;
    _showSaved(
      context,
      service == null ? 'Serviço criado.' : 'Serviço atualizado.',
    );
  }

  Future<void> _editRevenueGoal(BuildContext context) async {
    final value = await showDialog<double>(
      context: context,
      builder: (dialogContext) => _RevenueGoalDialog(
        initialValue: controller.data.settings.monthlyRevenueGoal,
      ),
    );
    if (value == null || !context.mounted) return;
    await controller.updateSettings(
      (settings) => settings.monthlyRevenueGoal = value,
    );
    if (context.mounted) _showSaved(context, 'Meta atualizada.');
  }

  void _showSaved(BuildContext context, String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }
}

class _EstablishmentHeader extends StatelessWidget {
  const _EstablishmentHeader({
    required this.businessName,
    required this.profile,
    required this.onNewCustomer,
    required this.onNewProfessional,
    required this.onNewService,
  });

  final String businessName;
  final AgendaBusinessProfile profile;
  final VoidCallback onNewCustomer;
  final VoidCallback onNewProfessional;
  final VoidCallback onNewService;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 900;
        final title = Column(
          key: const Key('establishment-wpf-header'),
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  'MEU ESTABELECIMENTO',
                  style: TextStyle(
                    color: t.accentDark,
                    fontSize: 10,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(width: 12),
                Container(width: 44, height: 1, color: t.accent),
              ],
            ),
            const SizedBox(height: 5),
            Text(
              'Meu estabelecimento',
              style: TextStyle(
                color: t.ink,
                fontSize: compact ? 27 : 30,
                height: 1.06,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              '$businessName  •  ${profile.segment}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(color: t.muted, fontSize: 12.5),
            ),
          ],
        );
        final actions = Wrap(
          spacing: 8,
          runSpacing: 8,
          alignment: compact ? WrapAlignment.start : WrapAlignment.end,
          children: [
            ElevatedButton.icon(
              key: const Key('establishment-new-customer'),
              onPressed: onNewCustomer,
              icon: const Icon(Icons.person_add_alt_1_rounded, size: 17),
              label: Text('Novo ${profile.customerSingular}'),
              style: ElevatedButton.styleFrom(minimumSize: const Size(142, 44)),
            ),
            OutlinedButton.icon(
              key: const Key('establishment-new-professional'),
              onPressed: onNewProfessional,
              icon: const Icon(Icons.person_pin_outlined, size: 17),
              label: Text('Novo ${profile.professionalSingular}'),
              style: OutlinedButton.styleFrom(minimumSize: const Size(158, 44)),
            ),
            OutlinedButton.icon(
              key: const Key('establishment-new-service'),
              onPressed: onNewService,
              icon: const Icon(Icons.content_cut_rounded, size: 17),
              label: Text('Novo ${profile.serviceSingular}'),
              style: OutlinedButton.styleFrom(minimumSize: const Size(132, 44)),
            ),
          ],
        );
        if (compact) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [title, const SizedBox(height: 14), actions],
          );
        }
        return Row(
          children: [
            Expanded(child: title),
            const SizedBox(width: 18),
            Flexible(child: actions),
          ],
        );
      },
    );
  }
}

class _MetricsOverview extends StatelessWidget {
  const _MetricsOverview({
    required this.customers,
    required this.professionals,
    required this.services,
    required this.profile,
  });

  final int customers;
  final int professionals;
  final int services;
  final AgendaBusinessProfile profile;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final metrics = [
      _MetricData(
        icon: Icons.groups_2_rounded,
        label: _capitalized(profile.customerPlural),
        value: '$customers',
        caption: 'cadastrados',
        tone: t.accentDark,
      ),
      _MetricData(
        icon: Icons.person_outline_rounded,
        label: _capitalized(profile.professionalPlural),
        value: '$professionals',
        caption: 'ativos',
        tone: t.accentDark,
      ),
      _MetricData(
        icon: Icons.assignment_rounded,
        label: _capitalized(profile.servicePlural),
        value: '$services',
        caption: 'no catálogo',
        tone: const Color(0xFF171614),
      ),
    ];
    return AgendaPanel(
      key: const Key('establishment-wpf-metrics'),
      radius: 16,
      padding: EdgeInsets.zero,
      child: Column(
        children: [
          _MobileSetupSummary(
            customers: customers,
            professionals: professionals,
            services: services,
          ),
          Divider(height: 1, color: t.line),
          SizedBox(
            height: 112,
            child: Row(
              children: [
                for (var index = 0; index < metrics.length; index++) ...[
                  Expanded(child: _MobileMetricItem(metric: metrics[index])),
                  if (index != metrics.length - 1)
                    VerticalDivider(
                      width: 1,
                      color: t.line,
                      indent: 12,
                      endIndent: 12,
                    ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  static String _capitalized(String value) =>
      value.isEmpty ? value : '${value[0].toUpperCase()}${value.substring(1)}';
}

class _MobileSetupSummary extends StatelessWidget {
  const _MobileSetupSummary({
    required this.customers,
    required this.professionals,
    required this.services,
  });

  final int customers;
  final int professionals;
  final int services;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final completed = [
      customers > 0,
      professionals > 0,
      services > 0,
    ].where((value) => value).length;
    final progress = completed / 3;
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(14),
            ),
            alignment: Alignment.center,
            child: Icon(
              Icons.storefront_outlined,
              color: t.accentDark,
              size: 22,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        'Estrutura cadastrada',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 12.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    Text(
                      '$completed de 3',
                      style: TextStyle(
                        color: t.accentDark,
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 7),
                ClipRRect(
                  borderRadius: BorderRadius.circular(99),
                  child: LinearProgressIndicator(
                    minHeight: 6,
                    value: progress,
                    backgroundColor: t.line,
                    valueColor: AlwaysStoppedAnimation<Color>(t.accent),
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  completed == 3
                      ? 'Clientes, equipe e catálogo prontos.'
                      : 'Complete clientes, equipe e catálogo.',
                  style: TextStyle(color: t.muted, fontSize: 10.5),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _MetricData {
  const _MetricData({
    required this.icon,
    required this.label,
    required this.value,
    required this.caption,
    required this.tone,
  });

  final IconData icon;
  final String label;
  final String value;
  final String caption;
  final Color tone;
}

class _MobileMetricItem extends StatelessWidget {
  const _MobileMetricItem({required this.metric});

  final _MetricData metric;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 12),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(11),
            ),
            alignment: Alignment.center,
            child: Icon(metric.icon, color: metric.tone, size: 19),
          ),
          const SizedBox(height: 5),
          Text(
            metric.value,
            maxLines: 1,
            style: TextStyle(
              color: t.ink,
              fontSize: 18,
              height: 1,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            metric.label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            textAlign: TextAlign.center,
            style: TextStyle(
              color: t.muted,
              fontSize: 10.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class _EstablishmentDashboard extends StatelessWidget {
  const _EstablishmentDashboard({
    required this.customers,
    required this.professionals,
    required this.services,
    required this.profile,
    required this.appointments,
    required this.averageRevenue,
    required this.revenue,
    required this.goal,
    required this.onManageCustomers,
    required this.onManageProfessionals,
    required this.onManageServices,
    required this.onNewCustomer,
    required this.onEditCustomer,
    required this.onEditProfessional,
    required this.onEditService,
    required this.onEditGoal,
  });

  final List<Customer> customers;
  final List<Professional> professionals;
  final List<ServiceItem> services;
  final AgendaBusinessProfile profile;
  final int appointments;
  final double averageRevenue;
  final double revenue;
  final double goal;
  final VoidCallback onManageCustomers;
  final VoidCallback onManageProfessionals;
  final VoidCallback onManageServices;
  final VoidCallback onNewCustomer;
  final ValueChanged<Customer> onEditCustomer;
  final ValueChanged<Professional> onEditProfessional;
  final ValueChanged<ServiceItem> onEditService;
  final VoidCallback onEditGoal;

  @override
  Widget build(BuildContext context) {
    final sortedCustomers = [...customers]
      ..sort((a, b) => b.lastSeenAt.compareTo(a.lastSeenAt));
    final activeServices = services
        .where((item) => item.isActive)
        .take(3)
        .toList();
    final activeProfessionals = professionals
        .where((item) => item.isActive)
        .take(2)
        .toList();

    final movement = _MovementCard(
      customers: sortedCustomers.take(4).toList(),
      profile: profile,
      total: customers.length,
      onManage: onManageCustomers,
      onNew: onNewCustomer,
      onEdit: onEditCustomer,
    );
    final catalog = _CatalogCard(
      services: activeServices,
      professionals: activeProfessionals,
      profile: profile,
      totalServices: services.where((item) => item.isActive).length,
      totalProfessionals: professionals.where((item) => item.isActive).length,
      onManageServices: onManageServices,
      onManageProfessionals: onManageProfessionals,
      onEditService: onEditService,
      onEditProfessional: onEditProfessional,
    );
    final summary = _MonthSummary(
      appointments: appointments,
      averageRevenue: averageRevenue,
      revenue: revenue,
      goal: goal,
      onEditGoal: onEditGoal,
    );

    return LayoutBuilder(
      key: const Key('establishment-wpf-grid'),
      builder: (context, constraints) {
        if (constraints.maxWidth >= 1040) {
          return SizedBox(
            height: 445,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(flex: 142, child: movement),
                const SizedBox(width: 12),
                Expanded(flex: 120, child: catalog),
                const SizedBox(width: 12),
                Expanded(flex: 82, child: summary),
              ],
            ),
          );
        }
        if (constraints.maxWidth >= 720) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(
                height: 445,
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(child: movement),
                    const SizedBox(width: 12),
                    Expanded(child: catalog),
                  ],
                ),
              ),
              const SizedBox(height: 12),
              summary,
            ],
          );
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            movement,
            const SizedBox(height: 12),
            catalog,
            const SizedBox(height: 12),
            summary,
          ],
        );
      },
    );
  }
}

class _MovementCard extends StatelessWidget {
  const _MovementCard({
    required this.customers,
    required this.profile,
    required this.total,
    required this.onManage,
    required this.onNew,
    required this.onEdit,
  });

  final List<Customer> customers;
  final AgendaBusinessProfile profile;
  final int total;
  final VoidCallback onManage;
  final VoidCallback onNew;
  final ValueChanged<Customer> onEdit;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _DashboardCard(
      key: const Key('establishment-movement-card'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _CardHeader(
            title: 'Movimento recente',
            badge: 'Atualizações',
            action: 'Ver ${profile.customerPlural}',
            onAction: onManage,
          ),
          const SizedBox(height: 8),
          if (customers.isEmpty)
            AgendaEmptyState(
              icon: Icons.groups_2_outlined,
              title: 'Nenhum movimento recente',
              message:
                  'Cadastre clientes ou conclua atendimentos para começar.',
              actionLabel: 'Novo ${profile.customerSingular}',
              onAction: onNew,
              compact: true,
            )
          else
            for (final item in customers)
              _ExpandableEntityRow(
                icon: Icons.person_outline_rounded,
                title: item.name,
                subtitle: [
                  item.phone,
                  item.profile,
                ].where((value) => value.trim().isNotEmpty).join('\n'),
                badge: 'Ativa',
                badgeColor: const Color(0xFF15945A),
                details: [
                  if (item.email.trim().isNotEmpty) 'E-mail: ${item.email}',
                  if (item.tags.trim().isNotEmpty) 'Tags: ${item.tags}',
                  item.acceptsWhatsApp
                      ? 'WhatsApp autorizado'
                      : 'WhatsApp não autorizado',
                ],
                onEdit: () => onEdit(item),
              ),
          const SizedBox(height: 10),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 10),
          InkWell(
            onTap: onManage,
            borderRadius: BorderRadius.circular(8),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    '+$total ${profile.customerPlural} no total',
                    style: TextStyle(color: t.ink, fontSize: 11.5),
                  ),
                ),
                Icon(Icons.chevron_right_rounded, color: t.ink, size: 17),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CatalogCard extends StatelessWidget {
  const _CatalogCard({
    required this.services,
    required this.professionals,
    required this.profile,
    required this.totalServices,
    required this.totalProfessionals,
    required this.onManageServices,
    required this.onManageProfessionals,
    required this.onEditService,
    required this.onEditProfessional,
  });

  final List<ServiceItem> services;
  final List<Professional> professionals;
  final AgendaBusinessProfile profile;
  final int totalServices;
  final int totalProfessionals;
  final VoidCallback onManageServices;
  final VoidCallback onManageProfessionals;
  final ValueChanged<ServiceItem> onEditService;
  final ValueChanged<Professional> onEditProfessional;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _DashboardCard(
      key: const Key('establishment-catalog-card'),
      padding: const EdgeInsets.fromLTRB(18, 16, 18, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _CardHeader(
            title: 'Catálogo em destaque',
            action: 'Ver ${profile.servicePlural}',
            onAction: onManageServices,
          ),
          const SizedBox(height: 12),
          Text(
            '${_capitalized(profile.servicePlural)} no catálogo',
            style: TextStyle(
              color: t.ink,
              fontSize: 11.5,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          if (services.isEmpty)
            _InlineEmpty(
              icon: Icons.assignment_outlined,
              label: 'Nenhum ${profile.serviceSingular} cadastrado',
            )
          else
            for (final item in services)
              _ExpandableEntityRow(
                icon: Icons.assignment_rounded,
                title: item.name,
                subtitle: '${item.durationMinutes} min',
                badge: money(item.price),
                details: [
                  if (item.category.trim().isNotEmpty)
                    'Categoria: ${item.category}',
                  'Duração: ${item.durationMinutes} minutos',
                  'Preço: ${money(item.price)}',
                ],
                onEdit: () => onEditService(item),
                compact: true,
              ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: Text(
                  '${_capitalized(profile.professionalPlural)} em destaque',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11.5,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              _TextLink(label: 'Ver equipe', onTap: onManageProfessionals),
            ],
          ),
          const SizedBox(height: 4),
          if (professionals.isEmpty)
            _InlineEmpty(
              icon: Icons.person_outline_rounded,
              label: 'Nenhum ${profile.professionalSingular} cadastrado',
            )
          else
            for (final item in professionals)
              _ExpandableEntityRow(
                icon: Icons.person_pin_rounded,
                title: item.name,
                subtitle: item.segmentLine,
                badge: item.role.trim().isEmpty ? 'Equipe' : item.role,
                badgeColor: t.accentDark,
                details: [
                  if (item.phone.trim().isNotEmpty) 'Telefone: ${item.phone}',
                  if (item.role.trim().isNotEmpty) 'Função: ${item.role}',
                  if (item.segmentLine.trim().isNotEmpty)
                    'Segmento: ${item.segmentLine}',
                ],
                onEdit: () => onEditProfessional(item),
                compact: true,
              ),
          const SizedBox(height: 10),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 9),
          Row(
            children: [
              Expanded(
                child: Text(
                  '$totalServices ${profile.servicePlural} no catálogo',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 10.7),
                ),
              ),
              Text(
                '$totalProfessionals ${profile.professionalPlural} no total',
                style: TextStyle(color: t.muted, fontSize: 10.7),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _MonthSummary extends StatelessWidget {
  const _MonthSummary({
    required this.appointments,
    required this.averageRevenue,
    required this.revenue,
    required this.goal,
    required this.onEditGoal,
  });

  final int appointments;
  final double averageRevenue;
  final double revenue;
  final double goal;
  final VoidCallback onEditGoal;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final progress = goal <= 0 ? 0.0 : (revenue / goal).clamp(0.0, 1.0);
    final percent = goal <= 0 ? 0 : ((revenue / goal) * 100).round();
    final remaining = (goal - revenue).clamp(0, double.infinity).toDouble();
    return _DashboardCard(
      key: const Key('establishment-month-card'),
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Resumo do mês',
            style: TextStyle(
              color: t.ink,
              fontSize: 17,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: t.warmSoft,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: t.accentSoft),
            ),
            child: Row(
              children: [
                AgendaIconBadge(
                  Icons.account_balance_wallet_outlined,
                  size: 38,
                  iconSize: 19,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Receita do mês',
                        style: TextStyle(color: t.muted, fontSize: 11.2),
                      ),
                      Text(
                        money(revenue, cents: false),
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 22,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _SummaryMetric(
                  icon: Icons.event_available_outlined,
                  label: 'Atendimentos',
                  value: '$appointments',
                  tone: t.accentDark,
                  background: t.warmSoft,
                ),
              ),
              Container(width: 1, height: 44, color: t.line),
              Expanded(
                child: _SummaryMetric(
                  icon: Icons.show_chart_rounded,
                  label: 'Receita média',
                  value: money(averageRevenue),
                  tone: const Color(0xFF15945A),
                  background: const Color(0xFFE8FAF1),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 12),
          Row(
            children: [
              Icon(Icons.track_changes_rounded, color: t.accentDark, size: 17),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  'Meta de faturamento',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11.8,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              _TextLink(
                label: 'Editar',
                icon: Icons.edit_outlined,
                onTap: onEditGoal,
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: Text(
                  '${money(revenue, cents: false)} / ${money(goal, cents: false)}',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 15.5,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              Text(
                '$percent%',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 13.5,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 7),
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: LinearProgressIndicator(
              minHeight: 7,
              value: progress,
              color: t.accent,
              backgroundColor: t.line,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            goal <= 0
                ? 'Defina uma meta para acompanhar o progresso.'
                : remaining <= 0
                ? 'Meta alcançada neste mês.'
                : 'Faltam ${money(remaining, cents: false)} para alcançar a meta.',
            style: TextStyle(color: t.muted, fontSize: 10.3, height: 1.35),
          ),
        ],
      ),
    );
  }
}

class _DashboardCard extends StatelessWidget {
  const _DashboardCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.fromLTRB(20, 18, 20, 14),
  });

  final Widget child;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) {
    return AgendaPanel(radius: 16, padding: padding, child: child);
  }
}

class _CardHeader extends StatelessWidget {
  const _CardHeader({
    required this.title,
    required this.action,
    required this.onAction,
    this.badge,
  });

  final String title;
  final String action;
  final VoidCallback onAction;
  final String? badge;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final heading = Row(
      children: [
        Expanded(
          child: Text(
            title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: t.ink,
              fontSize: 17,
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
        if (badge != null) ...[
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(11),
            ),
            child: Text(
              badge!,
              style: TextStyle(
                color: t.accentDark,
                fontSize: 10,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ],
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 350) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              heading,
              const SizedBox(height: 2),
              Align(
                alignment: Alignment.centerRight,
                child: _TextLink(label: action, onTap: onAction),
              ),
            ],
          );
        }
        return Row(
          children: [
            Expanded(child: heading),
            const SizedBox(width: 8),
            _TextLink(label: action, onTap: onAction),
          ],
        );
      },
    );
  }
}

class _ExpandableEntityRow extends StatefulWidget {
  const _ExpandableEntityRow({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.badge,
    required this.details,
    required this.onEdit,
    this.badgeColor,
    this.compact = false,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String badge;
  final List<String> details;
  final VoidCallback onEdit;
  final Color? badgeColor;
  final bool compact;

  @override
  State<_ExpandableEntityRow> createState() => _ExpandableEntityRowState();
}

class _ExpandableEntityRowState extends State<_ExpandableEntityRow> {
  bool expanded = false;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final badgeColor = widget.badgeColor ?? t.ink;
    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: InkWell(
        onTap: () => setState(() => expanded = !expanded),
        child: AnimatedSize(
          duration: AgendaMotion.duration(context, AgendaMotion.fast),
          alignment: Alignment.topCenter,
          child: Padding(
            padding: EdgeInsets.symmetric(vertical: widget.compact ? 7 : 9),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    AgendaIconBadge(
                      widget.icon,
                      size: widget.compact ? 30 : 34,
                      iconSize: widget.compact ? 14 : 16,
                      background: widget.badgeColor == const Color(0xFF15945A)
                          ? const Color(0xFFE0FAEA)
                          : null,
                      color: widget.badgeColor,
                    ),
                    const SizedBox(width: 9),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.title,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              color: t.ink,
                              fontSize: widget.compact ? 11.2 : 11.8,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            widget.subtitle.trim().isEmpty
                                ? 'Sem detalhes cadastrados'
                                : widget.subtitle.replaceAll('\n', '  |  '),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(color: t.muted, fontSize: 9.5),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 6),
                    Container(
                      constraints: const BoxConstraints(maxWidth: 105),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: badgeColor.withValues(alpha: .09),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        widget.badge,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: badgeColor,
                          fontSize: 9.3,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    const SizedBox(width: 5),
                    AnimatedRotation(
                      turns: expanded ? .5 : 0,
                      duration: AgendaMotion.duration(
                        context,
                        AgendaMotion.fast,
                      ),
                      child: Icon(
                        Icons.keyboard_arrow_down_rounded,
                        color: t.ink,
                        size: 18,
                      ),
                    ),
                  ],
                ),
                if (expanded) ...[
                  const SizedBox(height: 9),
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: t.graySoft,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        for (final detail in widget.details)
                          Padding(
                            padding: const EdgeInsets.only(bottom: 3),
                            child: Text(
                              detail,
                              style: TextStyle(color: t.muted, fontSize: 10.3),
                            ),
                          ),
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton.icon(
                            onPressed: widget.onEdit,
                            icon: const Icon(Icons.edit_outlined, size: 14),
                            label: const Text('Editar'),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _InlineEmpty extends StatelessWidget {
  const _InlineEmpty({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        children: [
          AgendaIconBadge(icon, size: 30, iconSize: 14),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              label,
              style: TextStyle(color: t.muted, fontSize: 10.5),
            ),
          ),
        ],
      ),
    );
  }
}

class _SummaryMetric extends StatelessWidget {
  const _SummaryMetric({
    required this.icon,
    required this.label,
    required this.value,
    required this.tone,
    required this.background,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color tone;
  final Color background;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: Row(
        children: [
          AgendaIconBadge(
            icon,
            size: 30,
            iconSize: 15,
            color: tone,
            background: background,
          ),
          const SizedBox(width: 6),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 8.8),
                ),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 15.5,
                    fontWeight: FontWeight.w800,
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

class _TextLink extends StatelessWidget {
  const _TextLink({required this.label, required this.onTap, this.icon});

  final String label;
  final VoidCallback onTap;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return TextButton(
      onPressed: onTap,
      style: TextButton.styleFrom(
        foregroundColor: t.ink,
        minimumSize: const Size(44, 44),
        padding: const EdgeInsets.symmetric(horizontal: 5),
        textStyle: const TextStyle(
          fontFamily: 'Segoe UI',
          fontSize: 10.8,
          fontWeight: FontWeight.w600,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[Icon(icon, size: 13), const SizedBox(width: 4)],
          Text(label),
          if (icon == null) ...[
            const SizedBox(width: 3),
            const Icon(Icons.chevron_right_rounded, size: 14),
          ],
        ],
      ),
    );
  }
}

class _RevenueGoalDialog extends StatefulWidget {
  const _RevenueGoalDialog({required this.initialValue});

  final double initialValue;

  @override
  State<_RevenueGoalDialog> createState() => _RevenueGoalDialogState();
}

class _RevenueGoalDialogState extends State<_RevenueGoalDialog> {
  late final TextEditingController controller;

  @override
  void initState() {
    super.initState();
    controller = TextEditingController(
      text: widget.initialValue.toStringAsFixed(2).replaceAll('.', ','),
    );
  }

  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AlertDialog(
      titlePadding: const EdgeInsets.fromLTRB(22, 20, 16, 0),
      contentPadding: const EdgeInsets.fromLTRB(22, 16, 22, 8),
      actionsPadding: const EdgeInsets.fromLTRB(16, 6, 16, 16),
      title: Row(
        children: [
          AgendaIconBadge(Icons.track_changes_rounded, size: 36, iconSize: 18),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              'Meta de faturamento',
              style: TextStyle(
                color: t.ink,
                fontSize: 20,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          IconButton(
            onPressed: () => Navigator.of(context).pop(),
            icon: const Icon(Icons.close_rounded),
          ),
        ],
      ),
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 380),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Defina quanto o estabelecimento pretende faturar por mês.',
              style: TextStyle(color: t.muted, fontSize: 12.5),
            ),
            const SizedBox(height: 16),
            TextField(
              key: const Key('establishment-revenue-goal-field'),
              controller: controller,
              autofocus: true,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(
                labelText: 'Meta mensal',
                prefixText: 'R\$ ',
              ),
            ),
          ],
        ),
      ),
      actions: [
        OutlinedButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        ElevatedButton.icon(
          onPressed: () {
            final value = double.tryParse(
              controller.text.trim().replaceAll('.', '').replaceAll(',', '.'),
            );
            if (value == null || value < 0) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Informe uma meta válida.')),
              );
              return;
            }
            Navigator.of(context).pop(value);
          },
          icon: const Icon(Icons.check_rounded, size: 17),
          label: const Text('Salvar meta'),
        ),
      ],
    );
  }
}

String _capitalized(String value) =>
    value.isEmpty ? value : '${value[0].toUpperCase()}${value.substring(1)}';
