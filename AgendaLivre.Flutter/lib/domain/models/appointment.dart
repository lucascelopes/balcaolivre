import 'appointment_product_line.dart';
import 'appointment_service_line.dart';
import 'appointment_status.dart';
import 'id_generator.dart';
import 'json_helpers.dart';

class Appointment {
  Appointment({
    String? id,
    this.segment = '',
    this.customerId = '',
    this.customerName = '',
    this.customerPhone = '',
    this.customerProfile = '',
    this.serviceId = '',
    this.serviceName = '',
    this.professionalId = '',
    this.professionalName = '',
    this.resourceName = '',
    DateTime? start,
    this.durationMinutes = 30,
    this.price = 0,
    this.status = AppointmentStatus.scheduled,
    this.serviceStartedAt,
    this.serviceElapsedSeconds = 0,
    this.serviceTimerPaused = false,
    List<AppointmentServiceLine>? serviceLines,
    List<AppointmentProductLine>? productLines,
    this.productSalesRecordedAt,
    this.paymentConfirmedAt,
    this.paymentMethod = '',
    this.paymentProvider = '',
    this.paymentReference = '',
    this.paymentStatus = '',
    this.notes = '',
    this.externalSource = '',
    this.externalReference = '',
    DateTime? createdAt,
    DateTime? updatedAt,
  }) : id = agendaIdOrGenerate(id),
       start = start ?? DateTime.now(),
       serviceLines = List<AppointmentServiceLine>.of(
         serviceLines ?? const <AppointmentServiceLine>[],
       ),
       productLines = List<AppointmentProductLine>.of(
         productLines ?? const <AppointmentProductLine>[],
       ),
       createdAt = createdAt ?? DateTime.now(),
       updatedAt = updatedAt ?? createdAt ?? DateTime.now();

  String id;
  String segment;
  String customerId;
  String customerName;
  String customerPhone;
  String customerProfile;
  String serviceId;
  String serviceName;
  String professionalId;
  String professionalName;
  String resourceName;
  DateTime start;
  int durationMinutes;
  double price;
  AppointmentStatus status;
  DateTime? serviceStartedAt;
  int serviceElapsedSeconds;
  bool serviceTimerPaused;
  List<AppointmentServiceLine> serviceLines;
  List<AppointmentProductLine> productLines;
  DateTime? productSalesRecordedAt;
  DateTime? paymentConfirmedAt;
  String paymentMethod;
  String paymentProvider;
  String paymentReference;
  String paymentStatus;
  String notes;
  String externalSource;
  String externalReference;
  DateTime createdAt;
  DateTime updatedAt;

  DateTime get end => start.add(Duration(minutes: durationMinutes));

  factory Appointment.fromJson(JsonMap json) => Appointment(
    id: jsonString(json, 'Id'),
    segment: jsonString(json, 'Segment'),
    customerId: jsonString(json, 'CustomerId'),
    customerName: jsonString(json, 'CustomerName', fallback: 'Cliente'),
    customerPhone: jsonString(json, 'CustomerPhone'),
    customerProfile: jsonString(json, 'CustomerProfile'),
    serviceId: jsonString(json, 'ServiceId'),
    serviceName: jsonString(json, 'ServiceName'),
    professionalId: jsonString(json, 'ProfessionalId'),
    professionalName: jsonString(json, 'ProfessionalName'),
    resourceName: jsonString(json, 'ResourceName'),
    start: jsonDateTime(json, 'Start', fallback: DateTime.now()),
    durationMinutes: jsonInt(json, 'DurationMinutes', fallback: 30),
    price: jsonDouble(json, 'Price'),
    status: appointmentStatusFromJson(jsonField(json, 'Status')),
    serviceStartedAt: jsonNullableDateTime(json, 'ServiceStartedAt'),
    serviceElapsedSeconds: jsonInt(json, 'ServiceElapsedSeconds'),
    serviceTimerPaused: jsonBool(json, 'ServiceTimerPaused'),
    serviceLines: jsonObjectList(
      json,
      'ServiceLines',
    ).map(AppointmentServiceLine.fromJson).toList(),
    productLines: jsonObjectList(
      json,
      'ProductLines',
    ).map(AppointmentProductLine.fromJson).toList(),
    productSalesRecordedAt: jsonNullableDateTime(
      json,
      'ProductSalesRecordedAt',
    ),
    paymentConfirmedAt: jsonNullableDateTime(json, 'PaymentConfirmedAt'),
    paymentMethod: jsonString(json, 'PaymentMethod'),
    paymentProvider: jsonString(json, 'PaymentProvider'),
    paymentReference: jsonString(json, 'PaymentReference'),
    paymentStatus: jsonString(json, 'PaymentStatus'),
    notes: jsonString(json, 'Notes'),
    externalSource: jsonString(json, 'ExternalSource'),
    externalReference: jsonString(json, 'ExternalReference'),
    createdAt: jsonDateTime(json, 'CreatedAt', fallback: DateTime.now()),
    updatedAt: jsonDateTime(json, 'UpdatedAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Segment': segment,
    'CustomerId': customerId,
    'CustomerName': customerName,
    'CustomerPhone': customerPhone,
    'CustomerProfile': customerProfile,
    'ServiceId': serviceId,
    'ServiceName': serviceName,
    'ProfessionalId': professionalId,
    'ProfessionalName': professionalName,
    'ResourceName': resourceName,
    'Start': start.toIso8601String(),
    'DurationMinutes': durationMinutes,
    'Price': price,
    'Status': status.jsonValue,
    'ServiceStartedAt': dateTimeToJson(serviceStartedAt),
    'ServiceElapsedSeconds': serviceElapsedSeconds,
    'ServiceTimerPaused': serviceTimerPaused,
    'ServiceLines': serviceLines.map((item) => item.toJson()).toList(),
    'ProductLines': productLines.map((item) => item.toJson()).toList(),
    'ProductSalesRecordedAt': dateTimeToJson(productSalesRecordedAt),
    'PaymentConfirmedAt': dateTimeToJson(paymentConfirmedAt),
    'PaymentMethod': paymentMethod,
    'PaymentProvider': paymentProvider,
    'PaymentReference': paymentReference,
    'PaymentStatus': paymentStatus,
    'Notes': notes,
    'ExternalSource': externalSource,
    'ExternalReference': externalReference,
    'CreatedAt': createdAt.toIso8601String(),
    'UpdatedAt': updatedAt.toIso8601String(),
  };
}
