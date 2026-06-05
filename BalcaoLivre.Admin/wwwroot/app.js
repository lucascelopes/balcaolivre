const state = {
  dashboard: null,
  licenses: [],
  support: [],
  blockedIps: [],
  health: null,
  currentView: "dashboard",
  realtimeStatus: "booting",
  realtimeSnapshot: null,
  isRefreshing: false,
  refreshQueued: false,
  lastSyncAt: null
};

let liveTimer = null;
let realtimeSource = null;
let realtimeFallbackTimer = null;
let refreshAllQueued = null;
let lastSupportCustomerMessageAt = "";

const livePollMs = 3000;
const realtimeFallbackPollMs = 5000;
const releaseDownloads = {
  version: "1.8.2026.10",
  publishedAt: "2026-06-05T17:49:03-03:00",
  installerUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.10.exe",
  trialUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/trial-download?plan=online",
  checkoutMonthlyUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout?plan=online-mensal",
  checkoutAnnualUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout?plan=online-anual",
  manifestUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/version.json"
};

const qs = (selector) => document.querySelector(selector);
const dateTime = (value) => value ? new Date(value).toLocaleString("pt-BR") : "-";
const timeOnly = (value) => value ? new Date(value).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit", second: "2-digit" }) : "-";
const number = (value) => Number(value || 0).toLocaleString("pt-BR");
const moneyFromCents = (value, currency = "BRL") =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: currency || "BRL" }).format((Number(value) || 0) / 100);

const adminApiBase = (() => {
  const configured = window.BALCAO_ADMIN_API_BASE || "";
  if (configured) return configured.replace(/\/$/, "");
  const adminSubdomain = location.hostname === "admin.balcaolivrepdv.com.br" || location.hostname.startsWith("admin.");
  return location.pathname.startsWith("/admin") || adminSubdomain ? "/admin-api" : "";
})();

function adminApiPath(path) {
  if (!adminApiBase) return path;
  const normalized = path.startsWith("/api/") ? path.slice(4) : path;
  return `${adminApiBase}${normalized}`;
}

async function api(path, options = {}) {
  const response = await fetch(adminApiPath(path), {
    credentials: "same-origin",
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    ...options
  });
  if (response.status === 401) {
    showLogin();
    throw new Error("Nao autenticado.");
  }
  const text = await response.text();
  let data = {};
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = { message: response.ok ? text : "API do admin indisponivel." };
    }
  }
  if (!response.ok) {
    throw new Error(data.message || "Erro na requisicao.");
  }
  return data;
}

function showLogin() {
  stopLiveRefresh();
  qs("#loginView").classList.remove("hidden");
  qs("#appView").classList.add("hidden");
}

function showApp() {
  qs("#loginView").classList.add("hidden");
  qs("#appView").classList.remove("hidden");
  renderRealtimeBadge();
}

async function login() {
  qs("#loginMessage").textContent = "";
  try {
    await api("/api/login", {
      method: "POST",
      body: JSON.stringify({
        user: qs("#loginUser").value,
        password: qs("#loginPassword").value
      })
    });
    showApp();
    startLiveRefresh();
    await loadRealtimeData({ notifySupport: false, force: true });
  } catch (error) {
    qs("#loginMessage").textContent = error.message;
  }
}

async function logout() {
  await api("/api/logout", { method: "POST" }).catch(() => {});
  stopLiveRefresh();
  showLogin();
}

function setView(view) {
  state.currentView = view;
  document.querySelectorAll(".view").forEach((item) => item.classList.add("hidden"));
  qs(`#${view}View`).classList.remove("hidden");
  document.querySelectorAll(".nav").forEach((item) => item.classList.toggle("active", item.dataset.view === view));
  const titles = {
    dashboard: ["Dashboard", "Operacao, vendas, clientes e licencas em tempo real."],
    licenses: ["Licencas", "Chaves, status, computador vinculado e vencimento."],
    support: ["Suporte", "Conversas entre clientes e administradores."],
    devices: ["Clientes", "Dados sincronizados pelos apps instalados."],
    downloads: ["Downloads", "Instaladores, checkout e manifesto de atualizacao."],
    keys: ["Criar chave", "Gere uma licenca unica por periodo."]
  };
  qs("#viewTitle").textContent = titles[view][0];
  qs("#viewSubtitle").textContent = titles[view][1];
}

async function loadRealtimeData(options = {}) {
  const notifySupport = options.notifySupport !== false;
  if (state.isRefreshing) {
    state.refreshQueued = true;
    return;
  }

  state.isRefreshing = true;
  renderSyncState("loading");

  try {
    const previousCustomerMessageAt = lastSupportCustomerMessageAt;
    const [dashboard, licenses, support, health, blockedIps] = await Promise.all([
      api("/api/dashboard"),
      api("/api/licenses"),
      api("/api/support"),
      api("/api/health"),
      api("/api/blocked-ips")
    ]);

    state.dashboard = dashboard;
    state.licenses = Array.isArray(licenses) ? licenses : [];
    state.support = Array.isArray(support) ? support : [];
    state.blockedIps = Array.isArray(blockedIps) ? blockedIps : [];
    state.health = health;
    state.lastSyncAt = new Date();
    lastSupportCustomerMessageAt = newestCustomerMessageAt(state.support);

    if (notifySupport) {
      notifySupportChange(previousCustomerMessageAt, lastSupportCustomerMessageAt);
    }

    renderAll();
    renderSyncState("ok");
  } catch (error) {
    console.warn("admin realtime refresh failed", error);
    renderSyncState("error", error.message);
  } finally {
    state.isRefreshing = false;
    if (state.refreshQueued) {
      state.refreshQueued = false;
      setTimeout(() => loadRealtimeData({ notifySupport }), 120);
    }
  }
}

function renderAll() {
  renderDashboard();
  renderLicenses();
  renderSupport();
  renderDevices();
  renderBlockedIps();
  renderDownloads();
  renderRealtimeBadge();
}

function startLiveRefresh() {
  if ("Notification" in window && Notification.permission === "default") {
    Notification.requestPermission().catch(() => {});
  }
  startRealtime();
  if (!liveTimer) {
    liveTimer = setInterval(() => {
      loadRealtimeData({ notifySupport: true }).catch((error) => console.warn("live refresh failed", error));
    }, livePollMs);
  }
}

function stopLiveRefresh() {
  if (liveTimer) {
    clearInterval(liveTimer);
    liveTimer = null;
  }
  stopRealtime();
}

function startRealtime() {
  if (realtimeSource || qs("#appView").classList.contains("hidden")) return;
  if (!("EventSource" in window)) {
    startRealtimeFallback();
    return;
  }

  updateRealtimeStatus("connecting");
  const source = new EventSource(adminApiPath("/api/realtime"), { withCredentials: true });
  realtimeSource = source;

  source.addEventListener("admin.ready", (event) => {
    updateRealtimeStatus("online", parseRealtimeEvent(event));
  });
  source.addEventListener("admin.changed", (event) => {
    updateRealtimeStatus("online", parseRealtimeEvent(event));
    queueRealtimeRefresh();
  });
  source.onopen = () => updateRealtimeStatus("online", state.realtimeSnapshot);
  source.onerror = () => {
    if (realtimeSource === source) {
      updateRealtimeStatus("reconnecting", state.realtimeSnapshot);
    }
  };
}

function startRealtimeFallback() {
  updateRealtimeStatus("fallback");
  if (realtimeFallbackTimer) return;
  realtimeFallbackTimer = setInterval(() => queueRealtimeRefresh(), realtimeFallbackPollMs);
}

function stopRealtime() {
  if (realtimeSource) {
    realtimeSource.close();
    realtimeSource = null;
  }
  if (realtimeFallbackTimer) {
    clearInterval(realtimeFallbackTimer);
    realtimeFallbackTimer = null;
  }
  if (refreshAllQueued) {
    clearTimeout(refreshAllQueued);
    refreshAllQueued = null;
  }
  updateRealtimeStatus("offline");
}

function parseRealtimeEvent(event) {
  try {
    return JSON.parse(event.data || "{}");
  } catch {
    return null;
  }
}

function updateRealtimeStatus(status, snapshot = null) {
  state.realtimeStatus = status;
  if (snapshot) state.realtimeSnapshot = snapshot;
  window.__balcaoAdminRealtimeStatus = {
    status,
    snapshot: state.realtimeSnapshot,
    updatedAt: new Date().toISOString()
  };
  renderRealtimeBadge();
}

function queueRealtimeRefresh() {
  if (refreshAllQueued || qs("#appView").classList.contains("hidden")) return;
  refreshAllQueued = setTimeout(() => {
    refreshAllQueued = null;
    loadRealtimeData({ notifySupport: true }).catch((error) => console.warn("realtime refresh failed", error));
  }, 120);
}

function renderSyncState(mode, message = "") {
  const lastSync = qs("#lastSync");
  const sidebarSync = qs("#sidebarSync");
  if (!lastSync || !sidebarSync) return;

  if (mode === "loading") {
    lastSync.textContent = state.lastSyncAt ? `Sincronizando... ${timeOnly(state.lastSyncAt)}` : "Sincronizando...";
    lastSync.className = "status-pill pending";
    sidebarSync.textContent = "Sincronizando dados";
    return;
  }

  if (mode === "error") {
    lastSync.textContent = message ? `Falha: ${message}` : "Falha no sync";
    lastSync.className = "status-pill error";
    sidebarSync.textContent = "Sync com falha";
    return;
  }

  lastSync.textContent = state.lastSyncAt ? `Atualizado ${timeOnly(state.lastSyncAt)}` : "Sem sync";
  lastSync.className = "status-pill online";
  sidebarSync.textContent = state.lastSyncAt ? `Atualizado ${timeOnly(state.lastSyncAt)}` : "Aguardando sync";
}

function renderRealtimeBadge() {
  const badge = qs("#realtimeMode");
  const storage = qs("#storageMode");
  if (!badge || !storage) return;

  const labels = {
    online: "Tempo real ligado",
    booting: "Tempo real iniciando",
    connecting: "Tempo real conectando",
    reconnecting: "Tempo real reconectando",
    fallback: "Atualizacao automatica",
    offline: "Tempo real desligado"
  };
  badge.textContent = labels[state.realtimeStatus] || labels.booting;
  badge.className = "status-pill";
  badge.classList.add(state.realtimeStatus === "online" || state.realtimeStatus === "fallback" ? "online" : state.realtimeStatus === "offline" ? "neutral" : "pending");

  const storageMode = state.health?.storage || "checking";
  const storageText = storageMode === "supabase"
    ? "Supabase ativo"
    : storageMode === "supabase-pendente"
      ? "Supabase pendente"
      : storageMode === "supabase-nao-configurado"
        ? "Supabase nao configurado"
        : storageMode === "checking"
          ? "Checando Supabase"
          : "Local JSON";
  storage.textContent = storageText;
  storage.className = "status-pill";
  storage.classList.add(storageMode === "supabase" ? "online" : storageMode === "local-json" ? "neutral" : storageMode === "supabase-pendente" || storageMode === "checking" ? "pending" : "error");
}

function newestCustomerMessageAt(tickets) {
  return tickets
    .flatMap((ticket) => ticket.messages || [])
    .filter((message) => message.sender === "cliente")
    .map((message) => message.when || "")
    .sort()
    .at(-1) || "";
}

function notifySupportChange(previous, current) {
  if (!previous || !current || current <= previous) return;
  document.title = "Novo suporte - Balcao Livre Admin";
  if ("Notification" in window && Notification.permission === "granted") {
    new Notification("Novo suporte Balcao Livre", { body: "Chegou mensagem de cliente no painel." });
  }
}

function renderDashboard() {
  if (!state.dashboard) return;
  const metrics = state.dashboard.metrics || {};
  const siteAnalytics = state.dashboard.siteAnalytics || {};
  const stripe = state.dashboard.stripe || {};

  qs("#mActive").textContent = number(metrics.activeLicenses);
  qs("#mAvailable").textContent = number(metrics.availableLicenses);
  qs("#mOnline").textContent = number(metrics.online24h);
  qs("#mUsers").textContent = number(metrics.registeredUsers);
  qs("#mDevices").textContent = number(metrics.devices);
  qs("#mSupport").textContent = number(metrics.openSupport);
  qs("#mSiteVisitors").textContent = `${number(metrics.siteVisitors24h)} / ${number(metrics.siteVisitorsTotal)}`;
  qs("#mSiteViews").textContent = `${number(metrics.siteViews24h)} / ${number(metrics.siteViewsTotal)}`;
  qs("#mCheckoutStarted").textContent = `${number(metrics.checkoutStarted24h)} / ${number(metrics.checkoutStartedTotal)}`;
  qs("#mStripePurchases").textContent = `${number(metrics.stripePurchases24h)} / ${number(metrics.stripePurchasesTotal)}`;
  qs("#mStripeRevenue").textContent = moneyFromCents(metrics.stripeRevenueCents || 0, stripe.currency || "BRL");
  qs("#mConversion").textContent = `${Number(metrics.stripeConversionRate || 0).toLocaleString("pt-BR")}%`;

  const topPages = siteAnalytics.topPages || [];
  qs("#analyticsBadge").textContent = `${number(siteAnalytics.views24h)} views 24h`;
  qs("#analyticsList").innerHTML = topPages.length
    ? topPages.map((item) => `
      <div class="list-row">
        <strong>${escapeHtml(item.path || "/")}</strong>
        <small>${number(item.views)} visualizacoes - ${number(item.visitors)} visitante(s)</small>
      </div>`).join("")
    : `<div class="empty-row">Nenhuma visita registrada pelo site ainda.</div>`;

  const recentPurchases = stripe.recentPurchases || [];
  qs("#stripeBadge").textContent = stripe.ok ? `${number(metrics.stripePurchasesTotal)} compra(s)` : "indisponivel";
  qs("#stripeList").innerHTML = !stripe.ok
    ? `<div class="empty-row">Stripe/Supabase indisponivel: ${escapeHtml(stripe.error || "sem detalhes")}</div>`
    : recentPurchases.length
      ? recentPurchases.map((item) => `
        <div class="list-row">
          <strong>${escapeHtml(item.plan || item.type || "Compra Stripe")}</strong>
          <small>${moneyFromCents(item.amountCents || 0, item.currency || stripe.currency || "BRL")} - ${dateTime(item.when)} - ${escapeHtml(item.licenseKey || item.checkoutSessionId || "")}</small>
        </div>`).join("")
      : `<div class="empty-row">Nenhuma compra Stripe confirmada ainda.</div>`;

  qs("#expiringList").innerHTML = (state.dashboard.expiringSoon || []).length
    ? state.dashboard.expiringSoon.map((item) => `
      <div class="list-row">
        <strong>${escapeHtml(item.customerName || item.businessName || "Cliente")}</strong>
        <small>${escapeHtml(item.key)} - expira ${dateTime(item.expiresAt)}</small>
      </div>`).join("")
    : `<div class="empty-row">Nenhuma licenca vencendo nos proximos 15 dias.</div>`;

  qs("#versionsList").innerHTML = (state.dashboard.versionDistribution || []).length
    ? state.dashboard.versionDistribution.map((item) => `
      <div class="list-row split">
        <strong>${escapeHtml(item.version)}</strong>
        <small>${number(item.count)} maquina(s)</small>
      </div>`).join("")
    : `<div class="empty-row">Nenhuma maquina sincronizada ainda.</div>`;

  qs("#eventsBadge").textContent = `${number((state.dashboard.events || []).length)} evento(s)`;
  qs("#eventsList").innerHTML = (state.dashboard.events || []).length
    ? state.dashboard.events.map((item) => `
      <div class="event-row">
        <strong>${escapeHtml(item.message)}</strong>
        <small>${escapeHtml(item.type)} - ${dateTime(item.when)}</small>
      </div>`).join("")
    : `<div class="empty-row">Sem eventos ainda.</div>`;
}

function renderDownloads() {
  const container = qs("#downloadCards");
  if (!container) return;

  qs("#releaseVersion").textContent = releaseDownloads.version;
  qs("#releasePublishedAt").textContent = `Publicado em ${dateTime(releaseDownloads.publishedAt)}`;
  qs("#releaseManifestUrl").textContent = releaseDownloads.manifestUrl;

  const cards = [
    {
      key: "trial",
      title: "Testadores",
      badge: "7 dias",
      text: "Gera teste Online e baixa o instalador atual.",
      primary: "Abrir teste",
      url: releaseDownloads.trialUrl,
    },
    {
      key: "checkoutMonthly",
      title: "Pagamento mensal",
      badge: "Stripe",
      text: "Abre checkout do Restaurante Profissional mensal.",
      primary: "Abrir pagamento",
      url: releaseDownloads.checkoutMonthlyUrl,
    },
    {
      key: "installer",
      title: "Clientes ativos",
      badge: "Auto-update",
      text: "Instalador usado pelo manifesto de atualizacao.",
      primary: "Baixar instalador",
      url: releaseDownloads.installerUrl,
    },
    {
      key: "checkoutAnnual",
      title: "Pagamento anual",
      badge: "Stripe",
      text: "Abre checkout do Restaurante Profissional anual.",
      primary: "Abrir anual",
      url: releaseDownloads.checkoutAnnualUrl,
    },
  ];

  container.innerHTML = cards.map((item) => `
    <article class="download-card">
      <div class="download-card-top">
        <span class="download-icon">${escapeHtml(item.title.slice(0, 2).toUpperCase())}</span>
        <span class="mini-badge">${escapeHtml(item.badge)}</span>
      </div>
      <h3>${escapeHtml(item.title)}</h3>
      <p>${escapeHtml(item.text)}</p>
      <div class="download-actions">
        <a class="button-link" href="${escapeHtml(item.url)}" target="_blank" rel="noopener">${escapeHtml(item.primary)}</a>
        <button class="download-copy" type="button" onclick="copyDownloadUrl('${escapeHtml(item.key)}')">Copiar</button>
      </div>
    </article>
  `).join("");
}

function renderSupport() {
  const target = qs("#supportList");
  if (!target) return;
  const query = qs("#supportSearch").value.trim().toLowerCase();
  const rows = state.support.filter((item) => {
    const haystack = `${item.shortId} ${item.licenseKey} ${item.customerName} ${item.businessName} ${item.ownerName} ${item.phone} ${item.machineCode} ${item.message}`.toLowerCase();
    return !query || haystack.includes(query);
  });
  qs("#supportLiveBadge").textContent = `${number(rows.length)} chamado(s)`;
  target.innerHTML = rows.length
    ? rows.map((item) => supportCard(item)).join("")
    : `<div class="empty-row padded">Nenhum suporte aberto.</div>`;
}

function supportCard(item) {
  const profileName = item.businessName || item.customerName || item.ownerName || "Cliente";
  const location = [item.city, item.state].filter(Boolean).join(" / ");
  const messages = (item.messages && item.messages.length ? item.messages : [{ sender: "cliente", message: item.message, when: item.createdAt }])
    .map((message) => `
      <div class="chat-line ${message.sender === "admin" ? "admin" : "client"}">
        <strong>${message.sender === "admin" ? "Suporte" : "Cliente"}</strong>
        <span>${escapeHtml(message.message || "")}</span>
        <small>${dateTime(message.when)}</small>
      </div>
    `).join("");
  return `
    <article class="support-card ${item.priority === "URGENTE" ? "urgent" : ""}">
      <div class="support-top">
        <div>
          <strong>${escapeHtml(profileName)}</strong>
          <span>${escapeHtml(item.category || "Suporte")} - ${escapeHtml(item.priority || "NORMAL")} - ${dateTime(item.createdAt)}</span>
        </div>
        <span class="status ${item.status}">${escapeHtml(item.status || "ABERTO")}</span>
      </div>
      <div class="support-chat">${messages}</div>
      <div class="support-meta">
        <span>Protocolo ${escapeHtml(item.shortId || item.id || "-")}</span>
        <span>Chave ${escapeHtml(item.licenseKey || "-")}</span>
        <span>PC ${escapeHtml(item.machineCode || "-")}</span>
        <span>Telefone ${escapeHtml(item.phone || "-")}</span>
        <span>${escapeHtml(location || item.cnpj || "-")}</span>
      </div>
      ${item.adminNote ? `<small class="support-note">Nota: ${escapeHtml(item.adminNote)}</small>` : ""}
      <textarea id="reply-${item.id}" class="reply-box" rows="2" placeholder="Responder cliente"></textarea>
      <div class="row-actions support-actions">
        <button onclick="replySupport('${item.id}')">Responder</button>
        ${item.status === "ABERTO" ? `<button onclick="setSupportStatus('${item.id}', 'EM_ATENDIMENTO')">Atender</button>` : ""}
        ${item.status !== "RESOLVIDO" ? `<button class="secondary" onclick="setSupportStatus('${item.id}', 'RESOLVIDO')">Resolver</button>` : ""}
      </div>
    </article>
  `;
}

function renderLicenses() {
  const target = qs("#licensesTable");
  if (!target) return;
  const query = qs("#licenseSearch").value.trim().toLowerCase();
  const rows = state.licenses.filter((item) => {
    const profile = item.profile || {};
    const env = item.environmentSnapshot || item.environment || {};
    const haystack = `${item.key} ${item.customerName} ${item.businessName} ${item.cnpj} ${item.phone} ${item.email} ${item.address} ${item.city} ${item.state} ${profile.address} ${env.publicIp} ${env.primaryLocalIp} ${(env.localIpAddresses || []).join(" ")} ${env.machineName} ${env.windowsUser} ${env.clientProduct} ${item.machineCode} ${item.appVersion} ${item.clientKind}`.toLowerCase();
    return !query || haystack.includes(query);
  });
  qs("#licenseLiveBadge").textContent = `${number(rows.length)} licenca(s)`;
  target.innerHTML = rows.length
    ? rows.map((item) => {
      const env = item.environmentSnapshot || item.environment || {};
      const profile = item.profile || {};
      const address = item.address || profile.address || "";
      const cityState = [item.city || profile.city, item.state || profile.state].filter(Boolean).join(" / ");
      const publicIp = normalizedIp(env.publicIp) || "-";
      const localIps = Array.isArray(env.localIpAddresses) ? env.localIpAddresses : [];
      const localIp = env.primaryLocalIp || localIps[0] || "-";
      const product = env.clientProduct || clientKindLabel(item.clientKind || "windows");
      const os = compactOs(env.operatingSystem || "");
      const publicIpBlocked = isIpBlocked(publicIp);
      return `
      <tr>
        <td><span class="status ${item.status}">${escapeHtml(item.status || "-")}</span></td>
        <td>
          <strong>${escapeHtml(item.customerName || item.businessName || "-")}</strong>
          <small>${escapeHtml([item.cnpj, item.phone, item.email].filter(Boolean).join(" | ") || "-")}</small>
          <small>${escapeHtml(cityState || address || "-")}</small>
          ${address && address !== cityState ? `<small>${escapeHtml(address)}</small>` : ""}
        </td>
        <td class="key-cell">${escapeHtml(item.key || "-")}</td>
        <td>
          <strong>${escapeHtml(product)}</strong>
          <small>PC ${escapeHtml(item.machineCode || "-")} ${env.machineName ? `| ${escapeHtml(env.machineName)}` : ""}</small>
          <small>${escapeHtml([item.appVersion, os, env.windowsUser].filter(Boolean).join(" | ") || "-")}</small>
        </td>
        <td class="network-cell">
          <span class="info-chip ${publicIpBlocked ? "blocked-ip" : ""}">IP ${escapeHtml(publicIp)}</span>
          <small>Local ${escapeHtml(localIp)}</small>
          ${env.timeZone ? `<small>${escapeHtml(env.timeZone)} ${escapeHtml(env.utcOffset || "")}</small>` : ""}
        </td>
        <td>${dateTime(item.expiresAt)}</td>
        <td>${dateTime(item.lastSeenAt)}</td>
        <td><div class="row-actions">
          <button class="secondary" onclick="copyKey('${item.key}')">Copiar</button>
          ${item.status === "BLOQUEADA"
            ? `<button onclick="unblockLicense('${item.id}')">Liberar</button>`
            : `<button class="danger" onclick="blockLicense('${item.id}')">Bloquear</button>`}
          ${ipActionButton(publicIp, item.customerName || item.businessName || item.key || "licenca")}
        </div></td>
      </tr>
    `;
    }).join("")
    : `<tr><td colspan="8" class="empty-cell">Nenhuma licenca encontrada.</td></tr>`;
}

function renderDevices() {
  const target = qs("#devicesList");
  if (!target) return;
  const devices = state.dashboard?.recentDevices || [];
  qs("#devicesLiveBadge").textContent = `${number(devices.length)} cliente(s) recentes`;
  target.innerHTML = devices.length
    ? devices.map((item) => {
      const profile = item.profile || {};
      const metrics = item.metrics || {};
      const env = item.environment || {};
      const name = profile.businessName || profile.legalName || profile.ownerName || "Cliente";
      const addressParts = [profile.address, profile.city, profile.state].filter(Boolean);
      const localIps = Array.isArray(env.localIpAddresses) ? env.localIpAddresses : [];
      const publicIp = normalizedIp(env.publicIp);
      return `
        <article class="device-card client-card">
          <div class="client-card-head">
            <strong>${escapeHtml(name)}</strong>
            <span>${escapeHtml(clientKindLabel(item.clientKind || "windows"))}</span>
          </div>
          <dl>
            <div><dt>Maquina</dt><dd>${escapeHtml(item.machineCode || "-")}</dd></div>
            <div><dt>PC</dt><dd>${escapeHtml(env.machineName || "-")}</dd></div>
            <div><dt>Usuario</dt><dd>${escapeHtml(env.windowsUser || "-")}</dd></div>
            <div><dt>IP publico</dt><dd>${escapeHtml(publicIp || "-")}</dd></div>
            <div><dt>IP local</dt><dd>${escapeHtml(env.primaryLocalIp || localIps[0] || "-")}</dd></div>
            <div><dt>Usuarios</dt><dd>${number(metrics.usersCount)}</dd></div>
            <div><dt>CNPJ</dt><dd>${escapeHtml(profile.cnpj || "-")}</dd></div>
            <div><dt>Telefone</dt><dd>${escapeHtml(profile.phone || "-")}</dd></div>
            <div><dt>Endereco</dt><dd>${escapeHtml(addressParts.join(" - ") || "-")}</dd></div>
            <div><dt>Responsavel</dt><dd>${escapeHtml(profile.ownerName || "-")}</dd></div>
            <div><dt>Versao</dt><dd>${escapeHtml(item.appVersion || "-")}</dd></div>
            <div><dt>Windows</dt><dd>${escapeHtml(compactOs(env.operatingSystem || "-"))}</dd></div>
            <div><dt>Ultimo uso</dt><dd>${dateTime(item.lastSeenAt)}</dd></div>
          </dl>
          <div class="client-card-actions">
            ${ipActionButton(publicIp, name)}
          </div>
        </article>
      `;
    }).join("")
    : `<div class="empty-row padded">Nenhum app sincronizou ainda.</div>`;
}

function renderBlockedIps() {
  const target = qs("#blockedIpList");
  if (!target) return;
  const blockedIps = state.blockedIps || [];
  const badge = qs("#blockedIpLiveBadge");
  if (badge) badge.textContent = `${number(blockedIps.length)} bloqueado(s)`;
  target.innerHTML = blockedIps.length
    ? blockedIps.map((item) => `
      <div class="blocked-ip-row">
        <div>
          <strong>${escapeHtml(item.ip || "-")}</strong>
          <small>${escapeHtml(item.reason || "Bloqueado pelo admin")}</small>
          <small>${number(item.hits)} tentativa(s) barrada(s) ${item.lastBlockedAt ? `- ultima ${dateTime(item.lastBlockedAt)}` : ""}</small>
        </div>
        <button onclick='unblockIp(${jsArg(item.ip || "")})'>Liberar IP</button>
      </div>
    `).join("")
    : `<div class="empty-row padded">Nenhum IP bloqueado.</div>`;
}

function normalizedIp(value) {
  const clean = String(value || "").trim().replace(/^::ffff:/i, "");
  return clean && clean !== "-" ? clean : "";
}

function isIpBlocked(ip) {
  const clean = normalizedIp(ip).toLowerCase();
  if (!clean) return false;
  return (state.blockedIps || []).some((item) => normalizedIp(item.ip).toLowerCase() === clean);
}

function ipActionButton(ip, source) {
  const clean = normalizedIp(ip);
  if (!clean) {
    return "";
  }

  return isIpBlocked(clean)
    ? `<button onclick='unblockIp(${jsArg(clean)})'>Liberar IP</button>`
    : `<button class="danger" onclick='blockIp(${jsArg(clean)}, ${jsArg(source || "cliente")})'>Bloquear IP</button>`;
}

function jsArg(value) {
  return JSON.stringify(String(value || ""))
    .replaceAll("<", "\\u003c")
    .replaceAll(">", "\\u003e")
    .replaceAll("&", "\\u0026")
    .replaceAll("'", "\\u0027");
}

function clientKindLabel(value) {
  const kind = String(value || "").toLowerCase();
  if (kind.includes("online")) return "PDV Online";
  if (kind.includes("offline")) return "PDV Offline";
  if (kind.includes("android") || kind.includes("mobile")) return "Mobile";
  if (kind.includes("web") || kind.includes("browser")) return "Web";
  return "Windows";
}

function compactOs(value) {
  return String(value || "")
    .replace(/^Microsoft\s+/i, "")
    .replace(/\s+/g, " ")
    .trim();
}

async function createKey() {
  const license = await api("/api/licenses", {
    method: "POST",
    body: JSON.stringify({
      customerName: qs("#keyCustomer").value,
      plan: qs("#keyPlan").value,
      amount: Number(qs("#keyAmount").value || 30),
      unit: qs("#keyUnit").value,
      notes: qs("#keyNotes").value
    })
  });
  qs("#createdKey").classList.remove("hidden");
  qs("#createdKey").innerHTML = `
    <strong>Chave criada</strong>
    <code>${escapeHtml(license.key)}</code>
    <span>Expira em ${dateTime(license.expiresAt)}</span>
    <button class="secondary" onclick="copyKey('${license.key}')">Copiar chave</button>
  `;
  await loadRealtimeData({ notifySupport: false, force: true });
  setView("licenses");
}

async function blockLicense(id) {
  await api(`/api/licenses/${id}/block`, { method: "POST" });
  await loadRealtimeData({ notifySupport: false, force: true });
}

async function unblockLicense(id) {
  await api(`/api/licenses/${id}/unblock`, { method: "POST" });
  await loadRealtimeData({ notifySupport: false, force: true });
}

async function blockIp(ip, source) {
  const clean = normalizedIp(ip);
  if (!clean) return;
  if (!confirm(`Bloquear o IP ${clean}? O app desse IP nao vai ativar, sincronizar, publicar cardapio ou abrir suporte.`)) {
    return;
  }

  await api("/api/blocked-ips", {
    method: "POST",
    body: JSON.stringify({
      ip: clean,
      source: source || "admin",
      reason: `Bloqueado em usuarios: ${source || clean}`
    })
  });
  await loadRealtimeData({ notifySupport: false, force: true });
}

async function unblockIp(ip) {
  const clean = normalizedIp(ip);
  if (!clean) return;
  await api("/api/blocked-ips/delete", {
    method: "POST",
    body: JSON.stringify({ ip: clean })
  });
  await loadRealtimeData({ notifySupport: false, force: true });
}

async function setSupportStatus(id, status) {
  await api(`/api/support/${id}/status`, {
    method: "POST",
    body: JSON.stringify({ status })
  });
  await loadRealtimeData({ notifySupport: false, force: true });
  setView("support");
}

async function replySupport(id) {
  const input = document.getElementById(`reply-${id}`);
  const message = input?.value.trim() || "";
  if (!message) return;
  await api(`/api/support/${id}/reply`, {
    method: "POST",
    body: JSON.stringify({ message })
  });
  input.value = "";
  await loadRealtimeData({ notifySupport: false, force: true });
  setView("support");
}

function copyKey(key) {
  navigator.clipboard.writeText(key).catch(() => {});
}

function copyDownloadUrl(key) {
  const map = {
    trial: releaseDownloads.trialUrl,
    checkoutMonthly: releaseDownloads.checkoutMonthlyUrl,
    checkoutAnnual: releaseDownloads.checkoutAnnualUrl,
    installer: releaseDownloads.installerUrl,
    manifest: releaseDownloads.manifestUrl,
  };
  const value = map[key] || "";
  if (value) navigator.clipboard.writeText(value).catch(() => {});
}

function escapeHtml(value) {
  return String(value || "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

qs("#loginButton").addEventListener("click", login);
qs("#loginPassword").addEventListener("keydown", (event) => {
  if (event.key === "Enter") login();
});
qs("#logoutButton").addEventListener("click", logout);
qs("#createKeyButton").addEventListener("click", createKey);
qs("#licenseSearch").addEventListener("input", renderLicenses);
qs("#supportSearch").addEventListener("input", renderSupport);
document.querySelectorAll(".nav[data-view]").forEach((button) => {
  button.addEventListener("click", () => setView(button.dataset.view));
});

api("/api/health")
  .then((health) => {
    state.health = health;
    renderRealtimeBadge();
  })
  .catch(() => {});

api("/api/session")
  .then((session) => {
    if (session.authenticated) {
      showApp();
      startLiveRefresh();
      return loadRealtimeData({ notifySupport: false, force: true });
    }
    showLogin();
  })
  .catch(showLogin);

renderDownloads();
