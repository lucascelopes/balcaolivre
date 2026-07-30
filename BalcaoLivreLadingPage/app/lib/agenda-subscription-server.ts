import { env } from "cloudflare:workers";
import { getAgendaD1 } from "../../db/index";
import {
  AgendaAndroidError,
  agendaAndroidOptionsResponse,
  applyAgendaStripeEntitlement,
  ensureAgendaEntitlementForUser,
  getAgendaEntitlementForUser,
} from "./agenda-android-server";
import {
  AgendaAccountError,
  agendaAccountErrorResponse,
  authenticateAgendaAccountUser,
} from "./agenda-account-server";
import { agendaAndroidErrorResponse } from "./agenda-android-server";

type JsonObject = Record<string, unknown>;

type ClaimRow = {
  claim_id: string;
  checkout_session_id: string;
  provider_customer_id: string | null;
  provider_subscription_id: string | null;
  plan: string;
  status: string;
  user_id: string | null;
  checkout_email_masked: string | null;
  current_period_ends_at: number | null;
  claimed_at: number | null;
  created_at: number;
  updated_at: number;
};

function runtimeValue(name: string) {
  const runtime = env as unknown as Record<string, unknown>;
  const value = runtime?.[name];
  if (typeof value === "string" && value.trim()) return value.trim();
  return typeof process !== "undefined" ? process.env[name]?.trim() || "" : "";
}

function jsonResponse(data: unknown, status = 200, methods = "GET, POST, OPTIONS") {
  return new Response(JSON.stringify(data), {
    status,
    headers: {
      "Access-Control-Allow-Headers": "Authorization, Content-Type",
      "Access-Control-Allow-Methods": methods,
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Max-Age": "86400",
      "Cache-Control": "no-store, max-age=0",
      "Content-Type": "application/json; charset=utf-8",
      "Referrer-Policy": "no-referrer",
      "X-Content-Type-Options": "nosniff",
    },
  });
}

export const agendaSubscriptionOptionsResponse = agendaAndroidOptionsResponse;

export function agendaSubscriptionErrorResponse(error: unknown) {
  return error instanceof AgendaAccountError
    ? agendaAccountErrorResponse(error)
    : agendaAndroidErrorResponse(error);
}

function objectValue(value: unknown): JsonObject {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as JsonObject)
    : {};
}

function stripeId(value: unknown) {
  if (typeof value === "string") return value;
  const object = objectValue(value);
  return typeof object.id === "string" ? object.id : "";
}

function stripeSeconds(value: unknown) {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric > 0
    ? Math.trunc(numeric * 1000)
    : null;
}

function safeStripeId(value: unknown, prefix: string) {
  const id = String(value || "").trim();
  if (!new RegExp(`^${prefix}_[A-Za-z0-9_]+$`).test(id)) {
    throw new AgendaAndroidError(400, "invalid_checkout", "O pagamento informado e invalido.");
  }
  return id;
}

function normalizedPlan(value: unknown) {
  const plan = String(value || "mensal").trim().toLowerCase();
  if (plan !== "mensal" && plan !== "anual") {
    throw new AgendaAndroidError(400, "invalid_checkout_plan", "Escolha o plano mensal ou anual.");
  }
  return plan;
}

function checkoutUrl(name: string, fallback: string) {
  const configured = runtimeValue(name);
  const value = configured || fallback;
  try {
    const parsed = new URL(value);
    if (parsed.protocol !== "https:" && parsed.protocol !== "http:") throw new Error();
    return parsed.toString();
  } catch {
    throw new AgendaAndroidError(503, "checkout_not_configured", `A variavel ${name} e invalida.`);
  }
}

function maskEmail(value: unknown) {
  const email = String(value || "").trim().toLowerCase();
  const at = email.indexOf("@");
  if (at <= 0) return null;
  const local = email.slice(0, at);
  const domain = email.slice(at + 1);
  if (!domain) return null;
  return `${local.slice(0, Math.min(2, local.length))}${local.length > 2 ? "***" : "*"}@${domain}`;
}

async function checkoutTrialContext(request: Request) {
  const authorization = request.headers.get("authorization") || "";
  if (!authorization.toLowerCase().startsWith("bearer ")) {
    return {
      authenticated: false,
      userId: "",
      daysRemaining: 7,
      endsAt: null as number | null,
    };
  }
  const user = await authenticateAgendaAccountUser(request);
  const account = await getAgendaD1()
    .prepare(
      `SELECT trial_started_at, trial_ends_at
       FROM agenda_cloud_accounts
       WHERE user_id = ?1
       LIMIT 1`,
    )
    .bind(user.id)
    .first<{ trial_started_at: number; trial_ends_at: number }>();
  const now = Date.now();
  const entitlement = await ensureAgendaEntitlementForUser(
    user.id,
    now,
    account
      ? {
          startedAt: Number(account.trial_started_at),
          endsAt: Number(account.trial_ends_at),
        }
      : undefined,
  );
  const endsAt = entitlement.trialEndsAt
    ? Date.parse(entitlement.trialEndsAt)
    : Number(account?.trial_ends_at || 0);
  const daysRemaining =
    Number.isFinite(endsAt) && endsAt > now
      ? Math.max(1, Math.ceil((endsAt - now) / (24 * 60 * 60 * 1000)))
      : 0;
  return {
    authenticated: true,
    userId: user.id,
    daysRemaining,
    endsAt: daysRemaining > 0 ? endsAt : null,
  };
}

async function stripeRequest(
  path: string,
  init?: { method?: "GET" | "POST"; params?: URLSearchParams; idempotencyKey?: string },
) {
  const secretKey = runtimeValue("STRIPE_SECRET_KEY");
  if (!secretKey) {
    throw new AgendaAndroidError(503, "stripe_not_configured", "O pagamento ainda nao foi configurado.");
  }
  const response = await fetch(`https://api.stripe.com${path}`, {
    method: init?.method || "GET",
    headers: {
      Authorization: `Bearer ${secretKey}`,
      ...(init?.params ? { "Content-Type": "application/x-www-form-urlencoded" } : {}),
      ...(init?.idempotencyKey ? { "Idempotency-Key": init.idempotencyKey.slice(0, 255) } : {}),
    },
    body: init?.params,
    cache: "no-store",
  });
  let data: JsonObject = {};
  try {
    data = (await response.json()) as JsonObject;
  } catch {
    // Provider details must not leak to clients.
  }
  if (!response.ok) {
    console.error("Agenda Stripe request failed", path, response.status);
    throw new AgendaAndroidError(502, "stripe_unavailable", "Nao foi possivel acessar o pagamento agora.");
  }
  return data;
}

async function readClaimBySession(sessionId: string) {
  return getAgendaD1()
    .prepare(
      "SELECT * FROM agenda_subscription_claims WHERE checkout_session_id = ?1 LIMIT 1",
    )
    .bind(sessionId)
    .first<ClaimRow>();
}

function subscriptionStatus(subscription: JsonObject) {
  const status = String(subscription.status || "").toLowerCase();
  if (status === "active") return "active";
  if (status === "trialing") return "trialing";
  if (status === "canceled") return "canceled";
  if (status === "paused") return "suspended";
  return "past_due";
}

function subscriptionPeriodEnd(subscription: JsonObject) {
  let period = stripeSeconds(subscription.current_period_end);
  if (period) return period;
  const items = objectValue(subscription.items).data;
  if (Array.isArray(items) && items.length) {
    period = stripeSeconds(objectValue(items[0]).current_period_end);
  }
  return period;
}

async function checkoutContext(sessionId: string) {
  const session = await stripeRequest(
    `/v1/checkout/sessions/${encodeURIComponent(sessionId)}?expand%5B%5D=subscription`,
  );
  const metadata = objectValue(session.metadata);
  const claimId = String(metadata.agenda_claim_id || "").trim();
  const plan = normalizedPlan(metadata.agenda_plan);
  const subscription = objectValue(session.subscription);
  return {
    session,
    claimId,
    plan,
    customerId: stripeId(session.customer),
    subscriptionId: stripeId(session.subscription),
    subscription,
    status: subscriptionStatus(subscription),
    periodEnd: subscriptionPeriodEnd(subscription),
    maskedEmail: maskEmail(
      objectValue(session.customer_details).email || session.customer_email,
    ),
  };
}

export async function createPublicAgendaSubscriptionCheckout(request: Request) {
  const requestUrl = new URL(request.url);
  let body: JsonObject = {};
  if (request.method === "POST") {
    try {
      body = objectValue(await request.json());
    } catch {
      body = {};
    }
  }
  const plan = normalizedPlan(body.plan || requestUrl.searchParams.get("plan"));
  const trial = await checkoutTrialContext(request);
  const secretKey = runtimeValue("STRIPE_SECRET_KEY");
  const priceId = runtimeValue(
    plan === "anual" ? "AGENDA_STRIPE_PRICE_ANUAL" : "AGENDA_STRIPE_PRICE_MENSAL",
  );
  if (!secretKey || !/^price_[A-Za-z0-9_]+$/.test(priceId)) {
    throw new AgendaAndroidError(
      503,
      "checkout_not_configured",
      `O pagamento ${plan} do Agenda Livre ainda nao foi configurado.`,
    );
  }

  const claimId = crypto.randomUUID();
  const origin = new URL(request.url).origin;
  const params = new URLSearchParams({
    mode: "subscription",
    "line_items[0][price]": priceId,
    "line_items[0][quantity]": "1",
    allow_promotion_codes: "true",
    payment_method_collection: "always",
    "subscription_data[trial_settings][end_behavior][missing_payment_method]": "cancel",
    "subscription_data[metadata][agenda_claim_id]": claimId,
    "subscription_data[metadata][agenda_product]": "agenda_livre",
    "subscription_data[metadata][agenda_plan]": plan,
    "metadata[agenda_claim_id]": claimId,
    "metadata[agenda_product]": "agenda_livre",
    "metadata[agenda_plan]": plan,
    "metadata[agenda_trial_days_remaining]": String(trial.daysRemaining),
    success_url: checkoutUrl(
      "AGENDA_CHECKOUT_SUCCESS_URL",
      `https://app.minhaagendalivre.com.br/?checkout=sucesso&session_id={CHECKOUT_SESSION_ID}`,
    ),
    cancel_url: checkoutUrl(
      "AGENDA_CHECKOUT_CANCEL_URL",
      trial.authenticated
        ? `${origin}/?billing=cancelado`
        : `${origin}/agenda-livre/#planos`,
    ),
  });
  if (trial.endsAt !== null) {
    const minimumExactTrialEnd = Date.now() + 48 * 60 * 60 * 1000;
    if (trial.endsAt >= minimumExactTrialEnd) {
      params.set(
        "subscription_data[trial_end]",
        String(Math.floor(trial.endsAt / 1000)),
      );
    } else {
      params.set(
        "subscription_data[trial_period_days]",
        String(trial.daysRemaining),
      );
    }
  } else if (!trial.authenticated) {
    params.set("subscription_data[trial_period_days]", "7");
  }
  if (trial.userId) {
    params.set("metadata[agenda_user_id]", trial.userId);
    params.set("subscription_data[metadata][agenda_user_id]", trial.userId);
  }
  if (plan === "anual") {
    params.set("shipping_address_collection[allowed_countries][0]", "BR");
  }
  const session = await stripeRequest("/v1/checkout/sessions", {
    method: "POST",
    params,
    idempotencyKey: `agenda-public-${plan}-${claimId}`,
  });
  const sessionId = safeStripeId(session.id, "cs");
  const url = String(session.url || "");
  if (!/^https:\/\/.+/i.test(url)) {
    throw new AgendaAndroidError(502, "checkout_unavailable", "Nao foi possivel abrir o pagamento agora.");
  }
  const now = Date.now();
  await getAgendaD1()
    .prepare(
      `INSERT INTO agenda_subscription_claims (
         claim_id, checkout_session_id, plan, status, created_at, updated_at
       ) VALUES (?1, ?2, ?3, 'checkout_open', ?4, ?4)`,
    )
    .bind(claimId, sessionId, plan, now)
    .run();

  if (request.method === "GET" && !request.headers.get("accept")?.includes("application/json")) {
    return Response.redirect(url, 303);
  }
  return jsonResponse(
    {
      ok: true,
      checkout: {
        url,
        sessionId,
        trialDaysRemaining: trial.daysRemaining,
        trialEndsAt: trial.endsAt ? new Date(trial.endsAt).toISOString() : null,
      },
    },
    200,
    "GET, POST, OPTIONS",
  );
}

export async function getAgendaSubscriptionCheckoutStatus(request: Request) {
  const sessionId = safeStripeId(new URL(request.url).searchParams.get("session_id"), "cs");
  let claim = await readClaimBySession(sessionId);
  if (!claim || claim.status === "checkout_open") {
    const context = await checkoutContext(sessionId);
    const complete = String(context.session.status || "") === "complete";
    const now = Date.now();
    await getAgendaD1()
      .prepare(
        `UPDATE agenda_subscription_claims
         SET provider_customer_id = COALESCE(?1, provider_customer_id),
             provider_subscription_id = COALESCE(?2, provider_subscription_id),
             status = ?3,
             checkout_email_masked = COALESCE(?4, checkout_email_masked),
             current_period_ends_at = COALESCE(?5, current_period_ends_at),
             updated_at = ?6
         WHERE checkout_session_id = ?7`,
      )
      .bind(
        context.customerId || null,
        context.subscriptionId || null,
        complete ? context.status : "checkout_open",
        context.maskedEmail,
        context.periodEnd,
        now,
        sessionId,
      )
      .run();
    claim = await readClaimBySession(sessionId);
  }
  if (!claim) {
    throw new AgendaAndroidError(404, "checkout_not_found", "Pagamento nao encontrado.");
  }
  return jsonResponse({
    ok: true,
    checkout: {
      sessionId,
      complete: claim.status !== "checkout_open",
      claimed: Boolean(claim.user_id),
      status: claim.status,
      plan: claim.plan,
      email: claim.checkout_email_masked,
    },
  });
}

export async function claimAgendaSubscription(request: Request) {
  const user = await authenticateAgendaAccountUser(request);
  if (user.kind !== "supabase") {
    throw new AgendaAndroidError(401, "account_required", "Entre na sua conta para ativar a assinatura.");
  }
  let body: JsonObject;
  try {
    body = objectValue(await request.json());
  } catch {
    throw new AgendaAndroidError(400, "invalid_json", "Envie um pagamento valido.");
  }
  const sessionId = safeStripeId(body.sessionId, "cs");
  const context = await checkoutContext(sessionId);
  if (
    String(context.session.status || "") !== "complete" ||
    !context.customerId ||
    !context.subscriptionId ||
    !context.claimId
  ) {
    throw new AgendaAndroidError(409, "checkout_incomplete", "O pagamento ainda nao foi confirmado.");
  }

  const existing = await readClaimBySession(sessionId);
  if (existing?.user_id && existing.user_id !== user.id) {
    throw new AgendaAndroidError(409, "checkout_already_claimed", "Este pagamento ja foi ativado em outra conta.");
  }

  const now = Date.now();
  await getAgendaD1()
    .prepare(
      `INSERT INTO agenda_cloud_accounts (
         user_id, email, payload_json, revision, schema_version,
         trial_started_at, trial_ends_at, last_device_id, created_at, updated_at
       ) VALUES (?1, ?2, NULL, 0, 1, ?3, ?4, '', ?3, ?3)
       ON CONFLICT(user_id) DO UPDATE SET email = excluded.email`,
    )
    .bind(user.id, user.email, now, now + 7 * 24 * 60 * 60 * 1000)
    .run();
  await ensureAgendaEntitlementForUser(user.id, now);

  const metadata = new URLSearchParams({
    "metadata[agenda_user_id]": user.id,
    "metadata[agenda_claim_id]": context.claimId,
    "metadata[agenda_product]": "agenda_livre",
    "metadata[agenda_plan]": context.plan,
  });
  await stripeRequest(
    `/v1/subscriptions/${encodeURIComponent(context.subscriptionId)}`,
    { method: "POST", params: metadata, idempotencyKey: `agenda-claim-sub-${sessionId}-${user.id}` },
  );
  await stripeRequest(
    `/v1/customers/${encodeURIComponent(context.customerId)}`,
    {
      method: "POST",
      params: new URLSearchParams({
        "metadata[agenda_user_id]": user.id,
        "metadata[agenda_product]": "agenda_livre",
      }),
      idempotencyKey: `agenda-claim-customer-${sessionId}-${user.id}`,
    },
  );

  await getAgendaD1()
    .prepare(
      `UPDATE agenda_subscription_claims
       SET provider_customer_id = ?1,
           provider_subscription_id = ?2,
           plan = ?3,
           status = ?4,
           user_id = ?5,
           checkout_email_masked = COALESCE(?6, checkout_email_masked),
           current_period_ends_at = ?7,
           claimed_at = COALESCE(claimed_at, ?8),
           updated_at = ?8
       WHERE checkout_session_id = ?9 AND (user_id IS NULL OR user_id = ?5)`,
    )
    .bind(
      context.customerId,
      context.subscriptionId,
      context.plan,
      context.status,
      user.id,
      context.maskedEmail,
      context.periodEnd,
      now,
      sessionId,
    )
    .run();

  await applyAgendaStripeEntitlement({
    userId: user.id,
    status: context.status,
    eventId: `claim-${sessionId}`.slice(0, 160),
    eventAt: now,
    currentPeriodEndsAt: context.periodEnd,
    providerCustomerId: context.customerId,
    providerSubscriptionId: context.subscriptionId,
  });
  return jsonResponse({
    ok: true,
    entitlement: await getAgendaEntitlementForUser(user.id),
    access: {
      webUrl: "https://app.minhaagendalivre.com.br/",
      windowsUrl:
        "https://minhaagendalivre.com.br/agenda-livre/agenda-livre-windows-1.0.0.zip",
    },
  }, 200, "POST, OPTIONS");
}

export async function createAgendaSubscriptionPortal(request: Request) {
  const user = await authenticateAgendaAccountUser(request);
  const entitlement = await getAgendaEntitlementForUser(user.id);
  const row = await getAgendaD1()
    .prepare(
      "SELECT provider_customer_id FROM agenda_android_entitlements WHERE user_id = ?1 LIMIT 1",
    )
    .bind(user.id)
    .first<{ provider_customer_id: string | null }>();
  if (!row?.provider_customer_id) {
    throw new AgendaAndroidError(409, "customer_not_found", "Esta conta ainda nao possui um pagamento salvo.");
  }
  const params = new URLSearchParams({
    customer: row.provider_customer_id,
    return_url: checkoutUrl(
      "AGENDA_PORTAL_RETURN_URL",
      "https://app.minhaagendalivre.com.br/?billing=atualizado",
    ),
  });
  const portal = await stripeRequest("/v1/billing_portal/sessions", {
    method: "POST",
    params,
    idempotencyKey: `agenda-portal-${user.id}-${crypto.randomUUID()}`,
  });
  const url = String(portal.url || "");
  if (!/^https:\/\/.+/i.test(url)) {
    throw new AgendaAndroidError(502, "portal_unavailable", "Nao foi possivel abrir a assinatura agora.");
  }
  return jsonResponse({ ok: true, portal: { url }, entitlement }, 200, "POST, OPTIONS");
}

export async function getAgendaSubscriptionSummary(request: Request) {
  const user = await authenticateAgendaAccountUser(request);
  const entitlement = await ensureAgendaEntitlementForUser(user.id);
  const row = await getAgendaD1()
    .prepare(
      `SELECT provider_customer_id, provider_subscription_id
       FROM agenda_android_entitlements
       WHERE user_id = ?1
       LIMIT 1`,
    )
    .bind(user.id)
    .first<{ provider_customer_id: string | null; provider_subscription_id: string | null }>();
  let card: { brand: string; last4: string; expMonth: number; expYear: number } | null = null;
  if (row?.provider_customer_id) {
    const customer = await stripeRequest(
      `/v1/customers/${encodeURIComponent(row.provider_customer_id)}?expand%5B%5D=invoice_settings.default_payment_method`,
    );
    let paymentMethod = objectValue(objectValue(customer.invoice_settings).default_payment_method);
    if (!paymentMethod.id && row.provider_subscription_id) {
      const subscription = await stripeRequest(
        `/v1/subscriptions/${encodeURIComponent(row.provider_subscription_id)}?expand%5B%5D=default_payment_method`,
      );
      paymentMethod = objectValue(subscription.default_payment_method);
    }
    const cardData = objectValue(paymentMethod.card);
    const last4 = String(cardData.last4 || "");
    if (/^\d{4}$/.test(last4)) {
      card = {
        brand: String(cardData.brand || "cartao"),
        last4,
        expMonth: Number(cardData.exp_month || 0),
        expYear: Number(cardData.exp_year || 0),
      };
    }
  }
  return jsonResponse({ ok: true, entitlement, card });
}
