const state = {
  dashboard: null,
  licenses: [],
  health: null,
  currentView: "dashboard"
};

const qs = (selector) => document.querySelector(selector);
const money = (value) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value || 0);
const dateTime = (value) => value ? new Date(value).toLocaleString("pt-BR") : "-";

async function api(path, options = {}) {
  const response = await fetch(path, {
    credentials: "same-origin",
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    ...options
  });
  if (response.status === 401) {
    showLogin();
    throw new Error("Nao autenticado.");
  }
  const text = await response.text();
  const data = text ? JSON.parse(text) : {};
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
  } catch (error) {
    qs("#loginMessage").textContent = error.message;
  }
}

async function logout() {
  await api("/api/logout", { method: "POST" }).catch(() => {});
  showLogin();
}

function setView(view) {
  state.currentView = view;
  document.querySelectorAll(".view").forEach((item) => item.classList.add("hidden"));
  qs(`#${view}View`).classList.remove("hidden");
  document.querySelectorAll(".nav").forEach((item) => item.classList.toggle("active", item.dataset.view === view));
  const titles = {
    dashboard: ["Dashboard", "Metricas de uso, chaves e clientes ativos."],
    licenses: ["Licencas", "Chaves criadas, status, maquina vinculada e vencimento."],
    devices: ["Clientes", "Dados cadastrais sincronizados pelo Balcao Livre PDV."],
    keys: ["Criar chave", "Gere uma licenca unica por periodo."]
  };
  qs("#viewTitle").textContent = titles[view][0];
  qs("#viewSubtitle").textContent = titles[view][1];
}

async function refreshAll() {
  const [dashboard, licenses, health] = await Promise.all([
    api("/api/dashboard"),
    api("/api/licenses"),
    api("/api/health")
  ]);
  state.dashboard = dashboard;
  state.licenses = licenses;
  state.health = health;
  renderDashboard();
  renderLicenses();
  renderDevices();
}

function renderDashboard() {
  const metrics = state.dashboard.metrics;
  qs("#mActive").textContent = metrics.activeLicenses;
  qs("#mAvailable").textContent = metrics.availableLicenses;
  qs("#mOnline").textContent = metrics.online24h;
  qs("#mSales").textContent = money(metrics.salesToday);
  qs("#storageMode").textContent = state.health?.storage === "supabase"
    ? "Supabase ativo"
    : state.health?.storage === "supabase-pendente"
      ? "Supabase pendente"
      : "Local JSON";
  qs("#storageMode").classList.toggle("online", state.health?.storage === "supabase");
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
      const name = profile.businessName || profile.legalName || profile.ownerName || "Cliente";
      const addressParts = [profile.address, profile.city, profile.state].filter(Boolean);
      return `
        <article class="device-card client-card">
          <strong>${escapeHtml(name)}</strong>
          <span>CNPJ: ${escapeHtml(profile.cnpj || "-")}</span>
          <span>Telefone: ${escapeHtml(profile.phone || "-")}</span>
          <span>Endereco: ${escapeHtml(addressParts.join(" - ") || "-")}</span>
          <span>Responsavel: ${escapeHtml(profile.ownerName || "-")}</span>
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
document.querySelectorAll(".nav[data-view]").forEach((button) => {
  button.addEventListener("click", () => setView(button.dataset.view));
});

api("/api/session")
  .then((session) => {
    if (session.authenticated) {
      showApp();
      return refreshAll();
    }
    showLogin();
  })
  .catch(showLogin);
