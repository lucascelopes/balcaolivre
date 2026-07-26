import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/agenda/agenda_page.dart' as agenda_ui;
import 'package:agenda_livre/features/establishment/establishment_page.dart';
import 'package:agenda_livre/features/finance/finance_page.dart';
import 'package:agenda_livre/features/home/home_page.dart';
import 'package:agenda_livre/features/marketing/marketing_page.dart';
import 'package:agenda_livre/features/reports/reports_page.dart';
import 'package:agenda_livre/features/settings/settings_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  for (final size in <Size>[
    const Size(1382, 736),
    const Size(390, 844),
    const Size(844, 390),
  ]) {
    testWidgets('páginas WPF permanecem responsivas em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      final controller = AgendaController(_MemoryAgendaRepository())
        ..data = AgendaData()
        ..loading = false;

      await _pumpPage(tester, size, HomePage(controller: controller));
      expect(find.byKey(const Key('home-hero')), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(
        tester,
        size,
        agenda_ui.AgendaPage(controller: controller),
      );
      expect(find.byKey(const Key('agenda-main-workspace')), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(tester, size, FinancePage(controller: controller));
      expect(find.text('Mercado Pago'), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(tester, size, ReportsPage(controller: controller));
      expect(find.text('Leituras rápidas'), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(tester, size, MarketingPage(controller: controller));
      expect(find.text('Fila de contatos'), findsOneWidget);
      expect(find.text('Mensagens prontas'), findsOneWidget);
      expect(find.text('Instagram'), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(tester, size, EstablishmentPage(controller: controller));
      expect(find.text('Meu estabelecimento'), findsOneWidget);
      expect(tester.takeException(), isNull);

      await _pumpPage(tester, size, SettingsPage(controller: controller));
      expect(find.text('Configurações'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }
}

Future<void> _pumpPage(WidgetTester tester, Size size, Widget page) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Scaffold(body: page),
    ),
  );
  await tester.pump();
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
