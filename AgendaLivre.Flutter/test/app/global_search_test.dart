import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('busca global abre páginas do WPF em qualquer área', (
    tester,
  ) async {
    final controller = _controller();
    await _pumpShell(tester, controller);

    expect(find.byKey(const Key('global-search-field')), findsOneWidget);
    await tester.enterText(
      find.byKey(const Key('global-search-field')),
      'suporte',
    );
    await tester.pump();
    expect(
      find.byKey(const Key('global-search-result-page-suporte')),
      findsOneWidget,
    );

    await tester.tap(
      find.byKey(const Key('global-search-result-page-suporte')),
    );
    await tester.pumpAndSettle();
    expect(controller.page, AgendaPage.support);
    expect(find.text('Central de ajuda'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('busca global localiza e abre um agendamento', (tester) async {
    final controller = _controller();
    await _pumpShell(tester, controller);

    await tester.enterText(
      find.byKey(const Key('global-search-field')),
      'Marina',
    );
    await tester.pump();
    final result = find.byKey(
      const Key('global-search-result-appointment-appointment-marina'),
    );
    expect(result, findsOneWidget);
    await tester.tap(result);
    await tester.pumpAndSettle();

    expect(controller.page, AgendaPage.agenda);
    expect(controller.searchQuery, 'Marina Costa');
    expect(find.text('Editar agendamento'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

AgendaController _controller() {
  final data = AgendaData(
    customers: [
      Customer(
        id: 'customer-marina',
        name: 'Marina Costa',
        phone: '(33) 99999-0000',
        lastSeenAt: DateTime(2026, 7, 20),
      ),
    ],
    services: [
      ServiceItem(
        id: 'service-corte',
        name: 'Corte feminino',
        category: 'Cabelo',
      ),
    ],
    appointments: [
      Appointment(
        id: 'appointment-marina',
        customerName: 'Marina Costa',
        customerPhone: '(33) 99999-0000',
        serviceName: 'Corte feminino',
        professionalName: 'Nina Almeida',
        start: DateTime(2026, 7, 30, 10),
      ),
    ],
  )..settings.businessName = 'Studio Nina Beauty';
  return AgendaController(_MemoryAgendaRepository(data))
    ..data = data
    ..loading = false;
}

Future<void> _pumpShell(
  WidgetTester tester,
  AgendaController controller,
) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = const Size(1366, 768);
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('').toThemeData(),
      home: ResponsiveAgendaShell(
        controller: controller,
        referenceNow: DateTime(2026, 7, 29, 9),
      ),
    ),
  );
  await tester.pump();
}

class _MemoryAgendaRepository implements AgendaRepository {
  _MemoryAgendaRepository(this.data);

  AgendaData data;

  @override
  Future<void> clear() async => data = AgendaData();

  @override
  Future<bool> hasData() async => true;

  @override
  Future<AgendaData?> load() async => data;

  @override
  Future<AgendaData> loadOrCreate() async => data;

  @override
  Future<void> save(AgendaData value) async => data = value;
}
