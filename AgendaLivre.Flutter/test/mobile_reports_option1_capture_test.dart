import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

const _output = 'artifacts/mobile-reports-option1-2026-07-30';

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
    Directory(_output).createSync(recursive: true);
  });

  for (final entry in const [
    ('estetica-coral-393x852', Size(393, 852), 'Estética', ''),
    ('clinica-azul-320x568', Size(320, 568), 'Clínica médica', 'medical-blue'),
  ]) {
    testWidgets('captura opção 1 ${entry.$1}', (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = entry.$2;
      addTearDown(tester.view.reset);
      final controller = _controller(entry.$3)..navigate(AgendaPage.reports);
      final captureKey = ValueKey('capture-${entry.$1}');

      await tester.pumpWidget(
        RepaintBoundary(
          key: captureKey,
          child: MaterialApp(
            debugShowCheckedModeBanner: false,
            theme: AgendaThemes.byId(entry.$4).toThemeData(),
            home: ResponsiveAgendaShell(
              controller: controller,
              referenceNow: DateTime(2026, 7, 30, 14),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('reports-mobile-diagnostics')),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
      await expectLater(
        find.byKey(captureKey),
        matchesGoldenFile('../$_output/${entry.$1}.png'),
      );
      if (entry.$2.width == 393) {
        await tester.ensureVisible(
          find.byKey(const Key('reports-mobile-goal')),
        );
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile('../$_output/estetica-coral-393x852-lower.png'),
        );
      }
    });
  }
}

AgendaController _controller(String segment) {
  final appointments = <Appointment>[];
  for (var index = 0; index < 15; index++) {
    final start = DateTime(2026, 7, 27 + (index % 5), 8 + (index % 8));
    appointments.add(
      Appointment(
        id: 'done-$index',
        customerName: 'Cliente ${index + 1}',
        serviceName: segment.contains('Clínica') ? 'Consulta' : 'Atendimento',
        start: start,
        price: 232,
        status: AppointmentStatus.done,
        paymentConfirmedAt: start.add(const Duration(hours: 1)),
        attendanceConfirmedAt: start.subtract(const Duration(days: 1)),
      ),
    );
  }
  for (var index = 0; index < 3; index++) {
    final start = DateTime(2026, 7, 28 + index, 17);
    appointments.add(
      Appointment(
        id: 'no-show-$index',
        customerName: 'Falta ${index + 1}',
        serviceName: 'Atendimento',
        start: start,
        status: AppointmentStatus.noShow,
        attendanceConfirmedAt: start.subtract(const Duration(days: 1)),
      ),
    );
  }
  for (var index = 0; index < 4; index++) {
    appointments.add(
      Appointment(
        id: 'pending-$index',
        customerName: ['Juliana', 'Beatriz', 'Camila', 'Rafael'][index],
        serviceName: 'Atendimento',
        start: DateTime(2026, 7, 31, 10 + index),
        status: AppointmentStatus.scheduled,
      ),
    );
  }
  for (final entry in [
    (DateTime(2026, 7, 6, 10), 2650.0),
    (DateTime(2026, 7, 13, 10), 3120.0),
  ]) {
    appointments.add(
      Appointment(
        customerName: 'Histórico',
        serviceName: 'Atendimento',
        start: entry.$1,
        price: entry.$2,
        status: AppointmentStatus.done,
        paymentConfirmedAt: entry.$1,
      ),
    );
  }
  for (var index = 0; index < 14; index++) {
    final start = DateTime(2026, 7, 20 + (index % 5), 8 + (index % 7));
    appointments.add(
      Appointment(
        customerName: 'Histórico ${index + 1}',
        serviceName: 'Atendimento',
        start: start,
        price: 2780 / 14,
        status: AppointmentStatus.done,
        paymentConfirmedAt: start,
      ),
    );
  }
  for (var index = 0; index < 2; index++) {
    appointments.add(
      Appointment(
        customerName: 'Confirmado ${index + 1}',
        serviceName: 'Atendimento',
        start: DateTime(2026, 7, 24, 16 + index),
        status: AppointmentStatus.confirmed,
      ),
    );
    appointments.add(
      Appointment(
        customerName: 'Pendente ${index + 1}',
        serviceName: 'Atendimento',
        start: DateTime(2026, 7, 25, 10 + index),
        status: AppointmentStatus.scheduled,
      ),
    );
  }
  return AgendaController(_MemoryAgendaRepository())
    ..data = AgendaData(
      settings: AgendaSettings(
        accountFullName: 'Marina Teste',
        businessName: segment.contains('Clínica')
            ? 'Clínica Horizonte'
            : 'Studio Fluxo',
        businessSegment: segment,
        monthlyRevenueGoal: 24332,
        onboardingCompleted: true,
      ),
      appointments: appointments,
    )
    ..loading = false
    ..selectedDate = DateTime(2026, 7, 30);
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
    'packages/font_awesome_flutter/FontAwesomeSolid',
    '$root\\lib\\fonts\\Font-Awesome-7-Free-Solid-900.otf',
  );
  await _loadFont(
    'packages/font_awesome_flutter/FontAwesomeRegular',
    '$root\\lib\\fonts\\Font-Awesome-7-Free-Regular-400.otf',
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
