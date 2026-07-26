import 'package:agenda_livre/app/agenda_app.dart';
import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('agenda ativa consulta automaticamente a nuvem', (tester) async {
    final repository = _CountingSyncRepository();
    final controller = AgendaController(repository);

    await tester.pumpWidget(AgendaLivreApp(controller: controller));
    await tester.pump();
    expect(repository.refreshCalls, 0);

    await tester.pump(const Duration(seconds: 10));
    expect(repository.refreshCalls, 1);

    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.hidden);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
    await tester.pump(const Duration(seconds: 10));
    expect(repository.refreshCalls, 1);

    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.hidden);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
    await tester.pump();
    expect(repository.refreshCalls, 2);

    await tester.pumpWidget(const SizedBox.shrink());
  });
}

class _CountingSyncRepository
    implements AgendaRepository, AgendaSyncRepository {
  int refreshCalls = 0;
  AgendaData value = AgendaData(
    settings: AgendaSettings(
      onboardingCompleted: true,
      businessSegment: 'Barbearia',
    ),
  );

  @override
  bool get hasConflict => false;

  @override
  bool get hasTrialStatus => false;

  @override
  bool get isSyncing => false;

  @override
  String? get syncMessage => null;

  @override
  bool get trialActive => true;

  @override
  int get trialDaysRemaining => 7;

  @override
  Future<void> clear() async => value = AgendaData();

  @override
  Future<bool> hasData() async => true;

  @override
  Future<AgendaData?> load() async => value;

  @override
  Future<AgendaData> loadOrCreate() async => value;

  @override
  Future<AgendaData?> refreshRemoteIfSafe() async {
    refreshCalls++;
    return null;
  }

  @override
  Future<AgendaData?> resolveConflictUsingCloud() async => null;

  @override
  Future<AgendaData?> resolveConflictUsingLocal() async => null;

  @override
  Future<void> retrySync() async {}

  @override
  Future<void> save(AgendaData data) async => value = data;
}
