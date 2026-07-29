import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
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
        final now = DateTime.now();
        final monthStart = DateTime(now.year, now.month);
        final nextMonth = DateTime(now.year, now.month + 1);
        final monthAppointments = controller
            .appointmentsBetween(monthStart, nextMonth)
            .where((item) => item.status != AppointmentStatus.blocked)
            .toList();
        final monthRevenue = controller.revenueBetween(monthStart, nextMonth);
        final averageRevenue = monthAppointments.isEmpty
            ? 0.0
            : monthRevenue / monthAppointments.length;

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
                      if (constraints.maxWidth >= 1040)
                        _WpfEstablishmentDesktop(
                          businessName: controller.businessName,
                          customers: data.customers,
                          professionals: data.professionals,
                          services: data.services,
                          monthAppointments: monthAppointments.length,
                          averageRevenue: averageRevenue,
                          monthRevenue: monthRevenue,
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
                          onNewProfessional: () => _editProfessional(context),
                          onNewService: () => _editService(context),
                          onEditCustomer: (item) =>
                              _editCustomer(context, customer: item),
                          onEditProfessional: (item) =>
                              _editProfessional(context, professional: item),
                          onEditService: (item) =>
                              _editService(context, service: item),
                        )
                      else ...[
                        _EstablishmentHero(
                          businessName: controller.businessName,
                          onNewCustomer: () => _editCustomer(context),
                          onNewProfessional: () => _editProfessional(context),
                          onNewService: () => _editService(context),
                        ),
                        const SizedBox(height: 14),
                        _MetricStrip(
                          metrics: [
                            _MetricData(
                              icon: Icons.groups_2_outlined,
                              label: 'Clientes',
                              value: data.customers.length.toString(),
                              caption: 'cadastrados',
                            ),
                            _MetricData(
                              icon: Icons.person_outline_rounded,
                              label: 'Profissionais',
                              value: controller.activeProfessionals.length
                                  .toString(),
                              caption: 'ativos',
                            ),
                            _MetricData(
                              icon: Icons.content_paste_outlined,
                              label: 'Serviços',
                              value: controller.activeServices.length
                                  .toString(),
                              caption: 'no catálogo',
                              tone: const Color(0xFF10B981),
                            ),
                            _MetricData(
                              icon: Icons.account_balance_wallet_outlined,
                              label: 'Receita do mês',
                              value: money(monthRevenue, cents: false),
                              caption: 'faturamento',
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        _OverviewGrid(
                          customers: data.customers,
                          professionals: data.professionals,
                          services: data.services,
                          monthAppointments: monthAppointments.length,
                          averageRevenue: averageRevenue,
                          monthRevenue: monthRevenue,
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
                        ),
                      ],
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

  void _showSaved(BuildContext context, String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }
}

class _WpfEstablishmentDesktop extends StatelessWidget {
  const _WpfEstablishmentDesktop({
    required this.businessName,
    required this.customers,
    required this.professionals,
    required this.services,
    required this.monthAppointments,
    required this.averageRevenue,
    required this.monthRevenue,
    required this.onManageCustomers,
    required this.onManageProfessionals,
    required this.onManageServices,
    required this.onNewCustomer,
    required this.onNewProfessional,
    required this.onNewService,
    required this.onEditCustomer,
    required this.onEditProfessional,
    required this.onEditService,
  });

  final String businessName;
  final List<Customer> customers;
  final List<Professional> professionals;
  final List<ServiceItem> services;
  final int monthAppointments;
  final double averageRevenue;
  final double monthRevenue;
  final VoidCallback onManageCustomers;
  final VoidCallback onManageProfessionals;
  final VoidCallback onManageServices;
  final VoidCallback onNewCustomer;
  final VoidCallback onNewProfessional;
  final VoidCallback onNewService;
  final ValueChanged<Customer> onEditCustomer;
  final ValueChanged<Professional> onEditProfessional;
  final ValueChanged<ServiceItem> onEditService;

  @override
  Widget build(BuildContext context) {
    final activeProfessionals = professionals
        .where((item) => item.isActive)
        .toList(growable: false);
    final activeServices = services
        .where((item) => item.isActive)
        .toList(growable: false);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _WpfEstablishmentHeader(
          businessName: businessName,
          onNewCustomer: onNewCustomer,
          onNewProfessional: onNewProfessional,
          onNewService: onNewService,
        ),
        const SizedBox(height: 12),
        _WpfEstablishmentMetricPanel(
          customerCount: customers.length,
          professionalCount: activeProfessionals.length,
          serviceCount: activeServices.length,
        ),
        const SizedBox(height: 12),
        SizedBox(
          height: 340,
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                flex: 17,
                child: _WpfRecentActivity(
                  customers: customers,
                  onManage: onManageCustomers,
                  onEdit: onEditCustomer,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 14,
                child: _WpfCatalogPanel(
                  professionals: activeProfessionals,
                  services: activeServices,
                  onManageProfessionals: onManageProfessionals,
                  onManageServices: onManageServices,
                  onEditProfessional: onEditProfessional,
                  onEditService: onEditService,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 10,
                child: _WpfMonthSummary(
                  appointments: monthAppointments,
                  averageRevenue: averageRevenue,
                  revenue: monthRevenue,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _WpfEstablishmentHeader extends StatelessWidget {
  const _WpfEstablishmentHeader({
    required this.businessName,
    required this.onNewCustomer,
    required this.onNewProfessional,
    required this.onNewService,
  });

  final String businessName;
  final VoidCallback onNewCustomer;
  final VoidCallback onNewProfessional;
  final VoidCallback onNewService;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      height: 74,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'MEU ESTABELECIMENTO',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 10,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Container(width: 44, height: 1, color: t.accent),
                  ],
                ),
                const SizedBox(height: 6),
                Text(
                  'Meu estabelecimento',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 29,
                    height: 1.06,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  businessName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 12.5),
                ),
              ],
            ),
          ),
          ElevatedButton.icon(
            onPressed: onNewCustomer,
            icon: const Icon(Icons.person_add_alt_1_rounded, size: 17),
            label: const Text('Novo cliente'),
          ),
          const SizedBox(width: 8),
          OutlinedButton.icon(
            onPressed: onNewProfessional,
            icon: const Icon(Icons.badge_outlined, size: 17),
            label: const Text('Novo profissional'),
          ),
          const SizedBox(width: 8),
          OutlinedButton.icon(
            onPressed: onNewService,
            icon: const Icon(Icons.content_cut_rounded, size: 17),
            label: const Text('Novo serviço'),
          ),
        ],
      ),
    );
  }
}

class _WpfEstablishmentMetricPanel extends StatelessWidget {
  const _WpfEstablishmentMetricPanel({
    required this.customerCount,
    required this.professionalCount,
    required this.serviceCount,
  });

  final int customerCount;
  final int professionalCount;
  final int serviceCount;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: EdgeInsets.zero,
      child: SizedBox(
        height: 124,
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              flex: 5,
              child: _WpfMiniBars(
                values: [customerCount, professionalCount, serviceCount],
              ),
            ),
            VerticalDivider(width: 1, thickness: 1, color: t.line),
            Expanded(
              flex: 3,
              child: _WpfEstablishmentMetric(
                icon: Icons.groups_2_outlined,
                label: 'Clientes',
                value: '$customerCount',
                caption: 'cadastrados',
              ),
            ),
            VerticalDivider(width: 1, thickness: 1, color: t.line),
            Expanded(
              flex: 3,
              child: _WpfEstablishmentMetric(
                icon: Icons.person_outline_rounded,
                label: 'Profissionais',
                value: '$professionalCount',
                caption: 'ativos',
              ),
            ),
            VerticalDivider(width: 1, thickness: 1, color: t.line),
            Expanded(
              flex: 3,
              child: _WpfEstablishmentMetric(
                icon: Icons.content_paste_outlined,
                label: 'Serviços',
                value: '$serviceCount',
                caption: 'no catálogo',
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _WpfMiniBars extends StatelessWidget {
  const _WpfMiniBars({required this.values});

  final List<int> values;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final largest = values.fold<int>(1, (value, item) {
      return item > value ? item : value;
    });
    final colors = [t.accent, const Color(0xFFF2B18B), t.accentDark];
    return Padding(
      padding: const EdgeInsets.fromLTRB(22, 20, 22, 16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          for (var index = 0; index < values.length; index++) ...[
            Expanded(
              child: Align(
                alignment: Alignment.bottomCenter,
                child: AnimatedContainer(
                  duration: const Duration(milliseconds: 220),
                  height: 12 + (values[index] / largest) * 62,
                  constraints: const BoxConstraints(maxWidth: 44),
                  decoration: BoxDecoration(
                    color: colors[index],
                    borderRadius: const BorderRadius.vertical(
                      top: Radius.circular(7),
                    ),
                  ),
                ),
              ),
            ),
            if (index != values.length - 1) const SizedBox(width: 18),
          ],
        ],
      ),
    );
  }
}

class _WpfEstablishmentMetric extends StatelessWidget {
  const _WpfEstablishmentMetric({
    required this.icon,
    required this.label,
    required this.value,
    required this.caption,
  });

  final IconData icon;
  final String label;
  final String value;
  final String caption;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 14),
      child: Row(
        children: [
          AgendaIconBadge(icon, size: 42, iconSize: 21),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(color: t.muted, fontSize: 11.5)),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 21,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                Text(caption, style: TextStyle(color: t.muted, fontSize: 10.5)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _WpfRecentActivity extends StatelessWidget {
  const _WpfRecentActivity({
    required this.customers,
    required this.onManage,
    required this.onEdit,
  });

  final List<Customer> customers;
  final VoidCallback onManage;
  final ValueChanged<Customer> onEdit;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final recent = [...customers]
      ..sort((a, b) => b.lastSeenAt.compareTo(a.lastSeenAt));
    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Row(
                  children: [
                    Flexible(
                      child: Text(
                        'Movimento recente',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 17,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    AgendaPill(label: 'Atualizações'),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              TextButton(
                onPressed: onManage,
                style: TextButton.styleFrom(
                  minimumSize: Size.zero,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 6,
                    vertical: 8,
                  ),
                ),
                child: const Text('Ver clientes  ›'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Expanded(
            child: recent.isEmpty
                ? const AgendaEmptyState(
                    icon: Icons.groups_2_outlined,
                    title: 'Nenhum cliente cadastrado',
                    message: 'Os clientes recentes aparecerão aqui.',
                    compact: true,
                  )
                : ListView.separated(
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: recent.take(4).length,
                    separatorBuilder: (_, _) =>
                        Divider(height: 1, color: t.line),
                    itemBuilder: (_, index) {
                      final customer = recent[index];
                      return _WpfEntityLine(
                        icon: Icons.person_outline_rounded,
                        title: customer.name,
                        subtitle: [
                          customer.phone,
                          customer.profile,
                        ].where((item) => item.trim().isNotEmpty).join('  |  '),
                        badge: customer.acceptsWhatsApp ? 'Ativa' : 'Inativa',
                        badgeColor: customer.acceptsWhatsApp
                            ? const Color(0xFF16A34A)
                            : t.muted,
                        onTap: () => onEdit(customer),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _WpfCatalogPanel extends StatelessWidget {
  const _WpfCatalogPanel({
    required this.professionals,
    required this.services,
    required this.onManageProfessionals,
    required this.onManageServices,
    required this.onEditProfessional,
    required this.onEditService,
  });

  final List<Professional> professionals;
  final List<ServiceItem> services;
  final VoidCallback onManageProfessionals;
  final VoidCallback onManageServices;
  final ValueChanged<Professional> onEditProfessional;
  final ValueChanged<ServiceItem> onEditService;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.fromLTRB(18, 14, 18, 8),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Catálogo em destaque',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 17,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                TextButton(
                  onPressed: onManageServices,
                  child: const Text('Ver serviços  ›'),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Serviços mais realizados',
              style: TextStyle(
                color: t.ink,
                fontSize: 11.5,
                fontWeight: FontWeight.w800,
              ),
            ),
            for (final service in services.take(3))
              _WpfEntityLine(
                icon: Icons.content_paste_outlined,
                title: service.name,
                subtitle: '${service.durationMinutes} min',
                badge: money(service.price),
                onTap: () => onEditService(service),
                dense: true,
              ),
            const SizedBox(height: 3),
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Profissionais em destaque',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                TextButton(
                  onPressed: onManageProfessionals,
                  child: const Text('Ver equipe  ›'),
                ),
              ],
            ),
            for (final professional in professionals.take(2))
              _WpfEntityLine(
                icon: Icons.badge_outlined,
                title: professional.name,
                subtitle: professional.segmentLine,
                badge: professional.role.trim().isEmpty
                    ? 'Equipe'
                    : professional.role,
                onTap: () => onEditProfessional(professional),
                dense: true,
              ),
          ],
        ),
      ),
    );
  }
}

class _WpfEntityLine extends StatelessWidget {
  const _WpfEntityLine({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.badge,
    required this.onTap,
    this.badgeColor,
    this.dense = false,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String badge;
  final VoidCallback onTap;
  final Color? badgeColor;
  final bool dense;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final tone = badgeColor ?? t.ink;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10),
      child: Padding(
        padding: EdgeInsets.symmetric(vertical: dense ? 5 : 8),
        child: Row(
          children: [
            AgendaIconBadge(icon, size: dense ? 28 : 32, iconSize: 15),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: dense ? 10.5 : 11.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 1),
                  Text(
                    subtitle.trim().isEmpty ? 'Sem detalhes' : subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 9.5),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color: tone.withValues(alpha: .09),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                badge,
                style: TextStyle(
                  color: tone,
                  fontSize: 9,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(width: 4),
            Icon(Icons.expand_more_rounded, color: t.muted, size: 17),
          ],
        ),
      ),
    );
  }
}

class _WpfMonthSummary extends StatelessWidget {
  const _WpfMonthSummary({
    required this.appointments,
    required this.averageRevenue,
    required this.revenue,
  });

  final int appointments;
  final double averageRevenue;
  final double revenue;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    const target = 2000.0;
    final progress = (revenue / target).clamp(0.0, 1.0);
    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Resumo do mês',
            style: TextStyle(
              color: t.ink,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: t.accentSoft,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Row(
              children: [
                AgendaIconBadge(
                  Icons.account_balance_wallet_outlined,
                  size: 34,
                  iconSize: 17,
                ),
                const SizedBox(width: 9),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Receita do mês',
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                    Text(
                      money(revenue, cents: false),
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 20,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _WpfSummaryMetric(
                  icon: Icons.calendar_month_outlined,
                  label: 'Atendimentos',
                  value: '$appointments',
                ),
              ),
              SizedBox(height: 42, child: VerticalDivider(color: t.line)),
              Expanded(
                child: _WpfSummaryMetric(
                  icon: Icons.trending_up_rounded,
                  label: 'Receita média',
                  value: money(averageRevenue),
                ),
              ),
            ],
          ),
          const Divider(height: 22),
          Row(
            children: [
              Icon(Icons.track_changes_rounded, color: t.ink, size: 17),
              const SizedBox(width: 7),
              const Expanded(
                child: Text(
                  'Meta de faturamento',
                  style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800),
                ),
              ),
              Icon(Icons.edit_outlined, color: t.muted, size: 15),
            ],
          ),
          const SizedBox(height: 11),
          Row(
            children: [
              Expanded(
                child: Text(
                  '${money(revenue, cents: false)} / ${money(target, cents: false)}',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              Text(
                '${(progress * 100).round()}%',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 7),
          LinearProgressIndicator(
            value: progress,
            minHeight: 8,
            borderRadius: BorderRadius.circular(6),
            color: t.accent,
            backgroundColor: t.line,
          ),
          const SizedBox(height: 7),
          Text(
            'Defina uma meta para acompanhar o progresso.',
            style: TextStyle(color: t.muted, fontSize: 9.5),
          ),
        ],
      ),
    );
  }
}

class _WpfSummaryMetric extends StatelessWidget {
  const _WpfSummaryMetric({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        Icon(icon, color: t.ink, size: 17),
        const SizedBox(width: 6),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: TextStyle(color: t.muted, fontSize: 9.5)),
              Text(
                value,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 13,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _EstablishmentHero extends StatelessWidget {
  const _EstablishmentHero({
    required this.businessName,
    required this.onNewCustomer,
    required this.onNewProfessional,
    required this.onNewService,
  });

  final String businessName;
  final VoidCallback onNewCustomer;
  final VoidCallback onNewProfessional;
  final VoidCallback onNewService;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: EdgeInsets.zero,
      child: Stack(
        children: [
          Positioned(
            right: 18,
            top: 8,
            bottom: 8,
            width: 300,
            child: Opacity(
              opacity: .045,
              child: Image.asset(
                'assets/branding/agenda-livre-logo-source.png',
                fit: BoxFit.cover,
                alignment: Alignment.centerRight,
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 18),
            child: LayoutBuilder(
              builder: (context, constraints) {
                final compact = constraints.maxWidth < 820;
                final heading = Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          'AGENDA LIVRE',
                          style: TextStyle(
                            color: t.accent,
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(width: 10),
                        Container(width: 28, height: 1, color: t.accent),
                      ],
                    ),
                    const SizedBox(height: 7),
                    Text(
                      'Meu estabelecimento',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: compact ? 24 : 29,
                        height: 1.05,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      'Centralize clientes, equipe e serviços do seu negócio em um só lugar.',
                      style: TextStyle(color: t.muted, fontSize: 12.5),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      businessName,
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 12.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                );
                final actions = Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  alignment: compact ? WrapAlignment.start : WrapAlignment.end,
                  children: [
                    ElevatedButton.icon(
                      onPressed: onNewCustomer,
                      icon: const Icon(Icons.person_add_alt_1, size: 17),
                      label: const Text('Novo cliente'),
                    ),
                    OutlinedButton.icon(
                      onPressed: onNewProfessional,
                      icon: const Icon(Icons.badge_outlined, size: 17),
                      label: const Text('Novo profissional'),
                    ),
                    OutlinedButton.icon(
                      onPressed: onNewService,
                      icon: const Icon(Icons.content_paste_outlined, size: 17),
                      label: const Text('Novo serviço'),
                    ),
                  ],
                );
                if (compact) {
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [heading, const SizedBox(height: 16), actions],
                  );
                }
                return Row(
                  children: [
                    Expanded(child: heading),
                    const SizedBox(width: 20),
                    Flexible(child: actions),
                  ],
                );
              },
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
    this.tone,
  });

  final IconData icon;
  final String label;
  final String value;
  final String caption;
  final Color? tone;
}

class _MetricStrip extends StatelessWidget {
  const _MetricStrip({required this.metrics});

  final List<_MetricData> metrics;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF171614),
        borderRadius: BorderRadius.circular(22),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          if (constraints.maxWidth < 720) {
            return Wrap(
              children: [
                for (final metric in metrics)
                  SizedBox(
                    width: constraints.maxWidth / 2,
                    child: _MetricItem(metric: metric),
                  ),
              ],
            );
          }
          return IntrinsicHeight(
            child: Row(
              children: [
                for (var index = 0; index < metrics.length; index++) ...[
                  Expanded(child: _MetricItem(metric: metrics[index])),
                  if (index != metrics.length - 1)
                    const VerticalDivider(
                      width: 1,
                      thickness: 1,
                      indent: 9,
                      endIndent: 9,
                      color: Color(0xFF3A3835),
                    ),
                ],
              ],
            ),
          );
        },
      ),
    );
  }
}

class _MetricItem extends StatelessWidget {
  const _MetricItem({required this.metric});

  final _MetricData metric;

  @override
  Widget build(BuildContext context) {
    final tone = metric.tone ?? AgendaThemeTokens.of(context).accent;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 21, vertical: 17),
      child: Row(
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: const Color(0xFF272522),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(metric.icon, color: tone, size: 21),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  metric.label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Color(0xFFD8D3CE),
                    fontSize: 11.5,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  metric.value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 21,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                Text(
                  metric.caption,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Color(0xFFAAA39D),
                    fontSize: 10.5,
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

class _OverviewGrid extends StatelessWidget {
  const _OverviewGrid({
    required this.customers,
    required this.professionals,
    required this.services,
    required this.monthAppointments,
    required this.averageRevenue,
    required this.monthRevenue,
    required this.onManageCustomers,
    required this.onManageProfessionals,
    required this.onManageServices,
    required this.onNewCustomer,
    required this.onEditCustomer,
    required this.onEditProfessional,
    required this.onEditService,
  });

  final List<Customer> customers;
  final List<Professional> professionals;
  final List<ServiceItem> services;
  final int monthAppointments;
  final double averageRevenue;
  final double monthRevenue;
  final VoidCallback onManageCustomers;
  final VoidCallback onManageProfessionals;
  final VoidCallback onManageServices;
  final VoidCallback onNewCustomer;
  final ValueChanged<Customer> onEditCustomer;
  final ValueChanged<Professional> onEditProfessional;
  final ValueChanged<ServiceItem> onEditService;

  @override
  Widget build(BuildContext context) {
    final recentCustomers = [...customers]
      ..sort((a, b) => b.lastSeenAt.compareTo(a.lastSeenAt));
    final activeProfessionals = professionals
        .where((item) => item.isActive)
        .take(4)
        .toList();
    final activeServices = services
        .where((item) => item.isActive)
        .toList()
        .reversed
        .take(4)
        .toList();

    final panels = <Widget>[
      _OverviewPanel(
        title: 'Clientes recentes',
        onManage: onManageCustomers,
        footer: customers.isEmpty
            ? null
            : '+${customers.length} clientes no total',
        empty: customers.isEmpty
            ? AgendaEmptyState(
                icon: Icons.groups_2_outlined,
                title: 'Nenhum cliente cadastrado',
                message:
                    'Os clientes criados nos últimos dias aparecerão aqui.',
                actionLabel: 'Novo cliente',
                onAction: onNewCustomer,
                compact: true,
              )
            : null,
        children: [
          for (final item in recentCustomers.take(4))
            _CompactEntityRow(
              icon: Icons.person_outline_rounded,
              title: item.name,
              subtitle: [
                item.phone,
                item.profile,
              ].where((part) => part.trim().isNotEmpty).join(' • '),
              badge: item.acceptsWhatsApp ? 'Ativa' : 'Inativa',
              onTap: () => onEditCustomer(item),
            ),
        ],
      ),
      _OverviewPanel(
        title: 'Equipe disponível',
        onManage: onManageProfessionals,
        footer: '${professionals.length} profissionais no total',
        empty: activeProfessionals.isEmpty
            ? const AgendaEmptyState(
                icon: Icons.badge_outlined,
                title: 'Nenhum profissional cadastrado',
                message: 'Cadastre a equipe para montar a agenda.',
                compact: true,
              )
            : null,
        children: [
          for (final item in activeProfessionals)
            _CompactEntityRow(
              icon: Icons.badge_outlined,
              title: item.name,
              subtitle: item.segmentLine,
              badge: item.role.trim().isEmpty ? 'Equipe' : item.role,
              onTap: () => onEditProfessional(item),
            ),
        ],
      ),
      _OverviewPanel(
        title: 'Serviços cadastrados',
        onManage: onManageServices,
        footer: '${services.length} serviços no catálogo',
        empty: activeServices.isEmpty
            ? const AgendaEmptyState(
                icon: Icons.content_paste_outlined,
                title: 'Nenhum serviço cadastrado',
                message: 'Crie serviços para montar os agendamentos.',
                compact: true,
              )
            : null,
        children: [
          for (final item in activeServices)
            _CompactEntityRow(
              icon: Icons.content_paste_outlined,
              title: item.name,
              subtitle: '${item.durationMinutes} min',
              badge: money(item.price),
              onTap: () => onEditService(item),
            ),
        ],
      ),
      _MonthSummary(
        appointments: monthAppointments,
        averageRevenue: averageRevenue,
        revenue: monthRevenue,
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth >= 1040) {
          return SizedBox(
            height: 340,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(flex: 104, child: panels[0]),
                const SizedBox(width: 12),
                Expanded(flex: 110, child: panels[1]),
                const SizedBox(width: 12),
                Expanded(flex: 130, child: panels[2]),
                const SizedBox(width: 12),
                Expanded(flex: 98, child: panels[3]),
              ],
            ),
          );
        }
        final columns = constraints.maxWidth >= 650 ? 2 : 1;
        final gap = 12.0;
        final width = (constraints.maxWidth - (columns - 1) * gap) / columns;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final panel in panels)
              SizedBox(width: width, height: 340, child: panel),
          ],
        );
      },
    );
  }
}

class _OverviewPanel extends StatelessWidget {
  const _OverviewPanel({
    required this.title,
    required this.onManage,
    required this.children,
    this.footer,
    this.empty,
  });

  final String title;
  final VoidCallback onManage;
  final List<Widget> children;
  final String? footer;
  final Widget? empty;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.fromLTRB(18, 18, 18, 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 15,
                    height: 1.2,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              const SizedBox(width: 7),
              OutlinedButton(
                onPressed: onManage,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size(84, 40),
                  padding: const EdgeInsets.symmetric(horizontal: 10),
                  textStyle: const TextStyle(fontSize: 11.5),
                ),
                child: const Text('Gerenciar'),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Expanded(
            child:
                empty ??
                ListView.separated(
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: children.length,
                  separatorBuilder: (_, _) => Divider(height: 1, color: t.line),
                  itemBuilder: (_, index) => children[index],
                ),
          ),
          if (footer != null) ...[
            const SizedBox(height: 8),
            Text(
              footer!,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: t.accent,
                fontSize: 11.5,
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _CompactEntityRow extends StatelessWidget {
  const _CompactEntityRow({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.badge,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String badge;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Row(
          children: [
            AgendaIconBadge(icon, size: 32, iconSize: 16),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle.trim().isEmpty
                        ? 'Sem detalhes cadastrados'
                        : subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 9.5),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 5),
            AgendaPill(label: badge),
          ],
        ),
      ),
    );
  }
}

class _MonthSummary extends StatelessWidget {
  const _MonthSummary({
    required this.appointments,
    required this.averageRevenue,
    required this.revenue,
  });

  final int appointments;
  final double averageRevenue;
  final double revenue;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Resumo do mês',
            style: TextStyle(
              color: t.ink,
              fontSize: 15.5,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 11),
          _SummaryItem(
            icon: Icons.calendar_month_outlined,
            label: 'Atendimentos',
            value: '$appointments',
            suffix: 'no mês',
            background: t.warmSoft,
          ),
          const SizedBox(height: 8),
          _SummaryItem(
            icon: Icons.payments_outlined,
            label: 'Receita média',
            value: money(averageRevenue),
            background: const Color(0xFFF3FCF8),
            tone: const Color(0xFF16A34A),
          ),
          const SizedBox(height: 8),
          _SummaryItem(
            icon: Icons.star_rounded,
            label: 'Receita do mês',
            value: money(revenue, cents: false),
            background: const Color(0xFFFFF8F0),
            tone: const Color(0xFFF59E0B),
          ),
        ],
      ),
    );
  }
}

class _SummaryItem extends StatelessWidget {
  const _SummaryItem({
    required this.icon,
    required this.label,
    required this.value,
    required this.background,
    this.suffix,
    this.tone,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color background;
  final String? suffix;
  final Color? tone;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final activeTone = tone ?? t.accent;
    return Container(
      padding: const EdgeInsets.all(9),
      decoration: BoxDecoration(
        color: background,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          AgendaIconBadge(
            icon,
            size: 32,
            iconSize: 17,
            color: activeTone,
            background: activeTone.withValues(alpha: .08),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(color: t.muted, fontSize: 10.5)),
                const SizedBox(height: 2),
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        value,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    if (suffix != null) ...[
                      const SizedBox(width: 4),
                      Text(
                        suffix!,
                        style: TextStyle(color: t.muted, fontSize: 9.5),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
