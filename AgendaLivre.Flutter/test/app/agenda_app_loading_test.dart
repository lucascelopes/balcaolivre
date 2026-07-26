import 'dart:async';

import 'package:agenda_livre/app/agenda_app.dart';
import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('loading mostra a marca sem cartão decorativo', (tester) async {
    final repository = _PendingAgendaRepository();
    final controller = AgendaController(repository);

    await tester.pumpWidget(AgendaLivreApp(controller: controller));
    await tester.pump();

    final logo = find.byKey(const Key('agenda-splash-logo'));
    expect(logo, findsOneWidget);
    expect(
      find.ancestor(of: logo, matching: find.byType(Container)),
      findsNothing,
    );
    expect(find.byType(LinearProgressIndicator), findsNothing);
    expect(find.text('Agenda Livre'), findsNothing);
    expect(tester.getSize(logo), const Size(190, 92));

    repository.complete();
    await tester.pumpAndSettle();
    await tester.pumpWidget(const SizedBox.shrink());
  });
}

class _PendingAgendaRepository implements AgendaRepository {
  final _load = Completer<AgendaData>();

  void complete() {
    if (_load.isCompleted) return;
    _load.complete(
      AgendaData(
        settings: AgendaSettings(
          onboardingCompleted: true,
          businessSegment: 'Salão de Beleza',
        ),
      ),
    );
  }

  @override
  Future<void> clear() async {}

  @override
  Future<bool> hasData() async => false;

  @override
  Future<AgendaData?> load() async => null;

  @override
  Future<AgendaData> loadOrCreate() => _load.future;

  @override
  Future<void> save(AgendaData data) async {}
}
