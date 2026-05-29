import crypto from "crypto";
import { ensurePaidLicenseFromSession } from "../../checkout/licensing";

export const runtime = "nodejs";

export async function POST(request) {
  const webhookSecret = process.env.STRIPE_WEBHOOK_SECRET;
  const signature = request.headers.get("stripe-signature") || "";
  const payload = await request.text();

  if (!webhookSecret) {
    return Response.json({ error: "Webhook nao configurado." }, { status: 500 });
  }

  if (!isValidStripeSignature(payload, signature, webhookSecret)) {
    return Response.json({ error: "Assinatura invalida." }, { status: 400 });
  }

  const event = JSON.parse(payload);

  if (event.type === "checkout.session.completed" || event.type === "checkout.session.async_payment_succeeded") {
    await ensurePaidLicenseFromSession(event.data.object);
  }

  return Response.json({ received: true });
}

function isValidStripeSignature(payload, header, secret) {
  const parts = Object.fromEntries(
    header.split(",").map((part) => {
      const [key, value] = part.split("=");
      return [key, value];
    })
  );
  const timestamp = parts.t;
  const expected = parts.v1;

  if (!timestamp || !expected) return false;

  const signedPayload = `${timestamp}.${payload}`;
  const digest = crypto.createHmac("sha256", secret).update(signedPayload).digest("hex");
  const expectedBuffer = Buffer.from(expected, "hex");
  const digestBuffer = Buffer.from(digest, "hex");

  return expectedBuffer.length === digestBuffer.length && crypto.timingSafeEqual(expectedBuffer, digestBuffer);
}
