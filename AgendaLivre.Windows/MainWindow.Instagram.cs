using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const string DefaultInstagramApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/instagram";
    private bool _instagramConnectionPolling;

    private void RefreshInstagramSurface()
    {
        if (SettingsInstagramStatusText is null || SettingsInstagramDetailText is null || SettingsInstagramConnectButton is null)
        {
            return;
        }

        var settings = _data.Settings;
        var username = NormalizeInstagramUsername(settings.InstagramUsername);
        SettingsInstagramStatusText.Text = settings.InstagramLinked
            ? string.IsNullOrWhiteSpace(username) ? "Instagram conectado" : $"Conectado: @{username}"
            : string.Equals(settings.InstagramState, "aguardando_oauth", StringComparison.OrdinalIgnoreCase)
                ? "Aguardando autorização"
                : "Não conectado";
        SettingsInstagramStatusText.Foreground = settings.InstagramLinked ? Solid("#C13584") : MutedBrush;
        SettingsInstagramDetailText.Text = settings.InstagramLinked
            ? "Direct e respostas estão disponíveis no Agenda Livre."
            : string.IsNullOrWhiteSpace(settings.InstagramLastError)
                ? "Conecte uma conta profissional Business ou Creator."
                : settings.InstagramLastError;
        SettingsInstagramConnectButton.Content = settings.InstagramLinked ? "Desconectar" : "Conectar";
    }

    private async void ConnectInstagramButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsInstagramConnectButton.IsEnabled = false;
        try
        {
            if (_data.Settings.InstagramLinked)
            {
                var disconnected = await DisconnectInstagramAsync();
                if (!disconnected.Ok)
                {
                    ShowStatus($"Instagram não desconectou: {disconnected.Message}");
                    return;
                }

                _data.Settings.InstagramLinked = false;
                _data.Settings.InstagramUsername = "";
                _data.Settings.InstagramDisplayName = "";
                _data.Settings.InstagramAccountId = "";
                _data.Settings.InstagramState = "desconectado";
                _data.Settings.InstagramLastError = "";
                _data.Settings.InstagramLastCheckedAt = DateTime.Now;
                _store.Save(_data);
                RefreshInstagramSurface();
                ShowStatus("Instagram desconectado do Agenda Livre.");
                return;
            }

            var current = await FetchInstagramStatusAsync();
            if (current.Connected)
            {
                ApplyInstagramConnectedState(current);
                ShowStatus($"Instagram conectado: @{_data.Settings.InstagramUsername}.");
                return;
            }

            var start = await StartInstagramOAuthAsync();
            if (!start.Ok || !IsTrustedInstagramAuthorizationUrl(start.AuthorizationUrl))
            {
                _data.Settings.InstagramLastError = start.Message;
                _data.Settings.InstagramState = "erro";
                _store.Save(_data);
                RefreshInstagramSurface();
                ShowStatus($"Instagram não iniciou: {start.Message}");
                return;
            }

            _data.Settings.InstagramState = "aguardando_oauth";
            _data.Settings.InstagramLastError = "";
            _data.Settings.InstagramLastCheckedAt = DateTime.Now;
            _store.Save(_data);
            RefreshInstagramSurface();
            Process.Start(new ProcessStartInfo(start.AuthorizationUrl) { UseShellExecute = true });
            ShowStatus("Autorize a conta profissional do Instagram na janela aberta.");
            _ = PollInstagramConnectionAfterOAuthAsync();
        }
        finally
        {
            SettingsInstagramConnectButton.IsEnabled = true;
        }
    }

    private async void OpenInstagramPanelButton_Click(object sender, RoutedEventArgs e)
    {
        var status = await FetchInstagramStatusAsync();
        if (!status.Connected)
        {
            _data.Settings.InstagramLinked = false;
            _data.Settings.InstagramLastError = status.Message;
            _data.Settings.InstagramLastCheckedAt = DateTime.Now;
            _store.Save(_data);
            RefreshInstagramSurface();
            ConnectInstagramButton_Click(sender, e);
            return;
        }

        ApplyInstagramConnectedState(status);
        await ShowInstagramInboxAsync();
    }

    private void OpenMarketingInstagramButton_Click(object sender, RoutedEventArgs e)
    {
        var caption = BuildMarketingMessage(_data.Customers.FirstOrDefault()?.Name ?? "Cliente");
        Clipboard.SetText(caption);
        var username = NormalizeInstagramUsername(_data.Settings.InstagramUsername);
        var url = string.IsNullOrWhiteSpace(username)
            ? "https://www.instagram.com/"
            : $"https://www.instagram.com/{Uri.EscapeDataString(username)}/";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        ShowStatus("Texto da campanha copiado. O Instagram foi aberto para você publicar.");
    }

    private async Task ShowInstagramInboxAsync()
    {
        var rows = new ObservableCollection<InstagramMessageRow>(await FetchInstagramMessagesAsync());
        var shell = CreateEditorDialog(
            "Instagram Direct",
            "Mensagens iniciadas pelos clientes no Instagram profissional.",
            "Responder");
        shell.Dialog.Width = 760;
        shell.Dialog.MaxHeight = 780;

        var account = string.IsNullOrWhiteSpace(_data.Settings.InstagramUsername)
            ? "Conta profissional conectada"
            : $"@{_data.Settings.InstagramUsername}";
        shell.Body.Children.Add(new TextBlock
        {
            Text = account,
            Foreground = Solid("#C13584"),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var list = new ListBox
        {
            ItemsSource = rows,
            DisplayMemberPath = nameof(InstagramMessageRow.Display),
            Height = 300,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Foreground = InkBrush,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 12)
        };
        list.SelectedItem = rows.LastOrDefault(row => row.Direction == "entrada");
        shell.Body.Children.Add(list);

        var reply = AddDialogTextField(shell.Body, "Resposta", "", "Digite uma resposta para a conversa selecionada.", multiline: true);
        shell.PrimaryButton.Click += async (_, _) =>
        {
            shell.ErrorText.Visibility = Visibility.Collapsed;
            if (list.SelectedItem is not InstagramMessageRow selected || string.IsNullOrWhiteSpace(selected.InstagramScopedId))
            {
                shell.ErrorText.Text = "Selecione uma mensagem recebida do cliente.";
                shell.ErrorText.Visibility = Visibility.Visible;
                return;
            }

            var text = reply.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                shell.ErrorText.Text = "Digite a resposta antes de enviar.";
                shell.ErrorText.Visibility = Visibility.Visible;
                return;
            }

            shell.PrimaryButton.IsEnabled = false;
            try
            {
                var sent = await SendInstagramMessageAsync(selected.InstagramScopedId, text);
                if (!sent.Ok)
                {
                    shell.ErrorText.Text = sent.Message;
                    shell.ErrorText.Visibility = Visibility.Visible;
                    return;
                }

                rows.Add(new InstagramMessageRow(
                    sent.RemoteMessageId,
                    selected.InstagramScopedId,
                    FirstFilled(_data.Settings.InstagramDisplayName, _data.Settings.InstagramUsername, "Você"),
                    _data.Settings.InstagramUsername,
                    text,
                    "saida",
                    DateTime.Now,
                    "enviado"));
                reply.Clear();
                list.ScrollIntoView(rows[^1]);
                ShowStatus("Resposta enviada pelo Instagram.");
            }
            finally
            {
                shell.PrimaryButton.IsEnabled = true;
            }
        };

        ShowAppDialog(shell.Dialog);
    }

    private async Task PollInstagramConnectionAfterOAuthAsync()
    {
        if (_instagramConnectionPolling)
        {
            return;
        }

        _instagramConnectionPolling = true;
        try
        {
            for (var attempt = 0; attempt < 24; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                var status = await FetchInstagramStatusAsync();
                if (!status.Connected)
                {
                    continue;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    ApplyInstagramConnectedState(status);
                    ShowStatus($"Instagram conectado: @{_data.Settings.InstagramUsername}.");
                });
                return;
            }
        }
        finally
        {
            _instagramConnectionPolling = false;
        }
    }

    private void ApplyInstagramConnectedState(InstagramStatusResult status)
    {
        _data.Settings.InstagramEnabled = true;
        _data.Settings.InstagramLinked = true;
        _data.Settings.InstagramUsername = NormalizeInstagramUsername(status.Username);
        _data.Settings.InstagramDisplayName = status.DisplayName;
        _data.Settings.InstagramAccountId = status.InstagramUserId;
        _data.Settings.InstagramState = FirstFilled(status.Status, "connected");
        _data.Settings.InstagramLastError = "";
        _data.Settings.InstagramLinkedAt ??= DateTime.Now;
        _data.Settings.InstagramLastCheckedAt = DateTime.Now;
        _store.Save(_data);
        RefreshInstagramSurface();
    }

    private async Task<InstagramStatusResult> FetchInstagramStatusAsync()
    {
        try
        {
            var response = await PostInstagramAsync("/status", CreateInstagramPayload(), TimeSpan.FromSeconds(12));
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
            var root = document.RootElement;
            return new InstagramStatusResult(
                ReadInstagramBool(root, "ok"),
                ReadInstagramBool(root, "connected"),
                ReadEvolutionString(root, "username"),
                ReadEvolutionString(root, "displayName", "name"),
                ReadEvolutionString(root, "instagramUserId", "accountId", "id"),
                ReadEvolutionString(root, "status", "state"),
                ReadEvolutionString(root, "message", "error", "detail"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new InstagramStatusResult(false, false, "", "", "", "erro", $"Instagram indisponível: {ex.Message}");
        }
    }

    private async Task<InstagramOAuthResult> StartInstagramOAuthAsync()
    {
        try
        {
            var response = await PostInstagramAsync("/oauth/start", CreateInstagramPayload(), TimeSpan.FromSeconds(15));
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
            var root = document.RootElement;
            return new InstagramOAuthResult(
                ReadInstagramBool(root, "ok"),
                ReadEvolutionString(root, "authorizationUrl", "url"),
                ReadEvolutionString(root, "message", "error", "detail"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new InstagramOAuthResult(false, "", $"Instagram indisponível: {ex.Message}");
        }
    }

    private async Task<InstagramActionResult> DisconnectInstagramAsync()
    {
        try
        {
            var response = await PostInstagramAsync("/disconnect", CreateInstagramPayload(), TimeSpan.FromSeconds(15));
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
            var root = document.RootElement;
            return new InstagramActionResult(
                ReadInstagramBool(root, "ok"),
                ReadEvolutionString(root, "message", "error", "detail"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new InstagramActionResult(false, $"Instagram indisponível: {ex.Message}");
        }
    }

    private async Task<List<InstagramMessageRow>> FetchInstagramMessagesAsync()
    {
        try
        {
            var response = await PostInstagramAsync("/messages", CreateInstagramPayload(), TimeSpan.FromSeconds(15));
            if ((int)response.StatusCode is < 200 or >= 300)
            {
                return [];
            }

            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
            if (!TryGetJsonProperty(document.RootElement, "messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var rows = new List<InstagramMessageRow>();
            foreach (var item in messages.EnumerateArray())
            {
                var direction = FirstFilled(ReadEvolutionString(item, "direction", "directionType"), "entrada").ToLowerInvariant();
                rows.Add(new InstagramMessageRow(
                    ReadEvolutionString(item, "id", "messageId", "mid"),
                    ReadEvolutionString(item, "instagramScopedId", "senderId", "recipientId"),
                    ReadEvolutionString(item, "senderName", "name"),
                    NormalizeInstagramUsername(ReadEvolutionString(item, "senderUsername", "username")),
                    ReadEvolutionString(item, "text", "message", "body"),
                    direction,
                    ReadInstagramDate(item),
                    ReadEvolutionString(item, "status")));
            }

            return rows.OrderBy(row => row.CreatedAt).TakeLast(150).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"Instagram messages failed: {ex.Message}");
            return [];
        }
    }

    private async Task<InstagramPublicationsResult> FetchInstagramPublicationsAsync()
    {
        try
        {
            var payload = CreateInstagramPayload();
            payload["limit"] = 30;
            var response = await PostInstagramAsync("/publications", payload, TimeSpan.FromSeconds(20));
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
            var root = document.RootElement;
            var message = ReadEvolutionString(root, "message", "error", "detail");
            var connected = response.StatusCode != (HttpStatusCode)428 &&
                            (!TryGetJsonProperty(root, "connected", out var connectedElement) ||
                             connectedElement.ValueKind != JsonValueKind.False);
            if ((int)response.StatusCode is < 200 or >= 300)
            {
                return new InstagramPublicationsResult(false, connected, message, []);
            }

            if (!TryGetJsonProperty(root, "publications", out var publications) ||
                publications.ValueKind != JsonValueKind.Array)
            {
                return new InstagramPublicationsResult(true, true, "", []);
            }

            var rows = new List<InstagramPublicationRecord>();
            foreach (var item in publications.EnumerateArray())
            {
                rows.Add(new InstagramPublicationRecord(
                    ReadEvolutionString(item, "id", "mediaId"),
                    ReadEvolutionString(item, "caption", "text"),
                    ReadEvolutionString(item, "mediaType", "type"),
                    ReadEvolutionString(item, "mediaUrl", "imageUrl"),
                    ReadEvolutionString(item, "thumbnailUrl", "mediaUrl", "imageUrl"),
                    ReadEvolutionString(item, "permalink", "url"),
                    ReadEvolutionString(item, "username"),
                    ReadInstagramDate(item),
                    ReadInstagramInt(item, "likeCount", "likes"),
                    ReadInstagramInt(item, "commentsCount", "comments")));
            }

            return new InstagramPublicationsResult(
                true,
                true,
                "",
                rows.OrderByDescending(row => row.PublishedAt).ToList());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"Instagram publications failed: {ex.Message}");
            return new InstagramPublicationsResult(false, true, $"Instagram indisponível: {ex.Message}", []);
        }
    }

    private async Task<InstagramSendResult> SendInstagramMessageAsync(string recipientId, string text)
    {
        try
        {
            var payload = CreateInstagramPayload();
            payload["recipientId"] = recipientId;
            payload["text"] = text;
            payload["messageId"] = Guid.NewGuid().ToString("N");
            var response = await PostInstagramAsync("/messages/send", payload, TimeSpan.FromSeconds(18));
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(response.Body) ? "{}" : response.Body);
            var root = document.RootElement;
            return new InstagramSendResult(
                ReadInstagramBool(root, "ok"),
                ReadEvolutionString(root, "message", "error", "detail"),
                ReadEvolutionString(root, "remoteMessageId", "messageId", "id"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new InstagramSendResult(false, $"Instagram indisponível: {ex.Message}", "");
        }
    }

    private Dictionary<string, object?> CreateInstagramPayload()
    {
        return new Dictionary<string, object?>
        {
            ["licenseKey"] = BuildAgendaLivreWhatsAppLicense(),
            ["machineHash"] = GetAgendaMachineFingerprint(),
            ["machineCode"] = GetAgendaMachineCode(),
            ["appVersion"] = GetAppVersion(),
            ["profile"] = new Dictionary<string, object?>
            {
                ["businessName"] = BusinessDisplayName(),
                ["ownerName"] = _data.Settings.AccountFullName,
                ["email"] = _data.Settings.AccountEmail
            }
        };
    }

    private async Task<(HttpStatusCode StatusCode, string Body)> PostInstagramAsync(string route, object payload, TimeSpan timeout)
    {
        var uri = BuildInstagramApiUri(route) ?? throw new InvalidOperationException("Endereço do Instagram inválido.");
        using var client = new HttpClient { Timeout = timeout };
        using var response = await client.PostAsync(
            uri,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private Uri? BuildInstagramApiUri(string route)
    {
        var configured = string.IsNullOrWhiteSpace(_data.Settings.InstagramApiUrl)
            ? DefaultInstagramApiUrl
            : _data.Settings.InstagramApiUrl.Trim();
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps
            || !baseUri.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase))
        {
            configured = DefaultInstagramApiUrl;
        }

        return Uri.TryCreate($"{configured.TrimEnd('/')}/{route.TrimStart('/')}", UriKind.Absolute, out var uri) ? uri : null;
    }

    private static bool ReadInstagramBool(JsonElement root, string name)
    {
        return TryGetJsonProperty(root, name, out var value) && value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static DateTime ReadInstagramDate(JsonElement item)
    {
        var raw = ReadEvolutionString(item, "createdAt", "timestamp", "when");
        if (DateTimeOffset.TryParse(raw, out var parsed))
        {
            return parsed.LocalDateTime;
        }

        if (long.TryParse(raw, out var unix))
        {
            return unix > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix).LocalDateTime
                : DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
        }

        return DateTime.Now;
    }

    private static int ReadInstagramInt(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetJsonProperty(item, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return Math.Max(0, number);
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), out number))
            {
                return Math.Max(0, number);
            }
        }

        return 0;
    }

    private static string NormalizeInstagramUsername(string value) =>
        (value ?? "").Trim().TrimStart('@').Replace(" ", "", StringComparison.Ordinal);

    private static bool IsTrustedInstagramAuthorizationUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && (uri.Host.Equals("www.instagram.com", StringComparison.OrdinalIgnoreCase)
                   || uri.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase)
                   || uri.Host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record InstagramStatusResult(
        bool Ok,
        bool Connected,
        string Username,
        string DisplayName,
        string InstagramUserId,
        string Status,
        string Message);

    private sealed record InstagramOAuthResult(bool Ok, string AuthorizationUrl, string Message);
    private sealed record InstagramActionResult(bool Ok, string Message);
    private sealed record InstagramSendResult(bool Ok, string Message, string RemoteMessageId);
    private sealed record InstagramPublicationsResult(
        bool Ok,
        bool Connected,
        string Message,
        IReadOnlyList<InstagramPublicationRecord> Publications);

    private sealed record InstagramPublicationRecord(
        string Id,
        string Caption,
        string MediaType,
        string MediaUrl,
        string ThumbnailUrl,
        string Permalink,
        string Username,
        DateTime PublishedAt,
        int LikeCount,
        int CommentsCount);

    private sealed record InstagramMessageRow(
        string Id,
        string InstagramScopedId,
        string SenderName,
        string SenderUsername,
        string Text,
        string Direction,
        DateTime CreatedAt,
        string Status)
    {
        public string Display
        {
            get
            {
                var sender = !string.IsNullOrWhiteSpace(SenderName)
                    ? SenderName
                    : !string.IsNullOrWhiteSpace(SenderUsername) ? $"@{SenderUsername}" : "Cliente";
                var arrow = Direction == "saida" ? "Você →" : $"{sender} →";
                return $"{CreatedAt:dd/MM HH:mm}  {arrow}  {Text}";
            }
        }
    }
}
