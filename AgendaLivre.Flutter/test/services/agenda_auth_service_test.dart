import 'dart:async';
import 'dart:convert';

import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'fake_http_transport.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test(
    'signup imediato envia o perfil e inicia a sessão sem confirmação',
    () async {
      late ServiceHttpRequest signupRequest;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/signup')) {
          signupRequest = request;
          return _authResponse(
            accessToken: 'signup-access',
            refreshToken: 'signup-refresh',
            expiresIn: 3600,
          );
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          return _jsonResponse(<String, Object?>{
            'id': 'user-1',
            'email': 'nina@example.com',
          });
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);

      final result = await auth.signUp(
        name: 'Nina Souza',
        businessName: 'Studio Nina',
        email: 'nina@example.com',
        password: 'segredo123',
      );

      expect(result.emailConfirmationRequired, isFalse);
      expect(result.session?.userId, 'user-1');
      expect(signupRequest.uri.path, '/auth/v1/signup');
      expect(signupRequest.headers['apikey'], 'sb_publishable_test');
      final body = Map<String, dynamic>.from(
        jsonDecode(signupRequest.body!) as Map,
      );
      expect(body['email'], 'nina@example.com');
      expect(body['password'], 'segredo123');
      expect(body['data'], <String, Object?>{
        'full_name': 'Nina Souza',
        'business_name': 'Studio Nina',
      });
      expect(preferences.getString(AgendaAuthService.sessionKey), isNotNull);
    },
  );

  test(
    'recuperação solicita link com callback seguro e não cria sessão',
    () async {
      late ServiceHttpRequest recoverRequest;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _configResponse();
        }
        if (request.uri.path.endsWith('/auth/v1/recover')) {
          recoverRequest = request;
          return _jsonResponse(const <String, Object?>{});
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);

      await auth.requestPasswordReset(
        email: ' nina@example.com ',
        redirectTo: Uri.parse(
          'https://app.minhaagendalivre.com.br/?theme=warm#old',
        ),
      );

      expect(recoverRequest.method, 'POST');
      expect(recoverRequest.uri.path, '/auth/v1/recover');
      final redirect = Uri.parse(
        recoverRequest.uri.queryParameters['redirect_to']!,
      );
      expect(redirect.fragment, isEmpty);
      expect(redirect.queryParameters['theme'], 'warm');
      expect(redirect.queryParameters['auth_callback'], 'recovery');
      expect(
        Map<String, dynamic>.from(jsonDecode(recoverRequest.body!) as Map),
        <String, Object?>{'email': 'nina@example.com'},
      );
      expect(auth.session, isNull);
      expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
      expect(
        AgendaAuthService.isPasswordRecoveryCallback(redirect),
        isFalse,
        reason: 'O redirect preparado ainda não contém um token de callback.',
      );
    },
  );

  test(
    'callback implícito valida token sem persistir conta autenticada',
    () async {
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _configResponse();
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          expect(request.method, 'GET');
          expect(request.headers['Authorization'], 'Bearer recovery-access');
          return _jsonResponse(<String, Object?>{
            'id': 'user-recovery',
            'email': 'nina@example.com',
          });
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);
      final callback = Uri.parse(
        'https://app.minhaagendalivre.com.br/?auth_callback=recovery'
        '#access_token=recovery-access&type=recovery&expires_in=3600',
      );

      final recovery = await auth.consumePasswordRecoveryCallback(callback);

      expect(recovery.userId, 'user-recovery');
      expect(recovery.email, 'nina@example.com');
      expect(recovery.accessToken, 'recovery-access');
      expect(auth.session, isNull);
      expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
      expect(AgendaAuthService.isPasswordRecoveryCallback(callback), isTrue);
      final sanitized = AgendaAuthService.sanitizePasswordRecoveryCallbackUri(
        callback,
      );
      expect(sanitized.toString(), 'https://app.minhaagendalivre.com.br/');
    },
  );

  test(
    'callback token_hash verifica OTP antes de aceitar recuperação',
    () async {
      late ServiceHttpRequest verifyRequest;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _configResponse();
        }
        if (request.uri.path.endsWith('/auth/v1/verify')) {
          verifyRequest = request;
          return _jsonResponse(<String, Object?>{
            'access_token': 'verified-access',
            'expires_in': 3600,
          });
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          expect(request.headers['Authorization'], 'Bearer verified-access');
          return _jsonResponse(<String, Object?>{
            'id': 'user-recovery',
            'email': 'nina@example.com',
          });
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);

      final recovery = await auth.consumePasswordRecoveryCallback(
        Uri.parse(
          'https://app.minhaagendalivre.com.br/'
          '?auth_callback=recovery&type=recovery&token_hash=otp-hash',
        ),
      );

      expect(recovery.accessToken, 'verified-access');
      expect(verifyRequest.method, 'POST');
      expect(
        Map<String, dynamic>.from(jsonDecode(verifyRequest.body!) as Map),
        <String, Object?>{'type': 'recovery', 'token_hash': 'otp-hash'},
      );
    },
  );

  test('callback expirado falha sem enviar token para a rede', () async {
    final transport = FakeHttpTransport((request) {
      fail('Callback com erro não deve fazer requisição: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    await expectLater(
      auth.consumePasswordRecoveryCallback(
        Uri.parse(
          'https://app.minhaagendalivre.com.br/?auth_callback=recovery'
          '#error=access_denied&error_code=otp_expired&error_description=expired',
        ),
      ),
      throwsA(
        isA<AgendaApiException>().having(
          (error) => error.message,
          'message',
          contains('expirou'),
        ),
      ),
    );
    expect(transport.requests, isEmpty);
    expect(auth.session, isNull);
  });

  test(
    'nova senha revalida identidade, altera usuário e encerra token',
    () async {
      var identityCalls = 0;
      late ServiceHttpRequest updateRequest;
      var logoutCalls = 0;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _configResponse();
        }
        if (request.uri.path.endsWith('/auth/v1/user') &&
            request.method == 'GET') {
          identityCalls++;
          return _jsonResponse(<String, Object?>{
            'id': 'user-recovery',
            'email': 'nina@example.com',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/user') &&
            request.method == 'PUT') {
          updateRequest = request;
          return _jsonResponse(<String, Object?>{
            'id': 'user-recovery',
            'email': 'nina@example.com',
          });
        }
        if (request.uri.path.endsWith('/auth/v1/logout')) {
          logoutCalls++;
          return _jsonResponse(const <String, Object?>{});
        }
        fail('Requisição inesperada: ${request.method} ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);
      final recovery = await auth.consumePasswordRecoveryCallback(
        Uri.parse(
          'https://app.minhaagendalivre.com.br/?auth_callback=recovery'
          '#access_token=recovery-access&type=recovery&expires_in=3600',
        ),
      );

      await auth.updateRecoveredPassword(
        recovery: recovery,
        password: 'novaSenha123',
      );

      expect(identityCalls, 2);
      expect(updateRequest.headers['Authorization'], 'Bearer recovery-access');
      expect(
        Map<String, dynamic>.from(jsonDecode(updateRequest.body!) as Map),
        <String, Object?>{'password': 'novaSenha123'},
      );
      expect(logoutCalls, 1);
      expect(auth.session, isNull);
    },
  );

  test('reenvio de confirmação usa endpoint suportado pelo Supabase', () async {
    late ServiceHttpRequest resendRequest;
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.path.endsWith('/auth/v1/resend')) {
        resendRequest = request;
        return _jsonResponse(const <String, Object?>{});
      }
      fail('Requisição inesperada: ${request.method} ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    await auth.resendSignUpConfirmation(
      email: 'nina@example.com',
      redirectTo: Uri.parse('https://app.minhaagendalivre.com.br/'),
    );

    expect(resendRequest.method, 'POST');
    expect(resendRequest.uri.path, '/auth/v1/resend');
    expect(
      Map<String, dynamic>.from(jsonDecode(resendRequest.body!) as Map),
      <String, Object?>{'type': 'signup', 'email': 'nina@example.com'},
    );
  });

  test('restore valida a identidade mesmo com token ainda valido', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      AgendaAuthService.sessionKey: jsonEncode(_storedSession()),
    });
    var identityCalls = 0;
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        identityCalls++;
        expect(request.headers['Authorization'], 'Bearer access-1');
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final restored = await auth.restoreSession();

    expect(identityCalls, 1);
    expect(restored?.userId, 'user-1');
    expect(restored?.issuer, 'https://example.supabase.co');
    expect(restored?.identityVerifiedAt, isNotNull);
    final persisted = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(AgendaAuthService.sessionKey)!) as Map,
    );
    expect(persisted['issuer'], 'https://example.supabase.co');
    expect(persisted['identityVerifiedAt'], isNotNull);
  });

  test('restore rejeita token de uma conta com id local diferente', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      AgendaAuthService.sessionKey: jsonEncode(
        _storedSession(userId: 'user-a'),
      ),
    });
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-b',
          'email': 'b@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final restored = await auth.restoreSession();

    expect(restored, isNull);
    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
    expect(
      transport.requests.where(
        (request) =>
            request.uri.queryParameters['grant_type'] == 'refresh_token',
      ),
      isEmpty,
    );
  });

  test('restore renova uma vez quando a validacao retorna 401', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      AgendaAuthService.sessionKey: jsonEncode(_storedSession()),
    });
    var identityCalls = 0;
    var refreshCalls = 0;
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        identityCalls++;
        if (request.headers['Authorization'] == 'Bearer access-1') {
          return _jsonResponse(const <String, Object?>{}, statusCode: 401);
        }
        expect(request.headers['Authorization'], 'Bearer access-2');
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      if (request.uri.queryParameters['grant_type'] == 'refresh_token') {
        refreshCalls++;
        return _authResponse(
          accessToken: 'access-2',
          refreshToken: 'refresh-2',
          expiresIn: 3600,
        );
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final restored = await auth.restoreSession();

    expect(identityCalls, 2);
    expect(refreshCalls, 1);
    expect(restored?.userId, 'user-1');
    expect(restored?.accessToken, 'access-2');
  });

  test(
    'restore offline so abre uma identidade validada anteriormente',
    () async {
      SharedPreferences.setMockInitialValues(<String, Object>{
        AgendaAuthService.sessionKey: jsonEncode(
          _storedSession(
            issuer: 'https://example.supabase.co',
            identityVerifiedAt: DateTime.now().toUtc(),
          ),
        ),
        _scopedConfigCacheKey(Uri.parse('https://agenda.example')):
            _cachedConfigEnvelope(Uri.parse('https://agenda.example')),
      });
      final transport = FakeHttpTransport((request) {
        throw const HttpTransportException('offline');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);

      final restored = await auth.restoreSession();

      expect(restored?.userId, 'user-1');
      expect(preferences.getString(AgendaAuthService.sessionKey), isNotNull);
    },
  );

  test('restore offline nao abre sessao legada ainda nao validada', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      AgendaAuthService.sessionKey: jsonEncode(_storedSession()),
    });
    final transport = FakeHttpTransport((request) {
      throw const HttpTransportException('offline');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final restored = await auth.restoreSession();

    expect(restored, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNotNull);
  });

  test('restore em voo nao sobrescreve sessao trocada por outra aba', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      AgendaAuthService.sessionKey: jsonEncode(_storedSession()),
    });
    final identityStarted = Completer<void>();
    final identityResponse = Completer<ServiceHttpResponse>();
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        identityStarted.complete();
        return identityResponse.future;
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final restoring = auth.restoreSession();
    await identityStarted.future;
    await preferences.setString(
      AgendaAuthService.sessionKey,
      jsonEncode(
        _storedSession(userId: 'user-2')
          ..['email'] = 'outra@example.com'
          ..['accessToken'] = 'access-2'
          ..['refreshToken'] = 'refresh-2',
      ),
    );
    identityResponse.complete(
      _jsonResponse(<String, Object?>{
        'id': 'user-1',
        'email': 'nina@example.com',
      }),
    );

    expect(await restoring, isNull);
    final stored = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(AgendaAuthService.sessionKey)!) as Map,
    );
    expect(stored['userId'], 'user-2');
    expect(stored['accessToken'], 'access-2');
    expect(auth.session, isNull);
  });

  test(
    'sessao v1 de desenvolvimento e descartada sem restaurar conta',
    () async {
      SharedPreferences.setMockInitialValues(<String, Object>{
        AgendaAuthService.legacySessionKey: jsonEncode(_storedSession()),
      });
      final transport = FakeHttpTransport((request) {
        fail('Sessao v1 nao pode acessar a rede: ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final auth = _auth(preferences, transport);

      final restored = await auth.restoreSession();

      expect(restored, isNull);
      expect(auth.session, isNull);
      expect(preferences.getString(AgendaAuthService.legacySessionKey), isNull);
      expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
      expect(transport.requests, isEmpty);
    },
  );

  test('login nunca persiste identidade divergente do token', () async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-a',
          refreshToken: 'refresh-a',
          expiresIn: 3600,
        );
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'b@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    await expectLater(
      auth.signIn(email: 'nina@example.com', password: 'segredo123'),
      throwsA(
        isA<AgendaApiException>().having(
          (error) => error.code,
          'code',
          'session_identity_mismatch',
        ),
      ),
    );

    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
  });

  test('refresh nunca troca a conta autenticada', () async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-a',
          refreshToken: 'refresh-a',
          expiresIn: 3600,
        );
      }
      if (request.uri.queryParameters['grant_type'] == 'refresh_token') {
        return _jsonResponse(<String, Object?>{
          'access_token': 'access-b',
          'refresh_token': 'refresh-b',
          'expires_in': 3600,
          'user': <String, Object?>{'id': 'user-b', 'email': 'b@example.com'},
        });
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);
    await auth.signIn(email: 'nina@example.com', password: 'segredo123');

    await expectLater(
      auth.accessToken(forceRefresh: true),
      throwsA(
        isA<AgendaApiException>().having(
          (error) => error.code,
          'code',
          'session_identity_mismatch',
        ),
      ),
    );

    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
  });

  test('cadastro com sessao imediata tambem revalida identidade', () async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.path.endsWith('/auth/v1/signup')) {
        return _authResponse(
          accessToken: 'signup-access',
          refreshToken: 'signup-refresh',
          expiresIn: 3600,
        );
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'b@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    await expectLater(
      auth.signUp(
        name: 'Nina Souza',
        businessName: 'Studio Nina',
        email: 'nina@example.com',
        password: 'segredo123',
      ),
      throwsA(
        isA<AgendaApiException>().having(
          (error) => error.code,
          'code',
          'session_identity_mismatch',
        ),
      ),
    );

    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
  });

  test('logout invalida validacao de login ainda em voo', () async {
    final identityStarted = Completer<void>();
    final identityResponse = Completer<ServiceHttpResponse>();
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-1',
          refreshToken: 'refresh-1',
          expiresIn: 3600,
        );
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        identityStarted.complete();
        return identityResponse.future;
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final signingIn = auth.signIn(
      email: 'nina@example.com',
      password: 'segredo123',
    );
    await identityStarted.future;
    await auth.clearLocalSession();
    identityResponse.complete(
      _jsonResponse(<String, Object?>{
        'id': 'user-1',
        'email': 'nina@example.com',
      }),
    );

    await expectLater(
      signingIn,
      throwsA(
        isA<AgendaApiException>().having(
          (error) => error.code,
          'code',
          'session_superseded',
        ),
      ),
    );
    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
  });

  test('refresh concorrente usa uma unica rotacao de token', () async {
    var refreshCalls = 0;
    final refreshStarted = Completer<void>();
    final refreshResponse = Completer<ServiceHttpResponse>();
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-1',
          refreshToken: 'refresh-1',
          expiresIn: 3600,
        );
      }
      if (request.uri.queryParameters['grant_type'] == 'refresh_token') {
        refreshCalls++;
        refreshStarted.complete();
        return refreshResponse.future;
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);
    await auth.signIn(email: 'nina@example.com', password: 'segredo123');

    final first = auth.accessToken(forceRefresh: true);
    final second = auth.accessToken(forceRefresh: true);
    await refreshStarted.future;
    expect(refreshCalls, 1);
    refreshResponse.complete(
      _authResponse(
        accessToken: 'access-2',
        refreshToken: 'refresh-2',
        expiresIn: 3600,
      ),
    );

    expect(await Future.wait(<Future<String>>[first, second]), <String>[
      'access-2',
      'access-2',
    ]);
    expect(refreshCalls, 1);
  });

  test('logout durante refresh nao ressuscita a sessao antiga', () async {
    final refreshStarted = Completer<void>();
    final refreshResponse = Completer<ServiceHttpResponse>();
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _configResponse();
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-1',
          refreshToken: 'refresh-1',
          expiresIn: 3600,
        );
      }
      if (request.uri.queryParameters['grant_type'] == 'refresh_token') {
        refreshStarted.complete();
        return refreshResponse.future;
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      fail('Requisicao inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);
    await auth.signIn(email: 'nina@example.com', password: 'segredo123');

    final refreshing = auth.refreshSession();
    await refreshStarted.future;
    await auth.clearLocalSession();
    refreshResponse.complete(
      _authResponse(
        accessToken: 'access-2',
        refreshToken: 'refresh-2',
        expiresIn: 3600,
      ),
    );

    await expectLater(
      refreshing,
      throwsA(
        isA<AgendaApiException>().having(
          (error) => error.code,
          'code',
          'session_superseded',
        ),
      ),
    );
    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
  });

  test('login exige config ao vivo e ignora cache persistido', () async {
    final apiBase = Uri.parse('https://agenda.example');
    SharedPreferences.setMockInitialValues(<String, Object>{
      _scopedConfigCacheKey(apiBase): _cachedConfigEnvelope(apiBase),
    });
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        throw const HttpTransportException('offline');
      }
      fail('Cache nao pode iniciar login: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    await expectLater(
      auth.signIn(email: 'nina@example.com', password: 'segredo123'),
      throwsA(isA<HttpTransportException>()),
    );
    expect(
      transport.requests.where(
        (request) => request.uri.queryParameters['grant_type'] == 'password',
      ),
      isEmpty,
    );
    expect(auth.session, isNull);
  });

  test('cache de config e isolado pelo apiBase', () async {
    final preferences = await SharedPreferences.getInstance();
    final transport = FakeHttpTransport((request) => _configResponse());
    final production = AgendaRemoteConfigService(
      preferences: preferences,
      transport: transport,
      apiBase: Uri.parse('https://agenda.example'),
    );
    final staging = AgendaRemoteConfigService(
      preferences: preferences,
      transport: transport,
      apiBase: Uri.parse('https://staging.agenda.example'),
    );

    expect(production.scopedCacheKey, isNot(staging.scopedCacheKey));
    await production.loadLive();
    expect(preferences.getString(production.scopedCacheKey), isNotNull);
    expect(preferences.getString(staging.scopedCacheKey), isNull);
    expect(preferences.getString('agenda_livre.remote.config.v1'), isNull);
  });

  test('login persiste a sessão e renova com refresh token', () async {
    var refreshCalls = 0;
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _jsonResponse(<String, Object?>{
          'supabaseUrl': 'https://example.supabase.co',
          'publishableKey': 'sb_publishable_test',
          'syncUrl': '/api/agenda/account/state',
        });
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-1',
          refreshToken: 'refresh-1',
          expiresIn: 1,
        );
      }
      if (request.uri.queryParameters['grant_type'] == 'refresh_token') {
        refreshCalls++;
        expect(
          Map<String, dynamic>.from(jsonDecode(request.body!) as Map),
          containsPair('refresh_token', 'refresh-1'),
        );
        return _authResponse(
          accessToken: 'access-2',
          refreshToken: 'refresh-2',
          expiresIn: 3600,
        );
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      fail('Requisição inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);

    final signedIn = await auth.signIn(
      email: 'nina@example.com',
      password: 'segredo123',
    );
    expect(signedIn.userId, 'user-1');
    expect(preferences.getString(AgendaAuthService.sessionKey), isNotNull);

    final token = await auth.accessToken();

    expect(token, 'access-2');
    expect(refreshCalls, 1);
    final stored = Map<String, dynamic>.from(
      jsonDecode(preferences.getString(AgendaAuthService.sessionKey)!) as Map,
    );
    expect(stored['refreshToken'], 'refresh-2');
  });

  test('logout remoto é best effort e sempre remove a sessão local', () async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/api/agenda/account/config')) {
        return _jsonResponse(<String, Object?>{
          'supabaseUrl': 'https://example.supabase.co',
          'publishableKey': 'sb_publishable_test',
          'syncUrl': '/api/agenda/account/state',
        });
      }
      if (request.uri.queryParameters['grant_type'] == 'password') {
        return _authResponse(
          accessToken: 'access-1',
          refreshToken: 'refresh-1',
          expiresIn: 3600,
        );
      }
      if (request.uri.path.endsWith('/auth/v1/user')) {
        return _jsonResponse(<String, Object?>{
          'id': 'user-1',
          'email': 'nina@example.com',
        });
      }
      if (request.uri.path.endsWith('/auth/v1/logout')) {
        throw const HttpTransportException('offline');
      }
      fail('Requisição inesperada: ${request.uri}');
    });
    final preferences = await SharedPreferences.getInstance();
    final auth = _auth(preferences, transport);
    await auth.signIn(email: 'nina@example.com', password: 'segredo123');

    await auth.signOut();

    expect(auth.session, isNull);
    expect(preferences.getString(AgendaAuthService.sessionKey), isNull);
  });

  test(
    'API de estado interpreta 409.remote sem sobrescrever o cliente',
    () async {
      late ServiceHttpRequest stateRequest;
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/api/agenda/account/config')) {
          return _jsonResponse(<String, Object?>{
            'supabaseUrl': 'https://example.supabase.co',
            'publishableKey': 'sb_publishable_test',
            'syncUrl': '/api/agenda/account/state',
          });
        }
        if (request.uri.queryParameters['grant_type'] == 'password') {
          return _authResponse(
            accessToken: 'access-1',
            refreshToken: 'refresh-1',
            expiresIn: 3600,
          );
        }
        if (request.uri.path.endsWith('/auth/v1/user')) {
          return _jsonResponse(<String, Object?>{
            'id': 'user-1',
            'email': 'nina@example.com',
          });
        }
        if (request.uri.path.endsWith('/api/agenda/account/state')) {
          stateRequest = request;
          return _jsonResponse(<String, Object?>{
            'ok': false,
            'error': <String, Object?>{
              'code': 'revision_conflict',
              'message': 'A agenda mudou em outro dispositivo.',
            },
            'remote': <String, Object?>{
              'exists': true,
              'revision': 9,
              'schemaVersion': 1,
              'payload': <String, Object?>{
                'Settings': <String, Object?>{'BusinessName': 'Windows'},
              },
              'updatedAt': '2026-07-18T12:00:00Z',
              'trial': <String, Object?>{'active': true, 'daysRemaining': 6},
            },
          }, statusCode: 409);
        }
        fail('Requisição inesperada: ${request.uri}');
      });
      final preferences = await SharedPreferences.getInstance();
      final config = AgendaRemoteConfigService(
        preferences: preferences,
        transport: transport,
        apiBase: Uri.parse('https://agenda.example'),
      );
      final auth = AgendaAuthService(
        preferences: preferences,
        configService: config,
        transport: transport,
      );
      await auth.signIn(email: 'nina@example.com', password: 'segredo123');
      final api = AgendaAccountApi(
        configService: config,
        authService: auth,
        transport: transport,
      );

      await expectLater(
        api.saveState(
          baseRevision: 8,
          schemaVersion: 1,
          payload: <String, dynamic>{
            'Settings': <String, dynamic>{'BusinessName': 'Web'},
          },
          deviceId: 'web-test',
        ),
        throwsA(
          isA<AgendaRevisionConflict>().having(
            (error) => error.remote.revision,
            'remote.revision',
            9,
          ),
        ),
      );
      expect(stateRequest.method, 'PUT');
      expect(stateRequest.headers['Authorization'], 'Bearer access-1');
      expect(
        Map<String, dynamic>.from(jsonDecode(stateRequest.body!) as Map),
        containsPair('baseRevision', 8),
      );
    },
  );
}

Map<String, Object?> _storedSession({
  String userId = 'user-1',
  String issuer = '',
  DateTime? identityVerifiedAt,
}) => <String, Object?>{
  'userId': userId,
  'email': 'nina@example.com',
  'accessToken': 'access-1',
  'refreshToken': 'refresh-1',
  'expiresAt': DateTime.now()
      .toUtc()
      .add(const Duration(hours: 1))
      .toIso8601String(),
  'issuer': issuer,
  'identityVerifiedAt': identityVerifiedAt?.toIso8601String(),
};

ServiceHttpResponse _configResponse() => _jsonResponse(<String, Object?>{
  'supabaseUrl': 'https://example.supabase.co',
  'publishableKey': 'sb_publishable_test',
  'syncUrl': '/api/agenda/account/state',
});

String _scopedConfigCacheKey(Uri apiBase) {
  final normalized = apiBase.toString().replaceFirst(RegExp(r'/+$'), '');
  final environment = base64Url
      .encode(utf8.encode(normalized))
      .replaceAll('=', '');
  return '${AgendaRemoteConfigService.cacheKey}.$environment';
}

String _cachedConfigEnvelope(Uri apiBase) => jsonEncode(<String, Object?>{
  'apiBase': apiBase.toString().replaceFirst(RegExp(r'/+$'), ''),
  'config': <String, Object?>{
    'supabaseUrl': 'https://example.supabase.co',
    'publishableKey': 'sb_publishable_test',
    'syncUrl': '/api/agenda/account/state',
  },
});

AgendaAuthService _auth(
  SharedPreferences preferences,
  FakeHttpTransport transport,
) {
  final config = AgendaRemoteConfigService(
    preferences: preferences,
    transport: transport,
    apiBase: Uri.parse('https://agenda.example'),
  );
  return AgendaAuthService(
    preferences: preferences,
    configService: config,
    transport: transport,
  );
}

ServiceHttpResponse _authResponse({
  required String accessToken,
  required String refreshToken,
  required int expiresIn,
}) => _jsonResponse(<String, Object?>{
  'access_token': accessToken,
  'refresh_token': refreshToken,
  'expires_in': expiresIn,
  'user': <String, Object?>{'id': 'user-1', 'email': 'nina@example.com'},
});

ServiceHttpResponse _jsonResponse(
  Map<String, Object?> body, {
  int statusCode = 200,
}) => ServiceHttpResponse(
  statusCode: statusCode,
  body: jsonEncode(body),
  headers: const <String, String>{'content-type': 'application/json'},
);
