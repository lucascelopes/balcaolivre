import 'dart:io';
import 'dart:ui' as ui;

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/onboarding/onboarding_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

final _referenceNow = DateTime(2026, 7, 19, 9);

void main() {
  setUpAll(() async {
    await initializeDateFormatting('pt_BR');
    await _loadSegoeUi();
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
    await _loadFontAwesome();
    Directory('artifacts/visual-audit-current').createSync(recursive: true);
  });

  for (final size in <Size>[const Size(1200, 640), const Size(390, 844)]) {
    testWidgets(
      'captura shell e Home reais em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
        tester.view.devicePixelRatio = 1;
        tester.view.physicalSize = size;
        addTearDown(tester.view.reset);

        final controller = _seededController();
        const captureKey = Key('visual-audit-capture');

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

        expect(tester.takeException(), isNull);
        if (size.width == 1200) {
          expect(
            tester.getSize(find.byKey(const Key('home-hero'))).height,
            176,
          );
        } else {
          expect(find.byType(BottomNavigationBar), findsNothing);
          expect(find.byType(NavigationBar), findsOneWidget);

          final first = tester.getCenter(
            find.descendant(
              of: find.byKey(const Key('home-metrics')),
              matching: find.textContaining('Agendamentos'),
            ),
          );
          final second = tester.getCenter(find.text('Confirmados'));
          final third = tester.getCenter(find.text('A confirmar'));
          final fourth = tester.getCenter(
            find.descendant(
              of: find.byKey(const Key('home-metrics')),
              matching: find.textContaining('Caixa'),
            ),
          );
          expect((first.dy - second.dy).abs(), lessThan(2));
          expect((third.dy - fourth.dy).abs(), lessThan(2));
          expect(first.dx, lessThan(second.dx));
          expect(third.dx, lessThan(fourth.dx));
          expect(first.dy, lessThan(third.dy));
        }
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile(
            '../artifacts/visual-audit-current/'
            'flutter-home-current-${size.width.toInt()}x${size.height.toInt()}.png',
          ),
        );
      },
    );
  }

  for (final size in <Size>[const Size(1200, 640), const Size(390, 844)]) {
    testWidgets(
      'captura escolha de tema real em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
        tester.view.devicePixelRatio = 1;
        tester.view.physicalSize = size;
        addTearDown(tester.view.reset);

        final controller = AgendaController(_MemoryAgendaRepository())
          ..data = AgendaData(
            settings: AgendaSettings(
              businessName: 'Studio Fluxo',
              onboardingCompleted: false,
            ),
          )
          ..loading = false;
        const captureKey = Key('visual-audit-theme-capture');

        await tester.pumpWidget(
          RepaintBoundary(
            key: captureKey,
            child: MaterialApp(
              debugShowCheckedModeBanner: false,
              theme: AgendaThemes.byId('').toThemeData(),
              home: OnboardingPage(controller: controller),
            ),
          ),
        );
        await _settleVisualAssets(tester);
        expect(tester.takeException(), isNull);
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile(
            '../artifacts/visual-audit-current/'
            'flutter-onboarding-initial-${size.width.toInt()}x${size.height.toInt()}.png',
          ),
        );

        await _enterOnboardingText(
          tester,
          const Key('onboarding-name-field'),
          'Marina Teste',
        );
        await _enterOnboardingText(
          tester,
          const Key('onboarding-phone-field'),
          '(33) 99131-4125',
        );
        await _enterOnboardingText(
          tester,
          const Key('onboarding-email-field'),
          'contato@studiofluxo.com.br',
        );
        await _enterOnboardingText(
          tester,
          const Key('onboarding-business-field'),
          'Studio Fluxo',
        );
        await _tapOnboarding(tester, const Key('onboarding-primary'));
        await _tapOnboarding(tester, const Key('onboarding-segment-salao'));

        expect(find.text('Agora, escolha o seu estilo'), findsOneWidget);
        expect(tester.takeException(), isNull);
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile(
            '../artifacts/visual-audit-current/'
            'flutter-theme-current-${size.width.toInt()}x${size.height.toInt()}.png',
          ),
        );
      },
    );
  }

  for (final size in <Size>[const Size(1200, 640), const Size(390, 844)]) {
    testWidgets(
      'captura shell e Agenda reais em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
        tester.view.devicePixelRatio = 1;
        tester.view.physicalSize = size;
        addTearDown(tester.view.reset);

        final controller = _seededController()..navigate(AgendaPage.agenda);
        const captureKey = Key('visual-audit-agenda-capture');

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

        expect(tester.takeException(), isNull);
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile(
            '../artifacts/visual-audit-current/'
            'flutter-agenda-current-${size.width.toInt()}x${size.height.toInt()}.png',
          ),
        );
      },
    );
  }

  for (final size in <Size>[
    const Size(1366, 768),
    const Size(1200, 640),
    const Size(390, 844),
  ]) {
    testWidgets(
      'captura shell e Financeiro reais em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
        tester.view.devicePixelRatio = 1;
        tester.view.physicalSize = size;
        addTearDown(tester.view.reset);

        final controller = _seededController()..navigate(AgendaPage.finance);
        const captureKey = Key('visual-audit-finance-capture');

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

        expect(tester.takeException(), isNull);
        await expectLater(
          find.byKey(captureKey),
          matchesGoldenFile(
            '../artifacts/visual-audit-current/'
            'flutter-finance-current-${size.width.toInt()}x${size.height.toInt()}.png',
          ),
        );
      },
    );
  }

  testWidgets('recorta apenas o client WPF em 1200x608', (tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1200, 608);
    addTearDown(tester.view.reset);

    final wpf = File(
      '../AgendaLivre.Windows/artifacts/shell-home-wpf-parity/'
      'wpf-home-current-1200x640.png',
    ).readAsBytesSync();
    final decodedWpf = await tester.runAsync(() async {
      final codec = await ui.instantiateImageCodec(wpf);
      return (codec, await codec.getNextFrame());
    });
    final (wpfCodec, wpfFrame) = decodedWpf!;
    addTearDown(() {
      wpfFrame.image.dispose();
      wpfCodec.dispose();
    });
    const captureKey = Key('visual-audit-wpf-client');

    await tester.pumpWidget(
      RepaintBoundary(
        key: captureKey,
        child: SizedBox(
          width: 1200,
          height: 608,
          child: ClipRect(
            child: Transform.translate(
              offset: const Offset(0, -32),
              child: RawImage(
                image: wpfFrame.image,
                width: 1200,
                height: 640,
                fit: BoxFit.fill,
                filterQuality: FilterQuality.none,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await expectLater(
      find.byKey(captureKey),
      matchesGoldenFile(
        '../artifacts/visual-audit-current/'
        'wpf-home-client-current-1200x608.png',
      ),
    );
  });

  testWidgets('captura Flutter Home no mesmo client 1200x608', (tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1200, 608);
    addTearDown(tester.view.reset);

    final controller = _seededController();
    const captureKey = Key('visual-audit-flutter-client');

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

    expect(tester.takeException(), isNull);
    await expectLater(
      find.byKey(captureKey),
      matchesGoldenFile(
        '../artifacts/visual-audit-current/'
        'flutter-home-client-current-1200x608.png',
      ),
    );
  });

  testWidgets('monta comparativo WPF e Flutter normalizado sem canvas vazio', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(2400, 608);
    addTearDown(tester.view.reset);

    final wpf = File(
      'artifacts/visual-audit-current/'
      'wpf-home-client-current-1200x608.png',
    ).readAsBytesSync();
    final flutter = File(
      'artifacts/visual-audit-current/'
      'flutter-home-client-current-1200x608.png',
    ).readAsBytesSync();
    final decodedImages = await tester.runAsync(() async {
      final decodedWpf = await ui.instantiateImageCodec(wpf);
      final decodedFlutter = await ui.instantiateImageCodec(flutter);
      return (
        decodedWpf,
        await decodedWpf.getNextFrame(),
        decodedFlutter,
        await decodedFlutter.getNextFrame(),
      );
    });
    final (wpfCodec, wpfFrame, flutterCodec, flutterFrame) = decodedImages!;
    addTearDown(() {
      wpfFrame.image.dispose();
      flutterFrame.image.dispose();
      wpfCodec.dispose();
      flutterCodec.dispose();
    });
    const captureKey = Key('visual-audit-comparison');

    await tester.pumpWidget(
      RepaintBoundary(
        key: captureKey,
        child: Directionality(
          textDirection: TextDirection.ltr,
          child: Row(
            children: [
              RawImage(
                image: wpfFrame.image,
                width: 1200,
                height: 608,
                fit: BoxFit.fill,
                filterQuality: FilterQuality.none,
              ),
              RawImage(
                image: flutterFrame.image,
                width: 1200,
                height: 608,
                fit: BoxFit.fill,
                filterQuality: FilterQuality.none,
              ),
            ],
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await expectLater(
      find.byKey(captureKey),
      matchesGoldenFile(
        '../artifacts/visual-audit-current/'
        'comparison-wpf-vs-flutter-normalized-2400x608.png',
      ),
    );
  });
}

AgendaController _seededController() {
  final data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 19));
  data.professionals.removeWhere(
    (item) => item.id == 'professional-manicure-1',
  );
  data.settings
    ..themeId = ''
    ..accountFullName = 'Marina Teste'
    ..businessName = 'Studio Fluxo';
  return AgendaController(_MemoryAgendaRepository())
    ..data = data
    ..loading = false
    ..selectedDate = DateTime(2026, 7, 19);
}

Future<void> _enterOnboardingText(
  WidgetTester tester,
  Key field,
  String value,
) async {
  final input = find.descendant(
    of: find.byKey(field),
    matching: find.byType(TextField),
  );
  await tester.ensureVisible(input);
  await tester.enterText(input, value);
  await tester.pump();
}

Future<void> _tapOnboarding(WidgetTester tester, Key key) async {
  final finder = find.byKey(key);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
  await tester.tap(finder);
  await tester.pumpAndSettle();
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
      precacheImage(
        const AssetImage('assets/branding/onboarding-theme.png'),
        context,
      ),
      precacheImage(
        const AssetImage('assets/branding/onboarding-store-calendar.png'),
        context,
      ),
      precacheImage(
        const AssetImage('assets/themes/default-warm.png'),
        context,
      ),
      precacheImage(
        const AssetImage('assets/themes/salon-classic-gold.png'),
        context,
      ),
      precacheImage(
        const AssetImage('assets/themes/salon-lilac-glow.png'),
        context,
      ),
      precacheImage(
        const AssetImage('assets/themes/salon-rose-luxe.png'),
        context,
      ),
    ]).timeout(const Duration(seconds: 10));
  });
  await tester.pumpAndSettle();
  await tester.pump(const Duration(milliseconds: 50));
}

Future<void> _loadSegoeUi() async {
  // Um unico rosto deixa o rasterizador de teste sintetizar os pesos. Ao
  // registrar Semibold/Bold sem descritores, alguns ButtonStyle voltam ao Ahem.
  await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
  // O binding de widget tests usa Ahem como fallback para estilos de botao
  // sem fontFamily. Substitui-lo aqui reproduz o fallback tipografico do Web.
  await _loadFont('Ahem', r'C:\Windows\Fonts\segoeui.ttf');
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

Future<void> _loadFont(String family, String path) async {
  final loader = FontLoader(family);
  final bytes = await File(path).readAsBytes();
  loader.addFont(Future<ByteData>.value(ByteData.sublistView(bytes)));
  await loader.load();
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
