using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using CheckBox = System.Windows.Controls.CheckBox;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private sealed class WhatsAppCatalogEntry
    {
        public int Number { get; set; }
        public string Code { get; set; } = "";
        public ProductTile Product { get; set; } = new();
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

    private void OpenWhatsAppConversation(WhatsAppMessageLog log, bool autoPressEnter)
    {
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
        if (clean is "CARDAPIO" or "MENU" or "CATALOGO")
        {
            return BuildWhatsAppMenuText();
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
                return "Nao encontrei pedido aguardando confirmacao. Envie os codigos do cardapio novamente.";
            }

            pending.Status = "CONFIRMADO";
            pending.ConfirmedAt = DateTime.Now;
            if (createOnConfirmation)
            {
                CreateDeliveryFromWhatsAppOrder(pending, selectOrder: false);
                return $"Pedido confirmado e enviado para o PDV. Total: {Money(pending.Total)}.";
            }

            SaveStore();
            return $"Pedido confirmado. Total: {Money(pending.Total)}.";
        }

        if (clean is "ALTERAR" or "MUDAR" or "NAO" or "NÃO")
        {
            if (pending is not null)
            {
                pending.Status = "ALTERACAO_SOLICITADA";
            }

            SaveStore();
            return "Tudo bem. Envie o pedido novamente usando os codigos do cardapio.";
        }

        var parsed = ParseWhatsAppOrderMessage(message, customerName, normalizedPhone);
        if (parsed.Items.Count == 0)
        {
            return $"{BuildWhatsAppOrderSummary(parsed)}\n\n{BuildWhatsAppMenuText()}";
        }

        WhatsAppPendingOrders.Insert(0, parsed);
        TrimWhatsAppPendingOrders();
        SaveStore();
        return BuildWhatsAppOrderSummary(parsed);
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
        var dialog = CreateDialog("WhatsApp Web", 860, 720);
        var enabledBox = new CheckBox { Content = "Abrir WhatsApp automaticamente ao finalizar venda/pedido", IsChecked = settings.Enabled };
        var enterBox = new CheckBox { Content = "Enviar automaticamente simulando Enter", IsChecked = settings.AutoPressEnter };
        var extensionHint = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold
        };
        var connectorBox = new CheckBox { Content = "Conector local para ler mensagens do WhatsApp Web", IsChecked = settings.LocalConnectorEnabled };
        var autoReplyBox = new CheckBox { Content = "Responder automaticamente pelo conector", IsChecked = settings.AutoReplyConnector };
        var autoCreateBox = new CheckBox { Content = "Criar pedido no PDV quando cliente responder SIM", IsChecked = settings.AutoCreateConfirmedOrders };
        var delayBox = new TextBox { Text = settings.SendDelaySeconds.ToString(Brazil) };
        var countryBox = new TextBox { Text = settings.DefaultCountryCode };
        var portBox = new TextBox { Text = settings.LocalConnectorPort.ToString(Brazil) };
        var menuBox = new TextBox
        {
            Text = BuildWhatsAppMenuText(),
            Height = 170,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var statusText = new TextBlock { Foreground = GreenText, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        var pendingList = new ListBox
        {
            DisplayMemberPath = nameof(WhatsAppPendingOrder.Display),
            ItemsSource = WhatsAppPendingOrders,
            MinHeight = 150
        };

        void RefreshExtensionState()
        {
            var installed = settings.ExtensionInstalledConfirmed;
            connectorBox.IsEnabled = installed;
            autoReplyBox.IsEnabled = installed && connectorBox.IsChecked == true;
            autoCreateBox.IsEnabled = installed && connectorBox.IsChecked == true;
            portBox.IsEnabled = installed && connectorBox.IsChecked == true;
            extensionHint.Foreground = installed ? GreenText : RedText;
            extensionHint.Text = installed
                ? "Conector instalado pelo PDV. Use o WhatsApp Web aberto por este botao para ler pedidos automaticamente."
                : "Clique em Instalar conector e abrir WhatsApp. O cliente final nao precisa abrir pasta nem mexer em chrome://extensions.";
        }
        var historyList = new ListBox
        {
            DisplayMemberPath = nameof(WhatsAppMessageLog.Display),
            ItemsSource = WhatsAppHistory,
            MinHeight = 150
        };

        var openWeb = DialogButton("Abrir WhatsApp Web", "#2F6FAE");
        openWeb.HorizontalAlignment = HorizontalAlignment.Stretch;
        openWeb.Width = double.NaN;
        openWeb.Click += (_, _) =>
        {
            Process.Start(new ProcessStartInfo("https://web.whatsapp.com") { UseShellExecute = true });
            statusText.Text = settings.ExtensionInstalledConfirmed
                ? "WhatsApp Web aberto. Deixe o cliente logado antes de finalizar pedidos."
                : "WhatsApp Web aberto sem conector. Para leitura automatica, use Instalar conector e abrir WhatsApp.";
        };

        var installConnector = DialogButton("Instalar conector e abrir WhatsApp", "#99620D");
        installConnector.HorizontalAlignment = HorizontalAlignment.Stretch;
        installConnector.Width = double.NaN;
        installConnector.Click += (_, _) =>
        {
            settings.Enabled = enabledBox.IsChecked == true;
            settings.AutoPressEnter = enterBox.IsChecked == true;
            settings.SendDelaySeconds = Math.Clamp(ParseInt(delayBox.Text, 8), 3, 30);
            settings.DefaultCountryCode = NormalizeCountryCode(countryBox.Text);
            settings.LocalConnectorPort = Math.Clamp(ParseInt(portBox.Text, 8787), 1024, 65535);
            var installed = TryInstallWhatsAppConnectorBrowser(settings, out var installMessage);
            statusText.Foreground = installed ? GreenText : RedText;
            statusText.Text = installMessage;
            RefreshExtensionState();
        };

        var resetConnector = DialogButton("Reinstalar conector", "#667684");
        resetConnector.HorizontalAlignment = HorizontalAlignment.Stretch;
        resetConnector.Width = double.NaN;
        resetConnector.Click += (_, _) =>
        {
            settings.ExtensionInstalledConfirmed = false;
            settings.LocalConnectorEnabled = false;
            _ = _whatsAppConnectorServer?.StopAsync();
            _whatsAppConnectorServer = null;
            SaveAppSettings();
            SaveStore();
            RefreshExtensionState();
            statusText.Foreground = AmberText;
            statusText.Text = "Conector resetado. Clique em Instalar conector e abrir WhatsApp para preparar de novo.";
        };

        void ApplyConnectorSettingsFromForm()
        {
            settings.Enabled = enabledBox.IsChecked == true;
            settings.AutoPressEnter = enterBox.IsChecked == true;
            settings.SendDelaySeconds = Math.Clamp(ParseInt(delayBox.Text, 8), 3, 30);
            settings.DefaultCountryCode = NormalizeCountryCode(countryBox.Text);
            settings.LocalConnectorEnabled = settings.ExtensionInstalledConfirmed && connectorBox.IsChecked == true;
            settings.LocalConnectorPort = Math.Clamp(ParseInt(portBox.Text, 8787), 1024, 65535);
            settings.AutoReplyConnector = autoReplyBox.IsChecked == true;
            settings.AutoCreateConfirmedOrders = autoCreateBox.IsChecked == true;
        }

        var save = DialogButton("Salvar configuracao", "#0F766E");
        save.HorizontalAlignment = HorizontalAlignment.Stretch;
        save.Width = double.NaN;
        save.Click += (_, _) =>
        {
            if (connectorBox.IsChecked == true && !settings.ExtensionInstalledConfirmed)
            {
                statusText.Foreground = RedText;
                statusText.Text = "Clique em Instalar conector e abrir WhatsApp antes de ligar leitura automatica.";
                SetStatus("WhatsApp: instale o conector pelo PDV antes de iniciar.");
                RefreshExtensionState();
                return;
            }

            ApplyConnectorSettingsFromForm();
            SaveAppSettings();
            SaveStore();
            EnsureWhatsAppConnectorServer();
            statusText.Foreground = GreenText;
            statusText.Text = "Configuracao de WhatsApp salva.";
            SetStatus("WhatsApp do cliente atualizado.");
        };

        connectorBox.Checked += (_, _) => RefreshExtensionState();
        connectorBox.Unchecked += (_, _) => RefreshExtensionState();

        var copyMenu = DialogButton("Copiar cardapio", "#2F6FAE");
        copyMenu.HorizontalAlignment = HorizontalAlignment.Stretch;
        copyMenu.Width = double.NaN;
        copyMenu.Click += (_, _) =>
        {
            menuBox.Text = BuildWhatsAppMenuText();
            System.Windows.Clipboard.SetText(menuBox.Text);
            statusText.Foreground = GreenText;
            statusText.Text = "Cardapio copiado.";
        };

        void ImportMessageManually()
        {
            var importDialog = CreateDialog("Importar pedido WhatsApp", 620, 680);
            var nameBox = new TextBox { Text = "CLIENTE WHATSAPP" };
            var phoneBox = new TextBox();
            var messageBox = new TextBox
            {
                Height = 180,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var previewBox = new TextBox
            {
                Height = 190,
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            WhatsAppPendingOrder? parsed = null;

            var parseButton = DialogButton("Ler mensagem", "#2F6FAE");
            parseButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            parseButton.Width = double.NaN;
            parseButton.Click += (_, _) =>
            {
                parsed = ParseWhatsAppOrderMessage(messageBox.Text, nameBox.Text, phoneBox.Text);
                previewBox.Text = BuildWhatsAppOrderSummary(parsed);
            };

            var createButton = DialogButton("Confirmar e lançar no PDV", "#0F766E");
            createButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            createButton.Width = double.NaN;
            createButton.Click += (_, _) =>
            {
                parsed ??= ParseWhatsAppOrderMessage(messageBox.Text, nameBox.Text, phoneBox.Text);
                if (parsed.Items.Count == 0)
                {
                    previewBox.Text = BuildWhatsAppOrderSummary(parsed);
                    return;
                }

                WhatsAppPendingOrders.Insert(0, parsed);
                CreateDeliveryFromWhatsAppOrder(parsed, selectOrder: true);
                pendingList.Items.Refresh();
                importDialog.Close();
            };

            var importPanel = DialogPanel();
            importPanel.Children.Add(DialogField("Cliente", nameBox));
            importPanel.Children.Add(DialogField("Telefone", phoneBox));
            importPanel.Children.Add(DialogField("Mensagem recebida", messageBox));
            importPanel.Children.Add(parseButton);
            importPanel.Children.Add(DialogField("Resumo / resposta", previewBox));
            importPanel.Children.Add(createButton);
            importDialog.Content = importPanel;
            importDialog.ShowDialog();
        }

        var importButton = DialogButton("Importar mensagem", "#0F766E");
        importButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        importButton.Width = double.NaN;
        importButton.Click += (_, _) => ImportMessageManually();

        var confirmPending = DialogButton("Lançar pendente selecionado", "#0F766E");
        confirmPending.HorizontalAlignment = HorizontalAlignment.Stretch;
        confirmPending.Width = double.NaN;
        confirmPending.Click += (_, _) =>
        {
            if (pendingList.SelectedItem is not WhatsAppPendingOrder order)
            {
                statusText.Foreground = RedText;
                statusText.Text = "Selecione um pedido pendente.";
                return;
            }

            CreateDeliveryFromWhatsAppOrder(order, selectOrder: true);
            pendingList.Items.Refresh();
            statusText.Foreground = GreenText;
            statusText.Text = $"Pedido {order.Id[..6]} enviado ao PDV.";
        };

        var resend = DialogButton("Reabrir selecionado", "#0F766E");
        resend.HorizontalAlignment = HorizontalAlignment.Stretch;
        resend.Width = double.NaN;
        resend.Click += (_, _) =>
        {
            if (historyList.SelectedItem is not WhatsAppMessageLog log)
            {
                statusText.Foreground = RedText;
                statusText.Text = "Selecione um item do historico.";
                return;
            }

            if (string.IsNullOrWhiteSpace(log.Phone))
            {
                statusText.Foreground = RedText;
                statusText.Text = "Historico sem telefone valido.";
                return;
            }

            statusText.Foreground = GreenText;
            statusText.Text = $"Reabrindo conversa de {log.CustomerName}.";
            OpenWhatsAppConversation(log, enterBox.IsChecked == true);
            historyList.Items.Refresh();
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogHint("Fluxo: o PDV abre web.whatsapp.com com telefone e mensagem pronta. Se o WhatsApp Web ja estiver logado, o Enter automatico tenta enviar a mensagem."));
        panel.Children.Add(enabledBox);
        panel.Children.Add(enterBox);
        panel.Children.Add(DialogField("Tempo antes do Enter automatico (3 a 30 segundos)", delayBox));
        panel.Children.Add(DialogField("DDI padrao para telefone sem pais", countryBox));
        panel.Children.Add(DialogLabel("Cardapio codificado"));
        panel.Children.Add(menuBox);
        panel.Children.Add(copyMenu);
        panel.Children.Add(importButton);
        panel.Children.Add(DialogLabel("Conector local"));
        panel.Children.Add(DialogHint("Clique em Instalar conector e abrir WhatsApp para o PDV preparar um navegador com a extensao carregada automaticamente."));
        panel.Children.Add(installConnector);
        panel.Children.Add(resetConnector);
        panel.Children.Add(extensionHint);
        panel.Children.Add(connectorBox);
        panel.Children.Add(autoReplyBox);
        panel.Children.Add(autoCreateBox);
        panel.Children.Add(DialogField("Porta local", portBox));
        panel.Children.Add(openWeb);
        panel.Children.Add(save);
        panel.Children.Add(DialogLabel("Pedidos pendentes"));
        panel.Children.Add(pendingList);
        panel.Children.Add(confirmPending);
        panel.Children.Add(DialogLabel("Historico"));
        panel.Children.Add(historyList);
        panel.Children.Add(resend);
        panel.Children.Add(statusText);
        dialog.Content = panel;
        RefreshExtensionState();
        dialog.ShowDialog();
    }

    private WhatsAppSettings GetWhatsAppSettings()
    {
        return _appSettings.WhatsApp ??= new WhatsAppSettings();
    }

    private void EnsureWhatsAppConnectorServer()
    {
        var settings = GetWhatsAppSettings();
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

    private bool TryInstallWhatsAppConnectorBrowser(WhatsAppSettings settings, out string message)
    {
        var extensionDir = FindWhatsAppExtensionDirectory();
        if (!Directory.Exists(extensionDir) || !File.Exists(Path.Combine(extensionDir, "manifest.json")))
        {
            message = "Nao encontrei os arquivos do conector WhatsApp no build do PDV.";
            return false;
        }

        var browserPath = FindChromiumBrowserExecutable();
        if (string.IsNullOrWhiteSpace(browserPath))
        {
            message = "Nao encontrei Chrome ou Edge instalado para carregar o conector.";
            return false;
        }

        settings.ExtensionInstalledConfirmed = true;
        settings.LocalConnectorEnabled = true;
        settings.AutoReplyConnector = true;
        settings.AutoCreateConfirmedOrders = true;
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

        Process.Start(new ProcessStartInfo
        {
            FileName = browserPath,
            Arguments = args,
            UseShellExecute = false
        });

        SetStatus("WhatsApp aberto com conector instalado pelo PDV.");
        message = "Conector instalado e WhatsApp aberto. No primeiro uso, escaneie o QR Code uma vez; depois essa sessao fica salva.";
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
