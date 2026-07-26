import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/models/customer.dart';
import 'package:agenda_livre/domain/models/professional.dart';
import 'package:agenda_livre/domain/models/product_item.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/establishment/editor_dialogs.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('Criar cliente reproduz o modal WPF no desktop', (tester) async {
    final controller = _controller();
    await _openDialog(
      tester,
      size: const Size(1365, 683),
      open: (context) =>
          showCustomerEditorDialog(context, controller: controller),
    );

    expect(
      tester.getSize(find.byKey(const ValueKey('customer-dialog-frame'))),
      const Size(620, 544),
    );
    expect(find.text('Criar cliente'), findsOneWidget);
    expect(
      find.text(
        'Cadastre os dados essenciais para agendar e manter o histórico.',
      ),
      findsOneWidget,
    );
    expect(find.text('Dados do cliente'), findsOneWidget);
    expect(find.text('Nome do cliente'), findsOneWidget);
    expect(find.text('WhatsApp principal'), findsOneWidget);
    expect(find.text('Tags'), findsOneWidget);
    expect(find.text('Segmento'), findsOneWidget);
    expect(find.text('Preferência de horário'), findsOneWidget);
    expect(find.text('Preferências, alergias e observações'), findsOneWidget);
    expect(find.text('Manhã'), findsOneWidget);
    expect(find.text('Tarde'), findsOneWidget);
    expect(find.text('Noite'), findsOneWidget);
    expect(find.text('Cancelar'), findsOneWidget);
    expect(find.text('Salvar cliente'), findsOneWidget);
    expect(find.text('E-mail'), findsNothing);
    expect(find.text('Documento'), findsNothing);
    expect(find.text('Aceita contato por WhatsApp'), findsNothing);
    expect(
      tester.getSize(find.widgetWithText(OutlinedButton, 'Cancelar')),
      const Size(130, 44),
    );
    expect(
      tester.getSize(find.widgetWithText(ElevatedButton, 'Salvar cliente')),
      const Size(154, 44),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('Editar cliente preserva campos ocultos e o perfil WPF', (
    tester,
  ) async {
    final lastSeenAt = DateTime(2026, 7, 10, 14, 30);
    final original = Customer(
      id: 'customer-1',
      name: 'Cliente original',
      phone: '(11) 99999-0000',
      email: 'cliente@example.com',
      document: '123.456.789-00',
      segment: 'Centro de Estética',
      profile: 'Preferência de horário: Tarde.\nSem perfume',
      tags: 'VIP',
      notes: 'Campo interno preservado',
      acceptsWhatsApp: false,
      lastSeenAt: lastSeenAt,
    );
    final controller = _controller(customers: [original]);
    await _openDialog(
      tester,
      size: const Size(1365, 683),
      open: (context) => showCustomerEditorDialog(
        context,
        controller: controller,
        customer: original,
      ),
    );

    expect(find.text('Editar cliente'), findsOneWidget);
    expect(find.text('Salvar alterações'), findsOneWidget);
    expect(find.text('Sem perfume'), findsOneWidget);

    await tester.enterText(find.byType(TextField).first, 'Cliente atualizado');
    await tester.tap(find.text('Noite'));
    await tester.enterText(find.byType(TextField).at(3), 'Sem fragrância');
    await tester.tap(find.text('Salvar alterações'));
    await tester.pumpAndSettle();

    final saved = controller.data.customers.single;
    expect(saved.name, 'Cliente atualizado');
    expect(saved.email, original.email);
    expect(saved.document, original.document);
    expect(saved.notes, original.notes);
    expect(saved.acceptsWhatsApp, isFalse);
    expect(saved.lastSeenAt, lastSeenAt);
    expect(saved.profile, 'Preferência de horário: Noite.\nSem fragrância');
    expect(tester.takeException(), isNull);
  });

  testWidgets('Gerenciar profissionais reproduz busca e tabela WPF', (
    tester,
  ) async {
    final controller = _controller(
      professionals: [
        Professional(
          id: 'professional-1',
          name: 'Ana Lima',
          role: 'Cabeleireira',
          segments: ['Salão de Beleza'],
          phone: '(11) 98888-7777',
        ),
      ],
    );
    await _openDialog(
      tester,
      size: const Size(1365, 683),
      open: (context) =>
          showProfessionalManagerDialog(context, controller: controller),
    );

    expect(
      tester.getSize(
        find.byKey(const ValueKey('professional-manager-dialog-frame')),
      ),
      const Size(828, 338),
    );
    expect(find.text('Gerenciar profissionais'), findsOneWidget);
    expect(
      find.text('Equipe, função e vínculo com o segmento da agenda.'),
      findsOneWidget,
    );
    expect(find.text('1 de 1 profissional'), findsOneWidget);
    expect(find.text('PROFISSIONAL'), findsOneWidget);
    expect(find.text('FUNÇÃO'), findsOneWidget);
    expect(find.text('SEGMENTO'), findsOneWidget);
    expect(find.text('STATUS'), findsOneWidget);
    expect(find.text('AÇÕES'), findsOneWidget);
    expect(find.text('Ana Lima'), findsOneWidget);
    expect(find.text('Cabeleireira'), findsOneWidget);
    expect(find.text('Salão de Beleza'), findsOneWidget);
    expect(find.text('Ativo'), findsOneWidget);
    expect(find.text('Editar'), findsOneWidget);
    expect(find.text('Cancelar'), findsOneWidget);
    expect(find.text('Novo profissional'), findsOneWidget);
    expect(
      tester.getSize(find.widgetWithText(OutlinedButton, 'Cancelar')),
      const Size(108, 40),
    );
    expect(
      tester.getSize(find.widgetWithText(ElevatedButton, 'Novo profissional')),
      const Size(164, 40),
    );

    await tester.enterText(find.byType(TextField), 'sem resultado');
    await tester.pump();
    expect(find.text('0 de 1 profissional'), findsOneWidget);
    expect(find.text('Nenhum profissional encontrado.'), findsOneWidget);

    await tester.enterText(find.byType(TextField), 'Ana');
    await tester.pump();
    expect(find.text('Ana Lima'), findsOneWidget);
    expect(find.text('1 de 1 profissional'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  for (final size in <Size>[const Size(390, 844), const Size(844, 390)]) {
    testWidgets('diálogos mantêm conteúdo e ações visíveis em '
        '${size.width.toInt()}x${size.height.toInt()}', (tester) async {
      final controller = _controller(
        professionals: [
          Professional(
            name: 'Ana Lima',
            role: 'Cabeleireira',
            segments: ['Salão de Beleza'],
            phone: '(11) 98888-7777',
          ),
        ],
      );
      await _openDialog(
        tester,
        size: size,
        open: (context) =>
            showCustomerEditorDialog(context, controller: controller),
      );

      final customerFrame = find.byKey(const ValueKey('customer-dialog-frame'));
      expect(tester.getSize(customerFrame).width, lessThanOrEqualTo(620));
      expect(
        tester.getSize(customerFrame).height,
        lessThanOrEqualTo(size.height - 32),
      );
      expect(find.text('Salvar cliente'), findsOneWidget);
      expect(
        tester
            .getBottomRight(
              find.widgetWithText(ElevatedButton, 'Salvar cliente'),
            )
            .dy,
        lessThanOrEqualTo(size.height),
      );
      expect(tester.takeException(), isNull);

      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();
      await _openDialog(
        tester,
        size: size,
        open: (context) =>
            showProfessionalManagerDialog(context, controller: controller),
      );

      final professionalFrame = find.byKey(
        const ValueKey('professional-manager-dialog-frame'),
      );
      expect(tester.getSize(professionalFrame).width, lessThanOrEqualTo(828));
      expect(
        tester.getSize(professionalFrame).height,
        lessThanOrEqualTo(size.height - 32),
      );
      expect(find.text('Ana Lima'), findsOneWidget);
      expect(find.text('Novo profissional'), findsOneWidget);
      expect(
        tester
            .getBottomRight(
              find.widgetWithText(ElevatedButton, 'Novo profissional'),
            )
            .dy,
        lessThanOrEqualTo(size.height),
      );
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('cadastra produto com estoque no celular sem overflow', (
    tester,
  ) async {
    final controller = _controller();
    await _openDialog(
      tester,
      size: const Size(390, 844),
      open: (context) =>
          showProductEditorDialog(context, controller: controller),
    );

    expect(
      find.byKey(const ValueKey('product-editor-dialog-frame')),
      findsOneWidget,
    );
    await tester.enterText(
      find.byKey(const Key('product-name-field')),
      'Shampoo profissional',
    );
    await tester.enterText(
      find.byKey(const Key('product-price-field')),
      '49,90',
    );
    await tester.enterText(find.byKey(const Key('product-stock-field')), '8');
    await tester.tap(find.text('Salvar produto'));
    await tester.pumpAndSettle();

    final product = controller.data.products.single;
    expect(product.name, 'Shampoo profissional');
    expect(product.price, 49.90);
    expect(product.stockQuantity, 8);
    expect(tester.takeException(), isNull);
  });
}

AgendaController _controller({
  List<Customer> customers = const [],
  List<Professional> professionals = const [],
  List<ProductItem> products = const [],
}) {
  final data = AgendaData(
    customers: customers,
    professionals: professionals,
    products: products,
  );
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
