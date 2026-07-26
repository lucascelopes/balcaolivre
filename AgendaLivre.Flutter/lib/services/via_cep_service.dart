import 'dart:convert';

import 'default_http_transport.dart';
import 'http_transport.dart';

class ViaCepService {
  ViaCepService({
    HttpTransport? transport,
    Uri? baseUri,
    this.timeout = const Duration(seconds: 8),
  }) : _transport = transport ?? createDefaultHttpTransport(),
       baseUri = baseUri ?? Uri.parse(defaultBaseUrl);

  static const String defaultBaseUrl = 'https://viacep.com.br/ws/';

  final HttpTransport _transport;
  final Uri baseUri;
  final Duration timeout;

  /// Looks up an address by CEP.
  ///
  /// Returns `null` when ViaCEP explicitly reports `erro: true`. Invalid CEPs,
  /// transport failures and malformed responses are reported as
  /// [ViaCepException] so the caller can distinguish them from "not found".
  Future<ViaCepAddress?> lookup(String cep) async {
    final normalizedCep = normalizeCep(cep);
    if (normalizedCep.length != 8) {
      throw const ViaCepException(
        ViaCepFailure.invalidCep,
        'O CEP deve conter exatamente 8 dígitos.',
      );
    }

    final uri = _endpointFor(normalizedCep);
    ServiceHttpResponse response;
    try {
      response = await _transport.send(
        ServiceHttpRequest(
          method: 'GET',
          uri: uri,
          headers: const <String, String>{'Accept': 'application/json'},
          timeout: timeout,
        ),
      );
    } on Object catch (error) {
      if (error is ViaCepException) {
        rethrow;
      }
      throw ViaCepException(
        ViaCepFailure.network,
        'Não foi possível consultar o CEP agora.',
        cause: error,
      );
    }

    if (!response.isSuccess) {
      throw ViaCepException(
        ViaCepFailure.http,
        'ViaCEP retornou HTTP ${response.statusCode}.',
        statusCode: response.statusCode,
      );
    }

    final Map<String, Object?> json;
    try {
      final decoded = jsonDecode(response.body);
      if (decoded is! Map) {
        throw const FormatException('A resposta não é um objeto JSON.');
      }
      json = decoded.map((key, value) => MapEntry(key.toString(), value));
    } on Object catch (error) {
      throw ViaCepException(
        ViaCepFailure.invalidResponse,
        'ViaCEP retornou uma resposta inválida.',
        cause: error,
      );
    }

    if (_readBool(json['erro'])) {
      return null;
    }

    return ViaCepAddress.fromJson(json);
  }

  static String normalizeCep(String value) =>
      value.replaceAll(RegExp(r'\D'), '');

  Uri _endpointFor(String cep) {
    final root = baseUri.toString().replaceFirst(RegExp(r'/+$'), '');
    return Uri.parse('$root/$cep/json/');
  }

  static bool _readBool(Object? value) => switch (value) {
    true => true,
    String text => text.toLowerCase() == 'true',
    num number => number != 0,
    _ => false,
  };
}

class ViaCepAddress {
  const ViaCepAddress({
    required this.cep,
    required this.street,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    required this.ibge,
    required this.gia,
    required this.ddd,
    required this.siafi,
    this.unit = '',
    this.region = '',
    this.stateName = '',
  });

  factory ViaCepAddress.fromJson(Map<String, Object?> json) {
    return ViaCepAddress(
      cep: _string(json['cep']),
      street: _string(json['logradouro']),
      complement: _string(json['complemento']),
      neighborhood: _string(json['bairro']),
      city: _string(json['localidade']),
      state: _string(json['uf']),
      ibge: _string(json['ibge']),
      gia: _string(json['gia']),
      ddd: _string(json['ddd']),
      siafi: _string(json['siafi']),
      unit: _string(json['unidade']),
      region: _string(json['regiao']),
      stateName: _string(json['estado']),
    );
  }

  final String cep;
  final String street;
  final String complement;
  final String neighborhood;
  final String city;
  final String state;
  final String ibge;
  final String gia;
  final String ddd;
  final String siafi;
  final String unit;
  final String region;
  final String stateName;

  String get formattedCep {
    final digits = ViaCepService.normalizeCep(cep);
    return digits.length == 8
        ? '${digits.substring(0, 5)}-${digits.substring(5)}'
        : cep;
  }

  static String _string(Object? value) => value?.toString().trim() ?? '';
}

enum ViaCepFailure { invalidCep, network, http, invalidResponse }

class ViaCepException implements Exception {
  const ViaCepException(
    this.failure,
    this.message, {
    this.statusCode,
    this.cause,
  });

  final ViaCepFailure failure;
  final String message;
  final int? statusCode;
  final Object? cause;

  @override
  String toString() => 'ViaCepException(${failure.name}): $message';
}
