const DEFAULT_CHECKOUT_URL =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout";

async function forwardCheckout(request) {
  const target = new URL(
    process.env.BALCAO_CHECKOUT_FUNCTION_URL ||
      process.env.NEXT_PUBLIC_BALCAO_CHECKOUT_FUNCTION_URL ||
      DEFAULT_CHECKOUT_URL
  );

  if (request.method === "GET") {
    const source = new URL(request.url);
    source.searchParams.forEach((value, key) => target.searchParams.set(key, value));
    return Response.redirect(target.toString(), 303);
  }

  const formData = await request.formData();
  const response = await fetch(target, {
    method: "POST",
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
      Origin: new URL(request.url).origin
    },
    body: new URLSearchParams(
      Array.from(formData.entries()).map(([key, value]) => [key, String(value)])
    ),
    redirect: "manual"
  });

  const location = response.headers.get("location");
  if (location && response.status >= 300 && response.status < 400) {
    return Response.redirect(location, 303);
  }

  const body = await response.text();
  return new Response(body, {
    status: response.status,
    headers: {
      "Content-Type": response.headers.get("content-type") || "application/json; charset=utf-8",
      "Cache-Control": "no-store"
    }
  });
}

export async function GET(request) {
  return forwardCheckout(request);
}

export async function POST(request) {
  return forwardCheckout(request);
}
