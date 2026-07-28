import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';

const _green = Color(0xFF16A34A);
const _red = Color(0xFFDC2626);
const _orange = Color(0xFFF59E0B);

class ReportsPage extends StatefulWidget {
  const ReportsPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  State<ReportsPage> createState() => _ReportsPageState();
}

enum _ChartMode { appointments, status }

class _ReportsPageState extends State<ReportsPage> {
  _ChartMode _chartMode = _ChartMode.appointments;

  Future<void> _copySummary(_ReportsSnapshot snapshot) async {
    await Clipboard.setData(ClipboardData(text: snapshot.summary));
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Resumo copiado para a área de transferência.'),
      ),
    );
  }

  Future<void> _exportCsv(_ReportsSnapshot snapshot) async {
    await Clipboard.setData(ClipboardData(text: snapshot.csv));
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Relatório em CSV copiado para a área de transferência.'),
      ),
    );
  }

  Future<void> _showPrintPreview(_ReportsSnapshot snapshot) async {
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Pré-visualização do relatório'),
        content: SizedBox(
          width: 560,
          child: SingleChildScrollView(child: SelectableText(snapshot.summary)),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Fechar'),
          ),
          ElevatedButton.icon(
            onPressed: () async {
              await Clipboard.setData(ClipboardData(text: snapshot.summary));
              if (dialogContext.mounted) Navigator.of(dialogContext).pop();
            },
            icon: const Icon(Icons.copy_rounded, size: 18),
            label: const Text('Copiar para imprimir'),
          ),
        ],
      ),
    );
    if (!mounted) return;
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final t = AgendaThemeTokens.of(context);
        final snapshot = _ReportsSnapshot.from(widget.controller, t);
        final desktop = MediaQuery.sizeOf(context).width >= 1200;
        return ColoredBox(
          color: Color(0xFFFAF9F7),
          child: LayoutBuilder(
            builder: (context, viewport) {
              final pagePadding = desktop
                  ? const EdgeInsets.fromLTRB(28, 20, 36, 94)
                  : const EdgeInsets.fromLTRB(14, 18, 14, 28);
              return SingleChildScrollView(
                padding: pagePadding,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _ReportsHero(
                      desktop: desktop,
                      period:
                          '${_dayMonth(snapshot.periodStart)} a ${_dayMonth(snapshot.periodEnd)}',
                      onCopy: () => _copySummary(snapshot),
                      onPrint: () => _showPrintPreview(snapshot),
                      onExport: () => _exportCsv(snapshot),
                    ),
                    const SizedBox(height: 14),
                    _ReportsMetricStrip(snapshot: snapshot, desktop: desktop),
                    const SizedBox(height: 10),
                    _ReportsBody(
                      desktop: desktop,
                      chart: _ReportChartCard(
                        snapshot: snapshot,
                        mode: _chartMode,
                        onModeChanged: (mode) =>
                            setState(() => _chartMode = mode),
                      ),
                      services: _ServicesCard(services: snapshot.services),
                      insights: _InsightsCard(insights: snapshot.insights),
                      professionals: _ProfessionalsCard(
                        professionals: snapshot.professionals,
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
}

class _ReportsHero extends StatelessWidget {
  const _ReportsHero({
    required this.desktop,
    required this.period,
    required this.onCopy,
    required this.onPrint,
    required this.onExport,
  });

  final bool desktop;
  final String period;
  final VoidCallback onCopy;
  final VoidCallback onPrint;
  final VoidCallback onExport;

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
          'Relatórios',
          style: TextStyle(
            color: t.ink,
            fontSize: 28,
            height: 1.05,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          'Resumo objetivo dos atendimentos, receita, serviços e profissionais.',
          style: TextStyle(color: t.muted, fontSize: 13),
        ),
        const SizedBox(height: 8),
        Text(
          period,
          style: TextStyle(
            color: t.accent,
            fontSize: 14,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    );
    final actions = Wrap(
      spacing: 10,
      runSpacing: 8,
      alignment: desktop ? WrapAlignment.end : WrapAlignment.start,
      children: [
        _ReportActionButton(
          width: 150,
          filled: true,
          icon: Icons.content_copy_rounded,
          label: 'Copiar resumo',
          onPressed: onCopy,
        ),
        _ReportActionButton(
          width: 154,
          icon: Icons.print_outlined,
          label: 'Pré-visualizar',
          onPressed: onPrint,
        ),
        _ReportActionButton(
          width: 142,
          icon: Icons.content_copy_rounded,
          label: 'Copiar CSV',
          onPressed: onExport,
        ),
      ],
    );

    return _ReportSurface(
      key: const Key('reports-hero'),
      radius: 24,
      minHeight: 140,
      padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 17),
      clip: true,
      child: Stack(
        children: [
          const Positioned(
            right: -2,
            top: -6,
            width: 300,
            height: 104,
            child: IgnorePointer(
              child: Opacity(opacity: .045, child: _ReportWatermark()),
            ),
          ),
          ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 92),
            child: desktop
                ? Row(
                    children: [
                      Expanded(child: heading),
                      const SizedBox(width: 18),
                      actions,
                    ],
                  )
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      heading,
                      const SizedBox(height: 15),
                      Align(alignment: Alignment.centerLeft, child: actions),
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

class _ReportActionButton extends StatelessWidget {
  const _ReportActionButton({
    required this.width,
    required this.icon,
    required this.label,
    required this.onPressed,
    this.filled = false,
  });

  final double width;
  final IconData icon;
  final String label;
  final VoidCallback onPressed;
  final bool filled;

  @override
  Widget build(BuildContext context) {
    final button = filled
        ? ElevatedButton.icon(
            onPressed: onPressed,
            icon: Icon(icon, size: 18),
            label: Text(label),
          )
        : OutlinedButton.icon(
            onPressed: onPressed,
            icon: Icon(icon, size: 18),
            label: Text(label),
          );
    return SizedBox(width: width, height: 44, child: button);
  }
}

class _ReportsBody extends StatelessWidget {
  const _ReportsBody({
    required this.desktop,
    required this.chart,
    required this.services,
    required this.insights,
    required this.professionals,
  });

  final bool desktop;
  final Widget chart;
  final Widget services;
  final Widget insights;
  final Widget professionals;

  @override
  Widget build(BuildContext context) {
    if (!desktop) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          chart,
          const SizedBox(height: 8),
          insights,
          const SizedBox(height: 8),
          services,
          const SizedBox(height: 8),
          professionals,
        ],
      );
    }
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          flex: 190,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [chart, const SizedBox(height: 8), services],
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          flex: 118,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [insights, const SizedBox(height: 8), professionals],
          ),
        ),
      ],
    );
  }
}

class _ReportsMetricStrip extends StatelessWidget {
  const _ReportsMetricStrip({required this.snapshot, required this.desktop});

  final _ReportsSnapshot snapshot;
  final bool desktop;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final metrics = <_ReportMetricData>[
      _ReportMetricData(
        label: 'Agendamentos',
        value: '${snapshot.appointments.length}',
        caption: 'total do período',
        icon: Icons.calendar_month_outlined,
        iconColor: t.accent,
        background: t.accentSoft,
      ),
      _ReportMetricData(
        label: 'Finalizados',
        value: '${snapshot.doneCount}',
        caption: 'concluídos',
        icon: Icons.check_circle_outline_rounded,
        iconColor: _green,
        background: t.blueSoft,
      ),
      _ReportMetricData(
        label: 'Cancelados/faltas',
        value: '${snapshot.lostCount}',
        caption: 'perdas',
        icon: Icons.error_outline_rounded,
        iconColor: _red,
        background: snapshot.lostCount > 0 ? t.redSoft : t.graySoft,
      ),
      _ReportMetricData(
        label: 'Receita',
        value: money(snapshot.revenue, cents: false),
        caption: 'entradas',
        icon: Icons.account_balance_wallet_outlined,
        iconColor: t.accent,
        background: t.warmSoft,
      ),
      _ReportMetricData(
        label: 'Ticket médio',
        value: money(snapshot.averageTicket, cents: false),
        caption: 'por finalizado',
        icon: Icons.payments_outlined,
        iconColor: t.accent,
        background: t.blueSoft,
      ),
      _ReportMetricData(
        label: 'Conclusão',
        value: '${snapshot.conclusion.toStringAsFixed(0)}%',
        caption: 'sobre o total',
        icon: Icons.donut_small_rounded,
        iconColor: _orange,
        background: t.yellowSoft,
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = desktop || constraints.maxWidth >= 720 ? 3 : 2;
        return GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: metrics.length,
          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: columns,
            crossAxisSpacing: 10,
            mainAxisSpacing: 10,
            mainAxisExtent: 83,
          ),
          itemBuilder: (context, index) => _ReportMetricCard(
            key: ValueKey('report-metric-${metrics[index].label}'),
            data: metrics[index],
          ),
        );
      },
    );
  }
}

class _ReportMetricData {
  const _ReportMetricData({
    required this.label,
    required this.value,
    required this.caption,
    required this.icon,
    required this.iconColor,
    required this.background,
  });

  final String label;
  final String value;
  final String caption;
  final IconData icon;
  final Color iconColor;
  final Color background;
}

class _ReportMetricCard extends StatelessWidget {
  const _ReportMetricCard({super.key, required this.data});

  final _ReportMetricData data;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _ReportSurface(
      radius: 14,
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Container(
            width: 30,
            height: 30,
            decoration: BoxDecoration(
              color: data.background,
              borderRadius: BorderRadius.circular(12),
            ),
            alignment: Alignment.center,
            child: Icon(data.icon, color: data.iconColor, size: 16),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  data.label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.muted,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 1),
                Text(
                  data.value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 18,
                    height: 1.05,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 1),
                Text(
                  data.caption,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ReportWatermark extends StatelessWidget {
  const _ReportWatermark();

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

class _ReportSurface extends StatelessWidget {
  const _ReportSurface({
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

class _ReportChartCard extends StatelessWidget {
  const _ReportChartCard({
    required this.snapshot,
    required this.mode,
    required this.onModeChanged,
  });

  final _ReportsSnapshot snapshot;
  final _ChartMode mode;
  final ValueChanged<_ChartMode> onModeChanged;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _ReportSurface(
      radius: 16,
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          LayoutBuilder(
            builder: (context, constraints) {
              final heading = Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    mode == _ChartMode.appointments
                        ? 'Agendamentos por dia'
                        : 'Status dos atendimentos',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 17,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    mode == _ChartMode.appointments
                        ? 'Volume dos últimos 7 dias'
                        : 'Distribuição dos atendimentos por situação.',
                    style: TextStyle(color: t.muted, fontSize: 12),
                  ),
                ],
              );
              final toggles = Wrap(
                spacing: 6,
                runSpacing: 6,
                children: [
                  _ChartModeButton(
                    label: 'Agendamentos',
                    selected: mode == _ChartMode.appointments,
                    onPressed: () => onModeChanged(_ChartMode.appointments),
                  ),
                  _ChartModeButton(
                    label: 'Status',
                    selected: mode == _ChartMode.status,
                    onPressed: () => onModeChanged(_ChartMode.status),
                  ),
                ],
              );
              if (constraints.maxWidth < 570) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [heading, const SizedBox(height: 10), toggles],
                );
              }
              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: heading),
                  const SizedBox(width: 12),
                  toggles,
                ],
              );
            },
          ),
          const SizedBox(height: 12),
          AnimatedSwitcher(
            duration: const Duration(milliseconds: 180),
            child: mode == _ChartMode.appointments
                ? _AppointmentsChart(
                    key: const ValueKey('appointments'),
                    days: snapshot.days,
                  )
                : _StatusChart(
                    key: const ValueKey('status'),
                    slices: snapshot.statusSlices,
                  ),
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            decoration: BoxDecoration(
              color: t.panel,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Row(
              children: [
                Icon(Icons.show_chart_rounded, color: t.accent, size: 18),
                const SizedBox(width: 9),
                Expanded(
                  child: Text(
                    mode == _ChartMode.appointments
                        ? 'Total de agendamentos no período: ${snapshot.appointments.length}'
                        : 'Total por status: ${snapshot.appointments.length}',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  mode == _ChartMode.appointments
                      ? 'Média diária: ${_averageText(snapshot.appointments.length / 7)}'
                      : 'Maior grupo: ${snapshot.topStatusLabel}',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 12,
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

class _ChartModeButton extends StatelessWidget {
  const _ChartModeButton({
    required this.label,
    required this.selected,
    required this.onPressed,
  });

  final String label;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    if (selected) {
      return ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          minimumSize: const Size(0, 40),
          padding: const EdgeInsets.symmetric(horizontal: 12),
        ),
        child: Text(label),
      );
    }
    return OutlinedButton(
      onPressed: onPressed,
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(0, 40),
        padding: const EdgeInsets.symmetric(horizontal: 12),
      ),
      child: Text(label),
    );
  }
}

class _AppointmentsChart extends StatelessWidget {
  const _AppointmentsChart({super.key, required this.days});

  final List<_ReportDay> days;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 600;
    return Column(
      children: [
        SizedBox(
          key: const ValueKey('reports-appointments-chart-canvas'),
          width: double.infinity,
          height: 104,
          child: CustomPaint(
            painter: _ReportColumnPainter(
              values: days.map((item) => item.count).toList(),
              accent: t.accent,
              grid: t.line,
              muted: t.muted,
              ink: t.ink,
            ),
          ),
        ),
        const SizedBox(height: 4),
        SizedBox(
          height: 18,
          child: Row(
            children: [
              const SizedBox(width: 32),
              for (final day in days)
                Expanded(
                  child: Text(
                    compact
                        ? _weekday(day.day)
                        : '${_weekday(day.day)}, ${shortDate(day.day)}',
                    textAlign: TextAlign.center,
                    maxLines: 1,
                    overflow: TextOverflow.clip,
                    style: TextStyle(
                      color: t.muted,
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              const SizedBox(width: 4),
            ],
          ),
        ),
      ],
    );
  }
}

class _StatusChart extends StatelessWidget {
  const _StatusChart({super.key, required this.slices});

  final List<_StatusSlice> slices;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final chart = SizedBox.square(
      dimension: 138,
      child: CustomPaint(
        painter: _ReportDonutPainter(
          slices: slices,
          emptyColor: t.graySoft,
          centerColor: t.ink,
        ),
      ),
    );
    final legend = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: slices.isEmpty
          ? [
              _LegendRow(
                color: t.graySoft,
                label: 'Sem agendamentos no período',
                value: '0',
              ),
            ]
          : [
              for (final slice in slices)
                _LegendRow(
                  color: slice.color,
                  label: slice.label,
                  value: '${slice.value} ag.',
                ),
            ],
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 460) {
          return Column(children: [chart, const SizedBox(height: 12), legend]);
        }
        return SizedBox(
          height: 164,
          child: Row(
            children: [
              chart,
              const SizedBox(width: 18),
              Expanded(child: SingleChildScrollView(child: legend)),
            ],
          ),
        );
      },
    );
  }
}

class _LegendRow extends StatelessWidget {
  const _LegendRow({
    required this.color,
    required this.label,
    required this.value,
  });

  final Color color;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Container(
            width: 9,
            height: 9,
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(3),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              label,
              style: TextStyle(
                color: t.ink,
                fontSize: 11.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Text(
            value,
            style: TextStyle(
              color: t.muted,
              fontSize: 11,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _InsightsCard extends StatelessWidget {
  const _InsightsCard({required this.insights});

  final List<_Insight> insights;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _ReportSurface(
      radius: 16,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Leituras rápidas',
            style: TextStyle(
              color: t.ink,
              fontSize: 18,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            'Pontos importantes do período',
            style: TextStyle(color: t.muted, fontSize: 12),
          ),
          const SizedBox(height: 10),
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = constraints.maxWidth >= 300 ? 2 : 1;
              return GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: insights.length,
                gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: columns,
                  crossAxisSpacing: 8,
                  mainAxisSpacing: 8,
                  mainAxisExtent: 50,
                ),
                itemBuilder: (context, index) =>
                    _InsightTile(insight: insights[index]),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _InsightTile extends StatelessWidget {
  const _InsightTile({required this.insight});

  final _Insight insight;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          AgendaIconBadge(
            insight.icon,
            size: 24,
            iconSize: 13,
            color: insight.tone,
            background: insight.background,
          ),
          const SizedBox(width: 7),
          Expanded(
            child: Text(
              insight.title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: t.ink,
                fontSize: 11.3,
                fontWeight: FontWeight.w700,
                height: 1.05,
              ),
            ),
          ),
          const SizedBox(width: 5),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
            decoration: BoxDecoration(
              color: insight.background,
              borderRadius: BorderRadius.circular(999),
            ),
            child: Text(
              insight.badge,
              maxLines: 1,
              style: TextStyle(
                color: insight.tone,
                fontSize: 9.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ServicesCard extends StatefulWidget {
  const _ServicesCard({required this.services});

  final List<_ServiceSummary> services;

  @override
  State<_ServicesCard> createState() => _ServicesCardState();
}

class _ServicesCardState extends State<_ServicesCard> {
  final ScrollController _controller = ScrollController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _move(double delta) {
    if (!_controller.hasClients) return;
    final target = (_controller.offset + delta).clamp(
      0.0,
      _controller.position.maxScrollExtent,
    );
    _controller.animateTo(
      target,
      duration: const Duration(milliseconds: 220),
      curve: Curves.easeOutCubic,
    );
  }

  @override
  Widget build(BuildContext context) {
    return _ReportSurface(
      radius: 16,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _ReportSectionHeading(
            icon: Icons.assignment_outlined,
            title: 'Serviços mais realizados',
            subtitle: 'Quantidade e receita gerada',
            trailing: widget.services.length > 3
                ? Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      _ReportCarouselButton(
                        icon: Icons.chevron_left_rounded,
                        tooltip: 'Serviços anteriores',
                        onPressed: () => _move(-208),
                      ),
                      const SizedBox(width: 6),
                      _ReportCarouselButton(
                        icon: Icons.chevron_right_rounded,
                        tooltip: 'Próximos serviços',
                        onPressed: () => _move(208),
                      ),
                    ],
                  )
                : null,
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 104,
            child: ListView.separated(
              controller: _controller,
              scrollDirection: Axis.horizontal,
              itemCount: widget.services.length,
              separatorBuilder: (_, _) => const SizedBox(width: 10),
              itemBuilder: (context, index) => SizedBox(
                width: 198,
                child: _ServiceTile(service: widget.services[index]),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ReportCarouselButton extends StatelessWidget {
  const _ReportCarouselButton({
    required this.icon,
    required this.tooltip,
    required this.onPressed,
  });

  final IconData icon;
  final String tooltip;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox.square(
      dimension: 40,
      child: IconButton(
        tooltip: tooltip,
        onPressed: onPressed,
        padding: EdgeInsets.zero,
        style: IconButton.styleFrom(
          foregroundColor: t.ink,
          backgroundColor: t.panel,
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        icon: Icon(icon, size: 20),
      ),
    );
  }
}

class _ServiceTile extends StatelessWidget {
  const _ServiceTile({required this.service});

  final _ServiceSummary service;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return ClipRRect(
      borderRadius: BorderRadius.circular(14),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: t.panel,
          border: Border.all(color: t.line),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Stack(
          children: [
            Positioned.fill(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(12, 12, 12, 9),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        AgendaIconBadge(
                          Icons.assignment_outlined,
                          size: 30,
                          iconSize: 15,
                          color: service.empty ? t.muted : t.accent,
                          background: service.empty ? t.graySoft : t.accentSoft,
                        ),
                        const Spacer(),
                        if (!service.empty)
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 7,
                              vertical: 3,
                            ),
                            decoration: BoxDecoration(
                              color: t.accentSoft,
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: Text(
                              money(service.revenue, cents: false),
                              style: TextStyle(
                                color: t.accentDark,
                                fontSize: 9.5,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                      ],
                    ),
                    const Spacer(),
                    Text(
                      service.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      service.empty
                          ? 'Sem atendimentos no período.'
                          : '${service.count} atendimento(s) no período',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.muted, fontSize: 11),
                    ),
                  ],
                ),
              ),
            ),
            Positioned(
              left: 0,
              right: 0,
              bottom: 0,
              height: 4,
              child: ColoredBox(color: service.empty ? t.graySoft : t.accent),
            ),
          ],
        ),
      ),
    );
  }
}

class _ProfessionalsCard extends StatelessWidget {
  const _ProfessionalsCard({required this.professionals});

  final List<_ProfessionalSummary> professionals;

  @override
  Widget build(BuildContext context) {
    return _ReportSurface(
      radius: 16,
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _ReportSectionHeading(
            icon: Icons.badge_outlined,
            title: 'Profissionais',
            subtitle: 'Atendimentos e receita da equipe',
          ),
          const SizedBox(height: 10),
          for (var index = 0; index < professionals.length; index++) ...[
            _ProfessionalTile(professional: professionals[index]),
            if (index != professionals.length - 1) const SizedBox(height: 7),
          ],
        ],
      ),
    );
  }
}

class _ReportSectionHeading extends StatelessWidget {
  const _ReportSectionHeading({
    required this.icon,
    required this.title,
    required this.subtitle,
    this.trailing,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        AgendaIconBadge(
          icon,
          size: 34,
          iconSize: 17,
          color: t.accent,
          background: t.accentSoft,
        ),
        const SizedBox(width: 9),
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
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 1),
              Text(
                subtitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: t.muted, fontSize: 12),
              ),
            ],
          ),
        ),
        if (trailing != null) ...[const SizedBox(width: 8), trailing!],
      ],
    );
  }
}

class _ProfessionalTile extends StatelessWidget {
  const _ProfessionalTile({required this.professional});

  final _ProfessionalSummary professional;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      constraints: const BoxConstraints(minHeight: 62),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          AgendaIconBadge(
            professional.empty
                ? Icons.person_outline_rounded
                : Icons.badge_outlined,
            size: 34,
            iconSize: 17,
            color: professional.empty ? t.muted : t.accent,
            background: professional.empty ? t.graySoft : t.accentSoft,
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  professional.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  professional.empty
                      ? 'Os atendimentos da equipe aparecerão aqui.'
                      : '${professional.done} finalizado(s) | ${professional.count} atendimento(s)',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11.2),
                ),
              ],
            ),
          ),
          if (!professional.empty) ...[
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: t.accentSoft,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                money(professional.revenue, cents: false),
                style: TextStyle(
                  color: t.accentDark,
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _ReportColumnPainter extends CustomPainter {
  const _ReportColumnPainter({
    required this.values,
    required this.accent,
    required this.grid,
    required this.muted,
    required this.ink,
  });

  final List<int> values;
  final Color accent;
  final Color grid;
  final Color muted;
  final Color ink;

  @override
  void paint(Canvas canvas, Size size) {
    if (values.isEmpty || size.width < 60 || size.height < 40) return;
    const left = 32.0;
    const right = 4.0;
    const top = 13.0;
    const bottom = 8.0;
    final width = size.width - left - right;
    final height = size.height - top - bottom;
    final maxValue = math.max(4, values.fold<int>(0, math.max));
    final gridPaint = Paint()
      ..color = grid
      ..strokeWidth = 1;
    for (var index = 0; index <= 4; index++) {
      final value = maxValue * (4 - index) / 4;
      final y = top + height * index / 4;
      canvas.drawLine(
        Offset(left, y),
        Offset(size.width - right, y),
        gridPaint,
      );
      final text = TextPainter(
        text: TextSpan(
          text: value.toStringAsFixed(value == value.roundToDouble() ? 0 : 1),
          style: TextStyle(color: muted, fontSize: 11),
        ),
        textDirection: TextDirection.ltr,
      )..layout(maxWidth: left - 5);
      text.paint(canvas, Offset(0, y - text.height / 2));
    }

    final slot = width / values.length;
    for (var index = 0; index < values.length; index++) {
      final center = left + slot * (index + .5);
      final barHeight = values[index] <= 0
          ? 0.0
          : height * values[index] / maxValue;
      if (barHeight > 0) {
        final barWidth = math.min(8.0, slot * .18);
        canvas.drawRRect(
          RRect.fromRectAndRadius(
            Rect.fromLTWH(
              center - barWidth / 2,
              top + height - barHeight,
              barWidth,
              barHeight,
            ),
            const Radius.circular(5),
          ),
          Paint()..color = accent,
        );
      } else {
        canvas.drawCircle(
          Offset(center, top + height),
          4.5,
          Paint()..color = Colors.white,
        );
        canvas.drawCircle(
          Offset(center, top + height),
          2.5,
          Paint()..color = accent,
        );
      }
      final label = TextPainter(
        text: TextSpan(
          text: '${values[index]} ag.',
          style: TextStyle(
            color: ink,
            fontSize: 11,
            fontWeight: FontWeight.w700,
          ),
        ),
        textDirection: TextDirection.ltr,
      )..layout(maxWidth: slot);
      final labelY = math.max(0.0, top + height - barHeight - label.height - 4);
      label.paint(canvas, Offset(center - label.width / 2, labelY));
    }
  }

  @override
  bool shouldRepaint(covariant _ReportColumnPainter oldDelegate) =>
      oldDelegate.values != values ||
      oldDelegate.accent != accent ||
      oldDelegate.grid != grid;
}

class _ReportDonutPainter extends CustomPainter {
  const _ReportDonutPainter({
    required this.slices,
    required this.emptyColor,
    required this.centerColor,
  });

  final List<_StatusSlice> slices;
  final Color emptyColor;
  final Color centerColor;

  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = math.min(size.width, size.height) / 2 - 14;
    final rect = Rect.fromCircle(center: center, radius: radius);
    const stroke = 18.0;
    final total = slices.fold<int>(0, (sum, item) => sum + item.value);
    if (total <= 0) {
      canvas.drawArc(
        rect,
        0,
        math.pi * 2,
        false,
        Paint()
          ..color = emptyColor
          ..style = PaintingStyle.stroke
          ..strokeWidth = stroke,
      );
    } else {
      var start = -math.pi / 2;
      for (final slice in slices) {
        final sweep = math.pi * 2 * slice.value / total;
        canvas.drawArc(
          rect,
          start + .018,
          math.max(0, sweep - .036),
          false,
          Paint()
            ..color = slice.color
            ..style = PaintingStyle.stroke
            ..strokeWidth = stroke
            ..strokeCap = StrokeCap.round,
        );
        start += sweep;
      }
    }
    final number = TextPainter(
      text: TextSpan(
        text: '$total',
        style: TextStyle(
          color: centerColor,
          fontSize: 21,
          fontWeight: FontWeight.w800,
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    number.paint(
      canvas,
      center - Offset(number.width / 2, number.height / 2 + 6),
    );
    final caption = TextPainter(
      text: TextSpan(
        text: 'total',
        style: TextStyle(
          color: centerColor.withValues(alpha: .58),
          fontSize: 9,
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    caption.paint(canvas, center - Offset(caption.width / 2, -10));
  }

  @override
  bool shouldRepaint(covariant _ReportDonutPainter oldDelegate) =>
      oldDelegate.slices != slices || oldDelegate.emptyColor != emptyColor;
}

class _ReportsSnapshot {
  const _ReportsSnapshot({
    required this.periodStart,
    required this.periodEnd,
    required this.appointments,
    required this.doneCount,
    required this.lostCount,
    required this.revenue,
    required this.averageTicket,
    required this.conclusion,
    required this.days,
    required this.statusSlices,
    required this.insights,
    required this.services,
    required this.professionals,
  });

  final DateTime periodStart;
  final DateTime periodEnd;
  final List<Appointment> appointments;
  final int doneCount;
  final int lostCount;
  final double revenue;
  final double averageTicket;
  final double conclusion;
  final List<_ReportDay> days;
  final List<_StatusSlice> statusSlices;
  final List<_Insight> insights;
  final List<_ServiceSummary> services;
  final List<_ProfessionalSummary> professionals;

  factory _ReportsSnapshot.from(
    AgendaController controller,
    AgendaThemeTokens t,
  ) {
    final selected = DateUtils.dateOnly(controller.selectedDate);
    final start = selected.subtract(const Duration(days: 6));
    final endExclusive = selected.add(const Duration(days: 1));
    final appointments =
        controller.data.appointments
            .where(
              (item) =>
                  _isBetween(item.start, start, endExclusive) &&
                  item.status != AppointmentStatus.blocked,
            )
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start));
    final done = appointments
        .where((item) => item.status == AppointmentStatus.done)
        .toList();
    final doneCount = done.length;
    final lostCount = appointments
        .where(
          (item) =>
              item.status == AppointmentStatus.cancelled ||
              item.status == AppointmentStatus.noShow,
        )
        .length;
    final serviceRevenue = done.fold<double>(
      0,
      (sum, item) => sum + item.price,
    );
    final revenue = controller.revenueBetween(start, endExclusive);
    final conclusion = appointments.isEmpty
        ? 0.0
        : doneCount * 100 / appointments.length;
    final days = List<_ReportDay>.generate(7, (index) {
      final day = start.add(Duration(days: index));
      return _ReportDay(
        day: day,
        count: appointments
            .where((item) => DateUtils.isSameDay(item.start, day))
            .length,
      );
    });

    final statusCounts = <String, int>{};
    for (final appointment in appointments) {
      final label = _statusLabel(appointment.status);
      statusCounts[label] = (statusCounts[label] ?? 0) + 1;
    }
    final statusRows = statusCounts.entries.toList()
      ..sort((a, b) => b.value.compareTo(a.value));
    const palette = <Color>[
      Color(0xFFC99A2E),
      Color(0xFF16A34A),
      Color(0xFF7C3AED),
      Color(0xFFDC2626),
      Color(0xFF0EA5E9),
      Color(0xFFF59E0B),
      Color(0xFF64748B),
    ];
    final statusSlices = <_StatusSlice>[
      for (var index = 0; index < statusRows.length; index++)
        _StatusSlice(
          label: statusRows[index].key,
          value: statusRows[index].value,
          color: palette[index % palette.length],
        ),
    ];

    final activeAppointments = appointments
        .where(
          (item) =>
              item.status != AppointmentStatus.cancelled &&
              item.status != AppointmentStatus.noShow,
        )
        .toList();
    final busyMinutes = activeAppointments.fold<int>(
      0,
      (sum, item) => sum + item.durationMinutes,
    );
    final professionalCount = math.max(1, controller.data.professionals.length);
    final workMinutesPerDay =
        math.max(
          1,
          controller.data.settings.workdayEndHour -
              controller.data.settings.workdayStartHour,
        ) *
        60;
    final capacityMinutes = math.max(
      1,
      professionalCount * workMinutesPerDay * 7,
    );
    final occupancy = math.min(100.0, busyMinutes * 100 / capacityMinutes);
    final productSales = controller.data.productSales
        .where((item) => _isBetween(item.soldAt, start, endExclusive))
        .toList();
    final manualPayments = controller.data.manualPayments
        .where((item) => _isBetween(item.paidAt, start, endExclusive))
        .toList();
    final uniqueCustomers = appointments
        .where(
          (item) =>
              item.status == AppointmentStatus.done &&
              item.customerName.trim().isNotEmpty,
        )
        .map((item) => item.customerName.trim().toLowerCase())
        .toSet()
        .length;

    _BestDay? bestDay;
    for (final day in days) {
      if (day.count == 0) continue;
      final dayRevenue = controller.revenueBetween(
        day.day,
        day.day.add(const Duration(days: 1)),
      );
      if (bestDay == null ||
          day.count > bestDay.count ||
          (day.count == bestDay.count && dayRevenue > bestDay.revenue)) {
        bestDay = _BestDay(day: day.day, count: day.count, revenue: dayRevenue);
      }
    }
    final ticket = doneCount == 0 ? 0.0 : serviceRevenue / doneCount;
    final insights = <_Insight>[
      _Insight(
        title: 'Melhor dia',
        detail: bestDay == null
            ? 'Sem movimento no período'
            : '${bestDay.count} agendamento(s) | ${money(bestDay.revenue, cents: false)}',
        badge: bestDay == null ? '-' : '${_weekday(bestDay.day)}.',
        icon: Icons.calendar_month_outlined,
        tone: t.accent,
        background: t.accentSoft,
      ),
      _Insight(
        title: 'Ocupação estimada',
        detail:
            '${(busyMinutes / 60).toStringAsFixed(0)}h em $professionalCount profissional(is)',
        badge: '${occupancy.toStringAsFixed(0)}%',
        icon: Icons.badge_outlined,
        tone: t.accent,
        background: t.blueSoft,
      ),
      _Insight(
        title: 'Clientes atendidos',
        detail: 'Clientes únicos com atendimento finalizado',
        badge: '$uniqueCustomers',
        icon: Icons.groups_outlined,
        tone: t.ink,
        background: t.graySoft,
      ),
      _Insight(
        title: 'Produtos vendidos',
        detail: '${productSales.length} venda(s) no período',
        badge: money(
          productSales.fold<double>(0, (sum, item) => sum + item.total),
          cents: false,
        ),
        icon: Icons.inventory_2_outlined,
        tone: t.accent,
        background: t.warmSoft,
      ),
      _Insight(
        title: 'Pagamentos avulsos',
        detail: '${manualPayments.length} recebimento(s) manual(is)',
        badge: money(
          manualPayments.fold<double>(0, (sum, item) => sum + item.value),
          cents: false,
        ),
        icon: Icons.account_balance_wallet_outlined,
        tone: t.accent,
        background: t.accentSoft,
      ),
      _Insight(
        title: 'Saúde da operação',
        detail:
            'Ticket ${money(ticket, cents: false)} | conclusão ${conclusion.toStringAsFixed(0)}%',
        badge: conclusion >= 70 ? 'Boa' : 'Atenção',
        icon: Icons.check_circle_outline_rounded,
        tone: conclusion >= 70 ? t.accent : t.ink,
        background: conclusion >= 70 ? t.blueSoft : t.yellowSoft,
      ),
    ];

    final serviceGroups = <String, List<Appointment>>{};
    final professionalGroups = <String, List<Appointment>>{};
    for (final appointment in appointments.where(
      (item) =>
          item.status != AppointmentStatus.cancelled &&
          item.status != AppointmentStatus.noShow,
    )) {
      if (appointment.serviceName.trim().isNotEmpty) {
        serviceGroups
            .putIfAbsent(appointment.serviceName.trim(), () => [])
            .add(appointment);
      }
      if (appointment.professionalName.trim().isNotEmpty) {
        professionalGroups
            .putIfAbsent(appointment.professionalName.trim(), () => [])
            .add(appointment);
      }
    }
    final services =
        serviceGroups.entries
            .map(
              (entry) => _ServiceSummary(
                name: entry.key,
                count: entry.value.length,
                revenue: entry.value
                    .where((item) => item.status == AppointmentStatus.done)
                    .fold<double>(0, (sum, item) => sum + item.price),
              ),
            )
            .toList()
          ..sort((a, b) {
            final count = b.count.compareTo(a.count);
            return count != 0 ? count : b.revenue.compareTo(a.revenue);
          });
    final limitedServices = services.take(6).toList();
    if (limitedServices.isEmpty) {
      limitedServices.add(
        const _ServiceSummary(name: 'Nenhum serviço', empty: true),
      );
    }

    final professionals =
        professionalGroups.entries.map((entry) {
          final completed = entry.value.where(
            (item) => item.status == AppointmentStatus.done,
          );
          return _ProfessionalSummary(
            name: entry.key,
            count: entry.value.length,
            done: completed.length,
            revenue: completed.fold<double>(0, (sum, item) => sum + item.price),
          );
        }).toList()..sort((a, b) {
          final count = b.count.compareTo(a.count);
          return count != 0 ? count : b.revenue.compareTo(a.revenue);
        });
    final limitedProfessionals = professionals.take(8).toList();
    if (limitedProfessionals.isEmpty) {
      limitedProfessionals.add(
        const _ProfessionalSummary(
          name: 'Nenhum profissional no período',
          empty: true,
        ),
      );
    }

    return _ReportsSnapshot(
      periodStart: start,
      periodEnd: selected,
      appointments: appointments,
      doneCount: doneCount,
      lostCount: lostCount,
      revenue: revenue,
      averageTicket: ticket,
      conclusion: conclusion,
      days: days,
      statusSlices: statusSlices,
      insights: insights,
      services: limitedServices,
      professionals: limitedProfessionals,
    );
  }

  String get topStatusLabel =>
      statusSlices.isEmpty ? '-' : statusSlices.first.label;

  String get summary =>
      '''
RELATÓRIO AGENDA LIVRE
Período: ${_dayMonth(periodStart)} a ${_dayMonth(periodEnd)}

Agendamentos: ${appointments.length}
Finalizados: $doneCount
Cancelados/faltas: $lostCount
Receita: ${money(revenue)}
Ticket médio: ${money(averageTicket)}
Conclusão: ${conclusion.toStringAsFixed(0)}%

Serviços mais realizados:
${services.where((item) => !item.empty).map((item) => '- ${item.name}: ${item.count} atendimento(s), ${money(item.revenue)}').join('\n')}

Profissionais:
${professionals.where((item) => !item.empty).map((item) => '- ${item.name}: ${item.done} finalizado(s), ${money(item.revenue)}').join('\n')}
''';

  String get csv {
    final rows = <String>[
      'Métrica;Valor',
      'Período;${_dayMonth(periodStart)} a ${_dayMonth(periodEnd)}',
      'Agendamentos;${appointments.length}',
      'Finalizados;$doneCount',
      'Cancelados/faltas;$lostCount',
      'Receita;${revenue.toStringAsFixed(2).replaceAll('.', ',')}',
      'Ticket médio;${averageTicket.toStringAsFixed(2).replaceAll('.', ',')}',
      'Conclusão;${conclusion.toStringAsFixed(0)}%',
    ];
    return rows.join('\n');
  }
}

class _ReportDay {
  const _ReportDay({required this.day, required this.count});

  final DateTime day;
  final int count;
}

class _StatusSlice {
  const _StatusSlice({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final int value;
  final Color color;
}

class _Insight {
  const _Insight({
    required this.title,
    required this.detail,
    required this.badge,
    required this.icon,
    required this.tone,
    required this.background,
  });

  final String title;
  final String detail;
  final String badge;
  final IconData icon;
  final Color tone;
  final Color background;
}

class _ServiceSummary {
  const _ServiceSummary({
    required this.name,
    this.count = 0,
    this.revenue = 0,
    this.empty = false,
  });

  final String name;
  final int count;
  final double revenue;
  final bool empty;
}

class _ProfessionalSummary {
  const _ProfessionalSummary({
    required this.name,
    this.count = 0,
    this.done = 0,
    this.revenue = 0,
    this.empty = false,
  });

  final String name;
  final int count;
  final int done;
  final double revenue;
  final bool empty;
}

class _BestDay {
  const _BestDay({
    required this.day,
    required this.count,
    required this.revenue,
  });

  final DateTime day;
  final int count;
  final double revenue;
}

bool _isBetween(DateTime value, DateTime start, DateTime end) =>
    !value.isBefore(start) && value.isBefore(end);

String _weekday(DateTime date) =>
    const ['seg', 'ter', 'qua', 'qui', 'sex', 'sáb', 'dom'][date.weekday - 1];

String _dayMonth(DateTime date) =>
    '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}';

String _averageText(double value) => value > 0 && value < 1
    ? value.toStringAsFixed(1).replaceAll('.', ',')
    : value.toStringAsFixed(0);

String _statusLabel(AppointmentStatus status) => switch (status) {
  AppointmentStatus.scheduled => 'Agendado',
  AppointmentStatus.confirmed => 'Confirmado',
  AppointmentStatus.waiting => 'Aguardando',
  AppointmentStatus.inService => 'Em atendimento',
  AppointmentStatus.done => 'Finalizado',
  AppointmentStatus.cancelled => 'Cancelado',
  AppointmentStatus.noShow => 'Faltou',
  AppointmentStatus.blocked => 'Bloqueado',
};
