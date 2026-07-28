enum AppointmentStatus {
  scheduled,
  confirmed,
  waiting,
  inService,
  done,
  cancelled,
  noShow,
  blocked,
}

extension AppointmentStatusJson on AppointmentStatus {
  String get jsonValue => switch (this) {
    AppointmentStatus.scheduled => 'Scheduled',
    AppointmentStatus.confirmed => 'Confirmed',
    AppointmentStatus.waiting => 'Waiting',
    AppointmentStatus.inService => 'InService',
    AppointmentStatus.done => 'Done',
    AppointmentStatus.cancelled => 'Cancelled',
    AppointmentStatus.noShow => 'NoShow',
    AppointmentStatus.blocked => 'Blocked',
  };
}

AppointmentStatus appointmentStatusFromJson(Object? value) {
  if (value is num) {
    final index = value.toInt();
    if (index >= 0 && index < AppointmentStatus.values.length) {
      return AppointmentStatus.values[index];
    }
  }

  final normalized = (value?.toString() ?? '')
      .replaceAll(RegExp(r'[^a-zA-Z]'), '')
      .toLowerCase();
  return switch (normalized) {
    'confirmed' => AppointmentStatus.confirmed,
    'waiting' => AppointmentStatus.waiting,
    'inservice' => AppointmentStatus.inService,
    'done' => AppointmentStatus.done,
    'cancelled' || 'canceled' => AppointmentStatus.cancelled,
    'noshow' => AppointmentStatus.noShow,
    'blocked' => AppointmentStatus.blocked,
    _ => AppointmentStatus.scheduled,
  };
}
