import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/store.dart';
import 'package:balcao_livre_flutter/src/whatsapp_cloud.dart';

void main() {
  test(
    'connects, sends and disconnects through the real cloud gateway',
    () async {
      var statusCalls = 0;
      var sendCalls = 0;
      var disconnectCalls = 0;
      final cloud = WhatsAppCloudClient(
        baseUrl: 'https://example.test/whatsapp',
        client: MockClient((request) async {
          final body = jsonDecode(request.body) as Map<String, dynamic>;
          expect(body['licenseKey'], 'BLV-123');
          expect('${body['machineHash']}', startsWith('flutter-'));
          if (request.url.path.endsWith('/activate')) {
            expect(body['storePhone'], '5533999999999');
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
          }
          if (request.url.path.endsWith('/status')) {
            statusCalls++;
            return http.Response(
              jsonEncode({
                'ok': true,
                'pending': false,
                'message': 'WhatsApp conectado pela Evolution.',
                'storePhone': '5533999999999',
              }),
              200,
            );
          }
          if (request.url.path.endsWith('/send')) {
            sendCalls++;
            expect(body['customerPhone'], '5533888888888');
            expect(body['message'], 'Pedido pronto.');
            return http.Response(
              jsonEncode({
                'ok': true,
                'pending': false,
                'message': 'WhatsApp enviado.',
                'storePhone': '5533999999999',
              }),
              200,
            );
          }
          disconnectCalls++;
          return http.Response(
            jsonEncode({
              'ok': true,
              'pending': false,
              'message': 'WhatsApp desconectado.',
              'storePhone': '',
            }),
            200,
          );
        }),
      );
      SharedPreferences.setMockInitialValues({});
      final store = BalcaoStore(whatsappClient: cloud);
      addTearDown(store.dispose);
      await store.hydrate();
      store.licenseKey = 'BLV-123';

      await store.connectWhatsApp('5533999999999');

      expect(store.whatsappConnected, isFalse);
      expect(store.whatsappConnectionStatus, 'PENDING');
      expect(store.whatsappOnboardingUrl, 'https://example.test/onboarding');

      await store.refreshWhatsAppQr();

      expect(statusCalls, 1);
      expect(store.whatsappConnected, isTrue);
      expect(store.whatsappConnectionStatus, 'CONNECTED');
      expect(store.whatsappOnboardingUrl, isEmpty);

      final sent = await store.sendWhatsAppMessage(
        customerPhone: '5533888888888',
        message: 'Pedido pronto.',
      );

      expect(sent, isTrue);
      expect(sendCalls, 1);
      expect(store.whatsappMessage, 'WhatsApp enviado.');

      await store.disconnectWhatsApp();

      expect(disconnectCalls, 1);
      expect(store.whatsappConnected, isFalse);
      expect(store.whatsappConnectionStatus, 'DISCONNECTED');
      expect(store.whatsappNumber, isEmpty);
    },
  );
}
