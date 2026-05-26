using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});
var adminUrls = Environment.GetEnvironmentVariable("BVPDV_ADMIN_URLS");
if (string.IsNullOrWhiteSpace(adminUrls))
{
    var renderPort = Environment.GetEnvironmentVariable("PORT");
    adminUrls = string.IsNullOrWhiteSpace(renderPort)
        ? "http://localhost:5188"
        : $"http://0.0.0.0:{renderPort}";
}

builder.WebHost.UseUrls(adminUrls);

builder.Services.AddSingleton<AdminStoreService>();
builder.Services.AddSingleton<AdminSessionService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", (AdminStoreService store) => Results.Ok(new { ok = true, app = "Balcao Livre PDV Admin", version = "1.2.2026", storage = store.StorageMode }));

app.MapPost("/api/login", async (HttpContext context, AdminSessionService sessions) =>
{
    var request = await context.Request.ReadFromJsonAsync<LoginRequest>(AdminJson.Options) ?? new LoginRequest();
    var user = Environment.GetEnvironmentVariable("BVPDV_ADMIN_USER") ?? "balcaoVirtualPDV";
    var password = Environment.GetEnvironmentVariable("BVPDV_ADMIN_PASSWORD") ?? "BVPDV24055";

    if (!string.Equals(request.User, user, StringComparison.Ordinal) ||
        !string.Equals(request.Password, password, StringComparison.Ordinal))
    {
        return Results.Json(new { ok = false, message = "Login ou senha invalidos." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = sessions.Create();
    context.Response.Cookies.Append(AdminSessionService.CookieName, token, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = false,
        Expires = DateTimeOffset.UtcNow.AddHours(12)
    });

    return Results.Ok(new { ok = true, user });
});

app.MapPost("/api/logout", (HttpContext context, AdminSessionService sessions) =>
{
    if (context.Request.Cookies.TryGetValue(AdminSessionService.CookieName, out var token))
    {
        sessions.Remove(token);
    }

    context.Response.Cookies.Delete(AdminSessionService.CookieName);
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/session", (HttpContext context, AdminSessionService sessions) =>
{
    return Results.Ok(new { authenticated = sessions.IsValid(context) });
});

app.MapGet("/api/dashboard", (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var snapshot = store.Read();
    return Results.Ok(AdminDashboard.From(snapshot));
});

app.MapGet("/api/licenses", (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    return Results.Ok(store.Read().Licenses.OrderByDescending(item => item.CreatedAt));
});

app.MapPost("/api/licenses", async (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var request = await context.Request.ReadFromJsonAsync<CreateLicenseRequest>(AdminJson.Options) ?? new CreateLicenseRequest();
    var amount = Math.Clamp(request.Amount <= 0 ? 30 : request.Amount, 1, 3650);
    var unit = NormalizeDurationUnit(request.Unit);
    var now = DateTimeOffset.UtcNow;
    var expiresAt = AddDuration(now, amount, unit);
    var key = LicenseKeyFactory.Create(expiresAt);

    var license = new LicenseRecord
    {
        Id = Guid.NewGuid().ToString("N"),
        Key = key,
        Plan = request.Plan.TrimOrDefault($"{amount} {DurationLabel(unit, amount)}"),
        CustomerName = request.CustomerName.TrimOrDefault("Cliente sem nome"),
        Notes = request.Notes.Trim(),
        Status = LicenseStatus.Available,
        CreatedAt = now,
        ExpiresAt = expiresAt,
        PeriodAmount = amount,
        PeriodUnit = unit
    };

    store.Update(data =>
    {
        data.Licenses.Add(license);
        data.Events.Add(AdminEvent.License("license.created", $"Chave criada para {license.CustomerName}", license.Key));
        return true;
    });

    return Results.Ok(license);
});

app.MapPost("/api/licenses/{id}/block", (string id, HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var result = store.Update(data =>
    {
        var license = data.Licenses.FirstOrDefault(item => item.Id == id || string.Equals(item.Key, id, StringComparison.OrdinalIgnoreCase));
        if (license is null) return null;
        license.Status = LicenseStatus.Blocked;
        data.Events.Add(AdminEvent.License("license.blocked", $"Chave bloqueada: {license.CustomerName}", license.Key));
        return license;
    });
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/licenses/{id}/unblock", (string id, HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var result = store.Update(data =>
    {
        var license = data.Licenses.FirstOrDefault(item => item.Id == id || string.Equals(item.Key, id, StringComparison.OrdinalIgnoreCase));
        if (license is null) return null;
        license.Status = string.IsNullOrWhiteSpace(license.MachineHash) ? LicenseStatus.Available : LicenseStatus.Active;
        data.Events.Add(AdminEvent.License("license.unblocked", $"Chave desbloqueada: {license.CustomerName}", license.Key));
        return license;
    });
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/app/activate", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(AdminActivationResponse.Deny("Chave e computador sao obrigatorios."), statusCode: StatusCodes.Status400BadRequest);
    }

    var response = store.Update(data =>
    {
        var now = DateTimeOffset.UtcNow;
        var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
        if (license is null)
        {
            data.Events.Add(AdminEvent.Device("activation.denied", "Tentativa com chave nao criada no admin", request));
            return AdminActivationResponse.Deny("Chave nao existe no painel admin. Gere uma chave nova.");
        }

        LicenseTools.RefreshLicenseStatus(license, now);
        if (license.Status == LicenseStatus.Blocked)
        {
            data.Events.Add(AdminEvent.License("activation.blocked", "Ativacao bloqueada", license.Key));
            return AdminActivationResponse.Deny("Esta chave esta bloqueada.");
        }

        if (license.ExpiresAt <= now)
        {
            license.Status = LicenseStatus.Expired;
            data.Events.Add(AdminEvent.License("activation.expired", "Tentativa com chave expirada", license.Key));
            return AdminActivationResponse.Deny("Esta chave esta expirada.");
        }

        var mobileClient = IsMobileClient(request);
        if (!string.IsNullOrWhiteSpace(license.MachineHash) &&
            !string.Equals(license.MachineHash, request.MachineHash, StringComparison.Ordinal) &&
            !mobileClient)
        {
            data.Events.Add(AdminEvent.License("activation.used_other_pc", "Chave ja vinculada a outro PC", license.Key));
            return AdminActivationResponse.Deny("Esta chave ja foi usada em outro computador.");
        }

        license.Status = LicenseStatus.Active;
        if (!mobileClient || string.IsNullOrWhiteSpace(license.MachineHash))
        {
            license.MachineHash = request.MachineHash;
            license.MachineCode = request.MachineCode;
        }
        license.ActivatedAt ??= now;
        license.LastSeenAt = now;
        license.AppVersion = request.AppVersion;
        license.CustomerName = request.Profile.BusinessName.TrimOrDefault(license.CustomerName);
        license.BusinessName = request.Profile.BusinessName;
        license.Cnpj = request.Profile.Cnpj;
        license.OwnerName = request.Profile.OwnerName;
        license.Phone = request.Profile.Phone;
        license.City = request.Profile.City;
        license.State = request.Profile.State;
        license.ConfigSnapshot = request.Settings;
        license.MetricsSnapshot = request.Metrics;

        UpsertDevice(data, request, now);
        data.Events.Add(AdminEvent.License("activation.ok", $"Ativacao: {license.CustomerName}", license.Key));
        return AdminActivationResponse.Allow("Chave ativada.", license.Plan, license.ExpiresAt);
    });

    return Results.Ok(response);
});

app.MapPost("/api/app/checkin", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    if (string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(new { ok = false, message = "Computador obrigatorio." }, statusCode: StatusCodes.Status400BadRequest);
    }

    store.Update(data =>
    {
        var now = DateTimeOffset.UtcNow;
        var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
        if (license is not null)
        {
            LicenseTools.RefreshLicenseStatus(license, now);
            license.LastSeenAt = now;
            license.AppVersion = request.AppVersion;
            license.BusinessName = request.Profile.BusinessName;
            license.Cnpj = request.Profile.Cnpj;
            license.OwnerName = request.Profile.OwnerName;
            license.Phone = request.Profile.Phone;
            license.City = request.Profile.City;
            license.State = request.Profile.State;
            license.ConfigSnapshot = request.Settings;
            license.MetricsSnapshot = request.Metrics;
        }

        UpsertDevice(data, request, now);
        data.Events.Add(AdminEvent.Device("device.checkin", $"Check-in {request.Profile.BusinessName.TrimOrDefault(request.MachineCode)}", request));
        return true;
    });

    return Results.Ok(new { ok = true });
});

app.MapPost("/api/app/sync", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppSyncPayload>(AdminJson.Options) ?? new AppSyncPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(new { ok = false, message = "Chave e computador sao obrigatorios." }, statusCode: StatusCodes.Status400BadRequest);
    }

    var accepted = MarkAppPayloadSeen(store, request, "sync.central", "Sync central recebido");
    if (!accepted)
    {
        return Results.Json(new { ok = false, message = "Chave sem permissao para sync." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var saved = store.SaveClientObject(
        "sync",
        request.LicenseKey,
        request.MachineHash,
        "latest.json",
        JsonSerializer.Serialize(request, AdminJson.Options),
        upsert: true);

    return Results.Ok(new { ok = true, saved, mode = store.StorageMode });
});

app.MapPost("/api/app/backup", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppBackupPayload>(AdminJson.Options) ?? new AppBackupPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(new { ok = false, message = "Chave e computador sao obrigatorios." }, statusCode: StatusCodes.Status400BadRequest);
    }

    var accepted = MarkAppPayloadSeen(store, request, "backup.received", "Backup versionado recebido");
    if (!accepted)
    {
        return Results.Json(new { ok = false, message = "Chave sem permissao para backup." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{ShortStableId(request.MachineHash)}.json";
    var saved = store.SaveClientObject(
        "backups",
        request.LicenseKey,
        request.MachineHash,
        fileName,
        JsonSerializer.Serialize(request, AdminJson.Options),
        upsert: false);

    return Results.Ok(new { ok = true, saved, fileName, mode = store.StorageMode });
});

app.MapFallbackToFile("index.html");
app.Run();

static bool MarkAppPayloadSeen(AdminStoreService store, AppClientPayload request, string eventType, string eventMessage)
{
    return store.Update(data =>
    {
        var now = DateTimeOffset.UtcNow;
        var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
        if (license is null)
        {
            data.Events.Add(AdminEvent.Device($"{eventType}.denied", $"{eventMessage}: chave nao encontrada", request));
            return false;
        }

        LicenseTools.RefreshLicenseStatus(license, now);
        if (license.Status == LicenseStatus.Blocked || license.Status == LicenseStatus.Expired)
        {
            data.Events.Add(AdminEvent.License($"{eventType}.blocked", $"{eventMessage}: chave {license.Status}", license.Key));
            return false;
        }

        if (!string.IsNullOrWhiteSpace(license.MachineHash) &&
            !string.Equals(license.MachineHash, request.MachineHash, StringComparison.Ordinal) &&
            !IsMobileClient(request))
        {
            data.Events.Add(AdminEvent.License($"{eventType}.other_pc", $"{eventMessage}: computador diferente", license.Key));
            return false;
        }

        if (string.IsNullOrWhiteSpace(license.MachineHash) && !IsMobileClient(request))
        {
            license.MachineHash = request.MachineHash;
            license.MachineCode = request.MachineCode;
            license.ActivatedAt ??= now;
        }

        license.Status = LicenseStatus.Active;
        license.LastSeenAt = now;
        license.AppVersion = request.AppVersion;
        license.BusinessName = request.Profile.BusinessName;
        license.Cnpj = request.Profile.Cnpj;
        license.OwnerName = request.Profile.OwnerName;
        license.Phone = request.Profile.Phone;
        license.City = request.Profile.City;
        license.State = request.Profile.State;
        license.ConfigSnapshot = request.Settings;
        license.MetricsSnapshot = request.Metrics;

        UpsertDevice(data, request, now);
        data.Events.Add(AdminEvent.Device(eventType, $"{eventMessage}: {request.Profile.BusinessName.TrimOrDefault(request.MachineCode)}", request));
        return true;
    });
}

static void UpsertDevice(AdminStore data, AppClientPayload request, DateTimeOffset now)
{
    var device = data.Devices.FirstOrDefault(item => string.Equals(item.MachineHash, request.MachineHash, StringComparison.Ordinal));
    if (device is null)
    {
        device = new DeviceRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            MachineHash = request.MachineHash,
            MachineCode = request.MachineCode,
            FirstSeenAt = now
        };
        data.Devices.Add(device);
    }

    device.LastSeenAt = now;
    device.LicenseKey = request.LicenseKey;
    device.ClientKind = NormalizeClientKind(request.ClientKind);
    device.AppVersion = request.AppVersion;
    device.Profile = request.Profile;
    device.Settings = request.Settings;
    device.Metrics = request.Metrics;
}

static bool IsMobileClient(AppClientPayload request)
{
    return string.Equals(NormalizeClientKind(request.ClientKind), "android", StringComparison.Ordinal)
        || request.MachineCode.StartsWith("AND-", StringComparison.OrdinalIgnoreCase);
}

static string NormalizeClientKind(string? value)
{
    var clean = (value ?? "").Trim().ToLowerInvariant();
    return string.IsNullOrWhiteSpace(clean) ? "windows" : clean;
}

static string ShortStableId(string value)
{
    using var sha = SHA256.Create();
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes)[..10].ToLowerInvariant();
}

static string NormalizeDurationUnit(string? unit)
{
    return (unit ?? "days").Trim().ToLowerInvariant() switch
    {
        "minute" or "minutes" or "minutos" or "min" => "minutes",
        "month" or "months" or "mes" or "meses" => "months",
        "year" or "years" or "ano" or "anos" => "years",
        _ => "days"
    };
}

static string DurationLabel(string unit, int amount)
{
    return unit switch
    {
        "minutes" => amount == 1 ? "minuto" : "minutos",
        "months" => amount == 1 ? "mes" : "meses",
        "years" => amount == 1 ? "ano" : "anos",
        _ => amount == 1 ? "dia" : "dias"
    };
}

static DateTimeOffset AddDuration(DateTimeOffset start, int amount, string unit)
{
    return unit switch
    {
        "minutes" => start.AddMinutes(amount),
        "months" => start.AddMonths(amount),
        "years" => start.AddYears(amount),
        _ => start.AddDays(amount)
    };
}

static class AdminJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

static class StringExtensions
{
    public static string TrimOrDefault(this string? value, string fallback)
    {
        var trimmed = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }
}

static class LicenseStatus
{
    public const string Available = "DISPONIVEL";
    public const string Active = "ATIVA";
    public const string Expired = "EXPIRADA";
    public const string Blocked = "BLOQUEADA";
}

static class LicenseTools
{
    public static void RefreshLicenseStatus(LicenseRecord license, DateTimeOffset now)
    {
        if (license.Status == LicenseStatus.Blocked)
        {
            return;
        }

        if (license.ExpiresAt <= now)
        {
            license.Status = LicenseStatus.Expired;
            return;
        }

        license.Status = string.IsNullOrWhiteSpace(license.MachineHash) ? LicenseStatus.Available : LicenseStatus.Active;
    }
}

static class LicenseKeyFactory
{
    private const string Secret = "BalcaoLivrePDV-local-license-v1";

    public static string Create(DateTimeOffset expiresAt)
    {
        var expiration = expiresAt.LocalDateTime.ToString("yyyyMMddHHmm");
        var serial = Convert.ToHexString(RandomNumberGenerator.GetBytes(4))[..8];
        var signature = SignV2(expiration, serial);
        return $"BLV-{expiration}-{serial}-{signature}";
    }

    public static string Normalize(string? value)
    {
        return (value ?? "")
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("_", "-", StringComparison.Ordinal);
    }

    private static string SignV2(string expiration, string serial)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"BLV|{expiration}|{serial}"));
        return Convert.ToHexString(bytes)[..10];
    }
}

sealed class AdminSessionService
{
    public const string CookieName = "bvpdv_admin";
    private readonly Dictionary<string, DateTimeOffset> _sessions = new();
    private readonly object _gate = new();

    public string Create()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        lock (_gate)
        {
            _sessions[token] = DateTimeOffset.UtcNow.AddHours(12);
        }
        return token;
    }

    public bool IsValid(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var token) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_gate)
        {
            if (!_sessions.TryGetValue(token, out var expiresAt))
            {
                return false;
            }

            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                _sessions.Remove(token);
                return false;
            }

            _sessions[token] = DateTimeOffset.UtcNow.AddHours(12);
            return true;
        }
    }

    public void Remove(string token)
    {
        lock (_gate)
        {
            _sessions.Remove(token);
        }
    }
}

sealed class AdminStoreService
{
    private readonly string _dataRoot;
    private readonly string _filePath;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private readonly string _supabaseBucket;
    private readonly string _supabaseObjectPath;
    private readonly HttpClient _httpClient = new();
    private readonly object _gate = new();
    private bool _lastSupabaseOk;

    public string StorageMode => UsesSupabase ? (_lastSupabaseOk ? "supabase" : "supabase-pendente") : "local-json";
    private bool UsesSupabase => !string.IsNullOrWhiteSpace(_supabaseUrl) && !string.IsNullOrWhiteSpace(_supabaseKey);

    public AdminStoreService(IWebHostEnvironment environment)
    {
        _dataRoot = Environment.GetEnvironmentVariable("BVPDV_ADMIN_DATA")
            ?? Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(_dataRoot);
        _filePath = Path.Combine(_dataRoot, "admin-store.json");
        _supabaseUrl = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_URL")
            ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? "").Trim().TrimEnd('/');
        _supabaseKey = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_SERVICE_ROLE_KEY")
            ?? Environment.GetEnvironmentVariable("BVPDV_SUPABASE_SECRET_KEY")
            ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
            ?? Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY")
            ?? "").Trim();
        _supabaseBucket = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_BUCKET")
            ?? "balcao-livre-admin").Trim();
        _supabaseObjectPath = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_OBJECT")
            ?? "admin-store.json").Trim().TrimStart('/');
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BalcaoLivrePDVAdmin/1.2.2026");
    }

    public AdminStore Read()
    {
        lock (_gate)
        {
            var store = LoadUnsafe();
            foreach (var license in store.Licenses)
            {
                LicenseTools.RefreshLicenseStatus(license, DateTimeOffset.UtcNow);
            }
            SaveUnsafe(store);
            return store;
        }
    }

    public T Update<T>(Func<AdminStore, T> update)
    {
        lock (_gate)
        {
            var store = LoadUnsafe();
            var result = update(store);
            foreach (var license in store.Licenses)
            {
                LicenseTools.RefreshLicenseStatus(license, DateTimeOffset.UtcNow);
            }
            TrimEvents(store);
            SaveUnsafe(store);
            return result;
        }
    }

    public bool SaveClientObject(string area, string licenseKey, string machineHash, string fileName, string json, bool upsert)
    {
        lock (_gate)
        {
            var objectPath = string.Join('/',
                SafeObjectSegment(area),
                SafeObjectSegment(licenseKey),
                SafeObjectSegment(machineHash),
                SafeObjectSegment(fileName));
            var localPath = Path.Combine(_dataRoot, objectPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            File.WriteAllText(localPath, json, Encoding.UTF8);

            if (!UsesSupabase)
            {
                return true;
            }

            return SaveJsonObjectToSupabaseUnsafe(objectPath, json, upsert);
        }
    }

    private AdminStore LoadUnsafe()
    {
        if (UsesSupabase)
        {
            var remote = LoadFromSupabaseUnsafe();
            if (remote is not null)
            {
                _lastSupabaseOk = true;
                SaveLocalUnsafe(remote);
                return remote;
            }

            _lastSupabaseOk = false;
        }

        if (!File.Exists(_filePath))
        {
            var empty = new AdminStore();
            SaveUnsafe(empty);
            return empty;
        }

        try
        {
            return JsonSerializer.Deserialize<AdminStore>(File.ReadAllText(_filePath, Encoding.UTF8), AdminJson.Options) ?? new AdminStore();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AdminStore();
        }
    }

    private void SaveUnsafe(AdminStore store)
    {
        SaveLocalUnsafe(store);
        if (UsesSupabase)
        {
            SaveToSupabaseUnsafe(store);
        }
    }

    private void SaveLocalUnsafe(AdminStore store)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(store, AdminJson.Options), Encoding.UTF8);
    }

    private AdminStore? LoadFromSupabaseUnsafe()
    {
        try
        {
            EnsureSupabaseBucketUnsafe();
            using var request = CreateSupabaseRequest(HttpMethod.Get, SupabaseObjectPath(_supabaseObjectPath));
            using var response = _httpClient.Send(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var empty = new AdminStore();
                SaveToSupabaseUnsafe(empty);
                return empty;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<AdminStore>(json, AdminJson.Options) ?? new AdminStore();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return null;
        }
    }

    private void SaveToSupabaseUnsafe(AdminStore store)
    {
        try
        {
            EnsureSupabaseBucketUnsafe();
            using var request = CreateSupabaseRequest(HttpMethod.Post, SupabaseObjectPath(_supabaseObjectPath));
            request.Headers.TryAddWithoutValidation("x-upsert", "true");
            request.Content = new StringContent(JsonSerializer.Serialize(store, AdminJson.Options), Encoding.UTF8, "application/json");
            using var response = _httpClient.Send(request);
            _lastSupabaseOk = response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or InvalidOperationException)
        {
        }
    }

    private bool SaveJsonObjectToSupabaseUnsafe(string objectPath, string json, bool upsert)
    {
        try
        {
            EnsureSupabaseBucketUnsafe();
            using var request = CreateSupabaseRequest(HttpMethod.Post, SupabaseObjectPath(objectPath));
            request.Headers.TryAddWithoutValidation("x-upsert", upsert ? "true" : "false");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = _httpClient.Send(request);
            _lastSupabaseOk = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or InvalidOperationException)
        {
            _lastSupabaseOk = false;
            return false;
        }
    }

    private void EnsureSupabaseBucketUnsafe()
    {
        var payload = JsonSerializer.Serialize(new
        {
            id = _supabaseBucket,
            name = _supabaseBucket,
            @public = false
        });
        using var request = CreateSupabaseRequest(HttpMethod.Post, "/storage/v1/bucket");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = _httpClient.Send(request);
        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (body.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("exists", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
    }

    private string SupabaseObjectPath(string objectPath)
    {
        var encodedPath = string.Join('/',
            objectPath
                .TrimStart('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
        return $"/storage/v1/object/{Uri.EscapeDataString(_supabaseBucket)}/{encodedPath}";
    }

    private HttpRequestMessage CreateSupabaseRequest(HttpMethod method, string pathAndQuery)
    {
        var request = new HttpRequestMessage(method, $"{_supabaseUrl}{pathAndQuery}");
        request.Headers.TryAddWithoutValidation("apikey", _supabaseKey);
        if (!_supabaseKey.StartsWith("sb_", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
        }
        return request;
    }

    private static void TrimEvents(AdminStore store)
    {
        if (store.Events.Count <= 500)
        {
            return;
        }

        store.Events = store.Events
            .OrderByDescending(item => item.When)
            .Take(500)
            .OrderBy(item => item.When)
            .ToList();
    }

    private static string SafeObjectSegment(string value)
    {
        var clean = new string((value ?? "")
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray())
            .Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? "sem-valor" : clean;
    }
}

sealed class AdminStore
{
    public List<LicenseRecord> Licenses { get; set; } = [];
    public List<DeviceRecord> Devices { get; set; } = [];
    public List<AdminEvent> Events { get; set; } = [];
}

sealed class LicenseRecord
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public string Status { get; set; } = LicenseStatus.Available;
    public string Plan { get; set; } = "";
    public int PeriodAmount { get; set; }
    public string PeriodUnit { get; set; } = "days";
    public string CustomerName { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Notes { get; set; } = "";
    public string MachineHash { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public AppSettingsSnapshot ConfigSnapshot { get; set; } = new();
    public AppMetricsSnapshot MetricsSnapshot { get; set; } = new();
}

sealed class DeviceRecord
{
    public string Id { get; set; } = "";
    public string MachineHash { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string LicenseKey { get; set; } = "";
    public string ClientKind { get; set; } = "windows";
    public string AppVersion { get; set; } = "";
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public RestaurantProfileSnapshot Profile { get; set; } = new();
    public AppSettingsSnapshot Settings { get; set; } = new();
    public AppMetricsSnapshot Metrics { get; set; } = new();
}

sealed class AdminEvent
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public string LicenseKey { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;

    public static AdminEvent License(string type, string message, string key) => new()
    {
        Type = type,
        Message = message,
        LicenseKey = key,
        When = DateTimeOffset.UtcNow
    };

    public static AdminEvent Device(string type, string message, AppClientPayload payload) => new()
    {
        Type = type,
        Message = message,
        LicenseKey = payload.LicenseKey,
        MachineCode = payload.MachineCode,
        When = DateTimeOffset.UtcNow
    };
}

sealed class LoginRequest
{
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
}

sealed class CreateLicenseRequest
{
    public string CustomerName { get; set; } = "";
    public string Plan { get; set; } = "";
    public int Amount { get; set; } = 30;
    public string Unit { get; set; } = "days";
    public string Notes { get; set; } = "";
}

class AppClientPayload
{
    public string EventName { get; set; } = "";
    public string LicenseKey { get; set; } = "";
    public string MachineHash { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string ClientKind { get; set; } = "windows";
    public string AppVersion { get; set; } = "";
    public DateTimeOffset? LocalExpiresAt { get; set; }
    public string LocalPlan { get; set; } = "";
    public RestaurantProfileSnapshot Profile { get; set; } = new();
    public AppSettingsSnapshot Settings { get; set; } = new();
    public AppMetricsSnapshot Metrics { get; set; } = new();
}

sealed class AppSyncPayload : AppClientPayload
{
    public string SyncKind { get; set; } = "summary";
    public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.UtcNow;
    public JsonElement Summary { get; set; }
}

sealed class AppBackupPayload : AppClientPayload
{
    public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.UtcNow;
    public string StoreHash { get; set; } = "";
    public long StoreBytes { get; set; }
    public JsonElement Store { get; set; }
}

sealed class AdminActivationResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string Plan { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }

    public static AdminActivationResponse Allow(string message, string plan, DateTimeOffset expiresAt) => new()
    {
        Ok = true,
        Message = message,
        Plan = plan,
        ExpiresAt = expiresAt
    };

    public static AdminActivationResponse Deny(string message) => new()
    {
        Ok = false,
        Message = message
    };
}

sealed class RestaurantProfileSnapshot
{
    public string OwnerName { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
}

sealed class AppSettingsSnapshot
{
    public bool WindowsNotificationsEnabled { get; set; }
    public bool NotificationSoundEnabled { get; set; }
    public bool InAppVibrationEnabled { get; set; }
    public string NotificationSound { get; set; } = "";
    public bool AutoPrintDelivery { get; set; }
    public bool AutoPrintKitchen { get; set; }
    public string PrintLayout { get; set; } = "";
    public string PreferredPrinterName { get; set; } = "";
    public bool ReceiptQrEnabled { get; set; }
    public string ReceiptQrKind { get; set; } = "";
    public bool AutoCheckUpdates { get; set; }
    public bool AdminSyncEnabled { get; set; }
    public bool SupabaseAuthEnabled { get; set; }
    public bool SupabaseUrlConfigured { get; set; }
    public string SupabaseUserEmail { get; set; } = "";
}

sealed class AppMetricsSnapshot
{
    public int TablesCount { get; set; }
    public int OpenBoardsCount { get; set; }
    public int DeliveryCount { get; set; }
    public int ProductsCount { get; set; }
    public int UsersCount { get; set; }
    public int CustomersCount { get; set; }
    public int LowStockCount { get; set; }
}

sealed class AdminDashboard
{
    public object Metrics { get; set; } = new();
    public IEnumerable<object> VersionDistribution { get; set; } = [];
    public IEnumerable<LicenseRecord> ExpiringSoon { get; set; } = [];
    public IEnumerable<DeviceRecord> RecentDevices { get; set; } = [];
    public IEnumerable<AdminEvent> Events { get; set; } = [];

    public static AdminDashboard From(AdminStore store)
    {
        var now = DateTimeOffset.UtcNow;
        var active = store.Licenses.Count(item => item.Status == LicenseStatus.Active && item.ExpiresAt > now);
        var available = store.Licenses.Count(item => item.Status == LicenseStatus.Available && item.ExpiresAt > now);
        var expired = store.Licenses.Count(item => item.Status == LicenseStatus.Expired || item.ExpiresAt <= now);
        var blocked = store.Licenses.Count(item => item.Status == LicenseStatus.Blocked);
        var online24h = store.Devices.Count(item => item.LastSeenAt >= now.AddHours(-24));
        var registeredUsers = store.Devices.Sum(item => item.Metrics.UsersCount);

        return new AdminDashboard
        {
            Metrics = new
            {
                totalLicenses = store.Licenses.Count,
                activeLicenses = active,
                availableLicenses = available,
                expiredLicenses = expired,
                blockedLicenses = blocked,
                devices = store.Devices.Count,
                online24h,
                registeredUsers
            },
            VersionDistribution = store.Devices
                .GroupBy(item => string.IsNullOrWhiteSpace(item.AppVersion) ? "sem versao" : item.AppVersion)
                .Select(group => new { version = group.Key, count = group.Count() })
                .OrderByDescending(item => item.count),
            ExpiringSoon = store.Licenses
                .Where(item => item.Status != LicenseStatus.Blocked && item.ExpiresAt > now && item.ExpiresAt <= now.AddDays(15))
                .OrderBy(item => item.ExpiresAt)
                .Take(8),
            RecentDevices = store.Devices.OrderByDescending(item => item.LastSeenAt).Take(10),
            Events = store.Events.OrderByDescending(item => item.When).Take(20)
        };
    }
}
