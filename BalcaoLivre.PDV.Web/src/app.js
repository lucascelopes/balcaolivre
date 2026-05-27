import {
  count,
  getAll,
  getOne,
  markEventsSynced,
  pendingEvents,
  put,
  saveSaleBundle,
  seedIfEmpty
} from "./db.js?v=10";
import { categories, initialProducts, initialSettings, paymentMethods } from "./data.js?v=10";
import { createSupabaseAuth, signInSupabase } from "./supabaseAuth.js?v=10";

const boardCount = 24;
const initialUsers = [
  { id: "user_master", number: "1", name: "ADMINISTRADOR", role: "GERENTE", pin: "1", canCash: true, canDiscount: true, canProducts: true, active: true },
  { id: "user_cashier", number: "2", name: "CAIXA 2", role: "CAIXA", pin: "2", canCash: true, canDiscount: false, canProducts: false, active: true }
];
const state = {
  mode: "tables",
  activeBoard: "000001",
  selectedCategory: "BEBIDAS",
  selectedProductCode: "000001",
  selectedPayment: "Dinheiro",
  products: [],
  customers: [],
  users: [],
  cashMovements: [],
  settings: initialSettings,
  auth: { enabled: true, configured: false, session: null, user: null },
  tickets: loadTickets(),
  ticketMeta: loadTicketMeta(),
  lastReceipt: null,
  dialogCleanup: null
};

const qs = (selector) => document.querySelector(selector);
const qsa = (selector) => Array.from(document.querySelectorAll(selector));

function money(value) {
  return Number(value || 0).toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL"
  });
}

function inputMoney(value) {
  return Number(value || 0).toLocaleString("pt-BR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

function parseMoney(value) {
  const normalized = String(value || "0").replace(/\./g, "").replace(",", ".");
  const parsed = Number.parseFloat(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
}

function uuid(prefix) {
  return `${prefix}_${crypto.randomUUID()}`;
}

function nowIso() {
  return new Date().toISOString();
}

function loadTickets() {
  return loadJson("blpdv_open_tickets", {});
}

function saveTickets() {
  localStorage.setItem("blpdv_open_tickets", JSON.stringify(state.tickets));
}

function loadTicketMeta() {
  return loadJson("blpdv_ticket_meta", {});
}

function saveTicketMeta() {
  localStorage.setItem("blpdv_ticket_meta", JSON.stringify(state.ticketMeta));
}

function loadJson(key, fallback) {
  try {
    return JSON.parse(localStorage.getItem(key) || JSON.stringify(fallback));
  } catch {
    return fallback;
  }
}

function normalizeDemoSettings(settings) {
  const next = { ...settings };
  if (!next.businessName || next.businessName === "COXITRUCK") {
    next.businessName = initialSettings.businessName;
  }
  if (!next.legalName || next.legalName === "COXITRUCK") {
    next.legalName = initialSettings.legalName;
  }
  if (next.ownerName === "Lucas") {
    next.ownerName = initialSettings.ownerName;
  }
  if (next.cnpj === "50.597.666/0001-47") {
    next.cnpj = initialSettings.cnpj;
  }
  if (next.phone === "(27) 98347-3241") {
    next.phone = initialSettings.phone;
  }
  return next;
}

function currentTicket() {
  if (!state.tickets[state.activeBoard]) {
    state.tickets[state.activeBoard] = [];
  }
  return state.tickets[state.activeBoard];
}

function currentMeta(board = state.activeBoard) {
  if (!state.ticketMeta[board]) {
    state.ticketMeta[board] = {
      coverValue: "0,00",
      servicePercent: "10",
      discountPercent: 0,
      customerName: "",
      people: 1
    };
  }
  return state.ticketMeta[board];
}

function currentSubtotal(ticket = currentTicket()) {
  return ticket.reduce((sum, item) => sum + item.qty * item.price, 0);
}

function currentTotal() {
  const meta = currentMeta();
  const subtotal = currentSubtotal();
  const cover = parseMoney(qs("#coverValue")?.value || meta.coverValue || "0,00");
  const servicePercent = Number.parseFloat(String(qs("#servicePercent")?.value || meta.servicePercent || "0").replace(",", ".")) || 0;
  const discountPercent = Number(meta.discountPercent || 0);
  const service = subtotal * servicePercent / 100;
  const discount = subtotal * discountPercent / 100;
  return Math.max(0, subtotal + cover + service - discount);
}

function boardTotal(board) {
  return currentSubtotal(state.tickets[board] || []);
}

function openBoards() {
  return Object.entries(state.tickets)
    .filter(([, rows]) => rows.length > 0)
    .map(([board, rows]) => ({ board, rows, total: currentSubtotal(rows) }));
}

function ticketLineCount(board = state.activeBoard) {
  return (state.tickets[board] || []).reduce((sum, item) => sum + item.qty, 0);
}

function currentTicketLegacyTotal() {
  return currentTicket().reduce((sum, item) => sum + item.qty * item.price, 0);
}

function currentQty() {
  return Math.max(1, Number.parseInt(qs("#productQty").value || "1", 10) || 1);
}

function productByCode(code) {
  const normalized = String(code || "").trim().padStart(6, "0");
  return state.products.find((product) => product.code === normalized || product.code.includes(String(code).trim()));
}

function activeProduct() {
  return productByCode(qs("#productCode").value) || state.products.find((product) => product.code === state.selectedProductCode);
}

function showToast(message) {
  const toast = qs("#toast");
  toast.textContent = message;
  toast.hidden = false;
  clearTimeout(showToast.timer);
  showToast.timer = setTimeout(() => {
    toast.hidden = true;
  }, 3200);
}

async function boot() {
  await seedIfEmpty(initialProducts, initialSettings);
  if (await count("users") === 0) {
    for (const user of initialUsers) await put("users", user);
  }
  state.products = await getAll("products");
  state.customers = await getAll("customers");
  state.users = await getAll("users");
  state.cashMovements = await getAll("cash_movements");
  const savedSettings = await getOne("terminal_settings", "main");
  state.settings = normalizeDemoSettings({ ...initialSettings, ...(savedSettings || {}) });
  if (!state.settings.syncEndpoint) {
    state.settings.syncEndpoint = initialSettings.syncEndpoint;
  }
  await put("terminal_settings", state.settings);
  state.auth = await createSupabaseAuth(state.settings);
  if (state.auth.configured && (!state.settings.supabaseUrl || !state.settings.supabaseAnonKey)) {
    state.settings.supabaseUrl = state.auth.url;
    state.settings.supabaseAnonKey = state.auth.anonKey;
    await put("terminal_settings", state.settings);
  }

  if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register(new URL("../sw.js", import.meta.url)).catch(() => {});
  }

  bindEvents();
  renderAll();
  updateOnlineState();
  renderLoginState();
  await refreshLocalCounters();

  window.addEventListener("online", () => {
    updateOnlineState();
    syncNow();
  });
  window.addEventListener("offline", updateOnlineState);
}

function bindEvents() {
  qs("#addLineButton").addEventListener("click", addCurrentProduct);
  qs("#clearSaleButton").addEventListener("click", clearCurrentTicket);
  qs("#finishSaleButton").addEventListener("click", finishSale);
  qs("#syncNowButton").addEventListener("click", syncNow);
  qs("#settingsButton").addEventListener("click", openSettings);
  qs("#topConfigButton").addEventListener("click", openSettings);
  qs("#enterPdvButton").addEventListener("click", enterPdv);
  qs("#closeLoginButton").addEventListener("click", closeLogin);
  qs("#saveSettingsButton").addEventListener("click", saveSettings);
  qs("#testNotificationButton").addEventListener("click", testNotification);
  qs("#checkUpdateButton").addEventListener("click", checkForUpdates);
  qs("#amountReceived").addEventListener("input", renderTotals);
  qs("#coverValue").addEventListener("input", updateTicketMeta);
  qs("#servicePercent").addEventListener("input", updateTicketMeta);
  qs("#productCode").addEventListener("input", updateProductPrice);
  qs("#productCode").addEventListener("keydown", (event) => {
    if (event.key === "Enter") addCurrentProduct();
  });

  qsa(".mode-tabs button").forEach((button) => {
    button.addEventListener("click", () => {
      state.mode = button.dataset.mode;
      if (state.mode === "counter") state.activeBoard = "BALCAO";
      if (state.mode === "delivery") state.activeBoard = "DELIVERY";
      if (state.mode === "tables" && !/^\d/.test(state.activeBoard)) state.activeBoard = "000001";
      renderAll();
    });
  });

  qsa(".ribbon-btn").forEach((button) => {
    button.addEventListener("click", () => handleRibbon(button.dataset.action));
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "F2") {
      event.preventDefault();
      addCurrentProduct();
    }
    if (event.key === "F9") {
      event.preventDefault();
      syncNow();
    }
  });
}

async function handleRibbon(action) {
  const handlers = {
    search: openSearchDialog,
    transfer: openTransferDialog,
    discount: openDiscountDialog,
    customer: openCustomerDialog,
    reopen: openReopenDialog,
    team: openTeamDialog,
    products: openProductDialog,
    cash: openCashDialog,
    "cash-toggle": toggleCash,
    delivery: newDelivery,
    stock: openStockDialog
  };

  if (handlers[action]) {
    await handlers[action]();
    return;
  }

  showToast("Acao sem botao configurado.");
}

function renderLoginState() {
  const overlay = qs("#loginOverlay");
  const help = qs("#loginHelp");
  const emailInput = qs("#loginEmail");
  const message = qs("#loginMessage");
  if (state.auth.configured) {
    help.textContent = "Entre com o email e senha da conta para liberar o PDV Online.";
    emailInput.value = state.auth.user?.email || emailInput.value;
    if (state.auth.session) {
      overlay.classList.add("hidden");
    } else {
      overlay.classList.remove("hidden");
    }
    return;
  }

  help.textContent = "Modo local ativo.";
  message.textContent = "";
  overlay.classList.add("hidden");
  qs("#loginOperator").value = qs("#loginOperator").value || qs("#operatorNumber").value || "1";
  requestAnimationFrame(() => qs("#productCode")?.focus());
}

function closeLogin() {
  if (state.auth.configured && !state.auth.session) {
    qs("#loginMessage").textContent = "Login Supabase obrigatorio para abrir o PDV.";
    return;
  }
  qs("#loginOverlay").classList.add("hidden");
  qs("#productCode").focus();
}

async function enterPdv() {
  qs("#loginMessage").textContent = "";

  if (state.auth.configured) {
    const email = qs("#loginEmail").value.trim();
    const password = qs("#loginPassword").value;
    if (!email || !password) {
      qs("#loginMessage").textContent = "Informe email e a key/senha da conta.";
      return;
    }

    const result = await signInSupabase(state.auth, email, password);
    if (!result.ok) {
      qs("#loginMessage").textContent = result.message;
      return;
    }

    const operator = email.split("@")[0].slice(0, 18) || "supabase";
    qs("#operatorNumber").value = operator;
    closeLogin();
    showToast(`Login Supabase conectado: ${email}.`);
    return;
  }

  const operator = qs("#loginOperator").value.trim();
  if (operator) {
    qs("#operatorNumber").value = operator;
  }
  closeLogin();
}

function renderAll() {
  renderMode();
  renderBoards();
  renderCategories();
  renderProducts();
  renderPayments();
  renderTicket();
  renderTotals();
  renderSettingsLabels();
}

function renderMode() {
  qsa(".mode-tabs button").forEach((button) => {
    button.classList.toggle("active", button.dataset.mode === state.mode);
  });
  const meta = currentMeta();
  qs("#ticketTitle").textContent = state.mode === "counter"
    ? "Balcao"
    : state.mode === "delivery"
      ? "Delivery"
      : state.mode === "kitchen"
        ? "Cozinha"
        : "Comanda";
  qs("#boardNumber").value = state.activeBoard;
  qs("#coverValue").value = meta.coverValue || "0,00";
  qs("#servicePercent").value = meta.servicePercent || "10";
}

function renderBoards() {
  const grid = qs("#boardsGrid");
  if (state.mode === "kitchen") {
    const boards = openBoards();
    grid.innerHTML = boards.length
      ? boards.map(({ board, rows, total }) => `
        <button class="board-card occupied ${state.activeBoard === board ? "selected" : ""}" data-board="${board}" type="button">
          <span>${board}</span>
          <small>${rows.length} LINHA(S)<br>${money(total)}</small>
        </button>
      `).join("")
      : `<div class="empty-state">Nenhuma comanda aberta para cozinha.</div>`;

    qsa("[data-board]").forEach((button) => {
      button.addEventListener("click", () => {
        state.activeBoard = button.dataset.board;
        renderAll();
      });
    });
    return;
  }

  if (state.mode === "counter" || state.mode === "delivery") {
    grid.innerHTML = `
      <button class="board-card occupied selected" type="button">
        <span>${state.activeBoard}</span>
        <small>${currentTicket().length} ITEM(NS)</small>
      </button>
    `;
    return;
  }

  grid.innerHTML = Array.from({ length: boardCount }, (_, index) => {
    const number = String(index + 1).padStart(6, "0");
    const occupied = (state.tickets[number] || []).length > 0;
    return `
      <button class="board-card ${occupied ? "occupied" : ""} ${state.activeBoard === number ? "selected" : ""}" data-board="${number}" type="button">
        <span>${number}</span>
        <small>${occupied ? "OCUPADA" : "LIVRE"}</small>
      </button>
    `;
  }).join("");

  qsa("[data-board]").forEach((button) => {
    button.addEventListener("click", () => {
      state.activeBoard = button.dataset.board;
      renderAll();
    });
  });
}

function renderCategories() {
  if (state.mode === "kitchen") {
    qs("#categoryGrid").innerHTML = `
      <button class="category-card active" type="button">PREPARO</button>
      <button class="category-card" type="button">PRONTOS</button>
      <button class="category-card" type="button">ENTREGUES</button>
    `;
    return;
  }

  qs("#categoryGrid").innerHTML = categories.map((category) => `
    <button class="category-card ${state.selectedCategory === category ? "active" : ""}" data-category="${category}" type="button">
      ${category}
    </button>
  `).join("");

  qsa("[data-category]").forEach((button) => {
    button.addEventListener("click", () => {
      state.selectedCategory = button.dataset.category;
      renderCategories();
      renderProducts();
    });
  });
}

function renderProducts() {
  qs(".product-title").textContent = state.mode === "kitchen"
    ? "Pedidos da Cozinha"
    : "Lista de Produtos / Pesquisa";

  if (state.mode === "kitchen") {
    const boards = openBoards();
    qs("#productGrid").innerHTML = boards.length
      ? boards.map(({ board, rows, total }) => `
        <article class="kitchen-card">
          <header><strong>${board}</strong><b>${money(total)}</b></header>
          ${rows.map((item) => `<div><span>${item.qty}x</span><strong>${item.name}</strong><small>${item.note || "sem observacao"}</small></div>`).join("")}
          <button class="secondary" data-kitchen-done="${board}" type="button">Marcar pronto</button>
        </article>
      `).join("")
      : `<div class="empty-state">Nenhum pedido aberto.</div>`;

    qsa("[data-kitchen-done]").forEach((button) => {
      button.addEventListener("click", () => showToast(`Pedido ${button.dataset.kitchenDone} marcado como pronto na tela.`));
    });
    return;
  }

  const products = state.products
    .filter((product) => product.active !== false && product.category === state.selectedCategory)
    .sort((a, b) => a.code.localeCompare(b.code));

  qs("#productGrid").innerHTML = products.map((product) => `
    <button class="product-card ${state.selectedProductCode === product.code ? "active" : ""}" data-product="${product.code}" type="button">
      <span>${product.name}</span>
      <small>${product.code} | Est. ${product.stock}</small>
      <b>${money(product.price)}</b>
    </button>
  `).join("");

  qsa("[data-product]").forEach((button) => {
    button.addEventListener("click", () => {
      state.selectedProductCode = button.dataset.product;
      qs("#productCode").value = state.selectedProductCode;
      updateProductPrice();
      addCurrentProduct();
    });
  });
}

function renderPayments() {
  qs("#paymentMethods").innerHTML = paymentMethods.map((method) => `
    <button class="${state.selectedPayment === method ? "active" : ""}" data-payment="${method}" type="button">${method}</button>
  `).join("");

  qsa("[data-payment]").forEach((button) => {
    button.addEventListener("click", () => {
      state.selectedPayment = button.dataset.payment;
      if (state.selectedPayment !== "Dinheiro") {
        qs("#amountReceived").value = inputMoney(currentTotal());
      }
      renderPayments();
      renderTotals();
    });
  });
}

function renderTicket() {
  const rows = currentTicket();
  qs("#ticketRows").innerHTML = rows.length
    ? rows.map((item) => `
      <div class="ticket-row" data-line="${item.id}">
        <span>${item.code}</span>
        <strong>${item.name}</strong>
        <span>${item.qty}</span>
        <b>${money(item.qty * item.price)}</b>
      </div>
    `).join("")
    : `<div class="empty-state">Nenhum produto lancado.</div>`;

  qsa("[data-line]").forEach((row) => {
    row.addEventListener("dblclick", () => {
      state.tickets[state.activeBoard] = currentTicket().filter((item) => item.id !== row.dataset.line);
      saveTickets();
      renderAll();
    });
  });

  qs("#paymentRows").innerHTML = currentTotal() > 0
    ? `<strong>Saldo atual: ${money(currentTotal())}</strong><br><span>${ticketLineCount()} item(ns) na ${state.activeBoard}</span>`
    : "Sem pagamento informado.";
}

function renderTotals() {
  currentMeta().coverValue = qs("#coverValue").value;
  currentMeta().servicePercent = qs("#servicePercent").value;
  saveTicketMeta();
  const total = currentTotal();
  const received = parseMoney(qs("#amountReceived").value);
  qs("#saleTotal").textContent = money(total);
  qs("#changeValue").textContent = money(Math.max(0, received - total));
}

function updateTicketMeta() {
  const meta = currentMeta();
  meta.coverValue = qs("#coverValue").value;
  meta.servicePercent = qs("#servicePercent").value;
  saveTicketMeta();
  renderTotals();
}

function renderSettingsLabels() {
  const businessName = state.settings.businessName || initialSettings.businessName;
  const cnpj = state.settings.cnpj || initialSettings.cnpj;
  const phone = state.settings.phone || initialSettings.phone;
  qs("#brandNameText").textContent = businessName;
  qs("#brandDocText").textContent = cnpj ? `CNPJ ${cnpj}` : "Caixa demonstrativo";
  qs("#brandPhoneText").textContent = phone || "-";
  qs("#connectionText").textContent = navigator.onLine ? "Online" : "Offline";
}

function updateProductPrice() {
  const product = activeProduct();
  if (product) {
    state.selectedProductCode = product.code;
  }
}

function addCurrentProduct() {
  const product = activeProduct();
  if (!product) {
    showToast("Produto nao encontrado.");
    return;
  }

  const qty = currentQty();
  if (product.stock < qty) {
    showToast(`${product.name} sem estoque suficiente.`);
    return;
  }

  const note = "";
  const ticket = currentTicket();
  const existing = ticket.find((item) => item.code === product.code && item.note === note);
  if (existing) {
    existing.qty += qty;
  } else {
    ticket.push({
      id: uuid("line"),
      productId: product.id,
      code: product.code,
      name: product.name,
      qty,
      price: product.price,
      note
    });
  }

  state.selectedProductCode = product.code;
  qs("#productCode").value = "";
  qs("#productQty").value = "1";
  qs("#amountReceived").value = inputMoney(currentTotal());
  saveTickets();
  renderAll();
}

function clearCurrentTicket() {
  state.tickets[state.activeBoard] = [];
  qs("#amountReceived").value = "0,00";
  saveTickets();
  renderAll();
}

async function finishSale() {
  const ticket = currentTicket();
  const meta = currentMeta();
  const subtotal = currentSubtotal(ticket);
  const total = currentTotal();
  const received = parseMoney(qs("#amountReceived").value);

  if (ticket.length === 0) {
    showToast("Inclua pelo menos um item antes de finalizar.");
    return;
  }

  if (received < total) {
    showToast("Valor recebido menor que o total.");
    return;
  }

  const createdAt = nowIso();
  const saleId = uuid("sale");
  const sale = {
    id: saleId,
    storeId: state.settings.storeId,
    terminalId: state.settings.terminalId,
    boardNumber: state.activeBoard,
    mode: state.mode,
    operatorNumber: qs("#operatorNumber").value.trim() || "1",
    customerName: meta.customerName || "",
    coverValue: parseMoney(meta.coverValue),
    servicePercent: Number(meta.servicePercent || 0),
    discountPercent: Number(meta.discountPercent || 0),
    subtotal,
    total,
    status: "closed",
    syncStatus: "pending",
    createdAt
  };

  const saleItems = ticket.map((item) => {
    const product = state.products.find((candidate) => candidate.id === item.productId);
    const updatedProduct = {
      ...product,
      stock: Math.max(0, Number(product?.stock || 0) - item.qty)
    };
    return {
      id: uuid("item"),
      saleId,
      productId: item.productId,
      code: item.code,
      name: item.name,
      qty: item.qty,
      price: item.price,
      total: item.qty * item.price,
      note: item.note,
      productSnapshot: updatedProduct,
      createdAt
    };
  });

  const payment = {
    id: uuid("payment"),
    saleId,
    method: state.selectedPayment,
    amount: received,
    change: Math.max(0, received - total),
    createdAt
  };

  const event = {
    id: uuid("event"),
    type: "sale_created",
    status: "pending",
    createdAt,
    payload: {
      sale,
      items: saleItems.map(({ productSnapshot, ...item }) => item),
      payment
    }
  };

  await saveSaleBundle({ sale, items: saleItems, payment, event });
  state.products = await getAll("products");
  state.tickets[state.activeBoard] = [];
  state.ticketMeta[state.activeBoard] = {
    ...currentMeta(),
    coverValue: "0,00",
    servicePercent: "10",
    discountPercent: 0,
    customerName: ""
  };
  saveTickets();
  saveTicketMeta();
  state.lastReceipt = { sale, items: saleItems, payment };
  qs("#amountReceived").value = "0,00";
  renderReceipt();
  renderAll();
  await refreshLocalCounters();
  showToast("Venda salva offline no navegador. Sync fica pendente ate ter internet/endpoint.");
}

function renderReceipt() {
  if (!state.lastReceipt) return;
  const { sale, items, payment } = state.lastReceipt;
  qs("#lastReceipt").innerHTML = `
    <div>Venda: ${sale.id.slice(0, 13)}</div>
    <div>${items.map((item) => `${item.qty}x ${item.name}`).join("<br>")}</div>
    <div><strong>Total ${money(sale.total)}</strong></div>
    <div>${payment.method} | Troco ${money(payment.change)}</div>
  `;
}

async function refreshLocalCounters() {
  qs("#localSalesCount").textContent = await count("sales");
  const pending = await pendingEvents(9999);
  qs("#localQueueCount").textContent = pending.length;
  qs("#syncBadge").textContent = `${pending.length} pendente${pending.length === 1 ? "" : "s"}`;
  qs("#cashState").textContent = state.settings.cashOpen ? "Aberto" : "Fechado";
}

function updateOnlineState() {
  const badge = qs("#onlineBadge");
  if (navigator.onLine) {
    badge.textContent = "online";
    badge.classList.remove("offline");
    qs("#connectionText").textContent = "Online";
  } else {
    badge.textContent = "offline";
    badge.classList.add("offline");
    qs("#connectionText").textContent = "Offline";
  }
}

async function syncNow() {
  await refreshLocalCounters();
  const events = await pendingEvents(20);
  if (events.length === 0) {
    showToast("Nada pendente para sincronizar.");
    return;
  }

  if (!navigator.onLine) {
    showToast("Sem internet. O PDV continua vendendo offline.");
    return;
  }

  if (!state.settings.adminSyncEnabled) {
    showToast("Sincronizacao admin desativada nas configuracoes.");
    return;
  }

  if (!state.settings.syncEndpoint) {
    showToast("Configure a URL do admin para sincronizar.");
    return;
  }

  try {
    const response = await fetch(state.settings.syncEndpoint, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        store_id: state.settings.storeId,
        terminal_id: state.settings.terminalId,
        events: events.map(({ id, type, createdAt, payload }) => ({
          event_id: id,
          type,
          created_at: createdAt,
          payload
        }))
      })
    });

    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    await markEventsSynced(events);
    state.settings.lastSyncAt = nowIso();
    await put("terminal_settings", state.settings);
    await refreshLocalCounters();
    showToast(`${events.length} evento(s) sincronizado(s).`);
  } catch (error) {
    showToast(`Sync falhou. Venda continua salva offline. ${error.message}`);
  }
}

function openSettings() {
  qs("#ownerNameInput").value = state.settings.ownerName || "";
  qs("#businessNameInput").value = state.settings.businessName || initialSettings.businessName;
  qs("#legalNameInput").value = state.settings.legalName || initialSettings.legalName;
  qs("#cnpjInput").value = state.settings.cnpj || initialSettings.cnpj;
  qs("#phoneInput").value = state.settings.phone || initialSettings.phone;
  qs("#cityInput").value = state.settings.city || "";
  qs("#stateInput").value = state.settings.state || "";
  qs("#addressInput").value = state.settings.address || "";
  qs("#visualToastInput").checked = Boolean(state.settings.visualToast);
  qs("#notificationSoundInput").checked = Boolean(state.settings.notificationSound);
  qs("#inAppVibrationInput").checked = Boolean(state.settings.inAppVibration);
  qs("#notificationSoundKindInput").value = state.settings.notificationSoundKind || "PADRAO";
  qs("#autoPrintDeliveryInput").checked = Boolean(state.settings.autoPrintDelivery);
  qs("#autoPrintKitchenInput").checked = Boolean(state.settings.autoPrintKitchen);
  qs("#printLayoutInput").value = state.settings.printLayout || "PEQUENO";
  qs("#preferredPrinterInput").value = state.settings.preferredPrinter || "";
  qs("#receiptQrEnabledInput").checked = Boolean(state.settings.receiptQrEnabled);
  qs("#receiptQrKindInput").value = state.settings.receiptQrKind || "PIX";
  qs("#receiptQrContentInput").value = state.settings.receiptQrContent || "";
  qs("#autoCheckUpdatesInput").checked = Boolean(state.settings.autoCheckUpdates);
  qs("#adminSyncEnabledInput").checked = Boolean(state.settings.adminSyncEnabled);
  qs("#syncEndpointInput").value = state.settings.syncEndpoint || "";
  qs("#supabaseAuthEnabledInput").checked = state.settings.supabaseAuthEnabled !== false;
  qs("#supabaseUrlInput").value = state.settings.supabaseUrl || state.auth.url || "";
  qs("#supabaseAnonKeyInput").value = state.settings.supabaseAnonKey || state.auth.anonKey || "";
  qs("#storeIdInput").value = state.settings.storeId || "loja_demo";
  qs("#terminalIdInput").value = state.settings.terminalId || "caixa_01";
  qs("#settingsStatus").textContent = "";
  qs("#settingsDialog").showModal();
}

async function saveSettings(event) {
  event.preventDefault();
  state.settings = {
    ...state.settings,
    ownerName: qs("#ownerNameInput").value.trim(),
    businessName: qs("#businessNameInput").value.trim() || initialSettings.businessName,
    legalName: qs("#legalNameInput").value.trim(),
    cnpj: qs("#cnpjInput").value.trim() || initialSettings.cnpj,
    phone: qs("#phoneInput").value.trim() || initialSettings.phone,
    city: qs("#cityInput").value.trim(),
    state: qs("#stateInput").value.trim().toUpperCase(),
    address: qs("#addressInput").value.trim(),
    visualToast: qs("#visualToastInput").checked,
    notificationSound: qs("#notificationSoundInput").checked,
    inAppVibration: qs("#inAppVibrationInput").checked,
    notificationSoundKind: qs("#notificationSoundKindInput").value,
    autoPrintDelivery: qs("#autoPrintDeliveryInput").checked,
    autoPrintKitchen: qs("#autoPrintKitchenInput").checked,
    printLayout: qs("#printLayoutInput").value,
    preferredPrinter: qs("#preferredPrinterInput").value.trim(),
    receiptQrEnabled: qs("#receiptQrEnabledInput").checked,
    receiptQrKind: qs("#receiptQrKindInput").value,
    receiptQrContent: qs("#receiptQrContentInput").value.trim(),
    autoCheckUpdates: qs("#autoCheckUpdatesInput").checked,
    adminSyncEnabled: qs("#adminSyncEnabledInput").checked,
    supabaseAuthEnabled: qs("#supabaseAuthEnabledInput").checked,
    supabaseUrl: qs("#supabaseUrlInput").value.trim(),
    supabaseAnonKey: qs("#supabaseAnonKeyInput").value.trim(),
    storeId: qs("#storeIdInput").value.trim() || "loja_demo",
    terminalId: qs("#terminalIdInput").value.trim() || "caixa_01",
    syncEndpoint: qs("#syncEndpointInput").value.trim()
  };
  await put("terminal_settings", state.settings);
  state.auth = await createSupabaseAuth(state.settings);
  qs("#settingsDialog").close();
  renderSettingsLabels();
  renderLoginState();
  showToast("Configuracao salva localmente.");
}

function testNotification() {
  qs("#settingsStatus").textContent = "Notificacao de teste enviada.";
  if (state.settings?.inAppVibration && "vibrate" in navigator) {
    navigator.vibrate(80);
  }
  showToast("Notificacao de teste.");
}

function checkForUpdates() {
  qs("#settingsStatus").textContent = "Versao Web 1.0 em uso. Nenhuma atualizacao encontrada agora.";
  showToast("Verificacao de atualizacao concluida.");
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function setDialog(title, subtitle, bodyHtml, footerHtml = `<button value="cancel" class="ghost">Fechar</button>`) {
  if (typeof state.dialogCleanup === "function") {
    state.dialogCleanup();
    state.dialogCleanup = null;
  }

  qs("#workDialogTitle").textContent = title;
  qs("#workDialogSubtitle").textContent = subtitle;
  qs("#workDialogBody").innerHTML = bodyHtml;
  qs("#workDialogFooter").innerHTML = footerHtml;
  qs("#workDialog").showModal();
}

async function enqueueEvent(type, payload) {
  await put("sync_queue", {
    id: uuid("event"),
    type,
    status: "pending",
    createdAt: nowIso(),
    payload
  });
  await refreshLocalCounters();
}

function openSearchDialog() {
  setDialog(
    "Pesquisa de produtos",
    "Busque por codigo, nome ou grupo e inclua direto na comanda.",
    `
      <div class="dialog-grid">
        <label>Pesquisar<input id="dialogSearchProduct" placeholder="Codigo ou nome"></label>
      </div>
      <div id="dialogProductResults" class="dialog-list"></div>
    `
  );

  const render = () => {
    const query = qs("#dialogSearchProduct").value.trim().toLowerCase();
    const results = state.products
      .filter((product) => product.active !== false)
      .filter((product) => !query || `${product.code} ${product.name} ${product.category}`.toLowerCase().includes(query))
      .slice(0, 30);

    qs("#dialogProductResults").innerHTML = results.map((product) => `
      <button class="list-row" data-dialog-product="${product.code}" type="button">
        <span>${escapeHtml(product.code)}</span>
        <strong>${escapeHtml(product.name)}</strong>
        <small>${escapeHtml(product.category)} | Est. ${product.stock}</small>
        <b>${money(product.price)}</b>
      </button>
    `).join("");

    qsa("[data-dialog-product]").forEach((button) => {
      button.addEventListener("click", () => {
        state.selectedProductCode = button.dataset.dialogProduct;
        qs("#productCode").value = state.selectedProductCode;
        addCurrentProduct();
        qs("#workDialog").close();
      });
    });
  };

  qs("#dialogSearchProduct").addEventListener("input", render);
  render();
  qs("#dialogSearchProduct").focus();
}

function openTransferDialog() {
  if (currentTicket().length === 0) {
    showToast("Nao ha itens para transferir.");
    return;
  }

  const destinations = Array.from({ length: boardCount }, (_, index) => String(index + 1).padStart(6, "0"))
    .filter((board) => board !== state.activeBoard);

  setDialog(
    "Transferir comanda",
    `Origem ${state.activeBoard} com ${ticketLineCount()} item(ns).`,
    `<div class="board-picker">${destinations.map((board) => `
      <button class="board-card ${state.tickets[board]?.length ? "occupied" : ""}" data-transfer-board="${board}" type="button">
        <span>${board}</span>
        <small>${state.tickets[board]?.length ? "OCUPADA" : "LIVRE"}</small>
      </button>`).join("")}</div>`
  );

  qsa("[data-transfer-board]").forEach((button) => {
    button.addEventListener("click", () => {
      const destination = button.dataset.transferBoard;
      state.tickets[destination] = [...(state.tickets[destination] || []), ...currentTicket()];
      state.ticketMeta[destination] = { ...(state.ticketMeta[destination] || {}), ...currentMeta() };
      state.tickets[state.activeBoard] = [];
      state.ticketMeta[state.activeBoard] = { coverValue: "0,00", servicePercent: "10", discountPercent: 0, customerName: "", people: 1 };
      state.activeBoard = destination;
      saveTickets();
      saveTicketMeta();
      qs("#workDialog").close();
      renderAll();
      showToast(`Comanda transferida para ${destination}.`);
    });
  });
}

function openDiscountDialog() {
  const meta = currentMeta();
  setDialog(
    "Desconto",
    "Aplique desconto percentual na venda atual.",
    `
      <div class="dialog-grid">
        <label>Subtotal<input readonly value="${inputMoney(currentSubtotal())}"></label>
        <label>Desconto %<input id="discountPercentInput" inputmode="decimal" value="${Number(meta.discountPercent || 0)}"></label>
        <label>Total com desconto<input id="discountPreviewInput" readonly value="${inputMoney(currentTotal())}"></label>
      </div>
    `,
    `<button value="cancel" class="ghost">Cancelar</button><button id="applyDiscountButton" class="primary" type="button">Aplicar desconto</button>`
  );

  const preview = () => {
    const percent = Number.parseFloat(qs("#discountPercentInput").value.replace(",", ".")) || 0;
    const previous = meta.discountPercent;
    meta.discountPercent = Math.max(0, Math.min(100, percent));
    qs("#discountPreviewInput").value = inputMoney(currentTotal());
    meta.discountPercent = previous;
  };

  qs("#discountPercentInput").addEventListener("input", preview);
  qs("#applyDiscountButton").addEventListener("click", () => {
    meta.discountPercent = Math.max(0, Math.min(100, Number.parseFloat(qs("#discountPercentInput").value.replace(",", ".")) || 0));
    saveTicketMeta();
    qs("#workDialog").close();
    renderAll();
    showToast("Desconto aplicado.");
  });
}

async function openCustomerDialog() {
  state.customers = await getAll("customers");
  setDialog(
    "Cadastro de clientes",
    "Clientes ficam locais e entram na fila de sync quando salvos.",
    `
      <div class="dialog-grid two">
        <label>Nome<input id="customerNameInput"></label>
        <label>Telefone<input id="customerPhoneInput"></label>
        <label>CNPJ/CPF<input id="customerDocInput"></label>
        <label>Cidade<input id="customerCityInput"></label>
      </div>
      <div id="customerList" class="dialog-list"></div>
    `,
    `<button value="cancel" class="ghost">Fechar</button><button id="saveCustomerButton" class="primary" type="button">Salvar cliente</button>`
  );

  const render = () => {
    qs("#customerList").innerHTML = state.customers.length
      ? state.customers.map((customer) => `
        <button class="list-row" data-customer="${customer.id}" type="button">
          <strong>${escapeHtml(customer.name)}</strong>
          <small>${escapeHtml(customer.phone || "-")} | ${escapeHtml(customer.doc || "-")}</small>
          <b>Usar</b>
        </button>
      `).join("")
      : `<div class="empty-state">Nenhum cliente cadastrado.</div>`;

    qsa("[data-customer]").forEach((button) => {
      button.addEventListener("click", () => {
        const customer = state.customers.find((item) => item.id === button.dataset.customer);
        currentMeta().customerName = customer?.name || "";
        saveTicketMeta();
        qs("#workDialog").close();
        showToast(`Cliente selecionado: ${customer?.name}.`);
      });
    });
  };

  qs("#saveCustomerButton").addEventListener("click", async () => {
    const name = qs("#customerNameInput").value.trim();
    if (!name) {
      showToast("Informe o nome do cliente.");
      return;
    }
    const customer = {
      id: uuid("customer"),
      name,
      phone: qs("#customerPhoneInput").value.trim(),
      doc: qs("#customerDocInput").value.trim(),
      city: qs("#customerCityInput").value.trim(),
      createdAt: nowIso()
    };
    await put("customers", customer);
    await enqueueEvent("customer_saved", customer);
    state.customers = await getAll("customers");
    currentMeta().customerName = customer.name;
    saveTicketMeta();
    render();
    showToast("Cliente salvo e vinculado.");
  });

  render();
}

async function openReopenDialog() {
  const sales = (await getAll("sales")).sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 20);
  setDialog(
    "Reabrir comanda",
    "Carregue uma venda local finalizada de volta para a tela.",
    `<div class="dialog-list">${sales.length ? sales.map((sale) => `
      <button class="list-row" data-reopen-sale="${sale.id}" type="button">
        <span>${escapeHtml(sale.boardNumber)}</span>
        <strong>${money(sale.total)}</strong>
        <small>${new Date(sale.createdAt).toLocaleString("pt-BR")} | ${escapeHtml(sale.mode)}</small>
        <b>Reabrir</b>
      </button>
    `).join("") : `<div class="empty-state">Nenhuma venda local finalizada.</div>`}</div>`
  );

  qsa("[data-reopen-sale]").forEach((button) => {
    button.addEventListener("click", async () => {
      const sale = sales.find((item) => item.id === button.dataset.reopenSale);
      const items = (await getAll("sale_items")).filter((item) => item.saleId === sale.id);
      const board = sale.boardNumber || "000001";
      state.activeBoard = board;
      state.mode = /^\d/.test(board) ? "tables" : sale.mode || "counter";
      state.tickets[board] = items.map((item) => ({
        id: uuid("line"),
        productId: item.productId,
        code: item.code,
        name: item.name,
        qty: item.qty,
        price: item.price,
        note: item.note || ""
      }));
      saveTickets();
      qs("#workDialog").close();
      renderAll();
      showToast(`Venda ${sale.id.slice(0, 12)} reaberta.`);
    });
  });
}

async function openTeamDialog() {
  state.users = await getAll("users");
  setDialog(
    "Equipe",
    "Operadores, garcons e caixas salvos localmente.",
    `
      <div class="dialog-grid three">
        <label>Numero<input id="userNumberInput" inputmode="numeric"></label>
        <label>Nome<input id="userNameInput"></label>
        <label>Perfil<input id="userRoleInput" value="CAIXA"></label>
      </div>
      <div id="userList" class="dialog-list"></div>
    `,
    `<button value="cancel" class="ghost">Fechar</button><button id="saveUserButton" class="primary" type="button">Salvar usuario</button>`
  );

  const render = () => {
    qs("#userList").innerHTML = state.users.map((user) => `
      <button class="list-row" data-user="${user.id}" type="button">
        <span>${escapeHtml(user.number)}</span>
        <strong>${escapeHtml(user.name)}</strong>
        <small>${escapeHtml(user.role)}</small>
        <b>${user.active ? "ATIVO" : "INATIVO"}</b>
      </button>
    `).join("");
  };

  qs("#saveUserButton").addEventListener("click", async () => {
    const number = qs("#userNumberInput").value.trim();
    const name = qs("#userNameInput").value.trim();
    if (!number || !name) {
      showToast("Informe numero e nome.");
      return;
    }
    const user = {
      id: uuid("user"),
      number,
      name: name.toUpperCase(),
      role: qs("#userRoleInput").value.trim().toUpperCase() || "CAIXA",
      pin: number,
      active: true,
      createdAt: nowIso()
    };
    await put("users", user);
    await enqueueEvent("user_saved", user);
    state.users = await getAll("users");
    render();
    showToast("Usuario salvo.");
  });

  render();
}

async function openProductDialog() {
  setDialog(
    "Cadastro de produtos",
    "Crie produtos no banco local do navegador.",
    `
      <div class="dialog-grid three">
        <label>Codigo<input id="productFormCode" inputmode="numeric"></label>
        <label>Produto<input id="productFormName"></label>
        <label>Grupo<input id="productFormCategory" value="${escapeHtml(state.selectedCategory)}"></label>
        <label>Preco venda<input id="productFormPrice" inputmode="decimal"></label>
        <label>Preco compra<input id="productFormCost" inputmode="decimal"></label>
        <label>Estoque<input id="productFormStock" inputmode="numeric"></label>
      </div>
      <div id="productFormList" class="dialog-list"></div>
    `,
    `<button value="cancel" class="ghost">Fechar</button><button id="saveProductButton" class="primary" type="button">Salvar produto</button>`
  );

  const render = () => {
    qs("#productFormList").innerHTML = state.products.slice(0, 60).map((product) => `
      <button class="list-row" data-edit-product="${product.code}" type="button">
        <span>${escapeHtml(product.code)}</span>
        <strong>${escapeHtml(product.name)}</strong>
        <small>${escapeHtml(product.category)} | Est. ${product.stock}</small>
        <b>${money(product.price)}</b>
      </button>
    `).join("");
    qsa("[data-edit-product]").forEach((button) => {
      button.addEventListener("click", () => {
        const product = state.products.find((item) => item.code === button.dataset.editProduct);
        qs("#productFormCode").value = product.code;
        qs("#productFormName").value = product.name;
        qs("#productFormCategory").value = product.category;
        qs("#productFormPrice").value = inputMoney(product.price);
        qs("#productFormCost").value = inputMoney(product.cost);
        qs("#productFormStock").value = product.stock;
      });
    });
  };

  qs("#saveProductButton").addEventListener("click", async () => {
    const code = qs("#productFormCode").value.trim().padStart(6, "0");
    const name = qs("#productFormName").value.trim().toUpperCase();
    if (!code || !name) {
      showToast("Informe codigo e produto.");
      return;
    }
    const existing = state.products.find((item) => item.code === code);
    const product = {
      id: existing?.id || code,
      code,
      name,
      category: qs("#productFormCategory").value.trim().toUpperCase() || "GERAL",
      price: parseMoney(qs("#productFormPrice").value),
      cost: parseMoney(qs("#productFormCost").value),
      stock: Number.parseInt(qs("#productFormStock").value || "0", 10) || 0,
      minStock: existing?.minStock ?? 0,
      active: true,
      updatedAt: nowIso()
    };
    await put("products", product);
    await enqueueEvent("product_saved", product);
    state.products = await getAll("products");
    renderAll();
    render();
    showToast("Produto salvo.");
  });

  render();
}

async function openCashDialog() {
  state.cashMovements = (await getAll("cash_movements")).sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  const totalCash = state.cashMovements.reduce((sum, item) => sum + item.amount, 0);
  setDialog(
    "Caixa",
    "Movimentos locais de suprimento, sangria e observacoes.",
    `
      <div class="dialog-grid three">
        <label>Tipo<input id="cashTypeInput" value="SUPRIMENTO"></label>
        <label>Valor<input id="cashAmountInput" inputmode="decimal"></label>
        <label>Observacao<input id="cashNoteInput"></label>
      </div>
      <div class="cash-summary"><strong>Saldo movimentos: ${money(totalCash)}</strong><span>Estado: ${state.settings.cashOpen ? "Aberto" : "Fechado"}</span></div>
      <div id="cashMovementList" class="dialog-list"></div>
    `,
    `<button value="cancel" class="ghost">Fechar</button><button id="saveCashMovementButton" class="primary" type="button">Salvar movimento</button>`
  );

  const render = () => {
    qs("#cashMovementList").innerHTML = state.cashMovements.length
      ? state.cashMovements.map((item) => `
        <div class="list-row">
          <span>${escapeHtml(item.type)}</span>
          <strong>${money(item.amount)}</strong>
          <small>${escapeHtml(item.note || "-")} | ${new Date(item.createdAt).toLocaleString("pt-BR")}</small>
        </div>
      `).join("")
      : `<div class="empty-state">Nenhum movimento de caixa.</div>`;
  };

  qs("#saveCashMovementButton").addEventListener("click", async () => {
    const rawType = qs("#cashTypeInput").value.trim().toUpperCase() || "SUPRIMENTO";
    const amount = parseMoney(qs("#cashAmountInput").value);
    const movement = {
      id: uuid("cash"),
      type: rawType,
      amount: rawType.includes("SANGRIA") ? -Math.abs(amount) : Math.abs(amount),
      note: qs("#cashNoteInput").value.trim(),
      terminalId: state.settings.terminalId,
      createdAt: nowIso()
    };
    await put("cash_movements", movement);
    await enqueueEvent("cash_movement_created", movement);
    state.cashMovements = (await getAll("cash_movements")).sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    render();
    showToast("Movimento salvo.");
  });

  render();
}

async function toggleCash() {
  state.settings.cashOpen = !state.settings.cashOpen;
  await put("terminal_settings", state.settings);
  await enqueueEvent("cash_state_changed", {
    terminalId: state.settings.terminalId,
    cashOpen: state.settings.cashOpen,
    changedAt: nowIso()
  });
  await refreshLocalCounters();
  showToast(state.settings.cashOpen ? "Caixa aberto." : "Caixa fechado.");
}

function newDelivery() {
  state.mode = "delivery";
  state.activeBoard = `DEL-${String(Date.now()).slice(-5)}`;
  currentMeta().customerName = "DELIVERY";
  saveTicketMeta();
  renderAll();
  showToast("Novo delivery aberto.");
}

async function openStockDialog() {
  setDialog(
    "Estoque / Receitas",
    "Ajuste estoque minimo e saldo local dos produtos.",
    `<div id="stockList" class="dialog-list stock-list"></div>`,
    `<button value="cancel" class="ghost">Fechar</button><button id="saveStockButton" class="primary" type="button">Salvar estoque</button>`
  );

  qs("#stockList").innerHTML = state.products.map((product) => `
    <div class="stock-edit-row" data-stock-row="${product.id}">
      <span>${escapeHtml(product.code)}</span>
      <strong>${escapeHtml(product.name)}</strong>
      <label>Estoque<input data-stock="${product.id}" inputmode="numeric" value="${product.stock}"></label>
      <label>Min<input data-min-stock="${product.id}" inputmode="numeric" value="${product.minStock || 0}"></label>
    </div>
  `).join("");

  qs("#saveStockButton").addEventListener("click", async () => {
    const updated = [];
    for (const product of state.products) {
      const stockInput = qs(`[data-stock="${CSS.escape(product.id)}"]`);
      const minInput = qs(`[data-min-stock="${CSS.escape(product.id)}"]`);
      const next = {
        ...product,
        stock: Number.parseInt(stockInput.value || "0", 10) || 0,
        minStock: Number.parseInt(minInput.value || "0", 10) || 0,
        updatedAt: nowIso()
      };
      await put("products", next);
      updated.push(next);
    }
    await enqueueEvent("stock_updated", { products: updated.map(({ id, code, stock, minStock }) => ({ id, code, stock, minStock })) });
    state.products = await getAll("products");
    qs("#workDialog").close();
    renderAll();
    showToast("Estoque salvo.");
  });
}

boot().catch((error) => {
  console.error(error);
  showToast(`Erro ao iniciar PDV: ${error.message}`);
});
