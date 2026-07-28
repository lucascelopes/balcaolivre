import 'dart:async';
import 'dart:convert';

import 'default_http_transport.dart';
import 'http_transport.dart';

typedef InstagramContextProvider = FutureOr<InstagramClientContext> Function();
typedef InstagramAccessTokenProvider = FutureOr<String?> Function();
typedef InstagramHeadersProvider = FutureOr<Map<String, String>> Function();

class InstagramServiceConfig {
  InstagramServiceConfig({
    required this.contextProvider,
    Uri? baseUri,
    this.accessTokenProvider,
    this.headersProvider,
    this.timeout = const Duration(seconds: 16),
  }) : baseUri = baseUri ?? Uri.parse(defaultBaseUrl) {
    if (!this.baseUri.hasScheme ||
        (this.baseUri.scheme != 'https' && this.baseUri.scheme != 'http')) {
      throw const InstagramException(
        InstagramFailure.invalidConfiguration,
        'A URL do serviço Instagram é inválida.',
      );
    }
  }

  static const String defaultBaseUrl =
      'https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/instagram';

  final Uri baseUri;
  final InstagramContextProvider contextProvider;
  final InstagramAccessTokenProvider? accessTokenProvider;
  final InstagramHeadersProvider? headersProvider;
  final Duration timeout;
}

class InstagramService {
  InstagramService({required this.config, HttpTransport? transport})
    : _transport = transport ?? createDefaultHttpTransport();

  final InstagramServiceConfig config;
  final HttpTransport _transport;

  Future<InstagramOAuthResult> startOAuth() async {
    final response = await _post('/oauth/start');
    return InstagramOAuthResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<InstagramStatusResult> fetchStatus() async {
    final response = await _post('/status');
    return InstagramStatusResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<InstagramActionResult> disconnect() async {
    final response = await _post('/disconnect');
    return InstagramActionResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<InstagramMessagesResult> fetchMessages({
    int limit = 100,
    DateTime? since,
    String instagramScopedUserId = '',
  }) async {
    final response = await _post(
      '/messages',
      payload: <String, Object?>{
        'limit': limit.clamp(1, 200),
        if (since != null) 'since': since.toUtc().toIso8601String(),
        if (instagramScopedUserId.trim().isNotEmpty)
          'instagramScopedUserId': instagramScopedUserId.trim(),
      },
    );
    return InstagramMessagesResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<InstagramSendResult> sendMessage({
    required String recipientId,
    required String text,
    String messageId = '',
  }) async {
    final cleanRecipient = recipientId.trim();
    final cleanText = text.trim();
    if (cleanRecipient.isEmpty) {
      throw const InstagramException(
        InstagramFailure.validation,
        'Selecione uma conversa recebida do Instagram.',
      );
    }
    if (cleanText.isEmpty) {
      throw const InstagramException(
        InstagramFailure.validation,
        'Digite a resposta antes de enviar.',
      );
    }
    final response = await _post(
      '/messages/send',
      payload: <String, Object?>{
        'recipientId': cleanRecipient,
        'text': cleanText,
        if (messageId.trim().isNotEmpty) 'messageId': messageId.trim(),
      },
    );
    return InstagramSendResult.fromJson(
      response.json,
      statusCode: response.statusCode,
    );
  }

  Future<_InstagramHttpResponse> _post(
    String path, {
    Map<String, Object?> payload = const <String, Object?>{},
  }) async {
    final InstagramClientContext context;
    try {
      context = await config.contextProvider();
      context.validate();
    } on InstagramException {
      rethrow;
    } on Object catch (error) {
      throw InstagramException(
        InstagramFailure.invalidConfiguration,
        'Não foi possível identificar esta conta para o Instagram.',
        cause: error,
      );
    }

    final headers = <String, String>{
      'Accept': 'application/json',
      'Content-Type': 'application/json; charset=utf-8',
    };
    try {
      final customHeaders = await config.headersProvider?.call();
      if (customHeaders != null) headers.addAll(customHeaders);
      final accessToken = (await config.accessTokenProvider?.call())?.trim();
      if (accessToken != null && accessToken.isNotEmpty) {
        headers['Authorization'] = 'Bearer $accessToken';
      }
    } on Object catch (error) {
      throw InstagramException(
        InstagramFailure.invalidConfiguration,
        'Não foi possível preparar a autorização do Instagram.',
        cause: error,
      );
    }

    final ServiceHttpResponse response;
    final uri = _endpoint(path);
    try {
      response = await _transport.send(
        ServiceHttpRequest(
          method: 'POST',
          uri: uri,
          headers: headers,
          body: jsonEncode(<String, Object?>{...context.toJson(), ...payload}),
          timeout: config.timeout,
        ),
      );
    } on Object catch (error) {
      throw InstagramException(
        InstagramFailure.network,
        'Não foi possível acessar o serviço do Instagram.',
        cause: error,
      );
    }

    try {
      final decoded = jsonDecode(response.body);
      if (decoded is! Map) {
        throw const FormatException('A resposta não é um objeto JSON.');
      }
      return _InstagramHttpResponse(
        statusCode: response.statusCode,
        json: decoded.map((key, value) => MapEntry(key.toString(), value)),
      );
    } on Object catch (error) {
      throw InstagramException(
        InstagramFailure.invalidResponse,
        'O serviço do Instagram retornou uma resposta inválida.',
        statusCode: response.statusCode,
        cause: error,
      );
    }
  }

  Uri _endpoint(String path) {
    final root = config.baseUri.toString().replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$root/${path.replaceFirst(RegExp(r'^/+'), '')}');
  }

  static bool isTrustedAuthorizationUrl(Uri uri) {
    if (uri.scheme != 'https') return false;
    final host = uri.host.toLowerCase();
    return host == 'instagram.com' ||
        host == 'www.instagram.com' ||
        host == 'facebook.com' ||
        host == 'www.facebook.com' ||
        host.endsWith('.facebook.com');
  }
}

class InstagramClientContext {
  const InstagramClientContext({
    required this.licenseKey,
    required this.machineHash,
    this.machineCode = '',
    this.appVersion = 'AgendaLivre.Flutter',
    this.profile = const <String, Object?>{},
  });

  final String licenseKey;
  final String machineHash;
  final String machineCode;
  final String appVersion;
  final Map<String, Object?> profile;

  void validate() {
    if (licenseKey.trim().isEmpty || machineHash.trim().length < 8) {
      throw const InstagramException(
        InstagramFailure.invalidConfiguration,
        'A identificação de licença e dispositivo é obrigatória.',
      );
    }
  }

  Map<String, Object?> toJson() => <String, Object?>{
    'licenseKey': licenseKey.trim(),
    'machineHash': machineHash.trim(),
    'machineCode': machineCode.trim(),
    'appVersion': appVersion.trim(),
    'profile': Map<String, Object?>.from(profile),
  };
}

class InstagramActionResult {
  const InstagramActionResult({
    required this.ok,
    this.message = '',
    this.statusCode = 0,
  });

  factory InstagramActionResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return InstagramActionResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
    );
  }

  final bool ok;
  final String message;
  final int statusCode;
}

class InstagramOAuthResult extends InstagramActionResult {
  const InstagramOAuthResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.authorizationUrl,
    this.expiresAt,
  });

  factory InstagramOAuthResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return InstagramOAuthResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      authorizationUrl: _uri(json['authorizationUrl'] ?? json['url']),
      expiresAt: _date(json['expiresAt']),
    );
  }

  final Uri? authorizationUrl;
  final DateTime? expiresAt;
}

class InstagramStatusResult extends InstagramActionResult {
  const InstagramStatusResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.connected = false,
    this.username = '',
    this.displayName = '',
    this.instagramUserId = '',
    this.status = '',
    this.tokenExpiresAt,
  });

  factory InstagramStatusResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return InstagramStatusResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      connected: _bool(json['connected']),
      username: _normalizeUsername(json['username']),
      displayName: _string(json['displayName'] ?? json['name']),
      instagramUserId: _string(
        json['instagramUserId'] ?? json['accountId'] ?? json['id'],
      ),
      status: _string(json['status'] ?? json['state']),
      tokenExpiresAt: _date(json['tokenExpiresAt']),
    );
  }

  final bool connected;
  final String username;
  final String displayName;
  final String instagramUserId;
  final String status;
  final DateTime? tokenExpiresAt;
}

class InstagramMessage {
  const InstagramMessage({
    required this.id,
    required this.instagramScopedId,
    required this.text,
    required this.direction,
    required this.createdAt,
    this.senderName = '',
    this.senderUsername = '',
    this.status = '',
  });

  factory InstagramMessage.fromJson(Map<String, Object?> json) {
    return InstagramMessage(
      id: _string(json['id'] ?? json['messageId'] ?? json['mid']),
      instagramScopedId: _string(
        json['instagramScopedId'] ??
            json['instagramScopedUserId'] ??
            json['senderId'] ??
            json['recipientId'],
      ),
      senderName: _string(json['senderName'] ?? json['name']),
      senderUsername: _normalizeUsername(
        json['senderUsername'] ?? json['username'],
      ),
      text: _string(json['text'] ?? json['message'] ?? json['body']),
      direction: _string(json['direction']).toLowerCase(),
      createdAt:
          _date(json['createdAt'] ?? json['timestamp'] ?? json['when']) ??
          DateTime.now(),
      status: _string(json['status']),
    );
  }

  final String id;
  final String instagramScopedId;
  final String senderName;
  final String senderUsername;
  final String text;
  final String direction;
  final DateTime createdAt;
  final String status;

  bool get inbound => direction == 'entrada' || direction == 'inbound';
}

class InstagramMessagesResult extends InstagramActionResult {
  const InstagramMessagesResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.messages = const <InstagramMessage>[],
  });

  factory InstagramMessagesResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    final rawMessages = json['messages'];
    return InstagramMessagesResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      messages: rawMessages is List
          ? rawMessages
                .whereType<Map>()
                .map(
                  (item) => InstagramMessage.fromJson(
                    item.map((key, value) => MapEntry(key.toString(), value)),
                  ),
                )
                .toList(growable: false)
          : const <InstagramMessage>[],
    );
  }

  final List<InstagramMessage> messages;
}

class InstagramSendResult extends InstagramActionResult {
  const InstagramSendResult({
    required super.ok,
    super.message,
    super.statusCode,
    this.remoteMessageId = '',
  });

  factory InstagramSendResult.fromJson(
    Map<String, Object?> json, {
    required int statusCode,
  }) {
    return InstagramSendResult(
      ok: _bool(json['ok']),
      message: _string(json['message']),
      statusCode: statusCode,
      remoteMessageId: _string(
        json['remoteMessageId'] ?? json['messageId'] ?? json['id'],
      ),
    );
  }

  final String remoteMessageId;
}

enum InstagramFailure {
  invalidConfiguration,
  validation,
  network,
  invalidResponse,
}

class InstagramException implements Exception {
  const InstagramException(
    this.failure,
    this.message, {
    this.statusCode,
    this.cause,
  });

  final InstagramFailure failure;
  final String message;
  final int? statusCode;
  final Object? cause;

  @override
  String toString() => 'InstagramException(${failure.name}): $message';
}

class _InstagramHttpResponse {
  const _InstagramHttpResponse({required this.statusCode, required this.json});

  final int statusCode;
  final Map<String, Object?> json;
}

String _string(Object? value) => value?.toString().trim() ?? '';

String _normalizeUsername(Object? value) =>
    _string(value).replaceFirst(RegExp(r'^@+'), '').replaceAll(' ', '');

bool _bool(Object? value) => switch (value) {
  true => true,
  String text => text.toLowerCase() == 'true' || text == '1',
  num number => number != 0,
  _ => false,
};

DateTime? _date(Object? value) {
  final text = _string(value);
  if (text.isEmpty) return null;
  final parsed = DateTime.tryParse(text);
  if (parsed != null) return parsed.toLocal();
  final unix = int.tryParse(text);
  if (unix == null) return null;
  return unix > 10000000000
      ? DateTime.fromMillisecondsSinceEpoch(unix)
      : DateTime.fromMillisecondsSinceEpoch(unix * 1000);
}

Uri? _uri(Object? value) {
  final text = _string(value);
  final uri = Uri.tryParse(text);
  return uri != null && uri.hasScheme ? uri : null;
}
