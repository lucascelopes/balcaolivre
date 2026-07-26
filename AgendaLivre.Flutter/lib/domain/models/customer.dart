import 'id_generator.dart';
import 'json_helpers.dart';

class Customer {
  Customer({
    String? id,
    this.name = '',
    this.phone = '',
    this.email = '',
    this.document = '',
    this.segment = '',
    this.profile = '',
    this.tags = '',
    this.notes = '',
    this.acceptsWhatsApp = true,
    DateTime? lastSeenAt,
  }) : id = agendaIdOrGenerate(id),
       lastSeenAt = lastSeenAt ?? DateTime.now();

  String id;
  String name;
  String phone;
  String email;
  String document;
  String segment;
  String profile;
  String tags;
  String notes;
  bool acceptsWhatsApp;
  DateTime lastSeenAt;

  factory Customer.fromJson(JsonMap json) => Customer(
    id: jsonString(json, 'Id'),
    name: jsonString(json, 'Name'),
    phone: jsonString(json, 'Phone'),
    email: jsonString(json, 'Email'),
    document: jsonString(json, 'Document'),
    segment: jsonString(json, 'Segment'),
    profile: jsonString(json, 'Profile'),
    tags: jsonString(json, 'Tags'),
    notes: jsonString(json, 'Notes'),
    acceptsWhatsApp: jsonBool(json, 'AcceptsWhatsApp', fallback: true),
    lastSeenAt: jsonDateTime(json, 'LastSeenAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Name': name,
    'Phone': phone,
    'Email': email,
    'Document': document,
    'Segment': segment,
    'Profile': profile,
    'Tags': tags,
    'Notes': notes,
    'AcceptsWhatsApp': acceptsWhatsApp,
    'LastSeenAt': lastSeenAt.toIso8601String(),
  };
}
