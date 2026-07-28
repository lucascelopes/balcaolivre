import { env } from "cloudflare:workers";
import { getAgendaD1 } from "../../db/index";
import {
  AgendaAndroidError,
  type AgendaEntitlementState,
  assertAgendaAndroidCanUse,
  authenticateAgendaAndroidDevice,
  ensureAgendaEntitlementForUser,
} from "./agenda-android-server";

const DEFAULT_SUPABASE_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co";
const DEFAULT_SUPABASE_PUBLISHABLE_KEY =
  "sb_publishable_qNl5_EGAeuhN6PqTzRIeyQ_YQV2MdV6";
const ACCOUNT_STATE_SYNC_URL = "/api/agenda/account/state";
const DEFAULT_SCHEMA_VERSION = 1;
const TRIAL_DURATION_MS = 7 * 24 * 60 * 60 * 1000;
const MAX_REQUEST_BYTES = 2_000_000;
const MAX_PAYLOAD_BYTES = 1_900_000;
const MAX_JSON_DEPTH = 64;

type JsonObject = Record<string, unknown>;

type SupabaseUser = {
  id?: unknown;
  email?: unknown;
};

export type AuthenticatedAgendaUser = {
  id: string;
  email: string;
  kind: "supabase" | "device";
};

type AccountRow = {
  user_id: string;
  email: string;
  payload_json: string | null;
  revision: number;
  schema_version: number;
  trial_started_at: number;
  trial_ends_at: number;
  last_device_id: string;
  created_at: number;
  updated_at: number;
};

type AccountState = {
  exists: boolean;
  revision: number;
  schemaVersion: number;
  payload: JsonObject | null;
  updatedAt: string;
  trial: {
    startedAt: string;
    endsAt: string;
    active: boolean;
    daysRemaining: number;
  };
  entitlement: AgendaEntitlementState;
};

export class AgendaAccountError extends Error {
  readonly status: number;
  readonly code: string;
  readonly details?: unknown;

  constructor(status: number, code: string, message: string, details?: unknown) {
    super(message);
    this.name = "AgendaAccountError";
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
    DEFAULT_SUPABASE_URL
  ).replace(/\/+$/, "");
}

function supabasePublishableKey() {
  return (
    runtimeValue("BALCAO_SUPABASE_PUBLISHABLE_KEY") ||
    runtimeValue("SUPABASE_PUBLISHABLE_KEY") ||
    DEFAULT_SUPABASE_PUBLISHABLE_KEY
  );
}

export function agendaAccountConfig() {
  return {
    supabaseUrl: supabaseUrl(),
    publishableKey: supabasePublishableKey(),
    syncUrl: ACCOUNT_STATE_SYNC_URL,
  };
}

function corsHeaders(methods = "GET, PUT, OPTIONS") {
  return {
    "Access-Control-Allow-Headers": "Authorization, Content-Type",
    "Access-Control-Allow-Methods": methods,
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Max-Age": "86400",
    "Cache-Control": "no-store, max-age=0",
    "Content-Type": "application/json; charset=utf-8",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  };
}

function jsonResponse(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: corsHeaders(),
  });
}

export function agendaAccountOptionsResponse(
  methods = "GET, PUT, OPTIONS",
) {
  const headers = corsHeaders(methods);
  delete (headers as Partial<typeof headers>)["Content-Type"];
  return new Response(null, { status: 204, headers });
}

export function agendaAccountConfigResponse() {
  return new Response(JSON.stringify(agendaAccountConfig()), {
    status: 200,
    headers: {
      ...corsHeaders("GET, OPTIONS"),
      "Cache-Control": "public, max-age=300",
    },
  });
}

export function agendaAccountErrorResponse(error: unknown) {
  if (error instanceof AgendaAccountError) {
    const entitlement =
      error.status === 402 &&
      error.details &&
      typeof error.details === "object" &&
      "entitlement" in error.details
        ? (error.details as { entitlement: unknown }).entitlement
        : undefined;
    return jsonResponse(
      {
        ok: false,
        error: {
          code: error.code,
          message: error.message,
          ...(error.details === undefined ? {} : { details: error.details }),
        },
        ...(entitlement === undefined ? {} : { entitlement }),
      },
      error.status,
    );
  }

  console.error("Agenda account route failed", error);
  return jsonResponse(
    {
      ok: false,
      error: {
        code: "internal_error",
        message: "Não foi possível acessar a conta agora.",
      },
    },
    500,
  );
}

async function authenticateSupabaseUser(request: Request): Promise<AuthenticatedAgendaUser> {
  const authorization = request.headers.get("authorization") || "";
  const match = authorization.match(/^Bearer\s+([^\s]+)$/i);
  const accessToken = match?.[1] || "";
  if (!accessToken || accessToken.length > 8192) {
    throw new AgendaAccountError(
      401,
      "unauthorized",
      "Entre na sua conta para continuar.",
    );
  }

  let response: Response;
  try {
    response = await fetch(`${supabaseUrl()}/auth/v1/user`, {
      method: "GET",
      headers: {
        apikey: supabasePublishableKey(),
        Authorization: `Bearer ${accessToken}`,
      },
      cache: "no-store",
    });
  } catch {
    throw new AgendaAccountError(
      503,
      "auth_unavailable",
      "O login está temporariamente indisponível.",
    );
  }

  if (response.status === 401 || response.status === 403) {
    throw new AgendaAccountError(
      401,
      "unauthorized",
      "Sua sessão expirou. Entre novamente.",
    );
  }
  if (!response.ok) {
    throw new AgendaAccountError(
      503,
      "auth_unavailable",
      "O login está temporariamente indisponível.",
    );
  }

  let user: SupabaseUser;
  try {
    user = (await response.json()) as SupabaseUser;
  } catch {
    throw new AgendaAccountError(
      503,
      "invalid_auth_response",
      "O login retornou uma resposta inválida.",
    );
  }

  const id = String(user.id || "").trim();
  if (!/^[A-Za-z0-9_-]{8,128}$/.test(id)) {
    throw new AgendaAccountError(
      401,
      "unauthorized",
      "Não foi possível identificar esta conta.",
    );
  }

  return {
    id,
    email: String(user.email || "").trim().toLowerCase().slice(0, 320),
    kind: "supabase",
  };
}

export async function authenticateAgendaAccountUser(
  request: Request,
): Promise<AuthenticatedAgendaUser> {
  if (!/^Device\s+/i.test(request.headers.get("authorization") || "")) {
    return authenticateSupabaseUser(request);
  }
  try {
    const device = await authenticateAgendaAndroidDevice(request);
    await assertAgendaAndroidCanUse(device.userId);
    return {
      id: device.userId,
      email: device.email,
      kind: "device",
    };
  } catch (error) {
    if (error instanceof AgendaAndroidError) {
      throw new AgendaAccountError(
        error.status,
        error.code,
        error.message,
        error.details,
      );
    }
    throw error;
  }
}

async function readJsonBody(request: Request): Promise<JsonObject> {
  const declaredLength = Number(request.headers.get("content-length") || "0");
  if (Number.isFinite(declaredLength) && declaredLength > MAX_REQUEST_BYTES) {
    throw new AgendaAccountError(
      413,
      "payload_too_large",
      "Os dados enviados ultrapassam o limite permitido.",
    );
  }

  const source = await request.text();
  if (!source || new TextEncoder().encode(source).byteLength > MAX_REQUEST_BYTES) {
    throw new AgendaAccountError(400, "invalid_body", "Envie os dados da agenda.");
  }

  try {
    const parsed = JSON.parse(source);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error();
    return parsed as JsonObject;
  } catch {
    throw new AgendaAccountError(400, "invalid_json", "O JSON enviado é inválido.");
  }
}

const SENSITIVE_SETTINGS_KEYS = new Set([
  "accountpasswordhash",
  "businesslogopath",
  "instagramaccountid",
  "instagramapiurl",
  "instagramlasterror",
  "instagramlastcheckedat",
  "instagramlinked",
  "instagramlinkedat",
  "instagramstate",
  "mercadopagoconnected",
  "mercadopagodefaultterminalid",
  "mercadopagodefaultterminallabel",
  "mercadopagolasterror",
  "mercadopagolastsyncat",
  "mercadopagolicensekey",
  "mercadopagopaymentsapiurl",
  "mercadopagoselleruserid",
  "publicbookingapiurl",
  "publicbookinglastsyncat",
  "whatsappconnectedname",
  "whatsappevolutionbaseurl",
  "whatsappevolutionapikey",
  "whatsappevolutioninstancename",
  "whatsappevolutionlastcheckedat",
  "whatsappevolutionqrbase64",
  "whatsappevolutionstate",
  "whatsapplastmessageat",
  "whatsapplinked",
  "whatsapplinkedat",
  "whatsappstorephone",
]);

function normalizedKey(value: string) {
  return value.replace(/[^a-z0-9]/gi, "").toLowerCase();
}

function isSensitiveKey(key: string, insideSettings: boolean) {
  const normalized = normalizedKey(key);
  return (
    (insideSettings && SENSITIVE_SETTINGS_KEYS.has(normalized)) ||
    normalized === "credential" ||
    normalized === "credentials" ||
    normalized === "token" ||
    normalized.endsWith("token") ||
    normalized.includes("password") ||
    normalized.includes("privatekey") ||
    normalized.includes("secret") ||
    normalized.endsWith("apikey")
  );
}

function sanitizeJson(
  value: unknown,
  insideSettings = false,
  depth = 0,
): unknown {
  if (depth > MAX_JSON_DEPTH) {
    throw new AgendaAccountError(
      400,
      "payload_too_deep",
      "A estrutura dos dados enviados é inválida.",
    );
  }
  if (value === null || typeof value !== "object") return value;
  if (Array.isArray(value)) {
    return value.map((item) => sanitizeJson(item, insideSettings, depth + 1));
  }

  const result: JsonObject = {};
  for (const [key, item] of Object.entries(value as JsonObject)) {
    if (isSensitiveKey(key, insideSettings)) continue;
    const childIsSettings = insideSettings || normalizedKey(key) === "settings";
    result[key] = sanitizeJson(item, childIsSettings, depth + 1);
  }
  return result;
}

const REQUIRED_AGENDA_ARRAYS = [
  "services",
  "professionals",
  "customers",
  "appointments",
  "products",
  "productsales",
  "manualpayments",
  "expenses",
  "whatsappmessages",
  "whatsappleads",
] as const;

function agendaPayloadFields(payload: JsonObject) {
  const fields = new Map<string, unknown>();
  for (const [key, value] of Object.entries(payload)) {
    const normalized = normalizedKey(key);
    if (!normalized || fields.has(normalized)) {
      throw new AgendaAccountError(
        400,
        "invalid_payload_contract",
        "A estrutura dos dados da agenda e invalida.",
      );
    }
    fields.set(normalized, value);
  }
  return fields;
}

function validateAgendaPayload(payload: JsonObject) {
  const fields = agendaPayloadFields(payload);
  const settings = fields.get("settings");
  if (!settings || typeof settings !== "object" || Array.isArray(settings)) {
    throw new AgendaAccountError(
      400,
      "invalid_payload_contract",
      "Os dados enviados nao contem as configuracoes da agenda.",
    );
  }

  for (const field of REQUIRED_AGENDA_ARRAYS) {
    if (!Array.isArray(fields.get(field))) {
      throw new AgendaAccountError(
        400,
        "invalid_payload_contract",
        "Os dados enviados estao incompletos ou incompativeis.",
      );
    }
  }
}

function requiredInteger(
  value: unknown,
  name: string,
  minimum: number,
  maximum: number,
) {
  if (!Number.isInteger(value) || Number(value) < minimum || Number(value) > maximum) {
    throw new AgendaAccountError(
      400,
      `invalid_${name}`,
      `O campo ${name} é inválido.`,
    );
  }
  return Number(value);
}

function deviceIdFrom(value: unknown) {
  const deviceId = String(value || "").trim();
  if (!deviceId || deviceId.length > 128 || /[\u0000-\u001f\u007f]/.test(deviceId)) {
    throw new AgendaAccountError(
      400,
      "invalid_device_id",
      "A identificação do dispositivo é inválida.",
    );
  }
  return deviceId;
}

async function ensureAccount(user: AuthenticatedAgendaUser) {
  if (user.kind === "device") {
    const existing = await readAccount(user.id);
    if (!existing) {
      throw new AgendaAccountError(
        401,
        "device_account_not_found",
        "Este aparelho precisa ser ativado novamente.",
      );
    }
    return;
  }
  const now = Date.now();
  await getAgendaD1()
    .prepare(
      `INSERT INTO agenda_cloud_accounts (
         user_id, email, payload_json, revision, schema_version,
         trial_started_at, trial_ends_at, last_device_id, created_at, updated_at
       ) VALUES (?1, ?2, NULL, 0, ?3, ?4, ?5, '', ?4, ?4)
       ON CONFLICT(user_id) DO UPDATE SET email = excluded.email`,
    )
    .bind(
      user.id,
      user.email,
      DEFAULT_SCHEMA_VERSION,
      now,
      now + TRIAL_DURATION_MS,
    )
    .run();
}

async function readAccount(userId: string) {
  return getAgendaD1()
    .prepare("SELECT * FROM agenda_cloud_accounts WHERE user_id = ?1 LIMIT 1")
    .bind(userId)
    .first<AccountRow>();
}

function parsePayload(value: string | null): JsonObject | null {
  if (value === null) return null;
  try {
    const parsed = JSON.parse(value);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error();
    return parsed as JsonObject;
  } catch {
    throw new AgendaAccountError(
      500,
      "invalid_stored_payload",
      "Os dados salvos desta conta estão inválidos.",
    );
  }
}

function accountState(
  row: AccountRow,
  entitlement: AgendaEntitlementState,
  now = Date.now(),
): AccountState {
  const startedAt = Number(row.trial_started_at);
  const endsAt = Number(row.trial_ends_at);
  return {
    exists: row.payload_json !== null,
    revision: Number(row.revision),
    schemaVersion: Number(row.schema_version),
    payload: parsePayload(row.payload_json),
    updatedAt: new Date(Number(row.updated_at)).toISOString(),
    trial: {
      startedAt: new Date(startedAt).toISOString(),
      endsAt: new Date(endsAt).toISOString(),
      active: now < endsAt,
      daysRemaining: Math.max(0, Math.ceil((endsAt - now) / (24 * 60 * 60 * 1000))),
    },
    entitlement,
  };
}

async function currentAccountState(user: AuthenticatedAgendaUser) {
  await ensureAccount(user);
  const row = await readAccount(user.id);
  if (!row) throw new Error("Account insert succeeded but the row was not found");
  const entitlement =
    user.kind === "supabase"
      ? await ensureAgendaEntitlementForUser(user.id)
      : await assertAgendaAndroidCanUse(user.id);
  return accountState(row, entitlement);
}

export async function getAgendaAccountState(request: Request) {
  const user = await authenticateAgendaAccountUser(request);
  return jsonResponse({ ok: true, ...(await currentAccountState(user)) });
}

export async function putAgendaAccountState(request: Request) {
  const user = await authenticateAgendaAccountUser(request);
  let currentEntitlement: AgendaEntitlementState;
  try {
    if (user.kind === "supabase") {
      currentEntitlement = await ensureAgendaEntitlementForUser(user.id);
      if (!currentEntitlement.canUse) {
        throw new AgendaAccountError(
          402,
          "subscription_required",
          "O teste terminou ou o pagamento precisa ser regularizado.",
          { entitlement: currentEntitlement },
        );
      }
    } else {
      currentEntitlement = await assertAgendaAndroidCanUse(user.id);
    }
  } catch (error) {
    if (error instanceof AgendaAndroidError && error.status === 402) {
      throw new AgendaAccountError(
        402,
        error.code,
        error.message,
        error.details,
      );
    }
    throw error;
  }
  const body = await readJsonBody(request);
  const baseRevision = requiredInteger(
    body.baseRevision,
    "base_revision",
    0,
    2_147_483_647,
  );
  const schemaVersion = requiredInteger(
    body.schemaVersion,
    "schema_version",
    DEFAULT_SCHEMA_VERSION,
    DEFAULT_SCHEMA_VERSION,
  );
  const deviceId = deviceIdFrom(body.deviceId);
  if (!body.payload || typeof body.payload !== "object" || Array.isArray(body.payload)) {
    throw new AgendaAccountError(
      400,
      "invalid_payload",
      "Envie os dados completos da agenda.",
    );
  }

  validateAgendaPayload(body.payload as JsonObject);
  const payload = sanitizeJson(body.payload) as JsonObject;
  const payloadJson = JSON.stringify(payload);
  if (new TextEncoder().encode(payloadJson).byteLength > MAX_PAYLOAD_BYTES) {
    throw new AgendaAccountError(
      413,
      "payload_too_large",
      "Os dados da agenda ultrapassam o limite permitido.",
    );
  }

  await ensureAccount(user);
  const updatedAt = Date.now();
  const result = await getAgendaD1()
    .prepare(
      `UPDATE agenda_cloud_accounts
       SET email = ?1, payload_json = ?2, revision = revision + 1,
           schema_version = ?3, last_device_id = ?4, updated_at = ?5
       WHERE user_id = ?6 AND revision = ?7`,
    )
    .bind(
      user.email,
      payloadJson,
      schemaVersion,
      deviceId,
      updatedAt,
      user.id,
      baseRevision,
    )
    .run();

  if (Number(result.meta?.changes || 0) !== 1) {
    const remote = await readAccount(user.id);
    if (!remote) throw new Error("Account disappeared during revision check");
    return jsonResponse(
      {
        ok: false,
        error: {
          code: "revision_conflict",
          message: "A agenda foi atualizada em outro dispositivo.",
        },
        remote: accountState(remote, currentEntitlement),
      },
      409,
    );
  }

  const updated = await readAccount(user.id);
  if (!updated) throw new Error("Account update succeeded but the row was not found");
  return jsonResponse({ ok: true, ...accountState(updated, currentEntitlement) });
}
