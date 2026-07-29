import 'id_generator.dart';
import 'json_helpers.dart';

class ProductSale {
  ProductSale({
    String? id,
    this.productId = '',
    this.productName = '',
    this.customerName = '',
    this.quantity = 1,
    this.unitPrice = 0,
    this.discount = 0,
    this.paymentMethod = '',
    this.paymentProvider = '',
    this.paymentReference = '',
    this.paymentStatus = '',
    this.cashSessionId = '',
    this.appointmentId = '',
    this.sourceChannel = '',
    this.channelConversationId = '',
    this.notes = '',
    DateTime? soldAt,
  }) : id = agendaIdOrGenerate(id),
       soldAt = soldAt ?? DateTime.now();

  String id;
  String productId;
  String productName;
  String customerName;
  int quantity;
  double unitPrice;
  double discount;
  String paymentMethod;
  String paymentProvider;
  String paymentReference;
  String paymentStatus;
  String cashSessionId;
  String appointmentId;
  String sourceChannel;
  String channelConversationId;
  String notes;
  DateTime soldAt;

  double get total {
    final result = (quantity * unitPrice) - discount;
    return result < 0 ? 0 : result;
  }

  factory ProductSale.fromJson(JsonMap json) => ProductSale(
    id: jsonString(json, 'Id'),
    productId: jsonString(json, 'ProductId'),
    productName: jsonString(json, 'ProductName'),
    customerName: jsonString(json, 'CustomerName'),
    quantity: jsonInt(json, 'Quantity', fallback: 1),
    unitPrice: jsonDouble(json, 'UnitPrice'),
    discount: jsonDouble(json, 'Discount'),
    paymentMethod: jsonString(json, 'PaymentMethod'),
    paymentProvider: jsonString(json, 'PaymentProvider'),
    paymentReference: jsonString(json, 'PaymentReference'),
    paymentStatus: jsonString(json, 'PaymentStatus'),
    cashSessionId: jsonString(json, 'CashSessionId'),
    appointmentId: jsonString(json, 'AppointmentId'),
    sourceChannel: jsonString(json, 'SourceChannel'),
    channelConversationId: jsonString(json, 'ChannelConversationId'),
    notes: jsonString(json, 'Notes'),
    soldAt: jsonDateTime(json, 'SoldAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'ProductId': productId,
    'ProductName': productName,
    'CustomerName': customerName,
    'Quantity': quantity,
    'UnitPrice': unitPrice,
    'Discount': discount,
    'PaymentMethod': paymentMethod,
    'PaymentProvider': paymentProvider,
    'PaymentReference': paymentReference,
    'PaymentStatus': paymentStatus,
    'CashSessionId': cashSessionId,
    'AppointmentId': appointmentId,
    'SourceChannel': sourceChannel,
    'ChannelConversationId': channelConversationId,
    'Notes': notes,
    'SoldAt': soldAt.toIso8601String(),
  };
}
