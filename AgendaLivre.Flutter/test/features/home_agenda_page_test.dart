import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/agenda/agenda_page.dart' as agenda;
import 'package:agenda_livre/features/home/home_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  for (final size in <Size>[
    const Size(1382, 736),
    const Size(1365, 694),
    const Size(390, 844),
    const Size(844, 390),
  ]) {
    testWidgets('Home e Agenda permanecem responsivas em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      final controller = AgendaController(_MemoryAgendaRepository())
        ..data = AgendaData()
        ..loading = false
        ..selectedDate = DateUtils.dateOnly(DateTime.now());

      await _pumpPage(tester, size, HomePage(controller: controller));
      expect(find.textContaining('Agendamentos'), findsOneWidget);
      expect(find.byKey(const Key('home-schedule-card')), findsOneWidget);
      expect(find.text('Ocupação de hoje'), findsOneWidget);
      expect(find.text('Desempenho da semana'), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(tester, size, agenda.AgendaPage(controller: controller));
      expect(find.text('Fluxo de atendimento'), findsOneWidget);
      expect(find.text('Quadro'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('Painel usa o primeiro nome do responsável como no WPF', (
    tester,
  ) async {
    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaData()
      ..data.settings.businessName = 'Studio Nina Beauty'
      ..data.settings.accountFullName = 'Nina Almeida'
      ..loading = false
      ..selectedDate = DateUtils.dateOnly(DateTime.now());

    await _pumpPage(
      tester,
      const Size(1200, 720),
      HomePage(controller: controller),
    );

    expect(find.textContaining(', Nina'), findsOneWidget);
    expect(find.text('MINHA AGENDA'), findsOneWidget);
    expect(find.byKey(const Key('home-occupancy-card')), findsOneWidget);
    expect(find.byKey(const Key('home-week-performance-card')), findsOneWidget);
    expect(find.text('Próximos horários'), findsNothing);
    expect(find.text('Atenção'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('faixa semanal do Painel troca a data selecionada', (
    tester,
  ) async {
    final selected = DateUtils.dateOnly(DateTime(2026, 7, 18));
    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaData()
      ..loading = false
      ..selectedDate = selected;

    await _pumpPage(
      tester,
      const Size(1200, 720),
      HomePage(controller: controller),
    );

    await tester.tap(find.byKey(const Key('home-next-day')));
    await tester.pump();
    expect(controller.selectedDate, DateTime(2026, 7, 19));
    expect(tester.takeException(), isNull);
  });

  testWidgets('toque no horário seleciona antes de oferecer cobrança', (
    tester,
  ) async {
    final date = DateUtils.dateOnly(DateTime.now());
    final appointment = Appointment(
      id: 'agenda-selection-test',
      customerName: 'Mariana Costa',
      serviceName: 'Manicure',
      professionalName: 'Profissional 1',
      start: DateTime(date.year, date.month, date.day, 10),
      durationMinutes: 45,
      price: 55,
      status: AppointmentStatus.confirmed,
    );
    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaData(appointments: [appointment])
      ..loading = false
      ..selectedDate = date;

    await _pumpPage(
      tester,
      const Size(1200, 720),
      agenda.AgendaPage(controller: controller),
    );

    await tester.tap(
      find.byKey(
        const ValueKey('agenda-board-appointment-agenda-selection-test'),
      ),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('selected-appointment-charge')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('appointment-payment-dialog')), findsNothing);
    expect(find.byKey(const Key('agenda-clear-selection')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'toque no cliente do Painel abre Receber e Editar fica como ação explícita',
    (tester) async {
      final date = DateUtils.dateOnly(DateTime.now());
      final appointment = Appointment(
        id: 'home-receive-test',
        customerName: 'Mariana Costa',
        serviceName: 'Manicure',
        professionalName: 'Profissional 1',
        start: DateTime(date.year, date.month, date.day, 10),
        durationMinutes: 45,
        price: 55,
        status: AppointmentStatus.confirmed,
      );
      final controller = AgendaController(_MemoryAgendaRepository())
        ..data = AgendaData(appointments: [appointment])
        ..loading = false
        ..selectedDate = date;

      await _pumpPage(
        tester,
        const Size(1200, 900),
        HomePage(controller: controller),
      );

      final appointmentFinder = find.byKey(
        const ValueKey('agenda-board-appointment-home-receive-test'),
      );
      await tester.ensureVisible(appointmentFinder);
      await tester.tap(appointmentFinder);
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('appointment-payment-dialog')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('appointment-dialog')), findsNothing);

      await tester.tap(find.text('Editar atendimento'));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('appointment-payment-dialog')), findsNothing);
      expect(find.byKey(const Key('appointment-dialog')), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}

Future<void> _pumpPage(WidgetTester tester, Size size, Widget page) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('').toThemeData(),
      home: Scaffold(body: page),
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
