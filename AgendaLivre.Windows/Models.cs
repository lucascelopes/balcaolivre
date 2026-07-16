using System.Text.Json.Serialization;

namespace AgendaLivre.Windows;

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    Waiting,
    InService,
    Done,
    Cancelled,
    NoShow,
    Blocked
}

public sealed class AgendaData
{
    public AgendaSettings Settings { get; set; } = new();
    public List<ServiceItem> Services { get; set; } = [];
    public List<Professional> Professionals { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
    public List<ProductItem> Products { get; set; } = [];
    public List<ProductSale> ProductSales { get; set; } = [];
    public List<ManualPayment> ManualPayments { get; set; } = [];
    public List<ExpenseItem> Expenses { get; set; } = [];
    public List<WhatsAppMessage> WhatsAppMessages { get; set; } = [];
}

public sealed class AgendaSettings
{
    public string AccountFullName { get; set; } = "";
    public string AccountPhone { get; set; } = "";
    public string AccountEmail { get; set; } = "";
    public string BusinessName { get; set; } = "Balcão Livre";
    public string BusinessDocument { get; set; } = "";
    public string BusinessPhone { get; set; } = "";
    public string BusinessAddress { get; set; } = "";
    public string BusinessSegment { get; set; } = "";
    public string ThemeId { get; set; } = "";
    public string ClientLabel { get; set; } = "Cliente";
    public string ClientDetailLabel { get; set; } = "Paciente / pet / veículo / preferência";
    public string ResourceLabel { get; set; } = "Sala, box ou cadeira";
    public bool OnboardingCompleted { get; set; } = true;
    public int WorkdayStartHour { get; set; } = 8;
    public int WorkdayEndHour { get; set; } = 20;
    public List<int> Workdays { get; set; } = [1, 2, 3, 4, 5, 6];
    public bool WorkdayBreakEnabled { get; set; } = true;
    public int WorkdayBreakStartHour { get; set; } = 12;
    public int WorkdayBreakEndHour { get; set; } = 13;
    public List<string> Resources { get; set; } = [];
    public string ProfessionalCountRange { get; set; } = "";
    public string MainObjective { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Neighborhood { get; set; } = "";
    public string Street { get; set; } = "";
    public string AddressNumber { get; set; } = "";
    public string AddressComplement { get; set; } = "";
    public string AccountPasswordHash { get; set; } = "";
    public DateTime AccountCreatedAt { get; set; } = DateTime.MinValue;
    public bool WhatsAppEnabled { get; set; } = true;
    public bool WhatsAppLinked { get; set; }
    public string WhatsAppStorePhone { get; set; } = "";
    public string WhatsAppConnectedName { get; set; } = "";
    public DateTime? WhatsAppLinkedAt { get; set; }
    public DateTime? WhatsAppLastMessageAt { get; set; }
    public bool WhatsAppAutoConfirmationsEnabled { get; set; } = true;
    public string WhatsAppEvolutionBaseUrl { get; set; } = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp";
    [JsonIgnore]
    public string WhatsAppEvolutionApiKey { get; set; } = "";
    public string WhatsAppEvolutionInstanceName { get; set; } = "agenda-livre";
    public string WhatsAppEvolutionState { get; set; } = "";
    public string WhatsAppEvolutionQrBase64 { get; set; } = "";
    public DateTime? WhatsAppEvolutionLastCheckedAt { get; set; }
    public bool InstagramEnabled { get; set; } = true;
    public bool InstagramLinked { get; set; }
    public string InstagramApiUrl { get; set; } = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/instagram";
    public string InstagramUsername { get; set; } = "";
    public string InstagramDisplayName { get; set; } = "";
    public string InstagramAccountId { get; set; } = "";
    public string InstagramState { get; set; } = "";
    public string InstagramLastError { get; set; } = "";
    public DateTime? InstagramLinkedAt { get; set; }
    public DateTime? InstagramLastCheckedAt { get; set; }
    public bool MercadoPagoEnabled { get; set; }
    public bool MercadoPagoConnected { get; set; }
    public string MercadoPagoLicenseKey { get; set; } = "";
    public string MercadoPagoPaymentsApiUrl { get; set; } = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments";
    public string MercadoPagoSellerUserId { get; set; } = "";
    public string MercadoPagoDefaultTerminalId { get; set; } = "";
    public string MercadoPagoDefaultTerminalLabel { get; set; } = "";
    public string MercadoPagoLastError { get; set; } = "";
    public DateTime? MercadoPagoLastSyncAt { get; set; }
}

public sealed class ServiceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Segment { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public int DurationMinutes { get; set; } = 30;
    public int PreparationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public decimal Price { get; set; }
    public decimal CommissionPercent { get; set; }
    public string DefaultResource { get; set; } = "";
    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public string DisplayName => $"{Name} - {DurationMinutes} min";

    public override string ToString() => DisplayName;
}

public sealed class Professional
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<string> Segments { get; set; } = [];
    public string Role { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Document { get; set; } = "";
    public decimal CommissionPercent { get; set; }
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public string SegmentLine => Segments.Count == 0 ? Role : $"{Role} | {string.Join(", ", Segments)}";

    public override string ToString() => Name;
}

public sealed class Customer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Document { get; set; } = "";
    public string Segment { get; set; } = "";
    public string Profile { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool AcceptsWhatsApp { get; set; } = true;
    public DateTime LastSeenAt { get; set; } = DateTime.Now;
}

public sealed class ProductItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Supplier { get; set; } = "";
    public decimal CostPrice { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int MinimumStock { get; set; }
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class ProductSale
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentProvider { get; set; } = "";
    public string PaymentReference { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime SoldAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public decimal Total => Math.Max(0, (Quantity * UnitPrice) - Discount);
}

public sealed class ManualPayment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Description { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Category { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentProvider { get; set; } = "";
    public string PaymentReference { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal Value { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.Now;
}

public sealed class ExpenseItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal Value { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public bool IsPaid { get; set; } = true;
}

public sealed class Appointment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Segment { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerProfile { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string ProfessionalId { get; set; } = "";
    public string ProfessionalName { get; set; } = "";
    public string ResourceName { get; set; } = "";
    public DateTime Start { get; set; } = DateTime.Now;
    public int DurationMinutes { get; set; } = 30;
    public decimal Price { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public DateTime End => Start.AddMinutes(DurationMinutes);
}

public sealed class WhatsAppMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Message { get; set; } = "";
    public string Direction { get; set; } = "saida";
    public string Status { get; set; } = "criado";
    public string Category { get; set; } = "Atendimento";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
}
