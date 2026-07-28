using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgendaLivre.Windows;

public sealed class AgendaDataSavedEventArgs(AgendaData data, string serialized) : EventArgs
{
    public AgendaData Data { get; } = data;
    public string Serialized { get; } = serialized;
}

public sealed class AgendaDataStore
{
    private static readonly string AuditDataRoot = Path.Combine(
        Path.GetTempPath(),
        "AgendaLivre.Windows-Audit",
        $"{Environment.ProcessId}-{Guid.NewGuid():N}");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly bool _seedWhenMissing;
    private readonly object _ioGate = new();

    public AgendaDataStore()
        : this(userId: null, seedWhenMissing: true)
    {
    }

    public AgendaDataStore(string userId)
        : this(userId, seedWhenMissing: false)
    {
    }

    private AgendaDataStore(string? userId, bool seedWhenMissing)
    {
        _seedWhenMissing = seedWhenMissing;
        var configuredRoot = Environment.GetEnvironmentVariable("AGENDA_LIVRE_DATA_ROOT");
        string baseRoot;
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            baseRoot = Path.GetFullPath(configuredRoot);
        }
        else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE")) ||
                 !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_SCREENSHOT_PATH")))
        {
            // Keep automated audits isolated from the user's real data.
            baseRoot = AuditDataRoot;
        }
        else
        {
            baseRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AgendaLivre.Windows");
        }

        DataRoot = string.IsNullOrWhiteSpace(userId)
            ? baseRoot
            : Path.Combine(baseRoot, "accounts", SafeAccountId(userId));

        DataPath = Path.Combine(DataRoot, "agenda-data.json");
    }

    public event EventHandler<AgendaDataSavedEventArgs>? Saved;

    public static string LegacyDataPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AgendaLivre.Windows",
        "agenda-data.json");

    public string DataRoot { get; }
    public string DataPath { get; }
    public bool HasLocalData => File.Exists(DataPath);

    public AgendaData LoadOrCreate()
    {
        Directory.CreateDirectory(DataRoot);

        if (!File.Exists(DataPath))
        {
            var created = _seedWhenMissing ? CreateSeedData() : CreateCleanData();
            created.Settings.OnboardingCompleted = _seedWhenMissing && IsAuditMode();
            Save(created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(DataPath);
            var data = JsonSerializer.Deserialize<AgendaData>(json, JsonOptions) ?? CreateSeedData();
            EnsureUsableData(data);
            Save(data);
            return data;
        }
        catch
        {
            var backupPath = Path.Combine(DataRoot, $"agenda-data-corrompido-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(DataPath, backupPath, overwrite: true);

            var recovered = _seedWhenMissing ? CreateSeedData() : CreateCleanData();
            recovered.Settings.OnboardingCompleted = _seedWhenMissing && IsAuditMode();
            Save(recovered);
            return recovered;
        }
    }

    public void Save(AgendaData data)
    {
        lock (_ioGate)
        {
            SaveCore(data, notifySaved: true);
        }
    }

    internal bool TrySaveFromSync(AgendaData data, Func<bool> canApply, Action committed)
    {
        ArgumentNullException.ThrowIfNull(canApply);
        ArgumentNullException.ThrowIfNull(committed);

        lock (_ioGate)
        {
            if (!canApply())
            {
                return false;
            }

            SaveCore(data, notifySaved: false);
            committed();
            return true;
        }
    }

    private void SaveCore(AgendaData data, bool notifySaved)
    {
        Directory.CreateDirectory(DataRoot);
        EnsureUsableData(data);
        var serialized = JsonSerializer.Serialize(data, JsonOptions);
        var temporaryPath = $"{DataPath}.tmp";
        var backupPath = $"{DataPath}.bak";

        File.WriteAllText(temporaryPath, serialized, new UTF8Encoding(false));
        if (File.Exists(DataPath))
        {
            File.Copy(DataPath, backupPath, overwrite: true);
        }

        File.Move(temporaryPath, DataPath, overwrite: true);
        if (!notifySaved)
        {
            return;
        }

        try
        {
            Saved?.Invoke(this, new AgendaDataSavedEventArgs(data, serialized));
        }
        catch
        {
            // A falha de uma integração posterior nunca invalida o save local já concluído.
        }
    }

    private static string SafeAccountId(string userId)
    {
        var clean = new string(userId
            .Trim()
            .ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .Take(96)
            .ToArray());
        if (clean.Length >= 8)
        {
            return clean;
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(userId.Trim()));
        return Convert.ToHexString(digest)[..32].ToLowerInvariant();
    }

    private static void EnsureUsableData(AgendaData data)
    {
        data.Settings ??= new AgendaSettings();
        data.Services ??= [];
        data.Professionals ??= [];
        data.Customers ??= [];
        data.Appointments ??= [];
        data.Products ??= [];
        data.ProductSales ??= [];
        data.ManualPayments ??= [];
        data.CustomerReceivables ??= [];
        data.Expenses ??= [];
        data.WhatsAppMessages ??= [];
        data.WhatsAppLeads ??= [];
        data.Settings.BusinessName ??= "Balcão Livre";
        data.Settings.BusinessLogoPath ??= "";
        data.Settings.BusinessDocument ??= "";
        data.Settings.BusinessPhone ??= "";
        data.Settings.PixKey ??= "";
        data.Settings.BusinessAddress ??= "";
        data.Settings.ThemeId ??= "";
        data.Settings.AccountFullName ??= "";
        data.Settings.AccountPhone ??= "";
        data.Settings.AccountEmail ??= "";
        data.Settings.ProfessionalCountRange ??= "";
        data.Settings.MainObjective ??= "";
        data.Settings.PostalCode ??= "";
        data.Settings.Neighborhood ??= "";
        data.Settings.Street ??= "";
        data.Settings.AddressNumber ??= "";
        data.Settings.AddressComplement ??= "";
        data.Settings.WhatsAppEvolutionBaseUrl ??= "";
        data.Settings.WhatsAppEvolutionInstanceName ??= "";
        data.Settings.WhatsAppEvolutionState ??= "";
        data.Settings.WhatsAppEvolutionQrBase64 ??= "";
        data.Settings.InstagramApiUrl ??= "";
        data.Settings.InstagramUsername ??= "";
        data.Settings.InstagramDisplayName ??= "";
        data.Settings.InstagramAccountId ??= "";
        data.Settings.InstagramState ??= "";
        data.Settings.InstagramLastError ??= "";
        data.Settings.AccountPasswordHash ??= "";
        data.Settings.Resources ??= [];
        data.Settings.MarketingSiteHeader ??= new MarketingCatalogHeader();
        data.Settings.MarketingSiteFooter ??= new MarketingCatalogFooter();
        data.Settings.MarketingSiteDesign ??= new MarketingCatalogDesign();
        data.Settings.MarketingSiteSections ??= [];
        data.Settings.MarketingSitePromotion ??= new MarketingSitePromotion();
        data.Settings.MarketingSitePromotion.Items ??= [];
        foreach (var section in data.Settings.MarketingSiteSections)
        {
            section.Id = string.IsNullOrWhiteSpace(section.Id)
                ? Guid.NewGuid().ToString("N")
                : section.Id;
            section.Items ??= [];
            foreach (var item in section.Items)
            {
                item.Id = string.IsNullOrWhiteSpace(item.Id)
                    ? Guid.NewGuid().ToString("N")
                    : item.Id;
            }
        }
        if (data.Settings.PublishedMarketingCatalog is { } publication)
        {
            publication.Header ??= new MarketingCatalogHeader();
            publication.Footer ??= new MarketingCatalogFooter();
            publication.Design ??= new MarketingCatalogDesign();
            publication.Sections ??= [];
            if (publication.Promotion is { } promotion)
            {
                promotion.Items ??= [];
            }
            foreach (var section in publication.Sections)
            {
                section.Id = string.IsNullOrWhiteSpace(section.Id)
                    ? Guid.NewGuid().ToString("N")
                    : section.Id;
                section.Items ??= [];
                foreach (var item in section.Items)
                {
                    item.Id = string.IsNullOrWhiteSpace(item.Id)
                        ? Guid.NewGuid().ToString("N")
                        : item.Id;
                }
            }
        }
        foreach (var professional in data.Professionals)
        {
            professional.Segments ??= [];
        }

        RepairPersistedText(data);
        NormalizeBusinessRules(data);

        if (string.IsNullOrWhiteSpace(data.Settings.ClientLabel))
        {
            data.Settings.ClientLabel = "Cliente";
        }

        if (string.IsNullOrWhiteSpace(data.Settings.ClientDetailLabel))
        {
            data.Settings.ClientDetailLabel = "Paciente / pet / veículo / preferência";
        }

        if (string.IsNullOrWhiteSpace(data.Settings.ResourceLabel))
        {
            data.Settings.ResourceLabel = "Sala, box ou cadeira";
        }

        foreach (var service in data.Services.Where(service => string.IsNullOrWhiteSpace(service.Id)))
        {
            service.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var professional in data.Professionals.Where(professional => string.IsNullOrWhiteSpace(professional.Id)))
        {
            professional.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var customer in data.Customers.Where(customer => string.IsNullOrWhiteSpace(customer.Id)))
        {
            customer.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var appointment in data.Appointments.Where(appointment => string.IsNullOrWhiteSpace(appointment.Id)))
        {
            appointment.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var product in data.Products.Where(product => string.IsNullOrWhiteSpace(product.Id)))
        {
            product.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var sale in data.ProductSales.Where(sale => string.IsNullOrWhiteSpace(sale.Id)))
        {
            sale.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var payment in data.ManualPayments.Where(payment => string.IsNullOrWhiteSpace(payment.Id)))
        {
            payment.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var receivable in data.CustomerReceivables.Where(receivable => string.IsNullOrWhiteSpace(receivable.Id)))
        {
            receivable.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var expense in data.Expenses.Where(expense => string.IsNullOrWhiteSpace(expense.Id)))
        {
            expense.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var message in data.WhatsAppMessages.Where(message => string.IsNullOrWhiteSpace(message.Id)))
        {
            message.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var message in data.WhatsAppMessages)
        {
            message.ClientRequestId ??= "";
            message.ProviderMessageId ??= "";
            message.Provider ??= "";
            message.Instance ??= "";
            message.ConversationId ??= "";
            message.LeadId ??= "";
            message.Type ??= "text";
            message.Kind ??= "";
            if (IsLegacyWhatsAppConnectionNotice(message))
            {
                message.Direction = "sistema";
                message.Status = "informativo";
                message.Category = "Sistema";
                message.ReadAt ??= message.CreatedAt;
            }
        }

        foreach (var lead in data.WhatsAppLeads)
        {
            lead.Id = string.IsNullOrWhiteSpace(lead.Id) ? Guid.NewGuid().ToString("N") : lead.Id;
            lead.Instance ??= "";
            lead.ConversationId ??= "";
            lead.CustomerName ??= "";
            lead.Phone ??= "";
            lead.Stage = string.IsNullOrWhiteSpace(lead.Stage) ? "new" : lead.Stage;
            lead.Summary ??= "";
            lead.Facts ??= [];
            lead.Intent ??= "";
            lead.RequestedService ??= "";
            lead.PreferredSchedule ??= "";
            lead.AssignedProfessional ??= "";
            lead.PreferredDate ??= "";
            lead.Period ??= "";
            lead.Notes ??= "";
        }

        BackfillCustomersFromAppointments(data);
    }

    private static void BackfillCustomersFromAppointments(AgendaData data)
    {
        foreach (var appointment in data.Appointments
                     .Where(item => item.Status != AppointmentStatus.Blocked)
                     .Where(item => !string.IsNullOrWhiteSpace(item.CustomerName)))
        {
            var customer = data.Customers.FirstOrDefault(item =>
                item.Name.Equals(appointment.CustomerName, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(item.Phone) &&
                 !string.IsNullOrWhiteSpace(appointment.CustomerPhone) &&
                 item.Phone.Equals(appointment.CustomerPhone, StringComparison.OrdinalIgnoreCase)));

            if (customer is null)
            {
                data.Customers.Add(new Customer
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = appointment.CustomerName.Trim(),
                    Phone = (appointment.CustomerPhone ?? "").Trim(),
                    Segment = appointment.Segment,
                    Profile = appointment.CustomerProfile,
                    LastSeenAt = appointment.Start
                });
                continue;
            }

            if (appointment.Start > customer.LastSeenAt)
            {
                customer.LastSeenAt = appointment.Start;
            }
        }
    }

    private static void NormalizeBusinessRules(AgendaData data)
    {
        data.Settings.WorkdayStartHour = Math.Clamp(data.Settings.WorkdayStartHour, 0, 23);
        data.Settings.WorkdayEndHour = Math.Clamp(data.Settings.WorkdayEndHour, 1, 24);
        if (data.Settings.WorkdayEndHour <= data.Settings.WorkdayStartHour)
        {
            data.Settings.WorkdayStartHour = 8;
            data.Settings.WorkdayEndHour = 20;
        }

        data.Settings.Workdays ??= [1, 2, 3, 4, 5, 6];
        data.Settings.Workdays = data.Settings.Workdays
            .Where(day => day is >= 0 and <= 6)
            .Distinct()
            .OrderBy(day => day == 0 ? 7 : day)
            .ToList();
        if (data.Settings.Workdays.Count == 0)
        {
            data.Settings.Workdays = [1, 2, 3, 4, 5, 6];
        }

        data.Settings.WorkdayBreakStartHour = Math.Clamp(data.Settings.WorkdayBreakStartHour, 0, 23);
        data.Settings.WorkdayBreakEndHour = Math.Clamp(data.Settings.WorkdayBreakEndHour, 1, 24);
        if (data.Settings.WorkdayBreakEndHour <= data.Settings.WorkdayBreakStartHour ||
            data.Settings.WorkdayBreakStartHour < data.Settings.WorkdayStartHour ||
            data.Settings.WorkdayBreakEndHour > data.Settings.WorkdayEndHour)
        {
            data.Settings.WorkdayBreakEnabled = false;
            data.Settings.WorkdayBreakStartHour = Math.Clamp(12, data.Settings.WorkdayStartHour, data.Settings.WorkdayEndHour - 1);
            data.Settings.WorkdayBreakEndHour = Math.Min(data.Settings.WorkdayEndHour, data.Settings.WorkdayBreakStartHour + 1);
        }

        data.Settings.Resources = data.Settings.Resources
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();

        foreach (var service in data.Services)
        {
            service.Name = string.IsNullOrWhiteSpace(service.Name) ? "Atendimento" : service.Name.Trim();
            service.DurationMinutes = Math.Clamp(service.DurationMinutes, 5, 480);
            service.PreparationMinutes = Math.Clamp(service.PreparationMinutes, 0, 240);
            service.BufferMinutes = Math.Clamp(service.BufferMinutes, 0, 240);
            service.Price = Math.Max(0, service.Price);
            service.CommissionPercent = Math.Clamp(service.CommissionPercent, 0, 100);
            service.DefaultResource = (service.DefaultResource ?? "").Trim();
        }

        foreach (var professional in data.Professionals)
        {
            professional.Name = string.IsNullOrWhiteSpace(professional.Name) ? "Profissional" : professional.Name.Trim();
            professional.CommissionPercent = Math.Clamp(professional.CommissionPercent, 0, 100);
            professional.Segments = professional.Segments
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        foreach (var customer in data.Customers)
        {
            customer.Name = (customer.Name ?? "").Trim();
            customer.Phone = (customer.Phone ?? "").Trim();
            customer.LastSeenAt = customer.LastSeenAt == DateTime.MinValue ? DateTime.Now : customer.LastSeenAt;
        }

        foreach (var appointment in data.Appointments)
        {
            appointment.CustomerName = string.IsNullOrWhiteSpace(appointment.CustomerName) ? "Cliente" : appointment.CustomerName.Trim();
            appointment.DurationMinutes = Math.Clamp(appointment.DurationMinutes, 5, 480);
            appointment.Price = Math.Max(0, appointment.Price);
            appointment.ResourceName = (appointment.ResourceName ?? "").Trim();
            appointment.CreatedAt = appointment.CreatedAt == DateTime.MinValue ? DateTime.Now : appointment.CreatedAt;
            appointment.UpdatedAt = appointment.UpdatedAt == DateTime.MinValue ? appointment.CreatedAt : appointment.UpdatedAt;
            appointment.PaymentConfirmedAt = appointment.PaymentConfirmedAt == DateTime.MinValue
                ? null
                : appointment.PaymentConfirmedAt;
        }

        foreach (var product in data.Products)
        {
            product.Name = string.IsNullOrWhiteSpace(product.Name) ? "Produto" : product.Name.Trim();
            product.Price = Math.Max(0, product.Price);
            product.CostPrice = Math.Max(0, product.CostPrice);
            product.StockQuantity = Math.Max(0, product.StockQuantity);
            product.MinimumStock = Math.Max(0, product.MinimumStock);
        }

        foreach (var sale in data.ProductSales)
        {
            sale.Quantity = Math.Max(1, sale.Quantity);
            sale.UnitPrice = Math.Max(0, sale.UnitPrice);
            sale.Discount = Math.Max(0, sale.Discount);
        }

        foreach (var payment in data.ManualPayments)
        {
            payment.Value = Math.Max(0, payment.Value);
        }

        foreach (var receivable in data.CustomerReceivables)
        {
            receivable.OriginalValue = Math.Max(0, receivable.OriginalValue);
            receivable.RemainingValue = Math.Clamp(receivable.RemainingValue, 0, receivable.OriginalValue);
            receivable.Status = NormalizeReceivableStatus(receivable.Status);
            receivable.OpenedAt = receivable.OpenedAt == DateTime.MinValue ? DateTime.Now : receivable.OpenedAt;
            receivable.UpdatedAt = receivable.UpdatedAt == DateTime.MinValue
                ? receivable.OpenedAt
                : receivable.UpdatedAt;
            if (receivable.UpdatedAt < receivable.OpenedAt)
            {
                receivable.UpdatedAt = receivable.OpenedAt;
            }

            receivable.DueAt = receivable.DueAt == DateTime.MinValue ? null : receivable.DueAt;
            receivable.PaidAt = receivable.PaidAt == DateTime.MinValue ? null : receivable.PaidAt;
            if (receivable.Status == "paid")
            {
                receivable.RemainingValue = 0;
                receivable.PaidAt ??= receivable.UpdatedAt;
            }
        }

        foreach (var expense in data.Expenses)
        {
            expense.Value = Math.Max(0, expense.Value);
        }
    }

    private static string NormalizeReceivableStatus(string status) =>
        (status ?? "").Trim().ToLowerInvariant() switch
        {
            "paid" => "paid",
            "cancelled" or "canceled" => "cancelled",
            _ => "open"
        };

    private static void RepairPersistedText(AgendaData data)
    {
        data.Settings.AccountFullName = RepairText(data.Settings.AccountFullName);
        data.Settings.AccountPhone = RepairText(data.Settings.AccountPhone);
        data.Settings.AccountEmail = RepairText(data.Settings.AccountEmail);
        data.Settings.BusinessName = RepairText(data.Settings.BusinessName);
        data.Settings.BusinessLogoPath = RepairText(data.Settings.BusinessLogoPath);
        data.Settings.BusinessDocument = RepairText(data.Settings.BusinessDocument);
        data.Settings.BusinessPhone = RepairText(data.Settings.BusinessPhone);
        data.Settings.PixKey = RepairText(data.Settings.PixKey);
        data.Settings.BusinessAddress = RepairText(data.Settings.BusinessAddress);
        data.Settings.BusinessSegment = RepairText(data.Settings.BusinessSegment);
        data.Settings.ThemeId = RepairText(data.Settings.ThemeId);
        data.Settings.ClientLabel = RepairText(data.Settings.ClientLabel);
        data.Settings.ClientDetailLabel = RepairText(data.Settings.ClientDetailLabel);
        data.Settings.ResourceLabel = RepairText(data.Settings.ResourceLabel);
        data.Settings.ProfessionalCountRange = RepairText(data.Settings.ProfessionalCountRange);
        data.Settings.MainObjective = RepairText(data.Settings.MainObjective);
        data.Settings.PostalCode = RepairText(data.Settings.PostalCode);
        data.Settings.Neighborhood = RepairText(data.Settings.Neighborhood);
        data.Settings.Street = RepairText(data.Settings.Street);
        data.Settings.AddressNumber = RepairText(data.Settings.AddressNumber);
        data.Settings.AddressComplement = RepairText(data.Settings.AddressComplement);
        data.Settings.WhatsAppStorePhone = RepairText(data.Settings.WhatsAppStorePhone);
        data.Settings.WhatsAppConnectedName = RepairText(data.Settings.WhatsAppConnectedName);
        data.Settings.WhatsAppEvolutionBaseUrl = RepairText(data.Settings.WhatsAppEvolutionBaseUrl);
        data.Settings.WhatsAppEvolutionApiKey = RepairText(data.Settings.WhatsAppEvolutionApiKey);
        data.Settings.WhatsAppEvolutionInstanceName = RepairText(data.Settings.WhatsAppEvolutionInstanceName);
        data.Settings.WhatsAppEvolutionState = RepairText(data.Settings.WhatsAppEvolutionState);
        data.Settings.WhatsAppEvolutionQrBase64 = RepairText(data.Settings.WhatsAppEvolutionQrBase64);
        data.Settings.InstagramApiUrl = RepairText(data.Settings.InstagramApiUrl);
        data.Settings.InstagramUsername = RepairText(data.Settings.InstagramUsername);
        data.Settings.InstagramDisplayName = RepairText(data.Settings.InstagramDisplayName);
        data.Settings.InstagramAccountId = RepairText(data.Settings.InstagramAccountId);
        data.Settings.InstagramState = RepairText(data.Settings.InstagramState);
        data.Settings.InstagramLastError = RepairText(data.Settings.InstagramLastError);
        data.Settings.MercadoPagoLicenseKey = RepairText(data.Settings.MercadoPagoLicenseKey);
        data.Settings.MercadoPagoPaymentsApiUrl = RepairText(data.Settings.MercadoPagoPaymentsApiUrl);
        data.Settings.MercadoPagoSellerUserId = RepairText(data.Settings.MercadoPagoSellerUserId);
        data.Settings.MercadoPagoDefaultTerminalId = RepairText(data.Settings.MercadoPagoDefaultTerminalId);
        data.Settings.MercadoPagoDefaultTerminalLabel = RepairText(data.Settings.MercadoPagoDefaultTerminalLabel);
        data.Settings.MercadoPagoLastError = RepairText(data.Settings.MercadoPagoLastError);
        data.Settings.MarketingSiteSeoTitle = RepairText(data.Settings.MarketingSiteSeoTitle);
        data.Settings.MarketingSiteSeoDescription = RepairText(data.Settings.MarketingSiteSeoDescription);
        RepairMarketingSitePromotion(data.Settings.MarketingSitePromotion);
        RepairMarketingCatalogHeader(data.Settings.MarketingSiteHeader);
        RepairMarketingCatalogFooter(data.Settings.MarketingSiteFooter);
        foreach (var section in data.Settings.MarketingSiteSections)
        {
            RepairMarketingCatalogSection(section);
        }
        if (data.Settings.PublishedMarketingCatalog is { } publication)
        {
            publication.SeoTitle = RepairText(publication.SeoTitle);
            publication.SeoDescription = RepairText(publication.SeoDescription);
            if (publication.Promotion is { } promotion)
            {
                RepairMarketingSitePromotion(promotion);
            }
            RepairMarketingCatalogHeader(publication.Header);
            RepairMarketingCatalogFooter(publication.Footer);
            foreach (var section in publication.Sections)
            {
                RepairMarketingCatalogSection(section);
            }
        }

        if (string.IsNullOrWhiteSpace(data.Settings.WhatsAppEvolutionBaseUrl)
            || data.Settings.WhatsAppEvolutionBaseUrl.Contains("/evolution-proxy", StringComparison.OrdinalIgnoreCase)
            || data.Settings.WhatsAppEvolutionBaseUrl.Contains("hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            data.Settings.WhatsAppEvolutionBaseUrl = "https://hzvplpotsdzxygkxrgyi.functions.supabase.co/functions/v1/whatsapp";
            data.Settings.WhatsAppEvolutionState = "";
            data.Settings.WhatsAppEvolutionQrBase64 = "";
            data.Settings.WhatsAppLinked = false;
        }

        if (string.IsNullOrWhiteSpace(data.Settings.InstagramApiUrl))
        {
            data.Settings.InstagramApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/instagram";
        }

        if (string.IsNullOrWhiteSpace(data.Settings.MercadoPagoPaymentsApiUrl))
        {
            data.Settings.MercadoPagoPaymentsApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments";
        }

        for (var index = 0; index < data.Settings.Resources.Count; index++)
        {
            data.Settings.Resources[index] = RepairText(data.Settings.Resources[index]);
        }

        foreach (var service in data.Services)
        {
            service.Segment = RepairText(service.Segment);
            service.Name = RepairText(service.Name);
            service.Category = RepairText(service.Category);
            service.Description = RepairText(service.Description);
            service.DefaultResource = RepairText(service.DefaultResource);
        }

        foreach (var professional in data.Professionals)
        {
            professional.Name = RepairText(professional.Name);
            professional.Role = RepairText(professional.Role);
            professional.Phone = RepairText(professional.Phone);
            professional.Email = RepairText(professional.Email);
            professional.Document = RepairText(professional.Document);
            professional.Notes = RepairText(professional.Notes);
            for (var index = 0; index < professional.Segments.Count; index++)
            {
                professional.Segments[index] = RepairText(professional.Segments[index]);
            }
        }

        foreach (var customer in data.Customers)
        {
            customer.Name = RepairText(customer.Name);
            customer.Phone = RepairText(customer.Phone);
            customer.Email = RepairText(customer.Email);
            customer.Document = RepairText(customer.Document);
            customer.Segment = RepairText(customer.Segment);
            customer.Profile = RepairText(customer.Profile);
            customer.Tags = RepairText(customer.Tags);
            customer.Notes = RepairText(customer.Notes);
        }

        foreach (var appointment in data.Appointments)
        {
            appointment.Segment = RepairText(appointment.Segment);
            appointment.CustomerId = RepairText(appointment.CustomerId);
            appointment.CustomerName = RepairText(appointment.CustomerName);
            appointment.CustomerPhone = RepairText(appointment.CustomerPhone);
            appointment.CustomerProfile = RepairText(appointment.CustomerProfile);
            appointment.ServiceId = RepairText(appointment.ServiceId);
            appointment.ServiceName = RepairText(appointment.ServiceName);
            appointment.ProfessionalId = RepairText(appointment.ProfessionalId);
            appointment.ProfessionalName = RepairText(appointment.ProfessionalName);
            appointment.ResourceName = RepairText(appointment.ResourceName);
            appointment.PaymentMethod = RepairText(appointment.PaymentMethod);
            appointment.PaymentProvider = RepairText(appointment.PaymentProvider);
            appointment.PaymentReference = RepairText(appointment.PaymentReference);
            appointment.PaymentStatus = RepairText(appointment.PaymentStatus);
            appointment.Notes = RepairText(appointment.Notes);
            appointment.ServiceLines ??= [];
            if (appointment.ServiceLines.Count == 0 && !string.IsNullOrWhiteSpace(appointment.ServiceName))
            {
                appointment.ServiceLines.Add(new AppointmentServiceLine
                {
                    ServiceId = appointment.ServiceId,
                    ServiceName = appointment.ServiceName,
                    Segment = appointment.Segment,
                    Quantity = 1,
                    DurationMinutes = Math.Max(1, appointment.DurationMinutes),
                    UnitPrice = Math.Max(0, appointment.Price)
                });
            }
            foreach (var line in appointment.ServiceLines)
            {
                line.ServiceId = RepairText(line.ServiceId);
                line.ServiceName = RepairText(line.ServiceName);
                line.Segment = RepairText(line.Segment);
                line.Quantity = Math.Max(1, line.Quantity);
                line.DurationMinutes = Math.Max(1, line.DurationMinutes);
                line.UnitPrice = Math.Max(0, line.UnitPrice);
            }
            appointment.ProductLines ??= [];
            foreach (var line in appointment.ProductLines)
            {
                line.ProductId = RepairText(line.ProductId);
                line.ProductName = RepairText(line.ProductName);
                line.Quantity = Math.Max(1, line.Quantity);
                line.UnitPrice = Math.Max(0, line.UnitPrice);
            }
        }

        foreach (var product in data.Products)
        {
            product.Name = RepairText(product.Name);
            product.Category = RepairText(product.Category);
            product.Sku = RepairText(product.Sku);
            product.Supplier = RepairText(product.Supplier);
            product.Notes = RepairText(product.Notes);
        }

        foreach (var sale in data.ProductSales)
        {
            sale.ProductId = RepairText(sale.ProductId);
            sale.ProductName = RepairText(sale.ProductName);
            sale.CustomerName = RepairText(sale.CustomerName);
            sale.PaymentMethod = RepairText(sale.PaymentMethod);
            sale.PaymentProvider = RepairText(sale.PaymentProvider);
            sale.PaymentReference = RepairText(sale.PaymentReference);
            sale.PaymentStatus = RepairText(sale.PaymentStatus);
            sale.Notes = RepairText(sale.Notes);
        }

        foreach (var payment in data.ManualPayments)
        {
            payment.Description = RepairText(payment.Description);
            payment.CustomerName = RepairText(payment.CustomerName);
            payment.Category = RepairText(payment.Category);
            payment.PaymentMethod = RepairText(payment.PaymentMethod);
            payment.PaymentProvider = RepairText(payment.PaymentProvider);
            payment.PaymentReference = RepairText(payment.PaymentReference);
            payment.PaymentStatus = RepairText(payment.PaymentStatus);
            payment.Notes = RepairText(payment.Notes);
        }

        foreach (var receivable in data.CustomerReceivables)
        {
            receivable.CustomerId = RepairText(receivable.CustomerId);
            receivable.CustomerName = RepairText(receivable.CustomerName);
            receivable.AppointmentId = RepairText(receivable.AppointmentId);
            receivable.Description = RepairText(receivable.Description);
            receivable.Status = RepairText(receivable.Status);
            receivable.PaymentMethod = RepairText(receivable.PaymentMethod);
            receivable.PaymentProvider = RepairText(receivable.PaymentProvider);
            receivable.PaymentReference = RepairText(receivable.PaymentReference);
            receivable.PaymentStatus = RepairText(receivable.PaymentStatus);
            receivable.Notes = RepairText(receivable.Notes);
        }

        foreach (var expense in data.Expenses)
        {
            expense.Description = RepairText(expense.Description);
            expense.Category = RepairText(expense.Category);
            expense.Supplier = RepairText(expense.Supplier);
            expense.PaymentMethod = RepairText(expense.PaymentMethod);
            expense.Notes = RepairText(expense.Notes);
        }

        foreach (var message in data.WhatsAppMessages)
        {
            message.ClientRequestId = RepairText(message.ClientRequestId);
            message.ProviderMessageId = RepairText(message.ProviderMessageId);
            message.Provider = RepairText(message.Provider);
            message.Instance = RepairText(message.Instance);
            message.ConversationId = RepairText(message.ConversationId);
            message.LeadId = RepairText(message.LeadId);
            message.Type = RepairText(message.Type);
            message.Kind = RepairText(message.Kind);
            message.CustomerName = RepairText(message.CustomerName);
            message.Phone = RepairText(message.Phone);
            message.Message = RepairText(message.Message);
            message.Direction = RepairText(message.Direction);
            message.Status = RepairText(message.Status);
            message.Category = RepairText(message.Category);
        }

        foreach (var lead in data.WhatsAppLeads)
        {
            lead.Instance = RepairText(lead.Instance);
            lead.ConversationId = RepairText(lead.ConversationId);
            lead.CustomerName = RepairText(lead.CustomerName);
            lead.Phone = RepairText(lead.Phone);
            lead.Stage = RepairText(lead.Stage);
            lead.Summary = RepairText(lead.Summary);
            lead.Intent = RepairText(lead.Intent);
            lead.RequestedService = RepairText(lead.RequestedService);
            lead.PreferredSchedule = RepairText(lead.PreferredSchedule);
            lead.AssignedProfessional = RepairText(lead.AssignedProfessional);
            lead.PreferredDate = RepairText(lead.PreferredDate);
            lead.Period = RepairText(lead.Period);
            lead.Notes = RepairText(lead.Notes);
            for (var index = 0; index < lead.Facts.Count; index++)
            {
                lead.Facts[index] = RepairText(lead.Facts[index]);
            }
        }
    }

    private static void RepairMarketingCatalogHeader(MarketingCatalogHeader header)
    {
        header.BusinessName = RepairText(header.BusinessName);
        header.Subtitle = RepairText(header.Subtitle);
        header.ButtonText = RepairText(header.ButtonText);
        header.Background = RepairText(header.Background);
    }

    private static void RepairMarketingSitePromotion(MarketingSitePromotion promotion)
    {
        promotion.Name = RepairText(promotion.Name);
        promotion.LimitPerCustomer = Math.Clamp(promotion.LimitPerCustomer, 1, 99);
        promotion.Items ??= [];
        foreach (var item in promotion.Items)
        {
            item.ServiceId = RepairText(item.ServiceId);
            item.ServiceName = RepairText(item.ServiceName);
            item.OriginalPrice = Math.Max(0, item.OriginalPrice);
            item.PromotionalPrice = Math.Max(0, item.PromotionalPrice);
        }
    }

    private static void RepairMarketingCatalogFooter(MarketingCatalogFooter footer)
    {
        footer.BusinessName = RepairText(footer.BusinessName);
        footer.Description = RepairText(footer.Description);
        footer.Address = RepairText(footer.Address);
        footer.Phone = RepairText(footer.Phone);
        footer.Hours = RepairText(footer.Hours);
        footer.Instagram = RepairText(footer.Instagram);
        footer.WhatsApp = RepairText(footer.WhatsApp);
    }

    private static void RepairMarketingCatalogSection(MarketingCatalogSection section)
    {
        section.Id = RepairText(section.Id);
        section.Type = RepairText(section.Type);
        section.Title = RepairText(section.Title);
        section.Subtitle = RepairText(section.Subtitle);
        section.Body = RepairText(section.Body);
        section.ButtonText = RepairText(section.ButtonText);
        section.ButtonTarget = RepairText(section.ButtonTarget);
        section.Layout = RepairText(section.Layout);
        section.Background = RepairText(section.Background);
        section.Alignment = RepairText(section.Alignment);
        foreach (var item in section.Items)
        {
            item.Id = RepairText(item.Id);
            item.Title = RepairText(item.Title);
            item.Text = RepairText(item.Text);
            item.Detail = RepairText(item.Detail);
            item.ImagePath = RepairText(item.ImagePath);
        }
    }

    private static bool IsLegacyWhatsAppConnectionNotice(WhatsAppMessage message) =>
        string.Equals(message.Category, "Conexão", StringComparison.OrdinalIgnoreCase) &&
        message.Message.StartsWith(
            "WhatsApp linkado. Confirmações, retornos e mensagens dos clientes aparecem neste painel.",
            StringComparison.OrdinalIgnoreCase);

    private static string RepairText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Older seed files were once written with a replacement character in
        // these domain labels. The original accent cannot be inferred in the
        // general case, so repair only the known persisted values.
        value = value
            .Replace("Mec�nica", "Mecânica", StringComparison.Ordinal)
            .Replace("Mec�nico", "Mecânico", StringComparison.Ordinal)
            .Replace("â€“", "–", StringComparison.Ordinal)
            .Replace("â€”", "—", StringComparison.Ordinal)
            .Replace("â€™", "’", StringComparison.Ordinal)
            .Replace("â€œ", "“", StringComparison.Ordinal)
            .Replace("â€�", "”", StringComparison.Ordinal)
            .Replace("â€¢", "•", StringComparison.Ordinal)
            .Replace("â†’", "→", StringComparison.Ordinal)
            .Replace("âœ“", "✓", StringComparison.Ordinal);

        // A plain "â" is valid Portuguese (for example, "Mecânica").
        // Only the leading bytes below are reliable mojibake indicators.
        if (!value.Contains('Ã') && !value.Contains('Â'))
        {
            return value;
        }

        try
        {
            var bytes = value.Select(character => character <= byte.MaxValue ? (byte)character : (byte)'?').ToArray();
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return value;
        }
    }

    private static AgendaData CreateSeedData()
    {
        var data = new AgendaData
        {
            Settings =
            {
                AccountFullName = "Nina Almeida",
                AccountPhone = "(33) 99800-7978",
                AccountEmail = "nina@studionina.com.br",
                BusinessName = "Studio Nina Beauty",
                BusinessPhone = "(33) 99800-7978",
                BusinessAddress = "Rua das Flores, 245 - Centro",
                BusinessSegment = "Salão de beleza",
                ThemeId = "theme-3",
                ClientLabel = "Cliente",
                ClientDetailLabel = "Preferências e observações",
                ResourceLabel = "Espaço de atendimento",
                OnboardingCompleted = true,
                WorkdayStartHour = 8,
                WorkdayEndHour = 20,
                Workdays = [1, 2, 3, 4, 5, 6],
                WorkdayBreakEnabled = true,
                WorkdayBreakStartHour = 12,
                WorkdayBreakEndHour = 13,
                ProfessionalCountRange = "4-6",
                MainObjective = "Organizar agenda e aumentar recorrência",
                InstagramEnabled = true,
                InstagramLinked = true,
                InstagramUsername = "@studioninabeauty",
                InstagramDisplayName = "Studio Nina Beauty",
                WhatsAppEnabled = true,
                WhatsAppLinked = true,
                WhatsAppConnectedName = "Studio Nina",
                WhatsAppStorePhone = "5533998007978",
                MercadoPagoEnabled = true,
                MercadoPagoConnected = true,
                MercadoPagoDefaultTerminalLabel = "Recepção",
                Resources = ["Cadeira 1", "Cadeira 2", "Mesa de unhas", "Sala estética", "Lavatório"]
            }
        };

        data.Services.AddRange(
        [
            Service("Salão de beleza", "Corte feminino", 60, 95, "Cadeira 1"),
            Service("Salão de beleza", "Escova modelada", 45, 70, "Cadeira 2"),
            Service("Salão de beleza", "Coloração completa", 150, 260, "Cadeira 1"),
            Service("Salão de beleza", "Hidratação premium", 60, 110, "Lavatório"),
            Service("Salão de beleza", "Manicure", 45, 55, "Mesa de unhas"),
            Service("Salão de beleza", "Pedicure", 50, 65, "Mesa de unhas"),
            Service("Salão de beleza", "Design de sobrancelhas", 30, 48, "Sala estética"),
            Service("Salão de beleza", "Limpeza de pele", 75, 150, "Sala estética")
        ]);

        data.Professionals.AddRange(
        [
            Professional("Nina Almeida", "Cabeleireira e proprietária", "Salão de beleza"),
            Professional("Camila Rocha", "Colorista", "Salão de beleza"),
            Professional("Júlia Martins", "Manicure e pedicure", "Salão de beleza"),
            Professional("Mariana Costa", "Esteticista", "Salão de beleza"),
            Professional("Laura Freitas", "Assistente", "Salão de beleza")
        ]);

        var today = DateTime.Today;
        AddSeedAppointment(data, "Salão de beleza", "Isabela Fernandes", "Corte em camadas", "(33) 98841-2103", "Corte feminino", "Nina Almeida", "Cadeira 1", today.AddHours(9), AppointmentStatus.Confirmed, "Prefere finalizar com ondas leves.");
        AddSeedAppointment(data, "Salão de beleza", "Ana Clara Souza", "Loiro bege", "(33) 99720-4431", "Coloração completa", "Camila Rocha", "Cadeira 2", today.AddHours(10), AppointmentStatus.InService, "Fazer teste de mecha.");
        AddSeedAppointment(data, "Salão de beleza", "Beatriz Lima", "Francesinha delicada", "(33) 99115-6802", "Manicure", "Júlia Martins", "Mesa de unhas", today.AddHours(11), AppointmentStatus.Waiting, "");
        AddSeedAppointment(data, "Salão de beleza", "Renata Alves", "Pele sensível", "(33) 98472-0920", "Limpeza de pele", "Mariana Costa", "Sala estética", today.AddHours(13), AppointmentStatus.Scheduled, "Usar produtos suaves.");
        AddSeedAppointment(data, "Salão de beleza", "Carolina Mendes", "Evento à noite", "(33) 99814-3370", "Escova modelada", "Nina Almeida", "Cadeira 1", today.AddHours(15), AppointmentStatus.Confirmed, "Finalização com volume.");
        AddSeedAppointment(data, "Salão de beleza", "Fernanda Nunes", "Manutenção mensal", "(33) 98760-1198", "Design de sobrancelhas", "Mariana Costa", "Sala estética", today.AddHours(16), AppointmentStatus.Scheduled, "");
        AddSeedAppointment(data, "Salão de beleza", "Luana Ribeiro", "Cabelo ressecado", "(33) 99221-5084", "Hidratação premium", "Camila Rocha", "Lavatório", today.AddHours(17), AppointmentStatus.Scheduled, "Aplicar máscara nutritiva.");

        data.Products.AddRange([
            new ProductItem { Name = "Shampoo Nutritivo", Category = "Home care", Supplier = "Belle Pro", CostPrice = 38, Price = 79, StockQuantity = 12, MinimumStock = 4 },
            new ProductItem { Name = "Máscara Reconstrutora", Category = "Tratamento", Supplier = "Belle Pro", CostPrice = 52, Price = 109, StockQuantity = 8, MinimumStock = 3 },
            new ProductItem { Name = "Óleo Finalizador", Category = "Finalização", Supplier = "Essenza", CostPrice = 29, Price = 65, StockQuantity = 15, MinimumStock = 5 }
        ]);
        data.ManualPayments.AddRange([
            new ManualPayment { Description = "Corte e escova", CustomerName = "Isabela Fernandes", Category = "Serviços", PaymentMethod = "Pix", PaymentStatus = "approved", Value = 165, PaidAt = today.AddHours(9) },
            new ManualPayment { Description = "Manicure", CustomerName = "Beatriz Lima", Category = "Serviços", PaymentMethod = "Cartão", PaymentStatus = "approved", Value = 55, PaidAt = today.AddHours(11) }
        ]);
        data.Expenses.AddRange([
            new ExpenseItem { Description = "Reposição de cosméticos", Category = "Produtos", Supplier = "Belle Pro", PaymentMethod = "Pix", Value = 420, Date = today.AddDays(-2), IsPaid = true },
            new ExpenseItem { Description = "Internet do salão", Category = "Operacional", Supplier = "Conecta", PaymentMethod = "Débito", Value = 119.90m, Date = today.AddDays(-5), IsPaid = true }
        ]);

        return data;
    }

    private static bool IsAuditMode() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_SCREENSHOT_PATH"));

    private static AgendaData CreateCleanData() =>
        new()
        {
            Settings = new AgendaSettings
            {
                BusinessName = "Agenda Livre",
                OnboardingCompleted = false,
                WorkdayStartHour = 8,
                WorkdayEndHour = 20,
                Workdays = [1, 2, 3, 4, 5, 6],
                Resources = []
            }
        };

    private static List<string> DefaultResources() =>
    [
        "Sala 1",
        "Sala 2",
        "Sala pet",
        "Tosa 1",
        "Box 1",
        "Box 2",
        "Mesa 1",
        "Mesa 2",
        "Cadeira 1",
        "Cadeira 2",
        "Cadeira beleza"
    ];

    private static ServiceItem Service(string segment, string name, int duration, decimal price, string resource) =>
        new()
        {
            Segment = segment,
            Name = name,
            DurationMinutes = duration,
            Price = price,
            DefaultResource = resource
        };

    private static Professional Professional(string name, string role, params string[] segments) =>
        new()
        {
            Name = name,
            Role = role,
            Segments = [.. segments]
        };

    private static void AddSeedAppointment(
        AgendaData data,
        string segment,
        string customer,
        string profile,
        string phone,
        string serviceName,
        string professionalName,
        string resource,
        DateTime start,
        AppointmentStatus status,
        string notes)
    {
        var service = data.Services.FirstOrDefault(item => item.Segment == segment && item.Name == serviceName);
        var professional = data.Professionals.FirstOrDefault(item => item.Name == professionalName);

        data.Appointments.Add(new Appointment
        {
            Segment = segment,
            CustomerName = customer,
            CustomerPhone = phone,
            CustomerProfile = profile,
            ServiceId = service?.Id ?? "",
            ServiceName = serviceName,
            ProfessionalId = professional?.Id ?? "",
            ProfessionalName = professionalName,
            ResourceName = resource,
            Start = start,
            DurationMinutes = service?.DurationMinutes ?? 30,
            Price = status == AppointmentStatus.Blocked ? 0 : service?.Price ?? 0,
            Status = status,
            Notes = notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        if (status != AppointmentStatus.Blocked)
        {
            data.Customers.Add(new Customer
            {
                Name = customer,
                Phone = phone,
                Segment = segment,
                Profile = profile,
                LastSeenAt = start
            });
        }
    }
}
