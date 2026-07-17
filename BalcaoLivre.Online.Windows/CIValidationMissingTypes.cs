namespace BalcaoLivre.Online.Windows;

// Compile-only bridge for two legacy DTOs referenced by the remote snapshot.
// This file lives only on codex/ifood-distributed-validation and must not be merged.
internal sealed class MobileBridgeSessionDto
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
}
