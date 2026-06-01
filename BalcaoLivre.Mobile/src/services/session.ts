import * as Application from "expo-application";
import * as Crypto from "expo-crypto";
import * as SecureStore from "expo-secure-store";
import Constants from "expo-constants";
import { Session, StoreProfile } from "../types";
import { normalizeKey } from "../utils/format";

const SESSION_KEY = "balcao_livre_mobile_session";
const DEVICE_KEY = "balcao_livre_mobile_device_id";

export function defaultAdminApiUrl() {
  return String(Constants.expoConfig?.extra?.adminApiUrl || "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/license");
}

export function defaultIFoodApiUrl() {
  return String(Constants.expoConfig?.extra?.ifoodApiUrl || "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood");
}

export async function deviceIdentity() {
  let deviceId = await SecureStore.getItemAsync(DEVICE_KEY);
  if (!deviceId) {
    deviceId = Crypto.randomUUID();
    await SecureStore.setItemAsync(DEVICE_KEY, deviceId);
  }

  const raw = [
    "mobile",
    Application.applicationId || "br.com.balcaolivrepdv.mobile",
    deviceId
  ].join("|");
  const machineHash = await Crypto.digestStringAsync(Crypto.CryptoDigestAlgorithm.SHA256, raw);
  const machineCode = `MOB-${machineHash.slice(0, 8).toUpperCase()}`;
  return { machineHash, machineCode };
}

export async function loadSession(): Promise<Session | null> {
  const text = await SecureStore.getItemAsync(SESSION_KEY);
  if (!text) return null;
  try {
    return JSON.parse(text) as Session;
  } catch {
    return null;
  }
}

export async function saveSession(session: Session) {
  await SecureStore.setItemAsync(SESSION_KEY, JSON.stringify(session));
}

export async function clearSession() {
  await SecureStore.deleteItemAsync(SESSION_KEY);
}

export function makeActivationPayload(licenseKey: string, profile: StoreProfile, machineHash: string, machineCode: string) {
  return {
    eventName: "mobile.activate",
    licenseKey: normalizeKey(licenseKey),
    machineHash,
    machineCode,
    clientKind: "mobile-expo",
    appVersion: `mobile-${Application.nativeApplicationVersion || "0.1.0"}`,
    profile: {
      email: profile.email.trim().toLowerCase(),
      businessName: profile.businessName.trim(),
      ownerName: profile.ownerName.trim(),
      legalName: profile.businessName.trim(),
      cnpj: profile.document.trim(),
      phone: profile.phone.trim(),
      city: profile.city.trim(),
      state: profile.state.trim().toUpperCase()
    },
    settings: {
      printLayout: "MOBILE",
      adminSyncEnabled: true,
      mobile: true
    },
    metrics: {}
  };
}
