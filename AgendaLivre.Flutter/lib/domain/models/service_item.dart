import 'id_generator.dart';
import 'json_helpers.dart';

class ServiceItem {
  ServiceItem({
    String? id,
    this.segment = '',
    this.name = '',
    this.category = '',
    this.description = '',
    this.durationMinutes = 30,
    this.preparationMinutes = 0,
    this.bufferMinutes = 0,
    this.price = 0,
    this.commissionPercent = 0,
    this.defaultResource = '',
    this.isActive = true,
  }) : id = agendaIdOrGenerate(id);

  String id;
  String segment;
  String name;
  String category;
  String description;
  int durationMinutes;
  int preparationMinutes;
  int bufferMinutes;
  double price;
  double commissionPercent;
  String defaultResource;
  bool isActive;

  String get displayName => '$name - $durationMinutes min';

  factory ServiceItem.fromJson(JsonMap json) => ServiceItem(
    id: jsonString(json, 'Id'),
    segment: jsonString(json, 'Segment'),
    name: jsonString(json, 'Name'),
    category: jsonString(json, 'Category'),
    description: jsonString(json, 'Description'),
    durationMinutes: jsonInt(json, 'DurationMinutes', fallback: 30),
    preparationMinutes: jsonInt(json, 'PreparationMinutes'),
    bufferMinutes: jsonInt(json, 'BufferMinutes'),
    price: jsonDouble(json, 'Price'),
    commissionPercent: jsonDouble(json, 'CommissionPercent'),
    defaultResource: jsonString(json, 'DefaultResource'),
    isActive: jsonBool(json, 'IsActive', fallback: true),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Segment': segment,
    'Name': name,
    'Category': category,
    'Description': description,
    'DurationMinutes': durationMinutes,
    'PreparationMinutes': preparationMinutes,
    'BufferMinutes': bufferMinutes,
    'Price': price,
    'CommissionPercent': commissionPercent,
    'DefaultResource': defaultResource,
    'IsActive': isActive,
  };

  @override
  String toString() => displayName;
}
