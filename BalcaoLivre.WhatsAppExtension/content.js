const PDV_ENDPOINT = "http://127.0.0.1:8787/whatsapp/message";
const seenMessages = new Set();
const initializedChats = new Set();
let readyForNewMessages = false;
let pendingUnreadOpen = null;

function textOf(element) {
  return (element?.innerText || element?.textContent || "").replace(/\s+/g, " ").trim();
}

function getChatName() {
  return (
    document.querySelector("header span[title]")?.getAttribute("title") ||
    textOf(document.querySelector("header")) ||
    "WhatsApp"
  );
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
  sendButton?.click();
}

function scan() {
  if (!readyForNewMessages) {
    scanCurrentChat();
    return;
  }

  if (pendingUnreadOpen) {
    if (Date.now() - pendingUnreadOpen.clickedAt < 900) return;
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

setTimeout(() => {
  readyForNewMessages = true;
  const chatName = getChatName();
  rememberVisibleMessages(chatName, getIncomingMessages());
  initializedChats.add(chatName);
}, 1500);

setInterval(scan, 2500);
scan();
