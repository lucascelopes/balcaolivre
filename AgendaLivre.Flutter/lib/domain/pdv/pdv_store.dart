import '../models/id_generator.dart';
import '../models/json_helpers.dart';

enum PdvSaleMode {
  tables('tables'),
  counter('counter'),
  delivery('delivery');

  const PdvSaleMode(this.wireName);

  final String wireName;

  static PdvSaleMode fromWire(String value) {
    final normalized = value.trim().toLowerCase();
    return switch (normalized) {
      'counter' || 'balcao' || 'balcão' => PdvSaleMode.counter,
      'delivery' || 'entrega' => PdvSaleMode.delivery,
      _ => PdvSaleMode.tables,
    };
  }
}

enum PdvTicketStatus {
  open('open'),
  completed('completed'),
  reopened('reopened'),
  canceled('canceled');

  const PdvTicketStatus(this.wireName);

  final String wireName;

  static PdvTicketStatus fromWire(String value) {
    final normalized = value.trim().toLowerCase();
    return PdvTicketStatus.values.firstWhere(
      (status) => status.wireName == normalized,
      orElse: () => PdvTicketStatus.open,
    );
  }
}

enum PdvPaymentMethod {
  cash('cash'),
  pix('pix'),
  credit('credit'),
  debit('debit'),
  other('other');

  const PdvPaymentMethod(this.wireName);

  final String wireName;

  static PdvPaymentMethod fromWire(String value) {
    final normalized = value.trim().toLowerCase();
    return switch (normalized) {
      'cash' || 'money' || 'dinheiro' => PdvPaymentMethod.cash,
      'pix' => PdvPaymentMethod.pix,
      'credit' || 'credito' || 'crédito' => PdvPaymentMethod.credit,
      'debit' || 'debito' || 'débito' => PdvPaymentMethod.debit,
      _ => PdvPaymentMethod.other,
    };
  }
}

enum PdvCashMovementType {
  opening('opening'),
  supply('supply'),
  withdrawal('withdrawal'),
  sale('sale'),
  refund('refund'),
  closing('closing');

  const PdvCashMovementType(this.wireName);

  final String wireName;

  bool get reducesBalance =>
      this == PdvCashMovementType.withdrawal ||
      this == PdvCashMovementType.refund;

  static PdvCashMovementType fromWire(String value) {
    final normalized = value.trim().toLowerCase();
    return switch (normalized) {
      'suprimento' || 'supply' => PdvCashMovementType.supply,
      'sangria' || 'withdrawal' => PdvCashMovementType.withdrawal,
      'sale' || 'venda' => PdvCashMovementType.sale,
      'refund' || 'estorno' => PdvCashMovementType.refund,
      'closing' || 'fechamento' => PdvCashMovementType.closing,
      _ => PdvCashMovementType.opening,
    };
  }
}

class PdvTicketItem {
  PdvTicketItem({
    String? id,
    this.productId = '',
    this.code = '',
    this.name = '',
    this.quantity = 1,
    this.unitPrice = 0,
    this.note = '',
  }) : id = agendaIdOrGenerate(id);

  String id;
  String productId;
  String code;
  String name;
  int quantity;
  double unitPrice;
  String note;

  double get total => quantity * unitPrice;

  bool matches(PdvTicketItem other) =>
      productId == other.productId &&
      code == other.code &&
      unitPrice == other.unitPrice &&
      note == other.note;

  factory PdvTicketItem.fromJson(JsonMap json) => PdvTicketItem(
    id: jsonString(json, 'Id'),
    productId: jsonString(json, 'ProductId'),
    code: jsonString(json, 'Code'),
    name: jsonString(json, 'Name'),
    quantity: jsonInt(json, 'Quantity', fallback: 1),
    unitPrice: jsonDouble(json, 'UnitPrice'),
    note: jsonString(json, 'Note'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'ProductId': productId,
    'Code': code,
    'Name': name,
    'Quantity': quantity,
    'UnitPrice': unitPrice,
    'Note': note,
  };
}

class PdvPayment {
  PdvPayment({
    String? id,
    this.method = PdvPaymentMethod.cash,
    this.amount = 0,
    this.provider = '',
    this.reference = '',
    this.status = 'approved',
    DateTime? createdAt,
  }) : id = agendaIdOrGenerate(id),
       createdAt = createdAt ?? DateTime.now();

  String id;
  PdvPaymentMethod method;
  double amount;
  String provider;
  String reference;
  String status;
  DateTime createdAt;

  bool get countsAsPaid {
    final normalized = status.trim().toLowerCase();
    return normalized != 'canceled' &&
        normalized != 'cancelled' &&
        normalized != 'failed' &&
        normalized != 'refused';
  }

  factory PdvPayment.fromJson(JsonMap json) => PdvPayment(
    id: jsonString(json, 'Id'),
    method: PdvPaymentMethod.fromWire(jsonString(json, 'Method')),
    amount: jsonDouble(json, 'Amount'),
    provider: jsonString(json, 'Provider'),
    reference: jsonString(json, 'Reference'),
    status: jsonString(json, 'Status', fallback: 'approved'),
    createdAt: jsonDateTime(
      json,
      'CreatedAt',
      fallback: DateTime.fromMillisecondsSinceEpoch(0),
    ),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Method': method.wireName,
    'Amount': amount,
    'Provider': provider,
    'Reference': reference,
    'Status': status,
    'CreatedAt': dateTimeToJson(createdAt),
  };
}

class PdvTicket {
  PdvTicket({
    String? id,
    this.storeId = '',
    this.terminalId = '',
    this.boardNumber = '000001',
    this.mode = PdvSaleMode.tables,
    this.status = PdvTicketStatus.open,
    this.operatorId = '',
    this.waiterId = '',
    this.customerId = '',
    this.customerName = '',
    this.coverValue = 0,
    this.servicePercent = 10,
    this.discountPercent = 0,
    this.people = 1,
    List<PdvTicketItem>? items,
    List<PdvPayment>? payments,
    DateTime? createdAt,
    DateTime? updatedAt,
    this.completedAt,
    this.reopenedFromSaleId = '',
  }) : id = agendaIdOrGenerate(id),
       items = List<PdvTicketItem>.of(items ?? const <PdvTicketItem>[]),
       payments = List<PdvPayment>.of(payments ?? const <PdvPayment>[]),
       createdAt = createdAt ?? DateTime.now(),
       updatedAt = updatedAt ?? createdAt ?? DateTime.now();

  String id;
  String storeId;
  String terminalId;
  String boardNumber;
  PdvSaleMode mode;
  PdvTicketStatus status;
  String operatorId;
  String waiterId;
  String customerId;
  String customerName;
  double coverValue;
  double servicePercent;
  double discountPercent;
  int people;
  List<PdvTicketItem> items;
  List<PdvPayment> payments;
  DateTime createdAt;
  DateTime updatedAt;
  DateTime? completedAt;
  String reopenedFromSaleId;

  double get subtotal =>
      items.fold<double>(0, (total, item) => total + item.total);

  double get serviceValue =>
      subtotal * servicePercent.clamp(0, 100).toDouble() / 100;

  double get discountValue =>
      subtotal * discountPercent.clamp(0, 100).toDouble() / 100;

  double get total => (subtotal + coverValue + serviceValue - discountValue)
      .clamp(0, double.infinity);

  double get paid => payments
      .where((payment) => payment.countsAsPaid)
      .fold<double>(0, (total, payment) => total + payment.amount);

  double get balance => (total - paid).clamp(0, double.infinity);

  double get change => (paid - total).clamp(0, double.infinity);

  int get itemCount =>
      items.fold<int>(0, (total, item) => total + item.quantity);

  bool get canFinalize => items.isNotEmpty && balance < .01;

  void addItem(PdvTicketItem item, {DateTime? now}) {
    if (item.quantity <= 0) {
      throw ArgumentError.value(item.quantity, 'quantity');
    }
    final existing = items
        .where((candidate) => candidate.matches(item))
        .firstOrNull;
    if (existing == null) {
      items.add(item);
    } else {
      existing.quantity += item.quantity;
    }
    updatedAt = now ?? DateTime.now();
  }

  void changeItemQuantity(String itemId, int quantity, {DateTime? now}) {
    final item = items.where((candidate) => candidate.id == itemId).firstOrNull;
    if (item == null) {
      throw StateError('Item da comanda não encontrado.');
    }
    if (quantity <= 0) {
      items.remove(item);
    } else {
      item.quantity = quantity;
    }
    updatedAt = now ?? DateTime.now();
  }

  void applyDiscount(double percent, {DateTime? now}) {
    discountPercent = percent.clamp(0, 100).toDouble();
    updatedAt = now ?? DateTime.now();
  }

  void addPayment(PdvPayment payment, {DateTime? now}) {
    if (payment.amount <= 0) {
      throw ArgumentError.value(payment.amount, 'amount');
    }
    payments.add(payment);
    updatedAt = now ?? DateTime.now();
  }

  void complete({DateTime? now}) {
    if (items.isEmpty) {
      throw StateError('Não é possível finalizar uma comanda vazia.');
    }
    if (balance >= .01) {
      throw StateError('Ainda existe saldo pendente na comanda.');
    }
    final timestamp = now ?? DateTime.now();
    status = PdvTicketStatus.completed;
    completedAt = timestamp;
    updatedAt = timestamp;
  }

  void reopen({DateTime? now}) {
    if (status != PdvTicketStatus.completed) {
      throw StateError('Somente uma venda finalizada pode ser reaberta.');
    }
    reopenedFromSaleId = id;
    status = PdvTicketStatus.reopened;
    completedAt = null;
    updatedAt = now ?? DateTime.now();
  }

  factory PdvTicket.fromJson(JsonMap json) => PdvTicket(
    id: jsonString(json, 'Id'),
    storeId: jsonString(json, 'StoreId'),
    terminalId: jsonString(json, 'TerminalId'),
    boardNumber: jsonString(json, 'BoardNumber', fallback: '000001'),
    mode: PdvSaleMode.fromWire(jsonString(json, 'Mode')),
    status: PdvTicketStatus.fromWire(jsonString(json, 'Status')),
    operatorId: jsonString(json, 'OperatorId'),
    waiterId: jsonString(json, 'WaiterId'),
    customerId: jsonString(json, 'CustomerId'),
    customerName: jsonString(json, 'CustomerName'),
    coverValue: jsonDouble(json, 'CoverValue'),
    servicePercent: jsonDouble(json, 'ServicePercent', fallback: 10),
    discountPercent: jsonDouble(json, 'DiscountPercent'),
    people: jsonInt(json, 'People', fallback: 1),
    items: jsonObjectList(
      json,
      'Items',
    ).map(PdvTicketItem.fromJson).toList(growable: true),
    payments: jsonObjectList(
      json,
      'Payments',
    ).map(PdvPayment.fromJson).toList(growable: true),
    createdAt: jsonDateTime(
      json,
      'CreatedAt',
      fallback: DateTime.fromMillisecondsSinceEpoch(0),
    ),
    updatedAt: jsonDateTime(
      json,
      'UpdatedAt',
      fallback: DateTime.fromMillisecondsSinceEpoch(0),
    ),
    completedAt: jsonNullableDateTime(json, 'CompletedAt'),
    reopenedFromSaleId: jsonString(json, 'ReopenedFromSaleId'),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'StoreId': storeId,
    'TerminalId': terminalId,
    'BoardNumber': boardNumber,
    'Mode': mode.wireName,
    'Status': status.wireName,
    'OperatorId': operatorId,
    'WaiterId': waiterId,
    'CustomerId': customerId,
    'CustomerName': customerName,
    'CoverValue': coverValue,
    'ServicePercent': servicePercent,
    'DiscountPercent': discountPercent,
    'People': people,
    'Items': items.map((item) => item.toJson()).toList(),
    'Payments': payments.map((payment) => payment.toJson()).toList(),
    'CreatedAt': dateTimeToJson(createdAt),
    'UpdatedAt': dateTimeToJson(updatedAt),
    'CompletedAt': dateTimeToJson(completedAt),
    'ReopenedFromSaleId': reopenedFromSaleId,
  };
}

class PdvCashMovement {
  PdvCashMovement({
    String? id,
    this.type = PdvCashMovementType.supply,
    this.amount = 0,
    this.note = '',
    this.operatorId = '',
    this.ticketId = '',
    DateTime? createdAt,
  }) : id = agendaIdOrGenerate(id),
       createdAt = createdAt ?? DateTime.now();

  String id;
  PdvCashMovementType type;
  double amount;
  String note;
  String operatorId;
  String ticketId;
  DateTime createdAt;

  double get signedAmount => type.reducesBalance ? -amount.abs() : amount.abs();

  factory PdvCashMovement.fromJson(JsonMap json) => PdvCashMovement(
    id: jsonString(json, 'Id'),
    type: PdvCashMovementType.fromWire(jsonString(json, 'Type')),
    amount: jsonDouble(json, 'Amount').abs(),
    note: jsonString(json, 'Note'),
    operatorId: jsonString(json, 'OperatorId'),
    ticketId: jsonString(json, 'TicketId'),
    createdAt: jsonDateTime(
      json,
      'CreatedAt',
      fallback: DateTime.fromMillisecondsSinceEpoch(0),
    ),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Type': type.wireName,
    'Amount': amount.abs(),
    'Note': note,
    'OperatorId': operatorId,
    'TicketId': ticketId,
    'CreatedAt': dateTimeToJson(createdAt),
  };
}

class PdvCashSession {
  PdvCashSession({
    String? id,
    this.storeId = '',
    this.terminalId = '',
    this.operatorId = '',
    this.openingAmount = 0,
    DateTime? openedAt,
    this.closedAt,
    this.closingAmount,
    List<PdvCashMovement>? movements,
  }) : id = agendaIdOrGenerate(id),
       openedAt = openedAt ?? DateTime.now(),
       movements = List<PdvCashMovement>.of(
         movements ?? const <PdvCashMovement>[],
       );

  String id;
  String storeId;
  String terminalId;
  String operatorId;
  double openingAmount;
  DateTime openedAt;
  DateTime? closedAt;
  double? closingAmount;
  List<PdvCashMovement> movements;

  bool get isOpen => closedAt == null;

  double get expectedBalance =>
      openingAmount +
      movements.fold<double>(
        0,
        (total, movement) => total + movement.signedAmount,
      );

  double? get difference =>
      closingAmount == null ? null : closingAmount! - expectedBalance;

  void addMovement(PdvCashMovement movement) {
    if (!isOpen) {
      throw StateError('O caixa está fechado.');
    }
    if (movement.amount <= 0) {
      throw ArgumentError.value(movement.amount, 'amount');
    }
    movements.add(movement);
  }

  void close(double countedAmount, {DateTime? now}) {
    if (!isOpen) {
      throw StateError('O caixa já está fechado.');
    }
    closingAmount = countedAmount;
    closedAt = now ?? DateTime.now();
  }

  factory PdvCashSession.fromJson(JsonMap json) => PdvCashSession(
    id: jsonString(json, 'Id'),
    storeId: jsonString(json, 'StoreId'),
    terminalId: jsonString(json, 'TerminalId'),
    operatorId: jsonString(json, 'OperatorId'),
    openingAmount: jsonDouble(json, 'OpeningAmount'),
    openedAt: jsonDateTime(
      json,
      'OpenedAt',
      fallback: DateTime.fromMillisecondsSinceEpoch(0),
    ),
    closedAt: jsonNullableDateTime(json, 'ClosedAt'),
    closingAmount: jsonField(json, 'ClosingAmount') == null
        ? null
        : jsonDouble(json, 'ClosingAmount'),
    movements: jsonObjectList(
      json,
      'Movements',
    ).map(PdvCashMovement.fromJson).toList(growable: true),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'StoreId': storeId,
    'TerminalId': terminalId,
    'OperatorId': operatorId,
    'OpeningAmount': openingAmount,
    'OpenedAt': dateTimeToJson(openedAt),
    'ClosedAt': dateTimeToJson(closedAt),
    'ClosingAmount': closingAmount,
    'Movements': movements.map((movement) => movement.toJson()).toList(),
  };
}

class PdvSyncEvent {
  PdvSyncEvent({
    String? eventId,
    this.type = '',
    JsonMap? payload,
    DateTime? createdAt,
    this.attemptCount = 0,
    this.lastError = '',
    this.syncedAt,
  }) : eventId = agendaIdOrGenerate(eventId),
       payload = Map<String, dynamic>.from(
         payload ?? const <String, dynamic>{},
       ),
       createdAt = createdAt ?? DateTime.now();

  String eventId;
  String type;
  JsonMap payload;
  DateTime createdAt;
  int attemptCount;
  String lastError;
  DateTime? syncedAt;

  bool get isPending => syncedAt == null;

  factory PdvSyncEvent.fromJson(JsonMap json) => PdvSyncEvent(
    eventId: jsonString(json, 'EventId'),
    type: jsonString(json, 'Type'),
    payload: jsonObject(json, 'Payload'),
    createdAt: jsonDateTime(
      json,
      'CreatedAt',
      fallback: DateTime.fromMillisecondsSinceEpoch(0),
    ),
    attemptCount: jsonInt(json, 'AttemptCount'),
    lastError: jsonString(json, 'LastError'),
    syncedAt: jsonNullableDateTime(json, 'SyncedAt'),
  );

  JsonMap toJson() => <String, dynamic>{
    'EventId': eventId,
    'Type': type,
    'Payload': payload,
    'CreatedAt': dateTimeToJson(createdAt),
    'AttemptCount': attemptCount,
    'LastError': lastError,
    'SyncedAt': dateTimeToJson(syncedAt),
  };
}

class PdvStore {
  PdvStore({
    this.version = 1,
    this.storeId = 'loja_demo',
    this.terminalId = 'caixa_01',
    List<PdvTicket>? tickets,
    List<PdvCashSession>? cashSessions,
    List<PdvSyncEvent>? syncQueue,
  }) : tickets = List<PdvTicket>.of(tickets ?? const <PdvTicket>[]),
       cashSessions = List<PdvCashSession>.of(
         cashSessions ?? const <PdvCashSession>[],
       ),
       syncQueue = List<PdvSyncEvent>.of(syncQueue ?? const <PdvSyncEvent>[]);

  int version;
  String storeId;
  String terminalId;
  List<PdvTicket> tickets;
  List<PdvCashSession> cashSessions;
  List<PdvSyncEvent> syncQueue;

  Iterable<PdvTicket> get openTickets => tickets.where(
    (ticket) =>
        ticket.status == PdvTicketStatus.open ||
        ticket.status == PdvTicketStatus.reopened,
  );

  PdvCashSession? get openCashSession => cashSessions
      .where(
        (session) =>
            session.isOpen &&
            session.storeId == storeId &&
            session.terminalId == terminalId,
      )
      .firstOrNull;

  PdvTicket openTicket({
    required String boardNumber,
    PdvSaleMode mode = PdvSaleMode.tables,
    String operatorId = '',
    String waiterId = '',
    DateTime? now,
  }) {
    final existing = openTickets
        .where((ticket) => ticket.boardNumber == boardNumber)
        .firstOrNull;
    if (existing != null) {
      return existing;
    }
    final timestamp = now ?? DateTime.now();
    final ticket = PdvTicket(
      storeId: storeId,
      terminalId: terminalId,
      boardNumber: boardNumber,
      mode: mode,
      operatorId: operatorId,
      waiterId: waiterId,
      createdAt: timestamp,
      updatedAt: timestamp,
    );
    tickets.add(ticket);
    enqueue('ticket_opened', ticket.toJson(), createdAt: timestamp);
    return ticket;
  }

  PdvTicket transferTicket(
    String sourceBoard,
    String destinationBoard, {
    DateTime? now,
  }) {
    if (sourceBoard == destinationBoard) {
      throw ArgumentError('A comanda de destino deve ser diferente.');
    }
    final source = openTickets
        .where((ticket) => ticket.boardNumber == sourceBoard)
        .firstOrNull;
    if (source == null || source.items.isEmpty) {
      throw StateError('Não há itens para transferir.');
    }
    final timestamp = now ?? DateTime.now();
    final destination = openTickets
        .where((ticket) => ticket.boardNumber == destinationBoard)
        .firstOrNull;
    if (destination == null) {
      source.boardNumber = destinationBoard;
      source.updatedAt = timestamp;
      enqueue('ticket_transferred', <String, dynamic>{
        'TicketId': source.id,
        'SourceBoard': sourceBoard,
        'DestinationBoard': destinationBoard,
      }, createdAt: timestamp);
      return source;
    }

    for (final item in source.items) {
      destination.addItem(item, now: timestamp);
    }
    destination
      ..payments.addAll(source.payments)
      ..customerId = source.customerId
      ..customerName = source.customerName
      ..operatorId = source.operatorId
      ..waiterId = source.waiterId
      ..coverValue = source.coverValue
      ..servicePercent = source.servicePercent
      ..discountPercent = source.discountPercent
      ..people = source.people
      ..updatedAt = timestamp;
    source
      ..status = PdvTicketStatus.canceled
      ..updatedAt = timestamp;
    enqueue('ticket_transferred', <String, dynamic>{
      'TicketId': source.id,
      'DestinationTicketId': destination.id,
      'SourceBoard': sourceBoard,
      'DestinationBoard': destinationBoard,
    }, createdAt: timestamp);
    return destination;
  }

  PdvCashSession openCash({
    required String operatorId,
    double openingAmount = 0,
    DateTime? now,
  }) {
    if (openCashSession != null) {
      throw StateError('Já existe um caixa aberto neste terminal.');
    }
    final timestamp = now ?? DateTime.now();
    final session = PdvCashSession(
      storeId: storeId,
      terminalId: terminalId,
      operatorId: operatorId,
      openingAmount: openingAmount,
      openedAt: timestamp,
    );
    cashSessions.add(session);
    enqueue('cash_opened', session.toJson(), createdAt: timestamp);
    return session;
  }

  void completeTicket(PdvTicket ticket, {DateTime? now}) {
    final timestamp = now ?? DateTime.now();
    ticket.complete(now: timestamp);
    enqueue('sale_created', ticket.toJson(), createdAt: timestamp);
    final cashPayment = ticket.payments
        .where(
          (payment) =>
              payment.method == PdvPaymentMethod.cash && payment.countsAsPaid,
        )
        .fold<double>(0, (total, payment) => total + payment.amount);
    if (cashPayment > 0) {
      openCashSession?.addMovement(
        PdvCashMovement(
          type: PdvCashMovementType.sale,
          amount: cashPayment - ticket.change,
          operatorId: ticket.operatorId,
          ticketId: ticket.id,
          createdAt: timestamp,
        ),
      );
    }
  }

  PdvSyncEvent enqueue(String type, JsonMap payload, {DateTime? createdAt}) {
    final event = PdvSyncEvent(
      type: type,
      payload: payload,
      createdAt: createdAt,
    );
    syncQueue.add(event);
    return event;
  }

  factory PdvStore.fromJson(JsonMap json) => PdvStore(
    version: jsonInt(json, 'Version', fallback: 1),
    storeId: jsonString(json, 'StoreId', fallback: 'loja_demo'),
    terminalId: jsonString(json, 'TerminalId', fallback: 'caixa_01'),
    tickets: jsonObjectList(
      json,
      'Tickets',
    ).map(PdvTicket.fromJson).toList(growable: true),
    cashSessions: jsonObjectList(
      json,
      'CashSessions',
    ).map(PdvCashSession.fromJson).toList(growable: true),
    syncQueue: jsonObjectList(
      json,
      'SyncQueue',
    ).map(PdvSyncEvent.fromJson).toList(growable: true),
  );

  JsonMap toJson() => <String, dynamic>{
    'Version': version,
    'StoreId': storeId,
    'TerminalId': terminalId,
    'Tickets': tickets.map((ticket) => ticket.toJson()).toList(),
    'CashSessions': cashSessions.map((session) => session.toJson()).toList(),
    'SyncQueue': syncQueue.map((event) => event.toJson()).toList(),
  };
}

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull {
    final iterator = this.iterator;
    return iterator.moveNext() ? iterator.current : null;
  }
}
