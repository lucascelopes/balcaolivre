import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:intl/intl.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app/agenda_app.dart';
import 'app/agenda_controller.dart';
import 'app/android_agenda_root.dart';
import 'app/web_agenda_root.dart';
import 'app/web_agenda_session.dart';
import 'data/repositories/shared_preferences_agenda_repository.dart';
import 'services/android_device_api.dart';
import 'services/android_secure_store.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  Intl.defaultLocale = 'pt_BR';
  await initializeDateFormatting('pt_BR');
  if (kIsWeb) {
    final preferences = await SharedPreferences.getInstance();
    runApp(
      AgendaLivreWebRoot(
        session: AgendaWebSessionController(preferences: preferences),
      ),
    );
    return;
  }
  if (defaultTargetPlatform == TargetPlatform.android) {
    final preferences = await SharedPreferences.getInstance();
    runApp(
      AgendaLivreAndroidRoot(
        session: AgendaAndroidSessionController(
          preferences: preferences,
          secureStore: const MethodChannelAndroidSecureStore(),
          config: AndroidBuildConfig.fromEnvironment(),
        ),
      ),
    );
    return;
  }
  final repository = await SharedPreferencesAgendaRepository.create();
  runApp(AgendaLivreApp(controller: AgendaController(repository)));
}
