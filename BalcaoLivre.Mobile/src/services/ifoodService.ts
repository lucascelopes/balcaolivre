import { enqueueEvent } from "../data/db";
import { Session } from "../types";

async function postJson<T>(baseUrl: string, path: string, body: unknown): Promise<T> {
  const response = await fetch(`${baseUrl.replace(/\/$/, "")}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body)
  });
  const data = await response.json();
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || `iFood retornou HTTP ${response.status}.`);
  }
  return data as T;
}

function basePayload(session: Session) {
  return {
    licenseKey: session.licenseKey,
    machineHash: session.machineHash,
    machineCode: session.machineCode,
    businessName: session.profile.businessName,
    cnpj: session.profile.document,
    phone: session.profile.phone,
    city: session.profile.city,
    state: session.profile.state,
    clientKind: "mobile-expo",
    appVersion: "mobile-0.1.0",
    profile: session.profile
  };
}

export async function pollIFoodOrders(session: Session) {
  const result = await postJson<{ ok: boolean; orders?: Record<string, unknown>[] }>(session.ifoodApiUrl, "/orders/sync", basePayload(session));
  for (const order of result.orders ?? []) {
    await enqueueEvent("ifood.order_imported", order);
  }
  return result.orders ?? [];
}

export async function sendIFoodAction(session: Session, orderId: string, action: string) {
  return postJson<Record<string, unknown>>(session.ifoodApiUrl, "/orders/action", {
    ...basePayload(session),
    orderId,
    action
  });
}
