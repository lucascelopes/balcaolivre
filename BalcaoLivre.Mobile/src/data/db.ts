import * as SQLite from "expo-sqlite";
import { CashMovement, Order, OrderItem, Payment, Product, Settings, Snapshot, SyncEvent } from "../types";
import { nowIso } from "../utils/format";
import { newId } from "../utils/id";

type Database = Awaited<ReturnType<typeof SQLite.openDatabaseAsync>>;

let dbPromise: Promise<Database> | null = null;

const defaultSettings: Settings = {
  id: "main",
  storeId: "loja_mobile",
  terminalId: "mobile_01",
  adminApiUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/license",
  ifoodApiUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood",
  windowsBridgeUrl: "http://192.168.1.100:5050",
  printMode: "WINDOWS_BRIDGE",
  printerAddress: "",
  autoSync: 1,
  cashOpen: 0,
  lastSyncAt: ""
};

export async function database() {
  if (!dbPromise) {
    dbPromise = SQLite.openDatabaseAsync("balcao_livre_mobile.db");
  }

  const db = await dbPromise;
  await migrate(db);
  return db;
}

async function migrate(db: Database) {
  await db.execAsync(`
    PRAGMA journal_mode = WAL;
    CREATE TABLE IF NOT EXISTS settings (
      id TEXT PRIMARY KEY NOT NULL,
      value TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS products (
      id TEXT PRIMARY KEY NOT NULL,
      code TEXT NOT NULL UNIQUE,
      name TEXT NOT NULL,
      category TEXT NOT NULL,
      price REAL NOT NULL,
      costPrice REAL NOT NULL,
      stock REAL NOT NULL,
      minStock REAL NOT NULL,
      active INTEGER NOT NULL,
      destination TEXT NOT NULL,
      imageUrl TEXT NOT NULL,
      updatedAt TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS orders (
      id TEXT PRIMARY KEY NOT NULL,
      kind TEXT NOT NULL,
      number TEXT NOT NULL,
      customerName TEXT NOT NULL,
      waiter TEXT NOT NULL,
      status TEXT NOT NULL,
      total REAL NOT NULL,
      notes TEXT NOT NULL,
      source TEXT NOT NULL,
      createdAt TEXT NOT NULL,
      updatedAt TEXT NOT NULL,
      closedAt TEXT NOT NULL
    );
    CREATE UNIQUE INDEX IF NOT EXISTS idx_orders_open_kind_number
      ON orders(kind, number, closedAt);
    CREATE TABLE IF NOT EXISTS order_items (
      id TEXT PRIMARY KEY NOT NULL,
      orderId TEXT NOT NULL,
      productId TEXT NOT NULL,
      code TEXT NOT NULL,
      name TEXT NOT NULL,
      quantity REAL NOT NULL,
      unitPrice REAL NOT NULL,
      total REAL NOT NULL,
      note TEXT NOT NULL,
      destination TEXT NOT NULL,
      status TEXT NOT NULL,
      createdAt TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS payments (
      id TEXT PRIMARY KEY NOT NULL,
      orderId TEXT NOT NULL,
      method TEXT NOT NULL,
      amount REAL NOT NULL,
      externalId TEXT NOT NULL,
      status TEXT NOT NULL,
      createdAt TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS cash_movements (
      id TEXT PRIMARY KEY NOT NULL,
      type TEXT NOT NULL,
      amount REAL NOT NULL,
      method TEXT NOT NULL,
      notes TEXT NOT NULL,
      createdAt TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS sync_events (
      id TEXT PRIMARY KEY NOT NULL,
      type TEXT NOT NULL,
      payload TEXT NOT NULL,
      status TEXT NOT NULL,
      createdAt TEXT NOT NULL,
      syncedAt TEXT NOT NULL,
      error TEXT NOT NULL
    );
  `);

  const row = await db.getFirstAsync<{ value: string }>("SELECT value FROM settings WHERE id = ?", ["main"]);
  if (!row) {
    await db.runAsync("INSERT OR REPLACE INTO settings (id, value) VALUES (?, ?)", ["main", JSON.stringify(defaultSettings)]);
    await seedProducts(db);
  }
}

async function seedProducts(db: Database) {
  const row = await db.getFirstAsync<{ total: number }>("SELECT COUNT(*) as total FROM products");
  const count = row?.total ?? 0;
  if (count > 0) return;
  const base = nowIso();
  const rows: Product[] = [
    productSeed("100001", "BL Burger da Casa", "HAMBURGERS", 29.9, 30, "COZINHA"),
    productSeed("200001", "Batata Frita Media", "PORCOES", 18.9, 55, "COZINHA"),
    productSeed("300001", "Coca-Cola Lata 350ml", "BEBIDAS", 7, 96, "BAR"),
    productSeed("300003", "Agua Mineral 500ml", "BEBIDAS", 4.5, 88, "BAR"),
    productSeed("500001", "Brownie com Sorvete", "SOBREMESAS", 19.9, 22, "COZINHA"),
    productSeed("600002", "Bacon Extra", "ADICIONAIS", 6, 37, "COZINHA")
  ].map((item) => ({ ...item, updatedAt: base }));

  await db.withTransactionAsync(async () => {
    for (const item of rows) {
      await db.runAsync(
        `INSERT OR REPLACE INTO products
         (id, code, name, category, price, costPrice, stock, minStock, active, destination, imageUrl, updatedAt)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
        [
          item.id,
          item.code,
          item.name,
          item.category,
          item.price,
          item.costPrice,
          item.stock,
          item.minStock,
          item.active,
          item.destination,
          item.imageUrl,
          item.updatedAt
        ]
      );
    }
  });
}

function productSeed(code: string, name: string, category: string, price: number, stock: number, destination: string): Product {
  return {
    id: newId("prd"),
    code,
    name,
    category,
    price,
    costPrice: 0,
    stock,
    minStock: 5,
    active: 1,
    destination,
    imageUrl: "",
    updatedAt: nowIso()
  };
}

export async function getSettings(): Promise<Settings> {
  const db = await database();
  const row = await db.getFirstAsync<{ value: string }>("SELECT value FROM settings WHERE id = ?", ["main"]);
  return row ? { ...defaultSettings, ...JSON.parse(row.value) } : defaultSettings;
}

export async function saveSettings(settings: Settings) {
  const db = await database();
  await db.runAsync("INSERT OR REPLACE INTO settings (id, value) VALUES (?, ?)", ["main", JSON.stringify(settings)]);
}

export async function countProducts() {
  const db = await database();
  const row = await db.getFirstAsync<{ total: number }>("SELECT COUNT(*) as total FROM products");
  return row?.total ?? 0;
}

export async function listProducts(query = ""): Promise<Product[]> {
  const db = await database();
  const text = `%${query.trim()}%`;
  return db.getAllAsync<Product>(
    `SELECT * FROM products
     WHERE active = 1 AND (? = '%%' OR code LIKE ? OR name LIKE ? OR category LIKE ?)
     ORDER BY category, name`,
    [text, text, text, text]
  );
}

export async function putProduct(product: Product, enqueue = true) {
  const db = await database();
  await db.runAsync(
    `INSERT OR REPLACE INTO products
     (id, code, name, category, price, costPrice, stock, minStock, active, destination, imageUrl, updatedAt)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    [
      product.id,
      product.code,
      product.name,
      product.category,
      product.price,
      product.costPrice,
      product.stock,
      product.minStock,
      product.active,
      product.destination,
      product.imageUrl,
      product.updatedAt
    ]
  );
  if (enqueue) await enqueueEvent("product.upserted", product);
}

export async function adjustStock(product: Product, quantity: number, reason: string) {
  const next = { ...product, stock: product.stock + quantity, updatedAt: nowIso() };
  await putProduct(next, false);
  await enqueueEvent("stock.adjusted", { code: product.code, quantity, reason, stock: next.stock });
}

export async function listOrders(kind?: string): Promise<Order[]> {
  const db = await database();
  if (kind) {
    return db.getAllAsync<Order>("SELECT * FROM orders WHERE kind = ? ORDER BY updatedAt DESC LIMIT 80", [kind]);
  }
  return db.getAllAsync<Order>("SELECT * FROM orders ORDER BY updatedAt DESC LIMIT 120");
}

export async function orderItems(orderId: string): Promise<OrderItem[]> {
  const db = await database();
  return db.getAllAsync<OrderItem>("SELECT * FROM order_items WHERE orderId = ? ORDER BY createdAt", [orderId]);
}

export async function openOrder(kind: Order["kind"], number: string, waiter = "1", customerName = "") {
  const db = await database();
  const existing = await db.getFirstAsync<Order>(
    "SELECT * FROM orders WHERE kind = ? AND number = ? AND closedAt = '' LIMIT 1",
    [kind, number]
  );
  if (existing) return existing;

  const order: Order = {
    id: newId("ord"),
    kind,
    number,
    waiter,
    customerName,
    status: "NOVO",
    total: 0,
    notes: "",
    source: "mobile",
    createdAt: nowIso(),
    updatedAt: nowIso(),
    closedAt: ""
  };
  await db.runAsync(
    `INSERT INTO orders (id, kind, number, customerName, waiter, status, total, notes, source, createdAt, updatedAt, closedAt)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    [order.id, order.kind, order.number, order.customerName, order.waiter, order.status, order.total, order.notes, order.source, order.createdAt, order.updatedAt, order.closedAt]
  );
  await enqueueEvent("order.opened", order);
  return order;
}

export async function addProductToOrder(order: Order, product: Product, quantity: number, note = "") {
  const db = await database();
  const item: OrderItem = {
    id: newId("itm"),
    orderId: order.id,
    productId: product.id,
    code: product.code,
    name: product.name,
    quantity,
    unitPrice: product.price,
    total: product.price * quantity,
    note,
    destination: product.destination,
    status: "PREPARO",
    createdAt: nowIso()
  };
  await db.withTransactionAsync(async () => {
    await db.runAsync(
      `INSERT INTO order_items
       (id, orderId, productId, code, name, quantity, unitPrice, total, note, destination, status, createdAt)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [item.id, item.orderId, item.productId, item.code, item.name, item.quantity, item.unitPrice, item.total, item.note, item.destination, item.status, item.createdAt]
    );
    await db.runAsync("UPDATE products SET stock = stock - ?, updatedAt = ? WHERE id = ?", [quantity, nowIso(), product.id]);
    await db.runAsync("UPDATE orders SET total = total + ?, status = ?, updatedAt = ? WHERE id = ?", [item.total, "PREPARO", nowIso(), order.id]);
  });
  await enqueueEvent("order.item_added", { order, item });
  return item;
}

export async function closeOrder(order: Order, method: Payment["method"], amount: number) {
  const db = await database();
  const payment: Payment = {
    id: newId("pay"),
    orderId: order.id,
    method,
    amount,
    externalId: "",
    status: "APPROVED",
    createdAt: nowIso()
  };
  await db.withTransactionAsync(async () => {
    await db.runAsync(
      "INSERT INTO payments (id, orderId, method, amount, externalId, status, createdAt) VALUES (?, ?, ?, ?, ?, ?, ?)",
      [payment.id, payment.orderId, payment.method, payment.amount, payment.externalId, payment.status, payment.createdAt]
    );
    await db.runAsync("UPDATE orders SET status = ?, closedAt = ?, updatedAt = ? WHERE id = ?", ["FECHADO", nowIso(), nowIso(), order.id]);
    await db.runAsync(
      "INSERT INTO cash_movements (id, type, amount, method, notes, createdAt) VALUES (?, ?, ?, ?, ?, ?)",
      [newId("cash"), "SALE", amount, method, `Pedido ${order.number}`, nowIso()]
    );
  });
  await enqueueEvent("payment.created", payment);
  await enqueueEvent("order.closed", { orderId: order.id, kind: order.kind, number: order.number, method, amount });
  return payment;
}

export async function setCashOpen(open: boolean, amount = 0) {
  const settings = await getSettings();
  await saveSettings({ ...settings, cashOpen: open ? 1 : 0 });
  await insertCashMovement(open ? "OPEN" : "CLOSE", amount, "DINHEIRO", open ? "Abertura de caixa" : "Fechamento de caixa");
  await enqueueEvent(open ? "cash.opened" : "cash.closed", { amount });
}

export async function insertCashMovement(type: CashMovement["type"], amount: number, method: string, notes: string) {
  const db = await database();
  const row: CashMovement = { id: newId("cash"), type, amount, method, notes, createdAt: nowIso() };
  await db.runAsync(
    "INSERT INTO cash_movements (id, type, amount, method, notes, createdAt) VALUES (?, ?, ?, ?, ?, ?)",
    [row.id, row.type, row.amount, row.method, row.notes, row.createdAt]
  );
  return row;
}

export async function cashMovements(): Promise<CashMovement[]> {
  const db = await database();
  return db.getAllAsync<CashMovement>("SELECT * FROM cash_movements ORDER BY createdAt DESC LIMIT 100");
}

export async function enqueueEvent(type: string, payload: Record<string, unknown>) {
  const db = await database();
  const event: SyncEvent = {
    id: newId("evt"),
    type,
    payload,
    status: "pending",
    createdAt: nowIso(),
    syncedAt: "",
    error: ""
  };
  await db.runAsync(
    "INSERT INTO sync_events (id, type, payload, status, createdAt, syncedAt, error) VALUES (?, ?, ?, ?, ?, ?, ?)",
    [event.id, event.type, JSON.stringify(event.payload), event.status, event.createdAt, event.syncedAt, event.error]
  );
  return event;
}

export async function pendingEvents(limit = 50): Promise<SyncEvent[]> {
  const db = await database();
  const rows = await db.getAllAsync<Omit<SyncEvent, "payload"> & { payload: string }>(
    "SELECT * FROM sync_events WHERE status = 'pending' ORDER BY createdAt LIMIT ?",
    [limit]
  );
  return rows.map((row) => ({ ...row, payload: JSON.parse(row.payload || "{}") }));
}

export async function markEventsSynced(ids: string[]) {
  if (ids.length === 0) return;
  const db = await database();
  const syncedAt = nowIso();
  await db.withTransactionAsync(async () => {
    for (const id of ids) {
      await db.runAsync("UPDATE sync_events SET status = 'synced', syncedAt = ?, error = '' WHERE id = ?", [syncedAt, id]);
    }
  });
}

export async function buildSnapshot(): Promise<Snapshot> {
  const db = await database();
  return {
    settings: await getSettings(),
    products: await db.getAllAsync<Product>("SELECT * FROM products ORDER BY category, name"),
    orders: await db.getAllAsync<Order>("SELECT * FROM orders ORDER BY updatedAt DESC LIMIT 300"),
    orderItems: await db.getAllAsync<OrderItem>("SELECT * FROM order_items ORDER BY createdAt DESC LIMIT 1200"),
    payments: await db.getAllAsync<Payment>("SELECT * FROM payments ORDER BY createdAt DESC LIMIT 500"),
    cashMovements: await cashMovements()
  };
}

export async function applySnapshot(snapshot: Partial<Snapshot>) {
  const db = await database();
  await db.withTransactionAsync(async () => {
    if (snapshot.settings) {
      await db.runAsync("INSERT OR REPLACE INTO settings (id, value) VALUES (?, ?)", ["main", JSON.stringify({ ...defaultSettings, ...snapshot.settings })]);
    }
    for (const product of snapshot.products ?? []) {
      await db.runAsync(
        `INSERT OR REPLACE INTO products
         (id, code, name, category, price, costPrice, stock, minStock, active, destination, imageUrl, updatedAt)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
        [
          product.id,
          product.code,
          product.name,
          product.category,
          product.price,
          product.costPrice,
          product.stock,
          product.minStock,
          product.active,
          product.destination,
          product.imageUrl,
          product.updatedAt
        ]
      );
    }
  });
}
