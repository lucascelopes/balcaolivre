import '../models/agenda_data.dart';

abstract interface class AgendaRepository {
  Future<bool> hasData();

  Future<AgendaData?> load();

  Future<AgendaData> loadOrCreate();

  Future<void> save(AgendaData data);

  Future<void> clear();
}

/// Optional capabilities exposed by repositories that synchronize with a
/// shared account. The regular repository contract remains unchanged for
/// Android, tests and other local-only platforms.
abstract interface class AgendaSyncRepository {
  bool get hasConflict;

  bool get isSyncing;

  String? get syncMessage;

  /// Whether the server has returned a trial status for this account.
  bool get hasTrialStatus;

  bool get trialActive;

  int get trialDaysRemaining;

  /// Rebases the preserved local work on the latest cloud revision and saves
  /// it with compare-and-swap semantics.
  Future<AgendaData?> resolveConflictUsingLocal();

  /// Uses the cloud snapshot after persisting a recovery copy of local work.
  Future<AgendaData?> resolveConflictUsingCloud();

  /// Checks whether a newer cloud revision can be applied without replacing
  /// local work. Returns the applied data, or `null` when nothing changed or
  /// the repository is not in a safe state to pull.
  Future<AgendaData?> refreshRemoteIfSafe();

  Future<void> retrySync();
}

abstract interface class AgendaEntitlementRepository {
  String get entitlementStatus;

  bool get entitlementCanUse;
}
