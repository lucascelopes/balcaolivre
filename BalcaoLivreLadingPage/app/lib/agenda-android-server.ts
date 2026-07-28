import { env } from "cloudflare:workers";
import { getAgendaAndroidR2, getAgendaD1 } from "../../db/index";

const SUPABASE_URL_FALLBACK = "https://hzvplpotsdzxygkxrgyi.supabase.co";
const SUPABASE_PUBLISHABLE_KEY_FALLBACK =
  "sb_publishable_qNl5_EGAeuhN6PqTzRIeyQ_YQV2MdV6";
const ANDROID_APPLICATION_ID = "br.com.balcaolivre.agenda_livre";
const TRIAL_DURATION_MS = 7 * 24 * 60 * 60 * 1000;
const DEVICE_SESSION_DURATION_MS = 180 * 24 * 60 * 60 * 1000;
const OFFLINE_LEASE_DURATION_MS = 24 * 60 * 60 * 1000;
const PROVISIONING_TOKEN_DURATION_MS = 30 * 24 * 60 * 60 * 1000;
const DOWNLOAD_TOKEN_DURATION_MS = 15 * 60 * 1000;
const MAX_JSON_BYTES = 64 * 1024;
const MAX_ICON_BYTES = 5 * 1024 * 1024;
const MAX_COVER_BYTES = 10 * 1024 * 1024;
const MAX_APK_BYTES = 95 * 1024 * 1024;

type JsonObject = Record<string, unknown>;

type SupabaseUserPayload = {
  id?: unknown;
  email?: unknown;
};

export type AgendaAndroidSupabaseUser = {
  kind: "supabase";
  userId: string;
  email: string;
};

export type AgendaAndroidDeviceActor = {
  kind: "device";
  userId: string;
  email: string;
  deviceId: string;
  devicePublicId: string;
  sessionId: string;
  buildId: string;
};

type EntitlementRow = {
  user_id: string;
  status: string;
  trial_started_at: number | null;
  trial_ends_at: number | null;
  current_period_ends_at: number | null;
  grace_ends_at: number | null;
  payment_url: string | null;
  support_url: string | null;
  provider: string | null;
  provider_customer_id: string | null;
  provider_subscription_id: string | null;
  provider_event_id: string | null;
  provider_event_at: number | null;
  created_at: number;
  updated_at: number;
};

type BuildRow = {
  id: string;
  user_id: string;
  registration_id: string;
  status: string;
  application_id: string;
  app_name: string;
  version_code: number;
  version_name: string;
  icon_object_key: string;
  icon_content_type: string;
  icon_sha256: string;
  cover_object_key: string | null;
  cover_content_type: string | null;
  cover_sha256: string | null;
  artifact_object_key: string | null;
  artifact_file_name: string | null;
  artifact_content_type: string | null;
  artifact_size: number | null;
  artifact_sha256: string | null;
  download_token_hash: string | null;
  download_token_expires_at: number | null;
  worker_id: string | null;
  attempt_count: number;
  error_code: string | null;
  error_message: string | null;
  created_at: number;
  started_at: number | null;
  completed_at: number | null;
  updated_at: number;
};

type DeviceSessionRow = {
  session_id: string;
  user_id: string;
  session_expires_at: number;
  device_id: string;
  device_public_id: string;
  build_id: string;
  email: string | null;
};

type UploadedImage = {
  objectKey: string;
  contentType: "image/png" | "image/jpeg" | "image/webp";
  sha256: string;
};

export type AgendaEntitlementState = {
  status: string;
  canUse: boolean;
  trialStartedAt: string | null;
  trialEndsAt: string | null;
  daysRemaining: number;
  currentPeriodEndsAt: string | null;
  graceEndsAt: string | null;
  leaseExpiresAt: string | null;
  leaseToken?: string;
  paymentUrl: string | null;
  supportUrl: string | null;
};

export class AgendaAndroidError extends Error {
  readonly status: number;
  readonly code: string;
  readonly details?: unknown;

  constructor(status: number, code: string, message: string, details?: unknown) {
    super(message);
    this.name = "AgendaAndroidError";
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

function runtimeValue(name: string) {
  const runtime = env as unknown as Record<string, unknown>;
  const value = runtime?.[name];
  if (typeof value === "string" && value.trim()) return value.trim();
  return typeof process !== "undefined" ? process.env[name]?.trim() || "" : "";
}

function supabaseUrl() {
  return (
    runtimeValue("BALCAO_SUPABASE_URL") ||
    runtimeValue("SUPABASE_URL") ||
    SUPABASE_URL_FALLBACK
  ).replace(/\/+$/, "");
}

function supabasePublishableKey() {
  return (
    runtimeValue("BALCAO_SUPABASE_PUBLISHABLE_KEY") ||
    runtimeValue("SUPABASE_PUBLISHABLE_KEY") ||
    SUPABASE_PUBLISHABLE_KEY_FALLBACK
  );
}

function corsHeaders(methods = "GET, POST, OPTIONS") {
  return {
    "Access-Control-Allow-Headers":
      "Authorization, Content-Type, X-Agenda-Apk-Sha256, X-Agenda-Worker-Id",
    "Access-Control-Allow-Methods": methods,
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Max-Age": "86400",
    "Cache-Control": "no-store, max-age=0",
    "Content-Type": "application/json; charset=utf-8",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  };
}

function jsonResponse(data: unknown, status = 200, methods?: string) {
  return new Response(JSON.stringify(data), {
    status,
    headers: corsHeaders(methods),
  });
}

export function agendaAndroidOptionsResponse(methods = "GET, POST, OPTIONS") {
  const headers = corsHeaders(methods);
  delete (headers as Partial<typeof headers>)["Content-Type"];
  return new Response(null, { status: 204, headers });
}

export function agendaAndroidErrorResponse(error: unknown) {
  if (error instanceof AgendaAndroidError) {
    return jsonResponse(
      {
        ok: false,
        error: {
          code: error.code,
          message: error.message,
          ...(error.details === undefined ? {} : { details: error.details }),
        },
      },
      error.status,
    );
  }

  console.error("Agenda Android route failed", error);
  return jsonResponse(
    {
      ok: false,
      error: {
        code: "internal_error",
        message: "Nao foi possivel concluir esta operacao agora.",
      },
    },
    500,
  );
}

function bytesToHex(bytes: ArrayBuffer | Uint8Array) {
  const view = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
  return Array.from(view, (byte) => byte.toString(16).padStart(2, "0")).join("");
}

function base64Url(bytes: Uint8Array) {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function randomToken(byteLength = 32) {
  const bytes = new Uint8Array(byteLength);
  crypto.getRandomValues(bytes);
  return base64Url(bytes);
}

async function sha256Hex(value: string | ArrayBuffer | Uint8Array) {
  const bytes =
    typeof value === "string"
      ? new TextEncoder().encode(value)
      : value instanceof Uint8Array
        ? value
        : new Uint8Array(value);
  return bytesToHex(await crypto.subtle.digest("SHA-256", bytes));
}

async function hmacSha256(secret: string, value: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  return base64Url(
    new Uint8Array(
      await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(value)),
    ),
  );
}

async function secretsMatch(provided: string, expected: string) {
  if (!provided || !expected) return false;
  const [a, b] = await Promise.all([sha256Hex(provided), sha256Hex(expected)]);
  let different = a.length ^ b.length;
  const size = Math.max(a.length, b.length);
  for (let i = 0; i < size; i += 1) {
    different |= (a.charCodeAt(i) || 0) ^ (b.charCodeAt(i) || 0);
  }
  return different === 0;
}

function safeIdentifier(value: unknown, field: string, maxLength = 128) {
  const result = String(value || "").trim();
  if (
    !result ||
    result.length > maxLength ||
    !/^[A-Za-z0-9._:-]+$/.test(result)
  ) {
    throw new AgendaAndroidError(400, `invalid_${field}`, `O campo ${field} e invalido.`);
  }
  return result;
}

function safeText(value: unknown, field: string, maxLength: number) {
  const result = String(value || "").trim().replace(/\s+/g, " ");
  if (
    !result ||
    result.length > maxLength ||
    /[\u0000-\u001f\u007f]/.test(result)
  ) {
    throw new AgendaAndroidError(400, `invalid_${field}`, `O campo ${field} e invalido.`);
  }
  return result;
}

function optionalUrl(value: unknown, field: string) {
  const text = String(value || "").trim();
  if (!text) return null;
  try {
    const parsed = new URL(text);
    if (parsed.protocol !== "https:" && parsed.protocol !== "http:") throw new Error();
    return parsed.toString();
  } catch {
    throw new AgendaAndroidError(400, `invalid_${field}`, `O campo ${field} e invalido.`);
  }
}

function iso(value: number | null | undefined) {
  return value == null ? null : new Date(Number(value)).toISOString();
}

function timestamp(value: unknown, field: string, allowNull = true) {
  if (value === null || value === undefined || value === "") {
    if (allowNull) return null;
    throw new AgendaAndroidError(400, `invalid_${field}`, `O campo ${field} e obrigatorio.`);
  }
  const parsed =
    typeof value === "number" ? value : Date.parse(String(value || "").trim());
  if (!Number.isFinite(parsed) || parsed < 0 || parsed > 8_640_000_000_000_000) {
    throw new AgendaAndroidError(400, `invalid_${field}`, `O campo ${field} e invalido.`);
  }
  return Math.trunc(parsed);
}

async function readJsonBody(request: Request, allowEmpty = false): Promise<JsonObject> {
  const declared = Number(request.headers.get("content-length") || "0");
  if (Number.isFinite(declared) && declared > MAX_JSON_BYTES) {
    throw new AgendaAndroidError(413, "payload_too_large", "O envio ultrapassa o limite permitido.");
  }
  const source = await request.text();
  if (!source && allowEmpty) return {};
  if (!source || new TextEncoder().encode(source).byteLength > MAX_JSON_BYTES) {
    throw new AgendaAndroidError(400, "invalid_body", "Envie os dados solicitados.");
  }
  try {
    const parsed = JSON.parse(source);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error();
    return parsed as JsonObject;
  } catch {
    throw new AgendaAndroidError(400, "invalid_json", "O JSON enviado e invalido.");
  }
}

export async function authenticateAgendaSupabaseUser(
  request: Request,
): Promise<AgendaAndroidSupabaseUser> {
  const authorization = request.headers.get("authorization") || "";
  const accessToken = authorization.match(/^Bearer\s+([^\s]+)$/i)?.[1] || "";
  if (!accessToken || accessToken.length > 8192) {
    throw new AgendaAndroidError(401, "unauthorized", "Entre na sua conta para continuar.");
  }

  let response: Response;
  try {
    response = await fetch(`${supabaseUrl()}/auth/v1/user`, {
      headers: {
        apikey: supabasePublishableKey(),
        Authorization: `Bearer ${accessToken}`,
      },
      cache: "no-store",
    });
  } catch {
    throw new AgendaAndroidError(503, "auth_unavailable", "O login esta temporariamente indisponivel.");
  }

  if (response.status === 401 || response.status === 403) {
    throw new AgendaAndroidError(401, "unauthorized", "Sua sessao expirou. Entre novamente.");
  }
  if (!response.ok) {
    throw new AgendaAndroidError(503, "auth_unavailable", "O login esta temporariamente indisponivel.");
  }

  let payload: SupabaseUserPayload;
  try {
    payload = (await response.json()) as SupabaseUserPayload;
  } catch {
    throw new AgendaAndroidError(503, "invalid_auth_response", "O login retornou uma resposta invalida.");
  }
  const userId = String(payload.id || "").trim();
  if (!/^[A-Za-z0-9_-]{8,128}$/.test(userId)) {
    throw new AgendaAndroidError(401, "unauthorized", "Nao foi possivel identificar esta conta.");
  }
  return {
    kind: "supabase",
    userId,
    email: String(payload.email || "").trim().toLowerCase().slice(0, 320),
  };
}

export async function authenticateAgendaAndroidDevice(
  request: Request,
): Promise<AgendaAndroidDeviceActor> {
  const authorization = request.headers.get("authorization") || "";
  const token = authorization.match(/^Device\s+([A-Za-z0-9_-]{32,512})$/i)?.[1] || "";
  if (!token) {
    throw new AgendaAndroidError(401, "invalid_device_session", "Este aparelho precisa ser ativado novamente.");
  }
  const tokenHash = await sha256Hex(token);
  const now = Date.now();
  const row = await getAgendaD1()
    .prepare(
      `SELECT s.id AS session_id, s.user_id, s.expires_at AS session_expires_at,
              d.id AS device_id, d.device_public_id, d.build_id, a.email
       FROM agenda_android_sessions s
       JOIN agenda_android_devices d ON d.id = s.device_id
       LEFT JOIN agenda_cloud_accounts a ON a.user_id = s.user_id
       WHERE s.token_hash = ?1 AND s.revoked_at IS NULL AND s.expires_at > ?2
         AND d.revoked_at IS NULL
       LIMIT 1`,
    )
    .bind(tokenHash, now)
    .first<DeviceSessionRow>();
  if (!row) {
    throw new AgendaAndroidError(401, "invalid_device_session", "Este aparelho precisa ser ativado novamente.");
  }

  await getAgendaD1().batch([
    getAgendaD1()
      .prepare("UPDATE agenda_android_sessions SET last_seen_at = ?1, updated_at = ?1 WHERE id = ?2")
      .bind(now, row.session_id),
    getAgendaD1()
      .prepare("UPDATE agenda_android_devices SET last_seen_at = ?1, updated_at = ?1 WHERE id = ?2")
      .bind(now, row.device_id),
  ]);

  return {
    kind: "device",
    userId: row.user_id,
    email: row.email || "",
    deviceId: row.device_id,
    devicePublicId: row.device_public_id,
    sessionId: row.session_id,
    buildId: row.build_id,
  };
}

async function authenticateAccountOrDevice(request: Request) {
  return /^Device\s+/i.test(request.headers.get("authorization") || "")
    ? authenticateAgendaAndroidDevice(request)
    : authenticateAgendaSupabaseUser(request);
}

async function requireSecret(request: Request, scheme: "Builder" | "Billing", environmentName: string) {
  const expected = runtimeValue(environmentName);
  if (!expected) {
    throw new AgendaAndroidError(503, "server_not_configured", "A integracao segura ainda nao foi configurada.");
  }
  const authorization = request.headers.get("authorization") || "";
  const provided = authorization.match(new RegExp(`^${scheme}\\s+([^\\s]+)$`, "i"))?.[1] || "";
  if (!(await secretsMatch(provided, expected))) {
    throw new AgendaAndroidError(401, "unauthorized", "Credencial interna invalida.");
  }
}

async function ensureCloudAccount(user: AgendaAndroidSupabaseUser) {
  const now = Date.now();
  await getAgendaD1()
    .prepare(
      `INSERT INTO agenda_cloud_accounts (
         user_id, email, payload_json, revision, schema_version,
         trial_started_at, trial_ends_at, last_device_id, created_at, updated_at
       ) VALUES (?1, ?2, NULL, 0, 1, ?3, ?4, '', ?3, ?3)
       ON CONFLICT(user_id) DO UPDATE SET email = excluded.email`,
    )
    .bind(user.userId, user.email, now, now + TRIAL_DURATION_MS)
    .run();
}

async function ensurePendingEntitlement(userId: string, now = Date.now()) {
  await getAgendaD1()
    .prepare(
      `INSERT INTO agenda_android_entitlements (
         user_id, status, trial_started_at, trial_ends_at,
         current_period_ends_at, grace_ends_at, created_at, updated_at
       ) VALUES (?1, 'pending_activation', NULL, NULL, NULL, NULL, ?2, ?2)
       ON CONFLICT(user_id) DO NOTHING`,
    )
    .bind(userId, now)
    .run();
}

async function readEntitlement(userId: string) {
  return getAgendaD1()
    .prepare("SELECT * FROM agenda_android_entitlements WHERE user_id = ?1 LIMIT 1")
    .bind(userId)
    .first<EntitlementRow>();
}

function entitlementCore(row: EntitlementRow | null, now = Date.now()) {
  const rawStatus = row?.status || "pending_activation";
  const trialStartedAt = row?.trial_started_at ?? null;
  const trialEndsAt = row?.trial_ends_at ?? null;
  const currentPeriodEndsAt = row?.current_period_ends_at ?? null;
  const graceEndsAt = row?.grace_ends_at ?? null;
  let status = rawStatus;
  let canUse = false;
  let accessEndsAt: number | null = null;

  if (rawStatus === "trialing") {
    canUse = trialEndsAt !== null && trialEndsAt > now;
    accessEndsAt = trialEndsAt;
    if (!canUse) status = "expired";
  } else if (rawStatus === "active") {
    canUse =
      currentPeriodEndsAt !== null
        ? currentPeriodEndsAt > now
        : row?.provider !== "stripe";
    accessEndsAt = currentPeriodEndsAt;
    if (!canUse) status = "expired";
  } else if (rawStatus === "past_due") {
    canUse = graceEndsAt !== null && graceEndsAt > now;
    accessEndsAt = graceEndsAt;
  } else if (rawStatus === "canceled") {
    canUse = currentPeriodEndsAt !== null && currentPeriodEndsAt > now;
    accessEndsAt = currentPeriodEndsAt;
  }

  const leaseExpiresAt = canUse
    ? Math.min(now + OFFLINE_LEASE_DURATION_MS, accessEndsAt ?? now + OFFLINE_LEASE_DURATION_MS)
    : null;
  return {
    status,
    canUse,
    trialStartedAt,
    trialEndsAt,
    currentPeriodEndsAt,
    graceEndsAt,
    leaseExpiresAt,
    daysRemaining:
      trialEndsAt && trialEndsAt > now
        ? Math.max(0, Math.ceil((trialEndsAt - now) / (24 * 60 * 60 * 1000)))
        : 0,
    paymentUrl: row?.payment_url || optionalRuntimeUrl("AGENDA_ANDROID_PAYMENT_URL"),
    supportUrl: row?.support_url || optionalRuntimeUrl("AGENDA_ANDROID_SUPPORT_URL"),
  };
}

function optionalRuntimeUrl(name: string) {
  const value = runtimeValue(name);
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null;
  } catch {
    return null;
  }
}

async function entitlementState(
  row: EntitlementRow | null,
  userId: string,
  sessionId?: string,
  now = Date.now(),
): Promise<AgendaEntitlementState> {
  const core = entitlementCore(row, now);
  let leaseToken: string | undefined;
  const leaseSecret = runtimeValue("AGENDA_ANDROID_LEASE_SECRET");
  if (core.canUse && core.leaseExpiresAt && leaseSecret && sessionId) {
    const payload = base64Url(
      new TextEncoder().encode(
        JSON.stringify({
          v: 1,
          sub: userId,
          sid: sessionId,
          status: core.status,
          exp: core.leaseExpiresAt,
        }),
      ),
    );
    leaseToken = `v1.${payload}.${await hmacSha256(leaseSecret, `v1.${payload}`)}`;
  }
  return {
    status: core.status,
    canUse: core.canUse,
    trialStartedAt: iso(core.trialStartedAt),
    trialEndsAt: iso(core.trialEndsAt),
    daysRemaining: core.daysRemaining,
    currentPeriodEndsAt: iso(core.currentPeriodEndsAt),
    graceEndsAt: iso(core.graceEndsAt),
    leaseExpiresAt: iso(core.leaseExpiresAt),
    ...(leaseToken ? { leaseToken } : {}),
    paymentUrl: core.paymentUrl,
    supportUrl: core.supportUrl,
  };
}

export async function assertAgendaAndroidCanUse(userId: string) {
  const row = await readEntitlement(userId);
  const state = await entitlementState(row, userId);
  if (!state.canUse) {
    throw new AgendaAndroidError(
      402,
      "subscription_required",
      "O teste terminou ou o pagamento precisa ser regularizado.",
      { entitlement: state },
    );
  }
  return state;
}

export async function ensureAgendaEntitlementForUser(
  userId: string,
  now = Date.now(),
) {
  await ensurePendingEntitlement(userId, now);
  const existing = await readEntitlement(userId);
  if (existing?.status === "pending_activation") {
    await getAgendaD1()
      .prepare(
        `UPDATE agenda_android_entitlements
         SET status = 'trialing',
             trial_started_at = COALESCE(trial_started_at, ?1),
             trial_ends_at = COALESCE(trial_ends_at, ?2),
             updated_at = ?1
         WHERE user_id = ?3 AND status = 'pending_activation'`,
      )
      .bind(now, now + TRIAL_DURATION_MS, userId)
      .run();
  }
  return entitlementState(await readEntitlement(userId), userId, undefined, now);
}

export async function getAgendaEntitlementForUser(userId: string) {
  return entitlementState(await readEntitlement(userId), userId);
}

export async function applyAgendaStripeEntitlement(update: {
  userId: string;
  status: string;
  eventId: string;
  eventAt: number;
  currentPeriodEndsAt: number | null;
  providerCustomerId?: string | null;
  providerSubscriptionId?: string | null;
}) {
  return applyStripeBillingUpdate(update);
}

function imageType(bytes: Uint8Array): UploadedImage["contentType"] | null {
  if (
    bytes.length >= 8 &&
    bytes[0] === 0x89 &&
    bytes[1] === 0x50 &&
    bytes[2] === 0x4e &&
    bytes[3] === 0x47 &&
    bytes[4] === 0x0d &&
    bytes[5] === 0x0a &&
    bytes[6] === 0x1a &&
    bytes[7] === 0x0a
  ) return "image/png";
  if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) {
    return "image/jpeg";
  }
  if (
    bytes.length >= 12 &&
    bytes[0] === 0x52 &&
    bytes[1] === 0x49 &&
    bytes[2] === 0x46 &&
    bytes[3] === 0x46 &&
    bytes[8] === 0x57 &&
    bytes[9] === 0x45 &&
    bytes[10] === 0x42 &&
    bytes[11] === 0x50
  ) return "image/webp";
  return null;
}

async function uploadImage(
  value: FormDataEntryValue | null,
  objectKey: string,
  maximumBytes: number,
  required: boolean,
): Promise<UploadedImage | null> {
  if (!(value instanceof File) || value.size === 0) {
    if (required) {
      throw new AgendaAndroidError(400, "image_required", "Escolha a imagem solicitada.");
    }
    return null;
  }
  if (value.size > maximumBytes) {
    throw new AgendaAndroidError(413, "image_too_large", "A imagem ultrapassa o limite permitido.");
  }
  const buffer = await value.arrayBuffer();
  const detected = imageType(new Uint8Array(buffer));
  if (!detected) {
    throw new AgendaAndroidError(415, "invalid_image", "Envie uma imagem PNG, JPEG ou WebP valida.");
  }
  const sha256 = await sha256Hex(buffer);
  const extension =
    detected === "image/png" ? "png" : detected === "image/webp" ? "webp" : "jpg";
  const key = `${objectKey}.${extension}`;
  await getAgendaAndroidR2().put(key, buffer, {
    httpMetadata: { contentType: detected, cacheControl: "private, max-age=0" },
    customMetadata: { sha256 },
  });
  return { objectKey: key, contentType: detected, sha256 };
}

function filenameSlug(value: string) {
  const normalized = value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^A-Za-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 60);
  return normalized || "estabelecimento";
}

async function nextVersionCode(userId: string, now: number) {
  const row = await getAgendaD1()
    .prepare("SELECT MAX(version_code) AS value FROM agenda_android_builds WHERE user_id = ?1")
    .bind(userId)
    .first<{ value: number | null }>();
  return Math.max(Math.floor(now / 1000), Number(row?.value || 0) + 1);
}

async function dispatchAndroidBuild(buildId: string) {
  const token = runtimeValue("AGENDA_ANDROID_GITHUB_TOKEN");
  const repository = runtimeValue("AGENDA_ANDROID_GITHUB_REPOSITORY");
  const workflow = runtimeValue("AGENDA_ANDROID_GITHUB_WORKFLOW") || "agenda-android-build.yml";
  const ref = runtimeValue("AGENDA_ANDROID_GITHUB_WORKFLOW_REF");
  if (!token || !repository || !ref) return "polling";
  if (
    !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repository) ||
    !/^[A-Za-z0-9_.\/-]+$/.test(workflow) ||
    !/^[A-Za-z0-9_.\/-]+$/.test(ref)
  ) {
    console.error("Agenda Android GitHub dispatch configuration is invalid");
    return "failed";
  }
  try {
    const response = await fetch(
      `https://api.github.com/repos/${repository}/actions/workflows/${encodeURIComponent(workflow)}/dispatches`,
      {
        method: "POST",
        headers: {
          Accept: "application/vnd.github+json",
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
          "User-Agent": "agenda-livre-android-builder",
          "X-GitHub-Api-Version": "2022-11-28",
        },
        body: JSON.stringify({ ref, inputs: { build_id: buildId } }),
        cache: "no-store",
      },
    );
    if (response.status !== 204) {
      console.error("Agenda Android GitHub dispatch failed", response.status);
      return "failed";
    }
    return "sent";
  } catch (error) {
    console.error("Agenda Android GitHub dispatch unavailable", error);
    return "failed";
  }
}

export async function preRegisterAgendaAndroid(request: Request) {
  const user = await authenticateAgendaSupabaseUser(request);
  const contentType = request.headers.get("content-type") || "";
  if (!contentType.toLowerCase().startsWith("multipart/form-data")) {
    throw new AgendaAndroidError(415, "multipart_required", "Envie o cadastro com as imagens selecionadas.");
  }
  let form: FormData;
  try {
    form = await request.formData();
  } catch {
    throw new AgendaAndroidError(400, "invalid_form", "O pre-cadastro enviado e invalido.");
  }
  const businessName = safeText(form.get("businessName"), "business_name", 80);
  if (String(form.get("sideloadConsent") || "").trim().toLowerCase() !== "true") {
    throw new AgendaAndroidError(
      400,
      "sideload_consent_required",
      "Confirme que entende a instalacao direta do aplicativo Android.",
    );
  }
  const registrationId = crypto.randomUUID();
  const buildId = crypto.randomUUID();
  const baseKey = `android/branding/${user.userId}/${registrationId}`;
  const uploadedKeys: string[] = [];
  let icon: UploadedImage | null = null;
  let cover: UploadedImage | null = null;
  try {
    icon = await uploadImage(form.get("icon"), `${baseKey}/icon`, MAX_ICON_BYTES, true);
    if (icon) uploadedKeys.push(icon.objectKey);
    cover = await uploadImage(
      form.get("cover") || form.get("photo"),
      `${baseKey}/cover`,
      MAX_COVER_BYTES,
      true,
    );
    if (cover) uploadedKeys.push(cover.objectKey);
    if (!icon || !cover) throw new Error("Required branding images were not uploaded");

    const now = Date.now();
    const versionCode = await nextVersionCode(user.userId, now);
    const versionName = `1.0.${versionCode}`;
    await ensureCloudAccount(user);
    await ensurePendingEntitlement(user.userId, now);
    const database = getAgendaD1();
    await database.batch([
      database
        .prepare(
          `INSERT INTO agenda_android_registrations (
             id, user_id, email, business_name, status,
             sideload_consent_at, sideload_consent_version, created_at, updated_at
           ) VALUES (?1, ?2, ?3, ?4, 'active', ?5, 'direct-apk-v1', ?5, ?5)`,
        )
        .bind(registrationId, user.userId, user.email, businessName, now),
      database
        .prepare(
          `INSERT INTO agenda_android_branding (
             user_id, registration_id, business_name,
             icon_object_key, icon_content_type, icon_sha256,
             cover_object_key, cover_content_type, cover_sha256,
             created_at, updated_at
           ) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?10)
           ON CONFLICT(user_id) DO UPDATE SET
             registration_id = excluded.registration_id,
             business_name = excluded.business_name,
             icon_object_key = excluded.icon_object_key,
             icon_content_type = excluded.icon_content_type,
             icon_sha256 = excluded.icon_sha256,
             cover_object_key = excluded.cover_object_key,
             cover_content_type = excluded.cover_content_type,
             cover_sha256 = excluded.cover_sha256,
             updated_at = excluded.updated_at`,
        )
        .bind(
          user.userId,
          registrationId,
          businessName,
          icon.objectKey,
          icon.contentType,
          icon.sha256,
          cover.objectKey,
          cover.contentType,
          cover.sha256,
          now,
        ),
      database
        .prepare(
          `INSERT INTO agenda_android_builds (
             id, user_id, registration_id, status, application_id, app_name,
             version_code, version_name,
             icon_object_key, icon_content_type, icon_sha256,
             cover_object_key, cover_content_type, cover_sha256,
             attempt_count, created_at, updated_at
           ) VALUES (
             ?1, ?2, ?3, 'queued', ?4, ?5, ?6, ?7,
             ?8, ?9, ?10, ?11, ?12, ?13, 0, ?14, ?14
           )`,
        )
        .bind(
          buildId,
          user.userId,
          registrationId,
          ANDROID_APPLICATION_ID,
          businessName,
          versionCode,
          versionName,
          icon.objectKey,
          icon.contentType,
          icon.sha256,
          cover.objectKey,
          cover.contentType,
          cover.sha256,
          now,
        ),
    ]);

    const dispatch = await dispatchAndroidBuild(buildId);
    const entitlement = await entitlementState(await readEntitlement(user.userId), user.userId);
    const origin = new URL(request.url).origin;
    return jsonResponse(
      {
        ok: true,
        registration: { id: registrationId, businessName },
        build: {
          id: buildId,
          status: "queued",
          versionCode,
          versionName,
          statusUrl: `${origin}/api/agenda/android/builds/${buildId}`,
          dispatch,
        },
        entitlement,
      },
      202,
    );
  } catch (error) {
    if (uploadedKeys.length) {
      try {
        await getAgendaAndroidR2().delete(uploadedKeys);
      } catch (cleanupError) {
        console.error("Could not clean failed Android registration uploads", cleanupError);
      }
    }
    throw error;
  }
}

async function readBuild(buildId: string) {
  return getAgendaD1()
    .prepare("SELECT * FROM agenda_android_builds WHERE id = ?1 LIMIT 1")
    .bind(buildId)
    .first<BuildRow>();
}

function buildPublicState(row: BuildRow) {
  return {
    id: row.id,
    status: row.status,
    appName: row.app_name,
    versionCode: Number(row.version_code),
    versionName: row.version_name,
    attempts: Number(row.attempt_count),
    createdAt: iso(row.created_at),
    startedAt: iso(row.started_at),
    completedAt: iso(row.completed_at),
    error:
      row.status === "failed"
        ? { code: row.error_code || "build_failed", message: row.error_message || "A geracao do aplicativo falhou." }
        : null,
  };
}

export async function getAgendaAndroidBuildStatus(request: Request, buildIdValue: string) {
  const user = await authenticateAgendaSupabaseUser(request);
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const row = await readBuild(buildId);
  if (!row || row.user_id !== user.userId) {
    throw new AgendaAndroidError(404, "build_not_found", "Este aplicativo nao foi encontrado.");
  }

  let download: unknown = null;
  if (row.status === "ready" && row.artifact_object_key) {
    const token = randomToken();
    const tokenHash = await sha256Hex(token);
    const expiresAt = Date.now() + DOWNLOAD_TOKEN_DURATION_MS;
    await getAgendaD1()
      .prepare(
        `UPDATE agenda_android_builds
         SET download_token_hash = ?1, download_token_expires_at = ?2, updated_at = ?3
         WHERE id = ?4 AND user_id = ?5 AND status = 'ready'`,
      )
      .bind(tokenHash, expiresAt, Date.now(), buildId, user.userId)
      .run();
    const origin = new URL(request.url).origin;
    download = {
      url: `${origin}/api/agenda/android/builds/${buildId}/download?token=${encodeURIComponent(token)}`,
      expiresAt: iso(expiresAt),
      fileName: row.artifact_file_name,
      size: row.artifact_size,
      sha256: row.artifact_sha256,
    };
  }

  return jsonResponse({ ok: true, build: buildPublicState(row), download });
}

function contentDisposition(fileName: string) {
  const fallback = fileName.replace(/[^A-Za-z0-9._-]/g, "_").slice(0, 120) || "agenda-livre.apk";
  return `attachment; filename="${fallback}"; filename*=UTF-8''${encodeURIComponent(fileName)}`;
}

export async function downloadAgendaAndroidBuild(request: Request, buildIdValue: string) {
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const token = new URL(request.url).searchParams.get("token") || "";
  if (!/^[A-Za-z0-9_-]{32,512}$/.test(token)) {
    throw new AgendaAndroidError(401, "invalid_download", "Este link de download e invalido.");
  }
  const tokenHash = await sha256Hex(token);
  const row = await getAgendaD1()
    .prepare(
      `SELECT * FROM agenda_android_builds
       WHERE id = ?1 AND status = 'ready' AND artifact_object_key IS NOT NULL
         AND download_token_hash = ?2 AND download_token_expires_at > ?3
       LIMIT 1`,
    )
    .bind(buildId, tokenHash, Date.now())
    .first<BuildRow>();
  if (!row || !row.artifact_object_key) {
    throw new AgendaAndroidError(401, "invalid_download", "Este link expirou. Gere um novo link na sua conta.");
  }
  const object = await getAgendaAndroidR2().get(row.artifact_object_key);
  if (!object) {
    throw new AgendaAndroidError(503, "artifact_unavailable", "O arquivo esta temporariamente indisponivel.");
  }
  const headers = new Headers({
    "Cache-Control": "private, no-store, max-age=0",
    "Content-Disposition": contentDisposition(row.artifact_file_name || "agenda-livre.apk"),
    "Content-Type": row.artifact_content_type || "application/vnd.android.package-archive",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  });
  if (row.artifact_size != null) headers.set("Content-Length", String(row.artifact_size));
  if (row.artifact_sha256) headers.set("X-Agenda-Apk-Sha256", row.artifact_sha256);
  if (object.httpEtag) headers.set("ETag", object.httpEtag);
  return new Response(object.body, { status: 200, headers });
}

function builderAssetUrl(request: Request, buildId: string, kind: "icon" | "cover") {
  return `${new URL(request.url).origin}/api/agenda/android/internal/builds/${buildId}/assets/${kind}`;
}

function buildManifest(request: Request, row: BuildRow, provisioningToken?: string, tokenExpiresAt?: number) {
  return {
    id: row.id,
    applicationId: row.application_id,
    appName: row.app_name,
    versionCode: Number(row.version_code),
    versionName: row.version_name,
    branding: {
      icon: {
        url: builderAssetUrl(request, row.id, "icon"),
        contentType: row.icon_content_type,
        sha256: row.icon_sha256,
      },
      cover: row.cover_object_key
        ? {
            url: builderAssetUrl(request, row.id, "cover"),
            contentType: row.cover_content_type,
            sha256: row.cover_sha256,
          }
        : null,
    },
    provisioning: provisioningToken
      ? { buildId: row.id, token: provisioningToken, expiresAt: iso(tokenExpiresAt) }
      : null,
    callbacks: {
      artifact: `${new URL(request.url).origin}/api/agenda/android/internal/builds/${row.id}/artifact`,
      failure: `${new URL(request.url).origin}/api/agenda/android/internal/builds/${row.id}/failure`,
    },
  };
}

async function claimBuild(request: Request, row: BuildRow, workerId: string) {
  const now = Date.now();
  const result = await getAgendaD1()
    .prepare(
      `UPDATE agenda_android_builds
       SET status = 'building', worker_id = ?1, attempt_count = attempt_count + 1,
           error_code = NULL, error_message = NULL, started_at = ?2,
           completed_at = NULL, updated_at = ?2
       WHERE id = ?3 AND status IN ('queued', 'failed')`,
    )
    .bind(workerId, now, row.id)
    .run();
  if (Number(result.meta?.changes || 0) !== 1) {
    throw new AgendaAndroidError(409, "build_not_claimable", "A compilacao ja foi assumida por outro executor.");
  }

  const token = randomToken(36);
  const tokenHash = await sha256Hex(token);
  const expiresAt = now + PROVISIONING_TOKEN_DURATION_MS;
  try {
    const database = getAgendaD1();
    await database.batch([
      database
        .prepare(
          `UPDATE agenda_android_provisioning_tokens
           SET expires_at = ?1
           WHERE build_id = ?2 AND used_at IS NULL AND expires_at > ?1`,
        )
        .bind(now, row.id),
      database
        .prepare(
          `INSERT INTO agenda_android_provisioning_tokens (
             id, build_id, user_id, token_hash, expires_at, used_at,
             used_device_id, created_at
           ) VALUES (?1, ?2, ?3, ?4, ?5, NULL, NULL, ?6)`,
        )
        .bind(crypto.randomUUID(), row.id, row.user_id, tokenHash, expiresAt, now),
    ]);
  } catch (error) {
    await getAgendaD1()
      .prepare(
        `UPDATE agenda_android_builds
         SET status = 'queued', worker_id = NULL, updated_at = ?1
         WHERE id = ?2 AND status = 'building' AND worker_id = ?3`,
      )
      .bind(Date.now(), row.id, workerId)
      .run();
    throw error;
  }
  const claimed = await readBuild(row.id);
  if (!claimed) throw new Error("Claimed build disappeared");
  return jsonResponse({ ok: true, build: buildManifest(request, claimed, token, expiresAt) });
}

function workerIdFrom(request: Request, body?: JsonObject) {
  return safeIdentifier(
    body?.workerId || request.headers.get("x-agenda-worker-id") || "runner",
    "worker_id",
    100,
  );
}

export async function claimNextAgendaAndroidBuild(request: Request) {
  await requireSecret(request, "Builder", "AGENDA_ANDROID_BUILDER_SECRET");
  const body = await readJsonBody(request, true);
  const workerId = workerIdFrom(request, body);
  for (let attempt = 0; attempt < 3; attempt += 1) {
    const row = await getAgendaD1()
      .prepare("SELECT * FROM agenda_android_builds WHERE status = 'queued' ORDER BY created_at ASC LIMIT 1")
      .first<BuildRow>();
    if (!row) return new Response(null, { status: 204 });
    try {
      return await claimBuild(request, row, workerId);
    } catch (error) {
      if (!(error instanceof AgendaAndroidError) || error.code !== "build_not_claimable") throw error;
    }
  }
  return new Response(null, { status: 204 });
}

export async function getInternalAgendaAndroidBuild(request: Request, buildIdValue: string) {
  await requireSecret(request, "Builder", "AGENDA_ANDROID_BUILDER_SECRET");
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const row = await readBuild(buildId);
  if (!row) throw new AgendaAndroidError(404, "build_not_found", "Compilacao nao encontrada.");
  return jsonResponse({ ok: true, status: buildPublicState(row), build: buildManifest(request, row) });
}

export async function claimAgendaAndroidBuild(request: Request, buildIdValue: string) {
  await requireSecret(request, "Builder", "AGENDA_ANDROID_BUILDER_SECRET");
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const body = await readJsonBody(request, true);
  const row = await readBuild(buildId);
  if (!row) throw new AgendaAndroidError(404, "build_not_found", "Compilacao nao encontrada.");
  return claimBuild(request, row, workerIdFrom(request, body));
}

export async function getInternalAgendaAndroidBuildAsset(
  request: Request,
  buildIdValue: string,
  kindValue: string,
) {
  await requireSecret(request, "Builder", "AGENDA_ANDROID_BUILDER_SECRET");
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const kind = String(kindValue || "");
  if (kind !== "icon" && kind !== "cover") {
    throw new AgendaAndroidError(404, "asset_not_found", "Imagem nao encontrada.");
  }
  const row = await readBuild(buildId);
  if (!row) throw new AgendaAndroidError(404, "build_not_found", "Compilacao nao encontrada.");
  const objectKey = kind === "icon" ? row.icon_object_key : row.cover_object_key;
  const contentType = kind === "icon" ? row.icon_content_type : row.cover_content_type;
  if (!objectKey) throw new AgendaAndroidError(404, "asset_not_found", "Imagem nao encontrada.");
  return r2ObjectResponse(objectKey, contentType || "application/octet-stream");
}

async function r2ObjectResponse(objectKey: string, contentType: string) {
  const object = await getAgendaAndroidR2().get(objectKey);
  if (!object) throw new AgendaAndroidError(404, "asset_not_found", "Arquivo nao encontrado.");
  const headers = new Headers({
    "Cache-Control": "private, no-store, max-age=0",
    "Content-Type": contentType,
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  });
  if (object.size != null) headers.set("Content-Length", String(object.size));
  if (object.httpEtag) headers.set("ETag", object.httpEtag);
  return new Response(object.body, { headers });
}

export async function uploadInternalAgendaAndroidArtifact(request: Request, buildIdValue: string) {
  await requireSecret(request, "Builder", "AGENDA_ANDROID_BUILDER_SECRET");
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const row = await readBuild(buildId);
  if (!row || row.status !== "building") {
    throw new AgendaAndroidError(409, "build_not_uploadable", "Esta compilacao nao aceita um artefato agora.");
  }
  const declaredLength = Number(request.headers.get("content-length") || "0");
  if (!Number.isInteger(declaredLength) || declaredLength <= 0) {
    throw new AgendaAndroidError(411, "content_length_required", "Informe o tamanho do APK.");
  }
  if (declaredLength > MAX_APK_BYTES) {
    throw new AgendaAndroidError(413, "apk_too_large", "O APK ultrapassa o limite permitido.");
  }
  const sha256 = String(request.headers.get("x-agenda-apk-sha256") || "").trim().toLowerCase();
  if (!/^[a-f0-9]{64}$/.test(sha256)) {
    throw new AgendaAndroidError(400, "invalid_apk_sha256", "Informe o SHA-256 valido do APK.");
  }
  if (!request.body) throw new AgendaAndroidError(400, "apk_required", "Envie o APK gerado.");
  const fileName = `agenda-livre-${filenameSlug(row.app_name)}-${row.version_name}.apk`;
  const objectKey = `android/apk/${row.user_id}/${row.id}/${fileName}`;
  const contentType = "application/vnd.android.package-archive";
  await getAgendaAndroidR2().put(objectKey, request.body, {
    httpMetadata: { contentType, cacheControl: "private, max-age=0" },
    customMetadata: {
      sha256,
      buildId: row.id,
      versionCode: String(row.version_code),
    },
  });
  const now = Date.now();
  try {
    const result = await getAgendaD1()
      .prepare(
        `UPDATE agenda_android_builds
         SET status = 'ready', artifact_object_key = ?1, artifact_file_name = ?2,
             artifact_content_type = ?3, artifact_size = ?4, artifact_sha256 = ?5,
             completed_at = ?6, updated_at = ?6
         WHERE id = ?7 AND status = 'building'`,
      )
      .bind(objectKey, fileName, contentType, declaredLength, sha256, now, row.id)
      .run();
    if (Number(result.meta?.changes || 0) !== 1) {
      throw new AgendaAndroidError(409, "build_not_uploadable", "A compilacao mudou antes do envio terminar.");
    }
  } catch (error) {
    try {
      await getAgendaAndroidR2().delete(objectKey);
    } catch (cleanupError) {
      console.error("Could not clean rejected Android artifact", cleanupError);
    }
    throw error;
  }
  return jsonResponse({
    ok: true,
    build: { id: row.id, status: "ready" },
    artifact: { fileName, size: declaredLength, sha256 },
  });
}

export async function failInternalAgendaAndroidBuild(request: Request, buildIdValue: string) {
  await requireSecret(request, "Builder", "AGENDA_ANDROID_BUILDER_SECRET");
  const buildId = safeIdentifier(buildIdValue, "build_id");
  const body = await readJsonBody(request);
  const code = safeIdentifier(body.code || "build_failed", "error_code", 80);
  const message = safeText(body.message || "A compilacao do aplicativo falhou.", "error_message", 500);
  const result = await getAgendaD1()
    .prepare(
      `UPDATE agenda_android_builds
       SET status = 'failed', error_code = ?1, error_message = ?2,
           completed_at = ?3, updated_at = ?3
       WHERE id = ?4 AND status = 'building'`,
    )
    .bind(code, message, Date.now(), buildId)
    .run();
  if (Number(result.meta?.changes || 0) !== 1) {
    throw new AgendaAndroidError(409, "build_not_fail-able", "A compilacao nao esta em andamento.");
  }
  return jsonResponse({ ok: true, build: { id: buildId, status: "failed" } });
}

async function startTrialIfNeeded(userId: string, now: number) {
  await ensurePendingEntitlement(userId, now);
  await getAgendaD1()
    .prepare(
      `UPDATE agenda_android_entitlements
       SET status = 'trialing', trial_started_at = ?1, trial_ends_at = ?2,
           updated_at = ?1
       WHERE user_id = ?3 AND status = 'pending_activation' AND trial_started_at IS NULL`,
    )
    .bind(now, now + TRIAL_DURATION_MS, userId)
    .run();
}

async function issueDeviceSession(deviceId: string, userId: string, now: number) {
  const token = randomToken(36);
  const tokenHash = await sha256Hex(token);
  const sessionId = crypto.randomUUID();
  const expiresAt = now + DEVICE_SESSION_DURATION_MS;
  await getAgendaD1()
    .prepare(
      `INSERT INTO agenda_android_sessions (
         id, device_id, user_id, token_hash, expires_at, revoked_at,
         last_seen_at, created_at, updated_at
       ) VALUES (?1, ?2, ?3, ?4, ?5, NULL, ?6, ?6, ?6)`,
    )
    .bind(sessionId, deviceId, userId, tokenHash, expiresAt, now)
    .run();
  return { id: sessionId, token, expiresAt };
}

function brandingPayload(request: Request, build: BuildRow) {
  const origin = new URL(request.url).origin;
  return {
    businessName: build.app_name,
    logoUrl: `${origin}/api/agenda/android/branding/icon`,
    coverUrl: build.cover_object_key
      ? `${origin}/api/agenda/android/branding/cover`
      : null,
  };
}

export async function redeemAgendaAndroidProvisioning(request: Request) {
  const body = await readJsonBody(request);
  const buildId = safeIdentifier(body.buildId, "build_id");
  const provisioningToken = safeIdentifier(body.provisioningToken, "provisioning_token", 512);
  const devicePublicId = safeIdentifier(body.deviceId, "device_id", 128);
  const platform = safeIdentifier(body.platform || "android", "platform", 24).toLowerCase();
  if (platform !== "android") {
    throw new AgendaAndroidError(400, "invalid_platform", "Esta ativacao e exclusiva para Android.");
  }
  const appVersion = safeText(body.appVersion || "unknown", "app_version", 64);
  const tokenHash = await sha256Hex(provisioningToken);
  const now = Date.now();
  const tokenRow = await getAgendaD1()
    .prepare(
      `SELECT p.id, p.user_id, p.expires_at, p.used_at, p.used_device_id,
              b.status AS build_status
       FROM agenda_android_provisioning_tokens p
       JOIN agenda_android_builds b ON b.id = p.build_id
       WHERE p.build_id = ?1 AND p.token_hash = ?2
       LIMIT 1`,
    )
    .bind(buildId, tokenHash)
    .first<{
      id: string;
      user_id: string;
      expires_at: number;
      used_at: number | null;
      used_device_id: string | null;
      build_status: string;
    }>();
  if (!tokenRow || tokenRow.expires_at <= now || tokenRow.build_status !== "ready") {
    throw new AgendaAndroidError(401, "invalid_provisioning", "A ativacao expirou ou ja foi utilizada.");
  }
  const replayForSameDevice = tokenRow.used_at !== null;
  if (replayForSameDevice && tokenRow.used_device_id !== devicePublicId) {
    throw new AgendaAndroidError(401, "invalid_provisioning", "A ativacao ja foi utilizada em outro aparelho.");
  }
  const build = await readBuild(buildId);
  if (!build) throw new AgendaAndroidError(404, "build_not_found", "Aplicativo nao encontrado.");

  if (!replayForSameDevice) {
    const claimed = await getAgendaD1()
      .prepare(
        `UPDATE agenda_android_provisioning_tokens
         SET used_at = ?1, used_device_id = ?2
         WHERE id = ?3 AND used_at IS NULL AND expires_at > ?1`,
      )
      .bind(now, devicePublicId, tokenRow.id)
      .run();
    if (Number(claimed.meta?.changes || 0) !== 1) {
      throw new AgendaAndroidError(409, "provisioning_already_used", "Esta ativacao ja foi utilizada.");
    }
  }

  let issuedSession: { id: string; token: string; expiresAt: number } | null = null;
  try {
    const device = await getAgendaD1()
      .prepare(
        "SELECT id FROM agenda_android_devices WHERE user_id = ?1 AND device_public_id = ?2 LIMIT 1",
      )
      .bind(tokenRow.user_id, devicePublicId)
      .first<{ id: string }>();
    const deviceId = device?.id || crypto.randomUUID();
    const database = getAgendaD1();
    await database.batch([
      database
        .prepare(
          `INSERT INTO agenda_android_devices (
             id, user_id, build_id, device_public_id, platform, app_version,
             revoked_at, last_seen_at, created_at, updated_at
           ) VALUES (?1, ?2, ?3, ?4, 'android', ?5, NULL, ?6, ?6, ?6)
           ON CONFLICT(user_id, device_public_id) DO UPDATE SET
             build_id = excluded.build_id, platform = excluded.platform,
             app_version = excluded.app_version, revoked_at = NULL,
             last_seen_at = excluded.last_seen_at, updated_at = excluded.updated_at`,
        )
        .bind(deviceId, tokenRow.user_id, buildId, devicePublicId, appVersion, now),
      database
        .prepare(
          `UPDATE agenda_android_sessions
           SET revoked_at = ?1, updated_at = ?1
           WHERE device_id = ?2 AND revoked_at IS NULL`,
        )
        .bind(now, deviceId),
    ]);
    issuedSession = await issueDeviceSession(deviceId, tokenRow.user_id, now);
    await startTrialIfNeeded(tokenRow.user_id, now);
    const entitlement = await entitlementState(
      await readEntitlement(tokenRow.user_id),
      tokenRow.user_id,
      issuedSession.id,
      now,
    );
    return jsonResponse({
      ok: true,
      device: {
        id: deviceId,
        token: issuedSession.token,
        expiresAt: iso(issuedSession.expiresAt),
      },
      account: { id: tokenRow.user_id },
      branding: brandingPayload(request, build),
      entitlement,
    });
  } catch (error) {
    const database = getAgendaD1();
    const compensation: D1PreparedStatement[] = [];
    if (issuedSession) {
      compensation.push(
        database
          .prepare(
            `UPDATE agenda_android_sessions
             SET revoked_at = ?1, updated_at = ?1
             WHERE id = ?2 AND revoked_at IS NULL`,
          )
          .bind(Date.now(), issuedSession.id),
      );
    }
    if (!replayForSameDevice) {
      compensation.push(
        database
          .prepare(
            `UPDATE agenda_android_provisioning_tokens
             SET used_at = NULL, used_device_id = NULL
             WHERE id = ?1 AND used_at = ?2 AND used_device_id = ?3`,
          )
          .bind(tokenRow.id, now, devicePublicId),
      );
    }
    try {
      if (compensation.length) await database.batch(compensation);
    } catch (compensationError) {
      console.error("Could not compensate failed Android provisioning", compensationError);
    }
    throw error;
  }
}

export async function refreshAgendaAndroidSession(request: Request) {
  const actor = await authenticateAgendaAndroidDevice(request);
  const body = await readJsonBody(request, true);
  const appVersion = body.appVersion
    ? safeText(body.appVersion, "app_version", 64)
    : null;
  const now = Date.now();
  const nextToken = randomToken(36);
  const nextHash = await sha256Hex(nextToken);
  const nextSessionId = crypto.randomUUID();
  const expiresAt = now + DEVICE_SESSION_DURATION_MS;
  const database = getAgendaD1();
  await database.batch([
    database
      .prepare(
        `INSERT INTO agenda_android_sessions (
           id, device_id, user_id, token_hash, expires_at, revoked_at,
           last_seen_at, created_at, updated_at
         ) VALUES (?1, ?2, ?3, ?4, ?5, NULL, ?6, ?6, ?6)`,
      )
      .bind(nextSessionId, actor.deviceId, actor.userId, nextHash, expiresAt, now),
    database
      .prepare(
        `UPDATE agenda_android_sessions SET revoked_at = ?1, updated_at = ?1
         WHERE device_id = ?2 AND id <> ?3 AND revoked_at IS NULL`,
      )
      .bind(now, actor.deviceId, nextSessionId),
    database
      .prepare(
        `UPDATE agenda_android_devices
         SET app_version = COALESCE(?1, app_version), last_seen_at = ?2, updated_at = ?2
         WHERE id = ?3`,
      )
      .bind(appVersion, now, actor.deviceId),
  ]);
  const build = await readBuild(actor.buildId);
  if (!build) throw new AgendaAndroidError(404, "build_not_found", "A configuracao deste aplicativo nao foi encontrada.");
  const entitlement = await entitlementState(
    await readEntitlement(actor.userId),
    actor.userId,
    nextSessionId,
    now,
  );
  return jsonResponse({
    ok: true,
    device: { id: actor.deviceId, token: nextToken, expiresAt: iso(expiresAt) },
    account: { id: actor.userId },
    branding: brandingPayload(request, build),
    entitlement,
  });
}

export async function revokeAgendaAndroidSession(request: Request) {
  const actor = await authenticateAgendaAndroidDevice(request);
  await getAgendaD1()
    .prepare("UPDATE agenda_android_sessions SET revoked_at = ?1, updated_at = ?1 WHERE id = ?2")
    .bind(Date.now(), actor.sessionId)
    .run();
  return jsonResponse({ ok: true });
}

export async function getAgendaAndroidEntitlement(request: Request) {
  const actor = await authenticateAccountOrDevice(request);
  await ensurePendingEntitlement(actor.userId);
  return jsonResponse({
    ok: true,
    account: { id: actor.userId },
    entitlement: await entitlementState(
      await readEntitlement(actor.userId),
      actor.userId,
      actor.kind === "device" ? actor.sessionId : undefined,
    ),
  });
}

export async function getAgendaAndroidBrandingAsset(request: Request, kindValue: string) {
  const actor = await authenticateAgendaAndroidDevice(request);
  const kind = String(kindValue || "");
  if (kind !== "icon" && kind !== "cover") {
    throw new AgendaAndroidError(404, "asset_not_found", "Imagem nao encontrada.");
  }
  const build = await readBuild(actor.buildId);
  if (!build) throw new AgendaAndroidError(404, "build_not_found", "Aplicativo nao encontrado.");
  const key = kind === "icon" ? build.icon_object_key : build.cover_object_key;
  const type = kind === "icon" ? build.icon_content_type : build.cover_content_type;
  if (!key) throw new AgendaAndroidError(404, "asset_not_found", "Imagem nao encontrada.");
  return r2ObjectResponse(key, type || "application/octet-stream");
}

const BILLING_STATUSES = new Set([
  "pending_activation",
  "trialing",
  "active",
  "past_due",
  "canceled",
  "suspended",
]);

export async function updateInternalAgendaAndroidEntitlement(
  request: Request,
  userIdValue: string,
) {
  await requireSecret(request, "Billing", "AGENDA_ANDROID_BILLING_SECRET");
  const userId = safeIdentifier(userIdValue, "user_id");
  const account = await getAgendaD1()
    .prepare("SELECT user_id FROM agenda_cloud_accounts WHERE user_id = ?1 LIMIT 1")
    .bind(userId)
    .first<{ user_id: string }>();
  if (!account) throw new AgendaAndroidError(404, "account_not_found", "Conta nao encontrada.");
  const body = await readJsonBody(request);
  const status = String(body.status || "").trim().toLowerCase();
  if (!BILLING_STATUSES.has(status)) {
    throw new AgendaAndroidError(400, "invalid_status", "O status da assinatura e invalido.");
  }
  const eventId = safeIdentifier(body.eventId, "event_id", 160);
  const eventAt = timestamp(body.eventAt, "event_at", false) as number;
  const currentPeriodEndsAt = timestamp(body.currentPeriodEndsAt, "current_period_ends_at");
  const graceEndsAt = timestamp(body.graceEndsAt, "grace_ends_at");
  const paymentUrl = body.paymentUrl === undefined ? undefined : optionalUrl(body.paymentUrl, "payment_url");
  const supportUrl = body.supportUrl === undefined ? undefined : optionalUrl(body.supportUrl, "support_url");
  const provider = body.provider ? safeIdentifier(body.provider, "provider", 40) : null;
  const customerId = body.customerId ? safeIdentifier(body.customerId, "customer_id", 160) : null;
  const subscriptionId = body.subscriptionId
    ? safeIdentifier(body.subscriptionId, "subscription_id", 160)
    : null;
  const now = Date.now();
  await ensurePendingEntitlement(userId, now);
  const existing = await readEntitlement(userId);
  if (existing?.provider_event_id === eventId) {
    return jsonResponse({
      ok: true,
      idempotent: true,
      entitlement: await entitlementState(existing, userId),
    });
  }
  if (existing?.provider_event_at != null && eventAt < existing.provider_event_at) {
    return jsonResponse({
      ok: true,
      ignoredAsStale: true,
      entitlement: await entitlementState(existing, userId),
    });
  }

  await getAgendaD1()
    .prepare(
      `UPDATE agenda_android_entitlements SET
         status = ?1,
         current_period_ends_at = ?2,
         grace_ends_at = ?3,
         payment_url = CASE WHEN ?4 = 1 THEN ?5 ELSE payment_url END,
         support_url = CASE WHEN ?6 = 1 THEN ?7 ELSE support_url END,
         provider = COALESCE(?8, provider),
         provider_customer_id = COALESCE(?9, provider_customer_id),
         provider_subscription_id = COALESCE(?10, provider_subscription_id),
         provider_event_id = ?11,
         provider_event_at = ?12,
         updated_at = ?13
       WHERE user_id = ?14`,
    )
    .bind(
      status,
      currentPeriodEndsAt,
      graceEndsAt,
      paymentUrl === undefined ? 0 : 1,
      paymentUrl ?? null,
      supportUrl === undefined ? 0 : 1,
      supportUrl ?? null,
      provider,
      customerId,
      subscriptionId,
      eventId,
      eventAt,
      now,
      userId,
    )
    .run();
  const updated = await readEntitlement(userId);
  return jsonResponse({ ok: true, entitlement: await entitlementState(updated, userId) });
}

type BillingUpdate = {
  userId: string;
  status: string;
  eventId: string;
  eventAt: number;
  currentPeriodEndsAt: number | null;
  graceEndsAt?: number | null;
  providerCustomerId?: string | null;
  providerSubscriptionId?: string | null;
};

async function applyStripeBillingUpdate(update: BillingUpdate) {
  if (!BILLING_STATUSES.has(update.status)) {
    throw new AgendaAndroidError(400, "invalid_status", "O status da assinatura e invalido.");
  }
  const account = await getAgendaD1()
    .prepare("SELECT user_id FROM agenda_cloud_accounts WHERE user_id = ?1 LIMIT 1")
    .bind(update.userId)
    .first<{ user_id: string }>();
  if (!account) return { applied: false, outcome: "account_not_found" };
  await ensurePendingEntitlement(update.userId);
  const existing = await readEntitlement(update.userId);
  if (existing?.provider_event_id === update.eventId) {
    return { applied: false, outcome: "duplicate" };
  }
  if (existing?.provider_event_at != null && update.eventAt < existing.provider_event_at) {
    return { applied: false, outcome: "stale" };
  }
  await getAgendaD1()
    .prepare(
      `UPDATE agenda_android_entitlements SET
         status = ?1,
         current_period_ends_at = ?2,
         grace_ends_at = ?3,
         provider = 'stripe',
         provider_customer_id = COALESCE(?4, provider_customer_id),
         provider_subscription_id = COALESCE(?5, provider_subscription_id),
         provider_event_id = ?6,
         provider_event_at = ?7,
         updated_at = ?8
       WHERE user_id = ?9`,
    )
    .bind(
      update.status,
      update.currentPeriodEndsAt,
      update.graceEndsAt ?? null,
      update.providerCustomerId ?? null,
      update.providerSubscriptionId ?? null,
      update.eventId,
      update.eventAt,
      Date.now(),
      update.userId,
    )
    .run();
  return { applied: true, outcome: "applied" };
}

function checkoutUrlFromEnvironment(name: string, fallback: string) {
  const configured = runtimeValue(name);
  if (!configured) return fallback;
  try {
    const parsed = new URL(configured);
    if (parsed.protocol !== "https:" && parsed.protocol !== "http:") throw new Error();
    return parsed.toString();
  } catch {
    throw new AgendaAndroidError(503, "checkout_not_configured", `A variavel ${name} e invalida.`);
  }
}

export async function createAgendaAndroidCheckout(request: Request) {
  const user = await authenticateAccountOrDevice(request);
  if (user.kind === "supabase") await ensureCloudAccount(user);
  await ensurePendingEntitlement(user.userId);
  const body = await readJsonBody(request, true);
  const plan = String(body.plan || "mensal").trim().toLowerCase();
  if (plan !== "mensal" && plan !== "anual") {
    throw new AgendaAndroidError(
      400,
      "invalid_checkout_plan",
      "Escolha o plano mensal ou anual.",
    );
  }
  const secretKey = runtimeValue("STRIPE_SECRET_KEY");
  const priceId =
    runtimeValue(
      plan === "anual"
        ? "AGENDA_STRIPE_PRICE_ANUAL"
        : "AGENDA_STRIPE_PRICE_MENSAL",
    ) ||
    (plan === "mensal" ? runtimeValue("AGENDA_ANDROID_STRIPE_PRICE_ID") : "");
  if (!secretKey || !priceId) {
    throw new AgendaAndroidError(
      503,
      "checkout_not_configured",
      `O pagamento ${plan} do Agenda Livre ainda nao foi configurado.`,
    );
  }
  if (!/^price_[A-Za-z0-9_]+$/.test(priceId)) {
    throw new AgendaAndroidError(503, "checkout_not_configured", "O plano configurado no servidor e invalido.");
  }
  const requestKey = body.idempotencyKey
    ? safeIdentifier(body.idempotencyKey, "idempotency_key", 100)
    : crypto.randomUUID();
  const origin = new URL(request.url).origin;
  const successUrl = checkoutUrlFromEnvironment(
    "AGENDA_ANDROID_CHECKOUT_SUCCESS_URL",
    `${origin}/?checkout=sucesso&session_id={CHECKOUT_SESSION_ID}`,
  );
  const cancelUrl = checkoutUrlFromEnvironment(
    "AGENDA_ANDROID_CHECKOUT_CANCEL_URL",
    `${origin}/?checkout=cancelado`,
  );
  const params = new URLSearchParams({
    mode: "subscription",
    "line_items[0][price]": priceId,
    "line_items[0][quantity]": "1",
    allow_promotion_codes: "true",
    client_reference_id: user.userId,
    "metadata[agenda_user_id]": user.userId,
    "metadata[agenda_product]": "agenda_livre",
    "metadata[agenda_plan]": plan,
    "subscription_data[metadata][agenda_user_id]": user.userId,
    "subscription_data[metadata][agenda_product]": "agenda_livre",
    "subscription_data[metadata][agenda_plan]": plan,
    success_url: successUrl,
    cancel_url: cancelUrl,
  });
  const entitlement = await readEntitlement(user.userId);
  if (entitlement?.provider_customer_id) {
    params.set("customer", entitlement.provider_customer_id);
  } else if (user.email) {
    params.set("customer_email", user.email);
  }
  const response = await fetch("https://api.stripe.com/v1/checkout/sessions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${secretKey}`,
      "Content-Type": "application/x-www-form-urlencoded",
      "Idempotency-Key": `agenda-${plan}-${user.userId}-${requestKey}`.slice(0, 255),
    },
    body: params,
    cache: "no-store",
  });
  let data: Record<string, unknown> = {};
  try {
    data = (await response.json()) as Record<string, unknown>;
  } catch {
    // The generic error below intentionally hides provider internals.
  }
  const url = typeof data.url === "string" ? data.url : "";
  if (!response.ok || !url) {
    console.error("Agenda Android Stripe checkout failed", response.status);
    throw new AgendaAndroidError(502, "checkout_unavailable", "Nao foi possivel abrir o pagamento agora.");
  }
  return jsonResponse({ ok: true, checkout: { url } }, 200, "POST, OPTIONS");
}

async function stripeSignatureHex(secret: string, value: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  return bytesToHex(
    await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(value)),
  );
}

async function verifyStripeSignature(payload: string, header: string, secret: string) {
  const values = header.split(",").map((part) => part.trim().split("=", 2));
  const timestampText = values.find(([key]) => key === "t")?.[1] || "";
  const signatures = values.filter(([key]) => key === "v1").map(([, value]) => value || "");
  const timestampSeconds = Number(timestampText);
  if (!Number.isInteger(timestampSeconds) || signatures.length === 0) return false;
  if (Math.abs(Date.now() - timestampSeconds * 1000) > 5 * 60 * 1000) return false;
  const expected = await stripeSignatureHex(secret, `${timestampText}.${payload}`);
  for (const signature of signatures) {
    if (await secretsMatch(signature.toLowerCase(), expected.toLowerCase())) return true;
  }
  return false;
}

async function stripeGet(path: string) {
  const secretKey = runtimeValue("STRIPE_SECRET_KEY");
  if (!secretKey) throw new AgendaAndroidError(503, "stripe_not_configured", "O pagamento nao foi configurado.");
  const response = await fetch(`https://api.stripe.com${path}`, {
    headers: { Authorization: `Bearer ${secretKey}` },
    cache: "no-store",
  });
  let data: JsonObject = {};
  try {
    data = (await response.json()) as JsonObject;
  } catch {
    // The error below is intentionally provider-neutral.
  }
  if (!response.ok) {
    throw new AgendaAndroidError(502, "stripe_unavailable", "Nao foi possivel confirmar a assinatura agora.");
  }
  return data;
}

function objectValue(value: unknown): JsonObject {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as JsonObject)
    : {};
}

function metadataUserId(value: unknown) {
  const metadata = objectValue(value);
  const candidate = String(metadata.agenda_user_id || "").trim();
  return /^[A-Za-z0-9_-]{8,128}$/.test(candidate) ? candidate : "";
}

function metadataClaimId(value: unknown) {
  const metadata = objectValue(value);
  const candidate = String(metadata.agenda_claim_id || "").trim();
  return /^[A-Za-z0-9_-]{8,128}$/.test(candidate) ? candidate : "";
}

function maskedCheckoutEmail(value: unknown) {
  const email = String(value || "").trim().toLowerCase();
  const at = email.indexOf("@");
  if (at <= 0 || at === email.length - 1) return null;
  const local = email.slice(0, at);
  return `${local.slice(0, Math.min(2, local.length))}${local.length > 2 ? "***" : "*"}${email.slice(at)}`;
}

function stripeId(value: unknown) {
  if (typeof value === "string") return value;
  const object = objectValue(value);
  return typeof object.id === "string" ? object.id : "";
}

function stripeSeconds(value: unknown) {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric > 0 ? Math.trunc(numeric * 1000) : null;
}

async function stripeSubscriptionContext(object: JsonObject) {
  const directSubscription = objectValue(object.subscription);
  const parent = objectValue(object.parent);
  const subscriptionDetails = objectValue(parent.subscription_details);
  const subscriptionId =
    stripeId(object.subscription) ||
    stripeId(subscriptionDetails.subscription) ||
    (String(object.object || "") === "subscription" ? stripeId(object) : "");
  let subscription = Object.keys(directSubscription).length ? directSubscription : {};
  if (!Object.keys(subscription).length && String(object.object || "") === "subscription") {
    subscription = object;
  }
  if (subscriptionId && !Object.keys(subscription).length) {
    subscription = await stripeGet(`/v1/subscriptions/${encodeURIComponent(subscriptionId)}`);
  }
  const userId =
    metadataUserId(object.metadata) ||
    metadataUserId(subscriptionDetails.metadata) ||
    metadataUserId(subscription.metadata);
  const customerId = stripeId(object.customer) || stripeId(subscription.customer);
  let periodEnd = stripeSeconds(subscription.current_period_end);
  if (!periodEnd) {
    const itemsData = objectValue(subscription.items).data;
    if (Array.isArray(itemsData) && itemsData.length > 0) {
      periodEnd = stripeSeconds(objectValue(itemsData[0]).current_period_end);
    }
  }
  if (!periodEnd) {
    const linesData = objectValue(object.lines).data;
    if (Array.isArray(linesData) && linesData.length > 0) {
      periodEnd = stripeSeconds(objectValue(objectValue(linesData[0]).period).end);
    }
  }
  return { userId, subscriptionId, customerId, periodEnd, subscription };
}

function agendaStatusFromStripe(value: unknown) {
  const status = String(value || "").toLowerCase();
  if (status === "active") return "active";
  if (status === "trialing") return "trialing";
  if (status === "canceled") return "canceled";
  if (status === "paused") return "suspended";
  return "past_due";
}

export async function handleAgendaAndroidStripeWebhook(request: Request) {
  const webhookSecret =
    runtimeValue("AGENDA_STRIPE_WEBHOOK_SECRET") ||
    runtimeValue("AGENDA_ANDROID_STRIPE_WEBHOOK_SECRET");
  if (!webhookSecret) {
    throw new AgendaAndroidError(503, "webhook_not_configured", "O webhook do pagamento nao foi configurado.");
  }
  const declaredLength = Number(request.headers.get("content-length") || "0");
  if (Number.isFinite(declaredLength) && declaredLength > 1024 * 1024) {
    throw new AgendaAndroidError(413, "payload_too_large", "O evento ultrapassa o limite permitido.");
  }
  const raw = await request.text();
  if (!raw || new TextEncoder().encode(raw).byteLength > 1024 * 1024) {
    throw new AgendaAndroidError(400, "invalid_event", "O evento enviado e invalido.");
  }
  const signature = request.headers.get("stripe-signature") || "";
  if (!(await verifyStripeSignature(raw, signature, webhookSecret))) {
    throw new AgendaAndroidError(400, "invalid_signature", "A assinatura do evento e invalida.");
  }
  let event: JsonObject;
  try {
    event = JSON.parse(raw) as JsonObject;
  } catch {
    throw new AgendaAndroidError(400, "invalid_event", "O evento enviado e invalido.");
  }
  const eventId = safeIdentifier(event.id, "event_id", 160);
  const eventType = safeText(event.type, "event_type", 100);
  const eventAt = stripeSeconds(event.created) || Date.now();
  const payloadSha256 = await sha256Hex(raw);
  const previous = await getAgendaD1()
    .prepare("SELECT event_id FROM agenda_android_billing_events WHERE event_id = ?1 LIMIT 1")
    .bind(eventId)
    .first<{ event_id: string }>();
  if (previous) return jsonResponse({ received: true, duplicate: true });

  const object = objectValue(objectValue(event.data).object);
  let userId = "";
  let outcome = "ignored";
  let update: BillingUpdate | null = null;
  if (
    eventType === "checkout.session.completed" ||
    eventType === "checkout.session.async_payment_succeeded"
  ) {
    if (
      String(object.payment_status || "") === "paid" ||
      String(object.payment_status || "") === "no_payment_required"
    ) {
      const context = await stripeSubscriptionContext(object);
      const metadata = objectValue(object.metadata);
      const claimId = metadataClaimId(metadata);
      if (claimId) {
        const sessionId = stripeId(object);
        const plan = String(metadata.agenda_plan || "mensal").toLowerCase() === "anual"
          ? "anual"
          : "mensal";
        const checkoutStatus = agendaStatusFromStripe(context.subscription.status);
        const customerDetails = objectValue(object.customer_details);
        const emailMasked = maskedCheckoutEmail(
          customerDetails.email || object.customer_email,
        );
        const now = Date.now();
        await getAgendaD1()
          .prepare(
            `INSERT INTO agenda_subscription_claims (
               claim_id, checkout_session_id, provider_customer_id,
               provider_subscription_id, plan, status, checkout_email_masked,
               current_period_ends_at, provider_event_id, provider_event_at,
               created_at, updated_at
             ) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?11)
             ON CONFLICT(claim_id) DO UPDATE SET
               provider_customer_id = COALESCE(excluded.provider_customer_id, provider_customer_id),
               provider_subscription_id = COALESCE(excluded.provider_subscription_id, provider_subscription_id),
               status = excluded.status,
               checkout_email_masked = COALESCE(excluded.checkout_email_masked, checkout_email_masked),
               current_period_ends_at = COALESCE(excluded.current_period_ends_at, current_period_ends_at),
               provider_event_id = excluded.provider_event_id,
               provider_event_at = excluded.provider_event_at,
               updated_at = excluded.updated_at`,
          )
          .bind(
            claimId,
            sessionId,
            context.customerId || null,
            context.subscriptionId || null,
            plan,
            checkoutStatus,
            emailMasked,
            context.periodEnd,
            eventId,
            eventAt,
            now,
          )
          .run();
        const claim = await getAgendaD1()
          .prepare(
            "SELECT user_id FROM agenda_subscription_claims WHERE claim_id = ?1 LIMIT 1",
          )
          .bind(claimId)
          .first<{ user_id: string | null }>();
        userId = claim?.user_id || "";
      } else {
        userId =
          metadataUserId(metadata) ||
          String(object.client_reference_id || "") ||
          context.userId;
      }
      if (/^[A-Za-z0-9_-]{8,128}$/.test(userId)) {
        update = {
          userId,
          status: agendaStatusFromStripe(context.subscription.status),
          eventId,
          eventAt,
          currentPeriodEndsAt: context.periodEnd,
          providerCustomerId: context.customerId,
          providerSubscriptionId: context.subscriptionId,
        };
      }
    }
  } else if (eventType === "invoice.paid" || eventType === "invoice.payment_failed") {
    const context = await stripeSubscriptionContext(object);
    userId = context.userId;
    if (userId) {
      update = {
        userId,
        status: eventType === "invoice.paid" ? "active" : "past_due",
        eventId,
        eventAt,
        currentPeriodEndsAt: context.periodEnd,
        providerCustomerId: context.customerId,
        providerSubscriptionId: context.subscriptionId,
      };
    }
  } else if (
    eventType === "customer.subscription.updated" ||
    eventType === "customer.subscription.deleted"
  ) {
    const context = await stripeSubscriptionContext(object);
    userId = context.userId;
    if (userId) {
      update = {
        userId,
        status:
          eventType === "customer.subscription.deleted"
            ? "canceled"
            : agendaStatusFromStripe(object.status),
        eventId,
        eventAt,
        currentPeriodEndsAt: context.periodEnd,
        providerCustomerId: context.customerId,
        providerSubscriptionId: context.subscriptionId,
      };
    }
  }

  if (update) {
    const result = await applyStripeBillingUpdate(update);
    outcome = result.outcome;
  } else if (eventType.startsWith("checkout.") || eventType.startsWith("invoice.") || eventType.startsWith("customer.subscription.")) {
    outcome = "not_agenda_android";
  }
  await getAgendaD1()
    .prepare(
      `INSERT OR IGNORE INTO agenda_android_billing_events (
         event_id, event_type, user_id, payload_sha256, outcome, created_at, processed_at
       ) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7)`,
    )
    .bind(eventId, eventType, userId || null, payloadSha256, outcome, eventAt, Date.now())
    .run();
  return jsonResponse({ received: true });
}
