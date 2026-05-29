import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

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
  terminalId?: string;
  items?: ChargeItemPayload[];
};

type StatusPayload = ClientPayload & {
  attemptId?: string;
  orderId?: string;
  localReference?: string;
};

type WebChargePayload = ChargePayload & {
  payerName?: string;
  payerEmail?: string;
  payerTaxId?: string;
  payerPhone?: string;
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

type MercadoPagoChargeItem = {
  id: string;
  title: string;
  description: string;
  quantity: number;
  unit_price: number;
};

type PagBankChargeItem = {
  reference_id: string;
  name: string;
  quantity: number;
  unit_amount: number;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const route = routeFromPath(new URL(req.url).pathname);

    if (route === "/health" && req.method === "GET") {
      return json({ ok: true, app: "Balcao Livre Payments" });
    }

    if (route === "/mercadopago/oauth/callback" && req.method === "GET") {
      return mercadoPagoOAuthCallback(req);
    }

    if (route === "/pagbank/oauth/callback" && req.method === "GET") {
      return pagBankOAuthCallback(req);
    }

    if (route === "/mercadopago/webhook" && req.method === "POST") {
      return mercadoPagoWebhook(req);
    }

    if (route === "/pagbank/webhook" && req.method === "POST") {
      return pagBankWebhook(req);
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/mercadopago/connect/start") {
      return startMercadoPagoConnect(req);
    }

    if (route === "/mercadopago/status") {
      return getMercadoPagoStatus(req);
    }

    if (route === "/mercadopago/terminals") {
      return listMercadoPagoTerminals(req);
    }

    if (route === "/mercadopago/terminal/select") {
      return selectMercadoPagoTerminal(req);
    }

    if (route === "/mercadopago/point/charge") {
      return createMercadoPagoPointCharge(req);
    }

    if (route === "/mercadopago/point/status") {
      return getMercadoPagoPointStatus(req);
    }

    if (route === "/mercadopago/web/charge") {
      return createMercadoPagoWebCharge(req);
    }

    if (route === "/mercadopago/web/status") {
      return getMercadoPagoWebStatus(req);
    }

    if (route === "/pagbank/connect/start") {
      return startPagBankConnect(req);
    }

    if (route === "/pagbank/status") {
      return getPagBankStatus(req);
    }

    if (route === "/pagbank/terminal/select") {
      return selectPagBankTerminal(req);
    }

    if (route === "/pagbank/web/charge") {
      return createPagBankWebCharge(req);
    }

    if (route === "/pagbank/web/status") {
      return getPagBankWebStatus(req);
    }

    return json({ ok: false, message: "Rota de pagamentos nao encontrada." }, 404);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function startMercadoPagoConnect(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const clientId = mercadoPagoClientId();
  if (!clientId) {
    return json({ ok: false, message: "Ativacao Mercado Pago pendente. Configure a aplicacao Mercado Pago da Balcao Livre." }, 500);
  }

  const state = crypto.randomUUID();
  const saved = await serviceClient().from("bv_mercadopago_oauth_states").insert({
    state,
    license_key: normalizeLicense(payload.licenseKey),
    machine_hash: stringValue(payload.machineHash),
    expires_at: new Date(Date.now() + 10 * 60_000).toISOString(),
  });
  if (saved.error) {
    return json({ ok: false, message: `Supabase recusou conexao Mercado Pago: ${saved.error.message}` }, 500);
  }

  const auth = new URL("https://auth.mercadopago.com.br/authorization");
  auth.searchParams.set("client_id", clientId);
  auth.searchParams.set("response_type", "code");
  auth.searchParams.set("platform_id", "mp");
  auth.searchParams.set("state", state);
  auth.searchParams.set("redirect_uri", mercadoPagoRedirectUri());
  auth.searchParams.set("scope", "offline_access");
  return json({ ok: true, authUrl: auth.toString() });
}

async function mercadoPagoOAuthCallback(req: Request) {
  const url = new URL(req.url);
  const code = stringValue(url.searchParams.get("code"));
  const state = stringValue(url.searchParams.get("state"));
  if (!code || !state) {
    return html("Mercado Pago", "Conexao cancelada ou incompleta.", false);
  }

  const supabase = serviceClient();
  const stateResult = await supabase
    .from("bv_mercadopago_oauth_states")
    .select("*")
    .eq("state", state)
    .maybeSingle();
  if (stateResult.error || !stateResult.data) {
    return html("Mercado Pago", "Conexao expirada. Volte ao PDV e tente de novo.", false);
  }

  const stateRow = stateResult.data as Record<string, unknown>;
  if (stringValue(stateRow.used_at) || new Date(stringValue(stateRow.expires_at)).getTime() < Date.now()) {
    return html("Mercado Pago", "Conexao expirada. Volte ao PDV e tente de novo.", false);
  }

  const token = await exchangeMercadoPagoToken({
    grant_type: "authorization_code",
    code,
    redirect_uri: mercadoPagoRedirectUri(),
  });
  const now = new Date().toISOString();
  const saved = await supabase.from("bv_mercadopago_connections").upsert({
    license_key: stringValue(stateRow.license_key),
    machine_hash: stringValue(stateRow.machine_hash),
    status: "CONNECTED",
    seller_user_id: stringValue(token.user_id),
    access_token: stringValue(token.access_token),
    refresh_token: stringValue(token.refresh_token),
    public_key: stringValue(token.public_key),
    token_type: stringValue(token.token_type),
    scope: stringValue(token.scope),
    expires_at: tokenExpiresAt(token),
    connected_at: now,
    last_sync_at: now,
    last_error: "",
    updated_at: now,
  }, { onConflict: "license_key" });
  if (saved.error) {
    return html("Mercado Pago", `Supabase recusou salvar conexao: ${saved.error.message}`, false);
  }

  await supabase.from("bv_mercadopago_oauth_states").update({ used_at: now }).eq("state", state);
  return html("Mercado Pago conectado", "Conta conectada. Volte ao Balcao Livre PDV e atualize as maquininhas.", true);
}

async function getMercadoPagoStatus(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const connection = await getConnection(normalizeLicense(payload.licenseKey));
  return json({
    ok: true,
    connected: !!connection?.access_token,
    status: stringValue(connection?.status) || "DISCONNECTED",
    sellerUserId: stringValue(connection?.seller_user_id),
    selectedTerminalId: stringValue(connection?.selected_terminal_id),
    selectedTerminalLabel: stringValue(connection?.selected_terminal_label),
    lastSyncAt: stringValue(connection?.last_sync_at),
    lastError: stringValue(connection?.last_error),
  });
}

async function mercadoPagoWebhook(req: Request) {
  const url = new URL(req.url);
  const payload = await readJson<Record<string, unknown>>(req);
  const eventText = [
    payload.type,
    payload.topic,
    payload.action,
    payload.resource,
    url.searchParams.get("type"),
    url.searchParams.get("topic"),
    url.searchParams.get("resource"),
  ].map(stringValue).join(" ").toLowerCase();
  const orderId = extractWebhookOrderId(payload, url, eventText);
  const paymentId = extractWebhookPaymentId(payload, url);

  if ((eventText.includes("order") || eventText.includes("point")) && orderId) {
    return syncMercadoPagoOrderWebhook(orderId);
  }

  if (!eventText.includes("payment") || !paymentId) {
    return json({ ok: true, ignored: true });
  }

  return syncMercadoPagoPaymentWebhook(paymentId);
}

async function syncMercadoPagoPaymentWebhook(paymentId: string) {
  const found = await findPaymentForWebhook(paymentId);
  if (!found.ok) {
    console.log("mercadopago.webhook.payment_not_found", { paymentId, message: found.message });
    return json({ ok: true, pending: true, message: found.message });
  }

  const payment = found.payment;
  const localReference = stringValue(payment.external_reference);
  const status = normalizePaymentStatus(payment);
  const patch = {
    payment_id: stringValue(payment.id) || paymentId,
    status,
    status_detail: stringValue(payment.status_detail),
    raw_response: payment,
    updated_at: new Date().toISOString(),
  };

  let updated = 0;
  if (localReference) {
    const byReference = await serviceClient()
      .from("bv_mercadopago_payment_attempts")
      .update(patch)
      .eq("license_key", found.licenseKey)
      .eq("local_reference", localReference)
      .select("id");
    if (byReference.error) throw new Error(`Supabase recusou webhook Mercado Pago: ${byReference.error.message}`);
    updated += Array.isArray(byReference.data) ? byReference.data.length : 0;
  }

  if (updated === 0) {
    const byPayment = await serviceClient()
      .from("bv_mercadopago_payment_attempts")
      .update(patch)
      .eq("license_key", found.licenseKey)
      .eq("payment_id", paymentId)
      .select("id");
    if (byPayment.error) throw new Error(`Supabase recusou webhook Mercado Pago: ${byPayment.error.message}`);
    updated += Array.isArray(byPayment.data) ? byPayment.data.length : 0;
  }

  return json({ ok: true, paymentId: stringValue(payment.id) || paymentId, localReference, status, updated });
}

async function syncMercadoPagoOrderWebhook(orderId: string) {
  const found = await findOrderForWebhook(orderId);
  if (!found.ok) {
    console.log("mercadopago.webhook.order_not_found", { orderId, message: found.message });
    return json({ ok: true, pending: true, message: found.message });
  }

  const order = found.order;
  const payment = firstPayment(order);
  const localReference = stringValue(order.external_reference);
  const paymentId = stringValue(payment.id);
  const status = normalizeOrderStatus(order);
  const patch = {
    payment_id: paymentId,
    status,
    status_detail: stringValue(order.status_detail || payment.status_detail),
    raw_response: order,
    updated_at: new Date().toISOString(),
  };

  let updated = 0;
  const byOrder = await serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .update(patch)
    .eq("license_key", found.licenseKey)
    .eq("order_id", orderId)
    .select("id");
  if (byOrder.error) throw new Error(`Supabase recusou webhook Mercado Pago Point: ${byOrder.error.message}`);
  updated += Array.isArray(byOrder.data) ? byOrder.data.length : 0;

  if (updated === 0 && localReference) {
    const byReference = await serviceClient()
      .from("bv_mercadopago_payment_attempts")
      .update(patch)
      .eq("license_key", found.licenseKey)
      .eq("local_reference", localReference)
      .select("id");
    if (byReference.error) throw new Error(`Supabase recusou webhook Mercado Pago Point: ${byReference.error.message}`);
    updated += Array.isArray(byReference.data) ? byReference.data.length : 0;
  }

  if (updated === 0 && paymentId) {
    const byPayment = await serviceClient()
      .from("bv_mercadopago_payment_attempts")
      .update(patch)
      .eq("license_key", found.licenseKey)
      .eq("payment_id", paymentId)
      .select("id");
    if (byPayment.error) throw new Error(`Supabase recusou webhook Mercado Pago Point: ${byPayment.error.message}`);
    updated += Array.isArray(byPayment.data) ? byPayment.data.length : 0;
  }

  return json({ ok: true, orderId, paymentId, localReference, status, updated });
}

async function listMercadoPagoTerminals(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json({ ...validation, terminals: [] }, validation.status);

  const token = await ensureAccessToken(normalizeLicense(payload.licenseKey));
  if (!token.ok) return json({ ok: false, message: token.message, terminals: [] }, token.status);

  const response = await mpFetch(token.accessToken, "/terminals/v1/list?limit=50&offset=0");
  const data = response.data as Record<string, unknown> | undefined;
  const terminals = Array.isArray(data?.terminals)
    ? data.terminals as Record<string, unknown>[]
    : Array.isArray(response.terminals)
      ? response.terminals as Record<string, unknown>[]
      : [];
  return json({ ok: true, terminals: terminals.map(terminalToClient) });
}

async function selectMercadoPagoTerminal(req: Request) {
  const payload = normalizePayloadKeys(await readJson<TerminalPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const terminalId = stringValue(payload.terminalId);
  if (!terminalId) return json({ ok: false, message: "Escolha uma maquininha." }, 400);

  const token = await ensureAccessToken(normalizeLicense(payload.licenseKey));
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const setup = await setupMercadoPagoTerminalForPdv(token.accessToken, terminalId);
  if (!setup.ok) {
    return json({
      ok: false,
      message: setup.message,
      operatingMode: setup.operatingMode,
    }, setup.status ?? 400);
  }

  const saved = await serviceClient()
    .from("bv_mercadopago_connections")
    .update({
      selected_terminal_id: terminalId,
      selected_terminal_label: terminalLabelWithMode(stringValue(payload.terminalLabel) || terminalId, setup.operatingMode),
      last_sync_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    })
    .eq("license_key", normalizeLicense(payload.licenseKey))
    .select("license_key")
    .maybeSingle();
  if (saved.error) return json({ ok: false, message: `Supabase recusou maquininha: ${saved.error.message}` }, 500);
  if (!saved.data) return json({ ok: false, message: "Conecte o Mercado Pago antes de escolher a maquininha." }, 404);
  return json({ ok: true, message: "Maquininha salva em modo PDV." });
}

async function createMercadoPagoPointCharge(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ChargePayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const connection = await getConnection(licenseKey);
  const terminalId = stringValue(payload.terminalId) || stringValue(connection?.selected_terminal_id);
  if (!terminalId) return json({ ok: false, message: "Escolha a maquininha Mercado Pago antes de cobrar." }, 400);

  const amount = roundMoney(payload.amount);
  if (amount <= 0) return json({ ok: false, message: "Valor invalido para cobranca." }, 400);

  const token = await ensureAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const setup = await setupMercadoPagoTerminalForPdv(token.accessToken, terminalId);
  if (!setup.ok) {
    return json({
      ok: false,
      message: setup.message,
      operatingMode: setup.operatingMode,
    }, setup.status ?? 400);
  }

  const localReference = sanitizeReference(payload.localReference || crypto.randomUUID());
  const defaultType = normalizePointPaymentMethod(payload.method);
  const body: Record<string, unknown> = {
    type: "point",
    external_reference: localReference,
    expiration_time: "PT10M",
    transactions: {
      payments: [{ amount: amount.toFixed(2) }],
    },
    config: {
      point: {
        terminal_id: terminalId,
        print_on_terminal: "no_ticket",
      },
      ...(defaultType ? { payment_method: { default_type: defaultType } } : {}),
    },
    description: stringValue(payload.description) || "Balcao Livre PDV",
  };

  const order = await mpFetch(token.accessToken, "/v1/orders", {
    method: "POST",
    headers: { "X-Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify(body),
  });
  const payment = firstPayment(order);
  const status = normalizeOrderStatus(order);
  const saved = await serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .upsert({
      license_key: licenseKey,
      machine_hash: stringValue(payload.machineHash),
      local_reference: localReference,
      method: stringValue(payload.method).toUpperCase() || "POINT",
      amount,
      order_id: stringValue(order.id),
      payment_id: stringValue(payment.id),
      terminal_id: terminalId,
      terminal_label: stringValue(connection?.selected_terminal_label) || terminalId,
      status,
      status_detail: stringValue(order.status_detail || payment.status_detail),
      raw_response: order,
      updated_at: new Date().toISOString(),
    }, { onConflict: "license_key,local_reference" })
    .select("id, order_id, payment_id, status, status_detail")
    .single();
  if (saved.error) return json({ ok: false, message: `Supabase recusou tentativa Mercado Pago: ${saved.error.message}` }, 500);

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
  const payload = normalizePayloadKeys(await readJson<StatusPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

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
  const token = await ensureAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const orderId = stringValue(row.order_id);
  const order = orderId ? await mpFetch(token.accessToken, `/v1/orders/${encodeURIComponent(orderId)}`) : row.raw_response as Record<string, unknown>;
  const payment = firstPayment(order);
  const status = normalizeOrderStatus(order);
  await serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .update({
      status,
      status_detail: stringValue(order.status_detail || payment.status_detail),
      payment_id: stringValue(payment.id) || stringValue(row.payment_id),
      raw_response: order,
      updated_at: new Date().toISOString(),
    })
    .eq("id", stringValue(row.id));

  return json({
    ok: true,
    attemptId: stringValue(row.id),
    orderId,
    paymentId: stringValue(payment.id) || stringValue(row.payment_id),
    status,
    statusDetail: stringValue(order.status_detail || payment.status_detail),
    paid: isPaid(status, payment),
  });
}

async function createMercadoPagoWebCharge(req: Request) {
  const payload = normalizePayloadKeys(await readJson<WebChargePayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const amount = roundMoney(payload.amount);
  if (amount <= 0) return json({ ok: false, message: "Valor invalido para cobranca." }, 400);

  const token = await ensureAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const method = stringValue(payload.method).toUpperCase();
  const localReference = sanitizeReference(payload.localReference || crypto.randomUUID());
  const requestedDescription = stringValue(payload.description) || "Balcao Livre PDV";
  const description = method === "PIX"
    ? clipText(requestedDescription.split(" - ")[0] || "Balcao Livre PDV", 40)
    : requestedDescription;
  const chargeItems = normalizeChargeItems(payload.items);

  if (method === "PIX") {
    const expiresAt = new Date(Date.now() + 15 * 60_000).toISOString();
    const paymentBody: Record<string, unknown> = {
      transaction_amount: amount,
      description,
      payment_method_id: "pix",
      external_reference: localReference,
      date_of_expiration: expiresAt,
      payer: {
        email: safePayerEmail(payload.payerEmail, localReference),
        first_name: safePayerName(payload.payerName),
      },
    };

    const payment = await mpFetch(token.accessToken, "/v1/payments", {
      method: "POST",
      headers: { "X-Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify(paymentBody),
    });
    const transactionData = paymentTransactionData(payment);
    const status = normalizePaymentStatus(payment);
    const saved = await savePaymentAttempt({
      licenseKey,
      machineHash: stringValue(payload.machineHash),
      localReference,
      method: "PIX_QR",
      amount,
      orderId: "",
      paymentId: stringValue(payment.id),
      status,
      statusDetail: stringValue(payment.status_detail),
      rawResponse: payment,
    });
    if (!saved.ok) return json(saved, saved.status);

    return json({
      ok: true,
      message: "Pix Mercado Pago gerado.",
      attemptId: saved.attemptId,
      localReference,
      paymentId: stringValue(payment.id),
      status,
      statusDetail: stringValue(payment.status_detail),
      qrCode: stringValue(transactionData.qr_code),
      qrCodeBase64: stringValue(transactionData.qr_code_base64),
      ticketUrl: stringValue(transactionData.ticket_url),
      paymentUrl: stringValue(transactionData.ticket_url),
      expiresAt,
    });
  }

  const preferenceItems = buildPreferenceItems(chargeItems, amount, description, localReference);
  const preferenceAdditionalInfo = buildPreferenceAdditionalInfo(description, chargeItems);
  const preference = await mpFetch(token.accessToken, "/checkout/preferences", {
    method: "POST",
    headers: { "X-Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify({
      items: preferenceItems,
      additional_info: preferenceAdditionalInfo,
      external_reference: localReference,
      expires: true,
      expiration_date_from: new Date().toISOString(),
      expiration_date_to: new Date(Date.now() + 30 * 60_000).toISOString(),
      payment_methods: {
        excluded_payment_types: [
          { id: "ticket" },
          { id: "bank_transfer" },
          { id: "atm" },
        ],
      },
      metadata: {
        source: "balcao_livre_pdv",
        license_key: licenseKey,
        machine_hash: stringValue(payload.machineHash),
      },
    }),
  });
  const paymentUrl = stringValue(preference.init_point || preference.sandbox_init_point);
  const saved = await savePaymentAttempt({
    licenseKey,
    machineHash: stringValue(payload.machineHash),
    localReference,
    method: method === "DEBITO" ? "LINK_DEBITO" : "LINK_CREDITO",
    amount,
    orderId: stringValue(preference.id),
    paymentId: "",
    status: "CREATED",
    statusDetail: "",
    rawResponse: preference,
  });
  if (!saved.ok) return json(saved, saved.status);

  return json({
    ok: true,
    message: "Link de pagamento Mercado Pago gerado.",
    attemptId: saved.attemptId,
    localReference,
    orderId: stringValue(preference.id),
    status: "CREATED",
    paymentUrl,
  });
}

async function getMercadoPagoWebStatus(req: Request) {
  const payload = normalizePayloadKeys(await readJson<StatusPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const current = await findPaymentAttempt(licenseKey, payload);
  if (!current.ok) return json(current, current.status);

  const row = current.row;
  const token = await ensureAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const paymentId = stringValue(row.payment_id);
  const localReference = stringValue(row.local_reference);
  let payment: Record<string, unknown> | null = null;
  if (paymentId) {
    payment = await mpFetch(token.accessToken, `/v1/payments/${encodeURIComponent(paymentId)}`);
  } else if (localReference) {
    payment = await findLatestPaymentByReference(token.accessToken, localReference);
  }

  if (!payment) {
    return json({
      ok: true,
      attemptId: stringValue(row.id),
      orderId: stringValue(row.order_id),
      paymentId: "",
      status: stringValue(row.status) || "CREATED",
      statusDetail: stringValue(row.status_detail),
      paid: false,
    });
  }

  const status = normalizePaymentStatus(payment);
  await serviceClient()
    .from("bv_mercadopago_payment_attempts")
    .update({
      status,
      status_detail: stringValue(payment.status_detail),
      payment_id: stringValue(payment.id) || paymentId,
      raw_response: payment,
      updated_at: new Date().toISOString(),
    })
    .eq("id", stringValue(row.id));

  return json({
    ok: true,
    attemptId: stringValue(row.id),
    orderId: stringValue(row.order_id),
    paymentId: stringValue(payment.id) || paymentId,
    status,
    statusDetail: stringValue(payment.status_detail),
    paid: isWebPaymentPaid(payment),
  });
}

async function startPagBankConnect(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const clientId = pagBankClientId();
  if (!clientId) {
    return json({ ok: false, message: "Ativacao PagBank pendente. Configure a aplicacao PagBank da Balcao Livre." }, 500);
  }

  const state = crypto.randomUUID();
  const saved = await serviceClient().from("bv_pagbank_oauth_states").insert({
    state,
    license_key: normalizeLicense(payload.licenseKey),
    machine_hash: stringValue(payload.machineHash),
    expires_at: new Date(Date.now() + 10 * 60_000).toISOString(),
  });
  if (saved.error) {
    return json({ ok: false, message: `Supabase recusou conexao PagBank: ${saved.error.message}` }, 500);
  }

  const auth = new URL(`${pagBankConnectBaseUrl()}/oauth2/authorize`);
  auth.searchParams.set("client_id", clientId);
  auth.searchParams.set("response_type", "code");
  auth.searchParams.set("redirect_uri", pagBankRedirectUri());
  auth.searchParams.set("scope", "payments.read payments.create checkout.create checkout.view accounts.read");
  auth.searchParams.set("state", state);
  return json({ ok: true, authUrl: auth.toString(), expiresAt: new Date(Date.now() + 10 * 60_000).toISOString() });
}

async function pagBankOAuthCallback(req: Request) {
  const url = new URL(req.url);
  const code = stringValue(url.searchParams.get("code"));
  const state = stringValue(url.searchParams.get("state"));
  if (!code || !state) {
    return providerHtml("pagbank", "PagBank", "Conexao cancelada ou incompleta.", false);
  }

  const supabase = serviceClient();
  const stateResult = await supabase
    .from("bv_pagbank_oauth_states")
    .select("*")
    .eq("state", state)
    .maybeSingle();
  if (stateResult.error || !stateResult.data) {
    return providerHtml("pagbank", "PagBank", "Conexao expirada. Volte ao PDV e tente de novo.", false);
  }

  const stateRow = stateResult.data as Record<string, unknown>;
  if (stringValue(stateRow.used_at) || new Date(stringValue(stateRow.expires_at)).getTime() < Date.now()) {
    return providerHtml("pagbank", "PagBank", "Conexao expirada. Volte ao PDV e tente de novo.", false);
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
      expires_at: pagBankTokenExpiresAt(token),
      connected_at: now,
      last_sync_at: now,
      last_error: "",
      updated_at: now,
    }, { onConflict: "license_key" });
    if (saved.error) {
      return providerHtml("pagbank", "PagBank", `Supabase recusou salvar conexao: ${saved.error.message}`, false);
    }

    await supabase.from("bv_pagbank_oauth_states").update({ used_at: now }).eq("state", state);
    return providerHtml("pagbank", "PagBank conectado", "Conta conectada. Volte ao Balcao Livre PDV; a tela atualiza sozinha.", true);
  } catch (error) {
    return providerHtml("pagbank", "PagBank", messageFromError(error), false);
  }
}

async function getPagBankStatus(req: Request) {
  const payload = normalizePayloadKeys(await readJson<ClientPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const connection = await getPagBankConnection(normalizeLicense(payload.licenseKey));
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

async function selectPagBankTerminal(req: Request) {
  const payload = normalizePayloadKeys(await readJson<TerminalPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const terminalId = stringValue(payload.terminalId) || "PLUGPAG";
  const terminalLabel = stringValue(payload.terminalLabel) || "Moderninha PlugPag";
  const comPort = stringValue(payload.comPort).toUpperCase().replace(/\s+/g, "");
  if (!comPort) return json({ ok: false, message: "Informe a porta COM da Moderninha." }, 400);

  const saved = await serviceClient()
    .from("bv_pagbank_connections")
    .update({
      selected_terminal_id: terminalId,
      selected_terminal_label: terminalLabel,
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

async function createPagBankWebCharge(req: Request) {
  const payload = normalizePayloadKeys(await readJson<WebChargePayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const amount = roundMoney(payload.amount);
  if (amount <= 0) return json({ ok: false, message: "Valor invalido para cobranca." }, 400);

  const token = await ensurePagBankAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const method = stringValue(payload.method).toUpperCase();
  const localReference = sanitizeReference(payload.localReference || crypto.randomUUID());
  const description = clipText(stringValue(payload.description) || "Balcao Livre PDV", 120);
  const items = buildPagBankItems(payload.items, amount, description, localReference);
  const customer = buildPagBankCustomer(payload, localReference);
  const expiresAt = new Date(Date.now() + 30 * 60_000).toISOString();

  if (method === "PIX") {
    const orderBody = compactObject({
      reference_id: localReference,
      customer,
      items,
      qr_codes: [{
        amount: { value: moneyCents(amount) },
      }],
      notification_urls: [pagBankWebhookUrl()].filter(Boolean),
    });

    const order = await pagBankFetch(token.accessToken, "/orders", {
      method: "POST",
      headers: { "x-idempotency-key": crypto.randomUUID() },
      body: JSON.stringify(orderBody),
    });
    const qrCode = firstRecord(order.qr_codes);
    const paymentUrl = findPagBankLink([qrCode, order], ["PAY", "CHECKOUT", "QRCODE.PNG", "QR_CODE", "QRCODE"]);
    const status = normalizePagBankStatus(order);
    const saved = await savePagBankPaymentAttempt({
      licenseKey,
      machineHash: stringValue(payload.machineHash),
      localReference,
      method: "PIX_QR",
      amount,
      orderId: stringValue(order.id),
      paymentId: stringValue(qrCode.id),
      terminalId: "",
      terminalLabel: "",
      status,
      statusDetail: stringValue(order.status_detail),
      rawResponse: order,
    });
    if (!saved.ok) return json(saved, saved.status);

    return json({
      ok: true,
      message: "Pix PagBank gerado.",
      attemptId: saved.attemptId,
      localReference,
      orderId: stringValue(order.id),
      paymentId: stringValue(qrCode.id),
      status,
      statusDetail: stringValue(order.status_detail),
      qrCode: stringValue(qrCode.text || qrCode.qr_code || qrCode.emv || qrCode.payload),
      qrCodeBase64: "",
      ticketUrl: paymentUrl,
      paymentUrl,
      expiresAt,
    });
  }

  const checkoutBody = compactObject({
    reference_id: localReference,
    expiration_date: expiresAt,
    customer_modifiable: true,
    items,
    payment_methods: [{ type: method === "DEBITO" ? "DEBIT_CARD" : "CREDIT_CARD" }],
    payment_methods_configs: [{
      type: method === "DEBITO" ? "DEBIT_CARD" : "CREDIT_CARD",
      config_options: [{ option: "INSTALLMENTS_LIMIT", value: "1" }],
    }],
    soft_descriptor: clipText(description.replace(/[^A-Za-z0-9 ]+/g, " "), 17),
    redirect_url: stringValue(Deno.env.get("PAYMENTS_RETURN_URL")) || "https://www.balcaolivrepdv.com.br",
    return_url: stringValue(Deno.env.get("PAYMENTS_RETURN_URL")) || "https://www.balcaolivrepdv.com.br",
    notification_urls: [pagBankWebhookUrl()].filter(Boolean),
    payment_notification_urls: [pagBankWebhookUrl()].filter(Boolean),
  });
  const checkout = await pagBankFetch(token.accessToken, "/checkouts", {
    method: "POST",
    headers: { "x-idempotency-key": crypto.randomUUID() },
    body: JSON.stringify(checkoutBody),
  });
  const paymentUrl = findPagBankLink([checkout], ["PAY", "CHECKOUT", "REDIRECT", "SELF"]);
  const status = normalizePagBankStatus(checkout);
  const saved = await savePagBankPaymentAttempt({
    licenseKey,
    machineHash: stringValue(payload.machineHash),
    localReference,
    method: method === "DEBITO" ? "CHECKOUT_DEBITO" : "CHECKOUT_CREDITO",
    amount,
    orderId: stringValue(checkout.id),
    paymentId: "",
    terminalId: "",
    terminalLabel: "",
    status,
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
    status,
    paymentUrl,
    expiresAt,
  });
}

async function getPagBankWebStatus(req: Request) {
  const payload = normalizePayloadKeys(await readJson<StatusPayload>(req));
  const validation = await ensureLicense(payload);
  if (!validation.ok) return json(validation, validation.status);

  const licenseKey = normalizeLicense(payload.licenseKey);
  const current = await findPagBankPaymentAttempt(licenseKey, payload);
  if (!current.ok) return json(current, current.status);

  const row = current.row;
  const token = await ensurePagBankAccessToken(licenseKey);
  if (!token.ok) return json({ ok: false, message: token.message }, token.status);

  const orderId = stringValue(row.order_id || payload.orderId);
  const method = stringValue(row.method).toUpperCase();
  const apiPath = method.includes("CHECKOUT")
    ? `/checkouts/${encodeURIComponent(orderId)}`
    : `/orders/${encodeURIComponent(orderId)}`;
  const remote = orderId
    ? await pagBankFetch(token.accessToken, apiPath)
    : row.raw_response as Record<string, unknown>;
  const charge = firstRecord(remote.charges);
  const status = normalizePagBankStatus(remote);
  const paymentId = stringValue(charge.id || row.payment_id);
  await serviceClient()
    .from("bv_pagbank_payment_attempts")
    .update({
      status,
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
    status,
    statusDetail: stringValue(remote.status_detail || charge.status_detail),
    paid: isPagBankPaid(status, remote),
  });
}

async function pagBankWebhook(req: Request) {
  const url = new URL(req.url);
  const payload = await readJson<Record<string, unknown>>(req);
  const orderId = extractPagBankWebhookId(payload, url, ["order", "orders", "checkout", "checkouts"]);
  const paymentId = extractPagBankWebhookId(payload, url, ["charge", "charges", "payment", "payments"]);
  const localReference = stringValue(payload.reference_id || payload.referenceId || payload.external_reference || url.searchParams.get("reference_id"));
  const status = normalizePagBankStatus(payload);
  const patch: Record<string, unknown> = {
    status,
    status_detail: stringValue(payload.status_detail),
    raw_response: payload,
    updated_at: new Date().toISOString(),
  };
  if (paymentId) patch.payment_id = paymentId;

  let query = serviceClient().from("bv_pagbank_payment_attempts").update(patch).select("id");
  if (localReference) {
    query = query.eq("local_reference", localReference);
  } else if (orderId) {
    query = query.eq("order_id", orderId);
  } else if (paymentId) {
    query = query.eq("payment_id", paymentId);
  } else {
    return json({ ok: true, ignored: true });
  }

  const updated = await query;
  if (updated.error) throw new Error(`Supabase recusou webhook PagBank: ${updated.error.message}`);
  return json({
    ok: true,
    orderId,
    paymentId,
    localReference,
    status,
    updated: Array.isArray(updated.data) ? updated.data.length : 0,
  });
}

async function savePaymentAttempt(params: {
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
    .from("bv_mercadopago_payment_attempts")
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
    .select("id, order_id, payment_id, status, status_detail")
    .single();
  if (saved.error) {
    return {
      ok: false,
      status: 500,
      message: `Supabase recusou tentativa Mercado Pago: ${saved.error.message}`,
    };
  }

  return {
    ok: true,
    status: 200,
    attemptId: stringValue(saved.data.id),
    orderId: stringValue(saved.data.order_id),
    paymentId: stringValue(saved.data.payment_id),
  };
}

async function findPaymentAttempt(licenseKey: string, payload: StatusPayload): Promise<
  | { ok: true; status: 200; row: Record<string, unknown> }
  | { ok: false; status: number; message: string }
> {
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
    return { ok: false, status: 400, message: "Informe a tentativa de pagamento." };
  }

  const current = await query.maybeSingle();
  if (current.error || !current.data) {
    return {
      ok: false,
      status: current.error ? 500 : 404,
      message: current.error?.message || "Tentativa nao encontrada.",
    };
  }

  return { ok: true, status: 200, row: current.data as Record<string, unknown> };
}

async function savePagBankPaymentAttempt(params: {
  licenseKey: string;
  machineHash: string;
  localReference: string;
  method: string;
  amount: number;
  orderId: string;
  paymentId: string;
  terminalId: string;
  terminalLabel: string;
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
      terminal_id: params.terminalId,
      terminal_label: params.terminalLabel,
      status: params.status,
      status_detail: params.statusDetail,
      raw_response: params.rawResponse,
      updated_at: new Date().toISOString(),
    }, { onConflict: "license_key,local_reference" })
    .select("id, order_id, payment_id, status, status_detail")
    .single();
  if (saved.error) {
    return {
      ok: false,
      status: 500,
      message: `Supabase recusou tentativa PagBank: ${saved.error.message}`,
    };
  }

  return {
    ok: true,
    status: 200,
    attemptId: stringValue(saved.data.id),
    orderId: stringValue(saved.data.order_id),
    paymentId: stringValue(saved.data.payment_id),
  };
}

async function findPagBankPaymentAttempt(licenseKey: string, payload: StatusPayload): Promise<
  | { ok: true; status: 200; row: Record<string, unknown> }
  | { ok: false; status: number; message: string }
> {
  let query = serviceClient()
    .from("bv_pagbank_payment_attempts")
    .select("*")
    .eq("license_key", licenseKey);

  if (isUuid(stringValue(payload.attemptId))) {
    query = query.eq("id", stringValue(payload.attemptId));
  } else if (stringValue(payload.orderId)) {
    query = query.eq("order_id", stringValue(payload.orderId));
  } else if (stringValue(payload.localReference)) {
    query = query.eq("local_reference", stringValue(payload.localReference));
  } else {
    return { ok: false, status: 400, message: "Informe a tentativa de pagamento." };
  }

  const current = await query.maybeSingle();
  if (current.error || !current.data) {
    return {
      ok: false,
      status: current.error ? 500 : 404,
      message: current.error?.message || "Tentativa PagBank nao encontrada.",
    };
  }

  return { ok: true, status: 200, row: current.data as Record<string, unknown> };
}

async function findLatestPaymentByReference(accessToken: string, localReference: string) {
  const search = await mpFetch(accessToken, `/v1/payments/search?external_reference=${encodeURIComponent(localReference)}&sort=date_created&criteria=desc`);
  const results = Array.isArray(search.results) ? search.results as Record<string, unknown>[] : [];
  return results[0] ?? null;
}

async function ensureLicense(payload: ClientPayload): Promise<
  | { ok: true; license: Record<string, unknown>; status: 200 }
  | { ok: false; message: string; status: number }
> {
  const licenseKey = normalizeLicense(payload.licenseKey);
  const machineHash = stringValue(payload.machineHash);
  if (!licenseKey || !machineHash) {
    return { ok: false, message: "Chave e computador sao obrigatorios.", status: 400 };
  }

  const result = await serviceClient().from("bv_licenses").select("*").eq("key", licenseKey).maybeSingle();
  if (result.error) return { ok: false, message: `Supabase recusou licenca: ${result.error.message}`, status: 500 };
  const license = result.data as Record<string, unknown> | null;
  if (!license) return { ok: false, message: "Chave nao existe no painel admin.", status: 401 };
  if (stringValue(license.status).toUpperCase() === "BLOQUEADA") return { ok: false, message: "Esta chave esta bloqueada.", status: 401 };
  return { ok: true, license, status: 200 };
}

async function getConnection(licenseKey: string) {
  const result = await serviceClient()
    .from("bv_mercadopago_connections")
    .select("*")
    .eq("license_key", normalizeLicense(licenseKey))
    .maybeSingle();
  if (result.error) throw new Error(`Supabase recusou conexao Mercado Pago: ${result.error.message}`);
  return result.data as Record<string, unknown> | null;
}

async function findPaymentForWebhook(paymentId: string): Promise<
  | { ok: true; licenseKey: string; payment: Record<string, unknown> }
  | { ok: false; message: string }
> {
  const connections = await serviceClient()
    .from("bv_mercadopago_connections")
    .select("license_key,access_token,refresh_token,expires_at")
    .not("access_token", "is", null);
  if (connections.error) {
    throw new Error(`Supabase recusou conexoes Mercado Pago: ${connections.error.message}`);
  }

  const rows = Array.isArray(connections.data) ? connections.data as Record<string, unknown>[] : [];
  for (const row of rows) {
    const licenseKey = normalizeLicense(row.license_key);
    if (!licenseKey) continue;
    const token = await ensureAccessToken(licenseKey);
    if (!token.ok) continue;

    try {
      const payment = await mpFetch(token.accessToken, `/v1/payments/${encodeURIComponent(paymentId)}`);
      return { ok: true, licenseKey, payment };
    } catch (error) {
      console.log("mercadopago.webhook.payment_lookup_skip", { licenseKey, paymentId, message: messageFromError(error) });
    }
  }

  return { ok: false, message: "Pagamento ainda nao localizado em nenhuma conta conectada." };
}

async function findOrderForWebhook(orderId: string): Promise<
  | { ok: true; licenseKey: string; order: Record<string, unknown> }
  | { ok: false; message: string }
> {
  const connections = await serviceClient()
    .from("bv_mercadopago_connections")
    .select("license_key,access_token,refresh_token,expires_at")
    .not("access_token", "is", null);
  if (connections.error) {
    throw new Error(`Supabase recusou conexoes Mercado Pago: ${connections.error.message}`);
  }

  const rows = Array.isArray(connections.data) ? connections.data as Record<string, unknown>[] : [];
  for (const row of rows) {
    const licenseKey = normalizeLicense(row.license_key);
    if (!licenseKey) continue;
    const token = await ensureAccessToken(licenseKey);
    if (!token.ok) continue;

    try {
      const order = await mpFetch(token.accessToken, `/v1/orders/${encodeURIComponent(orderId)}`);
      return { ok: true, licenseKey, order };
    } catch (error) {
      console.log("mercadopago.webhook.order_lookup_skip", { licenseKey, orderId, message: messageFromError(error) });
    }
  }

  return { ok: false, message: "Pedido Point ainda nao localizado em nenhuma conta conectada." };
}

async function ensureAccessToken(licenseKey: string): Promise<
  | { ok: true; accessToken: string }
  | { ok: false; message: string; status: number }
> {
  const connection = await getConnection(licenseKey);
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
    const token = await exchangeMercadoPagoToken({ grant_type: "refresh_token", refresh_token: refreshToken });
    await serviceClient()
      .from("bv_mercadopago_connections")
      .update({
        status: "CONNECTED",
        access_token: stringValue(token.access_token),
        refresh_token: stringValue(token.refresh_token) || refreshToken,
        public_key: stringValue(token.public_key),
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
    await serviceClient()
      .from("bv_mercadopago_connections")
      .update({ status: "ERROR", last_error: message, updated_at: new Date().toISOString() })
      .eq("license_key", normalizeLicense(licenseKey));
    return { ok: false, message, status: 401 };
  }
}

async function getPagBankConnection(licenseKey: string) {
  const result = await serviceClient()
    .from("bv_pagbank_connections")
    .select("*")
    .eq("license_key", normalizeLicense(licenseKey))
    .maybeSingle();
  if (result.error) throw new Error(`Supabase recusou conexao PagBank: ${result.error.message}`);
  return result.data as Record<string, unknown> | null;
}

async function ensurePagBankAccessToken(licenseKey: string): Promise<
  | { ok: true; accessToken: string }
  | { ok: false; message: string; status: number }
> {
  const connection = await getPagBankConnection(licenseKey);
  if (!connection || !stringValue(connection.access_token)) {
    return { ok: false, message: "PagBank ainda nao conectado para esta loja.", status: 404 };
  }

  const accessToken = stringValue(connection.access_token);
  const refreshToken = stringValue(connection.refresh_token);
  const expiresAt = new Date(stringValue(connection.expires_at)).getTime();
  if (!refreshToken || !expiresAt || expiresAt > Date.now() + 10 * 60_000) {
    return { ok: true, accessToken };
  }

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
        expires_at: pagBankTokenExpiresAt(token),
        last_sync_at: new Date().toISOString(),
        last_error: "",
        updated_at: new Date().toISOString(),
      })
      .eq("license_key", normalizeLicense(licenseKey));
    return { ok: true, accessToken: stringValue(token.access_token) };
  } catch (error) {
    const message = messageFromError(error);
    await serviceClient()
      .from("bv_pagbank_connections")
      .update({ status: "ERROR", last_error: message, updated_at: new Date().toISOString() })
      .eq("license_key", normalizeLicense(licenseKey));
    return { ok: false, message, status: 401 };
  }
}

async function mpFetch(accessToken: string, path: string, init: RequestInit = {}) {
  const response = await fetch(`https://api.mercadopago.com${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
      ...((init.headers ?? {}) as Record<string, string>),
    },
  });
  const text = await response.text();
  const data = parseJsonObject(text);
  if (!response.ok) throw new Error(`Mercado Pago recusou operacao: ${mercadoPagoErrorMessage(data, text, response.status)}`);
  return data as Record<string, unknown>;
}

async function setupMercadoPagoTerminalForPdv(accessToken: string, terminalId: string): Promise<
  | { ok: true; operatingMode: string }
  | { ok: false; message: string; status?: number; operatingMode?: string }
> {
  const id = stringValue(terminalId);
  if (!id) return { ok: false, message: "Maquininha Mercado Pago invalida.", status: 400 };

  try {
    const response = await mpFetch(accessToken, "/terminals/v1/setup", {
      method: "PATCH",
      body: JSON.stringify({
        terminals: [
          {
            id,
            operating_mode: "PDV",
          },
        ],
      }),
    });
    const terminals = Array.isArray(response.terminals) ? response.terminals as Record<string, unknown>[] : [];
    const terminal = terminals.find((item) => stringValue(item.id) === id) ?? terminals[0] ?? {};
    const operatingMode = stringValue(terminal.operating_mode).toUpperCase() || "PDV";
    if (operatingMode === "PDV") return { ok: true, operatingMode };

    return {
      ok: false,
      operatingMode,
      status: 409,
      message: `A maquininha voltou em modo ${operatingMode}. No Mercado Pago Point, a cobranca por API so chega na maquininha em modo PDV.`,
    };
  } catch (error) {
    const message = messageFromError(error);
    return {
      ok: false,
      status: 400,
      message: `${message}. Confirme se a Point esta associada a uma loja/caixa da conta Mercado Pago e reinicie a maquininha em modo PDV.`,
    };
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

async function exchangeMercadoPagoToken(params: Record<string, string>) {
  const clientId = mercadoPagoClientId();
  const clientSecret = mercadoPagoClientSecret();
  if (!clientId || !clientSecret) throw new Error("Ativacao Mercado Pago pendente. Configure a aplicacao Mercado Pago da Balcao Livre.");

  const response = await fetch("https://api.mercadopago.com/oauth/token", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({ client_id: clientId, client_secret: clientSecret, ...params }),
  });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(`Mercado Pago recusou token: ${stringValue(data.message || data.error || response.status)}`);
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

function serviceClient() {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const key = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  if (!url || !key) throw new Error("Supabase service role indisponivel.");
  return createClient(url, key, { auth: { persistSession: false } });
}

function mercadoPagoClientId() {
  return stringValue(Deno.env.get("MERCADO_PAGO_CLIENT_ID"));
}

function mercadoPagoClientSecret() {
  return stringValue(Deno.env.get("MERCADO_PAGO_CLIENT_SECRET"));
}

function mercadoPagoRedirectUri() {
  return stringValue(Deno.env.get("MERCADO_PAGO_REDIRECT_URI"))
    || `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/payments/mercadopago/oauth/callback`;
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
  return stringValue(Deno.env.get("PAGBANK_REDIRECT_URI"))
    || `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/payments/pagbank/oauth/callback`;
}

function pagBankWebhookUrl() {
  return stringValue(Deno.env.get("PAGBANK_WEBHOOK_URL"))
    || `${stringValue(Deno.env.get("SUPABASE_URL")).replace(/\/$/, "")}/functions/v1/payments/pagbank/webhook`;
}

function isPagBankSandbox() {
  return stringValue(Deno.env.get("PAGBANK_SANDBOX")).toLowerCase() === "true";
}

function pagBankApiBaseUrl() {
  return isPagBankSandbox()
    ? "https://sandbox.api.pagseguro.com"
    : "https://api.pagseguro.com";
}

function pagBankConnectBaseUrl() {
  return isPagBankSandbox()
    ? "https://connect.sandbox.pagseguro.uol.com.br"
    : "https://connect.pagseguro.uol.com.br";
}

function tokenExpiresAt(token: Record<string, unknown>) {
  const expiresIn = Math.max(60, Math.trunc(numberValue(token.expires_in)) || 15_552_000);
  return new Date(Date.now() + expiresIn * 1000).toISOString();
}

function pagBankTokenExpiresAt(token: Record<string, unknown>) {
  const explicit = stringValue(token.expires_at || token.expiration_date);
  if (explicit) return explicit;
  const expiresIn = Math.max(60, Math.trunc(numberValue(token.expires_in)) || 7_776_000);
  return new Date(Date.now() + expiresIn * 1000).toISOString();
}

function terminalToClient(terminal: Record<string, unknown>) {
  const id = stringValue(terminal.id);
  const serial = id.includes("__") ? id.split("__").pop() || id : id;
  const operatingMode = stringValue(terminal.operating_mode);
  return {
    id,
    label: terminalLabelWithMode(serial, operatingMode),
    posId: stringValue(terminal.pos_id),
    storeId: stringValue(terminal.store_id),
    externalPosId: stringValue(terminal.external_pos_id),
    operatingMode,
  };
}

function terminalLabelWithMode(label: string, operatingMode: string) {
  const cleanLabel = stringValue(label);
  const mode = stringValue(operatingMode).toUpperCase();
  if (!mode) return cleanLabel;
  if (cleanLabel.toUpperCase().includes(`(${mode})`)) return cleanLabel;
  return `${cleanLabel} (${mode})`;
}

function firstPayment(order: Record<string, unknown>) {
  const transactions = order.transactions as Record<string, unknown> | undefined;
  const payments = Array.isArray(transactions?.payments) ? transactions.payments as Record<string, unknown>[] : [];
  return payments[0] ?? {};
}

function normalizeOrderStatus(order: Record<string, unknown>) {
  const payment = firstPayment(order);
  return stringValue(payment.status || order.status).toUpperCase() || "CREATED";
}

function normalizePaymentStatus(payment: Record<string, unknown>) {
  return stringValue(payment.status).toUpperCase() || "CREATED";
}

function isPaid(status: string, payment: Record<string, unknown>) {
  const values = [status, stringValue(payment.status)].map((item) => item.toUpperCase());
  return values.some((item) => item === "PROCESSED" || item === "APPROVED" || item === "PAID");
}

function isWebPaymentPaid(payment: Record<string, unknown>) {
  const status = normalizePaymentStatus(payment);
  const detail = stringValue(payment.status_detail).toUpperCase();
  return status === "APPROVED" || detail === "ACCREDITED";
}

function paymentTransactionData(payment: Record<string, unknown>) {
  const point = payment.point_of_interaction as Record<string, unknown> | undefined;
  return (point?.transaction_data as Record<string, unknown> | undefined) ?? {};
}

function buildPagBankItems(value: unknown, amount: number, description: string, localReference: string): PagBankChargeItem[] {
  const items = normalizeChargeItems(value);
  const itemTotal = roundMoney(items.reduce((sum, item) => sum + item.unit_price * item.quantity, 0));
  if (items.length > 0 && Math.abs(itemTotal - amount) <= 0.01) {
    return items.map((item) => ({
      reference_id: item.id,
      name: clipText(item.title, 100),
      quantity: item.quantity,
      unit_amount: moneyCents(item.unit_price),
    }));
  }

  return [{
    reference_id: sanitizeItemId(localReference),
    name: clipText(description, 100),
    quantity: 1,
    unit_amount: moneyCents(amount),
  }];
}

function buildPagBankCustomer(payload: WebChargePayload, localReference: string) {
  const name = safePayerName(payload.payerName);
  const email = safePayerEmail(payload.payerEmail, localReference);
  const taxId = onlyDigits(payload.payerTaxId);
  const phone = onlyDigits(payload.payerPhone);
  const phones = phone.length >= 10
    ? [{ country: "55", area: phone.slice(0, 2), number: phone.slice(2).slice(0, 9), type: "MOBILE" }]
    : [];
  return compactObject({
    name,
    email,
    tax_id: taxId.length === 11 || taxId.length === 14 ? taxId : undefined,
    phones,
  });
}

function firstRecord(value: unknown) {
  if (Array.isArray(value) && value[0] && typeof value[0] === "object") {
    return value[0] as Record<string, unknown>;
  }

  return {};
}

function findPagBankLink(records: Record<string, unknown>[], preferredRels: string[]) {
  const links: Record<string, unknown>[] = [];
  for (const record of records) {
    if (Array.isArray(record.links)) {
      links.push(...record.links.filter((item): item is Record<string, unknown> => !!item && typeof item === "object"));
    }
  }

  const upperPrefs = preferredRels.map((item) => item.toUpperCase());
  const match = links.find((link) => upperPrefs.includes(stringValue(link.rel).toUpperCase()))
    || links.find((link) => stringValue(link.href));
  return stringValue(match?.href);
}

function normalizePagBankStatus(value: Record<string, unknown>) {
  const charge = firstRecord(value.charges);
  return stringValue(charge.status || value.status || value.payment_status).toUpperCase() || "CREATED";
}

function isPagBankPaid(status: string, value: Record<string, unknown>) {
  const charge = firstRecord(value.charges);
  const candidates = [
    status,
    stringValue(value.status),
    stringValue(value.payment_status),
    stringValue(charge.status),
  ].map((item) => item.toUpperCase());
  return candidates.some((item) => item === "PAID" || item === "APPROVED" || item === "AUTHORIZED" || item === "COMPLETED");
}

function extractPagBankWebhookId(payload: Record<string, unknown>, url: URL, names: string[]) {
  const data = payload.data as Record<string, unknown> | undefined;
  const candidates = [
    data?.id,
    data?.code,
    payload.id,
    payload.code,
    payload.order_id,
    payload.checkout_id,
    payload.charge_id,
    payload.payment_id,
    url.searchParams.get("id"),
    url.searchParams.get("code"),
  ].map(stringValue);
  for (const candidate of candidates) {
    if (candidate) return candidate;
  }

  const resource = stringValue(payload.resource || payload.resource_url || url.searchParams.get("resource"));
  if (!resource) return "";
  const pattern = new RegExp(`/(?:${names.map((name) => name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|")})/([^/?#]+)`, "i");
  return resource.match(pattern)?.[1] ?? "";
}

function extractWebhookPaymentId(payload: Record<string, unknown>, url: URL) {
  const data = payload.data as Record<string, unknown> | undefined;
  const candidates = [
    data?.id,
    payload.id,
    payload["data.id"],
    url.searchParams.get("data.id"),
    url.searchParams.get("id"),
  ].map(stringValue);
  for (const candidate of candidates) {
    if (candidate) return candidate;
  }

  const resource = stringValue(payload.resource || url.searchParams.get("resource"));
  const match = resource.match(/\/payments\/([^/?#]+)/i);
  return match?.[1] ?? "";
}

function extractWebhookOrderId(payload: Record<string, unknown>, url: URL, eventText: string) {
  const resource = stringValue(payload.resource || url.searchParams.get("resource"));
  const resourceMatch = resource.match(/\/orders\/([^/?#]+)/i);
  if (resourceMatch?.[1]) return resourceMatch[1];

  if (!eventText.includes("order") && !eventText.includes("point")) {
    return "";
  }

  const data = payload.data as Record<string, unknown> | undefined;
  const candidates = [
    data?.id,
    payload.order_id,
    payload.orderId,
    payload.id,
    url.searchParams.get("data.id"),
    url.searchParams.get("order_id"),
    url.searchParams.get("id"),
  ].map(stringValue);
  for (const candidate of candidates) {
    if (candidate) return candidate;
  }

  return "";
}

function normalizePointPaymentMethod(value: unknown) {
  const method = stringValue(value).toUpperCase();
  if (method.includes("DEBIT")) return "debit_card";
  if (method.includes("CRED")) return "credit_card";
  return "";
}

function normalizeChargeItems(value: unknown): MercadoPagoChargeItem[] {
  if (!Array.isArray(value)) return [];

  return value
    .slice(0, 50)
    .map((raw, index) => {
      if (!raw || typeof raw !== "object") return null;
      const item = raw as ChargeItemPayload;
      const title = clipText(stringValue(item.title || item.name), 120);
      const unitPrice = roundMoney(item.unitPrice ?? item.unit_price ?? item.price);
      const quantity = Math.max(1, Math.round(numberValue(item.quantity) || 1));
      if (!title || unitPrice <= 0) return null;

      return {
        id: sanitizeItemId(item.code || item.id || `ITEM-${index + 1}`),
        title,
        description: clipText(stringValue(item.description), 256),
        quantity,
        unit_price: unitPrice,
      };
    })
    .filter((item): item is MercadoPagoChargeItem => item !== null);
}

function buildPreferenceItems(items: MercadoPagoChargeItem[], amount: number, description: string, localReference: string) {
  const itemTotal = roundMoney(items.reduce((sum, item) => sum + item.unit_price * item.quantity, 0));
  if (items.length > 0 && Math.abs(itemTotal - amount) <= 0.01) {
    return items.map((item) => ({
      id: item.id,
      title: item.title,
      description: item.description,
      quantity: item.quantity,
      currency_id: "BRL",
      unit_price: item.unit_price,
    }));
  }

  return [{
    id: localReference,
    title: clipText(description, 120),
    quantity: 1,
    currency_id: "BRL",
    unit_price: amount,
  }];
}

function buildPreferenceAdditionalInfo(description: string, items: MercadoPagoChargeItem[]) {
  const itemText = items
    .map((item) => `${item.quantity}x ${item.title}`)
    .join("; ");
  return clipText(itemText ? `${description} | ${itemText}` : description, 600);
}

function sanitizeItemId(value: unknown) {
  const clean = stringValue(value).replace(/[^A-Za-z0-9_-]+/g, "-").replace(/^-+|-+$/g, "");
  return (clean || crypto.randomUUID()).slice(0, 64);
}

function clipText(value: unknown, maxLength: number) {
  const clean = stringValue(value).replace(/\s+/g, " ").trim();
  return clean.length <= maxLength ? clean : clean.slice(0, maxLength).trim();
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

async function readJson<T>(req: Request): Promise<T> {
  try {
    return await req.json() as T;
  } catch {
    return {} as T;
  }
}

function routeFromPath(pathname: string) {
  const marker = "/payments";
  const index = pathname.indexOf(marker);
  const route = index < 0 ? pathname : pathname.slice(index + marker.length) || "/";
  return route.endsWith("/") && route.length > 1 ? route.slice(0, -1) : route;
}

function normalizePayloadKeys<T>(value: T): T {
  if (Array.isArray(value)) return value.map((item) => normalizePayloadKeys(item)) as T;
  if (!value || typeof value !== "object") return value;
  const normalized: Record<string, unknown> = {};
  for (const [key, raw] of Object.entries(value as Record<string, unknown>)) {
    normalized[key ? key[0].toLowerCase() + key.slice(1) : key] = normalizePayloadKeys(raw);
  }
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
    const clean = value
      .trim()
      .replace(/[^\d,.-]/g, "")
      .replace(/\.(?=\d{3}(?:\D|$))/g, "")
      .replace(",", ".");
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

function parseJsonObject(text: string): Record<string, unknown> {
  if (!text) return {};
  try {
    const parsed = JSON.parse(text);
    return parsed && typeof parsed === "object" ? parsed as Record<string, unknown> : {};
  } catch {
    return {};
  }
}

function mercadoPagoErrorMessage(data: Record<string, unknown>, rawText: string, status: number) {
  const cause = Array.isArray(data.cause)
    ? data.cause
        .map((item) => {
          if (!item || typeof item !== "object") return stringValue(item);
          const causeItem = item as Record<string, unknown>;
          return stringValue(causeItem.description || causeItem.message || causeItem.code);
        })
        .filter(Boolean)
        .join(" | ")
    : "";
  return stringValue(data.message || data.error || cause || rawText || status);
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
      if (filtered.length === 0) continue;
      clean[key] = filtered;
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
  return new Response(JSON.stringify(payload), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}

function html(title: string, message: string, ok: boolean) {
  return providerHtml("mercadopago", title, message, ok);
}

function providerHtml(provider: string, title: string, message: string, ok: boolean) {
  const params = new URLSearchParams({
    [provider]: ok ? "connected" : "error",
    title: stringValue(title),
    message: stringValue(message),
  });

  return new Response(null, {
    status: 302,
    headers: {
      ...corsHeaders,
      "location": `https://balcaolivrepdv.com.br/?${params.toString()}`,
      "cache-control": "no-store",
    },
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
