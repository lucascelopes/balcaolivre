import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test('exports the selected customer with the store privacy notice', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    final customer = store.customers.first;
    customer
      ..document = '123.456.789-00'
      ..district = 'Centro'
      ..notes = 'Entregar na portaria'
      ..marketingConsent = true
      ..dataConsentAt = DateTime(2026, 7, 26)
      ..dataConsentSource = 'Cadastro no PDV';

    final exported = store.exportCustomerPrivacyData(customer.id);

    expect(exported, isNotNull);
    final envelope = jsonDecode(exported!) as Map<String, dynamic>;
    expect(envelope['schema'], 'balcao-livre-lgpd-customer-export');
    expect(envelope['privacyNotice'], contains(store.businessName));
    final data = envelope['customer'] as Map<String, dynamic>;
    expect(data['document'], '123.456.789-00');
    expect(data['district'], 'Centro');
    expect(data['marketingConsent'], isTrue);
  });

  test('anonymizes every personal and consent field and persists it', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    final customer = store.customers.first;
    customer
      ..document = '123'
      ..district = 'Centro'
      ..notes = 'Observacao'
      ..birthday = DateTime(1990, 1, 1)
      ..marketingConsent = true
      ..dataConsentAt = DateTime.now()
      ..dataConsentSource = 'PDV';

    final removed = await store.anonymizeCustomer(customer.id);

    expect(removed, isTrue);
    expect(customer.name, startsWith('CLIENTE ANONIMIZADO'));
    expect(customer.phone, isEmpty);
    expect(customer.document, isEmpty);
    expect(customer.address, isEmpty);
    expect(customer.district, isEmpty);
    expect(customer.notes, isEmpty);
    expect(customer.birthday, isNull);
    expect(customer.marketingConsent, isFalse);
    expect(customer.dataConsentAt, isNull);
    expect(customer.dataConsentSource, isEmpty);
    expect(customer.privacyRemovalAt, isNotNull);

    final prefs = await SharedPreferences.getInstance();
    final state =
        jsonDecode(prefs.getString('balcao_livre_flutter_state_v1')!)
            as Map<String, dynamic>;
    final saved = (state['customers'] as List<dynamic>).first;
    expect(saved['phone'], '');
    expect(saved['privacyRemovalAt'], isNotNull);
  });
}
