import { ensurePaidLicenseFromSession, fetchStripeSession } from "../licensing";

export async function GET(request) {
  const sessionId = new URL(request.url).searchParams.get("session_id");

  if (!sessionId) {
    return Response.json({ ok: false, message: "Sessao nao informada." }, { status: 400 });
  }

  try {
    const session = await fetchStripeSession(sessionId);
    const result = await ensurePaidLicenseFromSession(session);

    if (!result.paid) {
      return Response.json({ ok: false, paid: false, message: "Pagamento ainda nao confirmado." }, { status: 202 });
    }

    return Response.json({ ok: true, paid: true, license: result.license });
  } catch (error) {
    return Response.json(
      { ok: false, message: error instanceof Error ? error.message : "Nao foi possivel confirmar o pagamento." },
      { status: 500 }
    );
  }
}
