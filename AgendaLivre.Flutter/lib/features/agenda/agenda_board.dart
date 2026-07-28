import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import 'appointment_visuals.dart';

typedef EmptyAgendaSlotCallback =
    void Function(DateTime start, Professional? professional);

class AgendaScheduleBoard extends StatelessWidget {
  const AgendaScheduleBoard({
    super.key,
    required this.date,
    required this.appointments,
    required this.professionals,
    required this.settings,
    this.onAppointmentTap,
    this.onEmptySlotTap,
    this.onCreate,
    this.selectedAppointmentId,
    this.height = 342,
    this.compact = false,
    this.slotMinutes = 30,
    this.rowHeight,
    this.timeColumnWidth,
    this.headerHeight,
    this.emptyTitle = 'Agenda livre nesta data',
    this.emptyMessage =
        'Clique em um horário no quadro ou use o botão abaixo para criar o primeiro atendimento.',
    this.emptyActionLabel = '+ Agendar horário',
    this.radius = 14,
  });

  final DateTime date;
  final List<Appointment> appointments;
  final List<Professional> professionals;
  final AgendaSettings settings;
  final ValueChanged<Appointment>? onAppointmentTap;
  final EmptyAgendaSlotCallback? onEmptySlotTap;
  final VoidCallback? onCreate;
  final String? selectedAppointmentId;
  final double height;
  final bool compact;
  final int slotMinutes;
  final double? rowHeight;
  final double? timeColumnWidth;
  final double? headerHeight;
  final String emptyTitle;
  final String emptyMessage;
  final String emptyActionLabel;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final startHour = settings.workdayStartHour.clamp(0, 23);
    final endHour = math.max(
      startHour + 1,
      settings.workdayEndHour.clamp(1, 24),
    );
    final safeSlotMinutes = slotMinutes.clamp(15, 60);
    final slotCount = ((endHour - startHour) * 60 / safeSlotMinutes).ceil();
    final slotHeight = rowHeight ?? (compact ? 34.0 : 38.0);
    final timeWidth = timeColumnWidth ?? (compact ? 58.0 : 68.0);
    final columns = _columns();

    return LayoutBuilder(
      builder: (context, constraints) {
        final narrowCompact = compact && constraints.maxWidth < 480;
        final minColumnWidth = narrowCompact
            ? 130.0
            : (compact ? 172.0 : 232.0);
        final availableBoardWidth = compact
            ? constraints.maxWidth
            : math.max(620.0, constraints.maxWidth);
        final fitColumnWidth = columns.isEmpty
            ? availableBoardWidth - timeWidth
            : (availableBoardWidth - timeWidth) / columns.length;
        final columnWidth = math.max(minColumnWidth, fitColumnWidth);
        final boardWidth = timeWidth + columnWidth * columns.length;
        final visibleContentWidth = math.max(
          0.0,
          math.min(boardWidth - timeWidth, constraints.maxWidth - timeWidth),
        );

        return ClipRRect(
          borderRadius: BorderRadius.circular(radius),
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: t.panel,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(radius),
            ),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: SizedBox(
                key: const Key('agenda-board-canvas'),
                width: boardWidth,
                child: Column(
                  children: [
                    _BoardHeader(
                      columns: columns,
                      timeWidth: timeWidth,
                      columnWidth: columnWidth,
                      compact: compact,
                      height: headerHeight,
                    ),
                    Divider(height: 1, thickness: 1, color: t.line),
                    SizedBox(
                      key: const Key('agenda-board-viewport'),
                      height: height,
                      child: Stack(
                        children: [
                          SingleChildScrollView(
                            child: SizedBox(
                              width: boardWidth,
                              height: slotCount * slotHeight,
                              child: Stack(
                                children: [
                                  for (var slot = 0; slot < slotCount; slot++)
                                    for (
                                      var column = 0;
                                      column < columns.length;
                                      column++
                                    )
                                      Positioned(
                                        left: timeWidth + column * columnWidth,
                                        top: slot * slotHeight,
                                        width: columnWidth,
                                        height: slotHeight,
                                        child: InkWell(
                                          onTap: onEmptySlotTap == null
                                              ? null
                                              : () => onEmptySlotTap!(
                                                  DateTime(
                                                    date.year,
                                                    date.month,
                                                    date.day,
                                                    startHour +
                                                        (slot * safeSlotMinutes) ~/
                                                            60,
                                                    (slot * safeSlotMinutes) %
                                                        60,
                                                  ),
                                                  columns[column].professional,
                                                ),
                                          child: DecoratedBox(
                                            decoration: BoxDecoration(
                                              border: Border(
                                                right: BorderSide(
                                                  color: t.line,
                                                ),
                                                bottom: BorderSide(
                                                  color: t.line.withValues(
                                                    alpha:
                                                        safeSlotMinutes == 60 ||
                                                            slot.isOdd
                                                        ? 1
                                                        : .55,
                                                  ),
                                                ),
                                              ),
                                            ),
                                          ),
                                        ),
                                      ),
                                  for (var slot = 0; slot < slotCount; slot++)
                                    Positioned(
                                      left: 0,
                                      top: slot * slotHeight,
                                      width: timeWidth,
                                      height: slotHeight,
                                      child: Container(
                                        padding: const EdgeInsets.only(
                                          right: 10,
                                          top: 7,
                                        ),
                                        alignment: Alignment.topRight,
                                        decoration: BoxDecoration(
                                          color: t.warmSoft.withValues(
                                            alpha: .42,
                                          ),
                                          border: Border(
                                            right: BorderSide(color: t.line),
                                            bottom: BorderSide(
                                              color: t.line.withValues(
                                                alpha:
                                                    safeSlotMinutes == 60 ||
                                                        slot.isOdd
                                                    ? 1
                                                    : .55,
                                              ),
                                            ),
                                          ),
                                        ),
                                        child:
                                            (slot * safeSlotMinutes) % 60 == 0
                                            ? Text(
                                                '${(startHour + (slot * safeSlotMinutes) ~/ 60).toString().padLeft(2, '0')}:00',
                                                style: TextStyle(
                                                  color: t.muted,
                                                  fontSize: compact ? 10 : 11,
                                                  fontWeight: FontWeight.w600,
                                                ),
                                              )
                                            : null,
                                      ),
                                    ),
                                  for (final item in appointments)
                                    if (_positionFor(
                                          item,
                                          columns,
                                          startHour,
                                          endHour,
                                          timeWidth,
                                          columnWidth,
                                          slotHeight,
                                          safeSlotMinutes,
                                        )
                                        case final position?)
                                      Positioned(
                                        left: position.left,
                                        top: position.top,
                                        width: position.width,
                                        height: position.height,
                                        child: _AppointmentBoardCard(
                                          key: ValueKey(
                                            'agenda-board-appointment-${item.id}',
                                          ),
                                          appointment: item,
                                          selected:
                                              item.id == selectedAppointmentId,
                                          compact: compact,
                                          onTap: onAppointmentTap == null
                                              ? null
                                              : () => onAppointmentTap!(item),
                                        ),
                                      ),
                                ],
                              ),
                            ),
                          ),
                          if (appointments.isEmpty)
                            Positioned(
                              left: timeWidth,
                              top: 0,
                              bottom: 0,
                              width: compact
                                  ? visibleContentWidth
                                  : boardWidth - timeWidth,
                              child: IgnorePointer(
                                ignoring: onCreate == null,
                                child: Center(
                                  child: ConstrainedBox(
                                    constraints: const BoxConstraints(
                                      maxWidth: 368,
                                    ),
                                    child: DecoratedBox(
                                      key: const Key(
                                        'agenda-board-empty-state',
                                      ),
                                      decoration: BoxDecoration(
                                        color: t.panel.withValues(alpha: .96),
                                        border: Border.all(color: t.line),
                                        borderRadius: BorderRadius.circular(14),
                                      ),
                                      child: AgendaEmptyState(
                                        icon: Icons.event_available_rounded,
                                        title: emptyTitle,
                                        message: emptyMessage,
                                        actionLabel: onCreate == null
                                            ? null
                                            : emptyActionLabel,
                                        onAction: onCreate,
                                        compact: true,
                                      ),
                                    ),
                                  ),
                                ),
                              ),
                            ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      },
    );
  }

  List<_BoardColumn> _columns() {
    if (professionals.isNotEmpty) {
      return [
        for (final professional in professionals)
          _BoardColumn(
            id: professional.id,
            name: professional.name.trim().isEmpty
                ? 'Profissional'
                : professional.name,
            subtitle: professional.role,
            professional: professional,
          ),
      ];
    }

    final names = appointments
        .map((item) => item.professionalName.trim())
        .where((name) => name.isNotEmpty)
        .toSet()
        .toList();
    if (names.isNotEmpty) {
      return [
        for (final name in names)
          _BoardColumn(id: name, name: name, subtitle: '', professional: null),
      ];
    }
    return const [
      _BoardColumn(
        id: '',
        name: 'Agenda geral',
        subtitle: 'Sem profissional definido',
        professional: null,
      ),
    ];
  }

  _BoardPosition? _positionFor(
    Appointment item,
    List<_BoardColumn> columns,
    int startHour,
    int endHour,
    double timeWidth,
    double columnWidth,
    double slotHeight,
    int slotMinutes,
  ) {
    if (item.start.year != date.year ||
        item.start.month != date.month ||
        item.start.day != date.day ||
        item.status == AppointmentStatus.blocked) {
      return null;
    }
    final minutesFromStart =
        item.start.hour * 60 + item.start.minute - startHour * 60;
    if (minutesFromStart < 0 ||
        minutesFromStart >= (endHour - startHour) * 60) {
      return null;
    }
    var column = columns.indexWhere(
      (candidate) =>
          item.professionalId.isNotEmpty && candidate.id == item.professionalId,
    );
    if (column < 0) {
      column = columns.indexWhere(
        (candidate) =>
            item.professionalName.isNotEmpty &&
            candidate.name.toLowerCase() == item.professionalName.toLowerCase(),
      );
    }
    if (column < 0) column = 0;
    final top = minutesFromStart / slotMinutes * slotHeight + 3;
    final maximumHeight =
        (endHour * 60 - item.start.hour * 60 - item.start.minute) /
        slotMinutes *
        slotHeight;
    return _BoardPosition(
      left: timeWidth + column * columnWidth + 4,
      top: top,
      width: columnWidth - 8,
      height: math.max(
        28,
        math.min(
          item.durationMinutes / slotMinutes * slotHeight - 6,
          maximumHeight - 4,
        ),
      ),
    );
  }
}

class _BoardHeader extends StatelessWidget {
  const _BoardHeader({
    required this.columns,
    required this.timeWidth,
    required this.columnWidth,
    required this.compact,
    this.height,
  });

  final List<_BoardColumn> columns;
  final double timeWidth;
  final double columnWidth;
  final bool compact;
  final double? height;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      height: height ?? (compact ? 48 : 52),
      child: Row(
        children: [
          Container(
            width: timeWidth,
            height: double.infinity,
            alignment: Alignment.center,
            color: t.warmSoft.withValues(alpha: .5),
            child: Text(
              'Horário',
              style: TextStyle(
                color: t.ink,
                fontSize: compact ? 10 : 11,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          for (final column in columns)
            Container(
              width: columnWidth,
              height: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 12),
              decoration: BoxDecoration(
                border: Border(left: BorderSide(color: t.line)),
              ),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: compact ? 14 : 16,
                    backgroundColor: t.accentSoft,
                    child: Text(
                      initials(column.name),
                      style: TextStyle(
                        color: t.accentDark,
                        fontSize: compact ? 9 : 10,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  const SizedBox(width: 9),
                  Expanded(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          column.name,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: compact ? 11 : 12,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        if (column.subtitle.trim().isNotEmpty)
                          Text(
                            column.subtitle,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(color: t.muted, fontSize: 9.5),
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

class _AppointmentBoardCard extends StatelessWidget {
  const _AppointmentBoardCard({
    super.key,
    required this.appointment,
    required this.selected,
    required this.compact,
    this.onTap,
  });

  final Appointment appointment;
  final bool selected;
  final bool compact;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final status = appointmentStatusStyle(context, appointment.status);
    final service = appointment.serviceName.trim().isEmpty
        ? appointmentStatusLabel(appointment.status)
        : appointment.serviceName;
    final semanticsLabel =
        '${hour(appointment.start)}, ${appointment.customerName}, $service, '
        '${appointmentStatusLabel(appointment.status)}';
    return Semantics(
      button: onTap != null,
      selected: selected,
      label: semanticsLabel,
      child: Tooltip(
        message: semanticsLabel,
        waitDuration: const Duration(milliseconds: 650),
        child: Material(
          color: status.background,
          borderRadius: BorderRadius.circular(7),
          child: InkWell(
            onTap: onTap,
            borderRadius: BorderRadius.circular(7),
            child: LayoutBuilder(
              builder: (context, constraints) {
                final dense = constraints.maxHeight < 46;
                return Container(
                  padding: EdgeInsets.symmetric(
                    horizontal: compact ? 7 : 9,
                    vertical: dense ? 3 : (compact ? 5 : 6),
                  ),
                  decoration: BoxDecoration(
                    border: Border.all(
                      color: selected
                          ? t.ink
                          : status.foreground.withValues(alpha: .65),
                      width: selected ? 1.7 : 1,
                    ),
                    borderRadius: BorderRadius.circular(7),
                  ),
                  child: Row(
                    crossAxisAlignment: dense
                        ? CrossAxisAlignment.center
                        : CrossAxisAlignment.stretch,
                    children: [
                      Container(
                        width: 3,
                        height: dense ? 18 : double.infinity,
                        decoration: BoxDecoration(
                          color: status.foreground,
                          borderRadius: BorderRadius.circular(99),
                        ),
                      ),
                      const SizedBox(width: 7),
                      Expanded(
                        child: dense
                            ? Text(
                                '${hour(appointment.start)}  ${appointment.customerName}'
                                '${constraints.maxWidth >= 210 ? '  ·  $service' : ''}',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: TextStyle(
                                  color: t.ink,
                                  fontSize: compact ? 10 : 11,
                                  fontWeight: FontWeight.w800,
                                ),
                              )
                            : Column(
                                mainAxisAlignment: MainAxisAlignment.center,
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${hour(appointment.start)}  ${appointment.customerName}',
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: TextStyle(
                                      color: t.ink,
                                      fontSize: compact ? 10.5 : 11.5,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                  if (appointment.durationMinutes >= 30)
                                    Text(
                                      service,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: TextStyle(
                                        color: t.muted,
                                        fontSize: compact ? 9 : 10,
                                      ),
                                    ),
                                ],
                              ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        ),
      ),
    );
  }
}

class _BoardColumn {
  const _BoardColumn({
    required this.id,
    required this.name,
    required this.subtitle,
    required this.professional,
  });

  final String id;
  final String name;
  final String subtitle;
  final Professional? professional;
}

class _BoardPosition {
  const _BoardPosition({
    required this.left,
    required this.top,
    required this.width,
    required this.height,
  });

  final double left;
  final double top;
  final double width;
  final double height;
}
