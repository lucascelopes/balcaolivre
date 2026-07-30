import 'dart:math' as math;
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/business_profile.dart';
import '../../core/formatters.dart';
import '../../core/motion.dart';
import '../../domain/models/models.dart';
import 'finance_desktop_legacy.dart';

const _financeInk = Color(0xFF1C1B1A);
const _financeAccent = Color(0xFFED6823);
const _financeAccentDark = Color(0xFFB74716);
const _financeLine = Color(0xFFE7E1DC);
const _financeMuted = Color(0xFF746E69);
const _financeSoft = Color(0xFFF3F0ED);
const _financeGreen = Color(0xFF17855B);
const _financeGreenSoft = Color(0xFFE9F6EF);
const _financeRed = Color(0xFFC54842);
const _financeRedSoft = Color(0xFFFFEEEC);

class FinanceWpfDashboard extends StatefulWidget {
  const FinanceWpfDashboard({
    super.key,
    required this.controller,
    required this.onReceive,
    required this.onExpense,
    required this.onProduct,
  });

  final AgendaController controller;
  final VoidCallback onReceive;
  final VoidCallback onExpense;
  final VoidCallback onProduct;

  @override
  State<FinanceWpfDashboard> createState() => _FinanceWpfDashboardState();
}

class _FinanceWpfDashboardState extends State<FinanceWpfDashboard> {
  late DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);

  Future<void> _pickMonth() async {
    final selected = await showDatePicker(
      context: context,
      initialDate: _month,
      firstDate: DateTime(2020),
      lastDate: DateTime(2100, 12, 31),
      helpText: 'Selecionar mês da análise',
      fieldLabelText: 'Data de referência',
    );
    if (!mounted || selected == null) return;
    setState(() => _month = DateTime(selected.year, selected.month));
  }

  Future<void> _newMovement() async {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 760;
    final action = await showModalBottomSheet<_FinanceAction>(
      context: context,
      isScrollControlled: compact,
      showDragHandle: true,
      backgroundColor: compact ? t.appBackground : null,
      barrierColor: Colors.black.withValues(alpha: .42),
      shape: compact
          ? const RoundedRectangleBorder(
              borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
            )
          : null,
      builder: (context) =>
          compact ? const _NewMovementSheet() : const _LegacyNewMovementSheet(),
    );
    if (!mounted) return;
    switch (action) {
      case _FinanceAction.receive:
        widget.onReceive();
      case _FinanceAction.expense:
        widget.onExpense();
      case _FinanceAction.product:
        widget.onProduct();
      case null:
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (MediaQuery.sizeOf(context).width >= 760) {
      return FinanceDesktopLegacyDashboard(
        controller: widget.controller,
        onReceive: widget.onReceive,
        onExpense: widget.onExpense,
        onProduct: widget.onProduct,
      );
    }
    final t = AgendaThemeTokens.of(context);
    final overview = _FinanceOverview.from(widget.controller.data, _month);
    final profile = AgendaBusinessProfile.fromSettings(
      widget.controller.data.settings,
    );
    return ColoredBox(
      color: t.appBackground,
      child: LayoutBuilder(
        builder: (context, viewport) {
          final compact = viewport.maxWidth < 760;
          final wide = viewport.maxWidth >= 900;
          return SingleChildScrollView(
            key: const Key('finance-page-scroll'),
            padding: EdgeInsets.fromLTRB(
              compact ? 14 : 20,
              compact ? 14 : 18,
              compact ? 14 : 26,
              compact ? 96 : 44,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                AgendaReveal(
                  child: _FinanceToolbar(
                    month: _month,
                    compact: compact,
                    onMonth: _pickMonth,
                    onRefresh: () => setState(() {}),
                    onMovement: _newMovement,
                    onExport: () =>
                        widget.controller.navigate(AgendaPage.reports),
                  ),
                ),
                SizedBox(height: compact ? 14 : 16),
                AgendaReveal(
                  delay: const Duration(milliseconds: 35),
                  child: _FinanceResultHero(
                    overview: overview,
                    month: _month,
                    compact: compact,
                  ),
                ),
                const SizedBox(height: 12),
                AgendaReveal(
                  delay: const Duration(milliseconds: 70),
                  child: _KpiGrid(
                    overview: overview,
                    compact: compact,
                    profile: profile,
                  ),
                ),
                if (compact) ...[
                  const SizedBox(height: 12),
                  AgendaReveal(
                    delay: const Duration(milliseconds: 105),
                    child: _QuickOperationsCard(
                      overview: overview,
                      onReceive: widget.onReceive,
                      onExpense: widget.onExpense,
                      onProduct: widget.onProduct,
                    ),
                  ),
                  const SizedBox(height: 12),
                  AgendaReveal(
                    delay: const Duration(milliseconds: 140),
                    child: _MobileFinanceDetails(
                      overview: overview,
                      profile: profile,
                    ),
                  ),
                ] else ...[
                  const SizedBox(height: 12),
                  if (wide)
                    AgendaReveal(
                      delay: const Duration(milliseconds: 105),
                      child: IntrinsicHeight(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Expanded(
                              flex: 2,
                              child: _ResultFormationCard(overview: overview),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: _NextThirtyDaysCard(
                                overview: overview,
                                compact: compact,
                                profile: profile,
                              ),
                            ),
                          ],
                        ),
                      ),
                    )
                  else ...[
                    AgendaReveal(
                      delay: const Duration(milliseconds: 105),
                      child: _ResultFormationCard(overview: overview),
                    ),
                    const SizedBox(height: 12),
                    AgendaReveal(
                      delay: const Duration(milliseconds: 130),
                      child: _NextThirtyDaysCard(
                        overview: overview,
                        compact: compact,
                        profile: profile,
                      ),
                    ),
                  ],
                  const SizedBox(height: 12),
                  if (wide)
                    AgendaReveal(
                      delay: const Duration(milliseconds: 155),
                      child: IntrinsicHeight(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Expanded(child: _RiskCard(overview: overview)),
                            const SizedBox(width: 12),
                            Expanded(child: _FunnelCard(overview: overview)),
                            const SizedBox(width: 12),
                            Expanded(
                              child: _CompositionCard(overview: overview),
                            ),
                          ],
                        ),
                      ),
                    )
                  else ...[
                    AgendaReveal(
                      delay: const Duration(milliseconds: 155),
                      child: _RiskCard(overview: overview),
                    ),
                    const SizedBox(height: 12),
                    AgendaReveal(
                      delay: const Duration(milliseconds: 180),
                      child: _FunnelCard(overview: overview),
                    ),
                    const SizedBox(height: 12),
                    AgendaReveal(
                      delay: const Duration(milliseconds: 205),
                      child: _CompositionCard(overview: overview),
                    ),
                  ],
                  const SizedBox(height: 12),
                  AgendaReveal(
                    delay: const Duration(milliseconds: 230),
                    child: _ForecastCard(overview: overview, compact: compact),
                  ),
                  const SizedBox(height: 12),
                  AgendaReveal(
                    delay: const Duration(milliseconds: 255),
                    child: _QuickOperationsCard(
                      overview: overview,
                      onReceive: widget.onReceive,
                      onExpense: widget.onExpense,
                      onProduct: widget.onProduct,
                    ),
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

enum _FinanceAction { receive, expense, product }

class _FinanceToolbar extends StatelessWidget {
  const _FinanceToolbar({
    required this.month,
    required this.compact,
    required this.onMonth,
    required this.onRefresh,
    required this.onMovement,
    required this.onExport,
  });

  final DateTime month;
  final bool compact;
  final VoidCallback onMonth;
  final VoidCallback onRefresh;
  final VoidCallback onMovement;
  final VoidCallback onExport;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final title = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'FINANCEIRO',
          style: TextStyle(
            color: t.accent,
            fontSize: 10,
            fontWeight: FontWeight.w700,
            letterSpacing: .8,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          'Financeiro',
          style: TextStyle(
            color: t.ink,
            fontSize: 29,
            height: 1,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 7),
        Text(
          'Resultado, agenda e riscos para decidir com mais segurança.',
          style: TextStyle(color: t.muted, fontSize: 12.5),
        ),
      ],
    );
    final actions = Wrap(
      spacing: 8,
      runSpacing: 8,
      alignment: compact ? WrapAlignment.center : WrapAlignment.end,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        _ToolbarButton(
          key: const Key('finance-month-button'),
          icon: Icons.calendar_month_outlined,
          label: _monthLabel(month),
          caption: !compact && _isCurrentMonth(month) ? 'Mês atual' : null,
          onPressed: onMonth,
        ),
        _ToolbarButton(
          key: const Key('finance-refresh-button'),
          icon: Icons.refresh_rounded,
          label: 'Atualizar análise',
          onPressed: onRefresh,
        ),
        if (compact)
          _NewMovementToolbarButton(
            key: const Key('finance-new-movement-button'),
            onPressed: onMovement,
          )
        else
          _ToolbarButton(
            key: const Key('finance-new-movement-button'),
            icon: Icons.add_rounded,
            label: 'Nova movimentação',
            primary: true,
            onPressed: onMovement,
          ),
        _ToolbarButton(
          key: const Key('finance-export-button'),
          icon: Icons.download_outlined,
          label: 'Exportar',
          onPressed: onExport,
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
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(child: title),
        const SizedBox(width: 18),
        Flexible(
          flex: 3,
          child: Padding(
            padding: const EdgeInsets.only(top: 16),
            child: actions,
          ),
        ),
      ],
    );
  }
}

class _ToolbarButton extends StatelessWidget {
  const _ToolbarButton({
    super.key,
    required this.icon,
    required this.label,
    required this.onPressed,
    this.caption,
    this.primary = false,
  });

  final IconData icon;
  final String label;
  final String? caption;
  final bool primary;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final foreground = primary
        ? Theme.of(context).colorScheme.onPrimary
        : t.ink;
    return SizedBox(
      height: 44,
      child: OutlinedButton.icon(
        onPressed: onPressed,
        icon: Icon(icon, size: 18),
        label: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(label),
            if (caption != null) ...[
              const SizedBox(width: 7),
              Text(
                caption!,
                style: TextStyle(
                  color: t.muted,
                  fontSize: 9.5,
                  fontWeight: FontWeight.w400,
                ),
              ),
            ],
          ],
        ),
        style: OutlinedButton.styleFrom(
          foregroundColor: foreground,
          backgroundColor: primary ? t.accent : t.panel,
          side: BorderSide(color: primary ? t.accent : t.line),
          padding: const EdgeInsets.symmetric(horizontal: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(13),
          ),
          textStyle: const TextStyle(
            fontFamily: 'Segoe UI',
            fontSize: 12,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}

class _NewMovementToolbarButton extends StatelessWidget {
  const _NewMovementToolbarButton({super.key, required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final onAccent = Theme.of(context).colorScheme.onPrimary;
    return DecoratedBox(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(15),
        boxShadow: [
          BoxShadow(
            color: t.accent.withValues(alpha: .18),
            blurRadius: 12,
            offset: Offset(0, 5),
          ),
        ],
      ),
      child: Material(
        color: t.accent,
        borderRadius: BorderRadius.circular(15),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onPressed,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minWidth: 184, minHeight: 48),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 13),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    width: 30,
                    height: 30,
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: .18),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    alignment: Alignment.center,
                    child: Icon(Icons.add_rounded, color: onAccent, size: 20),
                  ),
                  const SizedBox(width: 10),
                  Text(
                    'Nova movimentação',
                    style: TextStyle(
                      color: onAccent,
                      fontFamily: 'Segoe UI',
                      fontSize: 12.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _NewMovementSheet extends StatelessWidget {
  const _NewMovementSheet();

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SafeArea(
      top: false,
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(18, 2, 18, 22),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 520),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: BoxDecoration(
                        color: t.accentSoft,
                        borderRadius: BorderRadius.circular(15),
                      ),
                      alignment: Alignment.center,
                      child: Icon(
                        Icons.swap_vert_circle_outlined,
                        color: t.accentDark,
                        size: 25,
                      ),
                    ),
                    const SizedBox(width: 13),
                    const Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Nova movimentação',
                            style: TextStyle(
                              color: _financeInk,
                              fontSize: 21,
                              height: 1.1,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          SizedBox(height: 5),
                          Text(
                            'Escolha o que deseja registrar.',
                            style: TextStyle(
                              color: _financeMuted,
                              fontSize: 12.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: 'Fechar',
                      onPressed: () => Navigator.pop(context),
                      icon: const Icon(Icons.close_rounded),
                      color: _financeMuted,
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                _MovementOption(
                  key: const Key('finance-movement-receive'),
                  icon: Icons.south_west_rounded,
                  title: 'Lançar entrada',
                  subtitle: 'Registre um valor que entrou no caixa.',
                  tone: _financeGreen,
                  soft: _financeGreenSoft,
                  onTap: () => Navigator.pop(context, _FinanceAction.receive),
                ),
                const SizedBox(height: 10),
                _MovementOption(
                  key: const Key('finance-movement-expense'),
                  icon: Icons.north_east_rounded,
                  title: 'Lançar despesa',
                  subtitle: 'Inclua uma saída e mantenha o resultado correto.',
                  tone: _financeRed,
                  soft: _financeRedSoft,
                  onTap: () => Navigator.pop(context, _FinanceAction.expense),
                ),
                const SizedBox(height: 10),
                _MovementOption(
                  key: const Key('finance-movement-product'),
                  icon: Icons.shopping_bag_outlined,
                  title: 'Vender produto',
                  subtitle: 'Registre a venda e atualize o estoque.',
                  tone: t.accent,
                  soft: t.accentSoft,
                  onTap: () => Navigator.pop(context, _FinanceAction.product),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _LegacyNewMovementSheet extends StatelessWidget {
  const _LegacyNewMovementSheet();

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const ListTile(
            title: Text(
              'Nova movimentação',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
            subtitle: Text('Escolha o que deseja registrar.'),
          ),
          ListTile(
            leading: const Icon(Icons.account_balance_wallet_outlined),
            title: const Text('Lançar entrada'),
            onTap: () => Navigator.pop(context, _FinanceAction.receive),
          ),
          ListTile(
            leading: const Icon(Icons.receipt_long_outlined),
            title: const Text('Lançar despesa'),
            onTap: () => Navigator.pop(context, _FinanceAction.expense),
          ),
          ListTile(
            leading: const Icon(Icons.shopping_bag_outlined),
            title: const Text('Vender produto'),
            onTap: () => Navigator.pop(context, _FinanceAction.product),
          ),
        ],
      ),
    );
  }
}

class _MovementOption extends StatelessWidget {
  const _MovementOption({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.tone,
    required this.soft,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color tone;
  final Color soft;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(17),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Container(
          constraints: const BoxConstraints(minHeight: 72),
          padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 10),
          decoration: BoxDecoration(
            border: Border.all(color: _financeLine),
            borderRadius: BorderRadius.circular(17),
          ),
          child: Row(
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: soft,
                  borderRadius: BorderRadius.circular(14),
                ),
                alignment: Alignment.center,
                child: Icon(icon, color: tone, size: 21),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        color: _financeInk,
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      subtitle,
                      style: const TextStyle(
                        color: _financeMuted,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Icon(Icons.chevron_right_rounded, color: tone, size: 22),
            ],
          ),
        ),
      ),
    );
  }
}

class _FinanceResultHero extends StatelessWidget {
  const _FinanceResultHero({
    required this.overview,
    required this.month,
    required this.compact,
  });

  final _FinanceOverview overview;
  final DateTime month;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final positive = overview.result >= 0;
    final tone = positive ? _financeGreen : _financeRed;
    final heroStart = Color.lerp(t.panel, t.accentSoft, .58)!;
    final heroEnd = Color.lerp(t.panel, t.warmSoft, .42)!;
    final hasGoal = overview.revenueGoal != null && overview.revenueGoal! > 0;
    final goalProgress = hasGoal
        ? (overview.revenue / overview.revenueGoal!).clamp(0, 1).toDouble()
        : 0.0;

    final summary = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Container(
              width: 42,
              height: 42,
              decoration: BoxDecoration(
                color: Color.lerp(t.panel, t.accentSoft, .22),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(
                positive
                    ? Icons.trending_up_rounded
                    : Icons.trending_down_rounded,
                color: tone,
                size: 23,
              ),
            ),
            const SizedBox(width: 11),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'RESULTADO DO MÊS',
                    style: TextStyle(
                      color: t.muted,
                      fontSize: 9.5,
                      fontWeight: FontWeight.w700,
                      letterSpacing: .65,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    _monthLabel(month),
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        AgendaAnimatedValue(
          value: money(overview.result),
          builder: (context, value) => Text(
            value,
            key: const Key('finance-result-hero-value'),
            style: TextStyle(
              color: tone,
              fontSize: compact ? 34 : 39,
              height: .95,
              fontWeight: FontWeight.w800,
              letterSpacing: -1.2,
            ),
          ),
        ),
        const SizedBox(height: 7),
        Text(
          overview.revenue <= 0
              ? 'Registre uma entrada para acompanhar a margem real.'
              : '${overview.margin.round()}% de margem sobre o que entrou no caixa.',
          style: TextStyle(
            color: t.muted,
            fontSize: 11,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );

    final breakdown = Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: t.panel.withValues(alpha: .88),
        border: Border.all(color: t.line.withValues(alpha: .7)),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: _HeroBreakdownValue(
                  label: 'Entrou',
                  value: money(overview.revenue),
                  tone: _financeGreen,
                ),
              ),
              Container(width: 1, height: 38, color: t.line),
              const SizedBox(width: 14),
              Expanded(
                child: _HeroBreakdownValue(
                  label: 'Saiu',
                  value: money(
                    overview.expenses + overview.unregisteredCommissions,
                  ),
                  tone: _financeRed,
                ),
              ),
            ],
          ),
          const SizedBox(height: 13),
          Row(
            children: [
              Expanded(
                child: Text(
                  hasGoal
                      ? 'Meta de receita'
                      : 'Meta mensal ainda não definida',
                  style: TextStyle(
                    color: t.muted,
                    fontSize: 10,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              if (hasGoal)
                Text(
                  '${(goalProgress * 100).round()}%',
                  style: TextStyle(
                    color: t.accentDark,
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                  ),
                ),
            ],
          ),
          const SizedBox(height: 6),
          ClipRRect(
            borderRadius: BorderRadius.circular(10),
            child: TweenAnimationBuilder<double>(
              tween: Tween(begin: 0, end: goalProgress),
              duration: AgendaMotion.duration(context, AgendaMotion.emphasized),
              curve: AgendaMotion.enterCurve,
              builder: (context, progress, _) => LinearProgressIndicator(
                minHeight: 7,
                value: progress,
                backgroundColor: t.accentSoft,
                color: t.accent,
              ),
            ),
          ),
        ],
      ),
    );

    return Container(
      key: const ValueKey('finance-kpi-Resultado líquido'),
      padding: EdgeInsets.all(compact ? 16 : 19),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [heroStart, heroEnd],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        border: Border.all(color: t.accent.withValues(alpha: .16)),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: t.accent.withValues(alpha: .08),
            blurRadius: 18,
            offset: const Offset(0, 7),
          ),
        ],
      ),
      child: compact
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [summary, const SizedBox(height: 16), breakdown],
            )
          : Row(
              children: [
                Expanded(flex: 5, child: summary),
                const SizedBox(width: 22),
                Expanded(flex: 4, child: breakdown),
              ],
            ),
    );
  }
}

class _HeroBreakdownValue extends StatelessWidget {
  const _HeroBreakdownValue({
    required this.label,
    required this.value,
    required this.tone,
  });

  final String label;
  final String value;
  final Color tone;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: TextStyle(color: t.muted, fontSize: 10.5)),
        const SizedBox(height: 3),
        AgendaAnimatedValue(
          value: value,
          builder: (context, formatted) => Text(
            formatted,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: tone,
              fontSize: 17,
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
      ],
    );
  }
}

class _KpiGrid extends StatelessWidget {
  const _KpiGrid({
    required this.overview,
    required this.compact,
    required this.profile,
  });

  final _FinanceOverview overview;
  final bool compact;
  final AgendaBusinessProfile profile;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final items = <_KpiData>[
      _KpiData(
        'Receita',
        money(overview.revenue),
        '${overview.revenueGrowth >= 0 ? '+' : ''}${overview.revenueGrowth.round()}%',
        Icons.payments_outlined,
        _financeGreen,
        overview.revenueGoal == null
            ? 0
            : overview.revenue / overview.revenueGoal!,
      ),
      _KpiData(
        'Despesas registradas',
        money(overview.expenses),
        '${overview.expenseShare.round()}% da receita',
        Icons.receipt_long_outlined,
        _financeRed,
        overview.expenseShare / 100,
      ),
      if (overview.commissions > 0 || overview.registeredCommissions > 0)
        _KpiData(
          'Comissões',
          money(overview.commissions),
          overview.unregisteredCommissions > 0
              ? '${money(overview.unregisteredCommissions)} ainda não lançado'
              : '${overview.commissionShare.round()}% da receita',
          Icons.groups_2_outlined,
          t.accentDark,
          overview.commissionShare / 100,
        ),
      _KpiData(
        'Agenda a receber',
        money(overview.pending),
        '${profile.activityCount(overview.pendingCount)} sem recebimento',
        Icons.event_available_outlined,
        t.accent,
        overview.revenue + overview.pending <= 0
            ? 0
            : overview.pending / (overview.revenue + overview.pending),
      ),
    ];
    return _Surface(
      key: const Key('finance-kpi-grid'),
      padding: EdgeInsets.zero,
      color: t.panel,
      borderColor: t.line,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: EdgeInsets.fromLTRB(
              compact ? 14 : 17,
              compact ? 13 : 15,
              compact ? 14 : 17,
              11,
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Movimentação do mês',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 15.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Entradas, custos e valores ainda a receber.',
                        style: TextStyle(color: t.muted, fontSize: 11),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: t.accentSoft,
                    borderRadius: BorderRadius.circular(9),
                  ),
                  child: Text(
                    'DADOS REAIS',
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 9,
                      fontWeight: FontWeight.w800,
                      letterSpacing: .35,
                    ),
                  ),
                ),
              ],
            ),
          ),
          Divider(height: 1, color: t.line),
          for (var index = 0; index < items.length; index++) ...[
            _KpiRow(
              key: ValueKey('finance-kpi-${items[index].label}'),
              data: items[index],
            ),
            if (index != items.length - 1)
              Divider(height: 1, indent: 61, endIndent: 14, color: t.line),
          ],
        ],
      ),
    );
  }
}

class _KpiData {
  const _KpiData(
    this.label,
    this.value,
    this.caption,
    this.icon,
    this.tone,
    this.progress,
  );
  final String label;
  final String value;
  final String caption;
  final IconData icon;
  final Color tone;
  final double progress;
}

class _KpiRow extends StatelessWidget {
  const _KpiRow({super.key, required this.data});
  final _KpiData data;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      constraints: const BoxConstraints(minHeight: 76),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 11),
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: data.tone.withValues(alpha: .1),
              borderRadius: BorderRadius.circular(11),
            ),
            child: Icon(data.icon, color: data.tone, size: 19),
          ),
          const SizedBox(width: 11),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  data.label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 5),
                ClipRRect(
                  borderRadius: BorderRadius.circular(7),
                  child: TweenAnimationBuilder<double>(
                    tween: Tween(
                      begin: 0,
                      end: data.progress.clamp(0, 1).toDouble(),
                    ),
                    duration: AgendaMotion.duration(
                      context,
                      AgendaMotion.emphasized,
                    ),
                    curve: AgendaMotion.enterCurve,
                    builder: (context, progress, _) => LinearProgressIndicator(
                      value: progress,
                      minHeight: 4,
                      color: data.tone,
                      backgroundColor: t.graySoft,
                    ),
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  data.caption,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 10.5),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          AgendaAnimatedValue(
            value: data.value,
            builder: (context, value) => Text(
              value,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: data.tone,
                fontSize: 16,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MobileFinanceDetails extends StatelessWidget {
  const _MobileFinanceDetails({required this.overview, required this.profile});

  final _FinanceOverview overview;
  final AgendaBusinessProfile profile;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('finance-details-panel'),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(19),
      ),
      clipBehavior: Clip.antiAlias,
      child: Theme(
        data: Theme.of(
          context,
        ).copyWith(dividerColor: Colors.transparent, splashColor: t.accentSoft),
        child: ExpansionTile(
          key: const Key('finance-details-toggle'),
          tilePadding: const EdgeInsets.symmetric(horizontal: 15, vertical: 3),
          childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
          leading: Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(Icons.analytics_outlined, color: t.accent, size: 20),
          ),
          title: Text(
            'Análises detalhadas',
            style: TextStyle(
              color: t.ink,
              fontSize: 14,
              fontWeight: FontWeight.w700,
            ),
          ),
          subtitle: Text(
            'Projeção, riscos e composição do caixa',
            style: TextStyle(color: t.muted, fontSize: 11),
          ),
          iconColor: t.accent,
          collapsedIconColor: t.muted,
          children: [
            _ResultFormationCard(overview: overview),
            const SizedBox(height: 12),
            _NextThirtyDaysCard(
              overview: overview,
              compact: true,
              profile: profile,
            ),
            const SizedBox(height: 12),
            _RiskCard(overview: overview),
            const SizedBox(height: 12),
            _FunnelCard(overview: overview),
            const SizedBox(height: 12),
            _CompositionCard(overview: overview),
            const SizedBox(height: 12),
            _ForecastCard(overview: overview, compact: true),
          ],
        ),
      ),
    );
  }
}

class _ResultFormationCard extends StatelessWidget {
  const _ResultFormationCard({required this.overview});
  final _FinanceOverview overview;

  @override
  Widget build(BuildContext context) {
    final values = [
      overview.revenue,
      overview.fees,
      overview.materials,
      overview.stock,
      overview.commissions,
      overview.otherExpenses,
      overview.result,
    ];
    final labels = const [
      'Recebimentos',
      'Taxas\nlançadas',
      'Materiais',
      'Estoque',
      'Comissões',
      'Outras\ndespesas',
      'Resultado',
    ];
    final maxValue = math.max(
      1.0,
      values.map((value) => value.abs()).fold<double>(0, math.max),
    );
    return _Surface(
      key: const Key('finance-result-formation-card'),
      minHeight: 242,
      borderColor: _financeAccent,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _SectionHeader(
            title: 'Formação do resultado',
            subtitle:
                'Cada custo aparece uma vez; comissão já lançada não é descontada novamente.',
            badge: 'CASCATA',
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 116,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                for (var i = 0; i < values.length; i++)
                  Expanded(
                    key: ValueKey('finance-result-category-$i'),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 4),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          if (values[i].abs() > 0)
                            Text(
                              money(values[i]),
                              maxLines: 1,
                              style: const TextStyle(
                                color: _financeInk,
                                fontSize: 9,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          const SizedBox(height: 3),
                          TweenAnimationBuilder<double>(
                            tween: Tween(
                              begin: 0,
                              end: values[i].abs() / maxValue,
                            ),
                            duration: AgendaMotion.duration(
                              context,
                              AgendaMotion.emphasized,
                            ),
                            curve: AgendaMotion.enterCurve,
                            builder: (context, progress, _) => Container(
                              height: math.max(2, 54 * progress),
                              decoration: BoxDecoration(
                                color: i == values.length - 1
                                    ? _financeInk
                                    : i == 0
                                    ? _financeAccent
                                    : const Color(0xFFF1C2A8),
                                borderRadius: BorderRadius.circular(3),
                              ),
                            ),
                          ),
                          const SizedBox(height: 7),
                          Text(
                            labels[i],
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              color: i == values.length - 1
                                  ? _financeInk
                                  : _financeMuted,
                              fontSize: 9.2,
                              fontWeight: i == values.length - 1
                                  ? FontWeight.w700
                                  : FontWeight.w400,
                            ),
                          ),
                        ],
                      ),
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

class _NextThirtyDaysCard extends StatelessWidget {
  const _NextThirtyDaysCard({
    required this.overview,
    required this.compact,
    required this.profile,
  });
  final _FinanceOverview overview;
  final bool compact;
  final AgendaBusinessProfile profile;

  @override
  Widget build(BuildContext context) {
    final maxValue = math.max(1, overview.potential30);
    return Container(
      key: const Key('finance-next-30-days-card'),
      constraints: BoxConstraints(minHeight: compact ? 286 : 242),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        color: _financeInk,
        borderRadius: BorderRadius.circular(15),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _SectionHeader(
            title: 'Agenda dos próximos 30 dias',
            subtitle: 'Valores registrados, sem multiplicadores estimados.',
            badge: 'DADOS REAIS',
            dark: true,
          ),
          const SizedBox(height: 9),
          _ScenarioRow(
            label: 'Potencial',
            value: overview.potential30,
            progress: overview.potential30 / maxValue,
            caption: profile.activityCount(overview.futureCount),
            tone: _financeAccent,
          ),
          _ScenarioRow(
            label: 'Confirmado',
            value: overview.confirmed30,
            progress: overview.confirmed30 / maxValue,
            caption:
                '${_percent(overview.confirmed30, overview.potential30).round()}% potencial',
            tone: const Color(0xFFF6CAB3),
          ),
          _ScenarioRow(
            label: 'Já recebido',
            value: overview.received30,
            progress: overview.received30 / maxValue,
            caption: '${overview.paidFutureCount} pagos',
            tone: const Color(0xFF817B75),
          ),
          const SizedBox(height: 9),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 8),
            decoration: BoxDecoration(
              color: const Color(0xFF211F1D),
              border: Border.all(color: const Color(0xFF45413E)),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Despesas futuras registradas',
                        style: TextStyle(
                          color: Color(0xFF9D9791),
                          fontSize: 9.5,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        money(overview.futureExpenses),
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
                const Icon(
                  Icons.receipt_long_outlined,
                  color: _financeAccent,
                  size: 23,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ScenarioRow extends StatelessWidget {
  const _ScenarioRow({
    required this.label,
    required this.value,
    required this.progress,
    required this.caption,
    required this.tone,
  });
  final String label;
  final double value;
  final double progress;
  final String caption;
  final Color tone;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 50,
      child: Row(
        children: [
          SizedBox(
            width: 82,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 10,
                    height: 1,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                AgendaAnimatedValue(
                  value: money(value),
                  builder: (context, formatted) => Text(
                    formatted,
                    style: TextStyle(
                      color: tone,
                      fontSize: 12.5,
                      height: 1,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(2),
              child: TweenAnimationBuilder<double>(
                tween: Tween(begin: 0, end: progress.clamp(0, 1).toDouble()),
                duration: AgendaMotion.duration(
                  context,
                  AgendaMotion.emphasized,
                ),
                curve: AgendaMotion.enterCurve,
                builder: (context, animatedProgress, _) =>
                    LinearProgressIndicator(
                      value: animatedProgress,
                      minHeight: 3,
                      color: tone,
                      backgroundColor: const Color(0xFF45413E),
                    ),
              ),
            ),
          ),
          const SizedBox(width: 8),
          Container(
            width: 67,
            height: 27,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFF2C2926),
              borderRadius: BorderRadius.circular(9),
            ),
            child: Text(
              caption,
              maxLines: 1,
              style: const TextStyle(color: Color(0xFFE8E2DD), fontSize: 8.5),
            ),
          ),
        ],
      ),
    );
  }
}

class _RiskCard extends StatelessWidget {
  const _RiskCard({required this.overview});
  final _FinanceOverview overview;

  @override
  Widget build(BuildContext context) {
    final risks = <(String, String)>[
      (
        'Últimos\n90 dias',
        overview.last90DaysRevenue > 0
            ? money(overview.last90DaysRevenue)
            : '—',
      ),
      (
        'Comissões',
        overview.commissions > 0 ? money(overview.commissions) : '—',
      ),
      (
        'Agenda ociosa',
        overview.idleRate == null ? '—' : '${overview.idleRate!.round()}%',
      ),
      ('Contas\nvencidas', overview.overdueCount.toString()),
      ('Inadimplência', '${overview.defaultRate.round()}%'),
      ('Materiais', overview.materials > 0 ? money(overview.materials) : '—'),
      ('Cancelamentos', '${overview.cancelRate.round()}%'),
      ('Margem líquida', '${overview.margin.round()}%'),
      ('Caixa futuro', money(overview.potential30 - overview.futureExpenses)),
    ];
    Widget riskCell((String, String) risk) => Container(
      key: ValueKey('finance-risk-${risk.$1.replaceAll('\n', '-')}'),
      height: 50,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: _financeSoft,
        border: Border.all(color: Colors.white, width: 2),
        borderRadius: BorderRadius.circular(7),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(
            risk.$1,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Color(0xFF74452D),
              fontSize: 9.2,
              height: 1,
              fontWeight: FontWeight.w600,
            ),
          ),
          Text(
            risk.$2,
            maxLines: 1,
            style: const TextStyle(
              color: Color(0xFFA44218),
              fontSize: 11,
              height: 1,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
    return _Surface(
      key: const Key('finance-risk-card'),
      minHeight: 234,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _SectionHeader(
            title: 'Sinais de risco observados',
            subtitle:
                'Métricas dos registros da conta; — indica base insuficiente.',
          ),
          const SizedBox(height: 10),
          for (var row = 0; row < 3; row++) ...[
            if (row > 0) const SizedBox(height: 4),
            Row(
              children: [
                for (var column = 0; column < 3; column++) ...[
                  if (column > 0) const SizedBox(width: 4),
                  Expanded(child: riskCell(risks[row * 3 + column])),
                ],
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _FunnelCard extends StatelessWidget {
  const _FunnelCard({required this.overview});
  final _FinanceOverview overview;

  @override
  Widget build(BuildContext context) {
    final stages = <(String, int, Color)>[
      ('AGENDADO', overview.scheduledCount, const Color(0xFFF5B58F)),
      ('CONFIRMADO', overview.confirmedCount, const Color(0xFFEF8B56)),
      ('REALIZADO', overview.doneCount, _financeAccent),
      ('RECEBIDO', overview.receivedCount, const Color(0xFFB74212)),
    ];
    final maximum = math.max(
      1,
      stages.map((stage) => stage.$2).fold<int>(0, math.max),
    );
    return _Surface(
      key: const Key('finance-receipt-funnel-card'),
      minHeight: 234,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _SectionHeader(
            title: 'Funil de recebimento',
            subtitle: 'Coorte de atendimentos do mês, sem ajuste matemático.',
          ),
          const SizedBox(height: 8),
          for (var index = 0; index < stages.length; index++)
            Align(
              child: Container(
                width: double.infinity,
                height: 27,
                margin: EdgeInsets.symmetric(horizontal: index * 15.0),
                padding: const EdgeInsets.symmetric(horizontal: 10),
                decoration: BoxDecoration(
                  color: stages[index].$3.withValues(
                    alpha: stages[index].$2 == 0 ? .35 : 1,
                  ),
                  borderRadius: BorderRadius.circular(2),
                ),
                child: Row(
                  children: [
                    Text(
                      stages[index].$1,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 8.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const Spacer(),
                    Text(
                      '${stages[index].$2} • ${_percent(stages[index].$2.toDouble(), maximum.toDouble()).round()}%',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 9,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          const SizedBox(height: 8),
          Text(
            overview.scheduledCount == 0
                ? 'Sem atendimentos no período selecionado.'
                : 'Conversão agendado → recebido: ${_percent(overview.receivedCount.toDouble(), overview.scheduledCount.toDouble()).round()}%',
            style: const TextStyle(
              color: _financeMuted,
              fontSize: 9.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class _CompositionCard extends StatelessWidget {
  const _CompositionCard({required this.overview});
  final _FinanceOverview overview;

  @override
  Widget build(BuildContext context) {
    final values = [
      ('Atendimentos', overview.serviceRevenue, _financeAccent),
      (
        'Contas recebidas',
        overview.receivablesRevenue,
        const Color(0xFFD95A18),
      ),
      ('Produtos', overview.productRevenue, const Color(0xFFA23F12)),
      ('Avulsos', overview.manualRevenue, const Color(0xFF74300F)),
    ];
    return _Surface(
      key: const Key('finance-receipt-composition-card'),
      minHeight: 234,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _SectionHeader(
            title: 'Composição dos recebimentos',
            subtitle: 'Origem real do valor recebido no mês.',
          ),
          const SizedBox(height: 9),
          for (var index = 0; index < values.length; index++)
            Align(
              child: Container(
                width: double.infinity,
                height: 30,
                margin: EdgeInsets.symmetric(horizontal: index * 14.0),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: values[index].$3.withValues(
                    alpha: values[index].$2 == 0 ? .3 : 1,
                  ),
                  borderRadius: BorderRadius.circular(2),
                ),
                child: Text(
                  '${money(values[index].$2)}  ${values[index].$1}',
                  maxLines: 1,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 9.2,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ),
          const SizedBox(height: 8),
          Text(
            'Total recebido: ${money(overview.revenue)}',
            textAlign: TextAlign.right,
            style: const TextStyle(
              color: _financeInk,
              fontSize: 10,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _ForecastCard extends StatelessWidget {
  const _ForecastCard({required this.overview, required this.compact});
  final _FinanceOverview overview;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return _Surface(
      key: const Key('finance-forecast-card'),
      minHeight: compact ? 314 : 230,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _SectionHeader(
            title: 'Projeção do resultado — 12 semanas',
            subtitle:
                'Faixa entre agenda total e confirmada, usando apenas despesas registradas.',
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 125,
            child: TweenAnimationBuilder<double>(
              tween: Tween(begin: 0, end: 1),
              duration: AgendaMotion.duration(context, AgendaMotion.emphasized),
              curve: AgendaMotion.enterCurve,
              builder: (context, progress, _) => CustomPaint(
                painter: _ForecastPainter(
                  potential: overview.forecastPotential,
                  confirmed: overview.forecastConfirmed,
                  expenses: overview.forecastExpenses,
                  progress: progress,
                ),
              ),
            ),
          ),
          const SizedBox(height: 8),
          const Wrap(
            spacing: 12,
            runSpacing: 7,
            children: [
              _Legend(tone: _financeAccent, label: 'Agenda total'),
              _Legend(tone: _financeInk, label: 'Despesas acumuladas'),
              _Legend(tone: Color(0xFFF2D8CA), label: 'Faixa confirmada'),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'Resultado potencial: ${money(overview.forecastPotential.last)}',
            textAlign: TextAlign.right,
            style: const TextStyle(
              color: _financeInk,
              fontSize: 10.5,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _ForecastPainter extends CustomPainter {
  const _ForecastPainter({
    required this.potential,
    required this.confirmed,
    required this.expenses,
    required this.progress,
  });
  final List<double> potential;
  final List<double> confirmed;
  final List<double> expenses;
  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    if (potential.length != 12) return;
    const left = 34.0;
    const bottom = 19.0;
    final width = size.width - left - 4;
    final height = size.height - bottom - 4;
    final maximum = math.max(
      1.0,
      [
        ...potential,
        ...confirmed,
        ...expenses,
      ].map((value) => value.abs()).fold<double>(0, math.max),
    );
    final grid = Paint()
      ..color = _financeLine
      ..strokeWidth = 1;
    for (var row = 0; row < 4; row++) {
      final y = height * row / 3;
      canvas.drawLine(Offset(left, y), Offset(size.width, y), grid);
    }
    Offset point(List<double> values, int index) => Offset(
      left + width * index / 11,
      height - (values[index] / maximum).clamp(-1, 1) * height * progress,
    );
    final band = Path()..moveTo(point(potential, 0).dx, point(potential, 0).dy);
    for (var i = 1; i < 12; i++) {
      band.lineTo(point(potential, i).dx, point(potential, i).dy);
    }
    for (var i = 11; i >= 0; i--) {
      band.lineTo(point(confirmed, i).dx, point(confirmed, i).dy);
    }
    band.close();
    canvas.drawPath(band, Paint()..color = const Color(0xFFF2D8CA));
    void drawLine(List<double> values, Color color, double stroke) {
      final path = Path()..moveTo(point(values, 0).dx, point(values, 0).dy);
      for (var i = 1; i < 12; i++) {
        path.lineTo(point(values, i).dx, point(values, i).dy);
      }
      canvas.drawPath(
        path,
        Paint()
          ..color = color
          ..style = PaintingStyle.stroke
          ..strokeWidth = stroke
          ..strokeCap = StrokeCap.round,
      );
    }

    drawLine(potential, _financeAccent, 2.5);
    drawLine(expenses, _financeInk, 2);
    for (var i = 0; i < 12; i++) {
      final label = TextPainter(
        text: TextSpan(
          text: 'S${i + 1}',
          style: const TextStyle(color: _financeMuted, fontSize: 8.5),
        ),
        textDirection: ui.TextDirection.ltr,
      )..layout();
      label.paint(
        canvas,
        Offset(point(potential, i).dx - label.width / 2, size.height - 13),
      );
    }
  }

  @override
  bool shouldRepaint(covariant _ForecastPainter oldDelegate) =>
      oldDelegate.potential != potential ||
      oldDelegate.confirmed != confirmed ||
      oldDelegate.expenses != expenses ||
      oldDelegate.progress != progress;
}

class _Legend extends StatelessWidget {
  const _Legend({required this.tone, required this.label});
  final Color tone;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Container(width: 20, height: 4, color: tone),
      const SizedBox(width: 5),
      Text(label, style: const TextStyle(color: _financeMuted, fontSize: 9.5)),
    ],
  );
}

class _QuickOperationsCard extends StatelessWidget {
  const _QuickOperationsCard({
    required this.overview,
    required this.onReceive,
    required this.onExpense,
    required this.onProduct,
  });
  final _FinanceOverview overview;
  final VoidCallback onReceive;
  final VoidCallback onExpense;
  final VoidCallback onProduct;

  @override
  Widget build(BuildContext context) {
    return _Surface(
      key: const Key('finance-quick-operations-card'),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          const Padding(
            padding: EdgeInsets.only(right: 8),
            child: Text(
              'Operações rápidas',
              style: TextStyle(
                color: _financeInk,
                fontSize: 14,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          _ToolbarButton(
            key: const Key('finance-quick-receive'),
            icon: Icons.account_balance_wallet_outlined,
            label: 'Lançar entrada',
            primary: true,
            onPressed: onReceive,
          ),
          _ToolbarButton(
            key: const Key('finance-quick-expense'),
            icon: Icons.receipt_long_outlined,
            label: 'Lançar despesa',
            onPressed: onExpense,
          ),
          _ToolbarButton(
            key: const Key('finance-quick-product'),
            icon: Icons.shopping_bag_outlined,
            label: 'Vender produto',
            onPressed: onProduct,
          ),
          Text(
            '${overview.pendingCount} cobrança(s) pendente(s)',
            style: const TextStyle(color: _financeMuted, fontSize: 10.5),
          ),
        ],
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({
    required this.title,
    required this.subtitle,
    this.badge,
    this.dark = false,
  });
  final String title;
  final String subtitle;
  final String? badge;
  final bool dark;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(
                  color: dark ? Colors.white : _financeInk,
                  fontSize: 15.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                subtitle,
                style: TextStyle(
                  color: dark ? const Color(0xFF9D9791) : _financeMuted,
                  fontSize: 10.2,
                ),
              ),
            ],
          ),
        ),
        if (badge != null)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: dark ? const Color(0xFF302D2A) : const Color(0xFFFFF1E9),
              borderRadius: BorderRadius.circular(9),
            ),
            child: Text(
              badge!,
              style: const TextStyle(
                color: _financeAccentDark,
                fontSize: 9,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
      ],
    );
  }
}

class _Surface extends StatelessWidget {
  const _Surface({
    super.key,
    required this.child,
    this.minHeight,
    this.padding = const EdgeInsets.all(14),
    this.borderColor = _financeLine,
    this.color = Colors.white,
  });
  final Widget child;
  final double? minHeight;
  final EdgeInsets padding;
  final Color borderColor;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: minHeight == null
          ? null
          : BoxConstraints(minHeight: minHeight!),
      padding: padding,
      decoration: BoxDecoration(
        color: color,
        border: Border.all(color: borderColor),
        borderRadius: BorderRadius.circular(19),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0B000000),
            blurRadius: 12,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: child,
    );
  }
}

class _FinanceOverview {
  const _FinanceOverview({
    required this.revenue,
    required this.previousRevenue,
    required this.revenueGoal,
    required this.serviceRevenue,
    required this.receivablesRevenue,
    required this.productRevenue,
    required this.manualRevenue,
    required this.expenses,
    required this.commissions,
    required this.registeredCommissions,
    required this.pending,
    required this.pendingCount,
    required this.fees,
    required this.materials,
    required this.stock,
    required this.otherExpenses,
    required this.futureCount,
    required this.paidFutureCount,
    required this.potential30,
    required this.confirmed30,
    required this.received30,
    required this.futureExpenses,
    required this.last90DaysRevenue,
    required this.idleRate,
    required this.defaultRate,
    required this.overdueCount,
    required this.cancelRate,
    required this.scheduledCount,
    required this.confirmedCount,
    required this.doneCount,
    required this.receivedCount,
    required this.forecastPotential,
    required this.forecastConfirmed,
    required this.forecastExpenses,
  });

  final double revenue;
  final double previousRevenue;
  final double? revenueGoal;
  final double serviceRevenue;
  final double receivablesRevenue;
  final double productRevenue;
  final double manualRevenue;
  final double expenses;
  final double commissions;
  final double registeredCommissions;
  final double pending;
  final int pendingCount;
  final double fees;
  final double materials;
  final double stock;
  final double otherExpenses;
  final int futureCount;
  final int paidFutureCount;
  final double potential30;
  final double confirmed30;
  final double received30;
  final double futureExpenses;
  final double last90DaysRevenue;
  final double? idleRate;
  final double defaultRate;
  final int overdueCount;
  final double cancelRate;
  final int scheduledCount;
  final int confirmedCount;
  final int doneCount;
  final int receivedCount;
  final List<double> forecastPotential;
  final List<double> forecastConfirmed;
  final List<double> forecastExpenses;

  double get unregisteredCommissions =>
      math.max(0, commissions - registeredCommissions);
  double get result => revenue - expenses - unregisteredCommissions;
  double get expenseShare => _percent(expenses, revenue);
  double get commissionShare => _percent(commissions, revenue);
  double get margin => _percent(result, revenue);
  double get revenueGrowth => previousRevenue == 0
      ? (revenue == 0 ? 0 : 100)
      : (revenue / previousRevenue - 1) * 100;

  factory _FinanceOverview.from(AgendaData data, DateTime selectedMonth) {
    final start = DateTime(selectedMonth.year, selectedMonth.month);
    final end = DateTime(selectedMonth.year, selectedMonth.month + 1);
    final previousStart = DateTime(selectedMonth.year, selectedMonth.month - 1);
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final futureEnd = today.add(const Duration(days: 30));
    final servicesById = {for (final item in data.services) item.id: item};
    final professionalsById = {
      for (final item in data.professionals) item.id: item,
    };
    final receivableAppointmentIds = data.customerReceivables
        .where((item) => item.status.trim().toLowerCase() != 'cancelled')
        .map((item) => item.appointmentId.trim().toLowerCase())
        .where((id) => id.isNotEmpty)
        .toSet();

    double appointmentRevenueBetween(DateTime from, DateTime to) => data
        .appointments
        .where((item) {
          final paidAt = item.paymentConfirmedAt;
          return paidAt != null &&
              _between(paidAt, from, to) &&
              !receivableAppointmentIds.contains(item.id.toLowerCase());
        })
        .fold(0, (sum, item) => sum + item.price);
    double receivableRevenueBetween(DateTime from, DateTime to) => data
        .customerReceivables
        .where((item) {
          final paidAt = item.paidAt;
          return item.status.trim().toLowerCase() == 'paid' &&
              paidAt != null &&
              _between(paidAt, from, to);
        })
        .fold(0, (sum, item) => sum + item.originalValue);
    double productRevenueBetween(DateTime from, DateTime to) => data
        .productSales
        .where((item) => _between(item.soldAt, from, to))
        .fold(0, (sum, item) => sum + item.total);
    double manualRevenueBetween(DateTime from, DateTime to) => data
        .manualPayments
        .where((item) => _between(item.paidAt, from, to))
        .fold(0, (sum, item) => sum + item.value);
    double revenueBetween(DateTime from, DateTime to) =>
        appointmentRevenueBetween(from, to) +
        receivableRevenueBetween(from, to) +
        productRevenueBetween(from, to) +
        manualRevenueBetween(from, to);

    final serviceRevenue = appointmentRevenueBetween(start, end);
    final receivablesRevenue = receivableRevenueBetween(start, end);
    final productRevenue = productRevenueBetween(start, end);
    final manualRevenue = manualRevenueBetween(start, end);
    final revenue =
        serviceRevenue + receivablesRevenue + productRevenue + manualRevenue;
    final selectedExpenses = data.expenses
        .where((item) => _between(item.date, start, end))
        .toList();
    final expenses = selectedExpenses.fold<double>(
      0,
      (sum, item) => sum + item.value,
    );
    double expenseByKind(_ExpenseKind kind) => selectedExpenses
        .where((item) => _expenseKind(item) == kind)
        .fold<double>(0, (sum, item) => sum + item.value);
    final fees = expenseByKind(_ExpenseKind.fee);
    final materials = expenseByKind(_ExpenseKind.material);
    final stock = expenseByKind(_ExpenseKind.stock);
    final registeredCommissions = expenseByKind(_ExpenseKind.commission);
    final otherExpenses =
        expenses - fees - materials - stock - registeredCommissions;
    double commissionFor(Appointment item) {
      final servicePercent =
          servicesById[item.serviceId]?.commissionPercent ?? 0;
      final professionalPercent =
          professionalsById[item.professionalId]?.commissionPercent ?? 0;
      final percent = servicePercent > 0 ? servicePercent : professionalPercent;
      return item.price * percent.clamp(0, 100) / 100;
    }

    final paidAppointments = data.appointments.where((item) {
      final paidAt = item.paymentConfirmedAt;
      return paidAt != null && _between(paidAt, start, end);
    });
    final commissions = paidAppointments.fold<double>(
      0,
      (sum, item) => sum + commissionFor(item),
    );
    final periodCutoff = now.isBefore(end) ? now : end;
    final activeReceivables = data.customerReceivables.where(
      (item) => !_receivableSettled(item.status),
    );
    final activeReceivableAppointmentIds = activeReceivables
        .map((item) => item.appointmentId.trim().toLowerCase())
        .where((id) => id.isNotEmpty)
        .toSet();
    final pendingAppointments = data.appointments.where(
      (item) =>
          item.price > 0 &&
          item.paymentConfirmedAt == null &&
          _between(item.start, start, end) &&
          item.start.isBefore(periodCutoff) &&
          _paymentIsDue(item.status) &&
          !activeReceivableAppointmentIds.contains(item.id.toLowerCase()),
    );
    final pendingReceivables = activeReceivables.where((item) {
      final dueAt = item.dueAt;
      return dueAt != null && _between(dueAt, start, end);
    });
    final overdueAppointments = pendingAppointments.where(
      (item) => item.start.isBefore(today),
    );
    final overdueReceivables = pendingReceivables.where(
      (item) => item.dueAt!.isBefore(today),
    );
    final pendingAmount =
        pendingAppointments.fold<double>(0, (sum, item) => sum + item.price) +
        pendingReceivables.fold<double>(
          0,
          (sum, item) => sum + math.max(0, item.remainingValue),
        );
    final pendingCount = pendingAppointments.length + pendingReceivables.length;
    final overdueCount = overdueAppointments.length + overdueReceivables.length;
    final future = data.appointments
        .where(
          (item) =>
              !_ignored(item.status) &&
              !item.start.isBefore(today) &&
              item.start.isBefore(futureEnd),
        )
        .toList();
    final confirmedStatuses = {
      AppointmentStatus.confirmed,
      AppointmentStatus.waiting,
      AppointmentStatus.inService,
      AppointmentStatus.done,
    };
    final monthAppointments = data.appointments
        .where(
          (item) => _between(item.start, start, end) && !_ignored(item.status),
        )
        .toList();
    final allMonthAppointments = data.appointments
        .where((item) => _between(item.start, start, end))
        .toList();
    final confirmedCount = monthAppointments
        .where((item) => confirmedStatuses.contains(item.status))
        .length;
    final doneCount = monthAppointments
        .where((item) => item.status == AppointmentStatus.done)
        .length;
    final receivedCount = monthAppointments
        .where((item) => item.paymentConfirmedAt != null)
        .length;
    final cancelledCount = allMonthAppointments
        .where(
          (item) =>
              item.status == AppointmentStatus.cancelled ||
              item.status == AppointmentStatus.noShow,
        )
        .length;
    final receivableIdsDueInPeriod = data.customerReceivables
        .where((item) {
          final dueAt = item.dueAt;
          return dueAt != null &&
              dueAt.isBefore(today) &&
              _between(dueAt, start, end);
        })
        .map((item) => item.appointmentId.trim().toLowerCase())
        .where((id) => id.isNotEmpty)
        .toSet();
    final maturedDirectPayments = data.appointments.where(
      (item) =>
          item.price > 0 &&
          _between(item.start, start, end) &&
          item.start.isBefore(today) &&
          _paymentIsDue(item.status) &&
          !receivableIdsDueInPeriod.contains(item.id.toLowerCase()),
    );
    final maturedReceivables = data.customerReceivables.where((item) {
      final dueAt = item.dueAt;
      return dueAt != null &&
          dueAt.isBefore(today) &&
          _between(dueAt, start, end) &&
          item.status.trim().toLowerCase() != 'cancelled';
    });
    final maturedPaymentCount =
        maturedDirectPayments.length + maturedReceivables.length;
    final idleRate = _configuredIdleRate(
      data: data,
      start: start,
      end: end,
      appointments: monthAppointments,
    );

    final forecastPotential = <double>[];
    final forecastConfirmed = <double>[];
    final forecastExpenses = <double>[];
    var potentialBalance =
        revenue - expenses - math.max(0, commissions - registeredCommissions);
    var confirmedBalance = potentialBalance;
    var cumulativeExpenses = 0.0;
    for (var week = 0; week < 12; week++) {
      final weekStart = today.add(Duration(days: week * 7));
      final weekEnd = weekStart.add(const Duration(days: 7));
      final weeklyAppointments = data.appointments.where(
        (item) =>
            !_ignored(item.status) && _between(item.start, weekStart, weekEnd),
      );
      final weeklyPotential = weeklyAppointments.fold<double>(
        0,
        (sum, item) => sum + item.price,
      );
      final weeklyConfirmed = weeklyAppointments
          .where((item) => confirmedStatuses.contains(item.status))
          .fold<double>(0, (sum, item) => sum + item.price);
      final weeklyExpenses = data.expenses
          .where((item) => _between(item.date, weekStart, weekEnd))
          .toList();
      final weeklyExpenseTotal = weeklyExpenses.fold<double>(
        0,
        (sum, item) => sum + item.value,
      );
      final weeklyRegisteredCommissions = weeklyExpenses
          .where((item) => _expenseKind(item) == _ExpenseKind.commission)
          .fold<double>(0, (sum, item) => sum + item.value);
      final weeklyCommissionProvision = math.max(
        0,
        weeklyAppointments.fold<double>(
              0,
              (sum, item) => sum + commissionFor(item),
            ) -
            weeklyRegisteredCommissions,
      );
      final weeklyCosts = weeklyExpenseTotal + weeklyCommissionProvision;
      cumulativeExpenses += weeklyCosts;
      potentialBalance += weeklyPotential - weeklyCosts;
      confirmedBalance += weeklyConfirmed - weeklyCosts;
      forecastPotential.add(potentialBalance);
      forecastConfirmed.add(confirmedBalance);
      forecastExpenses.add(cumulativeExpenses);
    }

    return _FinanceOverview(
      revenue: revenue,
      previousRevenue: revenueBetween(previousStart, start),
      revenueGoal: data.settings.monthlyRevenueGoal > 0
          ? data.settings.monthlyRevenueGoal
          : null,
      serviceRevenue: serviceRevenue,
      receivablesRevenue: receivablesRevenue,
      productRevenue: productRevenue,
      manualRevenue: manualRevenue,
      expenses: expenses,
      commissions: commissions,
      registeredCommissions: registeredCommissions,
      pending: pendingAmount,
      pendingCount: pendingCount,
      fees: fees,
      materials: materials,
      stock: stock,
      otherExpenses: math.max(0, otherExpenses),
      futureCount: future.length,
      paidFutureCount: future
          .where((item) => item.paymentConfirmedAt != null)
          .length,
      potential30: future.fold(0, (sum, item) => sum + item.price),
      confirmed30: future
          .where((item) => confirmedStatuses.contains(item.status))
          .fold(0, (sum, item) => sum + item.price),
      received30: future
          .where((item) => item.paymentConfirmedAt != null)
          .fold(0, (sum, item) => sum + item.price),
      futureExpenses: data.expenses
          .where(
            (item) =>
                !item.date.isBefore(today) && item.date.isBefore(futureEnd),
          )
          .fold(0, (sum, item) => sum + item.value),
      last90DaysRevenue: revenueBetween(
        today.subtract(const Duration(days: 90)),
        today.add(const Duration(days: 1)),
      ),
      idleRate: idleRate,
      defaultRate: _percent(
        overdueCount.toDouble(),
        maturedPaymentCount.toDouble(),
      ),
      overdueCount: overdueCount,
      cancelRate: _percent(
        cancelledCount.toDouble(),
        math.max(1, allMonthAppointments.length).toDouble(),
      ),
      scheduledCount: monthAppointments.length,
      confirmedCount: confirmedCount,
      doneCount: doneCount,
      receivedCount: receivedCount,
      forecastPotential: forecastPotential,
      forecastConfirmed: forecastConfirmed,
      forecastExpenses: forecastExpenses,
    );
  }
}

enum _ExpenseKind { fee, material, stock, commission, other }

_ExpenseKind _expenseKind(ExpenseItem item) {
  final searchable = _normalizeFinanceText(
    '${item.category} ${item.description} ${item.notes}',
  );
  if (searchable.contains('comiss')) return _ExpenseKind.commission;
  if (searchable.contains('taxa') ||
      searchable.contains('tarifa') ||
      searchable.contains('imposto') ||
      searchable.contains('tribut')) {
    return _ExpenseKind.fee;
  }
  if (searchable.contains('materia') ||
      searchable.contains('insumo') ||
      searchable.contains('descartavel')) {
    return _ExpenseKind.material;
  }
  if (searchable.contains('estoque') ||
      searchable.contains('mercadoria') ||
      searchable.contains('revenda')) {
    return _ExpenseKind.stock;
  }
  return _ExpenseKind.other;
}

String _normalizeFinanceText(String value) => value
    .trim()
    .toLowerCase()
    .replaceAll(RegExp('[áàâãä]'), 'a')
    .replaceAll(RegExp('[éèêë]'), 'e')
    .replaceAll(RegExp('[íìîï]'), 'i')
    .replaceAll(RegExp('[óòôõö]'), 'o')
    .replaceAll(RegExp('[úùûü]'), 'u')
    .replaceAll('ç', 'c');

bool _receivableSettled(String status) => const {
  'paid',
  'settled',
  'closed',
  'cancelled',
  'canceled',
  'refunded',
  'void',
  'quitado',
}.contains(_normalizeFinanceText(status));

bool _paymentIsDue(AppointmentStatus status) => const {
  AppointmentStatus.waiting,
  AppointmentStatus.inService,
  AppointmentStatus.done,
}.contains(status);

double? _configuredIdleRate({
  required AgendaData data,
  required DateTime start,
  required DateTime end,
  required Iterable<Appointment> appointments,
}) {
  final settings = data.settings;
  final activeProfessionals = data.professionals
      .where((item) => item.isActive)
      .length;
  final workdays = settings.workdays.toSet();
  final grossMinutes =
      (settings.workdayEndHour - settings.workdayStartHour) * 60;
  if (activeProfessionals == 0 || workdays.isEmpty || grossMinutes <= 0) {
    return null;
  }

  var breakMinutes = 0;
  if (settings.workdayBreakEnabled) {
    final breakStart = math.max(
      settings.workdayStartHour,
      settings.workdayBreakStartHour,
    );
    final breakEnd = math.min(
      settings.workdayEndHour,
      settings.workdayBreakEndHour,
    );
    breakMinutes = math.max(0, breakEnd - breakStart) * 60;
  }
  final dailyMinutes = grossMinutes - breakMinutes;
  if (dailyMinutes <= 0) return null;

  var configuredDays = 0;
  for (
    var day = DateUtils.dateOnly(start);
    day.isBefore(end);
    day = day.add(const Duration(days: 1))
  ) {
    if (workdays.contains(day.weekday)) configuredDays++;
  }
  final capacityMinutes = configuredDays * dailyMinutes * activeProfessionals;
  if (capacityMinutes <= 0) return null;

  final bookedMinutes = appointments.fold<int>(
    0,
    (sum, item) => sum + item.durationMinutes.clamp(5, 1440),
  );
  final occupancy = (bookedMinutes / capacityMinutes).clamp(0, 1);
  return (1 - occupancy) * 100;
}

bool _ignored(AppointmentStatus status) => const {
  AppointmentStatus.cancelled,
  AppointmentStatus.noShow,
  AppointmentStatus.blocked,
}.contains(status);

bool _between(DateTime value, DateTime start, DateTime end) =>
    !value.isBefore(start) && value.isBefore(end);

double _percent(double value, double total) =>
    total == 0 ? 0 : value / total * 100;

bool _isCurrentMonth(DateTime value) {
  final now = DateTime.now();
  return value.year == now.year && value.month == now.month;
}

String _monthLabel(DateTime value) {
  final formatted = DateFormat('MMMM \'de\' yyyy', 'pt_BR').format(value);
  return formatted[0].toUpperCase() + formatted.substring(1);
}
