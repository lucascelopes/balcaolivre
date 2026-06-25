import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const DEFAULT_THREAD_ID = "codex-default";
const LOCAL_LICENSE_KEY = "LOCAL";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": [
    "authorization",
    "x-client-info",
    "apikey",
    "content-type",
    "x-skun-device-id",
    "x-skun-device-secret",
    "x-skun-license",
    "x-skun-machine",
    "x-skun-machine-code",
    "x-skun-app-version",
    "x-skun-store-name",
    "x-balcao-license",
    "x-balcao-machine",
    "x-balcao-machine-code",
    "x-balcao-app-version",
    "x-balcao-store-name",
  ].join(", "),
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type DeviceValidation =
  | {
      ok: true;
      licenseKey: string;
      machineHash: string;
      machineCode: string;
      storeName: string;
      deviceId: string;
    }
  | { ok: false; message: string; status: number };

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  let route = "";
  try {
    const url = new URL(req.url);
    route = routeFromPath(url.pathname);

    if (route === "/health" && req.method === "GET") {
      return json({
        ok: true,
        app: "SkunKabam Codex MCP",
        storage: "supabase",
        authMode: "local-device-db",
      });
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    const body = await readJson(req);
    const validation = await validateLocalDevice(req, body);
    if (!validation.ok) {
      return json({ ok: false, message: validation.message }, validation.status);
    }

    if (route === "/sync") {
      return syncCodexEvent(body, validation);
    }

    if (route === "/cards/list") {
      return listCards(body, validation);
    }

    if (route === "/thread/get") {
      return getThread(body, validation);
    }

    return json({ ok: false, message: "Rota SkunKabam Codex nao encontrada." }, 404);
  } catch (error) {
    return json({
      ok: false,
      route,
      message: error instanceof Error ? error.message : String(error),
    }, 500);
  }
});

async function syncCodexEvent(body: Record<string, unknown>, validation: Extract<DeviceValidation, { ok: true }>) {
  const supabase = serviceClient();
  const threadInput = recordValue(body.thread);
  const externalThreadId = firstNonEmpty(
    threadInput.externalThreadId,
    threadInput.external_thread_id,
    threadInput.id,
    body.threadId,
    body.externalThreadId,
    DEFAULT_THREAD_ID,
  );

  const thread = await upsertThread(supabase, validation, threadInput, externalThreadId);
  let card: Record<string, unknown> | null = null;
  const cardInput = recordValue(body.card);
  if (Object.keys(cardInput).length > 0) {
    card = await upsertCard(supabase, validation, cardInput, thread);
  } else {
    card = await findCardByExternalId(supabase, validation, firstNonEmpty(body.externalCardId, body.cardId));
  }

  const message = await insertMessage(supabase, validation, recordValue(body.message), thread);
  const action = await insertAction(supabase, validation, recordValue(body.action), thread, card);
  const link = await insertLink(supabase, validation, recordValue(body.link), thread, card);

  return json({
    ok: true,
    thread,
    card,
    message,
    action,
    link,
  });
}

async function listCards(body: Record<string, unknown>, validation: Extract<DeviceValidation, { ok: true }>) {
  const status = normalizeCardStatus(body.status, "");
  const limit = boundedNumber(body.limit, 1, 100, 50);
  let query = serviceClient()
    .from("skunkabam_codex_cards")
    .select("id,external_card_id,title,description,status,priority,labels,assignee,due_at,completed_at,metadata,created_at,updated_at,thread_id")
    .eq("license_key", validation.licenseKey)
    .eq("machine_hash", validation.machineHash)
    .order("updated_at", { ascending: false })
    .limit(limit);

  if (status) {
    query = query.eq("status", status);
  }

  const { data, error } = await query;
  if (error) {
    return json({ ok: false, message: `Supabase recusou listar cards: ${error.message}` }, 500);
  }

  return json({ ok: true, cards: data ?? [] });
}

async function getThread(body: Record<string, unknown>, validation: Extract<DeviceValidation, { ok: true }>) {
  const externalThreadId = firstNonEmpty(body.threadId, body.externalThreadId, DEFAULT_THREAD_ID);
  const supabase = serviceClient();
  const found = await supabase
    .from("skunkabam_codex_threads")
    .select("id,external_thread_id,title,status,metadata,created_at,updated_at,last_message_at")
    .eq("license_key", validation.licenseKey)
    .eq("machine_hash", validation.machineHash)
    .eq("external_thread_id", externalThreadId)
    .maybeSingle();

  if (found.error) {
    return json({ ok: false, message: `Supabase recusou buscar thread: ${found.error.message}` }, 500);
  }

  if (!found.data) {
    return json({ ok: false, message: "Thread nao encontrada." }, 404);
  }

  const threadId = stringValue((found.data as Record<string, unknown>).id);
  const limit = boundedNumber(body.limit, 1, 200, 80);
  const [messages, actions, links, cards] = await Promise.all([
    supabase
      .from("skunkabam_codex_messages")
      .select("id,local_message_id,role,content,content_redacted,metadata,created_at")
      .eq("thread_id", threadId)
      .order("created_at", { ascending: true })
      .limit(limit),
    supabase
      .from("skunkabam_codex_actions")
      .select("id,card_id,action_type,title,summary,outcome,payload,created_at")
      .eq("thread_id", threadId)
      .order("created_at", { ascending: false })
      .limit(limit),
    supabase
      .from("skunkabam_codex_links")
      .select("id,card_id,link_type,title,url,path,metadata,created_at")
      .eq("thread_id", threadId)
      .order("created_at", { ascending: false })
      .limit(limit),
    supabase
      .from("skunkabam_codex_cards")
      .select("id,external_card_id,title,description,status,priority,labels,metadata,created_at,updated_at")
      .eq("thread_id", threadId)
      .order("updated_at", { ascending: false })
      .limit(50),
  ]);

  const failed = [messages.error, actions.error, links.error, cards.error].find(Boolean);
  if (failed) {
    return json({ ok: false, message: `Supabase recusou carregar thread: ${failed.message}` }, 500);
  }

  return json({
    ok: true,
    thread: found.data,
    messages: messages.data ?? [],
    actions: actions.data ?? [],
    links: links.data ?? [],
    cards: cards.data ?? [],
  });
}

async function upsertThread(
  supabase: ReturnType<typeof serviceClient>,
  validation: Extract<DeviceValidation, { ok: true }>,
  input: Record<string, unknown>,
  externalThreadId: string,
) {
  const now = new Date().toISOString();
  const payload = {
    license_key: validation.licenseKey,
    machine_hash: validation.machineHash,
    external_thread_id: limitText(externalThreadId, 160),
    source: limitText(firstNonEmpty(input.source, "codex"), 40),
    title: limitText(firstNonEmpty(input.title, "Atendimento Codex"), 180),
    status: normalizeThreadStatus(input.status),
    metadata: redactJson(recordValue(input.metadata)),
    updated_at: now,
  };

  const saved = await supabase
    .from("skunkabam_codex_threads")
    .upsert(payload, { onConflict: "license_key,machine_hash,external_thread_id" })
    .select("*")
    .single();

  if (saved.error) {
    throw new Error(`Supabase recusou salvar thread: ${saved.error.message}`);
  }

  return saved.data as Record<string, unknown>;
}

async function upsertCard(
  supabase: ReturnType<typeof serviceClient>,
  validation: Extract<DeviceValidation, { ok: true }>,
  input: Record<string, unknown>,
  thread: Record<string, unknown>,
) {
  const now = new Date().toISOString();
  const externalCardId = firstNonEmpty(input.externalCardId, input.external_card_id, input.id);
  const status = normalizeCardStatus(input.status, "backlog");
  const payload = {
    license_key: validation.licenseKey,
    machine_hash: validation.machineHash,
    thread_id: stringValue(thread.id) || null,
    external_card_id: externalCardId ? limitText(externalCardId, 160) : null,
    title: limitText(firstNonEmpty(input.title, "Tarefa Codex"), 220),
    description: limitText(redactText(firstNonEmpty(input.description, "")), 8000),
    status,
    priority: normalizePriority(input.priority),
    labels: normalizeLabels(input.labels),
    assignee: limitText(firstNonEmpty(input.assignee, ""), 120) || null,
    due_at: dateString(input.dueAt ?? input.due_at),
    completed_at: status === "done" ? dateString(input.completedAt ?? input.completed_at) ?? now : null,
    metadata: redactJson(recordValue(input.metadata)),
    updated_at: now,
  };

  const query = externalCardId
    ? supabase
      .from("skunkabam_codex_cards")
      .upsert(payload, { onConflict: "license_key,machine_hash,external_card_id" })
      .select("*")
      .single()
    : supabase
      .from("skunkabam_codex_cards")
      .insert(payload)
      .select("*")
      .single();

  const saved = await query;
  if (saved.error) {
    throw new Error(`Supabase recusou salvar card: ${saved.error.message}`);
  }

  return saved.data as Record<string, unknown>;
}

async function findCardByExternalId(
  supabase: ReturnType<typeof serviceClient>,
  validation: Extract<DeviceValidation, { ok: true }>,
  externalCardId: string,
) {
  if (!externalCardId) return null;
  const found = await supabase
    .from("skunkabam_codex_cards")
    .select("*")
    .eq("license_key", validation.licenseKey)
    .eq("machine_hash", validation.machineHash)
    .eq("external_card_id", externalCardId)
    .maybeSingle();

  if (found.error) {
    throw new Error(`Supabase recusou buscar card: ${found.error.message}`);
  }

  return found.data as Record<string, unknown> | null;
}

async function insertMessage(
  supabase: ReturnType<typeof serviceClient>,
  validation: Extract<DeviceValidation, { ok: true }>,
  input: Record<string, unknown>,
  thread: Record<string, unknown>,
) {
  if (Object.keys(input).length === 0) return null;
  const rawContent = firstNonEmpty(input.content, input.text, input.message);
  if (!rawContent) return null;
  const redacted = redactText(rawContent);
  const payload = {
    thread_id: stringValue(thread.id),
    license_key: validation.licenseKey,
    machine_hash: validation.machineHash,
    local_message_id: limitText(firstNonEmpty(input.localMessageId, input.local_message_id, input.id), 160) || null,
    role: normalizeRole(input.role),
    content: limitText(redacted, 60000),
    content_redacted: redacted !== rawContent,
    metadata: redactJson(recordValue(input.metadata)),
  };

  const saved = await supabase
    .from("skunkabam_codex_messages")
    .upsert(payload, { onConflict: "thread_id,local_message_id", ignoreDuplicates: false })
    .select("*")
    .single();

  if (saved.error) {
    throw new Error(`Supabase recusou salvar mensagem: ${saved.error.message}`);
  }

  await supabase
    .from("skunkabam_codex_threads")
    .update({ last_message_at: new Date().toISOString(), updated_at: new Date().toISOString() })
    .eq("id", stringValue(thread.id));

  return saved.data as Record<string, unknown>;
}

async function insertAction(
  supabase: ReturnType<typeof serviceClient>,
  validation: Extract<DeviceValidation, { ok: true }>,
  input: Record<string, unknown>,
  thread: Record<string, unknown>,
  card: Record<string, unknown> | null,
) {
  if (Object.keys(input).length === 0) return null;
  const actionType = limitText(firstNonEmpty(input.actionType, input.action_type, input.type, "note"), 80);
  const payload = {
    thread_id: stringValue(thread.id),
    card_id: stringValue(card?.id) || null,
    license_key: validation.licenseKey,
    machine_hash: validation.machineHash,
    action_type: actionType,
    title: limitText(firstNonEmpty(input.title, actionType), 220),
    summary: limitText(redactText(firstNonEmpty(input.summary, input.message, "")), 12000),
    outcome: normalizeOutcome(input.outcome ?? input.status),
    payload: redactJson(recordValue(input.payload)),
  };

  const saved = await supabase.from("skunkabam_codex_actions").insert(payload).select("*").single();
  if (saved.error) {
    throw new Error(`Supabase recusou salvar acao: ${saved.error.message}`);
  }

  return saved.data as Record<string, unknown>;
}

async function insertLink(
  supabase: ReturnType<typeof serviceClient>,
  validation: Extract<DeviceValidation, { ok: true }>,
  input: Record<string, unknown>,
  thread: Record<string, unknown>,
  card: Record<string, unknown> | null,
) {
  if (Object.keys(input).length === 0) return null;
  const url = firstNonEmpty(input.url, input.href);
  const path = firstNonEmpty(input.path, input.filePath, input.file_path);
  if (!url && !path) return null;

  const payload = {
    thread_id: stringValue(thread.id),
    card_id: stringValue(card?.id) || null,
    license_key: validation.licenseKey,
    machine_hash: validation.machineHash,
    link_type: limitText(firstNonEmpty(input.linkType, input.link_type, input.type, path ? "file" : "url"), 40),
    title: limitText(firstNonEmpty(input.title, path, url), 220),
    url: url ? limitText(redactText(url), 2000) : null,
    path: path ? limitText(redactText(path), 2000) : null,
    metadata: redactJson(recordValue(input.metadata)),
  };

  const saved = await supabase.from("skunkabam_codex_links").insert(payload).select("*").single();
  if (saved.error) {
    throw new Error(`Supabase recusou salvar link: ${saved.error.message}`);
  }

  return saved.data as Record<string, unknown>;
}

async function validateLocalDevice(req: Request, body: Record<string, unknown>): Promise<DeviceValidation> {
  const deviceId = normalizeDeviceId(firstNonEmpty(
    req.headers.get("x-skun-device-id"),
    body.deviceId,
    body.device_id,
    req.headers.get("x-skun-machine"),
    req.headers.get("x-balcao-machine"),
    body.machineHash,
    body.machine_hash,
  ));
  const deviceSecret = firstNonEmpty(
    req.headers.get("x-skun-device-secret"),
    body.deviceSecret,
    body.device_secret,
  );
  const machineCode = limitText(firstNonEmpty(
    req.headers.get("x-skun-machine-code"),
    req.headers.get("x-balcao-machine-code"),
    body.machineCode,
    body.machine_code,
    deviceId,
  ), 120);

  if (!deviceId || !deviceSecret) {
    return { ok: false, message: "Device ID e segredo local sao obrigatorios para usar o MCP.", status: 401 };
  }

  const supabase = serviceClient();
  const found = await supabase
    .from("skunkabam_codex_devices")
    .select("device_id,secret_hash,store_name,machine_code,enabled")
    .eq("device_id", deviceId)
    .maybeSingle();

  if (found.error) {
    return { ok: false, message: `Supabase recusou validar o PC local: ${found.error.message}`, status: 500 };
  }

  const row = found.data as Record<string, unknown> | null;
  if (!row) {
    return { ok: false, message: "PC local ainda nao cadastrado no SkunKabam MCP.", status: 401 };
  }

  if (row.enabled === false) {
    return { ok: false, message: "PC local desativado no SkunKabam MCP.", status: 403 };
  }

  const providedHash = await sha256Hex(deviceSecret);
  const expectedHash = stringValue(row.secret_hash).toLowerCase();
  if (!safeEqual(providedHash, expectedHash)) {
    return { ok: false, message: "Segredo local do SkunKabam MCP invalido.", status: 401 };
  }

  await supabase
    .from("skunkabam_codex_devices")
    .update({ last_seen_at: new Date().toISOString(), updated_at: new Date().toISOString() })
    .eq("device_id", deviceId);

  return {
    ok: true,
    licenseKey: LOCAL_LICENSE_KEY,
    machineHash: deviceId,
    machineCode: firstNonEmpty(row.machine_code, machineCode),
    storeName: firstNonEmpty(
      req.headers.get("x-skun-store-name"),
      req.headers.get("x-balcao-store-name"),
      body.storeName,
      body.store_name,
      row.store_name,
      "SkunKabam",
    ),
    deviceId,
  };
}

function routeFromPath(pathname: string) {
  const marker = "/skunkabam-codex";
  const index = pathname.indexOf(marker);
  if (index < 0) return pathname || "/";
  const route = pathname.slice(index + marker.length) || "/";
  return route.endsWith("/") && route.length > 1 ? route.slice(0, -1) : route;
}

async function readJson(req: Request): Promise<Record<string, unknown>> {
  try {
    return await req.json() as Record<string, unknown>;
  } catch {
    return {};
  }
}

function serviceClient() {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const key = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  if (!url || !key) {
    throw new Error("Supabase service role indisponivel.");
  }
  return createClient(url, key, { auth: { persistSession: false } });
}

function normalizeThreadStatus(value: unknown) {
  const text = stringValue(value).toLowerCase();
  return ["active", "paused", "done", "blocked", "archived"].includes(text) ? text : "active";
}

function normalizeCardStatus(value: unknown, fallback: string) {
  const text = stringValue(value).toLowerCase();
  return ["backlog", "todo", "doing", "review", "done", "blocked", "archived"].includes(text) ? text : fallback;
}

function normalizePriority(value: unknown) {
  const text = stringValue(value).toLowerCase();
  return ["low", "normal", "high", "urgent"].includes(text) ? text : "normal";
}

function normalizeOutcome(value: unknown) {
  const text = stringValue(value).toLowerCase();
  return ["logged", "success", "failed", "blocked", "skipped"].includes(text) ? text : "logged";
}

function normalizeRole(value: unknown) {
  const text = stringValue(value).toLowerCase();
  return ["user", "assistant", "system", "developer", "tool"].includes(text) ? text : "user";
}

function normalizeLabels(value: unknown) {
  if (!Array.isArray(value)) return [];
  return [...new Set(value.map((item) => limitText(stringValue(item).toLowerCase(), 50)).filter(Boolean))].slice(0, 20);
}

function normalizeDeviceId(value: unknown) {
  return stringValue(value)
    .toLowerCase()
    .replace(/[^a-z0-9._-]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120);
}

function normalizeLicense(value: unknown) {
  return stringValue(value).toUpperCase().replaceAll(" ", "").replaceAll("_", "-");
}

function safeEqual(left: string, right: string) {
  const encoder = new TextEncoder();
  const a = encoder.encode(left);
  const b = encoder.encode(right);
  if (a.length !== b.length) return false;

  let diff = 0;
  for (let index = 0; index < a.length; index += 1) {
    diff |= a[index] ^ b[index];
  }
  return diff === 0;
}

async function sha256Hex(value: string) {
  const bytes = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(bytes))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");
}

function firstNonEmpty(...values: unknown[]) {
  for (const value of values) {
    const text = stringValue(value);
    if (text) return text;
  }
  return "";
}

function recordValue(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};
}

function dateValue(value: unknown) {
  const timestamp = Date.parse(stringValue(value));
  return Number.isFinite(timestamp) ? new Date(timestamp) : null;
}

function dateString(value: unknown) {
  const date = dateValue(value);
  return date ? date.toISOString() : null;
}

function boundedNumber(value: unknown, min: number, max: number, fallback: number) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed)) return fallback;
  return Math.max(min, Math.min(max, Math.round(parsed)));
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function limitText(value: string, maxLength: number) {
  return value.length > maxLength ? `${value.slice(0, maxLength - 12)}...[cortado]` : value;
}

function redactText(value: string) {
  return String(value ?? "")
    .replace(/(SUPABASE_SERVICE_ROLE_KEY|SERVICE_ROLE_KEY|OPENAI_API_KEY|STRIPE_SECRET_KEY|GITHUB_TOKEN|AUTHORIZATION)\s*[:=]\s*["']?[^"',\s]+/gi, "$1=[REDACTED]")
    .replace(/\b(sb_secret|sk_live|sk_test|sk-proj|ghp|pat)_[A-Za-z0-9_-]{8,}\b/g, "$1_[REDACTED]")
    .replace(/\bBearer\s+[A-Za-z0-9._-]{16,}\b/gi, "Bearer [REDACTED]");
}

function redactJson(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(redactJson);
  }
  if (typeof value === "string") {
    return redactText(value);
  }
  if (!value || typeof value !== "object") {
    return value;
  }

  const output: Record<string, unknown> = {};
  for (const [key, inner] of Object.entries(value as Record<string, unknown>)) {
    if (/secret|token|password|authorization|apikey|service_role/i.test(key)) {
      output[key] = "[REDACTED]";
    } else {
      output[key] = redactJson(inner);
    }
  }
  return output;
}

function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}
