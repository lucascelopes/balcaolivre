import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import '../../services/mercado_pago_service.dart';
import '../agenda/appointment_payment_dialog.dart';
import '../establishment/editor_dialogs.dart';
import '../payments/mercado_pago_settings_dialog.dart';
import 'finance_wpf_dashboard.dart';
import 'product_sale_dialog.dart';

const _positive = Color(0xFF16A34A);
const _positiveDark = Color(0xFF166534);
const _negative = Color(0xFFDC2626);
const _negativeDark = Color(0xFF991B1B);
const _warning = Color(0xFFD97706);
const _warningDark = Color(0xFFB45309);
const _neutral = Color(0xFF64748B);

const _paymentMethods = <String>[
  'Pix',
  'Dinheiro',
  'Cartão de débito',
  'Cartão de crédito',
  'Mercado Pago - débito na maquininha',
  'Mercado Pago - crédito na maquininha',
  'Cortesia',
  'Fiado',
];

class FinancePage extends StatefulWidget {
  const FinancePage({super.key, required this.controller});

  final AgendaController controller;

  @override
  State<FinancePage> createState() => _FinancePageState();
}

class _FinancePageState extends State<FinancePage> {
  int? _selectedChartIndex;

  Future<void> _registerPayment() async {
    final saved = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => _PaymentDialog(controller: widget.controller),
    );
    if (!mounted || saved != true) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Pagamento registrado com sucesso.')),
    );
  }

  Future<void> _registerExpense() async {
    final saved = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => _ExpenseDialog(controller: widget.controller),
    );
    if (!mounted || saved != true) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Despesa cadastrada com sucesso.')),
    );
  }

  Future<void> _receiveAppointment(Appointment appointment) async {
    await showAppointmentPaymentDialog(context, widget.controller, appointment);
  }

  Future<void> _configureMercadoPago() async {
    final saved = await showMercadoPagoSettingsDialog(
      context,
      widget.controller,
    );
    if (!mounted || !saved) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Mercado Pago atualizado.')));
  }

  Future<void> _sellProduct() async {
    if (!widget.controller.data.products.any((product) => product.isActive)) {
      final created = await showProductEditorDialog(
        context,
        controller: widget.controller,
      );
      if (!mounted || !created) return;
    }
    final saved = await showProductSaleDialog(
      context,
      controller: widget.controller,
    );
    if (!mounted || !saved) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Venda de produto registrada.')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        const legacyDashboard = bool.fromEnvironment(
          'AGENDA_FINANCE_LEGACY_DASHBOARD',
        );
        if (!legacyDashboard) {
          return FinanceWpfDashboard(
            controller: widget.controller,
            onReceive: _registerPayment,
            onExpense: _registerExpense,
            onProduct: _sellProduct,
          );
        }
        final snapshot = _FinanceSnapshot.from(widget.controller);
        final desktop = MediaQuery.sizeOf(context).width >= 1200;
        return ColoredBox(
          color: const Color(0xFFFAF9F7),
          child: LayoutBuilder(
            builder: (context, viewport) {
              final horizontal = desktop
                  ? 28.0
                  : viewport.maxWidth < 560
                  ? 14.0
                  : 20.0;
              return SingleChildScrollView(
                key: const Key('finance-page-scroll'),
                padding: EdgeInsets.fromLTRB(
                  desktop ? 28 : horizontal,
                  desktop ? 20 : 16,
                  desktop ? 36 : horizontal,
                  desktop ? 94 : 34,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _FinanceHero(
                      controller: widget.controller,
                      desktop: desktop,
                      onReceive: _registerPayment,
                      onExpense: _registerExpense,
                      onProduct: _sellProduct,
                    ),
                    const SizedBox(height: 14),
                    _FinanceMetricStrip(snapshot: snapshot, desktop: desktop),
                    const SizedBox(height: 14),
                    _FinanceChannelAttributionCard(
                      snapshot: snapshot,
                      desktop: desktop,
                    ),
                    const SizedBox(height: 14),
                    _buildPrimaryCards(snapshot, desktop),
                    const SizedBox(height: 10),
                    _buildLowerCards(snapshot, desktop),
                  ],
                ),
              );
            },
          ),
        );
      },
    );
  }

  Widget _buildPrimaryCards(_FinanceSnapshot snapshot, bool desktop) {
    final cards = <Widget>[
      _FinanceSourcesCard(
        serviceTotal: snapshot.serviceMonth,
        productTotal: snapshot.productMonth,
        manualTotal: snapshot.manualMonth,
        total: snapshot.receivedMonth,
        onDetails: () => widget.controller.navigate(AgendaPage.reports),
      ),
      _PendingCard(
        appointments: snapshot.pending,
        total: snapshot.pendingTotal,
        onReceive: _receiveAppointment,
      ),
      _ExpensesCard(
        expenses: snapshot.expenses,
        total: snapshot.expensesMonth,
        onAdd: _registerExpense,
      ),
    ];
    if (!desktop) {
      return Column(
        key: const Key('finance-primary-stack'),
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          for (var index = 0; index < cards.length; index++) ...[
            if (index > 0) const SizedBox(height: 10),
            cards[index],
          ],
        ],
      );
    }
    return LayoutBuilder(
      builder: (context, constraints) {
        final unit = constraints.maxWidth / 3.22;
        return IntrinsicHeight(
          child: Row(
            key: const Key('finance-primary-grid'),
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(width: unit * 1.22 - 10, child: cards[0]),
              const SizedBox(width: 10),
              SizedBox(width: unit - 10, child: cards[1]),
              const SizedBox(width: 10),
              Expanded(child: cards[2]),
            ],
          ),
        );
      },
    );
  }

  Widget _buildLowerCards(_FinanceSnapshot snapshot, bool desktop) {
    final chart = _FinanceChartCard(
      days: snapshot.chartDays,
      selectedIndex: _selectedChartIndex,
      onSelected: (index) => setState(() => _selectedChartIndex = index),
    );
    final mercadoPago = _MercadoPagoCard(
      controller: widget.controller,
      onConfigure: _configureMercadoPago,
    );
    if (!desktop) {
      return Column(
        key: const Key('finance-lower-stack'),
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [chart, const SizedBox(height: 10), mercadoPago],
      );
    }
    return LayoutBuilder(
      builder: (context, constraints) {
        final unit = constraints.maxWidth / 3.25;
        return SizedBox(
          height: constraints.maxWidth < 1000 ? 300 : 267,
          child: Row(
            key: const Key('finance-lower-grid'),
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(width: unit * 2.15 - 10, child: chart),
              const SizedBox(width: 10),
              Expanded(child: mercadoPago),
            ],
          ),
        );
      },
    );
  }
}

class _FinanceHero extends StatelessWidget {
  const _FinanceHero({
    required this.controller,
    required this.desktop,
    required this.onReceive,
    required this.onExpense,
    required this.onProduct,
  });

  final AgendaController controller;
  final bool desktop;
  final VoidCallback onReceive;
  final VoidCallback onExpense;
  final VoidCallback onProduct;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final heading = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'AGENDA LIVRE',
              style: TextStyle(
                color: t.accent,
                fontSize: 10,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(width: 10),
            Container(width: 28, height: 1, color: t.accent),
          ],
        ),
        const SizedBox(height: 5),
        Text(
          'Financeiro',
          style: TextStyle(
            color: t.ink,
            fontSize: 28,
            height: 1.05,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          'Acompanhe entradas, saídas e a saúde financeira do seu negócio.',
          style: TextStyle(color: t.muted, fontSize: 12.5),
        ),
        const SizedBox(height: 5),
        Text(
          controller.businessName,
          style: TextStyle(
            color: t.accentDark,
            fontSize: 13,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
    final actions = <Widget>[
      _FinanceHeroAction(
        width: 150,
        primary: true,
        icon: Icons.account_balance_wallet_outlined,
        label: 'Lançar entrada',
        onPressed: onReceive,
      ),
      _FinanceHeroAction(
        width: 154,
        icon: Icons.arrow_circle_down_outlined,
        label: 'Lançar despesa',
        onPressed: onExpense,
      ),
      _FinanceHeroAction(
        width: 154,
        icon: Icons.shopping_bag_outlined,
        label: 'Vender produto',
        onPressed: onProduct,
      ),
    ];

    return _FinanceSurface(
      key: const Key('finance-hero'),
      radius: 24,
      minHeight: 140,
      padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 17),
      clip: true,
      child: Stack(
        children: [
          const Positioned(
            right: 0,
            top: -6,
            width: 300,
            height: 104,
            child: IgnorePointer(
              child: Opacity(opacity: .045, child: _FinanceWatermark()),
            ),
          ),
          ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 92),
            child: desktop
                ? Row(
                    children: [
                      Expanded(child: heading),
                      const SizedBox(width: 20),
                      Row(
                        children: [
                          for (
                            var index = 0;
                            index < actions.length;
                            index++
                          ) ...[
                            if (index > 0) const SizedBox(width: 8),
                            actions[index],
                          ],
                        ],
                      ),
                    ],
                  )
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      heading,
                      const SizedBox(height: 15),
                      for (var index = 0; index < actions.length; index++) ...[
                        if (index > 0) const SizedBox(height: 8),
                        SizedBox(height: 42, child: actions[index]),
                      ],
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

class _FinanceHeroAction extends StatelessWidget {
  const _FinanceHeroAction({
    required this.width,
    required this.icon,
    required this.label,
    required this.onPressed,
    this.primary = false,
  });

  final double width;
  final IconData icon;
  final String label;
  final VoidCallback onPressed;
  final bool primary;

  @override
  Widget build(BuildContext context) {
    final content = Row(
      mainAxisAlignment: MainAxisAlignment.center,
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 18),
        const SizedBox(width: 8),
        Flexible(
          child: FittedBox(
            fit: BoxFit.scaleDown,
            child: Text(label, maxLines: 1),
          ),
        ),
      ],
    );
    final button = primary
        ? ElevatedButton(
            onPressed: onPressed,
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(horizontal: 10),
            ),
            child: content,
          )
        : OutlinedButton(
            onPressed: onPressed,
            style: OutlinedButton.styleFrom(
              padding: const EdgeInsets.symmetric(horizontal: 10),
            ),
            child: content,
          );
    return SizedBox(width: width, height: 42, child: button);
  }
}

class _FinanceMetricStrip extends StatelessWidget {
  const _FinanceMetricStrip({required this.snapshot, required this.desktop});

  final _FinanceSnapshot snapshot;
  final bool desktop;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        if (!desktop) {
          return Container(
            key: const Key('finance-metric-strip'),
            height: 188,
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: const Color(0xFF171614),
              borderRadius: BorderRadius.circular(22),
            ),
            child: Column(
              children: [
                Expanded(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        child: _DarkFinanceMetric(
                          key: const Key('finance-metric-result'),
                          label: 'Resultado',
                          value: money(snapshot.balance),
                          caption: snapshot.balanceCaption,
                          badge: snapshot.balanceBadge,
                          icon: Icons.show_chart_rounded,
                          iconColor: Colors.white,
                          iconBackground: snapshot.balance == 0
                              ? t.accent
                              : snapshot.balanceTone,
                          valueColor: Colors.white,
                          compact: true,
                          showBottomDivider: true,
                        ),
                      ),
                      Expanded(
                        child: _DarkFinanceMetric(
                          key: const Key('finance-metric-received'),
                          label: 'Entrou',
                          value: money(snapshot.receivedMonth),
                          caption: 'recebido no mês',
                          icon: Icons.credit_card_rounded,
                          iconColor: t.accent,
                          valueColor: Colors.white,
                          compact: true,
                          showDivider: false,
                          showBottomDivider: true,
                        ),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        child: _DarkFinanceMetric(
                          key: const Key('finance-metric-pending'),
                          label: 'A cobrar',
                          value: money(snapshot.pendingTotal),
                          caption: snapshot.pendingCaption,
                          icon: Icons.schedule_rounded,
                          iconColor: t.accent,
                          valueColor: t.accent,
                          compact: true,
                        ),
                      ),
                      Expanded(
                        child: _DarkFinanceMetric(
                          key: const Key('finance-metric-expenses'),
                          label: 'Gastos',
                          value: money(snapshot.expensesMonth),
                          caption: 'lançados no mês',
                          icon: Icons.receipt_long_rounded,
                          iconColor: const Color(0xFFEF4444),
                          valueColor: const Color(0xFFFCA5A5),
                          compact: true,
                          showDivider: false,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          );
        }

        return Container(
          key: const Key('finance-metric-strip'),
          height: 102,
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: const Color(0xFF171614),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: _DarkFinanceMetric(
                  key: const Key('finance-metric-result'),
                  label: 'Resultado',
                  value: money(snapshot.balance),
                  caption: snapshot.balanceCaption,
                  badge: snapshot.balanceBadge,
                  icon: Icons.show_chart_rounded,
                  iconColor: Colors.white,
                  iconBackground: snapshot.balance == 0
                      ? t.accent
                      : snapshot.balanceTone,
                  valueColor: Colors.white,
                ),
              ),
              Expanded(
                child: _DarkFinanceMetric(
                  key: const Key('finance-metric-received'),
                  label: 'Entrou',
                  value: money(snapshot.receivedMonth),
                  caption: 'recebido no mês',
                  icon: Icons.credit_card_rounded,
                  iconColor: t.accent,
                  valueColor: Colors.white,
                ),
              ),
              Expanded(
                child: _DarkFinanceMetric(
                  key: const Key('finance-metric-pending'),
                  label: 'A cobrar',
                  value: money(snapshot.pendingTotal),
                  caption: snapshot.pendingCaption,
                  icon: Icons.schedule_rounded,
                  iconColor: t.accent,
                  valueColor: t.accent,
                ),
              ),
              Expanded(
                child: _DarkFinanceMetric(
                  key: const Key('finance-metric-expenses'),
                  label: 'Gastos',
                  value: money(snapshot.expensesMonth),
                  caption: 'lançados no mês',
                  icon: Icons.receipt_long_rounded,
                  iconColor: const Color(0xFFEF4444),
                  valueColor: const Color(0xFFFCA5A5),
                  showDivider: false,
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _DarkFinanceMetric extends StatelessWidget {
  const _DarkFinanceMetric({
    super.key,
    required this.label,
    required this.value,
    required this.caption,
    required this.icon,
    required this.iconColor,
    required this.valueColor,
    this.iconBackground = const Color(0xFF2A2826),
    this.badge,
    this.showDivider = true,
    this.showBottomDivider = false,
    this.compact = false,
  });

  final String label;
  final String value;
  final String caption;
  final IconData icon;
  final Color iconColor;
  final Color iconBackground;
  final Color valueColor;
  final String? badge;
  final bool showDivider;
  final bool showBottomDivider;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 8 : 14,
        vertical: compact ? 8 : 10,
      ),
      decoration: BoxDecoration(
        border: Border(
          right: showDivider
              ? const BorderSide(color: Color(0x32FFFFFF))
              : BorderSide.none,
          bottom: showBottomDivider
              ? const BorderSide(color: Color(0x32FFFFFF))
              : BorderSide.none,
        ),
      ),
      child: Row(
        children: [
          SizedBox(
            width: compact ? 39 : 52,
            child: Center(
              child: Container(
                width: compact ? 34 : 40,
                height: compact ? 34 : 40,
                decoration: BoxDecoration(
                  color: iconBackground,
                  borderRadius: BorderRadius.circular(compact ? 11 : 14),
                ),
                alignment: Alignment.center,
                child: Icon(icon, color: iconColor, size: compact ? 17 : 20),
              ),
            ),
          ),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: const Color(0xFFC9C4BE),
                          fontSize: compact ? 10.2 : 11.5,
                          fontWeight: FontWeight.w600,
                          height: 1,
                        ),
                      ),
                    ),
                    if (badge != null)
                      Container(
                        padding: EdgeInsets.symmetric(
                          horizontal: compact ? 5 : 8,
                          vertical: compact ? 2 : 3,
                        ),
                        decoration: BoxDecoration(
                          color: _financeBalanceBadgeBackground(
                            badge!,
                            AgendaThemeTokens.of(context),
                          ),
                          borderRadius: BorderRadius.circular(11),
                        ),
                        child: Text(
                          badge!,
                          style: TextStyle(
                            color: _financeBalanceBadgeForeground(
                              badge!,
                              AgendaThemeTokens.of(context),
                            ),
                            fontSize: compact ? 8.8 : 10,
                            fontWeight: FontWeight.w700,
                            height: 1,
                          ),
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: valueColor,
                    fontSize: compact ? 17.5 : 21,
                    fontWeight: FontWeight.w700,
                    height: 1,
                  ),
                ),
                Text(
                  caption,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: const Color(0xFF938D87),
                    fontSize: compact ? 9.2 : 10.5,
                    height: 1,
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

class _FinanceCardHeader extends StatelessWidget {
  const _FinanceCardHeader({
    required this.title,
    required this.value,
    required this.icon,
    required this.iconColor,
    required this.iconBackground,
    required this.valueColor,
    this.trailing,
  });

  final String title;
  final String value;
  final IconData icon;
  final Color iconColor;
  final Color iconBackground;
  final Color valueColor;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: iconBackground,
            borderRadius: BorderRadius.circular(12),
          ),
          alignment: Alignment.center,
          child: Icon(icon, size: 20, color: iconColor),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 16.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 1),
              Text(
                value,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: valueColor,
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
        if (trailing != null) ...[const SizedBox(width: 10), trailing!],
      ],
    );
  }
}

class _FinanceSurface extends StatelessWidget {
  const _FinanceSurface({
    super.key,
    required this.child,
    required this.radius,
    required this.padding,
    this.minHeight,
    this.clip = false,
  });

  final Widget child;
  final double radius;
  final EdgeInsets padding;
  final double? minHeight;
  final bool clip;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final body = Padding(padding: padding, child: child);
    return Container(
      constraints: minHeight == null
          ? null
          : BoxConstraints(minHeight: minHeight!),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(radius),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0C171411),
            blurRadius: 24,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: clip
          ? ClipRRect(borderRadius: BorderRadius.circular(radius), child: body)
          : body,
    );
  }
}

class _FinanceWatermark extends StatelessWidget {
  const _FinanceWatermark();

  static const _scale = 300 / 1021;

  @override
  Widget build(BuildContext context) {
    return ClipRect(
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: -328 * _scale,
            top: -(194 * _scale) - ((543 * _scale - 104) / 2),
            width: 1365 * _scale,
            height: 1365 * _scale,
            child: Image.asset(
              'assets/branding/agenda-livre-logo-source.png',
              fit: BoxFit.fill,
            ),
          ),
        ],
      ),
    );
  }
}

Color _financeBalanceBadgeBackground(String badge, AgendaThemeTokens tokens) {
  final normalized = badge.toLowerCase();
  if (normalized.contains('positiv')) return const Color(0xFFDCFCE7);
  if (normalized.contains('negativ')) return const Color(0xFFFEE2E2);
  return tokens.accentSoft;
}

Color _financeBalanceBadgeForeground(String badge, AgendaThemeTokens tokens) {
  final normalized = badge.toLowerCase();
  if (normalized.contains('positiv')) return _positiveDark;
  if (normalized.contains('negativ')) return _negativeDark;
  return tokens.accent;
}

class _FinanceSourcesCard extends StatelessWidget {
  const _FinanceSourcesCard({
    required this.serviceTotal,
    required this.productTotal,
    required this.manualTotal,
    required this.total,
    required this.onDetails,
  });

  final double serviceTotal;
  final double productTotal;
  final double manualTotal;
  final double total;
  final VoidCallback onDetails;

  @override
  Widget build(BuildContext context) {
    return _FinanceSurface(
      key: const Key('finance-sources-card'),
      radius: 16,
      minHeight: 152,
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _FinanceCardHeader(
            title: 'Entradas do mês',
            value: money(total),
            icon: Icons.payments_outlined,
            iconColor: _positive,
            iconBackground: const Color(0xFFECFDF5),
            valueColor: _positiveDark,
            trailing: OutlinedButton(
              onPressed: onDetails,
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(78, 40),
                padding: const EdgeInsets.symmetric(horizontal: 10),
              ),
              child: const Text('Detalhes'),
            ),
          ),
          const SizedBox(height: 9),
          _ValueLine(label: 'Serviços finalizados', value: money(serviceTotal)),
          _ValueLine(label: 'Produtos vendidos', value: money(productTotal)),
          _ValueLine(label: 'Recebimentos avulsos', value: money(manualTotal)),
        ],
      ),
    );
  }
}

class _FinanceChannelAttributionCard extends StatelessWidget {
  const _FinanceChannelAttributionCard({
    required this.snapshot,
    required this.desktop,
  });

  final _FinanceSnapshot snapshot;
  final bool desktop;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final cards = <_FinanceChannelSummary>[
      snapshot.directChannel,
      snapshot.whatsAppChannel,
      snapshot.instagramChannel,
    ];
    return _FinanceSurface(
      key: const Key('finance-channel-attribution-card'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Receita e agenda por canal',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'A origem acompanha o cliente, o agendamento e o recebimento.',
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                  ],
                ),
              ),
              DecoratedBox(
                decoration: BoxDecoration(
                  color: t.accentSoft,
                  border: Border.all(color: t.line),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 5,
                  ),
                  child: Text(
                    [
                          if (snapshot.whatsAppLinked) 'WhatsApp',
                          if (snapshot.instagramLinked) 'Instagram',
                        ].join(' + ').trim().isEmpty
                        ? 'Canais não conectados'
                        : '${[if (snapshot.whatsAppLinked) 'WhatsApp', if (snapshot.instagramLinked) 'Instagram'].join(' + ')} sincronizado(s)',
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (desktop)
            Row(
              children: [
                for (var index = 0; index < cards.length; index++) ...[
                  if (index > 0) const SizedBox(width: 8),
                  Expanded(child: _FinanceChannelTile(data: cards[index])),
                ],
              ],
            )
          else
            Column(
              children: [
                for (var index = 0; index < cards.length; index++) ...[
                  if (index > 0) const SizedBox(height: 8),
                  _FinanceChannelTile(data: cards[index]),
                ],
              ],
            ),
        ],
      ),
    );
  }
}

class _FinanceChannelTile extends StatelessWidget {
  const _FinanceChannelTile({required this.data});

  final _FinanceChannelSummary data;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final (icon, tone, background, label) = switch (data.channel) {
      'whatsapp' => (
        Icons.chat_rounded,
        const Color(0xFF15803D),
        const Color(0xFFDCFCE7),
        'WhatsApp',
      ),
      'instagram' => (
        Icons.camera_alt_outlined,
        const Color(0xFFBE185D),
        const Color(0xFFFCE7F3),
        'Instagram',
      ),
      _ => (
        Icons.calendar_month_outlined,
        t.ink,
        t.graySoft,
        'Direto / balcão',
      ),
    };
    return DecoratedBox(
      decoration: BoxDecoration(
        color: background.withValues(alpha: .45),
        border: Border.all(color: tone.withValues(alpha: .18)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: background,
              child: Icon(icon, size: 19, color: tone),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    label,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    money(data.revenue, cents: false),
                    style: TextStyle(
                      color: tone,
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    '${data.appointments} agendamento(s)'
                    '${data.channel == 'direct' ? '' : ' · ${data.conversations} conversa(s)'}',
                    style: TextStyle(color: t.muted, fontSize: 9.5),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ValueLine extends StatelessWidget {
  const _ValueLine({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 7),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.muted,
                    fontSize: 11.8,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Text(
                value,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
        Divider(height: 1, color: t.line),
      ],
    );
  }
}

class _PendingCard extends StatelessWidget {
  const _PendingCard({
    required this.appointments,
    required this.total,
    required this.onReceive,
  });

  final List<Appointment> appointments;
  final double total;
  final ValueChanged<Appointment> onReceive;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _FinanceSurface(
      key: const Key('finance-pending-card'),
      radius: 16,
      minHeight: 152,
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _FinanceCardHeader(
            title: 'A receber',
            value: money(total),
            icon: Icons.group_outlined,
            iconColor: t.accent,
            iconBackground: t.accentSoft,
            valueColor: _warningDark,
          ),
          const SizedBox(height: 9),
          Text(
            appointments.length == 1
                ? '1 atendimento em aberto'
                : '${appointments.length} atendimentos em aberto',
            style: TextStyle(color: t.muted, fontSize: 11.5),
          ),
          if (appointments.isEmpty)
            _CompactListRow(
              icon: Icons.person_outline_rounded,
              title: 'Sem cobrança pendente',
              detail: 'Tudo certo no caixa.',
              value: money(0),
              tone: t.accent,
              background: t.accentSoft,
            )
          else
            for (final appointment in appointments.take(8))
              _PendingAppointmentRow(
                appointment: appointment,
                onReceive: () => onReceive(appointment),
              ),
          if (appointments.isNotEmpty) ...[
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(Icons.touch_app_outlined, color: t.muted, size: 16),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    'Escolha o atendimento para receber sem duplicar a entrada.',
                    style: TextStyle(color: t.muted, fontSize: 10.5),
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _PendingAppointmentRow extends StatelessWidget {
  const _PendingAppointmentRow({
    required this.appointment,
    required this.onReceive,
  });

  final Appointment appointment;
  final VoidCallback onReceive;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final customer = appointment.customerName.trim().isEmpty
        ? 'Cliente'
        : appointment.customerName;
    return Padding(
      padding: const EdgeInsets.only(top: 7),
      child: Material(
        color: t.yellowSoft,
        borderRadius: BorderRadius.circular(11),
        child: InkWell(
          onTap: onReceive,
          borderRadius: BorderRadius.circular(11),
          child: Container(
            constraints: const BoxConstraints(minHeight: 56),
            padding: const EdgeInsets.fromLTRB(10, 6, 6, 6),
            decoration: BoxDecoration(
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(11),
            ),
            child: Row(
              children: [
                AgendaIconBadge(
                  Icons.person_outline_rounded,
                  background: t.panel,
                  color: t.accentDark,
                ),
                const SizedBox(width: 9),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        customer,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 11.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      Text(
                        '${shortDate(appointment.start)} ${hour(appointment.start)} · ${appointment.serviceName}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: t.muted, fontSize: 10),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  money(appointment.price),
                  style: TextStyle(
                    color: _warningDark,
                    fontSize: 11.5,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                IconButton(
                  key: ValueKey('finance-receive-${appointment.id}'),
                  tooltip: 'Receber este atendimento',
                  onPressed: onReceive,
                  icon: const Icon(Icons.arrow_forward_rounded, size: 18),
                  style: IconButton.styleFrom(
                    foregroundColor: t.accentDark,
                    minimumSize: const Size(44, 44),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ExpensesCard extends StatelessWidget {
  const _ExpensesCard({
    required this.expenses,
    required this.total,
    required this.onAdd,
  });

  final List<ExpenseItem> expenses;
  final double total;
  final VoidCallback onAdd;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _FinanceSurface(
      key: const Key('finance-expenses-card'),
      radius: 16,
      minHeight: 152,
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _FinanceCardHeader(
            title: 'Gastos',
            value: money(total),
            icon: Icons.receipt_long_outlined,
            iconColor: const Color(0xFFEF4444),
            iconBackground: const Color(0xFFFFF1F2),
            valueColor: _negativeDark,
          ),
          const SizedBox(height: 9),
          Text(
            'Despesas lançadas no mês.',
            style: TextStyle(color: t.muted, fontSize: 11.5),
          ),
          if (expenses.isEmpty)
            _CompactListRow(
              icon: Icons.receipt_outlined,
              title: 'Sem gastos lançados',
              detail: 'Despesas aparecerão aqui.',
              value: money(0),
              tone: _negative,
              background: t.redSoft,
            )
          else
            for (final expense in expenses.take(8))
              _CompactListRow(
                icon: Icons.receipt_outlined,
                title: expense.description.trim().isEmpty
                    ? 'Despesa'
                    : expense.description,
                detail:
                    '${shortDate(expense.date)} | ${expense.category.trim().isEmpty ? 'Despesa' : expense.category} | ${expense.isPaid ? 'pago' : 'pendente'}',
                value: money(expense.value),
                tone: _negative,
                background: t.redSoft,
              ),
          const SizedBox(height: 9),
          Align(
            alignment: Alignment.centerLeft,
            child: SizedBox(
              height: 40,
              child: OutlinedButton(
                onPressed: onAdd,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size(86, 40),
                ),
                child: const Text('Lançar'),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _CompactListRow extends StatelessWidget {
  const _CompactListRow({
    required this.icon,
    required this.title,
    required this.detail,
    required this.value,
    required this.tone,
    required this.background,
  });

  final IconData icon;
  final String title;
  final String detail;
  final String value;
  final Color tone;
  final Color background;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      constraints: const BoxConstraints(minHeight: 36),
      margin: const EdgeInsets.only(top: 6),
      child: Row(
        children: [
          SizedBox(
            width: 32,
            child: Align(
              alignment: Alignment.centerLeft,
              child: AgendaIconBadge(
                icon,
                size: 26,
                iconSize: 13,
                color: tone,
                background: background,
              ),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(left: 6, right: 10),
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
                      fontSize: 12.3,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 1),
                  Text(
                    detail,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 10.5),
                  ),
                ],
              ),
            ),
          ),
          Text(
            value,
            style: TextStyle(
              color: t.ink,
              fontSize: 12.2,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _FinanceChartCard extends StatelessWidget {
  const _FinanceChartCard({
    required this.days,
    required this.selectedIndex,
    required this.onSelected,
  });

  final List<_FinanceDay> days;
  final int? selectedIndex;
  final ValueChanged<int> onSelected;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final total = days.fold<double>(0, (sum, item) => sum + item.value);
    final average = days.isEmpty ? 0.0 : total / days.length;
    final best = days.fold<double>(
      0,
      (value, item) => math.max(value, item.value),
    );
    return _FinanceSurface(
      key: const Key('finance-chart-card'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Últimos 7 dias',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Recebido por dia.',
                      style: TextStyle(color: t.muted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
              Container(
                height: 30,
                constraints: const BoxConstraints(minWidth: 88),
                padding: const EdgeInsets.symmetric(horizontal: 12),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: t.accentSoft,
                  border: Border.all(color: t.line),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  'Recebido',
                  style: TextStyle(
                    color: t.accent,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          LayoutBuilder(
            builder: (context, chart) {
              final compactLabels = chart.maxWidth < 520;
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  GestureDetector(
                    behavior: HitTestBehavior.opaque,
                    onTapDown: (details) {
                      if (days.isEmpty) return;
                      const left = 56.0;
                      const right = 6.0;
                      final usable = math.max(
                        1.0,
                        chart.maxWidth - left - right,
                      );
                      final relative =
                          ((details.localPosition.dx - left) / usable).clamp(
                            0.0,
                            1.0,
                          );
                      onSelected((relative * (days.length - 1)).round());
                    },
                    child: SizedBox(
                      height: 76,
                      child: CustomPaint(
                        painter: _FinanceLinePainter(
                          values: days.map((item) => item.value).toList(),
                          lineColor: t.accent,
                          gridColor: const Color(0xFFE8E3DE),
                          labelColor: t.muted,
                          selectedIndex: selectedIndex,
                        ),
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.only(left: 56, top: 8),
                    child: Row(
                      children: [
                        for (var index = 0; index < days.length; index++)
                          Expanded(
                            child: InkWell(
                              onTap: () => onSelected(index),
                              borderRadius: BorderRadius.circular(6),
                              child: Padding(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 1,
                                ),
                                child: Column(
                                  children: [
                                    Text(
                                      money(days[index].value, cents: false),
                                      maxLines: 1,
                                      style: TextStyle(
                                        color: t.accent,
                                        fontSize: compactLabels ? 8.5 : 10.5,
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                    const SizedBox(height: 5),
                                    if (compactLabels) ...[
                                      Text(
                                        _weekday(days[index].day),
                                        style: TextStyle(
                                          color: t.muted,
                                          fontSize: 8,
                                        ),
                                      ),
                                      Text(
                                        shortDate(days[index].day),
                                        style: TextStyle(
                                          color: t.muted,
                                          fontSize: 8,
                                        ),
                                      ),
                                    ] else
                                      Text(
                                        '${_weekday(days[index].day)}, ${shortDate(days[index].day)}',
                                        maxLines: 1,
                                        textAlign: TextAlign.center,
                                        style: TextStyle(
                                          color: t.muted,
                                          fontSize: 10.2,
                                        ),
                                      ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: _ChartStat(
                  label: 'Total no período',
                  value: money(total),
                ),
              ),
              Expanded(
                child: _ChartStat(
                  label: 'Média diária',
                  value: money(average),
                  divider: true,
                ),
              ),
              Expanded(
                child: _ChartStat(
                  label: 'Maior dia',
                  value: money(best),
                  divider: true,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ChartStat extends StatelessWidget {
  const _ChartStat({
    required this.label,
    required this.value,
    this.divider = false,
  });

  final String label;
  final String value;
  final bool divider;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      decoration: BoxDecoration(
        border: divider ? Border(left: BorderSide(color: t.line)) : null,
      ),
      child: Column(
        children: [
          Text(
            label,
            textAlign: TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 11),
          ),
          const SizedBox(height: 3),
          Text(
            value,
            maxLines: 1,
            textAlign: TextAlign.center,
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

class _MercadoPagoCard extends StatelessWidget {
  const _MercadoPagoCard({required this.controller, required this.onConfigure});

  final AgendaController controller;
  final VoidCallback onConfigure;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final settings = controller.data.settings;
    final enabled = settings.mercadoPagoEnabled;
    final connected = enabled && settings.mercadoPagoConnected;
    final terminal = settings.mercadoPagoDefaultTerminalLabel.trim();
    final ready = connected && terminal.isNotEmpty;
    final status = ready
        ? terminal
        : enabled
        ? 'Falta conectar Point'
        : 'Desativado';
    final detail = ready
        ? 'Crédito e débito podem ir direto para a maquininha.'
        : 'Ative em Configurações para liberar cartão na maquininha.';
    return _FinanceSurface(
      key: const Key('finance-mercado-pago-card'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Mercado Pago',
            style: TextStyle(
              color: t.ink,
              fontSize: 17,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            'Configure cartão e maquininha para receber com mais agilidade.',
            style: TextStyle(color: t.muted, fontSize: 11.5),
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: t.warmSoft,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  status,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 2),
                Text(detail, style: TextStyle(color: t.muted, fontSize: 11)),
              ],
            ),
          ),
          const SizedBox(height: 10),
          SizedBox(
            height: 42,
            child: OutlinedButton(
              onPressed: onConfigure,
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 12),
              ),
              child: Row(
                children: [
                  const SizedBox(
                    width: 38,
                    child: Align(
                      alignment: Alignment.centerLeft,
                      child: Icon(Icons.credit_card_outlined, size: 19),
                    ),
                  ),
                  const Expanded(
                    child: Text(
                      'Configurar Mercado Pago',
                      style: TextStyle(fontWeight: FontWeight.w600),
                    ),
                  ),
                  Icon(Icons.chevron_right_rounded, size: 18, color: t.muted),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FinanceLinePainter extends CustomPainter {
  const _FinanceLinePainter({
    required this.values,
    required this.lineColor,
    required this.gridColor,
    required this.labelColor,
    required this.selectedIndex,
  });

  final List<double> values;
  final Color lineColor;
  final Color gridColor;
  final Color labelColor;
  final int? selectedIndex;

  @override
  void paint(Canvas canvas, Size size) {
    if (values.isEmpty || size.width <= 70 || size.height <= 30) return;
    const left = 56.0;
    const right = 6.0;
    const top = 10.0;
    const bottom = 14.0;
    final width = size.width - left - right;
    final height = size.height - top - bottom;
    final rawMax = values.fold<double>(0, math.max);
    final maxValue = rawMax <= 0 ? 1.0 : rawMax;

    final gridPaint = Paint()
      ..color = gridColor
      ..strokeWidth = 1;
    const labels = ['R\$ 300', 'R\$ 150', 'R\$ 0'];
    for (var index = 0; index < labels.length; index++) {
      final fraction = index / (labels.length - 1);
      final y = top + height * fraction;
      canvas.drawLine(
        Offset(left, y),
        Offset(size.width - right, y),
        gridPaint,
      );
      final painter = TextPainter(
        text: TextSpan(
          text: labels[index],
          style: TextStyle(color: labelColor, fontSize: 10.5),
        ),
        textDirection: TextDirection.ltr,
        maxLines: 1,
      )..layout(maxWidth: left - 6);
      final labelY = index == 0
          ? 0.0
          : index == labels.length - 1
          ? size.height - painter.height
          : y - painter.height / 2;
      painter.paint(canvas, Offset(0, labelY));
    }

    final path = Path();
    final points = <Offset>[];
    for (var index = 0; index < values.length; index++) {
      final x = values.length == 1
          ? left
          : left + width * index / (values.length - 1);
      final y = top + height - (values[index] / maxValue) * height;
      final point = Offset(x, y);
      points.add(point);
      if (index == 0) {
        path.moveTo(x, y);
      } else {
        path.lineTo(x, y);
      }
    }
    canvas.drawPath(
      path,
      Paint()
        ..color = lineColor
        ..style = PaintingStyle.stroke
        ..strokeWidth = 3
        ..strokeCap = StrokeCap.round
        ..strokeJoin = StrokeJoin.round,
    );

    for (var index = 0; index < points.length; index++) {
      if (values[index] <= 0) continue;
      final selected = selectedIndex == index;
      if (selected) {
        canvas.drawCircle(
          points[index],
          8,
          Paint()..color = lineColor.withValues(alpha: 0.16),
        );
      }
      canvas.drawCircle(
        points[index],
        selected ? 4.5 : 3.5,
        Paint()..color = Colors.white,
      );
      canvas.drawCircle(
        points[index],
        selected ? 3.2 : 2.4,
        Paint()..color = lineColor,
      );
    }
  }

  @override
  bool shouldRepaint(covariant _FinanceLinePainter oldDelegate) =>
      oldDelegate.values != values ||
      oldDelegate.lineColor != lineColor ||
      oldDelegate.gridColor != gridColor ||
      oldDelegate.labelColor != labelColor ||
      oldDelegate.selectedIndex != selectedIndex;
}

class _PaymentDialog extends StatefulWidget {
  const _PaymentDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_PaymentDialog> createState() => _PaymentDialogState();
}

class _PaymentDialogState extends State<_PaymentDialog> {
  final _formKey = GlobalKey<FormState>();
  final _description = TextEditingController(text: 'Pagamento avulso');
  final _value = TextEditingController(text: '0,00');
  final _notes = TextEditingController();
  String _customer = '';
  String _category = 'Agendamento';
  String _method = 'Pix';
  String? _error;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _description.addListener(_refreshSummary);
    _value.addListener(_refreshSummary);
    _notes.addListener(_refreshSummary);
  }

  void _refreshSummary() {
    if (mounted) setState(() {});
  }

  static const _categories = <String>[
    'Agendamento',
    'Produto',
    'Sinal',
    'Mensalidade',
    'Ajuste',
    'Outro',
  ];

  @override
  void dispose() {
    _description.removeListener(_refreshSummary);
    _value.removeListener(_refreshSummary);
    _notes.removeListener(_refreshSummary);
    _description.dispose();
    _value.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final parsedValue = _parseMoney(_value.text)!;
    final mercadoPago = _method.startsWith('Mercado Pago');
    final settings = widget.controller.data.settings;
    if (mercadoPago &&
        !(settings.mercadoPagoEnabled &&
            settings.mercadoPagoConnected &&
            settings.mercadoPagoDefaultTerminalId.trim().isNotEmpty)) {
      setState(() {
        _error =
            'Ative o Mercado Pago, conecte a conta e escolha uma maquininha Point em Configurações.';
      });
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      MercadoPagoPaymentOutcome? mercadoPagoOutcome;
      if (mercadoPago) {
        mercadoPagoOutcome = await _chargeWithMercadoPago(parsedValue);
        if (mercadoPagoOutcome == null) {
          if (mounted) setState(() => _saving = false);
          return;
        }
      }
      await widget.controller.addPayment(
        ManualPayment(
          description: _description.text.trim(),
          customerName: _customer.trim(),
          category: _category,
          paymentMethod: _method,
          paymentProvider: mercadoPago ? 'Mercado Pago' : '',
          paymentReference: mercadoPagoOutcome?.reference ?? '',
          paymentStatus: mercadoPagoOutcome?.status ?? '',
          notes: _notes.text.trim(),
          value: parsedValue,
          paidAt: DateTime.now(),
        ),
      );
      if (mounted) Navigator.of(context).pop(true);
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = error is MercadoPagoException
            ? error.message
            : 'Não foi possível registrar o pagamento: $error';
      });
    }
  }

  Future<MercadoPagoPaymentOutcome?> _chargeWithMercadoPago(
    double amount,
  ) async {
    final settings = widget.controller.data.settings;
    final service = widget.controller.mercadoPagoService;
    if (service == null ||
        !settings.mercadoPagoEnabled ||
        !settings.mercadoPagoConnected ||
        settings.mercadoPagoDefaultTerminalId.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Ative o Mercado Pago, conecte a conta e escolha uma maquininha Point em Configurações.',
      );
    }
    final method = _method.contains('débito')
        ? MercadoPagoPointMethod.debit
        : MercadoPagoPointMethod.credit;
    final amountInCents = (amount * 100).round();
    final charge = await service.createPointCharge(
      MercadoPagoPointChargeRequest(
        amountInCents: amountInCents,
        method: method,
        terminalId: settings.mercadoPagoDefaultTerminalId,
        description: _description.text.trim(),
        items: <MercadoPagoChargeItem>[
          MercadoPagoChargeItem(
            code: 'FINANCEIRO',
            title: _description.text.trim(),
            quantity: 1,
            unitPriceInCents: amountInCents,
            description: _notes.text.trim(),
          ),
        ],
      ),
    );
    if (!charge.ok) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidResponse,
        charge.message.trim().isEmpty
            ? 'Mercado Pago recusou a cobrança.'
            : charge.message,
        statusCode: charge.statusCode,
      );
    }
    if (!mounted) return null;
    return showMercadoPagoPointProgressDialog(
      context,
      service: service,
      charge: charge,
      amount: amount,
      method: method,
      terminalLabel: settings.mercadoPagoDefaultTerminalLabel.trim().isEmpty
          ? settings.mercadoPagoDefaultTerminalId
          : settings.mercadoPagoDefaultTerminalLabel,
    );
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final customers =
        widget.controller.data.customers
            .map((item) => item.name.trim())
            .where((item) => item.isNotEmpty)
            .toSet()
            .toList()
          ..sort();
    final ready =
        widget.controller.data.settings.mercadoPagoEnabled &&
        widget.controller.data.settings.mercadoPagoConnected &&
        widget.controller.data.settings.mercadoPagoDefaultTerminalId
            .trim()
            .isNotEmpty;
    return _FinanceDialogShell(
      dialogKey: const Key('finance-payment-dialog'),
      desktopWidth: 1040,
      bodyHorizontalPadding: 0,
      maxHeight: 760,
      title: 'Registrar pagamento',
      subtitle: 'Lance um recebimento avulso no financeiro.',
      primaryLabel: _saving ? 'Registrando...' : 'Registrar pagamento',
      primaryButtonWidth: 164,
      saving: _saving,
      onCancel: () => Navigator.of(context).pop(false),
      onPrimary: _submit,
      child: Form(
        key: _formKey,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final split = constraints.maxWidth >= 760;
            final form = Padding(
              padding: EdgeInsets.fromLTRB(
                split ? 30 : 16,
                split ? 8 : 4,
                split ? 30 : 16,
                split ? 22 : 18,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Dados do recebimento',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 22),
                  _FinanceLabeledField(
                    label: 'Descrição',
                    fieldKey: const Key('payment-description-field'),
                    child: TextFormField(
                      controller: _description,
                      autofocus: true,
                      style: const TextStyle(fontSize: 13),
                      decoration: _financeTextDecoration(
                        t,
                        hintText: 'Ex: Sinal de agendamento',
                        minHeight: 38,
                      ),
                      validator: (value) =>
                          value == null || value.trim().isEmpty
                          ? 'Informe a descrição.'
                          : null,
                    ),
                  ),
                  const SizedBox(height: 18),
                  _DialogPair(
                    left: _FinanceLabeledField(
                      label: 'Cliente',
                      fieldKey: const Key('payment-customer-field'),
                      child: Autocomplete<String>(
                        optionsBuilder: (text) {
                          final query = text.text.trim().toLowerCase();
                          if (query.isEmpty) return customers;
                          return customers.where(
                            (item) => item.toLowerCase().contains(query),
                          );
                        },
                        onSelected: (value) => _customer = value,
                        fieldViewBuilder:
                            (context, controller, focusNode, onSubmit) {
                              return TextFormField(
                                controller: controller,
                                focusNode: focusNode,
                                style: const TextStyle(fontSize: 13),
                                decoration: _financeComboDecoration(
                                  t,
                                  height: 38,
                                  suffixIcon: const Icon(
                                    Icons.arrow_drop_down_rounded,
                                    size: 20,
                                  ),
                                ),
                                onChanged: (value) =>
                                    setState(() => _customer = value),
                              );
                            },
                      ),
                    ),
                    right: _FinanceLabeledField(
                      label: 'Categoria',
                      fieldKey: const Key('payment-category-field'),
                      child: DropdownButtonFormField<String>(
                        initialValue: _category,
                        isExpanded: true,
                        style: TextStyle(color: t.ink, fontSize: 13),
                        decoration: _financeComboDecoration(t, height: 38),
                        items: [
                          for (final item in _categories)
                            DropdownMenuItem(value: item, child: Text(item)),
                        ],
                        onChanged: (value) =>
                            setState(() => _category = value ?? _category),
                      ),
                    ),
                  ),
                  const SizedBox(height: 18),
                  _FinanceLabeledField(
                    label: 'Forma de pagamento',
                    fieldKey: const Key('payment-method-field'),
                    child: DropdownButtonFormField<String>(
                      initialValue: _method,
                      isExpanded: true,
                      style: TextStyle(color: t.ink, fontSize: 13),
                      decoration: _financeComboDecoration(t, height: 38),
                      items: [
                        for (final item in _paymentMethods)
                          DropdownMenuItem(
                            value: item,
                            child: Text(
                              item,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                      ],
                      onChanged: (value) =>
                          setState(() => _method = value ?? _method),
                    ),
                  ),
                  const SizedBox(height: 18),
                  _FinanceLabeledField(
                    label: 'Observações',
                    fieldKey: const Key('payment-notes-field'),
                    child: SizedBox(
                      height: 66,
                      child: TextFormField(
                        controller: _notes,
                        expands: true,
                        minLines: null,
                        maxLines: null,
                        textAlignVertical: TextAlignVertical.top,
                        style: const TextStyle(fontSize: 13),
                        decoration: _financeTextDecoration(
                          t,
                          hintText:
                              'Ex: pago antecipado, comprovante enviado, ajuste manual',
                          minHeight: 66,
                          multiline: true,
                        ),
                      ),
                    ),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 10),
                    Text(
                      _error!,
                      style: const TextStyle(color: _negative, fontSize: 12),
                    ),
                  ],
                ],
              ),
            );
            final summary = _PaymentSummary(
              valueController: _value,
              category: _category,
              method: _method,
              customer: _customer,
              notes: _notes.text,
              ready: ready,
            );
            if (!split) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  form,
                  Divider(height: 1, color: t.line),
                  summary,
                ],
              );
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(flex: 7, child: form),
                Container(width: 1, height: 440, color: t.line),
                Expanded(flex: 3, child: summary),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _PaymentSummary extends StatelessWidget {
  const _PaymentSummary({
    required this.valueController,
    required this.category,
    required this.method,
    required this.customer,
    required this.notes,
    required this.ready,
  });

  final TextEditingController valueController;
  final String category;
  final String method;
  final String customer;
  final String notes;
  final bool ready;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('payment-summary'),
      color: const Color(0xFFFFFCFA),
      padding: const EdgeInsets.fromLTRB(28, 8, 28, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Resumo do recebimento',
            style: TextStyle(
              color: t.ink,
              fontSize: 16,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 12),
          _FinanceLabeledField(
            label: 'Valor recebido',
            fieldKey: const Key('payment-value-field'),
            child: TextFormField(
              controller: valueController,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              style: TextStyle(
                color: t.accent,
                fontSize: 27,
                fontWeight: FontWeight.w800,
              ),
              decoration:
                  _financeTextDecoration(
                    t,
                    hintText: '0,00',
                    minHeight: 58,
                  ).copyWith(
                    prefixText: 'R\$ ',
                    prefixStyle: TextStyle(
                      color: t.accent,
                      fontSize: 20,
                      fontWeight: FontWeight.w700,
                    ),
                    filled: false,
                    contentPadding: EdgeInsets.zero,
                    border: InputBorder.none,
                    enabledBorder: InputBorder.none,
                    focusedBorder: UnderlineInputBorder(
                      borderSide: BorderSide(color: t.accent),
                    ),
                  ),
              validator: (value) {
                final parsed = _parseMoney(value ?? '');
                return parsed == null || parsed <= 0
                    ? 'Informe um valor maior que zero.'
                    : null;
              },
            ),
          ),
          const SizedBox(height: 10),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 10),
          _PaymentSummaryRow(
            icon: Icons.event_outlined,
            label: 'Categoria',
            value: category,
          ),
          const SizedBox(height: 4),
          _PaymentSummaryRow(
            icon: Icons.credit_card_outlined,
            label: 'Forma de pagamento',
            value: method,
          ),
          const SizedBox(height: 4),
          _PaymentSummaryRow(
            icon: Icons.person_outline,
            label: 'Cliente',
            value: customer.trim().isEmpty ? 'Não informado' : customer.trim(),
          ),
          const SizedBox(height: 4),
          _PaymentSummaryRow(
            icon: Icons.description_outlined,
            label: 'Observações',
            value: notes.trim().isEmpty ? 'Nenhuma observação' : notes.trim(),
          ),
          const SizedBox(height: 6),
          Container(
            key: const Key('payment-terminal-card'),
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
            decoration: BoxDecoration(
              color: ready ? const Color(0xFFF0FDF4) : const Color(0xFFFFF7ED),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: ready ? _positive : _warning),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  Icons.point_of_sale_outlined,
                  color: ready ? _positive : t.accent,
                  size: 21,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        ready ? 'Maquininha pronta' : 'Maquininha desativada',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        ready
                            ? 'Point pronta para receber pagamentos.'
                            : 'Ative em Configurações para liberar crédito/débito.',
                        style: TextStyle(
                          color: t.muted,
                          fontSize: 11.5,
                          height: 1.35,
                        ),
                      ),
                    ],
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

class _PaymentSummaryRow extends StatelessWidget {
  const _PaymentSummaryRow({
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
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 20, color: t.muted),
        const SizedBox(width: 11),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: TextStyle(color: t.muted, fontSize: 11.5)),
              const SizedBox(height: 2),
              Text(
                value,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _ExpenseDialog extends StatefulWidget {
  const _ExpenseDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_ExpenseDialog> createState() => _ExpenseDialogState();
}

class _ExpenseDialogState extends State<_ExpenseDialog> {
  final _formKey = GlobalKey<FormState>();
  final _description = TextEditingController();
  final _supplier = TextEditingController();
  final _value = TextEditingController(text: '0,00');
  final _notes = TextEditingController();
  String _category = 'Operacional';
  String _method = 'Pix';
  String? _error;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _description.addListener(_refreshSummary);
    _supplier.addListener(_refreshSummary);
    _value.addListener(_refreshSummary);
    _notes.addListener(_refreshSummary);
  }

  void _refreshSummary() {
    if (mounted) setState(() {});
  }

  static const _categories = <String>[
    'Operacional',
    'Fornecedor',
    'Equipe',
    'Marketing',
    'Aluguel',
    'Impostos',
    'Estoque',
  ];

  @override
  void dispose() {
    _description.removeListener(_refreshSummary);
    _supplier.removeListener(_refreshSummary);
    _value.removeListener(_refreshSummary);
    _notes.removeListener(_refreshSummary);
    _description.dispose();
    _supplier.dispose();
    _value.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      await widget.controller.addExpense(
        ExpenseItem(
          description: _description.text.trim(),
          category: _category,
          supplier: _supplier.text.trim(),
          paymentMethod: _method,
          notes: _notes.text.trim(),
          value: _parseMoney(_value.text)!,
          date: DateTime.now(),
          isPaid: true,
        ),
      );
      if (mounted) Navigator.of(context).pop(true);
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = 'Não foi possível cadastrar a despesa: $error';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _FinanceDialogShell(
      dialogKey: const Key('finance-expense-dialog'),
      desktopWidth: 1040,
      bodyHorizontalPadding: 0,
      maxHeight: 840,
      title: 'Nova despesa',
      subtitle: 'Registre custos do dia, fornecedores ou operação.',
      primaryLabel: _saving ? 'Salvando...' : 'Salvar despesa',
      saving: _saving,
      onCancel: () => Navigator.of(context).pop(false),
      onPrimary: _submit,
      child: Form(
        key: _formKey,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final split = constraints.maxWidth >= 900;
            final form = Padding(
              padding: EdgeInsets.fromLTRB(
                split ? 30 : 16,
                split ? 8 : 4,
                split ? 30 : 16,
                split ? 20 : 18,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Dados da despesa',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 18),
                  _FinanceLabeledField(
                    label: 'Descrição',
                    fieldKey: const Key('expense-description-field'),
                    child: TextFormField(
                      controller: _description,
                      autofocus: true,
                      style: const TextStyle(fontSize: 13),
                      decoration: _financeTextDecoration(
                        t,
                        hintText: 'Ex: Aluguel, comissão, material',
                      ),
                      validator: (value) =>
                          value == null || value.trim().isEmpty
                          ? 'Informe a descrição.'
                          : null,
                    ),
                  ),
                  const SizedBox(height: 15),
                  _FinanceLabeledField(
                    label: 'Fornecedor / responsável',
                    fieldKey: const Key('expense-supplier-field'),
                    child: TextFormField(
                      controller: _supplier,
                      style: const TextStyle(fontSize: 13),
                      decoration: _financeTextDecoration(
                        t,
                        hintText: 'Ex: distribuidora, proprietário, equipe',
                      ),
                    ),
                  ),
                  const SizedBox(height: 15),
                  _DialogPair(
                    left: _FinanceLabeledField(
                      label: 'Categoria',
                      fieldKey: const Key('expense-category-field'),
                      child: DropdownButtonFormField<String>(
                        initialValue: _category,
                        isExpanded: true,
                        style: TextStyle(color: t.ink, fontSize: 13),
                        decoration: _financeComboDecoration(t),
                        items: [
                          for (final item in _categories)
                            DropdownMenuItem(value: item, child: Text(item)),
                        ],
                        onChanged: (value) =>
                            setState(() => _category = value ?? _category),
                      ),
                    ),
                    right: _FinanceLabeledField(
                      label: 'Forma de pagamento',
                      fieldKey: const Key('expense-method-field'),
                      child: DropdownButtonFormField<String>(
                        initialValue: _method,
                        isExpanded: true,
                        style: TextStyle(color: t.ink, fontSize: 13),
                        decoration: _financeComboDecoration(t),
                        items: [
                          for (final item in _paymentMethods)
                            DropdownMenuItem(
                              value: item,
                              child: Text(
                                item,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                        ],
                        onChanged: (value) =>
                            setState(() => _method = value ?? _method),
                      ),
                    ),
                  ),
                  const SizedBox(height: 15),
                  _FinanceLabeledField(
                    label: 'Valor',
                    fieldKey: const Key('expense-value-field'),
                    child: TextFormField(
                      controller: _value,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                      decoration: _financeTextDecoration(
                        t,
                        hintText: '0,00',
                        minHeight: 48,
                      ).copyWith(prefixText: 'R\$ '),
                      validator: (value) {
                        final parsed = _parseMoney(value ?? '');
                        return parsed == null || parsed <= 0
                            ? 'Informe um valor maior que zero.'
                            : null;
                      },
                    ),
                  ),
                  const SizedBox(height: 15),
                  _FinanceLabeledField(
                    label: 'Observações',
                    fieldKey: const Key('expense-notes-field'),
                    child: SizedBox(
                      height: 76,
                      child: TextFormField(
                        controller: _notes,
                        expands: true,
                        minLines: null,
                        maxLines: null,
                        textAlignVertical: TextAlignVertical.top,
                        style: const TextStyle(fontSize: 13),
                        decoration: _financeTextDecoration(
                          t,
                          hintText:
                              'Ex: vencimento, nota, parcela, recorrência',
                          minHeight: 76,
                          multiline: true,
                        ),
                      ),
                    ),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 10),
                    Text(
                      _error!,
                      style: const TextStyle(color: _negative, fontSize: 12),
                    ),
                  ],
                ],
              ),
            );
            final summary = _ExpenseSummary(
              description: _description.text,
              supplier: _supplier.text,
              value: _value.text,
              category: _category,
              method: _method,
            );
            if (!split) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  form,
                  Divider(height: 1, color: t.line),
                  summary,
                ],
              );
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(flex: 7, child: form),
                Container(width: 1, height: 440, color: t.line),
                Expanded(flex: 3, child: summary),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _ExpenseSummary extends StatelessWidget {
  const _ExpenseSummary({
    required this.description,
    required this.supplier,
    required this.value,
    required this.category,
    required this.method,
  });

  final String description;
  final String supplier;
  final String value;
  final String category;
  final String method;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final parsed = _parseMoney(value);
    final formatted = parsed == null ? 'R\$ 0,00' : money(parsed);
    return Container(
      key: const Key('expense-summary'),
      color: const Color(0xFFFFFCFA),
      padding: const EdgeInsets.fromLTRB(28, 8, 28, 18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Resumo da despesa',
            style: TextStyle(
              color: t.ink,
              fontSize: 16,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            formatted,
            key: const Key('expense-summary-value'),
            style: TextStyle(
              color: t.accent,
              fontSize: 30,
              height: 1.1,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 16),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 16),
          _PaymentSummaryRow(
            icon: Icons.description_outlined,
            label: 'Descrição',
            value: description.trim().isEmpty
                ? 'Não informada'
                : description.trim(),
          ),
          const SizedBox(height: 14),
          _PaymentSummaryRow(
            icon: Icons.category_outlined,
            label: 'Categoria',
            value: category,
          ),
          const SizedBox(height: 14),
          _PaymentSummaryRow(
            icon: Icons.credit_card_outlined,
            label: 'Forma de pagamento',
            value: method,
          ),
          const SizedBox(height: 14),
          _PaymentSummaryRow(
            icon: Icons.person_outline,
            label: 'Fornecedor / responsável',
            value: supplier.trim().isEmpty ? 'Não informado' : supplier.trim(),
          ),
          const SizedBox(height: 18),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 11),
            decoration: BoxDecoration(
              color: const Color(0xFFFFF5EF),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                Icon(Icons.sell_outlined, size: 18, color: t.accent),
                const SizedBox(width: 9),
                Text('Valor', style: TextStyle(color: t.muted, fontSize: 12)),
                const Spacer(),
                Text(
                  formatted,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
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

class _FinanceDialogShell extends StatelessWidget {
  const _FinanceDialogShell({
    required this.dialogKey,
    required this.desktopWidth,
    this.bodyHorizontalPadding = 24,
    required this.maxHeight,
    required this.title,
    required this.subtitle,
    required this.primaryLabel,
    this.primaryButtonWidth = 150,
    required this.saving,
    required this.onCancel,
    required this.onPrimary,
    required this.child,
  });

  final Key dialogKey;
  final double desktopWidth;
  final double bodyHorizontalPadding;
  final double maxHeight;
  final String title;
  final String subtitle;
  final String primaryLabel;
  final double primaryButtonWidth;
  final bool saving;
  final VoidCallback onCancel;
  final VoidCallback onPrimary;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final viewport = MediaQuery.sizeOf(context);
    final compact = viewport.width < 650;
    final horizontalInset = compact ? 8.0 : 20.0;
    final verticalInset = compact ? 8.0 : 16.0;
    final width = math.min(
      desktopWidth,
      math.max(0.0, viewport.width - horizontalInset * 2),
    );
    final height = math.min(
      620.0,
      math.min(maxHeight, math.max(0.0, viewport.height - verticalInset * 2)),
    );
    final radius = BorderRadius.circular(compact ? 14 : 2);

    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: EdgeInsets.symmetric(
        horizontal: horizontalInset,
        vertical: verticalInset,
      ),
      child: SizedBox(
        width: width,
        height: height,
        child: Material(
          key: dialogKey,
          color: t.panel,
          elevation: 14,
          shadowColor: Colors.black.withValues(alpha: .18),
          borderRadius: radius,
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: [
              Container(
                key: const Key('finance-dialog-header'),
                constraints: const BoxConstraints(minHeight: 88),
                width: double.infinity,
                padding: EdgeInsets.fromLTRB(
                  compact ? 16 : 22,
                  compact ? 14 : 18,
                  compact ? 16 : 22,
                  compact ? 14 : 18,
                ),
                decoration: BoxDecoration(
                  color: const Color(0xFFFFF9F4),
                  border: Border(bottom: BorderSide(color: t.line)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 22,
                        fontWeight: FontWeight.w700,
                        height: 1.2,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      subtitle,
                      style: TextStyle(
                        color: t.muted,
                        fontSize: 13,
                        height: 1.25,
                      ),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: SingleChildScrollView(
                  key: const Key('finance-dialog-scroll'),
                  padding: EdgeInsets.fromLTRB(
                    compact ? 16 : bodyHorizontalPadding,
                    compact ? 14 : 20,
                    compact ? 16 : bodyHorizontalPadding,
                    compact ? 16 : 18,
                  ),
                  child: child,
                ),
              ),
              Container(
                key: const Key('finance-dialog-footer'),
                width: double.infinity,
                padding: EdgeInsets.fromLTRB(
                  compact ? 16 : 22,
                  compact ? 12 : 16,
                  compact ? 16 : 22,
                  compact ? 14 : 18,
                ),
                decoration: BoxDecoration(
                  color: t.panel,
                  border: Border(top: BorderSide(color: t.line)),
                ),
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final cancelButton = SizedBox(
                      width: 110,
                      height: 40,
                      child: OutlinedButton(
                        key: const Key('finance-dialog-cancel'),
                        onPressed: saving ? null : onCancel,
                        style: OutlinedButton.styleFrom(
                          foregroundColor: t.ink,
                          padding: EdgeInsets.zero,
                          minimumSize: const Size(110, 40),
                          side: BorderSide(color: t.line),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                        child: const Text('Cancelar'),
                      ),
                    );
                    final primaryButton = SizedBox(
                      width: primaryButtonWidth,
                      height: 40,
                      child: ElevatedButton(
                        key: const Key('finance-dialog-save'),
                        onPressed: saving ? null : onPrimary,
                        style: ElevatedButton.styleFrom(
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          minimumSize: Size(primaryButtonWidth, 40),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                        child: saving
                            ? const SizedBox.square(
                                dimension: 16,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : Text(
                                primaryLabel,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                      ),
                    );
                    if (constraints.maxWidth >= 120 + primaryButtonWidth) {
                      return Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          cancelButton,
                          const SizedBox(width: 10),
                          primaryButton,
                        ],
                      );
                    }
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        primaryButton,
                        const SizedBox(height: 8),
                        cancelButton,
                      ],
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _FinanceLabeledField extends StatelessWidget {
  const _FinanceLabeledField({
    required this.label,
    required this.child,
    this.fieldKey,
  });

  final String label;
  final Widget child;
  final Key? fieldKey;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      key: fieldKey,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: 2),
        Text(
          label,
          style: TextStyle(
            color: t.muted,
            fontSize: 12,
            fontWeight: FontWeight.w600,
            height: 1.2,
          ),
        ),
        const SizedBox(height: 5),
        child,
      ],
    );
  }
}

InputDecoration _financeTextDecoration(
  AgendaThemeTokens t, {
  String? hintText,
  double minHeight = 40,
  bool multiline = false,
}) {
  return InputDecoration(
    hintText: hintText,
    hintStyle: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
    filled: true,
    fillColor: t.panel,
    isDense: true,
    constraints: BoxConstraints(minHeight: minHeight),
    contentPadding: multiline
        ? const EdgeInsets.fromLTRB(12, 10, 12, 10)
        : const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: BorderSide(color: t.line),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: BorderSide(color: t.line),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: BorderSide(color: t.accent, width: 1.3),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: const BorderSide(color: _negative),
    ),
    focusedErrorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: const BorderSide(color: _negative, width: 1.3),
    ),
  );
}

InputDecoration _financeComboDecoration(
  AgendaThemeTokens t, {
  String? hintText,
  Widget? suffixIcon,
  double height = 42,
}) {
  return InputDecoration(
    hintText: hintText,
    hintStyle: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
    suffixIcon: suffixIcon,
    suffixIconConstraints: const BoxConstraints(minWidth: 28, minHeight: 28),
    filled: false,
    isDense: true,
    constraints: BoxConstraints(minHeight: height, maxHeight: height),
    contentPadding: const EdgeInsets.fromLTRB(12, 8, 8, 8),
    border: UnderlineInputBorder(borderSide: BorderSide(color: t.line)),
    enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: t.line)),
    focusedBorder: UnderlineInputBorder(
      borderSide: BorderSide(color: t.accent, width: 1.3),
    ),
  );
}

class _DialogPair extends StatelessWidget {
  const _DialogPair({required this.left, required this.right});

  final Widget left;
  final Widget right;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 520) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [left, const SizedBox(height: 12), right],
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(child: left),
            const SizedBox(width: 16),
            Expanded(child: right),
          ],
        );
      },
    );
  }
}

class _FinanceSnapshot {
  const _FinanceSnapshot({
    required this.serviceMonth,
    required this.productMonth,
    required this.manualMonth,
    required this.receivedMonth,
    required this.expensesMonth,
    required this.balance,
    required this.pending,
    required this.pendingTotal,
    required this.expenses,
    required this.chartDays,
    required this.directChannel,
    required this.whatsAppChannel,
    required this.instagramChannel,
    required this.whatsAppLinked,
    required this.instagramLinked,
  });

  final double serviceMonth;
  final double productMonth;
  final double manualMonth;
  final double receivedMonth;
  final double expensesMonth;
  final double balance;
  final List<Appointment> pending;
  final double pendingTotal;
  final List<ExpenseItem> expenses;
  final List<_FinanceDay> chartDays;
  final _FinanceChannelSummary directChannel;
  final _FinanceChannelSummary whatsAppChannel;
  final _FinanceChannelSummary instagramChannel;
  final bool whatsAppLinked;
  final bool instagramLinked;

  factory _FinanceSnapshot.from(AgendaController controller) {
    final today = DateUtils.dateOnly(DateTime.now());
    final monthStart = DateTime(today.year, today.month);
    final nextMonth = DateTime(today.year, today.month + 1);
    final data = controller.data;
    final serviceMonth = _sumServiceRevenue(data, monthStart, nextMonth);
    final productMonth = data.productSales
        .where((item) => _isBetween(item.soldAt, monthStart, nextMonth))
        .fold<double>(0, (sum, item) => sum + item.total);
    final manualMonth = data.manualPayments
        .where((item) => _isBetween(item.paidAt, monthStart, nextMonth))
        .fold<double>(0, (sum, item) => sum + item.value);
    final expensesMonth = controller.expensesBetween(monthStart, nextMonth);
    final pending =
        data.appointments
            .where(
              (item) =>
                  item.price > 0 &&
                  item.paymentConfirmedAt == null &&
                  !const {
                    AppointmentStatus.cancelled,
                    AppointmentStatus.noShow,
                    AppointmentStatus.blocked,
                  }.contains(item.status),
            )
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start));
    final expenses = List<ExpenseItem>.of(data.expenses)
      ..sort((a, b) => b.date.compareTo(a.date));
    final chartDays = List<_FinanceDay>.generate(7, (index) {
      final day = today.add(Duration(days: index - 6));
      return _FinanceDay(
        day: day,
        value: _sumReceivedRevenue(data, day, day.add(const Duration(days: 1))),
      );
    });
    final receivedMonth = serviceMonth + productMonth + manualMonth;
    final directChannel = _financeChannelSummary(
      data,
      monthStart,
      nextMonth,
      'direct',
    );
    final whatsAppChannel = _financeChannelSummary(
      data,
      monthStart,
      nextMonth,
      'whatsapp',
    );
    final instagramChannel = _financeChannelSummary(
      data,
      monthStart,
      nextMonth,
      'instagram',
    );
    return _FinanceSnapshot(
      serviceMonth: serviceMonth,
      productMonth: productMonth,
      manualMonth: manualMonth,
      receivedMonth: receivedMonth,
      expensesMonth: expensesMonth,
      balance: receivedMonth - expensesMonth,
      pending: pending,
      pendingTotal: pending.fold<double>(0, (sum, item) => sum + item.price),
      expenses: expenses,
      chartDays: chartDays,
      directChannel: directChannel,
      whatsAppChannel: whatsAppChannel,
      instagramChannel: instagramChannel,
      whatsAppLinked: data.settings.whatsAppLinked,
      instagramLinked: data.settings.instagramLinked,
    );
  }

  String get pendingCaption => pending.length == 1
      ? '1 atendimento em aberto'
      : '${pending.length} atendimentos em aberto';

  String get balanceCaption => balance > 0
      ? 'Acima das despesas'
      : balance < 0
      ? 'Despesas acima das entradas'
      : 'Sem movimentação no período';

  String get balanceBadge => balance > 0
      ? 'Positivo'
      : balance < 0
      ? 'Negativo'
      : 'Neutro';

  Color get balanceTone => balance > 0
      ? _positive
      : balance < 0
      ? _negative
      : _neutral;

  Color get balanceSoftTone => balance > 0
      ? const Color(0xFFDCFCE7)
      : balance < 0
      ? const Color(0xFFFEE2E2)
      : const Color(0xFFE2E8F0);

  Color get balanceValueColor => balance > 0
      ? _positiveDark
      : balance < 0
      ? _negativeDark
      : const Color(0xFF334155);
}

class _FinanceDay {
  const _FinanceDay({required this.day, required this.value});

  final DateTime day;
  final double value;
}

class _FinanceChannelSummary {
  const _FinanceChannelSummary({
    required this.channel,
    required this.appointments,
    required this.conversations,
    required this.revenue,
  });

  final String channel;
  final int appointments;
  final int conversations;
  final double revenue;
}

_FinanceChannelSummary _financeChannelSummary(
  AgendaData data,
  DateTime start,
  DateTime end,
  String channel,
) {
  final normalized = _financeNormalizeChannel(channel);
  final appointments = data.appointments
      .where(
        (item) =>
            _isBetween(item.start, start, end) &&
            item.status != AppointmentStatus.blocked &&
            _financeAppointmentChannel(item) == normalized,
      )
      .toList();
  final appointmentById = <String, Appointment>{
    for (final item in data.appointments) item.id: item,
  };
  var revenue = appointments
      .where((item) => item.status == AppointmentStatus.done)
      .fold<double>(0, (sum, item) => sum + item.price);
  revenue += data.productSales
      .where(
        (item) =>
            _isBetween(item.soldAt, start, end) &&
            _financeResolvedChannel(
                  item.sourceChannel,
                  item.appointmentId,
                  appointmentById,
                ) ==
                normalized,
      )
      .fold<double>(0, (sum, item) => sum + item.total);
  revenue += data.manualPayments
      .where(
        (item) =>
            _isBetween(item.paidAt, start, end) &&
            _financeResolvedChannel(
                  item.sourceChannel,
                  item.appointmentId,
                  appointmentById,
                ) ==
                normalized,
      )
      .fold<double>(0, (sum, item) => sum + item.value);
  final conversations = data.channelConversations.where((item) {
    final timestamp = item.lastMessageAt ?? item.updatedAt;
    return _financeNormalizeChannel(item.channel) == normalized &&
        _isBetween(timestamp, start, end);
  }).length;
  return _FinanceChannelSummary(
    channel: normalized,
    appointments: appointments.length,
    conversations: conversations,
    revenue: revenue,
  );
}

String _financeResolvedChannel(
  String source,
  String appointmentId,
  Map<String, Appointment> appointmentById,
) {
  if (source.trim().isNotEmpty) return _financeNormalizeChannel(source);
  final appointment = appointmentById[appointmentId];
  return appointment == null
      ? 'direct'
      : _financeAppointmentChannel(appointment);
}

String _financeAppointmentChannel(Appointment appointment) =>
    _financeNormalizeChannel(
      appointment.bookingChannel.trim().isNotEmpty
          ? appointment.bookingChannel
          : appointment.externalSource,
    );

String _financeNormalizeChannel(String value) {
  final normalized = value.trim().toLowerCase();
  if (normalized.contains('whatsapp') ||
      normalized == 'wa' ||
      normalized == 'evolution') {
    return 'whatsapp';
  }
  if (normalized.contains('instagram') ||
      normalized == 'ig' ||
      normalized == 'direct') {
    return 'instagram';
  }
  return 'direct';
}

bool _isBetween(DateTime value, DateTime start, DateTime end) =>
    !value.isBefore(start) && value.isBefore(end);

double _sumReceivedRevenue(AgendaData data, DateTime start, DateTime end) =>
    _sumServiceRevenue(data, start, end) +
    data.productSales
        .where((item) => _isBetween(item.soldAt, start, end))
        .fold<double>(0, (sum, item) => sum + item.total) +
    data.manualPayments
        .where((item) => _isBetween(item.paidAt, start, end))
        .fold<double>(0, (sum, item) => sum + item.value);

double _sumServiceRevenue(AgendaData data, DateTime start, DateTime end) {
  final receivableAppointmentIds = data.customerReceivables
      .where((item) => item.status.trim().toLowerCase() != 'cancelled')
      .map((item) => item.appointmentId.trim().toLowerCase())
      .where((id) => id.isNotEmpty)
      .toSet();
  final appointmentRevenue = data.appointments
      .where((item) {
        final paidAt = item.paymentConfirmedAt;
        return paidAt != null &&
            _isBetween(paidAt, start, end) &&
            !receivableAppointmentIds.contains(item.id.trim().toLowerCase());
      })
      .fold<double>(0, (sum, item) => sum + item.price);
  final customerAccountRevenue = data.customerReceivables
      .where((item) {
        final paidAt = item.paidAt;
        return item.status.trim().toLowerCase() == 'paid' &&
            paidAt != null &&
            _isBetween(paidAt, start, end);
      })
      .fold<double>(0, (sum, item) => sum + item.originalValue);
  return appointmentRevenue + customerAccountRevenue;
}

String _weekday(DateTime date) =>
    const ['seg', 'ter', 'qua', 'qui', 'sex', 'sáb', 'dom'][date.weekday - 1];

double? _parseMoney(String source) {
  var normalized = source.trim().replaceAll(RegExp(r'[^0-9,.-]'), '');
  if (normalized.isEmpty) return null;
  if (normalized.contains(',')) {
    normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
  }
  return double.tryParse(normalized);
}
