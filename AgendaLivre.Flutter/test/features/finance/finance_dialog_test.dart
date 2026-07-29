import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/finance/finance_page.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../services/fake_http_transport.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('pt_BR');
    await _loadTestFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadTestFont('Ahem', r'C:\Windows\Fonts\segoeui.ttf');
  });

  testWidgets('mantém as dimensões desktop dos diálogos financeiros', (
    tester,
  ) async {
    final harness = await _pumpFinance(tester, const Size(1382, 736));
    expect(harness.initialLayoutException, isNull);

    await _openPaymentDialog(tester);
    expect(
      tester.getSize(find.byKey(const Key('finance-payment-dialog'))),
      const Size(1040, 620),
    );
    expect(tester.takeException(), isNull);
    await _closeDialog(tester);

    await _openExpenseDialog(tester);
    expect(
      tester.getSize(find.byKey(const Key('finance-expense-dialog'))),
      const Size(1040, 620),
    );
    expect(tester.takeException(), isNull);
  });

  for (final size in <Size>[
    const Size(390, 844),
    const Size(320, 568),
    const Size(844, 390),
  ]) {
    testWidgets('pagamento responde sem overflow e mantém rodapé em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      final harness = await _pumpFinance(tester, size);
      await _openPaymentDialog(tester);

      _expectResponsiveDialog(
        tester,
        size: size,
        dialogKey: const Key('finance-payment-dialog'),
        desktopWidth: 1040,
        firstFieldKey: const Key('payment-description-field'),
        pairedFieldKey: const Key('payment-customer-field'),
        stackedOnDesktop: true,
      );
      await _expectFixedFooterWhileScrolling(
        tester,
        firstFieldKey: const Key('payment-description-field'),
      );
      expect(
        harness.initialLayoutException,
        isNull,
        reason: 'A página financeira não deve ter overflow antes do diálogo.',
      );
    });

    testWidgets('despesa responde sem overflow e mantém rodapé em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      final harness = await _pumpFinance(tester, size);
      await _openExpenseDialog(tester);

      _expectResponsiveDialog(
        tester,
        size: size,
        dialogKey: const Key('finance-expense-dialog'),
        desktopWidth: 1040,
        firstFieldKey: const Key('expense-description-field'),
        pairedFieldKey: const Key('expense-category-field'),
        stackedOnDesktop: true,
      );
      await _expectFixedFooterWhileScrolling(
        tester,
        firstFieldKey: const Key('expense-description-field'),
      );
      expect(
        harness.initialLayoutException,
        isNull,
        reason: 'A página financeira não deve ter overflow antes do diálogo.',
      );
    });
  }

  testWidgets('salva uma nova despesa com os valores preenchidos', (
    tester,
  ) async {
    final harness = await _pumpFinance(tester, const Size(1382, 736));
    expect(harness.initialLayoutException, isNull);
    final before = DateTime.now();
    await _openExpenseDialog(tester);

    await tester.enterText(
      _textFieldInside(const Key('expense-description-field')),
      'Material de limpeza',
    );
    await tester.enterText(
      _textFieldInside(const Key('expense-supplier-field')),
      'Fornecedor local',
    );
    await tester.enterText(
      _textFieldInside(const Key('expense-value-field')),
      '120,50',
    );
    await tester.tap(find.byKey(const Key('finance-dialog-save')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('finance-expense-dialog')), findsNothing);
    expect(harness.controller.data.expenses, hasLength(1));
    expect(harness.repository.saveCalls, 1);
    final expense = harness.controller.data.expenses.single;
    expect(expense.description, 'Material de limpeza');
    expect(expense.supplier, 'Fornecedor local');
    expect(expense.category, 'Operacional');
    expect(expense.paymentMethod, 'Pix');
    expect(expense.value, closeTo(120.50, .001));
    expect(expense.isPaid, isTrue);
    expect(expense.date.isBefore(before), isFalse);
    expect(find.text('Despesa cadastrada com sucesso.'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('só registra entrada Mercado Pago depois da aprovação Point', (
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
            '{"ok":true,"attemptId":"A1","orderId":"O1","paymentId":"P1","status":"approved","paid":true}',
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
    final harness = await _pumpFinance(
      tester,
      const Size(1382, 736),
      mercadoPagoService: service,
    );
    harness.controller.data.settings
      ..mercadoPagoEnabled = true
      ..mercadoPagoConnected = true
      ..mercadoPagoDefaultTerminalId = 'T1'
      ..mercadoPagoDefaultTerminalLabel = 'Point balcão';
    await _openPaymentDialog(tester);

    await tester.enterText(
      _textFieldInside(const Key('payment-value-field')),
      '25,00',
    );
    await tester.tap(find.byKey(const Key('payment-method-field')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Mercado Pago - débito na maquininha').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('finance-dialog-save')));
    await tester.pump();

    expect(
      find.byKey(const Key('mercado-pago-point-progress')),
      findsOneWidget,
    );
    expect(harness.controller.data.manualPayments, isEmpty);
    await tester.pump(const Duration(milliseconds: 1400));
    await tester.pumpAndSettle();

    final payment = harness.controller.data.manualPayments.single;
    expect(payment.paymentProvider, 'Mercado Pago');
    expect(payment.paymentReference, 'P1');
    expect(payment.paymentStatus, 'approved');
  });

  testWidgets('executa as ações do cabeçalho financeiro alinhado ao WPF', (
    tester,
  ) async {
    final harness = await _pumpFinance(tester, const Size(1382, 736));

    await tester.tap(find.text('Nova movimentação'));
    await tester.pumpAndSettle();
    expect(find.text('Escolha o que deseja registrar.'), findsOneWidget);
    expect(find.text('Lançar entrada'), findsWidgets);
    expect(find.text('Lançar despesa'), findsWidgets);
    expect(find.text('Vender produto'), findsWidgets);
    Navigator.of(
      tester.element(find.text('Escolha o que deseja registrar.')),
    ).pop();
    await tester.pumpAndSettle();

    await tester.tap(find.text('Atualizar análise'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    await tester.tap(find.text('Exportar'));
    await tester.pumpAndSettle();
    expect(harness.controller.page, AgendaPage.reports);
  });

  testWidgets('renderiza o novo resumo lateral do recebimento', (tester) async {
    await _pumpFinance(tester, const Size(1358, 695));
    await _openPaymentDialog(tester);

    expect(find.byKey(const Key('payment-summary')), findsOneWidget);
    await expectLater(
      find.byType(Overlay),
      matchesGoldenFile('goldens/payment_dialog_option3.png'),
    );
  });

  testWidgets('renderiza o novo resumo lateral da despesa', (tester) async {
    await _pumpFinance(tester, const Size(1364, 686));
    await _openExpenseDialog(tester);

    expect(find.byKey(const Key('expense-summary')), findsOneWidget);
    await expectLater(
      find.byType(Overlay),
      matchesGoldenFile('goldens/expense_dialog_option3.png'),
    );
  });
}

Future<void> _loadTestFont(String family, String path) async {
  final loader = FontLoader(family);
  final bytes = await File(path).readAsBytes();
  loader.addFont(Future<ByteData>.value(ByteData.sublistView(bytes)));
  await loader.load();
}

Future<_FinanceHarness> _pumpFinance(
  WidgetTester tester,
  Size size, {
  MercadoPagoService? mercadoPagoService,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final repository = _MemoryAgendaRepository();
  final controller =
      AgendaController(repository, mercadoPagoService: mercadoPagoService)
        ..data = AgendaData()
        ..loading = false;
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Scaffold(body: FinancePage(controller: controller)),
    ),
  );
  await tester.pump();
  return _FinanceHarness(controller, repository, tester.takeException());
}

Future<void> _openPaymentDialog(WidgetTester tester) async {
  final button = find.text('Lançar entrada');
  expect(button, findsOneWidget);
  await tester.ensureVisible(button);
  await tester.pumpAndSettle();
  await tester.tap(button);
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('finance-payment-dialog')), findsOneWidget);
}

Future<void> _openExpenseDialog(WidgetTester tester) async {
  final button = find.widgetWithText(OutlinedButton, 'Lançar despesa');
  expect(button, findsOneWidget);
  await tester.ensureVisible(button);
  await tester.pumpAndSettle();
  await tester.tap(button);
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('finance-expense-dialog')), findsOneWidget);
}

Future<void> _closeDialog(WidgetTester tester) async {
  await tester.tap(find.byKey(const Key('finance-dialog-cancel')));
  await tester.pumpAndSettle();
}

void _expectResponsiveDialog(
  WidgetTester tester, {
  required Size size,
  required Key dialogKey,
  required double desktopWidth,
  required Key firstFieldKey,
  required Key pairedFieldKey,
  bool stackedOnDesktop = false,
}) {
  final compact = size.width < 650;
  final expectedWidth = compact
      ? size.width - 16
      : desktopWidth.clamp(0, size.width - 40).toDouble();
  final expectedHeight = (size.height - (compact ? 16 : 32))
      .clamp(0, 620)
      .toDouble();
  final dialog = find.byKey(dialogKey);
  final dialogRect = tester.getRect(dialog);

  expect(dialogRect.width, closeTo(expectedWidth, .1));
  expect(dialogRect.height, closeTo(expectedHeight, .1));
  expect(dialogRect.left, greaterThanOrEqualTo(0));
  expect(dialogRect.top, greaterThanOrEqualTo(0));
  expect(dialogRect.right, lessThanOrEqualTo(size.width));
  expect(dialogRect.bottom, lessThanOrEqualTo(size.height));

  final first = find.byKey(firstFieldKey);
  final paired = find.byKey(pairedFieldKey);
  if (compact || stackedOnDesktop) {
    expect(
      tester.getTopLeft(paired).dy,
      greaterThan(tester.getBottomLeft(first).dy),
    );
  } else {
    expect(
      tester.getTopLeft(paired).dy,
      closeTo(tester.getTopLeft(first).dy, .1),
    );
  }
  expect(find.byKey(const Key('finance-dialog-save')).hitTestable(), findsOne);
  expect(
    find.byKey(const Key('finance-dialog-cancel')).hitTestable(),
    findsOne,
  );
  expect(tester.takeException(), isNull);
}

Future<void> _expectFixedFooterWhileScrolling(
  WidgetTester tester, {
  required Key firstFieldKey,
}) async {
  final footer = find.byKey(const Key('finance-dialog-footer'));
  final firstField = find.byKey(firstFieldKey);
  final footerTopBefore = tester.getTopLeft(footer).dy;
  final fieldTopBefore = tester.getTopLeft(firstField).dy;

  await tester.drag(
    find.byKey(const Key('finance-dialog-scroll')),
    const Offset(0, -260),
  );
  await tester.pumpAndSettle();

  expect(tester.getTopLeft(footer).dy, closeTo(footerTopBefore, .1));
  expect(tester.getTopLeft(firstField).dy, lessThan(fieldTopBefore));
  expect(find.byKey(const Key('finance-dialog-save')).hitTestable(), findsOne);
  expect(
    find.byKey(const Key('finance-dialog-cancel')).hitTestable(),
    findsOne,
  );
  expect(tester.takeException(), isNull);
}

Finder _textFieldInside(Key key) =>
    find.descendant(of: find.byKey(key), matching: find.byType(TextFormField));

class _FinanceHarness {
  const _FinanceHarness(
    this.controller,
    this.repository,
    this.initialLayoutException,
  );

  final AgendaController controller;
  final _MemoryAgendaRepository repository;
  final Object? initialLayoutException;
}

class _MemoryAgendaRepository implements AgendaRepository {
  AgendaData? value;
  int saveCalls = 0;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value;

  @override
  Future<AgendaData> loadOrCreate() async => value ?? AgendaData();

  @override
  Future<void> save(AgendaData data) async {
    saveCalls += 1;
    value = data;
  }
}
