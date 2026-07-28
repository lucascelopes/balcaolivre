import 'package:agenda_livre/data/repositories/shared_preferences_agenda_repository.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const storageKey = 'agenda_livre.test.data';
  final referenceDate = DateTime.parse('2026-07-14T12:00:00-03:00');

  SharedPreferencesAgendaRepository createRepository(
    SharedPreferences preferences,
  ) => SharedPreferencesAgendaRepository(
    preferences,
    storageKey: storageKey,
    seedFactory: () => AgendaSeedData.salon(referenceDate: referenceDate),
  );

  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test('loadOrCreate persists a coherent salon seed', () async {
    final preferences = await SharedPreferences.getInstance();
    final repository = createRepository(preferences);

    final data = await repository.loadOrCreate();

    expect(data.settings.businessName, 'Lucas Barbearia');
    expect(data.settings.themeId, 'aesthetic-coral');
    expect(data.services, hasLength(4));
    expect(data.professionals, hasLength(2));
    expect(data.appointments, isEmpty);
    expect(await repository.hasData(), isTrue);
    expect(preferences.getString(storageKey), isNotNull);
  });

  test('save and load preserve changes and create a backup', () async {
    final preferences = await SharedPreferences.getInstance();
    final repository = createRepository(preferences);
    final data = await repository.loadOrCreate();

    data.settings.businessName = 'Novo Salão';
    await repository.save(data);

    final reloaded = await repository.load();
    expect(reloaded?.settings.businessName, 'Novo Salão');
    expect(preferences.getString(repository.backupKey), isNotNull);
  });

  test(
    'recovers the last valid backup when primary JSON is corrupted',
    () async {
      final preferences = await SharedPreferences.getInstance();
      final repository = createRepository(preferences);
      final original = await repository.loadOrCreate();
      original.settings.businessName = 'Versão atual';
      await repository.save(original);

      await preferences.setString(storageKey, '{invalid-json');

      final recovered = await repository.load();
      expect(recovered, isA<AgendaData>());
      expect(recovered?.settings.businessName, 'Lucas Barbearia');
    },
  );

  test('clear removes primary data and backup', () async {
    final preferences = await SharedPreferences.getInstance();
    final repository = createRepository(preferences);
    final data = await repository.loadOrCreate();
    await repository.save(data);

    await repository.clear();

    expect(await repository.hasData(), isFalse);
    expect(preferences.getString(repository.backupKey), isNull);
  });
}
