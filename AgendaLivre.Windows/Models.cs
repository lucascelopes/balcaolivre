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
    public List<CustomerReceivable> CustomerReceivables { get; set; } = [];
    public List<ExpenseItem> Expenses { get; set; } = [];
    public List<WhatsAppMessage> WhatsAppMessages { get; set; } = [];
    public List<WhatsAppLead> WhatsAppLeads { get; set; } = [];
}

public sealed class AgendaSettings
{
    public string AccountFullName { get; set; } = "";
    public string AccountPhone { get; set; } = "";
    public string AccountEmail { get; set; } = "";
    public string BusinessName { get; set; } = "Balcão Livre";
    public string BusinessLogoPath { get; set; } = "";
    public string BusinessDocument { get; set; } = "";
    public string BusinessPhone { get; set; } = "";
    public string PixKey { get; set; } = "";
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
    public decimal MonthlyRevenueGoal { get; set; } = 2000m;
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
    public string WhatsAppEvolutionBaseUrl { get; set; } = "https://hzvplpotsdzxygkxrgyi.functions.supabase.co/functions/v1/whatsapp";
    [JsonIgnore]
    public string WhatsAppEvolutionApiKey { get; set; } = "";
    public string WhatsAppEvolutionInstanceName { get; set; } = "agenda-livre";
    public string WhatsAppEvolutionState { get; set; } = "";
    public string WhatsAppEvolutionQrBase64 { get; set; } = "";
    public DateTime? WhatsAppEvolutionLastCheckedAt { get; set; }
    public string PublicBookingSlug { get; set; } = "";
    public string PublicBookingUrl { get; set; } = "";
    public string PublicBookingApiUrl { get; set; } = "https://minhaagendalivre.com.br";
    public DateTime? PublicBookingLastSyncAt { get; set; }
    public string PublicBookingCustomDomain { get; set; } = "";
    public string PublicBookingCustomDomainStatus { get; set; } = "";
    public string PublicBookingCustomDomainProviderStatus { get; set; } = "";
    public string PublicBookingCustomDomainSslStatus { get; set; } = "";
    public string PublicBookingCustomDomainCnameTarget { get; set; } = "";
    public string PublicBookingCustomDomainValidationRecordName { get; set; } = "";
    public string PublicBookingCustomDomainValidationRecordType { get; set; } = "";
    public string PublicBookingCustomDomainValidationRecordValue { get; set; } = "";
    public string PublicBookingCustomDomainLastError { get; set; } = "";
    public string MarketingSiteDraftSlug { get; set; } = "";
    public string MarketingSiteDraftCustomDomain { get; set; } = "";
    public string MarketingSiteTitle { get; set; } = "Sua beleza, do seu jeito";
    public string MarketingSiteSupportText { get; set; } = "Realce sua essência com cuidados personalizados para você se sentir incrível todos os dias.";
    public string MarketingSiteButtonText { get; set; } = "Agendar agora";
    public string MarketingSiteHeroImagePath { get; set; } = "";
    public string MarketingSiteAccentColor { get; set; } = "#FF6B4A";
    public string MarketingSiteAlignment { get; set; } = "left";
    public string MarketingSiteSpacing { get; set; } = "compact";
    public string MarketingSiteTitleFont { get; set; } = "Georgia";
    public double MarketingSiteImageContrast { get; set; } = 64;
    public bool MarketingSiteShowButton { get; set; } = true;
    public MarketingCatalogHeader MarketingSiteHeader { get; set; } = new();
    public MarketingCatalogFooter MarketingSiteFooter { get; set; } = new();
    public MarketingCatalogDesign MarketingSiteDesign { get; set; } = new();
    public List<MarketingCatalogSection> MarketingSiteSections { get; set; } = [];
    public string MarketingSiteSeoTitle { get; set; } = "";
    public string MarketingSiteSeoDescription { get; set; } = "";
    public int MarketingSiteBuilderVersion { get; set; }
    public DateTime? MarketingSitePublishedAt { get; set; }
    public MarketingSitePromotion MarketingSitePromotion { get; set; } = new();
    public MarketingCatalogPublication? PublishedMarketingCatalog { get; set; }
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

public sealed class MarketingCatalogPublication
{
    public int AddressSnapshotVersion { get; set; }
    public string Slug { get; set; } = "";
    public string CustomDomain { get; set; } = "";
    public string Title { get; set; } = "Sua beleza, do seu jeito";
    public string SupportText { get; set; } = "";
    public string ButtonText { get; set; } = "Agendar agora";
    public string HeroImagePath { get; set; } = "";
    public string AccentColor { get; set; } = "#FF6B4A";
    public string Alignment { get; set; } = "left";
    public string Spacing { get; set; } = "compact";
    public string TitleFont { get; set; } = "Georgia";
    public double ImageContrast { get; set; } = 64;
    public bool ShowButton { get; set; } = true;
    public MarketingCatalogHeader Header { get; set; } = new();
    public MarketingCatalogFooter Footer { get; set; } = new();
    public MarketingCatalogDesign Design { get; set; } = new();
    public List<MarketingCatalogSection> Sections { get; set; } = [];
    public string SeoTitle { get; set; } = "";
    public string SeoDescription { get; set; } = "";
    public MarketingSitePromotion? Promotion { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class MarketingSitePromotion
{
    public string Name { get; set; } = "Semana do autocuidado";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    public int LimitPerCustomer { get; set; } = 1;
    public bool HighlightInCatalog { get; set; } = true;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<MarketingSitePromotionItem> Items { get; set; } = [];
}

public sealed class MarketingSitePromotionItem
{
    public string ServiceId { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public decimal OriginalPrice { get; set; }
    public decimal PromotionalPrice { get; set; }
}

public sealed class MarketingCatalogHeader
{
    public string BusinessName { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string ButtonText { get; set; } = "Agendar agora";
    public bool ShowLogo { get; set; } = true;
    public bool ShowNavigation { get; set; } = true;
    public bool ShowButton { get; set; } = true;
    public bool Sticky { get; set; } = true;
    public string Background { get; set; } = "solid";
}

public sealed class MarketingCatalogFooter
{
    public string BusinessName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Hours { get; set; } = "";
    public string Instagram { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public bool ShowContact { get; set; } = true;
    public bool ShowHours { get; set; } = true;
    public bool ShowSocial { get; set; } = true;
}

public sealed class MarketingCatalogDesign
{
    public string ColorScheme { get; set; } = "warm";
    public string ButtonStyle { get; set; } = "rounded";
    public string CornerStyle { get; set; } = "rounded";
    public string ContentWidth { get; set; } = "standard";
}

public sealed class MarketingCatalogSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "benefits";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Body { get; set; } = "";
    public string ButtonText { get; set; } = "";
    public string ButtonTarget { get; set; } = "booking";
    public string Layout { get; set; } = "cards";
    public string Background { get; set; } = "light";
    public string Alignment { get; set; } = "left";
    public bool Enabled { get; set; } = true;
    public bool AutomaticContent { get; set; }
    public List<MarketingCatalogSectionItem> Items { get; set; } = [];
}

public sealed class MarketingCatalogSectionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string Detail { get; set; } = "";
    public string ImagePath { get; set; } = "";
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

public sealed class CustomerReceivable
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string AppointmentId { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal OriginalValue { get; set; }
    public decimal RemainingValue { get; set; }
    public string Status { get; set; } = "open";
    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentProvider { get; set; } = "";
    public string PaymentReference { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string Notes { get; set; } = "";
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
    public string CustomerId { get; set; } = "";
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
    public DateTime? ServiceStartedAt { get; set; }
    public int ServiceElapsedSeconds { get; set; }
    public bool ServiceTimerPaused { get; set; }
    public List<AppointmentServiceLine> ServiceLines { get; set; } = [];
    public List<AppointmentProductLine> ProductLines { get; set; } = [];
    public DateTime? ProductSalesRecordedAt { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentProvider { get; set; } = "";
    public string PaymentReference { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string Notes { get; set; } = "";
    public string ExternalSource { get; set; } = "";
    public string ExternalReference { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public DateTime End => Start.AddMinutes(DurationMinutes);
}

public sealed class AppointmentServiceLine
{
    public string ServiceId { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string Segment { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public int DurationMinutes { get; set; } = 30;
    public decimal UnitPrice { get; set; }

    [JsonIgnore]
    public decimal Total => Math.Max(0, Quantity) * Math.Max(0, UnitPrice);

    [JsonIgnore]
    public int TotalDurationMinutes => Math.Max(0, Quantity) * Math.Max(0, DurationMinutes);
}

public sealed class AppointmentProductLine
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    [JsonIgnore]
    public decimal Total => Math.Max(0, Quantity) * Math.Max(0, UnitPrice);
}

public sealed class WhatsAppMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientRequestId { get; set; } = "";
    public string ProviderMessageId { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Instance { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string LeadId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Message { get; set; } = "";
    public string Direction { get; set; } = "saida";
    public string Type { get; set; } = "text";
    public string Kind { get; set; } = "";
    public string Status { get; set; } = "criado";
    public string Category { get; set; } = "Atendimento";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public sealed class WhatsAppLead
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Instance { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Stage { get; set; } = "new";
    public int Score { get; set; }
    public string Summary { get; set; } = "";
    public List<string> Facts { get; set; } = [];
    public string Intent { get; set; } = "";
    public string RequestedService { get; set; } = "";
    public string PreferredSchedule { get; set; } = "";
    public string AssignedProfessional { get; set; } = "";
    public string PreferredDate { get; set; } = "";
    public string Period { get; set; } = "";
    public bool Unread { get; set; }
    public int UnreadCount { get; set; }
    public int FollowupCount { get; set; }
    public DateTime? NextFollowupAt { get; set; }
    public DateTime? LastInboundAt { get; set; }
    public DateTime? LastOutboundAt { get; set; }
    public DateTime? OptedOutAt { get; set; }
    public DateTime? HandedOffAt { get; set; }
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? LastMessageAt { get; set; }
}
