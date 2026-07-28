import 'dart:convert';
import 'dart:math';

import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/mercado_pago_service.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fake_http_transport.dart';

void main() {
  group('MercadoPagoService', () {
    test(
      'activates the signed client once before payment operations',
      () async {
        final transport = FakeHttpTransport((request) {
          if (request.uri.path.endsWith('/license/activate')) {
            return const ServiceHttpResponse(
              statusCode: 200,
              body: '{"ok":true,"message":"activated"}',
            );
          }
          return const ServiceHttpResponse(
            statusCode: 200,
            body: '{"ok":true,"connected":false}',
          );
        });
        final service = MercadoPagoService(
          transport: transport,
          config: MercadoPagoServiceConfig(
            baseUri: Uri.parse('https://api.example/functions/v1/payments'),
            licenseActivationUri: Uri.parse(
              'https://api.example/functions/v1/license/activate',
            ),
            contextProvider: () => const MercadoPagoClientContext(
              licenseKey: 'SIGNED-LICENSE',
              machineHash: 'WEB-HASH',
              machineCode: 'WEB-DEVICE',
              clientKind: 'web',
            ),
          ),
        );

        await service.fetchConnectionStatus();
        await service.fetchConnectionStatus();

        expect(transport.requests, hasLength(3));
        expect(
          transport.requests.where(
            (request) => request.uri.path.endsWith('/license/activate'),
          ),
          hasLength(1),
        );
        final activation = jsonDecode(transport.requests.first.body!);
        expect(activation['clientKind'], 'web');
        expect(activation['eventName'], 'agendalivre.mercadopago.activate');
      },
    );

    test('uses injected configuration to start the OAuth flow', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(
          statusCode: 200,
          body: '{"ok":true,"authUrl":"https://auth.example/authorize"}',
        ),
      );
      final service = _service(transport);

      final result = await service.startConnect();

      expect(result.ok, isTrue);
      expect(result.authUrl.toString(), 'https://auth.example/authorize');
      final request = transport.requests.single;
      expect(
        request.uri.toString(),
        'https://api.example/functions/v1/payments/mercadopago/connect/start',
      );
      expect(request.headers['Authorization'], 'Bearer user-session-token');
      expect(request.headers['X-Client-Version'], 'test');
      final payload = jsonDecode(request.body!) as Map<String, dynamic>;
      expect(payload['eventName'], 'agendalivre.mercadopago.connect');
      expect(payload['licenseKey'], 'LICENSE-INJECTED');
      expect(payload['machineHash'], 'HASH-INJECTED');
      expect(payload['appVersion'], 'AgendaLivre.Flutter.Test');
    });

    test('parses connection status and terminal records', () async {
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/status')) {
          return const ServiceHttpResponse(
            statusCode: 200,
            body: '''
{"ok":true,"connected":true,"status":"CONNECTED","sellerUserId":"42","selectedTerminalId":"T1"}
''',
          );
        }
        return const ServiceHttpResponse(
          statusCode: 200,
          body: '''
{"ok":true,"terminals":[{"id":"T1","label":"Point balcão","posId":"P1","storeId":"S1","operatingMode":"PDV"}]}
''',
        );
      });
      final service = _service(transport);

      final status = await service.fetchConnectionStatus();
      final terminals = await service.fetchTerminals();

      expect(status.connected, isTrue);
      expect(status.sellerUserId, '42');
      expect(terminals.terminals, hasLength(1));
      expect(terminals.terminals.single.display, 'Point balcão');
    });

    test(
      'serializes a Point charge in cents and parses approval status',
      () async {
        final transport = FakeHttpTransport((request) {
          if (request.uri.path.endsWith('/point/charge')) {
            return const ServiceHttpResponse(
              statusCode: 200,
              body: '''
{"ok":true,"attemptId":"A1","localReference":"REF1","orderId":"O1","status":"created"}
''',
            );
          }
          return const ServiceHttpResponse(
            statusCode: 200,
            body: '''
{"ok":true,"attemptId":"A1","orderId":"O1","paymentId":"P1","status":"approved","paid":true}
''',
          );
        });
        final service = _service(transport);

        final charge = await service.createPointCharge(
          const MercadoPagoPointChargeRequest(
            amountInCents: 10990,
            method: MercadoPagoPointMethod.debit,
            terminalId: 'T1',
            localReference: 'REF1',
            description: '2x Produto',
          ),
        );
        final chargeRequest = transport.requests.first;
        final chargePayload =
            jsonDecode(chargeRequest.body!) as Map<String, dynamic>;
        final status = await service.fetchPointStatus(
          attemptId: charge.attemptId,
        );

        expect(charge.ok, isTrue);
        expect(chargePayload['amount'], '109.90');
        expect(chargePayload['method'], 'DEBITO');
        expect(chargePayload['terminalId'], 'T1');
        expect(chargePayload['items'], hasLength(1));
        expect(status.paid, isTrue);
        expect(status.paymentId, 'P1');
      },
    );

    test('creates a Pix QR charge and checks the Web payment status', () async {
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/web/charge')) {
          return const ServiceHttpResponse(
            statusCode: 200,
            body: '''
{"ok":true,"attemptId":"A2","localReference":"REF2","paymentId":"P2","status":"pending","qrCode":"000201...","qrCodeBase64":"aW1hZ2U=","paymentUrl":"https://pay.example/pix","expiresAt":"2026-07-14T13:00:00Z"}
''',
          );
        }
        return const ServiceHttpResponse(
          statusCode: 200,
          body: '''
{"ok":true,"attemptId":"A2","paymentId":"P2","status":"approved","paid":true}
''',
        );
      });
      final service = _service(transport);

      final charge = await service.createPixCharge(
        const MercadoPagoPixChargeRequest(
          amountInCents: 5500,
          localReference: 'REF2',
          description: 'Manicure | 14/07 10:00',
          payerName: 'Ana Lima',
          payerEmail: 'ana@example.com',
        ),
      );
      final payload =
          jsonDecode(transport.requests.first.body!) as Map<String, dynamic>;
      final status = await service.fetchPixStatus(
        attemptId: charge.attemptId,
        paymentId: charge.paymentId,
      );

      expect(charge.ok, isTrue);
      expect(charge.qrCode, '000201...');
      expect(charge.qrCodeBase64, 'aW1hZ2U=');
      expect(charge.paymentUrl, 'https://pay.example/pix');
      expect(charge.expiresAt, DateTime.parse('2026-07-14T13:00:00Z'));
      expect(payload['amount'], '55.00');
      expect(payload['method'], 'PIX');
      expect(payload['payerName'], 'Ana Lima');
      expect(payload['payerEmail'], 'ana@example.com');
      expect(payload['items'], hasLength(1));
      expect(status.paid, isTrue);
      expect(status.paymentId, 'P2');
      expect(
        transport.requests.last.uri.path,
        endsWith('/mercadopago/web/status'),
      );
    });

    test(
      'cancels a pending Point charge with its remote identifiers',
      () async {
        final transport = FakeHttpTransport(
          (_) => const ServiceHttpResponse(
            statusCode: 200,
            body: '{"ok":true,"message":"cancelled"}',
          ),
        );

        final result = await _service(transport).cancelPointCharge(
          attemptId: 'A1',
          orderId: 'O1',
          localReference: 'REF1',
        );

        expect(result.ok, isTrue);
        final request = transport.requests.single;
        expect(request.uri.path, endsWith('/mercadopago/point/cancel'));
        final payload = jsonDecode(request.body!) as Map<String, dynamic>;
        expect(payload['attemptId'], 'A1');
        expect(payload['orderId'], 'O1');
        expect(payload['localReference'], 'REF1');
      },
    );

    test('releases the selected terminal through the cloud', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(
          statusCode: 200,
          body: '{"ok":true,"message":"released"}',
        ),
      );

      final result = await _service(transport).releaseTerminal();

      expect(result.ok, isTrue);
      final request = transport.requests.single;
      final payload = jsonDecode(request.body!) as Map<String, dynamic>;
      expect(request.uri.path, endsWith('/mercadopago/terminal/select'));
      expect(payload['terminalId'], isEmpty);
    });

    test(
      'keeps structured API errors instead of exposing response bodies',
      () async {
        final transport = FakeHttpTransport(
          (_) => const ServiceHttpResponse(
            statusCode: 401,
            body: '{"ok":false,"message":"Licença inválida"}',
          ),
        );

        final result = await _service(transport).fetchConnectionStatus();

        expect(result.ok, isFalse);
        expect(result.statusCode, 401);
        expect(result.message, 'Licença inválida');
      },
    );

    test('rejects invalid charges before calling the backend', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(statusCode: 200, body: '{}'),
      );

      expect(
        () => _service(transport).createPointCharge(
          const MercadoPagoPointChargeRequest(
            amountInCents: 0,
            method: MercadoPagoPointMethod.credit,
            terminalId: 'T1',
          ),
        ),
        throwsA(
          isA<MercadoPagoException>().having(
            (error) => error.failure,
            'failure',
            MercadoPagoFailure.validation,
          ),
        ),
      );
      expect(transport.requests, isEmpty);
    });

    test(
      'creates deterministic-shape local references without dependencies',
      () {
        final reference = MercadoPagoService.createLocalReference(
          now: DateTime(2026, 7, 14, 12, 34, 56),
          random: Random(1),
        );

        expect(reference, matches(RegExp(r'^AGL-20260714123456-[0-9A-F]{8}$')));
        expect(reference.length, lessThanOrEqualTo(32));
      },
    );
  });
}

MercadoPagoService _service(FakeHttpTransport transport) {
  return MercadoPagoService(
    transport: transport,
    config: MercadoPagoServiceConfig(
      baseUri: Uri.parse('https://api.example/functions/v1/payments'),
      activateClient: false,
      contextProvider: () => const MercadoPagoClientContext(
        licenseKey: 'LICENSE-INJECTED',
        machineHash: 'HASH-INJECTED',
        machineCode: 'DEVICE-INJECTED',
        appVersion: 'AgendaLivre.Flutter.Test',
        profile: <String, Object?>{'businessName': 'Agenda teste'},
      ),
      accessTokenProvider: () => 'user-session-token',
      headersProvider: () => const <String, String>{'X-Client-Version': 'test'},
    ),
  );
}
