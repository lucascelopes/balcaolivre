import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/agenda/appointment_payment_dialog.dart';
import 'package:agenda_livre/features/marketing/marketing_page.dart';
import 'package:agenda_livre/features/payments/mercado_pago_settings_dialog.dart';
import 'package:agenda_livre/features/pdv/pdv_page.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'services/fake_http_transport.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('pt_BR');
    await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
    await _loadFontAwesome();
    Directory(
      'artifacts/parity-current-2026-07-23',
    ).createSync(recursive: true);
  });

  for (final capture in <(String, Size)>[
    ('06-flutter-maquininha-automatica-desktop.png', const Size(1382, 736)),
    ('07-flutter-maquininha-automatica-mobile.png', const Size(390, 844)),
  ]) {
    testWidgets('captura ${capture.$1}', (tester) async {
      await _capturePointWait(tester, fileName: capture.$1, size: capture.$2);
    });
  }

  for (final capture in <(String, Size, bool)>[
    ('09-flutter-maquininha-setup-desktop.png', const Size(1382, 736), false),
    (
      '10-flutter-maquininha-conectada-desktop.png',
      const Size(1382, 736),
      true,
    ),
    ('11-flutter-maquininha-setup-mobile.png', const Size(390, 844), false),
  ]) {
    testWidgets('captura ${capture.$1}', (tester) async {
      await _captureSettings(
        tester,
        fileName: capture.$1,
        size: capture.$2,
        connected: capture.$3,
      );
    });
  }

  for (final capture in <(String, Size)>[
    ('14-flutter-pdv-desktop.png', const Size(1382, 736)),
    ('15-flutter-pdv-mobile.png', const Size(390, 844)),
  ]) {
    testWidgets('captura ${capture.$1}', (tester) async {
      await _capturePdv(tester, fileName: capture.$1, size: capture.$2);
    });
  }

  for (final capture in <(String, Size)>[
    ('18-flutter-marketing-desktop.png', const Size(1382, 736)),
    ('19-flutter-marketing-mobile.png', const Size(390, 844)),
  ]) {
    testWidgets('captura ${capture.$1}', (tester) async {
      await _captureMarketing(tester, fileName: capture.$1, size: capture.$2);
    });
  }
}

Future<void> _capturePointWait(
  WidgetTester tester, {
  required String fileName,
  required Size size,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final transport = FakeHttpTransport(
    (_) => const ServiceHttpResponse(
      statusCode: 200,
      body:
          '{"ok":true,"attemptId":"A1","orderId":"O1","localReference":"REF1","status":"AT_TERMINAL","paid":false}',
    ),
  );
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

  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData().copyWith(
        textTheme: ThemeData.light().textTheme.apply(fontFamily: 'Segoe UI'),
      ),
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              onPressed: () => showMercadoPagoPointProgressDialog(
                context,
                service: service,
                charge: const MercadoPagoChargeResult(
                  ok: true,
                  attemptId: 'A1',
                  orderId: 'O1',
                  localReference: 'REF1',
                  status: 'AT_TERMINAL',
                ),
                amount: 124.90,
                method: MercadoPagoPointMethod.debit,
                terminalId: 'PAX_Q92__Q92-1734055152',
                terminalLabel: 'Point Pro 3 · Q92-1734055152 (PDV)',
              ),
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.runAsync(() async {
    await precacheImage(
      const AssetImage('assets/branding/mercado-pago-pax-q92.png'),
      tester.element(find.byType(Scaffold)),
    );
  });
  await tester.tap(find.text('Abrir'));
  await tester.pump(const Duration(milliseconds: 300));
  await tester.pump(const Duration(milliseconds: 100));
  await tester.pump();

  expect(tester.takeException(), isNull);
  await expectLater(
    find.byKey(const Key('mercado-pago-point-progress-capture')),
    matchesGoldenFile('../artifacts/parity-current-2026-07-23/$fileName'),
  );

  await tester.tap(find.text('Cancelar'));
  await tester.pump();
  await tester.pump(const Duration(seconds: 2));
}

Future<void> _captureSettings(
  WidgetTester tester, {
  required String fileName,
  required Size size,
  required bool connected,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final terminalId = 'PAX_Q92__Q92-1734055152';
  final transport = FakeHttpTransport((request) {
    if (request.uri.path.endsWith('/mercadopago/status')) {
      return ServiceHttpResponse(
        statusCode: 200,
        body: connected
            ? '{"ok":true,"connected":true,"sellerUserId":"42","selectedTerminalId":"$terminalId","selectedTerminalLabel":"Point Pro 3 · Q92-1734055152 (PDV)"}'
            : '{"ok":true,"connected":false}',
      );
    }
    if (request.uri.path.endsWith('/mercadopago/terminals')) {
      return ServiceHttpResponse(
        statusCode: 200,
        body:
            '{"ok":true,"selectedTerminalId":"$terminalId","terminals":[{"id":"$terminalId","label":"Point Pro 3 · Q92-1734055152 (PDV)","operatingMode":"PDV","modelCode":"PAX_Q92","modelName":"Point Pro 3","serial":"Q92-1734055152","storeId":"Studio Nina Beauty"}]}',
      );
    }
    return const ServiceHttpResponse(statusCode: 200, body: '{"ok":true}');
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
  final data = AgendaData();
  data.settings
    ..businessName = 'Studio Nina Beauty'
    ..mercadoPagoEnabled = true
    ..mercadoPagoConnected = connected
    ..mercadoPagoDefaultTerminalId = connected ? terminalId : ''
    ..mercadoPagoDefaultTerminalLabel = connected
        ? 'Point Pro 3 · Q92-1734055152 (PDV)'
        : '';
  final controller =
      AgendaController(
          _MemoryAgendaRepository(data),
          mercadoPagoService: service,
        )
        ..data = data
        ..loading = false;

  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData().copyWith(
        textTheme: ThemeData.light().textTheme.apply(fontFamily: 'Segoe UI'),
      ),
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              onPressed: () =>
                  showMercadoPagoSettingsDialog(context, controller),
              child: const Text('Abrir configuração'),
            ),
          ),
        ),
      ),
    ),
  );
  if (connected) {
    await tester.runAsync(() async {
      await precacheImage(
        const AssetImage('assets/branding/mercado-pago-pax-q92.png'),
        tester.element(find.byType(Scaffold)),
      );
    });
  }
  await tester.tap(find.text('Abrir configuração'));
  await tester.pumpAndSettle();
  await tester.pump();

  expect(tester.takeException(), isNull);
  await expectLater(
    find.byKey(const Key('mercado-pago-wpf-settings-capture')),
    matchesGoldenFile('../artifacts/parity-current-2026-07-23/$fileName'),
  );
}

Future<void> _capturePdv(
  WidgetTester tester, {
  required String fileName,
  required Size size,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  final controller = _pdvController();

  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData().copyWith(
        textTheme: ThemeData.light().textTheme.apply(fontFamily: 'Segoe UI'),
      ),
      home: PdvPage(
        controller: controller,
        referenceNow: DateTime(2026, 7, 29, 10, 35),
        onExit: () {},
        onNavigate: (_) {},
      ),
    ),
  );
  await tester.runAsync(() async {
    await precacheImage(
      const AssetImage('assets/branding/agenda-livre-mark.png'),
      tester.element(find.byType(Scaffold)),
    );
  });
  await tester.pump();
  final layoutError = tester.takeException();
  await expectLater(
    find.byKey(Key(size.width >= 720 ? 'pdv-desktop' : 'pdv-mobile')),
    matchesGoldenFile('../artifacts/parity-current-2026-07-23/$fileName'),
  );
  expect(layoutError, isNull);
}

Future<void> _captureMarketing(
  WidgetTester tester, {
  required String fileName,
  required Size size,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  final controller = _pdvController();
  controller.data.settings
    ..businessName = 'Studio Nina Beauty'
    ..accountFullName = 'Nina Almeida'
    ..businessSegment = 'Salão de Beleza';
  while (controller.data.customers.length < 7) {
    final index = controller.data.customers.length + 1;
    controller.data.customers.add(
      Customer(
        name: 'Cliente $index',
        phone: '(33) 99900-${index.toString().padLeft(4, '0')}',
        lastSeenAt: DateTime(2026, 6, index),
      ),
    );
  }
  while (controller.data.appointments.length < 12) {
    final index = controller.data.appointments.length;
    controller.data.appointments.add(
      Appointment(
        id: 'marketing-$index',
        customerName: 'Cliente ${index + 1}',
        serviceName: 'Atendimento',
        professionalName: 'Camila Rocha',
        start: DateTime(2026, 7, 24 + (index % 3), 8 + (index % 8)),
        durationMinutes: 30,
        price: 80,
        status: AppointmentStatus.confirmed,
      ),
    );
  }
  controller.navigate(AgendaPage.marketing);
  const captureKey = Key('marketing-wpf-studio-capture');

  await tester.pumpWidget(
    RepaintBoundary(
      key: captureKey,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData().copyWith(
          textTheme: ThemeData.light().textTheme.apply(fontFamily: 'Segoe UI'),
        ),
        home: ResponsiveAgendaShell(
          controller: controller,
          referenceNow: DateTime(2026, 7, 23, 14, 2),
        ),
      ),
    ),
  );
  final context = tester.element(find.byType(MarketingPage));
  await tester.runAsync(() async {
    await Future.wait([
      for (final path in const [
        'assets/branding/marketing-story-background.png',
        'assets/branding/marketing-campaign-hair.png',
        'assets/branding/marketing-campaign-nails.png',
        'assets/branding/marketing-campaign-spa.png',
        'assets/branding/marketing-site-hero-hair.png',
      ])
        precacheImage(AssetImage(path), context),
    ]);
  });
  await tester.pumpAndSettle();
  final layoutError = tester.takeException();
  await expectLater(
    find.byKey(captureKey),
    matchesGoldenFile('../artifacts/parity-current-2026-07-23/$fileName'),
  );
  expect(layoutError, isNull);
}

AgendaController _pdvController() {
  final selectedDate = DateTime(2026, 7, 29);
  final appointments = <Appointment>[
    Appointment(
      id: 'a-running',
      customerName: 'Ana Clara Souza',
      serviceName: 'Coloração completa',
      professionalId: 'camila',
      professionalName: 'Camila Rocha',
      resourceName: 'Cadeira 1',
      start: DateTime(2026, 7, 29, 10),
      durationMinutes: 150,
      price: 280,
      status: AppointmentStatus.inService,
      serviceStartedAt: DateTime(2026, 7, 29, 10),
      serviceLines: [
        AppointmentServiceLine(
          serviceId: 'coloracao',
          serviceName: 'Coloração completa',
          durationMinutes: 150,
          unitPrice: 280,
        ),
      ],
    ),
    Appointment(
      id: 'a-beatriz',
      customerName: 'Beatriz Lima',
      serviceName: 'Manicure',
      professionalId: 'julia',
      professionalName: 'Júlia Martins',
      start: DateTime(2026, 7, 29, 11),
      durationMinutes: 45,
      price: 55,
      status: AppointmentStatus.waiting,
    ),
    Appointment(
      id: 'a-renata',
      customerName: 'Renata Alves',
      serviceName: 'Limpeza de pele',
      professionalId: 'mariana',
      professionalName: 'Mariana Costa',
      start: DateTime(2026, 7, 29, 13),
      durationMinutes: 75,
      price: 120,
      status: AppointmentStatus.scheduled,
    ),
    Appointment(
      id: 'a-isabela',
      customerName: 'Isabela Fernandes',
      serviceName: 'Corte feminino',
      professionalId: 'nina',
      professionalName: 'Nina Almeida',
      start: DateTime(2026, 7, 29, 9),
      durationMinutes: 60,
      price: 90,
      status: AppointmentStatus.confirmed,
    ),
  ];
  final data = AgendaData(
    settings: AgendaSettings(
      accountFullName: 'Isabela Ferreira',
      businessName: 'Agenda Livre',
      businessSegment: 'Centro de Estética',
      onboardingCompleted: true,
      workdayStartHour: 8,
      workdayEndHour: 19,
      resources: const ['Cadeira 1', 'Cadeira 2'],
    ),
    services: [
      ServiceItem(
        id: 'coloracao',
        name: 'Coloração',
        segment: 'Estética',
        durationMinutes: 90,
        price: 220,
      ),
      ServiceItem(
        id: 'tratamento',
        name: 'Tratamento capilar',
        segment: 'Estética',
        durationMinutes: 30,
        price: 60,
      ),
    ],
    professionals: [
      Professional(
        id: 'camila',
        name: 'Camila Rocha',
        role: 'Cabeleireira',
        segments: const ['Estética'],
      ),
      Professional(
        id: 'julia',
        name: 'Júlia Martins',
        role: 'Manicure',
        segments: const ['Estética'],
      ),
      Professional(
        id: 'mariana',
        name: 'Mariana Costa',
        role: 'Esteticista',
        segments: const ['Estética'],
      ),
      Professional(
        id: 'nina',
        name: 'Nina Almeida',
        role: 'Cabeleireira e proprietária',
        segments: const ['Estética'],
      ),
      Professional(
        id: 'laura',
        name: 'Laura Freitas',
        role: 'Assistente',
        segments: const ['Estética'],
      ),
    ],
    appointments: appointments,
  );
  return AgendaController(_MemoryAgendaRepository(data))
    ..data = data
    ..loading = false
    ..selectedDate = selectedDate;
}

Future<void> _loadFont(String family, String path) async {
  final bytes = File(path).readAsBytesSync();
  final loader = FontLoader(family)
    ..addFont(Future.value(ByteData.sublistView(bytes)));
  await loader.load();
}

Future<void> _loadFontAwesome() async {
  final config = File('.dart_tool/package_config.json').readAsStringSync();
  final match = RegExp(
    r'"name"\s*:\s*"font_awesome_flutter"[\s\S]*?"rootUri"\s*:\s*"([^"]+)"',
  ).firstMatch(config);
  if (match == null) return;
  final root = Uri.parse(match.group(1)!).toFilePath(windows: true);
  await _loadFont(
    'packages/font_awesome_flutter/FontAwesomeBrands',
    '$root\\lib\\fonts\\Font-Awesome-7-Brands-Regular-400.otf',
  );
  await _loadFont(
    'packages/font_awesome_flutter/FontAwesomeRegular',
    '$root\\lib\\fonts\\Font-Awesome-7-Free-Regular-400.otf',
  );
  await _loadFont(
    'packages/font_awesome_flutter/FontAwesomeSolid',
    '$root\\lib\\fonts\\Font-Awesome-7-Free-Solid-900.otf',
  );
}

class _MemoryAgendaRepository implements AgendaRepository {
  _MemoryAgendaRepository(this.data);

  AgendaData data;

  @override
  Future<void> clear() async => data = AgendaData();

  @override
  Future<bool> hasData() async => true;

  @override
  Future<AgendaData?> load() async => data;

  @override
  Future<AgendaData> loadOrCreate() async => data;

  @override
  Future<void> save(AgendaData value) async => data = value;
}
