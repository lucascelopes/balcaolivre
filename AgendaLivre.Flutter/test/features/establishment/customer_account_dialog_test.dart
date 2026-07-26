import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/establishment/customer_account_dialog.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../services/fake_http_transport.dart';

void main() {
  testWidgets('recebe toda a conta em dinheiro no celular sem overflow', (
    tester,
  ) async {
    final controller = _controller();
    await _open(tester, controller, const Size(390, 844));

    expect(find.text('Receber conta'), findsOneWidget);
    expect(find.text('2 atendimentos aguardando pagamento.'), findsOneWidget);
    expect(find.text('R\$ 150,00'), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.tap(find.byKey(const Key('customer-account-method')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Dinheiro').last);
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(const Key('customer-account-receive')));
    await tester.pumpAndSettle();

    expect(
      controller.data.customerReceivables,
      everyElement(
        isA<CustomerReceivable>()
            .having((item) => item.status, 'status', 'paid')
            .having((item) => item.remainingValue, 'saldo', 0)
            .having((item) => item.paymentMethod, 'forma', 'Dinheiro'),
      ),
    );
    expect(
      controller.data.appointments,
      everyElement(
        isA<Appointment>().having(
          (item) => item.paymentConfirmedAt,
          'confirmação',
          isNotNull,
        ),
      ),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('só baixa a conta Mercado Pago depois da aprovação Point', (
    tester,
  ) async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/point/charge')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"attemptId":"A1","orderId":"O1","localReference":"REF1","status":"created"}',
        );
      }
      return const ServiceHttpResponse(
        statusCode: 200,
        body:
            '{"ok":true,"attemptId":"A1","paymentId":"P1","status":"approved","paid":true}',
      );
    });
    final service = MercadoPagoService(
      transport: transport,
      config: MercadoPagoServiceConfig(
        activateClient: false,
        baseUri: Uri.parse('https://api.example/functions/v1/payments'),
        contextProvider: () => const MercadoPagoClientContext(
          licenseKey: 'TEST',
          machineHash: 'TEST',
        ),
      ),
    );
    final controller = _controller(service: service);
    controller.data.settings
      ..mercadoPagoEnabled = true
      ..mercadoPagoConnected = true
      ..mercadoPagoDefaultTerminalId = 'T1'
      ..mercadoPagoDefaultTerminalLabel = 'Point balcão';
    await _open(tester, controller, const Size(1200, 760));

    await tester.tap(find.byKey(const Key('customer-account-method')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Crédito na Point').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('customer-account-receive')));
    await tester.pump();

    expect(
      controller.data.customerReceivables.every(
        (item) => item.status == 'open',
      ),
      isTrue,
    );
    expect(
      find.byKey(const Key('mercado-pago-point-progress')),
      findsOneWidget,
    );

    await tester.pump(const Duration(milliseconds: 1400));
    await tester.pumpAndSettle();

    expect(
      controller.data.customerReceivables,
      everyElement(
        isA<CustomerReceivable>()
            .having((item) => item.status, 'status', 'paid')
            .having((item) => item.paymentProvider, 'provedor', 'Mercado Pago')
            .having((item) => item.paymentReference, 'referência', 'P1'),
      ),
    );
    expect(tester.takeException(), isNull);
  });
}

AgendaController _controller({MercadoPagoService? service}) {
  final customer = Customer(
    id: 'C1',
    name: 'Maria Souza',
    phone: '(11) 99999-9999',
  );
  final first = Appointment(
    id: 'A1',
    customerId: customer.id,
    customerName: customer.name,
    serviceName: 'Corte',
    price: 100,
  );
  final second = Appointment(
    id: 'A2',
    customerId: customer.id,
    customerName: customer.name,
    serviceName: 'Escova',
    price: 50,
  );
  final data = AgendaData(
    customers: [customer],
    appointments: [first, second],
    customerReceivables: [
      CustomerReceivable(
        id: 'R1',
        customerId: customer.id,
        customerName: customer.name,
        appointmentId: first.id,
        description: first.serviceName,
        originalValue: first.price,
        remainingValue: first.price,
      ),
      CustomerReceivable(
        id: 'R2',
        customerId: customer.id,
        customerName: customer.name,
        appointmentId: second.id,
        description: second.serviceName,
        originalValue: second.price,
        remainingValue: second.price,
      ),
    ],
  );
  return AgendaController(
      _MemoryAgendaRepository(),
      mercadoPagoService: service,
    )
    ..data = data
    ..loading = false;
}

Future<void> _open(
  WidgetTester tester,
  AgendaController controller,
  Size size,
) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Builder(
        builder: (context) => Scaffold(
          body: Center(
            child: ElevatedButton(
              key: const Key('open-customer-account'),
              onPressed: () => showCustomerAccountDialog(
                context,
                controller: controller,
                customer: controller.data.customers.single,
              ),
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.byKey(const Key('open-customer-account')));
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('customer-account-dialog')), findsOneWidget);
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
