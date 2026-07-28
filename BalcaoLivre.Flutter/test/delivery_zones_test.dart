import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/models.dart';
import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test(
    'persists delivery radius fees and suggests the smallest active zone',
    () async {
      SharedPreferences.setMockInitialValues({});
      final store = BalcaoStore();
      addTearDown(store.dispose);
      await store.hydrate();

      await store.saveDeliveryZone(
        radiusKm: '3,0',
        fee: '8,00',
        minimumOrder: '30,00',
        active: true,
        operator: '2',
        pin: '1234',
      );
      await store.saveDeliveryZone(
        radiusKm: '1,0',
        fee: '5,00',
        minimumOrder: '0',
        active: true,
        operator: '2',
        pin: '1234',
      );

      expect(store.deliveryZones, hasLength(2));
      expect(store.suggestedDeliveryZone!.radiusKm, 1);
      expect(store.suggestedDeliveryZone!.fee, 5);

      final prefs = await SharedPreferences.getInstance();
      final state =
          jsonDecode(prefs.getString('balcao_livre_flutter_state_v1')!)
              as Map<String, dynamic>;
      expect(state['deliveryZones'], hasLength(2));
    },
  );

  test(
    'new delivery keeps customer, zone fee, courier and print settings',
    () async {
      SharedPreferences.setMockInitialValues({});
      final store = BalcaoStore();
      addTearDown(store.dispose);
      await store.hydrate();

      await store.openOrder(
        OrderKind.delivery,
        customer: 'Maria',
        customerPhone: '5533999999999',
        address: 'Rua A, 10',
        district: 'Centro',
        notes: 'Portao azul',
        deliveryFee: '7,50',
        courier: 'Motoboy loja',
        autoPrint: false,
      );

      final order = store.selectedOrder!;
      expect(order.kind, OrderKind.delivery);
      expect(order.customerPhone, '5533999999999');
      expect(order.district, 'Centro');
      expect(order.notes, 'Portao azul');
      expect(order.deliveryFee, 7.5);
      expect(order.courier, 'Motoboy loja');
      expect(order.autoPrint, isFalse);
    },
  );

  test('blocks delivery-zone changes for an unauthorized role', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    final saved = await store.saveDeliveryZone(
      radiusKm: '1',
      fee: '5',
      minimumOrder: '0',
      active: true,
      operator: '3',
      pin: '3',
    );

    expect(saved, isFalse);
    expect(store.deliveryZones, isEmpty);
  });
}
