import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:intl/intl.dart';

import 'app/agenda_app.dart';
import 'app/agenda_controller.dart';
import 'data/seed/agenda_seed_data.dart';
import 'domain/models/models.dart';
import 'domain/repositories/agenda_repository.dart';
import 'services/http_transport.dart';
import 'services/mercado_pago_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  Intl.defaultLocale = 'pt_BR';
  await initializeDateFormatting('pt_BR');
  final data = _visualPreviewData();
  final service = MercadoPagoService(
    transport: const _VisualPreviewTransport(),
    config: MercadoPagoServiceConfig(
      activateClient: false,
      baseUri: Uri.parse('https://preview.local/functions/v1/payments'),
      contextProvider: () => const MercadoPagoClientContext(
        licenseKey: 'VISUAL-PREVIEW',
        machineHash: 'VISUAL-PREVIEW',
        clientKind: 'web',
      ),
    ),
  );
  runApp(
    AgendaLivreApp(
      controller: AgendaController(
        _VisualPreviewRepository(data),
        mercadoPagoService: service,
        authenticatedEmail: 'marina@studiofluxo.com.br',
      ),
    ),
  );
}

AgendaData _visualPreviewData() {
  final today = DateUtils.dateOnly(DateTime.now());
  final data = AgendaSeedData.salon(referenceDate: today);
  data.settings
    ..themeId = ''
    ..accountFullName = 'Marina Teste'
    ..accountEmail = 'marina@studiofluxo.com.br'
    ..businessName = 'Studio Fluxo'
    ..businessPhone = '(33) 99131-4125'
    ..businessSegment = 'Centro de Estética'
    ..monthlyRevenueGoal = 12000
    ..pixKey = '33.991.314/0001-25'
    ..mercadoPagoEnabled = true
    ..mercadoPagoConnected = true
    ..mercadoPagoSellerUserId = 'preview-seller'
    ..mercadoPagoDefaultTerminalId = 'POINT-CAIXA'
    ..mercadoPagoDefaultTerminalLabel = 'Point do balcão'
    ..mercadoPagoLastSyncAt = DateTime.now();

  final customer = Customer(
    id: 'customer-preview',
    name: 'Ana Martins',
    phone: '(33) 99999-1010',
    segment: 'Centro de Estética',
    profile: 'Prefere atendimento à tarde',
    tags: 'VIP',
    lastSeenAt: today.subtract(const Duration(days: 35)),
  );
  data.customers.add(customer);
  data.products.addAll([
    ProductItem(
      id: 'product-shampoo',
      name: 'Shampoo profissional',
      category: 'Cabelos',
      sku: 'SHP-01',
      costPrice: 24.90,
      price: 49.90,
      stockQuantity: 8,
      minimumStock: 3,
    ),
    ProductItem(
      id: 'product-oil',
      name: 'Óleo finalizador',
      category: 'Cabelos',
      sku: 'OLE-02',
      costPrice: 18,
      price: 39.90,
      stockQuantity: 2,
      minimumStock: 3,
    ),
  ]);

  data.appointments.addAll([
    Appointment(
      id: 'appointment-preview-1',
      segment: 'Centro de Estética',
      customerId: customer.id,
      customerName: customer.name,
      customerPhone: customer.phone,
      serviceId: 'service-manicure',
      serviceName: 'Manicure',
      professionalId: 'professional-designer-1',
      professionalName: 'Designer 1',
      resourceName: 'Mesa 1',
      start: today.add(const Duration(hours: 10)),
      durationMinutes: 45,
      price: 55,
      status: AppointmentStatus.confirmed,
    ),
    Appointment(
      id: 'appointment-preview-2',
      segment: 'Centro de Estética',
      customerId: customer.id,
      customerName: customer.name,
      customerPhone: customer.phone,
      serviceId: 'service-sobrancelha',
      serviceName: 'Design de sobrancelha',
      professionalId: 'professional-designer-1',
      professionalName: 'Designer 1',
      resourceName: 'Cadeira beleza',
      start: today.add(const Duration(hours: 14)),
      durationMinutes: 30,
      price: 80,
      status: AppointmentStatus.done,
      paymentMethod: 'Conta do cliente',
      paymentProvider: 'customer_account',
      paymentStatus: 'pending',
    ),
  ]);
  data.customerReceivables.add(
    CustomerReceivable(
      id: 'receivable-preview',
      customerId: customer.id,
      customerName: customer.name,
      appointmentId: 'appointment-preview-2',
      description: 'Design de sobrancelha',
      originalValue: 80,
      remainingValue: 80,
      openedAt: today.add(const Duration(hours: 14, minutes: 30)),
      updatedAt: today.add(const Duration(hours: 14, minutes: 30)),
      paymentProvider: 'customer_account',
      paymentStatus: 'pending',
    ),
  );
  data.expenses.add(
    ExpenseItem(
      id: 'expense-preview',
      description: 'Materiais de atendimento',
      category: 'Insumos',
      value: 75,
      date: today,
    ),
  );
  return data;
}

class _VisualPreviewRepository implements AgendaRepository {
  _VisualPreviewRepository(this._data);

  AgendaData? _data;

  @override
  Future<void> clear() async => _data = null;

  @override
  Future<bool> hasData() async => _data != null;

  @override
  Future<AgendaData?> load() async => _data;

  @override
  Future<AgendaData> loadOrCreate() async => _data ?? AgendaData();

  @override
  Future<void> save(AgendaData data) async => _data = data;
}

class _VisualPreviewTransport implements HttpTransport {
  const _VisualPreviewTransport();

  @override
  Future<ServiceHttpResponse> send(ServiceHttpRequest request) async {
    final path = request.uri.path;
    if (path.endsWith('/mercadopago/status')) {
      return _ok(
        '{"ok":true,"connected":true,"status":"connected",'
        '"sellerUserId":"preview-seller",'
        '"selectedTerminalId":"POINT-CAIXA",'
        '"selectedTerminalLabel":"Point do balcão",'
        '"lastSyncAt":"${DateTime.now().toIso8601String()}"}',
      );
    }
    if (path.endsWith('/mercadopago/terminals')) {
      return _ok(
        '{"ok":true,"selectedTerminalId":"POINT-CAIXA",'
        '"selectedTerminalLabel":"Point do balcão","terminals":['
        '{"id":"POINT-CAIXA","label":"Point do balcão",'
        '"posId":"POS-1","storeId":"STORE-1","operatingMode":"PDV"},'
        '{"id":"POINT-MOVEL","label":"Point móvel",'
        '"posId":"POS-2","storeId":"STORE-1","operatingMode":"PDV"}]}',
      );
    }
    if (path.endsWith('/mercadopago/point/charge')) {
      return _ok(
        '{"ok":true,"attemptId":"PREVIEW-POINT",'
        '"orderId":"ORDER-1","localReference":"PREVIEW-REF",'
        '"status":"created"}',
      );
    }
    if (path.endsWith('/mercadopago/point/status') ||
        path.endsWith('/mercadopago/web/status')) {
      return _ok(
        '{"ok":true,"attemptId":"PREVIEW-POINT",'
        '"paymentId":"PREVIEW-PAYMENT","status":"approved","paid":true}',
      );
    }
    if (path.endsWith('/mercadopago/web/charge')) {
      return _ok(
        '{"ok":true,"attemptId":"PREVIEW-PIX",'
        '"paymentId":"PREVIEW-PIX-PAYMENT",'
        '"localReference":"PREVIEW-PIX-REF","status":"pending",'
        '"qrCode":"00020126580014BR.GOV.BCB.PIX",'
        '"paymentUrl":"https://www.mercadopago.com.br/"}',
      );
    }
    if (path.endsWith('/mercadopago/connect/start')) {
      return _ok(
        '{"ok":true,"authUrl":"https://www.mercadopago.com.br/",'
        '"expiresAt":"${DateTime.now().add(const Duration(minutes: 10)).toIso8601String()}"}',
      );
    }
    return _ok('{"ok":true}');
  }

  ServiceHttpResponse _ok(String body) => ServiceHttpResponse(
    statusCode: 200,
    body: body,
    headers: const <String, String>{'content-type': 'application/json'},
  );
}
