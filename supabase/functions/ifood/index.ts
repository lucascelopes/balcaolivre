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

    if (route === "/catalog/sync") {
      return await handleCatalogSync(req);
    }

    if (route === "/merchant/status") {
      return await handleMerchantStatus(req);
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

    if (orderId) {
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
        if (isOrderCreatedEvent(event)) {
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
    const events = await pollEventsWithFreshToken(connection);
    for (const event of events) {
      const orderId = text(event.orderId ?? event.metadata?.orderId);
      const eventId = text(event.id);
      if (!orderId) continue;

      ackIds.push(eventId);

      const order = await ifoodJson(`/order/v1.0/orders/${encodeURIComponent(orderId)}`, connection.access_token ?? "");
      await serviceClient().from("bv_ifood_orders").upsert({
        order_id: orderId,
        connection_id: connection.id,
        merchant_id: connection.merchant_id,
        payload: order,
      }, { onConflict: "order_id" });

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

  const cancellationReasons = action === "cancel"
    ? await ifoodJson(`/order/v1.0/orders/${encodeURIComponent(orderId)}/cancellationReasons`, connection.access_token ?? "")
    : undefined;
  const command = buildOrderActionCommand(orderId, action, body);
  const ifoodResponse = await ifoodPost(command.path, connection.access_token ?? "", command.payload);
  await saveOrderAction(connection, orderId, action, command.status, {
    actionResponse: ifoodResponse,
    cancellationReasons,
  });

  return json({
    ok: true,
    message: action === "cancel"
      ? `${command.message} Motivos de cancelamento consultados antes da solicitacao.`
      : command.message,
    orderId,
    status: command.status,
    deliveredBy: text(body.deliveredBy) || "",
    cancellationReasonsFetched: action === "cancel",
  });
}

async function handleStockSync(req: Request) {
  const body = await readJson(req) as StoreContext & {
    connectionId?: string;
    productId?: string;
    externalCode?: string;
    productCode?: string;
    productName?: string;
    price?: number;
    amount?: number;
    reason?: string;
    imageDataUrl?: string;
    imageUrl?: string;
  };
  const productId = text(body.productId);
  const externalCode = text(body.externalCode || body.productCode);
  const amount = Math.max(0, Math.floor(Number(body.amount ?? 0)));
  const rawPrice = Number(body.price ?? 0);
  const price = Number.isFinite(rawPrice) ? Math.max(0, rawPrice) : 0;
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
  const responseParts: Record<string, unknown> = {};
  const modeParts: string[] = [];
  let statusWarning = "";
  let inventoryWarning = "";
  let itemStatusWarning = "";
  let itemPriceWarning = "";
  let imageWarning = "";
  let imageUpdated = false;
  let catalogLink: CatalogProduct | null = null;
  async function resolveCatalogLink() {
    catalogLink ??= await findIFoodCatalogProductLink(
      merchantId,
      connection.access_token ?? "",
      {
        productId,
        externalCode,
        productName: text(body.productName),
        imageDataUrl: "",
      },
    );
    return catalogLink;
  }

  const inventoryProductId = isUuid(productId) ? productId : "";
  const statusPayload: { productId?: string; externalCode?: string } = inventoryProductId
    ? { productId: inventoryProductId }
    : { externalCode: externalCode || productId };
  let catalogContext = "DEFAULT";
  if (price > 0 || amount === 0) {
    try {
      catalogContext = (await resolveCatalogLink()).catalogContext || catalogContext;
    } catch {
      catalogContext = "DEFAULT";
    }
  }

  if (inventoryProductId) {
    const inventoryResponse = await ifoodPost(
      `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/inventory`,
      connection.access_token ?? "",
      { productId: inventoryProductId, amount },
    );
    responseParts.inventory = inventoryResponse;
    modeParts.push("inventory");
  } else if (productId) {
    inventoryWarning = "Produto iFood sem productId UUID; apliquei disponibilidade/foto pelo catalogo.";
    responseParts.inventoryWarning = inventoryWarning;
    modeParts.push("inventory-warning");
  }

  if (statusPayload.externalCode || statusPayload.productId) {
    try {
      const statusResponse = await ifoodPatch(
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/products/status`,
        connection.access_token ?? "",
        [{
          ...statusPayload,
          status: amount > 0 ? "AVAILABLE" : "UNAVAILABLE",
          statusByCatalog: [{ status: amount > 0 ? "AVAILABLE" : "UNAVAILABLE", catalogContext }],
          resources: ["ITEM", "OPTION"],
        }],
      );
      responseParts.status = await waitIFoodCatalogBatch(
        merchantId,
        connection.access_token ?? "",
        statusResponse,
      );
      modeParts.push("status");
    } catch (error) {
      statusWarning = messageFromError(error);
      responseParts.statusWarning = statusWarning;
    }

    if (price > 0) {
      try {
        responseParts.productPrice = await updateIFoodProductPrice(
          merchantId,
          connection.access_token ?? "",
          statusPayload,
          price,
          catalogContext,
        );
        modeParts.push("product-price");
      } catch (error) {
        itemPriceWarning = messageFromError(error);
        responseParts.productPriceWarning = itemPriceWarning;
      }
    }
  }

  if (productId || externalCode) {
    try {
      const link = await resolveCatalogLink();
      const itemId = text(link.itemId);
      if (itemId) {
        responseParts.itemStatus = await updateIFoodItemStatus(
          merchantId,
          connection.access_token ?? "",
          itemId,
          amount > 0,
        );
        modeParts.push("item-status");

        if (price > 0) {
          responseParts.itemPrice = await updateIFoodItemPrice(
            merchantId,
            connection.access_token ?? "",
            itemId,
            price,
          );
          modeParts.push("item-price");
        }

      }
    } catch (error) {
      if (price > 0 && amount > 0) {
        itemPriceWarning = messageFromError(error);
        responseParts.itemPriceWarning = itemPriceWarning;
      } else {
        itemStatusWarning = messageFromError(error);
        responseParts.itemStatusWarning = itemStatusWarning;
      }
      modeParts.push("item-warning");
    }
  }

  const imageDataUrl = await resolveIFoodImageDataUrl(body);
  if (imageDataUrl) {
    try {
      responseParts.image = await updateIFoodProductImage(
        merchantId,
        connection.access_token ?? "",
        {
          productId,
          externalCode,
          productName: text(body.productName),
          imageDataUrl,
        },
      );
      imageUpdated = true;
      modeParts.push("image");
    } catch (error) {
      imageWarning = messageFromError(error);
      responseParts.imageWarning = imageWarning;
      modeParts.push("image-warning");
    }
  }

  ifoodResponse = responseParts;
  const mode = modeParts.length > 0 ? modeParts.join("+") : "status";
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

  const statusBaseMessage = inventoryProductId
    ? `Estoque iFood atualizado para ${amount}.`
    : `Disponibilidade iFood atualizada: ${amount > 0 ? "disponivel" : "indisponivel"}.`;
  const inventoryMessage = inventoryWarning ? ` ${inventoryWarning}` : "";
  const itemMessage = itemStatusWarning || itemPriceWarning
    ? ` Item pendente: ${itemStatusWarning || itemPriceWarning}`
    : "";
  const imageMessage = imageUpdated
    ? " Foto enviada."
    : imageWarning
      ? ` Foto pendente: ${imageWarning}`
      : "";
  return json({
    ok: true,
    message: statusBaseMessage + inventoryMessage + itemMessage + imageMessage,
    productId,
    externalCode,
    amount,
    mode,
    statusWarning,
    inventoryWarning,
    itemStatusWarning,
    itemPriceWarning,
    imageUpdated,
    imageWarning,
    itemUpdated: !itemStatusWarning && !itemPriceWarning && modeParts.some((mode) => mode.startsWith("item-")),
  });
}

async function updateIFoodItemStatus(
  merchantId: string,
  accessToken: string,
  itemId: string,
  isAvailable: boolean,
) {
  const status = isAvailable ? "AVAILABLE" : "UNAVAILABLE";
  const response = await ifoodPatchFirst(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/items/status`,
    accessToken,
    [
      { itemId, status, statusByCatalog: [{ status, catalogContext: "DEFAULT" }] },
      { itemId, status },
      [{ itemId, status }],
      [{ id: itemId, status }],
    ],
  );
  return await waitIFoodCatalogBatch(merchantId, accessToken, response);
}

async function updateIFoodProductPrice(
  merchantId: string,
  accessToken: string,
  statusPayload: { productId?: string; externalCode?: string },
  price: number,
  catalogContext: string,
) {
  const value = Math.round(Math.max(0, price) * 100) / 100;
  const path = `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/products/price`;
  const base = {
    ...statusPayload,
    resources: ["ITEM", "OPTION"],
  };
  const payloads = [
    [{ ...base, price: value, priceByCatalog: [{ value, catalogContext }] }],
    [{ ...base, value, priceByCatalog: [{ value, catalogContext }] }],
    [{ ...base, price: { value }, priceByCatalog: [{ value, catalogContext }] }],
    [{ ...base, amount: value, priceByCatalog: [{ value, catalogContext }] }],
  ];
  const responses: unknown[] = [];
  const errors: string[] = [];
  for (const payload of payloads) {
    try {
      const response = await ifoodPatch(path, accessToken, payload);
      responses.push(await waitIFoodCatalogBatch(merchantId, accessToken, response));
    } catch (error) {
      errors.push(messageFromError(error));
    }
  }

  if (responses.length === 0) {
    throw new Error(errors.filter(Boolean).join(" | ") || "iFood nao aceitou atualizar preco do produto.");
  }

  return { responses, errors: errors.filter(Boolean) };
}

async function updateIFoodItemPrice(
  merchantId: string,
  accessToken: string,
  itemId: string,
  price: number,
) {
  const value = Math.round(Math.max(0, price) * 100) / 100;
  const response = await ifoodPatchFirst(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/items/price`,
    accessToken,
    [
      { itemId, price: { value }, priceByCatalog: [{ value, catalogContext: "DEFAULT" }] },
      { itemId, price: { value } },
      [{ itemId, price: { value } }],
      [{ itemId, price: value }],
    ],
  );
  return await waitIFoodCatalogBatch(merchantId, accessToken, response);
}

async function ifoodPatchFirst(path: string, accessToken: string, payloads: unknown[]) {
  let lastError = "";
  for (const payload of payloads) {
    try {
      return await ifoodPatch(path, accessToken, payload);
    } catch (error) {
      lastError = messageFromError(error);
    }
  }

  throw new Error(lastError || "iFood nao aceitou a atualizacao do item.");
}

type IFoodProductSaleTarget = {
  productId: string;
  externalCode: string;
  productName: string;
  price: number;
  isAvailable: boolean;
};

async function updateIFoodItemSaleData(
  merchantId: string,
  accessToken: string,
  target: IFoodProductSaleTarget,
) {
  const link = await findIFoodCatalogProductLink(merchantId, accessToken, {
    productId: target.productId,
    externalCode: target.externalCode,
    productName: target.productName,
    imageDataUrl: "",
  });
  if (!link.itemId) {
    throw new Error("Nao encontrei o item do catalogo iFood para atualizar preco/status.");
  }

  const flat = await ifoodJson(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/items/${encodeURIComponent(link.itemId)}/flat`,
    accessToken,
  );
  const payload = buildIFoodItemSalePayload(flat, link, target);
  const update = await ifoodPut(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/items`,
    accessToken,
    payload,
  );
  return await waitIFoodCatalogBatch(merchantId, accessToken, update);
}

function buildIFoodItemSalePayload(
  flat: unknown,
  link: CatalogProduct,
  target: IFoodProductSaleTarget,
) {
  const source = isRecord(flat) ? flat : {};
  const payload = isRecord(source.item)
    ? structuredClone(source) as Record<string, unknown>
    : {
        item: Object.fromEntries(
          Object.entries(source).filter(([key]) => !["product", "products", "optionGroups", "options"].includes(key)),
        ),
        products: source.products,
        optionGroups: source.optionGroups,
        options: source.options,
      } as Record<string, unknown>;

  payload.item = isRecord(payload.item) ? { ...(payload.item as Record<string, unknown>) } : {};
  applyIFoodSaleFields(payload.item as Record<string, unknown>, target);

  let products = Array.isArray(payload.products)
    ? payload.products.filter(isRecord).map((product) => ({ ...product }))
    : [];
  if (products.length === 0) {
    const product = isRecord(payload.product)
      ? { ...(payload.product as Record<string, unknown>) }
      : {};
    products = [product];
  }

  let updated = false;
  for (const product of products) {
    const id = firstText(product.id, product.productId);
    const externalCode = firstText(product.externalCode, product.ean);
    if ((link.productId && id && id === link.productId)
      || (link.externalCode && externalCode && externalCode === link.externalCode)
      || products.length === 1) {
      applyIFoodSaleFields(product, target);
      if (!firstText(product.name, product.productName)) {
        product.name = target.productName || link.name || "Produto";
      }
      updated = true;
    }
  }

  if (!updated && products.length > 0) {
    applyIFoodSaleFields(products[0], target);
    if (!firstText(products[0].name, products[0].productName)) {
      products[0].name = target.productName || link.name || "Produto";
    }
  }

  payload.products = products;
  delete payload.product;
  return payload;
}

function applyIFoodSaleFields(target: Record<string, unknown>, sale: IFoodProductSaleTarget) {
  const status = sale.isAvailable ? "AVAILABLE" : "UNAVAILABLE";
  target.status = status;
  target.itemStatus = status;
  target.available = sale.isAvailable;
  target.isAvailable = sale.isAvailable;
  target.statusByCatalog = updateIFoodCatalogStatusArray(target.statusByCatalog, status);

  if (sale.price > 0) {
    const value = Math.round(sale.price * 100) / 100;
    setIFoodMoneyValue(target, "price", value);
    setIFoodMoneyValue(target, "itemPrice", value);
    setIFoodMoneyValue(target, "unitPrice", value);
    target.priceByCatalog = updateIFoodCatalogPriceArray(target.priceByCatalog, value);
  }
}

function setIFoodMoneyValue(target: Record<string, unknown>, key: string, value: number) {
  const current = target[key];
  target[key] = isRecord(current) ? { ...current, value } : { value };
}

function updateIFoodCatalogStatusArray(value: unknown, status: string) {
  const rows = Array.isArray(value) && value.length > 0
    ? value.filter(isRecord).map((row) => ({ ...row, status }))
    : [{ catalogContext: "DEFAULT", status }];
  return rows;
}

function updateIFoodCatalogPriceArray(value: unknown, price: number) {
  const rows = Array.isArray(value) && value.length > 0
    ? value.filter(isRecord).map((row) => ({ ...row, value: price }))
    : [{ catalogContext: "DEFAULT", value: price }];
  return rows;
}

async function waitIFoodCatalogBatch(
  merchantId: string,
  accessToken: string,
  response: unknown,
) {
  const record = isRecord(response) ? response : {};
  const batchId = firstText(record.batchId, record.id);
  if (!batchId) {
    return response;
  }

  let lastStatus: unknown = response;
  for (let attempt = 0; attempt < 10; attempt++) {
    await delay(1000);
    lastStatus = await ifoodJson(
      `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/batch/${encodeURIComponent(batchId)}`,
      accessToken,
    );
    const status = firstText(
      (lastStatus as Record<string, unknown>)?.status,
      (lastStatus as Record<string, unknown>)?.batchStatus,
      (lastStatus as Record<string, unknown>)?.state,
    ).toUpperCase();
    if (["COMPLETED", "SUCCESS", "DONE", "FINISHED", "CONCLUDED"].includes(status)) {
      const results = isRecord(lastStatus) && Array.isArray(lastStatus.results)
        ? lastStatus.results.filter(isRecord)
        : [];
      const failed = results.find((result) => {
        const value = firstText(result.result, result.status, result.state).toUpperCase();
        return value && !["SUCCESS", "COMPLETED", "DONE", "FINISHED", "CONCLUDED"].includes(value);
      });
      if (failed) {
        throw new Error(messageFromError(failed) || JSON.stringify(failed));
      }

      return { response, batch: lastStatus };
    }
    if (["FAILED", "ERROR", "CANCELED", "CANCELLED"].includes(status)) {
      throw new Error(messageFromError(lastStatus) || `Batch iFood ${batchId} falhou.`);
    }
  }

  return { response, batch: lastStatus, pending: true };
}

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

type IFoodProductImageTarget = {
  productId: string;
  externalCode: string;
  productName: string;
  imageDataUrl: string;
};

async function resolveIFoodImageDataUrl(body: { imageDataUrl?: string; imageUrl?: string }) {
  const dataUrl = text(body.imageDataUrl);
  if (dataUrl) {
    if (!isSupportedIFoodImageDataUrl(dataUrl)) {
      throw new Error("Foto do iFood precisa estar em JPG ou PNG.");
    }

    return dataUrl;
  }

  const imageUrl = text(body.imageUrl);
  if (!imageUrl) {
    return "";
  }

  const response = await fetch(imageUrl);
  if (!response.ok) {
    throw new Error(`Nao consegui baixar a foto do produto (${response.status}).`);
  }

  const contentType = text(response.headers.get("content-type")).split(";")[0].toLowerCase();
  if (!["image/jpeg", "image/jpg", "image/png"].includes(contentType)) {
    throw new Error("Foto do iFood precisa estar em JPG ou PNG.");
  }

  const buffer = await response.arrayBuffer();
  if (buffer.byteLength > 5_000_000) {
    throw new Error("Foto do iFood passou de 5MB.");
  }

  return `data:${contentType === "image/jpg" ? "image/jpeg" : contentType};base64,${bytesToBase64(new Uint8Array(buffer))}`;
}

function isSupportedIFoodImageDataUrl(value: string) {
  return /^data:image\/(?:png|jpe?g);base64,/i.test(value)
    && value.length <= 6_800_000;
}

function bytesToBase64(bytes: Uint8Array) {
  let binary = "";
  const chunkSize = 0x8000;
  for (let index = 0; index < bytes.length; index += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(index, index + chunkSize));
  }

  return btoa(binary);
}

async function updateIFoodProductImage(
  merchantId: string,
  accessToken: string,
  target: IFoodProductImageTarget,
) {
  const upload = await ifoodPost(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/image/upload`,
    accessToken,
    { image: target.imageDataUrl },
  );
  const imagePath = text((upload as Record<string, unknown>).imagePath);
  if (!imagePath) {
    throw new Error("iFood recebeu a foto, mas nao retornou imagePath.");
  }

  const link = await findIFoodCatalogProductLink(merchantId, accessToken, target);
  if (!link.itemId) {
    throw new Error("Nao encontrei o item do catalogo iFood para aplicar a foto.");
  }

  const flat = await ifoodJson(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/items/${encodeURIComponent(link.itemId)}/flat`,
    accessToken,
  );
  const payload = buildIFoodItemImagePayload(flat, link, imagePath, target.productName);
  const update = await ifoodPut(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/items`,
    accessToken,
    payload,
  );

  return {
    imagePath,
    itemId: link.itemId,
    productId: link.productId,
    externalCode: link.externalCode,
    update,
  };
}

async function findIFoodCatalogProductLink(
  merchantId: string,
  accessToken: string,
  target: { productId: string; externalCode: string },
) {
  const products = new Map<string, CatalogProduct>();
  const warnings: string[] = [];
  const catalogs = await ifoodJson(
    `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs`,
    accessToken,
  )
    .then(catalogsFromResponse)
    .catch((error) => {
      warnings.push(`Catalogos: ${messageFromError(error)}`);
      return [] as Record<string, unknown>[];
    });

  for (const catalog of catalogs) {
    const catalogId = firstText(catalog.catalogId, catalog.id, catalog.groupId);
    const groupId = firstText(catalog.groupId, catalog.id, catalog.catalogId);
    if (groupId) {
      for (const path of [
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs/${encodeURIComponent(groupId)}/sellableItems`,
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs/${encodeURIComponent(groupId)}/unsellableItems`,
      ]) {
        await appendCatalogProductsFromPath(path, accessToken, products, warnings);
      }
    }

    if (catalogId) {
      await appendCatalogProductsFromPath(
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs/${encodeURIComponent(catalogId)}/categories`,
        accessToken,
        products,
        warnings,
      );
    }
  }

  const productId = target.productId.toLowerCase();
  const externalCode = target.externalCode.toLowerCase();
  const match = [...products.values()].find((product) =>
    (productId && [product.productId, product.itemId].some((value) => value.toLowerCase() === productId))
    || (externalCode && product.externalCode.toLowerCase() === externalCode)
  );

  if (!match) {
    const suffix = warnings.length > 0 ? ` ${warnings.join(" | ")}` : "";
    throw new Error(`Produto vinculado nao apareceu no catalogo iFood.${suffix}`.trim());
  }

  return match;
}

function buildIFoodItemImagePayload(
  flat: unknown,
  link: CatalogProduct,
  imagePath: string,
  fallbackName: string,
) {
  const source = isRecord(flat) ? flat : {};
  const payload = isRecord(source.item)
    ? structuredClone(source) as Record<string, unknown>
    : {
        item: Object.fromEntries(
          Object.entries(source).filter(([key]) => !["product", "products", "optionGroups", "options"].includes(key)),
        ),
        products: source.products,
        optionGroups: source.optionGroups,
        options: source.options,
      } as Record<string, unknown>;

  let products = Array.isArray(payload.products)
    ? payload.products.filter(isRecord).map((product) => ({ ...product }))
    : [];
  if (products.length === 0) {
    const product = isRecord(payload.product)
      ? { ...(payload.product as Record<string, unknown>) }
      : {};
    products = [product];
  }

  let updated = false;
  for (const product of products) {
    const id = firstText(product.id, product.productId);
    const externalCode = firstText(product.externalCode, product.ean);
    if ((link.productId && id && id === link.productId)
      || (link.externalCode && externalCode && externalCode === link.externalCode)
      || products.length === 1) {
      product.imagePath = imagePath;
      if (!firstText(product.name, product.productName)) {
        product.name = fallbackName || link.name || "Produto";
      }
      updated = true;
    }
  }

  if (!updated) {
    products[0].imagePath = imagePath;
    if (!firstText(products[0].name, products[0].productName)) {
      products[0].name = fallbackName || link.name || "Produto";
    }
  }

  payload.products = products;
  delete payload.product;
  return payload;
}

type CatalogProduct = {
  productId: string;
  itemId: string;
  externalCode: string;
  name: string;
  category: string;
  catalogContext: string;
  price: number;
  stockQuantity: number | null;
  isAvailable: boolean | null;
};

async function handleCatalogSync(req: Request) {
  const body = await readJson(req) as StoreContext & { connectionId?: string };
  let connection = await findConnection(body);
  connection = await ensureToken(connection);
  const merchantId = text(connection.merchant_id);
  if (!merchantId) {
    return json({ ok: false, message: "Loja iFood nao identificada no vinculo." }, 400);
  }

  const accessToken = connection.access_token ?? "";
  const warnings: string[] = [];
  const products = new Map<string, CatalogProduct>();
  const catalogPath = `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs`;
  const catalogs = await ifoodJson(catalogPath, accessToken)
    .then(catalogsFromResponse)
    .catch((error) => {
      warnings.push(`Catalogos: ${messageFromError(error)}`);
      return [] as Record<string, unknown>[];
    });

  for (const catalog of catalogs) {
    const catalogId = firstText(catalog.catalogId, catalog.id, catalog.groupId);
    const groupId = firstText(catalog.groupId, catalog.id, catalog.catalogId);
    if (!catalogId && !groupId) continue;

    if (groupId) {
      for (const path of [
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs/${encodeURIComponent(groupId)}/sellableItems`,
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs/${encodeURIComponent(groupId)}/unsellableItems`,
      ]) {
        await appendCatalogProductsFromPath(path, accessToken, products, warnings);
      }
    }

    if (catalogId) {
      await appendCatalogProductsFromPath(
        `/catalog/v2.0/merchants/${encodeURIComponent(merchantId)}/catalogs/${encodeURIComponent(catalogId)}/categories`,
        accessToken,
        products,
        warnings,
      );
    }
  }

  const items = [...products.values()]
    .filter((product) => product.name)
    .sort((a, b) => a.category.localeCompare(b.category) || a.name.localeCompare(b.name));

  return json({
    ok: true,
    message: items.length === 0
      ? warnings.length > 0
        ? "Nao consegui ler produtos do catalogo iFood agora."
        : "Nenhum produto encontrado no catalogo iFood."
      : `${items.length} produto(s) do iFood sincronizado(s) para o estoque.`,
    syncedAt: new Date().toISOString(),
    warnings,
    products: items,
  });
}

async function handleMerchantStatus(req: Request) {
  const body = await readJson(req) as StoreContext & { connectionId?: string };
  let connection = await findConnection(body);
  connection = await ensureToken(connection);

  const merchantId = text(connection.merchant_id);
  if (!merchantId) {
    return json({ ok: false, message: "Loja iFood nao vinculada a esta conexao." }, 400);
  }

  const accessToken = connection.access_token ?? "";
  const status = await ifoodJson(`/merchant/v1.0/merchants/${encodeURIComponent(merchantId)}/status`, accessToken);
  const diagnostics: Record<string, unknown> = {};

  try {
    diagnostics.interruptions = await ifoodJson(`/merchant/v1.0/merchants/${encodeURIComponent(merchantId)}/interruptions`, accessToken);
  } catch (error) {
    diagnostics.interruptionsWarning = messageFromError(error);
  }

  try {
    diagnostics.openingHours = await ifoodJson(`/merchant/v1.0/merchants/${encodeURIComponent(merchantId)}/opening-hours`, accessToken);
  } catch (error) {
    diagnostics.openingHoursWarning = messageFromError(error);
  }

  return json({
    ok: true,
    merchantId,
    merchantName: connection.merchant_name,
    status,
    ...diagnostics,
    syncedAt: new Date().toISOString(),
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

async function ensureToken(connection: ConnectionRow, forceRefresh = false): Promise<ConnectionRow> {
  if (!forceRefresh
    && connection.access_token
    && connection.token_expires_at
    && new Date(connection.token_expires_at).getTime() > Date.now() + 120000) {
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

async function appendCatalogProductsFromPath(
  path: string,
  accessToken: string,
  products: Map<string, CatalogProduct>,
  warnings: string[],
) {
  try {
    const data = await ifoodJson(path, accessToken);
    for (const product of extractCatalogProducts(data)) {
      upsertCatalogProduct(products, product);
    }
  } catch (error) {
    warnings.push(`${path}: ${messageFromError(error)}`);
  }
}

function catalogsFromResponse(data: unknown) {
  if (Array.isArray(data)) return data.filter(isRecord);
  if (isRecord(data)) {
    for (const key of ["catalogs", "items", "data", "results"]) {
      const value = data[key];
      if (Array.isArray(value)) return value.filter(isRecord);
    }
  }

  return [];
}

function extractCatalogProducts(data: unknown) {
  const result: CatalogProduct[] = [];
  const source = recordsFromAny(data);
  for (const item of source) {
    const categoryName = text(item.categoryName ?? item.name ?? item.title);
    const childItems = nestedCatalogItems(item);
    if (childItems.length > 0) {
      for (const child of childItems) {
        const product = normalizeCatalogProduct(child, categoryName);
        if (product.name) result.push(product);
      }
      continue;
    }

    const product = normalizeCatalogProduct(item, "");
    if (product.name) result.push(product);
  }

  return result;
}

function recordsFromAny(data: unknown) {
  if (Array.isArray(data)) return data.filter(isRecord);
  if (!isRecord(data)) return [];
  for (const key of ["items", "categories", "products", "data", "results", "content"]) {
    const value = data[key];
    if (Array.isArray(value)) return value.filter(isRecord);
  }
  return [data];
}

function nestedCatalogItems(category: Record<string, unknown>) {
  const result: Record<string, unknown>[] = [];
  for (const key of ["items", "itens", "products", "children"]) {
    const value = category[key];
    if (Array.isArray(value)) {
      result.push(...value.filter(isRecord));
    }
  }
  return result;
}

function numberFromCatalogArray(value: unknown) {
  if (!Array.isArray(value)) {
    return 0;
  }

  for (const row of value) {
    if (!isRecord(row)) continue;
    const amount = firstPositiveNumber(
      numberAtPath(row, ["price", "value"]),
      numberAtPath(row, ["itemPrice", "value"]),
      numberAtPath(row, ["unitPrice", "value"]),
      numberAt(row, ["value", "price", "amount"], 0),
    );
    if (amount > 0) {
      return amount;
    }
  }

  return 0;
}

function statusFromCatalogArray(value: unknown) {
  if (!Array.isArray(value)) {
    return "";
  }

  for (const row of value) {
    if (!isRecord(row)) continue;
    const status = firstText(row.status, row.itemStatus, row.value);
    if (status) {
      return status;
    }
  }

  return "";
}

function catalogContextFromArray(value: unknown) {
  if (!Array.isArray(value)) {
    return "";
  }

  for (const row of value) {
    if (!isRecord(row)) continue;
    const context = firstText(row.catalogContext, row.context, row.name);
    if (context) {
      return context;
    }
  }

  return "";
}

function normalizeCatalogProduct(item: Record<string, unknown>, categoryName: string): CatalogProduct {
  const itemNode = isRecord(item.item) ? item.item : {};
  const products = Array.isArray(item.products) ? item.products.filter(isRecord) : [];
  const productNode = isRecord(item.product) ? item.product : products[0] ?? {};
  const itemId = firstText(
    item.itemId,
    item.id,
    item.catalogItemId,
    item.contextItemId,
    itemNode.id,
  );
  const productId = firstText(
    item.productId,
    item.itemProductId,
    itemNode.productId,
    productNode.id,
    productNode.productId,
    itemId,
  );
  const externalCode = firstText(
    item.itemExternalCode,
    item.externalCode,
    item.ean,
    itemNode.externalCode,
    productNode.externalCode,
    productNode.ean,
  );
  const name = firstText(
    item.itemName,
    item.name,
    item.productName,
    item.description,
    productNode.name,
    productNode.productName,
    itemNode.name,
  ).toUpperCase();
  const category = firstText(
    item.categoryName,
    categoryName,
    item.category,
    item.groupName,
  ).toUpperCase();
  const catalogContext = firstText(
    item.catalogContext,
    item.context,
    catalogContextFromArray(item.priceByCatalog),
    catalogContextFromArray(item.statusByCatalog),
    catalogContextFromArray(productNode.priceByCatalog),
    catalogContextFromArray(productNode.statusByCatalog),
    catalogContextFromArray(itemNode.priceByCatalog),
    catalogContextFromArray(itemNode.statusByCatalog),
  );

  return {
    productId,
    itemId,
    externalCode,
    name,
    category,
    catalogContext,
    price: firstPositiveNumber(
      numberFromCatalogArray(item.priceByCatalog),
      numberFromCatalogArray(productNode.priceByCatalog),
      numberFromCatalogArray(itemNode.priceByCatalog),
      numberAtPath(item, ["itemPrice", "value"]),
      numberAtPath(item, ["price", "value"]),
      numberAtPath(item, ["unitPrice", "value"]),
      numberAt(item, ["itemPrice", "price", "unitPrice", "value"], 0),
      numberAtPath(itemNode, ["price", "value"]),
    ),
    stockQuantity: firstNullableNumber(
      nullableNumberAt(item, ["itemQuantity", "stockQuantity", "quantity", "stock", "inventory", "amount"]),
      nullableNumberAt(productNode, ["quantity", "stock", "inventory", "amount"]),
      nullableNumberAt(itemNode, ["quantity", "stock", "inventory", "amount"]),
    ),
    isAvailable: availabilityFromStatus(firstText(
      statusFromCatalogArray(item.statusByCatalog),
      statusFromCatalogArray(productNode.statusByCatalog),
      statusFromCatalogArray(itemNode.statusByCatalog),
      item.status,
      item.itemStatus,
      itemNode.status,
      productNode.status,
      item.available,
      item.isAvailable,
    )),
  };
}

function upsertCatalogProduct(products: Map<string, CatalogProduct>, incoming: CatalogProduct) {
  const key = incoming.productId || incoming.itemId || incoming.externalCode || `${incoming.category}|${incoming.name}`;
  if (!key) return;

  const current = products.get(key);
  if (!current) {
    products.set(key, incoming);
    return;
  }

  products.set(key, {
    productId: current.productId || incoming.productId,
    itemId: current.itemId || incoming.itemId,
    externalCode: current.externalCode || incoming.externalCode,
    name: current.name || incoming.name,
    category: current.category || incoming.category,
    catalogContext: current.catalogContext || incoming.catalogContext,
    price: current.price > 0 ? current.price : incoming.price,
    stockQuantity: current.stockQuantity ?? incoming.stockQuantity,
    isAvailable: current.isAvailable ?? incoming.isAvailable,
  });
}

function firstText(...values: unknown[]) {
  for (const value of values) {
    const content = text(value);
    if (content) return content;
  }
  return "";
}

function firstPositiveNumber(...values: number[]) {
  return values.find((value) => Number.isFinite(value) && value > 0) ?? 0;
}

function firstNullableNumber(...values: Array<number | null>) {
  return values.find((value) => value !== null && Number.isFinite(value)) ?? null;
}

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function nullableNumberAt(source: Record<string, unknown>, keys: string[]) {
  const value = valueAt(source, keys);
  if (value === undefined || value === null || value === "") return null;
  const number = typeof value === "number" ? value : Number(String(value).replace(",", "."));
  return Number.isFinite(number) ? number : null;
}

function availabilityFromStatus(value: unknown): boolean | null {
  if (typeof value === "boolean") return value;
  const status = text(value).toUpperCase();
  if (!status) return null;
  if (["AVAILABLE", "ATIVO", "ACTIVE", "TRUE", "DISPONIVEL"].includes(status)) return true;
  if (["UNAVAILABLE", "PAUSED", "INACTIVE", "FALSE", "INDISPONIVEL", "PAUSADO"].includes(status)) return false;
  return null;
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

  const paths = [
    "/events/v1.0/events:polling?category=FOOD",
    "/events/v1.0/events:polling?category=FOOD&limit=50",
    "/events/v1.0/events:polling?limit=50",
    "/events/v1.0/events:polling",
    "/events/v1.0/events:polling?category=FOOD&groups=ORDER_STATUS",
    "/events/v1.0/events:polling?groups=ORDER_STATUS&limit=50",
    "/order/v1.0/orders:polling?limit=50",
  ];
  let lastError = "";

  for (const path of paths) {
    try {
      const response = await fetch(`${IFOOD_API_BASE}${path}`, { headers });
      const events = await eventsFromPollingResponse(response);
      return events;
    } catch (error) {
      lastError = `${path}: ${messageFromError(error)}`;
      console.error(`iFood polling failed ${path}`, lastError);
    }
  }

  throw new Error(lastError || "iFood nao retornou eventos agora.");
}

async function pollEventsWithFreshToken(connection: ConnectionRow) {
  try {
    return await pollEvents(connection);
  } catch (error) {
    const message = messageFromError(error);
    if (!/bad request|forbidden|unauthorized|token|permission|permiss/i.test(message)) {
      throw error;
    }

    const refreshed = await ensureToken(connection, true);
    return await pollEvents(refreshed);
  }
}

async function eventsFromPollingResponse(response: Response) {
    if (response.status === 204) return [];
    const data = await parseIfoodResponse(response);
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.events)) return data.events;
    return [];
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

async function ifoodPut(path: string, accessToken: string, payload: unknown) {
  const response = await fetch(`${IFOOD_API_BASE}${path}`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });
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
  const phoneWithLocalizer = phoneObject?.localizer
    ? `${phone} cod. ${text(phoneObject.localizer)}`.trim()
    : phone;
  const deliveredBy = text(delivery?.deliveredBy ?? order.deliveredBy).toUpperCase();
  const pickupCode = text(delivery?.pickupCode ?? order.pickupCode);
  const deliveryLocalizer = text(phoneObject?.localizer ?? delivery?.deliveryCode ?? order.deliveryCode);
  const scheduled = valueAt(order, ["scheduled", "scheduling"]) as Record<string, unknown> | undefined;
  const takeout = valueAt(order, ["takeout"]) as Record<string, unknown> | undefined;
  const preparation = valueAt(order, ["preparation"]) as Record<string, unknown> | undefined;
  const createdAt = text(order.createdAt ?? order.created_at ?? order.createdDate);
  const orderTiming = scheduled
    ? "SCHEDULED"
    : text(order.orderTiming ?? order.timing ?? order.type).toUpperCase();
  const preparationStartDateTime = text(
    scheduled?.preparationStartDateTime ??
    scheduled?.preparation_start_date_time ??
    scheduled?.preparationStart ??
    scheduled?.deliveryDateTimeStart ??
    scheduled?.startDateTime ??
    scheduled?.scheduledDateTime ??
    preparation?.start ??
    preparation?.Start ??
    preparation?.startDateTime ??
    preparation?.preparationStartDateTime ??
    order.preparationStartDateTime ??
    order.preparationStart,
  );
  const confirmationBase = orderTiming === "SCHEDULED" && preparationStartDateTime
    ? preparationStartDateTime
    : createdAt;
  const confirmationDeadlineAt = addMinutesIso(confirmationBase, 8);
  const deliveryExpectedAt = text(
    scheduled?.deliveryDateTimeStart ??
    scheduled?.scheduledDateTime ??
    scheduled?.deliveryDateTime ??
    scheduled?.startDateTime ??
    scheduled?.deliveryDateTimeEnd ??
    scheduled?.endDateTime ??
    delivery?.estimatedDeliveryDateTime ??
    delivery?.estimatedDeliveryTime ??
    delivery?.deliveryEstimateDateTime ??
    delivery?.deliveryEstimate ??
    delivery?.deliveryDateTimeStart ??
    delivery?.deliveryDateTime ??
    order.estimatedDeliveryDateTime ??
    order.estimatedDeliveryTime ??
    order.deliveryDateTimeStart,
  );
  const deliveredAt = text(
    delivery?.deliveredAt ??
    delivery?.deliveredDateTime ??
    order.deliveredAt ??
    order.deliveredDateTime ??
    order.concludedAt,
  );
  const collectedAt = text(
    delivery?.collectedAt ??
    delivery?.pickupAt ??
    delivery?.pickupDateTime ??
    order.collectedAt,
  );
  const shipmentInfo = [
    deliveredBy ? deliveredBy === "IFOOD" ? "Entrega iFood" : "Entrega propria" : "",
    pickupCode ? `Codigo coleta ${pickupCode}` : "",
    deliveryLocalizer ? `Localizador ${deliveryLocalizer}` : "",
  ].filter(Boolean).join(" | ");
  const payment = paymentDetails(order);
  const voucherSummary = voucherDetails(order);
  const cancellationInfo = cancellationDetails(order);
  const items = Array.isArray(order.items) ? order.items as Record<string, unknown>[] : [];
  const mappedItems = items.map((item, index) => {
    const quantity = numberAt(item, ["quantity"], 1);
    const total = firstNumber(
      numberAtPath(item, ["totalPrice", "value"]),
      numberAt(item, ["totalPrice"], 0),
      numberAtPath(item, ["price", "value"]),
      numberAt(item, ["price"], 0),
      numberAtPath(item, ["unitPrice", "value"]) * quantity,
      numberAt(item, ["unitPrice"], 0) * quantity,
    );
    const unitPrice = firstNumber(
      numberAtPath(item, ["unitPrice", "value"]),
      numberAt(item, ["unitPrice"], 0),
      quantity > 0 ? total / quantity : total,
    );
    return {
      code: text(item.externalCode ?? item.ean ?? String(index + 1).padStart(6, "0")),
      productId: text(item.productId ?? item.catalogItemId ?? item.id),
      name: text(item.name ?? "ITEM IFOOD").toUpperCase(),
      quantity: Math.max(1, Math.round(quantity)),
      unitPrice,
      notes: text(item.observations ?? item.observation ?? item.note),
    };
  });

  const total = firstNumber(
    numberAtPath(order, ["total", "orderAmount", "value"]),
    numberAtPath(order, ["total", "subTotal", "value"]),
    numberAtPath(order, ["total", "totalPrice", "value"]),
    numberAt(recordAt(order, "total"), ["orderAmount", "subTotal", "totalPrice"], 0),
    numberAt(order, ["totalPrice", "orderAmount"], 0),
    mappedItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0),
  );
  const street = text(deliveryAddress?.streetName ?? deliveryAddress?.street);
  const number = text(deliveryAddress?.streetNumber ?? deliveryAddress?.number);
  const complement = text(deliveryAddress?.complement);
  const district = text(deliveryAddress?.neighborhood);
  const city = text(deliveryAddress?.city);

  return {
    orderId: text(order.id ?? fallbackOrderId),
    displayId: text(order.displayId ?? order.shortReference ?? fallbackOrderId.slice(-6)),
    status: text(order.balcaoLivreStatus ?? order.status ?? order.orderStatus),
    createdAt,
    orderTiming,
    preparationStartDateTime,
    confirmationDeadlineAt,
    deliveryExpectedAt,
    deliveredAt,
    collectedAt,
    orderType: takeout ? "TAKEOUT" : text(order.orderType ?? order.type ?? "DELIVERY").toUpperCase(),
    deliveredBy,
    pickupCode,
    deliveryLocalizer,
    shipmentInfo,
    paymentMethod: payment.method,
    paymentSummary: payment.summary,
    changeFor: payment.changeFor,
    voucherSummary,
    cancellationInfo,
    customerName: text(customer?.name ?? "CLIENTE IFOOD"),
    customerDocument: text(
      customer?.documentNumber ??
      customer?.document ??
      customer?.cpf ??
      customer?.cnpj ??
      customer?.taxPayerIdentificationNumber ??
      customer?.taxpayerIdentificationNumber,
    ),
    phone: phoneWithLocalizer,
    address: text(deliveryAddress?.formattedAddress) || [street, number, complement, city].filter(Boolean).join(", "),
    district,
    notes: [text(order.observations ?? order.observation ?? order.note), takeout ? `Retirada: ${text(takeout.mode)}` : ""].filter(Boolean).join("\n"),
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

function paymentDetails(order: Record<string, unknown>) {
  const methods = paymentMethodObjects(order);
  const parts: string[] = [];
  let method = "";
  let changeFor = numberAtPath(order, ["cash", "changeFor", "value"]) ||
    numberAtPath(order, ["cash", "changeFor"]) ||
    numberAtPath(order, ["payment", "cash", "changeFor", "value"]) ||
    numberAtPath(order, ["payments", "cash", "changeFor", "value"]) ||
    numberAtPath(order, ["payment", "changeFor", "value"]) ||
    numberAtPath(order, ["payments", "changeFor", "value"]) ||
    numberAtPath(order, ["cash", "change", "value"]) ||
    numberAtPath(order, ["cash", "change"]) ||
    numberAtPath(order, ["payment", "cash", "change", "value"]) ||
    numberAtPath(order, ["payments", "cash", "change", "value"]) ||
    numberAtPath(order, ["payment", "change", "value"]) ||
    numberAtPath(order, ["payments", "change", "value"]);

  for (const item of methods) {
    const label = paymentLabel(item);
    if (!label) continue;
    method ||= label;
    const amount = numberAt(item, ["value", "amount"], 0) || numberAtPath(item, ["price", "value"]);
    changeFor ||= numberAtPath(item, ["cash", "changeFor", "value"]) ||
      numberAtPath(item, ["changeFor", "value"]) ||
      numberAt(item, ["changeFor"], 0) ||
      numberAtPath(item, ["cash", "change", "value"]) ||
      numberAtPath(item, ["change", "value"]) ||
      numberAt(item, ["change"], 0);
    const descriptor = [
      label,
      amount > 0 ? moneyText(amount) : "",
      isPaymentOnDelivery(item) ? "na entrega" : isPrepaidPayment(item) ? "online" : "",
    ].filter(Boolean).join(" ");
    if (descriptor) parts.push(descriptor);
  }

  let summary = [...new Set(parts)].join(" / ");
  if (changeFor > 0) {
    summary = summary ? `${summary} | Troco para ${moneyText(changeFor)}` : `Troco para ${moneyText(changeFor)}`;
  }

  return { method, summary, changeFor };
}

function paymentLabel(item: Record<string, unknown>) {
  const methodObject = isRecord(item.method) ? item.method : {};
  const paymentMethodObject = isRecord(item.paymentMethod) ? item.paymentMethod : {};
  const cardObject = isRecord(item.card) ? item.card : {};
  const creditObject = isRecord(item.credit) ? item.credit : {};
  const debitObject = isRecord(item.debit) ? item.debit : {};
  const method = normalizePaymentLabel(text(
    methodObject.method ??
    methodObject.name ??
    paymentMethodObject.method ??
    paymentMethodObject.name ??
    (!isRecord(item.method) ? item.method : undefined) ??
    item.name ??
    item.type,
  ));
  const brand = normalizeCardBrand(text(
    item.brand ??
    item.cardBrand ??
    item.card_brand ??
    item.issuer ??
    cardObject.brand ??
    cardObject.cardBrand ??
    creditObject.brand ??
    debitObject.brand ??
    paymentMethodObject.brand ??
    paymentMethodObject.cardBrand,
  ));

  if (!method && brand) return `CARTAO - Bandeira ${brand}`;
  if (brand && /CREDITO|DEBITO|CARTAO/i.test(method) && !method.toUpperCase().includes(brand.toUpperCase())) {
    return `${method} - Bandeira ${brand}`;
  }
  return method;
}

function paymentMethodObjects(order: Record<string, unknown>) {
  const result: Record<string, unknown>[] = [];
  for (const source of [order.payments, order.payment]) {
    if (Array.isArray(source)) {
      result.push(...source.filter(isRecord));
      continue;
    }

    if (!isRecord(source)) continue;
    for (const key of ["methods", "paymentMethods", "items"]) {
      const value = source[key];
      if (Array.isArray(value)) {
        result.push(...value.filter(isRecord));
      }
    }

    if (result.length === 0) {
      result.push(source);
    }
  }
  return result;
}

function normalizePaymentLabel(value: string) {
  const normalized = value.toUpperCase();
  if (["CASH", "MONEY", "DINHEIRO"].includes(normalized)) return "DINHEIRO";
  if (["CREDIT", "CREDIT_CARD", "CARTAO_CREDITO"].includes(normalized)) return "CARTAO CREDITO";
  if (["DEBIT", "DEBIT_CARD", "CARTAO_DEBITO"].includes(normalized)) return "CARTAO DEBITO";
  if (["MEAL_VOUCHER", "VOUCHER"].includes(normalized)) return "VOUCHER";
  return normalized.replaceAll("_", " ");
}

function normalizeCardBrand(value: string) {
  const normalized = value.trim().toUpperCase().replaceAll("_", " ");
  if (normalized === "VISA") return "Visa";
  if (["MASTERCARD", "MASTER CARD"].includes(normalized)) return "Mastercard";
  if (normalized === "ELO") return "Elo";
  if (["AMEX", "AMERICAN EXPRESS"].includes(normalized)) return "American Express";
  if (normalized === "HIPERCARD") return "Hipercard";
  return normalized
    ? normalized.toLowerCase().replace(/\b\w/g, (letter) => letter.toUpperCase())
    : "";
}

function isPaymentOnDelivery(item: Record<string, unknown>) {
  const joined = text(item.paymentType ?? item.prepaid ?? item.liability ?? item.type).toUpperCase();
  return joined.includes("OFFLINE") || joined.includes("ON_DELIVERY") || joined.includes("NA ENTREGA") || joined.includes("DELIVERY");
}

function isPrepaidPayment(item: Record<string, unknown>) {
  const joined = text(item.paymentType ?? item.prepaid ?? item.liability ?? item.type).toUpperCase();
  return joined.includes("PREPAID") || joined.includes("ONLINE");
}

function voucherDetails(order: Record<string, unknown>) {
  const values = new Set<string>();
  collectVoucherValues(order, values);
  return [...values].filter(Boolean).slice(0, 6).join(" / ");
}

function collectVoucherValues(value: unknown, values: Set<string>) {
  if (Array.isArray(value)) {
    for (const item of value) collectVoucherValues(item, values);
    return;
  }

  if (!isRecord(value)) {
    const simple = text(value);
    if (simple.toUpperCase().includes("VOUCHER") || simple.toUpperCase().includes("ENTGRATIS")) {
      values.add(simple);
    }
    return;
  }

  const code = text(value.code ?? value.voucherCode ?? value.couponCode ?? value.promoCode ?? value.campaignCode ?? value.target);
  const name = text(value.name ?? value.description ?? value.title);
  const amount = numberAt(value, ["value", "amount"], 0) || numberAtPath(value, ["amount", "value"]);
  const subsidy = subsidySummary(value);
  const joined = `${code} ${name}`.trim();
  const raw = JSON.stringify(value);
  if (joined.toUpperCase().includes("VOUCHER") ||
    joined.toUpperCase().includes("ENTGRATIS") ||
    (amount > 0 && /benefit|discount|voucher|coupon|promotion/i.test(raw))) {
    values.add([
      [code, name].filter(Boolean).join(" "),
      amount > 0 ? `Desconto ${moneyText(amount)}` : "",
      subsidy,
    ].filter(Boolean).join(" | "));
  }

  for (const [key, child] of Object.entries(value)) {
    if (/benefit|discount|voucher|coupon|promotion/i.test(key)) {
      collectVoucherValues(child, values);
    }
  }
}

function subsidySummary(value: Record<string, unknown>) {
  const parts = new Set<string>();
  collectSubsidyValues(value, parts);
  const list = [...parts].filter(Boolean).slice(0, 4);
  return list.length ? `Subsidio: ${list.join(", ")}` : "";
}

function collectSubsidyValues(value: unknown, parts: Set<string>) {
  if (Array.isArray(value)) {
    for (const item of value) collectSubsidyValues(item, parts);
    return;
  }

  if (!isRecord(value)) return;
  const sponsor = text(value.sponsor ?? value.sponsoredBy ?? value.provider ?? value.owner ?? value.name ?? value.description);
  const amount = numberAt(value, ["sponsorshipValue", "subsidy", "value", "amount"], 0) ||
    numberAtPath(value, ["sponsorshipValue", "value"]) ||
    numberAtPath(value, ["subsidy", "value"]) ||
    numberAtPath(value, ["amount", "value"]);
  if (sponsor && amount > 0 && /sponsor|subsid|liability/i.test(JSON.stringify(value))) {
    parts.add(`${normalizeSponsor(sponsor)} ${moneyText(amount)}`);
  }

  for (const [key, child] of Object.entries(value)) {
    if (/sponsor|subsid|liability/i.test(key)) {
      collectSubsidyValues(child, parts);
    }
  }
}

function normalizeSponsor(value: string) {
  const normalized = value.trim().toUpperCase();
  if (normalized === "IFOOD") return "iFood";
  if (["MERCHANT", "RESTAURANT"].includes(normalized)) return "Loja";
  return value.trim();
}

function cancellationDetails(order: Record<string, unknown>) {
  const cancellation = order.cancellation;
  if (isRecord(cancellation)) {
    return [cancellation.requestedBy, cancellation.reason, cancellation.cancellationCode, cancellation.message]
      .map(text)
      .filter(Boolean)
      .join(" - ");
  }

  return text(order.cancellationReason ?? order.cancelReason ?? order.cancellationMessage);
}

function numberAtPath(source: unknown, keys: string[]) {
  let current: unknown = source;
  for (const key of keys) {
    if (!isRecord(current)) return 0;
    current = current[key];
  }
  const number = typeof current === "number" ? current : Number(String(current ?? "").replace(",", "."));
  return Number.isFinite(number) ? number : 0;
}

function recordAt(source: Record<string, unknown>, key: string): Record<string, unknown> {
  const value = source[key];
  return isRecord(value) ? value : {};
}

function firstNumber(...values: number[]) {
  return values.find((value) => Number.isFinite(value) && value > 0) ?? 0;
}

function moneyText(value: number) {
  return value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
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
        status: "PREPARANDO",
        message: "Pedido iFood confirmado e em preparo.",
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
