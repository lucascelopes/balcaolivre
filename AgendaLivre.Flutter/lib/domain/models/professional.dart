import 'id_generator.dart';
import 'json_helpers.dart';

class Professional {
  Professional({
    String? id,
    this.name = '',
    List<String>? segments,
    this.role = '',
    this.phone = '',
    this.email = '',
    this.document = '',
    this.commissionPercent = 0,
    this.notes = '',
    this.isActive = true,
    this.authUserId = '',
    this.accessStatus = '',
    this.permissionScope = '',
    this.seatKind = '',
    this.monthlyPriceCents = 0,
    this.requiresPasswordChange = false,
  }) : id = agendaIdOrGenerate(id),
       segments = List<String>.of(segments ?? const <String>[]);

  String id;
  String name;
  List<String> segments;
  String role;
  String phone;
  String email;
  String document;
  double commissionPercent;
  String notes;
  bool isActive;
  String authUserId;
  String accessStatus;
  String permissionScope;
  String seatKind;
  int monthlyPriceCents;
  bool requiresPasswordChange;

  String get segmentLine =>
      segments.isEmpty ? role : '$role | ${segments.join(', ')}';
  bool get hasAppAccess =>
      authUserId.trim().isNotEmpty &&
      accessStatus.trim().toLowerCase() == 'active';

  factory Professional.fromJson(JsonMap json) => Professional(
    id: jsonString(json, 'Id'),
    name: jsonString(json, 'Name'),
    segments: jsonStringList(json, 'Segments'),
    role: jsonString(json, 'Role'),
    phone: jsonString(json, 'Phone'),
    email: jsonString(json, 'Email'),
    document: jsonString(json, 'Document'),
    commissionPercent: jsonDouble(json, 'CommissionPercent'),
    notes: jsonString(json, 'Notes'),
    isActive: jsonBool(json, 'IsActive', fallback: true),
    authUserId: jsonString(json, 'AuthUserId'),
    accessStatus: jsonString(json, 'AccessStatus'),
    permissionScope: jsonString(json, 'PermissionScope'),
    seatKind: jsonString(json, 'SeatKind'),
    monthlyPriceCents: jsonInt(json, 'MonthlyPriceCents'),
    requiresPasswordChange: jsonBool(json, 'RequiresPasswordChange'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Name': name,
    'Segments': segments,
    'Role': role,
    'Phone': phone,
    'Email': email,
    'Document': document,
    'CommissionPercent': commissionPercent,
    'Notes': notes,
    'IsActive': isActive,
    'AuthUserId': authUserId,
    'AccessStatus': accessStatus,
    'PermissionScope': permissionScope,
    'SeatKind': seatKind,
    'MonthlyPriceCents': monthlyPriceCents,
    'RequiresPasswordChange': requiresPasswordChange,
  };

  @override
  String toString() => name;
}
