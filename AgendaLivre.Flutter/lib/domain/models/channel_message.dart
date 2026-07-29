import 'id_generator.dart';
import 'json_helpers.dart';

class ChannelMessage {
  ChannelMessage({
    String? id,
    this.channel = '',
    this.accountId = '',
    this.conversationId = '',
    this.externalMessageId = '',
    this.externalUserId = '',
    this.externalUsername = '',
    this.customerId = '',
    this.appointmentId = '',
    this.direction = 'entrada',
    this.text = '',
    this.status = '',
    DateTime? createdAt,
  }) : id = agendaIdOrGenerate(id),
       createdAt = createdAt ?? DateTime.now();

  String id;
  String channel;
  String accountId;
  String conversationId;
  String externalMessageId;
  String externalUserId;
  String externalUsername;
  String customerId;
  String appointmentId;
  String direction;
  String text;
  String status;
  DateTime createdAt;

  factory ChannelMessage.fromJson(JsonMap json) => ChannelMessage(
    id: jsonString(json, 'Id'),
    channel: jsonString(json, 'Channel'),
    accountId: jsonString(json, 'AccountId'),
    conversationId: jsonString(json, 'ConversationId'),
    externalMessageId: jsonString(json, 'ExternalMessageId'),
    externalUserId: jsonString(json, 'ExternalUserId'),
    externalUsername: jsonString(json, 'ExternalUsername'),
    customerId: jsonString(json, 'CustomerId'),
    appointmentId: jsonString(json, 'AppointmentId'),
    direction: jsonString(json, 'Direction', fallback: 'entrada'),
    text: jsonString(json, 'Text'),
    status: jsonString(json, 'Status'),
    createdAt: jsonDateTime(json, 'CreatedAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Channel': channel,
    'AccountId': accountId,
    'ConversationId': conversationId,
    'ExternalMessageId': externalMessageId,
    'ExternalUserId': externalUserId,
    'ExternalUsername': externalUsername,
    'CustomerId': customerId,
    'AppointmentId': appointmentId,
    'Direction': direction,
    'Text': text,
    'Status': status,
    'CreatedAt': createdAt.toIso8601String(),
  };
}
