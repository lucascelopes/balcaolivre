import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const PUBLIC_MENU_BASE_URL = "https://cardapio.balcaolivrepdv.com.br";
const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const ADMIN_STORE_BUCKET = "balcao-livre-admin";
const ADMIN_STORE_OBJECT = "admin-store.json";
const OFFLINE_INSTALLER_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows/BalcaoLivrePDV-Setup-1.2.2026.1.exe";
const ONLINE_INSTALLER_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.5.exe";
const TRIAL_SOURCE = "landing_trial_download";
const TRIAL_DAYS = 7;
const TRIAL_WHATSAPP_URL = "https://wa.me/5527981267551?text=Ola%2C%20preciso%20liberar%20outro%20teste%20do%20Balcao%20Livre%20PDV.";

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
  clientKind?: string;
  appVersion?: string;
  localExpiresAt?: string | null;
  localPlan?: string;
  profile?: Record<string, unknown>;
  settings?: Record<string, unknown>;
  metrics?: Record<string, unknown>;
  environment?: Record<string, unknown>;
};

type SupportPayload = ClientPayload & {
  category?: string;
  priority?: string;
  message?: string;
  localWhen?: string | null;
};

type SupportMessagePayload = ClientPayload & {
  message?: string;
  localWhen?: string | null;
};

type PublicMenuPayload = ClientPayload & {
  slug?: string;
  publicUrl?: string;
  description?: string;
  themeColor?: string;
  logoUrl?: string;
  logoFileName?: string;
  logoContentType?: string;
  logoBase64?: string;
  coverImageUrl?: string;
  coverImageFileName?: string;
  coverImageContentType?: string;
  coverImageBase64?: string;
  storeOpen?: boolean;
  scheduleEnabled?: boolean;
  openTime?: string;
  closeTime?: string;
  waitMinMinutes?: number;
  waitMaxMinutes?: number;
  whatsappMessageOrdersEnabled?: boolean;
  discountEnabled?: boolean;
  discountCode?: string;
  discountAmount?: number;
  discountDescription?: string;
  loyaltyEnabled?: boolean;
  loyaltyGoal?: number;
  loyaltyMinimumOrder?: number;
  items?: PublicMenuItem[];
};

type PublicMenuOrderPayload = {
  slug?: string;
  menuId?: string;
  orderType?: string;
  customer?: Record<string, unknown>;
  items?: PublicMenuOrderItem[];
  notes?: string;
  subtotal?: number;
  deliveryFee?: number;
  total?: number;
  localId?: string;
  localWhen?: string;
};

type PublicMenuItem = {
  code?: string;
  name?: string;
  description?: string;
  category?: string;
  price?: number;
  stockQuantity?: number;
  isInStock?: boolean;
  isActive?: boolean;
  imageUrl?: string;
  sortOrder?: number;
};

type PublicMenuOrderItem = {
  code?: string;
  name?: string;
  category?: string;
  quantity?: number;
  price?: number;
  note?: string;
};

type PublicMenuOrderAckPayload = ClientPayload & {
  orderId?: string;
  pdvOrderId?: string;
  status?: string;
};

type PublicMenuOrderStatusPayload = {
  slug?: string;
  orderIds?: string[];
};

type MercadoPagoTerminalPayload = ClientPayload & {
  terminalId?: string;
  terminalLabel?: string;
};

type MercadoPagoChargePayload = ClientPayload & {
  amount?: number;
  method?: string;
  localReference?: string;
  description?: string;
  terminalId?: string;
};

type MercadoPagoPointStatusPayload = ClientPayload & {
  attemptId?: string;
  orderId?: string;
  localReference?: string;
};

type MobileSyncEvent = {
  id?: string;
  type?: string;
  payload?: Record<string, unknown>;
  status?: string;
  createdAt?: string;
};

type MobilePayload = ClientPayload & {
  events?: MobileSyncEvent[];
  snapshot?: Record<string, unknown>;
  localWhen?: string | null;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    const route = routeFromPath(url.pathname);

    if (route === "/health" && req.method === "GET") {
      return json({ ok: true, app: "Balcao Livre License", storage: "supabase" });
    }

    if ((route === "/trial/download" || route === "/api/trial/download") && req.method === "GET") {
      return createTrialDownload(req);
    }

    if (route === "/payments/mercadopago/oauth/callback" && req.method === "GET") {
      return handleMercadoPagoOAuthCallback(req);
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/activate" || route === "/api/app/activate") {
      return activate(req);
    }

    if (route === "/checkin" || route === "/api/app/checkin") {
      return checkIn(req);
    }

    if (route === "/mobile/bootstrap" || route === "/api/mobile/bootstrap") {
      return mobileBootstrap(req);
    }

    if (route === "/mobile/sync" || route === "/api/mobile/sync") {
      return mobileSync(req);
    }

    if (route === "/mobile/backup" || route === "/api/mobile/backup") {
      return mobileBackup(req);
    }

    if (route === "/support/list" || route === "/api/app/support/list") {
      return listSupportTickets(req);
    }

    if (route === "/support" || route === "/api/app/support") {
      return createSupportTicket(req);
    }

    const supportMessageRoute = route.match(/^\/(?:api\/app\/)?support\/([^/]+)\/message$/);
    if (supportMessageRoute) {
      return appendSupportMessage(req, decodeURIComponent(supportMessageRoute[1]));
    }

    if (route === "/menu/publish" || route === "/api/app/menu/publish") {
      return publishMenu(req);
    }

    if (route === "/menu/order" || route === "/api/app/menu/order") {
      return createPublicMenuOrder(req);
    }

    if (route === "/menu/order/status" || route === "/api/app/menu/order/status") {
      return getPublicMenuOrderStatus(req);
    }

    if (route === "/menu/orders/pending" || route === "/api/app/menu/orders/pending") {
      return listPublicMenuOrders(req);
    }

    if (route === "/menu/orders/ack" || route === "/api/app/menu/orders/ack") {
      return ackPublicMenuOrder(req);
    }

    if (route === "/payments/mercadopago/connect/start" || route === "/api/app/payments/mercadopago/connect/start") {
      return startMercadoPagoConnect(req);
    }

    if (route === "/payments/mercadopago/status" || route === "/api/app/payments/mercadopago/status") {
      return getMercadoPagoConnectionStatus(req);
    }

    if (route === "/payments/mercadopago/terminals" || route === "/api/app/payments/mercadopago/terminals") {
      return listMercadoPagoTerminals(req);
    }

    if (route === "/payments/mercadopago/terminal/select" || route === "/api/app/payments/mercadopago/terminal/select") {
      return selectMercadoPagoTerminal(req);
    }

    if (route === "/payments/mercadopago/point/charge" || route === "/api/app/payments/mercadopago/point/charge") {
      return createMercadoPagoPointCharge(req);
    }

    if (route === "/payments/mercadopago/point/status" || route === "/api/app/payments/mercadopago/point/status") {
      return getMercadoPagoPointStatus(req);
    }

    return json({ ok: false, message: "Rota de licenca nao encontrada." }, 404);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function activate(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: true, eventType: "activation" });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  await writeAdminStoreClientSeen(payload, result.license, "activation.ok", `Ativacao: ${businessName(payload.profile) || stringValue(payload.machineCode)}`);
  return json({
    ok: true,
    message: "Chave ativada pelo Supabase.",
    plan: result.license.plan,
    expiresAt: result.license.expires_at,
  });
}

async function checkIn(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: payload.eventName || "checkin" });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  await writeAdminStoreClientSeen(payload, result.license, stringValue(payload.eventName) || "device.checkin", `Check-in ${businessName(payload.profile) || stringValue(payload.machineCode)}`);
  return json({ ok: true, message: "Licenca sincronizada no Supabase.", mode: "supabase" });
}

async function mobileBootstrap(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<MobilePayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mobile.bootstrap", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const latest = await readMobileSnapshot(licenseKey, stringValue(payload.machineHash));
  await appendEvent("mobile.bootstrap", "Mobile bootstrap solicitado.", payload);

  return json({
    ok: true,
    message: "Snapshot mobile carregado.",
    plan: result.license.plan,
    expiresAt: result.license.expires_at,
    snapshot: latest ?? emptyMobileSnapshot(payload),
    serverTime: new Date().toISOString(),
  });
}

async function mobileSync(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<MobilePayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mobile.sync", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  const events = Array.isArray(payload.events) ? payload.events : [];
  if (payload.snapshot && typeof payload.snapshot === "object") {
    await writeMobileSnapshot(licenseKey, machineHash, payload.snapshot, "latest.json");
  }
  if (events.length > 0) {
    await writeMobileEventBatch(licenseKey, machineHash, events, payload);
  }

  const store = await readAdminStore();
  upsertAdminStoreLicense(store, payload, result.license);
  upsertAdminStoreDevice(store, payload);
  appendAdminStoreEvent(store, "mobile.sync", `Mobile sync: ${events.length} evento(s)`, payload);
  trimAdminStore(store);
  await writeAdminStore(store);
  await appendEvent("mobile.sync", `Mobile sync recebeu ${events.length} evento(s).`, payload);

  return json({
    ok: true,
    message: "Sync mobile recebido.",
    acceptedEventIds: events.map((event) => stringValue(event.id)).filter(Boolean),
    pullEvents: [],
    serverTime: new Date().toISOString(),
  });
}

async function mobileBackup(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<MobilePayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mobile.backup", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  const snapshot = payload.snapshot && typeof payload.snapshot === "object"
    ? payload.snapshot
    : emptyMobileSnapshot(payload);
  const fileName = `${new Date().toISOString().replace(/[:.]/g, "-")}.json`;
  await writeMobileSnapshot(licenseKey, machineHash, snapshot, "latest.json");
  await writeMobileSnapshot(licenseKey, machineHash, snapshot, `backups/${fileName}`);
  await appendEvent("mobile.backup", "Backup mobile versionado.", payload);

  return json({
    ok: true,
    message: "Backup mobile salvo.",
    fileName,
    serverTime: new Date().toISOString(),
  });
}

async function createTrialDownload(req: Request) {
  const url = new URL(req.url);
  const kind = normalizeTrialKind(url.searchParams.get("plan"));
  const installerUrl = kind === "online" ? ONLINE_INSTALLER_URL : OFFLINE_INSTALLER_URL;
  const clientKind = kind === "online" ? "windows-online" : "windows-offline";
  const now = new Date();
  const expiresAt = new Date(now.getTime() + TRIAL_DAYS * 24 * 60 * 60 * 1000);
  const ip = requestIp(req);
  const userAgent = stringValue(req.headers.get("user-agent"));
  const trialIpHash = (await activationSignature(`trial-ip|${ip || "unknown"}`)).slice(0, 32);
  const userAgentHash = userAgent ? (await activationSignature(`trial-ua|${userAgent}`)).slice(0, 32) : "";
  const supabase = serviceClient();

  const existing = await supabase
    .from("bv_licenses")
    .select("key, expires_at")
    .in("status", ["DISPONIVEL", "ATIVA"])
    .contains("profile", { source: TRIAL_SOURCE, installer: kind, trial_ip_hash: trialIpHash })
    .gt("expires_at", now.toISOString())
    .limit(1);

  if (existing.error) {
    return trialDownloadPage(
      "Download indisponivel",
      `Supabase recusou gerar a chave de teste: ${existing.error.message}`,
      false,
      500,
    );
  }

  if ((existing.data ?? []).length > 0) {
    await supabase.from("bv_license_events").insert({
      license_key: stringValue(existing.data?.[0]?.key),
      event_type: "trial.download.blocked_by_ip",
      message: "Nova chave de teste bloqueada por IP.",
      payload: { source: TRIAL_SOURCE, installer: kind, trialIpHash, userAgentHash },
    });
    return Response.redirect(TRIAL_WHATSAPP_URL, 303);
  }

  const expiresText = activationExpirationText(expiresAt);
  const serialPrefix = kind === "online" ? "ONL" : "OFF";
  const serial = `${serialPrefix}${crypto.randomUUID().replaceAll("-", "").slice(0, 9).toUpperCase()}`;
  const signature = (await activationSignature(`BLV|${expiresText}|${serial}`)).slice(0, 10);
  const key = `BLV-${expiresText}-${serial}-${signature}`;
  const profile = {
    source: TRIAL_SOURCE,
    installer: kind,
    trial_days: TRIAL_DAYS,
    trial_ip_hash: trialIpHash,
    user_agent_hash: userAgentHash,
    generated_at: now.toISOString(),
    installer_url: installerUrl,
  };

  const created = await supabase.from("bv_licenses").insert({
    key,
    status: "DISPONIVEL",
    plan: kind === "online" ? "Teste Online 7 dias" : "Teste Offline 7 dias",
    customer_name: "Teste gerado no download",
    client_kind: clientKind,
    profile,
    settings: {},
    metrics: {},
    expires_at: expiresAt.toISOString(),
    updated_at: now.toISOString(),
  });

  if (created.error) {
    return trialDownloadPage(
      "Download indisponivel",
      `Supabase recusou salvar a chave de teste: ${created.error.message}`,
      false,
      500,
    );
  }

  await supabase.from("bv_license_events").insert({
    license_key: key,
    event_type: "trial.download.generated",
    message: "Chave de teste gerada no download.",
    payload: { source: TRIAL_SOURCE, installer: kind, trialIpHash, userAgentHash, generatedAt: now.toISOString() },
  });

  return new Response(null, {
    status: 302,
    headers: {
      ...corsHeaders,
      Location: installerUrl,
      "Cache-Control": "no-store, max-age=0",
    },
  });
}

async function listSupportTickets(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "support.list", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message, tickets: [] }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  const store = await readAdminStore();
  const tickets = adminSupportTickets(store)
    .filter((ticket) =>
      stringValue(ticket.licenseKey).toUpperCase() === licenseKey &&
      stringValue(ticket.machineHash) === machineHash
    )
    .sort((a, b) =>
      supportStatusRank(stringValue(a.status)) - supportStatusRank(stringValue(b.status)) ||
      Date.parse(stringValue(b.updatedAt)) - Date.parse(stringValue(a.updatedAt))
    )
    .slice(0, 10)
    .map(supportTicketToClient);

  return json({ ok: true, tickets });
}

async function createSupportTicket(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<SupportPayload>(req)), req);
  payload.message = stringValue(payload.message);
  if (!payload.message) {
    return json({ ok: false, message: "Mensagem do suporte obrigatoria." }, 400);
  }

  const result = await ensureLicense(payload, { bindMachine: false, eventType: "support.opened", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const now = new Date().toISOString();
  const ticketId = crypto.randomUUID().replaceAll("-", "");
  const ticket = {
    id: ticketId,
    shortId: shortSupportId(ticketId),
    status: "ABERTO",
    category: stringValue(payload.category) || "Suporte tecnico",
    priority: normalizeSupportPriority(payload.priority),
    message: payload.message,
    messages: [supportMessage("cliente", payload.message, now)],
    adminNote: "",
    createdAt: now,
    updatedAt: now,
    resolvedAt: null,
    licenseKey,
    machineHash: stringValue(payload.machineHash),
    machineCode: stringValue(payload.machineCode),
    appVersion: stringValue(payload.appVersion),
    customerName: businessName(payload.profile) || "Cliente",
    email: stringValue(payload.profile?.email).toLowerCase(),
    businessName: businessName(payload.profile),
    ownerName: stringValue(payload.profile?.ownerName),
    phone: stringValue(payload.profile?.phone),
    cnpj: stringValue(payload.profile?.cnpj),
    city: stringValue(payload.profile?.city),
    state: stringValue(payload.profile?.state),
    address: stringValue(payload.profile?.address),
    profile: payload.profile ?? {},
    metrics: payload.metrics ?? {},
    environment: payload.environment ?? {},
  };

  const store = await readAdminStore();
  upsertAdminStoreLicense(store, payload, result.license);
  upsertAdminStoreDevice(store, payload);
  adminSupportTickets(store).push(ticket);
  appendAdminStoreEvent(store, "support.opened", `Suporte ${ticket.shortId}: ${ticket.businessName || ticket.machineCode}`, payload);
  trimAdminStore(store);
  await writeAdminStore(store);
  await appendEvent("support.opened", `Suporte ${ticket.shortId} aberto.`, payload);

  return json({ ok: true, ticketId: ticket.shortId, message: "Mensagem enviada. O suporte vai responder por aqui." });
}

async function appendSupportMessage(req: Request, id: string) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<SupportMessagePayload>(req)), req);
  const message = stringValue(payload.message);
  if (!message) {
    return json({ ok: false, message: "Mensagem obrigatoria." }, 400);
  }

  const result = await ensureLicense(payload, { bindMachine: false, eventType: "support.customer_message", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  const store = await readAdminStore();
  const ticket = findSupportTicket(store, id);
  if (!ticket) {
    return json({ ok: false, message: "Chamado nao encontrado." }, 404);
  }

  if (stringValue(ticket.licenseKey).toUpperCase() !== licenseKey || stringValue(ticket.machineHash) !== machineHash) {
    return json({ ok: false, message: "Chamado pertence a outro computador." }, 401);
  }

  const now = new Date().toISOString();
  const messages = supportMessages(ticket);
  messages.push(supportMessage("cliente", message, now));
  ticket.messages = messages;
  ticket.message = message;
  ticket.status = "ABERTO";
  ticket.updatedAt = now;
  ticket.profile = payload.profile ?? ticket.profile ?? {};
  ticket.metrics = payload.metrics ?? ticket.metrics ?? {};
  ticket.environment = payload.environment ?? ticket.environment ?? {};
  upsertAdminStoreLicense(store, payload, result.license);
  upsertAdminStoreDevice(store, payload);
  appendAdminStoreEvent(store, "support.customer_message", `Nova mensagem no suporte ${shortSupportId(stringValue(ticket.id))}`, payload);
  trimAdminStore(store);
  await writeAdminStore(store);
  await appendEvent("support.customer_message", `Nova mensagem no suporte ${shortSupportId(stringValue(ticket.id))}.`, payload);

  return json({ ok: true, ticketId: shortSupportId(stringValue(ticket.id)), message: "Mensagem enviada." });
}

async function publishMenu(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<PublicMenuPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "menu.publish" });
  if (!result.ok) {
    return json({ ok: false, message: result.message, slug: "", publicUrl: "", itemsPublished: 0 }, result.status ?? 401);
  }

  const supabase = serviceClient();
  const licenseKey = normalizeLicense(payload.licenseKey);
  const baseSlug = normalizeSlug(payload.slug || businessName(payload.profile) || "loja") || "loja";
  const existing = await supabase
    .from("bv_public_menus")
    .select("id, slug")
    .eq("store_id", licenseKey)
    .maybeSingle();

  if (existing.error) {
    return json(failMenu(`Supabase recusou consulta do cardapio: ${existing.error.message}`), 500);
  }

  const waitMin = Math.max(1, Math.round(numberValue(payload.waitMinMinutes) || 30));
  const openTime = normalizeClockText(payload.openTime);
  const closeTime = normalizeClockText(payload.closeTime);
  const menuPayload = {
    store_id: licenseKey,
    name: businessName(payload.profile) || "Balcao Livre",
    description: stringValue(payload.description) || "Cardapio digital.",
    phone: stringValue(payload.profile?.phone),
    address: stringValue(payload.profile?.address),
    city: stringValue(payload.profile?.city),
    state: stringValue(payload.profile?.state),
    logo_url: resolveInlineImageUrl(payload.logoUrl, payload.logoContentType, payload.logoBase64),
    cover_image_url: resolveInlineImageUrl(payload.coverImageUrl, payload.coverImageContentType, payload.coverImageBase64),
    theme_color: stringValue(payload.themeColor) || "#0f766e",
    store_open: payload.storeOpen !== false,
    schedule_enabled: payload.scheduleEnabled !== false,
    open_time: openTime,
    close_time: closeTime,
    wait_min_minutes: waitMin,
    wait_max_minutes: Math.max(waitMin, Math.round(numberValue(payload.waitMaxMinutes) || 60)),
    whatsapp_message_orders_enabled: payload.whatsappMessageOrdersEnabled === true,
    discount_enabled: payload.discountEnabled === true,
    discount_code: (stringValue(payload.discountCode) || "EXCLUSIVO4").toUpperCase(),
    discount_amount: Math.max(0, numberValue(payload.discountAmount) || 0),
    discount_description: stringValue(payload.discountDescription) || "Apresente este cupom no atendimento para receber o desconto.",
    loyalty_enabled: payload.loyaltyEnabled === true,
    loyalty_goal: Math.max(1, Math.round(numberValue(payload.loyaltyGoal) || 20)),
    loyalty_minimum_order: Math.max(0, numberValue(payload.loyaltyMinimumOrder) || 20),
    is_published: true,
    updated_at: new Date().toISOString(),
  };

  let menuId = stringValue(existing.data?.id);
  let slug = stringValue(existing.data?.slug);

  if (menuId) {
    let { error } = await supabase
      .from("bv_public_menus")
      .update({ ...menuPayload, slug: slug || baseSlug })
      .eq("id", menuId);
    if (error && isMissingWhatsAppOptionsColumn(error.message)) {
      const retry = await supabase
        .from("bv_public_menus")
        .update({ ...withoutWhatsAppOptionsColumn(menuPayload), slug: slug || baseSlug })
        .eq("id", menuId);
      error = retry.error;
    }
    if (error) {
      return json(failMenu(`Supabase recusou atualizar cardapio: ${error.message}`), 500);
    }
  } else {
    for (const candidate of slugCandidates(baseSlug)) {
      let inserted = await supabase
        .from("bv_public_menus")
        .insert({ ...menuPayload, slug: candidate })
        .select("id, slug")
        .single();
      if (inserted.error && isMissingWhatsAppOptionsColumn(inserted.error.message)) {
        inserted = await supabase
          .from("bv_public_menus")
          .insert({ ...withoutWhatsAppOptionsColumn(menuPayload), slug: candidate })
          .select("id, slug")
          .single();
      }

      if (!inserted.error && inserted.data) {
        menuId = inserted.data.id;
        slug = inserted.data.slug;
        break;
      }

      if (!isConflict(inserted.error)) {
        return json(failMenu(`Supabase recusou criar cardapio: ${inserted.error?.message || "erro desconhecido"}`), 500);
      }
    }
  }

  if (!menuId || !slug) {
    return json(failMenu("Nao foi possivel gerar um link unico para esse cardapio."), 409);
  }

  const deleteItems = await supabase.from("bv_public_menu_items").delete().eq("menu_id", menuId);
  if (deleteItems.error) {
    return json(failMenu(`Supabase recusou limpar itens antigos: ${deleteItems.error.message}`), 500);
  }

  const items = (payload.items ?? [])
    .filter((item) => item?.isActive !== false && stringValue(item?.name))
    .sort((a, b) =>
      numberValue(a.sortOrder) - numberValue(b.sortOrder)
      || stringValue(a.category).localeCompare(stringValue(b.category))
      || stringValue(a.name).localeCompare(stringValue(b.name))
    )
    .map((item, index) => ({
      menu_id: menuId,
      code: stringValue(item.code),
      name: stringValue(item.name),
      description: stringValue(item.description),
      category: stringValue(item.category) || "Cardapio",
      price: numberValue(item.price),
      stock_quantity: numberValue(item.stockQuantity),
      is_in_stock: item.isInStock !== false,
      image_url: stringValue(item.imageUrl),
      sort_order: numberValue(item.sortOrder) || index * 10,
      is_active: item.isActive !== false,
      updated_at: new Date().toISOString(),
    }));

  if (items.length) {
    const insertedItems = await supabase.from("bv_public_menu_items").insert(items);
    if (insertedItems.error) {
      return json(failMenu(`Supabase recusou itens do cardapio: ${insertedItems.error.message}`), 500);
    }
  }

  return json({
    ok: true,
    message: "Cardapio publicado pelo Supabase.",
    slug,
    publicUrl: `${PUBLIC_MENU_BASE_URL}/${slugToPath(slug)}`,
    itemsPublished: items.length,
  });
}

async function createPublicMenuOrder(req: Request) {
  const payload = normalizePayloadKeys(await readJson<PublicMenuOrderPayload>(req));
  const supabase = serviceClient();
  const slug = normalizeSlug(stringValue(payload.slug));
  const menuId = stringValue(payload.menuId);
  if (!slug && !isUuid(menuId)) {
    return json({ ok: false, message: "Cardapio nao informado." }, 400);
  }

  const menuQuery = supabase
    .from("bv_public_menus")
    .select("id, store_id, slug, name, is_published")
    .eq("is_published", true);
  const menuResult = isUuid(menuId)
    ? await menuQuery.eq("id", menuId).maybeSingle()
    : await menuQuery.eq("slug", slug).maybeSingle();

  if (menuResult.error) {
    return json({ ok: false, message: `Supabase recusou cardapio: ${menuResult.error.message}` }, 500);
  }

  const menu = menuResult.data as Record<string, unknown> | null;
  if (!menu) {
    return json({ ok: false, message: "Cardapio nao encontrado ou fora do ar." }, 404);
  }

  const storeId = normalizeLicense(menu.store_id);
  if (!storeId) {
    return json({ ok: false, message: "Cardapio sem licenca vinculada." }, 409);
  }

  const orderType = normalizePublicOrderType(payload.orderType);
  if (!orderType) {
    return json({ ok: false, message: "Escolha entrega, retirada ou mesa/local." }, 400);
  }

  const customer = payload.customer ?? {};
  const customerName = stringValue(customer.name);
  const customerPhone = stringValue(customer.phone);
  const address = stringValue(customer.address);
  const district = stringValue(customer.district);
  const reference = stringValue(customer.reference);
  const tableLabel = stringValue(customer.table);
  const desiredTime = stringValue(customer.time);
  const customerDocument = stringValue(customer.document);

  if (orderType === "DELIVERY" && !address) {
    return json({ ok: false, message: "Informe o endereco de entrega." }, 400);
  }

  if (orderType === "PICKUP" && !customerName) {
    return json({ ok: false, message: "Informe o nome para retirada." }, 400);
  }

  if (orderType === "TABLE" && !tableLabel) {
    return json({ ok: false, message: "Informe mesa ou comanda." }, 400);
  }

  const items = sanitizePublicOrderItems(payload.items ?? []);
  if (!items.length) {
    return json({ ok: false, message: "Adicione pelo menos um item." }, 400);
  }

  const computedSubtotal = roundMoney(items.reduce((sum, item) => sum + item.quantity * item.price, 0));
  const deliveryFee = roundMoney(numberValue(payload.deliveryFee));
  const payloadTotal = roundMoney(numberValue(payload.total));
  const total = payloadTotal > 0 ? payloadTotal : roundMoney(computedSubtotal + deliveryFee);
  const now = new Date().toISOString();
  const inserted = await supabase
    .from("bv_public_orders")
    .insert({
      menu_id: stringValue(menu.id),
      store_id: storeId,
      slug: stringValue(menu.slug),
      source: "CARDAPIO_ONLINE",
      status: "NOVO",
      customer_name: customerName,
      customer_phone: customerPhone,
      customer_document: customerDocument,
      order_type: orderType,
      table_label: tableLabel,
      address,
      district,
      reference,
      desired_time: desiredTime,
      notes: stringValue(payload.notes),
      subtotal: computedSubtotal,
      delivery_fee: deliveryFee,
      total,
      items,
      customer,
      payload,
      updated_at: now,
    })
    .select("id, created_at")
    .single();

  if (inserted.error) {
    return json({ ok: false, message: `Supabase recusou pedido: ${inserted.error.message}` }, 500);
  }

  return json({
    ok: true,
    message: "Pedido enviado ao PDV.",
    orderId: inserted.data.id,
    status: "NOVO",
    createdAt: inserted.data.created_at,
  });
}

async function getPublicMenuOrderStatus(req: Request) {
  const payload = normalizePayloadKeys(await readJson<PublicMenuOrderStatusPayload>(req));
  const ids = Array.from(new Set((payload.orderIds ?? []).map((id) => stringValue(id)).filter(isUuid))).slice(0, 20);
  if (!ids.length) {
    return json({ ok: true, orders: [] });
  }

  const slug = normalizeSlug(stringValue(payload.slug));
  let query = serviceClient()
    .from("bv_public_orders")
    .select("id, slug, status, order_type, pdv_order_id, created_at, updated_at")
    .in("id", ids);

  if (slug) {
    query = query.eq("slug", slug);
  }

  const result = await query;
  if (result.error) {
    return json({ ok: false, message: `Supabase recusou status dos pedidos: ${result.error.message}`, orders: [] }, 500);
  }

  return json({
    ok: true,
    orders: ((result.data ?? []) as Record<string, unknown>[]).map((row) => ({
      id: stringValue(row.id),
      status: stringValue(row.status),
      orderType: stringValue(row.order_type),
      pdvOrderId: stringValue(row.pdv_order_id),
      createdAt: stringValue(row.created_at),
      updatedAt: stringValue(row.updated_at),
    })),
  });
}

async function listPublicMenuOrders(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload & { limit?: number }>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "menu.orders.poll", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message, orders: [] }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const limit = Math.max(1, Math.min(50, Math.trunc(numberValue(payload.limit)) || 20));
  const supabase = serviceClient();
  const query = await supabase
    .from("bv_public_orders")
    .select("id, slug, source, status, customer_name, customer_phone, customer_document, order_type, table_label, address, district, reference, desired_time, notes, subtotal, delivery_fee, total, items, customer, created_at")
    .eq("store_id", licenseKey)
    .in("status", ["NOVO", "RECEBIDO"])
    .order("created_at", { ascending: true })
    .limit(limit);

  if (query.error) {
    return json({ ok: false, message: `Supabase recusou pedidos: ${query.error.message}`, orders: [] }, 500);
  }

  const rows = (query.data ?? []) as Record<string, unknown>[];
  const ids = rows.map((row) => stringValue(row.id)).filter(Boolean);
  if (ids.length) {
    await supabase
      .from("bv_public_orders")
      .update({ status: "RECEBIDO", updated_at: new Date().toISOString() })
      .eq("store_id", licenseKey)
      .in("id", ids)
      .eq("status", "NOVO");
  }

  return json({
    ok: true,
    message: ids.length ? `${ids.length} pedido(s) do cardapio recebido(s).` : "Nenhum pedido novo do cardapio.",
    orders: rows.map(publicOrderRowToClient),
  });
}

async function ackPublicMenuOrder(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<PublicMenuOrderAckPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "menu.orders.ack", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const orderId = stringValue(payload.orderId);
  if (!isUuid(orderId)) {
    return json({ ok: false, message: "Pedido do cardapio invalido." }, 400);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const status = normalizePublicOrderAckStatus(payload.status);
  const now = new Date().toISOString();
  const update: Record<string, unknown> = {
    status,
    pdv_order_id: stringValue(payload.pdvOrderId),
    updated_at: now,
  };
  if (status === "IMPORTADO") {
    update.imported_at = now;
  }

  const saved = await serviceClient()
    .from("bv_public_orders")
    .update(update)
    .eq("id", orderId)
    .eq("store_id", licenseKey)
    .select("id")
    .maybeSingle();

  if (saved.error) {
    return json({ ok: false, message: `Supabase recusou baixa do pedido: ${saved.error.message}` }, 500);
  }

  if (!saved.data) {
    return json({ ok: false, message: "Pedido nao encontrado para esta licenca." }, 404);
  }

  return json({ ok: true, message: "Pedido confirmado no PDV." });
}

async function startMercadoPagoConnect(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mercadopago.connect.start", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const clientId = mercadoPagoClientId();
  if (!clientId) {
    return json({ ok: false, message: "Configure MERCADO_PAGO_CLIENT_ID na Edge Function." }, 500);
  }

  const state = crypto.randomUUID();
  const supabase = serviceClient();
  const expiresAt = new Date(Date.now() + 10 * 60_000).toISOString();
  const saved = await supabase.from("bv_mercadopago_oauth_states").insert({
    state,
    license_key: normalizeLicense(payload.licenseKey),
    machine_hash: stringValue(payload.machineHash),
    expires_at: expiresAt,
  });
  if (saved.error) {
    return json({ ok: false, message: `Supabase recusou inicio Mercado Pago: ${saved.error.message}` }, 500);
  }

  const auth = new URL("https://auth.mercadopago.com.br/authorization");
  auth.searchParams.set("client_id", clientId);
  auth.searchParams.set("response_type", "code");
  auth.searchParams.set("platform_id", "mp");
  auth.searchParams.set("state", state);
  auth.searchParams.set("redirect_uri", mercadoPagoRedirectUri());
  auth.searchParams.set("scope", "offline_access");
  return json({ ok: true, authUrl: auth.toString(), expiresAt });
}

async function handleMercadoPagoOAuthCallback(req: Request) {
  const url = new URL(req.url);
  const code = stringValue(url.searchParams.get("code"));
  const state = stringValue(url.searchParams.get("state"));
  if (!code || !state) {
    return html("Mercado Pago", "Vinculo recusado ou incompleto.", false);
  }

  const supabase = serviceClient();
  const stateRow = await supabase
    .from("bv_mercadopago_oauth_states")
    .select("*")
    .eq("state", state)
    .maybeSingle();

  if (stateRow.error || !stateRow.data) {
    return html("Mercado Pago", "Estado de conexao invalido. Volte ao PDV e tente conectar de novo.", false);
  }

  const row = stateRow.data as Record<string, unknown>;
  if (stringValue(row.used_at) || new Date(stringValue(row.expires_at)).getTime() < Date.now()) {
    return html("Mercado Pago", "Essa conexao expirou. Volte ao PDV e conecte novamente.", false);
  }

  const token = await exchangeMercadoPagoToken({
    grant_type: "authorization_code",
    code,
    redirect_uri: mercadoPagoRedirectUri(),
  });
  const now = new Date().toISOString();
  const expiresAt = mercadoPagoTokenExpiresAt(token);
  const upsert = await supabase.from("bv_mercadopago_connections").upsert({
    license_key: stringValue(row.license_key),
    machine_hash: stringValue(row.machine_hash),
    status: "CONNECTED",
    seller_user_id: stringValue(token.user_id),
    access_token: stringValue(token.access_token),
    refresh_token: stringValue(token.refresh_token),
    public_key: stringValue(token.public_key),
    token_type: stringValue(token.token_type),
    scope: stringValue(token.scope),
    expires_at: expiresAt,
    connected_at: now,
    last_sync_at: now,
    last_error: "",
    updated_at: now,
  }, { onConflict: "license_key" });

  if (upsert.error) {
    return html("Mercado Pago", `Supabase recusou salvar conexao: ${upsert.error.message}`, false);
  }

  await supabase.from("bv_mercadopago_oauth_states").update({ used_at: now }).eq("state", state);
  return html("Mercado Pago conectado", "Conta Mercado Pago conectada. Pode voltar ao Balcao Livre PDV.", true);
}

async function getMercadoPagoConnectionStatus(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mercadopago.status", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const connection = await getMercadoPagoConnection(normalizeLicense(payload.licenseKey));
  return json({
    ok: true,
    connected: !!connection?.access_token,
    status: stringValue(connection?.status) || "DISCONNECTED",
    sellerUserId: stringValue(connection?.seller_user_id),
    selectedTerminalId: stringValue(connection?.selected_terminal_id),
    selectedTerminalLabel: stringValue(connection?.selected_terminal_label),
    expiresAt: stringValue(connection?.expires_at),
    lastSyncAt: stringValue(connection?.last_sync_at),
    lastError: stringValue(connection?.last_error),
  });
}

async function listMercadoPagoTerminals(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<ClientPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mercadopago.terminals", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message, terminals: [] }, result.status ?? 401);
  }

  const token = await ensureMercadoPagoAccessToken(normalizeLicense(payload.licenseKey));
  if (!token.ok) {
    return json({ ok: false, message: token.message, terminals: [] }, token.status ?? 400);
  }

  const response = await mercadoPagoFetch(token.accessToken, "/terminals/v1/list?limit=50&offset=0");
  const terminals = Array.isArray(response?.data?.terminals)
    ? response.data.terminals
    : Array.isArray(response?.terminals)
      ? response.terminals
      : [];
  return json({
    ok: true,
    terminals: terminals.map((terminal: Record<string, unknown>) => mercadoPagoTerminalToClient(terminal)),
  });
}

async function selectMercadoPagoTerminal(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<MercadoPagoTerminalPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mercadopago.terminal.select", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const terminalId = stringValue(payload.terminalId);
  if (!terminalId) {
    const saved = await serviceClient()
      .from("bv_mercadopago_connections")
      .update({
        selected_terminal_id: null,
        selected_terminal_label: null,
        last_sync_at: new Date().toISOString(),
        updated_at: new Date().toISOString(),
      })
      .eq("license_key", normalizeLicense(payload.licenseKey))
      .select("selected_terminal_id, selected_terminal_label")
      .maybeSingle();

    if (saved.error) {
      return json({ ok: false, message: `Supabase recusou liberar maquininha: ${saved.error.message}` }, 500);
    }

    if (!saved.data) {
      return json({ ok: false, message: "Conecte o Mercado Pago antes de liberar a maquininha." }, 404);
    }

    return json({ ok: true, message: "Maquininha liberada. A Point nao recebera mais cobrancas do PDV." });
  }

  const saved = await serviceClient()
    .from("bv_mercadopago_connections")
    .update({
      selected_terminal_id: terminalId,
      selected_terminal_label: stringValue(payload.terminalLabel) || terminalId,
      last_sync_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    })
    .eq("license_key", normalizeLicense(payload.licenseKey))
    .select("selected_terminal_id, selected_terminal_label")
    .maybeSingle();

  if (saved.error) {
    return json({ ok: false, message: `Supabase recusou maquininha: ${saved.error.message}` }, 500);
  }

  if (!saved.data) {
    return json({ ok: false, message: "Conecte o Mercado Pago antes de escolher a maquininha." }, 404);
  }

  return json({ ok: true, message: "Maquininha Mercado Pago salva." });
}

async function createMercadoPagoPointCharge(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<MercadoPagoChargePayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mercadopago.point.charge", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  const connection = await getMercadoPagoConnection(licenseKey);
  const terminalId = stringValue(payload.terminalId) || stringValue(connection?.selected_terminal_id);
  if (!terminalId) {
    return json({ ok: false, message: "Escolha uma maquininha Mercado Pago antes de cobrar." }, 400);
  }

  const amount = roundMoney(payload.amount);
  if (amount <= 0) {
    return json({ ok: false, message: "Valor da cobranca invalido." }, 400);
  }

  const token = await ensureMercadoPagoAccessToken(licenseKey);
  if (!token.ok) {
    return json({ ok: false, message: token.message }, token.status ?? 400);
  }

  const localReference = sanitizeMercadoPagoReference(payload.localReference || crypto.randomUUID());
  const method = normalizePointPaymentMethod(payload.method);
  const orderBody: Record<string, unknown> = {
    type: "point",
    external_reference: localReference,
    expiration_time: "PT10M",
    transactions: {
      payments: [{ amount: formatMercadoPagoAmount(amount) }],
    },
    config: {
      point: {
        terminal_id: terminalId,
        print_on_terminal: "no_ticket",
      },
      ...(method ? { payment_method: { default_type: method } } : {}),
    },
    description: stringValue(payload.description) || "Balcao Livre PDV",
  };

  const order = await mercadoPagoFetch(token.accessToken, "/v1/orders", {
    method: "POST",
    headers: { "X-Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify(orderBody),
  });
  const payment = Array.isArray(order?.transactions?.payments) ? order.transactions.payments[0] : {};
  const attempt = {
    license_key: licenseKey,
    machine_hash: stringValue(payload.machineHash),
    local_reference: localReference,
    method: stringValue(payload.method).toUpperCase() || "POINT",
    amount,
    order_id: stringValue(order?.id),
    payment_id: stringValue(payment?.id),
    terminal_id: terminalId,
    terminal_label: stringValue(connection?.selected_terminal_label) || terminalId,
    status: normalizeMercadoPagoOrderStatus(order),
    status_detail: stringValue(order?.status_detail || payment?.status_detail),
    raw_response: order,
    updated_at: new Date().toISOString(),
  };
  const saved = await serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .upsert(attempt, { onConflict: "license_key,local_reference" })
    .select("id, status, status_detail, order_id, payment_id")
    .single();

  if (saved.error) {
    return json({ ok: false, message: `Supabase recusou tentativa Mercado Pago: ${saved.error.message}` }, 500);
  }

  return json({
    ok: true,
    message: "Cobranca enviada para a maquininha.",
    attemptId: saved.data.id,
    localReference,
    orderId: saved.data.order_id,
    paymentId: saved.data.payment_id,
    status: saved.data.status,
    statusDetail: saved.data.status_detail,
  });
}

async function getMercadoPagoPointStatus(req: Request) {
  const payload = withRequestEnvironment(normalizePayloadKeys(await readJson<MercadoPagoPointStatusPayload>(req)), req);
  const result = await ensureLicense(payload, { bindMachine: false, eventType: "mercadopago.point.status", skipEvent: true });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  const licenseKey = normalizeLicense(payload.licenseKey);
  let query = serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .select("*")
    .eq("license_key", licenseKey);

  if (isUuid(stringValue(payload.attemptId))) {
    query = query.eq("id", stringValue(payload.attemptId));
  } else if (stringValue(payload.orderId)) {
    query = query.eq("order_id", stringValue(payload.orderId));
  } else if (stringValue(payload.localReference)) {
    query = query.eq("local_reference", stringValue(payload.localReference));
  } else {
    return json({ ok: false, message: "Informe a tentativa de pagamento." }, 400);
  }

  const current = await query.maybeSingle();
  if (current.error || !current.data) {
    return json({ ok: false, message: current.error?.message || "Tentativa nao encontrada." }, current.error ? 500 : 404);
  }

  const row = current.data as Record<string, unknown>;
  const token = await ensureMercadoPagoAccessToken(licenseKey);
  if (!token.ok) {
    return json({ ok: false, message: token.message }, token.status ?? 400);
  }

  const orderId = stringValue(row.order_id);
  const order = orderId ? await mercadoPagoFetch(token.accessToken, `/v1/orders/${encodeURIComponent(orderId)}`) : row.raw_response;
  const payment = Array.isArray(order?.transactions?.payments) ? order.transactions.payments[0] : {};
  const status = normalizeMercadoPagoOrderStatus(order);
  const statusDetail = stringValue(order?.status_detail || payment?.status_detail);
  await serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .update({
      status,
      status_detail: statusDetail,
      payment_id: stringValue(payment?.id) || stringValue(row.payment_id),
      raw_response: order,
      updated_at: new Date().toISOString(),
    })
    .eq("id", stringValue(row.id));

  return json({
    ok: true,
    attemptId: stringValue(row.id),
    orderId,
    paymentId: stringValue(payment?.id) || stringValue(row.payment_id),
    status,
    statusDetail,
    paid: isMercadoPagoPaid(status, payment),
  });
}

async function readMobileSnapshot(licenseKey: string, machineHash: string) {
  const machinePath = mobileStoragePath(licenseKey, machineHash, "latest.json");
  const sharedPath = mobileStoragePath(licenseKey, "shared", "latest.json");
  return await readStorageJson(machinePath) ?? await readStorageJson(sharedPath);
}

async function writeMobileSnapshot(
  licenseKey: string,
  machineHash: string,
  snapshot: Record<string, unknown>,
  fileName: string,
) {
  const path = mobileStoragePath(licenseKey, machineHash || "mobile", fileName);
  await writeStorageJson(path, {
    ...snapshot,
    _mobileSavedAt: new Date().toISOString(),
  });
}

async function writeMobileEventBatch(
  licenseKey: string,
  machineHash: string,
  events: MobileSyncEvent[],
  payload: MobilePayload,
) {
  const now = new Date().toISOString();
  const path = mobileStoragePath(
    licenseKey,
    "events",
    `${safeStorageSegment(machineHash || "mobile")}-${now.replace(/[:.]/g, "-")}-${crypto.randomUUID()}.json`,
  );
  await writeStorageJson(path, {
    licenseKey,
    machineHash,
    machineCode: stringValue(payload.machineCode),
    clientKind: normalizeClientKind(payload.clientKind),
    receivedAt: now,
    events: events.map((event) => ({
      id: stringValue(event.id) || crypto.randomUUID(),
      type: stringValue(event.type) || "mobile.event",
      payload: event.payload && typeof event.payload === "object" ? event.payload : {},
      status: stringValue(event.status) || "pending",
      createdAt: stringValue(event.createdAt) || now,
    })),
  });
}

async function readStorageJson(path: string): Promise<Record<string, unknown> | null> {
  const supabase = serviceClient();
  await ensureAdminStoreBucket(supabase);
  const { data, error } = await supabase.storage.from(ADMIN_STORE_BUCKET).download(path);
  if (error || !data) return null;
  try {
    return JSON.parse(await data.text()) as Record<string, unknown>;
  } catch {
    return null;
  }
}

async function writeStorageJson(path: string, value: Record<string, unknown>) {
  const supabase = serviceClient();
  await ensureAdminStoreBucket(supabase);
  const body = new Blob([JSON.stringify(value, null, 2)], { type: "application/json" });
  const { error } = await supabase.storage
    .from(ADMIN_STORE_BUCKET)
    .upload(path, body, { contentType: "application/json", upsert: true });
  if (error) {
    throw new Error(`Supabase recusou salvar arquivo mobile: ${error.message}`);
  }
}

function mobileStoragePath(licenseKey: string, machineHash: string, fileName: string) {
  return [
    "mobile",
    safeStorageSegment(licenseKey),
    safeStorageSegment(machineHash),
    ...fileName.split("/").map(safeStorageSegment),
  ].join("/");
}

function safeStorageSegment(value: unknown) {
  const clean = stringValue(value)
    .toLowerCase()
    .replace(/[^a-z0-9._-]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return clean || "item";
}

function emptyMobileSnapshot(payload: MobilePayload): Record<string, unknown> {
  return {
    settings: {
      id: "main",
      storeId: "loja_mobile",
      terminalId: "mobile_01",
      adminApiUrl: `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/license`,
      ifoodApiUrl: `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/ifood`,
      windowsBridgeUrl: "",
      printMode: "WINDOWS_BRIDGE",
      autoSync: 1,
      cashOpen: 0,
      lastSyncAt: "",
    },
    profile: payload.profile ?? {},
    products: [],
    orders: [],
    orderItems: [],
    payments: [],
    cashMovements: [],
  };
}

async function readAdminStore(): Promise<Record<string, unknown>> {
  const supabase = serviceClient();
  await ensureAdminStoreBucket(supabase);
  const { data, error } = await supabase.storage.from(ADMIN_STORE_BUCKET).download(ADMIN_STORE_OBJECT);
  if (error || !data) {
    return emptyAdminStore();
  }

  try {
    const text = await data.text();
    const parsed = JSON.parse(text) as Record<string, unknown>;
    return normalizeAdminStore(parsed);
  } catch {
    return emptyAdminStore();
  }
}

async function writeAdminStore(store: Record<string, unknown>) {
  const supabase = serviceClient();
  await ensureAdminStoreBucket(supabase);
  const body = new Blob([JSON.stringify(normalizeAdminStore(store), null, 2)], { type: "application/json" });
  const { error } = await supabase.storage
    .from(ADMIN_STORE_BUCKET)
    .upload(ADMIN_STORE_OBJECT, body, { contentType: "application/json", upsert: true });
  if (error) {
    throw new Error(`Supabase recusou salvar suporte: ${error.message}`);
  }
}

async function ensureAdminStoreBucket(supabase: ReturnType<typeof serviceClient>) {
  const { error } = await supabase.storage.createBucket(ADMIN_STORE_BUCKET, { public: false });
  if (error && !/already|exists|duplicate/i.test(error.message)) {
    throw new Error(`Supabase recusou bucket do admin: ${error.message}`);
  }
}

function emptyAdminStore(): Record<string, unknown> {
  return {
    licenses: [],
    devices: [],
    supportTickets: [],
    events: [],
  };
}

function normalizeAdminStore(value: Record<string, unknown>): Record<string, unknown> {
  const store = value && typeof value === "object" ? value : emptyAdminStore();
  if (!Array.isArray(store.licenses)) store.licenses = [];
  if (!Array.isArray(store.devices)) store.devices = [];
  if (!Array.isArray(store.supportTickets)) store.supportTickets = [];
  if (!Array.isArray(store.events)) store.events = [];
  return store;
}

function adminLicenses(store: Record<string, unknown>): Record<string, unknown>[] {
  return Array.isArray(store.licenses) ? store.licenses as Record<string, unknown>[] : [];
}

function adminDevices(store: Record<string, unknown>): Record<string, unknown>[] {
  return Array.isArray(store.devices) ? store.devices as Record<string, unknown>[] : [];
}

function adminSupportTickets(store: Record<string, unknown>): Record<string, unknown>[] {
  return Array.isArray(store.supportTickets) ? store.supportTickets as Record<string, unknown>[] : [];
}

function adminEvents(store: Record<string, unknown>): Record<string, unknown>[] {
  return Array.isArray(store.events) ? store.events as Record<string, unknown>[] : [];
}

function supportMessages(ticket: Record<string, unknown>): Record<string, unknown>[] {
  if (!Array.isArray(ticket.messages)) {
    ticket.messages = [];
  }
  return ticket.messages as Record<string, unknown>[];
}

function upsertAdminStoreLicense(store: Record<string, unknown>, payload: ClientPayload, license: Record<string, unknown>) {
  const licenseKey = normalizeLicense(payload.licenseKey);
  const list = adminLicenses(store);
  let row = list.find((item) => stringValue(item.key).toUpperCase() === licenseKey);
  const profile = payload.profile ?? {};
  const now = new Date().toISOString();
  if (!row) {
    row = {
      id: crypto.randomUUID().replaceAll("-", ""),
      key: licenseKey,
      createdAt: now,
      periodAmount: 0,
      periodUnit: "days",
    };
    list.push(row);
    store.licenses = list;
  }

  row.status = "ATIVA";
  row.plan = stringValue(license.plan) || stringValue(payload.localPlan) || "Licenca comercial";
  row.customerName = businessName(profile) || stringValue(license.customer_name) || stringValue(row.customerName) || "Cliente";
  row.email = stringValue(profile.email).toLowerCase() || stringValue(license.email);
  row.businessName = businessName(profile);
  row.ownerName = stringValue(profile.ownerName);
  row.cnpj = stringValue(profile.cnpj);
  row.phone = stringValue(profile.phone);
  row.address = stringValue(profile.address);
  row.city = stringValue(profile.city);
  row.state = stringValue(profile.state);
  row.environmentSnapshot = payload.environment ?? {};
  row.machineHash = stringValue(payload.machineHash) || stringValue(row.machineHash);
  row.machineCode = stringValue(payload.machineCode) || stringValue(row.machineCode);
  row.clientKind = normalizeClientKind(payload.clientKind);
  row.appVersion = stringValue(payload.appVersion);
  row.expiresAt = stringValue(license.expires_at) || stringValue(payload.localExpiresAt) || row.expiresAt || now;
  row.activatedAt = row.activatedAt || now;
  row.lastSeenAt = now;
  row.configSnapshot = payload.settings ?? {};
  row.metricsSnapshot = payload.metrics ?? {};
}

function upsertAdminStoreDevice(store: Record<string, unknown>, payload: ClientPayload) {
  const machineHash = stringValue(payload.machineHash);
  if (!machineHash) return;
  const list = adminDevices(store);
  const now = new Date().toISOString();
  let row = list.find((item) => stringValue(item.machineHash) === machineHash);
  if (!row) {
    row = {
      id: crypto.randomUUID().replaceAll("-", ""),
      machineHash,
      firstSeenAt: now,
    };
    list.push(row);
    store.devices = list;
  }

  row.machineCode = stringValue(payload.machineCode);
  row.licenseKey = normalizeLicense(payload.licenseKey);
  row.clientKind = normalizeClientKind(payload.clientKind);
  row.appVersion = stringValue(payload.appVersion);
  row.lastSeenAt = now;
  row.profile = payload.profile ?? {};
  row.settings = payload.settings ?? {};
  row.metrics = payload.metrics ?? {};
  row.environment = payload.environment ?? {};
}

async function writeAdminStoreClientSeen(
  payload: ClientPayload,
  license: Record<string, unknown>,
  eventType: string,
  eventMessage: string,
) {
  const store = await readAdminStore();
  upsertAdminStoreLicense(store, payload, license);
  upsertAdminStoreDevice(store, payload);
  appendAdminStoreEvent(store, eventType, eventMessage, payload);
  trimAdminStore(store);
  await writeAdminStore(store);
}

function appendAdminStoreEvent(store: Record<string, unknown>, type: string, message: string, payload: ClientPayload) {
  const list = adminEvents(store);
  list.push({
    type,
    message,
    licenseKey: normalizeLicense(payload.licenseKey),
    machineCode: stringValue(payload.machineCode),
    when: new Date().toISOString(),
  });
  store.events = list;
}

function supportMessage(sender: "cliente" | "admin", message: string, when = new Date().toISOString()) {
  return {
    id: crypto.randomUUID().replaceAll("-", ""),
    sender,
    message,
    when,
  };
}

function findSupportTicket(store: Record<string, unknown>, id: string) {
  const normalized = stringValue(id).toUpperCase();
  return adminSupportTickets(store).find((ticket) =>
    stringValue(ticket.id).toUpperCase() === normalized ||
    stringValue(ticket.shortId).toUpperCase() === normalized ||
    shortSupportId(stringValue(ticket.id)) === normalized
  );
}

function supportTicketToClient(ticket: Record<string, unknown>) {
  const id = stringValue(ticket.id);
  return {
    ...ticket,
    id,
    shortId: stringValue(ticket.shortId) || shortSupportId(id),
    messages: supportMessages(ticket)
      .slice()
      .sort((a, b) => Date.parse(stringValue(a.when)) - Date.parse(stringValue(b.when))),
  };
}

function shortSupportId(id: string) {
  const clean = stringValue(id).replaceAll("-", "").toUpperCase();
  return clean.slice(0, Math.min(8, clean.length));
}

function normalizeSupportPriority(value: unknown) {
  const clean = stringValue(value).toUpperCase();
  return ["URGENTE", "ALTA", "HIGH"].includes(clean) ? "URGENTE" : "NORMAL";
}

function normalizeSupportStatus(value: unknown) {
  const clean = stringValue(value).toUpperCase();
  if (["EM_ATENDIMENTO", "ATENDIMENTO", "ATENDENDO"].includes(clean)) return "EM_ATENDIMENTO";
  if (["RESOLVIDO", "RESOLVIDA", "FECHADO", "FECHADA"].includes(clean)) return "RESOLVIDO";
  return "ABERTO";
}

function supportStatusRank(value: unknown) {
  const status = normalizeSupportStatus(value);
  if (status === "ABERTO") return 0;
  if (status === "EM_ATENDIMENTO") return 1;
  return 2;
}

function trimAdminStore(store: Record<string, unknown>) {
  store.supportTickets = adminSupportTickets(store)
    .sort((a, b) =>
      supportStatusRank(stringValue(a.status)) - supportStatusRank(stringValue(b.status)) ||
      Date.parse(stringValue(b.updatedAt)) - Date.parse(stringValue(a.updatedAt))
    )
    .slice(0, 300);
  store.events = adminEvents(store)
    .sort((a, b) => Date.parse(stringValue(b.when)) - Date.parse(stringValue(a.when)))
    .slice(0, 500);
}

async function ensureLicense(
  payload: ClientPayload,
  options: { bindMachine: boolean; eventType: string; skipEvent?: boolean },
): Promise<
  | { ok: true; license: Record<string, unknown> }
  | { ok: false; message: string; status?: number }
> {
  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  const machineCode = stringValue(payload.machineCode);
  if (!licenseKey || !machineHash) {
    return { ok: false, message: "Chave e computador sao obrigatorios.", status: 400 };
  }

  const email = stringValue(payload.profile?.email).toLowerCase();
  if (!email || !email.includes("@")) {
    return { ok: false, message: "Email da conta invalido. Informe o email da loja.", status: 400 };
  }

  const validation = await validateSignedActivationLicense(licenseKey);
  if (!validation.ok) {
    return { ok: false, message: validation.message, status: 401 };
  }

  const supabase = serviceClient();
  const now = new Date().toISOString();
  const existing = await supabase.from("bv_licenses").select("*").eq("key", licenseKey).maybeSingle();
  if (existing.error) {
    return { ok: false, message: `Supabase recusou licenca: ${existing.error.message}`, status: 500 };
  }

  const current = existing.data as Record<string, unknown> | null;
  if (current && stringValue(current.status).toUpperCase() === "BLOQUEADA") {
    return { ok: false, message: "Esta chave esta bloqueada.", status: 401 };
  }

  const dbExpiresAt = dateValue(current?.expires_at);
  const effectiveExpiresAt = dbExpiresAt && dbExpiresAt.getTime() > validation.expiresAt.getTime()
    ? dbExpiresAt
    : validation.expiresAt;

  if (effectiveExpiresAt.getTime() <= Date.now()) {
    await supabase.from("bv_licenses").update({ status: "EXPIRADA", updated_at: now }).eq("key", licenseKey);
    return { ok: false, message: "Esta chave esta expirada.", status: 401 };
  }

  const machineInDb = stringValue(current?.machine_hash);
  if (machineInDb && machineInDb !== machineHash && !isMultiDeviceClient(payload)) {
    return { ok: false, message: "Esta chave ja foi usada em outro computador.", status: 401 };
  }

  const profile = payload.profile ?? {};
  const next = {
    key: licenseKey,
    status: "ATIVA",
    plan: stringValue(current?.plan) || stringValue(payload.localPlan) || "Licenca comercial",
    customer_name: businessName(profile) || stringValue(current?.customer_name) || "Cliente sem nome",
    email,
    business_name: businessName(profile),
    owner_name: stringValue(profile.ownerName),
    cnpj: stringValue(profile.cnpj),
    phone: stringValue(profile.phone),
    city: stringValue(profile.city),
    state: stringValue(profile.state),
    machine_hash: machineInDb || !options.bindMachine || isMultiDeviceClient(payload) ? machineInDb : machineHash,
    machine_code: machineInDb || !options.bindMachine || isMultiDeviceClient(payload) ? stringValue(current?.machine_code) : machineCode,
    app_version: stringValue(payload.appVersion),
    client_kind: normalizeClientKind(payload.clientKind),
    profile,
    settings: payload.settings ?? {},
    metrics: payload.metrics ?? {},
    expires_at: effectiveExpiresAt.toISOString(),
    activated_at: stringValue(current?.activated_at) || now,
    last_seen_at: now,
    updated_at: now,
  };

  const saved = await supabase.from("bv_licenses").upsert(next, { onConflict: "key" }).select("*").single();
  if (saved.error) {
    return { ok: false, message: `Supabase recusou salvar licenca: ${saved.error.message}`, status: 500 };
  }

  if (!options.skipEvent) {
    await appendEvent(options.eventType, "Licenca sincronizada no Supabase.", payload);
  }
  return { ok: true, license: saved.data };
}

async function appendEvent(eventType: string, message: string, payload: ClientPayload) {
  const supabase = serviceClient();
  await supabase.from("bv_license_events").insert({
    license_key: normalizeLicense(payload.licenseKey),
    machine_code: stringValue(payload.machineCode),
    event_type: eventType,
    message,
    payload,
  });
}

async function validateSignedActivationLicense(licenseKey: string): Promise<
  | { ok: true; expiresAt: Date }
  | { ok: false; message: string }
> {
  const normalized = normalizeLicense(licenseKey);
  const parts = normalized.split("-").filter(Boolean);
  if (parts.length === 4 && parts[0] === "BLV") {
    const expiresAt = parseExpiration(parts[1]);
    if (!expiresAt) return { ok: false, message: "Chave invalida. Data da licenca incorreta." };
    const expected = (await activationSignature(`BLV|${parts[1]}|${parts[2]}`)).slice(0, 10);
    if (parts[3] !== expected) return { ok: false, message: "Chave invalida. Assinatura nao confere." };
    return { ok: true, expiresAt };
  }

  if (parts.length === 3 && parts[0] === "BL") {
    const expiresAt = parseExpiration(parts[1]);
    if (!expiresAt) return { ok: false, message: "Chave invalida. Data da licenca incorreta." };
    const expected = (await activationSignature(`BL|${parts[1]}`)).slice(0, 8);
    if (parts[2] !== expected) return { ok: false, message: "Chave invalida. Assinatura nao confere." };
    return { ok: true, expiresAt };
  }

  return { ok: false, message: "Chave invalida. Use uma chave gerada pelo Balcao Livre." };
}

async function activationSignature(message: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(LICENSE_SECRET),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
  return Array.from(new Uint8Array(signature))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

function parseExpiration(value: string) {
  const clean = value.trim();
  if (!/^\d{8}(\d{4})?$/.test(clean)) return null;
  const year = Number(clean.slice(0, 4));
  const month = Number(clean.slice(4, 6)) - 1;
  const day = Number(clean.slice(6, 8));
  const hour = clean.length >= 10 ? Number(clean.slice(8, 10)) : 23;
  const minute = clean.length >= 12 ? Number(clean.slice(10, 12)) : 59;
  return new Date(year, month, day, hour, minute, clean.length >= 12 ? 0 : 59);
}

function dateValue(value: unknown) {
  const timestamp = Date.parse(stringValue(value));
  return Number.isFinite(timestamp) ? new Date(timestamp) : null;
}

function activationExpirationText(date: Date) {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getUTCFullYear()}${pad(date.getUTCMonth() + 1)}${pad(date.getUTCDate())}${pad(date.getUTCHours())}${pad(date.getUTCMinutes())}`;
}

function serviceClient() {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const key = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  if (!url || !key) {
    throw new Error("Supabase service role indisponivel.");
  }
  return createClient(url, key, { auth: { persistSession: false } });
}

async function readJson<T>(req: Request): Promise<T> {
  try {
    return await req.json() as T;
  } catch {
    return {} as T;
  }
}

function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}

function routeFromPath(pathname: string) {
  const marker = "/license";
  const index = pathname.indexOf(marker);
  if (index < 0) return pathname || "/";
  const route = pathname.slice(index + marker.length) || "/";
  return route.endsWith("/") && route.length > 1 ? route.slice(0, -1) : route;
}

function normalizeLicense(value: unknown) {
  return String(value ?? "").trim().toUpperCase().replaceAll(" ", "").replaceAll("_", "-");
}

function normalizePayloadKeys<T>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => normalizePayloadKeys(item)) as T;
  }

  if (!value || typeof value !== "object") {
    return value;
  }

  const normalized: Record<string, unknown> = {};
  for (const [key, raw] of Object.entries(value as Record<string, unknown>)) {
    const normalizedKey = key ? key[0].toLowerCase() + key.slice(1) : key;
    normalized[normalizedKey] = normalizePayloadKeys(raw);
  }

  return normalized as T;
}

function withRequestEnvironment<T extends ClientPayload>(payload: T, req: Request): T {
  const environment = payload.environment && typeof payload.environment === "object" && !Array.isArray(payload.environment)
    ? payload.environment
    : {};
  const publicIp = requestIp(req);
  environment.publicIp = stringValue(environment.publicIp) || publicIp;
  environment.forwardedFor = stringValue(req.headers.get("x-forwarded-for"));
  environment.userAgent = stringValue(environment.userAgent) || stringValue(req.headers.get("user-agent"));
  environment.requestHost = new URL(req.url).host;
  environment.serverSeenAt = new Date().toISOString();
  payload.environment = environment;
  return payload;
}

function normalizeClientKind(value: unknown) {
  const clean = stringValue(value).toLowerCase();
  return clean || "windows";
}

function normalizeTrialKind(value: unknown): "offline" | "online" {
  const clean = stringValue(value).toLowerCase();
  return clean.includes("online") ? "online" : "offline";
}

function requestIp(req: Request) {
  const forwardedFor = stringValue(req.headers.get("x-forwarded-for")).split(",")[0]?.trim();
  return stringValue(req.headers.get("cf-connecting-ip"))
    || stringValue(req.headers.get("x-real-ip"))
    || stringValue(req.headers.get("x-nf-client-connection-ip"))
    || forwardedFor
    || "unknown";
}

function isMultiDeviceClient(payload: ClientPayload) {
  const kind = normalizeClientKind(payload.clientKind);
  const code = stringValue(payload.machineCode).toUpperCase();
  return ["android", "web", "browser", "mobile", "mobile-expo"].includes(kind) ||
    kind.includes("mobile") ||
    code.startsWith("AND-") ||
    code.startsWith("MOB-") ||
    code.startsWith("WEB-");
}

function businessName(profile?: Record<string, unknown>) {
  return stringValue(profile?.businessName) || stringValue(profile?.legalName) || stringValue(profile?.ownerName);
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function numberValue(value: unknown) {
  const number = Number(value ?? 0);
  return Number.isFinite(number) ? number : 0;
}

function roundMoney(value: unknown) {
  return Math.round(numberValue(value) * 100) / 100;
}

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function normalizePublicOrderType(value: unknown) {
  const normalized = stringValue(value).toUpperCase().replace(/[^A-Z0-9]+/g, "_");
  if (["DELIVERY", "ENTREGA"].includes(normalized)) return "DELIVERY";
  if (["PICKUP", "RETIRADA", "TAKEOUT"].includes(normalized)) return "PICKUP";
  if (["TABLE", "MESA", "LOCAL", "MESA_LOCAL"].includes(normalized)) return "TABLE";
  return "";
}

function normalizePublicOrderAckStatus(value: unknown) {
  const normalized = stringValue(value).toUpperCase().replace(/[^A-Z0-9]+/g, "_");
  return [
    "NOVO",
    "RECEBIDO",
    "IMPORTADO",
    "PREPARO",
    "PREPARANDO",
    "PRONTO",
    "ROTA",
    "DESPACHADO",
    "ENTREGUE",
    "FINALIZADO",
    "CANCELADO",
    "ERRO",
  ].includes(normalized) ? normalized : "IMPORTADO";
}

function sanitizePublicOrderItems(items: PublicMenuOrderItem[]) {
  return items
    .map((item) => ({
      code: stringValue(item.code),
      name: stringValue(item.name),
      category: stringValue(item.category),
      quantity: Math.max(1, Math.trunc(numberValue(item.quantity)) || 1),
      price: roundMoney(item.price),
      note: stringValue(item.note),
    }))
    .filter((item) => item.name);
}

function publicOrderRowToClient(row: Record<string, unknown>) {
  return {
    id: stringValue(row.id),
    slug: stringValue(row.slug),
    source: stringValue(row.source),
    status: stringValue(row.status),
    customerName: stringValue(row.customer_name),
    customerPhone: stringValue(row.customer_phone),
    customerDocument: stringValue(row.customer_document),
    orderType: stringValue(row.order_type),
    tableLabel: stringValue(row.table_label),
    address: stringValue(row.address),
    district: stringValue(row.district),
    reference: stringValue(row.reference),
    desiredTime: stringValue(row.desired_time),
    notes: stringValue(row.notes),
    subtotal: numberValue(row.subtotal),
    deliveryFee: numberValue(row.delivery_fee),
    total: numberValue(row.total),
    items: Array.isArray(row.items) ? row.items : [],
    customer: row.customer && typeof row.customer === "object" ? row.customer : {},
    createdAt: stringValue(row.created_at),
  };
}

function normalizeSlug(value: string) {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 72)
    .replace(/-+$/g, "");
}

function normalizeClockText(value: unknown) {
  const text = stringValue(value)
    .replace(/[hH.,]/g, ":")
    .trim();
  if (!text) return "00:00";

  let hour = Number.NaN;
  let minute = 0;
  if (text.includes(":")) {
    const parts = text.split(":").map((part) => part.trim()).filter(Boolean);
    if (parts.length !== 2) return "00:00";
    hour = Number.parseInt(parts[0], 10);
    minute = Number.parseInt(parts[1], 10);
  } else if (/^\d{3,4}$/.test(text)) {
    const padded = text.padStart(4, "0");
    hour = Number.parseInt(padded.slice(0, 2), 10);
    minute = Number.parseInt(padded.slice(2), 10);
  } else if (/^\d{1,2}$/.test(text)) {
    hour = Number.parseInt(text, 10);
  }

  if (!Number.isInteger(hour) || !Number.isInteger(minute) || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
    return "00:00";
  }

  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function* slugCandidates(baseSlug: string) {
  yield baseSlug;
  for (let i = 1; i <= 999; i += 1) {
    yield `${String(i).padStart(3, "0")}-${baseSlug}`;
  }
}

function slugToPath(slug: string) {
  const match = slug.match(/^(\d{3})-(.+)$/);
  return match ? `${match[1]}/${match[2]}` : slug;
}

function isConflict(error: { code?: string; message?: string } | null) {
  return error?.code === "23505" || /duplicate|unique/i.test(error?.message ?? "");
}

function isMissingWhatsAppOptionsColumn(message: unknown) {
  const clean = String(message ?? "").toLowerCase();
  return clean.includes("whatsapp_message_orders_enabled")
    || clean.includes("schedule_enabled")
    || clean.includes("open_time")
    || clean.includes("close_time")
    || clean.includes("could not find")
    || clean.includes("pgrst204");
}

function withoutWhatsAppOptionsColumn<T extends Record<string, unknown>>(payload: T) {
  const {
    whatsapp_message_orders_enabled: _ignoredWhatsApp,
    schedule_enabled: _ignoredSchedule,
    open_time: _ignoredOpenTime,
    close_time: _ignoredCloseTime,
    ...legacyPayload
  } = payload;
  return legacyPayload;
}

function failMenu(message: string) {
  return { ok: false, message, slug: "", publicUrl: "", itemsPublished: 0 };
}

function resolveInlineImageUrl(url: unknown, contentType: unknown, base64: unknown) {
  const directUrl = stringValue(url);
  if (directUrl) return directUrl;

  const data = stringValue(base64);
  if (!data || data.length > 2_800_000) return "";

  const type = stringValue(contentType) || "image/png";
  if (!/^image\/(png|jpe?g|webp|gif|bmp)$/i.test(type)) return "";
  return `data:${type};base64,${data}`;
}

function mercadoPagoClientId() {
  return stringValue(Deno.env.get("MERCADO_PAGO_CLIENT_ID"));
}

function mercadoPagoClientSecret() {
  return stringValue(Deno.env.get("MERCADO_PAGO_CLIENT_SECRET"));
}

function mercadoPagoRedirectUri() {
  return stringValue(Deno.env.get("MERCADO_PAGO_REDIRECT_URI"))
    || `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/license/payments/mercadopago/oauth/callback`;
}

function mercadoPagoTokenExpiresAt(token: Record<string, unknown>) {
  const expiresIn = Math.max(60, Math.trunc(numberValue(token.expires_in)) || 15_552_000);
  return new Date(Date.now() + expiresIn * 1000).toISOString();
}

async function exchangeMercadoPagoToken(params: Record<string, string>) {
  const clientId = mercadoPagoClientId();
  const clientSecret = mercadoPagoClientSecret();
  if (!clientId || !clientSecret) {
    throw new Error("Configure MERCADO_PAGO_CLIENT_ID e MERCADO_PAGO_CLIENT_SECRET na Edge Function.");
  }

  const body = new URLSearchParams({
    client_id: clientId,
    client_secret: clientSecret,
    ...params,
  });
  const response = await fetch("https://api.mercadopago.com/oauth/token", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
  });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(`Mercado Pago recusou token: ${stringValue(data.message || data.error || response.status)}`);
  }

  return data as Record<string, unknown>;
}

async function getMercadoPagoConnection(licenseKey: string) {
  const row = await serviceClient()
    .from("bv_mercadopago_connections")
    .select("*")
    .eq("license_key", normalizeLicense(licenseKey))
    .maybeSingle();
  if (row.error) {
    throw new Error(`Supabase recusou conexao Mercado Pago: ${row.error.message}`);
  }

  return row.data as Record<string, unknown> | null;
}

async function ensureMercadoPagoAccessToken(licenseKey: string): Promise<
  | { ok: true; accessToken: string }
  | { ok: false; message: string; status?: number }
> {
  const connection = await getMercadoPagoConnection(licenseKey);
  if (!connection || !stringValue(connection.access_token)) {
    return { ok: false, message: "Mercado Pago ainda nao conectado para esta loja.", status: 404 };
  }

  const accessToken = stringValue(connection.access_token);
  const refreshToken = stringValue(connection.refresh_token);
  const expiresAt = new Date(stringValue(connection.expires_at)).getTime();
  if (!refreshToken || !expiresAt || expiresAt > Date.now() + 10 * 60_000) {
    return { ok: true, accessToken };
  }

  try {
    const token = await exchangeMercadoPagoToken({
      grant_type: "refresh_token",
      refresh_token: refreshToken,
    });
    await serviceClient()
      .from("bv_mercadopago_connections")
      .update({
        status: "CONNECTED",
        access_token: stringValue(token.access_token),
        refresh_token: stringValue(token.refresh_token) || refreshToken,
        public_key: stringValue(token.public_key),
        token_type: stringValue(token.token_type),
        scope: stringValue(token.scope),
        expires_at: mercadoPagoTokenExpiresAt(token),
        last_sync_at: new Date().toISOString(),
        last_error: "",
        updated_at: new Date().toISOString(),
      })
      .eq("license_key", normalizeLicense(licenseKey));
    return { ok: true, accessToken: stringValue(token.access_token) };
  } catch (error) {
    const message = messageFromError(error);
    await serviceClient()
      .from("bv_mercadopago_connections")
      .update({ status: "ERROR", last_error: message, updated_at: new Date().toISOString() })
      .eq("license_key", normalizeLicense(licenseKey));
    return { ok: false, message, status: 401 };
  }
}

async function mercadoPagoFetch(accessToken: string, path: string, init: RequestInit = {}) {
  const response = await fetch(`https://api.mercadopago.com${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
      ...((init.headers ?? {}) as Record<string, string>),
    },
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : {};
  if (!response.ok) {
    throw new Error(`Mercado Pago recusou operacao: ${stringValue(data.message || data.error || text || response.status)}`);
  }

  return data;
}

function mercadoPagoTerminalToClient(terminal: Record<string, unknown>) {
  const id = stringValue(terminal.id);
  const serial = id.includes("__") ? id.split("__").pop() || id : id;
  return {
    id,
    label: serial,
    posId: stringValue(terminal.pos_id),
    storeId: stringValue(terminal.store_id),
    externalPosId: stringValue(terminal.external_pos_id),
    operatingMode: stringValue(terminal.operating_mode),
  };
}

function sanitizeMercadoPagoReference(value: unknown) {
  const clean = stringValue(value).replace(/[^A-Za-z0-9_-]+/g, "-").replace(/^-+|-+$/g, "");
  return (clean || crypto.randomUUID()).slice(0, 64);
}

function normalizePointPaymentMethod(value: unknown) {
  const method = stringValue(value).toUpperCase();
  if (method.includes("DEBIT")) return "debit_card";
  if (method.includes("CRED")) return "credit_card";
  return "";
}

function formatMercadoPagoAmount(value: number) {
  return roundMoney(value).toFixed(2);
}

function normalizeMercadoPagoOrderStatus(order: Record<string, unknown>) {
  const transactions = order.transactions as Record<string, unknown> | undefined;
  const payment = Array.isArray(transactions?.payments) ? transactions.payments[0] as Record<string, unknown> : {};
  const paymentStatus = stringValue(payment?.status).toUpperCase();
  const orderStatus = stringValue(order?.status).toUpperCase();
  return paymentStatus || orderStatus || "CREATED";
}

function isMercadoPagoPaid(status: string, payment: Record<string, unknown>) {
  const normalized = status.toUpperCase();
  return ["PAID", "APPROVED", "PROCESSED"].includes(normalized)
    || ["PAID", "APPROVED", "PROCESSED"].includes(stringValue(payment?.status).toUpperCase());
}

function html(title: string, message: string, ok: boolean) {
  return new Response(`<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escapeHtml(title)}</title><body style="font-family:Segoe UI,Arial,sans-serif;background:#eef3f6;color:#17212b;margin:0;display:grid;place-items:center;min-height:100vh"><main style="max-width:520px;background:white;border:1px solid #d8e2ec;border-radius:14px;padding:28px;box-shadow:0 18px 44px rgba(22,34,45,.10)"><h1 style="margin:0 0 10px;color:${ok ? "#0f766e" : "#a11d1d"}">${escapeHtml(title)}</h1><p style="font-size:17px;line-height:1.5">${escapeHtml(message)}</p><p style="color:#607284">Pode fechar esta janela.</p></main></body></html>`, {
    status: 200,
    headers: { "Content-Type": "text/html; charset=utf-8" },
  });
}

function trialDownloadPage(title: string, message: string, ok: boolean, status = ok ? 200 : 400) {
  return new Response(`<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escapeHtml(title)}</title><body style="font-family:Segoe UI,Arial,sans-serif;background:#eef3f6;color:#17212b;margin:0;display:grid;place-items:center;min-height:100vh"><main style="max-width:560px;background:white;border:1px solid #d8e2ec;border-radius:14px;padding:30px;box-shadow:0 18px 44px rgba(22,34,45,.10)"><h1 style="margin:0 0 12px;color:${ok ? "#0f766e" : "#a11d1d"}">${escapeHtml(title)}</h1><p style="font-size:17px;line-height:1.55">${escapeHtml(message)}</p><a href="https://wa.me/5527981267551?text=Ola%2C%20preciso%20liberar%20um%20teste%20do%20Balcao%20Livre%20PDV." style="display:inline-flex;margin-top:10px;padding:12px 16px;border-radius:8px;background:#0f766e;color:white;text-decoration:none;font-weight:800">Falar no WhatsApp</a></main></body></html>`, {
    status: 200,
    headers: { ...corsHeaders, "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store, max-age=0" },
  });
}

function escapeHtml(value: unknown) {
  return stringValue(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}
