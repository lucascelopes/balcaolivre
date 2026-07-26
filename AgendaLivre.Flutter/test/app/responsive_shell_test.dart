import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('shell desktop replica dimensões e paleta padrão do WPF', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1382, 736);
    addTearDown(tester.view.reset);

    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
      ..loading = false;
    controller.data.settings.themeId = '';

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pumpAndSettle();

    final topBar = find.byKey(const Key('desktop-topbar'));
    final sidebar = find.byKey(const Key('desktop-sidebar'));
    expect(tester.getSize(topBar).height, 68);
    expect(tester.getSize(sidebar).width, 260);

    final sidebarDecoration =
        tester.widget<AnimatedContainer>(sidebar).decoration! as BoxDecoration;
    expect(sidebarDecoration.color, const Color(0xFF171614));
    final sidebarBorder = sidebarDecoration.border! as Border;
    expect(sidebarBorder.right.width, 3);
    expect(
      tester
          .widget<Material>(find.byKey(const Key('sidebar-profile-surface')))
          .color,
      const Color(0xFF24211F),
    );
    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget is Image &&
            widget.image is AssetImage &&
            (widget.image as AssetImage).assetName ==
                'assets/branding/agenda-livre-mark.png',
      ),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const Key('sidebar-toggle')));
    await tester.pumpAndSettle();
    expect(tester.getSize(sidebar).width, 72);

    await tester.tap(find.byKey(const Key('sidebar-profile-toggle')));
    await tester.pumpAndSettle();
    expect(tester.getSize(sidebar).width, 260);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mostra o trial Web no desktop e no mobile sem bloquear uso', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    final repository = _TrialAgendaRepository(active: true, daysRemaining: 6);
    final controller = AgendaController(repository, onLogout: () async {})
      ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
      ..loading = false;

    tester.view.physicalSize = const Size(1280, 720);
    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('agenda-trial-status-desktop')),
      findsOneWidget,
    );
    expect(find.text('Teste: 6 dias restantes'), findsOneWidget);
    expect(
      find.byKey(const Key('topbar-date-button')).hitTestable(),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);

    tester.view.physicalSize = const Size(390, 844);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('agenda-trial-status-mobile')), findsOneWidget);
    expect(find.textContaining('Teste: 6 dias restantes'), findsOneWidget);
    expect(find.byTooltip('Novo agendamento').hitTestable(), findsOneWidget);
    expect(
      find.byKey(const Key('mobile-date-filter')).hitTestable(),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('mobile-new-appointment')).hitTestable(),
      findsOneWidget,
    );
    expect(find.byType(NavigationBar), findsOneWidget);

    await tester.tap(find.byTooltip('Menu'));
    await tester.pumpAndSettle();
    for (final page in AgendaPage.values) {
      expect(
        find.byKey(Key('sidebar-destination-${page.name}')),
        findsOneWidget,
      );
    }
    expect(tester.takeException(), isNull);
  });

  testWidgets('identifica trial Web expirado sem bloquear a agenda', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1280, 720);
    addTearDown(tester.view.reset);
    final controller =
        AgendaController(
            _TrialAgendaRepository(active: false, daysRemaining: 0),
            onLogout: () async {},
          )
          ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
          ..loading = false;

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Teste de 7 dias expirado'), findsOneWidget);
    expect(
      find.byKey(const Key('topbar-date-button')).hitTestable(),
      findsOneWidget,
    );
    expect(find.byType(ResponsiveAgendaShell), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('temas personalizados usam sidebar clara e seleção AccentSoft', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1382, 736);
    addTearDown(tester.view.reset);

    final theme = AgendaThemes.byId('aesthetic-coral');
    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
      ..loading = false;

    await tester.pumpWidget(
      MaterialApp(
        theme: theme.toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pumpAndSettle();

    final sidebarDecoration =
        tester
                .widget<AnimatedContainer>(
                  find.byKey(const Key('desktop-sidebar')),
                )
                .decoration!
            as BoxDecoration;
    expect(sidebarDecoration.color, Colors.white);
    expect(
      tester
          .widget<Material>(find.byKey(const Key('sidebar-destination-home')))
          .color,
      theme.tokens.accentSoft,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('seletor desktop oferece sete dias e atalhos do WPF', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1382, 736);
    addTearDown(tester.view.reset);

    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
      ..loading = false
      ..selectedDate = DateUtils.dateOnly(DateTime.now());

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('topbar-date-button')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('date-popover')), findsOneWidget);
    expect(find.text('Ontem'), findsOneWidget);
    expect(find.text('Amanhã'), findsOneWidget);
    expect(find.text('Escolher outra data'), findsOneWidget);
    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget.key is ValueKey<String> &&
            (widget.key! as ValueKey<String>).value.startsWith('date-strip-'),
      ),
      findsNWidgets(7),
    );

    await tester.tap(find.text('Amanhã'));
    await tester.pumpAndSettle();
    expect(
      DateUtils.isSameDay(
        controller.selectedDate,
        DateUtils.dateOnly(DateTime.now()).add(const Duration(days: 1)),
      ),
      isTrue,
    );
    expect(tester.takeException(), isNull);
  });

  for (final size in <Size>[const Size(1382, 736), const Size(390, 844)]) {
    testWidgets(
      'renderiza todas as áreas em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
        tester.view.devicePixelRatio = 1;
        tester.view.physicalSize = size;
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
        await tester.pump();
        expect(tester.takeException(), isNull);

        for (final page in AgendaPage.values) {
          controller.navigate(page);
          await tester.pump();
          expect(
            tester.takeException(),
            isNull,
            reason: 'Falha ao renderizar ${page.name} em $size',
          );
        }
      },
    );
  }

  testWidgets('equaliza os cartões financeiros e usa a marca do WhatsApp', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1382, 736);
    addTearDown(tester.view.reset);

    final controller = AgendaController(_MemoryAgendaRepository())
      ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
      ..loading = false
      ..navigate(AgendaPage.finance);

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
        home: ResponsiveAgendaShell(controller: controller),
      ),
    );
    await tester.pump();

    final sources = find.byKey(const Key('finance-sources-card'));
    final pending = find.byKey(const Key('finance-pending-card'));
    final expenses = find.byKey(const Key('finance-expenses-card'));

    expect(sources, findsOneWidget);
    expect(pending, findsOneWidget);
    expect(expenses, findsOneWidget);
    expect(tester.getSize(sources).height, tester.getSize(pending).height);
    expect(tester.getSize(sources).height, tester.getSize(expenses).height);
    final whatsAppIcon = find.byWidgetPredicate(
      (widget) =>
          widget is FaIcon && widget.icon == FontAwesomeIcons.whatsapp.data,
    );
    expect(whatsAppIcon, findsOneWidget);
    final whatsAppFab = tester.widget<FloatingActionButton>(
      find.byKey(const Key('agenda-whatsapp-fab')),
    );
    expect(whatsAppFab.shape, isA<CircleBorder>());
    expect((whatsAppFab.shape! as CircleBorder).side, BorderSide.none);
    expect(whatsAppFab.elevation, 1);

    await tester.tap(whatsAppIcon);
    await tester.pumpAndSettle();
    expect(find.text('Conversas e confirmações'), findsOneWidget);
  });

  testWidgets('diálogo de negócio mantém componentes do desktop no mobile', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    await _openBusinessDialog(tester, const Size(1382, 736));

    final dialog = find.byKey(const Key('business-data-dialog'));
    final ownerCard = find.byKey(const Key('business-owner-card'));
    final establishmentCard = find.byKey(
      const Key('business-establishment-card'),
    );
    final locationCard = find.byKey(const Key('business-location-card'));
    final ownerField = find.byKey(const Key('business-owner-field'));

    expect(tester.getSize(dialog).width, closeTo(760, .1));
    expect(tester.getSize(dialog).height, closeTo(680, .1));
    expect(
      tester.getTopLeft(ownerCard).dy,
      closeTo(tester.getTopLeft(establishmentCard).dy, .1),
    );
    expect(
      tester.getSize(ownerCard).width,
      closeTo(tester.getSize(establishmentCard).width, .1),
    );
    expect(
      tester.getSize(locationCard).width,
      greaterThan(tester.getSize(ownerCard).width * 1.9),
    );
    final desktopFieldHeight = tester.getSize(ownerField).height;
    expect(desktopFieldHeight, closeTo(39, .1));
    expect(
      tester.getSize(find.byKey(const Key('business-cep-lookup'))).height,
      closeTo(39, .1),
    );
    expect(
      tester.getSize(find.byKey(const Key('business-dialog-save'))).height,
      closeTo(40, .1),
    );

    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pumpAndSettle();
    await _openBusinessDialog(tester, const Size(390, 844));

    final mobileDialog = find.byKey(const Key('business-data-dialog'));
    final mobileOwnerCard = find.byKey(const Key('business-owner-card'));
    final mobileEstablishmentCard = find.byKey(
      const Key('business-establishment-card'),
    );
    final mobileOwnerField = find.byKey(const Key('business-owner-field'));
    final footer = find.byKey(const Key('business-dialog-footer'));
    final saveButton = find.byKey(const Key('business-dialog-save'));
    final cancelButton = find.byKey(const Key('business-dialog-cancel'));
    expect(tester.takeException(), isNull);
    expect(tester.getSize(mobileDialog).width, closeTo(374, .1));
    expect(
      tester.getTopLeft(mobileEstablishmentCard).dy,
      greaterThan(tester.getBottomLeft(mobileOwnerCard).dy),
    );
    expect(
      tester.getSize(mobileOwnerField).height,
      closeTo(desktopFieldHeight, .1),
    );
    expect(saveButton, findsOneWidget);
    expect(cancelButton, findsOneWidget);
    final footerTopBeforeScroll = tester.getTopLeft(footer).dy;

    await tester.ensureVisible(find.byKey(const Key('business-number-field')));
    await tester.pumpAndSettle();

    expect(tester.getTopLeft(footer).dy, closeTo(footerTopBeforeScroll, .1));
    expect(
      find.byKey(const Key('business-number-field')).hitTestable(),
      findsOneWidget,
    );
    expect(saveButton.hitTestable(), findsOneWidget);
    expect(cancelButton.hitTestable(), findsOneWidget);

    await tester.tap(cancelButton);
    await tester.pumpAndSettle();
    expect(dialog, findsNothing);
  });

  testWidgets('diálogo de negócio salva alterações pelo rodapé fixo', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    final controller = await _openBusinessDialog(tester, const Size(390, 844));
    await tester.enterText(
      find.byKey(const Key('business-name-field')),
      'Novo Espaço',
    );
    await tester.tap(find.byKey(const Key('business-dialog-save')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('business-data-dialog')), findsNothing);
    expect(controller.data.settings.businessName, 'Novo Espaço');
  });

  testWidgets('diálogo de negócio cancela sem alterar os dados', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    final controller = await _openBusinessDialog(tester, const Size(390, 844));
    final originalName = controller.data.settings.businessName;
    await tester.enterText(
      find.byKey(const Key('business-name-field')),
      'Nome descartado',
    );
    await tester.tap(find.byKey(const Key('business-dialog-cancel')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('business-data-dialog')), findsNothing);
    expect(controller.data.settings.businessName, originalName);
  });

  testWidgets('erro de CEP mantém o botão alinhado ao campo no desktop', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    await _openBusinessDialog(tester, const Size(1382, 736));
    final cepField = find.byKey(const Key('business-cep-field'));
    final lookupButton = find.byKey(const Key('business-cep-lookup'));
    await tester.ensureVisible(cepField);
    await tester.pumpAndSettle();
    await tester.enterText(cepField, '123');
    await tester.tap(lookupButton);
    await tester.pumpAndSettle();

    expect(find.text('Não foi possível consultar o CEP.'), findsOneWidget);
    expect(
      tester.getTopLeft(lookupButton).dy,
      closeTo(tester.getTopLeft(cepField).dy, .1),
    );
    expect(tester.takeException(), isNull);
  });

  for (final size in <Size>[const Size(320, 568), const Size(844, 390)]) {
    testWidgets(
      'diálogo de negócio não transborda em ${size.width.toInt()}x${size.height.toInt()}',
      (tester) async {
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.reset);

        await _openBusinessDialog(tester, size);
        final footer = find.byKey(const Key('business-dialog-footer'));
        final footerTop = tester.getTopLeft(footer).dy;

        await tester.ensureVisible(
          find.byKey(const Key('business-complement-field')),
        );
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull);
        expect(tester.getTopLeft(footer).dy, closeTo(footerTop, .1));
        expect(
          find.byKey(const Key('business-complement-field')).hitTestable(),
          findsOneWidget,
        );
        expect(
          find.byKey(const Key('business-dialog-cancel')).hitTestable(),
          findsOneWidget,
        );
        expect(
          find.byKey(const Key('business-dialog-save')).hitTestable(),
          findsOneWidget,
        );
      },
    );
  }
}

Future<AgendaController> _openBusinessDialog(
  WidgetTester tester,
  Size size,
) async {
  tester.view.physicalSize = size;
  final controller = AgendaController(_MemoryAgendaRepository())
    ..data = AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14))
    ..loading = false
    ..navigate(AgendaPage.settings);

  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: ResponsiveAgendaShell(controller: controller),
    ),
  );
  await tester.pump();
  final openButton = find.byKey(const Key('business-edit-open'));
  expect(openButton, findsOneWidget);
  await tester.ensureVisible(openButton);
  await tester.pumpAndSettle();
  expect(openButton.hitTestable(), findsOneWidget);
  await tester.tap(openButton);
  await tester.pumpAndSettle();
  expect(find.byKey(const Key('business-data-dialog')), findsOneWidget);
  expect(tester.takeException(), isNull);
  return controller;
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
  Future<AgendaData> loadOrCreate() async => value ?? AgendaSeedData.salon();

  @override
  Future<void> save(AgendaData data) async => value = data;
}

class _TrialAgendaRepository extends _MemoryAgendaRepository
    implements AgendaSyncRepository {
  _TrialAgendaRepository({required this.active, required this.daysRemaining});

  final bool active;
  final int daysRemaining;

  @override
  bool get hasConflict => false;

  @override
  bool get hasTrialStatus => true;

  @override
  bool get isSyncing => false;

  @override
  String? get syncMessage => null;

  @override
  bool get trialActive => active;

  @override
  int get trialDaysRemaining => daysRemaining;

  @override
  Future<AgendaData?> refreshRemoteIfSafe() async => null;

  @override
  Future<AgendaData?> resolveConflictUsingCloud() async => null;

  @override
  Future<AgendaData?> resolveConflictUsingLocal() async => null;

  @override
  Future<void> retrySync() async {}
}
