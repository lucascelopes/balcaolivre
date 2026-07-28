import 'package:agenda_livre/app/agenda_app.dart';
import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/onboarding/onboarding_page.dart';
import 'package:agenda_livre/features/settings/settings_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('mostra a opção de sair e confirma sem apagar registros', (
    tester,
  ) async {
    final harness = await _openSettings(tester, const Size(1382, 736));
    final before = Map<String, dynamic>.from(harness.controller.data.toJson())
      ..remove('Settings');

    final actionExit = find.byKey(const Key('settings-exit-action'));
    expect(actionExit, findsOneWidget);

    final actionButton = find.byKey(const Key('settings-exit-action-button'));
    await tester.ensureVisible(actionButton);
    await tester.pumpAndSettle();
    expect(actionButton.hitTestable(), findsOneWidget);
    await tester.tap(actionButton);
    await tester.pumpAndSettle();
    final dialog = find.byKey(const Key('settings-exit-dialog'));
    expect(dialog, findsOneWidget);
    expect(tester.getSize(dialog).width, closeTo(500, .1));
    expect(find.text('Sair desta conta?'), findsOneWidget);
    expect(
      find.text('Clientes, serviços, agenda e financeiro continuam salvos.'),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const Key('settings-exit-cancel')));
    await tester.pumpAndSettle();
    expect(dialog, findsNothing);
    expect(harness.controller.needsOnboarding, isFalse);
    expect(harness.repository.saveCalls, 0);

    await tester.ensureVisible(actionButton);
    await tester.pumpAndSettle();
    expect(actionButton.hitTestable(), findsOneWidget);
    await tester.tap(actionButton);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('settings-exit-confirm')));
    await tester.pumpAndSettle();

    final after = Map<String, dynamic>.from(harness.controller.data.toJson())
      ..remove('Settings');
    expect(after, before);
    expect(find.byType(OnboardingPage), findsOneWidget);
    expect(find.byType(SettingsPage), findsNothing);
    expect(harness.controller.needsOnboarding, isTrue);
    expect(harness.repository.saveCalls, 1);
    expect(harness.repository.clearCalls, 0);
    expect(tester.takeException(), isNull);
  });

  for (final size in <Size>[
    const Size(390, 844),
    const Size(320, 568),
    const Size(844, 390),
  ]) {
    testWidgets('opção de sair permanece acessível em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      final harness = await _openSettings(tester, size);
      final actionButton = find.byKey(const Key('settings-exit-action-button'));
      await tester.ensureVisible(actionButton);
      await tester.pumpAndSettle();
      expect(actionButton.hitTestable(), findsOneWidget);
      expect(tester.takeException(), isNull);

      await tester.tap(actionButton);
      await tester.pumpAndSettle();
      final dialog = find.byKey(const Key('settings-exit-dialog'));
      final cancel = find.byKey(const Key('settings-exit-cancel'));
      final confirm = find.byKey(const Key('settings-exit-confirm'));
      expect(dialog, findsOneWidget);
      expect(cancel.hitTestable(), findsOneWidget);
      expect(confirm.hitTestable(), findsOneWidget);
      expect(tester.takeException(), isNull);

      await tester.tap(cancel);
      await tester.pumpAndSettle();
      expect(dialog, findsNothing);
      expect(harness.controller.needsOnboarding, isFalse);
      expect(harness.repository.saveCalls, 0);
      expect(harness.repository.clearCalls, 0);
    });
  }

  testWidgets('mantém a tela e os dados quando não consegue salvar a saída', (
    tester,
  ) async {
    final harness = await _openSettings(tester, const Size(1382, 736));
    final before = harness.controller.data.toJson();
    harness.repository.failSave = true;

    final actionButton = find.byKey(const Key('settings-exit-action-button'));
    await tester.ensureVisible(actionButton);
    await tester.pumpAndSettle();
    await tester.tap(actionButton);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('settings-exit-confirm')));
    await tester.pumpAndSettle();

    expect(find.byType(SettingsPage), findsOneWidget);
    expect(find.byType(OnboardingPage), findsNothing);
    expect(harness.controller.data.toJson(), before);
    expect(
      find.text('Não foi possível sair. Seus dados foram mantidos.'),
      findsOneWidget,
    );
    expect(harness.repository.saveCalls, 1);
    expect(harness.repository.clearCalls, 0);
    expect(tester.takeException(), isNull);
  });
}

Future<_SettingsHarness> _openSettings(WidgetTester tester, Size size) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final repository = _MemoryAgendaRepository(
    AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14)),
  );
  final controller = AgendaController(repository);
  await tester.pumpWidget(AgendaLivreApp(controller: controller));
  await tester.pumpAndSettle();
  controller.navigate(AgendaPage.settings);
  await tester.pumpAndSettle();
  expect(find.byType(SettingsPage), findsOneWidget);
  expect(tester.takeException(), isNull);
  return _SettingsHarness(controller, repository);
}

class _SettingsHarness {
  const _SettingsHarness(this.controller, this.repository);

  final AgendaController controller;
  final _MemoryAgendaRepository repository;
}

class _MemoryAgendaRepository implements AgendaRepository {
  _MemoryAgendaRepository(AgendaData initial) : value = _clone(initial);

  AgendaData? value;
  int saveCalls = 0;
  int clearCalls = 0;
  bool failSave = false;

  @override
  Future<void> clear() async {
    clearCalls++;
    value = null;
  }

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value == null ? null : _clone(value!);

  @override
  Future<AgendaData> loadOrCreate() async =>
      value == null ? AgendaData() : _clone(value!);

  @override
  Future<void> save(AgendaData data) async {
    saveCalls++;
    if (failSave) throw StateError('save failed');
    value = _clone(data);
  }
}

AgendaData _clone(AgendaData data) => AgendaData.fromJson(data.toJson());
