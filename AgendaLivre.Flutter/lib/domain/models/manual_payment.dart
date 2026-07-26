import 'id_generator.dart';
import 'json_helpers.dart';

class ManualPayment {
  ManualPayment({
    String? id,
    this.description = '',
    this.customerName = '',
    this.category = '',
    this.paymentMethod = '',
    this.paymentProvider = '',
    this.paymentReference = '',
    this.paymentStatus = '',
    this.notes = '',
    this.value = 0,
    DateTime? paidAt,
  }) : id = agendaIdOrGenerate(id),
       paidAt = paidAt ?? DateTime.now();

  String id;
  String description;
  String customerName;
  String category;
  String paymentMethod;
  String paymentProvider;
  String paymentReference;
  String paymentStatus;
  String notes;
  double value;
  DateTime paidAt;

  factory ManualPayment.fromJson(JsonMap json) => ManualPayment(
    id: jsonString(json, 'Id'),
    description: jsonString(json, 'Description'),
    customerName: jsonString(json, 'CustomerName'),
    category: jsonString(json, 'Category'),
    paymentMethod: jsonString(json, 'PaymentMethod'),
    paymentProvider: jsonString(json, 'PaymentProvider'),
    paymentReference: jsonString(json, 'PaymentReference'),
    paymentStatus: jsonString(json, 'PaymentStatus'),
    notes: jsonString(json, 'Notes'),
    value: jsonDouble(json, 'Value'),
    paidAt: jsonDateTime(json, 'PaidAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Description': description,
    'CustomerName': customerName,
    'Category': category,
    'PaymentMethod': paymentMethod,
    'PaymentProvider': paymentProvider,
    'PaymentReference': paymentReference,
    'PaymentStatus': paymentStatus,
    'Notes': notes,
    'Value': value,
    'PaidAt': paidAt.toIso8601String(),
  };
}
