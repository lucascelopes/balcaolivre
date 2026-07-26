import { billingFromPlanId, getPlan, trackAdminAnalytics } from "./licensing";

const STRIPE_CHECKOUT_URL = "https://api.stripe.com/v1/checkout/sessions";

const prices = {
  mensal: process.env.STRIPE_PRICE_OFFLINE_MENSAL || "",
  anual: process.env.STRIPE_PRICE_OFFLINE_ANUAL || "",
  "basico-mensal": process.env.STRIPE_PRICE_BASICO_MENSAL || "",
  "basico-anual": process.env.STRIPE_PRICE_BASICO_ANUAL || "",
  "completo-mensal": process.env.STRIPE_PRICE_COMPLETO_MENSAL || "",
  "completo-anual": process.env.STRIPE_PRICE_COMPLETO_ANUAL || "",
  "offline-mensal": process.env.STRIPE_PRICE_OFFLINE_MENSAL || "",
  "offline-anual": process.env.STRIPE_PRICE_OFFLINE_ANUAL || "",
  "online-mensal": process.env.STRIPE_PRICE_ONLINE_MENSAL || "",
  "online-anual": process.env.STRIPE_PRICE_ONLINE_ANUAL || ""
};

function planFromRequest(request, formData) {
  if (formData) {
    return String(formData.get("plan") || "").toLowerCase();
  }

  return new URL(request.url).searchParams.get("plan")?.toLowerCase() || "";
}

async function createCheckout(request, formData) {
  const plan = planFromRequest(request, formData);
  const price = prices[plan];
  const dynamicPlan = getPlan(plan);

  if (!price && !dynamicPlan) {
    return Response.json({ error: "Plano invalido." }, { status: 400 });
  }

  const secretKey = process.env.STRIPE_SECRET_KEY;

  if (!secretKey) {
    return Response.json(
      { error: "STRIPE_SECRET_KEY nao configurada no servidor." },
      { status: 500 }
    );
  }

  const origin = request.headers.get("origin") || new URL(request.url).origin;
  const params = new URLSearchParams({
    mode: "subscription",
    "line_items[0][quantity]": "1",
    allow_promotion_codes: "true",
    client_reference_id: plan,
    "metadata[plan]": plan,
    success_url: `${origin}/?checkout=sucesso&session_id={CHECKOUT_SESSION_ID}`,
    cancel_url: `${origin}/#planos`
  });

  if (price && !dynamicPlan) {
    params.set("line_items[0][price]", price);
  } else {
    params.set("line_items[0][price_data][currency]", "brl");
    params.set("line_items[0][price_data][unit_amount]", String(dynamicPlan.amount));
    params.set("line_items[0][price_data][recurring][interval]", dynamicPlan.interval);
    params.set("line_items[0][price_data][product_data][name]", dynamicPlan.name);
  }

  const stripeResponse = await fetch(STRIPE_CHECKOUT_URL, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${secretKey}`,
      "Content-Type": "application/x-www-form-urlencoded"
    },
    body: params
  });

  const data = await stripeResponse.json();

  if (!stripeResponse.ok || !data.url) {
    return Response.json(
      { error: data.error?.message || "Nao foi possivel abrir o checkout." },
      { status: stripeResponse.status || 500 }
    );
  }

  await trackAdminAnalytics("checkout.started", {
    plan,
    billing: billingFromPlanId(plan),
    amountCents: dynamicPlan?.amount || 0,
    currency: "BRL",
    source: "next-checkout",
    path: new URL(request.url).pathname,
    url: request.url
  }).catch(() => null);

  return Response.redirect(data.url, 303);
}

export async function GET(request) {
  return createCheckout(request);
}

export async function POST(request) {
  const formData = await request.formData();
  return createCheckout(request, formData);
}
