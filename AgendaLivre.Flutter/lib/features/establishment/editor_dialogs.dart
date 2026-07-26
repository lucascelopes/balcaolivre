import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import 'customer_account_dialog.dart';

Future<bool> showCustomerEditorDialog(
  BuildContext context, {
  required AgendaController controller,
  Customer? customer,
}) async {
  return await showDialog<bool>(
        context: context,
        barrierColor: const Color(0xC0000000),
        barrierDismissible: false,
        builder: (_) =>
            _CustomerEditorDialog(controller: controller, customer: customer),
      ) ??
      false;
}

Future<bool> showProfessionalEditorDialog(
  BuildContext context, {
  required AgendaController controller,
  Professional? professional,
}) async {
  return await showDialog<bool>(
        context: context,
        barrierColor: const Color(0xC0000000),
        barrierDismissible: false,
        builder: (_) => _ProfessionalEditorDialog(
          controller: controller,
          professional: professional,
        ),
      ) ??
      false;
}

Future<bool> showServiceEditorDialog(
  BuildContext context, {
  required AgendaController controller,
  ServiceItem? service,
}) async {
  return await showDialog<bool>(
        context: context,
        barrierColor: const Color(0xC0000000),
        barrierDismissible: false,
        builder: (_) =>
            _ServiceEditorDialog(controller: controller, service: service),
      ) ??
      false;
}

Future<bool> showProductEditorDialog(
  BuildContext context, {
  required AgendaController controller,
  ProductItem? product,
}) async {
  return await showDialog<bool>(
        context: context,
        barrierColor: const Color(0xC0000000),
        barrierDismissible: false,
        builder: (_) =>
            _ProductEditorDialog(controller: controller, product: product),
      ) ??
      false;
}

Future<void> showCustomerManagerDialog(
  BuildContext context, {
  required AgendaController controller,
}) => showDialog<void>(
  context: context,
  builder: (_) => _EntityManagerDialog(
    controller: controller,
    kind: _ManagerKind.customers,
  ),
);

Future<void> showProfessionalManagerDialog(
  BuildContext context, {
  required AgendaController controller,
}) => showDialog<void>(
  context: context,
  barrierColor: const Color(0xC0000000),
  barrierDismissible: false,
  builder: (_) => _ProfessionalManagerDialog(controller: controller),
);

Future<void> showServiceManagerDialog(
  BuildContext context, {
  required AgendaController controller,
}) => showDialog<void>(
  context: context,
  barrierColor: const Color(0xC0000000),
  barrierDismissible: false,
  builder: (_) => _ServiceManagerDialog(controller: controller),
);

Future<void> showProductManagerDialog(
  BuildContext context, {
  required AgendaController controller,
}) => showDialog<void>(
  context: context,
  barrierColor: const Color(0xC0000000),
  builder: (_) =>
      _EntityManagerDialog(controller: controller, kind: _ManagerKind.products),
);

enum _ManagerKind { customers, professionals, services, products }

class _EntityManagerDialog extends StatelessWidget {
  const _EntityManagerDialog({required this.controller, required this.kind});

  final AgendaController controller;
  final _ManagerKind kind;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Dialog(
      insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 920, maxHeight: 650),
        child: AnimatedBuilder(
          animation: controller,
          builder: (context, _) {
            final items = switch (kind) {
              _ManagerKind.customers => controller.data.customers,
              _ManagerKind.professionals => controller.data.professionals,
              _ManagerKind.services => controller.data.services,
              _ManagerKind.products => controller.data.products,
            };
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(22, 19, 14, 16),
                  child: LayoutBuilder(
                    builder: (context, constraints) {
                      final heading = Row(
                        children: [
                          AgendaIconBadge(_icon, size: 44, iconSize: 22),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _title,
                                  style: TextStyle(
                                    color: t.ink,
                                    fontSize: 20,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  '${items.length} ${_countLabel(items.length)}',
                                  style: TextStyle(
                                    color: t.muted,
                                    fontSize: 12,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          IconButton(
                            tooltip: 'Fechar',
                            onPressed: () => Navigator.of(context).pop(),
                            icon: const Icon(Icons.close_rounded),
                          ),
                        ],
                      );
                      final create = ElevatedButton.icon(
                        onPressed: () => _create(context),
                        icon: const Icon(Icons.add_rounded, size: 18),
                        label: Text(_newLabel),
                      );
                      if (constraints.maxWidth < 560) {
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            heading,
                            const SizedBox(height: 10),
                            create,
                          ],
                        );
                      }
                      return Row(
                        children: [
                          Expanded(child: heading),
                          const SizedBox(width: 10),
                          create,
                        ],
                      );
                    },
                  ),
                ),
                Divider(height: 1, color: t.line),
                Expanded(
                  child: items.isEmpty
                      ? Center(
                          child: AgendaEmptyState(
                            icon: _icon,
                            title: _emptyTitle,
                            message: _emptyMessage,
                            actionLabel: _newLabel,
                            onAction: () => _create(context),
                          ),
                        )
                      : ListView.separated(
                          padding: const EdgeInsets.all(16),
                          itemCount: items.length,
                          separatorBuilder: (_, _) => const SizedBox(height: 8),
                          itemBuilder: (context, index) {
                            final item = items[index];
                            return _managerRow(
                              context,
                              item: item,
                              onTap: () => _edit(context, item),
                            );
                          },
                        ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  String get _title => switch (kind) {
    _ManagerKind.customers => 'Gerenciar clientes',
    _ManagerKind.professionals => 'Gerenciar profissionais',
    _ManagerKind.services => 'Gerenciar serviços',
    _ManagerKind.products => 'Gerenciar produtos',
  };

  String get _newLabel => switch (kind) {
    _ManagerKind.customers => 'Novo cliente',
    _ManagerKind.professionals => 'Novo profissional',
    _ManagerKind.services => 'Novo serviço',
    _ManagerKind.products => 'Novo produto',
  };

  String get _emptyTitle => switch (kind) {
    _ManagerKind.customers => 'Nenhum cliente cadastrado',
    _ManagerKind.professionals => 'Nenhum profissional cadastrado',
    _ManagerKind.services => 'Nenhum serviço cadastrado',
    _ManagerKind.products => 'Nenhum produto cadastrado',
  };

  String get _emptyMessage => switch (kind) {
    _ManagerKind.customers =>
      'Cadastre clientes para acompanhar o relacionamento.',
    _ManagerKind.professionals =>
      'Cadastre a equipe responsável pelos atendimentos.',
    _ManagerKind.services => 'Crie serviços para liberar novos agendamentos.',
    _ManagerKind.products =>
      'Cadastre produtos para controlar estoque e registrar vendas.',
  };

  IconData get _icon => switch (kind) {
    _ManagerKind.customers => Icons.groups_2_outlined,
    _ManagerKind.professionals => Icons.badge_outlined,
    _ManagerKind.services => Icons.content_paste_outlined,
    _ManagerKind.products => Icons.inventory_2_outlined,
  };

  String _countLabel(int count) => switch (kind) {
    _ManagerKind.customers =>
      count == 1 ? 'cliente cadastrado' : 'clientes cadastrados',
    _ManagerKind.professionals =>
      count == 1 ? 'profissional cadastrado' : 'profissionais cadastrados',
    _ManagerKind.services =>
      count == 1 ? 'serviço cadastrado' : 'serviços cadastrados',
    _ManagerKind.products =>
      count == 1 ? 'produto cadastrado' : 'produtos cadastrados',
  };

  Future<void> _create(BuildContext context) async {
    switch (kind) {
      case _ManagerKind.customers:
        await showCustomerEditorDialog(context, controller: controller);
        return;
      case _ManagerKind.professionals:
        await showProfessionalEditorDialog(context, controller: controller);
        return;
      case _ManagerKind.services:
        await showServiceEditorDialog(context, controller: controller);
        return;
      case _ManagerKind.products:
        await showProductEditorDialog(context, controller: controller);
        return;
    }
  }

  Future<void> _edit(BuildContext context, Object item) async {
    switch (kind) {
      case _ManagerKind.customers:
        await showCustomerEditorDialog(
          context,
          controller: controller,
          customer: item as Customer,
        );
        return;
      case _ManagerKind.professionals:
        await showProfessionalEditorDialog(
          context,
          controller: controller,
          professional: item as Professional,
        );
        return;
      case _ManagerKind.services:
        await showServiceEditorDialog(
          context,
          controller: controller,
          service: item as ServiceItem,
        );
        return;
      case _ManagerKind.products:
        await showProductEditorDialog(
          context,
          controller: controller,
          product: item as ProductItem,
        );
        return;
    }
  }

  Widget _managerRow(
    BuildContext context, {
    required Object item,
    required VoidCallback onTap,
  }) {
    final t = AgendaThemeTokens.of(context);
    final openAccountItems = item is Customer
        ? controller.openCustomerReceivables(
            customerId: item.id,
            customerName: item.name,
          )
        : const <CustomerReceivable>[];
    final openAccountBalance = openAccountItems.fold(
      0.0,
      (total, receivable) => total + receivable.remainingValue,
    );
    final (title, subtitle, trailing) = switch (item) {
      Customer value => (
        value.name,
        [
          value.phone,
          value.profile,
        ].where((part) => part.trim().isNotEmpty).join(' • '),
        openAccountBalance > 0
            ? money(openAccountBalance)
            : value.acceptsWhatsApp
            ? 'WhatsApp ativo'
            : 'WhatsApp inativo',
      ),
      Professional value => (
        value.name,
        value.segmentLine,
        value.isActive ? 'Ativo' : 'Inativo',
      ),
      ServiceItem value => (
        value.name,
        '${value.durationMinutes} min • R\$ ${value.price.toStringAsFixed(2).replaceAll('.', ',')}',
        value.isActive ? 'Ativo' : 'Inativo',
      ),
      ProductItem value => (
        value.name,
        [
          if (value.category.trim().isNotEmpty) value.category,
          '${value.stockQuantity} em estoque',
          if (value.sku.trim().isNotEmpty) 'SKU ${value.sku}',
        ].join(' • '),
        value.isActive ? _formatCurrency(value.price) : 'Inativo',
      ),
      _ => ('', '', ''),
    };
    return Material(
      color: t.warmSoft,
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: t.accentSoft,
                foregroundColor: t.accentDark,
                child: Text(
                  title.trim().isEmpty
                      ? '?'
                      : title.trim().substring(0, 1).toUpperCase(),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
              const SizedBox(width: 12),
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
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    if (subtitle.isNotEmpty) ...[
                      const SizedBox(height: 3),
                      Text(
                        subtitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: t.muted, fontSize: 12),
                      ),
                    ],
                  ],
                ),
              ),
              AgendaPill(label: trailing),
              if (item is Customer && openAccountBalance > 0) ...[
                const SizedBox(width: 4),
                IconButton(
                  key: Key('receive-customer-account-${item.id}'),
                  tooltip: 'Receber saldo',
                  onPressed: () async {
                    final received = await showCustomerAccountDialog(
                      context,
                      controller: controller,
                      customer: item,
                    );
                    if (!context.mounted || !received) return;
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Conta recebida com sucesso.'),
                      ),
                    );
                  },
                  icon: Icon(
                    Icons.account_balance_wallet_outlined,
                    color: t.accent,
                    size: 20,
                  ),
                ),
              ],
              const SizedBox(width: 6),
              Icon(Icons.chevron_right_rounded, color: t.muted),
            ],
          ),
        ),
      ),
    );
  }
}

class _ServiceManagerDialog extends StatefulWidget {
  const _ServiceManagerDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_ServiceManagerDialog> createState() => _ServiceManagerDialogState();
}

class _ServiceManagerDialogState extends State<_ServiceManagerDialog> {
  final _search = TextEditingController();
  String? _selectedServiceId;
  bool _activeOnly = false;

  @override
  void initState() {
    super.initState();
    _search.addListener(_refresh);
  }

  @override
  void dispose() {
    _search.removeListener(_refresh);
    _search.dispose();
    super.dispose();
  }

  void _refresh() {
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final services = [
          ...widget.controller.data.services,
        ]..sort((a, b) => a.name.toLowerCase().compareTo(b.name.toLowerCase()));
        final query = _search.text.trim().toLowerCase();
        final filtered = services.where((service) {
          if (_activeOnly && !service.isActive) return false;
          if (query.isEmpty) return true;
          return service.name.toLowerCase().contains(query) ||
              service.category.toLowerCase().contains(query) ||
              service.segment.toLowerCase().contains(query) ||
              service.defaultResource.toLowerCase().contains(query);
        }).toList();
        ServiceItem? selected;
        if (filtered.isNotEmpty) {
          selected = filtered.firstWhere(
            (service) => service.id == _selectedServiceId,
            orElse: () => filtered.first,
          );
        }

        final size = MediaQuery.sizeOf(context);
        final desktop = size.width >= 900;
        final frameWidth = math.max(0.0, math.min(948.0, size.width - 32));
        final frameHeight = math.max(
          0.0,
          math.min(desktop ? 581.0 : 720.0, size.height - 32),
        );

        return Dialog(
          backgroundColor: Colors.transparent,
          elevation: 0,
          insetPadding: const EdgeInsets.all(16),
          child: _DialogFrame(
            key: const ValueKey('service-manager-dialog-frame'),
            width: frameWidth,
            height: frameHeight,
            child: Column(
              children: [
                _header(context, desktop: desktop),
                Expanded(
                  child: desktop
                      ? _desktopBody(
                          context,
                          services: services,
                          filtered: filtered,
                          selected: selected,
                        )
                      : _mobileBody(
                          context,
                          services: services,
                          filtered: filtered,
                          selected: selected,
                        ),
                ),
                _footer(context, desktop: desktop),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _header(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      height: desktop ? 82 : 104,
      padding: EdgeInsets.fromLTRB(22, desktop ? 16 : 13, 22, 13),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 40,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(14),
            ),
            alignment: Alignment.center,
            child: Icon(Icons.assignment_outlined, color: t.accent, size: 21),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Gerenciar serviços',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: desktop ? 20 : 18,
                    height: 1.1,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  'Catálogo com duração, preço e recurso padrão de atendimento.',
                  maxLines: desktop ? 1 : 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 12.5),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          _DialogCloseButton(onPressed: () => Navigator.of(context).pop()),
        ],
      ),
    );
  }

  Widget _desktopBody(
    BuildContext context, {
    required List<ServiceItem> services,
    required List<ServiceItem> filtered,
    required ServiceItem? selected,
  }) {
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: Colors.white,
      child: Row(
        children: [
          SizedBox(
            width: 440,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(22, 16, 8, 10),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _searchAndFilter(context),
                  const SizedBox(height: 11),
                  Text(
                    _countText(filtered.length),
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 7),
                  Expanded(
                    child: _serviceList(
                      context,
                      filtered: filtered,
                      selected: selected,
                    ),
                  ),
                ],
              ),
            ),
          ),
          VerticalDivider(width: 1, thickness: 1, color: t.line),
          Expanded(
            child: selected == null
                ? _emptySelection(context, services.isEmpty)
                : _serviceDetails(context, selected),
          ),
        ],
      ),
    );
  }

  Widget _mobileBody(
    BuildContext context, {
    required List<ServiceItem> services,
    required List<ServiceItem> filtered,
    required ServiceItem? selected,
  }) {
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: Colors.white,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 18),
        children: [
          _searchAndFilter(context),
          const SizedBox(height: 10),
          Text(
            _countText(filtered.length),
            style: TextStyle(
              color: t.ink,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 8),
          if (filtered.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 36),
              child: _emptySelection(context, services.isEmpty),
            )
          else
            for (final service in filtered)
              Padding(
                padding: const EdgeInsets.only(bottom: 2),
                child: _serviceListRow(
                  context,
                  service,
                  selected: service.id == selected?.id,
                ),
              ),
          if (selected != null) ...[
            const SizedBox(height: 14),
            Divider(height: 1, color: t.line),
            const SizedBox(height: 14),
            _serviceDetails(context, selected, compact: true),
          ],
        ],
      ),
    );
  }

  Widget _searchAndFilter(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        Expanded(
          child: SizedBox(
            height: 44,
            child: TextField(
              key: const ValueKey('service-manager-search'),
              controller: _search,
              style: TextStyle(color: t.ink, fontSize: 12.5),
              decoration: _dialogInputDecoration(
                context,
                hintText: 'Buscar serviços...',
                contentPadding: const EdgeInsets.symmetric(horizontal: 14),
              ),
            ),
          ),
        ),
        const SizedBox(width: 8),
        Tooltip(
          message: 'Exibir somente serviços ativos',
          child: SizedBox.square(
            dimension: 44,
            child: OutlinedButton(
              key: const ValueKey('service-manager-filter'),
              onPressed: () => setState(() => _activeOnly = !_activeOnly),
              style: OutlinedButton.styleFrom(
                foregroundColor: _activeOnly ? t.accent : t.ink,
                backgroundColor: _activeOnly ? t.accentSoft : Colors.white,
                padding: EdgeInsets.zero,
                side: BorderSide(color: _activeOnly ? t.accent : t.line),
              ),
              child: const Icon(Icons.filter_list_rounded, size: 18),
            ),
          ),
        ),
      ],
    );
  }

  Widget _serviceList(
    BuildContext context, {
    required List<ServiceItem> filtered,
    required ServiceItem? selected,
  }) {
    if (filtered.isEmpty) {
      return _emptySelection(context, widget.controller.data.services.isEmpty);
    }
    return ListView.builder(
      itemExtent: 56,
      itemCount: filtered.length,
      itemBuilder: (context, index) {
        final service = filtered[index];
        return _serviceListRow(
          context,
          service,
          selected: service.id == selected?.id,
        );
      },
    );
  }

  Widget _serviceListRow(
    BuildContext context,
    ServiceItem service, {
    required bool selected,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Material(
      color: selected ? t.accentSoft.withValues(alpha: .78) : Colors.white,
      borderRadius: BorderRadius.circular(selected ? 14 : 0),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => setState(() => _selectedServiceId = service.id),
        child: Row(
          children: [
            SizedBox(
              width: 4,
              height: 28,
              child: ColoredBox(
                color: selected ? t.accent : Colors.transparent,
              ),
            ),
            const SizedBox(width: 7),
            Container(
              width: 32,
              height: 32,
              decoration: BoxDecoration(
                color: t.accentSoft,
                borderRadius: BorderRadius.circular(16),
              ),
              alignment: Alignment.center,
              child: Icon(Icons.assignment_outlined, color: t.accent, size: 15),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    service.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${service.durationMinutes} min  •  ${_formatCurrency(service.price)}'
                    '${service.defaultResource.trim().isEmpty ? '' : '  •  ${service.defaultResource.trim()}'}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 10.8),
                  ),
                ],
              ),
            ),
            _EntityStatusPill(active: service.isActive),
            if (selected) ...[
              const SizedBox(width: 5),
              Icon(Icons.chevron_right_rounded, color: t.muted, size: 18),
            ],
            const SizedBox(width: 8),
          ],
        ),
      ),
    );
  }

  Widget _serviceDetails(
    BuildContext context,
    ServiceItem service, {
    bool compact = false,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: EdgeInsets.fromLTRB(
        compact ? 0 : 56,
        compact ? 0 : 20,
        compact ? 0 : 22,
        compact ? 4 : 12,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: compact ? MainAxisSize.min : MainAxisSize.max,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: compact ? 54 : 68,
                height: compact ? 54 : 68,
                decoration: BoxDecoration(
                  color: t.accentDark,
                  borderRadius: BorderRadius.circular(18),
                ),
                alignment: Alignment.center,
                child: Icon(
                  Icons.content_cut_rounded,
                  color: Colors.white,
                  size: compact ? 25 : 32,
                ),
              ),
              const SizedBox(width: 18),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      service.name,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: compact ? 19 : 21,
                        height: 1.1,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 6),
                    _EntityStatusPill(active: service.isActive),
                    const SizedBox(height: 8),
                    Text(
                      service.description.trim().isEmpty
                          ? service.isActive
                                ? 'Serviço ativo e disponível para agendamento.'
                                : 'Serviço inativo e indisponível para agendamento.'
                          : service.description.trim(),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.muted, fontSize: 11.2),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 10),
              OutlinedButton.icon(
                onPressed: () => showServiceEditorDialog(
                  context,
                  controller: widget.controller,
                  service: service,
                ),
                style: OutlinedButton.styleFrom(
                  foregroundColor: t.accent,
                  minimumSize: Size(compact ? 92 : 112, 42),
                ),
                icon: const Icon(Icons.edit_rounded, size: 15),
                label: const Text('Editar'),
              ),
            ],
          ),
          SizedBox(height: compact ? 12 : 18),
          _detailLine(
            context,
            icon: Icons.schedule_outlined,
            label: 'Duração',
            value: '${service.durationMinutes} min',
          ),
          _detailLine(
            context,
            icon: Icons.sell_outlined,
            label: 'Preço',
            value: _formatCurrency(service.price),
          ),
          _detailLine(
            context,
            icon: Icons.event_seat_outlined,
            label: 'Recurso padrão',
            value: _firstFilled([
              service.defaultResource,
            ], fallback: 'Não definido'),
          ),
          _detailLine(
            context,
            icon: Icons.storefront_outlined,
            label: 'Categoria',
            value: _firstFilled([service.category], fallback: 'Sem categoria'),
            last: true,
          ),
        ],
      ),
    );
  }

  Widget _detailLine(
    BuildContext context, {
    required IconData icon,
    required String label,
    required String value,
    bool last = false,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      height: 62,
      decoration: BoxDecoration(
        border: last ? null : Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Icon(icon, color: t.muted, size: 18),
          const SizedBox(width: 13),
          Expanded(
            child: Text(
              label,
              style: TextStyle(color: t.muted, fontSize: 12.5),
            ),
          ),
          const SizedBox(width: 12),
          Flexible(
            child: Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.end,
              style: TextStyle(
                color: t.ink,
                fontSize: 12.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _emptySelection(BuildContext context, bool noServices) {
    final t = AgendaThemeTokens.of(context);
    return Center(
      child: Text(
        noServices
            ? 'Nenhum serviço cadastrado.'
            : 'Nenhum serviço encontrado.',
        textAlign: TextAlign.center,
        style: TextStyle(color: t.muted, fontSize: 12.5),
      ),
    );
  }

  Widget _footer(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    final cancel = OutlinedButton(
      onPressed: () => Navigator.of(context).pop(),
      style: OutlinedButton.styleFrom(minimumSize: Size(desktop ? 108 : 0, 40)),
      child: const Text('Cancelar'),
    );
    final create = ElevatedButton.icon(
      onPressed: () =>
          showServiceEditorDialog(context, controller: widget.controller),
      style: ElevatedButton.styleFrom(minimumSize: Size(desktop ? 150 : 0, 40)),
      icon: const Icon(Icons.add_rounded, size: 17),
      label: const Text('Novo serviço'),
    );
    return Container(
      height: 72,
      padding: EdgeInsets.symmetric(
        horizontal: desktop ? 22 : 16,
        vertical: 14,
      ),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: t.line)),
      ),
      child: desktop
          ? Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [cancel, const SizedBox(width: 10), create],
            )
          : Row(
              children: [
                Expanded(child: cancel),
                const SizedBox(width: 10),
                Expanded(child: create),
              ],
            ),
    );
  }

  String _countText(int count) {
    final label = count == 1 ? 'serviço' : 'serviços';
    return '$count $label';
  }
}

class _EntityStatusPill extends StatelessWidget {
  const _EntityStatusPill({required this.active});

  final bool active;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: active ? const Color(0xFFE8F7EE) : t.graySoft,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        active ? 'ativo' : 'inativo',
        style: TextStyle(
          color: active ? const Color(0xFF15803D) : t.muted,
          fontSize: 10,
          height: 1,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _ProfessionalManagerDialog extends StatefulWidget {
  const _ProfessionalManagerDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_ProfessionalManagerDialog> createState() =>
      _ProfessionalManagerDialogState();
}

class _ProfessionalManagerDialogState
    extends State<_ProfessionalManagerDialog> {
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    _search.addListener(_refreshSearch);
  }

  @override
  void dispose() {
    _search.removeListener(_refreshSearch);
    _search.dispose();
    super.dispose();
  }

  void _refreshSearch() {
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final professionals = [...widget.controller.data.professionals]
          ..sort((a, b) => a.name.compareTo(b.name));
        final query = _search.text.trim().toLowerCase();
        final filtered = professionals.where((professional) {
          if (query.isEmpty) return true;
          return professional.name.toLowerCase().contains(query) ||
              professional.role.toLowerCase().contains(query) ||
              professional.segments.any(
                (segment) => segment.toLowerCase().contains(query),
              );
        }).toList();
        final size = MediaQuery.sizeOf(context);
        final desktop = size.width >= 1200;
        final visibleRows = math.max(1, math.min(filtered.length, 4));
        final desiredHeight = desktop
            ? 338.0 + ((visibleRows - 1) * 64)
            : 408.0 + ((visibleRows - 1) * 112);
        final frameWidth = desktop
            ? 828.0
            : math.max(0.0, math.min(828.0, size.width - 32));
        final frameHeight = math.min(
          desiredHeight,
          math.max(0.0, size.height - 32),
        );

        return Dialog(
          backgroundColor: Colors.transparent,
          elevation: 0,
          insetPadding: const EdgeInsets.all(16),
          child: _DialogFrame(
            key: const ValueKey('professional-manager-dialog-frame'),
            width: frameWidth,
            height: frameHeight,
            child: Column(
              children: [
                _professionalHeader(context, desktop: desktop),
                Expanded(
                  child: desktop
                      ? _professionalDesktopBody(
                          context,
                          professionals: professionals,
                          filtered: filtered,
                        )
                      : _professionalMobileBody(
                          context,
                          professionals: professionals,
                          filtered: filtered,
                        ),
                ),
                _professionalFooter(context, desktop: desktop),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _professionalHeader(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      height: desktop ? 82 : 106,
      padding: EdgeInsets.fromLTRB(22, desktop ? 18 : 14, 22, 14),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 40,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(14),
            ),
            alignment: Alignment.center,
            child: Icon(Icons.group_outlined, color: t.accent, size: 21),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Gerenciar profissionais',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: desktop ? 20 : 18,
                    fontWeight: FontWeight.w700,
                    height: 1.1,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  'Equipe, função e vínculo com o segmento da agenda.',
                  maxLines: desktop ? 1 : 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 12.5),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          _DialogCloseButton(onPressed: () => Navigator.of(context).pop()),
        ],
      ),
    );
  }

  Widget _professionalDesktopBody(
    BuildContext context, {
    required List<Professional> professionals,
    required List<Professional> filtered,
  }) {
    return ColoredBox(
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(22, 16, 22, 14),
        child: Column(
          children: [
            Row(
              children: [
                Expanded(child: _professionalSearchField(context)),
                const SizedBox(width: 14),
                _professionalCountBadge(
                  context,
                  filteredCount: filtered.length,
                  totalCount: professionals.length,
                ),
              ],
            ),
            const SizedBox(height: 14),
            Expanded(
              child: _professionalDesktopTable(
                context,
                professionals: professionals,
                filtered: filtered,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _professionalMobileBody(
    BuildContext context, {
    required List<Professional> professionals,
    required List<Professional> filtered,
  }) {
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _professionalSearchField(context),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerLeft,
              child: _professionalCountBadge(
                context,
                filteredCount: filtered.length,
                totalCount: professionals.length,
              ),
            ),
            const SizedBox(height: 10),
            Expanded(
              child: filtered.isEmpty
                  ? Center(
                      child: Text(
                        professionals.isEmpty
                            ? 'Nenhum profissional cadastrado.'
                            : 'Nenhum profissional encontrado.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: t.muted, fontSize: 12.5),
                      ),
                    )
                  : ListView.separated(
                      itemCount: filtered.length,
                      separatorBuilder: (_, _) => const SizedBox(height: 8),
                      itemBuilder: (context, index) =>
                          _professionalMobileCard(context, filtered[index]),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _professionalSearchField(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      height: 42,
      child: TextField(
        controller: _search,
        style: TextStyle(color: t.ink, fontSize: 13),
        decoration: _dialogInputDecoration(
          context,
          hintText: 'Buscar por nome, função ou segmento...',
        ),
      ),
    );
  }

  Widget _professionalCountBadge(
    BuildContext context, {
    required int filteredCount,
    required int totalCount,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: t.accentSoft,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        '$filteredCount de $totalCount '
        '${totalCount == 1 ? 'profissional' : 'profissionais'}',
        style: TextStyle(
          color: t.muted,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _professionalDesktopTable(
    BuildContext context, {
    required List<Professional> professionals,
    required List<Professional> filtered,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            height: 34,
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: BoxDecoration(
              color: t.graySoft,
              border: Border(bottom: BorderSide(color: t.line)),
            ),
            child: _professionalColumns(
              context,
              avatar: const SizedBox.shrink(),
              professional: _tableHeader(context, 'PROFISSIONAL'),
              role: _tableHeader(context, 'FUNÇÃO'),
              segment: _tableHeader(context, 'SEGMENTO'),
              status: _tableHeader(context, 'STATUS'),
              actions: _tableHeader(context, 'AÇÕES'),
            ),
          ),
          Expanded(
            child: filtered.isEmpty
                ? Center(
                    child: Text(
                      professionals.isEmpty
                          ? 'Nenhum profissional cadastrado.'
                          : 'Nenhum profissional encontrado.',
                      style: TextStyle(color: t.muted, fontSize: 12.5),
                    ),
                  )
                : ListView.builder(
                    itemExtent: 64,
                    itemCount: filtered.length,
                    itemBuilder: (context, index) =>
                        _professionalDesktopRow(context, filtered[index]),
                  ),
          ),
        ],
      ),
    );
  }

  Widget _tableHeader(BuildContext context, String text) {
    final t = AgendaThemeTokens.of(context);
    return Align(
      alignment: Alignment.centerLeft,
      child: Text(
        text,
        maxLines: 1,
        style: TextStyle(
          color: t.muted,
          fontSize: 10.5,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _professionalDesktopRow(
    BuildContext context,
    Professional professional,
  ) {
    final t = AgendaThemeTokens.of(context);
    final contact = _firstFilled([
      professional.phone,
      professional.email,
    ], fallback: 'Sem contato informado');
    final segment = professional.segments.isEmpty
        ? _firstFilled([
            widget.controller.data.settings.businessSegment,
          ], fallback: 'Agenda')
        : professional.segments.join(', ');
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: _professionalColumns(
        context,
        avatar: CircleAvatar(
          radius: 18,
          backgroundColor: t.accentSoft,
          foregroundColor: t.accent,
          child: Text(
            _managerInitials(professional.name),
            style: const TextStyle(fontSize: 11.5, fontWeight: FontWeight.w700),
          ),
        ),
        professional: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              professional.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: t.ink,
                fontSize: 13,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              contact,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(color: t.muted, fontSize: 10.5),
            ),
          ],
        ),
        role: Text(
          _firstFilled([professional.role], fallback: 'Equipe'),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: t.ink,
            fontSize: 12,
            fontWeight: FontWeight.w600,
          ),
        ),
        segment: Text(
          segment,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(color: t.muted, fontSize: 11.5),
        ),
        status: Align(
          alignment: Alignment.centerLeft,
          child: _professionalStatus(context, professional),
        ),
        actions: Align(
          alignment: Alignment.centerRight,
          child: _professionalEditButton(context, professional),
        ),
      ),
    );
  }

  Widget _professionalColumns(
    BuildContext context, {
    required Widget avatar,
    required Widget professional,
    required Widget role,
    required Widget segment,
    required Widget status,
    required Widget actions,
  }) {
    return Row(
      children: [
        SizedBox(
          width: 42,
          child: Align(alignment: Alignment.centerLeft, child: avatar),
        ),
        Expanded(flex: 200, child: professional),
        Expanded(flex: 125, child: role),
        Expanded(flex: 160, child: segment),
        SizedBox(width: 82, child: status),
        SizedBox(width: 88, child: actions),
      ],
    );
  }

  Widget _professionalMobileCard(
    BuildContext context,
    Professional professional,
  ) {
    final t = AgendaThemeTokens.of(context);
    final contact = _firstFilled([
      professional.phone,
      professional.email,
    ], fallback: 'Sem contato informado');
    final segment = professional.segments.isEmpty
        ? _firstFilled([
            widget.controller.data.settings.businessSegment,
          ], fallback: 'Agenda')
        : professional.segments.join(', ');
    return Container(
      height: 106,
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: t.accentSoft,
                foregroundColor: t.accent,
                child: Text(
                  _managerInitials(professional.name),
                  style: const TextStyle(
                    fontSize: 11.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              const SizedBox(width: 9),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      professional.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      contact,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 6),
              _professionalStatus(context, professional),
            ],
          ),
          const Spacer(),
          Row(
            children: [
              Expanded(
                child: Text(
                  '${_firstFilled([professional.role], fallback: 'Equipe')} • $segment',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11.5),
                ),
              ),
              const SizedBox(width: 8),
              _professionalEditButton(context, professional),
            ],
          ),
        ],
      ),
    );
  }

  Widget _professionalStatus(BuildContext context, Professional professional) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(
        color: professional.isActive ? t.accentSoft : t.graySoft,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        professional.isActive ? 'Ativo' : 'Inativo',
        style: TextStyle(
          color: professional.isActive ? t.accentDark : t.muted,
          fontSize: 10.5,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _professionalEditButton(
    BuildContext context,
    Professional professional,
  ) {
    return OutlinedButton.icon(
      onPressed: () => showProfessionalEditorDialog(
        context,
        controller: widget.controller,
        professional: professional,
      ),
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(74, 34),
        maximumSize: const Size(88, 34),
        padding: const EdgeInsets.symmetric(horizontal: 10),
      ),
      icon: const Icon(Icons.edit_outlined, size: 14),
      label: const Text('Editar'),
    );
  }

  Widget _professionalFooter(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    final cancel = OutlinedButton(
      onPressed: () => Navigator.of(context).pop(),
      style: OutlinedButton.styleFrom(
        fixedSize: desktop ? const Size(108, 40) : null,
        minimumSize: Size(desktop ? 108 : 0, 40),
        padding: const EdgeInsets.symmetric(horizontal: 10),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: const Text('Cancelar'),
    );
    final add = ElevatedButton(
      onPressed: () =>
          showProfessionalEditorDialog(context, controller: widget.controller),
      style: ElevatedButton.styleFrom(
        fixedSize: desktop ? const Size(164, 40) : null,
        minimumSize: Size(desktop ? 164 : 0, 40),
        padding: const EdgeInsets.symmetric(horizontal: 10),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: const Row(
        mainAxisAlignment: MainAxisAlignment.center,
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.add_rounded, size: 17),
          SizedBox(width: 7),
          Flexible(
            child: FittedBox(
              fit: BoxFit.scaleDown,
              child: Text('Novo profissional', maxLines: 1),
            ),
          ),
        ],
      ),
    );
    return Container(
      height: 72,
      padding: EdgeInsets.symmetric(
        horizontal: desktop ? 22 : 16,
        vertical: 14,
      ),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: t.line)),
      ),
      child: desktop
          ? Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [cancel, const SizedBox(width: 10), add],
            )
          : Row(
              children: [
                Expanded(child: cancel),
                const SizedBox(width: 10),
                Expanded(flex: 2, child: add),
              ],
            ),
    );
  }
}

class _CustomerEditorDialog extends StatefulWidget {
  const _CustomerEditorDialog({required this.controller, this.customer});

  final AgendaController controller;
  final Customer? customer;

  @override
  State<_CustomerEditorDialog> createState() => _CustomerEditorDialogState();
}

class _CustomerEditorDialogState extends State<_CustomerEditorDialog> {
  final _nameFocus = FocusNode();
  late final TextEditingController _name;
  late final TextEditingController _phone;
  late final TextEditingController _email;
  late final TextEditingController _document;
  late final TextEditingController _segment;
  late final TextEditingController _profile;
  late final TextEditingController _tags;
  late final TextEditingController _notes;
  late String _preferredTime;
  String? _errorMessage;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final customer = widget.customer;
    final businessSegment = widget.controller.data.settings.businessSegment;
    _name = TextEditingController(text: customer?.name ?? '');
    _phone = TextEditingController(text: customer?.phone ?? '');
    _email = TextEditingController(text: customer?.email ?? '');
    _document = TextEditingController(text: customer?.document ?? '');
    _segment = TextEditingController(
      text: customer?.segment.trim().isNotEmpty == true
          ? customer!.segment
          : _firstFilled([businessSegment], fallback: 'Salão de Beleza'),
    );
    final profile = customer?.profile ?? '';
    _preferredTime = _readCustomerPreferredTime(profile);
    _profile = TextEditingController(
      text: _removeCustomerPreferredTime(profile),
    );
    _tags = TextEditingController(text: customer?.tags ?? '');
    _notes = TextEditingController(text: customer?.notes ?? '');
  }

  @override
  void dispose() {
    _nameFocus.dispose();
    _name.dispose();
    _phone.dispose();
    _email.dispose();
    _document.dispose();
    _segment.dispose();
    _profile.dispose();
    _tags.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final customerName = _name.text.trim();
    if (customerName.isEmpty) {
      setState(() => _errorMessage = 'Informe o nome do cliente.');
      _nameFocus.requestFocus();
      return;
    }
    setState(() {
      _errorMessage = null;
      _saving = true;
    });
    final existing = widget.customer;
    final customer = Customer(
      id: existing?.id,
      name: customerName,
      phone: _phone.text.trim(),
      email: _email.text.trim(),
      document: _document.text.trim(),
      segment: _segment.text.trim(),
      profile: _buildCustomerProfile(_preferredTime, _profile.text),
      tags: _tags.text.trim(),
      notes: _notes.text.trim(),
      acceptsWhatsApp:
          existing?.acceptsWhatsApp ?? _phone.text.trim().isNotEmpty,
      lastSeenAt: existing?.lastSeenAt,
    );
    await widget.controller.saveCustomer(customer);
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    final desktop = size.width >= 1200;
    final frameWidth = desktop
        ? 620.0
        : math.max(0.0, math.min(620.0, size.width - 32));
    final frameHeight = math.min(
      desktop ? 544.0 : 760.0,
      math.max(0.0, size.height - 32),
    );
    final title = widget.customer == null ? 'Criar cliente' : 'Editar cliente';
    final saveLabel = widget.customer == null
        ? 'Salvar cliente'
        : 'Salvar alterações';

    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(16),
      child: _DialogFrame(
        key: const ValueKey('customer-dialog-frame'),
        width: frameWidth,
        height: frameHeight,
        child: Column(
          children: [
            _customerHeader(context, desktop: desktop, title: title),
            Expanded(child: _customerBody(context, desktop: desktop)),
            _customerFooter(context, desktop: desktop, saveLabel: saveLabel),
          ],
        ),
      ),
    );
  }

  Widget _customerHeader(
    BuildContext context, {
    required bool desktop,
    required String title,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      height: desktop ? 88 : 118,
      padding: EdgeInsets.fromLTRB(26, desktop ? 18 : 14, 22, 14),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(14),
            ),
            alignment: Alignment.center,
            child: Icon(
              Icons.person_outline_rounded,
              color: t.accent,
              size: 23,
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 22,
                    fontWeight: FontWeight.w700,
                    height: 1.05,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Cadastre os dados essenciais para agendar e manter o histórico.',
                  maxLines: desktop ? 1 : 3,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 13),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          _DialogCloseButton(
            onPressed: _saving ? null : () => Navigator.of(context).pop(false),
          ),
        ],
      ),
    );
  }

  Widget _customerBody(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: Colors.white,
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(
          desktop ? 26 : 16,
          18,
          desktop ? 26 : 16,
          10,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Dados do cliente',
              style: TextStyle(
                color: t.ink,
                fontSize: 18,
                fontWeight: FontWeight.w600,
              ),
            ),
            if (_errorMessage != null) ...[
              const SizedBox(height: 5),
              Text(
                _errorMessage!,
                style: const TextStyle(
                  color: Color(0xFFDC2626),
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
            const SizedBox(height: 8),
            _customerFieldRow(
              desktop: desktop,
              first: _LabeledDialogField(
                label: 'Nome do cliente',
                child: _dialogTextField(
                  context,
                  controller: _name,
                  focusNode: _nameFocus,
                  autofocus: true,
                  hintText: 'Digite o nome do cliente',
                  textInputAction: TextInputAction.next,
                ),
              ),
              second: _LabeledDialogField(
                label: 'WhatsApp principal',
                child: _dialogTextField(
                  context,
                  controller: _phone,
                  hintText: '(11) 9 9999-9999',
                  keyboardType: TextInputType.phone,
                  textInputAction: TextInputAction.next,
                ),
              ),
            ),
            const SizedBox(height: 14),
            _customerFieldRow(
              desktop: desktop,
              first: _LabeledDialogField(
                label: 'Tags',
                child: _customerTagsField(context),
              ),
              second: _LabeledDialogField(
                label: 'Segmento',
                child: _lockedSegmentField(context),
              ),
            ),
            const SizedBox(height: 14),
            _LabeledDialogField(
              label: 'Preferência de horário',
              child: _preferredTimePicker(context),
            ),
            const SizedBox(height: 14),
            _LabeledDialogField(
              label: 'Preferências, alergias e observações',
              child: SizedBox(
                height: 64,
                child: TextField(
                  controller: _profile,
                  minLines: 2,
                  maxLines: 3,
                  style: TextStyle(color: t.ink, fontSize: 13),
                  decoration: _dialogInputDecoration(
                    context,
                    hintText:
                        'Ex: cor preferida, alergias, produtos preferidos, observações importantes...',
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 9,
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

  Widget _customerFieldRow({
    required bool desktop,
    required Widget first,
    required Widget second,
  }) {
    if (!desktop) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [first, const SizedBox(height: 14), second],
      );
    }
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(child: first),
        const SizedBox(width: 16),
        Expanded(child: second),
      ],
    );
  }

  Widget _dialogTextField(
    BuildContext context, {
    required TextEditingController controller,
    required String hintText,
    FocusNode? focusNode,
    bool autofocus = false,
    TextInputType? keyboardType,
    TextInputAction? textInputAction,
  }) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      height: 42,
      child: TextField(
        controller: controller,
        focusNode: focusNode,
        autofocus: autofocus,
        keyboardType: keyboardType,
        textInputAction: textInputAction,
        style: TextStyle(color: t.ink, fontSize: 13),
        decoration: _dialogInputDecoration(context, hintText: hintText),
      ),
    );
  }

  Widget _customerTagsField(BuildContext context) {
    final options = _customerTagOptions(widget.controller.data.customers);
    return MenuAnchor(
      menuChildren: [
        for (final option in options)
          MenuItemButton(
            onPressed: () {
              _tags.text = option;
              _tags.selection = TextSelection.collapsed(offset: option.length);
            },
            child: Text(option),
          ),
      ],
      builder: (context, menuController, _) {
        final t = AgendaThemeTokens.of(context);
        return SizedBox(
          height: 42,
          child: TextField(
            controller: _tags,
            style: TextStyle(color: t.ink, fontSize: 13),
            decoration: _dialogInputDecoration(
              context,
              hintText: 'Selecione ou crie tags',
              suffixIcon: IconButton(
                tooltip: 'Mostrar tags',
                onPressed: () => menuController.isOpen
                    ? menuController.close()
                    : menuController.open(),
                icon: const Icon(Icons.arrow_drop_down_rounded),
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _lockedSegmentField(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      height: 42,
      padding: const EdgeInsets.symmetric(horizontal: 14),
      decoration: BoxDecoration(
        color: t.graySoft,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          Icon(Icons.lock_outline_rounded, size: 16, color: t.muted),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              _segment.text,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(color: t.ink, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }

  Widget _preferredTimePicker(BuildContext context) {
    const options = <(String, IconData)>[
      ('Manhã', Icons.wb_sunny_outlined),
      ('Tarde', Icons.wb_twilight_outlined),
      ('Noite', Icons.nightlight_outlined),
    ];
    return Row(
      children: [
        for (var index = 0; index < options.length; index++) ...[
          if (index > 0) const SizedBox(width: 12),
          Expanded(
            child: _PreferredTimeButton(
              label: options[index].$1,
              icon: options[index].$2,
              selected: _preferredTime == options[index].$1,
              onPressed: () {
                setState(() {
                  _preferredTime = _preferredTime == options[index].$1
                      ? ''
                      : options[index].$1;
                });
              },
            ),
          ),
        ],
      ],
    );
  }

  Widget _customerFooter(
    BuildContext context, {
    required bool desktop,
    required String saveLabel,
  }) {
    final t = AgendaThemeTokens.of(context);
    final cancel = OutlinedButton(
      onPressed: _saving ? null : () => Navigator.of(context).pop(false),
      style: OutlinedButton.styleFrom(
        fixedSize: desktop ? const Size(130, 44) : null,
        minimumSize: Size(desktop ? 130 : 0, 44),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: const Text('Cancelar'),
    );
    final save = ElevatedButton(
      onPressed: _saving ? null : _submit,
      style: ElevatedButton.styleFrom(
        fixedSize: desktop ? const Size(154, 44) : null,
        minimumSize: Size(desktop ? 154 : 0, 44),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: Text(_saving ? 'Salvando...' : saveLabel),
    );
    return Container(
      height: 70,
      padding: EdgeInsets.symmetric(
        horizontal: desktop ? 26 : 16,
        vertical: 12,
      ),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: t.line)),
      ),
      child: desktop
          ? Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [cancel, const SizedBox(width: 12), save],
            )
          : Row(
              children: [
                Expanded(child: cancel),
                const SizedBox(width: 10),
                Expanded(child: save),
              ],
            ),
    );
  }
}

class _DialogFrame extends StatelessWidget {
  const _DialogFrame({
    super.key,
    required this.width,
    required this.height,
    required this.child,
  });

  final double width;
  final double height;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      width: width,
      height: height,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Colors.white,
          border: Border.all(color: t.line),
          borderRadius: BorderRadius.circular(18),
          boxShadow: const [
            BoxShadow(
              color: Color(0x29000000),
              blurRadius: 28,
              offset: Offset(0, 12),
            ),
          ],
        ),
        child: ClipRRect(borderRadius: BorderRadius.circular(17), child: child),
      ),
    );
  }
}

class _DialogCloseButton extends StatelessWidget {
  const _DialogCloseButton({this.onPressed});

  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox.square(
      dimension: 40,
      child: OutlinedButton(
        onPressed: onPressed,
        style: OutlinedButton.styleFrom(
          foregroundColor: t.muted,
          backgroundColor: Colors.white,
          padding: EdgeInsets.zero,
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
        child: const Icon(Icons.close_rounded, size: 18),
      ),
    );
  }
}

class _LabeledDialogField extends StatelessWidget {
  const _LabeledDialogField({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          label,
          style: TextStyle(
            color: t.muted,
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

class _PreferredTimeButton extends StatelessWidget {
  const _PreferredTimeButton({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Semantics(
      button: true,
      selected: selected,
      child: SizedBox(
        height: 40,
        child: OutlinedButton.icon(
          onPressed: onPressed,
          style: OutlinedButton.styleFrom(
            foregroundColor: selected ? t.accentDark : t.ink,
            backgroundColor: selected ? t.accentSoft : Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 9),
            side: BorderSide(
              color: selected ? t.accent : t.line,
              width: selected ? 2 : 1,
            ),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
            ),
            textStyle: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
          icon: Icon(icon, size: 16),
          label: Text(label),
        ),
      ),
    );
  }
}

InputDecoration _dialogInputDecoration(
  BuildContext context, {
  required String hintText,
  EdgeInsetsGeometry? contentPadding,
  Widget? suffixIcon,
}) {
  final t = AgendaThemeTokens.of(context);
  final borderRadius = BorderRadius.circular(16);
  return InputDecoration(
    hintText: hintText,
    hintStyle: TextStyle(color: t.muted, fontSize: 13),
    filled: true,
    fillColor: Colors.white,
    isDense: true,
    contentPadding:
        contentPadding ??
        const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    suffixIcon: suffixIcon,
    suffixIconConstraints: suffixIcon == null
        ? null
        : const BoxConstraints.tightFor(width: 40, height: 40),
    border: OutlineInputBorder(
      borderRadius: borderRadius,
      borderSide: BorderSide(color: t.line),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: borderRadius,
      borderSide: BorderSide(color: t.line),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: borderRadius,
      borderSide: BorderSide(color: t.accent, width: 1.5),
    ),
  );
}

List<String> _customerTagOptions(List<Customer> customers) {
  const defaults = <String>[
    'VIP',
    'Recorrente',
    'Primeira visita',
    'Retorno',
    'Pós-venda',
    'Preferencial',
    'Aniversariante',
    'Fiado',
    'Atrasado',
    'Não chamar',
  ];
  final byNormalizedTag = <String, String>{};
  for (final customer in customers) {
    for (final rawTag in customer.tags.split(RegExp(r'[,;|]'))) {
      final tag = rawTag.trim();
      if (tag.isNotEmpty) {
        byNormalizedTag.putIfAbsent(tag.toLowerCase(), () => tag);
      }
    }
  }
  for (final tag in defaults) {
    byNormalizedTag.putIfAbsent(tag.toLowerCase(), () => tag);
  }
  return byNormalizedTag.values.toList()
    ..sort((a, b) => a.toLowerCase().compareTo(b.toLowerCase()));
}

String _firstFilled(Iterable<String> values, {required String fallback}) {
  for (final value in values) {
    final normalized = value.trim();
    if (normalized.isNotEmpty) return normalized;
  }
  return fallback;
}

String _managerInitials(String name) {
  final parts = name.trim().split(RegExp(r'\s+'));
  final initials = parts
      .where((part) => part.isNotEmpty)
      .take(2)
      .map((part) => String.fromCharCode(part.runes.first))
      .join()
      .toUpperCase();
  return initials.isEmpty ? '?' : initials;
}

String _normalizeCustomerPreferredTime(String value) {
  final normalized = value.trim().replaceFirst(RegExp(r'\.+$'), '');
  final lookup = normalized.toLowerCase();
  if (lookup.startsWith('manh')) return 'Manhã';
  if (lookup.startsWith('tarde')) return 'Tarde';
  if (lookup.startsWith('noite')) return 'Noite';
  return '';
}

String _readCustomerPreferredTime(String profile) {
  const prefix = 'Preferência de horário:';
  for (final rawLine in profile.replaceAll('\r\n', '\n').split('\n')) {
    final line = rawLine.trim();
    if (line.toLowerCase().startsWith(prefix.toLowerCase())) {
      return _normalizeCustomerPreferredTime(line.substring(prefix.length));
    }
  }
  return '';
}

String _removeCustomerPreferredTime(String profile) {
  const prefix = 'Preferência de horário:';
  return profile
      .replaceAll('\r\n', '\n')
      .split('\n')
      .where(
        (line) => !line.trim().toLowerCase().startsWith(prefix.toLowerCase()),
      )
      .join('\n')
      .trim();
}

String _buildCustomerProfile(String preferredTime, String observations) {
  final parts = <String>[];
  final normalizedTime = _normalizeCustomerPreferredTime(preferredTime);
  if (normalizedTime.isNotEmpty) {
    parts.add('Preferência de horário: $normalizedTime.');
  }
  final normalizedObservations = observations.trim();
  if (normalizedObservations.isNotEmpty) parts.add(normalizedObservations);
  return parts.join('\n');
}

class _ProfessionalEditorDialog extends StatefulWidget {
  const _ProfessionalEditorDialog({
    required this.controller,
    this.professional,
  });

  final AgendaController controller;
  final Professional? professional;

  @override
  State<_ProfessionalEditorDialog> createState() =>
      _ProfessionalEditorDialogState();
}

class _ProfessionalEditorDialogState extends State<_ProfessionalEditorDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _role;
  late final TextEditingController _segments;
  late final TextEditingController _phone;
  late final TextEditingController _email;
  late final TextEditingController _document;
  late final TextEditingController _commission;
  late final TextEditingController _notes;
  late final List<TextEditingController> _previewControllers;
  late bool _active;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final professional = widget.professional;
    _name = TextEditingController(text: professional?.name ?? '');
    _segments = TextEditingController(
      text:
          professional?.segments.join(', ') ??
          widget.controller.data.settings.businessSegment,
    );
    _role = TextEditingController(
      text: _firstFilled([
        professional?.role ?? '',
        _defaultRoleForSegment(_segments.text),
      ], fallback: 'Profissional'),
    );
    _phone = TextEditingController(text: professional?.phone ?? '');
    _email = TextEditingController(text: professional?.email ?? '');
    _document = TextEditingController(text: professional?.document ?? '');
    _commission = TextEditingController(
      text: _decimalText(professional?.commissionPercent ?? 0),
    );
    _notes = TextEditingController(text: professional?.notes ?? '');
    _active = professional?.isActive ?? true;
    _previewControllers = [_name, _role, _phone, _commission];
    for (final controller in _previewControllers) {
      controller.addListener(_refreshPreview);
    }
  }

  @override
  void dispose() {
    for (final controller in _previewControllers) {
      controller.removeListener(_refreshPreview);
    }
    _name.dispose();
    _role.dispose();
    _segments.dispose();
    _phone.dispose();
    _email.dispose();
    _document.dispose();
    _commission.dispose();
    _notes.dispose();
    super.dispose();
  }

  void _refreshPreview() {
    if (mounted) setState(() {});
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final existing = widget.professional;
    final professional = Professional(
      id: existing?.id,
      name: _name.text.trim(),
      role: _role.text.trim(),
      segments: _segments.text
          .split(',')
          .map((item) => item.trim())
          .where((item) => item.isNotEmpty)
          .toSet()
          .toList(),
      phone: _phone.text.trim(),
      email: _email.text.trim(),
      document: _document.text.trim(),
      commissionPercent: _parseDecimal(_commission.text),
      notes: _notes.text.trim(),
      isActive: _active,
    );
    await widget.controller.saveProfessional(professional);
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    return _EditorDialogShell(
      frameKey: const ValueKey('professional-editor-dialog-frame'),
      desktopWidth: 860,
      desktopHeight: 612,
      title: widget.professional == null
          ? 'Criar profissional'
          : 'Editar profissional',
      subtitle: 'Cadastre quem atende e em qual agenda ele aparece.',
      saveLabel: widget.professional == null
          ? 'Salvar profissional'
          : 'Salvar alterações',
      saving: _saving,
      onSave: _submit,
      formFlex: 64,
      previewFlex: 36,
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const _EditorSectionHeading(
              icon: Icons.badge_outlined,
              title: 'Identificação',
              subtitle: 'Dados usados na agenda e no cadastro da equipe.',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Nome do profissional',
                child: _compactEditorTextField(
                  context,
                  controller: _name,
                  autofocus: true,
                  hintText: 'Ex: Lucas',
                  validator: _required,
                ),
              ),
              _CompactLabeledField(
                label: 'Função',
                child: _compactEditorTextField(
                  context,
                  controller: _role,
                  hintText: 'Ex: Barbeiro, mecânico, dentista',
                ),
              ),
            ),
            const SizedBox(height: 9),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Telefone / WhatsApp',
                child: _compactEditorTextField(
                  context,
                  controller: _phone,
                  hintText: 'Ex: (27) 99999-0000',
                  keyboardType: TextInputType.phone,
                ),
              ),
              _CompactLabeledField(
                label: 'E-mail',
                child: _compactEditorTextField(
                  context,
                  controller: _email,
                  hintText: 'Ex: profissional@email.com',
                  keyboardType: TextInputType.emailAddress,
                ),
              ),
            ),
            const SizedBox(height: 9),
            const _EditorSectionHeading(
              icon: Icons.payments_outlined,
              title: 'Agenda e financeiro',
              subtitle: 'Segmento atendido, documento e comissão padrão.',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Segmento atendido',
                child: _compactLockedField(context, value: _segments.text),
              ),
              _CompactLabeledField(
                label: 'CPF / documento',
                child: _compactEditorTextField(
                  context,
                  controller: _document,
                  hintText: 'Ex: 123.456.789-00',
                ),
              ),
            ),
            const SizedBox(height: 9),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Comissão padrão (%)',
                child: _compactEditorTextField(
                  context,
                  controller: _commission,
                  hintText: 'Ex: 40',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                ),
              ),
              _ActiveEditorCheckbox(
                label: 'Profissional ativo na agenda',
                value: _active,
                onChanged: (value) => setState(() => _active = value),
              ),
            ),
            const SizedBox(height: 9),
            _CompactLabeledField(
              label: 'Observações internas',
              child: _compactEditorTextField(
                context,
                controller: _notes,
                hintText: 'Ex: folgas, especialidades, restrições de horário',
                height: 44,
                minLines: 2,
                maxLines: 2,
              ),
            ),
          ],
        ),
      ),
      preview: _professionalPreview(context),
    );
  }

  Widget _professionalPreview(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final name = _firstFilled([_name.text], fallback: 'Novo profissional');
    final role = _firstFilled([_role.text], fallback: 'Profissional');
    final segment = _firstFilled([_segments.text], fallback: 'Agenda');
    final contact = _firstFilled([
      _phone.text,
    ], fallback: 'WhatsApp não informado');
    final commission = math.max(0, _parseDecimal(_commission.text));
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Perfil do profissional',
          style: TextStyle(
            color: t.muted,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            CircleAvatar(
              radius: 22,
              backgroundColor: t.accentSoft,
              foregroundColor: t.accent,
              child: Text(
                _previewInitials(name, fallback: 'NP'),
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    name,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 18,
                      height: 1.05,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    role,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 11.5),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        _EditorPreviewRow(
          icon: Icons.chat_bubble_outline_rounded,
          label: 'Contato',
          value: contact,
        ),
        _EditorPreviewRow(
          icon: Icons.event_note_outlined,
          label: 'Agenda',
          value: segment,
        ),
        _EditorPreviewRow(
          icon: Icons.percent_rounded,
          label: 'Comissão',
          value: '${commission.toStringAsFixed(0)}%',
        ),
        _EditorPreviewRow(
          icon: Icons.check_circle_outline_rounded,
          label: 'Status',
          value: _active ? 'Ativo' : 'Inativo',
          valueColor: _active ? const Color(0xFF15803D) : t.muted,
        ),
        const SizedBox(height: 12),
        _EditorAgendaPreviewCard(text: '$name  •  $role  •  $segment'),
      ],
    );
  }
}

class _ServiceEditorDialog extends StatefulWidget {
  const _ServiceEditorDialog({required this.controller, this.service});

  final AgendaController controller;
  final ServiceItem? service;

  @override
  State<_ServiceEditorDialog> createState() => _ServiceEditorDialogState();
}

class _ServiceEditorDialogState extends State<_ServiceEditorDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _category;
  late final TextEditingController _segment;
  late final TextEditingController _description;
  late final TextEditingController _duration;
  late final TextEditingController _preparation;
  late final TextEditingController _buffer;
  late final TextEditingController _price;
  late final TextEditingController _commission;
  late final TextEditingController _resource;
  late final List<TextEditingController> _previewControllers;
  late bool _active;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final service = widget.service;
    _name = TextEditingController(text: service?.name ?? '');
    _segment = TextEditingController(
      text: service?.segment.trim().isNotEmpty == true
          ? service!.segment
          : widget.controller.data.settings.businessSegment,
    );
    _category = TextEditingController(
      text: service?.category.trim().isNotEmpty == true
          ? service!.category
          : _defaultServiceCategory(
              widget.controller.data.services,
              _segment.text,
            ),
    );
    _description = TextEditingController(text: service?.description ?? '');
    _duration = TextEditingController(
      text: (service?.durationMinutes ?? 30).toString(),
    );
    _preparation = TextEditingController(
      text: (service?.preparationMinutes ?? 0).toString(),
    );
    _buffer = TextEditingController(
      text: (service?.bufferMinutes ?? 0).toString(),
    );
    _price = TextEditingController(text: _decimalText(service?.price ?? 0));
    _commission = TextEditingController(
      text: _decimalText(service?.commissionPercent ?? 0),
    );
    _resource = TextEditingController(
      text: service?.defaultResource.trim().isNotEmpty == true
          ? service!.defaultResource
          : widget.controller.data.settings.resources.isEmpty
          ? ''
          : widget.controller.data.settings.resources.first,
    );
    _active = service?.isActive ?? true;
    _previewControllers = [
      _name,
      _category,
      _segment,
      _duration,
      _price,
      _commission,
      _resource,
    ];
    for (final controller in _previewControllers) {
      controller.addListener(_refreshPreview);
    }
  }

  @override
  void dispose() {
    for (final controller in _previewControllers) {
      controller.removeListener(_refreshPreview);
    }
    _name.dispose();
    _category.dispose();
    _segment.dispose();
    _description.dispose();
    _duration.dispose();
    _preparation.dispose();
    _buffer.dispose();
    _price.dispose();
    _commission.dispose();
    _resource.dispose();
    super.dispose();
  }

  void _refreshPreview() {
    if (mounted) setState(() {});
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final existing = widget.service;
    final service = ServiceItem(
      id: existing?.id,
      name: _name.text.trim(),
      category: _category.text.trim(),
      segment: _segment.text.trim(),
      description: _description.text.trim(),
      durationMinutes: int.tryParse(_duration.text) ?? 30,
      preparationMinutes: int.tryParse(_preparation.text) ?? 0,
      bufferMinutes: int.tryParse(_buffer.text) ?? 0,
      price: _parseDecimal(_price.text),
      commissionPercent: _parseDecimal(_commission.text),
      defaultResource: _resource.text.trim(),
      isActive: _active,
    );
    await widget.controller.saveService(service);
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final segmentOptions = _uniqueOptions([
      widget.controller.data.settings.businessSegment,
      for (final service in widget.controller.data.services) service.segment,
    ]);
    final categoryOptions = _uniqueOptions([
      for (final service in widget.controller.data.services) service.category,
      'Corte',
      'Coloração',
      'Tratamento',
      'Unhas',
      'Outros',
    ]);
    final resourceOptions = _uniqueOptions([
      ...widget.controller.data.settings.resources,
      for (final service in widget.controller.data.services)
        service.defaultResource,
    ]);
    return _EditorDialogShell(
      frameKey: const ValueKey('service-editor-dialog-frame'),
      desktopWidth: 840,
      desktopHeight: 630,
      title: widget.service == null ? 'Criar serviço' : 'Editar serviço',
      subtitle: 'Defina como o serviço aparece na agenda e no atendimento.',
      saveLabel: widget.service == null
          ? 'Salvar serviço'
          : 'Salvar alterações',
      saving: _saving,
      onSave: _submit,
      formFlex: 65,
      previewFlex: 35,
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const _EditorSectionHeading(
              icon: Icons.assignment_outlined,
              title: 'Catálogo',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Tipo de atendimento',
                child: _CompactMenuField(
                  controller: _segment,
                  options: segmentOptions,
                  hintText: 'Selecione o atendimento',
                  readOnly: true,
                ),
              ),
              _CompactLabeledField(
                label: 'Categoria',
                child: _CompactMenuField(
                  controller: _category,
                  options: categoryOptions,
                  hintText: 'Ex: Corte',
                ),
              ),
            ),
            const SizedBox(height: 8),
            _CompactLabeledField(
              label: 'Nome do serviço',
              child: _compactEditorTextField(
                context,
                controller: _name,
                autofocus: true,
                hintText: 'Ex: Corte masculino, consulta, revisão',
                validator: _required,
              ),
            ),
            const SizedBox(height: 8),
            _CompactLabeledField(
              label: 'Descrição para a equipe',
              child: _compactEditorTextField(
                context,
                controller: _description,
                hintText: 'Ex: inclui lavagem, avaliação inicial ou checklist',
                height: 42,
                minLines: 2,
                maxLines: 2,
              ),
            ),
            const SizedBox(height: 8),
            const _EditorSectionHeading(
              icon: Icons.schedule_outlined,
              title: 'Tempo e agenda',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Duração em minutos',
                child: _compactEditorTextField(
                  context,
                  controller: _duration,
                  hintText: 'Ex: 30',
                  keyboardType: TextInputType.number,
                  validator: _positiveInteger,
                ),
              ),
              _CompactLabeledField(
                label: 'Preparação antes (min)',
                child: _compactEditorTextField(
                  context,
                  controller: _preparation,
                  hintText: 'Ex: 5',
                  keyboardType: TextInputType.number,
                ),
              ),
            ),
            const SizedBox(height: 8),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Intervalo após (min)',
                child: _compactEditorTextField(
                  context,
                  controller: _buffer,
                  hintText: 'Ex: 10',
                  keyboardType: TextInputType.number,
                ),
              ),
              _CompactLabeledField(
                label: 'Sala, cadeira ou recurso padrão',
                child: _CompactMenuField(
                  controller: _resource,
                  options: resourceOptions,
                  hintText: 'Mesa, sala ou cadeira',
                ),
              ),
            ),
            const SizedBox(height: 8),
            const _EditorSectionHeading(
              icon: Icons.payments_outlined,
              title: 'Preço e equipe',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Valor de venda',
                child: _compactEditorTextField(
                  context,
                  controller: _price,
                  hintText: 'Ex: 45,00',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                ),
              ),
              _CompactLabeledField(
                label: 'Comissão (%)',
                child: _compactEditorTextField(
                  context,
                  controller: _commission,
                  hintText: 'Ex: 40',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 8),
            _ActiveEditorCheckbox(
              label: 'Serviço ativo para novos agendamentos',
              value: _active,
              onChanged: (value) => setState(() => _active = value),
            ),
          ],
        ),
      ),
      preview: _servicePreview(context),
    );
  }

  Widget _servicePreview(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final name = _firstFilled([_name.text], fallback: 'Novo serviço');
    final category = _firstFilled([_category.text], fallback: 'Sem categoria');
    final segment = _firstFilled([_segment.text], fallback: 'Agenda');
    final duration = math.max(0, int.tryParse(_duration.text.trim()) ?? 0);
    final price = math.max(0, _parseDecimal(_price.text)).toDouble();
    final commission = math.max(0, _parseDecimal(_commission.text)).toDouble();
    final resource = _firstFilled([_resource.text], fallback: 'Não definido');
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Prévia do serviço',
          style: TextStyle(
            color: t.muted,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          name,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: t.ink,
            fontSize: 18,
            height: 1.05,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          _active ? 'Ativo' : 'Inativo',
          style: TextStyle(
            color: _active ? const Color(0xFF15803D) : t.muted,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 12),
        Divider(height: 1, color: t.line),
        _EditorPreviewRow(
          icon: Icons.sell_outlined,
          label: 'Categoria e atendimento',
          value: '$category • $segment',
        ),
        _EditorPreviewRow(
          icon: Icons.schedule_outlined,
          label: 'Duração',
          value: '$duration min',
        ),
        _EditorPreviewRow(
          icon: Icons.payments_outlined,
          label: 'Valor e comissão',
          value:
              '${_formatCurrency(price)} • ${commission.toStringAsFixed(0)}% comissão',
          valueColor: t.accentDark,
        ),
        _EditorPreviewRow(
          icon: Icons.event_seat_outlined,
          label: 'Recurso padrão',
          value: resource,
        ),
        const SizedBox(height: 12),
        _EditorAgendaPreviewCard(
          text: '$name  •  $duration min  •  ${_formatCurrency(price)}',
        ),
      ],
    );
  }
}

class _ProductEditorDialog extends StatefulWidget {
  const _ProductEditorDialog({required this.controller, this.product});

  final AgendaController controller;
  final ProductItem? product;

  @override
  State<_ProductEditorDialog> createState() => _ProductEditorDialogState();
}

class _ProductEditorDialogState extends State<_ProductEditorDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _category;
  late final TextEditingController _sku;
  late final TextEditingController _supplier;
  late final TextEditingController _cost;
  late final TextEditingController _price;
  late final TextEditingController _stock;
  late final TextEditingController _minimumStock;
  late final TextEditingController _notes;
  late final List<TextEditingController> _previewControllers;
  late bool _active;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    final product = widget.product;
    _name = TextEditingController(text: product?.name ?? '');
    _category = TextEditingController(text: product?.category ?? '');
    _sku = TextEditingController(text: product?.sku ?? '');
    _supplier = TextEditingController(text: product?.supplier ?? '');
    _cost = TextEditingController(text: _decimalText(product?.costPrice ?? 0));
    _price = TextEditingController(text: _decimalText(product?.price ?? 0));
    _stock = TextEditingController(
      text: (product?.stockQuantity ?? 0).toString(),
    );
    _minimumStock = TextEditingController(
      text: (product?.minimumStock ?? 0).toString(),
    );
    _notes = TextEditingController(text: product?.notes ?? '');
    _active = product?.isActive ?? true;
    _previewControllers = <TextEditingController>[
      _name,
      _category,
      _sku,
      _supplier,
      _cost,
      _price,
      _stock,
      _minimumStock,
      _notes,
    ];
    for (final controller in _previewControllers) {
      controller.addListener(_refreshPreview);
    }
  }

  @override
  void dispose() {
    for (final controller in _previewControllers) {
      controller
        ..removeListener(_refreshPreview)
        ..dispose();
    }
    super.dispose();
  }

  void _refreshPreview() {
    if (mounted) setState(() {});
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    final existing = widget.product;
    try {
      final error = await widget.controller.saveProduct(
        ProductItem(
          id: existing?.id,
          name: _name.text.trim(),
          category: _category.text.trim(),
          sku: _sku.text.trim(),
          supplier: _supplier.text.trim(),
          costPrice: math.max(0, _parseDecimal(_cost.text)).toDouble(),
          price: math.max(0, _parseDecimal(_price.text)).toDouble(),
          stockQuantity: math.max(0, int.tryParse(_stock.text.trim()) ?? 0),
          minimumStock: math.max(
            0,
            int.tryParse(_minimumStock.text.trim()) ?? 0,
          ),
          notes: _notes.text.trim(),
          isActive: _active,
          createdAt: existing?.createdAt,
        ),
      );
      if (error != null) throw FormatException(error);
      if (mounted) Navigator.of(context).pop(true);
    } on Object catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = error is FormatException
            ? error.message
            : 'Não foi possível salvar o produto: $error';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final categoryOptions = _uniqueOptions([
      for (final product in widget.controller.data.products) product.category,
      'Cabelo',
      'Estética',
      'Higiene',
      'Cuidados',
      'Acessórios',
      'Outros',
    ]);
    return _EditorDialogShell(
      frameKey: const ValueKey('product-editor-dialog-frame'),
      desktopWidth: 880,
      desktopHeight: 650,
      title: widget.product == null ? 'Criar produto' : 'Editar produto',
      subtitle:
          'Cadastre preço, estoque e identificação para vender na agenda.',
      saveLabel: widget.product == null
          ? 'Salvar produto'
          : 'Salvar alterações',
      saving: _saving,
      onSave: _submit,
      formFlex: 64,
      previewFlex: 36,
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const _EditorSectionHeading(
              icon: Icons.inventory_2_outlined,
              title: 'Produto',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Nome do produto',
                child: _compactEditorTextField(
                  context,
                  controller: _name,
                  autofocus: true,
                  hintText: 'Ex: Shampoo profissional',
                  key: const Key('product-name-field'),
                  validator: _required,
                ),
              ),
              _CompactLabeledField(
                label: 'Categoria',
                child: _CompactMenuField(
                  controller: _category,
                  options: categoryOptions,
                  hintText: 'Ex: Cuidados',
                ),
              ),
            ),
            const SizedBox(height: 8),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'SKU / código',
                child: _compactEditorTextField(
                  context,
                  controller: _sku,
                  hintText: 'Ex: SHP-001',
                ),
              ),
              _CompactLabeledField(
                label: 'Fornecedor',
                child: _compactEditorTextField(
                  context,
                  controller: _supplier,
                  hintText: 'Ex: Distribuidora local',
                ),
              ),
            ),
            const SizedBox(height: 8),
            const _EditorSectionHeading(
              icon: Icons.payments_outlined,
              title: 'Preço',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Preço de custo',
                child: _compactEditorTextField(
                  context,
                  controller: _cost,
                  hintText: 'Ex: 25,00',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                ),
              ),
              _CompactLabeledField(
                label: 'Preço de venda',
                child: _compactEditorTextField(
                  context,
                  controller: _price,
                  hintText: 'Ex: 49,90',
                  key: const Key('product-price-field'),
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  validator: (value) => _parseDecimal(value ?? '') <= 0
                      ? 'Informe o preço de venda.'
                      : null,
                ),
              ),
            ),
            const SizedBox(height: 8),
            const _EditorSectionHeading(
              icon: Icons.warehouse_outlined,
              title: 'Estoque',
            ),
            _editorFieldRow(
              _CompactLabeledField(
                label: 'Quantidade atual',
                child: _compactEditorTextField(
                  context,
                  key: const Key('product-stock-field'),
                  controller: _stock,
                  hintText: 'Ex: 10',
                  keyboardType: TextInputType.number,
                ),
              ),
              _CompactLabeledField(
                label: 'Estoque mínimo',
                child: _compactEditorTextField(
                  context,
                  controller: _minimumStock,
                  hintText: 'Ex: 2',
                  keyboardType: TextInputType.number,
                ),
              ),
            ),
            const SizedBox(height: 8),
            _CompactLabeledField(
              label: 'Observações internas',
              child: _compactEditorTextField(
                context,
                controller: _notes,
                hintText: 'Ex: lote, validade, reposição ou localização',
                height: 44,
                minLines: 2,
                maxLines: 2,
              ),
            ),
            const SizedBox(height: 8),
            _ActiveEditorCheckbox(
              label: 'Produto ativo para novas vendas',
              value: _active,
              onChanged: (value) => setState(() => _active = value),
            ),
            if (_error != null) ...[
              const SizedBox(height: 8),
              Text(
                _error!,
                style: const TextStyle(color: Color(0xFFB91C1C), fontSize: 12),
              ),
            ],
          ],
        ),
      ),
      preview: _productPreview(context),
    );
  }

  Widget _productPreview(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final name = _firstFilled([_name.text], fallback: 'Novo produto');
    final category = _firstFilled([_category.text], fallback: 'Sem categoria');
    final sku = _firstFilled([_sku.text], fallback: 'Sem código');
    final supplier = _firstFilled([_supplier.text], fallback: 'Não informado');
    final price = math.max(0, _parseDecimal(_price.text)).toDouble();
    final cost = math.max(0, _parseDecimal(_cost.text)).toDouble();
    final stock = math.max(0, int.tryParse(_stock.text.trim()) ?? 0);
    final minimum = math.max(0, int.tryParse(_minimumStock.text.trim()) ?? 0);
    final lowStock = stock <= minimum;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Prévia do produto',
          style: TextStyle(
            color: t.muted,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          name,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: t.ink,
            fontSize: 18,
            height: 1.05,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          _active ? 'Ativo' : 'Inativo',
          style: TextStyle(
            color: _active ? const Color(0xFF15803D) : t.muted,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 12),
        Divider(height: 1, color: t.line),
        _EditorPreviewRow(
          icon: Icons.sell_outlined,
          label: 'Categoria e código',
          value: '$category • $sku',
        ),
        _EditorPreviewRow(
          icon: Icons.payments_outlined,
          label: 'Venda e custo',
          value: '${_formatCurrency(price)} • ${_formatCurrency(cost)} custo',
          valueColor: t.accentDark,
        ),
        _EditorPreviewRow(
          icon: Icons.inventory_outlined,
          label: 'Estoque',
          value: '$stock unidades • mínimo $minimum',
          valueColor: lowStock ? const Color(0xFFB45309) : null,
        ),
        _EditorPreviewRow(
          icon: Icons.local_shipping_outlined,
          label: 'Fornecedor',
          value: supplier,
        ),
        const SizedBox(height: 12),
        _EditorAgendaPreviewCard(
          text: '$name  •  $stock un.  •  ${_formatCurrency(price)}',
        ),
      ],
    );
  }
}

class _EditorDialogShell extends StatelessWidget {
  const _EditorDialogShell({
    required this.frameKey,
    required this.desktopWidth,
    required this.desktopHeight,
    required this.title,
    required this.subtitle,
    required this.saveLabel,
    required this.form,
    required this.preview,
    required this.saving,
    required this.onSave,
    required this.formFlex,
    required this.previewFlex,
  });

  final Key frameKey;
  final double desktopWidth;
  final double desktopHeight;
  final String title;
  final String subtitle;
  final String saveLabel;
  final Widget form;
  final Widget preview;
  final bool saving;
  final VoidCallback onSave;
  final int formFlex;
  final int previewFlex;

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    final desktop = size.width >= desktopWidth + 32;
    final frameWidth = math.max(0.0, math.min(desktopWidth, size.width - 32));
    final frameHeight = math.max(
      0.0,
      math.min(desktop ? desktopHeight : 760.0, size.height - 32),
    );
    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(16),
      child: _DialogFrame(
        key: frameKey,
        width: frameWidth,
        height: frameHeight,
        child: Column(
          children: [
            _header(context, desktop: desktop),
            Expanded(child: _body(context, desktop: desktop)),
            _footer(context, desktop: desktop),
          ],
        ),
      ),
    );
  }

  Widget _header(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      height: desktop ? 88 : 110,
      padding: EdgeInsets.fromLTRB(22, desktop ? 17 : 13, 22, 13),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: desktop ? 22 : 19,
                    height: 1.08,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  subtitle,
                  maxLines: desktop ? 1 : 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 12.5),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          _DialogCloseButton(
            onPressed: saving ? null : () => Navigator.of(context).pop(false),
          ),
        ],
      ),
    );
  }

  Widget _body(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    if (!desktop) {
      return ColoredBox(
        color: Colors.white,
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 22),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              form,
              const SizedBox(height: 20),
              Divider(height: 1, color: t.line),
              const SizedBox(height: 18),
              DecoratedBox(
                decoration: BoxDecoration(
                  color: const Color(0xFFFFFCFA),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: t.line),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: preview,
                ),
              ),
            ],
          ),
        ),
      );
    }
    return ColoredBox(
      color: Colors.white,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            flex: formFlex,
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(26, 16, 18, 16),
              child: form,
            ),
          ),
          VerticalDivider(width: 1, thickness: 1, color: t.line),
          Expanded(
            flex: previewFlex,
            child: ColoredBox(
              color: const Color(0xFFFFFCFA),
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(18, 16, 18, 16),
                child: preview,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _footer(BuildContext context, {required bool desktop}) {
    final t = AgendaThemeTokens.of(context);
    final cancel = OutlinedButton(
      onPressed: saving ? null : () => Navigator.of(context).pop(false),
      style: OutlinedButton.styleFrom(minimumSize: Size(desktop ? 110 : 0, 40)),
      child: const Text('Cancelar'),
    );
    final save = ElevatedButton(
      onPressed: saving ? null : onSave,
      style: ElevatedButton.styleFrom(minimumSize: Size(desktop ? 168 : 0, 40)),
      child: saving
          ? const SizedBox.square(
              dimension: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : Text(saveLabel),
    );
    return Container(
      height: 70,
      padding: EdgeInsets.symmetric(
        horizontal: desktop ? 22 : 16,
        vertical: 12,
      ),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: t.line)),
      ),
      child: desktop
          ? Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [cancel, const SizedBox(width: 10), save],
            )
          : Row(
              children: [
                Expanded(child: cancel),
                const SizedBox(width: 10),
                Expanded(child: save),
              ],
            ),
    );
  }
}

class _EditorSectionHeading extends StatelessWidget {
  const _EditorSectionHeading({
    required this.icon,
    required this.title,
    this.subtitle,
  });

  final IconData icon;
  final String title;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(0, 2, 0, 7),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Container(
            width: 28,
            height: 28,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(14),
            ),
            alignment: Alignment.center,
            child: Icon(icon, color: t.accent, size: 14),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    height: 1.1,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    subtitle!,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 10.5),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CompactLabeledField extends StatelessWidget {
  const _CompactLabeledField({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: t.muted,
            fontSize: 11.5,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 3),
        child,
      ],
    );
  }
}

class _CompactMenuField extends StatelessWidget {
  const _CompactMenuField({
    required this.controller,
    required this.options,
    required this.hintText,
    this.readOnly = false,
  });

  final TextEditingController controller;
  final List<String> options;
  final String hintText;
  final bool readOnly;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return MenuAnchor(
      menuChildren: [
        for (final option in options)
          MenuItemButton(
            onPressed: () {
              controller.text = option;
              controller.selection = TextSelection.collapsed(
                offset: option.length,
              );
            },
            child: Text(option),
          ),
      ],
      builder: (context, menuController, _) {
        return SizedBox(
          height: 32,
          child: TextField(
            controller: controller,
            readOnly: readOnly,
            onTap: readOnly && options.isNotEmpty ? menuController.open : null,
            style: TextStyle(color: t.ink, fontSize: 12),
            textAlignVertical: TextAlignVertical.center,
            decoration: _compactEditorDecoration(
              context,
              hintText: hintText,
              suffixIcon: IconButton(
                tooltip: 'Mostrar opções',
                onPressed: options.isEmpty
                    ? null
                    : () => menuController.isOpen
                          ? menuController.close()
                          : menuController.open(),
                padding: EdgeInsets.zero,
                icon: const Icon(Icons.arrow_drop_down_rounded, size: 19),
              ),
            ),
          ),
        );
      },
    );
  }
}

class _ActiveEditorCheckbox extends StatelessWidget {
  const _ActiveEditorCheckbox({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.only(top: 17),
      child: InkWell(
        onTap: () => onChanged(!value),
        borderRadius: BorderRadius.circular(8),
        child: Row(
          children: [
            SizedBox.square(
              dimension: 22,
              child: Checkbox(
                value: value,
                onChanged: (next) => onChanged(next ?? value),
                visualDensity: VisualDensity.compact,
                materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
              ),
            ),
            const SizedBox(width: 7),
            Expanded(
              child: Text(
                label,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 11.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _EditorPreviewRow extends StatelessWidget {
  const _EditorPreviewRow({
    required this.icon,
    required this.label,
    required this.value,
    this.valueColor,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 6),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Container(
            width: 26,
            height: 26,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(13),
            ),
            alignment: Alignment.center,
            child: Icon(icon, color: t.accent, size: 14),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(color: t.muted, fontSize: 10.5)),
                const SizedBox(height: 1),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: valueColor ?? t.ink,
                    fontSize: 11.8,
                    fontWeight: FontWeight.w600,
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

class _EditorAgendaPreviewCard extends StatelessWidget {
  const _EditorAgendaPreviewCard({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: t.warmSoft,
        border: Border.all(color: t.accentSoft),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Como aparece na agenda',
            style: TextStyle(
              color: t.ink,
              fontSize: 11.5,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            text,
            style: TextStyle(color: t.ink, fontSize: 11.5, height: 1.45),
          ),
        ],
      ),
    );
  }
}

Widget _editorFieldRow(Widget first, Widget second) {
  return LayoutBuilder(
    builder: (context, constraints) {
      if (constraints.maxWidth < 440) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [first, const SizedBox(height: 9), second],
        );
      }
      return Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: first),
          const SizedBox(width: 16),
          Expanded(child: second),
        ],
      );
    },
  );
}

Widget _compactEditorTextField(
  BuildContext context, {
  Key? key,
  required TextEditingController controller,
  required String hintText,
  double height = 32,
  bool autofocus = false,
  int minLines = 1,
  int maxLines = 1,
  TextInputType? keyboardType,
  String? Function(String?)? validator,
}) {
  final t = AgendaThemeTokens.of(context);
  return SizedBox(
    height: height,
    child: TextFormField(
      key: key,
      controller: controller,
      autofocus: autofocus,
      minLines: minLines,
      maxLines: maxLines,
      keyboardType: keyboardType,
      validator: validator,
      style: TextStyle(color: t.ink, fontSize: 12),
      textAlignVertical: TextAlignVertical.center,
      decoration: _compactEditorDecoration(context, hintText: hintText),
    ),
  );
}

Widget _compactLockedField(BuildContext context, {required String value}) {
  final t = AgendaThemeTokens.of(context);
  return Container(
    height: 32,
    padding: const EdgeInsets.symmetric(horizontal: 10),
    decoration: BoxDecoration(
      color: t.graySoft,
      border: Border.all(color: t.line),
      borderRadius: BorderRadius.circular(12),
    ),
    child: Row(
      children: [
        Icon(Icons.lock_outline_rounded, size: 13, color: t.muted),
        const SizedBox(width: 7),
        Expanded(
          child: Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: t.ink, fontSize: 12),
          ),
        ),
      ],
    ),
  );
}

InputDecoration _compactEditorDecoration(
  BuildContext context, {
  required String hintText,
  Widget? suffixIcon,
}) {
  final t = AgendaThemeTokens.of(context);
  final radius = BorderRadius.circular(12);
  return InputDecoration(
    hintText: hintText,
    hintStyle: TextStyle(color: t.muted, fontSize: 11.5),
    filled: true,
    fillColor: Colors.white,
    isDense: true,
    contentPadding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
    suffixIcon: suffixIcon,
    suffixIconConstraints: suffixIcon == null
        ? null
        : const BoxConstraints.tightFor(width: 30, height: 30),
    errorStyle: const TextStyle(fontSize: 0, height: 0),
    border: OutlineInputBorder(
      borderRadius: radius,
      borderSide: BorderSide(color: t.line),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: BorderSide(color: t.line),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: BorderSide(color: t.accent, width: 1.5),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: const BorderSide(color: Color(0xFFEF4444), width: 1.5),
    ),
    focusedErrorBorder: OutlineInputBorder(
      borderRadius: radius,
      borderSide: const BorderSide(color: Color(0xFFEF4444), width: 1.5),
    ),
  );
}

List<String> _uniqueOptions(Iterable<String> values) {
  final options = <String>[];
  final seen = <String>{};
  for (final value in values) {
    final normalized = value.trim();
    if (normalized.isEmpty || !seen.add(normalized.toLowerCase())) continue;
    options.add(normalized);
  }
  return options;
}

String _formatCurrency(num value) =>
    'R\$ ${value.toDouble().toStringAsFixed(2).replaceAll('.', ',')}';

String _previewInitials(String name, {required String fallback}) {
  final initials = name
      .trim()
      .split(RegExp(r'\s+'))
      .where((part) => part.isNotEmpty)
      .take(2)
      .map((part) => String.fromCharCode(part.runes.first).toUpperCase())
      .join();
  return initials.isEmpty ? fallback : initials;
}

String _defaultRoleForSegment(String segment) => switch (segment.trim()) {
  'Barbearia' => 'Barbeiro',
  'Clínica médica' => 'Profissional de saúde',
  'Petshop' => 'Atendimento pet',
  'Oficina' || 'Mecânica' => 'Mecânico',
  'Unha e beleza' || 'Unha e beleza + salão' => 'Profissional de beleza',
  'Cabelo e barbearia' => 'Cabeleireiro',
  _ => 'Profissional',
};

String _defaultServiceCategory(List<ServiceItem> services, String segment) {
  for (final service in services) {
    if (service.segment.trim().toLowerCase() == segment.trim().toLowerCase() &&
        service.category.trim().isNotEmpty) {
      return service.category.trim();
    }
  }
  for (final service in services) {
    if (service.category.trim().isNotEmpty) return service.category.trim();
  }
  return '';
}

String? _required(String? value) =>
    value == null || value.trim().isEmpty ? 'Este campo é obrigatório.' : null;

String? _positiveInteger(String? value) {
  final number = int.tryParse(value ?? '');
  return number == null || number <= 0 ? 'Informe um tempo válido.' : null;
}

double _parseDecimal(String value) =>
    double.tryParse(value.trim().replaceAll(',', '.')) ?? 0;

String _decimalText(double value) => value == value.roundToDouble()
    ? value.toInt().toString()
    : value.toStringAsFixed(2).replaceAll('.', ',');
