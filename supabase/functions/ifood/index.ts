import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const IFOOD_API_BASE = "https://merchant-api.ifood.com.br";
const DEFAULT_FUNCTION_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type StoreContext = {
  licenseKey?: string;
  machineHash?: string;
  machineCode?: string;
  businessName?: string;
  legalName?: string;
  cnpj?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  appVersion?: string;
};

type ConnectionRow = {
  id: string;
  license_key: string;
  machine_hash: string;
  authorization_code_verifier: string | null;
  merchant_id: string | null;
  merchant_name: string | null;
  access_token: string | null;
  refresh_token: string | null;
  token_expires_at: string | null;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    const route = routeFromPath(url.pathname);

    if (route === "/webhook") {
      return await handleWebhook(req);
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/connect/start") {
      return await handleConnectStart(req);
    }

    if (route === "/connect/finish") {
      return await handleConnectFinish(req);
    }

    if (route === "/orders/sync") {
      return await handleOrdersSync(req);
    }

    if (route === "/orders/action") {
      return await handleOrderAction(req);
    }

    if (route === "/stock/sync") {
      return await handleStockSync(req);
    }

    return json({ ok: false, message: "Rota iFood nao encontrada." }, 404);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function handleWebhook(req: Request) {
  const payload = await readJson(req);
  const events = Array.isArray(payload) ? payload : Array.isArray(payload?.events) ? payload.events : [payload];
  const supabase = serviceClient();
  const ackIds: string[] = [];

  for (const event of events) {
    const merchantId = eventMerchantId(event);
    const orderId = text(event?.orderId ?? event?.metadata?.orderId);
    const eventId = text(event?.id ?? event?.eventId);
    let connectionId: string | null = null;
    let connection: ConnectionRow | null = null;

    if (merchantId) {
      const { data } = await supabase
        .from("bv_ifood_connections")
        .select("*")
        .eq("merchant_id", merchantId)
        .maybeSingle();
      connection = data as ConnectionRow | null;
      connectionId = connection?.id ?? null;
    }

    await supabase.from("bv_ifood_webhook_events").insert({
      event_id: eventId || null,
      connection_id: connectionId,
      merchant_id: merchantId || null,
      order_id: orderId || null,
      payload: event,
    });

    if (eventId) {
      ackIds.push(eventId);
    }

    if (orderId && isOrderCreatedEvent(event)) {
      try {
        const accessToken = connection
          ? (await ensureToken(connection)).access_token ?? ""
          : await clientCredentialsAccessToken();
        const order = await ifoodJson(`/order/v1.0/orders/${encodeURIComponent(orderId)}`, accessToken);
        await supabase.from("bv_ifood_orders").upsert({
          order_id: orderId,
          connection_id: connectionId,
          merchant_id: merchantId || null,
          payload: order,
        }, { onConflict: "order_id" });
      } catch (error) {
        await supabase.from("bv_ifood_orders").upsert({
          order_id: orderId,
          connection_id: connectionId,
          merchant_id: merchantId || null,
          payload: {
            id: orderId,
            webhookEvent: event,
            warning: messageFromError(error),
          },
        }, { onConflict: "order_id" });
      }
    }
  }

  if (ackIds.length > 0) {
    try {
      const accessToken = await clientCredentialsAccessToken();
      await acknowledgeEventIds(accessToken, ackIds);
    } catch (error) {
      console.error("iFood webhook ACK failed", messageFromError(error));
    }
  }

  return json({ ok: true, receivedAt: new Date().toISOString(), events: events.length, acknowledged: ackIds.length });
}

async function handleConnectStart(req: Request) {
  const context = await readJson(req) as StoreContext;
  const webhookUrl = publicFunctionUrl() + "/webhook";
  return await connectCentralizedApp(context, webhookUrl);
}

async function connectCentralizedApp(context: StoreContext, webhookUrl: string) {
  const token = await ifoodForm("/authentication/v1.0/oauth/token", {
    grantType: "client_credentials",
    clientId: requiredEnv("IFOOD_CLIENT_ID"),
    clientSecret: requiredEnv("IFOOD_CLIENT_SECRET"),
  });

  const accessToken = text(token.accessToken);
  const merchants = await listMerchants(accessToken).catch(() => []);
  const latestMerchant = await latestWebhookMerchant();
  const merchant = merchants[0] ?? {};
  const merchantId = text(merchant.id) || latestMerchant;
  const merchantName = text(merchant.name ?? merchant.corporateName) || "Loja iFood";

  if (!merchantId) {
    return json({
      ok: false,
      message: "iFood conectado, mas nenhuma loja foi identificada. Gere um pedido de teste ou confira a permissao da loja no portal iFood.",
      webhookUrl,
    }, 400);
  }

  const expiresAt = new Date(Date.now() + Math.max(60, Number(token.expiresIn ?? 3600)) * 1000).toISOString();
  const row = baseConnectionRow(context, webhookUrl, {
    status: "connected",
    merchant_id: merchantId,
    merchant_name: merchantName,
    access_token: accessToken,
    refresh_token: text(token.refreshToken),
    token_expires_at: expiresAt,
  });

  const { data, error } = await serviceClient()
    .from("bv_ifood_connections")
    .upsert(row, { onConflict: "license_key,machine_hash" })
    .select("id")
    .single();

  if (error) throw error;

  return json({
    ok: true,
    status: "connected",
    message: `iFood conectado: ${merchantName}. Novos pedidos entram automaticamente no Delivery.`,
    connectionId: data.id,
    merchantId,
    merchantName,
    userCode: "",
    verificationUrl: "",
    verificationUrlComplete: "",
    expiresIn: Number(token.expiresIn ?? 0),
    webhookUrl,
  });
}

function baseConnectionRow(context: StoreContext, webhookUrl: string, extra: Record<string, unknown>) {
  return {
    license_key: normalized(context.licenseKey, "SEM-LICENCA"),
    machine_hash: normalized(context.machineHash, "SEM-MAQUINA"),
    machine_code: text(context.machineCode),
    business_name: text(context.businessName),
    legal_name: text(context.legalName),
    cnpj: text(context.cnpj),
    phone: text(context.phone),
    address: text(context.address),
    city: text(context.city),
    state: text(context.state),
    app_version: text(context.appVersion),
    webhook_url: webhookUrl,
    updated_at: new Date().toISOString(),
    ...extra,
  };
}

async function handleConnectFinish(req: Request) {
  const body = await readJson(req) as StoreContext & { connectionId?: string; authorizationCode?: string };
  const authorizationCode = text(body.authorizationCode);
  if (!authorizationCode) {
    return json({ ok: false, message: "Informe o codigo de autorizacao do iFood." }, 400);
  }

  const connection = await findConnection(body);
  if (!connection.authorization_code_verifier) {
    return json({ ok: false, message: "Gere o vinculo do iFood antes de finalizar." }, 400);
  }

  const token = await requestAuthorizationToken(authorizationCode, connection.authorization_code_verifier);
  const expiresAt = new Date(Date.now() + Math.max(60, Number(token.expiresIn ?? 3600)) * 1000).toISOString();
  const merchants = await listMerchants(text(token.accessToken));
  const merchant = merchants[0] ?? {};
  const merchantId = text(merchant.id);
  const merchantName = text(merchant.name ?? merchant.corporateName);
  const webhookUrl = publicFunctionUrl() + "/webhook";

  const { error } = await serviceClient()
    .from("bv_ifood_connections")
    .update({
      status: "connected",
      merchant_id: merchantId || null,
      merchant_name: merchantName || null,
      access_token: text(token.accessToken),
      refresh_token: text(token.refreshToken),
      token_expires_at: expiresAt,
      webhook_url: webhookUrl,
      updated_at: new Date().toISOString(),
    })
    .eq("id", connection.id);

  if (error) throw error;

  return json({
    ok: true,
    message: merchantName ? `iFood conectado: ${merchantName}.` : "iFood conectado.",
    connectionId: connection.id,
    merchantId,
    merchantName,
    webhookUrl,
  });
}

async function handleOrdersSync(req: Request) {
  const body = await readJson(req) as StoreContext & { connectionId?: string };
  let connection = await findConnection(body);
  connection = await ensureToken(connection);

  const storedOrders = await loadStoredOrders(connection);
  const orders = storedOrders;
  const ackIds: string[] = [];
  let pollingWarning = "";

  try {
    const events = await pollEvents(connection);
    for (const event of events) {
      const orderId = text(event.orderId ?? event.metadata?.orderId);
      const eventId = text(event.id);
      if (!orderId) continue;

      ackIds.push(eventId);

      const { data: existing } = await serviceClient()
        .from("bv_ifood_orders")
        .select("order_id")
        .eq("order_id", orderId)
        .maybeSingle();

      if (existing?.order_id) {
        continue;
      }

      const order = await ifoodJson(`/order/v1.0/orders/${encodeURIComponent(orderId)}`, connection.access_token ?? "");
      await serviceClient().from("bv_ifood_orders").insert({
        order_id: orderId,
        connection_id: connection.id,
        merchant_id: connection.merchant_id,
        payload: order,
      });

      orders.push(mapOrder(order, orderId));
    }

    if (ackIds.length > 0) {
      await acknowledgeEventIds(connection.access_token ?? "", ackIds);
    }
  } catch (error) {
    pollingWarning = messageFromError(error);
    console.error("iFood polling skipped", pollingWarning);
  }

  await serviceClient()
    .from("bv_ifood_connections")
    .update({ last_sync_at: new Date().toISOString(), updated_at: new Date().toISOString() })
    .eq("id", connection.id);

  return json({
    ok: true,
    message: orders.length === 0
      ? pollingWarning
        ? "Nenhum pedido novo recebido agora. iFood indisponivel para consulta automatica."
        : "Nenhum pedido novo recebido do iFood."
      : `${orders.length} pedido(s) iFood recebido(s).`,
    syncedAt: new Date().toISOString(),
    pollingWarning,
    orders,
  });
}

async function handleOrderAction(req: Request) {
  const body = await readJson(req) as StoreContext & {
    connectionId?: string;
    orderId?: string;
    action?: string;
    reason?: string;
    cancellationCode?: string;
    deliveredBy?: string;
  };
  const orderId = text(body.orderId);
  const action = normalizeOrderAction(body.action);
  if (!orderId) {
    return json({ ok: false, message: "Pedido iFood nao informado." }, 400);
  }

  if (!action) {
    return json({ ok: false, message: "Acao iFood invalida." }, 400);
  }

  let connection = await findConnection(body);
  connection = await ensureToken(connection);

  const command = buildOrderActionCommand(orderId, action, body);
  const ifoodResponse = await ifoodPost(command.path, connection.access_token ?? "", command.payload);
  await saveOrderAction(connection, orderId, action, command.status, ifoodResponse);

  return json({
    ok: true,
    message: command.message,
    orderId,
    status: command.status,
    deliveredBy: text(body.deliveredBy) || "",
  });
}

async function handleStockSync(req: Request) {
  const body = await readJson(req) as StoreContext & {
    connectionId?: string;
    productId?: string;
    externalCode?: string;
    productCode?: string;
    productName?: string;
    amount?: number;
    reason?: string;
  };
  const productId = text(body.productId);
  const externalCode = text(body.externalCode || body.productCode);
  const amount = Math.max(0, Math.floor(Number(body.amount ?? 0)));
  if (!productId && !externalCode) {
    return json({ ok: false, message: "Produto sem vinculo iFood. Informe productId ou codigo externo." }, 400);
  }

  let connection = await findConnection(body);
  connection = await ensureToken(connection);
  const merchantId = text(connection.merchant_id);
  if (!merchantId) {
    return json({ ok: false, message: "Loja iFood nao identificada no vinculo." }, 400);
  }

  let ifoodResponse: Record<string, unknown> = {};
  let mode = "inventory";
  if (productId) {
    ifoodResponse = await ifoodPost(
      `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/inventory`,
      connection.access_token ?? "",
      { productId, amount },
    );
  } else {
    mode = "status";
    ifoodResponse = await ifoodPatch(
      `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/products/status`,
      connection.access_token ?? "",
      [{
        externalCode,
        status: amount > 0 ? "AVAILABLE" : "UNAVAILABLE",
        resources: ["ITEM", "OPTION"],
      }],
    );
  }

  await saveStockSync(connection, {
    productId,
    externalCode,
    productCode: text(body.productCode),
    productName: text(body.productName),
    amount,
    reason: text(body.reason),
    mode,
    response: ifoodResponse,
  });

  return json({
    ok: true,
    message: productId
      ? `Estoque iFood atualizado para ${amount}.`
      : `Status iFood atualizado por codigo externo: ${amount > 0 ? "disponivel" : "indisponivel"}.`,
    productId,
    externalCode,
    amount,
    mode,
  });
}

async function findConnection(body: StoreContext & { connectionId?: string }): Promise<ConnectionRow> {
  let query = serviceClient()
    .from("bv_ifood_connections")
    .select("*")
    .limit(1);

  if (body.connectionId) {
    query = query.eq("id", body.connectionId);
  } else {
    query = query
      .eq("license_key", normalized(body.licenseKey, "SEM-LICENCA"))
      .eq("machine_hash", normalized(body.machineHash, "SEM-MAQUINA"));
  }

  const { data, error } = await query.maybeSingle();
  if (error) throw error;
  if (!data) throw new Error("Vinculo iFood nao encontrado. Clique em Conectar iFood primeiro.");
  return data as ConnectionRow;
}

async function ensureToken(connection: ConnectionRow): Promise<ConnectionRow> {
  if (connection.access_token && connection.token_expires_at && new Date(connection.token_expires_at).getTime() > Date.now() + 120000) {
    return connection;
  }

  const token = connection.refresh_token
    ? await ifoodForm("/authentication/v1.0/oauth/token", {
        grantType: "refresh_token",
        clientId: requiredEnv("IFOOD_CLIENT_ID"),
        clientSecret: requiredEnv("IFOOD_CLIENT_SECRET"),
        refreshToken: connection.refresh_token,
      })
    : await ifoodForm("/authentication/v1.0/oauth/token", {
        grantType: "client_credentials",
        clientId: requiredEnv("IFOOD_CLIENT_ID"),
        clientSecret: requiredEnv("IFOOD_CLIENT_SECRET"),
      });

  const expiresAt = new Date(Date.now() + Math.max(60, Number(token.expiresIn ?? 3600)) * 1000).toISOString();
  const next = {
    ...connection,
    access_token: text(token.accessToken),
    refresh_token: text(token.refreshToken) || connection.refresh_token || "",
    token_expires_at: expiresAt,
  };

  await serviceClient()
    .from("bv_ifood_connections")
    .update({
      access_token: next.access_token,
      refresh_token: next.refresh_token,
      token_expires_at: next.token_expires_at,
      updated_at: new Date().toISOString(),
    })
    .eq("id", connection.id);

  return next;
}

async function requestAuthorizationToken(authorizationCode: string, verifier: string) {
  return await ifoodForm("/authentication/v1.0/oauth/token", {
    grantType: "authorization_code",
    clientId: requiredEnv("IFOOD_CLIENT_ID"),
    clientSecret: requiredEnv("IFOOD_CLIENT_SECRET"),
    authorizationCode,
    authorizationCodeVerifier: verifier,
  });
}

async function clientCredentialsAccessToken() {
  const token = await ifoodForm("/authentication/v1.0/oauth/token", {
    grantType: "client_credentials",
    clientId: requiredEnv("IFOOD_CLIENT_ID"),
    clientSecret: requiredEnv("IFOOD_CLIENT_SECRET"),
  });
  return text(token.accessToken);
}

async function listMerchants(accessToken: string) {
  const data = await ifoodJson("/merchant/v1.0/merchants", accessToken);
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

async function latestWebhookMerchant() {
  const { data } = await serviceClient()
    .from("bv_ifood_webhook_events")
    .select("merchant_id")
    .not("merchant_id", "is", null)
    .order("received_at", { ascending: false })
    .limit(1)
    .maybeSingle();

  return text(data?.merchant_id);
}

async function pollEvents(connection: ConnectionRow) {
  const headers: HeadersInit = {
    Authorization: `Bearer ${connection.access_token}`,
    Accept: "application/json",
  };
  if (connection.merchant_id) {
    headers["x-polling-merchants"] = connection.merchant_id;
  }

  try {
    const response = await fetch(`${IFOOD_API_BASE}/order/v1.0/orders:polling?limit=50`, { headers });
    if (response.status === 204) return [];
    const data = await parseIfoodResponse(response);
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.events)) return data.events;
    return [];
  } catch (error) {
    console.error("iFood order polling fallback", messageFromError(error));
    const response = await fetch(`${IFOOD_API_BASE}/events/v1.0/events:polling?categories=FOOD&groups=ORDER_STATUS&limit=50`, { headers });
    if (response.status === 204) return [];
    const data = await parseIfoodResponse(response);
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.events)) return data.events;
    return [];
  }
}

async function acknowledgeEventIds(accessToken: string, ids: string[]) {
  if (!accessToken || ids.length === 0) return;

  const uniqueIds = ids.filter(Boolean);
  const response = await fetch(`${IFOOD_API_BASE}/order/v1.0/orders:acknowledgment`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify({ acknowledgedEventIds: uniqueIds }),
  });

  if (response.ok) {
    return;
  }

  const fallback = await fetch(`${IFOOD_API_BASE}/events/v1.0/events/acknowledgment`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify(uniqueIds),
  });

  if (!fallback.ok) {
    throw new Error(await fallback.text() || `ACK iFood retornou ${fallback.status}.`);
  }
}

async function loadStoredOrders(connection: ConnectionRow) {
  let query = serviceClient()
    .from("bv_ifood_orders")
    .select("order_id,payload")
    .order("imported_at", { ascending: false })
    .limit(50);

  if (connection.merchant_id) {
    query = query.or(`connection_id.eq.${connection.id},merchant_id.eq.${connection.merchant_id}`);
  } else {
    query = query.eq("connection_id", connection.id);
  }

  const { data, error } = await query;
  if (error) throw error;

  return (data ?? [])
    .map((row: Record<string, unknown>) => mapOrder(row.payload as Record<string, unknown>, text(row.order_id)))
    .filter((order: Record<string, unknown>) => text(order.orderId));
}

async function ifoodForm(path: string, payload: Record<string, string>) {
  const response = await fetch(`${IFOOD_API_BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded", Accept: "application/json" },
    body: new URLSearchParams(payload),
  });
  return await parseIfoodResponse(response);
}

async function ifoodJson(path: string, accessToken: string) {
  const response = await fetch(`${IFOOD_API_BASE}${path}`, {
    headers: { Authorization: `Bearer ${accessToken}`, Accept: "application/json" },
  });
  return await parseIfoodResponse(response);
}

async function ifoodPost(path: string, accessToken: string, payload?: Record<string, unknown>) {
  const headers: HeadersInit = {
    Authorization: `Bearer ${accessToken}`,
    Accept: "application/json",
  };
  const init: RequestInit = {
    method: "POST",
    headers,
  };

  if (payload && Object.keys(payload).length > 0) {
    headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(payload);
  }

  const response = await fetch(`${IFOOD_API_BASE}${path}`, init);
  return await parseIfoodResponse(response);
}

async function ifoodPatch(path: string, accessToken: string, payload: unknown) {
  const response = await fetch(`${IFOOD_API_BASE}${path}`, {
    method: "PATCH",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });
  return await parseIfoodResponse(response);
}

async function parseIfoodResponse(response: Response) {
  const body = await response.text();
  if (!response.ok) {
    throw new Error(body || `iFood retornou ${response.status}.`);
  }
  return body ? JSON.parse(body) : {};
}

function mapOrder(order: Record<string, unknown>, fallbackOrderId: string) {
  const customer = valueAt(order, ["customer"]) as Record<string, unknown> | undefined;
  const delivery = valueAt(order, ["delivery"]) as Record<string, unknown> | undefined;
  const phoneObject = valueAt(customer ?? {}, ["phone"]) as Record<string, unknown> | undefined;
  const deliveryAddress = valueAt(delivery ?? {}, ["deliveryAddress"]) as Record<string, unknown> | undefined;
  const phoneValue = customer ? valueAt(customer, ["phone"]) : undefined;
  const phone = typeof phoneValue === "object" && phoneValue !== null
    ? text((phoneValue as Record<string, unknown>).number)
    : text(phoneValue);
  const deliveredBy = text(delivery?.deliveredBy ?? order.deliveredBy).toUpperCase();
  const pickupCode = text(delivery?.pickupCode ?? order.pickupCode);
  const deliveryLocalizer = text(phoneObject?.localizer ?? delivery?.deliveryCode ?? order.deliveryCode);
  const scheduled = valueAt(order, ["scheduled", "scheduling"]) as Record<string, unknown> | undefined;
  const createdAt = text(order.createdAt ?? order.created_at ?? order.createdDate);
  const orderTiming = text(order.orderTiming ?? order.timing ?? order.type).toUpperCase();
  const preparationStartDateTime = text(
    scheduled?.preparationStartDateTime ??
    scheduled?.preparation_start_date_time ??
    scheduled?.preparationStart ??
    order.preparationStartDateTime ??
    order.preparationStart,
  );
  const confirmationBase = orderTiming === "SCHEDULED" && preparationStartDateTime
    ? preparationStartDateTime
    : createdAt;
  const confirmationDeadlineAt = addMinutesIso(confirmationBase, 8);
  const shipmentInfo = [
    deliveredBy ? deliveredBy === "IFOOD" ? "Entrega iFood" : "Entrega propria" : "",
    pickupCode ? `Codigo coleta ${pickupCode}` : "",
    deliveryLocalizer ? `Localizador ${deliveryLocalizer}` : "",
  ].filter(Boolean).join(" | ");
  const items = Array.isArray(order.items) ? order.items as Record<string, unknown>[] : [];
  const mappedItems = items.map((item, index) => {
    const quantity = numberAt(item, ["quantity"], 1);
    const total = numberAt(item, ["totalPrice", "price", "unitPrice"], 0);
    const unitPrice = numberAt(item, ["unitPrice"], quantity > 0 ? total / quantity : total);
    return {
      code: text(item.externalCode ?? item.ean ?? String(index + 1).padStart(6, "0")),
      productId: text(item.productId ?? item.catalogItemId ?? item.id),
      name: text(item.name ?? "ITEM IFOOD").toUpperCase(),
      quantity: Math.max(1, Math.round(quantity)),
      unitPrice,
      notes: text(item.observations ?? item.note),
    };
  });

  const total = numberAt(order, ["total", "totalPrice", "orderAmount"], mappedItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0));
  const street = text(deliveryAddress?.streetName ?? deliveryAddress?.street);
  const number = text(deliveryAddress?.streetNumber ?? deliveryAddress?.number);
  const complement = text(deliveryAddress?.complement);
  const district = text(deliveryAddress?.neighborhood);
  const city = text(deliveryAddress?.city);

  return {
    orderId: text(order.id ?? fallbackOrderId),
    displayId: text(order.displayId ?? order.shortReference ?? fallbackOrderId.slice(-6)),
    status: text(order.status ?? order.orderStatus),
    createdAt,
    orderTiming,
    preparationStartDateTime,
    confirmationDeadlineAt,
    orderType: text(order.orderType ?? order.type ?? "DELIVERY").toUpperCase(),
    deliveredBy,
    pickupCode,
    deliveryLocalizer,
    shipmentInfo,
    customerName: text(customer?.name ?? "CLIENTE IFOOD"),
    customerDocument: text(customer?.documentNumber ?? customer?.document),
    phone,
    address: [street, number, complement, city].filter(Boolean).join(", "),
    district,
    notes: text(order.observations ?? order.note),
    total,
    items: mappedItems,
  };
}

function normalizeOrderAction(value?: string) {
  const action = text(value).toLowerCase();
  if (["confirm", "confirmar", "accept", "aceitar"].includes(action)) return "confirm";
  if (["prepare", "preparar", "preparo", "startpreparation"].includes(action)) return "prepare";
  if (["ready", "pronto", "readytopickup"].includes(action)) return "ready";
  if (["dispatch", "despachar", "enviar", "sair"].includes(action)) return "dispatch";
  if (["cancel", "cancelar"].includes(action)) return "cancel";
  return "";
}

function addMinutesIso(value: string, minutes: number) {
  if (!value) return "";
  const time = Date.parse(value);
  if (!Number.isFinite(time)) return "";
  return new Date(time + minutes * 60000).toISOString();
}

function buildOrderActionCommand(
  orderId: string,
  action: string,
  body: { reason?: string; cancellationCode?: string; deliveredBy?: string },
) {
  const encoded = encodeURIComponent(orderId);
  switch (action) {
    case "confirm":
      return {
        path: `/order/v1.0/orders/${encoded}/confirm`,
        payload: undefined,
        status: "CONFIRMADO",
        message: "Pedido iFood confirmado.",
      };
    case "prepare":
      return {
        path: `/order/v1.0/orders/${encoded}/startPreparation`,
        payload: undefined,
        status: "PREPARANDO",
        message: "Pedido iFood marcado como em preparo.",
      };
    case "ready":
      return {
        path: `/order/v1.0/orders/${encoded}/readyToPickup`,
        payload: undefined,
        status: "PRONTO",
        message: "Pedido iFood marcado como pronto.",
      };
    case "dispatch":
      return {
        path: `/order/v1.0/orders/${encoded}/dispatch`,
        payload: { deliveredBy: text(body.deliveredBy) || "MERCHANT" },
        status: "DESPACHADO",
        message: "Pedido iFood despachado.",
      };
    case "cancel":
      const cancellationCode = cancellationCodeFrom(body.cancellationCode ?? body.reason);
      const reason = cancellationReasonFrom(body.reason ?? body.cancellationCode, cancellationCode);
      return {
        path: `/order/v1.0/orders/${encoded}/requestCancellation`,
        payload: {
          cancellationCode,
          reason,
        },
        status: "CANCELAMENTO",
        message: "Cancelamento iFood solicitado.",
      };
    default:
      throw new Error("Acao iFood invalida.");
  }
}

function cancellationCodeFrom(value?: string) {
  const match = text(value).match(/\d+/);
  return match?.[0] || "501";
}

function cancellationReasonFrom(value: unknown, cancellationCode: string) {
  const raw = text(value);
  const withoutCode = raw.replace(/^\d+\s*[-:]\s*/, "").trim();
  if (withoutCode) {
    return withoutCode;
  }

  switch (cancellationCode) {
    case "501":
      return "Loja sem produto";
    case "502":
      return "Loja sem capacidade";
    case "503":
      return "Cliente solicitou cancelamento";
    case "504":
      return "Endereco fora da area";
    default:
      return "Cancelamento solicitado pelo restaurante";
  }
}

async function saveOrderAction(
  connection: ConnectionRow,
  orderId: string,
  action: string,
  status: string,
  ifoodResponse: Record<string, unknown>,
) {
  const supabase = serviceClient();
  const { data } = await supabase
    .from("bv_ifood_orders")
    .select("payload")
    .eq("order_id", orderId)
    .maybeSingle();

  const current = typeof data?.payload === "object" && data?.payload !== null
    ? data.payload as Record<string, unknown>
    : { id: orderId };
  const history = Array.isArray(current.balcaoLivreActions)
    ? current.balcaoLivreActions as unknown[]
    : [];
  const payload = {
    ...current,
    balcaoLivreStatus: status,
    balcaoLivreLastAction: action,
    balcaoLivreActionAt: new Date().toISOString(),
    balcaoLivreActions: [
      ...history,
      {
        action,
        status,
        at: new Date().toISOString(),
        response: ifoodResponse,
      },
    ],
  };

  const { error } = await supabase
    .from("bv_ifood_orders")
    .upsert({
      order_id: orderId,
      connection_id: connection.id,
      merchant_id: connection.merchant_id,
      payload,
    }, { onConflict: "order_id" });

  if (error) throw error;
}

async function saveStockSync(
  connection: ConnectionRow,
  sync: {
    productId: string;
    externalCode: string;
    productCode: string;
    productName: string;
    amount: number;
    reason: string;
    mode: string;
    response: Record<string, unknown>;
  },
) {
  const { error } = await serviceClient()
    .from("bv_ifood_stock_sync")
    .insert({
      connection_id: connection.id,
      merchant_id: connection.merchant_id,
      product_id: sync.productId || null,
      external_code: sync.externalCode || null,
      product_code: sync.productCode || null,
      product_name: sync.productName || null,
      amount: sync.amount,
      reason: sync.reason || null,
      mode: sync.mode,
      payload: sync.response,
      synced_at: new Date().toISOString(),
    });

  if (error) {
    console.warn("iFood stock sync log skipped", error.message);
  }
}

function routeFromPath(pathname: string) {
  const marker = "/ifood";
  const index = pathname.indexOf(marker);
  if (index < 0) return "/";
  const route = pathname.slice(index + marker.length);
  return route || "/";
}

function eventMerchantId(event: Record<string, unknown>) {
  const direct = text(event?.merchantId ?? (event?.merchant as Record<string, unknown> | undefined)?.id);
  if (direct) return direct;

  const merchantIds = event?.merchantIds;
  if (Array.isArray(merchantIds) && merchantIds.length > 0) {
    return text(merchantIds[0]);
  }

  return "";
}

function isOrderCreatedEvent(event: Record<string, unknown>) {
  const fullCode = text(event.fullCode).toUpperCase();
  const code = text(event.code).toUpperCase();
  return Boolean(text(event.orderId)) && (fullCode === "PLACED" || code === "PLC" || fullCode === "CONFIRMED" || code === "CFM");
}

async function readJson(req: Request) {
  try {
    const textBody = await req.text();
    return textBody ? withTopLevelCamelAliases(JSON.parse(textBody)) : {};
  } catch {
    return {};
  }
}

function withTopLevelCamelAliases(value: unknown) {
  if (!value || Array.isArray(value) || typeof value !== "object") {
    return value;
  }

  const payload = value as Record<string, unknown>;
  for (const key of Object.keys(payload)) {
    if (!key || key[0] !== key[0].toUpperCase()) {
      continue;
    }

    const camelKey = key[0].toLowerCase() + key.slice(1);
    if (!(camelKey in payload)) {
      payload[camelKey] = payload[key];
    }
  }

  return payload;
}

function serviceClient() {
  const url = requiredEnv("SUPABASE_URL");
  const key = requiredEnv("SUPABASE_SERVICE_ROLE_KEY");
  return createClient(url, key, { auth: { persistSession: false } });
}

function publicFunctionUrl() {
  return (Deno.env.get("IFOOD_PUBLIC_FUNCTION_URL") ?? DEFAULT_FUNCTION_URL).replace(/\/$/, "");
}

function requiredEnv(name: string) {
  const value = Deno.env.get(name);
  if (!value) throw new Error(`Variavel ${name} nao configurada no Supabase.`);
  return value;
}

function json(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}

function text(value: unknown) {
  return String(value ?? "").trim();
}

function normalized(value: unknown, fallback: string) {
  const content = text(value).toUpperCase();
  return content || fallback;
}

function valueAt(source: Record<string, unknown>, keys: string[]) {
  for (const key of keys) {
    if (source && source[key] !== undefined && source[key] !== null) {
      return source[key];
    }
  }
  return undefined;
}

function numberAt(source: Record<string, unknown>, keys: string[], fallback: number) {
  const value = valueAt(source, keys);
  const number = typeof value === "number" ? value : Number(String(value ?? "").replace(",", "."));
  return Number.isFinite(number) ? number : fallback;
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}
