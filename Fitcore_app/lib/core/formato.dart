import 'package:intl/intl.dart';

// Fechas y pesos en formato colombiano (8 de octubre de 2026, $ 120.000).
const _locale = 'es_CO';

// El patrón es_CO de intl pone el símbolo al final; Angular (y el uso local) lo pone adelante.
final _moneda = NumberFormat.currency(locale: _locale, symbol: r'$', decimalDigits: 0, customPattern: '¤ #,##0');
final _decimal = NumberFormat('#,##0.##', _locale);

String moneda(num valor) => _moneda.format(valor);

/// Hasta dos decimales: 42,5.
String decimal(num valor) => _decimal.format(valor);

/// 8 oct 2026
String fechaMedia(DateTime f) => DateFormat.yMMMd(_locale).format(f);

/// 8 de octubre de 2026
String fechaLarga(DateTime f) => DateFormat.yMMMMd(_locale).format(f);

/// jueves, 8 de octubre de 2026
String fechaCompleta(DateTime f) => DateFormat.yMMMMEEEEd(_locale).format(f);

/// 08/10/26 14:30
String fechaCorta(DateTime f) => DateFormat('dd/MM/yy HH:mm', _locale).format(f);

/// 8 oct 2026, 14:30:00
String fechaHora(DateTime f) => '${fechaMedia(f)}, ${DateFormat.Hms(_locale).format(f)}';

/// 14:30:05
String hora(DateTime f) => DateFormat.Hms(_locale).format(f);

String plural(int n, String uno, String varios) => n == 1 ? uno : varios;
