import { env } from "cloudflare:workers";
import {
  getAgendaD1,
  getOptionalAgendaCatalogR2,
} from "../../db/index";
import {
  AgendaAccountError,
  authenticateAgendaAccountUser,
} from "./agenda-account-server";

const DEFAULT_ROOT_DOMAIN = "minhaagendalivre.com.br";
const DEFAULT_LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const ACTIVE_STATUSES = new Set(["requested", "pending", "confirmed"]);
const TERMINAL_STATUSES = new Set([
  "rejected",
  "cancelled",
  "completed",
  "slot_conflict",
]);
const ALLOWED_STATUSES = new Set([...ACTIVE_STATUSES, ...TERMINAL_STATUSES]);
const RESERVED_SLUGS = new Set([
  "www",
  "app",
  "admin",
  "pdv",
  "cardapio",
  "api",
]);
const FIFTEEN_MINUTES_MS = 15 * 60 * 1000;
const FOUR_HOURS_MS = 4 * 60 * 60 * 1000;
const PUBLIC_LOGO_MAXIMUM_DATA_URL_CHARACTERS = 132_000;
const PUBLIC_LOGO_MAXIMUM_BYTES = 96 * 1024;
const PUBLIC_LOGO_MAXIMUM_DIMENSION = 128;
const CATALOG_HERO_MAXIMUM_DATA_URL_CHARACTERS = 1_450_000;
const CATALOG_HERO_MAXIMUM_BYTES = 1_050_000;
const CATALOG_MEDIA_MAXIMUM_DATA_URL_CHARACTERS = 220_000;
const CATALOG_MEDIA_MAXIMUM_BYTES = 160_000;
const CATALOG_MEDIA_MAXIMUM_ITEMS = 24;

type JsonRecord = Record<string, unknown>;

type StoreRow = {
  id: string;
  owner_user_id: string;
  instance: string;
  license_hash: string;
  machine_hash: string;
  machine_code: string;
  desired_slug: string;
  slug: string;
  name: string;
  segment: string;
  theme_json: string;
  catalog_json: string;
  catalog_version: number;
  catalog_published_at: number;
  generated_at: string;
  last_synced_at: number;
};

type StoreDomainRow = {
  hostname: string;
  store_id: string;
  provider_id: string;
  status: string;
  provider_status: string;
  ssl_status: string;
  cname_target: string;
  validation_records_json: string;
  last_error: string;
  verified_at: number | null;
  created_at: number;
  updated_at: number;
};

type SnapshotRow = {
  services_json: string;
  generated_at: string;
  received_at: number;
};

type BookingRow = {
  id: string;
  store_id: string;
  source: string;
  status: string;
  idempotency_key: string;
  slot_key: string;
  service_id: string;
  service_name: string;
  slot_id: string;
  starts_at: string;
  starts_at_ms: number;
  duration_minutes: number;
  price_cents: number;
  professional_id: string;
  professional_name: string;
  resource_name: string;
  customer_name: string;
  customer_phone: string;
  notes: string;
  appointment_id: string | null;
  message: string | null;
  confirmation_sent_at: number | null;
  reminder_sent_at: number | null;
  confirmed_at: number | null;
  created_at: number;
  updated_at: number;
};

type BookingSlot = {
  id: string;
  time: string;
  start: string;
  professionalId: string;
  professionalName: string;
  resourceName: string;
};

type BookingDay = {
  date: string;
  label: string;
  availableSlots: BookingSlot[];
};

type BookingService = {
  id: string;
  name: string;
  durationMinutes: number;
  price: number;
  originalPrice: number;
  promotionName: string;
  discountPercent: number;
  days: BookingDay[];
};

type InternalIdentity = {
  license: string;
  licenseHash: string;
  machineHash: string;
  machineCode: string;
};

export class AgendaBookingError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = "AgendaBookingError";
    this.status = status;
    this.code = code;
  }
}

function runtimeValue(name: string) {
  const runtime = env as unknown as Record<string, unknown>;
  const value = runtime?.[name];
  if (typeof value === "string" && value.trim()) return value.trim();
  return typeof process !== "undefined" ? process.env[name]?.trim() || "" : "";
}

function rootDomain() {
  return (
    runtimeValue("AGENDA_BOOKING_ROOT_DOMAIN") ||
    runtimeValue("NEXT_PUBLIC_BOOKING_ROOT_DOMAIN") ||
    DEFAULT_ROOT_DOMAIN
  )
    .toLowerCase()
    .replace(/^https?:\/\//, "")
    .replace(/\/$/, "");
}

function publicUrl(slug: string) {
  return `https://${slug}.${rootDomain()}`;
}

function snapshotTtlMs() {
  const parsed = Number(runtimeValue("AGENDA_SNAPSHOT_TTL_SECONDS") || "90");
  const seconds = Number.isFinite(parsed) ? Math.min(300, Math.max(30, parsed)) : 90;
  return seconds * 1000;
}

function jsonHeaders() {
  return {
    "Cache-Control": "no-store, max-age=0",
    "Content-Type": "application/json; charset=utf-8",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  };
}

export function agendaJson(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), { status, headers: jsonHeaders() });
}

export function agendaErrorResponse(error: unknown) {
  if (error instanceof AgendaBookingError) {
    return agendaJson(
      { ok: false, error: { code: error.code, message: error.message } },
      error.status,
    );
  }

  console.error("Agenda booking route failed", error);
  return agendaJson(
    {
      ok: false,
      error: {
        code: "internal_error",
        message: "Não foi possível concluir a operação agora.",
      },
    },
    500,
  );
}

async function readJsonBody(request: Request, maxBytes: number): Promise<JsonRecord> {
  const declaredLength = Number(request.headers.get("content-length") || "0");
  if (Number.isFinite(declaredLength) && declaredLength > maxBytes) {
    throw new AgendaBookingError(413, "payload_too_large", "O conteúdo enviado é muito grande.");
  }

  const text = await request.text();
  if (!text || new TextEncoder().encode(text).byteLength > maxBytes) {
    throw new AgendaBookingError(400, "invalid_body", "Envie os dados solicitados.");
  }

  try {
    const parsed = JSON.parse(text);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error();
    return parsed as JsonRecord;
  } catch {
    throw new AgendaBookingError(400, "invalid_json", "O conteúdo enviado não é um JSON válido.");
  }
}

function stringValue(value: unknown, maxLength: number, fallback = "") {
  const text = String(value ?? "").trim();
  return (text || fallback).slice(0, maxLength);
}

function normalizeSlug(value: unknown) {
  const normalized = stringValue(value, 100)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-")
    .slice(0, 48)
    .replace(/-+$/g, "");

  const candidate = normalized.length >= 3 ? normalized : `agenda-${normalized || "loja"}`;
  return RESERVED_SLUGS.has(candidate) ? `${candidate}-agenda` : candidate;
}

function normalizeInstance(value: unknown) {
  const instance = stringValue(value, 128).toLowerCase();
  if (!/^[a-z0-9][a-z0-9_-]{2,127}$/.test(instance)) {
    throw new AgendaBookingError(400, "invalid_instance", "A identificação da agenda é inválida.");
  }
  return instance;
}

function bytesToHex(bytes: ArrayBuffer) {
  return Array.from(new Uint8Array(bytes))
    .map((value) => value.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

function bytesToBase64Url(bytes: ArrayBuffer) {
  let binary = "";
  for (const value of new Uint8Array(bytes)) binary += String.fromCharCode(value);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
}

async function sha256(value: string) {
  return bytesToHex(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value)));
}

async function hmacSha256(message: string, secret: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  return crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
}

function constantTimeEqual(left: string, right: string) {
  if (left.length !== right.length) return false;
  let difference = 0;
  for (let index = 0; index < left.length; index += 1) {
    difference |= left.charCodeAt(index) ^ right.charCodeAt(index);
  }
  return difference === 0;
}

function parseLicenseExpiration(value: string) {
  if (!/^\d{12}$/.test(value)) return null;
  const year = Number(value.slice(0, 4));
  const month = Number(value.slice(4, 6));
  const day = Number(value.slice(6, 8));
  const hour = Number(value.slice(8, 10));
  const minute = Number(value.slice(10, 12));
  if (
    year < 2024 ||
    month < 1 ||
    month > 12 ||
    day < 1 ||
    day > 31 ||
    hour > 23 ||
    minute > 59
  ) {
    return null;
  }
  const localCheck = new Date(Date.UTC(year, month - 1, day, hour, minute));
  if (
    localCheck.getUTCFullYear() !== year ||
    localCheck.getUTCMonth() !== month - 1 ||
    localCheck.getUTCDate() !== day
  ) {
    return null;
  }
  // The signed value is produced in America/Sao_Paulo local time (UTC-03).
  return Date.UTC(year, month - 1, day, hour + 3, minute, 59, 999);
}

async function authenticateInternal(request: Request): Promise<InternalIdentity> {
  const license = stringValue(request.headers.get("x-agenda-license"), 256).toUpperCase();
  const machineHash = stringValue(request.headers.get("x-agenda-machine"), 256).toLowerCase();
  const machineCode = stringValue(request.headers.get("x-agenda-machine-code"), 64).toUpperCase();
  const parts = license.split("-").filter(Boolean);

  if (
    parts.length !== 4 ||
    parts[0] !== "BLV" ||
    !/^[A-Z0-9]{8,64}$/.test(parts[2]) ||
    !/^[A-F0-9]{10}$/.test(parts[3]) ||
    !/^[a-f0-9]{32,128}$/.test(machineHash) ||
    !/^[A-F0-9]{8,32}$/.test(machineCode)
  ) {
    throw new AgendaBookingError(401, "invalid_license", "Credenciais da agenda inválidas.");
  }

  const expiresAt = parseLicenseExpiration(parts[1]);
  if (!expiresAt || expiresAt <= Date.now()) {
    throw new AgendaBookingError(401, "expired_license", "A licença da agenda está vencida.");
  }
  if (
    machineHash.slice(0, machineCode.length).toUpperCase() !== machineCode ||
    !parts[2].endsWith(machineCode)
  ) {
    throw new AgendaBookingError(401, "machine_mismatch", "A licença não pertence a este computador.");
  }

  const secret = runtimeValue("AGENDA_LICENSE_SECRET") || DEFAULT_LICENSE_SECRET;
  const signature = bytesToHex(
    await hmacSha256(`BLV|${parts[1]}|${parts[2]}`, secret),
  ).slice(0, 10);
  if (!constantTimeEqual(signature, parts[3])) {
    throw new AgendaBookingError(401, "invalid_license", "Credenciais da agenda inválidas.");
  }

  return {
    license,
    licenseHash: await sha256(license),
    machineHash,
    machineCode,
  };
}

async function authenticateCatalogOwner(request: Request) {
  try {
    return await authenticateAgendaAccountUser(request);
  } catch (error) {
    if (error instanceof AgendaAccountError) {
      throw new AgendaBookingError(error.status, error.code, error.message);
    }
    throw error;
  }
}

function sanitizeTheme(value: unknown) {
  if (typeof value === "string") return stringValue(value, 80);
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};

  const allowed = new Set([
    "id",
    "name",
    "key",
    "theme",
    "accent",
    "accentcolor",
    "accentsoft",
    "accentdark",
    "onaccent",
    "primary",
    "primarycolor",
    "secondary",
    "secondarycolor",
    "background",
    "backgroundcolor",
    "appbackground",
    "surface",
    "surfacecolor",
    "foreground",
    "text",
    "textcolor",
    "muted",
    "border",
    "line",
    "ink",
    "fontfamily",
    "palette",
    "colors",
    "logourl",
  ]);
  const output: JsonRecord = {};
  for (const [key, raw] of Object.entries(value as JsonRecord).slice(0, 40)) {
    if (!allowed.has(key.toLowerCase())) continue;
    if (key.toLowerCase() === "logourl") {
      output.logoUrl = sanitizePublicLogoDataUrl(raw);
      continue;
    }
    if (typeof raw === "string") {
      output[key] = stringValue(raw, 160);
    } else if (typeof raw === "number" || typeof raw === "boolean") {
      output[key] = raw;
    } else if (
      raw &&
      typeof raw === "object" &&
      !Array.isArray(raw) &&
      ["palette", "colors"].includes(key.toLowerCase())
    ) {
      const nested: JsonRecord = {};
      for (const [nestedKey, nestedValue] of Object.entries(raw as JsonRecord).slice(0, 30)) {
        if (typeof nestedValue === "string") nested[nestedKey] = stringValue(nestedValue, 160);
      }
      output[key] = nested;
    }
  }
  return output;
}

function sanitizePublicLogoDataUrl(value: unknown) {
  if (typeof value !== "string") {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }
  const dataUrl = value.trim();
  if (!dataUrl) return "";
  if (dataUrl.length > PUBLIC_LOGO_MAXIMUM_DATA_URL_CHARACTERS) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é muito grande.");
  }

  const match = /^data:image\/png;base64,([A-Za-z0-9+/]+={0,2})$/.exec(dataUrl);
  if (!match || match[1].length % 4 !== 0) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }

  let bytes: string;
  try {
    bytes = atob(match[1]);
  } catch {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }
  if (bytes.length < 24 || bytes.length > PUBLIC_LOGO_MAXIMUM_BYTES) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }

  const pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
  if (pngSignature.some((expected, index) => bytes.charCodeAt(index) !== expected)) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }
  const readUint32 = (offset: number) =>
    (((bytes.charCodeAt(offset) << 24) >>> 0) |
      (bytes.charCodeAt(offset + 1) << 16) |
      (bytes.charCodeAt(offset + 2) << 8) |
      bytes.charCodeAt(offset + 3)) >>> 0;
  const width = readUint32(16);
  const height = readUint32(20);
  if (
    width < 1 ||
    height < 1 ||
    width > PUBLIC_LOGO_MAXIMUM_DIMENSION ||
    height > PUBLIC_LOGO_MAXIMUM_DIMENSION
  ) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja possui dimensões inválidas.");
  }

  return dataUrl;
}

function normalizeCustomDomain(value: unknown) {
  const raw = stringValue(value, 300)
    .toLowerCase()
    .replace(/^https?:\/\//, "")
    .split("/")[0]
    .split(":")[0]
    .replace(/\.$/, "");
  if (!raw) return "";
  if (
    raw.length > 253 ||
    !raw.includes(".") ||
    raw === rootDomain() ||
    raw.endsWith(`.${rootDomain()}`) ||
    !raw.split(".").every((label) =>
      /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/.test(label),
    )
  ) {
    throw new AgendaBookingError(
      400,
      "invalid_custom_domain",
      "Informe um domínio próprio válido, como www.seusalao.com.br.",
    );
  }
  return raw;
}

type DomainProvisioningState = {
  providerId: string;
  status: "pending" | "active" | "failed";
  providerStatus: string;
  sslStatus: string;
  cnameTarget: string;
  validationRecordsJson: string;
  lastError: string;
  verifiedAt: number | null;
};

function cloudflareSaasConfiguration() {
  return {
    zoneId: runtimeValue("CLOUDFLARE_SAAS_ZONE_ID"),
    apiToken: runtimeValue("CLOUDFLARE_SAAS_API_TOKEN"),
    cnameTarget:
      runtimeValue("CLOUDFLARE_SAAS_CNAME_TARGET") ||
      `customers.${rootDomain()}`,
  };
}

type CloudflareSaasConfiguration = ReturnType<typeof cloudflareSaasConfiguration>;

function cloudflareErrorMessage(payload: JsonRecord, fallback: string) {
  const errors = Array.isArray(payload.errors) ? payload.errors : [];
  const message = errors
    .map((entry) =>
      entry && typeof entry === "object"
        ? stringValue((entry as JsonRecord).message, 240)
        : "",
    )
    .filter(Boolean)
    .join(" ");
  return message || fallback;
}

async function ensureCloudflareFallbackOrigin(
  configuration: CloudflareSaasConfiguration,
) {
  const endpoint =
    `https://api.cloudflare.com/client/v4/zones/${configuration.zoneId}/custom_hostnames/fallback_origin`;
  const headers = {
    Authorization: `Bearer ${configuration.apiToken}`,
    "Content-Type": "application/json",
  };
  const currentResponse = await fetch(endpoint, {
    method: "GET",
    headers,
    cache: "no-store",
  });
  let currentPayload: JsonRecord = {};
  try {
    currentPayload = await currentResponse.json() as JsonRecord;
  } catch {
    // A PUT below can still initialize a missing fallback origin.
  }
  const current = currentPayload.result &&
      typeof currentPayload.result === "object" &&
      !Array.isArray(currentPayload.result)
    ? currentPayload.result as JsonRecord
    : null;
  const currentOrigin = current ? stringValue(current.origin, 255).toLowerCase() : "";
  const currentStatus = current ? stringValue(current.status, 80).toLowerCase() : "";
  if (
    currentResponse.ok &&
    currentPayload.success !== false &&
    currentOrigin === configuration.cnameTarget.toLowerCase() &&
    ["initializing", "pending_deployment", "active"].includes(currentStatus)
  ) {
    return "";
  }

  const updateResponse = await fetch(endpoint, {
    method: "PUT",
    headers,
    body: JSON.stringify({ origin: configuration.cnameTarget }),
    cache: "no-store",
  });
  let updatePayload: JsonRecord = {};
  try {
    updatePayload = await updateResponse.json() as JsonRecord;
  } catch {
    // The generic message below avoids exposing an upstream HTML response.
  }
  if (!updateResponse.ok || updatePayload.success === false) {
    return cloudflareErrorMessage(
      updatePayload,
      "A Cloudflare não conseguiu preparar a origem segura dos domínios personalizados.",
    );
  }
  return "";
}

function cloudflareValidationRecords(result: JsonRecord) {
  const records: JsonRecord[] = [];
  const ownership = result.ownership_verification;
  if (ownership && typeof ownership === "object" && !Array.isArray(ownership)) {
    const source = ownership as JsonRecord;
    records.push({
      name: stringValue(source.name, 255),
      recordType: stringValue(source.type, 16, "TXT"),
      value: stringValue(source.value, 500),
    });
  }
  const ssl = result.ssl;
  if (ssl && typeof ssl === "object" && !Array.isArray(ssl)) {
    const validationRecords = (ssl as JsonRecord).validation_records;
    if (Array.isArray(validationRecords)) {
      for (const entry of validationRecords.slice(0, 10)) {
        if (!entry || typeof entry !== "object" || Array.isArray(entry)) continue;
        const source = entry as JsonRecord;
        records.push({
          name: stringValue(source.txt_name || source.http_url, 500),
          recordType: source.txt_name ? "TXT" : "HTTP",
          value: stringValue(source.txt_value || source.http_body, 1000),
        });
      }
    }
  }
  return JSON.stringify(records);
}

function cloudflareProvisioningState(
  result: JsonRecord,
  cnameTarget: string,
): DomainProvisioningState {
  const providerStatus = stringValue(result.status, 80, "pending").toLowerCase();
  const ssl = result.ssl && typeof result.ssl === "object" && !Array.isArray(result.ssl)
    ? result.ssl as JsonRecord
    : {};
  const sslStatus = stringValue(ssl.status, 80, "pending").toLowerCase();
  const active = providerStatus === "active" && sslStatus === "active";
  const failed = [providerStatus, sslStatus].some((status) =>
    ["failed", "validation_timed_out", "expired", "deleted"].includes(status),
  );
  return {
    providerId: stringValue(result.id, 128),
    status: active ? "active" : failed ? "failed" : "pending",
    providerStatus,
    sslStatus,
    cnameTarget,
    validationRecordsJson: cloudflareValidationRecords(result),
    lastError: "",
    verifiedAt: active ? Date.now() : null,
  };
}

async function provisionCloudflareCustomDomain(
  hostname: string,
  storeId: string,
  existing: StoreDomainRow | null,
): Promise<DomainProvisioningState> {
  const configuration = cloudflareSaasConfiguration();
  const fallback: DomainProvisioningState = {
    providerId: existing?.provider_id || "",
    status: existing?.status === "active" || existing?.status === "failed"
      ? existing.status
      : "pending",
    providerStatus: existing?.provider_status || "",
    sslStatus: existing?.ssl_status || "",
    cnameTarget: existing?.cname_target || configuration.cnameTarget,
    validationRecordsJson: existing?.validation_records_json || "[]",
    lastError: existing?.last_error || "",
    verifiedAt: existing?.verified_at || null,
  };
  if (!configuration.zoneId || !configuration.apiToken) {
    return {
      ...fallback,
      status: "pending",
      lastError: "A conexão automática do domínio aguarda a configuração segura da Cloudflare.",
    };
  }
  const fallbackOriginError = await ensureCloudflareFallbackOrigin(configuration);
  if (fallbackOriginError) {
    return {
      ...fallback,
      status: "failed",
      lastError: fallbackOriginError,
    };
  }

  const endpoint = existing?.provider_id
    ? `https://api.cloudflare.com/client/v4/zones/${configuration.zoneId}/custom_hostnames/${existing.provider_id}`
    : `https://api.cloudflare.com/client/v4/zones/${configuration.zoneId}/custom_hostnames`;
  const response = await fetch(endpoint, {
    method: existing?.provider_id ? "GET" : "POST",
    headers: {
      Authorization: `Bearer ${configuration.apiToken}`,
      "Content-Type": "application/json",
    },
    ...(existing?.provider_id
      ? {}
      : {
          body: JSON.stringify({
            hostname,
            custom_metadata: { store_id: storeId },
            ssl: {
              method: "http",
              type: "dv",
              settings: { min_tls_version: "1.2" },
            },
          }),
        }),
    cache: "no-store",
  });
  let payload: JsonRecord = {};
  try {
    payload = await response.json() as JsonRecord;
  } catch {
    // A generic message below is safer than exposing an upstream HTML response.
  }
  const result = payload.result && typeof payload.result === "object" && !Array.isArray(payload.result)
    ? payload.result as JsonRecord
    : null;
  if (!response.ok || payload.success === false || !result) {
    return {
      ...fallback,
      status: "failed",
      lastError: cloudflareErrorMessage(
        payload,
        "A Cloudflare não conseguiu conectar este domínio agora.",
      ),
    };
  }
  return cloudflareProvisioningState(result, configuration.cnameTarget);
}

async function deleteCloudflareCustomDomain(providerId: string) {
  const configuration = cloudflareSaasConfiguration();
  if (!providerId || !configuration.zoneId || !configuration.apiToken) return;
  await fetch(
    `https://api.cloudflare.com/client/v4/zones/${configuration.zoneId}/custom_hostnames/${providerId}`,
    {
      method: "DELETE",
      headers: { Authorization: `Bearer ${configuration.apiToken}` },
      cache: "no-store",
    },
  );
}

function catalogIdentifier(value: unknown, fallback = "") {
  const normalized = stringValue(value, 140)
    .toLowerCase()
    .replace(/[^a-z0-9-]+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120);
  return normalized || fallback;
}

function sanitizeCatalogHeader(value: unknown): JsonRecord {
  const source = value && typeof value === "object" && !Array.isArray(value)
    ? value as JsonRecord
    : {};
  return {
    businessName: stringValue(source.businessName, 120),
    subtitle: stringValue(source.subtitle, 120),
    buttonText: stringValue(source.buttonText, 80, "Agendar agora"),
    showLogo: source.showLogo !== false,
    showNavigation: source.showNavigation !== false,
    showButton: source.showButton !== false,
    sticky: source.sticky !== false,
    background: ["solid", "transparent", "soft"].includes(String(source.background))
      ? String(source.background)
      : "solid",
  };
}

function sanitizeCatalogFooter(value: unknown): JsonRecord {
  const source = value && typeof value === "object" && !Array.isArray(value)
    ? value as JsonRecord
    : {};
  return {
    businessName: stringValue(source.businessName, 120),
    description: stringValue(source.description, 360),
    address: stringValue(source.address, 240),
    phone: stringValue(source.phone, 40),
    hours: stringValue(source.hours, 160),
    instagram: stringValue(source.instagram, 80).replace(/^@+/, ""),
    whatsApp: stringValue(source.whatsApp, 40),
    showContact: source.showContact !== false,
    showHours: source.showHours !== false,
    showSocial: source.showSocial !== false,
  };
}

function sanitizeCatalogDesign(value: unknown): JsonRecord {
  const source = value && typeof value === "object" && !Array.isArray(value)
    ? value as JsonRecord
    : {};
  return {
    colorScheme: ["warm", "light", "dark"].includes(String(source.colorScheme))
      ? String(source.colorScheme)
      : "warm",
    buttonStyle: ["rounded", "pill", "square"].includes(String(source.buttonStyle))
      ? String(source.buttonStyle)
      : "rounded",
    cornerStyle: ["rounded", "soft", "sharp"].includes(String(source.cornerStyle))
      ? String(source.cornerStyle)
      : "rounded",
    contentWidth: ["compact", "standard", "wide"].includes(String(source.contentWidth))
      ? String(source.contentWidth)
      : "standard",
  };
}

function sanitizeCatalogSectionItem(value: unknown, fallbackId: string): JsonRecord | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const source = value as JsonRecord;
  const id = catalogIdentifier(source.id, fallbackId);
  if (!id) return null;
  return {
    id,
    title: stringValue(source.title, 160),
    text: stringValue(source.text, 600),
    detail: stringValue(source.detail, 160),
    mediaId: catalogIdentifier(source.mediaId),
  };
}

function sanitizeCatalogSections(value: unknown): JsonRecord[] {
  if (!Array.isArray(value)) return [];
  const allowedTypes = new Set([
    "services",
    "benefits",
    "team",
    "gallery",
    "before-after",
    "process",
    "testimonials",
    "faq",
    "brands",
    "location",
    "callout",
  ]);
  const sections: JsonRecord[] = [];
  const seen = new Set<string>();
  for (const [sectionIndex, rawSection] of value.slice(0, 20).entries()) {
    if (!rawSection || typeof rawSection !== "object" || Array.isArray(rawSection)) continue;
    const source = rawSection as JsonRecord;
    const id = catalogIdentifier(source.id, `section-${sectionIndex + 1}`);
    if (!id || seen.has(id)) continue;
    seen.add(id);
    const type = allowedTypes.has(String(source.type)) ? String(source.type) : "benefits";
    const items: JsonRecord[] = [];
    if (Array.isArray(source.items)) {
      for (const [itemIndex, rawItem] of source.items.slice(0, 12).entries()) {
        const item = sanitizeCatalogSectionItem(rawItem, `${id}-item-${itemIndex + 1}`);
        if (item) items.push(item);
      }
    }
    sections.push({
      id,
      type,
      title: stringValue(source.title, 160),
      subtitle: stringValue(source.subtitle, 120),
      body: stringValue(source.body, 800),
      buttonText: stringValue(source.buttonText, 80),
      buttonTarget: ["booking", "contact", "services"].includes(String(source.buttonTarget))
        ? String(source.buttonTarget)
        : "booking",
      layout: ["cards", "columns", "split", "steps", "gallery", "comparison"].includes(
        String(source.layout),
      )
        ? String(source.layout)
        : "cards",
      background: ["light", "soft", "accent", "dark"].includes(String(source.background))
        ? String(source.background)
        : "light",
      alignment: ["left", "center", "right"].includes(String(source.alignment))
        ? String(source.alignment)
        : "left",
      enabled: source.enabled !== false,
      automaticContent: source.automaticContent === true,
      items,
    });
  }
  return sections;
}

function sanitizeCatalogMediaUploads(value: unknown): JsonRecord[] {
  if (!Array.isArray(value)) return [];
  const uploads: JsonRecord[] = [];
  const seen = new Set<string>();
  for (const rawUpload of value.slice(0, CATALOG_MEDIA_MAXIMUM_ITEMS)) {
    if (!rawUpload || typeof rawUpload !== "object" || Array.isArray(rawUpload)) continue;
    const source = rawUpload as JsonRecord;
    const id = catalogIdentifier(source.id);
    const dataUrl = stringValue(source.dataUrl, CATALOG_MEDIA_MAXIMUM_DATA_URL_CHARACTERS);
    if (!id || !dataUrl || seen.has(id)) continue;
    seen.add(id);
    uploads.push({ id, dataUrl });
  }
  return uploads;
}

function sanitizeCatalogPromotion(value: unknown): JsonRecord | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const source = value as JsonRecord;
  const items: JsonRecord[] = [];
  const seen = new Set<string>();
  if (Array.isArray(source.items)) {
    for (const rawItem of source.items.slice(0, 50)) {
      if (!rawItem || typeof rawItem !== "object" || Array.isArray(rawItem)) continue;
      const item = rawItem as JsonRecord;
      const serviceId = stringValue(item.serviceId, 128);
      if (!serviceId || seen.has(serviceId)) continue;
      seen.add(serviceId);
      const originalPrice = Math.round(Math.max(0, Number(item.originalPrice) || 0) * 100) / 100;
      const promotionalPrice = Math.round(Math.max(0, Number(item.promotionalPrice) || 0) * 100) / 100;
      if (originalPrice <= 0 || promotionalPrice >= originalPrice) continue;
      items.push({
        serviceId,
        serviceName: stringValue(item.serviceName, 160),
        originalPrice,
        promotionalPrice,
      });
    }
  }
  return {
    name: stringValue(source.name, 120, "Oferta especial"),
    startDate: stringValue(source.startDate, 40),
    endDate: stringValue(source.endDate, 40),
    limitPerCustomer: Math.min(99, Math.max(1, Math.round(Number(source.limitPerCustomer) || 1))),
    highlightInCatalog: source.highlightInCatalog !== false,
    isPublished: source.isPublished === true,
    publishedAt: stringValue(source.publishedAt, 80),
    items,
  };
}

function sanitizeCatalog(value: unknown) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};
  const source = value as JsonRecord;
  const output: JsonRecord = {
    title: stringValue(source.title, 120, "Sua beleza, do seu jeito"),
    supportText: stringValue(source.supportText, 360),
    buttonText: stringValue(source.buttonText, 80, "Agendar agora"),
    accentColor: /^#[0-9a-f]{6}$/i.test(String(source.accentColor || ""))
      ? String(source.accentColor).toUpperCase()
      : "#FF6B4A",
    alignment: ["left", "center", "right"].includes(String(source.alignment))
      ? String(source.alignment)
      : "left",
    spacing: ["compact", "comfortable", "wide"].includes(String(source.spacing))
      ? String(source.spacing)
      : "compact",
    titleFont: ["Georgia", "Segoe UI", "Playfair Display"].includes(String(source.titleFont))
      ? String(source.titleFont)
      : "Georgia",
    imageContrast: Math.min(100, Math.max(0, Number(source.imageContrast) || 64)),
    showButton: source.showButton !== false,
    header: sanitizeCatalogHeader(source.header),
    footer: sanitizeCatalogFooter(source.footer),
    design: sanitizeCatalogDesign(source.design),
    sections: sanitizeCatalogSections(source.sections),
    seo: {
      title: stringValue(
        source.seo && typeof source.seo === "object" && !Array.isArray(source.seo)
          ? (source.seo as JsonRecord).title
          : "",
        160,
      ),
      description: stringValue(
        source.seo && typeof source.seo === "object" && !Array.isArray(source.seo)
          ? (source.seo as JsonRecord).description
          : "",
        320,
      ),
    },
    publishedAt: stringValue(source.publishedAt, 80),
  };
  if (Object.prototype.hasOwnProperty.call(source, "promotion")) {
    output.promotion = sanitizeCatalogPromotion(source.promotion);
  }
  if (Object.prototype.hasOwnProperty.call(source, "heroImageDataUrl")) {
    output.heroImageDataUrl = stringValue(
      source.heroImageDataUrl,
      CATALOG_HERO_MAXIMUM_DATA_URL_CHARACTERS,
    );
  }
  if (Object.prototype.hasOwnProperty.call(source, "mediaUploads")) {
    output.mediaUploads = sanitizeCatalogMediaUploads(source.mediaUploads);
  }
  return output;
}

function publishedCatalogInput(value: unknown) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const publishedAt = Date.parse(stringValue((value as JsonRecord).publishedAt, 80));
  if (!Number.isFinite(publishedAt) || publishedAt <= 0) return null;
  return {
    catalog: sanitizeCatalog(value),
    publishedAt,
  };
}

function parseStoredCatalog(value: string): JsonRecord {
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? parsed as JsonRecord
      : {};
  } catch {
    return {};
  }
}

function decodeCatalogHeroDataUrl(value: unknown) {
  const dataUrl = String(value || "").trim();
  if (!dataUrl) return null;
  if (dataUrl.length > CATALOG_HERO_MAXIMUM_DATA_URL_CHARACTERS) {
    throw new AgendaBookingError(400, "invalid_catalog_image", "A imagem da capa é muito grande.");
  }
  const match = /^data:image\/(jpeg|png);base64,([A-Za-z0-9+/]+={0,2})$/i.exec(dataUrl);
  if (!match || match[2].length % 4 !== 0) {
    throw new AgendaBookingError(400, "invalid_catalog_image", "A imagem da capa é inválida.");
  }
  let binary: string;
  try {
    binary = atob(match[2]);
  } catch {
    throw new AgendaBookingError(400, "invalid_catalog_image", "A imagem da capa é inválida.");
  }
  if (binary.length < 128 || binary.length > CATALOG_HERO_MAXIMUM_BYTES) {
    throw new AgendaBookingError(400, "invalid_catalog_image", "A imagem da capa é inválida.");
  }
  const isJpeg = binary.charCodeAt(0) === 0xff && binary.charCodeAt(1) === 0xd8;
  const isPng = [137, 80, 78, 71, 13, 10, 26, 10]
    .every((expected, index) => binary.charCodeAt(index) === expected);
  if ((match[1].toLowerCase() === "jpeg" && !isJpeg) ||
      (match[1].toLowerCase() === "png" && !isPng)) {
    throw new AgendaBookingError(400, "invalid_catalog_image", "A imagem da capa é inválida.");
  }
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return {
    bytes,
    contentType: match[1].toLowerCase() === "png" ? "image/png" : "image/jpeg",
  };
}

function decodeCatalogMediaDataUrl(value: unknown) {
  const dataUrl = String(value || "").trim();
  if (
    !dataUrl ||
    dataUrl.length > CATALOG_MEDIA_MAXIMUM_DATA_URL_CHARACTERS
  ) {
    throw new AgendaBookingError(
      400,
      "invalid_catalog_media",
      "Uma das imagens da seção é muito grande.",
    );
  }
  const match = /^data:image\/(jpeg|png);base64,([A-Za-z0-9+/]+={0,2})$/i.exec(dataUrl);
  if (!match || match[2].length % 4 !== 0) {
    throw new AgendaBookingError(
      400,
      "invalid_catalog_media",
      "Uma das imagens da seção é inválida.",
    );
  }
  let binary: string;
  try {
    binary = atob(match[2]);
  } catch {
    throw new AgendaBookingError(
      400,
      "invalid_catalog_media",
      "Uma das imagens da seção é inválida.",
    );
  }
  if (binary.length < 128 || binary.length > CATALOG_MEDIA_MAXIMUM_BYTES) {
    throw new AgendaBookingError(
      400,
      "invalid_catalog_media",
      "Uma das imagens da seção é inválida.",
    );
  }
  const isJpeg = binary.charCodeAt(0) === 0xff && binary.charCodeAt(1) === 0xd8;
  const isPng = [137, 80, 78, 71, 13, 10, 26, 10]
    .every((expected, index) => binary.charCodeAt(index) === expected);
  if (
    (match[1].toLowerCase() === "jpeg" && !isJpeg) ||
    (match[1].toLowerCase() === "png" && !isPng)
  ) {
    throw new AgendaBookingError(
      400,
      "invalid_catalog_media",
      "Uma das imagens da seção é inválida.",
    );
  }
  return {
    bytes: Uint8Array.from(binary, (character) => character.charCodeAt(0)),
    contentType: match[1].toLowerCase() === "png" ? "image/png" : "image/jpeg",
  };
}

function publicCatalog(catalogJson: string, slug: string, catalogVersion: number) {
  const catalog = parseStoredCatalog(catalogJson);
  const versionQuery = `?v=${Math.max(1, Math.trunc(catalogVersion || 1))}`;
  const { heroObjectKey: _heroObjectKey, ...safeCatalog } = catalog;
  const sections = Array.isArray(catalog.sections)
    ? catalog.sections.map((rawSection) => {
        if (!rawSection || typeof rawSection !== "object" || Array.isArray(rawSection)) {
          return rawSection;
        }
        const section = rawSection as JsonRecord;
        const items = Array.isArray(section.items)
          ? section.items.map((rawItem) => {
              if (!rawItem || typeof rawItem !== "object" || Array.isArray(rawItem)) {
                return rawItem;
              }
              const item = rawItem as JsonRecord;
              const mediaId = catalogIdentifier(item.mediaId);
              return {
                ...item,
                imageUrl: mediaId
                  ? `/api/agendar/${encodeURIComponent(slug)}/media/${encodeURIComponent(mediaId)}${versionQuery}`
                  : "",
              };
            })
          : [];
        return { ...section, items };
      })
    : [];
  return {
    ...safeCatalog,
    sections,
    heroImageUrl: typeof catalog.heroObjectKey === "string" && catalog.heroObjectKey
      ? `/api/agendar/${encodeURIComponent(slug)}/hero${versionQuery}`
      : "",
  };
}

function sanitizeServices(value: unknown): BookingService[] {
  if (!Array.isArray(value)) {
    throw new AgendaBookingError(400, "invalid_services", "A disponibilidade enviada é inválida.");
  }

  const services: BookingService[] = [];
  const seenServices = new Set<string>();
  for (const rawService of value.slice(0, 50)) {
    if (!rawService || typeof rawService !== "object" || Array.isArray(rawService)) continue;
    const source = rawService as JsonRecord;
    const id = stringValue(source.id, 128);
    const name = stringValue(source.name, 160);
    if (!id || !name || seenServices.has(id)) continue;
    seenServices.add(id);

    const durationNumber = Math.round(Number(source.durationMinutes));
    const durationMinutes = Number.isFinite(durationNumber)
      ? Math.min(480, Math.max(15, durationNumber))
      : 30;
    const rawPrice = Number(source.price);
    const price = Number.isFinite(rawPrice)
      ? Math.round(Math.min(1_000_000, Math.max(0, rawPrice)) * 100) / 100
      : 0;
    const rawOriginalPrice = Number(source.originalPrice);
    const originalPrice = Number.isFinite(rawOriginalPrice)
      ? Math.round(Math.min(1_000_000, Math.max(price, rawOriginalPrice)) * 100) / 100
      : price;
    const promotionName = stringValue(source.promotionName, 120);
    const rawDiscountPercent = Math.round(Number(source.discountPercent));
    const discountPercent = Number.isFinite(rawDiscountPercent)
      ? Math.min(100, Math.max(0, rawDiscountPercent))
      : 0;
    const days: BookingDay[] = [];
    const seenDays = new Set<string>();

    if (Array.isArray(source.days)) {
      for (const rawDay of source.days.slice(0, 31)) {
        if (!rawDay || typeof rawDay !== "object" || Array.isArray(rawDay)) continue;
        const daySource = rawDay as JsonRecord;
        const date = stringValue(daySource.date, 10);
        if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || seenDays.has(date)) continue;
        seenDays.add(date);
        const slots: BookingSlot[] = [];
        const seenSlots = new Set<string>();

        if (Array.isArray(daySource.availableSlots)) {
          for (const rawSlot of daySource.availableSlots.slice(0, 200)) {
            if (!rawSlot || typeof rawSlot !== "object" || Array.isArray(rawSlot)) continue;
            const slotSource = rawSlot as JsonRecord;
            const slotId = stringValue(slotSource.id, 160);
            const professionalId = stringValue(slotSource.professionalId, 128);
            const startMs = Date.parse(stringValue(slotSource.start, 80));
            if (
              !slotId ||
              !professionalId ||
              seenSlots.has(slotId) ||
              !Number.isFinite(startMs)
            ) {
              continue;
            }
            seenSlots.add(slotId);
            slots.push({
              id: slotId,
              time: /^\d{2}:\d{2}$/.test(stringValue(slotSource.time, 5))
                ? stringValue(slotSource.time, 5)
                : new Date(startMs).toISOString().slice(11, 16),
              start: new Date(startMs).toISOString(),
              professionalId,
              professionalName: stringValue(slotSource.professionalName, 160, "Profissional"),
              resourceName: stringValue(slotSource.resourceName, 160),
            });
          }
        }

        if (slots.length) {
          days.push({
            date,
            label: stringValue(daySource.label, 80, date),
            availableSlots: slots,
          });
        }
      }
    }

    if (days.length) {
      services.push({
        id,
        name,
        durationMinutes,
        price,
        originalPrice,
        promotionName,
        discountPercent,
        days,
      });
    }
  }
  return services;
}

function parseStoredServices(value: string): BookingService[] {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? (parsed as BookingService[]) : [];
  } catch {
    return [];
  }
}

function parseStoredTheme(value: string) {
  try {
    return JSON.parse(value);
  } catch {
    return {};
  }
}

async function allocateSlug(
  database: D1Database,
  desired: string,
  storeId: string | null,
  instance: string,
) {
  const digest = (await sha256(instance)).slice(0, 6).toLowerCase();
  for (let attempt = 0; attempt < 100; attempt += 1) {
    const suffix = attempt === 0 ? "" : `-${digest}${attempt === 1 ? "" : `-${attempt}`}`;
    const base = desired.slice(0, Math.max(3, 48 - suffix.length)).replace(/-+$/g, "");
    const candidate = `${base}${suffix}`;
    const owner = await database
      .prepare("SELECT id FROM agenda_stores WHERE slug = ?1 LIMIT 1")
      .bind(candidate)
      .first<{ id: string }>();
    if (!owner || owner.id === storeId) return candidate;
  }
  throw new AgendaBookingError(409, "slug_unavailable", "Não foi possível reservar o endereço desta loja.");
}

function assertStoreIdentity(store: StoreRow, identity: InternalIdentity) {
  if (
    store.license_hash !== identity.licenseHash ||
    store.machine_hash !== identity.machineHash ||
    store.machine_code !== identity.machineCode
  ) {
    throw new AgendaBookingError(403, "store_identity_mismatch", "Esta agenda pertence a outra instalação.");
  }
}

function assertStoreOwner(
  store: StoreRow,
  ownerId: string,
  identity: InternalIdentity,
) {
  if (store.owner_user_id) {
    if (store.owner_user_id !== ownerId) {
      throw new AgendaBookingError(
        403,
        "store_owner_mismatch",
        "Este catálogo pertence a outro perfil.",
      );
    }
    return;
  }
  assertStoreIdentity(store, identity);
}

function bookingInternal(row: BookingRow, instance: string) {
  const now = Date.now();
  const reminderDue =
    row.status === "confirmed" &&
    !row.reminder_sent_at &&
    row.starts_at_ms > now &&
    row.starts_at_ms <= now + FOUR_HOURS_MS;
  return {
    id: row.id,
    instance,
    source: row.source,
    leadId: row.id,
    phone: row.customer_phone,
    customerName: row.customer_name,
    serviceId: row.service_id,
    serviceName: row.service_name,
    slotId: row.slot_id,
    start: row.starts_at,
    durationMinutes: row.duration_minutes,
    professionalId: row.professional_id,
    professionalName: row.professional_name,
    resourceName: row.resource_name,
    price: row.price_cents / 100,
    notes: row.notes,
    status: row.status,
    appointmentId: row.appointment_id,
    message: row.message,
    confirmationSentAt: row.confirmation_sent_at
      ? new Date(row.confirmation_sent_at).toISOString()
      : null,
    reminderSentAt: row.reminder_sent_at
      ? new Date(row.reminder_sent_at).toISOString()
      : null,
    needsConfirmation: row.status === "confirmed" && !row.confirmation_sent_at,
    reminderDue,
    reminderAt: new Date(row.starts_at_ms - FOUR_HOURS_MS).toISOString(),
  };
}

async function pendingBookings(database: D1Database, store: StoreRow) {
  const now = Date.now();
  const dueBefore = now + FOUR_HOURS_MS;
  const result = await database
    .prepare(
      `SELECT * FROM agenda_bookings
       WHERE store_id = ?1
         AND (
           status IN ('requested', 'pending')
           OR (
             status = 'confirmed'
             AND (
               confirmation_sent_at IS NULL
               OR (reminder_sent_at IS NULL AND starts_at_ms > ?2 AND starts_at_ms <= ?3)
             )
           )
         )
       ORDER BY created_at ASC
       LIMIT 100`,
    )
    .bind(store.id, now, dueBefore)
    .all<BookingRow>();
  return (result.results || []).map((row) => bookingInternal(row, store.instance));
}

export async function syncAgenda(request: Request) {
  const [identity, owner] = await Promise.all([
    authenticateInternal(request),
    authenticateCatalogOwner(request),
  ]);
  const body = await readJsonBody(request, 3_000_000);
  const instance = normalizeInstance(body.instance);
  const storeName = stringValue(body.storeName, 160);
  if (storeName.length < 2) {
    throw new AgendaBookingError(400, "invalid_store_name", "Informe o nome da loja.");
  }

  const desiredSlug = normalizeSlug(body.desiredSlug || storeName);
  const segment = stringValue(body.segment, 120, "Serviços");
  const theme = sanitizeTheme(body.theme);
  const incomingPublication = publishedCatalogInput(body.catalog);
  const submittedCustomDomain = incomingPublication
    ? normalizeCustomDomain(body.customDomain)
    : null;
  const services = sanitizeServices(body.bookingServices);
  const parsedGeneratedAt = Date.parse(stringValue(body.generatedAt, 80));
  const generatedAt = Number.isFinite(parsedGeneratedAt)
    ? new Date(parsedGeneratedAt).toISOString()
    : new Date().toISOString();
  const now = Date.now();
  const database = getAgendaD1();
  let existing = await database
    .prepare("SELECT * FROM agenda_stores WHERE owner_user_id = ?1 LIMIT 1")
    .bind(owner.id)
    .first<StoreRow>();
  if (!existing) {
    const legacy = await database
      .prepare(
        "SELECT * FROM agenda_stores WHERE instance = ?1 AND owner_user_id = '' LIMIT 1",
      )
      .bind(instance)
      .first<StoreRow>();
    if (legacy) {
      assertStoreIdentity(legacy, identity);
      existing = legacy;
    }
  }
  if (existing?.owner_user_id && existing.owner_user_id !== owner.id) {
    throw new AgendaBookingError(
      403,
      "store_owner_mismatch",
      "Este catálogo pertence a outro perfil.",
    );
  }
  if (!Object.prototype.hasOwnProperty.call(theme, "logoUrl") && existing) {
    const previousTheme = parseStoredTheme(existing.theme_json) as JsonRecord;
    if (typeof previousTheme.logoUrl === "string" && previousTheme.logoUrl) {
      try {
        theme.logoUrl = sanitizePublicLogoDataUrl(previousTheme.logoUrl);
      } catch {
        // Invalid legacy image data is discarded instead of blocking the current sync.
      }
    }
  }
  const previousCatalog = existing ? parseStoredCatalog(existing.catalog_json) : {};
  const currentCatalogVersion = Number(existing?.catalog_version || 0);
  const currentCatalogPublishedAt = Number(existing?.catalog_published_at || 0);
  const shouldApplyPublication = Boolean(
    incomingPublication &&
      (currentCatalogVersion === 0 || incomingPublication.publishedAt > currentCatalogPublishedAt),
  );
  const effectiveDesiredSlug = existing && currentCatalogVersion > 0 && !shouldApplyPublication
    ? existing.desired_slug
    : desiredSlug;
  const catalog = shouldApplyPublication && incomingPublication
    ? incomingPublication.catalog
    : { ...previousCatalog };
  const referencedMediaIds = new Set<string>();
  if (Array.isArray(catalog.sections)) {
    for (const rawSection of catalog.sections) {
      if (!rawSection || typeof rawSection !== "object" || Array.isArray(rawSection)) continue;
      const section = rawSection as JsonRecord;
      if (!Array.isArray(section.items)) continue;
      for (const rawItem of section.items) {
        if (!rawItem || typeof rawItem !== "object" || Array.isArray(rawItem)) continue;
        const mediaId = catalogIdentifier((rawItem as JsonRecord).mediaId);
        if (mediaId) referencedMediaIds.add(mediaId);
      }
    }
  }
  const mediaUploads = shouldApplyPublication && Array.isArray(catalog.mediaUploads)
    ? catalog.mediaUploads
        .filter((value) => value && typeof value === "object" && !Array.isArray(value))
        .map((value) => value as JsonRecord)
        .filter((value) => referencedMediaIds.has(catalogIdentifier(value.id)))
        .map((value) => ({
          id: catalogIdentifier(value.id),
          ...decodeCatalogMediaDataUrl(value.dataUrl),
        }))
    : [];
  const heroUpload = shouldApplyPublication &&
      Object.prototype.hasOwnProperty.call(catalog, "heroImageDataUrl")
    ? decodeCatalogHeroDataUrl(catalog.heroImageDataUrl)
    : null;
  delete catalog.heroImageDataUrl;
  delete catalog.mediaUploads;
  if (shouldApplyPublication && !heroUpload && typeof previousCatalog.heroObjectKey === "string") {
    catalog.heroObjectKey = previousCatalog.heroObjectKey;
  }

  const storeId = existing?.id || crypto.randomUUID();
  const slug =
    existing && existing.desired_slug === effectiveDesiredSlug
      ? existing.slug
      : await allocateSlug(database, effectiveDesiredSlug, existing?.id || null, instance);
  if (shouldApplyPublication && heroUpload) {
    const extension = heroUpload.contentType === "image/png" ? "png" : "jpg";
    const catalogBucket = getOptionalAgendaCatalogR2();
    const heroObjectKey = catalogBucket
      ? `catalog/${storeId}/hero.${extension}`
      : `d1:${storeId}`;
    if (catalogBucket) {
      await catalogBucket.put(heroObjectKey, heroUpload.bytes, {
        httpMetadata: {
          contentType: heroUpload.contentType,
          cacheControl: "public, max-age=3600",
        },
        customMetadata: {
          storeId,
          slug,
          updatedAt: new Date(now).toISOString(),
        },
      });
      await database
        .prepare("DELETE FROM agenda_catalog_assets WHERE store_id = ?1")
        .bind(storeId)
        .run();
    } else {
      await database
        .prepare(
          `INSERT INTO agenda_catalog_assets (store_id, content_type, body, updated_at)
           VALUES (?1, ?2, ?3, ?4)
           ON CONFLICT(store_id) DO UPDATE SET
             content_type = excluded.content_type,
             body = excluded.body,
             updated_at = excluded.updated_at`,
        )
        .bind(
          storeId,
          heroUpload.contentType,
          heroUpload.bytes.buffer,
          now,
        )
        .run();
    }
    const previousObjectKey = typeof previousCatalog.heroObjectKey === "string"
      ? previousCatalog.heroObjectKey
      : "";
    if (
      catalogBucket &&
      previousObjectKey &&
      !previousObjectKey.startsWith("d1:") &&
      previousObjectKey !== heroObjectKey
    ) {
      await catalogBucket.delete(previousObjectKey);
    }
    catalog.heroObjectKey = heroObjectKey;
  }
  const themeJson = JSON.stringify(theme);
  const catalogJson = JSON.stringify(catalog);
  const catalogVersion = shouldApplyPublication
    ? currentCatalogVersion + 1
    : currentCatalogVersion;
  const catalogPublishedAt = shouldApplyPublication && incomingPublication
    ? incomingPublication.publishedAt
    : currentCatalogPublishedAt;
  const servicesJson = JSON.stringify(services);
  const existingDomain = await database
    .prepare("SELECT * FROM agenda_store_domains WHERE store_id = ?1 LIMIT 1")
    .bind(storeId)
    .first<StoreDomainRow>();
  const customDomain = shouldApplyPublication
    ? submittedCustomDomain || ""
    : existingDomain?.hostname || "";
  const shouldManageDomain = shouldApplyPublication || Boolean(
    existingDomain && submittedCustomDomain === existingDomain.hostname,
  );
  if (shouldManageDomain && customDomain) {
    const conflictingDomain = await database
      .prepare("SELECT store_id FROM agenda_store_domains WHERE hostname = ?1 LIMIT 1")
      .bind(customDomain)
      .first<{ store_id: string }>();
    if (conflictingDomain && conflictingDomain.store_id !== storeId) {
      throw new AgendaBookingError(
        409,
        "custom_domain_in_use",
        "Este domínio personalizado já está conectado a outro catálogo.",
      );
    }
  }
  if (shouldManageDomain && existingDomain && existingDomain.hostname !== customDomain) {
    await deleteCloudflareCustomDomain(existingDomain.provider_id);
  }
  let domainProvisioning: DomainProvisioningState | null = null;
  let domainProvisioningUpdatedAt = now;
  if (shouldManageDomain && customDomain) {
    const sameDomain = existingDomain?.hostname === customDomain;
    const shouldRefreshProvisioning = !sameDomain ||
      existingDomain.status !== "active" && now - Number(existingDomain.updated_at || 0) >= 60_000;
    if (shouldRefreshProvisioning) {
      domainProvisioning = await provisionCloudflareCustomDomain(
        customDomain,
        storeId,
        sameDomain ? existingDomain : null,
      );
    } else if (sameDomain) {
      domainProvisioning = {
        providerId: existingDomain.provider_id,
        status: existingDomain.status === "active" || existingDomain.status === "failed"
          ? existingDomain.status
          : "pending",
        providerStatus: existingDomain.provider_status,
        sslStatus: existingDomain.ssl_status,
        cnameTarget: existingDomain.cname_target,
        validationRecordsJson: existingDomain.validation_records_json,
        lastError: existingDomain.last_error,
        verifiedAt: existingDomain.verified_at,
      };
      domainProvisioningUpdatedAt = existingDomain.updated_at;
    }
  }
  if (!domainProvisioning && existingDomain && existingDomain.hostname === customDomain) {
    domainProvisioning = {
      providerId: existingDomain.provider_id,
      status: existingDomain.status === "active" || existingDomain.status === "failed"
        ? existingDomain.status
        : "pending",
      providerStatus: existingDomain.provider_status,
      sslStatus: existingDomain.ssl_status,
      cnameTarget: existingDomain.cname_target,
      validationRecordsJson: existingDomain.validation_records_json,
      lastError: existingDomain.last_error,
      verifiedAt: existingDomain.verified_at,
    };
    domainProvisioningUpdatedAt = existingDomain.updated_at;
  }

  const storeStatement = existing
    ? database
        .prepare(
          `UPDATE agenda_stores
           SET owner_user_id = ?1, instance = ?2, license_hash = ?3,
               machine_hash = ?4, machine_code = ?5, desired_slug = ?6,
               slug = ?7, name = ?8, segment = ?9, theme_json = ?10,
               catalog_json = ?11, catalog_version = ?12,
               catalog_published_at = ?13, generated_at = ?14,
               last_synced_at = ?15, updated_at = ?15
           WHERE id = ?16`,
        )
        .bind(
          owner.id,
          instance,
          identity.licenseHash,
          identity.machineHash,
          identity.machineCode,
          effectiveDesiredSlug,
          slug,
          storeName,
          segment,
          themeJson,
          catalogJson,
          catalogVersion,
          catalogPublishedAt,
          generatedAt,
          now,
          storeId,
        )
    : database
        .prepare(
          `INSERT INTO agenda_stores (
             id, owner_user_id, instance, license_hash, machine_hash, machine_code,
             desired_slug, slug, name, segment, theme_json, catalog_json,
             catalog_version, catalog_published_at, generated_at,
             last_synced_at, created_at, updated_at
           ) VALUES (
             ?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12,
             ?13, ?14, ?15, ?16, ?16, ?16
           )`,
        )
        .bind(
          storeId,
          owner.id,
          instance,
          identity.licenseHash,
          identity.machineHash,
          identity.machineCode,
          effectiveDesiredSlug,
          slug,
          storeName,
          segment,
          themeJson,
          catalogJson,
          catalogVersion,
          catalogPublishedAt,
          generatedAt,
          now,
        );

  const statements = [
    storeStatement,
    database
      .prepare(
        `INSERT INTO agenda_snapshots (store_id, services_json, generated_at, received_at)
         VALUES (?1, ?2, ?3, ?4)
         ON CONFLICT(store_id) DO UPDATE SET
           services_json = excluded.services_json,
           generated_at = excluded.generated_at,
           received_at = excluded.received_at`,
      )
      .bind(storeId, servicesJson, generatedAt, now),
  ];
  if (shouldApplyPublication) {
    statements.push(
      database.prepare("DELETE FROM agenda_catalog_media WHERE store_id = ?1").bind(storeId),
    );
    for (const upload of mediaUploads) {
      statements.push(
        database
          .prepare(
            `INSERT INTO agenda_catalog_media (
               store_id, media_id, content_type, body, updated_at
             ) VALUES (?1, ?2, ?3, ?4, ?5)`,
          )
          .bind(
            storeId,
            upload.id,
            upload.contentType,
            upload.bytes.buffer,
            now,
          ),
      );
    }
  }
  if (shouldManageDomain && existingDomain && existingDomain.hostname !== customDomain) {
    statements.push(
      database.prepare("DELETE FROM agenda_store_domains WHERE store_id = ?1").bind(storeId),
    );
  }
  if (shouldManageDomain && customDomain) {
    const provisioning = domainProvisioning ?? {
      providerId: "",
      status: "pending" as const,
      providerStatus: "",
      sslStatus: "",
      cnameTarget: cloudflareSaasConfiguration().cnameTarget,
      validationRecordsJson: "[]",
      lastError: "",
      verifiedAt: null,
    };
    statements.push(
      database
        .prepare(
          `INSERT INTO agenda_store_domains (
             hostname, store_id, provider_id, status, provider_status, ssl_status,
             cname_target, validation_records_json, last_error, verified_at,
             created_at, updated_at
           ) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12)
           ON CONFLICT(hostname) DO UPDATE SET
             store_id = excluded.store_id,
             provider_id = excluded.provider_id,
             status = excluded.status,
             provider_status = excluded.provider_status,
             ssl_status = excluded.ssl_status,
             cname_target = excluded.cname_target,
             validation_records_json = excluded.validation_records_json,
             last_error = excluded.last_error,
             verified_at = excluded.verified_at,
             updated_at = excluded.updated_at`,
        )
        .bind(
          customDomain,
          storeId,
          provisioning.providerId,
          provisioning.status,
          provisioning.providerStatus,
          provisioning.sslStatus,
          provisioning.cnameTarget,
          provisioning.validationRecordsJson,
          provisioning.lastError,
          provisioning.verifiedAt,
          existingDomain?.hostname === customDomain ? existingDomain.created_at : now,
          domainProvisioningUpdatedAt,
        ),
    );
  }
  await database.batch(statements);

  const stored: StoreRow = {
    id: storeId,
    owner_user_id: owner.id,
    instance,
    license_hash: identity.licenseHash,
    machine_hash: identity.machineHash,
    machine_code: identity.machineCode,
    desired_slug: effectiveDesiredSlug,
    slug,
    name: storeName,
    segment,
    theme_json: themeJson,
    catalog_json: catalogJson,
    catalog_version: catalogVersion,
    catalog_published_at: catalogPublishedAt,
    generated_at: generatedAt,
    last_synced_at: now,
  };
  return agendaJson({
    ok: true,
    profileId: owner.id,
    slug,
    publicUrl:
      customDomain && domainProvisioning?.status === "active"
        ? `https://${customDomain}`
        : publicUrl(slug),
    publication: {
      status: catalogVersion > 0 ? "published" : "draft",
      version: catalogVersion,
      publishedAt: catalogPublishedAt > 0
        ? new Date(catalogPublishedAt).toISOString()
        : null,
      applied: shouldApplyPublication,
    },
    customDomain: customDomain
      ? {
          hostname: customDomain,
          status: domainProvisioning?.status || "pending",
          providerStatus: domainProvisioning?.providerStatus || "",
          sslStatus: domainProvisioning?.sslStatus || "",
          cnameTarget:
            domainProvisioning?.cnameTarget || cloudflareSaasConfiguration().cnameTarget,
          validationRecords: JSON.parse(
            domainProvisioning?.validationRecordsJson || "[]",
          ),
          lastError: domainProvisioning?.lastError || "",
        }
      : null,
    bookings: await pendingBookings(database, stored),
  });
}

async function publicStore(database: D1Database, slugValue: string) {
  const slug = normalizeSlug(slugValue);
  if (slug !== slugValue.toLowerCase()) {
    throw new AgendaBookingError(404, "store_not_found", "Agenda não encontrada.");
  }
  const store = await database
    .prepare("SELECT * FROM agenda_stores WHERE slug = ?1 LIMIT 1")
    .bind(slug)
    .first<StoreRow>();
  if (!store) throw new AgendaBookingError(404, "store_not_found", "Agenda não encontrada.");
  if (Number(store.catalog_version || 0) <= 0 || Number(store.catalog_published_at || 0) <= 0) {
    throw new AgendaBookingError(
      404,
      "catalog_not_published",
      "Este catálogo ainda não foi publicado.",
    );
  }
  const snapshot = await database
    .prepare("SELECT * FROM agenda_snapshots WHERE store_id = ?1 LIMIT 1")
    .bind(store.id)
    .first<SnapshotRow>();
  if (!snapshot) {
    throw new AgendaBookingError(503, "availability_offline", "A agenda está temporariamente indisponível.");
  }
  return { store, snapshot };
}

function assertFresh(store: StoreRow, snapshot: SnapshotRow) {
  const receivedAt = Number(snapshot.received_at || store.last_synced_at);
  if (!Number.isFinite(receivedAt) || Date.now() - receivedAt > snapshotTtlMs()) {
    throw new AgendaBookingError(
      503,
      "availability_stale",
      "A loja está atualizando os horários. Tente novamente em instantes.",
    );
  }
}

function lockMoments(startsAtMs: number, durationMinutes: number) {
  const count = Math.ceil(durationMinutes / 15);
  return Array.from({ length: count }, (_, index) => startsAtMs + index * FIFTEEN_MINUTES_MS);
}

function resourceLockId(resourceName: string) {
  const normalized = resourceName
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120);
  return normalized ? `resource:${normalized}` : "";
}

async function availableServices(
  database: D1Database,
  store: StoreRow,
  snapshot: SnapshotRow,
) {
  const services = parseStoredServices(snapshot.services_json);
  const lockResult = await database
    .prepare(
      "SELECT professional_id, lock_start_ms FROM agenda_booking_locks WHERE store_id = ?1",
    )
    .bind(store.id)
    .all<{ professional_id: string; lock_start_ms: number }>();
  const locks = new Set(
    (lockResult.results || []).map(
      (lock) => `${lock.professional_id}|${Number(lock.lock_start_ms)}`,
    ),
  );
  const now = Date.now();

  return services
    .map((service) => ({
      ...service,
      days: service.days
        .map((day) => ({
          ...day,
          availableSlots: day.availableSlots.filter((slot) => {
            const startsAtMs = Date.parse(slot.start);
            if (!Number.isFinite(startsAtMs) || startsAtMs <= now) return false;
            const resourceId = resourceLockId(slot.resourceName);
            return !lockMoments(startsAtMs, service.durationMinutes).some(
              (moment) =>
                locks.has(`${slot.professionalId}|${moment}`) ||
                (resourceId ? locks.has(`${resourceId}|${moment}`) : false),
            );
          }),
        }))
        .filter((day) => day.availableSlots.length > 0),
    }))
    .filter((service) => service.days.length > 0);
}

export async function getAvailability(slug: string) {
  const database = getAgendaD1();
  const { store, snapshot } = await publicStore(database, slug);
  assertFresh(store, snapshot);
  const customDomain = await database
    .prepare("SELECT * FROM agenda_store_domains WHERE store_id = ?1 LIMIT 1")
    .bind(store.id)
    .first<StoreDomainRow>();
  return agendaJson({
    ok: true,
    store: {
      slug: store.slug,
      name: store.name,
      segment: store.segment,
      theme: parseStoredTheme(store.theme_json),
      catalog: publicCatalog(store.catalog_json, store.slug, Number(store.catalog_version || 1)),
      publication: {
        version: Number(store.catalog_version || 0),
        publishedAt: new Date(Number(store.catalog_published_at)).toISOString(),
      },
      publicUrl: customDomain?.status === "active"
        ? `https://${customDomain.hostname}`
        : publicUrl(store.slug),
      customDomain: customDomain
        ? {
            hostname: customDomain.hostname,
            status: customDomain.status,
            providerStatus: customDomain.provider_status,
            sslStatus: customDomain.ssl_status,
            cnameTarget: customDomain.cname_target,
            lastError: customDomain.last_error,
          }
        : null,
      generatedAt: snapshot.generated_at,
    },
    services: await availableServices(database, store, snapshot),
  });
}

export async function getCatalogMetadata(slugValue: string) {
  const slug = normalizeSlug(slugValue);
  if (slug !== slugValue.toLowerCase()) return null;
  const database = getAgendaD1();
  const store = await database
    .prepare(
      `SELECT name, segment, catalog_json, catalog_version
       FROM agenda_stores
       WHERE slug = ?1
       LIMIT 1`,
    )
    .bind(slug)
    .first<{
      name: string;
      segment: string;
      catalog_json: string;
      catalog_version: number;
    }>();
  if (!store || Number(store.catalog_version || 0) <= 0) return null;
  const catalog = parseStoredCatalog(store.catalog_json);
  const seo = catalog.seo && typeof catalog.seo === "object" && !Array.isArray(catalog.seo)
    ? catalog.seo as JsonRecord
    : {};
  return {
    name: store.name,
    segment: store.segment,
    title: stringValue(seo.title, 160, `${store.name} | Catálogo e agendamento`),
    description: stringValue(
      seo.description,
      320,
      `Conheça os serviços de ${store.name}, consulte horários e agende online.`,
    ),
    imageUrl: typeof catalog.heroObjectKey === "string" && catalog.heroObjectKey
      ? `/api/agendar/${encodeURIComponent(slug)}/hero`
      : "",
  };
}

export async function getCatalogHero(slug: string) {
  const database = getAgendaD1();
  const normalizedSlug = normalizeSlug(slug);
  if (normalizedSlug !== slug.toLowerCase()) {
    throw new AgendaBookingError(404, "store_not_found", "Catálogo não encontrado.");
  }
  const store = await database
    .prepare(
      "SELECT id, catalog_json, catalog_version FROM agenda_stores WHERE slug = ?1 LIMIT 1",
    )
    .bind(normalizedSlug)
    .first<{ id: string; catalog_json: string; catalog_version: number }>();
  if (!store || Number(store.catalog_version || 0) <= 0) {
    throw new AgendaBookingError(404, "store_not_found", "Catálogo não encontrado.");
  }
  const catalog = parseStoredCatalog(store.catalog_json);
  const objectKey = typeof catalog.heroObjectKey === "string" ? catalog.heroObjectKey : "";
  if (objectKey === `d1:${store.id}`) {
    const asset = await database
      .prepare(
        "SELECT content_type, body, updated_at FROM agenda_catalog_assets WHERE store_id = ?1 LIMIT 1",
      )
      .bind(store.id)
      .first<{ content_type: string; body: ArrayBuffer; updated_at: number }>();
    if (!asset?.body) {
      throw new AgendaBookingError(404, "catalog_image_not_found", "Imagem não encontrada.");
    }
    const body = new Uint8Array(asset.body);
    if (body.byteLength === 0) {
      throw new AgendaBookingError(404, "catalog_image_not_found", "Imagem não encontrada.");
    }
    return new Response(body, {
      headers: {
        "Cache-Control": "public, max-age=3600, stale-while-revalidate=86400",
        "Content-Type": asset.content_type,
        ETag: `W/\"${store.id}-${Number(asset.updated_at || 0)}\"`,
        "X-Content-Type-Options": "nosniff",
      },
    });
  }
  if (!objectKey || !objectKey.startsWith(`catalog/${store.id}/`)) {
    throw new AgendaBookingError(404, "catalog_image_not_found", "Imagem não encontrada.");
  }
  const bucket = getOptionalAgendaCatalogR2();
  if (!bucket) {
    throw new AgendaBookingError(404, "catalog_image_not_found", "Imagem não encontrada.");
  }
  const object = await bucket.get(objectKey);
  if (!object) throw new AgendaBookingError(404, "catalog_image_not_found", "Imagem não encontrada.");
  const headers = new Headers();
  object.writeHttpMetadata(headers);
  headers.set("Cache-Control", "public, max-age=3600, stale-while-revalidate=86400");
  headers.set("ETag", object.httpEtag);
  headers.set("X-Content-Type-Options", "nosniff");
  return new Response(object.body, { headers });
}

export async function getCatalogMedia(slug: string, mediaIdValue: string) {
  const database = getAgendaD1();
  const normalizedSlug = normalizeSlug(slug);
  const mediaId = catalogIdentifier(mediaIdValue);
  if (
    normalizedSlug !== slug.toLowerCase() ||
    !mediaId ||
    mediaId !== mediaIdValue.toLowerCase()
  ) {
    throw new AgendaBookingError(404, "catalog_media_not_found", "Imagem não encontrada.");
  }
  const store = await database
    .prepare(
      "SELECT id, catalog_json, catalog_version FROM agenda_stores WHERE slug = ?1 LIMIT 1",
    )
    .bind(normalizedSlug)
    .first<{ id: string; catalog_json: string; catalog_version: number }>();
  if (!store || Number(store.catalog_version || 0) <= 0) {
    throw new AgendaBookingError(404, "catalog_media_not_found", "Imagem não encontrada.");
  }
  const catalog = parseStoredCatalog(store.catalog_json);
  const referenced = Array.isArray(catalog.sections) && catalog.sections.some((rawSection) => {
    if (!rawSection || typeof rawSection !== "object" || Array.isArray(rawSection)) return false;
    const items = (rawSection as JsonRecord).items;
    return Array.isArray(items) && items.some((rawItem) =>
      rawItem &&
      typeof rawItem === "object" &&
      !Array.isArray(rawItem) &&
      catalogIdentifier((rawItem as JsonRecord).mediaId) === mediaId
    );
  });
  if (!referenced) {
    throw new AgendaBookingError(404, "catalog_media_not_found", "Imagem não encontrada.");
  }
  const asset = await database
    .prepare(
      `SELECT content_type, body, updated_at
       FROM agenda_catalog_media
       WHERE store_id = ?1 AND media_id = ?2
       LIMIT 1`,
    )
    .bind(store.id, mediaId)
    .first<{ content_type: string; body: ArrayBuffer; updated_at: number }>();
  if (!asset?.body) {
    throw new AgendaBookingError(404, "catalog_media_not_found", "Imagem não encontrada.");
  }
  const body = new Uint8Array(asset.body);
  if (body.byteLength === 0) {
    throw new AgendaBookingError(404, "catalog_media_not_found", "Imagem não encontrada.");
  }
  return new Response(body, {
    headers: {
      "Cache-Control": "public, max-age=3600, stale-while-revalidate=86400",
      "Content-Type": asset.content_type,
      ETag: `W/\"${store.id}-${mediaId}-${Number(asset.updated_at || 0)}\"`,
      "X-Content-Type-Options": "nosniff",
    },
  });
}

function normalizeBrazilPhone(value: unknown) {
  let digits = String(value ?? "").replace(/\D/g, "");
  if (digits.startsWith("0055")) digits = digits.slice(4);
  else if (digits.startsWith("55") && digits.length >= 12) digits = digits.slice(2);
  if (!/^\d{10,11}$/.test(digits) || /^([0-9])\1+$/.test(digits)) {
    throw new AgendaBookingError(400, "invalid_phone", "Informe um WhatsApp brasileiro válido com DDD.");
  }
  const areaCode = Number(digits.slice(0, 2));
  if (areaCode < 11 || areaCode > 99) {
    throw new AgendaBookingError(400, "invalid_phone", "Informe um WhatsApp brasileiro válido com DDD.");
  }
  return `55${digits}`;
}

function publicBooking(row: BookingRow, statusToken: string) {
  return {
    id: row.id,
    status: row.status,
    statusToken,
    startsAt: row.starts_at,
    serviceName: row.service_name,
  };
}

async function statusTokenFor(storeId: string, idempotencyKey: string) {
  const secret =
    runtimeValue("AGENDA_STATUS_TOKEN_SECRET") ||
    runtimeValue("AGENDA_LICENSE_SECRET") ||
    DEFAULT_LICENSE_SECRET;
  return bytesToBase64Url(
    await hmacSha256(`agenda-status|${storeId}|${idempotencyKey}`, secret),
  );
}

function bookingFromSnapshot(
  services: BookingService[],
  serviceId: string,
  slotId: string,
) {
  const service = services.find((item) => item.id === serviceId);
  if (!service) {
    throw new AgendaBookingError(409, "service_unavailable", "Este serviço não está mais disponível.");
  }
  for (const day of service.days) {
    const slot = day.availableSlots.find((item) => item.id === slotId);
    if (slot) return { service, slot };
  }
  throw new AgendaBookingError(409, "slot_unavailable", "Este horário não está mais disponível.");
}

export async function createAppointment(request: Request, slug: string) {
  const body = await readJsonBody(request, 20_000);
  const rawItems = Array.isArray(body.items) ? body.items.slice(0, 6) : [];
  const requestedItems = rawItems
    .filter((item) => item && typeof item === "object" && !Array.isArray(item))
    .map((item) => {
      const record = item as JsonRecord;
      return {
        serviceId: stringValue(record.serviceId, 128),
        slotId: stringValue(record.slotId, 160),
      };
    })
    .filter((item) => item.serviceId && item.slotId);
  if (!requestedItems.length) {
    requestedItems.push({
      serviceId: stringValue(body.serviceId, 128),
      slotId: stringValue(body.slotId, 160),
    });
  }
  const serviceId = requestedItems[0]?.serviceId || "";
  const slotId = requestedItems[0]?.slotId || "";
  const customerName = stringValue(body.customerName, 100).replace(/\s+/g, " ");
  const customerPhone = normalizeBrazilPhone(body.customerPhone);
  const notes = stringValue(body.notes, 500);
  const idempotencyKey = stringValue(
    body.idempotencyKey || request.headers.get("idempotency-key"),
    128,
  );
  if (customerName.length < 2) {
    throw new AgendaBookingError(400, "invalid_name", "Informe seu nome.");
  }
  if (!serviceId || !slotId || !/^[A-Za-z0-9._:-]{8,128}$/.test(idempotencyKey)) {
    throw new AgendaBookingError(400, "invalid_request", "Selecione um serviço e um horário válidos.");
  }

  const database = getAgendaD1();
  const { store, snapshot } = await publicStore(database, slug);
  assertFresh(store, snapshot);
  const statusToken = await statusTokenFor(store.id, idempotencyKey);
  const statusTokenHash = await sha256(statusToken);
  const existing = await database
    .prepare(
      "SELECT * FROM agenda_bookings WHERE store_id = ?1 AND idempotency_key = ?2 LIMIT 1",
    )
    .bind(store.id, idempotencyKey)
    .first<BookingRow>();
  if (existing) return agendaJson({ ok: true, booking: publicBooking(existing, statusToken) });

  const services = parseStoredServices(snapshot.services_json);
  const selections = requestedItems.map((item) =>
    bookingFromSnapshot(services, item.serviceId, item.slotId)
  );
  const { slot } = selections[0];
  const startsAtMs = Date.parse(slot.start);
  if (!Number.isFinite(startsAtMs) || startsAtMs <= Date.now()) {
    throw new AgendaBookingError(409, "slot_unavailable", "Este horário não está mais disponível.");
  }
  let elapsedMinutes = 0;
  for (const selection of selections) {
    const itemStartsAtMs = Date.parse(selection.slot.start);
    if (
      selection.slot.professionalId !== slot.professionalId ||
      itemStartsAtMs !== startsAtMs + elapsedMinutes * 60_000
    ) {
      throw new AgendaBookingError(
        409,
        "bundle_unavailable",
        "Esse conjunto de serviços não cabe mais no horário escolhido.",
      );
    }
    elapsedMinutes += selection.service.durationMinutes;
  }

  const now = Date.now();
  const preparedBookings = await Promise.all(selections.map(async (selection, index) => {
    const bookingId = crypto.randomUUID();
    const itemIdempotencyKey = index === 0
      ? idempotencyKey
      : `${idempotencyKey.slice(0, 120)}.${index}`;
    const itemStatusToken = index === 0
      ? statusToken
      : await statusTokenFor(store.id, itemIdempotencyKey);
    const itemStatusTokenHash = index === 0
      ? statusTokenHash
      : await sha256(itemStatusToken);
    const itemStartsAtMs = Date.parse(selection.slot.start);
    const slotKey = `${selection.slot.professionalId}:${itemStartsAtMs}`;
    const bookingStatement = database
      .prepare(
        `INSERT INTO agenda_bookings (
           id, store_id, source, status, status_token_hash, idempotency_key, slot_key,
           service_id, service_name, slot_id, starts_at, starts_at_ms, duration_minutes,
           price_cents, professional_id, professional_name, resource_name, customer_name,
           customer_phone, notes, created_at, updated_at
         ) VALUES (
           ?1, ?2, 'web', 'requested', ?3, ?4, ?5,
           ?6, ?7, ?8, ?9, ?10, ?11,
           ?12, ?13, ?14, ?15, ?16,
           ?17, ?18, ?19, ?19
         )`,
      )
      .bind(
        bookingId,
        store.id,
        itemStatusTokenHash,
        itemIdempotencyKey,
        slotKey,
        selection.service.id,
        selection.service.name,
        selection.slot.id,
        selection.slot.start,
        itemStartsAtMs,
        selection.service.durationMinutes,
        Math.round(selection.service.price * 100),
        selection.slot.professionalId,
        selection.slot.professionalName,
        selection.slot.resourceName,
        customerName,
        customerPhone,
        notes,
        now,
      );
    const lockOwners = [selection.slot.professionalId];
    const resourceId = resourceLockId(selection.slot.resourceName);
    if (resourceId) lockOwners.push(resourceId);
    const lockStatements = lockOwners.flatMap((ownerId) =>
      lockMoments(itemStartsAtMs, selection.service.durationMinutes).map((lockStartMs) =>
        database
          .prepare(
            `INSERT INTO agenda_booking_locks
               (store_id, professional_id, lock_start_ms, booking_id)
             VALUES (?1, ?2, ?3, ?4)`,
          )
          .bind(store.id, ownerId, lockStartMs, bookingId),
      ),
    );
    return { bookingId, statusToken: itemStatusToken, bookingStatement, lockStatements };
  }));
  const bookingStatements = preparedBookings.map((item) => item.bookingStatement);
  const lockStatements = preparedBookings.flatMap((item) => item.lockStatements);

  try {
    await database.batch([...bookingStatements, ...lockStatements]);
  } catch (error) {
    const idempotent = await database
      .prepare(
        "SELECT * FROM agenda_bookings WHERE store_id = ?1 AND idempotency_key = ?2 LIMIT 1",
      )
      .bind(store.id, idempotencyKey)
      .first<BookingRow>();
    if (idempotent) {
      return agendaJson({ ok: true, booking: publicBooking(idempotent, statusToken) });
    }
    console.warn("Agenda slot reservation conflict", error);
    throw new AgendaBookingError(409, "slot_unavailable", "Este horário acabou de ser reservado.");
  }

  const createdBookings = await Promise.all(preparedBookings.map((item) =>
    database
      .prepare("SELECT * FROM agenda_bookings WHERE id = ?1 LIMIT 1")
      .bind(item.bookingId)
      .first<BookingRow>()
  ));
  if (createdBookings.some((created) => !created)) {
    throw new Error("Booking insert succeeded but one or more rows were not found");
  }
  const publicBookings = createdBookings.map((created, index) =>
    publicBooking(created as BookingRow, preparedBookings[index].statusToken)
  );
  return agendaJson({
    ok: true,
    booking: publicBookings[0],
    bookings: publicBookings,
  }, 201);
}

function validSentAt(value: unknown) {
  if (value === true) return Date.now();
  if (typeof value !== "string") return undefined;
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function assertStatusTransition(current: string, next: string) {
  if (current === next) return;
  if (!ALLOWED_STATUSES.has(next)) {
    throw new AgendaBookingError(400, "invalid_status", "Status de agendamento inválido.");
  }
  if (TERMINAL_STATUSES.has(current)) {
    throw new AgendaBookingError(409, "booking_closed", "Este agendamento já foi encerrado.");
  }
  if (current === "confirmed" && !["cancelled", "completed"].includes(next)) {
    throw new AgendaBookingError(409, "invalid_transition", "Não é possível voltar o status deste agendamento.");
  }
}

export async function patchInternalBooking(request: Request, bookingId: string) {
  const [identity, owner] = await Promise.all([
    authenticateInternal(request),
    authenticateCatalogOwner(request),
  ]);
  const body = await readJsonBody(request, 20_000);
  const database = getAgendaD1();
  const booking = await database
    .prepare("SELECT * FROM agenda_bookings WHERE id = ?1 LIMIT 1")
    .bind(stringValue(bookingId, 128))
    .first<BookingRow>();
  if (!booking) throw new AgendaBookingError(404, "booking_not_found", "Agendamento não encontrado.");
  const store = await database
    .prepare("SELECT * FROM agenda_stores WHERE id = ?1 LIMIT 1")
    .bind(booking.store_id)
    .first<StoreRow>();
  if (!store) throw new AgendaBookingError(404, "store_not_found", "Agenda não encontrada.");
  assertStoreOwner(store, owner.id, identity);

  const nextStatus = body.status === undefined
    ? booking.status
    : stringValue(body.status, 32).toLowerCase();
  assertStatusTransition(booking.status, nextStatus);
  const appointmentId = body.appointmentId === undefined
    ? booking.appointment_id
    : stringValue(body.appointmentId, 128) || null;
  const message = body.message === undefined
    ? booking.message
    : stringValue(body.message, 500) || null;
  const confirmationSentAt = body.confirmationSentAt === undefined
    ? booking.confirmation_sent_at
    : validSentAt(body.confirmationSentAt);
  const reminderSentAt = body.reminderSentAt === undefined
    ? booking.reminder_sent_at
    : validSentAt(body.reminderSentAt);
  if (body.confirmationSentAt !== undefined && confirmationSentAt === undefined) {
    throw new AgendaBookingError(400, "invalid_confirmation_time", "Data de confirmação inválida.");
  }
  if (body.reminderSentAt !== undefined && reminderSentAt === undefined) {
    throw new AgendaBookingError(400, "invalid_reminder_time", "Data de lembrete inválida.");
  }

  const now = Date.now();
  const confirmedAt = nextStatus === "confirmed"
    ? booking.confirmed_at || now
    : booking.confirmed_at;
  const update = database
    .prepare(
      `UPDATE agenda_bookings
       SET status = ?1, appointment_id = ?2, message = ?3,
           confirmation_sent_at = ?4, reminder_sent_at = ?5,
           confirmed_at = ?6, updated_at = ?7
       WHERE id = ?8 AND store_id = ?9`,
    )
    .bind(
      nextStatus,
      appointmentId,
      message,
      confirmationSentAt ?? null,
      reminderSentAt ?? null,
      confirmedAt ?? null,
      now,
      booking.id,
      store.id,
    );
  if (TERMINAL_STATUSES.has(nextStatus)) {
    await database.batch([
      update,
      database.prepare("DELETE FROM agenda_booking_locks WHERE booking_id = ?1").bind(booking.id),
    ]);
  } else {
    await update.run();
  }

  const updated = await database
    .prepare("SELECT * FROM agenda_bookings WHERE id = ?1 LIMIT 1")
    .bind(booking.id)
    .first<BookingRow>();
  if (!updated) throw new Error("Booking update succeeded but row was not found");
  return agendaJson({ ok: true, booking: bookingInternal(updated, store.instance) });
}

export async function getBookingStatus(request: Request, slug: string, bookingId: string) {
  const token = stringValue(new URL(request.url).searchParams.get("token"), 256);
  if (!token) {
    throw new AgendaBookingError(401, "status_token_required", "Informe o código de acompanhamento.");
  }
  const database = getAgendaD1();
  const { store } = await publicStore(database, slug);
  const tokenHash = await sha256(token);
  const booking = await database
    .prepare(
      `SELECT * FROM agenda_bookings
       WHERE id = ?1 AND store_id = ?2 AND status_token_hash = ?3
       LIMIT 1`,
    )
    .bind(stringValue(bookingId, 128), store.id, tokenHash)
    .first<BookingRow>();
  if (!booking) {
    throw new AgendaBookingError(404, "booking_not_found", "Agendamento não encontrado.");
  }
  return agendaJson({
    ok: true,
    booking: {
      id: booking.id,
      status: booking.status,
      message: booking.message || "",
      startsAt: booking.starts_at,
      serviceName: booking.service_name,
    },
  });
}
