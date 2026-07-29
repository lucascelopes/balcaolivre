import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../domain/models/models.dart';
import '../agenda/appointment_dialog.dart';
import '../agenda/appointment_payment_dialog.dart';
import '../agenda/appointment_visuals.dart';

enum PdvPanelKind { details, edit, timer, products, receive }

class PdvPage extends StatefulWidget {
  const PdvPage({
    super.key,
    required this.controller,
    required this.onExit,
    required this.onNavigate,
    this.referenceNow,
  });

  final AgendaController controller;
  final VoidCallback onExit;
  final ValueChanged<AgendaPage> onNavigate;
  final DateTime? referenceNow;

  @override
  State<PdvPage> createState() => _PdvPageState();
}

class _PdvPageState extends State<PdvPage> {
  late final Timer _timer;
  final _searchController = TextEditingController();
  String? _selectedId;
  PdvPanelKind? _panel;
  bool _weekView = false;

  AgendaController get controller => widget.controller;
  DateTime get _now => widget.referenceNow ?? DateTime.now();

  @override
  void initState() {
    super.initState();
    _selectDefaultAppointment();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _timer.cancel();
    _searchController.dispose();
    super.dispose();
  }

  List<Appointment> get _visibleAppointments {
    final selected = DateUtils.dateOnly(controller.selectedDate);
    final start = _weekView ? _weekStart(selected) : selected;
    final end = start.add(Duration(days: _weekView ? 7 : 1));
    final query = _searchController.text.trim().toLowerCase();
    return controller.data.appointments.where((appointment) {
      if (appointment.status == AppointmentStatus.blocked ||
          appointment.start.isBefore(start) ||
          !appointment.start.isBefore(end)) {
        return false;
      }
      if (query.isEmpty) return true;
      return <String>[
        appointment.customerName,
        appointment.serviceName,
        appointment.professionalName,
      ].any((value) => value.toLowerCase().contains(query));
    }).toList()..sort((a, b) => a.start.compareTo(b.start));
  }

  List<Appointment> get _mobileAppointments {
    final items = List<Appointment>.of(_visibleAppointments);
    int priority(Appointment item) {
      if (item.status == AppointmentStatus.inService) return 0;
      if (!item.end.isBefore(_now)) return 1;
      return 2;
    }

    items.sort((a, b) {
      final byPriority = priority(a).compareTo(priority(b));
      if (byPriority != 0) return byPriority;
      if (priority(a) == 2) return b.start.compareTo(a.start);
      return a.start.compareTo(b.start);
    });
    return items;
  }

  Appointment? get _selectedAppointment {
    final selectedId = _selectedId;
    if (selectedId == null) return null;
    return controller.data.appointments
        .where((item) => item.id == selectedId)
        .firstOrNull;
  }

  void _selectDefaultAppointment() {
    final visible = _visibleAppointments;
    if (visible.isEmpty) {
      _selectedId = null;
      return;
    }
    if (_selectedId != null && visible.any((item) => item.id == _selectedId)) {
      return;
    }
    final running = visible
        .where((item) => item.status == AppointmentStatus.inService)
        .firstOrNull;
    _selectedId = (running ?? visible.first).id;
  }

  void _select(Appointment appointment) {
    setState(() {
      _selectedId = appointment.id;
      _panel ??= PdvPanelKind.details;
    });
  }

  Future<void> _changeDate(int amount) async {
    controller.selectDate(
      controller.selectedDate.add(Duration(days: amount * (_weekView ? 7 : 1))),
    );
    setState(() {
      _selectedId = null;
      _panel = null;
      _selectDefaultAppointment();
    });
  }

  void _goToday() {
    controller.selectDate(DateUtils.dateOnly(_now));
    setState(() {
      _selectedId = null;
      _panel = null;
      _selectDefaultAppointment();
    });
  }

  Future<void> _quickAdd([DateTime? initialStart]) async {
    await showAppointmentDialog(
      context,
      controller,
      initialStart:
          initialStart ?? controller.selectedDate.add(const Duration(hours: 9)),
    );
    if (mounted) setState(_selectDefaultAppointment);
  }

  Future<void> _edit(Appointment appointment) async {
    await showAppointmentDialog(context, controller, appointment: appointment);
    if (mounted) setState(() => _panel = PdvPanelKind.details);
  }

  Future<void> _toggleTimer(Appointment appointment) async {
    final error = await controller.toggleAppointmentServiceTimer(appointment);
    if (!mounted) return;
    _showMessage(error ?? 'Tempo do atendimento atualizado e sincronizado.');
    setState(() {});
  }

  Future<void> _finish(Appointment appointment) async {
    final error = await controller.finishAppointmentService(appointment);
    if (!mounted) return;
    _showMessage(
      error ?? 'Atendimento finalizado. O recebimento já pode ser feito.',
    );
    setState(() => _panel = PdvPanelKind.receive);
  }

  Future<void> _receive(Appointment appointment) async {
    await showAppointmentPaymentDialog(
      context,
      controller,
      appointment,
      includeProductLines: true,
      onEdit: () => _edit(appointment),
    );
    if (mounted) setState(() => _panel = PdvPanelKind.details);
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  void _openPanel(PdvPanelKind kind, {required bool compact}) {
    final appointment = _selectedAppointment;
    if (appointment == null) {
      _showMessage('Selecione um atendimento no calendário do PDV.');
      return;
    }
    if (!compact) {
      setState(() => _panel = kind);
      return;
    }
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Colors.transparent,
      builder: (sheetContext) => FractionallySizedBox(
        heightFactor: .88,
        child: Material(
          color: AgendaThemeTokens.of(sheetContext).panel,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
          clipBehavior: Clip.antiAlias,
          child: _PdvPanel(
            controller: controller,
            appointment: appointment,
            kind: kind,
            now: _now,
            onClose: () => Navigator.of(sheetContext).pop(),
            onEdit: () {
              Navigator.of(sheetContext).pop();
              _edit(appointment);
            },
            onToggleTimer: () => _toggleTimer(appointment),
            onFinish: () => _finish(appointment),
            onReceive: () {
              Navigator.of(sheetContext).pop();
              _receive(appointment);
            },
            onSaved: (message) {
              _showMessage(message);
              setState(() {});
            },
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    _selectDefaultAppointment();
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 900;
        return compact ? _mobile() : _desktop();
      },
    );
  }

  Widget _desktop() {
    final t = AgendaThemeTokens.of(context);
    final appointment = _selectedAppointment;
    return Scaffold(
      key: const Key('pdv-desktop'),
      backgroundColor: t.appBackground,
      body: Column(
        children: [
          _PdvTopBar(
            date: _now,
            searchController: _searchController,
            onSearch: (_) => setState(_selectDefaultAppointment),
            onQuickAdd: _quickAdd,
            onExit: widget.onExit,
          ),
          Expanded(
            child: Row(
              children: [
                _PdvNavigationRail(onNavigate: widget.onNavigate),
                Expanded(
                  child: Column(
                    children: [
                      _PdvActiveRibbon(
                        controller: controller,
                        appointment: appointment,
                        now: _now,
                        onToggleTimer: appointment == null
                            ? null
                            : () => _toggleTimer(appointment),
                        onFinish: appointment == null
                            ? null
                            : () => _finish(appointment),
                      ),
                      _dateToolbar(compact: false),
                      Expanded(
                        child: Stack(
                          children: [
                            Positioned.fill(
                              left: 14,
                              top: 12,
                              right: 96,
                              bottom: 14,
                              child: _PdvScheduleBoard(
                                controller: controller,
                                appointments: _visibleAppointments,
                                selectedDate: controller.selectedDate,
                                selectedId: _selectedId,
                                weekView: _weekView,
                                now: _now,
                                onSelect: _select,
                                onCreate: _quickAdd,
                              ),
                            ),
                            Positioned(
                              top: 10,
                              right: 14,
                              bottom: 10,
                              width: 74,
                              child: _PdvActionRail(
                                selected: _panel,
                                onSelected: (kind) =>
                                    _openPanel(kind, compact: false),
                              ),
                            ),
                            if (appointment != null && _panel != null)
                              Positioned(
                                top: 16,
                                right: 110,
                                width: 360,
                                height: math.max(
                                  320,
                                  math.min(
                                    496,
                                    MediaQuery.sizeOf(context).height - 230,
                                  ),
                                ),
                                child: _PdvPanel(
                                  controller: controller,
                                  appointment: appointment,
                                  kind: _panel!,
                                  now: _now,
                                  onClose: () => setState(() => _panel = null),
                                  onEdit: () => _edit(appointment),
                                  onToggleTimer: () =>
                                      _toggleTimer(appointment),
                                  onFinish: () => _finish(appointment),
                                  onReceive: () => _receive(appointment),
                                  onSaved: (message) {
                                    _showMessage(message);
                                    setState(() {});
                                  },
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
          ),
        ],
      ),
    );
  }

  Widget _mobile() {
    final t = AgendaThemeTokens.of(context);
    final appointment = _selectedAppointment;
    return Scaffold(
      key: const Key('pdv-mobile'),
      backgroundColor: t.appBackground,
      appBar: AppBar(
        toolbarHeight: 66,
        backgroundColor: t.panel,
        foregroundColor: t.ink,
        elevation: 0,
        scrolledUnderElevation: 0,
        shape: Border(bottom: BorderSide(color: t.line)),
        leadingWidth: 58,
        leading: Padding(
          padding: const EdgeInsets.only(left: 12),
          child: Image.asset(
            'assets/branding/agenda-livre-mark.png',
            fit: BoxFit.contain,
            semanticLabel: 'Agenda Livre',
          ),
        ),
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Agenda Livre · PDV',
              style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
            ),
            Text(
              'Operação em tempo real',
              style: TextStyle(fontSize: 10.5, fontWeight: FontWeight.w400),
            ),
          ],
        ),
        actions: [
          IconButton(
            key: const Key('pdv-mobile-quick-add'),
            tooltip: 'Encaixe rápido',
            onPressed: _quickAdd,
            icon: const Icon(Icons.add_box_outlined),
          ),
          IconButton(
            key: const Key('pdv-mobile-exit'),
            tooltip: 'Encerrar PDV',
            onPressed: widget.onExit,
            icon: const Icon(Icons.close_rounded),
          ),
          const SizedBox(width: 4),
        ],
      ),
      body: Column(
        children: [
          _PdvActiveRibbon(
            controller: controller,
            appointment: appointment,
            now: _now,
            compact: true,
            onToggleTimer: appointment == null
                ? null
                : () => _toggleTimer(appointment),
            onFinish: appointment == null ? null : () => _finish(appointment),
          ),
          _dateToolbar(compact: true),
          Expanded(
            child: _PdvMobileAgenda(
              appointments: _mobileAppointments,
              selectedId: _selectedId,
              selectedDate: controller.selectedDate,
              weekView: _weekView,
              onSelect: _select,
              onCreate: _quickAdd,
            ),
          ),
        ],
      ),
      bottomNavigationBar: _PdvMobileActions(
        enabled: appointment != null,
        selected: _panel,
        onSelected: (kind) => _openPanel(kind, compact: true),
      ),
    );
  }

  Widget _dateToolbar({required bool compact}) {
    final t = AgendaThemeTokens.of(context);
    final start = _weekView
        ? _weekStart(controller.selectedDate)
        : controller.selectedDate;
    final end = start.add(const Duration(days: 6));
    final title = _weekView
        ? '${shortDate(start)} a ${shortDate(end)}'
        : _pdvDate(controller.selectedDate);
    final count = _visibleAppointments.length;
    final running = _visibleAppointments
        .where((item) => item.status == AppointmentStatus.inService)
        .length;
    final open = math.max(0, count - running);
    return Container(
      key: const Key('pdv-date-toolbar'),
      constraints: BoxConstraints(minHeight: compact ? 100 : 64),
      padding: EdgeInsets.fromLTRB(
        compact ? 12 : 18,
        10,
        compact ? 12 : 18,
        10,
      ),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: compact
          ? Column(
              children: [
                Row(
                  children: [
                    _dateButton(
                      Icons.chevron_left_rounded,
                      () => _changeDate(-1),
                      'Anterior',
                      wide: false,
                    ),
                    const SizedBox(width: 6),
                    _todayButton(wide: false),
                    const SizedBox(width: 6),
                    _dateButton(
                      Icons.chevron_right_rounded,
                      () => _changeDate(1),
                      'Próximo',
                      wide: false,
                    ),
                    const Spacer(),
                    _viewToggle(),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        title,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 15,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    Text(
                      '$count · $running em andamento',
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                  ],
                ),
              ],
            )
          : Row(
              children: [
                _dateButton(
                  Icons.chevron_left_rounded,
                  () => _changeDate(-1),
                  'Anterior',
                  wide: true,
                ),
                const SizedBox(width: 8),
                _todayButton(wide: true),
                const SizedBox(width: 8),
                _dateButton(
                  Icons.chevron_right_rounded,
                  () => _changeDate(1),
                  'Próximo',
                  wide: true,
                ),
                Expanded(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        title,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 17,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '$count atendimento${count == 1 ? '' : 's'} · $running em andamento · $open em aberto',
                        style: TextStyle(color: t.muted, fontSize: 10.5),
                      ),
                    ],
                  ),
                ),
                _viewToggle(),
              ],
            ),
    );
  }

  Widget _dateButton(
    IconData icon,
    VoidCallback onPressed,
    String tooltip, {
    required bool wide,
  }) => SizedBox(
    width: wide ? 98 : 38,
    height: 38,
    child: OutlinedButton(
      onPressed: onPressed,
      style: OutlinedButton.styleFrom(padding: EdgeInsets.zero),
      child: Tooltip(message: tooltip, child: Icon(icon, size: 20)),
    ),
  );

  Widget _todayButton({required bool wide}) => SizedBox(
    width: wide ? 68 : null,
    height: 38,
    child: OutlinedButton(onPressed: _goToday, child: const Text('Hoje')),
  );

  Widget _viewToggle() {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: t.graySoft,
        borderRadius: BorderRadius.circular(13),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _toggleChoice(
            'Dia',
            !_weekView,
            () => setState(() {
              _weekView = false;
              _selectedId = null;
              _selectDefaultAppointment();
            }),
          ),
          _toggleChoice(
            'Semana',
            _weekView,
            () => setState(() {
              _weekView = true;
              _selectedId = null;
              _panel = null;
              _selectDefaultAppointment();
            }),
          ),
        ],
      ),
    );
  }

  Widget _toggleChoice(String label, bool selected, VoidCallback onTap) {
    final t = AgendaThemeTokens.of(context);
    return Material(
      key: Key('pdv-view-${label.toLowerCase()}'),
      color: selected ? t.accent : Colors.transparent,
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(10),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
          child: Text(
            label,
            style: TextStyle(
              color: selected ? Colors.white : t.ink,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ),
    );
  }
}

class _PdvTopBar extends StatelessWidget {
  const _PdvTopBar({
    required this.date,
    required this.searchController,
    required this.onSearch,
    required this.onQuickAdd,
    required this.onExit,
  });

  final DateTime date;
  final TextEditingController searchController;
  final ValueChanged<String> onSearch;
  final VoidCallback onQuickAdd;
  final VoidCallback onExit;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('pdv-topbar'),
      height: 70,
      decoration: BoxDecoration(
        color: t.panel,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 260,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 22),
              child: Row(
                children: [
                  Image.asset(
                    'assets/branding/agenda-livre-mark.png',
                    width: 50,
                    height: 34,
                    fit: BoxFit.contain,
                    semanticLabel: 'Agenda Livre',
                  ),
                  const SizedBox(width: 10),
                  const Expanded(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Agenda Livre · PDV',
                          maxLines: 1,
                          style: TextStyle(
                            fontSize: 15.5,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        Text(
                          'Operação em tempo real',
                          maxLines: 1,
                          style: TextStyle(fontSize: 10.5),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          VerticalDivider(width: 1, color: t.line),
          SizedBox(
            width: 160,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 28),
              child: Row(
                children: [
                  Icon(Icons.calendar_month_outlined, size: 16, color: t.ink),
                  const SizedBox(width: 7),
                  Flexible(
                    child: Text(
                      '${DateUtils.isSameDay(date, DateTime.now()) ? 'Hoje, ' : ''}'
                      '${date.day.toString().padLeft(2, '0')}/'
                      '${date.month.toString().padLeft(2, '0')}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 12.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  const SizedBox(width: 3),
                  Icon(
                    Icons.keyboard_arrow_down_rounded,
                    size: 16,
                    color: t.ink,
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(0, 0, 22, 0),
              child: Row(
                children: [
                  Expanded(
                    child: SizedBox(
                      height: 42,
                      child: TextField(
                        controller: searchController,
                        onChanged: onSearch,
                        decoration: const InputDecoration(
                          hintText: 'Pesquisar em todo o Agenda Livre...',
                          prefixIcon: Icon(Icons.search_rounded, size: 20),
                          contentPadding: EdgeInsets.symmetric(vertical: 11),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  SizedBox(
                    height: 40,
                    child: FilledButton.icon(
                      onPressed: onQuickAdd,
                      icon: const Icon(Icons.add_rounded, size: 18),
                      label: const Text('Encaixe rápido'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  SizedBox(
                    height: 40,
                    child: OutlinedButton(
                      onPressed: onExit,
                      child: const Text('Encerrar PDV'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PdvNavigationRail extends StatelessWidget {
  const _PdvNavigationRail({required this.onNavigate});

  final ValueChanged<AgendaPage> onNavigate;

  @override
  Widget build(BuildContext context) {
    const items = <(IconData, String, AgendaPage)>[
      (Icons.grid_view_rounded, 'Início', AgendaPage.home),
      (Icons.calendar_month_rounded, 'Calendário', AgendaPage.agenda),
      (Icons.attach_money_rounded, 'Financeiro', AgendaPage.finance),
      (Icons.query_stats_rounded, 'Relatórios', AgendaPage.reports),
      (Icons.storefront_rounded, 'Estabelecimento', AgendaPage.establishment),
      (Icons.campaign_rounded, 'Marketing', AgendaPage.marketing),
      (Icons.settings_rounded, 'Configurações', AgendaPage.settings),
    ];
    return Container(
      key: const Key('pdv-navigation-rail'),
      width: 64,
      color: const Color(0xFF171513),
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 10),
      child: Column(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: AgendaThemeTokens.of(context).accent,
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(
              Icons.calendar_month_rounded,
              color: Color(0xFF171513),
            ),
          ),
          const SizedBox(height: 16),
          for (var index = 0; index < items.length; index++)
            _PdvHoverIconButton(
              icon: items[index].$1,
              tooltip: items[index].$2,
              selected: index == 0,
              onTap: () => onNavigate(items[index].$3),
            ),
          const Spacer(),
          const _PdvHoverIconButton(
            icon: Icons.info_outline_rounded,
            tooltip: 'Ajuda',
          ),
          const SizedBox(height: 8),
          CircleAvatar(
            radius: 17,
            backgroundColor: Color(0xFF3A3531),
            child: Text(
              'AL',
              style: TextStyle(
                color: Colors.white,
                fontSize: 10,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PdvHoverIconButton extends StatefulWidget {
  const _PdvHoverIconButton({
    required this.icon,
    required this.tooltip,
    this.onTap,
    this.selected = false,
  });

  final IconData icon;
  final String tooltip;
  final VoidCallback? onTap;
  final bool selected;

  @override
  State<_PdvHoverIconButton> createState() => _PdvHoverIconButtonState();
}

class _PdvHoverIconButtonState extends State<_PdvHoverIconButton> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 6),
    child: Tooltip(
      message: widget.tooltip,
      child: MouseRegion(
        onEnter: (_) => setState(() => _hovered = true),
        onExit: (_) => setState(() => _hovered = false),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 140),
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: widget.selected
                ? AgendaThemeTokens.of(context).accent
                : _hovered
                ? const Color(0xFF342E29)
                : Colors.transparent,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: widget.onTap,
              borderRadius: BorderRadius.circular(12),
              child: Icon(
                widget.icon,
                color: widget.selected
                    ? const Color(0xFF171513)
                    : const Color(0xFFF6F2EE),
                size: 21,
              ),
            ),
          ),
        ),
      ),
    ),
  );
}

class _PdvActiveRibbon extends StatelessWidget {
  const _PdvActiveRibbon({
    required this.controller,
    required this.appointment,
    required this.now,
    required this.onToggleTimer,
    required this.onFinish,
    this.compact = false,
  });

  final AgendaController controller;
  final Appointment? appointment;
  final DateTime now;
  final VoidCallback? onToggleTimer;
  final VoidCallback? onFinish;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final item = appointment;
    final elapsed = item == null
        ? Duration.zero
        : controller.appointmentServiceElapsed(item, now: now);
    final running =
        item?.serviceStartedAt != null && !(item?.serviceTimerPaused ?? true);
    final time = _elapsedLabel(elapsed);
    return Container(
      key: const Key('pdv-active-ribbon'),
      height: compact ? 104 : 60,
      color: const Color(0xFF171513),
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 14 : 18,
        vertical: compact ? 10 : 8,
      ),
      child: item == null
          ? Row(
              children: [
                Icon(Icons.touch_app_rounded, color: t.accent, size: 21),
                const SizedBox(width: 10),
                const Expanded(
                  child: Text(
                    'Selecione um atendimento para operar o PDV.',
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            )
          : compact
          ? Column(
              children: [
                Row(
                  children: [
                    Expanded(child: _ribbonIdentity(item)),
                    Text(
                      time,
                      style: const TextStyle(
                        color: Colors.white,
                        letterSpacing: .4,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 7),
                Row(
                  children: [
                    Expanded(
                      child: _ribbonButton(
                        context,
                        running
                            ? 'Pausar'
                            : elapsed.inSeconds > 0
                            ? 'Retomar'
                            : 'Iniciar',
                        running
                            ? Icons.pause_rounded
                            : Icons.play_arrow_rounded,
                        onToggleTimer,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: _ribbonButton(
                        context,
                        'Finalizar',
                        Icons.check_rounded,
                        onFinish,
                        primary: true,
                      ),
                    ),
                  ],
                ),
              ],
            )
          : Row(
              children: [
                Expanded(child: _ribbonIdentity(item)),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 9,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFF2A2724),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.timer_outlined,
                        color: Colors.white,
                        size: 18,
                      ),
                      const SizedBox(width: 8),
                      Text(
                        time,
                        style: const TextStyle(
                          color: Colors.white,
                          letterSpacing: .4,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 10),
                AppointmentStatusBadge(status: item.status, compact: true),
                const SizedBox(width: 10),
                SizedBox(
                  width: 100,
                  child: _ribbonButton(
                    context,
                    running
                        ? 'Pausar'
                        : elapsed.inSeconds > 0
                        ? 'Retomar'
                        : 'Iniciar',
                    running ? Icons.pause_rounded : Icons.play_arrow_rounded,
                    onToggleTimer,
                  ),
                ),
                const SizedBox(width: 8),
                SizedBox(
                  width: 104,
                  child: _ribbonButton(
                    context,
                    'Finalizar',
                    Icons.check_rounded,
                    onFinish,
                    primary: true,
                  ),
                ),
              ],
            ),
    );
  }

  Widget _ribbonIdentity(Appointment item) => Column(
    mainAxisAlignment: MainAxisAlignment.center,
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        item.customerName,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: const TextStyle(
          color: Colors.white,
          fontSize: 14,
          fontWeight: FontWeight.w800,
        ),
      ),
      const SizedBox(height: 2),
      Text(
        '${item.serviceName} · ${item.professionalName} · ${hour(item.start)}–${hour(item.end)}',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: const TextStyle(color: Color(0xFFC9C3BD), fontSize: 10.5),
      ),
    ],
  );

  Widget _ribbonButton(
    BuildContext context,
    String label,
    IconData icon,
    VoidCallback? onPressed, {
    bool primary = false,
  }) => SizedBox(
    height: 36,
    child: primary
        ? FilledButton.icon(
            onPressed: onPressed,
            icon: Icon(icon, size: 17),
            label: Text(
              label,
              maxLines: 1,
              style: const TextStyle(fontSize: 12),
            ),
            style: FilledButton.styleFrom(
              padding: const EdgeInsets.symmetric(horizontal: 11),
            ),
          )
        : OutlinedButton.icon(
            onPressed: onPressed,
            icon: Icon(icon, size: 17),
            label: Text(
              label,
              maxLines: 1,
              style: const TextStyle(fontSize: 12),
            ),
            style: OutlinedButton.styleFrom(
              backgroundColor: Colors.white,
              foregroundColor: const Color(0xFF171513),
              padding: const EdgeInsets.symmetric(horizontal: 11),
            ),
          ),
  );
}

class _PdvScheduleBoard extends StatelessWidget {
  const _PdvScheduleBoard({
    required this.controller,
    required this.appointments,
    required this.selectedDate,
    required this.selectedId,
    required this.weekView,
    required this.now,
    required this.onSelect,
    required this.onCreate,
  });

  final AgendaController controller;
  final List<Appointment> appointments;
  final DateTime selectedDate;
  final String? selectedId;
  final bool weekView;
  final DateTime now;
  final ValueChanged<Appointment> onSelect;
  final ValueChanged<DateTime> onCreate;

  @override
  Widget build(BuildContext context) {
    final professionals = controller.activeProfessionals;
    final weekStart = _weekStart(selectedDate);
    final columns = weekView
        ? 7
        : professionals.isEmpty
        ? 1
        : math.max(4, professionals.length);
    final startHour = controller.data.settings.workdayStartHour.clamp(0, 23);
    final endHour = controller.data.settings.workdayEndHour.clamp(
      startHour + 1,
      24,
    );
    final boardHeight = (endHour - startHour) * 72.0;
    final focused = appointments
        .where((item) => item.id == selectedId)
        .firstOrNull;
    final focusTime = focused?.start ?? now;
    final initialScrollOffset = math.max(
      0.0,
      ((focusTime.hour * 60 + focusTime.minute) - startHour * 60 - 90) * 1.2,
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        final columnWidth = weekView ? 170.0 : 222.0;
        final boardWidth = math.max(
          constraints.maxWidth,
          72 + columns * columnWidth,
        );
        return ColoredBox(
          color: Colors.white,
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: SizedBox(
              width: boardWidth,
              height: constraints.maxHeight,
              child: Column(
                children: [
                  SizedBox(
                    height: 58,
                    child: Row(
                      children: [
                        _boardHeaderCell(context, width: 72, title: 'Horário'),
                        for (var index = 0; index < columns; index++)
                          _boardHeaderCell(
                            context,
                            width: (boardWidth - 72) / columns,
                            title: weekView
                                ? _weekdayShort(
                                    weekStart.add(Duration(days: index)),
                                  ).toUpperCase()
                                : professionals.isEmpty
                                ? 'Sem profissional'
                                : index >= professionals.length
                                ? ''
                                : professionals[index].name,
                            subtitle: weekView
                                ? '${shortDate(weekStart.add(Duration(days: index)))} · ${appointments.where((item) => DateUtils.isSameDay(item.start, weekStart.add(Duration(days: index)))).length}'
                                : professionals.isEmpty
                                ? 'Cadastre para abrir horários'
                                : index >= professionals.length
                                ? ''
                                : professionals[index].role,
                          ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: _PdvAutoScroll(
                      initialOffset: initialScrollOffset,
                      child: SizedBox(
                        width: boardWidth,
                        height: boardHeight,
                        child: Stack(
                          children: [
                            _gridCells(
                              context,
                              boardWidth: boardWidth,
                              columns: columns,
                              startHour: startHour,
                              endHour: endHour,
                              professionals: professionals,
                              weekStart: weekStart,
                            ),
                            for (final appointment in appointments)
                              if (_columnIndex(
                                    appointment,
                                    professionals,
                                    weekStart,
                                  )
                                  case final index?)
                                _positionedAppointment(
                                  context,
                                  appointment: appointment,
                                  column: index,
                                  columns: columns,
                                  boardWidth: boardWidth,
                                  boardHeight: boardHeight,
                                  startHour: startHour,
                                ),
                            if (_currentTimePosition(
                                  startHour,
                                  endHour,
                                  weekStart,
                                )
                                case final marker?)
                              Positioned(
                                left: 72,
                                right: 0,
                                top: marker,
                                child: IgnorePointer(
                                  child: Row(
                                    children: [
                                      Container(
                                        width: 7,
                                        height: 7,
                                        decoration: const BoxDecoration(
                                          color: Color(0xFFEF4444),
                                          shape: BoxShape.circle,
                                        ),
                                      ),
                                      const Expanded(
                                        child: Divider(
                                          height: 1,
                                          thickness: 1.2,
                                          color: Color(0xFFEF4444),
                                        ),
                                      ),
                                    ],
                                  ),
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
          ),
        );
      },
    );
  }

  Widget _boardHeaderCell(
    BuildContext context, {
    required double width,
    required String title,
    String subtitle = '',
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      width: width,
      decoration: BoxDecoration(
        color: const Color(0xFFFBF7F4),
        border: Border(
          right: BorderSide(color: t.line),
          bottom: BorderSide(color: t.line),
        ),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      alignment: Alignment.center,
      child: subtitle.isEmpty
          ? Text(title, style: TextStyle(color: t.muted, fontSize: 10.5))
          : Row(
              children: [
                CircleAvatar(
                  radius: 15,
                  backgroundColor: t.accentSoft,
                  child: Text(
                    initials(title),
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 9,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
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
                          fontSize: 12,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        subtitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: t.muted, fontSize: 9.5),
                      ),
                    ],
                  ),
                ),
              ],
            ),
    );
  }

  Widget _gridCells(
    BuildContext context, {
    required double boardWidth,
    required int columns,
    required int startHour,
    required int endHour,
    required List<Professional> professionals,
    required DateTime weekStart,
  }) {
    final t = AgendaThemeTokens.of(context);
    final columnWidth = (boardWidth - 72) / columns;
    final rows = (endHour - startHour) * 2;
    return Column(
      children: [
        for (var row = 0; row < rows; row++)
          SizedBox(
            height: 36,
            child: Row(
              children: [
                Container(
                  width: 72,
                  alignment: Alignment.topCenter,
                  padding: const EdgeInsets.only(top: 7),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFBF7F4),
                    border: Border(
                      right: BorderSide(color: t.line),
                      bottom: BorderSide(
                        color: t.line.withValues(alpha: row.isEven ? 1 : .55),
                      ),
                    ),
                  ),
                  child: row.isEven
                      ? Text(
                          '${(startHour + row ~/ 2).toString().padLeft(2, '0')}:00',
                          style: TextStyle(color: t.muted, fontSize: 10),
                        )
                      : null,
                ),
                for (var column = 0; column < columns; column++)
                  _PdvSlotCell(
                    key: ValueKey(
                      'pdv-slot-${weekView ? 'week' : 'day'}-$row-$column',
                    ),
                    width: columnWidth,
                    border: t.line.withValues(alpha: row.isEven ? 1 : .55),
                    enabled:
                        weekView ||
                        (professionals.isNotEmpty &&
                            column < professionals.length),
                    onTap: () {
                      final date = weekView
                          ? weekStart.add(Duration(days: column))
                          : DateUtils.dateOnly(selectedDate);
                      onCreate(
                        date.add(Duration(hours: startHour, minutes: row * 30)),
                      );
                    },
                  ),
              ],
            ),
          ),
      ],
    );
  }

  int? _columnIndex(
    Appointment appointment,
    List<Professional> professionals,
    DateTime weekStart,
  ) {
    if (weekView) {
      final index = DateUtils.dateOnly(
        appointment.start,
      ).difference(weekStart).inDays;
      return index >= 0 && index < 7 ? index : null;
    }
    if (professionals.isEmpty) return 0;
    final index = professionals.indexWhere(
      (item) =>
          item.id == appointment.professionalId ||
          item.name == appointment.professionalName,
    );
    return index < 0 ? 0 : index;
  }

  Widget _positionedAppointment(
    BuildContext context, {
    required Appointment appointment,
    required int column,
    required int columns,
    required double boardWidth,
    required double boardHeight,
    required int startHour,
  }) {
    final columnWidth = (boardWidth - 72) / columns;
    final dayStart = DateUtils.dateOnly(
      appointment.start,
    ).add(Duration(hours: startHour));
    final top = appointment.start.difference(dayStart).inMinutes * 1.2;
    final height = math
        .max(56.0, appointment.durationMinutes * 1.2 - 5)
        .clamp(56.0, math.max(56.0, boardHeight - top - 2))
        .toDouble();
    return Positioned(
      left: 72 + column * columnWidth + 6,
      width: columnWidth - 12,
      top: top.clamp(2, boardHeight - 56),
      height: height,
      child: _PdvAppointmentCard(
        key: Key('pdv-appointment-${appointment.id}'),
        appointment: appointment,
        selected: appointment.id == selectedId,
        onTap: () => onSelect(appointment),
      ),
    );
  }

  double? _currentTimePosition(int startHour, int endHour, DateTime weekStart) {
    final visibleDay = weekView
        ? !DateUtils.dateOnly(now).isBefore(weekStart) &&
              DateUtils.dateOnly(
                now,
              ).isBefore(weekStart.add(const Duration(days: 7)))
        : DateUtils.isSameDay(now, selectedDate);
    if (!visibleDay || now.hour < startHour || now.hour >= endHour) return null;
    return ((now.hour - startHour) * 60 + now.minute) * 1.2;
  }
}

class _PdvAutoScroll extends StatefulWidget {
  const _PdvAutoScroll({required this.initialOffset, required this.child});

  final double initialOffset;
  final Widget child;

  @override
  State<_PdvAutoScroll> createState() => _PdvAutoScrollState();
}

class _PdvAutoScrollState extends State<_PdvAutoScroll> {
  late final ScrollController _controller;

  @override
  void initState() {
    super.initState();
    _controller = ScrollController(initialScrollOffset: widget.initialOffset);
  }

  @override
  void didUpdateWidget(covariant _PdvAutoScroll oldWidget) {
    super.didUpdateWidget(oldWidget);
    if ((oldWidget.initialOffset - widget.initialOffset).abs() < 1) return;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted || !_controller.hasClients) return;
      _controller.jumpTo(
        widget.initialOffset.clamp(0, _controller.position.maxScrollExtent),
      );
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(controller: _controller, child: widget.child);
  }
}

class _PdvSlotCell extends StatefulWidget {
  const _PdvSlotCell({
    super.key,
    required this.width,
    required this.border,
    required this.enabled,
    required this.onTap,
  });

  final double width;
  final Color border;
  final bool enabled;
  final VoidCallback onTap;

  @override
  State<_PdvSlotCell> createState() => _PdvSlotCellState();
}

class _PdvSlotCellState extends State<_PdvSlotCell> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      width: widget.width,
      child: MouseRegion(
        onEnter: widget.enabled ? (_) => setState(() => _hovered = true) : null,
        onExit: widget.enabled ? (_) => setState(() => _hovered = false) : null,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 120),
          decoration: BoxDecoration(
            color: _hovered ? t.warmSoft : Colors.white,
            border: Border(
              right: BorderSide(color: t.line),
              bottom: BorderSide(color: widget.border),
            ),
          ),
          child: InkWell(onTap: widget.enabled ? widget.onTap : null),
        ),
      ),
    );
  }
}

class _PdvAppointmentCard extends StatefulWidget {
  const _PdvAppointmentCard({
    super.key,
    required this.appointment,
    required this.selected,
    required this.onTap,
  });

  final Appointment appointment;
  final bool selected;
  final VoidCallback onTap;

  @override
  State<_PdvAppointmentCard> createState() => _PdvAppointmentCardState();
}

class _PdvAppointmentCardState extends State<_PdvAppointmentCard> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final appointment = widget.appointment;
    final selected = widget.selected;
    final background = selected
        ? t.accent
        : appointment.status == AppointmentStatus.inService
        ? const Color(0xFFFFE6D8)
        : appointment.status == AppointmentStatus.done
        ? const Color(0xFFF0FDF4)
        : const Color(0xFFFFF1E9);
    final foreground = selected ? Colors.white : t.ink;
    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: AnimatedScale(
        duration: const Duration(milliseconds: 140),
        scale: _hovered ? 1.012 : 1,
        child: Material(
          color: background,
          elevation: _hovered || selected ? 5 : 0,
          shadowColor: const Color(0x331C1612),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
            side: BorderSide(
              color: selected ? t.accentDark : t.accent.withValues(alpha: .45),
              width: selected ? 1.5 : 1,
            ),
          ),
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            onTap: widget.onTap,
            child: Padding(
              padding: const EdgeInsets.all(6),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    appointment.customerName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: foreground,
                      fontSize: 10.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${hour(appointment.start)}–${hour(appointment.end)} · ${appointment.serviceName}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: selected
                          ? Colors.white.withValues(alpha: .9)
                          : t.muted,
                      fontSize: 8,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    appointmentStatusLabel(appointment.status),
                    maxLines: 1,
                    style: TextStyle(
                      color: selected ? Colors.white : t.accentDark,
                      fontSize: 7.5,
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

class _PdvActionRail extends StatelessWidget {
  const _PdvActionRail({required this.selected, required this.onSelected});

  final PdvPanelKind? selected;
  final ValueChanged<PdvPanelKind> onSelected;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(color: AgendaThemeTokens.of(context).line),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Column(
          children: [
            _PdvRailAction(
              kind: PdvPanelKind.details,
              icon: Icons.badge_outlined,
              label: 'Detalhes',
              shortcut: 'F2',
              selected: selected == PdvPanelKind.details,
              onTap: onSelected,
            ),
            _PdvRailAction(
              kind: PdvPanelKind.edit,
              icon: Icons.edit_outlined,
              label: 'Editar',
              shortcut: 'F3',
              selected: selected == PdvPanelKind.edit,
              onTap: onSelected,
            ),
            _PdvRailAction(
              kind: PdvPanelKind.timer,
              icon: Icons.timer_outlined,
              label: 'Tempo',
              shortcut: 'F4',
              selected: selected == PdvPanelKind.timer,
              onTap: onSelected,
            ),
            _PdvRailAction(
              kind: PdvPanelKind.products,
              icon: Icons.inventory_2_outlined,
              label: 'Produtos e\nserviços',
              shortcut: 'F5',
              selected: selected == PdvPanelKind.products,
              onTap: onSelected,
            ),
            _PdvRailAction(
              kind: PdvPanelKind.receive,
              icon: Icons.point_of_sale_outlined,
              label: 'Receber',
              shortcut: 'F6',
              selected: selected == PdvPanelKind.receive,
              onTap: onSelected,
            ),
            const Spacer(),
            Padding(
              padding: const EdgeInsets.all(8),
              child: Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: const Color(0xFF0FA958),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Center(
                  child: FaIcon(
                    FontAwesomeIcons.whatsapp,
                    color: Colors.white,
                    size: 24,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PdvRailAction extends StatefulWidget {
  const _PdvRailAction({
    required this.kind,
    required this.icon,
    required this.label,
    required this.shortcut,
    required this.selected,
    required this.onTap,
  });

  final PdvPanelKind kind;
  final IconData icon;
  final String label;
  final String shortcut;
  final bool selected;
  final ValueChanged<PdvPanelKind> onTap;

  @override
  State<_PdvRailAction> createState() => _PdvRailActionState();
}

class _PdvRailActionState extends State<_PdvRailAction> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      child: MouseRegion(
        key: Key('pdv-action-${widget.kind.name}'),
        onEnter: (_) => setState(() => _hovered = true),
        onExit: (_) => setState(() => _hovered = false),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 130),
          height: 65,
          decoration: BoxDecoration(
            color: widget.selected
                ? t.accentSoft
                : _hovered
                ? t.warmSoft
                : Colors.transparent,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(
              color: widget.selected ? t.accent : Colors.transparent,
            ),
          ),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: () => widget.onTap(widget.kind),
              borderRadius: BorderRadius.circular(10),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    widget.icon,
                    size: 20,
                    color: widget.selected ? t.accentDark : t.ink,
                  ),
                  const SizedBox(height: 3),
                  Flexible(
                    child: Text(
                      widget.label,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: widget.selected ? t.accentDark : t.ink,
                        fontSize: 8.5,
                        height: 1,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  Text(
                    widget.shortcut,
                    style: TextStyle(color: t.muted, fontSize: 7.5, height: 1),
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

class _PdvPanel extends StatelessWidget {
  const _PdvPanel({
    required this.controller,
    required this.appointment,
    required this.kind,
    required this.now,
    required this.onClose,
    required this.onEdit,
    required this.onToggleTimer,
    required this.onFinish,
    required this.onReceive,
    required this.onSaved,
  });

  final AgendaController controller;
  final Appointment appointment;
  final PdvPanelKind kind;
  final DateTime now;
  final VoidCallback onClose;
  final VoidCallback onEdit;
  final VoidCallback onToggleTimer;
  final VoidCallback onFinish;
  final VoidCallback onReceive;
  final ValueChanged<String> onSaved;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final (title, icon) = switch (kind) {
      PdvPanelKind.details => ('DETALHES', Icons.badge_outlined),
      PdvPanelKind.edit => ('EDITAR ATENDIMENTO', Icons.edit_outlined),
      PdvPanelKind.timer => ('TEMPO DO SERVIÇO', Icons.timer_outlined),
      PdvPanelKind.products => (
        'PRODUTOS E SERVIÇOS',
        Icons.inventory_2_outlined,
      ),
      PdvPanelKind.receive => ('RECEBER', Icons.point_of_sale_outlined),
    };
    return Material(
      key: Key('pdv-panel-${kind.name}'),
      elevation: 6,
      shadowColor: const Color(0x1F1C1612),
      color: t.panel,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(color: t.line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 12, 12),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    title,
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 10,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    color: t.accent,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(icon, color: Colors.white, size: 19),
                ),
                const SizedBox(width: 8),
                IconButton(
                  onPressed: onClose,
                  tooltip: 'Fechar',
                  icon: const Icon(Icons.close_rounded),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: _PdvCustomerHeader(appointment: appointment),
          ),
          const SizedBox(height: 8),
          Divider(height: 1, color: t.line, indent: 16, endIndent: 16),
          Expanded(
            child: switch (kind) {
              PdvPanelKind.details => _details(context),
              PdvPanelKind.edit => _editPanel(context),
              PdvPanelKind.timer => _timerPanel(context),
              PdvPanelKind.products => _PdvItemsEditor(
                controller: controller,
                appointment: appointment,
                onSaved: onSaved,
              ),
              PdvPanelKind.receive => _receivePanel(context),
            },
          ),
        ],
      ),
    );
  }

  Widget _details(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final services = appointment.serviceLines.isEmpty
        ? <AppointmentServiceLine>[
            AppointmentServiceLine(
              serviceId: appointment.serviceId,
              serviceName: appointment.serviceName,
              segment: appointment.segment,
              durationMinutes: appointment.durationMinutes,
              unitPrice: appointment.price,
            ),
          ]
        : appointment.serviceLines;
    final serviceSubtotal = services.fold<double>(
      0,
      (sum, service) => sum + service.total,
    );
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'RESUMO DO ATENDIMENTO',
          style: TextStyle(
            color: t.muted,
            fontSize: 8.5,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 8),
        _infoRow(
          context,
          Icons.calendar_today_outlined,
          'Data',
          _pdvFullDate(appointment.start),
        ),
        _infoRow(
          context,
          Icons.schedule_outlined,
          'Horário',
          '${hour(appointment.start)} às ${hour(appointment.end)}',
        ),
        _infoRow(
          context,
          Icons.timer_outlined,
          'Duração prevista',
          '${appointment.durationMinutes} min',
        ),
        _infoRow(
          context,
          Icons.person_outline_rounded,
          'Profissional',
          appointment.professionalName,
        ),
        _infoRow(
          context,
          Icons.info_outline_rounded,
          'Status',
          appointmentStatusLabel(appointment.status),
        ),
        const SizedBox(height: 14),
        Text(
          'SERVIÇOS CONTRATADOS · ${services.length}',
          style: TextStyle(
            color: t.muted,
            fontSize: 8.5,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 6),
        for (final service in services)
          SizedBox(
            height: 44,
            child: ListTile(
              dense: true,
              visualDensity: const VisualDensity(vertical: -4),
              minVerticalPadding: 0,
              contentPadding: const EdgeInsets.symmetric(horizontal: 4),
              leading: const Icon(Icons.content_cut_rounded, size: 18),
              title: Text(
                service.serviceName,
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
              subtitle: Text(
                '${service.quantity}x · ${service.totalDurationMinutes} min',
                style: TextStyle(color: t.muted, fontSize: 9.5),
              ),
              trailing: Text(
                money(service.total),
                style: TextStyle(
                  color: t.accentDark,
                  fontSize: 11,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ),
        const SizedBox(height: 8),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 9),
          decoration: BoxDecoration(
            color: t.graySoft,
            borderRadius: BorderRadius.circular(9),
          ),
          child: Row(
            children: [
              Icon(Icons.attach_money_rounded, size: 16, color: t.muted),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Subtotal dos serviços',
                  style: TextStyle(color: t.muted, fontSize: 10.5),
                ),
              ),
              Text(
                money(serviceSubtotal),
                style: TextStyle(
                  color: t.accentDark,
                  fontSize: 10.5,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _editPanel(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      const Text(
        'Edite cliente, profissional, horário, serviço e observações no formulário completo do Agenda Livre.',
      ),
      const SizedBox(height: 16),
      OutlinedButton.icon(
        onPressed: onEdit,
        icon: const Icon(Icons.open_in_new_rounded),
        label: const Text('Abrir edição completa'),
      ),
      const SizedBox(height: 10),
      _infoRow(
        context,
        Icons.person_outline_rounded,
        'Cliente',
        appointment.customerName,
      ),
      _infoRow(
        context,
        Icons.badge_outlined,
        'Profissional',
        appointment.professionalName,
      ),
      _infoRow(
        context,
        Icons.schedule_outlined,
        'Horário',
        '${shortDate(appointment.start)} · ${hour(appointment.start)}',
      ),
      _infoRow(
        context,
        Icons.notes_rounded,
        'Observações',
        appointment.notes.trim().isEmpty
            ? 'Sem observações'
            : appointment.notes,
      ),
    ],
  );

  Widget _timerPanel(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final elapsed = controller.appointmentServiceElapsed(appointment, now: now);
    final running =
        appointment.serviceStartedAt != null && !appointment.serviceTimerPaused;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Container(
          padding: const EdgeInsets.all(22),
          decoration: BoxDecoration(
            color: const Color(0xFF171513),
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            children: [
              Text(
                _elapsedLabel(elapsed),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 34,
                  letterSpacing: .8,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                running
                    ? 'Atendimento em andamento'
                    : elapsed.inSeconds > 0
                    ? 'Temporizador pausado'
                    : 'Temporizador pronto',
                style: const TextStyle(color: Color(0xFFC9C3BD), fontSize: 11),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: onToggleTimer,
          icon: Icon(running ? Icons.pause_rounded : Icons.play_arrow_rounded),
          label: Text(
            running
                ? 'Pausar'
                : elapsed.inSeconds > 0
                ? 'Retomar'
                : 'Iniciar atendimento',
          ),
        ),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: onFinish,
          icon: const Icon(Icons.check_rounded),
          label: const Text('Finalizar atendimento'),
        ),
        const SizedBox(height: 18),
        Text(
          'O tempo é salvo na nuvem junto com o atendimento e continua disponível no Windows, Web e Android.',
          style: TextStyle(color: t.muted, fontSize: 11, height: 1.45),
        ),
      ],
    );
  }

  Widget _receivePanel(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final products = appointment.productLines.fold<double>(
      0,
      (sum, line) => sum + line.total,
    );
    final total = controller.pdvAppointmentTotal(appointment);
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _summaryLine(context, 'Serviços', money(appointment.price)),
        _summaryLine(context, 'Produtos', money(products)),
        _summaryLine(context, 'Desconto', money(0)),
        const SizedBox(height: 4),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
          decoration: BoxDecoration(
            color: t.graySoft,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  'Total',
                  style: TextStyle(color: t.ink, fontWeight: FontWeight.w900),
                ),
              ),
              Text(
                money(total),
                style: TextStyle(
                  color: t.accentDark,
                  fontSize: 17,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: const [
            Chip(
              avatar: Icon(Icons.qr_code_rounded, size: 16),
              label: Text('Pix'),
            ),
            Chip(
              avatar: Icon(Icons.credit_card_rounded, size: 16),
              label: Text('Cartão'),
            ),
            Chip(
              avatar: Icon(Icons.payments_outlined, size: 16),
              label: Text('Dinheiro'),
            ),
          ],
        ),
        const SizedBox(height: 14),
        FilledButton.icon(
          onPressed: onReceive,
          icon: const Icon(Icons.lock_outline_rounded),
          label: Text('Receber ${money(total)}'),
        ),
        const SizedBox(height: 8),
        Text(
          'A confirmação abre o fluxo seguro de cobrança. Point e Pix só são registrados depois da aprovação do Mercado Pago.',
          style: TextStyle(color: t.muted, fontSize: 10.5, height: 1.4),
        ),
      ],
    );
  }

  Widget _infoRow(
    BuildContext context,
    IconData icon,
    String label,
    String value,
  ) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      constraints: const BoxConstraints(minHeight: 30),
      margin: const EdgeInsets.only(bottom: 4),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: t.graySoft,
        borderRadius: BorderRadius.circular(9),
      ),
      child: Row(
        children: [
          Icon(icon, size: 15, color: t.muted),
          const SizedBox(width: 9),
          Text(label, style: TextStyle(color: t.muted, fontSize: 10.5)),
          const Spacer(),
          Flexible(
            child: Text(
              value,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.end,
              style: TextStyle(
                color: t.ink,
                fontSize: 10.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _summaryLine(BuildContext context, String label, String value) =>
      Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        child: Row(
          children: [
            Expanded(
              child: Text(label, style: const TextStyle(fontSize: 10.5)),
            ),
            Text(
              value,
              style: TextStyle(
                color: AgendaThemeTokens.of(context).accentDark,
                fontSize: 10.5,
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ),
      );
}

class _PdvCustomerHeader extends StatelessWidget {
  const _PdvCustomerHeader({required this.appointment});

  final Appointment appointment;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      children: [
        CircleAvatar(
          radius: 19,
          backgroundColor: t.accentSoft,
          child: Text(
            initials(appointment.customerName),
            style: TextStyle(
              color: t.accentDark,
              fontSize: 10,
              fontWeight: FontWeight.w900,
            ),
          ),
        ),
        const SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                appointment.customerName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 14,
                  fontWeight: FontWeight.w900,
                ),
              ),
              Text(
                appointment.serviceName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: t.ink, fontSize: 10.5),
              ),
              Text(
                appointment.professionalName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: t.muted, fontSize: 9.5),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _PdvItemsEditor extends StatefulWidget {
  const _PdvItemsEditor({
    required this.controller,
    required this.appointment,
    required this.onSaved,
  });

  final AgendaController controller;
  final Appointment appointment;
  final ValueChanged<String> onSaved;

  @override
  State<_PdvItemsEditor> createState() => _PdvItemsEditorState();
}

class _PdvItemsEditorState extends State<_PdvItemsEditor> {
  late List<AppointmentServiceLine> _services;
  late List<AppointmentProductLine> _products;
  String? _serviceId;
  String? _productId;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _services = widget.appointment.serviceLines
        .map((line) => AppointmentServiceLine.fromJson(line.toJson()))
        .toList();
    if (_services.isEmpty && widget.appointment.serviceName.trim().isNotEmpty) {
      _services.add(
        AppointmentServiceLine(
          serviceId: widget.appointment.serviceId,
          serviceName: widget.appointment.serviceName,
          segment: widget.appointment.segment,
          durationMinutes: widget.appointment.durationMinutes,
          unitPrice: widget.appointment.price,
        ),
      );
    }
    _products = widget.appointment.productLines
        .map((line) => AppointmentProductLine.fromJson(line.toJson()))
        .toList();
    _serviceId = widget.controller.activeServices.firstOrNull?.id;
    _productId = widget.controller.data.products
        .where((item) => item.isActive)
        .firstOrNull
        ?.id;
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final activeProducts = widget.controller.data.products
        .where((item) => item.isActive)
        .toList();
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          'SERVIÇOS CONTRATADOS',
          style: TextStyle(
            color: t.muted,
            fontSize: 8.5,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 6),
        Row(
          children: [
            Expanded(
              child: DropdownButtonFormField<String>(
                initialValue: _serviceId,
                isExpanded: true,
                items: [
                  for (final service in widget.controller.activeServices)
                    DropdownMenuItem(
                      value: service.id,
                      child: Text(
                        service.displayName,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                ],
                onChanged: (value) => setState(() => _serviceId = value),
              ),
            ),
            const SizedBox(width: 8),
            IconButton.filledTonal(
              onPressed: _serviceId == null ? null : _addService,
              icon: const Icon(Icons.add_rounded),
            ),
          ],
        ),
        const SizedBox(height: 8),
        for (final line in _services)
          _lineRow(
            context,
            title: line.serviceName,
            subtitle: '${line.quantity}x · ${line.totalDurationMinutes} min',
            value: money(line.total),
            quantity: line.quantity,
            onMinus: () => _adjustService(line, -1),
            onPlus: () => _adjustService(line, 1),
          ),
        const SizedBox(height: 14),
        Text(
          'PRODUTOS UTILIZADOS',
          style: TextStyle(
            color: t.muted,
            fontSize: 8.5,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 6),
        Row(
          children: [
            Expanded(
              child: DropdownButtonFormField<String>(
                initialValue: _productId,
                isExpanded: true,
                items: [
                  for (final product in activeProducts)
                    DropdownMenuItem(
                      value: product.id,
                      child: Text(
                        '${product.name} · ${product.stockQuantity} em estoque',
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                ],
                onChanged: (value) => setState(() => _productId = value),
              ),
            ),
            const SizedBox(width: 8),
            IconButton.filledTonal(
              onPressed: _productId == null ? null : _addProduct,
              icon: const Icon(Icons.add_rounded),
            ),
          ],
        ),
        const SizedBox(height: 8),
        for (final line in _products)
          _lineRow(
            context,
            title: line.productName,
            subtitle: '${line.quantity}x · ${money(line.unitPrice)}',
            value: money(line.total),
            quantity: line.quantity,
            onMinus: () => _adjustProduct(line, -1),
            onPlus: () => _adjustProduct(line, 1),
          ),
        const SizedBox(height: 14),
        FilledButton.icon(
          onPressed: _busy ? null : _save,
          icon: _busy
              ? const SizedBox.square(
                  dimension: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.cloud_done_outlined),
          label: const Text('Salvar e sincronizar'),
        ),
      ],
    );
  }

  Widget _lineRow(
    BuildContext context, {
    required String title,
    required String subtitle,
    required String value,
    required int quantity,
    required VoidCallback onMinus,
    required VoidCallback onPlus,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 7),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                Text(subtitle, style: TextStyle(color: t.muted, fontSize: 9)),
              ],
            ),
          ),
          IconButton(
            onPressed: onMinus,
            visualDensity: VisualDensity.compact,
            icon: const Icon(Icons.remove_circle_outline_rounded, size: 20),
          ),
          Text(
            '$quantity',
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
          ),
          IconButton(
            onPressed: onPlus,
            visualDensity: VisualDensity.compact,
            icon: const Icon(Icons.add_circle_outline_rounded, size: 20),
          ),
          SizedBox(
            width: 68,
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: TextStyle(
                color: t.accentDark,
                fontSize: 10.5,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _addService() {
    final service = widget.controller.activeServices
        .where((item) => item.id == _serviceId)
        .firstOrNull;
    if (service == null) return;
    setState(() {
      final existing = _services
          .where((line) => line.serviceId == service.id)
          .firstOrNull;
      if (existing == null) {
        _services.add(
          AppointmentServiceLine(
            serviceId: service.id,
            serviceName: service.name,
            segment: service.segment,
            durationMinutes: service.durationMinutes,
            unitPrice: service.price,
          ),
        );
      } else {
        existing.quantity++;
      }
    });
  }

  void _addProduct() {
    final product = widget.controller.data.products
        .where((item) => item.id == _productId && item.isActive)
        .firstOrNull;
    if (product == null) return;
    setState(() {
      final existing = _products
          .where((line) => line.productId == product.id)
          .firstOrNull;
      if (existing == null) {
        _products.add(
          AppointmentProductLine(
            productId: product.id,
            productName: product.name,
            unitPrice: product.price,
          ),
        );
      } else if (existing.quantity < product.stockQuantity) {
        existing.quantity++;
      }
    });
  }

  void _adjustService(AppointmentServiceLine line, int delta) => setState(() {
    line.quantity += delta;
    if (line.quantity <= 0) _services.remove(line);
  });

  void _adjustProduct(AppointmentProductLine line, int delta) => setState(() {
    final stock =
        widget.controller.data.products
            .where((item) => item.id == line.productId)
            .firstOrNull
            ?.stockQuantity ??
        0;
    line.quantity = (line.quantity + delta).clamp(0, stock);
    if (line.quantity <= 0) _products.remove(line);
  });

  Future<void> _save() async {
    setState(() => _busy = true);
    final error = await widget.controller.savePdvAppointmentLines(
      widget.appointment,
      serviceLines: _services,
      productLines: _products,
    );
    if (!mounted) return;
    setState(() => _busy = false);
    widget.onSaved(
      error ?? 'Produtos e serviços salvos no atendimento e sincronizados.',
    );
  }
}

class _PdvMobileAgenda extends StatelessWidget {
  const _PdvMobileAgenda({
    required this.appointments,
    required this.selectedId,
    required this.selectedDate,
    required this.weekView,
    required this.onSelect,
    required this.onCreate,
  });

  final List<Appointment> appointments;
  final String? selectedId;
  final DateTime selectedDate;
  final bool weekView;
  final ValueChanged<Appointment> onSelect;
  final ValueChanged<DateTime> onCreate;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    if (appointments.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.event_available_rounded, size: 42, color: t.accent),
              const SizedBox(height: 12),
              Text(
                'Nenhum atendimento neste período.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: () => onCreate(
                  DateUtils.dateOnly(
                    selectedDate,
                  ).add(const Duration(hours: 9)),
                ),
                icon: const Icon(Icons.add_rounded),
                label: const Text('Criar encaixe'),
              ),
            ],
          ),
        ),
      );
    }
    return ListView.separated(
      key: const Key('pdv-mobile-agenda-list'),
      padding: const EdgeInsets.fromLTRB(12, 12, 12, 18),
      itemCount: appointments.length,
      separatorBuilder: (_, _) => const SizedBox(height: 9),
      itemBuilder: (context, index) {
        final appointment = appointments[index];
        final selected = appointment.id == selectedId;
        return Material(
          key: Key('pdv-mobile-appointment-${appointment.id}'),
          color: selected ? t.accentSoft : t.panel,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(
              color: selected ? t.accent : t.line,
              width: selected ? 1.5 : 1,
            ),
          ),
          child: InkWell(
            onTap: () => onSelect(appointment),
            borderRadius: BorderRadius.circular(16),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Row(
                children: [
                  SizedBox(
                    width: 58,
                    child: Column(
                      children: [
                        Text(
                          hour(appointment.start),
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 14,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        if (weekView)
                          Text(
                            _weekdayShort(appointment.start),
                            style: TextStyle(color: t.muted, fontSize: 9.5),
                          ),
                      ],
                    ),
                  ),
                  Container(
                    width: 3,
                    height: 54,
                    decoration: BoxDecoration(
                      color: appointment.status == AppointmentStatus.inService
                          ? const Color(0xFF10B981)
                          : t.accent,
                      borderRadius: BorderRadius.circular(999),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          appointment.customerName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 13,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          appointment.serviceName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: t.muted, fontSize: 10.5),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          appointment.professionalName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: t.muted, fontSize: 9.5),
                        ),
                      ],
                    ),
                  ),
                  AppointmentStatusBadge(
                    status: appointment.status,
                    compact: true,
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}

class _PdvMobileActions extends StatelessWidget {
  const _PdvMobileActions({
    required this.enabled,
    required this.selected,
    required this.onSelected,
  });

  final bool enabled;
  final PdvPanelKind? selected;
  final ValueChanged<PdvPanelKind> onSelected;

  @override
  Widget build(BuildContext context) {
    const items = <(PdvPanelKind, IconData, String)>[
      (PdvPanelKind.details, Icons.badge_outlined, 'Detalhes'),
      (PdvPanelKind.edit, Icons.edit_outlined, 'Editar'),
      (PdvPanelKind.timer, Icons.timer_outlined, 'Tempo'),
      (PdvPanelKind.products, Icons.inventory_2_outlined, 'Produtos'),
      (PdvPanelKind.receive, Icons.point_of_sale_outlined, 'Receber'),
    ];
    final t = AgendaThemeTokens.of(context);
    return Material(
      key: const Key('pdv-mobile-actions'),
      color: t.panel,
      elevation: 10,
      child: SafeArea(
        top: false,
        child: SizedBox(
          height: 68,
          child: Row(
            children: [
              for (final item in items)
                Expanded(
                  child: InkWell(
                    key: Key('pdv-mobile-action-${item.$1.name}'),
                    onTap: enabled ? () => onSelected(item.$1) : null,
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          item.$2,
                          size: 20,
                          color: selected == item.$1
                              ? t.accent
                              : enabled
                              ? t.ink
                              : t.muted.withValues(alpha: .45),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          item.$3,
                          style: TextStyle(
                            color: selected == item.$1
                                ? t.accentDark
                                : enabled
                                ? t.ink
                                : t.muted.withValues(alpha: .45),
                            fontSize: 8.5,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

DateTime _weekStart(DateTime date) =>
    DateUtils.dateOnly(date).subtract(Duration(days: date.weekday - 1));

String _pdvDate(DateTime date) {
  const weekdays = <String>[
    'segunda-feira',
    'terça-feira',
    'quarta-feira',
    'quinta-feira',
    'sexta-feira',
    'sábado',
    'domingo',
  ];
  const months = <String>[
    'janeiro',
    'fevereiro',
    'março',
    'abril',
    'maio',
    'junho',
    'julho',
    'agosto',
    'setembro',
    'outubro',
    'novembro',
    'dezembro',
  ];
  final value =
      '${weekdays[date.weekday - 1]}, ${date.day} de ${months[date.month - 1]}';
  return '${value[0].toUpperCase()}${value.substring(1)}';
}

String _pdvFullDate(DateTime date) =>
    '${date.day.toString().padLeft(2, '0')}/'
    '${date.month.toString().padLeft(2, '0')}/'
    '${date.year}';

String _weekdayShort(DateTime date) => const <String>[
  'seg',
  'ter',
  'qua',
  'qui',
  'sex',
  'sáb',
  'dom',
][date.weekday - 1];

String _elapsedLabel(Duration duration) {
  final hours = duration.inHours.toString().padLeft(2, '0');
  final minutes = duration.inMinutes.remainder(60).toString().padLeft(2, '0');
  final seconds = duration.inSeconds.remainder(60).toString().padLeft(2, '0');
  return '$hours:$minutes:$seconds';
}
