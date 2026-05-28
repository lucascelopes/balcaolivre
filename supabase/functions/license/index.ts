import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const PUBLIC_MENU_BASE_URL = "https://cardapio.balcaolivrepdv.com.br";
const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";

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
  waitMinMinutes?: number;
  waitMaxMinutes?: number;
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

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/activate" || route === "/api/app/activate") {
      return activate(req);
    }

    if (route === "/checkin" || route === "/api/app/checkin") {
      return checkIn(req);
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

    return json({ ok: false, message: "Rota de licenca nao encontrada." }, 404);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function activate(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const result = await ensureLicense(payload, { bindMachine: true, eventType: "activation" });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  return json({
    ok: true,
    message: "Chave ativada pelo Supabase.",
    plan: result.license.plan,
    expiresAt: result.license.expires_at,
  });
}

async function checkIn(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const result = await ensureLicense(payload, { bindMachine: false, eventType: payload.eventName || "checkin" });
  if (!result.ok) {
    return json({ ok: false, message: result.message }, result.status ?? 401);
  }

  return json({ ok: true, message: "Licenca sincronizada no Supabase.", mode: "supabase" });
}

async function publishMenu(req: Request) {
  const payload = normalizePayloadKeys(await readJson<PublicMenuPayload>(req));
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
    wait_min_minutes: waitMin,
    wait_max_minutes: Math.max(waitMin, Math.round(numberValue(payload.waitMaxMinutes) || 60)),
    discount_enabled: payload.discountEnabled !== false,
    discount_code: (stringValue(payload.discountCode) || "EXCLUSIVO4").toUpperCase(),
    discount_amount: Math.max(0, numberValue(payload.discountAmount) || 0),
    discount_description: stringValue(payload.discountDescription) || "Use no atendimento para ganhar desconto no pedido.",
    loyalty_enabled: payload.loyaltyEnabled !== false,
    loyalty_goal: Math.max(1, Math.round(numberValue(payload.loyaltyGoal) || 20)),
    loyalty_minimum_order: Math.max(0, numberValue(payload.loyaltyMinimumOrder) || 20),
    is_published: true,
    updated_at: new Date().toISOString(),
  };

  let menuId = stringValue(existing.data?.id);
  let slug = stringValue(existing.data?.slug);

  if (menuId) {
    const { error } = await supabase
      .from("bv_public_menus")
      .update({ ...menuPayload, slug: slug || baseSlug })
      .eq("id", menuId);
    if (error) {
      return json(failMenu(`Supabase recusou atualizar cardapio: ${error.message}`), 500);
    }
  } else {
    for (const candidate of slugCandidates(baseSlug)) {
      const inserted = await supabase
        .from("bv_public_menus")
        .insert({ ...menuPayload, slug: candidate })
        .select("id, slug")
        .single();

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
  const payload = normalizePayloadKeys(await readJson<ClientPayload & { limit?: number }>(req));
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
  const payload = normalizePayloadKeys(await readJson<PublicMenuOrderAckPayload>(req));
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

  if (validation.expiresAt.getTime() <= Date.now()) {
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
    expires_at: validation.expiresAt.toISOString(),
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

function normalizeClientKind(value: unknown) {
  const clean = stringValue(value).toLowerCase();
  return clean || "windows";
}

function isMultiDeviceClient(payload: ClientPayload) {
  const kind = normalizeClientKind(payload.clientKind);
  const code = stringValue(payload.machineCode).toUpperCase();
  return ["android", "web", "browser"].includes(kind) || code.startsWith("AND-") || code.startsWith("WEB-");
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

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}
