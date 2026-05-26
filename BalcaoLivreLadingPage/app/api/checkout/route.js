const STRIPE_CHECKOUT_URL = "https://api.stripe.com/v1/checkout/sessions";

const prices = {
  mensal: "price_1Tb3fcGTOG08DTzfMZxooHqI",
  anual: "price_1Tb3fcGTOG08DTzfsyFfmjRZ"
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

  if (!price) {
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
    "line_items[0][price]": price,
    "line_items[0][quantity]": "1",
    "automatic_payment_methods[enabled]": "true",
    allow_promotion_codes: "true",
    success_url: `${origin}/?checkout=sucesso&plano=${plan}`,
    cancel_url: `${origin}/#preco`
  });

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

  return Response.redirect(data.url, 303);
}

export async function GET(request) {
  return createCheckout(request);
}

export async function POST(request) {
  const formData = await request.formData();
  return createCheckout(request, formData);
}
