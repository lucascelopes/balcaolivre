import { Session } from "../types";

async function postJson<T>(baseUrl: string, path: string, body: unknown): Promise<T> {
  const response = await fetch(`${baseUrl.replace(/\/$/, "")}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body)
  });
  const data = await response.json();
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || `Pagamento retornou HTTP ${response.status}.`);
  }
  return data as T;
}

function basePayload(session: Session) {
  return {
    licenseKey: session.licenseKey,
    machineHash: session.machineHash,
    machineCode: session.machineCode,
    clientKind: "mobile-expo",
    appVersion: "mobile-0.1.0",
    profile: session.profile
  };
}

export async function createMercadoPagoCharge(session: Session, amount: number, method: string, localReference: string) {
  return postJson<Record<string, unknown>>(session.adminApiUrl, "/api/app/payments/mercadopago/point/charge", {
    ...basePayload(session),
    amount,
    method,
    localReference,
    description: `Balcao Livre Mobile ${localReference}`
  });
}

export async function getMercadoPagoStatus(session: Session, attemptId: string) {
  return postJson<Record<string, unknown>>(session.adminApiUrl, "/api/app/payments/mercadopago/point/status", {
    ...basePayload(session),
    attemptId
  });
}
