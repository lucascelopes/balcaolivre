typedef JsonMap = Map<String, dynamic>;

dynamic jsonField(JsonMap json, String pascalCaseKey) {
  if (json.containsKey(pascalCaseKey)) {
    return json[pascalCaseKey];
  }

  if (pascalCaseKey.isEmpty) {
    return null;
  }

  final camelCaseKey =
      '${pascalCaseKey[0].toLowerCase()}${pascalCaseKey.substring(1)}';
  return json[camelCaseKey];
}

String jsonString(JsonMap json, String key, {String fallback = ''}) {
  final value = jsonField(json, key);
  if (value == null) {
    return fallback;
  }
  return value is String ? value : value.toString();
}

bool jsonBool(JsonMap json, String key, {bool fallback = false}) {
  final value = jsonField(json, key);
  if (value is bool) {
    return value;
  }
  if (value is num) {
    return value != 0;
  }
  if (value is String) {
    return switch (value.trim().toLowerCase()) {
      'true' || '1' || 'yes' || 'sim' => true,
      'false' || '0' || 'no' || 'nao' || 'não' => false,
      _ => fallback,
    };
  }
  return fallback;
}

int jsonInt(JsonMap json, String key, {int fallback = 0}) {
  final value = jsonField(json, key);
  if (value is int) {
    return value;
  }
  if (value is num) {
    return value.toInt();
  }
  return int.tryParse(value?.toString() ?? '') ?? fallback;
}

double jsonDouble(JsonMap json, String key, {double fallback = 0}) {
  final value = jsonField(json, key);
  if (value is num) {
    return value.toDouble();
  }
  final normalized = value?.toString().trim().replaceAll(',', '.') ?? '';
  return double.tryParse(normalized) ?? fallback;
}

DateTime jsonDateTime(JsonMap json, String key, {required DateTime fallback}) {
  return jsonNullableDateTime(json, key) ?? fallback;
}

DateTime? jsonNullableDateTime(JsonMap json, String key) {
  final value = jsonField(json, key);
  if (value is DateTime) {
    return value;
  }
  if (value is int) {
    return DateTime.fromMillisecondsSinceEpoch(value);
  }
  if (value is String && value.trim().isNotEmpty) {
    final source = value.trim();
    // System.Text.Json includes the Windows UTC offset for local DateTime
    // values. Appointments are business wall-clock times, so preserve their
    // written hour instead of shifting it with the browser/CI time zone.
    final offset = RegExp(r'[+-]\d{2}:\d{2}$').firstMatch(source);
    if (offset != null && source.contains('T')) {
      return DateTime.tryParse(source.substring(0, offset.start));
    }
    return DateTime.tryParse(source)?.toLocal();
  }
  return null;
}

List<String> jsonStringList(JsonMap json, String key) {
  final value = jsonField(json, key);
  if (value is! Iterable) {
    return <String>[];
  }
  return value
      .where((item) => item != null)
      .map((item) => item.toString())
      .toList(growable: true);
}

List<int> jsonIntList(JsonMap json, String key) {
  final value = jsonField(json, key);
  if (value is! Iterable) {
    return <int>[];
  }
  return value
      .map((item) => item is num ? item.toInt() : int.tryParse('$item'))
      .whereType<int>()
      .toList(growable: true);
}

List<JsonMap> jsonObjectList(JsonMap json, String key) {
  final value = jsonField(json, key);
  if (value is! Iterable) {
    return <JsonMap>[];
  }
  return value
      .whereType<Map>()
      .map((item) => Map<String, dynamic>.from(item))
      .toList(growable: true);
}

JsonMap jsonObject(JsonMap json, String key) {
  final value = jsonField(json, key);
  return value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};
}

String? dateTimeToJson(DateTime? value) => value?.toIso8601String();
