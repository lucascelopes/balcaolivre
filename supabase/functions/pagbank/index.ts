import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type ClientPayload = {
  licenseKey?: string;
  machineHash?: string;
  machineCode?: string;
  appVersion?: string;
  profile?: Record<string, unknown>;
};

type TerminalPayload = ClientPayload & {
  terminalId?: string;
  terminalLabel?: string;
  comPort?: string;
};

type ChargePayload = ClientPayload & {
  amount?: number | string;
  method?: string;
  localReference?: string;
  description?: string;
  items?: ChargeItemPayload[];
  payerName?: string;
  payerEmail?: string;
  payerTaxId?: string;
  payerPhone?: string;
};

type StatusPayload = ClientPayload & {
  attemptId?: string;
  orderId?: string;
  localReference?: string;
};

type ChargeItemPayload = {
  code?: string;
  id?: string;
  title?: string;
  name?: string;
  quantity?: number | string;
  unitPrice?: number | string;
  unit_price?: number | string;
  price?: number | string;
  description?: string;
};

type PagBankItem = {
  reference_id: string;
  name: string;
  quantity: number;
  unit_amount: number;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });

  try {
    const route = routeFromPath(new URL(req.url).pathname);
    if (route === "/health" && req.method === "GET") return json({ ok: true, app: "Balcao Livre PagBank" });
    if (route === "/oauth/callback" && req.method === "GET") return oauthCallback(req);
    if (route === "/webhook" && req.method === "POST") return webhook(req);
    if (req.method !== "POST") return json({ ok: false, message: "Metodo nao permitido." }, 405);

    if (route === "/connect/start") return startConnect(req);
    if (route === "/status") return status(req);
    if (route === "/terminal/select") return selectTerminal(req);
    if (route === "/web/charge") return createWebCharge(req);
    if (route === "/web/status") return webStatus(req);

    return json({ ok: false, message: "Rota PagBank nao encontrada." }, 404);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function startConnect(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const clientId = pagBankClientId();
  if (!clientId) return json({ ok: false, message: "Ativacao PagBank pendente. Configure a aplicacao PagBank da Balcao Livre." }, 500);

  const state = crypto.randomUUID();
  const saved = await serviceClient().from("bv_pagbank_oauth_states").insert({
    state,
    license_key: normalizeLicense(payload.licenseKey),
    machine_hash: stringValue(payload.machineHash),
    expires_at: new Date(Date.now() + 10 * 60_000).toISOString(),
  });
  if (saved.error) return json({ ok: false, message: `Supabase recusou conexao PagBank: ${saved.error.message}` }, 500);

  const auth = new URL(`${pagBankConnectBaseUrl()}/oauth2/authorize`);
  auth.searchParams.set("client_id", clientId);
  auth.searchParams.set("response_type", "code");
  auth.searchParams.set("redirect_uri", pagBankRedirectUri());
  auth.searchParams.set("scope", "payments.read payments.create checkout.create checkout.view accounts.read");
  auth.searchParams.set("state", state);
  return json({ ok: true, authUrl: auth.toString(), expiresAt: new Date(Date.now() + 10 * 60_000).toISOString() });
}

async function oauthCallback(req: Request) {
  const url = new URL(req.url);
  const code = stringValue(url.searchParams.get("code"));
  const state = stringValue(url.searchParams.get("state"));
  if (!code || !state) return html("PagBank", "Conexao cancelada ou incompleta.", false);

  const supabase = serviceClient();
  const stateResult = await supabase.from("bv_pagbank_oauth_states").select("*").eq("state", state).maybeSingle();
  if (stateResult.error || !stateResult.data) return html("PagBank", "Conexao expirada. Volte ao PDV e tente de novo.", false);

  const stateRow = stateResult.data as Record<string, unknown>;
  if (stringValue(stateRow.used_at) || new Date(stringValue(stateRow.expires_at)).getTime() < Date.now()) {
    return html("PagBank", "Conexao expirada. Volte ao PDV e tente de novo.", false);
  }

  try {
    const token = await exchangePagBankToken({
      grant_type: "authorization_code",
      code,
      redirect_uri: pagBankRedirectUri(),
    });
    const now = new Date().toISOString();
    const saved = await supabase.from("bv_pagbank_connections").upsert({
      license_key: stringValue(stateRow.license_key),
      machine_hash: stringValue(stateRow.machine_hash),
      status: "CONNECTED",
      account_id: stringValue(token.account_id || token.accountId || token.user_id),
      access_token: stringValue(token.access_token),
      refresh_token: stringValue(token.refresh_token),
      token_type: stringValue(token.token_type),
      scope: stringValue(token.scope),
      expires_at: tokenExpiresAt(token),
      connected_at: now,
      last_sync_at: now,
      last_error: "",
      updated_at: now,
    }, { onConflict: "license_key" });
    if (saved.error) return html("PagBank", `Supabase recusou salvar conexao: ${saved.error.message}`, false);

    await supabase.from("bv_pagbank_oauth_states").update({ used_at: now }).eq("state", state);
    return html("PagBank conectado", "Conta conectada. Volte ao Balcao Livre PDV; a tela atualiza sozinha.", true);
  } catch (error) {
    return html("PagBank", messageFromError(error), false);
  }
}

async function status(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const connection = await getConnection(normalizeLicense(payload.licenseKey));
  return json({
    ok: true,
    connected: !!connection?.access_token,
    status: stringValue(connection?.status) || "DISCONNECTED",
    accountId: stringValue(connection?.account_id),
    selectedTerminalId: stringValue(connection?.selected_terminal_id),
    selectedTerminalLabel: stringValue(connection?.selected_terminal_label),
    comPort: stringValue(connection?.plugpag_com_port),
    lastSyncAt: stringValue(connection?.last_sync_at),
    lastError: stringValue(connection?.last_error),
  });
}

async function selectTerminal(req: Request) {
  const payload = normalizePayloadKeys(await readJson<TerminalPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const comPort = stringValue(payload.comPort).toUpperCase().replace(/\s+/g, "");
  if (!comPort) return json({ ok: false, message: "Informe a porta COM da Moderninha." }, 400);

  const saved = await serviceClient()
    .from("bv_pagbank_connections")
    .update({
      selected_terminal_id: stringValue(payload.terminalId) || "PLUGPAG",
      selected_terminal_label: stringValue(payload.terminalLabel) || "Moderninha PlugPag",
      plugpag_com_port: comPort,
      last_sync_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    })
    .eq("license_key", normalizeLicense(payload.licenseKey))
    .select("license_key")
    .maybeSingle();
  if (saved.error) return json({ ok: false, message: `Supabase recusou maquininha PagBank: ${saved.error.message}` }, 500);
  if (!saved.data) return json({ ok: false, message: "Conecte o PagBank antes de salvar a Moderninha." }, 404);
  return json({ ok: true, message: "Moderninha PlugPag salva." });
}

async function createWebCharge(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ChargePayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const amount = roundMoney(payload.amount);
  if (amount <= 0) return json({ ok: false, message: "Valor invalido para cobranca." }, 400);

  const token = await ensureAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const method = stringValue(payload.method).toUpperCase();
  const localReference = sanitizeReference(payload.localReference || crypto.randomUUID());
  const description = clipText(stringValue(payload.description) || "Balcao Livre PDV", 120);
  const items = buildItems(payload.items, amount, description, localReference);
  const expiresAt = new Date(Date.now() + 30 * 60_000).toISOString();

  if (method === "PIX") {
    const order = await pagBankFetch(token.accessToken, "/orders", {
      method: "POST",
      headers: { "x-idempotency-key": crypto.randomUUID() },
      body: JSON.stringify(compactObject({
        reference_id: localReference,
        customer: buildCustomer(payload, localReference),
        items,
        qr_codes: [{ amount: { value: moneyCents(amount) } }],
        notification_urls: [pagBankWebhookUrl()].filter(Boolean),
      })),
    });
    const qrCode = firstRecord(order.qr_codes);
    const status = normalizeStatus(order);
    const saved = await saveAttempt({
      licenseKey,
      machineHash: stringValue(payload.machineHash),
      localReference,
      method: "PIX_QR",
      amount,
      orderId: stringValue(order.id),
      paymentId: stringValue(qrCode.id),
      status,
      statusDetail: stringValue(order.status_detail),
      rawResponse: order,
    });
    if (!saved.ok) return json(saved, saved.status);

    const paymentUrl = findLink([qrCode, order], ["PAY", "CHECKOUT", "QRCODE.PNG", "QR_CODE", "QRCODE"]);
    return json({
      ok: true,
      message: "Pix PagBank gerado.",
      attemptId: saved.attemptId,
      localReference,
      orderId: stringValue(order.id),
      paymentId: stringValue(qrCode.id),
      status,
      qrCode: stringValue(qrCode.text || qrCode.qr_code || qrCode.emv || qrCode.payload),
      qrCodeBase64: "",
      ticketUrl: paymentUrl,
      paymentUrl,
      expiresAt,
    });
  }

  const type = method === "DEBITO" ? "DEBIT_CARD" : "CREDIT_CARD";
  const checkout = await pagBankFetch(token.accessToken, "/checkouts", {
    method: "POST",
    headers: { "x-idempotency-key": crypto.randomUUID() },
    body: JSON.stringify(compactObject({
      reference_id: localReference,
      expiration_date: expiresAt,
      customer_modifiable: true,
      items,
      payment_methods: [{ type }],
      payment_methods_configs: [{ type, config_options: [{ option: "INSTALLMENTS_LIMIT", value: "1" }] }],
      soft_descriptor: clipText(description.replace(/[^A-Za-z0-9 ]+/g, " "), 17),
      redirect_url: stringValue(Deno.env.get("PAYMENTS_RETURN_URL")) || "https://www.balcaolivrepdv.com.br",
      return_url: stringValue(Deno.env.get("PAYMENTS_RETURN_URL")) || "https://www.balcaolivrepdv.com.br",
      notification_urls: [pagBankWebhookUrl()].filter(Boolean),
      payment_notification_urls: [pagBankWebhookUrl()].filter(Boolean),
    })),
  });
  const saved = await saveAttempt({
    licenseKey,
    machineHash: stringValue(payload.machineHash),
    localReference,
    method: method === "DEBITO" ? "CHECKOUT_DEBITO" : "CHECKOUT_CREDITO",
    amount,
    orderId: stringValue(checkout.id),
    paymentId: "",
    status: normalizeStatus(checkout),
    statusDetail: stringValue(checkout.status_detail),
    rawResponse: checkout,
  });
  if (!saved.ok) return json(saved, saved.status);

  return json({
    ok: true,
    message: "Link de pagamento PagBank gerado.",
    attemptId: saved.attemptId,
    localReference,
    orderId: stringValue(checkout.id),
    status: normalizeStatus(checkout),
    paymentUrl: findLink([checkout], ["PAY", "CHECKOUT", "REDIRECT", "SELF"]),
    expiresAt,
  });
}

async function webStatus(req: Request) {
  const payload = normalizePayloadKeys(await readJson<StatusPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const current = await findAttempt(licenseKey, payload);
  if (!current.ok) return json(current, current.status);

  const token = await ensureAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const row = current.row;
  const orderId = stringValue(row.order_id || payload.orderId);
  const method = stringValue(row.method).toUpperCase();
  const path = method.includes("CHECKOUT") ? `/checkouts/${encodeURIComponent(orderId)}` : `/orders/${encodeURIComponent(orderId)}`;
  const remote = orderId ? await pagBankFetch(token.accessToken, path) : row.raw_response as Record<string, unknown>;
  const charge = firstRecord(remote.charges);
  const remoteStatus = normalizeStatus(remote);
  const paymentId = stringValue(charge.id || row.payment_id);

  await serviceClient()
    .from("bv_pagbank_payment_attempts")
    .update({
      status: remoteStatus,
      status_detail: stringValue(remote.status_detail || charge.status_detail),
      payment_id: paymentId,
      raw_response: remote,
      updated_at: new Date().toISOString(),
    })
    .eq("id", stringValue(row.id));

  return json({
    ok: true,
    attemptId: stringValue(row.id),
    orderId,
    paymentId,
    status: remoteStatus,
    statusDetail: stringValue(remote.status_detail || charge.status_detail),
    paid: isPaid(remoteStatus, remote),
  });
}

async function webhook(req: Request) {
  const url = new URL(req.url);
  const payload = await readJson<Record<string, unknown>>(req);
  const orderId = extractWebhookId(payload, url, ["order", "orders", "checkout", "checkouts"]);
  const paymentId = extractWebhookId(payload, url, ["charge", "charges", "payment", "payments"]);
  const localReference = stringValue(payload.reference_id || payload.referenceId || payload.external_reference || url.searchParams.get("reference_id"));
  const remoteStatus = normalizeStatus(payload);

  const patch: Record<string, unknown> = {
    status: remoteStatus,
    status_detail: stringValue(payload.status_detail),
    raw_response: payload,
    updated_at: new Date().toISOString(),
  };
  if (paymentId) patch.payment_id = paymentId;

  let query = serviceClient().from("bv_pagbank_payment_attempts").update(patch).select("id");
  if (localReference) query = query.eq("local_reference", localReference);
  else if (orderId) query = query.eq("order_id", orderId);
  else if (paymentId) query = query.eq("payment_id", paymentId);
  else return json({ ok: true, ignored: true });

  const updated = await query;
  if (updated.error) throw new Error(`Supabase recusou webhook PagBank: ${updated.error.message}`);
  return json({ ok: true, orderId, paymentId, localReference, status: remoteStatus, updated: Array.isArray(updated.data) ? updated.data.length : 0 });
}

async function ensureLicense(payload: ClientPayload): Promise<
  | { ok: true; license: Record<string, unknown>; status: 200 }
  | { ok: false; message: string; status: number }
> {
  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  if (!licenseKey || !machineHash) return { ok: false, message: "Chave e computador sao obrigatorios.", status: 400 };

  const result = await serviceClient().from("bv_licenses").select("*").eq("key", licenseKey).maybeSingle();
  if (result.error) return { ok: false, message: `Supabase recusou licenca: ${result.error.message}`, status: 500 };
  const license = result.data as Record<string, unknown> | null;
  if (!license) return { ok: false, message: "Chave nao existe no painel admin.", status: 401 };
  if (stringValue(license.status).toUpperCase() === "BLOQUEADA") return { ok: false, message: "Esta chave esta bloqueada.", status: 401 };
  return { ok: true, license, status: 200 };
}

async function getConnection(licenseKey: string) {
  const result = await serviceClient().from("bv_pagbank_connections").select("*").eq("license_key", normalizeLicense(licenseKey)).maybeSingle();
  if (result.error) throw new Error(`Supabase recusou conexao PagBank: ${result.error.message}`);
  return result.data as Record<string, unknown> | null;
}

async function ensureAccessToken(licenseKey: string): Promise<
  | { ok: true; accessToken: string }
  | { ok: false; message: string; status: number }
> {
  const connection = await getConnection(licenseKey);
  if (!connection || !stringValue(connection.access_token)) return { ok: false, message: "PagBank ainda nao conectado para esta loja.", status: 404 };

  const accessToken = stringValue(connection.access_token);
  const refreshToken = stringValue(connection.refresh_token);
  const expiresAt = new Date(stringValue(connection.expires_at)).getTime();
  if (!refreshToken || !expiresAt || expiresAt > Date.now() + 10 * 60_000) return { ok: true, accessToken };

  try {
    const token = await exchangePagBankToken({ grant_type: "refresh_token", refresh_token: refreshToken });
    await serviceClient()
      .from("bv_pagbank_connections")
      .update({
        status: "CONNECTED",
        account_id: stringValue(token.account_id || token.accountId || connection.account_id),
        access_token: stringValue(token.access_token),
        refresh_token: stringValue(token.refresh_token) || refreshToken,
        token_type: stringValue(token.token_type),
        scope: stringValue(token.scope),
        expires_at: tokenExpiresAt(token),
        last_sync_at: new Date().toISOString(),
        last_error: "",
        updated_at: new Date().toISOString(),
      })
      .eq("license_key", normalizeLicense(licenseKey));
    return { ok: true, accessToken: stringValue(token.access_token) };
  } catch (error) {
    const message = messageFromError(error);
    await serviceClient().from("bv_pagbank_connections").update({ status: "ERROR", last_error: message, updated_at: new Date().toISOString() }).eq("license_key", normalizeLicense(licenseKey));
    return { ok: false, message, status: 401 };
  }
}

async function pagBankFetch(accessToken: string, path: string, init: RequestInit = {}) {
  const response = await fetch(`${pagBankApiBaseUrl()}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      "accept": "application/json",
      Authorization: `Bearer ${accessToken}`,
      ...((init.headers ?? {}) as Record<string, string>),
    },
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : {};
  if (!response.ok) throw new Error(`PagBank recusou operacao: ${stringValue(data.message || data.error || text || response.status)}`);
  return data as Record<string, unknown>;
}

async function exchangePagBankToken(params: Record<string, string>) {
  const appToken = pagBankAppToken();
  const clientId = pagBankClientId();
  const clientSecret = pagBankClientSecret();
  if (!appToken || !clientId || !clientSecret) {
    throw new Error("Ativacao PagBank pendente. Configure PAGBANK_APP_TOKEN, PAGBANK_CLIENT_ID e PAGBANK_CLIENT_SECRET no Supabase.");
  }

  const path = params.grant_type === "refresh_token" ? "/oauth2/refresh" : "/oauth2/token";
  const response = await fetch(`${pagBankApiBaseUrl()}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "accept": "application/json",
      Authorization: `Bearer ${appToken}`,
      X_CLIENT_ID: clientId,
      X_CLIENT_SECRET: clientSecret,
    },
    body: JSON.stringify(params),
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : {};
  if (!response.ok) throw new Error(`PagBank recusou token: ${stringValue(data.message || data.error || text || response.status)}`);
  return data as Record<string, unknown>;
}

async function saveAttempt(params: {
  licenseKey: string;
  machineHash: string;
  localReference: string;
  method: string;
  amount: number;
  orderId: string;
  paymentId: string;
  status: string;
  statusDetail: string;
  rawResponse: Record<string, unknown>;
}) {
  const saved = await serviceClient()
    .from("bv_pagbank_payment_attempts")
    .upsert({
      license_key: params.licenseKey,
      machine_hash: params.machineHash,
      local_reference: params.localReference,
      method: params.method,
      amount: params.amount,
      order_id: params.orderId,
      payment_id: params.paymentId,
      terminal_id: "",
      terminal_label: "",
      status: params.status,
      status_detail: params.statusDetail,
      raw_response: params.rawResponse,
      updated_at: new Date().toISOString(),
    }, { onConflict: "license_key,local_reference" })
    .select("id, order_id, payment_id")
    .single();
  if (saved.error) return { ok: false, status: 500, message: `Supabase recusou tentativa PagBank: ${saved.error.message}` };
  return { ok: true, status: 200, attemptId: stringValue(saved.data.id), orderId: stringValue(saved.data.order_id), paymentId: stringValue(saved.data.payment_id) };
}

async function findAttempt(licenseKey: string, payload: StatusPayload): Promise<
  | { ok: true; status: 200; row: Record<string, unknown> }
  | { ok: false; status: number; message: string }
> {
  let query = serviceClient().from("bv_pagbank_payment_attempts").select("*").eq("license_key", licenseKey);
  if (isUuid(stringValue(payload.attemptId))) query = query.eq("id", stringValue(payload.attemptId));
  else if (stringValue(payload.orderId)) query = query.eq("order_id", stringValue(payload.orderId));
  else if (stringValue(payload.localReference)) query = query.eq("local_reference", stringValue(payload.localReference));
  else return { ok: false, status: 400, message: "Informe a tentativa de pagamento." };

  const current = await query.maybeSingle();
  if (current.error || !current.data) return { ok: false, status: current.error ? 500 : 404, message: current.error?.message || "Tentativa PagBank nao encontrada." };
  return { ok: true, status: 200, row: current.data as Record<string, unknown> };
}

function buildItems(value: unknown, amount: number, description: string, localReference: string): PagBankItem[] {
  const items = normalizeItems(value);
  const itemTotal = roundMoney(items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0));
  if (items.length > 0 && Math.abs(itemTotal - amount) <= 0.01) {
    return items.map((item) => ({
      reference_id: item.id,
      name: clipText(item.title, 100),
      quantity: item.quantity,
      unit_amount: moneyCents(item.unitPrice),
    }));
  }

  return [{ reference_id: sanitizeId(localReference), name: clipText(description, 100), quantity: 1, unit_amount: moneyCents(amount) }];
}

function normalizeItems(value: unknown) {
  if (!Array.isArray(value)) return [] as { id: string; title: string; quantity: number; unitPrice: number }[];
  return value
    .slice(0, 50)
    .map((raw, index) => {
      if (!raw || typeof raw !== "object") return null;
      const item = raw as ChargeItemPayload;
      const title = clipText(stringValue(item.title || item.name), 120);
      const unitPrice = roundMoney(item.unitPrice ?? item.unit_price ?? item.price);
      const quantity = Math.max(1, Math.round(numberValue(item.quantity) || 1));
      if (!title || unitPrice <= 0) return null;
      return { id: sanitizeId(item.code || item.id || `ITEM-${index + 1}`), title, quantity, unitPrice };
    })
    .filter((item): item is { id: string; title: string; quantity: number; unitPrice: number } => item !== null);
}

function buildCustomer(payload: ChargePayload, localReference: string) {
  const phone = onlyDigits(payload.payerPhone);
  const phones = phone.length >= 10 ? [{ country: "55", area: phone.slice(0, 2), number: phone.slice(2).slice(0, 9), type: "MOBILE" }] : [];
  const taxId = onlyDigits(payload.payerTaxId);
  return compactObject({
    name: safePayerName(payload.payerName),
    email: safePayerEmail(payload.payerEmail, localReference),
    tax_id: taxId.length === 11 || taxId.length === 14 ? taxId : undefined,
    phones,
  });
}

function normalizeStatus(value: Record<string, unknown>) {
  const charge = firstRecord(value.charges);
  return stringValue(charge.status || value.status || value.payment_status).toUpperCase() || "CREATED";
}

function isPaid(status: string, value: Record<string, unknown>) {
  const charge = firstRecord(value.charges);
  return [status, stringValue(value.status), stringValue(value.payment_status), stringValue(charge.status)]
    .map((item) => item.toUpperCase())
    .some((item) => item === "PAID" || item === "APPROVED" || item === "AUTHORIZED" || item === "COMPLETED");
}

function findLink(records: Record<string, unknown>[], preferredRels: string[]) {
  const links: Record<string, unknown>[] = [];
  for (const record of records) {
    if (Array.isArray(record.links)) links.push(...record.links.filter((item): item is Record<string, unknown> => !!item && typeof item === "object"));
  }

  const prefs = preferredRels.map((item) => item.toUpperCase());
  const match = links.find((link) => prefs.includes(stringValue(link.rel).toUpperCase())) || links.find((link) => stringValue(link.href));
  return stringValue(match?.href);
}

function firstRecord(value: unknown) {
  if (Array.isArray(value) && value[0] && typeof value[0] === "object") return value[0] as Record<string, unknown>;
  return {};
}

function extractWebhookId(payload: Record<string, unknown>, url: URL, names: string[]) {
  const data = payload.data as Record<string, unknown> | undefined;
  const candidates = [data?.id, data?.code, payload.id, payload.code, payload.order_id, payload.checkout_id, payload.charge_id, payload.payment_id, url.searchParams.get("id"), url.searchParams.get("code")].map(stringValue);
  for (const candidate of candidates) if (candidate) return candidate;
  const resource = stringValue(payload.resource || payload.resource_url || url.searchParams.get("resource"));
  if (!resource) return "";
  const pattern = new RegExp(`/(?:${names.map((name) => name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|")})/([^/?#]+)`, "i");
  return resource.match(pattern)?.[1] ?? "";
}

function serviceClient() {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const key = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  if (!url || !key) throw new Error("Supabase service role indisponivel.");
  return createClient(url, key, { auth: { persistSession: false } });
}

function pagBankClientId() {
  return stringValue(Deno.env.get("PAGBANK_CLIENT_ID"));
}

function pagBankClientSecret() {
  return stringValue(Deno.env.get("PAGBANK_CLIENT_SECRET"));
}

function pagBankAppToken() {
  return stringValue(Deno.env.get("PAGBANK_APP_TOKEN") || Deno.env.get("PAGBANK_ACCESS_TOKEN") || Deno.env.get("PAGBANK_TOKEN"));
}

function pagBankRedirectUri() {
  return stringValue(Deno.env.get("PAGBANK_REDIRECT_URI")) || `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/pagbank/oauth/callback`;
}

function pagBankWebhookUrl() {
  return stringValue(Deno.env.get("PAGBANK_WEBHOOK_URL")) || `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/pagbank/webhook`;
}

function isSandbox() {
  return stringValue(Deno.env.get("PAGBANK_SANDBOX")).toLowerCase() === "true";
}

function pagBankApiBaseUrl() {
  return isSandbox() ? "https://sandbox.api.pagseguro.com" : "https://api.pagseguro.com";
}

function pagBankConnectBaseUrl() {
  return isSandbox() ? "https://connect.sandbox.pagseguro.uol.com.br" : "https://connect.pagseguro.uol.com.br";
}

function tokenExpiresAt(token: Record<string, unknown>) {
  const explicit = stringValue(token.expires_at || token.expiration_date);
  if (explicit) return explicit;
  const expiresIn = Math.max(60, Math.trunc(numberValue(token.expires_in)) || 7_776_000);
  return new Date(Date.now() + expiresIn * 1000).toISOString();
}

function safePayerEmail(value: unknown, localReference: string) {
  const email = stringValue(value).toLowerCase();
  if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return email.slice(0, 254);
  const ref = sanitizeReference(localReference).toLowerCase() || crypto.randomUUID();
  return `cliente+${ref}@balcaolivrepdv.com.br`;
}

function safePayerName(value: unknown) {
  const name = stringValue(value).replace(/\s+/g, " ").trim();
  return (name || "Cliente").slice(0, 80);
}

function sanitizeReference(value: unknown) {
  const clean = stringValue(value).replace(/[^A-Za-z0-9_-]+/g, "-").replace(/^-+|-+$/g, "");
  return (clean || crypto.randomUUID()).slice(0, 64);
}

function sanitizeId(value: unknown) {
  return (stringValue(value).replace(/[^A-Za-z0-9_-]+/g, "-").replace(/^-+|-+$/g, "") || crypto.randomUUID()).slice(0, 64);
}

function clipText(value: unknown, maxLength: number) {
  const clean = stringValue(value).replace(/\s+/g, " ").trim();
  return clean.length <= maxLength ? clean : clean.slice(0, maxLength).trim();
}

async function readJson<T>(req: Request): Promise<T> {
  try {
    return await req.json() as T;
  } catch {
    return {} as T;
  }
}

function routeFromPath(pathname: string) {
  const marker = "/pagbank";
  const index = pathname.indexOf(marker);
  const route = index < 0 ? pathname : pathname.slice(index + marker.length) || "/";
  return route.endsWith("/") && route.length > 1 ? route.slice(0, -1) : route;
}

function normalizePayloadKeys<T>(value: T): T {
  if (Array.isArray(value)) return value.map((item) => normalizePayloadKeys(item)) as T;
  if (!value || typeof value !== "object") return value;
  const normalized: Record<string, unknown> = {};
  for (const [key, raw] of Object.entries(value as Record<string, unknown>)) normalized[key ? key[0].toLowerCase() + key.slice(1) : key] = normalizePayloadKeys(raw);
  return normalized as T;
}

function normalizeLicense(value: unknown) {
  return stringValue(value).toUpperCase().replaceAll(" ", "").replaceAll("_", "-");
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function numberValue(value: unknown) {
  if (typeof value === "string") {
    const clean = value.trim().replace(/[^\d,.-]/g, "").replace(/\.(?=\d{3}(?:\D|$))/g, "").replace(",", ".");
    const number = Number(clean || 0);
    return Number.isFinite(number) ? number : 0;
  }

  const number = Number(value ?? 0);
  return Number.isFinite(number) ? number : 0;
}

function roundMoney(value: unknown) {
  return Math.round(numberValue(value) * 100) / 100;
}

function moneyCents(value: unknown) {
  return Math.max(1, Math.round(roundMoney(value) * 100));
}

function onlyDigits(value: unknown) {
  return stringValue(value).replace(/\D+/g, "");
}

function compactObject<T extends Record<string, unknown>>(value: T): T {
  const clean: Record<string, unknown> = {};
  for (const [key, raw] of Object.entries(value)) {
    if (raw === undefined || raw === null || raw === "") continue;
    if (Array.isArray(raw)) {
      const filtered = raw.filter((item) => item !== undefined && item !== null && item !== "");
      if (filtered.length > 0) clean[key] = filtered;
      continue;
    }

    if (typeof raw === "object") {
      clean[key] = compactObject(raw as Record<string, unknown>);
      continue;
    }

    clean[key] = raw;
  }
  return clean as T;
}

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function json(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), { status, headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" } });
}

function html(title: string, message: string, ok: boolean) {
  const params = new URLSearchParams({ pagbank: ok ? "connected" : "error", title: stringValue(title), message: stringValue(message) });
  return new Response(null, {
    status: 302,
    headers: {
      ...corsHeaders,
      location: `https://balcaolivrepdv.com.br/?${params.toString()}`,
      "cache-control": "no-store",
    },
  });
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}
