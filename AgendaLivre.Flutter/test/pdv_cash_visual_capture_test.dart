import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/pdv/pdv_cash_dialogs.dart';
import 'package:agenda_livre/features/pdv/pdv_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('pt_BR');
    await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
  });

  for (final closing in [false, true]) {
    testWidgets('capture cash ${closing ? 'closing' : 'opening'}', (
      tester,
    ) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(1366, 768);
      addTearDown(tester.view.reset);
      final now = DateTime(2026, 7, 29, 18, 24);
      final data = AgendaSeedData.salon(referenceDate: now);
      data.settings
        ..accountFullName = 'Ana Clara Souza'
        ..businessName = 'Agenda Livre';
      if (closing) {
        data.cashSessions.add(
          CashSession(
            id: 'cash-audit',
            operatorName: 'Ana Clara Souza',
            openingBalance: 150,
            openedAt: DateTime(2026, 7, 29, 8, 2),
          ),
        );
        data.manualPayments.addAll([
          ManualPayment(
            description: 'Entrada de caixa',
            paymentMethod: 'Dinheiro',
            category: 'Ajuste',
            value: 100,
            cashSessionId: 'cash-audit',
            paidAt: DateTime(2026, 7, 29, 11, 40),
          ),
          ManualPayment(
            description: 'Recebimento Pix',
            paymentMethod: 'Pix',
            value: 1080,
            cashSessionId: 'cash-audit',
            paidAt: DateTime(2026, 7, 29, 14),
          ),
          ManualPayment(
            description: 'Recebimento crédito',
            paymentMethod: 'Cartão de crédito',
            value: 820,
            cashSessionId: 'cash-audit',
            paidAt: DateTime(2026, 7, 29, 15),
          ),
          ManualPayment(
            description: 'Recebimento débito',
            paymentMethod: 'Cartão de débito',
            value: 420,
            cashSessionId: 'cash-audit',
            paidAt: DateTime(2026, 7, 29, 16),
          ),
          ManualPayment(
            description: 'Recebimento dinheiro',
            paymentMethod: 'Dinheiro',
            value: 620,
            cashSessionId: 'cash-audit',
            paidAt: DateTime(2026, 7, 29, 17),
          ),
        ]);
        data.expenses.add(
          ExpenseItem(
            description: 'Retirada de valores',
            paymentMethod: 'Dinheiro',
            value: 80,
            cashSessionId: 'cash-audit',
            date: DateTime(2026, 7, 29, 13, 22),
          ),
        );
      }
      for (final appointment in data.appointments) {
        appointment.status = AppointmentStatus.done;
      }
      final controller = AgendaController(_MemoryRepository())
        ..data = data
        ..loading = false
        ..selectedDate = DateTime(2026, 7, 29);

      const captureKey = Key('pdv-cash-visual-capture');
      await tester.pumpWidget(
        RepaintBoundary(
          key: captureKey,
          child: MaterialApp(
            theme: AgendaThemes.byId('').toThemeData().copyWith(
              textTheme: ThemeData.light().textTheme.apply(
                fontFamily: 'Segoe UI',
              ),
            ),
            home: Builder(
              builder: (context) => Stack(
                children: [
                  PdvPage(
                    controller: controller,
                    referenceNow: now,
                    onExit: () {},
                    onNavigate: (_) {},
                  ),
                  Positioned(
                    left: 0,
                    top: 0,
                    child: Opacity(
                      opacity: 0,
                      child: FilledButton(
                        key: const Key('open-cash-audit'),
                        onPressed: () {
                          if (closing) {
                            showPdvCashClosingDialog(
                              context,
                              controller,
                              referenceNow: now,
                            );
                          } else {
                            showPdvCashOpeningDialog(
                              context,
                              controller,
                              referenceNow: now,
                            );
                          }
                        },
                        child: const Text('open'),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.byKey(const Key('open-cash-audit')));
      await tester.pumpAndSettle();
      await expectLater(
        find.byKey(captureKey),
        matchesGoldenFile(
          '../artifacts/pdv-cash-wpf-parity-2026-07-29/'
          'flutter-pdv-cash-${closing ? 'closing' : 'opening'}-1366x768.png',
        ),
      );
      expect(tester.takeException(), isNull);
    });
  }
}

Future<void> _loadFont(String family, String path) async {
  final bytes = File(path).readAsBytesSync();
  final loader = FontLoader(family)
    ..addFont(Future.value(ByteData.sublistView(bytes)));
  await loader.load();
}

class _MemoryRepository implements AgendaRepository {
  @override
  Future<void> clear() async {}

  @override
  Future<bool> hasData() async => false;

  @override
  Future<AgendaData?> load() async => null;

  @override
  Future<AgendaData> loadOrCreate() async => AgendaData();

  @override
  Future<void> save(AgendaData data) async {}
}
