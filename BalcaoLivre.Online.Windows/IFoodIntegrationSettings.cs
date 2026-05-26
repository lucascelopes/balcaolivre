using System.Text.Json.Serialization;

namespace BalcaoLivre.Online.Windows;

public sealed class IFoodIntegrationSettings
{
    public const string DefaultBackendUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood";

    public bool Enabled { get; set; }
    public string BackendUrl { get; set; } = DefaultBackendUrl;
    public string ConnectionId { get; set; } = "";
    public string ConnectionStatus { get; set; } = "";
    public string MerchantName { get; set; } = "";
    public string WebhookUrl { get; set; } = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/ifood/webhook";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime? AccessTokenExpiresAt { get; set; }
    public string AuthorizationCodeVerifier { get; set; } = "";
    public string LastUserCode { get; set; } = "";
    public string VerificationUrl { get; set; } = "";
    public string VerificationUrlComplete { get; set; } = "";
    public DateTime? LastSyncUtc { get; set; }
    public List<string> ImportedEventIds { get; set; } = [];

    [JsonIgnore]
    public bool HasCloudConnection => Enabled
        && !string.IsNullOrWhiteSpace(ConnectionId)
        && (string.IsNullOrWhiteSpace(ConnectionStatus)
            || string.Equals(ConnectionStatus, "conectado", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(MerchantId));

    [JsonIgnore]
    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    [JsonIgnore]
    public bool HasAccessToken =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        (!AccessTokenExpiresAt.HasValue || AccessTokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(2));
}
