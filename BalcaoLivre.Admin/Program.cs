using System.Security.Cryptography;
using System.Globalization;
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
app.UseStaticFiles();

app.MapGet("/", (HttpContext context) =>
{
    var configuredFrontend = Environment.GetEnvironmentVariable("BVPDV_ADMIN_FRONTEND_URL");
    var target = !string.IsNullOrWhiteSpace(configuredFrontend)
        ? configuredFrontend.TrimEnd('/') + "/admin"
        : $"{context.Request.Scheme}://{context.Request.Host.Host}:3000/admin";

    return Results.Redirect(target, permanent: false);
});

app.MapGet("/api/health", (AdminStoreService store) => Results.Ok(new { ok = true, app = "Balcao Livre PDV Admin", version = "1.2.2026", storage = store.StorageMode }));

app.MapPost("/api/login", async (HttpContext context, AdminSessionService sessions) =>
{
    var request = await context.Request.ReadFromJsonAsync<LoginRequest>(AdminJson.Options) ?? new LoginRequest();
    var user = (Environment.GetEnvironmentVariable("BVPDV_ADMIN_USER") ?? "").Trim();
    var password = Environment.GetEnvironmentVariable("BVPDV_ADMIN_PASSWORD") ?? "";

    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Json(new
        {
            ok = false,
            message = "Login admin nao configurado no servidor. Configure BVPDV_ADMIN_USER e BVPDV_ADMIN_PASSWORD."
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

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

app.MapGet("/api/support", (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    return Results.Ok(store.Read().SupportTickets
        .OrderBy(item => SupportStatusRank(item.Status))
        .ThenByDescending(item => IsUrgentSupport(item.Priority))
        .ThenByDescending(item => item.UpdatedAt));
});

app.MapPost("/api/support/{id}/status", async (string id, HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var request = await context.Request.ReadFromJsonAsync<UpdateSupportStatusRequest>(AdminJson.Options) ?? new UpdateSupportStatusRequest();
    var status = NormalizeSupportStatus(request.Status);
    var result = store.Update(data =>
    {
        var ticket = FindSupportTicket(data, id);
        if (ticket is null) return null;

        ticket.Status = status;
        ticket.AdminNote = (request.Note ?? "").Trim();
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        ticket.ResolvedAt = status == SupportStatus.Resolved ? ticket.UpdatedAt : null;
        data.Events.Add(AdminEvent.License("support.status", $"Suporte {ticket.ShortId}: {ticket.Status}", ticket.LicenseKey));
        return ticket;
    });

    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/support/{id}/reply", async (string id, HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var request = await context.Request.ReadFromJsonAsync<SupportReplyRequest>(AdminJson.Options) ?? new SupportReplyRequest();
    var message = (request.Message ?? "").Trim();
    if (string.IsNullOrWhiteSpace(message))
    {
        return Results.Json(new { ok = false, message = "Mensagem obrigatoria." }, statusCode: StatusCodes.Status400BadRequest);
    }

    var result = store.Update(data =>
    {
        var ticket = FindSupportTicket(data, id);
        if (ticket is null) return null;

        ticket.Messages.Add(SupportMessageRecord.Admin(message));
        ticket.AdminNote = "";
        ticket.Status = SupportStatus.Working;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        data.Events.Add(AdminEvent.License("support.reply", $"Resposta enviada no suporte {ticket.ShortId}", ticket.LicenseKey));
        return ticket;
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
    if (!HasValidAccountEmail(request, out var emailError))
    {
        return Results.Json(AdminActivationResponse.Deny(emailError), statusCode: StatusCodes.Status400BadRequest);
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
        license.Email = request.Profile.Email;
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

    if (response.Ok)
    {
        store.TryEnsureSupabaseAuthUser(request.Profile, request.LicenseKey, response.ExpiresAt, out _);
    }

    return Results.Ok(response);
});

app.MapPost("/api/app/checkin", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    if (!HasValidAccountEmail(request, out var emailError))
    {
        return Results.Json(new { ok = false, message = emailError }, statusCode: StatusCodes.Status400BadRequest);
    }

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
            license.Email = request.Profile.Email;
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

    var authReady = store.TryEnsureSupabaseAuthUser(request.Profile, request.LicenseKey, request.LocalExpiresAt, out var authMessage);
    return Results.Ok(new { ok = true, authReady, authMessage });
});

app.MapPost("/api/app/menu/publish", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppPublicMenuPublishPayload>(AdminJson.Options) ?? new AppPublicMenuPublishPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    request.Slug = NormalizePublicMenuSlug(request.Slug);
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(AppPublicMenuPublishResponse.Fail("Chave e computador sao obrigatorios para publicar o cardapio."), statusCode: StatusCodes.Status400BadRequest);
    }

    if (string.IsNullOrWhiteSpace(request.Slug))
    {
        return Results.Json(AppPublicMenuPublishResponse.Fail("Slug do cardapio obrigatorio."), statusCode: StatusCodes.Status400BadRequest);
    }

    if (!MarkAppPayloadSeen(store, request, "menu.publish", "Cardapio publico recebido"))
    {
        return Results.Json(AppPublicMenuPublishResponse.Fail("Licenca sem permissao para publicar cardapio."), statusCode: StatusCodes.Status401Unauthorized);
    }

    var response = store.PublishPublicMenu(request);
    return response.Ok ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status500InternalServerError);
});

app.MapPost("/api/app/support/list", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    var snapshot = store.Read();
    if (!CanReadSupportTickets(snapshot, request, out var error))
    {
        return Results.Json(new { ok = false, message = error }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var tickets = snapshot.SupportTickets
        .Where(item => string.Equals(item.LicenseKey, request.LicenseKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.MachineHash, request.MachineHash, StringComparison.Ordinal))
        .OrderBy(item => SupportStatusRank(item.Status))
        .ThenByDescending(item => item.UpdatedAt)
        .Take(10);
    return Results.Ok(new { ok = true, tickets });
});

app.MapPost("/api/app/support", async (HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppSupportPayload>(AdminJson.Options) ?? new AppSupportPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    request.Message = (request.Message ?? "").Trim();
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(AppSupportResponse.Fail("Chave e computador sao obrigatorios para suporte."), statusCode: StatusCodes.Status400BadRequest);
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.Json(AppSupportResponse.Fail("Mensagem do suporte obrigatoria."), statusCode: StatusCodes.Status400BadRequest);
    }

    var response = store.Update(data =>
    {
        var now = DateTimeOffset.UtcNow;
        var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
        if (license is null)
        {
            data.Events.Add(AdminEvent.Device("support.denied", "Suporte recusado: chave nao encontrada", request));
            return AppSupportResponse.Fail("Chave nao encontrada no admin.");
        }

        LicenseTools.RefreshLicenseStatus(license, now);
        if (license.Status == LicenseStatus.Blocked || license.Status == LicenseStatus.Expired)
        {
            data.Events.Add(AdminEvent.License("support.blocked", $"Suporte recusado: chave {license.Status}", license.Key));
            return AppSupportResponse.Fail($"Licenca {license.Status.ToLowerInvariant()}.");
        }

        if (!string.IsNullOrWhiteSpace(license.MachineHash) &&
            !string.Equals(license.MachineHash, request.MachineHash, StringComparison.Ordinal) &&
            !IsMobileClient(request))
        {
            data.Events.Add(AdminEvent.License("support.other_pc", "Suporte recusado: computador diferente", license.Key));
            return AppSupportResponse.Fail("Esta licenca esta vinculada a outro computador.");
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
        license.Email = request.Profile.Email;
        license.BusinessName = request.Profile.BusinessName;
        license.Cnpj = request.Profile.Cnpj;
        license.OwnerName = request.Profile.OwnerName;
        license.Phone = request.Profile.Phone;
        license.City = request.Profile.City;
        license.State = request.Profile.State;
        license.ConfigSnapshot = request.Settings;
        license.MetricsSnapshot = request.Metrics;
        UpsertDevice(data, request, now);

        var ticket = new SupportTicketRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Status = SupportStatus.Open,
            Category = request.Category.TrimOrDefault("Suporte tecnico"),
            Priority = NormalizeSupportPriority(request.Priority),
            Message = request.Message,
            CreatedAt = now,
            UpdatedAt = now,
            LicenseKey = request.LicenseKey,
            MachineHash = request.MachineHash,
            MachineCode = request.MachineCode,
            AppVersion = request.AppVersion,
            CustomerName = license.CustomerName,
            Email = request.Profile.Email,
            BusinessName = request.Profile.BusinessName.TrimOrDefault(license.CustomerName),
            OwnerName = request.Profile.OwnerName,
            Phone = request.Profile.Phone,
            Cnpj = request.Profile.Cnpj,
            City = request.Profile.City,
            State = request.Profile.State,
            Profile = request.Profile,
            Metrics = request.Metrics
        };
        ticket.Messages.Add(SupportMessageRecord.Customer(request.Message));
        data.SupportTickets.Add(ticket);
        TrimSupportTickets(data);
        data.Events.Add(AdminEvent.Device("support.opened", $"Suporte {ticket.ShortId}: {ticket.BusinessName.TrimOrDefault(ticket.MachineCode)}", request));
        return AppSupportResponse.Success(ticket.ShortId, "Suporte enviado para o admin.");
    });

    return response.Ok ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
});

app.MapPost("/api/app/support/{id}/message", async (string id, HttpContext context, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<AppSupportMessagePayload>(AdminJson.Options) ?? new AppSupportMessagePayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    request.Message = (request.Message ?? "").Trim();
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.Json(AppSupportResponse.Fail("Mensagem obrigatoria."), statusCode: StatusCodes.Status400BadRequest);
    }

    var response = store.Update(data =>
    {
        var ticket = FindSupportTicket(data, id);
        if (ticket is null)
        {
            return AppSupportResponse.Fail("Chamado nao encontrado.");
        }

        if (!string.Equals(ticket.LicenseKey, request.LicenseKey, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(ticket.MachineHash, request.MachineHash, StringComparison.Ordinal))
        {
            return AppSupportResponse.Fail("Chamado pertence a outro computador.");
        }

        var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
        if (license is null)
        {
            return AppSupportResponse.Fail("Chave nao encontrada no admin.");
        }

        LicenseTools.RefreshLicenseStatus(license, DateTimeOffset.UtcNow);
        if (license.Status == LicenseStatus.Blocked || license.Status == LicenseStatus.Expired)
        {
            return AppSupportResponse.Fail($"Licenca {license.Status.ToLowerInvariant()}.");
        }

        ticket.Messages.Add(SupportMessageRecord.Customer(request.Message));
        ticket.Message = request.Message;
        ticket.Status = SupportStatus.Open;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        UpsertDevice(data, request, ticket.UpdatedAt);
        data.Events.Add(AdminEvent.Device("support.customer_message", $"Nova mensagem no suporte {ticket.ShortId}", request));
        return AppSupportResponse.Success(ticket.ShortId, "Mensagem enviada.");
    });

    return response.Ok ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status401Unauthorized);
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

app.Run();

static bool HasValidAccountEmail(AppClientPayload request, out string message)
{
    request.Profile.Email = NormalizeAccountEmail(request.Profile.Email);
    if (string.IsNullOrWhiteSpace(request.Profile.Email))
    {
        message = "Email da conta e obrigatorio para criar/vincular o login Supabase.";
        return false;
    }

    if (!request.Profile.Email.Contains('@', StringComparison.Ordinal) ||
        !request.Profile.Email.Contains('.', StringComparison.Ordinal) ||
        request.Profile.Email.Length < 6)
    {
        message = "Email da conta invalido.";
        return false;
    }

    message = "";
    return true;
}

static string NormalizeAccountEmail(string? value)
{
    return (value ?? "").Trim().ToLowerInvariant();
}

static string NormalizePublicMenuSlug(string? value)
{
    var normalized = (value ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(normalized.Length);
    foreach (var ch in normalized)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
        {
            continue;
        }

        if (char.IsLetterOrDigit(ch))
        {
            sb.Append(ch);
        }
        else if (sb.Length > 0 && sb[^1] != '-')
        {
            sb.Append('-');
        }
    }

    var slug = sb.ToString().Trim('-');
    return slug.Length <= 72 ? slug : slug[..72].Trim('-');
}

static bool MarkAppPayloadSeen(AdminStoreService store, AppClientPayload request, string eventType, string eventMessage)
{
    if (!HasValidAccountEmail(request, out _))
    {
        return false;
    }

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
        license.Email = request.Profile.Email;
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

static SupportTicketRecord? FindSupportTicket(AdminStore data, string id)
{
    return data.SupportTickets.FirstOrDefault(item =>
        string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(item.ShortId, id, StringComparison.OrdinalIgnoreCase));
}

static bool CanReadSupportTickets(AdminStore data, AppClientPayload request, out string message)
{
    message = "";
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        message = "Chave e computador sao obrigatorios.";
        return false;
    }

    var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
    if (license is null)
    {
        message = "Chave nao encontrada no admin.";
        return false;
    }

    LicenseTools.RefreshLicenseStatus(license, DateTimeOffset.UtcNow);
    if (license.Status == LicenseStatus.Blocked || license.Status == LicenseStatus.Expired)
    {
        message = $"Licenca {license.Status.ToLowerInvariant()}.";
        return false;
    }

    if (!string.IsNullOrWhiteSpace(license.MachineHash) &&
        !string.Equals(license.MachineHash, request.MachineHash, StringComparison.Ordinal) &&
        !IsMobileClient(request))
    {
        message = "Esta licenca esta vinculada a outro computador.";
        return false;
    }

    return true;
}

static string NormalizeSupportStatus(string? value)
{
    return (value ?? "").Trim().ToUpperInvariant() switch
    {
        "EM_ATENDIMENTO" or "ATENDIMENTO" or "ATENDENDO" => SupportStatus.Working,
        "RESOLVIDO" or "RESOLVIDA" or "FECHADO" or "FECHADA" => SupportStatus.Resolved,
        _ => SupportStatus.Open
    };
}

static string NormalizeSupportPriority(string? value)
{
    return (value ?? "").Trim().ToUpperInvariant() switch
    {
        "URGENTE" or "ALTA" or "HIGH" => "URGENTE",
        _ => "NORMAL"
    };
}

static int SupportStatusRank(string? status)
{
    return NormalizeSupportStatus(status) switch
    {
        SupportStatus.Open => 0,
        SupportStatus.Working => 1,
        _ => 2
    };
}

static bool IsUrgentSupport(string? priority)
{
    return string.Equals(NormalizeSupportPriority(priority), "URGENTE", StringComparison.Ordinal);
}

static void TrimSupportTickets(AdminStore data)
{
    if (data.SupportTickets.Count <= 300)
    {
        return;
    }

    data.SupportTickets = data.SupportTickets
        .OrderBy(item => SupportStatusRank(item.Status))
        .ThenByDescending(item => item.UpdatedAt)
        .Take(300)
        .ToList();
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
    private readonly string _supabasePublicBucket;
    private readonly string _supabaseObjectPath;
    private readonly bool _supabaseRequired;
    private readonly HttpClient _httpClient = new();
    private readonly object _gate = new();
    private bool _lastSupabaseOk;

    public string StorageMode => UsesSupabase
        ? (_lastSupabaseOk ? "supabase" : "supabase-pendente")
        : (_supabaseRequired ? "supabase-nao-configurado" : "local-json");
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
        _supabasePublicBucket = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_PUBLIC_BUCKET")
            ?? "balcao-livre-public").Trim();
        _supabaseObjectPath = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_OBJECT")
            ?? "admin-store.json").Trim().TrimStart('/');
        _supabaseRequired = ReadBooleanEnvironment("BVPDV_REQUIRE_SUPABASE", defaultValue: true);
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

    public bool TryEnsureSupabaseAuthUser(RestaurantProfileSnapshot profile, string licenseKey, DateTimeOffset? expiresAt, out string message)
    {
        var email = (profile.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            message = "Email da conta nao informado.";
            return false;
        }

        if (!UsesSupabase)
        {
            message = "Supabase nao configurado no admin; email salvo somente no cadastro da licenca.";
            return false;
        }

        try
        {
            var normalizedKey = LicenseKeyFactory.Normalize(licenseKey);
            var payload = new
            {
                email,
                password = BuildInitialSupabasePassword(normalizedKey),
                email_confirm = true,
                user_metadata = new
                {
                    business_name = profile.BusinessName,
                    legal_name = profile.LegalName,
                    owner_name = profile.OwnerName,
                    cnpj = profile.Cnpj,
                    phone = profile.Phone
                },
                app_metadata = new
                {
                    app = "balcao_livre_pdv",
                    role = "restaurant",
                    license_key = normalizedKey,
                    license_expires_at = expiresAt
                }
            };

            using var request = CreateSupabaseAuthRequest(HttpMethod.Post, "/auth/v1/admin/users");
            request.Content = new StringContent(JsonSerializer.Serialize(payload, AdminJson.Options), Encoding.UTF8, "application/json");
            using var response = _httpClient.Send(request);
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                message = "Usuario Supabase criado/vinculado.";
                return true;
            }

            if (body.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("registered", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("exists", StringComparison.OrdinalIgnoreCase))
            {
                message = "Usuario Supabase ja existia; email mantido vinculado.";
                return true;
            }

            message = $"Supabase Auth recusou criacao do usuario: {body.TrimOrDefault(response.StatusCode.ToString())}";
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or InvalidOperationException)
        {
            message = $"Falha ao criar usuario Supabase: {ex.Message}";
            return false;
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

            if (!UsesSupabase)
            {
                if (_supabaseRequired)
                {
                    return false;
                }

                var localPath = Path.Combine(_dataRoot, objectPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                File.WriteAllText(localPath, json, Encoding.UTF8);
                return true;
            }

            return SaveJsonObjectToSupabaseUnsafe(objectPath, json, upsert);
        }
    }

    public AppPublicMenuPublishResponse PublishPublicMenu(AppPublicMenuPublishPayload request)
    {
        if (!UsesSupabase)
        {
            return AppPublicMenuPublishResponse.Fail("Supabase nao configurado no admin. Configure BVPDV_SUPABASE_URL e BVPDV_SUPABASE_SERVICE_ROLE_KEY.");
        }

        try
        {
            lock (_gate)
            {
                var now = DateTimeOffset.UtcNow;
                var slug = NormalizePublicMenuSlug(request.Slug);
                var logoUrl = ResolvePublicMenuLogoUnsafe(request, slug);
                var menuPayload = new Dictionary<string, object?>
                {
                    ["store_id"] = request.LicenseKey,
                    ["slug"] = slug,
                    ["name"] = request.Profile.BusinessName.TrimOrDefault(request.Profile.LegalName.TrimOrDefault(request.Profile.OwnerName.TrimOrDefault("Balcao Livre"))),
                    ["description"] = request.Description.TrimOrDefault("Cardapio digital."),
                    ["phone"] = request.Profile.Phone,
                    ["address"] = request.Profile.Address,
                    ["city"] = request.Profile.City,
                    ["state"] = request.Profile.State,
                    ["logo_url"] = logoUrl,
                    ["theme_color"] = request.ThemeColor.TrimOrDefault("#0f766e"),
                    ["is_published"] = true,
                    ["updated_at"] = now
                };

                using var upsert = CreateSupabaseAuthRequest(HttpMethod.Post, "/rest/v1/bv_public_menus?on_conflict=slug");
                upsert.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=representation");
                upsert.Content = new StringContent(JsonSerializer.Serialize(new[] { menuPayload }, AdminJson.Options), Encoding.UTF8, "application/json");
                using var upsertResponse = _httpClient.Send(upsert);
                var upsertBody = upsertResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!upsertResponse.IsSuccessStatusCode)
                {
                    return AppPublicMenuPublishResponse.Fail($"Supabase recusou menu: {upsertBody.TrimOrDefault(upsertResponse.StatusCode.ToString())}");
                }

                var menuId = ReadFirstJsonProperty(upsertBody, "id");
                if (string.IsNullOrWhiteSpace(menuId))
                {
                    return AppPublicMenuPublishResponse.Fail("Supabase nao retornou o ID do cardapio.");
                }

                using (var delete = CreateSupabaseAuthRequest(HttpMethod.Delete, $"/rest/v1/bv_public_menu_items?menu_id=eq.{Uri.EscapeDataString(menuId)}"))
                {
                    delete.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                    using var deleteResponse = _httpClient.Send(delete);
                    if (!deleteResponse.IsSuccessStatusCode)
                    {
                        var body = deleteResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        return AppPublicMenuPublishResponse.Fail($"Supabase recusou limpar itens antigos: {body.TrimOrDefault(deleteResponse.StatusCode.ToString())}");
                    }
                }

                var items = request.Items
                    .Where(item => item.IsActive && !string.IsNullOrWhiteSpace(item.Name))
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Category)
                    .ThenBy(item => item.Name)
                    .Select((item, index) => new Dictionary<string, object?>
                    {
                        ["menu_id"] = menuId,
                        ["code"] = item.Code,
                        ["name"] = item.Name,
                        ["description"] = item.Description,
                        ["category"] = item.Category.TrimOrDefault("Cardapio"),
                        ["price"] = item.Price,
                        ["stock_quantity"] = item.StockQuantity,
                        ["is_in_stock"] = item.IsInStock,
                        ["image_url"] = item.ImageUrl,
                        ["sort_order"] = item.SortOrder == 0 ? index * 10 : item.SortOrder,
                        ["is_active"] = item.IsActive,
                        ["updated_at"] = now
                    })
                    .ToList();

                if (items.Count > 0)
                {
                    using var insert = CreateSupabaseAuthRequest(HttpMethod.Post, "/rest/v1/bv_public_menu_items");
                    insert.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                    insert.Content = new StringContent(JsonSerializer.Serialize(items, AdminJson.Options), Encoding.UTF8, "application/json");
                    using var insertResponse = _httpClient.Send(insert);
                    if (!insertResponse.IsSuccessStatusCode)
                    {
                        var body = insertResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        return AppPublicMenuPublishResponse.Fail($"Supabase recusou itens do cardapio: {body.TrimOrDefault(insertResponse.StatusCode.ToString())}");
                    }
                }

                return AppPublicMenuPublishResponse.Success(slug, request.PublicUrl, items.Count, "Cardapio publicado.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or TaskCanceledException or InvalidOperationException or FormatException)
        {
            return AppPublicMenuPublishResponse.Fail($"Falha ao publicar no Supabase: {ex.Message}");
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
                if (!_supabaseRequired)
                {
                    SaveLocalUnsafe(remote);
                }
                return remote;
            }

            _lastSupabaseOk = false;
        }

        if (_supabaseRequired)
        {
            return new AdminStore();
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
        if (!_supabaseRequired)
        {
            SaveLocalUnsafe(store);
        }

        if (UsesSupabase)
        {
            if (_lastSupabaseOk || !_supabaseRequired)
            {
                SaveToSupabaseUnsafe(store);
            }
        }
    }

    private static bool ReadBooleanEnvironment(string name, bool defaultValue)
    {
        var value = (Environment.GetEnvironmentVariable(name) ?? "").Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("sim", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("on", StringComparison.OrdinalIgnoreCase);
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

    private string ResolvePublicMenuLogoUnsafe(AppPublicMenuPublishPayload request, string slug)
    {
        if (Uri.TryCreate((request.LogoUrl ?? "").Trim(), UriKind.Absolute, out var logoUri) &&
            (logoUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
             logoUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
        {
            return logoUri.ToString();
        }

        if (string.IsNullOrWhiteSpace(request.LogoBase64))
        {
            return "";
        }

        var bytes = Convert.FromBase64String(request.LogoBase64);
        if (bytes.Length <= 0 || bytes.Length > 2_000_000)
        {
            return "";
        }

        var contentType = NormalizeImageContentType(request.LogoContentType);
        var extension = LogoExtension(contentType, request.LogoFileName);
        var objectPath = $"menus/{SafeObjectSegment(slug)}/logo{extension}";
        EnsureSupabaseBucketUnsafe(_supabasePublicBucket, isPublic: true);
        using var upload = CreateSupabaseAuthRequest(HttpMethod.Post, SupabaseObjectPath(_supabasePublicBucket, objectPath));
        upload.Headers.TryAddWithoutValidation("x-upsert", "true");
        upload.Content = new ByteArrayContent(bytes);
        upload.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var response = _httpClient.Send(upload);
        if (!response.IsSuccessStatusCode)
        {
            return "";
        }

        return SupabasePublicObjectUrl(_supabasePublicBucket, objectPath);
    }

    private static string NormalizeImageContentType(string value)
    {
        var clean = (value ?? "").Trim().ToLowerInvariant();
        return clean is "image/jpeg" or "image/png" or "image/webp" or "image/gif" or "image/bmp"
            ? clean
            : "image/png";
    }

    private static string LogoExtension(string contentType, string fileName)
    {
        var fromName = Path.GetExtension(fileName ?? "").Trim().ToLowerInvariant();
        if (fromName is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp")
        {
            return fromName;
        }

        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => ".png"
        };
    }

    private static string ReadFirstJsonProperty(string json, string propertyName)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
        {
            return "";
        }

        var first = doc.RootElement[0];
        return first.TryGetProperty(propertyName, out var property) ? property.ToString() : "";
    }

    private void EnsureSupabaseBucketUnsafe()
    {
        EnsureSupabaseBucketUnsafe(_supabaseBucket, isPublic: false);
    }

    private void EnsureSupabaseBucketUnsafe(string bucket, bool isPublic)
    {
        var payload = JsonSerializer.Serialize(new
        {
            id = bucket,
            name = bucket,
            @public = isPublic
        });
        using var request = CreateSupabaseAuthRequest(HttpMethod.Post, "/storage/v1/bucket");
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
        return SupabaseObjectPath(_supabaseBucket, objectPath);
    }

    private static string SupabaseObjectPath(string bucket, string objectPath)
    {
        return $"/storage/v1/object/{Uri.EscapeDataString(bucket)}/{EncodeSupabaseObjectPath(objectPath)}";
    }

    private string SupabasePublicObjectUrl(string bucket, string objectPath)
    {
        return $"{_supabaseUrl}/storage/v1/object/public/{Uri.EscapeDataString(bucket)}/{EncodeSupabaseObjectPath(objectPath)}";
    }

    private static string EncodeSupabaseObjectPath(string objectPath)
    {
        return string.Join('/',
            objectPath
                .TrimStart('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
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

    private HttpRequestMessage CreateSupabaseAuthRequest(HttpMethod method, string pathAndQuery)
    {
        var request = new HttpRequestMessage(method, $"{_supabaseUrl}{pathAndQuery}");
        request.Headers.TryAddWithoutValidation("apikey", _supabaseKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
        return request;
    }

    private static string BuildInitialSupabasePassword(string licenseKey)
    {
        var clean = LicenseKeyFactory.Normalize(licenseKey);
        if (clean.Length >= 8)
        {
            return clean;
        }

        return $"{clean}#BL2026".PadRight(8, '0');
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

    private static string NormalizePublicMenuSlug(string value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length <= 72 ? slug : slug[..72].Trim('-');
    }
}

sealed class AdminStore
{
    public List<LicenseRecord> Licenses { get; set; } = [];
    public List<DeviceRecord> Devices { get; set; } = [];
    public List<SupportTicketRecord> SupportTickets { get; set; } = [];
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
    public string Email { get; set; } = "";
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
    public string WhatsAppPhone { get; set; } = "";
    public string WhatsAppBotId { get; set; } = "";
    public string WhatsAppStatus { get; set; } = "";
    public string WhatsAppLastError { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? WhatsAppRequestedAt { get; set; }
    public DateTimeOffset? WhatsAppActivatedAt { get; set; }
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

static class SupportStatus
{
    public const string Open = "ABERTO";
    public const string Working = "EM_ATENDIMENTO";
    public const string Resolved = "RESOLVIDO";
}

sealed class SupportTicketRecord
{
    public string Id { get; set; } = "";
    public string ShortId => string.IsNullOrWhiteSpace(Id) ? "" : Id[..Math.Min(8, Id.Length)].ToUpperInvariant();
    public string Status { get; set; } = SupportStatus.Open;
    public string Category { get; set; } = "";
    public string Priority { get; set; } = "NORMAL";
    public string Message { get; set; } = "";
    public List<SupportMessageRecord> Messages { get; set; } = [];
    public string AdminNote { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string LicenseKey { get; set; } = "";
    public string MachineHash { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public RestaurantProfileSnapshot Profile { get; set; } = new();
    public AppMetricsSnapshot Metrics { get; set; } = new();
}

sealed class SupportMessageRecord
{
    public string Id { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset When { get; set; }

    public static SupportMessageRecord Customer(string message) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Sender = "cliente",
        Message = message,
        When = DateTimeOffset.UtcNow
    };

    public static SupportMessageRecord Admin(string message) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Sender = "admin",
        Message = message,
        When = DateTimeOffset.UtcNow
    };
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

sealed class UpdateSupportStatusRequest
{
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
}

sealed class SupportReplyRequest
{
    public string Message { get; set; } = "";
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

sealed class AppPublicMenuPublishPayload : AppClientPayload
{
    public string Slug { get; set; } = "";
    public string PublicUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public string ThemeColor { get; set; } = "#0f766e";
    public string LogoUrl { get; set; } = "";
    public string LogoFileName { get; set; } = "";
    public string LogoContentType { get; set; } = "";
    public string LogoBase64 { get; set; } = "";
    public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.UtcNow;
    public List<AppPublicMenuItemPayload> Items { get; set; } = [];
}

sealed class AppPublicMenuItemPayload
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal StockQuantity { get; set; }
    public bool IsInStock { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string ImageUrl { get; set; } = "";
}

sealed class AppPublicMenuPublishResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string Slug { get; set; } = "";
    public string PublicUrl { get; set; } = "";
    public int ItemsPublished { get; set; }

    public static AppPublicMenuPublishResponse Success(string slug, string publicUrl, int itemsPublished, string message) => new()
    {
        Ok = true,
        Slug = slug,
        PublicUrl = publicUrl,
        ItemsPublished = itemsPublished,
        Message = message
    };

    public static AppPublicMenuPublishResponse Fail(string message) => new()
    {
        Ok = false,
        Message = message
    };
}

sealed class AppSupportPayload : AppClientPayload
{
    public string Category { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.UtcNow;
}

sealed class AppSupportMessagePayload : AppClientPayload
{
    public string Message { get; set; } = "";
    public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.UtcNow;
}

sealed class AppSupportResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string TicketId { get; set; } = "";

    public static AppSupportResponse Success(string ticketId, string message) => new()
    {
        Ok = true,
        TicketId = ticketId,
        Message = message
    };

    public static AppSupportResponse Fail(string message) => new()
    {
        Ok = false,
        Message = message
    };
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
    public string Email { get; set; } = "";
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
        var openSupport = store.SupportTickets.Count(item => item.Status != SupportStatus.Resolved);
        var urgentSupport = store.SupportTickets.Count(item =>
            item.Status != SupportStatus.Resolved &&
            string.Equals(item.Priority, "URGENTE", StringComparison.OrdinalIgnoreCase));

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
                registeredUsers,
                openSupport,
                urgentSupport
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
