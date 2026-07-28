/// Minimal, injectable HTTP contract used by the integration adapters.
///
/// Keeping the transport behind an interface makes the services unit-testable
/// and avoids coupling credentials or platform-specific clients to domain code.
abstract interface class HttpTransport {
  Future<ServiceHttpResponse> send(ServiceHttpRequest request);
}

class ServiceHttpRequest {
  const ServiceHttpRequest({
    required this.method,
    required this.uri,
    this.headers = const <String, String>{},
    this.body,
    this.timeout,
  });

  final String method;
  final Uri uri;
  final Map<String, String> headers;
  final String? body;
  final Duration? timeout;
}

class ServiceHttpResponse {
  const ServiceHttpResponse({
    required this.statusCode,
    required this.body,
    this.headers = const <String, String>{},
  });

  final int statusCode;
  final String body;
  final Map<String, String> headers;

  bool get isSuccess => statusCode >= 200 && statusCode < 300;
}

class HttpTransportException implements Exception {
  const HttpTransportException(this.message, {this.uri, this.cause});

  final String message;
  final Uri? uri;
  final Object? cause;

  @override
  String toString() => 'HttpTransportException: $message';
}
