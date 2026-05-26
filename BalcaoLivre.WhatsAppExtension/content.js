const PDV_ENDPOINT = "http://127.0.0.1:8787/whatsapp/message";
const seenMessages = new Set();

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
    .map((node) => {
      const bubbleText =
        textOf(node.querySelector("span.selectable-text")) ||
        textOf(node.querySelector("[dir='ltr']")) ||
        textOf(node);
      return { node, text: bubbleText };
    })
    .filter((item) => item.text && item.text.length <= 2000);
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
  for (const item of getIncomingMessages()) {
    const key = `${getChatName()}::${item.text}`;
    if (seenMessages.has(key)) continue;
    seenMessages.add(key);
    if (seenMessages.size > 500) {
      const first = seenMessages.values().next().value;
      seenMessages.delete(first);
    }
    postToPdv(item.text).catch(() => {});
  }
}

setInterval(scan, 2500);
scan();
