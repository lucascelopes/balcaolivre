import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:balcao_livre_flutter/src/store.dart';

void main() {
  test('team member is persisted with the cross-client user fields', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    final saved = await store.saveTeamMember(
      number: '4',
      name: '  Maria Souza ',
      role: 'garcom',
    );

    expect(saved, isTrue);
    expect(store.teamMembers.first.number, '4');
    expect(store.teamMembers.first.name, 'MARIA SOUZA');
    expect(store.teamMembers.first.role, 'GARCOM');

    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString('balcao_livre_flutter_state_v1');
    expect(raw, isNotNull);
    final state = jsonDecode(raw!) as Map<String, dynamic>;
    final team = state['teamMembers'] as List<dynamic>;
    expect(
      team.cast<Map<String, dynamic>>().any(
        (member) =>
            member['number'] == '4' &&
            member['name'] == 'MARIA SOUZA' &&
            member['role'] == 'GARCOM',
      ),
      isTrue,
    );
    expect(store.teamMembers.first.toJson()['employeeNumber'], '4');
  });

  test('team member number cannot be duplicated', () async {
    SharedPreferences.setMockInitialValues({});
    final store = BalcaoStore();
    addTearDown(store.dispose);
    await store.hydrate();

    final saved = await store.saveTeamMember(
      number: '1',
      name: 'Outro operador',
      role: 'CAIXA',
    );

    expect(saved, isFalse);
    expect(
      store.teamMembers.where((member) => member.number == '1'),
      hasLength(1),
    );
  });
}
