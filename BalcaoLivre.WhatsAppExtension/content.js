(() => {
  const VERSION = "0.2.0";
  const INSTANCE = `${VERSION}:${Date.now()}`;
  const PDV_ENDPOINT = "http://127.0.0.1:8787/whatsapp/message";
  const ATTR = "data-balcao-livre-whatsapp";
  const LAST_ATTR = "data-balcao-livre-whatsapp-last";

  window.__balcaoLivreWhatsAppConnectorInstance = INSTANCE;

  const seenMessages = new Set();
  const startupUnread = new Map();
  const knownRows = new Map();
  const sentReplies = new Map();

  let ready = false;
  let pendingChat = null;
  let activeChat = "";

  function mark(state) {
    document.documentElement.setAttribute(ATTR, `${VERSION}:${state}:${Date.now()}`);
  }

  function log(message) {
    document.documentElement.setAttribute(LAST_ATTR, `${Date.now()}:${message}`.slice(0, 260));
    console.debug(`[Balcao Livre WhatsApp] ${message}`);
  }

  function sameInstance() {
    return window.__balcaoLivreWhatsAppConnectorInstance === INSTANCE;
  }

  function textOf(element) {
    return (element?.innerText || element?.textContent || "").replace(/\s+/g, " ").trim();
  }

  function plain(value) {
    return (value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
  }

  function visible(element) {
    if (!element) return false;
    const rect = element.getBoundingClientRect();
    return rect.width > 0 && rect.height > 0;
  }

  function isLoggedIn() {
    return Boolean(document.querySelector("#pane-side"));
  }

  function currentChatName() {
    return (
      document.querySelector("header span[title]")?.getAttribute("title") ||
      textOf(document.querySelector("header")) ||
      "Cliente WhatsApp"
    );
  }

  function chatRows() {
    const pane = document.querySelector("#pane-side");
    if (!pane) return [];

    const rows = [
      ...pane.querySelectorAll("[data-testid='cell-frame-container'], [role='listitem'], [role='row'], div[aria-label][tabindex='0'], div[aria-label][tabindex='-1']")
    ];

    return [...new Set(rows)].filter((row) => {
      if (!visible(row) || row.closest("footer") || row.closest("header")) return false;
      const rect = row.getBoundingClientRect();
      const value = textOf(row);
      if (value.length < 2 || value.length > 650) return false;
      if (/^arquivadas?\b/i.test(value)) return false;
      return rect.width > 180 && rect.height >= 42 && rect.height <= 145;
    });
  }

  function rowName(row) {
    return row.querySelector("span[title]")?.getAttribute("title") || textOf(row).split(/\d{1,2}:\d{2}|Ontem|Yesterday/i)[0]?.trim() || "";
  }

  function rowKey(row) {
    return plain(rowName(row) || textOf(row).replace(/\d{1,2}:\d{2}.*/g, "").slice(0, 140));
  }

  function rowSignature(row) {
    const parts = [
      rowName(row),
      ...[...row.querySelectorAll("span[title], span[dir='auto'], div[dir='auto']")]
        .slice(0, 10)
        .map(textOf)
        .filter(Boolean)
    ];
    return plain([...new Set(parts)].join("|").replace(/\b\d{1,2}:\d{2}\b/g, "").slice(0, 320));
  }

  function looksUnread(value) {
    const label = plain(value);
    return label.includes("unread")
      || label.includes("nao lida")
      || label.includes("nao lidas")
      || label.includes("nao lido")
      || label.includes("nao lidos")
      || label.includes("nova mensagem")
      || label.includes("novas mensagens");
  }

  function unreadCount(row) {
    const labelled = [...row.querySelectorAll("[aria-label], [data-icon], [data-testid], [title]")]
      .find((el) => looksUnread(`${el.getAttribute("aria-label") || ""} ${el.getAttribute("data-icon") || ""} ${el.getAttribute("data-testid") || ""} ${el.getAttribute("title") || ""}`));

    const labelledNumber = labelled?.getAttribute("aria-label")?.match(/\d+/)?.[0];
    if (labelledNumber) return Math.max(1, Number(labelledNumber));

    const rect = row.getBoundingClientRect();
    const numberBubble = [...row.querySelectorAll("span, div")].find((el) => {
      const value = textOf(el);
      if (!/^\d{1,3}$/.test(value) || !visible(el)) return false;
      const itemRect = el.getBoundingClientRect();
      return itemRect.left > rect.left + rect.width * 0.55 && itemRect.width <= 42 && itemRect.height <= 32;
    });

    if (numberBubble) return Math.max(1, Number(textOf(numberBubble)));
    return labelled ? 1 : 0;
  }

  function captureStartupState() {
    startupUnread.clear();
    knownRows.clear();

    for (const row of chatRows()) {
      const key = rowKey(row);
      const signature = rowSignature(row);
      if (key) knownRows.set(key, signature);
      const count = unreadCount(row);
      if (key && count > 0) startupUnread.set(key, { count, signature });
    }

    markCurrentVisibleMessages();
    ready = true;
    mark("ready");
    log(`Leitor ativo. ${startupUnread.size} conversa(s) antiga(s) ignorada(s).`);
  }

  function newestUnreadChat() {
    for (const row of chatRows()) {
      const key = rowKey(row);
      if (!key) continue;

      const count = unreadCount(row);
      const signature = rowSignature(row);
      const baseline = startupUnread.get(key);
      knownRows.set(key, signature);

      if (count <= 0) continue;
      if (baseline && count <= baseline.count && signature === baseline.signature) continue;

      return {
        row,
        name: rowName(row),
        key,
        count: baseline ? Math.max(1, count - baseline.count) : count
      };
    }

    return null;
  }

  function clickElement(element) {
    if (!element) return false;
    const rect = element.getBoundingClientRect();
    const options = {
      bubbles: true,
      cancelable: true,
      view: window,
      clientX: rect.left + Math.max(8, rect.width / 2),
      clientY: rect.top + Math.max(8, rect.height / 2)
    };

    element.dispatchEvent(new PointerEvent("pointerover", options));
    element.dispatchEvent(new MouseEvent("mouseover", options));
    element.dispatchEvent(new PointerEvent("pointerdown", options));
    element.dispatchEvent(new MouseEvent("mousedown", options));
    element.dispatchEvent(new PointerEvent("pointerup", options));
    element.dispatchEvent(new MouseEvent("mouseup", options));
    element.dispatchEvent(new MouseEvent("click", options));
    return true;
  }

  function openUnreadChat(candidate) {
    if (!candidate) return false;
    const clickable = candidate.row.querySelector("[role='gridcell']") || candidate.row;
    clickable.scrollIntoView({ block: "center" });
    pendingChat = {
      count: Math.max(1, candidate.count || 1),
      clickedAt: Date.now(),
      previousChat: currentChatName(),
      name: candidate.name || candidate.key || "cliente"
    };
    log(`Abrindo conversa nova: ${pendingChat.name}`);
    return clickElement(clickable);
  }

  function incomingMessages() {
    const nodes = [
      ...document.querySelectorAll("div.message-in"),
      ...document.querySelectorAll("[data-testid='msg-container']"),
      ...document.querySelectorAll("div[data-id]")
    ];

    return [...new Set(nodes)]
      .filter((node) => !node.closest(".message-out") && !node.classList.contains("message-out"))
      .map((node, index) => ({
        node,
        index,
        text: textOf(node.querySelector("span.selectable-text")) || textOf(node.querySelector("[dir='ltr']")) || textOf(node)
      }))
      .filter((item) => item.text && item.text.length <= 2000);
  }

  function messageKey(chat, item) {
    const id = item.node.getAttribute("data-id") || item.node.querySelector("[data-id]")?.getAttribute("data-id") || `${item.index}:${item.text}`;
    return `${chat}:${id}`;
  }

  function remember(chat, item) {
    seenMessages.add(messageKey(chat, item));
    if (seenMessages.size > 1000) seenMessages.delete(seenMessages.values().next().value);
  }

  function markCurrentVisibleMessages() {
    const chat = currentChatName();
    activeChat = chat;
    for (const item of incomingMessages()) remember(chat, item);
  }

  function processCurrentChat(latestCount = 1) {
    const chat = currentChatName();
    const unseen = incomingMessages().filter((item) => !seenMessages.has(messageKey(chat, item)));
    if (unseen.length === 0) return;

    const limit = Math.max(1, latestCount || 1);
    const oldItems = unseen.length > limit ? unseen.slice(0, -limit) : [];
    for (const item of oldItems) remember(chat, item);

    for (const item of unseen.slice(-limit)) {
      remember(chat, item);
      postToPdv(chat, item.text).catch((error) => log(`Falha PDV: ${error?.message || error}`));
    }
  }

  async function postToPdv(chat, message) {
    log(`Mensagem recebida de ${chat}: ${message.slice(0, 80)}`);
    const response = await fetch(PDV_ENDPOINT, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        customerName: chat,
        chatId: chat,
        phone: "",
        message
      })
    });

    if (!response.ok) {
      log(`PDV respondeu HTTP ${response.status}`);
      return;
    }

    const result = await response.json();
    if (result?.autoReply && result?.reply) await sendReply(result.reply);
  }

  function inputBox() {
    return [
      ...document.querySelectorAll("footer div[contenteditable='true'][role='textbox'], footer div[contenteditable='true'], div[contenteditable='true'][role='textbox']")
    ].find(visible) || null;
  }

  function sendButton() {
    const candidate = [
      ...document.querySelectorAll("footer button[aria-label*='Enviar' i], footer button[aria-label*='Send' i], button[aria-label*='Enviar' i], button[aria-label*='Send' i], span[data-icon='send']")
    ].find(visible);
    return candidate?.closest("button") || candidate || null;
  }

  function replyKey(chat, text) {
    return `${chat}:${String(text || "").slice(0, 500)}`;
  }

  function recentlySent(chat, text) {
    const key = replyKey(chat, text);
    const now = Date.now();
    for (const [itemKey, sentAt] of sentReplies) {
      if (now - sentAt > 120000) sentReplies.delete(itemKey);
    }
    return sentReplies.has(key);
  }

  async function writeText(text) {
    const box = inputBox();
    if (!box) {
      log("Campo de mensagem nao encontrado.");
      return null;
    }

    box.focus();
    document.execCommand("selectAll", false, null);
    document.execCommand("insertText", false, text);
    box.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "insertText", data: text }));
    await new Promise((resolve) => setTimeout(resolve, 450));
    return box;
  }

  async function sendReply(text) {
    const chat = currentChatName();
    if (recentlySent(chat, text)) {
      log("Resposta duplicada ignorada.");
      return;
    }

    const box = await writeText(text);
    if (!box) return;

    const button = sendButton();
    if (button) {
      clickElement(button);
    } else {
      const options = { key: "Enter", code: "Enter", keyCode: 13, which: 13, bubbles: true, cancelable: true };
      box.dispatchEvent(new KeyboardEvent("keydown", options));
      box.dispatchEvent(new KeyboardEvent("keyup", options));
    }

    sentReplies.set(replyKey(chat, text), Date.now());
    log("Resposta enviada.");
  }

  function tick() {
    if (!sameInstance()) return;

    if (!isLoggedIn()) {
      mark("waiting-login");
      log("Aguardando WhatsApp logado.");
      return;
    }

    if (!ready) {
      captureStartupState();
      return;
    }

    if (pendingChat) {
      if (Date.now() - pendingChat.clickedAt < 1000) return;
      if (Date.now() - pendingChat.clickedAt > 7000) {
        log("Nao consegui abrir a conversa nova.");
        pendingChat = null;
        markCurrentVisibleMessages();
        return;
      }

      if (currentChatName() === pendingChat.previousChat && Date.now() - pendingChat.clickedAt < 2500) return;

      const count = pendingChat.count;
      pendingChat = null;
      activeChat = currentChatName();
      processCurrentChat(count);
      return;
    }

    if (currentChatName() !== activeChat) {
      markCurrentVisibleMessages();
      log("Conversa manual marcada como historico.");
      return;
    }

    processCurrentChat(1);
    openUnreadChat(newestUnreadChat());
  }

  mark("loaded");
  window.setTimeout(tick, 1200);
  window.setInterval(tick, 1800);
  return true;
})();
