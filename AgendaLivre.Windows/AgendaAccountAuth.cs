using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgendaLivre.Windows;

public sealed record AgendaAuthConfig(Uri SupabaseUrl, string PublishableKey, Uri SyncUrl);

public sealed record AgendaAuthSession(
    string UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string FullName,
    string BusinessName);

public sealed record AgendaSignUpResult(
    AgendaAuthSession? Session,
    bool ConfirmationRequired,
    string Message);

public sealed class AgendaAuthException : Exception
{
    public AgendaAuthException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

public sealed class AgendaSessionStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AgendaLivre.Windows/AuthSession/v1");
    private static readonly byte[] ConfigEntropy = Encoding.UTF8.GetBytes("AgendaLivre.Windows/AuthConfig/v1");
    private static readonly byte[] OnboardingEntropy = Encoding.UTF8.GetBytes("AgendaLivre.Windows/InitialOnboarding/v1");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly string _configPath;
    private readonly string _initialOnboardingPath;

    public AgendaSessionStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgendaLivre.Windows");
        _path = Path.Combine(root, "auth-session.bin");
        _configPath = Path.Combine(root, "auth-config.bin");
        _initialOnboardingPath = Path.Combine(root, "initial-onboarding.bin");
    }

    public AgendaAuthSession? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(_path);
            var clearBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<AgendaAuthSession>(clearBytes, JsonOptions);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            Clear();
            return null;
        }
    }

    public void Save(AgendaAuthSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var clearBytes = JsonSerializer.SerializeToUtf8Bytes(session, JsonOptions);
        var protectedBytes = ProtectedData.Protect(clearBytes, Entropy, DataProtectionScope.CurrentUser);
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, protectedBytes);
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public void Clear() => TryDelete(_path);

    public AgendaAuthConfig? LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            return null;
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(_configPath);
            var clearBytes = ProtectedData.Unprotect(protectedBytes, ConfigEntropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<AgendaAuthConfig>(clearBytes, JsonOptions);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            TryDelete(_configPath);
            return null;
        }
    }

    public void SaveConfig(AgendaAuthConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var directory = Path.GetDirectoryName(_configPath)!;
        Directory.CreateDirectory(directory);
        var clearBytes = JsonSerializer.SerializeToUtf8Bytes(config, JsonOptions);
        var protectedBytes = ProtectedData.Protect(clearBytes, ConfigEntropy, DataProtectionScope.CurrentUser);
        var temporaryPath = $"{_configPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, protectedBytes);
            File.Move(temporaryPath, _configPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public void MarkInitialOnboardingPending(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length == 0)
        {
            return;
        }

        var pendingEmails = LoadInitialOnboardingPendingEmails();
        pendingEmails.Add(normalizedEmail);
        SaveInitialOnboardingPendingEmails(pendingEmails);
    }

    public bool IsInitialOnboardingPending(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length == 0)
        {
            return false;
        }

        return LoadInitialOnboardingPendingEmails().Contains(normalizedEmail);
    }

    public void ClearInitialOnboardingPending(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length == 0)
        {
            return;
        }

        var pendingEmails = LoadInitialOnboardingPendingEmails();
        if (!pendingEmails.Remove(normalizedEmail))
        {
            return;
        }

        if (pendingEmails.Count == 0)
        {
            TryDelete(_initialOnboardingPath);
            return;
        }

        SaveInitialOnboardingPendingEmails(pendingEmails);
    }

    private HashSet<string> LoadInitialOnboardingPendingEmails()
    {
        if (!File.Exists(_initialOnboardingPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(_initialOnboardingPath);
            var clearBytes = ProtectedData.Unprotect(
                protectedBytes,
                OnboardingEntropy,
                DataProtectionScope.CurrentUser);
            try
            {
                var savedEmails = JsonSerializer.Deserialize<string[]>(clearBytes, JsonOptions) ?? [];
                return savedEmails
                    .Select(NormalizeEmail)
                    .Where(item => item.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                // Compatibility with the first marker format, which stored one plain e-mail.
                var legacyEmail = NormalizeEmail(Encoding.UTF8.GetString(clearBytes));
                return legacyEmail.Length == 0
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>([legacyEmail], StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
        {
            TryDelete(_initialOnboardingPath);
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveInitialOnboardingPendingEmails(HashSet<string> pendingEmails)
    {
        var directory = Path.GetDirectoryName(_initialOnboardingPath)!;
        Directory.CreateDirectory(directory);
        var clearBytes = JsonSerializer.SerializeToUtf8Bytes(
            pendingEmails.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray(),
            JsonOptions);
        var protectedBytes = ProtectedData.Protect(clearBytes, OnboardingEntropy, DataProtectionScope.CurrentUser);
        var temporaryPath = $"{_initialOnboardingPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, protectedBytes);
            File.Move(temporaryPath, _initialOnboardingPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static string NormalizeEmail(string? email) =>
        (email ?? "").Trim().ToLowerInvariant();

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A remote sign-out must not be blocked by a best-effort local cleanup.
        }
    }
}

public sealed class AgendaAuthSessionManager : IDisposable
{
    public static readonly Uri ConfigEndpoint = new(
        "https://agenda-livre-next.edodoy.chatgpt.site/api/agenda/account/config",
        UriKind.Absolute);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _client;
    private readonly AgendaSessionStore _sessionStore;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private readonly HashSet<string> _pendingInitialOnboardingEmails = new(StringComparer.OrdinalIgnoreCase);

    private AgendaAuthSessionManager(
        HttpClient client,
        AgendaSessionStore sessionStore,
        AgendaAuthConfig config,
        bool configurationLoadedFromCache = false)
    {
        _client = client;
        _sessionStore = sessionStore;
        Config = config;
        ConfigurationLoadedFromCache = configurationLoadedFromCache;
    }

    public AgendaAuthConfig Config { get; }
    public AgendaAuthSession? CurrentSession { get; private set; }
    public bool ConfigurationLoadedFromCache { get; }
    public bool IsOfflineSession { get; private set; }
    public bool RequiresInitialOnboarding
    {
        get
        {
            var email = CurrentSession?.Email?.Trim() ?? "";
            return email.Length > 0 &&
                   (_pendingInitialOnboardingEmails.Contains(email) ||
                    _sessionStore.IsInitialOnboardingPending(email));
        }
    }

    public void CompleteInitialOnboarding()
    {
        var email = CurrentSession?.Email?.Trim() ?? "";
        if (email.Length == 0)
        {
            return;
        }

        _pendingInitialOnboardingEmails.Remove(email);
        try
        {
            _sessionStore.ClearInitialOnboardingPending(email);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
        {
            // The completed account data is authoritative; stale marker cleanup can retry later.
        }
    }

    public static async Task<AgendaAuthSessionManager> CreateAsync(CancellationToken cancellationToken = default)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var sessionStore = new AgendaSessionStore();
        var cachedConfig = sessionStore.LoadConfig();
        try
        {
            using var response = await client.GetAsync(ConfigEndpoint, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new AgendaAuthException(
                    $"Não foi possível carregar a configuração da conta (HTTP {(int)response.StatusCode}).",
                    response.StatusCode);
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var supabaseUrl = ReadString(root, "supabaseUrl", "supabase_url");
            var publishableKey = ReadString(root, "publishableKey", "anonKey", "supabaseAnonKey", "publishable_key");
            var syncValue = ReadString(root, "syncUrl", "sync_url");
            if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri) ||
                supabaseUri.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrWhiteSpace(publishableKey))
            {
                throw new AgendaAuthException("A configuração de acesso recebida é inválida.");
            }

            var syncUri = Uri.TryCreate(syncValue, UriKind.Absolute, out var absoluteSyncUri)
                ? absoluteSyncUri
                : new Uri(ConfigEndpoint, string.IsNullOrWhiteSpace(syncValue) ? "/api/agenda/account/state" : syncValue);
            if (syncUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new AgendaAuthException("O endereço de sincronização precisa usar HTTPS.");
            }

            var config = new AgendaAuthConfig(supabaseUri, publishableKey, syncUri);
            try
            {
                sessionStore.SaveConfig(config);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
            {
                // O cache é uma conveniência offline; a configuração online válida continua utilizável.
            }

            return new AgendaAuthSessionManager(client, sessionStore, config);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            client.Dispose();
            throw;
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or TaskCanceledException or AgendaAuthException or JsonException) &&
            IsUsableConfig(cachedConfig))
        {
            return new AgendaAuthSessionManager(
                client,
                sessionStore,
                cachedConfig!,
                configurationLoadedFromCache: true);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public async Task<AgendaAuthSession?> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var stored = _sessionStore.Load();
        if (stored is null || !AgendaOfflineSessionPolicy.HasUsableCachedIdentity(
                stored.UserId,
                stored.Email,
                stored.RefreshToken))
        {
            return null;
        }

        CurrentSession = stored;
        IsOfflineSession = false;
        try
        {
            return await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AgendaAuthException exception) when (
            AgendaOfflineSessionPolicy.InvalidatesCachedSession(exception.StatusCode))
        {
            CurrentSession = null;
            _sessionStore.Clear();
            return null;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or AgendaAuthException or JsonException)
        {
            IsOfflineSession = true;
            return stored;
        }
    }

    private static bool IsUsableConfig(AgendaAuthConfig? config) =>
        config is not null &&
        config.SupabaseUrl is not null &&
        config.SyncUrl is not null &&
        config.SupabaseUrl.Scheme == Uri.UriSchemeHttps &&
        config.SyncUrl.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(config.PublishableKey);

    public async Task<AgendaAuthSession> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var payload = new { email = email.Trim(), password };
        using var document = await SendAuthAsync(
            HttpMethod.Post,
            "/auth/v1/token?grant_type=password",
            payload,
            bearerToken: Config.PublishableKey,
            cancellationToken);
        var session = ParseSession(document.RootElement, email.Trim(), "", "")
            ?? throw new AgendaAuthException("A conta respondeu sem uma sessão válida.");
        SetCurrentSession(session);
        return session;
    }

    public async Task<AgendaSignUpResult> SignUpAsync(
        string email,
        string password,
        string fullName,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            email = email.Trim(),
            password,
            data = new
            {
                full_name = fullName.Trim(),
                business_name = businessName.Trim(),
                source = "agenda-livre-windows"
            }
        };
        using var document = await SendAuthAsync(
            HttpMethod.Post,
            "/auth/v1/signup",
            payload,
            bearerToken: Config.PublishableKey,
            cancellationToken);
        if (IsObfuscatedExistingAccountSignUp(document.RootElement))
        {
            throw new AgendaAuthException("Este e-mail já possui uma conta. Use a opção Entrar.");
        }

        MarkInitialOnboardingPending(email);
        var session = ParseSession(document.RootElement, email.Trim(), fullName.Trim(), businessName.Trim());
        if (session is not null)
        {
            SetCurrentSession(session);
            return new AgendaSignUpResult(session, false, "Conta criada. Você já pode usar o Agenda Livre.");
        }

        return new AgendaSignUpResult(
            null,
            true,
            "Conta criada. Confirme o e-mail que enviamos e depois entre com sua senha.");
    }

    public async Task<string> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var session = CurrentSession ?? throw new AgendaAuthException("Sua sessão terminou. Entre novamente.");
        if (!forceRefresh && session.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return session.AccessToken;
        }

        return (await RefreshAsync(cancellationToken)).AccessToken;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = CurrentSession?.AccessToken;
        try
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                using var request = CreateAuthRequest(HttpMethod.Post, "/auth/v1/logout", accessToken);
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                using var response = await _client.SendAsync(request, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Local sign-out is authoritative even when the network is unavailable.
        }
        finally
        {
            CurrentSession = null;
            _sessionStore.Clear();
        }
    }

    private async Task<AgendaAuthSession> RefreshAsync(CancellationToken cancellationToken)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try
        {
            var current = CurrentSession ?? throw new AgendaAuthException("Sua sessão terminou. Entre novamente.");
            if (string.IsNullOrWhiteSpace(current.RefreshToken))
            {
                throw new AgendaAuthException("Sua sessão não pode ser renovada. Entre novamente.");
            }

            using var document = await SendAuthAsync(
                HttpMethod.Post,
                "/auth/v1/token?grant_type=refresh_token",
                new { refresh_token = current.RefreshToken },
                bearerToken: Config.PublishableKey,
                cancellationToken);
            var refreshed = ParseSession(document.RootElement, current.Email, current.FullName, current.BusinessName)
                ?? throw new AgendaAuthException("Não foi possível renovar sua sessão.");
            SetCurrentSession(refreshed);
            return refreshed;
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    private async Task<JsonDocument> SendAuthAsync(
        HttpMethod method,
        string path,
        object payload,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthRequest(method, path, bearerToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AgendaAuthException(TranslateAuthError(response.StatusCode, body), response.StatusCode);
        }

        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        }
        catch (JsonException exception)
        {
            throw new AgendaAuthException($"O serviço de conta respondeu em um formato inválido: {exception.Message}");
        }
    }

    private HttpRequestMessage CreateAuthRequest(HttpMethod method, string path, string bearerToken)
    {
        var request = new HttpRequestMessage(method, new Uri(Config.SupabaseUrl, path.TrimStart('/')));
        request.Headers.TryAddWithoutValidation("apikey", Config.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return request;
    }

    private void SetCurrentSession(AgendaAuthSession session)
    {
        CurrentSession = session;
        IsOfflineSession = false;
        _sessionStore.Save(session);
    }

    private void MarkInitialOnboardingPending(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail.Length == 0)
        {
            return;
        }

        _pendingInitialOnboardingEmails.Add(normalizedEmail);
        try
        {
            _sessionStore.MarkInitialOnboardingPending(normalizedEmail);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
        {
            // The in-memory marker still guarantees the flow for the current run.
        }
    }

    private static bool IsObfuscatedExistingAccountSignUp(JsonElement root)
    {
        var user = TryGetProperty(root, "user", out var nestedUser) && nestedUser.ValueKind == JsonValueKind.Object
            ? nestedUser
            : root;
        return user.ValueKind == JsonValueKind.Object &&
               TryGetProperty(user, "identities", out var identities) &&
               identities.ValueKind == JsonValueKind.Array &&
               identities.GetArrayLength() == 0;
    }

    private static AgendaAuthSession? ParseSession(
        JsonElement root,
        string fallbackEmail,
        string fallbackFullName,
        string fallbackBusinessName)
    {
        var accessToken = ReadString(root, "access_token", "accessToken");
        var refreshToken = ReadString(root, "refresh_token", "refreshToken");
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var user = TryGetProperty(root, "user", out var userElement) && userElement.ValueKind == JsonValueKind.Object
            ? userElement
            : default;
        var userId = user.ValueKind == JsonValueKind.Object ? ReadString(user, "id") : "";
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = ReadJwtSubject(accessToken);
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new AgendaAuthException("A sessão recebida não identifica o usuário.");
        }

        var email = user.ValueKind == JsonValueKind.Object
            ? FirstFilled(ReadString(user, "email"), fallbackEmail)
            : fallbackEmail;
        var fullName = fallbackFullName;
        var businessName = fallbackBusinessName;
        if (user.ValueKind == JsonValueKind.Object &&
            TryGetProperty(user, "user_metadata", out var metadata) &&
            metadata.ValueKind == JsonValueKind.Object)
        {
            fullName = FirstFilled(ReadString(metadata, "full_name", "fullName", "name"), fullName);
            businessName = FirstFilled(ReadString(metadata, "business_name", "businessName"), businessName);
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        if (TryGetProperty(root, "expires_at", out var expiresAtElement) && TryReadInt64(expiresAtElement, out var expiresAtSeconds))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtSeconds);
        }
        else if (TryGetProperty(root, "expires_in", out var expiresInElement) && TryReadInt64(expiresInElement, out var expiresInSeconds))
        {
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresInSeconds));
        }

        return new AgendaAuthSession(
            userId,
            email,
            accessToken,
            refreshToken,
            expiresAt,
            fullName,
            businessName);
    }

    private static string TranslateAuthError(HttpStatusCode statusCode, string body)
    {
        var raw = "";
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            raw = ReadString(document.RootElement, "msg", "message", "error_description", "error");
            if (TryGetProperty(document.RootElement, "error", out var nested) && nested.ValueKind == JsonValueKind.Object)
            {
                raw = FirstFilled(ReadString(nested, "message", "description"), raw);
            }
        }
        catch (JsonException)
        {
            raw = "";
        }

        var normalized = raw.ToLowerInvariant();
        if (normalized.Contains("invalid login credentials", StringComparison.Ordinal) ||
            normalized.Contains("invalid_credentials", StringComparison.Ordinal))
        {
            return "E-mail ou senha incorretos.";
        }

        if (normalized.Contains("email not confirmed", StringComparison.Ordinal))
        {
            return "Confirme seu e-mail antes de entrar.";
        }

        if (normalized.Contains("already registered", StringComparison.Ordinal) ||
            normalized.Contains("user already exists", StringComparison.Ordinal))
        {
            return "Este e-mail já possui uma conta. Use a opção Entrar.";
        }

        if (normalized.Contains("password", StringComparison.Ordinal) && normalized.Contains("least", StringComparison.Ordinal))
        {
            return "A senha precisa ter pelo menos 6 caracteres.";
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return "Muitas tentativas. Aguarde um pouco e tente novamente.";
        }

        return string.IsNullOrWhiteSpace(raw)
            ? $"Não foi possível acessar a conta (HTTP {(int)statusCode})."
            : raw.Trim();
    }

    internal static string ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString()?.Trim() ?? "";
            }

            if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString().Trim();
            }
        }

        return "";
    }

    internal static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryReadInt64(JsonElement element, out long value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out value))
        {
            return true;
        }

        return long.TryParse(element.ToString(), out value);
    }

    private static string ReadJwtSubject(string token)
    {
        try
        {
            var pieces = token.Split('.');
            if (pieces.Length < 2)
            {
                return "";
            }

            var payload = pieces[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            return ReadString(document.RootElement, "sub");
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return "";
        }
    }

    private static string FirstFilled(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";

    public void Dispose()
    {
        _sessionGate.Dispose();
        _client.Dispose();
    }
}
