import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/establishment/establishment_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

const _output = 'artifacts/establishment-wpf-parity-2026-07-30';

void main() {
  setUpAll(() async {
    final bytes = File(r'C:\Windows\Fonts\segoeui.ttf').readAsBytesSync();
    await (FontLoader(
      'Segoe UI',
    )..addFont(Future.value(ByteData.sublistView(bytes)))).load();
    await (FontLoader(
      'Ahem',
    )..addFont(Future.value(ByteData.sublistView(bytes)))).load();
    final materialIcons = File(
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    ).readAsBytesSync();
    await (FontLoader(
      'MaterialIcons',
    )..addFont(Future.value(ByteData.sublistView(materialIcons)))).load();
    Directory(_output).createSync(recursive: true);
  });

  for (final size in const [Size(390, 844), Size(320, 568)]) {
    final suffix = 'mobile-${size.width.toInt()}';
    testWidgets('Meu estabelecimento otimiza o web mobile em $suffix', (
      tester,
    ) async {
      final data = _fixture();
      final controller = AgendaController(_MemoryAgendaRepository(data))
        ..data = data
        ..loading = false;
      const captureKey = Key('establishment-parity-capture');

      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = size;
      addTearDown(tester.view.reset);

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
            home: Scaffold(body: EstablishmentPage(controller: controller)),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('establishment-wpf-header')), findsOneWidget);
      expect(
        find.byKey(const Key('establishment-wpf-metrics')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('establishment-movement-card')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('establishment-catalog-card')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('establishment-month-card')), findsOneWidget);
      expect(find.text('Movimento recente'), findsOneWidget);
      expect(find.text('Catálogo em destaque'), findsOneWidget);
      expect(find.text('Estrutura cadastrada'), findsOneWidget);
      expect(find.text('Serviços no catálogo'), findsOneWidget);
      expect(find.text('Meta de faturamento'), findsOneWidget);
      expect(tester.takeException(), isNull);

      await expectLater(
        find.byKey(captureKey),
        matchesGoldenFile('../../$_output/flutter-establishment-$suffix.png'),
      );
    });
  }

  testWidgets('mantém o estabelecimento legado no desktop', (tester) async {
    final data = _fixture();
    final controller = AgendaController(_MemoryAgendaRepository(data))
      ..data = data
      ..loading = false;
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1366, 768);
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: Scaffold(body: EstablishmentPage(controller: controller)),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Meu estabelecimento'), findsOneWidget);
    expect(find.byKey(const Key('establishment-wpf-metrics')), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('edita a meta mensal no fluxo do estabelecimento', (
    tester,
  ) async {
    final data = _fixture();
    final controller = AgendaController(_MemoryAgendaRepository(data))
      ..data = data
      ..loading = false;
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: Scaffold(body: EstablishmentPage(controller: controller)),
      ),
    );
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.widgetWithText(TextButton, 'Editar'));
    await tester.tap(find.widgetWithText(TextButton, 'Editar'));
    await tester.pumpAndSettle();

    expect(find.text('Meta de faturamento'), findsWidgets);
    await tester.enterText(
      find.byKey(const Key('establishment-revenue-goal-field')),
      '5000',
    );
    await tester.tap(find.text('Salvar meta'));
    await tester.pumpAndSettle();

    expect(controller.data.settings.monthlyRevenueGoal, 5000);
    expect(tester.takeException(), isNull);
  });
}

AgendaData _fixture() {
  final data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 30));
  data.settings
    ..themeId = ''
    ..businessName = 'Studio Nina Beauty'
    ..businessSegment = 'Salão de Beleza'
    ..monthlyRevenueGoal = 2000;
  data.customers
    ..clear()
    ..addAll([
      Customer(
        name: 'Luana Ribeiro',
        phone: '(11) 98888-1101',
        profile: 'Recorrente',
        lastSeenAt: DateTime(2026, 7, 29),
      ),
      Customer(
        name: 'Fernanda Nunes',
        phone: '(11) 97777-2202',
        profile: 'VIP',
        lastSeenAt: DateTime(2026, 7, 28),
      ),
      Customer(
        name: 'Carolina Mendes',
        phone: '(11) 96666-3303',
        profile: 'Nova',
        lastSeenAt: DateTime(2026, 7, 26),
      ),
      Customer(
        name: 'Renata Alves',
        phone: '(11) 95555-4404',
        profile: 'Recorrente',
        lastSeenAt: DateTime(2026, 7, 24),
      ),
    ]);
  return data;
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
