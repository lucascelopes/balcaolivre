using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalcaoLivre.Online.Windows;

public sealed class IFoodUserCodeResponse
{
    [JsonPropertyName("userCode")]
    public string UserCode { get; set; } = "";

    [JsonPropertyName("authorizationCodeVerifier")]
    public string AuthorizationCodeVerifier { get; set; } = "";

    [JsonPropertyName("verificationUrl")]
    public string VerificationUrl { get; set; } = "";

    [JsonPropertyName("verificationUrlComplete")]
    public string VerificationUrlComplete { get; set; } = "";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

public sealed class IFoodTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public sealed class IFoodMerchant
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("corporateName")]
    public string CorporateName { get; set; } = "";
}

public sealed class IFoodEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("fullCode")]
    public string FullCode { get; set; } = "";

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = "";

    [JsonPropertyName("merchantId")]
    public string MerchantId { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement Metadata { get; set; }
}

public sealed class IFoodImportedOrder
{
    public string OrderId { get; set; } = "";
    public string DisplayId { get; set; } = "";
    public string OrderType { get; set; } = "DELIVERY";
    public string CustomerName { get; set; } = "CLIENTE IFOOD";
    public string CustomerDocument { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string District { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal Total { get; set; }
    public List<IFoodImportedItem> Items { get; set; } = [];
}

public sealed class IFoodImportedItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Notes { get; set; } = "";
}
