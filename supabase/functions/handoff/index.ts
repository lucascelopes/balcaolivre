import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

type HandoffRow = {
  id: string;
  account_id: string;
  store_id: string;
  seat_id: string | null;
  purpose: string;
  target: string | null;
  expires_at: string;
  consumed_at: string | null;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (req.method !== "POST") return json({ ok: false, message: "Método não permitido." }, 405);

  try {
    const body = recordValue(await req.json().catch(() => ({})));
    const action = stringValue(body.action).toLowerCase();
    if (action === "claim_account") return await claimAccount(req, body);
    if (action === "activate_device") return await activateDevice(req, body);
    if (action === "activate_account_device") return await activateAccountDevice(req, body);
    if (action === "checkin") return await checkinDevice(req, body);
    if (action === "sync_device") return await syncDevice(req, body);
    if (action === "save_onboarding") return await saveOnboarding(req, body);
    if (action === "list_devices") return await listDevices(req, body);
    if (action === "create_extra_seat_invite") return await createExtraSeatInvite(req, body);
    if (action === "rename_device") return await renameDevice(req, body);
    if (action === "revoke_device") return await revokeDevice(req, body);
    return json({ ok: false, message: "Ação inválida." }, 400);
  } catch (error) {
    return json({ ok: false, message: messageFromError(error) }, 500);
  }
});

async function claimAccount(req: Request, body: Record<string, unknown>) {
  const user = await authenticatedUser(req);
  if (!user) return json({ ok: false, message: "Entre ou crie sua conta para continuar." }, 401);
  const handoff = await consumeCandidate(body.token, ["CHECKOUT_CLAIM"]);

  const client = serviceClient();
  const { data: account, error: accountError } = await client.from("bl_accounts")
    .select("id,owner_user_id")
    .eq("id", handoff.account_id)
    .single();
  if (accountError) throw new Error(`Falha ao localizar a conta: ${accountError.message}`);
  if (account.owner_user_id && account.owner_user_id !== user.id) {
    return json({ ok: false, message: "Esta compra já foi vinculada a outra conta." }, 409);
  }

  await client.from("bl_accounts").update({
    owner_user_id: user.id,
    email: user.email || null,
    status: "ACTIVE",
    updated_at: new Date().toISOString(),
  }).eq("id", handoff.account_id).throwOnError();
  await client.from("bl_store_members").upsert({
    store_id: handoff.store_id,
    user_id: user.id,
    role: "OWNER",
    status: "ACTIVE",
    updated_at: new Date().toISOString(),
  }, { onConflict: "store_id,user_id" }).throwOnError();
  return json({
    ok: true,
    accountId: handoff.account_id,
    storeId: handoff.store_id,
    next: "onboarding",
  });
}

async function activateDevice(req: Request, body: Record<string, unknown>) {
  const deviceKind = normalizeDeviceKind(body.deviceKind || body.device_kind);
  const installationId = stringValue(body.installationId || body.installation_id);
  if (installationId.length < 16) {
    return json({ ok: false, message: "Identificador seguro do dispositivo ausente." }, 400);
  }
  const handoff = await consumeCandidate(body.token, ["WEB_SIGN_IN", "WINDOWS_ACTIVATION", "MOBILE_ACTIVATION", "EXTRA_SEAT_INVITE"]);

  const seatKind = deviceKind === "MOBILE" ? "MOBILE" : "DESKTOP";
  const client = serviceClient();
  const { data: subscription, error: subscriptionError } = await client.from("bl_subscriptions")
    .select("status,current_period_end")
    .eq("store_id", handoff.store_id)
    .order("created_at", { ascending: false })
    .limit(1)
    .maybeSingle();
  if (subscriptionError) throw new Error(`Falha ao validar assinatura: ${subscriptionError.message}`);
  if (!subscription || !["ACTIVE", "TRIALING"].includes(subscription.status)
      || (subscription.current_period_end && Date.parse(subscription.current_period_end) <= Date.now())) {
    return json({ ok: false, code: "SUBSCRIPTION_INACTIVE", message: "A assinatura não está ativa." }, 403);
  }

  let seatId = handoff.seat_id;
  if (seatId) {
    const { data: linkedSeat } = await client.from("bl_device_seats")
      .select("id,seat_kind,status")
      .eq("id", seatId)
      .eq("store_id", handoff.store_id)
      .maybeSingle();
    if (!linkedSeat || linkedSeat.seat_kind !== seatKind || linkedSeat.status === "REVOKED") seatId = null;
  }
  if (!seatId) {
    const { data: availableSeat } = await client.from("bl_device_seats")
      .select("id")
      .eq("store_id", handoff.store_id)
      .eq("seat_kind", seatKind)
      .eq("status", "AVAILABLE")
      .order("ordinal", { ascending: true })
      .limit(1)
      .maybeSingle();
    seatId = availableSeat?.id || null;
  }
  if (!seatId) {
    return json({
      ok: false,
      code: seatKind === "MOBILE" ? "MOBILE_LIMIT_REACHED" : "DESKTOP_SEAT_REQUIRED",
      message: seatKind === "MOBILE"
        ? "O smartphone incluso já está vinculado. Revogue o aparelho anterior para trocar."
        : "Todos os caixas estão em uso. Adicione outro caixa por R$ 39,90/mês.",
    }, 409);
  }

  const installationHash = await sha256Hex(installationId);
  const { data: sameDevice } = await client.from("bl_devices")
    .select("id,seat_id,status")
    .eq("store_id", handoff.store_id)
    .eq("installation_id_hash", installationHash)
    .maybeSingle();
  if (sameDevice && sameDevice.status === "ACTIVE") {
    const lease = await issueLease(sameDevice.id, req);
    await attachConsumedDevice(handoff.id, sameDevice.id);
    return await activatedDeviceResponse(sameDevice.id, handoff.store_id, lease);
  }

  const { data: occupyingDevice } = await client.from("bl_devices")
    .select("id,display_name,device_kind")
    .eq("seat_id", seatId)
    .in("status", ["PENDING", "ACTIVE"])
    .limit(1)
    .maybeSingle();
  if (occupyingDevice) {
    return json({
      ok: false,
      code: "SEAT_IN_USE",
      message: "Esta vaga já está vinculada a outro dispositivo. Revogue-o antes de substituir.",
      device: {
        id: occupyingDevice.id,
        name: occupyingDevice.display_name,
        kind: occupyingDevice.device_kind,
      },
    }, 409);
  }

  const now = new Date().toISOString();
  const { data: device, error } = await client.from("bl_devices").upsert({
    store_id: handoff.store_id,
    seat_id: seatId,
    device_kind: deviceKind,
    installation_id_hash: installationHash,
    display_name: stringValue(body.displayName || body.display_name) || defaultDeviceName(deviceKind),
    platform: stringValue(body.platform) || null,
    app_version: stringValue(body.appVersion || body.app_version) || null,
    public_key: stringValue(body.publicKey || body.public_key) || null,
    status: "ACTIVE",
    activated_at: now,
    last_seen_at: now,
    last_seen_ip: requestIp(req),
    updated_at: now,
  }, { onConflict: "store_id,installation_id_hash" }).select("id").single();
  if (error) throw new Error(`Falha ao ativar o dispositivo: ${error.message}`);

  await client.from("bl_device_seats").update({
    status: "ASSIGNED",
    updated_at: now,
  }).eq("id", seatId).throwOnError();
  await attachConsumedDevice(handoff.id, device.id);
  const lease = await issueLease(device.id, req);
  return await activatedDeviceResponse(device.id, handoff.store_id, lease);
}

async function activateAccountDevice(req: Request, body: Record<string, unknown>) {
  const user = await authenticatedUser(req);
  if (!user) return json({ ok: false, message: "Entre na conta para liberar este dispositivo." }, 401);
  const requestedKind = normalizeDeviceKind(body.deviceKind || body.device_kind || "WEB");
  const purpose = requestedKind === "MOBILE"
    ? "MOBILE_ACTIVATION"
    : requestedKind === "WINDOWS"
    ? "WINDOWS_ACTIVATION"
    : "WEB_SIGN_IN";

  const client = serviceClient();
  const { data: memberships, error: memberError } = await client.from("bl_store_members")
    .select("store_id,role,status")
    .eq("user_id", user.id)
    .eq("status", "ACTIVE")
    .in("role", ["OWNER", "MANAGER"])
    .limit(2);
  if (memberError) throw new Error(`Falha ao localizar a conta: ${memberError.message}`);
  if (!memberships || memberships.length !== 1) {
    return json({
      ok: false,
      message: memberships?.length
        ? "Escolha a loja antes de liberar este dispositivo."
        : "Esta conta ainda não possui uma compra ativa do Balcão Livre.",
    }, 403);
  }

  const storeId = memberships[0].store_id;
  const { data: store, error: storeError } = await client.from("bl_stores")
    .select("id,account_id")
    .eq("id", storeId)
    .single();
  if (storeError) throw new Error(`Falha ao localizar a loja: ${storeError.message}`);

  const raw = randomToken();
  await client.from("bl_handoff_tokens").insert({
    store_id: store.id,
    account_id: store.account_id,
    token_hash: await sha256Hex(raw),
    purpose,
    target: requestedKind.toLowerCase(),
    expires_at: new Date(Date.now() + 5 * 60_000).toISOString(),
  }).throwOnError();

  return await activateDevice(req, {
    ...body,
    token: raw,
    deviceKind: requestedKind,
  });
}

async function checkinDevice(req: Request, body: Record<string, unknown>) {
  const rawLease = stringValue(body.leaseToken || body.lease_token);
  if (!rawLease) return json({ ok: false, message: "Sessão do dispositivo ausente." }, 401);
  const hash = await sha256Hex(rawLease);
  const client = serviceClient();
  const { data: lease, error } = await client.from("bl_device_leases")
    .select("id,device_id,expires_at,revoked_at")
    .eq("token_hash", hash)
    .maybeSingle();
  if (error) throw new Error(`Falha ao validar dispositivo: ${error.message}`);
  if (!lease || lease.revoked_at || Date.parse(lease.expires_at) <= Date.now()) {
    return json({ ok: false, code: "DEVICE_SESSION_EXPIRED", message: "Sessão do dispositivo expirada." }, 401);
  }

  const { data: device } = await client.from("bl_devices")
    .select("id,store_id,status")
    .eq("id", lease.device_id)
    .maybeSingle();
  if (!device || device.status !== "ACTIVE") {
    return json({ ok: false, code: "DEVICE_REVOKED", message: "Este dispositivo foi revogado." }, 403);
  }

  const now = new Date().toISOString();
  await client.from("bl_device_leases").update({
    last_seen_at: now,
    last_seen_ip: requestIp(req),
  }).eq("id", lease.id).throwOnError();
  await client.from("bl_devices").update({
    last_seen_at: now,
    last_seen_ip: requestIp(req),
    app_version: stringValue(body.appVersion || body.app_version) || null,
    updated_at: now,
  }).eq("id", device.id).throwOnError();

  return await activatedDeviceResponse(device.id, device.store_id, "");
}

async function saveOnboarding(req: Request, body: Record<string, unknown>) {
  const authorization = await authorizedDevice(req, body);
  if (!authorization.ok) return authorization.response;
  const { client, device } = authorization;

  const [{ data: entitlements, error: entitlementError }, { data: subscription, error: subscriptionError }] =
    await Promise.all([
      client.from("bl_entitlements")
        .select("subscription_id,modules,desktop_seat_limit,mercadopago_point_enabled")
        .eq("store_id", device.store_id)
        .single(),
      client.from("bl_subscriptions")
        .select("status,current_period_end")
        .eq("store_id", device.store_id)
        .order("created_at", { ascending: false })
        .limit(1)
        .maybeSingle(),
    ]);
  if (entitlementError) throw new Error(`Falha ao validar direitos do plano: ${entitlementError.message}`);
  if (subscriptionError) throw new Error(`Falha ao validar assinatura: ${subscriptionError.message}`);
  if (!subscription || !["ACTIVE", "TRIALING"].includes(subscription.status)
      || (subscription.current_period_end && Date.parse(subscription.current_period_end) <= Date.now())) {
    return json({ ok: false, code: "SUBSCRIPTION_INACTIVE", message: "A assinatura não está ativa." }, 403);
  }

  const restaurant = recordValue(body.restaurant);
  const serviceMode = recordValue(body.serviceMode || body.service_mode);
  const floorPlan = recordValue(body.floorPlan || body.floor_plan);
  const cashSetup = recordValue(body.cashSetup || body.cash_setup);
  const paymentMethods = recordValue(body.paymentMethods || body.payment_methods);
  const review = recordValue(body.review);
  const modules = stringArray(entitlements.modules).map((value) => value.toUpperCase());
  const selectedServiceModes = stringArray(serviceMode.modes).map((value) => value.toUpperCase());
  const selectedPayments = stringArray(paymentMethods.methods).map((value) => value.toUpperCase());
  const cashRegisterCount = Math.trunc(Number(cashSetup.registerCount || cashSetup.register_count || 1));
  const tableCount = Math.trunc(Number(floorPlan.tableCount || floorPlan.table_count || 1));
  const establishmentType = stringValue(restaurant.establishmentType || restaurant.establishment_type);
  const logoDataUrl = stringValue(restaurant.logoDataUrl || restaurant.logo_data_url);
  const allowedPayments = ["DINHEIRO", "PIX", "CREDITO", "DEBITO", "VALE", "FIADO"];
  const allowedEstablishmentTypes = ["Restaurante", "Bar", "Lanchonete", "Pizzaria", "Cafeteria", "Padaria", "Outro"];

  if (!stringValue(restaurant.businessName || restaurant.business_name)) {
    return json({ ok: false, message: "Informe o nome do estabelecimento." }, 400);
  }
  if (!allowedEstablishmentTypes.includes(establishmentType)) {
    return json({ ok: false, message: "Selecione um tipo de estabelecimento válido." }, 400);
  }
  if (logoDataUrl && (!/^data:image\/(png|jpeg);base64,/i.test(logoDataUrl) || logoDataUrl.length > 1_400_000)) {
    return json({ ok: false, message: "A logo deve ser PNG ou JPEG e ter até 1 MB." }, 400);
  }
  if (selectedServiceModes.length === 0 || selectedServiceModes.some((mode) => !["SALAO", "BALCAO", "DELIVERY"].includes(mode))) {
    return json({ ok: false, message: "Selecione ao menos um modo de atendimento válido." }, 400);
  }
  if (selectedServiceModes.some((mode) => !modules.includes(mode))) {
    return json({ ok: false, code: "MODULE_NOT_INCLUDED", message: "O plano atual não inclui todos os modos escolhidos." }, 403);
  }
  if (!Number.isInteger(tableCount) || tableCount < 1 || tableCount > 200) {
    return json({ ok: false, message: "Quantidade de mesas ou comandas inválida." }, 400);
  }
  if (!Number.isInteger(cashRegisterCount) || cashRegisterCount < 1 || cashRegisterCount > Number(entitlements.desktop_seat_limit)) {
    return json({
      ok: false,
      code: "DESKTOP_SEAT_REQUIRED",
      message: `O plano permite ${entitlements.desktop_seat_limit} caixa(s). Adicione outro caixa antes de continuar.`,
    }, 403);
  }
  if (selectedPayments.length === 0 || selectedPayments.some((method) => !allowedPayments.includes(method))) {
    return json({ ok: false, message: "Selecione ao menos uma forma de pagamento válida." }, 400);
  }
  if (Boolean(paymentMethods.mercadopagoPointEnabled || paymentMethods.mercadopago_point_enabled)
      && !entitlements.mercadopago_point_enabled) {
    return json({ ok: false, code: "POINT_NOT_INCLUDED", message: "Mercado Pago Point não está incluído neste plano." }, 403);
  }

  const now = new Date().toISOString();
  await client.from("bl_onboarding_configs").upsert({
    store_id: device.store_id,
    current_step: 6,
    restaurant: { ...restaurant, establishmentType, logoDataUrl },
    service_mode: { ...serviceMode, modes: selectedServiceModes },
    floor_plan: { ...floorPlan, tableCount },
    cash_setup: { ...cashSetup, registerCount: cashRegisterCount },
    payment_methods: { ...paymentMethods, methods: selectedPayments },
    review,
    completed_at: now,
    updated_at: now,
  }, { onConflict: "store_id" }).throwOnError();
  await client.from("bl_stores").update({
    name: stringValue(restaurant.businessName || restaurant.business_name),
    onboarding_status: "COMPLETE",
    updated_at: now,
  }).eq("id", device.store_id).throwOnError();
  await syncCashRegisters(client, device.store_id, cashRegisterCount, now);

  return json({
    ok: true,
    storeId: device.store_id,
    onboardingStatus: "COMPLETE",
    entitlements,
  });
}

async function syncDevice(req: Request, body: Record<string, unknown>) {
  const authorization = await authorizedDevice(req, body);
  if (!authorization.ok) return authorization.response;
  const { client, device } = authorization;
  const rawEvents = Array.isArray(body.events) ? body.events.slice(0, 100) : [];
  const events = rawEvents.map(recordValue).map((event) => ({
    device_id: device.id,
    store_id: device.store_id,
    event_id: stringValue(event.id).slice(0, 120),
    event_type: stringValue(event.type).slice(0, 120) || "mobile.event",
    payload: recordValue(event.payload),
    client_created_at: validDateOrNull(event.createdAt || event.created_at),
  })).filter((event) => event.event_id.length >= 8);

  if (events.length) {
    await client.from("bl_device_sync_events").upsert(events, {
      onConflict: "device_id,event_id",
      ignoreDuplicates: true,
    }).throwOnError();
  }
  const snapshot = recordValue(body.snapshot);
  if (Object.keys(snapshot).length) {
    await client.from("bl_device_snapshots").upsert({
      device_id: device.id,
      store_id: device.store_id,
      snapshot,
      client_updated_at: validDateOrNull(body.clientUpdatedAt || body.client_updated_at),
      updated_at: new Date().toISOString(),
    }, { onConflict: "device_id" }).throwOnError();
  }
  return json({ ok: true, accepted: events.length, deviceId: device.id, storeId: device.store_id });
}

async function authorizedDevice(req: Request, body: Record<string, unknown>) {
  const rawLease = stringValue(body.leaseToken || body.lease_token);
  if (!rawLease) {
    return { ok: false as const, response: json({ ok: false, message: "Sessão do dispositivo ausente." }, 401) };
  }
  const client = serviceClient();
  const { data: lease, error: leaseError } = await client.from("bl_device_leases")
    .select("id,device_id,expires_at,revoked_at")
    .eq("token_hash", await sha256Hex(rawLease))
    .maybeSingle();
  if (leaseError) throw new Error(`Falha ao validar dispositivo: ${leaseError.message}`);
  if (!lease || lease.revoked_at || Date.parse(lease.expires_at) <= Date.now()) {
    return {
      ok: false as const,
      response: json({ ok: false, code: "DEVICE_SESSION_EXPIRED", message: "Sessão do dispositivo expirada." }, 401),
    };
  }
  const { data: device, error: deviceError } = await client.from("bl_devices")
    .select("id,store_id,status")
    .eq("id", lease.device_id)
    .maybeSingle();
  if (deviceError) throw new Error(`Falha ao localizar dispositivo: ${deviceError.message}`);
  if (!device || device.status !== "ACTIVE") {
    return {
      ok: false as const,
      response: json({ ok: false, code: "DEVICE_REVOKED", message: "Este dispositivo foi revogado." }, 403),
    };
  }
  return { ok: true as const, client, lease, device };
}

async function syncCashRegisters(
  client: ReturnType<typeof serviceClient>,
  storeId: string,
  desiredCount: number,
  now: string,
) {
  const { data: current, error } = await client.from("bl_cash_registers")
    .select("id,status,created_at")
    .eq("store_id", storeId)
    .order("created_at", { ascending: true });
  if (error) throw new Error(`Falha ao consultar caixas: ${error.message}`);

  const rows = current || [];
  for (let index = 0; index < desiredCount; index += 1) {
    const existing = rows[index];
    if (existing) {
      await client.from("bl_cash_registers").update({
        name: `Caixa ${index + 1}`,
        status: "ACTIVE",
        updated_at: now,
      }).eq("id", existing.id).throwOnError();
    } else {
      await client.from("bl_cash_registers").insert({
        store_id: storeId,
        name: `Caixa ${index + 1}`,
        status: "ACTIVE",
        updated_at: now,
      }).throwOnError();
    }
  }
  for (const extra of rows.slice(desiredCount)) {
    await client.from("bl_cash_registers").update({
      status: "ARCHIVED",
      updated_at: now,
    }).eq("id", extra.id).throwOnError();
  }
}

async function revokeDevice(req: Request, body: Record<string, unknown>) {
  const user = await authenticatedUser(req);
  if (!user) return json({ ok: false, message: "Autenticação obrigatória." }, 401);
  const deviceId = stringValue(body.deviceId || body.device_id);
  const client = serviceClient();
  const { data: device } = await client.from("bl_devices")
    .select("id,store_id,seat_id,status")
    .eq("id", deviceId)
    .maybeSingle();
  if (!device) return json({ ok: false, message: "Dispositivo não encontrado." }, 404);

  const { data: member } = await client.from("bl_store_members")
    .select("role,status")
    .eq("store_id", device.store_id)
    .eq("user_id", user.id)
    .maybeSingle();
  if (!member || member.status !== "ACTIVE" || !["OWNER", "MANAGER"].includes(member.role)) {
    return json({ ok: false, message: "Somente proprietário ou gerente pode trocar dispositivos." }, 403);
  }

  const now = new Date().toISOString();
  await client.from("bl_devices").update({
    status: "REVOKED",
    revoked_at: now,
    updated_at: now,
  }).eq("id", device.id).throwOnError();
  await client.from("bl_device_leases").update({ revoked_at: now }).eq("device_id", device.id).throwOnError();
  await client.from("bl_device_seats").update({
    status: "AVAILABLE",
    updated_at: now,
  }).eq("id", device.seat_id).throwOnError();
  return json({ ok: true, deviceId: device.id });
}

async function renameDevice(req: Request, body: Record<string, unknown>) {
  const deviceId = stringValue(body.deviceId || body.device_id);
  const displayName = stringValue(body.displayName || body.display_name).slice(0, 80);
  if (!deviceId || displayName.length < 2) {
    return json({ ok: false, message: "Informe o dispositivo e um nome válido." }, 400);
  }
  const client = serviceClient();
  const { data: device, error } = await client.from("bl_devices")
    .select("id,store_id")
    .eq("id", deviceId)
    .maybeSingle();
  if (error) throw new Error(`Falha ao localizar o dispositivo: ${error.message}`);
  if (!device) return json({ ok: false, message: "Dispositivo não encontrado." }, 404);
  const access = await ownerOrManagerStore(req, { storeId: device.store_id });
  if (!access.ok) return access.response;
  await access.client.from("bl_devices").update({
    display_name: displayName,
    updated_at: new Date().toISOString(),
  }).eq("id", device.id).throwOnError();
  return json({ ok: true, deviceId: device.id, displayName });
}

async function listDevices(req: Request, body: Record<string, unknown>) {
  const access = await ownerOrManagerStore(req, body);
  if (!access.ok) return access.response;
  const { data: entitlements, error: entitlementError } = await access.client.from("bl_entitlements")
    .select("plan_code,desktop_seat_limit,mobile_seat_limit,expires_at")
    .eq("store_id", access.storeId)
    .single();
  if (entitlementError) throw new Error(`Falha ao consultar limites: ${entitlementError.message}`);

  const { data: seats, error } = await access.client.from("bl_device_seats")
    .select("id,seat_kind,source,ordinal,status,bl_devices(id,device_kind,display_name,status,activated_at,last_seen_at,platform,app_version)")
    .eq("store_id", access.storeId)
    .order("seat_kind", { ascending: true })
    .order("ordinal", { ascending: true });
  if (error) throw new Error(`Falha ao consultar dispositivos: ${error.message}`);
  return json({ ok: true, storeId: access.storeId, entitlements, seats: seats || [] });
}

async function createExtraSeatInvite(req: Request, body: Record<string, unknown>) {
  const access = await ownerOrManagerStore(req, body);
  if (!access.ok) return access.response;
  const target = stringValue(body.target).toLowerCase() === "web" ? "web" : "windows";

  const { data: seat, error: seatError } = await access.client.from("bl_device_seats")
    .select("id,ordinal,source,status")
    .eq("store_id", access.storeId)
    .eq("seat_kind", "DESKTOP")
    .eq("source", "EXTRA_SUBSCRIPTION")
    .eq("status", "AVAILABLE")
    .order("ordinal", { ascending: true })
    .limit(1)
    .maybeSingle();
  if (seatError) throw new Error(`Falha ao localizar o computador adicional: ${seatError.message}`);
  if (!seat) {
    return json({
      ok: false,
      code: "EXTRA_DESKTOP_SEAT_REQUIRED",
      message: "Compre um computador adicional ou remova um dispositivo antigo antes de gerar o link.",
    }, 409);
  }

  const { data: store, error: storeError } = await access.client.from("bl_stores")
    .select("account_id,name")
    .eq("id", access.storeId)
    .single();
  if (storeError) throw new Error(`Falha ao localizar a loja: ${storeError.message}`);

  const raw = randomToken();
  await access.client.from("bl_handoff_tokens").insert({
    store_id: access.storeId,
    account_id: store.account_id,
    seat_id: seat.id,
    token_hash: await sha256Hex(raw),
    purpose: "EXTRA_SEAT_INVITE",
    target,
    expires_at: new Date(Date.now() + 30 * 60_000).toISOString(),
  }).throwOnError();

  const webBase = stringValue(Deno.env.get("BALCAO_WEB_APP_URL")) || "https://app.balcaolivrepdv.com.br";
  const webUrl = new URL(webBase);
  webUrl.searchParams.set("handoff", raw);
  const windowsDeepLink = `balcaolivre://activate?token=${encodeURIComponent(raw)}`;
  const siteBase = stringValue(Deno.env.get("BALCAO_SITE_URL"))
    || stringValue(Deno.env.get("BALCAO_CHECKOUT_SUCCESS_URL"))
    || "https://www.balcaolivrepdv.com.br";
  const windowsActivationUrl = new URL(siteBase);
  windowsActivationUrl.searchParams.set("activate", "windows");
  windowsActivationUrl.searchParams.set("handoff", raw);
  const activationUrl = target === "web" ? webUrl.toString() : windowsActivationUrl.toString();
  return json({
    ok: true,
    seat: { id: seat.id, ordinal: seat.ordinal },
    target,
    expiresInMinutes: 30,
    activationUrl,
    shareUrl: activationUrl,
    webUrl: webUrl.toString(),
    windowsDeepLink,
    windowsInstallerUrl: stringValue(Deno.env.get("BALCAO_WINDOWS_INSTALLER_URL"))
      || "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.29.exe",
    shareText: `Abra este link em até 30 minutos para ativar o Caixa ${seat.ordinal} do ${store.name}: ${activationUrl}`,
  });
}

async function ownerOrManagerStore(req: Request, body: Record<string, unknown>) {
  const user = await authenticatedUser(req);
  if (!user) {
    return {
      ok: false as const,
      response: json({ ok: false, message: "Autenticação obrigatória." }, 401),
    };
  }
  const requestedStoreId = stringValue(body.storeId || body.store_id);
  let query = serviceClient().from("bl_store_members")
    .select("store_id,role,status")
    .eq("user_id", user.id)
    .eq("status", "ACTIVE")
    .in("role", ["OWNER", "MANAGER"]);
  if (requestedStoreId) query = query.eq("store_id", requestedStoreId);
  const { data: memberships, error } = await query.limit(2);
  if (error) throw new Error(`Falha ao validar acesso: ${error.message}`);
  if (!memberships || memberships.length !== 1) {
    return {
      ok: false as const,
      response: json({
        ok: false,
        message: memberships?.length
          ? "Escolha uma loja para continuar."
          : "Somente o proprietário ou gerente pode gerenciar dispositivos.",
      }, 403),
    };
  }
  return {
    ok: true as const,
    client: serviceClient(),
    storeId: memberships[0].store_id as string,
    user,
  };
}

async function consumeCandidate(token: unknown, purposes: string[]): Promise<HandoffRow> {
  const raw = stringValue(token);
  if (!raw) throw new Error("Link de acesso ausente.");
  const now = new Date().toISOString();
  const { data, error } = await serviceClient().from("bl_handoff_tokens")
    .update({ consumed_at: now })
    .select("id,account_id,store_id,seat_id,purpose,target,expires_at,consumed_at")
    .eq("token_hash", await sha256Hex(raw))
    .is("consumed_at", null)
    .gt("expires_at", now)
    .in("purpose", purposes)
    .maybeSingle();
  if (error) throw new Error(`Falha ao validar link: ${error.message}`);
  if (!data) {
    throw new Error("Este link expirou ou já foi usado.");
  }
  return data as HandoffRow;
}

async function attachConsumedDevice(id: string, deviceId: string) {
  await serviceClient().from("bl_handoff_tokens").update({
    consumed_by_device_id: deviceId,
  }).eq("id", id).not("consumed_at", "is", null).throwOnError();
}

async function issueLease(deviceId: string, req: Request) {
  const raw = randomToken();
  await serviceClient().from("bl_device_leases").insert({
    device_id: deviceId,
    token_hash: await sha256Hex(raw),
    expires_at: new Date(Date.now() + 30 * 24 * 60 * 60_000).toISOString(),
    last_seen_at: new Date().toISOString(),
    last_seen_ip: requestIp(req),
  }).throwOnError();
  return raw;
}

async function activatedDeviceResponse(deviceId: string, storeId: string, leaseToken: string) {
  const client = serviceClient();
  const [{ data: device }, { data: entitlements }, { data: onboarding }] = await Promise.all([
    client.from("bl_devices").select("id,device_kind,display_name,seat_id,status").eq("id", deviceId).single(),
    client.from("bl_entitlements").select("*").eq("store_id", storeId).single(),
    client.from("bl_onboarding_configs").select("*").eq("store_id", storeId).maybeSingle(),
  ]);
  return json({
    ok: true,
    device,
    storeId,
    ...(leaseToken ? { leaseToken } : {}),
    entitlements,
    onboarding,
  });
}

async function authenticatedUser(req: Request) {
  const authorization = stringValue(req.headers.get("authorization"));
  const token = authorization.toLowerCase().startsWith("bearer ") ? authorization.slice(7).trim() : "";
  if (!token) return null;
  const { data, error } = await serviceClient().auth.getUser(token);
  return error ? null : data.user;
}

function normalizeDeviceKind(value: unknown) {
  const kind = stringValue(value).toUpperCase();
  if (!["WINDOWS", "WEB", "MOBILE"].includes(kind)) throw new Error("Tipo de dispositivo inválido.");
  return kind as "WINDOWS" | "WEB" | "MOBILE";
}

function defaultDeviceName(kind: "WINDOWS" | "WEB" | "MOBILE") {
  if (kind === "WINDOWS") return "Caixa Windows";
  if (kind === "WEB") return "Caixa Web";
  return "Smartphone";
}

function requestIp(req: Request) {
  return stringValue(req.headers.get("cf-connecting-ip") || req.headers.get("x-forwarded-for")).split(",")[0] || null;
}

function randomToken() {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return Array.from(bytes).map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

async function sha256Hex(value: string) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(digest)).map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

function serviceClient() {
  return createClient(requiredEnv("SUPABASE_URL"), requiredEnv("SUPABASE_SERVICE_ROLE_KEY"), {
    auth: { persistSession: false },
  });
}

function requiredEnv(name: string) {
  const value = stringValue(Deno.env.get(name));
  if (!value) throw new Error(`${name} não configurado.`);
  return value;
}

function recordValue(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : {};
}

function stringArray(value: unknown) {
  return Array.isArray(value)
    ? value.map((item) => stringValue(item)).filter(Boolean)
    : [];
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function validDateOrNull(value: unknown) {
  const text = stringValue(value);
  return text && Number.isFinite(Date.parse(text)) ? new Date(text).toISOString() : null;
}

function json(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store" },
  });
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : "Erro inesperado.";
}
