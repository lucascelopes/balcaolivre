using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
builder.Services.AddSingleton<CommerceFulfillmentService>();
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Context.Response.Headers["Pragma"] = "no-cache";
        context.Context.Response.Headers["Expires"] = "0";
    }
});

app.MapGet("/", (HttpContext context) =>
{
    var configuredFrontend = Environment.GetEnvironmentVariable("BVPDV_ADMIN_FRONTEND_URL");
    var target = !string.IsNullOrWhiteSpace(configuredFrontend)
        ? configuredFrontend.TrimEnd('/') + "/admin"
        : $"{context.Request.Scheme}://{context.Request.Host.Host}:3000/admin";

    return Results.Redirect(target, permanent: false);
});

app.MapGet("/api/health", (AdminStoreService store) => Results.Ok(new { ok = true, app = "Balcao Livre PDV Admin", version = "1.2.2026", storage = store.StorageMode }));

app.MapMethods("/api/public/analytics", ["OPTIONS"], (HttpContext context) =>
{
    ApplyPublicAnalyticsCors(context);
    return Results.Ok(new { ok = true });
});

app.MapPost("/api/public/analytics", async (HttpContext context, AdminStoreService store) =>
{
    ApplyPublicAnalyticsCors(context);
    var request = await context.Request.ReadFromJsonAsync<PublicAnalyticsEventRequest>(AdminJson.Options) ?? new PublicAnalyticsEventRequest();
    var analyticsEvent = BuildPublicAnalyticsEvent(context, request);
    store.Update(data =>
    {
        UpsertSiteAnalytics(data, analyticsEvent);
        if (analyticsEvent.Type is AnalyticsEventType.CheckoutStarted or AnalyticsEventType.CheckoutCompleted)
        {
            data.Events.Add(AdminEvent.Analytics(
                analyticsEvent.Type,
                AnalyticsEventMessage(analyticsEvent),
                analyticsEvent.Plan));
        }
        return true;
    });

    return Results.Ok(new { ok = true });
});

app.MapPost("/api/login", async (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    var request = await context.Request.ReadFromJsonAsync<LoginRequest>(AdminJson.Options) ?? new LoginRequest();
    var requestedUser = NormalizeAdminLogin(request.User);
    var requestedPassword = request.Password ?? "";
    var user = NormalizeAdminLogin(Environment.GetEnvironmentVariable("BVPDV_ADMIN_USER") ?? "");
    var password = Environment.GetEnvironmentVariable("BVPDV_ADMIN_PASSWORD") ?? "";
    var hasConfiguredLogin = !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password);
    var authenticatedUser = "";

    if (hasConfiguredLogin &&
        string.Equals(requestedUser, user, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(requestedPassword, password, StringComparison.Ordinal))
    {
        authenticatedUser = user;
    }

    if (string.IsNullOrWhiteSpace(authenticatedUser))
    {
        var adminUsers = store.Read().AdminUsers
            .Where(item => !string.IsNullOrWhiteSpace(item.User))
            .ToList();
        hasConfiguredLogin = hasConfiguredLogin || adminUsers.Count > 0;

        var adminUser = adminUsers.FirstOrDefault(item =>
            string.Equals(NormalizeAdminLogin(item.User), requestedUser, StringComparison.OrdinalIgnoreCase));
        if (adminUser is not null && VerifyAdminPassword(adminUser, requestedPassword))
        {
            authenticatedUser = NormalizeAdminLogin(adminUser.User);
        }
    }

    if (!hasConfiguredLogin)
    {
        return Results.Json(new
        {
            ok = false,
            message = "Login admin nao configurado no Supabase. Crie um usuario admin ou configure BVPDV_ADMIN_USER e BVPDV_ADMIN_PASSWORD."
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (string.IsNullOrWhiteSpace(authenticatedUser))
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

    return Results.Ok(new { ok = true, user = authenticatedUser });
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

app.MapGet("/api/realtime", async (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var cancellation = context.RequestAborted;
    context.Response.ContentType = "text/event-stream; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache, no-transform";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.Headers["X-Accel-Buffering"] = "no";

    var snapshot = store.RefreshRealtimeState(forceRemote: true);
    var lastRevision = snapshot.Revision;
    var lastStorageMode = snapshot.StorageMode;
    await WriteSseAsync(context, "admin.ready", snapshot, cancellation);

    while (!cancellation.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellation);
            snapshot = store.RefreshRealtimeState(forceRemote: false);

            if (snapshot.Revision != lastRevision ||
                !string.Equals(snapshot.StorageMode, lastStorageMode, StringComparison.Ordinal))
            {
                lastRevision = snapshot.Revision;
                lastStorageMode = snapshot.StorageMode;
                await WriteSseAsync(context, "admin.changed", snapshot, cancellation);
                continue;
            }

            await context.Response.WriteAsync(": keepalive\n\n", cancellation);
            await context.Response.Body.FlushAsync(cancellation);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
});

app.MapGet("/api/dashboard", (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var snapshot = store.Read();
    var stripe = store.ReadStripeCheckoutSummary();
    return Results.Ok(AdminDashboard.From(snapshot, stripe));
});

app.MapGet("/api/client-intelligence", (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var snapshot = store.Read();
    var stripe = store.ReadStripeCheckoutSummary();
    return Results.Ok(ClientIntelligenceDashboard.From(snapshot, stripe));
});

app.MapPost("/api/visits/plan", async (
    HttpContext context,
    AdminSessionService sessions,
    IHttpClientFactory httpClientFactory) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();

    var request = await context.Request.ReadFromJsonAsync<VisitRoutePlanRequest>(
        AdminJson.Options,
        context.RequestAborted) ?? new VisitRoutePlanRequest();
    var candidates = request.Candidates
        .Where(item =>
            !string.IsNullOrWhiteSpace(item.Id)
            && double.IsFinite(item.Latitude)
            && double.IsFinite(item.Longitude))
        .Take(12)
        .ToList();
    if (candidates.Count == 0)
    {
        return Results.BadRequest(new { message = "Envie ao menos uma oportunidade válida." });
    }

    var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.Json(
            new
            {
                message = "OpenRouter não configurado. O painel continuará usando o plano local.",
                code = "openrouter_not_configured"
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // Só enviamos contexto comercial mínimo. Telefones, CNPJ e outras informações privadas ficam no servidor.
    var safeContext = new
    {
        candidates = candidates.Select(item => new
        {
            item.Id,
            item.Name,
            item.Neighborhood,
            item.Score,
            item.Stage,
            item.DistanceMeters
        })
    };
    var systemPrompt = """
        Você prioriza visitas comerciais presenciais. A geometria, o trânsito e a polyline serão calculados
        por um motor de rotas; você NÃO calcula nem inventa caminhos. Ordene somente os IDs recebidos usando
        potencial comercial, estágio do funil, distância informada e eficiência do dia.
        Responda exclusivamente JSON no formato:
        {"orderedIds":["id"],"summary":"até 140 caracteres","reasons":[{"id":"id","reason":"até 70 caracteres"}]}
        Use cada ID exatamente uma vez. Não inclua IDs desconhecidos.
        """;

    var openRouterRequest = new
    {
        model = "openrouter/free",
        temperature = 0.2,
        max_tokens = 650,
        messages = new object[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = JsonSerializer.Serialize(safeContext, AdminJson.Options) }
        }
    };

    using var openRouterMessage = new HttpRequestMessage(
        HttpMethod.Post,
        "https://openrouter.ai/api/v1/chat/completions");
    openRouterMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    openRouterMessage.Headers.TryAddWithoutValidation("X-Title", "Balcão Livre Admin");
    openRouterMessage.Content = JsonContent.Create(openRouterRequest, options: AdminJson.Options);

    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(18);
        using var response = await client.SendAsync(openRouterMessage, context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            return Results.Json(
                new { message = "A IA está indisponível agora. Use o plano local.", code = "openrouter_unavailable" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.RequestAborted));
        var content = body.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
        content = content.Trim();
        if (content.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBreak = content.IndexOf('\n');
            var lastFence = content.LastIndexOf("```", StringComparison.Ordinal);
            content = firstBreak >= 0 && lastFence > firstBreak
                ? content[(firstBreak + 1)..lastFence].Trim()
                : content;
        }

        var plan = JsonSerializer.Deserialize<VisitAiPlanResponse>(content, AdminJson.Options)
            ?? new VisitAiPlanResponse();
        var allowed = candidates.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var ordered = plan.OrderedIds
            .Where(allowed.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        ordered.AddRange(candidates.Select(item => item.Id).Where(id => !ordered.Contains(id, StringComparer.Ordinal)));
        var reasons = plan.Reasons
            .Where(item => allowed.Contains(item.Id))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        return Results.Ok(new
        {
            orderedIds = ordered,
            summary = plan.Summary.TrimOrDefault("Plano priorizado por potencial comercial e eficiência da rota."),
            reasons,
            model = "openrouter/free"
        });
    }
    catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
    {
        return Results.Json(
            new { message = "A IA demorou para responder. Use o plano local.", code = "openrouter_timeout" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception)
    {
        return Results.Json(
            new { message = "Não foi possível interpretar a sugestão da IA. Use o plano local.", code = "openrouter_invalid_response" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
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

app.MapPost("/api/licenses/{id}/renew", async (string id, HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var request = await context.Request.ReadFromJsonAsync<CreateLicenseRequest>(AdminJson.Options) ?? new CreateLicenseRequest();
    var amount = Math.Clamp(request.Amount <= 0 ? 30 : request.Amount, 1, 3650);
    var unit = NormalizeDurationUnit(request.Unit);
    var now = DateTimeOffset.UtcNow;

    var result = store.Update(data =>
    {
        var license = data.Licenses.FirstOrDefault(item =>
            item.Id == id || string.Equals(item.Key, id, StringComparison.OrdinalIgnoreCase));
        if (license is null) return null;

        var renewalStart = license.ExpiresAt > now ? license.ExpiresAt : now;
        license.ExpiresAt = AddDuration(renewalStart, amount, unit);
        license.PeriodAmount = amount;
        license.PeriodUnit = unit;
        license.Status = string.IsNullOrWhiteSpace(license.MachineHash)
            ? LicenseStatus.Available
            : LicenseStatus.Active;
        data.Events.Add(AdminEvent.License(
            "license.renewed",
            $"Licença renovada por {amount} {DurationLabel(unit, amount)}: {license.CustomerName}",
            license.Key));
        return license;
    });

    return result is null ? Results.NotFound() : Results.Ok(result);
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

app.MapGet("/api/fulfillments", async (
    HttpContext context,
    AdminSessionService sessions,
    CommerceFulfillmentService fulfillments) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    if (!fulfillments.IsConfigured)
    {
        return Results.Json(
            new { message = "Supabase de comércio não configurado no admin." },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var payload = await fulfillments.ListAsync(context.RequestAborted);
    return Results.Text(payload, "application/json");
});

app.MapPost("/api/fulfillments/{id}/status", async (
    string id,
    HttpContext context,
    AdminSessionService sessions,
    CommerceFulfillmentService fulfillments) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    if (!fulfillments.IsConfigured)
    {
        return Results.Json(
            new { message = "Supabase de comércio não configurado no admin." },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var request = await context.Request.ReadFromJsonAsync<UpdateFulfillmentStatusRequest>(AdminJson.Options)
        ?? new UpdateFulfillmentStatusRequest();
    var status = (request.Status ?? "").Trim().ToUpperInvariant();
    if (status is not ("READY" or "REQUESTED" or "SHIPPED" or "DELIVERED" or "CANCELED"))
    {
        return Results.Json(
            new { message = "Status de entrega inválido." },
            statusCode: StatusCodes.Status400BadRequest);
    }

    var payload = await fulfillments.UpdateStatusAsync(
        id,
        status,
        request.TrackingCode,
        context.RequestAborted);
    return payload is null
        ? Results.NotFound()
        : Results.Text(payload, "application/json");
});

app.MapGet("/api/blocked-ips", (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    return Results.Ok(store.Read().BlockedIps
        .OrderByDescending(item => item.UpdatedAt)
        .ThenByDescending(item => item.CreatedAt));
});

app.MapPost("/api/blocked-ips", async (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var request = await context.Request.ReadFromJsonAsync<BlockIpRequest>(AdminJson.Options) ?? new BlockIpRequest();
    var ip = NormalizeIpAddress(request.Ip);
    if (string.IsNullOrWhiteSpace(ip) || ip == "-")
    {
        return Results.Json(new { ok = false, message = "IP obrigatorio para bloquear." }, statusCode: StatusCodes.Status400BadRequest);
    }

    var result = store.Update(data =>
    {
        data.BlockedIps ??= [];
        var now = DateTimeOffset.UtcNow;
        var existing = data.BlockedIps.FirstOrDefault(item => string.Equals(NormalizeIpAddress(item.Ip), ip, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            existing = new BlockedIpRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Ip = ip,
                CreatedAt = now
            };
            data.BlockedIps.Add(existing);
        }

        existing.Ip = ip;
        existing.Reason = request.Reason.TrimOrDefault("Bloqueado pelo admin");
        existing.Source = request.Source.TrimOrDefault("admin");
        existing.UpdatedAt = now;
        data.Events.Add(AdminEvent.Analytics("ip.blocked", $"IP bloqueado: {ip}", existing.Reason));
        return existing;
    });

    return Results.Ok(result);
});

app.MapPost("/api/blocked-ips/delete", async (HttpContext context, AdminSessionService sessions, AdminStoreService store) =>
{
    if (!sessions.IsValid(context)) return Results.Unauthorized();
    var request = await context.Request.ReadFromJsonAsync<BlockIpRequest>(AdminJson.Options) ?? new BlockIpRequest();
    var ip = NormalizeIpAddress(request.Ip);
    if (string.IsNullOrWhiteSpace(ip))
    {
        return Results.Json(new { ok = false, message = "IP obrigatorio para liberar." }, statusCode: StatusCodes.Status400BadRequest);
    }

    var removed = store.Update(data =>
    {
        data.BlockedIps ??= [];
        var count = data.BlockedIps.RemoveAll(item => string.Equals(NormalizeIpAddress(item.Ip), ip, StringComparison.OrdinalIgnoreCase));
        if (count > 0)
        {
            data.Events.Add(AdminEvent.Analytics("ip.unblocked", $"IP liberado: {ip}", ""));
        }

        return count;
    });

    return removed <= 0 ? Results.NotFound() : Results.Ok(new { ok = true, ip });
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
    if (DenyBlockedClientIp(context, store, "ativacao") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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
        license.ClientKind = NormalizeClientKind(request.ClientKind);
        license.AppVersion = request.AppVersion;
        license.CustomerName = request.Profile.BusinessName.TrimOrDefault(license.CustomerName);
        license.Email = request.Profile.Email;
        license.BusinessName = request.Profile.BusinessName;
        license.Cnpj = request.Profile.Cnpj;
        license.OwnerName = request.Profile.OwnerName;
        license.Phone = request.Profile.Phone;
        license.Address = request.Profile.Address;
        license.City = request.Profile.City;
        license.State = request.Profile.State;
        license.ConfigSnapshot = request.Settings;
        license.MetricsSnapshot = request.Metrics;
        license.EnvironmentSnapshot = request.Environment;

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
    if (DenyBlockedClientIp(context, store, "check-in") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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
            license.ClientKind = NormalizeClientKind(request.ClientKind);
            license.AppVersion = request.AppVersion;
            license.Email = request.Profile.Email;
            license.BusinessName = request.Profile.BusinessName;
            license.Cnpj = request.Profile.Cnpj;
            license.OwnerName = request.Profile.OwnerName;
            license.Phone = request.Profile.Phone;
            license.Address = request.Profile.Address;
            license.City = request.Profile.City;
            license.State = request.Profile.State;
            license.ConfigSnapshot = request.Settings;
            license.MetricsSnapshot = request.Metrics;
            license.EnvironmentSnapshot = request.Environment;
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
    if (DenyBlockedClientIp(context, store, "cardapio") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppPublicMenuPublishPayload>(AdminJson.Options) ?? new AppPublicMenuPublishPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
    request.Slug = NormalizePublicMenuSlug(request.Slug);
    if (string.IsNullOrWhiteSpace(request.LicenseKey) || string.IsNullOrWhiteSpace(request.MachineHash))
    {
        return Results.Json(AppPublicMenuPublishResponse.Fail("Chave e computador sao obrigatorios para publicar o cardapio."), statusCode: StatusCodes.Status400BadRequest);
    }

    if (string.IsNullOrWhiteSpace(request.Slug))
    {
        return Results.Json(AppPublicMenuPublishResponse.Fail("Slug do cardapio obrigatorio."), statusCode: StatusCodes.Status400BadRequest);
    }

    if (!MarkAppPayloadSeenWithReason(store, request, "menu.publish", "Cardapio publico recebido", out var denyMessage))
    {
        return Results.Json(AppPublicMenuPublishResponse.Fail(denyMessage), statusCode: StatusCodes.Status401Unauthorized);
    }

    var response = store.PublishPublicMenu(request);
    return response.Ok ? Results.Ok(response) : Results.Json(response, statusCode: StatusCodes.Status500InternalServerError);
});

app.MapPost("/api/app/support/list", async (HttpContext context, AdminStoreService store) =>
{
    if (DenyBlockedClientIp(context, store, "suporte") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppClientPayload>(AdminJson.Options) ?? new AppClientPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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
    if (DenyBlockedClientIp(context, store, "suporte") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppSupportPayload>(AdminJson.Options) ?? new AppSupportPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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
        license.ClientKind = NormalizeClientKind(request.ClientKind);
        license.AppVersion = request.AppVersion;
        license.Email = request.Profile.Email;
        license.BusinessName = request.Profile.BusinessName;
        license.Cnpj = request.Profile.Cnpj;
        license.OwnerName = request.Profile.OwnerName;
        license.Phone = request.Profile.Phone;
        license.Address = request.Profile.Address;
        license.City = request.Profile.City;
        license.State = request.Profile.State;
        license.ConfigSnapshot = request.Settings;
        license.MetricsSnapshot = request.Metrics;
        license.EnvironmentSnapshot = request.Environment;
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
            Address = request.Profile.Address,
            City = request.Profile.City,
            State = request.Profile.State,
            Profile = request.Profile,
            Metrics = request.Metrics,
            Environment = request.Environment
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
    if (DenyBlockedClientIp(context, store, "suporte") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppSupportMessagePayload>(AdminJson.Options) ?? new AppSupportMessagePayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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
    if (DenyBlockedClientIp(context, store, "sync") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppSyncPayload>(AdminJson.Options) ?? new AppSyncPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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
    if (DenyBlockedClientIp(context, store, "backup") is { } blockedIpResult) return blockedIpResult;
    var request = await context.Request.ReadFromJsonAsync<AppBackupPayload>(AdminJson.Options) ?? new AppBackupPayload();
    request.LicenseKey = LicenseKeyFactory.Normalize(request.LicenseKey);
    AttachRequestEnvironment(context, request);
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

static void ApplyPublicAnalyticsCors(HttpContext context)
{
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    context.Response.Headers["Access-Control-Allow-Headers"] = "content-type";
    context.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
}

static SiteAnalyticsEvent BuildPublicAnalyticsEvent(HttpContext context, PublicAnalyticsEventRequest request)
{
    var type = NormalizeAnalyticsEventType(request.Type);
    var plan = SafeAnalyticsText(request.Plan, 80);
    var billing = SafeAnalyticsText(request.Billing, 40);
    var visitorSeed = SafeAnalyticsText(request.VisitorId, 160).TrimOrDefault(ClientIpHash(context));
    var sessionSeed = SafeAnalyticsText(request.SessionId, 160).TrimOrDefault(visitorSeed);
    var url = SafeAnalyticsText(request.Url, 500);
    var path = SafeAnalyticsText(request.Path, 180);
    if (string.IsNullOrWhiteSpace(path) && Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl))
    {
        path = parsedUrl.PathAndQuery;
    }

    return new SiteAnalyticsEvent
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = type,
        VisitorHash = HashAnalyticsIdentifier(visitorSeed),
        SessionHash = HashAnalyticsIdentifier(sessionSeed),
        Path = path.TrimOrDefault("/"),
        Url = url,
        Referrer = SafeAnalyticsText(request.Referrer, 300),
        Source = SafeAnalyticsText(request.Source, 80).TrimOrDefault("site"),
        Campaign = SafeAnalyticsText(request.Campaign, 120),
        Plan = plan,
        Billing = billing,
        CheckoutSessionId = SafeAnalyticsText(request.CheckoutSessionId, 120),
        StripeCustomerId = SafeAnalyticsText(request.StripeCustomerId, 120),
        SubscriptionId = SafeAnalyticsText(request.SubscriptionId, 120),
        Currency = SafeAnalyticsText(request.Currency, 12).TrimOrDefault("BRL").ToUpperInvariant(),
        AmountCents = Math.Max(0, request.AmountCents),
        UserAgentHash = HashAnalyticsIdentifier(context.Request.Headers.UserAgent.ToString()),
        When = DateTimeOffset.UtcNow
    };
}

static string NormalizeAnalyticsEventType(string? value)
{
    var clean = (value ?? "").Trim().ToLowerInvariant().Replace("_", ".");
    return clean switch
    {
        AnalyticsEventType.CheckoutStarted => AnalyticsEventType.CheckoutStarted,
        AnalyticsEventType.CheckoutCompleted => AnalyticsEventType.CheckoutCompleted,
        AnalyticsEventType.TrialDownload => AnalyticsEventType.TrialDownload,
        AnalyticsEventType.WhatsappClick => AnalyticsEventType.WhatsappClick,
        AnalyticsEventType.PlanView => AnalyticsEventType.PlanView,
        _ => AnalyticsEventType.SiteVisit
    };
}

static string SafeAnalyticsText(string? value, int maxLength)
{
    var clean = (value ?? "").Trim().Replace("\r", " ").Replace("\n", " ");
    return clean.Length <= maxLength ? clean : clean[..maxLength];
}

static string ClientIpHash(HttpContext context)
{
    var ip = ClientIpAddress(context);
    return string.IsNullOrWhiteSpace(ip) ? "" : $"ip:{ip}";
}

static void AttachRequestEnvironment(HttpContext context, AppClientPayload request)
{
    request.Environment ??= new ClientEnvironmentSnapshot();
    var environment = request.Environment;
    var publicIp = ClientIpAddress(context);
    if (string.IsNullOrWhiteSpace(environment.PublicIp))
    {
        environment.PublicIp = publicIp;
    }

    environment.ForwardedFor = SafeAnalyticsText(context.Request.Headers["X-Forwarded-For"].ToString(), 240);
    environment.UserAgent = SafeAnalyticsText(context.Request.Headers.UserAgent.ToString(), 300).TrimOrDefault(environment.UserAgent);
    environment.RequestHost = SafeAnalyticsText(context.Request.Host.ToString(), 120);
    environment.ServerSeenAt = DateTimeOffset.UtcNow;
}

static string ClientIpAddress(HttpContext context)
{
    var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault();

    var candidates = new[]
    {
        context.Request.Headers["CF-Connecting-IP"].ToString(),
        context.Request.Headers["X-Real-IP"].ToString(),
        context.Request.Headers["X-NF-Client-Connection-IP"].ToString(),
        forwarded,
        context.Connection.RemoteIpAddress?.ToString() ?? ""
    };

    foreach (var candidate in candidates)
    {
        var clean = SafeAnalyticsText(candidate, 80);
        if (!string.IsNullOrWhiteSpace(clean))
        {
            return clean;
        }
    }

    return "";
}

static string NormalizeIpAddress(string? value)
{
    var clean = SafeAnalyticsText(value, 80).Trim();
    if (string.IsNullOrWhiteSpace(clean))
    {
        return "";
    }

    if (clean.StartsWith("[", StringComparison.Ordinal) && clean.EndsWith("]", StringComparison.Ordinal))
    {
        clean = clean[1..^1];
    }

    if (clean.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
    {
        clean = clean["::ffff:".Length..];
    }

    return IPAddress.TryParse(clean, out var parsed) ? parsed.ToString() : clean;
}

static IResult? DenyBlockedClientIp(HttpContext context, AdminStoreService store, string area)
{
    var ip = NormalizeIpAddress(ClientIpAddress(context));
    if (string.IsNullOrWhiteSpace(ip))
    {
        return null;
    }

    var blocked = store.Read().BlockedIps?
        .FirstOrDefault(item => string.Equals(NormalizeIpAddress(item.Ip), ip, StringComparison.OrdinalIgnoreCase));
    if (blocked is null)
    {
        return null;
    }

    store.Update(data =>
    {
        data.BlockedIps ??= [];
        var now = DateTimeOffset.UtcNow;
        var existing = data.BlockedIps.FirstOrDefault(item => string.Equals(NormalizeIpAddress(item.Ip), ip, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            return false;
        }

        var shouldLog = existing.LastBlockedAt is null || existing.LastBlockedAt.Value.AddMinutes(10) <= now;
        existing.Hits++;
        existing.LastBlockedAt = now;
        existing.LastBlockedArea = area;
        existing.UpdatedAt = now;
        if (shouldLog)
        {
            data.Events.Add(AdminEvent.Analytics("ip.denied", $"IP bloqueado tentou acessar: {ip}", area));
        }

        return true;
    });

    return Results.Json(new { ok = false, message = "IP bloqueado pelo admin.", ip }, statusCode: StatusCodes.Status403Forbidden);
}

static string HashAnalyticsIdentifier(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "";
    }

    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"BalcaoLivreAnalytics|{value.Trim()}"));
    return Convert.ToHexString(bytes).ToLowerInvariant()[..24];
}

static void UpsertSiteAnalytics(AdminStore data, SiteAnalyticsEvent analyticsEvent)
{
    data.SiteAnalytics ??= [];
    if (!string.IsNullOrWhiteSpace(analyticsEvent.CheckoutSessionId) &&
        analyticsEvent.Type is AnalyticsEventType.CheckoutCompleted)
    {
        var existing = data.SiteAnalytics.FirstOrDefault(item =>
            item.Type == analyticsEvent.Type &&
            string.Equals(item.CheckoutSessionId, analyticsEvent.CheckoutSessionId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.When = analyticsEvent.When;
            existing.AmountCents = Math.Max(existing.AmountCents, analyticsEvent.AmountCents);
            existing.Currency = analyticsEvent.Currency.TrimOrDefault(existing.Currency.TrimOrDefault("BRL"));
            existing.Plan = analyticsEvent.Plan.TrimOrDefault(existing.Plan);
            existing.Billing = analyticsEvent.Billing.TrimOrDefault(existing.Billing);
            existing.StripeCustomerId = analyticsEvent.StripeCustomerId.TrimOrDefault(existing.StripeCustomerId);
            existing.SubscriptionId = analyticsEvent.SubscriptionId.TrimOrDefault(existing.SubscriptionId);
            TrimSiteAnalytics(data);
            return;
        }
    }

    data.SiteAnalytics.Add(analyticsEvent);
    TrimSiteAnalytics(data);
}

static void TrimSiteAnalytics(AdminStore data)
{
    var cutoff = DateTimeOffset.UtcNow.AddDays(-120);
    data.SiteAnalytics = data.SiteAnalytics
        .Where(item => item.When >= cutoff)
        .OrderByDescending(item => item.When)
        .Take(8000)
        .OrderBy(item => item.When)
        .ToList();
}

static string AnalyticsEventMessage(SiteAnalyticsEvent analyticsEvent)
{
    var plan = analyticsEvent.Plan.TrimOrDefault("plano");
    return analyticsEvent.Type switch
    {
        AnalyticsEventType.CheckoutCompleted => $"Compra Stripe confirmada: {plan}",
        AnalyticsEventType.CheckoutStarted => $"Checkout Stripe iniciado: {plan}",
        _ => $"Evento do site: {analyticsEvent.Type}"
    };
}

static bool MarkAppPayloadSeen(AdminStoreService store, AppClientPayload request, string eventType, string eventMessage)
{
    return MarkAppPayloadSeenWithReason(store, request, eventType, eventMessage, out _);
}

static bool MarkAppPayloadSeenWithReason(AdminStoreService store, AppClientPayload request, string eventType, string eventMessage, out string denyMessage)
{
    if (!HasValidAccountEmail(request, out _))
    {
        denyMessage = "Email da conta invalido. Abra Configuracoes e confirme o email usado na licenca.";
        return false;
    }

    var reason = "";
    var accepted = store.Update(data =>
    {
        var now = DateTimeOffset.UtcNow;
        var license = data.Licenses.FirstOrDefault(item => string.Equals(item.Key, request.LicenseKey, StringComparison.OrdinalIgnoreCase));
        if (license is null)
        {
            data.Events.Add(AdminEvent.Device($"{eventType}.denied", $"{eventMessage}: chave nao encontrada", request));
            reason = "Chave de licenca nao encontrada no admin. Gere ou ative essa licenca antes de publicar o cardapio.";
            return false;
        }

        LicenseTools.RefreshLicenseStatus(license, now);
        if (license.Status == LicenseStatus.Blocked || license.Status == LicenseStatus.Expired)
        {
            data.Events.Add(AdminEvent.License($"{eventType}.blocked", $"{eventMessage}: chave {license.Status}", license.Key));
            reason = license.Status == LicenseStatus.Expired
                ? "Licenca expirada no admin. Renove ou gere uma nova licenca antes de publicar o cardapio."
                : "Licenca bloqueada no admin. Desbloqueie essa licenca antes de publicar o cardapio.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(license.MachineHash) &&
            !string.Equals(license.MachineHash, request.MachineHash, StringComparison.Ordinal) &&
            !IsMobileClient(request))
        {
            data.Events.Add(AdminEvent.License($"{eventType}.other_pc", $"{eventMessage}: computador diferente", license.Key));
            reason = "Essa licenca ja esta vinculada a outro computador no admin.";
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
        license.ClientKind = NormalizeClientKind(request.ClientKind);
        license.AppVersion = request.AppVersion;
        license.Email = request.Profile.Email;
        license.BusinessName = request.Profile.BusinessName;
        license.Cnpj = request.Profile.Cnpj;
        license.OwnerName = request.Profile.OwnerName;
        license.Phone = request.Profile.Phone;
        license.Address = request.Profile.Address;
        license.City = request.Profile.City;
        license.State = request.Profile.State;
        license.ConfigSnapshot = request.Settings;
        license.MetricsSnapshot = request.Metrics;
        license.EnvironmentSnapshot = request.Environment;

        UpsertDevice(data, request, now);
        data.Events.Add(AdminEvent.Device(eventType, $"{eventMessage}: {request.Profile.BusinessName.TrimOrDefault(request.MachineCode)}", request));
        return true;
    });

    denyMessage = accepted ? "" : reason.TrimOrDefault("Admin recusou a publicacao do cardapio. Confira email, chave e computador da licenca.");
    return accepted;
}

static async Task WriteSseAsync(HttpContext context, string eventName, object payload, CancellationToken cancellation)
{
    await context.Response.WriteAsync($"event: {eventName}\n", cancellation);
    await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(payload, AdminJson.Options)}\n\n", cancellation);
    await context.Response.Body.FlushAsync(cancellation);
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
    device.Environment = request.Environment;
}

static bool IsMobileClient(AppClientPayload request)
{
    var kind = NormalizeClientKind(request.ClientKind);
    return string.Equals(kind, "android", StringComparison.Ordinal)
        || string.Equals(kind, "web", StringComparison.Ordinal)
        || string.Equals(kind, "browser", StringComparison.Ordinal)
        || request.MachineCode.StartsWith("AND-", StringComparison.OrdinalIgnoreCase)
        || request.MachineCode.StartsWith("WEB-", StringComparison.OrdinalIgnoreCase);
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

static string NormalizeAdminLogin(string value)
{
    return (value ?? "").Trim();
}

static bool VerifyAdminPassword(AdminUserRecord user, string password)
{
    if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
        string.IsNullOrWhiteSpace(user.PasswordSalt) ||
        user.PasswordIterations <= 0)
    {
        return false;
    }

    try
    {
        var salt = Convert.FromBase64String(user.PasswordSalt);
        var expected = Convert.FromBase64String(user.PasswordHash);
        if (salt.Length <= 0 || expected.Length <= 0)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password ?? ""),
            salt,
            user.PasswordIterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
    catch (FormatException)
    {
        return false;
    }
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

sealed class CommerceFulfillmentService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_supabaseUrl) &&
        !string.IsNullOrWhiteSpace(_supabaseKey);

    public CommerceFulfillmentService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _supabaseUrl = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_URL")
            ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? "").Trim().TrimEnd('/');
        _supabaseKey = (Environment.GetEnvironmentVariable("BVPDV_SUPABASE_SERVICE_ROLE_KEY")
            ?? Environment.GetEnvironmentVariable("BVPDV_SUPABASE_SECRET_KEY")
            ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
            ?? Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY")
            ?? "").Trim();
    }

    public async Task<string> ListAsync(CancellationToken cancellationToken)
    {
        const string selection =
            "id,store_id,status,model,recipient_name,recipient_phone,shipping_address," +
            "tracking_code,requested_at,shipped_at,delivered_at,created_at,updated_at," +
            "bl_stores(name,bl_accounts(email,phone,display_name))";
        var url = $"{_supabaseUrl}/rest/v1/bl_machine_fulfillments" +
            $"?select={Uri.EscapeDataString(selection)}&order=created_at.desc";
        using var request = CreateRequest(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return payload;
    }

    public async Task<string?> UpdateStatusAsync(
        string id,
        string status,
        string? trackingCode,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var fulfillmentId)) return null;

        var now = DateTimeOffset.UtcNow;
        var patch = new Dictionary<string, object?>
        {
            ["status"] = status,
            ["tracking_code"] = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim(),
            ["updated_at"] = now
        };
        if (status == "REQUESTED") patch["requested_at"] = now;
        if (status == "SHIPPED") patch["shipped_at"] = now;
        if (status == "DELIVERED") patch["delivered_at"] = now;

        var url = $"{_supabaseUrl}/rest/v1/bl_machine_fulfillments?id=eq.{fulfillmentId:D}";
        using var request = CreateRequest(HttpMethod.Patch, url);
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
        request.Content = JsonContent.Create(patch, options: AdminJson.Options);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return payload == "[]" ? null : payload;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", _supabaseKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}

sealed class AdminStoreService
{
    private static readonly TimeSpan RemoteRefreshInterval = TimeSpan.FromSeconds(3);
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
    private readonly HashSet<string> _ensuredSupabaseBuckets = new(StringComparer.OrdinalIgnoreCase);
    private bool _lastSupabaseOk;
    private long _revision;
    private string _lastFingerprint = "";
    private AdminStore? _cachedStore;
    private DateTimeOffset _lastChangedAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastRemoteRefreshAt = DateTimeOffset.MinValue;

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

    public AdminRealtimeSnapshot RefreshRealtimeState(bool forceRemote)
    {
        lock (_gate)
        {
            if (UsesSupabase)
            {
                RefreshSupabaseSnapshotUnsafe(forceRemote);
            }
            else if (string.IsNullOrWhiteSpace(_lastFingerprint))
            {
                _ = LoadUnsafe();
            }

            return new AdminRealtimeSnapshot(
                _revision,
                _lastChangedAt,
                StorageMode,
                UsesSupabase,
                DateTimeOffset.UtcNow);
        }
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

    public StripeCheckoutSummary ReadStripeCheckoutSummary()
    {
        if (!UsesSupabase)
        {
            return StripeCheckoutSummary.Empty("Supabase nao configurado.");
        }

        try
        {
            using var request = CreateSupabaseAuthRequest(
                HttpMethod.Get,
                "/rest/v1/bv_license_events?select=license_key,event_type,message,payload,created_at&event_type=in.(checkout.paid,checkout.renewed,checkout.paid.v2)&order=created_at.desc&limit=1000");
            using var response = _httpClient.Send(request);
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return StripeCheckoutSummary.Empty(body.TrimOrDefault(response.StatusCode.ToString()));
            }

            var records = JsonSerializer.Deserialize<List<SupabaseCheckoutEventRecord>>(body, AdminJson.Options) ?? [];
            return StripeCheckoutSummary.From(records);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return StripeCheckoutSummary.Empty(ex.Message);
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

    private static string NormalizeClockText(string? value)
    {
        var text = (value ?? "").Trim()
            .Replace('h', ':')
            .Replace('H', ':')
            .Replace('.', ':')
            .Replace(',', ':');
        if (string.IsNullOrWhiteSpace(text))
        {
            return "00:00";
        }

        int hour;
        int minute;
        if (text.Contains(':', StringComparison.Ordinal))
        {
            var parts = text.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out hour)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minute))
            {
                return "00:00";
            }
        }
        else if (text.Length is 3 or 4 && text.All(char.IsDigit))
        {
            var padded = text.PadLeft(4, '0');
            hour = int.Parse(padded[..2], CultureInfo.InvariantCulture);
            minute = int.Parse(padded[2..], CultureInfo.InvariantCulture);
        }
        else if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out hour))
        {
            return "00:00";
        }
        else
        {
            minute = 0;
        }

        return hour is < 0 or > 23 || minute is < 0 or > 59
            ? "00:00"
            : $"{hour:00}:{minute:00}";
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
                var baseSlug = NormalizePublicMenuSlug(request.Slug).TrimOrDefault("loja");
                var existingMenuId = FindPublicMenuIdByStoreIdUnsafe(request.LicenseKey);
                var slug = "";
                var menuId = "";
                var logoUrl = ResolvePublicMenuLogoUnsafe(request, baseSlug);
                var menuPayload = new Dictionary<string, object?>
                {
                    ["store_id"] = request.LicenseKey,
                    ["name"] = request.Profile.BusinessName.TrimOrDefault(request.Profile.LegalName.TrimOrDefault(request.Profile.OwnerName.TrimOrDefault("Balcao Livre"))),
                    ["description"] = request.Description.TrimOrDefault("Cardapio digital."),
                    ["phone"] = request.Profile.Phone,
                    ["address"] = request.Profile.Address,
                    ["city"] = request.Profile.City,
                    ["state"] = request.Profile.State,
                    ["logo_url"] = logoUrl,
                    ["theme_color"] = request.ThemeColor.TrimOrDefault("#0f766e"),
                    ["store_open"] = request.StoreOpen,
                    ["schedule_enabled"] = request.ScheduleEnabled,
                    ["open_time"] = NormalizeClockText(request.OpenTime),
                    ["close_time"] = NormalizeClockText(request.CloseTime),
                    ["wait_min_minutes"] = Math.Max(1, request.WaitMinMinutes),
                    ["wait_max_minutes"] = Math.Max(Math.Max(1, request.WaitMinMinutes), request.WaitMaxMinutes),
                    ["is_published"] = true,
                    ["updated_at"] = now
                };

                foreach (var candidate in PublicMenuSlugCandidates(baseSlug))
                {
                    menuPayload["slug"] = candidate;
                    var path = string.IsNullOrWhiteSpace(existingMenuId)
                        ? "/rest/v1/bv_public_menus"
                        : $"/rest/v1/bv_public_menus?id=eq.{Uri.EscapeDataString(existingMenuId)}";
                    using var write = CreateSupabaseAuthRequest(string.IsNullOrWhiteSpace(existingMenuId) ? HttpMethod.Post : HttpMethod.Patch, path);
                    write.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                    var writeJson = string.IsNullOrWhiteSpace(existingMenuId)
                        ? JsonSerializer.Serialize(new[] { menuPayload }, AdminJson.Options)
                        : JsonSerializer.Serialize(menuPayload, AdminJson.Options);
                    write.Content = new StringContent(writeJson, Encoding.UTF8, "application/json");
                    using var writeResponse = _httpClient.Send(write);
                    var writeBody = writeResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (writeResponse.IsSuccessStatusCode)
                    {
                        slug = candidate;
                        menuId = ReadFirstJsonProperty(writeBody, "id");
                        break;
                    }

                    if (IsPublicMenuSlugConflict(writeResponse, writeBody))
                    {
                        continue;
                    }

                    return AppPublicMenuPublishResponse.Fail($"Supabase recusou menu: {writeBody.TrimOrDefault(writeResponse.StatusCode.ToString())}");
                }

                if (string.IsNullOrWhiteSpace(menuId))
                {
                    return AppPublicMenuPublishResponse.Fail("Nao foi possivel gerar um link unico para esse cardapio.");
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

                return AppPublicMenuPublishResponse.Success(slug, BuildPublicMenuUrl(slug), items.Count, "Cardapio publicado.");
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
            var now = DateTimeOffset.UtcNow;
            if (_cachedStore is not null && now - _lastRemoteRefreshAt < RemoteRefreshInterval)
            {
                _lastSupabaseOk = true;
                return TrackStoreUnsafe(CloneStore(_cachedStore));
            }

            var remote = LoadFromSupabaseUnsafe();
            if (remote is not null)
            {
                _lastSupabaseOk = true;
                _lastRemoteRefreshAt = now;
                _cachedStore = CloneStore(remote);
                if (!_supabaseRequired)
                {
                    SaveLocalUnsafe(remote);
                }
                return TrackStoreUnsafe(remote);
            }

            _lastSupabaseOk = false;
            if (_cachedStore is not null)
            {
                return TrackStoreUnsafe(CloneStore(_cachedStore));
            }
        }

        if (_supabaseRequired)
        {
            return TrackStoreUnsafe(new AdminStore());
        }

        if (!File.Exists(_filePath))
        {
            var empty = new AdminStore();
            SaveUnsafe(empty);
            return TrackStoreUnsafe(empty);
        }

        try
        {
            return TrackStoreUnsafe(JsonSerializer.Deserialize<AdminStore>(File.ReadAllText(_filePath, Encoding.UTF8), AdminJson.Options) ?? new AdminStore());
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return TrackStoreUnsafe(new AdminStore());
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

        _cachedStore = CloneStore(store);
        _lastRemoteRefreshAt = DateTimeOffset.UtcNow;
        MarkRevisionUnsafe(store);
    }

    private void RefreshSupabaseSnapshotUnsafe(bool forceRemote)
    {
        var now = DateTimeOffset.UtcNow;
        if (!forceRemote && now - _lastRemoteRefreshAt < RemoteRefreshInterval)
        {
            return;
        }

        _lastRemoteRefreshAt = now;
        var remote = LoadFromSupabaseUnsafe();
        if (remote is null)
        {
            _lastSupabaseOk = false;
            return;
        }

        _lastSupabaseOk = true;
        _cachedStore = CloneStore(remote);
        if (!_supabaseRequired)
        {
            SaveLocalUnsafe(remote);
        }

        MarkRevisionUnsafe(remote);
    }

    private AdminStore TrackStoreUnsafe(AdminStore store)
    {
        MarkRevisionUnsafe(store);
        return store;
    }

    private static AdminStore CloneStore(AdminStore store)
    {
        return JsonSerializer.Deserialize<AdminStore>(
            JsonSerializer.Serialize(store, AdminJson.Options),
            AdminJson.Options) ?? new AdminStore();
    }

    private void MarkRevisionUnsafe(AdminStore store)
    {
        var fingerprint = ComputeStoreFingerprint(store);
        if (string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
        {
            return;
        }

        _lastFingerprint = fingerprint;
        _revision++;
        _lastChangedAt = DateTimeOffset.UtcNow;
    }

    private static string ComputeStoreFingerprint(AdminStore store)
    {
        var json = JsonSerializer.Serialize(store, AdminJson.Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
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
            var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            using var request = CreateSupabaseRequest(
                HttpMethod.Get,
                $"{SupabaseObjectPath(_supabaseObjectPath)}?t={cacheBuster}");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
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
            _lastSupabaseOk = false;
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

    private string FindPublicMenuIdByStoreIdUnsafe(string storeId)
    {
        using var lookup = CreateSupabaseAuthRequest(
            HttpMethod.Get,
            $"/rest/v1/bv_public_menus?store_id=eq.{Uri.EscapeDataString(storeId)}&select=id&order=updated_at.desc&limit=1");
        using var response = _httpClient.Send(lookup);
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Supabase recusou consulta de cardapio: {body.TrimOrDefault(response.StatusCode.ToString())}");
        }

        return ReadFirstJsonProperty(body, "id");
    }

    private static IEnumerable<string> PublicMenuSlugCandidates(string baseSlug)
    {
        var cleanBase = NormalizePublicMenuSlug(baseSlug).TrimOrDefault("loja");
        yield return cleanBase;
        for (var index = 1; index <= 999; index++)
        {
            yield return $"{index:000}-{cleanBase}";
        }
    }

    private static bool IsPublicMenuSlugConflict(HttpResponseMessage response, string body)
    {
        return response.StatusCode == System.Net.HttpStatusCode.Conflict
            || body.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || body.Contains("23505", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPublicMenuUrl(string slug)
    {
        return $"https://cardapio.balcaolivrepdv.com.br/{BuildPublicMenuPath(slug)}";
    }

    private static string BuildPublicMenuPath(string slug)
    {
        var normalized = NormalizePublicMenuSlug(slug);
        if (normalized.Length > 4 && normalized[3] == '-' && normalized[..3].All(char.IsDigit))
        {
            return $"{normalized[..3]}/{normalized[4..]}";
        }

        return normalized;
    }

    private void EnsureSupabaseBucketUnsafe()
    {
        EnsureSupabaseBucketUnsafe(_supabaseBucket, isPublic: false);
    }

    private void EnsureSupabaseBucketUnsafe(string bucket, bool isPublic)
    {
        if (_ensuredSupabaseBuckets.Contains(bucket))
        {
            return;
        }

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
            _ensuredSupabaseBuckets.Add(bucket);
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (body.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("exists", StringComparison.OrdinalIgnoreCase))
            {
                _ensuredSupabaseBuckets.Add(bucket);
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
    public List<AdminUserRecord> AdminUsers { get; set; } = [];
    public List<BlockedIpRecord> BlockedIps { get; set; } = [];
    public List<SiteAnalyticsEvent> SiteAnalytics { get; set; } = [];
    public List<LicenseRecord> Licenses { get; set; } = [];
    public List<DeviceRecord> Devices { get; set; } = [];
    public List<SupportTicketRecord> SupportTickets { get; set; } = [];
    public List<AdminEvent> Events { get; set; } = [];
}

sealed class AdminUserRecord
{
    public string User { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int PasswordIterations { get; set; } = 120000;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

sealed class BlockedIpRecord
{
    public string Id { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastBlockedAt { get; set; }
    public string LastBlockedArea { get; set; } = "";
    public int Hits { get; set; }
}

static class AnalyticsEventType
{
    public const string SiteVisit = "site.visit";
    public const string PlanView = "plan.view";
    public const string TrialDownload = "trial.download";
    public const string WhatsappClick = "whatsapp.click";
    public const string CheckoutStarted = "checkout.started";
    public const string CheckoutCompleted = "checkout.completed";
}

sealed class SiteAnalyticsEvent
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = AnalyticsEventType.SiteVisit;
    public string VisitorHash { get; set; } = "";
    public string SessionHash { get; set; } = "";
    public string Path { get; set; } = "";
    public string Url { get; set; } = "";
    public string Referrer { get; set; } = "";
    public string Source { get; set; } = "";
    public string Campaign { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Billing { get; set; } = "";
    public string CheckoutSessionId { get; set; } = "";
    public string StripeCustomerId { get; set; } = "";
    public string SubscriptionId { get; set; } = "";
    public string Currency { get; set; } = "BRL";
    public int AmountCents { get; set; }
    public string UserAgentHash { get; set; } = "";
    public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
}

sealed class PublicAnalyticsEventRequest
{
    public string Type { get; set; } = "";
    public string VisitorId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string Path { get; set; } = "";
    public string Url { get; set; } = "";
    public string Referrer { get; set; } = "";
    public string Source { get; set; } = "";
    public string Campaign { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Billing { get; set; } = "";
    public string CheckoutSessionId { get; set; } = "";
    public string StripeCustomerId { get; set; } = "";
    public string SubscriptionId { get; set; } = "";
    public string Currency { get; set; } = "BRL";
    public int AmountCents { get; set; }
}

sealed record AdminRealtimeSnapshot(
    long Revision,
    DateTimeOffset LastChangedAt,
    string StorageMode,
    bool SupabaseConfigured,
    DateTimeOffset ServerTime);

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
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Notes { get; set; } = "";
    public string MachineHash { get; set; } = "";
    public string MachineCode { get; set; } = "";
    public string ClientKind { get; set; } = "";
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
    public ClientEnvironmentSnapshot EnvironmentSnapshot { get; set; } = new();
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
    public ClientEnvironmentSnapshot Environment { get; set; } = new();
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
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public RestaurantProfileSnapshot Profile { get; set; } = new();
    public AppMetricsSnapshot Metrics { get; set; } = new();
    public ClientEnvironmentSnapshot Environment { get; set; } = new();
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

    public static AdminEvent Analytics(string type, string message, string plan) => new()
    {
        Type = type,
        Message = string.IsNullOrWhiteSpace(plan) ? message : $"{message} ({plan})",
        When = DateTimeOffset.UtcNow
    };
}

sealed class LoginRequest
{
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
}

sealed class VisitRoutePlanRequest
{
    public VisitRouteLocation CurrentLocation { get; set; } = new();
    public List<VisitRouteCandidate> Candidates { get; set; } = [];
}

sealed class VisitRouteLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

sealed class VisitRouteCandidate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Neighborhood { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Score { get; set; }
    public string Stage { get; set; } = "";
    public int DistanceMeters { get; set; }
}

sealed class VisitAiPlanResponse
{
    public List<string> OrderedIds { get; set; } = [];
    public string Summary { get; set; } = "";
    public List<VisitAiReason> Reasons { get; set; } = [];
}

sealed class VisitAiReason
{
    public string Id { get; set; } = "";
    public string Reason { get; set; } = "";
}

sealed class CreateLicenseRequest
{
    public string CustomerName { get; set; } = "";
    public string Plan { get; set; } = "";
    public int Amount { get; set; } = 30;
    public string Unit { get; set; } = "days";
    public string Notes { get; set; } = "";
}

sealed class UpdateFulfillmentStatusRequest
{
    public string? Status { get; set; }
    public string? TrackingCode { get; set; }
}

sealed class UpdateSupportStatusRequest
{
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
}

sealed class BlockIpRequest
{
    public string Ip { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Source { get; set; } = "";
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
    public ClientEnvironmentSnapshot Environment { get; set; } = new();
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
    public bool StoreOpen { get; set; } = true;
    public bool ScheduleEnabled { get; set; } = true;
    public string OpenTime { get; set; } = "00:00";
    public string CloseTime { get; set; } = "00:00";
    public int WaitMinMinutes { get; set; } = 30;
    public int WaitMaxMinutes { get; set; } = 60;
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

sealed class ClientEnvironmentSnapshot
{
    public string ClientProduct { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string WindowsUser { get; set; } = "";
    public string DomainName { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public string OSArchitecture { get; set; } = "";
    public string ProcessArchitecture { get; set; } = "";
    public string PrimaryLocalIp { get; set; } = "";
    public List<string> LocalIpAddresses { get; set; } = [];
    public string PublicIp { get; set; } = "";
    public string ForwardedFor { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string RequestHost { get; set; } = "";
    public string TimeZone { get; set; } = "";
    public string UtcOffset { get; set; } = "";
    public DateTimeOffset? ServerSeenAt { get; set; }
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

sealed class SupabaseCheckoutEventRecord
{
    [JsonPropertyName("license_key")]
    public string LicenseKey { get; set; } = "";
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = "";
    public string Message { get; set; } = "";
    public JsonElement Payload { get; set; }
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

sealed class StripePurchaseSummary
{
    public string LicenseKey { get; set; } = "";
    public string Type { get; set; } = "";
    public string Plan { get; set; } = "";
    public string CheckoutSessionId { get; set; } = "";
    public string Currency { get; set; } = "BRL";
    public int AmountCents { get; set; }
    public DateTimeOffset When { get; set; }
}

sealed class StripeCheckoutSummary
{
    public bool Ok { get; set; } = true;
    public string Error { get; set; } = "";
    public int TotalPurchases { get; set; }
    public int Purchases24h { get; set; }
    public long TotalRevenueCents { get; set; }
    public long CurrentMonthRevenueCents { get; set; }
    public long PreviousMonthRevenueCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public List<StripePurchaseSummary> RecentPurchases { get; set; } = [];
    public List<object> RevenueByDay { get; set; } = [];

    public static StripeCheckoutSummary Empty(string error = "") => new()
    {
        Ok = string.IsNullOrWhiteSpace(error),
        Error = error
    };

    public static StripeCheckoutSummary From(IEnumerable<SupabaseCheckoutEventRecord> records)
    {
        var now = DateTimeOffset.UtcNow;
        var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var purchases = records
            .Select(ToPurchase)
            .Where(item => !string.IsNullOrWhiteSpace(item.LicenseKey) || !string.IsNullOrWhiteSpace(item.CheckoutSessionId))
            .GroupBy(item => item.CheckoutSessionId.TrimOrDefault($"{item.LicenseKey}:{item.When:o}"), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.When).First())
            .OrderByDescending(item => item.When)
            .ToList();
        var currency = purchases.Select(item => item.Currency).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? "BRL";
        var currentMonthPurchases = purchases.Where(item => item.When >= currentMonthStart).ToList();
        var previousMonthPurchases = purchases
            .Where(item => item.When >= previousMonthStart && item.When < currentMonthStart)
            .ToList();

        return new StripeCheckoutSummary
        {
            TotalPurchases = purchases.Count,
            Purchases24h = purchases.Count(item => item.When >= now.AddHours(-24)),
            TotalRevenueCents = purchases.Sum(item => (long)Math.Max(0, item.AmountCents)),
            CurrentMonthRevenueCents = currentMonthPurchases.Sum(item => (long)Math.Max(0, item.AmountCents)),
            PreviousMonthRevenueCents = previousMonthPurchases.Sum(item => (long)Math.Max(0, item.AmountCents)),
            Currency = currency,
            RecentPurchases = purchases.Take(8).ToList(),
            RevenueByDay = currentMonthPurchases
                .GroupBy(item => item.When.UtcDateTime.Date)
                .OrderBy(group => group.Key)
                .Select(group => (object)new
                {
                    date = group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    revenueCents = group.Sum(item => (long)Math.Max(0, item.AmountCents))
                })
                .ToList()
        };
    }

    private static StripePurchaseSummary ToPurchase(SupabaseCheckoutEventRecord record)
    {
        var payload = record.Payload;
        var plan = PayloadString(payload, "plan_id").TrimOrDefault(PayloadString(payload, "plan"));
        var amountCents = PayloadInt(payload, "amount_total");
        if (amountCents <= 0)
        {
            amountCents = plan.ToLowerInvariant() switch
            {
                "basico-mensal" => 4990,
                "basico-anual" => 59880,
                "completo-mensal" => 9990,
                "completo-anual" => 119880,
                _ => 0
            };
        }
        return new StripePurchaseSummary
        {
            LicenseKey = record.LicenseKey,
            Type = record.EventType,
            Plan = plan,
            CheckoutSessionId = PayloadString(payload, "checkout_session_id"),
            Currency = PayloadString(payload, "currency").TrimOrDefault("BRL").ToUpperInvariant(),
            AmountCents = amountCents,
            When = PayloadDate(payload, "paid_at")
                ?? PayloadDate(payload, "renewed_at")
                ?? record.CreatedAt
        };
    }

    private static string PayloadString(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? "",
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => ""
        };
    }

    private static int PayloadInt(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return int.TryParse(PayloadString(payload, propertyName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static DateTimeOffset? PayloadDate(JsonElement payload, string propertyName)
    {
        return DateTimeOffset.TryParse(PayloadString(payload, propertyName), out var parsed)
            ? parsed
            : null;
    }
}

sealed class SiteAnalyticsDashboard
{
    public int TotalVisitors { get; set; }
    public int Visitors24h { get; set; }
    public int ViewsTotal { get; set; }
    public int Views24h { get; set; }
    public int CheckoutStartedTotal { get; set; }
    public int CheckoutStarted24h { get; set; }
    public int CheckoutCompletedTotal { get; set; }
    public int CheckoutCompleted24h { get; set; }
    public long CheckoutCompletedRevenueCents { get; set; }
    public int TrialDownloadsTotal { get; set; }
    public int WhatsappClicksTotal { get; set; }
    public List<object> TopPages { get; set; } = [];
    public List<SiteAnalyticsEvent> RecentEvents { get; set; } = [];

    public static SiteAnalyticsDashboard From(IEnumerable<SiteAnalyticsEvent> analytics)
    {
        var now = DateTimeOffset.UtcNow;
        var events = analytics
            .Where(item => item.When > DateTimeOffset.MinValue)
            .OrderByDescending(item => item.When)
            .ToList();
        var visits = events.Where(item => item.Type == AnalyticsEventType.SiteVisit).ToList();
        var started = events.Where(item => item.Type == AnalyticsEventType.CheckoutStarted).ToList();
        var completed = events
            .Where(item => item.Type == AnalyticsEventType.CheckoutCompleted)
            .GroupBy(item => item.CheckoutSessionId.TrimOrDefault(item.Id), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.When).First())
            .ToList();

        return new SiteAnalyticsDashboard
        {
            TotalVisitors = visits.Select(item => item.VisitorHash).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).Count(),
            Visitors24h = visits.Where(item => item.When >= now.AddHours(-24)).Select(item => item.VisitorHash).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).Count(),
            ViewsTotal = visits.Count,
            Views24h = visits.Count(item => item.When >= now.AddHours(-24)),
            CheckoutStartedTotal = started.Count,
            CheckoutStarted24h = started.Count(item => item.When >= now.AddHours(-24)),
            CheckoutCompletedTotal = completed.Count,
            CheckoutCompleted24h = completed.Count(item => item.When >= now.AddHours(-24)),
            CheckoutCompletedRevenueCents = completed.Sum(item => (long)Math.Max(0, item.AmountCents)),
            TrialDownloadsTotal = events.Count(item => item.Type == AnalyticsEventType.TrialDownload),
            WhatsappClicksTotal = events.Count(item => item.Type == AnalyticsEventType.WhatsappClick),
            TopPages = visits
                .GroupBy(item => item.Path.TrimOrDefault("/"))
                .Select(group => new { path = group.Key, views = group.Count(), visitors = group.Select(item => item.VisitorHash).Distinct(StringComparer.Ordinal).Count() })
                .OrderByDescending(item => item.views)
                .Take(8)
                .Cast<object>()
                .ToList(),
            RecentEvents = events
                .Where(item => item.Type != AnalyticsEventType.SiteVisit)
                .Take(12)
                .ToList()
        };
    }
}

sealed class AdminDashboard
{
    public object Metrics { get; set; } = new();
    public ClientIntelligenceDashboard ClientIntelligence { get; set; } = new();
    public SiteAnalyticsDashboard SiteAnalytics { get; set; } = new();
    public StripeCheckoutSummary Stripe { get; set; } = new();
    public IEnumerable<object> VersionDistribution { get; set; } = [];
    public IEnumerable<object> ActiveClientsByDay { get; set; } = [];
    public IEnumerable<LicenseRecord> ExpiringSoon { get; set; } = [];
    public IEnumerable<DeviceRecord> RecentDevices { get; set; } = [];
    public IEnumerable<AdminEvent> Events { get; set; } = [];

    public static AdminDashboard From(AdminStore store, StripeCheckoutSummary stripe)
    {
        var now = DateTimeOffset.UtcNow;
        var active = store.Licenses.Count(item => item.Status == LicenseStatus.Active && item.ExpiresAt > now);
        var available = store.Licenses.Count(item => item.Status == LicenseStatus.Available && item.ExpiresAt > now);
        var expired = store.Licenses.Count(item => item.Status == LicenseStatus.Expired || item.ExpiresAt <= now);
        var blocked = store.Licenses.Count(item => item.Status == LicenseStatus.Blocked);
        var online24h = store.Devices.Count(item => item.LastSeenAt >= now.AddHours(-24));
        var registeredUsers = store.Devices.Sum(item => item.Metrics.UsersCount);
        var openSupport = store.SupportTickets.Count(item => item.Status != SupportStatus.Resolved);
        var supportOpen = store.SupportTickets.Count(item => item.Status == SupportStatus.Open);
        var supportInProgress = store.SupportTickets.Count(item => item.Status == SupportStatus.Working);
        var expiringSoon30d = store.Licenses.Count(item =>
            item.Status != LicenseStatus.Blocked &&
            item.ExpiresAt > now &&
            item.ExpiresAt <= now.AddDays(30));
        var urgentSupport = store.SupportTickets.Count(item =>
            item.Status != SupportStatus.Resolved &&
            string.Equals(item.Priority, "URGENTE", StringComparison.OrdinalIgnoreCase));
        var site = SiteAnalyticsDashboard.From(store.SiteAnalytics);
        var stripePurchasesTotal = stripe.TotalPurchases > 0 ? stripe.TotalPurchases : site.CheckoutCompletedTotal;
        var stripePurchases24h = stripe.TotalPurchases > 0 ? stripe.Purchases24h : site.CheckoutCompleted24h;
        var stripeRevenueCents = stripe.TotalRevenueCents > 0 ? stripe.TotalRevenueCents : site.CheckoutCompletedRevenueCents;
        var stripeRevenueMonthCents = stripe.CurrentMonthRevenueCents > 0
            ? stripe.CurrentMonthRevenueCents
            : site.CheckoutCompletedRevenueCents;
        var conversionRate = site.TotalVisitors > 0
            ? Math.Round(stripePurchasesTotal * 100m / site.TotalVisitors, 1)
            : 0m;

        return new AdminDashboard
        {
            ClientIntelligence = ClientIntelligenceDashboard.From(store, stripe),
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
                waitingSupport = openSupport,
                supportOpen,
                supportInProgress,
                expiringSoon30d,
                urgentSupport,
                siteVisitors24h = site.Visitors24h,
                siteVisitorsTotal = site.TotalVisitors,
                siteViews24h = site.Views24h,
                siteViewsTotal = site.ViewsTotal,
                checkoutStarted24h = site.CheckoutStarted24h,
                checkoutStartedTotal = site.CheckoutStartedTotal,
                stripePurchases24h,
                stripePurchasesTotal,
                stripeRevenueCents,
                stripeRevenueMonthCents,
                stripeRevenuePreviousMonthCents = stripe.PreviousMonthRevenueCents,
                stripeConversionRate = conversionRate
            },
            SiteAnalytics = site,
            Stripe = stripe,
            VersionDistribution = store.Devices
                .GroupBy(item => string.IsNullOrWhiteSpace(item.AppVersion) ? "sem versao" : item.AppVersion)
                .Select(group => new { version = group.Key, count = group.Count() })
                .OrderByDescending(item => item.count),
            ActiveClientsByDay = store.Devices
                .Where(item => item.LastSeenAt >= now.AddDays(-90))
                .GroupBy(item => item.LastSeenAt.ToString("yyyy-MM-dd"))
                .Select(group => new { date = group.Key, count = group.Count() })
                .OrderBy(item => item.date)
                .Cast<object>(),
            ExpiringSoon = store.Licenses
                .Where(item => item.Status != LicenseStatus.Blocked && item.ExpiresAt > now && item.ExpiresAt <= now.AddDays(30))
                .OrderBy(item => item.ExpiresAt)
                .Take(8),
            RecentDevices = store.Devices.OrderByDescending(item => item.LastSeenAt).Take(10),
            Events = store.Events.OrderByDescending(item => item.When).Take(20)
        };
    }
}

sealed class ClientIntelligenceDashboard
{
    public ClientIntelligenceSummary Summary { get; set; } = new();
    public List<ClientIntelligenceItem> Clients { get; set; } = [];
    public int BlockedIpCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static ClientIntelligenceDashboard From(AdminStore store, StripeCheckoutSummary stripe)
    {
        var now = DateTimeOffset.UtcNow;
        var clients = new List<ClientIntelligenceItem>();
        var usedDeviceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var licenseGroup in store.Licenses
                     .GroupBy(ClientGroupKey)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var licenses = licenseGroup.ToList();
            var licenseKeys = licenses
                .Select(item => item.Key)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var identityCnpjs = licenses
                .Select(item => item.Cnpj)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var identityNames = licenses
                .SelectMany(item => new[] { item.BusinessName, item.CustomerName })
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var devices = store.Devices
                .Where(item =>
                    licenseKeys.Contains(item.LicenseKey) ||
                    identityCnpjs.Contains(item.Profile.Cnpj) ||
                    identityNames.Contains(item.Profile.BusinessName))
                .ToList();

            foreach (var device in devices)
            {
                usedDeviceIds.Add(device.Id);
            }

            clients.Add(BuildClient(licenses, devices, store, stripe, now));
        }

        foreach (var orphanGroup in store.Devices
                     .Where(item => !usedDeviceIds.Contains(item.Id))
                     .GroupBy(ClientGroupKey)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            clients.Add(BuildClient([], orphanGroup.ToList(), store, stripe, now));
        }

        clients = clients
            .OrderBy(PriorityRank)
            .ThenBy(item => item.HealthScore)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var healthy = clients.Count(item => item.HealthBand == "healthy");
        var attention = clients.Count(item => item.HealthBand == "attention");
        var critical = clients.Count(item => item.HealthBand == "critical");
        var synchronizedToday = clients.Count(item => item.LastSeenAt is not null && item.LastSeenAt >= now.AddHours(-24));

        return new ClientIntelligenceDashboard
        {
            Summary = new ClientIntelligenceSummary
            {
                TotalClients = clients.Count,
                ActiveClients = clients.Count(item => item.IsActive),
                HealthyClients = healthy,
                AttentionClients = attention,
                CriticalClients = critical,
                SynchronizedToday = synchronizedToday,
                SynchronizedTodayPercent = clients.Count == 0
                    ? 0
                    : (int)Math.Round(synchronizedToday * 100m / clients.Count)
            },
            Clients = clients,
            BlockedIpCount = store.BlockedIps.Count,
            UpdatedAt = now
        };
    }

    private static ClientIntelligenceItem BuildClient(
        List<LicenseRecord> licenses,
        List<DeviceRecord> devices,
        AdminStore store,
        StripeCheckoutSummary stripe,
        DateTimeOffset now)
    {
        var primaryLicense = licenses
            .OrderBy(item => item.Status == LicenseStatus.Active ? 0 : 1)
            .ThenByDescending(item => item.LastSeenAt)
            .ThenByDescending(item => item.ExpiresAt)
            .FirstOrDefault();
        var primaryDevice = devices.OrderByDescending(item => item.LastSeenAt).FirstOrDefault();
        var licenseKeys = licenses
            .Select(item => item.Key)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var machineCodes = devices
            .Select(item => item.MachineCode)
            .Concat(licenses.Select(item => item.MachineCode))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cnpj = primaryLicense?.Cnpj.TrimOrDefault(primaryDevice?.Profile.Cnpj ?? "")
            ?? primaryDevice?.Profile.Cnpj
            ?? "";
        var businessName = primaryLicense?.BusinessName.TrimOrDefault(primaryLicense.CustomerName)
            ?? primaryDevice?.Profile.BusinessName.TrimOrDefault(primaryDevice.Profile.LegalName.TrimOrDefault(primaryDevice.Profile.OwnerName))
            ?? "Cliente";
        var support = store.SupportTickets
            .Where(item =>
                licenseKeys.Contains(item.LicenseKey) ||
                (!string.IsNullOrWhiteSpace(cnpj) && string.Equals(item.Cnpj, cnpj, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(item.BusinessName.TrimOrDefault(item.CustomerName), businessName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var openSupport = support.Where(item => item.Status != SupportStatus.Resolved).ToList();
        var recentPayment = stripe.RecentPurchases
            .Where(item => licenseKeys.Contains(item.LicenseKey))
            .OrderByDescending(item => item.When)
            .FirstOrDefault();
        var lastSeenAt = devices.Select(item => (DateTimeOffset?)item.LastSeenAt)
            .Concat(licenses.Select(item => item.LastSeenAt))
            .Where(item => item is not null)
            .Max();

        var scoreTotal = 0d;
        var scoreWeight = 0d;
        void AddSignal(double weight, double value)
        {
            scoreTotal += weight * Math.Clamp(value, 0, 1);
            scoreWeight += weight;
        }

        if (licenses.Count > 0)
        {
            AddSignal(35, licenses.Average(item => LicenseHealth(item, now)));
        }
        else
        {
            AddSignal(35, 0.45);
        }

        var synchronizationExpected = devices.Count > 0 || licenses.Any(item =>
            item.Status == LicenseStatus.Active ||
            !string.IsNullOrWhiteSpace(item.MachineHash) ||
            !string.IsNullOrWhiteSpace(item.MachineCode));
        if (synchronizationExpected)
        {
            AddSignal(35, SyncHealth(lastSeenAt, now));
        }

        AddSignal(20, SupportHealth(openSupport));

        if (recentPayment is not null)
        {
            AddSignal(10, recentPayment.When >= now.AddDays(-45) ? 1 : 0.7);
        }

        var score = scoreWeight <= 0 ? 0 : (int)Math.Round(scoreTotal * 100 / scoreWeight);
        var healthBand = score >= 80 ? "healthy" : score >= 55 ? "attention" : "critical";
        var healthLabel = healthBand switch
        {
            "healthy" => "Saudável",
            "attention" => "Atenção",
            _ => "Crítico"
        };
        var reason = ResolveReason(licenses, lastSeenAt, openSupport, now);
        var actionView = reason.View;
        var activity = Enumerable.Range(0, 30).Select(_ => 0).ToArray();
        var startDate = now.Date.AddDays(-29);
        var matchingEvents = store.Events.Where(item =>
            licenseKeys.Contains(item.LicenseKey) ||
            machineCodes.Contains(item.MachineCode));
        foreach (var activityEvent in matchingEvents)
        {
            var index = (activityEvent.When.Date - startDate).Days;
            if (index is >= 0 and < 30) activity[index]++;
        }
        foreach (var ticket in support)
        {
            var index = (ticket.CreatedAt.Date - startDate).Days;
            if (index is >= 0 and < 30) activity[index]++;
        }
        if (lastSeenAt is not null)
        {
            var index = (lastSeenAt.Value.Date - startDate).Days;
            if (index is >= 0 and < 30) activity[index] = Math.Max(1, activity[index]);
        }

        var history = new List<ClientHistoryItem>();
        if (lastSeenAt is not null)
        {
            history.Add(new ClientHistoryItem { Type = "sync", Label = "Última sincronização", When = lastSeenAt.Value });
        }
        if (primaryLicense is not null)
        {
            history.Add(new ClientHistoryItem { Type = "license", Label = $"Licença {primaryLicense.Status.ToLowerInvariant()}", When = primaryLicense.ExpiresAt });
        }
        if (recentPayment is not null)
        {
            history.Add(new ClientHistoryItem { Type = "payment", Label = "Pagamento confirmado", When = recentPayment.When });
        }
        history.AddRange(openSupport.Select(item => new ClientHistoryItem
        {
            Type = "support",
            Label = $"Chamado {item.ShortId} · {item.Status.ToLowerInvariant()}",
            When = item.UpdatedAt > DateTimeOffset.MinValue ? item.UpdatedAt : item.CreatedAt
        }));

        var profile = primaryDevice?.Profile;
        var ownerName = primaryLicense?.OwnerName.TrimOrDefault(profile?.OwnerName ?? "") ?? profile?.OwnerName ?? "";
        var email = primaryLicense?.Email.TrimOrDefault(profile?.Email ?? "") ?? profile?.Email ?? "";
        var phone = primaryLicense?.Phone.TrimOrDefault(profile?.Phone ?? "") ?? profile?.Phone ?? "";
        var city = primaryLicense?.City.TrimOrDefault(profile?.City ?? "") ?? profile?.City ?? "";
        var state = primaryLicense?.State.TrimOrDefault(profile?.State ?? "") ?? profile?.State ?? "";

        return new ClientIntelligenceItem
        {
            Id = primaryLicense?.Id.TrimOrDefault(primaryDevice?.Id ?? businessName) ?? primaryDevice?.Id.TrimOrDefault(businessName) ?? businessName,
            Name = businessName,
            Initials = Initials(businessName),
            Cnpj = cnpj,
            City = city,
            State = state,
            Plan = primaryLicense?.Plan.TrimOrDefault("Sem plano") ?? "Sem plano",
            LicenseId = primaryLicense?.Id ?? "",
            LicenseKey = primaryLicense?.Key ?? primaryDevice?.LicenseKey ?? "",
            LicenseStatus = primaryLicense?.Status ?? "SEM_LICENCA",
            ClientSinceAt = primaryLicense?.CreatedAt ?? primaryDevice?.FirstSeenAt,
            ExpiresAt = primaryLicense?.ExpiresAt,
            LastSeenAt = lastSeenAt,
            SyncLabel = SyncLabel(lastSeenAt, now),
            OwnerName = ownerName,
            Email = email,
            Phone = phone,
            DeviceCount = devices.Count,
            LicenseCount = licenses.Count,
            OpenSupportCount = openSupport.Count,
            RecentPaymentAt = recentPayment?.When,
            HealthScore = score,
            HealthBand = healthBand,
            HealthLabel = healthLabel,
            Confidence = (int)Math.Round(scoreWeight),
            ConfidenceLabel = scoreWeight >= 90 ? "alta" : scoreWeight >= 70 ? "média" : "parcial",
            Reason = reason.Reason,
            SuggestedAction = reason.Action,
            ActionView = actionView,
            IsActive = licenses.Any(item => item.Status == LicenseStatus.Active && item.ExpiresAt > now),
            Activity30d = activity.ToList(),
            History = history.OrderByDescending(item => item.When).Take(8).ToList()
        };
    }

    private static double LicenseHealth(LicenseRecord license, DateTimeOffset now)
    {
        if (license.Status == LicenseStatus.Blocked) return 0.05;
        if (license.Status == LicenseStatus.Expired || license.ExpiresAt <= now) return 0;
        var days = (license.ExpiresAt - now).TotalDays;
        if (days <= 3) return 0.35;
        if (days <= 30) return 0.65;
        if (license.Status == LicenseStatus.Available) return 0.6;
        return 1;
    }

    private static int PriorityRank(ClientIntelligenceItem item)
    {
        var action = item.SuggestedAction;
        if (action.Contains("Renovar", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Revisar licença", StringComparison.OrdinalIgnoreCase)) return 0;
        if (action.Contains("sincronização", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("atualização", StringComparison.OrdinalIgnoreCase)) return 1;
        if (action.Contains("atendimento", StringComparison.OrdinalIgnoreCase)) return 2;
        if (item.HealthBand == "critical") return 3;
        if (item.HealthBand == "attention") return 4;
        return 5;
    }

    private static double SyncHealth(DateTimeOffset? lastSeenAt, DateTimeOffset now)
    {
        if (lastSeenAt is null) return 0.15;
        var age = now - lastSeenAt.Value;
        if (age <= TimeSpan.FromHours(24)) return 1;
        if (age <= TimeSpan.FromHours(48)) return 0.72;
        if (age <= TimeSpan.FromDays(7)) return 0.45;
        return 0.1;
    }

    private static double SupportHealth(List<SupportTicketRecord> tickets)
    {
        if (tickets.Any(item => string.Equals(item.Priority, "URGENTE", StringComparison.OrdinalIgnoreCase))) return 0.1;
        if (tickets.Any(item => item.Status == SupportStatus.Open)) return 0.55;
        if (tickets.Any(item => item.Status == SupportStatus.Working)) return 0.72;
        return 1;
    }

    private static (string Reason, string Action, string View) ResolveReason(
        List<LicenseRecord> licenses,
        DateTimeOffset? lastSeenAt,
        List<SupportTicketRecord> support,
        DateTimeOffset now)
    {
        if (licenses.Any(item => item.Status == LicenseStatus.Blocked))
            return ("Licença bloqueada", "Revisar licença", "licenses");
        if (licenses.Any(item => item.Status == LicenseStatus.Expired || item.ExpiresAt <= now))
            return ("Licença vencida", "Renovar licença", "licenses");
        var nextExpiration = licenses.Where(item => item.ExpiresAt > now).OrderBy(item => item.ExpiresAt).FirstOrDefault();
        if (nextExpiration is not null && nextExpiration.ExpiresAt <= now.AddDays(30))
        {
            var days = Math.Max(1, (int)Math.Ceiling((nextExpiration.ExpiresAt - now).TotalDays));
            return ($"Licença vence em {days} dia(s)", "Renovar licença", "licenses");
        }
        if (lastSeenAt is null && licenses.Any(item => item.Status == LicenseStatus.Active))
            return ("Sem sincronização registrada", "Ver sincronização", "devices");
        if (lastSeenAt is not null && lastSeenAt < now.AddHours(-48))
            return ($"Sem sincronizar há {HumanAge(now - lastSeenAt.Value)}", "Ver sincronização", "devices");
        var urgent = support.FirstOrDefault(item => string.Equals(item.Priority, "URGENTE", StringComparison.OrdinalIgnoreCase));
        if (urgent is not null) return ($"Chamado urgente {urgent.ShortId}", "Abrir atendimento", "support");
        var open = support.FirstOrDefault();
        if (open is not null) return ($"Chamado {open.ShortId} em aberto", "Abrir atendimento", "support");
        if (licenses.Count == 0) return ("Sem licença vinculada", "Vincular licença", "licenses");
        return ("Tudo em dia", "Ver detalhes", "devices");
    }

    private static string ClientGroupKey(LicenseRecord item) =>
        item.Cnpj.TrimOrDefault(item.BusinessName.TrimOrDefault(item.CustomerName.TrimOrDefault(item.Key))).Trim().ToUpperInvariant();

    private static string ClientGroupKey(DeviceRecord item) =>
        item.Profile.Cnpj.TrimOrDefault(item.LicenseKey.TrimOrDefault(item.Profile.BusinessName.TrimOrDefault(item.MachineCode))).Trim().ToUpperInvariant();

    private static string Initials(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "CL";
        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }

    private static string SyncLabel(DateTimeOffset? value, DateTimeOffset now)
    {
        if (value is null) return "Sem sincronização";
        var age = now - value.Value;
        if (age <= TimeSpan.FromMinutes(2)) return "Agora";
        if (age <= TimeSpan.FromHours(1)) return $"Há {Math.Max(1, (int)age.TotalMinutes)} min";
        if (age <= TimeSpan.FromDays(1)) return $"Há {Math.Max(1, (int)age.TotalHours)}h";
        return $"Há {Math.Max(1, (int)age.TotalDays)} dia(s)";
    }

    private static string HumanAge(TimeSpan age)
    {
        if (age < TimeSpan.FromDays(2)) return $"{Math.Max(1, (int)age.TotalHours)}h";
        return $"{Math.Max(1, (int)age.TotalDays)} dias";
    }
}

sealed class ClientIntelligenceSummary
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int HealthyClients { get; set; }
    public int AttentionClients { get; set; }
    public int CriticalClients { get; set; }
    public int SynchronizedToday { get; set; }
    public int SynchronizedTodayPercent { get; set; }
}

sealed class ClientIntelligenceItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Plan { get; set; } = "";
    public string LicenseId { get; set; } = "";
    public string LicenseKey { get; set; } = "";
    public string LicenseStatus { get; set; } = "";
    public DateTimeOffset? ClientSinceAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string SyncLabel { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public int DeviceCount { get; set; }
    public int LicenseCount { get; set; }
    public int OpenSupportCount { get; set; }
    public DateTimeOffset? RecentPaymentAt { get; set; }
    public int HealthScore { get; set; }
    public string HealthBand { get; set; } = "";
    public string HealthLabel { get; set; } = "";
    public int Confidence { get; set; }
    public string ConfidenceLabel { get; set; } = "";
    public string Reason { get; set; } = "";
    public string SuggestedAction { get; set; } = "";
    public string ActionView { get; set; } = "";
    public bool IsActive { get; set; }
    public List<int> Activity30d { get; set; } = [];
    public List<ClientHistoryItem> History { get; set; } = [];
}

sealed class ClientHistoryItem
{
    public string Type { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTimeOffset When { get; set; }
}
