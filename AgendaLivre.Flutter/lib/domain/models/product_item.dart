import 'id_generator.dart';
import 'json_helpers.dart';

class ProductItem {
  ProductItem({
    String? id,
    this.name = '',
    this.category = '',
    this.sku = '',
    this.supplier = '',
    this.costPrice = 0,
    this.price = 0,
    this.stockQuantity = 0,
    this.minimumStock = 0,
    this.notes = '',
    this.isActive = true,
    DateTime? createdAt,
  }) : id = agendaIdOrGenerate(id),
       createdAt = createdAt ?? DateTime.now();

  String id;
  String name;
  String category;
  String sku;
  String supplier;
  double costPrice;
  double price;
  int stockQuantity;
  int minimumStock;
  String notes;
  bool isActive;
  DateTime createdAt;

  factory ProductItem.fromJson(JsonMap json) => ProductItem(
    id: jsonString(json, 'Id'),
    name: jsonString(json, 'Name'),
    category: jsonString(json, 'Category'),
    sku: jsonString(json, 'Sku'),
    supplier: jsonString(json, 'Supplier'),
    costPrice: jsonDouble(json, 'CostPrice'),
    price: jsonDouble(json, 'Price'),
    stockQuantity: jsonInt(json, 'StockQuantity'),
    minimumStock: jsonInt(json, 'MinimumStock'),
    notes: jsonString(json, 'Notes'),
    isActive: jsonBool(json, 'IsActive', fallback: true),
    createdAt: jsonDateTime(json, 'CreatedAt', fallback: DateTime.now()),
  );

  JsonMap toJson() => <String, dynamic>{
    'Id': id,
    'Name': name,
    'Category': category,
    'Sku': sku,
    'Supplier': supplier,
    'CostPrice': costPrice,
    'Price': price,
    'StockQuantity': stockQuantity,
    'MinimumStock': minimumStock,
    'Notes': notes,
    'IsActive': isActive,
    'CreatedAt': createdAt.toIso8601String(),
  };
}
