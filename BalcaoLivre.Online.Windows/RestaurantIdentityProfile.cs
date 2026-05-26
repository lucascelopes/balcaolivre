namespace BalcaoLivre.Online.Windows;

public sealed class RestaurantIdentityProfile
{
    public string OwnerName { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string LocalLogoPath { get; set; } = "";

    public string DisplayName => !string.IsNullOrWhiteSpace(BusinessName) ? BusinessName : "Balcão Livre PDV";
}
