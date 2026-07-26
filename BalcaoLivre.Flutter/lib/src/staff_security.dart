import 'dart:convert';
import 'dart:math';
import 'dart:typed_data';

import 'package:crypto/crypto.dart';

class StaffSecurity {
  const StaffSecurity._();

  static const prefix = 'PBKDF2';
  static const iterations = 120000;
  static const saltLength = 16;
  static const hashLength = 32;

  static String hashPin(String pin, {List<int>? salt}) {
    final clean = pin.trim();
    if (clean.isEmpty) return '';
    final effectiveSalt = Uint8List.fromList(
      salt ??
          List<int>.generate(saltLength, (_) => Random.secure().nextInt(256)),
    );
    final hash = _pbkdf2(
      utf8.encode(clean),
      effectiveSalt,
      iterations,
      hashLength,
    );
    return [
      prefix,
      '$iterations',
      base64Encode(effectiveSalt),
      base64Encode(hash),
    ].join(r'$');
  }

  static bool verifyPin(String encoded, String pin) {
    final clean = pin.trim();
    if (encoded.trim().isEmpty || clean.isEmpty) return false;
    try {
      final parts = encoded.split(r'$');
      if (parts.length != 4 || parts[0] != prefix) return false;
      final rounds = int.parse(parts[1]);
      if (rounds < 1000 || rounds > 1000000) return false;
      final salt = base64Decode(parts[2]);
      final expected = base64Decode(parts[3]);
      final actual = _pbkdf2(utf8.encode(clean), salt, rounds, expected.length);
      return _fixedTimeEquals(actual, expected);
    } catch (_) {
      return false;
    }
  }

  static Uint8List _pbkdf2(
    List<int> password,
    List<int> salt,
    int rounds,
    int length,
  ) {
    final mac = Hmac(sha256, password);
    final output = Uint8List(length);
    var outputOffset = 0;
    var block = 1;
    while (outputOffset < length) {
      final blockInput = Uint8List(salt.length + 4)
        ..setRange(0, salt.length, salt)
        ..[salt.length] = (block >> 24) & 0xff
        ..[salt.length + 1] = (block >> 16) & 0xff
        ..[salt.length + 2] = (block >> 8) & 0xff
        ..[salt.length + 3] = block & 0xff;
      var current = Uint8List.fromList(mac.convert(blockInput).bytes);
      final mixed = Uint8List.fromList(current);
      for (var round = 1; round < rounds; round++) {
        current = Uint8List.fromList(mac.convert(current).bytes);
        for (var index = 0; index < mixed.length; index++) {
          mixed[index] ^= current[index];
        }
      }
      final take = min(mixed.length, length - outputOffset);
      output.setRange(outputOffset, outputOffset + take, mixed);
      outputOffset += take;
      block++;
    }
    return output;
  }

  static bool _fixedTimeEquals(List<int> left, List<int> right) {
    if (left.length != right.length) return false;
    var difference = 0;
    for (var index = 0; index < left.length; index++) {
      difference |= left[index] ^ right[index];
    }
    return difference == 0;
  }
}
