const config = window.BALCAO_CARDAPIO_CONFIG || {};
const qs = (selector) => document.querySelector(selector);
let lastRenderSignature = "";
let currentMenu = null;
let currentItems = [];
let currentSearch = "";
let currentMode = readStorage("balcao.cardapio.mode", "");
let customerFields = readStorage("balcao.cardapio.customer", {});
let cart = readStorage("balcao.cardapio.cart", []);
let orderNotes = readStorage("balcao.cardapio.notes", "");
let currentPage = normalizePage(location.hash.replace("#", "")) || readStorage("balcao.cardapio.page", "home");
let orderHistory = readStorage("balcao.cardapio.history", []);
let orderStatusPollTimer = 0;
let orderStatusPollRunning = false;

function currentSlug() {
  const url = new URL(window.location.href);
  const fromQuery = url.searchParams.get("loja") || url.searchParams.get("menu");
  if (fromQuery) return fromQuery.trim();

  const apexDomain = String(config.apexDomain || "balcaolivrepdv.com.br").toLowerCase();
  const host = url.hostname.toLowerCase();
  const reservedSubdomains = new Set(["admin", "api", "app", "cardapio", "pdv", "www"]);
  if (apexDomain && host.endsWith(`.${apexDomain}`)) {
    const subdomain = host.slice(0, -(apexDomain.length + 1)).split(".").filter(Boolean).pop();
    if (subdomain && !reservedSubdomains.has(subdomain)) {
      return subdomain.trim();
    }
  }

  const cleanPath = url.pathname
    .replace(/^\/cardapio\/?/i, "")
    .split("/")
    .filter(Boolean);
  if (cleanPath.length >= 2 && /^\d{3}$/.test(cleanPath[0])) {
    return `${cleanPath[0]}-${cleanPath[1]}`;
  }

  return cleanPath[0] || "demo";
}

function readStorage(key, fallback) {
  try {
    const value = localStorage.getItem(`${key}.${currentSlug()}`);
    return value ? JSON.parse(value) : fallback;
  } catch {
    return fallback;
  }
}

function writeStorage(key, value) {
  try {
    localStorage.setItem(`${key}.${currentSlug()}`, JSON.stringify(value));
  } catch {
    // Storage is optional; the menu still works without persistence.
  }
}

function money(value) {
  return Number(value || 0).toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL"
  });
}

function escapeHtml(value) {
  return String(value || "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function categoryId(category) {
  return `cat-${normalizeText(category).replace(/[^a-z0-9]+/g, "-") || "cardapio"}`;
}

function itemKey(item, index = 0) {
  return normalizeText(item.code || `${item.category}-${item.name}-${index}`).replace(/[^a-z0-9]+/g, "-");
}

function firstLetters(value) {
  const words = String(value || "")
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2);
  return (words.map((word) => word[0]).join("") || "BL").toUpperCase();
}

function validImageUrl(value) {
  const url = String(value || "").trim();
  return /^https?:\/\//i.test(url) ? url : "";
}

function iconSvg(name, className = "app-icon") {
  const icons = {
    home: '<path d="m3 11 9-8 9 8"/><path d="M5 10v10h14V10"/><path d="M9 20v-6h6v6"/>',
    receipt: '<path d="M5 4h14v16l-2-1-2 1-2-1-2 1-2-1-2 1V4Z"/><path d="M8 8h8M8 12h8M8 16h5"/>',
    tag: '<path d="M20 12v7a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-7"/><path d="M2 7h20v5H2z"/><path d="M12 22V7"/><path d="M12 7H7.5A2.5 2.5 0 1 1 12 4.5V7Z"/><path d="M12 7h4.5A2.5 2.5 0 1 0 12 4.5V7Z"/>',
    percent: '<path d="m19 5-14 14"/><circle cx="7" cy="7" r="2"/><circle cx="17" cy="17" r="2"/>',
    plus: '<path d="M12 5v14M5 12h14"/>',
    check: '<path d="m5 12 4 4L19 6"/>',
    search: '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.2-3.2"/>',
    store: '<path d="M4 10h16l-1-5H5l-1 5Z"/><path d="M6 10v9h12v-9"/><path d="M9 19v-5h6v5"/>',
    clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    info: '<circle cx="12" cy="12" r="9"/><path d="M12 10v6M12 7h.01"/>',
    phone: '<path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.3 1.8.6 2.6a2 2 0 0 1-.5 2.1L8 9.6a16 16 0 0 0 6.4 6.4l1.2-1.2a2 2 0 0 1 2.1-.5c.8.3 1.7.5 2.6.6a2 2 0 0 1 1.7 2Z"/>',
    whatsapp: '<path d="M20 11.8a8 8 0 0 1-11.7 7.1L4 20l1.1-4.1A8 8 0 1 1 20 11.8Z"/><path d="M9 8.7c.2 3 2.4 5.1 5.3 5.6l1.1-1.2-2-.9-.9.8c-1.1-.5-1.9-1.3-2.4-2.4l.8-.9-.9-2-1 .9Z"/>',
    map: '<path d="m9 18-6 3V6l6-3 6 3 6-3v15l-6 3-6-3Z"/><path d="M9 3v15M15 6v15"/>',
    drink: '<path d="M8 3h8l-1 9a3 3 0 0 1-6 0L8 3Z"/><path d="M12 15v6M9 21h6"/>',
    pizza: '<path d="M4 20 20 4c-6-1-12 1-16 16Z"/><path d="M8 13h.01M12 9h.01M11 16h.01"/>',
    burger: '<path d="M4 11c1-4 15-4 16 0H4Z"/><path d="M5 15h14"/><path d="M6 18h12"/><path d="M5 13h14"/>',
    dessert: '<path d="M7 9h10l-1 9H8L7 9Z"/><path d="M9 9V7a3 3 0 0 1 6 0v2"/><path d="M8 13h8"/>',
    delivery: '<path d="M3 7h10v9H3z"/><path d="M13 10h4l3 3v3h-7z"/><circle cx="7" cy="18" r="2"/><circle cx="17" cy="18" r="2"/>',
    food: '<path d="M7 3v8M4 3v8M10 3v8M4 11h6"/><path d="M7 11v10"/><path d="M17 3v18"/><path d="M14 3h6v8h-6z"/>'
  };
  const paths = icons[name] || icons.food;
  return `<svg class="${escapeHtml(className)}" viewBox="0 0 24 24" aria-hidden="true">${paths}</svg>`;
}

function categoryIcon(category) {
  const text = normalizeText(category);
  if (text.includes("bebida") || text.includes("drink") || text.includes("suco") || text.includes("bar")) return "drink";
  if (text.includes("pizza")) return "pizza";
  if (text.includes("lanche") || text.includes("burger") || text.includes("hamb")) return "burger";
  if (text.includes("sobremesa") || text.includes("doce")) return "dessert";
  if (text.includes("delivery") || text.includes("entrega")) return "delivery";
  return "food";
}

function phoneDigits(value) {
  return String(value || "").replace(/\D/g, "");
}

function formatPhone(value) {
  let digits = phoneDigits(value);
  if (digits.startsWith("55") && digits.length > 11) digits = digits.slice(2);
  if (digits.length === 11) return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
  if (digits.length === 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  return String(value || "").trim();
}

function menuAddress(menu = currentMenu) {
  return [menu?.address, menu?.city, menu?.state].filter(Boolean).join(", ");
}

function mapLinks(address) {
  const query = encodeURIComponent(address);
  return {
    embed: `https://www.google.com/maps?q=${query}&output=embed`,
    google: `https://www.google.com/maps/dir/?api=1&destination=${query}`,
    waze: `https://waze.com/ul?q=${query}&navigate=yes`,
    apple: `https://maps.apple.com/?q=${query}`
  };
}

function formatInfo(menu) {
  const description = String(menu.description || "").trim();
  const defaultDescription = /^cardapio digital atualizado pelo balcao livre pdv/i.test(normalizeText(description));
  if (description && !defaultDescription) {
    return description;
  }

  return menu.store_open === false
    ? "Loja pausada no momento."
    : "Escolha seus itens e envie o pedido para a loja.";
}

function waitTimeText(menu = currentMenu) {
  const min = Math.max(1, Math.round(Number(menu?.wait_min_minutes || 30)));
  const max = Math.max(min, Math.round(Number(menu?.wait_max_minutes || 60)));
  return min === max ? `${min} min` : `${min} a ${max} min`;
}

function isGeneratedDefaultDiscount(menu) {
  const code = String(menu?.discount_code || "").trim().toUpperCase();
  const amount = Number(menu?.discount_amount ?? 0);
  const description = String(menu?.discount_description || "").trim();
  return code === "EXCLUSIVO4"
    && Math.abs(amount - 4) < 0.001
    && description === "Use no atendimento para ganhar desconto no pedido.";
}

function isGeneratedDefaultLoyalty(menu) {
  const goal = Math.max(1, Math.round(Number(menu?.loyalty_goal || 20)));
  const minimum = Math.max(0, Number(menu?.loyalty_minimum_order || 20));
  return goal === 20 && Math.abs(minimum - 20) < 0.001;
}

function activeDiscount(menu = currentMenu) {
  if (!menu || menu.discount_enabled !== true || isGeneratedDefaultDiscount(menu)) return null;
  const code = String(menu.discount_code || "").trim().toUpperCase();
  const amount = Number(menu.discount_amount ?? 0);
  if (!code || !Number.isFinite(amount) || amount <= 0) return null;
  return {
    code,
    amount,
    description: String(menu.discount_description || "Apresente este cupom no atendimento para receber o desconto.").trim()
  };
}

function loyaltyConfig(menu = currentMenu) {
  if (!menu || menu.loyalty_enabled !== true || isGeneratedDefaultLoyalty(menu)) return null;
  const goal = Math.max(1, Math.round(Number(menu.loyalty_goal || 20)));
  const minimum = Math.max(0, Number(menu.loyalty_minimum_order || 20));
  return { goal, minimum };
}

function hasDiscountArea(menu = currentMenu) {
  return Boolean(activeDiscount(menu) || loyaltyConfig(menu));
}

function normalizeOrderStatus(value) {
  return String(value || "")
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

function publicOrderTypeToMode(value) {
  const normalized = normalizeOrderStatus(value);
  if (["DELIVERY", "ENTREGA"].includes(normalized)) return "delivery";
  if (["PICKUP", "RETIRADA", "TAKEOUT"].includes(normalized)) return "pickup";
  if (["TABLE", "MESA", "LOCAL", "MESA_LOCAL"].includes(normalized)) return "table";
  return "";
}

function shortOrderId(value) {
  const clean = String(value || "").replaceAll("-", "").trim();
  return clean ? clean.slice(-8).toUpperCase() : "";
}

function orderStatusInfo(order) {
  const status = normalizeOrderStatus(order.status);
  const mode = order.mode || publicOrderTypeToMode(order.orderType);
  const isDelivery = mode === "delivery";
  const isPickup = mode === "pickup";
  const isTable = mode === "table";

  if (["CANCELADO", "CANCELED", "CANCELLED", "ERRO"].includes(status)) {
    return { label: "Cancelado", detail: "Esse pedido foi cancelado pelo restaurante.", tone: "danger", step: 0 };
  }

  if (["ENTREGUE", "FINALIZADO", "CONCLUIDO", "CONCLUDED"].includes(status)) {
    return {
      label: isPickup ? "Retirado" : isTable ? "Entregue na mesa" : "Entregue",
      detail: isPickup ? "Pedido retirado no balcao." : isTable ? "Pedido entregue na mesa/local." : "Pedido entregue ao cliente.",
      tone: "done",
      step: 3
    };
  }

  if (["ROTA", "DESPACHADO", "IN_DELIVERY", "ON_THE_WAY"].includes(status)) {
    return {
      label: isPickup ? "Pronto para retirada" : isTable ? "Indo ate a mesa" : "Saiu para entrega",
      detail: isPickup
        ? "Pode retirar no balcao."
        : isTable
          ? "A equipe ja esta levando ate sua mesa/local."
          : "O pedido saiu do restaurante para entrega.",
      tone: "done",
      step: 2
    };
  }

  if (status === "PRONTO") {
    return {
      label: isPickup ? "Pronto para retirada" : isTable ? "Indo ate a mesa" : "Pronto para sair",
      detail: isPickup
        ? "Pode retirar no balcao."
        : isTable
          ? "A equipe vai levar ate sua mesa/local."
          : "O pedido esta pronto para despacho.",
      tone: "ready",
      step: 2
    };
  }

  if (["PREPARO", "PREPARANDO", "PREPARATION_STARTED"].includes(status)) {
    return {
      label: "Em preparo",
      detail: isDelivery ? "A cozinha esta preparando seu pedido para entrega." : "A cozinha esta preparando seu pedido.",
      tone: "active",
      step: 1
    };
  }

  if (["RECEBIDO", "IMPORTADO", "CONFIRMADO", "ACEITO"].includes(status)) {
    return {
      label: "Recebido no PDV",
      detail: "O restaurante ja recebeu o pedido no sistema.",
      tone: "active",
      step: 0
    };
  }

  return {
    label: "Enviado ao PDV",
    detail: "Aguardando o restaurante receber no sistema.",
    tone: "pending",
    step: 0
  };
}

function orderProgressLabels(order) {
  const mode = order.mode || publicOrderTypeToMode(order.orderType);
  if (mode === "delivery") return ["Recebido", "Preparo", "Entrega", "Concluido"];
  if (mode === "pickup") return ["Recebido", "Preparo", "Retirada", "Concluido"];
  return ["Recebido", "Preparo", "Mesa", "Concluido"];
}

function renderOrderProgress(order, statusInfo) {
  return `
    <div class="order-progress" aria-label="Andamento do pedido">
      ${orderProgressLabels(order).map((label, index) => `
        <span class="${index <= statusInfo.step ? "done" : ""}">${escapeHtml(label)}</span>
      `).join("")}
    </div>
  `;
}

function setThemeColor(color) {
  const accent = "#071A2C";
  document.documentElement.style.setProperty("--accent", accent);
  document.documentElement.style.setProperty("--accent-strong", "#03111f");
  document.documentElement.style.setProperty("--accent-soft", "#E8F0F6");
  document.querySelector('meta[name="theme-color"]')?.setAttribute("content", accent);
}

function normalizePage(value) {
  const page = String(value || "").toLowerCase();
  return ["home", "history", "discounts", "profile"].includes(page) ? page : "";
}

function displayImageUrl(value) {
  const url = String(value || "").trim();
  if (/^data:image\/(png|jpe?g|webp|gif|bmp);base64,/i.test(url)) return url;
  return validImageUrl(url);
}

function setCoverImage(menu, items) {
  const image = displayImageUrl(menu?.cover_image_url) || displayImageUrl(items.find((item) => displayImageUrl(item.image_url))?.image_url);
  document.documentElement.style.setProperty("--cover-image", image ? `url("${image.replaceAll('"', "%22")}")` : "none");
  document.body.classList.toggle("has-cover-image", Boolean(image));
}

function setPage(page, shouldScroll = true) {
  currentPage = normalizePage(page) || "home";
  writeStorage("balcao.cardapio.page", currentPage);
  if (location.hash.replace("#", "") !== currentPage) {
    history.replaceState(null, "", `#${currentPage}`);
  }
  document.querySelectorAll("[data-page-panel]").forEach((panel) => {
    panel.classList.toggle("active", panel.dataset.pagePanel === currentPage);
  });
  document.querySelectorAll("[data-nav-page]").forEach((button) => {
    button.classList.toggle("active", button.dataset.navPage === currentPage);
  });
  renderHistory();
  renderDiscounts();
  renderProfile();
  if (currentPage === "history") {
    refreshOrderStatuses();
  }
  if (shouldScroll) {
    window.scrollTo({ top: 0, behavior: "smooth" });
  }
}

function updateDiscountNavigation() {
  document.querySelectorAll('.bottom-nav [data-nav-page="discounts"]').forEach((item) => {
    item.hidden = false;
  });
  const page = qs("#pageDiscounts");
  if (page) page.hidden = false;
}

async function supabaseGet(path) {
  const baseUrl = String(config.supabaseUrl || "").replace(/\/$/, "");
  const key = String(config.publishableKey || config.anonKey || "").trim();
  if (!baseUrl || !key) {
    throw new Error("Cardapio sem configuracao do Supabase.");
  }

  const response = await fetch(`${baseUrl}/rest/v1${path}`, {
    headers: {
      apikey: key,
      Authorization: `Bearer ${key}`,
      Accept: "application/json"
    }
  });
  const data = await response.json().catch(() => []);
  if (!response.ok) {
    throw new Error(data.message || data.error || `HTTP ${response.status}`);
  }

  return data;
}

function licenseFunctionUrl(path) {
  const configured = String(config.licenseFunctionUrl || "").replace(/\/$/, "");
  const baseUrl = configured || `${String(config.supabaseUrl || "").replace(/\/$/, "")}/functions/v1/license`;
  if (!baseUrl || !String(config.supabaseUrl || "").trim()) {
    throw new Error("Cardapio sem endpoint de pedidos.");
  }

  return `${baseUrl}/${String(path || "").replace(/^\/+/, "")}`;
}

async function licensePost(path, payload) {
  const response = await fetch(licenseFunctionUrl(path), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json"
    },
    body: JSON.stringify(payload)
  });
  const data = await response.json().catch(() => ({}));
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || data.error || `HTTP ${response.status}`);
  }

  return data;
}

function setStatus(message, isError = false) {
  const status = qs("#status");
  status.textContent = message;
  status.classList.toggle("error", isError);
}

function renderActions(menu) {
  const actions = qs("#restaurantActions");
  const digits = phoneDigits(menu.phone);
  const links = [];
  if (digits.length >= 10) {
    const whatsappDigits = digits.startsWith("55") ? digits : `55${digits}`;
    const displayPhone = formatPhone(menu.phone);
    const whatsappText = encodeURIComponent(`Ola, vim pelo cardapio digital da ${menu.name || "loja"}.`);
    links.push(`<a class="action-link primary" href="https://wa.me/${whatsappDigits}?text=${whatsappText}" target="_blank" rel="noopener">${iconSvg("whatsapp")}<span><b>WhatsApp</b><small>${escapeHtml(displayPhone)}</small></span></a>`);
    links.push(`<a class="action-link" href="tel:+${whatsappDigits}">${iconSvg("phone")}<span><b>Ligar</b><small>${escapeHtml(displayPhone)}</small></span></a>`);
  }

  const address = menuAddress(menu);
  if (address) {
    links.push(`<button type="button" class="action-link" data-open-address>${iconSvg("map")}<span><b>Endereco</b><small>${escapeHtml(address)}</small></span></button>`);
  }

  actions.innerHTML = links.join("");
  actions.hidden = links.length === 0;
}

function renderAddressModal() {
  const body = qs("#addressPanelBody");
  if (!body || !currentMenu) return;
  const address = menuAddress(currentMenu);
  if (!address) return;
  const links = mapLinks(address);
  body.innerHTML = `
    <div class="address-map-wrap">
      <iframe class="address-map" title="Mapa da loja" src="${links.embed}" loading="lazy" referrerpolicy="no-referrer-when-downgrade"></iframe>
      <div class="delivery-radius-preview" aria-hidden="true">
        <span></span>
        <b>Raio</b>
      </div>
    </div>
    <div class="address-summary">
      <strong>${escapeHtml(currentMenu.name || "Loja")}</strong>
      <p>${escapeHtml(address)}</p>
    </div>
    <div class="map-actions">
      <a class="action-link primary" href="${links.waze}" target="_blank" rel="noopener">Abrir no Waze</a>
      <a class="action-link" href="${links.google}" target="_blank" rel="noopener">Google Maps</a>
      <a class="action-link" href="${links.apple}" target="_blank" rel="noopener">Maps iOS</a>
    </div>
  `;
}

function openAddressModal() {
  renderAddressModal();
  const overlay = qs("#addressOverlay");
  if (overlay) overlay.hidden = false;
}

function renderInfoModal() {
  const body = qs("#infoPanelBody");
  if (!body || !currentMenu) return;
  const address = menuAddress(currentMenu);
  const digits = phoneDigits(currentMenu.phone);
  const whatsappDigits = digits.length >= 10 ? (digits.startsWith("55") ? digits : `55${digits}`) : "";
  const whatsappText = encodeURIComponent(`Ola, vim pelo cardapio digital da ${currentMenu.name || "loja"}.`);
  const displayPhone = formatPhone(currentMenu.phone);
  body.innerHTML = `
    <div class="info-restaurant-card">
      <strong>${escapeHtml(currentMenu.name || "Restaurante")}</strong>
      <p>${escapeHtml(currentMenu.description || "Cardapio digital publicado pelo Balcao Livre PDV.")}</p>
    </div>
    <div class="info-list">
      ${displayPhone ? `<div>${iconSvg("phone")}<span><b>Telefone</b><small>${escapeHtml(displayPhone)}</small></span></div>` : ""}
      ${address ? `<div>${iconSvg("map")}<span><b>Endereco</b><small>${escapeHtml(address)}</small></span></div>` : ""}
      <div>${iconSvg("store")}<span><b>Status</b><small>${currentMenu.store_open === false ? "Loja pausada no momento" : "Cardapio disponivel"}</small></span></div>
    </div>
    <div class="map-actions">
      ${whatsappDigits ? `<a class="action-link primary" href="https://wa.me/${whatsappDigits}?text=${whatsappText}" target="_blank" rel="noopener">WhatsApp</a>` : ""}
      ${whatsappDigits ? `<a class="action-link" href="tel:+${whatsappDigits}">Ligar</a>` : ""}
      ${address ? `<button type="button" class="action-link" data-open-address>Ver mapa</button>` : ""}
    </div>
  `;
}

function openInfoModal() {
  renderInfoModal();
  const overlay = qs("#infoOverlay");
  if (overlay) overlay.hidden = false;
}

function renderCouponCard(menu) {
  const card = qs(".coupon-card");
  if (!card) return;
  const discount = activeDiscount(menu);
  const loyalty = loyaltyConfig(menu);
  card.hidden = !discount && !loyalty;
  if (!discount && !loyalty) return;

  if (discount) {
    card.innerHTML = `
      <div class="coupon-icon">${iconSvg("tag", "coupon-svg")}</div>
      <div>
        <span>Aplique o cupom</span>
        <strong>${escapeHtml(discount.code)}</strong>
        <span class="coupon-amount">${escapeHtml(money(discount.amount))} de desconto</span>
        <span>${escapeHtml(discount.description)}</span>
      </div>
      <b aria-hidden="true">&rsaquo;</b>
    `;
    return;
  }

  card.innerHTML = `
    <div class="coupon-icon">${iconSvg("tag", "coupon-svg")}</div>
    <div>
      <span>Fidelidade da loja</span>
      <strong>${escapeHtml(loyalty.goal)} pontos</strong>
      <span class="coupon-amount">Pedido minimo ${escapeHtml(money(loyalty.minimum))}</span>
      <span>Acompanhe seus pontos no cardapio.</span>
    </div>
    <b aria-hidden="true">&rsaquo;</b>
  `;
}

function renderFeatured(items) {
  const host = qs("#featuredSection");
  if (!host) return;
  const featured = items
    .filter((item) => item.is_in_stock !== false)
    .filter((item) => displayImageUrl(item.image_url))
    .slice(0, 4);
  if (!featured.length) {
    host.innerHTML = "";
    return;
  }

  host.innerHTML = `
    <div class="featured-title">Os favoritos da galera</div>
    <div class="featured-row">
      ${featured.map((item) => {
        const index = currentItems.indexOf(item);
        const key = itemKey(item, index);
        const imageUrl = displayImageUrl(item.image_url);
        return `
          <button type="button" class="featured-card" data-add-item="${escapeHtml(key)}">
            <div class="item-media">
              ${imageUrl
                ? `<img src="${escapeHtml(imageUrl)}" alt="${escapeHtml(item.name)}" loading="lazy">`
                : `<span>${escapeHtml(firstLetters(item.name))}</span>`}
              <span class="quick-add">${iconSvg("plus")}</span>
            </div>
            <div class="featured-body">
              <strong>${escapeHtml(item.name)}</strong>
              <span class="price">${money(item.price)}</span>
            </div>
          </button>
        `;
      }).join("")}
    </div>
  `;
}

function modeLabel(mode = currentMode) {
  return {
    delivery: "Entrega",
    pickup: "Retirada",
    table: "Mesa/local"
  }[mode] || "Nao definido";
}

function renderOrderPanel() {
  document.querySelectorAll("[data-order-mode]").forEach((button) => {
    button.classList.toggle("active", button.dataset.orderMode === currentMode);
  });

  const fields = qs("#orderFields");
  const value = (name) => escapeHtml(customerFields[name] || "");
  if (currentMode === "delivery") {
    fields.innerHTML = `
      <label class="field"><span>Nome</span><input data-order-field="name" value="${value("name")}" placeholder="Seu nome"></label>
      <label class="field"><span>Celular</span><input data-order-field="phone" value="${value("phone")}" placeholder="DDD + numero"></label>
      <label class="field wide"><span>Endereco de entrega</span><input data-order-field="address" value="${value("address")}" placeholder="Rua, numero, bairro"></label>
      <label class="field wide"><span>Referencia</span><input data-order-field="reference" value="${value("reference")}" placeholder="Opcional"></label>
    `;
    return;
  }

  if (currentMode === "pickup") {
    fields.innerHTML = `
      <label class="field"><span>Nome</span><input data-order-field="name" value="${value("name")}" placeholder="Nome para retirada"></label>
      <label class="field"><span>Celular</span><input data-order-field="phone" value="${value("phone")}" placeholder="DDD + numero"></label>
      <label class="field wide"><span>Horario desejado</span><input data-order-field="time" value="${value("time")}" placeholder="Opcional"></label>
    `;
    return;
  }

  if (currentMode === "table") {
    fields.innerHTML = `
      <label class="field"><span>Mesa ou comanda</span><input data-order-field="table" value="${value("table")}" placeholder="Ex.: mesa 4"></label>
      <label class="field"><span>Nome</span><input data-order-field="name" value="${value("name")}" placeholder="Opcional"></label>
    `;
    return;
  }

  fields.innerHTML = "";
}

function filterItems(items) {
  const query = normalizeText(currentSearch);
  if (!query) return items;
  return items.filter((item) => {
    const haystack = normalizeText([
      item.name,
      item.description,
      item.category,
      item.code
    ].filter(Boolean).join(" "));
    return haystack.includes(query);
  });
}

function cartQuantity(key) {
  return cart.find((line) => line.key === key)?.quantity || 0;
}

function cartCount() {
  return cart.reduce((total, line) => total + line.quantity, 0);
}

function cartTotal() {
  return cart.reduce((total, line) => total + (Number(line.price) || 0) * line.quantity, 0);
}

function saveCart() {
  writeStorage("balcao.cardapio.cart", cart);
  writeStorage("balcao.cardapio.customer", customerFields);
  writeStorage("balcao.cardapio.notes", orderNotes);
}

function addItemByKey(key) {
  const item = currentItems.find((entry, index) => itemKey(entry, index) === key);
  if (!item || item.is_in_stock === false) return;

  const existing = cart.find((line) => line.key === key);
  if (existing) {
    existing.quantity += 1;
  } else {
    cart.push({
      key,
      code: item.code || "",
      name: item.name || "Produto",
      category: item.category || "Cardapio",
      price: Number(item.price || 0),
      quantity: 1
    });
  }

  saveCart();
  renderEverything();
}

function changeCartQuantity(key, delta) {
  cart = cart
    .map((line) => line.key === key ? { ...line, quantity: Math.max(0, line.quantity + delta) } : line)
    .filter((line) => line.quantity > 0);
  saveCart();
  renderEverything();
}

function renderMenu(menu, items) {
  const visibleItems = filterItems(items);
  const cartSignature = cart.map((line) => `${line.key}:${line.quantity}`).join("|");
  const signature = JSON.stringify({ menu, items: visibleItems, search: currentSearch, cartSignature });
  if (signature === lastRenderSignature) {
    return;
  }

  lastRenderSignature = signature;
  setThemeColor(menu.theme_color);
  setCoverImage(menu, items);
  qs("#restaurantName").textContent = menu.name || "Cardapio digital";
  qs(".restaurant-header")?.classList.toggle("has-logo", Boolean(displayImageUrl(menu.logo_url)));
  const logo = qs("#restaurantLogo");
  const fallback = qs("#brandFallback");
  if (displayImageUrl(menu.logo_url)) {
    logo.src = displayImageUrl(menu.logo_url);
    logo.alt = menu.name || "Logo do restaurante";
    logo.hidden = false;
    fallback.hidden = true;
  } else {
    logo.hidden = true;
    fallback.hidden = true;
  }

  qs("#restaurantInfo").textContent = formatInfo(menu) || "Veja os produtos e chame a equipe para pedir.";
  const isOpen = menu.store_open !== false;
  const openText = qs("#storeOpenText");
  if (openText) {
    openText.innerHTML = `${iconSvg("store", "status-icon")}<b class="dot"></b> ${isOpen ? "Cardapio disponivel" : "Loja pausada"}`;
    openText.classList.toggle("closed", !isOpen);
  }
  const waitText = qs("#waitTimeText");
  if (waitText) waitText.textContent = "Atendimento da loja";
  renderActions(menu);
  renderCouponCard(menu);
  renderFeatured(items);

  const groups = new Map();
  for (const item of visibleItems) {
    const category = item.category || "Cardapio";
    if (!groups.has(category)) groups.set(category, []);
    groups.get(category).push(item);
  }

  const nav = qs("#categoryNav");
  nav.innerHTML = "";
  let firstCategory = true;
  for (const [category, categoryItems] of groups) {
    const id = categoryId(category);
    const link = document.createElement("a");
    link.href = `#${id}`;
    link.className = firstCategory ? "active" : "";
    link.innerHTML = `${iconSvg(categoryIcon(category), "category-icon")}<span>${escapeHtml(category)}</span><small>${categoryItems.length}</small>`;
    nav.appendChild(link);
    firstCategory = false;
  }

  const list = qs("#menuList");
  list.innerHTML = "";
  if (!visibleItems.length) {
    list.innerHTML = `<div class="empty-state">${currentSearch ? "Nenhum produto encontrado nessa busca." : "Cardapio publicado, mas ainda sem produtos ativos."}</div>`;
    setStatus(currentSearch ? `0 resultado para "${currentSearch}".` : "Cardapio publicado, mas ainda sem produtos ativos.", Boolean(!items.length));
    return;
  }

  for (const [category, categoryItems] of groups) {
    const id = categoryId(category);
    const section = document.createElement("section");
    section.className = "category";
    section.id = id;
    section.innerHTML = `
      <div class="category-head">
        <h2>${escapeHtml(category)}</h2>
        <span class="category-count">${categoryItems.length} item(ns)</span>
      </div>
      <div class="items"></div>
    `;
    const host = section.querySelector(".items");
    for (const item of categoryItems) {
      const index = currentItems.indexOf(item);
      const key = itemKey(item, index);
      const available = item.is_in_stock !== false;
      const quantity = cartQuantity(key);
      const stockText = Number.isFinite(Number(item.stock_quantity))
        ? `Estoque ${Number(item.stock_quantity).toLocaleString("pt-BR", { maximumFractionDigits: 3 })}`
        : "";
      const imageUrl = displayImageUrl(item.image_url);
      const card = document.createElement("article");
      card.className = `item${available ? "" : " unavailable"}${imageUrl ? "" : " no-image"}`;
      card.innerHTML = `
        <div class="item-media">
          ${imageUrl
            ? `
            <img src="${escapeHtml(imageUrl)}" alt="${escapeHtml(item.name)}" loading="lazy">
          `
            : `<span class="item-placeholder">${iconSvg(categoryIcon(item.category), "placeholder-icon")}<b>${escapeHtml(firstLetters(item.name))}</b></span>`}
        </div>
        <div class="item-body">
          <div class="item-top">
            <h3>${escapeHtml(item.name)}</h3>
            <strong class="price">${money(item.price)}</strong>
          </div>
          ${item.description ? `<p>${escapeHtml(item.description)}</p>` : ""}
          <div class="item-bottom">
            <div class="item-meta">
              ${item.code ? `<span class="code">Cod. ${escapeHtml(item.code)}</span>` : ""}
              <span class="stock ${available ? "ok" : "out"}">${available ? "Disponivel" : "Indisponivel"}${stockText ? ` - ${escapeHtml(stockText)}` : ""}</span>
            </div>
            <button type="button" class="item-action${quantity ? " in-cart" : ""}" data-add-item="${escapeHtml(key)}" ${available ? "" : "disabled"}>
              ${available
                ? `${iconSvg(quantity ? "check" : "plus")}<span>${quantity ? escapeHtml(quantity) : "Adicionar"}</span>`
                : "Indisponivel"}
            </button>
          </div>
        </div>
      `;
      host.appendChild(card);
    }
    list.appendChild(section);
  }

  setStatus(currentSearch
    ? `${visibleItems.length} resultado(s) para "${currentSearch}".`
    : `${visibleItems.length} item(ns) no cardapio.`);
}

function renderCartBar() {
  const bar = qs("#cartBar");
  const count = cartCount();
  bar.hidden = count === 0;
  qs("#cartBarTotal").textContent = money(cartTotal());
  qs("#cartBarCount").textContent = `${count} ${count === 1 ? "item" : "itens"}${currentMode ? ` - ${modeLabel()}` : ""}`;
}

function renderCartPanel() {
  const host = qs("#cartItems");
  if (!cart.length) {
    host.innerHTML = `<div class="empty-state">Seu pedido ainda esta vazio.</div>`;
  } else {
    host.innerHTML = cart.map((line) => `
      <div class="cart-line">
        <div class="cart-line-top">
          <strong>${escapeHtml(line.name)}</strong>
          <span class="cart-line-price">${money(line.price * line.quantity)}</span>
        </div>
        <div class="quantity-row">
          <button type="button" class="quantity-button" data-cart-dec="${escapeHtml(line.key)}">-</button>
          <span class="quantity-value">${line.quantity}</span>
          <button type="button" class="quantity-button" data-cart-inc="${escapeHtml(line.key)}">+</button>
          <span class="code">${money(line.price)} cada</span>
        </div>
      </div>
    `).join("");
  }

  qs("#orderNotes").value = orderNotes;
  qs("#sendOrder").textContent = "Realizar pedido";
  qs("#checkoutSummary").innerHTML = `
    <span>Tipo: ${escapeHtml(modeLabel())}</span>
    ${customerSummaryLines().map((line) => `<span>${escapeHtml(line)}</span>`).join("")}
    <strong>Total: ${money(cartTotal())}</strong>
  `;
}

function customerSummaryLines() {
  const lines = [];
  if (customerFields.name) lines.push(`Nome: ${customerFields.name}`);
  if (currentMode === "delivery" && customerFields.address) lines.push(`Endereco: ${customerFields.address}`);
  if (currentMode === "delivery" && customerFields.reference) lines.push(`Referencia: ${customerFields.reference}`);
  if (currentMode === "pickup" && customerFields.time) lines.push(`Horario: ${customerFields.time}`);
  if (currentMode === "table" && customerFields.table) lines.push(`Mesa/comanda: ${customerFields.table}`);
  if (customerFields.phone) lines.push(`Celular: ${customerFields.phone}`);
  return lines;
}

function buildOrderPayload() {
  return {
    slug: currentMenu?.slug || currentSlug(),
    menuId: currentMenu?.id || "",
    orderType: currentMode,
    customer: {
      name: customerFields.name || "",
      phone: customerFields.phone || "",
      address: customerFields.address || "",
      district: customerFields.district || "",
      reference: customerFields.reference || "",
      table: customerFields.table || "",
      time: customerFields.time || ""
    },
    notes: orderNotes.trim(),
    subtotal: cartTotal(),
    total: cartTotal(),
    localId: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
    localWhen: new Date().toISOString(),
    items: cart.map((line) => ({
      code: line.code || "",
      name: line.name || "Produto",
      category: line.category || "",
      quantity: line.quantity,
      price: Number(line.price || 0)
    }))
  };
}

function saveOrderHistory(remoteOrder = {}) {
  const remoteMode = publicOrderTypeToMode(remoteOrder.orderType);
  const order = {
    id: String(remoteOrder.orderId || Date.now()),
    remoteOrderId: remoteOrder.orderId || "",
    createdAt: new Date().toISOString(),
    updatedAt: remoteOrder.updatedAt || remoteOrder.createdAt || new Date().toISOString(),
    status: remoteOrder.status || "NOVO",
    orderType: remoteOrder.orderType || "",
    pdvOrderId: remoteOrder.pdvOrderId || "",
    mode: remoteMode || currentMode,
    customer: { ...customerFields },
    notes: orderNotes,
    total: cartTotal(),
    items: cart.map((line) => ({ ...line }))
  };
  orderHistory = [order, ...orderHistory].slice(0, 30);
  writeStorage("balcao.cardapio.history", orderHistory);
  return order;
}

function publicOrderIdsInHistory() {
  return orderHistory
    .map((order) => String(order.remoteOrderId || ""))
    .filter((id) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id))
    .slice(0, 20);
}

async function refreshOrderStatuses() {
  if (orderStatusPollRunning) return;
  const orderIds = publicOrderIdsInHistory();
  if (!orderIds.length) return;

  orderStatusPollRunning = true;
  try {
    const result = await licensePost("/menu/order/status", {
      slug: currentMenu?.slug || currentSlug(),
      orderIds
    });
    const rows = Array.isArray(result.orders) ? result.orders : [];
    let changed = false;
    for (const row of rows) {
      const order = orderHistory.find((item) => item.remoteOrderId === row.id);
      if (!order) continue;
      const nextStatus = row.status || order.status;
      const nextMode = publicOrderTypeToMode(row.orderType) || order.mode;
      if (order.status !== nextStatus || order.updatedAt !== row.updatedAt || order.pdvOrderId !== row.pdvOrderId || order.mode !== nextMode) {
        order.status = nextStatus;
        order.updatedAt = row.updatedAt || order.updatedAt;
        order.pdvOrderId = row.pdvOrderId || order.pdvOrderId || "";
        order.orderType = row.orderType || order.orderType || "";
        order.mode = nextMode;
        changed = true;
      }
    }

    if (changed) {
      writeStorage("balcao.cardapio.history", orderHistory);
      renderHistory();
      renderDiscounts();
      renderProfile();
    }
  } catch (error) {
    console.debug("Nao foi possivel atualizar status dos pedidos.", error);
  } finally {
    orderStatusPollRunning = false;
  }
}

function startOrderStatusPolling() {
  if (orderStatusPollTimer) {
    window.clearInterval(orderStatusPollTimer);
  }
  refreshOrderStatuses();
  orderStatusPollTimer = window.setInterval(refreshOrderStatuses, 15000);
}

function clearCurrentOrder() {
  cart = [];
  orderNotes = "";
  writeStorage("balcao.cardapio.cart", cart);
  writeStorage("balcao.cardapio.notes", orderNotes);
}

function validateOrder() {
  if (!currentMode) return "Escolha entrega, retirada ou mesa/local antes de realizar o pedido.";
  if (!cart.length) return "Adicione pelo menos um item ao pedido.";
  if (currentMode === "delivery" && !String(customerFields.address || "").trim()) return "Informe o endereco de entrega.";
  if (currentMode === "pickup" && !String(customerFields.name || "").trim()) return "Informe o nome para retirada.";
  if (currentMode === "table" && !String(customerFields.table || "").trim()) return "Informe a mesa ou comanda.";
  return "";
}

async function sendOrder() {
  const error = validateOrder();
  if (error) {
    setStatus(error, true);
    qs("#checkoutOverlay").hidden = false;
    renderCartPanel();
    return;
  }

  const button = qs("#sendOrder");
  const previousText = button.textContent;
  button.disabled = true;
  button.textContent = "Enviando...";
  try {
    const result = await licensePost("/menu/order", buildOrderPayload());
    saveOrderHistory({ orderId: result.orderId, status: result.status || "NOVO", createdAt: result.createdAt });
    clearCurrentOrder();
    setStatus("Pedido realizado. Ele ja caiu no PDV do restaurante.");
    qs("#checkoutOverlay").hidden = true;
    renderEverything();
    setPage("history");
    refreshOrderStatuses();
    return;
  } catch (sendError) {
    setStatus(sendError.message || "Nao foi possivel enviar o pedido ao PDV agora.", true);
  } finally {
    button.disabled = false;
    button.textContent = previousText || "Realizar pedido";
  }
  renderEverything();
}

function renderHistory() {
  const host = qs("#historyList");
  if (!host) return;
  if (!orderHistory.length) {
    host.innerHTML = `
      <div class="history-card">
        <h3>Nenhum pedido ainda</h3>
        <p class="muted">Quando o cliente enviar um pedido, ele aparece aqui para repetir depois.</p>
        <button type="button" class="send-order" data-nav-page="home">Fazer pedido</button>
      </div>
    `;
    return;
  }

  host.innerHTML = orderHistory.slice(0, 6).map((order) => {
    const when = new Date(order.createdAt);
    const statusInfo = orderStatusInfo(order);
    const displayId = shortOrderId(order.remoteOrderId || order.id);
    const title = Number.isNaN(when.getTime())
      ? "Pedido recente"
      : `Pedido em ${when.toLocaleDateString("pt-BR")} as ${when.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}`;
    const firstItem = order.items?.[0];
    const itemText = order.items?.length > 1
      ? `${firstItem?.quantity || 1}x ${firstItem?.name || "Produto"} + ${order.items.length - 1} item(ns)`
      : `${firstItem?.quantity || 1}x ${firstItem?.name || "Produto"}`;
    return `
      <article class="history-card">
        <div class="history-head">
          <div>
            <h3>${escapeHtml(title)}</h3>
            ${displayId ? `<p class="muted">Pedido ${escapeHtml(displayId)}</p>` : ""}
          </div>
          <span class="order-status ${escapeHtml(statusInfo.tone)}">${escapeHtml(statusInfo.label)}</span>
        </div>
        <p class="order-status-detail">${escapeHtml(statusInfo.detail)}</p>
        ${renderOrderProgress(order, statusInfo)}
        <p class="muted">${escapeHtml(modeLabel(order.mode))}${order.customer?.address ? ` - ${escapeHtml(order.customer.address)}` : ""}</p>
        <div class="history-total">
          <span>${escapeHtml(itemText)}</span>
          <strong>${money(order.total)}</strong>
        </div>
        <button type="button" class="secondary-action" data-repeat-order="${escapeHtml(order.id)}">Repetir pedido</button>
      </article>
    `;
  }).join("");
}

function renderDiscounts() {
  const loyalty = loyaltyConfig();
  const loyaltyCard = qs(".loyalty-card");
  if (loyaltyCard) loyaltyCard.hidden = !loyalty;
  const progressText = qs("#loyaltyProgressText");
  const progressBar = qs("#loyaltyProgress");
  const points = loyalty ? Math.min(loyalty.goal, orderHistory.length) : 0;
  if (progressText) progressText.textContent = loyalty ? `${points} / ${loyalty.goal}` : "0 / 0";
  if (progressBar) progressBar.style.width = loyalty ? `${Math.min(100, (points / loyalty.goal) * 100)}%` : "0%";

  const host = qs("#discountList");
  if (!host) return;
  const discount = activeDiscount();
  const cards = [];
  if (discount) {
    cards.push(`
      <article class="discount-card">
        <span class="coupon-icon">%</span>
        <div>
          <strong>Cupom ${escapeHtml(discount.code)}</strong>
          <p class="muted">${escapeHtml(discount.description)}</p>
        </div>
        <span class="discount-value">${escapeHtml(money(discount.amount))}</span>
      </article>
    `);
  }

  if (loyalty) {
    cards.push(`
      <article class="discount-card">
        <span class="coupon-icon">*</span>
        <div>
          <strong>Fidelidade</strong>
          <p class="muted">A cada pedido de no minimo ${escapeHtml(money(loyalty.minimum))}, voce ganha um ponto.</p>
        </div>
        <span class="discount-value">${points}/${loyalty.goal}</span>
      </article>
    `);
  }

  host.innerHTML = cards.length ? cards.join("") : `
    <article class="discount-empty-card">
      <span class="coupon-icon">%</span>
      <div>
        <strong>Sem ofertas no momento</strong>
        <p class="muted">Quando a loja criar um cupom ou fidelidade no sistema, a oferta aparece aqui automaticamente.</p>
      </div>
    </article>
  `;
}

function renderProfile() {
  const host = qs("#profilePanel");
  if (!host) return;
  const loyalty = loyaltyConfig();
  const points = loyalty ? Math.min(loyalty.goal, orderHistory.length) : 0;
  host.innerHTML = `
    <article class="profile-card">
      <h3>Meus pontos</h3>
      <p>${loyalty ? `A cada pedido de, no minimo, ${money(loyalty.minimum)}, voce ganha um ponto.` : "Fidelidade indisponivel no momento."}</p>
      <div class="progress"><span style="width:${loyalty ? Math.min(100, (points / loyalty.goal) * 100) : 0}%"></span></div>
    </article>
    <article class="profile-card">
      <h3>Dados do cliente</h3>
      <div class="profile-grid">
        <label class="field"><span>Nome</span><input data-profile-field="name" value="${escapeHtml(customerFields.name || "")}" placeholder="Seu nome"></label>
        <label class="field"><span>Telefone</span><input data-profile-field="phone" value="${escapeHtml(customerFields.phone || "")}" placeholder="DDD + numero"></label>
      </div>
    </article>
    <article class="profile-card">
      <h3>Meus enderecos</h3>
      <label class="field"><span>Endereco principal</span><input data-profile-field="address" value="${escapeHtml(customerFields.address || "")}" placeholder="Rua, numero, bairro"></label>
      <label class="field"><span>Referencia</span><input data-profile-field="reference" value="${escapeHtml(customerFields.reference || "")}" placeholder="Opcional"></label>
    </article>
  `;
}

function repeatOrder(orderId) {
  const order = orderHistory.find((item) => item.id === orderId);
  if (!order) return;
  currentMode = order.mode || currentMode || "delivery";
  customerFields = { ...customerFields, ...(order.customer || {}) };
  cart = (order.items || []).map((line) => ({ ...line }));
  orderNotes = order.notes || "";
  writeStorage("balcao.cardapio.mode", currentMode);
  saveCart();
  setPage("home");
  renderEverything();
}

function renderEverything() {
  renderOrderPanel();
  if (currentMenu) renderMenu(currentMenu, currentItems);
  renderCartBar();
  renderCartPanel();
  renderHistory();
  renderDiscounts();
  renderProfile();
  updateDiscountNavigation();
  setPage(currentPage, false);
}

async function boot() {
  const slug = currentSlug();
  try {
    const menus = await supabaseGet(`/bv_public_menus?slug=eq.${encodeURIComponent(slug)}&is_published=eq.true&select=id,slug,name,description,phone,address,city,state,logo_url,cover_image_url,theme_color,store_open,wait_min_minutes,wait_max_minutes,discount_enabled,discount_code,discount_amount,discount_description,loyalty_enabled,loyalty_goal,loyalty_minimum_order,updated_at&limit=1`);
    const menu = menus[0];
    if (!menu) {
      setStatus("Cardapio nao encontrado ou ainda nao publicado.", true);
      qs("#restaurantName").textContent = "Cardapio indisponivel";
      return;
    }

    const items = await supabaseGet(`/bv_public_menu_items?menu_id=eq.${encodeURIComponent(menu.id)}&is_active=eq.true&select=code,name,description,category,price,stock_quantity,is_in_stock,image_url,sort_order&order=category.asc,sort_order.asc,name.asc`);
    currentMenu = menu;
    currentItems = items;
    renderEverything();
    startOrderStatusPolling();
  } catch (error) {
    setStatus(error.message || "Nao foi possivel carregar o cardapio.", true);
    qs("#restaurantName").textContent = "Cardapio indisponivel";
  }
}

document.addEventListener("click", (event) => {
  const navButton = event.target.closest("[data-nav-page]");
  if (navButton) {
    setPage(navButton.dataset.navPage || "home");
    return;
  }

  const repeatButton = event.target.closest("[data-repeat-order]");
  if (repeatButton) {
    repeatOrder(repeatButton.dataset.repeatOrder);
    return;
  }

  const modeButton = event.target.closest("[data-order-mode]");
  if (modeButton) {
    currentMode = modeButton.dataset.orderMode || "";
    writeStorage("balcao.cardapio.mode", currentMode);
    renderEverything();
    return;
  }

  const addButton = event.target.closest("[data-add-item]");
  if (addButton) {
    addItemByKey(addButton.dataset.addItem);
    return;
  }

  const incButton = event.target.closest("[data-cart-inc]");
  if (incButton) {
    changeCartQuantity(incButton.dataset.cartInc, 1);
    return;
  }

  const decButton = event.target.closest("[data-cart-dec]");
  if (decButton) {
    changeCartQuantity(decButton.dataset.cartDec, -1);
    return;
  }

  if (event.target.closest("#openCart")) {
    qs("#checkoutOverlay").hidden = false;
    renderCartPanel();
    return;
  }

  if (event.target.closest("[data-close-cart]")) {
    qs("#checkoutOverlay").hidden = true;
    return;
  }

  if (event.target.closest("[data-open-address]")) {
    const infoOverlay = qs("#infoOverlay");
    if (infoOverlay) infoOverlay.hidden = true;
    openAddressModal();
    return;
  }

  if (event.target.closest("[data-close-address]") || event.target.id === "addressOverlay") {
    qs("#addressOverlay").hidden = true;
    return;
  }

  if (event.target.closest("[data-open-info]")) {
    openInfoModal();
    return;
  }

  if (event.target.closest("[data-close-info]") || event.target.id === "infoOverlay") {
    qs("#infoOverlay").hidden = true;
    return;
  }

  if (event.target.closest("#sendOrder")) {
    sendOrder();
  }
});

document.addEventListener("input", (event) => {
  if (event.target.matches("#menuSearch")) {
    currentSearch = event.target.value || "";
    renderEverything();
    return;
  }

  if (event.target.matches("[data-order-field]")) {
    customerFields[event.target.dataset.orderField] = event.target.value;
    writeStorage("balcao.cardapio.customer", customerFields);
    renderCartPanel();
    return;
  }

  if (event.target.matches("[data-profile-field]")) {
    customerFields[event.target.dataset.profileField] = event.target.value;
    writeStorage("balcao.cardapio.customer", customerFields);
    renderOrderPanel();
    return;
  }

  if (event.target.matches("#orderNotes")) {
    orderNotes = event.target.value;
    writeStorage("balcao.cardapio.notes", orderNotes);
  }
});

renderOrderPanel();
boot();
setInterval(() => {
  if (document.visibilityState === "visible") {
    boot();
  }
}, 30000);
