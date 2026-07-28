import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test('exports and restores a complete checksummed backup', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();
    await store.updateBusinessName('Loja Antes do Backup');
    final originalProducts = store.products.length;

    final backup = await store.createBackupJson(operator: '2', pin: '1234');

    expect(backup, isNotNull);
    final envelope = jsonDecode(backup!) as Map<String, dynamic>;
    expect(envelope['schema'], 'balcao-livre-flutter-backup');
    expect(envelope['version'], 1);
    expect('${envelope['checksum']}', hasLength(64));
    final payload = envelope['payload'] as Map<String, dynamic>;
    final team = payload['teamMembers'] as List<dynamic>;
    expect(jsonEncode(team), isNot(contains('"pin":"1234"')));
    expect(jsonEncode(team), contains(r'PBKDF2$120000$'));

    await store.updateBusinessName('Loja Alterada');
    store.products.removeLast();
    expect(store.products.length, originalProducts - 1);

    final restored = await store.restoreBackupJson(
      backupJson: backup,
      operator: '2',
      pin: '1234',
    );

    expect(restored, isTrue);
    expect(store.businessName, 'Loja Antes do Backup');
    expect(store.products.length, originalProducts);
    expect(store.backupMessage, contains('LUCAS CESAR'));
  });

  test(
    'blocks a backup whose payload no longer matches its checksum',
    () async {
      SharedPreferences.setMockInitialValues({});
      final store = BalcaoStore();
      addTearDown(store.dispose);
      await store.hydrate();
      final backup = await store.createBackupJson(operator: '2', pin: '1234');
      final envelope = jsonDecode(backup!) as Map<String, dynamic>;
      final payload = envelope['payload'] as Map<String, dynamic>;
      payload['businessName'] = 'Backup adulterado';

      final restored = await store.restoreBackupJson(
        backupJson: jsonEncode(envelope),
        operator: '2',
        pin: '1234',
      );

      expect(restored, isFalse);
      expect(store.backupMessage, contains('corrompido'));
    },
  );
}
