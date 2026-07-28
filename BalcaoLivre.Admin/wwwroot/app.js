const state = {
  dashboard: null,
  clientIntelligence: null,
  licenses: [],
  fulfillments: [],
  support: [],
  blockedIps: [],
  health: null,
  currentView: "dashboard",
  realtimeStatus: "booting",
  realtimeSnapshot: null,
  isRefreshing: false,
  refreshQueued: false,
  lastSyncAt: null,
  dashboardRange: 30,
  selectedClientId: "",
  clientHealthFilter: "all",
  clientHistoryOpen: false,
  selectedLicenseId: "",
  licenseFilter: "all",
  licenseActionNotice: "",
  selectedSupportId: "",
  supportFilter: "all",
  visitLeads: [],
  selectedVisitId: "",
  visitOutcome: "undecided",
  includedNearbyVisits: [],
  showAllNearbyVisits: false,
  visitsRouteOptimized: false,
  visitPanel: "route",
  userLocation: null,
  locationMode: "idle",
  locationAccuracy: null,
  locationUpdatedAt: null,
  nearbyLoading: false,
  routeGeometry: [],
  routeDistanceMeters: 0,
  routeDurationSeconds: 0,
  routeLoading: false,
  routeSource: "local",
  routeOrder: [],
  aiRouteSummary: "Ative sua localização e adicione oportunidades reais para planejar a rota.",
  aiRouteReasons: {},
  adminProfile: {
    email: "",
    name: "Administrador",
    firstName: "Administrador",
    initials: "AD"
  }
};

let liveTimer = null;
let realtimeSource = null;
let realtimeFallbackTimer = null;
let refreshAllQueued = null;
let lastSupportCustomerMessageAt = "";
let revenueChart = null;
let operationsChartInstance = null;
let targetGaugeChartInstance = null;
let licenseStatusChartInstance = null;
let supportResolutionChartInstance = null;
let clientActivityChartInstance = null;
let visitsMapInstance = null;
let visitsMapLayers = null;
let visitsNoticeTimer = null;
let visitGeolocationWatchId = null;
let visitsRouteRequestId = 0;

const livePollMs = 3000;
const realtimeFallbackPollMs = 5000;
const releaseDownloads = {
  version: "1.8.2026.29",
  publishedAt: "2026-07-28T12:00:00-03:00",
  installerUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.29.exe",
  trialUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/trial-download?plan=online",
  checkoutMonthlyUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout?plan=basico-mensal",
  checkoutAnnualUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout?plan=basico-anual",
  manifestUrl: "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/version.json"
};

const qs = (selector) => document.querySelector(selector);
const dateTime = (value) => value ? new Date(value).toLocaleString("pt-BR") : "-";
const dateOnly = (value) => value ? new Date(value).toLocaleDateString("pt-BR") : "-";
const timeOnly = (value) => value ? new Date(value).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit", second: "2-digit" }) : "-";
const number = (value) => Number(value || 0).toLocaleString("pt-BR");
const moneyFromCents = (value, currency = "BRL") =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: currency || "BRL" }).format((Number(value) || 0) / 100);
const wholeMoneyFromCents = (value, currency = "BRL") =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: currency || "BRL",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format((Number(value) || 0) / 100);
const isLocalPreview = false;
const adminApiBase = (() => {
  const configured = window.BALCAO_ADMIN_API_BASE || "";
  if (configured) return configured.replace(/\/$/, "");
  const adminSubdomain = location.hostname === "admin.balcaolivrepdv.com.br" || location.hostname.startsWith("admin.");
  return location.pathname.startsWith("/admin") || adminSubdomain ? "/admin-api" : "";
})();

function adminApiPath(path) {
  if (!adminApiBase) return path;
  const normalized = path.startsWith("/api/") ? path.slice(4) : path;
  return `${adminApiBase}/${normalized.replace(/^\/+/, "")}`;
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

function adminGreetingForNow() {
  const hour = new Date().getHours();
  if (hour < 12) return "Bom dia";
  if (hour < 18) return "Boa tarde";
  return "Boa noite";
}

function inferAdminName(email) {
  const knownNames = {
    "lucascesar@admin.com": "Lucas Cesar",
    "isabelagomes@admin.com": "Isabela Gomes"
  };
  return knownNames[String(email || "").toLowerCase()] || "Administrador";
}

function renderAdminProfile(payload = {}) {
  const supplied = payload.profile || {};
  const email = String(supplied.email || payload.user || "").toLowerCase();
  const name = String(supplied.name || inferAdminName(email)).trim() || "Administrador";
  const words = name.split(/\s+/).filter(Boolean);
  const firstName = String(supplied.firstName || words[0] || "Administrador");
  const initials = String(
    supplied.initials ||
    words.slice(0, 2).map((word) => word[0]?.toUpperCase() || "").join("") ||
    "AD"
  );

  state.adminProfile = { email, name, firstName, initials };
  const avatar = qs("#adminProfileAvatar");
  const profileName = qs("#adminProfileName");
  const greeting = qs("#adminGreeting");
  if (avatar) avatar.textContent = initials;
  if (profileName) profileName.textContent = name;
  if (greeting) greeting.textContent = `${adminGreetingForNow()}, ${firstName}`;
  const visitOwner = qs("#manualVisitOwner");
  if (visitOwner && [...visitOwner.options].some((option) => option.value === name)) {
    visitOwner.value = name;
  }
}

async function login() {
  qs("#loginMessage").textContent = "";
  try {
    const result = await api("/api/login", {
      method: "POST",
      body: JSON.stringify({
        user: qs("#loginUser").value,
        password: qs("#loginPassword").value
      })
    });
    renderAdminProfile(result);
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
  const content = qs(".content");
  if (content) content.dataset.view = view;
  document.querySelectorAll(".view").forEach((item) => item.classList.add("hidden"));
  qs(`#${view}View`).classList.remove("hidden");
  document.querySelectorAll(".nav").forEach((item) => item.classList.toggle("active", item.dataset.view === view));
  const titles = {
    dashboard: ["Visão geral", "Operação, vendas, clientes e licenças em tempo real."],
    licenses: ["Licenças", "Antecipe vencimentos, acompanhe ativações e mantenha a operação em dia."],
    visits: ["Visitas presenciais", "Conduza o funil e aproveite cada deslocamento."],
    support: ["Suporte", "Priorize chamados, responda clientes e acompanhe cada resolução."],
    devices: ["Clientes", "Saiba quem está saudável, quem precisa de atenção e qual ação tomar."],
    downloads: ["Downloads", "Instaladores, checkout e manifesto de atualizacao."],
    keys: ["Criar chave", "Gere uma licenca unica por periodo."]
  };
  qs("#viewTitle").textContent = titles[view][0];
  qs("#viewSubtitle").textContent = titles[view][1];
  qs(".admin-profile")?.removeAttribute("open");
  if (view === "visits") {
    renderManualVisits();
  }
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
    const [dashboard, licenses, fulfillments, support, health, blockedIps] = await Promise.all([
      api("/api/dashboard"),
      api("/api/licenses"),
      api("/api/fulfillments").catch(() => []),
      api("/api/support"),
      api("/api/health"),
      api("/api/blocked-ips")
    ]);

    state.dashboard = dashboard;
    state.clientIntelligence = dashboard.clientIntelligence || state.clientIntelligence;
    state.licenses = Array.isArray(licenses) ? licenses : [];
    state.fulfillments = Array.isArray(fulfillments) ? fulfillments : [];
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
  renderManualVisits();
  renderSupport();
  renderDevices();
  renderBlockedIps();
  renderDownloads();
  renderRealtimeBadge();
}

const visitStageMeta = [
  { id: "mapped", label: "Mapeados" },
  { id: "scheduled", label: "Agendados" },
  { id: "visited", label: "Visitados" },
  { id: "interested", label: "Interessados" }
];

const nearbyVisitOptions = [];
let nearbySearchCenter = null;

function ensureVisitData() {
  if (state.visitLeads.length) return;
  state.selectedVisitId = "";
  state.visitOutcome = "undecided";
  state.includedNearbyVisits = [];
  state.showAllNearbyVisits = false;
  state.visitsRouteOptimized = false;
  state.routeOrder = [];
  try {
    localStorage.removeItem("balcao-livre-visits-v2");
  } catch {
    // O painel continua funcional mesmo quando o navegador bloqueia armazenamento local.
  }
}

function persistVisitData() {
  try {
    localStorage.setItem("balcao-livre-visits-v2", JSON.stringify({
      visitLeads: state.visitLeads,
      selectedVisitId: state.selectedVisitId,
      visitOutcome: state.visitOutcome,
      includedNearbyVisits: state.includedNearbyVisits,
      showAllNearbyVisits: state.showAllNearbyVisits,
      visitsRouteOptimized: state.visitsRouteOptimized,
      routeOrder: state.routeOrder
    }));
  } catch {
    // O painel continua funcional mesmo quando o navegador bloqueia armazenamento local.
  }
}

function getSelectedVisitLead() {
  return state.visitLeads.find((item) => item.id === state.selectedVisitId) || state.visitLeads[0] || null;
}

function captureVisitDraft() {
  const lead = getSelectedVisitLead();
  const notes = qs("#visitNotes");
  if (lead && notes) lead.notes = notes.value;
}

function visitOutcomeLabel(value) {
  return {
    "not-interested": "Não interessado",
    undecided: "Indeciso",
    interested: "Interessado",
    trial: "Em teste"
  }[value] || "Indeciso";
}

function legacyRenderVisits() {
  const pipeline = qs("#visitsPipeline");
  const funnel = qs("#visitsFunnel");
  if (!pipeline || !funnel) return;

  ensureVisitData();
  const selected = getSelectedVisitLead();
  const tests = state.visitLeads.filter((item) => item.trialCreated).length;
  const funnelSteps = [
    { value: state.visitLeads.filter((item) => item.stage === "mapped").length, label: "mapeados" },
    { value: state.visitLeads.filter((item) => item.stage === "scheduled").length, label: "agendados" },
    { value: state.visitLeads.filter((item) => item.stage === "visited").length, label: "visitados" },
    { value: state.visitLeads.filter((item) => item.stage === "interested").length, label: "interessados" },
    { value: tests, label: "testes" }
  ];

  funnel.innerHTML = funnelSteps.map((item, index) => `
    <div class="visits-funnel-step">
      <strong>${item.value}</strong>
      <span>${item.label}</span>
    </div>
    ${index < funnelSteps.length - 1 ? '<i class="fa-solid fa-arrow-right" aria-hidden="true"></i>' : ""}
  `).join("");

  pipeline.innerHTML = visitStageMeta.map((stage) => {
    const rows = state.visitLeads.filter((item) => item.stage === stage.id);
    return `
      <section class="visits-stage" aria-label="${escapeHtml(stage.label)}">
        <header class="visits-stage-head">
          <strong>${escapeHtml(stage.label)} <span>${stage.count}</span></strong>
          <i class="fa-solid fa-chevron-right" aria-hidden="true"></i>
        </header>
        <div class="visits-stage-list">
          ${rows.map((item) => `
            <button class="visits-lead ${item.id === state.selectedVisitId ? "active" : ""}" type="button" onclick="selectVisitLead('${escapeHtml(item.id)}')">
              <span class="visits-lead-title">
                <strong>${escapeHtml(item.name)}</strong>
                <i class="fa-solid fa-ellipsis-vertical" aria-hidden="true"></i>
              </span>
              <span class="visits-lead-line">
                <span><i class="fa-solid fa-location-dot" aria-hidden="true"></i>${escapeHtml(item.neighborhood)}</span>
                <time>${escapeHtml(item.distance)}</time>
              </span>
              <span class="visits-lead-line">
                <span><i class="fa-regular fa-calendar" aria-hidden="true"></i>${escapeHtml(item.action)}</span>
                <time>${escapeHtml(item.when)}</time>
              </span>
            </button>
          `).join("")}
        </div>
        <button class="visits-stage-footer" type="button" onclick="showVisitsNotice('${stage.count} oportunidades em ${escapeHtml(stage.label.toLowerCase())}.')">Ver todos (${stage.count})</button>
      </section>
    `;
  }).join("");

  const selectedName = qs("#visitSelectedBusiness");
  const selectedMeta = qs("#visitSelectedMeta");
  const notes = qs("#visitNotes");
  const score = qs("#visitInterestScore");
  if (selected) {
    selectedName.textContent = selected.name;
    selectedMeta.textContent = `${selected.neighborhood} · ${selected.distance}`;
    notes.value = selected.notes || "";
    score.textContent = `${selected.score >= 80 ? "Alto" : selected.score >= 60 ? "Médio" : "Em análise"} · ${selected.score}%`;
    state.visitOutcome = selected.outcome || state.visitOutcome;
  } else {
    qs("#visitSelectedBusiness").textContent = "Nenhuma visita selecionada";
    qs("#visitSelectedMeta").textContent = "Os dados aparecerão quando houver oportunidades reais.";
    qs("#visitNotes").value = "";
    qs("#visitInterestScore").textContent = "Sem dados";
  }

  document.querySelectorAll("[data-visit-outcome]").forEach((button) => {
    button.classList.toggle("active", button.dataset.visitOutcome === state.visitOutcome);
  });

  renderVisitsNearby();
  renderVisitsRouteMetrics();

  const trialButton = qs("#generateVisitTrial");
  if (selected?.trialCreated) {
    trialButton.textContent = "Teste criado até 03/08";
    trialButton.disabled = true;
  } else {
    trialButton.textContent = "Gerar teste de 7 dias";
    trialButton.disabled = false;
  }

  if (visitsMapInstance) refreshVisitsMap();
}

function legacyRenderVisitsNearby() {
  const list = qs("#visitsNearbyList");
  if (!list) return;
  const visible = nearbyVisitOptions.slice(0, state.showAllNearbyVisits ? nearbyVisitOptions.length : 3);
  list.innerHTML = visible.map((item) => {
    const included = state.includedNearbyVisits.includes(item.id);
    return `
      <div class="visits-nearby-row">
        <div>
          <strong>${escapeHtml(item.name)}</strong>
          <span>${escapeHtml(item.distance)}</span>
        </div>
        <button class="${included ? "included" : ""}" type="button" onclick="toggleNearbyVisit('${escapeHtml(item.id)}')">
          ${included ? "Na rota" : "Incluir na rota"}
        </button>
      </div>
    `;
  }).join("");

  const showAll = qs("#showAllNearbyVisits");
  showAll.innerHTML = state.showAllNearbyVisits
    ? 'Mostrar menos <i class="fa-solid fa-chevron-up" aria-hidden="true"></i>'
    : 'Ver mais opções <i class="fa-solid fa-chevron-right" aria-hidden="true"></i>';
}

function legacyRenderVisitsRouteMetrics() {
  const includedCount = state.includedNearbyVisits.length;
  const count = Math.max(1, 1 + includedCount);
  const distance = state.visitsRouteOptimized ? 7.4 : 5.8 + includedCount * 0.8;
  const minutes = state.visitsRouteOptimized ? 118 : 93 + includedCount * 14;
  qs("#visitsRouteCount").textContent = String(count);
  qs("#visitsRouteDistance").textContent = `${distance.toFixed(1).replace(".", ",")} km`;
  qs("#visitsRouteTime").textContent = `${Math.floor(minutes / 60)}h${String(minutes % 60).padStart(2, "0")}`;
  qs("#optimizeVisitsRoute").textContent = state.visitsRouteOptimized ? "Rota otimizada" : "Otimizar rota";
}

function legacySelectVisitLead(id) {
  captureVisitDraft();
  const next = state.visitLeads.find((item) => item.id === id);
  if (!next) return;
  state.selectedVisitId = id;
  state.visitOutcome = next.outcome || "undecided";
  persistVisitData();
  renderVisits();
  if (visitsMapInstance && Array.isArray(next.coordinates)) {
    visitsMapInstance.panTo(next.coordinates, { animate: true });
  }
}

function setVisitOutcome(outcome) {
  captureVisitDraft();
  const lead = getSelectedVisitLead();
  state.visitOutcome = outcome;
  if (lead) lead.outcome = outcome;
  persistVisitData();
  renderVisits();
}

function legacyToggleNearbyVisit(id) {
  captureVisitDraft();
  const index = state.includedNearbyVisits.indexOf(id);
  if (index >= 0) {
    state.includedNearbyVisits.splice(index, 1);
  } else {
    state.includedNearbyVisits.push(id);
  }
  state.visitsRouteOptimized = false;
  persistVisitData();
  renderVisits();
}

function toggleAllNearbyVisits() {
  captureVisitDraft();
  state.showAllNearbyVisits = !state.showAllNearbyVisits;
  persistVisitData();
  renderVisits();
}

function legacyOptimizeVisitsRoute() {
  captureVisitDraft();
  state.visitsRouteOptimized = !state.visitsRouteOptimized;
  persistVisitData();
  renderVisits();
  showVisitsNotice(state.visitsRouteOptimized
    ? "Rota otimizada: 44 minutos e 2,1 km economizados hoje."
    : "Rota original restaurada.");
}

function generateVisitTrial() {
  captureVisitDraft();
  const lead = getSelectedVisitLead();
  if (!lead || lead.trialCreated) return;
  lead.trialCreated = true;
  lead.outcome = "trial";
  lead.action = "Teste ativo até 03/08";
  state.visitOutcome = "trial";
  persistVisitData();
  renderVisits();
  showVisitsNotice(`Teste de 7 dias criado para ${lead.name}. Válido até 03/08/2026.`);
}

function saveVisitAndContinue() {
  captureVisitDraft();
  const current = getSelectedVisitLead();
  if (current) {
    current.outcome = state.visitOutcome;
    current.savedAt = new Date().toISOString();
  }
  const next = state.visitLeads.find((item) => item.id !== state.selectedVisitId && item.stage === "scheduled");
  if (next) {
    state.selectedVisitId = next.id;
    state.visitOutcome = next.outcome || "undecided";
  }
  persistVisitData();
  renderVisits();
  showVisitsNotice(current ? `Visita de ${current.name} salva. Próxima parada carregada.` : "Visita salva.");
}

function showVisitsNotice(message) {
  const notice = qs("#visitsNotice");
  if (!notice) return;
  notice.textContent = message;
  notice.classList.remove("hidden");
  window.clearTimeout(visitsNoticeTimer);
  visitsNoticeTimer = window.setTimeout(() => notice.classList.add("hidden"), 3600);
}

function legacyInitVisitsMap() {
  const mapElement = qs("#visitsMap");
  if (!mapElement || visitsMapInstance) return;
  if (!window.L) {
    mapElement.innerHTML = '<div class="visits-map-error">Não foi possível carregar o mapa agora.</div>';
    return;
  }

  visitsMapInstance = window.L.map(mapElement, {
    zoomControl: true,
    attributionControl: true,
    scrollWheelZoom: true
  });

  window.L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors'
  }).addTo(visitsMapInstance);

  refreshVisitsMap();
}

function legacyRefreshVisitsMap() {
  if (!visitsMapInstance || !window.L) return;
  if (visitsMapLayers) visitsMapLayers.remove();
  visitsMapLayers = window.L.layerGroup().addTo(visitsMapInstance);

  const currentPosition = [-23.5621, -46.6572];
  const selected = getSelectedVisitLead();
  const selectedPoint = {
    id: selected?.id || "visit-unavailable",
    name: selected?.name || "Nenhuma visita selecionada",
    coordinates: Array.isArray(selected?.coordinates) ? selected.coordinates : [-23.5677, -46.6641]
  };
  const includedPoints = state.includedNearbyVisits
    .map((id) => nearbyVisitOptions.find((item) => item.id === id))
    .filter(Boolean);
  const points = [selectedPoint, ...includedPoints];
  const routePoints = [currentPosition, ...points.map((item) => item.coordinates)];

  window.L.polyline(routePoints, {
    color: "#FC601D",
    weight: 4,
    opacity: .95,
    lineJoin: "round"
  }).addTo(visitsMapLayers);

  points.forEach((point, index) => {
    const marker = window.L.circleMarker(point.coordinates, {
      radius: index === 0 ? 9 : 7,
      color: "#ffffff",
      weight: 2,
      fillColor: "#FC601D",
      fillOpacity: 1
    }).addTo(visitsMapLayers);
    marker.bindTooltip(`${index + 1} · ${escapeHtml(point.name)}`, {
      permanent: true,
      direction: index % 2 ? "right" : "top",
      offset: [0, -7]
    });
    if (point.id.startsWith("visit-")) {
      marker.on("click", () => selectVisitLead(point.id));
    }
  });

  window.L.marker(currentPosition, {
    icon: window.L.divIcon({
      className: "visit-current-position",
      html: '<i class="fa-solid fa-location-arrow" aria-hidden="true"></i>',
      iconSize: [27, 27],
      iconAnchor: [13, 13]
    })
  }).bindTooltip("Você está aqui", { direction: "bottom", offset: [0, 14] }).addTo(visitsMapLayers);

  visitsMapInstance.fitBounds(window.L.latLngBounds(routePoints), {
    padding: [34, 34],
    maxZoom: 14
  });
}

// Planejador de visitas v3: mapa primeiro, rota real e IA apenas para priorização.
function getVisitCurrentPosition() {
  return Array.isArray(state.userLocation) ? state.userLocation : [-23.5621, -46.6572];
}

function buildVisitRouteStops() {
  const selected = getSelectedVisitLead();
  const included = state.includedNearbyVisits
    .map((id) => nearbyVisitOptions.find((item) => item.id === id))
    .filter(Boolean);
  const unique = [
    selected && Array.isArray(selected.coordinates) ? { ...selected } : null,
    ...included
  ].filter(Boolean).filter((item, index, rows) => rows.findIndex((row) => row.name === item.name) === index);
  const order = state.routeOrder.length ? state.routeOrder : unique.map((item) => item.id);
  return [...unique].sort((a, b) => {
    const aIndex = order.indexOf(a.id);
    const bIndex = order.indexOf(b.id);
    return (aIndex < 0 ? 999 : aIndex) - (bIndex < 0 ? 999 : bIndex);
  });
}

function renderVisits() {
  const funnel = qs("#visitsFunnel");
  if (!funnel) return;
  ensureVisitData();
  const selected = getSelectedVisitLead();
  const tests = state.visitLeads.filter((item) => item.trialCreated).length;
  const funnelSteps = [
    { value: state.visitLeads.filter((item) => item.stage === "mapped").length, label: "mapeados" },
    { value: state.visitLeads.filter((item) => item.stage === "scheduled").length, label: "agendados" },
    { value: state.visitLeads.filter((item) => item.stage === "visited").length, label: "visitados" },
    { value: state.visitLeads.filter((item) => item.stage === "interested").length, label: "interessados" },
    { value: tests, label: "testes" }
  ];
  funnel.innerHTML = funnelSteps.map((item, index) => `
    <div class="visits-funnel-step"><strong>${item.value}</strong><span>${item.label}</span></div>
    ${index < funnelSteps.length - 1 ? '<i class="fa-solid fa-chevron-right" aria-hidden="true"></i>' : ""}
  `).join("");

  if (selected) {
    qs("#visitSelectedBusiness").textContent = selected.name;
    qs("#visitSelectedMeta").textContent = `${selected.neighborhood} · ${selected.distance}`;
    qs("#visitNotes").value = selected.notes || "";
    qs("#visitInterestScore").textContent =
      `${selected.score >= 80 ? "Alto" : selected.score >= 60 ? "Médio" : "Em análise"} · ${selected.score}%`;
    state.visitOutcome = selected.outcome || state.visitOutcome;
  } else {
    qs("#visitSelectedBusiness").textContent = "Nenhuma visita selecionada";
    qs("#visitSelectedMeta").textContent = "Os dados aparecerão quando houver oportunidades reais.";
    qs("#visitNotes").value = "";
    qs("#visitInterestScore").textContent = "Sem dados";
  }
  document.querySelectorAll("[data-visit-outcome]").forEach((button) => {
    button.classList.toggle("active", button.dataset.visitOutcome === state.visitOutcome);
  });
  renderVisitsNearby();
  renderVisitsRouteMetrics();
  renderVisitRouteList();
  renderVisitPanel();
  renderVisitLocationStatus();

  const trialButton = qs("#generateVisitTrial");
  if (selected?.trialCreated) {
    trialButton.textContent = "Teste criado até 03/08";
    trialButton.disabled = true;
  } else {
    trialButton.textContent = "Gerar teste de 7 dias";
    trialButton.disabled = false;
  }
  if (visitsMapInstance) refreshVisitsMap();
}

function renderVisitsNearby() {
  const list = qs("#visitsNearbyList");
  if (!list) return;
  const visible = nearbyVisitOptions.slice(0, state.showAllNearbyVisits ? nearbyVisitOptions.length : 3);
  list.innerHTML = visible.map((item) => {
    const included = state.includedNearbyVisits.includes(item.id);
    return `
      <div class="visits-nearby-row">
        <div><strong>${escapeHtml(item.name)}</strong><span>${escapeHtml(item.neighborhood)} · ${escapeHtml(item.distance)}</span></div>
        <button class="${included ? "included" : ""}" type="button" onclick="toggleNearbyVisit('${escapeHtml(item.id)}')">
          <i class="fa-solid ${included ? "fa-check" : "fa-plus"}" aria-hidden="true"></i>
          ${included ? "Na rota" : "Adicionar"}
        </button>
      </div>
    `;
  }).join("");
  if (state.nearbyLoading) {
    list.innerHTML = '<div class="visits-empty"><i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i> Buscando restaurantes e comércios reais por perto...</div>';
  } else if (!visible.length) {
    list.innerHTML = `<div class="visits-empty">${state.locationMode === "live"
      ? "Nenhum estabelecimento foi encontrado nesta região."
      : "Ative a localização para buscar estabelecimentos reais por perto."}</div>`;
  }
  qs("#showAllNearbyVisits").textContent = state.nearbyLoading
    ? "Buscando locais..."
    : visible.length
    ? (state.showAllNearbyVisits ? "Mostrar menos" : "Ver todas")
    : "Nenhum local carregado";
  qs("#showAllNearbyVisits").disabled = state.nearbyLoading || !visible.length;
}

function renderVisitsRouteMetrics() {
  const count = buildVisitRouteStops().length;
  const distance = Math.max(0, state.routeDistanceMeters || 0) / 1000;
  const minutes = count
    ? Math.max(1, Math.round((state.routeDurationSeconds || 0) / 60) + count * 22)
    : 0;
  qs("#visitsRouteCount").textContent = String(count);
  qs("#visitsRouteDistance").textContent = `${distance.toFixed(1).replace(".", ",")} km`;
  qs("#visitsRouteTime").textContent = `${Math.floor(minutes / 60)}h${String(minutes % 60).padStart(2, "0")}`;
  const button = qs("#optimizeVisitsRoute");
  button.disabled = state.routeLoading;
  button.innerHTML = state.routeLoading
    ? '<i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i> Calculando'
    : '<i class="fa-solid fa-wand-magic-sparkles" aria-hidden="true"></i> Recalcular plano';
}

function renderVisitRouteList() {
  const list = qs("#visitRouteList");
  if (!list) return;
  const stops = buildVisitRouteStops();
  let elapsedMinutes = 9;
  list.innerHTML = stops.map((item, index) => {
    elapsedMinutes += 8 + index * 3;
    const eta = new Date(2026, 6, 27, 13, elapsedMinutes)
      .toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
    const reason = state.aiRouteReasons[item.id]
      || (Number(item.score || 0) >= 75 ? "Alto potencial e perto da rota" : "Boa oportunidade com pouco desvio");
    const action = item.id.startsWith("visit-")
      ? `selectVisitLead('${escapeHtml(item.id)}')`
      : `focusNearbyVisit('${escapeHtml(item.id)}')`;
    return `
      <li class="visits-route-stop ${item.id === state.selectedVisitId ? "active" : ""}">
        <button type="button" onclick="${action}">
          <span class="visits-stop-number">${index + 1}</span>
          <span class="visits-stop-copy">
            <strong>${escapeHtml(item.name)}</strong>
            <small>${escapeHtml(item.neighborhood || "São Paulo")} · ${escapeHtml(reason)}</small>
          </span>
          <span class="visits-stop-time"><strong>${eta}</strong><small>${escapeHtml(item.distance || "na rota")}</small></span>
        </button>
      </li>
    `;
  }).join("");
  qs("#visitAiSummary").textContent = state.aiRouteSummary;
  qs("#visitAiSource").textContent = state.routeSource === "openrouter" ? "IA assistiva" : "Plano local";
}

function focusNearbyVisit(id) {
  const item = nearbyVisitOptions.find((entry) => entry.id === id);
  if (!item) return;
  visitsMapInstance?.panTo(item.coordinates, { animate: true });
  showVisitsNotice(`${item.name} destacado no mapa.`);
}

function selectVisitLead(id) {
  captureVisitDraft();
  const next = state.visitLeads.find((item) => item.id === id);
  if (!next) return;
  state.selectedVisitId = id;
  state.visitOutcome = next.outcome || "undecided";
  state.visitPanel = "register";
  persistVisitData();
  renderVisits();
  if (visitsMapInstance && Array.isArray(next.coordinates)) visitsMapInstance.panTo(next.coordinates, { animate: true });
}

function toggleNearbyVisit(id) {
  captureVisitDraft();
  const index = state.includedNearbyVisits.indexOf(id);
  if (index >= 0) state.includedNearbyVisits.splice(index, 1);
  else state.includedNearbyVisits.push(id);
  state.routeOrder = [];
  state.routeSource = "local";
  persistVisitData();
  renderVisits();
  calculateVisitRoute();
}

function optimizeVisitsRoute() {
  captureVisitDraft();
  if (!buildVisitRouteStops().length) {
    state.routeLoading = false;
    state.routeSource = "local";
    state.routeOrder = [];
    state.routeGeometry = [];
    state.routeDistanceMeters = 0;
    state.routeDurationSeconds = 0;
    state.aiRouteSummary = "Ative a localização e adicione uma oportunidade para a IA montar a rota.";
    renderVisitsRouteMetrics();
    renderVisitRouteList();
    showVisitsNotice("Adicione ao menos uma oportunidade antes de calcular com a IA.");
    return Promise.resolve();
  }
  state.routeLoading = true;
  renderVisitsRouteMetrics();
  return planVisitsWithAi().then(calculateVisitRoute);
}

function setVisitPanel(panel) {
  state.visitPanel = panel === "register" ? "register" : "route";
  renderVisitPanel();
}

function renderVisitPanel() {
  const register = state.visitPanel === "register";
  qs("#visitRoutePanel")?.classList.toggle("hidden", register);
  qs("#visitRegisterPanel")?.classList.toggle("hidden", !register);
  document.querySelectorAll("[data-visit-panel]").forEach((button) => {
    const active = button.dataset.visitPanel === state.visitPanel;
    button.classList.toggle("active", active);
    button.setAttribute("aria-selected", String(active));
  });
}

function renderVisitLocationStatus() {
  const live = state.locationMode === "live";
  qs("#visitLocationDot")?.classList.toggle("demo", !live);
  qs("#visitLocationTitle").textContent = live ? "Localização ao vivo" : "Localização desativada";
  qs("#visitLocationMeta").textContent = live
    ? `Atualizada ${state.locationUpdatedAt ? timeOnly(state.locationUpdatedAt) : "agora"}${state.locationAccuracy ? ` · precisão ${Math.round(state.locationAccuracy)} m` : ""}`
    : "Ative para planejar a partir de onde você está";
  qs("#enableVisitLocation").innerHTML = live
    ? '<i class="fa-solid fa-satellite-dish" aria-hidden="true"></i> Localização ativa'
    : '<i class="fa-solid fa-location-crosshairs" aria-hidden="true"></i> Ativar localização';
}

function enableVisitLocation() {
  if (!navigator.geolocation) {
    showVisitsNotice("Este navegador não oferece localização em tempo real.");
    return;
  }
  if (visitGeolocationWatchId !== null) {
    showVisitsNotice("A localização ao vivo já está ativa.");
    return;
  }
  qs("#visitLocationMeta").textContent = "Aguardando sua permissão...";
  visitGeolocationWatchId = navigator.geolocation.watchPosition((position) => {
    const nextLocation = [position.coords.latitude, position.coords.longitude];
    state.userLocation = nextLocation;
    state.locationMode = "live";
    state.locationAccuracy = position.coords.accuracy;
    state.locationUpdatedAt = new Date().toISOString();
    renderVisitLocationStatus();
    const movedMeters = nearbySearchCenter
      ? haversineDistance(nearbySearchCenter, nextLocation)
      : Number.POSITIVE_INFINITY;
    if (!nearbyVisitOptions.length || movedMeters > 1200) {
      loadNearbyVisitOptions();
    } else {
      calculateVisitRoute();
    }
  }, (error) => {
    visitGeolocationWatchId = null;
    showVisitsNotice(error.code === 1
      ? "Permissão de localização não concedida."
      : "Não foi possível obter sua localização agora.");
    renderVisitLocationStatus();
  }, { enableHighAccuracy: true, maximumAge: 15000, timeout: 12000 });
}

async function loadNearbyVisitOptions() {
  if (state.locationMode !== "live" || !Array.isArray(state.userLocation) || state.nearbyLoading) return;
  const [latitude, longitude] = state.userLocation;
  state.nearbyLoading = true;
  nearbySearchCenter = [...state.userLocation];
  renderVisitsNearby();
  try {
    const result = await api(
      `/api/visits/nearby?latitude=${encodeURIComponent(latitude)}&longitude=${encodeURIComponent(longitude)}`
    );
    const opportunities = Array.isArray(result.opportunities) ? result.opportunities : [];
    nearbyVisitOptions.splice(0, nearbyVisitOptions.length, ...opportunities.map((item) => ({
      id: item.id,
      name: item.name,
      neighborhood: item.neighborhood || "Região próxima",
      category: item.category || "estabelecimento",
      distanceMeters: Number(item.distanceMeters || 0),
      distance: `${(Number(item.distanceMeters || 0) / 1000).toFixed(1).replace(".", ",")} km`,
      score: Number(item.score || 60),
      stage: item.stage || "mapped",
      coordinates: [Number(item.latitude), Number(item.longitude)]
    })).filter((item) =>
      item.id &&
      item.name &&
      item.coordinates.every(Number.isFinite)
    ));

    state.includedNearbyVisits = nearbyVisitOptions.slice(0, 4).map((item) => item.id);
    state.routeOrder = [];
    state.routeSource = "local";
    persistVisitData();
    renderVisits();
    if (nearbyVisitOptions.length) {
      showVisitsNotice(`${nearbyVisitOptions.length} oportunidades reais encontradas. A IA está priorizando a rota.`);
      await optimizeVisitsRoute();
    } else {
      showVisitsNotice(result.message || "Nenhuma oportunidade encontrada nesta região.");
    }
  } catch (error) {
    nearbyVisitOptions.splice(0, nearbyVisitOptions.length);
    state.includedNearbyVisits = [];
    state.aiRouteSummary = "Não foi possível buscar estabelecimentos próximos agora.";
    showVisitsNotice(error.message || "Falha ao buscar oportunidades próximas.");
  } finally {
    state.nearbyLoading = false;
    renderVisits();
  }
}

function haversineDistance(a, b) {
  const radians = (value) => value * Math.PI / 180;
  const earth = 6371000;
  const dLat = radians(b[0] - a[0]);
  const dLon = radians(b[1] - a[1]);
  const value = Math.sin(dLat / 2) ** 2
    + Math.cos(radians(a[0])) * Math.cos(radians(b[0])) * Math.sin(dLon / 2) ** 2;
  return earth * 2 * Math.atan2(Math.sqrt(value), Math.sqrt(1 - value));
}

function deterministicVisitOrder(stops) {
  const remaining = [...stops];
  const ordered = [];
  let cursor = getVisitCurrentPosition();
  while (remaining.length) {
    remaining.sort((a, b) => {
      const aCost = haversineDistance(cursor, a.coordinates) - Number(a.score || 60) * 12;
      const bCost = haversineDistance(cursor, b.coordinates) - Number(b.score || 60) * 12;
      return aCost - bCost;
    });
    const next = remaining.shift();
    ordered.push(next);
    cursor = next.coordinates;
  }
  return ordered;
}

async function calculateVisitRoute() {
  const requestId = ++visitsRouteRequestId;
  const localOrder = deterministicVisitOrder(buildVisitRouteStops());
  if (!state.routeOrder.length || state.routeSource !== "openrouter") state.routeOrder = localOrder.map((item) => item.id);
  const ordered = buildVisitRouteStops();
  const points = [getVisitCurrentPosition(), ...ordered.map((item) => item.coordinates)];
  if (points.length < 2) {
    state.routeLoading = false;
    state.routeGeometry = [];
    state.routeDistanceMeters = 0;
    state.routeDurationSeconds = 0;
    renderVisitsRouteMetrics();
    renderVisitRouteList();
    refreshVisitsMap();
    return;
  }
  state.routeLoading = true;
  renderVisitsRouteMetrics();
  qs("#visitMapStatus").innerHTML = '<i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i> Calculando rota pelas ruas';
  try {
    const coordinates = points.map(([lat, lon]) => `${lon},${lat}`).join(";");
    const response = await fetch(`https://router.project-osrm.org/route/v1/driving/${coordinates}?overview=full&geometries=geojson&steps=false`);
    if (!response.ok) throw new Error("OSRM indisponível");
    const route = (await response.json()).routes?.[0];
    if (!route) throw new Error("Rota não encontrada");
    if (requestId !== visitsRouteRequestId) return;
    state.routeGeometry = route.geometry.coordinates.map(([lon, lat]) => [lat, lon]);
    state.routeDistanceMeters = route.distance;
    state.routeDurationSeconds = route.duration;
    state.visitsRouteOptimized = true;
    qs("#visitMapStatus").innerHTML = '<i class="fa-solid fa-road" aria-hidden="true"></i> OpenStreetMap · rota pelas ruas';
  } catch {
    if (requestId !== visitsRouteRequestId) return;
    state.routeGeometry = points;
    state.routeDistanceMeters = points.slice(1).reduce((total, point, index) => total + haversineDistance(points[index], point), 0);
    state.routeDurationSeconds = Math.max(900, state.routeDistanceMeters / 7.5);
    qs("#visitMapStatus").innerHTML = '<i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i> Rota aproximada · OSRM indisponível';
  } finally {
    if (requestId === visitsRouteRequestId) {
      state.routeLoading = false;
      persistVisitData();
      renderVisitsRouteMetrics();
      renderVisitRouteList();
      refreshVisitsMap();
    }
  }
}

async function planVisitsWithAi() {
  const stops = deterministicVisitOrder(buildVisitRouteStops());
  state.routeOrder = stops.map((item) => item.id);
  state.routeSource = "local";
  state.aiRouteSummary = "Priorizamos potencial comercial, proximidade e menor desvio da rota.";
  state.aiRouteReasons = Object.fromEntries(stops.map((item) => [
    item.id,
    Number(item.score || 0) >= 75 ? "Alto potencial com pouco deslocamento" : "Oportunidade eficiente na rota"
  ]));
  if (!stops.length) return;
  if (isLocalPreview) {
    showVisitsNotice("Plano local aplicado. A IA entra quando o OpenRouter estiver configurado no servidor.");
    return;
  }
  try {
    const response = await fetch(adminApiPath("/api/visits/plan"), {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        currentLocation: { latitude: getVisitCurrentPosition()[0], longitude: getVisitCurrentPosition()[1] },
        candidates: stops.map((item) => ({
          id: item.id,
          name: item.name,
          neighborhood: item.neighborhood || "",
          latitude: item.coordinates[0],
          longitude: item.coordinates[1],
          score: Number(item.score || 60),
          stage: item.stage || "mapped",
          distanceMeters: Math.round(haversineDistance(getVisitCurrentPosition(), item.coordinates))
        }))
      })
    });
    if (!response.ok) throw new Error("IA indisponível");
    const result = await response.json();
    const validIds = (Array.isArray(result.orderedIds) ? result.orderedIds : [])
      .filter((id) => stops.some((item) => item.id === id));
    state.routeOrder = [...new Set([...validIds, ...stops.map((item) => item.id)])];
    state.routeSource = "openrouter";
    state.aiRouteSummary = result.summary || state.aiRouteSummary;
    state.aiRouteReasons = Object.fromEntries((result.reasons || []).map((item) => [item.id, item.reason]));
    showVisitsNotice("A IA reorganizou as visitas. A rota foi recalculada pelas ruas.");
  } catch {
    showVisitsNotice("IA indisponível agora. Aplicamos o plano local por distância e potencial.");
  }
}

function refreshVisitsMap() {
  if (!visitsMapInstance || !window.L) return;
  if (visitsMapLayers) visitsMapLayers.remove();
  visitsMapLayers = window.L.layerGroup().addTo(visitsMapInstance);
  const currentPosition = getVisitCurrentPosition();
  const points = buildVisitRouteStops();
  if (state.locationMode !== "live" && !points.length) {
    visitsMapInstance.setView([-14.235, -51.9253], 4);
    return;
  }
  const fallbackRoute = [currentPosition, ...points.map((item) => item.coordinates)];
  const routePoints = state.routeGeometry.length ? state.routeGeometry : fallbackRoute;
  window.L.polyline(routePoints, {
    color: "#FC601D", weight: 5, opacity: .9, lineJoin: "round"
  }).addTo(visitsMapLayers);
  points.forEach((point, index) => {
    const marker = window.L.circleMarker(point.coordinates, {
      radius: 10, color: "#ffffff", weight: 2, fillColor: "#FC601D", fillOpacity: 1
    }).addTo(visitsMapLayers);
    marker.bindTooltip(`${index + 1} · ${escapeHtml(point.name)}`, { direction: "top", offset: [0, -9] });
    if (point.id.startsWith("visit-")) marker.on("click", () => selectVisitLead(point.id));
  });
  window.L.marker(currentPosition, {
    icon: window.L.divIcon({
      className: "visit-current-position",
      html: '<i class="fa-solid fa-location-arrow" aria-hidden="true"></i>',
      iconSize: [27, 27],
      iconAnchor: [13, 13]
    })
  }).bindTooltip(state.locationMode === "live" ? "Você está aqui" : "Localização ainda não ativada", {
    direction: "bottom", offset: [0, 14]
  }).addTo(visitsMapLayers);
  visitsMapInstance.fitBounds(window.L.latLngBounds(fallbackRoute), { padding: [34, 34], maxZoom: 14 });
}

function initVisitsMap() {
  const mapElement = qs("#visitsMap");
  if (!mapElement || visitsMapInstance) return;
  if (!window.L) {
    mapElement.innerHTML = '<div class="visits-map-error">Não foi possível carregar o mapa agora.</div>';
    return;
  }
  visitsMapInstance = window.L.map(mapElement, {
    zoomControl: true, attributionControl: true, scrollWheelZoom: true
  });
  window.L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors'
  }).addTo(visitsMapInstance);
  refreshVisitsMap();
  calculateVisitRoute();
}

function startVisitsRoute() {
  const next = buildVisitRouteStops()[0];
  if (next) showVisitsNotice(`Rota iniciada. Próxima parada: ${next.name}.`);
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
  // O admin roda em Cloudflare Workers e usa atualizacao periodica para nao
  // manter uma requisicao SSE aberta na borda.
  startRealtimeFallback();
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

  qs("#mActive").textContent = number(metrics.totalLicenses ?? metrics.activeLicenses);
  qs("#mAvailable").textContent = number(metrics.expiringSoon30d ?? metrics.availableLicenses);
  qs("#mOnline").textContent = number(metrics.online24h);
  qs("#mUsers").textContent = number(metrics.registeredUsers);
  const waitingSupport = metrics.waitingSupport ?? state.support.filter((item) => item.status !== "RESOLVIDO").length;
  qs("#mDevices").textContent = number(waitingSupport);
  qs("#mSupport").textContent = number(metrics.openSupport);
  const openSupport = metrics.supportOpen ?? state.support.filter((item) => item.status === "ABERTO").length;
  const inProgressSupport = metrics.supportInProgress ?? state.support.filter((item) => item.status === "EM_ATENDIMENTO").length;
  qs("#mSupportBreakdown").textContent = `${number(openSupport)} abertos • ${number(inProgressSupport)} em andamento`;
  qs("#mSiteVisitors").textContent = `${number(metrics.siteVisitors24h)} / ${number(metrics.siteVisitorsTotal)}`;
  qs("#mSiteViews").textContent = `${number(metrics.siteViews24h)} / ${number(metrics.siteViewsTotal)}`;
  qs("#mCheckoutStarted").textContent = `${number(metrics.checkoutStarted24h)} / ${number(metrics.checkoutStartedTotal)}`;
  qs("#mStripePurchases").textContent = `${number(metrics.stripePurchases24h)} / ${number(metrics.stripePurchasesTotal)}`;
  const monthRevenueCents = Number(metrics.stripeRevenueMonthCents ?? metrics.stripeRevenueCents ?? 0);
  const previousRevenueCents = Number(metrics.stripeRevenuePreviousMonthCents || 0);
  qs("#mStripeRevenue").textContent = wholeMoneyFromCents(monthRevenueCents, stripe.currency || "BRL");
  qs("#mConversion").textContent = `${Number(metrics.stripeConversionRate || 0).toLocaleString("pt-BR")}%`;
  renderRevenueComparison(monthRevenueCents, previousRevenueCents, stripe.currency || "BRL");
  renderRadarUpdatedAt();
  renderOperationsChart(stripe.revenueByDay || []);
  renderTargetGauge(monthRevenueCents, stripe.currency || "BRL");
  renderOperationalFunnels();
  renderLicenseStatusChart();
  renderSupportResolutionChart();
  renderSupportHeatmap();
  renderRadarRisks();

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

function renderRevenueComparison(currentCents, previousCents, currency) {
  const comparison = qs("#revenueComparison");
  const previous = qs("#revenuePrevious");
  if (!comparison || !previous) return;

  if (previousCents > 0) {
    const change = ((currentCents - previousCents) * 100) / previousCents;
    const sign = change >= 0 ? "+" : "";
    comparison.textContent = `${sign}${change.toLocaleString("pt-BR", { maximumFractionDigits: 1 })}%`;
    previous.textContent = `(${wholeMoneyFromCents(previousCents, currency)})`;
    return;
  }

  comparison.textContent = `${number(state.dashboard?.metrics?.stripePurchasesTotal || 0)} compras`;
  previous.textContent = "processadas no período";
}

function renderRadarUpdatedAt() {
  const value = state.lastSyncAt || new Date();
  const formatted = value.toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  });
  qs("#radarUpdatedAt").textContent = formatted;
  qs("#radarReferenceAt").textContent = formatted;
}

function renderOperationsChart(revenueByDay) {
  const canvas = qs("#operationsChart");
  if (!canvas || typeof Chart === "undefined") return;
  document.querySelectorAll("[data-dashboard-range]").forEach((button) => {
    button.classList.toggle("active", Number(button.dataset.dashboardRange) === state.dashboardRange);
  });

  const activeByDay = state.dashboard?.activeClientsByDay || state.dashboard?.operations?.activeClientsByDay || [];
  const revenueMap = new Map(
    (Array.isArray(revenueByDay) ? revenueByDay : []).map((item) => [String(item.date || ""), Number(item.revenueCents || 0)])
  );
  const activeMap = new Map(
    (Array.isArray(activeByDay) ? activeByDay : []).map((item) => [String(item.date || ""), Number(item.count || 0)])
  );
  const availableDates = [...new Set([...revenueMap.keys(), ...activeMap.keys()])].filter(Boolean).sort();
  const fallbackDate = new Date().toISOString().slice(0, 10);
  const dates = (availableDates.length ? availableDates : [fallbackDate]).slice(-state.dashboardRange);
  const revenueValues = dates.map((date) => revenueMap.has(date) ? revenueMap.get(date) / 100 : 0);
  const activeValues = dates.map((date) => activeMap.has(date) ? activeMap.get(date) : null);
  const dateRange = qs("#operationsDateRange");
  if (dateRange && dates.length) {
    const formatDate = (value) => {
      const [year, month, day] = value.split("-");
      return `${day}/${month}/${year}`;
    };
    dateRange.textContent = `${formatDate(dates[0])} – ${formatDate(dates[dates.length - 1])}`;
  }

  if (!activeValues.some((value) => Number(value) > 0)) {
    activeValues[activeValues.length - 1] = Number(state.dashboard?.metrics?.online24h || 0);
  }

  const labels = dates.map((value) => {
    const [, month, day] = value.split("-");
    return `${day}/${month}`;
  });

  if (operationsChartInstance) operationsChartInstance.destroy();
  operationsChartInstance = new Chart(canvas, {
    data: {
      labels,
      datasets: [
        {
          type: "bar",
          label: "Receita",
          data: revenueValues,
          yAxisID: "revenue",
          backgroundColor: "#FC601D",
          borderColor: "#FC601D",
          borderWidth: 0,
          borderRadius: 0,
          barPercentage: .56,
          categoryPercentage: .78
        },
        {
          type: "line",
          label: "Clientes ativos",
          data: activeValues,
          yAxisID: "clients",
          borderColor: "#1F1D1B",
          backgroundColor: "#1F1D1B",
          borderWidth: 1.7,
          pointRadius: (context) => context.raw == null ? 0 : 2.2,
          pointHoverRadius: 4,
          tension: .24,
          spanGaps: true
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: { duration: 360 },
      interaction: { intersect: false, mode: "index" },
      plugins: {
        legend: { display: false },
        tooltip: {
          displayColors: true,
          backgroundColor: "#171717",
          callbacks: {
            label: (context) => context.dataset.yAxisID === "revenue"
              ? ` Receita: ${moneyFromCents(Number(context.raw || 0) * 100)}`
              : ` Clientes ativos: ${number(context.raw)}`
          }
        }
      },
      scales: {
        x: {
          border: { display: false },
          grid: { display: false },
          ticks: {
            color: "#77706B",
            font: { size: 8 },
            maxRotation: 0,
            autoSkip: true,
            maxTicksLimit: state.dashboardRange <= 7 ? 7 : 8
          }
        },
        revenue: {
          beginAtZero: true,
          position: "left",
          border: { display: false },
          grid: { color: "#E7E0DA" },
          ticks: {
            color: "#77706B",
            font: { size: 8 },
            callback: (value) => new Intl.NumberFormat("pt-BR", { notation: "compact", maximumFractionDigits: 1 }).format(value)
          }
        },
        clients: {
          beginAtZero: true,
          position: "right",
          border: { display: false },
          grid: { display: false },
          ticks: { color: "#77706B", font: { size: 8 }, precision: 0 }
        }
      }
    }
  });
}

function renderTargetGauge(currentCents, currency) {
  const canvas = qs("#targetGaugeChart");
  if (!canvas || typeof Chart === "undefined") return;

  const metrics = state.dashboard?.metrics || {};
  const targetCents = Math.max(1, Number(metrics.revenueTargetCents || 5000000));
  const percent = Math.min(100, Math.max(0, (currentCents * 100) / targetCents));
  const missing = Math.max(0, targetCents - currentCents);
  qs("#targetPercent").textContent = `${Math.round(percent)}%`;
  qs("#targetCurrent").textContent = wholeMoneyFromCents(currentCents, currency);
  qs("#targetGap").textContent = missing > 0
    ? `Faltam ${wholeMoneyFromCents(missing, currency)} para a meta`
    : "Meta mensal alcançada";

  if (targetGaugeChartInstance) targetGaugeChartInstance.destroy();
  targetGaugeChartInstance = new Chart(canvas, {
    type: "doughnut",
    data: {
      datasets: [{
        data: [percent, Math.max(0, 100 - percent)],
        backgroundColor: ["#FC601D", "#E8E1DB"],
        borderWidth: 0,
        circumference: 220,
        rotation: 250
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      cutout: "72%",
      animation: { duration: 360 },
      plugins: {
        legend: { display: false },
        tooltip: { enabled: false }
      }
    }
  });
}

function renderOperationalFunnels() {
  const metrics = state.dashboard?.metrics || {};
  const totalLicenses = Number(metrics.totalLicenses ?? metrics.activeLicenses ?? 0);
  const visitors = Number(metrics.siteVisitorsTotal || 0);
  const checkouts = Number(metrics.checkoutStartedTotal || 0);
  const payments = Number(metrics.stripePurchasesTotal || 0);
  const created = Number(metrics.licensesCreatedFromPayments ?? Math.min(totalLicenses, payments));
  const firstAccess = Number(metrics.firstAccessFromPayments ?? Math.min(Number(metrics.devices || 0), created));
  const active = Number(metrics.activeLicenses || 0);
  const renew30 = Number(metrics.expiringSoon30d || 0);
  const renewed = Number(metrics.renewedThisMonth || 0);
  const fulfillments = Array.isArray(state.fulfillments) ? state.fulfillments : [];
  const waiting = fulfillments.filter((item) =>
    ["WAITING_ADDRESS", "READY", "REQUESTED"].includes(String(item.status || "").toUpperCase()));
  const shipped = fulfillments.filter((item) => String(item.status || "").toUpperCase() === "SHIPPED");
  const delivered = fulfillments.filter((item) => String(item.status || "").toUpperCase() === "DELIVERED");

  const deliveryStages = [
    { label: "Aguardando envio", value: waiting.length },
    { label: "Enviado", value: shipped.length },
    { label: "Entregue", value: delivered.length }
  ];
  const deliveryTotal = Math.max(1, fulfillments.length);
  const fulfillmentRows = fulfillments
    .filter((item) => String(item.status || "").toUpperCase() !== "CANCELED")
    .slice(0, 4)
    .map((item) => {
      const status = String(item.status || "").toUpperCase();
      const store = item.bl_stores || {};
      const account = store.bl_accounts || {};
      const customer = item.recipient_name || account.display_name || store.name || "Cliente";
      const statusLabel = status === "SHIPPED"
        ? "Enviado"
        : status === "DELIVERED"
          ? "Entregue"
          : "Aguardando envio";
      const action = status === "SHIPPED"
        ? `<button type="button" onclick="updateFulfillmentStatus('${item.id}', 'DELIVERED')">Marcar entregue</button>`
        : status === "DELIVERED"
          ? ""
          : `<button type="button" onclick="updateFulfillmentStatus('${item.id}', 'SHIPPED')">Marcar enviado</button>`;
      return `
        <div class="fulfillment-row">
          <span><strong>${escapeHtml(customer)}</strong><small>${escapeHtml(statusLabel)}${item.tracking_code ? ` • ${escapeHtml(item.tracking_code)}` : ""}</small></span>
          ${action}
        </div>`;
    }).join("");

  qs("#deliveryFunnel").innerHTML = deliveryStages.map((item) => `
    <div class="delivery-stage">
      <div><b>${escapeHtml(item.label)}</b><strong>${number(item.value)}</strong></div>
      <small>${formatPercent(item.value, deliveryTotal)}</small>
    </div>
  `).join("") + (fulfillmentRows
    ? `<div class="fulfillment-list">${fulfillmentRows}</div>`
    : `<div class="empty-row">Nenhuma maquininha pendente de envio.</div>`);

  renderCompactFunnel("#acquisitionFunnel", [
    ["Visitas no site", visitors],
    ["Iniciaram checkout", checkouts],
    ["Pagamento aprovado", payments]
  ], "Taxa de conversão");
  renderCompactFunnel("#activationFunnel", [
    ["Pagamento aprovado", payments],
    ["Chave criada", created],
    ["Primeiro acesso", firstAccess]
  ], "Taxa de ativação");
  renderCompactFunnel("#retentionFunnel", [
    ["Clientes ativos", active],
    ["Renova em 30 dias", renew30],
    ["Renovada", renewed]
  ], "Taxa de renovação");
}

async function updateFulfillmentStatus(id, status) {
  let trackingCode = "";
  if (status === "SHIPPED") {
    trackingCode = window.prompt("Código de rastreio (opcional):", "") || "";
  }
  await api(`/api/fulfillments/${id}/status`, {
    method: "POST",
    body: JSON.stringify({ status, trackingCode })
  });
  await loadRealtimeData({ notifySupport: false, force: true });
}

function renderCompactFunnel(selector, rows, summaryLabel) {
  const target = qs(selector);
  if (!target) return;
  const firstValue = Math.max(1, Number(rows[0]?.[1] || 0));
  const lastValue = Number(rows.at(-1)?.[1] || 0);
  target.innerHTML = rows.map(([label, value]) => {
    const width = Math.max(value > 0 ? 4 : 0, Math.min(100, (Number(value || 0) * 100) / firstValue));
    return `
      <div class="funnel-row">
        <span>${escapeHtml(label)}</span>
        <strong>${number(value)}</strong>
        <span class="funnel-bar-track"><i class="funnel-bar-fill" style="width:${width.toFixed(1)}%"></i></span>
        <small>${formatPercent(value, firstValue)}</small>
      </div>
    `;
  }).join("") + `
    <div class="funnel-total"><span>${escapeHtml(summaryLabel)}</span><b>${formatPercent(lastValue, firstValue)}</b></div>
  `;
}

function formatPercent(value, total) {
  if (!Number(total)) return "0%";
  return `${((Number(value || 0) * 100) / Number(total)).toLocaleString("pt-BR", { maximumFractionDigits: 1 })}%`;
}

function renderLicenseStatusChart() {
  const canvas = qs("#licenseStatusChart");
  const legend = qs("#licenseStatusLegend");
  if (!canvas || !legend || typeof Chart === "undefined") return;
  const metrics = state.dashboard?.metrics || {};
  const expiring = Number(metrics.expiringSoon30d || 0);
  const healthy = Number(metrics.healthyLicenses ?? Math.max(0, Number(metrics.activeLicenses || 0) - expiring));
  const expired = Number(metrics.expiredLicenses || 0);
  const blocked = Number(metrics.blockedLicenses || 0);
  const values = [healthy, expiring, expired, blocked];
  const total = Number(metrics.totalLicenses || values.reduce((sum, value) => sum + value, 0));
  const labels = ["Ativas", "A vencer (≤ 30 dias)", "Vencidas", "Suspensas"];
  const colors = ["#FC601D", "#FF9568", "#F6C1AA", "#CFC6BF"];

  if (licenseStatusChartInstance) licenseStatusChartInstance.destroy();
  licenseStatusChartInstance = new Chart(canvas, {
    type: "doughnut",
    data: {
      labels,
      datasets: [{ data: values, backgroundColor: colors, borderColor: "#FBF8F5", borderWidth: 1 }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      cutout: "63%",
      animation: { duration: 360 },
      plugins: { legend: { display: false } }
    }
  });

  legend.innerHTML = labels.map((label, index) => `
    <div class="license-legend-row">
      <i class="license-legend-dot" style="background:${colors[index]}"></i>
      <span>${escapeHtml(label)}</span>
      <b>${number(values[index])}</b>
      <small>${formatPercent(values[index], total)}</small>
    </div>
  `).join("") + `<div class="license-legend-total"><span>Total</span><b>${number(total)}</b></div>`;
}

function renderSupportResolutionChart() {
  const canvas = qs("#supportResolutionChart");
  if (!canvas || typeof Chart === "undefined") return;
  let ranges = state.dashboard?.supportResolutionRanges || [];
  if (!ranges.length) {
    ranges = state.support
      .filter((item) => item.resolvedAt && item.createdAt)
      .slice(-6)
      .map((item) => {
        const hours = Math.max(1, (new Date(item.resolvedAt) - new Date(item.createdAt)) / 3600000);
        return {
          label: new Date(item.resolvedAt).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" }),
          low: Math.max(0, Math.round(hours * .62)),
          high: Math.max(1, Math.round(hours * 1.18))
        };
      });
  }
  if (!ranges.length) ranges = [{ label: "Sem dados", low: 0, high: 0 }];

  if (supportResolutionChartInstance) supportResolutionChartInstance.destroy();
  supportResolutionChartInstance = new Chart(canvas, {
    type: "bar",
    data: {
      labels: ranges.map((item) => item.label),
      datasets: [{
        data: ranges.map((item) => [Number(item.low || 0), Number(item.high || 0)]),
        backgroundColor: "#FC601D",
        borderColor: "#D94C12",
        borderWidth: 1,
        borderRadius: 0,
        barPercentage: .42
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: { duration: 360 },
      plugins: {
        legend: { display: false },
        tooltip: {
          displayColors: false,
          callbacks: {
            label: (context) => `${context.raw[0]}h – ${context.raw[1]}h`
          }
        }
      },
      scales: {
        x: {
          border: { display: false },
          grid: { display: false },
          ticks: { color: "#77706B", font: { size: 8 }, maxRotation: 0 }
        },
        y: {
          beginAtZero: true,
          border: { display: false },
          grid: { color: "#E7E0DA" },
          ticks: { color: "#77706B", font: { size: 8 }, callback: (value) => `${value}h` }
        }
      }
    }
  });
}

function renderSupportHeatmap() {
  const target = qs("#supportHeatmap");
  if (!target) return;
  const days = ["Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom"];
  const hours = Array.from({ length: 12 }, (_value, index) => index * 2);
  const matrix = Array.from({ length: 7 }, () => Array(12).fill(0));
  const fixture = state.dashboard?.supportDemandHeatmap;

  if (Array.isArray(fixture) && fixture.length === 7) {
    fixture.forEach((row, dayIndex) => row.slice(0, 12).forEach((value, hourIndex) => {
      matrix[dayIndex][hourIndex] = Number(value || 0);
    }));
  } else {
    state.support.forEach((item) => {
      const date = new Date(item.createdAt || item.updatedAt || 0);
      if (Number.isNaN(date.getTime())) return;
      const dayIndex = (date.getDay() + 6) % 7;
      matrix[dayIndex][Math.min(11, Math.floor(date.getHours() / 2))] += 1;
    });
  }

  const max = Math.max(1, ...matrix.flat());
  target.innerHTML = `<span></span>${hours.map((hour) => `<span class="heatmap-time">${String(hour).padStart(2, "0")}h</span>`).join("")}`
    + days.map((day, dayIndex) => `
      <span class="heatmap-label">${day}</span>
      ${matrix[dayIndex].map((value) => {
        const alpha = value ? .12 + (value / max) * .88 : .05;
        return `<i class="heatmap-cell" title="${day}, ${value} chamado(s)" style="background:rgba(252,96,29,${alpha.toFixed(2)})"></i>`;
      }).join("")}
    `).join("");
}

function renderRadarRisks() {
  const target = qs("#riskList");
  if (!target) return;
  const metrics = state.dashboard?.metrics || {};
  const risks = [
    {
      value: metrics.expiringSoon30d || 0,
      label: "licenças vencendo",
      detail: "Nos próximos 30 dias",
      view: "licenses"
    },
    {
      value: metrics.waitingSupport ?? state.support.filter((item) => item.status !== "RESOLVIDO").length,
      label: "clientes aguardando",
      detail: "Fila de atendimento",
      view: "support"
    },
    {
      value: metrics.supportOutOfSla ?? metrics.urgentSupport ?? 0,
      label: "chamados fora do SLA",
      detail: "Prioridade média ou alta",
      view: "support"
    }
  ];
  target.innerHTML = risks.map((item) => `
    <button class="risk-item" type="button" data-risk-view="${escapeHtml(item.view)}">
      <strong>${number(item.value)}</strong>
      <span class="risk-copy"><b>${escapeHtml(item.label)}</b><span>${escapeHtml(item.detail)}</span></span>
      <i class="fa-solid fa-chevron-right" aria-hidden="true"></i>
    </button>
  `).join("");
  target.querySelectorAll("[data-risk-view]").forEach((button) => {
    button.addEventListener("click", () => setView(button.dataset.riskView));
  });
}

function renderRevenueChart(revenueByDay, totalCents) {
  const canvas = qs("#revenueChart");
  if (!canvas || typeof Chart === "undefined") return;

  const currentMonth = new Date();
  const year = currentMonth.getFullYear();
  const month = currentMonth.getMonth();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const dailyValues = new Map(
    (Array.isArray(revenueByDay) ? revenueByDay : []).map((item) => [String(item.date || ""), Number(item.revenueCents || 0)])
  );
  const labels = [];
  const values = [];
  for (let day = 1; day <= daysInMonth; day += 1) {
    const iso = `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    labels.push(`${String(day).padStart(2, "0")}/${String(month + 1).padStart(2, "0")}`);
    values.push(dailyValues.has(iso) ? dailyValues.get(iso) : null);
  }

  if (!values.some((value) => Number(value) > 0) && totalCents > 0) {
    values[values.length - 1] = totalCents;
  }

  if (revenueChart) revenueChart.destroy();
  revenueChart = new Chart(canvas, {
    type: "line",
    data: {
      labels,
      datasets: [{
        data: values,
        borderColor: "#FC601D",
        backgroundColor: "transparent",
        borderWidth: 2.5,
        pointRadius: (context) => {
          return context.raw == null ? 0 : 4;
        },
        pointHoverRadius: 5,
        pointBackgroundColor: "#FFFFFF",
        pointBorderColor: "#FC601D",
        pointBorderWidth: 2,
        tension: .28,
        spanGaps: true,
        fill: false
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: { duration: 420 },
      interaction: { intersect: false, mode: "index" },
      plugins: {
        legend: { display: false },
        tooltip: {
          displayColors: false,
          backgroundColor: "#171717",
          callbacks: {
            label: (context) => moneyFromCents(context.parsed.y, state.dashboard?.stripe?.currency || "BRL")
          }
        }
      },
      scales: {
        x: {
          border: { display: false },
          grid: { display: false },
          ticks: {
            color: "#77706B",
            font: { size: 10 },
            maxRotation: 0,
            autoSkip: false,
            callback: (_value, index) => {
              const day = index + 1;
              const anchors = [1, 6, 12, 18, 24, daysInMonth];
              return anchors.includes(day) ? labels[index] : "";
            }
          }
        },
        y: {
          beginAtZero: true,
          border: { display: false },
          grid: { color: "#E6DED8", borderDash: [4, 4] },
          ticks: { display: false, count: 4 }
        }
      }
    }
  });
}

function renderDashboardPriorities() {
  const target = qs("#priorityList");
  if (!target) return;

  const priorities = [];
  const expiring = state.dashboard?.expiringSoon || [];
  expiring.slice(0, 3).forEach((item) => {
    const expiresAt = item.expiresAt ? new Date(item.expiresAt) : null;
    priorities.push({
      customer: item.customerName || item.businessName || "Cliente",
      customerMeta: item.cnpj || item.key || "Licença ativa",
      action: "Renovar licença",
      actionMeta: item.plan || "Plano vigente",
      due: expiresAt ? compactDate(expiresAt) : "Em breve",
      dueMeta: expiresAt ? relativeDue(expiresAt) : "Revisão necessária",
      view: "licenses"
    });
  });

  state.support
    .filter((item) => item.status !== "RESOLVIDO")
    .slice(0, Math.max(0, 4 - priorities.length))
    .forEach((item) => {
      priorities.push({
        customer: item.businessName || item.customerName || item.ownerName || "Cliente",
        customerMeta: item.shortId ? `Chamado ${item.shortId}` : "Suporte",
        action: item.priority === "URGENTE" ? "Atender chamado urgente" : "Responder chamado",
        actionMeta: item.category || item.message || "Aguardando atendimento",
        due: "Hoje",
        dueMeta: item.createdAt ? `Aberto ${relativeAge(new Date(item.createdAt))}` : "Aguardando retorno",
        view: "support"
      });
    });

  const fillers = [
    {
      customer: "Licenças disponíveis",
      customerMeta: `${number(state.dashboard?.metrics?.availableLicenses || 0)} prontas para ativar`,
      action: "Revisar novas ativações",
      actionMeta: "Acompanhar clientes em implantação",
      due: "Hoje",
      dueMeta: "Operação em tempo real",
      view: "licenses"
    },
    {
      customer: "Clientes conectados",
      customerMeta: `${number(state.dashboard?.metrics?.online24h || 0)} online nas últimas 24h`,
      action: "Acompanhar atividade",
      actionMeta: "Verificar sincronização dos aplicativos",
      due: "Hoje",
      dueMeta: "Dados atualizados",
      view: "devices"
    },
    {
      customer: "Compras confirmadas",
      customerMeta: `${number(state.dashboard?.metrics?.stripePurchases24h || 0)} nas últimas 24h`,
      action: "Conferir novos pagamentos",
      actionMeta: "Validar licenças geradas",
      due: "Hoje",
      dueMeta: "Conferência diária",
      view: "licenses"
    }
  ];

  while (priorities.length < 4 && fillers.length) priorities.push(fillers.shift());
  const visible = priorities.slice(0, 4);
  qs("#dashboardSummary").textContent = visible.length
    ? `Seu negócio está em dia. Há ${visible.length} ${visible.length === 1 ? "ponto" : "pontos"} para revisar.`
    : "Seu negócio está em dia. Nenhuma pendência importante agora.";

  target.innerHTML = visible.length
    ? visible.map((item, index) => `
      <button class="priority-row" type="button" data-priority-view="${escapeHtml(item.view)}">
        <span class="priority-number">${String(index + 1).padStart(2, "0")}</span>
        <span class="priority-cell">
          <strong>${escapeHtml(item.customer)}</strong>
          <span>${escapeHtml(item.customerMeta)}</span>
        </span>
        <span class="priority-cell">
          <strong>${escapeHtml(item.action)}</strong>
          <span>${escapeHtml(item.actionMeta)}</span>
        </span>
        <span class="priority-cell priority-due">
          <strong>${escapeHtml(item.due)}</strong>
          <span>${escapeHtml(item.dueMeta)}</span>
        </span>
      </button>
    `).join("")
    : `<div class="priority-empty">Nenhuma prioridade pendente neste momento.</div>`;

  target.querySelectorAll("[data-priority-view]").forEach((button) => {
    button.addEventListener("click", () => setView(button.dataset.priorityView));
  });
}

function compactDate(value) {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) return "-";
  const today = new Date();
  const isToday = value.toDateString() === today.toDateString();
  if (isToday) return `Hoje, ${value.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" })}`;
  const tomorrow = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 1);
  if (value.toDateString() === tomorrow.toDateString()) {
    return `Amanhã, ${value.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" })}`;
  }
  return value.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" });
}

function relativeDue(value) {
  const today = new Date();
  const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const targetStart = new Date(value.getFullYear(), value.getMonth(), value.getDate());
  const days = Math.ceil((targetStart - todayStart) / 86400000);
  if (days <= 0) return "Vence hoje";
  if (days === 1) return "Vence amanhã";
  return `Vence em ${days} dias`;
}

function relativeAge(value) {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) return "recentemente";
  const minutes = Math.max(1, Math.round((Date.now() - value.getTime()) / 60000));
  if (minutes < 60) return `há ${minutes} min`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `há ${hours} h`;
  return `há ${Math.round(hours / 24)} d`;
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
  const queue = qs("#supportQueue");
  const conversation = qs("#supportConversation");
  const context = qs("#supportContext");
  if (!queue || !conversation || !context) return;

  const openItems = state.support.filter((item) => item.status === "ABERTO");
  const urgentItems = openItems.filter((item) => item.priority === "URGENTE");
  qs("#supportOpenBadge").textContent = `${number(openItems.length)} aberto${openItems.length === 1 ? "" : "s"}`;
  qs("#supportUrgentBadge").textContent = `${number(urgentItems.length)} urgente${urgentItems.length === 1 ? "" : "s"}`;

  document.querySelectorAll("[data-support-filter]").forEach((button) => {
    button.classList.toggle("active", button.dataset.supportFilter === state.supportFilter);
  });

  const query = qs("#supportSearch")?.value.trim().toLowerCase() || "";
  const rows = state.support.filter((item) => {
    const haystack = `${item.shortId} ${item.licenseKey} ${item.customerName} ${item.businessName} ${item.ownerName} ${item.phone} ${item.machineCode} ${item.message} ${item.category}`.toLowerCase();
    if (query && !haystack.includes(query)) return false;
    if (state.supportFilter === "open") return item.status === "ABERTO";
    if (state.supportFilter === "working") return item.status === "EM_ATENDIMENTO";
    return item.status !== "RESOLVIDO";
  });

  if (!state.selectedSupportId
    || !state.support.some((item) => item.id === state.selectedSupportId)
    || (rows.length && !rows.some((item) => item.id === state.selectedSupportId))) {
    state.selectedSupportId = rows[0]?.id || state.support[0]?.id || "";
  }

  queue.innerHTML = rows.length
    ? rows.map((item) => supportQueueItem(item)).join("")
    : `<div class="support-empty"><i class="fa-regular fa-comments" aria-hidden="true"></i><strong>Nenhum chamado encontrado</strong><span>Ajuste a busca ou o filtro da fila.</span></div>`;

  const selected = state.support.find((item) => item.id === state.selectedSupportId);
  if (!selected) {
    conversation.innerHTML = `<div class="support-empty large"><i class="fa-regular fa-comment-dots" aria-hidden="true"></i><strong>Selecione um chamado</strong></div>`;
    context.innerHTML = "";
    return;
  }

  conversation.innerHTML = supportConversation(selected);
  context.innerHTML = supportClientContext(selected);
}

function supportDisplayName(item) {
  return item.businessName || item.customerName || item.ownerName || "Cliente";
}

function supportStatusLabel(status) {
  return ({
    ABERTO: "ABERTO",
    EM_ATENDIMENTO: "EM ATENDIMENTO",
    RESOLVIDO: "RESOLVIDO"
  })[status] || status || "ABERTO";
}

function supportQueueIcon(item) {
  if (item.priority === "URGENTE") return "fa-circle-exclamation";
  if (item.status === "EM_ATENDIMENTO") return "fa-clock";
  return "fa-comment-dots";
}

function supportQueueItem(item) {
  const selected = item.id === state.selectedSupportId;
  const profileName = supportDisplayName(item);
  return `
    <button type="button" class="support-queue-item ${selected ? "selected" : ""} ${item.priority === "URGENTE" ? "urgent" : ""}" onclick="selectSupportTicket('${escapeHtml(item.id)}')">
      <span class="support-priority-dot ${item.priority === "URGENTE" ? "urgent" : item.status === "EM_ATENDIMENTO" ? "working" : "neutral"}" aria-hidden="true"></span>
      <i class="fa-regular ${supportQueueIcon(item)} support-ticket-icon" aria-hidden="true"></i>
      <span class="support-ticket-copy">
        <strong>${escapeHtml(profileName)}</strong>
        <span>${escapeHtml(item.category || "Suporte")}</span>
        <small>${dateTime(item.createdAt).replace(",", "")}</small>
      </span>
      <span class="support-ticket-state">
        <b class="${item.priority === "URGENTE" ? "urgent" : item.status || "ABERTO"}">${item.priority === "URGENTE" ? "URGENTE" : supportStatusLabel(item.status)}</b>
        <small>${escapeHtml(item.ageLabel || "agora")}</small>
      </span>
    </button>
  `;
}

function supportConversation(item) {
  const profileName = supportDisplayName(item);
  const rawMessages = item.messages?.length
    ? item.messages
    : [{ sender: "cliente", message: item.message, when: item.createdAt }];
  const messageCards = rawMessages.map((message) => {
    const isAdmin = message.sender === "admin" || message.sender === "suporte";
    return `
      <article class="support-message ${isAdmin ? "admin" : "client"}">
        <span class="support-message-avatar">${isAdmin ? "IS" : `<i class="fa-solid fa-user" aria-hidden="true"></i>`}</span>
        <div class="support-message-content">
          <strong>${isAdmin ? "Suporte" : "Cliente"}</strong>
          <div class="support-message-bubble">
            <p>${escapeHtml(message.message || "")}</p>
            <time>${timeOnly(message.when).slice(0, 5)}</time>
            ${isAdmin ? `<i class="fa-solid fa-check-double" aria-label="Mensagem entregue"></i>` : ""}
          </div>
        </div>
      </article>
    `;
  });
  const messages = [
    messageCards[0] || "",
    `<div class="support-date-divider"><span>${dateOnly(rawMessages[0]?.when || item.createdAt)}</span></div>`,
    ...messageCards.slice(1)
  ].join("");
  const attendLabel = item.status === "ABERTO" ? "Assumir atendimento" : item.status === "EM_ATENDIMENTO" ? "Em atendimento" : "Reabrir chamado";
  const attendStatus = item.status === "ABERTO" ? "EM_ATENDIMENTO" : item.status === "RESOLVIDO" ? "ABERTO" : "EM_ATENDIMENTO";

  return `
    <header class="support-conversation-head">
      <div>
        <h2>${escapeHtml(profileName)}</h2>
        <p>Protocolo ${escapeHtml(item.shortId || item.id || "-")} <span>·</span> ${escapeHtml(item.category || "Suporte")} <span>·</span> <b class="support-inline-status ${item.status || "ABERTO"}">${supportStatusLabel(item.status)}</b></p>
      </div>
      <button type="button" class="support-assume-button ${item.status === "EM_ATENDIMENTO" ? "active" : ""}" onclick="setSupportStatus('${escapeHtml(item.id)}', '${attendStatus}')">
        <i class="fa-regular fa-user" aria-hidden="true"></i>
        ${attendLabel}
      </button>
    </header>
    <div class="support-message-scroll">
      ${messages}
    </div>
    <footer class="support-composer">
      <textarea id="reply-${escapeHtml(item.id)}" rows="2" placeholder="Escreva uma resposta..."></textarea>
      <div class="support-composer-actions">
        <button type="button" class="support-attach-button" aria-label="Anexar arquivo" title="Anexar arquivo">
          <i class="fa-solid fa-paperclip" aria-hidden="true"></i>
        </button>
        <div>
          <select id="supportQuickReply" aria-label="Resposta rápida" onchange="useSupportQuickReply('${escapeHtml(item.id)}')">
            <option value="">Resposta rápida</option>
            <option value="Vou verificar os dados e retorno em alguns minutos.">Verificar dados</option>
            <option value="A sincronização foi concluída. Pode testar novamente?">Sincronização concluída</option>
            <option value="Recebi as informações. Vou acompanhar por aqui.">Confirmar recebimento</option>
          </select>
          <button type="button" class="support-send-button" onclick="replySupport('${escapeHtml(item.id)}')">Enviar</button>
        </div>
      </div>
    </footer>
  `;
}

function supportClientContext(item) {
  const profile = item.profile || {};
  const environment = item.environmentSnapshot || item.environment || {};
  const profileName = supportDisplayName(item);
  const cnpj = item.cnpj || profile.cnpj || "-";
  const responsible = item.responsible || item.ownerName || profile.ownerName || profile.contactName || "-";
  const phone = item.phone || profile.phone || "-";
  const plan = item.plan || item.licensePlan || "Balcão Livre Pro";
  const machine = item.machineCode || environment.machineName || "-";
  const lastSync = item.lastSeenAt || item.lastSyncAt || environment.lastSeenAt;
  const version = item.appVersion || environment.appVersion || environment.version || "-";
  const steps = item.nextSteps || [
    { label: "Validar vínculo da unidade 2", done: false },
    { label: "Confirmar nova sincronização", done: false },
    { label: "Responder cliente", done: false }
  ];

  return `
    <div class="support-context-content">
      <h2>Contexto do cliente</h2>
      <div class="support-company">
        <i class="fa-solid fa-store" aria-hidden="true"></i>
        <strong>${escapeHtml(profileName)}</strong>
      </div>
      <dl class="support-contact-list">
        <div><dt><i class="fa-regular fa-id-card" aria-hidden="true"></i><span>CNPJ</span></dt><dd>${escapeHtml(cnpj)}</dd></div>
        <div><dt><i class="fa-regular fa-user" aria-hidden="true"></i><span>Responsável</span></dt><dd>${escapeHtml(responsible)}</dd></div>
        <div><dt><i class="fa-solid fa-phone" aria-hidden="true"></i><span>Telefone</span></dt><dd>${escapeHtml(phone)}</dd></div>
      </dl>
      <dl class="support-technical-list">
        <div><dt><i class="fa-regular fa-rectangle-list" aria-hidden="true"></i>Licença</dt><dd>${escapeHtml(plan)} <b>· ativa</b></dd></div>
        <div><dt><i class="fa-solid fa-desktop" aria-hidden="true"></i>Computador</dt><dd>${escapeHtml(machine)}</dd></div>
        <div><dt><i class="fa-solid fa-rotate" aria-hidden="true"></i>Última sincronização</dt><dd>${lastSync ? escapeHtml(item.lastSyncLabel || dateTime(lastSync)) : "-"}</dd></div>
        <div><dt><i class="fa-regular fa-clock" aria-hidden="true"></i>Versão do aplicativo</dt><dd>${escapeHtml(version)}</dd></div>
      </dl>
      <section class="support-next-steps">
        <h3>Próximos passos</h3>
        ${steps.map((step, index) => `
          <label>
            <input type="checkbox" ${step.done ? "checked" : ""} onchange="toggleSupportStep('${escapeHtml(item.id)}', ${index}, this.checked)">
            <span>${escapeHtml(step.label)}</span>
          </label>
        `).join("")}
      </section>
    </div>
    <div class="support-context-actions">
      <button type="button" class="support-resolve-button" onclick="setSupportStatus('${escapeHtml(item.id)}', 'RESOLVIDO')">
        <i class="fa-solid fa-check" aria-hidden="true"></i>
        Resolver chamado
      </button>
      <button type="button" class="support-view-client-button" onclick="openSupportClient('${escapeHtml(item.id)}')">
        <i class="fa-regular fa-user" aria-hidden="true"></i>
        Ver cliente
      </button>
    </div>
  `;
}

function renderLicenses() {
  const target = qs("#licensesTable");
  if (!target) return;
  const now = new Date();
  const allLicenses = [...state.licenses].sort((left, right) => {
    const leftExpired = licenseTiming(left, now).expired;
    const rightExpired = licenseTiming(right, now).expired;
    if (leftExpired !== rightExpired) return leftExpired ? 1 : -1;
    const leftExpires = new Date(left.expiresAt || 0).getTime();
    const rightExpires = new Date(right.expiresAt || 0).getTime();
    return leftExpires - rightExpires;
  });
  const query = qs("#licenseSearch").value.trim().toLowerCase();
  const rows = allLicenses.filter((item) => {
    const profile = item.profile || {};
    const env = item.environmentSnapshot || item.environment || {};
    const haystack = `${item.key} ${item.customerName} ${item.businessName} ${item.cnpj} ${item.plan} ${item.phone} ${profile.phone} ${env.machineName} ${env.clientProduct} ${item.machineCode} ${item.clientKind}`.toLowerCase();
    if (query && !haystack.includes(query)) return false;

    const timing = licenseTiming(item, now);
    if (state.licenseFilter === "expired") return timing.expired;
    if (state.licenseFilter === "7") return !timing.expired && timing.days <= 7;
    if (state.licenseFilter === "30") return !timing.expired && timing.days <= 30;
    return true;
  });

  const metrics = state.dashboard?.metrics || {};
  const computedActive = allLicenses.filter((item) => {
    const timing = licenseTiming(item, now);
    return !timing.expired && item.status !== "BLOQUEADA";
  }).length;
  const computedSoon = allLicenses.filter((item) => {
    const timing = licenseTiming(item, now);
    return !timing.expired && timing.days <= 30;
  }).length;
  const computedExpired = allLicenses.filter((item) => licenseTiming(item, now).expired).length;
  qs("#licenseMetricTotal").textContent = number(metrics.totalLicenses ?? allLicenses.length);
  qs("#licenseMetricActive").textContent = number(metrics.activeLicenses ?? computedActive);
  qs("#licenseMetricSoon").textContent = number(metrics.expiringSoon30d ?? computedSoon);
  qs("#licenseMetricExpired").textContent = number(metrics.expiredLicenses ?? computedExpired);
  qs("#licenseBlockedIpCount").textContent = number(state.blockedIps.length);

  document.querySelectorAll("[data-license-filter]").forEach((button) => {
    button.classList.toggle("active", button.dataset.licenseFilter === state.licenseFilter);
  });

  if (!rows.some((item) => item.id === state.selectedLicenseId)) {
    state.selectedLicenseId = rows[0]?.id || "";
  }

  target.innerHTML = rows.length
    ? rows.map((item) => {
      const env = item.environmentSnapshot || item.environment || {};
      const timing = licenseTiming(item, now);
      const plan = item.plan || env.clientProduct || "Balcão Livre Pro";
      const product = env.clientProduct || clientKindLabel(item.clientKind || "windows");
      const action = licenseRowAction(item, timing);
      return `
        <tr class="${item.id === state.selectedLicenseId ? "selected" : ""}" onclick='selectLicense(${jsArg(item.id)})'>
          <td>
            <strong>${escapeHtml(item.customerName || item.businessName || "Cliente")}</strong>
            <small>${escapeHtml(item.cnpj || "CNPJ não informado")}</small>
          </td>
          <td class="key-cell">${escapeHtml(item.key || "-")}</td>
          <td>
            <strong>${escapeHtml(plan)}</strong>
            <small>${escapeHtml(product)}</small>
          </td>
          <td class="license-due-cell ${timing.urgent ? "urgent" : ""}">
            <strong>${escapeHtml(timing.label)}</strong>
            <small>${dateOnly(item.expiresAt)}</small>
          </td>
          <td class="license-last-seen">${dateTimeCompact(item.lastSeenAt)}</td>
          <td>
            <button type="button" class="license-row-action ${action.secondary ? "secondary" : ""}" onclick='event.stopPropagation(); ${action.handler}'>
              ${escapeHtml(action.label)}
            </button>
          </td>
        </tr>
      `;
    }).join("")
    : `<tr><td colspan="6" class="empty-cell">Nenhuma licença encontrada para este filtro.</td></tr>`;

  const selected = allLicenses.find((item) => item.id === state.selectedLicenseId);
  renderLicenseImmediatePanel(selected, now);
  renderLicenseNotice();
}

function licenseTiming(item, now = new Date()) {
  const expiration = new Date(item.expiresAt || 0);
  const milliseconds = expiration.getTime() - now.getTime();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const expirationDay = new Date(expiration.getFullYear(), expiration.getMonth(), expiration.getDate());
  const calendarDays = Math.round((expirationDay.getTime() - today.getTime()) / 86400000);
  const days = Math.max(0, calendarDays);
  const expired = item.status === "EXPIRADA" || milliseconds <= 0;
  if (expired) {
    const elapsedDays = Math.max(1, Math.abs(calendarDays));
    return {
      days: -elapsedDays,
      expired: true,
      urgent: true,
      label: elapsedDays === 1 ? "1 dia vencida" : `${elapsedDays} dias vencida`
    };
  }
  return {
    days,
    expired: false,
    urgent: days <= 30,
    label: days === 0 ? "Vence hoje" : days === 1 ? "1 dia" : `${days} dias`
  };
}

function licenseRowAction(item, timing) {
  if (item.status === "BLOQUEADA") {
    return {
      label: "Revisar vínculo",
      secondary: true,
      handler: `selectLicense(${jsArg(item.id)})`
    };
  }
  if (timing.expired) {
    return {
      label: "Revisar vínculo",
      secondary: true,
      handler: `selectLicense(${jsArg(item.id)})`
    };
  }
  if (timing.days <= 30) {
    return {
      label: "Renovar",
      secondary: false,
      handler: `renewLicense(${jsArg(item.id)})`
    };
  }
  return {
    label: "Reenviar chave",
    secondary: true,
    handler: `copyLicenseKey(${jsArg(item.id)})`
  };
}

function renderLicenseImmediatePanel(item, now = new Date()) {
  const panel = qs("#licenseImmediatePanel");
  if (!panel) return;
  if (!item) {
    panel.innerHTML = `
      <div class="license-detail-empty">
        <i class="fa-regular fa-folder-open" aria-hidden="true"></i>
        <strong>Nenhuma licença selecionada</strong>
        <span>Ajuste a busca ou os filtros para continuar.</span>
      </div>
    `;
    return;
  }

  const env = item.environmentSnapshot || item.environment || {};
  const profile = item.profile || {};
  const timing = licenseTiming(item, now);
  const activatedAt = item.activatedAt || item.createdAt || item.lastSeenAt;
  const expiration = new Date(item.expiresAt || now);
  const renewalSuggested = new Date(expiration);
  renewalSuggested.setDate(renewalSuggested.getDate() + 30);
  const responsible = item.ownerName || profile.ownerName || item.responsible || "Não informado";
  const contact = item.phone || profile.phone || item.email || profile.email || "Não informado";
  const machine = item.machineCode || env.machineName || "Não vinculado";
  const plan = item.plan || env.clientProduct || "Balcão Livre Pro";
  const product = env.clientProduct || clientKindLabel(item.clientKind || "windows");
  const publicIp = normalizedIp(env.publicIp);

  panel.innerHTML = `
    <header class="license-immediate-head">
      <h2>Ação imediata</h2>
    </header>
    <div class="license-selected-title">
      <strong>${escapeHtml(item.customerName || item.businessName || "Cliente")}</strong>
      <span class="license-due-pill ${timing.expired ? "expired" : ""}">${escapeHtml(timing.expired ? "Licença vencida" : `Vence em ${timing.label.toLowerCase()}`)}</span>
    </div>

    <div class="license-timeline" aria-label="Ciclo da licença">
      <div class="license-timeline-track" aria-hidden="true">
        <i class="fa-solid fa-circle done"></i><i class="fa-solid fa-circle done"></i><i class="fa-solid fa-circle"></i>
      </div>
      <div class="license-timeline-labels">
        <span><b>${dateOnly(activatedAt)}</b><small>Ativada</small></span>
        <span><b>${dateOnly(item.expiresAt)}</b><small>Vencimento</small></span>
        <span><b>${dateOnly(renewalSuggested)}</b><small>Renovação sugerida</small></span>
      </div>
    </div>

    <dl class="license-detail-list">
      <div><dt>Plano atual</dt><dd>${escapeHtml(plan)} <small>(${escapeHtml(product)})</small></dd></div>
      <div><dt>Chave</dt><dd class="key-cell">${escapeHtml(item.key || "-")}</dd></div>
      <div>
        <dt>Computador vinculado</dt>
        <dd>${escapeHtml(machine)}
          ${machine !== "Não vinculado" ? `<button type="button" class="icon-copy" onclick='copyText(${jsArg(machine)}, "Computador copiado")' aria-label="Copiar computador"><i class="fa-regular fa-copy" aria-hidden="true"></i></button>` : ""}
        </dd>
      </div>
      <div><dt>Última sincronização</dt><dd>${dateTimeCompact(item.lastSeenAt)}</dd></div>
      <div><dt>Responsável</dt><dd>${escapeHtml(responsible)}</dd></div>
      <div><dt>Contato</dt><dd>${escapeHtml(contact)}</dd></div>
      ${publicIp ? `<div><dt>IP público</dt><dd>${escapeHtml(publicIp)}</dd></div>` : ""}
    </dl>

    <div class="license-detail-actions">
      <button type="button" onclick='renewLicense(${jsArg(item.id)})'>Renovar licença</button>
      <button type="button" class="secondary" onclick='copyLicenseKey(${jsArg(item.id)})'>
        Copiar chave
        <i class="fa-regular fa-copy" aria-hidden="true"></i>
      </button>
      ${item.status === "BLOQUEADA"
        ? `<button type="button" class="license-unblock-action" onclick='unblockLicense(${jsArg(item.id)})'>Liberar licença</button>`
        : ""}
    </div>
  `;
}

function dateTimeCompact(value) {
  if (!value) return "-";
  const parsed = new Date(value);
  return `${parsed.toLocaleDateString("pt-BR")}, ${parsed.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}`;
}

function selectLicense(id) {
  state.selectedLicenseId = id;
  renderLicenses();
}

function showLicenseNotice(message) {
  state.licenseActionNotice = message;
  renderLicenseNotice();
  window.clearTimeout(showLicenseNotice.timer);
  showLicenseNotice.timer = window.setTimeout(() => {
    state.licenseActionNotice = "";
    renderLicenseNotice();
  }, 2800);
}

function renderLicenseNotice() {
  const notice = qs("#licenseActionNotice");
  if (!notice) return;
  notice.textContent = state.licenseActionNotice;
  notice.classList.toggle("hidden", !state.licenseActionNotice);
}

function copyText(value, successMessage = "Copiado") {
  if (!value) return;
  navigator.clipboard.writeText(value)
    .then(() => showLicenseNotice(successMessage))
    .catch(() => showLicenseNotice("Não foi possível copiar."));
}

function copyLicenseKey(id) {
  const item = state.licenses.find((license) => license.id === id);
  if (!item?.key) return;
  copyText(item.key, "Chave copiada para a área de transferência.");
}

async function renewLicense(id) {
  const item = state.licenses.find((license) => license.id === id);
  if (!item) return;
  state.selectedLicenseId = id;

  if (isLocalPreview) {
    const start = Math.max(Date.now(), new Date(item.expiresAt || 0).getTime());
    item.expiresAt = new Date(start + (30 * 86400000)).toISOString();
    item.status = item.machineCode ? "ATIVA" : "DISPONIVEL";
    showLicenseNotice(`Licença de ${item.customerName || item.businessName || "cliente"} renovada por 30 dias.`);
    renderLicenses();
    return;
  }

  try {
    await api(`/api/licenses/${id}/renew`, {
      method: "POST",
      body: JSON.stringify({ amount: 30, unit: "days" })
    });
    await loadRealtimeData({ notifySupport: false, force: true });
    state.selectedLicenseId = id;
    renderLicenses();
    showLicenseNotice(`Licença de ${item.customerName || item.businessName || "cliente"} renovada por 30 dias.`);
  } catch (error) {
    showLicenseNotice(error.message || "Não foi possível renovar a licença.");
  }
}

function renderDevices() {
  const target = qs("#devicesList");
  if (!target) return;
  const intelligence = state.clientIntelligence || {};
  const summary = intelligence.summary || {};
  const clients = Array.isArray(intelligence.clients) ? intelligence.clients : [];
  const total = Number(summary.totalClients || clients.length || 0);
  const healthy = Number(summary.healthyClients || 0);
  const attention = Number(summary.attentionClients || 0);
  const critical = Number(summary.criticalClients || 0);
  const percentage = (value) => total > 0 ? Math.round(Number(value || 0) * 1000 / total) / 10 : 0;

  qs("#clientActiveCount").textContent = number(summary.activeClients || 0);
  qs("#clientSyncedPercent").textContent = `${number(summary.synchronizedTodayPercent || 0)}%`;
  qs("#clientSyncedCount").textContent = `${number(summary.synchronizedToday || 0)} de ${number(total)} clientes`;
  qs("#clientUpdatedAt").textContent = intelligence.updatedAt
    ? `Última atualização: ${dateTime(intelligence.updatedAt)}`
    : "Aguardando atualização";
  qs("#clientSecurityUpdatedAt").textContent = intelligence.updatedAt ? dateTime(intelligence.updatedAt) : "—";

  const healthSegments = [
    { key: "healthy", label: "Saudáveis", value: healthy },
    { key: "attention", label: "Atenção", value: attention },
    { key: "critical", label: "Críticos", value: critical }
  ];
  qs("#clientHealthBar").innerHTML = total
    ? healthSegments
      .filter((item) => item.value > 0)
      .map((item) => `<span class="${item.key}" style="flex:${item.value}" title="${escapeHtml(item.label)}: ${number(item.value)}"></span>`)
      .join("")
    : `<span class="empty" style="flex:1"></span>`;
  qs("#clientHealthLegend").innerHTML = healthSegments.map((item) => `
    <button type="button" class="client-health-legend-item ${state.clientHealthFilter === item.key ? "active" : ""}" onclick='setClientHealthFilter(${jsArg(item.key)})'>
      <i class="${item.key}" aria-hidden="true"></i>
      <span><b>${percentage(item.value)}%</b> ${escapeHtml(item.label)}</span>
      <small>${number(item.value)} cliente(s)</small>
    </button>
  `).join("");

  const query = (qs("#clientIntelligenceSearch")?.value || "").trim().toLowerCase();
  const filtered = clients.filter((item) => {
    const matchesFilter = state.clientHealthFilter === "all" || item.healthBand === state.clientHealthFilter;
    const haystack = `${item.name} ${item.cnpj} ${item.city} ${item.state} ${item.plan} ${item.reason}`.toLowerCase();
    return matchesFilter && (!query || haystack.includes(query));
  });
  if (!state.selectedClientId || !clients.some((item) => item.id === state.selectedClientId)) {
    state.selectedClientId = filtered[0]?.id || clients[0]?.id || "";
  }

  target.innerHTML = filtered.length
    ? filtered.map((item, index) => `
      <div class="client-priority-row ${item.id === state.selectedClientId ? "selected" : ""}" role="row" tabindex="0" onclick='selectIntelligenceClient(${jsArg(item.id)})' onkeydown='handleClientRowKey(event, ${jsArg(item.id)})'>
        <span class="client-rank ${index < 3 ? "top" : ""}" role="cell">${index + 1}</span>
        <span class="client-identity" role="cell">
          <i>${escapeHtml(item.initials || "CL")}</i>
          <span><b>${escapeHtml(item.name || "Cliente")}</b><small>${escapeHtml(item.cnpj || "CNPJ não informado")}</small></span>
        </span>
        <span role="cell">${escapeHtml([item.city, item.state].filter(Boolean).join(", ") || "—")}</span>
        <span role="cell">${escapeHtml(item.plan || "Sem plano")}</span>
        <span class="client-score ${escapeHtml(item.healthBand || "attention")}" role="cell">${number(item.healthScore)}/100</span>
        <span class="client-reason" role="cell">${escapeHtml(item.reason || "Dados insuficientes")}</span>
        <span class="client-action-cell" role="cell"><button type="button" class="client-action" onclick='event.stopPropagation(); openClientSuggestedAction(${jsArg(item.id)})'>${escapeHtml(item.suggestedAction || "Ver detalhes")}<i class="fa-solid fa-chevron-right" aria-hidden="true"></i></button></span>
      </div>
    `).join("")
    : `<div class="client-intelligence-empty">
        <i class="fa-regular fa-folder-open" aria-hidden="true"></i>
        <strong>Nenhum cliente encontrado</strong>
        <span>Ajuste a busca ou o filtro de saúde.</span>
      </div>`;

  renderClientIntelligenceDetail(clients.find((item) => item.id === state.selectedClientId) || filtered[0] || null);
}

function renderBlockedIps() {
  const blockedIps = state.blockedIps || [];
  const badge = qs("#blockedIpLiveBadge");
  if (badge) badge.textContent = number(state.clientIntelligence?.blockedIpCount ?? blockedIps.length);
}

function renderClientIntelligenceDetail(item) {
  const target = qs("#clientIntelligenceDetail");
  if (!target) return;
  if (!item) {
    target.innerHTML = `
      <div class="client-detail-empty">
        <i class="fa-regular fa-address-card" aria-hidden="true"></i>
        <strong>Selecione um cliente</strong>
        <span>Os sinais de saúde e as ações aparecerão aqui.</span>
      </div>`;
    if (clientActivityChartInstance) {
      clientActivityChartInstance.destroy();
      clientActivityChartInstance = null;
    }
    return;
  }

  const history = Array.isArray(item.history) ? item.history : [];
  const expiryText = item.expiresAt ? dateTime(item.expiresAt) : "Sem vencimento informado";
  const syncText = item.lastSeenAt ? dateTime(item.lastSeenAt) : "Sem sincronização";
  const sinceText = item.clientSinceAt ? `Cliente desde ${dateOnly(item.clientSinceAt)}` : `${number(item.licenseCount)} licença(s)`;
  target.innerHTML = `
    <header class="client-detail-head">
      <span class="client-detail-avatar">${escapeHtml(item.initials || "CL")}</span>
      <div>
        <h2>${escapeHtml(item.name || "Cliente")}</h2>
        <p>${escapeHtml([item.city, item.state].filter(Boolean).join(", ") || "Local não informado")} <i aria-hidden="true">•</i> ${escapeHtml(sinceText)}</p>
      </div>
      <div class="client-detail-score ${escapeHtml(item.healthBand || "attention")}">
        <strong>${number(item.healthScore)}<small>/100</small></strong>
        <span>Saúde</span>
      </div>
    </header>

    <section class="client-activity-section">
      <div class="client-activity-title">
        <div><b>Atividade observada</b><span>últimos 30 dias</span></div>
        <small>Confiança ${escapeHtml(item.confidenceLabel || "parcial")} · ${number(item.confidence || 0)}% dos sinais</small>
      </div>
      <div class="client-activity-chart"><canvas id="clientActivityChart" aria-label="Atividade observada nos últimos 30 dias"></canvas></div>
    </section>

    <section class="client-signal-grid">
      <div>
        <b>Licença</b>
        <p><i class="signal-dot" aria-hidden="true"></i>${escapeHtml(item.reason || item.licenseStatus || "Sem dado")}</p>
        <small>${escapeHtml(expiryText)}</small>
        <p class="signal-secondary"><i class="signal-dot neutral" aria-hidden="true"></i>${escapeHtml(item.plan || "Sem plano")}</p>
      </div>
      <div>
        <b>Sincronização</b>
        <p><i class="signal-dot" aria-hidden="true"></i>${escapeHtml(item.syncLabel || "Sem sincronização")}</p>
        <small>${escapeHtml(syncText)}</small>
        <p class="signal-secondary"><i class="signal-dot neutral" aria-hidden="true"></i>${number(item.deviceCount)} dispositivo(s)</p>
      </div>
      <div>
        <b>Contato responsável</b>
        <p><i class="fa-regular fa-user" aria-hidden="true"></i>${escapeHtml(item.ownerName || "Não informado")}</p>
        <small>${escapeHtml(item.email || item.phone || "Sem contato cadastrado")}</small>
        <p class="signal-secondary">${escapeHtml(item.phone || "")}</p>
      </div>
    </section>

    <div class="client-detail-actions">
      <button type="button" onclick='openClientContact(${jsArg(item.id)})'><i class="fa-regular fa-envelope" aria-hidden="true"></i>Entrar em contato</button>
      <button type="button" class="secondary" onclick='toggleClientHistory(${jsArg(item.id)})'>${state.clientHistoryOpen ? "Ocultar histórico" : "Ver histórico"}</button>
    </div>

    <section class="client-history ${state.clientHistoryOpen ? "" : "hidden"}">
      <h3>Histórico recente</h3>
      ${history.length
        ? history.map((entry) => `
          <div><i class="fa-solid ${clientHistoryIcon(entry.type)}" aria-hidden="true"></i><span><b>${escapeHtml(entry.label || "Evento")}</b><small>${dateTime(entry.when)}</small></span></div>
        `).join("")
        : `<p>Nenhum evento recente disponível para este cliente.</p>`}
    </section>
  `;
  requestAnimationFrame(() => renderClientActivityChart(item));
}

function renderClientActivityChart(item) {
  const canvas = qs("#clientActivityChart");
  if (!canvas || typeof Chart === "undefined") return;
  if (clientActivityChartInstance) clientActivityChartInstance.destroy();
  const activity = Array.isArray(item.activity30d) ? item.activity30d.slice(-30) : [];
  const values = activity.length ? activity : Array(30).fill(0);
  const now = new Date();
  const labels = values.map((_, index) => {
    const value = new Date(now);
    value.setDate(now.getDate() - (values.length - 1 - index));
    return value.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" });
  });
  clientActivityChartInstance = new Chart(canvas, {
    type: "line",
    data: {
      labels,
      datasets: [{
        data: values,
        borderColor: "#FC601D",
        backgroundColor: "#FC601D",
        borderWidth: 1.8,
        pointRadius: 0,
        pointHoverRadius: 3,
        tension: .18,
        fill: false
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation: { duration: 220 },
      interaction: { intersect: false, mode: "index" },
      plugins: { legend: { display: false }, tooltip: { displayColors: false } },
      scales: {
        x: {
          grid: { display: false },
          border: { display: false },
          ticks: { color: "#77706b", font: { size: 9 }, maxTicksLimit: 2, maxRotation: 0 }
        },
        y: {
          beginAtZero: true,
          suggestedMax: Math.max(2, ...values),
          grid: { color: "#e7e0da", borderDash: [3, 3] },
          border: { display: false },
          ticks: { display: false, precision: 0 }
        }
      }
    }
  });
}

function selectIntelligenceClient(id) {
  state.selectedClientId = String(id || "");
  state.clientHistoryOpen = false;
  renderDevices();
}

function handleClientRowKey(event, id) {
  if (event.key !== "Enter" && event.key !== " ") return;
  event.preventDefault();
  selectIntelligenceClient(id);
}

function setClientHealthFilter(value) {
  state.clientHealthFilter = String(value || "all");
  const select = qs("#clientHealthFilter");
  if (select) select.value = state.clientHealthFilter;
  state.selectedClientId = "";
  renderDevices();
}

function toggleClientHistory(id) {
  state.selectedClientId = String(id || state.selectedClientId);
  state.clientHistoryOpen = !state.clientHistoryOpen;
  renderDevices();
}

function openClientContact(id) {
  const item = (state.clientIntelligence?.clients || []).find((client) => client.id === id);
  if (!item) return;
  setView("support");
  qs("#supportSearch").value = item.name || item.licenseKey || "";
  renderSupport();
}

function openClientSuggestedAction(id) {
  const item = (state.clientIntelligence?.clients || []).find((client) => client.id === id);
  if (!item) return;
  if (item.actionView === "licenses") {
    setView("licenses");
    qs("#licenseSearch").value = item.name || item.licenseKey || "";
    renderLicenses();
    return;
  }
  if (item.actionView === "support") {
    openClientContact(id);
    return;
  }
  state.clientHistoryOpen = true;
  renderDevices();
}

function clientHistoryIcon(type) {
  if (type === "license") return "fa-key";
  if (type === "payment") return "fa-receipt";
  if (type === "support") return "fa-headset";
  return "fa-rotate";
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
  state.selectedSupportId = id;
  if (isLocalPreview) {
    const ticket = state.support.find((item) => item.id === id);
    if (!ticket) return;
    ticket.status = status;
    if (status === "EM_ATENDIMENTO") {
      ticket.ageLabel = "agora";
      showSupportNotice(`Atendimento assumido por ${state.adminProfile.name}.`);
    } else if (status === "RESOLVIDO") {
      showSupportNotice("Chamado resolvido e cliente notificado.");
    } else {
      showSupportNotice("Chamado reaberto.");
    }
    renderSupport();
    return;
  }
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
  state.selectedSupportId = id;
  if (isLocalPreview) {
    const ticket = state.support.find((item) => item.id === id);
    if (!ticket) return;
    ticket.messages = ticket.messages || [];
    ticket.messages.push({
      sender: "admin",
      message,
      when: new Date().toISOString()
    });
    ticket.status = "EM_ATENDIMENTO";
    ticket.ageLabel = "agora";
    input.value = "";
    showSupportNotice("Resposta enviada ao cliente.");
    renderSupport();
    return;
  }
  await api(`/api/support/${id}/reply`, {
    method: "POST",
    body: JSON.stringify({ message })
  });
  input.value = "";
  await loadRealtimeData({ notifySupport: false, force: true });
  setView("support");
}

function selectSupportTicket(id) {
  state.selectedSupportId = id;
  renderSupport();
}

function useSupportQuickReply(id) {
  const select = qs("#supportQuickReply");
  const input = document.getElementById(`reply-${id}`);
  if (!select?.value || !input) return;
  input.value = select.value;
  input.focus();
  select.value = "";
}

function toggleSupportStep(id, index, checked) {
  const ticket = state.support.find((item) => item.id === id);
  if (!ticket) return;
  ticket.nextSteps = ticket.nextSteps || [
    { label: "Validar vínculo da unidade 2", done: false },
    { label: "Confirmar nova sincronização", done: false },
    { label: "Responder cliente", done: false }
  ];
  if (ticket.nextSteps[index]) ticket.nextSteps[index].done = checked;
}

function openSupportClient(id) {
  const ticket = state.support.find((item) => item.id === id);
  if (!ticket) return;
  const name = supportDisplayName(ticket);
  const clients = state.clientIntelligence?.clients || [];
  const match = clients.find((item) =>
    `${item.name} ${item.businessName} ${item.cnpj}`.toLowerCase().includes(name.toLowerCase())
    || (ticket.cnpj && item.cnpj === ticket.cnpj)
  );
  state.selectedClientId = match?.id || "";
  setView("devices");
  if (qs("#clientIntelligenceSearch")) qs("#clientIntelligenceSearch").value = name;
  renderDevices();
}

function showSupportNotice(message) {
  const notice = qs("#supportNotice");
  if (!notice) return;
  notice.textContent = message;
  notice.classList.remove("hidden");
  window.clearTimeout(showSupportNotice.timer);
  showSupportNotice.timer = window.setTimeout(() => notice.classList.add("hidden"), 2800);
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

function manualVisitDateKey(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function manualVisitDateFromKey(value) {
  const [year, month, day] = String(value || "").split("-").map(Number);
  return new Date(year, Math.max(0, month - 1), day || 1);
}

function loadManualVisits() {
  try {
    const stored = JSON.parse(localStorage.getItem("balcao-livre-manual-visits-v1") || "null");
    if (Array.isArray(stored)) return stored;
  } catch {
    // Mantém a agenda disponível mesmo com o armazenamento local bloqueado.
  }
  const today = manualVisitDateKey(new Date());
  return [
    { id: "manual-1", company: "Padaria Pérola", neighborhood: "Pérola", contact: "Maria Aparecida", date: today, time: "09:30", duration: 45, priority: "Alta", row: "top" },
    { id: "manual-2", company: "Mercado Bom Vizinho", neighborhood: "Pérola", contact: "João Paulo", date: today, time: "10:30", duration: 45, priority: "Média", row: "bottom" },
    { id: "manual-3", company: "Boutique Mineira", neighborhood: "Pérola", contact: "Fernanda Souza", date: today, time: "12:00", duration: 45, priority: "Alta", row: "top" },
    { id: "manual-4", company: "Açaí da Praça", neighborhood: "Pérola", contact: "Rafael Lima", date: today, time: "15:00", duration: 45, priority: "Média", row: "top" },
    { id: "manual-5", company: "Papelaria Central", neighborhood: "Centro", contact: "Ana Beatriz", date: today, time: "11:00", duration: 30, priority: "Média", row: "top" }
  ];
}

const manualVisitsState = {
  date: new Date(),
  visits: loadManualVisits(),
  recentCompanies: [
    { company: "Café do Bairro", neighborhood: "Pérola", contact: "Carlos Alberto", phone: "(31) 99876-5432", icon: "fa-mug-saucer" },
    { company: "Drogaria Pérola", neighborhood: "Pérola", contact: "Juliana Martins", phone: "(31) 98865-2100", icon: "fa-capsules" },
    { company: "Loja Estação", neighborhood: "Pérola", contact: "Rodrigo Ferreira", phone: "(31) 99955-3344", icon: "fa-bag-shopping" }
  ]
};

function persistManualVisits() {
  try {
    localStorage.setItem("balcao-livre-manual-visits-v1", JSON.stringify(manualVisitsState.visits));
  } catch {
    // Mantém a agenda disponível mesmo com o armazenamento local bloqueado.
  }
}

function manualVisitTimeValue(value) {
  const [hour, minute] = String(value || "08:00").split(":").map(Number);
  return hour + minute / 60;
}

function manualVisitDateLabel(date) {
  const formatted = date.toLocaleDateString("pt-BR", {
    weekday: "long",
    day: "2-digit",
    month: "long"
  });
  return formatted.charAt(0).toUpperCase() + formatted.slice(1);
}

function resetManualVisitForm() {
  const form = qs("#manualVisitForm");
  if (!form) return;
  form.reset();
  qs("#manualVisitEditId").value = "";
  qs("#manualVisitDate").value = manualVisitDateKey(manualVisitsState.date);
  qs("#manualVisitTime").value = "09:30";
  qs("#manualVisitDuration").value = "45";
  qs("#manualVisitOwner").value = state.adminProfile.name === "Isabela Gomes" ? "Isabela Gomes" : "Lucas Cesar";
  qs("#manualVisitFormTitle").textContent = "Adicionar visita";
  qs("#manualVisitSubmit").textContent = "Agendar";
  qs("#manualVisitCancelEdit").classList.add("hidden");
}

function renderManualRecentCompanies() {
  const container = qs("#manualRecentCompanies");
  if (!container) return;
  const neighborhood = qs("#manualVisitNeighborhood")?.value || "Pérola";
  const companies = manualVisitsState.recentCompanies.filter((item) =>
    neighborhood === "Todos" || item.neighborhood === neighborhood
  );
  container.innerHTML = companies.length ? companies.map((item) => `
    <button class="manual-recent-company" type="button"
      data-company="${escapeHtml(item.company)}"
      data-neighborhood="${escapeHtml(item.neighborhood)}"
      data-contact="${escapeHtml(item.contact)}">
      <span class="manual-recent-icon"><i class="fa-solid ${escapeHtml(item.icon)}" aria-hidden="true"></i></span>
      <strong>${escapeHtml(item.company)}</strong>
      <span>${escapeHtml(item.neighborhood)}</span>
      <span><i class="fa-regular fa-user" aria-hidden="true"></i>${escapeHtml(item.contact)}</span>
      <span>${escapeHtml(item.phone)}</span>
      <b>Selecionar</b>
      <i class="fa-solid fa-chevron-right" aria-hidden="true"></i>
    </button>
  `).join("") : '<p class="manual-recent-empty">Nenhuma empresa recente neste bairro.</p>';
}

function renderManualVisits() {
  const cards = qs("#manualVisitsCards");
  if (!cards) return;
  const dateKey = manualVisitDateKey(manualVisitsState.date);
  const neighborhood = qs("#manualVisitNeighborhood")?.value || "Pérola";
  const visits = manualVisitsState.visits
    .filter((item) => item.date === dateKey && (neighborhood === "Todos" || item.neighborhood === neighborhood))
    .sort((a, b) => manualVisitTimeValue(a.time) - manualVisitTimeValue(b.time));

  qs("#manualVisitDateLabel").textContent = manualVisitDateLabel(manualVisitsState.date);
  qs("#manualVisitDate").value = dateKey;
  cards.innerHTML = visits.map((item, index) => {
    const position = Math.max(0, Math.min(100, ((manualVisitTimeValue(item.time) - 8) / 9) * 100));
    const row = item.row || (item.id === "manual-2" || item.time === "10:30" ? "bottom" : "top");
    const priorityClass = item.priority === "Alta" ? "high" : "medium";
    return `
      <article class="manual-visit-card ${row}" style="--visit-position: ${position}%">
        <span class="manual-visit-connector" aria-hidden="true"></span>
        <strong class="manual-visit-time">${escapeHtml(item.time)}</strong>
        <h3>${escapeHtml(item.company)}</h3>
        <p><i class="fa-solid fa-location-dot" aria-hidden="true"></i>Bairro ${escapeHtml(item.neighborhood)}</p>
        <p><i class="fa-regular fa-user" aria-hidden="true"></i>${escapeHtml(item.contact)}</p>
        <p>
          <i class="fa-regular fa-clock" aria-hidden="true"></i>${escapeHtml(item.duration)} min
          <span class="manual-visit-separator">•</span>
          <span class="manual-visit-priority ${priorityClass}"><i class="fa-solid ${item.priority === "Alta" ? "fa-angles-up" : "fa-minus"}" aria-hidden="true"></i>${escapeHtml(item.priority)}</span>
        </p>
        <button class="manual-visit-edit" type="button" data-visit-edit="${escapeHtml(item.id)}">
          <i class="fa-solid fa-pen" aria-hidden="true"></i>Editar
        </button>
      </article>
    `;
  }).join("");

  const empty = qs("#manualVisitsEmpty");
  empty.classList.toggle("hidden", visits.length > 0);
  qs(".manual-visits-axis")?.classList.toggle("is-empty", visits.length === 0);
  renderManualRecentCompanies();
}

function shiftManualVisitDay(amount) {
  const next = new Date(manualVisitsState.date);
  next.setDate(next.getDate() + amount);
  manualVisitsState.date = next;
  resetManualVisitForm();
  renderManualVisits();
}

function openManualVisitForm() {
  qs("#manualVisitForm")?.scrollIntoView({ behavior: "smooth", block: "center" });
  window.setTimeout(() => qs("#manualVisitCompany")?.focus(), 260);
}

function editManualVisit(id) {
  const visit = manualVisitsState.visits.find((item) => item.id === id);
  if (!visit) return;
  qs("#manualVisitEditId").value = visit.id;
  qs("#manualVisitCompany").value = visit.company;
  qs("#manualVisitDate").value = visit.date;
  qs("#manualVisitTime").value = visit.time;
  qs("#manualVisitDuration").value = String(visit.duration);
  if ([...qs("#manualVisitOwner").options].some((option) => option.value === visit.contact)) {
    qs("#manualVisitOwner").value = visit.contact;
  }
  qs("#manualVisitFormTitle").textContent = "Editar visita";
  qs("#manualVisitSubmit").textContent = "Salvar";
  qs("#manualVisitCancelEdit").classList.remove("hidden");
  openManualVisitForm();
}

function saveManualVisit(event) {
  event.preventDefault();
  const company = qs("#manualVisitCompany").value.trim();
  if (!company) {
    qs("#manualVisitCompany").focus();
    return;
  }
  const editId = qs("#manualVisitEditId").value;
  const neighborhood = qs("#manualVisitNeighborhood").value === "Todos"
    ? "Pérola"
    : qs("#manualVisitNeighborhood").value;
  const existing = manualVisitsState.visits.find((item) => item.id === editId);
  const next = {
    id: editId || `manual-${Date.now()}`,
    company,
    neighborhood: existing?.neighborhood || neighborhood,
    contact: qs("#manualVisitOwner").value,
    date: qs("#manualVisitDate").value,
    time: qs("#manualVisitTime").value,
    duration: Number(qs("#manualVisitDuration").value || 45),
    priority: existing?.priority || "Média",
    row: existing?.row || "top"
  };
  if (existing) Object.assign(existing, next);
  else manualVisitsState.visits.push(next);
  manualVisitsState.date = manualVisitDateFromKey(next.date);
  persistManualVisits();
  resetManualVisitForm();
  renderManualVisits();
  showVisitsNotice(existing ? "Visita atualizada." : `${company} foi adicionada à agenda.`);
}

function selectManualRecentCompany(button) {
  qs("#manualVisitCompany").value = button.dataset.company || "";
  const neighborhood = button.dataset.neighborhood || "Pérola";
  if ([...qs("#manualVisitNeighborhood").options].some((option) => option.value === neighborhood)) {
    qs("#manualVisitNeighborhood").value = neighborhood;
  }
  openManualVisitForm();
}


qs("#loginForm")?.addEventListener("submit", (event) => {
  event.preventDefault();
  login();
});
qs("#loginPasswordToggle")?.addEventListener("click", (event) => {
  const button = event.currentTarget;
  const input = qs("#loginPassword");
  const showingPassword = input.type === "text";
  input.type = showingPassword ? "password" : "text";
  button.setAttribute("aria-pressed", String(!showingPassword));
  button.setAttribute("aria-label", showingPassword ? "Mostrar senha" : "Ocultar senha");
  button.querySelector("i")?.classList.toggle("fa-eye", showingPassword);
  button.querySelector("i")?.classList.toggle("fa-eye-slash", !showingPassword);
});
qs("#forgotPasswordLink")?.addEventListener("click", () => {
  qs("#loginMessage").textContent = "Solicite a redefinição da senha ao responsável pelo painel.";
});
qs("#logoutButton").addEventListener("click", logout);
qs("#createKeyButton").addEventListener("click", createKey);
qs("#licenseSearch").addEventListener("input", renderLicenses);
document.querySelectorAll("[data-license-filter]").forEach((button) => {
  button.addEventListener("click", () => {
    state.licenseFilter = button.dataset.licenseFilter || "all";
    state.selectedLicenseId = "";
    renderLicenses();
  });
});
qs("#blockedLicenseIpsButton")?.addEventListener("click", () => {
  setView("devices");
});
qs("#supportSearch").addEventListener("input", renderSupport);
document.querySelectorAll("[data-support-filter]").forEach((button) => {
  button.addEventListener("click", () => {
    state.supportFilter = button.dataset.supportFilter || "all";
    renderSupport();
  });
});
qs("#clientIntelligenceSearch")?.addEventListener("input", renderDevices);
qs("#clientHealthFilter")?.addEventListener("change", (event) => {
  state.clientHealthFilter = event.target.value || "all";
  state.selectedClientId = "";
  renderDevices();
});
qs("#scoreHelpButton")?.addEventListener("click", () => {
  const panel = qs("#scoreMethodology");
  const button = qs("#scoreHelpButton");
  const isHidden = panel.classList.toggle("hidden");
  button.setAttribute("aria-expanded", String(!isHidden));
});
qs("#manualVisitPreviousDay")?.addEventListener("click", () => shiftManualVisitDay(-1));
qs("#manualVisitNextDay")?.addEventListener("click", () => shiftManualVisitDay(1));
qs("#manualVisitToday")?.addEventListener("click", () => {
  manualVisitsState.date = new Date();
  resetManualVisitForm();
  renderManualVisits();
});
qs("#manualVisitOpenForm")?.addEventListener("click", openManualVisitForm);
qs("#manualVisitNeighborhood")?.addEventListener("change", renderManualVisits);
qs("#manualVisitForm")?.addEventListener("submit", saveManualVisit);
qs("#manualVisitCancelEdit")?.addEventListener("click", resetManualVisitForm);
qs("#manualVisitsCards")?.addEventListener("click", (event) => {
  const button = event.target.closest("[data-visit-edit]");
  if (button) editManualVisit(button.dataset.visitEdit);
});
qs("#manualRecentCompanies")?.addEventListener("click", (event) => {
  const button = event.target.closest(".manual-recent-company");
  if (button) selectManualRecentCompany(button);
});
document.querySelectorAll("[data-visit-outcome]").forEach((button) => {
  button.addEventListener("click", () => setVisitOutcome(button.dataset.visitOutcome || "undecided"));
});
qs("#optimizeVisitsRoute")?.addEventListener("click", optimizeVisitsRoute);
qs("#showAllNearbyVisits")?.addEventListener("click", toggleAllNearbyVisits);
qs("#enableVisitLocation")?.addEventListener("click", enableVisitLocation);
qs("#startVisitsRoute")?.addEventListener("click", startVisitsRoute);
document.querySelectorAll("[data-visit-panel]").forEach((button) => {
  button.addEventListener("click", () => setVisitPanel(button.dataset.visitPanel || "route"));
});
qs("#generateVisitTrial")?.addEventListener("click", generateVisitTrial);
qs("#saveVisitAndContinue")?.addEventListener("click", saveVisitAndContinue);
qs("#visitNotes")?.addEventListener("change", () => {
  captureVisitDraft();
  persistVisitData();
});
document.querySelectorAll(".nav[data-view]").forEach((button) => {
  button.addEventListener("click", () => setView(button.dataset.view));
});
document.querySelectorAll("[data-view-target]").forEach((button) => {
  button.addEventListener("click", () => setView(button.dataset.viewTarget));
});
document.querySelectorAll("[data-dashboard-range]").forEach((button) => {
  button.addEventListener("click", () => {
    state.dashboardRange = Number(button.dataset.dashboardRange || 30);
    renderOperationsChart(state.dashboard?.stripe?.revenueByDay || []);
  });
});

qs("#globalSearch").addEventListener("keydown", (event) => {
  if (event.key !== "Enter") return;
  const query = event.currentTarget.value.trim();
  if (!query) return;

  const supportMatch = state.support.some((item) =>
    `${item.shortId} ${item.customerName} ${item.businessName} ${item.ownerName} ${item.phone} ${item.message}`
      .toLowerCase()
      .includes(query.toLowerCase())
  );
  const clientMatch = (state.clientIntelligence?.clients || []).some((item) =>
    `${item.name} ${item.cnpj} ${item.city} ${item.state} ${item.licenseKey}`
      .toLowerCase()
      .includes(query.toLowerCase())
  );
  ensureVisitData();
  const visitMatch = state.visitLeads.find((item) =>
    `${item.name} ${item.neighborhood} ${item.action}`
      .toLowerCase()
      .includes(query.toLowerCase())
  );

  if (visitMatch) {
    state.selectedVisitId = visitMatch.id;
    state.visitOutcome = visitMatch.outcome || "undecided";
    setView("visits");
  } else if (clientMatch) {
    setView("devices");
    qs("#clientIntelligenceSearch").value = query;
    renderDevices();
  } else if (supportMatch) {
    setView("support");
    qs("#supportSearch").value = query;
    renderSupport();
  } else {
    setView("licenses");
    qs("#licenseSearch").value = query;
    renderLicenses();
  }
});

document.addEventListener("click", (event) => {
  const profile = qs(".admin-profile");
  if (profile?.open && !profile.contains(event.target)) profile.removeAttribute("open");
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
      renderAdminProfile(session);
      showApp();
      startLiveRefresh();
      return loadRealtimeData({ notifySupport: false, force: true });
    }
    showLogin();
  })
  .catch(showLogin);

renderDownloads();
