export const categories = [
  "BEBIDAS",
  "REFEICOES",
  "PIZZAS",
  "COMPOSICOES",
  "DELIVERY",
  "SOBREMESAS"
];

export const paymentMethods = ["Dinheiro", "Pix", "Credito", "Debito"];

export const initialProducts = [
  { id: "000001", code: "000001", name: "DOSE RED LABEL", category: "BEBIDAS", price: 12, cost: 6.4, stock: 24, minStock: 4, active: true },
  { id: "000002", code: "000002", name: "CERVEJAS", category: "BEBIDAS", price: 12, cost: 5.2, stock: 32, minStock: 8, active: true },
  { id: "000003", code: "000003", name: "REFRIGERANTE LATA", category: "BEBIDAS", price: 8, cost: 3.1, stock: 36, minStock: 10, active: true },
  { id: "000004", code: "000004", name: "SUCO NATURAL", category: "BEBIDAS", price: 14, cost: 5.8, stock: 18, minStock: 5, active: true },
  { id: "000005", code: "000005", name: "AGUA MINERAL", category: "BEBIDAS", price: 4, cost: 1.4, stock: 48, minStock: 12, active: true },
  { id: "000006", code: "000006", name: "MARMITA EXECUTIVA", category: "REFEICOES", price: 24.9, cost: 11.2, stock: 18, minStock: 4, active: true },
  { id: "000007", code: "000007", name: "FILE COM FRITAS", category: "REFEICOES", price: 39.9, cost: 19.8, stock: 10, minStock: 3, active: true },
  { id: "000008", code: "000008", name: "HAMBURGUER", category: "REFEICOES", price: 28, cost: 13.6, stock: 16, minStock: 4, active: true },
  { id: "000009", code: "000009", name: "COXINHA", category: "REFEICOES", price: 6, cost: 2.1, stock: 30, minStock: 8, active: true },
  { id: "000010", code: "000010", name: "PIZZA MUSSARELA", category: "PIZZAS", price: 58, cost: 27.5, stock: 8, minStock: 2, active: true },
  { id: "000011", code: "000011", name: "PIZZA CALABRESA", category: "PIZZAS", price: 62, cost: 29.6, stock: 8, minStock: 2, active: true },
  { id: "000012", code: "000012", name: "PIZZA BROTINHO", category: "PIZZAS", price: 36, cost: 17.2, stock: 10, minStock: 3, active: true },
  { id: "000013", code: "000013", name: "ADICIONAL BACON", category: "COMPOSICOES", price: 7, cost: 2.9, stock: 20, minStock: 5, active: true },
  { id: "000014", code: "000014", name: "ADICIONAL QUEIJO", category: "COMPOSICOES", price: 5, cost: 1.8, stock: 20, minStock: 5, active: true },
  { id: "000015", code: "000015", name: "TAXA DE ENTREGA", category: "DELIVERY", price: 8, cost: 0, stock: 999, minStock: 0, active: true },
  { id: "000016", code: "000016", name: "BROWNIE", category: "SOBREMESAS", price: 18, cost: 7.6, stock: 12, minStock: 3, active: true }
];

export const initialSettings = {
  id: "main",
  storeId: "loja_demo",
  terminalId: "caixa_01",
  ownerName: "Operador",
  businessName: "BALCAO LIVRE PDV",
  legalName: "BALCAO LIVRE DEMO",
  cnpj: "",
  phone: "(27) 98126-7551",
  city: "",
  state: "",
  address: "",
  logoName: "",
  visualToast: true,
  notificationSound: true,
  inAppVibration: false,
  notificationSoundKind: "PADRAO",
  autoPrintDelivery: false,
  autoPrintKitchen: false,
  printLayout: "PEQUENO",
  preferredPrinter: "",
  receiptQrEnabled: false,
  receiptQrKind: "PIX",
  receiptQrContent: "",
  autoCheckUpdates: true,
  adminSyncEnabled: true,
  syncEndpoint: "https://balcaolivrepdv.onrender.com",
  supabaseAuthEnabled: true,
  supabaseUrl: "",
  supabaseAnonKey: "",
  cashOpen: true,
  lastSyncAt: null
};
