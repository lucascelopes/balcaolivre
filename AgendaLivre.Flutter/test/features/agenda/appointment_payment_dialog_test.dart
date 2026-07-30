import 'dart:convert';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/agenda/appointment_payment_dialog.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../services/fake_http_transport.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('replica a janela WPF de cobrança em 540x470', (tester) async {
    final harness = await _pumpPaymentDialog(tester, const Size(1382, 736));

    expect(
      tester.getSize(find.byKey(const Key('appointment-payment-dialog'))),
      const Size(540, 470),
    );
    expect(find.text('Editar atendimento'), findsOneWidget);
    expect(find.text('Como cobrar?'), findsOneWidget);
    expect(find.text('Serviço'), findsOneWidget);
    expect(find.text('Cliente'), findsOneWidget);
    expect(find.text('Local'), findsOneWidget);
    expect(find.text('A receber'), findsOneWidget);
    expect(find.text('Pix'), findsOneWidget);
    expect(find.text('Dinheiro'), findsWidgets);
    expect(find.text('Débito'), findsNothing);
    expect(find.text('Crédito'), findsNothing);
    expect(find.text('Conta do cliente'), findsWidgets);
    expect(find.text('Enviar débito para a Point'), findsNothing);
    expect(harness.repository.saveCalls, 0);
    expect(tester.takeException(), isNull);
  });

  testWidgets('adiciona o atendimento à conta do cliente pelo seletor', (
    tester,
  ) async {
    final harness = await _pumpPaymentDialog(tester, const Size(1382, 736));

    await tester.drag(find.byType(ListWheelScrollView), const Offset(0, -64));
    await tester.pumpAndSettle();
    await tester.drag(find.byType(ListWheelScrollView), const Offset(0, -64));
    await tester.pumpAndSettle();
    final actionLabel = tester
        .widget<Text>(
          find.descendant(
            of: find.byKey(const Key('appointment-payment-action')),
            matching: find.byType(Text),
          ),
        )
        .data;
    expect(actionLabel, 'Adicionar à conta do cliente');

    await tester.tap(
      find.byKey(const Key('appointment-payment-action')).hitTestable(),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('appointment-payment-dialog')), findsNothing);
    expect(harness.controller.data.customerReceivables, hasLength(1));
    expect(harness.appointment.status, AppointmentStatus.done);
    expect(harness.appointment.paymentMethod, 'Conta do cliente');
    expect(harness.repository.saveCalls, 1);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mantém o fluxo de cobrança acessível no celular', (
    tester,
  ) async {
    await _pumpPaymentDialog(tester, const Size(390, 844));

    final size = tester.getSize(
      find.byKey(const Key('appointment-payment-dialog')),
    );
    expect(size.width, 370);
    expect(size.height, lessThanOrEqualTo(650));
    expect(
      find.byKey(const Key('appointment-payment-action')).hitTestable(),
      findsOne,
    );
    expect(find.text('Como cobrar?'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('registra dinheiro somente após confirmação explícita', (
    tester,
  ) async {
    final harness = await _pumpPaymentDialog(tester, const Size(1382, 736));

    await tester.tap(
      find.byKey(const Key('appointment-payment-option-cash')).hitTestable(),
    );
    await tester.pumpAndSettle();
    await tester.tap(
      find.byKey(const Key('appointment-payment-action')).hitTestable(),
    );
    await tester.pumpAndSettle();

    expect(find.text('Receber em dinheiro'), findsOneWidget);
    expect(harness.appointment.paymentConfirmedAt, isNull);
    await tester.tap(find.byKey(const Key('confirm-cash-payment')));
    await tester.pumpAndSettle();

    expect(harness.appointment.paymentMethod, 'Dinheiro');
    expect(harness.appointment.paymentProvider, 'Manual');
    expect(harness.repository.saveCalls, 1);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'após o pagamento oferece retorno e cria novo agendamento preenchido',
    (tester) async {
      final harness = await _pumpPaymentDialog(tester, const Size(390, 844));

      await tester.tap(
        find.byKey(const Key('appointment-payment-option-cash')).hitTestable(),
      );
      await tester.pumpAndSettle();
      await tester.tap(
        find.byKey(const Key('appointment-payment-action')).hitTestable(),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('confirm-cash-payment')));
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('appointment-payment-rebook-offer')),
        findsOneWidget,
      );
      expect(find.text('Mantenha a recorrência do cliente'), findsOneWidget);

      await tester.tap(find.byKey(const Key('appointment-payment-rebook')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('appointment-dialog')), findsOneWidget);
      expect(find.text('Agendar retorno'), findsOneWidget);
      expect(find.textContaining('Cliente e serviço já preenchidos'), findsOne);

      await tester.tap(
        find.byKey(const Key('appointment-continue')).hitTestable(),
      );
      await tester.pumpAndSettle();
      expect(find.text('Mariana Costa'), findsOneWidget);

      await tester.tap(
        find.byKey(const Key('appointment-continue')).hitTestable(),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('appointment-save')).hitTestable());
      await tester.pumpAndSettle();

      expect(harness.controller.data.appointments, hasLength(2));
      final created = harness.controller.data.appointments.singleWhere(
        (item) => item.id != harness.appointment.id,
      );
      expect(created.customerId, harness.appointment.customerId);
      expect(created.customerName, harness.appointment.customerName);
      expect(created.serviceId, harness.appointment.serviceId);
      expect(created.professionalId, harness.appointment.professionalId);
      expect(created.status, AppointmentStatus.scheduled);
      expect(created.paymentConfirmedAt, isNull);
      expect(created.start.isAfter(DateTime.now()), isTrue);
      expect(tester.takeException(), isNull);
    },
  );

  for (final size in <Size>[const Size(1200, 760), const Size(390, 844)]) {
    testWidgets(
      'PDV cobra serviço e produtos pela Point em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
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
        final harness = await _pumpPaymentDialog(
          tester,
          size,
          mercadoPagoService: service,
          includeProductLines: true,
        );

        await tester.drag(
          find.byType(ListWheelScrollView),
          const Offset(0, -64),
        );
        await tester.pumpAndSettle();
        await tester.drag(
          find.byType(ListWheelScrollView),
          const Offset(0, -64),
        );
        await tester.pumpAndSettle();
        expect(find.text('Enviar débito para a Point'), findsOneWidget);
        await tester.tap(
          find.byKey(const Key('appointment-payment-action')).hitTestable(),
        );
        await tester.pump();
        expect(harness.appointment.paymentConfirmedAt, isNull);
        expect(harness.controller.data.productSales, isEmpty);
        expect(
          find.byKey(const Key('mercado-pago-point-progress')),
          findsOneWidget,
        );
        expect(
          find.byKey(const Key('mercado-pago-terminal-card')),
          findsOneWidget,
        );
        expect(
          find.byKey(const Key('mercado-pago-terminal-image')),
          findsOneWidget,
        );
        expect(find.text('Point Pro 3'), findsOneWidget);
        expect(find.textContaining('Q92-1734055152'), findsOneWidget);

        final chargeBody =
            jsonDecode(
                  transport.requests
                      .firstWhere(
                        (request) => request.uri.path.endsWith('/point/charge'),
                      )
                      .body!,
                )
                as Map<String, dynamic>;
        expect(chargeBody['amount'], '124.90');

        await tester.pump(const Duration(milliseconds: 1400));
        await tester.pumpAndSettle();

        expect(harness.appointment.paymentProvider, 'Mercado Pago');
        expect(harness.appointment.paymentReference, 'P1');
        expect(harness.controller.data.productSales, hasLength(1));
        expect(harness.controller.data.productSales.single.total, 35);
        expect(harness.controller.data.products.single.stockQuantity, 7);
        expect(tester.takeException(), isNull);
      },
    );
  }
}

Future<_PaymentHarness> _pumpPaymentDialog(
  WidgetTester tester,
  Size size, {
  MercadoPagoService? mercadoPagoService,
  bool includeProductLines = false,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final customer = Customer(
    id: 'customer-payment',
    name: 'Mariana Costa',
    phone: '(33) 99999-1111',
  );
  final service = ServiceItem(
    id: 'service-payment',
    segment: 'Salão de beleza',
    name: 'Corte e escova',
    durationMinutes: 60,
    price: 89.90,
    defaultResource: 'Cadeira 2',
  );
  final professional = Professional(
    id: 'professional-payment',
    name: 'Camila',
    segments: <String>['Salão de beleza'],
  );
  final appointment = Appointment(
    id: 'appointment-payment',
    customerId: customer.id,
    customerName: customer.name,
    customerPhone: customer.phone,
    segment: service.segment,
    serviceId: service.id,
    serviceName: service.name,
    professionalId: professional.id,
    professionalName: professional.name,
    resourceName: 'Cadeira 2',
    start: DateTime(2026, 7, 20, 10),
    durationMinutes: 60,
    price: 89.90,
    status: AppointmentStatus.inService,
    productLines: includeProductLines
        ? <AppointmentProductLine>[
            AppointmentProductLine(
              productId: 'product-payment',
              productName: 'Finalizador',
              quantity: 1,
              unitPrice: 35,
            ),
          ]
        : null,
  );
  final data = AgendaData(
    settings: AgendaSettings(
      businessName: 'Studio Mariana',
      businessSegment: 'Salão de beleza',
      onboardingCompleted: true,
      pixKey: 'financeiro@studio.com.br',
      mercadoPagoEnabled: mercadoPagoService != null,
      mercadoPagoConnected: mercadoPagoService != null,
      mercadoPagoDefaultTerminalId: mercadoPagoService == null
          ? ''
          : 'PAX_Q92__Q92-1734055152',
      mercadoPagoDefaultTerminalLabel: mercadoPagoService == null
          ? ''
          : 'Point Pro 3 · Q92-1734055152 (PDV)',
    ),
    customers: <Customer>[customer],
    appointments: <Appointment>[appointment],
    services: <ServiceItem>[service],
    professionals: <Professional>[professional],
    products: includeProductLines
        ? <ProductItem>[
            ProductItem(
              id: 'product-payment',
              name: 'Finalizador',
              price: 35,
              stockQuantity: 8,
            ),
          ]
        : null,
  );
  final repository = _MemoryAgendaRepository(data);
  final controller =
      AgendaController(repository, mercadoPagoService: mercadoPagoService)
        ..data = data
        ..loading = false;

  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('').toThemeData(),
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              onPressed: () => showAppointmentPaymentDialog(
                context,
                controller,
                appointment,
                includeProductLines: includeProductLines,
              ),
              child: const Text('Abrir cobrança'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Abrir cobrança'));
  await tester.pumpAndSettle();
  return _PaymentHarness(controller, repository, appointment);
}

class _PaymentHarness {
  const _PaymentHarness(this.controller, this.repository, this.appointment);

  final AgendaController controller;
  final _MemoryAgendaRepository repository;
  final Appointment appointment;
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
