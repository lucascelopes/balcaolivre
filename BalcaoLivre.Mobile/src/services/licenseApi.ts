import { Session, Snapshot, StoreProfile, SyncEvent } from "../types";
import { buildSnapshot } from "../data/db";
import { defaultAdminApiUrl, defaultIFoodApiUrl, deviceIdentity, makeActivationPayload, saveSession } from "./session";
import { normalizeKey } from "../utils/format";

type ApiResult = {
  ok: boolean;
  message?: string;
  plan?: string;
  expiresAt?: string;
  snapshot?: Partial<Snapshot>;
  acceptedEventIds?: string[];
  pullEvents?: SyncEvent[];
};

async function postJson<T>(baseUrl: string, path: string, body: unknown): Promise<T> {
  const url = `${baseUrl.replace(/\/$/, "")}${path}`;
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body)
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : {};
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || `Servidor retornou HTTP ${response.status}.`);
  }
  return data as T;
}

export async function activateMobile(licenseKey: string, profile: StoreProfile, adminApiUrl = defaultAdminApiUrl()) {
  const identity = await deviceIdentity();
  const payload = makeActivationPayload(licenseKey, profile, identity.machineHash, identity.machineCode);
  const result = await postJson<ApiResult>(adminApiUrl, "/api/app/activate", payload);
  const session: Session = {
    licenseKey: normalizeKey(licenseKey),
    machineHash: identity.machineHash,
    machineCode: identity.machineCode,
    adminApiUrl,
    ifoodApiUrl: defaultIFoodApiUrl(),
    plan: result.plan || "Mobile",
    expiresAt: result.expiresAt || "",
    profile
  };
  await saveSession(session);
  return session;
}

export async function bootstrapMobile(session: Session) {
  const payload = {
    eventName: "mobile.bootstrap",
    licenseKey: session.licenseKey,
    machineHash: session.machineHash,
    machineCode: session.machineCode,
    clientKind: "mobile-expo",
    appVersion: "mobile-0.1.0",
    profile: session.profile
  };
  return postJson<ApiResult>(session.adminApiUrl, "/api/mobile/bootstrap", payload);
}

export async function syncMobile(session: Session, events: SyncEvent[]) {
  const snapshot = await buildSnapshot();
  const payload = {
    eventName: "mobile.sync",
    licenseKey: session.licenseKey,
    machineHash: session.machineHash,
    machineCode: session.machineCode,
    clientKind: "mobile-expo",
    appVersion: "mobile-0.1.0",
    profile: session.profile,
    events,
    snapshot
  };
  return postJson<ApiResult>(session.adminApiUrl, "/api/mobile/sync", payload);
}

export async function backupMobile(session: Session) {
  const snapshot = await buildSnapshot();
  const payload = {
    eventName: "mobile.backup",
    licenseKey: session.licenseKey,
    machineHash: session.machineHash,
    machineCode: session.machineCode,
    clientKind: "mobile-expo",
    appVersion: "mobile-0.1.0",
    profile: session.profile,
    snapshot
  };
  return postJson<ApiResult>(session.adminApiUrl, "/api/mobile/backup", payload);
}
