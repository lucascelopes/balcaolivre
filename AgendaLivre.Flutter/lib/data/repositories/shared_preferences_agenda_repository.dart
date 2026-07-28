import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../../domain/models/agenda_data.dart';
import '../../domain/repositories/agenda_repository.dart';
import '../seed/agenda_seed_data.dart';

typedef AgendaSeedFactory = AgendaData Function();

class SharedPreferencesAgendaRepository implements AgendaRepository {
  SharedPreferencesAgendaRepository(
    this._preferences, {
    AgendaSeedFactory? seedFactory,
    this.storageKey = defaultStorageKey,
  }) : _seedFactory = seedFactory ?? AgendaSeedData.salon;

  static const String defaultStorageKey = 'agenda_livre.data.v1';

  final SharedPreferences _preferences;
  final AgendaSeedFactory _seedFactory;
  final String storageKey;

  String get backupKey => '$storageKey.backup';

  static Future<SharedPreferencesAgendaRepository> create({
    AgendaSeedFactory? seedFactory,
    String storageKey = defaultStorageKey,
  }) async {
    final preferences = await SharedPreferences.getInstance();
    return SharedPreferencesAgendaRepository(
      preferences,
      seedFactory: seedFactory,
      storageKey: storageKey,
    );
  }

  @override
  Future<bool> hasData() async {
    final value = _preferences.getString(storageKey);
    return value != null && value.trim().isNotEmpty;
  }

  @override
  Future<AgendaData?> load() async {
    final primary = _preferences.getString(storageKey);
    if (primary == null || primary.trim().isEmpty) {
      return null;
    }

    final decoded = _decode(primary);
    if (decoded != null) {
      return decoded;
    }

    final backup = _preferences.getString(backupKey);
    final recovered = backup == null ? null : _decode(backup);
    if (recovered == null) {
      return null;
    }

    await _write(storageKey, jsonEncode(recovered.toJson()));
    return recovered;
  }

  @override
  Future<AgendaData> loadOrCreate() async {
    final stored = await load();
    if (stored != null) {
      return stored;
    }

    final seeded = _seedFactory();
    await save(seeded);
    return seeded;
  }

  @override
  Future<void> save(AgendaData data) async {
    final previous = _preferences.getString(storageKey);
    if (previous != null && previous.trim().isNotEmpty) {
      await _write(backupKey, previous);
    }
    await _write(storageKey, jsonEncode(data.toJson()));
  }

  @override
  Future<void> clear() async {
    final primaryRemoved = await _preferences.remove(storageKey);
    final backupRemoved = await _preferences.remove(backupKey);
    if (!primaryRemoved || !backupRemoved) {
      throw StateError('Não foi possível limpar os dados locais da agenda.');
    }
  }

  AgendaData? _decode(String source) {
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map) {
        return null;
      }
      return AgendaData.fromJson(Map<String, dynamic>.from(decoded));
    } on FormatException {
      return null;
    } on TypeError {
      return null;
    }
  }

  Future<void> _write(String key, String value) async {
    final saved = await _preferences.setString(key, value);
    if (!saved) {
      throw StateError('Não foi possível salvar os dados locais da agenda.');
    }
  }
}
