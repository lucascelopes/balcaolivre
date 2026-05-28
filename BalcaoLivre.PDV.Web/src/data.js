export const categories = [
  "BEBIDAS",
  "REFEICOES",
  "PIZZAS",
  "COMPOSICOES",
  "DELIVERY",
  "SOBREMESAS"
];

export const paymentMethods = ["Dinheiro", "Pix", "Credito", "Debito"];

export const initialProducts = [];

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
  syncEndpoint: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/license",
  supabaseAuthEnabled: true,
  supabaseUrl: "",
  supabaseAnonKey: "",
  cashOpen: true,
  lastSyncAt: null
};
