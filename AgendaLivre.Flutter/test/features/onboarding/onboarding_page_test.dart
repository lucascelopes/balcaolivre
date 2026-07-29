import 'dart:convert';

import 'package:agenda_livre/app/agenda_app.dart';
import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/onboarding/onboarding_page.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

const _desktop = Size(1365, 768);
const _mobile = Size(390, 844);

const _card = Key('onboarding-card');
const _sidebar = Key('onboarding-sidebar');
const _content = Key('onboarding-content');
const _brandMark = Key('onboarding-brand-mark');
const _illustration = Key('onboarding-illustration');
const _sideProgress = Key('onboarding-side-progress');
const _mobileHeader = Key('onboarding-mobile-header');
const _progress = Key('onboarding-progress');

const _name = Key('onboarding-name-field');
const _phone = Key('onboarding-phone-field');
const _email = Key('onboarding-email-field');
const _business = Key('onboarding-business-field');
const _logo = Key('onboarding-logo-button');
const _logoPreview = Key('onboarding-logo-preview');

const _primary = Key('onboarding-primary');
const _back = Key('onboarding-back');
const _skip = Key('onboarding-skip');
const _themeSkip = Key('onboarding-theme-skip');

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('reproduz a composição editorial selecionada no desktop', (
    tester,
  ) async {
    await _pumpOnboarding(tester, _desktop);

    expect(find.text('Cadastre seu negócio'), findsOneWidget);
    expect(
      find.text('Comece configurando os dados do seu negócio.'),
      findsOneWidget,
    );
    expect(find.text('Tudo pronto para começar.'), findsOneWidget);
    expect(find.byKey(_brandMark), findsOneWidget);
    expect(find.byKey(_illustration), findsOneWidget);
    expect(find.byKey(_sideProgress), findsOneWidget);

    expect(tester.getSize(find.byKey(_card)), _desktop);
    expect(tester.getSize(find.byKey(_sidebar)).width, closeTo(559.65, .1));
    expect(tester.getSize(find.byKey(_content)).width, closeTo(804.35, .1));
    expect(tester.getTopLeft(find.byKey(_card)), Offset.zero);

    for (final key in [_name, _phone, _email, _business]) {
      final size = tester.getSize(find.byKey(key));
      expect(size.width, closeTo(280, .1));
      expect(size.height, 64);
    }
    expect(tester.getSize(find.byKey(_logo)), const Size(180, 48));
    final primarySize = tester.getSize(find.byKey(_primary));
    expect(primarySize.width, 610);
    expect(primarySize.height, 48);

    final context = tester.element(find.byKey(_card));
    final tokens = Theme.of(context).extension<AgendaThemeTokens>()!;
    expect(tokens.appBackground, const Color(0xFFFAF9F7));
    expect(tokens.panel, const Color(0xFFFFFFFF));
    expect(tokens.accent, const Color(0xFFED6823));
    expect(tokens.accentDark, const Color(0xFFC95016));
    expect(tokens.accentSoft, const Color(0xFFFFF1E9));
    expect(tokens.line, const Color(0xFFE8E3DE));
    expect(tester.takeException(), isNull);
  });

  for (final size in <Size>[
    _mobile,
    const Size(320, 568),
    const Size(844, 390),
  ]) {
    testWidgets('mantém a primeira página utilizável em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      await _pumpOnboarding(tester, size);

      expect(find.byKey(_mobileHeader), findsOneWidget);
      for (final key in [_name, _phone, _email, _business]) {
        final rect = tester.getRect(find.byKey(key));
        expect(rect.left, greaterThanOrEqualTo(16));
        expect(rect.right, lessThanOrEqualTo(size.width - 16));
      }

      await tester.ensureVisible(find.byKey(_primary));
      await tester.pumpAndSettle();
      expect(find.byKey(_primary).hitTestable(), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('mantém o tema como subetapa 2 e percorre as seis etapas', (
    tester,
  ) async {
    final harness = await _pumpOnboarding(tester, _desktop);
    await _fillInitialData(tester);
    await _tap(tester, _primary);

    _expectProgress(tester, '2/6');
    expect(find.text('Escolha o seu segmento'), findsOneWidget);
    expect(
      find.byKey(const Key('onboarding-segment-centro-estetica')),
      findsOneWidget,
    );

    await _tap(tester, const Key('onboarding-segment-centro-estetica'));
    _expectProgress(tester, '2/6');
    expect(find.text('Agora, escolha o seu estilo'), findsOneWidget);
    expect(find.byKey(_skip), findsNothing);
    expect(find.byKey(_themeSkip), findsNothing);
    final themeCardSize = tester.getSize(
      find.byKey(const Key('onboarding-theme-default')),
    );
    expect(themeCardSize.width, closeTo(299, .1));
    expect(themeCardSize.height, closeTo(132, .1));

    await _tap(tester, _back);
    _expectProgress(tester, '2/6');
    expect(find.text('Escolha o seu segmento'), findsOneWidget);

    await _tap(tester, const Key('onboarding-segment-centro-estetica'));
    await _tap(tester, const Key('onboarding-theme-aesthetic-coral'));
    await _tap(tester, _primary);

    _expectProgress(tester, '3/6');
    expect(find.text('Quantas pessoas atendem?'), findsOneWidget);
    expect(
      tester.getSize(find.byKey(const Key('onboarding-team-two'))).height,
      closeTo(100, .1),
    );
    await _tap(tester, const Key('onboarding-team-two'));
    await _tap(tester, _primary);

    _expectProgress(tester, '4/6');
    expect(find.text('O que você quer conquistar?'), findsOneWidget);
    await _tap(tester, const Key('onboarding-objective-agenda'));
    await _tap(tester, _primary);

    _expectProgress(tester, '5/6');
    expect(find.text('Onde fica o seu negócio?'), findsNWidgets(2));
    for (final key in const [
      Key('onboarding-cep-field'),
      Key('onboarding-neighborhood-field'),
      Key('onboarding-street-field'),
      Key('onboarding-number-field'),
      Key('onboarding-complement-field'),
    ]) {
      expect(tester.getSize(find.byKey(key)).height, closeTo(79, .1));
    }
    await _enterIn(
      tester,
      const Key('onboarding-street-field'),
      'Rua Piracicaba',
    );
    await _enterIn(tester, const Key('onboarding-number-field'), '10');
    await _enterIn(
      tester,
      const Key('onboarding-neighborhood-field'),
      'Lourdes',
    );
    await _enterIn(tester, const Key('onboarding-complement-field'), 'Sala 2');
    await _tap(tester, _primary);

    _expectProgress(tester, '6/6');
    expect(find.text('Revise e conclua'), findsOneWidget);
    expect(find.byKey(const Key('onboarding-review-panel')), findsOneWidget);
    expect(find.text('Lucas Barbearia'), findsOneWidget);
    expect(find.text('Centro de Estética'), findsOneWidget);
    expect(find.textContaining('2 pessoas'), findsOneWidget);
    expect(find.textContaining('Organizar minha agenda'), findsOneWidget);
    expect(find.textContaining('Rua Piracicaba'), findsOneWidget);

    await _tap(tester, _primary);
    expect(find.byType(OnboardingPage), findsNothing);
    expect(find.byType(ResponsiveAgendaShell), findsOneWidget);

    final settings = harness.controller.data.settings;
    expect(settings.accountFullName, 'Lucas Cesar Lopes');
    expect(settings.accountPhone, '(33) 99800-7983');
    expect(settings.accountEmail, 'lucas@example.com');
    expect(settings.businessName, 'Lucas Barbearia');
    expect(settings.businessSegment, 'Centro de Estética');
    expect(settings.themeId, 'aesthetic-coral');
    expect(settings.professionalCountRange, '2 profissionais');
    expect(settings.mainObjective, 'Organizar agenda');
    expect(settings.onboardingCompleted, isTrue);
    expect(settings.clientLabel, 'Cliente');
    expect(settings.clientDetailLabel, 'Preferência / alergia / estilo');
    expect(settings.resourceLabel, 'Mesa ou cadeira');
    expect(settings.workdayStartHour, 9);
    expect(settings.workdayEndHour, 20);
    expect(settings.resources, ['Mesa 1', 'Mesa 2', 'Cadeira beleza']);
    expect(harness.controller.data.services, hasLength(4));
    expect(harness.controller.data.services.first.name, 'Manicure');
    expect(harness.controller.data.professionals, hasLength(2));
    expect(harness.controller.data.professionals.first.name, 'Manicure 1');
    expect(harness.repository.saveCalls, 1);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Pular reproduz os padrões definidos no desktop', (tester) async {
    final harness = await _pumpOnboarding(tester, _desktop);

    await _tap(tester, _skip);
    expect(find.text('Escolha o seu segmento'), findsOneWidget);
    await _tap(tester, _skip);
    _expectProgress(tester, '3/6');
    await _tap(tester, _skip);
    _expectProgress(tester, '4/6');
    await _tap(tester, _skip);
    _expectProgress(tester, '5/6');
    await _tap(tester, _skip);
    _expectProgress(tester, '6/6');
    expect(find.byKey(_skip), findsNothing);

    await _tap(tester, _primary);
    final settings = harness.controller.data.settings;
    expect(settings.accountFullName, isEmpty);
    expect(settings.accountPhone, isEmpty);
    expect(settings.accountEmail, isEmpty);
    expect(settings.businessName, 'Meu salão de beleza');
    expect(settings.businessSegment, 'Salão de Beleza');
    expect(settings.themeId, isEmpty);
    expect(settings.professionalCountRange, '1 profissional');
    expect(settings.mainObjective, 'Organizar agenda');
    expect(settings.businessAddress, isEmpty);
    expect(settings.onboardingCompleted, isTrue);
    expect(harness.controller.data.services, hasLength(8));
    expect(harness.controller.data.professionals, hasLength(1));
    expect(tester.takeException(), isNull);
  });

  testWidgets('números das etapas navegam com os mesmos padrões do WPF', (
    tester,
  ) async {
    await _pumpOnboarding(tester, _desktop);
    await _fillInitialData(tester);

    await _tap(tester, const Key('onboarding-step-4'));
    _expectProgress(tester, '4/6');
    expect(find.text('O que você quer conquistar?'), findsOneWidget);

    await _tap(tester, const Key('onboarding-step-1'));
    _expectProgress(tester, '1/6');
    expect(find.text('Cadastre seu negócio'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('escolhe, normaliza e mantém uma logo real no fluxo', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = _desktop;
    addTearDown(tester.view.reset);

    final repository = _MemoryAgendaRepository(
      AgendaData(
        settings: AgendaSettings(
          businessName: 'Balcão Livre',
          onboardingCompleted: false,
        ),
      ),
    );
    final controller = AgendaController(repository);
    await controller.initialize();
    final png = base64Decode(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk'
      '+A8AAQUBAScY42YAAAAASUVORK5CYII=',
    );

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: OnboardingPage(
          controller: controller,
          pickBusinessLogo: () async => PlatformFile(
            name: 'marca-real.png',
            size: png.length,
            bytes: png,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await _tap(tester, _logo);
    expect(
      find.descendant(
        of: find.byKey(_logoPreview),
        matching: find.byType(Image),
      ),
      findsOneWidget,
    );
    expect(find.text('Alterar logo'), findsOneWidget);

    await _fillInitialData(tester);
    await _tap(tester, _primary);
    expect(
      controller.data.settings.businessLogoPath,
      startsWith('data:image/png;base64,'),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('salva a logo real também no fluxo mobile', (tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = _mobile;
    addTearDown(tester.view.reset);

    final repository = _MemoryAgendaRepository(
      AgendaData(
        settings: AgendaSettings(
          businessName: 'Balcão Livre',
          onboardingCompleted: false,
        ),
      ),
    );
    final controller = AgendaController(repository);
    await controller.initialize();
    final png = base64Decode(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk'
      '+A8AAQUBAScY42YAAAAASUVORK5CYII=',
    );

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: OnboardingPage(
          controller: controller,
          pickBusinessLogo: () async => PlatformFile(
            name: 'marca-real-mobile.png',
            size: png.length,
            bytes: png,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await _tap(tester, _logo);
    expect(
      find.descendant(
        of: find.byKey(_logoPreview),
        matching: find.byType(Image),
      ),
      findsOneWidget,
    );

    await _fillInitialData(tester);
    await _tap(tester, _primary);
    expect(
      controller.data.settings.businessLogoPath,
      startsWith('data:image/png;base64,'),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('não avança com celular ou e-mail inválidos', (tester) async {
    await _pumpOnboarding(tester, _desktop);
    await _enterIn(tester, _name, 'Lucas Cesar Lopes');
    await _enterIn(tester, _phone, '123');
    await _enterIn(tester, _email, 'email-invalido');
    await _enterIn(tester, _business, 'Lucas Barbearia');
    await _tap(tester, _primary);

    expect(find.text('Cadastre seu negócio'), findsOneWidget);
    expect(find.text('Escolha o seu segmento'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('exibe o e-mail autenticado sem permitir edição', (tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = _desktop;
    addTearDown(tester.view.reset);
    final repository = _MemoryAgendaRepository(
      AgendaData(
        settings: AgendaSettings(
          accountEmail: 'fixture@example.com',
          businessName: 'Balcão Livre',
          businessSegment: '',
          onboardingCompleted: false,
        ),
      ),
    );
    final controller = AgendaController(
      repository,
      onLogout: () async {},
      authenticatedEmail: 'conta.real@example.com',
    );

    await tester.pumpWidget(AgendaLivreApp(controller: controller));
    await tester.pumpAndSettle();

    final input = find.descendant(
      of: find.byKey(_email),
      matching: find.byType(TextField),
    );
    expect(input, findsOneWidget);
    expect(tester.widget<TextField>(input).readOnly, isTrue);
    expect(
      tester.widget<TextField>(input).controller?.text,
      'conta.real@example.com',
    );
    expect(controller.data.settings.accountEmail, 'conta.real@example.com');
    expect(tester.takeException(), isNull);
  });
}

Future<_Harness> _pumpOnboarding(WidgetTester tester, Size size) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final repository = _MemoryAgendaRepository(
    AgendaData(
      settings: AgendaSettings(
        businessName: 'Balcão Livre',
        businessSegment: '',
        themeId: '',
        onboardingCompleted: false,
      ),
    ),
  );
  final controller = AgendaController(repository);
  await tester.pumpWidget(AgendaLivreApp(controller: controller));
  await tester.pumpAndSettle();

  expect(find.byType(OnboardingPage), findsOneWidget);
  expect(controller.needsOnboarding, isTrue);
  expect(tester.takeException(), isNull);
  return _Harness(controller, repository);
}

Future<void> _fillInitialData(WidgetTester tester) async {
  await _enterIn(tester, _name, 'Lucas Cesar Lopes');
  await _enterIn(tester, _phone, '(33) 99800-7983');
  await _enterIn(tester, _email, 'lucas@example.com');
  await _enterIn(tester, _business, 'Lucas Barbearia');
}

Future<void> _enterIn(WidgetTester tester, Key field, String value) async {
  final input = find.descendant(
    of: find.byKey(field),
    matching: find.byType(TextField),
  );
  expect(input, findsOneWidget);
  await tester.enterText(input, value);
  await tester.pump();
}

Future<void> _tap(WidgetTester tester, Key key) async {
  final finder = find.byKey(key);
  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
  expect(finder.hitTestable(), findsOneWidget);
  await tester.tap(finder);
  await tester.pumpAndSettle();
  expect(tester.takeException(), isNull);
}

void _expectProgress(WidgetTester tester, String value) {
  final finder = find.byKey(_progress);
  expect(finder, findsOneWidget);
  expect(tester.widget<Text>(finder).data, value);
}

class _Harness {
  const _Harness(this.controller, this.repository);

  final AgendaController controller;
  final _MemoryAgendaRepository repository;
}

class _MemoryAgendaRepository implements AgendaRepository {
  _MemoryAgendaRepository(AgendaData initial) : value = _clone(initial);

  AgendaData? value;
  int saveCalls = 0;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value == null ? null : _clone(value!);

  @override
  Future<AgendaData> loadOrCreate() async =>
      value == null ? AgendaData() : _clone(value!);

  @override
  Future<void> save(AgendaData data) async {
    saveCalls++;
    value = _clone(data);
  }
}

AgendaData _clone(AgendaData data) => AgendaData.fromJson(data.toJson());
