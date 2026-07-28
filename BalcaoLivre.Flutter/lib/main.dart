import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

import 'src/app.dart';

const _supabaseUrl = String.fromEnvironment(
  'BALCAO_SUPABASE_URL',
  defaultValue: 'https://hzvplpotsdzxygkxrgyi.supabase.co',
);

const _supabaseAnonKey = String.fromEnvironment(
  'BALCAO_SUPABASE_ANON_KEY',
  defaultValue: 'sb_publishable_qNl5_EGAeuhN6PqTzRIeyQ_YQV2MdV6',
);

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await Supabase.initialize(
      url: _supabaseUrl,
      publishableKey: _supabaseAnonKey,
    ).timeout(const Duration(seconds: 10));
  } catch (error, stackTrace) {
    FlutterError.reportError(
      FlutterErrorDetails(
        exception: error,
        stack: stackTrace,
        library: 'balcao_livre_flutter',
        context: ErrorDescription('inicializando sincronizacao remota'),
      ),
    );
  }
  runApp(const BalcaoLivreApp());
}
