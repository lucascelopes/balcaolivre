import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const STRIPE_CHECKOUT_URL = "https://api.stripe.com/v1/checkout/sessions";
const STRIPE_API_URL = "https://api.stripe.com/v1";
const DEFAULT_SITE_URL = "https://www.balcaolivrepdv.com.br";
const OFFLINE_INSTALLER_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows/BalcaoLivrePDV-Setup-1.2.2026.1.exe";
const ONLINE_INSTALLER_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.19.exe";
const DEFAULT_ADMIN_ANALYTICS_URL = "https://balcaolivrepdv.onrender.com/api/public/analytics";
const ONLINE_FEATURES = ["pdv", "whatsapp", "cardapio", "garcom", "mercado-pago", "nfce", "equipe", "entregadores", "ifood"];
const OFFLINE_FEATURES = ["pdv", "caixa", "estoque", "nfce"];

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
    amount: 2990,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-offline",
  },
  "offline-anual": {
    name: "Balcao Livre PDV Para Restaurantes Offline",
    amount: 22990,
    interval: "year",
    periodAmount: 1,
    periodUnit: "years",
    clientKind: "windows-offline",
  },
  "online-mensal": {
    name: "Balcao Livre PDV Restaurante Profissional",
    amount: 14900,
    interval: "month",
    periodAmount: 1,
    periodUnit: "months",
    clientKind: "windows-online",
  },
  "online-anual": {
    name: "Balcao Livre PDV Restaurante Profissional",
    amount: 139900,
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

    if (url.pathname.endsWith("/success")) {
      return await handleSuccessPage(url);
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

  await trackAdminAnalytics("checkout.started", {
    plan: planId,
    billing: billingFromPlanId(planId),
    amountCents: plan.amount,
    currency: "BRL",
    source: "supabase-checkout",
    path: url.pathname,
    url: url.toString(),
  }).catch(() => null);

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
  if (!licenseKey) {
    return json({ ok: false, message: "Informe a chave que sera renovada." }, 400);
  }

  const license = await findLicenseByKey(licenseKey);
  if (!license) {
    return json({ ok: false, message: "Chave nao encontrada para renovacao." }, 404);
  }

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

async function handleSuccessPage(url: URL) {
  const sessionId = stringValue(url.searchParams.get("session_id"));
  if (!sessionId) {
    return successHtml("Sessao nao informada", "Nao recebi a sessao do Stripe para localizar a compra.", "", "", "", false, 400);
  }

  const session = await fetchStripeSession(sessionId);
  const result = await ensurePaidLicenseFromSession(session);
  if (!result.paid || !result.license) {
    return successHtml(
      "Pagamento em processamento",
      "Se o pagamento acabou de ser feito, aguarde alguns segundos e atualize esta pagina.",
      "",
      "",
      "",
      false,
      202,
    );
  }

  const license = result.license as Record<string, unknown>;
  const expiresAt = stringValue(license.expiresAt)
    ? new Date(stringValue(license.expiresAt)).toLocaleDateString("pt-BR")
    : "-";

  return successHtml(
    "Pagamento confirmado",
    "Sua chave foi salva no sistema. Baixe o instalador abaixo e ative com a mesma chave.",
    stringValue(license.key),
    stringValue(license.plan),
    expiresAt,
    true,
    200,
    stringValue(license.installerUrl),
  );
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

  if (renewalLicenseKey) {
    return await renewLicenseKeyFromSession(session, plan, planId, sessionId, renewalLicenseKey);
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
    stripe_customer_id: stringValue(session.customer),
    payment_status: stringValue(session.payment_status),
    plan_id: planId,
    features: featuresForPlanId(planId),
    ifood_enabled: featuresForPlanId(planId).includes("ifood"),
    whatsapp_enabled: featuresForPlanId(planId).includes("whatsapp"),
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
    .select("key,plan,email,customer_name,client_kind,expires_at,created_at")
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

  await trackAdminAnalytics("checkout.completed", {
    plan: planId,
    billing: billingFromPlanId(planId),
    amountCents: Number(session.amount_total || plan.amount),
    currency: stringValue(session.currency || "BRL").toUpperCase(),
    checkoutSessionId: sessionId,
    stripeCustomerId: stringValue(session.customer),
    subscriptionId: stringValue(session.subscription),
    source: "stripe-checkout",
  }).catch(() => null);

  return { paid: true, license: normalizeLicense(data) };
}

async function renewLicenseKeyFromSession(
  session: Record<string, unknown>,
  plan: CheckoutPlan,
  planId: string,
  sessionId: string,
  licenseKey: string,
) {
  const current = await findLicenseByKey(licenseKey);
  if (!current) {
    throw new Error("Licenca de renovacao nao encontrada.");
  }

  if (stringValue(current.status).toUpperCase() === "BLOQUEADA") {
    throw new Error("Esta chave esta bloqueada e nao pode ser renovada automaticamente.");
  }

  const now = new Date();
  const currentExpiresAt = parseDateValue(current.expires_at);
  const baseDate = currentExpiresAt && currentExpiresAt.getTime() > now.getTime() ? currentExpiresAt : now;
  const expiresAt = addPlanPeriod(baseDate, plan);
  const customerDetails = session.customer_details as Record<string, unknown> | undefined;
  const currentProfile = recordValue(current.profile);
  const email = stringValue(customerDetails?.email) || stringValue(current.email);
  const name = stringValue(customerDetails?.name) || stringValue(current.customer_name) || "Cliente pago pelo site";
  const profile = {
    ...currentProfile,
    source: "landing_checkout",
    checkout_session_id: sessionId,
    first_checkout_session_id: stringValue(currentProfile.first_checkout_session_id)
      || stringValue(currentProfile.checkout_session_id)
      || sessionId,
    subscription_id: stringValue(session.subscription) || stringValue(currentProfile.subscription_id),
    stripe_customer_id: stringValue(session.customer) || stringValue(currentProfile.stripe_customer_id),
    payment_status: stringValue(session.payment_status),
    plan_id: planId,
    features: featuresForPlanId(planId),
    ifood_enabled: featuresForPlanId(planId).includes("ifood"),
    whatsapp_enabled: featuresForPlanId(planId).includes("whatsapp"),
    amount_total: Number(session.amount_total || plan.amount),
    currency: stringValue(session.currency || "brl"),
    renewed_at: now.toISOString(),
    renewal_expires_at: expiresAt.toISOString(),
  };

  const { data, error } = await serviceClient()
    .from("bv_licenses")
    .update({
      status: stringValue(current.machine_hash) ? "ATIVA" : "DISPONIVEL",
      plan: plan.name,
      customer_name: name,
      email,
      client_kind: plan.clientKind,
      profile,
      expires_at: expiresAt.toISOString(),
      updated_at: now.toISOString(),
    })
    .eq("key", licenseKey)
    .select("key,plan,email,customer_name,client_kind,expires_at,created_at")
    .single();

  if (error) {
    throw new Error(`Supabase recusou renovar a chave: ${error.message}`);
  }

  await serviceClient().from("bv_license_events").insert({
    license_key: licenseKey,
    event_type: "checkout.renewed",
    message: "Assinatura renovada pelo Stripe na mesma chave.",
    payload: profile,
  });

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

  await trackAdminAnalytics("checkout.completed", {
    plan: planId,
    billing: billingFromPlanId(planId),
    amountCents: Number(session.amount_total || plan.amount),
    currency: stringValue(session.currency || "BRL").toUpperCase(),
    checkoutSessionId: sessionId,
    stripeCustomerId: stringValue(session.customer),
    subscriptionId: stringValue(session.subscription),
    source: "stripe-renewal",
  }).catch(() => null);

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
    .select("key,plan,email,customer_name,client_kind,expires_at,created_at")
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
    clientKind: license.client_kind,
    installerUrl: installerUrlForClientKind(license.client_kind),
    expiresAt: license.expires_at,
    createdAt: license.created_at,
  };
}

function installerUrlForClientKind(clientKind: unknown) {
  return stringValue(clientKind).toLowerCase().includes("online")
    ? ONLINE_INSTALLER_URL
    : OFFLINE_INSTALLER_URL;
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

function parseDateValue(value: unknown) {
  const timestamp = Date.parse(stringValue(value));
  return Number.isFinite(timestamp) ? new Date(timestamp) : null;
}

async function createLicenseKey(expiresAt: Date, planId: string) {
  const expiresText = activationExpirationText(expiresAt);
  const prefix = planId.includes("online") ? "ONL" : "OFF";
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
  const functionBasePath = url.pathname.replace(/\/(renew|status|success|webhook)$/, "");
  return {
    successUrl: `${url.origin}${functionBasePath}/success?session_id={CHECKOUT_SESSION_ID}`,
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
    return annual ? "online-anual" : "online-mensal";
  }

  if (text.includes("online") || text.includes("hibrido") || text.includes("integracoes")) {
    return annual ? "online-anual" : "online-mensal";
  }

  return annual ? "offline-anual" : "offline-mensal";
}
function billingFromPlanId(planId: string) {
  const parts = stringValue(planId).split("-");
  return parts[1] || "";
}

function featuresForPlanId(planId: string) {
  return stringValue(planId).toLowerCase().includes("online") ? ONLINE_FEATURES : OFFLINE_FEATURES;
}

async function trackAdminAnalytics(type: string, data: Record<string, unknown>) {
  const endpoint = stringValue(Deno.env.get("BALCAO_ADMIN_ANALYTICS_URL"))
    || stringValue(Deno.env.get("ADMIN_ANALYTICS_URL"))
    || DEFAULT_ADMIN_ANALYTICS_URL;
  if (!endpoint) {
    return;
  }

  await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ type, ...data }),
  });
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

function successHtml(
  title: string,
  message: string,
  licenseKey: string,
  plan: string,
  expiresAt: string,
  ok: boolean,
  status = 200,
  installerUrl = "",
) {
  const installerButton = installerUrl
    ? `<a href="${escapeAttr(installerUrl)}" style="display:inline-flex;align-items:center;justify-content:center;min-height:48px;padding:0 18px;border-radius:8px;background:#083b52;color:white;text-decoration:none;font-weight:900">Baixar instalador</a>`
    : "";
  const licenseBox = licenseKey
    ? `<div style="margin:18px 0;padding:16px;border:1px solid #cbd9e5;border-radius:10px;background:#f7fafc"><span style="display:block;color:#607284;font-size:13px;font-weight:800">Chave de ativacao</span><strong style="display:block;margin-top:6px;font-size:18px;word-break:break-all">${escapeHtml(licenseKey)}</strong></div><dl style="display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:0 0 18px"><div style="padding:12px;border:1px solid #d8e2ec;border-radius:8px"><dt style="color:#607284;font-size:12px;font-weight:800">Plano</dt><dd style="margin:4px 0 0;font-weight:900">${escapeHtml(plan || "-")}</dd></div><div style="padding:12px;border:1px solid #d8e2ec;border-radius:8px"><dt style="color:#607284;font-size:12px;font-weight:800">Validade</dt><dd style="margin:4px 0 0;font-weight:900">${escapeHtml(expiresAt || "-")}</dd></div></dl>`
    : "";
  const refreshButton = ok
    ? ""
    : `<a href="javascript:location.reload()" style="display:inline-flex;align-items:center;justify-content:center;min-height:48px;padding:0 18px;border-radius:8px;background:#083b52;color:white;text-decoration:none;font-weight:900">Atualizar</a>`;

  return new Response(`<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escapeHtml(title)}</title><body style="font-family:Segoe UI,Arial,sans-serif;background:#eef3f6;color:#102130;margin:0;display:grid;place-items:center;min-height:100vh;padding:20px"><main style="width:min(620px,100%);background:white;border:1px solid #d8e2ec;border-radius:14px;padding:28px;box-shadow:0 18px 44px rgba(22,34,45,.10)"><span style="display:inline-flex;margin-bottom:12px;padding:6px 10px;border-radius:999px;background:${ok ? "#e7f6ef" : "#fff4d8"};color:${ok ? "#106b38" : "#99620d"};font-weight:900">${ok ? "Pago" : "Aguardando"}</span><h1 style="margin:0 0 10px;font-size:28px;color:#083b52">${escapeHtml(title)}</h1><p style="font-size:17px;line-height:1.5;color:#4d6072">${escapeHtml(message)}</p>${licenseBox}<div style="display:flex;flex-wrap:wrap;gap:10px">${installerButton}${refreshButton}<a href="${escapeAttr(DEFAULT_SITE_URL)}" style="display:inline-flex;align-items:center;justify-content:center;min-height:48px;padding:0 18px;border-radius:8px;border:1px solid #cbd9e5;color:#083b52;text-decoration:none;font-weight:900">Voltar ao site</a></div></main></body></html>`, {
    status: 200,
    headers: { ...corsHeaders, "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store" },
  });
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function escapeHtml(value: string) {
  return stringValue(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function escapeAttr(value: string) {
  return escapeHtml(value).replaceAll("`", "&#96;");
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : "Erro inesperado.";
}
