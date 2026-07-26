import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/via_cep_service.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fake_http_transport.dart';

void main() {
  group('ViaCepService', () {
    test('normalizes CEP, calls ViaCEP and parses the address', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(
          statusCode: 200,
          body: '''
{
  "cep": "01001-000",
  "logradouro": "Praça da Sé",
  "complemento": "lado ímpar",
  "bairro": "Sé",
  "localidade": "São Paulo",
  "uf": "SP",
  "ibge": "3550308",
  "gia": "1004",
  "ddd": "11",
  "siafi": "7107"
}
''',
        ),
      );
      final service = ViaCepService(transport: transport);

      final result = await service.lookup('01001-000');

      expect(result, isNotNull);
      expect(result!.street, 'Praça da Sé');
      expect(result.neighborhood, 'Sé');
      expect(result.city, 'São Paulo');
      expect(result.state, 'SP');
      expect(result.formattedCep, '01001-000');
      expect(transport.requests, hasLength(1));
      expect(transport.requests.single.method, 'GET');
      expect(
        transport.requests.single.uri.toString(),
        'https://viacep.com.br/ws/01001000/json/',
      );
    });

    test('returns null when ViaCEP reports an unknown CEP', () async {
      final transport = FakeHttpTransport(
        (_) =>
            const ServiceHttpResponse(statusCode: 200, body: '{"erro": true}'),
      );

      final result = await ViaCepService(
        transport: transport,
      ).lookup('00000-000');

      expect(result, isNull);
    });

    test('rejects malformed CEP without sending a request', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(statusCode: 200, body: '{}'),
      );

      expect(
        () => ViaCepService(transport: transport).lookup('1234'),
        throwsA(
          isA<ViaCepException>().having(
            (error) => error.failure,
            'failure',
            ViaCepFailure.invalidCep,
          ),
        ),
      );
      expect(transport.requests, isEmpty);
    });

    test('exposes HTTP failures without attempting to parse them', () async {
      final transport = FakeHttpTransport(
        (_) => const ServiceHttpResponse(
          statusCode: 503,
          body: '<html>offline</html>',
        ),
      );

      expect(
        () => ViaCepService(transport: transport).lookup('01001000'),
        throwsA(
          isA<ViaCepException>()
              .having((error) => error.failure, 'failure', ViaCepFailure.http)
              .having((error) => error.statusCode, 'statusCode', 503),
        ),
      );
    });
  });
}
