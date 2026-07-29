import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart' as app;
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import 'agenda_board.dart';
import 'appointment_dialog.dart';
import 'appointment_payment_dialog.dart';
import 'appointment_visuals.dart';

class AgendaPage extends StatefulWidget {
  const AgendaPage({super.key, required this.controller, this.onWhatsApp});

  final app.AgendaController controller;
  final ValueChanged<Appointment>? onWhatsApp;

  @override
  State<AgendaPage> createState() => _AgendaPageState();
}

class _AgendaPageState extends State<AgendaPage> {
  String? _selectedAppointmentId;
  final ScrollController _pageScrollController = ScrollController();

  app.AgendaController get controller => widget.controller;

  @override
  void dispose() {
    _pageScrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final items = controller.appointmentsForSelectedDate;
        final selected = _selected(items);
        return LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 720;
            final desktopWorkspace = constraints.maxWidth >= 860;
            return SingleChildScrollView(
              controller: _pageScrollController,
              padding: EdgeInsets.fromLTRB(
                compact ? 12 : 28,
                compact ? 12 : 20,
                compact ? 12 : 28,
                compact ? 30 : 18,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _pageHeading(context, compact: compact),
                  const SizedBox(height: 14),
                  _metrics(items),
                  if (selected != null) ...[
                    const SizedBox(height: 14),
                    _selectedCard(selected),
                  ],
                  const SizedBox(height: 14),
                  if (desktopWorkspace)
                    SizedBox(
                      height: 460,
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Expanded(
                            child: _mainAgenda(
                              items,
                              compact: false,
                              desktopHeight: 460,
                            ),
                          ),
                          const SizedBox(width: 14),
                          SizedBox(
                            width: 340,
                            child: _appointmentFlowCard(items, fixed: true),
                          ),
                        ],
                      ),
                    )
                  else ...[
                    _mainAgenda(items, compact: compact),
                    const SizedBox(height: 12),
                    _appointmentFlowCard(items),
                  ],
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _pageHeading(BuildContext context, {required bool compact}) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      height: compact ? 88 : 78,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'AGENDA',
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
          const SizedBox(height: 7),
          Text(
            'Agenda de hoje',
            style: TextStyle(
              color: t.ink,
              fontSize: compact ? 25 : 29,
              height: 1.08,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            fullDate(controller.selectedDate).toLowerCase(),
            style: TextStyle(color: t.muted, fontSize: 12.5),
          ),
        ],
      ),
    );
  }

  Widget _metrics(List<Appointment> items) {
    final confirmed = items
        .where(
          (item) => const {
            AppointmentStatus.confirmed,
            AppointmentStatus.waiting,
            AppointmentStatus.inService,
          }.contains(item.status),
        )
        .length;
    final open = items.where(isOpenAppointment).length;
    final finalized = items
        .where((item) => item.status == AppointmentStatus.done)
        .length;
    final percentage = items.isEmpty
        ? 0
        : (confirmed / items.length * 100).round();
    final forecast = items
        .where(
          (item) => !const {
            AppointmentStatus.cancelled,
            AppointmentStatus.noShow,
            AppointmentStatus.blocked,
          }.contains(item.status),
        )
        .fold<double>(0, (sum, item) => sum + item.price);

    final late = items
        .where(
          (item) =>
              item.start.isBefore(DateTime.now()) &&
              DateUtils.isSameDay(item.start, DateTime.now()) &&
              const {
                AppointmentStatus.scheduled,
                AppointmentStatus.confirmed,
              }.contains(item.status),
        )
        .length;
    return AgendaDarkMetricStrip(
      key: const Key('agenda-metrics'),
      metrics: [
        AgendaDarkMetricData(
          label: 'Atendimentos',
          value: '$open',
          caption: 'em aberto no dia',
          icon: Icons.groups_rounded,
        ),
        AgendaDarkMetricData(
          label: 'Confirmados',
          value: '$confirmed',
          caption: '$percentage% do total',
          icon: Icons.check_circle_outline_rounded,
          tone: const Color(0xFF16A34A),
        ),
        AgendaDarkMetricData(
          label: 'Horários livres',
          value: '${_availableSlots(items)}',
          caption: 'janelas de 30 min',
          icon: Icons.schedule_rounded,
        ),
        AgendaDarkMetricData(
          label: 'Caixa previsto',
          value: money(forecast, cents: false),
          caption: '$finalized finalizado(s) | $late atraso(s)',
          icon: Icons.account_balance_wallet_outlined,
        ),
      ],
    );
  }

  Widget _selectedCard(Appointment item) {
    final t = AgendaThemeTokens.of(context);
    final actionButtons = <Widget>[];
    if (item.status == AppointmentStatus.scheduled) {
      actionButtons.add(
        _statusButton(
          item,
          AppointmentStatus.confirmed,
          'Confirmar',
          Icons.verified_outlined,
        ),
      );
    }
    if (const {
      AppointmentStatus.scheduled,
      AppointmentStatus.confirmed,
    }.contains(item.status)) {
      actionButtons.add(
        _statusButton(
          item,
          AppointmentStatus.waiting,
          'Chegou',
          Icons.person_pin_circle_outlined,
        ),
      );
    }
    if (const {
      AppointmentStatus.confirmed,
      AppointmentStatus.waiting,
    }.contains(item.status)) {
      actionButtons.add(
        _statusButton(
          item,
          AppointmentStatus.inService,
          'Iniciar',
          Icons.play_arrow_rounded,
        ),
      );
    }
    if (const {
      AppointmentStatus.waiting,
      AppointmentStatus.inService,
    }.contains(item.status)) {
      actionButtons.add(
        _statusButton(
          item,
          AppointmentStatus.done,
          'Finalizar',
          Icons.check_rounded,
        ),
      );
    }

    return AgendaPanel(
      radius: 16,
      padding: const EdgeInsets.all(14),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 700;
          final info = Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AgendaIconBadge(
                appointmentStatusIcon(item.status),
                background: appointmentStatusStyle(
                  context,
                  item.status,
                ).background,
                color: appointmentStatusStyle(context, item.status).foreground,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Wrap(
                      spacing: 8,
                      runSpacing: 6,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text(
                          item.customerName,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        AppointmentStatusBadge(status: item.status),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${hour(item.start)}–${hour(item.end)}  •  ${item.serviceName}  •  ${item.professionalName}',
                      style: TextStyle(color: t.muted, fontSize: 12),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      money(item.price),
                      style: TextStyle(
                        color: t.accentDark,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                key: const Key('agenda-clear-selection'),
                tooltip: 'Fechar detalhes',
                onPressed: () => setState(() => _selectedAppointmentId = null),
                icon: const Icon(Icons.close_rounded, size: 20),
              ),
            ],
          );
          final actions = Wrap(
            spacing: 7,
            runSpacing: 7,
            children: [
              ...actionButtons,
              if (!const {
                    AppointmentStatus.cancelled,
                    AppointmentStatus.noShow,
                    AppointmentStatus.blocked,
                  }.contains(item.status) &&
                  !controller.appointmentHasRegisteredCharge(item))
                FilledButton.icon(
                  key: const Key('selected-appointment-charge'),
                  onPressed: () => _openPayment(item),
                  icon: const Icon(Icons.payments_outlined, size: 16),
                  label: const Text('Receber'),
                  style: FilledButton.styleFrom(minimumSize: const Size(0, 44)),
                ),
              if (item.customerPhone.trim().isNotEmpty)
                OutlinedButton.icon(
                  onPressed: () => _openWhatsApp(item),
                  icon: const Icon(Icons.chat_outlined, size: 16),
                  label: const Text('WhatsApp'),
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size(0, 44),
                  ),
                ),
              OutlinedButton.icon(
                onPressed: () => _editAppointment(item),
                icon: const Icon(Icons.edit_outlined, size: 16),
                label: const Text('Editar'),
                style: OutlinedButton.styleFrom(minimumSize: const Size(0, 44)),
              ),
            ],
          );
          if (compact) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [info, const SizedBox(height: 12), actions],
            );
          }
          return Row(
            children: [
              Expanded(child: info),
              const SizedBox(width: 12),
              actions,
            ],
          );
        },
      ),
    );
  }

  Widget _statusButton(
    Appointment item,
    AppointmentStatus target,
    String label,
    IconData icon,
  ) {
    return OutlinedButton.icon(
      onPressed: () => _setStatus(item, target),
      icon: Icon(icon, size: 16),
      label: Text(label),
      style: OutlinedButton.styleFrom(minimumSize: const Size(0, 44)),
    );
  }

  Widget _mainAgenda(
    List<Appointment> items, {
    required bool compact,
    double? desktopHeight,
  }) {
    final t = AgendaThemeTokens.of(context);
    final countLabel =
        '${items.length} atendimento${items.length == 1 ? '' : 's'}';
    final rangeLabel =
        '${controller.data.settings.workdayStartHour.toString().padLeft(2, '0')}:00-'
        '${controller.data.settings.workdayEndHour.toString().padLeft(2, '0')}:00';
    final boardViewportHeight = desktopHeight == null
        ? (compact ? 360.0 : 354.0)
        : math.max(250.0, desktopHeight - 148);
    return AgendaPanel(
      key: const Key('agenda-main-workspace'),
      radius: 16,
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 10),
            child: LayoutBuilder(
              builder: (context, constraints) => _agendaToolbar(
                availableWidth: constraints.maxWidth,
                countLabel: countLabel,
                rangeLabel: rangeLabel,
                ink: t.ink,
                muted: t.muted,
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 180),
              child: switch (controller.agendaMode) {
                app.AgendaViewMode.board => AgendaScheduleBoard(
                  key: const ValueKey('board'),
                  date: controller.selectedDate,
                  appointments: items,
                  professionals: controller.activeProfessionals,
                  settings: controller.data.settings,
                  selectedAppointmentId: _selectedAppointmentId,
                  compact: compact,
                  height: boardViewportHeight,
                  onAppointmentTap: _selectAppointment,
                  onEmptySlotTap: (start, professional) =>
                      _newAppointment(start: start, professional: professional),
                  onCreate: () => _newAppointment(),
                ),
                app.AgendaViewMode.list => _listView(items),
                app.AgendaViewMode.week => _weekView(),
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _agendaToolbar({
    required double availableWidth,
    required String countLabel,
    required String rangeLabel,
    required Color ink,
    required Color muted,
  }) {
    final showDate = availableWidth >= 720;
    final showRange = availableWidth >= 1040;
    final showCount = availableWidth >= 620;
    final showToday = availableWidth >= 900;
    return Row(
      children: [
        _modeSelector(compact: availableWidth < 500),
        const Spacer(),
        _dateStepButton(
          key: const Key('agenda-previous-day'),
          tooltip: 'Dia anterior',
          icon: Icons.chevron_left_rounded,
          onPressed: () => _selectRelativeDay(-1),
        ),
        if (showDate) ...[
          const SizedBox(width: 5),
          _toolbarDatePanel(ink: ink, muted: muted),
        ],
        const SizedBox(width: 5),
        _dateStepButton(
          key: const Key('agenda-next-day'),
          tooltip: 'Próximo dia',
          icon: Icons.chevron_right_rounded,
          onPressed: () => _selectRelativeDay(1),
        ),
        if (showToday && !_isToday(controller.selectedDate)) ...[
          const SizedBox(width: 6),
          SizedBox(
            height: 44,
            child: OutlinedButton(
              key: const Key('agenda-today-button'),
              onPressed: () {
                controller.selectDate(DateUtils.dateOnly(DateTime.now()));
                setState(() => _selectedAppointmentId = null);
              },
              child: const Text('Hoje'),
            ),
          ),
        ],
        if (showCount) ...[
          const Spacer(),
          Text(
            countLabel,
            key: const Key('agenda-toolbar-count'),
            style: TextStyle(
              color: AgendaThemeTokens.of(context).accentDark,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
        if (showRange) ...[
          const SizedBox(width: 10),
          Text(
            rangeLabel,
            key: const Key('agenda-toolbar-range'),
            style: TextStyle(
              color: muted,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ],
    );
  }

  Widget _dateStepButton({
    required Key key,
    required String tooltip,
    required IconData icon,
    required VoidCallback onPressed,
  }) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      width: 44,
      height: 44,
      child: IconButton(
        key: key,
        tooltip: tooltip,
        onPressed: onPressed,
        padding: EdgeInsets.zero,
        constraints: const BoxConstraints.tightFor(width: 44, height: 44),
        style: IconButton.styleFrom(
          foregroundColor: t.ink,
          backgroundColor: t.panel,
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
        ),
        icon: Icon(icon, size: 18),
      ),
    );
  }

  Widget _toolbarDatePanel({required Color ink, required Color muted}) {
    final t = AgendaThemeTokens.of(context);
    final date = controller.selectedDate;
    final base =
        '${_weekday(date).toLowerCase()}, ${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}';
    final label = _isToday(date) ? 'Hoje, $base' : base;
    return SizedBox(
      width: 180,
      height: 44,
      child: Material(
        key: const Key('agenda-toolbar-date'),
        color: t.panel,
        shape: RoundedRectangleBorder(
          side: BorderSide(color: t.line),
          borderRadius: BorderRadius.circular(10),
        ),
        child: InkWell(
          onTap: _pickDate,
          borderRadius: BorderRadius.circular(10),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Expanded(
                  child: Text(
                    label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontFamily: 'Segoe UI',
                      color: ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Icon(Icons.calendar_month_rounded, color: muted, size: 15),
              ],
            ),
          ),
        ),
      ),
    );
  }

  void _selectRelativeDay(int offset) {
    controller.selectDate(controller.selectedDate.add(Duration(days: offset)));
    setState(() => _selectedAppointmentId = null);
  }

  Widget _modeSelector({bool compact = false}) {
    return _AgendaModeSelector(
      value: controller.agendaMode,
      onChanged: controller.setAgendaMode,
      compact: compact,
    );
  }

  Widget _listView(List<Appointment> items) {
    if (items.isEmpty) {
      return AgendaEmptyState(
        icon: Icons.event_busy_outlined,
        title: 'Nenhum atendimento encontrado',
        message: controller.searchQuery.isEmpty
            ? 'Este dia ainda está livre. Crie um agendamento para começar.'
            : 'Tente remover a busca ou usar outro termo.',
        actionLabel: controller.searchQuery.isEmpty ? 'Agendar' : null,
        onAction: controller.searchQuery.isEmpty
            ? () => _newAppointment()
            : null,
      );
    }
    return Column(
      children: [
        for (var index = 0; index < items.length; index++) ...[
          _AppointmentListTile(
            appointment: items[index],
            selected: items[index].id == _selectedAppointmentId,
            onTap: () => _selectAppointment(items[index]),
            onEdit: () => _editAppointment(items[index]),
          ),
          if (index != items.length - 1) const SizedBox(height: 8),
        ],
      ],
    );
  }

  Widget _weekView() {
    final t = AgendaThemeTokens.of(context);
    final selected = controller.selectedDate;
    final monday = DateUtils.dateOnly(
      selected.subtract(Duration(days: selected.weekday - DateTime.monday)),
    );
    final weekEnd = monday.add(const Duration(days: 7));
    final weekItems = controller.appointmentsBetween(monday, weekEnd);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              for (var offset = 0; offset < 7; offset++) ...[
                _WeekDayCard(
                  date: monday.add(Duration(days: offset)),
                  count: weekItems
                      .where(
                        (item) => DateUtils.isSameDay(
                          item.start,
                          monday.add(Duration(days: offset)),
                        ),
                      )
                      .length,
                  selected: DateUtils.isSameDay(
                    selected,
                    monday.add(Duration(days: offset)),
                  ),
                  onTap: () =>
                      controller.selectDate(monday.add(Duration(days: offset))),
                ),
                if (offset != 6) const SizedBox(width: 8),
              ],
            ],
          ),
        ),
        const SizedBox(height: 12),
        if (weekItems.isEmpty)
          AgendaEmptyState(
            icon: Icons.calendar_view_week_outlined,
            title: 'Semana livre',
            message: 'Não há atendimentos agendados nesta semana.',
            actionLabel: 'Agendar',
            onAction: () => _newAppointment(),
          )
        else
          for (var day = 0; day < 7; day++)
            if (weekItems
                    .where(
                      (item) => DateUtils.isSameDay(
                        item.start,
                        monday.add(Duration(days: day)),
                      ),
                    )
                    .toList()
                case final dayItems when dayItems.isNotEmpty) ...[
              Padding(
                padding: const EdgeInsets.only(top: 8, bottom: 7),
                child: Text(
                  '${_weekday(monday.add(Duration(days: day)))} • ${shortDate(monday.add(Duration(days: day)))}',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              for (final item in dayItems) ...[
                _AppointmentListTile(
                  appointment: item,
                  selected: item.id == _selectedAppointmentId,
                  onTap: () {
                    controller.selectDate(item.start);
                    _selectAppointment(item);
                  },
                  onEdit: () => _editAppointment(item),
                ),
                const SizedBox(height: 7),
              ],
            ],
      ],
    );
  }

  Widget _appointmentFlowCard(List<Appointment> items, {bool fixed = false}) {
    final t = AgendaThemeTokens.of(context);
    final operational =
        items
            .where(
              (item) => !const {
                AppointmentStatus.cancelled,
                AppointmentStatus.noShow,
                AppointmentStatus.blocked,
              }.contains(item.status),
            )
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start));
    final awaiting = operational
        .where(
          (item) => const {
            AppointmentStatus.scheduled,
            AppointmentStatus.confirmed,
          }.contains(item.status),
        )
        .toList();
    final inService = operational
        .where(
          (item) => const {
            AppointmentStatus.waiting,
            AppointmentStatus.inService,
          }.contains(item.status),
        )
        .toList();
    final readyForPayment = operational
        .where(
          (item) =>
              item.status == AppointmentStatus.done &&
              item.paymentConfirmedAt == null,
        )
        .toList();

    final sections = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _appointmentFlowSection(
          key: const Key('agenda-flow-awaiting'),
          title: 'Aguardando chegada',
          emptyText: 'Nenhuma chegada aguardando.',
          icon: Icons.account_circle_outlined,
          tone: t.accentDark,
          countBackground: const Color(0xFFF5F3F1),
          countForeground: t.ink,
          items: awaiting,
          visibleCount: 2,
          actionLabel: 'Confirmar chegada',
          onAction: (item) => _setStatus(item, AppointmentStatus.waiting),
        ),
        const SizedBox(height: 9),
        _appointmentFlowSection(
          key: const Key('agenda-flow-in-service'),
          title: 'Em atendimento',
          emptyText: 'Nenhum atendimento em andamento.',
          icon: Icons.pending_actions_rounded,
          tone: const Color(0xFFD97706),
          countBackground: const Color(0xFFFFF7E6),
          countForeground: const Color(0xFF9A5A00),
          items: inService,
          visibleCount: 1,
          metaFor: (item) => '${item.durationMinutes.clamp(15, 180)} min',
        ),
        const SizedBox(height: 9),
        _appointmentFlowSection(
          key: const Key('agenda-flow-payment'),
          title: 'Pronto para cobrar',
          emptyText: 'Nenhum pagamento pendente.',
          icon: Icons.payments_outlined,
          tone: const Color(0xFF15803D),
          countBackground: const Color(0xFFEAFBF2),
          countForeground: const Color(0xFF15803D),
          items: readyForPayment,
          visibleCount: 2,
          actionLabel: 'Receber',
          onAction: _openPayment,
        ),
      ],
    );

    return AgendaPanel(
      key: const Key('agenda-appointment-flow'),
      radius: 16,
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(15, 14, 15, 0),
            child: Row(
              children: [
                Icon(Icons.swap_vert_rounded, color: t.accentDark, size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Fluxo de atendimento',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontFamily: 'Segoe UI',
                      color: t.ink,
                      fontSize: 17,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          if (fixed)
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 15),
                child: sections,
              ),
            )
          else
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 15),
              child: sections,
            ),
          Padding(
            padding: const EdgeInsets.fromLTRB(15, 11, 15, 14),
            child: OutlinedButton(
              key: const Key('agenda-flow-view-all'),
              onPressed: () =>
                  controller.setAgendaMode(app.AgendaViewMode.list),
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(38),
                foregroundColor: t.ink,
                side: BorderSide(color: t.line),
                textStyle: const TextStyle(
                  fontFamily: 'Segoe UI',
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              child: const Text('Ver agenda completa  ›'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _appointmentFlowSection({
    required Key key,
    required String title,
    required String emptyText,
    required IconData icon,
    required Color tone,
    required Color countBackground,
    required Color countForeground,
    required List<Appointment> items,
    required int visibleCount,
    String? actionLabel,
    ValueChanged<Appointment>? onAction,
    String Function(Appointment)? metaFor,
  }) {
    final t = AgendaThemeTokens.of(context);
    final visible = items.take(visibleCount).toList();
    return Container(
      key: key,
      padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 8),
      decoration: BoxDecoration(
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, color: tone, size: 18),
              const SizedBox(width: 7),
              Expanded(
                child: Text(
                  title,
                  style: TextStyle(
                    color: tone,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                decoration: BoxDecoration(
                  color: countBackground,
                  borderRadius: BorderRadius.circular(7),
                ),
                child: Text(
                  '${items.length}',
                  style: TextStyle(
                    color: countForeground,
                    fontSize: 10.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
          if (visible.isEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(0, 8, 0, 4),
              child: Text(
                emptyText,
                style: TextStyle(color: t.muted, fontSize: 10.5),
              ),
            )
          else
            for (final item in visible)
              _appointmentFlowRow(
                item,
                tone: tone,
                actionLabel: actionLabel,
                onAction: onAction,
                meta: metaFor?.call(item),
              ),
        ],
      ),
    );
  }

  Widget _appointmentFlowRow(
    Appointment item, {
    required Color tone,
    String? actionLabel,
    ValueChanged<Appointment>? onAction,
    String? meta,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 9),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 39,
            child: Text(
              hour(item.start),
              style: TextStyle(color: t.muted, fontSize: 10.5),
            ),
          ),
          SizedBox(
            width: 37,
            child: CircleAvatar(
              radius: 15.5,
              backgroundColor: t.accentSoft,
              child: Text(
                initials(item.customerName),
                style: TextStyle(
                  color: t.accentDark,
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
          const SizedBox(width: 7),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.customerName.trim().isEmpty
                      ? 'Cliente'
                      : item.customerName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 1),
                Text(
                  item.serviceName.trim().isEmpty
                      ? 'Atendimento'
                      : item.serviceName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 10),
                ),
              ],
            ),
          ),
          const SizedBox(width: 7),
          if (actionLabel != null && onAction != null)
            OutlinedButton(
              key: Key('agenda-flow-action-${item.id}'),
              onPressed: () => onAction(item),
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(102, 30),
                maximumSize: const Size(112, 30),
                padding: const EdgeInsets.symmetric(horizontal: 6),
                foregroundColor: tone,
                backgroundColor: t.panel,
                side: BorderSide(color: tone),
                textStyle: const TextStyle(
                  fontSize: 9.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
              child: Text(
                actionLabel,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            )
          else if (meta != null)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 5),
              decoration: BoxDecoration(
                color: const Color(0xFFFFF7E6),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.schedule_rounded, color: t.ink, size: 13),
                  const SizedBox(width: 4),
                  Text(
                    meta,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 10.5,
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

  Appointment? _selected(List<Appointment> items) {
    if (_selectedAppointmentId == null) return null;
    return items.where((item) => item.id == _selectedAppointmentId).firstOrNull;
  }

  void _selectAppointment(Appointment item) {
    setState(() => _selectedAppointmentId = item.id);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted || !_pageScrollController.hasClients) return;
      _pageScrollController.animateTo(
        0,
        duration: const Duration(milliseconds: 260),
        curve: Curves.easeOutCubic,
      );
    });
  }

  int _availableSlots(List<Appointment> items) {
    final settings = controller.data.settings;
    final professionalCount = math.max(
      1,
      controller.activeProfessionals.length,
    );
    final workdayMinutes = math.max(
      60,
      (settings.workdayEndHour - settings.workdayStartHour) * 60,
    );
    final busyMinutes = items
        .where(isOpenAppointment)
        .fold<int>(0, (sum, item) => sum + math.max(15, item.durationMinutes));
    return math.max(
      0,
      ((workdayMinutes * professionalCount) - busyMinutes) ~/ 30,
    );
  }

  Future<void> _pickDate() async {
    final selected = await showDatePicker(
      context: context,
      initialDate: controller.selectedDate,
      firstDate: DateTime(2020),
      lastDate: DateTime(DateTime.now().year + 5, 12, 31),
    );
    if (selected != null) {
      controller.selectDate(selected);
      setState(() => _selectedAppointmentId = null);
    }
  }

  Future<void> _newAppointment({
    DateTime? start,
    Professional? professional,
  }) async {
    final selectedStart =
        start ??
        DateTime(
          controller.selectedDate.year,
          controller.selectedDate.month,
          controller.selectedDate.day,
          math.max(
            controller.data.settings.workdayStartHour,
            _isToday(controller.selectedDate) ? DateTime.now().hour + 1 : 0,
          ),
        );
    await showAppointmentDialog(
      context,
      controller,
      initialStart: selectedStart,
    );
  }

  Future<void> _editAppointment(Appointment item) async {
    await showAppointmentDialog(context, controller, appointment: item);
  }

  Future<void> _openPayment(Appointment item) async {
    await showAppointmentPaymentDialog(
      context,
      controller,
      item,
      onEdit: () => _editAppointment(item),
    );
  }

  Future<void> _setStatus(Appointment item, AppointmentStatus target) async {
    final error = await controller.setAppointmentStatus(item, target);
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            error ?? 'Status alterado para ${appointmentStatusLabel(target)}.',
          ),
        ),
      );
  }

  void _openWhatsApp(Appointment item) {
    if (widget.onWhatsApp != null) {
      widget.onWhatsApp!(item);
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'WhatsApp de ${item.customerName}: ${item.customerPhone}',
        ),
      ),
    );
  }
}

class _AgendaModeSelector extends StatelessWidget {
  const _AgendaModeSelector({
    required this.value,
    required this.onChanged,
    required this.compact,
  });

  final app.AgendaViewMode value;
  final ValueChanged<app.AgendaViewMode> onChanged;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(
        color: t.accentSoft,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _button(context, app.AgendaViewMode.board, 'Quadro'),
          _button(context, app.AgendaViewMode.list, 'Lista'),
          _button(context, app.AgendaViewMode.week, 'Semana'),
        ],
      ),
    );
  }

  Widget _button(BuildContext context, app.AgendaViewMode mode, String label) {
    final t = AgendaThemeTokens.of(context);
    final selected = value == mode;
    return Material(
      color: selected ? t.accent : Colors.transparent,
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        onTap: () => onChanged(mode),
        borderRadius: BorderRadius.circular(8),
        child: SizedBox(
          width: compact ? 68 : 80,
          height: 42,
          child: Center(
            child: Text(
              label,
              style: TextStyle(
                color: selected ? Colors.white : t.muted,
                fontSize: 12,
                fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _AppointmentListTile extends StatelessWidget {
  const _AppointmentListTile({
    required this.appointment,
    required this.selected,
    required this.onTap,
    required this.onEdit,
  });

  final Appointment appointment;
  final bool selected;
  final VoidCallback onTap;
  final VoidCallback onEdit;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final status = appointmentStatusStyle(context, appointment.status);
    return Material(
      color: selected ? t.warmSoft : t.panel,
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(8),
        child: Container(
          padding: const EdgeInsets.all(11),
          decoration: BoxDecoration(
            border: Border.all(color: selected ? t.accent : t.line),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Row(
            children: [
              Container(
                width: 4,
                height: 45,
                decoration: BoxDecoration(
                  color: status.foreground,
                  borderRadius: BorderRadius.circular(99),
                ),
              ),
              const SizedBox(width: 10),
              SizedBox(
                width: 52,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      hour(appointment.start),
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 14,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      '${appointment.durationMinutes} min',
                      style: TextStyle(color: t.muted, fontSize: 9.5),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
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
                        fontSize: 12.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${appointment.serviceName} • ${appointment.professionalName}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              AppointmentStatusBadge(status: appointment.status, compact: true),
              IconButton(
                tooltip: 'Editar',
                onPressed: onEdit,
                icon: const Icon(Icons.edit_outlined, size: 18),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _WeekDayCard extends StatelessWidget {
  const _WeekDayCard({
    required this.date,
    required this.count,
    required this.selected,
    required this.onTap,
  });

  final DateTime date;
  final int count;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      width: 92,
      child: Material(
        color: selected ? t.accentSoft : t.panel,
        borderRadius: BorderRadius.circular(8),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(8),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 10),
            decoration: BoxDecoration(
              border: Border.all(color: selected ? t.accent : t.line),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(
              children: [
                Text(
                  _weekday(date).substring(0, 3).toUpperCase(),
                  style: TextStyle(
                    color: selected ? t.accentDark : t.muted,
                    fontSize: 9,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  '${date.day}',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                Text(
                  '$count atendimento${count == 1 ? '' : 's'}',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: t.muted, fontSize: 8.5),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

bool _isToday(DateTime date) => DateUtils.isSameDay(date, DateTime.now());

String _weekday(DateTime date) => const [
  'Segunda-feira',
  'Terça-feira',
  'Quarta-feira',
  'Quinta-feira',
  'Sexta-feira',
  'Sábado',
  'Domingo',
][date.weekday - 1];
