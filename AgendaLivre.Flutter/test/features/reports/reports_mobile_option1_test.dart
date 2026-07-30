import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/reports/reports_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets(
    'opção 1 conecta diagnóstico, funil, tendência, ação e meta reais',
    (tester) async {
      final controller = await _pump(
        tester,
        const Size(393, 852),
        segment: 'Estética',
      );

      expect(
        find.textContaining('Você perdeu', findRichText: true),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: find.byKey(const Key('reports-mobile-metric-bookings')),
          matching: find.text('22'),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: find.byKey(const Key('reports-mobile-metric-confirmed')),
          matching: find.text('18'),
        ),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: find.byKey(const Key('reports-mobile-metric-completed')),
          matching: find.text('15'),
        ),
        findsOneWidget,
      );
      expect(find.byKey(const Key('reports-mobile-chart')), findsOneWidget);
      expect(
        find.byKey(const Key('reports-mobile-recommendation')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('reports-mobile-goal')), findsOneWidget);

      await tester.tap(find.byKey(const Key('reports-mobile-period')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Mês selecionado'));
      await tester.pumpAndSettle();
      expect(find.text('01/07 a 31/07'), findsOneWidget);

      await tester.ensureVisible(
        find.byKey(const Key('reports-mobile-trend-metric')),
      );
      await tester.tap(find.byKey(const Key('reports-mobile-trend-metric')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Agendamentos').last);
      await tester.pumpAndSettle();
      expect(find.text('Agendamentos'), findsOneWidget);

      await tester.ensureVisible(
        find.byKey(const Key('reports-mobile-open-agenda')),
      );
      await tester.tap(find.byKey(const Key('reports-mobile-open-agenda')));
      await tester.pump();
      expect(controller.page, AgendaPage.agenda);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('linguagem muda para clínica, petshop e oficina', (tester) async {
    for (final entry in const [
      ('Clínica médica', 'Novas consultas', 'pacientes'),
      ('Petshop', 'Novos cuidados', 'tutores'),
      ('Oficina mecânica', 'Novas ordens', 'chegada'),
    ]) {
      await _pump(
        tester,
        const Size(393, 852),
        segment: entry.$1,
        currentAppointments: [
          Appointment(
            customerName: 'Cliente',
            serviceName: 'Serviço',
            start: DateTime(2026, 7, 30, 10),
            status: AppointmentStatus.scheduled,
          ),
        ],
      );

      expect(find.text(entry.$2), findsOneWidget);
      expect(find.textContaining(entry.$3, findRichText: true), findsWidgets);
      expect(tester.takeException(), isNull);
    }
  });

  testWidgets('permanece utilizável em 320 px e tema clínico', (tester) async {
    await _pump(
      tester,
      const Size(320, 568),
      segment: 'Clínica médica',
      themeId: 'medical-blue',
    );

    expect(find.byKey(const Key('reports-mobile-diagnostics')), findsOneWidget);
    expect(find.byKey(const Key('reports-mobile-period')), findsOneWidget);
    expect(find.byKey(const Key('reports-mobile-chart')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('desktop continua usando a composição legada', (tester) async {
    await _pump(tester, const Size(1366, 768), segment: 'Estética');

    expect(find.text('Movimento no mês'), findsOneWidget);
    expect(find.byKey(const Key('reports-mobile-diagnostics')), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('breakpoint mobile termina em 760 px', (tester) async {
    await _pump(tester, const Size(759, 900), segment: 'Estética');
    expect(find.byKey(const Key('reports-mobile-diagnostics')), findsOneWidget);

    await _pump(tester, const Size(760, 900), segment: 'Estética');
    expect(find.byKey(const Key('reports-mobile-diagnostics')), findsNothing);
    expect(find.text('Movimento no mês'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

Future<AgendaController> _pump(
  WidgetTester tester,
  Size size, {
  required String segment,
  String themeId = 'aesthetic-coral',
  List<Appointment>? currentAppointments,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  final appointments = currentAppointments ?? _currentWeekAppointments();
  final controller = AgendaController(_MemoryAgendaRepository())
    ..data = AgendaData(
      settings: AgendaSettings(
        businessName: 'Studio Fluxo',
        businessSegment: segment,
        monthlyRevenueGoal: 24332,
      ),
      appointments: [...appointments, ..._previousTrendAppointments()],
    )
    ..loading = false
    ..selectedDate = DateTime(2026, 7, 30);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId(themeId).toThemeData(),
      home: Scaffold(body: ReportsPage(controller: controller)),
    ),
  );
  await tester.pumpAndSettle();
  return controller;
}

List<Appointment> _currentWeekAppointments() {
  final result = <Appointment>[];
  for (var index = 0; index < 15; index++) {
    final start = DateTime(2026, 7, 27 + (index % 5), 8 + (index % 8));
    result.add(
      Appointment(
        id: 'done-$index',
        customerName: 'Cliente ${index + 1}',
        serviceName: 'Serviço',
        start: start,
        price: 232,
        status: AppointmentStatus.done,
        paymentConfirmedAt: start.add(const Duration(hours: 1)),
        attendanceConfirmedAt: start.subtract(const Duration(days: 1)),
      ),
    );
  }
  for (var index = 0; index < 3; index++) {
    final start = DateTime(2026, 7, 28 + index, 17);
    result.add(
      Appointment(
        id: 'no-show-$index',
        customerName: 'Falta ${index + 1}',
        serviceName: 'Serviço',
        start: start,
        status: AppointmentStatus.noShow,
        attendanceConfirmedAt: start.subtract(const Duration(days: 1)),
      ),
    );
  }
  for (var index = 0; index < 4; index++) {
    result.add(
      Appointment(
        id: 'pending-$index',
        customerName: ['Juliana', 'Beatriz', 'Camila', 'Rafael'][index],
        serviceName: 'Serviço',
        start: DateTime(2026, 7, 31, 10 + index),
        status: AppointmentStatus.scheduled,
      ),
    );
  }
  return result;
}

List<Appointment> _previousTrendAppointments() {
  final result = <Appointment>[
    for (final entry in [
      (DateTime(2026, 7, 6, 10), 2650.0),
      (DateTime(2026, 7, 13, 10), 3120.0),
    ])
      Appointment(
        customerName: 'Histórico',
        serviceName: 'Serviço',
        start: entry.$1,
        price: entry.$2,
        status: AppointmentStatus.done,
        paymentConfirmedAt: entry.$1,
      ),
  ];
  for (var index = 0; index < 14; index++) {
    final start = DateTime(2026, 7, 20 + (index % 5), 8 + (index % 7));
    result.add(
      Appointment(
        customerName: 'Histórico ${index + 1}',
        serviceName: 'Serviço',
        start: start,
        price: 2780 / 14,
        status: AppointmentStatus.done,
        paymentConfirmedAt: start,
      ),
    );
  }
  for (var index = 0; index < 2; index++) {
    result.add(
      Appointment(
        customerName: 'Confirmado ${index + 1}',
        serviceName: 'Serviço',
        start: DateTime(2026, 7, 24, 16 + index),
        status: AppointmentStatus.confirmed,
      ),
    );
    result.add(
      Appointment(
        customerName: 'Pendente ${index + 1}',
        serviceName: 'Serviço',
        start: DateTime(2026, 7, 25, 10 + index),
        status: AppointmentStatus.scheduled,
      ),
    );
  }
  return result;
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
