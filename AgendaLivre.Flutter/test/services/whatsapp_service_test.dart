import 'package:agenda_livre/services/whatsapp_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('WhatsAppService', () {
    test('normalizes a Brazilian local number', () {
      expect(
        WhatsAppService.normalizePhone('(11) 98888-7777'),
        '5511988887777',
      );
      expect(
        WhatsAppService.normalizePhone('+55 11 98888-7777'),
        '5511988887777',
      );
    });

    test('builds an HTTPS wa.me URI with an encoded message', () {
      const service = WhatsAppService();

      final uri = service.buildWaMeUri(
        phone: '(11) 98888-7777',
        message: 'Olá! Horário às 14h?',
      );

      expect(uri.scheme, 'https');
      expect(uri.host, 'wa.me');
      expect(uri.path, '/5511988887777');
      expect(uri.queryParameters['text'], 'Olá! Horário às 14h?');
    });

    test('returns a wa.me fallback when no proxy is configured', () async {
      const service = WhatsAppService();

      final result = await service.sendText(
        phone: '11988887777',
        message: 'Confirma seu horário?',
      );

      expect(result.sent, isFalse);
      expect(result.proxyAttempted, isFalse);
      expect(result.fallbackUri.host, 'wa.me');
    });

    test('sends normalized data through an injected secure proxy', () async {
      final proxy = _FakeWhatsAppProxy();
      final service = WhatsAppService(proxy: proxy);

      final result = await service.sendText(
        phone: '(11) 98888-7777',
        message: '  Mensagem segura  ',
      );

      expect(result.sent, isTrue);
      expect(result.proxyAttempted, isTrue);
      expect(result.messageId, 'message-1');
      expect(proxy.lastPhone, '5511988887777');
      expect(proxy.lastMessage, 'Mensagem segura');
    });

    test('rejects an invalid phone', () {
      expect(
        () => WhatsAppService.normalizePhone('123'),
        throwsA(isA<WhatsAppValidationException>()),
      );
    });
  });
}

class _FakeWhatsAppProxy implements WhatsAppEvolutionProxy {
  String lastPhone = '';
  String lastMessage = '';

  @override
  Future<void> disconnect() async {}

  @override
  Future<WhatsAppProxyConnection> getConnection() async {
    return const WhatsAppProxyConnection(
      state: WhatsAppConnectionState.connected,
    );
  }

  @override
  Future<WhatsAppProxyQrCode> requestQrCode() async {
    return const WhatsAppProxyQrCode(base64: 'qr');
  }

  @override
  Future<WhatsAppProxySendResult> sendText({
    required String phone,
    required String message,
  }) async {
    lastPhone = phone;
    lastMessage = message;
    return const WhatsAppProxySendResult(
      accepted: true,
      messageId: 'message-1',
      status: 'sent',
    );
  }
}
