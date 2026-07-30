import 'package:agenda_livre/app/web_agenda_session.dart';
import 'package:agenda_livre/features/subscription/agenda_subscription_pages.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('cada conta recebe exatamente tres dias distintos de lembrete', () {
    final first = agendaTrialReminderDaysForUser('conta-nova-1');
    final second = agendaTrialReminderDaysForUser('conta-nova-2');

    expect(first, hasLength(3));
    expect(first.every((day) => day >= 1 && day <= 7), isTrue);
    expect(agendaTrialReminderDaysForUser('conta-nova-1'), equals(first));
    expect(second, hasLength(3));
  });

  testWidgets(
    'aviso mobile informa dias, nao exige cartao e pode ser fechado',
    (tester) async {
      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      SharedPreferences.setMockInitialValues(<String, Object>{});
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: _UnusedTransport(),
        apiBase: Uri.parse('https://agenda.example'),
      );
      addTearDown(session.dispose);
      var closed = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Stack(
              children: [
                const Center(child: Text('Agenda ativa')),
                AgendaSubscriptionReminder(
                  session: session,
                  daysRemaining: 6,
                  expired: false,
                  onClose: () => closed = true,
                ),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.text('Teste grátis • 6 dias restantes'), findsOneWidget);
      expect(find.textContaining('não exigem cartão'), findsOneWidget);
      expect(find.textContaining('Stripe'), findsWidgets);
      expect(find.text('Agenda ativa'), findsOneWidget);

      await tester.tap(find.byKey(const Key('subscription-reminder-later')));
      await tester.pump();

      expect(closed, isTrue);
    },
  );
}

class _UnusedTransport implements HttpTransport {
  @override
  Future<ServiceHttpResponse> send(ServiceHttpRequest request) {
    throw StateError('Nenhuma requisição era esperada: ${request.uri}');
  }
}
