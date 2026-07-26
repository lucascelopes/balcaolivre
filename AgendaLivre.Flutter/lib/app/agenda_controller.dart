import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../domain/models/models.dart';
import '../domain/repositories/agenda_repository.dart';
import '../services/instagram_service.dart';
import '../services/mercado_pago_service.dart';

enum AgendaPage {
  home,
  agenda,
  finance,
  reports,
  establishment,
  marketing,
  settings,
}

enum AgendaViewMode { board, list, week }

class AgendaController extends ChangeNotifier {
  AgendaController(
    this._repository, {
    Future<void> Function()? onLogout,
    this.instagramService,
    this.mercadoPagoService,
    this.authenticatedEmail = '',
  }) : _onLogout = onLogout {
    final repository = _repository;
    if (repository is Listenable) {
      (repository as Listenable).addListener(_onRepositoryChanged);
    }
  }

  final AgendaRepository _repository;
  final Future<void> Function()? _onLogout;
  final InstagramService? instagramService;
  final MercadoPagoService? mercadoPagoService;
  final String authenticatedEmail;

  AgendaData data = AgendaData();
  AgendaPage page = AgendaPage.home;
  AgendaViewMode agendaMode = AgendaViewMode.board;
  DateTime selectedDate = _dateOnly(DateTime.now());
  String searchQuery = '';
  bool loading = true;
  String? loadError;
  int _dataMutationGeneration = 0;
  bool _resolvingSyncConflict = false;

  bool get hasAuthenticatedSession => _onLogout != null;

  AgendaSyncRepository? get _syncRepository =>
      _repository is AgendaSyncRepository
      ? _repository as AgendaSyncRepository
      : null;

  bool get hasSyncConflict => _syncRepository?.hasConflict ?? false;
  bool get isSyncing => _syncRepository?.isSyncing ?? false;
  String? get syncMessage => _syncRepository?.syncMessage;

  String? get trialStatusLabel {
    final repository = _syncRepository;
    if (!hasAuthenticatedSession ||
        repository == null ||
        !repository.hasTrialStatus) {
      return null;
    }
    if (!repository.trialActive) return 'Teste de 7 dias expirado';
    final days = repository.trialDaysRemaining.clamp(0, 7);
    if (days == 0) return 'Teste: último dia';
    if (days == 1) return 'Teste: 1 dia restante';
    return 'Teste: $days dias restantes';
  }

  bool get isTrialExpired {
    final repository = _syncRepository;
    return hasAuthenticatedSession &&
        repository != null &&
        repository.hasTrialStatus &&
        !repository.trialActive;
  }

  Future<void> initialize() async {
    loading = true;
    loadError = null;
    notifyListeners();
    try {
      data = await _repository.loadOrCreate();
      _normalize();
      await _resolveSyncConflictAutomatically();
    } catch (error) {
      loadError = 'Não foi possível abrir os dados locais: $error';
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  bool get needsOnboarding =>
      !data.settings.onboardingCompleted ||
      data.settings.businessSegment.trim().isEmpty;

  String get businessName {
    final value = data.settings.businessName.trim();
    return value.isEmpty ? 'Balcão Livre' : value;
  }

  String get accountName {
    final value = data.settings.accountFullName.trim();
    return value.isEmpty ? businessName : value;
  }

  String get accountEmail {
    final authenticated = authenticatedEmail.trim();
    if (authenticated.isNotEmpty) return authenticated;
    return data.settings.accountEmail.trim();
  }

  String get profileSubtitle {
    final trial = trialStatusLabel;
    final identity = authenticatedEmail.trim();
    if (identity.isEmpty) return trial ?? accountName;
    if (trial == null) return identity;
    return '$identity | $trial';
  }

  List<Appointment> get appointmentsForSelectedDate {
    final query = searchQuery.trim().toLowerCase();
    return data.appointments.where((item) {
      if (!_isSameDay(item.start, selectedDate)) return false;
      if (query.isEmpty) return true;
      return <String>[
        item.customerName,
        item.customerPhone,
        item.customerProfile,
        item.serviceName,
        item.professionalName,
        item.resourceName,
        item.notes,
      ].any((value) => value.toLowerCase().contains(query));
    }).toList()..sort((a, b) => a.start.compareTo(b.start));
  }

  List<Appointment> appointmentsBetween(DateTime start, DateTime end) =>
      data.appointments
          .where(
            (item) => !item.start.isBefore(start) && item.start.isBefore(end),
          )
          .toList()
        ..sort((a, b) => a.start.compareTo(b.start));

  List<Appointment> get openAppointments => data.appointments
      .where(
        (item) => const {
          AppointmentStatus.scheduled,
          AppointmentStatus.confirmed,
          AppointmentStatus.waiting,
          AppointmentStatus.inService,
        }.contains(item.status),
      )
      .toList();

  List<Professional> get activeProfessionals =>
      data.professionals.where((item) => item.isActive).toList();

  List<ServiceItem> get activeServices =>
      data.services.where((item) => item.isActive).toList();

  double revenueBetween(DateTime start, DateTime end) {
    final receivableAppointmentIds = data.customerReceivables
        .where((item) => item.status.trim().toLowerCase() != 'cancelled')
        .map((item) => item.appointmentId.trim().toLowerCase())
        .where((id) => id.isNotEmpty)
        .toSet();
    final services = data.appointments
        .where((item) {
          final paidAt = item.paymentConfirmedAt;
          return paidAt != null &&
              !paidAt.isBefore(start) &&
              paidAt.isBefore(end) &&
              !receivableAppointmentIds.contains(item.id.trim().toLowerCase());
        })
        .fold<double>(0, (sum, item) => sum + item.price);
    final customerAccounts = data.customerReceivables
        .where((item) {
          final paidAt = item.paidAt;
          return item.status.trim().toLowerCase() == 'paid' &&
              paidAt != null &&
              !paidAt.isBefore(start) &&
              paidAt.isBefore(end);
        })
        .fold<double>(0, (sum, item) => sum + item.originalValue);
    final products = data.productSales
        .where(
          (item) => !item.soldAt.isBefore(start) && item.soldAt.isBefore(end),
        )
        .fold<double>(0, (sum, item) => sum + item.total);
    final manual = data.manualPayments
        .where(
          (item) => !item.paidAt.isBefore(start) && item.paidAt.isBefore(end),
        )
        .fold<double>(0, (sum, item) => sum + item.value);
    return services + customerAccounts + products + manual;
  }

  double expensesBetween(DateTime start, DateTime end) => data.expenses
      .where((item) => !item.date.isBefore(start) && item.date.isBefore(end))
      .fold<double>(0, (sum, item) => sum + item.value);

  void navigate(AgendaPage value) {
    if (page == value) return;
    page = value;
    notifyListeners();
  }

  void selectDate(DateTime value) {
    selectedDate = _dateOnly(value);
    notifyListeners();
  }

  void setSearch(String value) {
    if (searchQuery == value) return;
    searchQuery = value;
    notifyListeners();
  }

  void setAgendaMode(AgendaViewMode value) {
    if (agendaMode == value) return;
    agendaMode = value;
    notifyListeners();
  }

  Future<String?> saveAppointment(Appointment appointment) async {
    appointment.durationMinutes = appointment.durationMinutes.clamp(5, 480);
    appointment.price = appointment.price < 0 ? 0 : appointment.price;
    appointment.updatedAt = DateTime.now();

    final index = data.appointments.indexWhere(
      (item) => item.id == appointment.id,
    );
    final existing = index < 0 ? null : data.appointments[index];
    final scheduleUnchanged =
        existing != null &&
        existing.start.isAtSameMomentAs(appointment.start) &&
        existing.end.isAtSameMomentAs(appointment.end);

    final businessWindowError = validateBusinessWindow(
      appointment.start,
      appointment.end,
    );
    if (businessWindowError != null && !scheduleUnchanged) {
      return businessWindowError;
    }

    final conflict = _appointmentConflict(appointment);
    if (conflict != null) {
      return 'Conflito com ${conflict.customerName} às '
          '${conflict.start.hour.toString().padLeft(2, '0')}:'
          '${conflict.start.minute.toString().padLeft(2, '0')}.';
    }

    if (index < 0) {
      data.appointments.add(appointment);
    } else {
      data.appointments[index] = appointment;
    }
    if (appointment.status != AppointmentStatus.blocked &&
        appointment.customerName.trim().isNotEmpty) {
      _upsertCustomerFromAppointment(appointment);
    }
    await _persist();
    return null;
  }

  Future<void> deleteAppointment(String id) async {
    data.appointments.removeWhere((item) => item.id == id);
    await _persist();
  }

  Future<String?> setAppointmentStatus(
    Appointment appointment,
    AppointmentStatus target,
  ) async {
    final current = appointment.status;
    final allowed = switch (target) {
      AppointmentStatus.confirmed => current == AppointmentStatus.scheduled,
      AppointmentStatus.waiting =>
        current == AppointmentStatus.scheduled ||
            current == AppointmentStatus.confirmed,
      AppointmentStatus.inService =>
        current == AppointmentStatus.confirmed ||
            current == AppointmentStatus.waiting,
      AppointmentStatus.done =>
        current == AppointmentStatus.waiting ||
            current == AppointmentStatus.inService,
      AppointmentStatus.cancelled || AppointmentStatus.noShow => !const {
        AppointmentStatus.done,
        AppointmentStatus.cancelled,
        AppointmentStatus.noShow,
      }.contains(current),
      AppointmentStatus.scheduled || AppointmentStatus.blocked => true,
    };
    if (!allowed) return 'Essa mudança de status não é permitida.';
    appointment.status = target;
    appointment.updatedAt = DateTime.now();
    await _persist();
    return null;
  }

  Duration appointmentServiceElapsed(Appointment appointment, {DateTime? now}) {
    var seconds = appointment.serviceElapsedSeconds.clamp(0, 0x7fffffff);
    final startedAt = appointment.serviceStartedAt;
    if (startedAt != null && !appointment.serviceTimerPaused) {
      seconds += (now ?? DateTime.now())
          .difference(startedAt)
          .inSeconds
          .clamp(0, 0x7fffffff);
    }
    return Duration(seconds: seconds);
  }

  Future<String?> toggleAppointmentServiceTimer(
    Appointment appointment, {
    DateTime? now,
  }) async {
    final current = data.appointments
        .where((item) => item.id == appointment.id)
        .firstOrNull;
    if (current == null) return 'O atendimento não está mais disponível.';
    if (const {
      AppointmentStatus.done,
      AppointmentStatus.cancelled,
      AppointmentStatus.noShow,
      AppointmentStatus.blocked,
    }.contains(current.status)) {
      return 'Esse atendimento não pode ter o tempo alterado.';
    }

    final instant = now ?? DateTime.now();
    final startedAt = current.serviceStartedAt;
    if (startedAt != null && !current.serviceTimerPaused) {
      current
        ..serviceElapsedSeconds += instant
            .difference(startedAt)
            .inSeconds
            .clamp(0, 0x7fffffff)
        ..serviceStartedAt = null
        ..serviceTimerPaused = true;
    } else {
      current
        ..serviceStartedAt = instant
        ..serviceTimerPaused = false
        ..status = AppointmentStatus.inService;
    }
    current.updatedAt = instant;
    await _persist();
    return null;
  }

  Future<String?> finishAppointmentService(
    Appointment appointment, {
    DateTime? now,
  }) async {
    final current = data.appointments
        .where((item) => item.id == appointment.id)
        .firstOrNull;
    if (current == null) return 'O atendimento não está mais disponível.';
    if (const {
      AppointmentStatus.cancelled,
      AppointmentStatus.noShow,
      AppointmentStatus.blocked,
    }.contains(current.status)) {
      return 'Esse atendimento não pode ser finalizado.';
    }

    final instant = now ?? DateTime.now();
    final startedAt = current.serviceStartedAt;
    if (startedAt != null && !current.serviceTimerPaused) {
      current.serviceElapsedSeconds += instant
          .difference(startedAt)
          .inSeconds
          .clamp(0, 0x7fffffff);
    }
    current
      ..serviceStartedAt = null
      ..serviceTimerPaused = false
      ..status = AppointmentStatus.done
      ..updatedAt = instant;
    await _persist();
    return null;
  }

  double pdvAppointmentTotal(Appointment appointment) {
    final products = appointment.productLines.fold<double>(
      0,
      (sum, line) => sum + line.total,
    );
    return appointment.price.clamp(0, double.infinity) + products;
  }

  Future<String?> savePdvAppointmentLines(
    Appointment appointment, {
    required Iterable<AppointmentServiceLine> serviceLines,
    required Iterable<AppointmentProductLine> productLines,
  }) async {
    final current = data.appointments
        .where((item) => item.id == appointment.id)
        .firstOrNull;
    if (current == null) return 'O atendimento não está mais disponível.';
    if (current.paymentConfirmedAt != null) {
      return 'Os itens desse atendimento já foram recebidos.';
    }

    final normalizedServices = serviceLines
        .where((line) => line.quantity > 0)
        .map(
          (line) => AppointmentServiceLine(
            serviceId: line.serviceId.trim(),
            serviceName: line.serviceName.trim(),
            segment: line.segment.trim(),
            quantity: line.quantity.clamp(1, 1000),
            durationMinutes: line.durationMinutes.clamp(1, 1440),
            unitPrice: line.unitPrice.clamp(0, double.infinity),
          ),
        )
        .toList();
    final normalizedProducts = productLines
        .where((line) => line.quantity > 0)
        .map(
          (line) => AppointmentProductLine(
            productId: line.productId.trim(),
            productName: line.productName.trim(),
            quantity: line.quantity.clamp(1, 1000),
            unitPrice: line.unitPrice.clamp(0, double.infinity),
          ),
        )
        .toList();

    for (final line in normalizedProducts) {
      final product = data.products
          .where((item) => item.id == line.productId && item.isActive)
          .firstOrNull;
      if (product == null) {
        return 'O produto ${line.productName} não está mais disponível.';
      }
      if (line.quantity > product.stockQuantity) {
        return 'Estoque insuficiente para ${product.name}.';
      }
    }

    final next = Appointment.fromJson(current.toJson());
    next
      ..serviceLines = normalizedServices
      ..productLines = normalizedProducts;
    final activeServices = normalizedServices.where(
      (line) => line.serviceName.isNotEmpty,
    );
    next
      ..serviceId = activeServices.firstOrNull?.serviceId ?? ''
      ..serviceName = activeServices.isEmpty
          ? 'Sem serviço'
          : activeServices
                .map(
                  (line) => line.quantity > 1
                      ? '${line.serviceName} (${line.quantity}x)'
                      : line.serviceName,
                )
                .join(' + ')
      ..segment = activeServices.firstOrNull?.segment ?? next.segment
      ..price = normalizedServices.fold<double>(
        0,
        (sum, line) => sum + line.total,
      )
      ..durationMinutes = normalizedServices.isEmpty
          ? next.durationMinutes.clamp(1, 1440)
          : normalizedServices
                .fold<int>(0, (sum, line) => sum + line.totalDurationMinutes)
                .clamp(1, 1440)
      ..updatedAt = DateTime.now();
    return saveAppointment(next);
  }

  CustomerReceivable? openCustomerReceivableForAppointment(
    String appointmentId,
  ) => data.customerReceivables
      .where(
        (item) =>
            item.appointmentId.toLowerCase() == appointmentId.toLowerCase() &&
            item.status.toLowerCase() != 'cancelled',
      )
      .firstOrNull;

  bool appointmentHasRegisteredCharge(Appointment appointment) =>
      appointment.paymentConfirmedAt != null ||
      openCustomerReceivableForAppointment(appointment.id) != null;

  Future<String?> confirmAppointmentPayment(
    Appointment appointment, {
    required String paymentMethod,
    String paymentProvider = 'Manual',
    String paymentReference = '',
    String paymentStatus = 'approved',
    DateTime? confirmedAt,
  }) async {
    final current = data.appointments
        .where((item) => item.id == appointment.id)
        .firstOrNull;
    if (current == null) {
      return 'O atendimento não está mais disponível.';
    }
    final stateError = _appointmentChargeStateError(current);
    if (stateError != null) return stateError;

    final now = confirmedAt ?? DateTime.now();
    current
      ..status = AppointmentStatus.done
      ..paymentConfirmedAt = now
      ..paymentMethod = paymentMethod.trim()
      ..paymentProvider = paymentProvider.trim()
      ..paymentReference = paymentReference.trim().isEmpty
          ? 'manual_${now.millisecondsSinceEpoch}'
          : paymentReference.trim()
      ..paymentStatus = paymentStatus.trim().isEmpty
          ? 'approved'
          : paymentStatus.trim()
      ..updatedAt = now;
    await _persist();
    return null;
  }

  Future<String?> confirmPdvAppointmentPayment(
    Appointment appointment, {
    required String paymentMethod,
    String paymentProvider = 'Manual',
    String paymentReference = '',
    String paymentStatus = 'approved',
    DateTime? confirmedAt,
  }) async {
    final current = data.appointments
        .where((item) => item.id == appointment.id)
        .firstOrNull;
    if (current == null) {
      return 'O atendimento não está mais disponível.';
    }
    final stateError = _appointmentChargeStateError(current);
    if (stateError != null) return stateError;

    for (final line in current.productLines.where(
      (item) => item.quantity > 0,
    )) {
      final product = data.products
          .where((item) => item.id == line.productId && item.isActive)
          .firstOrNull;
      if (product == null) {
        return 'O produto ${line.productName} não está mais disponível.';
      }
      if (line.quantity > product.stockQuantity) {
        return 'Estoque insuficiente para ${product.name}.';
      }
    }

    final now = confirmedAt ?? DateTime.now();
    final method = paymentMethod.trim();
    final provider = paymentProvider.trim().isEmpty
        ? 'Manual'
        : paymentProvider.trim();
    final reference = paymentReference.trim().isEmpty
        ? 'manual_${now.millisecondsSinceEpoch}'
        : paymentReference.trim();
    final status = paymentStatus.trim().isEmpty
        ? 'approved'
        : paymentStatus.trim();
    current
      ..status = AppointmentStatus.done
      ..paymentConfirmedAt = now
      ..paymentMethod = method
      ..paymentProvider = provider
      ..paymentReference = reference
      ..paymentStatus = status
      ..updatedAt = now;

    if (current.productSalesRecordedAt == null) {
      for (final line in current.productLines.where(
        (item) => item.quantity > 0,
      )) {
        data.productSales.add(
          ProductSale(
            productId: line.productId,
            productName: line.productName,
            customerName: current.customerName,
            quantity: line.quantity,
            unitPrice: line.unitPrice,
            paymentMethod: method,
            paymentProvider: provider,
            paymentReference: reference,
            paymentStatus: status,
            notes: 'Atendimento ${current.id}',
            soldAt: now,
          ),
        );
        final product = data.products
            .where((item) => item.id == line.productId)
            .firstOrNull;
        if (product != null) {
          product.stockQuantity = (product.stockQuantity - line.quantity).clamp(
            0,
            0x7fffffff,
          );
        }
      }
      current.productSalesRecordedAt = now;
    }

    await _persist();
    return null;
  }

  Future<String?> addAppointmentToCustomerAccount(
    Appointment appointment, {
    DateTime? openedAt,
  }) async {
    final current = data.appointments
        .where((item) => item.id == appointment.id)
        .firstOrNull;
    if (current == null) {
      return 'O atendimento não está mais disponível.';
    }
    final stateError = _appointmentChargeStateError(current);
    if (stateError != null) return stateError;

    final customer = _resolveAppointmentCustomer(current);
    if (customer == null) {
      return 'Cadastre ou selecione um cliente com nome e telefone únicos antes de adicionar o valor à conta.';
    }

    final now = openedAt ?? DateTime.now();
    final value = current.price < 0 ? 0.0 : current.price;
    final receivable = CustomerReceivable(
      customerId: customer.id,
      customerName: customer.name,
      appointmentId: current.id,
      description: current.serviceName.trim().isEmpty
          ? 'Atendimento'
          : current.serviceName.trim(),
      originalValue: value,
      remainingValue: value,
      status: 'open',
      openedAt: now,
      updatedAt: now,
      paymentProvider: 'customer_account',
      paymentStatus: 'pending',
    );

    current
      ..customerId = customer.id
      ..status = AppointmentStatus.done
      ..paymentConfirmedAt = null
      ..paymentMethod = 'Conta do cliente'
      ..paymentProvider = 'customer_account'
      ..paymentReference = receivable.id
      ..paymentStatus = 'pending'
      ..updatedAt = now;
    data.customerReceivables.add(receivable);
    await _persist();
    return null;
  }

  String? _appointmentChargeStateError(Appointment appointment) {
    if (const {
      AppointmentStatus.cancelled,
      AppointmentStatus.noShow,
      AppointmentStatus.blocked,
    }.contains(appointment.status)) {
      return 'Esse atendimento foi encerrado sem cobrança.';
    }
    if (appointmentHasRegisteredCharge(appointment)) {
      return 'Esse atendimento já possui um pagamento ou saldo registrado.';
    }
    return null;
  }

  Customer? _resolveAppointmentCustomer(Appointment appointment) {
    final customerId = appointment.customerId.trim();
    if (customerId.isNotEmpty) {
      final linked = data.customers
          .where((item) => item.id.toLowerCase() == customerId.toLowerCase())
          .firstOrNull;
      if (linked != null) return linked;
    }

    final phone = _normalizedBrazilPhone(appointment.customerPhone);
    if (phone.isNotEmpty) {
      final matches = data.customers
          .where((item) => _normalizedBrazilPhone(item.phone) == phone)
          .toList(growable: false);
      if (matches.length == 1) return matches.single;
    }

    final name = appointment.customerName.trim().toLowerCase();
    if (name.isEmpty) return null;
    final matches = data.customers
        .where((item) => item.name.trim().toLowerCase() == name)
        .toList(growable: false);
    return matches.length == 1 ? matches.single : null;
  }

  Future<void> saveCustomer(Customer customer) async {
    final index = data.customers.indexWhere((item) => item.id == customer.id);
    if (index < 0) {
      data.customers.add(customer);
    } else {
      data.customers[index] = customer;
    }
    await _persist();
  }

  Future<void> saveProfessional(Professional professional) async {
    professional.commissionPercent = professional.commissionPercent.clamp(
      0,
      100,
    );
    final index = data.professionals.indexWhere(
      (item) => item.id == professional.id,
    );
    if (index < 0) {
      data.professionals.add(professional);
    } else {
      data.professionals[index] = professional;
    }
    await _persist();
  }

  Future<void> saveService(ServiceItem service) async {
    service.durationMinutes = service.durationMinutes.clamp(5, 480);
    service.preparationMinutes = service.preparationMinutes.clamp(0, 240);
    service.bufferMinutes = service.bufferMinutes.clamp(0, 240);
    service.price = service.price < 0 ? 0 : service.price;
    service.commissionPercent = service.commissionPercent.clamp(0, 100);
    final index = data.services.indexWhere((item) => item.id == service.id);
    if (index < 0) {
      data.services.add(service);
    } else {
      data.services[index] = service;
    }
    await _persist();
  }

  Future<String?> saveProduct(ProductItem product) async {
    product
      ..name = product.name.trim()
      ..category = product.category.trim()
      ..sku = product.sku.trim()
      ..supplier = product.supplier.trim()
      ..costPrice = product.costPrice < 0 ? 0 : product.costPrice
      ..price = product.price < 0 ? 0 : product.price
      ..stockQuantity = product.stockQuantity < 0 ? 0 : product.stockQuantity
      ..minimumStock = product.minimumStock < 0 ? 0 : product.minimumStock
      ..notes = product.notes.trim();
    if (product.name.isEmpty) return 'Informe o nome do produto.';
    final duplicated = data.products.any(
      (item) =>
          item.id != product.id &&
          item.name.trim().toLowerCase() == product.name.toLowerCase(),
    );
    if (duplicated) return 'Já existe um produto com esse nome.';

    final index = data.products.indexWhere((item) => item.id == product.id);
    if (index < 0) {
      data.products.add(product);
    } else {
      data.products[index] = product;
    }
    await _persist();
    return null;
  }

  Future<String?> registerProductSale(ProductSale sale) async {
    final product = data.products
        .where((item) => item.id == sale.productId)
        .firstOrNull;
    if (product == null || !product.isActive) {
      return 'Selecione um produto ativo para registrar a venda.';
    }

    sale.quantity = sale.quantity.clamp(1, 100000);
    sale.discount = sale.discount < 0 ? 0 : sale.discount;
    final gross = product.price * sale.quantity;
    if (sale.discount > gross) {
      return 'O desconto não pode ser maior que o total da venda.';
    }

    sale
      ..productName = product.name
      ..unitPrice = product.price
      ..customerName = sale.customerName.trim()
      ..paymentMethod = sale.paymentMethod.trim()
      ..paymentProvider = sale.paymentProvider.trim()
      ..paymentReference = sale.paymentReference.trim()
      ..paymentStatus = sale.paymentStatus.trim()
      ..notes = sale.notes.trim();

    data.productSales.add(sale);
    product.stockQuantity = (product.stockQuantity - sale.quantity).clamp(
      0,
      0x7fffffff,
    );
    await _persist();
    return null;
  }

  List<CustomerReceivable> openCustomerReceivables({
    String customerId = '',
    String customerName = '',
  }) {
    final normalizedId = customerId.trim().toLowerCase();
    final normalizedName = customerName.trim().toLowerCase();
    final items = data.customerReceivables.where((item) {
      if (item.status.trim().toLowerCase() != 'open' ||
          item.remainingValue <= 0) {
        return false;
      }
      if (normalizedId.isNotEmpty) {
        return item.customerId.trim().toLowerCase() == normalizedId;
      }
      return normalizedName.isNotEmpty &&
          item.customerId.trim().isEmpty &&
          item.customerName.trim().toLowerCase() == normalizedName;
    }).toList()..sort((a, b) => a.openedAt.compareTo(b.openedAt));
    return items;
  }

  Future<String?> settleCustomerReceivables(
    Iterable<String> receivableIds, {
    required String paymentMethod,
    String paymentProvider = 'Manual',
    String paymentReference = '',
    String paymentStatus = 'approved',
    DateTime? paidAt,
  }) async {
    final requested = receivableIds
        .map((item) => item.trim().toLowerCase())
        .where((item) => item.isNotEmpty)
        .toSet();
    final currentItems = data.customerReceivables
        .where(
          (item) =>
              requested.contains(item.id.trim().toLowerCase()) &&
              item.status.trim().toLowerCase() == 'open' &&
              item.remainingValue > 0,
        )
        .toList(growable: false);
    if (currentItems.isEmpty) {
      return 'Esse saldo já foi quitado ou alterado.';
    }

    final now = paidAt ?? DateTime.now();
    final method = paymentMethod.trim().isEmpty
        ? 'Pagamento recebido'
        : paymentMethod.trim();
    final provider = paymentProvider.trim().isEmpty
        ? 'Manual'
        : paymentProvider.trim();
    final reference = paymentReference.trim().isEmpty
        ? 'manual_${now.millisecondsSinceEpoch}'
        : paymentReference.trim();
    final status = paymentStatus.trim().isEmpty
        ? 'approved'
        : paymentStatus.trim();

    for (final item in currentItems) {
      item
        ..remainingValue = 0
        ..status = 'paid'
        ..paidAt = now
        ..updatedAt = now
        ..paymentMethod = method
        ..paymentProvider = provider
        ..paymentReference = reference
        ..paymentStatus = status;

      final appointment = data.appointments
          .where(
            (candidate) =>
                candidate.id.toLowerCase() ==
                item.appointmentId.trim().toLowerCase(),
          )
          .firstOrNull;
      appointment
        ?..paymentConfirmedAt = now
        ..paymentMethod = method
        ..paymentProvider = provider
        ..paymentReference = reference
        ..paymentStatus = status
        ..updatedAt = now;
    }

    await _persist();
    return null;
  }

  Future<void> addPayment(ManualPayment payment) async {
    payment.value = payment.value < 0 ? 0 : payment.value;
    data.manualPayments.add(payment);
    await _persist();
  }

  Future<void> addExpense(ExpenseItem expense) async {
    expense.value = expense.value < 0 ? 0 : expense.value;
    data.expenses.add(expense);
    await _persist();
  }

  Future<void> addWhatsAppMessage(WhatsAppMessage message) async {
    data.whatsAppMessages.add(message);
    data.whatsAppMessages.sort((a, b) => a.createdAt.compareTo(b.createdAt));
    if (data.whatsAppMessages.length > 1000) {
      data.whatsAppMessages = data.whatsAppMessages
          .skip(data.whatsAppMessages.length - 1000)
          .toList();
    }
    data.settings.whatsAppLastMessageAt = message.createdAt;
    await _persist();
  }

  Future<void> updateSettings(
    void Function(AgendaSettings settings) update,
  ) async {
    update(data.settings);
    _normalizeSettings(data.settings);
    await _persist();
  }

  bool isConfiguredWorkday(DateTime date) =>
      data.settings.workdays.contains(date.weekday % 7);

  bool overlapsConfiguredBreak(DateTime start, DateTime end) {
    final settings = data.settings;
    if (!settings.workdayBreakEnabled) return false;
    final breakStart = DateTime(
      start.year,
      start.month,
      start.day,
      settings.workdayBreakStartHour,
    );
    final breakEnd = DateTime(
      start.year,
      start.month,
      start.day,
      settings.workdayBreakEndHour,
    );
    return start.isBefore(breakEnd) && end.isAfter(breakStart);
  }

  String? validateBusinessWindow(DateTime start, DateTime end) {
    final settings = data.settings;
    if (!isConfiguredWorkday(start)) {
      return 'O estabelecimento não atende no dia selecionado.';
    }
    final workdayStart = DateTime(
      start.year,
      start.month,
      start.day,
      settings.workdayStartHour,
    );
    final workdayEnd = DateTime(
      start.year,
      start.month,
      start.day,
      settings.workdayEndHour,
    );
    if (start.isBefore(workdayStart) || end.isAfter(workdayEnd)) {
      final from = settings.workdayStartHour.toString().padLeft(2, '0');
      final to = settings.workdayEndHour.toString().padLeft(2, '0');
      return 'O atendimento precisa ficar dentro do expediente: '
          '$from:00 até $to:00.';
    }
    if (overlapsConfiguredBreak(start, end)) {
      final from = settings.workdayBreakStartHour.toString().padLeft(2, '0');
      final to = settings.workdayBreakEndHour.toString().padLeft(2, '0');
      return 'Esse horário coincide com o intervalo: $from:00 às $to:00.';
    }
    return null;
  }

  Future<void> completeOnboarding({
    required String accountName,
    required String phone,
    required String email,
    required String businessName,
    required String segment,
    required String themeId,
    required String teamSize,
    required String objective,
    required String postalCode,
    required String neighborhood,
    required String street,
    required String number,
    required String complement,
  }) async {
    final settings = data.settings;
    final verifiedEmail = authenticatedEmail.trim();
    settings
      ..accountFullName = accountName.trim()
      ..accountPhone = phone.trim()
      ..accountEmail = verifiedEmail.isEmpty ? email.trim() : verifiedEmail
      ..businessName = businessName.trim()
      ..businessPhone = phone.trim()
      ..businessSegment = segment.trim()
      ..themeId = themeId
      ..professionalCountRange = teamSize
      ..mainObjective = objective
      ..postalCode = postalCode.trim()
      ..neighborhood = neighborhood.trim()
      ..street = street.trim()
      ..addressNumber = number.trim()
      ..addressComplement = complement.trim()
      ..businessAddress = _buildOnboardingAddress(
        street: street,
        number: number,
        neighborhood: neighborhood,
        complement: complement,
        postalCode: postalCode,
      )
      ..onboardingCompleted = true
      ..accountCreatedAt = DateTime.now();
    await _persist();
  }

  Future<void> restartOnboarding() async {
    data.settings.onboardingCompleted = false;
    await _persist();
  }

  Future<void> exitCurrentSystem() async {
    final nextData = AgendaData.fromJson(data.toJson());
    final settings = nextData.settings;
    settings
      ..businessName = 'Balcão Livre'
      ..businessDocument = ''
      ..businessPhone = ''
      ..businessAddress = ''
      ..accountFullName = ''
      ..accountPhone = ''
      ..accountEmail = ''
      ..businessSegment = ''
      ..clientLabel = 'Cliente'
      ..clientDetailLabel = 'Paciente / pet / veículo / preferência'
      ..resourceLabel = 'Sala, box ou cadeira'
      ..workdayStartHour = 8
      ..workdayEndHour = 20
      ..workdays = <int>[1, 2, 3, 4, 5, 6]
      ..workdayBreakEnabled = true
      ..workdayBreakStartHour = 12
      ..workdayBreakEndHour = 13
      ..resources = <String>[]
      ..professionalCountRange = ''
      ..mainObjective = ''
      ..postalCode = ''
      ..neighborhood = ''
      ..street = ''
      ..addressNumber = ''
      ..addressComplement = ''
      ..accountPasswordHash = ''
      ..accountCreatedAt = DateTime(1)
      ..mercadoPagoEnabled = false
      ..mercadoPagoConnected = false
      ..mercadoPagoLicenseKey = ''
      ..mercadoPagoPaymentsApiUrl = AgendaSettings().mercadoPagoPaymentsApiUrl
      ..mercadoPagoSellerUserId = ''
      ..mercadoPagoDefaultTerminalId = ''
      ..mercadoPagoDefaultTerminalLabel = ''
      ..mercadoPagoLastError = ''
      ..mercadoPagoLastSyncAt = null
      ..instagramEnabled = true
      ..instagramLinked = false
      ..instagramUsername = ''
      ..instagramDisplayName = ''
      ..instagramAccountId = ''
      ..instagramState = ''
      ..instagramLastError = ''
      ..instagramLinkedAt = null
      ..instagramLastCheckedAt = null
      ..onboardingCompleted = false;

    _dataMutationGeneration++;
    await _repository.save(nextData);
    data = nextData;
    page = AgendaPage.home;
    agendaMode = AgendaViewMode.board;
    selectedDate = _dateOnly(DateTime.now());
    searchQuery = '';
    notifyListeners();
  }

  Future<void> logoutOrExit() async {
    final logout = _onLogout;
    if (logout != null) {
      await logout();
      return;
    }
    await exitCurrentSystem();
  }

  Future<void> resolveSyncConflictUsingCloud() async {
    final repository = _syncRepository;
    if (repository == null) return;
    final remote = await repository.resolveConflictUsingCloud();
    if (remote == null) return;
    data = remote;
    _normalize();
    notifyListeners();
  }

  Future<void> _resolveSyncConflictAutomatically() async {
    final repository = _syncRepository;
    if (repository == null ||
        !repository.hasConflict ||
        _resolvingSyncConflict) {
      return;
    }

    _resolvingSyncConflict = true;
    try {
      final remote = await repository.resolveConflictUsingCloud();
      if (remote == null) return;
      data = remote;
      _normalize();
      notifyListeners();
    } on Object {
      // The local snapshot and its recovery copy stay available. A later
      // repository notification or foreground refresh can retry safely.
    } finally {
      _resolvingSyncConflict = false;
    }
  }

  Future<void> resolveSyncConflictUsingLocal() async {
    final repository = _syncRepository;
    if (repository == null) return;
    final remote = await repository.resolveConflictUsingLocal();
    if (remote == null) return;
    data = remote;
    _normalize();
    notifyListeners();
  }

  /// Called by the Web lifecycle when the browser tab becomes active again.
  /// A controller-side generation check complements the repository guards so
  /// an edit made while the request is in flight remains visible.
  Future<void> refreshRemoteIfSafe() async {
    final repository = _syncRepository;
    if (repository == null || loading || loadError != null) return;
    final startingMutationGeneration = _dataMutationGeneration;
    try {
      final remote = await repository.refreshRemoteIfSafe();
      if (remote == null ||
          startingMutationGeneration != _dataMutationGeneration) {
        return;
      }
      data = remote;
      _normalize();
      notifyListeners();
    } on Object {
      // Foreground refresh is best effort. Local data remains available and
      // the repository exposes connectivity/conflict status to the UI.
    }
  }

  Future<void> retrySync() async {
    await _syncRepository?.retrySync();
  }

  String exportJson() =>
      const JsonEncoder.withIndent('  ').convert(data.toJson());

  Future<String?> importJson(String source) async {
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map) return 'O arquivo não contém uma agenda válida.';
      data = AgendaData.fromJson(Map<String, dynamic>.from(decoded));
      _normalize();
      await _persist();
      return null;
    } catch (_) {
      return 'Não foi possível ler o JSON informado.';
    }
  }

  Appointment? _appointmentConflict(Appointment candidate) {
    final service = data.services
        .where((item) => item.id == candidate.serviceId)
        .firstOrNull;
    final candidateStart = candidate.start.subtract(
      Duration(minutes: service?.preparationMinutes ?? 0),
    );
    final candidateEnd = candidate.end.add(
      Duration(minutes: service?.bufferMinutes ?? 0),
    );
    for (final item in data.appointments) {
      if (item.id == candidate.id) continue;
      if (const {
        AppointmentStatus.cancelled,
        AppointmentStatus.noShow,
      }.contains(item.status)) {
        continue;
      }
      final sameProfessional =
          candidate.professionalId.isNotEmpty &&
          item.professionalId == candidate.professionalId;
      final sameResource =
          candidate.resourceName.isNotEmpty &&
          item.resourceName.toLowerCase() ==
              candidate.resourceName.toLowerCase();
      if (!sameProfessional && !sameResource) continue;
      final otherService = data.services
          .where((service) => service.id == item.serviceId)
          .firstOrNull;
      final otherStart = item.start.subtract(
        Duration(minutes: otherService?.preparationMinutes ?? 0),
      );
      final otherEnd = item.end.add(
        Duration(minutes: otherService?.bufferMinutes ?? 0),
      );
      if (candidateStart.isBefore(otherEnd) &&
          candidateEnd.isAfter(otherStart)) {
        return item;
      }
    }
    return null;
  }

  void _upsertCustomerFromAppointment(Appointment appointment) {
    final normalizedPhone = appointment.customerPhone.replaceAll(
      RegExp(r'\D'),
      '',
    );
    final index = data.customers.indexWhere((item) {
      final phone = item.phone.replaceAll(RegExp(r'\D'), '');
      if (normalizedPhone.isNotEmpty && phone == normalizedPhone) return true;
      return item.name.trim().toLowerCase() ==
          appointment.customerName.trim().toLowerCase();
    });
    if (index < 0) {
      data.customers.add(
        Customer(
          name: appointment.customerName,
          phone: appointment.customerPhone,
          segment: appointment.segment,
          profile: appointment.customerProfile,
          notes: appointment.notes,
          lastSeenAt: appointment.start,
        ),
      );
    } else {
      final customer = data.customers[index];
      customer
        ..name = appointment.customerName
        ..phone = appointment.customerPhone
        ..segment = appointment.segment
        ..profile = appointment.customerProfile
        ..lastSeenAt = appointment.start;
    }
  }

  void _normalize() {
    final settings = data.settings;
    settings.businessName = settings.businessName.trim().isEmpty
        ? 'Balcão Livre'
        : settings.businessName.trim();
    _normalizeSettings(settings);
  }

  void _normalizeSettings(AgendaSettings settings) {
    final verifiedEmail = authenticatedEmail.trim();
    if (verifiedEmail.isNotEmpty) settings.accountEmail = verifiedEmail;
    settings.workdayStartHour = settings.workdayStartHour.clamp(0, 23);
    settings.workdayEndHour = settings.workdayEndHour.clamp(1, 24);
    if (settings.workdayEndHour <= settings.workdayStartHour) {
      settings.workdayStartHour = 8;
      settings.workdayEndHour = 20;
    }
    settings.workdays =
        settings.workdays.where((day) => day >= 0 && day <= 6).toSet().toList()
          ..sort((a, b) {
            final left = a == 0 ? 7 : a;
            final right = b == 0 ? 7 : b;
            return left.compareTo(right);
          });
    if (settings.workdays.isEmpty) {
      settings.workdays = <int>[1, 2, 3, 4, 5, 6];
    }
    settings.workdayBreakStartHour = settings.workdayBreakStartHour.clamp(
      0,
      23,
    );
    settings.workdayBreakEndHour = settings.workdayBreakEndHour.clamp(1, 24);
    final invalidBreak =
        settings.workdayBreakEndHour <= settings.workdayBreakStartHour ||
        settings.workdayBreakStartHour < settings.workdayStartHour ||
        settings.workdayBreakEndHour > settings.workdayEndHour;
    if (invalidBreak) {
      settings.workdayBreakEnabled = false;
      settings.workdayBreakStartHour = 12.clamp(
        settings.workdayStartHour,
        settings.workdayEndHour - 1,
      );
      settings.workdayBreakEndHour = _minInt(
        settings.workdayEndHour,
        settings.workdayBreakStartHour + 1,
      );
    }
    settings.resources = settings.resources
        .map((item) => item.trim())
        .where((item) => item.isNotEmpty)
        .toSet()
        .toList();
  }

  Future<void> _persist() async {
    _dataMutationGeneration++;
    await _repository.save(data);
    notifyListeners();
  }

  void _onRepositoryChanged() {
    notifyListeners();
    if (_syncRepository?.hasConflict ?? false) {
      unawaited(_resolveSyncConflictAutomatically());
    }
  }

  @override
  void dispose() {
    final repository = _repository;
    if (repository is Listenable) {
      (repository as Listenable).removeListener(_onRepositoryChanged);
    }
    super.dispose();
  }
}

int _minInt(int first, int second) => first < second ? first : second;

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}

DateTime _dateOnly(DateTime value) =>
    DateTime(value.year, value.month, value.day);

bool _isSameDay(DateTime a, DateTime b) =>
    a.year == b.year && a.month == b.month && a.day == b.day;

String _normalizedBrazilPhone(String value) {
  var digits = value.replaceAll(RegExp(r'\D'), '');
  while (digits.startsWith('0')) {
    digits = digits.substring(1);
  }
  if (digits.length > 11 && digits.startsWith('55')) {
    digits = digits.substring(2);
  }
  return digits;
}

String _buildOnboardingAddress({
  required String street,
  required String number,
  required String neighborhood,
  required String complement,
  required String postalCode,
}) {
  final cleanStreet = street.trim();
  final cleanNumber = number.trim();
  final parts = <String>[];
  if (cleanStreet.isNotEmpty) {
    parts.add(cleanNumber.isEmpty ? cleanStreet : '$cleanStreet, $cleanNumber');
  }
  final cleanNeighborhood = neighborhood.trim();
  if (cleanNeighborhood.isNotEmpty) parts.add(cleanNeighborhood);
  final cleanComplement = complement.trim();
  if (cleanComplement.isNotEmpty) parts.add(cleanComplement);
  final cleanPostalCode = postalCode.trim();
  if (cleanPostalCode.isNotEmpty) parts.add('CEP $cleanPostalCode');
  return parts.join(' | ');
}
