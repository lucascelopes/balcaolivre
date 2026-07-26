import 'dart:convert';

import 'package:agenda_livre/app/android_agenda_root.dart';
import 'package:agenda_livre/domain/models/agenda_data.dart';
import 'package:agenda_livre/services/agenda_account_api.dart';
import 'package:agenda_livre/services/android_device_api.dart';
import 'package:agenda_livre/services/android_secure_store.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  late DateTime now;
  late SharedPreferences preferences;
  late MemoryAndroidSecureStore secureStore;

  setUp(() async {
    now = DateTime.utc(2026, 7, 18, 12);
    SharedPreferences.setMockInitialValues(<String, Object>{});
    preferences = await SharedPreferences.getInstance();
    secureStore = MemoryAndroidSecureStore();
  });

  test(
    'first opening redeems once and stores only device session securely',
    () async {
      final api = FakeAndroidDeviceApi(
        onRedeem: () => _session(now),
        onRefresh: (current) => current,
      );
      final controller = _controller(
        preferences: preferences,
        secureStore: secureStore,
        deviceApi: api,
        now: () => now,
      );

      await controller.initialize();

      expect(controller.stage, AndroidAgendaStage.active);
      expect(controller.agendaController, isNotNull);
      expect(api.redeemCalls, 1);
      expect(api.refreshCalls, 0);
      expect(
        secureStore.values[AgendaAndroidSessionController.secureSessionKey],
        contains('opaque-device-token'),
      );
      expect(
        preferences.getKeys().any(
          (key) =>
              preferences.get(key).toString().contains('opaque-device-token'),
        ),
        isFalse,
      );
      controller.dispose();
    },
  );

  test(
    'restored session opens offline only while server lease is valid',
    () async {
      final valid = _session(now);
      secureStore.values[AgendaAndroidSessionController.secureSessionKey] =
          jsonEncode(valid.toJson());
      final controller = _controller(
        preferences: preferences,
        secureStore: secureStore,
        deviceApi: FakeAndroidDeviceApi(
          onRedeem: () => valid,
          onRefresh: (_) => throw const HttpTransportException('offline'),
        ),
        now: () => now,
      );

      await controller.initialize();

      expect(controller.stage, AndroidAgendaStage.active);
      expect(controller.offline, isTrue);
      expect(controller.agendaController, isNotNull);
      controller.dispose();
    },
  );

  test(
    'expired offline lease never opens agenda without server check',
    () async {
      final expired = _session(
        now,
        entitlement: AndroidEntitlement(
          status: 'active',
          canUse: true,
          checkedAt: now.subtract(const Duration(days: 2)),
          leaseExpiresAt: now.subtract(const Duration(hours: 1)),
        ),
      );
      secureStore.values[AgendaAndroidSessionController.secureSessionKey] =
          jsonEncode(expired.toJson());
      final controller = _controller(
        preferences: preferences,
        secureStore: secureStore,
        deviceApi: FakeAndroidDeviceApi(
          onRedeem: () => expired,
          onRefresh: (_) => throw const HttpTransportException('offline'),
        ),
        now: () => now,
      );

      await controller.initialize();

      expect(controller.stage, AndroidAgendaStage.connectionRequired);
      expect(controller.agendaController, isNull);
      controller.dispose();
    },
  );

  test('past due response replaces agenda with payment gate', () async {
    final current = _session(now);
    secureStore.values[AgendaAndroidSessionController.secureSessionKey] =
        jsonEncode(current.toJson());
    final blocked = AndroidEntitlement(
      status: 'past_due',
      canUse: false,
      checkedAt: now,
      leaseExpiresAt: now,
      paymentUrl: 'https://agenda.example/pagar',
    );
    final controller = _controller(
      preferences: preferences,
      secureStore: secureStore,
      deviceApi: FakeAndroidDeviceApi(
        onRedeem: () => current,
        onRefresh: (_) =>
            throw AndroidEntitlementException(blocked, 'Pagamento pendente'),
      ),
      now: () => now,
    );

    await controller.initialize();

    expect(controller.stage, AndroidAgendaStage.subscriptionRequired);
    expect(controller.agendaController, isNull);
    expect(controller.entitlement?.status, 'past_due');
    controller.dispose();
  });

  testWidgets('active Android agenda keeps personalized branding visible', (
    tester,
  ) async {
    final current = _session(now);
    final controller = _controller(
      preferences: preferences,
      secureStore: secureStore,
      deviceApi: FakeAndroidDeviceApi(
        onRedeem: () => current,
        onRefresh: (value) => value,
      ),
      now: () => now,
    );
    await controller.initialize();

    await tester.pumpWidget(
      AgendaLivreAndroidRoot(session: controller, autoInitialize: false),
    );
    await tester.pump();

    expect(find.byKey(const Key('android-active-brand')), findsOneWidget);
    expect(find.text('Studio Aurora'), findsWidgets);
    controller.dispose();
  });
}

AgendaAndroidSessionController _controller({
  required SharedPreferences preferences,
  required MemoryAndroidSecureStore secureStore,
  required AndroidDeviceSessionApi deviceApi,
  required DateTime Function() now,
}) => AgendaAndroidSessionController(
  preferences: preferences,
  secureStore: secureStore,
  deviceApi: deviceApi,
  now: now,
  config: AndroidBuildConfig(
    apiBase: Uri.parse('https://agenda.example'),
    buildId: 'build-123',
    provisioningToken: 'one-time-build-token',
    appVersion: '1.0.0',
    businessName: 'Studio Aurora',
    logoAsset: 'assets/branding/agenda-livre-mark.png',
    coverAsset: '',
    paymentUrl: 'https://agenda.example/pagar',
    supportUrl: 'https://agenda.example/suporte',
    devMode: false,
  ),
  stateClientFactory: (_, _) => FakeStateClient(),
);

AndroidDeviceSession _session(
  DateTime now, {
  AndroidEntitlement? entitlement,
}) => AndroidDeviceSession(
  accountId: 'account-1',
  deviceId: 'device-1',
  deviceToken: 'opaque-device-token',
  branding: const AndroidBranding(
    businessName: 'Studio Aurora',
    logoUrl: 'https://cdn.example/logo.png',
    coverUrl: 'https://cdn.example/cover.jpg',
  ),
  entitlement:
      entitlement ??
      AndroidEntitlement(
        status: 'trialing',
        canUse: true,
        checkedAt: now,
        leaseExpiresAt: now.add(const Duration(hours: 12)),
        trialEndsAt: now.add(const Duration(days: 7)),
        daysRemaining: 7,
      ),
);

class MemoryAndroidSecureStore implements AndroidSecureStore {
  final Map<String, String> values = <String, String>{};

  @override
  Future<void> delete(String key) async => values.remove(key);

  @override
  Future<String?> read(String key) async => values[key];

  @override
  Future<void> write(String key, String value) async => values[key] = value;
}

class FakeAndroidDeviceApi implements AndroidDeviceSessionApi {
  FakeAndroidDeviceApi({required this.onRedeem, required this.onRefresh});

  final AndroidDeviceSession Function() onRedeem;
  final AndroidDeviceSession Function(AndroidDeviceSession current) onRefresh;
  int redeemCalls = 0;
  int refreshCalls = 0;

  @override
  Future<Uri> createCheckout(
    AndroidDeviceSession current, {
    required String idempotencyKey,
  }) async => Uri.parse('https://agenda.example/checkout');

  @override
  Future<AndroidDeviceSession> redeem({
    required String buildId,
    required String provisioningToken,
    required String deviceId,
    required String appVersion,
    required String fallbackBusinessName,
  }) async {
    redeemCalls++;
    return onRedeem();
  }

  @override
  Future<AndroidDeviceSession> refresh(AndroidDeviceSession current) async {
    refreshCalls++;
    return onRefresh(current);
  }
}

class FakeStateClient implements AgendaAccountStateClient {
  @override
  Future<AgendaRemoteState> fetchState() async => AgendaRemoteState(
    exists: false,
    revision: 0,
    schemaVersion: 1,
    payload: null,
    updatedAt: null,
    trial: const AgendaTrialStatus(active: true, daysRemaining: 7),
  );

  @override
  Future<AgendaRemoteState> saveState({
    required int baseRevision,
    required int schemaVersion,
    required Map<String, dynamic> payload,
    required String deviceId,
  }) async => AgendaRemoteState(
    exists: true,
    revision: baseRevision + 1,
    schemaVersion: schemaVersion,
    payload: AgendaData.fromJson(payload).toJson(),
    updatedAt: DateTime.now().toUtc(),
    trial: const AgendaTrialStatus(active: true, daysRemaining: 7),
  );
}
