import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/finance/finance_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('mantém o grid do WPF no mínimo desktop de 1200x640', (
    tester,
  ) async {
    await _pumpFinance(tester, const Size(1200, 640), contentWidth: 940);

    final hero = tester.getRect(find.byKey(const Key('finance-hero')));
    final strip = tester.getRect(find.byKey(const Key('finance-metric-strip')));
    final sources = tester.getRect(
      find.byKey(const Key('finance-sources-card')),
    );
    final pending = tester.getRect(
      find.byKey(const Key('finance-pending-card')),
    );
    final expenses = tester.getRect(
      find.byKey(const Key('finance-expenses-card')),
    );
    final chart = tester.getRect(find.byKey(const Key('finance-chart-card')));
    final mercadoPago = tester.getRect(
      find.byKey(const Key('finance-mercado-pago-card')),
    );

    // O WPF reserva 260 px para a barra lateral no seu MinWidth de 1200.
    expect(hero.left, closeTo(288, .1));
    expect(hero.right, closeTo(1164, .1));
    expect(strip.left, closeTo(hero.left, .1));
    expect(strip.right, closeTo(hero.right, .1));
    expect(strip.height, closeTo(102, .1));
    expect(sources.width / pending.width, closeTo(1.22, .03));
    expect(expenses.width - pending.width, closeTo(10, 1));
    expect((sources.top - pending.top).abs(), lessThan(.1));
    expect((sources.bottom - expenses.bottom).abs(), lessThan(.1));
    expect(chart.width / mercadoPago.width, closeTo(2.15 / 1.1, .04));
    expect((chart.top - mercadoPago.top).abs(), lessThan(.1));
    expect((chart.bottom - mercadoPago.bottom).abs(), lessThan(.1));
    expect(find.text('Lançar entrada'), findsOneWidget);
    expect(find.text('Lançar despesa'), findsOneWidget);
    expect(find.text('Vender produto'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('reempilha os mesmos blocos do WPF sem overflow em 390x844', (
    tester,
  ) async {
    await _pumpFinance(tester, const Size(390, 844));

    expect(find.byKey(const Key('finance-primary-stack')), findsOneWidget);
    expect(find.byKey(const Key('finance-lower-stack')), findsOneWidget);
    expect(find.byKey(const Key('finance-metric-strip')), findsOneWidget);
    final hero = tester.getRect(find.byKey(const Key('finance-hero')));
    final strip = tester.getRect(find.byKey(const Key('finance-metric-strip')));
    final result = tester.getRect(
      find.byKey(const Key('finance-metric-result')),
    );
    final received = tester.getRect(
      find.byKey(const Key('finance-metric-received')),
    );
    final pending = tester.getRect(
      find.byKey(const Key('finance-metric-pending')),
    );
    final expenses = tester.getRect(
      find.byKey(const Key('finance-metric-expenses')),
    );
    expect(hero.left, closeTo(14, .1));
    expect(hero.right, closeTo(376, .1));
    expect(strip.height, closeTo(188, .1));
    expect(result.top, closeTo(received.top, .1));
    expect(pending.top, greaterThan(result.top));
    expect(pending.top, closeTo(expenses.top, .1));
    final stripWidget = tester.widget<Container>(
      find.byKey(const Key('finance-metric-strip')),
    );
    expect(
      (stripWidget.decoration! as BoxDecoration).color,
      const Color(0xFF171614),
    );
    expect(find.text('Lançar entrada'), findsOneWidget);
    expect(find.text('Lançar despesa'), findsOneWidget);
    expect(find.text('Vender produto'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('calcula recebimentos e pendências pelas mesmas datas do WPF', (
    tester,
  ) async {
    final now = DateTime.now();
    final paidThisMonth = DateTime(now.year, now.month, 5, 10);
    final paidLastMonth = DateTime(now.year, now.month - 1, 25, 10);
    final data = AgendaData(
      appointments: [
        Appointment(
          id: 'paid-now',
          customerName: 'Pago no mês',
          serviceName: 'Serviço pago',
          start: paidLastMonth,
          price: 80,
          status: AppointmentStatus.done,
          paymentConfirmedAt: paidThisMonth,
        ),
        Appointment(
          id: 'paid-before',
          customerName: 'Pago antes',
          serviceName: 'Serviço antigo',
          start: paidThisMonth,
          price: 900,
          status: AppointmentStatus.done,
          paymentConfirmedAt: paidLastMonth,
        ),
        Appointment(
          id: 'customer-account',
          customerName: 'Conta do cliente',
          serviceName: 'Serviço em conta',
          start: paidThisMonth,
          price: 70,
          status: AppointmentStatus.done,
          paymentConfirmedAt: paidThisMonth,
        ),
        Appointment(
          id: 'done-unpaid',
          customerName: 'Ainda em aberto',
          serviceName: 'Serviço pendente',
          start: paidThisMonth,
          price: 45,
          status: AppointmentStatus.done,
        ),
      ],
      customerReceivables: [
        CustomerReceivable(
          appointmentId: 'customer-account',
          customerName: 'Conta do cliente',
          originalValue: 70,
          remainingValue: 70,
          status: 'open',
          openedAt: paidThisMonth,
        ),
        CustomerReceivable(
          appointmentId: 'settled-account',
          customerName: 'Saldo quitado',
          originalValue: 30,
          remainingValue: 0,
          status: 'paid',
          paidAt: paidThisMonth,
          openedAt: paidLastMonth,
        ),
      ],
    );

    await _pumpFinance(tester, const Size(390, 844), data: data);

    final sources = find.byKey(const Key('finance-sources-card'));
    expect(
      find.descendant(of: sources, matching: find.textContaining('110,00')),
      findsNWidgets(2),
    );
    final pendingCard = find.byKey(const Key('finance-pending-card'));
    expect(
      find.descendant(of: pendingCard, matching: find.text('Ainda em aberto')),
      findsOneWidget,
    );
    expect(
      find.descendant(of: pendingCard, matching: find.textContaining('45,00')),
      findsNWidgets(2),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('abre a cobrança vinculada ao atendimento pendente escolhido', (
    tester,
  ) async {
    final pending = Appointment(
      id: 'pending-to-receive',
      customerName: 'Mariana Costa',
      serviceName: 'Manicure',
      start: DateTime.now(),
      price: 55,
      status: AppointmentStatus.done,
    );
    await _pumpFinance(
      tester,
      const Size(1200, 720),
      data: AgendaData(appointments: [pending]),
    );

    await tester.tap(
      find.byKey(const ValueKey('finance-receive-pending-to-receive')),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('appointment-payment-dialog')), findsOneWidget);
    expect(find.byKey(const Key('finance-payment-dialog')), findsNothing);
    expect(find.text('Mariana Costa'), findsWidgets);
    expect(tester.takeException(), isNull);
  });
}

Future<AgendaController> _pumpFinance(
  WidgetTester tester,
  Size size, {
  double? contentWidth,
  AgendaData? data,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  final controller = AgendaController(_MemoryAgendaRepository())
    ..data = data ?? AgendaData()
    ..loading = false;
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Scaffold(
        body: Align(
          alignment: Alignment.topRight,
          child: SizedBox(
            width: contentWidth,
            height: size.height,
            child: FinancePage(controller: controller),
          ),
        ),
      ),
    ),
  );
  await tester.pump();
  return controller;
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
