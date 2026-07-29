import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/support/support_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  for (final size in const [Size(1366, 768), Size(390, 844), Size(844, 390)]) {
    testWidgets(
      'central de ajuda permanece responsiva em ${size.width}x${size.height}',
      (tester) async {
        await _pumpSupport(tester, size);

        expect(find.text('Central de ajuda'), findsOneWidget);
        expect(find.text('Tópicos mais acessados'), findsOneWidget);
        expect(find.text('Chat no Agenda Livre'), findsOneWidget);
        expect(find.text('Diagnóstico rápido'), findsOneWidget);
        expect(tester.takeException(), isNull);
      },
    );
  }

  testWidgets('busca guias e envia uma mensagem no chat', (tester) async {
    await _pumpSupport(tester, const Size(1366, 768));

    await tester.enterText(
      find.byKey(const Key('support-search-field')),
      'agendamento',
    );
    await tester.tap(find.byKey(const Key('support-search-button')));
    await tester.pump();
    expect(find.byKey(const Key('support-search-result')), findsOneWidget);
    expect(find.textContaining('Encontramos'), findsOneWidget);

    await tester.tap(find.byKey(const Key('support-open-chat')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('support-chat-message')),
      'Preciso reagendar um cliente',
    );
    await tester.tap(find.byKey(const Key('support-chat-send')));
    await tester.pump();
    expect(find.text('Preciso reagendar um cliente'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

Future<void> _pumpSupport(WidgetTester tester, Size size) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);

  final data = AgendaData();
  final controller = AgendaController(_MemoryAgendaRepository(data))
    ..data = data
    ..loading = false;
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Scaffold(body: SupportPage(controller: controller)),
    ),
  );
  await tester.pump();
}

class _MemoryAgendaRepository implements AgendaRepository {
  _MemoryAgendaRepository(this.data);

  AgendaData data;

  @override
  Future<void> clear() async => data = AgendaData();

  @override
  Future<bool> hasData() async => true;

  @override
  Future<AgendaData?> load() async => data;

  @override
  Future<AgendaData> loadOrCreate() async => data;

  @override
  Future<void> save(AgendaData value) async => data = value;
}
