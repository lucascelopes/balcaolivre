import 'json_helpers.dart';

class AppointmentProductLine {
  AppointmentProductLine({
    this.productId = '',
    this.productName = '',
    this.quantity = 1,
    this.unitPrice = 0,
  });

  String productId;
  String productName;
  int quantity;
  double unitPrice;

  double get total =>
      quantity.clamp(0, 0x7fffffff) * unitPrice.clamp(0, double.infinity);

  factory AppointmentProductLine.fromJson(JsonMap json) =>
      AppointmentProductLine(
        productId: jsonString(json, 'ProductId'),
        productName: jsonString(json, 'ProductName'),
        quantity: jsonInt(json, 'Quantity', fallback: 1),
        unitPrice: jsonDouble(json, 'UnitPrice'),
      );

  JsonMap toJson() => <String, dynamic>{
    'ProductId': productId,
    'ProductName': productName,
    'Quantity': quantity,
    'UnitPrice': unitPrice,
  };
}
