import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/models.dart';
import 'package:balcao_livre_flutter/src/staff_security.dart';
import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test('verifies the PBKDF2 SHA-256 format shared with WPF', () {
    const wpfCompatible =
        r'PBKDF2$120000$EBESExQVFhcYGRobHB0eHw==$zV3rw3sx0cCLGcKV2G7yh8n34lgL/N96TwUnSfDAjyE=';

    expect(StaffSecurity.verifyPin(wpfCompatible, '1234'), isTrue);
    expect(StaffSecurity.verifyPin(wpfCompatible, '9999'), isFalse);

    final generated = StaffSecurity.hashPin(
      '4567',
      salt: List<int>.generate(16, (index) => index),
    );
    expect(generated, startsWith(r'PBKDF2$120000$'));
    expect(StaffSecurity.verifyPin(generated, '4567'), isTrue);
  });

  test('normalizes the same default role permissions as WPF', () {
    final manager = TeamMember(
      id: 'manager',
      number: '2',
      name: 'GERENTE',
      role: 'GERENTE',
    )..normalizeRolePermissions();
    final waiter = TeamMember(
      id: 'waiter',
      number: '3',
      name: 'GARCOM',
      role: 'GARCOM',
    )..normalizeRolePermissions();

    expect(manager.allows(StaffPermission.discount), isTrue);
    expect(manager.allows(StaffPermission.backup), isTrue);
    expect(waiter.allows(StaffPermission.transfer), isTrue);
    expect(waiter.allows(StaffPermission.discount), isFalse);
  });

  test('applies a discount only after an authorized password', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    await store.addProduct(store.products.first);
    final before = store.selectedOrder!.subtotal;

    final refused = await store.applyDiscount(
      amount: '5,00',
      reason: 'Cortesia',
      operator: '3',
      pin: '3',
    );
    expect(refused, isFalse);
    expect(store.selectedOrder!.subtotal, before);

    final applied = await store.applyDiscount(
      amount: '5,00',
      reason: 'Cortesia',
      operator: '2',
      pin: '1234',
    );

    expect(applied, isTrue);
    expect(store.selectedOrder!.items.last.code, 'DESC');
    expect(store.selectedOrder!.items.last.name, 'CORTESIA');
    expect(store.selectedOrder!.subtotal, closeTo(before - 5, 0.001));
    expect(store.securityMessage, contains('LUCAS CESAR'));
  });
}
