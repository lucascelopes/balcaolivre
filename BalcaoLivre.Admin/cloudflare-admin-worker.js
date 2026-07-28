const JSON_HEADERS = {
  "content-type": "application/json; charset=utf-8",
  "cache-control": "no-store"
};

const ADMIN_BUCKET = "balcao-livre-admin";
const ADMIN_OBJECT = "admin-store.json";
const ACCESS_COOKIE = "__Host-bl_admin_access";
const REFRESH_COOKIE = "__Host-bl_admin_refresh";
const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (!url.pathname.startsWith("/admin-api/")) {
      return env.ASSETS.fetch(request);
    }

    const path = url.pathname.replace(/^\/admin-api/, "") || "/";
    try {
      if (path === "/health" && request.method === "GET") {
        if (!hasSupabaseBackend(env)) {
          return json({
            ok: false,
            app: "Balcao Livre PDV Admin",
            version: "1.2.2026",
            storage: "supabase-nao-configurado",
            runtime: "cloudflare"
          }, 503);
        }
        const store = await readAdminStore(env);
        return json({
          ok: true,
          app: "Balcao Livre PDV Admin",
          version: "1.2.2026",
          storage: "supabase",
          runtime: "cloudflare",
          records: {
            licenses: store.licenses.length,
            devices: store.devices.length,
            support: store.supportTickets.length
          }
        });
      }

      if (path === "/login" && request.method === "POST") {
        return login(request, env);
      }

      if (path === "/logout" && request.method === "POST") {
        return logout(request, env);
      }

      const auth = await authenticateAdmin(request, env);
      if (!auth.ok) {
        return json({ authenticated: false, message: "Sessao administrativa invalida." }, 401);
      }

      if (path === "/session" && request.method === "GET") {
        return withSession(json({
          authenticated: true,
          user: auth.email,
          profile: adminProfile(auth.user, auth.email, env)
        }), auth);
      }

      if (path === "/realtime" && request.method === "GET") {
        return withSession(new Response(
          `event: admin.ready\ndata: ${JSON.stringify({
            revision: Date.now(),
            storageMode: "supabase",
            usesSupabase: true,
            checkedAt: new Date().toISOString()
          })}\n\n`,
          {
            headers: {
              "content-type": "text/event-stream; charset=utf-8",
              "cache-control": "no-cache, no-transform"
            }
          }
        ), auth);
      }

  if (path === "/visits/plan" && request.method === "POST") {
    return withSession(await planVisits(request, env), auth);
  }

  if (path === "/visits/nearby" && request.method === "GET") {
    return withSession(await findNearbyVisits(request), auth);
  }

      const response = await handleAdminApi(path, request, env, auth);
      return withSession(response, auth);
    } catch (error) {
      return json({
        ok: false,
        message: cleanError(error)
      }, 500);
    }
  }
};

async function login(request, env) {
  if (!env.SUPABASE_URL || !env.SUPABASE_ANON_KEY) {
    return json({ message: "Supabase Auth nao configurado no Worker." }, 503);
  }

  let body;
  try {
    body = await request.json();
  } catch {
    return json({ message: "Dados de login invalidos." }, 400);
  }

  const email = String(body?.user || "").trim().toLowerCase();
  const password = String(body?.password || "");
  if (!email || !password) {
    return json({ message: "Informe email e senha." }, 400);
  }
  if (!adminEmails(env).has(email)) {
    return json({ message: "Este usuario nao tem acesso administrativo." }, 403);
  }

  const response = await fetch(`${supabaseUrl(env)}/auth/v1/token?grant_type=password`, {
    method: "POST",
    headers: {
      "apikey": env.SUPABASE_ANON_KEY,
      "content-type": "application/json"
    },
    body: JSON.stringify({ email, password })
  });
  const payload = await readResponseJson(response);
  if (!response.ok || !payload?.access_token || !payload?.refresh_token) {
    return json({ message: "Login ou senha invalidos." }, 403);
  }

  const authenticatedEmail = String(payload.user?.email || email).toLowerCase();
  if (!adminEmails(env).has(authenticatedEmail)) {
    return json({ message: "Este usuario nao tem acesso administrativo." }, 403);
  }

  const result = json({
    ok: true,
    user: authenticatedEmail,
    profile: adminProfile(payload.user, authenticatedEmail, env)
  });
  appendSessionCookies(result.headers, payload);
  return result;
}

async function logout(request, env) {
  const cookies = parseCookies(request.headers.get("cookie"));
  const accessToken = cookies[ACCESS_COOKIE];
  if (accessToken && env.SUPABASE_ANON_KEY) {
    await fetch(`${supabaseUrl(env)}/auth/v1/logout`, {
      method: "POST",
      headers: {
        "apikey": env.SUPABASE_ANON_KEY,
        "authorization": `Bearer ${accessToken}`
      }
    }).catch(() => {});
  }

  const response = json({ ok: true });
  clearSessionCookies(response.headers);
  return response;
}

async function authenticateAdmin(request, env) {
  if (!env.SUPABASE_URL || !env.SUPABASE_ANON_KEY) return { ok: false };
  const cookies = parseCookies(request.headers.get("cookie"));
  let accessToken = cookies[ACCESS_COOKIE] || "";
  const refreshToken = cookies[REFRESH_COOKIE] || "";

  let user = accessToken ? await supabaseUser(accessToken, env) : null;
  let refreshedSession = null;
  if (!user && refreshToken) {
    const refreshResponse = await fetch(`${supabaseUrl(env)}/auth/v1/token?grant_type=refresh_token`, {
      method: "POST",
      headers: {
        "apikey": env.SUPABASE_ANON_KEY,
        "content-type": "application/json"
      },
      body: JSON.stringify({ refresh_token: refreshToken })
    });
    if (refreshResponse.ok) {
      refreshedSession = await refreshResponse.json();
      accessToken = String(refreshedSession?.access_token || "");
      user = accessToken ? await supabaseUser(accessToken, env) : null;
    }
  }

  const email = String(user?.email || "").toLowerCase();
  if (!email || !adminEmails(env).has(email)) return { ok: false };
  return { ok: true, email, user, accessToken, refreshedSession };
}

async function supabaseUser(accessToken, env) {
  const response = await fetch(`${supabaseUrl(env)}/auth/v1/user`, {
    headers: {
      "apikey": env.SUPABASE_ANON_KEY,
      "authorization": `Bearer ${accessToken}`
    }
  });
  return response.ok ? response.json() : null;
}

async function handleAdminApi(path, request, env, auth) {
  if (!hasSupabaseBackend(env)) {
    return json({ message: "SUPABASE_SERVICE_ROLE_KEY nao configurado no Worker." }, 503);
  }

  if (request.method !== "GET" && !validMutationOrigin(request)) {
    return json({ message: "Origem da requisicao nao autorizada." }, 403);
  }

  if (path === "/dashboard" && request.method === "GET") {
    const [store, stripe] = await Promise.all([
      readAdminStore(env),
      readStripeCheckoutSummary(env)
    ]);
    return json(buildDashboard(store, stripe));
  }

  if (path === "/payments/manual" && request.method === "POST") {
    return saveManualPayment(request, env, auth);
  }

  if (path === "/client-intelligence" && request.method === "GET") {
    const store = await readAdminStore(env);
    return json(buildClientIntelligence(store));
  }

  if (path === "/licenses" && request.method === "GET") {
    const store = await readAdminStore(env);
    refreshLicenseStatuses(store);
    return json(sortByDate(store.licenses, "createdAt"));
  }

  if (path === "/licenses" && request.method === "POST") {
    return createLicense(request, env, auth);
  }

  const licenseAction = path.match(/^\/licenses\/([^/]+)\/(renew|block|unblock)$/);
  if (licenseAction && request.method === "POST") {
    return updateLicense(
      decodeURIComponent(licenseAction[1]),
      licenseAction[2],
      request,
      env,
      auth
    );
  }

  if (path === "/blocked-ips" && request.method === "GET") {
    const store = await readAdminStore(env);
    return json(sortByDate(store.blockedIps, "updatedAt"));
  }

  if (path === "/blocked-ips" && request.method === "POST") {
    return blockIp(request, env, auth);
  }

  if (path === "/blocked-ips/delete" && request.method === "POST") {
    return unblockIp(request, env, auth);
  }

  if (path === "/support" && request.method === "GET") {
    const store = await readAdminStore(env);
    return json(sortSupport(store.supportTickets));
  }

  const supportAction = path.match(/^\/support\/([^/]+)\/(status|reply)$/);
  if (supportAction && request.method === "POST") {
    return updateSupport(
      decodeURIComponent(supportAction[1]),
      supportAction[2],
      request,
      env,
      auth
    );
  }

  return json({ message: "Rota administrativa nao encontrada." }, 404);
}

async function createLicense(request, env, auth) {
  const body = await safeJson(request);
  const amount = clampNumber(body?.amount, 1, 3650, 30);
  const unit = normalizeDurationUnit(body?.unit);
  const now = new Date();
  const expiresAt = addDuration(now, amount, unit);
  const key = await createLicenseKey(expiresAt);
  const license = {
    id: crypto.randomUUID().replaceAll("-", ""),
    key,
    plan: cleanText(body?.plan, 100) || `${amount} ${durationLabel(unit, amount)}`,
    customerName: cleanText(body?.customerName, 160) || "Cliente sem nome",
    notes: cleanText(body?.notes, 500),
    status: "DISPONIVEL",
    createdAt: now.toISOString(),
    expiresAt: expiresAt.toISOString(),
    periodAmount: amount,
    periodUnit: unit
  };

  await supabaseRest(env, "/rest/v1/bv_licenses", {
    method: "POST",
    headers: { "prefer": "return=minimal" },
    body: JSON.stringify({
      key: license.key,
      status: license.status,
      plan: license.plan,
      customer_name: license.customerName,
      created_at: license.createdAt,
      expires_at: license.expiresAt,
      updated_at: license.createdAt
    })
  });

  const store = await readAdminStore(env);
  store.licenses.push(license);
  appendEvent(store, "license.created", `Chave criada para ${license.customerName}`, license.key, auth.email);
  await writeAdminStore(env, store);
  return json(license);
}

async function updateLicense(id, action, request, env, auth) {
  const store = await readAdminStore(env);
  refreshLicenseStatuses(store);
  const normalized = id.toUpperCase();
  const license = store.licenses.find((item) =>
    String(item.id || "").toUpperCase() === normalized ||
    String(item.key || "").toUpperCase() === normalized
  );
  if (!license) return json({ message: "Licenca nao encontrada." }, 404);

  const now = new Date();
  if (action === "renew") {
    const body = await safeJson(request);
    const amount = clampNumber(body?.amount, 1, 3650, 30);
    const unit = normalizeDurationUnit(body?.unit);
    const currentExpiry = new Date(license.expiresAt || 0);
    const base = currentExpiry > now ? currentExpiry : now;
    license.expiresAt = addDuration(base, amount, unit).toISOString();
    license.periodAmount = amount;
    license.periodUnit = unit;
    license.status = license.machineHash ? "ATIVA" : "DISPONIVEL";
    appendEvent(store, "license.renewed", `Licenca renovada: ${license.customerName || license.key}`, license.key, auth.email);
  } else if (action === "block") {
    license.status = "BLOQUEADA";
    appendEvent(store, "license.blocked", `Chave bloqueada: ${license.customerName || license.key}`, license.key, auth.email);
  } else {
    license.status = license.machineHash ? "ATIVA" : "DISPONIVEL";
    appendEvent(store, "license.unblocked", `Chave desbloqueada: ${license.customerName || license.key}`, license.key, auth.email);
  }

  await supabaseRest(
    env,
    `/rest/v1/bv_licenses?key=eq.${encodeURIComponent(license.key)}`,
    {
      method: "PATCH",
      headers: { "prefer": "return=minimal" },
      body: JSON.stringify({
        status: license.status,
        expires_at: license.expiresAt,
        updated_at: now.toISOString()
      })
    }
  );
  await writeAdminStore(env, store);
  return json(license);
}

async function blockIp(request, env, auth) {
  const body = await safeJson(request);
  const ip = normalizeIp(body?.ip);
  if (!ip) return json({ message: "IP obrigatorio para bloquear." }, 400);
  const store = await readAdminStore(env);
  const now = new Date().toISOString();
  let item = store.blockedIps.find((row) => normalizeIp(row.ip) === ip);
  if (!item) {
    item = { id: crypto.randomUUID().replaceAll("-", ""), ip, createdAt: now };
    store.blockedIps.push(item);
  }
  item.reason = cleanText(body?.reason, 300) || "Bloqueado pelo admin";
  item.source = cleanText(body?.source, 160) || "admin";
  item.updatedAt = now;
  appendEvent(store, "ip.blocked", `IP bloqueado: ${ip}`, "", auth.email);
  await writeAdminStore(env, store);
  return json(item);
}

async function unblockIp(request, env, auth) {
  const body = await safeJson(request);
  const ip = normalizeIp(body?.ip);
  if (!ip) return json({ message: "IP obrigatorio para liberar." }, 400);
  const store = await readAdminStore(env);
  const before = store.blockedIps.length;
  store.blockedIps = store.blockedIps.filter((item) => normalizeIp(item.ip) !== ip);
  if (store.blockedIps.length === before) return json({ message: "IP nao encontrado." }, 404);
  appendEvent(store, "ip.unblocked", `IP liberado: ${ip}`, "", auth.email);
  await writeAdminStore(env, store);
  return json({ ok: true, ip });
}

async function updateSupport(id, action, request, env, auth) {
  const body = await safeJson(request);
  const store = await readAdminStore(env);
  const normalized = id.toUpperCase();
  const ticket = store.supportTickets.find((item) =>
    String(item.id || "").toUpperCase() === normalized ||
    String(item.shortId || "").toUpperCase() === normalized
  );
  if (!ticket) return json({ message: "Chamado nao encontrado." }, 404);
  const now = new Date().toISOString();

  if (action === "status") {
    ticket.status = normalizeSupportStatus(body?.status);
    ticket.adminNote = cleanText(body?.note, 500);
    ticket.resolvedAt = ticket.status === "RESOLVIDO" ? now : null;
    appendEvent(store, "support.status", `Suporte ${ticket.shortId || ticket.id}: ${ticket.status}`, ticket.licenseKey, auth.email);
  } else {
    const message = cleanText(body?.message, 4000);
    if (!message) return json({ message: "Mensagem obrigatoria." }, 400);
    ticket.messages = Array.isArray(ticket.messages) ? ticket.messages : [];
    ticket.messages.push({
      id: crypto.randomUUID().replaceAll("-", ""),
      sender: "admin",
      message,
      when: now
    });
    ticket.status = "EM_ATENDIMENTO";
    ticket.adminNote = "";
    appendEvent(store, "support.reply", `Resposta enviada no suporte ${ticket.shortId || ticket.id}`, ticket.licenseKey, auth.email);
  }
  ticket.updatedAt = now;
  await writeAdminStore(env, store);
  return json(ticket);
}

async function saveManualPayment(request, env, auth) {
  const body = await safeJson(request);
  const visitId = cleanText(body?.visitId, 120);
  const company = cleanText(body?.company, 160);
  const amountCents = Math.round(Number(body?.amountCents || 0));
  const method = String(body?.method || "").trim().toUpperCase();
  if (!visitId || !company) {
    return json({ message: "Visita e empresa sao obrigatorias." }, 400);
  }
  if (!Number.isFinite(amountCents) || amountCents <= 0) {
    return json({ message: "Informe um valor recebido valido." }, 400);
  }
  if (!["PIX", "DINHEIRO", "CARTAO", "STRIPE"].includes(method)) {
    return json({ message: "Forma de pagamento invalida." }, 400);
  }

  const store = await readAdminStore(env);
  const now = new Date().toISOString();
  let payment = store.manualPayments.find((item) => String(item.visitId || "") === visitId);
  if (!payment) {
    payment = {
      id: crypto.randomUUID().replaceAll("-", ""),
      visitId,
      createdAt: now
    };
    store.manualPayments.push(payment);
  }

  Object.assign(payment, {
    source: "manual",
    status: "RECEBIDO",
    company,
    visitDate: cleanText(body?.visitDate, 30),
    amountCents,
    currency: "BRL",
    method,
    responsible: cleanText(body?.responsible, 120),
    when: now,
    adminUser: auth.email || ""
  });
  appendEvent(
    store,
    "payment.manual",
    `Pagamento recebido em visita: ${company} (${formatMoneyBr(amountCents)})`,
    "",
    auth.email
  );
  await writeAdminStore(env, store);
  return json(payment);
}

async function readAdminStore(env) {
  const response = await supabaseFetch(
    env,
    `/storage/v1/object/authenticated/${ADMIN_BUCKET}/${ADMIN_OBJECT}`,
    { method: "GET" }
  );
  if (response.status === 404) return emptyAdminStore();
  if (!response.ok) {
    throw new Error(`Supabase recusou leitura do admin (${response.status}).`);
  }
  try {
    return normalizeAdminStore(await response.json());
  } catch {
    return emptyAdminStore();
  }
}

async function writeAdminStore(env, store) {
  const normalized = normalizeAdminStore(store);
  trimStore(normalized);
  const response = await supabaseFetch(
    env,
    `/storage/v1/object/${ADMIN_BUCKET}/${ADMIN_OBJECT}`,
    {
      method: "POST",
      headers: {
        "content-type": "application/json",
        "x-upsert": "true"
      },
      body: JSON.stringify(normalized, null, 2)
    }
  );
  if (!response.ok) {
    const details = await response.text();
    throw new Error(`Supabase recusou salvar o admin (${response.status}): ${cleanText(details, 180)}`);
  }
}

const STRIPE_PLAN_AMOUNTS = {
  "basico-mensal": 4990,
  "basico-anual": 59880,
  "completo-mensal": 9990,
  "completo-anual": 119880
};

async function readStripeCheckoutSummary(env) {
  const empty = (error = "") => ({
    ok: !error,
    error,
    currency: "BRL",
    totalPurchases: 0,
    purchases24h: 0,
    totalRevenueCents: 0,
    currentMonthRevenueCents: 0,
    previousMonthRevenueCents: 0,
    recentPurchases: [],
    revenueByDay: []
  });

  try {
    const response = await supabaseRest(
      env,
      "/rest/v1/bv_license_events?select=license_key,event_type,message,payload,created_at&event_type=in.(checkout.paid,checkout.renewed,checkout.paid.v2)&order=created_at.desc&limit=1000"
    );
    const records = await response.json();
    const purchases = (Array.isArray(records) ? records : [])
      .map((record) => {
        let payload = record?.payload;
        if (typeof payload === "string") {
          try {
            payload = JSON.parse(payload);
          } catch {
            payload = {};
          }
        }
        if (!payload || typeof payload !== "object" || Array.isArray(payload)) payload = {};
        const plan = String(payload.plan_id || payload.plan || "").trim().toLowerCase();
        const amountCents = Number(
          payload.amount_total ??
          payload.amount_cents ??
          payload.amountCents ??
          STRIPE_PLAN_AMOUNTS[plan] ??
          0
        );
        const when = validDate(payload.paid_at || payload.renewed_at || record.created_at)?.toISOString()
          || new Date().toISOString();
        return {
          licenseKey: String(record.license_key || ""),
          type: String(record.event_type || "checkout.paid"),
          plan,
          checkoutSessionId: String(payload.checkout_session_id || ""),
          customerName: String(payload.display_name || payload.customer_name || ""),
          currency: String(payload.currency || "BRL").toUpperCase(),
          amountCents: Number.isFinite(amountCents) ? Math.max(0, Math.round(amountCents)) : 0,
          when
        };
      })
      .filter((item) => item.licenseKey || item.checkoutSessionId)
      .filter((item, index, rows) => {
        const key = item.checkoutSessionId || `${item.licenseKey}:${item.when}`;
        return rows.findIndex((candidate) =>
          (candidate.checkoutSessionId || `${candidate.licenseKey}:${candidate.when}`) === key
        ) === index;
      })
      .sort((a, b) => Date.parse(b.when) - Date.parse(a.when));

    const now = new Date();
    const currentMonthStart = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1);
    const previousMonthStart = Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1);
    const currentMonth = purchases.filter((item) => Date.parse(item.when) >= currentMonthStart);
    const previousMonth = purchases.filter((item) => {
      const timestamp = Date.parse(item.when);
      return timestamp >= previousMonthStart && timestamp < currentMonthStart;
    });
    const revenueByDay = new Map();
    for (const item of currentMonth) {
      const date = item.when.slice(0, 10);
      revenueByDay.set(date, (revenueByDay.get(date) || 0) + item.amountCents);
    }

    return {
      ok: true,
      error: "",
      currency: purchases.find((item) => item.currency)?.currency || "BRL",
      totalPurchases: purchases.length,
      purchases24h: purchases.filter((item) => Date.parse(item.when) >= Date.now() - 86400000).length,
      totalRevenueCents: purchases.reduce((sum, item) => sum + item.amountCents, 0),
      currentMonthRevenueCents: currentMonth.reduce((sum, item) => sum + item.amountCents, 0),
      previousMonthRevenueCents: previousMonth.reduce((sum, item) => sum + item.amountCents, 0),
      recentPurchases: purchases.slice(0, 8),
      revenueByDay: [...revenueByDay.entries()]
        .sort(([left], [right]) => left.localeCompare(right))
        .map(([date, revenueCents]) => ({ date, revenueCents }))
    };
  } catch (error) {
    return empty(cleanError(error));
  }
}

function buildDashboard(store, stripeSummary = null) {
  refreshLicenseStatuses(store);
  const now = Date.now();
  const licenses = store.licenses;
  const devices = store.devices;
  const support = store.supportTickets;
  const active = licenses.filter((item) => item.status === "ATIVA" && Date.parse(item.expiresAt) > now).length;
  const available = licenses.filter((item) => item.status === "DISPONIVEL" && Date.parse(item.expiresAt) > now).length;
  const expired = licenses.filter((item) => item.status === "EXPIRADA" || Date.parse(item.expiresAt) <= now).length;
  const blocked = licenses.filter((item) => item.status === "BLOQUEADA").length;
  const online24h = devices.filter((item) => Date.parse(item.lastSeenAt) >= now - 86400000).length;
  const openSupport = support.filter((item) => normalizeSupportStatus(item.status) !== "RESOLVIDO").length;
  const supportOpen = support.filter((item) => normalizeSupportStatus(item.status) === "ABERTO").length;
  const supportInProgress = support.filter((item) => normalizeSupportStatus(item.status) === "EM_ATENDIMENTO").length;
  const expiringSoon = licenses
    .filter((item) => item.status !== "BLOQUEADA" && Date.parse(item.expiresAt) > now && Date.parse(item.expiresAt) <= now + 30 * 86400000)
    .sort((a, b) => Date.parse(a.expiresAt) - Date.parse(b.expiresAt));
  const versionCounts = new Map();
  for (const device of devices) {
    const version = String(device.appVersion || "sem versao");
    versionCounts.set(version, (versionCounts.get(version) || 0) + 1);
  }
  const dayCounts = new Map();
  for (const device of devices) {
    const date = validDate(device.lastSeenAt)?.toISOString().slice(0, 10);
    if (date) dayCounts.set(date, (dayCounts.get(date) || 0) + 1);
  }
  const siteAnalytics = normalizeSiteAnalytics(store.siteAnalytics);
  const clientIntelligence = buildClientIntelligence(store);
  const stripe = stripeSummary || {
    ok: true,
    error: "",
    currency: "BRL",
    totalPurchases: siteAnalytics.checkoutCompletedTotal,
    purchases24h: siteAnalytics.checkoutCompleted24h,
    totalRevenueCents: siteAnalytics.checkoutCompletedRevenueCents,
    currentMonthRevenueCents: siteAnalytics.checkoutCompletedRevenueCents,
    previousMonthRevenueCents: 0,
    recentPurchases: [],
    revenueByDay: []
  };
  const stripePurchasesTotal = stripe.totalPurchases > 0
    ? stripe.totalPurchases
    : siteAnalytics.checkoutCompletedTotal;
  const stripePurchases24h = stripe.totalPurchases > 0
    ? stripe.purchases24h
    : siteAnalytics.checkoutCompleted24h;
  const stripeRevenueCents = stripe.totalRevenueCents > 0
    ? stripe.totalRevenueCents
    : siteAnalytics.checkoutCompletedRevenueCents;
  const stripeRevenueMonthCents = stripe.currentMonthRevenueCents > 0
    ? stripe.currentMonthRevenueCents
    : siteAnalytics.checkoutCompletedRevenueCents;

  return {
    metrics: {
      totalLicenses: licenses.length,
      activeLicenses: active,
      availableLicenses: available,
      expiredLicenses: expired,
      blockedLicenses: blocked,
      devices: devices.length,
      online24h,
      registeredUsers: devices.reduce((sum, item) => sum + Number(item.metrics?.usersCount || 0), 0),
      openSupport,
      waitingSupport: openSupport,
      supportOpen,
      supportInProgress,
      expiringSoon30d: expiringSoon.length,
      urgentSupport: support.filter((item) => normalizeSupportStatus(item.status) !== "RESOLVIDO" && String(item.priority).toUpperCase() === "URGENTE").length,
      siteVisitors24h: siteAnalytics.visitors24h,
      siteVisitorsTotal: siteAnalytics.totalVisitors,
      siteViews24h: siteAnalytics.views24h,
      siteViewsTotal: siteAnalytics.viewsTotal,
      checkoutStarted24h: siteAnalytics.checkoutStarted24h,
      checkoutStartedTotal: siteAnalytics.checkoutStartedTotal,
      stripePurchases24h,
      stripePurchasesTotal,
      stripeRevenueCents,
      stripeRevenueMonthCents,
      stripeRevenuePreviousMonthCents: stripe.previousMonthRevenueCents,
      stripeConversionRate: siteAnalytics.totalVisitors
        ? Math.round(stripePurchasesTotal * 1000 / siteAnalytics.totalVisitors) / 10
        : 0
    },
    clientIntelligence,
    siteAnalytics,
    stripe,
    versionDistribution: [...versionCounts.entries()].map(([version, count]) => ({ version, count })).sort((a, b) => b.count - a.count),
    activeClientsByDay: [...dayCounts.entries()].map(([date, count]) => ({ date, count })).sort((a, b) => a.date.localeCompare(b.date)),
    expiringSoon: expiringSoon.slice(0, 8),
    recentDevices: sortByDate(devices, "lastSeenAt").slice(0, 10),
    events: sortByDate(store.events, "when").slice(0, 20)
  };
}

function buildClientIntelligence(store) {
  const now = Date.now();
  const clients = store.licenses.map((license) => {
    const devices = store.devices.filter((device) =>
      String(device.licenseKey || "").toUpperCase() === String(license.key || "").toUpperCase()
    );
    const lastSeenAt = [license.lastSeenAt, ...devices.map((item) => item.lastSeenAt)]
      .filter(Boolean)
      .sort()
      .at(-1) || null;
    const isBlocked = license.status === "BLOQUEADA";
    const isExpired = Date.parse(license.expiresAt) <= now;
    const isOnline = Date.parse(lastSeenAt) >= now - 86400000;
    const healthScore = isBlocked || isExpired ? 20 : isOnline ? 92 : lastSeenAt ? 62 : 45;
    const healthBand = healthScore >= 80 ? "healthy" : healthScore >= 50 ? "attention" : "critical";
    const name = license.businessName || license.customerName || "Cliente";
    return {
      id: license.id || license.key,
      name,
      initials: initials(name),
      cnpj: license.cnpj || "",
      city: license.city || license.environmentSnapshot?.city || "",
      state: license.state || license.environmentSnapshot?.state || "",
      plan: license.plan || "Sem plano",
      healthScore,
      healthBand,
      reason: isBlocked ? "Licenca bloqueada" : isExpired ? "Licenca expirada" : isOnline ? "Sincronizado nas ultimas 24h" : "Aguardando sincronizacao",
      suggestedAction: healthBand === "healthy" ? "Ver detalhes" : "Revisar cliente",
      actionView: "licenses",
      deviceCount: devices.length,
      lastSeenAt,
      isActive: !isBlocked && !isExpired,
      licenseCount: 1,
      licenseKey: license.key,
      licenseStatus: license.status,
      expiresAt: license.expiresAt,
      clientSinceAt: license.createdAt,
      ownerName: license.ownerName || "",
      email: license.email || "",
      phone: license.phone || "",
      confidence: devices.length ? 90 : 55,
      confidenceLabel: devices.length ? "alta" : "parcial",
      syncLabel: isOnline ? "Sincronizado hoje" : lastSeenAt ? "Sincronizacao antiga" : "Sem sincronizacao",
      history: sortByDate(store.events.filter((event) => event.licenseKey === license.key), "when").slice(0, 20)
    };
  });
  const healthyClients = clients.filter((item) => item.healthBand === "healthy").length;
  const attentionClients = clients.filter((item) => item.healthBand === "attention").length;
  const criticalClients = clients.filter((item) => item.healthBand === "critical").length;
  const synchronizedToday = clients.filter((item) => Date.parse(item.lastSeenAt) >= now - 86400000).length;
  return {
    summary: {
      totalClients: clients.length,
      activeClients: clients.filter((item) => item.isActive).length,
      healthyClients,
      attentionClients,
      criticalClients,
      synchronizedToday,
      synchronizedTodayPercent: clients.length ? Math.round(synchronizedToday * 100 / clients.length) : 0
    },
    clients: clients.sort((a, b) => a.healthScore - b.healthScore || a.name.localeCompare(b.name)),
    blockedIpCount: store.blockedIps.length,
    updatedAt: new Date().toISOString()
  };
}

async function supabaseRest(env, path, init = {}) {
  const response = await supabaseFetch(env, path, {
    ...init,
    headers: {
      "content-type": "application/json",
      ...(init.headers || {})
    }
  });
  if (!response.ok) {
    const details = await response.text();
    throw new Error(`Supabase recusou a operacao (${response.status}): ${cleanText(details, 180)}`);
  }
  return response;
}

function supabaseFetch(env, path, init = {}) {
  const headers = new Headers(init.headers || {});
  headers.set("apikey", env.SUPABASE_SERVICE_ROLE_KEY);
  headers.set("authorization", `Bearer ${env.SUPABASE_SERVICE_ROLE_KEY}`);
  return fetch(`${supabaseUrl(env)}${path}`, { ...init, headers });
}

function emptyAdminStore() {
  return {
    licenses: [],
    devices: [],
    supportTickets: [],
    blockedIps: [],
    manualPayments: [],
    events: [],
    siteAnalytics: {}
  };
}

function normalizeAdminStore(value) {
  const store = value && typeof value === "object" ? value : emptyAdminStore();
  for (const key of ["licenses", "devices", "supportTickets", "blockedIps", "manualPayments", "events"]) {
    if (!Array.isArray(store[key])) store[key] = [];
  }
  if (!store.siteAnalytics || typeof store.siteAnalytics !== "object") store.siteAnalytics = {};
  return store;
}

function normalizeSiteAnalytics(value) {
  const source = value && typeof value === "object" ? value : {};
  return {
    visitors24h: Number(source.visitors24h || 0),
    totalVisitors: Number(source.totalVisitors || 0),
    views24h: Number(source.views24h || 0),
    viewsTotal: Number(source.viewsTotal || 0),
    checkoutStarted24h: Number(source.checkoutStarted24h || 0),
    checkoutStartedTotal: Number(source.checkoutStartedTotal || 0),
    checkoutCompleted24h: Number(source.checkoutCompleted24h || 0),
    checkoutCompletedTotal: Number(source.checkoutCompletedTotal || 0),
    checkoutCompletedRevenueCents: Number(source.checkoutCompletedRevenueCents || 0),
    topPages: Array.isArray(source.topPages) ? source.topPages : []
  };
}

function refreshLicenseStatuses(store) {
  const now = Date.now();
  for (const license of store.licenses) {
    license.status = String(license.status || "DISPONIVEL").toUpperCase();
    if (license.status !== "BLOQUEADA" && Date.parse(license.expiresAt) <= now) {
      license.status = "EXPIRADA";
    }
  }
}

function trimStore(store) {
  store.supportTickets = sortSupport(store.supportTickets).slice(0, 300);
  store.events = sortByDate(store.events, "when").slice(0, 500);
  store.blockedIps = sortByDate(store.blockedIps, "updatedAt").slice(0, 1000);
  store.manualPayments = sortByDate(store.manualPayments, "when").slice(0, 1000);
}

function appendEvent(store, type, message, licenseKey = "", adminUser = "") {
  store.events.push({
    type,
    message,
    licenseKey: licenseKey || "",
    machineCode: "",
    adminUser,
    when: new Date().toISOString()
  });
}

function sortSupport(items) {
  const rank = { ABERTO: 0, EM_ATENDIMENTO: 1, RESOLVIDO: 2 };
  return [...items].sort((a, b) =>
    (rank[normalizeSupportStatus(a.status)] ?? 3) - (rank[normalizeSupportStatus(b.status)] ?? 3) ||
    Date.parse(b.updatedAt || b.createdAt || 0) - Date.parse(a.updatedAt || a.createdAt || 0)
  );
}

function sortByDate(items, field) {
  return [...items].sort((a, b) => Date.parse(b?.[field] || 0) - Date.parse(a?.[field] || 0));
}

function formatMoneyBr(amountCents) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL"
  }).format(Number(amountCents || 0) / 100);
}

function normalizeSupportStatus(value) {
  const clean = String(value || "").toUpperCase();
  if (["EM_ATENDIMENTO", "ATENDIMENTO", "ATENDENDO"].includes(clean)) return "EM_ATENDIMENTO";
  if (["RESOLVIDO", "RESOLVIDA", "FECHADO", "FECHADA"].includes(clean)) return "RESOLVIDO";
  return "ABERTO";
}

function normalizeDurationUnit(value) {
  const unit = String(value || "days").toLowerCase();
  if (["months", "month", "mes", "meses"].includes(unit)) return "months";
  if (["years", "year", "ano", "anos"].includes(unit)) return "years";
  return "days";
}

function durationLabel(unit, amount) {
  if (unit === "years") return amount === 1 ? "ano" : "anos";
  if (unit === "months") return amount === 1 ? "mes" : "meses";
  return amount === 1 ? "dia" : "dias";
}

function addDuration(date, amount, unit) {
  const result = new Date(date);
  if (unit === "years") result.setUTCFullYear(result.getUTCFullYear() + amount);
  else if (unit === "months") result.setUTCMonth(result.getUTCMonth() + amount);
  else result.setUTCDate(result.getUTCDate() + amount);
  return result;
}

async function createLicenseKey(expiresAt) {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Sao_Paulo",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23"
  }).formatToParts(expiresAt);
  const get = (type) => parts.find((item) => item.type === type)?.value || "";
  const expiration = `${get("year")}${get("month")}${get("day")}${get("hour")}${get("minute")}`;
  const random = crypto.getRandomValues(new Uint8Array(4));
  const serial = [...random].map((value) => value.toString(16).padStart(2, "0")).join("").toUpperCase();
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(LICENSE_SECRET),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"]
  );
  const signatureBytes = await crypto.subtle.sign(
    "HMAC",
    key,
    new TextEncoder().encode(`BLV|${expiration}|${serial}`)
  );
  const signature = [...new Uint8Array(signatureBytes)]
    .map((value) => value.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase()
    .slice(0, 10);
  return `BLV-${expiration}-${serial}-${signature}`;
}

async function planVisits(request, env) {
  if (!env.OPENROUTER_API_KEY && (!env.AGENDA_AI_SERVICE || !env.ADMIN_AI_BRIDGE_SECRET)) {
    return json({
      message: "OpenRouter nao configurado. O painel continuara usando o plano local.",
      code: "openrouter_not_configured"
    }, 503);
  }

  const body = await safeJson(request);
  const candidates = sanitizeCandidates(body?.candidates);
  if (!candidates.length) {
    return json({ message: "Envie ao menos uma oportunidade valida." }, 400);
  }

  const systemPrompt = [
    "Voce prioriza visitas comerciais presenciais.",
    "A geometria, o transito e a polyline sao calculados por outro servico; nao invente caminhos.",
    "Ordene somente os IDs recebidos usando potencial comercial, estagio do funil, distancia e eficiencia do dia.",
    "Responda exclusivamente JSON:",
    '{"orderedIds":["id"],"summary":"ate 140 caracteres","reasons":[{"id":"id","reason":"ate 70 caracteres"}]}',
    "Use cada ID exatamente uma vez e nao inclua IDs desconhecidos."
  ].join(" ");

  const openRouterPayload = {
    model: "openrouter/free",
    temperature: 0.2,
    max_tokens: 650,
    messages: [
      { role: "system", content: systemPrompt },
      { role: "user", content: JSON.stringify({ candidates }) }
    ]
  };
  const openRouterResponse = env.OPENROUTER_API_KEY
    ? await fetch("https://openrouter.ai/api/v1/chat/completions", {
        method: "POST",
        headers: {
          "authorization": `Bearer ${env.OPENROUTER_API_KEY}`,
          "content-type": "application/json",
          "x-title": "Balcao Livre Admin"
        },
        body: JSON.stringify(openRouterPayload)
      })
    : await env.AGENDA_AI_SERVICE.fetch("https://agenda-ai.internal/api/internal/admin/openrouter", {
        method: "POST",
        headers: {
          "content-type": "application/json",
          "x-admin-ai-bridge-secret": env.ADMIN_AI_BRIDGE_SECRET
        },
        body: JSON.stringify(openRouterPayload)
      });
  if (!openRouterResponse.ok) {
    return json({ message: "A IA esta indisponivel agora. Use o plano local.", code: "openrouter_unavailable" }, 503);
  }

  try {
    const responseBody = await openRouterResponse.json();
    const content = stripJsonFence(responseBody?.choices?.[0]?.message?.content || "");
    const plan = JSON.parse(content);
    const allowed = new Set(candidates.map((item) => item.id));
    const orderedIds = [];
    for (const id of Array.isArray(plan.orderedIds) ? plan.orderedIds : []) {
      if (allowed.has(id) && !orderedIds.includes(id)) orderedIds.push(id);
    }
    for (const candidate of candidates) {
      if (!orderedIds.includes(candidate.id)) orderedIds.push(candidate.id);
    }
    return json({
      orderedIds,
      summary: cleanText(plan.summary, 140) || "Plano priorizado por potencial comercial e eficiencia da rota.",
      reasons: (Array.isArray(plan.reasons) ? plan.reasons : [])
        .filter((item) => allowed.has(item?.id))
        .map((item) => ({ id: item.id, reason: cleanText(item.reason, 70) })),
      model: "openrouter/free"
    });
  } catch {
    return json({ message: "Nao foi possivel interpretar a sugestao da IA. Use o plano local.", code: "openrouter_invalid_response" }, 503);
  }
}

async function findNearbyVisits(request) {
  const url = new URL(request.url);
  const latitude = Number(url.searchParams.get("latitude"));
  const longitude = Number(url.searchParams.get("longitude"));
  if (
    !Number.isFinite(latitude) ||
    !Number.isFinite(longitude) ||
    Math.abs(latitude) > 90 ||
    Math.abs(longitude) > 180
  ) {
    return json({ message: "Localizacao invalida." }, 400);
  }

  const latitudeRadius = 0.055;
  const longitudeRadius = latitudeRadius / Math.max(0.35, Math.cos(latitude * Math.PI / 180));
  const searchUrl = new URL("https://nominatim.openstreetmap.org/search");
  searchUrl.search = new URLSearchParams({
    format: "jsonv2",
    addressdetails: "1",
    limit: "12",
    bounded: "1",
    viewbox: [
      longitude - longitudeRadius,
      latitude + latitudeRadius,
      longitude + longitudeRadius,
      latitude - latitudeRadius
    ].join(","),
    q: "restaurant"
  }).toString();

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 12000);
  let payload = [];
  try {
    const response = await fetch(searchUrl, {
      headers: {
        "accept": "application/json",
        "accept-language": "pt-BR",
        "user-agent": "BalcaoLivreAdmin/1.0 (admin.balcaolivrepdv.com.br)"
      },
      signal: controller.signal
    });
    if (response.ok) payload = await response.json();
  } catch {
    payload = [];
  } finally {
    clearTimeout(timeout);
  }

  if (!Array.isArray(payload) || !payload.length) {
    return json({ opportunities: [], message: "Nenhum estabelecimento encontrado nesta regiao." });
  }

  const opportunities = payload
    .map((place) => {
      const address = place.address || {};
      const lat = Number(place.lat);
      const lon = Number(place.lon);
      const name = cleanText(place.name || String(place.display_name || "").split(",")[0], 120);
      if (!name || !Number.isFinite(lat) || !Number.isFinite(lon)) return null;
      const category = cleanText(place.type || place.category || "restaurant", 40);
      const distanceMeters = Math.round(geoDistance(latitude, longitude, lat, lon));
      const baseScore = {
        restaurant: 88,
        fast_food: 84,
        food_court: 82,
        bar: 78,
        cafe: 76,
        bakery: 74,
        ice_cream: 72,
        butcher: 70,
        deli: 70,
        convenience: 66,
        supermarket: 64
      }[category] || 80;
      const importanceBonus = Math.min(8, Math.round(Number(place.importance || 0) * 12));
      return {
        id: `osm-${place.osm_type || "place"}-${place.osm_id}`,
        name,
        category,
        neighborhood: cleanText(
          address.suburb ||
          address.neighbourhood ||
          address.city_district ||
          address.city ||
          address.town ||
          "Regiao proxima",
          80
        ),
        latitude: lat,
        longitude: lon,
        distanceMeters,
        score: Math.min(95, baseScore + importanceBonus),
        stage: "mapped"
      };
    })
    .filter(Boolean)
    .sort((a, b) => (b.score - a.score) || (a.distanceMeters - b.distanceMeters))
    .filter((item, index, rows) =>
      rows.findIndex((row) => row.name.toLowerCase() === item.name.toLowerCase()) === index
    )
    .slice(0, 12);

  return json({ opportunities });
}

function geoDistance(lat1, lon1, lat2, lon2) {
  const radians = (value) => value * Math.PI / 180;
  const dLat = radians(lat2 - lat1);
  const dLon = radians(lon2 - lon1);
  const value = Math.sin(dLat / 2) ** 2
    + Math.cos(radians(lat1)) * Math.cos(radians(lat2)) * Math.sin(dLon / 2) ** 2;
  return 6371000 * 2 * Math.atan2(Math.sqrt(value), Math.sqrt(1 - value));
}

function sanitizeCandidates(value) {
  if (!Array.isArray(value)) return [];
  return value.slice(0, 12).map((item) => ({
    id: cleanText(item?.id, 80),
    name: cleanText(item?.name, 120),
    neighborhood: cleanText(item?.neighborhood, 80),
    score: clampNumber(item?.score, 0, 100, 60),
    stage: cleanText(item?.stage, 40),
    distanceMeters: clampNumber(item?.distanceMeters, 0, 100000, 0)
  })).filter((item) => item.id && item.name);
}

function stripJsonFence(value) {
  const text = String(value || "").trim();
  if (!text.startsWith("```")) return text;
  return text.replace(/^```(?:json)?\s*/i, "").replace(/\s*```$/, "").trim();
}

function appendSessionCookies(headers, session) {
  const maxAge = Math.max(60, Number(session.expires_in || 3600));
  headers.append("set-cookie", cookie(ACCESS_COOKIE, session.access_token, maxAge));
  headers.append("set-cookie", cookie(REFRESH_COOKIE, session.refresh_token, 60 * 60 * 24 * 30));
}

function clearSessionCookies(headers) {
  headers.append("set-cookie", cookie(ACCESS_COOKIE, "", 0));
  headers.append("set-cookie", cookie(REFRESH_COOKIE, "", 0));
}

function cookie(name, value, maxAge) {
  return `${name}=${encodeURIComponent(String(value || ""))}; Path=/; Max-Age=${maxAge}; HttpOnly; Secure; SameSite=Strict`;
}

function withSession(response, auth) {
  if (auth?.refreshedSession) appendSessionCookies(response.headers, auth.refreshedSession);
  return response;
}

function parseCookies(header) {
  const result = {};
  for (const part of String(header || "").split(";")) {
    const index = part.indexOf("=");
    if (index <= 0) continue;
    const name = part.slice(0, index).trim();
    const value = part.slice(index + 1).trim();
    try {
      result[name] = decodeURIComponent(value);
    } catch {
      result[name] = value;
    }
  }
  return result;
}

function adminEmails(env) {
  return new Set(String(env.ADMIN_EMAILS || "")
    .split(",")
    .map((value) => value.trim().toLowerCase())
    .filter(Boolean));
}

function adminProfile(user, email, env) {
  const metadata = user?.user_metadata || {};
  const configuredName = configuredAdminNames(env).get(email) || "";
  const name = String(
    configuredName ||
    metadata.full_name ||
    metadata.name ||
    metadata.display_name ||
    email.split("@")[0] ||
    "Administrador"
  ).trim();
  const words = name.split(/\s+/).filter(Boolean);
  const firstName = words[0] || "Administrador";
  const initials = words.slice(0, 2).map((word) => word[0]?.toUpperCase() || "").join("") || "AD";
  return { email, name, firstName, initials };
}

function configuredAdminNames(env) {
  return new Map(String(env.ADMIN_DISPLAY_NAMES || "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean)
    .map((entry) => {
      const separator = entry.indexOf("=");
      return separator > 0
        ? [entry.slice(0, separator).trim().toLowerCase(), entry.slice(separator + 1).trim()]
        : ["", ""];
    })
    .filter(([email, name]) => email && name));
}

function validMutationOrigin(request) {
  const origin = request.headers.get("origin");
  if (!origin) return true;
  try {
    return new URL(origin).hostname === "admin.balcaolivrepdv.com.br";
  } catch {
    return false;
  }
}

function hasSupabaseBackend(env) {
  return Boolean(env.SUPABASE_URL && env.SUPABASE_SERVICE_ROLE_KEY);
}

function supabaseUrl(env) {
  return String(env.SUPABASE_URL || "").replace(/\/+$/, "");
}

function normalizeIp(value) {
  return String(value || "").trim().replace(/^::ffff:/i, "").slice(0, 80);
}

function initials(value) {
  return String(value || "CL")
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0] || "")
    .join("")
    .toUpperCase();
}

function validDate(value) {
  const date = new Date(value);
  return Number.isFinite(date.getTime()) ? date : null;
}

function clampNumber(value, minimum, maximum, fallback) {
  const number = Number(value);
  return Number.isFinite(number) ? Math.min(maximum, Math.max(minimum, number)) : fallback;
}

function cleanText(value, maxLength = 300) {
  return String(value || "").replace(/[\r\n\t]+/g, " ").trim().slice(0, maxLength);
}

function cleanError(error) {
  const message = error instanceof Error ? error.message : String(error || "");
  return cleanText(message, 240) || "Falha interna no Worker do admin.";
}

async function safeJson(request) {
  try {
    return await request.json();
  } catch {
    return {};
  }
}

async function readResponseJson(response) {
  try {
    return await response.json();
  } catch {
    return {};
  }
}

function json(value, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: JSON_HEADERS
  });
}
