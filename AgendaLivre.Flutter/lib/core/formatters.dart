import 'package:intl/intl.dart';

final _currency = NumberFormat.currency(
  locale: 'pt_BR',
  symbol: 'R\$',
  decimalDigits: 2,
);
final _currencyWhole = NumberFormat.currency(
  locale: 'pt_BR',
  symbol: 'R\$',
  decimalDigits: 0,
);

String money(num value, {bool cents = true}) =>
    (cents ? _currency : _currencyWhole).format(value);

String shortDate(DateTime date) => DateFormat('dd/MM', 'pt_BR').format(date);

String fullDate(DateTime date) =>
    DateFormat("EEEE, d 'de' MMMM 'de' y", 'pt_BR')
        .format(date)
        .replaceFirstMapped(
          RegExp(r'^.'),
          (match) => match.group(0)!.toUpperCase(),
        );

String hour(DateTime date) => DateFormat('HH:mm', 'pt_BR').format(date);

String initials(String value) {
  final words = value
      .trim()
      .split(RegExp(r'\s+'))
      .where((item) => item.isNotEmpty)
      .toList();
  if (words.isEmpty) return 'AL';
  if (words.length == 1) {
    return words.first
        .substring(0, words.first.length.clamp(0, 2))
        .toUpperCase();
  }
  return '${words.first[0]}${words.last[0]}'.toUpperCase();
}
