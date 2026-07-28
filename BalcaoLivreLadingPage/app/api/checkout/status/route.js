const DEFAULT_CHECKOUT_URL =
  "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout";

export async function GET(request) {
  const source = new URL(request.url);
  const sessionId = source.searchParams.get("session_id");
  if (!sessionId) {
    return Response.json({ ok: false, message: "Sessão não informada." }, { status: 400 });
  }

  const base =
    process.env.BALCAO_CHECKOUT_FUNCTION_URL ||
    process.env.NEXT_PUBLIC_BALCAO_CHECKOUT_FUNCTION_URL ||
    DEFAULT_CHECKOUT_URL;
  const response = await fetch(
    `${base.replace(/\/$/, "")}/status?session_id=${encodeURIComponent(sessionId)}`,
    { cache: "no-store" }
  );
  return new Response(await response.text(), {
    status: response.status,
    headers: {
      "Content-Type": response.headers.get("content-type") || "application/json; charset=utf-8",
      "Cache-Control": "no-store"
    }
  });
}
