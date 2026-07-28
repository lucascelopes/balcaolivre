using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const string DefaultPublicBookingApiUrl = "https://minhaagendalivre.com.br";
    private const string LegacyPublicBookingApiUrl = "https://agenda-livre-next.edodoy.chatgpt.site";
    private const string PublicBookingRootDomain = "minhaagendalivre.com.br";
    private static readonly TimeSpan OnlineBookingSyncInterval = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan OnlineBookingReminderMinimum = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan OnlineBookingReminderMaximum = TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(15));

    private readonly HttpClient _onlineBookingClient = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer _onlineBookingSyncTimer = new() { Interval = OnlineBookingSyncInterval };
    private readonly DispatcherTimer _onlineBookingDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    private CancellationTokenSource? _onlineBookingCancellation;
    private bool _onlineBookingStarted;
    private bool _onlineBookingSyncRunning;
    private bool _onlineBookingSyncRequested;
    private string? _onlineBookingLastSyncedLogoFingerprint;
    private string? _onlineBookingCachedLogoFingerprint;
    private string _onlineBookingCachedLogoDataUrl = "";

    private void StartOnlineBookingSync()
    {
        if (_onlineBookingStarted)
        {
            return;
        }

        _onlineBookingStarted = true;
        _onlineBookingCancellation?.Dispose();
        _onlineBookingCancellation = new CancellationTokenSource();
        _onlineBookingSyncTimer.Tick += OnlineBookingSyncTimer_Tick;
        _onlineBookingDebounceTimer.Tick += OnlineBookingDebounceTimer_Tick;
        _onlineBookingSyncTimer.Start();
        ScheduleOnlineBookingSync();
    }

    private async void OnlineBookingSyncTimer_Tick(object? sender, EventArgs e)
    {
        await RunOnlineBookingSyncAsync();
    }

    private async void OnlineBookingDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _onlineBookingDebounceTimer.Stop();
        await RunOnlineBookingSyncAsync();
    }

    private void ScheduleOnlineBookingSync()
    {
        if (!_onlineBookingStarted || _onlineBookingCancellation is null)
        {
            return;
        }

        _onlineBookingDebounceTimer.Stop();
        _onlineBookingDebounceTimer.Start();
    }

    private void MainWindow_OnlineBookingClosed(object? sender, EventArgs e)
    {
        _onlineBookingStarted = false;
        _onlineBookingSyncTimer.Stop();
        _onlineBookingDebounceTimer.Stop();
        _onlineBookingCancellation?.Cancel();
        _onlineBookingCancellation?.Dispose();
        _onlineBookingCancellation = null;
        _onlineBookingClient.Dispose();
    }

    private async Task RunOnlineBookingSyncAsync()
    {
        if (!_onlineBookingStarted || _onlineBookingCancellation is null)
        {
            return;
        }

        if (_onlineBookingSyncRunning)
        {
            _onlineBookingSyncRequested = true;
            return;
        }

        _onlineBookingSyncRunning = true;
        try
        {
            do
            {
                _onlineBookingSyncRequested = false;
                await SyncOnlineBookingOnceAsync(_onlineBookingCancellation.Token);
            }
            while (_onlineBookingSyncRequested && !_onlineBookingCancellation.IsCancellationRequested);
        }
        catch (OperationCanceledException) when (_onlineBookingCancellation?.IsCancellationRequested != false)
        {
            // The window is closing.
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine($"Online booking sync timed out: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or ObjectDisposedException)
        {
            Debug.WriteLine($"Online booking sync skipped: {ex.Message}");
        }
        finally
        {
            _onlineBookingSyncRunning = false;
        }
    }

    private async Task SyncOnlineBookingOnceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var publishedCatalog = MarketingCatalogPublicationForSync();
        var desiredSlug = publishedCatalog?.Slug.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(desiredSlug))
        {
            desiredSlug = _data.Settings.PublicBookingSlug.Trim();
        }
        if (string.IsNullOrWhiteSpace(desiredSlug))
        {
            desiredSlug = SlugifyPublicBookingStore(BusinessDisplayName());
        }

        var theme = ThemeById(_data.Settings.ThemeId);
        var logoFingerprint = PublicBookingLogoFingerprint(_data.Settings.BusinessLogoPath);
        var shouldSyncLogo = !string.Equals(
            logoFingerprint,
            _onlineBookingLastSyncedLogoFingerprint,
            StringComparison.Ordinal);
        var themePayload = new Dictionary<string, object?>
        {
            ["id"] = theme.Id,
            ["name"] = theme.Name,
            ["fontFamily"] = theme.FontFamily,
            ["appBackground"] = theme.AppBackground,
            ["panel"] = theme.Panel,
            ["accent"] = theme.Accent,
            ["accentDark"] = theme.AccentDark,
            ["accentSoft"] = theme.AccentSoft,
            ["onAccent"] = OnAccentBrush is SolidColorBrush b ? ColorHex(b.Color) : "#FFFFFF",
            ["line"] = theme.Line,
            ["ink"] = theme.Ink,
            ["muted"] = theme.Muted
        };
        if (shouldSyncLogo)
        {
            if (!string.Equals(
                    logoFingerprint,
                    _onlineBookingCachedLogoFingerprint,
                    StringComparison.Ordinal))
            {
                _onlineBookingCachedLogoFingerprint = logoFingerprint;
                _onlineBookingCachedLogoDataUrl = BuildPublicBookingLogoDataUrl(_data.Settings.BusinessLogoPath);
            }

            themePayload["logoUrl"] = _onlineBookingCachedLogoDataUrl;
        }

        Dictionary<string, object?>? catalogPayload = null;
        var catalogHeroFingerprint = "";
        var shouldSyncCatalogHero = false;
        if (publishedCatalog is not null)
        {
            catalogHeroFingerprint = MarketingCatalogHeroFingerprint(publishedCatalog.HeroImagePath);
            shouldSyncCatalogHero = !string.Equals(
                catalogHeroFingerprint,
                _onlineBookingLastSyncedCatalogHeroFingerprint,
                StringComparison.Ordinal);
            catalogPayload = new Dictionary<string, object?>
            {
                ["title"] = publishedCatalog.Title,
                ["supportText"] = publishedCatalog.SupportText,
                ["buttonText"] = publishedCatalog.ButtonText,
                ["accentColor"] = publishedCatalog.AccentColor,
                ["alignment"] = publishedCatalog.Alignment,
                ["spacing"] = publishedCatalog.Spacing,
                ["titleFont"] = publishedCatalog.TitleFont,
                ["imageContrast"] = publishedCatalog.ImageContrast,
                ["showButton"] = publishedCatalog.ShowButton,
                ["header"] = new Dictionary<string, object?>
                {
                    ["businessName"] = publishedCatalog.Header.BusinessName,
                    ["subtitle"] = publishedCatalog.Header.Subtitle,
                    ["buttonText"] = publishedCatalog.Header.ButtonText,
                    ["showLogo"] = publishedCatalog.Header.ShowLogo,
                    ["showNavigation"] = publishedCatalog.Header.ShowNavigation,
                    ["showButton"] = publishedCatalog.Header.ShowButton,
                    ["sticky"] = publishedCatalog.Header.Sticky,
                    ["background"] = publishedCatalog.Header.Background
                },
                ["footer"] = new Dictionary<string, object?>
                {
                    ["businessName"] = publishedCatalog.Footer.BusinessName,
                    ["description"] = publishedCatalog.Footer.Description,
                    ["address"] = publishedCatalog.Footer.Address,
                    ["phone"] = publishedCatalog.Footer.Phone,
                    ["hours"] = publishedCatalog.Footer.Hours,
                    ["instagram"] = publishedCatalog.Footer.Instagram,
                    ["whatsApp"] = publishedCatalog.Footer.WhatsApp,
                    ["showContact"] = publishedCatalog.Footer.ShowContact,
                    ["showHours"] = publishedCatalog.Footer.ShowHours,
                    ["showSocial"] = publishedCatalog.Footer.ShowSocial
                },
                ["design"] = new Dictionary<string, object?>
                {
                    ["colorScheme"] = publishedCatalog.Design.ColorScheme,
                    ["buttonStyle"] = publishedCatalog.Design.ButtonStyle,
                    ["cornerStyle"] = publishedCatalog.Design.CornerStyle,
                    ["contentWidth"] = publishedCatalog.Design.ContentWidth
                },
                ["sections"] = BuildMarketingCatalogSectionsPayload(publishedCatalog.Sections),
                ["mediaUploads"] = BuildMarketingCatalogMediaUploads(publishedCatalog.Sections),
                ["seo"] = new Dictionary<string, object?>
                {
                    ["title"] = publishedCatalog.SeoTitle,
                    ["description"] = publishedCatalog.SeoDescription
                },
                ["promotion"] = publishedCatalog.Promotion is { IsPublished: true } promotion
                    ? new Dictionary<string, object?>
                    {
                        ["name"] = promotion.Name,
                        ["startDate"] = promotion.StartDate.ToString("O", CultureInfo.InvariantCulture),
                        ["endDate"] = promotion.EndDate.ToString("O", CultureInfo.InvariantCulture),
                        ["limitPerCustomer"] = promotion.LimitPerCustomer,
                        ["highlightInCatalog"] = promotion.HighlightInCatalog,
                        ["isPublished"] = promotion.IsPublished,
                        ["publishedAt"] = promotion.PublishedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "",
                        ["items"] = promotion.Items.Select(item => new Dictionary<string, object?>
                        {
                            ["serviceId"] = item.ServiceId,
                            ["serviceName"] = item.ServiceName,
                            ["originalPrice"] = item.OriginalPrice,
                            ["promotionalPrice"] = item.PromotionalPrice
                        }).ToList()
                    }
                    : null,
                ["publishedAt"] = publishedCatalog.PublishedAt?.ToString("O", CultureInfo.InvariantCulture) ?? ""
            };
            if (shouldSyncCatalogHero)
            {
                if (!string.Equals(
                        catalogHeroFingerprint,
                        _onlineBookingCachedCatalogHeroFingerprint,
                        StringComparison.Ordinal))
                {
                    _onlineBookingCachedCatalogHeroFingerprint = catalogHeroFingerprint;
                    _onlineBookingCachedCatalogHeroDataUrl = BuildMarketingCatalogHeroDataUrl(publishedCatalog.HeroImagePath);
                }
                if (!string.IsNullOrWhiteSpace(_onlineBookingCachedCatalogHeroDataUrl))
                {
                    catalogPayload["heroImageDataUrl"] = _onlineBookingCachedCatalogHeroDataUrl;
                }
            }
        }

        var payload = new
        {
            instance = WhatsAppRealtimeInstanceName(),
            desiredSlug,
            storeName = BusinessDisplayName(),
            segment = _data.Settings.BusinessSegment,
            generatedAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
            theme = themePayload,
            catalog = catalogPayload,
            customDomain = publishedCatalog?.CustomDomain,
            bookingServices = BuildWhatsAppBookingServicesSnapshot(DateTime.Today)
        };

        var accountAccessToken = _syncCoordinator is null
            ? ""
            : await _syncCoordinator.GetAccountAccessTokenAsync(cancellationToken);
        using var request = CreateOnlineBookingRequest(
            HttpMethod.Post,
            "/api/internal/agenda/sync",
            payload,
            accountAccessToken);
        using var response = await _onlineBookingClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"Online booking sync returned HTTP {(int)response.StatusCode}: {CompactOnlineBookingError(body)}");
            if (publishedCatalog is not null && MarketingSiteSavedStatusText is not null)
            {
                MarketingSiteSavedStatusText.Text = "Publicação pendente. Tentaremos novamente.";
            }
            return;
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (TryGetRealtimeProperty(root, "ok", out var okValue) &&
            okValue.ValueKind is JsonValueKind.True or JsonValueKind.False &&
            !okValue.GetBoolean())
        {
            Debug.WriteLine($"Online booking sync was rejected: {CompactOnlineBookingError(body)}");
            if (publishedCatalog is not null && MarketingSiteSavedStatusText is not null)
            {
                MarketingSiteSavedStatusText.Text = "Publicação pendente. Tentaremos novamente.";
            }
            return;
        }

        if (publishedCatalog is not null &&
            MarketingSiteSavedStatusText is not null &&
            TryGetRealtimeProperty(root, "publication", out var publicationElement) &&
            publicationElement.ValueKind == JsonValueKind.Object &&
            string.Equals(
                ReadRealtimeString(publicationElement, "status"),
                "published",
                StringComparison.OrdinalIgnoreCase))
        {
            var cloudPublishedAtText = ReadRealtimeString(publicationElement, "publishedAt");
            MarketingSiteSavedStatusText.Text = DateTimeOffset.TryParse(
                cloudPublishedAtText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var cloudPublishedAt)
                ? $"Publicado na nuvem em {cloudPublishedAt.ToLocalTime():dd/MM, HH:mm}"
                : "Publicado na nuvem";
        }

        if (shouldSyncLogo)
        {
            _onlineBookingLastSyncedLogoFingerprint = logoFingerprint;
        }
        if (shouldSyncCatalogHero && !string.IsNullOrWhiteSpace(_onlineBookingCachedCatalogHeroDataUrl))
        {
            _onlineBookingLastSyncedCatalogHeroFingerprint = catalogHeroFingerprint;
        }

        var returnedSlug = ReadRealtimeString(root, "slug");
        var publicUrl = ReadRealtimeString(root, "publicUrl", "url");
        if (!string.IsNullOrWhiteSpace(returnedSlug))
        {
            _data.Settings.PublicBookingSlug = returnedSlug.Trim();
        }

        if (string.IsNullOrWhiteSpace(publicUrl) && !string.IsNullOrWhiteSpace(_data.Settings.PublicBookingSlug))
        {
            publicUrl = BuildPublicBookingUrl(_data.Settings.PublicBookingSlug);
        }

        if (!string.IsNullOrWhiteSpace(publicUrl))
        {
            _data.Settings.PublicBookingUrl = publicUrl.Trim();
        }

        if (TryGetRealtimeProperty(root, "customDomain", out var customDomainElement) &&
            customDomainElement.ValueKind == JsonValueKind.Object)
        {
            _data.Settings.PublicBookingCustomDomain = ReadRealtimeString(customDomainElement, "hostname");
            _data.Settings.PublicBookingCustomDomainStatus = ReadRealtimeString(customDomainElement, "status");
            _data.Settings.PublicBookingCustomDomainProviderStatus = ReadRealtimeString(customDomainElement, "providerStatus");
            _data.Settings.PublicBookingCustomDomainSslStatus = ReadRealtimeString(customDomainElement, "sslStatus");
            _data.Settings.PublicBookingCustomDomainCnameTarget = ReadRealtimeString(customDomainElement, "cnameTarget");
            _data.Settings.PublicBookingCustomDomainValidationRecordName = "";
            _data.Settings.PublicBookingCustomDomainValidationRecordType = "";
            _data.Settings.PublicBookingCustomDomainValidationRecordValue = "";
            if (TryGetRealtimeProperty(customDomainElement, "validationRecords", out var validationRecordsElement) &&
                validationRecordsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var validationRecordElement in validationRecordsElement.EnumerateArray())
                {
                    if (validationRecordElement.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var validationRecordName = ReadRealtimeString(validationRecordElement, "name");
                    var validationRecordValue = ReadRealtimeString(validationRecordElement, "value");
                    if (string.IsNullOrWhiteSpace(validationRecordName) || string.IsNullOrWhiteSpace(validationRecordValue))
                    {
                        continue;
                    }

                    _data.Settings.PublicBookingCustomDomainValidationRecordName = validationRecordName;
                    _data.Settings.PublicBookingCustomDomainValidationRecordType = ReadRealtimeString(validationRecordElement, "recordType");
                    _data.Settings.PublicBookingCustomDomainValidationRecordValue = validationRecordValue;
                    break;
                }
            }
            _data.Settings.PublicBookingCustomDomainLastError = ReadRealtimeString(customDomainElement, "lastError");
            UpdateMarketingSiteCustomDomainStatus();
        }
        else if (customDomainElement.ValueKind == JsonValueKind.Null)
        {
            _data.Settings.PublicBookingCustomDomain = "";
            _data.Settings.PublicBookingCustomDomainStatus = "";
            _data.Settings.PublicBookingCustomDomainProviderStatus = "";
            _data.Settings.PublicBookingCustomDomainSslStatus = "";
            _data.Settings.PublicBookingCustomDomainCnameTarget = "";
            _data.Settings.PublicBookingCustomDomainValidationRecordName = "";
            _data.Settings.PublicBookingCustomDomainValidationRecordType = "";
            _data.Settings.PublicBookingCustomDomainValidationRecordValue = "";
            _data.Settings.PublicBookingCustomDomainLastError = "";
            UpdateMarketingSiteCustomDomainStatus();
        }

        _data.Settings.PublicBookingApiUrl = NormalizePublicBookingApiUrl(_data.Settings.PublicBookingApiUrl);
        _data.Settings.PublicBookingLastSyncAt = DateTime.Now;
        _store.Save(_data);

        if (!TryGetRealtimeProperty(root, "bookings", out var bookingsElement) ||
            bookingsElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var bookings = bookingsElement
            .EnumerateArray()
            .Select(ParseWhatsAppBookingRequest)
            .Where(item => item is not null)
            .Cast<WhatsAppBookingRequest>()
            .Select(item => item with { Source = "agenda-online" })
            .ToList();

        foreach (var booking in bookings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(booking.Instance, WhatsAppRealtimeInstanceName(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(booking.Status, "pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(booking.Status, "requested", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessOnlineBookingRequestAsync(booking, cancellationToken);
                continue;
            }

            if (string.Equals(booking.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessOnlineBookingNotificationsAsync(booking, cancellationToken);
            }
        }
    }

    private async Task ProcessOnlineBookingRequestAsync(
        WhatsAppBookingRequest booking,
        CancellationToken cancellationToken)
    {
        var resolution = await Dispatcher.InvokeAsync(() => CommitWhatsAppBooking(booking));
        var patched = await PatchOnlineBookingAsync(
            booking.Id,
            new
            {
                status = resolution.Status,
                appointmentId = resolution.AppointmentId,
                message = resolution.Message
            },
            cancellationToken);

        if (patched && string.Equals(resolution.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessOnlineBookingNotificationsAsync(
                booking with { Status = "confirmed" },
                cancellationToken);
        }
    }

    private async Task ProcessOnlineBookingNotificationsAsync(
        WhatsAppBookingRequest booking,
        CancellationToken cancellationToken)
    {
        if (booking.Start is null || booking.Start.Value <= DateTime.Now || string.IsNullOrWhiteSpace(booking.Phone))
        {
            return;
        }

        if (booking.ConfirmationSentAt is null)
        {
            var confirmation = BuildOnlineBookingConfirmationMessage(booking);
            var result = await SendWhatsAppEvolutionTextAsync(
                booking.Phone,
                confirmation,
                $"agenda-online-confirmation-{booking.Id}",
                booking.CustomerName);
            if (!result.Ok)
            {
                Debug.WriteLine($"Online booking confirmation was not accepted for {booking.Id}: {result.Message}");
                return;
            }

            var sentAt = DateTimeOffset.UtcNow;
            var patched = await PatchOnlineBookingAsync(
                booking.Id,
                new { confirmationSentAt = sentAt.ToString("O", CultureInfo.InvariantCulture) },
                cancellationToken);
            if (patched)
            {
                booking = booking with { ConfirmationSentAt = sentAt.LocalDateTime };
            }
        }

        var untilStart = booking.Start.Value - DateTime.Now;
        if (booking.ReminderSentAt is not null ||
            untilStart < OnlineBookingReminderMinimum ||
            untilStart > OnlineBookingReminderMaximum)
        {
            return;
        }

        var reminder = BuildOnlineBookingReminderMessage(booking);
        var reminderResult = await SendWhatsAppEvolutionTextAsync(
            booking.Phone,
            reminder,
            $"agenda-online-reminder-{booking.Id}",
            booking.CustomerName);
        if (!reminderResult.Ok)
        {
            Debug.WriteLine($"Online booking reminder was not accepted for {booking.Id}: {reminderResult.Message}");
            return;
        }

        await PatchOnlineBookingAsync(
            booking.Id,
            new { reminderSentAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
            cancellationToken);
    }

    private string BuildOnlineBookingConfirmationMessage(WhatsAppBookingRequest booking)
    {
        var firstName = FirstName(booking.CustomerName);
        var date = booking.Start!.Value.ToString("dd/MM", Brazil);
        var time = booking.Start.Value.ToString("HH:mm", Brazil);
        return $"Olá, {firstName}! Seu agendamento de {booking.ServiceName} na {BusinessDisplayName()} foi confirmado para {date} às {time}. Se precisar, responda esta mensagem.";
    }

    private string BuildOnlineBookingReminderMessage(WhatsAppBookingRequest booking)
    {
        var firstName = FirstName(booking.CustomerName);
        var date = booking.Start!.Value.ToString("dd/MM", Brazil);
        var time = booking.Start.Value.ToString("HH:mm", Brazil);
        return $"Olá, {firstName}! Lembrete da {BusinessDisplayName()}: seu atendimento de {booking.ServiceName} será em poucas horas, no dia {date} às {time}. Até breve!";
    }

    private async Task<bool> PatchOnlineBookingAsync(
        string bookingId,
        object payload,
        CancellationToken cancellationToken)
    {
        var accountAccessToken = _syncCoordinator is null
            ? ""
            : await _syncCoordinator.GetAccountAccessTokenAsync(cancellationToken);
        using var request = CreateOnlineBookingRequest(
            HttpMethod.Patch,
            $"/api/internal/agenda/bookings/{Uri.EscapeDataString(bookingId)}",
            payload,
            accountAccessToken);
        using var response = await _onlineBookingClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Debug.WriteLine($"Online booking update returned HTTP {(int)response.StatusCode}: {CompactOnlineBookingError(body)}");
        return false;
    }

    private HttpRequestMessage CreateOnlineBookingRequest(
        HttpMethod method,
        string path,
        object payload,
        string accountAccessToken)
    {
        var request = new HttpRequestMessage(method, PublicBookingEndpoint(path));
        if (!string.IsNullOrWhiteSpace(accountAccessToken))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accountAccessToken.Trim()}");
        }
        request.Headers.TryAddWithoutValidation("x-agenda-license", BuildAgendaLivreWhatsAppLicense());
        request.Headers.TryAddWithoutValidation("x-agenda-machine", GetAgendaMachineFingerprint());
        request.Headers.TryAddWithoutValidation("x-agenda-machine-code", GetAgendaMachineCode());
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        return request;
    }

    private Uri PublicBookingEndpoint(string path)
    {
        var baseUrl = NormalizePublicBookingApiUrl(_data.Settings.PublicBookingApiUrl);
        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }

    private static string NormalizePublicBookingApiUrl(string? value)
    {
        var configuredValue = value?.Trim().TrimEnd('/');
        var candidate = string.IsNullOrWhiteSpace(configuredValue) ||
                        string.Equals(configuredValue, LegacyPublicBookingApiUrl, StringComparison.OrdinalIgnoreCase)
            ? DefaultPublicBookingApiUrl
            : configuredValue;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return DefaultPublicBookingApiUrl;
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private string BuildPublicBookingUrl(string slug)
    {
        var baseUri = new Uri(NormalizePublicBookingApiUrl(_data.Settings.PublicBookingApiUrl));
        if (baseUri.IsLoopback || string.Equals(baseUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return $"{baseUri.GetLeftPart(UriPartial.Authority)}/agendar/{Uri.EscapeDataString(slug)}";
        }

        return $"https://{slug}.{PublicBookingRootDomain}";
    }

    private static string SlugifyPublicBookingStore(string value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "minha-agenda" : value.Trim();
        var decomposed = source.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSeparator = false;
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(lower);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = builder.Length > 0;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "minha-agenda";
        }

        if (slug.Length > 48)
        {
            slug = slug[..48].TrimEnd('-');
        }

        if (slug.Length < 3)
        {
            slug = $"agenda-{slug}";
        }

        if (slug is "www" or "app" or "admin" or "pdv" or "cardapio" or "api")
        {
            slug = $"{slug}-agenda";
        }

        return slug;
    }

    private static string CompactOnlineBookingError(string body)
    {
        var compact = (body ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
        return compact.Length <= 240 ? compact : compact[..240];
    }
}
