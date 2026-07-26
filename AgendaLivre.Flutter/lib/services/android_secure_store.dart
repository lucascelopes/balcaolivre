import 'package:flutter/services.dart';

/// Stores Android device credentials behind the platform Keystore.
///
/// Only opaque server-issued credentials belong here. Agenda data and sync
/// metadata continue to use their per-account SharedPreferences namespace.
abstract interface class AndroidSecureStore {
  Future<String?> read(String key);

  Future<void> write(String key, String value);

  Future<void> delete(String key);
}

class MethodChannelAndroidSecureStore implements AndroidSecureStore {
  const MethodChannelAndroidSecureStore({MethodChannel? channel})
    : _channel = channel ?? const MethodChannel(channelName);

  static const String channelName = 'agenda_livre/secure_storage';

  final MethodChannel _channel;

  @override
  Future<String?> read(String key) async {
    final value = await _channel.invokeMethod<String>('read', <String, Object?>{
      'key': key,
    });
    final normalized = value?.trim() ?? '';
    return normalized.isEmpty ? null : normalized;
  }

  @override
  Future<void> write(String key, String value) async {
    if (value.isEmpty) {
      throw ArgumentError.value(value, 'value', 'must not be empty');
    }
    await _channel.invokeMethod<void>('write', <String, Object?>{
      'key': key,
      'value': value,
    });
  }

  @override
  Future<void> delete(String key) =>
      _channel.invokeMethod<void>('delete', <String, Object?>{'key': key});
}
