import 'id_generator.dart';
import 'json_helpers.dart';

class CustomerReceivable {
  CustomerReceivable({
    String? id,
    this.customerId = '',
    this.customerName = '',
    this.appointmentId = '',
    this.description = '',
    this.originalValue = 0,
    this.remainingValue = 0,
    this.status = 'open',
    DateTime? openedAt,
    DateTime? updatedAt,
    this.dueAt,
    this.paidAt,
    this.paymentMethod = '',
    this.paymentProvider = '',
    this.paymentReference = '',
    this.paymentStatus = '',
    this.cashSessionId = '',
    this.sourceChannel = '',
    this.channelConversationId = '',
    this.notes = '',
  }) : id = agendaIdOrGenerate(id),
       openedAt = openedAt ?? DateTime.now(),
       updatedAt = updatedAt ?? DateTime.now();

  String id;
  String customerId;
  String customerName;
  String appointmentId;
  String description;
  double originalValue;
  double remainingValue;
  String status;
  DateTime openedAt;
  DateTime updatedAt;
  DateTime? dueAt;
  DateTime? paidAt;
  String paymentMethod;
  String paymentProvider;
  String paymentReference;
  String paymentStatus;
  String cashSessionId;
  String sourceChannel;
  String channelConversationId;
  String notes;

  factory CustomerReceivable.fromJson(JsonMap json) => CustomerReceivable(
    id: jsonString(json, 'Id'),
    customerId: jsonString(json, 'CustomerId'),
    customerName: jsonString(json, 'CustomerName'),
    appointmentId: jsonString(json, 'AppointmentId'),
    description: jsonString(json, 'Description'),
    originalValue: jsonDouble(json, 'OriginalValue'),
    remainingValue: jsonDouble(json, 'RemainingValue'),
    status: jsonString(json, 'Status', fallback: 'open'),
    openedAt: jsonDateTime(json, 'OpenedAt', fallback: DateTime.now()),
    updatedAt: jsonDateTime(json, 'UpdatedAt', fallback: DateTime.now()),
    dueAt: jsonNullableDateTime(json, 'DueAt'),
    paidAt: jsonNullableDateTime(json, 'PaidAt'),
    paymentMethod: jsonString(json, 'PaymentMethod'),
    paymentProvider: jsonString(json, 'PaymentProvider'),
    paymentReference: jsonString(json, 'PaymentReference'),
    paymentStatus: jsonString(json, 'PaymentStatus'),
    cashSessionId: jsonString(json, 'CashSessionId'),
    sourceChannel: jsonString(json, 'SourceChannel'),
    channelConversationId: jsonString(json, 'ChannelConversationId'),
    notes: jsonString(json, 'Notes'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'CustomerId': customerId,
    'CustomerName': customerName,
    'AppointmentId': appointmentId,
    'Description': description,
    'OriginalValue': originalValue,
    'RemainingValue': remainingValue,
    'Status': status,
    'OpenedAt': openedAt.toIso8601String(),
    'UpdatedAt': updatedAt.toIso8601String(),
    'DueAt': dateTimeToJson(dueAt),
    'PaidAt': dateTimeToJson(paidAt),
    'PaymentMethod': paymentMethod,
    'PaymentProvider': paymentProvider,
    'PaymentReference': paymentReference,
    'PaymentStatus': paymentStatus,
    'CashSessionId': cashSessionId,
    'SourceChannel': sourceChannel,
    'ChannelConversationId': channelConversationId,
    'Notes': notes,
  };
}
