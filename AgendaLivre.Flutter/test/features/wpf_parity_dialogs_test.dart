import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/models/professional.dart';
import 'package:agenda_livre/domain/models/service_item.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/establishment/editor_dialogs.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('Gerenciar serviços reproduz o master-detail WPF no desktop', (
    tester,
  ) async {
    final controller = _controller(
      services: [
        ServiceItem(
          id: 'service-haircut',
          name: 'Corte feminino',
          category: 'Cabelo',
          segment: 'Salão de Beleza',
          durationMinutes: 45,
          price: 85,
          defaultResource: 'Cadeira 1',
        ),
        ServiceItem(
          id: 'service-beard',
          name: 'Barba clássica',
          category: 'Barbearia',
          segment: 'Salão de Beleza',
          durationMinutes: 20,
          price: 40,
          defaultResource: 'Cadeira 2',
          isActive: false,
        ),
      ],
    );

    await _openDialog(
      tester,
      size: const Size(1366, 768),
      open: (context) =>
          showServiceManagerDialog(context, controller: controller),
    );

    final frame = find.byKey(const ValueKey('service-manager-dialog-frame'));
    final desktopFrameSize = tester.getSize(frame);
    expect(desktopFrameSize.width, closeTo(948, 2));
    expect(desktopFrameSize.height, closeTo(581, 2));

    expect(find.text('Gerenciar serviços'), findsOneWidget);
    expect(
      find.text('Catálogo com duração, preço e recurso padrão de atendimento.'),
      findsOneWidget,
    );
    expect(_textFieldWithHint('Buscar serviços...'), findsOneWidget);
    expect(find.byTooltip('Exibir somente serviços ativos'), findsOneWidget);
    expect(find.text('2 serviços'), findsOneWidget);

    expect(find.text('Corte feminino'), findsOneWidget);
    await tester.tap(find.text('Corte feminino'));
    await tester.pump();
    expect(find.text('Corte feminino'), findsWidgets);
    expect(find.text('Duração'), findsOneWidget);
    expect(find.text('Preço'), findsOneWidget);
    expect(find.text('Recurso padrão'), findsOneWidget);
    expect(find.text('Categoria'), findsOneWidget);
    expect(
      find.text('Serviço ativo e disponível para agendamento.'),
      findsOneWidget,
    );
    expect(find.text('45 min'), findsOneWidget);
    expect(find.text('Cadeira 1'), findsOneWidget);

    await tester.tap(find.text('Barba clássica'));
    await tester.pump();
    expect(
      find.text('Serviço inativo e indisponível para agendamento.'),
      findsOneWidget,
    );
    expect(find.text('20 min'), findsOneWidget);
    expect(find.text('Cadeira 2'), findsOneWidget);

    await tester.tap(find.text('Corte feminino').first);
    await tester.pump();
    await tester.tap(find.byTooltip('Exibir somente serviços ativos'));
    await tester.pump();
    expect(find.text('1 serviço'), findsOneWidget);
    expect(find.text('Barba clássica'), findsNothing);

    await tester.tap(find.byTooltip('Exibir somente serviços ativos'));
    await tester.pump();
    await tester.enterText(_textFieldWithHint('Buscar serviços...'), 'barba');
    await tester.pump();
    expect(find.text('1 serviço'), findsOneWidget);
    expect(find.text('Barba clássica'), findsWidgets);

    expect(find.widgetWithText(OutlinedButton, 'Cancelar'), findsOneWidget);
    expect(_elevatedButtonWithText('Novo serviço'), findsOneWidget);

    tester.view.physicalSize = const Size(1600, 900);
    await tester.pumpAndSettle();
    expect(tester.getSize(frame), desktopFrameSize);
    expect(tester.takeException(), isNull);
  });

  testWidgets('Criar profissional usa conta individual e rodapé WPF', (
    tester,
  ) async {
    final controller = _controller();
    await _openDialog(
      tester,
      size: const Size(1366, 768),
      open: (context) =>
          showProfessionalEditorDialog(context, controller: controller),
    );

    final frame = find.byKey(
      const ValueKey('professional-editor-dialog-frame'),
    );
    expect(tester.getSize(frame).width, closeTo(1100, 2));
    expect(tester.getSize(frame).height, closeTo(710, 2));

    expect(find.text('Criar profissional'), findsOneWidget);
    expect(
      find.text(
        'Cadastre os dados e crie o acesso individual ao Agenda Livre.',
      ),
      findsOneWidget,
    );
    expect(find.text('Dados do profissional'), findsOneWidget);
    expect(find.text('Nome do profissional'), findsOneWidget);
    expect(find.text('Telefone / WhatsApp'), findsOneWidget);
    expect(find.text('Segmento atendido'), findsOneWidget);
    expect(find.text('Observações internas'), findsOneWidget);

    expect(find.text('Conta para entrar no app'), findsOneWidget);
    expect(find.text('E-mail de acesso'), findsOneWidget);
    expect(find.text('Senha inicial'), findsOneWidget);
    expect(find.text('Confirmar senha'), findsOneWidget);
    expect(find.text('Permissões no app'), findsOneWidget);
    expect(
      tester.getTopLeft(find.text('Conta para entrar no app')).dx,
      greaterThan(
        tester.getTopLeft(find.text('Dados do profissional')).dx + 350,
      ),
    );

    _expectFixedFooter(
      tester,
      frame: frame,
      primaryLabel: 'Salvar e criar profissional',
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('Criar serviço usa seções, prévia e rodapé WPF', (tester) async {
    final controller = _controller();
    await _openDialog(
      tester,
      size: const Size(1366, 768),
      open: (context) =>
          showServiceEditorDialog(context, controller: controller),
    );

    final frame = find.byKey(const ValueKey('service-editor-dialog-frame'));
    expect(tester.getSize(frame).width, closeTo(840, 2));
    expect(tester.getSize(frame).height, closeTo(630, 2));

    expect(find.text('Criar serviço'), findsOneWidget);
    expect(
      find.text('Defina como o serviço aparece na agenda e no atendimento.'),
      findsOneWidget,
    );
    expect(find.text('Catálogo'), findsOneWidget);
    expect(find.text('Tempo e agenda'), findsOneWidget);
    expect(find.text('Preço e equipe'), findsOneWidget);
    expect(find.text('Nome do serviço'), findsOneWidget);
    expect(find.text('Duração em minutos'), findsOneWidget);
    expect(find.text('Sala, cadeira ou recurso padrão'), findsOneWidget);
    expect(find.text('Valor de venda'), findsOneWidget);

    expect(find.text('Prévia do serviço'), findsOneWidget);
    expect(find.text('Categoria e atendimento'), findsOneWidget);
    expect(find.text('Duração'), findsOneWidget);
    expect(find.text('Valor e comissão'), findsOneWidget);
    expect(find.text('Recurso padrão'), findsOneWidget);
    expect(find.text('Como aparece na agenda'), findsOneWidget);
    expect(
      tester.getTopLeft(find.text('Prévia do serviço')).dx,
      greaterThan(tester.getTopLeft(find.text('Catálogo')).dx + 250),
    );

    _expectFixedFooter(tester, frame: frame, primaryLabel: 'Salvar serviço');
    expect(tester.takeException(), isNull);
  });

  testWidgets('diálogos WPF mantêm rolagem e ações alcançáveis no celular', (
    tester,
  ) async {
    final controller = _controller(
      services: [
        ServiceItem(
          name: 'Corte feminino',
          category: 'Cabelo',
          durationMinutes: 45,
          price: 85,
        ),
      ],
      professionals: [
        Professional(
          name: 'Ana Lima',
          role: 'Cabeleireira',
          segments: ['Salão de Beleza'],
        ),
      ],
    );

    await _openDialog(
      tester,
      size: const Size(390, 844),
      open: (context) =>
          showProfessionalEditorDialog(context, controller: controller),
    );
    _expectNarrowDialog(
      tester,
      frameKey: 'professional-editor-dialog-frame',
      primaryLabel: 'Salvar e criar profissional',
    );
    await tester.tap(find.text('Cancelar'));
    await tester.pumpAndSettle();

    await _openCurrentAppDialog(
      tester,
      (context) => showServiceEditorDialog(context, controller: controller),
    );
    _expectNarrowDialog(
      tester,
      frameKey: 'service-editor-dialog-frame',
      primaryLabel: 'Salvar serviço',
    );
    await tester.tap(find.text('Cancelar'));
    await tester.pumpAndSettle();

    await _openCurrentAppDialog(
      tester,
      (context) => showServiceManagerDialog(context, controller: controller),
    );
    _expectNarrowDialog(
      tester,
      frameKey: 'service-manager-dialog-frame',
      primaryLabel: 'Novo serviço',
    );
    expect(find.text('Corte feminino'), findsWidgets);
    expect(tester.takeException(), isNull);
  });
}

Finder _textFieldWithHint(String hint) => find.byWidgetPredicate(
  (widget) => widget is TextField && widget.decoration?.hintText == hint,
  description: 'TextField with hint "$hint"',
);

Finder _elevatedButtonWithText(String text) => find.ancestor(
  of: find.text(text),
  matching: find.byWidgetPredicate((widget) => widget is ElevatedButton),
);

void _expectFixedFooter(
  WidgetTester tester, {
  required Finder frame,
  required String primaryLabel,
}) {
  final cancel = find.widgetWithText(OutlinedButton, 'Cancelar');
  final primary = _elevatedButtonWithText(primaryLabel);
  expect(cancel, findsOneWidget);
  expect(primary, findsOneWidget);

  final frameRect = tester.getRect(frame);
  final cancelRect = tester.getRect(cancel);
  final primaryRect = tester.getRect(primary);
  expect(primaryRect.center.dy, closeTo(cancelRect.center.dy, 1));
  expect(frameRect.bottom - primaryRect.bottom, inInclusiveRange(10, 24));
}

void _expectNarrowDialog(
  WidgetTester tester, {
  required String frameKey,
  required String primaryLabel,
}) {
  final frame = find.byKey(ValueKey(frameKey));
  final primary = _elevatedButtonWithText(primaryLabel);
  expect(frame, findsOneWidget);
  expect(tester.getSize(frame).width, lessThanOrEqualTo(358));
  expect(tester.getSize(frame).height, lessThanOrEqualTo(812));
  expect(
    find.descendant(of: frame, matching: find.byType(Scrollable)),
    findsWidgets,
  );
  expect(primary, findsOneWidget);
  expect(tester.getBottomRight(primary).dy, lessThanOrEqualTo(844));
  expect(tester.takeException(), isNull);
}

AgendaController _controller({
  List<ServiceItem> services = const [],
  List<Professional> professionals = const [],
}) {
  final data = AgendaData(services: services, professionals: professionals);
  data.settings
    ..businessSegment = 'Salão de Beleza'
    ..themeId = 'aesthetic-coral';
  return AgendaController(_MemoryAgendaRepository())
    ..data = data
    ..loading = false;
}

Future<void> _openDialog(
  WidgetTester tester, {
  required Size size,
  required Future<dynamic> Function(BuildContext context) open,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Builder(
        builder: (context) => Scaffold(
          body: Center(
            child: ElevatedButton(
              key: const ValueKey('open-dialog'),
              onPressed: () => open(context),
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.byKey(const ValueKey('open-dialog')));
  await tester.pumpAndSettle();
}

Future<void> _openCurrentAppDialog(
  WidgetTester tester,
  Future<dynamic> Function(BuildContext context) open,
) async {
  final context = tester.element(find.byKey(const ValueKey('open-dialog')));
  open(context);
  await tester.pumpAndSettle();
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
