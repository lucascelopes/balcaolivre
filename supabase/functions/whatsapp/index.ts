import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const DEFAULT_GRAPH_VERSION = "v25.0";
const DEFAULT_ADMIN_BUCKET = "balcao-livre-admin";
const DEFAULT_ADMIN_OBJECT = "admin-store.json";
const DEFAULT_VERIFY_TOKEN = "balcao_livre_meta_webhook_2026";
const DEFAULT_PHONE_NUMBER_ID = "154114447792775";
const CENTRAL_BOT_ID = "META_CLOUD";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type ClientPayload = {
  eventName?: string;
  licenseKey?: string;
  machineHash?: string;
  machineCode?: string;
  appVersion?: string;
  localExpiresAt?: string | null;
  localPlan?: string;
  profile?: Record<string, unknown>;
  settings?: Record<string, unknown>;
  metrics?: Record<string, unknown>;
};

type WhatsAppActivationPayload = ClientPayload & {
  storePhone?: string;
  localWhen?: string;
};

type WhatsAppSendPayload = WhatsAppActivationPayload & {
  customerName?: string;
  customerPhone?: string;
  message?: string;
  boardKind?: string;
  boardNumber?: string;
  total?: number;
};

type AdminStore = {
  licenses?: AdminLicense[];
  events?: AdminEvent[];
  whatsAppProcessedMessageIds?: string[];
  whatsAppMetaToken?: string;
};

type AdminLicense = {
  key?: string;
  status?: string;
  machineHash?: string;
  expiresAt?: string;
  whatsAppPhone?: string;
  whatsAppBotId?: string;
  whatsAppStatus?: string;
  whatsAppLastError?: string;
  whatsAppRequestedAt?: string;
  whatsAppActivatedAt?: string;
};

type AdminEvent = {
  type: string;
  message: string;
  licenseKey: string;
  machineCode: string;
  when: string;
};

type MetaWebhookMessage = {
  id: string;
  from: string;
  timestamp?: string;
  type: string;
  text?: { body?: string };
  button?: { text?: string; payload?: string };
  interactive?: Record<string, unknown>;
};

type MetaWebhookStatus = {
  id?: string;
  status?: string;
  timestamp?: string;
  recipient_id?: string;
};

type MetaWebhookEvent = {
  field: string;
  phoneNumberId: string;
  businessPhone: string;
  customerPhone: string;
  customerName: string;
  messageId: string;
  messageType: string;
  text: string;
  status: string;
  rawMessage?: MetaWebhookMessage;
  rawStatus?: MetaWebhookStatus;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    const route = routeFromPath(url.pathname);

    if (route === "/webhook") {
      return req.method === "GET" ? verifyWebhook(url) : receiveWebhook(req);
    }

    if (route === "/health" && req.method === "GET") {
      return health();
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/activate") {
      return activateStorePhone(req);
    }

    if (route === "/send") {
      return sendMessage(req);
    }

    return json({ ok: false, message: "Rota WhatsApp nao encontrada." }, 404);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

function verifyWebhook(url: URL) {
  const mode = url.searchParams.get("hub.mode") ?? "";
  const token = url.searchParams.get("hub.verify_token") ?? "";
  const challenge = url.searchParams.get("hub.challenge") ?? "";

  if (mode.toLowerCase() === "subscribe" && token && token === verifyToken() && challenge) {
    return new Response(challenge, {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "text/plain; charset=utf-8" },
    });
  }

  return new Response("Meta webhook verification failed.", {
    status: 403,
    headers: { ...corsHeaders, "Content-Type": "text/plain; charset=utf-8" },
  });
}

async function receiveWebhook(req: Request) {
  const payload = await readJson(req);
  const events = extractWebhookEvents(payload);
  console.log("whatsapp.meta.webhook", summarizeWebhook(payload, events));
  if (events.length > 0) {
    await processWebhookEvents(events);
  }

  return json({
    ok: true,
    receivedAt: new Date().toISOString(),
    messages: events.filter((event) => event.rawMessage).length,
    statuses: events.filter((event) => event.rawStatus).length,
  });
}

async function activateStorePhone(req: Request) {
  const payload = await readJson(req) as WhatsAppActivationPayload;
  const storePhone = normalizePhone(payload.storePhone);
  if (!storePhone) {
    return json(fail("Informe o numero do WhatsApp da loja com DDD."), 400);
  }

  if (!await hasMetaAccessToken()) {
    return json(pending("WhatsApp central ainda nao configurado no Supabase.", storePhone), 503);
  }

  const licenseCheck = await validateAndUpdateLicense(payload, (license) => {
    license.whatsAppPhone = storePhone;
    license.whatsAppBotId = CENTRAL_BOT_ID;
    license.whatsAppStatus = "ATIVO";
    license.whatsAppLastError = "";
    license.whatsAppRequestedAt = new Date().toISOString();
    license.whatsAppActivatedAt ??= new Date().toISOString();
  }, {
    type: "whatsapp.meta.active",
    message: `WhatsApp Meta ativo: ${maskPhone(storePhone)}`,
  });

  if (!licenseCheck.ok) {
    return json(fail(licenseCheck.message), licenseCheck.status);
  }

  return json(active("WhatsApp automatico ativado pelo Supabase.", storePhone));
}

async function sendMessage(req: Request) {
  const payload = await readJson(req) as WhatsAppSendPayload;
  const customerPhone = normalizePhone(payload.customerPhone);
  const message = String(payload.message ?? "").trim();

  if (!customerPhone) {
    return json(fail("Cliente sem telefone valido para WhatsApp."), 400);
  }

  if (!message) {
    return json(fail("Mensagem do WhatsApp vazia."), 400);
  }

  if (!await hasMetaAccessToken()) {
    return json(pending("WhatsApp central ainda nao configurado no Supabase.", normalizePhone(payload.storePhone)), 503);
  }

  const licenseCheck = await validateAndUpdateLicense(payload, (license) => {
    if (payload.storePhone) {
      license.whatsAppPhone = normalizePhone(payload.storePhone);
    }
    license.whatsAppBotId = CENTRAL_BOT_ID;
    license.whatsAppStatus = "ATIVO";
    license.whatsAppActivatedAt ??= new Date().toISOString();
  }, {
    type: "whatsapp.meta.send",
    message: `WhatsApp solicitado para ${maskPhone(customerPhone)}`,
  });

  if (!licenseCheck.ok) {
    return json(fail(licenseCheck.message), licenseCheck.status);
  }

  const sent = await sendMetaText(customerPhone, message);
  await updateLicenseError(payload, sent.ok ? "" : sent.message, sent.ok
    ? `WhatsApp enviado para ${maskPhone(customerPhone)}`
    : `WhatsApp falhou: ${sent.message}`);

  return sent.ok
    ? json(active("WhatsApp enviado.", normalizePhone(payload.storePhone)))
    : json(fail(sent.message), 502);
}

async function health() {
  return json({
    ok: true,
    provider: "meta",
    phoneNumberConfigured: Boolean(phoneNumberId()),
    tokenConfigured: await hasMetaAccessToken(),
  });
}

async function sendMetaText(phone: string, body: string, store?: AdminStore | null) {
  const token = metaAccessToken(store ?? null) || await metaAccessTokenFromStore();
  if (!token) {
    return { ok: false, message: "Token da Meta nao configurado no Supabase." };
  }

  const response = await fetch(`https://graph.facebook.com/${graphVersion()}/${phoneNumberId()}/messages`, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      messaging_product: "whatsapp",
      recipient_type: "individual",
      to: phone,
      type: "text",
      text: {
        preview_url: false,
        body,
      },
    }),
  });

  const text = await response.text();
  if (response.ok) {
    return { ok: true, message: "WhatsApp enviado." };
  }

  return { ok: false, message: extractMetaError(response.status, text) };
}

async function processWebhookEvents(events: MetaWebhookEvent[]) {
  const context = await readAdminStore();
  if (!context.ok) {
    console.error("whatsapp.meta.webhook_store_unavailable", context.message);
  }

  const store = context.ok ? context.store : null;
  let changed = false;

  for (const event of events) {
    const license = store ? findWhatsAppLicense(store, event.businessPhone) : null;
    const licenseKey = normalizeLicense(license?.key);
    const clientPayload: ClientPayload = {
      eventName: event.rawMessage ? "whatsapp.webhook.message" : "whatsapp.webhook.status",
      licenseKey,
      machineCode: event.phoneNumberId,
    };

    if (store && event.rawMessage) {
      const preview = event.text || event.messageType;
      appendEvent(store, clientPayload, "whatsapp.meta.incoming",
        `Mensagem WhatsApp de ${event.customerName || maskPhone(event.customerPhone)}: ${compactText(preview, 90)}`);
      changed = true;
    }

    if (store && event.rawStatus) {
      appendEvent(store, clientPayload, "whatsapp.meta.status",
        `Status WhatsApp ${event.status || "desconhecido"} para ${maskPhone(event.customerPhone)}`);
      changed = true;
    }

    if (event.rawMessage && shouldAutoReply(event, store)) {
      const reply = buildIncomingReply(event);
      const sent = await sendMetaText(event.customerPhone, reply, store);
      if (store) {
        appendEvent(store, clientPayload, sent.ok ? "whatsapp.meta.reply_sent" : "whatsapp.meta.reply_failed",
          sent.ok
            ? `Resposta automatica enviada para ${maskPhone(event.customerPhone)}`
            : `Resposta automatica falhou para ${maskPhone(event.customerPhone)}: ${sent.message}`);
        markWebhookMessageProcessed(store, event.messageId);
        changed = true;
      }
    }
  }

  if (store && changed) {
    await saveAdminStore(store);
  }
}

function shouldAutoReply(event: MetaWebhookEvent, store: AdminStore | null) {
  if (!event.customerPhone || !event.messageId || event.messageType !== "text") {
    return false;
  }

  if (!metaAccessToken(store) || !autoReplyEnabled()) {
    return false;
  }

  return store ? !hasProcessedWebhookMessage(store, event.messageId) : false;
}

function buildIncomingReply(event: MetaWebhookEvent) {
  const text = normalizeText(event.text);
  const greeting = "Ola! Recebemos sua mensagem no Balcao Livre.";
  if (text.includes("cardapio") || text.includes("menu")) {
    return `${greeting} Envie os itens que deseja pedir que o restaurante acompanha por aqui.`;
  }

  if (text.includes("pedido") || text.includes("entrega")) {
    return `${greeting} O restaurante ja foi notificado e vai acompanhar seu pedido por aqui.`;
  }

  return `${greeting} Em instantes o restaurante responde por aqui.`;
}

function findWhatsAppLicense(store: AdminStore, businessPhone: string) {
  const normalizedBusinessPhone = normalizePhone(businessPhone);
  if (!normalizedBusinessPhone) return null;

  return (store.licenses ?? []).find((license) =>
    normalizePhone(license.whatsAppPhone) === normalizedBusinessPhone
  ) ?? null;
}

function hasProcessedWebhookMessage(store: AdminStore, messageId: string) {
  const ids = store.whatsAppProcessedMessageIds ?? [];
  return ids.includes(messageId);
}

function markWebhookMessageProcessed(store: AdminStore, messageId: string) {
  store.whatsAppProcessedMessageIds ??= [];
  if (!messageId || store.whatsAppProcessedMessageIds.includes(messageId)) {
    return;
  }

  store.whatsAppProcessedMessageIds.unshift(messageId);
  store.whatsAppProcessedMessageIds = store.whatsAppProcessedMessageIds.slice(0, 500);
}

async function validateAndUpdateLicense(
  payload: ClientPayload,
  update: (license: AdminLicense) => void,
  event: { type: string; message: string },
) {
  const licenseKey = normalizeLicense(payload.licenseKey);
  if (!licenseKey) {
    return { ok: false, status: 401, message: "Licenca obrigatoria para usar WhatsApp automatico." };
  }

  const context = await readAdminStore();
  if (!context.ok) {
    return { ok: false, status: 503, message: context.message };
  }

  const store = context.store;
  const license = findLicense(store, licenseKey);
  if (!license) {
    return { ok: false, status: 401, message: "Chave nao encontrada no Supabase." };
  }

  const blocked = String(license.status ?? "").toUpperCase() === "BLOQUEADA";
  if (blocked) {
    return { ok: false, status: 403, message: "Licenca bloqueada para WhatsApp." };
  }

  if (license.expiresAt && Date.parse(license.expiresAt) <= Date.now()) {
    return { ok: false, status: 403, message: "Licenca expirada para WhatsApp." };
  }

  const requestMachine = String(payload.machineHash ?? "").trim();
  const licenseMachine = String(license.machineHash ?? "").trim();
  if (licenseMachine && requestMachine && licenseMachine !== requestMachine) {
    return { ok: false, status: 403, message: "Licenca pertence a outro computador." };
  }

  update(license);
  appendEvent(store, payload, event.type, event.message);
  await saveAdminStore(store);
  return { ok: true, status: 200, message: "" };
}

async function updateLicenseError(payload: ClientPayload, error: string, eventMessage: string) {
  try {
    const context = await readAdminStore();
    if (!context.ok) return;

    const license = findLicense(context.store, normalizeLicense(payload.licenseKey));
    if (!license) return;

    license.whatsAppLastError = error;
    appendEvent(context.store, payload, error ? "whatsapp.meta.failed" : "whatsapp.meta.sent", eventMessage);
    await saveAdminStore(context.store);
  } catch (error) {
    console.error("whatsapp.meta.store_update_failed", messageFromError(error));
  }
}

async function readAdminStore(): Promise<{ ok: true; store: AdminStore } | { ok: false; message: string }> {
  const supabase = serviceClient();
  if (!supabase) {
    return { ok: false, message: "Supabase nao tem service role disponivel para validar licenca." };
  }

  const { data, error } = await supabase.storage.from(adminBucket()).download(adminObjectPath());
  if (error || !data) {
    return { ok: false, message: "Nao consegui ler licencas no Supabase." };
  }

  try {
    const store = JSON.parse(await data.text()) as AdminStore;
    store.licenses ??= [];
    store.events ??= [];
    store.whatsAppProcessedMessageIds ??= [];
    return { ok: true, store };
  } catch (_error) {
    return { ok: false, message: "Arquivo de licencas do Supabase esta invalido." };
  }
}

async function saveAdminStore(store: AdminStore) {
  const supabase = serviceClient();
  if (!supabase) throw new Error("Supabase service role indisponivel.");

  if ((store.events?.length ?? 0) > 500) {
    store.events = [...(store.events ?? [])]
      .sort((a, b) => Date.parse(b.when) - Date.parse(a.when))
      .slice(0, 500)
      .sort((a, b) => Date.parse(a.when) - Date.parse(b.when));
  }

  const content = new Blob([JSON.stringify(store, null, 2)], { type: "application/json" });
  const { error } = await supabase.storage
    .from(adminBucket())
    .upload(adminObjectPath(), content, {
      upsert: true,
      contentType: "application/json",
    });

  if (error) throw error;
}

function serviceClient() {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const key = serviceRoleKey();
  if (!url || !key) return null;
  return createClient(url, key, { auth: { persistSession: false } });
}

function serviceRoleKey() {
  const legacy = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  if (legacy) return legacy;

  const secretKeys = Deno.env.get("SUPABASE_SECRET_KEYS") ?? "";
  if (!secretKeys) return "";

  try {
    const parsed = JSON.parse(secretKeys) as Record<string, string>;
    return parsed.default ?? Object.values(parsed)[0] ?? "";
  } catch (_error) {
    return "";
  }
}

function appendEvent(store: AdminStore, payload: ClientPayload, type: string, message: string) {
  store.events ??= [];
  store.events.push({
    type,
    message,
    licenseKey: normalizeLicense(payload.licenseKey),
    machineCode: String(payload.machineCode ?? ""),
    when: new Date().toISOString(),
  });
}

function findLicense(store: AdminStore, licenseKey: string) {
  return (store.licenses ?? []).find((license) =>
    normalizeLicense(license.key) === licenseKey
  ) ?? null;
}

async function readJson(req: Request) {
  try {
    return camelizeKeys(await req.json());
  } catch (_error) {
    return {};
  }
}

function camelizeKeys(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(camelizeKeys);
  }

  if (!value || typeof value !== "object") {
    return value;
  }

  const output: Record<string, unknown> = {};
  for (const [key, item] of Object.entries(value as Record<string, unknown>)) {
    const normalized = key ? key[0].toLowerCase() + key.slice(1) : key;
    output[normalized] = camelizeKeys(item);
  }

  return output;
}

function routeFromPath(pathname: string) {
  const marker = "/whatsapp";
  const index = pathname.indexOf(marker);
  if (index < 0) return "/";
  const route = pathname.slice(index + marker.length) || "/";
  return route.endsWith("/") && route.length > 1 ? route.slice(0, -1) : route;
}

async function hasMetaAccessToken() {
  return Boolean(phoneNumberId() && (metaAccessToken(null) || await metaAccessTokenFromStore()));
}

async function metaAccessTokenFromStore() {
  const context = await readAdminStore();
  const token = context.ok ? metaAccessToken(context.store) : "";
  return token || await metaAccessTokenFromDb();
}

async function metaAccessTokenFromDb() {
  const supabase = serviceClient();
  if (!supabase) return "";

  const { data, error } = await supabase.rpc("balcao_whatsapp_meta_token");
  if (error || typeof data !== "string") {
    if (error) console.error("whatsapp.meta.token_rpc_failed", error.message);
    return "";
  }

  return normalizeBearer(data);
}

function metaAccessToken(store: AdminStore | null) {
  return normalizeBearer(Deno.env.get("META_WHATSAPP_TOKEN")
    ?? Deno.env.get("WHATSAPP_META_TOKEN")
    ?? store?.whatsAppMetaToken
    ?? "");
}

function phoneNumberId() {
  return String(Deno.env.get("META_WHATSAPP_PHONE_NUMBER_ID")
    ?? Deno.env.get("WHATSAPP_META_PHONE_NUMBER_ID")
    ?? DEFAULT_PHONE_NUMBER_ID).trim();
}

function verifyToken() {
  return String(Deno.env.get("META_WHATSAPP_VERIFY_TOKEN")
    ?? Deno.env.get("WHATSAPP_META_VERIFY_TOKEN")
    ?? DEFAULT_VERIFY_TOKEN).trim();
}

function graphVersion() {
  return String(Deno.env.get("META_GRAPH_VERSION") ?? DEFAULT_GRAPH_VERSION).trim().replace(/^\/+|\/+$/g, "");
}

function autoReplyEnabled() {
  const value = String(Deno.env.get("META_WHATSAPP_AUTO_REPLY") ?? "true").trim().toLowerCase();
  return !["0", "false", "nao", "off"].includes(value);
}

function adminBucket() {
  return String(Deno.env.get("BVPDV_SUPABASE_BUCKET") ?? DEFAULT_ADMIN_BUCKET).trim();
}

function adminObjectPath() {
  return String(Deno.env.get("BVPDV_SUPABASE_OBJECT") ?? DEFAULT_ADMIN_OBJECT).trim().replace(/^\/+/, "");
}

function active(message: string, storePhone: string) {
  return { ok: true, pending: false, message, storePhone };
}

function pending(message: string, storePhone: string) {
  return { ok: false, pending: true, message, storePhone };
}

function fail(message: string) {
  return { ok: false, pending: false, message };
}

function json(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}

function normalizePhone(value: unknown) {
  const digits = String(value ?? "").replace(/\D/g, "");
  if (!digits) return "";
  if (digits.length <= 11) return `55${digits}`;
  return digits;
}

function normalizeLicense(value: unknown) {
  return String(value ?? "").trim().toUpperCase();
}

function normalizeBearer(value: string) {
  const token = String(value ?? "").trim();
  return token.toLowerCase().startsWith("bearer ") ? token.slice(7).trim() : token;
}

function maskPhone(phone: string) {
  const digits = normalizePhone(phone);
  if (digits.length <= 4) return digits;
  return `${digits.slice(0, 4)}...${digits.slice(-4)}`;
}

function extractMetaError(status: number, body: string) {
  if (!body) return `Meta retornou HTTP ${status}.`;
  try {
    const jsonBody = JSON.parse(body);
    const error = jsonBody?.error ?? {};
    const message = error.error_user_msg ?? error.message ?? error.code;
    if (message) return String(message);
  } catch (_error) {
    // plain text fallback below
  }

  const compact = body.replace(/\s+/g, " ").trim();
  return compact.length > 260 ? compact.slice(0, 260) : compact;
}

function extractWebhookEvents(payload: unknown): MetaWebhookEvent[] {
  const output: MetaWebhookEvent[] = [];
  const root = payload as Record<string, unknown>;
  const entries = Array.isArray(root?.entry) ? root.entry as Record<string, unknown>[] : [];
  for (const entry of entries) {
    const changes = Array.isArray(entry.changes) ? entry.changes as Record<string, unknown>[] : [];
    for (const change of changes) {
      const field = String(change.field ?? "");
      const value = (change.value ?? {}) as Record<string, unknown>;
      const metadata = (value.metadata ?? {}) as Record<string, unknown>;
      const contacts = Array.isArray(value.contacts) ? value.contacts as Record<string, unknown>[] : [];
      const firstContact = contacts[0] ?? {};
      const profile = (firstContact.profile ?? {}) as Record<string, unknown>;
      const phoneNumberId = String(metadata.phone_number_id ?? "");
      const businessPhone = normalizePhone(metadata.display_phone_number);
      const customerName = String(profile.name ?? "");

      const messages = Array.isArray(value.messages) ? value.messages as MetaWebhookMessage[] : [];
      for (const message of messages) {
        output.push({
          field,
          phoneNumberId,
          businessPhone,
          customerPhone: normalizePhone(message.from),
          customerName,
          messageId: String(message.id ?? ""),
          messageType: String(message.type ?? ""),
          text: extractMessageText(message),
          status: "",
          rawMessage: message,
        });
      }

      const statuses = Array.isArray(value.statuses) ? value.statuses as MetaWebhookStatus[] : [];
      for (const status of statuses) {
        output.push({
          field,
          phoneNumberId,
          businessPhone,
          customerPhone: normalizePhone(status.recipient_id),
          customerName: "",
          messageId: String(status.id ?? ""),
          messageType: "",
          text: "",
          status: String(status.status ?? ""),
          rawStatus: status,
        });
      }
    }
  }

  return output;
}

function extractMessageText(message: MetaWebhookMessage) {
  if (message.text?.body) return String(message.text.body);
  if (message.button?.text) return String(message.button.text);
  if (message.button?.payload) return String(message.button.payload);
  if (message.interactive) return compactText(JSON.stringify(message.interactive), 160);
  return "";
}

function summarizeWebhook(payload: unknown, events = extractWebhookEvents(payload)) {
  try {
    const value = payload as Record<string, unknown>;
    const entries = Array.isArray(value?.entry) ? value.entry as Record<string, unknown>[] : [];
    const changes = entries.flatMap((entry) => Array.isArray(entry.changes) ? entry.changes as Record<string, unknown>[] : []);
    return {
      entries: entries.length,
      changes: changes.length,
      messages: events.filter((event) => event.rawMessage).length,
      statuses: events.filter((event) => event.rawStatus).length,
    };
  } catch (_error) {
    return { entries: 0, changes: 0, messages: 0, statuses: 0 };
  }
}

function compactText(value: unknown, maxLength: number) {
  const compact = String(value ?? "").replace(/\s+/g, " ").trim();
  return compact.length > maxLength ? `${compact.slice(0, maxLength - 1)}...` : compact;
}

function normalizeText(value: unknown) {
  return String(value ?? "")
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()
    .trim();
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}
