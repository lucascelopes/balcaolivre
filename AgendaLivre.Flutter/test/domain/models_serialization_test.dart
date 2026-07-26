import 'dart:convert';

import 'package:agenda_livre/domain/models/models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('AgendaData JSON', () {
    test('round-trips every model and keeps the WPF field names', () {
      final createdAt = DateTime(2026, 7, 14, 10, 30);
      final data = AgendaData(
        settings: AgendaSettings(
          accountFullName: 'Lucas Cesar Lopes',
          accountPhone: '(33) 99800-7983',
          accountEmail: 'lucas@example.com',
          businessName: 'Lucas Barbearia',
          businessDocument: '12.345.678/0001-90',
          businessPhone: '(33) 99800-7983',
          businessAddress: 'Rua Piracicaba, 10',
          businessLogoPath: 'C:/Agenda/logo.png',
          publicBookingSlug: 'lucas-barbearia',
          publicBookingUrl: 'https://agenda.example/lucas-barbearia',
          publicBookingApiUrl: 'https://api.example/booking',
          publicBookingLastSyncAt: createdAt,
          marketingSitePromotion: MarketingSitePromotion(
            name: 'Semana da beleza',
            startDate: createdAt,
            endDate: createdAt.add(const Duration(days: 7)),
            limitPerCustomer: 2,
            highlightInCatalog: true,
            isPublished: true,
            publishedAt: createdAt,
            items: <MarketingSitePromotionItem>[
              MarketingSitePromotionItem(
                serviceId: 'service-1',
                serviceName: 'Manicure',
                originalPrice: 55,
                promotionalPrice: 45,
              ),
            ],
          ),
          publishedMarketingCatalog: MarketingCatalogPublication(
            slug: 'lucas-barbearia',
            promotion: MarketingSitePromotion(
              name: 'Semana da beleza',
              startDate: createdAt,
              endDate: createdAt.add(const Duration(days: 7)),
              isPublished: true,
              publishedAt: createdAt,
              items: <MarketingSitePromotionItem>[
                MarketingSitePromotionItem(
                  serviceId: 'service-1',
                  serviceName: 'Manicure',
                  originalPrice: 55,
                  promotionalPrice: 45,
                ),
              ],
            ),
          ),
          businessSegment: 'Centro de Estética',
          themeId: 'aesthetic-coral',
          resources: const <String>['Mesa 1', 'Cadeira beleza'],
          workdays: const <int>[1, 2, 3, 4, 5],
          workdayBreakEnabled: true,
          workdayBreakStartHour: 12,
          workdayBreakEndHour: 14,
          professionalCountRange: '2 profissionais',
          mainObjective: 'Implementar agendamento online',
          postalCode: '35032390',
          neighborhood: 'Lourdes',
          street: 'Rua Piracicaba',
          addressNumber: '10',
          addressComplement: 'Sala 2',
          accountPasswordHash: 'hash',
          accountCreatedAt: createdAt,
          whatsAppLinked: true,
          whatsAppStorePhone: '5533998007983',
          whatsAppConnectedName: 'Lucas Barbearia',
          whatsAppLinkedAt: createdAt,
          whatsAppLastMessageAt: createdAt,
          whatsAppEvolutionApiKey: 'key',
          whatsAppEvolutionState: 'open',
          whatsAppEvolutionQrBase64: 'base64',
          whatsAppEvolutionLastCheckedAt: createdAt,
          instagramEnabled: true,
          instagramLinked: true,
          instagramUsername: 'lucasbarbearia',
          instagramDisplayName: 'Lucas Barbearia',
          instagramAccountId: 'instagram-account',
          instagramState: 'connected',
          instagramLinkedAt: createdAt,
          instagramLastCheckedAt: createdAt,
          mercadoPagoEnabled: true,
          mercadoPagoConnected: true,
          mercadoPagoLicenseKey: 'license',
          mercadoPagoSellerUserId: 'seller',
          mercadoPagoDefaultTerminalId: 'terminal',
          mercadoPagoDefaultTerminalLabel: 'Caixa',
          mercadoPagoLastError: 'none',
          mercadoPagoLastSyncAt: createdAt,
        ),
        services: <ServiceItem>[
          ServiceItem(
            id: 'service-1',
            segment: 'Centro de Estética',
            name: 'Manicure',
            category: 'Unhas',
            description: 'Descrição',
            durationMinutes: 45,
            preparationMinutes: 5,
            bufferMinutes: 10,
            price: 55,
            commissionPercent: 20,
            defaultResource: 'Mesa 1',
          ),
        ],
        professionals: <Professional>[
          Professional(
            id: 'professional-1',
            name: 'Manicure 1',
            segments: const <String>['Centro de Estética'],
            role: 'Manicure',
            phone: '(33) 90000-0000',
            email: 'pro@example.com',
            document: '123',
            commissionPercent: 30,
            notes: 'Notas',
          ),
        ],
        customers: <Customer>[
          Customer(
            id: 'customer-1',
            name: 'Ana',
            phone: '(33) 91111-1111',
            email: 'ana@example.com',
            document: '456',
            segment: 'Centro de Estética',
            profile: 'Francesinha',
            tags: 'VIP',
            notes: 'Cliente recorrente',
            lastSeenAt: createdAt,
          ),
        ],
        appointments: <Appointment>[
          Appointment(
            id: 'appointment-1',
            segment: 'Centro de Estética',
            customerName: 'Ana',
            customerPhone: '(33) 91111-1111',
            customerProfile: 'Francesinha',
            serviceId: 'service-1',
            serviceName: 'Manicure',
            professionalId: 'professional-1',
            professionalName: 'Manicure 1',
            resourceName: 'Mesa 1',
            start: createdAt,
            durationMinutes: 45,
            price: 55,
            status: AppointmentStatus.confirmed,
            notes: 'Observação',
            externalSource: 'public_booking',
            externalReference: 'booking-123',
            createdAt: createdAt,
            updatedAt: createdAt,
          ),
        ],
        products: <ProductItem>[
          ProductItem(
            id: 'product-1',
            name: 'Esmalte',
            category: 'Unhas',
            sku: 'ESM-01',
            supplier: 'Fornecedor',
            costPrice: 10,
            price: 20,
            stockQuantity: 8,
            minimumStock: 2,
            notes: 'Vermelho',
            createdAt: createdAt,
          ),
        ],
        productSales: <ProductSale>[
          ProductSale(
            id: 'sale-1',
            productId: 'product-1',
            productName: 'Esmalte',
            customerName: 'Ana',
            quantity: 2,
            unitPrice: 20,
            discount: 5,
            paymentMethod: 'Pix',
            paymentProvider: 'Local',
            paymentReference: 'REF',
            paymentStatus: 'aprovado',
            notes: 'Venda',
            soldAt: createdAt,
          ),
        ],
        manualPayments: <ManualPayment>[
          ManualPayment(
            id: 'payment-1',
            description: 'Pagamento avulso',
            customerName: 'Ana',
            category: 'Agendamento',
            paymentMethod: 'Pix',
            paymentProvider: 'Local',
            paymentReference: 'PAY',
            paymentStatus: 'aprovado',
            notes: 'Entrada',
            value: 55,
            paidAt: createdAt,
          ),
        ],
        expenses: <ExpenseItem>[
          ExpenseItem(
            id: 'expense-1',
            description: 'Material',
            category: 'Insumos',
            supplier: 'Fornecedor',
            paymentMethod: 'Pix',
            notes: 'Compra',
            value: 25,
            date: createdAt,
          ),
        ],
        whatsAppMessages: <WhatsAppMessage>[
          WhatsAppMessage(
            id: 'message-1',
            providerMessageId: 'provider-message-1',
            provider: 'evolution',
            instance: 'agenda-livre',
            conversationId: 'conversation-1',
            clientRequestId: 'request-1',
            leadId: 'lead-1',
            customerName: 'Ana',
            phone: '5533911111111',
            message: 'Agendamento confirmado',
            direction: 'saida',
            status: 'enviado',
            category: 'Confirmação',
            createdAt: createdAt,
            sentAt: createdAt,
            receivedAt: createdAt,
            readAt: createdAt,
          ),
        ],
        whatsAppLeads: <WhatsAppLead>[
          WhatsAppLead(
            id: 'lead-1',
            instance: 'agenda-livre',
            conversationId: 'conversation-1',
            customerName: 'Ana',
            phone: '5533911111111',
            stage: 'qualified',
            score: 90,
            summary: 'Quer agendar manicure.',
            facts: const <String>['cliente recorrente'],
            intent: 'booking',
            requestedService: 'Manicure',
            preferredSchedule: 'terça à tarde',
            unread: true,
            unreadCount: 2,
            followupCount: 1,
            nextFollowupAt: createdAt,
            lastInboundAt: createdAt,
            createdAt: createdAt,
            updatedAt: createdAt,
            lastMessageAt: createdAt,
          ),
        ],
      );

      final wireJson = jsonEncode(data.toJson());
      final decoded = AgendaData.fromJson(
        Map<String, dynamic>.from(jsonDecode(wireJson) as Map),
      );

      expect(decoded.toJson(), equals(data.toJson()));
      expect(decoded.toJson(), contains('Settings'));
      expect(decoded.toJson(), isNot(contains('settings')));
      expect(decoded.appointments.single.status, AppointmentStatus.confirmed);
      expect(
        decoded.appointments.single.end.difference(createdAt).inMinutes,
        45,
      );
      expect(decoded.productSales.single.total, 35);
      expect(decoded.services.single.displayName, 'Manicure - 45 min');
      expect(decoded.settings.publicBookingSlug, 'lucas-barbearia');
      expect(decoded.settings.marketingSitePromotion.isPublished, isTrue);
      expect(
        decoded
            .settings
            .publishedMarketingCatalog
            ?.promotion
            ?.items
            .single
            .promotionalPrice,
        45,
      );
      expect(decoded.appointments.single.externalReference, 'booking-123');
      expect(decoded.whatsAppMessages.single.clientRequestId, 'request-1');
    });

    test('accepts camelCase and numeric values from migrated clients', () {
      final data = AgendaData.fromJson(<String, dynamic>{
        'settings': <String, dynamic>{
          'businessName': 'Salão Teste',
          'workdayStartHour': '9',
        },
        'appointments': <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'appointment-1',
            'customerName': 'Cliente',
            'durationMinutes': 60.0,
            'price': '75,50',
            'status': 'in_service',
            'start': '2026-07-14T09:00:00-03:00',
          },
        ],
      });

      expect(data.settings.businessName, 'Salão Teste');
      expect(data.settings.workdayStartHour, 9);
      expect(data.appointments.single.durationMinutes, 60);
      expect(data.appointments.single.price, 75.5);
      expect(data.appointments.single.status, AppointmentStatus.inService);
      expect(data.appointments.single.start.hour, 9);
    });

    test('keeps the Windows wall-clock time when JSON includes an offset', () {
      final data = AgendaData.fromJson(<String, dynamic>{
        'Appointments': <Map<String, dynamic>>[
          <String, dynamic>{
            'Id': 'windows-appointment',
            'Start': '2026-07-14T10:00:00-03:00',
          },
        ],
      });

      expect(data.appointments.single.start.isUtc, isFalse);
      expect(data.appointments.single.start.hour, 10);
    });
  });
}
