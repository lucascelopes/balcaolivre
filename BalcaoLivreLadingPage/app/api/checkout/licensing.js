import crypto from "crypto";

const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const OFFLINE_INSTALLER_URL =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows/BalcaoLivrePDV-Setup-1.0.2026.exe";
const ONLINE_INSTALLER_URL =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.exe";

export const checkoutPlans = {
  "offline-mensal": {
    name: "Balcao Livre PDV Para Restaurantes Offline",
    amount: 1700,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-offline"
  },
  "offline-anual": {
    name: "Balcao Livre PDV Para Restaurantes Offline",
    amount: 20000,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-offline"
  },
  "online-mensal": {
    name: "Balcao Livre PDV Restaurante Hibrido Online",
    amount: 13900,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-online"
  },
  "online-anual": {
    name: "Balcao Livre PDV Restaurante Hibrido Online",
    amount: 139000,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-online"
  },
  "complete-mensal": {
    name: "Balcao Livre PDV Restaurantes Completo com Integracoes",
    amount: 17900,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-online"
  },
  "complete-anual": {
    name: "Balcao Livre PDV Restaurantes Completo com Integracoes",
    amount: 179000,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-online"
  }
};

export function getPlan(planId) {
  return checkoutPlans[String(planId || "").toLowerCase()] || null;
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
  const prefix = String(planId || "").includes("online") || String(planId || "").includes("complete") ? "ONL" : "OFF";
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
  return String(clientKind || "").toLowerCase().includes("online")
    ? ONLINE_INSTALLER_URL
    : OFFLINE_INSTALLER_URL;
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
