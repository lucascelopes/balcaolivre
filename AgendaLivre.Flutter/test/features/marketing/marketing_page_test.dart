import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/marketing/marketing_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('replica o estúdio de conteúdo WPF no desktop', (tester) async {
    final controller = _controller(
      AgendaData()..settings.businessName = 'Lucas Barbearia',
    );

    await _pumpMarketing(tester, controller, const Size(1366, 768));

    expect(find.text('Suas campanhas'), findsOneWidget);
    expect(find.text('Criar campanha'), findsWidgets);
    expect(find.text('Nenhuma campanha criada ainda'), findsOneWidget);
    await tester.tap(find.byKey(const Key('marketing-hub-whatsapp')));
    await tester.pump();

    expect(find.text('ESTÚDIO DE CONTEÚDO'), findsOneWidget);
    expect(find.text(' / MARKETING'), findsOneWidget);
    expect(find.text('Criar publicação'), findsOneWidget);
    expect(find.text('Título da campanha'), findsOneWidget);
    expect(find.text('Texto da publicação'), findsOneWidget);
    expect(find.text('Horários disponíveis'), findsOneWidget);
    expect(find.text('Editar arte'), findsOneWidget);
    expect(find.text('Publicação'), findsOneWidget);
    expect(find.text('Exportar PNG'), findsOneWidget);
    expect(find.text('Publicar no WhatsApp'), findsOneWidget);
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
    final controller = _controller(
      AgendaData()..settings.businessName = 'Lucas Barbearia',
    );
    await _pumpMarketing(tester, controller, const Size(1366, 768));
    await tester.tap(find.byKey(const Key('marketing-hub-whatsapp')));
    await tester.pump();

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
        'Olá, Cliente! Lucas Barbearia: Semana especial — 20% de desconto em serviços selecionados.',
      ),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const Key('marketing-update-promotion')));
    await tester.pump();
    expect(
      tester.widget<TextField>(messageField).controller!.text,
      'Olá, {nome}! {empresa}: Semana especial — 20% de desconto em serviços selecionados.',
    );

    await tester.enterText(nameField, 'Semana premium');
    await tester.tap(find.byKey(const Key('marketing-update-promotion')));
    await tester.pump();
    expect(
      tester.widget<TextField>(messageField).controller!.text,
      'Olá, {nome}! {empresa}: Semana premium — 20% de desconto em serviços selecionados.',
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
}

AgendaController _controller(AgendaData data) =>
    AgendaController(_MemoryAgendaRepository())
      ..data = data
      ..loading = false;

Future<void> _pumpMarketing(
  WidgetTester tester,
  AgendaController controller,
  Size size,
) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
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
