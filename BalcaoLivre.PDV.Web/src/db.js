const DB_NAME = "balcao_livre_pdv_web";
const DB_VERSION = 2;

const stores = [
  "products",
  "customers",
  "cash_sessions",
  "sales",
  "sale_items",
  "payments",
  "sync_queue",
  "sync_state",
  "terminal_settings",
  "users",
  "cash_movements"
];

let dbPromise;

export function openDb() {
  if (dbPromise) return dbPromise;

  dbPromise = new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);

    request.onupgradeneeded = () => {
      const db = request.result;

      for (const name of stores) {
        if (!db.objectStoreNames.contains(name)) {
          db.createObjectStore(name, { keyPath: "id" });
        }
      }

      const sales = request.transaction.objectStore("sales");
      if (!sales.indexNames.contains("by_created_at")) {
        sales.createIndex("by_created_at", "createdAt");
      }

      const queue = request.transaction.objectStore("sync_queue");
      if (!queue.indexNames.contains("by_status")) {
        queue.createIndex("by_status", "status");
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });

  return dbPromise;
}

function txDone(transaction) {
  return new Promise((resolve, reject) => {
    transaction.oncomplete = () => resolve();
    transaction.onerror = () => reject(transaction.error);
    transaction.onabort = () => reject(transaction.error);
  });
}

export async function getAll(storeName) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const request = db.transaction(storeName, "readonly").objectStore(storeName).getAll();
    request.onsuccess = () => resolve(request.result || []);
    request.onerror = () => reject(request.error);
  });
}

export async function getOne(storeName, id) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const request = db.transaction(storeName, "readonly").objectStore(storeName).get(id);
    request.onsuccess = () => resolve(request.result || null);
    request.onerror = () => reject(request.error);
  });
}

export async function put(storeName, value) {
  const db = await openDb();
  const transaction = db.transaction(storeName, "readwrite");
  transaction.objectStore(storeName).put(value);
  await txDone(transaction);
  return value;
}

export async function putMany(storeName, values) {
  const db = await openDb();
  const transaction = db.transaction(storeName, "readwrite");
  const store = transaction.objectStore(storeName);
  for (const value of values) store.put(value);
  await txDone(transaction);
  return values;
}

export async function deleteOne(storeName, id) {
  const db = await openDb();
  const transaction = db.transaction(storeName, "readwrite");
  transaction.objectStore(storeName).delete(id);
  await txDone(transaction);
}

export async function count(storeName) {
  const db = await openDb();
  return new Promise((resolve, reject) => {
    const request = db.transaction(storeName, "readonly").objectStore(storeName).count();
    request.onsuccess = () => resolve(request.result || 0);
    request.onerror = () => reject(request.error);
  });
}

export async function seedIfEmpty(initialProducts, initialSettings) {
  const productCount = await count("products");
  if (productCount === 0) {
    await putMany("products", initialProducts);
  }

  const settings = await getOne("terminal_settings", "main");
  if (!settings) {
    await put("terminal_settings", initialSettings);
  }
}

export async function saveSaleBundle({ sale, items, payment, event }) {
  const db = await openDb();
  const transaction = db.transaction(["sales", "sale_items", "payments", "sync_queue", "products"], "readwrite");

  transaction.objectStore("sales").put(sale);
  const itemStore = transaction.objectStore("sale_items");
  const productStore = transaction.objectStore("products");

  for (const item of items) {
    itemStore.put(item);
    productStore.put(item.productSnapshot);
  }

  transaction.objectStore("payments").put(payment);
  transaction.objectStore("sync_queue").put(event);
  await txDone(transaction);
}

export async function pendingEvents(limit = 20) {
  const all = await getAll("sync_queue");
  return all
    .filter((event) => event.status === "pending")
    .sort((a, b) => a.createdAt.localeCompare(b.createdAt))
    .slice(0, limit);
}

export async function markEventsSynced(events) {
  const now = new Date().toISOString();
  await putMany("sync_queue", events.map((event) => ({
    ...event,
    status: "synced",
    syncedAt: now
  })));
}
