import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/marketing/marketing_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('replica o estúdio de conteúdo WPF no desktop', (tester) async {
    final data = AgendaData()
      ..settings.businessName = 'Lucas Barbearia'
      ..settings.businessSegment = 'Barbearia';
    final controller = _controller(data);

    await _pumpMarketing(tester, controller, const Size(1366, 768));

    expect(find.text('Suas campanhas'), findsOneWidget);
    expect(find.text('Criar campanha'), findsWidgets);
    expect(find.text('Nenhuma campanha criada ainda'), findsOneWidget);
    await tester.tap(find.byKey(const Key('marketing-hub-whatsapp')));
    await tester.pumpAndSettle();

    expect(find.text('ESTÚDIO DE CONTEÚDO'), findsOneWidget);
    expect(find.text(' / MARKETING'), findsOneWidget);
    expect(find.text('Criar publicação'), findsOneWidget);
    expect(find.text('Título da campanha'), findsOneWidget);
    expect(find.text('Texto da publicação'), findsOneWidget);
    expect(find.text('Janelas sugeridas pela agenda'), findsOneWidget);
    expect(find.text('Ajustar'), findsOneWidget);
    expect(find.text('Publicação'), findsOneWidget);
    expect(find.text('Exportar PNG'), findsOneWidget);
    expect(find.text('Publicar no WhatsApp'), findsOneWidget);
    expect(find.textContaining('Editar arte'), findsOneWidget);
    expect(find.text('Coleção editorial'), findsOneWidget);

    final editor = tester.getRect(
      find.byKey(const Key('marketing-promotion-name')),
    );
    final preview = tester.getRect(
      find.byKey(const Key('marketing-message-preview')),
    );
    final campaign = tester.getRect(
      find.byKey(const Key('marketing-active-campaign')),
    );
    final contacts = tester.getRect(
      find.byKey(const Key('marketing-contacts-panel')),
    );
    expect(preview.left, greaterThan(editor.right));
    expect(
      tester.getRect(find.byKey(const Key('marketing-open-whatsapp'))).left,
      greaterThan(preview.right),
    );
    expect(campaign.bottom, lessThan(contacts.top));
    expect(tester.takeException(), isNull);
  });

  testWidgets('expande tokens e atualiza a promoção como no WPF', (
    tester,
  ) async {
    final data = AgendaData()
      ..settings.businessName = 'Lucas Barbearia'
      ..settings.businessSegment = 'Barbearia';
    final controller = _controller(data);
    await _pumpMarketing(tester, controller, const Size(1366, 768));
    await tester.tap(find.byKey(const Key('marketing-hub-whatsapp')));
    await tester.pumpAndSettle();

    final nameField = find.byKey(const Key('marketing-promotion-name'));
    final messageField = find.byKey(const Key('marketing-promotion-message'));

    await tester.tap(nameField);
    await tester.pump();
    final focusedController = tester.widget<TextField>(nameField).controller!;
    expect(focusedController.selection.baseOffset, 0);
    expect(
      focusedController.selection.extentOffset,
      focusedController.text.length,
    );

    await tester.enterText(nameField, 'Semana especial');
    await tester.enterText(
      messageField,
      'Olá, {nome}! {empresa}: {promocao} — {oferta}.',
    );
    await tester.pump();
    expect(
      find.text(
        'Olá, Cliente! Lucas Barbearia: Semana especial — Corte e barba com condição especial nesta semana.',
      ),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const Key('marketing-update-promotion')));
    await tester.pump();
    expect(
      tester.widget<TextField>(messageField).controller!.text,
      'Olá, {nome}! {empresa}: Semana especial — Corte e barba com condição especial nesta semana.',
    );

    await tester.enterText(nameField, 'Semana premium');
    await tester.tap(find.byKey(const Key('marketing-update-promotion')));
    await tester.pump();
    expect(
      tester.widget<TextField>(messageField).controller!.text,
      'Olá, {nome}! {empresa}: Semana premium — Corte e barba com condição especial nesta semana.',
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('ordena a fila e usa o template grande sem telefone', (
    tester,
  ) async {
    final now = DateTime.now();
    final data = AgendaData(
      customers: [
        Customer(
          name: 'Maria Souza',
          phone: '(11) 95555-0000',
          profile: 'Corte',
          lastSeenAt: now.subtract(const Duration(days: 45)),
        ),
        Customer(
          name: 'Cliente sem telefone',
          phone: '',
          profile: 'Barba',
          lastSeenAt: now.subtract(const Duration(days: 40)),
        ),
      ],
      appointments: [
        Appointment(
          customerName: 'Nina / Tutor João',
          customerPhone: '(11) 97777-2002',
          serviceName: 'Banho e tosa',
          start: now.add(const Duration(days: 1)),
          status: AppointmentStatus.scheduled,
        ),
        Appointment(
          customerName: 'Patrícia Lima',
          customerPhone: '(11) 95555-4004',
          serviceName: 'Manicure',
          start: now.subtract(const Duration(days: 1)),
          status: AppointmentStatus.noShow,
        ),
      ],
    )..settings.businessName = 'Balcão Livre';

    await _pumpMarketing(tester, _controller(data), const Size(1366, 768));
    await tester.tap(find.byKey(const Key('marketing-hub-new-customers')));
    await tester.pump();

    expect(find.text('Nina / Tutor João'), findsOneWidget);
    expect(find.text('Patrícia Lima'), findsOneWidget);
    expect(find.text('Confirmação'), findsOneWidget);
    expect(find.text('Retorno'), findsOneWidget);
    final ninaTop = tester.getTopLeft(find.text('Nina / Tutor João')).dy;
    final patriciaTop = tester.getTopLeft(find.text('Patrícia Lima')).dy;
    expect(ninaTop, lessThan(patriciaTop));

    await tester.drag(find.byType(Scrollable).first, const Offset(0, -650));
    await tester.pumpAndSettle();
    final contactScroll = find.descendant(
      of: find.byKey(const Key('marketing-contacts-panel')),
      matching: find.byType(Scrollable),
    );
    await tester.drag(contactScroll, const Offset(0, -500));
    await tester.pumpAndSettle();
    expect(find.text('Maria Souza'), findsOneWidget);
    expect(find.text('Cliente sem telefone'), findsOneWidget);
    expect(find.text('Sem retorno'), findsNWidgets(2));
    final mariaTop = tester.getTopLeft(find.text('Maria Souza')).dy;
    final noPhoneTop = tester.getTopLeft(find.text('Cliente sem telefone')).dy;
    expect(mariaTop, lessThan(noPhoneTop));
    final emptyContact = find.byKey(
      const Key('marketing-contact-empty-Cliente sem telefone'),
    );
    expect(emptyContact, findsOneWidget);
    expect(
      find.descendant(of: emptyContact, matching: find.text('Abrir')),
      findsNothing,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('mantem o estado vazio responsivo no celular', (tester) async {
    await _pumpMarketing(
      tester,
      _controller(AgendaData()),
      const Size(390, 844),
    );

    expect(find.text('Suas campanhas'), findsOneWidget);
    expect(find.text('Criar campanha'), findsWidgets);
    expect(tester.takeException(), isNull);
  });

  testWidgets('estúdio mobile usa conteúdo da oficina sem prometer PNG', (
    tester,
  ) async {
    final data = AgendaData()
      ..settings.businessName = 'Oficina Central'
      ..settings.businessSegment = 'Oficina mecânica';
    await _pumpMarketing(tester, _controller(data), const Size(390, 844));

    await tester.tap(find.byKey(const Key('marketing-hub-whatsapp')));
    await tester.pumpAndSettle();

    expect(find.text('Copiar mensagem'), findsOneWidget);
    expect(find.text('Exportar PNG'), findsNothing);
    expect(find.text('Modelos para Oficina'), findsOneWidget);
    expect(find.text('Revisão'), findsWidgets);
    expect(find.text('Cabelo'), findsNothing);
    expect(find.text('Unhas'), findsNothing);
    expect(find.text('Coleção editorial'), findsNothing);
    final assetNames = tester
        .widgetList<Image>(find.byType(Image))
        .map((image) => image.image)
        .whereType<AssetImage>()
        .map((image) => image.assetName);
    expect(
      assetNames.any(
        (name) =>
            name.contains('marketing-campaign-hair') ||
            name.contains('marketing-campaign-nails') ||
            name.contains('marketing-campaign-spa') ||
            name.contains('marketing-site-hero-hair'),
      ),
      isFalse,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('porta a promoção do site do WPF e publica no celular', (
    tester,
  ) async {
    final data = AgendaData(
      services: [
        ServiceItem(
          name: 'Revisão preventiva',
          category: 'Mecânica',
          durationMinutes: 60,
          price: 180,
        ),
      ],
    )..settings.businessSegment = 'Oficina mecânica';
    final controller = _controller(data);
    await _pumpMarketing(tester, controller, const Size(390, 844));

    await tester.drag(
      find.byKey(const Key('marketing-hub-scroll')),
      const Offset(0, -620),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('marketing-hub-discount')));
    await tester.pumpAndSettle();

    expect(find.text('Criar promoção no site'), findsOneWidget);
    expect(find.text('Seleção de serviços'), findsOneWidget);
    expect(find.text('Resumo da promoção'), findsOneWidget);
    expect(
      find.byKey(const Key('marketing-promotion-service-0')),
      findsOneWidget,
    );

    await tester.drag(
      find.byKey(const Key('marketing-promotion-scroll')),
      const Offset(0, -900),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('marketing-promotion-publish')));
    await tester.pumpAndSettle();
    expect(find.textContaining('publicada no catálogo online'), findsOneWidget);
    final promotion =
        controller.data.settings.publishedMarketingCatalog?.promotion;
    expect(promotion?.isPublished, isTrue);
    expect(promotion?.items.single.serviceName, 'Revisão preventiva');
    expect(tester.takeException(), isNull);
  });

  testWidgets('catálogo mobile usa segmento e cor do tema', (tester) async {
    final data = AgendaData()
      ..settings.businessName = 'Oficina Central'
      ..settings.businessSegment = 'Oficina mecânica';
    final controller = _controller(data);
    await _pumpMarketing(
      tester,
      controller,
      const Size(390, 844),
      themeId: 'aesthetic-sage',
    );

    await tester.ensureVisible(
      find.byKey(const Key('marketing-hub-edit-catalog')),
    );
    await tester.tap(find.byKey(const Key('marketing-hub-edit-catalog')));
    await tester.pumpAndSettle();

    final title = tester.widget<TextField>(
      find.byKey(const Key('marketing-catalog-title')),
    );
    expect(title.controller?.text, 'Seu veículo em boas mãos');
    final tokens = Theme.of(
      tester.element(find.byKey(const Key('marketing-catalog-editor')).first),
    ).extension<AgendaThemeTokens>()!;
    final expectedAccent =
        '#${(tokens.accent.toARGB32() & 0x00FFFFFF).toRadixString(16).padLeft(6, '0').toUpperCase()}';

    await tester.ensureVisible(
      find.byKey(const Key('marketing-catalog-publish')),
    );
    await tester.tap(find.byKey(const Key('marketing-catalog-publish')));
    await tester.pumpAndSettle();

    final catalog = controller.data.settings.publishedMarketingCatalog;
    expect(catalog?.accentColor, expectedAccent);
    expect(
      catalog?.heroImagePath,
      'assets/branding/onboarding-team-workshop.png',
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('porta o editor de catálogo WPF no desktop e no celular', (
    tester,
  ) async {
    final data = AgendaData()..settings.businessName = 'Studio Nina Beauty';
    final controller = _controller(data);
    await _pumpMarketing(tester, controller, const Size(1366, 768));

    await tester.tap(find.byKey(const Key('marketing-hub-edit-catalog')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('marketing-catalog-editor')), findsWidgets);
    expect(find.text('Editar catálogo'), findsOneWidget);
    expect(find.text('Desktop'), findsOneWidget);
    expect(find.text('Tablet'), findsOneWidget);
    expect(find.text('Celular'), findsOneWidget);
    expect(find.text('Editar seção'), findsOneWidget);
    expect(find.text('Capa principal'), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.enterText(
      find.byKey(const Key('marketing-catalog-title')),
      'Beleza que combina com você',
    );
    await tester.tap(find.byKey(const Key('marketing-catalog-device-2')));
    await tester.pump();
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(const Key('marketing-catalog-publish')));
    await tester.pumpAndSettle();
    expect(
      controller.data.settings.publishedMarketingCatalog?.title,
      'Beleza que combina com você',
    );
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(const Key('marketing-catalog-back')));
    await tester.pumpAndSettle();

    tester.view.physicalSize = const Size(390, 844);
    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
        home: Scaffold(body: MarketingPage(controller: controller)),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    await tester.tap(find.byKey(const Key('marketing-hub-catalog-row')));
    await tester.pumpAndSettle();
    expect(find.text('Editar catálogo'), findsOneWidget);
    expect(find.byKey(const Key('marketing-catalog-hero')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

AgendaController _controller(AgendaData data) =>
    AgendaController(_MemoryAgendaRepository())
      ..data = data
      ..loading = false;

Future<void> _pumpMarketing(
  WidgetTester tester,
  AgendaController controller,
  Size size, {
  String themeId = 'aesthetic-coral',
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId(themeId).toThemeData(),
      home: Scaffold(body: MarketingPage(controller: controller)),
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
