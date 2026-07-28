import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/features/agenda/agenda_board.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('centraliza o estado vazio na área visível da agenda', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(800, 620);
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
        home: Scaffold(
          body: Center(
            child: SizedBox(
              width: 650,
              child: AgendaScheduleBoard(
                date: DateTime(2026, 7, 14),
                appointments: const [],
                professionals: const [],
                settings: AgendaSettings(),
                onCreate: () {},
                height: 410,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    final viewport = find.byKey(const Key('agenda-board-viewport'));
    final emptyState = find.byKey(const Key('agenda-board-empty-state'));

    expect(viewport, findsOneWidget);
    expect(emptyState, findsOneWidget);
    expect(
      tester.getCenter(emptyState).dy,
      closeTo(tester.getCenter(viewport).dy, .1),
    );
    expect(
      tester.getCenter(emptyState).dx,
      closeTo(tester.getTopLeft(viewport).dx + 68 + (650 - 68) / 2, .1),
    );
    expect(find.text('Agenda livre nesta data'), findsOneWidget);
    expect(find.text('+ Agendar horário').hitTestable(), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('mantém estado vazio e ação visíveis no quadro compacto', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 700);
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: Scaffold(
          body: Center(
            child: SizedBox(
              width: 350,
              child: AgendaScheduleBoard(
                date: DateTime(2026, 7, 19),
                appointments: const [],
                professionals: [
                  Professional(id: 'p1', name: 'Manicure 1'),
                  Professional(id: 'p2', name: 'Designer 1'),
                ],
                settings: AgendaSettings(),
                onCreate: () {},
                compact: true,
                height: 430,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();

    final viewportRect = tester.getRect(
      find.byKey(const Key('agenda-board-viewport')),
    );
    final emptyRect = tester.getRect(
      find.byKey(const Key('agenda-board-empty-state')),
    );

    expect(emptyRect.left, greaterThanOrEqualTo(viewportRect.left + 58));
    expect(emptyRect.right, lessThanOrEqualTo(viewportRect.right));
    expect(find.text('Agenda livre nesta data'), findsOneWidget);
    expect(find.text('+ Agendar horário').hitTestable(), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
