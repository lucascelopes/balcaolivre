class MercadoPagoTerminalVisual {
  const MercadoPagoTerminalVisual({
    required this.modelCode,
    required this.modelName,
    required this.serial,
    required this.assetPath,
  });

  factory MercadoPagoTerminalVisual.resolve({
    required String terminalId,
    String terminalLabel = '',
    String modelCode = '',
    String modelName = '',
    String serial = '',
  }) {
    final cleanId = terminalId.trim();
    final cleanLabel = terminalLabel.trim();
    var resolvedCode = modelCode.trim().toUpperCase();
    if (resolvedCode.isEmpty) {
      final separator = cleanId.indexOf('__');
      if (separator > 0) {
        resolvedCode = cleanId.substring(0, separator).trim().toUpperCase();
      }
    }
    if (resolvedCode.isEmpty) {
      final searchable = '$cleanId $cleanLabel'.toUpperCase();
      resolvedCode = _knownCodes.firstWhere(
        searchable.contains,
        orElse: () => '',
      );
    }

    var resolvedSerial = serial.trim();
    if (resolvedSerial.isEmpty) {
      final separator = cleanId.indexOf('__');
      resolvedSerial = separator >= 0 && separator + 2 < cleanId.length
          ? cleanId.substring(separator + 2).trim()
          : cleanId;
    }

    final resolvedName = modelName.trim().isNotEmpty
        ? modelName.trim()
        : switch (resolvedCode) {
            'NEWLAND_N950' => 'Point Smart 2',
            'INGENICO_MOVE2500' => 'Point Pro',
            'GERTEC_MP35P' => 'Point Pro 2',
            'PAX_A910' => 'Point Smart',
            'PAX_Q92' => 'Point Pro 3',
            _ => _nameFromLabel(cleanLabel),
          };

    return MercadoPagoTerminalVisual(
      modelCode: resolvedCode,
      modelName: resolvedName,
      serial: resolvedSerial,
      assetPath: switch (resolvedCode) {
        'NEWLAND_N950' => 'assets/branding/mercado-pago-newland-n950.png',
        'INGENICO_MOVE2500' =>
          'assets/branding/mercado-pago-ingenico-move2500.png',
        'GERTEC_MP35P' => 'assets/branding/mercado-pago-gertec-mp35p.png',
        'PAX_A910' => 'assets/branding/mercado-pago-pax-a910.png',
        'PAX_Q92' => 'assets/branding/mercado-pago-pax-q92.png',
        _ => '',
      },
    );
  }

  final String modelCode;
  final String modelName;
  final String serial;
  final String assetPath;

  static const _knownCodes = <String>[
    'NEWLAND_N950',
    'INGENICO_MOVE2500',
    'GERTEC_MP35P',
    'PAX_A910',
    'PAX_Q92',
  ];

  static String _nameFromLabel(String label) {
    final clean = label
        .replaceAll(RegExp(r'\s*[·|]\s*.*$'), '')
        .replaceAll(RegExp(r'\s*\(PDV\)\s*$', caseSensitive: false), '')
        .trim();
    return clean.isEmpty ? 'Point Mercado Pago' : clean;
  }
}
