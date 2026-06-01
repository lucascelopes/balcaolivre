export type OrderKind = "MESA" | "BALCAO" | "DELIVERY";
export type OrderStatus = "LIVRE" | "NOVO" | "PREPARO" | "PREPARANDO" | "DESPACHADO" | "ENTREGUE" | "CANCELADO" | "FECHADO";
export type PaymentMethod = "DINHEIRO" | "PIX" | "CREDITO" | "DEBITO" | "MERCADO_PAGO";

export type Session = {
  licenseKey: string;
  machineHash: string;
  machineCode: string;
  adminApiUrl: string;
  ifoodApiUrl: string;
  plan: string;
  expiresAt: string;
  profile: StoreProfile;
};

export type StoreProfile = {
  email: string;
  businessName: string;
  ownerName: string;
  document: string;
  phone: string;
  city: string;
  state: string;
};

export type Settings = {
  id: "main";
  storeId: string;
  terminalId: string;
  adminApiUrl: string;
  ifoodApiUrl: string;
  windowsBridgeUrl: string;
  printMode: "WINDOWS_BRIDGE" | "ESC_POS_NETWORK" | "ESC_POS_BLUETOOTH";
  printerAddress: string;
  autoSync: number;
  cashOpen: number;
  lastSyncAt: string;
};

export type Product = {
  id: string;
  code: string;
  name: string;
  category: string;
  price: number;
  costPrice: number;
  stock: number;
  minStock: number;
  active: number;
  destination: string;
  imageUrl: string;
  updatedAt: string;
};

export type Order = {
  id: string;
  kind: OrderKind;
  number: string;
  customerName: string;
  waiter: string;
  status: OrderStatus;
  total: number;
  notes: string;
  source: string;
  createdAt: string;
  updatedAt: string;
  closedAt: string;
};

export type OrderItem = {
  id: string;
  orderId: string;
  productId: string;
  code: string;
  name: string;
  quantity: number;
  unitPrice: number;
  total: number;
  note: string;
  destination: string;
  status: OrderStatus;
  createdAt: string;
};

export type Payment = {
  id: string;
  orderId: string;
  method: PaymentMethod;
  amount: number;
  externalId: string;
  status: string;
  createdAt: string;
};

export type CashMovement = {
  id: string;
  type: "OPEN" | "CLOSE" | "SALE" | "SUPPLY" | "WITHDRAW";
  amount: number;
  method: string;
  notes: string;
  createdAt: string;
};

export type SyncEvent = {
  id: string;
  type: string;
  payload: Record<string, unknown>;
  status: "pending" | "synced" | "error";
  createdAt: string;
  syncedAt: string;
  error: string;
};

export type Snapshot = {
  settings: Settings;
  products: Product[];
  orders: Order[];
  orderItems: OrderItem[];
  payments: Payment[];
  cashMovements: CashMovement[];
};
