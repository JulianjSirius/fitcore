import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_web_plugins/url_strategy.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:intl/intl.dart';

import 'app/rutas.dart';
import 'theme/tema.dart';

Future<void> main() async {
  // URLs limpias (/rutinas en vez de /#/rutinas), como en fitcore-web.
  usePathUrlStrategy();
  Intl.defaultLocale = 'es_CO';
  await initializeDateFormatting('es_CO');
  runApp(const FitCoreApp());
}

class FitCoreApp extends StatelessWidget {
  const FitCoreApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'FitCore',
      debugShowCheckedModeBanner: false,
      theme: temaFitCore(),
      routerConfig: enrutador,
      locale: const Locale('es', 'CO'),
      supportedLocales: const [Locale('es', 'CO'), Locale('es')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
    );
  }
}
