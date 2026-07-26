import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/payments/mercado_pago_settings_dialog.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../services/fake_http_transport.dart';

void main() {
  testWidgets('allows first-time connection while Mercado Pago switch is off', (
    tester,
  ) async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/mercadopago/connect/start')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body: '{"ok":true,"authUrl":"https://auth.example/authorize"}',
        );
      }
      return const ServiceHttpResponse(
        statusCode: 200,
        body: '{"ok":true,"connected":false}',
      );
    });
    final controller = _controller(transport);
    expect(controller.data.settings.mercadoPagoEnabled, isFalse);
    await _pumpLauncher(tester, controller, const Size(390, 844));

    await tester.tap(find.byKey(const Key('open-mercado-pago')));
    await tester.pumpAndSettle();

    final connect = tester.widget<FilledButton>(
      find.byKey(const Key('mercado-pago-connect')),
    );
    expect(connect.onPressed, isNotNull);

    await tester.tap(find.byKey(const Key('mercado-pago-connect')));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(controller.data.settings.mercadoPagoEnabled, isTrue);
    expect(
      transport.requests.any(
        (request) => request.uri.path.endsWith('/mercadopago/connect/start'),
      ),
      isTrue,
    );
  });

  testWidgets('configures the connected Point on desktop', (tester) async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/mercadopago/status')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"connected":true,"sellerUserId":"42","selectedTerminalId":"PAX_Q92__Q92-1734055152","selectedTerminalLabel":"Point Pro 3 · Q92-1734055152 (PDV)"}',
        );
      }
      if (request.uri.path.endsWith('/mercadopago/terminals')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"selectedTerminalId":"PAX_Q92__Q92-1734055152","terminals":[{"id":"PAX_Q92__Q92-1734055152","label":"Point Pro 3 · Q92-1734055152 (PDV)","operatingMode":"PDV","modelCode":"PAX_Q92","modelName":"Point Pro 3","serial":"Q92-1734055152","storeId":"Studio Nina Beauty"}]}',
        );
      }
      return const ServiceHttpResponse(statusCode: 200, body: '{"ok":true}');
    });
    final controller = _controller(transport);
    controller.data.settings.mercadoPagoEnabled = true;
    await _pumpLauncher(tester, controller, const Size(1200, 800));

    await tester.tap(find.byKey(const Key('open-mercado-pago')));
    await tester.pumpAndSettle();

    expect(find.text('Mercado Pago na agenda'), findsOneWidget);
    expect(find.text('Pronto'), findsOneWidget);
    expect(find.text('Maquininha conectada'), findsOneWidget);
    expect(find.text('Point Pro 3'), findsOneWidget);
    expect(
      find.byKey(const Key('mercado-pago-settings-terminal-image')),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);

    await tester.tap(find.byKey(const Key('mercado-pago-save')));
    await tester.pumpAndSettle();

    expect(controller.data.settings.mercadoPagoConnected, isTrue);
    expect(
      controller.data.settings.mercadoPagoDefaultTerminalId,
      'PAX_Q92__Q92-1734055152',
    );
    expect(
      transport.requests.any(
        (request) => request.uri.path.endsWith('/mercadopago/terminal/select'),
      ),
      isTrue,
    );
  });

  testWidgets('stacks the same setup flow on mobile without overflow', (
    tester,
  ) async {
    final transport = FakeHttpTransport(
      (_) => const ServiceHttpResponse(
        statusCode: 200,
        body: '{"ok":true,"connected":false}',
      ),
    );
    final controller = _controller(transport);
    controller.data.settings.mercadoPagoEnabled = true;
    await _pumpLauncher(tester, controller, const Size(390, 844));

    await tester.tap(find.byKey(const Key('open-mercado-pago')));
    await tester.pumpAndSettle();

    expect(find.text('Conectar conta'), findsWidgets);
    expect(find.text('Encontrar maquininha'), findsOneWidget);
    expect(
      find.byKey(const Key('mercado-pago-terminal-select')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('mercado-pago-save')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

AgendaController _controller(FakeHttpTransport transport) {
  final service = MercadoPagoService(
    transport: transport,
    config: MercadoPagoServiceConfig(
      activateClient: false,
      baseUri: Uri.parse('https://api.example/functions/v1/payments'),
      contextProvider: () => const MercadoPagoClientContext(
        licenseKey: 'TEST-LICENSE',
        machineHash: 'TEST-MACHINE',
      ),
    ),
  );
  return AgendaController(
      _MemoryAgendaRepository(),
      mercadoPagoService: service,
    )
    ..data = AgendaData()
    ..loading = false;
}

Future<void> _pumpLauncher(
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
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              key: const Key('open-mercado-pago'),
              onPressed: () =>
                  showMercadoPagoSettingsDialog(context, controller),
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    ),
  );
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
