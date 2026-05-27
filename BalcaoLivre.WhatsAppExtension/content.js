const PDV_ENDPOINT = "http://127.0.0.1:8787/whatsapp/message";
const seenMessages = new Set();
const initializedChats = new Set();
const startupUnreadBaselines = new Map();
let readyForNewMessages = false;
let pendingUnreadOpen = null;
let startupUnreadBaselineCaptured = false;

function textOf(element) {
  return (element?.innerText || element?.textContent || "").replace(/\s+/g, " ").trim();
}

function plain(value) {
  return (value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
}

function isUnreadLabel(value) {
  const label = plain(value);
  return (
    label.includes("unread") ||
    label.includes("unread-count") ||
    label.includes("nao lida") ||
    label.includes("nao lidas") ||
    label.includes("nao lido") ||
    label.includes("nao lidos") ||
    label.includes("nova mensagem") ||
    label.includes("novas mensagens")
  );
}

function getChatName() {
  return (
    document.querySelector("header span[title]")?.getAttribute("title") ||
    textOf(document.querySelector("header")) ||
    "WhatsApp"
  );
}

function chatNameFromRow(row) {
  return (
    row.querySelector("span[title]")?.getAttribute("title") ||
    textOf(row).split(/\d{1,2}:\d{2}|Ontem|Yesterday/i)[0]?.trim() ||
    ""
  );
}

function chatRowKey(row) {
  const title = chatNameFromRow(row);
  const rowText = textOf(row)
    .replace(/\d{1,2}:\d{2}.*/g, "")
    .slice(0, 120);
  return plain(title || rowText || row.getAttribute("data-id") || "");
}

function chatRowSignature(row) {
  const pieces = [
    chatNameFromRow(row),
    ...[...row.querySelectorAll("span[title], span[dir='auto'], div[dir='auto']")]
      .map(textOf)
      .filter(Boolean)
  ];
  return plain([...new Set(pieces)].join("|").slice(0, 240));
}

function getIncomingMessages() {
  const nodes = [
    ...document.querySelectorAll("div.message-in"),
    ...document.querySelectorAll("[data-testid='msg-container']")
  ];

  return nodes
    .filter((node) => !node.closest("div.message-out"))
    .map((node, index) => {
      const bubbleText =
        textOf(node.querySelector("span.selectable-text")) ||
        textOf(node.querySelector("[dir='ltr']")) ||
        textOf(node);
      return { node, text: bubbleText, index };
    })
    .filter((item) => item.text && item.text.length <= 2000);
}

function messageKey(chatName, item) {
  const id =
    item.node.getAttribute("data-id") ||
    item.node.querySelector("[data-id]")?.getAttribute("data-id") ||
    `${item.index}::${item.text}`;
  return `${chatName}::${id}`;
}

function rememberMessage(chatName, item) {
  seenMessages.add(messageKey(chatName, item));
  if (seenMessages.size > 800) {
    const first = seenMessages.values().next().value;
    seenMessages.delete(first);
  }
}

function rememberVisibleMessages(chatName, messages) {
  for (const item of messages) {
    rememberMessage(chatName, item);
  }
}

function unreadMarkerOf(row) {
  const markers = [
    ...row.querySelectorAll("span[aria-label], div[aria-label], span[data-icon], [data-testid]")
  ];
  return markers.find((marker) => {
    const label = `${marker.getAttribute("aria-label") || ""} ${marker.getAttribute("data-icon") || ""} ${marker.getAttribute("data-testid") || ""}`.toLowerCase();
    return (
      label.includes("unread") ||
      label.includes("não lida") ||
      label.includes("nao lida") ||
      label.includes("não lidas") ||
      label.includes("nao lidas") ||
      label.includes("unread-count")
    );
  });
}

function unreadCountOf(row, marker) {
  const label = marker?.getAttribute("aria-label") || "";
  const labelNumber = label.match(/\d+/)?.[0];
  if (labelNumber) return Math.max(1, Number(labelNumber));

  const markerText = textOf(marker);
  const markerNumber = markerText.match(/^\d+$/)?.[0];
  if (markerNumber) return Math.max(1, Number(markerNumber));

  const smallNumbers = [...row.querySelectorAll("span")]
    .map((span) => textOf(span))
    .map((text) => text.match(/^\d{1,2}$/)?.[0])
    .filter(Boolean)
    .map(Number);
  return Math.max(1, smallNumbers.at(-1) || 1);
}

function findUnreadChat() {
  const listRoots = [
    ...document.querySelectorAll("[aria-label*='Chat' i], [aria-label*='conversa' i], [role='grid']")
  ];
  const rows = [
    ...listRoots.flatMap((root) => [
      ...root.querySelectorAll("[role='row'], [role='listitem'], [data-testid='cell-frame-container']")
    ]),
    ...document.querySelectorAll("[data-testid='cell-frame-container']")
  ];

  for (const row of rows) {
    if (row.closest("footer") || row.closest("header")) continue;
    const marker = unreadMarkerOf(row);
    if (!marker) continue;
    if (row.closest(".message-in, .message-out")) continue;

    return {
      row,
      count: unreadCountOf(row, marker)
    };
  }

  return null;
}

function clickUnreadChat() {
  const unread = findUnreadChat();
  if (!unread) return false;

  const clickable =
    unread.row.querySelector("[role='gridcell']") ||
    unread.row.querySelector("[data-testid='cell-frame-container']") ||
    unread.row;

  clickable.scrollIntoView({ block: "center" });
  clickable.click();
  pendingUnreadOpen = {
    count: unread.count,
    clickedAt: Date.now()
  };
  return true;
}

function visible(element) {
  if (!element) return false;
  const rect = element.getBoundingClientRect();
  return rect.width > 0 && rect.height > 0;
}

function chatRows() {
  const listRoots = [
    document.querySelector("#pane-side"),
    ...document.querySelectorAll("[data-testid='chat-list'], [aria-label*='Chat' i], [aria-label*='conversa' i], [aria-label*='Lista' i], [role='grid']")
  ].filter(Boolean);

  const rows = [
    ...listRoots.flatMap((root) => [
      ...root.querySelectorAll("[role='row'], [role='listitem'], [data-testid='cell-frame-container'], [tabindex='0'], [tabindex='-1']")
    ]),
    ...document.querySelectorAll("#pane-side [role='row'], #pane-side [role='listitem'], #pane-side [data-testid='cell-frame-container'], #pane-side [tabindex='0'], #pane-side [tabindex='-1']")
  ];

  return [...new Set(rows)].filter((row) => {
    if (!row || row.closest("footer") || row.closest("header") || row.closest(".message-in, .message-out")) return false;
    if (!visible(row)) return false;
    const rect = row.getBoundingClientRect();
    return rect.width > 180 && rect.height >= 38;
  });
}

function unreadMarkerOf(row) {
  const markers = [
    row,
    ...row.querySelectorAll("span[aria-label], div[aria-label], span[data-icon], div[data-icon], [data-testid], span, div")
  ];
  const labelMarker = markers.find((marker) => {
    const label = `${marker.getAttribute("aria-label") || ""} ${marker.getAttribute("data-icon") || ""} ${marker.getAttribute("data-testid") || ""} ${marker.getAttribute("title") || ""}`;
    return isUnreadLabel(label);
  });
  if (labelMarker) return labelMarker;

  const rowRect = row.getBoundingClientRect();
  return [...row.querySelectorAll("span, div")].find((marker) => {
    const markerText = textOf(marker);
    if (!/^\d{1,3}$/.test(markerText) || !visible(marker)) return false;
    const rect = marker.getBoundingClientRect();
    return rect.width <= 36
      && rect.height <= 28
      && rect.left > rowRect.left + rowRect.width * 0.55;
  });
}

function findUnreadChatsRaw() {
  const found = [];
  const seenRows = new Set();

  function add(row, marker) {
    if (!row || seenRows.has(row)) return;
    if (row.closest("footer") || row.closest("header") || row.closest(".message-in, .message-out")) return;
    if (!visible(row)) return;
    seenRows.add(row);
    found.push({
      row,
      count: unreadCountOf(row, marker),
      name: chatNameFromRow(row),
      key: chatRowKey(row),
      signature: chatRowSignature(row)
    });
  }

  const directMarkers = [
    ...document.querySelectorAll("#pane-side span[aria-label], #pane-side div[aria-label], #pane-side span[data-icon], #pane-side div[data-icon], #pane-side [data-testid]")
  ];
  for (const marker of directMarkers) {
    const label = `${marker.getAttribute("aria-label") || ""} ${marker.getAttribute("data-icon") || ""} ${marker.getAttribute("data-testid") || ""} ${marker.getAttribute("title") || ""}`;
    if (!isUnreadLabel(label)) continue;
    add(marker.closest("[role='row'], [role='listitem'], [data-testid='cell-frame-container'], #pane-side [tabindex='0'], #pane-side [tabindex='-1']"), marker);
  }

  for (const row of chatRows()) {
    const marker = unreadMarkerOf(row);
    if (marker) add(row, marker);
  }

  return found;
}

function captureStartupUnreadBaselines() {
  startupUnreadBaselines.clear();
  for (const unread of findUnreadChatsRaw()) {
    if (!unread.key) continue;
    startupUnreadBaselines.set(unread.key, {
      count: unread.count,
      signature: unread.signature
    });
  }
  startupUnreadBaselineCaptured = true;
}

function findUnreadChat() {
  for (const unread of findUnreadChatsRaw()) {
    const baseline = startupUnreadBaselines.get(unread.key);
    if (startupUnreadBaselineCaptured && baseline) {
      if (unread.count <= baseline.count && unread.signature === baseline.signature) {
        continue;
      }
      unread.count = Math.max(1, unread.count - baseline.count);
    }
    return unread;
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
    clientX: rect.left + Math.max(4, rect.width / 2),
    clientY: rect.top + Math.max(4, rect.height / 2)
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

function clickUnreadChat() {
  const unread = findUnreadChat();
  if (!unread) return false;

  const clickable =
    unread.row.querySelector("[role='gridcell']") ||
    unread.row.querySelector("[data-testid='cell-frame-container']") ||
    unread.row;

  clickable.scrollIntoView({ block: "center" });
  clickElement(clickable);
  pendingUnreadOpen = {
    count: Math.max(1, unread.count || 1),
    clickedAt: Date.now(),
    chatBefore: getChatName(),
    targetName: unread.name || ""
  };
  return true;
}

function processMessages(chatName, messages, processLatestCount = 0) {
  let itemsToProcess = messages;
  if (processLatestCount > 0 && messages.length > processLatestCount) {
    const oldMessages = messages.slice(0, -processLatestCount);
    rememberVisibleMessages(chatName, oldMessages);
    itemsToProcess = messages.slice(-processLatestCount);
  }

  for (const item of itemsToProcess) {
    const key = messageKey(chatName, item);
    if (seenMessages.has(key)) continue;
    rememberMessage(chatName, item);
    postToPdv(item.text).catch(() => {});
  }
}

function scanCurrentChat(options = {}) {
  const chatName = getChatName();
  const messages = getIncomingMessages();

  if (!readyForNewMessages) {
    rememberVisibleMessages(chatName, messages);
    return;
  }

  if (!initializedChats.has(chatName)) {
    initializedChats.add(chatName);
    if (options.processLatestCount) {
      processMessages(chatName, messages, options.processLatestCount);
      return;
    }

    rememberVisibleMessages(chatName, messages);
    return;
  }

  processMessages(chatName, messages);
}

async function postToPdv(message) {
  const response = await fetch(PDV_ENDPOINT, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      customerName: getChatName(),
      chatId: getChatName(),
      phone: "",
      message
    })
  });

  if (!response.ok) return;
  const result = await response.json();
  if (result?.autoReply && result?.reply) {
    await sendReply(result.reply);
  }
}

async function sendReply(text) {
  const editable =
    document.querySelector("footer div[contenteditable='true'][role='textbox']") ||
    document.querySelector("footer div[contenteditable='true']");
  if (!editable) return;

  editable.focus();
  document.execCommand("selectAll", false, null);
  document.execCommand("insertText", false, text);
  editable.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "insertText", data: text }));
  await new Promise((resolve) => setTimeout(resolve, 250));

  const sendButton =
    document.querySelector("footer button[aria-label*='Send']") ||
    document.querySelector("footer button[aria-label*='Enviar']") ||
    document.querySelector("span[data-icon='send']")?.closest("button");
  if (sendButton) clickElement(sendButton);
}

function isWhatsAppLoggedIn() {
  return Boolean(document.querySelector("#pane-side, [data-testid='chat-list'], [aria-label*='Chat' i], [aria-label*='conversa' i], [role='grid'], footer div[contenteditable='true']"));
}

function armForNewMessages() {
  if (readyForNewMessages) return true;
  if (!isWhatsAppLoggedIn()) return false;

  captureStartupUnreadBaselines();
  const chatName = getChatName();
  rememberVisibleMessages(chatName, getIncomingMessages());
  initializedChats.add(chatName);
  readyForNewMessages = true;
  return true;
}

function scan() {
  if (!armForNewMessages()) {
    return;
  }

  if (pendingUnreadOpen) {
    if (Date.now() - pendingUnreadOpen.clickedAt < 900) return;
    if (pendingUnreadOpen.chatBefore
        && getChatName() === pendingUnreadOpen.chatBefore
        && Date.now() - pendingUnreadOpen.clickedAt < 2200) {
      return;
    }
    const latestCount = pendingUnreadOpen.count || 1;
    pendingUnreadOpen = null;
    scanCurrentChat({ processLatestCount: latestCount });
    return;
  }

  scanCurrentChat();

  if (clickUnreadChat()) {
    return;
  }
}

const observer = new MutationObserver(() => window.clearTimeout(window.__balcaoLivreScanSoon) || (window.__balcaoLivreScanSoon = window.setTimeout(scan, 300)));
observer.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ["aria-label", "data-icon", "data-testid", "class"] });

setTimeout(scan, 1500);
setInterval(scan, 1800);
scan();
