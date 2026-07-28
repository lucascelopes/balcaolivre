import 'package:agenda_livre/services/agenda_livre_license_identity.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test(
    'matches the signed BLV identity accepted by the WPF cloud contract',
    () {
      expect(
        AgendaLivreLicenseIdentity.forAccount('user-123'),
        'BLV-203512312359-AGENDALIVREFCDEC6DF-271D97E378',
      );
      expect(
        AgendaLivreLicenseIdentity.forAccount(' USER-123 '),
        AgendaLivreLicenseIdentity.forAccount('user-123'),
      );
    },
  );
}
