using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BalcaoLivre.Online.Windows;

public sealed class IFoodCloudClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new EmptyStringNullableDateTimeConverter() }
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };

    public async Task<IFoodCloudStartResponse> StartConnectionAsync(
        string backendUrl,
        IFoodCloudStoreContext context,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IFoodCloudStartResponse>(
            backendUrl,
            "connect/start",
            context,
            cancellationToken);
    }

    public async Task<IFoodCloudFinishResponse> FinishConnectionAsync(
        string backendUrl,
        IFoodCloudFinishRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IFoodCloudFinishResponse>(
            backendUrl,
            "connect/finish",
            request,
            cancellationToken);
    }

    public async Task<IFoodCloudSyncResponse> SyncOrdersAsync(
        string backendUrl,
        IFoodCloudSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IFoodCloudSyncResponse>(
            backendUrl,
            "orders/sync",
            request,
            cancellationToken);
    }

    public async Task<IFoodCloudActionResponse> SendOrderActionAsync(
        string backendUrl,
        IFoodCloudOrderActionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IFoodCloudActionResponse>(
            backendUrl,
            "orders/action",
            request,
            cancellationToken);
    }

    public async Task<IFoodCloudStockSyncResponse> SyncStockAsync(
        string backendUrl,
        IFoodCloudStockSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IFoodCloudStockSyncResponse>(
            backendUrl,
            "stock/sync",
            request,
            cancellationToken);
    }

    public async Task<IFoodCloudCatalogSyncResponse> SyncCatalogAsync(
        string backendUrl,
        IFoodCloudCatalogSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IFoodCloudCatalogSyncResponse>(
            backendUrl,
            "catalog/sync",
            request,
            cancellationToken);
    }

    private async Task<T> PostAsync<T>(string backendUrl, string path, object payload, CancellationToken cancellationToken)
    {
        var endpoint = BuildUri(backendUrl, path);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = ExtractMessage(body);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)
                ? $"Supabase iFood retornou {(int)response.StatusCode}."
                : detail);
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException("Resposta vazia do Supabase iFood.");
    }

    private static Uri BuildUri(string backendUrl, string path)
    {
        var baseUrl = (backendUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = IFoodIntegrationSettings.DefaultBackendUrl;
        }

        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Backend Supabase do iFood esta invalido.");
        }

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private static string ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return SimplifyMessage(message.GetString() ?? "");
            }

            if (document.RootElement.TryGetProperty("error", out var error))
            {
                return error.ValueKind == JsonValueKind.String
                    ? SimplifyMessage(error.GetString() ?? "")
                    : SimplifyMessage(error.GetRawText());
            }
        }
        catch (JsonException)
        {
            return SimplifyMessage(body);
        }

        return SimplifyMessage(body);
    }

    private static string SimplifyMessage(string value)
    {
        var message = (value ?? "").Trim();
        for (var i = 0; i < 2; i++)
        {
            if (string.IsNullOrWhiteSpace(message) || !message.StartsWith("{", StringComparison.Ordinal))
            {
                break;
            }

            try
            {
                using var document = JsonDocument.Parse(message);
                if (document.RootElement.TryGetProperty("message", out var nestedMessage))
                {
                    message = nestedMessage.GetString() ?? message;
                    continue;
                }

                if (document.RootElement.TryGetProperty("error", out var nestedError))
                {
                    if (nestedError.ValueKind == JsonValueKind.String)
                    {
                        message = nestedError.GetString() ?? message;
                        continue;
                    }

                    if (nestedError.TryGetProperty("message", out var nestedErrorMessage))
                    {
                        message = nestedErrorMessage.GetString() ?? message;
                        continue;
                    }
                }
            }
            catch (JsonException)
            {
                break;
            }

            break;
        }

        return message;
    }
}

public class IFoodCloudStoreContext
{
    public string LicenseKey { get; set; } = "";
    public string MachineHash { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string AppVersion { get; set; } = "";
}

public sealed class IFoodCloudFinishRequest : IFoodCloudStoreContext
{
    public string ConnectionId { get; set; } = "";
    public string AuthorizationCode { get; set; } = "";
}

public sealed class IFoodCloudSyncRequest : IFoodCloudStoreContext
{
    public string ConnectionId { get; set; } = "";
}

public sealed class IFoodCloudOrderActionRequest : IFoodCloudStoreContext
{
    public string ConnectionId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public string DeliveredBy { get; set; } = "MERCHANT";
}

public sealed class IFoodCloudStockSyncRequest : IFoodCloudStoreContext
{
    public string ConnectionId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ExternalCode { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = "";
    public string ImageDataUrl { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}

public sealed class IFoodCloudCatalogSyncRequest : IFoodCloudStoreContext
{
    public string ConnectionId { get; set; } = "";
}

public sealed class IFoodCloudStartResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public string Status { get; set; } = "";
    public string UserCode { get; set; } = "";
    public string VerificationUrl { get; set; } = "";
    public string VerificationUrlComplete { get; set; } = "";
    public int ExpiresIn { get; set; }
    public string MerchantId { get; set; } = "";
    public string MerchantName { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
}

public sealed class IFoodCloudFinishResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public string MerchantName { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
}

public sealed class IFoodCloudSyncResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public DateTime? SyncedAt { get; set; }
    public List<IFoodImportedOrder> Orders { get; set; } = [];
}

public sealed class IFoodCloudActionResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string Status { get; set; } = "";
    public string DeliveredBy { get; set; } = "";
}

public sealed class IFoodCloudStockSyncResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ExternalCode { get; set; } = "";
    public int Amount { get; set; }
    public string Mode { get; set; } = "";
    public bool ImageUpdated { get; set; }
    public string ImageWarning { get; set; } = "";
}

public sealed class IFoodCloudCatalogSyncResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public DateTime? SyncedAt { get; set; }
    public List<IFoodCatalogProduct> Products { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class IFoodCatalogProduct
{
    public string ProductId { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string ExternalCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? StockQuantity { get; set; }
    public bool? IsAvailable { get; set; }
}
