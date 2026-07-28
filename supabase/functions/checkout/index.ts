import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import {
  BALCAO_PLANS,
  BalcaoPlan,
  BalcaoPlanCode,
  getBalcaoPlan,
  getExtraSeatPriceId,
  getStripePriceId,
  isBalcaoPlanCode,
  parseExtraDesktopQuantity,
} from "./balcao-commerce.ts";

const STRIPE_CHECKOUT_URL = "https://api.stripe.com/v1/checkout/sessions";
const STRIPE_API_URL = "https://api.stripe.com/v1";
const DEFAULT_SITE_URL = "https://www.balcaolivrepdv.com.br";
const DEFAULT_WEB_URL = "https://app.balcaolivrepdv.com.br";
const ONLINE_INSTALLER_URL =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.29.exe";
const DEFAULT_ADMIN_ANALYTICS_URL = "https://balcaolivrepdv.onrender.com/api/public/analytics";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type, stripe-signature",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type CheckoutInput = {
  plan: string;
  extraDesktopQuantity: number;
  email: string;
  claimToken: string;
};

type CommerceContext = {
  accountId: string;
  storeId: string;
  subscriptionId: string;
  plan: BalcaoPlan;
  extraDesktopQuantity: number;
  currentPeriodEnd: string;
  legacyLicenseKey: string;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });

  try {
    const url = new URL(req.url);
    if (url.pathname.endsWith("/webhook")) return await handleWebhook(req);
    if (url.pathname.endsWith("/status")) return await handleStatus(req, url);
    if (url.pathname.endsWith("/success")) return await handleSuccessPage(url);
    if (url.pathname.endsWith("/renew")) return await handleLegacyRenewal(req, url);
    if (url.pathname.endsWith("/seats")) return await handleSeatAdjustment(req);
    if (req.method === "GET" || req.method === "POST") return await handleCheckout(req, url);
    return json({ ok: false, message: "Método não permitido." }, 405);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function handleCheckout(req: Request, url: URL) {
  const input = await checkoutInputFromRequest(req, url);
  const plan = getBalcaoPlan(input.plan);
  const { successUrl, cancelUrl } = checkoutReturnUrls(req);
  const params = new URLSearchParams({
    mode: "subscription",
    client_reference_id: plan.code,
    "metadata[plan]": plan.code,
    "metadata[extra_desktop_quantity]": String(input.extraDesktopQuantity),
    "subscription_data[metadata][plan]": plan.code,
    "subscription_data[metadata][extra_desktop_quantity]": String(input.extraDesktopQuantity),
    allow_promotion_codes: "true",
    success_url: successUrl,
    cancel_url: cancelUrl,
    "phone_number_collection[enabled]": "true",
  });

  if (input.email) params.set("customer_email", input.email);
  if (plan.interval === "year") {
    params.set("shipping_address_collection[allowed_countries][0]", "BR");
    params.set("billing_address_collection", "required");
  } else {
    params.set("billing_address_collection", "auto");
  }

  addStripeLineItem(params, 0, plan, 1);
  if (input.extraDesktopQuantity > 0) {
    addStripeExtraSeatLineItem(params, 1, plan, input.extraDesktopQuantity);
  }

  const response = await stripeRequest(STRIPE_CHECKOUT_URL, {
    method: "POST",
    body: params,
  });
  if (!response.ok || !response.data.url) {
    return json(
      { ok: false, message: response.data.error?.message || "Não foi possível abrir a compra." },
      response.status || 500,
    );
  }

  if (input.claimToken) {
    if (input.claimToken.length < 32) {
      return json({ ok: false, message: "Token de ativação automática inválido." }, 400);
    }
    await serviceClient().from("bl_checkout_claims").insert({
      token_hash: await sha256Hex(input.claimToken),
      provider_checkout_session_id: stringValue(response.data.id),
      plan_code: plan.code,
      extra_desktop_quantity: input.extraDesktopQuantity,
      expires_at: new Date(Date.now() + 30 * 60_000).toISOString(),
    }).throwOnError();
  }

  await trackAdminAnalytics("checkout.started", {
    plan: plan.code,
    billing: plan.interval,
    amountCents: plan.amountCents,
    extraDesktopQuantity: input.extraDesktopQuantity,
    currency: "BRL",
    source: "supabase-checkout",
  }).catch(() => null);

  if (wantsJson(req)) {
    return json({ ok: true, checkoutUrl: response.data.url });
  }

  return new Response(null, {
    status: 303,
    headers: { ...corsHeaders, Location: response.data.url, "Cache-Control": "no-store" },
  });
}

async function handleStatus(req: Request, url: URL) {
  const input = req.method === "POST"
    ? recordValue(await req.json().catch(() => ({})))
    : {};
  let sessionId = stringValue(url.searchParams.get("session_id") || input.session_id || input.sessionId);
  const claimToken = stringValue(input.claimToken || input.claim_token);
  if (!sessionId && claimToken) {
    const { data: claim, error } = await serviceClient().from("bl_checkout_claims")
      .select("provider_checkout_session_id,expires_at,consumed_at")
      .eq("token_hash", await sha256Hex(claimToken))
      .maybeSingle();
    if (error) throw new Error(`Falha ao localizar o pagamento: ${error.message}`);
    if (!claim || claim.consumed_at || Date.parse(claim.expires_at) <= Date.now()) {
      return json({ ok: false, paid: false, message: "Ativação automática expirada." }, 410);
    }
    sessionId = claim.provider_checkout_session_id;
  }
  if (!sessionId) return json({ ok: false, message: "Sessão não informada." }, 400);

  const session = await fetchStripeSession(sessionId);
  if (!isPaidSession(session)) {
    return json({ ok: false, paid: false, message: "Pagamento ainda não confirmado." }, 202);
  }

  const commerce = await ensurePaidCommerceFromSession(session);
  const handoff = await createHandoffBundle(commerce);
  if (claimToken) {
    await serviceClient().from("bl_checkout_claims").update({
      consumed_at: new Date().toISOString(),
    }).eq("token_hash", await sha256Hex(claimToken)).throwOnError();
  }
  return json({
    ok: true,
    paid: true,
    purchase: publicPurchase(commerce),
    handoff,
  });
}

async function handleSuccessPage(url: URL) {
  const sessionId = stringValue(url.searchParams.get("session_id"));
  if (!sessionId) return json({ ok: false, message: "Sessão não informada." }, 400);

  const target = new URL(siteUrl());
  target.searchParams.set("checkout", "sucesso");
  target.searchParams.set("session_id", sessionId);
  return new Response(null, {
    status: 303,
    headers: { ...corsHeaders, Location: target.toString(), "Cache-Control": "no-store" },
  });
}

async function handleWebhook(req: Request) {
  const signature = stringValue(req.headers.get("stripe-signature"));
  const payload = await req.text();
  if (!await isValidStripeSignature(payload, signature, requiredEnv("STRIPE_WEBHOOK_SECRET"))) {
    return json({ ok: false, message: "Assinatura inválida." }, 400);
  }

  const event = JSON.parse(payload);
  const eventId = stringValue(event?.id);
  const eventType = stringValue(event?.type);
  if (!eventId || !eventType) {
    return json({ ok: false, message: "Evento do Stripe inválido." }, 400);
  }
  if (!await reserveWebhookEvent(eventId, eventType)) {
    return json({ received: true, duplicate: true });
  }

  const object = recordValue(event?.data?.object);
  try {
    if (eventType === "checkout.session.completed" || eventType === "checkout.session.async_payment_succeeded") {
      await ensurePaidCommerceFromSession(object);
    } else if (eventType === "customer.subscription.updated" || eventType === "customer.subscription.deleted") {
      await syncSubscriptionState(object, eventType === "customer.subscription.deleted");
    } else if (eventType === "invoice.paid" || eventType === "invoice.payment_failed") {
      await syncInvoiceState(object, eventType === "invoice.paid");
    }
    await completeWebhookEvent(eventId);
  } catch (error) {
    await failWebhookEvent(eventId, messageFromError(error));
    throw error;
  }

  return json({ received: true });
}

async function reserveWebhookEvent(eventId: string, eventType: string) {
  const client = serviceClient();
  const { error } = await client.from("bl_webhook_events").insert({
    event_id: eventId,
    event_type: eventType,
    status: "PROCESSING",
  });
  if (!error) return true;
  if (error.code !== "23505") {
    throw new Error(`Falha ao registrar evento do Stripe: ${error.message}`);
  }

  const { data: existing, error: lookupError } = await client.from("bl_webhook_events")
    .select("status,attempts")
    .eq("event_id", eventId)
    .single();
  if (lookupError) throw new Error(`Falha ao consultar evento do Stripe: ${lookupError.message}`);
  if (existing.status !== "FAILED") return false;

  const { data: claimed, error: retryError } = await client.from("bl_webhook_events").update({
    status: "PROCESSING",
    attempts: Number(existing.attempts || 1) + 1,
    last_error: null,
    updated_at: new Date().toISOString(),
  }).eq("event_id", eventId).eq("status", "FAILED").select("id").maybeSingle();
  if (retryError) throw new Error(`Falha ao reprocessar evento do Stripe: ${retryError.message}`);
  return Boolean(claimed);
}

async function completeWebhookEvent(eventId: string) {
  const now = new Date().toISOString();
  await serviceClient().from("bl_webhook_events").update({
    status: "COMPLETED",
    completed_at: now,
    last_error: null,
    updated_at: now,
  }).eq("event_id", eventId).eq("status", "PROCESSING").throwOnError();
}

async function failWebhookEvent(eventId: string, errorMessage: string) {
  await serviceClient().from("bl_webhook_events").update({
    status: "FAILED",
    last_error: errorMessage.slice(0, 2_000),
    updated_at: new Date().toISOString(),
  }).eq("event_id", eventId).eq("status", "PROCESSING").throwOnError();
}

async function ensurePaidCommerceFromSession(session: Record<string, unknown>): Promise<CommerceContext> {
  const sessionId = stringValue(session.id);
  const metadata = recordValue(session.metadata);
  const plan = getBalcaoPlan(metadata.plan || session.client_reference_id);
  const extraDesktopQuantity = parseExtraDesktopQuantity(metadata.extra_desktop_quantity);
  if (!sessionId || !isPaidSession(session)) throw new Error("Sessão de compra ainda não está paga.");

  const stripeCustomerId = stringValue(session.customer);
  const stripeSubscriptionId = stringValue(session.subscription);
  if (!stripeCustomerId || !stripeSubscriptionId) {
    throw new Error("O Stripe não retornou o cliente ou a assinatura.");
  }

  const customerDetails = recordValue(session.customer_details);
  const email = stringValue(customerDetails.email).toLowerCase();
  const phone = stringValue(customerDetails.phone);
  const displayName = stringValue(customerDetails.name) || "Cliente Balcão Livre";
  const client = serviceClient();

  const account = await findOrCreateAccount(stripeCustomerId, email, phone, displayName);
  const store = await findOrCreateStore(account.id, displayName);
  const subscriptionObject = await fetchStripeSubscription(stripeSubscriptionId);
  const period = stripeSubscriptionPeriod(subscriptionObject, plan);
  const subscription = await upsertSubscription({
    accountId: account.id,
    storeId: store.id,
    sessionId,
    stripeCustomerId,
    stripeSubscriptionId,
    plan,
    extraDesktopQuantity,
    period,
  });

  await client.from("bl_entitlements").upsert({
    store_id: store.id,
    subscription_id: subscription.id,
    plan_code: plan.code,
    modules: plan.modules,
    desktop_seat_limit: plan.desktopSeats + extraDesktopQuantity,
    mobile_seat_limit: plan.mobileSeats,
    web_uses_desktop_seat: true,
    mercadopago_point_enabled: plan.mercadopagoPoint,
    machine_fulfillment_included: plan.machineFulfillment,
    reports_level: plan.reportsLevel,
    effective_at: period.start,
    expires_at: period.end,
    updated_at: new Date().toISOString(),
  }, { onConflict: "store_id" }).throwOnError();

  await syncSeats(store.id, subscription.id, plan.desktopSeats + extraDesktopQuantity, plan.mobileSeats);
  if (plan.machineFulfillment) {
    await upsertMachineFulfillment(subscription.id, store.id, session);
  }

  const legacyLicenseKey = await ensureLegacyLicense({
    session,
    plan,
    accountId: account.id,
    storeId: store.id,
    subscriptionId: subscription.id,
    periodEnd: period.end,
    email,
    displayName,
  });

  await trackAdminAnalytics("checkout.completed", {
    plan: plan.code,
    billing: plan.interval,
    amountCents: Number(session.amount_total || plan.amountCents),
    extraDesktopQuantity,
    checkoutSessionId: sessionId,
    stripeCustomerId,
    subscriptionId: stripeSubscriptionId,
    storeId: store.id,
    source: "stripe-checkout",
  }).catch(() => null);

  return {
    accountId: account.id,
    storeId: store.id,
    subscriptionId: subscription.id,
    plan,
    extraDesktopQuantity,
    currentPeriodEnd: period.end,
    legacyLicenseKey,
  };
}

async function findOrCreateAccount(
  stripeCustomerId: string,
  email: string,
  phone: string,
  displayName: string,
) {
  const client = serviceClient();
  const { data: existing, error: lookupError } = await client
    .from("bl_accounts")
    .select("id")
    .eq("stripe_customer_id", stripeCustomerId)
    .maybeSingle();
  if (lookupError) throw new Error(`Falha ao consultar conta: ${lookupError.message}`);
  if (existing) {
    const { data, error } = await client.from("bl_accounts").update({
      email: email || null,
      phone: phone || null,
      display_name: displayName,
      status: "ACTIVE",
      updated_at: new Date().toISOString(),
    }).eq("id", existing.id).select("id").single();
    if (error) throw new Error(`Falha ao atualizar conta: ${error.message}`);
    return data;
  }

  const { data, error } = await client.from("bl_accounts").insert({
    stripe_customer_id: stripeCustomerId,
    email: email || null,
    phone: phone || null,
    display_name: displayName,
    status: "ACTIVE",
  }).select("id").single();
  if (error) throw new Error(`Falha ao criar conta: ${error.message}`);
  return data;
}

async function findOrCreateStore(accountId: string, displayName: string) {
  const client = serviceClient();
  const { data: existing, error: lookupError } = await client
    .from("bl_stores")
    .select("id")
    .eq("account_id", accountId)
    .order("created_at", { ascending: true })
    .limit(1)
    .maybeSingle();
  if (lookupError) throw new Error(`Falha ao consultar loja: ${lookupError.message}`);
  if (existing) return existing;

  const { data, error } = await client.from("bl_stores").insert({
    account_id: accountId,
    name: displayName,
    onboarding_status: "PENDING",
  }).select("id").single();
  if (error) throw new Error(`Falha ao criar loja: ${error.message}`);

  await client.from("bl_onboarding_configs").insert({ store_id: data.id }).throwOnError();
  return data;
}

async function upsertSubscription(input: {
  accountId: string;
  storeId: string;
  sessionId: string;
  stripeCustomerId: string;
  stripeSubscriptionId: string;
  plan: BalcaoPlan;
  extraDesktopQuantity: number;
  period: { start: string; end: string };
}) {
  const client = serviceClient();
  const row = {
    account_id: input.accountId,
    store_id: input.storeId,
    provider: "STRIPE",
    provider_customer_id: input.stripeCustomerId,
    provider_subscription_id: input.stripeSubscriptionId,
    provider_checkout_session_id: input.sessionId,
    plan_code: input.plan.code,
    billing_interval: input.plan.interval === "year" ? "YEAR" : "MONTH",
    status: "ACTIVE",
    base_quantity: 1,
    extra_desktop_quantity: input.extraDesktopQuantity,
    current_period_start: input.period.start,
    current_period_end: input.period.end,
    updated_at: new Date().toISOString(),
  };
  const { data, error } = await client.from("bl_subscriptions")
    .upsert(row, { onConflict: "provider_subscription_id" })
    .select("id")
    .single();
  if (error) throw new Error(`Falha ao salvar assinatura: ${error.message}`);
  return data;
}

async function syncSeats(
  storeId: string,
  subscriptionId: string,
  desktopLimit: number,
  mobileLimit: number,
) {
  const client = serviceClient();
  const desired = [
    ...Array.from({ length: desktopLimit }, (_, index) => ({
      store_id: storeId,
      subscription_id: subscriptionId,
      seat_kind: "DESKTOP",
      source: index === 0 ? "PLAN" : "EXTRA_SUBSCRIPTION",
      ordinal: index + 1,
      status: "AVAILABLE",
      updated_at: new Date().toISOString(),
    })),
    ...Array.from({ length: mobileLimit }, (_, index) => ({
      store_id: storeId,
      subscription_id: subscriptionId,
      seat_kind: "MOBILE",
      source: "PLAN",
      ordinal: index + 1,
      status: "AVAILABLE",
      updated_at: new Date().toISOString(),
    })),
  ];

  for (const seat of desired) {
    const { data: existing } = await client.from("bl_device_seats")
      .select("id,status")
      .eq("store_id", storeId)
      .eq("seat_kind", seat.seat_kind)
      .eq("ordinal", seat.ordinal)
      .maybeSingle();
    await client.from("bl_device_seats").upsert({
      ...seat,
      status: existing?.status === "ASSIGNED" ? "ASSIGNED" : "AVAILABLE",
    }, { onConflict: "store_id,seat_kind,ordinal" }).throwOnError();
  }

  await client.from("bl_device_seats")
    .update({ status: "REVOKED", updated_at: new Date().toISOString() })
    .eq("store_id", storeId)
    .eq("seat_kind", "DESKTOP")
    .gt("ordinal", desktopLimit)
    .throwOnError();
  await client.from("bl_device_seats")
    .update({ status: "REVOKED", updated_at: new Date().toISOString() })
    .eq("store_id", storeId)
    .eq("seat_kind", "MOBILE")
    .gt("ordinal", mobileLimit)
    .throwOnError();
}

async function upsertMachineFulfillment(subscriptionId: string, storeId: string, session: Record<string, unknown>) {
  const customer = recordValue(session.customer_details);
  const collected = recordValue(recordValue(session.collected_information).shipping_details);
  const legacyShipping = recordValue(session.shipping_details);
  const shipping = Object.keys(collected).length ? collected : legacyShipping;
  const address = recordValue(shipping.address || customer.address);
  const hasAddress = Boolean(address.line1 && address.city && address.postal_code && address.state);
  await serviceClient().from("bl_machine_fulfillments").upsert({
    subscription_id: subscriptionId,
    store_id: storeId,
    provider: "MERCADO_PAGO",
    model: "POINT_PRO_3",
    status: hasAddress ? "READY" : "WAITING_ADDRESS",
    recipient_name: stringValue(shipping.name || customer.name) || null,
    recipient_phone: stringValue(shipping.phone || customer.phone) || null,
    shipping_address: address,
    updated_at: new Date().toISOString(),
  }, { onConflict: "subscription_id" }).throwOnError();
}

async function ensureLegacyLicense(input: {
  session: Record<string, unknown>;
  plan: BalcaoPlan;
  accountId: string;
  storeId: string;
  subscriptionId: string;
  periodEnd: string;
  email: string;
  displayName: string;
}) {
  const sessionId = stringValue(input.session.id);
  const existing = await findLegacyLicenseByCheckoutSession(sessionId);
  if (existing?.key) return stringValue(existing.key);

  const key = await createLegacyLicenseKey(new Date(input.periodEnd), input.plan.code);
  const profile = {
    source: "stripe_checkout_v2",
    checkout_session_id: sessionId,
    stripe_subscription_id: stringValue(input.session.subscription),
    stripe_customer_id: stringValue(input.session.customer),
    commerce_subscription_id: input.subscriptionId,
    account_id: input.accountId,
    store_id: input.storeId,
    plan_id: input.plan.code,
    features: input.plan.modules,
    device_seats_managed: true,
  };
  const { error } = await serviceClient().from("bv_licenses").insert({
    key,
    status: "DISPONIVEL",
    plan: input.plan.name,
    customer_name: input.displayName,
    email: input.email || null,
    client_kind: "windows-online",
    profile,
    settings: {},
    metrics: {},
    expires_at: input.periodEnd,
    updated_at: new Date().toISOString(),
  });
  if (error) throw new Error(`Falha ao criar compatibilidade da licença: ${error.message}`);

  await serviceClient().from("bv_license_events").insert({
    license_key: key,
    event_type: "checkout.paid.v2",
    message: "Assinatura criada com handoff seguro; chave não exposta ao cliente.",
    payload: profile,
  }).throwOnError();
  return key;
}

async function createHandoffBundle(commerce: CommerceContext) {
  const [claim, web, windows] = await Promise.all([
    issueHandoffToken(commerce, "CHECKOUT_CLAIM", null, 30),
    issueHandoffToken(commerce, "WEB_SIGN_IN", "DESKTOP", 15),
    issueHandoffToken(commerce, "WINDOWS_ACTIVATION", "DESKTOP", 30),
  ]);
  const claimUrl = new URL(siteUrl());
  claimUrl.searchParams.set("checkout", "criar-conta");
  claimUrl.searchParams.set("claim", claim);

  const webUrl = new URL(stringValue(Deno.env.get("BALCAO_WEB_APP_URL")) || DEFAULT_WEB_URL);
  webUrl.searchParams.set("handoff", web);

  return {
    claimUrl: claimUrl.toString(),
    webUrl: webUrl.toString(),
    windowsDeepLink: `balcaolivre://activate?token=${encodeURIComponent(windows)}`,
    windowsInstallerUrl: stringValue(Deno.env.get("BALCAO_WINDOWS_INSTALLER_URL")) || ONLINE_INSTALLER_URL,
  };
}

async function issueHandoffToken(
  commerce: CommerceContext,
  purpose: "CHECKOUT_CLAIM" | "WEB_SIGN_IN" | "WINDOWS_ACTIVATION",
  seatKind: "DESKTOP" | null,
  lifetimeMinutes: number,
) {
  const raw = randomToken();
  let seatId: string | null = null;
  if (seatKind) {
    const { data } = await serviceClient().from("bl_device_seats")
      .select("id")
      .eq("store_id", commerce.storeId)
      .eq("seat_kind", seatKind)
      .in("status", ["AVAILABLE", "ASSIGNED"])
      .order("ordinal", { ascending: true })
      .limit(1)
      .maybeSingle();
    seatId = data?.id || null;
  }

  const expiresAt = new Date(Date.now() + lifetimeMinutes * 60_000).toISOString();
  await serviceClient().from("bl_handoff_tokens").insert({
    store_id: commerce.storeId,
    account_id: commerce.accountId,
    seat_id: seatId,
    token_hash: await sha256Hex(raw),
    purpose,
    target: purpose === "WINDOWS_ACTIVATION" ? "windows" : purpose === "WEB_SIGN_IN" ? "web" : "account",
    expires_at: expiresAt,
  }).throwOnError();
  return raw;
}

function publicPurchase(commerce: CommerceContext) {
  return {
    accountId: commerce.accountId,
    storeId: commerce.storeId,
    subscriptionId: commerce.subscriptionId,
    planCode: commerce.plan.code,
    planName: commerce.plan.name,
    billingInterval: commerce.plan.interval,
    extraDesktopQuantity: commerce.extraDesktopQuantity,
    desktopSeats: commerce.plan.desktopSeats + commerce.extraDesktopQuantity,
    mobileSeats: commerce.plan.mobileSeats,
    machineIncluded: commerce.plan.machineFulfillment,
    mercadopagoPointEnabled: commerce.plan.mercadopagoPoint,
    currentPeriodEnd: commerce.currentPeriodEnd,
  };
}

async function syncSubscriptionState(subscription: Record<string, unknown>, deleted: boolean) {
  const stripeSubscriptionId = stringValue(subscription.id);
  if (!stripeSubscriptionId) return;
  const metadata = recordValue(subscription.metadata);
  if (!isBalcaoPlanCode(metadata.plan)) return;
  const plan = getBalcaoPlan(metadata.plan);
  const extraDesktopQuantity = subscriptionExtraDesktopQuantity(subscription, plan);
  const status = deleted ? "CANCELED" : normalizeSubscriptionStatus(subscription.status);
  const period = stripeSubscriptionPeriod(subscription, plan);
  const { data, error } = await serviceClient().from("bl_subscriptions").update({
    plan_code: plan.code,
    billing_interval: plan.interval === "year" ? "YEAR" : "MONTH",
    status,
    extra_desktop_quantity: extraDesktopQuantity,
    current_period_start: period.start,
    current_period_end: period.end,
    cancel_at_period_end: Boolean(subscription.cancel_at_period_end),
    updated_at: new Date().toISOString(),
  }).eq("provider_subscription_id", stripeSubscriptionId).select("id,store_id").maybeSingle();
  if (error) throw new Error(`Falha ao sincronizar assinatura: ${error.message}`);
  if (!data) return;

  await serviceClient().from("bl_entitlements").update({
    plan_code: plan.code,
    modules: plan.modules,
    desktop_seat_limit: plan.desktopSeats + extraDesktopQuantity,
    mobile_seat_limit: plan.mobileSeats,
    mercadopago_point_enabled: plan.mercadopagoPoint,
    machine_fulfillment_included: plan.machineFulfillment,
    reports_level: plan.reportsLevel,
    expires_at: period.end,
    updated_at: new Date().toISOString(),
  }).eq("store_id", data.store_id).throwOnError();
  await syncSeats(data.store_id, data.id, plan.desktopSeats + extraDesktopQuantity, plan.mobileSeats);
}

async function handleSeatAdjustment(req: Request) {
  if (req.method !== "POST") return json({ ok: false, message: "Método não permitido." }, 405);
  const user = await authenticatedUser(req);
  if (!user) return json({ ok: false, message: "Entre na sua conta para alterar os dispositivos." }, 401);

  const body = recordValue(await req.json().catch(() => ({})));
  const desiredQuantity = Number(body.extraDesktopQuantity ?? body.extra_desktop_quantity);
  if (!Number.isInteger(desiredQuantity) || desiredQuantity < 0 || desiredQuantity > 20) {
    return json({ ok: false, message: "Informe de 0 a 20 computadores adicionais." }, 400);
  }

  const client = serviceClient();
  const { data: membership, error: membershipError } = await client.from("bl_store_members")
    .select("store_id,role")
    .eq("user_id", user.id)
    .eq("status", "ACTIVE")
    .in("role", ["OWNER", "MANAGER"])
    .limit(1)
    .maybeSingle();
  if (membershipError) throw new Error(`Falha ao validar o responsável: ${membershipError.message}`);
  if (!membership) return json({ ok: false, message: "Somente o proprietário ou gerente pode alterar dispositivos." }, 403);

  const { data: localSubscription, error: subscriptionError } = await client.from("bl_subscriptions")
    .select("id,provider_subscription_id,plan_code,status")
    .eq("store_id", membership.store_id)
    .in("status", ["ACTIVE", "TRIALING", "PAST_DUE"])
    .order("created_at", { ascending: false })
    .limit(1)
    .maybeSingle();
  if (subscriptionError) throw new Error(`Falha ao localizar a assinatura: ${subscriptionError.message}`);
  if (!localSubscription?.provider_subscription_id || !isBalcaoPlanCode(localSubscription.plan_code)) {
    return json({ ok: false, message: "Assinatura ativa não encontrada." }, 404);
  }

  const plan = getBalcaoPlan(localSubscription.plan_code);
  const stripeSubscription = await fetchStripeSubscription(localSubscription.provider_subscription_id);
  const expectedPriceId = getExtraSeatPriceId(plan.interval);
  const extraItem = subscriptionItems(stripeSubscription).find((item) =>
    stringValue(recordValue(item.price).id) === expectedPriceId
  );
  const currentQuantity = extraItem ? Math.max(0, Number(extraItem.quantity || 0)) : 0;
  if (currentQuantity === desiredQuantity) {
    return json({
      ok: true,
      unchanged: true,
      extraDesktopQuantity: desiredQuantity,
      desktopSeats: plan.desktopSeats + desiredQuantity,
    });
  }

  const params = new URLSearchParams({
    proration_behavior: "create_prorations",
    payment_behavior: "pending_if_incomplete",
    "metadata[plan]": plan.code,
    "metadata[extra_desktop_quantity]": String(desiredQuantity),
  });
  if (extraItem) {
    params.set("items[0][id]", stringValue(extraItem.id));
    if (desiredQuantity === 0) params.set("items[0][deleted]", "true");
    else params.set("items[0][quantity]", String(desiredQuantity));
  } else if (desiredQuantity > 0) {
    params.set("items[0][price]", expectedPriceId);
    params.set("items[0][quantity]", String(desiredQuantity));
  }

  const response = await stripeRequest(
    `${STRIPE_API_URL}/subscriptions/${encodeURIComponent(localSubscription.provider_subscription_id)}`,
    { method: "POST", body: params },
  );
  if (!response.ok) {
    return json({
      ok: false,
      message: response.data.error?.message || "Não foi possível alterar os computadores adicionais.",
    }, response.status || 500);
  }

  await syncSubscriptionState(recordValue(response.data), false);
  await trackAdminAnalytics("subscription.extra_desktop.updated", {
    storeId: membership.store_id,
    plan: plan.code,
    previousQuantity: currentQuantity,
    extraDesktopQuantity: desiredQuantity,
    desktopSeats: plan.desktopSeats + desiredQuantity,
    proration: true,
  }).catch(() => null);

  return json({
    ok: true,
    extraDesktopQuantity: desiredQuantity,
    desktopSeats: plan.desktopSeats + desiredQuantity,
    prorated: true,
  });
}

function subscriptionItems(subscription: Record<string, unknown>) {
  const items = recordValue(subscription.items);
  return Array.isArray(items.data)
    ? items.data.map(recordValue)
    : [];
}

function subscriptionExtraDesktopQuantity(subscription: Record<string, unknown>, plan: BalcaoPlan) {
  const expectedPriceId = getExtraSeatPriceId(plan.interval);
  const fromItems = subscriptionItems(subscription)
    .filter((item) => stringValue(recordValue(item.price).id) === expectedPriceId)
    .reduce((total, item) => total + Math.max(0, Number(item.quantity || 0)), 0);
  if (fromItems > 0) return parseExtraDesktopQuantity(fromItems);
  return parseExtraDesktopQuantity(recordValue(subscription.metadata).extra_desktop_quantity);
}

async function syncInvoiceState(invoice: Record<string, unknown>, paid: boolean) {
  const stripeSubscriptionId = stringValue(invoice.subscription);
  if (!stripeSubscriptionId) return;
  await serviceClient().from("bl_subscriptions").update({
    status: paid ? "ACTIVE" : "PAST_DUE",
    updated_at: new Date().toISOString(),
  }).eq("provider_subscription_id", stripeSubscriptionId).throwOnError();
}

async function handleLegacyRenewal(req: Request, url: URL) {
  const input = await renewalInputFromRequest(req, url);
  const legacy = await findLegacyLicenseByKey(normalizeLicenseKey(input.licenseKey));
  if (!legacy) return json({ ok: false, message: "Assinatura não encontrada para renovação." }, 404);

  const profile = recordValue(legacy.profile);
  const requestedPlan = input.plan || profile.plan_id;
  const planCode = normalizeLegacyPlanCode(requestedPlan, legacy);
  const plan = getBalcaoPlan(planCode);
  const params = new URLSearchParams({
    mode: "subscription",
    client_reference_id: plan.code,
    "metadata[plan]": plan.code,
    "metadata[legacy_renewal_key]": stringValue(legacy.key),
    "metadata[extra_desktop_quantity]": "0",
    "subscription_data[metadata][plan]": plan.code,
    "subscription_data[metadata][extra_desktop_quantity]": "0",
    allow_promotion_codes: "true",
    success_url: checkoutReturnUrls(req).successUrl,
    cancel_url: checkoutReturnUrls(req).cancelUrl,
    "phone_number_collection[enabled]": "true",
  });
  if (plan.interval === "year") {
    params.set("shipping_address_collection[allowed_countries][0]", "BR");
    params.set("billing_address_collection", "required");
  }
  const customerId = stringValue(profile.stripe_customer_id);
  if (customerId) params.set("customer", customerId);
  else if (input.email || legacy.email) params.set("customer_email", input.email || stringValue(legacy.email));
  addStripeLineItem(params, 0, plan, 1);

  const response = await stripeRequest(STRIPE_CHECKOUT_URL, { method: "POST", body: params });
  if (!response.ok || !response.data.url) {
    return json({ ok: false, message: response.data.error?.message || "Não foi possível abrir a renovação." }, 500);
  }
  return new Response(null, {
    status: 303,
    headers: { ...corsHeaders, Location: response.data.url, "Cache-Control": "no-store" },
  });
}

function addStripeLineItem(params: URLSearchParams, index: number, plan: BalcaoPlan, quantity: number) {
  const prefix = `line_items[${index}]`;
  const priceId = getStripePriceId(plan);
  params.set(`${prefix}[quantity]`, String(quantity));
  params.set(`${prefix}[price]`, priceId);
}

function addStripeExtraSeatLineItem(
  params: URLSearchParams,
  index: number,
  plan: BalcaoPlan,
  quantity: number,
) {
  const prefix = `line_items[${index}]`;
  const priceId = getExtraSeatPriceId(plan.interval);
  params.set(`${prefix}[quantity]`, String(quantity));
  params.set(`${prefix}[price]`, priceId);
}

async function checkoutInputFromRequest(req: Request, url: URL): Promise<CheckoutInput> {
  let source: Record<string, unknown> = {};
  if (req.method === "GET") {
    source = Object.fromEntries(url.searchParams.entries());
  } else if (stringValue(req.headers.get("content-type")).includes("application/json")) {
    source = recordValue(await req.json().catch(() => ({})));
  } else {
    source = Object.fromEntries((await req.formData()).entries());
  }
  return {
    plan: stringValue(source.plan).toLowerCase(),
    extraDesktopQuantity: parseExtraDesktopQuantity(
      source.extraDesktopQuantity || source.extra_desktop_quantity || source.extraCaixas,
    ),
    email: stringValue(source.email).toLowerCase(),
    claimToken: stringValue(source.claimToken || source.claim_token),
  };
}

async function renewalInputFromRequest(req: Request, url: URL) {
  let source: Record<string, unknown> = {};
  if (req.method === "GET") {
    source = Object.fromEntries(url.searchParams.entries());
  } else if (stringValue(req.headers.get("content-type")).includes("application/json")) {
    source = recordValue(await req.json().catch(() => ({})));
  } else {
    source = Object.fromEntries((await req.formData()).entries());
  }
  return {
    licenseKey: stringValue(source.licenseKey || source.license_key),
    plan: stringValue(source.plan).toLowerCase(),
    email: stringValue(source.email).toLowerCase(),
  };
}

function checkoutReturnUrls(req: Request) {
  const origin = stringValue(req.headers.get("origin")) || siteUrl();
  const success = new URL(siteUrl());
  success.searchParams.set("checkout", "sucesso");
  success.searchParams.set("session_id", "{CHECKOUT_SESSION_ID}");
  return {
    successUrl: success.toString().replace("%7BCHECKOUT_SESSION_ID%7D", "{CHECKOUT_SESSION_ID}"),
    cancelUrl: `${origin.replace(/\/$/, "")}/#planos`,
  };
}

async function fetchStripeSession(sessionId: string) {
  const response = await stripeRequest(
    `${STRIPE_API_URL}/checkout/sessions/${encodeURIComponent(sessionId)}`,
  );
  if (!response.ok) throw new Error(response.data.error?.message || "Não foi possível consultar o pagamento.");
  return response.data as Record<string, unknown>;
}

async function fetchStripeSubscription(subscriptionId: string) {
  const response = await stripeRequest(`${STRIPE_API_URL}/subscriptions/${encodeURIComponent(subscriptionId)}`);
  if (!response.ok) throw new Error(response.data.error?.message || "Não foi possível consultar a assinatura.");
  return response.data as Record<string, unknown>;
}

async function stripeRequest(url: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${requiredEnv("STRIPE_SECRET_KEY")}`);
  if (init.body instanceof URLSearchParams) {
    headers.set("Content-Type", "application/x-www-form-urlencoded");
  }
  const response = await fetch(url, { ...init, headers });
  return {
    ok: response.ok,
    status: response.status,
    data: await response.json().catch(() => ({})),
  };
}

function stripeSubscriptionPeriod(subscription: Record<string, unknown>, plan: BalcaoPlan) {
  const start = epochToIso(subscription.current_period_start) || new Date().toISOString();
  const end = epochToIso(subscription.current_period_end) || addPeriod(new Date(start), plan.interval).toISOString();
  return { start, end };
}

function epochToIso(value: unknown) {
  const seconds = Number(value);
  return Number.isFinite(seconds) && seconds > 0 ? new Date(seconds * 1000).toISOString() : "";
}

function addPeriod(date: Date, interval: "month" | "year") {
  const next = new Date(date);
  if (interval === "year") next.setUTCFullYear(next.getUTCFullYear() + 1);
  else next.setUTCMonth(next.getUTCMonth() + 1);
  return next;
}

function normalizeSubscriptionStatus(value: unknown) {
  const status = stringValue(value).toLowerCase();
  if (status === "active") return "ACTIVE";
  if (status === "trialing") return "TRIALING";
  if (status === "past_due" || status === "unpaid") return "PAST_DUE";
  if (status === "paused") return "PAUSED";
  if (status === "canceled" || status === "incomplete_expired") return "CANCELED";
  return "PENDING";
}

async function findLegacyLicenseByCheckoutSession(sessionId: string) {
  const { data, error } = await serviceClient().from("bv_licenses")
    .select("key")
    .eq("profile->>checkout_session_id", sessionId)
    .limit(1)
    .maybeSingle();
  if (error) throw new Error(`Falha ao consultar compatibilidade da licença: ${error.message}`);
  return data;
}

async function findLegacyLicenseByKey(key: string) {
  if (!key) return null;
  const { data, error } = await serviceClient().from("bv_licenses")
    .select("*")
    .eq("key", key)
    .maybeSingle();
  if (error) throw new Error(`Falha ao consultar assinatura antiga: ${error.message}`);
  return data as Record<string, unknown> | null;
}

async function createLegacyLicenseKey(expiresAt: Date, planId: BalcaoPlanCode) {
  const expiresText = activationExpirationText(expiresAt);
  const serial = `ONL${crypto.randomUUID().replaceAll("-", "").slice(0, 9).toUpperCase()}`;
  const signature = (await hmacHex(requiredEnv("BALCAO_LICENSE_SECRET"), `BLV|${expiresText}|${serial}`)).slice(0, 10);
  return `BLV-${expiresText}-${serial}-${signature}`;
}

function normalizeLegacyPlanCode(requested: unknown, legacy: Record<string, unknown>): BalcaoPlanCode {
  const value = stringValue(requested).toLowerCase();
  if (value in BALCAO_PLANS) return value as BalcaoPlanCode;
  const text = `${value} ${stringValue(legacy.plan)} ${stringValue(legacy.client_kind)}`.toLowerCase();
  const annual = text.includes("anual") || text.includes("year") || text.includes("ano");
  const complete = text.includes("completo") || text.includes("complete") || text.includes("online");
  return complete
    ? (annual ? "completo-anual" : "completo-mensal")
    : (annual ? "basico-anual" : "basico-mensal");
}

function isPaidSession(session: Record<string, unknown>) {
  return session.payment_status === "paid" || session.status === "complete";
}

function activationExpirationText(date: Date) {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getUTCFullYear()}${pad(date.getUTCMonth() + 1)}${pad(date.getUTCDate())}${pad(date.getUTCHours())}${pad(date.getUTCMinutes())}`;
}

function normalizeLicenseKey(value: unknown) {
  return stringValue(value).toUpperCase().replaceAll(" ", "").replaceAll("_", "-");
}

function randomToken() {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return Array.from(bytes).map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

async function isValidStripeSignature(payload: string, header: string, secret: string) {
  const values = Object.fromEntries(header.split(",").map((part) => {
    const [key, value] = part.split("=");
    return [key, value];
  }));
  if (!values.t || !values.v1) return false;
  const digest = await hmacHex(secret, `${values.t}.${payload}`);
  return safeEqualHex(digest.toLowerCase(), values.v1.toLowerCase());
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
  return Array.from(new Uint8Array(digest)).map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

function safeEqualHex(a: string, b: string) {
  if (a.length !== b.length) return false;
  let result = 0;
  for (let index = 0; index < a.length; index += 1) result |= a.charCodeAt(index) ^ b.charCodeAt(index);
  return result === 0;
}

async function trackAdminAnalytics(type: string, data: Record<string, unknown>) {
  const endpoint = stringValue(Deno.env.get("BALCAO_ADMIN_ANALYTICS_URL"))
    || stringValue(Deno.env.get("ADMIN_ANALYTICS_URL"))
    || DEFAULT_ADMIN_ANALYTICS_URL;
  if (!endpoint) return;
  await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ type, ...data }),
  });
}

function siteUrl() {
  return (stringValue(Deno.env.get("BALCAO_CHECKOUT_SUCCESS_URL")) || DEFAULT_SITE_URL).replace(/\/$/, "");
}

function wantsJson(req: Request) {
  return stringValue(req.headers.get("content-type")).includes("application/json")
    || stringValue(req.headers.get("accept")).includes("application/json");
}

async function authenticatedUser(req: Request) {
  const authorization = stringValue(req.headers.get("authorization"));
  if (!authorization.toLowerCase().startsWith("bearer ")) return null;
  const token = authorization.slice(7).trim();
  if (!token) return null;
  const client = createClient(requiredEnv("SUPABASE_URL"), requiredEnv("SUPABASE_ANON_KEY"), {
    auth: { persistSession: false },
  });
  const { data, error } = await client.auth.getUser(token);
  if (error || !data.user) return null;
  return data.user;
}

function serviceClient() {
  return createClient(requiredEnv("SUPABASE_URL"), requiredEnv("SUPABASE_SERVICE_ROLE_KEY"), {
    auth: { persistSession: false },
  });
}

function requiredEnv(name: string) {
  const value = stringValue(Deno.env.get(name));
  if (!value) throw new Error(`${name} não configurado.`);
  return value;
}

function recordValue(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : {};
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function json(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" },
  });
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : "Erro inesperado.";
}
