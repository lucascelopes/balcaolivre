import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:intl/intl.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/business_profile.dart';
import '../../core/formatters.dart';
import '../../core/motion.dart';
import '../../domain/models/models.dart';

enum ReportsMobilePeriod { day, week, month }

enum _ReportsTrendMetric { revenue, bookings, completed }

class ReportsMobileOptionOne extends StatefulWidget {
  const ReportsMobileOptionOne({
    super.key,
    required this.controller,
    required this.period,
    required this.onPeriodChanged,
    required this.onCopy,
    required this.onExport,
    required this.exporting,
    required this.legacyGoalText,
  });

  final AgendaController controller;
  final ReportsMobilePeriod period;
  final ValueChanged<ReportsMobilePeriod> onPeriodChanged;
  final VoidCallback onCopy;
  final VoidCallback onExport;
  final bool exporting;
  final String legacyGoalText;

  @override
  State<ReportsMobileOptionOne> createState() => _ReportsMobileOptionOneState();
}

class _ReportsMobileOptionOneState extends State<ReportsMobileOptionOne> {
  _ReportsTrendMetric _trendMetric = _ReportsTrendMetric.revenue;

  @override
  Widget build(BuildContext context) {
    final profile = AgendaBusinessProfile.fromSettings(
      widget.controller.data.settings,
    );
    final snapshot = _MobileDiagnosticSnapshot.from(
      controller: widget.controller,
      period: widget.period,
      profile: profile,
    );

    return SingleChildScrollView(
      key: const Key('reports-mobile-diagnostics'),
      padding: EdgeInsets.fromLTRB(
        MediaQuery.sizeOf(context).width <= 340 ? 12 : 16,
        12,
        MediaQuery.sizeOf(context).width <= 340 ? 12 : 16,
        24,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox.shrink(
            child: Text(
              'Diagnóstico e Ações',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
          AgendaReveal(child: _periodToolbar(context, snapshot)),
          const SizedBox(height: 21),
          AgendaReveal(
            delay: const Duration(milliseconds: 35),
            child: _diagnosisHero(context, snapshot),
          ),
          const SizedBox(height: 22),
          AgendaReveal(
            delay: const Duration(milliseconds: 70),
            child: _funnel(context, snapshot),
          ),
          const SizedBox(height: 20),
          AgendaReveal(
            delay: const Duration(milliseconds: 105),
            child: _trend(context, snapshot),
          ),
          const SizedBox(height: 18),
          AgendaReveal(
            delay: const Duration(milliseconds: 140),
            child: _recommendedAction(context, snapshot),
          ),
          const SizedBox(height: 18),
          AgendaReveal(
            delay: const Duration(milliseconds: 175),
            child: _goal(context, snapshot),
          ),
          const SizedBox(height: 14),
          AgendaReveal(
            delay: const Duration(milliseconds: 210),
            child: _whyItMatters(context, snapshot),
          ),
          const SizedBox(height: 12),
          TextButton.icon(
            key: const Key('reports-mobile-export-bottom'),
            onPressed: widget.exporting ? null : widget.onExport,
            icon: widget.exporting
                ? const SizedBox.square(
                    dimension: 15,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const FaIcon(FontAwesomeIcons.fileArrowDown, size: 15),
            label: const Text('Exportar relatório em PDF'),
          ),
        ],
      ),
    );
  }

  Widget _periodToolbar(
    BuildContext context,
    _MobileDiagnosticSnapshot snapshot,
  ) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        SizedBox(
          height: 46,
          child: OutlinedButton.icon(
            key: const Key('reports-mobile-period'),
            onPressed: () => _choosePeriod(context),
            icon: const FaIcon(FontAwesomeIcons.calendarDays, size: 16),
            label: Text(snapshot.periodLabel),
            style: OutlinedButton.styleFrom(
              foregroundColor: t.ink,
              backgroundColor: t.panel,
              side: BorderSide(color: t.line),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
              ),
              padding: const EdgeInsets.symmetric(horizontal: 13),
              textStyle: const TextStyle(
                fontFamily: 'Segoe UI',
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            snapshot.referenceDateLabel,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: t.muted,
              fontSize: 12,
              height: 1.15,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
        const SizedBox(width: 6),
        IconButton(
          key: const Key('reports-mobile-copy-top'),
          onPressed: widget.onCopy,
          tooltip: 'Compartilhar resumo',
          style: IconButton.styleFrom(
            foregroundColor: t.accent,
            backgroundColor: t.accentSoft,
            minimumSize: const Size(46, 46),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
            ),
          ),
          icon: const FaIcon(FontAwesomeIcons.shareNodes, size: 18),
        ),
        const SizedBox.shrink(child: Text('Copiar resumo')),
      ],
    );
  }

  Future<void> _choosePeriod(BuildContext context) async {
    final t = AgendaThemeTokens.of(context);
    final selected = await showModalBottomSheet<ReportsMobilePeriod>(
      context: context,
      useSafeArea: true,
      showDragHandle: true,
      backgroundColor: t.panel,
      builder: (context) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Período do relatório',
              style: TextStyle(
                color: t.ink,
                fontSize: 19,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            _periodOption(
              context,
              ReportsMobilePeriod.day,
              'Dia selecionado',
              'Veja um dia específico',
            ),
            _periodOption(
              context,
              ReportsMobilePeriod.week,
              'Semana selecionada',
              'Compare o ritmo da semana',
            ),
            _periodOption(
              context,
              ReportsMobilePeriod.month,
              'Mês selecionado',
              'Analise o mês completo',
            ),
          ],
        ),
      ),
    );
    if (selected != null && selected != widget.period) {
      widget.onPeriodChanged(selected);
    }
  }

  Widget _periodOption(
    BuildContext context,
    ReportsMobilePeriod period,
    String title,
    String subtitle,
  ) {
    final t = AgendaThemeTokens.of(context);
    final selected = widget.period == period;
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 4),
      onTap: () => Navigator.pop(context, period),
      leading: Container(
        width: 38,
        height: 38,
        decoration: BoxDecoration(
          color: selected ? t.accentSoft : t.graySoft,
          borderRadius: BorderRadius.circular(12),
        ),
        alignment: Alignment.center,
        child: FaIcon(
          period == ReportsMobilePeriod.day
              ? FontAwesomeIcons.calendarDay
              : period == ReportsMobilePeriod.week
              ? FontAwesomeIcons.calendarWeek
              : FontAwesomeIcons.calendar,
          color: selected ? t.accent : t.muted,
          size: 16,
        ),
      ),
      title: Text(
        title,
        style: TextStyle(
          color: t.ink,
          fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
        ),
      ),
      subtitle: Text(subtitle),
      trailing: selected
          ? FaIcon(FontAwesomeIcons.circleCheck, color: t.accent, size: 18)
          : null,
    );
  }

  Widget _diagnosisHero(
    BuildContext context,
    _MobileDiagnosticSnapshot snapshot,
  ) {
    final t = AgendaThemeTokens.of(context);
    final diagnosis = snapshot.diagnosis;
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth <= 320;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'DIAGNÓSTICO PRINCIPAL',
              style: TextStyle(
                color: t.accent,
                fontSize: 10,
                fontWeight: FontWeight.w800,
                letterSpacing: .45,
              ),
            ),
            const SizedBox(height: 8),
            Text.rich(
              TextSpan(
                style: TextStyle(
                  color: t.ink,
                  fontFamily: 'Segoe UI',
                  fontSize: compact ? 25 : 29,
                  height: 1.06,
                  fontWeight: FontWeight.w700,
                  letterSpacing: -.65,
                ),
                children: [
                  TextSpan(text: diagnosis.prefix),
                  TextSpan(
                    text: diagnosis.highlight,
                    style: TextStyle(color: diagnosis.tone),
                  ),
                  TextSpan(text: diagnosis.suffix),
                ],
              ),
            ),
            const SizedBox(height: 9),
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 520),
              child: Text(
                diagnosis.support,
                style: TextStyle(color: t.muted, fontSize: 13, height: 1.3),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _funnel(BuildContext context, _MobileDiagnosticSnapshot snapshot) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('reports-mobile-period-table'),
      padding: const EdgeInsets.fromLTRB(0, 2, 0, 0),
      child: LayoutBuilder(
        builder: (context, constraints) {
          return CustomPaint(
            painter: _FunnelConnectorPainter(
              accent: t.accent,
              warning: const Color(0xFFE53935),
              positive: const Color(0xFF159462),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (var index = 0; index < snapshot.metrics.length; index++)
                  Expanded(
                    child: _funnelStage(
                      context,
                      snapshot.metrics[index],
                      index: index,
                      compact: constraints.maxWidth <= 330,
                    ),
                  ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _funnelStage(
    BuildContext context,
    _DiagnosticMetric metric, {
    required int index,
    required bool compact,
  }) {
    final t = AgendaThemeTokens.of(context);
    final tone = index == 3 ? const Color(0xFF159462) : t.accent;
    return Container(
      key: Key('reports-mobile-metric-${metric.keyName}'),
      padding: EdgeInsets.symmetric(horizontal: compact ? 2 : 4),
      child: Column(
        children: [
          Container(
            width: compact ? 43 : 48,
            height: compact ? 43 : 48,
            decoration: BoxDecoration(
              color: Color.lerp(t.panel, tone, .08),
              shape: BoxShape.circle,
              border: Border.all(color: tone.withValues(alpha: .16)),
            ),
            alignment: Alignment.center,
            child: FaIcon(metric.icon, color: tone, size: compact ? 17 : 19),
          ),
          const SizedBox(height: 8),
          SizedBox(
            height: 25,
            child: FittedBox(
              fit: BoxFit.scaleDown,
              child: AgendaAnimatedValue(
                value: metric.value,
                builder: (context, value) => Text(
                  value,
                  style: TextStyle(
                    color: index == 3 ? tone : t.ink,
                    fontSize: compact ? 19 : 22,
                    height: 1,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -.25,
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(height: 4),
          SizedBox(
            height: 28,
            child: Text(
              metric.label,
              textAlign: TextAlign.center,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: t.ink,
                fontSize: compact ? 9 : 10,
                height: 1.1,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          const SizedBox(height: 5),
          Container(
            constraints: const BoxConstraints(minHeight: 30),
            padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
            decoration: BoxDecoration(
              color: t.warmSoft,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              metric.comparison,
              textAlign: TextAlign.center,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: metric.direction < 0
                    ? const Color(0xFFE53935)
                    : metric.direction > 0
                    ? const Color(0xFF138A5C)
                    : t.muted,
                fontSize: compact ? 8 : 8.5,
                height: 1.05,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _trend(BuildContext context, _MobileDiagnosticSnapshot snapshot) {
    final t = AgendaThemeTokens.of(context);
    final points = snapshot.trendPoints;
    final values = points
        .map((point) => point.valueFor(_trendMetric))
        .toList(growable: false);
    return Container(
      key: const Key('reports-mobile-chart'),
      padding: const EdgeInsets.fromLTRB(4, 17, 4, 2),
      decoration: BoxDecoration(
        border: Border(top: BorderSide(color: t.line)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  'Evolução do período',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              PopupMenuButton<_ReportsTrendMetric>(
                key: const Key('reports-mobile-trend-metric'),
                initialValue: _trendMetric,
                onSelected: (value) => setState(() => _trendMetric = value),
                color: t.panel,
                position: PopupMenuPosition.under,
                itemBuilder: (context) => const [
                  PopupMenuItem(
                    value: _ReportsTrendMetric.revenue,
                    child: Text('Receita recebida'),
                  ),
                  PopupMenuItem(
                    value: _ReportsTrendMetric.bookings,
                    child: Text('Agendamentos'),
                  ),
                  PopupMenuItem(
                    value: _ReportsTrendMetric.completed,
                    child: Text('Realizados'),
                  ),
                ],
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      _trendMetricLabel,
                      style: TextStyle(
                        color: t.muted,
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(width: 5),
                    FaIcon(
                      FontAwesomeIcons.chevronDown,
                      color: t.muted,
                      size: 10,
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          SizedBox(
            height: 58,
            child: TweenAnimationBuilder<double>(
              tween: Tween(begin: 0, end: 1),
              duration: AgendaMotion.duration(context, AgendaMotion.emphasized),
              curve: AgendaMotion.enterCurve,
              builder: (context, progress, _) => CustomPaint(
                painter: _DiagnosticTrendPainter(
                  values: values,
                  accent: t.accent,
                  grid: t.line,
                  progress: progress,
                ),
              ),
            ),
          ),
          const SizedBox(height: 6),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (var index = 0; index < points.length; index++)
                Expanded(
                  child: Container(
                    key: ValueKey('reports-mobile-chart-label-$index'),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 2,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: index == points.length - 1
                          ? t.accentSoft
                          : Colors.transparent,
                      borderRadius: BorderRadius.circular(9),
                    ),
                    child: Column(
                      children: [
                        FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            _formatTrendValue(points[index]),
                            style: TextStyle(
                              color: index == points.length - 1
                                  ? t.accent
                                  : t.ink,
                              fontSize: 10,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          points[index].label,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: t.muted, fontSize: 8.5),
                        ),
                      ],
                    ),
                  ),
                ),
              const SizedBox.shrink(
                key: ValueKey('reports-mobile-chart-label-4'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  String get _trendMetricLabel => switch (_trendMetric) {
    _ReportsTrendMetric.revenue => 'Receita recebida',
    _ReportsTrendMetric.bookings => 'Agendamentos',
    _ReportsTrendMetric.completed => 'Realizados',
  };

  String _formatTrendValue(_DiagnosticTrendPoint point) =>
      switch (_trendMetric) {
        _ReportsTrendMetric.revenue => money(point.revenue, cents: false),
        _ReportsTrendMetric.bookings => '${point.bookings}',
        _ReportsTrendMetric.completed => '${point.completed}',
      };

  Widget _recommendedAction(
    BuildContext context,
    _MobileDiagnosticSnapshot snapshot,
  ) {
    final t = AgendaThemeTokens.of(context);
    final action = snapshot.recommendation;
    return Container(
      key: const Key('reports-mobile-recommendation'),
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 13),
      decoration: BoxDecoration(
        color: Color.lerp(t.panel, t.accentSoft, .34),
        border: Border.all(color: t.accent.withValues(alpha: .25)),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: t.panel,
                  shape: BoxShape.circle,
                  border: Border.all(color: t.accent.withValues(alpha: .18)),
                ),
                alignment: Alignment.center,
                child: FaIcon(action.icon, color: t.accent, size: 18),
              ),
              const SizedBox(width: 11),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'AÇÃO RECOMENDADA',
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 9,
                        fontWeight: FontWeight.w800,
                        letterSpacing: .4,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      action.title,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 18,
                        height: 1.08,
                        fontWeight: FontWeight.w700,
                        letterSpacing: -.25,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      action.detail,
                      style: TextStyle(
                        color: t.muted,
                        fontSize: 11.5,
                        height: 1.25,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (action.names.isNotEmpty) ...[
            const SizedBox(height: 12),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                for (final name in action.names.take(3))
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 5,
                    ),
                    decoration: BoxDecoration(
                      color: t.panel,
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Text(
                      name,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 9.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                if (action.names.length > 3)
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 5,
                    ),
                    decoration: BoxDecoration(
                      border: Border.all(
                        color: t.accent.withValues(alpha: .35),
                      ),
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Text(
                      '+${action.names.length - 3}',
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 9.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
              ],
            ),
          ],
          const SizedBox(height: 12),
          SizedBox(
            height: 46,
            child: FilledButton.icon(
              key: const Key('reports-mobile-open-agenda'),
              onPressed: () {
                final focusDate = action.focusDate;
                if (focusDate != null) {
                  widget.controller.selectDate(focusDate);
                }
                widget.controller.navigate(AgendaPage.agenda);
              },
              icon: FaIcon(action.buttonIcon, size: 16),
              label: Text(action.buttonLabel),
              style: FilledButton.styleFrom(
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(15),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _goal(BuildContext context, _MobileDiagnosticSnapshot snapshot) {
    final t = AgendaThemeTokens.of(context);
    final goal = snapshot.goal;
    final percentage = (goal.progress * 100).round();
    return Container(
      key: const Key('reports-mobile-goal'),
      padding: const EdgeInsets.fromLTRB(4, 2, 4, 2),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox.shrink(child: Text(widget.legacyGoalText)),
          if (goal.target <= 0)
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Defina uma meta de receita',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      Text(
                        'Acompanhe o progresso real de cada período.',
                        style: TextStyle(color: t.muted, fontSize: 11),
                      ),
                    ],
                  ),
                ),
                TextButton(
                  onPressed: () =>
                      widget.controller.navigate(AgendaPage.settings),
                  child: const Text('Configurar'),
                ),
              ],
            )
          else
            Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: Text(
                        goal.title,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 17,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    Text(
                      '$percentage%',
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 27,
                        height: 1,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  '${money(goal.current, cents: false)} de '
                  '${money(goal.target, cents: false)}',
                  style: TextStyle(color: t.muted, fontSize: 11),
                ),
                const SizedBox(height: 9),
                ClipRRect(
                  borderRadius: BorderRadius.circular(999),
                  child: TweenAnimationBuilder<double>(
                    tween: Tween(begin: 0, end: goal.progress),
                    duration: AgendaMotion.duration(
                      context,
                      AgendaMotion.emphasized,
                    ),
                    curve: AgendaMotion.enterCurve,
                    builder: (context, value, _) => LinearProgressIndicator(
                      minHeight: 8,
                      value: value,
                      backgroundColor: t.graySoft,
                      valueColor: AlwaysStoppedAnimation(t.accent),
                    ),
                  ),
                ),
                const SizedBox(height: 7),
                Text(
                  goal.remaining <= 0
                      ? 'Meta atingida. Continue acompanhando a qualidade da agenda.'
                      : 'Faltam ${money(goal.remaining, cents: false)} para atingir a meta.',
                  style: TextStyle(
                    color: goal.remaining <= 0
                        ? const Color(0xFF138A5C)
                        : t.muted,
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }

  Widget _whyItMatters(
    BuildContext context,
    _MobileDiagnosticSnapshot snapshot,
  ) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(13),
      decoration: BoxDecoration(
        color: t.warmSoft,
        borderRadius: BorderRadius.circular(17),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(color: t.panel, shape: BoxShape.circle),
            alignment: Alignment.center,
            child: FaIcon(
              FontAwesomeIcons.lightbulb,
              color: t.accent,
              size: 17,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Por que isso importa?',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  snapshot.whyItMatters,
                  style: TextStyle(color: t.muted, fontSize: 11, height: 1.25),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _MobileDiagnosticSnapshot {
  const _MobileDiagnosticSnapshot({
    required this.periodLabel,
    required this.referenceDateLabel,
    required this.metrics,
    required this.diagnosis,
    required this.trendPoints,
    required this.recommendation,
    required this.goal,
    required this.whyItMatters,
  });

  final String periodLabel;
  final String referenceDateLabel;
  final List<_DiagnosticMetric> metrics;
  final _Diagnosis diagnosis;
  final List<_DiagnosticTrendPoint> trendPoints;
  final _Recommendation recommendation;
  final _DiagnosticGoal goal;
  final String whyItMatters;

  factory _MobileDiagnosticSnapshot.from({
    required AgendaController controller,
    required ReportsMobilePeriod period,
    required AgendaBusinessProfile profile,
  }) {
    final range = _DiagnosticRange.forPeriod(controller.selectedDate, period);
    final current = _PeriodFacts.from(controller, range);
    final duration = range.end.difference(range.start);
    final previous = _PeriodFacts.from(
      controller,
      _DiagnosticRange(start: range.start.subtract(duration), end: range.start),
    );
    final labels = _segmentMetricLabels(profile);
    final metrics = <_DiagnosticMetric>[
      _DiagnosticMetric(
        keyName: 'bookings',
        label: labels.$1,
        value: '${current.bookings}',
        comparison: _countComparison(current.bookings, previous.bookings),
        direction: current.bookings.compareTo(previous.bookings),
        icon: FontAwesomeIcons.calendarDays,
      ),
      _DiagnosticMetric(
        keyName: 'confirmed',
        label: labels.$2,
        value: '${current.confirmed}',
        comparison: _countComparison(current.confirmed, previous.confirmed),
        direction: current.confirmed.compareTo(previous.confirmed),
        icon: FontAwesomeIcons.certificate,
      ),
      _DiagnosticMetric(
        keyName: 'completed',
        label: labels.$3,
        value: '${current.completed}',
        comparison: _countComparison(current.completed, previous.completed),
        direction: current.completed.compareTo(previous.completed),
        icon: FontAwesomeIcons.circleCheck,
      ),
      _DiagnosticMetric(
        keyName: 'revenue',
        label: 'Receita recebida',
        value: money(current.revenue, cents: false),
        comparison: _moneyComparison(current.revenue, previous.revenue),
        direction: current.revenue.compareTo(previous.revenue),
        icon: FontAwesomeIcons.moneyBillWave,
      ),
    ];
    final diagnosis = _diagnosisFor(profile, current);
    final trendPoints = [
      for (var offset = -3; offset <= 0; offset++)
        _DiagnosticTrendPoint.from(
          controller,
          _DiagnosticRange.shifted(range, period, offset),
          period,
        ),
    ];
    final recommendation = _recommendationFor(profile, current);
    final goal = _DiagnosticGoal.from(
      controller: controller,
      period: period,
      current: current.revenue,
    );

    return _MobileDiagnosticSnapshot(
      periodLabel: range.label(period),
      referenceDateLabel: DateFormat(
        "EEE, d 'de' MMMM 'de' y",
        'pt_BR',
      ).format(controller.selectedDate),
      metrics: metrics,
      diagnosis: diagnosis,
      trendPoints: trendPoints,
      recommendation: recommendation,
      goal: goal,
      whyItMatters: _whyFor(profile, current),
    );
  }
}

class _DiagnosticRange {
  const _DiagnosticRange({required this.start, required this.end});

  final DateTime start;
  final DateTime end;

  static _DiagnosticRange forPeriod(
    DateTime selected,
    ReportsMobilePeriod period,
  ) {
    final day = DateUtils.dateOnly(selected);
    return switch (period) {
      ReportsMobilePeriod.day => _DiagnosticRange(
        start: day,
        end: day.add(const Duration(days: 1)),
      ),
      ReportsMobilePeriod.week => () {
        final start = day.subtract(Duration(days: day.weekday - 1));
        return _DiagnosticRange(
          start: start,
          end: start.add(const Duration(days: 7)),
        );
      }(),
      ReportsMobilePeriod.month => _DiagnosticRange(
        start: DateTime(day.year, day.month),
        end: DateTime(day.year, day.month + 1),
      ),
    };
  }

  static _DiagnosticRange shifted(
    _DiagnosticRange current,
    ReportsMobilePeriod period,
    int offset,
  ) {
    return switch (period) {
      ReportsMobilePeriod.day => _DiagnosticRange(
        start: current.start.add(Duration(days: offset)),
        end: current.end.add(Duration(days: offset)),
      ),
      ReportsMobilePeriod.week => _DiagnosticRange(
        start: current.start.add(Duration(days: offset * 7)),
        end: current.end.add(Duration(days: offset * 7)),
      ),
      ReportsMobilePeriod.month => _DiagnosticRange(
        start: DateTime(current.start.year, current.start.month + offset),
        end: DateTime(current.start.year, current.start.month + offset + 1),
      ),
    };
  }

  String label(ReportsMobilePeriod period) => switch (period) {
    ReportsMobilePeriod.day => DateFormat('dd/MM', 'pt_BR').format(start),
    ReportsMobilePeriod.week =>
      '${DateFormat('dd/MM', 'pt_BR').format(start)} a '
          '${DateFormat('dd/MM', 'pt_BR').format(end.subtract(const Duration(days: 1)))}',
    ReportsMobilePeriod.month =>
      '${DateFormat('dd/MM', 'pt_BR').format(start)} a '
          '${DateFormat('dd/MM', 'pt_BR').format(end.subtract(const Duration(days: 1)))}',
  };
}

class _PeriodFacts {
  const _PeriodFacts({
    required this.appointments,
    required this.bookings,
    required this.confirmed,
    required this.completed,
    required this.unconfirmed,
    required this.noShows,
    required this.cancelled,
    required this.revenue,
  });

  final List<Appointment> appointments;
  final int bookings;
  final int confirmed;
  final int completed;
  final int unconfirmed;
  final int noShows;
  final int cancelled;
  final double revenue;

  factory _PeriodFacts.from(
    AgendaController controller,
    _DiagnosticRange range,
  ) {
    final appointments = controller
        .appointmentsBetween(range.start, range.end)
        .where((item) => item.status != AppointmentStatus.blocked)
        .toList(growable: false);
    final bookings = appointments
        .where((item) => item.status != AppointmentStatus.cancelled)
        .length;
    final confirmed = appointments.where((item) {
      if (item.attendanceConfirmedAt != null &&
          item.status != AppointmentStatus.cancelled) {
        return true;
      }
      return const {
        AppointmentStatus.confirmed,
        AppointmentStatus.waiting,
        AppointmentStatus.inService,
        AppointmentStatus.done,
      }.contains(item.status);
    }).length;
    final completed = appointments
        .where((item) => item.status == AppointmentStatus.done)
        .length;
    final unconfirmed = appointments
        .where((item) => item.status == AppointmentStatus.scheduled)
        .length;
    final noShows = appointments
        .where((item) => item.status == AppointmentStatus.noShow)
        .length;
    final cancelled = appointments
        .where((item) => item.status == AppointmentStatus.cancelled)
        .length;
    return _PeriodFacts(
      appointments: appointments,
      bookings: bookings,
      confirmed: confirmed,
      completed: completed,
      unconfirmed: unconfirmed,
      noShows: noShows,
      cancelled: cancelled,
      revenue: controller.revenueBetween(range.start, range.end),
    );
  }
}

class _DiagnosticMetric {
  const _DiagnosticMetric({
    required this.keyName,
    required this.label,
    required this.value,
    required this.comparison,
    required this.direction,
    required this.icon,
  });

  final String keyName;
  final String label;
  final String value;
  final String comparison;
  final int direction;
  final FaIconData icon;
}

class _Diagnosis {
  const _Diagnosis({
    required this.prefix,
    required this.highlight,
    required this.suffix,
    required this.support,
    required this.tone,
  });

  final String prefix;
  final String highlight;
  final String suffix;
  final String support;
  final Color tone;
}

class _Recommendation {
  const _Recommendation({
    required this.title,
    required this.detail,
    required this.buttonLabel,
    required this.icon,
    required this.buttonIcon,
    required this.names,
    this.focusDate,
  });

  final String title;
  final String detail;
  final String buttonLabel;
  final FaIconData icon;
  final FaIconData buttonIcon;
  final List<String> names;
  final DateTime? focusDate;
}

class _DiagnosticTrendPoint {
  const _DiagnosticTrendPoint({
    required this.label,
    required this.revenue,
    required this.bookings,
    required this.completed,
  });

  final String label;
  final double revenue;
  final int bookings;
  final int completed;

  factory _DiagnosticTrendPoint.from(
    AgendaController controller,
    _DiagnosticRange range,
    ReportsMobilePeriod period,
  ) {
    final facts = _PeriodFacts.from(controller, range);
    final label = switch (period) {
      ReportsMobilePeriod.day => DateFormat(
        'dd/MM',
        'pt_BR',
      ).format(range.start),
      ReportsMobilePeriod.week =>
        '${DateFormat('dd/MM', 'pt_BR').format(range.start)}–'
            '${DateFormat('dd/MM', 'pt_BR').format(range.end.subtract(const Duration(days: 1)))}',
      ReportsMobilePeriod.month => DateFormat(
        'MMM',
        'pt_BR',
      ).format(range.start),
    };
    return _DiagnosticTrendPoint(
      label: label,
      revenue: facts.revenue,
      bookings: facts.bookings,
      completed: facts.completed,
    );
  }

  double valueFor(_ReportsTrendMetric metric) => switch (metric) {
    _ReportsTrendMetric.revenue => revenue,
    _ReportsTrendMetric.bookings => bookings.toDouble(),
    _ReportsTrendMetric.completed => completed.toDouble(),
  };
}

class _DiagnosticGoal {
  const _DiagnosticGoal({
    required this.title,
    required this.current,
    required this.target,
  });

  final String title;
  final double current;
  final double target;

  double get progress =>
      target <= 0 ? 0 : (current / target).clamp(0, 1).toDouble();
  double get remaining => math.max(0, target - current);

  factory _DiagnosticGoal.from({
    required AgendaController controller,
    required ReportsMobilePeriod period,
    required double current,
  }) {
    final monthly = math
        .max(0, controller.data.settings.monthlyRevenueGoal)
        .toDouble();
    final target = switch (period) {
      ReportsMobilePeriod.day => monthly / 30,
      ReportsMobilePeriod.week => monthly / 4.345,
      ReportsMobilePeriod.month => monthly,
    };
    return _DiagnosticGoal(
      title: switch (period) {
        ReportsMobilePeriod.day => 'Sua meta do dia',
        ReportsMobilePeriod.week => 'Sua meta da semana',
        ReportsMobilePeriod.month => 'Sua meta do mês',
      },
      current: current,
      target: target,
    );
  }
}

(String, String, String) _segmentMetricLabels(AgendaBusinessProfile profile) {
  final segment = _normalizeSegment(profile.segment);
  if (segment.contains('oficina')) {
    return ('Novas ordens', 'Ordens confirmadas', 'Ordens finalizadas');
  }
  if (segment.contains('clinica')) {
    return ('Novas consultas', 'Consultas confirmadas', 'Consultas realizadas');
  }
  if (segment.contains('pet')) {
    return ('Novos cuidados', 'Cuidados confirmados', 'Pets atendidos');
  }
  return ('Novos agendamentos', 'Confirmados', 'Realizados');
}

_Diagnosis _diagnosisFor(AgendaBusinessProfile profile, _PeriodFacts facts) {
  const danger = Color(0xFFE5303A);
  const positive = Color(0xFF138A5C);
  final segment = _normalizeSegment(profile.segment);
  if (facts.noShows > 0) {
    final count = facts.noShows;
    if (segment.contains('clinica')) {
      return _Diagnosis(
        prefix: 'Você teve ',
        highlight: '$count ${count == 1 ? 'falta' : 'faltas'}',
        suffix: '\nnas consultas do período',
        support:
            'Confirmar com antecedência ajuda pacientes e equipe a manterem o plano de atendimento.',
        tone: danger,
      );
    }
    if (segment.contains('pet')) {
      return _Diagnosis(
        prefix: '',
        highlight: '$count ${count == 1 ? 'cuidado' : 'cuidados'}',
        suffix: '\nnão foram realizados',
        support:
            'Lembretes aos tutores ajudam a proteger a rotina de banho, tosa, vacina ou retorno.',
        tone: danger,
      );
    }
    if (segment.contains('oficina')) {
      return _Diagnosis(
        prefix: '',
        highlight: '$count ${count == 1 ? 'veículo' : 'veículos'}',
        suffix: '\nnão chegaram ao box',
        support:
            'Confirme a chegada antes de reservar box, elevador e tempo da equipe.',
        tone: danger,
      );
    }
    return _Diagnosis(
      prefix: 'Você perdeu ',
      highlight: '$count ${count == 1 ? 'horário' : 'horários'}',
      suffix: '\npor falta de confirmação',
      support:
          'Confirmar é o principal filtro para preencher a agenda e reduzir faltas.',
      tone: danger,
    );
  }
  if (facts.unconfirmed > 0) {
    final count = facts.unconfirmed;
    final noun = switch (profile.activityPlural) {
      'consultas' => count == 1 ? 'consulta' : 'consultas',
      'ordens de serviço' => count == 1 ? 'ordem' : 'ordens',
      'atendimentos pet' => count == 1 ? 'cuidado' : 'cuidados',
      _ => count == 1 ? 'horário' : 'horários',
    };
    final audience = switch (profile.activityPlural) {
      'consultas' => ' dos pacientes',
      'ordens de serviço' => ' de chegada',
      'atendimentos pet' => ' dos tutores',
      _ => '',
    };
    return _Diagnosis(
      prefix: '',
      highlight: '$count $noun',
      suffix:
          '\nainda ${count == 1 ? 'precisa' : 'precisam'} de confirmação$audience',
      support:
          'Priorize quem está mais perto do horário e libere vagas sem resposta.',
      tone: const Color(0xFFF05A20),
    );
  }
  if (facts.bookings == 0) {
    return _Diagnosis(
      prefix: 'Seu diagnóstico começa\ncom o ',
      highlight: 'primeiro agendamento',
      suffix: '',
      support:
          'Assim que houver movimento, você verá conversão, presença e receita conectadas.',
      tone: const Color(0xFFF05A20),
    );
  }
  final attendance = facts.bookings == 0
      ? 0
      : ((facts.completed / facts.bookings) * 100).round();
  return _Diagnosis(
    prefix: 'Sua taxa de realização\nchegou a ',
    highlight: '$attendance%',
    suffix: '',
    support:
        'Acompanhe a tendência e mantenha confirmações e retornos no ritmo certo.',
    tone: positive,
  );
}

_Recommendation _recommendationFor(
  AgendaBusinessProfile profile,
  _PeriodFacts facts,
) {
  final pending =
      facts.appointments
          .where((item) => item.status == AppointmentStatus.scheduled)
          .toList()
        ..sort((a, b) => a.start.compareTo(b.start));
  if (pending.isNotEmpty) {
    final count = pending.length;
    final segment = _normalizeSegment(profile.segment);
    final title = segment.contains('clinica')
        ? 'Confirmar $count ${count == 1 ? 'consulta' : 'consultas'}'
        : segment.contains('pet')
        ? 'Lembrar $count ${count == 1 ? 'tutor' : 'tutores'}'
        : segment.contains('oficina')
        ? 'Confirmar chegada de $count ${count == 1 ? 'veículo' : 'veículos'}'
        : 'Enviar lembrete para $count ${count == 1 ? 'cliente' : 'clientes'}';
    return _Recommendation(
      title: title,
      detail:
          'Revise os horários sem confirmação e fale pelo canal conectado antes de liberar a vaga.',
      buttonLabel: 'Revisar na agenda',
      icon: FontAwesomeIcons.bell,
      buttonIcon: FontAwesomeIcons.paperPlane,
      names: pending
          .map((item) => item.customerName.trim())
          .where((name) => name.isNotEmpty)
          .toList(growable: false),
      focusDate: pending.first.start,
    );
  }
  final missed = facts.appointments
      .where((item) => item.status == AppointmentStatus.noShow)
      .toList();
  if (missed.isNotEmpty) {
    return _Recommendation(
      title: profile.marketingReturnTitle,
      detail:
          'Retome o contato com quem faltou e ofereça um novo horário sem perder o histórico.',
      buttonLabel: 'Preparar reagendamento',
      icon: FontAwesomeIcons.rotate,
      buttonIcon: FontAwesomeIcons.calendarPlus,
      names: missed
          .map((item) => item.customerName.trim())
          .where((name) => name.isNotEmpty)
          .toList(growable: false),
      focusDate: missed.first.start,
    );
  }
  if (facts.bookings == 0) {
    return _Recommendation(
      title: profile.newActivityLabel,
      detail:
          'Abra a agenda e registre o primeiro horário para começar a medir o período.',
      buttonLabel: 'Abrir agenda',
      icon: FontAwesomeIcons.calendarPlus,
      buttonIcon: FontAwesomeIcons.arrowRight,
      names: const [],
    );
  }
  return _Recommendation(
    title: profile.marketingReturnTitle,
    detail: profile.marketingReturnDetail,
    buttonLabel: 'Planejar próximos horários',
    icon: FontAwesomeIcons.arrowsRotate,
    buttonIcon: FontAwesomeIcons.arrowRight,
    names: const [],
    focusDate: facts.appointments.firstOrNull?.start,
  );
}

String _whyFor(AgendaBusinessProfile profile, _PeriodFacts facts) {
  final segment = _normalizeSegment(profile.segment);
  if (segment.contains('oficina')) {
    return 'Cada chegada confirmada melhora o uso de boxes e elevadores e reduz tempo ocioso da equipe.';
  }
  if (segment.contains('clinica')) {
    return 'Cada confirmação reduz faltas, protege o cuidado do paciente e melhora o uso da agenda clínica.';
  }
  if (segment.contains('pet')) {
    return 'Cada confirmação ajuda a organizar profissionais, espaço e o próximo cuidado de cada pet.';
  }
  if (facts.noShows > 0 || facts.unconfirmed > 0) {
    return 'Cada confirmação aumenta a chance de o cliente comparecer e reduz o impacto das faltas na receita.';
  }
  return 'Acompanhar confirmação, realização e receita juntos mostra onde agir sem depender de suposições.';
}

String _countComparison(int current, int previous) {
  if (current == 0 && previous == 0) return '0% sem alteração';
  if (previous == 0) return '+100% novo';
  final delta = ((current - previous) * 100 / previous).round();
  if (delta == 0) return '0% estável';
  return '${delta > 0 ? '+' : ''}$delta% vs. anterior';
}

String _moneyComparison(double current, double previous) {
  if (current == 0 && previous == 0) return '0% sem alteração';
  if (previous == 0) return '+100% novo';
  final delta = ((current - previous) * 100 / previous).round();
  if (delta == 0) return '0% estável';
  return '${delta > 0 ? '+' : ''}$delta% vs. anterior';
}

String _normalizeSegment(String value) => value
    .trim()
    .toLowerCase()
    .replaceAll(RegExp('[áàâãä]'), 'a')
    .replaceAll(RegExp('[éèêë]'), 'e')
    .replaceAll(RegExp('[íìîï]'), 'i')
    .replaceAll(RegExp('[óòôõö]'), 'o')
    .replaceAll(RegExp('[úùûü]'), 'u')
    .replaceAll('ç', 'c');

class _FunnelConnectorPainter extends CustomPainter {
  const _FunnelConnectorPainter({
    required this.accent,
    required this.warning,
    required this.positive,
  });

  final Color accent;
  final Color warning;
  final Color positive;

  @override
  void paint(Canvas canvas, Size size) {
    if (size.width <= 0) return;
    final segment = size.width / 4;
    final centers = [
      for (var index = 0; index < 4; index++)
        Offset(segment * (index + .5), 24),
    ];
    for (var index = 0; index < centers.length - 1; index++) {
      final start = centers[index] + const Offset(24, 0);
      final end = centers[index + 1] - const Offset(24, 0);
      if (end.dx <= start.dx) continue;
      final path = Path()
        ..moveTo(start.dx, start.dy)
        ..quadraticBezierTo((start.dx + end.dx) / 2, 43, end.dx, end.dy);
      final color = index == 2
          ? positive
          : index == 0
          ? warning
          : accent;
      final paint = Paint()
        ..color = color.withValues(alpha: .7)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1.4
        ..strokeCap = StrokeCap.round;
      for (final metric in path.computeMetrics()) {
        var distance = 0.0;
        while (distance < metric.length) {
          canvas.drawPath(
            metric.extractPath(distance, math.min(distance + 3, metric.length)),
            paint,
          );
          distance += 6;
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant _FunnelConnectorPainter oldDelegate) =>
      oldDelegate.accent != accent ||
      oldDelegate.warning != warning ||
      oldDelegate.positive != positive;
}

class _DiagnosticTrendPainter extends CustomPainter {
  const _DiagnosticTrendPainter({
    required this.values,
    required this.accent,
    required this.grid,
    required this.progress,
  });

  final List<double> values;
  final Color accent;
  final Color grid;
  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    if (values.isEmpty || size.width <= 0 || size.height <= 0) return;
    final gridPaint = Paint()
      ..color = grid.withValues(alpha: .7)
      ..strokeWidth = 1;
    canvas.drawLine(
      Offset(0, size.height - 7),
      Offset(size.width, size.height - 7),
      gridPaint,
    );
    final maxValue = values.fold<double>(0, math.max);
    final safeMax = maxValue <= 0 ? 1 : maxValue;
    final step = values.length == 1 ? 0.0 : size.width / (values.length - 1);
    final points = [
      for (var index = 0; index < values.length; index++)
        Offset(
          step * index,
          5 + (size.height - 16) * (1 - (values[index] / safeMax)),
        ),
    ];
    final fullPath = Path()..moveTo(points.first.dx, points.first.dy);
    for (var index = 1; index < points.length; index++) {
      fullPath.lineTo(points[index].dx, points[index].dy);
    }
    final linePaint = Paint()
      ..color = accent
      ..strokeWidth = 2.2
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;
    for (final metric in fullPath.computeMetrics()) {
      canvas.drawPath(
        metric.extractPath(0, metric.length * progress),
        linePaint,
      );
    }
    final dotPaint = Paint()..color = accent;
    final outline = Paint()
      ..color = accent
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.7;
    for (var index = 0; index < points.length; index++) {
      if (index / math.max(1, points.length - 1) > progress) continue;
      if (index == points.length - 1) {
        canvas.drawCircle(points[index], 4.5, Paint()..color = Colors.white);
        canvas.drawCircle(points[index], 4.5, outline);
      } else {
        canvas.drawCircle(points[index], 3.5, dotPaint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _DiagnosticTrendPainter oldDelegate) =>
      oldDelegate.values != values ||
      oldDelegate.accent != accent ||
      oldDelegate.grid != grid ||
      oldDelegate.progress != progress;
}
