import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const STRIPE_CHECKOUT_URL = "https://api.stripe.com/v1/checkout/sessions";
const STRIPE_API_URL = "https://api.stripe.com/v1";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type, stripe-signature",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type CheckoutPlan = {
  name: string;
  amount: number;
  interval: "month" | "year";
  periodAmount: number;
  periodUnit: "months" | "years";
  clientKind: string;
};

const checkoutPlans: Record<string, CheckoutPlan> = {
  "offline-mensal": {
    name: "Balcao Livre PDV Para Restaurantes Offline",
    amount: 1700,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-offline",
  },
  "offline-anual": {
    name: "Balcao Livre PDV Para Restaurantes Offline",
    amount: 20000,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-offline",
  },
  "online-mensal": {
    name: "Balcao Livre PDV Restaurante Hibrido Online",
    amount: 13900,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-online",
  },
  "online-anual": {
    name: "Balcao Livre PDV Restaurante Hibrido Online",
    amount: 139000,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-online",
  },
  "complete-mensal": {
    name: "Balcao Livre PDV Restaurantes Completo com Integracoes",
    amount: 17900,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-online",
  },
  "complete-anual": {
    name: "Balcao Livre PDV Restaurantes Completo com Integracoes",
    amount: 179000,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-online",
  },
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);

    if (url.pathname.endsWith("/webhook")) {
      return await handleWebhook(req);
    }

    if (url.pathname.endsWith("/status")) {
      return await handleStatus(url);
    }

    if (req.method === "POST" || req.method === "GET") {
      return await handleCheckout(req, url);
    }

    return json({ ok: false, message: "Metodo nao permitido." }, 405);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function handleCheckout(req: Request, url: URL) {
  const planId = await planFromRequest(req, url);
  const plan = checkoutPlans[planId];

  if (!plan) {
    return json({ ok: false, message: "Plano invalido." }, 400);
  }

  const origin = stringValue(req.headers.get("origin"))
    || stringValue(Deno.env.get("BALCAO_CHECKOUT_SUCCESS_URL")).replace(/\/$/, "")
    || `${url.protocol}//${url.host}`;
  const successUrl = `${origin}/?checkout=sucesso&session_id={CHECKOUT_SESSION_ID}`;
  const cancelUrl = `${origin}/#planos`;

  const params = new URLSearchParams({
    mode: "subscription",
    client_reference_id: planId,
    "metadata[plan]": planId,
    "line_items[0][quantity]": "1",
    "line_items[0][price_data][currency]": "brl",
    "line_items[0][price_data][unit_amount]": String(plan.amount),
    "line_items[0][price_data][recurring][interval]": plan.interval,
    "line_items[0][price_data][product_data][name]": plan.name,
    allow_promotion_codes: "true",
    success_url: successUrl,
    cancel_url: cancelUrl,
  });

  const response = await fetch(STRIPE_CHECKOUT_URL, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${requiredEnv("STRIPE_SECRET_KEY")}`,
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body: params,
  });
  const data = await response.json();

  if (!response.ok || !data.url) {
    return json({ ok: false, message: data.error?.message || "Nao foi possivel abrir a compra." }, response.status || 500);
  }

  return new Response(null, {
    status: 303,
    headers: { ...corsHeaders, Location: data.url, "Cache-Control": "no-store" },
  });
}

async function handleStatus(url: URL) {
  const sessionId = stringValue(url.searchParams.get("session_id"));
  if (!sessionId) {
    return json({ ok: false, message: "Sessao nao informada." }, 400);
  }

  const session = await fetchStripeSession(sessionId);
  const result = await ensurePaidLicenseFromSession(session);

  if (!result.paid) {
    return json({ ok: false, paid: false, message: "Pagamento ainda nao confirmado." }, 202);
  }

  return json({ ok: true, paid: true, license: result.license });
}

async function handleWebhook(req: Request) {
  const secret = requiredEnv("STRIPE_WEBHOOK_SECRET");
  const signature = stringValue(req.headers.get("stripe-signature"));
  const payload = await req.text();

  if (!await isValidStripeSignature(payload, signature, secret)) {
    return json({ ok: false, message: "Assinatura invalida." }, 400);
  }

  const event = JSON.parse(payload);
  if (event.type === "checkout.session.completed" || event.type === "checkout.session.async_payment_succeeded") {
    await ensurePaidLicenseFromSession(event.data.object);
  }

  return json({ received: true });
}

async function ensurePaidLicenseFromSession(session: Record<string, unknown>) {
  const sessionId = stringValue(session.id);
  const metadata = session.metadata as Record<string, unknown> | undefined;
  const planId = stringValue(metadata?.plan || session.client_reference_id);
  const plan = checkoutPlans[planId];

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
  const customerDetails = session.customer_details as Record<string, unknown> | undefined;
  const email = stringValue(customerDetails?.email);
  const name = stringValue(customerDetails?.name) || "Cliente pago pelo site";
  const licenseKey = await createLicenseKey(expiresAt, planId);
  const profile = {
    source: "landing_checkout",
    checkout_session_id: sessionId,
    subscription_id: stringValue(session.subscription),
    payment_status: stringValue(session.payment_status),
    plan_id: planId,
    amount_total: Number(session.amount_total || plan.amount),
    currency: stringValue(session.currency || "brl"),
    paid_at: now.toISOString(),
  };

  const { data, error } = await serviceClient()
    .from("bv_licenses")
    .insert({
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
      updated_at: now.toISOString(),
    })
    .select("key,plan,email,customer_name,expires_at,created_at")
    .single();

  if (error) {
    throw new Error(`Supabase recusou salvar a chave: ${error.message}`);
  }

  await serviceClient().from("bv_license_events").insert({
    license_key: licenseKey,
    event_type: "checkout.paid",
    message: "Licenca paga gerada automaticamente pela landing.",
    payload: profile,
  });

  return { paid: true, license: normalizeLicense(data) };
}

async function fetchStripeSession(sessionId: string) {
  const response = await fetch(`${STRIPE_API_URL}/checkout/sessions/${encodeURIComponent(sessionId)}`, {
    headers: { Authorization: `Bearer ${requiredEnv("STRIPE_SECRET_KEY")}` },
  });
  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.error?.message || "Nao foi possivel consultar o pagamento.");
  }

  return data;
}

async function findLicenseByCheckoutSession(sessionId: string) {
  const { data, error } = await serviceClient()
    .from("bv_licenses")
    .select("key,plan,email,customer_name,expires_at,created_at")
    .eq("profile->>checkout_session_id", sessionId)
    .limit(1)
    .maybeSingle();

  if (error) {
    throw new Error(`Supabase recusou consultar a chave: ${error.message}`);
  }

  return data;
}

function isPaidSession(session: Record<string, unknown>) {
  return session.payment_status === "paid" || session.status === "complete";
}

function normalizeLicense(license: Record<string, unknown> | null) {
  if (!license) return null;
  return {
    key: license.key,
    plan: license.plan,
    email: license.email,
    customerName: license.customer_name,
    expiresAt: license.expires_at,
    createdAt: license.created_at,
  };
}

function addPlanPeriod(date: Date, plan: CheckoutPlan) {
  const next = new Date(date);
  if (plan.periodUnit === "years") {
    next.setUTCFullYear(next.getUTCFullYear() + plan.periodAmount);
  } else {
    next.setUTCMonth(next.getUTCMonth() + plan.periodAmount);
  }
  return next;
}

async function createLicenseKey(expiresAt: Date, planId: string) {
  const expiresText = activationExpirationText(expiresAt);
  const prefix = planId.includes("online") || planId.includes("complete") ? "ONL" : "OFF";
  const serial = `${prefix}${crypto.randomUUID().replaceAll("-", "").slice(0, 9).toUpperCase()}`;
  const signature = (await hmacHex(LICENSE_SECRET, `BLV|${expiresText}|${serial}`)).slice(0, 10);
  return `BLV-${expiresText}-${serial}-${signature}`;
}

function activationExpirationText(date: Date) {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getUTCFullYear()}${pad(date.getUTCMonth() + 1)}${pad(date.getUTCDate())}${pad(date.getUTCHours())}${pad(date.getUTCMinutes())}`;
}

async function isValidStripeSignature(payload: string, header: string, secret: string) {
  const values = Object.fromEntries(header.split(",").map((part) => {
    const [key, value] = part.split("=");
    return [key, value];
  }));
  const timestamp = values.t;
  const expected = values.v1;

  if (!timestamp || !expected) return false;

  const digest = await hmacHex(secret, `${timestamp}.${payload}`);
  return safeEqualHex(digest.toLowerCase(), expected.toLowerCase());
}

async function hmacHex(secret: string, message: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
  return Array.from(new Uint8Array(signature))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

function safeEqualHex(a: string, b: string) {
  if (a.length !== b.length) return false;
  let result = 0;
  for (let index = 0; index < a.length; index += 1) {
    result |= a.charCodeAt(index) ^ b.charCodeAt(index);
  }
  return result === 0;
}

async function planFromRequest(req: Request, url: URL) {
  if (req.method === "GET") {
    return stringValue(url.searchParams.get("plan")).toLowerCase();
  }

  const contentType = stringValue(req.headers.get("content-type"));
  if (contentType.includes("application/json")) {
    const body = await req.json().catch(() => ({}));
    return stringValue(body.plan).toLowerCase();
  }

  const form = await req.formData();
  return stringValue(form.get("plan")).toLowerCase();
}

function serviceClient() {
  const url = requiredEnv("SUPABASE_URL");
  const key = requiredEnv("SUPABASE_SERVICE_ROLE_KEY");
  return createClient(url, key, { auth: { persistSession: false } });
}

function requiredEnv(name: string) {
  const value = stringValue(Deno.env.get(name));
  if (!value) throw new Error(`${name} nao configurado.`);
  return value;
}

function json(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" },
  });
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : "Erro inesperado.";
}
