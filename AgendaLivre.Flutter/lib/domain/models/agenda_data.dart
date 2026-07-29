import 'agenda_settings.dart';
import 'appointment.dart';
import 'cash_session.dart';
import 'channel_conversation.dart';
import 'channel_message.dart';
import 'customer.dart';
import 'customer_receivable.dart';
import 'expense_item.dart';
import 'json_helpers.dart';
import 'manual_payment.dart';
import 'product_item.dart';
import 'product_sale.dart';
import 'professional.dart';
import 'service_item.dart';
import 'whatsapp_message.dart';
import 'whatsapp_lead.dart';

class AgendaData {
  AgendaData({
    AgendaSettings? settings,
    List<ServiceItem>? services,
    List<Professional>? professionals,
    List<Customer>? customers,
    List<Appointment>? appointments,
    List<ProductItem>? products,
    List<ProductSale>? productSales,
    List<ManualPayment>? manualPayments,
    List<CustomerReceivable>? customerReceivables,
    List<ExpenseItem>? expenses,
    List<CashSession>? cashSessions,
    List<WhatsAppMessage>? whatsAppMessages,
    List<WhatsAppLead>? whatsAppLeads,
    List<ChannelConversation>? channelConversations,
    List<ChannelMessage>? channelMessages,
  }) : settings = settings ?? AgendaSettings(),
       services = List<ServiceItem>.of(services ?? const <ServiceItem>[]),
       professionals = List<Professional>.of(
         professionals ?? const <Professional>[],
       ),
       customers = List<Customer>.of(customers ?? const <Customer>[]),
       appointments = List<Appointment>.of(
         appointments ?? const <Appointment>[],
       ),
       products = List<ProductItem>.of(products ?? const <ProductItem>[]),
       productSales = List<ProductSale>.of(
         productSales ?? const <ProductSale>[],
       ),
       manualPayments = List<ManualPayment>.of(
         manualPayments ?? const <ManualPayment>[],
       ),
       customerReceivables = List<CustomerReceivable>.of(
         customerReceivables ?? const <CustomerReceivable>[],
       ),
       expenses = List<ExpenseItem>.of(expenses ?? const <ExpenseItem>[]),
       cashSessions = List<CashSession>.of(
         cashSessions ?? const <CashSession>[],
       ),
       whatsAppMessages = List<WhatsAppMessage>.of(
         whatsAppMessages ?? const <WhatsAppMessage>[],
       ),
       whatsAppLeads = List<WhatsAppLead>.of(
         whatsAppLeads ?? const <WhatsAppLead>[],
       ),
       channelConversations = List<ChannelConversation>.of(
         channelConversations ?? const <ChannelConversation>[],
       ),
       channelMessages = List<ChannelMessage>.of(
         channelMessages ?? const <ChannelMessage>[],
       );

  AgendaSettings settings;
  List<ServiceItem> services;
  List<Professional> professionals;
  List<Customer> customers;
  List<Appointment> appointments;
  List<ProductItem> products;
  List<ProductSale> productSales;
  List<ManualPayment> manualPayments;
  List<CustomerReceivable> customerReceivables;
  List<ExpenseItem> expenses;
  List<CashSession> cashSessions;
  List<WhatsAppMessage> whatsAppMessages;
  List<WhatsAppLead> whatsAppLeads;
  List<ChannelConversation> channelConversations;
  List<ChannelMessage> channelMessages;

  factory AgendaData.fromJson(JsonMap json) => AgendaData(
    settings: AgendaSettings.fromJson(jsonObject(json, 'Settings')),
    services: jsonObjectList(
      json,
      'Services',
    ).map(ServiceItem.fromJson).toList(),
    professionals: jsonObjectList(
      json,
      'Professionals',
    ).map(Professional.fromJson).toList(),
    customers: jsonObjectList(
      json,
      'Customers',
    ).map(Customer.fromJson).toList(),
    appointments: jsonObjectList(
      json,
      'Appointments',
    ).map(Appointment.fromJson).toList(),
    products: jsonObjectList(
      json,
      'Products',
    ).map(ProductItem.fromJson).toList(),
    productSales: jsonObjectList(
      json,
      'ProductSales',
    ).map(ProductSale.fromJson).toList(),
    manualPayments: jsonObjectList(
      json,
      'ManualPayments',
    ).map(ManualPayment.fromJson).toList(),
    customerReceivables: jsonObjectList(
      json,
      'CustomerReceivables',
    ).map(CustomerReceivable.fromJson).toList(),
    expenses: jsonObjectList(
      json,
      'Expenses',
    ).map(ExpenseItem.fromJson).toList(),
    cashSessions: jsonObjectList(
      json,
      'CashSessions',
    ).map(CashSession.fromJson).toList(),
    whatsAppMessages: jsonObjectList(
      json,
      'WhatsAppMessages',
    ).map(WhatsAppMessage.fromJson).toList(),
    whatsAppLeads: jsonObjectList(
      json,
      'WhatsAppLeads',
    ).map(WhatsAppLead.fromJson).toList(),
    channelConversations: jsonObjectList(
      json,
      'ChannelConversations',
    ).map(ChannelConversation.fromJson).toList(),
    channelMessages: jsonObjectList(
      json,
      'ChannelMessages',
    ).map(ChannelMessage.fromJson).toList(),
  );

  JsonMap toJson() => <String, dynamic>{
    'Settings': settings.toJson(),
    'Services': services.map((item) => item.toJson()).toList(),
    'Professionals': professionals.map((item) => item.toJson()).toList(),
    'Customers': customers.map((item) => item.toJson()).toList(),
    'Appointments': appointments.map((item) => item.toJson()).toList(),
    'Products': products.map((item) => item.toJson()).toList(),
    'ProductSales': productSales.map((item) => item.toJson()).toList(),
    'ManualPayments': manualPayments.map((item) => item.toJson()).toList(),
    'CustomerReceivables': customerReceivables
        .map((item) => item.toJson())
        .toList(),
    'Expenses': expenses.map((item) => item.toJson()).toList(),
    'CashSessions': cashSessions.map((item) => item.toJson()).toList(),
    'WhatsAppMessages': whatsAppMessages.map((item) => item.toJson()).toList(),
    'WhatsAppLeads': whatsAppLeads.map((item) => item.toJson()).toList(),
    'ChannelConversations': channelConversations
        .map((item) => item.toJson())
        .toList(),
    'ChannelMessages': channelMessages.map((item) => item.toJson()).toList(),
  };
}
