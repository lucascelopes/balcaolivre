import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/reports/reports_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('replica o relatório operacional atual do WPF no desktop', (
    tester,
  ) async {
    await _pumpReports(tester, const Size(1366, 768));

    expect(find.text('RELATÓRIOS'), findsOneWidget);
    expect(find.text('Relatórios'), findsOneWidget);
    expect(find.text('Imprimir'), findsOneWidget);
    expect(find.text('Exportar'), findsOneWidget);
    expect(find.text('Dia'), findsOneWidget);
    expect(find.text('Semana'), findsOneWidget);
    expect(find.text('Mês'), findsOneWidget);
    expect(find.text('Movimento no mês'), findsOneWidget);
    expect(find.text('Destaques do período'), findsOneWidget);

    final appointments = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Atendimentos')),
    );
    final revenue = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Receita')),
    );
    final ticket = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Ticket médio')),
    );
    final attendance = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Taxa de presença')),
    );
    final cancellations = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Cancelamentos')),
    );

    expect((appointments.top - revenue.top).abs(), lessThan(.1));
    expect((appointments.top - ticket.top).abs(), lessThan(.1));
    expect((appointments.top - attendance.top).abs(), lessThan(.1));
    expect((appointments.top - cancellations.top).abs(), lessThan(.1));
    expect((appointments.width - revenue.width).abs(), lessThan(.1));
    expect(tester.takeException(), isNull);
  });

  testWidgets('reorganiza métricas e cartões no mobile sem overflow', (
    tester,
  ) async {
    await _pumpReports(tester, const Size(390, 844));

    final appointments = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Atendimentos')),
    );
    final revenue = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Receita')),
    );
    final ticket = tester.getRect(
      find.byKey(const ValueKey('reports-metric-Ticket médio')),
    );

    expect((appointments.top - revenue.top).abs(), lessThan(.1));
    expect(ticket.top, greaterThanOrEqualTo(appointments.bottom));
    expect(revenue.right, lessThanOrEqualTo(390));
    expect(find.text('Imprimir'), findsOneWidget);
    expect(find.text('Exportar'), findsOneWidget);
    expect(find.text('Movimento no mês'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

Future<void> _pumpReports(WidgetTester tester, Size size) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  final controller = AgendaController(_MemoryAgendaRepository())
    ..data = AgendaData()
    ..loading = false;
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Scaffold(body: ReportsPage(controller: controller)),
    ),
  );
  await tester.pump();
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
  Future<void> save(AgendaData data) async => value = data;
}
