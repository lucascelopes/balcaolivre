using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BalcaoLivre.Online.Windows;

public sealed class MobileBridgeSessionDto
{
    public string LicenseKey { get; set; } = "";
    public string LoginEmail { get; set; } = "";
    public string OperatorName { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Responsible { get; set; } = "";
    public string Document { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Address { get; set; } = "";
    public string AdminApiUrl { get; set; } = "";
    public bool CashOpen { get; set; }
    public bool OnlineStoreOpen { get; set; }
}

public sealed class WhatsAppBrainLearningExample
{
    public string Input { get; set; } = "";
    public string Response { get; set; } = "";
    public string Intent { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class EvolutionChatConversation
{
    public string Phone { get; set; } = "";
    public string Name { get; set; } = "";
    public string LastMessage { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class EvolutionChatLine
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime When { get; set; } = DateTime.Now;
    public bool FromMe { get; set; }
}

internal sealed class EvolutionSendResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
}

internal sealed class RestaurantNfceIssueResult
{
    public bool Attempted { get; init; }
    public bool Ok { get; init; }
    public string StatusCode { get; init; } = "";
    public string StatusMessage { get; init; } = "";
}

internal sealed class PdvOperationalRule
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
    public string Warning { get; init; } = "";
}

internal static class PdvOperationalCore
{
    public static PdvOperationalRule ValidateProductAdd(
        MainWindow.TableTile? board,
        MainWindow.ProductTile product,
        int quantity,
        bool cashOpen)
    {
        if (board is null)
        {
            return new PdvOperationalRule { Message = "Selecione uma mesa, ficha ou entrega." };
        }

        if (!product.Active)
        {
            return new PdvOperationalRule { Message = "Produto inativo." };
        }

        if (quantity <= 0)
        {
            return new PdvOperationalRule { Message = "Quantidade invalida." };
        }

        return new PdvOperationalRule { Ok = true };
    }

    public static PdvOperationalRule ValidateReceive(
        MainWindow.TableTile? board,
        decimal total,
        decimal paidTotal,
        bool cashOpen)
    {
        if (!cashOpen)
        {
            return new PdvOperationalRule { Message = "Caixa fechado." };
        }

        if (total <= 0 || paidTotal > total)
        {
            return new PdvOperationalRule { Message = "Valores do recebimento invalidos." };
        }

        return new PdvOperationalRule { Ok = true };
    }

    public static void NormalizeStore(MainWindow.AppStore store)
    {
        store.Tables ??= [];
        store.DeliveryTiles ??= [];
        store.KitchenTiles ??= [];
        store.Products ??= [];
        store.Customers ??= [];
        store.WhatsAppHistory ??= [];
        store.WhatsAppPendingOrders ??= [];
    }
}

public partial class MainWindow
{
    private static bool IsEvolutionLocalWhatsApp(WhatsAppSettings settings)
    {
        return string.Equals(settings.Provider, "EVOLUTION", StringComparison.OrdinalIgnoreCase)
               || string.Equals(settings.Provider, "EVOLUTION_LOCAL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateEvolutionLocalSettings(WhatsAppSettings settings, out string message)
    {
        if (!Uri.TryCreate(settings.EvolutionLocalBaseUrl, UriKind.Absolute, out _))
        {
            message = "URL da Evolution invalida.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.EvolutionLocalInstanceName))
        {
            message = "Nome da instancia Evolution nao informado.";
            return false;
        }

        message = "";
        return true;
    }

    private async Task<EvolutionSendResult> SendEvolutionLocalTextAsync(
        WhatsAppSettings settings,
        string phone,
        string text)
    {
        if (!TryValidateEvolutionLocalSettings(settings, out var validation))
        {
            return new EvolutionSendResult { Message = validation };
        }

        if (settings.EvolutionLocalBaseUrl.Contains("evolution-proxy", StringComparison.OrdinalIgnoreCase))
        {
            return new EvolutionSendResult
            {
                Message = "Proxy Evolution sem contrato de envio nesta build. Configure a URL direta da Evolution."
            };
        }

        try
        {
            var endpoint =
                $"{settings.EvolutionLocalBaseUrl.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(settings.EvolutionLocalInstanceName.Trim())}";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (!string.IsNullOrWhiteSpace(settings.EvolutionLocalApiKey))
            {
                request.Headers.TryAddWithoutValidation("apikey", settings.EvolutionLocalApiKey.Trim());
            }

            var payload = new { number = phone, text, delay = 800, linkPreview = false };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode
                ? new EvolutionSendResult { Ok = true, Message = "Mensagem enviada." }
                : new EvolutionSendResult { Message = $"Evolution respondeu HTTP {(int)response.StatusCode}." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new EvolutionSendResult { Message = ex.Message };
        }
    }

    private Task<List<EvolutionChatConversation>> FetchEvolutionLocalChatsAsync(WhatsAppSettings settings)
    {
        return Task.FromResult(new List<EvolutionChatConversation>());
    }

    private Task<List<EvolutionChatLine>> FetchEvolutionLocalChatMessagesAsync(
        WhatsAppSettings settings,
        EvolutionChatConversation chat)
    {
        return Task.FromResult(new List<EvolutionChatLine>());
    }

    private async Task SendWhatsAppLogViaEvolutionLocalAsync(WhatsAppMessageLog log)
    {
        var settings = GetWhatsAppSettings();
        var phone = NormalizeWhatsAppPhone(log.Phone, settings.DefaultCountryCode);
        var result = await SendEvolutionLocalTextAsync(settings, phone, log.Message);
        await Dispatcher.InvokeAsync(() =>
        {
            log.LastAttemptAt = DateTime.Now;
            log.SendAttempts++;
            log.Status = result.Ok ? "ENVIADA" : "ERRO";
            log.Error = result.Ok ? "" : result.Message;
            if (result.Ok)
            {
                log.SentAt = DateTime.Now;
            }

            SaveStore();
        });
    }

    private Task PollEvolutionLocalMessagesAsync() => Task.CompletedTask;

    private Task ProcessWhatsAppSendQueueAsync() => Task.CompletedTask;

    private void StartWhatsAppSendQueueIfNeeded()
    {
        var settings = GetWhatsAppSettings();
        if (!settings.Enabled)
        {
            return;
        }

        _whatsAppSendQueueTimer.Start();
        if (IsEvolutionLocalWhatsApp(settings) && settings.EvolutionLocalAutoReceiveEnabled)
        {
            _whatsAppEvolutionPollTimer.Start();
        }
    }

    private void QueuePublicMenuOrderWhatsAppStatusNotification(TableTile order)
    {
        // O status permanece salvo no pedido; o envio depende do provedor configurado.
    }

    private static string FormatWhatsAppDisplayPhone(string phone)
    {
        var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 13 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            return $"+55 ({digits.Substring(2, 2)}) {digits.Substring(4, 5)}-{digits.Substring(9, 4)}";
        }

        if (digits.Length == 12 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            return $"+55 ({digits.Substring(2, 2)}) {digits.Substring(4, 4)}-{digits.Substring(8, 4)}";
        }

        return string.IsNullOrWhiteSpace(digits) ? "-" : $"+{digits}";
    }

    private static string BuildInitials(string value)
    {
        var words = (value ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }

    private static string WhatsAppFirstName(string value)
    {
        return (value ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "cliente";
    }

    private void SetWhatsAppManualChat(string phone, bool active)
    {
    }

    private static void TryApplyEvolutionLocalEnv(WhatsAppSettings settings)
    {
        var baseUrl = Environment.GetEnvironmentVariable("BALCAO_EVOLUTION_BASE_URL");
        var apiKey = Environment.GetEnvironmentVariable("BALCAO_EVOLUTION_API_KEY");
        var instance = Environment.GetEnvironmentVariable("BALCAO_EVOLUTION_INSTANCE");
        if (!string.IsNullOrWhiteSpace(baseUrl)) settings.EvolutionLocalBaseUrl = baseUrl.Trim();
        if (!string.IsNullOrWhiteSpace(apiKey)) settings.EvolutionLocalApiKey = apiKey.Trim();
        if (!string.IsNullOrWhiteSpace(instance)) settings.EvolutionLocalInstanceName = instance.Trim();
    }

    private void ShowFiscalComplianceDialog()
    {
        System.Windows.MessageBox.Show(
            "A configuracao fiscal esta disponivel, mas a emissao automatica exige o modulo fiscal completo.",
            "Fiscal",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ShowImplementationGuideDialog()
    {
        System.Windows.MessageBox.Show(
            "Configure certificados, CSC, ambiente e dados fiscais antes de habilitar emissao automatica.",
            "Guia de implantacao",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ShowPrivacyLgpdDialog()
    {
        System.Windows.MessageBox.Show(
            BuildPrivacyNoticeText(),
            "Privacidade e LGPD",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private string BuildPrivacyNoticeText()
    {
        var business = FirstNonEmpty(_profile.BusinessName, _profile.LegalName, AppDisplayName);
        return $"{business} trata dados pessoais para cadastro, atendimento, pedidos, pagamentos e obrigacoes legais. " +
               "O titular pode solicitar acesso, correcao, portabilidade ou eliminacao quando permitido pela LGPD.";
    }

    private static bool IsLegacyPrivacyNoticeText(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    private void ExportCustomerPrivacyData(CustomerRecord? customer, TextBlock statusText)
    {
        if (customer is null)
        {
            statusText.Text = "Selecione um cliente.";
            statusText.Foreground = RedText;
            return;
        }

        Directory.CreateDirectory(ExportDir);
        var path = Path.Combine(ExportDir, $"lgpd-cliente-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(customer, JsonOptions), Encoding.UTF8);
        statusText.Text = $"Dados exportados: {path}";
        statusText.Foreground = GreenText;
        SetStatus(statusText.Text);
    }

    private bool AnonymizeCustomerRecord(CustomerRecord? customer, TextBlock statusText)
    {
        if (customer is null)
        {
            statusText.Text = "Selecione um cliente.";
            statusText.Foreground = RedText;
            return false;
        }

        if (System.Windows.MessageBox.Show(
                "Remover os dados pessoais deste cliente?",
                "LGPD",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return false;
        }

        customer.Cpf = "";
        customer.Name = $"CLIENTE ANONIMIZADO {DateTime.Now:yyyyMMddHHmmss}";
        customer.Phone = "";
        customer.Address = "";
        customer.District = "";
        customer.Notes = "";
        customer.Birthday = null;
        customer.MarketingConsent = false;
        customer.DataConsentAt = null;
        customer.DataConsentSource = "";
        customer.PrivacyRemovalAt = DateTime.Now;
        SaveStore();
        statusText.Text = "Dados pessoais removidos.";
        statusText.Foreground = GreenText;
        SetStatus(statusText.Text);
        return true;
    }

    private RestaurantNfceIssueResult TryIssueRestaurantNfceForPaidTicket(
        TableTile? board,
        List<TicketLine> lines,
        List<PaymentLine> payments,
        decimal total)
    {
        var settings = _appSettings.FiscalTef;
        if (settings?.Enabled != true
            || settings.RestaurantNfEnabled != true
            || settings.RestaurantAutoIssueAfterPayment != true)
        {
            return new RestaurantNfceIssueResult { Ok = true };
        }

        return new RestaurantNfceIssueResult
        {
            Attempted = true,
            StatusCode = "MODULO_INDISPONIVEL",
            StatusMessage = "Modulo emissor NFC-e nao esta presente nesta fonte."
        };
    }
}
