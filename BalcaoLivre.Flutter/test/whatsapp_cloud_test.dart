import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:balcao_livre_flutter/src/whatsapp_cloud.dart';

void main() {
  test('starts the same centralized WhatsApp activation used by WPF', () async {
    late http.Request request;
    final client = WhatsAppCloudClient(
      baseUrl: 'https://example.test/whatsapp/',
      client: MockClient((incoming) async {
        request = incoming;
        return http.Response(
          jsonEncode({
            'ok': false,
            'pending': true,
            'message': 'Abra o QR Code.',
            'storePhone': '5533999999999',
            'onboardingUrl': 'https://example.test/onboarding',
          }),
          200,
        );
      }),
    );
    addTearDown(client.dispose);

    final response = await client.activate({
      'licenseKey': 'BLV-123',
      'machineHash': 'flutter-1',
    }, storePhone: '5533999999999');

    expect(request.url.toString(), 'https://example.test/whatsapp/activate');
    expect(jsonDecode(request.body)['licenseKey'], 'BLV-123');
    expect(jsonDecode(request.body)['storePhone'], '5533999999999');
    expect(response['pending'], isTrue);
    expect(response['onboardingUrl'], 'https://example.test/onboarding');
  });

  test('keeps onboarding details returned with HTTP 428', () async {
    final client = WhatsAppCloudClient(
      baseUrl: 'https://example.test/whatsapp',
      client: MockClient(
        (_) async => http.Response(
          jsonEncode({
            'ok': false,
            'pending': true,
            'message': 'Conecte pelo QR Code.',
            'onboardingUrl': 'https://example.test/qr',
          }),
          428,
        ),
      ),
    );
    addTearDown(client.dispose);

    final response = await client.send(
      {'licenseKey': 'BLV-123'},
      storePhone: '5533999999999',
      customerPhone: '5533888888888',
      message: 'Pedido pronto.',
    );

    expect(response['pending'], isTrue);
    expect(response['onboardingUrl'], 'https://example.test/qr');
  });

  test('surfaces a refused WhatsApp request', () async {
    final client = WhatsAppCloudClient(
      baseUrl: 'https://example.test/whatsapp',
      client: MockClient(
        (_) async => http.Response(
          jsonEncode({'ok': false, 'message': 'Licenca bloqueada.'}),
          403,
        ),
      ),
    );
    addTearDown(client.dispose);

    expect(
      () => client.status({'licenseKey': 'BLV-123'}),
      throwsA(
        isA<WhatsAppCloudException>().having(
          (error) => error.message,
          'message',
          'Licenca bloqueada.',
        ),
      ),
    );
  });
}
