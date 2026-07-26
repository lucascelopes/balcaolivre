import 'dart:io';

import 'package:agenda_livre/domain/models/models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('Flutter serializes every field exposed by the WPF data contract', () {
    final modelsSource = _wpfModelsFile().readAsStringSync();
    final flutterContracts = <String, Set<String>>{
      'AgendaData': AgendaData().toJson().keys.toSet(),
      'AgendaSettings': AgendaSettings().toJson().keys.toSet(),
      'ServiceItem': ServiceItem().toJson().keys.toSet(),
      'Professional': Professional().toJson().keys.toSet(),
      'Customer': Customer().toJson().keys.toSet(),
      'Appointment': Appointment().toJson().keys.toSet(),
      'ProductItem': ProductItem().toJson().keys.toSet(),
      'ProductSale': ProductSale().toJson().keys.toSet(),
      'ManualPayment': ManualPayment().toJson().keys.toSet(),
      'CustomerReceivable': CustomerReceivable().toJson().keys.toSet(),
      'ExpenseItem': ExpenseItem().toJson().keys.toSet(),
      'WhatsAppMessage': WhatsAppMessage().toJson().keys.toSet(),
      'WhatsAppLead': WhatsAppLead().toJson().keys.toSet(),
    };

    for (final entry in flutterContracts.entries) {
      expect(
        entry.value,
        _wpfProperties(modelsSource, entry.key),
        reason:
            '${entry.key} must keep the exact same JSON fields in WPF and Flutter.',
      );
    }
  });
}

File _wpfModelsFile() {
  final candidates = <File>[
    File('../AgendaLivre.Windows/Models.cs'),
    File('AgendaLivre.Windows/Models.cs'),
  ];
  return candidates.firstWhere(
    (candidate) => candidate.existsSync(),
    orElse: () => throw StateError('AgendaLivre.Windows/Models.cs not found.'),
  );
}

Set<String> _wpfProperties(String source, String className) {
  final classPattern = RegExp('public sealed class $className\\s*\\{');
  final classMatch = classPattern.firstMatch(source);
  if (classMatch == null) {
    throw StateError('WPF class $className not found.');
  }

  final nextClassMatches = RegExp(
    r'\npublic sealed class \w+\s*\{',
  ).allMatches(source, classMatch.end);
  final nextClass = nextClassMatches.isEmpty ? null : nextClassMatches.first;
  final body = source.substring(
    classMatch.end,
    nextClass?.start ?? source.length,
  );
  return RegExp(
    r'public\s+[^;\r\n]+?\s+(\w+)\s*\{\s*get;\s*set;\s*\}',
  ).allMatches(body).map((match) => match.group(1)!).toSet();
}
