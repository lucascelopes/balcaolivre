import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/finance/product_sale_dialog.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../services/fake_http_transport.dart';

void main() {
  testWidgets('registra venda e baixa estoque no celular', (tester) async {
    final controller = _controller();
    await _open(tester, controller, const Size(390, 844));

    await tester.enterText(find.byKey(const Key('product-sale-quantity')), '2');
    await tester.enterText(
      find.byKey(const Key('product-sale-discount')),
      '5,00',
    );
    await tester.tap(find.byKey(const Key('product-sale-save')));
    await tester.pumpAndSettle();

    final sale = controller.data.productSales.single;
    expect(sale.quantity, 2);
    expect(sale.total, 95);
    expect(controller.data.products.single.stockQuantity, 8);
    expect(tester.takeException(), isNull);
  });

  testWidgets('só salva venda Mercado Pago depois da aprovação Point', (
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

    await tester.tap(find.byKey(const Key('product-sale-method')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Mercado Pago - crédito na maquininha').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('product-sale-save')));
    await tester.pump();

    expect(controller.data.productSales, isEmpty);
    expect(
      find.byKey(const Key('mercado-pago-point-progress')),
      findsOneWidget,
    );
    await tester.pump(const Duration(milliseconds: 1400));
    await tester.pumpAndSettle();

    final sale = controller.data.productSales.single;
    expect(sale.paymentProvider, 'Mercado Pago');
    expect(sale.paymentReference, 'P1');
    expect(sale.paymentStatus, 'approved');
  });
}

AgendaController _controller({MercadoPagoService? service}) {
  final data = AgendaData(
    products: <ProductItem>[
      ProductItem(
        id: 'P1',
        name: 'Shampoo',
        sku: 'SHP-1',
        price: 50,
        stockQuantity: 10,
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
              key: const Key('open-sale'),
              onPressed: () =>
                  showProductSaleDialog(context, controller: controller),
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.byKey(const Key('open-sale')));
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('product-sale-dialog')), findsOneWidget);
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
