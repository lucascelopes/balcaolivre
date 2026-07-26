import 'dart:convert';

import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/instagram_service.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fake_http_transport.dart';

void main() {
  group('InstagramService', () {
    test('starts the official OAuth flow with the cloud identity', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"authorizationUrl":"https://www.instagram.com/oauth/authorize","expiresAt":"2026-07-23T18:00:00Z"}',
        ),
      );
      final service = _service(transport);

      final result = await service.startOAuth();

      expect(result.ok, isTrue);
      expect(
        result.authorizationUrl.toString(),
        'https://www.instagram.com/oauth/authorize',
      );
      final request = transport.requests.single;
      expect(request.uri.path, endsWith('/instagram/oauth/start'));
      expect(request.headers['Authorization'], 'Bearer USER-TOKEN');
      final payload = jsonDecode(request.body!) as Map<String, dynamic>;
      expect(payload['licenseKey'], 'BLV-TEST');
      expect(payload['machineHash'], 'A1B2C3D4');
      expect(payload['machineCode'], 'A1B2C3D4');
    });

    test('parses status and Direct messages', () async {
      final transport = FakeHttpTransport((request) {
        if (request.uri.path.endsWith('/status')) {
          return const ServiceHttpResponse(
            statusCode: 200,
            body:
                '{"ok":true,"connected":true,"username":"agenda.livre","displayName":"Agenda Livre","instagramUserId":"ig-42","status":"ATIVO"}',
          );
        }
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"messages":[{"id":"m1","instagramScopedId":"u1","senderName":"Nina","senderUsername":"nina","text":"Tem horário hoje?","direction":"entrada","createdAt":"2026-07-23T15:00:00Z","status":"recebida"}]}',
        );
      });
      final service = _service(transport);

      final status = await service.fetchStatus();
      final messages = await service.fetchMessages();

      expect(status.connected, isTrue);
      expect(status.username, 'agenda.livre');
      expect(messages.messages, hasLength(1));
      expect(messages.messages.single.inbound, isTrue);
      expect(messages.messages.single.senderName, 'Nina');
    });

    test('sends a Direct reply to the selected conversation', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"message":"Mensagem enviada.","remoteMessageId":"remote-1"}',
        ),
      );
      final service = _service(transport);

      final result = await service.sendMessage(
        recipientId: 'u1',
        text: 'Sim, às 16h.',
        messageId: 'local-1',
      );

      expect(result.ok, isTrue);
      expect(result.remoteMessageId, 'remote-1');
      final payload =
          jsonDecode(transport.requests.single.body!) as Map<String, dynamic>;
      expect(payload['recipientId'], 'u1');
      expect(payload['text'], 'Sim, às 16h.');
      expect(payload['messageId'], 'local-1');
    });

    test('accepts only trusted Meta authorization hosts', () {
      expect(
        InstagramService.isTrustedAuthorizationUrl(
          Uri.parse('https://www.instagram.com/oauth/authorize'),
        ),
        isTrue,
      );
      expect(
        InstagramService.isTrustedAuthorizationUrl(
          Uri.parse('https://business.facebook.com/dialog/oauth'),
        ),
        isTrue,
      );
      expect(
        InstagramService.isTrustedAuthorizationUrl(
          Uri.parse('https://instagram.example.com/oauth'),
        ),
        isFalse,
      );
      expect(
        InstagramService.isTrustedAuthorizationUrl(
          Uri.parse('http://www.instagram.com/oauth'),
        ),
        isFalse,
      );
    });
  });
}

InstagramService _service(FakeHttpTransport transport) {
  return InstagramService(
    transport: transport,
    config: InstagramServiceConfig(
      baseUri: Uri.parse('https://api.example/functions/v1/instagram'),
      contextProvider: () => const InstagramClientContext(
        licenseKey: 'BLV-TEST',
        machineHash: 'A1B2C3D4',
        machineCode: 'A1B2C3D4',
        appVersion: 'AgendaLivre.Test',
      ),
      accessTokenProvider: () => 'USER-TOKEN',
    ),
  );
}
