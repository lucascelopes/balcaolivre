import 'json_helpers.dart';

class AppointmentServiceLine {
  AppointmentServiceLine({
    this.serviceId = '',
    this.serviceName = '',
    this.segment = '',
    this.quantity = 1,
    this.durationMinutes = 30,
    this.unitPrice = 0,
  });

  String serviceId;
  String serviceName;
  String segment;
  int quantity;
  int durationMinutes;
  double unitPrice;

  double get total =>
      quantity.clamp(0, 0x7fffffff) * unitPrice.clamp(0, double.infinity);

  int get totalDurationMinutes =>
      quantity.clamp(0, 0x7fffffff) * durationMinutes.clamp(0, 0x7fffffff);

  factory AppointmentServiceLine.fromJson(JsonMap json) =>
      AppointmentServiceLine(
        serviceId: jsonString(json, 'ServiceId'),
        serviceName: jsonString(json, 'ServiceName'),
        segment: jsonString(json, 'Segment'),
        quantity: jsonInt(json, 'Quantity', fallback: 1),
        durationMinutes: jsonInt(json, 'DurationMinutes', fallback: 30),
        unitPrice: jsonDouble(json, 'UnitPrice'),
      );

  JsonMap toJson() => <String, dynamic>{
    'ServiceId': serviceId,
    'ServiceName': serviceName,
    'Segment': segment,
    'Quantity': quantity,
    'DurationMinutes': durationMinutes,
    'UnitPrice': unitPrice,
  };
}
