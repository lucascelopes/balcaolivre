using System.Net.Http;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private bool _cloudBackupRunning;
    private bool _centralSyncRunning;
    private DateTime _lastCentralSyncQueuedAt = DateTime.MinValue;

    private void QueueCloudBackup(AppStore store, bool force = false)
    {
        if (!_appSettings.CloudBackupEnabled || !_appSettings.AdminSyncEnabled || _cloudBackupRunning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NormalizeActivationKey(_appSettings.ActivationKey)))
        {
            return;
        }

        if (!force && _appSettings.LastCloudBackupAt.HasValue && DateTime.Now - _appSettings.LastCloudBackupAt.Value < TimeSpan.FromHours(6))
        {
            return;
        }

        var endpoint = BuildAdminApiUri("/api/app/backup");
        if (endpoint is null)
        {
            return;
        }

        var storeJson = JsonSerializer.Serialize(store, JsonOptions);
        var payload = FillAdminPayload(new AdminCloudBackupPayload(), force ? "backup.manual" : "backup.auto");
        payload.LocalWhen = DateTimeOffset.Now;
        payload.Store = store;
        payload.StoreBytes = Encoding.UTF8.GetByteCount(storeJson);
        payload.StoreHash = Sha256Hex(storeJson);

        _cloudBackupRunning = true;
        _ = SendCloudBackupAsync(endpoint, payload, force);
    }

    private async Task SendCloudBackupAsync(Uri endpoint, AdminCloudBackupPayload payload, bool showStatus)
    {
        try
        {
            await PostAdminJsonAsync(endpoint, payload, TimeSpan.FromSeconds(25));
            await Dispatcher.InvokeAsync(() =>
            {
                _appSettings.LastCloudBackupAt = DateTime.Now;
                SaveAppSettings();
                if (showStatus)
                {
                    SetStatus("Backup versionado enviado para o gateway online.");
                }
            }, DispatcherPriority.Background);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            Debug.WriteLine($"Cloud backup failed: {ex.Message}");
            if (showStatus)
            {
                await Dispatcher.InvokeAsync(() => SetStatus($"Backup online indisponivel agora: {ex.Message}"), DispatcherPriority.Background);
            }
        }
        finally
        {
            _cloudBackupRunning = false;
        }
    }

    private void QueueCentralSync(string eventName, AppStore store, bool force = false)
    {
        if (!_appSettings.CentralSyncEnabled || !_appSettings.AdminSyncEnabled || _centralSyncRunning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NormalizeActivationKey(_appSettings.ActivationKey)))
        {
            return;
        }

        if (!force && DateTime.Now - _lastCentralSyncQueuedAt < TimeSpan.FromMinutes(10))
        {
            return;
        }

        var endpoint = BuildAdminApiUri("/api/app/sync");
        if (endpoint is null)
        {
            return;
        }

        _lastCentralSyncQueuedAt = DateTime.Now;
        var payload = FillAdminPayload(new AdminCentralSyncPayload(), eventName);
        payload.SyncKind = "summary";
        payload.LocalWhen = DateTimeOffset.Now;
        payload.Summary = BuildCentralSyncSummary(store);

        _centralSyncRunning = true;
        _ = SendCentralSyncAsync(endpoint, payload, force);
    }

    private async Task SendCentralSyncAsync(Uri endpoint, AdminCentralSyncPayload payload, bool showStatus)
    {
        try
        {
            await PostAdminJsonAsync(endpoint, payload, TimeSpan.FromSeconds(10));
            await Dispatcher.InvokeAsync(() =>
            {
                _appSettings.LastCentralSyncAt = DateTime.Now;
                SaveAppSettings();
                ApplyRestaurantIdentity();
                if (showStatus)
                {
                    SetStatus("Sync central atualizado.");
                }
            }, DispatcherPriority.Background);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            Debug.WriteLine($"Central sync failed: {ex.Message}");
            if (showStatus)
            {
                await Dispatcher.InvokeAsync(() => SetStatus($"Sync central indisponivel agora: {ex.Message}"), DispatcherPriority.Background);
            }
        }
        finally
        {
            _centralSyncRunning = false;
        }
    }

    private async Task PostAdminJsonAsync(Uri endpoint, object payload, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = timeout };
        using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(endpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
                ? $"Gateway online retornou {(int)response.StatusCode}."
                : body);
        }
    }

    private T FillAdminPayload<T>(T payload, string eventName)
        where T : AdminClientPayload
    {
        var source = CreateAdminClientPayload(
            eventName,
            _appSettings.ActivationKey,
            _appSettings.ActivationExpiresAt,
            _appSettings.ActivationPlan);
        payload.EventName = source.EventName;
        payload.LicenseKey = source.LicenseKey;
        payload.MachineHash = source.MachineHash;
        payload.MachineCode = source.MachineCode;
        payload.AppVersion = source.AppVersion;
        payload.LocalExpiresAt = source.LocalExpiresAt;
        payload.LocalPlan = source.LocalPlan;
        payload.Profile = source.Profile;
        payload.Settings = source.Settings;
        payload.Metrics = source.Metrics;
        return payload;
    }

    private CentralSyncSummary BuildCentralSyncSummary(AppStore store)
    {
        var boards = store.Tables.Concat(store.DeliveryTiles).ToList();
        var today = DateTime.Today;
        var payments = boards
            .SelectMany(board => board.ClosedPayments.Concat(board.Payments))
            .Where(payment => payment.When.Date == today)
            .ToList();
        var openBoards = boards.Count(board =>
            !string.Equals(board.Status, "LIVRE", StringComparison.OrdinalIgnoreCase)
            && (board.Lines.Count > 0 || board.Payments.Count > 0 || board.Total > 0));

        return new CentralSyncSummary
        {
            BusinessName = store.Profile?.BusinessName ?? "",
            OpenCash = IsCashOpen(),
            CashTotal = store.CashTotal,
            OpenBoards = openBoards,
            DeliveryOrders = store.DeliveryTiles.Count,
            KitchenPending = store.KitchenTiles.Count(tile => !string.Equals(tile.Status, "ENTREGUE", StringComparison.OrdinalIgnoreCase)),
            TodayRevenue = payments.Sum(payment => payment.Amount),
            Products = store.Products.Count,
            Users = store.Users.Count,
            Customers = store.Customers.Count,
            LowStock = store.Products.Count(product => product.IsLowStock)
        };
    }

    private static string Sha256Hex(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    public sealed class AdminCentralSyncPayload : AdminClientPayload
    {
        public string SyncKind { get; set; } = "summary";
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
        public CentralSyncSummary Summary { get; set; } = new();
    }

    public sealed class AdminCloudBackupPayload : AdminClientPayload
    {
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
        public string StoreHash { get; set; } = "";
        public long StoreBytes { get; set; }
        public AppStore Store { get; set; } = new();
    }

    public sealed class CentralSyncSummary
    {
        public string BusinessName { get; set; } = "";
        public bool OpenCash { get; set; }
        public decimal CashTotal { get; set; }
        public int OpenBoards { get; set; }
        public int DeliveryOrders { get; set; }
        public int KitchenPending { get; set; }
        public decimal TodayRevenue { get; set; }
        public int Products { get; set; }
        public int Users { get; set; }
        public int Customers { get; set; }
        public int LowStock { get; set; }
    }
}
