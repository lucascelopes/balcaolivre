import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/core/ui.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('renderiza os componentes visuais do Agenda Livre', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
        home: const Scaffold(
          body: AgendaPanel(
            child: AgendaPageHeader(
              title: 'Agenda Livre',
              subtitle: 'Seu negócio em ordem.',
            ),
          ),
        ),
      ),
    );

    expect(find.text('Agenda Livre'), findsOneWidget);
    expect(find.text('Seu negócio em ordem.'), findsOneWidget);
    expect(find.byType(AgendaPanel), findsOneWidget);
  });
}
