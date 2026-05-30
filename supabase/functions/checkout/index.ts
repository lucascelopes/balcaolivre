import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const STRIPE_CHECKOUT_URL = "https://api.stripe.com/v1/checkout/sessions";
const STRIPE_API_URL = "https://api.stripe.com/v1";
const DEFAULT_SITE_URL = "https://www.balcaolivrepdv.com.br";

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

    if (url.pathname.endsWith("/renew")) {
      return await handleRenewal(req, url);
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

  const { successUrl, cancelUrl } = checkoutReturnUrls(req, url);

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

async function handleRenewal(req: Request, url: URL) {
  if (req.method !== "GET" && req.method !== "POST") {
    return json({ ok: false, message: "Metodo nao permitido." }, 405);
  }

  const input = await renewalInputFromRequest(req, url);
  const licenseKey = normalizeLicenseKey(input.licenseKey);
  const license = licenseKey ? await findLicenseByKey(licenseKey) : null;
  const planId = resolveRenewalPlanId(input.plan, license);
  const plan = checkoutPlans[planId];
  if (!plan) {
    return json({ ok: false, message: "Plano de renovacao invalido." }, 400);
  }

  const email = stringValue(input.email) || stringValue(license?.email);
  const { successUrl, cancelUrl } = checkoutReturnUrls(req, url);
  const customerId = await resolveStripeCustomerId(license).catch(() => "");
  const params = new URLSearchParams({
    mode: "subscription",
    client_reference_id: licenseKey || planId,
    "metadata[plan]": planId,
    "metadata[source]": "license_renewal",
    "metadata[renewal_license_key]": licenseKey,
    "line_items[0][quantity]": "1",
    "line_items[0][price_data][currency]": "brl",
    "line_items[0][price_data][unit_amount]": String(plan.amount),
    "line_items[0][price_data][recurring][interval]": plan.interval,
    "line_items[0][price_data][product_data][name]": `${plan.name} - renovacao`,
    allow_promotion_codes: "true",
    success_url: successUrl,
    cancel_url: cancelUrl,
  });

  if (customerId) {
    params.set("customer", customerId);
  } else if (email) {
    params.set("customer_email", email);
  }

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
    return json({ ok: false, message: data.error?.message || "Nao foi possivel abrir a renovacao." }, response.status || 500);
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
  const renewalLicenseKey = normalizeLicenseKey(metadata?.renewal_license_key);
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
    renewal_of_license_key: renewalLicenseKey,
    subscription_id: stringValue(session.subscription),
    stripe_customer_id: stringValue(session.customer),
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
    event_type: renewalLicenseKey ? "checkout.renewed" : "checkout.paid",
    message: renewalLicenseKey
      ? `Licenca renovada pelo Stripe. Chave anterior: ${renewalLicenseKey}.`
      : "Licenca paga gerada automaticamente pela landing.",
    payload: profile,
  });

  if (renewalLicenseKey) {
    await markLicenseRenewed(renewalLicenseKey, licenseKey, session, planId, expiresAt);
  }

  await sendMetaPurchaseEvent(session, plan, planId, sessionId, email).catch(async (error) => {
    await serviceClient().from("bv_license_events").insert({
      license_key: licenseKey,
      event_type: "meta.purchase.failed",
      message: "Falha ao enviar Purchase para Meta.",
      payload: {
        checkout_session_id: sessionId,
        error: messageFromError(error),
      },
    });
  });

  return { paid: true, license: normalizeLicense(data) };
}

async function sendMetaPurchaseEvent(
  session: Record<string, unknown>,
  plan: CheckoutPlan,
  planId: string,
  sessionId: string,
  email: string,
) {
  const pixelId = stringValue(Deno.env.get("META_PIXEL_ID"));
  const accessToken = stringValue(Deno.env.get("META_CAPI_ACCESS_TOKEN"));

  if (!pixelId || !accessToken) {
    return;
  }

  const amount = Number(session.amount_total || plan.amount) / 100;
  const currency = stringValue(session.currency || "brl").toUpperCase();
  const eventSourceUrl = stringValue(Deno.env.get("BALCAO_CHECKOUT_SUCCESS_URL")) || "https://balcaolivrepdv.com.br/";
  const userData: Record<string, unknown> = {};

  if (email) {
    userData.em = [await sha256Hex(email.toLowerCase())];
  }

  const payload = {
    data: [{
      event_name: "Purchase",
      event_time: Math.floor(Date.now() / 1000),
      event_id: sessionId,
      action_source: "website",
      event_source_url: eventSourceUrl,
      user_data: userData,
      custom_data: {
        currency,
        value: amount,
        content_name: plan.name,
        content_ids: [planId],
        content_type: "product",
      },
    }],
    ...(stringValue(Deno.env.get("META_TEST_EVENT_CODE")) ? { test_event_code: stringValue(Deno.env.get("META_TEST_EVENT_CODE")) } : {}),
  };

  const response = await fetch(`https://graph.facebook.com/v20.0/${encodeURIComponent(pixelId)}/events?access_token=${encodeURIComponent(accessToken)}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  const result = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(result?.error?.message || "Meta recusou o evento Purchase.");
  }

  return result;
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

async function fetchStripeSubscription(subscriptionId: string) {
  const response = await fetch(`${STRIPE_API_URL}/subscriptions/${encodeURIComponent(subscriptionId)}`, {
    headers: { Authorization: `Bearer ${requiredEnv("STRIPE_SECRET_KEY")}` },
  });
  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.error?.message || "Nao foi possivel consultar a assinatura Stripe.");
  }

  return data as Record<string, unknown>;
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

async function findLicenseByKey(licenseKey: string) {
  const { data, error } = await serviceClient()
    .from("bv_licenses")
    .select("*")
    .eq("key", licenseKey)
    .limit(1)
    .maybeSingle();

  if (error) {
    throw new Error(`Supabase recusou consultar a licenca: ${error.message}`);
  }

  return data as Record<string, unknown> | null;
}

async function markLicenseRenewed(
  previousLicenseKey: string,
  nextLicenseKey: string,
  session: Record<string, unknown>,
  planId: string,
  expiresAt: Date,
) {
  const current = await findLicenseByKey(previousLicenseKey);
  if (!current) {
    return;
  }

  const profile = {
    ...recordValue(current.profile),
    renewed_at: new Date().toISOString(),
    renewed_checkout_session_id: stringValue(session.id),
    renewal_license_key: nextLicenseKey,
    renewal_plan_id: planId,
    renewal_expires_at: expiresAt.toISOString(),
    subscription_id: stringValue(session.subscription) || stringValue(recordValue(current.profile).subscription_id),
    stripe_customer_id: stringValue(session.customer) || stringValue(recordValue(current.profile).stripe_customer_id),
  };

  await serviceClient()
    .from("bv_licenses")
    .update({
      status: "EXPIRADA",
      profile,
      updated_at: new Date().toISOString(),
    })
    .eq("key", previousLicenseKey);

  await serviceClient().from("bv_license_events").insert({
    license_key: previousLicenseKey,
    event_type: "checkout.renewal_linked",
    message: `Renovacao paga. Nova chave: ${nextLicenseKey}.`,
    payload: profile,
  });
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

async function sha256Hex(message: string) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(message));
  return Array.from(new Uint8Array(digest))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");
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

async function renewalInputFromRequest(req: Request, url: URL) {
  if (req.method === "GET") {
    return {
      licenseKey: stringValue(url.searchParams.get("license_key") || url.searchParams.get("licenseKey")),
      plan: stringValue(url.searchParams.get("plan")),
      email: stringValue(url.searchParams.get("email")),
    };
  }

  const contentType = stringValue(req.headers.get("content-type"));
  if (contentType.includes("application/json")) {
    const body = await req.json().catch(() => ({}));
    return {
      licenseKey: stringValue(body.licenseKey || body.license_key),
      plan: stringValue(body.plan),
      email: stringValue(body.email),
    };
  }

  const form = await req.formData();
  return {
    licenseKey: stringValue(form.get("licenseKey") || form.get("license_key")),
    plan: stringValue(form.get("plan")),
    email: stringValue(form.get("email")),
  };
}

function checkoutReturnUrls(req: Request, url: URL) {
  const origin = stringValue(req.headers.get("origin"))
    || stringValue(Deno.env.get("BALCAO_CHECKOUT_SUCCESS_URL")).replace(/\/$/, "")
    || DEFAULT_SITE_URL;
  return {
    successUrl: `${origin}/?checkout=sucesso&session_id={CHECKOUT_SESSION_ID}`,
    cancelUrl: `${origin}/#planos`,
    origin,
  };
}

function resolveRenewalPlanId(requestedPlan: string, license: Record<string, unknown> | null) {
  const requested = stringValue(requestedPlan).toLowerCase();
  if (checkoutPlans[requested]) {
    return requested;
  }

  const profile = recordValue(license?.profile);
  const profilePlan = stringValue(profile.plan_id).toLowerCase();
  if (checkoutPlans[profilePlan]) {
    return profilePlan;
  }

  const text = `${stringValue(license?.plan)} ${stringValue(license?.client_kind)} ${requested}`.toLowerCase();
  const annual = text.includes("anual") || text.includes("year") || text.includes("ano");
  if (text.includes("complete") || text.includes("completo")) {
    return annual ? "complete-anual" : "complete-mensal";
  }

  if (text.includes("online") || text.includes("hibrido") || text.includes("integracoes")) {
    return annual ? "online-anual" : "online-mensal";
  }

  return annual ? "offline-anual" : "offline-mensal";
}

async function resolveStripeCustomerId(license: Record<string, unknown> | null) {
  const profile = recordValue(license?.profile);
  const direct = stringValue(profile.stripe_customer_id || profile.customer_id || profile.stripeCustomerId);
  if (direct) {
    return direct;
  }

  const subscriptionId = stringValue(profile.subscription_id || profile.stripe_subscription_id || profile.subscriptionId);
  if (!subscriptionId) {
    return "";
  }

  const subscription = await fetchStripeSubscription(subscriptionId);
  return stringValue(subscription.customer);
}

function recordValue(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : {};
}

function normalizeLicenseKey(value: unknown) {
  return stringValue(value).toUpperCase().replaceAll(" ", "").replaceAll("_", "-");
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
