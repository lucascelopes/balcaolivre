import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('estado legado preserva acesso durante os sete dias de teste', () {
    final state = AgendaRemoteState.fromJson(<String, dynamic>{
      'exists': false,
      'revision': 0,
      'schemaVersion': 1,
      'payload': null,
      'updatedAt': '2026-07-30T12:00:00.000Z',
      'trial': <String, Object?>{
        'startedAt': '2026-07-29T12:00:00.000Z',
        'endsAt': '2026-08-05T12:00:00.000Z',
        'active': true,
        'daysRemaining': 6,
      },
    });

    expect(state.trial.active, isTrue);
    expect(state.trial.daysRemaining, 6);
    expect(state.entitlement.status, 'trialing');
    expect(state.entitlement.canUse, isTrue);
    expect(state.entitlement.daysRemaining, 6);
    expect(
      state.entitlement.trialEndsAt,
      DateTime.parse('2026-08-05T12:00:00.000Z'),
    );
  });

  test('estado legado bloqueia somente depois que o teste terminou', () {
    final state = AgendaRemoteState.fromJson(<String, dynamic>{
      'exists': true,
      'revision': 1,
      'schemaVersion': 1,
      'payload': <String, Object?>{},
      'updatedAt': '2026-07-30T12:00:00.000Z',
      'trial': <String, Object?>{
        'startedAt': '2026-07-20T12:00:00.000Z',
        'endsAt': '2026-07-27T12:00:00.000Z',
        'active': false,
        'daysRemaining': 0,
      },
    });

    expect(state.entitlement.status, 'expired');
    expect(state.entitlement.canUse, isFalse);
  });

  test('entitlement moderno continua sendo a fonte principal', () {
    final state = AgendaRemoteState.fromJson(<String, dynamic>{
      'exists': true,
      'revision': 2,
      'schemaVersion': 1,
      'payload': <String, Object?>{},
      'updatedAt': '2026-07-30T12:00:00.000Z',
      'trial': <String, Object?>{'active': false, 'daysRemaining': 0},
      'entitlement': <String, Object?>{
        'status': 'active',
        'canUse': true,
        'daysRemaining': 0,
        'currentPeriodEndsAt': '2026-08-30T12:00:00.000Z',
      },
    });

    expect(state.entitlement.status, 'active');
    expect(state.entitlement.canUse, isTrue);
  });
}
