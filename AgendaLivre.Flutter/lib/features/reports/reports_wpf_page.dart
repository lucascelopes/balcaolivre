import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../domain/models/models.dart';
import 'reports_mobile_option1.dart';

enum _ReportPeriodMode { day, week, month }

class WpfReportsPage extends StatefulWidget {
  const WpfReportsPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  State<WpfReportsPage> createState() => _WpfReportsPageState();
}

class _WpfReportsPageState extends State<WpfReportsPage> {
  _ReportPeriodMode _mode = _ReportPeriodMode.month;
  _ReportPeriodMode _mobileMode = _ReportPeriodMode.week;
  DateTime? _selectedMovementDay;
  bool _exporting = false;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        return ColoredBox(
          color: const Color(0xFFFAF9F7),
          child: LayoutBuilder(
            builder: (context, constraints) {
              final mobile = constraints.maxWidth < 760;
              final snapshot = _WpfReportSnapshot.from(
                widget.controller,
                mobile ? _mobileMode : _mode,
              );
              if (mobile) {
                return ReportsMobileOptionOne(
                  controller: widget.controller,
                  period: switch (_mobileMode) {
                    _ReportPeriodMode.day => ReportsMobilePeriod.day,
                    _ReportPeriodMode.week => ReportsMobilePeriod.week,
                    _ReportPeriodMode.month => ReportsMobilePeriod.month,
                  },
                  onPeriodChanged: (period) {
                    setState(() {
                      _mobileMode = switch (period) {
                        ReportsMobilePeriod.day => _ReportPeriodMode.day,
                        ReportsMobilePeriod.week => _ReportPeriodMode.week,
                        ReportsMobilePeriod.month => _ReportPeriodMode.month,
                      };
                    });
                  },
                  onCopy: () => _copyMobileSummary(snapshot),
                  onExport: () => _exportPdf(snapshot),
                  exporting: _exporting,
                  legacyGoalText: '',
                );
              }
              final desktop = constraints.maxWidth >= 980;
              return SingleChildScrollView(
                padding: EdgeInsets.fromLTRB(
                  desktop ? 18 : 14,
                  18,
                  desktop ? 18 : 14,
                  48,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _header(context, snapshot, desktop: desktop),
                    const SizedBox(height: 14),
                    _periodControls(context, snapshot, desktop: desktop),
                    const SizedBox(height: 10),
                    _metricStrip(context, snapshot, desktop: desktop),
                    const SizedBox(height: 12),
                    if (desktop)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            flex: 7,
                            child: _movementCard(context, snapshot),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            flex: 4,
                            child: _highlightsCard(context, snapshot),
                          ),
                        ],
                      )
                    else ...[
                      _movementCard(context, snapshot),
                      const SizedBox(height: 12),
                      _highlightsCard(context, snapshot),
                    ],
                  ],
                ),
              );
            },
          ),
        );
      },
    );
  }

  Future<void> _copyMobileSummary(_WpfReportSnapshot snapshot) async {
    final text = [
      'Relatório • ${snapshot.periodLabel}',
      '${snapshot.appointments.length} agendamentos',
      '${snapshot.completed} realizados',
      '${snapshot.cancellations} cancelados ou faltas',
      '${money(snapshot.revenue)} recebidos',
    ].join('\n');
    await Clipboard.setData(ClipboardData(text: text));
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        const SnackBar(content: Text('Resumo copiado para compartilhar.')),
      );
  }

  Widget _header(
    BuildContext context,
    _WpfReportSnapshot snapshot, {
    required bool desktop,
  }) {
    final t = AgendaThemeTokens.of(context);
    final heading = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'RELATÓRIOS',
              style: TextStyle(
                color: t.accent,
                fontSize: 10,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(width: 12),
            Container(width: 44, height: 1, color: t.accent),
          ],
        ),
        const SizedBox(height: 5),
        Text(
          'Relatórios',
          style: TextStyle(
            color: t.ink,
            fontSize: 28,
            height: 1,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 5),
        Text(
          'Entenda a jornada dos agendamentos e onde melhorar.',
          style: TextStyle(color: t.muted, fontSize: 12.5),
        ),
      ],
    );
    final actions = Wrap(
      spacing: 10,
      runSpacing: 8,
      children: [
        OutlinedButton.icon(
          key: const Key('reports-print-pdf'),
          onPressed: _exporting ? null : () => _printPdf(snapshot),
          icon: const Icon(Icons.print_outlined, size: 17),
          label: const Text('Imprimir'),
        ),
        OutlinedButton.icon(
          key: const Key('reports-export-pdf'),
          onPressed: _exporting ? null : () => _exportPdf(snapshot),
          icon: _exporting
              ? const SizedBox.square(
                  dimension: 15,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.download_rounded, size: 17),
          label: const Text('Exportar'),
        ),
      ],
    );
    return desktop
        ? Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Expanded(child: heading),
              const SizedBox(width: 16),
              actions,
            ],
          )
        : Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              heading,
              const SizedBox(height: 14),
              Align(alignment: Alignment.centerLeft, child: actions),
            ],
          );
  }

  Widget _periodControls(
    BuildContext context,
    _WpfReportSnapshot snapshot, {
    required bool desktop,
  }) {
    final t = AgendaThemeTokens.of(context);
    final selector = Container(
      height: 42,
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(9),
      ),
      clipBehavior: Clip.antiAlias,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _periodButton('Dia', _ReportPeriodMode.day),
          _periodButton('Semana', _ReportPeriodMode.week),
          _periodButton('Mês', _ReportPeriodMode.month),
        ],
      ),
    );
    final period = Container(
      height: 42,
      padding: const EdgeInsets.symmetric(horizontal: 13),
      decoration: BoxDecoration(
        color: t.accentSoft,
        border: Border.all(color: const Color(0xFFF4D4C3)),
        borderRadius: BorderRadius.circular(9),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.calendar_month_outlined, color: t.accent, size: 17),
          const SizedBox(width: 8),
          Flexible(
            child: Text(
              snapshot.periodLabel,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: t.ink,
                fontSize: 13,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
    if (!desktop) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [selector, const SizedBox(height: 8), period],
      );
    }
    return Row(
      children: [
        selector,
        const SizedBox(width: 12),
        period,
        const Spacer(),
        Text(
          'Dados atualizados automaticamente',
          style: TextStyle(color: t.muted, fontSize: 10.5),
        ),
      ],
    );
  }

  Widget _periodButton(String label, _ReportPeriodMode mode) {
    final t = AgendaThemeTokens.of(context);
    final selected = _mode == mode;
    return SizedBox(
      width: MediaQuery.sizeOf(context).width < 440 ? 98 : 122,
      height: 40,
      child: TextButton(
        onPressed: selected ? null : () => setState(() => _mode = mode),
        style: TextButton.styleFrom(
          foregroundColor: selected ? Colors.white : t.ink,
          backgroundColor: selected ? t.accent : Colors.white,
          disabledForegroundColor: Colors.white,
          shape: const RoundedRectangleBorder(),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
          ),
        ),
      ),
    );
  }

  Widget _metricStrip(
    BuildContext context,
    _WpfReportSnapshot snapshot, {
    required bool desktop,
  }) {
    final metrics = [
      _Metric(
        Icons.groups_outlined,
        'Atendimentos',
        '${snapshot.appointments.length}',
        switch (_mode) {
          _ReportPeriodMode.day => 'no dia',
          _ReportPeriodMode.week => 'na semana',
          _ReportPeriodMode.month => 'no mês',
        },
      ),
      _Metric(
        Icons.payments_outlined,
        'Receita',
        money(snapshot.revenue, cents: false),
        'recebida',
      ),
      _Metric(
        Icons.local_activity_outlined,
        'Ticket médio',
        money(snapshot.ticket, cents: false),
        'por finalizado',
      ),
      _Metric(
        Icons.fact_check_outlined,
        'Taxa de presença',
        '${snapshot.attendanceRate.round()}%',
        'comparecimento',
      ),
      _Metric(
        Icons.event_busy_outlined,
        'Cancelamentos',
        '${snapshot.cancellations}',
        'no período',
      ),
    ];
    final t = AgendaThemeTokens.of(context);
    return Container(
      constraints: BoxConstraints(minHeight: desktop ? 94 : 0),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(12),
        boxShadow: const [
          BoxShadow(
            color: Color(0x10000000),
            blurRadius: 5,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: desktop
          ? Row(
              children: [
                for (var index = 0; index < metrics.length; index++) ...[
                  Expanded(child: _metricItem(context, metrics[index])),
                  if (index < metrics.length - 1)
                    Container(width: 1, height: 54, color: t.line),
                ],
              ],
            )
          : Wrap(
              children: [
                for (final metric in metrics)
                  SizedBox(
                    width: (MediaQuery.sizeOf(context).width - 30) / 2,
                    child: _metricItem(context, metric),
                  ),
              ],
            ),
    );
  }

  Widget _metricItem(BuildContext context, _Metric metric) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      key: Key('reports-metric-${metric.label}'),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      child: Row(
        children: [
          CircleAvatar(
            radius: 18,
            backgroundColor: t.accentSoft,
            foregroundColor: t.accent,
            child: Icon(metric.icon, size: 18),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  metric.label,
                  style: TextStyle(color: t.muted, fontSize: 10.5),
                ),
                Text(
                  metric.value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 21,
                    height: 1.05,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                Text(
                  metric.hint,
                  style: TextStyle(color: t.muted, fontSize: 9),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _movementCard(BuildContext context, _WpfReportSnapshot snapshot) {
    final t = AgendaThemeTokens.of(context);
    return _surface(
      context,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  switch (_mode) {
                    _ReportPeriodMode.day => 'Movimento no dia',
                    _ReportPeriodMode.week => 'Movimento na semana',
                    _ReportPeriodMode.month => 'Movimento no mês',
                  },
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              if (_mode == _ReportPeriodMode.month) _intensityLegend(t),
            ],
          ),
          const SizedBox(height: 2),
          Text(switch (_mode) {
            _ReportPeriodMode.day =>
              'Veja em quais horários sua agenda ficou mais movimentada.',
            _ReportPeriodMode.week =>
              'Compare rapidamente o movimento de cada dia.',
            _ReportPeriodMode.month =>
              'Quanto mais forte o laranja, maior o movimento.',
          }, style: TextStyle(color: t.muted, fontSize: 10)),
          const SizedBox(height: 12),
          _movementGrid(context, snapshot),
        ],
      ),
    );
  }

  Widget _intensityLegend(AgendaThemeTokens t) {
    const colors = [
      Color(0xFFFFF1E9),
      Color(0xFFFAD0BB),
      Color(0xFFF59A69),
      Color(0xFFED6823),
    ];
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text('Menos', style: TextStyle(color: t.muted, fontSize: 9)),
        const SizedBox(width: 5),
        for (final color in colors)
          Container(
            width: 12,
            height: 12,
            margin: const EdgeInsets.only(right: 3),
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(3),
            ),
          ),
        const SizedBox(width: 2),
        Text('Mais', style: TextStyle(color: t.muted, fontSize: 9)),
      ],
    );
  }

  Widget _movementGrid(BuildContext context, _WpfReportSnapshot snapshot) {
    final headers = snapshot.movementHeaders;
    final cells = snapshot.movementCells;
    final rowHeight = _mode == _ReportPeriodMode.month ? 48.0 : 72.0;
    return Column(
      children: [
        Row(
          children: [
            for (final header in headers)
              Expanded(
                child: Container(
                  height: 32,
                  alignment: Alignment.center,
                  color: const Color(0xFFFBF9F7),
                  child: Text(
                    header,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
          ],
        ),
        GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: cells.length,
          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: headers.length,
            mainAxisExtent: rowHeight,
          ),
          itemBuilder: (context, index) {
            final cell = cells[index];
            final selected =
                cell.day != null &&
                _selectedMovementDay != null &&
                DateUtils.isSameDay(cell.day, _selectedMovementDay);
            final background = selected
                ? AgendaThemeTokens.of(context).accent
                : switch (cell.count) {
                    <= 0 => cell.muted ? const Color(0xFFFBF9F7) : Colors.white,
                    1 => const Color(0xFFFFF1E9),
                    2 => const Color(0xFFFAD0BB),
                    3 => const Color(0xFFF59A69),
                    _ => AgendaThemeTokens.of(context).accent,
                  };
            final lightText = selected || cell.count >= 4;
            return Tooltip(
              message:
                  '${cell.detail}\nReceita: ${money(cell.revenue, cents: false)}',
              child: InkWell(
                onTap: cell.day == null
                    ? null
                    : () => _showDayDetails(snapshot, cell.day!),
                child: Container(
                  decoration: BoxDecoration(
                    color: background,
                    border: Border.all(
                      color: selected
                          ? AgendaThemeTokens.of(context).accent
                          : AgendaThemeTokens.of(context).line,
                    ),
                  ),
                  alignment: Alignment.center,
                  padding: const EdgeInsets.all(3),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        cell.label,
                        style: TextStyle(
                          color: cell.muted
                              ? const Color(0xFFBDB6B0)
                              : lightText
                              ? Colors.white
                              : AgendaThemeTokens.of(context).ink,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        cell.count == 0 ? '—' : '${cell.count}',
                        style: TextStyle(
                          color: cell.muted
                              ? const Color(0xFFBDB6B0)
                              : lightText
                              ? Colors.white
                              : AgendaThemeTokens.of(context).ink,
                          fontSize: 9,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        ),
      ],
    );
  }

  Widget _highlightsCard(BuildContext context, _WpfReportSnapshot snapshot) {
    final t = AgendaThemeTokens.of(context);
    final highlights = [
      (Icons.star_outline_rounded, 'Melhor dia', snapshot.bestDay),
      (Icons.schedule_outlined, 'Horário mais ocupado', snapshot.bestTime),
      (
        Icons.content_cut_rounded,
        'Serviço mais realizado',
        snapshot.topService,
      ),
      (Icons.wb_twilight_outlined, 'Dia mais tranquilo', snapshot.quietDay),
    ];
    return _surface(
      context,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Destaques do período',
            style: TextStyle(
              color: t.ink,
              fontSize: 17,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            'O que mais marcou seus resultados.',
            style: TextStyle(color: t.muted, fontSize: 10),
          ),
          const SizedBox(height: 9),
          for (var index = 0; index < highlights.length; index++) ...[
            _highlightRow(
              context,
              icon: highlights[index].$1,
              label: highlights[index].$2,
              value: highlights[index].$3,
            ),
            if (index < highlights.length - 1)
              Divider(height: 1, color: t.line),
          ],
          const SizedBox(height: 10),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 11),
            decoration: BoxDecoration(
              color: t.accentSoft,
              border: Border.all(color: t.accent),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                Icon(Icons.trending_up_rounded, color: t.accent, size: 20),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    snapshot.trend,
                    style: TextStyle(
                      color: t.accent,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
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

  Widget _highlightRow(
    BuildContext context, {
    required IconData icon,
    required String label,
    required String value,
  }) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      height: 68,
      child: Row(
        children: [
          CircleAvatar(
            radius: 19,
            backgroundColor: t.accentSoft,
            foregroundColor: t.accent,
            child: Icon(icon, size: 19),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(color: t.muted, fontSize: 10)),
                const SizedBox(height: 2),
                Text(
                  value,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
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

  Widget _surface(BuildContext context, {required Widget child}) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(14),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0D000000),
            blurRadius: 6,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: child,
    );
  }

  Future<void> _showDayDetails(
    _WpfReportSnapshot snapshot,
    DateTime day,
  ) async {
    setState(() => _selectedMovementDay = day);
    final items =
        widget.controller.data.appointments
            .where(
              (item) =>
                  DateUtils.isSameDay(item.start, day) &&
                  item.status != AppointmentStatus.blocked,
            )
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start));
    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) {
        final t = AgendaThemeTokens.of(dialogContext);
        return AlertDialog(
          title: Text(_fullPeriodDay(day)),
          content: SizedBox(
            width: 420,
            child: items.isEmpty
                ? const Text('Nenhum atendimento neste dia.')
                : ListView.separated(
                    shrinkWrap: true,
                    itemCount: items.length,
                    separatorBuilder: (_, _) => Divider(color: t.line),
                    itemBuilder: (context, index) {
                      final item = items[index];
                      return ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: CircleAvatar(
                          backgroundColor: t.accentSoft,
                          foregroundColor: t.accent,
                          child: Text(hour(item.start)),
                        ),
                        title: Text(item.customerName),
                        subtitle: Text(
                          '${item.serviceName} · ${item.professionalName}',
                        ),
                        trailing: Text(money(item.price, cents: false)),
                      );
                    },
                  ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Fechar'),
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.of(dialogContext).pop();
                widget.controller
                  ..selectDate(day)
                  ..navigate(AgendaPage.agenda);
              },
              child: const Text('Abrir na agenda'),
            ),
          ],
        );
      },
    );
  }

  Future<void> _printPdf(_WpfReportSnapshot snapshot) async {
    setState(() => _exporting = true);
    try {
      final bytes = await _buildPdf(snapshot);
      await Printing.layoutPdf(
        name: _pdfFileName(snapshot),
        onLayout: (_) async => bytes,
      );
    } on Object {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Não foi possível imprimir o relatório.')),
      );
    } finally {
      if (mounted) setState(() => _exporting = false);
    }
  }

  Future<void> _exportPdf(_WpfReportSnapshot snapshot) async {
    setState(() => _exporting = true);
    try {
      final bytes = await _buildPdf(snapshot);
      await Printing.sharePdf(bytes: bytes, filename: _pdfFileName(snapshot));
    } on Object {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Não foi possível exportar o PDF.')),
      );
    } finally {
      if (mounted) setState(() => _exporting = false);
    }
  }

  String _pdfFileName(_WpfReportSnapshot snapshot) {
    final date = DateFormat('yyyy-MM-dd').format(snapshot.start);
    return 'agenda-livre-relatorio-$date.pdf';
  }

  Future<Uint8List> _buildPdf(_WpfReportSnapshot snapshot) async {
    final fontData = await rootBundle.load(
      'assets/fonts/LibreBaskerville-Variable.ttf',
    );
    final font = pw.Font.ttf(fontData);
    final document = pw.Document(
      title: 'Relatório - ${widget.controller.businessName}',
      author: 'Agenda Livre',
      subject: snapshot.periodLabel,
    );
    final accent = PdfColor.fromHex('#ED6823');
    final ink = PdfColor.fromHex('#181512');
    final muted = PdfColor.fromHex('#69605A');
    final soft = PdfColor.fromHex('#FFF1E9');
    final line = PdfColor.fromHex('#E5DCD6');

    document.addPage(
      pw.MultiPage(
        pageTheme: pw.PageTheme(
          pageFormat: PdfPageFormat.a4,
          margin: const pw.EdgeInsets.all(30),
          theme: pw.ThemeData.withFont(base: font, bold: font),
          buildBackground: (_) => pw.FullPage(
            ignoreMargins: true,
            child: pw.Container(color: PdfColor.fromHex('#FFFDFB')),
          ),
        ),
        header: (_) => pw.Row(
          mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
          children: [
            pw.Row(
              children: [
                pw.Container(
                  width: 26,
                  height: 26,
                  alignment: pw.Alignment.center,
                  decoration: pw.BoxDecoration(
                    color: accent,
                    borderRadius: pw.BorderRadius.circular(7),
                  ),
                  child: pw.Text(
                    'AL',
                    style: pw.TextStyle(
                      color: PdfColors.white,
                      fontWeight: pw.FontWeight.bold,
                      fontSize: 9,
                    ),
                  ),
                ),
                pw.SizedBox(width: 8),
                pw.Text(
                  'agenda livre',
                  style: pw.TextStyle(
                    color: ink,
                    fontWeight: pw.FontWeight.bold,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
            pw.Text(
              'Gerado em ${DateFormat('dd/MM/yyyy').format(DateTime.now())}',
              style: pw.TextStyle(color: muted, fontSize: 8),
            ),
          ],
        ),
        footer: (context) => pw.Row(
          mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
          children: [
            pw.Text(
              'Agenda Livre · www.balcaolivrepdv.com.br',
              style: pw.TextStyle(color: muted, fontSize: 7),
            ),
            pw.Text(
              '${context.pageNumber}/${context.pagesCount}',
              style: pw.TextStyle(color: muted, fontSize: 7),
            ),
          ],
        ),
        build: (_) => [
          pw.SizedBox(height: 24),
          pw.Text(
            'Relatório ${switch (_mode) {
              _ReportPeriodMode.day => 'do dia',
              _ReportPeriodMode.week => 'semanal',
              _ReportPeriodMode.month => 'mensal',
            }}',
            style: pw.TextStyle(
              color: ink,
              fontSize: 27,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
          pw.SizedBox(height: 5),
          pw.Text(
            widget.controller.businessName,
            style: pw.TextStyle(
              color: accent,
              fontSize: 16,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
          pw.SizedBox(height: 3),
          pw.Text(
            snapshot.periodLabel,
            style: pw.TextStyle(color: ink, fontSize: 9),
          ),
          pw.SizedBox(height: 18),
          pw.Container(
            padding: const pw.EdgeInsets.all(16),
            decoration: pw.BoxDecoration(
              color: PdfColors.white,
              border: pw.Border.all(color: line),
              borderRadius: pw.BorderRadius.circular(10),
            ),
            child: pw.Column(
              crossAxisAlignment: pw.CrossAxisAlignment.start,
              children: [
                pw.Text(
                  'Visão geral',
                  style: pw.TextStyle(
                    color: ink,
                    fontSize: 14,
                    fontWeight: pw.FontWeight.bold,
                  ),
                ),
                pw.SizedBox(height: 12),
                pw.Row(
                  children: [
                    _pdfMetric(
                      'Atendimentos',
                      '${snapshot.appointments.length}',
                      accent,
                      ink,
                      muted,
                      soft,
                    ),
                    _pdfMetric(
                      'Receita recebida',
                      money(snapshot.revenue, cents: false),
                      accent,
                      ink,
                      muted,
                      soft,
                    ),
                    _pdfMetric(
                      'Ticket médio',
                      money(snapshot.ticket, cents: false),
                      accent,
                      ink,
                      muted,
                      soft,
                    ),
                  ],
                ),
                pw.SizedBox(height: 12),
                pw.Row(
                  children: [
                    _pdfMetric(
                      'Comparecimento',
                      '${snapshot.attendanceRate.round()}%',
                      accent,
                      ink,
                      muted,
                      soft,
                    ),
                    _pdfMetric(
                      'Cancelamentos',
                      '${snapshot.cancellations}',
                      accent,
                      ink,
                      muted,
                      soft,
                    ),
                    _pdfMetric(
                      'Concluídos',
                      '${snapshot.completed}',
                      accent,
                      ink,
                      muted,
                      soft,
                    ),
                  ],
                ),
              ],
            ),
          ),
          pw.SizedBox(height: 16),
          pw.Text(
            'Destaques do período',
            style: pw.TextStyle(
              color: ink,
              fontSize: 14,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
          pw.SizedBox(height: 8),
          pw.Table(
            border: pw.TableBorder.all(color: line),
            children: [
              _pdfHighlightRow('Melhor dia', snapshot.bestDay, ink, muted),
              _pdfHighlightRow(
                'Horário mais ocupado',
                snapshot.bestTime,
                ink,
                muted,
              ),
              _pdfHighlightRow(
                'Serviço mais realizado',
                snapshot.topService,
                ink,
                muted,
              ),
              _pdfHighlightRow(
                'Dia mais tranquilo',
                snapshot.quietDay,
                ink,
                muted,
              ),
            ],
          ),
          pw.SizedBox(height: 14),
          pw.Container(
            padding: const pw.EdgeInsets.all(12),
            decoration: pw.BoxDecoration(
              color: soft,
              border: pw.Border.all(color: accent),
              borderRadius: pw.BorderRadius.circular(8),
            ),
            child: pw.Text(
              snapshot.trend,
              style: pw.TextStyle(
                color: accent,
                fontSize: 10,
                fontWeight: pw.FontWeight.bold,
              ),
            ),
          ),
          pw.SizedBox(height: 18),
          pw.Text(
            'Atendimentos do período',
            style: pw.TextStyle(
              color: ink,
              fontSize: 14,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
          pw.SizedBox(height: 8),
          if (snapshot.appointments.isEmpty)
            pw.Text(
              'Nenhum atendimento registrado.',
              style: pw.TextStyle(color: muted, fontSize: 9),
            )
          else
            pw.TableHelper.fromTextArray(
              headers: const [
                'Data',
                'Cliente',
                'Serviço',
                'Profissional',
                'Valor',
              ],
              data: [
                for (final item in snapshot.appointments)
                  [
                    DateFormat('dd/MM HH:mm').format(item.start),
                    item.customerName,
                    item.serviceName,
                    item.professionalName,
                    money(item.price, cents: false),
                  ],
              ],
              headerDecoration: pw.BoxDecoration(color: accent),
              headerStyle: pw.TextStyle(
                color: PdfColors.white,
                fontSize: 8,
                fontWeight: pw.FontWeight.bold,
              ),
              cellStyle: pw.TextStyle(color: ink, fontSize: 7.5),
              cellDecoration: (index, data, rowNum) => pw.BoxDecoration(
                color: rowNum.isEven ? PdfColors.white : soft,
              ),
              border: pw.TableBorder.all(color: line),
            ),
        ],
      ),
    );
    return document.save();
  }

  pw.Widget _pdfMetric(
    String label,
    String value,
    PdfColor accent,
    PdfColor ink,
    PdfColor muted,
    PdfColor soft,
  ) => pw.Expanded(
    child: pw.Container(
      margin: const pw.EdgeInsets.symmetric(horizontal: 3),
      padding: const pw.EdgeInsets.all(10),
      decoration: pw.BoxDecoration(
        color: soft,
        borderRadius: pw.BorderRadius.circular(7),
      ),
      child: pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.Text(label, style: pw.TextStyle(color: muted, fontSize: 7.5)),
          pw.SizedBox(height: 3),
          pw.Text(
            value,
            style: pw.TextStyle(
              color: accent,
              fontSize: 16,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
        ],
      ),
    ),
  );

  pw.TableRow _pdfHighlightRow(
    String label,
    String value,
    PdfColor ink,
    PdfColor muted,
  ) => pw.TableRow(
    children: [
      pw.Padding(
        padding: const pw.EdgeInsets.all(8),
        child: pw.Text(label, style: pw.TextStyle(color: muted, fontSize: 8)),
      ),
      pw.Padding(
        padding: const pw.EdgeInsets.all(8),
        child: pw.Text(
          value,
          style: pw.TextStyle(
            color: ink,
            fontSize: 8,
            fontWeight: pw.FontWeight.bold,
          ),
        ),
      ),
    ],
  );
}

class _Metric {
  const _Metric(this.icon, this.label, this.value, this.hint);

  final IconData icon;
  final String label;
  final String value;
  final String hint;
}

class _MovementCell {
  const _MovementCell({
    required this.label,
    required this.count,
    required this.detail,
    required this.revenue,
    this.day,
    this.muted = false,
  });

  final String label;
  final int count;
  final String detail;
  final double revenue;
  final DateTime? day;
  final bool muted;
}

class _WpfReportSnapshot {
  const _WpfReportSnapshot({
    required this.start,
    required this.end,
    required this.periodLabel,
    required this.appointments,
    required this.completed,
    required this.cancellations,
    required this.revenue,
    required this.ticket,
    required this.attendanceRate,
    required this.movementHeaders,
    required this.movementCells,
    required this.bestDay,
    required this.bestTime,
    required this.topService,
    required this.quietDay,
    required this.trend,
  });

  final DateTime start;
  final DateTime end;
  final String periodLabel;
  final List<Appointment> appointments;
  final int completed;
  final int cancellations;
  final double revenue;
  final double ticket;
  final double attendanceRate;
  final List<String> movementHeaders;
  final List<_MovementCell> movementCells;
  final String bestDay;
  final String bestTime;
  final String topService;
  final String quietDay;
  final String trend;

  factory _WpfReportSnapshot.from(
    AgendaController controller,
    _ReportPeriodMode mode,
  ) {
    final selected = DateUtils.dateOnly(controller.selectedDate);
    late final DateTime start;
    late final DateTime end;
    late final String periodLabel;
    switch (mode) {
      case _ReportPeriodMode.day:
        start = selected;
        end = start.add(const Duration(days: 1));
        periodLabel = _fullPeriodDay(start);
      case _ReportPeriodMode.week:
        start = selected.subtract(
          Duration(days: (selected.weekday - DateTime.monday) % 7),
        );
        end = start.add(const Duration(days: 7));
        periodLabel = start.month == end.subtract(const Duration(days: 1)).month
            ? '${DateFormat('dd').format(start)} a ${DateFormat('dd').format(end.subtract(const Duration(days: 1)))} de ${_monthName(start)}'
            : '${DateFormat('dd/MM').format(start)} a ${DateFormat('dd/MM').format(end.subtract(const Duration(days: 1)))}';
      case _ReportPeriodMode.month:
        start = DateTime(selected.year, selected.month);
        end = DateTime(selected.year, selected.month + 1);
        periodLabel = '${_monthName(start)} de ${start.year}';
    }
    final appointments =
        controller.data.appointments
            .where(
              (item) =>
                  !item.start.isBefore(start) &&
                  item.start.isBefore(end) &&
                  item.status != AppointmentStatus.blocked,
            )
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start));
    final completed = appointments
        .where((item) => item.status == AppointmentStatus.done)
        .length;
    final attended = appointments
        .where(
          (item) =>
              item.status == AppointmentStatus.waiting ||
              item.status == AppointmentStatus.inService ||
              item.status == AppointmentStatus.done,
        )
        .length;
    final attendanceBase = appointments
        .where(
          (item) =>
              item.status == AppointmentStatus.waiting ||
              item.status == AppointmentStatus.inService ||
              item.status == AppointmentStatus.done ||
              item.status == AppointmentStatus.cancelled ||
              item.status == AppointmentStatus.noShow,
        )
        .length;
    final cancellations = appointments
        .where(
          (item) =>
              item.status == AppointmentStatus.cancelled ||
              item.status == AppointmentStatus.noShow,
        )
        .length;
    final revenue = controller.revenueBetween(start, end);
    final ticket = completed == 0 ? 0.0 : revenue / completed;

    final headers = <String>[];
    final cells = <_MovementCell>[];
    if (mode == _ReportPeriodMode.day) {
      const slots = [8, 10, 12, 14, 16, 18];
      headers.addAll(
        slots.map((hour) => '${hour.toString().padLeft(2, '0')}h'),
      );
      for (final slot in slots) {
        final slotEnd = slot == slots.last ? 24 : slot + 2;
        final values = appointments
            .where(
              (item) => item.start.hour >= slot && item.start.hour < slotEnd,
            )
            .toList();
        cells.add(
          _MovementCell(
            label: '${slot.toString().padLeft(2, '0')}h',
            count: values.length,
            detail: _appointmentCount(values.length),
            revenue: _receivedAppointmentRevenue(values),
            day: start,
          ),
        );
      }
    } else if (mode == _ReportPeriodMode.week) {
      for (var offset = 0; offset < 7; offset++) {
        final day = start.add(Duration(days: offset));
        headers.add(_weekdayShort(day));
        final values = appointments
            .where((item) => DateUtils.isSameDay(item.start, day))
            .toList();
        cells.add(
          _MovementCell(
            label: '${day.day}',
            count: values.length,
            detail: _appointmentCount(values.length),
            revenue: controller.revenueBetween(
              day,
              day.add(const Duration(days: 1)),
            ),
            day: day,
          ),
        );
      }
    } else {
      headers.addAll(const [
        'Segunda',
        'Terça',
        'Quarta',
        'Quinta',
        'Sexta',
        'Sábado',
        'Domingo',
      ]);
      final firstCell = start.subtract(Duration(days: start.weekday - 1));
      for (var index = 0; index < 42; index++) {
        final day = firstCell.add(Duration(days: index));
        final inMonth = day.month == start.month;
        final values = inMonth
            ? appointments
                  .where((item) => DateUtils.isSameDay(item.start, day))
                  .toList()
            : <Appointment>[];
        cells.add(
          _MovementCell(
            label: '${day.day}',
            count: values.length,
            detail: inMonth ? _appointmentCount(values.length) : 'Fora do mês',
            revenue: inMonth
                ? controller.revenueBetween(
                    day,
                    day.add(const Duration(days: 1)),
                  )
                : 0,
            day: inMonth ? day : null,
            muted: !inMonth,
          ),
        );
      }
    }

    final active = appointments
        .where(
          (item) =>
              item.status != AppointmentStatus.cancelled &&
              item.status != AppointmentStatus.noShow,
        )
        .toList();
    final byDay = <DateTime, List<Appointment>>{};
    final byHour = <int, List<Appointment>>{};
    final byService = <String, List<Appointment>>{};
    for (final item in active) {
      final day = DateUtils.dateOnly(item.start);
      byDay.putIfAbsent(day, () => []).add(item);
      byHour.putIfAbsent(item.start.hour, () => []).add(item);
      final service = item.serviceName.trim();
      if (service.isNotEmpty) {
        byService.putIfAbsent(service, () => []).add(item);
      }
    }
    final dayEntries = byDay.entries.toList()
      ..sort((a, b) {
        final count = b.value.length.compareTo(a.value.length);
        return count != 0 ? count : a.key.compareTo(b.key);
      });
    final quietEntries = byDay.entries.toList()
      ..sort((a, b) {
        final count = a.value.length.compareTo(b.value.length);
        return count != 0 ? count : a.key.compareTo(b.key);
      });
    final hourEntries = byHour.entries.toList()
      ..sort((a, b) {
        final count = b.value.length.compareTo(a.value.length);
        return count != 0 ? count : a.key.compareTo(b.key);
      });
    final serviceEntries = byService.entries.toList()
      ..sort((a, b) {
        final count = b.value.length.compareTo(a.value.length);
        return count != 0 ? count : a.key.compareTo(b.key);
      });

    final periodLength = end.difference(start);
    final previousStart = start.subtract(periodLength);
    final previousCount = controller.data.appointments
        .where(
          (item) =>
              !item.start.isBefore(previousStart) &&
              item.start.isBefore(start) &&
              item.status != AppointmentStatus.blocked,
        )
        .length;
    final trend = _trendText(appointments.length, previousCount);

    return _WpfReportSnapshot(
      start: start,
      end: end,
      periodLabel: periodLabel,
      appointments: appointments,
      completed: completed,
      cancellations: cancellations,
      revenue: revenue,
      ticket: ticket,
      attendanceRate: attendanceBase == 0 ? 0 : attended * 100 / attendanceBase,
      movementHeaders: headers,
      movementCells: cells,
      bestDay: dayEntries.isEmpty
          ? 'Sem dados no período'
          : '${_weekdayLong(dayEntries.first.key)} · ${_appointmentCount(dayEntries.first.value.length)}',
      bestTime: hourEntries.isEmpty
          ? 'Sem dados no período'
          : '${hourEntries.first.key.toString().padLeft(2, '0')}h às ${((hourEntries.first.key + 1) % 24).toString().padLeft(2, '0')}h · ${_appointmentCount(hourEntries.first.value.length)}',
      topService: serviceEntries.isEmpty
          ? 'Sem dados no período'
          : '${serviceEntries.first.key} · ${serviceEntries.first.value.length} vez(es)',
      quietDay: quietEntries.isEmpty
          ? 'Sem dados no período'
          : '${_weekdayLong(quietEntries.first.key)} · ${_appointmentCount(quietEntries.first.value.length)}',
      trend: trend,
    );
  }
}

double _receivedAppointmentRevenue(Iterable<Appointment> appointments) =>
    appointments
        .where((item) => item.status == AppointmentStatus.done)
        .fold<double>(0, (sum, item) => sum + item.price);

String _appointmentCount(int count) =>
    count == 1 ? '1 atendimento' : '$count atendimentos';

String _monthName(DateTime date) {
  final value = DateFormat('MMMM', 'pt_BR').format(date);
  return value.isEmpty
      ? value
      : '${value[0].toUpperCase()}${value.substring(1)}';
}

String _weekdayLong(DateTime date) {
  final value = DateFormat('EEEE', 'pt_BR').format(date);
  return value.isEmpty
      ? value
      : '${value[0].toUpperCase()}${value.substring(1)}';
}

String _weekdayShort(DateTime date) {
  final value = DateFormat('EEE', 'pt_BR').format(date).replaceAll('.', '');
  return value.isEmpty
      ? value
      : '${value[0].toUpperCase()}${value.substring(1)}';
}

String _fullPeriodDay(DateTime date) =>
    DateFormat("EEEE, dd 'de' MMMM", 'pt_BR')
        .format(date)
        .replaceFirstMapped(
          RegExp(r'^.'),
          (match) => match.group(0)!.toUpperCase(),
        );

String _trendText(int current, int previous) {
  if (current == 0 && previous == 0) {
    return 'Ainda não há movimento para comparar neste período.';
  }
  if (previous == 0) {
    return '$current atendimento(s) neste período; o anterior estava sem movimento.';
  }
  final variation = (current - previous) * 100 / previous;
  if (variation.abs() < 1) {
    return 'Seu movimento ficou estável em relação ao período anterior.';
  }
  return variation > 0
      ? 'Seu movimento está ${variation.round()}% acima do período anterior.'
      : 'Seu movimento está ${variation.abs().round()}% abaixo do período anterior.';
}
