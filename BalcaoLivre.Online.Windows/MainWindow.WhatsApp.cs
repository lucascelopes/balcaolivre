using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Forms = System.Windows.Forms;
using CheckBox = System.Windows.Controls.CheckBox;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private const int WhatsAppConnectorPort = 8787;
    private Window? _whatsAppAutomationWindow;
    private WebView2? _whatsAppAutomationView;
    private TextBlock? _whatsAppAutomationStatusText;
    private string _lastWhatsAppAutomationStatus = "";

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
  let readyForNewMessages = false;
  let pendingUnreadOpen = null;

  function textOf(element) {
    return (element?.innerText || element?.textContent || "").replace(/\s+/g, " ").trim();
  }

  function plain(value) {
    return (value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
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
      ...row.querySelectorAll("span[aria-label], div[aria-label], span[data-icon], [data-testid]")
    ];
    return markers.find((marker) => {
      const label = plain(`${marker.getAttribute("aria-label") || ""} ${marker.getAttribute("data-icon") || ""} ${marker.getAttribute("data-testid") || ""}`);
      return label.includes("unread") || label.includes("nao lida") || label.includes("nao lidas") || label.includes("unread-count");
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

  function findUnreadChat() {
    const listRoots = [
      ...document.querySelectorAll("[aria-label*='Chat' i], [aria-label*='conversa' i], [aria-label*='Lista' i], [role='grid'], #pane-side, [data-testid='chat-list']")
    ];
    const rows = [
      ...listRoots.flatMap((root) => [
        ...root.querySelectorAll("[role='row'], [role='listitem'], [data-testid='cell-frame-container'], [tabindex]")
      ]),
      ...document.querySelectorAll("[data-testid='cell-frame-container'], #pane-side [tabindex]")
    ];

    for (const row of rows) {
      if (row.closest("footer") || row.closest("header")) continue;
      const marker = unreadMarkerOf(row);
      if (!marker) continue;
      if (row.closest(".message-in, .message-out")) continue;
      return { row, count: unreadCountOf(row, marker) };
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
    pendingUnreadOpen = { count: unread.count, clickedAt: Date.now() };
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
    let itemsToProcess = messages;
    if (processLatestCount > 0 && messages.length > processLatestCount) {
      rememberVisibleMessages(chatName, messages.slice(0, -processLatestCount));
      itemsToProcess = messages.slice(-processLatestCount);
    }

    for (const item of itemsToProcess) {
      const key = messageKey(chatName, item);
      if (seenMessages.has(key)) continue;
      rememberMessage(chatName, item);
      postToPdv(item.text);
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
      if (chatName && chatName !== "WhatsApp" && messages.length > 0) {
        processMessages(chatName, messages, 1);
        return;
      }
      rememberVisibleMessages(chatName, messages);
      return;
    }

    processMessages(chatName, messages);
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
    button?.click();
    const sent = Boolean(button);
    postStatus(sent ? "reply" : "error", sent ? "Resposta enviada no WhatsApp." : "Nao encontrei o botao enviar do WhatsApp.");
    return sent;
  }

  const pendingSendKey = "__balcaoLivrePendingSend";

  async function attemptPendingSend() {
    const raw = sessionStorage.getItem(pendingSendKey);
    if (!raw) return;

    let pending;
    try {
      pending = JSON.parse(raw);
    } catch {
      sessionStorage.removeItem(pendingSendKey);
      return;
    }

    if (!pending?.text || Date.now() - Number(pending.createdAt || 0) > 120000) {
      sessionStorage.removeItem(pendingSendKey);
      return;
    }

    const sent = await sendReplyText(pending.text);
    if (sent) {
      sessionStorage.removeItem(pendingSendKey);
      postStatus("reply", "Mensagem enviada no WhatsApp.");
    }
  }

  window.__balcaoLivreSendReply = async function(text) {
    return await sendReplyText(text);
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
    setTimeout(attemptPendingSend, 1800);
    return true;
  };

  function scan() {
    attemptPendingSend();

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
    if (!clickUnreadChat()) {
      const loggedIn = Boolean(document.querySelector("[aria-label*='Chat' i], [aria-label*='conversa' i], [role='grid'], footer div[contenteditable='true']"));
      postStatus(loggedIn ? "waiting" : "login", loggedIn ? "Aguardando mensagem nova no WhatsApp." : "Aguardando login/QR Code do WhatsApp.");
    }
  }

  setTimeout(() => {
    readyForNewMessages = true;
    const chatName = getChatName();
    rememberVisibleMessages(chatName, getIncomingMessages());
    initializedChats.add(chatName);
    postStatus("ready", "Leitor do Balcao Livre ativo no WhatsApp.");
  }, 1500);

  setInterval(scan, 2500);
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

        var message = BuildWhatsAppSaleMessage(context);
        var log = AddWhatsAppLog(context, phone, message, "ABRINDO", "");
        SaveStore();
        OpenWhatsAppConversation(log, settings.AutoPressEnter);
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
