import 'id_generator.dart';
import 'json_helpers.dart';

class WhatsAppMessage {
  WhatsAppMessage({
    String? id,
    this.providerMessageId = '',
    this.provider = '',
    this.instance = '',
    this.conversationId = '',
    this.clientRequestId = '',
    this.leadId = '',
    this.customerName = '',
    this.phone = '',
    this.message = '',
    this.direction = 'saida',
    this.type = 'text',
    this.kind = '',
    this.status = 'criado',
    this.category = 'Atendimento',
    DateTime? createdAt,
    this.sentAt,
    this.receivedAt,
    this.readAt,
  }) : id = agendaIdOrGenerate(id),
       createdAt = createdAt ?? DateTime.now();

  String id;
  String providerMessageId;
  String provider;
  String instance;
  String conversationId;
  String clientRequestId;
  String leadId;
  String customerName;
  String phone;
  String message;
  String direction;
  String type;
  String kind;
  String status;
  String category;
  DateTime createdAt;
  DateTime? sentAt;
  DateTime? receivedAt;
  DateTime? readAt;

  factory WhatsAppMessage.fromJson(JsonMap json) => WhatsAppMessage(
    id: jsonString(json, 'Id'),
    providerMessageId: jsonString(json, 'ProviderMessageId'),
    provider: jsonString(json, 'Provider'),
    instance: jsonString(json, 'Instance'),
    conversationId: jsonString(json, 'ConversationId'),
    clientRequestId: jsonString(json, 'ClientRequestId'),
    leadId: jsonString(json, 'LeadId'),
    customerName: jsonString(json, 'CustomerName'),
    phone: jsonString(json, 'Phone'),
    message: jsonString(json, 'Message'),
    direction: jsonString(json, 'Direction', fallback: 'saida'),
    type: jsonString(json, 'Type', fallback: 'text'),
    kind: jsonString(json, 'Kind'),
    status: jsonString(json, 'Status', fallback: 'criado'),
    category: jsonString(json, 'Category', fallback: 'Atendimento'),
    createdAt: jsonDateTime(json, 'CreatedAt', fallback: DateTime.now()),
    sentAt: jsonNullableDateTime(json, 'SentAt'),
    receivedAt: jsonNullableDateTime(json, 'ReceivedAt'),
    readAt: jsonNullableDateTime(json, 'ReadAt'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'ProviderMessageId': providerMessageId,
    'Provider': provider,
    'Instance': instance,
    'ConversationId': conversationId,
    'ClientRequestId': clientRequestId,
    'LeadId': leadId,
    'CustomerName': customerName,
    'Phone': phone,
    'Message': message,
    'Direction': direction,
    'Type': type,
    'Kind': kind,
    'Status': status,
    'Category': category,
    'CreatedAt': createdAt.toIso8601String(),
    'SentAt': dateTimeToJson(sentAt),
    'ReceivedAt': dateTimeToJson(receivedAt),
    'ReadAt': dateTimeToJson(readAt),
  };
}
