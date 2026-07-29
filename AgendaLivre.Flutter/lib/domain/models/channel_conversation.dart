import 'id_generator.dart';
import 'json_helpers.dart';

class ChannelConversation {
  ChannelConversation({
    String? id,
    this.channel = '',
    this.accountId = '',
    this.externalConversationId = '',
    this.externalUserId = '',
    this.externalUsername = '',
    this.customerId = '',
    this.customerName = '',
    this.phone = '',
    this.appointmentId = '',
    this.assignedAgent = '',
    this.intent = '',
    this.stage = 'new',
    this.unread = false,
    this.unreadCount = 0,
    this.lastInboundAt,
    this.lastOutboundAt,
    this.lastMessageAt,
    DateTime? createdAt,
    DateTime? updatedAt,
  }) : id = agendaIdOrGenerate(id),
       createdAt = createdAt ?? DateTime.now(),
       updatedAt = updatedAt ?? DateTime.now();

  String id;
  String channel;
  String accountId;
  String externalConversationId;
  String externalUserId;
  String externalUsername;
  String customerId;
  String customerName;
  String phone;
  String appointmentId;
  String assignedAgent;
  String intent;
  String stage;
  bool unread;
  int unreadCount;
  DateTime? lastInboundAt;
  DateTime? lastOutboundAt;
  DateTime? lastMessageAt;
  DateTime createdAt;
  DateTime updatedAt;

  factory ChannelConversation.fromJson(JsonMap json) => ChannelConversation(
    id: jsonString(json, 'Id'),
    channel: jsonString(json, 'Channel'),
    accountId: jsonString(json, 'AccountId'),
    externalConversationId: jsonString(json, 'ExternalConversationId'),
    externalUserId: jsonString(json, 'ExternalUserId'),
    externalUsername: jsonString(json, 'ExternalUsername'),
    customerId: jsonString(json, 'CustomerId'),
    customerName: jsonString(json, 'CustomerName'),
    phone: jsonString(json, 'Phone'),
    appointmentId: jsonString(json, 'AppointmentId'),
    assignedAgent: jsonString(json, 'AssignedAgent'),
    intent: jsonString(json, 'Intent'),
    stage: jsonString(json, 'Stage', fallback: 'new'),
    unread: jsonBool(json, 'Unread'),
    unreadCount: jsonInt(json, 'UnreadCount'),
    lastInboundAt: jsonNullableDateTime(json, 'LastInboundAt'),
    lastOutboundAt: jsonNullableDateTime(json, 'LastOutboundAt'),
    lastMessageAt: jsonNullableDateTime(json, 'LastMessageAt'),
    createdAt: jsonDateTime(json, 'CreatedAt', fallback: DateTime.now()),
    updatedAt: jsonDateTime(json, 'UpdatedAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Channel': channel,
    'AccountId': accountId,
    'ExternalConversationId': externalConversationId,
    'ExternalUserId': externalUserId,
    'ExternalUsername': externalUsername,
    'CustomerId': customerId,
    'CustomerName': customerName,
    'Phone': phone,
    'AppointmentId': appointmentId,
    'AssignedAgent': assignedAgent,
    'Intent': intent,
    'Stage': stage,
    'Unread': unread,
    'UnreadCount': unreadCount,
    'LastInboundAt': dateTimeToJson(lastInboundAt),
    'LastOutboundAt': dateTimeToJson(lastOutboundAt),
    'LastMessageAt': dateTimeToJson(lastMessageAt),
    'CreatedAt': createdAt.toIso8601String(),
    'UpdatedAt': updatedAt.toIso8601String(),
  };
}
