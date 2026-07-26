import 'dart:convert';

import 'package:http/http.dart' as http;

const defaultWhatsAppCloudUrl = String.fromEnvironment(
  'BALCAO_WHATSAPP_API_URL',
  defaultValue:
      'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp',
);

class WhatsAppCloudException implements Exception {
  const WhatsAppCloudException(this.message);

  final String message;

  @override
  String toString() => message;
}

class WhatsAppCloudClient {
  WhatsAppCloudClient({http.Client? client, String? baseUrl})
    : _client = client ?? http.Client(),
      _ownsClient = client == null,
      baseUrl = (baseUrl ?? defaultWhatsAppCloudUrl).replaceFirst(
        RegExp(r'/+$'),
        '',
      );

  final http.Client _client;
  final bool _ownsClient;
  final String baseUrl;

  Future<Map<String, dynamic>> activate(
    Map<String, dynamic> storeContext, {
    required String storePhone,
  }) => _post('/activate', {...storeContext, 'storePhone': storePhone});

  Future<Map<String, dynamic>> status(
    Map<String, dynamic> storeContext, {
    String storePhone = '',
  }) => _post('/status', {...storeContext, 'storePhone': storePhone});

  Future<Map<String, dynamic>> send(
    Map<String, dynamic> storeContext, {
    required String storePhone,
    required String customerPhone,
    required String message,
    String messageId = '',
    String customerName = '',
    String boardKind = '',
    String boardNumber = '',
    double total = 0,
  }) => _post('/send', {
    ...storeContext,
    'storePhone': storePhone,
    'customerPhone': customerPhone,
    'customerName': customerName,
    'message': message,
    'messageId': messageId,
    'boardKind': boardKind,
    'boardNumber': boardNumber,
    'total': total,
  });

  Future<Map<String, dynamic>> disconnect(Map<String, dynamic> storeContext) =>
      _post('/disconnect', storeContext);

  Future<Map<String, dynamic>> _post(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = Uri.tryParse('$baseUrl$path');
    if (uri == null) {
      throw const WhatsAppCloudException('Endereco do WhatsApp invalido.');
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
      final success =
          response.statusCode >= 200 &&
          response.statusCode < 300 &&
          data['ok'] != false;
      final actionRequired = data['pending'] == true;
      if (!success && !actionRequired) {
        throw WhatsAppCloudException(
          _message(data).isEmpty
              ? 'WhatsApp retornou erro ${response.statusCode}.'
              : _message(data),
        );
      }
      return data;
    } on WhatsAppCloudException {
      rethrow;
    } on FormatException {
      throw const WhatsAppCloudException('Resposta invalida do WhatsApp.');
    } catch (_) {
      throw const WhatsAppCloudException(
        'Nao consegui falar com o WhatsApp agora.',
      );
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
