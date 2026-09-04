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

  testWidgets(
    'bloqueio expirado desfoca a agenda e oferece renovacao e saida',
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

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Stack(
              children: [
                const Center(child: Text('Home da agenda')),
                AgendaSubscriptionReminder(
                  session: session,
                  daysRemaining: 0,
                  expired: true,
                  onClose: () => fail('bloqueio expirado nao pode ser fechado'),
                ),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(BackdropFilter), findsOneWidget);
      expect(find.byIcon(Icons.lock_outline_rounded), findsOneWidget);
      expect(find.text('Renove para continuar com sua agenda'), findsOneWidget);
      expect(find.byKey(const Key('subscription-lock-renew')), findsOneWidget);
      expect(
        find.byKey(const Key('subscription-lock-sign-out')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('subscription-reminder-close')),
        findsNothing,
      );
      expect(
        find.byKey(const Key('subscription-reminder-later')),
        findsNothing,
      );
      expect(tester.takeException(), isNull);
    },
  );
}

class _UnusedTransport implements HttpTransport {
  @override
  Future<ServiceHttpResponse> send(ServiceHttpRequest request) {
    throw StateError('Nenhuma requisição era esperada: ${request.uri}');
  }
}
