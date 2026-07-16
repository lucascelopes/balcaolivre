using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgendaLivre.Windows;

public sealed class AgendaDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AgendaDataStore()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("AGENDA_LIVRE_DATA_ROOT");
        DataRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AgendaLivre.Windows")
            : Path.GetFullPath(configuredRoot);

        DataPath = Path.Combine(DataRoot, "agenda-data.json");
    }

    public string DataRoot { get; }
    public string DataPath { get; }

    public AgendaData LoadOrCreate()
    {
        Directory.CreateDirectory(DataRoot);

        if (!File.Exists(DataPath))
        {
            var seeded = CreateSeedData();
            seeded.Settings.OnboardingCompleted = false;
            Save(seeded);
            return seeded;
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

            var seeded = CreateSeedData();
            seeded.Settings.OnboardingCompleted = false;
            Save(seeded);
            return seeded;
        }
    }

    public void Save(AgendaData data)
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
        data.Expenses ??= [];
        data.WhatsAppMessages ??= [];
        data.Settings.BusinessName ??= "Balcão Livre";
        data.Settings.BusinessDocument ??= "";
        data.Settings.BusinessPhone ??= "";
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
        foreach (var professional in data.Professionals)
        {
            professional.Segments ??= [];
        }

        RepairPersistedText(data);
        NormalizeBusinessRules(data);

        if (data.Settings.Resources.Count == 0)
        {
            data.Settings.Resources.AddRange(DefaultResources());
        }

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

        foreach (var expense in data.Expenses.Where(expense => string.IsNullOrWhiteSpace(expense.Id)))
        {
            expense.Id = Guid.NewGuid().ToString("N");
        }

        foreach (var message in data.WhatsAppMessages.Where(message => string.IsNullOrWhiteSpace(message.Id)))
        {
            message.Id = Guid.NewGuid().ToString("N");
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

        foreach (var expense in data.Expenses)
        {
            expense.Value = Math.Max(0, expense.Value);
        }
    }

    private static void RepairPersistedText(AgendaData data)
    {
        data.Settings.AccountFullName = RepairText(data.Settings.AccountFullName);
        data.Settings.AccountPhone = RepairText(data.Settings.AccountPhone);
        data.Settings.AccountEmail = RepairText(data.Settings.AccountEmail);
        data.Settings.BusinessName = RepairText(data.Settings.BusinessName);
        data.Settings.BusinessDocument = RepairText(data.Settings.BusinessDocument);
        data.Settings.BusinessPhone = RepairText(data.Settings.BusinessPhone);
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

        if (string.IsNullOrWhiteSpace(data.Settings.WhatsAppEvolutionBaseUrl)
            || data.Settings.WhatsAppEvolutionBaseUrl.Contains("/evolution-proxy", StringComparison.OrdinalIgnoreCase))
        {
            data.Settings.WhatsAppEvolutionBaseUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp";
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
            appointment.CustomerName = RepairText(appointment.CustomerName);
            appointment.CustomerPhone = RepairText(appointment.CustomerPhone);
            appointment.CustomerProfile = RepairText(appointment.CustomerProfile);
            appointment.ServiceId = RepairText(appointment.ServiceId);
            appointment.ServiceName = RepairText(appointment.ServiceName);
            appointment.ProfessionalId = RepairText(appointment.ProfessionalId);
            appointment.ProfessionalName = RepairText(appointment.ProfessionalName);
            appointment.ResourceName = RepairText(appointment.ResourceName);
            appointment.Notes = RepairText(appointment.Notes);
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
            message.CustomerName = RepairText(message.CustomerName);
            message.Phone = RepairText(message.Phone);
            message.Message = RepairText(message.Message);
            message.Direction = RepairText(message.Direction);
            message.Status = RepairText(message.Status);
            message.Category = RepairText(message.Category);
        }
    }

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
                BusinessName = "Balcão Livre",
                WorkdayStartHour = 8,
                WorkdayEndHour = 20,
                Resources = DefaultResources()
            }
        };

        data.Services.AddRange(
        [
            Service("Clínica médica", "Consulta médica", 45, 180, "Sala 1"),
            Service("Clínica médica", "Retorno", 30, 90, "Sala 1"),
            Service("Clínica médica", "Exame simples", 30, 120, "Sala 2"),
            Service("Petshop", "Banho e tosa", 90, 95, "Tosa 1"),
            Service("Petshop", "Consulta veterinária", 40, 160, "Sala pet"),
            Service("Petshop", "Vacinação", 25, 85, "Sala pet"),
            Service("Mecânica", "Diagnóstico", 60, 120, "Box 1"),
            Service("Mecânica", "Troca de óleo", 45, 90, "Box 2"),
            Service("Mecânica", "Revisão completa", 150, 420, "Box 1"),
            Service("Unha e beleza", "Manicure", 45, 55, "Mesa 1"),
            Service("Unha e beleza", "Alongamento de unha", 120, 180, "Mesa 2"),
            Service("Unha e beleza", "Sobrancelha", 30, 45, "Cadeira beleza"),
            Service("Cabelo e barbearia", "Corte masculino", 35, 45, "Cadeira 1"),
            Service("Cabelo e barbearia", "Barba", 25, 35, "Cadeira 1"),
            Service("Cabelo e barbearia", "Escova", 45, 70, "Cadeira 2"),
            Service("Cabelo e barbearia", "Coloração", 120, 240, "Cadeira 2")
        ]);

        data.Professionals.AddRange(
        [
            Professional("Dra. Ana Ribeiro", "Médica", "Clínica médica"),
            Professional("Dr. Marcos Leal", "Clínico", "Clínica médica"),
            Professional("Bruno Vet", "Veterinário", "Petshop"),
            Professional("Camila Pet", "Banho e tosa", "Petshop"),
            Professional("Carlos Oficina", "Mecânico", "Mecânica"),
            Professional("Rafa Diagnóstico", "Mecânico", "Mecânica"),
            Professional("Duda Nails", "Manicure", "Unha e beleza"),
            Professional("Nay Beauty", "Designer", "Unha e beleza"),
            Professional("Leo Barber", "Barbeiro", "Cabelo e barbearia"),
            Professional("Marta Hair", "Cabeleireira", "Cabelo e barbearia")
        ]);

        var today = DateTime.Today;
        AddSeedAppointment(data, "Clínica médica", "Maria Souza", "Paciente 0321", "(11) 98888-1001", "Consulta médica", "Dra. Ana Ribeiro", "Sala 1", today.AddHours(9), AppointmentStatus.Confirmed, "Primeira consulta.");
        AddSeedAppointment(data, "Petshop", "Nina / Tutor João", "Spitz, banho especial", "(11) 97777-2002", "Banho e tosa", "Camila Pet", "Tosa 1", today.AddHours(10), AppointmentStatus.Scheduled, "Usar shampoo hipoalergênico.");
        AddSeedAppointment(data, "Mecânica", "Fiat Argo - Lucas", "Placa BRA2E26", "(11) 96666-3003", "Diagnóstico", "Carlos Oficina", "Box 1", today.AddHours(13), AppointmentStatus.Waiting, "Cliente relatou barulho na suspensão.");
        AddSeedAppointment(data, "Unha e beleza", "Patrícia Lima", "Francesinha", "(11) 95555-4004", "Manicure", "Duda Nails", "Mesa 1", today.AddHours(15), AppointmentStatus.Scheduled, "");
        AddSeedAppointment(data, "Cabelo e barbearia", "André Costa", "Degradê baixo", "(11) 94444-5005", "Corte masculino", "Leo Barber", "Cadeira 1", today.AddHours(17), AppointmentStatus.Confirmed, "");
        AddSeedAppointment(data, "Clínica médica", "Horário bloqueado", "Reunião interna", "", "Bloqueio interno", "Dr. Marcos Leal", "Sala 2", today.AddDays(1).AddHours(11), AppointmentStatus.Blocked, "Treinamento da equipe.");

        return data;
    }

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
