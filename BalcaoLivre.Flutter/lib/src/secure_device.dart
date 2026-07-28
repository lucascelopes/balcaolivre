import 'dart:convert';

import 'package:http/http.dart' as http;

const _handoffUrl = String.fromEnvironment(
  'BALCAO_HANDOFF_URL',
  defaultValue: 'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/handoff',
);

class SecureDeviceActivation {
  const SecureDeviceActivation({
    required this.deviceId,
    required this.storeId,
    required this.leaseToken,
    required this.planCode,
    required this.modules,
    required this.expiresAt,
    required this.onboarding,
  });

  final String deviceId;
  final String storeId;
  final String leaseToken;
  final String planCode;
  final List<String> modules;
  final DateTime? expiresAt;
  final Map<String, dynamic> onboarding;

  factory SecureDeviceActivation.fromJson(Map<String, dynamic> json) {
    final device = _map(json['device']);
    final entitlements = _map(json['entitlements']);
    return SecureDeviceActivation(
      deviceId: '${device['id'] ?? ''}'.trim(),
      storeId: '${json['storeId'] ?? ''}'.trim(),
      leaseToken: '${json['leaseToken'] ?? ''}'.trim(),
      planCode: '${entitlements['plan_code'] ?? ''}'.trim(),
      modules: _list(entitlements['modules'])
          .map((item) => '$item'.trim().toUpperCase())
          .where((item) => item.isNotEmpty)
          .toList(),
      expiresAt: DateTime.tryParse('${entitlements['expires_at'] ?? ''}'),
      onboarding: _map(json['onboarding']),
    );
  }
}

class SecureDeviceClient {
  SecureDeviceClient({http.Client? client}) : _client = client ?? http.Client();

  final http.Client _client;

  Future<SecureDeviceActivation> activateMobile({
    required String accessToken,
    required String installationId,
    required String displayName,
    required String appVersion,
  }) async {
    return _request(
      accessToken: accessToken,
      body: {
        'action': 'activate_account_device',
        'deviceKind': 'MOBILE',
        'installationId': installationId,
        'displayName': displayName,
        'platform': 'flutter',
        'appVersion': appVersion,
      },
    );
  }

  Future<SecureDeviceActivation> checkIn({
    required String leaseToken,
    required String appVersion,
  }) {
    return _request(
      body: {
        'action': 'checkin',
        'leaseToken': leaseToken,
        'appVersion': appVersion,
      },
    );
  }

  Future<int> syncDevice({
    required String leaseToken,
    required List<Map<String, dynamic>> events,
    required Map<String, dynamic> snapshot,
  }) async {
    final response = await _post({
      'action': 'sync_device',
      'leaseToken': leaseToken,
      'events': events,
      'snapshot': snapshot,
      'clientUpdatedAt': DateTime.now().toUtc().toIso8601String(),
    });
    return response['accepted'] is num
        ? (response['accepted'] as num).toInt()
        : 0;
  }

  Future<SecureDeviceActivation> _request({
    String accessToken = '',
    required Map<String, dynamic> body,
  }) async {
    final json = await _post(body, accessToken: accessToken);
    return SecureDeviceActivation.fromJson(json);
  }

  Future<Map<String, dynamic>> _post(
    Map<String, dynamic> body, {
    String accessToken = '',
  }) async {
    final response = await _client
        .post(
          Uri.parse(_handoffUrl),
          headers: {
            'Content-Type': 'application/json',
            if (accessToken.isNotEmpty) 'Authorization': 'Bearer $accessToken',
          },
          body: jsonEncode(body),
        )
        .timeout(const Duration(seconds: 20));
    final decoded = jsonDecode(response.body);
    final json = decoded is Map<String, dynamic>
        ? decoded
        : <String, dynamic>{};
    if (response.statusCode < 200 ||
        response.statusCode >= 300 ||
        json['ok'] != true) {
      throw StateError(
        '${json['message'] ?? 'Não foi possível liberar este smartphone.'}',
      );
    }
    return json;
  }
}

Map<String, dynamic> _map(Object? value) =>
    value is Map<String, dynamic> ? value : <String, dynamic>{};

List<dynamic> _list(Object? value) =>
    value is List<dynamic> ? value : const <dynamic>[];
