using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private async Task LinkWhatsAppGatewayAsync(string storePhone)
    {
        try
        {
            var result = await ActivateWhatsAppGatewayAsync(storePhone);
            if (result.Ok && IsWhatsAppEvolutionConnected(result.State))
            {
                ApplyWhatsAppConnectedState(result, storePhone);
                ShowStatus($"WhatsApp linkado: {FormatPhone(storePhone)}.");
                return;
            }

            _data.Settings.WhatsAppLinked = false;
            _data.Settings.WhatsAppEvolutionState = result.Pending ? "qrcode" : "erro";
            _data.Settings.WhatsAppEvolutionQrBase64 = result.QrBase64;
            _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
            _store.Save(_data);
            RefreshWhatsAppSurface();

            if (result.Pending && !string.IsNullOrWhiteSpace(result.OnboardingUrl))
            {
                if (string.IsNullOrWhiteSpace(result.QrBase64))
                {
                    OpenTrustedWhatsAppOnboardingUrl(result.OnboardingUrl);
                    ShowStatus("A conexão do WhatsApp foi aberta. Escaneie o QR e depois clique em Verificar conexão.");
                }
                else
                {
                    ShowStatus("QR do WhatsApp gerado. Escaneie pelo celular da loja.");
                }

                return;
            }

            ShowStatus($"WhatsApp não linkou: {result.Message}");
        }
        finally
        {
            SetWhatsAppButtonsEnabled(true);
        }
    }

    private async Task<WhatsAppEvolutionResult> ActivateWhatsAppGatewayAsync(string storePhone)
    {
        var payload = CreateWhatsAppGatewayPayload(storePhone);
        var response = await PostWhatsAppGatewayAsync("/activate", payload, TimeSpan.FromSeconds(25));
        var result = ParseWhatsAppGatewayResult(response.StatusCode, response.Body);
        if (!result.Ok && !result.Pending)
        {
            // A sessão de onboarding pode já existir mesmo quando a Evolution falha
            // temporariamente ao recriar a instância. Nesse caso, preserve o fluxo
            // recuperável retornado por /status em vez de esconder o QR do usuário.
            var current = await FetchWhatsAppGatewayStatusAsync();
            if (current.Pending && !string.IsNullOrWhiteSpace(current.OnboardingUrl))
            {
                result = current;
            }
        }

        if (!result.Pending || string.IsNullOrWhiteSpace(result.OnboardingUrl))
        {
            return result;
        }

        var qr = await TryDownloadWhatsAppGatewayQrAsync(result.OnboardingUrl);
        return new WhatsAppEvolutionResult
        {
            Ok = result.Ok,
            Pending = result.Pending,
            Message = result.Message,
            State = result.State,
            QrBase64 = qr,
            OnboardingUrl = result.OnboardingUrl,
            ConnectedName = result.ConnectedName,
            ConnectedPhone = result.ConnectedPhone
        };
    }

    private async Task<WhatsAppEvolutionResult> FetchWhatsAppGatewayStatusAsync()
    {
        try
        {
            var phone = NormalizeBrazilPhone(FirstFilled(
                _data.Settings.WhatsAppStorePhone,
                _data.Settings.BusinessPhone,
                _data.Settings.AccountPhone));
            var response = await PostWhatsAppGatewayAsync(
                "/status",
                CreateWhatsAppGatewayPayload(phone),
                TimeSpan.FromSeconds(12));
            return ParseWhatsAppGatewayResult(response.StatusCode, response.Body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao consultar WhatsApp: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> DisconnectWhatsAppGatewayAsync()
    {
        try
        {
            var response = await PostWhatsAppGatewayAsync(
                "/disconnect",
                CreateWhatsAppGatewayPayload(_data.Settings.WhatsAppStorePhone),
                TimeSpan.FromSeconds(20));
            return ParseWhatsAppGatewayResult(response.StatusCode, response.Body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao deslinkar WhatsApp: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> SendWhatsAppGatewayTextAsync(
        string phone,
        string text,
        string attemptId)
    {
        try
        {
            var payload = CreateWhatsAppGatewayPayload(_data.Settings.WhatsAppStorePhone);
            payload["customerPhone"] = NormalizeBrazilPhone(phone);
            payload["message"] = text;
            payload["messageId"] = attemptId;
            var response = await PostWhatsAppGatewayAsync("/send", payload, TimeSpan.FromSeconds(20));
            return ParseWhatsAppGatewayResult(response.StatusCode, response.Body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return WhatsAppEvolutionResult.Fail(
                $"Falha ao enviar mensagem: {ex.Message}",
                deliveryUncertain: true);
        }
        catch (InvalidOperationException ex)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao enviar mensagem: {ex.Message}");
        }
    }

    private async Task<List<WhatsAppEvolutionIncomingMessage>> FetchWhatsAppGatewayMessagesAsync()
    {
        try
        {
            var payload = CreateWhatsAppGatewayPayload(_data.Settings.WhatsAppStorePhone);
            payload["limit"] = 100;
            var response = await PostWhatsAppGatewayAsync("/messages", payload, TimeSpan.FromSeconds(15));
            if ((int)response.StatusCode is < 200 or >= 300 || string.IsNullOrWhiteSpace(response.Body))
            {
                Debug.WriteLine($"WhatsApp gateway messages failed: {(int)response.StatusCode} {ReadEvolutionMessage(response.Body)}");
                return [];
            }

            return ParseWhatsAppEvolutionMessages(response.Body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"WhatsApp gateway messages failed: {ex.Message}");
            return [];
        }
    }

    private Dictionary<string, object?> CreateWhatsAppGatewayPayload(string storePhone)
    {
        return new Dictionary<string, object?>
        {
            ["licenseKey"] = BuildAgendaLivreWhatsAppLicense(),
            ["machineHash"] = GetAgendaMachineFingerprint(),
            ["machineCode"] = GetAgendaMachineCode(),
            ["appVersion"] = GetAppVersion(),
            ["localPlan"] = "Agenda Livre",
            ["localExpiresAt"] = new DateTime(2035, 12, 31, 23, 59, 0, DateTimeKind.Utc).ToString("O"),
            ["storePhone"] = NormalizeBrazilPhone(storePhone),
            ["profile"] = new Dictionary<string, object?>
            {
                ["businessName"] = BusinessDisplayName(),
                ["ownerName"] = _data.Settings.AccountFullName,
                ["email"] = _data.Settings.AccountEmail,
                ["phone"] = FirstFilled(_data.Settings.BusinessPhone, _data.Settings.AccountPhone),
                ["cnpj"] = OnlyDigits(_data.Settings.BusinessDocument),
                ["city"] = _data.Settings.Neighborhood,
                ["state"] = ""
            }
        };
    }

    private async Task<(HttpStatusCode StatusCode, string Body)> PostWhatsAppGatewayAsync(
        string route,
        object payload,
        TimeSpan timeout)
    {
        var uri = BuildWhatsAppGatewayUri(route)
                  ?? throw new InvalidOperationException("Endereço seguro do WhatsApp inválido.");
        using var client = new HttpClient { Timeout = timeout };
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private Uri? BuildWhatsAppGatewayUri(string route)
    {
        var baseUrl = NormalizeWhatsAppEvolutionBaseUrl(_data.Settings.WhatsAppEvolutionBaseUrl).TrimEnd('/');
        return Uri.TryCreate($"{baseUrl}/{route.TrimStart('/')}", UriKind.Absolute, out var uri) ? uri : null;
    }

    private static WhatsAppEvolutionResult ParseWhatsAppGatewayResult(HttpStatusCode statusCode, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = document.RootElement;
            var ok = ReadGatewayBool(root, "ok");
            var pending = ReadGatewayBool(root, "pending");
            var responseStatus = ReadEvolutionString(root, "status", "deliveryStatus", "messageStatus");
            var hasAccepted = TryGetJsonProperty(root, "accepted", out _);
            var accepted = hasAccepted && ReadGatewayBool(root, "accepted");
            var existingPending = hasAccepted &&
                WhatsAppManualSendPolicy.IsExistingPending(accepted, responseStatus);
            var message = ReadEvolutionString(root, "message", "error", "detail");
            var storePhone = NormalizeBrazilPhone(ReadEvolutionString(root, "storePhone", "phone", "displayPhone"));
            var state = ok && !pending ? "open" : pending ? "qrcode" : "erro";
            return new WhatsAppEvolutionResult
            {
                Ok = ok || existingPending,
                Pending = pending || existingPending,
                ExistingPending = existingPending,
                DeliveryStatus = existingPending ? "pendente" : NormalizeWhatsAppDeliveryStatus(responseStatus),
                DeliveryUncertain = !ok && !existingPending && IsAmbiguousWhatsAppHttpStatus(statusCode),
                Message = string.IsNullOrWhiteSpace(message)
                    ? ((int)statusCode is >= 200 and < 300 ? "WhatsApp respondeu sem detalhes." : $"WhatsApp respondeu HTTP {(int)statusCode}.")
                    : message,
                State = state,
                OnboardingUrl = ReadEvolutionString(root, "onboardingUrl"),
                ConnectedPhone = storePhone,
                ProviderMessageId = ReadEvolutionString(root, "providerMessageId", "remoteMessageId")
            };
        }
        catch (JsonException)
        {
            return WhatsAppEvolutionResult.Fail(ReadEvolutionMessage(body));
        }
    }

    private static bool ReadGatewayBool(JsonElement root, string name)
    {
        return TryGetJsonProperty(root, name, out var value) && value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static bool IsAmbiguousWhatsAppHttpStatus(HttpStatusCode statusCode) =>
        WhatsAppManualSendPolicy.IsAmbiguousHttpStatus((int)statusCode);

    private static async Task<string> TryDownloadWhatsAppGatewayQrAsync(string onboardingUrl)
    {
        if (!IsTrustedWhatsAppOnboardingUrl(onboardingUrl))
        {
            return "";
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(onboardingUrl);
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (response.IsSuccessStatusCode && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (bytes.Length is > 0 and < 2_000_000)
                    {
                        return $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";
                    }
                }
            }
            catch (HttpRequestException)
            {
                return "";
            }

            await Task.Delay(700);
        }

        return "";
    }

    private static void OpenTrustedWhatsAppOnboardingUrl(string onboardingUrl)
    {
        if (IsTrustedWhatsAppOnboardingUrl(onboardingUrl))
        {
            Process.Start(new ProcessStartInfo(onboardingUrl) { UseShellExecute = true });
        }
    }

    private static bool IsTrustedWhatsAppOnboardingUrl(string onboardingUrl)
    {
        if (!Uri.TryCreate(onboardingUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var regularFunctionUrl =
            uri.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.Contains(
                "/functions/v1/whatsapp/onboarding/",
                StringComparison.OrdinalIgnoreCase);
        var edgeRuntimeUrl =
            uri.Host.Equals("edge-runtime.supabase.com", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.StartsWith(
                "/whatsapp/onboarding/",
                StringComparison.OrdinalIgnoreCase);

        return regularFunctionUrl || edgeRuntimeUrl;
    }
}
