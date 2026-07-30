import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/marketing/marketing_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

const _output = 'artifacts/marketing-mobile-segment-2026-07-30';

void main() {
  setUpAll(() async {
    await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont('Ahem', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
    await _loadFontAwesome();
    Directory(_output).createSync(recursive: true);
  });

  for (final size in const [Size(320, 568), Size(393, 852)]) {
    final suffix = '${size.width.toInt()}x${size.height.toInt()}';
    for (final state in const ['studio', 'promotion', 'catalog']) {
      testWidgets('Marketing Oficina $state em $suffix', (tester) async {
        tester.view.devicePixelRatio = 1;
        tester.view.physicalSize = size;
        addTearDown(tester.view.reset);
        final data = AgendaData(
          services: [
            ServiceItem(
              name: 'Revisão preventiva',
              category: 'Mecânica',
              durationMinutes: 60,
              price: 180,
            ),
            ServiceItem(
              name: 'Troca de óleo',
              category: 'Manutenção',
              durationMinutes: 40,
              price: 120,
            ),
          ],
        );
        data.settings
          ..businessName = 'Oficina Central'
          ..businessSegment = 'Oficina mecânica'
          ..publishedMarketingCatalog = MarketingCatalogPublication(
            title: 'Seu veículo em boas mãos',
            supportText:
                'Confira revisões, valores e duração e escolha o melhor horário.',
            sections: [
              MarketingCatalogSection(
                id: 'services',
                type: 'services',
                title: 'Serviços',
                items: [
                  MarketingCatalogSectionItem(
                    id: 'review',
                    title: 'Revisão preventiva',
                    detail: '60 min · R\$ 180',
                  ),
                  MarketingCatalogSectionItem(
                    id: 'oil',
                    title: 'Troca de óleo',
                    detail: '40 min · R\$ 120',
                  ),
                ],
              ),
            ],
          );
        final controller = AgendaController(_MemoryAgendaRepository(data))
          ..data = data
          ..loading = false;
        const captureKey = Key('marketing-mobile-segment-capture');

        await tester.pumpWidget(
          RepaintBoundary(
            key: captureKey,
            child: MaterialApp(
              debugShowCheckedModeBanner: false,
              theme: AgendaThemes.byId('aesthetic-sage').toThemeData(),
              home: Scaffold(body: MarketingPage(controller: controller)),
            ),
          ),
        );
        await tester.pumpAndSettle();

        final target = switch (state) {
          'studio' => const Key('marketing-hub-whatsapp'),
          'promotion' => const Key('marketing-hub-discount'),
          _ => const Key('marketing-hub-edit-catalog'),
        };
        final finder = find.byKey(target);
        expect(finder, findsOneWidget);
        await tester.ensureVisible(finder);
        await tester.pumpAndSettle();
        await tester.tap(finder);
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull);
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile('../../$_output/marketing-$state-$suffix.png'),
        );
      });
    }
  }
}

Future<void> _loadFont(String family, String path) async {
  final loader = FontLoader(family);
  final bytes = File(path).readAsBytesSync();
  loader.addFont(Future.value(ByteData.sublistView(bytes)));
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
