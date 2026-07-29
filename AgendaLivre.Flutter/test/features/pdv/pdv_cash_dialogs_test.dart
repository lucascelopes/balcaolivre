import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/pdv/pdv_cash_dialogs.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('opening dialog works on desktop and persists the opening fund', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1366, 768);
    addTearDown(tester.view.reset);
    final controller = _controller();

    await tester.pumpWidget(_Harness(controller: controller));
    await tester.tap(find.byKey(const Key('show-opening')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('pdv-cash-opening-dialog')), findsOneWidget);
    expect(find.text('Abrir caixa'), findsOneWidget);
    expect(find.text('Entrar sem abrir caixa'), findsOneWidget);
    await tester.tap(find.text('+ R\$ 200'));
    await tester.tap(find.byKey(const Key('pdv-cash-opening-confirm')));
    await tester.pumpAndSettle();

    final session = controller.openCashSessionForDay(_reference)!;
    expect(session.openingBalance, 200);
    expect(session.operatorName, 'Lucas Cesar Lopes');
    expect(find.byKey(const Key('pdv-cash-opening-dialog')), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('closing dialog mirrors the WPF summary and closes the shift', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1366, 768);
    addTearDown(tester.view.reset);
    final controller = _controller();
    await controller.openCashSession(
      openingBalance: 150,
      openedAt: _reference.subtract(const Duration(hours: 2)),
    );

    await tester.pumpWidget(_Harness(controller: controller));
    await tester.tap(find.byKey(const Key('show-closing')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('pdv-cash-closing-dialog')), findsOneWidget);
    expect(
      find.text('Conferência lado a lado com recibo do turno'),
      findsOneWidget,
    );
    expect(find.text('Conferir e fechar caixa'), findsOneWidget);
    expect(find.text('Tudo certo · diferença R\$ 0,00'), findsOneWidget);

    await tester.tap(find.byKey(const Key('pdv-cash-closing-confirm')));
    await tester.pumpAndSettle();

    expect(controller.data.cashSessions.single.isOpen, isFalse);
    expect(controller.data.cashSessions.single.closingBalance, 150);
    expect(tester.takeException(), isNull);
  });

  testWidgets('opening and closing surfaces remain usable on mobile', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.reset);
    final controller = _controller();

    await tester.pumpWidget(_Harness(controller: controller));
    await tester.tap(find.byKey(const Key('show-opening')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('pdv-cash-opening-dialog')), findsOneWidget);
    expect(
      find.byKey(const Key('pdv-cash-opening-confirm')).hitTestable(),
      findsOneWidget,
    );
    await tester.tap(find.byKey(const Key('pdv-cash-opening-skip')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('show-closing')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-cash-closing-dialog')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

final _reference = DateTime(2026, 7, 14, 10);

AgendaController _controller() => AgendaController(_MemoryAgendaRepository())
  ..data = AgendaData(
    settings: AgendaSettings(
      accountFullName: 'Lucas Cesar Lopes',
      businessName: 'Lucas Barbearia',
      businessSegment: 'Barbearia',
      onboardingCompleted: true,
    ),
  )
  ..loading = false;

class _Harness extends StatelessWidget {
  const _Harness({required this.controller});

  final AgendaController controller;

  @override
  Widget build(BuildContext context) => MaterialApp(
    theme: AgendaThemes.byId('').toThemeData(),
    home: Builder(
      builder: (context) => Scaffold(
        body: Column(
          children: [
            FilledButton(
              key: const Key('show-opening'),
              onPressed: () => showPdvCashOpeningDialog(
                context,
                controller,
                referenceNow: _reference,
              ),
              child: const Text('Opening'),
            ),
            FilledButton(
              key: const Key('show-closing'),
              onPressed: () => showPdvCashClosingDialog(
                context,
                controller,
                referenceNow: _reference,
              ),
              child: const Text('Closing'),
            ),
          ],
        ),
      ),
    ),
  );
}

class _MemoryAgendaRepository implements AgendaRepository {
  AgendaData? value;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value;

  @override
  Future<AgendaData> loadOrCreate() async => value ?? AgendaData();

  @override
  Future<void> save(AgendaData data) async {
    value = AgendaData.fromJson(data.toJson());
  }
}
