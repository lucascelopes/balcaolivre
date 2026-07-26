enum OrderKind { table, counter, delivery, ifood }

enum OrderStatus { open, preparing, dispatched, delivered, closed, canceled }

enum StaffPermission {
  transfer,
  cancel,
  discount,
  manageProducts,
  reports,
  cash,
  delivery,
  inventory,
  kitchen,
  ifood,
  settings,
  backup,
  fiscal,
  deliveryZones,
  centralSync,
}

class Product {
  Product({
    required this.id,
    required this.code,
    required this.name,
    required this.category,
    required this.price,
    required this.cost,
    required this.stock,
    required this.minStock,
    this.unit = 'un',
    this.imageData = '',
    this.active = true,
  });

  final String id;
  String code;
  String name;
  String category;
  double price;
  double cost;
  int stock;
  int minStock;
  String unit;
  String imageData;
  bool active;

  double get margin => price <= 0 ? 0 : ((price - cost) / price) * 100;

  Map<String, dynamic> toJson() => {
    'id': id,
    'code': code,
    'name': name,
    'category': category,
    'price': price,
    'cost': cost,
    'stock': stock,
    'minStock': minStock,
    'unit': unit,
    'imageData': imageData,
    'active': active,
  };

  factory Product.fromJson(Map<String, dynamic> json) => Product(
    id: json['id'] as String,
    code: json['code'] as String,
    name: json['name'] as String,
    category: json['category'] as String,
    price: (json['price'] as num).toDouble(),
    cost: (json['cost'] as num).toDouble(),
    stock: (json['stock'] as num).round(),
    minStock: (json['minStock'] as num).round(),
    unit: json['unit'] as String? ?? 'un',
    imageData: json['imageData'] as String? ?? '',
    active: json['active'] as bool? ?? true,
  );
}

class OrderItem {
  OrderItem({
    required this.productId,
    required this.code,
    required this.name,
    required this.quantity,
    required this.price,
    required this.cost,
  });

  final String productId;
  final String code;
  final String name;
  int quantity;
  double price;
  double cost;

  double get total => price * quantity;
  double get totalCost => cost * quantity;

  Map<String, dynamic> toJson() => {
    'productId': productId,
    'code': code,
    'name': name,
    'quantity': quantity,
    'price': price,
    'cost': cost,
  };

  factory OrderItem.fromJson(Map<String, dynamic> json) => OrderItem(
    productId: json['productId'] as String,
    code: json['code'] as String,
    name: json['name'] as String,
    quantity: (json['quantity'] as num).round(),
    price: (json['price'] as num).toDouble(),
    cost: (json['cost'] as num).toDouble(),
  );
}

class Order {
  Order({
    required this.id,
    required this.number,
    required this.kind,
    required this.status,
    required this.createdAt,
    this.customerName = '',
    this.waiter = '1',
    this.address = '',
    this.paymentMethod = '',
    this.ifoodRepasse = 0,
    this.coverCharge = 0,
    this.servicePercent = 10,
    List<OrderItem>? items,
  }) : items = items ?? [];

  final String id;
  String number;
  OrderKind kind;
  OrderStatus status;
  DateTime createdAt;
  String customerName;
  String waiter;
  String address;
  String paymentMethod;
  double ifoodRepasse;
  double coverCharge;
  double servicePercent;
  final List<OrderItem> items;

  bool get isOpen =>
      status != OrderStatus.closed && status != OrderStatus.canceled;
  double get itemsTotal => items.fold(0, (sum, item) => sum + item.total);
  double get serviceBase => items
      .where((item) => item.code != 'DESC' && item.total > 0)
      .fold(0, (sum, item) => sum + item.total);
  double get serviceAmount => serviceBase * (servicePercent / 100);
  double get subtotal => itemsTotal + coverCharge + serviceAmount;
  double get costTotal => items.fold(0, (sum, item) => sum + item.totalCost);
  double get profit => subtotal - costTotal;
  int get itemsCount => items.fold(0, (sum, item) => sum + item.quantity);

  Map<String, dynamic> toJson() => {
    'id': id,
    'number': number,
    'kind': kind.name,
    'status': status.name,
    'createdAt': createdAt.toIso8601String(),
    'customerName': customerName,
    'waiter': waiter,
    'address': address,
    'paymentMethod': paymentMethod,
    'ifoodRepasse': ifoodRepasse,
    'coverCharge': coverCharge,
    'servicePercent': servicePercent,
    'items': items.map((item) => item.toJson()).toList(),
  };

  factory Order.fromJson(Map<String, dynamic> json) => Order(
    id: json['id'] as String,
    number: json['number'] as String,
    kind: OrderKind.values.byName(json['kind'] as String),
    status: OrderStatus.values.byName(json['status'] as String),
    createdAt: DateTime.parse(json['createdAt'] as String),
    customerName: json['customerName'] as String? ?? '',
    waiter: json['waiter'] as String? ?? '1',
    address: json['address'] as String? ?? '',
    paymentMethod: json['paymentMethod'] as String? ?? '',
    ifoodRepasse: (json['ifoodRepasse'] as num?)?.toDouble() ?? 0,
    coverCharge: (json['coverCharge'] as num?)?.toDouble() ?? 0,
    servicePercent: (json['servicePercent'] as num?)?.toDouble() ?? 10,
    items: (json['items'] as List<dynamic>? ?? [])
        .map((item) => OrderItem.fromJson(item as Map<String, dynamic>))
        .toList(),
  );
}

class Customer {
  Customer({
    required this.id,
    required this.name,
    required this.phone,
    this.document = '',
    this.address = '',
    this.points = 0,
    this.cashback = 0,
    this.lastPurchaseAt,
  });

  final String id;
  String name;
  String phone;
  String document;
  String address;
  int points;
  double cashback;
  DateTime? lastPurchaseAt;

  bool get missing =>
      lastPurchaseAt == null ||
      DateTime.now().difference(lastPurchaseAt!).inDays >= 45;

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'phone': phone,
    'document': document,
    'address': address,
    'points': points,
    'cashback': cashback,
    'lastPurchaseAt': lastPurchaseAt?.toIso8601String(),
  };

  factory Customer.fromJson(Map<String, dynamic> json) => Customer(
    id: json['id'] as String,
    name: json['name'] as String,
    phone: json['phone'] as String,
    document: json['document'] as String? ?? '',
    address: json['address'] as String? ?? '',
    points: (json['points'] as num?)?.round() ?? 0,
    cashback: (json['cashback'] as num?)?.toDouble() ?? 0,
    lastPurchaseAt: json['lastPurchaseAt'] == null
        ? null
        : DateTime.parse(json['lastPurchaseAt'] as String),
  );
}

class TeamMember {
  TeamMember({
    required this.id,
    required this.number,
    required this.name,
    required this.role,
    this.active = true,
    this.pinHash = '',
    this.legacyPin = '',
    this.isMaster = false,
    this.canTransfer = false,
    this.canCancel = false,
    this.canDiscount = false,
    this.canManageProducts = false,
    this.canReports = false,
    this.canCash = false,
    this.canDelivery = false,
    this.canInventory = false,
    this.canKitchen = false,
    this.canIFood = false,
    this.canSettings = false,
    this.canBackup = false,
    this.canFiscal = false,
    this.canDeliveryZones = false,
    this.canCentralSync = false,
  });

  final String id;
  String number;
  String name;
  String role;
  bool active;
  String pinHash;
  String legacyPin;
  bool isMaster;
  bool canTransfer;
  bool canCancel;
  bool canDiscount;
  bool canManageProducts;
  bool canReports;
  bool canCash;
  bool canDelivery;
  bool canInventory;
  bool canKitchen;
  bool canIFood;
  bool canSettings;
  bool canBackup;
  bool canFiscal;
  bool canDeliveryZones;
  bool canCentralSync;

  void normalizeRolePermissions() {
    final normalized = role.trim().toUpperCase();
    if (isMaster || normalized == 'MASTER') {
      isMaster = true;
      canTransfer = true;
      canCancel = true;
      canDiscount = true;
      canManageProducts = true;
      canReports = true;
      canCash = true;
      canDelivery = true;
      canInventory = true;
      canKitchen = true;
      canIFood = true;
      canSettings = true;
      canBackup = true;
      canFiscal = true;
      canDeliveryZones = true;
      canCentralSync = true;
      return;
    }
    if (normalized == 'GERENTE') {
      canTransfer = true;
      canCancel = true;
      canDiscount = true;
      canManageProducts = true;
      canReports = true;
      canCash = true;
      canDelivery = true;
      canInventory = true;
      canKitchen = true;
      canIFood = true;
      canSettings = true;
      canBackup = true;
      canDeliveryZones = true;
      canCentralSync = true;
    } else if (normalized == 'CAIXA') {
      canCash = true;
      canCancel = true;
      canDiscount = true;
      canDelivery = true;
    } else if (normalized == 'GARCOM') {
      canTransfer = true;
      canDelivery = true;
    } else if (normalized == 'COZINHA') {
      canKitchen = true;
    } else if (normalized == 'ENTREGADOR') {
      canDelivery = true;
    }
  }

  bool allows(StaffPermission permission) {
    if (!active) return false;
    return switch (permission) {
      StaffPermission.transfer => isMaster || canTransfer,
      StaffPermission.cancel => isMaster || canCancel,
      StaffPermission.discount => isMaster || canDiscount,
      StaffPermission.manageProducts => isMaster || canManageProducts,
      StaffPermission.reports => isMaster || canReports,
      StaffPermission.cash => isMaster || canCash,
      StaffPermission.delivery => isMaster || canDelivery,
      StaffPermission.inventory => isMaster || canInventory,
      StaffPermission.kitchen => isMaster || canKitchen,
      StaffPermission.ifood => isMaster || canIFood,
      StaffPermission.settings => isMaster || canSettings,
      StaffPermission.backup => isMaster || canBackup,
      StaffPermission.fiscal => isMaster || canFiscal,
      StaffPermission.deliveryZones => isMaster || canDeliveryZones,
      StaffPermission.centralSync => isMaster || canCentralSync,
    };
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'number': number,
    'employeeNumber': number,
    'name': name,
    'role': role,
    'active': active,
    'pinHash': pinHash,
    'isMaster': isMaster,
    'canTransfer': canTransfer,
    'canCancel': canCancel,
    'canDiscount': canDiscount,
    'canManageProducts': canManageProducts,
    'canReports': canReports,
    'canCash': canCash,
    'canDelivery': canDelivery,
    'canInventory': canInventory,
    'canKitchen': canKitchen,
    'canIFood': canIFood,
    'canSettings': canSettings,
    'canBackup': canBackup,
    'canFiscal': canFiscal,
    'canDeliveryZones': canDeliveryZones,
    'canCentralSync': canCentralSync,
  };

  factory TeamMember.fromJson(Map<String, dynamic> json) {
    final member = TeamMember(
      id: (json['id'] ?? json['number'] ?? json['employeeNumber']).toString(),
      number: (json['number'] ?? json['employeeNumber'] ?? '').toString(),
      name: (json['name'] ?? json['displayName'] ?? '').toString(),
      role: (json['role'] ?? 'GARCOM').toString().toUpperCase(),
      active: json['active'] as bool? ?? true,
      pinHash: (json['pinHash'] ?? '').toString(),
      legacyPin: (json['pin'] ?? '').toString(),
      isMaster: json['isMaster'] as bool? ?? false,
      canTransfer: json['canTransfer'] as bool? ?? false,
      canCancel: json['canCancel'] as bool? ?? false,
      canDiscount: json['canDiscount'] as bool? ?? false,
      canManageProducts: json['canManageProducts'] as bool? ?? false,
      canReports: json['canReports'] as bool? ?? false,
      canCash: json['canCash'] as bool? ?? false,
      canDelivery: json['canDelivery'] as bool? ?? false,
      canInventory: json['canInventory'] as bool? ?? false,
      canKitchen: json['canKitchen'] as bool? ?? false,
      canIFood: json['canIFood'] as bool? ?? false,
      canSettings: json['canSettings'] as bool? ?? false,
      canBackup: json['canBackup'] as bool? ?? false,
      canFiscal: json['canFiscal'] as bool? ?? false,
      canDeliveryZones: json['canDeliveryZones'] as bool? ?? false,
      canCentralSync: json['canCentralSync'] as bool? ?? false,
    );
    member.normalizeRolePermissions();
    return member;
  }
}

class CashMovement {
  CashMovement({
    required this.id,
    required this.type,
    required this.amount,
    required this.note,
    required this.createdAt,
  });

  final String id;
  final String type;
  final double amount;
  final String note;
  final DateTime createdAt;

  Map<String, dynamic> toJson() => {
    'id': id,
    'type': type,
    'amount': amount,
    'note': note,
    'createdAt': createdAt.toIso8601String(),
  };

  factory CashMovement.fromJson(Map<String, dynamic> json) => CashMovement(
    id: json['id'] as String,
    type: json['type'] as String,
    amount: (json['amount'] as num).toDouble(),
    note: json['note'] as String,
    createdAt: DateTime.parse(json['createdAt'] as String),
  );
}

class StockMovement {
  StockMovement({
    required this.id,
    required this.productId,
    required this.type,
    required this.quantity,
    required this.note,
    required this.createdAt,
  });

  final String id;
  final String productId;
  final String type;
  final int quantity;
  final String note;
  final DateTime createdAt;

  Map<String, dynamic> toJson() => {
    'id': id,
    'productId': productId,
    'type': type,
    'quantity': quantity,
    'note': note,
    'createdAt': createdAt.toIso8601String(),
  };

  factory StockMovement.fromJson(Map<String, dynamic> json) => StockMovement(
    id: json['id'] as String,
    productId: json['productId'] as String,
    type: json['type'] as String,
    quantity: (json['quantity'] as num).round(),
    note: json['note'] as String? ?? '',
    createdAt: DateTime.parse(json['createdAt'] as String),
  );
}
