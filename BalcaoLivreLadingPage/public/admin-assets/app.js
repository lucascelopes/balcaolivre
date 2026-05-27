const state = {
  dashboard: null,
  licenses: [],
  support: [],
  health: null,
  currentView: "dashboard"
};
let liveTimer = null;
let lastSupportCustomerMessageAt = "";
const supportPollMs = 30000;

const qs = (selector) => document.querySelector(selector);
const dateTime = (value) => value ? new Date(value).toLocaleString("pt-BR") : "-";
const adminApiBase = (() => {
  const configured = window.BALCAO_ADMIN_API_BASE || "";
  if (configured) return configured.replace(/\/$/, "");
  return location.pathname.startsWith("/admin") ? "/admin-api" : "";
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
      data = {
        message: response.ok
          ? text
          : "API do admin indisponivel. Verifique se o servidor local esta aberto."
      };
    }
  }
  if (!response.ok) {
    throw new Error(data.message || "Erro na requisicao.");
  }
  return data;
}

function showLogin() {
  qs("#loginView").classList.remove("hidden");
  qs("#appView").classList.add("hidden");
}

function showApp() {
  qs("#loginView").classList.add("hidden");
  qs("#appView").classList.remove("hidden");
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
    await refreshAll();
    startLiveRefresh();
  } catch (error) {
    qs("#loginMessage").textContent = error.message;
  }
}

async function logout() {
  await api("/api/logout", { method: "POST" }).catch(() => {});
  if (liveTimer) {
    clearInterval(liveTimer);
    liveTimer = null;
  }
  showLogin();
}

function setView(view) {
  state.currentView = view;
  document.querySelectorAll(".view").forEach((item) => item.classList.add("hidden"));
  qs(`#${view}View`).classList.remove("hidden");
  document.querySelectorAll(".nav").forEach((item) => item.classList.toggle("active", item.dataset.view === view));
  const titles = {
    dashboard: ["Dashboard", "Uso do programa, chaves e clientes ativos."],
    licenses: ["Licencas", "Chaves criadas, status, maquina vinculada e vencimento."],
    support: ["Suporte", "Conversas do PDV por consulta economica."],
    devices: ["Clientes", "Dados cadastrais sincronizados pelo Balcao Livre PDV."],
    keys: ["Criar chave", "Gere uma licenca unica por periodo."]
  };
  qs("#viewTitle").textContent = titles[view][0];
  qs("#viewSubtitle").textContent = titles[view][1];
}

async function refreshAll() {
  const [dashboard, licenses, support, health] = await Promise.all([
    api("/api/dashboard"),
    api("/api/licenses"),
    api("/api/support"),
    api("/api/health")
  ]);
  state.dashboard = dashboard;
  state.licenses = licenses;
  state.support = support;
  lastSupportCustomerMessageAt = newestCustomerMessageAt(support);
  state.health = health;
  renderDashboard();
  renderLicenses();
  renderSupport();
  renderDevices();
}

async function refreshLive() {
  if (qs("#appView").classList.contains("hidden")) return;
  const previousCustomerMessageAt = lastSupportCustomerMessageAt;
  const [dashboard, support] = await Promise.all([
    api("/api/dashboard"),
    api("/api/support")
  ]);
  state.dashboard = dashboard;
  state.support = support;
  lastSupportCustomerMessageAt = newestCustomerMessageAt(support);
  notifySupportChange(previousCustomerMessageAt, lastSupportCustomerMessageAt);
  renderDashboard();
  renderSupport();
  renderDevices();
}

function startLiveRefresh() {
  if (liveTimer) return;
  if ("Notification" in window && Notification.permission === "default") {
    Notification.requestPermission().catch(() => {});
  }
  liveTimer = setInterval(() => {
    refreshLive().catch((error) => console.warn("live refresh failed", error));
  }, supportPollMs);
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
  const metrics = state.dashboard.metrics;
  qs("#mActive").textContent = metrics.activeLicenses;
  qs("#mAvailable").textContent = metrics.availableLicenses;
  qs("#mOnline").textContent = metrics.online24h;
  qs("#mUsers").textContent = metrics.registeredUsers;
  qs("#mDevices").textContent = metrics.devices;
  qs("#mSupport").textContent = metrics.openSupport || 0;
  const storageMode = state.health?.storage || "supabase-nao-configurado";
  qs("#storageMode").textContent = storageMode === "supabase"
    ? "Supabase ativo"
    : storageMode === "supabase-pendente"
      ? "Supabase pendente"
      : storageMode === "supabase-nao-configurado"
        ? "Supabase nao configurado"
        : "Local JSON";
  qs("#storageMode").classList.toggle("online", storageMode === "supabase");
  qs("#storageMode").classList.toggle("pending", storageMode === "supabase-pendente");
  qs("#storageMode").classList.toggle("error", storageMode === "supabase-nao-configurado");
  qs("#expiringList").innerHTML = state.dashboard.expiringSoon.length
    ? state.dashboard.expiringSoon.map((item) => `
      <div><strong>${escapeHtml(item.customerName || item.businessName || "Cliente")}</strong>
      <small>${item.key} - expira ${dateTime(item.expiresAt)}</small></div>`).join("")
    : `<div>Nenhuma licenca vencendo nos proximos 15 dias.</div>`;
  qs("#versionsList").innerHTML = state.dashboard.versionDistribution.length
    ? state.dashboard.versionDistribution.map((item) => `<div><strong>${escapeHtml(item.version)}</strong><small>${item.count} maquina(s)</small></div>`).join("")
    : `<div>Nenhuma maquina sincronizada ainda.</div>`;
  qs("#eventsList").innerHTML = state.dashboard.events.length
    ? state.dashboard.events.map((item) => `<div><strong>${escapeHtml(item.message)}</strong><small>${escapeHtml(item.type)} - ${dateTime(item.when)}</small></div>`).join("")
    : `<div>Sem eventos ainda.</div>`;
}

function renderSupport() {
  const target = qs("#supportList");
  if (!target) return;
  const query = qs("#supportSearch").value.trim().toLowerCase();
  const rows = state.support.filter((item) => {
    const haystack = `${item.shortId} ${item.licenseKey} ${item.customerName} ${item.businessName} ${item.ownerName} ${item.phone} ${item.machineCode} ${item.message}`.toLowerCase();
    return !query || haystack.includes(query);
  });
  qs("#supportLiveBadge").textContent = `${rows.length} chamado(s) - consulta a cada ${supportPollMs / 1000}s`;
  target.innerHTML = rows.length
    ? rows.map((item) => supportCard(item)).join("")
    : `<div class="device-card">Nenhum suporte aberto.</div>`;
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
  const query = qs("#licenseSearch").value.trim().toLowerCase();
  const rows = state.licenses.filter((item) => {
    const haystack = `${item.key} ${item.customerName} ${item.businessName} ${item.cnpj} ${item.machineCode}`.toLowerCase();
    return !query || haystack.includes(query);
  });
  qs("#licensesTable").innerHTML = rows.map((item) => `
    <tr>
      <td><span class="status ${item.status}">${item.status}</span></td>
      <td><strong>${escapeHtml(item.customerName || item.businessName || "-")}</strong><small>${escapeHtml(item.cnpj || item.phone || "")}</small></td>
      <td class="key-cell">${item.key}</td>
      <td>${escapeHtml(item.machineCode || "-")}<small>${escapeHtml(item.appVersion || "")}</small></td>
      <td>${dateTime(item.expiresAt)}</td>
      <td>${dateTime(item.lastSeenAt)}</td>
      <td><div class="row-actions">
        <button class="secondary" onclick="copyKey('${item.key}')">Copiar</button>
        ${item.status === "BLOQUEADA"
          ? `<button onclick="unblockLicense('${item.id}')">Liberar</button>`
          : `<button class="danger" onclick="blockLicense('${item.id}')">Bloquear</button>`}
      </div></td>
    </tr>
  `).join("");
}

function renderDevices() {
  const devices = state.dashboard.recentDevices || [];
  qs("#devicesList").innerHTML = devices.length
    ? devices.map((item) => {
      const profile = item.profile || {};
      const metrics = item.metrics || {};
      const name = profile.businessName || profile.legalName || profile.ownerName || "Cliente";
      const addressParts = [profile.address, profile.city, profile.state].filter(Boolean);
      return `
        <article class="device-card client-card">
          <strong>${escapeHtml(name)}</strong>
          <span>Maquina: ${escapeHtml(item.machineCode || "-")}</span>
          <span>Usuarios do app: ${Number(metrics.usersCount || 0)}</span>
          <span>CNPJ: ${escapeHtml(profile.cnpj || "-")}</span>
          <span>Telefone: ${escapeHtml(profile.phone || "-")}</span>
          <span>Endereco: ${escapeHtml(addressParts.join(" - ") || "-")}</span>
          <span>Responsavel: ${escapeHtml(profile.ownerName || "-")}</span>
          <span>Versao: ${escapeHtml(item.appVersion || "-")}</span>
          <small>Ultima vez mexido: ${dateTime(item.lastSeenAt)}</small>
        </article>
      `;
    }).join("")
    : `<div class="device-card">Nenhum app sincronizou ainda.</div>`;
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
    <code>${license.key}</code>
    <span>Expira em ${dateTime(license.expiresAt)}</span>
    <button class="secondary" onclick="copyKey('${license.key}')">Copiar chave</button>
  `;
  await refreshAll();
  setView("licenses");
}

async function blockLicense(id) {
  await api(`/api/licenses/${id}/block`, { method: "POST" });
  await refreshAll();
}

async function unblockLicense(id) {
  await api(`/api/licenses/${id}/unblock`, { method: "POST" });
  await refreshAll();
}

async function setSupportStatus(id, status) {
  await api(`/api/support/${id}/status`, {
    method: "POST",
    body: JSON.stringify({ status })
  });
  await refreshAll();
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
  await refreshAll();
  setView("support");
}

function copyKey(key) {
  navigator.clipboard.writeText(key).catch(() => {});
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
qs("#refreshButton").addEventListener("click", refreshAll);
qs("#createKeyButton").addEventListener("click", createKey);
qs("#licenseSearch").addEventListener("input", renderLicenses);
qs("#supportSearch").addEventListener("input", renderSupport);
document.querySelectorAll(".nav[data-view]").forEach((button) => {
  button.addEventListener("click", () => setView(button.dataset.view));
});

api("/api/session")
  .then((session) => {
    if (session.authenticated) {
      showApp();
      startLiveRefresh();
      return refreshAll();
    }
    showLogin();
  })
  .catch(showLogin);
