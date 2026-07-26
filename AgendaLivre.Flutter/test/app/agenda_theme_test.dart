import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('ações primárias usam Accent e o mesmo contraste do WPF', () {
    for (final spec in AgendaThemes.all) {
      final theme = spec.toThemeData();
      final expectedForeground = _highestContrast(
        spec.tokens.accent,
        spec.tokens.ink,
      );
      final elevatedStyle = theme.elevatedButtonTheme.style!;

      expect(
        theme.colorScheme.primary,
        spec.tokens.accent,
        reason: '${spec.id}: ColorScheme.primary',
      );
      expect(
        theme.colorScheme.onPrimary,
        expectedForeground,
        reason: '${spec.id}: ColorScheme.onPrimary',
      );
      expect(
        elevatedStyle.backgroundColor!.resolve({}),
        spec.tokens.accent,
        reason: '${spec.id}: botão primário',
      );
      expect(
        elevatedStyle.foregroundColor!.resolve({}),
        expectedForeground,
        reason: '${spec.id}: contraste do botão primário',
      );
      expect(
        theme.snackBarTheme.backgroundColor,
        const Color(0x99000000),
        reason: '${spec.id}: fundo do aviso transitório',
      );
      expect(
        theme.snackBarTheme.behavior,
        SnackBarBehavior.floating,
        reason: '${spec.id}: aviso deve ficar afastado da borda inferior',
      );
      expect(
        theme.snackBarTheme.insetPadding,
        const EdgeInsets.fromLTRB(16, 12, 16, 24),
        reason: '${spec.id}: margem segura do aviso transitório',
      );
      expect(
        theme.snackBarTheme.contentTextStyle?.color,
        Colors.white,
        reason: '${spec.id}: texto do aviso transitório',
      );
      expect(
        theme.snackBarTheme.actionTextColor,
        Colors.white,
        reason: '${spec.id}: ação do aviso transitório',
      );
      expect(
        theme.snackBarTheme.closeIconColor,
        Colors.white,
        reason: '${spec.id}: ícone do aviso transitório',
      );
    }
  });
}

Color _highestContrast(Color background, Color darkForeground) {
  return _contrastRatio(background, darkForeground) >=
          _contrastRatio(background, Colors.white)
      ? darkForeground
      : Colors.white;
}

double _contrastRatio(Color first, Color second) {
  final firstLuminance = first.computeLuminance();
  final secondLuminance = second.computeLuminance();
  final lighter = firstLuminance > secondLuminance
      ? firstLuminance
      : secondLuminance;
  final darker = firstLuminance < secondLuminance
      ? firstLuminance
      : secondLuminance;
  return (lighter + 0.05) / (darker + 0.05);
}
