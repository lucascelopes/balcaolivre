using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace AgendaLivre.Windows;

public sealed record AgendaTrialState(
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndsAt,
    bool Active,
    int DaysRemaining);

public sealed record AgendaRemoteState(
    bool Exists,
    long Revision,
    int SchemaVersion,
    JsonElement Payload,
    DateTimeOffset? UpdatedAt,
    AgendaTrialState? Trial);

public sealed class AgendaSyncStatusEventArgs(string message, bool isWarning = false) : EventArgs
{
    public string Message { get; } = message;
    public bool IsWarning { get; } = isWarning;
}

public sealed class AgendaSyncConflictEventArgs(
    string message,
    long remoteRevision,
    string localCopyPath,
    string remoteCopyPath) : EventArgs
{
    public string Message { get; } = message;
    public long RemoteRevision { get; } = remoteRevision;
    public string LocalCopyPath { get; } = localCopyPath;
    public string RemoteCopyPath { get; } = remoteCopyPath;
}

public sealed class AgendaRemoteDataAppliedEventArgs(AgendaData data, long revision) : EventArgs
{
    public AgendaData Data { get; } = data;
    public long Revision { get; } = revision;
}

internal sealed record AgendaPendingSyncConflict(
    AgendaRemoteState Remote,
    string OriginalLocalSerialized,
    string LocalCopyPath,
    string RemoteCopyPath);

public sealed class AgendaSyncCoordinator : IDisposable
{
    private const int CloudSchemaVersion = 1;
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromSeconds(1.4);
    private static readonly TimeSpan ForegroundRefreshThrottle = TimeSpan.FromSeconds(8);
    private readonly AgendaDataStore _store;
    private readonly AgendaAuthSessionManager _auth;
    private readonly AgendaAccountStateClient _client;
    private readonly AgendaSyncMetadataStore _metadataStore;
    private readonly bool _allowLegacyMigration;
    private readonly SemaphoreSlim _pushGate = new(1, 1);
    private readonly object _pendingGate = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private CancellationTokenSource? _debounceCancellation;
    private AgendaPendingSyncConflict? _pendingConflict;
    private string _pendingSerialized = "";
    private long _revision;
    private bool _remoteExists;
    private AgendaSyncMetadata _metadata;
    private bool _bootstrapPending;
    private bool _attached;
    private bool _conflicted;
    private bool _disposed;
    private DateTimeOffset _lastCloudReadAttemptAt = DateTimeOffset.MinValue;

    private AgendaSyncCoordinator(
        AgendaDataStore store,
        AgendaAuthSessionManager auth,
        AgendaAccountStateClient client,
        string deviceId,
        bool allowLegacyMigration)
    {
        _store = store;
        _auth = auth;
        _client = client;
        DeviceId = deviceId;
        _allowLegacyMigration = allowLegacyMigration;
        _metadataStore = new AgendaSyncMetadataStore(store.DataRoot);
        _metadata = _metadataStore.Load();
        _revision = _metadata.BaseRevision;
    }

    public event EventHandler<AgendaSyncStatusEventArgs>? StatusChanged;
    public event EventHandler<AgendaSyncConflictEventArgs>? ConflictDetected;
    public event EventHandler<AgendaRemoteDataAppliedEventArgs>? RemoteDataApplied;

    public string DeviceId { get; }
    public AgendaData InitialData { get; private set; } = new();
    public string InitialNotice { get; private set; } = "";
    public AgendaSyncConflictEventArgs? InitialConflict { get; private set; }
    public AgendaTrialState? Trial { get; private set; }
    public bool HasConflict
    {
        get
        {
            lock (_pendingGate)
            {
                return _conflicted && _pendingConflict is not null;
            }
        }
    }

    public static async Task<AgendaSyncCoordinator> CreateAndReconcileAsync(
        AgendaDataStore store,
        AgendaAuthSessionManager auth,
        CancellationToken cancellationToken = default,
        bool allowLegacyMigration = true)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(auth);
        var coordinator = new AgendaSyncCoordinator(
            store,
            auth,
            new AgendaAccountStateClient(auth),
            AgendaDeviceIdentity.GetOrCreate(),
            allowLegacyMigration);
        await coordinator.ReconcileBeforeOpenAsync(cancellationToken);
        return coordinator;
    }

    public void Attach()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_attached)
        {
            return;
        }

        _attached = true;
        _store.Saved += Store_Saved;
        if ((_bootstrapPending || (!_remoteExists && IsReadyForCloud(InitialData))) &&
            File.Exists(_store.DataPath))
        {
            MarkPending();
            QueueSerialized(File.ReadAllText(_store.DataPath));
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        string serialized;
        lock (_pendingGate)
        {
            serialized = _pendingSerialized;
            _pendingSerialized = "";
            _debounceCancellation?.Cancel();
        }

        if (string.IsNullOrWhiteSpace(serialized) && _metadata.Pending && File.Exists(_store.DataPath))
        {
            serialized = File.ReadAllText(_store.DataPath);
        }

        if (!string.IsNullOrWhiteSpace(serialized) && !_conflicted)
        {
            await PushSerializedAsync(serialized, cancellationToken);
        }
    }

    public async Task ResolveConflictAsync(
        AgendaSyncConflictResolution resolution,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CancellationToken retryToken = default;
        AgendaData? remoteDataApplied = null;
        long appliedRevision = 0;

        await _pushGate.WaitAsync(cancellationToken);
        try
        {
            AgendaPendingSyncConflict pending;
            lock (_pendingGate)
            {
                pending = _pendingConflict
                          ?? throw new InvalidOperationException("Não há conflito de sincronização pendente.");
            }

            var transition = AgendaSyncConflictPolicy.CreateTransition(resolution, pending.Remote.Revision);
            var currentLocalSerialized = File.Exists(_store.DataPath)
                ? File.ReadAllText(_store.DataPath)
                : pending.OriginalLocalSerialized;

            if (transition.ApplyRemote)
            {
                // O usuário pode ter continuado trabalhando depois do alerta. Preserve também
                // essa versão mais recente antes de aplicar a escolha pela nuvem.
                SaveConflictCopies(pending.Remote, currentLocalSerialized);
                var local = JsonSerializer.Deserialize<AgendaData>(
                                currentLocalSerialized,
                                CloudAgendaPayloadCodec.JsonOptions)
                            ?? throw new JsonException("Os dados locais não puderam ser lidos para resolver o conflito.");
                var merged = CloudAgendaPayloadCodec.ApplyRemote(pending.Remote.Payload, local);
                ApplyAuthenticatedProfileDefaults(merged, _auth.CurrentSession);

                var applied = _store.TrySaveFromSync(
                    merged,
                    canApply: () => IsCurrentConflict(pending),
                    committed: () =>
                    {
                        lock (_pendingGate)
                        {
                            _revision = transition.BaseRevision;
                            _remoteExists = pending.Remote.Exists;
                            Trial = pending.Remote.Trial;
                            InitialData = merged;
                            _bootstrapPending = false;
                            _conflicted = false;
                            _pendingConflict = null;
                            InitialConflict = null;
                            CancelQueuedPushLocked();
                            _metadata = new AgendaSyncMetadata(
                                transition.BaseRevision,
                                transition.Pending,
                                DateTimeOffset.UtcNow);
                            _metadataStore.Save(_metadata);
                        }
                    });
                if (!applied)
                {
                    throw new InvalidOperationException("O conflito mudou enquanto estava sendo resolvido. Tente novamente.");
                }

                remoteDataApplied = merged;
                appliedRevision = transition.BaseRevision;
            }
            else if (transition.QueueLocal)
            {
                lock (_pendingGate)
                {
                    if (!ReferenceEquals(_pendingConflict, pending))
                    {
                        throw new InvalidOperationException("O conflito mudou enquanto estava sendo resolvido. Tente novamente.");
                    }

                    _revision = transition.BaseRevision;
                    _remoteExists = pending.Remote.Exists;
                    Trial = pending.Remote.Trial;
                    _bootstrapPending = false;
                    _conflicted = false;
                    _pendingConflict = null;
                    InitialConflict = null;
                    _metadata = new AgendaSyncMetadata(
                        transition.BaseRevision,
                        transition.Pending,
                        DateTimeOffset.UtcNow);
                    _metadataStore.Save(_metadata);
                    retryToken = ReplaceQueuedPushLocked(currentLocalSerialized);
                }
            }
        }
        finally
        {
            _pushGate.Release();
        }

        if (remoteDataApplied is not null)
        {
            RemoteDataApplied?.Invoke(
                this,
                new AgendaRemoteDataAppliedEventArgs(remoteDataApplied, appliedRevision));
            RaiseStatus("Conflito resolvido: os dados da nuvem foram aplicados. As cópias anteriores continuam preservadas.");
        }
        else if (retryToken.CanBeCanceled)
        {
            _ = DebounceAndPushAsync(retryToken);
            RaiseStatus("Conflito resolvido: os dados deste computador serão enviados para a nuvem.");
        }
    }

    public async Task RefreshFromCloudOnActivationAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginForegroundRefresh())
        {
            return;
        }

        var pushGateEntered = false;
        try
        {
            pushGateEntered = await _pushGate.WaitAsync(0, cancellationToken);
            if (!pushGateEntered || HasLocalChangesThatBlockPull())
            {
                return;
            }

            var remote = await _client.GetAsync(cancellationToken);
            Trial = remote.Trial;
            _remoteExists = remote.Exists;

            if (remote.Revision == _revision)
            {
                return;
            }

            if (remote.Revision < _revision)
            {
                RaiseStatus("A nuvem respondeu com uma revisão anterior; os dados deste computador foram preservados.", warning: true);
                return;
            }

            if (remote.SchemaVersion > CloudSchemaVersion)
            {
                RaiseStatus("Há uma versão mais nova da agenda na nuvem. Atualize o aplicativo para recebê-la com segurança.", warning: true);
                return;
            }

            if (!remote.Exists || remote.Payload.ValueKind != JsonValueKind.Object ||
                !File.Exists(_store.DataPath))
            {
                return;
            }

            var local = JsonSerializer.Deserialize<AgendaData>(
                            File.ReadAllText(_store.DataPath),
                            CloudAgendaPayloadCodec.JsonOptions)
                        ?? throw new JsonException("Os dados locais não puderam ser lidos para atualização.");
            var merged = CloudAgendaPayloadCodec.ApplyRemote(remote.Payload, local);
            ApplyAuthenticatedProfileDefaults(merged, _auth.CurrentSession);

            var applied = _store.TrySaveFromSync(
                merged,
                canApply: () => !HasLocalChangesThatBlockPull(),
                committed: () => CommitRemoteRefresh(remote, merged));
            if (!applied)
            {
                return;
            }

            RemoteDataApplied?.Invoke(this, new AgendaRemoteDataAppliedEventArgs(merged, remote.Revision));
            RaiseStatus("Agenda atualizada com as alterações feitas em outro dispositivo.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A janela está fechando; os dados locais permanecem válidos.
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // A retomada pode acontecer sem internet. O próximo Activated tentará novamente.
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // O aplicativo foi fechado enquanto a consulta estava em andamento.
        }
        catch (Exception exception) when (exception is AgendaAuthException or JsonException or IOException or UnauthorizedAccessException)
        {
            RaiseStatus($"Não foi possível buscar as alterações da nuvem. Os dados deste computador foram preservados: {CompactMessage(exception.Message)}", warning: true);
        }
        finally
        {
            if (pushGateEntered)
            {
                _pushGate.Release();
            }
        }
    }

    private bool TryBeginForegroundRefresh()
    {
        lock (_pendingGate)
        {
            if (_disposed || _conflicted || _metadata.Pending ||
                !string.IsNullOrWhiteSpace(_pendingSerialized))
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            if (now - _lastCloudReadAttemptAt < ForegroundRefreshThrottle)
            {
                return false;
            }

            _lastCloudReadAttemptAt = now;
            return true;
        }
    }

    private bool HasLocalChangesThatBlockPull()
    {
        lock (_pendingGate)
        {
            return _disposed || _conflicted || _metadata.Pending ||
                   !string.IsNullOrWhiteSpace(_pendingSerialized);
        }
    }

    private void CommitRemoteRefresh(AgendaRemoteState remote, AgendaData merged)
    {
        lock (_pendingGate)
        {
            _revision = remote.Revision;
            _remoteExists = remote.Exists;
            Trial = remote.Trial;
            InitialData = merged;
            _metadata = new AgendaSyncMetadata(remote.Revision, Pending: false, DateTimeOffset.UtcNow);
            _metadataStore.Save(_metadata);
        }
    }

    private async Task ReconcileBeforeOpenAsync(CancellationToken cancellationToken)
    {
        var hadAccountDataBeforeBootstrap = _store.HasLocalData;
        var local = _store.LoadOrCreate();
        ApplyAuthenticatedProfileDefaults(local, _auth.CurrentSession);
        _store.Save(local);

        try
        {
            _lastCloudReadAttemptAt = DateTimeOffset.UtcNow;
            var remote = await _client.GetAsync(cancellationToken);
            _revision = remote.Revision;
            _remoteExists = remote.Exists;
            Trial = remote.Trial;
            if (_metadata.Pending)
            {
                if (remote.Revision == _metadata.BaseRevision)
                {
                    _bootstrapPending = IsReadyForCloud(local);
                    InitialNotice = "Há alterações locais pendentes; elas serão sincronizadas após a abertura.";
                }
                else
                {
                    var localSerialized = File.ReadAllText(_store.DataPath);
                    InitialConflict = RegisterConflict(remote, localSerialized, notify: false);
                    InitialNotice = $"Conflito detectado: {InitialConflict.Message}";
                }
            }
            else if (remote.Exists && remote.Payload.ValueKind == JsonValueKind.Object)
            {
                local = CloudAgendaPayloadCodec.ApplyRemote(remote.Payload, local);
                ApplyAuthenticatedProfileDefaults(local, _auth.CurrentSession);
                _store.Save(local);
                _metadata = new AgendaSyncMetadata(remote.Revision, Pending: false, DateTimeOffset.UtcNow);
                _metadataStore.Save(_metadata);
                InitialNotice = "Agenda sincronizada com sua conta.";
            }
            else
            {
                if (_allowLegacyMigration &&
                    !hadAccountDataBeforeBootstrap &&
                    TryReadLegacyData(out var legacy))
                {
                    var session = _auth.CurrentSession;
                    var legacyEmail = legacy.Settings?.AccountEmail?.Trim() ?? "";
                    var sameEmail = session is not null &&
                                    !string.IsNullOrWhiteSpace(legacyEmail) &&
                                    legacyEmail.Equals(session.Email, StringComparison.OrdinalIgnoreCase);
                    var shouldMigrate = sameEmail || ConfirmLegacyMigration(legacyEmail, session?.Email ?? "");
                    if (shouldMigrate)
                    {
                        local = legacy;
                        ApplyAuthenticatedProfileDefaults(local, session);
                        _store.Save(local);
                        _bootstrapPending = IsReadyForCloud(local);
                        InitialNotice = "Os dados locais anteriores foram copiados para esta conta. O arquivo original foi preservado.";
                    }
                    else
                    {
                        InitialNotice = "Conta pronta. Conclua o cadastro para iniciar sua agenda.";
                    }
                }
                else
                {
                    InitialNotice = "Conta pronta. Conclua o cadastro para iniciar sua agenda.";
                }
            }

            if (remote.Trial is { Active: true } trial && trial.DaysRemaining >= 0)
            {
                InitialNotice += $" Teste: {trial.DaysRemaining} dia(s) restante(s).";
            }
            else if (remote.Trial is { Active: false })
            {
                InitialNotice += " Seu período de teste precisa ser renovado.";
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or AgendaAuthException or JsonException)
        {
            InitialNotice = $"Modo offline: os dados deste computador foram abertos. {CompactMessage(exception.Message)}";
        }

        InitialData = local;
    }

    private static bool TryReadLegacyData(out AgendaData data)
    {
        data = new AgendaData();
        var path = AgendaDataStore.LegacyDataPath;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            data = JsonSerializer.Deserialize<AgendaData>(File.ReadAllText(path), CloudAgendaPayloadCodec.JsonOptions)
                   ?? new AgendaData();
            return data.Settings is not null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private static bool ConfirmLegacyMigration(string legacyEmail, string accountEmail)
    {
        var source = string.IsNullOrWhiteSpace(legacyEmail)
            ? "sem e-mail identificado"
            : legacyEmail;
        var target = string.IsNullOrWhiteSpace(accountEmail)
            ? "a conta atual"
            : accountEmail;
        return MessageBox.Show(
                   $"Encontramos uma agenda antiga neste computador ({source}).\n\nDeseja copiar esses dados para {target}? O arquivo antigo será preservado.",
                   "Importar agenda deste computador",
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private void Store_Saved(object? sender, AgendaDataSavedEventArgs e)
    {
        if (!_attached || _disposed || _conflicted || !IsReadyForCloud(e.Data))
        {
            return;
        }

        MarkPending();
        QueueSerialized(e.Serialized);
    }

    private void QueueSerialized(string serialized)
    {
        CancellationToken token;
        lock (_pendingGate)
        {
            if (_disposed || _conflicted)
            {
                return;
            }

            token = ReplaceQueuedPushLocked(serialized);
        }

        _ = DebounceAndPushAsync(token);
    }

    private async Task DebounceAndPushAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceInterval, cancellationToken);
            string serialized;
            lock (_pendingGate)
            {
                serialized = _pendingSerialized;
                _pendingSerialized = "";
            }

            if (!string.IsNullOrWhiteSpace(serialized))
            {
                var consecutiveFailures = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await PushSerializedAsync(serialized, cancellationToken);
                        return;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception) when (AgendaSyncRetryPolicy.IsRetryable(exception))
                    {
                        consecutiveFailures++;
                        var delay = AgendaSyncRetryPolicy.DelayAfterFailure(consecutiveFailures);
                        RaiseStatus(
                            $"Alterações salvas neste computador. Sincronização pendente; nova tentativa em {delay.TotalSeconds:0}s: {CompactMessage(exception.Message)}",
                            warning: true);
                        await Task.Delay(delay, cancellationToken);

                        lock (_pendingGate)
                        {
                            if (_disposed || _conflicted || !string.IsNullOrWhiteSpace(_pendingSerialized))
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A newer save replaced this pending push.
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // O aplicativo foi fechado durante uma tentativa automática.
        }
    }

    private async Task PushSerializedAsync(string serialized, CancellationToken cancellationToken)
    {
        await _pushGate.WaitAsync(cancellationToken);
        try
        {
            if (_conflicted || _disposed)
            {
                return;
            }

            var payload = CloudAgendaPayloadCodec.CreateCloudPayload(serialized);
            var result = await _client.PutAsync(
                _revision,
                CloudSchemaVersion,
                payload,
                DeviceId,
                cancellationToken);
            if (result.ConflictRemote is not null)
            {
                HandleConflict(result.ConflictRemote, serialized);
                return;
            }

            var state = result.State ?? throw new AgendaAuthException("A sincronização respondeu sem estado.");
            _revision = state.Revision;
            _remoteExists = state.Exists;
            Trial = state.Trial;
            bool hasNewerPendingSave;
            lock (_pendingGate)
            {
                hasNewerPendingSave = !string.IsNullOrWhiteSpace(_pendingSerialized);
            }

            _metadata = new AgendaSyncMetadata(state.Revision, Pending: hasNewerPendingSave, DateTimeOffset.UtcNow);
            _metadataStore.Save(_metadata);
            RaiseStatus("Alterações sincronizadas com sua conta.");
        }
        finally
        {
            _pushGate.Release();
        }
    }

    private void HandleConflict(AgendaRemoteState remote, string localSerialized)
    {
        RegisterConflict(remote, localSerialized, notify: true);
    }

    private AgendaSyncConflictEventArgs RegisterConflict(
        AgendaRemoteState remote,
        string localSerialized,
        bool notify)
    {
        var copies = SaveConflictCopies(remote, localSerialized);
        var message = "Outra versão da agenda foi alterada em outro dispositivo. Nenhum lado foi sobrescrito; as duas cópias foram preservadas.";
        var eventArgs = new AgendaSyncConflictEventArgs(
            message,
            remote.Revision,
            copies.LocalPath,
            copies.RemotePath);

        lock (_pendingGate)
        {
            _conflicted = true;
            _pendingConflict = new AgendaPendingSyncConflict(
                remote,
                localSerialized,
                copies.LocalPath,
                copies.RemotePath);
            CancelQueuedPushLocked();
        }

        if (notify)
        {
            RaiseStatus(message, warning: true);
            ConflictDetected?.Invoke(this, eventArgs);
        }

        return eventArgs;
    }

    private (string LocalPath, string RemotePath) SaveConflictCopies(AgendaRemoteState remote, string localSerialized)
    {
        var directory = Path.Combine(_store.DataRoot, "sync-conflicts");
        Directory.CreateDirectory(directory);
        var stamp = $"{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}"[..28];
        var localPath = Path.Combine(directory, $"agenda-local-{stamp}.json");
        var remotePath = Path.Combine(directory, $"agenda-remota-r{remote.Revision}-{stamp}.json");
        File.WriteAllText(localPath, localSerialized, new UTF8Encoding(false));
        File.WriteAllText(
            remotePath,
            remote.Payload.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                ? remote.Payload.GetRawText()
                : "{}",
            new UTF8Encoding(false));
        return (localPath, remotePath);
    }

    private bool IsCurrentConflict(AgendaPendingSyncConflict pending)
    {
        lock (_pendingGate)
        {
            return !_disposed && _conflicted && ReferenceEquals(_pendingConflict, pending);
        }
    }

    private CancellationToken ReplaceQueuedPushLocked(string serialized)
    {
        _pendingSerialized = serialized;
        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        _debounceCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
        return _debounceCancellation.Token;
    }

    private void CancelQueuedPushLocked()
    {
        _pendingSerialized = "";
        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        _debounceCancellation = null;
    }

    private void MarkPending()
    {
        if (_metadata.Pending)
        {
            return;
        }

        _metadata = new AgendaSyncMetadata(_revision, Pending: true, DateTimeOffset.UtcNow);
        _metadataStore.Save(_metadata);
    }

    private void RaiseStatus(string message, bool warning = false) =>
        StatusChanged?.Invoke(this, new AgendaSyncStatusEventArgs(message, warning));

    private static bool IsReadyForCloud(AgendaData data) =>
        data.Settings.OnboardingCompleted &&
        !string.IsNullOrWhiteSpace(data.Settings.BusinessSegment) &&
        !string.IsNullOrWhiteSpace(data.Settings.AccountEmail);

    private static void ApplyAuthenticatedProfileDefaults(AgendaData data, AgendaAuthSession? session)
    {
        if (session is null)
        {
            return;
        }

        data.Settings.AccountEmail = session.Email;
        if (string.IsNullOrWhiteSpace(data.Settings.AccountFullName))
        {
            data.Settings.AccountFullName = session.FullName;
        }

        if ((string.IsNullOrWhiteSpace(data.Settings.BusinessName) ||
             data.Settings.BusinessName.Equals("Agenda Livre", StringComparison.OrdinalIgnoreCase) ||
             data.Settings.BusinessName.Equals("Balcão Livre", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(session.BusinessName))
        {
            data.Settings.BusinessName = session.BusinessName;
        }

        if (data.Settings.AccountCreatedAt == DateTime.MinValue)
        {
            data.Settings.AccountCreatedAt = DateTime.Now;
        }

        data.Settings.AccountPasswordHash = "";
    }

    private static string CompactMessage(string message)
    {
        var clean = (message ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
        return clean.Length <= 180 ? clean : clean[..180];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetimeCancellation.Cancel();
        if (_attached)
        {
            _store.Saved -= Store_Saved;
        }

        lock (_pendingGate)
        {
            CancelQueuedPushLocked();
        }

        _client.Dispose();
        _lifetimeCancellation.Dispose();
    }
}

internal sealed record AgendaSyncMetadata(long BaseRevision, bool Pending, DateTimeOffset UpdatedAt)
{
    public static AgendaSyncMetadata Empty { get; } = new(0, false, DateTimeOffset.MinValue);
}

internal sealed class AgendaSyncMetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;

    public AgendaSyncMetadataStore(string dataRoot)
    {
        _path = Path.Combine(dataRoot, "sync-meta.json");
    }

    public AgendaSyncMetadata Load()
    {
        if (!File.Exists(_path))
        {
            return AgendaSyncMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<AgendaSyncMetadata>(File.ReadAllText(_path), JsonOptions)
                   ?? AgendaSyncMetadata.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return AgendaSyncMetadata.Empty;
        }
    }

    public void Save(AgendaSyncMetadata metadata)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(metadata, JsonOptions), new UTF8Encoding(false));
        File.Move(temporaryPath, _path, overwrite: true);
    }
}

internal sealed record AgendaPutResult(AgendaRemoteState? State, AgendaRemoteState? ConflictRemote);

internal sealed class AgendaAccountStateClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AgendaAuthSessionManager _auth;
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(25) };

    public AgendaAccountStateClient(AgendaAuthSessionManager auth)
    {
        _auth = auth;
    }

    public async Task<AgendaRemoteState> GetAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, payload: null, forceRefresh: false, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            using var retry = await SendAsync(HttpMethod.Get, payload: null, forceRefresh: true, cancellationToken);
            return await ParseSuccessfulStateAsync(retry, cancellationToken);
        }

        return await ParseSuccessfulStateAsync(response, cancellationToken);
    }

    public async Task<AgendaPutResult> PutAsync(
        long baseRevision,
        int schemaVersion,
        JsonElement payload,
        string deviceId,
        CancellationToken cancellationToken)
    {
        var body = new { baseRevision, schemaVersion, payload, deviceId };
        using var response = await SendAsync(HttpMethod.Put, body, forceRefresh: false, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            using var retry = await SendAsync(HttpMethod.Put, body, forceRefresh: true, cancellationToken);
            return await ParsePutAsync(retry, cancellationToken);
        }

        return await ParsePutAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        object? payload,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var token = await _auth.GetAccessTokenAsync(forceRefresh, cancellationToken);
        using var request = new HttpRequestMessage(method, _auth.Config.SyncUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (payload is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        }

        return await _client.SendAsync(request, cancellationToken);
    }

    private static async Task<AgendaPutResult> ParsePutAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            using var conflictDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (AgendaAuthSessionManager.TryGetProperty(conflictDocument.RootElement, "remote", out var remote))
            {
                return new AgendaPutResult(null, ParseState(remote));
            }

            throw new AgendaAuthException("A agenda foi alterada em outro dispositivo.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new AgendaAuthException(ReadStateError(response.StatusCode, body), response.StatusCode);
        }

        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return new AgendaPutResult(ParseState(document.RootElement), null);
    }

    private static async Task<AgendaRemoteState> ParseSuccessfulStateAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AgendaAuthException(ReadStateError(response.StatusCode, body), response.StatusCode);
        }

        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return ParseState(document.RootElement);
    }

    private static AgendaRemoteState ParseState(JsonElement root)
    {
        var exists = ReadBool(root, "exists");
        var revision = ReadLong(root, "revision");
        var schemaVersion = (int)Math.Clamp(ReadLong(root, "schemaVersion", "schema_version"), 0, int.MaxValue);
        var payload = AgendaAuthSessionManager.TryGetProperty(root, "payload", out var payloadElement)
            ? payloadElement.Clone()
            : JsonDocument.Parse("{}").RootElement.Clone();
        var updatedAt = ReadDate(root, "updatedAt", "updated_at");
        AgendaTrialState? trial = null;
        if (AgendaAuthSessionManager.TryGetProperty(root, "trial", out var trialElement) &&
            trialElement.ValueKind == JsonValueKind.Object)
        {
            trial = new AgendaTrialState(
                ReadDate(trialElement, "startedAt", "started_at"),
                ReadDate(trialElement, "endsAt", "ends_at"),
                ReadBool(trialElement, "active"),
                (int)Math.Clamp(ReadLong(trialElement, "daysRemaining", "days_remaining"), 0, int.MaxValue));
        }

        return new AgendaRemoteState(exists, revision, schemaVersion, payload, updatedAt, trial);
    }

    private static string ReadStateError(HttpStatusCode statusCode, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (AgendaAuthSessionManager.TryGetProperty(document.RootElement, "error", out var error))
            {
                var message = error.ValueKind == JsonValueKind.Object
                    ? AgendaAuthSessionManager.ReadString(error, "message", "detail")
                    : error.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message.Trim();
                }
            }
        }
        catch (JsonException)
        {
            // Use the generic status message below.
        }

        return statusCode == HttpStatusCode.Unauthorized
            ? "Sua sessão terminou. Entre novamente."
            : $"A sincronização respondeu HTTP {(int)statusCode}.";
    }

    private static bool ReadBool(JsonElement element, string name)
    {
        if (!AgendaAuthSessionManager.TryGetProperty(element, name, out var value))
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True ||
               (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed);
    }

    private static long ReadLong(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!AgendaAuthSessionManager.TryGetProperty(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }

            if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return 0;
    }

    private static DateTimeOffset? ReadDate(JsonElement element, params string[] names)
    {
        var value = AgendaAuthSessionManager.ReadString(element, names);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    public void Dispose() => _client.Dispose();
}

internal static class CloudAgendaPayloadCodec
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static JsonElement CreateCloudPayload(string serialized)
    {
        var data = JsonSerializer.Deserialize<AgendaData>(serialized, JsonOptions)
            ?? throw new JsonException("Os dados locais não puderam ser preparados para sincronização.");
        SanitizeForCloud(data);
        return JsonSerializer.SerializeToElement(data, JsonOptions);
    }

    public static AgendaData ApplyRemote(JsonElement payload, AgendaData local)
    {
        var remote = JsonSerializer.Deserialize<AgendaData>(payload.GetRawText(), JsonOptions)
            ?? throw new JsonException("O estado remoto da agenda está vazio.");
        remote.Settings ??= new AgendaSettings();
        PreserveDeviceOnlySettings(remote.Settings, local.Settings);
        return remote;
    }

    private static void SanitizeForCloud(AgendaData data)
    {
        data.Settings ??= new AgendaSettings();
        var settings = data.Settings;
        settings.BusinessLogoPath = "";
        settings.AccountPasswordHash = "";

        settings.WhatsAppLinked = false;
        settings.WhatsAppStorePhone = "";
        settings.WhatsAppConnectedName = "";
        settings.WhatsAppLinkedAt = null;
        settings.WhatsAppLastMessageAt = null;
        settings.WhatsAppEvolutionBaseUrl = "";
        settings.WhatsAppEvolutionApiKey = "";
        settings.WhatsAppEvolutionInstanceName = "";
        settings.WhatsAppEvolutionState = "";
        settings.WhatsAppEvolutionQrBase64 = "";
        settings.WhatsAppEvolutionLastCheckedAt = null;

        settings.PublicBookingApiUrl = "";
        settings.PublicBookingLastSyncAt = null;

        settings.InstagramLinked = false;
        settings.InstagramApiUrl = "";
        settings.InstagramAccountId = "";
        settings.InstagramState = "";
        settings.InstagramLastError = "";
        settings.InstagramLinkedAt = null;
        settings.InstagramLastCheckedAt = null;

        settings.MercadoPagoConnected = false;
        settings.MercadoPagoLicenseKey = "";
        settings.MercadoPagoPaymentsApiUrl = "";
        settings.MercadoPagoSellerUserId = "";
        settings.MercadoPagoDefaultTerminalId = "";
        settings.MercadoPagoDefaultTerminalLabel = "";
        settings.MercadoPagoLastError = "";
        settings.MercadoPagoLastSyncAt = null;

    }

    private static void PreserveDeviceOnlySettings(AgendaSettings target, AgendaSettings local)
    {
        target.BusinessLogoPath = local.BusinessLogoPath;
        target.AccountPasswordHash = "";

        target.WhatsAppLinked = local.WhatsAppLinked;
        target.WhatsAppStorePhone = local.WhatsAppStorePhone;
        target.WhatsAppConnectedName = local.WhatsAppConnectedName;
        target.WhatsAppLinkedAt = local.WhatsAppLinkedAt;
        target.WhatsAppLastMessageAt = local.WhatsAppLastMessageAt;
        target.WhatsAppEvolutionBaseUrl = local.WhatsAppEvolutionBaseUrl;
        target.WhatsAppEvolutionApiKey = local.WhatsAppEvolutionApiKey;
        target.WhatsAppEvolutionInstanceName = local.WhatsAppEvolutionInstanceName;
        target.WhatsAppEvolutionState = local.WhatsAppEvolutionState;
        target.WhatsAppEvolutionQrBase64 = local.WhatsAppEvolutionQrBase64;
        target.WhatsAppEvolutionLastCheckedAt = local.WhatsAppEvolutionLastCheckedAt;

        target.PublicBookingApiUrl = local.PublicBookingApiUrl;
        target.PublicBookingLastSyncAt = local.PublicBookingLastSyncAt;

        target.InstagramLinked = local.InstagramLinked;
        target.InstagramApiUrl = local.InstagramApiUrl;
        target.InstagramAccountId = local.InstagramAccountId;
        target.InstagramState = local.InstagramState;
        target.InstagramLastError = local.InstagramLastError;
        target.InstagramLinkedAt = local.InstagramLinkedAt;
        target.InstagramLastCheckedAt = local.InstagramLastCheckedAt;

        target.MercadoPagoConnected = local.MercadoPagoConnected;
        target.MercadoPagoLicenseKey = local.MercadoPagoLicenseKey;
        target.MercadoPagoPaymentsApiUrl = local.MercadoPagoPaymentsApiUrl;
        target.MercadoPagoSellerUserId = local.MercadoPagoSellerUserId;
        target.MercadoPagoDefaultTerminalId = local.MercadoPagoDefaultTerminalId;
        target.MercadoPagoDefaultTerminalLabel = local.MercadoPagoDefaultTerminalLabel;
        target.MercadoPagoLastError = local.MercadoPagoLastError;
        target.MercadoPagoLastSyncAt = local.MercadoPagoLastSyncAt;
    }
}

internal static class AgendaDeviceIdentity
{
    public static string GetOrCreate()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgendaLivre.Windows");
        var path = Path.Combine(root, "device-id");
        try
        {
            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path).Trim();
                if (Guid.TryParse(existing, out _))
                {
                    return existing;
                }
            }

            Directory.CreateDirectory(root);
            var created = Guid.NewGuid().ToString("D");
            File.WriteAllText(path, created, new UTF8Encoding(false));
            return created;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return $"ephemeral-{Guid.NewGuid():N}";
        }
    }
}
