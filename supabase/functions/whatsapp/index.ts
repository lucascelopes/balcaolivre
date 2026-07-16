import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import {
  connectEvolutionInstance,
  createOnboardingStateKey,
  createEvolutionInstance,
  decodeEvolutionQrImage,
  disconnectEvolutionInstance,
  evolutionConnectionState,
  evolutionHealth,
  evolutionInstanceName,
  extractEvolutionMessages,
  extractEvolutionQr,
  findEvolutionMessages,
  normalizeEvolutionMessageLimit,
  onboardingProviderFromState,
  resolveWhatsAppProvider,
  sendEvolutionText,
  type EvolutionConfig,
  type EvolutionQrImage,
} from "./evolution.ts";

const DEFAULT_GRAPH_VERSION = "v25.0";
const DEFAULT_ADMIN_BUCKET = "balcao-livre-admin";
const DEFAULT_ADMIN_OBJECT = "admin-store.json";
const DEFAULT_VERIFY_TOKEN = "balcao_livre_meta_webhook_2026";
const PUBLIC_MENU_BASE_URL = "https://cardapio.balcaolivrepdv.com.br";
const DEFAULT_PHONE_NUMBER_ID = "154114447792775";
const DEFAULT_META_APP_ID = "355393956897950";
const CENTRAL_BOT_ID = "META_CLOUD";
const ONBOARDING_STATE_MINUTES = 30;
const BOT_ORDER_LOOKBACK_DAYS = 3;

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
  messageId?: string;
  customerName?: string;
  customerPhone?: string;
  message?: string;
  boardKind?: string;
  boardNumber?: string;
  total?: number;
};

type WhatsAppMessagesPayload = ClientPayload & {
  limit?: unknown;
};

type WhatsAppStoreConnection = {
  license_key?: string;
  machine_hash?: string;
  store_phone?: string;
  waba_id?: string;
  business_id?: string;
  phone_number_id?: string;
  phone_display_number?: string;
  access_token?: string;
  token_type?: string;
  status?: string;
  last_error?: string;
  connected_at?: string;
  updated_at?: string;
  meta_payload?: Record<string, unknown>;
};

type WhatsAppOnboardingState = {
  state?: string;
  license_key?: string;
  machine_hash?: string;
  store_phone?: string;
  expires_at?: string;
  created_at?: string;
};

type AdminStore = {
  licenses?: AdminLicense[];
  events?: AdminEvent[];
  whatsAppProcessedMessageIds?: string[];
  whatsAppProcessedSendIds?: string[];
  whatsAppMetaToken?: string;
};

type AdminLicense = {
  id?: string;
  key?: string;
  status?: string;
  plan?: string;
  customerName?: string;
  email?: string;
  businessName?: string;
  ownerName?: string;
  cnpj?: string;
  phone?: string;
  city?: string;
  state?: string;
  machineHash?: string;
  machineCode?: string;
  appVersion?: string;
  expiresAt?: string;
  createdAt?: string;
  activatedAt?: string;
  lastSeenAt?: string;
  whatsAppPhone?: string;
  whatsAppBotId?: string;
  whatsAppStatus?: string;
  whatsAppLastError?: string;
  whatsAppRequestedAt?: string;
  whatsAppActivatedAt?: string;
  whatsAppWabaId?: string;
  whatsAppBusinessId?: string;
  whatsAppPhoneNumberId?: string;
  whatsAppDisplayPhone?: string;
  whatsAppConnectedAt?: string;
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

type PublicMenuBotSnapshot = {
  id?: string;
  store_id?: string;
  slug?: string;
  name?: string;
  description?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  store_open?: boolean;
  wait_min_minutes?: number;
  wait_max_minutes?: number;
  is_published?: boolean;
};

type PublicOrderBotSnapshot = {
  id?: string;
  status?: string;
  order_type?: string;
  customer_phone?: string;
  table_label?: string;
  address?: string;
  total?: number;
  pdv_order_id?: string;
  created_at?: string;
  updated_at?: string;
};

Deno.serve((req) => {
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

    if (route === "/onboarding/start" && req.method === "GET") {
      return startOnboarding(url);
    }

    if (route === "/onboarding/callback" && req.method === "GET") {
      return completeOnboardingCallback(url);
    }

    if (route === "/onboarding/complete" && req.method === "POST") {
      return completeOnboarding(req);
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/activate") {
      return activateStorePhone(req);
    }

    if (route === "/status") {
      return storeStatus(req);
    }

    if (route === "/send") {
      return sendMessage(req);
    }

    if (route === "/messages") {
      return listMessages(req);
    }

    if (route === "/disconnect") {
      return disconnectStorePhone(req);
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

function startOnboarding(url: URL) {
  const state = String(url.searchParams.get("state") ?? "").trim();
  if (onboardingProviderFromState(state) !== "evolution") {
    return startMetaOnboarding(url);
  }

  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return text(provider.message, 503);
  }
  if (provider.kind !== "evolution") {
    return text("Evolution nao esta configurada no Supabase.", 503);
  }
  return startEvolutionOnboarding(url, provider.config);
}

async function startMetaOnboarding(url: URL) {
  const state = String(url.searchParams.get("state") ?? "").trim();
  if (!state) {
    return text("Link invalido. Abra a conexao pelo botao WhatsApp dentro do PDV.", 400);
  }

  const context = await readOnboardingState(state);
  if (!context.ok) {
    return text(context.message, 400);
  }

  const appId = metaAppId();
  const configId = embeddedSignupConfigId();
  if (!appId || !configId) {
    return text("Meta nao configurado. Falta App ID ou Config ID do Embedded Signup.", 503);
  }

  return Response.redirect(metaOAuthUrl({
    appId,
    configId,
    graphVersion: graphVersion(),
    state,
    redirectUri: functionRouteUrl(url, "/onboarding/callback"),
  }), 302);
}

async function completeOnboarding(req: Request) {
  const payload = await readJson(req) as Record<string, unknown>;
  const stateKey = String(payload.state ?? "").trim();
  const code = String(payload.code ?? "").trim();
  const sessionInfo = normalizeSessionInfo(payload.sessionInfo ?? payload.session ?? {});

  const result = await finishOnboarding(stateKey, code, sessionInfo);
  return json(result.body, result.status);
}

async function completeOnboardingCallback(url: URL) {
  const error = String(url.searchParams.get("error_message") || url.searchParams.get("error_description") || url.searchParams.get("error") || "").trim();
  if (error) {
    return text(`Meta recusou a conexao: ${error}`, 400);
  }

  const stateKey = String(url.searchParams.get("state") ?? "").trim();
  const code = String(url.searchParams.get("code") ?? "").trim();
  const result = await finishOnboarding(stateKey, code, {}, functionRouteUrl(url, "/onboarding/callback"));
  return text(result.body.message || (result.body.ok ? "WhatsApp conectado. Pode voltar para o PDV." : "Falha ao conectar WhatsApp."), result.status);
}

async function finishOnboarding(stateKey: string, code: string, sessionInfo: Record<string, unknown>, redirectUri = "") {
  if (!stateKey || !code) {
    return { status: 400, body: fail("Meta nao retornou codigo de conexao.") };
  }

  const state = await consumeOnboardingState(stateKey);
  if (!state.ok) {
    return { status: 400, body: fail(state.message) };
  }

  const tokenResult = await exchangeMetaCode(code, redirectUri);
  if (!tokenResult.ok) {
    return { status: 502, body: fail(tokenResult.message) };
  }

  const phoneInfo = await resolveOnboardedPhone(tokenResult.accessToken, sessionInfo, normalizePhone(state.state.store_phone));
  if (!phoneInfo.ok) {
    return { status: 502, body: fail(phoneInfo.message) };
  }

  if (phoneInfo.wabaId) {
    await subscribeWaba(tokenResult.accessToken, phoneInfo.wabaId);
  }

  const connection: WhatsAppStoreConnection = {
    license_key: normalizeLicense(state.state.license_key),
    machine_hash: String(state.state.machine_hash ?? ""),
    store_phone: normalizePhone(phoneInfo.displayPhone || state.state.store_phone),
    waba_id: phoneInfo.wabaId,
    business_id: phoneInfo.businessId,
    phone_number_id: phoneInfo.phoneNumberId,
    phone_display_number: phoneInfo.displayPhone,
    access_token: tokenResult.accessToken,
    token_type: tokenResult.tokenType,
    status: "ATIVO",
    last_error: "",
    connected_at: new Date().toISOString(),
    updated_at: new Date().toISOString(),
    meta_payload: sessionInfo,
  };

  const saved = await saveStoreConnection(connection);
  if (!saved.ok) {
    return { status: 500, body: fail(saved.message) };
  }

  await updateLicenseConnection(connection);

  return { status: 200, body: {
    ok: true,
    message: "WhatsApp conectado. Pode voltar para o PDV.",
    storePhone: connection.store_phone,
    phoneNumberId: connection.phone_number_id,
    wabaId: connection.waba_id,
  } };
}

function activateStorePhone(req: Request) {
  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return json(fail(provider.message), 503);
  }
  return provider.kind === "evolution"
    ? activateEvolutionStorePhone(req, provider.config)
    : activateMetaStorePhone(req);
}

async function activateMetaStorePhone(req: Request) {
  const payload = await readJson(req) as WhatsAppActivationPayload;
  const storePhone = normalizePhone(payload.storePhone);
  if (!storePhone) {
    return json(fail("Informe o numero do WhatsApp da loja com DDD."), 400);
  }

  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return json(fail(context.message), context.status);
  }

  const connection = await readStoreConnection(context.licenseKey);
  if (connection.ok && connection.connection && connectionUsableForPhone(connection.connection, storePhone)) {
    applyConnectionToLicense(context.license, connection.connection, storePhone);
    appendEvent(context.store, payload, "whatsapp.meta.active", `WhatsApp Meta conectado: ${maskPhone(storePhone)}`);
    await saveAdminStore(context.store);
    return json(active("WhatsApp automatico conectado pelo numero da loja.", storePhone, connection.connection));
  }

  const onboarding = await createOnboardingSession(req, payload, storePhone);
  context.license.whatsAppPhone = storePhone;
  context.license.whatsAppBotId = CENTRAL_BOT_ID;
  context.license.whatsAppStatus = "AGUARDANDO_META";
  context.license.whatsAppLastError = onboarding.ok ? "" : onboarding.message;
  context.license.whatsAppRequestedAt = new Date().toISOString();
  appendEvent(context.store, payload, "whatsapp.meta.onboarding_required",
    onboarding.ok
      ? `WhatsApp aguardando conexao Meta: ${maskPhone(storePhone)}`
      : `WhatsApp Meta pendente: ${onboarding.message}`);
  await saveAdminStore(context.store);

  if (!onboarding.ok) {
    return json(pending(onboarding.message, storePhone), 503);
  }

  return json(pending("Abra a conexao Meta para liberar o WhatsApp desse restaurante.", storePhone, onboarding.url));
}

function sendMessage(req: Request) {
  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return json(fail(provider.message), 503);
  }
  return provider.kind === "evolution"
    ? sendEvolutionMessage(req, provider.config)
    : sendMetaMessage(req);
}

function listMessages(req: Request) {
  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return privateJson(fail(provider.message), 503);
  }
  if (provider.kind !== "evolution") {
    return privateJson(fail("Consulta de mensagens disponivel apenas com Evolution."), 503);
  }
  return listEvolutionMessages(req, provider.config);
}

function disconnectStorePhone(req: Request) {
  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return privateJson(fail(provider.message), 503);
  }
  if (provider.kind !== "evolution") {
    return privateJson(fail("Desconexao disponivel apenas com Evolution."), 503);
  }
  return disconnectEvolutionStorePhone(req, provider.config);
}

async function sendMetaMessage(req: Request) {
  const payload = await readJson(req) as WhatsAppSendPayload;
  const customerPhone = normalizePhone(payload.customerPhone);
  const message = String(payload.message ?? "").trim();

  if (!customerPhone) {
    return json(fail("Cliente sem telefone valido para WhatsApp."), 400);
  }

  if (!message) {
    return json(fail("Mensagem do WhatsApp vazia."), 400);
  }

  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return json(fail(context.message), context.status);
  }

  const connection = await readStoreConnection(context.licenseKey);
  if (!connection.ok || !connection.connection || !connectionUsable(connection.connection)) {
    const onboarding = await createOnboardingSession(req, payload, normalizePhone(payload.storePhone || context.license.whatsAppPhone));
    context.license.whatsAppStatus = "AGUARDANDO_META";
    context.license.whatsAppLastError = "Numero da loja ainda nao conectado na Meta.";
    appendEvent(context.store, payload, "whatsapp.meta.onboarding_required",
      `WhatsApp nao enviado. Conecte o numero da loja antes de enviar para ${maskPhone(customerPhone)}`);
    await saveAdminStore(context.store);
    return json(pending("Conecte o numero da loja na Meta antes de enviar WhatsApp.", normalizePhone(payload.storePhone), onboarding.ok ? onboarding.url : ""), 428);
  }

  if (payload.storePhone) {
    context.license.whatsAppPhone = normalizePhone(payload.storePhone);
  }
  applyConnectionToLicense(context.license, connection.connection, normalizePhone(payload.storePhone || context.license.whatsAppPhone));
  appendEvent(context.store, payload, "whatsapp.meta.send", `WhatsApp solicitado para ${maskPhone(customerPhone)}`);
  await saveAdminStore(context.store);

  const sent = await sendMetaText(customerPhone, message, { connection: connection.connection });
  await updateLicenseError(payload, sent.ok ? "" : sent.message, sent.ok
    ? `WhatsApp enviado para ${maskPhone(customerPhone)}`
    : `WhatsApp falhou: ${sent.message}`);

  return sent.ok
    ? json(active("WhatsApp enviado.", normalizePhone(payload.storePhone), connection.connection))
    : json(fail(sent.message), 502);
}

function storeStatus(req: Request) {
  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return json(fail(provider.message), 503);
  }
  return provider.kind === "evolution"
    ? evolutionStoreStatus(req, provider.config)
    : metaStoreStatus(req);
}

async function metaStoreStatus(req: Request) {
  const payload = await readJson(req) as WhatsAppActivationPayload;
  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return json(fail(context.message), context.status);
  }

  const connection = await readStoreConnection(context.licenseKey);
  if (connection.ok && connection.connection && connectionUsable(connection.connection)) {
    return json(active("WhatsApp conectado na Meta.", normalizePhone(connection.connection.store_phone || context.license.whatsAppPhone), connection.connection));
  }

  const storePhone = normalizePhone(payload.storePhone || context.license.whatsAppPhone);
  const onboarding = storePhone ? await createOnboardingSession(req, payload, storePhone) : null;
  return json(pending(
    storePhone ? "WhatsApp ainda precisa ser conectado na Meta." : "Informe o numero da loja para conectar WhatsApp.",
    storePhone,
    onboarding?.ok ? onboarding.url : "",
  ));
}

async function health() {
  const provider = resolveWhatsAppProvider();
  if (provider.kind === "invalid") {
    return json({
      ok: false,
      provider: "evolution",
      configured: false,
      message: provider.message,
    }, 503);
  }

  if (provider.kind === "evolution") {
    const result = await evolutionHealth(provider.config);
    return result.ok
      ? json({
        ok: true,
        provider: "evolution",
        configured: true,
        origin: true,
        status: result.status,
      })
      : json({
        ok: false,
        provider: "evolution",
        configured: true,
        origin: false,
        status: result.status,
        message: result.message,
      }, 502);
  }

  return metaHealth();
}

async function metaHealth() {
  return json({
    ok: true,
    provider: "meta",
    phoneNumberConfigured: Boolean(phoneNumberId()),
    tokenConfigured: await hasMetaAccessToken(),
    onboardingConfigured: Boolean(metaAppId() && metaAppSecret() && embeddedSignupConfigId()),
  });
}

async function activateEvolutionStorePhone(req: Request, config: EvolutionConfig) {
  const payload = await readJson(req) as WhatsAppActivationPayload;
  const storePhone = normalizePhone(payload.storePhone);
  if (!storePhone) {
    return json(fail("Informe o numero do WhatsApp da loja com DDD."), 400);
  }

  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return json(fail(context.message), context.status);
  }

  const instanceName = await evolutionInstanceName(context.licenseKey);
  const instance = await ensureEvolutionInstance(config, instanceName);
  if (!instance.ok) {
    await saveEvolutionError(context, payload, instance.message, "whatsapp.evolution.activate_failed");
    return json(fail(instance.message), 502);
  }

  if (instance.state === "open") {
    const registeredPhone = normalizePhone(context.license.whatsAppPhone);
    if (!phoneNumbersMatch(registeredPhone, storePhone)) {
      return json(fail(
        `Esta licenca ja esta conectada ao WhatsApp ${maskPhone(registeredPhone)}. Desconecte a sessao atual antes de trocar o numero.`,
      ), 409);
    }
    applyEvolutionActive(context.license, storePhone, instanceName);
    appendEvent(context.store, payload, "whatsapp.evolution.active",
      `WhatsApp Evolution conectado: ${maskPhone(storePhone)}`);
    await saveAdminStore(context.store);
    return json(active("WhatsApp conectado pela Evolution.", storePhone));
  }

  const onboarding = await createEvolutionOnboardingSession(req, payload, storePhone);
  applyEvolutionPending(context.license, storePhone, instanceName, onboarding.ok ? "" : onboarding.message);
  appendEvent(context.store, payload, "whatsapp.evolution.onboarding_required",
    onboarding.ok
      ? `WhatsApp aguardando QR Code Evolution: ${maskPhone(storePhone)}`
      : `WhatsApp Evolution pendente: ${onboarding.message}`);
  await saveAdminStore(context.store);

  if (!onboarding.ok) {
    return json(pending(onboarding.message, storePhone), 503);
  }

  return json(pending(
    "Abra o QR Code e conecte o WhatsApp desse restaurante.",
    storePhone,
    onboarding.url,
  ));
}

async function evolutionStoreStatus(req: Request, config: EvolutionConfig) {
  const payload = await readJson(req) as WhatsAppActivationPayload;
  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return json(fail(context.message), context.status);
  }

  const requestedPhone = normalizePhone(payload.storePhone);
  const registeredPhone = normalizePhone(context.license.whatsAppPhone);
  const storePhone = registeredPhone || requestedPhone;
  const instanceName = await evolutionInstanceName(context.licenseKey);
  const state = await evolutionConnectionState(config, instanceName);
  if (state.ok && state.data.state === "open") {
    if (!phoneNumbersMatch(registeredPhone, requestedPhone)) {
      return json(fail(
        `Esta licenca esta conectada ao WhatsApp ${maskPhone(registeredPhone)}.`,
      ), 409);
    }
    applyEvolutionActive(context.license, storePhone, instanceName);
    appendEvent(context.store, payload, "whatsapp.evolution.active", "WhatsApp Evolution conectado.");
    await saveAdminStore(context.store);
    return json(active("WhatsApp conectado pela Evolution.", storePhone));
  }

  if (!state.ok && !state.notFound) {
    return json(fail(state.message), 502);
  }

  const onboarding = storePhone
    ? await createEvolutionOnboardingSession(req, payload, storePhone)
    : null;
  return json(pending(
    storePhone
      ? "WhatsApp ainda precisa ser conectado pelo QR Code."
      : "Informe o numero da loja para conectar WhatsApp.",
    storePhone,
    onboarding?.ok ? onboarding.url : "",
  ));
}

async function sendEvolutionMessage(req: Request, config: EvolutionConfig) {
  const payload = await readJson(req) as WhatsAppSendPayload;
  const customerPhone = normalizePhone(payload.customerPhone);
  const message = String(payload.message ?? "").trim();
  if (!customerPhone) {
    return json(fail("Cliente sem telefone valido para WhatsApp."), 400);
  }
  if (!message) {
    return json(fail("Mensagem do WhatsApp vazia."), 400);
  }

  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return json(fail(context.message), context.status);
  }

  const storePhone = normalizePhone(context.license.whatsAppPhone || payload.storePhone);
  const instanceName = await evolutionInstanceName(context.licenseKey);
  const sendId = evolutionSendId(context.licenseKey, payload.messageId);
  if (sendId && hasProcessedEvolutionSend(context.store, sendId)) {
    return json(active("WhatsApp ja havia sido enviado.", storePhone));
  }

  const sent = await sendEvolutionText(config, instanceName, customerPhone, message);
  if (!sent.ok) {
    if (sent.status !== 502) {
      const state = await evolutionConnectionState(config, instanceName);
      if ((state.ok && state.data.state !== "open") || (!state.ok && state.notFound)) {
        const onboarding = storePhone
          ? await createEvolutionOnboardingSession(req, payload, storePhone)
          : null;
        applyEvolutionPending(
          context.license,
          storePhone,
          instanceName,
          "Numero da loja ainda nao conectado na Evolution.",
        );
        appendEvent(context.store, payload, "whatsapp.evolution.onboarding_required",
          `WhatsApp nao enviado. Conecte o QR Code antes de enviar para ${maskPhone(customerPhone)}`);
        await saveAdminStore(context.store);
        return json(pending(
          "Conecte o WhatsApp pelo QR Code antes de enviar mensagens.",
          storePhone,
          onboarding?.ok ? onboarding.url : "",
        ), 428);
      }
    }

    await saveEvolutionError(context, payload, sent.message, "whatsapp.evolution.send_failed");
    return json(fail(sent.message), 502);
  }

  if (sendId) markProcessedEvolutionSend(context.store, sendId);
  applyEvolutionActive(context.license, storePhone, instanceName);
  appendEvent(context.store, payload, "whatsapp.evolution.sent",
    `WhatsApp enviado para ${maskPhone(customerPhone)}`);
  await saveAdminStoreAfterSend(context.store);
  return json(active("WhatsApp enviado.", storePhone));
}

async function listEvolutionMessages(req: Request, config: EvolutionConfig) {
  const payload = await readJson(req) as WhatsAppMessagesPayload;
  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return privateJson(fail(context.message), context.status);
  }

  const limit = normalizeEvolutionMessageLimit(payload.limit);
  const instanceName = await evolutionInstanceName(context.licenseKey);
  const result = await findEvolutionMessages(config, instanceName, limit);
  if (!result.ok) {
    return result.notFound
      ? privateJson(fail("WhatsApp ainda nao conectado para esta licenca."), 404)
      : privateJson(fail(result.message), 502);
  }

  const messages = extractEvolutionMessages(result.data, limit);
  return privateJson({
    ok: true,
    pending: false,
    messages,
    count: messages.length,
  });
}

async function disconnectEvolutionStorePhone(req: Request, config: EvolutionConfig) {
  const payload = await readJson(req) as ClientPayload;
  const context = await loadValidatedLicense(payload);
  if (!context.ok) {
    return privateJson(fail(context.message), context.status);
  }

  const instanceName = await evolutionInstanceName(context.licenseKey);
  const disconnected = await disconnectEvolutionInstance(config, instanceName);
  if (!disconnected.ok) {
    await saveEvolutionError(
      context,
      payload,
      disconnected.message,
      "whatsapp.evolution.disconnect_failed",
    );
    return privateJson(fail(disconnected.message), 502);
  }

  clearEvolutionConnection(context.license);
  appendEvent(
    context.store,
    payload,
    "whatsapp.evolution.disconnected",
    "WhatsApp Evolution desconectado e instancia removida.",
  );
  await saveAdminStore(context.store);
  return privateJson(active("WhatsApp desconectado.", ""));
}

async function startEvolutionOnboarding(url: URL, config: EvolutionConfig) {
  const stateKey = String(url.searchParams.get("state") ?? "").trim();
  if (!stateKey) {
    return text("Link invalido. Abra a conexao pelo botao WhatsApp dentro do PDV.", 400);
  }

  const stateContext = await readOnboardingState(stateKey);
  if (!stateContext.ok) {
    return text(`Link indisponivel. ${stateContext.message}`, 400);
  }

  const validation = await loadValidatedLicense({
    licenseKey: stateContext.state.license_key,
    machineHash: stateContext.state.machine_hash,
  });
  if (!validation.ok) {
    return text(`Licenca recusada. ${validation.message}`, validation.status);
  }

  const storePhone = normalizePhone(stateContext.state.store_phone || validation.license.whatsAppPhone);
  const instanceName = await evolutionInstanceName(validation.licenseKey);
  const instance = await ensureEvolutionInstance(config, instanceName);
  if (!instance.ok) {
    return text(`Evolution indisponivel. ${instance.message} Atualize esta pagina para tentar novamente.`, 502);
  }

  if (instance.state === "open") {
    applyEvolutionActive(validation.license, storePhone, instanceName);
    appendEvent(validation.store, { licenseKey: validation.licenseKey },
      "whatsapp.evolution.connected", `WhatsApp Evolution conectado: ${maskPhone(storePhone)}`);
    await saveAdminStore(validation.store);
    await consumeOnboardingState(stateKey);
    return text("WhatsApp conectado. Pode fechar esta tela e voltar para o PDV.");
  }

  const connection = await connectEvolutionInstance(config, instanceName);
  if (!connection.ok) {
    return text(`Nao consegui gerar o QR Code. ${connection.message} Atualize esta pagina para tentar novamente.`, 502);
  }

  const qr = extractEvolutionQr(connection.data);
  const image = decodeEvolutionQrImage(qr.image);
  if (image) {
    return evolutionQrImageResponse(image);
  }
  if (qr.pairingCode) {
    return text(`Codigo de pareamento do WhatsApp: ${qr.pairingCode}`);
  }
  return text("Aguardando a Evolution gerar o QR Code. Atualize esta pagina em alguns segundos.", 202);
}

async function ensureEvolutionInstance(config: EvolutionConfig, instanceName: string): Promise<
  | { ok: true; state: string }
  | { ok: false; message: string }
> {
  const current = await evolutionConnectionState(config, instanceName);
  if (current.ok) {
    return { ok: true, state: current.data.state };
  }
  if (!current.notFound) {
    return { ok: false, message: current.message };
  }

  const created = await createEvolutionInstance(config, instanceName);
  if (created.ok) {
    return { ok: true, state: "close" };
  }

  // Outro /activate pode ter criado a mesma instancia ao mesmo tempo.
  const raced = await evolutionConnectionState(config, instanceName);
  return raced.ok
    ? { ok: true, state: raced.data.state }
    : { ok: false, message: created.message };
}

function applyEvolutionActive(license: AdminLicense, storePhone: string, instanceName: string) {
  license.whatsAppPhone = normalizePhone(storePhone || license.whatsAppPhone);
  license.whatsAppBotId = instanceName;
  license.whatsAppStatus = "ATIVO";
  license.whatsAppLastError = "";
  license.whatsAppActivatedAt ??= new Date().toISOString();
  license.whatsAppConnectedAt = new Date().toISOString();
  license.whatsAppWabaId = "";
  license.whatsAppBusinessId = "";
  license.whatsAppPhoneNumberId = "";
  license.whatsAppDisplayPhone = normalizePhone(storePhone);
}

function phoneNumbersMatch(left: string, right: string) {
  const normalizedLeft = normalizePhone(left);
  const normalizedRight = normalizePhone(right);
  return !normalizedLeft || !normalizedRight || normalizedLeft === normalizedRight ||
    normalizedLeft.endsWith(normalizedRight) || normalizedRight.endsWith(normalizedLeft);
}

function applyEvolutionPending(
  license: AdminLicense,
  storePhone: string,
  instanceName: string,
  error: string,
) {
  license.whatsAppPhone = normalizePhone(storePhone || license.whatsAppPhone);
  license.whatsAppBotId = instanceName;
  license.whatsAppStatus = "AGUARDANDO_EVOLUTION";
  license.whatsAppLastError = error;
  license.whatsAppRequestedAt = new Date().toISOString();
}

function clearEvolutionConnection(license: AdminLicense) {
  license.whatsAppPhone = "";
  license.whatsAppBotId = "";
  license.whatsAppStatus = "INATIVO";
  license.whatsAppLastError = "";
  license.whatsAppRequestedAt = "";
  license.whatsAppActivatedAt = "";
  license.whatsAppWabaId = "";
  license.whatsAppBusinessId = "";
  license.whatsAppPhoneNumberId = "";
  license.whatsAppDisplayPhone = "";
  license.whatsAppConnectedAt = "";
}

async function saveEvolutionError(
  context: { store: AdminStore; license: AdminLicense },
  payload: ClientPayload,
  message: string,
  eventType: string,
) {
  context.license.whatsAppStatus = "ERRO_EVOLUTION";
  context.license.whatsAppLastError = message;
  appendEvent(context.store, payload, eventType, `WhatsApp Evolution falhou: ${message}`);
  await saveAdminStore(context.store);
}

function evolutionSendId(licenseKey: string, messageId: unknown) {
  const id = String(messageId ?? "").trim().replace(/[^a-zA-Z0-9_-]/g, "").slice(0, 128);
  return id ? `${normalizeLicense(licenseKey)}:${id}` : "";
}

function hasProcessedEvolutionSend(store: AdminStore, sendId: string) {
  return (store.whatsAppProcessedSendIds ?? []).includes(sendId);
}

function markProcessedEvolutionSend(store: AdminStore, sendId: string) {
  store.whatsAppProcessedSendIds ??= [];
  store.whatsAppProcessedSendIds = [sendId, ...store.whatsAppProcessedSendIds.filter((id) => id !== sendId)]
    .slice(0, 500);
}

async function saveAdminStoreAfterSend(store: AdminStore) {
  try {
    await Promise.race([
      saveAdminStore(store),
      new Promise((_, reject) => setTimeout(() => reject(new Error("timeout")), 1_500)),
    ]);
  } catch (error) {
    console.error("whatsapp.evolution.send_log_failed", messageFromError(error));
  }
}

async function sendMetaText(
  phone: string,
  body: string,
  options: { store?: AdminStore | null; connection?: WhatsAppStoreConnection | null } = {},
) {
  const token = normalizeBearer(options.connection?.access_token ?? "") || metaAccessToken(options.store ?? null) || await metaAccessTokenFromStore();
  if (!token) {
    return { ok: false, message: "Token da Meta nao configurado no Supabase." };
  }

  const fromPhoneNumberId = String(options.connection?.phone_number_id || phoneNumberId()).trim();
  if (!fromPhoneNumberId) {
    return { ok: false, message: "Phone Number ID da Meta nao configurado." };
  }

  const response = await fetch(`https://graph.facebook.com/${graphVersion()}/${fromPhoneNumberId}/messages`, {
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
    const connection = await readStoreConnectionByPhoneNumber(event.phoneNumberId);
    const license = store
      ? connection?.license_key
        ? findLicense(store, normalizeLicense(connection.license_key))
        : findWhatsAppLicense(store, event.businessPhone, event.phoneNumberId)
      : null;
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

    if (event.rawMessage && shouldAutoReply(event, store, connection)) {
      const reply = await buildIncomingReply(event, license);
      const sent = await sendMetaText(event.customerPhone, reply, { store, connection });
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

function shouldAutoReply(event: MetaWebhookEvent, store: AdminStore | null, connection: WhatsAppStoreConnection | null) {
  if (!event.customerPhone || !event.messageId || event.messageType !== "text") {
    return false;
  }

  if (!autoReplyEnabled() || !connectionUsable(connection ?? {})) {
    return false;
  }

  return store ? !hasProcessedWebhookMessage(store, event.messageId) : false;
}

async function buildIncomingReply(event: MetaWebhookEvent, license: AdminLicense | null) {
  const licenseKey = normalizeLicense(license?.key);
  const menu = licenseKey ? await readPublicMenuForBot(licenseKey) : null;
  const text = normalizeBotText(event.text);
  const storeName = firstNonEmpty(menu?.name, license?.businessName, license?.customerName, "nossa loja");

  if (isBotIntent(text, ["atendente", "humano", "pessoa", "suporte", "falar com alguem", "falar com atendente"])) {
    return buildHumanReply(storeName);
  }

  if (isBotIntent(text, ["cancelar", "cancelamento", "cancela", "alterar", "mudar pedido", "trocar pedido"])) {
    return [
      `Entendi. Vou chamar o atendimento da ${storeName} para conferir isso com voce.`,
      "Se for um pedido em andamento, aguarde a confirmacao da loja antes de refazer ou cancelar.",
    ].join("\n");
  }

  if (isBotIntent(text, [
    "status",
    "status do pedido",
    "meu pedido",
    "acompanhar pedido",
    "andamento",
    "pedido a caminho",
    "cade meu pedido",
    "chegou",
    "caminho",
    "rota",
    "saiu",
  ])) {
    const order = licenseKey ? await readLatestPublicOrderForCustomer(licenseKey, event.customerPhone) : null;
    return buildOrderStatusReply(storeName, menu, order);
  }

  if (isBotIntent(text, ["horario", "hora", "aberto", "fechado", "funciona", "funcionamento", "atendendo"])) {
    return buildStoreHoursReply(storeName, menu);
  }

  if (isBotIntent(text, ["cardapio", "cardápio", "menu", "catalogo", "catalogo", "preco", "precos", "valor"])) {
    return buildGreetingReply(storeName, menu);
  }

  if (isBotIntent(text, ["delivery", "entrega", "retirada", "retirar", "buscar", "balcao", "mesa", "comanda", "quero pedir", "fazer pedido", "pedir"])) {
    return buildOrderStartReply(storeName, menu);
  }

  if (isBotIntent(text, ["pix", "cartao", "cartao", "debito", "credito", "dinheiro", "pagamento", "pagar"])) {
    return [
      `Na ${storeName}, a forma de pagamento pode ser combinada no pedido.`,
      "Para pedir pelo cardapio online, acesse:",
      publicMenuUrlLine(menu),
      "Se preferir, envie ATENDENTE que alguem da loja continua por aqui.",
    ].filter(Boolean).join("\n");
  }

  if (isSimpleGreetingIntent(text)) {
    return buildGreetingReply(storeName, menu);
  }

  if (isThanksIntent(text)) {
    return `Por nada! Quando precisar, envie CARDAPIO para ver os produtos ou STATUS para acompanhar seu pedido na ${storeName}.`;
  }

  return buildFallbackReply(storeName, menu);
}

async function readPublicMenuForBot(licenseKey: string) {
  const supabase = serviceClient();
  if (!supabase || !licenseKey) return null;

  const { data, error } = await supabase
    .from("bv_public_menus")
    .select("id, store_id, slug, name, description, phone, address, city, state, store_open, wait_min_minutes, wait_max_minutes, is_published")
    .eq("store_id", licenseKey)
    .eq("is_published", true)
    .order("updated_at", { ascending: false })
    .limit(1)
    .maybeSingle();

  if (error) {
    console.error("whatsapp.bot.menu_lookup_failed", error.message);
    return null;
  }

  return data as PublicMenuBotSnapshot | null;
}

async function readLatestPublicOrderForCustomer(licenseKey: string, customerPhone: string) {
  const supabase = serviceClient();
  const phone = normalizePhone(customerPhone);
  if (!supabase || !licenseKey || !phone) return null;

  const since = new Date(Date.now() - BOT_ORDER_LOOKBACK_DAYS * 24 * 60 * 60 * 1000).toISOString();
  const { data, error } = await supabase
    .from("bv_public_orders")
    .select("id, status, order_type, customer_phone, table_label, address, total, pdv_order_id, created_at, updated_at")
    .eq("store_id", licenseKey)
    .gte("created_at", since)
    .order("created_at", { ascending: false })
    .limit(30);

  if (error) {
    console.error("whatsapp.bot.order_lookup_failed", error.message);
    return null;
  }

  return ((data ?? []) as PublicOrderBotSnapshot[])
    .find((order) => phoneMatches(phone, order.customer_phone)) ?? null;
}

function buildGreetingReply(storeName: string, menu: PublicMenuBotSnapshot | null) {
  return [
    `${greetingForNow()}! Bem-vindo ao atendimento da ${storeName}.`,
    "Cardapio online:",
    publicMenuUrlLine(menu),
    storeOpenLine(menu),
    "Para acompanhar um pedido, envie STATUS. Para falar com alguem da loja, envie ATENDENTE.",
  ].filter(Boolean).join("\n");
}

function buildOrderStartReply(storeName: string, menu: PublicMenuBotSnapshot | null) {
  return [
    `Para fazer seu pedido na ${storeName}, abra o cardapio online:`,
    publicMenuUrlLine(menu),
    storeOpenLine(menu),
    "No cardapio voce escolhe entrega, retirada, mesa ou comanda quando estiver disponivel.",
  ].filter(Boolean).join("\n");
}

function buildStoreHoursReply(storeName: string, menu: PublicMenuBotSnapshot | null) {
  if (!menu) {
    return [
      `${storeName} ainda nao tem cardapio online publicado neste numero.`,
      "Envie ATENDENTE para confirmar o horario diretamente com a loja.",
    ].join("\n");
  }

  if (menu.store_open === false) {
    return [
      `No momento a ${storeName} esta fechada ou fora do horario de atendimento online.`,
      "Voce ainda pode consultar o cardapio:",
      publicMenuUrlLine(menu),
      "Quando a loja voltar, os pedidos online ficam disponiveis novamente.",
    ].join("\n");
  }

  return [
    `${storeName} esta atendendo online agora.`,
    waitTimeLine(menu),
    "Cardapio:",
    publicMenuUrlLine(menu),
  ].filter(Boolean).join("\n");
}

function buildOrderStatusReply(storeName: string, menu: PublicMenuBotSnapshot | null, order: PublicOrderBotSnapshot | null) {
  if (!order) {
    return [
      "Nao encontrei um pedido recente para este WhatsApp.",
      "Se voce acabou de pedir, aguarde alguns instantes e envie STATUS novamente.",
      "Para fazer um novo pedido:",
      publicMenuUrlLine(menu),
      "Para atendimento humano, envie ATENDENTE.",
    ].filter(Boolean).join("\n");
  }

  const orderId = shortOrderId(order.id);
  const status = normalizeOrderStatus(order.status);
  const phrase = orderStatusPhrase(status, order.order_type);
  const total = Number(order.total ?? 0) > 0 ? `Total: ${moneyText(Number(order.total))}.` : "";
  const pdv = order.pdv_order_id ? `Referencia no PDV: ${order.pdv_order_id}.` : "";
  return [
    `Pedido ${orderId} - ${phrase}`,
    total,
    pdv,
    status === "DESPACHADO" || status === "ROTA"
      ? "Ele saiu para entrega e esta a caminho."
      : "",
    status === "PRONTO"
      ? "Se for retirada, pode ir ate a loja. Se for entrega, aguarde a saida para rota."
      : "",
    status === "CANCELADO" || status === "ERRO"
      ? `Se precisar conferir, envie ATENDENTE para falar com a ${storeName}.`
      : "",
  ].filter(Boolean).join("\n");
}

function buildHumanReply(storeName: string) {
  return [
    `Certo. Vou deixar registrado para o atendimento da ${storeName} continuar por aqui.`,
    "Se for urgente, envie tambem o numero do pedido ou o nome usado no pedido.",
  ].join("\n");
}

function buildFallbackReply(storeName: string, menu: PublicMenuBotSnapshot | null) {
  return [
    `Recebi sua mensagem no atendimento da ${storeName}.`,
    "Para ver produtos e fazer pedido, envie CARDAPIO ou acesse:",
    publicMenuUrlLine(menu),
    "Para acompanhar pedido, envie STATUS. Para falar com atendente, envie ATENDENTE.",
  ].filter(Boolean).join("\n");
}

function publicMenuUrlLine(menu: PublicMenuBotSnapshot | null) {
  const slug = normalizePublicMenuSlug(menu?.slug);
  return slug ? `${PUBLIC_MENU_BASE_URL}/${slugToPath(slug)}` : "Cardapio online ainda nao publicado.";
}

function storeOpenLine(menu: PublicMenuBotSnapshot | null) {
  if (!menu) return "";
  if (menu.store_open === false) {
    return "A loja online esta fechada agora. O cardapio fica disponivel para consulta.";
  }

  return `Loja online aberta. ${waitTimeLine(menu)}`.trim();
}

function waitTimeLine(menu: PublicMenuBotSnapshot | null) {
  if (!menu) return "";
  const min = Math.max(1, Math.round(Number(menu.wait_min_minutes ?? 30) || 30));
  const max = Math.max(min, Math.round(Number(menu.wait_max_minutes ?? 60) || 60));
  if (!min || !max) return "";
  return min === max
    ? `Previsao media: ${min} min.`
    : `Previsao media: ${min} a ${max} min.`;
}

function orderStatusPhrase(status: string, orderType: unknown) {
  const type = String(orderType ?? "").toUpperCase();
  const delivery = type === "DELIVERY";
  switch (status) {
    case "NOVO":
    case "RECEBIDO":
    case "IMPORTADO":
    case "CONFIRMADO":
    case "ACEITO":
      return "recebido pela loja e aguardando preparo.";
    case "AGUARDANDO":
    case "PENDENTE":
    case "PENDING":
      return "aguardando confirmacao da loja.";
    case "PREPARO":
    case "PREPARANDO":
      return "em preparo.";
    case "PRONTO":
      return delivery ? "pronto e aguardando sair para entrega." : "pronto para retirada/consumo.";
    case "ROTA":
    case "DESPACHADO":
      return "saiu para entrega.";
    case "ENTREGUE":
    case "FINALIZADO":
      return "entregue/finalizado.";
    case "CANCELADO":
    case "CANCELAMENTO":
    case "EXPIRADO":
    case "ERRO":
      return "cancelado.";
    default:
      return "em acompanhamento pela loja.";
  }
}

function normalizeOrderStatus(value: unknown) {
  const status = String(value ?? "").toUpperCase().replace(/[^A-Z0-9]+/g, "_");
  return status === "IN_DELIVERY" || status === "ON_THE_WAY" ? "DESPACHADO" : status;
}

function shortOrderId(value: unknown) {
  const clean = String(value ?? "").trim();
  return clean ? clean.split("-")[0].toUpperCase() : "ONLINE";
}

function moneyText(value: number) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}

function phoneMatches(left: string, right: unknown) {
  const a = normalizePhone(left);
  const b = normalizePhone(right);
  return Boolean(a && b && (a === b || a.endsWith(b) || b.endsWith(a)));
}

function normalizePublicMenuSlug(value: unknown) {
  return String(value ?? "")
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function slugToPath(slug: string) {
  return encodeURIComponent(slug).replace(/%2F/gi, "/");
}

function normalizeBotText(value: unknown) {
  return normalizeText(value)
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function isBotIntent(text: string, terms: string[]) {
  return terms.some((term) => {
    const normalized = normalizeBotText(term);
    return normalized && (` ${text} `).includes(` ${normalized} `);
  });
}

function isSimpleGreetingIntent(text: string) {
  return [
    "oi",
    "ola",
    "bom dia",
    "boa tarde",
    "boa noite",
    "e ai",
    "quero atendimento",
  ].includes(text);
}

function isThanksIntent(text: string) {
  return ["obrigado", "obrigada", "valeu", "blz", "beleza"].includes(text);
}

function greetingForNow() {
  const hour = Number(new Intl.DateTimeFormat("pt-BR", {
    timeZone: "America/Sao_Paulo",
    hour: "2-digit",
    hour12: false,
  }).format(new Date()));
  if (hour < 12) return "Bom dia";
  if (hour < 18) return "Boa tarde";
  return "Boa noite";
}

function findWhatsAppLicense(store: AdminStore, businessPhone: string, phoneNumberIdValue = "") {
  const normalizedBusinessPhone = normalizePhone(businessPhone);
  const normalizedPhoneNumberId = String(phoneNumberIdValue ?? "").trim();
  if (!normalizedBusinessPhone && !normalizedPhoneNumberId) return null;

  return (store.licenses ?? []).find((license) =>
    (normalizedBusinessPhone && normalizePhone(license.whatsAppPhone) === normalizedBusinessPhone)
      || (normalizedPhoneNumberId && String(license.whatsAppPhoneNumberId ?? "") === normalizedPhoneNumberId)
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

async function _validateAndUpdateLicense(
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
  const license = await findOrRegisterLicense(store, payload, licenseKey);
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
  if (licenseMachine && licenseMachine !== requestMachine) {
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
    store.whatsAppProcessedSendIds ??= [];
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

async function readStoreConnection(licenseKey: string): Promise<{ ok: true; connection: WhatsAppStoreConnection | null } | { ok: false; message: string }> {
  const supabase = serviceClient();
  if (!supabase) return { ok: false, message: "Supabase service role indisponivel." };

  const { data, error } = await supabase
    .from("balcao_whatsapp_store_connections")
    .select("*")
    .eq("license_key", normalizeLicense(licenseKey))
    .maybeSingle();

  if (error) return { ok: false, message: error.message };
  return { ok: true, connection: data as WhatsAppStoreConnection | null };
}

async function readStoreConnectionByPhoneNumber(phoneNumberIdValue: string) {
  const supabase = serviceClient();
  if (!supabase || !phoneNumberIdValue) return null;

  const { data, error } = await supabase
    .from("balcao_whatsapp_store_connections")
    .select("*")
    .eq("phone_number_id", phoneNumberIdValue)
    .maybeSingle();

  if (error) {
    console.error("whatsapp.meta.connection_lookup_failed", error.message);
    return null;
  }

  return data as WhatsAppStoreConnection | null;
}

async function saveStoreConnection(connection: WhatsAppStoreConnection) {
  const supabase = serviceClient();
  if (!supabase) return { ok: false, message: "Supabase service role indisponivel." };

  const { error } = await supabase
    .from("balcao_whatsapp_store_connections")
    .upsert({
      ...connection,
      license_key: normalizeLicense(connection.license_key),
      store_phone: normalizePhone(connection.store_phone),
      updated_at: new Date().toISOString(),
    }, { onConflict: "license_key" });

  return error ? { ok: false, message: error.message } : { ok: true, message: "" };
}

function createOnboardingSession(req: Request, payload: ClientPayload, storePhone: string) {
  if (!metaAppId() || !metaAppSecret() || !embeddedSignupConfigId()) {
    return {
      ok: false,
      message: "Configuracao Meta incompleta. Falta App ID, App Secret ou Config ID do Embedded Signup.",
      url: "",
    };
  }

  return createOnboardingStateSession(req, payload, storePhone, "meta");
}

function createEvolutionOnboardingSession(
  req: Request,
  payload: ClientPayload,
  storePhone: string,
) {
  return createOnboardingStateSession(req, payload, storePhone, "evo");
}

async function createOnboardingStateSession(
  req: Request,
  payload: ClientPayload,
  storePhone: string,
  providerPrefix: "meta" | "evo",
) {

  const supabase = serviceClient();
  if (!supabase) {
    return { ok: false, message: "Supabase service role indisponivel.", url: "" };
  }

  const state = createOnboardingStateKey(providerPrefix === "evo" ? "evolution" : "meta");
  const expiresAt = new Date(Date.now() + ONBOARDING_STATE_MINUTES * 60_000).toISOString();
  const { error } = await supabase
    .from("balcao_whatsapp_onboarding_states")
    .insert({
      state,
      license_key: normalizeLicense(payload.licenseKey),
      machine_hash: String(payload.machineHash ?? ""),
      store_phone: normalizePhone(storePhone),
      expires_at: expiresAt,
    });

  if (error) return { ok: false, message: error.message, url: "" };

  const url = new URL(functionRouteUrl(new URL(req.url), "/onboarding/start"));
  url.searchParams.set("state", state);
  return { ok: true, message: "", url: url.toString() };
}

async function readOnboardingState(state: string): Promise<{ ok: true; state: WhatsAppOnboardingState } | { ok: false; message: string }> {
  const supabase = serviceClient();
  if (!supabase) return { ok: false, message: "Supabase service role indisponivel." };

  const { data, error } = await supabase
    .from("balcao_whatsapp_onboarding_states")
    .select("*")
    .eq("state", state)
    .maybeSingle();

  if (error || !data) return { ok: false, message: error?.message ?? "Link de conexao nao encontrado." };

  const row = data as WhatsAppOnboardingState;
  if (row.expires_at && Date.parse(row.expires_at) <= Date.now()) {
    return { ok: false, message: "Esse link expirou. Gere outro pelo PDV." };
  }

  return { ok: true, state: row };
}

async function consumeOnboardingState(state: string) {
  const result = await readOnboardingState(state);
  if (!result.ok) return result;

  const supabase = serviceClient();
  if (supabase) {
    await supabase.from("balcao_whatsapp_onboarding_states").delete().eq("state", state);
  }

  return result;
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

async function findOrRegisterLicense(store: AdminStore, payload: ClientPayload, licenseKey: string) {
  const existing = findLicense(store, licenseKey);
  if (existing) {
    return existing;
  }

  const validation = await validateSignedActivationLicense(licenseKey);
  if (!validation.ok) {
    return null;
  }

  const profile = asRecord(payload.profile);
  const now = new Date().toISOString();
  const license: AdminLicense = {
    id: crypto.randomUUID(),
    key: licenseKey,
    status: "ATIVA",
    plan: stringFrom(payload.localPlan) || validation.plan,
    customerName: firstNonEmpty(profile.businessName, profile.ownerName, profile.email),
    email: stringFrom(profile.email).toLowerCase(),
    businessName: stringFrom(profile.businessName),
    ownerName: stringFrom(profile.ownerName),
    cnpj: stringFrom(profile.cnpj),
    phone: stringFrom(profile.phone),
    city: stringFrom(profile.city),
    state: stringFrom(profile.state),
    machineHash: stringFrom(payload.machineHash),
    machineCode: stringFrom(payload.machineCode),
    appVersion: stringFrom(payload.appVersion),
    createdAt: now,
    activatedAt: now,
    lastSeenAt: now,
    expiresAt: pickValidExpiration(payload.localExpiresAt, validation.expiresAt),
  };

  store.licenses ??= [];
  store.licenses.unshift(license);
  appendEvent(store, payload, "license.auto_registered", "Licenca registrada automaticamente no Supabase pelo PDV.");
  return license;
}

async function validateSignedActivationLicense(licenseKey: string): Promise<
  | { ok: true; expiresAt: string; plan: string }
  | { ok: false }
> {
  const normalized = normalizeLicense(licenseKey);
  if (!normalized) return { ok: false };

  if (normalized === "BL-TESTE-2026") {
    return {
      ok: true,
      expiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      plan: "Teste 30 dias",
    };
  }

  const parts = normalized.split("-").filter(Boolean);
  if (parts.length === 4 && parts[0] === "BLV") {
    const expiresAt = parseActivationExpiration(parts[1]);
    if (!expiresAt || expiresAt.getTime() <= Date.now()) return { ok: false };

    const expected = (await activationSignature(`BLV|${parts[1]}|${parts[2]}`)).slice(0, 10);
    return expected === parts[3].toUpperCase()
      ? { ok: true, expiresAt: expiresAt.toISOString(), plan: "Licenca comercial" }
      : { ok: false };
  }

  if (parts.length === 3 && parts[0] === "BL") {
    const expiresAt = parseActivationExpiration(parts[1]);
    if (!expiresAt || expiresAt.getTime() <= Date.now()) return { ok: false };

    const expected = (await activationSignature(`BL|${parts[1]}`)).slice(0, 8);
    return expected === parts[2].toUpperCase()
      ? { ok: true, expiresAt: expiresAt.toISOString(), plan: "Licenca comercial" }
      : { ok: false };
  }

  return { ok: false };
}

function parseActivationExpiration(value: string) {
  if (!/^\d{8}(\d{4})?$/.test(value)) {
    return null;
  }

  const year = Number(value.slice(0, 4));
  const month = Number(value.slice(4, 6));
  const day = Number(value.slice(6, 8));
  if (!year || month < 1 || month > 12 || day < 1 || day > 31) {
    return null;
  }

  if (value.length === 8) {
    return new Date(Date.UTC(year, month - 1, day, 26, 59, 59, 999));
  }

  const hour = Number(value.slice(8, 10));
  const minute = Number(value.slice(10, 12));
  if (hour > 23 || minute > 59) {
    return null;
  }

  return new Date(Date.UTC(year, month - 1, day, hour + 3, minute, 0, 0));
}

async function activationSignature(message: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode("BalcaoLivrePDV-local-license-v1"),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
  return Array.from(new Uint8Array(signature))
    .map((value) => value.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

function pickValidExpiration(localExpiresAt: unknown, signedExpiresAt: string) {
  const localText = stringFrom(localExpiresAt);
  const signedTime = Date.parse(signedExpiresAt);
  const localTime = Date.parse(localText);
  if (Number.isFinite(localTime) && localTime > Date.now() && localTime <= signedTime) {
    return new Date(localTime).toISOString();
  }

  return signedExpiresAt;
}

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : {};
}

function stringFrom(value: unknown) {
  return String(value ?? "").trim();
}

function firstNonEmpty(...values: unknown[]) {
  for (const value of values) {
    const text = stringFrom(value);
    if (text) return text;
  }

  return "";
}

async function loadValidatedLicense(payload: ClientPayload): Promise<
  | { ok: true; store: AdminStore; license: AdminLicense; licenseKey: string }
  | { ok: false; status: number; message: string }
> {
  const licenseKey = normalizeLicense(payload.licenseKey);
  if (!licenseKey) {
    return { ok: false, status: 401, message: "Licenca obrigatoria para usar WhatsApp automatico." };
  }

  const context = await readAdminStore();
  if (!context.ok) {
    return { ok: false, status: 503, message: context.message };
  }

  const license = await findOrRegisterLicense(context.store, payload, licenseKey);
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
  if (licenseMachine && licenseMachine !== requestMachine) {
    return { ok: false, status: 403, message: "Licenca pertence a outro computador." };
  }

  return { ok: true, store: context.store, license, licenseKey };
}

function applyConnectionToLicense(license: AdminLicense, connection: WhatsAppStoreConnection, storePhone: string) {
  license.whatsAppPhone = normalizePhone(storePhone || connection.store_phone || license.whatsAppPhone);
  license.whatsAppBotId = CENTRAL_BOT_ID;
  license.whatsAppStatus = "ATIVO";
  license.whatsAppLastError = "";
  license.whatsAppActivatedAt ??= new Date().toISOString();
  license.whatsAppConnectedAt = connection.connected_at || new Date().toISOString();
  license.whatsAppWabaId = String(connection.waba_id ?? "");
  license.whatsAppBusinessId = String(connection.business_id ?? "");
  license.whatsAppPhoneNumberId = String(connection.phone_number_id ?? "");
  license.whatsAppDisplayPhone = String(connection.phone_display_number ?? "");
}

async function updateLicenseConnection(connection: WhatsAppStoreConnection) {
  const context = await readAdminStore();
  if (!context.ok) return;

  const license = findLicense(context.store, normalizeLicense(connection.license_key));
  if (!license) return;

  applyConnectionToLicense(license, connection, normalizePhone(connection.store_phone));
  appendEvent(context.store, { licenseKey: license.key, machineHash: connection.machine_hash },
    "whatsapp.meta.connected", `WhatsApp Meta conectado: ${maskPhone(normalizePhone(connection.store_phone))}`);
  await saveAdminStore(context.store);
}

function connectionUsable(connection: WhatsAppStoreConnection) {
  return Boolean(
    normalizeBearer(connection.access_token ?? "")
      && String(connection.phone_number_id ?? "").trim()
      && String(connection.status ?? "ATIVO").toUpperCase() !== "BLOQUEADA"
  );
}

function connectionUsableForPhone(connection: WhatsAppStoreConnection, storePhone: string) {
  if (!connectionUsable(connection)) return false;
  const left = normalizePhone(storePhone);
  const right = normalizePhone(connection.store_phone || connection.phone_display_number);
  return !left || !right || left === right || left.endsWith(right) || right.endsWith(left);
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

function metaAppId() {
  return String(Deno.env.get("META_WHATSAPP_APP_ID")
    ?? Deno.env.get("WHATSAPP_META_APP_ID")
    ?? DEFAULT_META_APP_ID).trim();
}

function metaAppSecret() {
  return String(Deno.env.get("META_WHATSAPP_APP_SECRET")
    ?? Deno.env.get("WHATSAPP_META_APP_SECRET")
    ?? "").trim();
}

function embeddedSignupConfigId() {
  return String(Deno.env.get("META_WHATSAPP_EMBEDDED_CONFIG_ID")
    ?? Deno.env.get("WHATSAPP_META_EMBEDDED_CONFIG_ID")
    ?? Deno.env.get("META_WHATSAPP_CONFIG_ID")
    ?? "").trim();
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

async function exchangeMetaCode(code: string, redirectUri = "") {
  const appId = metaAppId();
  const secret = metaAppSecret();
  if (!appId || !secret) {
    return { ok: false as const, message: "App Secret da Meta nao configurado na Supabase." };
  }

  const url = new URL(`https://graph.facebook.com/${graphVersion()}/oauth/access_token`);
  url.searchParams.set("client_id", appId);
  url.searchParams.set("client_secret", secret);
  url.searchParams.set("code", code);
  if (redirectUri) {
    url.searchParams.set("redirect_uri", redirectUri);
  }

  const response = await fetch(url);
  const text = await response.text();
  if (!response.ok) {
    return { ok: false as const, message: extractMetaError(response.status, text) };
  }

  try {
    const parsed = JSON.parse(text) as Record<string, unknown>;
    const accessToken = normalizeBearer(String(parsed.access_token ?? ""));
    if (!accessToken) return { ok: false as const, message: "Meta nao retornou token de acesso." };
    return {
      ok: true as const,
      accessToken,
      tokenType: String(parsed.token_type ?? ""),
      raw: parsed,
    };
  } catch (_error) {
    return { ok: false as const, message: "Resposta da Meta veio invalida." };
  }
}

async function resolveOnboardedPhone(accessToken: string, sessionInfo: Record<string, unknown>, wantedPhone: string) {
  const phoneNumberId = firstString(sessionInfo, [
    ["phone_number_id"],
    ["data", "phone_number_id"],
    ["phoneNumberId"],
    ["phone", "id"],
  ]);
  const wabaId = firstString(sessionInfo, [
    ["waba_id"],
    ["data", "waba_id"],
    ["whatsapp_business_account_id"],
    ["data", "whatsapp_business_account_id"],
    ["wabaId"],
  ]);
  const businessId = firstString(sessionInfo, [
    ["business_id"],
    ["data", "business_id"],
    ["businessId"],
  ]);
  const displayPhone = normalizePhone(firstString(sessionInfo, [
    ["phone_number"],
    ["display_phone_number"],
    ["data", "phone_number"],
    ["data", "display_phone_number"],
  ]));

  if (phoneNumberId) {
    return { ok: true as const, phoneNumberId, wabaId, businessId, displayPhone: displayPhone || wantedPhone };
  }

  const wabaIds = wabaId ? [wabaId] : await resolveWabaIdsFromToken(accessToken);
  if (wabaIds.length === 0) {
    return { ok: false as const, message: "Meta nao retornou WABA nem Phone Number ID." };
  }

  for (const candidateWabaId of wabaIds) {
    const resolved = await resolvePhoneFromWaba(accessToken, candidateWabaId, businessId, wantedPhone);
    if (resolved.ok) return resolved;
  }

  return { ok: false as const, message: "Nenhum numero WhatsApp encontrado nessa WABA." };
}

async function resolvePhoneFromWaba(accessToken: string, wabaId: string, businessId: string, wantedPhone: string) {
  const response = await fetch(`https://graph.facebook.com/${graphVersion()}/${wabaId}/phone_numbers?fields=id,display_phone_number,verified_name`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  const text = await response.text();
  if (!response.ok) {
    return { ok: false as const, message: extractMetaError(response.status, text) };
  }

  try {
    const parsed = JSON.parse(text) as { data?: Array<Record<string, unknown>> };
    const numbers = Array.isArray(parsed.data) ? parsed.data : [];
    const wanted = normalizePhone(wantedPhone);
    const match = numbers.find((item) => {
      const current = normalizePhone(item.display_phone_number);
      return wanted && current && (current === wanted || current.endsWith(wanted) || wanted.endsWith(current));
    }) ?? numbers[0];

    const id = String(match?.id ?? "");
    if (!id) return { ok: false as const, message: "Nenhum numero WhatsApp encontrado nessa WABA." };

    return {
      ok: true as const,
      phoneNumberId: id,
      wabaId,
      businessId,
      displayPhone: normalizePhone(match?.display_phone_number) || wanted,
    };
  } catch (_error) {
    return { ok: false as const, message: "Lista de numeros da Meta veio invalida." };
  }
}

async function resolveWabaIdsFromToken(accessToken: string) {
  const appId = metaAppId();
  const secret = metaAppSecret();
  if (!appId || !secret) return [];

  const url = new URL(`https://graph.facebook.com/${graphVersion()}/debug_token`);
  url.searchParams.set("input_token", accessToken);
  url.searchParams.set("access_token", `${appId}|${secret}`);

  const response = await fetch(url);
  const text = await response.text();
  if (!response.ok) {
    console.error("whatsapp.meta.debug_token_failed", extractMetaError(response.status, text));
    return [];
  }

  try {
    const parsed = JSON.parse(text) as { data?: { granular_scopes?: Array<{ scope?: string; target_ids?: string[] }> } };
    const scopes = Array.isArray(parsed.data?.granular_scopes) ? parsed.data?.granular_scopes ?? [] : [];
    return [...new Set(scopes
      .filter((scope) => String(scope.scope ?? "").includes("whatsapp"))
      .flatMap((scope) => Array.isArray(scope.target_ids) ? scope.target_ids : [])
      .map((id) => String(id ?? "").trim())
      .filter(Boolean))];
  } catch (_error) {
    return [];
  }
}

async function subscribeWaba(accessToken: string, wabaId: string) {
  if (!wabaId) return;
  const response = await fetch(`https://graph.facebook.com/${graphVersion()}/${wabaId}/subscribed_apps`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    console.error("whatsapp.meta.subscribe_failed", extractMetaError(response.status, await response.text()));
  }
}

function firstString(root: Record<string, unknown>, paths: string[][]) {
  for (const path of paths) {
    let current: unknown = root;
    for (const key of path) {
      current = current && typeof current === "object" ? (current as Record<string, unknown>)[key] : undefined;
    }
    const value = String(current ?? "").trim();
    if (value) return value;
  }
  return "";
}

function normalizeSessionInfo(value: unknown): Record<string, unknown> {
  if (typeof value === "string") {
    try {
      return JSON.parse(value) as Record<string, unknown>;
    } catch (_error) {
      return {};
    }
  }

  return value && typeof value === "object" ? value as Record<string, unknown> : {};
}

function active(message: string, storePhone: string, connection?: WhatsAppStoreConnection | null) {
  return {
    ok: true,
    pending: false,
    message,
    storePhone,
    phoneNumberId: connection?.phone_number_id ?? "",
    wabaId: connection?.waba_id ?? "",
  };
}

function pending(message: string, storePhone: string, onboardingUrl = "") {
  return { ok: false, pending: true, message, storePhone, onboardingUrl };
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

function privateJson(data: unknown, status = 200) {
  const response = json(data, status);
  response.headers.set("cache-control", "no-store, max-age=0");
  return response;
}

function text(body: string, status = 200) {
  const headers = new Headers(corsHeaders);
  headers.set("content-type", "text/plain; charset=utf-8");
  return new Response(body, { status, headers });
}

function evolutionQrImageResponse(image: EvolutionQrImage) {
  const headers = new Headers(corsHeaders);
  headers.set("content-type", image.contentType);
  headers.set("content-disposition", "inline; filename=whatsapp-qr.png");
  headers.set("cache-control", "no-store, max-age=0");
  headers.set("refresh", "8");
  return new Response(Uint8Array.from(image.bytes).buffer, {
    status: 200,
    headers,
  });
}

function _html(body: string, status = 200) {
  const headers = new Headers(corsHeaders);
  headers.set("content-type", "text/html; charset=utf-8");
  headers.set(
    "content-security-policy",
    [
      "default-src 'self' https://connect.facebook.net https://www.facebook.com https://web.facebook.com https://static.xx.fbcdn.net",
      "script-src 'self' 'unsafe-inline' https://connect.facebook.net",
      "style-src 'self' 'unsafe-inline'",
      "connect-src 'self' https://www.facebook.com https://web.facebook.com https://graph.facebook.com",
      "frame-src https://www.facebook.com https://web.facebook.com",
      "img-src 'self' data: https:",
    ].join("; "),
  );
  return new Response(body, {
    status,
    headers,
  });
}

function metaOAuthUrl(options: { appId: string; configId: string; graphVersion: string; state: string; redirectUri: string }) {
  const url = new URL(`https://www.facebook.com/${options.graphVersion}/dialog/oauth`);
  url.searchParams.set("client_id", options.appId);
  url.searchParams.set("redirect_uri", options.redirectUri);
  url.searchParams.set("state", options.state);
  url.searchParams.set("config_id", options.configId);
  url.searchParams.set("response_type", "code");
  url.searchParams.set("override_default_response_type", "true");
  url.searchParams.set("extras", JSON.stringify({
    feature: "whatsapp_embedded_signup",
    sessionInfoVersion: "3",
    setup: {},
  }));
  return url.toString();
}

function functionRouteUrl(url: URL, route: string) {
  const next = new URL(url.toString());
  const functionMarker = "/functions/v1/whatsapp";
  const functionIndex = url.pathname.indexOf(functionMarker);
  const pathname = functionIndex >= 0
    ? `${url.pathname.slice(0, functionIndex + functionMarker.length)}${route}`
    : `${functionMarker}${route}`;
  next.protocol = "https:";
  next.pathname = pathname;
  next.search = "";
  return next.toString();
}

function _onboardingMessagePage(title: string, message: string) {
  return `<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>${escapeHtml(title)}</title>
  <style>
    body{margin:0;font-family:Arial,sans-serif;background:#f4f7fb;color:#17212b;display:grid;place-items:center;min-height:100vh}
    main{width:min(520px,calc(100vw - 32px));background:#fff;border:1px solid #d8e3ef;border-radius:14px;padding:28px;box-shadow:0 18px 50px rgba(31,52,75,.14)}
    h1{margin:0 0 10px;font-size:24px}
    p{margin:0;color:#52657a;line-height:1.5}
  </style>
</head>
<body><main><h1>${escapeHtml(title)}</h1><p>${escapeHtml(message)}</p></main></body>
</html>`;
}

function _onboardingStartPage(options: { appId: string; configId: string; graphVersion: string; state: string; storePhone: string; completeUrl: string }) {
  return `<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Conectar WhatsApp</title>
  <style>
    body{margin:0;font-family:Arial,sans-serif;background:#eef4fa;color:#17212b;display:grid;place-items:center;min-height:100vh}
    main{width:min(560px,calc(100vw - 32px));background:#fff;border:1px solid #d8e3ef;border-radius:16px;padding:28px;box-shadow:0 18px 50px rgba(31,52,75,.16)}
    h1{margin:0 0 8px;font-size:24px}
    p{margin:0 0 18px;color:#52657a;line-height:1.45}
    button{width:100%;border:0;border-radius:10px;background:#0f766e;color:white;font-weight:700;font-size:16px;padding:16px;cursor:pointer}
    button:disabled{background:#8aa4b8;cursor:wait}
    .status{margin-top:16px;padding:14px;border-radius:10px;background:#f6f9fc;color:#52657a;min-height:22px}
    .ok{background:#eaf8ef;color:#176b36}.err{background:#ffe2df;color:#a11d1d}
  </style>
</head>
<body>
  <main>
    <h1>Conectar WhatsApp da loja</h1>
    <p>Numero informado: <b>${escapeHtml(maskPhone(options.storePhone))}</b>. Entre com a conta Meta Business que administra esse WhatsApp.</p>
    <button id="connect">Conectar pelo Meta</button>
    <div id="status" class="status">Aguardando inicio da conexao.</div>
  </main>
  <script>
    const appId = ${JSON.stringify(options.appId)};
    const configId = ${JSON.stringify(options.configId)};
    const graphVersion = ${JSON.stringify(options.graphVersion)};
    const state = ${JSON.stringify(options.state)};
    const completeUrl = ${JSON.stringify(options.completeUrl)};
    let sessionInfo = {};
    const button = document.getElementById("connect");
    const status = document.getElementById("status");
    function setStatus(text, cls){ status.textContent = text; status.className = "status " + (cls || ""); }
    window.addEventListener("message", (event) => {
      if (!String(event.origin || "").includes("facebook.com")) return;
      try {
        const data = typeof event.data === "string" ? JSON.parse(event.data) : event.data;
        if (data && (data.type === "WA_EMBEDDED_SIGNUP" || data.event)) sessionInfo = data.data || data;
      } catch (_) {}
    });
    window.fbAsyncInit = function() {
      FB.init({ appId, cookie: true, xfbml: false, version: graphVersion });
      button.disabled = false;
    };
    button.onclick = function() {
      button.disabled = true;
      setStatus("Abrindo Meta Business...");
      FB.login(async function(response) {
        try {
          const code = response && response.authResponse && response.authResponse.code;
          if (!code) throw new Error("A Meta nao retornou codigo de conexao.");
          setStatus("Validando conexao na Supabase...");
          const result = await fetch(completeUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ state, code, sessionInfo })
          });
          const data = await result.json();
          if (!result.ok || !data.ok) throw new Error(data.message || "Falha ao conectar WhatsApp.");
          setStatus("WhatsApp conectado. Pode voltar para o PDV.", "ok");
        } catch (error) {
          button.disabled = false;
          setStatus(error.message || String(error), "err");
        }
      }, {
        config_id: configId,
        response_type: "code",
        override_default_response_type: true,
        extras: {
          feature: "whatsapp_embedded_signup",
          sessionInfoVersion: "3",
          setup: {}
        }
      });
    };
  </script>
  <script async defer crossorigin="anonymous" src="https://connect.facebook.net/pt_BR/sdk.js"></script>
</body>
</html>`;
}

function escapeHtml(value: unknown) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
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
