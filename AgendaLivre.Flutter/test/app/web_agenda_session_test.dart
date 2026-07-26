import 'dart:async';
import 'dart:convert';

import 'package:agenda_livre/app/web_agenda_session.dart';
import 'package:agenda_livre/data/repositories/shared_preferences_agenda_repository.dart';
import 'package:agenda_livre/data/seed/agenda_seed_data.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/domain/models/agenda_settings.dart';
import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../services/fake_http_transport.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test('dados legados sem login nunca abrem a conta demonstrativa', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      SharedPreferencesAgendaRepository.defaultStorageKey: jsonEncode(
        AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14)).toJson(),
      ),
    });
    final preferences = await SharedPreferences.getInstance();
    final transport = FakeHttpTransport((request) {
      fail('Sem sessao, nenhuma requisicao deve ser feita: ${request.uri}');
    });
    final session = AgendaWebSessionController(
      preferences: preferences,
      transport: transport,
      apiBase: Uri.parse('https://agenda.example'),
    );
    addTearDown(session.dispose);

    await session.initialize();

    expect(session.initializing, isFalse);
    expect(session.authSession, isNull);
    expect(session.agendaController, isNull);
    expect(transport.requests, isEmpty);
  });

  test(
    'callback de recuperação limpa URL e nunca abre a conta persistida',
    () async {
      SharedPreferences.setMockInitialValues(<String, Object>{
        AgendaAuthService.sessionKey: jsonEncode(<String, Object?>{
          'userId': 'old-user',
          'email': 'old@example.com',
          'accessToken': 'old-access',
          'refreshToken': 'old-refresh',
          'expiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(hours: 1))
              .toIso8601String(),
          'issuer': 'https://example.supabase.co',
          'identityVerifiedAt': DateTime.now().toUtc().toIso8601String(),
        }),
      });
      final callback = Uri.parse(
        'https://app.minhaagendalivre.com.br/?auth_callback=recovery'
        '#access_token=recovery-access&type=recovery&expires_in=3600',
      );
      Uri? replacedUri;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          expect(request.headers['Authorization'], 'Bearer recovery-access');
          return _jsonResponse(<String, Object?>{
            'id': 'recovery-user',
            'email': 'nina@example.com',
          });
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
        authCallbackUriProvider: () => callback,
        authCallbackUriReplacer: (uri) => replacedUri = uri,
      );
      addTearDown(session.dispose);

      await session.initialize();

      expect(replacedUri.toString(), 'https://app.minhaagendalivre.com.br/');
      expect(session.passwordRecoveryPending, isTrue);
      expect(session.passwordRecoveryEmail, 'nina@example.com');
      expect(session.authSession, isNull);
      expect(session.agendaController, isNull);
      expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
    },
  );

  test(
    'sessao restaurada abre somente os dados do usuario autenticado',
    () async {
      final userData = AgendaData(
        settings: AgendaSettings(
          businessName: 'Studio Nina',
          businessSegment: 'Salao e beleza',
          onboardingCompleted: true,
        ),
      );
      SharedPreferences.setMockInitialValues(<String, Object>{
        AgendaAuthService.sessionKey: jsonEncode(<String, Object?>{
          'userId': 'user-nina',
          'email': 'nina@example.com',
          'accessToken': 'access-nina',
          'refreshToken': 'refresh-nina',
          'expiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(hours: 1))
              .toIso8601String(),
        }),
        AgendaWebSessionController.storageKeyForUser('user-nina'): jsonEncode(
          userData.toJson(),
        ),
        SharedPreferencesAgendaRepository.defaultStorageKey: jsonEncode(
          AgendaSeedData.salon(referenceDate: DateTime(2026, 7, 14)).toJson(),
        ),
      });
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          expect(request.headers['Authorization'], 'Bearer access-nina');
          return _jsonResponse(<String, Object?>{
            'id': 'user-nina',
            'email': 'nina@example.com',
          });
        }
        if (request.uri.path.endsWith('/api/agenda/account/state') &&
            request.method == 'GET') {
          expect(request.headers['Authorization'], 'Bearer access-nina');
          return _jsonResponse(_emptyRemoteState());
        }
        fail('Requisicao inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
      );
      addTearDown(session.dispose);

      await session.initialize();
      final controller = session.agendaController;
      expect(session.authSession?.userId, 'user-nina');
      expect(controller, isNotNull);

      await controller!.initialize();

      expect(controller.loadError, isNull);
      expect(controller.businessName, 'Studio Nina');
      expect(controller.businessName, isNot('Lucas Barbearia'));
    },
  );

  test(
    'troca de conta em outra aba aposenta a conta atual antes de validar',
    () async {
      final validationStarted = Completer<void>();
      final validationResponse = Completer<ServiceHttpResponse>();
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/token') &&
            request.uri.queryParameters['grant_type'] == 'password') {
          return _authResponse(
            userId: 'user-a',
            email: 'a@example.com',
            accessToken: 'access-a',
            refreshToken: 'refresh-a',
          );
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          if (request.headers['Authorization'] == 'Bearer access-a') {
            return _jsonResponse(<String, Object?>{
              'id': 'user-a',
              'email': 'a@example.com',
            });
          }
          expect(request.headers['Authorization'], 'Bearer access-b');
          if (!validationStarted.isCompleted) validationStarted.complete();
          return validationResponse.future;
        }
        fail('Requisicao inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
      );
      addTearDown(session.dispose);

      await session.initialize();
      await session.signIn(email: 'a@example.com', password: 'segredo123');
      final controllerA = session.agendaController;
      expect(controllerA, isNotNull);

      await preferences.setString(
        AgendaAuthService.sessionKey,
        jsonEncode(<String, Object?>{
          'userId': 'user-b',
          'email': 'b@example.com',
          'accessToken': 'access-b',
          'refreshToken': 'refresh-b',
          'expiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(hours: 1))
              .toIso8601String(),
          'issuer': 'https://example.supabase.co',
          'identityVerifiedAt': DateTime.now().toUtc().toIso8601String(),
        }),
      );

      final reconcile = session.reconcilePersistedSession();
      await validationStarted.future;

      expect(session.agendaController, isNull);

      validationResponse.complete(
        _jsonResponse(<String, Object?>{
          'id': 'user-b',
          'email': 'b@example.com',
        }),
      );
      await reconcile;

      expect(session.authSession?.userId, 'user-b');
      expect(session.agendaController, isNotNull);
      expect(session.agendaController, isNot(same(controllerA)));
    },
  );

  test('401 atrasado da conta aposentada não encerra a sessão atual', () async {
    final oldSaveStarted = Completer<void>();
    final oldSaveResponse = Completer<ServiceHttpResponse>();
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _jsonResponse(<String, Object?>{
          'supabaseUrl': 'https://example.supabase.co',
          'publishableKey': 'sb_publishable_test',
          'syncUrl': '/api/agenda/account/state',
        });
      }
      if (request.uri.path.endsWith('/auth/v1/token') &&
          request.uri.queryParameters['grant_type'] == 'password') {
        final body = Map<String, dynamic>.from(
          jsonDecode(request.body!) as Map,
        );
        final accountB = body['email'] == 'b@example.com';
        return _authResponse(
          userId: accountB ? 'user-b' : 'user-a',
          email: accountB ? 'b@example.com' : 'a@example.com',
          accessToken: accountB ? 'access-b' : 'access-a',
          refreshToken: accountB ? 'refresh-b' : 'refresh-a',
        );
      }
      if (request.uri.path.endsWith('/auth/v1/logout')) {
        return _jsonResponse(const <String, Object?>{});
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        final accountB = request.headers['Authorization'] == 'Bearer access-b';
        return _jsonResponse(<String, Object?>{
          'id': accountB ? 'user-b' : 'user-a',
          'email': accountB ? 'b@example.com' : 'a@example.com',
        });
      }
      if (request.uri.path.endsWith('/api/agenda/account/state') &&
          request.method == 'GET') {
        return _jsonResponse(_emptyRemoteState());
      }
      if (request.uri.path.endsWith('/api/agenda/account/state') &&
          request.method == 'PUT') {
        expect(request.headers['Authorization'], 'Bearer access-a');
        if (!oldSaveStarted.isCompleted) oldSaveStarted.complete();
        return oldSaveResponse.future;
      }
      fail('Requisição inesperada: ${request.method} ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final session = AgendaWebSessionController(
      preferences: preferences,
      transport: transport,
      apiBase: Uri.parse('https://agenda.example'),
    );
    addTearDown(session.dispose);

    await session.initialize();
    await session.signIn(email: 'a@example.com', password: 'segredo123');
    final oldController = session.agendaController!;
    await oldController.initialize();
    await oldController.updateSettings(
      (settings) => settings.businessName = 'Alteração da conta A',
    );
    await oldSaveStarted.future;

    await session.signOut();
    await session.signIn(email: 'b@example.com', password: 'segredo123');
    final currentController = session.agendaController;
    expect(session.authSession?.userId, 'user-b');
    expect(currentController, isNotNull);

    oldSaveResponse.complete(
      _jsonResponse(<String, Object?>{
        'error': <String, Object?>{
          'code': 'unauthorized',
          'message': 'Token antigo revogado.',
        },
      }, statusCode: 401),
    );
    await _settleAsync();

    expect(session.authSession?.userId, 'user-b');
    expect(session.agendaController, same(currentController));
    expect(session.errorMessage, isNull);
    expect(
      transport.requests.where(
        (request) =>
            request.uri.queryParameters['grant_type'] == 'refresh_token',
      ),
      isEmpty,
      reason: 'A operação antiga não pode renovar usando a conta B.',
    );
    final stored = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(AgendaAuthService.sessionKey)!) as Map,
    );
    expect(stored['userId'], 'user-b');
  });
  test(
    'reconcile atrasado nunca sobrescreve um novo login explicito',
    () async {
      final validationBStarted = Completer<void>();
      final validationBResponse = Completer<ServiceHttpResponse>();
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/token') &&
            request.uri.queryParameters['grant_type'] == 'password') {
          final body = Map<String, dynamic>.from(
            jsonDecode(request.body!) as Map,
          );
          final accountC = body['email'] == 'c@example.com';
          return _authResponse(
            userId: accountC ? 'user-c' : 'user-a',
            email: accountC ? 'c@example.com' : 'a@example.com',
            accessToken: accountC ? 'access-c' : 'access-a',
            refreshToken: accountC ? 'refresh-c' : 'refresh-a',
          );
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          final authorization = request.headers['Authorization'];
          if (authorization == 'Bearer access-b') {
            if (!validationBStarted.isCompleted) validationBStarted.complete();
            return validationBResponse.future;
          }
          final accountC = authorization == 'Bearer access-c';
          return _jsonResponse(<String, Object?>{
            'id': accountC ? 'user-c' : 'user-a',
            'email': accountC ? 'c@example.com' : 'a@example.com',
          });
        }
        fail('Requisicao inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
      );
      addTearDown(session.dispose);

      await session.initialize();
      await session.signIn(email: 'a@example.com', password: 'segredo123');
      expect(session.authSession?.userId, 'user-a');

      await preferences.setString(
        AgendaAuthService.sessionKey,
        jsonEncode(<String, Object?>{
          'userId': 'user-b',
          'email': 'b@example.com',
          'accessToken': 'access-b',
          'refreshToken': 'refresh-b',
          'expiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(hours: 1))
              .toIso8601String(),
          'issuer': 'https://example.supabase.co',
          'identityVerifiedAt': DateTime.now().toUtc().toIso8601String(),
        }),
      );

      final reconcile = session.reconcilePersistedSession();
      await validationBStarted.future;
      final loginC = session.signIn(
        email: 'c@example.com',
        password: 'segredo123',
      );
      validationBResponse.complete(
        _jsonResponse(<String, Object?>{
          'id': 'user-b',
          'email': 'b@example.com',
        }),
      );

      await reconcile;
      await loginC;

      expect(session.authSession?.userId, 'user-c');
      expect(session.agendaController, isNotNull);
      final stored = Map<String, dynamic>.from(
        jsonDecode(preferences.getString(AgendaAuthService.sessionKey)!) as Map,
      );
      expect(stored['userId'], 'user-c');
    },
  );

  test(
    'assinatura da landing abre Stripe apos restaurar a conta da nuvem',
    () async {
      SharedPreferences.setMockInitialValues(<String, Object>{
        AgendaAuthService.sessionKey: jsonEncode(<String, Object?>{
          'userId': 'user-stripe',
          'email': 'stripe@example.com',
          'accessToken': 'access-stripe',
          'refreshToken': 'refresh-stripe',
          'expiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(hours: 1))
              .toIso8601String(),
          'issuer': 'https://example.supabase.co',
          'identityVerifiedAt': DateTime.now().toUtc().toIso8601String(),
        }),
      });
      final browserUri = Uri.parse(
        'https://app.minhaagendalivre.com.br/?subscribe=mensal',
      );
      Uri? replacedUri;
      Uri? launchedCheckout;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          expect(request.headers['Authorization'], 'Bearer access-stripe');
          return _jsonResponse(<String, Object?>{
            'id': 'user-stripe',
            'email': 'stripe@example.com',
          });
        }
        if (request.uri.path.endsWith('/api/agenda/android/checkout')) {
          expect(request.method, 'POST');
          expect(request.headers['Authorization'], 'Bearer access-stripe');
          final body = Map<String, dynamic>.from(
            jsonDecode(request.body!) as Map,
          );
          expect(body['plan'], 'mensal');
          expect(body['idempotencyKey'], contains('user-stripe-mensal-'));
          return _jsonResponse(<String, Object?>{
            'ok': true,
            'checkout': <String, Object?>{
              'url': 'https://checkout.stripe.com/c/pay/test-session',
            },
          });
        }
        fail('Requisicao inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final session = AgendaWebSessionController(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
        authCallbackUriProvider: () => browserUri,
        authCallbackUriReplacer: (uri) => replacedUri = uri,
        checkoutLauncher: (uri) async {
          launchedCheckout = uri;
          return true;
        },
      );
      addTearDown(session.dispose);

      await session.initialize();

      expect(
        launchedCheckout,
        Uri.parse('https://checkout.stripe.com/c/pay/test-session'),
      );
      expect(replacedUri, Uri.parse('https://app.minhaagendalivre.com.br/'));
      expect(session.authSession?.userId, 'user-stripe');
      expect(session.errorMessage, isNull);
    },
  );
}

Map<String, Object?> _emptyRemoteState() => <String, Object?>{
  'exists': false,
  'revision': 0,
  'schemaVersion': 1,
  'payload': null,
  'updatedAt': null,
  'trial': <String, Object?>{'active': true, 'daysRemaining': 7},
};

ServiceHttpResponse _authResponse({
  required String userId,
  required String email,
  required String accessToken,
  required String refreshToken,
}) => _jsonResponse(<String, Object?>{
  'access_token': accessToken,
  'refresh_token': refreshToken,
  'expires_in': 3600,
  'user': <String, Object?>{'id': userId, 'email': email},
});

ServiceHttpResponse _jsonResponse(
  Map<String, Object?> body, {
  int statusCode = 200,
}) => ServiceHttpResponse(
  statusCode: statusCode,
  body: jsonEncode(body),
  headers: const <String, String>{'content-type': 'application/json'},
);

Future<void> _settleAsync() async {
  await Future<void>.delayed(Duration.zero);
  await Future<void>.delayed(Duration.zero);
  await Future<void>.delayed(Duration.zero);
}
