import 'dart:convert';

import 'package:crypto/crypto.dart';

/// Produces the same signed BLV identity accepted by the Agenda Livre cloud
/// services and already used by the WPF client.
abstract final class AgendaLivreLicenseIdentity {
  static const String _secret = 'BalcaoLivrePDV-local-license-v1';
  static const String _expires = '203512312359';
  static const String _scopePrefix = 'AGENDALIVRE';

  static String machineCodeForAccount(String accountScope) {
    final normalized = accountScope.trim().toLowerCase();
    if (normalized.isEmpty) {
      throw const FormatException('A conta Agenda Livre não foi identificada.');
    }
    return sha256
        .convert(utf8.encode(normalized))
        .toString()
        .substring(0, 8)
        .toUpperCase();
  }

  static String forAccount(String accountScope) {
    final accountCode = machineCodeForAccount(accountScope);
    final serial = '$_scopePrefix$accountCode';
    final message = 'BLV|$_expires|$serial';
    final signature = Hmac(
      sha256,
      utf8.encode(_secret),
    ).convert(utf8.encode(message)).toString().substring(0, 10).toUpperCase();
    return 'BLV-$_expires-$serial-$signature';
  }
}
