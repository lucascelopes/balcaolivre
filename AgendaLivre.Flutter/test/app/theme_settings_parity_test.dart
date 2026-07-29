import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets(
    'Configurações herda a cor do tema, mantém Mercado Pago contornado e '
    'não cruza o FAB',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(1354, 625);
      addTearDown(tester.view.reset);

      final spec = AgendaThemes.byId('aesthetic-coral');
      final controller = AgendaController(_MemoryAgendaRepository())
        ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
        ..loading = false
        ..navigate(AgendaPage.settings);

      await tester.pumpWidget(
        MaterialApp(
          theme: spec.toThemeData(),
          home: ResponsiveAgendaShell(controller: controller),
        ),
      );
      await tester.pumpAndSettle();

      final topNew = tester.widget<ElevatedButton>(
        _elevatedButtonWithText('Novo'),
      );
      final newService = tester.widget<ElevatedButton>(
        _elevatedButtonWithText('Criar serviço'),
      );
      final linkWhatsApp = tester.widget<ElevatedButton>(
        _elevatedButtonWithText('Linkar WhatsApp'),
      );
      expect(topNew.style, isNull);
      expect(newService.style?.backgroundColor, isNull);
      expect(linkWhatsApp.style?.backgroundColor, isNull);
      expect(find.widgetWithText(OutlinedButton, 'Configurar'), findsOneWidget);
      expect(find.widgetWithText(ElevatedButton, 'Configurar'), findsNothing);
      expect(
        Theme.of(
          tester.element(_elevatedButtonWithText('Criar serviço')),
        ).elevatedButtonTheme.style!.backgroundColor!.resolve({}),
        spec.tokens.accent,
      );

      final exitButton = find.byKey(const Key('settings-exit-action-button'));
      await tester.ensureVisible(exitButton);
      await tester.pumpAndSettle();
      final exitRect = tester.getRect(exitButton);
      final fabRect = tester.getRect(find.byType(FloatingActionButton));
      expect(exitRect.right, lessThan(fabRect.left));
      expect(exitRect.overlaps(fabRect), isFalse);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('abrir Configurações remove o aviso ativo sem criar outro', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1382, 736);
    addTearDown(tester.view.reset);

    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
      ..loading = false;

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pumpAndSettle();

    ScaffoldMessenger.of(
      tester.element(find.byType(Scaffold)),
    ).showSnackBar(const SnackBar(content: Text('Aviso anterior')));
    await tester.pump();
    expect(find.text('Aviso anterior'), findsOneWidget);

    controller.navigate(AgendaPage.settings);
    await tester.pump();

    expect(find.text('Aviso anterior'), findsNothing);
    expect(find.text('Configurações abertas.'), findsNothing);
    expect(tester.takeException(), isNull);
  });
}

Finder _elevatedButtonWithText(String text) => find.ancestor(
  of: find.text(text),
  matching: find.byWidgetPredicate((widget) => widget is ElevatedButton),
);

class _MemoryAgendaRepository implements AgendaRepository {
  AgendaData? value;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value;

  @override
  Future<AgendaData> loadOrCreate() async => value ?? AgendaSeedData.salon();

  @override
  Future<void> save(AgendaData data) async => value = data;
}
