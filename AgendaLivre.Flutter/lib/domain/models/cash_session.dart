import 'id_generator.dart';
import 'json_helpers.dart';

class CashSession {
  CashSession({
    String? id,
    this.operatorName = '',
    this.terminalName = 'PDV principal',
    this.openingBalance = 0,
    DateTime? openedAt,
    this.closingBalance,
    this.expectedClosingBalance = 0,
    this.closingDifference = 0,
    this.totalSales = 0,
    this.cashSales = 0,
    this.pixSales = 0,
    this.creditCardSales = 0,
    this.debitCardSales = 0,
    this.cardSales = 0,
    this.cashEntries = 0,
    this.cashWithdrawals = 0,
    this.appointmentCount = 0,
    this.completedAppointmentCount = 0,
    this.cancelledAppointmentCount = 0,
    this.noShowAppointmentCount = 0,
    this.serviceElapsedSeconds = 0,
    this.printSummaryOnClose = false,
    this.closedAt,
    this.notes = '',
  }) : id = agendaIdOrGenerate(id),
       openedAt = openedAt ?? DateTime.now();

  String id;
  String operatorName;
  String terminalName;
  double openingBalance;
  DateTime openedAt;
  double? closingBalance;
  double expectedClosingBalance;
  double closingDifference;
  double totalSales;
  double cashSales;
  double pixSales;
  double creditCardSales;
  double debitCardSales;
  double cardSales;
  double cashEntries;
  double cashWithdrawals;
  int appointmentCount;
  int completedAppointmentCount;
  int cancelledAppointmentCount;
  int noShowAppointmentCount;
  int serviceElapsedSeconds;
  bool printSummaryOnClose;
  DateTime? closedAt;
  String notes;

  bool get isOpen => closedAt == null;

  factory CashSession.fromJson(JsonMap json) => CashSession(
    id: jsonString(json, 'Id'),
    operatorName: jsonString(json, 'OperatorName'),
    terminalName: jsonString(json, 'TerminalName', fallback: 'PDV principal'),
    openingBalance: jsonDouble(json, 'OpeningBalance'),
    openedAt: jsonDateTime(json, 'OpenedAt', fallback: DateTime.now()),
    closingBalance: jsonField(json, 'ClosingBalance') == null
        ? null
        : jsonDouble(json, 'ClosingBalance'),
    expectedClosingBalance: jsonDouble(json, 'ExpectedClosingBalance'),
    closingDifference: jsonDouble(json, 'ClosingDifference'),
    totalSales: jsonDouble(json, 'TotalSales'),
    cashSales: jsonDouble(json, 'CashSales'),
    pixSales: jsonDouble(json, 'PixSales'),
    creditCardSales: jsonDouble(json, 'CreditCardSales'),
    debitCardSales: jsonDouble(json, 'DebitCardSales'),
    cardSales: jsonDouble(json, 'CardSales'),
    cashEntries: jsonDouble(json, 'CashEntries'),
    cashWithdrawals: jsonDouble(json, 'CashWithdrawals'),
    appointmentCount: jsonInt(json, 'AppointmentCount'),
    completedAppointmentCount: jsonInt(json, 'CompletedAppointmentCount'),
    cancelledAppointmentCount: jsonInt(json, 'CancelledAppointmentCount'),
    noShowAppointmentCount: jsonInt(json, 'NoShowAppointmentCount'),
    serviceElapsedSeconds: jsonInt(json, 'ServiceElapsedSeconds'),
    printSummaryOnClose: jsonBool(json, 'PrintSummaryOnClose'),
    closedAt: jsonNullableDateTime(json, 'ClosedAt'),
    notes: jsonString(json, 'Notes'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'OperatorName': operatorName,
    'TerminalName': terminalName,
    'OpeningBalance': openingBalance,
    'OpenedAt': openedAt.toIso8601String(),
    'ClosingBalance': closingBalance,
    'ExpectedClosingBalance': expectedClosingBalance,
    'ClosingDifference': closingDifference,
    'TotalSales': totalSales,
    'CashSales': cashSales,
    'PixSales': pixSales,
    'CreditCardSales': creditCardSales,
    'DebitCardSales': debitCardSales,
    'CardSales': cardSales,
    'CashEntries': cashEntries,
    'CashWithdrawals': cashWithdrawals,
    'AppointmentCount': appointmentCount,
    'CompletedAppointmentCount': completedAppointmentCount,
    'CancelledAppointmentCount': cancelledAppointmentCount,
    'NoShowAppointmentCount': noShowAppointmentCount,
    'ServiceElapsedSeconds': serviceElapsedSeconds,
    'PrintSummaryOnClose': printSummaryOnClose,
    'ClosedAt': dateTimeToJson(closedAt),
    'Notes': notes,
  };
}
