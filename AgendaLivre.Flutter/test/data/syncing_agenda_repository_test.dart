import 'dart:async';
import 'dart:convert';

import 'package:agenda_livre/data/repositories/shared_preferences_agenda_repository.dart';
import 'package:agenda_livre/data/repositories/syncing_agenda_repository.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test(
    'conta sem estado remoto abre vazia e não envia seed no bootstrap',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final remote = _FakeStateClient(_remote(exists: false, revision: 0));
      final repository = _repository(preferences, remote);

      final data = await repository.loadOrCreate();

      expect(data.settings.onboardingCompleted, isFalse);
      expect(data.settings.businessSegment, isEmpty);
      expect(data.services, isEmpty);
      expect(data.professionals, isEmpty);
      expect(remote.saveCalls, 0);
    },
  );

  test(
    'estado remoto é baixado antes de abrir e fica no cache da conta',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final remoteData = _data('Agenda da nuvem');
      final remote = _FakeStateClient(
        _remote(revision: 7, payload: remoteData.toJson()),
      );
      final repository = _repository(preferences, remote);

      final loaded = await repository.loadOrCreate();

      expect(loaded.settings.businessName, 'Agenda da nuvem');
      expect(repository.revision, 7);
      expect(
        (await _local(preferences).load())?.settings.businessName,
        'Agenda da nuvem',
      );
    },
  );

  test(
    'fixture legada da conta de teste nunca abre dentro do usuario autenticado',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final legacyDemo = AgendaSeedData.salon(
        referenceDate: DateTime(2026, 7, 14),
      );
      final remote = _FakeStateClient(
        _remote(revision: 3, payload: legacyDemo.toJson()),
      );
      final repository = _repository(preferences, remote);

      final loaded = await repository.loadOrCreate();

      expect(loaded.settings.onboardingCompleted, isFalse);
      expect(loaded.settings.businessName, isNot('Lucas Barbearia'));
      expect(loaded.settings.accountFullName, isEmpty);
      expect(loaded.services, isEmpty);
      expect(loaded.professionals, isEmpty);
      expect(repository.revision, 3);
      expect(remote.saveCalls, 0);
      final quarantine = _fixtureQuarantine(preferences, repository);
      expect(quarantine, hasLength(1));
      expect(quarantine.single['source'], 'remote-bootstrap');
      expect(quarantine.single['remoteRevision'], 3);
      expect((quarantine.single['payload'] as Map)['Services'], hasLength(4));

      loaded.settings
        ..businessName = 'Studio Nina'
        ..accountFullName = 'Nina Souza'
        ..onboardingCompleted = true;
      await repository.save(loaded);
      await _settleAsync();

      expect(remote.saveCalls, 1);
      expect(remote.lastBaseRevision, 3);
      expect(
        remote.lastPayload?['Settings'],
        containsPair('BusinessName', 'Studio Nina'),
      );
    },
  );

  test('fixture legada no cache isolado tambem abre uma conta vazia', () async {
    final preferences = await SharedPreferences.getInstance();
    await _local(
      preferences,
    ).save(AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14)));
    final remote = _FakeStateClient(_remote(exists: false, revision: 0));
    final repository = _repository(preferences, remote);

    final loaded = await repository.loadOrCreate();

    expect(loaded.settings.onboardingCompleted, isFalse);
    expect(loaded.settings.businessName, isNot('Lucas Barbearia'));
    expect(loaded.services, isEmpty);
    expect(remote.saveCalls, 0);
    expect(
      (await _local(preferences).load())?.settings.businessName,
      isNot('Lucas Barbearia'),
    );
    final quarantine = _fixtureQuarantine(preferences, repository);
    expect(quarantine, hasLength(1));
    expect(quarantine.single['source'], 'local-bootstrap');
  });

  test('payload exato da conta de teste em producao abre onboarding', () async {
    final preferences = await SharedPreferences.getInstance();
    final remote = _FakeStateClient(
      _remote(revision: 12, payload: _productionTestFixture().toJson()),
    );
    final repository = _repository(preferences, remote);

    final loaded = await repository.loadOrCreate();

    expect(loaded.settings.onboardingCompleted, isFalse);
    expect(loaded.settings.accountEmail, isEmpty);
    expect(loaded.services, isEmpty);
    expect(loaded.professionals, isEmpty);
    expect(repository.revision, 12);
    expect(remote.saveCalls, 0);
    final quarantine = _fixtureQuarantine(preferences, repository);
    expect(quarantine, hasLength(1));
    expect(quarantine.single['source'], 'remote-bootstrap');
    expect(quarantine.single['remoteRevision'], 12);
    final backupSettings = Map<String, dynamic>.from(
      (quarantine.single['payload'] as Map)['Settings'] as Map,
    );
    expect(backupSettings['AccountEmail'], 'teste@agendalivre.local');
  });

  test(
    'fixture conhecida que ganhou dado real nao e colocada em quarentena',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final evolved = _productionTestFixture()
        ..services.add(
          ServiceItem(
            id: 'real-service-after-onboarding',
            name: 'Servico criado pelo usuario',
            durationMinutes: 60,
            price: 120,
          ),
        );
      final remote = _FakeStateClient(
        _remote(revision: 13, payload: evolved.toJson()),
      );
      final repository = _repository(preferences, remote);

      final loaded = await repository.loadOrCreate();

      expect(loaded.settings.onboardingCompleted, isTrue);
      expect(loaded.services, hasLength(2));
      expect(
        loaded.services.any(
          (item) => item.id == 'real-service-after-onboarding',
        ),
        isTrue,
      );
      expect(
        preferences.getString(repository.fixtureQuarantineBackupKey),
        isNull,
      );
    },
  );

  test(
    'retomada bloqueia fixture nova, preserva a tela local e cria backup',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final remote = _FakeStateClient(
        _remote(revision: 1, payload: _data('Conta real').toJson()),
      );
      final repository = _repository(preferences, remote);
      await repository.loadOrCreate();
      remote.state = _remote(
        revision: 2,
        payload: _productionTestFixture().toJson(),
      );

      final refreshed = await repository.refreshRemoteIfSafe();

      expect(refreshed, isNull);
      expect((await repository.load())?.settings.businessName, 'Conta real');
      expect(repository.revision, 2);
      expect(remote.saveCalls, 0);
      final quarantine = _fixtureQuarantine(preferences, repository);
      expect(quarantine, hasLength(1));
      expect(quarantine.single['source'], 'remote-foreground-refresh');
      expect(quarantine.single['remoteRevision'], 2);
    },
  );

  test(
    'escolher nuvem em conflito nunca aplica fixture e mantem backups',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final initialRemote = _FakeStateClient(
        _remote(revision: 1, payload: _data('Base real').toJson()),
      );
      final initialRepository = _repository(preferences, initialRemote);
      final local = await initialRepository.loadOrCreate();
      initialRemote.saveError = StateError('offline');
      local.settings.businessName = 'Alteracao real offline';
      await initialRepository.save(local);
      await _settleAsync();

      final remote = _FakeStateClient(
        _remote(revision: 2, payload: _productionTestFixture().toJson()),
      );
      final repository = _repository(preferences, remote);
      final loaded = await repository.loadOrCreate();
      expect(loaded.settings.businessName, 'Alteracao real offline');
      expect(repository.hasConflict, isTrue);

      final resolved = await repository.resolveConflictUsingCloud();

      expect(resolved?.settings.onboardingCompleted, isFalse);
      expect(resolved?.settings.accountEmail, isEmpty);
      expect(resolved?.services, isEmpty);
      expect(repository.hasConflict, isFalse);
      expect(repository.revision, 2);
      expect(remote.saveCalls, 0);
      expect(preferences.getString(repository.conflictBackupKey), isNotNull);
      final quarantine = _fixtureQuarantine(preferences, repository);
      expect(quarantine, hasLength(1));
      expect(quarantine.single['source'], 'remote-conflict-cloud');
      expect(quarantine.single['remoteRevision'], 2);
    },
  );

  test('fila com fixture e copiada antes de ser descartada', () async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(
      _syncKey,
      jsonEncode(<String, Object?>{
        'revision': 5,
        'schemaVersion': 1,
        'pendingBaseRevision': 5,
        'pendingPayload': _productionTestFixture().toJson(),
      }),
    );
    final remote = _FakeStateClient(_remote(exists: false, revision: 5));
    final repository = _repository(preferences, remote);

    final loaded = await repository.loadOrCreate();

    expect(loaded.settings.onboardingCompleted, isFalse);
    expect(remote.saveCalls, 0);
    final metadata = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(_syncKey)!) as Map,
    );
    expect(metadata['pendingPayload'], isNull);
    final quarantine = _fixtureQuarantine(preferences, repository);
    expect(quarantine, hasLength(1));
    expect(quarantine.single['source'], 'pending-bootstrap');
    expect(quarantine.single['pendingBaseRevision'], 5);
  });

  test(
    'fila offline persiste e é reenviada sem perder o cache local',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final initial = _data('Versão inicial');
      final offlineRemote = _FakeStateClient(
        _remote(revision: 5, payload: initial.toJson()),
      );
      final first = _repository(preferences, offlineRemote);
      final local = await first.loadOrCreate();
      offlineRemote.saveError = StateError('offline');
      local.settings.businessName = 'Alteração offline';

      await first.save(local);
      await _settleAsync();

      final metadata = Map<String, dynamic>.from(
        jsonDecode(preferences.getString(_syncKey)!) as Map,
      );
      expect(metadata['pendingBaseRevision'], 5);
      expect(metadata['pendingPayload'], isA<Map>());

      final onlineRemote = _FakeStateClient(
        _remote(revision: 5, payload: initial.toJson()),
      );
      final reopened = _repository(preferences, onlineRemote);
      final reopenedData = await reopened.loadOrCreate();
      expect(reopenedData.settings.businessName, 'Alteração offline');

      await _settleAsync();
      await reopened.retrySync();

      expect(onlineRemote.saveCalls, 1);
      expect(onlineRemote.lastBaseRevision, 5);
      expect(
        onlineRemote.lastPayload?['Settings'],
        containsPair('BusinessName', 'Alteração offline'),
      );
      final flushed = Map<String, dynamic>.from(
        jsonDecode(preferences.getString(_syncKey)!) as Map,
      );
      expect(flushed['pendingPayload'], isNull);
    },
  );

  test('revisão remota divergente gera conflito e preserva o local', () async {
    final preferences = await SharedPreferences.getInstance();
    final base = _data('Base');
    final firstRemote = _FakeStateClient(
      _remote(revision: 3, payload: base.toJson()),
    );
    final first = _repository(preferences, firstRemote);
    final local = await first.loadOrCreate();
    firstRemote.saveError = StateError('offline');
    local.settings.businessName = 'Minha alteração local';
    await first.save(local);
    await _settleAsync();

    final otherDevice = _data('Alteração do Windows');
    final remote = _FakeStateClient(
      _remote(revision: 4, payload: otherDevice.toJson()),
    );
    final reopened = _repository(preferences, remote);

    final loaded = await reopened.loadOrCreate();

    expect(loaded.settings.businessName, 'Minha alteração local');
    expect(reopened.hasConflict, isTrue);
    expect(remote.saveCalls, 0);

    final reloaded = await reopened.resolveConflictUsingCloud();
    expect(reloaded?.settings.businessName, 'Alteração do Windows');
    expect(reopened.hasConflict, isFalse);
    final backup = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(reopened.conflictBackupKey)!) as Map,
    );
    final localBackup = Map<String, dynamic>.from(
      backup['localPayload'] as Map,
    );
    expect(
      (localBackup['Settings'] as Map)['BusinessName'],
      'Minha alteração local',
    );
  });

  test('escolher dados locais faz rebase e salva na revisão remota', () async {
    final preferences = await SharedPreferences.getInstance();
    final firstRemote = _FakeStateClient(
      _remote(revision: 3, payload: _data('Base').toJson()),
    );
    final first = _repository(preferences, firstRemote);
    final local = await first.loadOrCreate();
    firstRemote.saveError = StateError('offline');
    local.settings.businessName = 'Minha versão local';
    await first.save(local);
    await _settleAsync();

    final remote = _FakeStateClient(
      _remote(revision: 4, payload: _data('Versão do Windows').toJson()),
    );
    final reopened = _repository(preferences, remote);
    await reopened.loadOrCreate();

    final resolved = await reopened.resolveConflictUsingLocal();

    expect(resolved?.settings.businessName, 'Minha versão local');
    expect(reopened.hasConflict, isFalse);
    expect(reopened.revision, 5);
    expect(remote.lastBaseRevision, 4);
    expect(
      (remote.lastPayload?['Settings'] as Map)['BusinessName'],
      'Minha versão local',
    );
  });

  test(
    'payload mantém dados operacionais e não envia segredos locais',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final remote = _FakeStateClient(_remote(exists: false, revision: 0));
      final repository = _repository(preferences, remote);
      final data = await repository.loadOrCreate();
      data.settings
        ..businessName = 'Studio'
        ..publicBookingSlug = 'studio'
        ..publicBookingUrl = 'https://agenda.example/studio'
        ..publicBookingApiUrl = 'https://internal.example/booking'
        ..whatsAppEnabled = true
        ..whatsAppAutoConfirmationsEnabled = true
        ..whatsAppEvolutionApiKey = 'secret'
        ..instagramUsername = 'studio'
        ..instagramDisplayName = 'Studio Oficial';
      data.whatsAppMessages.add(
        WhatsAppMessage(id: 'message-1', message: 'Olá'),
      );
      data.whatsAppLeads.add(WhatsAppLead(id: 'lead-1', customerName: 'Ana'));

      await repository.save(data);
      await _settleAsync();

      final payload = remote.lastPayload!;
      final settings = Map<String, dynamic>.from(payload['Settings'] as Map);
      expect(settings['WhatsAppEnabled'], isTrue);
      expect(settings['WhatsAppAutoConfirmationsEnabled'], isTrue);
      expect(settings['InstagramUsername'], 'studio');
      expect(settings['PublicBookingSlug'], 'studio');
      expect(settings['PublicBookingUrl'], 'https://agenda.example/studio');
      expect(settings.containsKey('PublicBookingApiUrl'), isFalse);
      expect(settings.containsKey('WhatsAppEvolutionApiKey'), isFalse);
      expect(payload['WhatsAppMessages'], hasLength(1));
      expect(payload['WhatsAppLeads'], hasLength(1));
    },
  );

  test(
    'retomada aplica revisao nova, preserva campos locais e nao gera push',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final localData = _data('Cache Web')
        ..settings.businessLogoPath = 'browser-logo-local'
        ..settings.publicBookingApiUrl = 'https://local.example/booking'
        ..settings.whatsAppEvolutionApiKey = 'local-secret'
        ..settings.whatsAppEvolutionInstanceName = 'web-instance';
      await _local(preferences).save(localData);

      final remote = _FakeStateClient(
        _remote(revision: 2, payload: _data('Nuvem inicial').toJson()),
      );
      final repository = _repository(preferences, remote);
      await repository.loadOrCreate();
      final newerCloud = _data('Atualizado no Windows')
        ..settings.publicBookingSlug = 'studio-novo'
        ..settings.whatsAppEnabled = true;
      remote.state = _remote(revision: 3, payload: newerCloud.toJson());

      final refreshed = await repository.refreshRemoteIfSafe();

      expect(refreshed?.settings.businessName, 'Atualizado no Windows');
      expect(refreshed?.settings.publicBookingSlug, 'studio-novo');
      expect(refreshed?.settings.whatsAppEnabled, isTrue);
      expect(refreshed?.settings.businessLogoPath, 'browser-logo-local');
      expect(
        refreshed?.settings.publicBookingApiUrl,
        'https://local.example/booking',
      );
      expect(refreshed?.settings.whatsAppEvolutionApiKey, 'local-secret');
      expect(refreshed?.settings.whatsAppEvolutionInstanceName, 'web-instance');
      expect(repository.revision, 3);
      expect(remote.fetchCalls, 2);
      expect(remote.saveCalls, 0);
      final metadata = Map<String, dynamic>.from(
        jsonDecode(preferences.getString(_syncKey)!) as Map,
      );
      expect(metadata['pendingPayload'], isNull);
    },
  );

  test('retomada limita foco repetido e nao reaplica mesma revisao', () async {
    final preferences = await SharedPreferences.getInstance();
    final clock = _MutableClock(DateTime.utc(2026, 7, 18, 10));
    final remote = _FakeStateClient(
      _remote(revision: 4, payload: _data('Revisao atual').toJson()),
    );
    final repository = _repository(
      preferences,
      remote,
      now: clock.call,
      foregroundRefreshInterval: const Duration(seconds: 5),
    );
    await repository.loadOrCreate();
    remote.state = _remote(
      revision: 4,
      payload: _data('Payload inesperado').toJson(),
    );

    expect(await repository.refreshRemoteIfSafe(), isNull);
    expect(remote.fetchCalls, 1);
    clock.advance(const Duration(seconds: 5));
    expect(await repository.refreshRemoteIfSafe(), isNull);
    expect(remote.fetchCalls, 2);
    expect((await repository.load())?.settings.businessName, 'Revisao atual');
    expect(await repository.refreshRemoteIfSafe(), isNull);
    expect(remote.fetchCalls, 2);
  });

  test('retomada nao consulta nuvem com alteracao local pendente', () async {
    final preferences = await SharedPreferences.getInstance();
    final remote = _FakeStateClient(
      _remote(revision: 1, payload: _data('Base').toJson()),
    );
    final repository = _repository(preferences, remote);
    final local = await repository.loadOrCreate();
    remote.saveError = StateError('offline');
    local.settings.businessName = 'Trabalho local pendente';
    await repository.save(local);
    await _settleAsync();
    remote.state = _remote(
      revision: 2,
      payload: _data('Mudou no Windows').toJson(),
    );
    final fetchCalls = remote.fetchCalls;

    expect(await repository.refreshRemoteIfSafe(), isNull);
    expect(remote.fetchCalls, fetchCalls);
    expect(
      (await repository.load())?.settings.businessName,
      'Trabalho local pendente',
    );
  });

  test('edicao iniciada durante consulta impede aplicacao remota', () async {
    final preferences = await SharedPreferences.getInstance();
    final remote = _FakeStateClient(
      _remote(revision: 1, payload: _data('Base Web').toJson()),
    );
    final repository = _repository(preferences, remote);
    await repository.loadOrCreate();

    final fetch = Completer<AgendaRemoteState>();
    remote.nextFetch = fetch;
    final refresh = repository.refreshRemoteIfSafe();
    await _settleAsync();
    final local = (await repository.load())!;
    local.settings.businessName = 'Editado enquanto consultava';
    await repository.save(local);
    remote.saveError = StateError('offline');
    fetch.complete(
      _remote(
        revision: 2,
        payload: _data('Alteracao concorrente do Windows').toJson(),
      ),
    );

    expect(await refresh, isNull);
    await _settleAsync();
    expect(
      (await repository.load())?.settings.businessName,
      'Editado enquanto consultava',
    );
    final metadata = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(_syncKey)!) as Map,
    );
    expect(metadata['pendingPayload'], isA<Map>());
  });

  test('Windows e Flutter compartilham a mesma agenda em duas vias', () async {
    final preferences = await SharedPreferences.getInstance();
    final remote = _FakeStateClient(
      _remote(revision: 1, payload: _data('Agenda compartilhada').toJson()),
    );
    final windowsLocal = SharedPreferencesAgendaRepository(
      preferences,
      storageKey: 'agenda_livre.test.windows',
      seedFactory: AgendaData.new,
    );
    final flutterLocal = SharedPreferencesAgendaRepository(
      preferences,
      storageKey: 'agenda_livre.test.flutter',
      seedFactory: AgendaData.new,
    );
    final windows = SyncingAgendaRepository(
      local: windowsLocal,
      remote: remote,
      preferences: preferences,
      syncMetadataKey: 'agenda_livre.test.windows.sync',
      deviceId: 'windows-device',
      foregroundRefreshInterval: Duration.zero,
    );
    final flutter = SyncingAgendaRepository(
      local: flutterLocal,
      remote: remote,
      preferences: preferences,
      syncMetadataKey: 'agenda_livre.test.flutter.sync',
      deviceId: 'flutter-device',
      foregroundRefreshInterval: Duration.zero,
    );

    final windowsData = await windows.loadOrCreate();
    await flutter.loadOrCreate();
    windowsData.customers.add(
      Customer(id: 'customer-from-windows', name: 'Cliente do Windows'),
    );
    await windows.save(windowsData);
    await _settleAsync();

    final receivedInFlutter = await flutter.refreshRemoteIfSafe();
    expect(receivedInFlutter?.customers.single.name, 'Cliente do Windows');

    final flutterData = (await flutter.load())!;
    flutterData.services.add(
      ServiceItem(
        id: 'service-from-flutter',
        name: 'Serviço do Flutter',
        durationMinutes: 30,
        price: 55,
      ),
    );
    await flutter.save(flutterData);
    await _settleAsync();

    final receivedInWindows = await windows.refreshRemoteIfSafe();
    expect(receivedInWindows?.services.single.name, 'Serviço do Flutter');
    expect(remote.state.revision, 3);
  });
}

const _storageKey = 'agenda_livre.data.v2.test-user';
const _syncKey = '$_storageKey.sync';

SyncingAgendaRepository _repository(
  SharedPreferences preferences,
  _FakeStateClient remote, {
  DateTime Function()? now,
  Duration foregroundRefreshInterval = Duration.zero,
}) => SyncingAgendaRepository(
  local: _local(preferences),
  remote: remote,
  preferences: preferences,
  syncMetadataKey: _syncKey,
  deviceId: 'web-test-device',
  now: now,
  foregroundRefreshInterval: foregroundRefreshInterval,
);

SharedPreferencesAgendaRepository _local(SharedPreferences preferences) =>
    SharedPreferencesAgendaRepository(
      preferences,
      storageKey: _storageKey,
      seedFactory: () =>
          AgendaData(settings: AgendaSettings(onboardingCompleted: false)),
    );

List<Map<String, dynamic>> _fixtureQuarantine(
  SharedPreferences preferences,
  SyncingAgendaRepository repository,
) {
  final decoded = Map<String, dynamic>.from(
    jsonDecode(preferences.getString(repository.fixtureQuarantineBackupKey)!)
        as Map,
  );
  return (decoded['backups'] as List)
      .map((entry) => Map<String, dynamic>.from(entry as Map))
      .toList();
}

AgendaData _data(String businessName) => AgendaData(
  settings: AgendaSettings(
    businessName: businessName,
    businessSegment: 'Beleza',
    onboardingCompleted: true,
  ),
);

AgendaData _productionTestFixture() => AgendaData(
  settings: AgendaSettings(
    accountFullName: 'Nina',
    accountEmail: 'teste@agendalivre.local',
    businessName: 'Meu salao de beleza',
    businessSegment: 'Oficina',
    onboardingCompleted: true,
  ),
  services: <ServiceItem>[
    ServiceItem(
      id: '656fb5c2a9ec80005',
      name: 'Revisao Completa Carro 1900-2010',
      durationMinutes: 30,
      price: 450,
    ),
  ],
  professionals: <Professional>[
    Professional(id: '656fb5880e8180004', name: 'Rafael', role: 'Mecanico'),
  ],
  customers: List<Customer>.generate(
    4,
    (index) => Customer(
      id: 'fixture-customer-$index',
      name: 'Cliente de teste $index',
      lastSeenAt: DateTime.utc(2026, 7, 18, 9 + index),
    ),
  ),
  appointments: List<Appointment>.generate(
    4,
    (index) => Appointment(
      id: 'fixture-appointment-$index',
      customerId: 'fixture-customer-$index',
      customerName: 'Cliente de teste $index',
      serviceId: '656fb5c2a9ec80005',
      serviceName: 'Revisao Completa Carro 1900-2010',
      professionalId: '656fb5880e8180004',
      professionalName: 'Rafael',
      start: DateTime.utc(2026, 7, 18, 9 + index),
      createdAt: DateTime.utc(2026, 7, 18),
      updatedAt: DateTime.utc(2026, 7, 18),
    ),
  ),
  manualPayments: List<ManualPayment>.generate(
    3,
    (index) => ManualPayment(
      id: 'fixture-payment-$index',
      description: 'Pagamento de teste $index',
      value: 100.0 + index,
      paidAt: DateTime.utc(2026, 7, 18, 12 + index),
    ),
  ),
);

AgendaRemoteState _remote({
  bool exists = true,
  required int revision,
  Map<String, dynamic>? payload,
}) => AgendaRemoteState(
  exists: exists,
  revision: revision,
  schemaVersion: 1,
  payload: payload,
  updatedAt: DateTime.utc(2026, 7, 18),
  trial: const AgendaTrialStatus(active: true, daysRemaining: 7),
);

Future<void> _settleAsync() async {
  await Future<void>.delayed(Duration.zero);
  await Future<void>.delayed(Duration.zero);
}

class _FakeStateClient implements AgendaAccountStateClient {
  _FakeStateClient(this.state);

  AgendaRemoteState state;
  Object? saveError;
  Completer<AgendaRemoteState>? nextFetch;
  int fetchCalls = 0;
  int saveCalls = 0;
  int? lastBaseRevision;
  Map<String, dynamic>? lastPayload;

  @override
  Future<AgendaRemoteState> fetchState() async {
    fetchCalls++;
    final pending = nextFetch;
    if (pending == null) return state;
    nextFetch = null;
    return pending.future;
  }

  @override
  Future<AgendaRemoteState> saveState({
    required int baseRevision,
    required int schemaVersion,
    required Map<String, dynamic> payload,
    required String deviceId,
  }) async {
    saveCalls++;
    lastBaseRevision = baseRevision;
    lastPayload = Map<String, dynamic>.from(payload);
    final error = saveError;
    if (error != null) throw error;
    state = AgendaRemoteState(
      exists: true,
      revision: baseRevision + 1,
      schemaVersion: schemaVersion,
      payload: payload,
      updatedAt: DateTime.utc(2026, 7, 18, 12),
      trial: state.trial,
    );
    return state;
  }
}

class _MutableClock {
  _MutableClock(this.value);

  DateTime value;

  DateTime call() => value;

  void advance(Duration duration) {
    value = value.add(duration);
  }
}
