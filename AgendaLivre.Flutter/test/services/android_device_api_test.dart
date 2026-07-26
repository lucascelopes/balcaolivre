import 'dart:convert';

import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:agenda_livre/services/android_device_api.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fake_http_transport.dart';

void main() {
  final now = DateTime.utc(2026, 7, 18, 12);
  final config = AndroidBuildConfig(
    apiBase: Uri.parse('https://agenda.example'),
    buildId: 'build-123',
    provisioningToken: 'one-time-token',
    appVersion: '1.2.3',
    businessName: 'Studio Aurora',
    logoAsset: 'assets/branding/tenant-logo.png',
    coverAsset: 'assets/branding/tenant-cover.jpg',
    paymentUrl: 'https://agenda.example/pagar',
    supportUrl: 'https://agenda.example/suporte',
    devMode: false,
  );

  test('redeems one-time build token and parses device session', () async {
    final transport = FakeHttpTransport(
      (request) => ServiceHttpResponse(
        statusCode: 200,
        body: jsonEncode(<String, Object?>{
          'device': <String, Object?>{
            'id': 'device-1',
            'token': 'opaque-device-token',
          },
          'account': <String, Object?>{'id': 'account-1'},
          'branding': <String, Object?>{
            'businessName': 'Studio Aurora',
            'logoUrl': 'https://cdn.example/logo.png',
            'photoUrl': 'https://cdn.example/cover.jpg',
          },
          'entitlement': <String, Object?>{
            'status': 'trialing',
            'canUse': true,
            'checkedAt': now.toIso8601String(),
            'leaseExpiresAt': now
                .add(const Duration(hours: 12))
                .toIso8601String(),
            'trialEndsAt': now.add(const Duration(days: 7)).toIso8601String(),
            'daysRemaining': 7,
          },
        }),
      ),
    );
    final api = AndroidDeviceApi(
      config: config,
      transport: transport,
      now: () => now,
    );

    final session = await api.redeem(
      buildId: config.buildId,
      provisioningToken: config.provisioningToken,
      deviceId: 'generated-device-id',
      appVersion: config.appVersion,
      fallbackBusinessName: config.businessName,
    );

    expect(session.accountId, 'account-1');
    expect(session.deviceId, 'device-1');
    expect(session.deviceToken, 'opaque-device-token');
    expect(session.branding.businessName, 'Studio Aurora');
    expect(session.entitlement.canUseOfflineAt(now), isTrue);
    final request = transport.requests.single;
    expect(request.method, 'POST');
    expect(request.uri.path, '/api/agenda/android/provision/redeem');
    expect(request.headers.containsKey('Authorization'), isFalse);
    final body = jsonDecode(request.body!) as Map<String, dynamic>;
    expect(body['provisioningToken'], 'one-time-token');
    expect(body['deviceId'], 'generated-device-id');
    expect(body['platform'], 'android');
  });

  test('refresh uses Device authorization and rotates opaque token', () async {
    final current = _session(now, token: 'old-token');
    final transport = FakeHttpTransport(
      (request) => ServiceHttpResponse(
        statusCode: 200,
        body: jsonEncode(<String, Object?>{
          'device': <String, Object?>{'token': 'new-token'},
          'account': <String, Object?>{'id': current.accountId},
          'branding': current.branding.toJson(),
          'entitlement': current.entitlement.toJson(),
        }),
      ),
    );
    final api = AndroidDeviceApi(
      config: config,
      transport: transport,
      now: () => now,
    );

    final refreshed = await api.refresh(current);

    expect(refreshed.deviceToken, 'new-token');
    expect(
      transport.requests.single.headers['Authorization'],
      'Device old-token',
    );
    expect(
      jsonDecode(transport.requests.single.body!)['deviceId'],
      current.deviceId,
    );
  });

  test('offline lease is clamped to 24 hours and to trial end', () {
    final entitlement = AndroidEntitlement.fromJson(<String, Object?>{
      'status': 'trialing',
      'canUse': true,
      'checkedAt': now.toIso8601String(),
      'leaseExpiresAt': now.add(const Duration(days: 30)).toIso8601String(),
      'trialEndsAt': now.add(const Duration(hours: 4)).toIso8601String(),
    }, receivedAt: now);

    expect(entitlement.leaseExpiresAt, now.add(const Duration(hours: 4)));
    expect(
      entitlement.canUseOfflineAt(now.add(const Duration(hours: 5))),
      isFalse,
    );
  });

  test('state API blocks immediately on HTTP 402', () async {
    AndroidEntitlement? reported;
    final transport = FakeHttpTransport(
      (_) => ServiceHttpResponse(
        statusCode: 402,
        body: jsonEncode(<String, Object?>{
          'error': <String, Object?>{
            'code': 'subscription_required',
            'message': 'Teste encerrado',
          },
          'entitlement': <String, Object?>{
            'status': 'past_due',
            'canUse': false,
            'checkedAt': now.toIso8601String(),
            'leaseExpiresAt': now.toIso8601String(),
          },
        }),
      ),
    );
    final current = _session(now);
    final api = AndroidAgendaAccountApi(
      config: config,
      transport: transport,
      sessionProvider: ({bool forceRefresh = false}) async => current,
      onEntitlement: (value) => reported = value,
    );

    await expectLater(api.fetchState(), throwsA(isA<AgendaApiException>()));
    expect(reported?.status, 'past_due');
    expect(reported?.canUse, isFalse);
  });
}

AndroidDeviceSession _session(DateTime now, {String token = 'device-token'}) =>
    AndroidDeviceSession(
      accountId: 'account-1',
      deviceId: 'device-1',
      deviceToken: token,
      branding: const AndroidBranding(businessName: 'Studio Aurora'),
      entitlement: AndroidEntitlement(
        status: 'active',
        canUse: true,
        checkedAt: now,
        leaseExpiresAt: now.add(const Duration(hours: 12)),
      ),
    );
