import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/agenda/appointment_dialog.dart';
import 'package:agenda_livre/features/establishment/editor_dialogs.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

const _desktopSize = Size(1366, 768);
const _mobileSize = Size(390, 844);
final _referenceNow = DateTime(2026, 7, 19, 9);

void main() {
  setUpAll(() async {
    await initializeDateFormatting('pt_BR');
    await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont('Ahem', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
    await _loadFontAwesome();
    Directory(
      'artifacts/ux-product-audit-2026-07-21',
    ).createSync(recursive: true);
  });

  final desktopScreens = <(String, AgendaPage)>[
    ('01-home-desktop.png', AgendaPage.home),
    ('02-agenda-desktop.png', AgendaPage.agenda),
    ('05-financeiro-desktop.png', AgendaPage.finance),
    ('06-relatorios-desktop.png', AgendaPage.reports),
    ('07-estabelecimento-desktop.png', AgendaPage.establishment),
    ('08-marketing-desktop.png', AgendaPage.marketing),
    ('09-configuracoes-desktop.png', AgendaPage.settings),
  ];

  for (final (fileName, page) in desktopScreens) {
    testWidgets('captura $fileName', (tester) async {
      await _captureShell(
        tester,
        size: _desktopSize,
        page: page,
        fileName: fileName,
      );
    });
  }

  final mobileScreens = <(String, AgendaPage)>[
    ('10-home-mobile.png', AgendaPage.home),
    ('11-agenda-mobile.png', AgendaPage.agenda),
    ('14-financeiro-mobile.png', AgendaPage.finance),
    ('15-marketing-mobile.png', AgendaPage.marketing),
    ('16-estabelecimento-mobile.png', AgendaPage.establishment),
    ('17-configuracoes-mobile.png', AgendaPage.settings),
    ('18-relatorios-mobile.png', AgendaPage.reports),
  ];

  for (final (fileName, page) in mobileScreens) {
    testWidgets('captura $fileName', (tester) async {
      await _captureShell(
        tester,
        size: _mobileSize,
        page: page,
        fileName: fileName,
      );
    });
  }

  for (final (fileName, size) in <(String, Size)>[
    ('27-agenda-selecionada-desktop.png', _desktopSize),
    ('28-agenda-selecionada-mobile.png', _mobileSize),
  ]) {
    testWidgets('captura $fileName', (tester) async {
      await _captureSelectedAgenda(tester, size: size, fileName: fileName);
    });
  }

  for (final (fileName, size) in <(String, Size)>[
    ('03-editar-agendamento-desktop.png', _desktopSize),
    ('12-editar-agendamento-mobile.png', _mobileSize),
  ]) {
    testWidgets('captura $fileName', (tester) async {
      await _captureAppointmentEditor(tester, size: size, fileName: fileName);
    });
  }

  testWidgets('captura etapas restantes da edição de agendamento', (
    tester,
  ) async {
    await _captureAppointmentEditor(
      tester,
      size: _desktopSize,
      fileName: '21-editar-agendamento-cliente-desktop.png',
      advanceSteps: 1,
    );
  });

  testWidgets('captura confirmação da edição de agendamento', (tester) async {
    await _captureAppointmentEditor(
      tester,
      size: _desktopSize,
      fileName: '22-editar-agendamento-confirmar-desktop.png',
      advanceSteps: 2,
    );
  });

  testWidgets('captura edicao de agenda legada em dia fechado', (tester) async {
    await _captureAppointmentEditor(
      tester,
      size: _desktopSize,
      fileName: '29-editar-agendamento-legado-dia-fechado.png',
      preserveOriginalSchedule: true,
    );
  });

  for (final (fileName, size) in <(String, Size)>[
    ('19-registrar-pagamento-desktop.png', _desktopSize),
    ('20-registrar-pagamento-mobile.png', _mobileSize),
  ]) {
    testWidgets('captura $fileName', (tester) async {
      await _capturePaymentDialog(tester, size: size, fileName: fileName);
    });
  }

  for (final (fileName, size) in <(String, Size)>[
    ('04-editar-cliente-desktop.png', _desktopSize),
    ('13-editar-cliente-mobile.png', _mobileSize),
  ]) {
    testWidgets('captura $fileName', (tester) async {
      await _captureCustomerEditor(tester, size: size, fileName: fileName);
    });
  }
}

Future<void> _captureShell(
  WidgetTester tester, {
  required Size size,
  required AgendaPage page,
  required String fileName,
}) async {
  _setViewport(tester, size);
  final controller = _seededController()..navigate(page);
  const captureKey = Key('ux-audit-shell-capture');

  await tester.pumpWidget(
    RepaintBoundary(
      key: captureKey,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(
          controller: controller,
          referenceNow: _referenceNow,
        ),
      ),
    ),
  );
  await _settleVisualAssets(tester);
  final layoutException = tester.takeException();
  expect(layoutException, isNull);
  await _saveCapture(tester, captureKey, fileName);
}

Future<void> _captureAppointmentEditor(
  WidgetTester tester, {
  required Size size,
  required String fileName,
  int advanceSteps = 0,
  bool preserveOriginalSchedule = false,
}) async {
  _setViewport(tester, size);
  final controller = _seededController();
  final appointment = controller.data.appointments.first;
  if (!preserveOriginalSchedule) {
    appointment.start = DateTime(2026, 7, 20, 10);
  }
  const captureKey = Key('ux-audit-appointment-capture');

  await tester.pumpWidget(
    RepaintBoundary(
      key: captureKey,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AgendaThemes.byId('').toThemeData(),
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: ElevatedButton(
                key: const Key('open-appointment-editor'),
                onPressed: () => showAppointmentDialog(
                  context,
                  controller,
                  appointment: appointment,
                ),
                child: const Text('Editar agendamento'),
              ),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.byKey(const Key('open-appointment-editor')));
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('appointment-dialog')), findsOneWidget);
  for (var index = 0; index < advanceSteps; index++) {
    await tester.tap(find.byKey(const Key('appointment-continue')));
    await tester.pumpAndSettle();
  }
  expect(tester.takeException(), isNull);
  await _saveCapture(tester, captureKey, fileName);
}

Future<void> _captureSelectedAgenda(
  WidgetTester tester, {
  required Size size,
  required String fileName,
}) async {
  _setViewport(tester, size);
  final controller = _seededController()..navigate(AgendaPage.agenda);
  const captureKey = Key('ux-audit-selected-agenda-capture');

  await tester.pumpWidget(
    RepaintBoundary(
      key: captureKey,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(
          controller: controller,
          referenceNow: _referenceNow,
        ),
      ),
    ),
  );
  await _settleVisualAssets(tester);
  final appointment = find.byKey(
    const ValueKey('agenda-board-appointment-ux-audit-appointment'),
  );
  await tester.ensureVisible(appointment);
  await tester.tap(appointment);
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('agenda-clear-selection')), findsOneWidget);
  expect(tester.takeException(), isNull);
  await _saveCapture(tester, captureKey, fileName);
}

Future<void> _capturePaymentDialog(
  WidgetTester tester, {
  required Size size,
  required String fileName,
}) async {
  _setViewport(tester, size);
  final controller = _seededController()..navigate(AgendaPage.finance);
  const captureKey = Key('ux-audit-payment-capture');

  await tester.pumpWidget(
    RepaintBoundary(
      key: captureKey,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(
          controller: controller,
          referenceNow: _referenceNow,
        ),
      ),
    ),
  );
  await _settleVisualAssets(tester);
  final receive = find.byKey(const Key('finance-quick-receive'));
  await tester.ensureVisible(receive);
  await tester.pumpAndSettle();
  await tester.tap(receive);
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('finance-payment-dialog')), findsOneWidget);
  expect(tester.takeException(), isNull);
  await _saveCapture(tester, captureKey, fileName);
}

Future<void> _captureCustomerEditor(
  WidgetTester tester, {
  required Size size,
  required String fileName,
}) async {
  _setViewport(tester, size);
  final controller = _seededController();
  final customer = controller.data.customers.first;
  const captureKey = Key('ux-audit-customer-capture');

  await tester.pumpWidget(
    RepaintBoundary(
      key: captureKey,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AgendaThemes.byId('').toThemeData(),
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: ElevatedButton(
                key: const Key('open-customer-editor'),
                onPressed: () => showCustomerEditorDialog(
                  context,
                  controller: controller,
                  customer: customer,
                ),
                child: const Text('Editar cliente'),
              ),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.byKey(const Key('open-customer-editor')));
  await tester.pumpAndSettle();
  expect(find.text('Editar cliente'), findsWidgets);
  expect(tester.takeException(), isNull);
  await _saveCapture(tester, captureKey, fileName);
}

AgendaController _seededController() {
  final data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 19));
  data.professionals.removeWhere(
    (item) => item.id == 'professional-manicure-1',
  );
  final service = data.services.first;
  final professional = data.professionals.first;
  data.customers.add(
    Customer(
      id: 'ux-audit-customer',
      name: 'Mariana Costa',
      phone: '(33) 99876-5432',
      email: 'mariana@example.com',
      segment: 'Centro de Estética',
      profile: 'Preferência de horário: Tarde.\nPele sensível.',
      tags: 'VIP, retorno',
      notes: 'Confirmar pelo WhatsApp antes do atendimento.',
      lastSeenAt: DateTime(2026, 7, 10, 14, 30),
    ),
  );
  data.appointments.add(
    Appointment(
      id: 'ux-audit-appointment',
      segment: service.segment,
      customerName: 'Mariana Costa',
      customerPhone: '(33) 99876-5432',
      customerProfile: 'Prefere atendimento no fim da tarde',
      serviceId: service.id,
      serviceName: service.name,
      professionalId: professional.id,
      professionalName: professional.name,
      resourceName: service.defaultResource,
      start: DateTime(2026, 7, 19, 10),
      durationMinutes: service.durationMinutes,
      price: service.price,
      status: AppointmentStatus.confirmed,
      notes: 'Primeira visita — confirmar pelo WhatsApp.',
      createdAt: DateTime(2026, 7, 18, 9, 30),
      updatedAt: DateTime(2026, 7, 18, 9, 30),
    ),
  );
  data.settings
    ..themeId = ''
    ..accountFullName = 'Marina Teste'
    ..businessName = 'Studio Fluxo'
    ..onboardingCompleted = true;
  return AgendaController(_MemoryAgendaRepository())
    ..data = data
    ..loading = false
    ..selectedDate = DateTime(2026, 7, 19);
}

void _setViewport(WidgetTester tester, Size size) {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
}

Future<void> _saveCapture(
  WidgetTester tester,
  Key captureKey,
  String fileName,
) async {
  await expectLater(
    find.byKey(captureKey),
    matchesGoldenFile('../artifacts/ux-product-audit-2026-07-21/$fileName'),
  );
}

Future<void> _settleVisualAssets(WidgetTester tester) async {
  await tester.pump();
  final context = tester.element(find.byType(MaterialApp).first);
  await tester.runAsync(() async {
    await Future.wait([
      precacheImage(
        const AssetImage('assets/branding/agenda-livre-logo-source.png'),
        context,
      ),
      precacheImage(
        const AssetImage('assets/branding/agenda-livre-mark.png'),
        context,
      ),
    ]).timeout(const Duration(seconds: 10));
  });
  await tester.pumpAndSettle();
  await tester.pump(const Duration(milliseconds: 50));
}

Future<void> _loadFont(String family, String path) async {
  final loader = FontLoader(family);
  final bytes = await File(path).readAsBytes();
  loader.addFont(Future<ByteData>.value(ByteData.sublistView(bytes)));
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
