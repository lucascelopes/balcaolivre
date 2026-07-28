import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:balcao_livre_flutter/src/ifood_cloud.dart';

void main() {
  test('starts the same centralized iFood connection used by WPF', () async {
    late http.Request request;
    final client = IFoodCloudClient(
      baseUrl: 'https://example.test/ifood/',
      client: MockClient((incoming) async {
        request = incoming;
        return http.Response(
          jsonEncode({
            'ok': true,
            'status': 'connected',
            'connectionId': 'conn-1',
            'merchantName': 'Loja Centro',
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      }),
    );
    addTearDown(client.dispose);

    final response = await client.startConnection({
      'licenseKey': 'BLV-123',
      'machineHash': 'flutter-1',
    });

    expect(request.url.toString(), 'https://example.test/ifood/connect/start');
    expect(request.method, 'POST');
    expect(jsonDecode(request.body)['licenseKey'], 'BLV-123');
    expect(response['connectionId'], 'conn-1');
  });

  test('sends order actions through the cloud gateway', () async {
    late Map<String, dynamic> body;
    final client = IFoodCloudClient(
      baseUrl: 'https://example.test/ifood',
      client: MockClient((request) async {
        body = jsonDecode(request.body) as Map<String, dynamic>;
        return http.Response(
          jsonEncode({'ok': true, 'orderId': 'order-1', 'status': 'CONFIRMED'}),
          200,
        );
      }),
    );
    addTearDown(client.dispose);

    await client.sendOrderAction(
      {'licenseKey': 'BLV-123'},
      connectionId: 'conn-1',
      orderId: 'order-1',
      action: 'confirm',
    );

    expect(body['connectionId'], 'conn-1');
    expect(body['orderId'], 'order-1');
    expect(body['action'], 'confirm');
    expect(body['deliveredBy'], 'MERCHANT');
  });

  test('surfaces the backend message on a refused request', () async {
    final client = IFoodCloudClient(
      baseUrl: 'https://example.test/ifood',
      client: MockClient(
        (_) async => http.Response(
          jsonEncode({'ok': false, 'message': 'Loja sem permissao iFood.'}),
          403,
        ),
      ),
    );
    addTearDown(client.dispose);

    expect(
      () => client.syncOrders({}, connectionId: 'conn-1'),
      throwsA(
        isA<IFoodCloudException>().having(
          (error) => error.message,
          'message',
          'Loja sem permissao iFood.',
        ),
      ),
    );
  });
}
