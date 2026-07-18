namespace AgendaLivre.Windows;

internal static class WhatsAppPhoneNormalizer
{
    public static string Normalize(string? phone)
    {
        var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length is 10 or 11)
        {
            digits = $"55{digits}";
        }

        // A phone read from a WhatsApp conversation is a provider identifier.
        // Some Brazilian accounts still use the legacy 8-digit local form.
        // Inserting a ninth digit here can target a different recipient.
        return digits;
    }
}
