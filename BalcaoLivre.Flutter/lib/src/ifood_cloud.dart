import 'dart:convert';

import 'package:http/http.dart' as http;

const defaultIFoodCloudUrl = String.fromEnvironment(
  'BALCAO_IFOOD_API_URL',
  defaultValue: 'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood',
);

class IFoodCloudException implements Exception {
  const IFoodCloudException(this.message);

  final String message;

  @override
  String toString() => message;
}

class IFoodCloudClient {
  IFoodCloudClient({http.Client? client, String? baseUrl})
    : _client = client ?? http.Client(),
      _ownsClient = client == null,
      baseUrl = (baseUrl ?? defaultIFoodCloudUrl).replaceFirst(
        RegExp(r'/+$'),
        '',
      );

  final http.Client _client;
  final bool _ownsClient;
  final String baseUrl;

  Future<Map<String, dynamic>> startConnection(
    Map<String, dynamic> storeContext,
  ) => _post('/connect/start', storeContext);

  Future<Map<String, dynamic>> finishConnection(
    Map<String, dynamic> storeContext, {
    required String connectionId,
    required String authorizationCode,
  }) => _post('/connect/finish', {
    ...storeContext,
    'connectionId': connectionId,
    'authorizationCode': authorizationCode,
  });

  Future<Map<String, dynamic>> syncOrders(
    Map<String, dynamic> storeContext, {
    required String connectionId,
  }) => _post('/orders/sync', {...storeContext, 'connectionId': connectionId});

  Future<Map<String, dynamic>> sendOrderAction(
    Map<String, dynamic> storeContext, {
    required String connectionId,
    required String orderId,
    required String action,
    String reason = '',
    String deliveredBy = 'MERCHANT',
  }) => _post('/orders/action', {
    ...storeContext,
    'connectionId': connectionId,
    'orderId': orderId,
    'action': action,
    'reason': reason,
    'deliveredBy': deliveredBy,
  });

  Future<Map<String, dynamic>> _post(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = Uri.tryParse('$baseUrl$path');
    if (uri == null) {
      throw const IFoodCloudException('Endereco do iFood invalido.');
    }

    try {
      final response = await _client
          .post(
            uri,
            headers: const {'content-type': 'application/json'},
            body: jsonEncode(payload),
          )
          .timeout(const Duration(seconds: 25));
      final decoded = response.body.trim().isEmpty
          ? <String, dynamic>{}
          : jsonDecode(utf8.decode(response.bodyBytes));
      final data = decoded is Map<String, dynamic>
          ? decoded
          : <String, dynamic>{};
      final ok =
          response.statusCode >= 200 &&
          response.statusCode < 300 &&
          data['ok'] != false;
      if (!ok) {
        throw IFoodCloudException(
          _message(data).isEmpty
              ? 'iFood retornou erro ${response.statusCode}.'
              : _message(data),
        );
      }
      return data;
    } on IFoodCloudException {
      rethrow;
    } on FormatException {
      throw const IFoodCloudException('Resposta invalida do iFood.');
    } catch (_) {
      throw const IFoodCloudException('Nao consegui falar com o iFood agora.');
    }
  }

  String _message(Map<String, dynamic> data) {
    final value = data['message'] ?? data['error'] ?? '';
    if (value is Map) {
      return '${value['message'] ?? value['error'] ?? ''}'.trim();
    }
    return '$value'.trim();
  }

  void dispose() {
    if (_ownsClient) _client.close();
  }
}
