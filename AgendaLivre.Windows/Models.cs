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
    public string ClientLabel { get; set; } = "Cliente";
    public string ClientDetailLabel { get; set; } = "Paciente / pet / veículo / preferência";
    public string ResourceLabel { get; set; } = "Sala, box ou cadeira";
    public bool OnboardingCompleted { get; set; } = true;
    public int WorkdayStartHour { get; set; } = 8;
    public int WorkdayEndHour { get; set; } = 20;
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
}

public sealed class ServiceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Segment { get; set; } = "";
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; } = 30;
    public decimal Price { get; set; }
    public string DefaultResource { get; set; } = "";

    [JsonIgnore]
    public string DisplayName => $"{Name} - {DurationMinutes} min";
}

public sealed class Professional
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<string> Segments { get; set; } = [];
    public string Role { get; set; } = "";

    [JsonIgnore]
    public string SegmentLine => Segments.Count == 0 ? Role : $"{Role} | {string.Join(", ", Segments)}";
}

public sealed class Customer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Segment { get; set; } = "";
    public string Profile { get; set; } = "";
    public DateTime LastSeenAt { get; set; } = DateTime.Now;
}

public sealed class ProductItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
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
    public DateTime SoldAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public decimal Total => Quantity * UnitPrice;
}

public sealed class ManualPayment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Description { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal Value { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.Now;
}

public sealed class ExpenseItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
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
