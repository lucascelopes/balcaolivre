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

  testWidgets('mantém o dashboard analítico do WPF no desktop', (tester) async {
    await _pumpFinance(tester, const Size(1200, 640), contentWidth: 940);

    final result = tester.getRect(
      find.byKey(const Key('finance-result-formation-card')),
    );
    final nextThirtyDays = tester.getRect(
      find.byKey(const Key('finance-next-30-days-card')),
    );
    final risk = tester.getRect(find.byKey(const Key('finance-risk-card')));
    final funnel = tester.getRect(
      find.byKey(const Key('finance-receipt-funnel-card')),
    );
    final composition = tester.getRect(
      find.byKey(const Key('finance-receipt-composition-card')),
    );

    expect(find.byKey(const Key('finance-kpi-grid')), findsOneWidget);
    expect(find.byKey(const Key('finance-kpi-Receita')), findsOneWidget);
    expect(
      find.byKey(const Key('finance-kpi-Agenda a receber')),
      findsOneWidget,
    );
    expect(result.top, closeTo(nextThirtyDays.top, .1));
    expect(result.bottom, closeTo(nextThirtyDays.bottom, .1));
    expect(risk.top, closeTo(funnel.top, .1));
    expect(risk.top, closeTo(composition.top, .1));
    expect(risk.bottom, closeTo(funnel.bottom, .1));
    expect(risk.bottom, closeTo(composition.bottom, .1));
    expect(find.byKey(const Key('finance-forecast-card')), findsOneWidget);
    expect(
      find.byKey(const Key('finance-quick-operations-card')),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('reempilha o dashboard do WPF sem overflow em 390x844', (
    tester,
  ) async {
    await _pumpFinance(tester, const Size(390, 844));

    final result = tester.getRect(
      find.byKey(const Key('finance-result-formation-card')),
    );
    final nextThirtyDays = tester.getRect(
      find.byKey(const Key('finance-next-30-days-card')),
    );
    final risk = tester.getRect(find.byKey(const Key('finance-risk-card')));
    final funnel = tester.getRect(
      find.byKey(const Key('finance-receipt-funnel-card')),
    );
    final composition = tester.getRect(
      find.byKey(const Key('finance-receipt-composition-card')),
    );

    expect(find.text('Financeiro'), findsOneWidget);
    expect(find.byKey(const Key('finance-kpi-grid')), findsOneWidget);
    expect(result.left, closeTo(14, .1));
    expect(result.right, closeTo(376, .1));
    expect(nextThirtyDays.top, greaterThan(result.bottom));
    expect(risk.top, greaterThan(nextThirtyDays.bottom));
    expect(funnel.top, greaterThan(risk.bottom));
    expect(composition.top, greaterThan(funnel.bottom));
    expect(find.byKey(const Key('finance-quick-receive')), findsOneWidget);
    expect(find.byKey(const Key('finance-quick-expense')), findsOneWidget);
    expect(find.byKey(const Key('finance-quick-product')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('calcula receita e agenda a receber pelas datas do WPF', (
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

    final revenue = find.byKey(const Key('finance-kpi-Receita'));
    expect(
      find.descendant(of: revenue, matching: find.textContaining('110,00')),
      findsOneWidget,
    );
    final pending = find.byKey(const Key('finance-kpi-Agenda a receber'));
    expect(
      find.descendant(of: pending, matching: find.textContaining('45,00')),
      findsOneWidget,
    );
    expect(
      find.descendant(
        of: pending,
        matching: find.text('1 atendimento(s) sem recebimento'),
      ),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('abre o lançamento de entrada pelas operações rápidas', (
    tester,
  ) async {
    await _pumpFinance(tester, const Size(1200, 720));

    await tester.ensureVisible(find.byKey(const Key('finance-quick-receive')));
    await tester.tap(find.byKey(const Key('finance-quick-receive')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('finance-payment-dialog')), findsOneWidget);
    expect(find.byKey(const Key('appointment-payment-dialog')), findsNothing);
    expect(find.text('Registrar pagamento'), findsWidgets);
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
