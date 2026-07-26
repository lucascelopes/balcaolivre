import 'id_generator.dart';
import 'json_helpers.dart';

class WhatsAppLead {
  WhatsAppLead({
    String? id,
    this.instance = '',
    this.conversationId = '',
    this.customerName = '',
    this.phone = '',
    this.stage = 'new',
    this.score = 0,
    this.summary = '',
    List<String>? facts,
    this.intent = '',
    this.requestedService = '',
    this.preferredSchedule = '',
    this.assignedProfessional = '',
    this.preferredDate = '',
    this.period = '',
    this.unread = false,
    this.unreadCount = 0,
    this.followupCount = 0,
    this.nextFollowupAt,
    this.lastInboundAt,
    this.lastOutboundAt,
    this.optedOutAt,
    this.handedOffAt,
    this.notes = '',
    DateTime? createdAt,
    DateTime? updatedAt,
    this.lastMessageAt,
  }) : id = agendaIdOrGenerate(id),
       facts = List<String>.of(facts ?? const <String>[]),
       createdAt = createdAt ?? DateTime.now(),
       updatedAt = updatedAt ?? DateTime.now();

  String id;
  String instance;
  String conversationId;
  String customerName;
  String phone;
  String stage;
  int score;
  String summary;
  List<String> facts;
  String intent;
  String requestedService;
  String preferredSchedule;
  String assignedProfessional;
  String preferredDate;
  String period;
  bool unread;
  int unreadCount;
  int followupCount;
  DateTime? nextFollowupAt;
  DateTime? lastInboundAt;
  DateTime? lastOutboundAt;
  DateTime? optedOutAt;
  DateTime? handedOffAt;
  String notes;
  DateTime createdAt;
  DateTime updatedAt;
  DateTime? lastMessageAt;

  factory WhatsAppLead.fromJson(JsonMap json) => WhatsAppLead(
    id: jsonString(json, 'Id'),
    instance: jsonString(json, 'Instance'),
    conversationId: jsonString(json, 'ConversationId'),
    customerName: jsonString(json, 'CustomerName'),
    phone: jsonString(json, 'Phone'),
    stage: jsonString(json, 'Stage', fallback: 'new'),
    score: jsonInt(json, 'Score'),
    summary: jsonString(json, 'Summary'),
    facts: jsonStringList(json, 'Facts'),
    intent: jsonString(json, 'Intent'),
    requestedService: jsonString(json, 'RequestedService'),
    preferredSchedule: jsonString(json, 'PreferredSchedule'),
    assignedProfessional: jsonString(json, 'AssignedProfessional'),
    preferredDate: jsonString(json, 'PreferredDate'),
    period: jsonString(json, 'Period'),
    unread: jsonBool(json, 'Unread'),
    unreadCount: jsonInt(json, 'UnreadCount'),
    followupCount: jsonInt(json, 'FollowupCount'),
    nextFollowupAt: jsonNullableDateTime(json, 'NextFollowupAt'),
    lastInboundAt: jsonNullableDateTime(json, 'LastInboundAt'),
    lastOutboundAt: jsonNullableDateTime(json, 'LastOutboundAt'),
    optedOutAt: jsonNullableDateTime(json, 'OptedOutAt'),
    handedOffAt: jsonNullableDateTime(json, 'HandedOffAt'),
    notes: jsonString(json, 'Notes'),
    createdAt: jsonDateTime(json, 'CreatedAt', fallback: DateTime.now()),
    updatedAt: jsonDateTime(json, 'UpdatedAt', fallback: DateTime.now()),
    lastMessageAt: jsonNullableDateTime(json, 'LastMessageAt'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Instance': instance,
    'ConversationId': conversationId,
    'CustomerName': customerName,
    'Phone': phone,
    'Stage': stage,
    'Score': score,
    'Summary': summary,
    'Facts': facts,
    'Intent': intent,
    'RequestedService': requestedService,
    'PreferredSchedule': preferredSchedule,
    'AssignedProfessional': assignedProfessional,
    'PreferredDate': preferredDate,
    'Period': period,
    'Unread': unread,
    'UnreadCount': unreadCount,
    'FollowupCount': followupCount,
    'NextFollowupAt': dateTimeToJson(nextFollowupAt),
    'LastInboundAt': dateTimeToJson(lastInboundAt),
    'LastOutboundAt': dateTimeToJson(lastOutboundAt),
    'OptedOutAt': dateTimeToJson(optedOutAt),
    'HandedOffAt': dateTimeToJson(handedOffAt),
    'Notes': notes,
    'CreatedAt': createdAt.toIso8601String(),
    'UpdatedAt': updatedAt.toIso8601String(),
    'LastMessageAt': dateTimeToJson(lastMessageAt),
  };
}
