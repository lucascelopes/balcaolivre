// This is a basic Flutter widget test.
//
// To perform an interaction with a widget in your test, use the WidgetTester
// utility in the flutter_test package. For example, you can send tap and scroll
// gestures. You can also use WidgetTester to find child widgets in the widget
// tree, read text, and verify that the values of widget properties are correct.

import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/app.dart';
import 'package:balcao_livre_flutter/src/store.dart';

const captureQaGoldens = bool.fromEnvironment('BALCAO_CAPTURE_GOLDENS');

Future<void> captureQaFrame(String fileName) async {
  if (!captureQaGoldens) return;
  await expectLater(
    find.byKey(const Key('qaCaptureFrame')),
    matchesGoldenFile('../design-qa-artifacts/$fileName'),
  );
}

ThemeData qaCaptureTheme() {
  return ThemeData(
    useMaterial3: true,
    fontFamily: 'Segoe UI',
    colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFFFC601D)),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: const Color(0xFFFC601D),
        foregroundColor: const Color(0xFF202020),
      ),
    ),
  );
}

void main() {
  setUpAll(() async {
    final fontFile = File(r'C:\Windows\Fonts\segoeui.ttf');
    if (fontFile.existsSync()) {
      final bytes = await fontFile.readAsBytes();
      final loader = FontLoader('Segoe UI')
        ..addFont(
          Future<ByteData>.value(
            ByteData.view(
              bytes.buffer,
              bytes.offsetInBytes,
              bytes.lengthInBytes,
            ),
          ),
        );
      await loader.load();
    }
    final materialIcons = File(
      r'C:\flutter\bin\cache\artifacts\material_fonts\MaterialIcons-Regular.otf',
    );
    if (materialIcons.existsSync()) {
      final bytes = await materialIcons.readAsBytes();
      final loader = FontLoader('MaterialIcons')
        ..addFont(
          Future<ByteData>.value(
            ByteData.view(
              bytes.buffer,
              bytes.offsetInBytes,
              bytes.lengthInBytes,
            ),
          ),
        );
      await loader.load();
    }
  });

  testWidgets('Balcao Livre app opens login', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const BalcaoLivreApp());
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.text('Entrada no PDV'), findsOneWidget);
    expect(find.text('Entrar'), findsOneWidget);
  });

  testWidgets('desktop follows WPF operational navigation', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1600, 900);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(MaterialApp(home: HomeScreen(store: store)));
    await tester.pumpAndSettle();

    expect(find.text('Balcao Livre PDV'), findsOneWidget);
    expect(find.text('Comanda'), findsWidgets);
    expect(find.text('Cozinha'), findsOneWidget);
    expect(find.text('Delivery'), findsWidgets);

    await tester.tap(find.text('Cozinha'));
    await tester.pumpAndSettle();

    expect(find.text('Cozinha por praça'), findsOneWidget);
    expect(find.text('Forno'), findsWidgets);
    expect(find.text('Fritadeira'), findsWidgets);
    expect(find.text('Montagem'), findsWidgets);

    await tester.tap(find.text('Delivery'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Fila de pedidos delivery'), findsOneWidget);
    expect(find.text('Pedido selecionado'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mobile keeps the same WPF areas without overflow', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(MaterialApp(home: HomeScreen(store: store)));
    await tester.pumpAndSettle();

    expect(find.text('Comanda'), findsWidgets);
    expect(find.text('Cozinha'), findsOneWidget);
    expect(find.text('Delivery'), findsWidgets);

    await tester.tap(find.text('Cozinha'));
    await tester.pumpAndSettle();

    expect(find.text('Cozinha por praça'), findsOneWidget);
    expect(tester.takeException(), isNull);

    final deliveryTab = find.text('Delivery').last;
    await tester.ensureVisible(deliveryTab);
    await tester.pumpAndSettle();
    await tester.tap(deliveryTab);
    await tester.pumpAndSettle();

    expect(find.textContaining('Fila de pedidos delivery'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('visual QA captures the current WPF-style desktop shell', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1536, 816);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = true;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Painel do caixa'), findsOneWidget);
    expect(find.text('Comanda'), findsWidgets);
    expect(find.text('MESAS / COMANDAS'), findsOneWidget);
    expect(find.text('Fechar conta'), findsOneWidget);
    expect(find.text('Venda rapida'), findsNothing);
    expect(tester.takeException(), isNull);
    await captureQaFrame('wpf-parity-desktop-1536x816.png');

    await tester.tap(find.text('Delivery').first);
    await tester.pumpAndSettle();
    expect(find.textContaining('Fila de pedidos delivery'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await captureQaFrame('flutter-operation-delivery-1536x816.png');
  });

  testWidgets('visual QA captures kitchen and delivery at the WPF viewport', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = true;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Cozinha').first);
    await tester.pumpAndSettle();
    expect(find.text('Cozinha por praça'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await captureQaFrame('flutter-operation-cozinha-1920x1020.png');

    await tester.tap(find.text('Delivery').first);
    await tester.pumpAndSettle();
    expect(find.textContaining('Fila de pedidos delivery'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await captureQaFrame('flutter-operation-delivery-1920x1020.png');
  });

  testWidgets('visual QA captures the Mesas Produtos Conta mobile flow', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = true;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Mesas'), findsOneWidget);
    expect(find.text('Produtos'), findsOneWidget);
    expect(find.text('Conta'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await captureQaFrame('wpf-parity-mobile-390x844.png');
  });

  testWidgets('closed cash dashboard matches WPF on desktop', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = false;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Caixa fechado'), findsWidgets);
    expect(find.text('Vendas x lucro por dia'), findsOneWidget);
    expect(find.text('Análise da semana'), findsOneWidget);
    expect(find.text('Antes de abrir'), findsOneWidget);
    expect(find.text('Lucro 7 dias'), findsOneWidget);
    expect(find.byKey(const Key('topCashAction')), findsOneWidget);
    expect(find.text('Abrir caixa'), findsWidgets);
    await captureQaFrame('flutter-operation-closed-cash-1920x1020.png');
    expect(tester.takeException(), isNull);
  });

  testWidgets('closed cash opens with the WPF operator and initial cash flow', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = false;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('cashClosedOpenButton')));
    await tester.pumpAndSettle();

    expect(
      find.text('Informe quanto dinheiro vivo existe no restaurante agora.'),
      findsOneWidget,
    );
    expect(find.text('Dinheiro vivo inicial'), findsOneWidget);
    await captureQaFrame('flutter-cash-open-dialog-1920x1020.png');
    await tester.enterText(
      find.byKey(const Key('cashOpenInitialAmount')),
      '100,00',
    );
    await tester.tap(find.byKey(const Key('cashOpenConfirm')));
    await tester.pumpAndSettle();

    expect(store.cashOpen, isTrue);
    expect(find.text('MESAS / COMANDAS'), findsOneWidget);
    expect(find.text('FILTROS'), findsOneWidget);
    expect(find.text('Livres'), findsOneWidget);
    expect(find.text('Ocupadas'), findsOneWidget);
    expect(find.text('Conta'), findsOneWidget);
    expect(find.byKey(const Key('topCashAction')), findsOneWidget);
    expect(find.text('Fechar caixa'), findsWidgets);
    expect(
      store.movements
          .firstWhere((movement) => movement.type == 'ABERTURA')
          .amount,
      100,
    );
    await captureQaFrame('flutter-operation-open-cash-1920x1020.png');
    expect(tester.takeException(), isNull);
  });

  testWidgets('closed cash dashboard stays usable at 390x844', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = false;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Caixa fechado'), findsWidgets);
    expect(find.text('Vendas x lucro por dia'), findsOneWidget);
    expect(find.text('Painel'), findsOneWidget);
    expect(find.text('Relatórios'), findsOneWidget);
    expect(find.text('Estoque'), findsWidgets);
    expect(find.text('Caixa'), findsOneWidget);
    expect(find.text('Comanda'), findsNothing);
    await captureQaFrame('flutter-operation-closed-cash-mobile-390x844.png');
    expect(tester.takeException(), isNull);
  });

  testWidgets('cash opening flow stays usable at 390x844', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = false;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('cashClosedOpenButton')));
    await tester.pumpAndSettle();

    expect(find.text('Abrir caixa'), findsWidgets);
    expect(find.text('Dinheiro vivo inicial'), findsOneWidget);
    await captureQaFrame('flutter-cash-open-dialog-mobile-390x844.png');

    await tester.enterText(
      find.byKey(const Key('cashOpenInitialAmount')),
      '100,00',
    );
    await tester.tap(find.byKey(const Key('cashOpenConfirm')));
    await tester.pumpAndSettle();

    expect(store.cashOpen, isTrue);
    expect(find.text('Comanda'), findsOneWidget);
    expect(find.text('Mesas'), findsOneWidget);
    expect(find.text('Produtos'), findsOneWidget);
    expect(find.text('Conta'), findsOneWidget);
    await captureQaFrame('flutter-operation-open-cash-mobile-390x844.png');
    expect(tester.takeException(), isNull);
  });

  testWidgets('stock matches the WPF management page and saves movements', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = false;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Estoque').first);
    await tester.pumpAndSettle();

    expect(find.text('Controle de estoque'), findsWidgets);
    expect(find.text('Custo total em estoque'), findsOneWidget);
    expect(find.text('Venda potencial'), findsWidgets);
    expect(find.text('Lucro bruto estimado'), findsOneWidget);
    expect(find.byKey(const Key('stockChangePhoto')), findsOneWidget);
    expect(find.byKey(const Key('stockRegisterMovement')), findsOneWidget);
    await captureQaFrame('flutter-stock-1920x1020.png');

    final product = store.products.first;
    final previousStock = product.stock;
    await tester.enterText(find.byKey(const Key('stockMovementQuantity')), '3');
    await tester.tap(find.byKey(const Key('stockRegisterMovement')));
    await tester.pumpAndSettle();

    expect(product.stock, previousStock + 3);
    expect(store.stockMovements.first.productId, product.id);
    await tester.tap(find.text('Movimentações'));
    await tester.pumpAndSettle();
    expect(find.textContaining('+3'), findsWidgets);
    expect(tester.takeException(), isNull);
  });

  testWidgets('stock stays usable in the mobile closed-cash navigation', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = false;

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('mobileClosedStock')));
    await tester.pumpAndSettle();

    expect(find.text('Controle de estoque'), findsWidgets);
    expect(find.byKey(const Key('stockSearch')), findsOneWidget);
    await captureQaFrame('flutter-stock-mobile-390x844.png');
    expect(tester.takeException(), isNull);
  });

  testWidgets('team registration persists through the WPF ribbon flow', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(
      MaterialApp(
        theme: qaCaptureTheme(),
        home: HomeScreen(store: store),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Equipe'));
    await tester.pumpAndSettle();
    expect(find.text('Equipe cadastrada'), findsOneWidget);

    await tester.enterText(
      find.byKey(const Key('teamMemberName')),
      'Maria Souza',
    );
    await tester.tap(find.byKey(const Key('teamMemberSave')));
    await tester.pumpAndSettle();

    expect(store.teamMembers.first.name, 'MARIA SOUZA');
    expect(find.text('MARIA SOUZA'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('iFood ribbon exposes the real cloud connection flow', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(
      MaterialApp(
        theme: qaCaptureTheme(),
        home: HomeScreen(store: store),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('iFood').first);
    await tester.pumpAndSettle();

    expect(find.text('iFood Online'), findsWidgets);
    expect(find.byKey(const Key('ifoodConnect')), findsOneWidget);
    expect(find.byKey(const Key('ifoodSyncOrders')), findsOneWidget);
    expect(find.text('Simular iFood'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mobile reaches the same iFood cloud flow from More', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(
      MaterialApp(
        theme: qaCaptureTheme(),
        home: HomeScreen(store: store),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('mobileMore')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('ifoodHub')));
    await tester.pumpAndSettle();

    expect(find.text('iFood Online'), findsWidgets);
    expect(find.byKey(const Key('ifoodConnect')), findsOneWidget);
    expect(find.byKey(const Key('ifoodSyncOrders')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('WhatsApp ribbon exposes the real cloud onboarding flow', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1920, 1020);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(
      MaterialApp(
        theme: qaCaptureTheme(),
        home: HomeScreen(store: store),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('WhatsApp').first);
    await tester.pumpAndSettle();

    expect(find.text('WhatsApp Online'), findsWidgets);
    expect(find.byKey(const Key('whatsappStorePhone')), findsOneWidget);
    expect(find.byKey(const Key('whatsappConnect')), findsOneWidget);
    expect(find.byKey(const Key('whatsappRefresh')), findsOneWidget);
    expect(find.text('Confirmar conectado'), findsNothing);

    await tester.enterText(
      find.byKey(const Key('whatsappStorePhone')),
      '5533999999999',
    );
    await tester.tap(find.byKey(const Key('whatsappConnect')));
    await tester.pumpAndSettle();

    expect(
      find.text('Entre na conta da loja antes de conectar o WhatsApp.'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('mobile reaches the same WhatsApp cloud flow from More', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    await tester.pumpWidget(
      MaterialApp(
        theme: qaCaptureTheme(),
        home: HomeScreen(store: store),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('mobileMore')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('whatsappHub')));
    await tester.pumpAndSettle();

    expect(find.text('WhatsApp Online'), findsWidgets);
    expect(find.byKey(const Key('whatsappConnect')), findsOneWidget);
    expect(find.byKey(const Key('whatsappRefresh')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('cash reconciliation matches the WPF split dialog on desktop', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1600, 900);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = true;
    store.unreconciledCashOpenedAt = DateTime(2026, 7, 20, 13, 55);

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final trigger = find.text('Abrir caixa').first;
    await tester.ensureVisible(trigger);
    await tester.tap(trigger);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('cashReconciliationDialog')), findsOneWidget);
    expect(find.text('Reconciliação do caixa anterior'), findsOneWidget);
    expect(find.text('20/07/2026 · 13:55'), findsOneWidget);
    expect(find.text('Autorização do gerente'), findsOneWidget);
    expect(find.byKey(const Key('cashReconciliationOperator')), findsOneWidget);
    expect(find.byKey(const Key('cashReconciliationPassword')), findsOneWidget);
    expect(find.byKey(const Key('cashReconciliationConfirm')), findsOneWidget);
    expect(tester.takeException(), isNull);
    await captureQaFrame('cash-reconciliation-desktop.png');
  });

  testWidgets('cash reconciliation stays usable at 390x844', (
    WidgetTester tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetPhysicalSize);
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.cashOpen = false;
    store.cashReconciliationRequired = true;
    store.unreconciledCashOpenedAt = DateTime(2026, 7, 20, 13, 55);

    await tester.pumpWidget(
      RepaintBoundary(
        key: const Key('qaCaptureFrame'),
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: qaCaptureTheme(),
          home: HomeScreen(store: store),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final trigger = find.text('Abrir caixa').first;
    await tester.ensureVisible(trigger);
    await tester.tap(trigger);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('cashReconciliationDialog')), findsOneWidget);
    expect(find.text('20/07/2026 · 13:55'), findsOneWidget);
    expect(find.byKey(const Key('cashReconciliationConfirm')), findsOneWidget);
    expect(find.byKey(const Key('cashReconciliationCancel')), findsOneWidget);
    await captureQaFrame('cash-reconciliation-mobile-390x844.png');
    expect(tester.takeException(), isNull);
  });
}
