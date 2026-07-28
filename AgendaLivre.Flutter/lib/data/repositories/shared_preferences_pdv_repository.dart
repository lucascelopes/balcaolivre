import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../../domain/pdv/pdv.dart';
import '../../domain/repositories/pdv_repository.dart';

class SharedPreferencesPdvRepository implements PdvRepository {
  SharedPreferencesPdvRepository(
    this._preferences, {
    this.storageKey = defaultStorageKey,
  });

  static const String defaultStorageKey = 'balcao_livre.pdv.v1';

  final SharedPreferences _preferences;
  final String storageKey;

  String get backupKey => '$storageKey.backup';

  static Future<SharedPreferencesPdvRepository> create({
    String storageKey = defaultStorageKey,
  }) async {
    final preferences = await SharedPreferences.getInstance();
    return SharedPreferencesPdvRepository(preferences, storageKey: storageKey);
  }

  @override
  Future<bool> hasData() async {
    final value = _preferences.getString(storageKey);
    return value != null && value.trim().isNotEmpty;
  }

  @override
  Future<PdvStore?> load() async {
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
  Future<PdvStore> loadOrCreate({
    required String storeId,
    required String terminalId,
  }) async {
    final stored = await load();
    if (stored != null) {
      return stored;
    }
    final created = PdvStore(storeId: storeId, terminalId: terminalId);
    await save(created);
    return created;
  }

  @override
  Future<void> save(PdvStore store) async {
    final previous = _preferences.getString(storageKey);
    if (previous != null && previous.trim().isNotEmpty) {
      await _write(backupKey, previous);
    }
    await _write(storageKey, jsonEncode(store.toJson()));
  }

  @override
  Future<void> clear() async {
    final primaryRemoved = await _preferences.remove(storageKey);
    final backupRemoved = await _preferences.remove(backupKey);
    if (!primaryRemoved || !backupRemoved) {
      throw StateError('Não foi possível limpar os dados locais do PDV.');
    }
  }

  PdvStore? _decode(String source) {
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map) {
        return null;
      }
      return PdvStore.fromJson(Map<String, dynamic>.from(decoded));
    } on FormatException {
      return null;
    } on TypeError {
      return null;
    }
  }

  Future<void> _write(String key, String value) async {
    final saved = await _preferences.setString(key, value);
    if (!saved) {
      throw StateError('Não foi possível salvar os dados locais do PDV.');
    }
  }
}
