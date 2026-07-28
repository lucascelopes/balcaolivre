export type BillingInterval = "month" | "year";
export type BalcaoPlanCode =
  | "basico-mensal"
  | "basico-anual"
  | "completo-mensal"
  | "completo-anual";

export type BalcaoPlan = {
  code: BalcaoPlanCode;
  name: string;
  tier: "ESSENTIAL" | "COMPLETE";
  interval: BillingInterval;
  amountCents: number;
  priceEnv: string;
  modules: string[];
  desktopSeats: number;
  mobileSeats: number;
  mercadopagoPoint: boolean;
  machineFulfillment: boolean;
  reportsLevel: "BASIC" | "ADVANCED";
};

const ESSENTIAL_MODULES = [
  "PDV",
  "SALAO",
  "MESAS",
  "COMANDAS",
  "PRODUTOS",
  "CLIENTES",
  "CAIXA",
  "RELATORIOS_BASICOS",
];

const COMPLETE_MODULES = [
  ...ESSENTIAL_MODULES,
  "BALCAO",
  "DELIVERY",
  "CARDAPIO_DIGITAL",
  "WHATSAPP",
  "IFOOD",
  "GARCOM",
  "COZINHA",
  "ESTOQUE",
  "NUVEM",
  "RELATORIOS_AVANCADOS",
  "MERCADOPAGO_POINT",
];

export const BALCAO_PLANS: Record<BalcaoPlanCode, BalcaoPlan> = {
  "basico-mensal": {
    code: "basico-mensal",
    name: "Balcão Livre Essencial",
    tier: "ESSENTIAL",
    interval: "month",
    amountCents: 4_990,
    priceEnv: "STRIPE_PRICE_BALCAO_ESSENCIAL_MONTH",
    modules: ESSENTIAL_MODULES,
    desktopSeats: 1,
    mobileSeats: 1,
    mercadopagoPoint: false,
    machineFulfillment: false,
    reportsLevel: "BASIC",
  },
  "basico-anual": {
    code: "basico-anual",
    name: "Balcão Livre Essencial Anual",
    tier: "ESSENTIAL",
    interval: "year",
    amountCents: 59_880,
    priceEnv: "STRIPE_PRICE_BALCAO_ESSENCIAL_YEAR",
    modules: [...ESSENTIAL_MODULES, "MERCADOPAGO_POINT"],
    desktopSeats: 1,
    mobileSeats: 1,
    mercadopagoPoint: true,
    machineFulfillment: true,
    reportsLevel: "BASIC",
  },
  "completo-mensal": {
    code: "completo-mensal",
    name: "Balcão Livre Completo",
    tier: "COMPLETE",
    interval: "month",
    amountCents: 9_990,
    priceEnv: "STRIPE_PRICE_BALCAO_COMPLETO_MONTH",
    modules: COMPLETE_MODULES,
    desktopSeats: 1,
    mobileSeats: 1,
    mercadopagoPoint: true,
    machineFulfillment: false,
    reportsLevel: "ADVANCED",
  },
  "completo-anual": {
    code: "completo-anual",
    name: "Balcão Livre Completo Anual",
    tier: "COMPLETE",
    interval: "year",
    amountCents: 119_880,
    priceEnv: "STRIPE_PRICE_BALCAO_COMPLETO_YEAR",
    modules: COMPLETE_MODULES,
    desktopSeats: 1,
    mobileSeats: 1,
    mercadopagoPoint: true,
    machineFulfillment: true,
    reportsLevel: "ADVANCED",
  },
};

export const EXTRA_DESKTOP_SEAT = {
  month: {
    amountCents: 3_990,
    priceEnv: "STRIPE_PRICE_BALCAO_EXTRA_DESKTOP_MONTH",
  },
  year: {
    amountCents: 47_880,
    priceEnv: "STRIPE_PRICE_BALCAO_EXTRA_DESKTOP_YEAR",
  },
} as const;

export function isBalcaoPlanCode(value: unknown): value is BalcaoPlanCode {
  return typeof value === "string" && value in BALCAO_PLANS;
}

export function getBalcaoPlan(value: unknown): BalcaoPlan {
  if (!isBalcaoPlanCode(value)) {
    throw new Error("Plano do Balcão Livre inválido.");
  }
  return BALCAO_PLANS[value];
}

export function getStripePriceId(plan: BalcaoPlan): string {
  const priceId = (Deno.env.get(plan.priceEnv) || "").trim();
  if (!priceId.startsWith("price_")) {
    throw new Error(`Preço oficial do Stripe não configurado: ${plan.priceEnv}.`);
  }
  return priceId;
}

export function getExtraSeatPriceId(interval: BillingInterval): string {
  const envName = EXTRA_DESKTOP_SEAT[interval].priceEnv;
  const priceId = (Deno.env.get(envName) || "").trim();
  if (!priceId.startsWith("price_")) {
    throw new Error(`Preço oficial do Stripe não configurado: ${envName}.`);
  }
  return priceId;
}

export function parseExtraDesktopQuantity(value: unknown): number {
  const quantity = Number(value || 0);
  if (!Number.isInteger(quantity) || quantity < 0 || quantity > 50) {
    throw new Error("Quantidade de caixas adicionais inválida.");
  }
  return quantity;
}
