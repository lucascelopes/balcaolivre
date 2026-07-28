import 'dart:convert';

import 'package:agenda_livre/domain/models/models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('current WPF payment payload survives the Flutter JSON round trip', () {
    final wpfPayload = <String, dynamic>{
      'Settings': <String, dynamic>{
        'BusinessName': 'Studio Nina',
        'PixKey': 'financeiro@studionina.com.br',
      },
      'Services': <Object>[],
      'Professionals': <Object>[],
      'Customers': <Object>[],
      'Appointments': <Object>[
        <String, dynamic>{
          'Id': 'appointment-1',
          'Segment': 'Salão e beleza',
          'CustomerId': 'customer-1',
          'CustomerName': 'Ana Souza',
          'CustomerPhone': '5533999999999',
          'CustomerProfile': 'Cliente recorrente',
          'ServiceId': 'service-1',
          'ServiceName': 'Corte e escova',
          'ProfessionalId': 'professional-1',
          'ProfessionalName': 'Nina Almeida',
          'ResourceName': 'Cadeira 1',
          'Start': '2026-07-19T11:00:00-03:00',
          'DurationMinutes': 60,
          'Price': 149.90,
          'Status': 'Done',
          'PaymentConfirmedAt': '2026-07-19T11:45:00-03:00',
          'PaymentMethod': 'credit_card',
          'PaymentProvider': 'mercado_pago_point',
          'PaymentReference': 'payment-123',
          'PaymentStatus': 'approved',
          'Notes': 'Pagamento confirmado na Point.',
          'ExternalSource': 'public_booking',
          'ExternalReference': 'booking-123',
          'CreatedAt': '2026-07-18T09:30:00-03:00',
          'UpdatedAt': '2026-07-19T11:45:00-03:00',
        },
      ],
      'Products': <Object>[],
      'ProductSales': <Object>[],
      'ManualPayments': <Object>[],
      'CustomerReceivables': <Object>[
        <String, dynamic>{
          'Id': 'receivable-1',
          'CustomerId': 'customer-1',
          'CustomerName': 'Ana Souza',
          'AppointmentId': 'appointment-1',
          'Description': 'Corte e escova',
          'OriginalValue': 149.90,
          'RemainingValue': 49.90,
          'Status': 'partial',
          'OpenedAt': '2026-07-19T11:45:00-03:00',
          'UpdatedAt': '2026-07-19T12:00:00-03:00',
          'DueAt': '2026-07-26T18:00:00-03:00',
          'PaidAt': null,
          'PaymentMethod': 'customer_account',
          'PaymentProvider': 'local',
          'PaymentReference': 'account-entry-123',
          'PaymentStatus': 'pending',
          'Notes': 'Entrada de R\$ 100,00 recebida.',
        },
      ],
      'Expenses': <Object>[],
      'WhatsAppMessages': <Object>[],
      'WhatsAppLeads': <Object>[],
    };

    final wirePayload = Map<String, dynamic>.from(
      jsonDecode(jsonEncode(wpfPayload)) as Map,
    );
    final data = AgendaData.fromJson(wirePayload);
    final roundTrip = Map<String, dynamic>.from(
      jsonDecode(jsonEncode(data.toJson())) as Map,
    );

    expect(data.settings.pixKey, 'financeiro@studionina.com.br');
    expect(data.appointments, hasLength(1));
    expect(data.customerReceivables, hasLength(1));

    final appointment = data.appointments.single;
    expect(appointment.customerId, 'customer-1');
    expect(appointment.paymentConfirmedAt?.hour, 11);
    expect(appointment.paymentMethod, 'credit_card');
    expect(appointment.paymentProvider, 'mercado_pago_point');
    expect(appointment.paymentReference, 'payment-123');
    expect(appointment.paymentStatus, 'approved');

    final receivable = data.customerReceivables.single;
    expect(receivable.customerId, 'customer-1');
    expect(receivable.appointmentId, 'appointment-1');
    expect(receivable.originalValue, 149.90);
    expect(receivable.remainingValue, 49.90);
    expect(receivable.status, 'partial');
    expect(receivable.dueAt?.day, 26);
    expect(receivable.paidAt, isNull);
    expect(receivable.paymentReference, 'account-entry-123');

    expect(roundTrip.keys, containsAll(wpfPayload.keys));
    final settingsJson = Map<String, dynamic>.from(
      roundTrip['Settings'] as Map,
    );
    final appointmentJson = Map<String, dynamic>.from(
      (roundTrip['Appointments'] as List).single as Map,
    );
    final receivableJson = Map<String, dynamic>.from(
      (roundTrip['CustomerReceivables'] as List).single as Map,
    );

    expect(
      settingsJson.keys,
      containsAll((wpfPayload['Settings'] as Map).keys),
    );
    expect(settingsJson['PixKey'], 'financeiro@studionina.com.br');
    expect(
      appointmentJson.keys,
      containsAll(((wpfPayload['Appointments'] as List).single as Map).keys),
    );
    expect(appointmentJson['CustomerId'], 'customer-1');
    expect(appointmentJson['PaymentMethod'], 'credit_card');
    expect(appointmentJson['PaymentProvider'], 'mercado_pago_point');
    expect(appointmentJson['PaymentReference'], 'payment-123');
    expect(appointmentJson['PaymentStatus'], 'approved');
    expect(
      receivableJson.keys,
      containsAll(
        ((wpfPayload['CustomerReceivables'] as List).single as Map).keys,
      ),
    );
    expect(receivableJson['CustomerId'], 'customer-1');
    expect(receivableJson['AppointmentId'], 'appointment-1');
    expect(receivableJson['RemainingValue'], 49.90);
    expect(receivableJson['PaymentStatus'], 'pending');
  });
}
