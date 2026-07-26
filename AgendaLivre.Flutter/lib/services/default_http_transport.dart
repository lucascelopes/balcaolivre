import 'dart:async';

import 'package:http/http.dart' as http;

import 'http_transport.dart';

HttpTransport createDefaultHttpTransport() => PackageHttpTransport();

class PackageHttpTransport implements HttpTransport {
  PackageHttpTransport({http.Client? client})
    : _client = client ?? http.Client();

  final http.Client _client;

  @override
  Future<ServiceHttpResponse> send(ServiceHttpRequest request) {
    final operation = _send(request);
    final timeout = request.timeout;
    if (timeout == null) {
      return operation;
    }

    return operation.timeout(
      timeout,
      onTimeout: () => throw HttpTransportException(
        'A requisição excedeu o tempo limite.',
        uri: request.uri,
      ),
    );
  }

  Future<ServiceHttpResponse> _send(ServiceHttpRequest request) async {
    try {
      final packageRequest = http.Request(request.method, request.uri)
        ..headers.addAll(request.headers);
      final body = request.body;
      if (body != null) {
        packageRequest.body = body;
      }

      final streamedResponse = await _client.send(packageRequest);
      final response = await http.Response.fromStream(streamedResponse);
      return ServiceHttpResponse(
        statusCode: response.statusCode,
        body: response.body,
        headers: response.headers,
      );
    } on HttpTransportException {
      rethrow;
    } on Object catch (error) {
      throw HttpTransportException(
        'Não foi possível concluir a requisição HTTP.',
        uri: request.uri,
        cause: error,
      );
    }
  }
}
