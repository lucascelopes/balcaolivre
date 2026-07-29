import 'dart:convert';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final referenceDate = DateTime(2026, 7, 14, 9);

  group('AgendaController initialization', () {
    test('loads and normalizes persisted data', () async {
      final repository = _FakeAgendaRepository(
        AgendaData(
          settings: AgendaSettings(
            businessName: '   ',
            businessSegment: 'Centro de Estética',
            workdayStartHour: 22,
            workdayEndHour: 8,
            resources: const [' Mesa 1 ', '', 'Mesa 1', ' Cadeira 1 '],
          ),
        ),
      );
      final controller = AgendaController(repository);
      var notifications = 0;
      controller.addListener(() => notifications++);

      await controller.initialize();

      expect(controller.loading, isFalse);
      expect(controller.loadError, isNull);
      expect(controller.businessName, isNotEmpty);
      expect(controller.data.settings.workdayStartHour, 8);
      expect(controller.data.settings.workdayEndHour, 20);
      expect(controller.data.settings.resources, ['Mesa 1', 'Cadeira 1']);
      expect(repository.loadOrCreateCalls, 1);
      expect(notifications, greaterThanOrEqualTo(2));
    });

    test(
      'exposes repository failures without staying in loading state',
      () async {
        final repository = _FakeAgendaRepository(
          AgendaData(),
          loadError: StateError('boom'),
        );
        final controller = AgendaController(repository);

        await controller.initialize();

        expect(controller.loading, isFalse);
        expect(controller.loadError, contains('boom'));
      },
    );

    test(
      'applies the cloud automatically on startup and later conflicts',
      () async {
        final repository = _FakeSyncAgendaRepository(
          local: _baseData(),
          cloud: AgendaData(
            settings: AgendaSettings(
              businessName: 'Agenda da nuvem',
              businessSegment: 'Centro de Estética',
              onboardingCompleted: true,
            ),
          ),
        );
        final controller = AgendaController(repository);

        await controller.initialize();

        expect(controller.businessName, 'Agenda da nuvem');
        expect(controller.hasSyncConflict, isFalse);
        expect(repository.resolveCloudCalls, 1);

        repository.triggerCloudConflict(
          AgendaData(
            settings: AgendaSettings(
              businessName: 'Atualização do Windows',
              businessSegment: 'Centro de Estética',
              onboardingCompleted: true,
            ),
          ),
        );
        await Future<void>.delayed(Duration.zero);
        await Future<void>.delayed(Duration.zero);

        expect(controller.businessName, 'Atualização do Windows');
        expect(controller.hasSyncConflict, isFalse);
        expect(repository.resolveCloudCalls, 2);
      },
    );
  });

  group('appointment CRUD', () {
    test('creates, updates and deletes an appointment', () async {
      final service = _service();
      final repository = _FakeAgendaRepository(_baseData(services: [service]));
      final controller = await _initializedController(repository);
      final appointment = Appointment(
        id: 'appointment-1',
        segment: 'Centro de Estética',
        customerName: 'Ana Lima',
        customerPhone: '(33) 99999-1111',
        customerProfile: 'Francesinha',
        serviceId: service.id,
        serviceName: service.name,
        professionalId: 'professional-1',
        professionalName: 'Manicure 1',
        resourceName: 'Mesa 1',
        start: referenceDate,
        durationMinutes: 1,
        price: -10,
      );

      final createError = await controller.saveAppointment(appointment);

      expect(createError, isNull);
      expect(controller.data.appointments, hasLength(1));
      expect(appointment.durationMinutes, 5);
      expect(appointment.price, 0);
      expect(controller.data.customers, hasLength(1));
      expect(controller.data.customers.single.name, 'Ana Lima');
      expect(repository.saveCalls, 1);

      appointment
        ..customerName = 'Ana Souza'
        ..customerProfile = 'Esmalte vermelho'
        ..durationMinutes = 45
        ..price = 55;
      final updateError = await controller.saveAppointment(appointment);

      expect(updateError, isNull);
      expect(controller.data.appointments, hasLength(1));
      expect(controller.data.appointments.single.customerName, 'Ana Souza');
      expect(controller.data.customers, hasLength(1));
      expect(controller.data.customers.single.profile, 'Esmalte vermelho');
      expect(repository.saveCalls, 2);

      await controller.deleteAppointment(appointment.id);

      expect(controller.data.appointments, isEmpty);
      expect(repository.saveCalls, 3);
      expect(repository.savedData?.appointments, isEmpty);
    });
  });

  group('appointment conflict detection', () {
    test(
      'uses preparation and buffer windows for the same professional',
      () async {
        final service = _service(preparationMinutes: 10, bufferMinutes: 15);
        final existing = Appointment(
          id: 'existing',
          customerName: 'Cliente existente',
          serviceId: service.id,
          serviceName: service.name,
          professionalId: 'professional-1',
          professionalName: 'Manicure 1',
          resourceName: 'Mesa 1',
          start: referenceDate,
          durationMinutes: 30,
        );
        final repository = _FakeAgendaRepository(
          _baseData(services: [service], appointments: [existing]),
        );
        final controller = await _initializedController(repository);
        final candidate = Appointment(
          id: 'candidate',
          customerName: 'Novo cliente',
          serviceId: service.id,
          serviceName: service.name,
          professionalId: 'professional-1',
          professionalName: 'Manicure 1',
          resourceName: 'Mesa 2',
          start: referenceDate.add(const Duration(minutes: 50)),
          durationMinutes: 30,
        );

        final error = await controller.saveAppointment(candidate);

        expect(error, isNotNull);
        expect(error, contains('Cliente existente'));
        expect(controller.data.appointments, hasLength(1));
        expect(repository.saveCalls, 0);
      },
    );

    test('ignores cancelled appointments when checking conflicts', () async {
      final service = _service();
      final cancelled = Appointment(
        id: 'cancelled',
        customerName: 'Cancelado',
        serviceId: service.id,
        professionalId: 'professional-1',
        resourceName: 'Mesa 1',
        start: referenceDate,
        durationMinutes: 45,
        status: AppointmentStatus.cancelled,
      );
      final repository = _FakeAgendaRepository(
        _baseData(services: [service], appointments: [cancelled]),
      );
      final controller = await _initializedController(repository);
      final candidate = Appointment(
        id: 'candidate',
        customerName: 'Novo cliente',
        serviceId: service.id,
        professionalId: 'professional-1',
        resourceName: 'Mesa 1',
        start: referenceDate,
        durationMinutes: 45,
      );

      final error = await controller.saveAppointment(candidate);

      expect(error, isNull);
      expect(controller.data.appointments, hasLength(2));
      expect(repository.saveCalls, 1);
    });
  });

  group('business window validation', () {
    test(
      'matches WPF workdays, operating hours and configured break',
      () async {
        final repository = _FakeAgendaRepository(_baseData());
        final controller = await _initializedController(repository);

        expect(
          controller.validateBusinessWindow(
            DateTime(2026, 7, 19, 10),
            DateTime(2026, 7, 19, 11),
          ),
          'O estabelecimento não atende no dia selecionado.',
        );
        expect(
          controller.validateBusinessWindow(
            DateTime(2026, 7, 14, 8, 30),
            DateTime(2026, 7, 14, 9, 30),
          ),
          contains('09:00 até 20:00'),
        );
        expect(
          controller.validateBusinessWindow(
            DateTime(2026, 7, 14, 12, 30),
            DateTime(2026, 7, 14, 13, 15),
          ),
          contains('12:00 às 13:00'),
        );
        expect(
          controller.validateBusinessWindow(
            DateTime(2026, 7, 14, 13),
            DateTime(2026, 7, 14, 14),
          ),
          isNull,
        );
      },
    );

    test(
      'permite editar dados de um agendamento antigo fora do horário atual',
      () async {
        final legacy = Appointment(
          id: 'legacy-closed-day',
          customerName: 'Cliente antigo',
          serviceId: 'service-1',
          serviceName: 'Manicure',
          professionalId: 'professional-1',
          professionalName: 'Manicure 1',
          resourceName: 'Mesa 1',
          start: DateTime(2026, 7, 19, 10),
          durationMinutes: 45,
          price: 55,
        );
        final repository = _FakeAgendaRepository(
          _baseData(services: [_service()], appointments: [legacy]),
        );
        final controller = await _initializedController(repository);
        final edited = Appointment.fromJson(
          controller.data.appointments.single.toJson(),
        )..notes = 'Observação atualizada';

        final error = await controller.saveAppointment(edited);

        expect(error, isNull);
        expect(
          controller.data.appointments.single.notes,
          'Observação atualizada',
        );
        expect(repository.saveCalls, 1);
      },
    );

    test('salva uma exceção nova somente com o aviso auditado', () async {
      final repository = _FakeAgendaRepository(
        _baseData(services: [_service()]),
      );
      final controller = await _initializedController(repository);
      final exceptional = Appointment(
        id: 'exceptional-closed-day',
        customerName: 'Cliente avisado',
        serviceId: 'service-1',
        serviceName: 'Manicure',
        professionalId: 'professional-1',
        professionalName: 'Manicure 1',
        resourceName: 'Mesa 1',
        start: DateTime(2026, 7, 19, 10),
        durationMinutes: 45,
        price: 55,
      );

      expect(await controller.saveAppointment(exceptional), isNotNull);
      exceptional
        ..scheduleExceptionAcknowledged = true
        ..scheduleExceptionReason =
            'O estabelecimento está fechado no dia selecionado.'
        ..scheduleExceptionAssistantSource = 'local-rules'
        ..scheduleExceptionAcknowledgedAt = DateTime(2026, 7, 18, 9);

      expect(await controller.saveAppointment(exceptional), isNull);
      expect(
        controller.data.appointments.single.scheduleExceptionAcknowledged,
        isTrue,
      );
      expect(repository.saveCalls, 1);
    });
  });

  group('appointment status transitions', () {
    test(
      'accepts the operational sequence and rejects invalid jumps',
      () async {
        final appointment = Appointment(
          id: 'appointment-1',
          customerName: 'Ana',
          start: referenceDate,
        );
        final repository = _FakeAgendaRepository(
          _baseData(appointments: [appointment]),
        );
        final controller = await _initializedController(repository);
        final stored = controller.data.appointments.single;

        expect(
          await controller.setAppointmentStatus(stored, AppointmentStatus.done),
          isNotNull,
        );
        expect(stored.status, AppointmentStatus.scheduled);
        expect(repository.saveCalls, 0);

        expect(
          await controller.setAppointmentStatus(
            stored,
            AppointmentStatus.confirmed,
          ),
          isNull,
        );
        expect(
          await controller.setAppointmentStatus(
            stored,
            AppointmentStatus.waiting,
          ),
          isNull,
        );
        expect(
          await controller.setAppointmentStatus(
            stored,
            AppointmentStatus.inService,
          ),
          isNull,
        );
        expect(
          await controller.setAppointmentStatus(stored, AppointmentStatus.done),
          isNull,
        );
        expect(stored.status, AppointmentStatus.done);
        expect(repository.saveCalls, 4);

        expect(
          await controller.setAppointmentStatus(
            stored,
            AppointmentStatus.cancelled,
          ),
          isNotNull,
        );
        expect(stored.status, AppointmentStatus.done);
        expect(repository.saveCalls, 4);
      },
    );
  });

  group('PDV appointment operation', () {
    test('starts, pauses, resumes and finishes the service timer', () async {
      final appointment = Appointment(
        id: 'appointment-timer',
        customerName: 'Ana',
        serviceName: 'Manicure',
        start: referenceDate,
        price: 55,
        status: AppointmentStatus.confirmed,
      );
      final repository = _FakeAgendaRepository(
        _baseData(appointments: [appointment]),
      );
      final controller = await _initializedController(repository);
      final stored = controller.data.appointments.single;

      expect(
        await controller.toggleAppointmentServiceTimer(
          stored,
          now: DateTime(2026, 7, 14, 9),
        ),
        isNull,
      );
      expect(stored.status, AppointmentStatus.inService);
      expect(stored.serviceStartedAt, DateTime(2026, 7, 14, 9));
      expect(
        controller.appointmentServiceElapsed(
          stored,
          now: DateTime(2026, 7, 14, 9, 5),
        ),
        const Duration(minutes: 5),
      );

      expect(
        await controller.toggleAppointmentServiceTimer(
          stored,
          now: DateTime(2026, 7, 14, 9, 5),
        ),
        isNull,
      );
      expect(stored.serviceTimerPaused, isTrue);
      expect(stored.serviceElapsedSeconds, 300);

      expect(
        await controller.toggleAppointmentServiceTimer(
          stored,
          now: DateTime(2026, 7, 14, 9, 6),
        ),
        isNull,
      );
      expect(stored.serviceTimerPaused, isFalse);
      expect(
        await controller.finishAppointmentService(
          stored,
          now: DateTime(2026, 7, 14, 9, 8),
        ),
        isNull,
      );
      expect(stored.status, AppointmentStatus.done);
      expect(stored.serviceStartedAt, isNull);
      expect(stored.serviceElapsedSeconds, 420);
      expect(repository.saveCalls, 4);
    });

    test(
      'saves service and product lines and calculates the PDV total',
      () async {
        final appointment = Appointment(
          id: 'appointment-items',
          customerName: 'Ana',
          serviceName: 'Manicure',
          start: referenceDate,
          price: 55,
          status: AppointmentStatus.inService,
        );
        final data = _baseData(appointments: [appointment])
          ..products.add(
            ProductItem(
              id: 'product-1',
              name: 'Óleo nutritivo',
              price: 12,
              stockQuantity: 3,
            ),
          );
        final repository = _FakeAgendaRepository(data);
        final controller = await _initializedController(repository);

        final error = await controller.savePdvAppointmentLines(
          controller.data.appointments.single,
          serviceLines: [
            AppointmentServiceLine(
              serviceId: 'service-1',
              serviceName: 'Manicure premium',
              segment: 'Centro de Estética',
              quantity: 2,
              durationMinutes: 25,
              unitPrice: 35,
            ),
          ],
          productLines: [
            AppointmentProductLine(
              productId: 'product-1',
              productName: 'Óleo nutritivo',
              quantity: 2,
              unitPrice: 12,
            ),
          ],
        );

        expect(error, isNull);
        final stored = controller.data.appointments.single;
        expect(stored.serviceName, 'Manicure premium (2x)');
        expect(stored.durationMinutes, 50);
        expect(stored.price, 70);
        expect(controller.pdvAppointmentTotal(stored), 94);
        expect(repository.saveCalls, 1);
      },
    );

    test('records a paid product sale and stock reduction only once', () async {
      final appointment = Appointment(
        id: 'appointment-pdv-paid',
        customerName: 'Ana',
        serviceName: 'Manicure premium',
        start: referenceDate,
        price: 70,
        status: AppointmentStatus.inService,
        productLines: [
          AppointmentProductLine(
            productId: 'product-1',
            productName: 'Óleo nutritivo',
            quantity: 2,
            unitPrice: 12,
          ),
        ],
      );
      final data = _baseData(appointments: [appointment])
        ..products.add(
          ProductItem(
            id: 'product-1',
            name: 'Óleo nutritivo',
            price: 12,
            stockQuantity: 3,
          ),
        );
      final repository = _FakeAgendaRepository(data);
      final controller = await _initializedController(repository);
      final stored = controller.data.appointments.single;
      final paidAt = DateTime(2026, 7, 14, 10, 15);

      final error = await controller.confirmPdvAppointmentPayment(
        stored,
        paymentMethod: 'Débito na Point',
        paymentProvider: 'Mercado Pago',
        paymentReference: 'payment-pdv-123',
        paymentStatus: 'approved',
        confirmedAt: paidAt,
      );

      expect(error, isNull);
      expect(stored.status, AppointmentStatus.done);
      expect(stored.paymentConfirmedAt, paidAt);
      expect(stored.productSalesRecordedAt, paidAt);
      expect(controller.data.productSales, hasLength(1));
      expect(controller.data.productSales.single.total, 24);
      expect(
        controller.data.productSales.single.paymentReference,
        'payment-pdv-123',
      );
      expect(controller.data.products.single.stockQuantity, 1);
      expect(repository.saveCalls, 1);

      final duplicate = await controller.confirmPdvAppointmentPayment(
        stored,
        paymentMethod: 'Pix',
      );
      expect(duplicate, contains('já possui'));
      expect(controller.data.productSales, hasLength(1));
      expect(controller.data.products.single.stockQuantity, 1);
      expect(repository.saveCalls, 1);
    });
  });

  group('appointment charging', () {
    test(
      'confirms payment once and persists the complete WPF contract',
      () async {
        final appointment = Appointment(
          id: 'appointment-paid',
          customerName: 'Ana',
          serviceName: 'Manicure',
          start: referenceDate,
          price: 55,
          status: AppointmentStatus.inService,
        );
        final repository = _FakeAgendaRepository(
          _baseData(appointments: [appointment]),
        );
        final controller = await _initializedController(repository);
        final stored = controller.data.appointments.single;
        final paidAt = DateTime(2026, 7, 14, 10, 15);

        final error = await controller.confirmAppointmentPayment(
          stored,
          paymentMethod: 'Débito na Point',
          paymentProvider: 'Mercado Pago',
          paymentReference: 'payment-123',
          paymentStatus: 'approved',
          confirmedAt: paidAt,
        );

        expect(error, isNull);
        expect(stored.status, AppointmentStatus.done);
        expect(stored.paymentConfirmedAt, paidAt);
        expect(stored.paymentMethod, 'Débito na Point');
        expect(stored.paymentProvider, 'Mercado Pago');
        expect(stored.paymentReference, 'payment-123');
        expect(stored.paymentStatus, 'approved');
        expect(repository.saveCalls, 1);

        final duplicate = await controller.confirmAppointmentPayment(
          stored,
          paymentMethod: 'Pix',
        );
        expect(duplicate, contains('já possui'));
        expect(repository.saveCalls, 1);
      },
    );

    test('adds a linked appointment to the unique customer account', () async {
      final appointment = Appointment(
        id: 'appointment-account',
        customerName: 'Ana Lima',
        customerPhone: '+55 (33) 99999-1111',
        serviceName: 'Corte',
        start: referenceDate,
        price: 80,
        status: AppointmentStatus.waiting,
      );
      final data = _baseData(appointments: [appointment])
        ..customers.add(
          Customer(
            id: 'customer-1',
            name: 'Ana Lima',
            phone: '(33) 99999-1111',
          ),
        );
      final repository = _FakeAgendaRepository(data);
      final controller = await _initializedController(repository);
      final stored = controller.data.appointments.single;
      final openedAt = DateTime(2026, 7, 14, 10, 20);

      final error = await controller.addAppointmentToCustomerAccount(
        stored,
        openedAt: openedAt,
      );

      expect(error, isNull);
      expect(stored.customerId, 'customer-1');
      expect(stored.status, AppointmentStatus.done);
      expect(stored.paymentConfirmedAt, isNull);
      expect(stored.paymentMethod, 'Conta do cliente');
      expect(stored.paymentProvider, 'customer_account');
      expect(stored.paymentStatus, 'pending');
      expect(controller.data.customerReceivables, hasLength(1));
      final receivable = controller.data.customerReceivables.single;
      expect(receivable.customerId, 'customer-1');
      expect(receivable.appointmentId, stored.id);
      expect(receivable.originalValue, 80);
      expect(receivable.remainingValue, 80);
      expect(receivable.openedAt, openedAt);
      expect(stored.paymentReference, receivable.id);
      expect(repository.saveCalls, 1);

      final duplicate = await controller.addAppointmentToCustomerAccount(
        stored,
      );
      expect(duplicate, contains('já possui'));
      expect(controller.data.customerReceivables, hasLength(1));
      expect(repository.saveCalls, 1);
    });

    test(
      'settles all selected customer receivables without duplicate revenue',
      () async {
        final paidAt = DateTime(2026, 7, 15, 14, 30);
        final appointment = Appointment(
          id: 'appointment-receivable',
          customerId: 'customer-1',
          customerName: 'Ana Lima',
          serviceName: 'Corte',
          start: referenceDate,
          price: 80,
          status: AppointmentStatus.done,
          paymentMethod: 'Conta do cliente',
          paymentProvider: 'customer_account',
          paymentStatus: 'pending',
        );
        final receivable = CustomerReceivable(
          id: 'receivable-1',
          customerId: 'customer-1',
          customerName: 'Ana Lima',
          appointmentId: appointment.id,
          description: 'Corte',
          originalValue: 80,
          remainingValue: 80,
        );
        appointment.paymentReference = receivable.id;
        final data = _baseData(appointments: [appointment])
          ..customers.add(Customer(id: 'customer-1', name: 'Ana Lima'))
          ..customerReceivables.add(receivable);
        final repository = _FakeAgendaRepository(data);
        final controller = await _initializedController(repository);

        final open = controller.openCustomerReceivables(
          customerId: 'customer-1',
        );
        expect(open.map((item) => item.id), ['receivable-1']);

        final error = await controller.settleCustomerReceivables(
          open.map((item) => item.id),
          paymentMethod: 'Crédito na Point',
          paymentProvider: 'Mercado Pago',
          paymentReference: 'payment-456',
          paymentStatus: 'approved',
          paidAt: paidAt,
        );

        expect(error, isNull);
        final settled = controller.data.customerReceivables.single;
        expect(settled.status, 'paid');
        expect(settled.remainingValue, 0);
        expect(settled.paidAt, paidAt);
        expect(settled.paymentProvider, 'Mercado Pago');
        expect(settled.paymentReference, 'payment-456');
        final storedAppointment = controller.data.appointments.single;
        expect(storedAppointment.paymentConfirmedAt, paidAt);
        expect(storedAppointment.paymentReference, 'payment-456');
        expect(
          controller.revenueBetween(DateTime(2026, 7, 1), DateTime(2026, 8, 1)),
          80,
        );
        expect(repository.saveCalls, 1);

        final duplicate = await controller.settleCustomerReceivables([
          'receivable-1',
        ], paymentMethod: 'Dinheiro');
        expect(duplicate, contains('já foi quitado'));
        expect(repository.saveCalls, 1);
      },
    );

    test(
      'rejects charging closed appointments and ambiguous customers',
      () async {
        final cancelled = Appointment(
          id: 'cancelled-charge',
          customerName: 'Ana',
          start: referenceDate,
          status: AppointmentStatus.cancelled,
        );
        final ambiguous = Appointment(
          id: 'ambiguous-account',
          customerName: 'Maria',
          start: referenceDate.add(const Duration(hours: 1)),
          price: 30,
        );
        final data = _baseData(appointments: [cancelled, ambiguous])
          ..customers.addAll([
            Customer(id: 'maria-1', name: 'Maria'),
            Customer(id: 'maria-2', name: 'Maria'),
          ]);
        final repository = _FakeAgendaRepository(data);
        final controller = await _initializedController(repository);

        expect(
          await controller.confirmAppointmentPayment(
            controller.data.appointments.first,
            paymentMethod: 'Dinheiro',
          ),
          contains('encerrado sem cobrança'),
        );
        expect(
          await controller.addAppointmentToCustomerAccount(
            controller.data.appointments.last,
          ),
          contains('nome e telefone únicos'),
        );
        expect(repository.saveCalls, 0);
      },
    );
  });

  group('financial totals', () {
    test(
      'calculates revenue and expenses inside the requested period',
      () async {
        final start = DateTime(2026, 7, 1);
        final end = DateTime(2026, 8, 1);
        final data =
            _baseData(
                appointments: [
                  Appointment(
                    id: 'done-inside',
                    start: DateTime(2026, 7, 10),
                    status: AppointmentStatus.done,
                    price: 100,
                    paymentConfirmedAt: DateTime(2026, 7, 10, 12),
                  ),
                  Appointment(
                    id: 'scheduled-inside',
                    start: DateTime(2026, 7, 11),
                    status: AppointmentStatus.scheduled,
                    price: 500,
                  ),
                  Appointment(
                    id: 'done-outside',
                    start: DateTime(2026, 8, 2),
                    status: AppointmentStatus.done,
                    price: 1000,
                    paymentConfirmedAt: DateTime(2026, 8, 2, 12),
                  ),
                ],
              )
              ..productSales.add(
                ProductSale(
                  id: 'sale-inside',
                  quantity: 2,
                  unitPrice: 20,
                  discount: 5,
                  soldAt: DateTime(2026, 7, 12),
                ),
              )
              ..manualPayments.add(
                ManualPayment(
                  id: 'payment-inside',
                  value: 50,
                  paidAt: DateTime(2026, 7, 13),
                ),
              )
              ..expenses.addAll([
                ExpenseItem(
                  id: 'expense-inside',
                  value: 30,
                  date: DateTime(2026, 7, 14),
                ),
                ExpenseItem(
                  id: 'expense-outside',
                  value: 300,
                  date: DateTime(2026, 8, 2),
                ),
              ]);
        final controller = await _initializedController(
          _FakeAgendaRepository(data),
        );

        expect(controller.revenueBetween(start, end), 185);
        expect(controller.expensesBetween(start, end), 30);
      },
    );
  });

  group('import and export', () {
    test(
      'round-trips controller data and persists the imported agenda',
      () async {
        final sourceRepository = _FakeAgendaRepository(
          _baseData(
            services: [_service()],
            appointments: [
              Appointment(
                id: 'appointment-1',
                customerName: 'Ana',
                start: referenceDate,
                status: AppointmentStatus.confirmed,
              ),
            ],
          ),
        );
        final source = await _initializedController(sourceRepository);
        final exported = source.exportJson();
        final targetRepository = _FakeAgendaRepository(AgendaData());
        final target = await _initializedController(targetRepository);

        final importError = await target.importJson(exported);

        expect(importError, isNull);
        expect(target.data.settings.businessName, 'Lucas Barbearia');
        expect(target.data.services.single.name, 'Manicure');
        expect(
          target.data.appointments.single.status,
          AppointmentStatus.confirmed,
        );
        expect(targetRepository.saveCalls, 1);
        expect(targetRepository.savedData?.appointments, hasLength(1));
      },
    );

    test('rejects malformed or non-object JSON', () async {
      final repository = _FakeAgendaRepository(_baseData());
      final controller = await _initializedController(repository);

      expect(await controller.importJson('{invalid'), isNotNull);
      expect(await controller.importJson('[]'), isNotNull);
      expect(controller.data.settings.businessName, 'Lucas Barbearia');
      expect(repository.saveCalls, 0);
    });
  });

  group('onboarding', () {
    test('mantém o e-mail autenticado em onboarding e importações', () async {
      final repository = _FakeAgendaRepository(
        AgendaData(
          settings: AgendaSettings(
            accountEmail: 'fixture@example.com',
            businessName: '',
            businessSegment: '',
            onboardingCompleted: false,
          ),
        ),
      );
      final controller = AgendaController(
        repository,
        onLogout: () async {},
        authenticatedEmail: 'conta.real@example.com',
      );
      await controller.initialize();

      expect(controller.data.settings.accountEmail, 'conta.real@example.com');
      await controller.completeOnboarding(
        accountName: 'Isabela',
        phone: '(33) 99131-4125',
        email: 'outra@example.com',
        businessName: 'Minha Agenda',
        segment: 'Salão de Beleza',
        themeId: '',
        teamSize: '1 profissional',
        objective: 'Organizar agenda',
        postalCode: '',
        neighborhood: '',
        street: '',
        number: '',
        complement: '',
      );
      expect(controller.data.settings.accountEmail, 'conta.real@example.com');
      expect(
        repository.savedData?.settings.accountEmail,
        'conta.real@example.com',
      );

      final imported = AgendaData.fromJson(controller.data.toJson());
      imported.settings.accountEmail = 'importado@example.com';
      expect(
        await controller.importJson(jsonEncode(imported.toJson())),
        isNull,
      );
      expect(controller.data.settings.accountEmail, 'conta.real@example.com');
      expect(
        repository.savedData?.settings.accountEmail,
        'conta.real@example.com',
      );
    });

    test('completes and restarts the guided setup', () async {
      final repository = _FakeAgendaRepository(
        AgendaData(
          settings: AgendaSettings(
            businessName: '',
            businessSegment: '',
            onboardingCompleted: false,
          ),
        ),
      );
      final controller = await _initializedController(repository);
      final startedAt = DateTime.now();

      expect(controller.needsOnboarding, isTrue);

      await controller.completeOnboarding(
        accountName: '  Lucas Cesar Lopes  ',
        phone: '  (33) 99800-7983 ',
        email: ' lucas@example.com ',
        businessName: ' Lucas Barbearia ',
        segment: ' Centro de Estética ',
        themeId: 'aesthetic-coral',
        teamSize: '2 profissionais',
        objective: 'Implementar agendamento online',
        postalCode: '35032-390',
        neighborhood: ' Lourdes ',
        street: ' Rua Piracicaba ',
        number: ' 10 ',
        complement: ' Sala 2 ',
      );

      final settings = controller.data.settings;
      expect(controller.needsOnboarding, isFalse);
      expect(settings.accountFullName, 'Lucas Cesar Lopes');
      expect(settings.businessName, 'Lucas Barbearia');
      expect(settings.businessSegment, 'Centro de Estética');
      expect(settings.themeId, 'aesthetic-coral');
      expect(
        settings.businessAddress,
        'Rua Piracicaba, 10 | Lourdes | Sala 2 | CEP 35032-390',
      );
      expect(settings.onboardingCompleted, isTrue);
      expect(settings.accountCreatedAt.isBefore(startedAt), isFalse);
      expect(repository.saveCalls, 1);

      await controller.restartOnboarding();

      expect(controller.needsOnboarding, isTrue);
      expect(settings.onboardingCompleted, isFalse);
      expect(settings.businessName, 'Lucas Barbearia');
      expect(repository.saveCalls, 2);
    });

    test('exits to onboarding without deleting operational data', () async {
      final seeded = AgendaSeedData.salon(referenceDate: referenceDate);
      seeded.settings
        ..mercadoPagoEnabled = true
        ..mercadoPagoConnected = true
        ..mercadoPagoLicenseKey = 'license'
        ..mercadoPagoSellerUserId = 'seller'
        ..mercadoPagoDefaultTerminalId = 'point-1'
        ..mercadoPagoDefaultTerminalLabel = 'Point 1'
        ..mercadoPagoLastError = 'old error'
        ..mercadoPagoLastSyncAt = referenceDate
        ..whatsAppLinked = true;
      final repository = _FakeAgendaRepository(seeded);
      final controller = await _initializedController(repository);
      controller
        ..navigate(AgendaPage.settings)
        ..setSearch('Maria');
      final operationalData = Map<String, dynamic>.from(
        controller.data.toJson(),
      )..remove('Settings');

      await controller.exitCurrentSystem();

      final persistedOperationalData = Map<String, dynamic>.from(
        controller.data.toJson(),
      )..remove('Settings');
      final settings = controller.data.settings;
      expect(persistedOperationalData, operationalData);
      expect(controller.needsOnboarding, isTrue);
      expect(controller.page, AgendaPage.home);
      expect(controller.agendaMode, AgendaViewMode.board);
      expect(controller.searchQuery, isEmpty);
      expect(settings.businessName, 'Balcão Livre');
      expect(settings.businessSegment, isEmpty);
      expect(settings.accountFullName, isEmpty);
      expect(settings.businessAddress, isEmpty);
      expect(settings.resources, isEmpty);
      expect(settings.workdayStartHour, 8);
      expect(settings.workdayEndHour, 20);
      expect(settings.themeId, 'aesthetic-coral');
      expect(settings.whatsAppLinked, isTrue);
      expect(settings.mercadoPagoEnabled, isFalse);
      expect(settings.mercadoPagoConnected, isFalse);
      expect(settings.mercadoPagoLicenseKey, isEmpty);
      expect(settings.mercadoPagoDefaultTerminalId, isEmpty);
      expect(settings.onboardingCompleted, isFalse);
      expect(repository.saveCalls, 1);
      expect(repository.clearCalls, 0);
    });
  });

  group('professional access policy', () {
    test('own agenda account only opens agenda and support', () {
      final controller = AgendaController(
        _FakeAgendaRepository(_baseData()),
        professionalId: 'professional-1',
        permissionScope: 'own_agenda',
      )..page = AgendaPage.agenda;

      expect(controller.canAccessPage(AgendaPage.agenda), isTrue);
      expect(controller.canAccessPage(AgendaPage.support), isTrue);
      expect(controller.canAccessPage(AgendaPage.establishment), isFalse);
      expect(controller.canAccessPage(AgendaPage.finance), isFalse);

      controller.navigate(AgendaPage.finance);
      expect(controller.page, AgendaPage.agenda);
    });

    test('agenda_clients also opens establishment and manager opens all', () {
      final clients = AgendaController(
        _FakeAgendaRepository(_baseData()),
        professionalId: 'professional-1',
        permissionScope: 'agenda_clients',
      );
      final manager = AgendaController(
        _FakeAgendaRepository(_baseData()),
        professionalId: 'professional-2',
        permissionScope: 'manager',
      );

      expect(clients.canAccessPage(AgendaPage.establishment), isTrue);
      expect(clients.canAccessPage(AgendaPage.reports), isFalse);
      expect(AgendaPage.values.every(manager.canAccessPage), isTrue);
    });
  });

  group('PDV cash session parity', () {
    test(
      'opens, links transactions and closes with the WPF cash formula',
      () async {
        final day = DateTime(2026, 7, 14);
        final session = CashSession(
          id: 'cash-1',
          operatorName: 'Lucas',
          openingBalance: 100,
          openedAt: day.add(const Duration(hours: 8)),
        );
        final repository = _FakeAgendaRepository(
          AgendaData(
            settings: AgendaSettings(
              accountFullName: 'Lucas',
              businessName: 'Lucas Barbearia',
              businessSegment: 'Barbearia',
            ),
            cashSessions: [session],
            appointments: [
              Appointment(
                id: 'paid-service',
                customerName: 'Ana',
                serviceName: 'Corte',
                start: day.add(const Duration(hours: 9)),
                price: 80,
                status: AppointmentStatus.done,
                paymentConfirmedAt: day.add(
                  const Duration(hours: 9, minutes: 45),
                ),
                paymentMethod: 'Dinheiro',
                cashSessionId: session.id,
                serviceElapsedSeconds: 2400,
              ),
              Appointment(
                id: 'cancelled-service',
                customerName: 'Bia',
                start: day.add(const Duration(hours: 11)),
                status: AppointmentStatus.cancelled,
              ),
            ],
            productSales: [
              ProductSale(
                productName: 'Pomada',
                quantity: 1,
                unitPrice: 50,
                paymentMethod: 'Pix',
                cashSessionId: session.id,
                soldAt: day.add(const Duration(hours: 10)),
              ),
            ],
            manualPayments: [
              ManualPayment(
                description: 'Reforço',
                category: 'Ajuste',
                paymentMethod: 'Dinheiro',
                cashSessionId: session.id,
                value: 20,
                paidAt: day.add(const Duration(hours: 12)),
              ),
            ],
            expenses: [
              ExpenseItem(
                description: 'Sangria',
                paymentMethod: 'Dinheiro',
                cashSessionId: session.id,
                value: 30,
                date: day.add(const Duration(hours: 13)),
              ),
            ],
          ),
        );
        final controller = await _initializedController(repository);
        final current = controller.openCashSessionForDay(day)!;

        final snapshot = controller.buildPdvCashClosingSnapshot(
          current,
          reference: day.add(const Duration(hours: 18)),
        );

        expect(snapshot.appointmentCount, 2);
        expect(snapshot.completedCount, 1);
        expect(snapshot.cancelledCount, 1);
        expect(snapshot.totalSales, 130);
        expect(snapshot.cashSales, 80);
        expect(snapshot.pixSales, 50);
        expect(snapshot.cashEntries, 20);
        expect(snapshot.cashWithdrawals, 30);
        expect(snapshot.expectedBalance, 170);

        await controller.closeCashSession(
          current,
          closingBalance: 165,
          notes: 'Faltaram cinco reais',
          closedAt: day.add(const Duration(hours: 18)),
        );

        expect(current.isOpen, isFalse);
        expect(current.expectedClosingBalance, 170);
        expect(current.closingDifference, -5);
        expect(current.totalSales, 130);
        expect(current.notes, 'Faltaram cinco reais');
        expect(repository.saveCalls, 1);
      },
    );

    test('new PDV movements inherit the open cash session', () async {
      final day = DateTime(2026, 7, 14, 8);
      final repository = _FakeAgendaRepository(
        AgendaData(
          settings: AgendaSettings(
            accountFullName: 'Lucas',
            businessName: 'Lucas Barbearia',
            businessSegment: 'Barbearia',
          ),
          products: [
            ProductItem(
              id: 'product-1',
              name: 'Pomada',
              price: 25,
              stockQuantity: 10,
            ),
          ],
        ),
      );
      final controller = await _initializedController(repository);
      final session = await controller.openCashSession(
        openingBalance: 50,
        openedAt: day,
      );

      final sale = ProductSale(
        productId: 'product-1',
        quantity: 1,
        paymentMethod: 'Dinheiro',
        soldAt: day.add(const Duration(hours: 1)),
      );
      final payment = ManualPayment(
        description: 'Entrada',
        paymentMethod: 'Pix',
        value: 15,
        paidAt: day.add(const Duration(hours: 2)),
      );
      final expense = ExpenseItem(
        description: 'Sangria',
        paymentMethod: 'Dinheiro',
        value: 10,
        date: day.add(const Duration(hours: 3)),
      );

      expect(await controller.registerProductSale(sale), isNull);
      await controller.addPayment(payment);
      await controller.addExpense(expense);

      expect(sale.cashSessionId, session.id);
      expect(payment.cashSessionId, session.id);
      expect(expense.cashSessionId, session.id);
    });
  });
}

AgendaData _baseData({
  List<ServiceItem>? services,
  List<Appointment>? appointments,
}) {
  return AgendaData(
    settings: AgendaSettings(
      accountFullName: 'Lucas Cesar Lopes',
      businessName: 'Lucas Barbearia',
      businessSegment: 'Centro de Estética',
      onboardingCompleted: true,
      workdayStartHour: 9,
      workdayEndHour: 20,
      resources: const ['Mesa 1', 'Mesa 2'],
    ),
    services: services,
    appointments: appointments,
  );
}

ServiceItem _service({int preparationMinutes = 0, int bufferMinutes = 0}) {
  return ServiceItem(
    id: 'service-1',
    segment: 'Centro de Estética',
    name: 'Manicure',
    durationMinutes: 45,
    preparationMinutes: preparationMinutes,
    bufferMinutes: bufferMinutes,
    price: 55,
    defaultResource: 'Mesa 1',
  );
}

Future<AgendaController> _initializedController(
  _FakeAgendaRepository repository,
) async {
  final controller = AgendaController(repository);
  await controller.initialize();
  repository.saveCalls = 0;
  return controller;
}

class _FakeAgendaRepository implements AgendaRepository {
  _FakeAgendaRepository(AgendaData initial, {this.loadError})
    : _stored = _clone(initial);

  AgendaData? _stored;
  final Object? loadError;
  AgendaData? savedData;
  int loadOrCreateCalls = 0;
  int saveCalls = 0;
  int clearCalls = 0;

  @override
  Future<void> clear() async {
    clearCalls++;
    _stored = null;
    savedData = null;
  }

  @override
  Future<bool> hasData() async => _stored != null;

  @override
  Future<AgendaData?> load() async => _stored == null ? null : _clone(_stored!);

  @override
  Future<AgendaData> loadOrCreate() async {
    loadOrCreateCalls++;
    if (loadError != null) throw loadError!;
    _stored ??= AgendaData();
    return _clone(_stored!);
  }

  @override
  Future<void> save(AgendaData data) async {
    saveCalls++;
    savedData = _clone(data);
    _stored = _clone(data);
  }

  static AgendaData _clone(AgendaData data) =>
      AgendaData.fromJson(data.toJson());
}

class _FakeSyncAgendaRepository extends ChangeNotifier
    implements AgendaRepository, AgendaSyncRepository {
  _FakeSyncAgendaRepository({
    required AgendaData local,
    required AgendaData cloud,
  }) : _local = _clone(local),
       _cloud = _clone(cloud);

  AgendaData _local;
  AgendaData _cloud;
  bool _hasConflict = true;
  int resolveCloudCalls = 0;

  @override
  bool get hasConflict => _hasConflict;

  @override
  bool get isSyncing => false;

  @override
  String? get syncMessage => _hasConflict ? 'Sincronizando com a nuvem.' : null;

  @override
  bool get hasTrialStatus => false;

  @override
  bool get trialActive => true;

  @override
  int get trialDaysRemaining => 7;

  void triggerCloudConflict(AgendaData cloud) {
    _cloud = _clone(cloud);
    _hasConflict = true;
    notifyListeners();
  }

  @override
  Future<AgendaData?> resolveConflictUsingCloud() async {
    if (!_hasConflict) return null;
    resolveCloudCalls++;
    _local = _clone(_cloud);
    _hasConflict = false;
    notifyListeners();
    return _clone(_local);
  }

  @override
  Future<AgendaData?> resolveConflictUsingLocal() async {
    _hasConflict = false;
    notifyListeners();
    return _clone(_local);
  }

  @override
  Future<AgendaData?> refreshRemoteIfSafe() async => null;

  @override
  Future<void> retrySync() async {}

  @override
  Future<void> clear() async => _local = AgendaData();

  @override
  Future<bool> hasData() async => true;

  @override
  Future<AgendaData?> load() async => _clone(_local);

  @override
  Future<AgendaData> loadOrCreate() async => _clone(_local);

  @override
  Future<void> save(AgendaData data) async => _local = _clone(data);

  static AgendaData _clone(AgendaData data) =>
      AgendaData.fromJson(data.toJson());
}
