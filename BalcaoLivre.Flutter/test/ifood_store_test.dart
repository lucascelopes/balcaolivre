import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/ifood_cloud.dart';
import 'package:balcao_livre_flutter/src/models.dart';
import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test(
    'connects and imports real iFood gateway orders without duplicates',
    () async {
      var syncCalls = 0;
      var actionCalls = 0;
      final cloud = IFoodCloudClient(
        baseUrl: 'https://example.test/ifood',
        client: MockClient((request) async {
          if (request.url.path.endsWith('/connect/start')) {
            return http.Response(
              jsonEncode({
                'ok': true,
                'status': 'connected',
                'connectionId': 'conn-1',
                'merchantId': 'merchant-1',
                'merchantName': 'Loja Centro',
                'message': 'iFood conectado.',
              }),
              200,
            );
          }
          if (request.url.path.endsWith('/orders/action')) {
            actionCalls++;
            final body = jsonDecode(request.body) as Map<String, dynamic>;
            expect(body['orderId'], 'order-1');
            expect(body['action'], 'dispatch');
            return http.Response(
              jsonEncode({
                'ok': true,
                'message': 'Pedido iFood despachado.',
                'orderId': 'order-1',
                'status': 'DESPACHADO',
              }),
              200,
            );
          }
          syncCalls++;
          return http.Response(
            jsonEncode({
              'ok': true,
              'message': '1 pedido(s) iFood recebido(s).',
              'syncedAt': '2026-07-26T20:00:00Z',
              'orders': [
                {
                  'orderId': 'order-1',
                  'displayId': '1234',
                  'status': 'CONFIRMED',
                  'createdAt': '2026-07-26T19:55:00Z',
                  'customerName': 'Cliente iFood',
                  'address': 'Rua Central, 10',
                  'district': 'Centro',
                  'paymentMethod': 'ONLINE',
                  'total': 56,
                  'items': [
                    {
                      'productId': '000001',
                      'code': '000001',
                      'name': 'X-BURGER ARTESANAL',
                      'quantity': 2,
                      'unitPrice': 28,
                    },
                  ],
                },
              ],
            }),
            200,
          );
        }),
      );
      SharedPreferences.setMockInitialValues({});
      final store = BalcaoStore(ifoodClient: cloud);
      addTearDown(store.dispose);
      await store.hydrate();
      store.licenseKey = 'BLV-123';

      await store.connectIfood();
      final firstImport = await store.syncIfoodOrders();
      final secondImport = await store.syncIfoodOrders();

      expect(store.ifoodConnected, isTrue);
      expect(store.ifoodMerchantName, 'Loja Centro');
      expect(firstImport, 1);
      expect(secondImport, 0);
      expect(syncCalls, 2);
      final imported = store.orders.singleWhere(
        (order) => order.id == 'ifood-order-1',
      );
      expect(imported.kind, OrderKind.ifood);
      expect(imported.status, OrderStatus.preparing);
      expect(imported.number, '1234');
      expect(imported.items.single.quantity, 2);
      expect(imported.subtotal, 56);
      expect(imported.ifoodRepasse, closeTo(49.28, 0.001));

      await store.updateOrderStatus(imported, OrderStatus.dispatched);
      expect(actionCalls, 1);
      expect(imported.status, OrderStatus.dispatched);
    },
  );
}
