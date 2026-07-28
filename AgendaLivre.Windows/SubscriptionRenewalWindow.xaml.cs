using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace AgendaLivre.Windows;

public partial class SubscriptionRenewalWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AgendaAuthSessionManager _auth;
    private readonly AgendaSyncCoordinator _coordinator;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(25) };
    private bool _busy;

    public SubscriptionRenewalWindow(
        AgendaAuthSessionManager auth,
        AgendaSyncCoordinator coordinator)
    {
        InitializeComponent();
        _auth = auth;
        _coordinator = coordinator;
        Loaded += SubscriptionRenewalWindow_Loaded;
        Closed += (_, _) => _httpClient.Dispose();
    }

    private async void SubscriptionRenewalWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCardSummaryAsync();
    }

    private async Task LoadCardSummaryAsync()
    {
        try
        {
            using var response = await SendAsync(
                HttpMethod.Get,
                "/api/agenda/subscriptions/summary",
                payload: null);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                SavedCardTitle.Text = "Cartão ainda não disponível";
                return;
            }

            using var document = JsonDocument.Parse(body);
            if (!AgendaAuthSessionManager.TryGetProperty(document.RootElement, "card", out var card) ||
                card.ValueKind != JsonValueKind.Object)
            {
                SavedCardTitle.Text = "Nenhum cartão salvo nesta conta";
                SavedCardDetail.Text = "A Stripe pedirá um cartão no Checkout seguro.";
                return;
            }

            var brand = AgendaAuthSessionManager.ReadString(card, "brand");
            var last4 = AgendaAuthSessionManager.ReadString(card, "last4");
            var month = ReadInt(card, "expMonth");
            var year = ReadInt(card, "expYear");
            SavedCardTitle.Text = $"{FormatBrand(brand)} terminado em {last4}";
            SavedCardDetail.Text = month > 0 && year > 0
                ? $"Validade {month:00}/{year}. Salvo e protegido pela Stripe."
                : "Salvo e protegido pela Stripe.";
        }
        catch
        {
            SavedCardTitle.Text = "Não foi possível consultar o cartão";
            SavedCardDetail.Text = "Você ainda pode renovar pelo Checkout seguro.";
        }
    }

    private async void Renew_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string plan }) return;
        await OpenBillingUrlAsync(
            "/api/agenda/android/checkout",
            new
            {
                plan,
                idempotencyKey = $"{_auth.CurrentSession?.UserId}-{plan}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            },
            "checkout");
    }

    private async void Portal_Click(object sender, RoutedEventArgs e)
    {
        await OpenBillingUrlAsync(
            "/api/agenda/subscriptions/portal",
            new { },
            "portal");
    }

    private async Task OpenBillingUrlAsync(string path, object payload, string property)
    {
        if (_busy) return;
        _busy = true;
        StatusText.Text = "Abrindo pagamento seguro…";
        try
        {
            using var response = await SendAsync(HttpMethod.Post, path, payload);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new AgendaAuthException(ReadError(body));
            }

            using var document = JsonDocument.Parse(body);
            if (!AgendaAuthSessionManager.TryGetProperty(document.RootElement, property, out var billing) ||
                billing.ValueKind != JsonValueKind.Object)
            {
                throw new AgendaAuthException("O servidor não retornou o link da Stripe.");
            }

            var url = AgendaAuthSessionManager.ReadString(billing, "url");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new AgendaAuthException("O link de pagamento retornado é inválido.");
            }
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
            StatusText.Text = "Conclua na Stripe e depois clique em “Verificar licença”.";
        }
        catch (Exception exception) when (exception is HttpRequestException or AgendaAuthException or TaskCanceledException)
        {
            StatusText.Text = exception.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        StatusText.Text = "Verificando sua licença…";
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var entitlement = await _coordinator.RefreshEntitlementAsync(timeout.Token);
            if (entitlement?.CanUse == true)
            {
                DialogResult = true;
                Close();
                return;
            }
            StatusText.Text = "O pagamento ainda não foi confirmado. Aguarde alguns segundos e tente novamente.";
        }
        catch (Exception exception) when (exception is HttpRequestException or AgendaAuthException or TaskCanceledException)
        {
            StatusText.Text = exception.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? payload)
    {
        var token = await _auth.GetAccessTokenAsync(forceRefresh: false);
        var uri = new Uri(_auth.Config.SyncUrl, path);
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (payload is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }
        return await _httpClient.SendAsync(request);
    }

    private static int ReadInt(JsonElement source, string name)
    {
        if (!AgendaAuthSessionManager.TryGetProperty(source, name, out var value)) return 0;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : 0;
    }

    private static string FormatBrand(string brand) => brand.ToLowerInvariant() switch
    {
        "visa" => "Visa",
        "mastercard" => "Mastercard",
        "amex" => "American Express",
        "elo" => "Elo",
        _ => "Cartão"
    };

    private static string ReadError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (AgendaAuthSessionManager.TryGetProperty(document.RootElement, "error", out var error))
            {
                var message = error.ValueKind == JsonValueKind.Object
                    ? AgendaAuthSessionManager.ReadString(error, "message")
                    : error.ToString();
                if (!string.IsNullOrWhiteSpace(message)) return message;
            }
        }
        catch (JsonException)
        {
        }
        return "Não foi possível abrir o pagamento agora.";
    }
}
