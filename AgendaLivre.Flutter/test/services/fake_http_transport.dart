import 'dart:async';

import 'package:agenda_livre/services/http_transport.dart';

typedef FakeHttpHandler =
    FutureOr<ServiceHttpResponse> Function(ServiceHttpRequest request);

class FakeHttpTransport implements HttpTransport {
  FakeHttpTransport(this.handler);

  final FakeHttpHandler handler;
  final List<ServiceHttpRequest> requests = <ServiceHttpRequest>[];

  @override
  Future<ServiceHttpResponse> send(ServiceHttpRequest request) async {
    requests.add(request);
    return handler(request);
  }
}
