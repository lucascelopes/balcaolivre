int _agendaIdSequence = 0;

String generateAgendaId() {
  final timestamp = DateTime.now().microsecondsSinceEpoch.toRadixString(16);
  final sequence = (_agendaIdSequence++ & 0xffff)
      .toRadixString(16)
      .padLeft(4, '0');
  return '$timestamp$sequence';
}

String agendaIdOrGenerate(String? value) {
  final clean = value?.trim() ?? '';
  return clean.isEmpty ? generateAgendaId() : clean;
}
