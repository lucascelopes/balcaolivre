const config = window.BALCAO_CARDAPIO_CONFIG || {};
const qs = (selector) => document.querySelector(selector);
let lastRenderSignature = "";

function currentSlug() {
  const url = new URL(window.location.href);
  const fromQuery = url.searchParams.get("loja") || url.searchParams.get("menu");
  if (fromQuery) return fromQuery.trim();

  const cleanPath = url.pathname
    .replace(/^\/cardapio\/?/i, "")
    .split("/")
    .filter(Boolean);
  return cleanPath[0] || "demo";
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

function setStatus(message, isError = false) {
  const status = qs("#status");
  status.textContent = message;
  status.classList.toggle("error", isError);
}

function renderMenu(menu, items) {
  const signature = JSON.stringify({ menu, items });
  if (signature === lastRenderSignature) {
    return;
  }

  lastRenderSignature = signature;
  const color = menu.theme_color || "#0f766e";
  document.documentElement.style.setProperty("--accent", color);
  qs("#restaurantName").textContent = menu.name || "Cardapio digital";
  const logo = qs("#restaurantLogo");
  const fallback = qs("#brandFallback");
  if (menu.logo_url) {
    logo.src = menu.logo_url;
    logo.alt = menu.name || "Logo do restaurante";
    logo.hidden = false;
    fallback.hidden = true;
  } else {
    logo.hidden = true;
    fallback.hidden = false;
  }

  const info = [menu.description, menu.phone, menu.address, menu.city, menu.state]
    .filter(Boolean)
    .join("  |  ");
  qs("#restaurantInfo").textContent = info || "Veja os produtos e chame a equipe para pedir.";

  const groups = new Map();
  for (const item of items) {
    const category = item.category || "Cardapio";
    if (!groups.has(category)) groups.set(category, []);
    groups.get(category).push(item);
  }

  const nav = qs("#categoryNav");
  nav.innerHTML = "";
  for (const category of groups.keys()) {
    const id = `cat-${category.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]+/g, "-")}`;
    const link = document.createElement("a");
    link.href = `#${id}`;
    link.textContent = category;
    nav.appendChild(link);
  }

  const list = qs("#menuList");
  list.innerHTML = "";
  for (const [category, categoryItems] of groups) {
    const id = `cat-${category.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]+/g, "-")}`;
    const section = document.createElement("section");
    section.className = "category";
    section.id = id;
    section.innerHTML = `<h2>${escapeHtml(category)}</h2><div class="items"></div>`;
    const host = section.querySelector(".items");
    for (const item of categoryItems) {
      const available = item.is_in_stock !== false;
      const stockText = Number.isFinite(Number(item.stock_quantity))
        ? `Estoque ${Number(item.stock_quantity).toLocaleString("pt-BR", { maximumFractionDigits: 3 })}`
        : "";
      const card = document.createElement("article");
      card.className = `item${available ? "" : " unavailable"}`;
      card.innerHTML = `
        <div>
          <h3>${escapeHtml(item.name)}</h3>
          ${item.code ? `<span class="code">${escapeHtml(item.code)}</span>` : ""}
        </div>
        <strong class="price">${money(item.price)}</strong>
        ${item.description ? `<p>${escapeHtml(item.description)}</p>` : ""}
        <div class="stock ${available ? "ok" : "out"}">${available ? "Disponivel" : "Indisponivel"}${stockText ? ` | ${escapeHtml(stockText)}` : ""}</div>
      `;
      host.appendChild(card);
    }
    list.appendChild(section);
  }

  setStatus(items.length
    ? `${items.length} produto(s) disponiveis.`
    : "Cardapio publicado, mas ainda sem produtos ativos.");
}

async function boot() {
  const slug = currentSlug();
  try {
    const menus = await supabaseGet(`/bv_public_menus?slug=eq.${encodeURIComponent(slug)}&is_published=eq.true&select=id,slug,name,description,phone,address,city,state,logo_url,theme_color,updated_at&limit=1`);
    const menu = menus[0];
    if (!menu) {
      setStatus("Cardapio nao encontrado ou ainda nao publicado.", true);
      qs("#restaurantName").textContent = "Cardapio indisponivel";
      return;
    }

    const items = await supabaseGet(`/bv_public_menu_items?menu_id=eq.${encodeURIComponent(menu.id)}&is_active=eq.true&select=code,name,description,category,price,stock_quantity,is_in_stock,image_url,sort_order&order=category.asc,sort_order.asc,name.asc`);
    renderMenu(menu, items);
  } catch (error) {
    setStatus(error.message || "Nao foi possivel carregar o cardapio.", true);
    qs("#restaurantName").textContent = "Cardapio indisponivel";
  }
}

boot();
setInterval(() => {
  if (document.visibilityState === "visible") {
    boot();
  }
}, 30000);
