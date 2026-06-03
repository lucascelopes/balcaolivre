using System.IO;
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
        DataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgendaLivre.Windows");

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
        File.WriteAllText(DataPath, JsonSerializer.Serialize(data, JsonOptions));
    }

    private static void EnsureUsableData(AgendaData data)
    {
        data.Settings ??= new AgendaSettings();
        data.Services ??= [];
        data.Professionals ??= [];
        data.Customers ??= [];
        data.Appointments ??= [];
        data.Settings.BusinessDocument ??= "";
        data.Settings.BusinessPhone ??= "";
        data.Settings.BusinessAddress ??= "";

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

        if (data.Services.Count == 0 || data.Professionals.Count == 0)
        {
            var seeded = CreateSeedData();
            if (data.Services.Count == 0)
            {
                data.Services.AddRange(seeded.Services);
            }

            if (data.Professionals.Count == 0)
            {
                data.Professionals.AddRange(seeded.Professionals);
            }
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
    }

    private static AgendaData CreateSeedData()
    {
        var data = new AgendaData
        {
            Settings =
            {
                BusinessName = "Agenda Livre",
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
