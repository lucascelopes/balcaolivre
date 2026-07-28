import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/models.dart';
import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test('closes cash with counted drawer and WPF payment totals', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.orders.clear();
    store.movements
      ..clear()
      ..addAll([
        _movement('ABERTURA', 100, 'Caixa aberto'),
        _movement('VENDA', 50, 'M1 - Dinheiro'),
        _movement('VENDA', 20, 'M2 - Pix'),
        _movement('VENDA', 30, 'M3 - Credito'),
        _movement('VENDA', 10, 'M4 - Debito'),
        _movement('SANGRIA', -5, 'Retirada'),
      ]);

    expect(store.expectedCash, 145);
    expect(store.todayPaymentTotals.pix, 20);
    expect(store.todayPaymentTotals.credit, 30);
    expect(store.todayPaymentTotals.debit, 10);
    expect(store.todayPaymentTotals.other, 50);

    final closed = await store.closeCashProfessionally(
      countedCash: 145,
      notes: '',
      operator: '2',
      pin: '1234',
    );

    expect(closed, isTrue);
    expect(store.cashOpen, isFalse);
    expect(store.cashClosings, hasLength(1));
    expect(store.cashClosings.single.difference, 0);
    expect(store.cashClosings.single.operator, 'LUCAS CESAR');

    final prefs = await SharedPreferences.getInstance();
    final state =
        jsonDecode(prefs.getString('balcao_livre_flutter_state_v1')!)
            as Map<String, dynamic>;
    expect(state['cashClosings'], hasLength(1));
  });

  test('requires a note when counted cash differs', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.orders.clear();
    store.movements
      ..clear()
      ..add(_movement('ABERTURA', 100, 'Caixa aberto'));

    final closed = await store.closeCashProfessionally(
      countedCash: 90,
      notes: '',
      operator: '2',
      pin: '1234',
    );

    expect(closed, isFalse);
    expect(store.cashOpen, isTrue);
    expect(store.securityMessage, contains('Explique a diferenca'));
  });

  test('blocks closing for a role without cash permission', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    store.orders.clear();
    store.movements.clear();

    final closed = await store.closeCashProfessionally(
      countedCash: 0,
      notes: '',
      operator: '3',
      pin: '3',
    );

    expect(closed, isFalse);
    expect(store.cashOpen, isTrue);
    expect(store.cashClosings, isEmpty);
  });
}

CashMovement _movement(String type, double amount, String note) => CashMovement(
  id: '$type-$amount-$note',
  type: type,
  amount: amount,
  note: note,
  createdAt: DateTime.now(),
);
