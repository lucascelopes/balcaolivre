/// Contract for a server-side Evolution proxy.
///
/// Implementations must authenticate with a short-lived end-user session and
/// must never expose an Evolution API key, service-role key, HMAC secret or
/// instance credential to this Flutter client.
abstract interface class WhatsAppEvolutionProxy {
  Future<WhatsAppProxyConnection> getConnection();

  Future<WhatsAppProxyQrCode> requestQrCode();

  Future<void> disconnect();

  Future<WhatsAppProxySendResult> sendText({
    required String phone,
    required String message,
  });
}

class WhatsAppService {
  const WhatsAppService({WhatsAppEvolutionProxy? proxy}) : _proxy = proxy;

  final WhatsAppEvolutionProxy? _proxy;

  bool get hasEvolutionProxy => _proxy != null;

  /// Converts Brazilian local numbers to the international wa.me format.
  /// Already international numbers are preserved after punctuation is removed.
  static String normalizePhone(String value) {
    var digits = value.replaceAll(RegExp(r'\D'), '');
    if (digits.length == 10 || digits.length == 11) {
      digits = '55$digits';
    }

    if (digits.length < 8 || digits.length > 15) {
      throw const WhatsAppValidationException(
        'Informe um telefone válido com DDD e código do país.',
      );
    }
    return digits;
  }

  /// Builds a safe HTTPS deep link. Opening the URI remains a UI/platform
  /// responsibility, keeping this adapter independent from url_launcher.
  Uri buildWaMeUri({required String phone, String message = ''}) {
    final normalizedPhone = normalizePhone(phone);
    final cleanMessage = message.trim();
    return Uri.https(
      'wa.me',
      '/$normalizedPhone',
      cleanMessage.isEmpty ? null : <String, String>{'text': cleanMessage},
    );
  }

  /// Sends through the configured secure proxy when available and always
  /// returns a wa.me fallback URI for graceful recovery.
  Future<WhatsAppSendResult> sendText({
    required String phone,
    required String message,
  }) async {
    final normalizedPhone = normalizePhone(phone);
    final cleanMessage = message.trim();
    if (cleanMessage.isEmpty) {
      throw const WhatsAppValidationException(
        'A mensagem do WhatsApp não pode estar vazia.',
      );
    }

    final fallbackUri = buildWaMeUri(
      phone: normalizedPhone,
      message: cleanMessage,
    );
    final proxy = _proxy;
    if (proxy == null) {
      return WhatsAppSendResult(
        sent: false,
        proxyAttempted: false,
        fallbackUri: fallbackUri,
        status: 'fallback',
        message: 'Abra o WhatsApp para concluir o envio.',
      );
    }

    try {
      final proxyResult = await proxy.sendText(
        phone: normalizedPhone,
        message: cleanMessage,
      );
      return WhatsAppSendResult(
        sent: proxyResult.accepted,
        proxyAttempted: true,
        fallbackUri: fallbackUri,
        messageId: proxyResult.messageId,
        status: proxyResult.status,
        message: proxyResult.message,
      );
    } on Object catch (error) {
      return WhatsAppSendResult(
        sent: false,
        proxyAttempted: true,
        fallbackUri: fallbackUri,
        status: 'proxy_error',
        message: 'O envio online falhou. Abra o WhatsApp para continuar.',
        cause: error,
      );
    }
  }

  Future<WhatsAppProxyConnection> getConnection() async {
    final proxy = _proxy;
    if (proxy == null) {
      return const WhatsAppProxyConnection(
        state: WhatsAppConnectionState.disconnected,
      );
    }
    return proxy.getConnection();
  }

  Future<WhatsAppProxyQrCode> requestQrCode() {
    final proxy = _proxy;
    if (proxy == null) {
      throw const WhatsAppProxyUnavailableException();
    }
    return proxy.requestQrCode();
  }

  Future<void> disconnect() async {
    final proxy = _proxy;
    if (proxy != null) {
      await proxy.disconnect();
    }
  }
}

enum WhatsAppConnectionState {
  disconnected,
  awaitingQrCode,
  connected,
  error,
  unknown,
}

class WhatsAppProxyConnection {
  const WhatsAppProxyConnection({
    required this.state,
    this.phone = '',
    this.displayName = '',
    this.checkedAt,
    this.message = '',
  });

  final WhatsAppConnectionState state;
  final String phone;
  final String displayName;
  final DateTime? checkedAt;
  final String message;

  bool get isConnected => state == WhatsAppConnectionState.connected;
}

class WhatsAppProxyQrCode {
  const WhatsAppProxyQrCode({required this.base64, this.expiresAt});

  final String base64;
  final DateTime? expiresAt;
}

class WhatsAppProxySendResult {
  const WhatsAppProxySendResult({
    required this.accepted,
    this.messageId = '',
    this.status = '',
    this.message = '',
  });

  final bool accepted;
  final String messageId;
  final String status;
  final String message;
}

class WhatsAppSendResult {
  const WhatsAppSendResult({
    required this.sent,
    required this.proxyAttempted,
    required this.fallbackUri,
    this.messageId = '',
    this.status = '',
    this.message = '',
    this.cause,
  });

  final bool sent;
  final bool proxyAttempted;
  final Uri fallbackUri;
  final String messageId;
  final String status;
  final String message;
  final Object? cause;
}

class WhatsAppValidationException implements Exception {
  const WhatsAppValidationException(this.message);

  final String message;

  @override
  String toString() => 'WhatsAppValidationException: $message';
}

class WhatsAppProxyUnavailableException implements Exception {
  const WhatsAppProxyUnavailableException();

  @override
  String toString() =>
      'WhatsAppProxyUnavailableException: proxy seguro não configurado.';
}
