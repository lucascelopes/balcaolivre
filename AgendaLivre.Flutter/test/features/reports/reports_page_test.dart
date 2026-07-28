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

  testWidgets('repete a grade 3 por 2 e as ações do WPF no desktop', (
    tester,
  ) async {
    await _pumpReports(tester, const Size(1366, 768));

    expect(find.text('AGENDA LIVRE'), findsOneWidget);
    expect(find.text('Relatórios'), findsOneWidget);
    expect(find.text('Copiar resumo'), findsOneWidget);
    expect(find.text('Pré-visualizar'), findsOneWidget);
    expect(find.text('Copiar CSV'), findsOneWidget);
    expect(find.text('Leituras rápidas'), findsOneWidget);
    expect(find.text('Serviços mais realizados'), findsOneWidget);
    expect(find.text('Profissionais'), findsOneWidget);

    final appointments = tester.getRect(
      find.byKey(const ValueKey('report-metric-Agendamentos')),
    );
    final completed = tester.getRect(
      find.byKey(const ValueKey('report-metric-Finalizados')),
    );
    final lost = tester.getRect(
      find.byKey(const ValueKey('report-metric-Cancelados/faltas')),
    );
    final revenue = tester.getRect(
      find.byKey(const ValueKey('report-metric-Receita')),
    );
    final ticket = tester.getRect(
      find.byKey(const ValueKey('report-metric-Ticket médio')),
    );

    expect((appointments.top - completed.top).abs(), lessThan(.1));
    expect((appointments.top - lost.top).abs(), lessThan(.1));
    expect((revenue.top - ticket.top).abs(), lessThan(.1));
    expect(revenue.top, greaterThan(appointments.bottom));
    expect((appointments.width - completed.width).abs(), lessThan(.1));
    expect(
      tester
          .getSize(
            find.byKey(const ValueKey('reports-appointments-chart-canvas')),
          )
          .width,
      greaterThan(400),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('reorganiza para duas colunas no mobile sem overflow', (
    tester,
  ) async {
    await _pumpReports(tester, const Size(390, 844));

    final appointments = tester.getRect(
      find.byKey(const ValueKey('report-metric-Agendamentos')),
    );
    final completed = tester.getRect(
      find.byKey(const ValueKey('report-metric-Finalizados')),
    );
    final lost = tester.getRect(
      find.byKey(const ValueKey('report-metric-Cancelados/faltas')),
    );

    expect((appointments.top - completed.top).abs(), lessThan(.1));
    expect(lost.top, greaterThan(appointments.bottom));
    expect(completed.right, lessThanOrEqualTo(390));
    expect(find.text('Copiar resumo'), findsOneWidget);
    expect(find.text('Copiar CSV'), findsOneWidget);
    expect(
      tester
          .getSize(
            find.byKey(const ValueKey('reports-appointments-chart-canvas')),
          )
          .width,
      greaterThan(300),
    );
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
