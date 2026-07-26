import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../domain/models/agenda_data.dart';
import '../../domain/models/agenda_settings.dart';
import '../../domain/repositories/agenda_repository.dart';
import '../../services/agenda_account_api.dart';

typedef AgendaUnauthorizedCallback = FutureOr<void> Function();

class SyncingAgendaRepository extends ChangeNotifier
    implements AgendaRepository, AgendaSyncRepository {
  SyncingAgendaRepository({
    required AgendaRepository local,
    required AgendaAccountStateClient remote,
    required SharedPreferences preferences,
    required this.syncMetadataKey,
    required this.deviceId,
    this.onUnauthorized,
    this.foregroundRefreshInterval = const Duration(seconds: 5),
    DateTime Function()? now,
  }) : _local = local,
       _remote = remote,
       _preferences = preferences,
       _now = now ?? DateTime.now;

  static const int currentSchemaVersion = 1;

  final AgendaRepository _local;
  final AgendaAccountStateClient _remote;
  final SharedPreferences _preferences;
  final String syncMetadataKey;
  final String deviceId;
  final AgendaUnauthorizedCallback? onUnauthorized;
  final Duration foregroundRefreshInterval;
  final DateTime Function() _now;

  int _revision = 0;
  int _schemaVersion = currentSchemaVersion;
  bool _bootstrapping = false;
  bool _syncing = false;
  bool _refreshing = false;
  Map<String, dynamic>? _pendingPayload;
  int? _pendingBaseRevision;
  int _pendingGeneration = 0;
  AgendaRevisionConflict? _conflict;
  String? _lastSyncError;
  AgendaTrialStatus _trial = const AgendaTrialStatus();
  bool _hasTrialStatus = false;
  int _localMutationGeneration = 0;
  DateTime? _lastRemoteCheckAt;
  Completer<void>? _refreshDoneCompleter;
  Completer<void>? _remoteApplyCompleter;
  int? _remoteApplyBaseRevision;

  int get revision => _revision;
  int get schemaVersion => _schemaVersion;
  AgendaTrialStatus get trial => _trial;
  String? get lastSyncError => _lastSyncError;
  String get conflictBackupKey => '$syncMetadataKey.conflict-backup';
  String get fixtureQuarantineBackupKey =>
      '$syncMetadataKey.fixture-quarantine-backup';

  @override
  bool get hasTrialStatus => _hasTrialStatus;

  @override
  bool get trialActive => _trial.active;

  @override
  int get trialDaysRemaining => _trial.daysRemaining;

  @override
  bool get hasConflict => _conflict != null;

  @override
  bool get isSyncing => _syncing || _refreshing;

  @override
  String? get syncMessage {
    if (_conflict != null) {
      return 'Existem alterações mais recentes salvas em outro dispositivo. '
          'Seus dados locais foram preservados.';
    }
    if (_lastSyncError != null) {
      return 'As alterações estão salvas neste navegador e serão '
          'sincronizadas quando a conexão voltar.';
    }
    if (_syncing) return 'Sincronizando alterações…';
    return null;
  }

  @override
  Future<bool> hasData() => _local.hasData();

  @override
  Future<AgendaData?> load() async {
    final data = await _local.load();
    if (!_isKnownFixtureData(data)) return data;

    await _backupKnownFixture(source: 'local-load', payload: data!.toJson());
    final empty = _emptyAgenda();
    await _local.clear();
    await _local.save(empty);
    return _clone(empty);
  }

  @override
  Future<AgendaData> loadOrCreate() async {
    _bootstrapping = true;
    _restoreSyncMetadata();
    if (_pendingPayload case final pending?
        when _isKnownFixtureData(_agendaFromPayload(pending))) {
      await _backupKnownFixture(
        source: 'pending-bootstrap',
        payload: pending,
        remoteRevision: _revision,
        pendingBaseRevision: _pendingBaseRevision,
      );
      _pendingPayload = null;
      _pendingBaseRevision = null;
      await _persistSyncMetadata();
    }
    var localData = await _local.load();
    if (_isKnownFixtureData(localData)) {
      await _backupKnownFixture(
        source: 'local-bootstrap',
        payload: localData!.toJson(),
        remoteRevision: _revision,
      );
      await _local.clear();
      localData = null;
    }
    var retryAfterBootstrap = false;
    try {
      _lastRemoteCheckAt = _now();
      final remoteState = await _remote.fetchState();
      _validateSchema(remoteState);
      _trial = remoteState.trial;
      _hasTrialStatus = true;

      final pending = _pendingPayload;
      if (pending != null) {
        final pendingBase = _pendingBaseRevision ?? _revision;
        if (remoteState.revision != pendingBase) {
          _conflict = AgendaRevisionConflict(remoteState);
          _lastSyncError = null;
        } else {
          _revision = remoteState.revision;
          _schemaVersion = remoteState.schemaVersion;
          retryAfterBootstrap = true;
        }
        await _persistSyncMetadata();
        if (localData != null) return _clone(localData);
        final recovered = AgendaData.fromJson(pending);
        await _local.save(recovered);
        return _clone(recovered);
      }

      _applyRemoteMetadata(remoteState);

      if (remoteState.exists) {
        final payload = remoteState.payload;
        if (payload == null) {
          throw StateError('O estado remoto da agenda está vazio.');
        }
        final remoteData = AgendaData.fromJson(payload);
        if (_isKnownFixtureData(remoteData)) {
          // Early Web builds could persist a bundled fixture into the first
          // authenticated account. Preserve it for recovery, but never apply
          // it to the authenticated user's local cache or write it remotely.
          await _backupKnownFixture(
            source: 'remote-bootstrap',
            payload: payload,
            remoteRevision: remoteState.revision,
            remoteSchemaVersion: remoteState.schemaVersion,
          );
          final empty = _emptyAgenda();
          await _local.clear();
          await _local.save(empty);
          _lastSyncError = null;
          await _persistSyncMetadata();
          return _clone(empty);
        }
        _restoreLocalOnlySettings(remoteData, localData);
        await _local.save(remoteData);
        _lastSyncError = null;
        await _persistSyncMetadata();
        return _clone(remoteData);
      }

      if (localData != null) {
        // A cache is already isolated by user id. Do not upload it during
        // bootstrap; the first explicit user change will schedule a sync.
        await _persistSyncMetadata();
        return _clone(localData);
      }

      final empty = _emptyAgenda();
      await _local.save(empty);
      await _persistSyncMetadata();
      return _clone(empty);
    } on _FixtureQuarantineBackupException {
      rethrow;
    } on AgendaApiException catch (error) {
      if (error.isUnauthorized) {
        await onUnauthorized?.call();
        rethrow;
      }
      _lastSyncError = error.message;
      return _offlineData(localData);
    } on Object catch (error) {
      _lastSyncError = error.toString();
      return _offlineData(localData);
    } finally {
      _bootstrapping = false;
      notifyListeners();
      if (retryAfterBootstrap && !hasConflict) {
        unawaited(_drainPending());
      }
    }
  }

  Future<AgendaData> _offlineData(AgendaData? localData) async {
    if (localData != null) {
      if (!_isKnownFixtureData(localData)) return _clone(localData);
      await _backupKnownFixture(
        source: 'local-offline-fallback',
        payload: localData.toJson(),
        remoteRevision: _revision,
      );
      await _local.clear();
    }
    final empty = _emptyAgenda();
    await _local.save(empty);
    return _clone(empty);
  }

  @override
  Future<void> save(AgendaData data) async {
    final snapshot = _clone(data);
    _localMutationGeneration++;
    _pendingBaseRevision ??= _remoteApplyBaseRevision ?? _revision;
    _pendingPayload = _payloadForRemote(snapshot);
    _pendingGeneration++;

    // A foreground pull may already be committing its validated snapshot.
    // Let that short local write finish, then make the user's edit the final
    // cache value. Its base remains the pre-pull revision, preserving CAS.
    final remoteApply = _remoteApplyCompleter;
    if (remoteApply != null) await remoteApply.future;
    await _local.save(snapshot);
    await _persistSyncMetadata();
    if (_bootstrapping || hasConflict) return;
    if (_refreshing) {
      final refreshDone = _refreshDoneCompleter?.future;
      if (refreshDone != null) {
        unawaited(refreshDone.then((_) => _drainPending()));
      }
      return;
    }
    unawaited(_drainPending());
  }

  @override
  Future<void> clear() async {
    _pendingPayload = null;
    _pendingBaseRevision = null;
    _conflict = null;
    _lastSyncError = null;
    _hasTrialStatus = false;
    await _local.clear();
    await _preferences.remove(syncMetadataKey);
    notifyListeners();
  }

  @override
  Future<void> retrySync() async {
    if (hasConflict) return;
    await _drainPending();
  }

  Future<void> _drainPending() async {
    if (_syncing || _refreshing || _pendingPayload == null || hasConflict) {
      return;
    }
    _syncing = true;
    _lastSyncError = null;
    notifyListeners();
    try {
      while (_pendingPayload != null && !hasConflict) {
        final payload = _pendingPayload!;
        final baseRevision = _pendingBaseRevision ?? _revision;
        final generation = _pendingGeneration;
        try {
          final saved = await _remote.saveState(
            baseRevision: baseRevision,
            schemaVersion: currentSchemaVersion,
            payload: payload,
            deviceId: deviceId,
          );
          _applyRemoteMetadata(saved);
          _lastSyncError = null;
          if (generation == _pendingGeneration) {
            _pendingPayload = null;
            _pendingBaseRevision = null;
          } else {
            _pendingBaseRevision = saved.revision;
          }
          await _persistSyncMetadata();
        } on AgendaRevisionConflict catch (conflict) {
          _conflict = conflict;
          _lastSyncError = null;
          await _persistSyncMetadata();
        } on AgendaApiException catch (error) {
          _lastSyncError = error.message;
          await _persistSyncMetadata();
          if (error.isUnauthorized) await onUnauthorized?.call();
          break;
        } on Object catch (error) {
          _lastSyncError = error.toString();
          await _persistSyncMetadata();
          break;
        }
      }
    } finally {
      _syncing = false;
      notifyListeners();
    }
  }

  @override
  Future<AgendaData?> refreshRemoteIfSafe() async {
    if (!_canStartForegroundRefresh()) return null;

    final checkedAt = _now();
    final previousCheck = _lastRemoteCheckAt;
    if (previousCheck != null &&
        checkedAt.difference(previousCheck) < foregroundRefreshInterval) {
      return null;
    }

    final startingMutationGeneration = _localMutationGeneration;
    final startingRevision = _revision;
    final startingSchemaVersion = _schemaVersion;
    final startingTrial = _trial;
    final startingHasTrialStatus = _hasTrialStatus;
    final refreshDone = Completer<void>();
    _refreshDoneCompleter = refreshDone;
    _refreshing = true;
    _lastRemoteCheckAt = checkedAt;
    notifyListeners();

    try {
      final remoteState = await _remote.fetchState();
      _validateSchema(remoteState);
      if (!_canApplyForegroundRefresh(startingMutationGeneration)) return null;

      // Never regress or rewrite the same revision. A foreground pull only
      // applies a strictly newer server snapshot.
      if (!remoteState.exists || remoteState.revision <= startingRevision) {
        _trial = remoteState.trial;
        _hasTrialStatus = true;
        _lastSyncError = null;
        return null;
      }
      final payload = remoteState.payload;
      if (payload == null) {
        throw StateError('O estado remoto da agenda está vazio.');
      }

      final currentLocal = await _local.load();
      if (!_canApplyForegroundRefresh(startingMutationGeneration)) return null;
      final remoteData = AgendaData.fromJson(payload);
      if (_isKnownFixtureData(remoteData)) {
        await _backupKnownFixture(
          source: 'remote-foreground-refresh',
          payload: payload,
          remoteRevision: remoteState.revision,
          remoteSchemaVersion: remoteState.schemaVersion,
        );
        if (!_canApplyForegroundRefresh(startingMutationGeneration)) {
          return null;
        }
        // Advance only the local CAS base. The fixture remains untouched in
        // the cloud and the user's current local view remains intact.
        _applyRemoteMetadata(remoteState);
        _lastSyncError = null;
        await _persistSyncMetadata();
        return null;
      }
      _restoreLocalOnlySettings(remoteData, currentLocal);

      // Once the final safety check passes, briefly serialize the local cache
      // write. A user save that starts here waits and wins afterwards.
      final remoteApply = Completer<void>();
      _remoteApplyCompleter = remoteApply;
      _remoteApplyBaseRevision = startingRevision;
      try {
        if (!_canApplyForegroundRefresh(startingMutationGeneration)) {
          return null;
        }
        await _local.save(remoteData);
        if (!_canApplyForegroundRefresh(startingMutationGeneration)) {
          return null;
        }

        _applyRemoteMetadata(remoteState);
        _lastSyncError = null;
        await _persistSyncMetadata();

        // A save can begin while metadata is being persisted. Roll the sync
        // base back so its CAS cannot silently replace this remote change.
        if (!_canApplyForegroundRefresh(startingMutationGeneration)) {
          _revision = startingRevision;
          _schemaVersion = startingSchemaVersion;
          _trial = startingTrial;
          _hasTrialStatus = startingHasTrialStatus;
          await _persistSyncMetadata();
          return null;
        }
        return _clone(remoteData);
      } finally {
        _remoteApplyBaseRevision = null;
        _remoteApplyCompleter = null;
        if (!remoteApply.isCompleted) remoteApply.complete();
      }
    } on _FixtureQuarantineBackupException {
      rethrow;
    } on AgendaApiException catch (error) {
      _lastSyncError = error.message;
      if (error.isUnauthorized) await onUnauthorized?.call();
      return null;
    } on Object catch (error) {
      _lastSyncError = error.toString();
      return null;
    } finally {
      _refreshing = false;
      _refreshDoneCompleter = null;
      if (!refreshDone.isCompleted) refreshDone.complete();
      notifyListeners();
    }
  }

  bool _canStartForegroundRefresh() =>
      !_bootstrapping &&
      !_syncing &&
      !_refreshing &&
      _pendingPayload == null &&
      !hasConflict;

  bool _canApplyForegroundRefresh(int startingMutationGeneration) =>
      !_syncing &&
      _pendingPayload == null &&
      !hasConflict &&
      _localMutationGeneration == startingMutationGeneration;

  @override
  Future<AgendaData?> resolveConflictUsingLocal() async {
    final conflict = _conflict;
    if (conflict == null || _syncing || _refreshing) return null;
    final remoteState = conflict.remote;
    _validateSchema(remoteState);
    final currentLocal = await _local.load();
    final payload =
        _pendingPayload ??
        (currentLocal == null ? null : _payloadForRemote(currentLocal));
    if (payload == null) return null;

    final generation = _pendingGeneration;
    _syncing = true;
    _lastSyncError = null;
    notifyListeners();
    try {
      final saved = await _remote.saveState(
        baseRevision: remoteState.revision,
        schemaVersion: currentSchemaVersion,
        payload: payload,
        deviceId: deviceId,
      );
      _applyRemoteMetadata(saved);
      _conflict = null;
      if (generation == _pendingGeneration) {
        _pendingPayload = null;
        _pendingBaseRevision = null;
      } else {
        _pendingBaseRevision = saved.revision;
      }
      _lastSyncError = null;
      await _persistSyncMetadata();
      return currentLocal == null ? null : _clone(currentLocal);
    } on AgendaRevisionConflict catch (nextConflict) {
      _conflict = nextConflict;
      await _persistSyncMetadata();
      rethrow;
    } on AgendaApiException catch (error) {
      _lastSyncError = error.message;
      await _persistSyncMetadata();
      if (error.isUnauthorized) await onUnauthorized?.call();
      rethrow;
    } on Object catch (error) {
      _lastSyncError = error.toString();
      await _persistSyncMetadata();
      rethrow;
    } finally {
      _syncing = false;
      notifyListeners();
      if (_pendingPayload != null && !hasConflict) {
        unawaited(_drainPending());
      }
    }
  }

  @override
  Future<AgendaData?> resolveConflictUsingCloud() async {
    final conflict = _conflict;
    if (conflict == null || _syncing || _refreshing) return null;
    final remoteState = conflict.remote;
    _validateSchema(remoteState);
    if (!remoteState.exists || remoteState.payload == null) return null;

    final currentLocal = await _local.load();
    final startingMutationGeneration = _localMutationGeneration;
    final backupSaved = await _preferences.setString(
      conflictBackupKey,
      jsonEncode(<String, Object?>{
        'savedAt': _now().toUtc().toIso8601String(),
        'remoteRevision': remoteState.revision,
        'pendingBaseRevision': _pendingBaseRevision,
        'pendingPayload': _pendingPayload,
        'localPayload': currentLocal?.toJson(),
      }),
    );
    if (!backupSaved) {
      throw StateError(
        'Nao foi possivel criar a copia de seguranca dos dados locais.',
      );
    }
    if (_localMutationGeneration != startingMutationGeneration) {
      throw StateError(
        'Os dados locais mudaram durante a resolucao. Tente novamente.',
      );
    }

    final data = AgendaData.fromJson(remoteState.payload!);
    if (_isKnownFixtureData(data)) {
      await _backupKnownFixture(
        source: 'remote-conflict-cloud',
        payload: remoteState.payload!,
        remoteRevision: remoteState.revision,
        remoteSchemaVersion: remoteState.schemaVersion,
        pendingBaseRevision: _pendingBaseRevision,
      );
      if (_localMutationGeneration != startingMutationGeneration) {
        throw StateError(
          'Os dados locais mudaram durante a resolucao. Tente novamente.',
        );
      }
      final empty = _emptyAgenda();
      await _local.save(empty);
      _applyRemoteMetadata(remoteState);
      _conflict = null;
      _pendingPayload = null;
      _pendingBaseRevision = null;
      _pendingGeneration++;
      _lastSyncError = null;
      await _persistSyncMetadata();
      notifyListeners();
      return _clone(empty);
    }
    _restoreLocalOnlySettings(data, currentLocal);
    await _local.save(data);
    _applyRemoteMetadata(remoteState);
    _conflict = null;
    _pendingPayload = null;
    _pendingBaseRevision = null;
    _pendingGeneration++;
    _lastSyncError = null;
    await _persistSyncMetadata();
    notifyListeners();
    return _clone(data);
  }

  void _applyRemoteMetadata(AgendaRemoteState state) {
    _validateSchema(state);
    _revision = state.revision;
    _schemaVersion = state.schemaVersion;
    _trial = state.trial;
    _hasTrialStatus = true;
  }

  void _restoreSyncMetadata() {
    final source = _preferences.getString(syncMetadataKey);
    if (source == null || source.trim().isEmpty) return;
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map) return;
      final json = Map<String, dynamic>.from(decoded);
      _revision = _intValue(json['revision']);
      _schemaVersion = _intValue(
        json['schemaVersion'],
        fallback: currentSchemaVersion,
      );
      final rawPending = json['pendingPayload'];
      _pendingPayload = rawPending is Map
          ? Map<String, dynamic>.from(rawPending)
          : null;
      _pendingBaseRevision = _pendingPayload == null
          ? null
          : _intValue(json['pendingBaseRevision'], fallback: _revision);
      if (_pendingPayload != null) _pendingGeneration++;
    } on Object {
      _revision = 0;
      _schemaVersion = currentSchemaVersion;
      _pendingPayload = null;
      _pendingBaseRevision = null;
    }
  }

  Future<void> _persistSyncMetadata() async {
    final saved = await _preferences.setString(
      syncMetadataKey,
      jsonEncode(<String, Object?>{
        'revision': _revision,
        'schemaVersion': _schemaVersion,
        'pendingBaseRevision': _pendingBaseRevision,
        'pendingPayload': _pendingPayload,
      }),
    );
    if (!saved) {
      throw StateError('Não foi possível guardar a fila de sincronização.');
    }
  }

  Future<void> _backupKnownFixture({
    required String source,
    required Map<String, dynamic> payload,
    int? remoteRevision,
    int? remoteSchemaVersion,
    int? pendingBaseRevision,
  }) async {
    final payloadCopy = Map<String, dynamic>.from(
      jsonDecode(jsonEncode(payload)) as Map,
    );
    final payloadFingerprint = jsonEncode(payloadCopy);
    final backups = <Map<String, dynamic>>[];
    final existing = _preferences.getString(fixtureQuarantineBackupKey);
    if (existing != null && existing.trim().isNotEmpty) {
      try {
        final decoded = jsonDecode(existing);
        if (decoded is Map) {
          final rawBackups = decoded['backups'];
          if (rawBackups is List) {
            for (final entry in rawBackups) {
              if (entry is Map) {
                backups.add(Map<String, dynamic>.from(entry));
              }
            }
          }
        }
      } on Object {
        // A malformed old quarantine record must not prevent creating a new,
        // recoverable backup for the fixture being handled now.
      }
    }

    final alreadyBackedUp = backups.any((entry) {
      if (entry['source'] != source ||
          entry['remoteRevision'] != remoteRevision ||
          entry['pendingBaseRevision'] != pendingBaseRevision) {
        return false;
      }
      try {
        return jsonEncode(entry['payload']) == payloadFingerprint;
      } on Object {
        return false;
      }
    });
    if (alreadyBackedUp) return;
    backups.add(<String, dynamic>{
      'savedAt': _now().toUtc().toIso8601String(),
      'source': source,
      'remoteRevision': remoteRevision,
      'remoteSchemaVersion': remoteSchemaVersion,
      'pendingBaseRevision': pendingBaseRevision,
      'payload': payloadCopy,
    });

    final saved = await _preferences.setString(
      fixtureQuarantineBackupKey,
      jsonEncode(<String, Object?>{'version': 1, 'backups': backups}),
    );
    if (!saved) {
      throw _FixtureQuarantineBackupException(
        'Nao foi possivel criar a copia de seguranca da conta de teste.',
      );
    }
  }

  static void _validateSchema(AgendaRemoteState state) {
    if (state.schemaVersion > currentSchemaVersion) {
      throw StateError(
        'Esta agenda foi salva por uma versão mais recente do aplicativo.',
      );
    }
  }

  static AgendaData _emptyAgenda() =>
      AgendaData(settings: AgendaSettings(onboardingCompleted: false));

  static AgendaData? _agendaFromPayload(Map<String, dynamic> payload) {
    try {
      return AgendaData.fromJson(payload);
    } on Object {
      return null;
    }
  }

  static bool _isKnownFixtureData(AgendaData? data) {
    if (data == null) return false;
    const serviceIds = <String>{
      'service-manicure',
      'service-pedicure',
      'service-alongamento',
      'service-sobrancelha',
    };
    const professionalIds = <String>{
      'professional-manicure-1',
      'professional-designer-1',
    };
    final settings = data.settings;
    final bundledFixture =
        _fixtureCardinalityMatches(
          data,
          services: 4,
          professionals: 2,
          manualPayments: 1,
        ) &&
        _stableTextFingerprint(<String>[
              settings.accountFullName,
              settings.businessName,
            ]) ==
            1397679179 &&
        settings.accountPhone.replaceAll(RegExp(r'\D'), '') == '33998007983' &&
        settings.businessPhone.replaceAll(RegExp(r'\D'), '') == '33998007983' &&
        settings.themeId == 'aesthetic-coral' &&
        data.services.map((item) => item.id).toSet().containsAll(serviceIds) &&
        data.professionals
            .map((item) => item.id)
            .toSet()
            .containsAll(professionalIds) &&
        data.manualPayments.single.id == 'payment-opening-history';
    final contaminatedTestAccount =
        _fixtureCardinalityMatches(
          data,
          services: 1,
          professionals: 1,
          customers: 4,
          appointments: 4,
          manualPayments: 3,
        ) &&
        settings.accountEmail.trim().toLowerCase() ==
            'teste@agendalivre.local' &&
        settings.accountFullName.trim() == 'Nina' &&
        _normalizedFixtureText(settings.businessName) ==
            'meu salao de beleza' &&
        _normalizedFixtureText(settings.businessSegment) == 'oficina' &&
        settings.onboardingCompleted &&
        data.services.single.id == '656fb5c2a9ec80005' &&
        data.services.single.name == 'Revisao Completa Carro 1900-2010' &&
        data.services.single.durationMinutes == 30 &&
        data.services.single.price == 450 &&
        data.professionals.single.id == '656fb5880e8180004' &&
        data.professionals.single.name == 'Rafael' &&
        data.professionals.single.role == 'Mecanico';
    return bundledFixture || contaminatedTestAccount;
  }

  static bool _fixtureCardinalityMatches(
    AgendaData data, {
    required int services,
    required int professionals,
    int customers = 0,
    int appointments = 0,
    int products = 0,
    int productSales = 0,
    int manualPayments = 0,
    int customerReceivables = 0,
    int expenses = 0,
    int whatsAppMessages = 0,
    int whatsAppLeads = 0,
  }) =>
      data.services.length == services &&
      data.professionals.length == professionals &&
      data.customers.length == customers &&
      data.appointments.length == appointments &&
      data.products.length == products &&
      data.productSales.length == productSales &&
      data.manualPayments.length == manualPayments &&
      data.customerReceivables.length == customerReceivables &&
      data.expenses.length == expenses &&
      data.whatsAppMessages.length == whatsAppMessages &&
      data.whatsAppLeads.length == whatsAppLeads;

  static int _stableTextFingerprint(Iterable<String> values) {
    var hash = 5381;
    for (final value in values) {
      for (final codeUnit in value.trim().codeUnits) {
        hash = ((hash * 33) ^ codeUnit) & 0x7fffffff;
      }
      hash = ((hash * 33) ^ 31) & 0x7fffffff;
    }
    return hash;
  }

  static String _normalizedFixtureText(String value) => value
      .trim()
      .toLowerCase()
      .replaceAll(RegExp('[áàâãä]'), 'a')
      .replaceAll(RegExp('[éèêë]'), 'e')
      .replaceAll(RegExp('[íìîï]'), 'i')
      .replaceAll(RegExp('[óòôõö]'), 'o')
      .replaceAll(RegExp('[úùûü]'), 'u')
      .replaceAll('ç', 'c');

  static AgendaData _clone(AgendaData data) =>
      AgendaData.fromJson(data.toJson());

  static Map<String, dynamic> _payloadForRemote(AgendaData data) {
    final payload = Map<String, dynamic>.from(data.toJson());
    final rawSettings = payload['Settings'];
    if (rawSettings is Map) {
      final settings = Map<String, dynamic>.from(rawSettings)
        ..remove('AccountPasswordHash')
        ..remove('BusinessLogoPath')
        ..remove('PublicBookingApiUrl')
        ..remove('PublicBookingLastSyncAt')
        ..remove('WhatsAppLinked')
        ..remove('WhatsAppStorePhone')
        ..remove('WhatsAppConnectedName')
        ..remove('WhatsAppLinkedAt')
        ..remove('WhatsAppLastMessageAt')
        ..remove('WhatsAppEvolutionBaseUrl')
        ..remove('WhatsAppEvolutionApiKey')
        ..remove('WhatsAppEvolutionInstanceName')
        ..remove('WhatsAppEvolutionState')
        ..remove('WhatsAppEvolutionQrBase64')
        ..remove('WhatsAppEvolutionLastCheckedAt')
        ..remove('InstagramLinked')
        ..remove('InstagramApiUrl')
        ..remove('InstagramAccountId')
        ..remove('InstagramState')
        ..remove('InstagramLastError')
        ..remove('InstagramLinkedAt')
        ..remove('InstagramLastCheckedAt')
        ..remove('MercadoPagoConnected')
        ..remove('MercadoPagoLicenseKey')
        ..remove('MercadoPagoPaymentsApiUrl')
        ..remove('MercadoPagoSellerUserId')
        ..remove('MercadoPagoDefaultTerminalId')
        ..remove('MercadoPagoDefaultTerminalLabel')
        ..remove('MercadoPagoLastError')
        ..remove('MercadoPagoLastSyncAt');
      payload['Settings'] = settings;
    }
    return payload;
  }

  static void _restoreLocalOnlySettings(AgendaData remote, AgendaData? local) {
    if (local == null) return;
    remote.settings
      ..accountPasswordHash = local.settings.accountPasswordHash
      ..businessLogoPath = local.settings.businessLogoPath
      ..publicBookingApiUrl = local.settings.publicBookingApiUrl
      ..publicBookingLastSyncAt = local.settings.publicBookingLastSyncAt
      ..whatsAppLinked = local.settings.whatsAppLinked
      ..whatsAppStorePhone = local.settings.whatsAppStorePhone
      ..whatsAppConnectedName = local.settings.whatsAppConnectedName
      ..whatsAppLinkedAt = local.settings.whatsAppLinkedAt
      ..whatsAppLastMessageAt = local.settings.whatsAppLastMessageAt
      ..whatsAppEvolutionBaseUrl = local.settings.whatsAppEvolutionBaseUrl
      ..whatsAppEvolutionApiKey = local.settings.whatsAppEvolutionApiKey
      ..whatsAppEvolutionInstanceName =
          local.settings.whatsAppEvolutionInstanceName
      ..whatsAppEvolutionState = local.settings.whatsAppEvolutionState
      ..whatsAppEvolutionQrBase64 = local.settings.whatsAppEvolutionQrBase64
      ..whatsAppEvolutionLastCheckedAt =
          local.settings.whatsAppEvolutionLastCheckedAt
      ..instagramLinked = local.settings.instagramLinked
      ..instagramApiUrl = local.settings.instagramApiUrl
      ..instagramAccountId = local.settings.instagramAccountId
      ..instagramState = local.settings.instagramState
      ..instagramLastError = local.settings.instagramLastError
      ..instagramLinkedAt = local.settings.instagramLinkedAt
      ..instagramLastCheckedAt = local.settings.instagramLastCheckedAt
      ..mercadoPagoConnected = local.settings.mercadoPagoConnected
      ..mercadoPagoLicenseKey = local.settings.mercadoPagoLicenseKey
      ..mercadoPagoPaymentsApiUrl = local.settings.mercadoPagoPaymentsApiUrl
      ..mercadoPagoSellerUserId = local.settings.mercadoPagoSellerUserId
      ..mercadoPagoDefaultTerminalId =
          local.settings.mercadoPagoDefaultTerminalId
      ..mercadoPagoDefaultTerminalLabel =
          local.settings.mercadoPagoDefaultTerminalLabel
      ..mercadoPagoLastError = local.settings.mercadoPagoLastError
      ..mercadoPagoLastSyncAt = local.settings.mercadoPagoLastSyncAt;
  }
}

class _FixtureQuarantineBackupException implements Exception {
  const _FixtureQuarantineBackupException(this.message);

  final String message;

  @override
  String toString() => message;
}

int _intValue(Object? value, {int fallback = 0}) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse(value?.toString() ?? '') ?? fallback;
}
