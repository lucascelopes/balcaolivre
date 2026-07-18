using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const string WhatsAppLocalApiBaseUrl = "http://127.0.0.1:8090";
    private readonly HttpClient _whatsAppRealtimeClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private CancellationTokenSource? _whatsAppRealtimeCancellation;
    private Task? _whatsAppRealtimeTask;
    private long _whatsAppRealtimeCursor;

    private string WhatsAppLocalApiTokenPath() =>
        Path.Combine(_store.DataRoot, "bot-runtime", "agenda-local-api-token");

    private string WhatsAppRealtimeInstanceName()
    {
        if (!IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            return NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName);
        }

        var license = BuildAgendaLivreWhatsAppLicense().Trim().ToUpperInvariant();
        var seed = $"balcao-whatsapp:evolution:v1:{license}";
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed))).ToLowerInvariant();
        return $"bl-{digest[..32]}";
    }

    private string WhatsAppRealtimeInstanceQuery() =>
        $"instance={Uri.EscapeDataString(WhatsAppRealtimeInstanceName())}";

    private void StartWhatsAppRealtime()
    {
        if (_whatsAppRealtimeTask is { IsCompleted: false })
        {
            return;
        }

        WriteWhatsAppRealtimeDiagnostic($"start instance={WhatsAppRealtimeInstanceName()}");
        _whatsAppRealtimeCancellation?.Dispose();
        _whatsAppRealtimeCancellation = new CancellationTokenSource();
        _whatsAppRealtimeTask = Task.Run(
            () => RunWhatsAppRealtimeLoopAsync(_whatsAppRealtimeCancellation.Token),
            _whatsAppRealtimeCancellation.Token);
    }

    private void MainWindow_WhatsAppRealtimeClosed(object? sender, EventArgs e)
    {
        _whatsAppRealtimeCancellation?.Cancel();
        _whatsAppRealtimeClient.Dispose();
    }

    private async Task RunWhatsAppRealtimeLoopAsync(CancellationToken cancellationToken)
    {
        var retryDelay = TimeSpan.FromSeconds(1);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var token = await ReadWhatsAppLocalApiTokenAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(token))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                await BackfillWhatsAppRealtimeSnapshotAsync(token, cancellationToken);
                await ConsumeWhatsAppRealtimeEventsAsync(token, cancellationToken);
                retryDelay = TimeSpan.FromSeconds(1);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException)
            {
                Debug.WriteLine($"Agenda WhatsApp realtime reconnecting: {ex.Message}");
                WriteWhatsAppRealtimeDiagnostic("reconnect", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Agenda WhatsApp realtime unexpected failure: {ex}");
                WriteWhatsAppRealtimeDiagnostic("unexpected", ex);
            }

            await Task.Delay(retryDelay, cancellationToken);
            retryDelay = TimeSpan.FromSeconds(Math.Min(15, retryDelay.TotalSeconds * 2));
        }
    }

    private async Task<string> ReadWhatsAppLocalApiTokenAsync(CancellationToken cancellationToken)
    {
        var path = WhatsAppLocalApiTokenPath();
        if (!File.Exists(path))
        {
            return "";
        }

        try
        {
            return (await File.ReadAllTextAsync(path, cancellationToken)).Trim();
        }
        catch (IOException)
        {
            return "";
        }
    }

    private HttpRequestMessage CreateWhatsAppLocalApiRequest(HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{WhatsAppLocalApiBaseUrl}/{path.TrimStart('/')}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<WhatsAppEvolutionResult> SendWhatsAppLocalApiTextAsync(
        string phone,
        string text,
        string customerName,
        string requestId,
        string token)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var request = CreateWhatsAppLocalApiRequest(
            HttpMethod.Post,
            $"/api/agenda/send?{WhatsAppRealtimeInstanceQuery()}",
            token,
            new
            {
                requestId,
                phone = NormalizeBrazilPhone(phone),
                text,
                customerName = FirstFilled(customerName, FormatPhone(phone))
            });

        try
        {
            using var response = await _whatsAppRealtimeClient.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (WhatsAppManualSendPolicy.AllowsLegacyFallback((int)response.StatusCode, body))
            {
                return new WhatsAppEvolutionResult
                {
                    Ok = false,
                    EndpointNotFound = true,
                    Message = "O bot local ainda não oferece a rota de envio."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Conflict &&
                    TryParseExistingWhatsAppPending(body, out var existingPending))
                {
                    return existingPending;
                }

                return WhatsAppEvolutionResult.Fail(
                    ReadEvolutionMessage(body),
                    IsAmbiguousWhatsAppHttpStatus(response.StatusCode));
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return WhatsAppEvolutionResult.Success("Mensagem aceita pelo bot local.");
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var responseStatus = ReadRealtimeString(root, "status", "deliveryStatus", "messageStatus");
                var hasAccepted = TryGetJsonProperty(root, "accepted", out _);
                var accepted = hasAccepted && ReadRealtimeBool(root, "accepted");
                var existingPending = hasAccepted &&
                    WhatsAppManualSendPolicy.IsExistingPending(accepted, responseStatus);
                var providerMessageId = ReadRealtimeString(root, "providerMessageId", "remoteMessageId");
                if (string.IsNullOrWhiteSpace(providerMessageId) &&
                    TryGetJsonProperty(root, "key", out var key) &&
                    key.ValueKind == JsonValueKind.Object)
                {
                    providerMessageId = ReadRealtimeString(key, "id", "messageId");
                }

                if (existingPending)
                {
                    return new WhatsAppEvolutionResult
                    {
                        Ok = true,
                        Pending = true,
                        ExistingPending = true,
                        DeliveryStatus = "pendente",
                        ProviderMessageId = providerMessageId,
                        Message = FirstFilled(
                            ReadRealtimeString(root, "message", "detail"),
                            "Esta tentativa já estava em processamento.")
                    };
                }

                if (TryGetJsonProperty(root, "ok", out var okValue) &&
                    okValue.ValueKind == JsonValueKind.False)
                {
                    return WhatsAppEvolutionResult.Fail(
                        FirstFilled(
                            ReadRealtimeString(root, "message", "error", "detail"),
                            "O bot local recusou a mensagem."));
                }

                if (hasAccepted && !accepted)
                {
                    return WhatsAppEvolutionResult.Fail(
                        FirstFilled(
                            ReadRealtimeString(root, "message", "error", "detail"),
                            "O bot local não aceitou a mensagem."));
                }

                return new WhatsAppEvolutionResult
                {
                    Ok = true,
                    Pending = NormalizeWhatsAppDeliveryStatus(responseStatus) is "pendente" or "incerto",
                    DeliveryStatus = NormalizeWhatsAppDeliveryStatus(responseStatus),
                    ProviderMessageId = providerMessageId,
                    Message = FirstFilled(
                        ReadRealtimeString(root, "message", "detail"),
                        "Mensagem aceita pelo bot local.")
                };
            }
            catch (JsonException)
            {
                return WhatsAppEvolutionResult.Success("Mensagem aceita pelo bot local.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or ObjectDisposedException)
        {
            return WhatsAppEvolutionResult.Fail(
                $"Falha no envio pelo bot local: {ex.Message}",
                deliveryUncertain: true);
        }
    }

    private static bool TryParseExistingWhatsAppPending(
        string body,
        out WhatsAppEvolutionResult result)
    {
        result = WhatsAppEvolutionResult.Fail("A tentativa anterior não está mais em processamento.");
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var responseStatus = ReadRealtimeString(root, "status", "deliveryStatus", "messageStatus");
            var accepted = ReadRealtimeBool(root, "accepted");
            if (!WhatsAppManualSendPolicy.IsExistingPending(accepted, responseStatus))
            {
                return false;
            }

            var providerMessageId = ReadRealtimeString(root, "providerMessageId", "remoteMessageId");
            result = new WhatsAppEvolutionResult
            {
                Ok = true,
                Pending = true,
                ExistingPending = true,
                DeliveryStatus = "pendente",
                ProviderMessageId = providerMessageId,
                Message = FirstFilled(
                    ReadRealtimeString(root, "message", "detail"),
                    "Esta tentativa já estava em processamento.")
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task BackfillWhatsAppRealtimeSnapshotAsync(string token, CancellationToken cancellationToken)
    {
        using var request = CreateWhatsAppLocalApiRequest(
            HttpMethod.Get,
            $"/api/agenda/snapshot?{WhatsAppRealtimeInstanceQuery()}",
            token);
        using var response = await _whatsAppRealtimeClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Snapshot local respondeu HTTP {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var cursor = ReadRealtimeInt64(root, "cursor");
        var leads = ReadRealtimeArray(root, "leads")
            .Select(ParseWhatsAppRealtimeLead)
            .Where(item => item is not null)
            .Cast<WhatsAppLead>()
            .ToList();
        var messages = ReadRealtimeArray(root, "messages")
            .Select(ParseWhatsAppRealtimeMessage)
            .Where(item => item is not null)
            .Cast<WhatsAppMessage>()
            .ToList();
        var bookings = ReadRealtimeArray(root, "bookings")
            .Select(ParseWhatsAppBookingRequest)
            .Where(item => item is not null)
            .Cast<WhatsAppBookingRequest>()
            .ToList();

        WriteWhatsAppRealtimeDiagnostic(
            $"snapshot instance={WhatsAppRealtimeInstanceName()} cursor={cursor} leads={leads.Count} messages={messages.Count} bookings={bookings.Count}");
        await Dispatcher.InvokeAsync(() => MergeWhatsAppRealtimeSnapshot(
            messages,
            leads,
            cursor,
            authoritative: true));
        WriteWhatsAppRealtimeDiagnostic($"snapshot-merged cursor={cursor}");
        foreach (var booking in bookings)
        {
            await ProcessWhatsAppBookingRequestAsync(booking, cancellationToken);
        }
    }

    private void WriteWhatsAppRealtimeDiagnostic(string message, Exception? exception = null)
    {
        try
        {
            var directory = Path.Combine(_store.DataRoot, "bot-runtime");
            Directory.CreateDirectory(directory);
            var line = $"{DateTimeOffset.Now:O} {message}";
            if (exception is not null)
            {
                line += $" | {exception.GetType().Name}: {exception.Message}";
            }

            File.AppendAllText(
                Path.Combine(directory, "agenda-realtime.log"),
                line + Environment.NewLine,
                Encoding.UTF8);
        }
        catch
        {
            // Diagnostics must never interrupt the realtime connection.
        }
    }

    private async Task ConsumeWhatsAppRealtimeEventsAsync(string token, CancellationToken cancellationToken)
    {
        var cursor = Interlocked.Read(ref _whatsAppRealtimeCursor);
        using var request = CreateWhatsAppLocalApiRequest(
            HttpMethod.Get,
            $"/api/agenda/events?{WhatsAppRealtimeInstanceQuery()}&cursor={cursor.ToString(CultureInfo.InvariantCulture)}",
            token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var response = await _whatsAppRealtimeClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Eventos locais responderam HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var eventName = "";
        var eventId = "";
        var data = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            if (line.Length == 0)
            {
                if (data.Length > 0)
                {
                    await DispatchWhatsAppRealtimeEventAsync(eventName, eventId, data.ToString(), cancellationToken);
                }

                eventName = "";
                eventId = "";
                data.Clear();
                continue;
            }

            if (line.StartsWith(':'))
            {
                continue;
            }

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                eventName = line[6..].Trim();
            }
            else if (line.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                eventId = line[3..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (data.Length > 0)
                {
                    data.Append('\n');
                }

                data.Append(line[5..].TrimStart());
            }
        }
    }

    private async Task DispatchWhatsAppRealtimeEventAsync(
        string eventName,
        string eventId,
        string data,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;
        var cursor = ReadRealtimeInt64(root, "cursor");
        if (cursor <= 0 && long.TryParse(eventId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
        {
            cursor = parsedId;
        }

        WhatsAppMessage? message = null;
        WhatsAppLead? lead = null;
        WhatsAppBookingRequest? booking = null;
        if (eventName.Equals("whatsapp.message", StringComparison.OrdinalIgnoreCase))
        {
            message = TryGetRealtimeProperty(root, "message", out var messageElement)
                ? ParseWhatsAppRealtimeMessage(messageElement)
                : ParseWhatsAppRealtimeMessage(root);
            if (TryGetRealtimeProperty(root, "lead", out var leadElement))
            {
                lead = ParseWhatsAppRealtimeLead(leadElement);
            }
        }
        else if (eventName.Equals("lead.updated", StringComparison.OrdinalIgnoreCase))
        {
            lead = TryGetRealtimeProperty(root, "lead", out var leadElement)
                ? ParseWhatsAppRealtimeLead(leadElement)
                : ParseWhatsAppRealtimeLead(root);
        }
        else if (eventName.Equals("booking.requested", StringComparison.OrdinalIgnoreCase))
        {
            booking = TryGetRealtimeProperty(root, "booking", out var bookingElement)
                ? ParseWhatsAppBookingRequest(bookingElement)
                : ParseWhatsAppBookingRequest(root);
        }
        else if (eventName.Equals("booking.updated", StringComparison.OrdinalIgnoreCase) ||
                 eventName.Equals("booking.resolved", StringComparison.OrdinalIgnoreCase))
        {
            booking = TryGetRealtimeProperty(root, "booking", out var bookingElement)
                ? ParseWhatsAppBookingRequest(bookingElement)
                : ParseWhatsAppBookingRequest(root);
        }
        else
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Dispatcher.InvokeAsync(() => MergeWhatsAppRealtimeSnapshot(
            message is null ? [] : [message],
            lead is null ? [] : [lead],
            cursor));
        if (booking is not null)
        {
            await ProcessWhatsAppBookingRequestAsync(booking, cancellationToken);
        }
    }

    private void MergeWhatsAppRealtimeSnapshot(
        IReadOnlyCollection<WhatsAppMessage> messages,
        IReadOnlyCollection<WhatsAppLead> leads,
        long cursor,
        bool authoritative = false)
    {
        var expectedInstance = WhatsAppRealtimeInstanceName();
        var changed = _data.WhatsAppMessages.RemoveAll(item =>
                          !string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase)) > 0;
        changed |= _data.WhatsAppLeads.RemoveAll(item =>
                       !string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase)) > 0;
        var newIncoming = false;
        var failedOutgoing = false;
        WhatsAppMessage? latestIncoming = null;
        foreach (var lead in leads.Where(item =>
                     string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase)))
        {
            changed |= UpsertWhatsAppRealtimeLead(lead);
        }

        foreach (var message in messages
                     .Where(item => string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(item => item.CreatedAt))
        {
            var result = UpsertWhatsAppRealtimeMessage(message);
            changed |= result.Changed;
            newIncoming |= result.AddedIncoming;
            failedOutgoing |= result.FailedOutgoing;
            if (result.AddedIncoming && (latestIncoming is null || message.CreatedAt >= latestIncoming.CreatedAt))
            {
                latestIncoming = message;
            }
        }

        if (authoritative)
        {
            var consolidation = WhatsAppMessageIdentityReconciler.ConsolidateAuthoritativeExactDuplicates(
                _data.WhatsAppMessages,
                messages.Where(item =>
                    string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase)),
                MergeWhatsAppDuplicateLocalState);
            changed |= consolidation.Changed;

            var snapshotLeadIds = leads
                .Where(item => string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Id)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            changed |= _data.WhatsAppLeads.RemoveAll(item =>
                string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase) &&
                !snapshotLeadIds.Contains(item.Id)) > 0;

            var snapshotByProviderId = messages
                .Where(item => !string.IsNullOrWhiteSpace(item.ProviderMessageId))
                .GroupBy(item => item.ProviderMessageId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var local in _data.WhatsAppMessages.Where(item =>
                         string.Equals(item.Instance, expectedInstance, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(local.ProviderMessageId) &&
                    snapshotByProviderId.TryGetValue(local.ProviderMessageId, out var source))
                {
                    changed |= SetIfFilled(local.Phone, NormalizeBrazilPhone(source.Phone), value => local.Phone = value);
                    changed |= SetIfFilled(local.LeadId, source.LeadId, value => local.LeadId = value);
                    changed |= SetIfFilled(local.ConversationId, source.ConversationId, value => local.ConversationId = value);
                    var currentStatus = NormalizeWhatsAppDeliveryStatus(local.Status, IsWhatsAppIncoming(local));
                    var sourceStatus = NormalizeWhatsAppDeliveryStatus(source.Status, IsWhatsAppIncoming(source));
                    if (WhatsAppManualSendPolicy.CanTransitionDeliveryStatus(currentStatus, sourceStatus))
                    {
                        local.Status = sourceStatus;
                        changed = true;
                    }
                }
            }

        }

        if (cursor > 0)
        {
            var current = Interlocked.Read(ref _whatsAppRealtimeCursor);
            if (cursor > current)
            {
                Interlocked.Exchange(ref _whatsAppRealtimeCursor, cursor);
            }
        }

        WriteWhatsAppRealtimeDiagnostic(
            $"merge authoritative={authoritative} changed={changed} localLeads={_data.WhatsAppLeads.Count} localMessages={_data.WhatsAppMessages.Count}");
        if (!changed)
        {
            return;
        }

        if (newIncoming && latestIncoming is not null)
        {
            _selectedWhatsAppReplyPhone = NormalizeBrazilPhone(latestIncoming.Phone);
            _selectedWhatsAppReplyName = NormalizeWhatsAppBookingCustomerName(
                latestIncoming.CustomerName,
                latestIncoming.Phone);
            _whatsAppConversationOpen = true;
            WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        }

        if (_whatsAppConversationOpen && WhatsAppFloatingPanel.Visibility == Visibility.Visible)
        {
            MarkWhatsAppConversationReadCore(_selectedWhatsAppReplyPhone);
        }

        _data.Settings.WhatsAppLastMessageAt = DateTime.Now;
        _data.WhatsAppMessages = _data.WhatsAppMessages
            .OrderByDescending(item => item.CreatedAt)
            .Take(1000)
            .ToList();

        _store.Save(_data);
        RefreshWhatsAppSurface();
        if (newIncoming)
        {
            ShowStatus("Nova mensagem do WhatsApp recebida.");
        }
        else if (failedOutgoing)
        {
            ShowStatus("O WhatsApp não entregou uma mensagem. Ela foi marcada em vermelho.");
        }
    }

    private (bool Changed, bool AddedIncoming, bool FailedOutgoing) UpsertWhatsAppRealtimeMessage(WhatsAppMessage incoming)
    {
        var exact = WhatsAppMessageIdentityReconciler.ConsolidateExactMatches(
            _data.WhatsAppMessages,
            incoming,
            MergeWhatsAppDuplicateLocalState);
        var providerId = FirstFilled(incoming.ProviderMessageId, incoming.Id);
        var existing = exact.Keeper;
        if (existing is null)
        {
            incoming.Id = FirstFilled(incoming.Id, providerId, Guid.NewGuid().ToString("N"));
            incoming.ProviderMessageId = providerId;
            _data.WhatsAppMessages.Insert(0, incoming);
            return (true, IsWhatsAppIncoming(incoming), IsWhatsAppOutgoing(incoming) &&
                NormalizeWhatsAppDeliveryStatus(incoming.Status) == "erro");
        }

        var changed = exact.Changed;
        changed |= SetIfFilled(existing.ClientRequestId, incoming.ClientRequestId, value => existing.ClientRequestId = value);
        changed |= SetIfFilled(existing.ProviderMessageId, incoming.ProviderMessageId, value => existing.ProviderMessageId = value);
        changed |= SetIfFilled(existing.Provider, incoming.Provider, value => existing.Provider = value);
        changed |= SetIfFilled(existing.Instance, incoming.Instance, value => existing.Instance = value);
        changed |= SetIfFilled(existing.ConversationId, incoming.ConversationId, value => existing.ConversationId = value);
        changed |= SetIfFilled(existing.LeadId, incoming.LeadId, value => existing.LeadId = value);
        changed |= SetIfFilled(existing.CustomerName, incoming.CustomerName, value => existing.CustomerName = value);
        changed |= SetIfFilled(existing.Phone, incoming.Phone, value => existing.Phone = value);
        changed |= SetIfFilled(existing.Message, incoming.Message, value => existing.Message = value);
        changed |= SetIfFilled(existing.Direction, incoming.Direction, value => existing.Direction = value);
        changed |= SetIfFilled(existing.Type, incoming.Type, value => existing.Type = value);
        changed |= SetIfFilled(existing.Kind, incoming.Kind, value => existing.Kind = value);
        var currentStatus = NormalizeWhatsAppDeliveryStatus(existing.Status, IsWhatsAppIncoming(existing));
        var incomingStatus = NormalizeWhatsAppDeliveryStatus(incoming.Status, IsWhatsAppIncoming(incoming));
        var failedOutgoing = false;
        if (WhatsAppManualSendPolicy.CanTransitionDeliveryStatus(currentStatus, incomingStatus))
        {
            failedOutgoing = IsWhatsAppOutgoing(existing) &&
                currentStatus != "erro" &&
                incomingStatus == "erro";
            existing.Status = incomingStatus;
            changed = true;
        }
        changed |= SetIfFilled(existing.Category, incoming.Category, value => existing.Category = value);
        if (incoming.SentAt.HasValue && existing.SentAt != incoming.SentAt)
        {
            existing.SentAt = incoming.SentAt;
            changed = true;
        }

        if (incoming.ReceivedAt.HasValue && existing.ReceivedAt != incoming.ReceivedAt)
        {
            existing.ReceivedAt = incoming.ReceivedAt;
            changed = true;
        }

        return (changed, false, failedOutgoing);
    }

    private bool MergeWhatsAppDuplicateLocalState(
        WhatsAppMessage keeper,
        WhatsAppMessage duplicate)
    {
        var changed = false;
        changed |= FillWhatsAppMessageIfBlank(keeper.Provider, duplicate.Provider, value => keeper.Provider = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Instance, duplicate.Instance, value => keeper.Instance = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.ConversationId, duplicate.ConversationId, value => keeper.ConversationId = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.LeadId, duplicate.LeadId, value => keeper.LeadId = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.CustomerName, duplicate.CustomerName, value => keeper.CustomerName = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Phone, duplicate.Phone, value => keeper.Phone = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Message, duplicate.Message, value => keeper.Message = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Direction, duplicate.Direction, value => keeper.Direction = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Type, duplicate.Type, value => keeper.Type = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Kind, duplicate.Kind, value => keeper.Kind = value);
        changed |= FillWhatsAppMessageIfBlank(keeper.Category, duplicate.Category, value => keeper.Category = value);

        var currentStatus = NormalizeWhatsAppDeliveryStatus(keeper.Status, IsWhatsAppIncoming(keeper));
        var duplicateStatus = NormalizeWhatsAppDeliveryStatus(duplicate.Status, IsWhatsAppIncoming(duplicate));
        if (WhatsAppManualSendPolicy.CanTransitionDeliveryStatus(currentStatus, duplicateStatus))
        {
            keeper.Status = duplicateStatus;
            changed = true;
        }

        if (!keeper.SentAt.HasValue && duplicate.SentAt.HasValue)
        {
            keeper.SentAt = duplicate.SentAt;
            changed = true;
        }

        if (!keeper.ReceivedAt.HasValue && duplicate.ReceivedAt.HasValue)
        {
            keeper.ReceivedAt = duplicate.ReceivedAt;
            changed = true;
        }

        if (!keeper.ReadAt.HasValue && duplicate.ReadAt.HasValue)
        {
            keeper.ReadAt = duplicate.ReadAt;
            changed = true;
        }

        return changed;
    }

    private static bool FillWhatsAppMessageIfBlank(
        string target,
        string value,
        Action<string> setter)
    {
        if (!string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private bool UpsertWhatsAppRealtimeLead(WhatsAppLead incoming)
    {
        var phone = NormalizeBrazilPhone(incoming.Phone);
        var existing = _data.WhatsAppLeads.FirstOrDefault(item =>
            (!string.IsNullOrWhiteSpace(incoming.Id) && string.Equals(item.Id, incoming.Id, StringComparison.OrdinalIgnoreCase)) ||
            (string.IsNullOrWhiteSpace(incoming.Id) &&
             !string.IsNullOrWhiteSpace(phone) &&
             string.Equals(NormalizeBrazilPhone(item.Phone), phone, StringComparison.OrdinalIgnoreCase)));
        if (existing is null)
        {
            incoming.Id = string.IsNullOrWhiteSpace(incoming.Id) ? Guid.NewGuid().ToString("N") : incoming.Id;
            incoming.Phone = phone;
            incoming.Facts ??= [];
            _data.WhatsAppLeads.Add(incoming);
            return true;
        }

        var changed = false;
        changed |= SetIfFilled(existing.Instance, incoming.Instance, value => existing.Instance = value);
        changed |= SetIfFilled(existing.ConversationId, incoming.ConversationId, value => existing.ConversationId = value);
        changed |= SetIfFilled(existing.CustomerName, incoming.CustomerName, value => existing.CustomerName = value);
        changed |= SetIfFilled(existing.Phone, phone, value => existing.Phone = value);
        changed |= SetIfFilled(existing.Stage, incoming.Stage, value => existing.Stage = value);
        changed |= SetIfFilled(existing.Summary, incoming.Summary, value => existing.Summary = value);
        changed |= SetIfFilled(existing.Intent, incoming.Intent, value => existing.Intent = value);
        changed |= SetIfFilled(existing.RequestedService, incoming.RequestedService, value => existing.RequestedService = value);
        changed |= SetIfFilled(existing.PreferredSchedule, incoming.PreferredSchedule, value => existing.PreferredSchedule = value);
        changed |= SetIfFilled(existing.AssignedProfessional, incoming.AssignedProfessional, value => existing.AssignedProfessional = value);
        changed |= SetIfFilled(existing.PreferredDate, incoming.PreferredDate, value => existing.PreferredDate = value);
        changed |= SetIfFilled(existing.Period, incoming.Period, value => existing.Period = value);
        changed |= SetIfFilled(existing.Notes, incoming.Notes, value => existing.Notes = value);
        if (existing.Score != incoming.Score)
        {
            existing.Score = incoming.Score;
            changed = true;
        }

        changed |= SetIfDifferent(existing.Unread, incoming.Unread, value => existing.Unread = value);
        changed |= SetIfDifferent(existing.UnreadCount, incoming.UnreadCount, value => existing.UnreadCount = value);
        changed |= SetIfDifferent(existing.FollowupCount, incoming.FollowupCount, value => existing.FollowupCount = value);
        changed |= SetIfDifferent(existing.NextFollowupAt, incoming.NextFollowupAt, value => existing.NextFollowupAt = value);
        changed |= SetIfDifferent(existing.LastInboundAt, incoming.LastInboundAt, value => existing.LastInboundAt = value);
        changed |= SetIfDifferent(existing.LastOutboundAt, incoming.LastOutboundAt, value => existing.LastOutboundAt = value);
        changed |= SetIfDifferent(existing.OptedOutAt, incoming.OptedOutAt, value => existing.OptedOutAt = value);
        changed |= SetIfDifferent(existing.HandedOffAt, incoming.HandedOffAt, value => existing.HandedOffAt = value);
        changed |= SetIfDifferent(existing.LastMessageAt, incoming.LastMessageAt, value => existing.LastMessageAt = value);
        changed |= SetIfDifferent(existing.UpdatedAt, incoming.UpdatedAt, value => existing.UpdatedAt = value);
        if (incoming.Facts.Count > 0 && !existing.Facts.SequenceEqual(incoming.Facts, StringComparer.OrdinalIgnoreCase))
        {
            existing.Facts = incoming.Facts.ToList();
            changed = true;
        }

        return changed;
    }

    private static bool SetIfFilled(string target, string value, Action<string> setter)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(target, value, StringComparison.Ordinal))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private static bool SetIfDifferent<T>(T target, T value, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(target, value))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private void MarkWhatsAppConversationRead(string phone)
    {
        if (MarkWhatsAppConversationReadCore(phone))
        {
            _store.Save(_data);
        }

        foreach (var leadId in _data.WhatsAppLeads
                     .Where(item => string.Equals(NormalizeBrazilPhone(item.Phone), NormalizeBrazilPhone(phone), StringComparison.OrdinalIgnoreCase))
                     .Select(item => item.Id)
                     .Where(item => !string.IsNullOrWhiteSpace(item))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _ = PatchWhatsAppLeadReadAsync(leadId);
        }
    }

    private bool MarkWhatsAppConversationReadCore(string phone)
    {
        phone = NormalizeBrazilPhone(phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var changed = false;
        var now = DateTime.Now;
        foreach (var message in _data.WhatsAppMessages.Where(item =>
                     IsWhatsAppIncoming(item) &&
                     item.ReadAt is null &&
                     string.Equals(NormalizeBrazilPhone(item.Phone), phone, StringComparison.OrdinalIgnoreCase)))
        {
            message.ReadAt = now;
            changed = true;
        }

        foreach (var lead in _data.WhatsAppLeads.Where(item =>
                     string.Equals(NormalizeBrazilPhone(item.Phone), phone, StringComparison.OrdinalIgnoreCase)))
        {
            if (lead.Unread || lead.UnreadCount != 0)
            {
                lead.Unread = false;
                lead.UnreadCount = 0;
                changed = true;
            }
        }

        return changed;
    }

    private WhatsAppLead? ActiveWhatsAppLead() =>
        _data.WhatsAppLeads
            .Where(item => string.Equals(
                NormalizeBrazilPhone(item.Phone),
                NormalizeBrazilPhone(_selectedWhatsAppReplyPhone),
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefault();

    private void RefreshWhatsAppLeadCard(bool showingConversation)
    {
        var lead = showingConversation ? ActiveWhatsAppLead() : null;
        WhatsAppLeadCard.Visibility = showingConversation ? Visibility.Visible : Visibility.Collapsed;
        WhatsAppOutcomeWonButton.IsEnabled = lead is not null;
        WhatsAppOutcomeLaterButton.IsEnabled = lead is not null;
        WhatsAppOutcomeLostButton.IsEnabled = lead is not null;
        if (lead is null)
        {
            UpdateWhatsAppOutcomeButtonAppearance(WhatsAppOutcomeWonButton, selected: false);
            UpdateWhatsAppOutcomeButtonAppearance(WhatsAppOutcomeLaterButton, selected: false);
            UpdateWhatsAppOutcomeButtonAppearance(WhatsAppOutcomeLostButton, selected: false);
            return;
        }

        WhatsAppLeadNameText.Text = FirstFilled(lead.CustomerName, FormatPhone(lead.Phone), "Lead do WhatsApp");
        WhatsAppLeadStageText.Text = WhatsAppLeadStageLabel(lead.Stage);
        WhatsAppLeadScoreText.Text = $"{Math.Clamp(lead.Score, 0, 100)} pts";
        WhatsAppLeadSummaryText.Text = FirstFilled(
            lead.Summary,
            lead.Notes,
            lead.Intent.Length > 0 ? $"Interesse: {lead.Intent}" : "",
            "Novo contato pelo WhatsApp.");

        var facts = new List<string>();
        facts.AddRange(lead.Facts.Where(item => !string.IsNullOrWhiteSpace(item)));
        if (!string.IsNullOrWhiteSpace(lead.RequestedService)) facts.Add($"Serviço: {lead.RequestedService}");
        if (!string.IsNullOrWhiteSpace(lead.PreferredDate)) facts.Add($"Data: {lead.PreferredDate}");
        if (!string.IsNullOrWhiteSpace(lead.Period)) facts.Add($"Período: {lead.Period}");
        if (!string.IsNullOrWhiteSpace(lead.AssignedProfessional)) facts.Add($"Profissional: {lead.AssignedProfessional}");
        WhatsAppLeadFactsText.Text = facts.Count == 0
            ? "Aguardando qualificação."
            : string.Join("  •  ", facts.Distinct(StringComparer.OrdinalIgnoreCase).Take(6));

        var stage = lead.Stage.Trim().ToLowerInvariant();
        UpdateWhatsAppOutcomeButtonAppearance(WhatsAppOutcomeWonButton, stage == "won");
        UpdateWhatsAppOutcomeButtonAppearance(WhatsAppOutcomeLaterButton, stage == "qualified");
        UpdateWhatsAppOutcomeButtonAppearance(WhatsAppOutcomeLostButton, stage == "lost");
    }

    private static void UpdateWhatsAppOutcomeButtonAppearance(Button button, bool selected)
    {
        var foreground = AccentTextBrush;
        button.Background = selected ? AccentSoftBrush : PanelBrush;
        button.BorderBrush = selected ? AccentBrush : LineBrush;
        button.Foreground = foreground;
        TextElement.SetForeground(button, foreground);
    }

    private static string WhatsAppLeadStageLabel(string stage) => stage.Trim().ToLowerInvariant() switch
    {
        "qualifying" => "Qualificando",
        "qualified" => "Qualificado",
        "hot" => "Lead quente",
        "handoff" => "Atendimento humano",
        "won" => "Ganho",
        "lost" => "Perdido",
        "opted_out" => "Sem contato",
        _ => "Novo"
    };

    private async void UpdateWhatsAppLeadStageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string stage } || ActiveWhatsAppLead() is not { } lead)
        {
            return;
        }

        stage = stage.Trim().ToLowerInvariant();
        if (stage is not ("qualified" or "won" or "lost"))
        {
            return;
        }

        var previous = lead.Stage;
        lead.Stage = stage;
        lead.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshWhatsAppLeadCard(showingConversation: true);
        if (await PatchWhatsAppLeadStageAsync(lead.Id, stage))
        {
            ShowStatus($"Lead marcado como {WhatsAppLeadStageLabel(stage).ToLowerInvariant()}.");
            return;
        }

        lead.Stage = previous;
        _store.Save(_data);
        RefreshWhatsAppLeadCard(showingConversation: true);
        ShowStatus("Não consegui atualizar o lead no robô local. A alteração foi desfeita.");
    }

    private async Task<bool> PatchWhatsAppLeadStageAsync(string leadId, string stage)
    {
        var token = await ReadWhatsAppLocalApiTokenAsync(CancellationToken.None);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(leadId))
        {
            return false;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var request = CreateWhatsAppLocalApiRequest(
            HttpMethod.Patch,
            $"/api/agenda/leads/{Uri.EscapeDataString(leadId)}?{WhatsAppRealtimeInstanceQuery()}",
            token,
            new { stage });
        try
        {
            using var response = await _whatsAppRealtimeClient.SendAsync(request, timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed))
            {
                return false;
            }

            using var aliasRequest = CreateWhatsAppLocalApiRequest(
                HttpMethod.Post,
                $"/api/agenda/leads/{Uri.EscapeDataString(leadId)}/stage?{WhatsAppRealtimeInstanceQuery()}",
                token,
                new { stage });
            using var aliasResponse = await _whatsAppRealtimeClient.SendAsync(aliasRequest, timeout.Token);
            return aliasResponse.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or ObjectDisposedException)
        {
            Debug.WriteLine($"Agenda lead stage update failed: {ex.Message}");
            return false;
        }
    }

    private async Task PatchWhatsAppLeadReadAsync(string leadId)
    {
        var token = await ReadWhatsAppLocalApiTokenAsync(CancellationToken.None);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        using var request = CreateWhatsAppLocalApiRequest(
            HttpMethod.Patch,
            $"/api/agenda/leads/{Uri.EscapeDataString(leadId)}?{WhatsAppRealtimeInstanceQuery()}",
            token,
            new { unread = false });
        try
        {
            using var response = await _whatsAppRealtimeClient.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Agenda lead read update returned HTTP {(int)response.StatusCode}.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or ObjectDisposedException)
        {
            Debug.WriteLine($"Agenda lead read update failed: {ex.Message}");
        }
    }

    private static WhatsAppMessage? ParseWhatsAppRealtimeMessage(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var phone = NormalizeBrazilPhone(ReadRealtimeString(element, "phone", "customerPhone"));
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var direction = NormalizeRealtimeDirection(ReadRealtimeString(element, "direction"));
        var type = FirstFilled(ReadRealtimeString(element, "type"), "text");
        var text = ReadRealtimeString(element, "text", "message", "body");
        if (string.IsNullOrWhiteSpace(text))
        {
            text = RealtimeMediaLabel(type, direction);
        }

        var occurredAt = ReadRealtimeDate(element, "occurredAt", "createdAt", "timestamp") ?? DateTime.Now;
        var clientRequestId = ReadRealtimeString(element, "clientRequestId", "requestId", "idempotencyKey");
        var providerId = ReadRealtimeString(element, "providerMessageId", "messageId", "id");
        var id = ReadRealtimeString(element, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            var key = $"{providerId}|{phone}|{direction}|{occurredAt:O}|{text}";
            id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
        }

        return new WhatsAppMessage
        {
            Id = id,
            ClientRequestId = clientRequestId,
            ProviderMessageId = providerId,
            Provider = ReadRealtimeString(element, "provider"),
            Instance = ReadRealtimeString(element, "instance"),
            ConversationId = FirstFilled(ReadRealtimeString(element, "conversationId"), phone),
            LeadId = ReadRealtimeString(element, "leadId"),
            CustomerName = FirstFilled(ReadRealtimeString(element, "customerName", "name"), FormatPhone(phone)),
            Phone = phone,
            Message = text,
            Direction = direction,
            Type = type,
            Kind = ReadRealtimeString(element, "kind"),
            Status = NormalizeWhatsAppDeliveryStatus(
                ReadRealtimeString(element, "status", "deliveryStatus", "messageStatus"),
                direction == "entrada"),
            Category = "Atendimento",
            CreatedAt = occurredAt,
            SentAt = direction == "saida" ? occurredAt : null,
            ReceivedAt = ReadRealtimeDate(element, "receivedAt") ?? (direction == "entrada" ? occurredAt : null),
            ReadAt = ReadRealtimeDate(element, "readAt")
        };
    }

    private static WhatsAppLead? ParseWhatsAppRealtimeLead(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = ReadRealtimeString(element, "id", "leadId");
        var phone = NormalizeBrazilPhone(ReadRealtimeString(element, "phone", "customerPhone"));
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var preferredDate = ReadRealtimeString(element, "preferredDate");
        var period = ReadRealtimeString(element, "period", "preferredPeriod");
        var notes = ReadRealtimeString(element, "notes");
        var facts = ReadRealtimeFacts(element);
        var service = ReadRealtimeString(element, "service", "requestedService");
        var professional = ReadRealtimeString(element, "professional", "assignedProfessional");
        return new WhatsAppLead
        {
            Id = id,
            Instance = ReadRealtimeString(element, "instance"),
            ConversationId = FirstFilled(ReadRealtimeString(element, "conversationId"), phone),
            CustomerName = FirstFilled(ReadRealtimeString(element, "customerName", "name"), FormatPhone(phone)),
            Phone = phone,
            Stage = FirstFilled(ReadRealtimeString(element, "stage", "status"), "new"),
            Score = ReadRealtimeInt32(element, "score"),
            Summary = FirstFilled(ReadRealtimeString(element, "summary"), notes),
            Facts = facts,
            Intent = ReadRealtimeString(element, "intent"),
            RequestedService = service,
            PreferredDate = preferredDate,
            Period = period,
            PreferredSchedule = string.Join(" ", new[] { preferredDate, period }.Where(value => !string.IsNullOrWhiteSpace(value))),
            AssignedProfessional = professional,
            Unread = ReadRealtimeBool(element, "unread"),
            UnreadCount = ReadRealtimeInt32(element, "unreadCount"),
            FollowupCount = ReadRealtimeInt32(element, "followupCount"),
            NextFollowupAt = ReadRealtimeDate(element, "nextFollowupAt"),
            LastInboundAt = ReadRealtimeDate(element, "lastInboundAt", "lastIncomingAt"),
            LastOutboundAt = ReadRealtimeDate(element, "lastOutboundAt", "lastOutgoingAt"),
            CreatedAt = ReadRealtimeDate(element, "createdAt") ?? DateTime.Now,
            UpdatedAt = ReadRealtimeDate(element, "updatedAt") ?? DateTime.Now,
            LastMessageAt = ReadRealtimeDate(element, "lastMessageAt", "lastInboundAt", "lastIncomingAt", "lastOutboundAt", "lastOutgoingAt"),
            OptedOutAt = ReadRealtimeDate(element, "optedOutAt"),
            HandedOffAt = ReadRealtimeDate(element, "handedOffAt"),
            Notes = notes
        };
    }

    private static string NormalizeRealtimeDirection(string direction) => direction.Trim().ToLowerInvariant() switch
    {
        "entrada" or "incoming" or "inbound" or "received" or "in" => "entrada",
        _ => "saida"
    };

    private static string RealtimeMediaLabel(string type, string direction)
    {
        var label = type.Trim().ToLowerInvariant() switch
        {
            "audio" or "ptt" => "Áudio",
            "image" => "Imagem",
            "video" => "Vídeo",
            "document" => "Documento",
            "location" => "Localização",
            "contact" => "Contato",
            _ => "Mensagem"
        };
        return direction == "entrada" ? $"{label} recebido(a)." : $"{label} enviado(a).";
    }

    private static IReadOnlyList<JsonElement> ReadRealtimeArray(JsonElement element, string name)
    {
        return TryGetRealtimeProperty(element, name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.Clone()).ToList()
            : [];
    }

    private static bool TryGetRealtimeProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string ReadRealtimeString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetRealtimeProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString()?.Trim() ?? "";
            }

            if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString();
            }
        }

        return "";
    }

    private static int ReadRealtimeInt32(JsonElement element, string name)
    {
        if (!TryGetRealtimeProperty(element, name, out var value))
        {
            return 0;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static long ReadRealtimeInt64(JsonElement element, string name)
    {
        if (!TryGetRealtimeProperty(element, name, out var value))
        {
            return 0;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)
            ? number
            : long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static bool ReadRealtimeBool(JsonElement element, string name)
    {
        if (!TryGetRealtimeProperty(element, name, out var value))
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True ||
               (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed);
    }

    private static DateTime? ReadRealtimeDate(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetRealtimeProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unix))
            {
                try
                {
                    return (unix > 9_999_999_999
                        ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                        : DateTimeOffset.FromUnixTimeSeconds(unix)).LocalDateTime;
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }
            }

            if (value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed.LocalDateTime;
            }
        }

        return null;
    }

    private static List<string> ReadRealtimeFacts(JsonElement element)
    {
        if (!TryGetRealtimeProperty(element, "facts", out var facts))
        {
            return [];
        }

        if (facts.ValueKind == JsonValueKind.Array)
        {
            return facts.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.ToString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Take(12)
                .ToList();
        }

        if (facts.ValueKind == JsonValueKind.Object)
        {
            return facts.EnumerateObject()
                .Select(property => $"{property.Name}: {property.Value}")
                .Take(12)
                .ToList();
        }

        var single = facts.ValueKind == JsonValueKind.String ? facts.GetString() : facts.ToString();
        return string.IsNullOrWhiteSpace(single) ? [] : [single!];
    }
}
