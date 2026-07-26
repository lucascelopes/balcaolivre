import crypto from "crypto";

const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const ONLINE_INSTALLER_URL =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.27.exe";
const DEFAULT_ADMIN_ANALYTICS_URL = "https://balcaolivrepdv.onrender.com/api/public/analytics";
const BASIC_ONLINE_FEATURES = ["pdv", "caixa", "produtos", "mesas", "comandas", "estoque", "relatorios"];
const COMPLETE_ONLINE_FEATURES = [
  ...BASIC_ONLINE_FEATURES,
  "whatsapp",
  "cardapio",
  "garcom",
  "delivery",
  "mercado-pago",
  "nfce",
  "equipe",
  "entregadores",
  "ifood"
];

const basicMonthlyPlan = {
  name: "Balcao Livre PDV Online Basico",
  amount: 2999,
  interval: "month",
  periodAmount: 1,
  periodUnit: "months",
  clientKind: "windows-online"
};
const basicAnnualPlan = {
  name: "Balcao Livre PDV Online Basico",
  amount: 29990,
  interval: "year",
  periodAmount: 1,
  periodUnit: "years",
  clientKind: "windows-online"
};
const completeMonthlyPlan = {
  name: "Balcao Livre PDV Online Completo",
  amount: 9999,
  interval: "month",
  periodAmount: 1,
  periodUnit: "months",
  clientKind: "windows-online"
};
const completeAnnualPlan = {
  name: "Balcao Livre PDV Online Completo",
  amount: 99990,
  interval: "year",
  periodAmount: 1,
  periodUnit: "years",
  clientKind: "windows-online"
};

export const checkoutPlans = {
  "basico-mensal": basicMonthlyPlan,
  "basico-anual": basicAnnualPlan,
  "completo-mensal": completeMonthlyPlan,
  "completo-anual": completeAnnualPlan,
  "offline-mensal": basicMonthlyPlan,
  "offline-anual": basicAnnualPlan,
  "online-mensal": completeMonthlyPlan,
  "online-anual": completeAnnualPlan
};

export function getPlan(planId) {
  return checkoutPlans[normalizeCheckoutPlanId(planId)] || null;
}
export function billingFromPlanId(planId) {
  return String(planId || "").split("-")[1] || "";
}

export function featuresForPlanId(planId) {
  return isCompletePlanText(normalizeCheckoutPlanId(planId)) ? COMPLETE_ONLINE_FEATURES : BASIC_ONLINE_FEATURES;
}

export async function trackAdminAnalytics(type, data = {}) {
  const endpoint =
    process.env.BALCAO_ADMIN_ANALYTICS_URL ||
    process.env.ADMIN_ANALYTICS_URL ||
    DEFAULT_ADMIN_ANALYTICS_URL;

  if (!endpoint || !type) return;

  await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ type, ...data }),
    cache: "no-store"
  });
}

export function addPlanPeriod(date, plan) {
  const next = new Date(date);
  if (plan.periodUnit === "years") {
    next.setUTCFullYear(next.getUTCFullYear() + plan.periodAmount);
  } else {
    next.setUTCMonth(next.getUTCMonth() + plan.periodAmount);
  }
  return next;
}

export function createLicenseKey(expiresAt, planId) {
  const expiresText = activationExpirationText(expiresAt);
  const prefix = "ONL";
  const serial = `${prefix}${crypto.randomUUID().replaceAll("-", "").slice(0, 9).toUpperCase()}`;
  const signature = crypto
    .createHmac("sha256", LICENSE_SECRET)
    .update(`BLV|${expiresText}|${serial}`)
    .digest("hex")
    .toUpperCase()
    .slice(0, 10);
  return `BLV-${expiresText}-${serial}-${signature}`;
}

function activationExpirationText(date) {
  const pad = (value) => String(value).padStart(2, "0");
  return `${date.getUTCFullYear()}${pad(date.getUTCMonth() + 1)}${pad(date.getUTCDate())}${pad(date.getUTCHours())}${pad(date.getUTCMinutes())}`;
}

export async function fetchStripeSession(sessionId) {
  const secretKey = process.env.STRIPE_SECRET_KEY;
  if (!secretKey) {
    throw new Error("Pagamento nao configurado no servidor.");
  }

  const response = await fetch(`https://api.stripe.com/v1/checkout/sessions/${encodeURIComponent(sessionId)}?expand[]=customer`, {
    headers: { Authorization: `Bearer ${secretKey}` },
    cache: "no-store"
  });
  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.error?.message || "Nao foi possivel consultar o pagamento.");
  }

  return data;
}
export async function ensurePaidLicenseFromSession(session) {
  const sessionId = String(session?.id || "");
  const planId = String(session?.metadata?.plan || "");
  const plan = getPlan(planId);

  if (!sessionId || !plan) {
    throw new Error("Sessao de compra invalida.");
  }

  if (!isPaidSession(session)) {
    return { paid: false, license: null };
  }

  const existing = await findLicenseByCheckoutSession(sessionId);
  if (existing) {
    return { paid: true, license: normalizeLicense(existing) };
  }

  const now = new Date();
  const expiresAt = addPlanPeriod(now, plan);
  const email = String(session.customer_details?.email || session.customer?.email || "");
  const name = String(session.customer_details?.name || session.customer?.name || "Cliente pago pelo site");
  const licenseKey = createLicenseKey(expiresAt, planId);
  const profile = {
    source: "landing_checkout",
    checkout_session_id: sessionId,
    subscription_id: String(session.subscription || ""),
    payment_status: String(session.payment_status || ""),
    plan_id: planId,
    features: featuresForPlanId(planId),
    ifood_enabled: featuresForPlanId(planId).includes("ifood"),
    whatsapp_enabled: featuresForPlanId(planId).includes("whatsapp"),
    amount_total: Number(session.amount_total || plan.amount),
    currency: String(session.currency || "brl"),
    paid_at: now.toISOString()
  };

  const saved = await supabaseRequest("/rest/v1/bv_licenses?select=key,plan,email,customer_name,client_kind,expires_at,created_at", {
    method: "POST",
    headers: { Prefer: "return=representation" },
    body: JSON.stringify({
      key: licenseKey,
      status: "DISPONIVEL",
      plan: plan.name,
      customer_name: name,
      email,
      client_kind: plan.clientKind,
      profile,
      settings: {},
      metrics: {},
      expires_at: expiresAt.toISOString(),
      updated_at: now.toISOString()
    })
  });

  await supabaseRequest("/rest/v1/bv_license_events", {
    method: "POST",
    body: JSON.stringify({
      license_key: licenseKey,
      event_type: "checkout.paid",
      message: "Licenca paga gerada automaticamente pela landing.",
      payload: profile
    })
  }).catch(() => null);

  await trackAdminAnalytics("checkout.completed", {
    plan: planId,
    billing: billingFromPlanId(planId),
    amountCents: Number(session.amount_total || plan.amount),
    currency: String(session.currency || "BRL").toUpperCase(),
    checkoutSessionId: sessionId,
    stripeCustomerId: String(session.customer || ""),
    subscriptionId: String(session.subscription || ""),
    source: "stripe-checkout"
  }).catch(() => null);

  return { paid: true, license: normalizeLicense(saved?.[0]) };
}

export function isPaidSession(session) {
  return session?.payment_status === "paid" || session?.status === "complete";
}

export async function findLicenseByCheckoutSession(sessionId) {
  const query = `/rest/v1/bv_licenses?select=key,plan,email,customer_name,client_kind,expires_at,created_at&profile->>checkout_session_id=eq.${encodeURIComponent(sessionId)}&limit=1`;
  const data = await supabaseRequest(query, { method: "GET" });
  return Array.isArray(data) ? data[0] : null;
}

function normalizeLicense(license) {
  if (!license) return null;
  return {
    key: license.key,
    plan: license.plan,
    email: license.email,
    customerName: license.customer_name,
    clientKind: license.client_kind,
    installerUrl: installerUrlForClientKind(license.client_kind),
    expiresAt: license.expires_at,
    createdAt: license.created_at
  };
}

function installerUrlForClientKind(clientKind) {
  return ONLINE_INSTALLER_URL;
}

function isCompletePlanText(text) {
  return text.includes("completo")
    || text.includes("complete")
    || text.includes("profissional")
    || text.includes("premium")
    || text.includes("comercial")
    || text.includes("hibrido")
    || text.includes("integracoes")
    || text.includes("online-mensal")
    || text.includes("online-anual")
    || text.includes("99")
    || text.includes("149")
    || text.includes("139");
}

function normalizePlanText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function normalizeCheckoutPlanId(value) {
  const clean = normalizePlanText(value);
  if (checkoutPlans[clean]) return clean;
  const annual = clean.includes("anual") || clean.includes("annual") || clean.includes("year") || clean.includes("ano");
  if (isCompletePlanText(clean) || clean === "online") return annual ? "completo-anual" : "completo-mensal";
  if (clean.includes("basico") || clean.includes("basic") || clean.includes("offline")) return annual ? "basico-anual" : "basico-mensal";
  return clean;
}

async function supabaseRequest(path, init = {}) {
  const url = (process.env.SUPABASE_URL || process.env.BVPDV_SUPABASE_URL || "").replace(/\/$/, "");
  const key = process.env.SUPABASE_SERVICE_ROLE_KEY
    || process.env.BVPDV_SUPABASE_SERVICE_ROLE_KEY
    || process.env.BVPDV_SUPABASE_SECRET_KEY
    || process.env.SUPABASE_SECRET_KEY;

  if (!url || !key) {
    throw new Error("Banco de dados nao configurado no servidor.");
  }

  const response = await fetch(`${url}${path}`, {
    ...init,
    headers: {
      apikey: key,
      Authorization: `Bearer ${key}`,
      "Content-Type": "application/json",
      ...(init.headers || {})
    },
    cache: "no-store"
  });

  if (response.status === 204) return null;
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;

  if (!response.ok) {
    throw new Error(data?.message || data?.error || "Banco de dados recusou a operacao.");
  }

  return data;
}
