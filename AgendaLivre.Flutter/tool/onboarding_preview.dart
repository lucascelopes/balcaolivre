import 'package:agenda_livre/app/agenda_app.dart';
import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:intl/intl.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  Intl.defaultLocale = 'pt_BR';
  await initializeDateFormatting('pt_BR');
  runApp(
    AgendaLivreApp(
      controller: AgendaController(
        _OnboardingPreviewRepository(
          AgendaData(
            settings: AgendaSettings(
              businessName: 'Balcão Livre',
              onboardingCompleted: false,
            ),
          ),
        ),
      ),
    ),
  );
}

class _OnboardingPreviewRepository implements AgendaRepository {
  _OnboardingPreviewRepository(this.value);

  AgendaData? value;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async =>
      value == null ? null : AgendaData.fromJson(value!.toJson());

  @override
  Future<AgendaData> loadOrCreate() async =>
      value == null ? AgendaData() : AgendaData.fromJson(value!.toJson());

  @override
  Future<void> save(AgendaData data) async {
    value = AgendaData.fromJson(data.toJson());
  }
}
