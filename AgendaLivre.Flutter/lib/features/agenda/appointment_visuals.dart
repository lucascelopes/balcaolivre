import 'package:flutter/material.dart';

import '../../app/theme/agenda_theme.dart';
import '../../domain/models/models.dart';

String appointmentStatusLabel(AppointmentStatus status) => switch (status) {
  AppointmentStatus.scheduled => 'Agendado',
  AppointmentStatus.confirmed => 'Confirmado',
  AppointmentStatus.waiting => 'Chegou',
  AppointmentStatus.inService => 'Em atendimento',
  AppointmentStatus.done => 'Finalizado',
  AppointmentStatus.cancelled => 'Cancelado',
  AppointmentStatus.noShow => 'Faltou',
  AppointmentStatus.blocked => 'Bloqueado',
};

IconData appointmentStatusIcon(AppointmentStatus status) => switch (status) {
  AppointmentStatus.scheduled => Icons.schedule_rounded,
  AppointmentStatus.confirmed => Icons.verified_rounded,
  AppointmentStatus.waiting => Icons.person_pin_circle_rounded,
  AppointmentStatus.inService => Icons.play_circle_fill_rounded,
  AppointmentStatus.done => Icons.check_circle_rounded,
  AppointmentStatus.cancelled => Icons.cancel_rounded,
  AppointmentStatus.noShow => Icons.person_off_rounded,
  AppointmentStatus.blocked => Icons.block_rounded,
};

bool isOpenAppointment(Appointment appointment) => const {
  AppointmentStatus.scheduled,
  AppointmentStatus.confirmed,
  AppointmentStatus.waiting,
  AppointmentStatus.inService,
}.contains(appointment.status);

@immutable
class AppointmentStatusStyle {
  const AppointmentStatusStyle({
    required this.foreground,
    required this.background,
  });

  final Color foreground;
  final Color background;
}

AppointmentStatusStyle appointmentStatusStyle(
  BuildContext context,
  AppointmentStatus status,
) {
  final t = AgendaThemeTokens.of(context);
  return switch (status) {
    AppointmentStatus.scheduled => AppointmentStatusStyle(
      foreground: t.accentDark,
      background: t.warmSoft,
    ),
    AppointmentStatus.confirmed => AppointmentStatusStyle(
      foreground: t.accent,
      background: t.accentSoft,
    ),
    AppointmentStatus.waiting => AppointmentStatusStyle(
      foreground: const Color(0xFF2563EB),
      background: t.blueSoft,
    ),
    AppointmentStatus.inService => const AppointmentStatusStyle(
      foreground: Color(0xFF10B981),
      background: Color(0xFFECFDF5),
    ),
    AppointmentStatus.done => const AppointmentStatusStyle(
      foreground: Color(0xFF16A34A),
      background: Color(0xFFECFDF5),
    ),
    AppointmentStatus.cancelled ||
    AppointmentStatus.noShow => AppointmentStatusStyle(
      foreground: const Color(0xFFDC2626),
      background: t.redSoft,
    ),
    AppointmentStatus.blocked => AppointmentStatusStyle(
      foreground: const Color(0xFF64748B),
      background: t.graySoft,
    ),
  };
}

class AppointmentStatusBadge extends StatelessWidget {
  const AppointmentStatusBadge({
    super.key,
    required this.status,
    this.compact = false,
  });

  final AppointmentStatus status;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final style = appointmentStatusStyle(context, status);
    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 7 : 9,
        vertical: compact ? 4 : 5,
      ),
      decoration: BoxDecoration(
        color: style.background,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            appointmentStatusIcon(status),
            size: compact ? 12 : 14,
            color: style.foreground,
          ),
          const SizedBox(width: 4),
          Text(
            appointmentStatusLabel(status),
            style: TextStyle(
              color: style.foreground,
              fontSize: compact ? 10 : 11,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

@immutable
class AgendaDarkMetricData {
  const AgendaDarkMetricData({
    required this.label,
    required this.value,
    required this.caption,
    required this.icon,
    this.tone,
  });

  final String label;
  final String value;
  final String caption;
  final IconData icon;
  final Color? tone;
}

/// The dark, four-column operational strip used by the Windows dashboard.
///
/// It stays a single row on desktop and becomes a two-column/one-column grid
/// on narrow layouts so the same information remains readable on mobile.
class AgendaDarkMetricStrip extends StatelessWidget {
  const AgendaDarkMetricStrip({
    super.key,
    required this.metrics,
    this.compact = false,
  });

  final List<AgendaDarkMetricData> metrics;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 760
            ? metrics.length
            : constraints.maxWidth >= 330
            ? 2
            : 1;
        final rows = (metrics.length / columns).ceil();
        final itemWidth = (constraints.maxWidth - 16 - (columns - 1)) / columns;

        return Container(
          decoration: BoxDecoration(
            color: const Color(0xFF171614),
            borderRadius: BorderRadius.circular(compact ? 18 : 22),
          ),
          padding: const EdgeInsets.all(8),
          child: Wrap(
            children: [
              for (var index = 0; index < metrics.length; index++)
                SizedBox(
                  width: itemWidth,
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      border: Border(
                        right: index % columns == columns - 1
                            ? BorderSide.none
                            : const BorderSide(color: Color(0xFF3A3734)),
                        bottom: index ~/ columns == rows - 1
                            ? BorderSide.none
                            : const BorderSide(color: Color(0xFF3A3734)),
                      ),
                    ),
                    child: _AgendaDarkMetric(
                      metric: metrics[index],
                      accent: t.accent,
                      compact: columns < 4,
                    ),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}

class _AgendaDarkMetric extends StatelessWidget {
  const _AgendaDarkMetric({
    required this.metric,
    required this.accent,
    required this.compact,
  });

  final AgendaDarkMetricData metric;
  final Color accent;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final tone = metric.tone ?? accent;
    return ConstrainedBox(
      constraints: BoxConstraints(minHeight: compact ? 78 : 83),
      child: Padding(
        padding: EdgeInsets.symmetric(
          horizontal: compact ? 10 : 14,
          vertical: 10,
        ),
        child: Row(
          children: [
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: const Color(0xFF2A2826),
                borderRadius: BorderRadius.circular(14),
              ),
              alignment: Alignment.center,
              child: Icon(metric.icon, color: tone, size: 21),
            ),
            SizedBox(width: compact ? 9 : 12),
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    metric.label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: const Color(0xFFC9C4BE),
                      fontSize: compact ? 10 : 11.5,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    metric.value,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 23,
                      fontWeight: FontWeight.w600,
                      height: 1.05,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    metric.caption,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: const Color(0xFF938D87),
                      fontSize: compact ? 9.5 : 10.5,
                    ),
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
