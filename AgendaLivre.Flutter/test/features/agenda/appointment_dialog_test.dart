import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/agenda/appointment_dialog.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('replica o modal desktop WPF com fluxo em três etapas', (
    tester,
  ) async {
    final harness = await _pumpDialog(tester, const Size(1382, 736));

    expect(
      tester.getSize(find.byKey(const Key('appointment-dialog'))),
      const Size(900, 520),
    );
    expect(find.byKey(const Key('appointment-step-1')), findsOneWidget);
    expect(find.byKey(const Key('appointment-step-2')), findsOneWidget);
    expect(find.byKey(const Key('appointment-step-3')), findsOneWidget);
    expect(find.text('Horário e serviço'), findsOneWidget);
    expect(find.byKey(const Key('appointment-more-actions')), findsOneWidget);
    await tester.tap(find.byKey(const Key('appointment-more-actions')));
    await tester.pumpAndSettle();
    expect(find.text('Duplicar agendamento'), findsOneWidget);
    expect(find.text('Marcar que faltou'), findsOneWidget);
    expect(find.text('Cancelar agendamento'), findsOneWidget);
    expect(find.text('Excluir definitivamente'), findsOneWidget);
    await tester.tapAt(const Offset(8, 8));
    await tester.pumpAndSettle();

    final footer = find.byKey(const Key('appointment-dialog-footer'));
    final footerTop = tester.getTopLeft(footer).dy;

    await tester.tap(find.byKey(const Key('appointment-continue')));
    await tester.pumpAndSettle();
    expect(find.text('Cliente'), findsWidgets);
    expect(tester.getTopLeft(footer).dy, closeTo(footerTop, .1));

    await tester.tap(find.byKey(const Key('appointment-continue')));
    await tester.pumpAndSettle();
    expect(find.text('Revise antes de salvar'), findsOneWidget);
    expect(find.byKey(const Key('appointment-save')).hitTestable(), findsOne);
    expect(tester.getTopLeft(footer).dy, closeTo(footerTop, .1));
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(const Key('appointment-save')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('appointment-dialog')), findsNothing);
    expect(harness.repository.saveCalls, 1);
  });

  testWidgets('mantém fullscreen mobile e rodapé fixo durante a rolagem', (
    tester,
  ) async {
    await _pumpDialog(tester, const Size(390, 844));

    expect(
      tester.getSize(find.byKey(const Key('appointment-dialog'))),
      const Size(390, 844),
    );
    final footer = find.byKey(const Key('appointment-dialog-footer'));
    final footerTop = tester.getTopLeft(footer).dy;
    final scheduleTop = tester.getTopLeft(find.text('Horário e serviço')).dy;

    await tester.drag(
      find.byKey(const Key('appointment-dialog-scroll')),
      const Offset(0, -260),
    );
    await tester.pumpAndSettle();

    expect(tester.getTopLeft(footer).dy, closeTo(footerTop, .1));
    expect(
      tester.getTopLeft(find.text('Horário e serviço')).dy,
      lessThan(scheduleTop),
    );
    expect(
      find.byKey(const Key('appointment-continue')).hitTestable(),
      findsOne,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('assistente sugere um encaixe para horário excepcional', (
    tester,
  ) async {
    final start = _futureMondayAtTen().copyWith(hour: 21);
    await _pumpNewDialog(tester, const Size(1382, 736), initialStart: start);

    expect(find.byKey(const Key('schedule-assistant-card')), findsOneWidget);
    expect(find.text('Sugestão inteligente de horário'), findsOneWidget);
    expect(
      find.byKey(const Key('schedule-assistant-use-suggestion')),
      findsOneWidget,
    );
    await tester.ensureVisible(
      find.byKey(const Key('schedule-assistant-use-suggestion')),
    );
    await tester.pumpAndSettle();
    await tester.tap(
      find.byKey(const Key('schedule-assistant-use-suggestion')),
    );
    await tester.pump();
    expect(find.byKey(const Key('schedule-assistant-card')), findsNothing);
    expect(tester.takeException(), isNull);
  });
}

Future<_DialogHarness> _pumpDialog(WidgetTester tester, Size size) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final start = _futureMondayAtTen();
  final data = AgendaSeedData.salon(referenceDate: start);
  final service = data.services.first;
  final professional = data.professionals.first;
  final appointment = Appointment(
    id: 'appointment-dialog-test',
    segment: service.segment,
    customerName: 'Mariana Costa',
    customerPhone: '(11) 99876-5432',
    customerProfile: 'Prefere atendimento no fim da tarde',
    serviceId: service.id,
    serviceName: service.name,
    professionalId: professional.id,
    professionalName: professional.name,
    resourceName: service.defaultResource,
    start: start,
    durationMinutes: service.durationMinutes,
    price: service.price,
    status: AppointmentStatus.confirmed,
    notes: 'Primeira visita — confirmar pelo WhatsApp.',
  );
  data.appointments.add(appointment);
  final repository = _MemoryAgendaRepository(data);
  final controller = AgendaController(repository)
    ..data = data
    ..loading = false;

  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('').toThemeData(),
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              onPressed: () {
                showAppointmentDialog(
                  context,
                  controller,
                  appointment: appointment,
                );
              },
              child: const Text('Abrir agendamento'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Abrir agendamento'));
  await tester.pumpAndSettle();
  return _DialogHarness(repository);
}

Future<void> _pumpNewDialog(
  WidgetTester tester,
  Size size, {
  required DateTime initialStart,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final data = AgendaSeedData.salon(referenceDate: initialStart);
  final controller = AgendaController(_MemoryAgendaRepository(data))
    ..data = data
    ..loading = false;
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('').toThemeData(),
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              onPressed: () => showAppointmentDialog(
                context,
                controller,
                initialStart: initialStart,
              ),
              child: const Text('Novo agendamento'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Novo agendamento'));
  await tester.pumpAndSettle();
}

DateTime _futureMondayAtTen() {
  final now = DateTime.now();
  var date = DateTime(
    now.year,
    now.month,
    now.day,
    10,
  ).add(Duration(days: (DateTime.monday - now.weekday) % 7));
  if (!date.isAfter(now)) date = date.add(const Duration(days: 7));
  return date;
}

class _DialogHarness {
  const _DialogHarness(this.repository);

  final _MemoryAgendaRepository repository;
}

class _MemoryAgendaRepository implements AgendaRepository {
  _MemoryAgendaRepository(this.data);

  AgendaData data;
  int saveCalls = 0;

  @override
  Future<void> clear() async => data = AgendaData();

  @override
  Future<bool> hasData() async => true;

  @override
  Future<AgendaData?> load() async => data;

  @override
  Future<AgendaData> loadOrCreate() async => data;

  @override
  Future<void> save(AgendaData value) async {
    data = value;
    saveCalls++;
  }
}
