import 'id_generator.dart';
import 'json_helpers.dart';

class ExpenseItem {
  ExpenseItem({
    String? id,
    this.description = '',
    this.category = '',
    this.supplier = '',
    this.paymentMethod = '',
    this.notes = '',
    this.value = 0,
    DateTime? date,
    this.isPaid = true,
  }) : id = agendaIdOrGenerate(id),
       date = date ?? DateTime.now();

  String id;
  String description;
  String category;
  String supplier;
  String paymentMethod;
  String notes;
  double value;
  DateTime date;
  bool isPaid;

  factory ExpenseItem.fromJson(JsonMap json) => ExpenseItem(
    id: jsonString(json, 'Id'),
    description: jsonString(json, 'Description'),
    category: jsonString(json, 'Category'),
    supplier: jsonString(json, 'Supplier'),
    paymentMethod: jsonString(json, 'PaymentMethod'),
    notes: jsonString(json, 'Notes'),
    value: jsonDouble(json, 'Value'),
    date: jsonDateTime(json, 'Date', fallback: DateTime.now()),
    isPaid: jsonBool(json, 'IsPaid', fallback: true),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Description': description,
    'Category': category,
    'Supplier': supplier,
    'PaymentMethod': paymentMethod,
    'Notes': notes,
    'Value': value,
    'Date': date.toIso8601String(),
    'IsPaid': isPaid,
  };
}
