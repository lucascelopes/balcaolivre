import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('pt_BR');
    await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
    await _loadFontAwesome();
    Directory('artifacts/marketing-hub-2026-07-29').createSync(recursive: true);
  });

  testWidgets('hub de Marketing replica o WPF em 1366x768', (tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1366, 768);
    addTearDown(tester.view.reset);

    final data = AgendaData()..settings.businessName = 'Studio Nina Beauty';
    final controller = AgendaController(_MemoryAgendaRepository(data))
      ..data = data
      ..loading = false
      ..navigate(AgendaPage.marketing);
    const captureKey = Key('marketing-hub-golden');

    await tester.pumpWidget(
      RepaintBoundary(
        key: captureKey,
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: AgendaThemes.byId('').toThemeData().copyWith(
            textTheme: ThemeData.light().textTheme.apply(
              fontFamily: 'Segoe UI',
            ),
          ),
          home: ResponsiveAgendaShell(
            controller: controller,
            referenceNow: DateTime(2026, 7, 23, 14, 2),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    await expectLater(
      find.byKey(captureKey),
      matchesGoldenFile(
        '../../../artifacts/marketing-hub-2026-07-29/'
        'flutter-marketing-hub-1366x768.png',
      ),
    );
  });
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
    r'"name"\s*:\s*"font_awesome_flutter"[\s\S]*?'
    r'"rootUri"\s*:\s*"([^"]+)"',
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
