using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Forms = System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ListBox = System.Windows.Controls.ListBox;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private const int WhatsAppConnectorPort = 8787;
    private static readonly HttpClient SendPulseWhatsAppHttp = new()
    {
        BaseAddress = new Uri("https://api.sendpulse.com/whatsapp/")
    };
    private Window? _whatsAppAutomationWindow;
    private WebView2? _whatsAppAutomationView;
    private TextBlock? _whatsAppAutomationStatusText;
    private string _lastWhatsAppAutomationStatus = "";
    private readonly Dictionary<string, DateTime> _whatsAppIncomingDedupe = new();
    private bool _sendPulseActivationRunning;

    private sealed class WhatsAppCatalogEntry
    {
        public int Number { get; set; }
        public string Code { get; set; } = "";
        public ProductTile Product { get; set; } = new();
    }

    private sealed class WhatsAppWebViewMessage
    {
        public string Type { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ChatId { get; set; } = "";
        public string Message { get; set; } = "";
        public string Status { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    private sealed record WhatsAppSaleContext(
        string CustomerName,
        string Phone,
        string BoardKind,
        string BoardNumber,
        decimal Total,
        string ReceiptPath,
        List<TicketLine> Lines,
        List<PaymentLine> Payments);

    private const string WhatsAppWebViewAutomationScript = """
(() => {
  if (window.__balcaoLivrePdvAtendimento) return;
  window.__balcaoLivrePdvAtendimento = true;

  const seenMessages = new Set();
  const initializedChats = new Set();
  const sentReplyKeys = new Map();
  let activeChatName = "";
  let readyForNewMessages = false;
  let pendingUnreadOpen = null;
  let pendingSendInProgress = false;
  const startupUnreadBaselines = new Map();
  let startupUnreadBaselineCaptured = false;

  function textOf(element) {
    return (element?.innerText || element?.textContent || "").replace(/\s+/g, " ").trim();
  }

  function plain(value) {
    return (value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
  }

  function isUnreadLabel(value) {
    const label = plain(value);
    return label.includes("unread")
      || label.includes("unread-count")
      || label.includes("nao lida")
      || label.includes("nao lidas")
      || label.includes("nao lido")
      || label.includes("nao lidos")
      || label.includes("nova mensagem")
      || label.includes("novas mensagens");
  }

  function postStatus(status, detail = "") {
    const key = `${status}::${detail}`;
    if (window.__balcaoLivreLastStatus === key && status !== "message") return;
    window.__balcaoLivreLastStatus = key;
    window.chrome?.webview?.postMessage({
      type: "status",
      status,
      detail
    });
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
      ...document.querySelectorAll("[data-testid='msg-container']"),
      ...document.querySelectorAll("div[data-id]")
    ];

    return [...new Set(nodes)]
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
    for (const item of messages) rememberMessage(chatName, item);
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

  function unreadCountOf(row, marker) {
    const labelNumber = (marker?.getAttribute("aria-label") || "").match(/\d+/)?.[0];
    if (labelNumber) return Math.max(1, Number(labelNumber));
    const markerNumber = textOf(marker).match(/^\d+$/)?.[0];
    if (markerNumber) return Math.max(1, Number(markerNumber));
    const smallNumbers = [...row.querySelectorAll("span")]
      .map((span) => textOf(span))
      .map((text) => text.match(/^\d{1,2}$/)?.[0])
      .filter(Boolean)
      .map(Number);
    return Math.max(1, smallNumbers.at(-1) || 1);
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
      if (!marker) continue;
      add(row, marker);
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
    const chatBefore = getChatName();
    const clickable =
      unread.row.querySelector("[role='gridcell']") ||
      unread.row.querySelector("[data-testid='cell-frame-container']") ||
      unread.row;
    clickable.scrollIntoView({ block: "center" });
    clickElement(clickable);
    pendingUnreadOpen = { count: Math.max(1, unread.count || 1), clickedAt: Date.now(), chatBefore, targetName: unread.name || "", targetKey: unread.key || "" };
    postStatus("chat", `Abrindo conversa com ${unread.count} mensagem(ns) nova(s).`);
    return true;
  }

  function postToPdv(message) {
    postStatus("message", `Mensagem recebida: ${message.slice(0, 80)}`);
    window.chrome?.webview?.postMessage({
      type: "message",
      customerName: getChatName(),
      chatId: getChatName(),
      phone: "",
      message
    });
  }

  function processMessages(chatName, messages, processLatestCount = 0) {
    const unseenItems = [];
    for (const item of messages) {
      const key = messageKey(chatName, item);
      if (!seenMessages.has(key)) unseenItems.push(item);
    }

    const limit = processLatestCount > 0 ? 1 : 1;
    const skippedItems = unseenItems.length > limit ? unseenItems.slice(0, -limit) : [];
    for (const item of skippedItems) {
      rememberMessage(chatName, item);
    }

    const itemsToProcess = unseenItems.slice(-limit);
    for (const item of itemsToProcess) {
      rememberMessage(chatName, item);
      postToPdv(item.text);
    }
  }

  function pruneSentReplyKeys() {
    const now = Date.now();
    for (const [key, sentAt] of sentReplyKeys) {
      if (now - sentAt > 90000) sentReplyKeys.delete(key);
    }
  }

  function replyKeyOf(chatName, text) {
    return `${chatName || "WhatsApp"}::${String(text || "").slice(0, 500)}`;
  }

  function wasReplyRecentlySent(chatName, text) {
    pruneSentReplyKeys();
    const key = replyKeyOf(chatName, text);
    const sentAt = sentReplyKeys.get(key);
    return Boolean(sentAt && Date.now() - sentAt < 90000);
  }

  function markReplySent(chatName, text) {
    pruneSentReplyKeys();
    sentReplyKeys.set(replyKeyOf(chatName, text), Date.now());
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

  function markCurrentChatVisibleAsSeen() {
    const chatName = getChatName();
    activeChatName = chatName;
    initializedChats.add(chatName);
    rememberVisibleMessages(chatName, getIncomingMessages());
  }

  function visible(element) {
    if (!element) return false;
    const rect = element.getBoundingClientRect();
    return rect.width > 0 && rect.height > 0;
  }

  function messageInput() {
    const candidates = [
      ...document.querySelectorAll("footer div[contenteditable='true'][role='textbox']"),
      ...document.querySelectorAll("footer div[contenteditable='true']"),
      ...document.querySelectorAll("div[contenteditable='true'][role='textbox']"),
      ...document.querySelectorAll("div[contenteditable='true'][data-tab]")
    ];
    return candidates.find(visible) || null;
  }

  function sendButton() {
    const candidates = [
      ...document.querySelectorAll("footer button[aria-label*='Send' i]"),
      ...document.querySelectorAll("footer button[aria-label*='Enviar' i]"),
      ...document.querySelectorAll("button[aria-label*='Send' i]"),
      ...document.querySelectorAll("button[aria-label*='Enviar' i]"),
      ...document.querySelectorAll("span[data-icon='send']")
    ];
    const element = candidates.find(visible);
    return element?.closest("button") || element || null;
  }

  async function writeMessage(text) {
    const editable =
      messageInput();
    if (!editable) {
      postStatus("error", "Nao encontrei o campo de mensagem do WhatsApp.");
      return false;
    }

    editable.focus();
    document.execCommand("selectAll", false, null);
    document.execCommand("insertText", false, text);
    editable.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "insertText", data: text }));
    await new Promise((resolve) => setTimeout(resolve, 450));
    return true;
  }

  async function sendReplyText(text) {
    if (!(await writeMessage(text))) return false;

    const button = sendButton();
    if (button) clickElement(button);
    const sent = Boolean(button);
    postStatus(sent ? "reply" : "error", sent ? "Resposta enviada no WhatsApp." : "Nao encontrei o botao enviar do WhatsApp.");
    return sent;
  }

  const pendingSendKey = "__balcaoLivrePendingSend";

  function readPendingSend() {
    const raw = sessionStorage.getItem(pendingSendKey);
    if (!raw) return null;

    let pending;
    try {
      pending = JSON.parse(raw);
    } catch {
      sessionStorage.removeItem(pendingSendKey);
      return null;
    }

    if (!pending?.text || Date.now() - Number(pending.createdAt || 0) > 600000) {
      sessionStorage.removeItem(pendingSendKey);
      return null;
    }

    return pending;
  }

  async function waitForValue(factory, timeoutMs = 22000, stepMs = 400) {
    const startedAt = Date.now();
    while (Date.now() - startedAt < timeoutMs) {
      const value = factory();
      if (value) return value;
      await new Promise((resolve) => setTimeout(resolve, stepMs));
    }

    return null;
  }

  async function attemptPendingSend() {
    if (pendingSendInProgress) return false;

    const pending = readPendingSend();
    if (!pending) return false;

    pendingSendInProgress = true;
    try {
      postStatus("waiting", "Aguardando o WhatsApp abrir a conversa para enviar.");
      const editable = await waitForValue(() => messageInput(), 22000, 500);
      if (!editable) {
        postStatus("waiting", "WhatsApp ainda carregando a conversa para envio automatico.");
        return false;
      }

      const sent = await sendReplyText(pending.text);
      if (sent) {
        sessionStorage.removeItem(pendingSendKey);
        postStatus("reply", "Mensagem enviada no WhatsApp.");
        return true;
      }

      return false;
    } finally {
      pendingSendInProgress = false;
    }
  }

  window.__balcaoLivreSendReply = async function(text) {
    const chatName = getChatName();
    if (wasReplyRecentlySent(chatName, text)) {
      postStatus("reply", "Resposta duplicada ignorada.");
      return true;
    }

    const sent = await sendReplyText(text);
    if (sent) markReplySent(chatName, text);
    return sent;
  };

  window.__balcaoLivreSendToPhone = async function(phone, text) {
    const cleanPhone = String(phone || "").replace(/\D/g, "");
    if (!cleanPhone || !text) {
      postStatus("error", "Telefone ou mensagem vazia para WhatsApp.");
      return false;
    }

    sessionStorage.setItem(pendingSendKey, JSON.stringify({
      phone: cleanPhone,
      text,
      createdAt: Date.now()
    }));
    location.href = `https://web.whatsapp.com/send?phone=${encodeURIComponent(cleanPhone)}&text=${encodeURIComponent(text)}`;
    return true;
  };

  function isWhatsAppLoggedIn() {
    return Boolean(document.querySelector("#pane-side, [data-testid='chat-list'], [aria-label*='Chat' i], [aria-label*='conversa' i], [role='grid'], footer div[contenteditable='true']"));
  }

  function armForNewMessages() {
    if (readyForNewMessages) return true;
    if (!isWhatsAppLoggedIn()) {
      postStatus("login", "Aguardando login/QR Code do WhatsApp.");
      return false;
    }

    captureStartupUnreadBaselines();
    markCurrentChatVisibleAsSeen();
    readyForNewMessages = true;
    postStatus("ready", "Leitor do Balcao Livre ativo no WhatsApp.");
    return true;
  }

  function scan() {
    if (readPendingSend()) {
      attemptPendingSend();
      return;
    }

    if (!armForNewMessages()) {
      return;
    }

    if (pendingUnreadOpen) {
      if (Date.now() - pendingUnreadOpen.clickedAt < 900) return;
      if (Date.now() - pendingUnreadOpen.clickedAt > 7000) {
        pendingUnreadOpen = null;
        markCurrentChatVisibleAsSeen();
        postStatus("waiting", "Nao consegui abrir automaticamente a conversa nao lida.");
        return;
      }

      if (pendingUnreadOpen.chatBefore
          && getChatName() === pendingUnreadOpen.chatBefore
          && Date.now() - pendingUnreadOpen.clickedAt < 2200) {
        return;
      }

      pendingUnreadOpen = null;
      activeChatName = getChatName();
      scanCurrentChat({ processLatestCount: 1 });
      return;
    }

    const chatName = getChatName();
    if (activeChatName && chatName !== activeChatName) {
      markCurrentChatVisibleAsSeen();
      postStatus("waiting", "Conversa aberta manualmente marcada como vista.");
      return;
    }

    scanCurrentChat();
    if (!clickUnreadChat()) {
      postStatus(isWhatsAppLoggedIn() ? "waiting" : "login", isWhatsAppLoggedIn() ? "Aguardando mensagem nova no WhatsApp." : "Aguardando login/QR Code do WhatsApp.");
    }
  }

  const observer = new MutationObserver(() => window.clearTimeout(window.__balcaoLivreScanSoon) || (window.__balcaoLivreScanSoon = window.setTimeout(scan, 300)));
  observer.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ["aria-label", "data-icon", "data-testid", "class"] });

  setTimeout(scan, 1500);
  setInterval(scan, 1800);
  scan();
})();
""";

    private void QueueWhatsAppReceipt(WhatsAppSaleContext? context)
    {
        var settings = GetWhatsAppSettings();
        if (!settings.Enabled || context is null)
        {
            return;
        }

        var phone = NormalizeWhatsAppPhone(context.Phone, settings.DefaultCountryCode);
        if (string.IsNullOrWhiteSpace(phone))
        {
            if (!string.IsNullOrWhiteSpace(context.CustomerName))
            {
                AddWhatsAppLog(context, "", BuildWhatsAppSaleMessage(context), "SEM_TELEFONE", "Cliente sem telefone valido.");
                SaveStore();
            }

            return;
        }

        var message = BuildSendPulseSaleMessage(context);
        var log = AddWhatsAppLog(context, phone, message, "ENVIANDO_WHATSAPP", "");
        SaveStore();
        _ = SendWhatsAppLogViaSendPulseAsync(log);
    }

    private WhatsAppSaleContext CreateWhatsAppSaleContext(
        TableTile board,
        List<TicketLine> lines,
        List<PaymentLine> payments,
        decimal total,
        string receiptPath)
    {
        return new WhatsAppSaleContext(
            board.CustomerName,
            board.Phone,
            board.Kind,
            board.Number,
            total,
            receiptPath,
            lines.Select(CloneLine).ToList(),
            payments.Select(ClonePayment).ToList());
    }

    private WhatsAppMessageLog AddWhatsAppLog(WhatsAppSaleContext context, string phone, string message, string status, string error)
    {
        var log = new WhatsAppMessageLog
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerName = string.IsNullOrWhiteSpace(context.CustomerName) ? "CLIENTE" : context.CustomerName,
            Phone = phone,
            BoardKind = context.BoardKind,
            BoardNumber = context.BoardNumber,
            Total = context.Total,
            Message = message,
            Status = status,
            Error = error,
            When = DateTime.Now
        };

        WhatsAppHistory.Insert(0, log);
        TrimWhatsAppHistory();
        return log;
    }

    private WhatsAppMessageLog AddWhatsAppInteractionLog(string customerName, string phone, string message, string status, string error = "", decimal total = 0)
    {
        var normalizedPhone = NormalizeWhatsAppPhone(phone, GetWhatsAppSettings().DefaultCountryCode);
        var log = new WhatsAppMessageLog
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? "CLIENTE WHATSAPP" : customerName.Trim(),
            Phone = normalizedPhone,
            BoardKind = "WHATSAPP",
            BoardNumber = "",
            Total = total,
            Message = message,
            Status = status,
            Error = error,
            When = DateTime.Now
        };

        WhatsAppHistory.Insert(0, log);
        TrimWhatsAppHistory();
        return log;
    }

    private void OpenWhatsAppConversation(WhatsAppMessageLog log, bool autoPressEnter)
    {
        if (_whatsAppAutomationView?.CoreWebView2 is not null)
        {
            _ = OpenWhatsAppConversationInAutomationAsync(log);
            return;
        }

        try
        {
            var uri = BuildWhatsAppWebUri(log.Phone, log.Message);
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            log.Status = "CONVERSA_ABERTA";
            log.OpenedAt = DateTime.Now;
            SaveStore();
            SetStatus($"WhatsApp Web aberto para {log.CustomerName}.");

            if (autoPressEnter)
            {
                _ = PressWhatsAppEnterAsync(log.Id, GetWhatsAppSettings().SendDelaySeconds);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            log.Status = "ERRO_ABRIR";
            log.Error = ex.Message;
            SaveStore();
            SetStatus($"Nao foi possivel abrir o WhatsApp Web: {ex.Message}");
        }
    }

    private async Task OpenWhatsAppConversationInAutomationAsync(WhatsAppMessageLog log)
    {
        try
        {
            var script = $"window.__balcaoLivreSendToPhone({JsonSerializer.Serialize(log.Phone, MainWindowJson.Options)}, {JsonSerializer.Serialize(log.Message, MainWindowJson.Options)});";
            var result = await _whatsAppAutomationView!.CoreWebView2.ExecuteScriptAsync(script);
            if (string.Equals(result, "true", StringComparison.OrdinalIgnoreCase))
            {
                log.Status = "ENVIANDO_WEBVIEW";
                log.OpenedAt = DateTime.Now;
                log.Error = "";
                SaveStore();
                SetStatus($"WhatsApp enviando pela janela do atendimento: {log.CustomerName}.");
                return;
            }

            log.Status = "ERRO_WEBVIEW";
            log.Error = "A janela do WhatsApp nao aceitou o envio automatico.";
            SaveStore();
            SetStatus(log.Error);
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException)
        {
            log.Status = "ERRO_WEBVIEW";
            log.Error = ex.Message;
            SaveStore();
            SetStatus($"Falha ao enviar pelo atendimento WhatsApp: {ex.Message}");
        }
    }

    private async Task PressWhatsAppEnterAsync(string logId, int delaySeconds)
    {
        var safeDelay = Math.Clamp(delaySeconds, 3, 30);
        await Task.Delay(TimeSpan.FromSeconds(safeDelay));
        await Dispatcher.InvokeAsync(() =>
        {
            var log = WhatsAppHistory.FirstOrDefault(item => item.Id == logId);
            if (log is null)
            {
                return;
            }

            try
            {
                Forms.SendKeys.SendWait("{ENTER}");
                Forms.SendKeys.Flush();
                log.Status = "ENTER_ENVIADO";
                log.SentAt = DateTime.Now;
                log.Error = "";
                SetStatus($"WhatsApp enviado por Enter para {log.CustomerName}.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                log.Status = "ERRO_ENTER";
                log.Error = ex.Message;
                SetStatus($"WhatsApp aberto, mas o Enter automatico falhou: {ex.Message}");
            }

            SaveStore();
        }, DispatcherPriority.Background);
    }

    private string BuildWhatsAppMenuText()
    {
        var catalog = BuildWhatsAppCatalog();
        if (catalog.Count == 0)
        {
            return "Cardapio indisponivel no momento. Nenhum produto ativo com estoque disponivel.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Cardapio");
        foreach (var group in catalog.GroupBy(item => item.Product.Category).OrderBy(group => group.Key))
        {
            sb.AppendLine();
            sb.AppendLine(group.Key);
            foreach (var item in group.OrderBy(item => item.Code))
            {
                sb.AppendLine($"{item.Code} ({item.Number}) - {item.Product.Name} - {Money(item.Product.Price)}");
            }
        }

        var first = catalog[0];
        var second = catalog.Skip(1).FirstOrDefault() ?? first;
        sb.AppendLine();
        sb.AppendLine("Para pedir, envie assim:");
        sb.AppendLine($"{first.Code} x2");
        if (!ReferenceEquals(second, first))
        {
            sb.AppendLine($"{second.Code} x1");
        }
        sb.AppendLine();
        sb.AppendLine($"Tambem pode usar o numero da lista, exemplo: {first.Number} x2.");
        sb.AppendLine("Produtos sem estoque nao aparecem aqui.");
        return sb.ToString();
    }

    private string BuildWhatsAppGreetingMenuText()
    {
        return $"{WhatsAppGreetingFor(DateTime.Now)}! Segue nosso cardapio:\n\n{BuildWhatsAppMenuText()}";
    }

    private static string WhatsAppGreetingFor(DateTime when)
    {
        return when.Hour switch
        {
            < 12 => "Bom dia",
            < 18 => "Boa tarde",
            _ => "Boa noite"
        };
    }

    private WhatsAppPendingOrder ParseWhatsAppOrderMessage(string message, string customerName, string phone)
    {
        var order = new WhatsAppPendingOrder
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? "CLIENTE WHATSAPP" : customerName.Trim().ToUpperInvariant(),
            Phone = NormalizeWhatsAppPhone(phone, GetWhatsAppSettings().DefaultCountryCode),
            ConversationKey = BuildWhatsAppConversationKey(customerName, phone, GetWhatsAppSettings().DefaultCountryCode),
            SourceMessage = message,
            CreatedAt = DateTime.Now
        };

        var catalog = BuildWhatsAppCatalog();
        var byCode = catalog
            .SelectMany(item => new[]
            {
                new { Key = NormalizeCatalogCode(item.Code), Item = item },
                new { Key = item.Number.ToString(Brazil), Item = item },
                new { Key = NormalizeCatalogCode(item.Product.Code), Item = item }
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First().Item);

        foreach (var rawLine in SplitWhatsAppOrderLines(message))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (TryReadPaymentMethod(line, out var payment))
            {
                order.PaymentMethod = payment;
                continue;
            }

            if (TryReadAddressLine(line, out var address))
            {
                order.Address = string.IsNullOrWhiteSpace(order.Address) ? address : $"{order.Address} {address}";
                continue;
            }

            if (TryParseCodeQuantity(line, byCode, out var entry, out var quantity))
            {
                AddPendingOrderItem(order, entry.Product, entry.Code, quantity);
                continue;
            }

            if (TryParseNameQuantity(line, catalog, out entry, out quantity))
            {
                AddPendingOrderItem(order, entry.Product, entry.Code, quantity);
            }
            else if (!IsLikelyNonProductLine(line))
            {
                order.Warnings.Add($"Nao entendi: {line}");
            }
        }

        order.Total = order.Items.Sum(item => item.Total);
        if (order.Items.Count == 0)
        {
            order.Status = "NAO_ENTENDIDO";
        }

        return order;
    }

    private string BuildWhatsAppOrderSummary(WhatsAppPendingOrder order)
    {
        if (order.Items.Count == 0)
        {
            var example = BuildWhatsAppCatalog().FirstOrDefault()?.Code ?? "1";
            return $"Nao consegui identificar os produtos. Envie usando os codigos do cardapio, exemplo: {example} x2.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Pedido encontrado");
        sb.AppendLine();
        foreach (var item in order.Items)
        {
            sb.AppendLine($"{item.Quantity}x {item.Name} - {Money(item.Total)}");
        }

        sb.AppendLine();
        sb.AppendLine($"Total: {Money(order.Total)}");
        if (!string.IsNullOrWhiteSpace(order.PaymentMethod))
        {
            sb.AppendLine($"Pagamento: {order.PaymentMethod}");
        }

        if (!string.IsNullOrWhiteSpace(order.Address))
        {
            sb.AppendLine($"Endereco: {order.Address}");
        }

        if (order.Warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Observacoes para conferir:");
            foreach (var warning in order.Warnings.Take(3))
            {
                sb.AppendLine($"- {warning}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Confirma o pedido? Responda SIM para confirmar ou ALTERAR para mudar.");
        return sb.ToString();
    }

    private string HandleWhatsAppIncomingMessage(string customerName, string phone, string message, bool createOnConfirmation)
    {
        var normalizedPhone = NormalizeWhatsAppPhone(phone, GetWhatsAppSettings().DefaultCountryCode);
        var conversationKey = BuildWhatsAppConversationKey(customerName, phone, GetWhatsAppSettings().DefaultCountryCode);
        var clean = NormalizeWhatsAppText(message);

        AddWhatsAppInteractionLog(customerName, normalizedPhone, message, "RECEBIDA");

        string Reply(string text, string status, decimal total = 0)
        {
            AddWhatsAppInteractionLog(customerName, normalizedPhone, text, status, total: total);
            SaveStore();
            return text;
        }

        if (IsWhatsAppMenuRequest(clean) || IsSimpleWhatsAppGreeting(clean))
        {
            return Reply(BuildWhatsAppGreetingMenuText(), "CARDAPIO_ENVIADO");
        }

        var pending = WhatsAppPendingOrders
            .Where(item => string.Equals(item.ConversationKey, conversationKey, StringComparison.Ordinal)
                && item.Status == "AGUARDANDO_CONFIRMACAO")
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        if (clean is "SIM" or "CONFIRMO" or "CONFIRMAR" or "OK" or "PODE")
        {
            if (pending is null)
            {
                return Reply("Nao encontrei pedido aguardando confirmacao.\n\n" + BuildWhatsAppGreetingMenuText(), "CARDAPIO_ENVIADO");
            }

            pending.Status = "CONFIRMADO";
            pending.ConfirmedAt = DateTime.Now;
            if (createOnConfirmation)
            {
                CreateDeliveryFromWhatsAppOrder(pending, selectOrder: false);
                return Reply($"Pedido confirmado e enviado para o PDV. Total: {Money(pending.Total)}.", "PEDIDO_CRIADO", pending.Total);
            }

            return Reply($"Pedido confirmado. Total: {Money(pending.Total)}.", "CONFIRMADO", pending.Total);
        }

        if (clean is "ALTERAR" or "MUDAR" or "NAO")
        {
            if (pending is not null)
            {
                pending.Status = "ALTERACAO_SOLICITADA";
            }

            return Reply("Tudo bem. Envie o pedido novamente usando os codigos do cardapio.", "ALTERACAO_SOLICITADA");
        }

        var parsed = ParseWhatsAppOrderMessage(message, customerName, normalizedPhone);
        if (parsed.Items.Count == 0)
        {
            if (pending is null)
            {
                return Reply(BuildWhatsAppGreetingMenuText(), "CARDAPIO_ENVIADO");
            }

            return Reply("Nao entendi sua resposta. Responda SIM para confirmar, ALTERAR para mudar, ou envie o pedido novamente usando os codigos do cardapio.", "AGUARDANDO_CONFIRMACAO", pending.Total);
        }

        WhatsAppPendingOrders.Insert(0, parsed);
        TrimWhatsAppPendingOrders();
        return Reply(BuildWhatsAppOrderSummary(parsed), "RESUMO_ENVIADO", parsed.Total);
    }

    private TableTile CreateDeliveryFromWhatsAppOrder(WhatsAppPendingOrder order, bool selectOrder)
    {
        var tile = new TableTile
        {
            Number = $"D{DeliveryTiles.Count + 1:00000}",
            Kind = "DELIVERY",
            Status = "NOVO",
            CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "CLIENTE WHATSAPP" : order.CustomerName,
            Phone = order.Phone,
            Address = order.Address,
            Detail = "WHATSAPP",
            ExternalSource = "WHATSAPP",
            ExternalOrderId = order.Id,
            Notes = string.IsNullOrWhiteSpace(order.PaymentMethod) ? "WhatsApp" : $"WhatsApp / Pagamento: {order.PaymentMethod}"
        };

        foreach (var item in order.Items)
        {
            tile.Lines.Add(new TicketLine
            {
                Code = item.ProductCode,
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Sector = item.Sector
            });
        }

        tile.Total = tile.Lines.Sum(line => line.Total);
        DeliveryTiles.Add(tile);
        order.Status = "ENVIADO_AO_PDV";
        order.ConfirmedAt ??= DateTime.Now;
        UpsertCustomerRecord("", tile.CustomerName, tile.Phone, tile.Address, "", tile.Notes);
        SaveStore();
        RefreshBoardForMode();
        if (selectOrder)
        {
            ModeList.SelectedItem = "Delivery";
            RefreshBoardForMode();
            SelectTable(BoardTiles.Count - 1, saveCurrent: false);
        }

        SetStatus($"Pedido WhatsApp criado no PDV: {tile.Number} {Money(tile.Total)}");
        return tile;
    }

    private List<WhatsAppCatalogEntry> BuildWhatsAppCatalog()
    {
        var categoryCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var number = 1;
        return Products
            .Where(product => product.Active && product.Price >= 0 && product.StockQuantity > 0)
            .OrderBy(product => product.Category)
            .ThenBy(product => product.Name)
            .Select(product =>
            {
                var categoryKey = string.IsNullOrWhiteSpace(product.Category) ? "GERAL" : product.Category;
                categoryCounters.TryGetValue(categoryKey, out var count);
                count++;
                categoryCounters[categoryKey] = count;
                var code = NormalizeCatalogCode(product.WhatsAppCode);
                if (string.IsNullOrWhiteSpace(code))
                {
                    code = AutoWhatsAppCode(product, categoryKey, count);
                }

                return new WhatsAppCatalogEntry
                {
                    Number = number++,
                    Code = code,
                    Product = product
                };
            })
            .ToList();
    }

    private static IEnumerable<string> SplitWhatsAppOrderLines(string message)
    {
        return (message ?? "")
            .Replace(";", "\n", StringComparison.Ordinal)
            .Replace(",", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool TryParseCodeQuantity(
        string line,
        Dictionary<string, WhatsAppCatalogEntry> byCode,
        out WhatsAppCatalogEntry entry,
        out int quantity)
    {
        entry = new WhatsAppCatalogEntry();
        quantity = 1;
        foreach (var pattern in new[]
        {
            @"(?i)^\s*(?<code>[A-Z]{1,4}\d{1,5}|\d{1,5})\s*(?:x|\*)\s*(?<qty>\d{1,3})\s*$",
            @"(?i)^\s*(?<qty>\d{1,3})\s*(?:x|\*)\s*(?<code>[A-Z]{1,4}\d{1,5}|\d{1,5})\s*$",
            @"(?i)^\s*(?<code>[A-Z]{1,4}\d{1,5})\s+(?<qty>\d{1,3})\s*$",
            @"(?i)^\s*(?<code>\d{1,5})\s*$"
        })
        {
            var match = Regex.Match(line, pattern);
            if (!match.Success)
            {
                continue;
            }

            var code = NormalizeCatalogCode(match.Groups["code"].Value);
            if (!byCode.TryGetValue(code, out var foundEntry))
            {
                continue;
            }

            entry = foundEntry;
            quantity = match.Groups["qty"].Success ? Math.Max(1, ParseInt(match.Groups["qty"].Value, 1)) : 1;
            return true;
        }

        return false;
    }

    private bool TryParseNameQuantity(string line, List<WhatsAppCatalogEntry> catalog, out WhatsAppCatalogEntry entry, out int quantity)
    {
        entry = new WhatsAppCatalogEntry();
        quantity = 1;
        line = Regex.Replace(line, @"(?i)^\s*(quero|queria|manda|mande|me ve|vou querer|pedido)\s+", "", RegexOptions.CultureInvariant);
        var match = Regex.Match(line, @"(?i)^\s*(?<qty>\d{1,3})\s*x?\s+(?<name>.+)$");
        var name = line;
        if (match.Success)
        {
            quantity = Math.Max(1, ParseInt(match.Groups["qty"].Value, 1));
            name = match.Groups["name"].Value;
        }

        return TryFindCatalogByName(name, catalog, out entry);
    }

    private static bool TryFindCatalogByName(string rawName, List<WhatsAppCatalogEntry> catalog, out WhatsAppCatalogEntry entry)
    {
        entry = new WhatsAppCatalogEntry();
        var needle = NormalizeWhatsAppText(rawName);
        if (string.IsNullOrWhiteSpace(needle))
        {
            return false;
        }

        var candidates = catalog.Select(item => new
            {
                Entry = item,
                Names = new[] { item.Product.Name, item.Product.Category }
                    .Concat((item.Product.WhatsAppAliases ?? "").Split(new[] { ',', ';', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    .Concat(new[] { item.Code, item.Product.Code })
                    .Select(NormalizeWhatsAppText)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList()
            })
            .ToList();

        var exact = candidates.FirstOrDefault(candidate =>
            candidate.Names.Any(name => name.Contains(needle, StringComparison.Ordinal) || needle.Contains(name, StringComparison.Ordinal)));
        if (exact is not null)
        {
            entry = exact.Entry;
            return true;
        }

        var fuzzy = candidates
            .Select(candidate => new
            {
                candidate.Entry,
                Distance = candidate.Names.Min(name => LevenshteinDistance(needle, name))
            })
            .OrderBy(candidate => candidate.Distance)
            .FirstOrDefault();

        if (fuzzy is not null && fuzzy.Distance <= Math.Max(2, needle.Length / 4))
        {
            entry = fuzzy.Entry;
            return true;
        }

        return false;
    }

    private static void AddPendingOrderItem(WhatsAppPendingOrder order, ProductTile product, string whatsAppCode, int quantity)
    {
        var existing = order.Items.FirstOrDefault(item => item.ProductCode == product.Code);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            order.Total = order.Items.Sum(item => item.Total);
            return;
        }

        order.Items.Add(new WhatsAppPendingOrderItem
        {
            ProductCode = product.Code,
            WhatsAppCode = whatsAppCode,
            Name = product.Name,
            Quantity = quantity,
            UnitPrice = product.Price,
            Sector = product.Sector
        });
        order.Total = order.Items.Sum(item => item.Total);
    }

    private static bool TryReadPaymentMethod(string line, out string payment)
    {
        var normalized = NormalizeWhatsAppText(line);
        payment = normalized switch
        {
            "PIX" => "PIX",
            "DINHEIRO" => "DINHEIRO",
            "CARTAO" or "CARTAO DEBITO" or "DEBITO" => "CARTAO DEBITO",
            "CREDITO" or "CARTAO CREDITO" => "CARTAO CREDITO",
            _ => ""
        };
        return !string.IsNullOrWhiteSpace(payment);
    }

    private static bool TryReadAddressLine(string line, out string address)
    {
        var normalized = NormalizeWhatsAppText(line);
        var looksLikeAddress = normalized.Contains("RUA", StringComparison.Ordinal)
            || normalized.Contains("AVENIDA", StringComparison.Ordinal)
            || normalized.Contains("AV ", StringComparison.Ordinal)
            || normalized.Contains("ENDERECO", StringComparison.Ordinal)
            || normalized.Contains("END", StringComparison.Ordinal);
        address = looksLikeAddress ? line.Trim() : "";
        return looksLikeAddress;
    }

    private static bool IsLikelyNonProductLine(string line)
    {
        var normalized = NormalizeWhatsAppText(line);
        return normalized.Length < 3
            || normalized.Contains("OBRIGADO", StringComparison.Ordinal)
            || normalized.Contains("BOA", StringComparison.Ordinal)
            || normalized.Contains("OLA", StringComparison.Ordinal);
    }

    private static bool IsWhatsAppMenuRequest(string clean)
    {
        return clean is "CARDAPIO" or "MENU" or "CATALOGO" or "VER CARDAPIO" or "MANDAR CARDAPIO" or "ENVIAR CARDAPIO";
    }

    private static bool IsSimpleWhatsAppGreeting(string clean)
    {
        return clean is "OI" or "OLA" or "BOM DIA" or "BOA TARDE" or "BOA NOITE" or "QUERO PEDIR" or "QUERO FAZER PEDIDO" or "PEDIDO";
    }

    private static string CategoryCodePrefix(string category)
    {
        var normalized = NormalizeWhatsAppText(category);
        return string.IsNullOrWhiteSpace(normalized) ? "P" : normalized[..1];
    }

    private static string AutoWhatsAppCode(ProductTile product, string category, int fallbackIndex)
    {
        var prefix = CategoryCodePrefix(category);
        var numericCode = new string((product.Code ?? "").Where(char.IsDigit).ToArray()).TrimStart('0');
        if (!string.IsNullOrWhiteSpace(numericCode))
        {
            return $"{prefix}{numericCode}";
        }

        return $"{prefix}{fallbackIndex}";
    }

    private static string NormalizeCatalogCode(string value)
    {
        return new string((value ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static string NormalizeWhatsAppText(string value)
    {
        var normalized = (value ?? "").Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : ' ');
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static int LevenshteinDistance(string left, string right)
    {
        if (left.Length == 0) return right.Length;
        if (right.Length == 0) return left.Length;
        var costs = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++) costs[j] = j;
        for (var i = 1; i <= left.Length; i++)
        {
            var previous = costs[0];
            costs[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var current = costs[j];
                costs[j] = Math.Min(Math.Min(costs[j] + 1, costs[j - 1] + 1), previous + (left[i - 1] == right[j - 1] ? 0 : 1));
                previous = current;
            }
        }

        return costs[right.Length];
    }

    private void ShowWhatsAppDialog()
    {
        ShowSimpleSendPulseWhatsAppDialog();
    }

    private void ShowSimpleSendPulseWhatsAppDialog()
    {
        if (!RequirePermission(user => IsCashUser(user) || CanOperateDelivery(user), "WhatsApp do cliente"))
        {
            return;
        }

        SaveActiveTicketToCurrentBoard();
        var settings = GetWhatsAppSettings();
        StopLegacyWhatsAppRuntime(settings);
        NormalizeWhatsAppSendPulseOnlySettings();
        SaveAppSettings();

        var dialog = CreateDialog("WhatsApp automatico", 760, 560);
        var phoneBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(settings.SendPulseStorePhone)
                ? NormalizeWhatsAppPhone(_profile.Phone, settings.DefaultCountryCode)
                : settings.SendPulseStorePhone
        };
        var title = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Solid("#18222B")
        };
        var status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var hint = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#667684"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        var badgeText = new TextBlock
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12
        };
        var badge = new Border
        {
            Child = badgeText,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4, 10, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var historyList = new ListBox
        {
            DisplayMemberPath = nameof(WhatsAppMessageLog.Display),
            ItemsSource = WhatsAppHistory,
            Height = 170
        };

        void SavePhone()
        {
            var nextPhone = NormalizeWhatsAppPhone(phoneBox.Text, settings.DefaultCountryCode);
            if (!string.Equals(settings.SendPulseStorePhone, nextPhone, StringComparison.Ordinal))
            {
                settings.SendPulseBotId = "";
                settings.SendPulseActivationPending = true;
            }

            settings.Enabled = true;
            settings.Provider = "META";
            settings.DefaultCountryCode = "55";
            settings.SendPulseStorePhone = nextPhone;
            NormalizeWhatsAppSendPulseOnlySettings();
            StopLegacyWhatsAppRuntime(settings);
            SaveAppSettings();
            SaveStore();
        }

        void RenderState(string? message = null, bool? ok = null)
        {
            var phone = NormalizeWhatsAppPhone(phoneBox.Text, settings.DefaultCountryCode);
            var active = settings.Enabled
                && !string.IsNullOrWhiteSpace(_appSettings.ActivationKey)
                && !string.IsNullOrWhiteSpace(settings.SendPulseBotId)
                && !settings.SendPulseActivationPending
                && !string.IsNullOrWhiteSpace(phone);
            title.Text = active ? "WhatsApp conectado" : "WhatsApp precisa conectar";
            badge.Background = Solid(active ? "#E8F7F4" : "#FFF2CB");
            badge.BorderBrush = Solid(active ? "#BDE5DD" : "#F7D87A");
            badge.BorderThickness = new Thickness(1);
            badgeText.Foreground = active ? GreenText : AmberText;
            badgeText.Text = active ? "ATIVO" : "PENDENTE";
            status.Foreground = ok.HasValue
                ? ok.Value ? GreenText : AmberText
                : active ? GreenText : Solid("#667684");
            status.Text = message ?? (active
                ? "Numero conectado na Meta. O PDV envia mensagens automaticas por esse WhatsApp."
                : "Informe o numero da loja e conecte pela Meta. Depois disso os scripts saem pelo WhatsApp desse restaurante.");
            hint.Text = active
                ? $"Numero da loja: {phone}. O operador nao precisa abrir WhatsApp Web."
                : "O navegador vai abrir a tela segura da Meta Business. O PDV nao mostra chave, token ou mensagens scriptadas para o usuario.";
        }

        var activate = DialogButton("Conectar numero", "#0F766E");
        activate.Click += async (_, _) =>
        {
            SavePhone();
            if (string.IsNullOrWhiteSpace(settings.SendPulseStorePhone))
            {
                phoneBox.Focus();
                RenderState("Informe o numero do WhatsApp da loja com DDD.", false);
                return;
            }

            activate.IsEnabled = false;
            RenderState("Conectando WhatsApp na Meta...", null);
            var result = await ActivateSendPulseStorePhoneAsync(settings, settings.SendPulseStorePhone);
            activate.IsEnabled = true;
            historyList.Items.Refresh();
            RenderState(result.Message, result.Ok);
            SetStatus(result.Message);
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { activate }
        };
        activate.Margin = new Thickness(0, 12, 0, 0);

        var statusCard = BorderCard();
        statusCard.Margin = new Thickness(0, 0, 0, 12);
        statusCard.Child = new StackPanel
        {
            Children = { title, badge, hint }
        };

        var phoneCard = BorderCard();
        phoneCard.Margin = new Thickness(0, 0, 0, 12);
        phoneCard.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Numero da loja",
                    Foreground = Solid("#18222B"),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                },
                DialogHint("Use o numero do WhatsApp Business que atende os clientes. Se ainda nao estiver conectado, o PDV abre a Meta para vincular esse numero."),
                DialogField("WhatsApp", phoneBox),
                actions,
                status
            }
        };

        var historyCard = BorderCard();
        historyCard.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Ultimos envios",
                    Foreground = Solid("#18222B"),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                },
                historyList
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(statusCard);
        panel.Children.Add(phoneCard);
        panel.Children.Add(historyCard);
        dialog.Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        RenderState();
        dialog.ShowDialog();
    }

    private void ShowSendPulseWhatsAppDialog()
    {
        if (!RequirePermission(user => IsCashUser(user) || CanOperateDelivery(user), "WhatsApp do cliente"))
        {
            return;
        }

        SaveActiveTicketToCurrentBoard();
        var settings = GetWhatsAppSettings();
        StopLegacyWhatsAppRuntime(settings);
        NormalizeWhatsAppSendPulseOnlySettings();
        SaveAppSettings();

        var board = CurrentBoard;
        var dialog = CreateDialog("WhatsApp SendPulse", 900, 720);
        var apiKeyBox = new PasswordBox
        {
            Password = settings.SendPulseApiKey,
            Height = 38
        };
        var botIdBox = new TextBox { Text = settings.SendPulseBotId };
        var customerBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(board?.CustomerName) ? "CLIENTE" : board.CustomerName
        };
        var phoneBox = new TextBox
        {
            Text = NormalizeWhatsAppPhone(board?.Phone ?? "", settings.DefaultCountryCode)
        };
        var saleClosedBox = ScriptBox(settings.SendPulseSaleClosedScript);
        var confirmedBox = ScriptBox(settings.SendPulseOrderConfirmedScript);
        var readyBox = ScriptBox(settings.SendPulseOrderReadyScript);
        var dispatchedBox = ScriptBox(settings.SendPulseOrderDispatchedScript);
        var scriptSelector = new ComboBox
        {
            ItemsSource = new[]
            {
                "Venda finalizada",
                "Pedido confirmado",
                "Pedido pronto",
                "Saiu para entrega"
            },
            SelectedIndex = 0
        };
        var messageBox = new TextBox
        {
            Height = 130,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var statusText = new TextBlock
        {
            Foreground = Solid("#667684"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var botsList = new ListBox
        {
            DisplayMemberPath = nameof(SendPulseBotOption.Display),
            Height = 92
        };
        var historyList = new ListBox
        {
            DisplayMemberPath = nameof(WhatsAppMessageLog.Display),
            ItemsSource = WhatsAppHistory,
            Height = 130
        };

        TextBox ScriptBox(string value) => new()
        {
            Text = value,
            Height = 92,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Grid TwoColumns(UIElement left, UIElement right)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            if (left is FrameworkElement leftElement)
            {
                leftElement.Margin = new Thickness(0, 0, 8, 0);
            }

            if (right is FrameworkElement rightElement)
            {
                rightElement.Margin = new Thickness(8, 0, 0, 0);
            }

            grid.Children.Add(left);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
            return grid;
        }

        StackPanel ButtonRow(params Button[] buttons)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            foreach (var button in buttons)
            {
                button.Margin = new Thickness(10, 12, 0, 0);
                row.Children.Add(button);
            }

            return row;
        }

        void SaveSendPulseSettingsFromInputs()
        {
            settings.Enabled = true;
            settings.Provider = "META";
            settings.DefaultCountryCode = "55";
            settings.SendPulseApiKey = NormalizeSendPulseApiKey(apiKeyBox.Password);
            settings.SendPulseBotId = botIdBox.Text.Trim();
            settings.SendPulseSaleClosedScript = saleClosedBox.Text.Trim();
            settings.SendPulseOrderConfirmedScript = confirmedBox.Text.Trim();
            settings.SendPulseOrderReadyScript = readyBox.Text.Trim();
            settings.SendPulseOrderDispatchedScript = dispatchedBox.Text.Trim();
            NormalizeWhatsAppSendPulseOnlySettings();
            StopLegacyWhatsAppRuntime(settings);
            SaveAppSettings();
        }

        string SelectedTemplate()
        {
            return scriptSelector.SelectedIndex switch
            {
                1 => confirmedBox.Text,
                2 => readyBox.Text,
                3 => dispatchedBox.Text,
                _ => saleClosedBox.Text
            };
        }

        void RefreshPreview()
        {
            SaveActiveTicketToCurrentBoard();
            messageBox.Text = BuildSendPulseMessageFromTemplate(
                SelectedTemplate(),
                customerBox.Text,
                phoneBox.Text,
                CurrentBoard);
        }

        var save = DialogButton("Salvar", "#0F766E");
        save.Click += (_, _) =>
        {
            SaveSendPulseSettingsFromInputs();
            statusText.Foreground = GreenText;
            statusText.Text = "Configuracao SendPulse salva. O modulo antigo de WhatsApp Web fica desligado.";
            SetStatus("WhatsApp SendPulse configurado.");
        };

        var fetchBots = DialogButton("Buscar bots", "#245B91");
        fetchBots.Click += async (_, _) =>
        {
            SaveSendPulseSettingsFromInputs();
            fetchBots.IsEnabled = false;
            statusText.Foreground = Solid("#667684");
            statusText.Text = "Consultando bots do WhatsApp na SendPulse...";
            var result = await FetchSendPulseBotsAsync(settings);
            fetchBots.IsEnabled = true;
            if (!result.Ok)
            {
                statusText.Foreground = RedText;
                statusText.Text = result.Message;
                return;
            }

            botsList.ItemsSource = result.Bots;
            if (result.Bots.Count > 0 && string.IsNullOrWhiteSpace(botIdBox.Text))
            {
                botIdBox.Text = result.Bots[0].Id;
                SaveSendPulseSettingsFromInputs();
            }

            statusText.Foreground = GreenText;
            statusText.Text = result.Bots.Count == 0
                ? "Nenhum bot WhatsApp encontrado nessa conta SendPulse."
                : $"Bots encontrados: {result.Bots.Count:N0}. Use o Bot ID do WhatsApp conectado.";
        };

        var applyScript = DialogButton("Aplicar script", "#245B91");
        applyScript.Click += (_, _) => RefreshPreview();
        scriptSelector.SelectionChanged += (_, _) => RefreshPreview();

        var sendTest = DialogButton("Enviar mensagem", "#0F766E");
        sendTest.Click += async (_, _) =>
        {
            SaveSendPulseSettingsFromInputs();
            var phone = NormalizeWhatsAppPhone(phoneBox.Text, settings.DefaultCountryCode);
            var message = messageBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                statusText.Foreground = RedText;
                statusText.Text = "Informe o telefone do cliente com DDD.";
                phoneBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                statusText.Foreground = RedText;
                statusText.Text = "Informe a mensagem antes de enviar.";
                messageBox.Focus();
                return;
            }

            var log = AddWhatsAppInteractionLog(customerBox.Text, phone, message, "ENVIANDO_WHATSAPP");
            SaveStore();
            historyList.Items.Refresh();
            sendTest.IsEnabled = false;
            statusText.Foreground = Solid("#667684");
            statusText.Text = "Enviando pela API SendPulse...";
            await SendWhatsAppLogViaSendPulseAsync(log);
            sendTest.IsEnabled = true;
            historyList.Items.Refresh();
            statusText.Foreground = string.IsNullOrWhiteSpace(log.Error) ? GreenText : RedText;
            statusText.Text = string.IsNullOrWhiteSpace(log.Error)
                ? "Mensagem enviada pela SendPulse."
                : log.Error;
        };

        RefreshPreview();

        var configCard = BorderCard();
        configCard.Margin = new Thickness(0, 0, 0, 12);
        configCard.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Conexao SendPulse",
                    Foreground = Solid("#18222B"),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                },
                DialogHint("Use API key da SendPulse e o Bot ID do WhatsApp conectado. O PDV nao abre WhatsApp Web e nao usa envio por navegador."),
                TwoColumns(DialogField("API key", apiKeyBox), DialogField("Bot ID WhatsApp", botIdBox)),
                botsList,
                ButtonRow(fetchBots, save)
            }
        };

        var scriptsCard = BorderCard();
        scriptsCard.Margin = new Thickness(0, 0, 0, 12);
        scriptsCard.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Mensagens scriptadas",
                    Foreground = Solid("#18222B"),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                },
                DialogHint("Tokens aceitos: {cliente}, {loja}, {pedido}, {total}, {itens}, {data}, {hora}."),
                DialogField("Venda finalizada", saleClosedBox),
                TwoColumns(DialogField("Pedido confirmado", confirmedBox), DialogField("Pedido pronto", readyBox)),
                DialogField("Saiu para entrega", dispatchedBox)
            }
        };

        var sendCard = BorderCard();
        sendCard.Margin = new Thickness(0, 0, 0, 12);
        sendCard.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Enviar para cliente",
                    Foreground = Solid("#18222B"),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                },
                TwoColumns(DialogField("Cliente", customerBox), DialogField("Telefone", phoneBox)),
                DialogField("Script", scriptSelector),
                DialogField("Mensagem", messageBox),
                ButtonRow(applyScript, sendTest),
                statusText
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(configCard);
        panel.Children.Add(scriptsCard);
        panel.Children.Add(sendCard);
        panel.Children.Add(DialogLabel("Historico"));
        panel.Children.Add(historyList);

        dialog.Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        dialog.ShowDialog();
    }

    private void ShowLegacyWhatsAppDialog()
    {
        if (!RequirePermission(user => IsCashUser(user) || CanOperateDelivery(user), "WhatsApp do cliente"))
        {
            return;
        }

        var settings = GetWhatsAppSettings();
        settings.DefaultCountryCode = "55";
        settings.LocalConnectorPort = WhatsAppConnectorPort;
        if (_whatsAppAutomationWindow is null)
        {
            settings.ExtensionInstalledConfirmed = false;
            settings.LocalConnectorEnabled = false;
            settings.ManagedBrowserProcessId = 0;
            _ = _whatsAppConnectorServer?.StopAsync();
            _whatsAppConnectorServer = null;
            SaveAppSettings();
        }

        var dialog = CreateDialog("Atendimento WhatsApp", 860, 700);
        var extensionHint = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold
        };
        var menuBox = new TextBox
        {
            Text = BuildWhatsAppGreetingMenuText(),
            Height = 210,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var statusText = new TextBlock
        {
            Foreground = _whatsAppAutomationWindow is null ? AmberText : GreenText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Text = _whatsAppAutomationWindow is null
                ? "Atendimento desligado. Clique em Ativar atendimento e abrir WhatsApp."
                : string.IsNullOrWhiteSpace(_lastWhatsAppAutomationStatus)
                    ? "Atendimento aberto. Deixe a janela do WhatsApp aberta."
                    : _lastWhatsAppAutomationStatus.Split(':', 2).LastOrDefault() ?? "Atendimento aberto."
        };
        var pendingList = new ListBox
        {
            DisplayMemberPath = nameof(WhatsAppPendingOrder.Display),
            ItemsSource = WhatsAppPendingOrders,
            MinHeight = 150
        };

        void RefreshExtensionState()
        {
            var active = _whatsAppAutomationWindow is not null
                && settings.ExtensionInstalledConfirmed
                && settings.LocalConnectorEnabled;
            extensionHint.Foreground = active ? GreenText : AmberText;
            extensionHint.Text = active
                ? "Atendimento ligado. O PDV responde mensagens novas com o cardapio do estoque e cria o pedido quando o cliente confirmar."
                : "Atendimento desligado. Clique no botao abaixo; o PDV abre o WhatsApp e prepara tudo sozinho.";
        }
        var historyList = new ListBox
        {
            DisplayMemberPath = nameof(WhatsAppMessageLog.Display),
            ItemsSource = WhatsAppHistory,
            MinHeight = 150
        };

        var installConnector = DialogButton("Ativar atendimento e abrir WhatsApp", "#99620D");
        installConnector.HorizontalAlignment = HorizontalAlignment.Stretch;
        installConnector.Width = double.NaN;
        installConnector.Click += async (_, _) =>
        {
            settings.DefaultCountryCode = "55";
            settings.LocalConnectorPort = WhatsAppConnectorPort;
            settings.AutoReplyConnector = true;
            settings.AutoCreateConfirmedOrders = true;
            statusText.Foreground = AmberText;
            statusText.Text = "Abrindo atendimento WhatsApp...";
            installConnector.IsEnabled = false;
            await OpenWhatsAppAutomationWindowAsync(statusText, RefreshExtensionState);
            installConnector.IsEnabled = true;
            RefreshExtensionState();
        };

        var resetConnector = DialogButton("Pausar atendimento", "#667684");
        resetConnector.HorizontalAlignment = HorizontalAlignment.Stretch;
        resetConnector.Width = double.NaN;
        resetConnector.Click += (_, _) =>
        {
            settings.ExtensionInstalledConfirmed = false;
            settings.LocalConnectorEnabled = false;
            _whatsAppAutomationWindow?.Close();
            CloseManagedWhatsAppBrowser(settings);
            _ = _whatsAppConnectorServer?.StopAsync();
            _whatsAppConnectorServer = null;
            SaveAppSettings();
            SaveStore();
            RefreshExtensionState();
            statusText.Foreground = AmberText;
            statusText.Text = "Atendimento pausado. Para voltar, clique em Ativar atendimento e abrir WhatsApp.";
        };

        var resetLogin = DialogButton("Resetar login WhatsApp", "#A11D1D");
        resetLogin.HorizontalAlignment = HorizontalAlignment.Stretch;
        resetLogin.Width = double.NaN;
        resetLogin.Click += (_, _) =>
        {
            settings.ExtensionInstalledConfirmed = false;
            settings.LocalConnectorEnabled = false;
            _whatsAppAutomationWindow?.Close();
            CloseManagedWhatsAppBrowser(settings);
            DeleteWhatsAppAutomationProfile();
            SaveAppSettings();
            SaveStore();
            RefreshExtensionState();
            statusText.Foreground = AmberText;
            statusText.Text = "Sessao do WhatsApp resetada. Clique em Ativar atendimento e escaneie o QR Code novamente.";
            SetStatus("Sessao do WhatsApp resetada.");
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogHint("Clique em Ativar atendimento. O PDV abre o WhatsApp, responde clientes novos com o cardapio do estoque e cria o pedido quando receber SIM."));
        panel.Children.Add(installConnector);
        panel.Children.Add(extensionHint);
        panel.Children.Add(resetConnector);
        panel.Children.Add(resetLogin);
        panel.Children.Add(DialogLabel("Cardapio do estoque"));
        panel.Children.Add(DialogHint("Esta previa usa os produtos ativos com estoque disponivel. O cliente recebe esse cardapio automaticamente quando chama no WhatsApp."));
        panel.Children.Add(menuBox);
        panel.Children.Add(DialogLabel("Pedidos pendentes"));
        panel.Children.Add(pendingList);
        panel.Children.Add(DialogLabel("Historico de mensagens"));
        panel.Children.Add(historyList);
        panel.Children.Add(statusText);
        dialog.Content = panel;
        RefreshExtensionState();
        dialog.ShowDialog();
    }

    private async Task<bool> OpenWhatsAppAutomationWindowAsync(TextBlock statusText, Action refreshState)
    {
        var settings = GetWhatsAppSettings();
        settings.ExtensionInstalledConfirmed = true;
        settings.LocalConnectorEnabled = true;
        settings.AutoReplyConnector = true;
        settings.AutoCreateConfirmedOrders = true;
        settings.DefaultCountryCode = "55";
        settings.LocalConnectorPort = WhatsAppConnectorPort;
        SaveAppSettings();
        SaveStore();
        _lastWhatsAppAutomationStatus = "";

        if (_whatsAppAutomationWindow is not null)
        {
            _whatsAppAutomationWindow.Activate();
            statusText.Foreground = GreenText;
            statusText.Text = "Atendimento WhatsApp ja esta aberto.";
            refreshState();
            return true;
        }

        var webView = new WebView2();
        var window = new Window
        {
            Title = "Atendimento WhatsApp - Balcao Livre PDV",
            Owner = this,
            Width = 1180,
            Height = 760,
            MinWidth = 920,
            MinHeight = 620,
            Content = webView
        };

        _whatsAppAutomationWindow = window;
        _whatsAppAutomationView = webView;
        _whatsAppAutomationStatusText = statusText;
        window.Closed += (_, _) =>
        {
            _whatsAppAutomationWindow = null;
            _whatsAppAutomationView = null;
            _whatsAppAutomationStatusText = null;
            settings.LocalConnectorEnabled = false;
            SaveAppSettings();
            SaveStore();
            Dispatcher.BeginInvoke(() =>
            {
                statusText.Foreground = AmberText;
                statusText.Text = "Atendimento fechado. Clique em Ativar atendimento para abrir de novo.";
                refreshState();
            }, DispatcherPriority.Background);
        };

        try
        {
            window.Show();
            var profileDir = Path.Combine(_dataRoot, "whatsapp-webview-profile");
            Directory.CreateDirectory(profileDir);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profileDir);
            await webView.EnsureCoreWebView2Async(environment);
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";
            webView.CoreWebView2.WebMessageReceived += WhatsAppAutomationMessageReceived;
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(WhatsAppWebViewAutomationScript);
            webView.CoreWebView2.NavigationCompleted += async (_, _) =>
            {
                try
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(WhatsAppWebViewAutomationScript);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"WhatsApp automation reinject failed: {ex.Message}");
                }
            };
            webView.CoreWebView2.Navigate("https://web.whatsapp.com");
            statusText.Foreground = GreenText;
            statusText.Text = "Atendimento aberto. Escaneie o QR Code se aparecer; depois deixe essa janela aberta.";
            refreshState();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or COMException)
        {
            _whatsAppAutomationWindow = null;
            _whatsAppAutomationView = null;
            settings.ExtensionInstalledConfirmed = false;
            settings.LocalConnectorEnabled = false;
            SaveAppSettings();
            SaveStore();
            try
            {
                window.Close();
            }
            catch (InvalidOperationException)
            {
            }

            statusText.Foreground = RedText;
            statusText.Text = $"Nao consegui abrir o atendimento WhatsApp: {ex.Message}";
            refreshState();
            return false;
        }
    }

    private async void WhatsAppAutomationMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        WhatsAppWebViewMessage? request;
        try
        {
            request = JsonSerializer.Deserialize<WhatsAppWebViewMessage>(e.WebMessageAsJson, MainWindowJson.Options);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"WhatsApp WebView message ignored: {ex.Message}");
            return;
        }

        if (request is null
            || string.IsNullOrWhiteSpace(request.Message))
        {
            if (request is not null && string.Equals(request.Type, "status", StringComparison.OrdinalIgnoreCase))
            {
                UpdateWhatsAppAutomationStatus(request.Status, request.Detail);
            }

            return;
        }

        if (string.Equals(request.Type, "status", StringComparison.OrdinalIgnoreCase))
        {
            UpdateWhatsAppAutomationStatus(request.Status, request.Detail);
            return;
        }

        if (!string.Equals(request.Type, "message", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (ShouldIgnoreDuplicateWhatsAppAutomationMessage(request))
        {
            UpdateWhatsAppAutomationStatus("waiting", "Mensagem duplicada ignorada pelo PDV.");
            return;
        }

        UpdateWhatsAppAutomationStatus("message", string.IsNullOrWhiteSpace(request.Detail) ? request.Message : request.Detail);
        var settings = GetWhatsAppSettings();
        var reply = HandleWhatsAppIncomingMessage(
            string.IsNullOrWhiteSpace(request.CustomerName) ? request.ChatId : request.CustomerName,
            request.Phone,
            request.Message,
            settings.AutoCreateConfirmedOrders);

        if (string.IsNullOrWhiteSpace(reply) || !settings.AutoReplyConnector || _whatsAppAutomationView?.CoreWebView2 is null)
        {
            return;
        }

        var script = $"window.__balcaoLivreSendReply({JsonSerializer.Serialize(reply, MainWindowJson.Options)});";
        try
        {
            var result = await _whatsAppAutomationView.CoreWebView2.ExecuteScriptAsync(script);
            if (!string.Equals(result, "true", StringComparison.OrdinalIgnoreCase))
            {
                UpdateWhatsAppAutomationStatus("error", "O PDV gerou a resposta, mas o WhatsApp nao confirmou o clique em enviar.");
            }
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"WhatsApp reply failed: {ex.Message}");
            UpdateWhatsAppAutomationStatus("error", $"Falha ao enviar resposta: {ex.Message}");
        }
    }

    private bool ShouldIgnoreDuplicateWhatsAppAutomationMessage(WhatsAppWebViewMessage request)
    {
        var now = DateTime.Now;
        var staleKeys = new List<string>();
        foreach (var item in _whatsAppIncomingDedupe)
        {
            if (now - item.Value > TimeSpan.FromMinutes(10))
            {
                staleKeys.Add(item.Key);
            }
        }

        foreach (var key in staleKeys)
        {
            _whatsAppIncomingDedupe.Remove(key);
        }

        var customerKey = NormalizeWhatsAppText(string.IsNullOrWhiteSpace(request.CustomerName) ? request.ChatId : request.CustomerName);
        var phoneKey = NormalizeWhatsAppText(request.Phone);
        var messageKey = NormalizeWhatsAppText(request.Message);
        var dedupeKey = $"{customerKey}|{phoneKey}|{messageKey}";
        if (_whatsAppIncomingDedupe.TryGetValue(dedupeKey, out var lastSeen)
            && now - lastSeen < TimeSpan.FromSeconds(45))
        {
            return true;
        }

        _whatsAppIncomingDedupe[dedupeKey] = now;
        return false;
    }

    private void UpdateWhatsAppAutomationStatus(string status, string detail)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "status" : status.Trim().ToLowerInvariant();
        var text = normalized switch
        {
            "ready" => "Leitor ativo no WhatsApp.",
            "login" => "Aguardando QR Code/login do WhatsApp.",
            "waiting" => "Aguardando mensagem nova no WhatsApp.",
            "chat" => detail,
            "message" => detail,
            "reply" => detail,
            "error" => detail,
            _ => string.IsNullOrWhiteSpace(detail) ? normalized : detail
        };

        if (string.IsNullOrWhiteSpace(text) || string.Equals(_lastWhatsAppAutomationStatus, $"{normalized}:{text}", StringComparison.Ordinal))
        {
            return;
        }

        _lastWhatsAppAutomationStatus = $"{normalized}:{text}";
        var brush = normalized switch
        {
            "error" => RedText,
            "login" or "waiting" => AmberText,
            _ => GreenText
        };

        Dispatcher.BeginInvoke(() =>
        {
            if (_whatsAppAutomationStatusText is not null)
            {
                _whatsAppAutomationStatusText.Foreground = brush;
                _whatsAppAutomationStatusText.Text = text;
            }

            SetStatus($"WhatsApp: {text}");
        }, DispatcherPriority.Background);
    }

    private void DeleteWhatsAppAutomationProfile()
    {
        var profileDir = Path.Combine(_dataRoot, "whatsapp-webview-profile");
        try
        {
            if (Directory.Exists(profileDir))
            {
                Directory.Delete(profileDir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"WhatsApp profile reset skipped: {ex.Message}");
            SetStatus($"Nao consegui apagar sessao antiga do WhatsApp: {ex.Message}");
        }
    }

    private WhatsAppSettings GetWhatsAppSettings()
    {
        return _appSettings.WhatsApp ??= new WhatsAppSettings();
    }

    private sealed class SendPulseBotOption
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsActive { get; set; }
        public string Display => string.IsNullOrWhiteSpace(Name)
            ? $"{Id}  {Status}".Trim()
            : $"{Name}  |  {Phone}  |  {Id}  {Status}".Trim();
    }

    private sealed class SendPulseBotsResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public List<SendPulseBotOption> Bots { get; set; } = [];

        public static SendPulseBotsResult Fail(string message) => new() { Ok = false, Message = message };
        public static SendPulseBotsResult OkResult(List<SendPulseBotOption> bots) => new() { Ok = true, Bots = bots };
    }

    private sealed class SendPulseActivationResult
    {
        public bool Ok { get; set; }
        public bool NeedsConnection { get; set; }
        public string Message { get; set; } = "";
        public string OnboardingUrl { get; set; } = "";

        public static SendPulseActivationResult Success(string message) => new() { Ok = true, Message = message };
        public static SendPulseActivationResult Fail(string message, bool needsConnection = false, string onboardingUrl = "") => new()
        {
            Ok = false,
            NeedsConnection = needsConnection,
            Message = message,
            OnboardingUrl = onboardingUrl
        };
    }

    private bool NormalizeWhatsAppSendPulseOnlySettings()
    {
        var settings = GetWhatsAppSettings();
        var changed = false;

        if (!settings.Enabled)
        {
            settings.Enabled = true;
            changed = true;
        }

        if (!string.Equals(settings.Provider, "META", StringComparison.Ordinal))
        {
            settings.Provider = "META";
            changed = true;
        }

        if (!string.Equals(settings.DefaultCountryCode, "55", StringComparison.Ordinal))
        {
            settings.DefaultCountryCode = "55";
            changed = true;
        }

        if (settings.AutoPressEnter)
        {
            settings.AutoPressEnter = false;
            changed = true;
        }

        if (settings.ExtensionInstalledConfirmed)
        {
            settings.ExtensionInstalledConfirmed = false;
            changed = true;
        }

        if (settings.LocalConnectorEnabled)
        {
            settings.LocalConnectorEnabled = false;
            changed = true;
        }

        if (settings.AutoReplyConnector)
        {
            settings.AutoReplyConnector = false;
            changed = true;
        }

        if (settings.AutoCreateConfirmedOrders)
        {
            settings.AutoCreateConfirmedOrders = false;
            changed = true;
        }

        if (settings.ManagedBrowserProcessId != 0)
        {
            settings.ManagedBrowserProcessId = 0;
            changed = true;
        }

        if (settings.LocalConnectorPort != WhatsAppConnectorPort)
        {
            settings.LocalConnectorPort = WhatsAppConnectorPort;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(settings.SendPulseSaleClosedScript))
        {
            settings.SendPulseSaleClosedScript = DefaultSendPulseScript("SALE_CLOSED");
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.SendPulseOrderConfirmedScript))
        {
            settings.SendPulseOrderConfirmedScript = DefaultSendPulseScript("ORDER_CONFIRMED");
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.SendPulseOrderReadyScript))
        {
            settings.SendPulseOrderReadyScript = DefaultSendPulseScript("ORDER_READY");
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.SendPulseOrderDispatchedScript))
        {
            settings.SendPulseOrderDispatchedScript = DefaultSendPulseScript("ORDER_DISPATCHED");
            changed = true;
        }

        return changed;
    }

    private static void MergeWhatsAppSendPulseSettings(WhatsAppSettings? source, WhatsAppSettings target)
    {
        if (source is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseApiKey))
        {
            target.SendPulseApiKey = source.SendPulseApiKey;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseBotId))
        {
            target.SendPulseBotId = source.SendPulseBotId;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseStorePhone))
        {
            target.SendPulseStorePhone = source.SendPulseStorePhone;
        }

        if (source.SendPulseLastActivationAt.HasValue)
        {
            target.SendPulseLastActivationAt = source.SendPulseLastActivationAt;
        }

        if (source.SendPulseActivationPending)
        {
            target.SendPulseActivationPending = true;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseSaleClosedScript))
        {
            target.SendPulseSaleClosedScript = source.SendPulseSaleClosedScript;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseOrderConfirmedScript))
        {
            target.SendPulseOrderConfirmedScript = source.SendPulseOrderConfirmedScript;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseOrderReadyScript))
        {
            target.SendPulseOrderReadyScript = source.SendPulseOrderReadyScript;
        }

        if (!string.IsNullOrWhiteSpace(source.SendPulseOrderDispatchedScript))
        {
            target.SendPulseOrderDispatchedScript = source.SendPulseOrderDispatchedScript;
        }
    }

    private void StopLegacyWhatsAppRuntime(WhatsAppSettings settings)
    {
        settings.Provider = "META";
        settings.AutoPressEnter = false;
        settings.ExtensionInstalledConfirmed = false;
        settings.LocalConnectorEnabled = false;
        settings.AutoReplyConnector = false;
        settings.AutoCreateConfirmedOrders = false;
        CloseManagedWhatsAppBrowser(settings);
        _ = _whatsAppConnectorServer?.StopAsync();
        _whatsAppConnectorServer = null;
        if (_whatsAppAutomationWindow is not null)
        {
            try
            {
                _whatsAppAutomationWindow.Close();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private static string DefaultSendPulseScript(string kind)
    {
        return kind switch
        {
            "ORDER_CONFIRMED" => "Ola, {cliente}. Recebemos seu pedido {pedido} no {loja}. Total: {total}. Ja estamos preparando.",
            "ORDER_READY" => "Ola, {cliente}. Seu pedido {pedido} esta pronto no {loja}.",
            "ORDER_DISPATCHED" => "Ola, {cliente}. Seu pedido {pedido} saiu para entrega. Total: {total}.",
            _ => "Ola, {cliente}.\nSeu pedido {pedido} foi finalizado no {loja}.\nTotal: {total}\n\nItens:\n{itens}\n\nObrigado pela preferencia."
        };
    }

    private string BuildSendPulseSaleMessage(WhatsAppSaleContext context)
    {
        return ApplySendPulseScriptTokens(
            GetWhatsAppSettings().SendPulseSaleClosedScript,
            context.CustomerName,
            context.Phone,
            context.BoardKind,
            context.BoardNumber,
            context.Total,
            context.Lines);
    }

    private string BuildSendPulseMessageFromTemplate(string template, string customerName, string phone, TableTile? board)
    {
        var lines = board?.Lines ?? new List<TicketLine>();
        var total = board?.Total ?? lines.Sum(line => line.Total);
        return ApplySendPulseScriptTokens(
            template,
            customerName,
            phone,
            board?.Kind ?? "PEDIDO",
            board?.Number ?? "",
            total,
            lines);
    }

    private string ApplySendPulseScriptTokens(
        string template,
        string customerName,
        string phone,
        string boardKind,
        string boardNumber,
        decimal total,
        IEnumerable<TicketLine> lines)
    {
        var business = string.IsNullOrWhiteSpace(_profile.BusinessName) ? AppReceiptName : _profile.BusinessName.Trim();
        var customer = string.IsNullOrWhiteSpace(customerName) ? "cliente" : customerName.Trim();
        var order = $"{boardKind} {boardNumber}".Trim();
        if (string.IsNullOrWhiteSpace(order))
        {
            order = "pedido";
        }

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cliente"] = customer,
            ["loja"] = business,
            ["pedido"] = order,
            ["telefone"] = NormalizeWhatsAppPhone(phone, "55"),
            ["total"] = Money(total),
            ["itens"] = BuildSendPulseItemsText(lines),
            ["data"] = DateTime.Now.ToString("dd/MM/yyyy", Brazil),
            ["hora"] = DateTime.Now.ToString("HH:mm", Brazil)
        };

        var text = string.IsNullOrWhiteSpace(template) ? DefaultSendPulseScript("SALE_CLOSED") : template;
        foreach (var item in replacements)
        {
            text = Regex.Replace(
                text,
                "\\{" + Regex.Escape(item.Key) + "\\}",
                item.Value.Replace("$", "$$", StringComparison.Ordinal),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return text.Trim();
    }

    private string BuildSendPulseItemsText(IEnumerable<TicketLine> lines)
    {
        var visibleLines = lines
            .Where(line => !IsTableCharge(line))
            .Take(12)
            .ToList();
        if (visibleLines.Count == 0)
        {
            return "Itens nao informados";
        }

        var sb = new StringBuilder();
        foreach (var line in visibleLines)
        {
            sb.AppendLine($"{line.Quantity}x {line.Name} - {Money(line.Total)}");
        }

        return sb.ToString().Trim();
    }

    private async Task<SendPulseBotsResult> FetchSendPulseBotsAsync(WhatsAppSettings settings)
    {
        if (!TryValidateSendPulseSettings(settings, requireBotId: false, out var error))
        {
            return SendPulseBotsResult.Fail(error);
        }

        try
        {
            using var request = CreateSendPulseRequest(HttpMethod.Get, "bots", settings);
            using var response = await SendPulseWhatsAppHttp.SendAsync(request).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return SendPulseBotsResult.Fail(ExtractSendPulseError(response.StatusCode, body));
            }

            return SendPulseBotsResult.OkResult(ParseSendPulseBots(body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return SendPulseBotsResult.Fail($"Falha ao consultar SendPulse: {ex.Message}");
        }
    }

    private async Task<SendPulseActivationResult> ActivateSendPulseStorePhoneAsync(WhatsAppSettings settings, string storePhone)
    {
        var phone = NormalizeWhatsAppPhone(storePhone, settings.DefaultCountryCode);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return SendPulseActivationResult.Fail("Informe o numero do WhatsApp da loja com DDD.");
        }

        settings.SendPulseStorePhone = phone;
        if (string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            await SavePendingSendPulseActivationAsync(settings).ConfigureAwait(false);
            return SendPulseActivationResult.Fail("Ative a licenca antes de liberar WhatsApp automatico.", needsConnection: true);
        }

        var endpoint = BuildWhatsAppFunctionUri("/activate");
        if (endpoint is null)
        {
            await SavePendingSendPulseActivationAsync(settings).ConfigureAwait(false);
            return SendPulseActivationResult.Fail("URL do WhatsApp no Supabase invalida.", needsConnection: true);
        }

        var basePayload = CreateAdminClientPayload("whatsapp.activate", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        var payload = new AdminWhatsAppActivationPayload
        {
            EventName = basePayload.EventName,
            LicenseKey = basePayload.LicenseKey,
            MachineHash = basePayload.MachineHash,
            MachineCode = basePayload.MachineCode,
            AppVersion = basePayload.AppVersion,
            LocalExpiresAt = basePayload.LocalExpiresAt,
            LocalPlan = basePayload.LocalPlan,
            Profile = basePayload.Profile,
            Settings = basePayload.Settings,
            Metrics = basePayload.Metrics,
            StorePhone = phone,
            LocalWhen = DateTimeOffset.Now
        };

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<AdminWhatsAppResult>(json, JsonOptions);
            if (result is null)
            {
                await SavePendingSendPulseActivationAsync(settings).ConfigureAwait(false);
                return SendPulseActivationResult.Fail(response.IsSuccessStatusCode
                    ? "Supabase nao retornou o status do WhatsApp."
                    : "Supabase recusou o WhatsApp, mas nao retornou detalhes.", needsConnection: true);
            }

            settings.SendPulseStorePhone = string.IsNullOrWhiteSpace(result.StorePhone) ? phone : result.StorePhone;
            settings.Enabled = true;
            settings.Provider = "META";
            settings.SendPulseLastActivationAt = DateTime.Now;
            if (result.Ok)
            {
                settings.SendPulseBotId = "META";
                settings.SendPulseActivationPending = false;
            }
            else
            {
                settings.SendPulseBotId = "";
                settings.SendPulseActivationPending = true;
                if (!string.IsNullOrWhiteSpace(result.OnboardingUrl))
                {
                    OpenWhatsAppOnboardingUrl(result.OnboardingUrl);
                }
            }

            NormalizeWhatsAppSendPulseOnlySettings();
            await Dispatcher.InvokeAsync(() =>
            {
                SaveAppSettings();
                SaveStore();
            }, DispatcherPriority.Background);

            var message = string.IsNullOrWhiteSpace(result.Message)
                ? result.Ok ? "WhatsApp automatico ativado para esse numero." : "Numero salvo. A ativacao automatica ficou pendente."
                : result.Message.Trim();
            return result.Ok
                ? SendPulseActivationResult.Success(message)
                : SendPulseActivationResult.Fail(message, needsConnection: result.Pending, onboardingUrl: result.OnboardingUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
        {
            await SavePendingSendPulseActivationAsync(settings).ConfigureAwait(false);
            Debug.WriteLine($"Supabase WhatsApp activation failed: {ex.Message}");
            return SendPulseActivationResult.Fail("Supabase indisponivel agora. O numero ficou salvo e sera tentado novamente.", needsConnection: true);
        }
    }

    private void OpenWhatsAppOnboardingUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            SetStatus("Abra a tela da Meta, conecte o WhatsApp e depois clique de novo em Conectar numero.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            SetStatus($"Nao consegui abrir a conexao Meta: {ex.Message}");
        }
    }

    private async Task SavePendingSendPulseActivationAsync(WhatsAppSettings settings)
    {
        settings.Enabled = true;
        settings.Provider = "META";
        settings.SendPulseBotId = "";
        settings.SendPulseActivationPending = true;
        settings.SendPulseLastActivationAt = DateTime.Now;
        NormalizeWhatsAppSendPulseOnlySettings();
        await Dispatcher.InvokeAsync(() =>
        {
            SaveAppSettings();
            SaveStore();
        }, DispatcherPriority.Background);
    }

    private async Task TryAutoActivatePendingSendPulseAsync()
    {
        var settings = GetWhatsAppSettings();
        if (_sendPulseActivationRunning
            || !settings.SendPulseActivationPending
            || string.IsNullOrWhiteSpace(settings.SendPulseStorePhone)
            || string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            return;
        }

        _sendPulseActivationRunning = true;
        try
        {
            var result = await ActivateSendPulseStorePhoneAsync(settings, settings.SendPulseStorePhone);
            if (result.Ok)
            {
                SetStatus(result.Message);
            }
        }
        finally
        {
            _sendPulseActivationRunning = false;
        }
    }

    private static SendPulseBotOption? FindSendPulseBotForPhone(IEnumerable<SendPulseBotOption> bots, string phone)
    {
        return bots
            .Where(bot => !string.IsNullOrWhiteSpace(bot.Phone))
            .FirstOrDefault(bot => PhoneMatchesBot(phone, bot.Phone));
    }

    private static bool PhoneMatchesBot(string phone, string botPhone)
    {
        var left = new string((phone ?? "").Where(char.IsDigit).ToArray());
        var right = new string((botPhone ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(left, right, StringComparison.Ordinal)
            || left.EndsWith(right, StringComparison.Ordinal)
            || right.EndsWith(left, StringComparison.Ordinal);
    }

    private async Task SendWhatsAppLogViaSendPulseAsync(WhatsAppMessageLog log)
    {
        var settings = GetWhatsAppSettings();
        if (!TryValidateSendPulseSettings(settings, requireBotId: false, out var validationError))
        {
            await UpdateSendPulseLogAsync(log, false, validationError).ConfigureAwait(false);
            return;
        }

        var phone = NormalizeWhatsAppPhone(log.Phone, settings.DefaultCountryCode);
        if (string.IsNullOrWhiteSpace(phone))
        {
            await UpdateSendPulseLogAsync(log, false, "Cliente sem telefone valido para WhatsApp.").ConfigureAwait(false);
            return;
        }

        var endpoint = BuildWhatsAppFunctionUri("/send");
        if (endpoint is null)
        {
            await UpdateSendPulseLogAsync(log, false, "URL do WhatsApp no Supabase invalida.").ConfigureAwait(false);
            return;
        }

        var basePayload = CreateAdminClientPayload("whatsapp.send", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        var payload = new AdminWhatsAppSendPayload
        {
            EventName = basePayload.EventName,
            LicenseKey = basePayload.LicenseKey,
            MachineHash = basePayload.MachineHash,
            MachineCode = basePayload.MachineCode,
            AppVersion = basePayload.AppVersion,
            LocalExpiresAt = basePayload.LocalExpiresAt,
            LocalPlan = basePayload.LocalPlan,
            Profile = basePayload.Profile,
            Settings = basePayload.Settings,
            Metrics = basePayload.Metrics,
            StorePhone = settings.SendPulseStorePhone,
            CustomerName = log.CustomerName,
            CustomerPhone = phone,
            Message = log.Message,
            BoardKind = log.BoardKind,
            BoardNumber = log.BoardNumber,
            Total = log.Total,
            LocalWhen = DateTimeOffset.Now
        };

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<AdminWhatsAppResult>(json, JsonOptions);
            if (result is null)
            {
                await UpdateSendPulseLogAsync(log, false, response.IsSuccessStatusCode
                    ? "Supabase nao retornou o status do WhatsApp."
                    : "Supabase recusou o envio do WhatsApp.").ConfigureAwait(false);
                return;
            }

            if (!response.IsSuccessStatusCode || !result.Ok)
            {
                if (result.Pending)
                {
                    settings.SendPulseActivationPending = true;
                    settings.SendPulseBotId = "";
                    await Dispatcher.InvokeAsync(SaveAppSettings, DispatcherPriority.Background);
                }

                await UpdateSendPulseLogAsync(log, false, result.Message).ConfigureAwait(false);
                return;
            }

            settings.SendPulseBotId = "META";
            settings.SendPulseActivationPending = false;
            settings.SendPulseStorePhone = string.IsNullOrWhiteSpace(result.StorePhone) ? settings.SendPulseStorePhone : result.StorePhone;
            await Dispatcher.InvokeAsync(SaveAppSettings, DispatcherPriority.Background);
            await UpdateSendPulseLogAsync(log, true, "").ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            await UpdateSendPulseLogAsync(log, false, $"Falha ao enviar pelo Supabase: {ex.Message}").ConfigureAwait(false);
        }
    }

    private async Task UpdateSendPulseLogAsync(WhatsAppMessageLog log, bool sent, string error)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            log.Status = sent ? "ENVIADO_WHATSAPP" : "ERRO_WHATSAPP";
            log.Error = error;
            log.SentAt = sent ? DateTime.Now : null;
            SaveStore();
            SetStatus(sent
                ? $"WhatsApp enviado para {log.CustomerName}."
                : $"WhatsApp falhou: {error}");
        }, DispatcherPriority.Background);
    }

    private bool TryValidateSendPulseSettings(WhatsAppSettings settings, bool requireBotId, out string error)
    {
        if (string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            error = "Ative a licenca antes de usar WhatsApp automatico.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.SendPulseStorePhone))
        {
            error = "Ative o numero da loja no modulo WhatsApp.";
            return false;
        }

        if (requireBotId && string.IsNullOrWhiteSpace(settings.SendPulseBotId))
        {
            error = "Ative o WhatsApp automatico antes de enviar mensagens.";
            return false;
        }

        error = "";
        return true;
    }

    private static HttpRequestMessage CreateSendPulseRequest(HttpMethod method, string path, WhatsAppSettings settings, object? payload = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeSendPulseApiKey(settings.SendPulseApiKey));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (payload is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(payload, MainWindowJson.Options), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string NormalizeSendPulseApiKey(string value)
    {
        var key = (value ?? "").Trim();
        return key.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? key[7..].Trim() : key;
    }

    private static List<SendPulseBotOption> ParseSendPulseBots(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return data.EnumerateArray()
            .Select(item => new SendPulseBotOption
            {
                Id = FirstJsonText(item, "id", "bot_id"),
                Name = item.TryGetProperty("channel_data", out var channelData)
                    ? FirstJsonText(channelData, "name", "title")
                    : FirstJsonText(item, "name", "title"),
                Phone = item.TryGetProperty("channel_data", out channelData)
                    ? FirstJsonText(channelData, "phone")
                    : FirstJsonText(item, "phone"),
                Status = FirstJsonText(item, "status"),
                IsActive = FirstJsonText(item, "status") is "3" or "active" or "ACTIVE"
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .ToList();
    }

    private static bool IsSendPulseFailure(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.False;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ExtractSendPulseError(HttpStatusCode statusCode, string body)
    {
        var fallback = $"SendPulse retornou {(int)statusCode} {statusCode}.";
        if (string.IsNullOrWhiteSpace(body))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var message = FirstJsonText(root, "message", "error_description", "error");
            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            if (root.TryGetProperty("data", out var data))
            {
                message = data.ValueKind == JsonValueKind.String
                    ? data.GetString() ?? ""
                    : FirstJsonText(data, "message", "error", "description");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch (JsonException)
        {
        }

        var compact = Regex.Replace(body, @"\s+", " ").Trim();
        return compact.Length > 260 ? compact[..260] : compact;
    }

    private static string FirstJsonText(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "",
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => ""
            };
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return "";
    }

    private void ResetWhatsAppRuntimeState()
    {
        var settings = GetWhatsAppSettings();
        settings.ExtensionInstalledConfirmed = false;
        settings.LocalConnectorEnabled = false;
        settings.ManagedBrowserProcessId = 0;
        settings.LocalConnectorPort = WhatsAppConnectorPort;
    }

    private void EnsureWhatsAppConnectorServer()
    {
        var settings = GetWhatsAppSettings();
        if (NormalizeWhatsAppSendPulseOnlySettings())
        {
            SaveAppSettings();
        }

        if (string.Equals(settings.Provider, "META", StringComparison.OrdinalIgnoreCase))
        {
            _ = _whatsAppConnectorServer?.StopAsync();
            _whatsAppConnectorServer = null;
            return;
        }

        settings.LocalConnectorPort = WhatsAppConnectorPort;
        if (!settings.ExtensionInstalledConfirmed || !settings.LocalConnectorEnabled)
        {
            _ = _whatsAppConnectorServer?.StopAsync();
            _whatsAppConnectorServer = null;
            return;
        }

        if (_whatsAppConnectorServer is { Port: var port } && port == settings.LocalConnectorPort)
        {
            return;
        }

        _ = _whatsAppConnectorServer?.StopAsync();
        _whatsAppConnectorServer = new WhatsAppLocalConnectorServer(settings.LocalConnectorPort);
        try
        {
            _ = _whatsAppConnectorServer.StartAsync(async request =>
            {
                return await Dispatcher.InvokeAsync(() =>
                {
                    var currentSettings = GetWhatsAppSettings();
                    var reply = HandleWhatsAppIncomingMessage(
                        string.IsNullOrWhiteSpace(request.CustomerName) ? request.ChatId : request.CustomerName,
                        request.Phone,
                        request.Message,
                        currentSettings.AutoCreateConfirmedOrders);
                    return new WhatsAppConnectorResponse
                    {
                        Ok = true,
                        Reply = currentSettings.AutoReplyConnector ? reply : "",
                        AutoReply = currentSettings.AutoReplyConnector
                    };
                }, DispatcherPriority.Background);
            });
        }
        catch (Exception ex) when (ex is HttpListenerException or InvalidOperationException)
        {
            Debug.WriteLine($"WhatsApp connector failed: {ex.Message}");
            _whatsAppConnectorServer = null;
        }
    }

    private static void CloseManagedWhatsAppBrowser(WhatsAppSettings settings)
    {
        if (settings.ManagedBrowserProcessId <= 0)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(settings.ManagedBrowserProcessId);
            var processName = process.ProcessName ?? "";
            if (!processName.Contains("chrome", StringComparison.OrdinalIgnoreCase)
                && !processName.Contains("msedge", StringComparison.OrdinalIgnoreCase))
            {
                settings.ManagedBrowserProcessId = 0;
                return;
            }

            if (!process.HasExited)
            {
                if (!process.CloseMainWindow() || !process.WaitForExit(1500))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(1500);
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            Debug.WriteLine($"Managed WhatsApp browser close skipped: {ex.Message}");
        }
        finally
        {
            settings.ManagedBrowserProcessId = 0;
        }
    }

    private bool TryInstallWhatsAppConnectorBrowser(WhatsAppSettings settings, out string message)
    {
        var extensionDir = FindWhatsAppExtensionDirectory();
        if (!Directory.Exists(extensionDir) || !File.Exists(Path.Combine(extensionDir, "manifest.json")))
        {
            message = "Nao encontrei os arquivos do atendimento WhatsApp no build do PDV.";
            return false;
        }

        var browserPath = FindChromiumBrowserExecutable();
        if (string.IsNullOrWhiteSpace(browserPath))
        {
            message = "Nao encontrei Chrome ou Edge instalado para abrir o atendimento WhatsApp.";
            return false;
        }

        settings.ExtensionInstalledConfirmed = true;
        settings.LocalConnectorEnabled = true;
        settings.LocalConnectorPort = WhatsAppConnectorPort;
        settings.AutoReplyConnector = true;
        settings.AutoCreateConfirmedOrders = true;
        CloseManagedWhatsAppBrowser(settings);
        SaveAppSettings();
        SaveStore();
        EnsureWhatsAppConnectorServer();

        var profileDir = Path.Combine(_dataRoot, "whatsapp-browser-profile");
        Directory.CreateDirectory(profileDir);
        var args = string.Join(" ",
            QuoteArg($"--user-data-dir={profileDir}"),
            QuoteArg($"--disable-extensions-except={extensionDir}"),
            QuoteArg($"--load-extension={extensionDir}"),
            "--no-first-run",
            "--new-window",
            QuoteArg("https://web.whatsapp.com"));

        var browserProcess = Process.Start(new ProcessStartInfo
        {
            FileName = browserPath,
            Arguments = args,
            UseShellExecute = false
        });

        settings.ManagedBrowserProcessId = browserProcess?.Id ?? 0;
        SaveAppSettings();
        SaveStore();

        SetStatus("Atendimento WhatsApp aberto pelo PDV.");
        message = "Atendimento ativado e WhatsApp aberto. No primeiro uso, escaneie o QR Code uma vez; depois essa sessao fica salva.";
        return true;
    }

    private static string? FindChromiumBrowserExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string QuoteArg(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static string FindWhatsAppExtensionDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && current is not null; i++, current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, "BalcaoLivre.WhatsAppExtension");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "BalcaoLivre.WhatsAppExtension");
    }

    private static string BuildWhatsAppWebUri(string phone, string message)
    {
        return $"https://web.whatsapp.com/send?phone={Uri.EscapeDataString(phone)}&text={Uri.EscapeDataString(message)}";
    }

    private string BuildWhatsAppSaleMessage(WhatsAppSaleContext context)
    {
        var business = string.IsNullOrWhiteSpace(_profile.BusinessName) ? AppReceiptName : _profile.BusinessName.Trim();
        var customer = string.IsNullOrWhiteSpace(context.CustomerName) ? "cliente" : context.CustomerName.Trim();
        var sb = new StringBuilder();
        sb.AppendLine($"Ola, {customer}.");
        sb.AppendLine($"Seu pedido {context.BoardKind} {context.BoardNumber} foi finalizado no {business}.");
        sb.AppendLine($"Total: {Money(context.Total)}");

        if (context.Payments.Count > 0)
        {
            sb.AppendLine($"Pagamento: {string.Join(", ", context.Payments.GroupBy(item => item.Method).Select(group => $"{group.Key} {Money(group.Sum(item => item.Amount))}"))}");
        }

        var visibleLines = context.Lines.Where(line => !IsTableCharge(line)).Take(12).ToList();
        if (visibleLines.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Itens:");
            foreach (var line in visibleLines)
            {
                sb.AppendLine($"- {line.Quantity}x {line.Name} - {Money(line.Total)}");
            }

            var hidden = context.Lines.Count(line => !IsTableCharge(line)) - visibleLines.Count;
            if (hidden > 0)
            {
                sb.AppendLine($"+ {hidden:N0} item(ns)");
            }
        }

        sb.AppendLine();
        sb.Append("Obrigado pela preferencia.");
        return sb.ToString();
    }

    private static string NormalizeWhatsAppPhone(string rawPhone, string defaultCountryCode)
    {
        var source = (rawPhone ?? "").Trim();
        var codeIndex = source.IndexOf(" cod", StringComparison.OrdinalIgnoreCase);
        if (codeIndex >= 0)
        {
            source = source[..codeIndex];
        }

        var digits = new string(source.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        var country = NormalizeCountryCode(defaultCountryCode);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return "";
        }

        if (digits.StartsWith(country, StringComparison.Ordinal) && digits.Length >= country.Length + 8)
        {
            return digits;
        }

        if (digits.Length is 10 or 11)
        {
            return country + digits;
        }

        return digits.Length >= 8 ? digits : "";
    }

    private static string BuildWhatsAppConversationKey(string customerName, string phone, string defaultCountryCode)
    {
        var normalizedPhone = NormalizeWhatsAppPhone(phone, defaultCountryCode);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return normalizedPhone;
        }

        var normalizedName = NormalizeWhatsAppText(customerName);
        return string.IsNullOrWhiteSpace(normalizedName) ? "CHAT-DESCONHECIDO" : $"CHAT-{normalizedName}";
    }

    private static string NormalizeCountryCode(string value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? "55" : digits;
    }

    private void TrimWhatsAppHistory()
    {
        while (WhatsAppHistory.Count > 500)
        {
            WhatsAppHistory.RemoveAt(WhatsAppHistory.Count - 1);
        }
    }

    private void TrimWhatsAppPendingOrders()
    {
        while (WhatsAppPendingOrders.Count > 100)
        {
            WhatsAppPendingOrders.RemoveAt(WhatsAppPendingOrders.Count - 1);
        }
    }
}
