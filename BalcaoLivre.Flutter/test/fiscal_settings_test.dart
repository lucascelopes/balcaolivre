import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test('persists the same separated Fiscal and TEF settings as WPF', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    final saved = await store.saveFiscalSettings(
      enabled: true,
      fiscal: 'NFC-E',
      tef: 'STONE',
      merchantCode: 'LOJA-123',
      cscId: 'CSC-SEGREDO',
      environment: 'PRODUCAO',
      requireBeforeReceipt: true,
      operator: '2',
      pin: '1234',
    );

    expect(saved, isTrue);
    expect(store.fiscalEnabled, isTrue);
    expect(store.fiscalProvider, 'NFC-E');
    expect(store.tefProvider, 'STONE');
    expect(store.fiscalEnvironment, 'PRODUCAO');
    expect(store.requireFiscalBeforeReceipt, isTrue);

    final prefs = await SharedPreferences.getInstance();
    final state =
        jsonDecode(prefs.getString('balcao_livre_flutter_state_v1')!)
            as Map<String, dynamic>;
    expect(state['fiscalMerchantCode'], 'LOJA-123');
    expect(state['fiscalCscId'], 'CSC-SEGREDO');
  });

  test('blocks Fiscal and TEF settings for an unauthorized role', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    final saved = await store.saveFiscalSettings(
      enabled: true,
      fiscal: 'SAT',
      tef: 'REDE',
      merchantCode: '',
      cscId: '',
      environment: 'HOMOLOGACAO',
      requireBeforeReceipt: false,
      operator: '3',
      pin: '3',
    );

    expect(saved, isFalse);
    expect(store.fiscalEnabled, isFalse);
    expect(store.fiscalMessage, contains('permissao'));
  });
}
