using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Printing;
using System.Media;
using System.Reflection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QRCoder;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using DrawingIcon = System.Drawing.Icon;
using Forms = System.Windows.Forms;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SystemIcons = System.Drawing.SystemIcons;
using TextBox = System.Windows.Controls.TextBox;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow : Window
{
    private const string AppDisplayName = "Balcao Livre PDV Online";
    private const string AppReceiptName = "BALCAO LIVRE PDV ONLINE";
    private const string DefaultUpdateManifestUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/version.json";
    private const string DefaultAdminApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/license";
    private const string DefaultPaymentsApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments";
    private const string DefaultPagBankApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/pagbank";
    private const string DefaultCheckoutFunctionUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/checkout";
    private static bool PagBankIntegrationAvailable => false;
    private const string LegacyAdminApiUrl = "https://balcaolivrepdv.onrender.com";
    private const string DefaultWhatsAppFunctionUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp";
    private const string PublicMenuApexHost = "balcaolivrepdv.com.br";
    private const string PublicMenuHost = "cardapio.balcaolivrepdv.com.br";
    private const string DefaultPublicMenuBaseUrl = "https://cardapio.balcaolivrepdv.com.br";
    private const string DefaultIFoodAlertSoundFile = "Assets\\ifood-order-alert.mp3";
    private const string PasswordHashPrefix = "PBKDF2";
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly Brush FreeTile = Solid("#8FDB7A");
    private static readonly Brush NewTile = Solid("#0B3A52");
    private static readonly Brush OccupiedTile = Solid("#F49A93");
    private static readonly Brush PrepTile = Solid("#F4CC61");
    private static readonly Brush PreparingTile = Solid("#F0B15A");
    private static readonly Brush ConfirmedTile = Solid("#D8D96B");
    private static readonly Brush AcceptedTile = Solid("#A6D977");
    private static readonly Brush AccountTile = Solid("#B7C4D8");
    private static readonly Brush WaitingTile = Solid("#D9D76D");
    private static readonly Brush RouteTile = Solid("#B59CFF");
    private static readonly Brush ReadyTile = Solid("#8FDB7A");
    private static readonly Brush DispatchedTile = Solid("#67DCCB");
    private static readonly Brush DeliveredTile = Solid("#9BE58F");
    private static readonly Brush CancellationRequestedTile = Solid("#F49A93");
    private static readonly Brush CancelledTile = Solid("#F49A93");
    private static readonly Brush BlueSoft = Solid("#EAF8FA");
    private static readonly Brush GreenSoft = Solid("#E6FBF8");
    private static readonly Brush GreenText = Solid("#176B36");
    private static readonly Brush AmberSoft = Solid("#FFF2CB");
    private static readonly Brush AmberText = Solid("#99620D");
    private static readonly Brush RedSoft = Solid("#FFE2DF");
    private static readonly Brush RedText = Solid("#A11D1D");
    private const string CouvertCode = "900001";
    private const string ServiceCode = "900002";
    private const double RibbonScrollStep = 260;
    private static readonly int[] ActivationWarningDays = [1, 3, 7, 15, 30];

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct PlugPagTransactionResult
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65543)]
        public string RawBuffer;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string Message;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string TransactionCode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
        public string Date;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
        public string Time;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
        public string HostNsu;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 31)]
        public string CardBrand;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 7)]
        public string Bin;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public string Holder;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 11)]
        public string UserReference;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 66)]
        public string TerminalSerialNumber;
    }

    [DllImport("PPPagSeguro.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int InitBTConnection(byte[] comport);

    [DllImport("PPPagSeguro.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int SetVersionName(byte[] appName, byte[] version);

    [DllImport("PPPagSeguro.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int SimplePaymentTransaction(
        int paymentMethod,
        int installmentType,
        int installments,
        byte[] amount,
        byte[] userReference,
        IntPtr transactionResult);
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.8) };
    private readonly DispatcherTimer _licenseTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly DispatcherTimer _ifoodSyncTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _deliveryCountdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _supportPollTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly DispatcherTimer _publicMenuPublishTimer = new() { Interval = TimeSpan.FromSeconds(7) };
    private readonly DispatcherTimer _publicMenuOrderPollTimer = new() { Interval = TimeSpan.FromSeconds(8) };
    private readonly DispatcherTimer _kitchenPrintBatchTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly DispatcherTimer _localBackupTimer = new() { Interval = TimeSpan.FromMinutes(30) };
    private readonly Dictionary<TableTile, HashSet<TicketLine>> _pendingKitchenPrintLines = [];
    private readonly IFoodCloudClient _ifoodClient = new();
    private const int MaxAutomaticLocalBackupFiles = 336;

    private readonly string _dataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BalcaoLivre.Online.Windows");

    private KeyboardArea _area = KeyboardArea.Products;
    private int _selectedTableIndex;
    private int _selectedProductIndex;
    private int _selectedCategoryIndex;
    private int _selectedTicketIndex;
    private bool _supportPollRunning;
    private DateTimeOffset? _lastSupportAdminMessageAt;
    private bool _updatingTableSelection;
    private bool _ifoodPresenceLoopStarted;
    private readonly object _settingsFileLock = new();
    private decimal _cashTotal;
    private string _currentUser = "";
    private LocalHubSettings _settings = new();
    private RestaurantIdentityProfile _profile = new();
    private AppSettings _appSettings = new();
    private Forms.NotifyIcon? _trayIcon;
    private WaiterLocalServer? _waiterServer;
    private WhatsAppLocalConnectorServer? _whatsAppConnectorServer;
    private bool _exitRequested;
    private bool _activationPromptOpen;
    private bool _ifoodSyncRunning;
    private bool _publicMenuPublishRunning;
    private bool _publicMenuOrderPollRunning;
    private bool _suppressPublicMenuQueue;
    private string _pendingPublicMenuSignature = "";
    private string _lastPublishedPublicMenuSignature = "";
    private bool _suppressNextToastSound;
    private MediaPlayer? _ifoodAlertPlayer;
    private DateTime _lastIFoodSyncErrorAt = DateTime.MinValue;
    private DateTime _lastIFoodCatalogSyncUtc = DateTime.MinValue;
    private DateTime _lastPublicMenuOrderSyncErrorAt = DateTime.MinValue;

    public ObservableCollection<RibbonAction> RibbonActions { get; } = [];
    public ObservableCollection<string> Modes { get; } = [];
    public ObservableCollection<TableTile> Tables { get; } = [];
    public ObservableCollection<TableTile> BoardTiles { get; } = [];
    public ObservableCollection<TableTile> DeliveryTiles { get; } = [];
    public ObservableCollection<TableTile> KitchenTiles { get; } = [];
    public ObservableCollection<CategoryTile> Categories { get; } = [];
    public ObservableCollection<ProductTile> Products { get; } = [];
    public ObservableCollection<ProductTile> VisibleProducts { get; } = [];
    public ObservableCollection<TicketLine> TicketLines { get; } = [];
    public ObservableCollection<PaymentLine> Payments { get; } = [];
    public ObservableCollection<UserAccount> Users { get; } = [];
    public ObservableCollection<CustomerRecord> Customers { get; } = [];
    public ObservableCollection<DeliveryDriver> Drivers { get; } = [];
    public ObservableCollection<CashMovement> CashMovements { get; } = [];
    public ObservableCollection<WhatsAppMessageLog> WhatsAppHistory { get; } = [];
    public ObservableCollection<WhatsAppPendingOrder> WhatsAppPendingOrders { get; } = [];

    private string StoreFile => Path.Combine(_dataRoot, "commandas-store.json");
    private string SettingsFile => Path.Combine(_dataRoot, "app-settings.json");
    private string ProfileFile => Path.Combine(_dataRoot, "restaurant-profile.json");
    private string BackupDir => Path.Combine(_dataRoot, "backups");
    private string AutomaticBackupDir => Path.Combine(BackupDir, "automaticos");
    private string SqlBackupDir => Path.Combine(BackupDir, "sql");
    private string LatestLocalBackupFile => Path.Combine(BackupDir, "commandas-store-latest.json");
    private string LatestSqlBackupFile => Path.Combine(BackupDir, "commandas-store-latest.sql");
    private string ExportDir
    {
        get
        {
            var path = Path.Combine(_dataRoot, "exports");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    private TableTile? CurrentBoard => BoardTiles.Count == 0
        ? null
        : BoardTiles[Math.Clamp(_selectedTableIndex, 0, BoardTiles.Count - 1)];

    private string CurrentMode => ModeList?.SelectedItem as string ?? "Comandas";

    public MainWindow()
    {
        InitializeComponent();
        _toastTimer.Tick += (_, _) => HideToast();
        _licenseTimer.Tick += (_, _) => CheckActivationExpiry();
        _ifoodSyncTimer.Tick += async (_, _) => await AutoImportIFoodOrdersAsync();
        _deliveryCountdownTimer.Tick += (_, _) => RefreshDeliveryCountdownTiles();
        _supportPollTimer.Tick += async (_, _) => await PollSupportNotificationsAsync();
        _publicMenuOrderPollTimer.Tick += async (_, _) => await PollPublicMenuOrdersAsync();
        _kitchenPrintBatchTimer.Tick += (_, _) => FlushPendingKitchenPrints();
        _localBackupTimer.Tick += (_, _) => CreateLocalStoreBackup("auto-30min", force: false);
        _publicMenuPublishTimer.Tick += async (_, _) =>
        {
            _publicMenuPublishTimer.Stop();
            await PublishGeneratedPublicMenuAsync(silent: true);
        };
        Directory.CreateDirectory(_dataRoot);
        LoadAppSettings();
        LoadRestaurantProfile();
        ApplyRestaurantIdentity();
        SeedStaticUi();
        LoadStore();
        CreateLocalStoreBackup("inicializacao", force: false);
        RefreshOnlineStoreButton();
        _ = SyncIFoodPresenceOnceAsync();
        StartIFoodPresenceLoop();
        ResetWhatsAppRuntimeState();
        SaveAppSettings();
        DataContext = this;
        ModeList.SelectedIndex = 0;
        RefreshBoardForMode();
        SelectTable(0);
        SelectCategory(0);
        FilterProducts();
        SelectProduct(0);
        SelectArea(KeyboardArea.Products);
        RefreshTotals();
        InitializeTrayIcon();
        Closing += MainWindow_Closing;
        _ = StartWaiterServerAsync();
        Loaded += (_, _) =>
        {
            if (_appSettings.IFood?.HasCloudConnection == true)
            {
                _ifoodSyncTimer.Start();
                _ = AutoImportIFoodOrdersAsync(force: true);
                StartIFoodPresenceLoop();
            }

            if (!RequireStartupLogin())
            {
                return;
            }

            _localBackupTimer.Start();
            TableBox.Focus();
            TableBox.SelectAll();
            SelectArea(KeyboardArea.Ticket);
            SetStatus("Digite a mesa e pressione Enter. Modo online pronto; caixa local continua funcionando.");
            _licenseTimer.Start();
            if (!_ifoodSyncTimer.IsEnabled)
            {
                _ifoodSyncTimer.Start();
            }
            _deliveryCountdownTimer.Start();
            _supportPollTimer.Start();
            _publicMenuOrderPollTimer.Start();
            if (_appSettings.AutoCheckUpdates)
            {
                _ = Dispatcher.BeginInvoke(async () =>
                    await CheckForUpdatesAsync(showIfCurrent: false, autoInstall: true));
            }
            QueueAdminCheckIn("startup");
        };
    }

    private UserAccount? CurrentUser => Users.FirstOrDefault(user =>
        string.Equals(user.Name, _currentUser, StringComparison.OrdinalIgnoreCase));

    private static bool IsManagerUser(UserAccount user)
    {
        return user.IsMaster
            || string.Equals(user.Role, "MASTER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Role, "GERENTE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCashUser(UserAccount user)
    {
        return IsManagerUser(user)
            || user.CanCash
            || string.Equals(user.Role, "CAIXA", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanManageSettings(UserAccount user) => IsManagerUser(user) || user.CanSettings;
    private static bool CanOperateDelivery(UserAccount user) => IsServiceOrCashUser(user) || user.CanDelivery;
    private static bool CanManageInventory(UserAccount user) => IsManagerUser(user) || user.CanInventory;
    private static bool CanManageIFood(UserAccount user) => IsManagerUser(user) || user.CanIFood;
    private static bool CanManageBackup(UserAccount user) => IsManagerUser(user) || user.CanBackup;
    private static bool CanManageFiscal(UserAccount user) => IsManagerUser(user) || user.CanFiscal;
    private static bool CanManageKitchen(UserAccount user) => IsManagerUser(user) || user.CanKitchen;
    private static bool CanManageDeliveryZones(UserAccount user) => IsManagerUser(user) || user.CanDeliveryZones;
    private static bool CanSyncCentralData(UserAccount user) => IsManagerUser(user) || user.CanCentralSync;

    private static bool IsWaiterUser(UserAccount user)
    {
        return IsManagerUser(user)
            || user.CanTransfer
            || string.Equals(user.Role, "GARCOM", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsServiceOrCashUser(UserAccount user)
    {
        return IsWaiterUser(user) || IsCashUser(user);
    }

    private bool RequireStartupLogin()
    {
        if (!EnsureFirstInstallSetup())
        {
            _exitRequested = true;
            Application.Current.Shutdown();
            return false;
        }

        if (CurrentUser is not null)
        {
            return true;
        }

        var authenticated = ShowOperatorPasswordDialog(
            "Entrada no PDV",
            "Informe operador e senha para iniciar o sistema.",
            "Entrar",
            _ => true,
            out var user);

        if (!authenticated || user is null)
        {
            _exitRequested = true;
            Application.Current.Shutdown();
            return false;
        }

        _currentUser = user.Name;
        SetStatus($"Operador conectado: {user.Name} ({user.Role}).");
        return true;
    }

    private bool EnsureFirstInstallSetup()
    {
        if (HasValidActivation() && !CanCreateFirstAccessUser())
        {
            return true;
        }

        return ShowInstallSetupDialog();
    }

    private bool HasValidActivation()
    {
        return _appSettings.ActivationCompleted
            && !string.IsNullOrWhiteSpace(_appSettings.ActivationKey)
            && (string.IsNullOrWhiteSpace(_appSettings.ActivationMachineHash)
                || string.Equals(_appSettings.ActivationMachineHash, GetMachineFingerprint(), StringComparison.Ordinal))
            && (!_appSettings.ActivationExpiresAt.HasValue || _appSettings.ActivationExpiresAt.Value >= DateTime.Now);
    }

    private void CheckActivationExpiry()
    {
        if (_activationPromptOpen
            || !_appSettings.ActivationCompleted
            || !_appSettings.ActivationExpiresAt.HasValue)
        {
            return;
        }

        if (_appSettings.ActivationExpiresAt.Value > DateTime.Now)
        {
            ShowActivationWarningIfNeeded();
            return;
        }

        _licenseTimer.Stop();
        var expiredKey = _appSettings.ActivationKey;
        var expiredPlan = _appSettings.ActivationPlan;
        _appSettings.ActivationCompleted = false;
        SaveAppSettings();
        ShowToast("Ativacao expirada", "A licenca venceu. Pague a assinatura para receber uma nova chave.", "BL", "#A11D1D", "#FFE2DF");
        SetStatus("Ativacao expirada. Abrindo pagamento da assinatura.");
        OpenLicenseRenewalPage(expiredKey, expiredPlan, showMessage: false);

        if (!ShowInstallSetupDialog())
        {
            _exitRequested = true;
            Application.Current.Shutdown();
            return;
        }

        _licenseTimer.Start();
    }

    private void ShowActivationWarningIfNeeded()
    {
        if (!_appSettings.ActivationExpiresAt.HasValue)
        {
            return;
        }

        var now = DateTime.Now;
        var remaining = _appSettings.ActivationExpiresAt.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            return;
        }

        var daysRemaining = Math.Max(1, (int)Math.Ceiling(remaining.TotalDays));
        var threshold = ActivationWarningDays.FirstOrDefault(days => daysRemaining <= days);
        if (threshold == 0)
        {
            return;
        }

        var warningKey = $"{NormalizeActivationKey(_appSettings.ActivationKey)}|{_appSettings.ActivationExpiresAt.Value:yyyyMMddHHmm}|{threshold}";
        if (string.Equals(_appSettings.ActivationLastWarningKey, warningKey, StringComparison.Ordinal))
        {
            return;
        }

        _appSettings.ActivationLastWarningKey = warningKey;
        SaveAppSettings();

        var dayText = daysRemaining == 1 ? "1 dia" : $"{daysRemaining} dias";
        var expiresText = _appSettings.ActivationExpiresAt.Value.ToString("dd/MM/yyyy HH:mm", Brazil);
        var message = $"Sua licenca expira em {dayText}. Vencimento: {expiresText}.";
        ShowToast("Licenca perto do vencimento", message, "BL", "#99620D", "#FFF2CB");
        SetStatus(message);
    }

    private bool OpenLicenseRenewalPage(string licenseKey = "", string plan = "", bool showMessage = true)
    {
        var key = NormalizeActivationKey(string.IsNullOrWhiteSpace(licenseKey) ? _appSettings.ActivationKey : licenseKey);
        var url = BuildLicenseRenewalUrl(key, string.IsNullOrWhiteSpace(plan) ? _appSettings.ActivationPlan : plan);
        if (string.IsNullOrWhiteSpace(url))
        {
            if (showMessage)
            {
                SetStatus("Nao encontrei a chave para abrir a renovacao.");
            }

            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            if (showMessage)
            {
                SetStatus("Pagamento da assinatura aberto no Stripe.");
            }

            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            if (showMessage)
            {
                SetStatus($"Nao consegui abrir o pagamento: {ex.Message}");
            }

            return false;
        }
    }

    private string BuildLicenseRenewalUrl(string licenseKey, string plan)
    {
        var key = NormalizeActivationKey(licenseKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            return "";
        }

        var query = new List<string>
        {
            $"license_key={Uri.EscapeDataString(key)}"
        };

        var planText = string.IsNullOrWhiteSpace(plan) ? _appSettings.ActivationPlan : plan;
        if (!string.IsNullOrWhiteSpace(planText))
        {
            query.Add($"plan={Uri.EscapeDataString(planText)}");
        }

        if (!string.IsNullOrWhiteSpace(_profile.Email))
        {
            query.Add($"email={Uri.EscapeDataString(_profile.Email.Trim().ToLowerInvariant())}");
        }

        return $"{DefaultCheckoutFunctionUrl.TrimEnd('/')}/renew?{string.Join("&", query)}";
    }

    private async Task<AdminActivationResult> TryValidateAdminActivationAsync(string normalizedKey, DateTime? expiresAt, string plan)
    {
        if (!_appSettings.AdminSyncEnabled)
        {
            return AdminActivationResult.Allow(plan, expiresAt, "Sincronizacao admin desligada.");
        }

        var endpoints = BuildAdminActivationEndpoints();
        if (endpoints.Count == 0)
        {
            return AdminActivationResult.Allow(plan, expiresAt, "URL do admin invalida. Ativacao local liberada.");
        }

        var payload = CreateAdminClientPayload("activation", normalizedKey, expiresAt, plan);
        AdminActivationResult? lastDeny = null;
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(endpoint.Uri, content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdminActivationResult>(json, JsonOptions);
                if (result is not null)
                {
                    if (result.Ok)
                    {
                        if (endpoint.IsDefaultSupabase)
                        {
                            _appSettings.AdminApiUrl = DefaultAdminApiUrl;
                            SaveAppSettings();
                        }

                        return result;
                    }

                    lastDeny = result;
                    if (!ShouldRetryActivationOnSupabase(result))
                    {
                        return result;
                    }

                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    lastDeny = AdminActivationResult.Deny("Admin recusou a ativacao, mas nao retornou detalhes.");
                }
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
            {
                Debug.WriteLine($"Admin activation sync unavailable: {ex.Message}");
            }
        }

        if (lastDeny is not null)
        {
            return lastDeny;
        }

        return AdminActivationResult.Allow(plan, expiresAt, "Admin offline. Ativacao local liberada.");
    }

    private List<(Uri Uri, bool IsDefaultSupabase)> BuildAdminActivationEndpoints()
    {
        var endpoints = new List<(Uri Uri, bool IsDefaultSupabase)>();
        var configured = BuildAdminApiUri("/api/app/activate");
        if (configured is not null)
        {
            endpoints.Add((configured, string.Equals(
                (_appSettings.AdminApiUrl ?? "").Trim().TrimEnd('/'),
                DefaultAdminApiUrl,
                StringComparison.OrdinalIgnoreCase)));
        }

        var fallback = BuildAdminApiUri(DefaultAdminApiUrl, "/api/app/activate");
        if (fallback is not null && endpoints.All(item => item.Uri != fallback))
        {
            endpoints.Add((fallback, true));
        }

        return endpoints;
    }

    private static bool ShouldRetryActivationOnSupabase(AdminActivationResult result)
    {
        var message = result.Message ?? "";
        return message.Contains("Chave nao existe no painel admin", StringComparison.OrdinalIgnoreCase)
            || message.Contains("chave nao criada", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Gere uma chave nova", StringComparison.OrdinalIgnoreCase);
    }

    private void QueueAdminCheckIn(string eventName, bool force = false)
    {
        if (!_appSettings.AdminSyncEnabled || string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            return;
        }

        if (!force
            && _appSettings.LastAdminSyncAt.HasValue
            && DateTime.Now - _appSettings.LastAdminSyncAt.Value < TimeSpan.FromMinutes(15))
        {
            return;
        }

        var payload = CreateAdminClientPayload(eventName, _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        _ = Task.Run(async () =>
        {
            if (await SendAdminCheckInAsync(payload))
            {
                _ = Dispatcher.BeginInvoke(() =>
                {
                    _appSettings.LastAdminSyncAt = DateTime.Now;
                    SaveAppSettings();
                    ApplyRestaurantIdentity();
                });
            }
        });
    }

    private async Task<bool> SendAdminCheckInAsync(AdminClientPayload payload)
    {
        var endpoint = BuildAdminApiUri("/api/app/checkin");
        if (endpoint is null)
        {
            return false;
        }

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
        {
            Debug.WriteLine($"Admin check-in failed: {ex.Message}");
            return false;
        }
    }

    private async Task<T> PostPaymentsOperationAsync<T>(string path, AdminClientPayload payload, TimeSpan timeout)
        where T : AdminMercadoPagoResult, new()
    {
        var endpoint = BuildPaymentsApiUri(path);
        if (endpoint is null)
        {
            return new T { Ok = false, Message = "URL de pagamentos invalida." };
        }

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = timeout };
            using var content = new StringContent(JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(body, JsonOptions) ?? new T
            {
                Ok = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "" : body
            };

            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message))
            {
                result.Message = string.IsNullOrWhiteSpace(body)
                    ? $"Admin retornou {(int)response.StatusCode}."
                    : body;
            }

            return result;
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"Admin operation failed ({path}): {ex.Message}");
            return new T { Ok = false, Message = "Nao foi possivel falar com o Supabase agora." };
        }
    }

    private async Task<T> PostPagBankOperationAsync<T>(string path, AdminClientPayload payload, TimeSpan timeout)
        where T : AdminMercadoPagoResult, new()
    {
        var endpoint = BuildAdminApiUri(DefaultPagBankApiUrl, path);
        if (endpoint is null)
        {
            return new T { Ok = false, Message = "URL do PagBank invalida." };
        }

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = timeout };
            using var content = new StringContent(JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(body, JsonOptions) ?? new T
            {
                Ok = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "" : body
            };

            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message))
            {
                result.Message = string.IsNullOrWhiteSpace(body)
                    ? $"PagBank retornou {(int)response.StatusCode}."
                    : body;
            }

            return result;
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"PagBank operation failed ({path}): {ex.Message}");
            return new T { Ok = false, Message = "Nao foi possivel falar com o Supabase agora." };
        }
    }

    private Task<AdminMercadoPagoConnectResult> StartMercadoPagoConnectAsync()
    {
        var payload = FillAdminPayload(new AdminClientPayload(), "mercadopago.connect.start");
        return PostPaymentsOperationAsync<AdminMercadoPagoConnectResult>(
            "/mercadopago/connect/start",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminMercadoPagoConnectionStatusResult> FetchMercadoPagoConnectionStatusAsync()
    {
        var payload = FillAdminPayload(new AdminClientPayload(), "mercadopago.status");
        return PostPaymentsOperationAsync<AdminMercadoPagoConnectionStatusResult>(
            "/mercadopago/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminMercadoPagoTerminalsResult> FetchMercadoPagoTerminalsAsync()
    {
        var payload = FillAdminPayload(new AdminClientPayload(), "mercadopago.terminals");
        return PostPaymentsOperationAsync<AdminMercadoPagoTerminalsResult>(
            "/mercadopago/terminals",
            payload,
            TimeSpan.FromSeconds(14));
    }

    private Task<AdminMercadoPagoResult> SelectMercadoPagoTerminalAsync(string terminalId, string terminalLabel)
    {
        var payload = FillAdminPayload(new AdminMercadoPagoTerminalPayload
        {
            TerminalId = terminalId,
            TerminalLabel = terminalLabel
        }, "mercadopago.terminal.select");
        return PostPaymentsOperationAsync<AdminMercadoPagoResult>(
            "/mercadopago/terminal/select",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminMercadoPagoChargeResult> CreateMercadoPagoPointChargeAsync(decimal amount, string method, string localReference, string description)
    {
        var payload = FillAdminPayload(new AdminMercadoPagoChargePayload
        {
            Amount = MercadoPagoAmountValue(amount),
            Method = method,
            LocalReference = localReference,
            Description = description,
            TerminalId = _appSettings.MercadoPago.DefaultTerminalId,
            Items = BuildMercadoPagoChargeItems()
        }, "mercadopago.point.charge");
        return PostPaymentsOperationAsync<AdminMercadoPagoChargeResult>(
            "/mercadopago/point/charge",
            payload,
            TimeSpan.FromSeconds(14));
    }

    private Task<AdminMercadoPagoPointStatusResult> FetchMercadoPagoPointStatusAsync(string attemptId, string orderId, string localReference)
    {
        var payload = FillAdminPayload(new AdminMercadoPagoPointStatusPayload
        {
            AttemptId = attemptId,
            OrderId = orderId,
            LocalReference = localReference
        }, "mercadopago.point.status");
        return PostPaymentsOperationAsync<AdminMercadoPagoPointStatusResult>(
            "/mercadopago/point/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminMercadoPagoWebChargeResult> CreateMercadoPagoWebChargeAsync(decimal amount, string method, string localReference, string description, string payer)
    {
        var payload = FillAdminPayload(new AdminMercadoPagoWebChargePayload
        {
            Amount = MercadoPagoAmountValue(amount),
            Method = method,
            LocalReference = localReference,
            Description = description,
            PayerName = payer,
            Items = BuildMercadoPagoChargeItems()
        }, "mercadopago.web.charge");
        return PostPaymentsOperationAsync<AdminMercadoPagoWebChargeResult>(
            "/mercadopago/web/charge",
            payload,
            TimeSpan.FromSeconds(18));
    }

    private Task<AdminMercadoPagoPointStatusResult> FetchMercadoPagoWebStatusAsync(string attemptId, string orderId, string localReference)
    {
        var payload = FillAdminPayload(new AdminMercadoPagoPointStatusPayload
        {
            AttemptId = attemptId,
            OrderId = orderId,
            LocalReference = localReference
        }, "mercadopago.web.status");
        return PostPaymentsOperationAsync<AdminMercadoPagoPointStatusResult>(
            "/mercadopago/web/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private void ApplyMercadoPagoStatus(AdminMercadoPagoConnectionStatusResult result)
    {
        _appSettings.MercadoPago ??= new MercadoPagoPaymentSettings();
        _appSettings.MercadoPago.Connected = result.Ok && result.Connected;
        _appSettings.MercadoPago.SellerUserId = result.SellerUserId ?? "";
        _appSettings.MercadoPago.LastError = result.LastError ?? "";
        if (!string.IsNullOrWhiteSpace(result.SelectedTerminalId))
        {
            _appSettings.MercadoPago.DefaultTerminalId = result.SelectedTerminalId.Trim();
            _appSettings.MercadoPago.DefaultTerminalLabel = string.IsNullOrWhiteSpace(result.SelectedTerminalLabel)
                ? result.SelectedTerminalId.Trim()
                : result.SelectedTerminalLabel.Trim();
        }

        if (DateTime.TryParse(result.LastSyncAt, Brazil, DateTimeStyles.AssumeLocal, out var lastSync))
        {
            _appSettings.MercadoPago.LastSyncAt = lastSync;
        }
    }

    private bool IsMercadoPagoPointReady(string method)
    {
        if (!_appSettings.MercadoPago.Enabled || !_appSettings.MercadoPago.Connected)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_appSettings.MercadoPago.DefaultTerminalId))
        {
            return false;
        }

        return method is "CREDITO" or "DEBITO";
    }

    private async Task<PaymentLine?> ProcessMercadoPagoPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        if (!_appSettings.MercadoPago.Enabled)
        {
            return null;
        }

        if (!_appSettings.MercadoPago.Connected)
        {
            System.Windows.MessageBox.Show(owner, "Conecte a conta Mercado Pago em Config antes de cobrar.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var hasTerminal = !string.IsNullOrWhiteSpace(_appSettings.MercadoPago.DefaultTerminalId);
        var mode = ChooseMercadoPagoCollectionMode(method, hasTerminal, owner);
        if (string.IsNullOrWhiteSpace(mode))
        {
            return null;
        }

        return mode == "POINT"
            ? await ProcessMercadoPagoPointPaymentAsync(method, amount, payer, owner)
            : await ProcessMercadoPagoWebPaymentAsync(method, amount, payer, owner);
    }

    private string ChooseMercadoPagoCollectionMode(string method, bool hasTerminal, Window owner)
    {
        if (!hasTerminal)
        {
            return "WEB";
        }

        var result = "";
        var isPix = method == "PIX";
        var dialog = CreateDialog("Mercado Pago", 500, 360);
        dialog.Owner = owner;
        dialog.ResizeMode = ResizeMode.NoResize;

        var primaryMode = isPix ? "WEB" : "POINT";
        var secondaryMode = "WEB";
        var primary = DialogButton(isPix ? "Gerar QR Pix" : "Enviar para Point", "#08A99B");
        var secondary = DialogButton(isPix ? "Usar Pix copia-e-cola" : "Gerar link", "#2D73B9");
        var cancel = DialogButton("Cancelar", "#5B6B7A");
        foreach (var button in new[] { primary, secondary, cancel })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }

        primary.Click += (_, _) =>
        {
            result = primaryMode;
            dialog.Close();
        };
        secondary.Click += (_, _) =>
        {
            result = secondaryMode;
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        var panel = DialogPanel();
        panel.Children.Add(SectionTitle(isPix ? "Como cobrar este Pix?" : "Como cobrar este cartao?"));
        panel.Children.Add(new TextBlock
        {
            Text = isPix
                ? "O Pix sai por QR na tela ou copia-e-cola do Mercado Pago. A Point integrada via API fica para debito/credito; assim o PDV nao promete Pix na maquininha quando o Mercado Pago nao liberar."
                : "A Point recebe a cobranca online pela conta Mercado Pago. Nao e por cabo USB: a maquininha precisa estar conectada na conta da loja e selecionada em Config.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });
        panel.Children.Add(primary);
        if (!isPix)
        {
            panel.Children.Add(secondary);
        }
        panel.Children.Add(cancel);
        dialog.Content = panel;
        dialog.ShowDialog();
        return result;
    }

    private async Task<PaymentLine?> ProcessMercadoPagoPointPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        if (!_appSettings.MercadoPago.Enabled)
        {
            return null;
        }

        if (!_appSettings.MercadoPago.Connected)
        {
            System.Windows.MessageBox.Show(owner, "Conecte a conta Mercado Pago em Config antes de cobrar na maquininha.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        if (string.IsNullOrWhiteSpace(_appSettings.MercadoPago.DefaultTerminalId))
        {
            System.Windows.MessageBox.Show(owner, "Escolha a maquininha Mercado Pago em Config antes de cobrar.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var localReference = $"BLV-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
        var charge = await CreateMercadoPagoPointChargeAsync(
            amount,
            method,
            localReference,
            BuildMercadoPagoChargeDescription(method, amount));

        if (!charge.Ok)
        {
            System.Windows.MessageBox.Show(owner, string.IsNullOrWhiteSpace(charge.Message) ? "Mercado Pago recusou a cobranca." : charge.Message, "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var cancelled = false;
        var paid = false;
        var lastStatus = charge.Status;
        var waitDialog = CreateDialog("Mercado Pago Point", 460, 270);
        waitDialog.Owner = owner;
        waitDialog.ResizeMode = ResizeMode.NoResize;
        var statusText = new TextBlock
        {
            Text = $"Cobranca enviada para {_appSettings.MercadoPago.DefaultTerminalLabel}. Aguarde o pagamento.",
            Foreground = Solid("#071A2C"),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        var detailText = new TextBlock
        {
            Text = $"Valor: {Money(amount)} | {method}",
            Foreground = Solid("#5B6B7A"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var cancel = DialogButton("Parar espera", "#5B6B7A");
        cancel.HorizontalAlignment = HorizontalAlignment.Stretch;
        cancel.Click += (_, _) =>
        {
            cancelled = true;
            waitDialog.Close();
        };
        waitDialog.Closed += (_, _) => cancelled = !paid;
        var panel = DialogPanel();
        panel.Children.Add(SectionTitle(method == "PIX" ? "Confirme o Pix na maquininha" : "Passe o cartao na maquininha"));
        panel.Children.Add(statusText);
        panel.Children.Add(detailText);
        panel.Children.Add(new TextBlock
        {
            Text = "O PDV fecha a venda somente quando o Mercado Pago confirmar.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 16)
        });
        panel.Children.Add(cancel);
        waitDialog.Content = panel;
        waitDialog.Show();

        for (var attempt = 0; attempt < 45 && !cancelled; attempt++)
        {
            await Task.Delay(attempt == 0 ? 1200 : 2500);
            if (cancelled)
            {
                break;
            }

            var status = await FetchMercadoPagoPointStatusAsync(charge.AttemptId, charge.OrderId, charge.LocalReference);
            if (!status.Ok)
            {
                detailText.Text = string.IsNullOrWhiteSpace(status.Message)
                    ? $"Aguardando retorno... ultima situacao: {TextOrDefault(lastStatus, "CRIADO")}"
                    : status.Message;
                continue;
            }

            lastStatus = TextOrDefault(status.Status, lastStatus);
            detailText.Text = $"Situacao: {TextOrDefault(lastStatus, "aguardando")} | tentativa {attempt + 1}/45";
            if (status.Paid)
            {
                paid = true;
                waitDialog.Close();
                return new PaymentLine
                {
                    Payer = string.IsNullOrWhiteSpace(payer) ? "Cliente" : payer.Trim(),
                    Method = $"MP {method}",
                    Amount = amount,
                    TenderedAmount = amount,
                    ChangeAmount = 0,
                    When = DateTime.Now
                };
            }

            if (IsMercadoPagoFinalFailure(lastStatus))
            {
                waitDialog.Close();
                System.Windows.MessageBox.Show(owner, $"Pagamento nao aprovado: {lastStatus}", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        if (!paid && !cancelled)
        {
            waitDialog.Close();
            System.Windows.MessageBox.Show(owner, "Ainda nao houve confirmacao do Mercado Pago. Confira a maquininha antes de tentar de novo.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        return null;
    }

    private async Task<PaymentLine?> ProcessMercadoPagoWebPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        var localReference = $"BLV-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
        var isPix = method == "PIX";
        var charge = await CreateMercadoPagoWebChargeAsync(
            amount,
            method,
            localReference,
            BuildMercadoPagoWebChargeDescription(method, amount),
            payer);

        if (!charge.Ok)
        {
            System.Windows.MessageBox.Show(owner, string.IsNullOrWhiteSpace(charge.Message) ? "Mercado Pago recusou a cobranca." : charge.Message, "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var displayPayload = isPix
            ? TextOrDefault(charge.QrCode, charge.PaymentUrl)
            : charge.PaymentUrl;
        var qrSource = isPix
            ? TryCreateBitmapFromBase64(charge.QrCodeBase64) ?? TryCreateQrBitmap(displayPayload, 8)
            : TryCreateQrBitmap(displayPayload, 8);

        if (string.IsNullOrWhiteSpace(displayPayload) || qrSource is null)
        {
            System.Windows.MessageBox.Show(owner, "Mercado Pago gerou a cobranca, mas nao retornou QR/link para exibir.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var cancelled = false;
        var paid = false;
        var lastStatus = charge.Status;
        var waitDialog = CreateDialog(isPix ? "Pix Mercado Pago" : "Pagamento Mercado Pago", 620, isPix ? 760 : 690);
        waitDialog.Owner = owner;
        waitDialog.ResizeMode = ResizeMode.NoResize;

        var title = new TextBlock
        {
            Text = isPix ? "Mostre o QR Pix para o cliente" : "Mostre ou abra o link para o cliente pagar",
            Foreground = Solid("#071A2C"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        var subtitle = new TextBlock
        {
            Text = isPix
                ? "O PDV fecha a venda somente quando o Mercado Pago confirmar o Pix."
                : "O cliente paga pelo Mercado Pago. O PDV fecha a venda somente quando o pagamento for aprovado.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 14)
        };
        var image = new System.Windows.Controls.Image
        {
            Source = qrSource,
            Width = 330,
            Height = 330,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        var codeBox = new TextBox
        {
            Text = displayPayload,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = isPix ? 92 : 62,
            Margin = new Thickness(0, 0, 0, 12),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var statusText = new TextBlock
        {
            Text = $"Aguardando pagamento de {Money(amount)}...",
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 12)
        };
        var copy = DialogButton(isPix ? "Copiar Pix" : "Copiar link", "#5B6B7A");
        var open = DialogButton(isPix ? "Abrir Pix" : "Abrir link", "#2D73B9");
        var cancel = DialogButton("Parar espera", "#5B6B7A");
        foreach (var button in new[] { copy, open, cancel })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }

        copy.Click += (_, _) =>
        {
            try
            {
                System.Windows.Clipboard.SetText(displayPayload);
                statusText.Text = isPix ? "Pix copia-e-cola copiado." : "Link copiado.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or ExternalException)
            {
                statusText.Text = "Nao foi possivel copiar agora.";
            }
        };
        open.Click += (_, _) =>
        {
            var url = TextOrDefault(charge.PaymentUrl, charge.TicketUrl);
            if (string.IsNullOrWhiteSpace(url))
            {
                statusText.Text = "Sem link para abrir. Use o QR/copia-e-cola.";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                statusText.Text = "Nao foi possivel abrir o link.";
            }
        };
        cancel.Click += (_, _) =>
        {
            cancelled = true;
            waitDialog.Close();
        };
        waitDialog.Closed += (_, _) => cancelled = !paid;

        var buttons = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 0) };
        buttons.Children.Add(copy);
        buttons.Children.Add(open);
        buttons.Children.Add(cancel);

        var panel = DialogPanel();
        panel.Children.Add(title);
        panel.Children.Add(subtitle);
        panel.Children.Add(image);
        panel.Children.Add(DialogLabel(isPix ? "Pix copia-e-cola" : "Link de pagamento"));
        panel.Children.Add(codeBox);
        panel.Children.Add(statusText);
        panel.Children.Add(buttons);
        waitDialog.Content = panel;
        waitDialog.Show();
        _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (TryPrintQrOnlyToDefaultPrinter(displayPayload, isPix ? "Mercado Pago Pix QR" : "Mercado Pago Link QR"))
            {
                statusText.Text = $"{(isPix ? "Pix" : "Link")} impresso em QR grande. Aguardando pagamento de {Money(amount)}...";
            }
        }));

        for (var attempt = 0; attempt < 72 && !cancelled; attempt++)
        {
            await Task.Delay(attempt == 0 ? 1500 : 2500);
            if (cancelled)
            {
                break;
            }

            var status = await FetchMercadoPagoWebStatusAsync(charge.AttemptId, charge.OrderId, charge.LocalReference);
            if (!status.Ok)
            {
                statusText.Text = string.IsNullOrWhiteSpace(status.Message)
                    ? $"Aguardando retorno... ultima situacao: {TextOrDefault(lastStatus, "CRIADO")}"
                    : status.Message;
                continue;
            }

            lastStatus = TextOrDefault(status.Status, lastStatus);
            statusText.Text = $"Situacao: {TextOrDefault(lastStatus, "aguardando")} | tentativa {attempt + 1}/72";
            if (status.Paid)
            {
                paid = true;
                waitDialog.Close();
                return new PaymentLine
                {
                    Payer = string.IsNullOrWhiteSpace(payer) ? "Cliente" : payer.Trim(),
                    Method = isPix ? "MP PIX" : $"MP LINK {method}",
                    Amount = amount,
                    TenderedAmount = amount,
                    ChangeAmount = 0,
                    When = DateTime.Now
                };
            }

            if (IsMercadoPagoFinalFailure(lastStatus))
            {
                waitDialog.Close();
                System.Windows.MessageBox.Show(owner, $"Pagamento nao aprovado: {lastStatus}", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        if (!paid && !cancelled)
        {
            waitDialog.Close();
            System.Windows.MessageBox.Show(owner, "Ainda nao houve confirmacao do Mercado Pago. Confira a conta antes de tentar de novo.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        return null;
    }

    private static bool IsMercadoPagoFinalFailure(string status)
    {
        var normalized = (status ?? "").Trim().ToUpperInvariant();
        return normalized is "CANCELLED" or "CANCELED" or "FAILED" or "REJECTED" or "EXPIRED" or "REFUNDED";
    }

    private string BuildMercadoPagoChargeDescription(string method, decimal amount)
    {
        var merchantName = FirstNonEmpty(_profile.BusinessName, _profile.LegalName, AppDisplayName);
        var boardLabel = BuildMercadoPagoBoardLabel(CurrentBoard);
        var itemSummary = BuildMercadoPagoItemSummary();
        var parts = new List<string> { merchantName };

        if (!string.IsNullOrWhiteSpace(boardLabel))
        {
            parts.Add(boardLabel);
        }

        parts.Add($"{method} {Money(amount)}");
        if (!string.IsNullOrWhiteSpace(itemSummary))
        {
            parts.Add(itemSummary);
        }

        return ClipMercadoPagoDescription(string.Join(" - ", parts));
    }

    private string BuildMercadoPagoWebChargeDescription(string method, decimal amount)
    {
        var merchantName = FirstNonEmpty(_profile.BusinessName, _profile.LegalName, AppDisplayName);
        return ClipMercadoPagoDescription($"{merchantName} - {method} {Money(amount)}");
    }

    private List<AdminMercadoPagoItemPayload> BuildMercadoPagoChargeItems()
    {
        return TicketLines
            .Where(line => line.Quantity > 0
                           && line.UnitPrice > 0
                           && !string.IsNullOrWhiteSpace(line.Name))
            .Take(50)
            .Select(line => new AdminMercadoPagoItemPayload
            {
                Code = CompactSingleLine(line.Code),
                Title = ClipMercadoPagoItemTitle(line.Name),
                Quantity = line.Quantity,
                UnitPrice = MercadoPagoAmountValue(line.UnitPrice),
                Description = BuildMercadoPagoLineDescription(line)
            })
            .ToList();
    }

    private string BuildMercadoPagoItemSummary()
    {
        var lines = TicketLines
            .Where(line => line.Quantity > 0 && !string.IsNullOrWhiteSpace(line.Name))
            .ToList();
        if (lines.Count == 0)
        {
            return "";
        }

        var summary = lines
            .Take(3)
            .Select(line => $"{line.Quantity}x {CompactSingleLine(line.Name)}")
            .ToList();
        if (lines.Count > summary.Count)
        {
            summary.Add($"+{lines.Count - summary.Count} item(ns)");
        }

        return string.Join("; ", summary);
    }

    private static string BuildMercadoPagoBoardLabel(TableTile? board)
    {
        if (board is null || string.IsNullOrWhiteSpace(board.Number))
        {
            return "";
        }

        return $"{BoardKindLabel(board)} {CompactSingleLine(board.Number)}";
    }

    private static string ClipMercadoPagoDescription(string value)
    {
        var clean = CompactSingleLine(value);
        return clean.Length <= 120 ? clean : clean[..120].Trim();
    }

    private static string ClipMercadoPagoItemTitle(string value)
    {
        var clean = CompactSingleLine(value);
        return clean.Length <= 120 ? clean : clean[..120].Trim();
    }

    private static string BuildMercadoPagoLineDescription(TicketLine line)
    {
        return CompactSingleLine(string.Join(" | ", new[] { line.ModifierSummary, line.Note, line.Sector }
            .Where(part => !string.IsNullOrWhiteSpace(part))));
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            var clean = CompactSingleLine(value);
            if (!string.IsNullOrWhiteSpace(clean))
            {
                return clean;
            }
        }

        return AppDisplayName;
    }

    private static string CompactSingleLine(string? value)
    {
        return string.Join(" ", (value ?? "")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CompactDeliveryTileSubtitle(TableTile tile)
    {
        if (string.Equals(tile.ExternalSource, "IFOOD", StringComparison.OrdinalIgnoreCase))
        {
            if (IsIFoodTakeout(tile))
            {
                return "iFood retirada";
            }

            if (string.Equals(tile.ExternalOrderTiming, "SCHEDULED", StringComparison.OrdinalIgnoreCase))
            {
                return "iFood agendado";
            }

            return IsIFoodShipment(tile.ExternalDeliveredBy) ? "iFood entrega" : "iFood loja";
        }

        if (string.Equals(tile.ExternalSource, "CARDAPIO_ONLINE", StringComparison.OrdinalIgnoreCase))
        {
            return PublicMenuOrderTypeLabel(tile.ExternalOrderType) switch
            {
                "MESA/LOCAL" => "Cardapio mesa",
                "RETIRADA" => "Cardapio retirada",
                "ENTREGA" => "Cardapio entrega",
                _ => "Cardapio"
            };
        }

        var detail = CompactSingleLine(tile.Detail);
        if (detail.Contains("WHATSAPP", StringComparison.OrdinalIgnoreCase))
        {
            return "WhatsApp";
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            return string.IsNullOrWhiteSpace(tile.CustomerName) ? "Delivery" : CompactTileText(tile.CustomerName, 15);
        }

        return CompactTileText(detail, 15);
    }

    private static string CompactTileText(string value, int maxLength)
    {
        var clean = CompactSingleLine(value);
        if (clean.Length <= maxLength)
        {
            return clean;
        }

        return clean[..maxLength].TrimEnd();
    }

    private Task<AdminPagBankConnectResult> StartPagBankConnectAsync()
    {
        var payload = FillAdminPayload(new AdminClientPayload(), "pagbank.connect.start");
        return PostPagBankOperationAsync<AdminPagBankConnectResult>(
            "/connect/start",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminPagBankConnectionStatusResult> FetchPagBankConnectionStatusAsync()
    {
        var payload = FillAdminPayload(new AdminClientPayload(), "pagbank.status");
        return PostPagBankOperationAsync<AdminPagBankConnectionStatusResult>(
            "/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminMercadoPagoResult> SelectPagBankTerminalAsync(string terminalId, string terminalLabel, string comPort)
    {
        var payload = FillAdminPayload(new AdminPagBankTerminalPayload
        {
            TerminalId = terminalId,
            TerminalLabel = terminalLabel,
            ComPort = comPort
        }, "pagbank.terminal.select");
        return PostPagBankOperationAsync<AdminMercadoPagoResult>(
            "/terminal/select",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private Task<AdminPagBankWebChargeResult> CreatePagBankWebChargeAsync(decimal amount, string method, string localReference, string description, string payer)
    {
        var payload = FillAdminPayload(new AdminPagBankWebChargePayload
        {
            Amount = MercadoPagoAmountValue(amount),
            Method = method,
            LocalReference = localReference,
            Description = description,
            PayerName = payer,
            PayerTaxId = CurrentBoard?.CustomerCpf ?? "",
            PayerPhone = CurrentBoard?.Phone ?? "",
            Items = BuildMercadoPagoChargeItems()
        }, "pagbank.web.charge");
        return PostPagBankOperationAsync<AdminPagBankWebChargeResult>(
            "/web/charge",
            payload,
            TimeSpan.FromSeconds(18));
    }

    private Task<AdminMercadoPagoPointStatusResult> FetchPagBankWebStatusAsync(string attemptId, string orderId, string localReference)
    {
        var payload = FillAdminPayload(new AdminMercadoPagoPointStatusPayload
        {
            AttemptId = attemptId,
            OrderId = orderId,
            LocalReference = localReference
        }, "pagbank.web.status");
        return PostPagBankOperationAsync<AdminMercadoPagoPointStatusResult>(
            "/web/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private void ApplyPagBankStatus(AdminPagBankConnectionStatusResult result)
    {
        _appSettings.PagBank ??= new PagBankPaymentSettings();
        _appSettings.PagBank.Connected = result.Ok && result.Connected;
        _appSettings.PagBank.AccountId = result.AccountId ?? "";
        _appSettings.PagBank.LastError = result.LastError ?? "";
        if (!string.IsNullOrWhiteSpace(result.SelectedTerminalId))
        {
            _appSettings.PagBank.DefaultTerminalId = result.SelectedTerminalId.Trim();
            _appSettings.PagBank.DefaultTerminalLabel = string.IsNullOrWhiteSpace(result.SelectedTerminalLabel)
                ? result.SelectedTerminalId.Trim()
                : result.SelectedTerminalLabel.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.ComPort))
        {
            _appSettings.PagBank.PlugPagComPort = result.ComPort.Trim().ToUpperInvariant();
        }

        if (DateTime.TryParse(result.LastSyncAt, Brazil, DateTimeStyles.AssumeLocal, out var lastSync))
        {
            _appSettings.PagBank.LastSyncAt = lastSync;
        }
    }

    private async Task<PaymentLine?> ProcessIntegratedPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        var mercadoPagoEnabled = _appSettings.MercadoPago?.Enabled == true;
        var pagBankEnabled = PagBankIntegrationAvailable && _appSettings.PagBank?.Enabled == true;
        if (!mercadoPagoEnabled && !pagBankEnabled)
        {
            return null;
        }

        var provider = ChoosePaymentProvider(method, mercadoPagoEnabled, pagBankEnabled, owner);
        return provider switch
        {
            "PAGBANK" => await ProcessPagBankPaymentAsync(method, amount, payer, owner),
            "MERCADOPAGO" => await ProcessMercadoPagoPaymentAsync(method, amount, payer, owner),
            _ => null
        };
    }

    private string ChoosePaymentProvider(string method, bool mercadoPagoEnabled, bool pagBankEnabled, Window owner)
    {
        if (mercadoPagoEnabled && !pagBankEnabled)
        {
            return "MERCADOPAGO";
        }

        if (pagBankEnabled && !mercadoPagoEnabled)
        {
            return "PAGBANK";
        }

        var result = "";
        var dialog = CreateDialog("Escolher integracao", 500, 330);
        dialog.Owner = owner;
        dialog.ResizeMode = ResizeMode.NoResize;

        var mp = DialogButton("Mercado Pago", "#0B3A52");
        var pg = DialogButton("PagBank", "#08A99B");
        var cancel = DialogButton("Cancelar", "#5B6B7A");
        foreach (var button in new[] { pg, mp, cancel })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }

        pg.Click += (_, _) =>
        {
            result = "PAGBANK";
            dialog.Close();
        };
        mp.Click += (_, _) =>
        {
            result = "MERCADOPAGO";
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        var panel = DialogPanel();
        panel.Children.Add(SectionTitle($"Como cobrar {method}?"));
        panel.Children.Add(new TextBlock
        {
            Text = "As duas integracoes estao ativas. Escolha por onde esta venda sera cobrada.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });
        panel.Children.Add(pg);
        panel.Children.Add(mp);
        panel.Children.Add(cancel);
        dialog.Content = panel;
        dialog.ShowDialog();
        return result;
    }

    private async Task<PaymentLine?> ProcessPagBankPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        if (!PagBankIntegrationAvailable)
        {
            System.Windows.MessageBox.Show(owner, "PagBank esta em breve nesta versao. Use Mercado Pago, Pix manual ou outra forma de pagamento por enquanto.", "PagBank em breve", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        if (!_appSettings.PagBank.Enabled)
        {
            return null;
        }

        if (!_appSettings.PagBank.Connected)
        {
            System.Windows.MessageBox.Show(owner, "Conecte a conta PagBank em Config antes de cobrar.", "PagBank", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var mode = ChoosePagBankCollectionMode(method, owner);
        if (string.IsNullOrWhiteSpace(mode))
        {
            return null;
        }

        return mode == "PLUGPAG"
            ? await ProcessPagBankPlugPagPaymentAsync(method, amount, payer, owner)
            : await ProcessPagBankWebPaymentAsync(method, amount, payer, owner);
    }

    private string ChoosePagBankCollectionMode(string method, Window owner)
    {
        var canUsePlugPag = _appSettings.PagBank.PlugPagEnabled
            && method is "CREDITO" or "DEBITO"
            && !string.IsNullOrWhiteSpace(_appSettings.PagBank.PlugPagComPort);
        if (!canUsePlugPag)
        {
            return "WEB";
        }

        var result = "";
        var dialog = CreateDialog("PagBank", 500, 360);
        dialog.Owner = owner;
        dialog.ResizeMode = ResizeMode.NoResize;

        var terminal = DialogButton("Enviar para maquininha", "#08A99B");
        var online = DialogButton(method == "PIX" ? "Gerar QR Pix" : "Gerar link", "#2D73B9");
        var cancel = DialogButton("Cancelar", "#5B6B7A");
        foreach (var button in new[] { terminal, online, cancel })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }

        terminal.Click += (_, _) =>
        {
            result = "PLUGPAG";
            dialog.Close();
        };
        online.Click += (_, _) =>
        {
            result = "WEB";
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();

        var panel = DialogPanel();
        panel.Children.Add(SectionTitle(method == "PIX" ? "Como cobrar este Pix?" : "Como cobrar este cartao?"));
        panel.Children.Add(new TextBlock
        {
            Text = method == "PIX"
                ? "Pix no PagBank sai por QR/link online. A integracao PlugPag desta versao fica para credito/debito na Moderninha pareada por Bluetooth."
                : $"PlugPag envia o valor para a Moderninha pareada na porta {_appSettings.PagBank.PlugPagComPort}. O online gera link/checkout PagBank.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });
        if (method is "CREDITO" or "DEBITO")
        {
            panel.Children.Add(terminal);
        }
        panel.Children.Add(online);
        panel.Children.Add(cancel);
        dialog.Content = panel;
        dialog.ShowDialog();
        return result;
    }

    private async Task<PaymentLine?> ProcessPagBankPlugPagPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        var comPort = CompactSingleLine(_appSettings.PagBank.PlugPagComPort).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(comPort))
        {
            System.Windows.MessageBox.Show(owner, "Informe a porta Bluetooth COM da Moderninha em Config > PagBank.", "PagBank PlugPag", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var localReference = $"BLV-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
        var cancelled = false;
        var waitDialog = CreateDialog("PagBank PlugPag", 470, 285);
        waitDialog.Owner = owner;
        waitDialog.ResizeMode = ResizeMode.NoResize;
        var statusText = new TextBlock
        {
            Text = $"Enviando {Money(amount)} para a Moderninha em {comPort}.",
            Foreground = Solid("#071A2C"),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        var detailText = new TextBlock
        {
            Text = "Aguarde o cliente passar o cartao. O PDV so registra quando o PlugPag retornar aprovado.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 16)
        };
        var stop = DialogButton("Parar espera", "#5B6B7A");
        stop.HorizontalAlignment = HorizontalAlignment.Stretch;
        stop.Click += (_, _) =>
        {
            cancelled = true;
            waitDialog.Close();
        };
        var panel = DialogPanel();
        panel.Children.Add(SectionTitle(method == "CREDITO" ? "Credito na maquininha" : "Debito na maquininha"));
        panel.Children.Add(statusText);
        panel.Children.Add(detailText);
        panel.Children.Add(stop);
        waitDialog.Content = panel;
        waitDialog.Show();

        PlugPagChargeResult nativeResult;
        try
        {
            nativeResult = await Task.Run(() => ExecutePlugPagCharge(method, amount, localReference, comPort));
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException or SEHException or InvalidOperationException)
        {
            waitDialog.Close();
            System.Windows.MessageBox.Show(owner, $"Nao consegui iniciar o PlugPag: {ex.Message}", "PagBank PlugPag", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        if (waitDialog.IsVisible)
        {
            waitDialog.Close();
        }

        if (cancelled)
        {
            return null;
        }

        if (!nativeResult.Approved)
        {
            System.Windows.MessageBox.Show(owner, nativeResult.Message, "PagBank PlugPag", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return new PaymentLine
        {
            Payer = string.IsNullOrWhiteSpace(payer) ? "Cliente" : payer.Trim(),
            Method = $"PAGBANK {method}",
            Amount = amount,
            TenderedAmount = amount,
            ChangeAmount = 0,
            When = DateTime.Now
        };
    }

    private PlugPagChargeResult ExecutePlugPagCharge(string method, decimal amount, string localReference, string comPort)
    {
        var cents = Math.Max(1, (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
        var amountBytes = Encoding.ASCII.GetBytes($"{cents:000000000000}");
        var referenceBytes = Encoding.ASCII.GetBytes(CompactSingleLine(localReference)[..Math.Min(10, CompactSingleLine(localReference).Length)]);
        var portBytes = Encoding.ASCII.GetBytes(comPort);
        var appName = Encoding.ASCII.GetBytes("BalcaoLivre");
        var version = Encoding.ASCII.GetBytes(GetAppVersion());
        var paymentMethod = method == "DEBITO" ? 2 : 1;
        var result = new PlugPagTransactionResult();
        var size = Marshal.SizeOf<PlugPagTransactionResult>();
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(result, ptr, false);
            SetVersionName(appName, version);
            var init = InitBTConnection(portBytes);
            if (init != 0)
            {
                return PlugPagChargeResult.Fail($"Moderninha nao conectou na porta {comPort}. Codigo: {init}.");
            }

            var response = SimplePaymentTransaction(paymentMethod, 1, 1, amountBytes, referenceBytes, ptr);
            result = Marshal.PtrToStructure<PlugPagTransactionResult>(ptr);
            return response == 0
                ? PlugPagChargeResult.Ok(result)
                : PlugPagChargeResult.Fail($"Pagamento negado ou interrompido. Codigo: {response}. {CompactSingleLine(result.Message)}");
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private async Task<PaymentLine?> ProcessPagBankWebPaymentAsync(string method, decimal amount, string payer, Window owner)
    {
        var localReference = $"BLV-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
        var isPix = method == "PIX";
        var charge = await CreatePagBankWebChargeAsync(
            amount,
            method,
            localReference,
            BuildMercadoPagoChargeDescription(method, amount),
            payer);

        if (!charge.Ok)
        {
            System.Windows.MessageBox.Show(owner, string.IsNullOrWhiteSpace(charge.Message) ? "PagBank recusou a cobranca." : charge.Message, "PagBank", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var displayPayload = isPix
            ? TextOrDefault(charge.QrCode, charge.PaymentUrl)
            : charge.PaymentUrl;
        var qrSource = isPix
            ? TryCreateBitmapFromBase64(charge.QrCodeBase64) ?? TryCreateQrBitmap(displayPayload, 8)
            : TryCreateQrBitmap(displayPayload, 8);

        if (string.IsNullOrWhiteSpace(displayPayload) || qrSource is null)
        {
            System.Windows.MessageBox.Show(owner, "PagBank gerou a cobranca, mas nao retornou QR/link para exibir.", "PagBank", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var cancelled = false;
        var paid = false;
        var lastStatus = charge.Status;
        var waitDialog = CreateDialog(isPix ? "Pix PagBank" : "Pagamento PagBank", 620, isPix ? 760 : 690);
        waitDialog.Owner = owner;
        waitDialog.ResizeMode = ResizeMode.NoResize;

        var title = new TextBlock
        {
            Text = isPix ? "Mostre o QR Pix PagBank para o cliente" : "Mostre ou abra o link PagBank para o cliente pagar",
            Foreground = Solid("#071A2C"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        var subtitle = new TextBlock
        {
            Text = isPix
                ? "O PDV fecha a venda somente quando o PagBank confirmar o Pix."
                : "O cliente paga no checkout PagBank. O PDV fecha a venda somente quando o pagamento for aprovado.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 14)
        };
        var image = new System.Windows.Controls.Image
        {
            Source = qrSource,
            Width = 330,
            Height = 330,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        var codeBox = new TextBox
        {
            Text = displayPayload,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = isPix ? 92 : 62,
            Margin = new Thickness(0, 0, 0, 12),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var statusText = new TextBlock
        {
            Text = $"Aguardando pagamento de {Money(amount)}...",
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 12)
        };
        var copy = DialogButton(isPix ? "Copiar Pix" : "Copiar link", "#5B6B7A");
        var open = DialogButton(isPix ? "Abrir Pix" : "Abrir link", "#2D73B9");
        var cancel = DialogButton("Parar espera", "#5B6B7A");
        foreach (var button in new[] { copy, open, cancel })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }

        copy.Click += (_, _) =>
        {
            try
            {
                System.Windows.Clipboard.SetText(displayPayload);
                statusText.Text = isPix ? "Pix copia-e-cola copiado." : "Link copiado.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or ExternalException)
            {
                statusText.Text = "Nao foi possivel copiar agora.";
            }
        };
        open.Click += (_, _) =>
        {
            var url = TextOrDefault(charge.PaymentUrl, charge.TicketUrl);
            if (string.IsNullOrWhiteSpace(url))
            {
                statusText.Text = "Sem link para abrir. Use o QR/copia-e-cola.";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                statusText.Text = "Nao foi possivel abrir o link.";
            }
        };
        cancel.Click += (_, _) =>
        {
            cancelled = true;
            waitDialog.Close();
        };
        waitDialog.Closed += (_, _) => cancelled = !paid;

        var buttons = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 0) };
        buttons.Children.Add(copy);
        buttons.Children.Add(open);
        buttons.Children.Add(cancel);

        var panel = DialogPanel();
        panel.Children.Add(title);
        panel.Children.Add(subtitle);
        panel.Children.Add(image);
        panel.Children.Add(DialogLabel(isPix ? "Pix copia-e-cola" : "Link de pagamento"));
        panel.Children.Add(codeBox);
        panel.Children.Add(statusText);
        panel.Children.Add(buttons);
        waitDialog.Content = panel;
        waitDialog.Show();

        for (var attempt = 0; attempt < 72 && !cancelled; attempt++)
        {
            await Task.Delay(attempt == 0 ? 1500 : 2500);
            if (cancelled)
            {
                break;
            }

            var status = await FetchPagBankWebStatusAsync(charge.AttemptId, charge.OrderId, charge.LocalReference);
            if (!status.Ok)
            {
                statusText.Text = string.IsNullOrWhiteSpace(status.Message)
                    ? $"Aguardando retorno... ultima situacao: {TextOrDefault(lastStatus, "CRIADO")}"
                    : status.Message;
                continue;
            }

            lastStatus = TextOrDefault(status.Status, lastStatus);
            statusText.Text = $"Situacao: {TextOrDefault(lastStatus, "aguardando")} | tentativa {attempt + 1}/72";
            if (status.Paid)
            {
                paid = true;
                waitDialog.Close();
                return new PaymentLine
                {
                    Payer = string.IsNullOrWhiteSpace(payer) ? "Cliente" : payer.Trim(),
                    Method = isPix ? "PAGBANK PIX" : $"PAGBANK LINK {method}",
                    Amount = amount,
                    TenderedAmount = amount,
                    ChangeAmount = 0,
                    When = DateTime.Now
                };
            }

            if (IsMercadoPagoFinalFailure(lastStatus))
            {
                waitDialog.Close();
                System.Windows.MessageBox.Show(owner, $"Pagamento nao aprovado: {lastStatus}", "PagBank", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        if (!paid && !cancelled)
        {
            waitDialog.Close();
            System.Windows.MessageBox.Show(owner, "Ainda nao houve confirmacao do PagBank. Confira a conta antes de tentar de novo.", "PagBank", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        return null;
    }

    private async Task<AdminSupportResult> SendAdminSupportAsync(string category, string priority, string message)
    {
        if (!_appSettings.AdminSyncEnabled)
        {
            return AdminSupportResult.Fail("Sincronizacao admin desligada nas configuracoes.");
        }

        var endpoints = BuildAdminApiUrisWithLegacyFallback("/api/app/support");
        if (endpoints.Count == 0)
        {
            return AdminSupportResult.Fail("URL do admin invalida.");
        }

        var basePayload = CreateAdminClientPayload("support.request", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        var payload = new AdminSupportPayload
        {
            EventName = basePayload.EventName,
            LicenseKey = basePayload.LicenseKey,
            MachineHash = basePayload.MachineHash,
            MachineCode = basePayload.MachineCode,
            AppVersion = basePayload.AppVersion,
            LocalExpiresAt = basePayload.LocalExpiresAt,
            LocalPlan = basePayload.LocalPlan,
            Profile = basePayload.Profile,
            Settings = basePayload.Settings,
            Metrics = basePayload.Metrics,
            Category = category,
            Priority = priority,
            Message = message,
            LocalWhen = DateTimeOffset.Now
        };

        AdminSupportResult? lastResult = null;
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(endpoint, content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdminSupportResult>(json, JsonOptions);
                if (result is not null)
                {
                    lastResult = result;
                    if (result.Ok || !IsAdminRouteMissing(result.Message))
                    {
                        return result;
                    }

                    continue;
                }

                return response.IsSuccessStatusCode
                    ? AdminSupportResult.OkResult("", "Suporte enviado para o admin.")
                    : AdminSupportResult.Fail("Admin recusou o suporte, mas nao retornou detalhes.");
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
            {
                Debug.WriteLine($"Admin support sync failed ({endpoint}): {ex.Message}");
            }
        }

        return lastResult is not null && !IsAdminRouteMissing(lastResult.Message)
            ? lastResult
            : AdminSupportResult.Fail("Admin indisponivel agora. Verifique a internet ou a URL do admin.");
    }

    private async Task<AdminSupportListResult> FetchAdminSupportTicketsAsync()
    {
        if (!_appSettings.AdminSyncEnabled || string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            return new AdminSupportListResult();
        }

        var endpoints = BuildAdminApiUrisWithLegacyFallback("/api/app/support/list");
        if (endpoints.Count == 0)
        {
            return new AdminSupportListResult { Ok = false, Message = "URL do admin invalida." };
        }

        var payload = CreateAdminClientPayload("support.list", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        AdminSupportListResult? lastResult = null;
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(endpoint, content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdminSupportListResult>(json, JsonOptions) ?? new AdminSupportListResult { Ok = response.IsSuccessStatusCode };
                lastResult = result;
                if (result.Ok || !IsAdminRouteMissing(result.Message))
                {
                    return result;
                }
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
            {
                Debug.WriteLine($"Admin support list failed ({endpoint}): {ex.Message}");
            }
        }

        return lastResult is not null && !IsAdminRouteMissing(lastResult.Message)
            ? lastResult
            : new AdminSupportListResult { Ok = false, Message = "Nao foi possivel atualizar o suporte agora." };
    }

    private async Task<AdminSupportResult> SendAdminSupportMessageAsync(string ticketId, string message)
    {
        var endpoints = BuildAdminApiUrisWithLegacyFallback($"/api/app/support/{Uri.EscapeDataString(ticketId)}/message");
        if (endpoints.Count == 0)
        {
            return AdminSupportResult.Fail("URL do admin invalida.");
        }

        var basePayload = CreateAdminClientPayload("support.message", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        var payload = new AdminSupportMessagePayload
        {
            EventName = basePayload.EventName,
            LicenseKey = basePayload.LicenseKey,
            MachineHash = basePayload.MachineHash,
            MachineCode = basePayload.MachineCode,
            AppVersion = basePayload.AppVersion,
            LocalExpiresAt = basePayload.LocalExpiresAt,
            LocalPlan = basePayload.LocalPlan,
            Profile = basePayload.Profile,
            Settings = basePayload.Settings,
            Metrics = basePayload.Metrics,
            Message = message,
            LocalWhen = DateTimeOffset.Now
        };

        AdminSupportResult? lastResult = null;
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(endpoint, content);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AdminSupportResult>(json, JsonOptions)
                    ?? (response.IsSuccessStatusCode
                        ? AdminSupportResult.OkResult(ticketId, "Mensagem enviada.")
                        : AdminSupportResult.Fail("Admin recusou a mensagem."));
                lastResult = result;
                if (result.Ok || !IsAdminRouteMissing(result.Message))
                {
                    return result;
                }
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
            {
                Debug.WriteLine($"Admin support message failed ({endpoint}): {ex.Message}");
            }
        }

        return lastResult is not null && !IsAdminRouteMissing(lastResult.Message)
            ? lastResult
            : AdminSupportResult.Fail("Admin indisponivel agora. A mensagem nao foi enviada.");
    }

    private static bool IsAdminRouteMissing(string? message)
    {
        return (message ?? "").Contains("Rota de licenca nao encontrada", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PollSupportNotificationsAsync()
    {
        if (_supportPollRunning || !_appSettings.AdminSyncEnabled || string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            return;
        }

        _supportPollRunning = true;
        try
        {
            var result = await FetchAdminSupportTicketsAsync();
            if (!result.Ok)
            {
                return;
            }

            var latestAdminMessage = result.Tickets
                .SelectMany(ticket => ticket.Messages)
                .Where(message => string.Equals(message.Sender, "admin", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(message => message.When)
                .FirstOrDefault();
            if (latestAdminMessage is null)
            {
                return;
            }

            if (_lastSupportAdminMessageAt.HasValue && latestAdminMessage.When > _lastSupportAdminMessageAt.Value)
            {
                ShowToast("Suporte respondeu", "Abra o botao Suporte para ver a conversa.", "?", "#08A99B", "#E6FBF8");
                SetStatus("Nova resposta do suporte recebida.");
            }

            _lastSupportAdminMessageAt = latestAdminMessage.When;
        }
        finally
        {
            _supportPollRunning = false;
        }
    }

    private async Task PollPublicMenuOrdersAsync(bool force = false)
    {
        if (_publicMenuOrderPollRunning || !_appSettings.AdminSyncEnabled || string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            return;
        }

        _publicMenuOrderPollRunning = true;
        try
        {
            var result = await FetchPublicMenuOrdersAsync();
            if (!result.Ok || result.Orders.Count == 0)
            {
                return;
            }

            var imported = 0;
            var importedTiles = new List<TableTile>();
            foreach (var order in result.Orders.Where(order => !string.IsNullOrWhiteSpace(order.Id)))
            {
                var existing = FindPublicMenuOrder(order.Id);
                if (existing is not null)
                {
                    await AckPublicMenuOrderAsync(order.Id, existing.Number);
                    continue;
                }

                var tile = CreatePublicMenuDelivery(order);
                importedTiles.Add(tile);
                imported++;
                await AckPublicMenuOrderAsync(order.Id, tile.Number);
            }

            if (imported <= 0)
            {
                return;
            }

            SaveStore();
            RefreshBoardForMode();
            if (string.Equals(CurrentMode, "Delivery", StringComparison.OrdinalIgnoreCase) && BoardTiles.Count > 0)
            {
                SelectTable(BoardTiles.Count - 1, saveCurrent: false);
            }

            var latest = importedTiles.Last();
            var message = imported == 1
                ? $"Pedido do cardapio recebido: {latest.Number} - {Money(latest.Total)}."
                : $"{imported} pedidos do cardapio recebidos. Ultimo: {latest.Number} - {Money(latest.Total)}.";
            NotifyPublicMenuOrdersReceived(importedTiles);
            SetStatus(message);
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
        {
            if (DateTime.Now - _lastPublicMenuOrderSyncErrorAt > TimeSpan.FromMinutes(3))
            {
                _lastPublicMenuOrderSyncErrorAt = DateTime.Now;
                SetStatus($"Cardapio online indisponivel agora: {ex.Message}");
            }
        }
        finally
        {
            _publicMenuOrderPollRunning = false;
        }
    }

    private void NotifyPublicMenuOrdersReceived(IReadOnlyList<TableTile> orders)
    {
        if (orders.Count == 0)
        {
            return;
        }

        var latest = orders[^1];
        var message = orders.Count == 1
            ? $"{latest.Number} - {latest.CustomerName} - {Money(latest.Total)}"
            : $"{orders.Count} pedidos do cardapio. Ultimo: {latest.Number} - {Money(latest.Total)}";
        _suppressNextToastSound = true;
        ShowToast("Novo pedido do cardapio", message, "QR", "#08A99B", "#E6FBF8");
        PlayIFoodOrderSound();
        VibrateInApp();
        Dispatcher.BeginInvoke(() => ShowPublicMenuOrderAlertDialog(latest, orders.Count), DispatcherPriority.Background);
    }

    private void ShowPublicMenuOrderAlertDialog(TableTile order, int batchCount)
    {
        if (!IsPublicMenuDeliveryBoard(order))
        {
            SetStatus("Pedido do cardapio invalido para abrir alerta.");
            return;
        }

        var dialog = CreateDialog("Novo pedido do cardapio", 820, 660);
        dialog.ResizeMode = ResizeMode.NoResize;
        dialog.Topmost = true;
        dialog.Loaded += (_, _) =>
        {
            dialog.Activate();
            dialog.Topmost = false;
        };
        dialog.Closed += (_, _) => StopIFoodOrderSound();

        var statusText = new TextBlock
        {
            Text = $"Status: {order.Status} | Tipo: {PublicMenuOrderTypeLabel(order.ExternalOrderType)}",
            Foreground = Solid("#5B6B7A"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var actionMessage = new TextBlock
        {
            Text = batchCount > 1
                ? $"{batchCount} pedidos chegaram juntos. Este alerta abriu o mais recente."
                : "Pedido importado no Delivery. Confira e avance a producao.",
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };

        void SetOrderStatus(string status, string message)
        {
            StopIFoodOrderSound();
            order.Status = status;
            order.RefreshVisualState();
            SaveStore();
            QueuePublicMenuOrderStatusSync(order);
            OpenPublicMenuDeliveryOrder(order);
            SetStatus(message);
            dialog.Close();
        }

        var openDelivery = DialogButton("Abrir no Delivery", "#0B3A52");
        var prepare = DialogButton("Preparar pedido", "#08A99B");
        var ready = DialogButton("Marcar pronto", "#99620D");
        var close = DialogButton("Fechar alerta", "#5B6B7A");

        openDelivery.Click += (_, _) =>
        {
            StopIFoodOrderSound();
            OpenPublicMenuDeliveryOrder(order);
            dialog.Close();
        };
        prepare.Click += (_, _) => SetOrderStatus("PREPARO", $"Pedido do cardapio {order.Number} em preparo.");
        ready.Click += (_, _) => SetOrderStatus("PRONTO", $"Pedido do cardapio {order.Number} marcado como pronto.");
        close.Click += (_, _) =>
        {
            StopIFoodOrderSound();
            dialog.Close();
        };

        foreach (var button in new[] { openDelivery, prepare, ready, close })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
            button.MinHeight = 52;
            button.Margin = new Thickness(0, 0, 10, 10);
        }

        var header = new Border
        {
            Background = Solid("#E6FBF8"),
            BorderBrush = Solid("#08A99B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 16, 18, 16),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "NOVO PEDIDO DO CARDAPIO",
                        Foreground = Solid("#08A99B"),
                        FontSize = 30,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"{order.Number}  |  {order.CustomerName}  |  {Money(order.Total)}",
                        Foreground = Solid("#071A2C"),
                        FontSize = 17,
                        FontWeight = FontWeights.SemiBold,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0)
                    },
                    statusText
                }
            }
        };

        var items = new ListBox
        {
            ItemsSource = order.Lines
                .Where(line => !string.Equals(line.Code, "WEB-TOTAL", StringComparison.OrdinalIgnoreCase))
                .Select(line =>
                {
                    var note = string.IsNullOrWhiteSpace(line.Note) ? "" : $" | Obs item: {line.Note.Trim()}";
                    return $"{line.Quantity:N0}x {line.Name}   {Money(line.Total)}{note}";
                })
                .ToList(),
            MinHeight = 120,
            MaxHeight = 170,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var details = BorderCard();
        details.Margin = new Thickness(0, 12, 0, 0);
        details.Child = new StackPanel
        {
            Children =
            {
                SectionTitle("Dados do pedido"),
                new TextBlock { Text = $"Pedido online: {EmptyDash(order.ExternalDisplayId)} | ID: {EmptyDash(order.ExternalOrderId)}", Foreground = Solid("#071A2C"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Cliente: {order.CustomerName}", Foreground = Solid("#071A2C"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Contato: {EmptyDash(order.Phone)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Endereco: {EmptyDash(order.Address)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Bairro/referencia: {EmptyDash(order.District)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Obs: {EmptyDash(order.Notes)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 4, 0, 8), TextWrapping = TextWrapping.Wrap },
                items
            }
        };

        var buttons = new UniformGrid { Columns = 4 };
        buttons.Children.Add(openDelivery);
        buttons.Children.Add(prepare);
        buttons.Children.Add(ready);
        buttons.Children.Add(close);

        var panel = DialogPanel();
        panel.Children.Add(header);
        panel.Children.Add(details);
        panel.Children.Add(actionMessage);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            PanningMode = PanningMode.VerticalOnly
        });

        var footer = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(18, 12, 8, 8),
            Child = buttons
        };
        Grid.SetRow(footer, 1);
        root.Children.Add(footer);

        dialog.Content = root;
        dialog.ShowDialog();
    }

    private void OpenPublicMenuDeliveryOrder(TableTile order)
    {
        ModeList.SelectedItem = "Delivery";
        RefreshBoardForMode();
        var index = BoardTiles.IndexOf(order);
        if (index < 0)
        {
            index = BoardTiles
                .Select((tile, tileIndex) => new { tile, tileIndex })
                .FirstOrDefault(item =>
                    string.Equals(item.tile.ExternalSource, order.ExternalSource, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.tile.ExternalOrderId, order.ExternalOrderId, StringComparison.OrdinalIgnoreCase))
                ?.tileIndex ?? -1;
        }

        if (index >= 0)
        {
            SelectTable(index, saveCurrent: false);
        }
    }

    private async Task<AdminPublicMenuOrdersResult> FetchPublicMenuOrdersAsync()
    {
        var endpoint = BuildAdminApiUri("/api/app/menu/orders/pending");
        if (endpoint is null)
        {
            return new AdminPublicMenuOrdersResult { Ok = false, Message = "URL do admin invalida." };
        }

        var basePayload = CreateAdminClientPayload("menu.orders.poll", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        var payload = new AdminPublicMenuOrdersPollPayload
        {
            EventName = basePayload.EventName,
            LicenseKey = basePayload.LicenseKey,
            MachineHash = basePayload.MachineHash,
            MachineCode = basePayload.MachineCode,
            AppVersion = basePayload.AppVersion,
            LocalExpiresAt = basePayload.LocalExpiresAt,
            LocalPlan = basePayload.LocalPlan,
            Profile = basePayload.Profile,
            Settings = basePayload.Settings,
            Metrics = basePayload.Metrics,
            Limit = 25
        };

        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(endpoint, content);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AdminPublicMenuOrdersResult>(json, JsonOptions)
            ?? new AdminPublicMenuOrdersResult { Ok = response.IsSuccessStatusCode };
    }

    private async Task<bool> AckPublicMenuOrderAsync(string orderId, string pdvOrderId, string status = "IMPORTADO")
    {
        var endpoint = BuildAdminApiUri("/api/app/menu/orders/ack");
        if (endpoint is null)
        {
            return false;
        }

        var basePayload = CreateAdminClientPayload("menu.orders.ack", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
        var payload = new AdminPublicMenuOrderAckPayload
        {
            EventName = basePayload.EventName,
            LicenseKey = basePayload.LicenseKey,
            MachineHash = basePayload.MachineHash,
            MachineCode = basePayload.MachineCode,
            AppVersion = basePayload.AppVersion,
            LocalExpiresAt = basePayload.LocalExpiresAt,
            LocalPlan = basePayload.LocalPlan,
            Profile = basePayload.Profile,
            Settings = basePayload.Settings,
            Metrics = basePayload.Metrics,
            OrderId = orderId,
            PdvOrderId = pdvOrderId,
            Status = NormalizePublicMenuOrderStatus(status)
        };

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
        {
            Debug.WriteLine($"Public menu order ack failed: {ex.Message}");
            return false;
        }
    }

    private void QueuePublicMenuOrderStatusSync(TableTile? order)
    {
        if (order is null)
        {
            return;
        }

        if (!IsPublicMenuDeliveryBoard(order))
        {
            return;
        }

        _ = SyncPublicMenuOrderStatusAsync(order);
    }

    private async Task SyncPublicMenuOrderStatusAsync(TableTile order)
    {
        var synced = await AckPublicMenuOrderAsync(order.ExternalOrderId, order.Number, order.Status);
        if (!synced)
        {
            Debug.WriteLine($"Public menu order status sync failed: {order.ExternalOrderId} -> {order.Status}");
        }
    }

    private TableTile? FindPublicMenuOrder(string orderId)
    {
        return DeliveryTiles.FirstOrDefault(tile =>
            string.Equals(tile.ExternalSource, "CARDAPIO_ONLINE", StringComparison.OrdinalIgnoreCase)
            && string.Equals(tile.ExternalOrderId, orderId, StringComparison.OrdinalIgnoreCase));
    }

    private TableTile CreatePublicMenuDelivery(AdminPublicMenuOrderSnapshot order)
    {
        var typeLabel = PublicMenuOrderTypeLabel(order.OrderType);
        var number = NextDeliveryNumber();
        var createdAt = LocalTimeOrNull(order.CreatedAt) ?? DateTime.Now;
        var tile = new TableTile
        {
            Number = number,
            Kind = "DELIVERY",
            Status = "NOVO",
            CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "CLIENTE CARDAPIO" : order.CustomerName.Trim().ToUpperInvariant(),
            CustomerCpf = order.CustomerDocument.Trim(),
            Phone = order.CustomerPhone.Trim(),
            Address = order.Address.Trim(),
            District = string.IsNullOrWhiteSpace(order.District) ? order.Reference.Trim() : order.District.Trim(),
            Detail = $"CARDAPIO {typeLabel}".Trim(),
            Notes = BuildPublicMenuOrderNotes(order),
            ExternalSource = "CARDAPIO_ONLINE",
            ExternalOrderId = order.Id,
            ExternalDisplayId = ShortPublicMenuOrderId(order.Id),
            ExternalOrderType = order.OrderType,
            ExternalPaymentSummary = "Pagamento no atendimento",
            ExternalCreatedAt = createdAt,
            CreatedAt = createdAt
        };

        foreach (var item in order.Items.Where(item => !string.IsNullOrWhiteSpace(item.Name)))
        {
            var quantity = Math.Max(1, item.Quantity);
            var product = ResolvePublicMenuProduct(item);
            var code = product?.Code ?? NormalizeProductCode(item.Code);
            if (string.IsNullOrWhiteSpace(code))
            {
                code = "WEB";
            }

            var line = new TicketLine
            {
                Code = code,
                Name = product?.Name ?? item.Name.Trim().ToUpperInvariant(),
                Quantity = quantity,
                UnitPrice = item.Price,
                Note = item.Note.Trim(),
                Sector = product is null ? "COZINHA" : NormalizeProductDestination(product.Sector, "CAIXA")
            };
            tile.Lines.Add(line);

            if (product is not null)
            {
                product.StockQuantity -= quantity;
                product.SoldQuantity += quantity;
                product.StockHistory.Add(new StockMovement
                {
                    ProductCode = product.Code,
                    Type = "CARDAPIO",
                    Quantity = -quantity,
                    Reason = $"Pedido cardapio {number}",
                    When = DateTime.Now
                });
                QueueIFoodStockSync(product, $"Pedido cardapio {number}");
            }
        }

        if (order.DeliveryFee > 0)
        {
            tile.Lines.Add(new TicketLine
            {
                Code = "000020",
                Name = "TAXA ENTREGA",
                Quantity = 1,
                UnitPrice = order.DeliveryFee,
                Sector = "CAIXA"
            });
        }

        tile.Total = order.Total > 0 ? order.Total : tile.Lines.Sum(line => line.Total);
        var lineTotal = tile.Lines.Sum(line => line.Total);
        if (tile.Total > 0 && Math.Abs(tile.Total - lineTotal) >= 0.01m)
        {
            tile.Lines.Add(new TicketLine
            {
                Code = "WEB-TOTAL",
                Name = "AJUSTE TOTAL CARDAPIO",
                Quantity = 1,
                UnitPrice = tile.Total - lineTotal,
                Sector = "CAIXA"
            });
        }

        DeliveryTiles.Add(tile);
        ScheduleKitchenPrint(tile, tile.Lines);
        if (!string.IsNullOrWhiteSpace(tile.CustomerName))
        {
            UpsertCustomerRecord(tile.CustomerCpf, tile.CustomerName, tile.Phone, tile.Address, tile.District, tile.Notes);
        }

        if (_appSettings.AutoPrintDelivery)
        {
            _ = TryPrintTextToDefaultPrinter(BuildDeliveryPrintText(tile, tile.District, _appSettings.PrintLayout), $"Cardapio {tile.Number}", _appSettings.PrintLayout == "PEQUENO");
        }

        return tile;
    }

    private ProductTile? ResolvePublicMenuProduct(AdminPublicMenuOrderItemSnapshot item)
    {
        var rawCode = item.Code.Trim();
        var normalizedCode = string.IsNullOrWhiteSpace(rawCode) ? "" : NormalizeProductCode(rawCode);
        var normalizedName = NormalizeProductLookupText(item.Name);
        return Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedCode)
                && string.Equals(product.Code, normalizedCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(rawCode)
                && string.Equals(product.Code, rawCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedName)
                && string.Equals(NormalizeProductLookupText(product.Name), normalizedName, StringComparison.Ordinal));
    }

    private string NextDeliveryNumber()
    {
        var used = DeliveryTiles.Select(tile => tile.Number).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = DeliveryTiles.Count + 1; i < 99999; i++)
        {
            var number = $"D{i:00000}";
            if (!used.Contains(number))
            {
                return number;
            }
        }

        return $"D{DateTime.Now:HHmmss}";
    }

    private static string BuildPublicMenuOrderNotes(AdminPublicMenuOrderSnapshot order)
    {
        var lines = new List<string>
        {
            $"Pedido cardapio online {ShortPublicMenuOrderId(order.Id)}",
            $"Tipo: {PublicMenuOrderTypeLabel(order.OrderType)}"
        };

        if (!string.IsNullOrWhiteSpace(order.TableLabel)) lines.Add($"Mesa/comanda: {order.TableLabel}");
        if (!string.IsNullOrWhiteSpace(order.DesiredTime)) lines.Add($"Horario desejado: {order.DesiredTime}");
        if (!string.IsNullOrWhiteSpace(order.Reference)) lines.Add($"Referencia: {order.Reference}");
        if (!string.IsNullOrWhiteSpace(order.Notes)) lines.Add($"Obs: {order.Notes}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PublicMenuOrderTypeLabel(string value)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "DELIVERY" or "ENTREGA" => "ENTREGA",
            "PICKUP" or "RETIRADA" or "TAKEOUT" => "RETIRADA",
            "TABLE" or "MESA" or "LOCAL" => "MESA/LOCAL",
            _ => "PEDIDO"
        };
    }

    private static string NormalizePublicMenuOrderStatus(string value)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "RECEBIDO" => "IMPORTADO",
            "PREPARANDO" => "PREPARO",
            "FINALIZADO" => "ENTREGUE",
            _ when string.IsNullOrWhiteSpace(normalized) => "IMPORTADO",
            _ => normalized
        };
    }

    private static string ShortPublicMenuOrderId(string value)
    {
        var clean = (value ?? "").Replace("-", "", StringComparison.Ordinal).Trim();
        return clean.Length <= 8 ? clean.ToUpperInvariant() : clean[^8..].ToUpperInvariant();
    }

    private static bool IsPublicMenuDeliveryBoard(TableTile? order)
    {
        return order is not null
            && string.Equals(order.ExternalSource, "CARDAPIO_ONLINE", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(order.ExternalOrderId);
    }

    private Uri? BuildAdminApiUri(string path)
    {
        var baseUrl = (_appSettings.AdminApiUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultAdminApiUrl;
            _appSettings.AdminApiUrl = baseUrl;
        }

        return BuildAdminApiUri(baseUrl, path);
    }

    private List<Uri> BuildAdminApiUrisWithLegacyFallback(string path)
    {
        var uris = new List<Uri>();
        var primary = BuildAdminApiUri(path);
        if (primary is not null)
        {
            uris.Add(primary);
        }

        var configured = (_appSettings.AdminApiUrl ?? DefaultAdminApiUrl).Trim().TrimEnd('/');
        if (string.Equals(configured, DefaultAdminApiUrl, StringComparison.OrdinalIgnoreCase))
        {
            var fallback = BuildAdminApiUri(LegacyAdminApiUrl, path);
            if (fallback is not null && !uris.Any(uri => string.Equals(uri.ToString(), fallback.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                uris.Add(fallback);
            }
        }

        return uris;
    }

    private static Uri? BuildAdminApiUri(string baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private static Uri? BuildPaymentsApiUri(string path)
    {
        return BuildAdminApiUri(DefaultPaymentsApiUrl, path);
    }

    private Uri? BuildWhatsAppFunctionUri(string path)
    {
        var baseUrl = (_appSettings.WhatsAppFunctionUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultWhatsAppFunctionUrl;
            _appSettings.WhatsAppFunctionUrl = baseUrl;
        }

        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private AdminClientPayload CreateAdminClientPayload(string eventName, string licenseKey, DateTime? expiresAt, string plan)
    {
        var boards = Tables.Concat(DeliveryTiles).ToList();
        var openBoards = boards.Count(board =>
            !string.Equals(board.Status, "LIVRE", StringComparison.OrdinalIgnoreCase)
            && (board.Lines.Count > 0 || board.Payments.Count > 0 || board.Total > 0));

        return new AdminClientPayload
        {
            EventName = eventName,
            LicenseKey = NormalizeActivationKey(licenseKey),
            MachineHash = GetMachineFingerprint(),
            MachineCode = GetMachineCode(),
            AppVersion = GetAppVersion(),
            LocalExpiresAt = expiresAt,
            LocalPlan = plan,
            Profile = new AdminProfileSnapshot
            {
                Email = _profile.Email,
                OwnerName = _profile.OwnerName,
                BusinessName = _profile.BusinessName,
                LegalName = _profile.LegalName,
                Cnpj = _profile.Cnpj,
                Phone = _profile.Phone,
                Address = _profile.Address,
                City = _profile.City,
                State = _profile.State
            },
            Settings = new AdminSettingsSnapshot
            {
                WindowsNotificationsEnabled = _appSettings.WindowsNotificationsEnabled,
                NotificationSoundEnabled = _appSettings.NotificationSoundEnabled,
                InAppVibrationEnabled = _appSettings.InAppVibrationEnabled,
                NotificationSound = _appSettings.NotificationSound,
                AutoPrintDelivery = _appSettings.AutoPrintDelivery,
                AutoPrintKitchen = _appSettings.AutoPrintKitchen,
                PrintLayout = _appSettings.PrintLayout,
                PreferredPrinterName = _appSettings.PreferredPrinterName,
                ReceiptQrEnabled = _appSettings.ReceiptQrEnabled,
                ReceiptQrKind = _appSettings.ReceiptQrKind,
                ReceiptQrContentPreview = MaskConfigValue(_appSettings.ReceiptQrContent),
                AutoCheckUpdates = _appSettings.AutoCheckUpdates,
                AdminSyncEnabled = _appSettings.AdminSyncEnabled,
                SupabaseAuthEnabled = !string.IsNullOrWhiteSpace(_profile.Email),
                SupabaseUrlConfigured = false,
                SupabaseUserEmail = _profile.Email
            },
            Metrics = new AdminMetricsSnapshot
            {
                TablesCount = Tables.Count,
                OpenBoardsCount = openBoards,
                DeliveryCount = DeliveryTiles.Count,
                ProductsCount = Products.Count,
                UsersCount = Users.Count,
                CustomersCount = Customers.Count,
                LowStockCount = Products.Count(product => product.IsLowStock)
            }
        };
    }

    private static string MaskConfigValue(string value)
    {
        var clean = (value ?? "").Trim();
        if (clean.Length <= 6)
        {
            return clean;
        }

        return $"{clean[..3]}***{clean[^3..]}";
    }

    private static bool IsReasonableEmail(string value)
    {
        var clean = (value ?? "").Trim();
        var at = clean.IndexOf('@');
        return at > 0
            && at == clean.LastIndexOf('@')
            && at < clean.Length - 3
            && clean[(at + 1)..].Contains('.', StringComparison.Ordinal)
            && !clean.Any(char.IsWhiteSpace);
    }

    private static string MaskSupportLicense(string value)
    {
        var clean = NormalizeActivationKey(value);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return "sem licenca";
        }

        if (clean.Length <= 10)
        {
            return clean;
        }

        return $"{clean[..6]}...{clean[^4..]}";
    }

    private string BuildActivationSummary()
    {
        var keyText = MaskSupportLicense(_appSettings.ActivationKey);
        var planText = string.IsNullOrWhiteSpace(_appSettings.ActivationPlan)
            ? "plano nao informado"
            : _appSettings.ActivationPlan.Trim();
        var expirationText = _appSettings.ActivationExpiresAt.HasValue
            ? _appSettings.ActivationExpiresAt.Value.ToString("dd/MM/yyyy HH:mm", Brazil)
            : "sem vencimento local";
        var syncText = _appSettings.LastAdminSyncAt.HasValue
            ? $"Ultima sincronizacao: {_appSettings.LastAdminSyncAt.Value:dd/MM/yyyy HH:mm}"
            : "Ainda nao sincronizado nesta instalacao";

        return $"Chave: {keyText}\nValidade: {expirationText}\nPlano: {planText}\n{syncText}";
    }

    private static string TextOrDefault(string? value, string fallback)
    {
        var clean = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(clean) ? fallback : clean;
    }

    private void ApplyRestaurantIdentity()
    {
        var profile = _profile;
        var displayName = string.IsNullOrWhiteSpace(profile.BusinessName)
            ? AppDisplayName
            : profile.BusinessName;

        BrandNameText.Text = displayName;
        Title = string.Equals(displayName, AppDisplayName, StringComparison.Ordinal)
            ? AppDisplayName
            : $"{displayName} - {AppDisplayName}";

        var meta = new List<string>();
        if (!string.IsNullOrWhiteSpace(profile.Cnpj)) meta.Add($"CNPJ {profile.Cnpj}");
        if (!string.IsNullOrWhiteSpace(profile.Phone)) meta.Add(profile.Phone);
        meta.Add(BuildHeaderSyncText());
        BrandMetaText.Text = string.Join("  |  ", meta);
        UpdateTrayTitle();

        ApplyBrandLogo(profile.LocalLogoPath);
    }

    private string BuildHeaderSyncText()
    {
        var lastSyncAt = _appSettings.LastCentralSyncAt;
        if (_appSettings.LastAdminSyncAt.HasValue
            && (!lastSyncAt.HasValue || _appSettings.LastAdminSyncAt.Value > lastSyncAt.Value))
        {
            lastSyncAt = _appSettings.LastAdminSyncAt;
        }

        return lastSyncAt.HasValue
            ? $"Sync {lastSyncAt.Value:dd/MM HH:mm}"
            : "Sync pendente";
    }

    private void InitializeTrayIcon()
    {
        if (_trayIcon is not null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Sair", null, (_, _) => ExitFromTray());

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Visible = true,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        UpdateTrayTitle();
    }

    private static DrawingIcon LoadTrayIcon()
    {
        try
        {
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute));
            if (resource?.Stream is not null)
            {
                using var stream = resource.Stream;
                using var icon = new DrawingIcon(stream);
                return (DrawingIcon)icon.Clone();
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            Debug.WriteLine($"Tray icon load failed: {ex.Message}");
        }

        return SystemIcons.Application;
    }

    private void UpdateTrayTitle()
    {
        if (_trayIcon is null)
        {
            return;
        }

        var title = string.IsNullOrWhiteSpace(_profile.BusinessName)
            ? AppDisplayName
            : _profile.BusinessName.Trim();
        _trayIcon.Text = title.Length > 63 ? title[..63] : title;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_exitRequested)
        {
            return;
        }

        e.Cancel = true;
        SaveActiveTicketToCurrentBoard();
        SaveStore();
        CreateLocalStoreBackup("fechamento-janela", force: true);
        Hide();
        SetStatus("App rodando em segundo plano na bandeja do Windows.");
    }

    private void RestoreFromTray()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        SaveActiveTicketToCurrentBoard();
        SaveStore();
        CreateLocalStoreBackup("sair", force: true);
        _trayIcon?.Dispose();
        _trayIcon = null;
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _deliveryCountdownTimer.Stop();
        _supportPollTimer.Stop();
        _ifoodSyncTimer.Stop();
        _licenseTimer.Stop();
        _toastTimer.Stop();
        _kitchenPrintBatchTimer.Stop();
        _localBackupTimer.Stop();

        if (_waiterServer is not null)
        {
            _ = _waiterServer.StopAsync();
        }
        if (_whatsAppConnectorServer is not null)
        {
            _ = _whatsAppConnectorServer.StopAsync();
        }

        _trayIcon?.Dispose();
        _trayIcon = null;
        base.OnClosed(e);
    }

    private void ApplyBrandLogo(string logoPath)
    {
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            if (TryApplyBrandLogo(new Uri(logoPath, UriKind.Absolute)))
            {
                return;
            }
        }

        if (TryApplyBrandLogo(new Uri("pack://application:,,,/Assets/app-icon.png", UriKind.Absolute)))
        {
            return;
        }

        BrandLogoImage.Source = null;
        BrandLogoImage.Visibility = Visibility.Collapsed;
        BrandLogoFallback.Visibility = Visibility.Visible;
    }

    private bool TryApplyBrandLogo(Uri source)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = source;
            image.EndInit();
            image.Freeze();
            BrandLogoImage.Source = image;
            BrandLogoImage.Visibility = Visibility.Visible;
            BrandLogoFallback.Visibility = Visibility.Collapsed;
            return true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or InvalidOperationException)
        {
            Debug.WriteLine($"Logo load failed: {ex.Message}");
            return false;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.F9)
        {
            ReceiveTicket();
            e.Handled = true;
            return;
        }

        if (key == Key.F10)
        {
            ToggleCashRegister();
            e.Handled = true;
            return;
        }

        if (TryHandleModeShortcut(key, Keyboard.FocusedElement is TextBox))
        {
            e.Handled = true;
            return;
        }

        if (HandleFocusedTextField(e))
        {
            return;
        }

        if (TryDigit(e, out var digit))
        {
            if (BlockIFoodDeliveryEdit("incluir produto"))
            {
                e.Handled = true;
                return;
            }

            CodeBox.Text += digit;
            CodeBox.CaretIndex = CodeBox.Text.Length;
            SelectArea(KeyboardArea.Products);
            FilterProducts();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.F1:
                SelectArea(KeyboardArea.Tables);
                FocusActiveArea();
                e.Handled = true;
                break;
            case Key.F2:
                if (!BlockIFoodDeliveryEdit("incluir produto"))
                {
                    IncludeSelectedProduct(requireCode: true);
                }

                e.Handled = true;
                break;
            case Key.F3:
                if (IsCurrentIFoodDeliveryLocked())
                {
                    OpenIFoodActionsForCurrentOrder();
                }
                else
                {
                    ShowProductSearchDialog();
                }

                e.Handled = true;
                break;
            case Key.F4:
                PrintKitchen();
                e.Handled = true;
                break;
            case Key.F5:
                CloseTicket();
                e.Handled = true;
                break;
            case Key.F6:
                if (!BlockIFoodDeliveryEdit("transferir pedido"))
                {
                    ShowTransferDialog();
                }

                e.Handled = true;
                break;
            case Key.F7:
                SetStatus("Use o botao Excluir na propria linha do item.");
                e.Handled = true;
                break;
            case Key.F8:
                AddPrepayment();
                e.Handled = true;
                break;
            case Key.F9:
                ReceiveTicket();
                e.Handled = true;
                break;
            case Key.Enter:
                EnterAction();
                e.Handled = true;
                break;
            case Key.Escape:
                CodeBox.Text = "";
                SearchBox.Text = "";
                FilterProducts();
                SetStatus("Busca limpa.");
                e.Handled = true;
                break;
            case Key.Back:
                if (BlockIFoodDeliveryEdit("editar codigo"))
                {
                    e.Handled = true;
                    break;
                }

                if (CodeBox.Text.Length > 0)
                {
                    CodeBox.Text = CodeBox.Text[..^1];
                    FilterProducts();
                }
                e.Handled = true;
                break;
            case Key.Add:
            case Key.OemPlus:
                if (!BlockIFoodDeliveryEdit("alterar quantidade"))
                {
                    ChangeQuantity(1);
                }

                e.Handled = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
                if (!BlockIFoodDeliveryEdit("alterar quantidade"))
                {
                    ChangeQuantity(-1);
                }

                e.Handled = true;
                break;
            case Key.Left:
                MoveSelection(-1, 0);
                e.Handled = true;
                break;
            case Key.Right:
                MoveSelection(1, 0);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(0, -1);
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(0, 1);
                e.Handled = true;
                break;
            case Key.Tab:
                CycleArea();
                e.Handled = true;
                break;
        }
    }

    private bool TryHandleModeShortcut(Key key, bool focusedTextField)
    {
        var modifiers = Keyboard.Modifiers;
        var plainShortcut = !focusedTextField && modifiers == ModifierKeys.None;
        var textFieldShortcut = focusedTextField && modifiers == ModifierKeys.Alt;
        if (!plainShortcut && !textFieldShortcut)
        {
            return false;
        }

        var mode = key switch
        {
            Key.D1 or Key.NumPad1 => "Comandas",
            Key.D2 or Key.NumPad2 => "Balcao",
            Key.D3 or Key.NumPad3 => "Delivery",
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(mode))
        {
            return false;
        }

        ModeList.SelectedItem = mode;
        ModeList.ScrollIntoView(mode);
        SelectArea(KeyboardArea.Tables);
        FocusActiveArea();
        SetStatus($"Modo {mode} selecionado pelo atalho {ModeShortcutText(mode)}.");
        return true;
    }

    private static string ModeShortcutText(string mode)
    {
        return mode switch
        {
            "Comandas" => "1",
            "Balcao" => "2",
            "Delivery" => "3",
            _ => ""
        };
    }

    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox || string.IsNullOrWhiteSpace(e.Text))
        {
            return;
        }

        if (e.Text.Length == 1 && char.IsDigit(e.Text[0]))
        {
            if (BlockIFoodDeliveryEdit("incluir produto"))
            {
                e.Handled = true;
                return;
            }

            CodeBox.Text += e.Text;
            CodeBox.CaretIndex = CodeBox.Text.Length;
            SelectArea(KeyboardArea.Products);
            FilterProducts();
            e.Handled = true;
        }
    }

    private void RibbonScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void RibbonScrollLeft_Click(object sender, RoutedEventArgs e) => ScrollRibbon(-RibbonScrollStep);

    private void RibbonScrollRight_Click(object sender, RoutedEventArgs e) => ScrollRibbon(RibbonScrollStep);

    private void RibbonScrollViewer_Loaded(object sender, RoutedEventArgs e) => UpdateRibbonScrollButtons();

    private void RibbonScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateRibbonScrollButtons();

    private void RibbonScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateRibbonScrollButtons();

    private void ScrollRibbon(double delta)
    {
        var nextOffset = Math.Clamp(RibbonScrollViewer.HorizontalOffset + delta, 0, RibbonScrollViewer.ScrollableWidth);
        RibbonScrollViewer.ScrollToHorizontalOffset(nextOffset);
        UpdateRibbonScrollButtons();
    }

    private void UpdateRibbonScrollButtons()
    {
        if (!IsLoaded)
        {
            return;
        }

        var canScroll = RibbonScrollViewer.ScrollableWidth > 0.5;
        RibbonLeftButton.Visibility = canScroll ? Visibility.Visible : Visibility.Collapsed;
        RibbonRightButton.Visibility = canScroll ? Visibility.Visible : Visibility.Collapsed;
        RibbonLeftButton.IsEnabled = canScroll && RibbonScrollViewer.HorizontalOffset > 0.5;
        RibbonRightButton.IsEnabled = canScroll && RibbonScrollViewer.HorizontalOffset < RibbonScrollViewer.ScrollableWidth - 0.5;
    }

    private bool HandleFocusedTextField(KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is not TextBox textBox)
        {
            return false;
        }

        if (e.Key is >= Key.F1 and <= Key.F24 or Key.Tab)
        {
            return false;
        }

        if (IsAreaNavigationKey(e.Key))
        {
            return false;
        }

        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0
            && TryKeypadDigit(e, out var keypadDigit))
        {
            InsertTextAtCaret(textBox, keypadDigit.ToString(Brazil));
            if (textBox == CodeBox)
            {
                FilterProducts();
                SelectArea(KeyboardArea.Products);
            }

            e.Handled = true;
            return true;
        }

        if (textBox == TableBox)
        {
            if (e.Key == Key.Enter)
            {
                LocateTableFromInput();
                e.Handled = true;
            }

            return true;
        }

        if (textBox == WaiterBox)
        {
            if (e.Key == Key.Enter)
            {
                if (!TryApplyStaffFromInput(CurrentBoard))
                {
                    e.Handled = true;
                    return true;
                }

                SaveStore();
                CodeBox.Focus();
                CodeBox.SelectAll();
                SelectArea(KeyboardArea.Products);
                e.Handled = true;
            }

            return true;
        }

        if (textBox == CodeBox)
        {
            if (e.Key == Key.Enter)
            {
                if (BlockIFoodDeliveryEdit("incluir produto"))
                {
                    e.Handled = true;
                    return true;
                }

                QuantityBox.Focus();
                QuantityBox.SelectAll();
                SelectArea(KeyboardArea.Products);
                SetStatus("Informe a quantidade e pressione Enter para incluir.");
                e.Handled = true;
            }

            return true;
        }

        if (textBox == SearchBox)
        {
            if (e.Key == Key.Enter)
            {
                if (IsCurrentIFoodDeliveryLocked())
                {
                    OpenIFoodActionsForCurrentOrder();
                    e.Handled = true;
                    return true;
                }

                FilterProducts();
                SelectArea(KeyboardArea.Products);
                ProductsList.Focus();
                e.Handled = true;
            }

            return true;
        }

        if (textBox == QuantityBox)
        {
            if (e.Key == Key.Enter)
            {
                if (BlockIFoodDeliveryEdit("incluir produto"))
                {
                    e.Handled = true;
                    return true;
                }

                IncludeSelectedProduct(requireCode: true);
                e.Handled = true;
            }

            return true;
        }

        if (textBox == PriceBox || textBox == NoteBox)
        {
            if (e.Key == Key.Enter)
            {
                if (BlockIFoodDeliveryEdit("aplicar taxas"))
                {
                    e.Handled = true;
                    return true;
                }

                ApplyTableCharges();
                e.Handled = true;
            }

            return true;
        }

        return false;
    }

    private void CommandTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void ModeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModeList.SelectedItem is string mode)
        {
            SaveActiveTicketToCurrentBoard();
            RefreshBoardForMode();
            SelectTable(0, saveCurrent: false);
            SetStatus($"Modo: {mode}");
        }
    }

    private void RibbonButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string action })
        {
            return;
        }

        switch (action)
        {
            case "SearchProducts":
                ShowProductSearchDialog();
                break;
            case "TransferProducts":
                ShowTransferDialog();
                break;
            case "Discount":
                ShowDiscountDialog();
                break;
            case "ChangeClient":
                ShowClientDialog();
                break;
            case "ReopenCommand":
                ReopenCurrentCommand(requireManagerApproval: true);
                break;
            case "PeopleCount":
                ShowStaffDialog();
                break;
            case "ProductCatalog":
                ShowProductCatalogDialog();
                break;
            case "Users":
                ShowUsersDialog();
                break;
            case "Cash":
                ShowCashDialog();
                break;
            case "CloseCash":
                ToggleCashRegister();
                break;
            case "DeliveryNew":
                ShowDeliveryOrderDialog();
                break;
            case "DeliveryZones":
                ShowDeliveryZonesDialog();
                break;
            case "IFood":
                ShowIFoodDialog();
                break;
            case "WhatsApp":
                ShowWhatsAppDialog();
                break;
            case "WaiterWeb":
                ShowWaiterWebDialog();
                break;
            case "Inventory":
                ShowInventoryDialog();
                break;
            case "Cardapio":
                ShowCardapioDialog();
                break;
            case "Reports":
                ShowReportsDialog();
                break;
            case "Backup":
                ShowBackupDialog();
                break;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsDialog();
    }

    private void WhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        ShowWhatsAppDialog();
    }

    private async void OnlineStoreButton_Click(object sender, RoutedEventArgs e)
    {
        var ifood = _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (!HasIFoodConnectionConfigured(ifood))
        {
            ShowIFoodDialog();
            RefreshOnlineStoreButton();
            return;
        }

        var open = !_appSettings.PublicMenuStoreOpen;
        _appSettings.PublicMenuStoreOpen = open;
        ifood.Enabled = open;
        SaveAppSettings();
        SaveStore();
        RefreshOnlineStoreButton();

        if (open)
        {
            _ifoodSyncTimer.Start();
            _ = AutoImportIFoodOrdersAsync(force: true);
            _ = SyncIFoodPresenceOnceAsync();
        }
        else
        {
            _ifoodSyncTimer.Stop();
        }

        var result = await PublishGeneratedPublicMenuAsync(silent: true);
        RefreshOnlineStoreButton();
        if (!result.Ok)
        {
            SetStatus($"Loja online {(open ? "ligada" : "desligada")} no PDV. Cardapio pendente: {result.Message}");
            return;
        }

        SetStatus(open
            ? "Loja online ligada para iFood e cardapio."
            : "Loja online desligada para iFood e cardapio.");
    }

    private static bool HasIFoodConnectionConfigured(IFoodIntegrationSettings? settings)
    {
        return settings is not null
            && !string.IsNullOrWhiteSpace(settings.ConnectionId)
            && (string.IsNullOrWhiteSpace(settings.ConnectionStatus)
                || string.Equals(settings.ConnectionStatus, "conectado", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(settings.MerchantId));
    }

    private void RefreshOnlineStoreButton()
    {
        if (OnlineStoreButton is null)
        {
            return;
        }

        var ifood = _appSettings.IFood;
        var connected = HasIFoodConnectionConfigured(ifood);
        if (!connected)
        {
            OnlineStoreButtonText.Text = "Conectar iFood";
            OnlineStoreBadgeText.Text = "IF";
            OnlineStoreButton.Background = Solid("#0B2633");
            OnlineStoreButton.BorderBrush = Solid("#255665");
            OnlineStoreButton.Foreground = Solid("#DFFBFA");
            OnlineStoreBadge.Background = Solid("#EAF4F8");
            OnlineStoreBadgeText.Foreground = Solid("#0B3A52");
            OnlineStoreButton.ToolTip = "Conectar iFood para ligar/desligar a loja online";
            return;
        }

        var open = _appSettings.PublicMenuStoreOpen;
        OnlineStoreButtonText.Text = open ? "Loja online" : "Loja offline";
        OnlineStoreBadgeText.Text = open ? "ON" : "OFF";
        OnlineStoreButton.Background = Solid(open ? "#0B3A52" : "#A11D1D");
        OnlineStoreButton.BorderBrush = Solid(open ? "#0B3A52" : "#A11D1D");
        OnlineStoreButton.Foreground = Brushes.White;
        OnlineStoreBadge.Background = Solid(open ? "#DFFBF8" : "#FFE2DF");
        OnlineStoreBadgeText.Foreground = Solid(open ? "#0B3A52" : "#A11D1D");
        OnlineStoreButton.ToolTip = open
            ? "Clique para deixar iFood e cardapio offline"
            : "Clique para deixar iFood e cardapio online";
    }

    private void SupportButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSupportDialog();
    }

    private void ShowSupportDialog()
    {
        var dialog = CreateDialog("Suporte online", 680, 600);
        var categoryBox = new ComboBox
        {
            ItemsSource = new[]
            {
                "Suporte tecnico",
                "Licenca / renovacao",
                "iFood / delivery",
                "Impressora",
                "WhatsApp",
                "Financeiro"
            },
            SelectedIndex = 0
        };
        var priorityBox = new ComboBox
        {
            ItemsSource = new[] { "Normal", "Urgente" },
            SelectedIndex = 0
        };
        var messageBox = new TextBox
        {
            MinHeight = 74,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var messagesPanel = new StackPanel();
        var messagesScroll = new ScrollViewer
        {
            Content = messagesPanel,
            Height = 220,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Brushes.Transparent,
            Padding = new Thickness(0, 0, 4, 0)
        };
        var chatFrame = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 10, 0, 10),
            Child = messagesScroll
        };
        var status = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var send = DialogButton("Enviar mensagem", "#08A99B");
        send.MinWidth = 170;
        var newTicketFields = new StackPanel();
        string activeTicketId = "";
        DateTimeOffset? lastDialogAdminMessageAt = _lastSupportAdminMessageAt;

        static Grid SupportTwoColumns(UIElement left, UIElement right)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            if (left is FrameworkElement leftElement)
            {
                leftElement.Margin = new Thickness(0, 0, 8, 0);
            }
            if (right is FrameworkElement rightElement)
            {
                rightElement.Margin = new Thickness(8, 0, 0, 0);
            }
            grid.Children.Add(left);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
            return grid;
        }

        Border Bubble(string sender, string message, DateTimeOffset when)
        {
            var isAdmin = string.Equals(sender, "admin", StringComparison.OrdinalIgnoreCase);
            return new Border
            {
                Background = Solid(isAdmin ? "#EAF8FA" : "#E6FBF8"),
                BorderBrush = Solid(isAdmin ? "#C9D8E7" : "#BDE5DD"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(isAdmin ? 0 : 78, 0, isAdmin ? 78 : 0, 9),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = isAdmin ? "Suporte" : "Voce",
                            Foreground = Solid(isAdmin ? "#0B3A52" : "#08A99B"),
                            FontWeight = FontWeights.Bold,
                            FontSize = 12
                        },
                        new TextBlock
                        {
                            Text = message,
                            Foreground = Solid("#071A2C"),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 4, 0, 0)
                        },
                        new TextBlock
                        {
                            Text = when.ToLocalTime().ToString("dd/MM HH:mm", Brazil),
                            Foreground = Solid("#5B6B7A"),
                            FontSize = 11,
                            Margin = new Thickness(0, 5, 0, 0)
                        }
                    }
                }
            };
        }

        void RenderSupport(AdminSupportTicketSnapshot? ticket)
        {
            messagesPanel.Children.Clear();
            if (ticket is null)
            {
                activeTicketId = "";
                newTicketFields.Visibility = Visibility.Visible;
                messagesPanel.Children.Add(new Border
                {
                    Background = Solid("#F8FBFD"),
                    BorderBrush = Solid("#E3EBF2"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Nenhuma conversa aberta",
                                Foreground = Solid("#071A2C"),
                                FontWeight = FontWeights.Bold,
                                FontSize = 14
                            },
                            new TextBlock
                            {
                                Text = "Digite sua mensagem para iniciar o atendimento.",
                                Foreground = Solid("#5B6B7A"),
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 4, 0, 0)
                            }
                        }
                    }
                });
                status.Text = "Digite a mensagem. A conversa aparece para o admin responder em tempo real.";
                status.Foreground = Solid("#5B6B7A");
                return;
            }

            activeTicketId = string.IsNullOrWhiteSpace(ticket.ShortId) ? ticket.Id : ticket.ShortId;
            newTicketFields.Visibility = ticket.Status == "RESOLVIDO" ? Visibility.Visible : Visibility.Collapsed;
            IEnumerable<AdminSupportMessageSnapshot> messages = ticket.Messages.Count > 0
                ? ticket.Messages
                : new[] { new AdminSupportMessageSnapshot { Sender = "cliente", Message = ticket.Message, When = ticket.CreatedAt } };
            foreach (var item in messages.OrderBy(item => item.When))
            {
                if (!string.IsNullOrWhiteSpace(item.Message))
                {
                    messagesPanel.Children.Add(Bubble(item.Sender, item.Message, item.When));
                }
            }

            status.Text = $"Protocolo {activeTicketId} - {ticket.Status}. Atualizacao automatica ligada.";
            status.Foreground = ticket.Status == "RESOLVIDO" ? Solid("#08A99B") : Solid("#5B6B7A");
            _ = Dispatcher.BeginInvoke(() => messagesScroll.ScrollToEnd());
        }

        async Task RefreshSupport(bool notify)
        {
            var result = await FetchAdminSupportTicketsAsync();
            if (!result.Ok)
            {
                status.Text = TextOrDefault(result.Message, "Nao foi possivel atualizar o suporte agora.");
                status.Foreground = RedText;
                return;
            }

            var ticket = result.Tickets
                .OrderBy(item => item.Status == "RESOLVIDO" ? 1 : 0)
                .ThenByDescending(item => item.UpdatedAt)
                .FirstOrDefault();
            var latestAdmin = result.Tickets
                .SelectMany(item => item.Messages)
                .Where(item => string.Equals(item.Sender, "admin", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.When)
                .FirstOrDefault();
            if (notify && latestAdmin is not null && lastDialogAdminMessageAt.HasValue && latestAdmin.When > lastDialogAdminMessageAt.Value)
            {
                ShowToast("Suporte respondeu", "Nova mensagem recebida no suporte.", "?", "#08A99B", "#E6FBF8");
            }

            if (latestAdmin is not null)
            {
                lastDialogAdminMessageAt = latestAdmin.When;
                _lastSupportAdminMessageAt = latestAdmin.When;
            }

            RenderSupport(ticket);
        }

        send.Click += async (_, _) =>
        {
            var message = messageBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                status.Text = "Descreva o problema antes de enviar.";
                status.Foreground = RedText;
                messageBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
            {
                status.Text = "Ative uma licenca antes de enviar suporte vinculado ao admin.";
                status.Foreground = RedText;
                return;
            }

            send.IsEnabled = false;
            status.Text = "Enviando mensagem...";
            status.Foreground = Solid("#5B6B7A");
            var result = string.IsNullOrWhiteSpace(activeTicketId)
                ? await SendAdminSupportAsync(
                    categoryBox.SelectedItem?.ToString() ?? "Suporte tecnico",
                    priorityBox.SelectedItem?.ToString() ?? "Normal",
                    message)
                : await SendAdminSupportMessageAsync(activeTicketId, message);
            send.IsEnabled = true;

            if (result.Ok)
            {
                if (!string.IsNullOrWhiteSpace(result.TicketId))
                {
                    activeTicketId = result.TicketId;
                }

                messageBox.Clear();
                status.Text = TextOrDefault(result.Message, "Mensagem enviada.");
                status.Foreground = Solid("#08A99B");
                SetStatus(status.Text);
                QueueAdminCheckIn("support.sent", force: true);
                await RefreshSupport(false);
                return;
            }

            status.Text = TextOrDefault(result.Message, "Nao foi possivel enviar suporte agora.");
            status.Foreground = RedText;
            SetStatus(status.Text);
        };

        var panel = DialogPanel();
        panel.Margin = new Thickness(18, 14, 18, 14);
        newTicketFields.Children.Add(SupportTwoColumns(DialogField("Assunto", categoryBox), DialogField("Prioridade", priorityBox)));
        panel.Children.Add(newTicketFields);
        panel.Children.Add(chatFrame);
        panel.Children.Add(DialogField("Mensagem", messageBox));
        var footer = new Grid { Margin = new Thickness(0, 2, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        status.VerticalAlignment = VerticalAlignment.Center;
        status.Margin = new Thickness(0, 12, 12, 0);
        footer.Children.Add(status);
        Grid.SetColumn(send, 1);
        footer.Children.Add(send);
        panel.Children.Add(footer);
        var supportDialogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        supportDialogTimer.Tick += async (_, _) => await RefreshSupport(true);
        dialog.Closed += (_, _) => supportDialogTimer.Stop();
        dialog.Content = panel;
        _ = Dispatcher.BeginInvoke(async () =>
        {
            await RefreshSupport(false);
            supportDialogTimer.Start();
        });
        dialog.ShowDialog();
    }

    private void ShowSettingsDialog()
    {
        if (!RequirePermission(CanManageSettings, "Configuracoes do sistema"))
        {
            return;
        }

        var profile = new RestaurantIdentityProfile
        {
            Email = _profile.Email,
            OwnerName = _profile.OwnerName,
            BusinessName = _profile.BusinessName,
            LegalName = _profile.LegalName,
            Cnpj = _profile.Cnpj,
            Phone = _profile.Phone,
            Address = _profile.Address,
            City = _profile.City,
            State = _profile.State,
            Latitude = _profile.Latitude,
            Longitude = _profile.Longitude,
            LocalLogoPath = _profile.LocalLogoPath,
            LocalCoverPath = _profile.LocalCoverPath
        };
        var dialog = CreateDialog("Configuracoes do sistema", 1040, 720);
        var emailBox = new TextBox { Text = profile.Email };
        var ownerBox = new TextBox { Text = profile.OwnerName };
        var businessBox = new TextBox { Text = profile.BusinessName };
        var legalBox = new TextBox { Text = profile.LegalName };
        var cnpjBox = new TextBox { Text = profile.Cnpj };
        AttachCnpjMask(cnpjBox);
        var phoneBox = new TextBox { Text = profile.Phone };
        var addressBox = new TextBox { Text = profile.Address };
        var cityBox = new TextBox { Text = profile.City };
        var stateBox = new TextBox { Text = profile.State };
        var logoPath = profile.LocalLogoPath;
        var coverPath = profile.LocalCoverPath;
        var logoText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(logoPath) ? "Nenhuma imagem selecionada" : Path.GetFileName(logoPath),
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap
        };
        var coverText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(coverPath) ? "Nenhuma capa selecionada" : Path.GetFileName(coverPath),
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap
        };
        var waitMinBox = new TextBox
        {
            Text = Math.Max(1, _appSettings.PublicMenuWaitMinMinutes).ToString(Brazil),
            MinHeight = 38
        };
        var waitMaxBox = new TextBox
        {
            Text = Math.Max(_appSettings.PublicMenuWaitMinMinutes, _appSettings.PublicMenuWaitMaxMinutes).ToString(Brazil),
            MinHeight = 38
        };
        var ifoodSoundPath = _appSettings.IFoodAlertSoundPath;
        var ifoodSoundText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(ifoodSoundPath) ? "Toque iFood: alerta de pedido do app" : Path.GetFileName(ifoodSoundPath),
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap
        };
        const string defaultPrinterOption = "Usar padrao do Windows";
        _appSettings.SectorPrinters ??= [];
        var installedPrinters = new List<string> { defaultPrinterOption };
        installedPrinters.AddRange(GetInstalledPrinterNames());
        if (!string.IsNullOrWhiteSpace(_appSettings.PreferredPrinterName)
            && installedPrinters.All(item => !string.Equals(item, _appSettings.PreferredPrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            installedPrinters.Add(_appSettings.PreferredPrinterName);
        }
        foreach (var printer in _appSettings.SectorPrinters.Select(setting => setting.PrinterName).Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            if (installedPrinters.All(item => !string.Equals(item, printer, StringComparison.OrdinalIgnoreCase)))
            {
                installedPrinters.Add(printer);
            }
        }

        var selectedPrinter = string.IsNullOrWhiteSpace(_appSettings.PreferredPrinterName)
            ? defaultPrinterOption
            : _appSettings.PreferredPrinterName;
        var sectorPrinterRows = new List<(TextBox SectorBox, ComboBox PrinterBox)>();
        var sectorPrinterList = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var printerSelectedText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.Bold,
            Foreground = Solid("#071A2C"),
            Margin = new Thickness(0, 2, 0, 4)
        };
        var defaultPrinterText = new TextBlock
        {
            Text = $"Padrao do Windows: {GetDefaultPrinterName()}",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var versionText = new TextBlock
        {
            Text = $"Versao do app: {GetAppVersion()}",
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.Bold,
            FontSize = 15
        };
        var status = new TextBlock
        {
            Foreground = GreenText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var selectedQrKind = NormalizeReceiptQrKind(_appSettings.ReceiptQrKind);
        var qrContentBox = new TextBox
        {
            Text = _appSettings.ReceiptQrContent,
            MinHeight = 38,
            Margin = new Thickness(0, 4, 0, 2)
        };
        var qrHint = new TextBlock
        {
            Text = "Exemplos: chave Pix, @instagram, link do Google Maps ou qualquer URL. Se ficar vazio, o PDV imprime um QR simples com dados da loja e total.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var licenseText = new TextBlock
        {
            Text = BuildActivationSummary(),
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 8)
        };
        var linkedAccountText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(profile.Email)
                ? "Nenhum email de conta vinculado."
                : $"Conta vinculada: {profile.Email}",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var soundButtons = new List<Button>();
        var printSizeButtons = new List<Button>();

        Border ToggleCard(string title, string subtitle, Func<bool> get, Action<bool> set)
        {
            var titleText = new TextBlock { FontWeight = FontWeights.Bold };
            var subtitleText = new TextBlock { Foreground = Solid("#5B6B7A"), FontSize = 11, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap };
            var card = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                Child = new StackPanel { Children = { titleText, subtitleText } }
            };

            void Refresh()
            {
                var enabled = get();
                card.Background = enabled ? Solid("#F2FBFC") : Brushes.White;
                card.BorderBrush = enabled ? Solid("#20C8BE") : Solid("#D6E2EA");
                titleText.Foreground = enabled ? Solid("#03151F") : Solid("#071A2C");
                titleText.Text = enabled ? $"{title}: ligado" : $"{title}: desligado";
                subtitleText.Text = subtitle;
            }

            card.MouseLeftButtonDown += (_, _) =>
            {
                set(!get());
                Refresh();
            };
            Refresh();
            return card;
        }

        Button SegmentButton(string text, string tag)
        {
            return new Button
            {
                Content = text,
                Tag = tag,
                Height = 38,
                Margin = new Thickness(0, 0, 8, 8),
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Template = RoundedButtonTemplate()
            };
        }

        void RefreshSegments(IEnumerable<Button> buttons, string selected, string selectedColor)
        {
            foreach (var button in buttons)
            {
                var active = string.Equals(button.Tag?.ToString(), selected, StringComparison.Ordinal);
                button.Background = active ? Solid("#EAF8FA") : Brushes.White;
                button.BorderBrush = active ? Solid(selectedColor) : Solid("#CAD6E2");
                button.Foreground = active ? Solid(selectedColor) : Solid("#071A2C");
                button.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
            }
        }

        var mpStatusText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 10)
        };
        var mpTerminalBox = new ComboBox
        {
            MinHeight = 38,
            DisplayMemberPath = nameof(AdminMercadoPagoTerminalDto.Display),
            Margin = new Thickness(0, 4, 0, 8)
        };
        var mpTerminalField = DialogField("Point da loja", mpTerminalBox);
        var mpEnabledCheck = new CheckBox
        {
            Content = "Usar Mercado Pago no F9",
            IsChecked = _appSettings.MercadoPago?.Enabled == true,
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        mpEnabledCheck.Visibility = Visibility.Collapsed;
        var mpConnect = DialogButton("Conectar Mercado Pago", "#0B3A52");
        var mpRefresh = DialogButton("Atualizar Point", "#0B3A52");
        var mpSaveTerminal = DialogButton("Salvar Point", "#0B3A52");
        var mpToggle = DialogButton("Ativar no F9", "#0B3A52");
        var mpConnectionPillText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var mpTerminalPillText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var mpConnectionPill = new Border
        {
            Background = Solid("#EEF4F8"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 6, 8),
            Child = mpConnectionPillText
        };
        var mpTerminalPill = new Border
        {
            Background = Solid("#EEF4F8"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(6, 0, 0, 8),
            Child = mpTerminalPillText
        };
        var mpPillGrid = new UniformGrid { Columns = 2, Rows = 1 };
        mpPillGrid.Children.Add(mpConnectionPill);
        mpPillGrid.Children.Add(mpTerminalPill);
        Border MpInfoCard(string title, string body, string accent)
        {
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = title, Foreground = Solid(accent), FontSize = 12, FontWeight = FontWeights.Bold },
                        new TextBlock { Text = body, Foreground = Solid("#5B6B7A"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) }
                    }
                }
            };
        }

        var mpFlowGrid = new UniformGrid { Columns = 3, Rows = 1, Margin = new Thickness(0, 0, 0, 10) };
        mpFlowGrid.Children.Add(MpInfoCard("Pix", "QR e copia-e-cola na tela.", "#0B3A52"));
        mpFlowGrid.Children.Add(MpInfoCard("Cartao", "Envia para a Point em modo PDV.", "#0B3A52"));
        mpFlowGrid.Children.Add(MpInfoCard("F9", "Fecha so quando aprovar.", "#99620D"));

        var mpPointNote = new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(11, 9, 11, 9),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "A Point integrada funciona pela internet da conta Mercado Pago. Ela precisa estar vinculada a loja e em modo PDV; nao usa cabo USB.",
                Foreground = Solid("#5B6B7A"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            }
        };
        var mpAutoRefreshRunning = false;
        mpEnabledCheck.Checked += (_, _) =>
        {
            (_appSettings.MercadoPago ??= new MercadoPagoPaymentSettings()).Enabled = true;
            RefreshMercadoPagoCard();
        };
        mpEnabledCheck.Unchecked += (_, _) =>
        {
            (_appSettings.MercadoPago ??= new MercadoPagoPaymentSettings()).Enabled = false;
            RefreshMercadoPagoCard();
        };
        mpToggle.Click += (_, _) =>
        {
            var mp = _appSettings.MercadoPago ??= new MercadoPagoPaymentSettings();
            mp.Enabled = !mp.Enabled;
            SaveAppSettings();
            status.Foreground = mp.Enabled ? GreenText : Solid("#5B6B7A");
            status.Text = mp.Enabled
                ? "Mercado Pago ativado no F9."
                : "Mercado Pago desativado no F9. As outras formas de pagamento continuam normais.";
            RefreshMercadoPagoCard();
        };
        void RefreshMercadoPagoCard()
        {
            var mp = _appSettings.MercadoPago ??= new MercadoPagoPaymentSettings();
            var terminal = string.IsNullOrWhiteSpace(mp.DefaultTerminalLabel)
                ? mp.DefaultTerminalId
                : mp.DefaultTerminalLabel;
            mpStatusText.Text = mp.Connected
                ? string.IsNullOrWhiteSpace(terminal)
                    ? "Conta conectada. Escolha a Point para debito/credito; Pix ja usa QR/link."
                    : $"Point selecionada: {terminal}. Pix continua por QR/link."
                : "Conecte a conta da loja para liberar Pix, link e Point no F9.";
            if (!string.IsNullOrWhiteSpace(mp.LastError))
            {
                mpStatusText.Text += $" Ultimo erro: {mp.LastError}";
            }

            mpStatusText.Foreground = mp.Enabled && mp.Connected
                ? GreenText
                : Solid("#5B6B7A");
            mpEnabledCheck.IsChecked = mp.Enabled;
            mpToggle.Content = mp.Enabled ? "Desativar no F9" : "Ativar no F9";
            mpToggle.Background = mp.Enabled ? Brushes.White : Solid("#0B3A52");
            mpToggle.Foreground = mp.Enabled ? Solid("#5B6B7A") : Brushes.White;
            mpToggle.BorderThickness = mp.Enabled ? new Thickness(1) : new Thickness(0);
            mpToggle.BorderBrush = mp.Enabled ? Solid("#BFD1E2") : Brushes.Transparent;
            mpConnect.Content = mp.Connected ? "Reconectar conta" : "Conectar Mercado Pago";
            mpConnectionPillText.Text = mp.Connected
                ? mp.Enabled ? "Ativo no F9" : "Conectado, desligado"
                : "Conta nao conectada";
            mpConnectionPillText.Foreground = mp.Connected && mp.Enabled ? Solid("#0B3A52") : Solid("#5B6B7A");
            mpConnectionPill.Background = mp.Connected && mp.Enabled ? Solid("#F2FBFC") : Solid("#F1F5F8");
            mpTerminalPillText.Text = string.IsNullOrWhiteSpace(terminal)
                ? "Sem Point selecionada"
                : $"Point: {terminal}";
            mpTerminalPillText.Foreground = string.IsNullOrWhiteSpace(terminal) ? Solid("#5B6B7A") : Solid("#0B3A52");
            mpTerminalPill.Background = string.IsNullOrWhiteSpace(terminal) ? Solid("#F1F5F8") : Solid("#F2FBFC");
            mpTerminalBox.IsEnabled = mp.Connected;
            mpTerminalField.Visibility = mp.Connected ? Visibility.Visible : Visibility.Collapsed;
            mpSaveTerminal.IsEnabled = mp.Connected;
            mpRefresh.IsEnabled = true;
        }

        async Task RefreshMercadoPagoOnlineAsync(bool loadTerminals)
        {
            var connection = await FetchMercadoPagoConnectionStatusAsync();
            ApplyMercadoPagoStatus(connection);
            if (!connection.Ok)
            {
                status.Foreground = RedText;
                status.Text = TextOrDefault(connection.Message, "Nao consegui consultar o Mercado Pago agora.");
                RefreshMercadoPagoCard();
                return;
            }

            if (loadTerminals && connection.Connected)
            {
                var terminals = await FetchMercadoPagoTerminalsAsync();
                if (terminals.Ok)
                {
                    var mp = _appSettings.MercadoPago ??= new MercadoPagoPaymentSettings();
                    var list = terminals.Terminals
                        .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                        .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .ToList();
                    if (!string.IsNullOrWhiteSpace(mp.DefaultTerminalId)
                        && list.All(item => !string.Equals(item.Id, mp.DefaultTerminalId, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Insert(0, new AdminMercadoPagoTerminalDto
                        {
                            Id = mp.DefaultTerminalId,
                            Label = string.IsNullOrWhiteSpace(mp.DefaultTerminalLabel)
                                ? mp.DefaultTerminalId
                                : mp.DefaultTerminalLabel
                        });
                    }

                    mpTerminalBox.ItemsSource = null;
                    mpTerminalBox.ItemsSource = list;
                    mpTerminalBox.SelectedItem = list.FirstOrDefault(item =>
                        string.Equals(item.Id, mp.DefaultTerminalId, StringComparison.OrdinalIgnoreCase));
                    status.Foreground = GreenText;
                    status.Text = list.Count == 0
                        ? "Conta conectada. Sem maquininha ativa; o F9 usa Pix QR e link de pagamento."
                        : "Maquininhas Mercado Pago atualizadas. O F9 tambem pode usar Pix QR e link.";
                }
                else
                {
                    status.Foreground = Solid("#99620D");
                    status.Text = "Conta Mercado Pago conectada. Nao encontrei maquininha agora; o F9 usa Pix QR e link de pagamento.";
                }
            }
            else if (connection.Connected)
            {
                status.Foreground = GreenText;
                status.Text = "Conta Mercado Pago conectada.";
            }

            SaveAppSettings();
            RefreshMercadoPagoCard();
        }

        async Task AutoRefreshMercadoPagoUntilConnectedAsync()
        {
            if (mpAutoRefreshRunning)
            {
                return;
            }

            mpAutoRefreshRunning = true;
            try
            {
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    await Task.Delay(attempt == 0 ? 1800 : 3000);
                    if (!dialog.IsVisible)
                    {
                        return;
                    }

                    status.Foreground = Solid("#5B6B7A");
                    status.Text = "Aguardando autorizacao do Mercado Pago...";
                    await RefreshMercadoPagoOnlineAsync(loadTerminals: true);
                    if (_appSettings.MercadoPago?.Connected == true)
                    {
                        status.Foreground = GreenText;
                        status.Text = string.IsNullOrWhiteSpace(_appSettings.MercadoPago.DefaultTerminalId)
                            ? "Mercado Pago confirmado. Sem maquininha ativa; F9 usa Pix QR e link."
                            : "Mercado Pago confirmado e maquininha carregada.";
                        return;
                    }
                }

                status.Foreground = Solid("#99620D");
                status.Text = "Ainda nao confirmei a autorizacao. Se voce terminou no navegador, clique em Atualizar.";
            }
            finally
            {
                mpAutoRefreshRunning = false;
                RefreshMercadoPagoCard();
            }
        }

        mpConnect.Click += async (_, _) =>
        {
            if (!TryApplyProfileInputs())
            {
                return;
            }

            (_appSettings.MercadoPago ??= new MercadoPagoPaymentSettings()).Enabled = true;
            SaveRestaurantProfile();
            SaveAppSettings();
            status.Foreground = Solid("#5B6B7A");
            status.Text = "Abrindo Mercado Pago para conectar a conta da loja...";
            mpConnect.IsEnabled = false;
            try
            {
                var result = await StartMercadoPagoConnectAsync();
                if (!result.Ok || string.IsNullOrWhiteSpace(result.AuthUrl))
                {
                    status.Foreground = RedText;
                    status.Text = TextOrDefault(result.Message, "Nao consegui iniciar a conexao Mercado Pago.");
                    return;
                }

                Process.Start(new ProcessStartInfo(result.AuthUrl) { UseShellExecute = true });
                status.Foreground = GreenText;
                status.Text = "Autorize no Mercado Pago. O PDV vai confirmar sozinho quando a conta conectar.";
                _ = AutoRefreshMercadoPagoUntilConnectedAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                status.Foreground = RedText;
                status.Text = $"Nao consegui abrir o navegador: {ex.Message}";
            }
            finally
            {
                mpConnect.IsEnabled = true;
                RefreshMercadoPagoCard();
            }
        };

        mpRefresh.Click += async (_, _) =>
        {
            status.Foreground = Solid("#5B6B7A");
            status.Text = "Consultando conta e maquininhas Mercado Pago...";
            mpRefresh.IsEnabled = false;
            try
            {
                await RefreshMercadoPagoOnlineAsync(loadTerminals: true);
            }
            finally
            {
                mpRefresh.IsEnabled = true;
                RefreshMercadoPagoCard();
            }
        };

        mpSaveTerminal.Click += async (_, _) =>
        {
            if (mpTerminalBox.SelectedItem is not AdminMercadoPagoTerminalDto terminal || string.IsNullOrWhiteSpace(terminal.Id))
            {
                status.Foreground = RedText;
                status.Text = "Escolha uma maquininha Mercado Pago.";
                return;
            }

            status.Foreground = Solid("#5B6B7A");
            status.Text = "Salvando maquininha da loja...";
            mpSaveTerminal.IsEnabled = false;
            try
            {
                var result = await SelectMercadoPagoTerminalAsync(terminal.Id, terminal.Display);
                if (result.Ok)
                {
                    var mp = _appSettings.MercadoPago ??= new MercadoPagoPaymentSettings();
                    mp.Enabled = true;
                    mp.Connected = true;
                    mp.DefaultTerminalId = terminal.Id;
                    mp.DefaultTerminalLabel = terminal.Display;
                    mp.LastSyncAt = DateTime.Now;
                    mp.LastError = "";
                    SaveAppSettings();
                    status.Foreground = GreenText;
                    status.Text = "Maquininha salva. No F9 voce pode escolher Point, QR Pix ou link.";
                    RefreshMercadoPagoCard();
                    return;
                }

                status.Foreground = RedText;
                status.Text = TextOrDefault(result.Message, "Nao consegui salvar a maquininha.");
            }
            finally
            {
                mpSaveTerminal.IsEnabled = true;
                RefreshMercadoPagoCard();
            }
        };

        var mpActionGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 0, 0, 10) };
        var mpTerminalActionGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 0, 0, 0) };
        mpConnect.HorizontalAlignment = HorizontalAlignment.Stretch;
        mpRefresh.HorizontalAlignment = HorizontalAlignment.Stretch;
        mpSaveTerminal.HorizontalAlignment = HorizontalAlignment.Stretch;
        mpToggle.HorizontalAlignment = HorizontalAlignment.Stretch;
        mpConnect.Width = double.NaN;
        mpRefresh.Width = double.NaN;
        mpSaveTerminal.Width = double.NaN;
        mpToggle.Width = double.NaN;
        mpConnect.Margin = new Thickness(0, 0, 6, 0);
        mpRefresh.Margin = new Thickness(6, 0, 0, 0);
        mpSaveTerminal.Margin = new Thickness(0, 0, 6, 0);
        mpToggle.Margin = new Thickness(6, 0, 0, 0);
        mpActionGrid.Children.Add(mpConnect);
        mpActionGrid.Children.Add(mpRefresh);
        mpTerminalActionGrid.Children.Add(mpSaveTerminal);
        mpTerminalActionGrid.Children.Add(mpToggle);
        var mpCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#D6E2EA"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Mercado Pago", Foreground = Solid("#071A2C"), FontSize = 18, FontWeight = FontWeights.Bold },
                    new TextBlock { Text = "Cobranca integrada no F9 com confirmacao antes de fechar a venda.", Foreground = Solid("#5B6B7A"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 10) },
                    mpPillGrid,
                    mpStatusText,
                    mpFlowGrid,
                    mpPointNote,
                    mpActionGrid,
                    mpTerminalField,
                    mpTerminalActionGrid
                }
            }
        };
        RefreshMercadoPagoCard();

        var pgStatusText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var pgComPortBox = new TextBox
        {
            Text = _appSettings.PagBank?.PlugPagComPort ?? "",
            Margin = new Thickness(0, 4, 0, 10)
        };
        var pgTerminalBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(_appSettings.PagBank?.DefaultTerminalLabel)
                ? "Moderninha PlugPag"
                : _appSettings.PagBank.DefaultTerminalLabel,
            Margin = new Thickness(0, 4, 0, 10)
        };
        var pgTerminalField = DialogField("Nome da maquininha", pgTerminalBox);
        var pgComPortField = DialogField("Porta Bluetooth COM", pgComPortBox);
        var pgConnect = DialogButton("Conectar conta", "#0B3A52");
        var pgRefresh = DialogButton("Atualizar conta", "#0B3A52");
        var pgToggle = DialogButton("Ativar no F9", "#0B3A52");
        var pgSaveTerminal = DialogButton("Salvar PlugPag", "#0B3A52");
        var pgPlugPagToggle = DialogButton("Ativar maquininha", "#99620D");
        var pgConnectionPillText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var pgTerminalPillText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var pgConnectionPill = new Border
        {
            Background = Solid("#EEF4F8"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 6, 8),
            Child = pgConnectionPillText
        };
        var pgTerminalPill = new Border
        {
            Background = Solid("#EEF4F8"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(6, 0, 0, 8),
            Child = pgTerminalPillText
        };
        var pgPillGrid = new UniformGrid { Columns = 2, Rows = 1 };
        pgPillGrid.Children.Add(pgConnectionPill);
        pgPillGrid.Children.Add(pgTerminalPill);
        var pgCustomerExplanation = new Border
        {
            Background = Solid("#F4F8FB"),
            BorderBrush = Solid("#D6E2EA"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 2, 0, 10),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Em breve", Foreground = Solid("#0B3A52"), FontSize = 13, FontWeight = FontWeights.Bold },
                    new TextBlock { Text = "O PagBank vai ficar bloqueado ate a integracao estar pronta. Por enquanto, mantenha Mercado Pago ou recebimento manual.", Foreground = Solid("#435466"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) }
                }
            }
        };
        var pgPointNote = new TextBlock
        {
            Text = "Quando for liberado, o PDV mostrara aqui a conexao da conta e a configuracao da maquininha.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var pgAutoRefreshRunning = false;

        void RefreshPagBankCard()
        {
            var pg = _appSettings.PagBank ??= new PagBankPaymentSettings();
            if (!PagBankIntegrationAvailable)
            {
                pg.Enabled = false;
                pg.PlugPagEnabled = false;
                pgStatusText.Text = "Em breve. A integracao PagBank ainda nao esta liberada nesta versao do PDV.";
                pgStatusText.Foreground = Solid("#5B6B7A");
                pgToggle.Content = "Em breve";
                pgConnect.Content = "Em breve";
                pgRefresh.Content = "Em breve";
                pgPlugPagToggle.Content = "Em breve";
                pgSaveTerminal.Content = "Em breve";
                foreach (var control in new Control[] { pgTerminalBox, pgComPortBox, pgToggle, pgConnect, pgRefresh, pgPlugPagToggle, pgSaveTerminal })
                {
                    control.IsEnabled = false;
                }

                pgConnectionPillText.Text = "Em breve";
                pgConnectionPillText.Foreground = Solid("#0B3A52");
                pgConnectionPill.Background = Solid("#F1F5F8");
                pgTerminalPillText.Text = "Bloqueado";
                pgTerminalPillText.Foreground = Solid("#5B6B7A");
                pgTerminalPill.Background = Solid("#F1F5F8");
                return;
            }

            var terminal = string.IsNullOrWhiteSpace(pg.DefaultTerminalLabel)
                ? pg.DefaultTerminalId
                : pg.DefaultTerminalLabel;
            var plugPagReady = pg.PlugPagEnabled && !string.IsNullOrWhiteSpace(pg.PlugPagComPort);
            pgStatusText.Text = pg.Connected
                ? plugPagReady
                    ? $"Conta conectada. Online ativo e Moderninha em {pg.PlugPagComPort} pronta para credito/debito."
                    : "Conta conectada. PagBank online ja usa Pix QR/link; informe a porta COM para ativar a Moderninha PlugPag."
                : "Conecte a conta PagBank da loja para usar Pix QR/link. Depois informe a porta COM da Moderninha para credito/debito integrado.";
            if (!string.IsNullOrWhiteSpace(pg.LastError))
            {
                pgStatusText.Text += $" Ultimo erro: {pg.LastError}";
            }

            pgStatusText.Foreground = pg.Enabled && pg.Connected ? GreenText : Solid("#5B6B7A");
            pgToggle.Content = pg.Enabled ? "Desativar no F9" : "Ativar no F9";
            pgToggle.Background = pg.Enabled ? Solid("#5B6B7A") : Solid("#0B3A52");
            pgPlugPagToggle.Content = pg.PlugPagEnabled ? "Desativar maquininha" : "Ativar maquininha";
            pgPlugPagToggle.Background = pg.PlugPagEnabled ? Solid("#5B6B7A") : Solid("#99620D");
            pgConnect.Content = pg.Connected ? "Reconectar conta" : "Conectar conta";
            pgConnectionPillText.Text = pg.Connected
                ? pg.Enabled ? "Ativo no F9" : "Conectado, desligado"
                : "Conta nao conectada";
            pgConnectionPillText.Foreground = pg.Connected && pg.Enabled ? GreenText : Solid("#5B6B7A");
            pgConnectionPill.Background = pg.Connected && pg.Enabled ? Solid("#E6F6F2") : Solid("#F1F5F8");
            pgTerminalPillText.Text = plugPagReady
                ? $"PlugPag: {pg.PlugPagComPort}"
                : "Sem maquininha PlugPag";
            pgTerminalPillText.Foreground = plugPagReady ? Solid("#99620D") : Solid("#5B6B7A");
            pgTerminalPill.Background = plugPagReady ? Solid("#FFF4D6") : Solid("#F1F5F8");
            pgComPortBox.Text = pg.PlugPagComPort;
            if (!string.IsNullOrWhiteSpace(terminal))
            {
                pgTerminalBox.Text = terminal;
            }
        }

        async Task RefreshPagBankOnlineAsync()
        {
            var connection = await FetchPagBankConnectionStatusAsync();
            ApplyPagBankStatus(connection);
            if (!connection.Ok)
            {
                status.Foreground = RedText;
                status.Text = TextOrDefault(connection.Message, "Nao consegui consultar o PagBank agora.");
                RefreshPagBankCard();
                return;
            }

            status.Foreground = connection.Connected ? GreenText : Solid("#99620D");
            status.Text = connection.Connected
                ? "Conta PagBank conectada. Pix/link online estao prontos."
                : "PagBank ainda nao conectado.";
            SaveAppSettings();
            RefreshPagBankCard();
        }

        async Task AutoRefreshPagBankUntilConnectedAsync()
        {
            if (pgAutoRefreshRunning)
            {
                return;
            }

            pgAutoRefreshRunning = true;
            try
            {
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    await Task.Delay(attempt == 0 ? 1800 : 3000);
                    if (!dialog.IsVisible)
                    {
                        return;
                    }

                    status.Foreground = Solid("#5B6B7A");
                    status.Text = "Aguardando autorizacao do PagBank...";
                    await RefreshPagBankOnlineAsync();
                    if (_appSettings.PagBank?.Connected == true)
                    {
                        status.Foreground = GreenText;
                        status.Text = "PagBank confirmado. Configure a porta COM se for usar Moderninha PlugPag.";
                        return;
                    }
                }

                status.Foreground = Solid("#99620D");
                status.Text = "Ainda nao confirmei o PagBank. Se voce terminou no navegador, clique em Atualizar conta.";
            }
            finally
            {
                pgAutoRefreshRunning = false;
                RefreshPagBankCard();
            }
        }

        pgToggle.Click += (_, _) =>
        {
            if (!PagBankIntegrationAvailable)
            {
                status.Foreground = Solid("#5B6B7A");
                status.Text = "PagBank esta em breve.";
                return;
            }

            var pg = _appSettings.PagBank ??= new PagBankPaymentSettings();
            pg.Enabled = !pg.Enabled;
            SaveAppSettings();
            status.Foreground = pg.Enabled ? GreenText : Solid("#5B6B7A");
            status.Text = pg.Enabled
                ? "PagBank ativado no F9."
                : "PagBank desativado no F9. As outras formas de pagamento continuam normais.";
            RefreshPagBankCard();
        };
        pgPlugPagToggle.Click += (_, _) =>
        {
            if (!PagBankIntegrationAvailable)
            {
                status.Foreground = Solid("#5B6B7A");
                status.Text = "Maquininha PagBank esta em breve.";
                return;
            }

            var pg = _appSettings.PagBank ??= new PagBankPaymentSettings();
            pg.PlugPagEnabled = !pg.PlugPagEnabled;
            SaveAppSettings();
            status.Foreground = pg.PlugPagEnabled ? GreenText : Solid("#5B6B7A");
            status.Text = pg.PlugPagEnabled
                ? "Maquininha PagBank ativada. Salve a porta COM da Moderninha."
                : "Maquininha PagBank desativada. PagBank online continua disponivel.";
            RefreshPagBankCard();
        };
        pgConnect.Click += async (_, _) =>
        {
            if (!PagBankIntegrationAvailable)
            {
                status.Foreground = Solid("#5B6B7A");
                status.Text = "Conexao PagBank esta em breve.";
                return;
            }

            if (!TryApplyProfileInputs())
            {
                return;
            }

            (_appSettings.PagBank ??= new PagBankPaymentSettings()).Enabled = true;
            SaveRestaurantProfile();
            SaveAppSettings();
            status.Foreground = Solid("#5B6B7A");
            status.Text = "Abrindo PagBank para conectar a conta da loja...";
            pgConnect.IsEnabled = false;
            try
            {
                var result = await StartPagBankConnectAsync();
                if (!result.Ok || string.IsNullOrWhiteSpace(result.AuthUrl))
                {
                    status.Foreground = RedText;
                    status.Text = TextOrDefault(result.Message, "Nao consegui iniciar a conexao PagBank.");
                    return;
                }

                Process.Start(new ProcessStartInfo(result.AuthUrl) { UseShellExecute = true });
                status.Foreground = GreenText;
                status.Text = "Autorize no PagBank. O PDV vai confirmar sozinho quando a conta conectar.";
                _ = AutoRefreshPagBankUntilConnectedAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                status.Foreground = RedText;
                status.Text = $"Nao consegui abrir o navegador: {ex.Message}";
            }
            finally
            {
                pgConnect.IsEnabled = true;
                RefreshPagBankCard();
            }
        };
        pgRefresh.Click += async (_, _) =>
        {
            if (!PagBankIntegrationAvailable)
            {
                status.Foreground = Solid("#5B6B7A");
                status.Text = "Atualizacao PagBank esta em breve.";
                return;
            }

            status.Foreground = Solid("#5B6B7A");
            status.Text = "Consultando conta PagBank...";
            pgRefresh.IsEnabled = false;
            try
            {
                await RefreshPagBankOnlineAsync();
            }
            finally
            {
                pgRefresh.IsEnabled = true;
                RefreshPagBankCard();
            }
        };
        pgSaveTerminal.Click += async (_, _) =>
        {
            if (!PagBankIntegrationAvailable)
            {
                status.Foreground = Solid("#5B6B7A");
                status.Text = "PlugPag PagBank esta em breve.";
                return;
            }

            var pg = _appSettings.PagBank ??= new PagBankPaymentSettings();
            var comPort = CompactSingleLine(pgComPortBox.Text).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(comPort))
            {
                status.Foreground = RedText;
                status.Text = "Informe a porta COM da Moderninha, exemplo COM5.";
                pgComPortBox.Focus();
                return;
            }

            pg.DefaultTerminalLabel = CompactSingleLine(pgTerminalBox.Text);
            pg.DefaultTerminalId = string.IsNullOrWhiteSpace(pg.DefaultTerminalLabel)
                ? "PLUGPAG"
                : pg.DefaultTerminalLabel;
            pg.PlugPagComPort = comPort;
            pg.PlugPagEnabled = true;
            SaveAppSettings();
            status.Foreground = Solid("#5B6B7A");
            status.Text = "Salvando Moderninha PlugPag...";
            pgSaveTerminal.IsEnabled = false;
            try
            {
                var result = await SelectPagBankTerminalAsync(pg.DefaultTerminalId, pg.DefaultTerminalLabel, comPort);
                status.Foreground = result.Ok ? GreenText : Solid("#99620D");
                status.Text = result.Ok
                    ? "Moderninha salva. No F9, credito/debito podem sair pelo PlugPag."
                    : TextOrDefault(result.Message, "Salvei localmente, mas nao consegui sincronizar com o Supabase agora.");
            }
            finally
            {
                pgSaveTerminal.IsEnabled = true;
                RefreshPagBankCard();
            }
        };

        var pgActionGrid = new UniformGrid { Columns = 3, Rows = 1, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var button in new[] { pgToggle, pgConnect, pgRefresh, pgPlugPagToggle, pgSaveTerminal })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }
        pgToggle.Margin = new Thickness(0, 0, 6, 0);
        pgConnect.Margin = new Thickness(6, 0, 6, 0);
        pgRefresh.Margin = new Thickness(6, 0, 0, 0);
        pgPlugPagToggle.Margin = new Thickness(0, 0, 6, 8);
        pgSaveTerminal.Margin = new Thickness(6, 0, 0, 8);
        pgActionGrid.Children.Add(pgToggle);
        pgActionGrid.Children.Add(pgConnect);
        pgActionGrid.Children.Add(pgRefresh);
        var pgPlugPagGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 0, 0, 0) };
        pgPlugPagGrid.Children.Add(pgPlugPagToggle);
        pgPlugPagGrid.Children.Add(pgSaveTerminal);
        var pgCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#D6E2EA"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "PagBank", Foreground = Solid("#071A2C"), FontSize = 18, FontWeight = FontWeights.Bold },
                    new TextBlock { Text = "Pix/link online pela conta PagBank da loja. Credito/debito presencial usam PlugPag quando a Moderninha estiver pareada no Windows.", Foreground = Solid("#5B6B7A"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 10) },
                    pgPillGrid,
                    pgStatusText,
                    pgCustomerExplanation,
                    pgPointNote,
                    pgTerminalField,
                    pgComPortField,
                    pgActionGrid,
                    pgPlugPagGrid
                }
            }
        };
        RefreshPagBankCard();

        var soundGrid = new UniformGrid { Columns = 4, Rows = 1, Margin = new Thickness(0, 6, 0, 4) };
        foreach (var item in new[] { ("Padrao", "PADRAO"), ("Aviso", "AVISO"), ("Erro", "ERRO"), ("Nenhum", "NENHUM") })
        {
            var button = SegmentButton(item.Item1, item.Item2);
            button.Click += (_, _) =>
            {
                _appSettings.NotificationSound = item.Item2;
                RefreshSegments(soundButtons, _appSettings.NotificationSound, "#0B3A52");
            };
            soundButtons.Add(button);
            soundGrid.Children.Add(button);
        }

        var sizeGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 6, 0, 4) };
        foreach (var size in new[] { "PEQUENO", "GRANDE" })
        {
            var button = SegmentButton(size, size);
            button.Click += (_, _) =>
            {
                _appSettings.PrintLayout = size;
                RefreshSegments(printSizeButtons, _appSettings.PrintLayout, "#0B3A52");
            };
            printSizeButtons.Add(button);
            sizeGrid.Children.Add(button);
        }

        void RefreshPrinterCard()
        {
            printerSelectedText.Text = string.Equals(selectedPrinter, defaultPrinterOption, StringComparison.Ordinal)
                ? "Usando impressora padrao do Windows"
                : selectedPrinter;
        }

        var useDefaultPrinter = DialogButton("Usar padrao", "#5B6B7A");
        useDefaultPrinter.MinWidth = 120;
        useDefaultPrinter.Click += (_, _) =>
        {
            selectedPrinter = defaultPrinterOption;
            RefreshPrinterCard();
            status.Text = "Impressora voltou para o padrao do Windows. Clique em Salvar configuracoes.";
        };

        var choosePrinter = DialogButton("Escolher impressora", "#0B3A52");
        choosePrinter.MinWidth = 170;
        choosePrinter.Click += (_, _) =>
        {
            var printerDialog = CreateDialog("Escolher impressora", 520, 520);
            var list = new ListBox
            {
                ItemsSource = installedPrinters,
                SelectedItem = selectedPrinter,
                MinHeight = 300,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };
            var apply = DialogButton("Aplicar impressora", "#08A99B");
            apply.Click += (_, _) =>
            {
                selectedPrinter = list.SelectedItem?.ToString() ?? defaultPrinterOption;
                RefreshPrinterCard();
                status.Text = "Impressora selecionada. Clique em Salvar configuracoes.";
                printerDialog.Close();
            };

            var pickerPanel = DialogPanel();
            pickerPanel.Children.Add(SectionTitle("Impressora preferida"));
            pickerPanel.Children.Add(DialogHint("Escolha a impressora que o PDV deve usar. Se ficar no padrao, ele acompanha a impressora padrao do Windows."));
            pickerPanel.Children.Add(list);
            apply.HorizontalAlignment = HorizontalAlignment.Stretch;
            apply.Margin = new Thickness(0, 12, 0, 0);
            pickerPanel.Children.Add(apply);
            printerDialog.Content = pickerPanel;
            printerDialog.ShowDialog();
        };

        var printerActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        choosePrinter.Margin = new Thickness(0, 0, 8, 0);
        printerActions.Children.Add(choosePrinter);
        printerActions.Children.Add(useDefaultPrinter);
        var printerCard = new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 10),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Impressora preferida", Foreground = Solid("#5B6B7A"), FontWeight = FontWeights.SemiBold },
                    printerSelectedText,
                    defaultPrinterText,
                    printerActions
                }
            }
        };
        RefreshPrinterCard();

        void AddSectorPrinterRow(string sector = "", string printerName = "")
        {
            var selected = string.IsNullOrWhiteSpace(printerName)
                ? defaultPrinterOption
                : printerName.Trim();
            if (installedPrinters.All(item => !string.Equals(item, selected, StringComparison.OrdinalIgnoreCase)))
            {
                installedPrinters.Add(selected);
            }

            var sectorBox = new TextBox
            {
                Text = NormalizeProductDestination(sector, ""),
                CharacterCasing = System.Windows.Controls.CharacterCasing.Upper,
                MinHeight = 34,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var printerBox = new ComboBox
            {
                ItemsSource = installedPrinters.ToList(),
                SelectedItem = selected,
                MinHeight = 34,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var remove = DialogButton("Remover", "#5B6B7A");
            remove.MinWidth = 86;
            remove.Height = 34;

            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(sectorBox);
            Grid.SetColumn(printerBox, 1);
            row.Children.Add(printerBox);
            Grid.SetColumn(remove, 2);
            row.Children.Add(remove);

            sectorPrinterRows.Add((sectorBox, printerBox));
            sectorPrinterList.Children.Add(row);
            remove.Click += (_, _) =>
            {
                sectorPrinterRows.RemoveAll(item => ReferenceEquals(item.SectorBox, sectorBox));
                sectorPrinterList.Children.Remove(row);
                status.Text = "Destino removido. Clique em Salvar configuracoes.";
            };
        }

        Border BuildSectorPrintersCard()
        {
            sectorPrinterRows.Clear();
            sectorPrinterList.Children.Clear();
            foreach (var setting in _appSettings.SectorPrinters
                         .Where(item => !string.IsNullOrWhiteSpace(item.Sector))
                          .OrderBy(item => string.Equals(NormalizeProductDestination(item.Sector, ""), "COZINHA", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                          .ThenBy(item => NormalizeProductDestination(item.Sector, ""), StringComparer.OrdinalIgnoreCase))
            {
                AddSectorPrinterRow(setting.Sector, setting.PrinterName);
            }

            var addSector = DialogButton("Adicionar destino", "#0B3A52");
            addSector.HorizontalAlignment = HorizontalAlignment.Left;
            addSector.Click += (_, _) =>
            {
                AddSectorPrinterRow();
                status.Text = "Informe o nome do destino, escolha a impressora e salve.";
            };

            return new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 10),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Destinos de producao", Foreground = Solid("#5B6B7A"), FontWeight = FontWeights.SemiBold },
                        new TextBlock { Text = "COZINHA ja vem criada. Produto em CAIXA nao imprime producao; produto em COZINHA ou outro destino sai agrupado na impressora escolhida.", Foreground = Solid("#5B6B7A"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 8) },
                        sectorPrinterList,
                        addSector
                    }
                }
            };
        }

        var sectorPrintersCard = BuildSectorPrintersCard();

        var qrKindButtons = new List<Button>();
        var qrKindGrid = new UniformGrid { Columns = 2, Rows = 2, Margin = new Thickness(0, 8, 0, 4) };
        foreach (var kind in new[] { "PIX", "INSTAGRAM", "GOOGLE MAPS", "LINK" })
        {
            var button = SegmentButton(kind, kind);
            button.Click += (_, _) =>
            {
                selectedQrKind = kind;
                RefreshSegments(qrKindButtons, selectedQrKind, "#08A99B");
                status.Text = "Tipo do QR atualizado. Clique em Salvar configuracoes.";
            };
            qrKindButtons.Add(button);
            qrKindGrid.Children.Add(button);
        }

        var qrTitle = new TextBlock { FontWeight = FontWeights.Bold, Foreground = Solid("#071A2C") };
        var qrSubtitle = new TextBlock { Foreground = Solid("#5B6B7A"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 12, 0) };
        var qrToggle = DialogButton("Ligar QR", "#0B3A52");
        qrToggle.MinWidth = 110;
        var qrHeader = new Grid();
        qrHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        qrHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        qrHeader.Children.Add(new StackPanel { Children = { qrTitle, qrSubtitle } });
        Grid.SetColumn(qrToggle, 1);
        qrHeader.Children.Add(qrToggle);
        var qrOptionsPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        qrOptionsPanel.Children.Add(DialogLabel("Tipo do QR"));
        qrOptionsPanel.Children.Add(qrKindGrid);
        qrOptionsPanel.Children.Add(DialogLabel("Conteudo do QR"));
        qrOptionsPanel.Children.Add(qrContentBox);
        qrOptionsPanel.Children.Add(qrHint);
        var qrCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 10),
            Child = new StackPanel { Children = { qrHeader, qrOptionsPanel } }
        };

        void RefreshQrCard()
        {
            var enabled = _appSettings.ReceiptQrEnabled;
            qrCard.Background = enabled ? Solid("#F2FBFC") : Brushes.White;
            qrCard.BorderBrush = enabled ? Solid("#20C8BE") : Solid("#CAD6E2");
            qrTitle.Foreground = enabled ? Solid("#03151F") : Solid("#071A2C");
            qrTitle.Text = enabled ? "QR no comprovante: ligado" : "QR no comprovante: desligado";
            qrSubtitle.Text = enabled
                ? "O comprovante imprime o QR usando o conteudo abaixo. Para Pix, informe sua chave Pix."
                : "Desligado para nao poluir o comprovante. Ligue somente quando usar Pix, Instagram, Maps ou link.";
            qrToggle.Content = enabled ? "Desligar QR" : "Ligar QR";
            qrToggle.Background = enabled ? Solid("#5B6B7A") : Solid("#0B3A52");
            qrOptionsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        qrToggle.Click += (_, _) =>
        {
            _appSettings.ReceiptQrEnabled = !_appSettings.ReceiptQrEnabled;
            RefreshQrCard();
            status.Text = _appSettings.ReceiptQrEnabled
                ? "QR ligado. Informe o conteudo para Pix/link ou deixe vazio para QR com dados da loja. Depois salve."
                : "QR desligado. Clique em Salvar configuracoes.";
        };

        var chooseLogo = DialogButton("Trocar foto/logo", "#0B3A52");
        chooseLogo.HorizontalAlignment = HorizontalAlignment.Stretch;
        chooseLogo.Click += (_, _) =>
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Escolher foto/logo do restaurante",
                Filter = "Imagens|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos os arquivos|*.*",
                Multiselect = false
            };

            if (fileDialog.ShowDialog(this) != true)
            {
                return;
            }

            logoPath = CopyLogoToAppIdentityFolder(fileDialog.FileName);
            logoText.Text = Path.GetFileName(logoPath);
            profile.LocalLogoPath = logoPath;
            _profile.LocalLogoPath = logoPath;
            SaveRestaurantProfile();
            ApplyBrandLogo(logoPath);
            SaveStore();
            status.Text = "Foto/logo aplicada no topo do app.";
            SetStatus("Foto/logo do restaurante atualizada.");
        };

        var chooseCover = DialogButton("Trocar imagem de capa", "#0B3A52");
        chooseCover.HorizontalAlignment = HorizontalAlignment.Stretch;
        chooseCover.Click += (_, _) =>
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Escolher imagem de capa do cardapio",
                Filter = "Imagens|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos os arquivos|*.*",
                Multiselect = false
            };

            if (fileDialog.ShowDialog(this) != true)
            {
                return;
            }

            coverPath = CopyImageToAppIdentityFolder(fileDialog.FileName, "restaurant-cover");
            coverText.Text = Path.GetFileName(coverPath);
            profile.LocalCoverPath = coverPath;
            _profile.LocalCoverPath = coverPath;
            SaveRestaurantProfile();
            SaveStore();
            status.Text = "Imagem de capa aplicada no cardapio digital.";
            SetStatus("Capa do cardapio atualizada.");
        };

        var chooseIFoodSound = DialogButton("Escolher toque iFood", "#0B3A52");
        chooseIFoodSound.HorizontalAlignment = HorizontalAlignment.Stretch;
        chooseIFoodSound.Click += (_, _) =>
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Escolher toque para pedido iFood",
                Filter = "Audio|*.wav;*.mp3;*.wma;*.m4a;*.aac|Todos os arquivos|*.*",
                Multiselect = false
            };

            if (fileDialog.ShowDialog(this) != true)
            {
                return;
            }

            ifoodSoundPath = CopyNotificationSoundToAppFolder(fileDialog.FileName);
            ifoodSoundText.Text = Path.GetFileName(ifoodSoundPath);
            status.Text = "Toque iFood selecionado. Clique em Salvar configuracoes.";
        };

        var clearIFoodSound = DialogButton("Usar toque padrao", "#5B6B7A");
        clearIFoodSound.HorizontalAlignment = HorizontalAlignment.Stretch;
        clearIFoodSound.Click += (_, _) =>
        {
            ifoodSoundPath = "";
            ifoodSoundText.Text = "Toque iFood: alerta de pedido do app";
            status.Text = "Toque iFood voltou para o alerta de pedido do app. Clique em Salvar configuracoes.";
        };

        var testIFoodSound = DialogButton("Testar toque iFood", "#99620D");
        testIFoodSound.HorizontalAlignment = HorizontalAlignment.Stretch;
        testIFoodSound.Click += (_, _) =>
        {
            _appSettings.IFoodAlertSoundPath = ifoodSoundPath;
            PlayIFoodOrderSound();
            VibrateInApp();
            status.Text = "Toque iFood testado.";
        };

        var testNotification = DialogButton("Testar notificacao", "#0B3A52");
        testNotification.HorizontalAlignment = HorizontalAlignment.Stretch;
        testNotification.Click += (_, _) =>
        {
            ShowToast("Teste de aviso", "Aviso visual e vibracao curta foram testados.", "NT", "#0B3A52", "#EAF8FA");
            status.Text = "Notificacao de teste enviada.";
        };
        var checkUpdate = DialogButton("Verificar atualizacao agora", "#0B3A52");
        checkUpdate.HorizontalAlignment = HorizontalAlignment.Stretch;
        checkUpdate.Click += async (_, _) =>
        {
            _appSettings.UpdateManifestUrl = DefaultUpdateManifestUrl;
            SaveAppSettings();
            await CheckForUpdatesAsync(showIfCurrent: true);
        };

        bool TryApplyProfileInputs()
        {
            var accountEmail = emailBox.Text.Trim().ToLowerInvariant();
            var businessName = businessBox.Text.Trim();
            var ownerName = ownerBox.Text.Trim();
            var cnpj = cnpjBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(accountEmail))
            {
                status.Foreground = RedText;
                status.Text = "Informe o email da conta. Ele cria/vincula o login no Supabase para Android e PDV Web.";
                emailBox.Focus();
                return false;
            }

            if (!IsReasonableEmail(accountEmail))
            {
                status.Foreground = RedText;
                status.Text = "Informe um email valido para vincular a conta.";
                emailBox.Focus();
                emailBox.SelectAll();
                return false;
            }

            if (string.IsNullOrWhiteSpace(businessName) && string.IsNullOrWhiteSpace(ownerName))
            {
                status.Foreground = RedText;
                status.Text = "Informe o nome da loja ou do responsavel.";
                businessBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cnpj))
            {
                status.Foreground = RedText;
                status.Text = "Informe o CNPJ para vincular a conta.";
                cnpjBox.Focus();
                return false;
            }

            var locationChanged = !string.Equals(_profile.Address, addressBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_profile.City, cityBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_profile.State, stateBox.Text.Trim(), StringComparison.OrdinalIgnoreCase);

            profile.Email = accountEmail;
            profile.OwnerName = ownerName;
            profile.BusinessName = businessName;
            profile.LegalName = legalBox.Text.Trim();
            profile.Cnpj = cnpj;
            profile.Phone = phoneBox.Text.Trim();
            profile.Address = addressBox.Text.Trim();
            profile.City = cityBox.Text.Trim();
            profile.State = stateBox.Text.Trim().ToUpperInvariant();
            profile.Latitude = locationChanged ? 0 : _profile.Latitude;
            profile.Longitude = locationChanged ? 0 : _profile.Longitude;
            profile.LocalLogoPath = logoPath;
            profile.LocalCoverPath = coverPath;
            _profile = profile;
            linkedAccountText.Text = string.IsNullOrWhiteSpace(profile.Email)
                ? "Nenhum email de conta vinculado."
                : $"Conta vinculada: {profile.Email}";
            return true;
        }

        var syncAccount = DialogButton("Sincronizar conta agora", "#0B3A52");
        syncAccount.HorizontalAlignment = HorizontalAlignment.Stretch;
        syncAccount.Click += async (_, _) =>
        {
            if (!TryApplyProfileInputs())
            {
                return;
            }

            SaveRestaurantProfile();
            SaveAppSettings();
            var payload = CreateAdminClientPayload("profile.sync", _appSettings.ActivationKey, _appSettings.ActivationExpiresAt, _appSettings.ActivationPlan);
            status.Foreground = Solid("#5B6B7A");
            status.Text = "Sincronizando conta com o admin/Supabase...";
            syncAccount.IsEnabled = false;
            try
            {
                if (await SendAdminCheckInAsync(payload))
                {
                    _appSettings.LastAdminSyncAt = DateTime.Now;
                    SaveAppSettings();
                    ApplyRestaurantIdentity();
                    licenseText.Text = BuildActivationSummary();
                    status.Foreground = GreenText;
                    status.Text = "Conta sincronizada. Android e Web usam esses dados vinculados a chave.";
                }
                else
                {
                    status.Foreground = RedText;
                    status.Text = "Nao consegui sincronizar agora. Verifique internet/admin.";
                }
            }
            finally
            {
                syncAccount.IsEnabled = true;
            }
        };

        var logoutAccount = DialogButton("Sair desta conta", "#A11D1D");
        logoutAccount.HorizontalAlignment = HorizontalAlignment.Stretch;
        logoutAccount.Click += (_, _) =>
        {
            var confirm = System.Windows.MessageBox.Show(
                this,
                "Deseja sair desta conta neste computador? A chave sera removida desta instalacao e o PDV pedira ativacao/login novamente.",
                "Sair da conta",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _appSettings.ActivationCompleted = false;
            _appSettings.ActivationKey = "";
            _appSettings.ActivationPlan = "";
            _appSettings.ActivationActivatedAt = null;
            _appSettings.ActivationExpiresAt = null;
            _appSettings.ActivationMachineHash = "";
            _appSettings.ActivationLastWarningKey = "";
            _appSettings.LastAdminSyncAt = null;
            SaveAppSettings();
            ApplyRestaurantIdentity();
            _licenseTimer.Stop();
            SetStatus("Conta removida deste computador.");
            dialog.Close();
            if (!ShowInstallSetupDialog())
            {
                _exitRequested = true;
                Application.Current.Shutdown();
            }
        };

        var save = DialogButton("Salvar configuracoes", "#03151F");
        save.HorizontalAlignment = HorizontalAlignment.Stretch;
        save.Click += (_, _) =>
        {
            if (!TryApplyProfileInputs())
            {
                return;
            }

            _appSettings.PreferredPrinterName = string.Equals(selectedPrinter, defaultPrinterOption, StringComparison.Ordinal)
                ? ""
                : selectedPrinter.Trim();
            _appSettings.SectorPrinters = sectorPrinterRows
                .Select(row => new SectorPrinterSetting
                {
                    Sector = NormalizeProductDestination(row.SectorBox.Text, ""),
                    PrinterName = string.Equals(row.PrinterBox.SelectedItem?.ToString(), defaultPrinterOption, StringComparison.Ordinal)
                        ? ""
                        : (row.PrinterBox.SelectedItem?.ToString() ?? "").Trim()
                })
                .Where(setting => !string.IsNullOrWhiteSpace(setting.Sector))
                .Where(setting => IsProductionDestinationName(setting.Sector))
                .GroupBy(setting => NormalizeProductDestination(setting.Sector, ""), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            NormalizeProductionDestinations();
            _appSettings.UpdateManifestUrl = DefaultUpdateManifestUrl;
            _appSettings.ReceiptQrKind = NormalizeReceiptQrKind(selectedQrKind);
            _appSettings.ReceiptQrContent = qrContentBox.Text.Trim();
            var waitMin = Math.Max(1, ParseInt(waitMinBox.Text, _appSettings.PublicMenuWaitMinMinutes <= 0 ? 30 : _appSettings.PublicMenuWaitMinMinutes));
            var waitMax = Math.Max(waitMin, ParseInt(waitMaxBox.Text, _appSettings.PublicMenuWaitMaxMinutes <= 0 ? 60 : _appSettings.PublicMenuWaitMaxMinutes));
            _appSettings.PublicMenuWaitMinMinutes = waitMin;
            _appSettings.PublicMenuWaitMaxMinutes = waitMax;
            _appSettings.NotificationSoundEnabled = false;
            _appSettings.NotificationSound = "NENHUM";
            _appSettings.IFoodAlertSoundPath = ifoodSoundPath;
            if (!PagBankIntegrationAvailable && _appSettings.PagBank is { } pagBank)
            {
                pagBank.Enabled = false;
                pagBank.PlugPagEnabled = false;
            }

            SaveRestaurantProfile();
            SaveAppSettings();
            ApplyRestaurantIdentity();
            SaveStore();
            QueueAdminCheckIn("profile.updated", force: true);
            status.Foreground = GreenText;
            status.Text = "Configuracoes salvas.";
            SetStatus("Configuracoes atualizadas.");
            dialog.Close();
        };

        Border SettingsHero()
        {
            var badge = new Border
            {
                Background = Solid("#0B3A52"),
                BorderBrush = Solid("#255665"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 7, 12, 7),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = "BL",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13
                }
            };

            Border HeroChip(string text)
            {
                return new Border
                {
                    Background = Solid("#092B3A"),
                    BorderBrush = Solid("#1D5160"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(999),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 6, 0),
                    Child = new TextBlock
                    {
                        Text = text,
                        Foreground = Solid("#DFFBFA"),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold
                    }
                };
            }

            var chips = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            chips.Children.Add(HeroChip("Loja"));
            chips.Children.Add(HeroChip("Pagamentos"));
            chips.Children.Add(HeroChip("iFood"));
            chips.Children.Add(HeroChip("Impressao"));

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var copy = new StackPanel
            {
                Children =
                {
                    badge,
                    new TextBlock
                    {
                        Text = "Ajustes do PDV",
                        Foreground = Brushes.White,
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 12, 0, 3)
                    },
                    new TextBlock
                    {
                        Text = "Conta, cardapio, pedidos, pagamentos e impressao em um painel mais direto.",
                        Foreground = Solid("#B9D6E3"),
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
            grid.Children.Add(copy);
            Grid.SetColumn(chips, 1);
            grid.Children.Add(chips);

            return new Border
            {
                Background = Solid("#03151F"),
                BorderBrush = Solid("#0E3A4A"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18),
                Child = grid
            };
        }

        Border SettingsCard(string title, string subtitle, params UIElement[] children)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Solid("#071A2C"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 3)
            });
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    Foreground = Solid("#5B6B7A"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }

            foreach (var child in children)
            {
                stack.Children.Add(child);
            }

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#D6E2EA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Child = stack
            };
        }

        TextBlock MutedText(string text, double fontSize = 12)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Solid("#5B6B7A"),
                FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        Border ActionRow(string title, TextBlock detail, Button button)
        {
            detail.Margin = new Thickness(0, 2, 0, 0);
            button.Width = 190;
            button.Height = 36;
            button.Margin = new Thickness(14, 0, 0, 0);
            button.HorizontalAlignment = HorizontalAlignment.Right;
            button.VerticalAlignment = VerticalAlignment.Center;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, Foreground = Solid("#071A2C"), FontWeight = FontWeights.Bold },
                    detail
                }
            });
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);

            return new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#D6E2EA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                Child = grid
            };
        }

        UniformGrid ButtonStrip(params Button[] buttons)
        {
            var grid = new UniformGrid
            {
                Columns = Math.Max(1, buttons.Length),
                Rows = 1,
                Margin = new Thickness(0, 0, 0, 8)
            };
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                button.Width = double.NaN;
                button.Margin = new Thickness(index == 0 ? 0 : 6, 0, index == buttons.Length - 1 ? 0 : 6, 0);
                grid.Children.Add(button);
            }

            return grid;
        }

        var root = new StackPanel
        {
            Margin = new Thickness(18),
            Background = Solid("#EEF4F8")
        };

        void AddCard(StackPanel stack, UIElement element)
        {
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.Margin = new Thickness(0, 0, 0, 14);
            }
            stack.Children.Add(element);
        }

        var siteText = new TextBlock
        {
            Text = "www.balcaolivrepdv.com.br",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.Bold,
            TextDecorations = TextDecorations.Underline,
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 0)
        };
        siteText.MouseLeftButtonDown += (_, _) =>
        {
            Process.Start(new ProcessStartInfo("https://www.balcaolivrepdv.com.br") { UseShellExecute = true });
        };

        root.Children.Add(SettingsHero());

        var accountCard = SettingsCard(
            "Conta e licenca",
            "Vinculo da loja, chave ativa e sincronizacao com os outros aplicativos.",
            new TextBlock { Text = "Conta vinculada", Foreground = Solid("#071A2C"), FontWeight = FontWeights.Bold },
            linkedAccountText,
            licenseText,
            MutedText("Esses dados ficam ligados a chave para o Android, PDV Web e admin reconhecerem a mesma loja.", 11),
            syncAccount,
            logoutAccount);

        var companyCard = SettingsCard(
            "Empresa",
            "Dados usados em recibos, cardapio online, impressao e integracoes.",
            TwoColumnFields(("Email da conta", emailBox), ("Responsavel", ownerBox)),
            TwoColumnFields(("Nome fantasia", businessBox), ("Razao social", legalBox)),
            TwoColumnFields(("CNPJ", cnpjBox), ("Telefone", phoneBox)),
            TwoColumnFields(("Cidade", cityBox), ("UF", stateBox)),
            DialogField("Endereco", addressBox));

        var mediaCard = SettingsCard(
            "Marca e cardapio",
            "Visual da loja, tempo exibido no cardapio e alerta sonoro de pedido.",
            ActionRow("Foto/logo", logoText, chooseLogo),
            ActionRow("Imagem de capa do cardapio", coverText, chooseCover),
            MutedText("O botao Loja online no topo liga/desliga o cardapio e o recebimento iFood. O tempo abaixo aparece no topo do cardapio.", 11),
            TwoColumnFields(("Tempo minimo (min)", waitMinBox), ("Tempo maximo (min)", waitMaxBox)),
            ActionRow("Toque de pedido iFood", ifoodSoundText, chooseIFoodSound),
            ButtonStrip(clearIFoodSound, testIFoodSound));

        var aboutCard = SettingsCard(
            "Sobre o sistema",
            "Informacoes de versao e canais oficiais.",
            MutedText("Versao online: pedidos iFood entram no delivery; venda local continua funcionando mesmo se a internet cair.", 12),
            versionText,
            new TextBlock { Text = "2026 Balcao Livre", Foreground = Solid("#071A2C"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) },
            new TextBlock { Text = "2026 Nagazaki Software", Foreground = Solid("#5B6B7A"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
            MutedText("Site oficial", 11),
            siteText,
            MutedText("Dados usados nos comprovantes, recibos, cardapio online e impressoes locais.", 11));

        var alertsCard = SettingsCard(
            "Avisos do app",
            "Controle os avisos visuais sem poluir a tela de venda.",
            ToggleCard("Toast visual no app", "Mostra aviso dentro do PDV quando uma acao importante acontecer.", () => _appSettings.WindowsNotificationsEnabled, value => _appSettings.WindowsNotificationsEnabled = value),
            ToggleCard("Vibracao no app", "Faz uma vibracao visual curta na janela/toast.", () => _appSettings.InAppVibrationEnabled, value => _appSettings.InAppVibrationEnabled = value),
            testNotification);

        var printCard = SettingsCard(
            "Impressao principal",
            "Padrao de cupom e impressora usada pelo caixa.",
            ToggleCard("Imprimir delivery automaticamente", "Pedidos novos saem na impressora configurada.", () => _appSettings.AutoPrintDelivery, value => _appSettings.AutoPrintDelivery = value),
            ToggleCard("Enviar producao automaticamente", "Itens com destino COZINHA saem agrupados alguns segundos depois de entrar no pedido. F4 fica para reimprimir.", () => _appSettings.AutoPrintKitchen, value => _appSettings.AutoPrintKitchen = value),
            DialogLabel("Modelo padrao"),
            sizeGrid,
            printerCard);

        var productionCard = SettingsCard(
            "Producao e QR",
            "Destinos de cozinha/balcao e QR impresso no comprovante.",
            sectorPrintersCard,
            qrCard);

        var updateCard = SettingsCard(
            "Atualizacoes",
            "Mantenha o PDV atualizado sem procurar instalador manualmente.",
            ToggleCard("Atualizar automaticamente ao abrir", "Consulta o servidor ao entrar no PDV. Se houver versao nova, baixa, instala e reabre o sistema.", () => _appSettings.AutoCheckUpdates, value => _appSettings.AutoCheckUpdates = value),
            checkUpdate);

        var contentGrid = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.08, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.92, GridUnitType.Star) });
        var leftColumn = new StackPanel { Margin = new Thickness(0, 0, 7, 0) };
        var rightColumn = new StackPanel { Margin = new Thickness(7, 0, 0, 0) };
        AddCard(leftColumn, companyCard);
        AddCard(leftColumn, mediaCard);
        AddCard(leftColumn, mpCard);
        AddCard(leftColumn, pgCard);
        AddCard(rightColumn, accountCard);
        AddCard(rightColumn, alertsCard);
        AddCard(rightColumn, printCard);
        AddCard(rightColumn, productionCard);
        AddCard(rightColumn, aboutCard);
        AddCard(rightColumn, updateCard);
        contentGrid.Children.Add(leftColumn);
        Grid.SetColumn(rightColumn, 1);
        contentGrid.Children.Add(rightColumn);
        root.Children.Add(contentGrid);

        var outer = new Grid();
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var scroll = new ScrollViewer
        {
            Content = root,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            PanningMode = PanningMode.VerticalOnly
        };
        outer.Children.Add(scroll);

        save.Width = 240;
        save.Height = 42;
        var footer = new Border
        {
            Background = Solid("#FFFFFF"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(18, 10, 18, 10)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        status.Text = "Altere os dados e clique em Salvar configuracoes.";
        status.Margin = new Thickness(0, 0, 14, 0);
        status.VerticalAlignment = VerticalAlignment.Center;
        footerGrid.Children.Add(status);
        Grid.SetColumn(save, 1);
        footerGrid.Children.Add(save);
        footer.Child = footerGrid;
        Grid.SetRow(footer, 1);
        outer.Children.Add(footer);

        dialog.Content = outer;
        RefreshSegments(soundButtons, _appSettings.NotificationSound, "#0B3A52");
        RefreshSegments(printSizeButtons, _appSettings.PrintLayout, "#0B3A52");
        RefreshSegments(qrKindButtons, selectedQrKind, "#08A99B");
        RefreshQrCard();
        RefreshMercadoPagoCard();
        _ = Dispatcher.BeginInvoke(async () =>
        {
            if ((_appSettings.MercadoPago?.Enabled == true || !string.IsNullOrWhiteSpace(_appSettings.ActivationKey)) && dialog.IsVisible)
            {
                await RefreshMercadoPagoOnlineAsync(loadTerminals: true);
            }
        });
        dialog.ShowDialog();
    }

    private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingTableSelection && TablesList.SelectedIndex >= 0)
        {
            SelectTable(TablesList.SelectedIndex);
            SelectArea(KeyboardArea.Tables);
        }
    }

    private void TablesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TablesList.SelectedIndex < 0)
        {
            return;
        }

        SelectTable(TablesList.SelectedIndex);
        SelectArea(KeyboardArea.Tables);

        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        if (IsIFoodOrder(board))
        {
            ShowIFoodOrderActionDialog(board, isNewOrder: false);
            e.Handled = true;
            return;
        }

        if (board.Status == "LIVRE" && (board.ClosedLines.Count == 0 || HasReceivedPayment(board)))
        {
            OpenOrSwitchTable();
        }
        else
        {
            ReopenCurrentCommand();
        }

        if (board.Status == "LIVRE")
        {
            TablesList.Focus();
            SelectArea(KeyboardArea.Tables);
        }
        else
        {
            CodeBox.Focus();
            CodeBox.SelectAll();
            SelectArea(KeyboardArea.Products);
        }
        e.Handled = true;
    }

    private void CreateTables_Click(object sender, RoutedEventArgs e)
    {
        ShowCreateTablesDialog();
    }

    private void ShowCreateTablesDialog()
    {
        if (CurrentMode != "Comandas")
        {
            ModeList.SelectedItem = "Comandas";
            RefreshBoardForMode();
        }

        var dialog = CreateDialog("Criar mesas", 520, 430);
        dialog.MinWidth = 480;
        dialog.MinHeight = 410;

        var outer = new Grid();
        outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var panel = new StackPanel { Margin = new Thickness(22, 20, 22, 12) };
        panel.Children.Add(new TextBlock
        {
            Text = "Criar mesas do salao",
            Foreground = Solid("#071A2C"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Informe o primeiro numero e quantas mesas o estabelecimento usa.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 12)
        });

        var startBox = new TextBox { Text = GetNextTableNumber().ToString(Brazil), Margin = new Thickness(0, 4, 10, 0) };
        var countBox = new TextBox { Text = "20", Margin = new Thickness(0, 4, 0, 0) };
        var fields = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        fields.ColumnDefinitions.Add(new ColumnDefinition());
        fields.ColumnDefinitions.Add(new ColumnDefinition());
        var startField = DialogField("Numero inicial", startBox);
        var countField = DialogField("Quantidade de mesas", countBox);
        Grid.SetColumn(countField, 1);
        fields.Children.Add(startField);
        fields.Children.Add(countField);
        panel.Children.Add(fields);

        var previewTitle = new TextBlock
        {
            Text = "Previa",
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        var previewText = new TextBlock
        {
            Foreground = Solid("#405366"),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(new Border
        {
            Background = Solid("#E6FBF8"),
            BorderBrush = Solid("#08A99B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 2, 0, 12),
            Child = new StackPanel { Children = { previewTitle, previewText } }
        });

        var status = new TextBlock
        {
            Foreground = RedText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        panel.Children.Add(status);

        void RefreshPreview()
        {
            var start = ParseInt(startBox.Text, 0);
            var count = ParseInt(countBox.Text, 0);
            if (start <= 0 || count <= 0)
            {
                previewText.Text = "Digite numeros validos para ver quais mesas serao criadas.";
                return;
            }

            var end = start + count - 1;
            previewText.Text = $"Serao criadas as mesas {start:000000} ate {end:000000}. Mesas que ja existem serao mantidas.";
        }

        startBox.TextChanged += (_, _) => RefreshPreview();
        countBox.TextChanged += (_, _) => RefreshPreview();

        var create = DialogButton("Criar mesas", "#08A99B");
        create.Margin = new Thickness(0);
        create.Width = 180;
        create.IsDefault = true;
        create.Click += (_, _) =>
        {
            var start = ParseInt(startBox.Text, 0);
            var count = ParseInt(countBox.Text, 0);
            if (start <= 0 || count <= 0)
            {
                status.Text = "Informe inicio e quantidade maiores que zero.";
                return;
            }

            if (count > 300)
            {
                status.Text = "Crie no maximo 300 mesas por vez.";
                return;
            }

            var firstCreated = CreateTablesRange(start, count);
            if (firstCreated < 0)
            {
                status.Text = "Essas mesas ja existem.";
                return;
            }

            SaveStore();
            RefreshBoardForMode();
            SelectTable(firstCreated, saveCurrent: false);
            TableBox.Focus();
            TableBox.SelectAll();
            SetStatus($"{count} mesa(s) configuradas. Se alguma ja existia, foi mantida.");
            dialog.Close();
        };

        var footer = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(22, 12, 22, 12)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footerGrid.Children.Add(new TextBlock
        {
            Text = "Enter confirma. Esc fecha.",
            Foreground = Solid("#5B6B7A"),
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(create, 1);
        footerGrid.Children.Add(create);
        footer.Child = footerGrid;

        outer.Children.Add(panel);
        Grid.SetRow(footer, 1);
        outer.Children.Add(footer);
        dialog.Content = outer;
        dialog.Loaded += (_, _) =>
        {
            RefreshPreview();
            countBox.Focus();
            countBox.SelectAll();
        };
        dialog.ShowDialog();
    }

    private void CategoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoriesList.SelectedIndex >= 0)
        {
            SelectCategory(CategoriesList.SelectedIndex);
            FilterProducts();
            SelectArea(KeyboardArea.Categories);
        }
    }

    private void ProductsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsList.SelectedIndex >= 0)
        {
            SelectProduct(ProductsList.SelectedIndex);
            SelectArea(KeyboardArea.Products);
        }
    }

    private void ProductsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        IncludeSelectedProduct(requireCode: false);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterProducts();
    }

    private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterProducts();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentIFoodDeliveryLocked())
        {
            OpenIFoodActionsForCurrentOrder();
            SelectArea(KeyboardArea.Ticket);
            return;
        }

        ShowProductSearchDialog();
        SelectArea(KeyboardArea.Products);
    }

    private void DeleteTicketLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (BlockIFoodDeliveryEdit("excluir item"))
        {
            e.Handled = true;
            SelectArea(KeyboardArea.Ticket);
            return;
        }

        if (sender is FrameworkElement { DataContext: TicketLine line })
        {
            RemoveTicketLine(line);
        }

        e.Handled = true;
        SelectArea(KeyboardArea.Ticket);
    }

    private void ConfirmInclude_Click(object sender, RoutedEventArgs e)
    {
        if (BlockIFoodDeliveryEdit("aplicar taxas"))
        {
            return;
        }

        ToggleTableCharges();
    }

    private void EnterAction()
    {
        if (_area == KeyboardArea.Tables)
        {
            OpenOrSwitchTable();
            return;
        }

        if (_area == KeyboardArea.Categories)
        {
            SelectArea(KeyboardArea.Products);
            FocusActiveArea();
            return;
        }

        if (_area == KeyboardArea.Ticket)
        {
            CodeBox.Focus();
            CodeBox.SelectAll();
            SelectArea(KeyboardArea.Products);
            return;
        }

        IncludeSelectedProduct(requireCode: true);
    }

    private void MoveSelection(int dx, int dy)
    {
        switch (_area)
        {
            case KeyboardArea.Tables:
                MoveTable(dx, dy);
                break;
            case KeyboardArea.Categories:
                MoveCategory(dx, dy);
                break;
            case KeyboardArea.Products:
                MoveProduct(dx, dy);
                break;
            case KeyboardArea.Ticket:
                MoveTicket(dy == 0 ? dx : dy);
                break;
        }
    }

    private void MoveTable(int dx, int dy)
    {
        var cols = EstimateColumns(TablesList, 81, 4);
        var next = _selectedTableIndex + dx + dy * cols;
        SelectTable(Wrap(next, BoardTiles.Count));
    }

    private void MoveCategory(int dx, int dy)
    {
        var cols = EstimateColumns(CategoriesList, 134, 4);
        var next = _selectedCategoryIndex + dx + dy * cols;
        SelectCategory(Wrap(next, Categories.Count));
        FilterProducts();
    }

    private void MoveProduct(int dx, int dy)
    {
        var delta = dy != 0 ? dy : dx;
        var next = _selectedProductIndex + delta;
        SelectProduct(Wrap(next, VisibleProducts.Count));
    }

    private void MoveTicket(int delta)
    {
        if (TicketLines.Count == 0)
        {
            return;
        }

        _selectedTicketIndex = Wrap(_selectedTicketIndex + delta, TicketLines.Count);
        TicketList.SelectedIndex = _selectedTicketIndex;
        TicketList.ScrollIntoView(TicketLines[_selectedTicketIndex]);
    }

    private void SelectArea(KeyboardArea area)
    {
        _area = area;
        ActiveAreaText.Text = area switch
        {
            KeyboardArea.Tables => CurrentMode == "Balcao" ? "Area: Fichas" : "Area: Mesas",
            KeyboardArea.Categories => "Area: Categorias",
            KeyboardArea.Ticket => "Area: Comanda",
            _ => "Area: Venda rapida"
        };
    }

    private void CycleArea()
    {
        SelectArea(_area switch
        {
            KeyboardArea.Ticket => KeyboardArea.Tables,
            KeyboardArea.Tables => KeyboardArea.Products,
            _ => KeyboardArea.Ticket
        });
        FocusActiveArea();
    }

    private void FocusActiveArea()
    {
        switch (_area)
        {
            case KeyboardArea.Tables:
                TablesList.Focus();
                break;
            case KeyboardArea.Categories:
                CategoriesList.Focus();
                break;
            case KeyboardArea.Products:
                ProductsList.Focus();
                break;
            case KeyboardArea.Ticket:
                TableBox.Focus();
                TableBox.SelectAll();
                break;
        }
    }

    private static int EstimateColumns(FrameworkElement element, double itemWidthWithMargin, int fallback)
    {
        var width = element.ActualWidth;
        if (double.IsNaN(width) || width <= 0 || itemWidthWithMargin <= 0)
        {
            return fallback;
        }

        return Math.Max(1, (int)Math.Floor(width / itemWidthWithMargin));
    }

    private static bool IsAreaNavigationKey(Key key)
    {
        return key is Key.Left or Key.Right or Key.Up or Key.Down;
    }

    private void RefreshBoardForMode()
    {
        BoardTiles.Clear();

        IEnumerable<TableTile> source = CurrentMode switch
        {
            "Balcao" => Tables.Where(table => table.Kind == "BALCAO"),
            "Delivery" => DeliveryTiles
                .OrderBy(DeliveryBoardSortGroup)
                .ThenBy(DeliveryBoardTimeSort)
                .ThenBy(tile => tile.CreatedAt)
                .ThenBy(DeliveryBoardNumberSort),
            _ => Tables.Where(table => table.Kind == "MESA")
        };

        foreach (var tile in source)
        {
            UpdateIFoodDynamicDetail(tile);
            BoardTiles.Add(tile);
        }

        BoardTitleText.Text = CurrentMode switch
        {
            "Balcao" => "Fichas de balcao",
            "Delivery" => "Pedidos Delivery",
            _ => "Comandas / Mesas"
        };
        CreateTablesButton.Visibility = CurrentMode == "Comandas" ? Visibility.Visible : Visibility.Collapsed;

        UpdateCommandPanelText();
        if (BoardTiles.Count == 0)
        {
            ClearBoardSelectionForEmptyMode();
            RefreshIFoodDeliveryLockUi(null);
        }
    }

    private void RefreshDeliveryCountdownTiles()
    {
        if (!string.Equals(CurrentMode, "Delivery", StringComparison.OrdinalIgnoreCase) || BoardTiles.Count == 0)
        {
            return;
        }

        var hasCountdown = false;
        foreach (var tile in BoardTiles)
        {
            if (IsIFoodDeliveryBoard(tile)
                && ((IsIFoodWaitingForConfirmation(tile) && GetIFoodConfirmationDeadline(tile).HasValue)
                    || tile.ExternalDeliveryExpectedAt.HasValue
                    || tile.ExternalDeliveredAt.HasValue
                    || NormalizeIFoodBoardStatus(tile.Status) == "ENTREGUE"))
            {
                hasCountdown = true;
                break;
            }
        }

        if (hasCountdown)
        {
            TablesList.Items.Refresh();
        }
    }

    private void ClearBoardSelectionForEmptyMode()
    {
        _selectedTableIndex = 0;
        TicketLines.Clear();
        Payments.Clear();
        _selectedTicketIndex = -1;
        TicketList.SelectedIndex = -1;
        TableBox.Text = "";
        WaiterBox.Text = GetDefaultStaffNumberForKind(CurrentMode == "Balcao" ? "BALCAO" : "MESA");
        PriceBox.Text = "0,00";
        NoteBox.Text = "10";
        OpenInfoText.Text = CurrentMode switch
        {
            "Comandas" => "Nenhuma mesa cadastrada  |  Clique em Criar mesas",
            "Balcao" => "Nenhuma ficha aberta  |  Enter cria uma ficha",
            "Delivery" => "Nenhum pedido delivery",
            _ => "Nenhuma comanda"
        };
        RefreshTotals();
        RefreshIFoodDeliveryLockUi(null);
    }

    private void UpdateCommandPanelText()
    {
        CommandTitleText.Text = CurrentMode switch
        {
            "Balcao" => "Venda de ficha",
            "Delivery" => "Pedido",
            _ => "Comanda"
        };

        TableFieldLabel.Text = CurrentMode switch
        {
            "Balcao" => "Ficha / cliente",
            "Delivery" => "Pedido",
            _ => "Mesa / cliente"
        };

        WaiterFieldLabel.Text = "Oper/Garcom";
    }

    private void LocateTableFromInput()
    {
        var raw = TableBox.Text.Trim().ToUpperInvariant();
        if (CurrentMode == "Balcao")
        {
            LocateCounterFichaFromInput(raw);
            return;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            SetStatus("Digite o numero da mesa.");
            TableBox.Focus();
            return;
        }

        var normalized = int.TryParse(raw, NumberStyles.Integer, Brazil, out var number)
            ? number.ToString("000000", Brazil)
            : raw;

        var index = FindBoardTileIndex(normalized, raw);
        if (index < 0 && CurrentMode != "Comandas" && int.TryParse(raw, out _))
        {
            SaveActiveTicketToCurrentBoard();
            ModeList.SelectedItem = "Comandas";
            RefreshBoardForMode();
            index = FindBoardTileIndex(normalized, raw);
        }

        if (index < 0)
        {
            if (!int.TryParse(raw, NumberStyles.Integer, Brazil, out _))
            {
                SetCustomerForCurrentBoard(raw);
                return;
            }

            index = CreateTableFromInput(normalized);
        }

        SelectTable(index);
        OpenOrSwitchTable();
        WaiterBox.Focus();
        WaiterBox.SelectAll();
        SelectArea(KeyboardArea.Ticket);
        SetStatus($"Mesa {BoardTiles[index].Number} pronta. Informe operador/garcom e pressione Enter.");
    }

    private void LocateCounterFichaFromInput(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            var newIndex = CreateNextCounterFicha();
            SelectTable(newIndex);
            CodeBox.Focus();
            CodeBox.SelectAll();
            SelectArea(KeyboardArea.Products);
            SetStatus($"Ficha {BoardTiles[newIndex].Number} criada. Informe o codigo do produto.");
            return;
        }

        if (!LooksLikeFichaNumber(raw))
        {
            SetCustomerForCurrentBoard(raw);
            CodeBox.Focus();
            CodeBox.SelectAll();
            SelectArea(KeyboardArea.Products);
            return;
        }

        var normalized = NormalizeFichaNumber(raw);
        var index = FindBoardTileIndex(normalized, raw);
        if (index < 0)
        {
            index = CreateCounterFicha(normalized);
        }

        SelectTable(index);
        OpenOrSwitchTable();
        CodeBox.Focus();
        CodeBox.SelectAll();
        SelectArea(KeyboardArea.Products);
        SetStatus($"Ficha {BoardTiles[index].Number} pronta para venda de balcao.");
    }

    private void SetCustomerForCurrentBoard(string name)
    {
        var board = CurrentBoard;
        if (board is null || string.IsNullOrWhiteSpace(name))
        {
            SetStatus("Selecione uma mesa ou ficha antes de informar o cliente.");
            return;
        }

        board.CustomerName = name.Trim().ToUpperInvariant();
        if (board.Kind == "MESA" && board.Status == "LIVRE")
        {
            board.Status = "OCUPADA";
        }
        else if (board.Kind == "BALCAO" && board.Status is "LIVRE" or "FINALIZADO")
        {
            board.Status = "ABERTO";
        }

        TableBox.Text = board.CustomerName;
        TablesList.Items.Refresh();
        RefreshTotals();
        SaveStore();
        SetStatus($"{BoardKindLabel(board)} {board.Number}: cliente {board.CustomerName}");
    }

    private int CreateTableFromInput(string number)
    {
        var table = new TableTile
        {
            Number = number,
            Kind = "MESA",
            Status = "LIVRE",
            Waiter = 0,
            CreatedAt = DateTime.Now
        };

        Tables.Add(table);
        RefreshBoardForMode();
        SaveStore();
        SetStatus($"Mesa criada: {number}");
        return BoardTiles.IndexOf(table);
    }

    private int CreateTablesRange(int start, int count)
    {
        TableTile? firstCreated = null;
        for (var offset = 0; offset < count; offset++)
        {
            var number = (start + offset).ToString("000000", Brazil);
            var exists = Tables.Any(table => table.Kind == "MESA"
                && string.Equals(table.Number, number, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                continue;
            }

            var table = new TableTile
            {
                Number = number,
                Kind = "MESA",
                Status = "LIVRE",
                CreatedAt = DateTime.Now
            };
            Tables.Add(table);
            firstCreated ??= table;
        }

        RefreshBoardForMode();
        return firstCreated is null ? -1 : BoardTiles.IndexOf(firstCreated);
    }

    private int GetNextTableNumber()
    {
        return Tables
            .Where(table => table.Kind == "MESA")
            .Select(table => int.TryParse(table.Number, NumberStyles.Integer, Brazil, out var parsed) ? parsed : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private int CreateNextCounterFicha()
    {
        var next = Tables
            .Where(table => table.Kind == "BALCAO")
            .Select(table => TryParseFichaNumber(table.Number))
            .DefaultIfEmpty(0)
            .Max() + 1;

        return CreateCounterFicha($"F{next:00000}");
    }

    private int CreateCounterFicha(string number)
    {
        var ficha = new TableTile
        {
            Number = NormalizeFichaNumber(number),
            Kind = "BALCAO",
            Status = "ABERTO",
            Waiter = Math.Max(0, ParseInt(GetDefaultStaffNumberForKind("BALCAO"), 0)),
            CreatedAt = DateTime.Now
        };

        Tables.Add(ficha);
        RefreshBoardForMode();
        SaveStore();
        SetStatus($"Ficha criada: {ficha.Number}");
        return BoardTiles.IndexOf(ficha);
    }

    private static bool LooksLikeFichaNumber(string raw)
    {
        raw = raw.Trim().ToUpperInvariant();
        if (int.TryParse(raw, NumberStyles.Integer, Brazil, out _))
        {
            return true;
        }

        return raw.Length > 1
               && raw[0] is 'F' or 'B'
               && int.TryParse(raw[1..], NumberStyles.Integer, Brazil, out _);
    }

    private static string NormalizeFichaNumber(string raw)
    {
        raw = raw.Trim().ToUpperInvariant();
        if (int.TryParse(raw, NumberStyles.Integer, Brazil, out var number))
        {
            return $"F{number:00000}";
        }

        if (raw.Length > 1 && raw[0] is 'F' or 'B' && int.TryParse(raw[1..], NumberStyles.Integer, Brazil, out number))
        {
            return $"{raw[0]}{number:00000}";
        }

        return raw;
    }

    private static int TryParseFichaNumber(string raw)
    {
        raw = raw.Trim().ToUpperInvariant();
        if (int.TryParse(raw, NumberStyles.Integer, Brazil, out var number))
        {
            return number;
        }

        return raw.Length > 1 && raw[0] is 'F' or 'B' && int.TryParse(raw[1..], NumberStyles.Integer, Brazil, out number)
            ? number
            : 0;
    }

    private int FindBoardTileIndex(string normalized, string raw)
    {
        for (var i = 0; i < BoardTiles.Count; i++)
        {
            var number = BoardTiles[i].Number.Trim().ToUpperInvariant();
            if (number == normalized || number == raw || AreSameFicha(number, normalized) || AreSameFicha(number, raw))
            {
                return i;
            }

            if (int.TryParse(number, NumberStyles.Integer, Brazil, out var boardNumber)
                && int.TryParse(raw, NumberStyles.Integer, Brazil, out var typedNumber)
                && boardNumber == typedNumber)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool AreSameFicha(string left, string right)
    {
        return TryParseFichaNumber(left) > 0
               && TryParseFichaNumber(left) == TryParseFichaNumber(right)
               && (left.StartsWith('F') || left.StartsWith('B') || right.StartsWith('F') || right.StartsWith('B'));
    }

    private void LoadActiveTicketFromBoard(TableTile board)
    {
        var canDeleteLines = !IsIFoodDeliveryBoard(board);
        TicketLines.Clear();
        foreach (var line in board.Lines)
        {
            line.CanDelete = canDeleteLines;
            TicketLines.Add(line);
        }

        Payments.Clear();
        foreach (var payment in board.Payments)
        {
            Payments.Add(payment);
        }

        _selectedTicketIndex = TicketLines.Count > 0 ? 0 : -1;
        TicketList.SelectedIndex = _selectedTicketIndex;
    }

    private string GetDefaultStaffNumberForKind(string kind)
    {
        var isCounter = string.Equals(kind, "BALCAO", StringComparison.OrdinalIgnoreCase);
        Func<UserAccount, bool> allowed = IsServiceOrCashUser;
        var preferredRole = isCounter ? "CAIXA" : "GARCOM";

        if (CurrentUser is { } currentUser
            && allowed(currentUser)
            && string.Equals(currentUser.Role, preferredRole, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(StaffNumber(currentUser), NumberStyles.Integer, Brazil, out var currentNumber)
            && currentNumber > 0)
        {
            return currentNumber.ToString(Brazil);
        }

        var preferred = Users.FirstOrDefault(user =>
            allowed(user)
            && string.Equals(user.Role, preferredRole, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(StaffNumber(user), NumberStyles.Integer, Brazil, out var number)
            && number > 0);
        if (preferred is not null)
        {
            return StaffNumber(preferred);
        }

        var any = Users.FirstOrDefault(user =>
            allowed(user)
            && int.TryParse(StaffNumber(user), NumberStyles.Integer, Brazil, out var number)
            && number > 0);
        return any is null ? "" : StaffNumber(any);
    }

    private UserAccount? FindAllowedStaffByNumber(int number, Func<UserAccount, bool> allowed)
    {
        if (number <= 0)
        {
            return null;
        }

        var key = number.ToString(Brazil);
        return Users.FirstOrDefault(user => allowed(user) && StaffNumber(user) == key);
    }

    private string BuildStaffOptions(Func<UserAccount, bool> allowed)
    {
        var options = Users
            .Where(user => allowed(user))
            .Select(user => new { User = user, Number = StaffNumber(user) })
            .Where(item => int.TryParse(item.Number, NumberStyles.Integer, Brazil, out var number) && number > 0)
            .OrderBy(item => item.User.Role)
            .ThenBy(item => int.Parse(item.Number, Brazil))
            .Take(5)
            .Select(item => $"{item.Number} - {item.User.Name}")
            .ToList();

        return options.Count == 0
            ? "Cadastre a equipe primeiro no botao Equipe."
            : $"Use: {string.Join(", ", options)}.";
    }

    private bool TryApplyStaffFromInput(TableTile? board)
    {
        var kind = board?.Kind ?? (CurrentMode == "Balcao" ? "BALCAO" : "MESA");
        var isCounter = string.Equals(kind, "BALCAO", StringComparison.OrdinalIgnoreCase);
        var label = isCounter ? "operador/garcom" : "garcom/operador";
        Func<UserAccount, bool> allowed = IsServiceOrCashUser;
        var normalized = NormalizeStaffNumber(WaiterBox.Text);

        if (!int.TryParse(normalized, NumberStyles.Integer, Brazil, out var staffNumber) || staffNumber <= 0)
        {
            SetStatus($"Informe um numero de {label} cadastrado. {BuildStaffOptions(allowed)}");
            WaiterBox.Focus();
            WaiterBox.SelectAll();
            SelectArea(KeyboardArea.Ticket);
            return false;
        }

        var staff = FindAllowedStaffByNumber(staffNumber, allowed);
        if (staff is null)
        {
            SetStatus($"Numero de {label} nao cadastrado. {BuildStaffOptions(allowed)}");
            WaiterBox.Focus();
            WaiterBox.SelectAll();
            SelectArea(KeyboardArea.Ticket);
            return false;
        }

        WaiterBox.Text = staffNumber.ToString(Brazil);
        if (board is not null)
        {
            board.Waiter = staffNumber;
        }

        return true;
    }

    private void LoadChargeInputsFromBoard(TableTile board)
    {
        var servicePercent = board.ServicePercent;
        if (!board.ChargesEnabled && servicePercent <= 0)
        {
            servicePercent = 10m;
        }

        PriceBox.Text = board.CouvertAmount.ToString("N2", Brazil);
        NoteBox.Text = servicePercent.ToString("0.##", Brazil);
    }

    private void SaveActiveTicketToCurrentBoard()
    {
        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        board.Lines = TicketLines.ToList();
        board.Payments = Payments.ToList();
        board.Total = TicketLines.Sum(line => line.Total);
    }

    private void SelectTable(int index, bool saveCurrent = true)
    {
        if (BoardTiles.Count == 0) return;

        if (saveCurrent)
        {
            SaveActiveTicketToCurrentBoard();
        }

        _selectedTableIndex = Wrap(index, BoardTiles.Count);
        foreach (var table in Tables) table.IsSelected = false;
        foreach (var delivery in DeliveryTiles) delivery.IsSelected = false;
        foreach (var kitchen in KitchenTiles) kitchen.IsSelected = false;

        var selected = BoardTiles[_selectedTableIndex];
        selected.IsSelected = true;
        _updatingTableSelection = true;
        try
        {
            TablesList.SelectedIndex = _selectedTableIndex;
            TablesList.ScrollIntoView(selected);
        }
        finally
        {
            _updatingTableSelection = false;
        }
        TableBox.Text = selected.Kind == "MESA" && !string.IsNullOrWhiteSpace(selected.CustomerName)
            ? selected.CustomerName
            : selected.Number;
        WaiterBox.Text = selected.Waiter > 0
            ? selected.Waiter.ToString(Brazil)
            : GetDefaultStaffNumberForKind(selected.Kind);
        LoadChargeInputsFromBoard(selected);
        OpenInfoText.Text = BuildBoardInfo(selected, Payments.Sum(payment => payment.Amount));
        LoadActiveTicketFromBoard(selected);
        RefreshTotals();
        RefreshIFoodDeliveryLockUi(selected);
    }

    private void SelectCategory(int index)
    {
        if (Categories.Count == 0) return;
        _selectedCategoryIndex = Wrap(index, Categories.Count);
        foreach (var category in Categories) category.IsSelected = false;
        Categories[_selectedCategoryIndex].IsSelected = true;
        CategoriesList.SelectedIndex = _selectedCategoryIndex;
    }

    private void SelectProduct(int index)
    {
        if (IsCurrentIFoodDeliveryLocked())
        {
            SelectedProductText.Text = "Pedido iFood bloqueado para edicao. Use F9 ou Acoes iFood para confirmar/despachar.";
            return;
        }

        if (VisibleProducts.Count == 0)
        {
            SelectedProductText.Text = "Nenhum produto encontrado";
            return;
        }

        _selectedProductIndex = Wrap(index, VisibleProducts.Count);
        foreach (var product in VisibleProducts) product.IsSelected = false;
        var selected = VisibleProducts[_selectedProductIndex];
        selected.IsSelected = true;
        ProductsList.SelectedIndex = _selectedProductIndex;
        ProductsList.ScrollIntoView(selected);
        SelectedProductText.Text = $"{selected.Code} - {selected.Name} - {selected.PriceText}";
    }

    private void FilterProducts()
    {
        var query = string.IsNullOrWhiteSpace(CodeBox.Text) ? SearchBox.Text.Trim() : CodeBox.Text.Trim();

        var filtered = Products.Where(product => product.Active);
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(product =>
                product.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                product.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                product.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        VisibleProducts.Clear();
        foreach (var product in filtered
                     .OrderByDescending(product => product.SoldQuantity)
                     .ThenBy(product => product.Name))
        {
            VisibleProducts.Add(product);
        }

        SelectProduct(0);
    }

    private void SelectProductByCode(string code)
    {
        var index = VisibleProducts
            .Select((product, productIndex) => new { product, productIndex })
            .FirstOrDefault(item => string.Equals(item.product.Code, code, StringComparison.OrdinalIgnoreCase))
            ?.productIndex ?? -1;

        if (index >= 0)
        {
            SelectProduct(index);
        }
    }

    private void IncludeSelectedProduct(bool requireCode = true)
    {
        var board = CurrentBoard;
        if (board is null)
        {
            SetStatus("Selecione uma mesa, balcao ou pedido.");
            return;
        }

        if (IsIFoodDeliveryBoard(board))
        {
            BlockIFoodDeliveryEdit("incluir produto");
            return;
        }

        if (board.Kind == "BALCAO" && board.Status == "FINALIZADO")
        {
            var nextIndex = CreateNextCounterFicha();
            SelectTable(nextIndex);
            board = CurrentBoard;
            if (board is null)
            {
                SetStatus("Nao foi possivel criar a ficha.");
                return;
            }
        }

        var typedCode = CodeBox.Text.Trim();
        if (typedCode == "0")
        {
            ShowProductSearchDialog();
            return;
        }

        if (requireCode && string.IsNullOrWhiteSpace(typedCode))
        {
            SetStatus("Digite o codigo do produto antes de incluir.");
            CodeBox.Focus();
            CodeBox.SelectAll();
            return;
        }

        if (!TryApplyStaffFromInput(board))
        {
            return;
        }

        ProductTile? product = null;
        if (!string.IsNullOrWhiteSpace(typedCode))
        {
            product = Products.FirstOrDefault(item => item.Code == typedCode.PadLeft(6, '0'))
                      ?? Products.FirstOrDefault(item => item.Code.Contains(typedCode, StringComparison.OrdinalIgnoreCase));
        }

        product ??= VisibleProducts.Count == 0 ? null : VisibleProducts[Math.Clamp(_selectedProductIndex, 0, VisibleProducts.Count - 1)];
        if (product is null)
        {
            SetStatus("Produto nao encontrado.");
            return;
        }

        var qty = Math.Max(1, ParseInt(QuantityBox.Text, 1));
        var note = "";
        var chargesWereActive = HasAppliedTableCharges();
        if (product.IsPizza || product.Category == "PIZZAS")
        {
            var pizzaNote = ShowPizzaDialog(product, note);
            if (pizzaNote is null)
            {
                SetStatus("Pizza cancelada.");
                return;
            }

            note = pizzaNote;
        }

        if (!RequireManagerForClosedAccountEdit(board, "incluir produto"))
        {
            return;
        }

        var line = new TicketLine
        {
            Code = product.Code,
            Name = product.Name,
            Quantity = qty,
            UnitPrice = product.Price,
            Note = note,
            Sector = NormalizeProductDestination(product.Sector, "CAIXA")
        };

        var existingLine = TicketLines.FirstOrDefault(item => CanMergeTicketLine(item, line));
        if (existingLine is not null)
        {
            existingLine.Quantity += qty;
            _selectedTicketIndex = TicketLines.IndexOf(existingLine);
            TicketList.Items.Refresh();
            line = existingLine;
        }
        else
        {
            TicketLines.Add(line);
            _selectedTicketIndex = TicketLines.Count - 1;
        }

        TicketList.SelectedIndex = _selectedTicketIndex;
        if (_selectedTicketIndex >= 0)
        {
            TicketList.ScrollIntoView(TicketLines[_selectedTicketIndex]);
        }

        board.Status = board.Kind switch
        {
            "BALCAO" => "ABERTO",
            "DELIVERY" => board.Status is "NOVO" or "ENTREGUE" ? "PREPARO" : board.Status,
            "KDS" => board.Status is "ENTREGUE" ? "RECEBIDO" : board.Status,
            _ => "OCUPADA"
        };
        board.Waiter = ParseInt(WaiterBox.Text, board.Waiter);
        var stockChanged = false;
        if (product.StockQuantity > 0)
        {
            product.StockQuantity = Math.Max(0, product.StockQuantity - qty);
            stockChanged = true;
        }

        product.SoldQuantity += qty;

        CodeBox.Text = "";
        QuantityBox.Text = "1";
        FocusAfterProductInclude(board);
        FilterProducts();
        if (chargesWereActive)
        {
            ApplyTableCharges(showStatus: false);
        }
        else
        {
            SaveActiveTicketToCurrentBoard();
            RefreshTotals();
            SaveStore();
        }

        SetStatus(existingLine is null
            ? $"Incluido: {qty}x {line.Name}"
            : $"Agrupado: {line.Quantity}x {line.Name}");
        ScheduleKitchenPrint(board, [line]);
        if (stockChanged)
        {
            QueueIFoodStockSync(product, $"Venda PDV {board.Kind} {board.Number}");
        }
    }

    private void FocusAfterProductInclude(TableTile board)
    {
        if (board.Kind == "MESA")
        {
            TableBox.Focus();
            TableBox.SelectAll();
            SelectArea(KeyboardArea.Ticket);
            return;
        }

        CodeBox.Focus();
        CodeBox.SelectAll();
        SelectArea(KeyboardArea.Products);
    }

    private void ToggleTableCharges()
    {
        if (BlockIFoodDeliveryEdit("aplicar taxas"))
        {
            return;
        }

        if (HasAppliedTableCharges())
        {
            RemoveTableCharges();
            return;
        }

        ApplyTableCharges();
    }

    private void ApplyTableCharges(bool showStatus = true)
    {
        var board = CurrentBoard;
        if (board is null)
        {
            SetStatus("Selecione uma mesa ou ficha antes de aplicar couvert/garcom.");
            return;
        }

        if (IsIFoodDeliveryBoard(board))
        {
            BlockIFoodDeliveryEdit("aplicar taxas");
            return;
        }

        if (!RequireManagerForClosedAccountEdit(board, "alterar taxas"))
        {
            return;
        }

        if (!TryApplyStaffFromInput(board))
        {
            return;
        }

        var couvert = ParseMoney(PriceBox.Text, 0);
        var servicePercent = ParseMoney(NoteBox.Text, 0);
        board.ChargesEnabled = true;
        board.CouvertAmount = Math.Max(0, couvert);
        board.ServicePercent = Math.Max(0, servicePercent);
        for (var i = TicketLines.Count - 1; i >= 0; i--)
        {
            if (IsTableCharge(TicketLines[i]))
            {
                TicketLines.RemoveAt(i);
            }
        }

        var people = Math.Max(1, board.People);
        var productTotal = TicketLines.Sum(line => line.Total);
        if (board.CouvertAmount > 0)
        {
            TicketLines.Add(new TicketLine
            {
                Code = CouvertCode,
                Name = "COUVERT",
                Quantity = people,
                UnitPrice = board.CouvertAmount,
                Note = $"{people} pessoa(s)",
                Sector = "CAIXA"
            });
        }

        var serviceBase = productTotal + board.CouvertAmount * people;
        var serviceValue = Math.Round(serviceBase * board.ServicePercent / 100m, 2);
        if (serviceValue > 0)
        {
            TicketLines.Add(new TicketLine
            {
                Code = ServiceCode,
                Name = $"SERVICO GARCOM {board.ServicePercent:N2}%",
                Quantity = 1,
                UnitPrice = serviceValue,
                Sector = "CAIXA"
            });
        }

        if (TicketLines.Count > 0)
        {
            board.Status = board.Kind switch
            {
                "BALCAO" => "ABERTO",
                "DELIVERY" => board.Status is "NOVO" or "ENTREGUE" ? "PREPARO" : board.Status,
                "KDS" => board.Status is "ENTREGUE" ? "RECEBIDO" : board.Status,
                _ => "OCUPADA"
            };
        }

        SaveActiveTicketToCurrentBoard();
        RefreshTotals();
        SaveStore();
        if (showStatus)
        {
            SetStatus($"Taxas ativadas: couvert {Money(board.CouvertAmount)} x {people}, garcom {board.ServicePercent:N2}%.");
        }
    }

    private void RemoveTableCharges(bool showStatus = true)
    {
        if (BlockIFoodDeliveryEdit("remover taxas"))
        {
            return;
        }

        if (CurrentBoard is { } board)
        {
            if (!RequireManagerForClosedAccountEdit(board, "alterar taxas"))
            {
                return;
            }

            board.ChargesEnabled = false;
        }

        var removed = false;
        for (var i = TicketLines.Count - 1; i >= 0; i--)
        {
            if (IsTableCharge(TicketLines[i]))
            {
                TicketLines.RemoveAt(i);
                removed = true;
            }
        }

        SaveActiveTicketToCurrentBoard();
        RefreshTotals();
        SaveStore();
        if (showStatus)
        {
            SetStatus(removed ? "Couvert/% garcom desativados." : "Couvert/% garcom ja estavam desativados.");
        }
    }

    private bool HasAppliedTableCharges()
    {
        return CurrentBoard is { ChargesEnabled: true } || TicketLines.Any(IsTableCharge);
    }

    private static bool IsTableCharge(TicketLine line)
    {
        return line.Code == CouvertCode || line.Code == ServiceCode;
    }

    private static bool CanMergeTicketLine(TicketLine existing, TicketLine incoming)
    {
        return !IsTableCharge(existing)
               && !IsTableCharge(incoming)
               && string.Equals(existing.Code, incoming.Code, StringComparison.OrdinalIgnoreCase)
               && string.Equals(existing.Name, incoming.Name, StringComparison.OrdinalIgnoreCase)
               && string.Equals(existing.Note ?? "", incoming.Note ?? "", StringComparison.OrdinalIgnoreCase)
               && string.Equals(existing.Sector ?? "", incoming.Sector ?? "", StringComparison.OrdinalIgnoreCase)
               && existing.UnitPrice == incoming.UnitPrice;
    }

    private void OpenOrSwitchTable()
    {
        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        if (board.Kind == "DELIVERY")
        {
            if (IsIFoodOrder(board))
            {
                ShowIFoodOrderActionDialog(board, isNewOrder: false);
                return;
            }

            board.Status = NextDeliveryStatus(board.Status);
        }
        else if (board.Kind == "KDS")
        {
            if (TicketLines.Count > 0)
            {
                var line = TicketLines[Math.Clamp(_selectedTicketIndex, 0, TicketLines.Count - 1)];
                line.KitchenStatus = NextKitchenStatus(line.KitchenStatus);
                if (line.KitchenStatus == "PREPARANDO")
                {
                    line.KitchenStartedAt ??= DateTime.Now;
                }
                else if (line.KitchenStatus == "PRONTO")
                {
                    line.KitchenReadyAt = DateTime.Now;
                }

                board.Lines = TicketLines.Select(CloneLine).ToList();
                board.Status = AggregateKitchenStatus(board.Lines);
                TicketList.Items.Refresh();
            }
            else
            {
                board.Status = NextKitchenStatus(board.Status);
            }
        }
        else if (board.Status == "LIVRE")
        {
            if (!TryApplyStaffFromInput(board))
            {
                return;
            }

            board.Status = board.Kind == "BALCAO" ? "ABERTO" : "OCUPADA";
        }

        SetStatus($"{board.Kind} {board.Number}: {board.Status}");
        QueuePublicMenuOrderStatusSync(board);
        RefreshTotals();
        SaveStore();
    }

    private void RemoveSelectedLine()
    {
        if (BlockIFoodDeliveryEdit("excluir item"))
        {
            return;
        }

        if (TicketLines.Count == 0)
        {
            return;
        }

        var line = TicketLines[Math.Clamp(_selectedTicketIndex, 0, TicketLines.Count - 1)];
        RemoveTicketLine(line);
    }

    private void RemoveTicketLine(TicketLine line)
    {
        if (BlockIFoodDeliveryEdit("excluir item"))
        {
            return;
        }

        if (!TicketLines.Contains(line))
        {
            return;
        }

        if (IsTableCharge(line))
        {
            if (CurrentBoard is { } chargeBoard && !RequireManagerForClosedAccountEdit(chargeBoard, "excluir item"))
            {
                return;
            }

            RemoveTableCharges();
            return;
        }

        if (CurrentBoard is { } board && !RequireManagerForClosedAccountEdit(board, "excluir item"))
        {
            return;
        }

        var chargesWereActive = HasAppliedTableCharges();
        TicketLines.Remove(line);
        var restoredProduct = Products.FirstOrDefault(product => string.Equals(product.Code, line.Code, StringComparison.OrdinalIgnoreCase));
        if (restoredProduct is not null)
        {
            restoredProduct.StockQuantity += Math.Max(0, line.Quantity);
        }

        _selectedTicketIndex = Math.Min(_selectedTicketIndex, TicketLines.Count - 1);
        if (chargesWereActive)
        {
            ApplyTableCharges(showStatus: false);
        }
        else
        {
            SaveActiveTicketToCurrentBoard();
            RefreshTotals();
            SaveStore();
        }

        SetStatus($"Removido: {line.Name}");
        if (restoredProduct is not null)
        {
            QueueIFoodStockSync(restoredProduct, "Item removido da venda");
        }
    }

    private void TransferSelectedLine()
    {
        if (BlockIFoodDeliveryEdit("transferir item"))
        {
            return;
        }

        if (TicketLines.Count == 0)
        {
            SetStatus("Nao ha item para transferir.");
            return;
        }

        var source = CurrentBoard;
        if (source is null)
        {
            return;
        }

        if (!RequireManagerForClosedAccountEdit(source, "transferir item"))
        {
            return;
        }

        var mesas = Tables.Where(table => table.Kind == "MESA").ToList();
        if (mesas.Count < 2)
        {
            SetStatus("Nenhuma mesa destino disponivel.");
            return;
        }

        var sourceIndex = Math.Max(0, mesas.IndexOf(source));
        var destination = mesas[(sourceIndex + 1) % mesas.Count];
        if (ReferenceEquals(destination, source))
        {
            destination = mesas[(sourceIndex + 2) % mesas.Count];
        }

        var line = TicketLines[Math.Clamp(_selectedTicketIndex, 0, TicketLines.Count - 1)];
        TicketLines.Remove(line);
        destination.Lines.Add(CloneLine(line));
        destination.Status = "OCUPADA";
        destination.Waiter = source.Waiter == 0 ? 1 : source.Waiter;
        destination.Total = destination.Lines.Sum(item => item.Total);
        SaveActiveTicketToCurrentBoard();
        RefreshTotals();
        SaveStore();
        SetStatus($"{line.Name} transferido para mesa {destination.Number}.");
    }

    private void AddPrepayment()
    {
        if (!IsCashOpen())
        {
            SetStatus("Caixa fechado. Pressione F10 para abrir antes de receber pagamento.");
            return;
        }

        var ticketTotal = TicketLines.Sum(line => line.Total);
        var paidTotal = Payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0, ticketTotal - paidTotal);
        if (balance <= 0)
        {
            SetStatus("Comanda sem saldo para antecipar.");
            return;
        }

        if (CurrentBoard is { } board && !TryApplyStaffFromInput(board))
        {
            return;
        }

        var payment = ShowPaymentDialog(balance);
        if (payment is null)
        {
            SetStatus("Pagamento antecipado cancelado.");
            return;
        }

        Payments.Add(payment);
        _cashTotal += payment.Amount;
        SaveActiveTicketToCurrentBoard();
        RefreshTotals();
        SaveStore();
        SetStatus($"Antecipado: {payment.Method} {Money(payment.Amount)}");
    }

    private PaymentLine? ShowPaymentDialog(decimal balance)
    {
        PaymentLine? result = null;
        var dialog = CreateDialog("Pagamento antecipado", 390, 292);
        dialog.ResizeMode = ResizeMode.NoResize;

        var payerBox = new TextBox { Text = $"Cliente {Payments.Count + 1}", Margin = new Thickness(0, 4, 0, 10) };
        var amountBox = new TextBox { Text = balance.ToString("N2", Brazil), Margin = new Thickness(0, 4, 0, 10) };
        var methods = new ListBox
        {
            ItemsSource = new[] { "PIX", "CARTAO", "DINHEIRO", "VALE" },
            SelectedIndex = 0,
            Height = 78,
            Margin = new Thickness(0, 4, 0, 12)
        };
        var message = new TextBlock
        {
            Text = $"Saldo: {Money(balance)}",
            Foreground = AmberText,
            FontWeight = FontWeights.SemiBold
        };

        var confirm = DialogButton("Enter confirma", "#08A99B");

        void ConfirmPayment()
        {
            var amount = ParseMoney(amountBox.Text, 0);
            if (amount <= 0 || amount > balance)
            {
                message.Text = "Valor invalido para o saldo.";
                message.Foreground = RedText;
                amountBox.Focus();
                amountBox.SelectAll();
                return;
            }

            result = new PaymentLine
            {
                Payer = string.IsNullOrWhiteSpace(payerBox.Text) ? $"Cliente {Payments.Count + 1}" : payerBox.Text.Trim(),
                Method = methods.SelectedItem?.ToString() ?? "PIX",
                Amount = amount,
                When = DateTime.Now
            };
            dialog.DialogResult = true;
            dialog.Close();
        }

        confirm.Click += (_, _) => ConfirmPayment();
        dialog.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ConfirmPayment();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.DialogResult = false;
                dialog.Close();
                e.Handled = true;
            }
        };

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = "Nome", Foreground = Solid("#5B6B7A"), FontWeight = FontWeights.SemiBold });
        panel.Children.Add(payerBox);
        panel.Children.Add(new TextBlock { Text = "Valor", Foreground = Solid("#5B6B7A"), FontWeight = FontWeights.SemiBold });
        panel.Children.Add(amountBox);
        panel.Children.Add(new TextBlock { Text = "Forma", Foreground = Solid("#5B6B7A"), FontWeight = FontWeights.SemiBold });
        panel.Children.Add(methods);
        panel.Children.Add(message);
        panel.Children.Add(confirm);
        dialog.Content = panel;
        amountBox.Focus();
        amountBox.SelectAll();
        dialog.ShowDialog();
        return result;
    }

    private void ChangeQuantity(int delta)
    {
        if (BlockIFoodDeliveryEdit("alterar quantidade"))
        {
            return;
        }

        var qty = Math.Max(1, ParseInt(QuantityBox.Text, 1) + delta);
        QuantityBox.Text = qty.ToString(Brazil);
    }

    private void ShowProductSearchDialog()
    {
        if (IsCurrentIFoodDeliveryLocked())
        {
            OpenIFoodActionsForCurrentOrder();
            return;
        }

        var dialog = CreateDialog("Pesquisa de produtos", 720, 520);
        var queryBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
        var list = new ListBox { DisplayMemberPath = nameof(ProductTile.SearchDisplay), Height = 360 };

        void RefreshSearch()
        {
            var query = queryBox.Text.Trim();
            var filtered = Products
                .Where(product => string.IsNullOrWhiteSpace(query)
                    || product.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || product.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(product => product.Category)
                .ThenBy(product => product.Name)
                .ToList();
            list.ItemsSource = filtered;
            if (filtered.Count > 0) list.SelectedIndex = 0;
        }

        void SelectProductFromSearch()
        {
            if (list.SelectedItem is not ProductTile product)
            {
                return;
            }

            ApplyProductSelection(product);
            dialog.Close();
        }

        queryBox.TextChanged += (_, _) => RefreshSearch();
        list.MouseDoubleClick += (_, _) => SelectProductFromSearch();
        dialog.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectProductFromSearch();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogLabel("Digite codigo, nome ou grupo. Use setas e Enter para selecionar."));
        panel.Children.Add(queryBox);
        panel.Children.Add(list);
        panel.Children.Add(DialogHint("A pesquisa tambem abre digitando 0 no campo Codigo e pressionando F2/Enter."));
        dialog.Content = panel;
        RefreshSearch();
        queryBox.Focus();
        dialog.ShowDialog();
    }

    private void ShowProductCatalogDialog()
    {
        if (!RequirePermission(user => IsManagerUser(user) || user.CanManageProducts, "Cadastro de produtos"))
        {
            return;
        }

        var dialog = CreateDialog("Cadastro de produtos", 1040, 680);
        var productsList = new ListBox
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent
        };
        productsList.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse("""
<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <Border Background="#F8FBFD" BorderBrush="#CAD6E2" BorderThickness="1" CornerRadius="8" Padding="10" Margin="0,0,0,7">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <TextBlock Text="{Binding Name}" Foreground="#071A2C" FontWeight="SemiBold" FontSize="13" TextTrimming="CharacterEllipsis"/>
      <TextBlock Grid.Column="1" Text="{Binding PriceText}" Foreground="#08A99B" FontWeight="Bold" Margin="12,0,0,0"/>
      <TextBlock Grid.Row="1" Text="{Binding Code}" Foreground="#5B6B7A" FontSize="11" Margin="0,3,0,0"/>
      <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding Category}" Foreground="#5B6B7A" FontSize="11" HorizontalAlignment="Right" Margin="12,3,0,0"/>
      <TextBlock Grid.Row="2" Grid.ColumnSpan="2" Text="{Binding ProfitMarginText}" Foreground="#5B6B7A" FontSize="11" Margin="0,3,0,0"/>
    </Grid>
  </Border>
</DataTemplate>
""");

        var queryBox = new TextBox { ToolTip = "Buscar produto" };
        var countText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            Margin = new Thickness(0, 6, 0, 10)
        };
        var formTitle = new TextBlock
        {
            Text = "Novo produto",
            Foreground = Solid("#071A2C"),
            FontSize = 20,
            FontWeight = FontWeights.Bold
        };
        var formSubtitle = new TextBlock
        {
            Text = "Preencha nome, categoria e preco.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 0)
        };
        var statusText = new TextBlock
        {
            Foreground = GreenText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var codeBox = new TextBox();
        var nameBox = new TextBox();
        var costBox = new TextBox();
        var priceBox = new TextBox();
        var stockBox = new TextBox();
        var minBox = new TextBox();
        var imagePath = "";
        var imageText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var imagePreview = new System.Windows.Controls.Image
        {
            Width = 104,
            Height = 84,
            Stretch = Stretch.UniformToFill
        };
        var imagePreviewFrame = new Border
        {
            Width = 112,
            Height = 92,
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4),
            Child = imagePreview
        };
        var groupBox = new ComboBox
        {
            ItemsSource = Categories.Select(category => category.Name).ToList(),
            IsEditable = true,
            MinHeight = 34
        };
        var sectorBox = new ComboBox
        {
            ItemsSource = GetConfiguredProductSectors(),
            IsEditable = true,
            MinHeight = 34
        };
        void RefreshSectorOptions(string currentSector = "")
        {
            var sectors = GetConfiguredProductSectors();
            var cleanCurrent = NormalizeProductDestination(currentSector, "");
            if (!string.IsNullOrWhiteSpace(cleanCurrent)
                && sectors.All(item => !string.Equals(item, cleanCurrent, StringComparison.OrdinalIgnoreCase)))
            {
                sectors.Add(cleanCurrent);
            }

            sectorBox.ItemsSource = sectors;
        }
        var pizzaBox = new CheckBox { Content = "Pizza / produto com sabores", Margin = new Thickness(0, 8, 0, 4) };
        var activeBox = new CheckBox { Content = "Mostrar na venda", IsChecked = true, Margin = new Thickness(0, 2, 0, 0) };
        var marginText = new TextBlock
        {
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 8)
        };
        var ifoodCompositionVisible = HasIFoodConnectionConfigured(_appSettings.IFood);
        var ifoodInfoText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var ifoodApplyButton = DialogButton("Aplicar no iFood", "#0B3A52");
        ifoodApplyButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        ifoodApplyButton.Width = double.NaN;
        ifoodApplyButton.Margin = new Thickness(0);
        var modifiersBox = new TextBox
        {
            Height = 92,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Visibility = Visibility.Collapsed
        };
        var recipeBox = new TextBox
        {
            Height = 92,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Visibility = Visibility.Collapsed
        };
        var chooseImageButton = DialogButton("Escolher foto", "#0B3A52");
        var clearImageButton = DialogButton("Remover foto", "#5B6B7A");

        void FocusAndSelect(Control control)
        {
            control.Focus();
            if (control is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        Border FormSection(string title, params UIElement[] children)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Solid("#071A2C"),
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Margin = new Thickness(0, 0, 0, 8)
            });
            foreach (var child in children)
            {
                stack.Children.Add(child);
            }

            return new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 12),
                Child = stack
            };
        }

        Grid TwoColumns(UIElement left, UIElement right)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            if (left is FrameworkElement leftElement)
            {
                leftElement.Margin = new Thickness(0, 0, 8, 0);
            }
            if (right is FrameworkElement rightElement)
            {
                rightElement.Margin = new Thickness(8, 0, 0, 0);
            }
            grid.Children.Add(left);
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
            return grid;
        }

        Grid ProductPhotoEditor()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            imagePreviewFrame.Margin = new Thickness(0, 0, 14, 0);
            grid.Children.Add(imagePreviewFrame);

            var actions = new StackPanel();
            actions.Children.Add(imageText);
            chooseImageButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            chooseImageButton.Width = double.NaN;
            chooseImageButton.Margin = new Thickness(0, 0, 0, 0);
            clearImageButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            clearImageButton.Width = double.NaN;
            actions.Children.Add(chooseImageButton);
            actions.Children.Add(clearImageButton);
            actions.Children.Add(DialogHint("A foto aparece no cardapio digital e tambem vai para o iFood ao clicar em Aplicar no iFood."));
            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);
            return grid;
        }

        string FormatModifiers(IEnumerable<ProductModifier> modifiers)
        {
            return string.Join(Environment.NewLine, modifiers.Select(item =>
                $"{item.Name};{item.Price.ToString("N2", Brazil)};{(item.Required ? "SIM" : "NAO")}"));
        }

        string FormatRecipeItems(IEnumerable<ProductRecipeItem> items)
        {
            return string.Join(Environment.NewLine, items.Select(item =>
                $"{item.ProductCode};{item.Name};{item.Quantity.ToString("N2", Brazil)};{item.Unit}"));
        }

        void RefreshIFoodCompositionState()
        {
            var product = productsList.SelectedItem as ProductTile;
            var hasConnection = HasIFoodConnectionConfigured(_appSettings.IFood);
            var hasLink = product is not null && HasIFoodCatalogLink(product);
            ifoodApplyButton.IsEnabled = hasConnection && hasLink;
            ifoodInfoText.Text = !hasConnection
                ? "Conecte o iFood para aplicar estoque, disponibilidade e foto."
                : product is null
                    ? "Selecione um produto para aplicar no iFood."
                    : hasLink
                        ? "Aplica estoque, disponibilidade e foto deste produto no iFood."
                        : "Produto sem vinculo de catalogo iFood. Importe/sincronize produtos do iFood primeiro.";
        }

        void RefreshProductList(ProductTile? selected = null)
        {
            var query = queryBox.Text.Trim();
            var filtered = Products
                .Where(product => string.IsNullOrWhiteSpace(query)
                    || product.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || product.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Code)
                .ToList();

            productsList.ItemsSource = filtered;
            countText.Text = filtered.Count == 1 ? "1 produto cadastrado" : $"{filtered.Count:N0} produtos cadastrados";
            if (selected is not null && filtered.Contains(selected))
            {
                productsList.SelectedItem = selected;
                productsList.ScrollIntoView(selected);
            }
            else if (filtered.Count > 0 && productsList.SelectedItem is null)
            {
                productsList.SelectedIndex = 0;
            }
        }

        void RefreshMarginPreview()
        {
            var cost = ParseMoney(costBox.Text, 0);
            var sale = ParseMoney(priceBox.Text, 0);
            var profit = sale - cost;
            var margin = sale > 0 ? profit / sale * 100m : 0m;
            marginText.Text = $"Lucro un.: {Money(profit)}  |  Margem: {margin:N2}%";
            marginText.Foreground = profit < 0 ? RedText : profit == 0 ? Solid("#5B6B7A") : GreenText;
        }

        void RefreshProductImagePreview()
        {
            imagePreview.Source = null;
            var path = (imagePath ?? "").Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                imageText.Text = "Sem foto cadastrada.";
                clearImageButton.IsEnabled = false;
                return;
            }

            imageText.Text = Path.GetFileName(path);
            clearImageButton.IsEnabled = true;
            if (!File.Exists(path))
            {
                imageText.Text = "Foto nao encontrada nesta maquina.";
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                imagePreview.Source = bitmap;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException or FileFormatException)
            {
                imageText.Text = "Nao consegui abrir esta foto.";
            }
        }

        void StartNewProduct()
        {
            productsList.SelectedIndex = -1;
            codeBox.Text = NextProductCode();
            nameBox.Text = "";
            costBox.Text = "0,00";
            priceBox.Text = "0,00";
            stockBox.Text = "0";
            minBox.Text = "0";
            groupBox.SelectedIndex = groupBox.Items.Count > 0 ? 0 : -1;
            RefreshSectorOptions();
            sectorBox.Text = "CAIXA";
            pizzaBox.IsChecked = false;
            activeBox.IsChecked = true;
            imagePath = "";
            modifiersBox.Text = "";
            recipeBox.Text = "";
            formTitle.Text = "Novo produto";
            formSubtitle.Text = "Informe compra, venda e estoque. A margem calcula sozinha.";
            statusText.Text = "";
            RefreshMarginPreview();
            RefreshProductImagePreview();
            RefreshIFoodCompositionState();
            FocusAndSelect(nameBox);
            SetStatus("Novo produto.");
        }

        void LoadProduct(ProductTile product)
        {
            codeBox.Text = product.Code;
            nameBox.Text = product.Name;
            costBox.Text = product.CostPrice.ToString("N2", Brazil);
            priceBox.Text = product.Price.ToString("N2", Brazil);
            stockBox.Text = product.StockQuantity.ToString("N0", Brazil);
            minBox.Text = product.MinimumStock.ToString("N0", Brazil);
            groupBox.SelectedItem = product.Category;
            RefreshSectorOptions(product.Sector);
            sectorBox.Text = NormalizeProductDestination(product.Sector, "CAIXA");
            pizzaBox.IsChecked = product.IsPizza;
            activeBox.IsChecked = product.Active;
            imagePath = product.ImagePath ?? "";
            modifiersBox.Text = FormatModifiers(product.Modifiers);
            recipeBox.Text = FormatRecipeItems(product.RecipeItems);
            formTitle.Text = product.Name;
            var ifoodSummary = ifoodCompositionVisible ? $"  |  {product.IFoodCompositionText}" : "";
            formSubtitle.Text = $"{product.Code}  |  {product.Category}  |  venda {product.PriceText}  |  {product.ProfitMarginText}  |  {product.ImageStatusText}{ifoodSummary}";
            statusText.Text = "";
            RefreshMarginPreview();
            RefreshProductImagePreview();
            RefreshIFoodCompositionState();
        }

        productsList.SelectionChanged += (_, _) =>
        {
            if (productsList.SelectedItem is ProductTile product) LoadProduct(product);
        };
        queryBox.TextChanged += (_, _) => RefreshProductList();

        bool SaveProduct()
        {
            var code = string.IsNullOrWhiteSpace(codeBox.Text) ? NextProductCode() : codeBox.Text.Trim().PadLeft(6, '0');
            var name = nameBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(name))
            {
                statusText.Foreground = RedText;
                statusText.Text = "Informe o nome do produto.";
                FocusAndSelect(nameBox);
                return false;
            }

            var category = (groupBox.Text ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(category))
            {
                category = "GERAL";
            }

            var cost = ParseMoney(costBox.Text, -1);
            if (cost < 0)
            {
                statusText.Foreground = RedText;
                statusText.Text = "Informe um preco de compra valido.";
                FocusAndSelect(costBox);
                return false;
            }

            var price = ParseMoney(priceBox.Text, -1);
            if (price < 0)
            {
                statusText.Foreground = RedText;
                statusText.Text = "Informe um preco de venda valido.";
                FocusAndSelect(priceBox);
                return false;
            }

            if (Categories.All(item => !string.Equals(item.Name, category, StringComparison.OrdinalIgnoreCase)))
            {
                Categories.Add(new CategoryTile(category));
                groupBox.ItemsSource = Categories.Select(item => item.Name).ToList();
            }

            var productDestination = NormalizeProductDestination(sectorBox.Text, "CAIXA");
            if (!TryEnsureProductionDestinationConfigured(productDestination))
            {
                statusText.Foreground = RedText;
                statusText.Text = "Escolha a impressora do novo destino ou use CAIXA.";
                FocusAndSelect(sectorBox);
                return false;
            }

            var product = Products.FirstOrDefault(item => item.Code == code);
            if (product is null)
            {
                product = new ProductTile();
                Products.Add(product);
            }

            product ??= new ProductTile();
            product.Code = code;
            product.Name = name;
            product.Category = category;
            product.CostPrice = cost;
            product.Price = price;
            product.StockQuantity = ParseMoney(stockBox.Text, 0);
            product.MinimumStock = ParseMoney(minBox.Text, 0);
            product.Sector = productDestination;
            RefreshSectorOptions(product.Sector);
            product.IsPizza = pizzaBox.IsChecked == true;
            product.Active = activeBox.IsChecked == true;
            product.ImagePath = (imagePath ?? "").Trim();
            product.WhatsAppCode = "";
            product.WhatsAppAliases = "";
            if (ifoodCompositionVisible)
            {
                product.IFoodCompositionEnabled = false;
                product.Modifiers = [];
                product.RecipeItems = [];
            }
            ProductsList.Items.Refresh();
            RefreshProductList(product);
            FilterProducts();
            SaveStore();
            statusText.Foreground = GreenText;
            statusText.Text = $"Produto salvo: {product.Name}";
            formTitle.Text = product.Name;
            var ifoodSummary = ifoodCompositionVisible ? $"  |  {product.IFoodCompositionText}" : "";
            formSubtitle.Text = $"{product.Code}  |  {product.Category}  |  venda {product.PriceText}  |  {product.ProfitMarginText}  |  {product.ImageStatusText}{ifoodSummary}";
            RefreshMarginPreview();
            RefreshIFoodCompositionState();
            SetStatus($"Produto salvo: {product.Code} {product.Name}");
            QueueIFoodStockSync(product, "Cadastro de produto");
            return true;
        }

        var addProductButton = DialogButton("Novo produto", "#0B3A52");
        addProductButton.Click += (_, _) => StartNewProduct();

        var saveButton = DialogButton("Salvar produto", "#0B3A52");
        saveButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        saveButton.Width = double.NaN;
        saveButton.Click += (_, _) => SaveProduct();
        ifoodApplyButton.Click += (_, _) =>
        {
            var product = productsList.SelectedItem as ProductTile;
            if (product is null)
            {
                statusText.Foreground = RedText;
                statusText.Text = "Selecione um produto para aplicar no iFood.";
                return;
            }

            if (!SaveProduct())
            {
                return;
            }

            product = productsList.SelectedItem as ProductTile ?? Products.FirstOrDefault(item => item.Code == codeBox.Text.Trim().PadLeft(6, '0'));
            if (product is null)
            {
                return;
            }

            if (!HasIFoodCatalogLink(product))
            {
                statusText.Foreground = RedText;
                statusText.Text = "Produto sem vinculo iFood. Sincronize produtos do iFood antes de aplicar.";
                RefreshIFoodCompositionState();
                return;
            }

            statusText.Foreground = Solid("#0B3A52");
            statusText.Text = "Aplicando estoque, disponibilidade e foto no iFood...";
            QueueIFoodStockSync(product, "Aplicado no cadastro de produto");
            RefreshIFoodCompositionState();
        };

        chooseImageButton.Click += (_, _) =>
        {
            var fileDialog = new OpenFileDialog
            {
                Title = "Escolher foto do produto",
                Filter = "Imagens|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos os arquivos|*.*"
            };

            if (fileDialog.ShowDialog(this) != true)
            {
                return;
            }

            var codeForFile = NormalizeProductCode(codeBox.Text);
            if (string.IsNullOrWhiteSpace(codeForFile))
            {
                codeForFile = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }

            imagePath = CopyImageToAppIdentityFolder(fileDialog.FileName, $"product-{SafeFileName(codeForFile)}");
            RefreshProductImagePreview();
            statusText.Foreground = GreenText;
            statusText.Text = "Foto aplicada. Salve o produto e use Aplicar no iFood para enviar.";
        };

        clearImageButton.Click += (_, _) =>
        {
            imagePath = "";
            RefreshProductImagePreview();
            statusText.Foreground = Solid("#5B6B7A");
            statusText.Text = "Foto removida. Clique em Salvar produto.";
        };

        void OnEnter(UIElement element, Action action)
        {
            element.PreviewKeyDown += (_, e) =>
            {
                if ((e.Key == Key.Enter || e.Key == Key.Return) && Keyboard.Modifiers == ModifierKeys.None)
                {
                    action();
                    e.Handled = true;
                }
            };
        }

        OnEnter(codeBox, () => FocusAndSelect(nameBox));
        OnEnter(nameBox, () => FocusAndSelect(groupBox));
        OnEnter(groupBox, () => FocusAndSelect(costBox));
        OnEnter(costBox, () => FocusAndSelect(priceBox));
        OnEnter(priceBox, () => FocusAndSelect(sectorBox));
        OnEnter(sectorBox, () => FocusAndSelect(stockBox));
        OnEnter(stockBox, () => FocusAndSelect(minBox));
        OnEnter(minBox, () =>
        {
            if (SaveProduct())
            {
                StartNewProduct();
            }
        });
        OnEnter(saveButton, () => SaveProduct());
        costBox.TextChanged += (_, _) => RefreshMarginPreview();
        priceBox.TextChanged += (_, _) => RefreshMarginPreview();

        var root = new Grid { Margin = new Thickness(18) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftGrid = new Grid();
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftGrid.Children.Add(new TextBlock
        {
            Text = "Produtos",
            Foreground = Solid("#071A2C"),
            FontSize = 18,
            FontWeight = FontWeights.Bold
        });
        var searchPanel = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        searchPanel.Children.Add(DialogLabel("Buscar"));
        searchPanel.Children.Add(queryBox);
        searchPanel.Children.Add(countText);
        Grid.SetRow(searchPanel, 1);
        leftGrid.Children.Add(searchPanel);
        Grid.SetRow(productsList, 2);
        leftGrid.Children.Add(productsList);
        addProductButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        addProductButton.Width = double.NaN;
        Grid.SetRow(addProductButton, 3);
        leftGrid.Children.Add(addProductButton);

        var leftCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 16, 0),
            Child = leftGrid
        };
        root.Children.Add(leftCard);

        var formHeader = new Border
        {
            Background = Solid("#E6FBF8"),
            BorderBrush = Solid("#08A99B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel { Children = { formTitle, formSubtitle } }
        };

        var basicSection = FormSection("Produto",
            TwoColumns(DialogField("Codigo", codeBox), DialogField("Nome do produto", nameBox)),
            DialogField("Categoria", groupBox));
        var photoSection = FormSection("Foto do produto", ProductPhotoEditor());

        var saleSection = FormSection("Compra e venda",
            TwoColumns(DialogField("Preco de compra", costBox), DialogField("Preco de venda", priceBox)),
            marginText,
            DialogField("Destino do produto", sectorBox),
            DialogHint("CAIXA nao manda para producao. COZINHA imprime uma ordem agrupada depois que o pedido entrar. Digite outro destino para cadastrar a impressora dele."),
            activeBox,
            pizzaBox);

        var stockSection = FormSection("Estoque",
            TwoColumns(DialogField("Quantidade atual", stockBox), DialogField("Estoque minimo", minBox)));
        var ifoodSection = FormSection("iFood",
            ifoodInfoText,
            ifoodApplyButton);

        var form = new StackPanel();
        form.Children.Add(formHeader);
        form.Children.Add(basicSection);
        form.Children.Add(photoSection);
        form.Children.Add(saleSection);
        form.Children.Add(stockSection);
        form.Children.Add(ifoodSection);
        form.Children.Add(saveButton);
        form.Children.Add(statusText);

        var scroll = new ScrollViewer
        {
            Content = form,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetColumn(scroll, 1);
        root.Children.Add(scroll);
        dialog.Content = root;

        RefreshProductList();
        if (Products.Count == 0)
        {
            StartNewProduct();
        }
        else
        {
            productsList.SelectedIndex = 0;
        }

        nameBox.Focus();
        nameBox.SelectAll();
        dialog.ShowDialog();
    }

    private void ShowUsersDialog()
    {
        if (!RequirePermission(IsManagerUser, "Cadastro de usuarios"))
        {
            return;
        }

        var dialog = CreateDialog("Usuarios e permissoes", 820, 690);
        var usersList = new ListBox { DisplayMemberPath = nameof(UserAccount.Display), Width = 300 };
        usersList.ItemsSource = Users;
        var nameBox = new TextBox();
        var pinBox = new TextBox();
        var roleBox = new ComboBox { ItemsSource = new[] { "MASTER", "CAIXA", "GARCOM", "GERENTE", "COZINHA" }, SelectedIndex = 1, MinHeight = 34 };
        var masterBox = new CheckBox { Content = "Master" };
        var transferBox = new CheckBox { Content = "Transferir comandas completas" };
        var cancelBox = new CheckBox { Content = "Excluir/cancelar comandas" };
        var discountBox = new CheckBox { Content = "Conceder desconto" };
        var productsBox = new CheckBox { Content = "Cadastrar produtos" };
        var reportsBox = new CheckBox { Content = "Ver relatorios" };
        var cashBox = new CheckBox { Content = "Operar caixa e retiradas" };
        var deliveryBox = new CheckBox { Content = "Criar delivery/retirada" };
        var inventoryBox = new CheckBox { Content = "Ajustar estoque" };
        var kitchenBox = new CheckBox { Content = "Operar cozinha/KDS" };
        var ifoodBox = new CheckBox { Content = "Configurar iFood" };
        var settingsBox = new CheckBox { Content = "Configuracoes do sistema/cardapio" };
        var backupBox = new CheckBox { Content = "Backup/exportacao" };
        var fiscalBox = new CheckBox { Content = "Modulo fiscal/TEF" };
        var zonesBox = new CheckBox { Content = "Taxas por raio no mapa" };
        var syncBox = new CheckBox { Content = "Sync central/nuvem" };

        void LoadUser(UserAccount user)
        {
            nameBox.Text = user.Name;
            pinBox.Text = string.IsNullOrWhiteSpace(user.PinHash) ? user.Pin : "";
            pinBox.ToolTip = string.IsNullOrWhiteSpace(user.PinHash)
                ? "PIN antigo. Ao salvar, ele sera protegido com hash."
                : "Senha protegida. Deixe em branco para manter a senha atual.";
            roleBox.SelectedItem = user.Role;
            masterBox.IsChecked = user.IsMaster;
            transferBox.IsChecked = user.CanTransfer;
            cancelBox.IsChecked = user.CanCancel;
            discountBox.IsChecked = user.CanDiscount;
            productsBox.IsChecked = user.CanManageProducts;
            reportsBox.IsChecked = user.CanReports;
            cashBox.IsChecked = user.CanCash;
            deliveryBox.IsChecked = user.CanDelivery;
            inventoryBox.IsChecked = user.CanInventory;
            kitchenBox.IsChecked = user.CanKitchen;
            ifoodBox.IsChecked = user.CanIFood;
            settingsBox.IsChecked = user.CanSettings;
            backupBox.IsChecked = user.CanBackup;
            fiscalBox.IsChecked = user.CanFiscal;
            zonesBox.IsChecked = user.CanDeliveryZones;
            syncBox.IsChecked = user.CanCentralSync;
        }

        usersList.SelectionChanged += (_, _) =>
        {
            if (usersList.SelectedItem is UserAccount user) LoadUser(user);
        };

        var saveButton = DialogButton("Salvar usuario", "#08A99B");
        saveButton.Click += (_, _) =>
        {
            var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "OPERADOR" : nameBox.Text.Trim().ToUpperInvariant();
            var user = Users.FirstOrDefault(item => item.Name == name);
            if (user is null)
            {
                user = new UserAccount();
                Users.Add(user);
            }

            user.Role = roleBox.SelectedItem?.ToString() ?? "CAIXA";
            user.IsMaster = masterBox.IsChecked == true;
            user.CanTransfer = transferBox.IsChecked == true || user.IsMaster;
            user.CanCancel = cancelBox.IsChecked == true || user.IsMaster;
            user.CanDiscount = discountBox.IsChecked == true || user.IsMaster;
            user.CanManageProducts = productsBox.IsChecked == true || user.IsMaster;
            user.CanReports = reportsBox.IsChecked == true || user.IsMaster;
            user.CanCash = cashBox.IsChecked == true || user.IsMaster;
            user.CanDelivery = deliveryBox.IsChecked == true || user.IsMaster;
            user.CanInventory = inventoryBox.IsChecked == true || user.IsMaster;
            user.CanKitchen = kitchenBox.IsChecked == true || user.IsMaster;
            user.CanIFood = ifoodBox.IsChecked == true || user.IsMaster;
            user.CanSettings = settingsBox.IsChecked == true || user.IsMaster;
            user.CanBackup = backupBox.IsChecked == true || user.IsMaster;
            user.CanFiscal = fiscalBox.IsChecked == true || user.IsMaster;
            user.CanDeliveryZones = zonesBox.IsChecked == true || user.IsMaster;
            user.CanCentralSync = syncBox.IsChecked == true || user.IsMaster;
            user.Name = name;
            if (!string.IsNullOrWhiteSpace(pinBox.Text) || string.IsNullOrWhiteSpace(user.PinHash))
            {
                SetUserPassword(user, string.IsNullOrWhiteSpace(pinBox.Text) ? "0000" : pinBox.Text.Trim());
            }
            NormalizeRolePermissions(user);
            usersList.Items.Refresh();
            SaveStore();
            SetStatus($"Usuario salvo: {user.Name}");
        };

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(usersList);

        var form = DialogPanel();
        form.Children.Add(DialogLabel("Nome"));
        form.Children.Add(nameBox);
        form.Children.Add(DialogLabel("PIN / senha"));
        form.Children.Add(pinBox);
        form.Children.Add(DialogLabel("Perfil"));
        form.Children.Add(roleBox);
        form.Children.Add(masterBox);
        form.Children.Add(transferBox);
        form.Children.Add(cancelBox);
        form.Children.Add(discountBox);
        form.Children.Add(productsBox);
        form.Children.Add(reportsBox);
        form.Children.Add(cashBox);
        form.Children.Add(deliveryBox);
        form.Children.Add(inventoryBox);
        form.Children.Add(kitchenBox);
        form.Children.Add(ifoodBox);
        form.Children.Add(settingsBox);
        form.Children.Add(backupBox);
        form.Children.Add(fiscalBox);
        form.Children.Add(zonesBox);
        form.Children.Add(syncBox);
        form.Children.Add(saveButton);
        Grid.SetColumn(form, 1);
        grid.Children.Add(form);
        dialog.Content = grid;
        if (Users.Count > 0) usersList.SelectedIndex = 0;
        dialog.ShowDialog();
    }

    private void ShowDeliveryOrderDialog()
    {
        if (!RequirePermission(CanOperateDelivery, "Delivery"))
        {
            return;
        }

        var dialog = CreateDialog("Novo pedido delivery / retirada", 760, 690);
        dialog.ResizeMode = ResizeMode.NoResize;
        var orderType = "ENTREGA";
        var printSize = _appSettings.PrintLayout;
        var autoPrint = _appSettings.AutoPrintDelivery;
        var cpfBox = new TextBox();
        var phoneBox = new TextBox();
        var nameBox = new TextBox();
        var addressBox = new TextBox();
        var districtBox = new TextBox();
        var feeBox = new TextBox { Text = "8,00" };
        var zoneText = new TextBlock
        {
            Foreground = AmberText,
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var driverBox = new ComboBox { ItemsSource = Drivers.Select(driver => driver.Name).ToList(), MinHeight = 34 };
        var notesBox = new TextBox { Height = 62, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        var typeButtons = new List<Button>();
        var sizeButtons = new List<Button>();
        var typeGrid = new UniformGrid { Columns = 3, Rows = 1, Margin = new Thickness(0, 6, 0, 10) };
        var sizeGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 6, 0, 10) };
        var printCard = new Border
        {
            Background = Solid("#E6FBF8"),
            BorderBrush = Solid("#08A99B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 10)
        };
        var printTitle = new TextBlock { FontWeight = FontWeights.Bold };
        var printHint = new TextBlock { Foreground = Solid("#5B6B7A"), FontSize = 11, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap };

        void RefreshButtons(IEnumerable<Button> buttons, string selected)
        {
            foreach (var button in buttons)
            {
                var active = string.Equals(button.Tag?.ToString(), selected, StringComparison.Ordinal);
                button.Background = active ? Solid("#EAF8FA") : Brushes.White;
                button.BorderBrush = active ? Solid("#0B3A52") : Solid("#CAD6E2");
                button.Foreground = active ? Solid("#0B3A52") : Solid("#071A2C");
                button.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
            }
        }

        Button SegmentButton(string text)
        {
            return new Button
            {
                Content = text,
                Tag = text,
                Height = 42,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Template = RoundedButtonTemplate()
            };
        }

        foreach (var type in new[] { "ENTREGA", "RETIRADA", "BALCAO" })
        {
            var button = SegmentButton(type);
            button.Click += (_, _) =>
            {
                orderType = type;
                RefreshButtons(typeButtons, orderType);
                if (orderType == "ENTREGA")
                {
                    ApplyDeliveryZoneFee(districtBox.Text, feeBox, zoneText);
                }
                else
                {
                    feeBox.Text = "0,00";
                    zoneText.Text = "Retirada/balcao nao cobra taxa.";
                    zoneText.Foreground = Solid("#5B6B7A");
                }
            };
            typeButtons.Add(button);
            typeGrid.Children.Add(button);
        }

        foreach (var size in new[] { "PEQUENO", "GRANDE" })
        {
            var button = SegmentButton(size);
            button.Click += (_, _) =>
            {
                printSize = size;
                RefreshButtons(sizeButtons, printSize);
            };
            sizeButtons.Add(button);
            sizeGrid.Children.Add(button);
        }

        void RefreshPrintCard()
        {
            printCard.Background = autoPrint ? Solid("#E6FBF8") : Brushes.White;
            printCard.BorderBrush = autoPrint ? Solid("#08A99B") : Solid("#CAD6E2");
            printTitle.Foreground = autoPrint ? Solid("#08A99B") : Solid("#071A2C");
            printTitle.Text = autoPrint ? "Imprimir automaticamente" : "Nao imprimir automaticamente";
            printHint.Text = autoPrint
                ? "Ao criar, envia o pedido para a impressora padrao do Windows."
                : "Cria o pedido sem mandar para a impressora.";
        }

        printCard.MouseLeftButtonDown += (_, _) =>
        {
            autoPrint = !autoPrint;
            RefreshPrintCard();
        };
        printCard.Child = new StackPanel { Children = { printTitle, printHint } };

        void LoadCustomerIntoDelivery(CustomerRecord customer)
        {
            cpfBox.Text = customer.Cpf;
            phoneBox.Text = customer.Phone;
            nameBox.Text = customer.Name;
            addressBox.Text = customer.Address;
            districtBox.Text = customer.District;
            if (orderType == "ENTREGA")
            {
                ApplyDeliveryZoneFee(districtBox.Text, feeBox, zoneText);
            }
            if (!string.IsNullOrWhiteSpace(customer.Notes))
            {
                notesBox.Text = customer.Notes;
            }
        }

        districtBox.TextChanged += (_, _) =>
        {
            if (orderType == "ENTREGA")
            {
                ApplyDeliveryZoneFee(districtBox.Text, feeBox, zoneText);
            }
        };

        var includeCustomerButton = DialogButton("Incluir cliente cadastrado", "#0B3A52");
        includeCustomerButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        includeCustomerButton.Width = double.NaN;
        includeCustomerButton.Margin = new Thickness(0, 0, 0, 10);
        includeCustomerButton.Click += (_, _) =>
        {
            var customer = ShowCustomerPickerDialog();
            if (customer is null)
            {
                return;
            }

            LoadCustomerIntoDelivery(customer);
            SetStatus($"Cliente incluido no delivery: {customer.Name}");
        };

        var zonesButton = DialogButton("Taxas por raio no mapa", "#0B3A52");
        zonesButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        zonesButton.Width = double.NaN;
        zonesButton.Margin = new Thickness(0, 0, 0, 10);
        zonesButton.Click += (_, _) =>
        {
            ShowDeliveryZonesDialog();
            if (orderType == "ENTREGA")
            {
                ApplyDeliveryZoneFee(districtBox.Text, feeBox, zoneText);
            }
        };

        var createButton = DialogButton("Criar pedido", "#08A99B");
        createButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        createButton.Width = double.NaN;
        createButton.Click += (_, _) =>
        {
            var number = $"D{DeliveryTiles.Count + 1:00000}";
            var fee = ParseMoney(feeBox.Text, 0);
            var cpf = cpfBox.Text.Trim();
            var district = districtBox.Text.Trim();
            var tile = new TableTile
            {
                Number = number,
                Kind = "DELIVERY",
                Status = "NOVO",
                CustomerName = string.IsNullOrWhiteSpace(nameBox.Text) ? "CLIENTE BALCAO" : nameBox.Text.Trim().ToUpperInvariant(),
                CustomerCpf = cpf,
                Phone = phoneBox.Text.Trim(),
                Address = addressBox.Text.Trim(),
                District = district,
                Detail = string.IsNullOrWhiteSpace(district) ? orderType : $"{orderType} {district}",
                Driver = driverBox.SelectedItem?.ToString() ?? "",
                Notes = notesBox.Text.Trim()
            };

            if (fee > 0)
            {
                tile.Lines.Add(new TicketLine { Code = "000020", Name = "TAXA ENTREGA", Quantity = 1, UnitPrice = fee, Sector = "CAIXA" });
                tile.Total = fee;
            }

            DeliveryTiles.Add(tile);
            if (!string.IsNullOrWhiteSpace(tile.CustomerName))
            {
                UpsertCustomerRecord(tile.CustomerCpf, tile.CustomerName, tile.Phone, tile.Address, tile.District, tile.Notes);
            }

            SaveStore();
            ModeList.SelectedItem = "Delivery";
            RefreshBoardForMode();
            SelectTable(BoardTiles.Count - 1, saveCurrent: false);
            if (autoPrint)
            {
                var printed = TryPrintTextToDefaultPrinter(BuildDeliveryPrintText(tile, district, printSize), $"Pedido {tile.Number}", printSize == "PEQUENO");
                SetStatus(printed
                    ? $"Delivery criado e impresso: {number}"
                    : $"Delivery criado: {number}. Impressora padrao indisponivel.");
            }
            else
            {
                SetStatus($"Delivery criado: {number}");
            }
            dialog.Close();
        };

        var panel = new Grid { Margin = new Thickness(18) };
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });

        var summary = new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14, 10, 14, 10),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Cadastro rapido de pedido", Foreground = Solid("#071A2C"), FontWeight = FontWeights.Bold, FontSize = 15 },
                    new TextBlock { Text = "Cria o pedido e pode imprimir automaticamente na impressora padrao do computador.", Foreground = Solid("#5B6B7A"), FontSize = 12, Margin = new Thickness(0, 3, 0, 0) }
                }
            }
        };
        panel.Children.Add(summary);

        var body = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        Grid.SetRow(body, 1);

        var form = new Grid();
        form.ColumnDefinitions.Add(new ColumnDefinition());
        form.ColumnDefinitions.Add(new ColumnDefinition());
        form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cpfBlock = DialogField("CPF/CNPJ", cpfBox);
        var phoneBlock = DialogField("Telefone", phoneBox);
        var nameBlock = DialogField("Cliente", nameBox);
        var addressBlock = DialogField("Endereco", addressBox);
        var districtBlock = DialogField("Bairro / referencia", districtBox);
        var notesBlock = DialogField("Observacao", notesBox);
        Grid.SetColumn(phoneBlock, 1);
        Grid.SetRow(nameBlock, 1);
        Grid.SetColumnSpan(nameBlock, 2);
        Grid.SetRow(addressBlock, 2);
        Grid.SetColumnSpan(addressBlock, 2);
        Grid.SetRow(districtBlock, 3);
        Grid.SetColumnSpan(districtBlock, 2);
        Grid.SetRow(notesBlock, 4);
        Grid.SetColumnSpan(notesBlock, 2);
        form.Children.Add(cpfBlock);
        form.Children.Add(phoneBlock);
        form.Children.Add(nameBlock);
        form.Children.Add(addressBlock);
        form.Children.Add(districtBlock);
        form.Children.Add(notesBlock);
        body.Children.Add(form);

        var side = new StackPanel { Margin = new Thickness(16, 0, 0, 0) };
        side.Children.Add(includeCustomerButton);
        side.Children.Add(zonesButton);
        side.Children.Add(DialogLabel("Tipo"));
        side.Children.Add(typeGrid);
        side.Children.Add(DialogLabel("Entregador"));
        side.Children.Add(driverBox);
        side.Children.Add(DialogLabel("Taxa"));
        side.Children.Add(feeBox);
        side.Children.Add(zoneText);
        side.Children.Add(DialogLabel("Impressao"));
        side.Children.Add(printCard);
        side.Children.Add(sizeGrid);
        Grid.SetColumn(side, 1);
        body.Children.Add(side);
        panel.Children.Add(body);

        Grid.SetRow(createButton, 2);
        panel.Children.Add(createButton);
        dialog.Content = panel;
        RefreshButtons(typeButtons, orderType);
        RefreshButtons(sizeButtons, printSize);
        RefreshPrintCard();
        ApplyDeliveryZoneFee(districtBox.Text, feeBox, zoneText);
        phoneBox.Focus();
        dialog.ShowDialog();
    }

    private async Task StartWaiterServerAsync()
    {
        if (_waiterServer is not null)
        {
            return;
        }

        for (var port = 5050; port <= 5055; port++)
        {
            var server = new WaiterLocalServer(
                port,
                () => RunOnUiAsync(BuildWaiterState),
                request => RunOnUiAsync(() => OpenWaiterBoard(request)),
                request => RunOnUiAsync(() => AddWaiterProduct(request)),
                request => RunOnUiAsync(() => SaveWaiterBoardNote(request)),
                request => RunOnUiAsync(() => RemoveWaiterLine(request)),
                request => RunOnUiAsync(() => RequestWaiterBill(request)));

            try
            {
                await server.StartAsync();
                _waiterServer = server;
                SetStatus($"Garcom Web ativo: {server.NetworkUrl}");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Waiter web failed on port {port}: {ex.Message}");
                await server.DisposeAsync();
            }
        }

        SetStatus("Nao foi possivel iniciar o Garcom Web. Portas 5050-5055 ocupadas.");
    }

    private Task<T> RunOnUiAsync<T>(Func<T> action)
    {
        return Dispatcher.InvokeAsync(action).Task;
    }

    private void ShowWaiterWebDialog()
    {
        var dialog = CreateDialog("Garcom Web local", 760, 620);
        var networkUrl = _waiterServer?.NetworkUrl ?? "iniciando...";
        var localUrl = _waiterServer?.LocalUrl ?? "iniciando...";
        var urlBox = new TextBox { Text = networkUrl, IsReadOnly = true };
        var localBox = new TextBox { Text = localUrl, IsReadOnly = true };
        var waiterPhoneBox = new TextBox();
        var status = new TextBlock
        {
            Text = _waiterServer is null
                ? "Servidor do garcom ainda nao iniciou. Feche e abra esta tela em alguns segundos."
                : "No celular do garcom, escaneie o QR Code ou envie o link pelo WhatsApp. O aparelho precisa estar no mesmo Wi-Fi do caixa Windows.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#5B6B7A"),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var copy = DialogButton("Copiar link manualmente", "#5B6B7A");
        var open = DialogButton("Abrir neste PC", "#0B3A52");
        var sendWhatsApp = DialogButton("Enviar pelo WhatsApp", "#08A99B");
        copy.Click += (_, _) =>
        {
            System.Windows.Clipboard.SetText(networkUrl);
            SetStatus("Link do Garcom Web copiado.");
        };
        open.Click += (_, _) =>
        {
            if (_waiterServer is not null)
            {
                Process.Start(new ProcessStartInfo(localUrl) { UseShellExecute = true });
            }
        };
        sendWhatsApp.Click += (_, _) =>
        {
            if (_waiterServer is null)
            {
                SetStatus("Garcom Web ainda nao iniciou.");
                return;
            }

            var message = BuildWaiterShareMessage(networkUrl);
            var phone = NormalizeWaiterSharePhone(waiterPhoneBox.Text);
            var shareUrl = string.IsNullOrWhiteSpace(phone)
                ? $"https://wa.me/?text={Uri.EscapeDataString(message)}"
                : $"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}";
            Process.Start(new ProcessStartInfo(shareUrl) { UseShellExecute = true });
            SetStatus(string.IsNullOrWhiteSpace(phone)
                ? "WhatsApp aberto com o link do Garcom Web pronto para enviar."
                : "Conversa do WhatsApp aberta com o link do Garcom Web pronto.");
        };

        var qrSource = _waiterServer is null ? null : TryCreateQrBitmap(networkUrl, 7);
        var qrCard = new Border
        {
            Background = Solid("#FFFFFF"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 10, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = qrSource is null
                ? new TextBlock
                {
                    Text = "QR Code indisponivel. Use o botao Enviar pelo WhatsApp.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Solid("#5B6B7A"),
                    Width = 220
                }
                : new System.Windows.Controls.Image
                {
                    Source = qrSource,
                    Width = 220,
                    Height = 220,
                    Stretch = Stretch.Uniform,
                    ToolTip = networkUrl
                }
        };

        var shareGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        shareGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        shareGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var phoneField = DialogField("Telefone do garcom (opcional)", waiterPhoneBox);
        shareGrid.Children.Add(phoneField);
        sendWhatsApp.Margin = new Thickness(10, 22, 0, 0);
        sendWhatsApp.MinWidth = 210;
        Grid.SetColumn(sendWhatsApp, 1);
        shareGrid.Children.Add(sendWhatsApp);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        copy.Margin = new Thickness(8, 12, 0, 0);
        open.Margin = new Thickness(8, 12, 0, 0);
        buttons.Children.Add(open);
        buttons.Children.Add(copy);

        var panel = DialogPanel();
        panel.Children.Add(SectionTitle("Acesso do garcom"));
        panel.Children.Add(new TextBlock
        {
            Text = "Use rede local. O celular/tablet precisa estar no mesmo Wi-Fi do computador do caixa.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Mais facil: abra a camera do celular e aponte para o QR Code abaixo. Nao precisa copiar nem digitar no navegador.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 0)
        });
        panel.Children.Add(qrCard);
        panel.Children.Add(shareGrid);
        panel.Children.Add(DialogField("Link para celular/tablet", urlBox));
        panel.Children.Add(DialogField("Link neste computador", localBox));
        panel.Children.Add(status);
        panel.Children.Add(DialogHint("Se o celular nao abrir, libere o Balcao Livre PDV Online no Firewall do Windows e confira o IP do caixa."));
        panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private string BuildWaiterShareMessage(string url)
    {
        var restaurant = ResolveWaiterRestaurantName();
        var name = string.IsNullOrWhiteSpace(restaurant) ? "Balcao Livre" : restaurant.Trim();
        return $"Link do Garcom Web - {name}\n{url}\n\nAbra no celular conectado ao mesmo Wi-Fi do caixa.";
    }

    private static string NormalizeWaiterSharePhone(string value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        digits = digits.TrimStart('0');
        if (digits.Length is 10 or 11)
        {
            digits = $"55{digits}";
        }

        return digits.Length >= 12 ? digits : "";
    }

    private WaiterStateDto BuildWaiterState()
    {
        SaveActiveTicketToCurrentBoard();

        return new WaiterStateDto
        {
            RestaurantName = ResolveWaiterRestaurantName(),
            ServerTime = DateTime.Now,
            Boards = Tables
                .OrderBy(table => int.TryParse(table.Number, out var number) ? number : int.MaxValue)
                .ThenBy(table => table.Number)
                .Select(ToWaiterBoardDto)
                .ToList(),
            Products = Products
                .Where(product => product.Active)
                .OrderBy(product => product.Category)
                .ThenBy(product => product.Name)
                .Select(product => new WaiterProductDto
                {
                    Code = product.Code,
                    Name = product.Name,
                    Category = product.Category,
                    Price = product.Price,
                    PriceText = Money(product.Price),
                    Stock = product.StockQuantity
                })
                .ToList(),
            Categories = Products
                .Where(product => product.Active)
                .Select(product => product.Category)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList(),
            Staff = Users
                .Where(IsServiceOrCashUser)
                .Select(user => new WaiterStaffDto
                {
                    Number = StaffNumber(user),
                    Name = user.Name,
                    Role = user.Role
                })
                .Where(user => !string.IsNullOrWhiteSpace(user.Number))
                .OrderBy(user => user.Role)
                .ThenBy(user => int.TryParse(user.Number, out var number) ? number : int.MaxValue)
                .ToList()
        };
    }

    private string ResolveWaiterRestaurantName()
    {
        foreach (var value in new[]
        {
            _profile.BusinessName,
            _profile.LegalName,
            _profile.OwnerName
        })
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return AppDisplayName;
    }

    private static WaiterBoardDto ToWaiterBoardDto(TableTile board)
    {
        return new WaiterBoardDto
        {
            Kind = board.Kind,
            Number = board.Number,
            Title = board.DisplayTitle,
            Status = board.Status,
            CustomerName = board.CustomerName,
            Notes = board.Notes,
            Waiter = board.Waiter,
            Total = board.Total,
            TotalText = Money(board.Total),
            Lines = board.Lines.Select((line, index) => new WaiterLineDto
            {
                Index = index,
                Code = line.Code,
                Name = line.Name,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                UnitPriceText = Money(line.UnitPrice),
                Total = line.Total,
                TotalText = Money(line.Total),
                Note = line.Note
            }).ToList()
        };
    }

    private WaiterActionResult OpenWaiterBoard(WaiterOpenBoardRequest request)
    {
        var state = BuildWaiterState();
        if (!TryResolveWaiterStaff(request.WaiterNumber, out var waiterNumber, out var staffError))
        {
            return WaiterActionResult.Fail(staffError, state);
        }

        var board = FindWaiterBoard(request.BoardNumber);
        if (board is null)
        {
            return WaiterActionResult.Fail("Mesa nao existe. Crie as mesas no caixa Windows.", state);
        }

        if (IsIFoodDeliveryBoard(board))
        {
            return WaiterActionResult.Fail("Pedido iFood nao permite adicionar item manualmente.", state);
        }

        if (HasReceivedPayment(board))
        {
            return WaiterActionResult.Fail("Mesa ja recebida/finalizada. Abra outra mesa no caixa.", state);
        }

        if (IsClosedAccountForConference(board))
        {
            return WaiterActionResult.Fail(BuildClosedAccountWaiterMessage(board), state);
        }

        board.Waiter = waiterNumber;
        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            board.CustomerName = request.CustomerName.Trim().ToUpperInvariant();
        }

        if (board.Status == "LIVRE")
        {
            board.Status = "OCUPADA";
        }

        RefreshAfterWaiterMutation(board);
        return WaiterActionResult.Success($"Mesa {board.Number} aberta para o garcom {waiterNumber}.", BuildWaiterState());
    }

    private WaiterActionResult AddWaiterProduct(WaiterAddProductRequest request)
    {
        var state = BuildWaiterState();
        if (!TryResolveWaiterStaff(request.WaiterNumber, out var waiterNumber, out var staffError))
        {
            return WaiterActionResult.Fail(staffError, state);
        }

        var board = FindWaiterBoard(request.BoardNumber);
        if (board is null)
        {
            return WaiterActionResult.Fail("Mesa nao existe. Crie as mesas no caixa Windows.", state);
        }

        if (HasReceivedPayment(board))
        {
            return WaiterActionResult.Fail("Mesa ja recebida/finalizada. Nao da para lancar item.", state);
        }

        if (IsClosedAccountForConference(board))
        {
            return WaiterActionResult.Fail(BuildClosedAccountWaiterMessage(board), state);
        }

        var code = NormalizeProductCode(request.ProductCode);
        var product = Products.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(item => item.Code.Contains(request.ProductCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (product is null || !product.Active)
        {
            return WaiterActionResult.Fail("Produto nao encontrado no cadastro.", state);
        }

        var qty = Math.Max(1, request.Quantity);
        var lineToAdd = new TicketLine
        {
            Code = product.Code,
            Name = product.Name,
            Quantity = qty,
            UnitPrice = product.Price,
            Note = request.Note.Trim(),
            Sector = NormalizeProductDestination(product.Sector, "CAIXA")
        };
        var existing = board.Lines.FirstOrDefault(line => CanMergeTicketLine(line, lineToAdd));
        var kitchenLine = lineToAdd;
        if (existing is null)
        {
            board.Lines.Add(lineToAdd);
        }
        else
        {
            existing.Quantity += qty;
            kitchenLine = existing;
        }

        board.Waiter = waiterNumber;
        board.Status = "OCUPADA";
        board.Total = board.Lines.Sum(line => line.Total);
        var stockChanged = false;
        if (product.StockQuantity > 0)
        {
            product.StockQuantity = Math.Max(0, product.StockQuantity - qty);
            stockChanged = true;
        }

        product.SoldQuantity += qty;
        if (stockChanged)
        {
            QueueIFoodStockSync(product, $"Venda garcom web {board.Number}");
        }
        PrintWaiterKitchenLine(board, kitchenLine);
        RefreshAfterWaiterMutation(board);
        return WaiterActionResult.Success($"Incluido: {qty}x {product.Name}.", BuildWaiterState());
    }

    private WaiterActionResult SaveWaiterBoardNote(WaiterBoardNoteRequest request)
    {
        var state = BuildWaiterState();
        if (!TryResolveWaiterStaff(request.WaiterNumber, out var waiterNumber, out var staffError))
        {
            return WaiterActionResult.Fail(staffError, state);
        }

        var board = FindWaiterBoard(request.BoardNumber);
        if (board is null)
        {
            return WaiterActionResult.Fail("Mesa nao encontrada.", state);
        }

        if (IsIFoodDeliveryBoard(board))
        {
            return WaiterActionResult.Fail("Pedido iFood nao permite alterar observacao pelo garcom.", state);
        }

        if (HasReceivedPayment(board))
        {
            return WaiterActionResult.Fail("Mesa ja recebida/finalizada. Nao da para alterar observacao.", state);
        }

        if (IsClosedAccountForConference(board))
        {
            return WaiterActionResult.Fail(BuildClosedAccountWaiterMessage(board), state);
        }

        board.Waiter = waiterNumber;
        board.Notes = (request.Note ?? "").Trim();
        if (board.Status == "LIVRE" && !string.IsNullOrWhiteSpace(board.Notes))
        {
            board.Status = "OCUPADA";
        }

        RefreshAfterWaiterMutation(board);
        return WaiterActionResult.Success(
            string.IsNullOrWhiteSpace(board.Notes) ? "Observacao removida." : "Observacao salva na mesa.",
            BuildWaiterState());
    }

    private WaiterActionResult RemoveWaiterLine(WaiterRemoveLineRequest request)
    {
        var state = BuildWaiterState();
        var board = FindWaiterBoard(request.BoardNumber);
        if (board is null)
        {
            return WaiterActionResult.Fail("Mesa nao encontrada.", state);
        }

        if (IsIFoodDeliveryBoard(board))
        {
            return WaiterActionResult.Fail("Pedido iFood nao permite excluir item manualmente.", state);
        }

        if (IsClosedAccountForConference(board))
        {
            return WaiterActionResult.Fail(BuildClosedAccountWaiterMessage(board), state);
        }

        if (request.LineIndex < 0 || request.LineIndex >= board.Lines.Count)
        {
            return WaiterActionResult.Fail("Item nao encontrado na comanda.", state);
        }

        var line = board.Lines[request.LineIndex];
        if (IsTableCharge(line))
        {
            return WaiterActionResult.Fail("Taxa/couvert deve ser alterado no caixa.", state);
        }

        board.Lines.RemoveAt(request.LineIndex);
        var restoredProduct = Products.FirstOrDefault(product => string.Equals(product.Code, line.Code, StringComparison.OrdinalIgnoreCase));
        if (restoredProduct is not null)
        {
            restoredProduct.StockQuantity += Math.Max(0, line.Quantity);
            QueueIFoodStockSync(restoredProduct, $"Item removido garcom web {board.Number}");
        }

        board.Total = board.Lines.Sum(item => item.Total);
        if (board.Lines.Count == 0 && board.Payments.Count == 0)
        {
            board.Status = "LIVRE";
            board.Waiter = 0;
            board.CustomerName = "";
            board.Notes = "";
        }

        RefreshAfterWaiterMutation(board);
        return WaiterActionResult.Success($"Removido: {line.Name}.", BuildWaiterState());
    }

    private WaiterActionResult RequestWaiterBill(WaiterBoardRequest request)
    {
        var state = BuildWaiterState();
        var board = FindWaiterBoard(request.BoardNumber);
        if (board is null)
        {
            return WaiterActionResult.Fail("Mesa nao encontrada.", state);
        }

        if (board.Lines.Count == 0)
        {
            return WaiterActionResult.Fail("Mesa sem itens.", state);
        }

        if (request.Paid)
        {
            if (!IsCashOpen())
            {
                return WaiterActionResult.Fail("Caixa fechado. Abra o caixa no Windows antes de receber.", state);
            }

            var total = board.Lines.Sum(line => line.Total);
            var paidTotal = board.Payments.Sum(payment => payment.Amount);
            var balance = Math.Max(0, total - paidTotal);
            var method = NormalizeWaiterPaymentMethod(request.PaymentMethod);
            var tendered = request.TenderedAmount > 0 ? request.TenderedAmount : balance;
            if (tendered <= 0 || balance <= 0)
            {
                return WaiterActionResult.Fail("Valor recebido invalido.", state);
            }

            if (tendered < balance)
            {
                return WaiterActionResult.Fail($"Valor recebido menor que o saldo: {Money(balance)}.", state);
            }

            if (tendered > balance && method != "DINHEIRO")
            {
                return WaiterActionResult.Fail("Troco acima do saldo somente em DINHEIRO.", state);
            }

            var payment = new PaymentLine
            {
                Payer = string.IsNullOrWhiteSpace(request.Payer)
                    ? string.IsNullOrWhiteSpace(board.CustomerName) ? "Cliente" : board.CustomerName
                    : request.Payer.Trim(),
                Method = method,
                Amount = balance,
                TenderedAmount = tendered,
                ChangeAmount = Math.Max(0, tendered - balance),
                When = DateTime.Now
            };
            board.Payments.Add(payment);
            _cashTotal += payment.Amount;

            var paidReceiptNumber = NextReceiptNumber();
            var receiptText = BuildWaiterPaidReceipt(board, paidReceiptNumber);
            var path = Path.Combine(ExportDir, $"garcom-pago-{board.Number}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, receiptText, Encoding.UTF8);
            var paidPrinted = TryPrintTextToDefaultPrinter(
                receiptText,
                $"Pago mesa {board.Number}",
                compact: IsCompactReceiptLayout(),
                qrPayload: GetReceiptQrPayload(total),
                qrCaption: GetReceiptQrCaption(total));

            var closedLines = board.Lines.Select(CloneLine).ToList();
            var closedPayments = board.Payments.Select(ClonePayment).ToList();
            board.ClosedLines = closedLines;
            board.ClosedPayments = closedPayments;
            board.LastClosedAt = DateTime.Now;
            board.LastReceiptPath = path;
            board.Status = "LIVRE";
            board.Total = 0;
            board.Lines.Clear();
            board.Payments.Clear();
            ResetBoardAfterReceivedPayment(board);
            RefreshAfterWaiterMutation(board);
            return WaiterActionResult.Success(
                paidPrinted ? "Pagamento recebido e comprovante impresso." : "Pagamento recebido. Impressora indisponivel.",
                BuildWaiterState());
        }

        board.Status = "CONTA";
        var receiptNumber = NextReceiptNumber();
        var printText = BuildWaiterBillReceipt(board, receiptNumber);
        var printed = TryPrintTextToDefaultPrinter(
            printText,
            $"Conferencia mesa {board.Number}",
            compact: IsCompactReceiptLayout(),
            qrPayload: GetReceiptQrPayload(board.Total),
            qrCaption: GetReceiptQrCaption(board.Total));
        RefreshAfterWaiterMutation(board);
        return WaiterActionResult.Success(
            printed ? "Conta solicitada e impressa no caixa." : "Conta solicitada. Impressora indisponivel.",
            BuildWaiterState());
    }

    private static string NormalizeWaiterPaymentMethod(string method)
    {
        var normalized = (method ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "DINHEIRO" => "DINHEIRO",
            "PIX" => "PIX",
            "CREDITO" => "CREDITO",
            "CARTAO" => "CREDITO",
            "CARTAO CREDITO" => "CREDITO",
            "DEBITO" => "DEBITO",
            "CARTAO DEBITO" => "DEBITO",
            "VALE" => "VALE",
            "FIADO" => "FIADO",
            _ => "DINHEIRO"
        };
    }

    private TableTile? FindWaiterBoard(string number)
    {
        var normalized = NormalizeBoardNumber(number);
        return Tables.FirstOrDefault(table => string.Equals(table.Number, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryResolveWaiterStaff(string value, out int number, out string error)
    {
        number = 0;
        error = "";
        var normalized = NormalizeStaffNumber(value);
        if (!int.TryParse(normalized, NumberStyles.Integer, Brazil, out number) || number <= 0)
        {
            error = $"Informe o numero do garcom/operador. {BuildStaffOptions(IsServiceOrCashUser)}";
            return false;
        }

        var staff = FindAllowedStaffByNumber(number, IsServiceOrCashUser);
        if (staff is null)
        {
            error = $"Garcom/operador nao cadastrado. {BuildStaffOptions(IsServiceOrCashUser)}";
            return false;
        }

        return true;
    }

    private void RefreshAfterWaiterMutation(TableTile board)
    {
        board.Total = board.Lines.Sum(line => line.Total);
        if (ReferenceEquals(CurrentBoard, board))
        {
            LoadActiveTicketFromBoard(board);
            RefreshTotals();
            TicketList.Items.Refresh();
        }

        TablesList.Items.Refresh();
        SaveStore();
    }

    private void PrintWaiterKitchenLine(TableTile board, TicketLine line)
    {
        ScheduleKitchenPrint(board, [line]);
    }

    private string BuildWaiterKitchenText(TableTile board, IEnumerable<TicketLine> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{board.Kind} {board.Number}");
        var sector = NormalizeSectorName(lines.FirstOrDefault()?.Sector, "SEM SETOR");
        sb.AppendLine($"SETOR {sector}");
        var staffLine = BuildStaffReceiptLine(board);
        if (!string.IsNullOrWhiteSpace(staffLine))
        {
            sb.AppendLine(staffLine);
        }

        sb.AppendLine(DateTime.Now.ToString("g", Brazil));
        foreach (var noteLine in BuildBoardNoteLines(board.Notes))
        {
            sb.AppendLine(noteLine);
        }

        sb.AppendLine("--------------------------------");
        foreach (var line in lines)
        {
            sb.AppendLine($"{line.Quantity}x {line.Name}");
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine($"OBS: {line.Note}");
            }
        }

        return sb.ToString();
    }

    private string BuildWaiterBillReceipt(TableTile board, int receiptNumber)
    {
        var width = GetReceiptTextWidth();
        var sb = new StringBuilder();
        sb.AppendLine(CenterReceipt(AppReceiptName, width));
        sb.AppendLine(CenterReceipt("CONFERENCIA DA CONTA", width));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(ClipReceipt($"{BoardKindLabel(board)} {board.Number}", width));
        var staffLine = BuildStaffReceiptLine(board);
        if (!string.IsNullOrWhiteSpace(staffLine))
        {
            sb.AppendLine(ClipReceipt(staffLine, width));
        }

        foreach (var noteLine in BuildBoardNoteLines(board.Notes))
        {
            sb.AppendLine(ClipReceipt(noteLine, width));
        }

        sb.AppendLine(DateTime.Now.ToString("g", Brazil));
        sb.AppendLine(new string('-', width));
        foreach (var line in board.Lines)
        {
            sb.AppendLine(ClipReceipt(line.Name, width));
            sb.AppendLine(ReceiptColumns($"{line.Quantity},000 x {Money(line.UnitPrice)}", Money(line.Total), width));
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine(ClipReceipt($"OBS: {line.Note}", width));
            }
        }

        sb.AppendLine(new string('-', width));
        sb.AppendLine(ReceiptColumns("TOTAL", Money(board.Total), width));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(CenterReceipt($"CONTROLE {receiptNumber:000000}", width));
        sb.AppendLine();
        sb.AppendLine(CenterReceipt("NAO E DOCUMENTO FISCAL", width));
        return sb.ToString();
    }

    private string BuildWaiterPaidReceipt(TableTile board, int receiptNumber)
    {
        var width = GetReceiptTextWidth();
        var sb = new StringBuilder();
        sb.AppendLine(CenterReceipt(AppReceiptName, width));
        sb.AppendLine(CenterReceipt("COMPROVANTE PAGO", width));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(ClipReceipt($"{BoardKindLabel(board)} {board.Number}", width));
        var staffLine = BuildStaffReceiptLine(board);
        if (!string.IsNullOrWhiteSpace(staffLine))
        {
            sb.AppendLine(ClipReceipt(staffLine, width));
        }

        if (!string.IsNullOrWhiteSpace(board.CustomerName))
        {
            sb.AppendLine(ClipReceipt($"CLIENTE: {board.CustomerName}", width));
        }

        foreach (var noteLine in BuildBoardNoteLines(board.Notes))
        {
            sb.AppendLine(ClipReceipt(noteLine, width));
        }

        sb.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", Brazil));
        sb.AppendLine(new string('-', width));
        foreach (var line in board.Lines)
        {
            sb.AppendLine(ClipReceipt(line.Name, width));
            sb.AppendLine(ReceiptColumns($"{line.Quantity},000 x {Money(line.UnitPrice)}", Money(line.Total), width));
            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine(ClipReceipt($"OBS: {line.Note}", width));
            }
        }

        sb.AppendLine(new string('-', width));
        var total = board.Lines.Sum(line => line.Total);
        var paid = board.Payments.Sum(payment => payment.Amount);
        var tenderedTotal = board.Payments.Sum(payment => payment.TenderedAmount > 0 ? payment.TenderedAmount : payment.Amount);
        var explicitChange = board.Payments.Sum(payment => payment.ChangeAmount);
        sb.AppendLine(ReceiptColumns("TOTAL", Money(total), width));
        foreach (var payment in board.Payments.GroupBy(item => item.Method).OrderBy(item => item.Key))
        {
            sb.AppendLine(ReceiptColumns(payment.Key.ToUpperInvariant(), Money(payment.Sum(item => item.Amount)), width));
        }

        if (tenderedTotal > paid)
        {
            sb.AppendLine(ReceiptColumns("RECEBIDO", Money(tenderedTotal), width));
        }

        sb.AppendLine(ReceiptColumns("TROCO", Money(Math.Max(explicitChange, paid - total)), width));
        sb.AppendLine(ReceiptColumns("STATUS", "PAGO", width));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(CenterReceipt($"CONTROLE {receiptNumber:000000}", width));
        sb.AppendLine();
        sb.AppendLine(CenterReceipt("NAO E DOCUMENTO FISCAL", width));
        return sb.ToString();
    }

    private static IEnumerable<string> BuildBoardNoteLines(string notes)
    {
        return (notes ?? "")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => $"OBS MESA: {line}");
    }

    private static string NormalizeBoardNumber(string value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? "" : digits.PadLeft(6, '0');
    }

    private static string NormalizeProductCode(string value)
    {
        var typed = (value ?? "").Trim();
        return typed.All(char.IsDigit) ? typed.PadLeft(6, '0') : typed;
    }

    private void ShowIFoodDialog()
    {
        if (!RequirePermission(CanManageIFood, "Integracao iFood"))
        {
            return;
        }

        var settings = _appSettings.IFood ??= new IFoodIntegrationSettings();
        EnsureIFoodCloudSettings(settings);
        var dialog = CreateDialog("iFood Online", 680, 450);
        var connectionBox = new TextBlock
        {
            Text = BuildIFoodStatusText(settings),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#5A6B7C"),
            Margin = new Thickness(0, 4, 0, 0)
        };
        var operationBox = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#52687A"),
            Margin = new Thickness(0, 10, 0, 0),
            Text = settings.HasCloudConnection
                ? "Recebimento automatico ligado. Pedidos e produtos do iFood sincronizam em segundo plano com o PDV."
                : HasIFoodConnectionConfigured(settings)
                    ? "Loja offline no PDV. Use o botao Loja online no topo para voltar a receber pedidos."
                : "Clique em conectar uma vez. Depois disso os pedidos entram no Delivery e os produtos aparecem no estoque."
        };

        void SaveFields()
        {
            settings.Enabled = true;
            _appSettings.PublicMenuStoreOpen = true;
            EnsureIFoodCloudSettings(settings);
            SaveAppSettings();
            SaveStore();
            RefreshOnlineStoreButton();
        }

        void ShowIFoodMessage(string message)
        {
            connectionBox.Text = BuildIFoodStatusText(settings);
            operationBox.Text = message;
            SetStatus(message);
        }

        var connect = DialogButton(settings.HasCloudConnection ? "Reconectar iFood" : "Conectar iFood", "#08A99B");
        connect.MinHeight = 58;
        connect.FontSize = 17;

        connect.Click += async (_, _) =>
        {
            try
            {
                SaveFields();
                connect.IsEnabled = false;
                ShowIFoodMessage("Conectando com o iFood...");
                var response = await _ifoodClient.StartConnectionAsync(settings.BackendUrl, CreateIFoodCloudContext());
                settings.ConnectionId = response.ConnectionId;
                settings.LastUserCode = response.UserCode;
                settings.VerificationUrl = response.VerificationUrl;
                settings.VerificationUrlComplete = response.VerificationUrlComplete;
                settings.MerchantId = response.MerchantId;
                settings.MerchantName = response.MerchantName;
                settings.WebhookUrl = string.IsNullOrWhiteSpace(response.WebhookUrl) ? settings.WebhookUrl : response.WebhookUrl;
                settings.ConnectionStatus = string.IsNullOrWhiteSpace(response.Status)
                    ? string.IsNullOrWhiteSpace(response.MerchantId) ? "aguardando autorizacao" : "conectado"
                    : response.Status;
                SaveAppSettings();
                if (string.Equals(settings.ConnectionStatus, "conectado", StringComparison.OrdinalIgnoreCase))
                {
                    _ifoodSyncTimer.Start();
                    ShowIFoodMessage("iFood conectado. Pedidos entram no Delivery e produtos do iFood aparecem no estoque.");
                    RefreshOnlineStoreButton();
                    _ = AutoImportIFoodOrdersAsync(force: true);
                }
                else if (!string.IsNullOrWhiteSpace(settings.VerificationUrlComplete))
                {
                    Process.Start(new ProcessStartInfo(settings.VerificationUrlComplete) { UseShellExecute = true });
                    ShowIFoodMessage("Finalize a autorizacao no navegador do iFood. O PDV continua aguardando a loja ficar conectada.");
                }
                else
                {
                    ShowIFoodMessage(string.IsNullOrWhiteSpace(response.Message)
                        ? "Vinculo iFood iniciado. Aguarde a autorizacao da loja."
                        : response.Message);
                }
            }
            catch (Exception ex)
            {
                ShowIFoodMessage($"Falha ao conectar iFood: {ex.Message}");
            }
            finally
            {
                connect.IsEnabled = true;
            }
        };

        var header = BorderCard();
        header.Background = Solid("#EAF8F4");
        header.BorderBrush = Solid("#8ACCC2");
        var headerPanel = new StackPanel();
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Pedidos iFood automaticos",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Solid("#071A2C")
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Conecte a loja uma vez. Os novos pedidos aparecem no Delivery sem procurar codigo, webhook ou configuracao tecnica.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#52687A"),
            Margin = new Thickness(0, 8, 0, 0)
        });
        headerPanel.Children.Add(connectionBox);
        headerPanel.Children.Add(operationBox);
        header.Child = headerPanel;

        var flow = BorderCard();
        flow.Margin = new Thickness(0, 14, 0, 0);
        var flowPanel = new StackPanel();
        flowPanel.Children.Add(SectionTitle("Recebimento de pedidos"));
        flowPanel.Children.Add(new TextBlock
        {
            Text = "O Balcao Livre fica escutando o iFood em segundo plano. Pedido novo abre um aviso grande e produto criado no iFood entra no estoque do PDV.",
            Foreground = Solid("#5A6B7C"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        flowPanel.Children.Add(connect);
        var homologation = DialogButton("Checklist homologacao Order", "#0B3A52");
        homologation.MinHeight = 46;
        homologation.HorizontalAlignment = HorizontalAlignment.Stretch;
        homologation.Margin = new Thickness(0, 10, 0, 0);
        homologation.Click += (_, _) => ShowIFoodHomologationDialog();
        flowPanel.Children.Add(homologation);
        flowPanel.Children.Add(new TextBlock
        {
            Text = "Sem botao de buscar: o PDV atualiza sozinho enquanto estiver aberto e conectado a internet.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#5A6B7C"),
            Margin = new Thickness(0, 10, 0, 0)
        });
        flow.Child = flowPanel;

        var outer = DialogPanel();
        outer.Children.Add(header);
        outer.Children.Add(flow);
        dialog.Content = outer;
        dialog.ShowDialog();
    }

    private async Task<int> ImportIFoodOrdersAsync(Action<string>? log = null)
    {
        var settings = _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (!settings.Enabled)
        {
            log?.Invoke("Conecte o iFood para receber pedidos automaticamente.");
            return 0;
        }

        if (!settings.HasCloudConnection)
        {
            log?.Invoke("Finalize o vinculo iFood para receber pedidos automaticamente.");
            return 0;
        }

        try
        {
            var response = await _ifoodClient.SyncOrdersAsync(
                settings.BackendUrl,
                CreateIFoodSyncRequest(settings.ConnectionId));

            settings.LastSyncUtc = response.SyncedAt?.ToUniversalTime() ?? DateTime.UtcNow;
            SaveAppSettings();

            if (response.Orders.Count == 0)
            {
                log?.Invoke(string.IsNullOrWhiteSpace(response.Message)
                    ? "Nenhum pedido novo recebido do iFood."
                    : response.Message);
                return 0;
            }

            var importedCount = 0;
            var updatedCount = 0;
            var importedTiles = new List<TableTile>();
            var platformCancelledTiles = new List<TableTile>();
            foreach (var order in response.Orders.Where(order => !string.IsNullOrWhiteSpace(order.OrderId)))
            {
                var existingOrder = FindIFoodOrder(order.OrderId);
                if (existingOrder is not null)
                {
                    var previousStatus = NormalizeIFoodBoardStatus(existingOrder.Status);
                    if (ApplyIFoodDeliveryUpdate(existingOrder, order))
                    {
                        updatedCount++;
                        var nextStatus = NormalizeIFoodBoardStatus(existingOrder.Status);
                        if (previousStatus != nextStatus && nextStatus is "CANCELAMENTO" or "CANCELADO")
                        {
                            platformCancelledTiles.Add(existingOrder);
                        }
                    }

                    continue;
                }

                importedTiles.Add(CreateIFoodDelivery(order));
                importedCount++;
            }

            if (importedCount > 0 || updatedCount > 0)
            {
                SaveStore();
                if (updatedCount > 0 && importedCount == 0)
                {
                    RefreshBoardForMode();
                }
            }

            log?.Invoke(importedCount == 0
                ? updatedCount == 0
                    ? "Pedidos iFood conferidos automaticamente; nenhum pedido novo importado."
                    : $"{updatedCount} pedido(s) iFood atualizado(s) pelo status do iFood."
                : $"{importedCount} pedido(s) iFood importado(s) para Delivery.");
            if (log is null && importedTiles.Count > 0)
            {
                NotifyIFoodOrdersReceived(importedTiles);
            }
            if (log is null && platformCancelledTiles.Count > 0)
            {
                NotifyIFoodPlatformCancellation(platformCancelledTiles);
            }
            return importedCount;
        }
        catch (Exception ex)
        {
            log?.Invoke($"Falha ao sincronizar iFood: {ex.Message}");
            return 0;
        }
    }

    private async Task<int> SyncIFoodCatalogProductsAsync(bool force = false, Action<string>? log = null)
    {
        var settings = _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (!settings.Enabled || !settings.HasCloudConnection)
        {
            return 0;
        }

        if (!force && DateTime.UtcNow - _lastIFoodCatalogSyncUtc < TimeSpan.FromMinutes(2))
        {
            return 0;
        }

        try
        {
            var response = await _ifoodClient.SyncCatalogAsync(
                settings.BackendUrl,
                CreateIFoodCatalogSyncRequest(settings.ConnectionId));

            _lastIFoodCatalogSyncUtc = DateTime.UtcNow;
            var changed = 0;
            foreach (var item in response.Products.Where(item => !string.IsNullOrWhiteSpace(item.Name)))
            {
                if (UpsertIFoodCatalogProduct(item))
                {
                    changed++;
                }
            }

            if (changed > 0)
            {
                SaveStore();
                ProductsList.Items.Refresh();
                FilterProducts();
            }

            if (changed > 0)
            {
                var message = changed == 1
                    ? "1 produto do iFood entrou/atualizou no estoque."
                    : $"{changed} produtos do iFood entraram/atualizaram no estoque.";
                log?.Invoke(message);
                if (log is null)
                {
                    SetStatus(message);
                }
            }
            else if (log is not null && !string.IsNullOrWhiteSpace(response.Message))
            {
                log.Invoke(response.Message);
            }

            return changed;
        }
        catch (Exception ex)
        {
            _lastIFoodCatalogSyncUtc = DateTime.UtcNow;
            log?.Invoke($"Falha ao sincronizar produtos iFood: {ex.Message}");
            return 0;
        }
    }

    private void ShowIFoodHomologationDialog()
    {
        var dialog = CreateDialog("Homologacao Order iFood", 980, 720);
        var orderIdBoxes = Enumerable.Range(1, 5)
            .Select(_ => new TextBox { MinHeight = 34, FontSize = 14, Padding = new Thickness(8, 5, 8, 5) })
            .ToArray();
        var selectedSummary = new TextBlock
        {
            Text = "Selecione um pedido iFood para copiar o orderId.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var orders = DeliveryTiles
            .Where(IsIFoodDeliveryBoard)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();
        var ordersList = new ListBox
        {
            Height = 210,
            ItemsSource = orders.Select(order => $"{order.ExternalDisplayId}  |  {NormalizeIFoodBoardStatus(order.Status)}  |  {BuildIFoodScenarioEvidence(order)}  |  ID {order.ExternalOrderId}").ToList()
        };

        ordersList.SelectionChanged += (_, _) =>
        {
            if (ordersList.SelectedIndex < 0 || ordersList.SelectedIndex >= orders.Count)
            {
                return;
            }

            var order = orders[ordersList.SelectedIndex];
            selectedSummary.Text = BuildIFoodHomologationOrderSummary(order);
            System.Windows.Clipboard.SetText(order.ExternalOrderId);
            SetStatus($"orderId copiado: {order.ExternalOrderId}");
        };

        Button ScenarioButton(int scenario, string title, string description)
        {
            var button = DialogButton($"Usar selecionado no cenario {scenario}", "#0B3A52");
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Margin = new Thickness(0, 8, 0, 0);
            button.Click += (_, _) =>
            {
                if (ordersList.SelectedIndex < 0 || ordersList.SelectedIndex >= orders.Count)
                {
                    selectedSummary.Text = "Selecione um pedido iFood primeiro.";
                    selectedSummary.Foreground = RedText;
                    return;
                }

                var order = orders[ordersList.SelectedIndex];
                orderIdBoxes[scenario - 1].Text = order.ExternalOrderId;
                selectedSummary.Foreground = Solid("#5B6B7A");
                selectedSummary.Text = $"{title}: orderId preenchido. {BuildIFoodScenarioEvidence(order)}";
            };
            return button;
        }

        Border ScenarioCard(int scenario, string title, string description)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"Cenario {scenario}: {title}",
                FontWeight = FontWeights.Bold,
                Foreground = Solid("#071A2C"),
                FontSize = 15
            });
            stack.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Solid("#5B6B7A"),
                Margin = new Thickness(0, 4, 0, 8)
            });
            stack.Children.Add(DialogField("orderId para informar no chamado", orderIdBoxes[scenario - 1]));
            stack.Children.Add(ScenarioButton(scenario, title, description));
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 10, 10),
                Child = stack
            };
        }

        var left = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
        left.Children.Add(SectionTitle("Pedidos iFood recebidos"));
        left.Children.Add(new TextBlock
        {
            Text = "Ao selecionar um pedido, o orderId e copiado. Use os botoes de cada cenario para montar o texto do chamado.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        left.Children.Add(ordersList);
        left.Children.Add(selectedSummary);

        var copy = DialogButton("Copiar texto do chamado", "#08A99B");
        copy.HorizontalAlignment = HorizontalAlignment.Stretch;
        copy.Margin = new Thickness(0, 12, 0, 0);
        copy.Click += (_, _) =>
        {
            var lines = new[]
            {
                $"Cenario 1 - Pedido agendado com voucher: {orderIdBoxes[0].Text.Trim()}",
                $"Cenario 2 - Pedido manual com cancelamento: {orderIdBoxes[1].Text.Trim()}",
                $"Cenario 3 - Pedido para retirada: {orderIdBoxes[2].Text.Trim()}",
                $"Cenario 4 - Cancelamento pela plataforma de negociacao: {orderIdBoxes[3].Text.Trim()}",
                $"Cenario 5 - Dinheiro com troco/observacao/CPF-CNPJ: {orderIdBoxes[4].Text.Trim()}"
            };
            System.Windows.Clipboard.SetText(string.Join(Environment.NewLine, lines));
            SetStatus("Texto do chamado de homologacao copiado.");
        };
        left.Children.Add(copy);

        var scenarios = new UniformGrid { Columns = 1 };
        scenarios.Children.Add(ScenarioCard(1, "Pedido agendado com voucher", "Criar pedido para o dia seguinte, usar VOUCHER_ENTGRATIS e gravar no PDV a data/hora do agendamento, voucher, itens, cliente e orderId."));
        scenarios.Children.Add(ScenarioCard(2, "Pedido manual com cancelamento", "Criar pedido manual com cartao na entrega, abrir no PDV, cancelar por F9/Acoes iFood e gravar status cancelamento/cancelado com orderId."));
        scenarios.Children.Add(ScenarioCard(3, "Pedido para retirada", "Criar pedido TAKEOUT/retirada no local, confirmar, preparar e marcar pronto mostrando retirada/codigo, cliente, itens, total e orderId."));
        scenarios.Children.Add(ScenarioCard(4, "Cancelamento pela plataforma", "Iniciar cancelamento pela plataforma de negociacao e gravar no PDV a notificacao, motivo, status bloqueado/cancelado e orderId."));
        scenarios.Children.Add(ScenarioCard(5, "Dinheiro com troco", "Criar pedido em dinheiro com valor de troco, observacao e CPF/CNPJ; gravar todos esses dados no detalhe do pedido e o orderId."));

        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.Children.Add(left);
        var scroll = new ScrollViewer
        {
            Content = scenarios,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetColumn(scroll, 1);
        layout.Children.Add(scroll);

        var panel = DialogPanel();
        panel.Children.Add(layout);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private async Task AutoImportIFoodOrdersAsync(bool force = false)
    {
        var settings = _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (!settings.HasCloudConnection || _ifoodSyncRunning)
        {
            return;
        }

        if (!force
            && settings.LastSyncUtc.HasValue
            && DateTime.UtcNow - settings.LastSyncUtc.Value < TimeSpan.FromSeconds(12))
        {
            return;
        }

        _ifoodSyncRunning = true;
        try
        {
            var catalogChanged = await SyncIFoodCatalogProductsAsync(force);
            var imported = await ImportIFoodOrdersAsync();
            if (imported <= 0 && catalogChanged <= 0)
            {
                return;
            }

            RefreshBoardForMode();
            if (string.Equals(CurrentMode, "Delivery", StringComparison.OrdinalIgnoreCase) && BoardTiles.Count > 0)
            {
                SelectTable(BoardTiles.Count - 1, saveCurrent: false);
            }

            var message = imported > 0
                ? imported == 1
                    ? "Novo pedido iFood recebido no Delivery."
                    : $"{imported} novos pedidos iFood recebidos no Delivery."
                : catalogChanged == 1
                    ? "1 produto iFood sincronizado no estoque."
                    : $"{catalogChanged} produtos iFood sincronizados no estoque.";
            StatusText.Text = message;
        }
        catch (Exception ex)
        {
            if (DateTime.Now - _lastIFoodSyncErrorAt > TimeSpan.FromMinutes(3))
            {
                _lastIFoodSyncErrorAt = DateTime.Now;
                SetStatus($"iFood indisponivel agora: {ex.Message}");
            }
        }
        finally
        {
            _ifoodSyncRunning = false;
        }
    }

    private IFoodCloudStoreContext CreateIFoodCloudContext()
    {
        var businessName = string.IsNullOrWhiteSpace(_profile.BusinessName)
            ? AppDisplayName
            : _profile.BusinessName.Trim();

        return new IFoodCloudStoreContext
        {
            LicenseKey = NormalizeActivationKey(_appSettings.ActivationKey),
            MachineHash = GetMachineFingerprint(),
            MachineCode = GetMachineCode(),
            BusinessName = businessName,
            LegalName = _profile.LegalName,
            Cnpj = _profile.Cnpj,
            Phone = _profile.Phone,
            Address = _profile.Address,
            City = _profile.City,
            State = _profile.State,
            AppVersion = GetAppVersion()
        };
    }

    private IFoodCloudFinishRequest CreateIFoodFinishRequest(string connectionId, string authorizationCode)
    {
        var context = CreateIFoodCloudContext();
        return new IFoodCloudFinishRequest
        {
            LicenseKey = context.LicenseKey,
            MachineHash = context.MachineHash,
            MachineCode = context.MachineCode,
            BusinessName = context.BusinessName,
            LegalName = context.LegalName,
            Cnpj = context.Cnpj,
            Phone = context.Phone,
            Address = context.Address,
            City = context.City,
            State = context.State,
            AppVersion = context.AppVersion,
            ConnectionId = connectionId,
            AuthorizationCode = authorizationCode
        };
    }

    private IFoodCloudSyncRequest CreateIFoodSyncRequest(string connectionId)
    {
        var context = CreateIFoodCloudContext();
        return new IFoodCloudSyncRequest
        {
            LicenseKey = context.LicenseKey,
            MachineHash = context.MachineHash,
            MachineCode = context.MachineCode,
            BusinessName = context.BusinessName,
            LegalName = context.LegalName,
            Cnpj = context.Cnpj,
            Phone = context.Phone,
            Address = context.Address,
            City = context.City,
            State = context.State,
            AppVersion = context.AppVersion,
            ConnectionId = connectionId
        };
    }

    private IFoodCloudCatalogSyncRequest CreateIFoodCatalogSyncRequest(string connectionId)
    {
        var context = CreateIFoodCloudContext();
        return new IFoodCloudCatalogSyncRequest
        {
            LicenseKey = context.LicenseKey,
            MachineHash = context.MachineHash,
            MachineCode = context.MachineCode,
            BusinessName = context.BusinessName,
            LegalName = context.LegalName,
            Cnpj = context.Cnpj,
            Phone = context.Phone,
            Address = context.Address,
            City = context.City,
            State = context.State,
            AppVersion = context.AppVersion,
            ConnectionId = connectionId
        };
    }

    private IFoodCloudOrderActionRequest CreateIFoodOrderActionRequest(string connectionId, string orderId, string action, string reason = "", string deliveredBy = "")
    {
        var context = CreateIFoodCloudContext();
        return new IFoodCloudOrderActionRequest
        {
            LicenseKey = context.LicenseKey,
            MachineHash = context.MachineHash,
            MachineCode = context.MachineCode,
            BusinessName = context.BusinessName,
            LegalName = context.LegalName,
            Cnpj = context.Cnpj,
            Phone = context.Phone,
            Address = context.Address,
            City = context.City,
            State = context.State,
            AppVersion = context.AppVersion,
            ConnectionId = connectionId,
            OrderId = orderId,
            Action = action,
            Reason = reason,
            DeliveredBy = string.IsNullOrWhiteSpace(deliveredBy) ? "MERCHANT" : NormalizeIFoodDeliveredBy(deliveredBy)
        };
    }

    private IFoodCloudStockSyncRequest CreateIFoodStockSyncRequest(string connectionId, IFoodStockSyncSnapshot product)
    {
        var context = CreateIFoodCloudContext();
        return new IFoodCloudStockSyncRequest
        {
            LicenseKey = context.LicenseKey,
            MachineHash = context.MachineHash,
            MachineCode = context.MachineCode,
            BusinessName = context.BusinessName,
            LegalName = context.LegalName,
            Cnpj = context.Cnpj,
            Phone = context.Phone,
            Address = context.Address,
            City = context.City,
            State = context.State,
            AppVersion = context.AppVersion,
            ConnectionId = connectionId,
            ProductId = product.ProductId,
            ExternalCode = product.ExternalCode,
            ProductCode = product.Code,
            ProductName = product.Name,
            Amount = product.Amount,
            Reason = product.Reason,
            ImageDataUrl = product.ImageDataUrl,
            ImageUrl = product.ImageUrl
        };
    }

    private readonly record struct IFoodStockSyncSnapshot(
        string Code,
        string Name,
        string ProductId,
        string ExternalCode,
        int Amount,
        string Reason,
        string ImageDataUrl,
        string ImageUrl);

    private sealed record IFoodPresenceSyncSnapshot(
        string BackendUrl,
        IFoodCloudSyncRequest Request);

    private void StartIFoodPresenceLoop()
    {
        if (_ifoodPresenceLoopStarted)
        {
            return;
        }

        _ifoodPresenceLoopStarted = true;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            while (!_exitRequested)
            {
                await SyncIFoodPresenceOnceAsync();
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        });
    }

    private async Task SyncIFoodPresenceOnceAsync()
    {
        try
        {
            var snapshot = CreateIFoodPresenceSyncSnapshot();
            if (snapshot is null)
            {
                return;
            }

            var response = await _ifoodClient.SyncOrdersAsync(snapshot.BackendUrl, snapshot.Request).ConfigureAwait(false);
            var settings = _appSettings.IFood;
            if (settings is not null
                && settings.HasCloudConnection
                && string.Equals(settings.ConnectionId, snapshot.Request.ConnectionId, StringComparison.Ordinal))
            {
                settings.LastSyncUtc = response.SyncedAt?.ToUniversalTime() ?? DateTime.UtcNow;
                SaveAppSettings();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"iFood presence sync skipped: {ex.Message}");
        }
    }

    private IFoodPresenceSyncSnapshot? CreateIFoodPresenceSyncSnapshot()
    {
        var settings = _appSettings.IFood;
        if (settings is null || !settings.HasCloudConnection || _ifoodSyncRunning)
        {
            return null;
        }

        if (settings.LastSyncUtc.HasValue
            && DateTime.UtcNow - settings.LastSyncUtc.Value < TimeSpan.FromSeconds(25))
        {
            return null;
        }

        var backendUrl = settings.BackendUrl;
        var connectionId = settings.ConnectionId;
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return null;
        }

        return new IFoodPresenceSyncSnapshot(backendUrl, CreateIFoodSyncRequest(connectionId));
    }

    private void QueueIFoodStockSync(ProductTile? product, string reason)
    {
        if (product is null)
        {
            return;
        }

        var settings = _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (!HasIFoodConnectionConfigured(settings))
        {
            return;
        }

        var productId = (product.IFoodProductId ?? "").Trim();
        var externalCode = (product.IFoodExternalCode ?? "").Trim();
        if (!HasIFoodCatalogLink(product))
        {
            if (string.Equals(product.Category, "IFOOD", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus($"Produto iFood sem vinculo no catalogo: {product.Name}. Sincronize/importe o item para atualizar estoque no iFood.");
            }

            return;
        }

        var amount = Math.Max(0, (int)Math.Floor(product.StockQuantity));
        var imagePayload = BuildIFoodProductImagePayload(product);
        var snapshot = new IFoodStockSyncSnapshot(
            product.Code,
            product.Name,
            productId,
            externalCode,
            amount,
            reason,
            imagePayload.DataUrl,
            imagePayload.Url);
        _ = SyncIFoodStockSnapshotAsync(settings.BackendUrl, settings.ConnectionId, snapshot);
    }

    private static IFoodProductImageSyncPayload BuildIFoodProductImagePayload(ProductTile product)
    {
        var image = BuildPublicMenuProductImageUrl(product).Trim();
        if (string.IsNullOrWhiteSpace(image))
        {
            return new IFoodProductImageSyncPayload("", "");
        }

        return image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            ? new IFoodProductImageSyncPayload(image, "")
            : new IFoodProductImageSyncPayload("", image);
    }

    private readonly record struct IFoodProductImageSyncPayload(string DataUrl, string Url);

    private static bool HasIFoodCatalogLink(ProductTile product)
    {
        return !string.IsNullOrWhiteSpace(product.IFoodProductId)
               || !string.IsNullOrWhiteSpace(product.IFoodExternalCode);
    }

    private async Task SyncIFoodStockSnapshotAsync(string backendUrl, string connectionId, IFoodStockSyncSnapshot product)
    {
        try
        {
            var response = await _ifoodClient.SyncStockAsync(
                backendUrl,
                CreateIFoodStockSyncRequest(connectionId, product));
            RunOnUiThread(() =>
            {
                var message = string.IsNullOrWhiteSpace(response.Message)
                    ? $"Estoque iFood atualizado: {product.Name} = {product.Amount:N0}."
                    : $"{response.Message} {product.Name}.";
                if ((!string.IsNullOrWhiteSpace(product.ImageDataUrl) || !string.IsNullOrWhiteSpace(product.ImageUrl))
                    && response.ImageUpdated
                    && !message.Contains("foto", StringComparison.OrdinalIgnoreCase))
                {
                    message = $"{message.TrimEnd()} Foto enviada.";
                }

                if (!string.IsNullOrWhiteSpace(response.ImageWarning)
                    && !message.Contains(response.ImageWarning, StringComparison.OrdinalIgnoreCase))
                {
                    message = $"{message.TrimEnd()} Foto pendente: {response.ImageWarning}";
                }

                SetStatus(message.Trim());
            });
        }
        catch (Exception ex)
        {
            RunOnUiThread(() => SetStatus($"Falha ao atualizar estoque no iFood ({product.Name}): {ex.Message}"));
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.BeginInvoke(action);
        }
    }

    private bool HasIFoodOrder(string orderId)
    {
        return FindIFoodOrder(orderId) is not null;
    }

    private TableTile? FindIFoodOrder(string orderId)
    {
        return DeliveryTiles.FirstOrDefault(tile =>
            string.Equals(tile.ExternalSource, "IFOOD", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(tile.ExternalOrderId, orderId, StringComparison.OrdinalIgnoreCase));
    }

    private bool ApplyIFoodDeliveryUpdate(TableTile tile, IFoodImportedOrder order)
    {
        var changed = false;

        bool SetString(string current, string value, Action<string> assign, bool requireValue = true)
        {
            var normalized = value?.Trim() ?? "";
            if (requireValue && string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (string.Equals(current, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            assign(normalized);
            return true;
        }

        bool SetDate(DateTime? current, DateTime? value, Action<DateTime?> assign)
        {
            if (!value.HasValue)
            {
                return false;
            }

            if (current.HasValue && Math.Abs((current.Value - value.Value).TotalSeconds) < 1)
            {
                return false;
            }

            assign(value);
            return true;
        }

        var incomingStatus = StatusFromIFoodOrder(order);
        if (ShouldCorrectFutureDeliveredIFoodStatus(tile, incomingStatus))
        {
            tile.Status = incomingStatus;
            tile.ExternalDeliveredAt = null;
            changed = true;
        }
        else if (ShouldApplyIFoodStatus(tile.Status, incomingStatus))
        {
            tile.Status = incomingStatus;
            changed = true;
        }

        changed |= SetString(tile.ExternalDisplayId, order.DisplayId, value => tile.ExternalDisplayId = value);
        changed |= SetString(tile.ExternalDeliveredBy, NormalizeIFoodDeliveredBy(order.DeliveredBy), value => tile.ExternalDeliveredBy = value, requireValue: false);
        changed |= SetString(tile.ExternalPickupCode, order.PickupCode, value => tile.ExternalPickupCode = value, requireValue: false);
        changed |= SetString(tile.ExternalDeliveryLocalizer, order.DeliveryLocalizer, value => tile.ExternalDeliveryLocalizer = value, requireValue: false);
        changed |= SetString(tile.ExternalShipmentInfo, order.ShipmentInfo, value => tile.ExternalShipmentInfo = value, requireValue: false);
        changed |= SetString(tile.ExternalOrderTiming, order.OrderTiming, value => tile.ExternalOrderTiming = value, requireValue: false);
        changed |= SetString(tile.ExternalOrderType, order.OrderType, value => tile.ExternalOrderType = value, requireValue: false);
        changed |= SetString(tile.ExternalPaymentMethod, order.PaymentMethod, value => tile.ExternalPaymentMethod = value, requireValue: false);
        changed |= SetString(tile.ExternalPaymentSummary, order.PaymentSummary, value => tile.ExternalPaymentSummary = value, requireValue: false);
        changed |= SetString(tile.ExternalVoucherSummary, order.VoucherSummary, value => tile.ExternalVoucherSummary = value, requireValue: false);
        changed |= SetString(tile.ExternalCancellationInfo, order.CancellationInfo, value => tile.ExternalCancellationInfo = value, requireValue: false);
        changed |= SetString(tile.CustomerName, string.IsNullOrWhiteSpace(order.CustomerName) ? "" : order.CustomerName.Trim().ToUpperInvariant(), value => tile.CustomerName = value, requireValue: false);
        changed |= SetString(tile.CustomerCpf, order.CustomerDocument, value => tile.CustomerCpf = value, requireValue: false);
        changed |= SetString(tile.Phone, order.Phone, value => tile.Phone = value, requireValue: false);
        if (order.ChangeFor > 0m && tile.ExternalChangeFor != order.ChangeFor)
        {
            tile.ExternalChangeFor = order.ChangeFor;
            changed = true;
        }
        changed |= SetDate(tile.ExternalCreatedAt, LocalTimeOrNull(order.CreatedAt), value => tile.ExternalCreatedAt = value);
        changed |= SetDate(tile.ExternalPreparationStartAt, LocalTimeOrNull(order.PreparationStartDateTime), value => tile.ExternalPreparationStartAt = value);
        changed |= SetDate(tile.ExternalConfirmationDeadlineAt, LocalTimeOrNull(order.ConfirmationDeadlineAt), value => tile.ExternalConfirmationDeadlineAt = value);
        changed |= SetDate(tile.ExternalDeliveryExpectedAt, LocalTimeOrNull(order.DeliveryExpectedAt), value => tile.ExternalDeliveryExpectedAt = value);
        changed |= SetDate(tile.ExternalDeliveredAt, LocalTimeOrNull(order.DeliveredAt), value => tile.ExternalDeliveredAt = value);
        changed |= SetDate(tile.ExternalCollectedAt, LocalTimeOrNull(order.CollectedAt), value => tile.ExternalCollectedAt = value);
        if (!order.DeliveredAt.HasValue
            && tile.ExternalDeliveredAt.HasValue
            && tile.ExternalDeliveredAt.Value > DateTime.Now.AddMinutes(1)
            && !IsFinalIFoodStatus(tile.Status))
        {
            tile.ExternalDeliveredAt = null;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(order.Address) && !string.Equals(tile.Address, order.Address, StringComparison.Ordinal))
        {
            tile.Address = order.Address;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(order.District) && !string.Equals(tile.District, order.District, StringComparison.Ordinal))
        {
            tile.District = order.District;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(order.Notes) &&
            !tile.Notes.Contains(order.Notes, StringComparison.OrdinalIgnoreCase))
        {
            tile.Notes = string.IsNullOrWhiteSpace(tile.Notes)
                ? order.Notes.Trim()
                : $"{tile.Notes.Trim()}\n{order.Notes.Trim()}";
            changed = true;
        }

        UpdateIFoodDynamicDetail(tile);
        return changed;
    }

    private TableTile CreateIFoodDelivery(IFoodImportedOrder order)
    {
        var display = string.IsNullOrWhiteSpace(order.DisplayId) ? (DeliveryTiles.Count + 1).ToString("00000", Brazil) : order.DisplayId;
        var stockWarnings = new List<string>();
        var tile = new TableTile
        {
            Number = $"I{display}".Length <= 8 ? $"I{display}" : $"I{DeliveryTiles.Count + 1:00000}",
            Kind = "DELIVERY",
            Status = StatusFromIFoodOrder(order),
            CreatedAt = LocalTimeOrNow(order.CreatedAt),
            CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "CLIENTE IFOOD" : order.CustomerName.Trim().ToUpperInvariant(),
            CustomerCpf = order.CustomerDocument,
            Phone = order.Phone,
            Address = order.Address,
            District = order.District,
            Detail = $"IFOOD {order.OrderType} {IFoodShipmentLabel(order.DeliveredBy)}".Trim(),
            Notes = $"iFood {display} / {order.OrderId}\n{order.ShipmentInfo}\n{order.Notes}".Trim(),
            ExternalSource = "IFOOD",
            ExternalOrderId = order.OrderId,
            ExternalDisplayId = display,
            ExternalDeliveredBy = NormalizeIFoodDeliveredBy(order.DeliveredBy),
            ExternalPickupCode = order.PickupCode,
            ExternalDeliveryLocalizer = order.DeliveryLocalizer,
            ExternalShipmentInfo = order.ShipmentInfo,
            ExternalOrderTiming = order.OrderTiming,
            ExternalOrderType = order.OrderType,
            ExternalPaymentMethod = order.PaymentMethod,
            ExternalPaymentSummary = order.PaymentSummary,
            ExternalChangeFor = order.ChangeFor,
            ExternalVoucherSummary = order.VoucherSummary,
            ExternalCancellationInfo = order.CancellationInfo,
            ExternalCreatedAt = LocalTimeOrNull(order.CreatedAt),
            ExternalPreparationStartAt = LocalTimeOrNull(order.PreparationStartDateTime),
            ExternalConfirmationDeadlineAt = LocalTimeOrNull(order.ConfirmationDeadlineAt),
            ExternalDeliveryExpectedAt = LocalTimeOrNull(order.DeliveryExpectedAt),
            ExternalDeliveredAt = LocalTimeOrNull(order.DeliveredAt),
            ExternalCollectedAt = LocalTimeOrNull(order.CollectedAt)
        };
        UpdateIFoodDynamicDetail(tile);

        foreach (var item in order.Items)
        {
            var product = ResolveOrCreateIFoodProduct(item, display, stockWarnings);
            var itemCode = product?.Code ?? (string.IsNullOrWhiteSpace(item.Code) ? "IFOOD" : item.Code);
            var itemName = product?.Name ?? item.Name;
            var itemSector = product is null ? "COZINHA" : NormalizeProductDestination(product.Sector, "CAIXA");
            var line = tile.Lines.FirstOrDefault(line =>
                string.Equals(line.Code, itemCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(line.Name, itemName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(line.Note, item.Notes, StringComparison.OrdinalIgnoreCase));
            if (line is null)
            {
                tile.Lines.Add(new TicketLine
                {
                    Code = itemCode,
                    Name = itemName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Note = item.Notes,
                    Sector = itemSector
                });
            }
            else
            {
                line.Quantity += item.Quantity;
            }

            if (product is not null)
            {
                ApplyIFoodStockMovement(product, item.Quantity, display);
                QueueIFoodStockSync(product, $"Pedido iFood {display}");
            }
        }

        if (stockWarnings.Count > 0)
        {
            tile.Notes = $"{tile.Notes}\n{string.Join("\n", stockWarnings)}".Trim();
        }
        tile.ExternalStockApplied = true;

        tile.Total = tile.Lines.Sum(line => line.Total);
        if (order.Total > 0m && Math.Abs(order.Total - tile.Total) >= 0.01m)
        {
            tile.Lines.Add(new TicketLine
            {
                Code = "IFOOD-TOTAL",
                Name = "AJUSTE TOTAL IFOOD",
                Quantity = 1,
                UnitPrice = order.Total - tile.Total,
                Sector = "CAIXA"
            });
            tile.Total = order.Total;
        }

        DeliveryTiles.Add(tile);
        ScheduleKitchenPrint(tile, tile.Lines);
        UpsertCustomerRecord(tile.CustomerCpf, tile.CustomerName, tile.Phone, tile.Address, tile.District, tile.Notes);
        if (_appSettings.AutoPrintDelivery && !IsIFoodDeliveryBoard(tile))
        {
            _ = TryPrintTextToDefaultPrinter(BuildDeliveryPrintText(tile, tile.District, _appSettings.PrintLayout), $"Delivery {tile.Number}", _appSettings.PrintLayout == "PEQUENO");
        }

        return tile;
    }

    private bool UpsertIFoodCatalogProduct(IFoodCatalogProduct item)
    {
        var productId = (item.ProductId ?? "").Trim();
        var itemId = (item.ItemId ?? "").Trim();
        var ifoodProductId = string.IsNullOrWhiteSpace(productId) ? itemId : productId;
        var externalCode = (item.ExternalCode ?? "").Trim();
        var normalizedExternalCode = string.IsNullOrWhiteSpace(externalCode) ? "" : NormalizeProductCode(externalCode);
        var normalizedName = NormalizeProductLookupText(item.Name);
        var category = NormalizeIFoodCatalogCategory(item.Category);

        var product = Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(ifoodProductId)
                && string.Equals(product.IFoodProductId, ifoodProductId, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(productId)
                && string.Equals(product.IFoodProductId, productId, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(itemId)
                && string.Equals(product.IFoodProductId, itemId, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(externalCode)
                && string.Equals(product.IFoodExternalCode, externalCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedExternalCode)
                && string.Equals(product.IFoodExternalCode, normalizedExternalCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedExternalCode)
                && string.Equals(product.Code, normalizedExternalCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(externalCode)
                && string.Equals(product.Code, externalCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedName)
                && string.Equals(NormalizeProductLookupText(product.Name), normalizedName, StringComparison.Ordinal));

        if (product is null)
        {
            var code = !string.IsNullOrWhiteSpace(normalizedExternalCode)
                       && normalizedExternalCode.All(char.IsDigit)
                       && normalizedExternalCode.Length <= 6
                ? normalizedExternalCode.PadLeft(6, '0')
                : NextProductCode();
            while (Products.Any(product => string.Equals(product.Code, code, StringComparison.OrdinalIgnoreCase)))
            {
                code = NextProductCode();
            }

            product = new ProductTile
            {
                Code = code,
                Name = string.IsNullOrWhiteSpace(item.Name) ? "ITEM IFOOD" : item.Name.Trim().ToUpperInvariant(),
                Category = category,
                Price = item.Price,
                Sector = "COZINHA",
                Active = item.IsAvailable != false,
                IFoodProductId = ifoodProductId,
                IFoodExternalCode = externalCode,
                StockQuantity = item.StockQuantity ?? DefaultIFoodImportedStockQuantity(item),
                MinimumStock = 0
            };

            Products.Add(product);
            EnsureProductCategory(category);
            if (product.StockQuantity != 0m)
            {
                product.StockHistory.Add(new StockMovement
                {
                    ProductCode = product.Code,
                    Type = "IFOOD",
                    Quantity = product.StockQuantity,
                    Reason = "Sincronizacao catalogo iFood",
                    When = DateTime.Now
                });
            }

            return true;
        }

        var changed = false;
        changed |= SetIfDifferent(product.IFoodProductId, ifoodProductId, value => product.IFoodProductId = value);
        changed |= SetIfDifferent(product.IFoodExternalCode, externalCode, value => product.IFoodExternalCode = value);

        if (!string.IsNullOrWhiteSpace(item.Name)
            && (string.IsNullOrWhiteSpace(product.Name)
                || string.Equals(product.Name, "ITEM IFOOD", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product.Category, "IFOOD", StringComparison.OrdinalIgnoreCase)))
        {
            changed |= SetIfDifferent(product.Name, item.Name.Trim().ToUpperInvariant(), value => product.Name = value);
        }

        if (!string.IsNullOrWhiteSpace(category)
            && (string.IsNullOrWhiteSpace(product.Category)
                || string.Equals(product.Category, "IFOOD", StringComparison.OrdinalIgnoreCase)))
        {
            changed |= SetIfDifferent(product.Category, category, value => product.Category = value);
        }

        if (item.Price > 0m && product.Price != item.Price)
        {
            product.Price = item.Price;
            changed = true;
        }

        var active = item.IsAvailable != false;
        if (product.Active != active)
        {
            product.Active = active;
            changed = true;
        }

        if (item.StockQuantity.HasValue && product.StockQuantity != item.StockQuantity.Value)
        {
            var previous = product.StockQuantity;
            product.StockQuantity = Math.Max(0m, item.StockQuantity.Value);
            product.StockHistory.Add(new StockMovement
            {
                ProductCode = product.Code,
                Type = "IFOOD",
                Quantity = product.StockQuantity - previous,
                Reason = "Sincronizacao catalogo iFood",
                When = DateTime.Now
            });
            changed = true;
        }
        else if (!item.StockQuantity.HasValue
                 && product.StockQuantity <= 0m
                 && product.SoldQuantity <= 0m
                 && HasIFoodCatalogLink(product)
                 && item.IsAvailable != false)
        {
            var defaultQuantity = DefaultIFoodImportedStockQuantity(item);
            product.StockQuantity = defaultQuantity;
            product.StockHistory.Add(new StockMovement
            {
                ProductCode = product.Code,
                Type = "IFOOD",
                Quantity = defaultQuantity,
                Reason = "Estoque inicial catalogo iFood",
                When = DateTime.Now
            });
            changed = true;
        }

        EnsureProductCategory(product.Category);
        return changed;
    }

    private static bool SetIfDifferent(string current, string next, Action<string> assign)
    {
        var normalized = (next ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || string.Equals(current ?? "", normalized, StringComparison.Ordinal))
        {
            return false;
        }

        assign(normalized);
        return true;
    }

    private static string NormalizeIFoodCatalogCategory(string value)
    {
        var category = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(category) ? "IFOOD" : category.ToUpperInvariant();
    }

    private static decimal DefaultIFoodImportedStockQuantity(IFoodCatalogProduct item)
    {
        return item.IsAvailable == false ? 0m : 10m;
    }

    private void EnsureProductCategory(string category)
    {
        var normalized = NormalizeIFoodCatalogCategory(category);
        if (Categories.All(item => !string.Equals(item.Name, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            Categories.Add(new CategoryTile(normalized));
        }
    }

    private ProductTile ResolveOrCreateIFoodProduct(IFoodImportedItem item, string display, ICollection<string> stockWarnings, bool announce = true)
    {
        var rawCode = (item.Code ?? "").Trim();
        var normalizedCode = string.IsNullOrWhiteSpace(rawCode) ? "" : NormalizeProductCode(rawCode);
        var productId = (item.ProductId ?? "").Trim();
        var normalizedName = NormalizeProductLookupText(item.Name);

        var product = Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(productId)
                && string.Equals(product.IFoodProductId, productId, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(rawCode)
                && string.Equals(product.IFoodExternalCode, rawCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedCode)
                && string.Equals(product.Code, normalizedCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(rawCode)
                && string.Equals(product.Code, rawCode, StringComparison.OrdinalIgnoreCase))
            ?? Products.FirstOrDefault(product =>
                !string.IsNullOrWhiteSpace(normalizedName)
                && string.Equals(NormalizeProductLookupText(product.Name), normalizedName, StringComparison.Ordinal));

        if (product is not null)
        {
            if (!string.IsNullOrWhiteSpace(productId))
            {
                product.IFoodProductId = productId;
            }

            if (!string.IsNullOrWhiteSpace(rawCode))
            {
                product.IFoodExternalCode = rawCode;
            }

            return product;
        }

        var code = !string.IsNullOrWhiteSpace(normalizedCode) && normalizedCode.All(char.IsDigit) && normalizedCode.Length <= 6
            ? normalizedCode.PadLeft(6, '0')
            : NextProductCode();
        while (Products.Any(product => string.Equals(product.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            code = NextProductCode();
        }

        product = new ProductTile
        {
            Code = code,
            Name = string.IsNullOrWhiteSpace(item.Name) ? "ITEM IFOOD" : item.Name.Trim().ToUpperInvariant(),
            Category = "IFOOD",
            Price = item.UnitPrice,
            Sector = "BALCAO",
            Active = true,
            IFoodProductId = productId,
            IFoodExternalCode = rawCode,
            StockQuantity = 0,
            MinimumStock = 0
        };
        Products.Add(product);
        if (Categories.All(category => !string.Equals(category.Name, "IFOOD", StringComparison.OrdinalIgnoreCase)))
        {
            Categories.Add(new CategoryTile("IFOOD"));
        }

        stockWarnings.Add($"ESTOQUE IFOOD: produto criado automaticamente ({product.Code}) para revisar codigo/vinculo do iFood.");
        if (announce)
        {
            SetStatus($"Produto iFood criado no estoque: {product.Name} ({product.Code}).");
        }
        return product;
    }

    private static void ApplyIFoodStockMovement(ProductTile product, int quantity, string display)
    {
        var amount = Math.Max(1, quantity);
        product.StockQuantity -= amount;
        product.SoldQuantity += amount;
        product.StockHistory.Add(new StockMovement
        {
            ProductCode = product.Code,
            Type = "IFOOD",
            Quantity = -amount,
            Reason = $"Pedido iFood {display}",
            When = DateTime.Now
        });
    }

    private static string NormalizeIFoodDeliveredBy(string value)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant();
        return normalized is "IFOOD" or "MERCHANT" ? normalized : "";
    }

    private static string IFoodShipmentLabel(string deliveredBy)
    {
        return NormalizeIFoodDeliveredBy(deliveredBy) switch
        {
            "IFOOD" => "ENTREGA IFOOD",
            "MERCHANT" => "ENTREGA LOJA",
            _ => ""
        };
    }

    private static bool IsIFoodShipment(string deliveredBy)
    {
        return NormalizeIFoodDeliveredBy(deliveredBy) == "IFOOD";
    }

    private static bool IsMerchantShipment(string deliveredBy)
    {
        return NormalizeIFoodDeliveredBy(deliveredBy) == "MERCHANT";
    }

    private static string StatusFromIFoodImportedStatus(string status)
    {
        var normalized = (status ?? "").Trim().ToUpperInvariant().Replace("-", "_", StringComparison.Ordinal);
        return normalized switch
        {
            "CONFIRMED" or "CONFIRMADO" or "ACCEPTED" or "ACEITO" => "PREPARANDO",
            "PREPARATION_STARTED" or "START_PREPARATION" or "PREPARING" or "PREPARO" or "PREPARANDO" => "PREPARANDO",
            "READY_TO_PICKUP" or "READY" or "PRONTO" => "PRONTO",
            "DISPATCHED" or "IN_DELIVERY" or "ON_THE_WAY" or "ROUTE" or "ROTA" or "DESPACHADO" => "DESPACHADO",
            "CANCELLATION_REQUESTED" or "CANCEL_REQUESTED" or "CANCELAMENTO" => "CANCELAMENTO",
            "CANCELLED" or "CANCELED" or "CANCELADO" => "CANCELADO",
            "CONCLUDED" or "DELIVERED" or "ENTREGUE" or "FINALIZADO" => "ENTREGUE",
            "PLACED" or "CREATED" or "NOVO" or "" => "NOVO",
            _ => normalized
        };
    }

    private static string StatusFromIFoodOrder(IFoodImportedOrder order)
    {
        if (order.DeliveredAt.HasValue && order.DeliveredAt.Value <= DateTime.UtcNow.AddMinutes(1))
        {
            return "ENTREGUE";
        }

        return StatusFromIFoodImportedStatus(order.Status);
    }

    private static bool ShouldCorrectFutureDeliveredIFoodStatus(TableTile tile, string incomingStatus)
    {
        var current = NormalizeIFoodBoardStatus(tile.Status);
        var incoming = NormalizeIFoodBoardStatus(incomingStatus);
        return current == "ENTREGUE"
            && incoming is "NOVO" or "PLACED" or "CREATED"
            && tile.ExternalDeliveredAt.HasValue
            && tile.ExternalDeliveredAt.Value > DateTime.Now.AddMinutes(1);
    }

    private static bool IsFinalIFoodStatus(string status)
    {
        return NormalizeIFoodBoardStatus(status) is "ENTREGUE" or "FINALIZADO" or "CANCELAMENTO" or "CANCELADO";
    }

    private static bool ShouldApplyIFoodStatus(string currentStatus, string incomingStatus)
    {
        var current = NormalizeIFoodBoardStatus(currentStatus);
        var incoming = NormalizeIFoodBoardStatus(incomingStatus);
        if (string.IsNullOrWhiteSpace(incoming))
        {
            return false;
        }

        if (current == incoming)
        {
            return false;
        }

        if (incoming is "CANCELAMENTO" or "CANCELADO" or "ENTREGUE")
        {
            return true;
        }

        if (current is "CANCELAMENTO" or "CANCELADO" or "ENTREGUE")
        {
            return false;
        }

        return IFoodStatusRank(incoming) >= IFoodStatusRank(current);
    }

    private static int IFoodStatusRank(string status)
    {
        return NormalizeIFoodBoardStatus(status) switch
        {
            "NOVO" or "PLACED" or "CREATED" => 0,
            "PREPARO" or "PREPARANDO" or "CONFIRMADO" or "ACEITO" => 1,
            "PRONTO" => 3,
            "DESPACHADO" => 4,
            "ENTREGUE" => 5,
            "CANCELAMENTO" => 6,
            "CANCELADO" => 7,
            _ => 0
        };
    }

    private static string NormalizeIFoodBoardStatus(string status)
    {
        return StatusFromIFoodImportedStatus(status);
    }

    private static bool IsIFoodOrder(TableTile? board)
    {
        return board is not null
            && string.Equals(board.ExternalSource, "IFOOD", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(board.ExternalOrderId);
    }

    private static bool IsIFoodDeliveryBoard(TableTile? board)
    {
        return board is not null
            && string.Equals(board.Kind, "DELIVERY", StringComparison.OrdinalIgnoreCase)
            && string.Equals(board.ExternalSource, "IFOOD", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentIFoodDeliveryLocked()
    {
        return IsIFoodDeliveryBoard(CurrentBoard);
    }

    private bool BlockIFoodDeliveryEdit(string action)
    {
        if (!IsCurrentIFoodDeliveryLocked())
        {
            return false;
        }

        SetStatus($"Pedido iFood nao permite {action}. Use F9 ou Acoes iFood para confirmar/despachar.");
        return true;
    }

    private void OpenIFoodActionsForCurrentOrder()
    {
        var board = CurrentBoard;
        if (board is not null && IsIFoodOrder(board))
        {
            ShowIFoodOrderActionDialog(board, isNewOrder: false);
            return;
        }

        SetStatus("Pedido iFood sem ID externo. Nao da para enviar acao ao iFood.");
    }

    private void NotifyIFoodOrdersReceived(IReadOnlyList<TableTile> orders)
    {
        if (orders.Count == 0)
        {
            return;
        }

        var latest = orders[^1];
        var message = orders.Count == 1
            ? $"{latest.Number} - {latest.CustomerName} - {Money(latest.Total)}"
            : $"{orders.Count} pedidos iFood novos. Ultimo: {latest.Number} - {Money(latest.Total)}";
        _suppressNextToastSound = true;
        ShowToast("Novo pedido iFood", message, "IF", "#A11D1D", "#FFE2DF");
        PlayIFoodOrderSound();
        VibrateInApp();
        Dispatcher.BeginInvoke(() => ShowIFoodOrderActionDialog(latest, isNewOrder: true), DispatcherPriority.Background);
    }

    private void NotifyIFoodPlatformCancellation(IReadOnlyList<TableTile> orders)
    {
        if (orders.Count == 0)
        {
            return;
        }

        var latest = orders[^1];
        var message = orders.Count == 1
            ? $"{latest.Number} - orderId {latest.ExternalOrderId}"
            : $"{orders.Count} pedidos iFood cancelados pela plataforma. Ultimo: {latest.ExternalOrderId}";
        ShowToast("Cancelamento iFood", message, "CX", "#A11D1D", "#FFE2DF");
        SetStatus($"Cancelamento recebido do iFood: {latest.ExternalDisplayId} / {latest.ExternalOrderId}.");
    }

    private void ShowIFoodOrderActionDialog(TableTile order, bool isNewOrder)
    {
        if (!IsIFoodOrder(order))
        {
            SetStatus("Pedido iFood invalido para despacho.");
            return;
        }

        var dialog = CreateDialog(isNewOrder ? "Novo pedido iFood" : "Despacho iFood", 820, 720);
        dialog.ResizeMode = ResizeMode.NoResize;
        dialog.Topmost = isNewOrder;
        dialog.Loaded += (_, _) =>
        {
            dialog.Activate();
            dialog.Topmost = false;
        };
        dialog.Closed += (_, _) => StopIFoodOrderSound();

        var statusText = new TextBlock
        {
            Text = BuildIFoodOrderStatusText(order),
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var deadlineText = new TextBlock
        {
            Text = BuildIFoodConfirmationDeadlineText(order),
            Foreground = IsIFoodConfirmationExpired(order) ? RedText : Solid("#99620D"),
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var actionMessage = new TextBlock
        {
            Text = BuildIFoodActionHint(order),
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var cancelReasonBox = new ComboBox
        {
            ItemsSource = new[]
            {
                "501 - Loja sem produto",
                "502 - Loja sem capacidade",
                "503 - Cliente solicitou cancelamento",
                "504 - Endereco fora da area"
            },
            SelectedIndex = 0,
            Margin = new Thickness(0, 6, 0, 0)
        };

        async Task RunActionAsync(string action, Button button)
        {
            try
            {
                if (!CanRunIFoodAction(order, action, out var blockedReason))
                {
                    actionMessage.Text = blockedReason;
                    actionMessage.Foreground = RedText;
                    return;
                }

                button.IsEnabled = false;
                StopIFoodOrderSound();
                actionMessage.Text = "Enviando para o iFood...";
                actionMessage.Foreground = Solid("#5B6B7A");
                var reason = action == "cancel"
                    ? cancelReasonBox.SelectedItem?.ToString() ?? "501 - Loja sem produto"
                    : "";
                var result = await SendIFoodOrderActionAsync(order, action, reason);
                if (result.Success)
                {
                    statusText.Text = BuildIFoodOrderStatusText(order);
                    deadlineText.Text = BuildIFoodConfirmationDeadlineText(order);
                    deadlineText.Foreground = IsIFoodConfirmationExpired(order) ? RedText : Solid("#99620D");
                    actionMessage.Text = string.IsNullOrWhiteSpace(result.Message)
                        ? $"Atualizado: {order.Status}."
                        : result.Message;
                    actionMessage.Foreground = Solid("#08A99B");
                    dialog.Close();
                }
            }
            catch (Exception ex)
            {
                actionMessage.Text = $"Falha no iFood: {ex.Message}";
                actionMessage.Foreground = RedText;
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        var confirm = DialogButton("Confirmar pedido", "#08A99B");
        var prepare = DialogButton("Preparar pedido", "#0B3A52");
        var ready = DialogButton("Marcar pronto", "#99620D");
        var dispatch = DialogButton("Despachar pedido", "#08A99B");
        var cancel = DialogButton("Cancelar no iFood", "#A11D1D");
        var openDelivery = DialogButton("Abrir Delivery", "#0B3A52");

        void RefreshActionState()
        {
            statusText.Text = BuildIFoodOrderStatusText(order);
            deadlineText.Text = BuildIFoodConfirmationDeadlineText(order);
            deadlineText.Foreground = IsIFoodConfirmationExpired(order) ? RedText : Solid("#99620D");
            ApplyIFoodActionButtonState(order, confirm, "confirm");
            ApplyIFoodActionButtonState(order, prepare, "prepare");
            ApplyIFoodActionButtonState(order, ready, "ready");
            ApplyIFoodActionButtonState(order, dispatch, "dispatch");
            ApplyIFoodActionButtonState(order, cancel, "cancel");
            cancelReasonBox.IsEnabled = CanRunIFoodAction(order, "cancel", out _);
            dispatch.Content = IsIFoodTakeout(order)
                ? "Retirada concluida"
                : IsIFoodShipment(order.ExternalDeliveredBy) ? "Entrega iFood" : "Despachar pedido";
        }

        RefreshActionState();
        var deadlineTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        deadlineTimer.Tick += (_, _) => RefreshActionState();
        deadlineTimer.Start();
        dialog.Closed += (_, _) => deadlineTimer.Stop();

        confirm.Click += async (_, _) => await RunActionAsync("confirm", confirm);
        prepare.Click += async (_, _) => await RunActionAsync("prepare", prepare);
        ready.Click += async (_, _) => await RunActionAsync("ready", ready);
        dispatch.Click += async (_, _) => await RunActionAsync("dispatch", dispatch);
        cancel.Click += async (_, _) => await RunActionAsync("cancel", cancel);
        openDelivery.Click += (_, _) =>
        {
            StopIFoodOrderSound();
            ModeList.SelectedItem = "Delivery";
            RefreshBoardForMode();
            var index = BoardTiles.IndexOf(order);
            if (index >= 0)
            {
                SelectTable(index, saveCurrent: false);
            }
            dialog.Close();
        };

        foreach (var button in new[] { confirm, prepare, ready, dispatch, cancel, openDelivery })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
            button.MinHeight = 46;
            button.Margin = new Thickness(0, 0, 10, 10);
        }

        var header = new Border
        {
            Background = Solid(isNewOrder ? "#FFE2DF" : "#EAF8F4"),
            BorderBrush = Solid(isNewOrder ? "#A11D1D" : "#8ACCC2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18, 15, 18, 15),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = isNewOrder ? "NOVO PEDIDO IFOOD" : "PEDIDO IFOOD",
                        Foreground = Solid(isNewOrder ? "#A11D1D" : "#08A99B"),
                        FontSize = 28,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"{order.Number}  |  {order.CustomerName}  |  {Money(order.Total)}",
                        Foreground = Solid("#071A2C"),
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0)
                    },
                    statusText,
                    deadlineText
                }
            }
        };

        var items = new ListBox
        {
            ItemsSource = order.Lines.Select(line =>
            {
                var note = string.IsNullOrWhiteSpace(line.Note) ? "" : $" | Obs item: {line.Note.Trim()}";
                return $"{line.Quantity:N0}x {line.Name}   {Money(line.Total)}{note}";
            }).ToList(),
            MinHeight = 105,
            MaxHeight = 135,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var details = BorderCard();
        details.Margin = new Thickness(0, 12, 0, 0);
        details.Child = new StackPanel
        {
            Children =
            {
                SectionTitle("Dados do pedido"),
                new TextBlock { Text = $"orderId: {order.ExternalOrderId}", Foreground = Solid("#071A2C"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Pedido iFood: {EmptyDash(order.ExternalDisplayId)} | Status: {NormalizeIFoodBoardStatus(order.Status)}", Foreground = Solid("#071A2C"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Cliente: {order.CustomerName}", Foreground = Solid("#071A2C"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = BuildIFoodOrderTypeText(order), Foreground = Solid("#08A99B"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = BuildIFoodScheduleText(order), Foreground = Solid("#99620D"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Entrega: {BuildIFoodShipmentText(order)}", Foreground = Solid("#08A99B"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = BuildIFoodPaymentText(order), Foreground = Solid("#0B3A52"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = BuildIFoodVoucherText(order), Foreground = Solid("#08A99B"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = BuildIFoodCancellationText(order), Foreground = RedText, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"CPF/CNPJ: {EmptyDash(order.CustomerCpf)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Telefone: {EmptyDash(order.Phone)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Endereco: {EmptyDash(order.Address)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Bairro: {EmptyDash(order.District)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = $"Obs: {EmptyDash(order.Notes)}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 3, 0, 8), TextWrapping = TextWrapping.Wrap },
                items
            }
        };

        var buttons = new UniformGrid { Columns = 3, Rows = 2 };
        buttons.Children.Add(confirm);
        buttons.Children.Add(prepare);
        buttons.Children.Add(ready);
        buttons.Children.Add(dispatch);
        buttons.Children.Add(cancel);
        buttons.Children.Add(openDelivery);

        var panel = DialogPanel();
        panel.Children.Add(header);
        panel.Children.Add(BuildIFoodHomologationChecklistPanel(order));
        panel.Children.Add(details);
        panel.Children.Add(DialogField("Motivo para cancelar", cancelReasonBox));
        panel.Children.Add(actionMessage);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            PanningMode = PanningMode.VerticalOnly
        });

        var footer = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(18, 12, 8, 8),
            Child = buttons
        };
        Grid.SetRow(footer, 1);
        root.Children.Add(footer);

        dialog.Content = root;
        dialog.ShowDialog();
    }

    private Border BuildIFoodHomologationChecklistPanel(TableTile order)
    {
        var card = BorderCard();
        card.Margin = new Thickness(0, 12, 0, 0);

        var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 8, 0, 0) };

        void AddCell(string label, string value)
        {
            var hasValue = !string.IsNullOrWhiteSpace(value);
            grid.Children.Add(new Border
            {
                Background = Solid(hasValue ? "#EAF8F4" : "#FFF7ED"),
                BorderBrush = Solid(hasValue ? "#8ACCC2" : "#FDBA74"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = label,
                            Foreground = Solid("#5B6B7A"),
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = hasValue ? value.Trim() : "Nao informado neste pedido",
                            Foreground = Solid(hasValue ? "#08A99B" : "#9A5B00"),
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 3, 0, 0),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            });
        }

        AddCell("orderId para o chamado", order.ExternalOrderId);
        AddCell("Cliente", order.CustomerName);
        AddCell("CPF/CNPJ do cliente", order.CustomerCpf);
        AddCell("Telefone", order.Phone);
        AddCell("Tipo / entrega / retirada", $"{BuildIFoodOrderTypeText(order)} | {BuildIFoodShipmentText(order)}".Trim(' ', '|'));
        AddCell("Endereco ou retirada", IsIFoodTakeout(order) ? BuildIFoodShipmentText(order) : order.Address);
        AddCell("Agendamento visivel", BuildIFoodScheduleText(order));
        AddCell("Voucher/cupom", BuildIFoodVoucherText(order));
        AddCell("Pagamento e troco", BuildIFoodPaymentText(order));
        AddCell("Observacao do pedido", BuildIFoodObservationEvidence(order));
        AddCell("Cancelamento", BuildIFoodCancellationText(order));
        AddCell("Status atual", NormalizeIFoodBoardStatus(order.Status));

        card.Child = new StackPanel
        {
            Children =
            {
                SectionTitle("Campos para homologacao"),
                new TextBlock
                {
                    Text = "Grave esta area no video: ela mostra os dados exigidos no checklist do iFood para Order.",
                    Foreground = Solid("#5B6B7A"),
                    TextWrapping = TextWrapping.Wrap
                },
                grid
            }
        };
        return card;
    }

    private async Task<IFoodOrderActionResult> SendIFoodOrderActionAsync(TableTile order, string action, string reason)
    {
        var settings = _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (!settings.HasCloudConnection)
        {
            var message = "iFood nao conectado. Abra IFood > Conectar iFood.";
            SetStatus(message);
            return new IFoodOrderActionResult(false, message);
        }

        if (!CanRunIFoodAction(order, action, out var blockedReason))
        {
            SetStatus(blockedReason);
            return new IFoodOrderActionResult(false, blockedReason);
        }

        IFoodCloudActionResponse response;
        try
        {
            response = await _ifoodClient.SendOrderActionAsync(
                settings.BackendUrl,
                CreateIFoodOrderActionRequest(settings.ConnectionId, order.ExternalOrderId, action, reason, order.ExternalDeliveredBy));
        }
        catch (InvalidOperationException ex) when (IsIFoodAlreadyCancelledMessage(ex.Message))
        {
            MarkIFoodOrderCancelled(order);
            var message = "Pedido ja estava cancelado no iFood. Atualizei o status local.";
            SetStatus(message);
            return new IFoodOrderActionResult(true, message);
        }

        order.Status = NormalizeIFoodBoardStatus(string.IsNullOrWhiteSpace(response.Status) ? OrderStatusFromIFoodAction(action) : response.Status);
        if (!string.IsNullOrWhiteSpace(response.DeliveredBy))
        {
            order.ExternalDeliveredBy = NormalizeIFoodDeliveredBy(response.DeliveredBy);
        }

        UpdateIFoodDynamicDetail(order);

        SaveStore();
        RefreshBoardForMode();
        var statusMessage = string.IsNullOrWhiteSpace(response.Message) ? $"iFood atualizado: {order.Status}." : response.Message;
        if (string.Equals(action, "confirm", StringComparison.OrdinalIgnoreCase))
        {
            var printed = TryPrintTextToDefaultPrinter(
                BuildDeliveryPrintText(order, order.District, _appSettings.PrintLayout),
                $"iFood {order.Number}",
                _appSettings.PrintLayout == "PEQUENO");
            statusMessage += printed
                ? " Pedido impresso."
                : " Impressora padrao indisponivel.";
        }

        SetStatus(statusMessage);
        return new IFoodOrderActionResult(true, statusMessage);
    }

    private readonly record struct IFoodOrderActionResult(bool Success, string Message);

    private static bool IsIFoodAlreadyCancelledMessage(string message)
    {
        var normalized = (message ?? "").ToLowerInvariant();
        return normalized.Contains("already cancelled", StringComparison.Ordinal)
               || normalized.Contains("already canceled", StringComparison.Ordinal)
               || normalized.Contains("ja cancelado", StringComparison.Ordinal)
               || normalized.Contains("já cancelado", StringComparison.Ordinal);
    }

    private void MarkIFoodOrderCancelled(TableTile order)
    {
        order.Status = "CANCELADO";
        UpdateIFoodDynamicDetail(order);
        SaveStore();
        RefreshBoardForMode();
    }

    private static string OrderStatusFromIFoodAction(string action)
    {
        return action switch
        {
            "confirm" => "PREPARANDO",
            "prepare" => "PREPARANDO",
            "ready" => "PRONTO",
            "dispatch" => "DESPACHADO",
            "cancel" => "CANCELAMENTO",
            _ => "IFOOD"
        };
    }

    private static bool CanRunIFoodAction(TableTile order, string action, out string reason)
    {
        var status = NormalizeIFoodBoardStatus(order.Status);
        var deliveredBy = NormalizeIFoodDeliveredBy(order.ExternalDeliveredBy);
        var finished = status is "DESPACHADO" or "CANCELAMENTO" or "CANCELADO" or "ENTREGUE" or "CONCLUDED";
        reason = "";

        if (finished)
        {
            reason = $"Pedido iFood ja esta {status.ToLowerInvariant()}. Nao ha nova acao para enviar.";
            return false;
        }

        if (IsIFoodConfirmationExpired(order))
        {
            reason = "Prazo de 8 minutos para aceitar o pedido iFood expirou. Sincronize o iFood antes de agir nesse pedido.";
            return false;
        }

        switch (action)
        {
            case "confirm":
                if (status is "NOVO" or "PLACED" or "CREATED")
                {
                    return true;
                }

                reason = "Pedido iFood ja foi confirmado. Nao precisa confirmar de novo.";
                return false;

            case "cancel":
                if (status is "NOVO" or "PLACED" or "CREATED")
                {
                    return true;
                }

                reason = "Pedido iFood confirmado/em preparo nao pode ser cancelado por esse atalho.";
                return false;

            case "prepare":
                if (status is "CONFIRMADO" or "ACEITO")
                {
                    return true;
                }

                reason = status is "NOVO" or "PLACED" or "CREATED"
                    ? "Confirme o pedido antes de iniciar preparo."
                    : "Pedido iFood ja passou da etapa de preparo.";
                return false;

            case "ready":
                if (status is "CONFIRMADO" or "ACEITO" or "PREPARO" or "PREPARANDO")
                {
                    return true;
                }

                reason = status is "NOVO" or "PLACED" or "CREATED"
                    ? "Confirme o pedido antes de marcar pronto."
                    : "Pedido iFood ja foi marcado como pronto ou finalizado.";
                return false;

            case "dispatch":
                if (IsIFoodShipment(deliveredBy))
                {
                    reason = "Entrega e do iFood. Marque pronto e aguarde o entregador iFood; nao despache pela loja.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(deliveredBy) && !IsMerchantShipment(deliveredBy))
                {
                    reason = $"Tipo de entrega iFood nao reconhecido: {deliveredBy}.";
                    return false;
                }

                if (status is "CONFIRMADO" or "ACEITO" or "PREPARO" or "PREPARANDO" or "PRONTO")
                {
                    return true;
                }

                reason = "Confirme o pedido antes de despachar entrega propria.";
                return false;

            default:
                reason = "Acao iFood invalida.";
                return false;
        }
    }

    private static void ApplyIFoodActionButtonState(TableTile order, Button button, string action)
    {
        var canRun = CanRunIFoodAction(order, action, out var reason);
        button.IsEnabled = canRun;
        button.Opacity = canRun ? 1 : 0.42;
        button.ToolTip = canRun ? null : reason;
    }

    private static string BuildIFoodActionHint(TableTile order)
    {
        var status = NormalizeIFoodBoardStatus(order.Status);
        if (IsIFoodDelivered(order))
        {
            return "Pedido entregue no iFood. Nenhuma acao restante para enviar.";
        }

        if (IsIFoodDeliveryLate(order))
        {
            return $"{BuildIFoodDeliveryTimeText(order)}. Confira preparo/coleta antes de avancar.";
        }

        if (status is "NOVO" or "PLACED" or "CREATED")
        {
            return IsIFoodConfirmationExpired(order)
                ? "Prazo de aceite expirou. O iFood deve cancelar esse pedido; sincronize antes de agir."
                : $"Novo pedido: {BuildIFoodConfirmationDeadlineText(order)}. Depois de confirmado, o cancelamento fica bloqueado.";
        }

        if (status is "PREPARO" or "PREPARANDO" or "CONFIRMADO" or "ACEITO")
        {
            var timeText = BuildIFoodPreparationTimeText(order);
            return string.IsNullOrWhiteSpace(timeText)
                ? "Pedido em preparo. Marque pronto quando finalizar."
                : $"Pedido em preparo. {timeText}.";
        }

        if (status == "PRONTO")
        {
            return IsIFoodShipment(order.ExternalDeliveredBy)
                ? "Pedido pronto. Entrega iFood: aguarde a coleta do entregador."
                : "Pedido pronto. Entrega propria: despache quando sair para entrega.";
        }

        return $"Pedido iFood em status {status}.";
    }

    private static string BuildIFoodOrderStatusText(TableTile order)
    {
        var deliveryTime = BuildIFoodDeliveryTimeText(order);
        var parts = new[]
        {
            $"Status: {NormalizeIFoodBoardStatus(order.Status)}",
            BuildIFoodOrderTypeText(order),
            BuildIFoodScheduleText(order),
            string.IsNullOrWhiteSpace(deliveryTime) ? "" : deliveryTime,
            $"Pedido iFood: {order.ExternalDisplayId}",
            BuildIFoodShipmentText(order),
            BuildIFoodPaymentText(order),
            BuildIFoodVoucherText(order),
            $"ID {order.ExternalOrderId}"
        }.Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join("  |  ", parts);
    }

    private static string BuildIFoodOrderTypeText(TableTile order)
    {
        var type = (order.ExternalOrderType ?? "").Trim().ToUpperInvariant();
        return type switch
        {
            "TAKEOUT" or "PICKUP" or "RETIRADA" => "Tipo: retirada no local",
            "DELIVERY" => "Tipo: entrega",
            _ => string.IsNullOrWhiteSpace(type) ? "" : $"Tipo: {type}"
        };
    }

    private static bool IsIFoodTakeout(TableTile order)
    {
        var type = (order.ExternalOrderType ?? "").Trim().ToUpperInvariant();
        return type is "TAKEOUT" or "PICKUP" or "RETIRADA";
    }

    private static string BuildIFoodScheduleText(TableTile order)
    {
        if (!string.Equals(order.ExternalOrderTiming, "SCHEDULED", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        var start = order.ExternalPreparationStartAt ?? order.ExternalDeliveryExpectedAt;
        return start.HasValue
            ? $"Agendado: {start.Value:dd/MM/yyyy HH:mm}"
            : "Agendado: horario nao informado";
    }

    private static string BuildIFoodPaymentText(TableTile order)
    {
        var summary = (order.ExternalPaymentSummary ?? "").Trim();
        var method = (order.ExternalPaymentMethod ?? "").Trim();
        var change = order.ExternalChangeFor > 0m ? $" | Troco para {Money(order.ExternalChangeFor)}" : "";
        if (!string.IsNullOrWhiteSpace(summary))
        {
            return $"Pagamento: {summary}{change}";
        }

        return string.IsNullOrWhiteSpace(method) ? change.TrimStart(' ', '|') : $"Pagamento: {method}{change}";
    }

    private static string BuildIFoodVoucherText(TableTile order)
    {
        return string.IsNullOrWhiteSpace(order.ExternalVoucherSummary)
            ? ""
            : $"Voucher/cupom: {order.ExternalVoucherSummary.Trim()}";
    }

    private static string BuildIFoodCancellationText(TableTile order)
    {
        if (!string.IsNullOrWhiteSpace(order.ExternalCancellationInfo))
        {
            return $"Cancelamento: {order.ExternalCancellationInfo.Trim()}";
        }

        var status = NormalizeIFoodBoardStatus(order.Status);
        return status is "CANCELAMENTO" or "CANCELADO"
            ? $"Cancelamento recebido da plataforma: {status}"
            : "";
    }

    private static string BuildIFoodObservationEvidence(TableTile order)
    {
        if (string.IsNullOrWhiteSpace(order.Notes))
        {
            return "";
        }

        var ignored = new[]
        {
            order.ExternalOrderId,
            order.ExternalDisplayId,
            order.ExternalShipmentInfo
        }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

        var lines = order.Notes
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !line.StartsWith("iFood ", StringComparison.OrdinalIgnoreCase))
            .Where(line => !line.StartsWith("ESTOQUE IFOOD:", StringComparison.OrdinalIgnoreCase))
            .Where(line => ignored.All(value => !line.Equals(value, StringComparison.OrdinalIgnoreCase)))
            .Where(line => ignored.All(value => !line.Contains(value, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join(" | ", lines);
    }

    private static string BuildIFoodScenarioEvidence(TableTile order)
    {
        var parts = new[]
        {
            NormalizeIFoodBoardStatus(order.Status),
            BuildIFoodOrderTypeText(order),
            BuildIFoodScheduleText(order),
            BuildIFoodVoucherText(order),
            BuildIFoodPaymentText(order),
            BuildIFoodCancellationText(order),
            string.IsNullOrWhiteSpace(order.CustomerCpf) ? "" : $"CPF/CNPJ: {order.CustomerCpf}",
            BuildIFoodObservationEvidence(order)
        }.Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(" | ", parts);
    }

    private static string BuildIFoodHomologationOrderSummary(TableTile order)
    {
        var lines = new[]
        {
            $"orderId: {order.ExternalOrderId}",
            $"displayId: {order.ExternalDisplayId}",
            $"status: {NormalizeIFoodBoardStatus(order.Status)}",
            BuildIFoodOrderTypeText(order),
            BuildIFoodScheduleText(order),
            BuildIFoodShipmentText(order),
            BuildIFoodPaymentText(order),
            BuildIFoodVoucherText(order),
            BuildIFoodCancellationText(order),
            $"Cliente: {order.CustomerName}",
            string.IsNullOrWhiteSpace(order.Phone) ? "" : $"Telefone: {order.Phone}",
            string.IsNullOrWhiteSpace(order.Address) ? "" : $"Endereco: {order.Address}",
            string.IsNullOrWhiteSpace(order.CustomerCpf) ? "" : $"CPF/CNPJ: {order.CustomerCpf}",
            string.IsNullOrWhiteSpace(BuildIFoodObservationEvidence(order)) ? "" : $"Obs: {BuildIFoodObservationEvidence(order)}"
        }.Where(line => !string.IsNullOrWhiteSpace(line));
        return string.Join(Environment.NewLine, lines);
    }

    private static DateTime LocalTimeOrNow(DateTime? value) => LocalTimeOrNull(value) ?? DateTime.Now;

    private static DateTime? LocalTimeOrNull(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc ? value.Value.ToLocalTime() : value.Value;
    }

    private static bool IsIFoodWaitingForConfirmation(TableTile order)
    {
        var status = NormalizeIFoodBoardStatus(order.Status);
        return status is "NOVO" or "PLACED" or "CREATED";
    }

    private static DateTime? GetIFoodConfirmationDeadline(TableTile order)
    {
        if (order.ExternalConfirmationDeadlineAt.HasValue)
        {
            return order.ExternalConfirmationDeadlineAt.Value;
        }

        var isScheduled = string.Equals(order.ExternalOrderTiming, "SCHEDULED", StringComparison.OrdinalIgnoreCase);
        var baseAt = isScheduled
            ? order.ExternalPreparationStartAt ?? order.ExternalCreatedAt ?? order.CreatedAt
            : order.ExternalCreatedAt ?? order.CreatedAt;
        return baseAt == default ? null : baseAt.AddMinutes(8);
    }

    private static bool IsIFoodConfirmationExpired(TableTile order)
    {
        return IsIFoodWaitingForConfirmation(order)
               && GetIFoodConfirmationDeadline(order) is { } deadline
               && DateTime.Now > deadline;
    }

    private static bool HasIFoodActiveConfirmationCountdown(TableTile order)
    {
        return IsIFoodWaitingForConfirmation(order)
               && GetIFoodConfirmationDeadline(order) is { } deadline
               && DateTime.Now <= deadline;
    }

    private static bool IsIFoodPreparingStatus(TableTile order)
    {
        return NormalizeIFoodBoardStatus(order.Status) is "PREPARO" or "PREPARANDO" or "CONFIRMADO" or "ACEITO";
    }

    private static DateTime? GetIFoodPreparationStart(TableTile order)
    {
        if (order.ExternalPreparationStartAt.HasValue)
        {
            return order.ExternalPreparationStartAt.Value;
        }

        return string.Equals(order.ExternalOrderTiming, "IMMEDIATE", StringComparison.OrdinalIgnoreCase)
            ? order.ExternalCreatedAt ?? order.CreatedAt
            : null;
    }

    private static string FormatShortCountdown(DateTime target)
    {
        var remaining = target - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            return "00:00";
        }

        var minutes = Math.Max(0, (int)Math.Floor(remaining.TotalMinutes));
        return $"{minutes:00}:{remaining.Seconds:00}";
    }

    private static bool IsIFoodFinalOrCancelled(TableTile order)
    {
        var status = NormalizeIFoodBoardStatus(order.Status);
        return status is "ENTREGUE" or "FINALIZADO" or "CANCELAMENTO" or "CANCELADO";
    }

    private static bool IsIFoodDelivered(TableTile order)
    {
        return order.ExternalDeliveredAt.HasValue || NormalizeIFoodBoardStatus(order.Status) is "ENTREGUE" or "FINALIZADO";
    }

    private static bool IsIFoodDeliveryLate(TableTile order)
    {
        return IsIFoodDeliveryBoard(order)
               && order.ExternalDeliveryExpectedAt.HasValue
               && DateTime.Now > order.ExternalDeliveryExpectedAt.Value
               && !IsIFoodFinalOrCancelled(order);
    }

    private static string BuildIFoodDeliveryTimeText(TableTile order)
    {
        if (!IsIFoodDeliveryBoard(order))
        {
            return "";
        }

        if (order.ExternalDeliveredAt is { } deliveredAt)
        {
            return $"Entregue {deliveredAt:HH:mm}";
        }

        if (NormalizeIFoodBoardStatus(order.Status) == "ENTREGUE")
        {
            return "Entregue";
        }

        if (order.ExternalDeliveryExpectedAt is not { } expectedAt)
        {
            return "";
        }

        if (IsIFoodDeliveryLate(order))
        {
            var delay = DateTime.Now - expectedAt;
            return $"Atrasado {Math.Max(1, (int)Math.Ceiling(delay.TotalMinutes))}m";
        }

        return $"Previsao {expectedAt:HH:mm}";
    }

    private static string BuildIFoodPreparationTimeText(TableTile order)
    {
        if (!IsIFoodDeliveryBoard(order) || !IsIFoodPreparingStatus(order))
        {
            return "";
        }

        var preparationStart = GetIFoodPreparationStart(order);
        if (preparationStart is { } startAt && DateTime.Now < startAt)
        {
            return $"preparar as {startAt:HH:mm} ({FormatShortCountdown(startAt)})";
        }

        if (order.ExternalDeliveryExpectedAt is { } expectedAt)
        {
            return $"previsao {expectedAt:HH:mm}";
        }

        return "";
    }

    private static string BuildIFoodConfirmationDeadlineText(TableTile order)
    {
        if (!IsIFoodWaitingForConfirmation(order))
        {
            return "Prazo de aceite: pedido ja confirmado/avancado.";
        }

        var deadline = GetIFoodConfirmationDeadline(order);
        if (!deadline.HasValue)
        {
            return "Prazo de aceite: nao informado pelo iFood.";
        }

        var remaining = deadline.Value - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            return $"Prazo de aceite expirado as {deadline.Value:HH:mm}.";
        }

        var minutes = Math.Max(0, (int)Math.Floor(remaining.TotalMinutes));
        return $"Aceitar ate {deadline.Value:HH:mm:ss}  |  faltam {minutes:00}:{remaining.Seconds:00}";
    }

    private static string BuildBoardTileTimerText(TableTile order)
    {
        if (!IsIFoodDeliveryBoard(order))
        {
            return "";
        }

        if (IsIFoodWaitingForConfirmation(order))
        {
            var deadline = GetIFoodConfirmationDeadline(order);
            if (!deadline.HasValue)
            {
                return "";
            }

            var remaining = deadline.Value - DateTime.Now;
            if (remaining <= TimeSpan.Zero)
            {
                return "EXPIRADO";
            }

            var minutes = Math.Max(0, (int)Math.Floor(remaining.TotalMinutes));
            return $"ACEITE {minutes:00}:{remaining.Seconds:00}";
        }

        if (IsIFoodDelivered(order))
        {
            return order.ExternalDeliveredAt is { } deliveredAt ? $"ENTREGUE {deliveredAt:HH:mm}" : "ENTREGUE";
        }

        if (IsIFoodDeliveryLate(order))
        {
            var delay = DateTime.Now - order.ExternalDeliveryExpectedAt!.Value;
            return $"ATRASADO {Math.Max(1, (int)Math.Ceiling(delay.TotalMinutes))}M";
        }

        if (IsIFoodPreparingStatus(order))
        {
            var preparationStart = GetIFoodPreparationStart(order);
            if (preparationStart is { } startAt && DateTime.Now < startAt)
            {
                return $"PREP {FormatShortCountdown(startAt)}";
            }
        }

        return order.ExternalDeliveryExpectedAt is { } expectedAt && !IsIFoodFinalOrCancelled(order)
            ? $"PREV {expectedAt:HH:mm}"
            : "";
    }

    private static Brush BuildBoardTileTimerBrush(TableTile order)
    {
        if (!IsIFoodDeliveryBoard(order))
        {
            return AmberText;
        }

        if (IsIFoodWaitingForConfirmation(order))
        {
            var deadline = GetIFoodConfirmationDeadline(order);
            if (!deadline.HasValue)
            {
                return AmberText;
            }

            var remaining = deadline.Value - DateTime.Now;
            return remaining <= TimeSpan.Zero || remaining <= TimeSpan.FromMinutes(2) ? RedText : AmberText;
        }

        if (IsIFoodDelivered(order))
        {
            return GreenText;
        }

        if (IsIFoodDeliveryLate(order))
        {
            return RedText;
        }

        return AmberText;
    }

    private static void UpdateIFoodDynamicDetail(TableTile tile)
    {
        if (!IsIFoodDeliveryBoard(tile))
        {
            return;
        }

        var type = string.IsNullOrWhiteSpace(tile.ExternalOrderTiming)
            ? "IFOOD"
            : $"IFOOD {tile.ExternalOrderTiming}";
        if (IsIFoodTakeout(tile))
        {
            type = $"{type} RETIRADA";
        }

        var stage = IsIFoodWaitingForConfirmation(tile) && GetIFoodConfirmationDeadline(tile) is { } deadline
            ? $"ACEITAR {deadline:HH:mm}"
            : IsIFoodDelivered(tile)
                ? "ENTREGUE"
                : IsIFoodDeliveryLate(tile)
                    ? "ATRASADO"
                    : IsIFoodPreparingStatus(tile)
                        ? "PREPARANDO"
                        : NormalizeIFoodBoardStatus(tile.Status);
        tile.Detail = $"{type} {stage} {IFoodShipmentLabel(tile.ExternalDeliveredBy)}".Trim();
        tile.RefreshVisualState();
    }

    private static string BuildIFoodShipmentText(TableTile order)
    {
        var parts = new List<string>();
        if (IsIFoodTakeout(order))
        {
            parts.Add("RETIRADA NO LOCAL");
        }

        var label = IFoodShipmentLabel(order.ExternalDeliveredBy);
        if (!string.IsNullOrWhiteSpace(label))
        {
            parts.Add(label);
        }

        if (!string.IsNullOrWhiteSpace(order.ExternalPickupCode))
        {
            parts.Add($"Coleta {order.ExternalPickupCode}");
        }

        if (!string.IsNullOrWhiteSpace(order.ExternalDeliveryLocalizer))
        {
            parts.Add($"Localizador {order.ExternalDeliveryLocalizer}");
        }

        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(order.ExternalShipmentInfo))
        {
            parts.Add(order.ExternalShipmentInfo);
        }

        return parts.Count == 0 ? "Entrega nao informada" : string.Join(" | ", parts);
    }

    private static string EmptyDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private void PlayIFoodOrderSound()
    {
        var customPath = _appSettings.IFoodAlertSoundPath;
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            if (TryPlayIFoodAlertFile(customPath))
            {
                return;
            }
        }

        var bundledPath = Path.Combine(AppContext.BaseDirectory, DefaultIFoodAlertSoundFile);
        if (File.Exists(bundledPath) && TryPlayIFoodAlertFile(bundledPath))
        {
            return;
        }

        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch (Exception fallback)
        {
            Debug.WriteLine($"iFood fallback sound failed: {fallback.Message}");
        }
    }

    private bool TryPlayIFoodAlertFile(string path)
    {
        try
        {
            _ifoodAlertPlayer?.Stop();
            _ifoodAlertPlayer = new MediaPlayer();
            _ifoodAlertPlayer.Open(new Uri(path, UriKind.Absolute));
            _ifoodAlertPlayer.Volume = 1;
            _ifoodAlertPlayer.Play();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"iFood alert file failed: {ex.Message}");
            return false;
        }
    }

    private void StopIFoodOrderSound()
    {
        try
        {
            _ifoodAlertPlayer?.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"iFood alert stop failed: {ex.Message}");
        }
    }

    private static string BuildIFoodStatusText(IFoodIntegrationSettings settings)
    {
        var status = settings.HasCloudConnection
            ? string.IsNullOrWhiteSpace(settings.MerchantName)
                ? "conectado"
                : $"conectado: {settings.MerchantName}"
            : !settings.Enabled && !string.IsNullOrWhiteSpace(settings.ConnectionId)
                ? "conectado, loja offline no PDV"
                : string.IsNullOrWhiteSpace(settings.ConnectionId)
                    ? "nao conectado"
                    : "aguardando autorizacao do iFood";

        var last = settings.LastSyncUtc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(settings.LastSyncUtc.Value, TimeZoneInfo.Local).ToString("g", Brazil)
            : "nunca";
        return $"Status iFood: {status}. Ultima atualizacao: {last}.";
    }

    private static bool EnsureIFoodCloudSettings(IFoodIntegrationSettings settings)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(settings.BackendUrl) ||
            settings.BackendUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            settings.BackendUrl = IFoodIntegrationSettings.DefaultBackendUrl;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.WebhookUrl) ||
            settings.WebhookUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            settings.WebhookUrl = $"{IFoodIntegrationSettings.DefaultBackendUrl}/webhook";
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(settings.ClientId) || !string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            settings.ClientId = "";
            settings.ClientSecret = "";
            settings.AccessToken = "";
            settings.RefreshToken = "";
            settings.AuthorizationCodeVerifier = "";
            changed = true;
        }

        return changed;
    }

    private void ShowInventoryDialog()
    {
        if (!RequirePermission(CanManageInventory, "Estoque operacional"))
        {
            return;
        }

        var dialog = CreateDialog("Estoque operacional", 1020, 680);
        var sortedProducts = Products.OrderBy(product => product.Category).ThenBy(product => product.Name).ToList();
        var qtyBox = new TextBox();
        var minBox = new TextBox();
        var movementBox = new TextBox { Text = "1" };
        var reasonBox = new TextBox { Text = "Entrada manual" };
        var searchBox = new TextBox
        {
            Margin = new Thickness(0, 8, 8, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = "Busque por codigo, produto, grupo ou status"
        };
        var searchCountText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var createProductButton = DialogButton("Criar produto", "#08A99B");
        createProductButton.Height = 34;
        createProductButton.MinWidth = 112;
        createProductButton.Padding = new Thickness(14, 0, 14, 0);
        createProductButton.Margin = new Thickness(10, 0, 0, 0);
        createProductButton.HorizontalAlignment = HorizontalAlignment.Right;
        var movementSummary = new TextBlock
        {
            Text = "Sem movimento registrado.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var selectedName = new TextBlock
        {
            Text = "Selecione um produto",
            Foreground = Solid("#071A2C"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        var selectedMeta = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        var selectedStatus = new TextBlock
        {
            Foreground = Solid("#08A99B"),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var totalProductsValue = new TextBlock();
        var lowStockValue = new TextBlock();
        var totalUnitsValue = new TextBlock();
        var inventoryValue = new TextBlock();

        void LoadStock(ProductTile product)
        {
            qtyBox.Text = product.StockQuantity.ToString("N0", Brazil);
            minBox.Text = product.MinimumStock.ToString("N0", Brazil);
            selectedName.Text = product.Name;
            var ifoodMeta = _appSettings.IFood?.HasCloudConnection == true
                ? HasIFoodCatalogLink(product)
                    ? "  |  iFood sync ativo"
                    : string.Equals(product.Category, "IFOOD", StringComparison.OrdinalIgnoreCase)
                        ? "  |  iFood sem vinculo de catalogo"
                        : ""
                : "";
            selectedMeta.Text = $"{product.Code}  |  {product.Category}  |  {product.PriceText}{ifoodMeta}";
            selectedStatus.Text = product.StockStatusText;
            selectedStatus.Foreground = Solid(product.IsLowStock ? "#A11D1D" : "#08A99B");
            var lastMovement = product.StockHistory
                .OrderByDescending(item => item.When)
                .FirstOrDefault();
            movementSummary.Text = lastMovement is null
                ? "Sem movimento registrado."
                : $"Ultimo: {lastMovement.Display}";
        }

        void ClearStockSelection(string message)
        {
            qtyBox.Text = "";
            minBox.Text = "";
            selectedName.Text = message;
            selectedMeta.Text = "";
            selectedStatus.Text = "";
            movementSummary.Text = "Nenhum produto selecionado.";
        }

        Border CreateInventoryMetric(string title, TextBlock valueText, string detail, string color)
        {
            valueText.Foreground = Solid("#071A2C");
            valueText.FontSize = 21;
            valueText.FontWeight = FontWeights.Bold;
            valueText.Margin = new Thickness(0, 3, 0, 0);

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Margin = new Thickness(0, 0, 10, 0),
                ClipToBounds = true
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(new Border { Background = Solid(color) });

            var stack = new StackPanel { Margin = new Thickness(14, 12, 14, 10) };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Solid("#5B6B7A"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            });
            stack.Children.Add(valueText);
            stack.Children.Add(new TextBlock
            {
                Text = detail,
                Foreground = Solid("#5B6B7A"),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(stack, 1);
            grid.Children.Add(stack);
            border.Child = grid;
            return border;
        }

        void UpdateMetrics()
        {
            totalProductsValue.Text = sortedProducts.Count.ToString("N0", Brazil);
            lowStockValue.Text = sortedProducts.Count(product => product.IsLowStock).ToString("N0", Brazil);
            totalUnitsValue.Text = sortedProducts.Sum(product => product.StockQuantity).ToString("N0", Brazil);
            inventoryValue.Text = Money(sortedProducts.Sum(product => Math.Max(0, product.StockQuantity) * product.Price));
        }

        var grid = new Grid { Margin = new Thickness(18) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(94) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var metrics = new UniformGrid { Columns = 4, Rows = 1 };
        metrics.Children.Add(CreateInventoryMetric("Produtos", totalProductsValue, "itens cadastrados", "#0B3A52"));
        metrics.Children.Add(CreateInventoryMetric("Criticos", lowStockValue, "abaixo do estoque minimo", "#A11D1D"));
        metrics.Children.Add(CreateInventoryMetric("Unidades", totalUnitsValue, "saldo fisico total", "#99620D"));
        metrics.Children.Add(CreateInventoryMetric("Valor estoque", inventoryValue, "preco de venda estimado", "#08A99B"));
        grid.Children.Add(metrics);

        var body = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        Grid.SetRow(body, 1);

        var tableCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Margin = new Thickness(0, 0, 12, 0)
        };
        var tablePanel = new Grid { Margin = new Thickness(14, 12, 14, 14) };
        tablePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(86) });
        tablePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var tableHeader = new Grid();
        tableHeader.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        tableHeader.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tableHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var tableTitle = new StackPanel();
        tableTitle.Children.Add(new TextBlock
        {
            Text = "Produtos em estoque",
            Foreground = Solid("#071A2C"),
            FontSize = 15,
            FontWeight = FontWeights.Bold
        });
        tableTitle.Children.Add(new TextBlock
        {
            Text = "Busque e selecione um produto para ajustar saldo ou registrar movimento.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
        tableHeader.Children.Add(tableTitle);

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        searchCountText.Text = $"{sortedProducts.Count:N0} exibidos";
        headerActions.Children.Add(searchCountText);
        headerActions.Children.Add(createProductButton);
        Grid.SetColumn(searchCountText, 1);
        Grid.SetColumn(headerActions, 1);
        tableHeader.Children.Add(headerActions);

        searchBox.Text = "";
        Grid.SetRow(searchBox, 1);
        Grid.SetColumnSpan(searchBox, 2);
        tableHeader.Children.Add(searchBox);
        tablePanel.Children.Add(tableHeader);

        var table = new DataGrid
        {
            ItemsSource = sortedProducts,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.None,
            AlternatingRowBackground = Solid("#F8FBFD"),
            BorderBrush = Solid("#E3EBF2"),
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            RowHeight = 39,
            FontSize = 13
        };
        table.Columns.Add(new DataGridTextColumn { Header = "Codigo", Binding = new System.Windows.Data.Binding(nameof(ProductTile.Code)), Width = 72 });
        table.Columns.Add(new DataGridTextColumn { Header = "Produto", Binding = new System.Windows.Data.Binding(nameof(ProductTile.Name)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        table.Columns.Add(new DataGridTextColumn { Header = "Grupo", Binding = new System.Windows.Data.Binding(nameof(ProductTile.Category)), Width = 112 });
        table.Columns.Add(new DataGridTextColumn { Header = "Atual", Binding = new System.Windows.Data.Binding(nameof(ProductTile.StockQuantity)) { StringFormat = "N0" }, Width = 74 });
        table.Columns.Add(new DataGridTextColumn { Header = "Min.", Binding = new System.Windows.Data.Binding(nameof(ProductTile.MinimumStock)) { StringFormat = "N0" }, Width = 64 });
        table.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding(nameof(ProductTile.StockStatusText)), Width = 96 });

        bool ProductMatchesSearch(ProductTile product, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return true;
            }

            return product.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || product.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
                   || product.StockStatusText.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        void ApplySearch(ProductTile? selected = null)
        {
            var term = searchBox.Text.Trim();
            var filtered = sortedProducts.Where(product => ProductMatchesSearch(product, term)).ToList();
            table.ItemsSource = filtered;
            searchCountText.Text = string.IsNullOrWhiteSpace(term)
                ? $"{filtered.Count:N0} exibidos"
                : $"{filtered.Count:N0} encontrados";

            if (selected is not null && filtered.Contains(selected))
            {
                table.SelectedItem = selected;
                table.ScrollIntoView(selected);
            }
            else if (filtered.Count > 0)
            {
                table.SelectedIndex = 0;
            }
            else
            {
                ClearStockSelection("Nenhum produto encontrado");
            }
        }

        searchBox.TextChanged += (_, _) => ApplySearch();
        createProductButton.Click += (_, _) =>
        {
            var selectedBefore = table.SelectedItem as ProductTile;
            var previousCodes = Products.Select(product => product.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            ShowProductCatalogDialog();
            sortedProducts = Products.OrderBy(product => product.Category).ThenBy(product => product.Name).ToList();
            var created = sortedProducts.FirstOrDefault(product => !previousCodes.Contains(product.Code));
            UpdateMetrics();
            ApplySearch(created ?? selectedBefore);
            table.Items.Refresh();
        };
        table.SelectionChanged += (_, _) =>
        {
            if (table.SelectedItem is ProductTile product) LoadStock(product);
        };
        Grid.SetRow(table, 1);
        tablePanel.Children.Add(table);
        tableCard.Child = tablePanel;
        body.Children.Add(tableCard);

        void RefreshInventory(ProductTile product)
        {
            ApplySearch(product);
            UpdateMetrics();
            SaveStore();
            table.Items.Refresh();
        }

        void ChangeStock(decimal multiplier, string type)
        {
            if (table.SelectedItem is not ProductTile product)
            {
                return;
            }

            var amount = ParseMoney(movementBox.Text, 0);
            if (amount <= 0)
            {
                SetStatus("Informe uma quantidade maior que zero.");
                movementBox.Focus();
                movementBox.SelectAll();
                return;
            }

            if (multiplier < 0 && amount > product.StockQuantity)
            {
                SetStatus($"Saida maior que o saldo de {product.Name}.");
                movementBox.Focus();
                movementBox.SelectAll();
                return;
            }

            var delta = amount * multiplier;
            product.StockQuantity = Math.Max(0, product.StockQuantity + delta);
            product.MinimumStock = ParseMoney(minBox.Text, product.MinimumStock);
            product.StockHistory.Add(new StockMovement
            {
                ProductCode = product.Code,
                Type = type,
                Quantity = delta,
                Reason = string.IsNullOrWhiteSpace(reasonBox.Text) ? type : reasonBox.Text.Trim(),
                When = DateTime.Now
            });
            RefreshInventory(product);
            SetStatus($"{type}: {product.Name} {amount:N0}. Saldo {product.StockQuantity:N0}");
            QueueIFoodStockSync(product, $"Estoque {type}");
        }

        var setButton = DialogButton("Salvar saldo/minimo", "#0B3A52");
        setButton.Click += (_, _) =>
        {
            if (table.SelectedItem is ProductTile product)
            {
                product.StockQuantity = ParseMoney(qtyBox.Text, product.StockQuantity);
                product.MinimumStock = ParseMoney(minBox.Text, product.MinimumStock);
                product.StockHistory.Add(new StockMovement
                {
                    ProductCode = product.Code,
                    Type = "AJUSTE",
                    Quantity = 0,
                    Reason = "Ajuste manual de saldo/minimo",
                    When = DateTime.Now
                });
                RefreshInventory(product);
                SetStatus($"Estoque atualizado: {product.Name} {product.StockQuantity:N0}");
                QueueIFoodStockSync(product, "Ajuste manual de estoque");
            }
        };
        var inButton = DialogButton("Entrada no estoque", "#08A99B");
        inButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(reasonBox.Text)
                || string.Equals(reasonBox.Text.Trim(), "Saida manual", StringComparison.OrdinalIgnoreCase))
            {
                reasonBox.Text = "Entrada manual";
            }

            ChangeStock(1, "ENTRADA");
        };
        var outButton = DialogButton("Saida do estoque", "#A11D1D");
        outButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(reasonBox.Text)
                || string.Equals(reasonBox.Text.Trim(), "Entrada manual", StringComparison.OrdinalIgnoreCase))
            {
                reasonBox.Text = "Saida manual";
            }

            ChangeStock(-1, "SAIDA");
        };

        movementBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                reasonBox.Focus();
                reasonBox.SelectAll();
                e.Handled = true;
            }
        };
        reasonBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                inButton.Focus();
                e.Handled = true;
            }
        };

        foreach (var button in new[] { setButton, inButton, outButton })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }
        inButton.Margin = new Thickness(0, 8, 0, 0);
        outButton.Margin = new Thickness(0, 8, 0, 0);

        var movementButtons = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 0)
        };
        movementButtons.Children.Add(inButton);
        movementButtons.Children.Add(outButton);

        Border SideSection(string title, string detail, params UIElement[] children)
        {
            var section = new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 10, 0, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Solid("#071A2C"),
                FontSize = 14,
                FontWeight = FontWeights.Bold
            });
            if (!string.IsNullOrWhiteSpace(detail))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = detail,
                    Foreground = Solid("#5B6B7A"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 8)
                });
            }

            foreach (var child in children)
            {
                stack.Children.Add(child);
            }

            section.Child = stack;
            return section;
        }

        var sideCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9)
        };
        var panel = new StackPanel { Margin = new Thickness(18, 16, 18, 16) };
        panel.Children.Add(selectedName);
        panel.Children.Add(selectedMeta);
        panel.Children.Add(selectedStatus);
        panel.Children.Add(new Border { Height = 1, Background = Solid("#E3EBF2"), Margin = new Thickness(0, 14, 0, 10) });
        panel.Children.Add(SideSection(
            "Saldo do produto",
            "Use para corrigir o saldo atual ou o estoque minimo.",
            DialogLabel("Quantidade atual"),
            qtyBox,
            DialogLabel("Estoque minimo"),
            minBox,
            setButton));
        panel.Children.Add(SideSection(
            "Entrada e saida",
            "A saida usa exatamente a quantidade digitada abaixo.",
            DialogLabel("Quantidade"),
            movementBox,
            DialogLabel("Motivo"),
            reasonBox,
            movementButtons));
        panel.Children.Add(movementSummary);
        sideCard.Child = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false
        };
        Grid.SetColumn(sideCard, 1);
        body.Children.Add(sideCard);
        grid.Children.Add(body);
        dialog.Content = grid;
        UpdateMetrics();
        ApplySearch();
        dialog.ShowDialog();
    }

    private void ShowCashDialog()
    {
        if (!RequirePermission(IsCashUser, "Operacao de caixa"))
        {
            return;
        }

        var dialog = CreateDialog("Caixa: entradas, retiradas e fechamento", 740, 520);
        var totalText = new TextBlock { Text = Money(_cashTotal), Foreground = GreenText, FontSize = 30, FontWeight = FontWeights.Bold };
        var amountBox = new TextBox { Text = "50,00" };
        var reasonBox = new TextBox { Text = "Movimento de caixa" };
        var list = new ListBox { DisplayMemberPath = nameof(CashMovement.Display), Height = 285, ItemsSource = CashMovements };

        void AddMovement(decimal multiplier, string type)
        {
            if (!IsCashOpen())
            {
                SetStatus("Caixa fechado. Pressione F10 para abrir antes de movimentar.");
                return;
            }

            var amount = ParseMoney(amountBox.Text, 0);
            if (amount <= 0)
            {
                return;
            }

            var signed = amount * multiplier;
            _cashTotal += signed;
            CashMovements.Add(new CashMovement { Type = type, Amount = signed, Reason = reasonBox.Text.Trim(), User = _currentUser, When = DateTime.Now });
            totalText.Text = Money(_cashTotal);
            RefreshTotals();
            SaveStore();
            SetStatus($"{CashMovementLabel(type)}: {Money(amount)}");
        }

        var supply = DialogButton("Entrada de caixa", "#08A99B");
        supply.Click += (_, _) => AddMovement(1, "ENTRADA");
        var withdrawal = DialogButton("Retirada de caixa", "#A11D1D");
        withdrawal.Click += (_, _) => AddMovement(-1, "RETIRADA");
        var close = DialogButton(IsCashOpen() ? "F10 Fechar caixa" : "F10 Abrir caixa", "#0B3A52");
        close.Click += (_, _) =>
        {
            if (ToggleCashRegister())
            {
                totalText.Text = Money(_cashTotal);
                close.Content = IsCashOpen() ? "F10 Fechar caixa" : "F10 Abrir caixa";
                list.Items.Refresh();
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "Atalho do operador: F10 abre ou fecha o caixa.",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(DialogLabel("Total em caixa"));
        panel.Children.Add(totalText);
        panel.Children.Add(DialogLabel("Valor"));
        panel.Children.Add(amountBox);
        panel.Children.Add(DialogLabel("Motivo"));
        panel.Children.Add(reasonBox);
        panel.Children.Add(supply);
        panel.Children.Add(withdrawal);
        panel.Children.Add(close);
        panel.Children.Add(DialogLabel("Movimentos"));
        panel.Children.Add(list);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private bool ToggleCashRegister()
    {
        return IsCashOpen() ? CloseCashRegister() : OpenCashRegister();
    }

    private bool OpenCashRegister()
    {
        if (IsCashOpen())
        {
            SetStatus("Caixa ja esta aberto. F10 fecha o caixa.");
            return false;
        }

        var openingCash = ShowCashOpeningDialog();
        if (openingCash is null)
        {
            SetStatus("Abertura de caixa cancelada.");
            return false;
        }

        _cashTotal = openingCash.Value;
        CashMovements.Add(new CashMovement
        {
            Type = "ABERTURA",
            Amount = openingCash.Value,
            Reason = "Dinheiro vivo inicial",
            User = _currentUser,
            When = DateTime.Now
        });
        SaveStore();
        RefreshTotals();
        SetStatus($"Caixa aberto com dinheiro vivo inicial de {Money(openingCash.Value)}. F10 fecha o caixa.");
        return true;
    }

    private decimal? ShowCashOpeningDialog()
    {
        decimal? result = null;
        var dialog = CreateDialog("Abrir caixa", 470, 410);
        dialog.ResizeMode = ResizeMode.NoResize;

        var operatorBox = new TextBox
        {
            Text = CurrentUser is { } current && IsCashUser(current) ? StaffNumber(current) : "",
            Margin = new Thickness(0, 4, 0, 8)
        };
        if (string.IsNullOrWhiteSpace(operatorBox.Text) && CurrentUser is { } currentByName && IsCashUser(currentByName))
        {
            operatorBox.Text = currentByName.Name;
        }

        var passwordBox = new PasswordBox
        {
            Height = 34,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var amountBox = new TextBox
        {
            Text = Math.Max(0, _cashTotal).ToString("N2", Brazil),
            Margin = new Thickness(0, 4, 0, 8)
        };
        var message = new TextBlock
        {
            Text = "Informe quanto dinheiro vivo existe no restaurante agora.",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var error = new TextBlock
        {
            Foreground = RedText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        var open = DialogButton("Abrir caixa", "#08A99B");
        open.HorizontalAlignment = HorizontalAlignment.Stretch;
        open.Width = double.NaN;

        void Confirm()
        {
            var user = FindAuthenticatedUser(operatorBox.Text, passwordBox.Password, IsCashUser);
            if (user is null)
            {
                error.Text = "Operador de caixa, senha ou permissao invalidos.";
                passwordBox.Clear();
                if (string.IsNullOrWhiteSpace(operatorBox.Text))
                {
                    operatorBox.Focus();
                    operatorBox.SelectAll();
                    return;
                }

                passwordBox.Focus();
                return;
            }

            var amount = ParseMoney(amountBox.Text, -1);
            if (amount < 0)
            {
                error.Text = "Informe um valor valido para o dinheiro vivo.";
                amountBox.Focus();
                amountBox.SelectAll();
                return;
            }

            _currentUser = user.Name;
            result = amount;
            dialog.Close();
        }

        open.Click += (_, _) => Confirm();
        operatorBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                passwordBox.Focus();
                e.Handled = true;
            }
        };
        passwordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                amountBox.Focus();
                amountBox.SelectAll();
                e.Handled = true;
            }
        };
        amountBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Confirm();
                e.Handled = true;
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(message);
        panel.Children.Add(DialogLabel("Operador do caixa"));
        panel.Children.Add(operatorBox);
        panel.Children.Add(DialogLabel("Senha"));
        panel.Children.Add(passwordBox);
        panel.Children.Add(DialogLabel("Dinheiro vivo inicial"));
        panel.Children.Add(amountBox);
        panel.Children.Add(error);
        panel.Children.Add(open);
        dialog.Content = panel;
        dialog.Loaded += (_, _) =>
        {
            operatorBox.Focus();
            operatorBox.SelectAll();
        };
        dialog.ShowDialog();
        return result;
    }

    private bool CloseCashRegister()
    {
        if (!RequirePermission(IsCashUser, "Fechar caixa"))
        {
            return false;
        }

        if (!IsCashOpen())
        {
            SetStatus("Caixa ja esta fechado. F10 abre o caixa.");
            return false;
        }

        SaveActiveTicketToCurrentBoard();
        var pending = GetPendingCashBoards();
        if (pending.Count > 0)
        {
            ShowPendingCashCloseDialog(pending);
            SetStatus($"Fechamento bloqueado: {pending.Count} conta(s) pendente(s).");
            return false;
        }

        var closing = ShowProfessionalCashClosingDialog();
        if (closing is null)
        {
            SetStatus("Fechamento de caixa cancelado.");
            return false;
        }

        _cashTotal = closing.CountedCash;
        CashMovements.Add(new CashMovement
        {
            Type = "FECHAMENTO",
            Amount = closing.Difference,
            Reason = $"Contado {Money(closing.CountedCash)} | esperado {Money(closing.ExpectedCash)} | diferenca {Money(closing.Difference)} | {closing.Notes}",
            User = _currentUser,
            When = closing.When,
            Closing = closing
        });
        var report = BuildCashClosingReport();
        var path = WriteReportFile("fechamento-caixa", report);
        var printed = TryPrintTextToDefaultPrinter(report, "Fechamento de caixa", compact: true);
        SaveStore();
        RefreshTotals();
        SetStatus(printed
            ? $"Caixa fechado e resumo impresso. Fechamento gerado: {path}"
            : $"Caixa fechado. Fechamento gerado: {path}. Impressora padrao indisponivel.");
        return true;
    }

    private bool IsCashOpen()
    {
        var lastState = CashMovements
            .Where(item => item.Type is "ABERTURA" or "FECHAMENTO")
            .OrderByDescending(item => item.When)
            .FirstOrDefault();
        return lastState is null || lastState.Type != "FECHAMENTO";
    }

    private List<TableTile> GetPendingCashBoards()
    {
        return Tables
            .Concat(DeliveryTiles)
            .Where(IsCashPendingBoard)
            .OrderBy(tile => tile.Kind)
            .ThenBy(tile => tile.Number)
            .ToList();
    }

    private static bool IsCashPendingBoard(TableTile board)
    {
        var total = board.Lines.Sum(line => line.Total);
        if (total <= 0 && board.Total > 0)
        {
            total = board.Total;
        }

        var paid = board.Payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0, total - paid);
        if (balance > 0.009m || board.Lines.Count > 0 || board.Payments.Count > 0)
        {
            return true;
        }

        return board.Kind == "MESA"
               && board.Status is not ("LIVRE" or "FINALIZADO" or "ENTREGUE" or "CANCELADO");
    }

    private void ShowPendingCashCloseDialog(IReadOnlyList<TableTile> pending)
    {
        var dialog = CreateDialog("Fechamento bloqueado", 620, 430);
        var list = new ListBox
        {
            Height = 230,
            ItemsSource = pending.Select(PendingCashBoardDisplay).ToList(),
            Margin = new Thickness(0, 8, 0, 12)
        };
        var ok = DialogButton("Entendi", "#0B3A52");
        ok.HorizontalAlignment = HorizontalAlignment.Stretch;
        ok.Click += (_, _) => dialog.Close();

        var panel = DialogPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "Nao da para fechar o caixa enquanto existir mesa, ficha ou pedido com movimento pendente.",
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(DialogLabel("Pendencias"));
        panel.Children.Add(list);
        panel.Children.Add(ok);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private static string PendingCashBoardDisplay(TableTile board)
    {
        var total = board.Lines.Sum(line => line.Total);
        if (total <= 0 && board.Total > 0)
        {
            total = board.Total;
        }

        var paid = board.Payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0, total - paid);
        var customer = string.IsNullOrWhiteSpace(board.CustomerName) ? "" : $"  {board.CustomerName}";
        return $"{BoardKindLabel(board)} {board.Number}  {board.Status}{customer}  Saldo {Money(balance)}";
    }

    private void ShowTransferDialog()
    {
        if (BlockIFoodDeliveryEdit("transferir pedido"))
        {
            return;
        }

        if (!RequirePermission(user => user.IsMaster || user.CanTransfer, "Transferencia de comanda"))
        {
            return;
        }

        SaveActiveTicketToCurrentBoard();
        var current = CurrentBoard;
        var transferBoards = GetTransferBoards();
        var sources = transferBoards
            .Where(board => !IsIFoodDeliveryBoard(board) && !HasReceivedPayment(board) && (board.Lines.Count > 0 || board.Payments.Count > 0))
            .ToList();
        if (sources.Count == 0)
        {
            ShowNoTransferSourceDialog();
            return;
        }

        var selectedSourceIndex = current is null ? -1 : sources.FindIndex(board => ReferenceEquals(board, current));
        if (selectedSourceIndex < 0)
        {
            selectedSourceIndex = 0;
        }

        var dialog = CreateDialog("Mover ou juntar comandas", 980, 620);
        var transferBoardTemplate = (DataTemplate)FindResource("TransferBoardTemplate");
        var sourceList = new ListBox
        {
            ItemsSource = sources,
            ItemTemplate = transferBoardTemplate,
            ItemContainerStyle = TransferListBoxItemStyle(),
            Height = 238,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 10, 0, 0)
        };
        var destinationList = new ListBox
        {
            ItemTemplate = transferBoardTemplate,
            ItemContainerStyle = TransferListBoxItemStyle(),
            Height = 238,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 10, 0, 12)
        };
        var selectedDestination = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var sourceDetails = new StackPanel();
        var actionTitle = new TextBlock
        {
            Text = "Selecione origem e destino",
            Foreground = Solid("#071A2C"),
            FontSize = 17,
            FontWeight = FontWeights.Bold
        };
        var actionDescription = new TextBlock
        {
            Text = "Quando o destino estiver livre, o pedido e movido. Quando estiver ocupado, o PDV junta as comandas.",
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var actionCard = new Border
        {
            Background = Solid("#F8FBFE"),
            BorderBrush = Solid("#D6E2EA"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel
            {
                Children =
                {
                    actionTitle,
                    actionDescription
                }
            }
        };
        Button? transferButton = null;

        Border Card(string title, string subtitle, UIElement content, string color)
        {
            var stack = new StackPanel { Margin = new Thickness(16, 14, 16, 16) };
            stack.Children.Add(new TextBlock { Text = title, Foreground = Solid("#071A2C"), FontSize = 16, FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = subtitle, Foreground = Solid("#5B6B7A"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 12) });
            stack.Children.Add(content);
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#D6E2EA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 12),
                Child = new Grid
                {
                    Children =
                    {
                        new Border { Width = 4, Background = Solid(color), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(8, 0, 0, 8) },
                        stack
                    }
                }
            };
        }

        Border MiniStat(string label, string value, string color)
        {
            var stack = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
            stack.Children.Add(new TextBlock { Text = label, Foreground = Solid("#5B6B7A"), FontSize = 11, FontWeight = FontWeights.SemiBold });
            stack.Children.Add(new TextBlock { Text = value, Foreground = Solid(color), FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 0) });
            return new Border
            {
                Background = Solid("#F8FBFE"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 8, 0),
                Child = stack
            };
        }

        Border SourceLineCard(TicketLine line)
        {
            var row = new DockPanel { LastChildFill = true };
            row.Children.Add(new TextBlock
            {
                Text = Money(line.Total),
                Foreground = Solid("#08A99B"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 0, 0)
            });
            DockPanel.SetDock(row.Children[0], Dock.Right);
            row.Children.Add(new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = line.Name, Foreground = Solid("#071A2C"), FontWeight = FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis },
                    new TextBlock { Text = $"{line.Quantity:N0} x {Money(line.UnitPrice)}", Foreground = Solid("#5B6B7A"), FontSize = 11 }
                }
            });
            return new Border
            {
                Background = Solid("#F8FBFE"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 7),
                Child = row
            };
        }

        void RefreshSourceDetails()
        {
            sourceDetails.Children.Clear();
            if (sourceList.SelectedItem is not TableTile source)
            {
                sourceDetails.Children.Add(new TextBlock { Text = "Selecione a comanda de origem.", Foreground = Solid("#5B6B7A") });
                return;
            }

            sourceDetails.Children.Add(new TextBlock { Text = $"{BoardKindLabel(source)} {source.Number}", Foreground = Solid("#071A2C"), FontSize = 24, FontWeight = FontWeights.Bold });
            sourceDetails.Children.Add(new TextBlock { Text = $"{source.DisplayStatus}  |  origem selecionada", Foreground = Solid("#0B3A52"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 0) });
            if (!string.IsNullOrWhiteSpace(source.CustomerName))
            {
                sourceDetails.Children.Add(new TextBlock { Text = $"Cliente: {source.CustomerName}", Foreground = Solid("#5B6B7A"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            }

            var stats = new UniformGrid { Columns = 3, Margin = new Thickness(0, 12, -8, 0) };
            stats.Children.Add(MiniStat("Itens", source.Lines.Count.ToString("N0", Brazil), "#0B3A52"));
            stats.Children.Add(MiniStat("Pagamentos", Money(source.Payments.Sum(payment => payment.Amount)), "#99620D"));
            stats.Children.Add(MiniStat("Total", Money(source.Lines.Sum(line => line.Total)), "#0B3A52"));
            sourceDetails.Children.Add(stats);

            var sourceItems = new StackPanel();
            foreach (var line in source.Lines)
            {
                sourceItems.Children.Add(SourceLineCard(line));
            }

            sourceDetails.Children.Add(new ScrollViewer
            {
                Content = sourceItems,
                Height = 128,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 10, 0, 0)
            });
        }

        void RefreshDestinationText()
        {
            if (destinationList.SelectedItem is not TableTile destination)
            {
                selectedDestination.Text = "Selecione uma mesa/ficha destino.";
                actionTitle.Text = "Selecione o destino";
                actionDescription.Text = "Escolha uma mesa, ficha ou delivery para mover ou juntar.";
                if (transferButton is not null)
                {
                    transferButton.Content = "Mover ou juntar";
                    transferButton.Background = Solid("#0B3A52");
                }
                return;
            }

            var occupied = IsBoardOccupiedForTransfer(destination);
            selectedDestination.Text = $"{BoardKindLabel(destination)} {destination.Number} - {destination.DisplayStatus} - {Money(destination.Total)}";
            actionTitle.Text = occupied ? "Juntar comandas" : "Mover comanda";
            actionDescription.Text = occupied
                ? $"O destino ja tem itens. O PDV vai somar os itens e pagamentos da origem em {BoardKindLabel(destination)} {destination.Number}."
                : $"Destino livre. O PDV vai mover a comanda inteira para {BoardKindLabel(destination)} {destination.Number}.";
            actionCard.Background = Solid(occupied ? "#FFF8E7" : "#F2FBFC");
            actionCard.BorderBrush = Solid(occupied ? "#E8B85B" : "#BBDCE5");
            if (transferButton is not null)
            {
                transferButton.Content = occupied ? "Juntar comandas" : "Mover comanda";
                transferButton.Background = Solid(occupied ? "#99620D" : "#0B3A52");
                transferButton.BorderBrush = transferButton.Background;
            }
        }

        void RefreshDestinationOptions()
        {
            if (sourceList.SelectedItem is not TableTile source)
            {
                destinationList.ItemsSource = null;
                selectedDestination.Text = "Selecione uma origem antes do destino.";
                return;
            }

            var destinations = transferBoards
                .Where(table => !ReferenceEquals(table, source)
                    && !HasReceivedPayment(table)
                    && table.Status is not ("FINALIZADO" or "ENTREGUE" or "CANCELADO"))
                .ToList();
            destinationList.ItemsSource = destinations;
            destinationList.SelectedIndex = destinations.Count > 0 ? 0 : -1;
            if (destinations.Count == 0)
            {
                selectedDestination.Text = "Nenhum destino disponivel para esta transferencia.";
            }
            else
            {
                RefreshDestinationText();
            }
        }

        sourceList.SelectionChanged += (_, _) =>
        {
            RefreshSourceDetails();
            RefreshDestinationOptions();
        };
        destinationList.SelectionChanged += (_, _) => RefreshDestinationText();

        transferButton = DialogButton("Mover ou juntar", "#0B3A52");
        transferButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        transferButton.Width = double.NaN;
        transferButton.IsDefault = true;
        transferButton.Click += (_, _) =>
        {
            if (sourceList.SelectedItem is not TableTile source || destinationList.SelectedItem is not TableTile destination)
            {
                SetStatus("Selecione origem e destino para transferir.");
                return;
            }

            TransferFullCommand(source, destination);
            dialog.Close();
        };
        destinationList.PreviewKeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter)
            {
                return;
            }

            transferButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            args.Handled = true;
        };
        sourceList.PreviewKeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter)
            {
                return;
            }

            destinationList.Focus();
            args.Handled = true;
        };

        var sourcePanel = new StackPanel();
        sourcePanel.Children.Add(new TextBlock { Text = "Escolha qual comanda vai sair do lugar atual.", Foreground = Solid("#5B6B7A"), TextWrapping = TextWrapping.Wrap });
        sourcePanel.Children.Add(sourceList);
        sourcePanel.Children.Add(new Border { Height = 1, Background = Solid("#CAD6E2"), Margin = new Thickness(0, 10, 0, 10) });
        sourcePanel.Children.Add(sourceDetails);
        var destinationPanel = new StackPanel();
        destinationPanel.Children.Add(selectedDestination);
        destinationPanel.Children.Add(actionCard);
        destinationPanel.Children.Add(destinationList);
        destinationPanel.Children.Add(transferButton);

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        var left = new StackPanel();
        left.Children.Add(Card("Origem", "Selecione a comanda completa que sera transferida.", sourcePanel, "#0B3A52"));
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);
        var right = new StackPanel();
        right.Children.Add(Card("Destino", "Escolha destino livre para mover ou ocupado para juntar.", destinationPanel, "#0B3A52"));
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        dialog.Content = grid;
        sourceList.SelectedIndex = selectedSourceIndex;
        RefreshSourceDetails();
        RefreshDestinationOptions();
        dialog.Loaded += (_, _) => sourceList.Focus();
        dialog.ShowDialog();
    }

    private List<TableTile> GetTransferBoards()
    {
        return Tables
            .Concat(DeliveryTiles)
            .Where(board => board.Kind is "MESA" or "BALCAO" or "DELIVERY")
            .ToList();
    }

    private void ShowNoTransferSourceDialog()
    {
        var dialog = CreateDialog("Transferencia de comanda completa", 520, 260);
        var panel = DialogPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "Nao ha comanda/ficha aberta com itens para transferir.",
            Foreground = Solid("#071A2C"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Abra uma mesa/ficha, inclua pelo menos um produto e depois use F6 Transferir Comanda.",
            Foreground = Solid("#5B6B7A"),
            Margin = new Thickness(0, 10, 0, 18),
            TextWrapping = TextWrapping.Wrap
        });
        var ok = DialogButton("Entendi", "#0B3A52");
        ok.HorizontalAlignment = HorizontalAlignment.Stretch;
        ok.Width = double.NaN;
        ok.Click += (_, _) => dialog.Close();
        panel.Children.Add(ok);
        dialog.Content = panel;
        dialog.ShowDialog();
        SetStatus("Nenhuma comanda aberta com itens para transferir.");
    }

    private static Style TransferListBoxItemStyle()
    {
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 0, 0, 8)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
        return style;
    }

    private void TransferFullCommand(TableTile source, TableTile destination)
    {
        var sourceHadContent = IsBoardOccupiedForTransfer(source);
        var destinationHadContent = IsBoardOccupiedForTransfer(destination);
        if (!sourceHadContent)
        {
            SetStatus("Comanda vazia para transferir.");
            return;
        }

        if (HasReceivedPayment(source) || HasReceivedPayment(destination))
        {
            SetStatus("Comanda ja recebida/finalizada nao pode participar da transferencia.");
            return;
        }

        var movedLines = source.Lines.Select(CloneLine).ToList();
        var movedPayments = source.Payments.Select(ClonePayment).ToList();
        destination.Lines.AddRange(movedLines);
        destination.Payments.AddRange(movedPayments);
        destination.Total = destination.Lines.Sum(line => line.Total);
        destination.Status = destination.Kind switch
        {
            "BALCAO" => "ABERTO",
            "DELIVERY" => "PREPARO",
            "KDS" => "RECEBIDO",
            _ => "OCUPADA"
        };
        destination.Waiter = destination.Waiter == 0 ? source.Waiter : destination.Waiter;
        destination.People = destinationHadContent
            ? Math.Max(1, destination.People) + Math.Max(0, source.People)
            : Math.Max(1, source.People);
        if (string.IsNullOrWhiteSpace(destination.CustomerName))
        {
            destination.CustomerName = source.CustomerName;
            destination.CustomerCpf = source.CustomerCpf;
            destination.Phone = source.Phone;
            destination.Address = source.Address;
            destination.District = source.District;
            destination.Notes = source.Notes;
        }
        if (!destination.ChargesEnabled)
        {
            destination.ChargesEnabled = source.ChargesEnabled;
            destination.CouvertAmount = source.CouvertAmount;
            destination.ServicePercent = source.ServicePercent;
        }

        var originLabel = $"{BoardKindLabel(source)} {source.Number}";
        source.Lines.Clear();
        source.Payments.Clear();
        source.Total = 0m;
        source.CustomerName = "";
        source.CustomerCpf = "";
        source.Phone = "";
        source.Address = "";
        source.District = "";
        source.Notes = "";
        source.People = 1;
        source.ChargesEnabled = false;
        source.CouvertAmount = 0m;
        source.ServicePercent = 10m;
        source.Status = source.Kind switch
        {
            "DELIVERY" => "NOVO",
            "KDS" => "RECEBIDO",
            _ => "LIVRE"
        };

        RefreshBoardForMode();
        var destinationIndex = BoardTiles.IndexOf(destination);
        SelectTable(destinationIndex >= 0 ? destinationIndex : 0, saveCurrent: false);
        RefreshTotals();
        SaveStore();
        SetStatus(destinationHadContent
            ? $"{originLabel} juntada com {BoardKindLabel(destination)} {destination.Number}."
            : $"{originLabel} transferida para {BoardKindLabel(destination)} {destination.Number}.");
    }

    private static bool IsBoardOccupiedForTransfer(TableTile board)
    {
        return board.Lines.Count > 0 || board.Payments.Count > 0;
    }

    private void ShowDiscountDialog()
    {
        if (!RequirePermission(user => user.IsMaster || user.CanDiscount, "Desconto"))
        {
            return;
        }

        var total = TicketLines.Sum(line => line.Total);
        if (total <= 0)
        {
            SetStatus("Comanda sem valor para desconto.");
            return;
        }

        var dialog = CreateDialog("Desconto autorizado", 390, 280);
        var amountBox = new TextBox { Text = "5,00" };
        var reasonBox = new TextBox { Text = "Desconto gerente" };
        var message = new TextBlock { Text = $"Total atual: {Money(total)}", Foreground = AmberText, FontWeight = FontWeights.SemiBold };
        var apply = DialogButton("Aplicar desconto", "#A11D1D");
        apply.Click += (_, _) =>
        {
            var amount = ParseMoney(amountBox.Text, 0);
            if (amount <= 0 || amount >= total)
            {
                message.Text = "Valor invalido.";
                message.Foreground = RedText;
                return;
            }

            if (!ShowOperatorPasswordDialog(
                    "Autorizacao do gerente",
                    "Para aplicar desconto, informe a conta e senha do gerente.",
                    "Autorizar desconto",
                    IsManagerUser,
                    out var manager)
                || manager is null)
            {
                message.Text = "Desconto cancelado. Somente gerente pode autorizar.";
                message.Foreground = RedText;
                return;
            }

            TicketLines.Add(new TicketLine
            {
                Code = "DESC",
                Name = string.IsNullOrWhiteSpace(reasonBox.Text) ? "DESCONTO" : reasonBox.Text.Trim().ToUpperInvariant(),
                Quantity = 1,
                UnitPrice = -amount,
                Sector = "CAIXA"
            });
            SaveActiveTicketToCurrentBoard();
            RefreshTotals();
            SaveStore();
            SetStatus($"Desconto aplicado: {Money(amount)} por {manager.Name}.");
            dialog.Close();
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogLabel("Valor"));
        panel.Children.Add(amountBox);
        panel.Children.Add(DialogLabel("Motivo"));
        panel.Children.Add(reasonBox);
        panel.Children.Add(message);
        panel.Children.Add(apply);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void ShowClientDialog()
    {
        var dialog = CreateDialog("Cadastro de clientes", 820, 560);
        var customersList = new ListBox
        {
            ItemsSource = Customers,
            DisplayMemberPath = nameof(CustomerRecord.Display),
            Width = 300,
            Height = 370
        };
        var cpfBox = new TextBox();
        var nameBox = new TextBox();
        var phoneBox = new TextBox();
        var addressBox = new TextBox { Height = 64, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        var districtBox = new TextBox();
        var notesBox = new TextBox { Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };

        void LoadCustomer(CustomerRecord customer)
        {
            cpfBox.Text = customer.Cpf;
            nameBox.Text = customer.Name;
            phoneBox.Text = customer.Phone;
            addressBox.Text = customer.Address;
            districtBox.Text = customer.District;
            notesBox.Text = customer.Notes;
        }

        customersList.SelectionChanged += (_, _) =>
        {
            if (customersList.SelectedItem is CustomerRecord customer)
            {
                LoadCustomer(customer);
            }
        };

        var newButton = DialogButton("Novo cliente", "#0B3A52");
        newButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        newButton.Width = double.NaN;
        newButton.Click += (_, _) =>
        {
            cpfBox.Text = "";
            nameBox.Text = "";
            phoneBox.Text = "";
            addressBox.Text = "";
            districtBox.Text = "";
            notesBox.Text = "";
            customersList.SelectedItem = null;
            nameBox.Focus();
        };

        var save = DialogButton("Salvar cliente", "#08A99B");
        save.HorizontalAlignment = HorizontalAlignment.Stretch;
        save.Width = double.NaN;
        save.Click += (_, _) =>
        {
            var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "CLIENTE" : nameBox.Text.Trim().ToUpperInvariant();
            var customer = UpsertCustomerRecord(cpfBox.Text.Trim(), name, phoneBox.Text.Trim(), addressBox.Text.Trim(), districtBox.Text.Trim(), notesBox.Text.Trim());
            customersList.Items.Refresh();
            customersList.SelectedItem = customer;
            SaveStore();
            SetStatus($"Cliente salvo: {customer.Name}");
        };

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = DialogPanel();
        left.Children.Add(DialogLabel("Clientes cadastrados"));
        left.Children.Add(customersList);
        left.Children.Add(newButton);
        grid.Children.Add(left);

        var form = DialogPanel();
        form.Children.Add(TwoColumnFields(("CPF/CNPJ", cpfBox), ("Telefone", phoneBox)));
        form.Children.Add(DialogLabel("Nome"));
        form.Children.Add(nameBox);
        form.Children.Add(DialogLabel("Endereco"));
        form.Children.Add(addressBox);
        form.Children.Add(DialogLabel("Bairro / referencia"));
        form.Children.Add(districtBox);
        form.Children.Add(DialogLabel("Observacao"));
        form.Children.Add(notesBox);
        form.Children.Add(save);
        Grid.SetColumn(form, 1);
        grid.Children.Add(form);
        dialog.Content = grid;
        if (Customers.Count > 0)
        {
            customersList.SelectedIndex = 0;
        }
        dialog.ShowDialog();
    }

    private CustomerRecord? ShowCustomerPickerDialog()
    {
        if (Customers.Count == 0)
        {
            SetStatus("Nenhum cliente cadastrado. Use Cadastro > Clientes primeiro.");
            return null;
        }

        CustomerRecord? selectedCustomer = null;
        var dialog = CreateDialog("Incluir cliente no delivery", 560, 500);
        var queryBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
        var list = new ListBox
        {
            DisplayMemberPath = nameof(CustomerRecord.Display),
            Height = 330
        };

        void Refresh()
        {
            var query = queryBox.Text.Trim();
            var filtered = Customers
                .Where(customer => string.IsNullOrWhiteSpace(query)
                    || customer.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || customer.Phone.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || customer.Cpf.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || customer.Address.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || customer.District.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(customer => customer.Name)
                .ToList();
            list.ItemsSource = filtered;
            if (filtered.Count > 0)
            {
                list.SelectedIndex = 0;
            }
        }

        void Confirm()
        {
            if (list.SelectedItem is not CustomerRecord customer)
            {
                return;
            }

            selectedCustomer = customer;
            dialog.Close();
        }

        var include = DialogButton("Incluir no delivery", "#08A99B");
        include.HorizontalAlignment = HorizontalAlignment.Stretch;
        include.Width = double.NaN;
        include.Click += (_, _) => Confirm();
        queryBox.TextChanged += (_, _) => Refresh();
        list.MouseDoubleClick += (_, _) => Confirm();
        dialog.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Confirm();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogLabel("Buscar por nome, CPF, telefone, endereco ou bairro"));
        panel.Children.Add(queryBox);
        panel.Children.Add(list);
        panel.Children.Add(include);
        dialog.Content = panel;
        Refresh();
        queryBox.Focus();
        dialog.ShowDialog();
        return selectedCustomer;
    }

    private CustomerRecord? FindCustomerRecord(string cpf, string phone, string name)
    {
        cpf = NormalizeLookup(cpf);
        phone = NormalizeLookup(phone);
        name = name.Trim();
        return Customers.FirstOrDefault(customer =>
                   !string.IsNullOrWhiteSpace(cpf) && NormalizeLookup(customer.Cpf) == cpf)
               ?? Customers.FirstOrDefault(customer =>
                   !string.IsNullOrWhiteSpace(phone) && NormalizeLookup(customer.Phone) == phone)
               ?? Customers.FirstOrDefault(customer =>
                   !string.IsNullOrWhiteSpace(name) && string.Equals(customer.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private CustomerRecord UpsertCustomerRecord(string cpf, string name, string phone, string address, string district, string notes)
    {
        var customer = FindCustomerRecord(cpf, phone, name);
        if (customer is null)
        {
            customer = new CustomerRecord();
            Customers.Add(customer);
        }

        customer.Cpf = cpf.Trim();
        customer.Name = string.IsNullOrWhiteSpace(name) ? "CLIENTE" : name.Trim().ToUpperInvariant();
        customer.Phone = phone.Trim();
        customer.Address = address.Trim();
        customer.District = district.Trim();
        customer.Notes = notes.Trim();
        return customer;
    }

    private static string NormalizeLookup(string value)
    {
        return new string((value ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private void ShowPeopleDialog()
    {
        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        var dialog = CreateDialog("Quantidade de pessoas", 360, 220);
        var peopleBox = new TextBox { Text = Math.Max(1, board.People).ToString(Brazil) };
        var save = DialogButton("Salvar", "#08A99B");
        save.Click += (_, _) =>
        {
            board.People = Math.Max(1, ParseInt(peopleBox.Text, 1));
            SaveStore();
            SetStatus($"{board.Number}: {board.People} pessoa(s)");
            dialog.Close();
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogLabel("Pessoas na mesa/comanda"));
        panel.Children.Add(peopleBox);
        panel.Children.Add(save);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void ShowStaffDialog()
    {
        if (!RequirePermission(IsManagerUser, "Cadastro de equipe"))
        {
            return;
        }

        var dialog = CreateDialog("Garcons e operadores de caixa", 720, 500);
        var staffList = new ListBox { DisplayMemberPath = nameof(UserAccount.Display), Width = 290 };
        var nameBox = new TextBox();
        var numberBox = new TextBox();
        var passwordBox = new PasswordBox { Height = 34 };
        var roleBox = new ComboBox
        {
            ItemsSource = new[] { "GARCOM", "CAIXA", "GERENTE" },
            SelectedIndex = 0,
            MinHeight = 34
        };
        var status = new TextBlock
        {
            Foreground = GreenText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        List<UserAccount> StaffUsers()
        {
            return Users
                .Where(user => user.Role is "GARCOM" or "CAIXA" or "GERENTE" || (!user.IsMaster && !string.Equals(user.Role, "MASTER", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(user => user.Role)
                .ThenBy(user => user.Name)
                .ToList();
        }

        void RefreshStaffList(UserAccount? selected = null)
        {
            var staff = StaffUsers();
            staffList.ItemsSource = staff;
            if (selected is not null && staff.Contains(selected))
            {
                staffList.SelectedItem = selected;
            }
            else if (staff.Count > 0 && staffList.SelectedIndex < 0)
            {
                staffList.SelectedIndex = 0;
            }
        }

        void ClearForm()
        {
            staffList.SelectedIndex = -1;
            nameBox.Text = "";
            numberBox.Text = GetNextStaffNumber().ToString(Brazil);
            passwordBox.Clear();
            roleBox.SelectedIndex = 0;
            nameBox.Focus();
        }

        void LoadStaff(UserAccount user)
        {
            nameBox.Text = user.Name;
            numberBox.Text = StaffNumber(user);
            passwordBox.Password = string.IsNullOrWhiteSpace(user.PinHash) ? user.Pin : "";
            passwordBox.ToolTip = string.IsNullOrWhiteSpace(user.PinHash)
                ? "PIN antigo. Ao salvar, ele sera protegido com hash."
                : "Senha protegida. Deixe em branco para manter a senha atual.";
            roleBox.SelectedItem = user.Role is "CAIXA" or "GERENTE" ? user.Role : "GARCOM";
        }

        staffList.SelectionChanged += (_, _) =>
        {
            if (staffList.SelectedItem is UserAccount user)
            {
                LoadStaff(user);
            }
        };

        var newButton = DialogButton("Novo cadastro", "#0B3A52");
        newButton.Click += (_, _) => ClearForm();

        var saveButton = DialogButton("Salvar equipe", "#08A99B");
        saveButton.Click += (_, _) =>
        {
            var name = nameBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(name))
            {
                status.Foreground = RedText;
                status.Text = "Informe o nome.";
                nameBox.Focus();
                return;
            }

            var role = roleBox.SelectedItem?.ToString() ?? "GARCOM";
            var staffNumber = NormalizeStaffNumber(numberBox.Text);
            var password = passwordBox.Password.Trim();
            if (string.IsNullOrWhiteSpace(staffNumber))
            {
                status.Foreground = RedText;
                status.Text = "Informe o numero.";
                numberBox.Focus();
                return;
            }

            var selectedUser = staffList.SelectedItem as UserAccount;
            if (string.IsNullOrWhiteSpace(password) && selectedUser is null)
            {
                status.Foreground = RedText;
                status.Text = "Informe a senha.";
                passwordBox.Focus();
                return;
            }

            var duplicatedNumber = Users.FirstOrDefault(item =>
                !ReferenceEquals(item, selectedUser)
                && StaffNumber(item) == staffNumber
                && item.Role == role);
            if (duplicatedNumber is not null && !string.Equals(duplicatedNumber.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                status.Foreground = RedText;
                status.Text = $"Numero {staffNumber} ja esta em uso por {duplicatedNumber.Name}.";
                numberBox.Focus();
                numberBox.SelectAll();
                return;
            }

            var user = Users.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                user = new UserAccount();
                Users.Add(user);
            }

            user.Name = name;
            user.EmployeeNumber = staffNumber;
            user.Role = role;
            user.IsMaster = false;
            user.CanTransfer = role is "GARCOM" or "GERENTE";
            user.CanCash = role is "CAIXA" or "GERENTE";
            user.CanCancel = role is "CAIXA" or "GERENTE";
            user.CanDiscount = role is "CAIXA" or "GERENTE";
            user.CanReports = role == "GERENTE";
            user.CanManageProducts = role == "GERENTE";
            if (!string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(user.PinHash))
            {
                SetUserPassword(user, string.IsNullOrWhiteSpace(password) ? staffNumber : password);
            }
            NormalizeRolePermissions(user);
            RefreshStaffList(user);
            SaveStore();
            status.Foreground = GreenText;
            status.Text = role == "CAIXA"
                ? $"Operador de caixa salvo: {user.Name}"
                : role == "GERENTE"
                    ? $"Gerente salvo: {user.Name}"
                    : $"Garcom salvo: {user.Name}";
            SetStatus(status.Text);
        };

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel();
        left.Children.Add(new TextBlock
        {
            Text = "Equipe cadastrada",
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8)
        });
        left.Children.Add(staffList);
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var form = DialogPanel();
        form.Children.Add(DialogLabel("Nome"));
        form.Children.Add(nameBox);
        form.Children.Add(DialogLabel("Numero"));
        form.Children.Add(numberBox);
        form.Children.Add(DialogLabel("Senha"));
        form.Children.Add(passwordBox);
        form.Children.Add(DialogLabel("Funcao"));
        form.Children.Add(roleBox);
        form.Children.Add(DialogHint("O numero identifica garcom/operador na comanda e no comprovante. A senha libera login e operacoes do caixa."));
        var buttons = new UniformGrid { Columns = 2, Margin = new Thickness(0, 8, 0, 0) };
        buttons.Children.Add(newButton);
        buttons.Children.Add(saveButton);
        form.Children.Add(buttons);
        form.Children.Add(status);
        Grid.SetColumn(form, 1);
        grid.Children.Add(form);

        dialog.Content = grid;
        RefreshStaffList();
        if (staffList.SelectedItem is not UserAccount)
        {
            ClearForm();
        }
        dialog.ShowDialog();
    }

    private int GetNextStaffNumber()
    {
        return Users
            .Where(user => user.Role is "GARCOM" or "CAIXA" or "GERENTE")
            .Select(user => int.TryParse(StaffNumber(user), NumberStyles.Integer, Brazil, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private static string StaffNumber(UserAccount user)
    {
        return !string.IsNullOrWhiteSpace(user.EmployeeNumber)
            ? NormalizeStaffNumber(user.EmployeeNumber)
            : NormalizeStaffNumber(user.Pin);
    }

    private static string NormalizeStaffNumber(string value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return "";
        }

        return int.TryParse(digits, NumberStyles.Integer, Brazil, out var parsed)
            ? parsed.ToString(Brazil)
            : digits;
    }

    private void ShowCardapioDialog()
    {
        if (!RequirePermission(CanManageSettings, "Cardapio digital"))
        {
            return;
        }

        var dialog = CreateDialog("Cardapio com QR Code", 920, 700);
        var linkBox = new TextBox
        {
            MinHeight = 42,
            IsReadOnly = true,
            Background = Solid("#F7FAFD"),
            FontSize = 15,
            Padding = new Thickness(10, 8, 10, 8)
        };

        var statusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#5B6B7A"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        var linkPreview = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = GreenText,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var qrHost = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Width = 238,
            Height = 238,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var activeProducts = Products
            .Where(product => product.Active)
            .OrderBy(product => product.Category)
            .ThenBy(product => product.Name)
            .ThenBy(product => product.Code)
            .ToList();
        var discountEnabledBox = new CheckBox
        {
            Content = "Mostrar cupom no cardapio do cliente",
            IsChecked = _appSettings.PublicMenuDiscountConfigured && _appSettings.PublicMenuDiscountEnabled,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var discountCodeBox = new TextBox
        {
            Text = NormalizePublicMenuDiscountCode(_appSettings.PublicMenuDiscountCode),
            CharacterCasing = System.Windows.Controls.CharacterCasing.Upper,
            MinHeight = 42,
            FontSize = 15,
            Padding = new Thickness(10, 8, 10, 8)
        };
        var discountAmountBox = new TextBox
        {
            Text = Math.Max(0, _appSettings.PublicMenuDiscountAmount).ToString("N2", Brazil),
            MinHeight = 42,
            FontSize = 15,
            Padding = new Thickness(10, 8, 10, 8)
        };
        var discountDescriptionBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(_appSettings.PublicMenuDiscountDescription)
                ? "Apresente este cupom no atendimento para receber o desconto."
                : _appSettings.PublicMenuDiscountDescription.Trim(),
            MinHeight = 70,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var loyaltyEnabledBox = new CheckBox
        {
            Content = "Mostrar fidelidade no cardapio",
            IsChecked = _appSettings.PublicMenuLoyaltyConfigured && _appSettings.PublicMenuLoyaltyEnabled,
            Margin = new Thickness(0, 10, 0, 8)
        };
        var loyaltyGoalBox = new TextBox
        {
            Text = Math.Max(1, _appSettings.PublicMenuLoyaltyGoal).ToString(Brazil),
            MinHeight = 42,
            FontSize = 15,
            Padding = new Thickness(10, 8, 10, 8)
        };
        var loyaltyMinimumBox = new TextBox
        {
            Text = Math.Max(0, _appSettings.PublicMenuLoyaltyMinimumOrder).ToString("N2", Brazil),
            MinHeight = 42,
            FontSize = 15,
            Padding = new Thickness(10, 8, 10, 8)
        };
        var discountStatus = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#5B6B7A"),
            Margin = new Thickness(0, 10, 0, 0)
        };
        var discountPreview = BorderCard();
        discountPreview.Margin = new Thickness(0, 12, 0, 0);

        TextBlock ProductCell(string text, double fontSize = 12, FontWeight? weight = null, string color = "#071A2C")
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = weight ?? FontWeights.Normal,
                Foreground = Solid(color),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        var productRows = new StackPanel();
        foreach (var product in activeProducts.Take(80))
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var code = ProductCell(string.IsNullOrWhiteSpace(product.Code) ? "-" : product.Code, 11, FontWeights.SemiBold, "#5B6B7A");
            var name = ProductCell(product.Name, 13, FontWeights.SemiBold);
            var price = ProductCell(Money(product.Price), 12, FontWeights.SemiBold, "#08A99B");
            var stock = ProductCell(product.StockQuantity.ToString("N0", Brazil), 11, FontWeights.SemiBold, product.StockQuantity < 0 ? "#A11D1D" : "#5B6B7A");

            row.Children.Add(code);
            Grid.SetColumn(name, 1);
            row.Children.Add(name);
            Grid.SetColumn(price, 2);
            row.Children.Add(price);
            Grid.SetColumn(stock, 3);
            row.Children.Add(stock);

            productRows.Children.Add(new Border
            {
                Background = Solid("#F7FAFD"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Child = row
            });
        }

        if (activeProducts.Count == 0)
        {
            productRows.Children.Add(new TextBlock
            {
                Text = "Nenhum produto ativo no cadastro.",
                Foreground = Solid("#5B6B7A"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });
        }

        string RefreshGeneratedLink(bool save)
        {
            var baseUrl = DefaultPublicMenuBaseUrl;
            _appSettings.PublicMenuBaseUrl = baseUrl;
            var slug = EnsurePublicMenuSlug();
            var menuUrl = BuildPublicMenuUrl(slug);
            _profile.MenuPublicUrl = menuUrl;
            linkBox.Text = menuUrl;

            if (save)
            {
                _suppressPublicMenuQueue = true;
                try
                {
                    SaveAppSettings();
                    SaveRestaurantProfile();
                    SaveStore();
                }
                finally
                {
                    _suppressPublicMenuQueue = false;
                }
            }

            return menuUrl;
        }

        bool TryGetPublicMenuUrl(out string menuUrl)
        {
            menuUrl = RefreshGeneratedLink(save: true);
            if (IsValidPublicMenuUrl(menuUrl))
            {
                return true;
            }

            statusText.Text = "Nao foi possivel gerar o link do cardapio.";
            statusText.Foreground = RedText;
            return false;
        }

        void RefreshPreview()
        {
            var menuUrl = RefreshGeneratedLink(save: false);
            if (!IsValidPublicMenuUrl(menuUrl))
            {
                qrHost.Child = new TextBlock
                {
                    Text = "Nao foi possivel gerar o QR Code.",
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Solid("#5B6B7A"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                linkPreview.Text = "";
                statusText.Text = "O cliente escaneia o QR Code e abre o cardapio usando qualquer internet do celular.";
                statusText.Foreground = Solid("#5B6B7A");
                return;
            }

            var qrSource = TryCreateQrBitmap(menuUrl, 8);
            qrHost.Child = qrSource is null
                ? new TextBlock
                {
                    Text = "Nao foi possivel gerar o QR Code para esse link.",
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Foreground = RedText,
                    VerticalAlignment = VerticalAlignment.Center
                }
                : new System.Windows.Controls.Image
                {
                    Source = qrSource,
                    Width = 210,
                    Height = 210,
                    Stretch = Stretch.Uniform,
                    ToolTip = menuUrl
                };
            linkPreview.Text = menuUrl;
            var publishText = _appSettings.LastPublicMenuPublishAt.HasValue
                ? $"Ultima publicacao: {_appSettings.LastPublicMenuPublishAt.Value:dd/MM HH:mm}."
                : "Ainda nao publicado nesta maquina.";
            statusText.Text = $"{publishText} O link e gerado pelo Balcao Livre e os dados publicados vao para o Supabase.";
            statusText.Foreground = GreenText;
        }

        void RefreshDiscountPreview()
        {
            var code = NormalizePublicMenuDiscountCode(discountCodeBox.Text);
            var amount = Math.Max(0, ParseMoney(discountAmountBox.Text, _appSettings.PublicMenuDiscountAmount));
            var description = string.IsNullOrWhiteSpace(discountDescriptionBox.Text)
                ? "Apresente este cupom no atendimento para receber o desconto."
                : discountDescriptionBox.Text.Trim();
            var loyaltyGoal = Math.Max(1, ParseInt(loyaltyGoalBox.Text, _appSettings.PublicMenuLoyaltyGoal <= 0 ? 20 : _appSettings.PublicMenuLoyaltyGoal));
            var loyaltyMinimum = Math.Max(0, ParseMoney(loyaltyMinimumBox.Text, _appSettings.PublicMenuLoyaltyMinimumOrder));
            discountPreview.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = discountEnabledBox.IsChecked == true ? $"Cupom {code}" : "Cupom oculto",
                        Foreground = Solid("#071A2C"),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold
                    },
                    new TextBlock
                    {
                        Text = discountEnabledBox.IsChecked == true ? $"{Money(amount)} de desconto" : "O cliente nao vai ver cupom ativo.",
                        Foreground = GreenText,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 4, 0, 0)
                    },
                    new TextBlock
                    {
                        Text = description,
                        Foreground = Solid("#5B6B7A"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 6, 0, 0)
                    },
                    new Border { Height = 1, Background = Solid("#CAD6E2"), Margin = new Thickness(0, 12, 0, 10) },
                    new TextBlock
                    {
                        Text = loyaltyEnabledBox.IsChecked == true
                            ? $"Fidelidade: {loyaltyGoal:N0} ponto(s), pedido minimo {Money(loyaltyMinimum)}."
                            : "Fidelidade oculta no cardapio.",
                        Foreground = Solid("#071A2C"),
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };
        }

        bool SaveDiscountSettings()
        {
            var code = NormalizePublicMenuDiscountCode(discountCodeBox.Text);
            var amount = Math.Max(0, ParseMoney(discountAmountBox.Text, -1));
            if (amount < 0)
            {
                discountStatus.Text = "Informe um valor de desconto valido.";
                discountStatus.Foreground = RedText;
                return false;
            }

            var goal = Math.Max(1, ParseInt(loyaltyGoalBox.Text, -1));
            if (goal <= 0)
            {
                discountStatus.Text = "Informe uma meta de fidelidade valida.";
                discountStatus.Foreground = RedText;
                return false;
            }

            var minimum = Math.Max(0, ParseMoney(loyaltyMinimumBox.Text, -1));
            if (minimum < 0)
            {
                discountStatus.Text = "Informe um pedido minimo valido.";
                discountStatus.Foreground = RedText;
                return false;
            }

            _appSettings.PublicMenuDiscountEnabled = discountEnabledBox.IsChecked == true;
            _appSettings.PublicMenuDiscountConfigured = _appSettings.PublicMenuDiscountEnabled;
            _appSettings.PublicMenuDiscountCode = code;
            _appSettings.PublicMenuDiscountAmount = amount;
            _appSettings.PublicMenuDiscountDescription = string.IsNullOrWhiteSpace(discountDescriptionBox.Text)
                ? "Apresente este cupom no atendimento para receber o desconto."
                : discountDescriptionBox.Text.Trim();
            _appSettings.PublicMenuLoyaltyEnabled = loyaltyEnabledBox.IsChecked == true;
            _appSettings.PublicMenuLoyaltyConfigured = _appSettings.PublicMenuLoyaltyEnabled;
            _appSettings.PublicMenuLoyaltyGoal = goal;
            _appSettings.PublicMenuLoyaltyMinimumOrder = minimum;
            SaveAppSettings();
            QueuePublicMenuPublish();
            RefreshDiscountPreview();
            RefreshPreview();
            discountStatus.Text = "Descontos salvos. O cardapio atualiza automaticamente.";
            discountStatus.Foreground = Solid("#0B3A52");
            return true;
        }

        var publish = DialogButton("Publicar/atualizar", "#08A99B");
        publish.Visibility = Visibility.Collapsed;
        var print = DialogButton("Imprimir", "#0B3A52");
        var copy = DialogButton("Copiar", "#5B6B7A");
        var open = DialogButton("Abrir cardapio", "#0B3A52");
        var saveDiscounts = DialogButton("Salvar descontos", "#0B3A52");

        async Task PublishCurrentMenuAsync(bool automatic)
        {
            if (!TryGetPublicMenuUrl(out _))
            {
                return;
            }

            publish.IsEnabled = false;
            statusText.Text = automatic
                ? "Publicando cardapio automaticamente..."
                : "Publicando cardapio no Supabase...";
            statusText.Foreground = Solid("#5B6B7A");
            try
            {
                var result = await PublishGeneratedPublicMenuAsync(silent: false);
                if (result.Ok)
                {
                    RefreshPreview();
                    statusText.Text = $"Publicado: {result.ItemsPublished:N0} produto(s). Link pronto para o QR.";
                    statusText.Foreground = GreenText;
                    return;
                }

                statusText.Text = string.IsNullOrWhiteSpace(result.Message)
                    ? "Nao foi possivel publicar o cardapio."
                    : result.Message.Trim();
                statusText.Foreground = RedText;
            }
            finally
            {
                publish.IsEnabled = true;
            }
        }

        saveDiscounts.Click += (_, _) => SaveDiscountSettings();

        print.Click += (_, _) =>
        {
            if (!TryGetPublicMenuUrl(out var menuUrl))
            {
                return;
            }

            if (PrintMenuQrWithWindowsDialog(menuUrl))
            {
                SetStatus("Impressao do cardapio enviada.");
                return;
            }

            SetStatus("Impressao do cardapio cancelada.");
        };

        copy.Click += (_, _) =>
        {
            if (!TryGetPublicMenuUrl(out var menuUrl))
            {
                return;
            }

            System.Windows.Clipboard.SetText(menuUrl);
            SetStatus("Link publico do cardapio copiado.");
        };

        open.Click += (_, _) =>
        {
            if (!TryGetPublicMenuUrl(out var menuUrl))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(menuUrl) { UseShellExecute = true });
        };

        var left = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        left.Children.Add(SectionTitle("Cardapio online"));
        left.Children.Add(new TextBlock
        {
            Text = "O link e fixo do Balcao Livre. Os produtos abaixo saem direto do cadastro e estoque da loja.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });
        left.Children.Add(DialogField("Link do cliente", linkBox));
        left.Children.Add(statusText);
        left.Children.Add(linkPreview);
        left.Children.Add(new TextBlock
        {
            Text = $"{activeProducts.Count:N0} produto(s) ativo(s) no sistema da loja",
            FontWeight = FontWeights.Bold,
            Foreground = Solid("#071A2C"),
            Margin = new Thickness(0, 16, 0, 6)
        });
        left.Children.Add(new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new ScrollViewer
            {
                Height = 210,
                Content = productRows,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            }
        });
        left.Children.Add(new TextBlock
        {
            Text = "Texto para o cliente: abra a camera do celular, aponte para o QR Code e veja o cardapio atualizado com produtos, precos e disponibilidade.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#536879"),
            Margin = new Thickness(0, 16, 0, 0)
        });

        var actions = new WrapPanel { Margin = new Thickness(0, 16, 0, 0) };
        foreach (var button in new[] { print, copy, open })
        {
            button.Margin = new Thickness(0, 0, 8, 8);
            button.MinWidth = 152;
            actions.Children.Add(button);
        }

        left.Children.Add(actions);

        var preview = new StackPanel();
        preview.Children.Add(new TextBlock
        {
            Text = ResolveWaiterRestaurantName(),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = Solid("#071A2C"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        });
        preview.Children.Add(new TextBlock
        {
            Text = "CARDAPIO DIGITAL",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = GreenText,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });
        preview.Children.Add(qrHost);
        preview.Children.Add(new TextBlock
        {
            Text = "Aponte a camera do celular para o QR Code",
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeights.Bold,
            Foreground = Solid("#071A2C"),
            Margin = new Thickness(0, 14, 0, 4)
        });
        preview.Children.Add(new TextBlock
        {
            Text = "Veja o cardapio, escolha os itens e chame a equipe para pedir.",
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Foreground = Solid("#5B6B7A")
        });

        var previewCard = new Border
        {
            Background = Solid("#F7FAFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18),
            Child = preview
        };

        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        layout.Children.Add(left);
        Grid.SetColumn(previewCard, 1);
        layout.Children.Add(previewCard);

        var publishPanel = DialogPanel();
        publishPanel.Children.Add(layout);
        publishPanel.Children.Add(DialogHint("Mudancas em nome, logo, produtos, preco, estoque e descontos entram em fila automatica e sao publicadas em tempo real quando o app salva esses dados."));

        var discountFields = BorderCard();
        discountFields.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Configure o que aparece na aba Descontos do cardapio publico.",
                    Foreground = Solid("#5B6B7A"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                discountEnabledBox,
                TwoColumnFields(("Cupom", discountCodeBox), ("Valor do desconto", discountAmountBox)),
                DialogField("Texto exibido para o cliente", discountDescriptionBox),
                loyaltyEnabledBox,
                TwoColumnFields(("Meta de pontos", loyaltyGoalBox), ("Pedido minimo para pontuar", loyaltyMinimumBox))
            }
        };

        var discountActions = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        foreach (var button in new[] { saveDiscounts })
        {
            button.Margin = new Thickness(0, 0, 8, 8);
            button.MinWidth = 180;
            discountActions.Children.Add(button);
        }

        var discountPanel = DialogPanel();
        discountPanel.Children.Add(SectionTitle("Descontos"));
        discountPanel.Children.Add(discountFields);
        discountPanel.Children.Add(discountPreview);
        discountPanel.Children.Add(discountActions);
        discountPanel.Children.Add(discountStatus);
        discountPanel.Children.Add(DialogHint("O cliente ve o cupom e a fidelidade no cardapio. A loja decide no caixa como aplicar o desconto no pedido."));

        var tabs = new System.Windows.Controls.TabControl();
        tabs.Items.Add(new System.Windows.Controls.TabItem
        {
            Header = "QR e produtos",
            Content = new ScrollViewer
            {
                Content = publishPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            }
        });
        tabs.Items.Add(new System.Windows.Controls.TabItem
        {
            Header = "Descontos",
            Content = new ScrollViewer
            {
                Content = discountPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            }
        });
        dialog.Content = tabs;
        RefreshGeneratedLink(save: true);
        RefreshPreview();
        RefreshDiscountPreview();
        dialog.Loaded += async (_, _) => await PublishCurrentMenuAsync(automatic: true);
        dialog.ShowDialog();
    }

    private static bool IsValidPublicMenuUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePublicMenuUrl(string value)
    {
        value = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        if (!value.Contains("://", StringComparison.Ordinal) && value.Contains('.'))
        {
            value = $"https://{value}";
        }

        return value;
    }

    private static string NormalizePublicMenuBaseUrl(string value)
    {
        var normalized = NormalizePublicMenuUrl(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = DefaultPublicMenuBaseUrl;
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.Trim('/');
            if (path.Equals("cardapio", StringComparison.OrdinalIgnoreCase))
            {
                normalized = new UriBuilder(uri) { Path = "", Query = "", Fragment = "" }.Uri.GetLeftPart(UriPartial.Authority);
            }

            if (IsPublicMenuApexHost(uri) || IsPublicMenuHost(uri))
            {
                normalized = $"https://{PublicMenuHost}";
            }
        }

        return normalized.TrimEnd('/');
    }

    private static bool IsPublicMenuApexHost(Uri uri)
    {
        return uri.Host.Equals(PublicMenuApexHost, StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals($"www.{PublicMenuApexHost}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicMenuHost(Uri uri)
    {
        return uri.Host.Equals(PublicMenuHost, StringComparison.OrdinalIgnoreCase);
    }

    private string EnsurePublicMenuSlug()
    {
        var baseSlug = BuildPublicMenuBaseSlug();
        var slug = NormalizePublicMenuSlug(_profile.MenuSlug);
        if (IsNumberedPublicMenuSlugForBase(slug, baseSlug))
        {
            return slug;
        }

        slug = baseSlug;
        _profile.MenuSlug = slug;
        return slug;
    }

    private string BuildPublicMenuBaseSlug()
    {
        var nameSlug = NormalizePublicMenuSlug(ResolveWaiterRestaurantName());
        return string.IsNullOrWhiteSpace(nameSlug) ? "loja" : nameSlug;
    }

    private static bool IsNumberedPublicMenuSlugForBase(string slug, string baseSlug)
    {
        return slug.Length > baseSlug.Length + 4
            && slug[3] == '-'
            && slug[..3].All(char.IsDigit)
            && slug[4..].Equals(baseSlug, StringComparison.OrdinalIgnoreCase);
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
        if (slug.Length > 72)
        {
            slug = slug[..72].Trim('-');
        }

        return slug;
    }

    private static string NormalizePublicMenuDiscountCode(string value)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
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
            else if ((ch == '-' || ch == '_') && sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        var code = sb.ToString().Trim('-');
        if (code.Length > 24)
        {
            code = code[..24].Trim('-');
        }

        return string.IsNullOrWhiteSpace(code) ? "EXCLUSIVO4" : code;
    }

    private string BuildPublicMenuUrl(string slug)
    {
        var baseUrl = NormalizePublicMenuBaseUrl(_appSettings.PublicMenuBaseUrl);
        if (!IsValidPublicMenuUrl(baseUrl))
        {
            baseUrl = DefaultPublicMenuBaseUrl;
        }

        var normalizedSlug = NormalizePublicMenuSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            normalizedSlug = EnsurePublicMenuSlug();
        }

        _appSettings.PublicMenuBaseUrl = baseUrl;
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            && (IsPublicMenuApexHost(uri) || IsPublicMenuHost(uri)))
        {
            return $"{uri.Scheme}://{PublicMenuHost}/{BuildPublicMenuPath(normalizedSlug)}";
        }

        return $"{baseUrl.TrimEnd('/')}/cardapio/{BuildPublicMenuPath(normalizedSlug)}";
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

    private void QueuePublicMenuPublish()
    {
        if (_suppressPublicMenuQueue
            || !IsLoaded
            || !_appSettings.PublicMenuAutoPublish
            || !_appSettings.AdminSyncEnabled
            || string.IsNullOrWhiteSpace(_appSettings.ActivationKey)
            || Products.Count == 0)
        {
            return;
        }

        EnsurePublicMenuSlug();
        _profile.MenuPublicUrl = BuildPublicMenuUrl(_profile.MenuSlug);
        var signature = BuildPublicMenuSignature();
        if (signature == _lastPublishedPublicMenuSignature || signature == _pendingPublicMenuSignature)
        {
            return;
        }

        _pendingPublicMenuSignature = signature;
        _publicMenuPublishTimer.Stop();
        _publicMenuPublishTimer.Start();
    }

    private string BuildPublicMenuSignature()
    {
        var sb = new StringBuilder();
        sb.AppendLine(_profile.MenuSlug);
        sb.AppendLine(_profile.MenuPublicUrl);
        sb.AppendLine(_profile.BusinessName);
        sb.AppendLine(_profile.LegalName);
        sb.AppendLine(_profile.Phone);
        sb.AppendLine(_profile.Address);
        sb.AppendLine(_profile.City);
        sb.AppendLine(_profile.State);
        sb.AppendLine(GetPublicMenuLogoStamp());
        sb.AppendLine(GetPublicMenuImageStamp(_profile.LocalCoverPath));
        sb.AppendLine(_appSettings.PublicMenuStoreOpen.ToString());
        sb.AppendLine(_appSettings.PublicMenuWaitMinMinutes.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(_appSettings.PublicMenuWaitMaxMinutes.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(_appSettings.PublicMenuDiscountConfigured.ToString());
        sb.AppendLine(_appSettings.PublicMenuDiscountEnabled.ToString());
        sb.AppendLine(_appSettings.PublicMenuDiscountCode);
        sb.AppendLine(_appSettings.PublicMenuDiscountAmount.ToString("0.00", CultureInfo.InvariantCulture));
        sb.AppendLine(_appSettings.PublicMenuDiscountDescription);
        sb.AppendLine(_appSettings.PublicMenuLoyaltyConfigured.ToString());
        sb.AppendLine(_appSettings.PublicMenuLoyaltyEnabled.ToString());
        sb.AppendLine(_appSettings.PublicMenuLoyaltyGoal.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(_appSettings.PublicMenuLoyaltyMinimumOrder.ToString("0.00", CultureInfo.InvariantCulture));

        foreach (var product in Products
            .Where(product => product.Active)
            .OrderBy(product => product.Category)
            .ThenBy(product => product.Name)
            .ThenBy(product => product.Code))
        {
            sb.Append(product.Code).Append('|')
                .Append(product.Name).Append('|')
                .Append(product.Category).Append('|')
                .Append(product.Price.ToString("0.00", CultureInfo.InvariantCulture)).Append('|')
                .Append(product.StockQuantity.ToString("0.###", CultureInfo.InvariantCulture)).Append('|')
                .Append(GetPublicMenuImageStamp(product.ImagePath)).Append('|')
                .Append(product.Active).AppendLine();
        }

        return Sha256Hex(sb.ToString());
    }

    private string GetPublicMenuLogoStamp()
    {
        return GetPublicMenuImageStamp(_profile.LocalLogoPath);
    }

    private string GetPublicMenuImageStamp(string imagePath)
    {
        var logoPath = (imagePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return "";
        }

        if (IsValidPublicMenuUrl(logoPath))
        {
            return logoPath;
        }

        try
        {
            var info = new FileInfo(logoPath);
            return info.Exists ? $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc:O}" : logoPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException or FileFormatException)
        {
            return logoPath;
        }
    }

    private async Task<AdminPublicMenuPublishResult> PublishGeneratedPublicMenuAsync(bool silent)
    {
        if (_publicMenuPublishRunning)
        {
            return AdminPublicMenuPublishResult.Fail("Publicacao do cardapio ja esta em andamento.");
        }

        var endpoint = BuildAdminApiUri("/api/app/menu/publish");
        if (endpoint is null)
        {
            return AdminPublicMenuPublishResult.Fail("URL do admin invalida.");
        }

        if (!_appSettings.AdminSyncEnabled || string.IsNullOrWhiteSpace(_appSettings.ActivationKey))
        {
            return AdminPublicMenuPublishResult.Fail("Ative a licenca/admin antes de publicar o cardapio online.");
        }

        _publicMenuPublishRunning = true;
        try
        {
            _suppressPublicMenuQueue = true;
            try
            {
                EnsurePublicMenuSlug();
                _profile.MenuPublicUrl = BuildPublicMenuUrl(_profile.MenuSlug);
                SaveAppSettings();
                SaveRestaurantProfile();
                SaveStore();
            }
            finally
            {
                _suppressPublicMenuQueue = false;
            }

            var payload = BuildPublicMenuPublishPayload();
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AdminPublicMenuPublishResult>(body, JsonOptions)
                ?? new AdminPublicMenuPublishResult
                {
                    Ok = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Cardapio publicado." : body,
                    Slug = payload.Slug,
                    PublicUrl = payload.PublicUrl,
                    ItemsPublished = payload.Items.Count
                };

            if (!response.IsSuccessStatusCode)
            {
                result.Ok = false;
                result.Message = NormalizePublicMenuPublishError(result.Message, response.StatusCode);
            }

            if (result.Ok)
            {
                var publishedSlug = NormalizePublicMenuSlug(result.Slug);
                if (!string.IsNullOrWhiteSpace(publishedSlug))
                {
                    _profile.MenuSlug = publishedSlug;
                }

                _profile.MenuPublicUrl = IsValidPublicMenuUrl(result.PublicUrl)
                    ? result.PublicUrl
                    : BuildPublicMenuUrl(_profile.MenuSlug);
                _lastPublishedPublicMenuSignature = BuildPublicMenuSignature();
                _pendingPublicMenuSignature = "";
                _appSettings.LastPublicMenuPublishAt = DateTime.Now;
                SaveAppSettings();
                SaveRestaurantProfile();
                SaveStore();
                if (!silent)
                {
                    SetStatus($"Cardapio publicado: {result.ItemsPublished:N0} produto(s).");
                }
            }
            else if (!silent)
            {
                SetStatus($"Falha ao publicar cardapio: {result.Message}");
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException or IOException)
        {
            var result = AdminPublicMenuPublishResult.Fail($"Falha ao publicar cardapio: {ex.Message}");
            if (!silent)
            {
                SetStatus(result.Message);
            }

            return result;
        }
        finally
        {
            _publicMenuPublishRunning = false;
        }
    }

    private static string NormalizePublicMenuPublishError(string? message, System.Net.HttpStatusCode statusCode)
    {
        var text = (message ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return $"Admin retornou HTTP {(int)statusCode}.";
        }

        if (text.Contains("Licenca sem permissao para publicar cardapio", StringComparison.OrdinalIgnoreCase))
        {
            return "A chave desta loja nao esta ativa no painel de licencas. Gere ou ative a licenca no admin e tente publicar de novo.";
        }

        if (statusCode == System.Net.HttpStatusCode.Unauthorized &&
            text.Contains("sem permissao", StringComparison.OrdinalIgnoreCase))
        {
            return "A chave desta loja foi recusada pelo painel de licencas. Confirme se a chave esta ativa e vinculada a este computador.";
        }

        return text;
    }

    private AdminPublicMenuPublishPayload BuildPublicMenuPublishPayload()
    {
        var payload = FillAdminPayload(new AdminPublicMenuPublishPayload(), "public_menu.publish");
        payload.Slug = EnsurePublicMenuSlug();
        payload.PublicUrl = BuildPublicMenuUrl(payload.Slug);
        payload.ThemeColor = "#0f766e";
        payload.Description = "Cardapio digital atualizado pelo Balcao Livre PDV.";
        payload.StoreOpen = _appSettings.PublicMenuStoreOpen;
        payload.WaitMinMinutes = Math.Max(1, _appSettings.PublicMenuWaitMinMinutes);
        payload.WaitMaxMinutes = Math.Max(payload.WaitMinMinutes, _appSettings.PublicMenuWaitMaxMinutes);
        var publishDiscount = _appSettings.PublicMenuDiscountConfigured && _appSettings.PublicMenuDiscountEnabled;
        var publishLoyalty = _appSettings.PublicMenuLoyaltyConfigured && _appSettings.PublicMenuLoyaltyEnabled;
        payload.DiscountEnabled = publishDiscount;
        payload.DiscountCode = NormalizePublicMenuDiscountCode(_appSettings.PublicMenuDiscountCode);
        payload.DiscountAmount = Math.Max(0, _appSettings.PublicMenuDiscountAmount);
        payload.DiscountDescription = string.IsNullOrWhiteSpace(_appSettings.PublicMenuDiscountDescription)
            ? "Apresente este cupom no atendimento para receber o desconto."
            : _appSettings.PublicMenuDiscountDescription.Trim();
        payload.LoyaltyEnabled = publishLoyalty;
        payload.LoyaltyGoal = Math.Max(1, _appSettings.PublicMenuLoyaltyGoal);
        payload.LoyaltyMinimumOrder = Math.Max(0, _appSettings.PublicMenuLoyaltyMinimumOrder);
        FillPublicMenuLogo(payload);
        FillPublicMenuCoverImage(payload);

        var index = 0;
        payload.Items = Products
            .Where(product => product.Active)
            .OrderBy(product => product.Category)
            .ThenBy(product => product.Name)
            .ThenBy(product => product.Code)
            .Select(product => new AdminPublicMenuProductSnapshot
            {
                Code = product.Code,
                Name = product.Name,
                Category = string.IsNullOrWhiteSpace(product.Category) ? "Cardapio" : product.Category,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsInStock = product.StockQuantity >= 0,
                IsActive = product.Active,
                ImageUrl = BuildPublicMenuProductImageUrl(product),
                SortOrder = index++ * 10
            })
            .ToList();
        return payload;
    }

    private void FillPublicMenuLogo(AdminPublicMenuPublishPayload payload)
    {
        FillPublicMenuImage(
            _profile.LocalLogoPath,
            url => payload.LogoUrl = url,
            (fileName, contentType, base64) =>
            {
                payload.LogoFileName = fileName;
                payload.LogoContentType = contentType;
                payload.LogoBase64 = base64;
            },
            "Public menu logo payload failed");
    }

    private void FillPublicMenuCoverImage(AdminPublicMenuPublishPayload payload)
    {
        FillPublicMenuImage(
            _profile.LocalCoverPath,
            url => payload.CoverImageUrl = url,
            (fileName, contentType, base64) =>
            {
                payload.CoverImageFileName = fileName;
                payload.CoverImageContentType = contentType;
                payload.CoverImageBase64 = base64;
            },
            "Public menu cover payload failed");
    }

    private static string BuildPublicMenuProductImageUrl(ProductTile product)
    {
        return BuildPublicMenuInlineImageUrl(product.ImagePath, $"Public menu product image payload failed ({product.Code})");
    }

    private static string BuildPublicMenuInlineImageUrl(string path, string debugPrefix)
    {
        var imagePath = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return "";
        }

        if (IsValidPublicMenuUrl(imagePath))
        {
            return imagePath;
        }

        try
        {
            var info = new FileInfo(imagePath);
            if (!info.Exists || info.Length <= 0)
            {
                return "";
            }

            var compressed = TryCreateCompressedImageDataUrl(info.FullName);
            if (!string.IsNullOrWhiteSpace(compressed))
            {
                return compressed;
            }

            if (info.Length <= 700_000)
            {
                return $"data:{GetLogoContentType(info.Extension)};base64,{Convert.ToBase64String(File.ReadAllBytes(info.FullName))}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException or FileFormatException)
        {
            Debug.WriteLine($"{debugPrefix}: {ex.Message}");
        }

        return "";
    }

    private static string TryCreateCompressedImageDataUrl(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            BitmapSource source = bitmap;
            const int maxPixel = 640;
            if (source.PixelWidth > maxPixel || source.PixelHeight > maxPixel)
            {
                var scale = Math.Min((double)maxPixel / source.PixelWidth, (double)maxPixel / source.PixelHeight);
                var scaled = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                scaled.Freeze();
                source = scaled;
            }

            byte[] bytes = [];
            foreach (var quality in new[] { 78, 64, 50 })
            {
                using var stream = new MemoryStream();
                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);
                bytes = stream.ToArray();
                if (bytes.Length <= 120_000)
                {
                    break;
                }
            }

            return bytes.Length > 0
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}"
                : "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException or FileFormatException)
        {
            Debug.WriteLine($"Product image compression failed: {ex.Message}");
            return "";
        }
    }

    private static void FillPublicMenuImage(string path, Action<string> setUrl, Action<string, string, string> setFile, string debugPrefix)
    {
        var logoPath = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return;
        }

        if (IsValidPublicMenuUrl(logoPath))
        {
            setUrl(logoPath);
            return;
        }

        try
        {
            var info = new FileInfo(logoPath);
            if (!info.Exists || info.Length <= 0 || info.Length > 2_000_000)
            {
                return;
            }

            setFile(info.Name, GetLogoContentType(info.Extension), Convert.ToBase64String(File.ReadAllBytes(info.FullName)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Debug.WriteLine($"{debugPrefix}: {ex.Message}");
        }
    }

    private static string GetLogoContentType(string extension)
    {
        return extension.TrimStart('.').ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            _ => "image/png"
        };
    }

    private string BuildMenuQrReceiptText(string menuUrl)
    {
        const int width = 32;
        var sb = new StringBuilder();
        var restaurant = ResolveWaiterRestaurantName().ToUpperInvariant();
        var divider = new string('-', width);

        foreach (var line in WrapReceipt(restaurant, width))
        {
            sb.AppendLine(CenterReceipt(line, width));
        }

        if (!string.IsNullOrWhiteSpace(_profile.Cnpj))
        {
            sb.AppendLine(CenterReceipt($"CNPJ: {_profile.Cnpj.Trim()}", width));
        }

        if (!string.IsNullOrWhiteSpace(_profile.Phone))
        {
            sb.AppendLine(CenterReceipt($"TEL: {_profile.Phone.Trim()}", width));
        }

        var location = string.Join(" - ", new[] { _profile.Address, _profile.City, _profile.State }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()));
        foreach (var line in WrapReceipt(location.ToUpperInvariant(), width))
        {
            sb.AppendLine(CenterReceipt(line, width));
        }

        sb.AppendLine(divider);
        sb.AppendLine(CenterReceipt("CARDAPIO DIGITAL", width));
        sb.AppendLine(divider);
        foreach (var line in WrapReceipt("Aponte a camera do celular para o QR Code e veja nosso cardapio.", width))
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();
        foreach (var line in WrapReceipt("Funciona em qualquer internet do celular.", width))
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();
        foreach (var line in WrapReceipt(menuUrl, width))
        {
            sb.AppendLine(line);
        }

        sb.AppendLine(divider);
        sb.AppendLine(CenterReceipt("OBRIGADO PELA PREFERENCIA", width));
        return sb.ToString();
    }

    private bool PrintMenuQrWithWindowsDialog(string menuUrl)
    {
        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
            {
                return false;
            }

            var qr = TryCreateQrBitmap(menuUrl, 14);
            var restaurant = ResolveWaiterRestaurantName();
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                PageWidth = dialog.PrintableAreaWidth,
                PageHeight = dialog.PrintableAreaHeight,
                PagePadding = new Thickness(44),
                ColumnWidth = double.PositiveInfinity,
                TextAlignment = TextAlignment.Center
            };

            document.Blocks.Add(new Paragraph(new Run(restaurant))
            {
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = Solid("#071A2C"),
                Margin = new Thickness(0, 0, 0, 4)
            });
            document.Blocks.Add(new Paragraph(new Run("CARDAPIO DIGITAL"))
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Solid("#0B3A52"),
                Margin = new Thickness(0, 0, 0, 20)
            });

            if (qr is not null)
            {
                document.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image
                {
                    Source = qr,
                    Width = 300,
                    Height = 300,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center
                })
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                });
            }

            document.Blocks.Add(new Paragraph(new Run("Aponte a camera do celular para o QR Code"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Solid("#071A2C"),
                Margin = new Thickness(0, 0, 0, 6)
            });
            document.Blocks.Add(new Paragraph(new Run("Veja o cardapio, escolha os itens e chame a equipe para pedir."))
            {
                FontSize = 15,
                Foreground = Solid("#5B6B7A"),
                Margin = new Thickness(0, 0, 0, 16)
            });
            document.Blocks.Add(new Paragraph(new Run(menuUrl))
            {
                FontSize = 11,
                Foreground = Solid("#5B6B7A"),
                Margin = new Thickness(0, 0, 0, 0)
            });

            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Cardapio QR Code");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Menu QR Windows print failed: {ex}");
            return false;
        }
    }

    private void OpenMenuQrPoster(string menuUrl, bool compact, bool autoPrint)
    {
        var path = Path.Combine(ExportDir, compact ? "cardapio-qr-pos58.html" : "cardapio-qr-cartaz.html");
        File.WriteAllText(path, BuildMenuQrPosterHtml(menuUrl, compact, autoPrint), Encoding.UTF8);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private string BuildMenuQrPosterHtml(string menuUrl, bool compact, bool autoPrint)
    {
        var qrBytes = TryCreateQrPngBytes(menuUrl, compact ? 10 : 14) ?? [];
        var qrData = qrBytes.Length > 0
            ? $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}"
            : "";
        var restaurant = EscapeHtml(ResolveWaiterRestaurantName());
        var cnpj = EscapeHtml(_profile.Cnpj.Trim());
        var phone = EscapeHtml(_profile.Phone.Trim());
        var location = EscapeHtml(string.Join(" - ", new[] { _profile.Address, _profile.City, _profile.State }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())));
        var url = EscapeHtml(menuUrl);
        var productCount = Products.Count(product => product.Active);
        var generatedAt = EscapeHtml(DateTime.Now.ToString("dd/MM/yyyy HH:mm", Brazil));

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"pt-br\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>Cardapio QR Code</title>");
        sb.AppendLine("<style>");
        if (compact)
        {
            sb.AppendLine("@page{size:58mm auto;margin:3mm}body{margin:0;background:#fff;color:#101820;font-family:Arial,sans-serif}.ticket{width:52mm;margin:0 auto;text-align:center}.name{font-size:15px;font-weight:800;margin:2mm 0 1mm}.kicker{font-size:10px;font-weight:800;color:#0f766e;letter-spacing:.4px}.qr{width:38mm;height:38mm;margin:3mm auto 2mm;display:block}.main{font-size:13px;font-weight:800;line-height:1.2;margin:1mm 0}.sub,.meta,.url{font-size:9px;line-height:1.25;color:#3f5261}.url{word-break:break-all;margin-top:2mm}.line{border-top:1px dashed #111;margin:3mm 0}.print{display:none}");
        }
        else
        {
            sb.AppendLine("@page{size:A4;margin:12mm}body{margin:0;background:#eef4f8;color:#101820;font-family:Segoe UI,Arial,sans-serif}.ticket{max-width:720px;margin:0 auto;background:#fff;border:1px solid #d5e2ed;border-radius:14px;padding:34px;text-align:center;box-shadow:0 18px 50px rgba(20,45,70,.12)}.name{font-size:38px;font-weight:850;margin:0 0 8px}.kicker{font-size:16px;font-weight:850;color:#0f766e;letter-spacing:1px}.qr{width:300px;height:300px;margin:28px auto 18px;display:block}.main{font-size:30px;font-weight:850;margin:0 0 8px}.sub{font-size:18px;color:#3f5261;line-height:1.45}.meta{font-size:14px;color:#5d7080;margin-top:18px}.url{font-size:13px;color:#5d7080;word-break:break-all;margin-top:14px}.line{border-top:1px solid #d5e2ed;margin:24px 0}.print{position:fixed;right:18px;top:18px;border:0;border-radius:8px;background:#0f766e;color:#fff;font-weight:800;padding:12px 18px}@media print{body{background:#fff}.ticket{box-shadow:none}.print{display:none}}");
        }

        sb.AppendLine("</style></head><body>");
        if (!compact)
        {
            sb.AppendLine("<button class=\"print\" onclick=\"window.print()\">Imprimir</button>");
        }

        sb.AppendLine("<main class=\"ticket\">");
        sb.AppendLine($"<div class=\"name\">{restaurant}</div>");
        sb.AppendLine("<div class=\"kicker\">CARDAPIO DIGITAL</div>");
        sb.AppendLine("<div class=\"line\"></div>");
        if (!string.IsNullOrWhiteSpace(qrData))
        {
            sb.AppendLine($"<img class=\"qr\" src=\"{qrData}\" alt=\"QR Code do cardapio\">");
        }

        sb.AppendLine("<div class=\"main\">Aponte a camera do celular</div>");
        sb.AppendLine("<div class=\"sub\">Veja o cardapio, escolha os itens e chame a equipe para pedir.</div>");
        sb.AppendLine("<div class=\"line\"></div>");
        if (!string.IsNullOrWhiteSpace(cnpj))
        {
            sb.AppendLine($"<div class=\"meta\">CNPJ: {cnpj}</div>");
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            sb.AppendLine($"<div class=\"meta\">Telefone: {phone}</div>");
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            sb.AppendLine($"<div class=\"meta\">{location}</div>");
        }

        sb.AppendLine($"<div class=\"meta\">{productCount:N0} produtos ativos - gerado em {generatedAt}</div>");
        sb.AppendLine($"<div class=\"url\">{url}</div>");
        sb.AppendLine("</main>");
        if (autoPrint)
        {
            sb.AppendLine("<script>setTimeout(function(){ window.print(); }, 450);</script>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private void ShowReportsDialog()
    {
        if (!RequirePermission(IsManagerUser, "Relatorios"))
        {
            return;
        }

        var dialog = CreateDialog("Relatorios e BI operacional", 1120, 720);
        var shell = new Border
        {
            Background = Solid("#F4F7FA"),
            Padding = new Thickness(18)
        };
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(96) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });

        var periodBox = new ComboBox
        {
            ItemsSource = new[] { "Hoje", "Total" },
            SelectedIndex = 0,
            MinHeight = 36,
            Width = 170,
            Padding = new Thickness(8, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = Solid("#C9D8E7"),
            Margin = new Thickness(8, 0, 0, 0)
        };
        var controls = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#DCE7F1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        var controlsContent = new StackPanel { Orientation = Orientation.Horizontal };
        controlsContent.Children.Add(new TextBlock
        {
            Text = "Periodo do relatorio",
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        controlsContent.Children.Add(periodBox);
        controls.Child = controlsContent;
        root.Children.Add(controls);

        var metricsHost = new ContentControl();
        Grid.SetRow(metricsHost, 1);
        root.Children.Add(metricsHost);

        var bodyHost = new ContentControl();
        Grid.SetRow(bodyHost, 2);
        root.Children.Add(bodyHost);

        string SelectedPeriod() => periodBox.SelectedItem?.ToString() ?? "Hoje";

        void RenderReport()
        {
            var period = SelectedPeriod();
            var openOrders = GetOpenReportOrders();
            var topProducts = GetTopReportProducts(period, 8);
            var lowStock = Products
                .Where(product => product.MinimumStock > 0 && product.StockQuantity <= product.MinimumStock)
                .OrderBy(product => product.StockQuantity - product.MinimumStock)
                .ThenBy(product => product.Name)
                .Take(8)
                .ToList();
            var cashMovements = GetReportCashMovements(period)
                .OrderByDescending(item => item.When)
                .Take(8)
                .ToList();
            var openTotal = openOrders.Sum(tile => tile.Total);
            var soldItems = GetSoldItemsForReportPeriod(period);
            var profitSummary = GetProfitSummaryForReportPeriod(period);
            var showIFoodReport = IsIFoodReportEnabled();
            var ifoodSummary = showIFoodReport ? GetIFoodReportSummary(period) : default;

            root.RowDefinitions[1].Height = showIFoodReport ? new GridLength(164) : new GridLength(86);
            var metrics = new UniformGrid
            {
                Columns = showIFoodReport ? 3 : 5,
                Rows = showIFoodReport ? 2 : 1,
                Margin = new Thickness(0, 4, 0, 0)
            };
            metrics.Children.Add(CreateMetricCard("Em aberto", Money(openTotal), $"{openOrders.Count} comandas/pedidos", "#0B3A52"));
            metrics.Children.Add(CreateMetricCard("Caixa atual", Money(_cashTotal), IsCashOpen() ? "caixa aberto" : "caixa fechado", IsCashOpen() ? "#08A99B" : "#A11D1D"));
            metrics.Children.Add(CreateMetricCard("Itens vendidos", soldItems.ToString("N0", Brazil), $"{topProducts.Count} produtos no ranking - {period}", "#99620D"));
            metrics.Children.Add(CreateMetricCard("Lucro bruto", Money(profitSummary.Profit), $"Margem {profitSummary.Margin:N2}% - {period}", profitSummary.Profit >= 0 ? "#08A99B" : "#A11D1D"));
            if (showIFoodReport)
            {
                metrics.Children.Add(CreateMetricCard("Lucro iFood", Money(ifoodSummary.EstimatedNetProfit), $"Taxa est. {Money(ifoodSummary.EstimatedFee)} - {period}", ifoodSummary.EstimatedNetProfit >= 0 ? "#08A99B" : "#A11D1D"));
            }

            metrics.Children.Add(CreateMetricCard("Estoque baixo", lowStock.Count.ToString("N0", Brazil), "itens abaixo do minimo", lowStock.Count == 0 ? "#08A99B" : "#A11D1D"));
            metricsHost.Content = metrics;

            var body = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(showIFoodReport ? 1.08 : 1.15, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(showIFoodReport ? 0.92 : 0.85, GridUnitType.Star) });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var left = new Grid { Margin = new Thickness(0, 0, 10, 0) };
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.08, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.92, GridUnitType.Star) });
            left.Children.Add(CreateReportSection("Comandas abertas", "Mesas, balcao e delivery com movimento", CreateOrderReportList(openOrders)));

            var topProductsSection = CreateReportSection("Produtos mais vendidos", $"Ranking por quantidade vendida - {period}", CreateProductRanking(topProducts));
            Grid.SetRow(topProductsSection, 1);
            left.Children.Add(topProductsSection);
            body.Children.Add(left);

            var right = new Grid { Margin = new Thickness(10, 0, 0, 0) };
            if (showIFoodReport)
            {
                right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.12, GridUnitType.Star) });
                right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.88, GridUnitType.Star) });
                right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.88, GridUnitType.Star) });
                right.Children.Add(CreateReportSection("iFood", "Taxas estimadas: loja 12%, entrega iFood 23%", CreateIFoodReportList(ifoodSummary)));

                var stockSection = CreateReportSection("Estoque critico", "Produtos no minimo ou abaixo", CreateLowStockList(lowStock));
                Grid.SetRow(stockSection, 1);
                right.Children.Add(stockSection);

                var cashSection = CreateReportSection("Caixa", $"Total atual {Money(_cashTotal)} - {period}", CreateCashMovementList(cashMovements));
                Grid.SetRow(cashSection, 2);
                right.Children.Add(cashSection);
            }
            else
            {
                right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                right.Children.Add(CreateReportSection("Estoque critico", "Produtos no minimo ou abaixo", CreateLowStockList(lowStock)));

                var cashSection = CreateReportSection("Caixa", $"Total atual {Money(_cashTotal)} - {period}", CreateCashMovementList(cashMovements));
                Grid.SetRow(cashSection, 1);
                right.Children.Add(cashSection);
            }
            Grid.SetColumn(right, 1);
            body.Children.Add(right);
            bodyHost.Content = body;
        }

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var openFolder = DialogButton("Abrir pasta", "#0B3A52");
        openFolder.Margin = new Thickness(0, 10, 10, 0);
        openFolder.Click += (_, _) => Process.Start(new ProcessStartInfo(ExportDir) { UseShellExecute = true });
        var print = DialogButton("Imprimir", "#99620D");
        print.Margin = new Thickness(0, 10, 10, 0);
        print.Click += (_, _) =>
        {
            var period = SelectedPeriod();
            var reportText = BuildOperationalReport(period);
            var printed = TryPrintTextToDefaultPrinter(reportText, $"Relatorio operacional {period}", compact: true);
            SetStatus(printed
                ? $"Relatorio {period.ToLowerInvariant()} impresso."
                : "Relatorio nao impresso. Impressora padrao indisponivel.");
        };
        var export = DialogButton("Exportar TXT", "#08A99B");
        export.Margin = new Thickness(0, 10, 0, 0);
        export.Click += (_, _) =>
        {
            var period = SelectedPeriod();
            var reportText = BuildOperationalReport(period);
            var path = WriteReportFile("relatorio-operacional", reportText);
            SetStatus($"Relatorio {period.ToLowerInvariant()} exportado: {path}");
        };
        footer.Children.Add(openFolder);
        footer.Children.Add(print);
        footer.Children.Add(export);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        periodBox.SelectionChanged += (_, _) => RenderReport();
        RenderReport();
        shell.Child = root;
        dialog.Content = shell;
        dialog.ShowDialog();
    }

    private List<TableTile> GetOpenReportOrders()
    {
        return Tables
            .Concat(DeliveryTiles)
            .Where(tile => tile.Total > 0 || tile.Status is not ("LIVRE" or "FINALIZADO" or "ENTREGUE"))
            .OrderByDescending(tile => tile.Total)
            .ThenBy(tile => tile.Kind)
            .ThenBy(tile => tile.Number)
            .ToList();
    }

    private List<ProductTile> GetTopReportProducts(string period, int take)
    {
        if (IsTodayReportPeriod(period))
        {
            var lines = GetClosedReportLines(period);
            return lines
                .GroupBy(line => new { line.Code, line.Name })
                .Select(group =>
                {
                    var source = Products.FirstOrDefault(product => product.Code == group.Key.Code);
                    var quantity = group.Sum(line => line.Quantity);
                    var revenue = group.Sum(line => line.Total);
                    var averagePrice = quantity > 0 ? revenue / quantity : source?.Price ?? 0;
                    return new ProductTile(group.Key.Code, group.Key.Name, source?.Category ?? "", averagePrice)
                    {
                        CostPrice = source?.CostPrice ?? 0,
                        SoldQuantity = quantity,
                        StockQuantity = source?.StockQuantity ?? 0,
                        MinimumStock = source?.MinimumStock ?? 0
                    };
                })
                .Where(product => product.SoldQuantity > 0)
                .OrderByDescending(product => product.SoldQuantity)
                .ThenBy(product => product.Name)
                .Take(take)
                .ToList();
        }

        return Products
            .Where(product => product.SoldQuantity > 0)
            .OrderByDescending(product => product.SoldQuantity)
            .ThenBy(product => product.Name)
            .Take(take)
            .ToList();
    }

    private decimal GetSoldItemsForReportPeriod(string period)
    {
        return IsTodayReportPeriod(period)
            ? GetClosedReportLines(period).Sum(line => line.Quantity)
            : Products.Sum(product => product.SoldQuantity);
    }

    private ProfitSummary GetProfitSummaryForLines(IEnumerable<TicketLine> lines)
    {
        var reportLines = lines.ToList();
        var revenue = reportLines.Sum(line => line.Total);
        var cost = reportLines.Sum(line =>
        {
            var product = Products.FirstOrDefault(item => item.Code == line.Code);
            return (product?.CostPrice ?? 0m) * line.Quantity;
        });
        return ProfitSummary.From(revenue, cost);
    }

    private ProfitSummary GetProfitSummaryForReportPeriod(string period)
    {
        if (IsTodayReportPeriod(period))
        {
            return GetProfitSummaryForLines(GetClosedReportLines(period));
        }

        var totalRevenue = Products.Sum(product => product.Price * product.SoldQuantity);
        var totalCost = Products.Sum(product => product.CostPrice * product.SoldQuantity);
        return ProfitSummary.From(totalRevenue, totalCost);
    }

    private bool IsIFoodReportEnabled()
    {
        var settings = _appSettings.IFood;
        return settings is { Enabled: true, HasCloudConnection: true } || DeliveryTiles.Any(IsIFoodDeliveryBoard);
    }

    private IFoodReportSummary GetIFoodReportSummary(string period)
    {
        var orders = GetIFoodReportOrders(period);
        var validOrders = orders
            .Where(order => NormalizeIFoodBoardStatus(order.Status) is not ("CANCELADO" or "CANCELAMENTO"))
            .ToList();
        var merchantOrders = validOrders.Where(order => !IsIFoodShipment(order.ExternalDeliveredBy)).ToList();
        var shipmentOrders = validOrders.Where(order => IsIFoodShipment(order.ExternalDeliveredBy)).ToList();
        var merchantRevenue = merchantOrders.Sum(IFoodReportOrderRevenue);
        var shipmentRevenue = shipmentOrders.Sum(IFoodReportOrderRevenue);
        var revenue = merchantRevenue + shipmentRevenue;
        var cost = validOrders.Sum(GetBoardProductCost);
        var merchantFee = merchantRevenue * 0.12m;
        var shipmentFee = shipmentRevenue * 0.23m;
        var estimatedFee = merchantFee + shipmentFee;
        var estimatedNetProfit = revenue - cost - estimatedFee;
        var margin = revenue > 0 ? estimatedNetProfit / revenue * 100m : 0m;
        return new IFoodReportSummary(
            orders.Count,
            validOrders.Count,
            merchantOrders.Count,
            shipmentOrders.Count,
            orders.Count(order => NormalizeIFoodBoardStatus(order.Status) is "CANCELADO" or "CANCELAMENTO"),
            orders.Count(IsIFoodDelivered),
            merchantRevenue,
            shipmentRevenue,
            revenue,
            cost,
            merchantFee,
            shipmentFee,
            estimatedFee,
            estimatedNetProfit,
            margin);
    }

    private List<TableTile> GetIFoodReportOrders(string period)
    {
        var orders = DeliveryTiles.Where(IsIFoodDeliveryBoard);
        if (IsTodayReportPeriod(period))
        {
            orders = orders.Where(order => IFoodReportOrderDate(order).Date == DateTime.Today);
        }

        return orders
            .OrderByDescending(IFoodReportOrderDate)
            .ThenBy(order => order.Number)
            .ToList();
    }

    private static DateTime IFoodReportOrderDate(TableTile order)
    {
        return order.LastClosedAt
            ?? order.ExternalDeliveredAt
            ?? order.ExternalCreatedAt
            ?? order.CreatedAt;
    }

    private static decimal IFoodReportOrderRevenue(TableTile order)
    {
        if (order.Total > 0m)
        {
            return order.Total;
        }

        if (order.ClosedLines.Count > 0)
        {
            return order.ClosedLines.Sum(line => line.Total);
        }

        return order.Lines.Sum(line => line.Total);
    }

    private decimal GetBoardProductCost(TableTile order)
    {
        var lines = order.Lines.Count > 0 ? order.Lines : order.ClosedLines;
        return lines.Sum(line =>
        {
            var product = Products.FirstOrDefault(item => item.Code == line.Code);
            return (product?.CostPrice ?? 0m) * line.Quantity;
        });
    }

    private IEnumerable<CashMovement> GetReportCashMovements(string period)
    {
        return IsTodayReportPeriod(period)
            ? CashMovements.Where(item => item.When.Date == DateTime.Today)
            : CashMovements;
    }

    private List<TicketLine> GetClosedReportLines(string period)
    {
        var boards = Tables.Concat(DeliveryTiles);
        if (IsTodayReportPeriod(period))
        {
            boards = boards.Where(board => board.LastClosedAt.HasValue && board.LastClosedAt.Value.Date == DateTime.Today);
        }

        return boards.SelectMany(board => board.ClosedLines).ToList();
    }

    private static bool IsTodayReportPeriod(string period)
    {
        return string.Equals(period, "Hoje", StringComparison.OrdinalIgnoreCase);
    }

    private static Border CreateMetricCard(string title, string value, string detail, string accentColor)
    {
        var root = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#DCE7F1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 10, 10),
            ClipToBounds = true
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new Border { Background = Solid(accentColor) });

        var text = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold
        });
        text.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = Solid("#071A2C"),
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 2, 0, 0)
        });
        text.Children.Add(new TextBlock
        {
            Text = detail,
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        root.Child = grid;
        return root;
    }

    private static Border CreateReportSection(string title, string subtitle, UIElement content)
    {
        var section = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#DCE7F1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid { Margin = new Thickness(16, 14, 16, 14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.Children.Add(new Border
        {
            Background = Solid("#0B3A52"),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 1, 0, 5)
        });
        var headerText = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        headerText.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Solid("#071A2C"),
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        headerText.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(headerText, 1);
        header.Children.Add(headerText);
        grid.Children.Add(header);

        var scroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(0, 0, 4, 0)
        };
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        section.Child = grid;
        return section;
    }

    private UIElement CreateOrderReportList(IReadOnlyCollection<TableTile> orders)
    {
        var panel = new StackPanel();
        if (orders.Count == 0)
        {
            panel.Children.Add(CreateEmptyReportState("Nenhuma comanda aberta."));
            return panel;
        }

        foreach (var order in orders)
        {
            panel.Children.Add(CreateReportRow(
                $"{order.Kind} {order.Number}",
                string.IsNullOrWhiteSpace(order.CustomerName) ? order.Status : $"{order.Status} - {order.CustomerName}",
                Money(order.Total),
                StatusColor(order.Status)));
        }

        return panel;
    }

    private UIElement CreateProductRanking(IReadOnlyList<ProductTile> products)
    {
        var panel = new StackPanel();
        if (products.Count == 0)
        {
            panel.Children.Add(CreateEmptyReportState("Ainda nao ha produtos vendidos."));
            return panel;
        }

        var max = Math.Max(1m, products.Max(product => product.SoldQuantity));
        foreach (var product in products)
        {
            var totalProfit = product.ProfitAmount * product.SoldQuantity;
            panel.Children.Add(CreateProgressReportRow(
                product.Name,
                $"Qtd {product.SoldQuantity:N0}  Compra {product.CostPriceText}  Venda {product.PriceText}  Margem {product.ProfitMargin:N2}%",
                Money(totalProfit),
                (double)(product.SoldQuantity / max),
                totalProfit >= 0 ? "#08A99B" : "#A11D1D"));
        }

        return panel;
    }

    private UIElement CreateLowStockList(IReadOnlyCollection<ProductTile> products)
    {
        var panel = new StackPanel();
        if (products.Count == 0)
        {
            panel.Children.Add(CreateEmptyReportState("Estoque dentro do minimo."));
            return panel;
        }

        foreach (var product in products)
        {
            panel.Children.Add(CreateReportRow(
                product.Name,
                $"Estoque {product.StockQuantity:N0}  Minimo {product.MinimumStock:N0}",
                product.Code,
                "#A11D1D"));
        }

        return panel;
    }

    private UIElement CreateCashMovementList(IReadOnlyCollection<CashMovement> movements)
    {
        var panel = new StackPanel();
        if (movements.Count == 0)
        {
            panel.Children.Add(CreateEmptyReportState("Nenhum movimento de caixa."));
            return panel;
        }

        foreach (var movement in movements)
        {
            panel.Children.Add(CreateReportRow(
                $"{movement.Type} - {movement.User}",
                $"{movement.When:g}  {movement.Reason}",
                Money(movement.Amount),
                movement.Amount < 0 ? "#A11D1D" : "#08A99B"));
        }

        return panel;
    }

    private UIElement CreateIFoodReportList(IFoodReportSummary summary)
    {
        var panel = new StackPanel();
        if (summary.TotalOrders == 0)
        {
            panel.Children.Add(CreateEmptyReportState("Nenhum pedido iFood no periodo."));
            return panel;
        }

        panel.Children.Add(CreateReportRow(
            "Vendas iFood",
            $"{summary.ValidOrders:N0} pedido(s), {summary.DeliveredOrders:N0} entregue(s), {summary.CancelledOrders:N0} cancelado(s)",
            Money(summary.Revenue),
            "#0B3A52"));
        panel.Children.Add(CreateReportRow(
            "Entrega propria / merchant",
            $"{summary.MerchantOrders:N0} pedido(s) x 12%",
            $"-{Money(summary.EstimatedMerchantFee)}",
            "#99620D"));
        panel.Children.Add(CreateReportRow(
            "Entrega iFood",
            $"{summary.IFoodShipmentOrders:N0} pedido(s) x 23%",
            $"-{Money(summary.EstimatedIFoodShipmentFee)}",
            "#A11D1D"));
        panel.Children.Add(CreateReportRow(
            "Lucro liquido estimado",
            $"Custo produto {Money(summary.ProductCost)}  |  Margem {summary.EstimatedMargin:N2}%",
            Money(summary.EstimatedNetProfit),
            summary.EstimatedNetProfit >= 0 ? "#08A99B" : "#A11D1D"));
        return panel;
    }

    private static Border CreateReportRow(string title, string subtitle, string trailing, string accentColor)
    {
        var row = new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#E3EBF2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Margin = new Thickness(0, 0, 0, 7),
            Padding = new Thickness(10, 8, 10, 8)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });

        grid.Children.Add(new Border { Background = Solid(accentColor), CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 1, 10, 1) });

        var text = new StackPanel { Margin = new Thickness(10, 0, 8, 0) };
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        text.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        var right = new TextBlock
        {
            Text = trailing,
            Foreground = Solid("#071A2C"),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        row.Child = grid;
        return row;
    }

    private static Border CreateProgressReportRow(string title, string subtitle, string trailing, double ratio, string accentColor)
    {
        var row = CreateReportRow(title, subtitle, trailing, accentColor);
        if (row.Child is not Grid grid || grid.Children.OfType<StackPanel>().FirstOrDefault() is not { } text)
        {
            return row;
        }

        var track = new Border
        {
            Background = Solid("#EAF8FA"),
            Height = 5,
            CornerRadius = new CornerRadius(3),
            Margin = new Thickness(0, 7, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var bar = new Border
        {
            Background = Solid(accentColor),
            Width = Math.Clamp(ratio, 0.05, 1) * 180,
            CornerRadius = new CornerRadius(3),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        track.Child = bar;
        text.Children.Add(track);
        return row;
    }

    private static Border CreateEmptyReportState(string text)
    {
        return new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#E3EBF2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Solid("#5B6B7A"),
                FontWeight = FontWeights.SemiBold
            }
        };
    }

    private static string StatusColor(string status)
    {
        return status switch
        {
            "LIVRE" or "PRONTO" or "ENTREGUE" or "FINALIZADO" => "#087D73",
            "CONTA" or "AGUARDANDO" or "PREPARO" or "PREPARANDO" or "ROTA" => "#8A5B09",
            _ => "#9E2F27"
        };
    }

    private static string CashMovementLabel(string type)
    {
        return type switch
        {
            "SANGRIA" or "RETIRADA" => "Retirada de caixa",
            "SUPRIMENTO" or "ENTRADA" => "Entrada de caixa",
            "ABERTURA" => "Abertura",
            "FECHAMENTO" => "Fechamento",
            _ => type
        };
    }

    private void ShowBackupDialog()
    {
        if (!RequirePermission(CanManageBackup, "Backup e exportacao"))
        {
            return;
        }

        SaveStore();
        var dialog = CreateDialog("Backup e exportacao", 800, 640);
        var result = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var cloudBackupBox = new CheckBox
        {
            Content = "Backup completo versionado",
            IsChecked = _appSettings.CloudBackupEnabled,
            Margin = new Thickness(0, 0, 0, 6)
        };
        var centralSyncBox = new CheckBox
        {
            Content = "Sync central economico",
            IsChecked = _appSettings.CentralSyncEnabled,
            Margin = new Thickness(0, 0, 0, 6)
        };
        var cloudStatus = new TextBlock();
        var syncStatus = new TextBlock();
        var lastLocalBackupText = new TextBlock();
        var lastBackupText = new TextBlock();
        var lastSyncText = new TextBlock();

        Border BackupCard(string title, string description, params UIElement[] children)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Solid("#071A2C"),
                FontSize = 15,
                FontWeight = FontWeights.Bold
            });
            if (!string.IsNullOrWhiteSpace(description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = description,
                    Foreground = Solid("#5B6B7A"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 10)
                });
            }

            foreach (var child in children)
            {
                stack.Children.Add(child);
            }

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12),
                Child = stack
            };
        }

        Border StatusPill(TextBlock text, string onText, bool enabled)
        {
            text.Text = enabled ? onText : "desligado";
            text.Foreground = enabled ? Solid("#0B3A52") : Solid("#5B6B7A");
            text.FontWeight = FontWeights.Bold;
            return new Border
            {
                Background = enabled ? Solid("#EAF4F8") : Solid("#F1F5F8"),
                BorderBrush = enabled ? Solid("#0B3A52") : Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 8, 0),
                Child = text
            };
        }

        void StyleLastText(TextBlock text)
        {
            text.Foreground = Solid("#5B6B7A");
            text.FontSize = 12;
            text.TextWrapping = TextWrapping.Wrap;
            text.Margin = new Thickness(0, 2, 0, 0);
        }

        StyleLastText(lastBackupText);
        StyleLastText(lastSyncText);
        StyleLastText(lastLocalBackupText);

        void RefreshBackupState()
        {
            cloudStatus.Text = _appSettings.CloudBackupEnabled ? "backup ligado" : "backup desligado";
            cloudStatus.Foreground = _appSettings.CloudBackupEnabled ? Solid("#0B3A52") : Solid("#5B6B7A");
            syncStatus.Text = _appSettings.CentralSyncEnabled ? "sync ligado" : "sync desligado";
            syncStatus.Foreground = _appSettings.CentralSyncEnabled ? Solid("#0B3A52") : Solid("#5B6B7A");
            lastBackupText.Text = _appSettings.LastCloudBackupAt.HasValue
                ? $"Ultimo backup online: {_appSettings.LastCloudBackupAt.Value:dd/MM/yyyy HH:mm}"
                : "Ultimo backup online: ainda nao enviado";
            lastLocalBackupText.Text = _appSettings.LastLocalBackupAt.HasValue
                ? $"Ultimo backup local versionado: {_appSettings.LastLocalBackupAt.Value:dd/MM/yyyy HH:mm}"
                : "Ultimo backup local versionado: ainda nao gerado";
            lastSyncText.Text = _appSettings.LastCentralSyncAt.HasValue
                ? $"Ultimo sync central: {_appSettings.LastCentralSyncAt.Value:dd/MM/yyyy HH:mm}"
                : "Ultimo sync central: ainda nao enviado";
        }

        var saveOnline = DialogButton("Salvar automacao", "#0B3A52");
        saveOnline.Click += (_, _) =>
        {
            _appSettings.CloudBackupEnabled = cloudBackupBox.IsChecked == true;
            _appSettings.CentralSyncEnabled = centralSyncBox.IsChecked == true;
            SaveAppSettings();
            QueueCentralSync("sync.settings.saved", CreateStoreSnapshot(), force: true);
            RefreshBackupState();
            result.Text = "Automacao online salva.";
            SetStatus("Backup automatico e sync central atualizados.");
        };
        var backup = DialogButton("Gerar backup agora", "#0B3A52");
        backup.Click += (_, _) =>
        {
            var path = CreateLocalStoreBackup("manual", force: true);
            QueueCloudBackup(CreateStoreSnapshot(), force: true);
            _appSettings.LastCloudBackupAt = DateTime.Now;
            SaveAppSettings();
            RefreshBackupState();
            result.Text = string.IsNullOrWhiteSpace(path)
                ? "Nao foi possivel gerar o backup local."
                : $"{path}\n{LatestSqlBackupFile}";
            SetStatus(string.IsNullOrWhiteSpace(path)
                ? "Falha ao gerar backup local. Veja se a pasta de dados esta liberada."
                : $"Backup local gerado: {path}. Espelho SQL atualizado.");
        };
        var restore = DialogButton("Restaurar ultimo backup", "#B45309");
        restore.Click += (_, _) =>
        {
            var confirm = System.Windows.MessageBox.Show(
                this,
                "Restaurar o ultimo backup local vai substituir os dados carregados agora. Continuar?",
                "Restaurar backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            if (!TryLoadStoreFromPath(LatestLocalBackupFile, out var backupStore) || backupStore is null || !ApplyStore(backupStore))
            {
                result.Text = "Nao encontrei um backup local valido para restaurar.";
                SetStatus("Restauracao cancelada: backup local invalido ou ausente.");
                return;
            }

            WriteStoreFile(CreateStoreSnapshot());
            RefreshBoardForMode();
            SelectTable(0);
            SelectCategory(0);
            FilterProducts();
            SelectProduct(0);
            RefreshTotals();
            RefreshBackupState();
            result.Text = $"Restaurado de: {LatestLocalBackupFile}";
            SetStatus("Backup local restaurado e arquivo principal regravado com seguranca.");
        };
        var open = DialogButton("Abrir pasta de dados", "#5B6B7A");
        open.Click += (_, _) => Process.Start(new ProcessStartInfo(_dataRoot) { UseShellExecute = true });
        var export = DialogButton("Exportar resumo CSV", "#0B3A52");
        export.Click += (_, _) =>
        {
            var path = Path.Combine(ExportDir, $"produtos-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            var lines = Products.Select(product => $"{product.Code};{product.Name};{product.Category};{product.CostPrice.ToString("N2", Brazil)};{product.Price.ToString("N2", Brazil)};{product.ProfitMargin.ToString("N2", Brazil)};{product.StockQuantity.ToString("N0", Brazil)}");
            File.WriteAllLines(path, new[] { "codigo;nome;grupo;preco_compra;preco_venda;margem_percentual;estoque" }.Concat(lines), Encoding.UTF8);
            result.Text = path;
            SetStatus($"CSV gerado: {path}");
        };

        foreach (var button in new[] { saveOnline, backup, restore, export, open })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }

        var pathBox = new TextBox
        {
            Text = $"{StoreFile}\n{LatestLocalBackupFile}\n{LatestSqlBackupFile}",
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Background = Solid("#F8FBFD"),
            Foreground = Solid("#435466"),
            Height = 64,
            Padding = new Thickness(10)
        };

        var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        statusRow.Children.Add(StatusPill(cloudStatus, "backup ligado", _appSettings.CloudBackupEnabled));
        statusRow.Children.Add(StatusPill(syncStatus, "sync ligado", _appSettings.CentralSyncEnabled));

        var cloudBox = BackupCard(
            "Nuvem",
            "Modo economico: sync envia resumo operacional; backup completo versionado roda a cada 6 horas ou manualmente.",
            cloudBackupBox,
            DialogHint("Salva uma copia completa dos dados para recuperacao."),
            centralSyncBox,
            DialogHint("Mantem clientes, licenca e resumo prontos para web/admin/mobile."),
            lastBackupText,
            lastSyncText);

        var localBox = BackupCard(
            "Dados locais",
            "O PDV grava com arquivo temporario, mantem .bak, atualiza um espelho JSON/SQL e cria copia versionada a cada 30 minutos.",
            pathBox,
            lastLocalBackupText,
            DialogHint("A pasta automaticos guarda as copias para recuperacao manual mesmo depois de reiniciar o computador."));

        var actionsBox = BackupCard(
            "Acoes",
            "Use backup manual antes de formatar, trocar de computador ou fazer alteracao grande.",
            backup,
            restore,
            export,
            open,
            saveOnline,
            result);

        var layout = new Grid { Margin = new Thickness(18) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel();
        header.Children.Add(new TextBlock
        {
            Text = "Protecao dos dados",
            Foreground = Solid("#071A2C"),
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = "Backup local automatico a cada 30 minutos, espelho SQL e copia versionada na nuvem.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 12)
        });
        header.Children.Add(statusRow);
        layout.Children.Add(header);

        var content = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        Grid.SetRow(content, 1);

        var left = new StackPanel();
        left.Children.Add(localBox);
        left.Children.Add(cloudBox);
        content.Children.Add(left);

        Grid.SetColumn(actionsBox, 1);
        actionsBox.Margin = new Thickness(12, 0, 0, 12);
        content.Children.Add(actionsBox);
        layout.Children.Add(content);

        RefreshBackupState();
        dialog.Content = layout;
        dialog.ShowDialog();
    }

    private void DeleteCurrentCommand()
    {
        if (!RequirePermission(user => user.IsMaster || user.CanCancel, "Excluir comanda"))
        {
            return;
        }

        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        TicketLines.Clear();
        Payments.Clear();
        board.Lines.Clear();
        board.Payments.Clear();
        board.ClosedLines.Clear();
        board.ClosedPayments.Clear();
        board.LastClosedAt = null;
        board.LastReceiptPath = "";
        board.Total = 0;
        board.Status = board.Kind switch
        {
            "DELIVERY" => "CANCELADO",
            "KDS" => "CANCELADO",
            "BALCAO" => "LIVRE",
            _ => "LIVRE"
        };
        RefreshTotals();
        SaveStore();
        QueuePublicMenuOrderStatusSync(board);
        SetStatus($"{board.Kind} {board.Number} excluida/cancelada.");
    }

    private void ReopenCurrentCommand(bool requireManagerApproval = false)
    {
        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        var restoredClosedCommand = false;
        var hasActiveCommand = board.Lines.Count > 0 || TicketLines.Count > 0 || board.Payments.Count > 0 || Payments.Count > 0;
        var requiresManagerApproval = requireManagerApproval || !hasActiveCommand;
        UserAccount? approvingManager = null;
        if (requiresManagerApproval && !ShowOperatorPasswordDialog(
                "Autorizacao do gerente",
                "Para reabrir ou liberar comanda, informe a conta e senha do gerente.",
                "Autorizar reabertura",
                IsManagerUser,
                out approvingManager))
        {
            SetStatus("Reabertura cancelada. Somente gerente pode reabrir comanda.");
            return;
        }

        if (!hasActiveCommand && HasReceivedPayment(board))
        {
            TicketLines.Clear();
            Payments.Clear();
            board.Lines.Clear();
            board.Payments.Clear();
            board.Total = 0;
            RefreshTotals();
            SaveStore();
            SetStatus($"{BoardKindLabel(board)} {board.Number} ja foi recebida. Para nova venda, abra uma comanda/ficha vazia.");
            return;
        }

        if (!hasActiveCommand && board.ClosedLines.Count > 0)
        {
            board.Lines = board.ClosedLines.Select(CloneLine).ToList();
            board.Payments = board.ClosedPayments.Select(ClonePayment).ToList();
            board.Total = board.Lines.Sum(line => line.Total);
            LoadActiveTicketFromBoard(board);
            restoredClosedCommand = true;
        }
        else if (!hasActiveCommand)
        {
            TicketLines.Clear();
            Payments.Clear();
            board.Lines.Clear();
            board.Payments.Clear();
            board.Total = 0;
            board.Status = board.Kind switch
            {
                "DELIVERY" => "NOVO",
                "KDS" => "RECEBIDO",
                _ => "LIVRE"
            };
            RefreshTotals();
            SaveStore();
            QueuePublicMenuOrderStatusSync(board);
            SetStatus($"{board.Kind} {board.Number} liberada.");
            return;
        }

        board.Status = board.Kind switch
        {
            "DELIVERY" => "PREPARO",
            "KDS" => "RECEBIDO",
            "BALCAO" => "ABERTO",
            _ => "OCUPADA"
        };
        RefreshTotals();
        SaveStore();
        QueuePublicMenuOrderStatusSync(board);
        var managerSuffix = approvingManager is null ? "" : $" por {approvingManager.Name}";
        SetStatus(restoredClosedCommand
            ? $"{board.Kind} {board.Number} reaberta com a ultima conta fechada{managerSuffix}."
            : $"{board.Kind} {board.Number} reaberta{managerSuffix}.");
    }

    private static bool HasReceivedPayment(TableTile board)
    {
        return board.LastClosedAt.HasValue
               || !string.IsNullOrWhiteSpace(board.LastReceiptPath)
               || board.ClosedPayments.Count > 0;
    }

    private static bool IsClosedAccountForConference(TableTile? board)
    {
        return board is not null
               && string.Equals(board.Status, "CONTA", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildClosedAccountWaiterMessage(TableTile board)
    {
        return $"{BoardKindLabel(board)} {board.Number} esta com a conta fechada. Somente gerente no caixa pode liberar alteracoes.";
    }

    private bool RequireManagerForClosedAccountEdit(TableTile board, string action)
    {
        if (!IsClosedAccountForConference(board))
        {
            return true;
        }

        if (!ShowOperatorPasswordDialog(
                "Autorizacao do gerente",
                $"{BoardKindLabel(board)} {board.Number} esta com a conta fechada. Para {action}, informe a conta e senha do gerente.",
                "Autorizar",
                IsManagerUser,
                out var approvingManager))
        {
            SetStatus($"{BoardKindLabel(board)} {board.Number} continua em conta. Somente gerente pode {action}.");
            return false;
        }

        board.Status = board.Kind switch
        {
            "DELIVERY" => "PREPARO",
            "KDS" => "RECEBIDO",
            "BALCAO" => "ABERTO",
            _ => "OCUPADA"
        };
        QueuePublicMenuOrderStatusSync(board);
        SetStatus($"{BoardKindLabel(board)} {board.Number} liberada por {approvingManager?.Name ?? "gerente"} para {action}.");
        return true;
    }

    private string? ShowPizzaDialog(ProductTile product, string existingNote)
    {
        var dialog = CreateDialog($"Pizzaria - {product.Name}", 520, 520);
        string? result = null;
        var sizeBox = new ComboBox { ItemsSource = new[] { "PEQUENA", "MEDIA", "GRANDE", "FAMILIA" }, SelectedIndex = 2, MinHeight = 34 };
        var flavor1 = new ComboBox { ItemsSource = Products.Where(item => item.Category == "PIZZAS").Select(item => item.Name).ToList(), SelectedIndex = 0, MinHeight = 34 };
        var flavor2 = new ComboBox { ItemsSource = Products.Where(item => item.Category == "PIZZAS").Select(item => item.Name).ToList(), SelectedIndex = 0, MinHeight = 34 };
        var borderBox = new CheckBox { Content = "Borda catupiry", Margin = new Thickness(0, 10, 0, 8) };
        var noteBox = new TextBox { Text = existingNote, Height = 64, AcceptsReturn = true };
        var ok = DialogButton("Confirmar pizza", "#08A99B");
        ok.Click += (_, _) =>
        {
            var parts = new List<string>
            {
                $"Tamanho {sizeBox.SelectedItem}",
                $"1/2 {flavor1.SelectedItem}",
                $"1/2 {flavor2.SelectedItem}"
            };
            if (borderBox.IsChecked == true) parts.Add("Borda catupiry");
            if (!string.IsNullOrWhiteSpace(noteBox.Text)) parts.Add(noteBox.Text.Trim());
            result = string.Join(" | ", parts);
            dialog.Close();
        };
        dialog.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };
        var panel = DialogPanel();
        panel.Children.Add(DialogLabel("Tamanho"));
        panel.Children.Add(sizeBox);
        panel.Children.Add(DialogLabel("Sabor 1"));
        panel.Children.Add(flavor1);
        panel.Children.Add(DialogLabel("Sabor 2"));
        panel.Children.Add(flavor2);
        panel.Children.Add(borderBox);
        panel.Children.Add(DialogLabel("Observacao"));
        panel.Children.Add(noteBox);
        panel.Children.Add(ok);
        dialog.Content = panel;
        dialog.ShowDialog();
        return result;
    }

    private void ApplyProductSelection(ProductTile product)
    {
        if (BlockIFoodDeliveryEdit("selecionar produto"))
        {
            return;
        }

        var categoryIndex = Categories.ToList().FindIndex(category => category.Name == product.Category);
        if (categoryIndex >= 0)
        {
            SelectCategory(categoryIndex);
        }

        FilterProducts();
        var productIndex = VisibleProducts.IndexOf(product);
        if (productIndex >= 0)
        {
            SelectProduct(productIndex);
        }

        CodeBox.Text = product.Code;
        SelectArea(KeyboardArea.Products);
        SetStatus($"Produto selecionado: {product.Code} {product.Name}");
    }

    private bool RequirePermission(Func<UserAccount, bool> allowed, string feature)
    {
        var current = CurrentUser;
        if (current is not null && allowed(current))
        {
            return true;
        }

        return ShowOperatorPasswordDialog(
            $"Autorizacao - {feature}",
            "Informe operador autorizado e senha.",
            "Autorizar",
            allowed,
            out _);
    }

    private bool ShowOperatorPasswordDialog(
        string title,
        string instruction,
        string buttonText,
        Func<UserAccount, bool> allowed,
        out UserAccount? authenticatedUser,
        bool allowFirstAccess = false)
    {
        authenticatedUser = null;
        UserAccount? resultUser = null;
        var dialog = CreateDialog(title, 430, 315);
        dialog.ResizeMode = ResizeMode.NoResize;

        var operatorBox = new TextBox
        {
            Text = CurrentUser is { } current ? StaffNumber(current) : "",
            Margin = new Thickness(0, 4, 0, 8)
        };
        if (string.IsNullOrWhiteSpace(operatorBox.Text) && CurrentUser is { } currentByName)
        {
            operatorBox.Text = currentByName.Name;
        }

        var password = new PasswordBox { Margin = new Thickness(0, 4, 0, 8), Height = 34 };
        var message = new TextBlock
        {
            Text = instruction,
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var error = new TextBlock
        {
            Foreground = RedText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        var approved = false;
        var button = DialogButton(buttonText, "#03151F");
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.Width = double.NaN;
        var firstAccessButton = DialogButton("Criar primeiro acesso", "#0B3A52");
        firstAccessButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        firstAccessButton.Width = double.NaN;
        firstAccessButton.Visibility = allowFirstAccess && CanCreateFirstAccessUser()
            ? Visibility.Visible
            : Visibility.Collapsed;

        void TryApprove()
        {
            var user = FindAuthenticatedUser(operatorBox.Text, password.Password, allowed);
            if (user is null)
            {
                error.Text = "Operador, senha ou permissao invalidos.";
                password.Clear();
                if (string.IsNullOrWhiteSpace(operatorBox.Text))
                {
                    operatorBox.Focus();
                    operatorBox.SelectAll();
                    return;
                }

                password.Focus();
                return;
            }

            _currentUser = user.Name;
            resultUser = user;
            approved = true;
            dialog.Close();
        }

        button.Click += (_, _) => TryApprove();
        firstAccessButton.Click += (_, _) =>
        {
            if (ShowFirstAccessDialog(out var user) && user is not null)
            {
                resultUser = user;
                approved = true;
                dialog.Close();
            }
        };
        operatorBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                password.Focus();
                e.Handled = true;
            }
        };
        password.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TryApprove();
                e.Handled = true;
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(message);
        panel.Children.Add(DialogLabel("Operador"));
        panel.Children.Add(operatorBox);
        panel.Children.Add(DialogLabel("Senha"));
        panel.Children.Add(password);
        panel.Children.Add(error);
        panel.Children.Add(button);
        panel.Children.Add(firstAccessButton);
        if (firstAccessButton.Visibility == Visibility.Visible)
        {
            panel.Children.Add(DialogHint("Primeiro uso: crie o gerente do sistema antes de operar o caixa."));
        }
        dialog.Content = panel;
        dialog.Loaded += (_, _) =>
        {
            operatorBox.Focus();
            operatorBox.SelectAll();
        };
        dialog.ShowDialog();
        authenticatedUser = resultUser;
        return approved;
    }

    private bool ShowInstallSetupDialog()
    {
        var needsAdmin = CanCreateFirstAccessUser();
        var activated = false;
        UserAccount? createdAdmin = null;
        var dialog = CreateDialog("Ativacao do Balcao Livre PDV", 680, needsAdmin ? 780 : 660);
        dialog.ResizeMode = ResizeMode.CanResize;

        var keyBox = new TextBox
        {
            Text = _appSettings.ActivationKey,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var accountEmailBox = new TextBox { Text = _profile.Email };
        var businessNameBox = new TextBox { Text = _profile.BusinessName };
        var legalNameBox = new TextBox { Text = _profile.LegalName };
        var cnpjBox = new TextBox { Text = _profile.Cnpj };
        AttachCnpjMask(cnpjBox);
        var phoneBox = new TextBox { Text = _profile.Phone };
        var adminNameBox = new TextBox { Text = _profile.OwnerName };
        var adminNumberBox = new TextBox { Text = GetNextStaffNumber().ToString(Brazil) };
        var passwordBox = new PasswordBox { Height = 34 };
        var confirmBox = new PasswordBox { Height = 34 };
        var error = new TextBlock
        {
            Foreground = RedText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var activate = DialogButton(needsAdmin ? "Ativar e criar administrador" : "Ativar sistema", "#08A99B");
        activate.HorizontalAlignment = HorizontalAlignment.Stretch;
        activate.Width = double.NaN;
        var renewSubscription = DialogButton("Pagar assinatura no Stripe", "#0B3A52");
        renewSubscription.HorizontalAlignment = HorizontalAlignment.Stretch;
        renewSubscription.Width = double.NaN;

        UIElement CreateLogo()
        {
            var image = new System.Windows.Controls.Image
            {
                Height = 120,
                MaxWidth = 520,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "setup-logo.png");
                if (File.Exists(filePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    image.Source = bitmap;
                }

                if (image.Source is null)
                {
                    var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/setup-logo.png", UriKind.Absolute));
                    if (resource?.Stream is not null)
                    {
                        using var stream = resource.Stream;
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        image.Source = bitmap;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException)
            {
                Debug.WriteLine($"Setup logo load failed: {ex.Message}");
            }

            if (image.Source is null)
            {
                return new Border
                {
                    Background = BlueSoft,
                    BorderBrush = Solid("#CAD6E2"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 16),
                    Child = new TextBlock
                    {
                        Text = AppDisplayName,
                        Foreground = Solid("#123E66"),
                        FontSize = 28,
                        FontWeight = FontWeights.Bold
                    }
                };
            }

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 0, 16),
                Child = image
            };
        }

        async Task ConfirmAsync()
        {
            if (!activate.IsEnabled)
            {
                return;
            }

            activate.IsEnabled = false;
            error.Foreground = RedText;
            error.Text = "";
            var key = keyBox.Text.Trim();
            try
            {
                if (!TryActivateLicenseKey(key, out var expiresAt, out var plan, out var licenseMessage))
                {
                    error.Text = licenseMessage;
                    keyBox.Focus();
                    keyBox.SelectAll();
                    return;
                }

                var accountEmail = accountEmailBox.Text.Trim().ToLowerInvariant();
                var businessName = businessNameBox.Text.Trim();
                var ownerName = adminNameBox.Text.Trim();
                var cnpj = cnpjBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(accountEmail) || !IsReasonableEmail(accountEmail))
                {
                    error.Text = "Informe o email da conta para vincular a chave.";
                    accountEmailBox.Focus();
                    accountEmailBox.SelectAll();
                    return;
                }

                if (string.IsNullOrWhiteSpace(businessName) && string.IsNullOrWhiteSpace(ownerName))
                {
                    error.Text = "Informe o nome da loja ou do responsavel.";
                    businessNameBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cnpj))
                {
                    error.Text = "Informe o CNPJ da loja para vincular a chave.";
                    cnpjBox.Focus();
                    return;
                }

                _profile.Email = accountEmail;
                _profile.OwnerName = ownerName;
                _profile.BusinessName = businessName;
                _profile.LegalName = legalNameBox.Text.Trim();
                _profile.Cnpj = cnpj;
                _profile.Phone = phoneBox.Text.Trim();
                SaveRestaurantProfile();

                if (needsAdmin)
                {
                    var name = ownerName.ToUpperInvariant();
                    var number = NormalizeStaffNumber(adminNumberBox.Text);
                    var password = passwordBox.Password.Trim();
                    var confirmation = confirmBox.Password.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        error.Text = "Informe o nome do administrador.";
                        adminNameBox.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(number))
                    {
                        error.Text = "Informe o numero do administrador.";
                        adminNumberBox.Focus();
                        return;
                    }

                    if (password.Length < 3)
                    {
                        error.Text = "Informe uma senha com pelo menos 3 digitos.";
                        passwordBox.Focus();
                        return;
                    }

                    if (!string.Equals(password, confirmation, StringComparison.Ordinal))
                    {
                        error.Text = "A confirmacao da senha nao confere.";
                        confirmBox.Focus();
                        return;
                    }

                    foreach (var seedUser in Users.Where(IsDefaultSeedUser).ToList())
                    {
                        Users.Remove(seedUser);
                    }

                    var user = new UserAccount
                    {
                        Name = name,
                        EmployeeNumber = number,
                        Role = "GERENTE",
                        IsMaster = false,
                        CanTransfer = true,
                        CanCash = true,
                        CanCancel = true,
                        CanDiscount = true,
                        CanManageProducts = true,
                        CanReports = true
                    };
                    SetUserPassword(user, password);
                    NormalizeRolePermissions(user);
                    Users.Add(user);
                    createdAdmin = user;
                    _currentUser = user.Name;
                }

                var normalizedKey = NormalizeActivationKey(key);
                error.Text = "Validando chave no admin...";
                var adminResult = await TryValidateAdminActivationAsync(normalizedKey, expiresAt, plan);
                if (!adminResult.Ok)
                {
                    error.Text = adminResult.Message;
                    keyBox.Focus();
                    keyBox.SelectAll();
                    return;
                }

                if (adminResult.ExpiresAt.HasValue)
                {
                    expiresAt = adminResult.ExpiresAt.Value;
                }

                if (!string.IsNullOrWhiteSpace(adminResult.Plan))
                {
                    plan = adminResult.Plan;
                }

                if (!TryRegisterActivationUse(normalizedKey, expiresAt, plan, out var registerMessage))
                {
                    error.Text = registerMessage;
                    keyBox.Focus();
                    keyBox.SelectAll();
                    return;
                }

                _appSettings.ActivationCompleted = true;
                _appSettings.ActivationKey = normalizedKey;
                _appSettings.ActivationPlan = plan;
                _appSettings.ActivationActivatedAt = DateTime.Now;
                _appSettings.ActivationExpiresAt = expiresAt;
                _appSettings.ActivationMachineHash = GetMachineFingerprint();
                _appSettings.ActivationLastWarningKey = "";
                SaveAppSettings();
                SaveStore();
                activated = true;
                _licenseTimer.Start();
                QueueAdminCheckIn("activation", force: true);
                SetStatus(expiresAt.HasValue
                    ? $"Sistema ativado ate {expiresAt.Value:g}."
                    : "Sistema ativado.");
                dialog.Close();
            }
            finally
            {
                if (!activated)
                {
                    activate.IsEnabled = true;
                }
            }
        }

        activate.Click += async (_, _) => await ConfirmAsync();
        renewSubscription.Click += (_, _) =>
        {
            var key = string.IsNullOrWhiteSpace(keyBox.Text) ? _appSettings.ActivationKey : keyBox.Text;
            if (!OpenLicenseRenewalPage(key, _appSettings.ActivationPlan, showMessage: true))
            {
                error.Foreground = RedText;
                error.Text = "Informe a chave vencida para abrir a renovacao.";
            }
            else
            {
                error.Foreground = Solid("#0B3A52");
                error.Text = "Depois do pagamento, a pagina do site mostra uma nova chave. Cole a nova chave aqui e clique em Ativar sistema.";
            }
        };
        keyBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                accountEmailBox.Focus();
                accountEmailBox.SelectAll();
                e.Handled = true;
            }
        };
        accountEmailBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                businessNameBox.Focus();
                businessNameBox.SelectAll();
                e.Handled = true;
            }
        };
        businessNameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                legalNameBox.Focus();
                legalNameBox.SelectAll();
                e.Handled = true;
            }
        };
        legalNameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                cnpjBox.Focus();
                cnpjBox.SelectAll();
                e.Handled = true;
            }
        };
        cnpjBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                phoneBox.Focus();
                phoneBox.SelectAll();
                e.Handled = true;
            }
        };
        phoneBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                if (needsAdmin)
                {
                    adminNameBox.Focus();
                    adminNameBox.SelectAll();
                }
                else
                {
                    await ConfirmAsync();
                }
                e.Handled = true;
            }
        };
        adminNameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                adminNumberBox.Focus();
                adminNumberBox.SelectAll();
                e.Handled = true;
            }
        };
        adminNumberBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                passwordBox.Focus();
                e.Handled = true;
            }
        };
        passwordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                confirmBox.Focus();
                e.Handled = true;
            }
        };
        confirmBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await ConfirmAsync();
                e.Handled = true;
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(CreateLogo());
        panel.Children.Add(new TextBlock
        {
            Text = needsAdmin
                ? "Primeira instalacao: ative o sistema e crie o administrador antes de usar o PDV."
                : "Ative a chave do sistema para continuar usando o PDV.",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(DialogLabel("Chave de ativacao"));
        panel.Children.Add(keyBox);
        panel.Children.Add(DialogHint($"Codigo deste computador: {GetMachineCode()}. A chave fica vinculada a este PC."));
        panel.Children.Add(renewSubscription);
        panel.Children.Add(DialogHint("Se a chave venceu, pague pelo Stripe. A renovacao gera uma nova chave para colar neste campo."));
        panel.Children.Add(new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 14, 0, 4),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Conta da loja", Foreground = Solid("#071A2C"), FontWeight = FontWeights.Bold, FontSize = 15, Margin = new Thickness(0, 0, 0, 8) },
                    DialogHint("Vincule chave, email, nome e CNPJ. O admin salva esses dados no Supabase para o Android e o PDV Web reconhecerem a mesma loja."),
                    DialogField("Email da conta", accountEmailBox),
                    DialogField("Nome fantasia", businessNameBox),
                    DialogField("Razao social", legalNameBox),
                    DialogField("CNPJ", cnpjBox),
                    DialogField("Telefone", phoneBox)
                }
            }
        });

        if (needsAdmin)
        {
            panel.Children.Add(new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 14, 0, 4),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Administrador", Foreground = Solid("#071A2C"), FontWeight = FontWeights.Bold, FontSize = 15, Margin = new Thickness(0, 0, 0, 8) },
                        DialogField("Nome", adminNameBox),
                        DialogField("Numero do operador", adminNumberBox),
                        DialogField("Senha", passwordBox),
                        DialogField("Confirmar senha", confirmBox)
                    }
                }
            });
        }

        panel.Children.Add(error);
        panel.Children.Add(activate);
        dialog.Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        dialog.Loaded += (_, _) =>
        {
            keyBox.Focus();
            keyBox.SelectAll();
        };
        _activationPromptOpen = true;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            _activationPromptOpen = false;
        }

        if (createdAdmin is not null)
        {
            _currentUser = createdAdmin.Name;
        }
        return activated;
    }

    private bool ShowFirstAccessDialog(out UserAccount? createdUser)
    {
        createdUser = null;
        UserAccount? resultUser = null;
        var dialog = CreateDialog("Primeiro acesso", 470, 440);
        dialog.ResizeMode = ResizeMode.NoResize;

        var nameBox = new TextBox { Text = _profile.OwnerName };
        var numberBox = new TextBox { Text = GetNextStaffNumber().ToString(Brazil) };
        var passwordBox = new PasswordBox { Height = 34 };
        var confirmBox = new PasswordBox { Height = 34 };
        var error = new TextBlock
        {
            Foreground = RedText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var create = DialogButton("Criar gerente e entrar", "#08A99B");
        create.HorizontalAlignment = HorizontalAlignment.Stretch;
        create.Width = double.NaN;

        void Confirm()
        {
            var name = nameBox.Text.Trim().ToUpperInvariant();
            var number = NormalizeStaffNumber(numberBox.Text);
            var password = passwordBox.Password.Trim();
            var confirmation = confirmBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                error.Text = "Informe o nome do gerente.";
                nameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(number))
            {
                error.Text = "Informe o numero do operador.";
                numberBox.Focus();
                return;
            }

            if (password.Length < 3)
            {
                error.Text = "Informe uma senha com pelo menos 3 digitos.";
                passwordBox.Focus();
                return;
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                error.Text = "A confirmacao da senha nao confere.";
                confirmBox.Focus();
                return;
            }

            var duplicatedNumber = Users.FirstOrDefault(user =>
                !IsDefaultSeedUser(user)
                && StaffNumber(user) == number);
            if (duplicatedNumber is not null)
            {
                error.Text = $"Numero {number} ja esta em uso por {duplicatedNumber.Name}.";
                numberBox.Focus();
                numberBox.SelectAll();
                return;
            }

            var user = Users.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (user is null || IsDefaultSeedUser(user))
            {
                user = new UserAccount();
                Users.Add(user);
            }

            user.Name = name;
            user.EmployeeNumber = number;
            user.Role = "GERENTE";
            user.IsMaster = false;
            user.CanTransfer = true;
            user.CanCash = true;
            user.CanCancel = true;
            user.CanDiscount = true;
            user.CanManageProducts = true;
            user.CanReports = true;
            SetUserPassword(user, password);
            NormalizeRolePermissions(user);

            _currentUser = user.Name;
            SaveStore();
            resultUser = user;
            SetStatus($"Primeiro acesso criado: {user.Name}.");
            dialog.Close();
        }

        create.Click += (_, _) => Confirm();
        nameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                numberBox.Focus();
                numberBox.SelectAll();
                e.Handled = true;
            }
        };
        numberBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                passwordBox.Focus();
                e.Handled = true;
            }
        };
        passwordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                confirmBox.Focus();
                e.Handled = true;
            }
        };
        confirmBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Confirm();
                e.Handled = true;
            }
        };

        var panel = DialogPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "Crie o gerente do sistema. Ele podera configurar o restaurante, cadastrar produtos, equipe, estoque e relatorios.",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(DialogLabel("Nome do gerente"));
        panel.Children.Add(nameBox);
        panel.Children.Add(DialogLabel("Numero do operador"));
        panel.Children.Add(numberBox);
        panel.Children.Add(DialogLabel("Senha"));
        panel.Children.Add(passwordBox);
        panel.Children.Add(DialogLabel("Confirmar senha"));
        panel.Children.Add(confirmBox);
        panel.Children.Add(error);
        panel.Children.Add(create);
        dialog.Content = panel;
        dialog.Loaded += (_, _) =>
        {
            nameBox.Focus();
            nameBox.SelectAll();
        };
        dialog.ShowDialog();
        createdUser = resultUser;
        return resultUser is not null;
    }

    private bool CanCreateFirstAccessUser()
    {
        return Users.Count == 0 || Users.All(IsDefaultSeedUser);
    }

    private static bool TryActivateLicenseKey(string key, out DateTime? expiresAt, out string plan, out string message)
    {
        expiresAt = null;
        plan = "";
        var normalized = NormalizeActivationKey(key);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            message = "Informe a chave de ativacao.";
            return false;
        }

        if (normalized == "BL-TESTE-2026")
        {
            expiresAt = DateTime.Now.AddDays(30);
            plan = "Teste 30 dias";
            message = "Chave de teste ativada.";
            return true;
        }

        if (TryValidateSignedActivationKey(normalized, out var signedExpiration))
        {
            expiresAt = signedExpiration;
            plan = "Licenca comercial";
            message = "Chave ativada.";
            return true;
        }

        message = "Chave invalida. Verifique a digitacao ou gere uma nova chave.";
        return false;
    }

    private static bool TryValidateSignedActivationKey(string normalized, out DateTime expiration)
    {
        expiration = DateTime.MinValue;
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts is ["BLV", _, _, _])
        {
            if (!TryParseActivationExpiration(parts[1], out expiration))
            {
                return false;
            }

            if (expiration < DateTime.Now)
            {
                return false;
            }

            var expectedV2 = CreateActivationSignatureV2(parts[1], parts[2]);
            return string.Equals(parts[3], expectedV2, StringComparison.OrdinalIgnoreCase);
        }

        if (parts.Length != 3 || parts[0] != "BL")
        {
            return false;
        }

        if (!TryParseActivationExpiration(parts[1], out expiration))
        {
            return false;
        }

        if (expiration < DateTime.Now)
        {
            return false;
        }

        var expected = CreateActivationSignature(parts[1]);
        return string.Equals(parts[2], expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseActivationExpiration(string value, out DateTime expiration)
    {
        if (!DateTime.TryParseExact(
                value,
                new[] { "yyyyMMddHHmm", "yyyyMMdd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out expiration))
        {
            return false;
        }

        if (value.Length == 8)
        {
            expiration = expiration.Date.AddDays(1).AddTicks(-1);
        }

        return true;
    }

    private static string NormalizeActivationKey(string value)
    {
        return (value ?? "")
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("_", "-", StringComparison.Ordinal);
    }

    private static string CreateActivationSignature(string expirationText)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("BalcaoLivrePDV-local-license-v1"));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"BL|{expirationText}"));
        return Convert.ToHexString(bytes)[..8];
    }

    private static string CreateActivationSignatureV2(string expirationText, string serial)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("BalcaoLivrePDV-local-license-v1"));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"BLV|{expirationText}|{serial}"));
        return Convert.ToHexString(bytes)[..10];
    }

    private static bool TryRegisterActivationUse(string normalizedKey, DateTime? expiresAt, string plan, out string message)
    {
        var machineHash = GetMachineFingerprint();
        var ledger = LoadActivationLedger();
        var existing = ledger.Activations.FirstOrDefault(item =>
            string.Equals(item.Key, normalizedKey, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            if (string.Equals(existing.MachineHash, machineHash, StringComparison.Ordinal))
            {
                message = "Chave ja vinculada a este computador.";
                return true;
            }

            message = "Esta chave ja foi vinculada a outro computador. Gere uma nova chave para este PC.";
            return false;
        }

        ledger.Activations.Add(new ActivationUse
        {
            Key = normalizedKey,
            MachineHash = machineHash,
            MachineCode = GetMachineCode(),
            Plan = plan,
            ActivatedAt = DateTime.Now,
            ExpiresAt = expiresAt,
            AppVersion = GetAppVersion()
        });
        SaveActivationLedger(ledger);
        message = "Chave vinculada a este computador.";
        return true;
    }

    private static ActivationLedger LoadActivationLedger()
    {
        var file = GetActivationLedgerFile();
        try
        {
            if (File.Exists(file))
            {
                return JsonSerializer.Deserialize<ActivationLedger>(File.ReadAllText(file, Encoding.UTF8), JsonOptions) ?? new ActivationLedger();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Activation ledger load failed: {ex.Message}");
        }

        return new ActivationLedger();
    }

    private static void SaveActivationLedger(ActivationLedger ledger)
    {
        var file = GetActivationLedgerFile();
        WriteTextAtomic(file, JsonSerializer.Serialize(ledger, JsonOptions));
    }

    private static string GetActivationLedgerFile()
    {
        var candidates = ActivationLedgerCandidates().ToList();
        var existing = candidates.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        foreach (var candidate in candidates)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(candidate)!);
                return candidate;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Activation ledger path unavailable: {ex.Message}");
            }
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BalcaoLivrePDV.License",
            "activation-ledger.json");
        Directory.CreateDirectory(Path.GetDirectoryName(fallback)!);
        return fallback;
    }

    private static IEnumerable<string> ActivationLedgerCandidates()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BalcaoLivrePDV",
            "activation-ledger.json");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BalcaoLivrePDV.License",
            "activation-ledger.json");
    }

    private static string GetMachineCode()
    {
        return GetMachineFingerprint()[..8];
    }

    private static string GetMachineFingerprint()
    {
        var parts = new List<string>();
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var machineGuid = key?.GetValue("MachineGuid")?.ToString();
            if (!string.IsNullOrWhiteSpace(machineGuid))
            {
                parts.Add(machineGuid.Trim());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Debug.WriteLine($"MachineGuid unavailable: {ex.Message}");
        }

        parts.Add(Environment.MachineName);
        var source = string.Join("|", parts.Where(item => !string.IsNullOrWhiteSpace(item))).ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash);
    }

    private static bool IsDefaultSeedUser(UserAccount user)
    {
        return (string.Equals(user.Name, "MASTER", StringComparison.OrdinalIgnoreCase)
                && string.Equals(user.Role, "MASTER", StringComparison.OrdinalIgnoreCase)
                && (user.Pin == "1234" || StaffNumber(user) == "1234"))
            || (string.Equals(user.Name, "CAIXA", StringComparison.OrdinalIgnoreCase)
                && string.Equals(user.Role, "CAIXA", StringComparison.OrdinalIgnoreCase)
                && (user.Pin == "1111" || StaffNumber(user) == "1"))
            || (string.Equals(user.Name, "GARCOM", StringComparison.OrdinalIgnoreCase)
                && string.Equals(user.Role, "GARCOM", StringComparison.OrdinalIgnoreCase)
                && (user.Pin == "2222" || StaffNumber(user) == "1"));
    }

    private void RemoveDefaultSeedUsersIfRealUsersExist()
    {
        if (Users.Any(user => !IsDefaultSeedUser(user)))
        {
            foreach (var seedUser in Users.Where(IsDefaultSeedUser).ToList())
            {
                Users.Remove(seedUser);
            }
        }
    }

    private UserAccount? FindAuthenticatedUser(string operatorText, string password, Func<UserAccount, bool> allowed)
    {
        var typed = (operatorText ?? "").Trim();
        var pin = (password ?? "").Trim();
        if (string.IsNullOrWhiteSpace(typed) || string.IsNullOrWhiteSpace(pin))
        {
            return null;
        }

        var number = NormalizeStaffNumber(typed);
        return Users.FirstOrDefault(user =>
            allowed(user)
            && IsUserPassword(user, pin)
            && (string.Equals(user.Name, typed, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(number) && StaffNumber(user) == number)));
    }

    private static bool IsUserPassword(UserAccount user, string password)
    {
        return VerifyUserPassword(user, password);
    }

    private string NextProductCode()
    {
        var max = Products
            .Select(product => int.TryParse(product.Code, out var code) ? code : 0)
            .DefaultIfEmpty(0)
            .Max();
        return (max + 1).ToString("000000", Brazil);
    }

    private Window CreateDialog(string title, double width, double height)
    {
        var dialog = new ModernDialogWindow
        {
            Title = title,
            Owner = this,
            Width = width,
            Height = Math.Min(height + 28, SystemParameters.WorkArea.Height - 56),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            Background = Solid("#EEF2F5"),
            FontFamily = new FontFamily("Segoe UI")
        };
        ApplyDialogResources(dialog);
        dialog.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };
        return dialog;
    }

    private void LoadAppSettings()
    {
        var shouldSaveSettings = false;
        try
        {
            if (File.Exists(SettingsFile))
            {
                _appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile, Encoding.UTF8), JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _appSettings = new AppSettings();
        }

        if (_appSettings.NotificationSound is not ("PADRAO" or "AVISO" or "ERRO" or "NENHUM"))
        {
            _appSettings.NotificationSound = "PADRAO";
        }

        _appSettings.IFood ??= new IFoodIntegrationSettings();
        if (EnsureIFoodCloudSettings(_appSettings.IFood))
        {
            shouldSaveSettings = true;
        }

        if (NormalizeWhatsAppSendPulseOnlySettings())
        {
            shouldSaveSettings = true;
        }

        _appSettings.MercadoPago ??= new MercadoPagoPaymentSettings();
        _appSettings.MercadoPago.DefaultTerminalId = (_appSettings.MercadoPago.DefaultTerminalId ?? "").Trim();
        _appSettings.MercadoPago.DefaultTerminalLabel = (_appSettings.MercadoPago.DefaultTerminalLabel ?? "").Trim();
        _appSettings.MercadoPago.SellerUserId = (_appSettings.MercadoPago.SellerUserId ?? "").Trim();
        _appSettings.MercadoPago.LastError = (_appSettings.MercadoPago.LastError ?? "").Trim();

        _appSettings.PagBank ??= new PagBankPaymentSettings();
        _appSettings.PagBank.AccountId = (_appSettings.PagBank.AccountId ?? "").Trim();
        _appSettings.PagBank.DefaultTerminalId = (_appSettings.PagBank.DefaultTerminalId ?? "").Trim();
        _appSettings.PagBank.DefaultTerminalLabel = (_appSettings.PagBank.DefaultTerminalLabel ?? "").Trim();
        _appSettings.PagBank.PlugPagComPort = (_appSettings.PagBank.PlugPagComPort ?? "").Trim().ToUpperInvariant();
        _appSettings.PagBank.LastError = (_appSettings.PagBank.LastError ?? "").Trim();

        if (_appSettings.PrintLayout is not ("PEQUENO" or "GRANDE"))
        {
            _appSettings.PrintLayout = "GRANDE";
            shouldSaveSettings = true;
        }

        if (!_appSettings.LargeReceiptDefaultApplied)
        {
            _appSettings.PrintLayout = "GRANDE";
            _appSettings.LargeReceiptDefaultApplied = true;
            shouldSaveSettings = true;
        }

        if (NormalizeProductionDestinations())
        {
            shouldSaveSettings = true;
        }

        _appSettings.ReceiptQrKind = NormalizeReceiptQrKind(_appSettings.ReceiptQrKind);
        if (!_appSettings.ReceiptQrEnabled)
        {
            _appSettings.ReceiptQrContent = _appSettings.ReceiptQrContent?.Trim() ?? "";
        }

        if (string.IsNullOrWhiteSpace(_appSettings.UpdateManifestUrl))
        {
            _appSettings.UpdateManifestUrl = DefaultUpdateManifestUrl;
            shouldSaveSettings = true;
        }

        if (string.IsNullOrWhiteSpace(_appSettings.AdminApiUrl) ||
            string.Equals(_appSettings.AdminApiUrl.Trim().TrimEnd('/'), "http://localhost:5188", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(_appSettings.AdminApiUrl.Trim().TrimEnd('/'), "https://balcaolivrepdv.onrender.com", StringComparison.OrdinalIgnoreCase))
        {
            _appSettings.AdminApiUrl = DefaultAdminApiUrl;
            shouldSaveSettings = true;
        }

        if (string.IsNullOrWhiteSpace(_appSettings.WhatsAppFunctionUrl))
        {
            _appSettings.WhatsAppFunctionUrl = DefaultWhatsAppFunctionUrl;
            shouldSaveSettings = true;
        }

        if (_appSettings.PublicMenuWaitMinMinutes <= 0)
        {
            _appSettings.PublicMenuWaitMinMinutes = 30;
            shouldSaveSettings = true;
        }

        if (_appSettings.PublicMenuWaitMaxMinutes < _appSettings.PublicMenuWaitMinMinutes)
        {
            _appSettings.PublicMenuWaitMaxMinutes = Math.Max(_appSettings.PublicMenuWaitMinMinutes, 60);
            shouldSaveSettings = true;
        }

        if (string.IsNullOrWhiteSpace(_appSettings.PublicMenuDiscountCode))
        {
            _appSettings.PublicMenuDiscountCode = "EXCLUSIVO4";
            shouldSaveSettings = true;
        }

        if (_appSettings.PublicMenuDiscountAmount <= 0)
        {
            _appSettings.PublicMenuDiscountAmount = 4m;
            shouldSaveSettings = true;
        }

        if (string.IsNullOrWhiteSpace(_appSettings.PublicMenuDiscountDescription)
            || string.Equals(_appSettings.PublicMenuDiscountDescription.Trim(), "Use no atendimento para ganhar desconto no pedido.", StringComparison.Ordinal))
        {
            _appSettings.PublicMenuDiscountDescription = "Apresente este cupom no atendimento para receber o desconto.";
            shouldSaveSettings = true;
        }

        if (_appSettings.PublicMenuLoyaltyGoal <= 0)
        {
            _appSettings.PublicMenuLoyaltyGoal = 20;
            shouldSaveSettings = true;
        }

        if (_appSettings.PublicMenuLoyaltyMinimumOrder <= 0)
        {
            _appSettings.PublicMenuLoyaltyMinimumOrder = 20m;
            shouldSaveSettings = true;
        }

        if (shouldSaveSettings)
        {
            SaveAppSettings();
        }
    }

    private void SaveAppSettings()
    {
        lock (_settingsFileLock)
        {
            WriteTextAtomic(SettingsFile, JsonSerializer.Serialize(_appSettings, JsonOptions));
        }
    }

    private static string NormalizeReceiptQrKind(string? value)
    {
        var kind = (value ?? "").Trim().ToUpperInvariant();
        return kind switch
        {
            "PIX" => "PIX",
            "INSTAGRAM" => "INSTAGRAM",
            "GOOGLE MAPS" => "GOOGLE MAPS",
            "MAPS" => "GOOGLE MAPS",
            "LINK" => "LINK",
            _ => "PIX"
        };
    }

    private static string NormalizeSectorName(string? value, string fallback = "COZINHA")
    {
        var sector = (value ?? "").Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(sector) ? fallback : sector;
    }

    private static string NormalizeProductDestination(string? value, string fallback = "CAIXA")
    {
        var destination = NormalizeSectorName(value, fallback);
        return destination switch
        {
            "BALCAO" or "BALCÃO" or "SEM SETOR" or "DELIVERY" or "ENTREGA" or "RETIRADA" => "CAIXA",
            _ => destination
        };
    }

    private static bool IsProductionDestinationName(string? value)
    {
        var destination = NormalizeProductDestination(value, "CAIXA");
        return !string.Equals(destination, "CAIXA", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyDefaultProductionDestination(string? value)
    {
        return NormalizeSectorName(value, "") is "BAR" or "PIZZA" or "SOBREMESA" or "SOBREMESAS";
    }

    private bool NormalizeProductionDestinations()
    {
        _appSettings.SectorPrinters ??= [];
        var normalizedSectorPrinters = _appSettings.SectorPrinters
            .Where(setting => !string.IsNullOrWhiteSpace(setting.Sector))
            .Select(setting => new SectorPrinterSetting
            {
                Sector = NormalizeProductDestination(setting.Sector, ""),
                PrinterName = (setting.PrinterName ?? "").Trim()
            })
            .Where(setting => !string.IsNullOrWhiteSpace(setting.Sector))
            .Where(setting => IsProductionDestinationName(setting.Sector))
            .Where(setting => !IsLegacyDefaultProductionDestination(setting.Sector) || !string.IsNullOrWhiteSpace(setting.PrinterName))
            .GroupBy(setting => setting.Sector, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (normalizedSectorPrinters.All(setting => !string.Equals(setting.Sector, "COZINHA", StringComparison.OrdinalIgnoreCase)))
        {
            normalizedSectorPrinters.Insert(0, new SectorPrinterSetting { Sector = "COZINHA" });
        }

        var changed = normalizedSectorPrinters.Count != _appSettings.SectorPrinters.Count
            || normalizedSectorPrinters.Where((setting, index) =>
                !string.Equals(setting.Sector, _appSettings.SectorPrinters[index].Sector, StringComparison.Ordinal)
                || !string.Equals(setting.PrinterName, _appSettings.SectorPrinters[index].PrinterName, StringComparison.Ordinal)).Any();

        if (changed)
        {
            _appSettings.SectorPrinters = normalizedSectorPrinters;
        }

        return changed;
    }

    private List<string> GetConfiguredProductSectors()
    {
        var sectors = new List<string> { "CAIXA" };
        sectors.AddRange((_appSettings.SectorPrinters ?? [])
            .Select(setting => NormalizeSectorName(setting.Sector, ""))
            .Where(sector => !string.IsNullOrWhiteSpace(sector))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(sector => sector == "COZINHA" ? 0 : 1)
            .ThenBy(sector => sector, StringComparer.OrdinalIgnoreCase));
        return sectors
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<string> GetKnownProductSectors()
    {
        var sectorPrinters = _appSettings.SectorPrinters ?? [];
        return Products
            .Select(product => NormalizeProductDestination(product.Sector, "CAIXA"))
            .Concat(sectorPrinters.Select(setting => NormalizeSectorName(setting.Sector, "")))
            .Where(sector => !string.IsNullOrWhiteSpace(sector))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(sector => sector, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool TryEnsureProductionDestinationConfigured(string destination)
    {
        var normalized = NormalizeProductDestination(destination, "CAIXA");
        if (!IsProductionDestinationName(normalized))
        {
            return true;
        }

        _appSettings.SectorPrinters ??= [];
        if (_appSettings.SectorPrinters.Any(setting =>
                string.Equals(NormalizeProductDestination(setting.Sector, ""), normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        const string defaultPrinterOption = "Usar padrao do Windows";
        var installedPrinters = new List<string> { defaultPrinterOption };
        installedPrinters.AddRange(GetInstalledPrinterNames());
        var printerBox = new ComboBox
        {
            ItemsSource = installedPrinters,
            SelectedIndex = 0,
            MinHeight = 36
        };
        var status = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var dialog = CreateDialog($"Impressora de {normalized}", 520, 330);
        var confirmed = false;
        var save = DialogButton("Salvar destino", "#08A99B");
        save.HorizontalAlignment = HorizontalAlignment.Stretch;
        save.Width = double.NaN;
        save.Click += (_, _) =>
        {
            var selected = printerBox.SelectedItem?.ToString() ?? defaultPrinterOption;
            var selectedPrinterName = string.Equals(selected, defaultPrinterOption, StringComparison.Ordinal)
                ? GetDefaultPrinterName()
                : selected.Trim();
            _appSettings.SectorPrinters.Add(new SectorPrinterSetting
            {
                Sector = normalized,
                PrinterName = string.Equals(selectedPrinterName, "nenhuma", StringComparison.OrdinalIgnoreCase) ? "" : selectedPrinterName
            });
            NormalizeProductionDestinations();
            SaveAppSettings();
            confirmed = true;
            dialog.Close();
        };

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(SectionTitle($"Novo destino: {normalized}"));
        panel.Children.Add(DialogHint("Escolha a impressora que recebe os pedidos deste destino. CAIXA nao manda para producao; destinos como COZINHA imprimem ordem de preparo."));
        panel.Children.Add(DialogField("Impressora", printerBox));
        panel.Children.Add(save);
        panel.Children.Add(status);
        dialog.Content = panel;
        dialog.ShowDialog();
        return confirmed;
    }

    private void LoadRestaurantProfile()
    {
        try
        {
            if (File.Exists(ProfileFile))
            {
                _profile = JsonSerializer.Deserialize<RestaurantIdentityProfile>(File.ReadAllText(ProfileFile, Encoding.UTF8), JsonOptions) ?? new RestaurantIdentityProfile();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _profile = new RestaurantIdentityProfile();
        }
    }

    private void SaveRestaurantProfile()
    {
        WriteTextAtomic(ProfileFile, JsonSerializer.Serialize(_profile, JsonOptions));
        QueuePublicMenuPublish();
    }

    private static TextBlock SectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Solid("#071A2C"),
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
    }

    private static Grid TwoColumnFields((string Label, UIElement Input) left, (string Label, UIElement Input) right)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var leftField = DialogField(left.Label, left.Input);
        var rightField = DialogField(right.Label, right.Input);
        Grid.SetColumn(rightField, 1);
        grid.Children.Add(leftField);
        grid.Children.Add(rightField);
        return grid;
    }

    private string GetDefaultPrinterName()
    {
        try
        {
            return LocalPrintServer.GetDefaultPrintQueue()?.FullName ?? "nenhuma";
        }
        catch (Exception ex) when (ex is PrintSystemException or InvalidOperationException)
        {
            return "nenhuma";
        }
    }

    private static List<string> GetInstalledPrinterNames()
    {
        try
        {
            var server = new LocalPrintServer();
            return server.GetPrintQueues(new[]
                {
                    EnumeratedPrintQueueTypes.Local,
                    EnumeratedPrintQueueTypes.Connections
                })
                .Select(queue => string.IsNullOrWhiteSpace(queue.FullName) ? queue.Name : queue.FullName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is PrintSystemException or InvalidOperationException)
        {
            Debug.WriteLine($"Unable to list printers: {ex.Message}");
            return [];
        }
    }

    private string GetPrinterNameForSector(string sector)
    {
        var normalized = NormalizeProductDestination(sector, "");
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        var sectorPrinters = _appSettings.SectorPrinters ?? [];
        var setting = sectorPrinters.FirstOrDefault(item =>
            string.Equals(NormalizeSectorName(item.Sector, ""), normalized, StringComparison.OrdinalIgnoreCase));
        return (setting?.PrinterName ?? "").Trim();
    }

    private PrintQueue? GetConfiguredPrintQueue(string printerName = "")
    {
        try
        {
            var requested = string.IsNullOrWhiteSpace(printerName)
                ? _appSettings.PreferredPrinterName
                : printerName;
            if (!string.IsNullOrWhiteSpace(requested))
            {
                var server = new LocalPrintServer();
                var preferred = requested.Trim();
                return server.GetPrintQueues(new[]
                    {
                        EnumeratedPrintQueueTypes.Local,
                        EnumeratedPrintQueueTypes.Connections
                    })
                    .FirstOrDefault(queue =>
                        string.Equals(queue.FullName, preferred, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(queue.Name, preferred, StringComparison.OrdinalIgnoreCase))
                    ?? server.GetPrintQueue(preferred);
            }
        }
        catch (Exception ex) when (ex is PrintSystemException or InvalidOperationException)
        {
            Debug.WriteLine($"Preferred printer unavailable: {ex.Message}");
        }

        try
        {
            return LocalPrintServer.GetDefaultPrintQueue();
        }
        catch (Exception ex) when (ex is PrintSystemException or InvalidOperationException)
        {
            Debug.WriteLine($"Default printer unavailable: {ex.Message}");
            return null;
        }
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? "1.6.2026";
    }

    private async Task<bool> CheckForUpdatesAsync(bool showIfCurrent, bool autoInstall = false)
    {
        var manifestUrl = (_appSettings.UpdateManifestUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            manifestUrl = DefaultUpdateManifestUrl;
            _appSettings.UpdateManifestUrl = DefaultUpdateManifestUrl;
            SaveAppSettings();
        }

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            using var response = await client.GetAsync(manifestUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
            _appSettings.LastUpdateCheckAt = DateTime.Now;
            SaveAppSettings();

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.InstallerUrl))
            {
                if (showIfCurrent)
                {
                    SetStatus("Manifesto de atualizacao invalido. Verifique o version.json.");
                }

                return false;
            }

            if (!IsVersionNewer(manifest.Version, GetAppVersion()))
            {
                if (showIfCurrent)
                {
                    ShowToast("Sistema atualizado", $"Versao instalada: {GetAppVersion()}.", "AT", "#08A99B", "#E6FBF8");
                    SetStatus("Nenhuma atualizacao disponivel.");
                }

                return false;
            }

            if (autoInstall)
            {
                ShowToast("Atualizacao encontrada", $"Baixando versao {manifest.Version}.", "AT", "#08A99B", "#E6FBF8");
                SetStatus($"Atualizacao {manifest.Version} encontrada. Baixando instalador automaticamente...");
                return await DownloadAndOpenInstallerAsync(manifest, status: null, silentInstall: true);
            }

            ShowUpdateDialog(manifest);
            return false;
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or JsonException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            if (showIfCurrent)
            {
                SetStatus("Nao foi possivel verificar atualizacoes agora.");
                ShowToast("Atualizacao indisponivel", "Confira a URL ou a internet e tente novamente.", "AT", "#99620D", "#FFF2CB");
            }

            return false;
        }
    }

    private void ShowUpdateDialog(UpdateManifest manifest)
    {
        var dialog = CreateDialog("Atualizacao disponivel", 560, 420);
        var panel = DialogPanel();
        var status = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var version = new TextBlock
        {
            Text = $"Nova versao: {manifest.Version}",
            Foreground = Solid("#08A99B"),
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 4, 0, 8)
        };
        var notes = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(manifest.Notes)
                ? "Baixe o instalador e execute para atualizar o Balcao Livre PDV."
                : manifest.Notes,
            Foreground = Solid("#405366"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var download = DialogButton("Baixar instalador", "#08A99B");
        var later = DialogButton("Depois", "#5B6B7A");
        var buttons = new UniformGrid
        {
            Columns = 2,
            Rows = 1,
            Margin = new Thickness(0, 8, 0, 0)
        };

        download.HorizontalAlignment = HorizontalAlignment.Stretch;
        later.HorizontalAlignment = HorizontalAlignment.Stretch;
        buttons.Children.Add(later);
        buttons.Children.Add(download);

        later.Click += (_, _) => dialog.Close();
        download.Click += async (_, _) =>
        {
            download.IsEnabled = false;
            later.IsEnabled = false;
            status.Text = "Baixando instalador...";
            try
            {
                if (await DownloadAndOpenInstallerAsync(manifest, status, silentInstall: false))
                {
                    dialog.Close();
                }
            }
            finally
            {
                download.IsEnabled = true;
                later.IsEnabled = true;
            }
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Tem uma nova versao pronta para instalar.",
            Foreground = Solid("#071A2C"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(version);
        panel.Children.Add(notes);
        if (manifest.Required)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Atualizacao obrigatoria para continuar recebendo suporte.",
                Foreground = RedText,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        panel.Children.Add(status);
        panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private async Task<bool> DownloadAndOpenInstallerAsync(UpdateManifest manifest, TextBlock? status, bool silentInstall)
    {
        if (!Uri.TryCreate(manifest.InstallerUrl.Trim(), UriKind.Absolute, out var uri))
        {
            if (status is not null)
            {
                status.Text = "URL do instalador invalida.";
            }

            SetStatus("URL do instalador invalida.");
            return false;
        }

        var downloads = silentInstall
            ? Path.Combine(_dataRoot, "updates")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(downloads);
        var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.LocalPath));
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            fileName = $"BalcaoLivrePDVOnline-Setup-{SafeFileName(manifest.Version)}.exe";
        }

        var destination = Path.Combine(downloads, fileName);
        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        using var response = await client.GetAsync(uri, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using (var input = await response.Content.ReadAsStreamAsync())
        await using (var output = File.Create(destination))
        {
            await input.CopyToAsync(output);
        }

        if (status is not null)
        {
            status.Text = $"Instalador salvo em {destination}.";
        }

        var arguments = silentInstall
            ? "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"
            : "";

        SetStatus(silentInstall
            ? "Instalador baixado. Atualizando e reabrindo o PDV..."
            : "Instalador baixado. Execute para atualizar o PDV.");
        ShowToast("Instalador baixado", silentInstall
            ? "O PDV sera fechado e reaberto atualizado."
            : "O instalador sera aberto agora.", "AT", "#08A99B", "#E6FBF8");
        try
        {
            Process.Start(new ProcessStartInfo(destination)
            {
                UseShellExecute = true,
                Arguments = arguments
            });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Debug.WriteLine($"Installer start failed: {ex.Message}");
            if (status is not null)
            {
                status.Text = "Instalador baixado, mas nao foi possivel abrir automaticamente.";
            }

            SetStatus("Instalador baixado, mas nao foi possivel abrir automaticamente.");
            ShowToast("Atualizacao pendente", "Abra o instalador baixado para concluir.", "AT", "#99620D", "#FFF2CB");
            return false;
        }

        if (silentInstall)
        {
            await Task.Delay(700);
            _exitRequested = true;
            SaveActiveTicketToCurrentBoard();
            SaveStore();
            _trayIcon?.Dispose();
            _trayIcon = null;
            Application.Current.Shutdown();
        }

        return true;
    }

    private static bool IsVersionNewer(string candidate, string installed)
    {
        var candidateVersion = TryParseAppVersion(candidate);
        var installedVersion = TryParseAppVersion(installed);
        if (candidateVersion is not null && installedVersion is not null)
        {
            return candidateVersion.CompareTo(installedVersion) > 0;
        }

        return !string.Equals(candidate.Trim(), installed.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static Version? TryParseAppVersion(string value)
    {
        var clean = value.Trim();
        var suffixIndex = clean.IndexOfAny(new[] { '-', '+' });
        if (suffixIndex >= 0)
        {
            clean = clean[..suffixIndex];
        }

        return Version.TryParse(clean, out var version) ? version : null;
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(invalid.Contains(ch) ? '-' : ch);
        }

        return builder.Length == 0 ? "1.6.2026" : builder.ToString();
    }

    private static string CopyLogoToAppIdentityFolder(string sourcePath)
    {
        return CopyImageToAppIdentityFolder(sourcePath, "restaurant-logo");
    }

    private static string CopyImageToAppIdentityFolder(string sourcePath, string fileStem)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BalcaoLivre.Online.Windows",
            "identity");
        Directory.CreateDirectory(root);

        var extension = Path.GetExtension(sourcePath);
        var destination = Path.Combine(root, $"{fileStem}{extension}");
        File.Copy(sourcePath, destination, overwrite: true);
        return destination;
    }

    private static string CopyNotificationSoundToAppFolder(string sourcePath)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BalcaoLivre.Online.Windows",
            "media");
        Directory.CreateDirectory(root);

        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".wav";
        }

        var destination = Path.Combine(root, $"ifood-alert{extension.ToLowerInvariant()}");
        File.Copy(sourcePath, destination, overwrite: true);
        return destination;
    }

    private static StackPanel DialogPanel()
    {
        return new StackPanel { Margin = new Thickness(18) };
    }

    private static Border BorderCard()
    {
        return new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#D6E4F1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };
    }

    private static Border OrderAlertHero(
        string platform,
        string title,
        string orderNumber,
        string customer,
        string total,
        string accent,
        string subtitle,
        params UIElement[] extraRows)
    {
        var rows = new StackPanel();
        rows.Children.Add(new TextBlock
        {
            Text = platform.ToUpperInvariant(),
            Foreground = Solid("#BCEFEA"),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        rows.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        rows.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Solid("#D8E9EF"),
            FontSize = 13,
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var row in extraRows)
        {
            rows.Children.Add(row);
        }

        var summary = new Border
        {
            Background = Solid("#FFFFFF"),
            BorderBrush = Solid("#D8E8EF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = orderNumber,
                        Foreground = Solid(accent),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = EmptyDash(customer),
                        Foreground = Solid("#071A2C"),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 5, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = total,
                        Foreground = Solid("#071A2C"),
                        FontSize = 26,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 12, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(rows);
        Grid.SetColumn(summary, 1);
        grid.Children.Add(summary);

        return new Border
        {
            Background = Solid("#062230"),
            BorderBrush = Solid(accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(22),
            Child = grid
        };
    }

    private static WrapPanel OrderChipPanel(params (string Label, string Value, string Accent)[] chips)
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        foreach (var chip in chips)
        {
            if (string.IsNullOrWhiteSpace(chip.Value))
            {
                continue;
            }

            panel.Children.Add(new Border
            {
                Background = Solid("#F3F8FB"),
                BorderBrush = Solid("#D8E8EF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new TextBlock
                {
                    Text = $"{chip.Label}: {chip.Value}",
                    Foreground = Solid(chip.Accent),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        return panel;
    }

    private static Border OrderInfoCard(string label, string value, string accent = "#0B3A52", string background = "#F8FBFD")
    {
        return new Border
        {
            Background = Solid(background),
            BorderBrush = Solid("#D6E4F1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(13, 10, 13, 10),
            Margin = new Thickness(0, 0, 10, 10),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        Foreground = Solid("#5B6B7A"),
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = EmptyDash(value),
                        Foreground = Solid(accent),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 5, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private static UniformGrid OrderInfoGrid(params (string Label, string Value, string Accent, string Background)[] cards)
    {
        var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 10, 0, 0) };
        foreach (var card in cards)
        {
            grid.Children.Add(OrderInfoCard(card.Label, card.Value, card.Accent, card.Background));
        }

        return grid;
    }

    private static Border OrderItemsCard(IEnumerable<string> lines, string title, string subtitle)
    {
        var list = new StackPanel();
        var count = 0;
        foreach (var line in lines.Where(item => !string.IsNullOrWhiteSpace(item)).Take(60))
        {
            count++;
            list.Children.Add(new Border
            {
                Background = Solid(count % 2 == 0 ? "#FFFFFF" : "#F8FBFD"),
                BorderBrush = Solid("#E3EDF5"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = line,
                    Foreground = Solid("#071A2C"),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        if (count == 0)
        {
            list.Children.Add(new TextBlock
            {
                Text = "Nenhum item informado.",
                Foreground = Solid("#5B6B7A"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            });
        }

        var card = BorderCard();
        card.Margin = new Thickness(0, 12, 0, 0);
        card.Child = new StackPanel
        {
            Children =
            {
                SectionTitle(title),
                new TextBlock
                {
                    Text = subtitle,
                    Foreground = Solid("#5B6B7A"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                },
                new ScrollViewer
                {
                    Content = list,
                    MaxHeight = 160,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Margin = new Thickness(0, 10, 0, 0),
                    PanningMode = PanningMode.VerticalOnly
                }
            }
        };
        return card;
    }

    private static Border OrderActionMessageCard(TextBlock message)
    {
        return new Border
        {
            Background = Solid("#F3F8FB"),
            BorderBrush = Solid("#D6E4F1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 12, 0, 0),
            Child = message
        };
    }

    private static TextBlock DialogLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Solid("#5B6B7A"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 6, 0, 4)
        };
    }

    private static StackPanel DialogField(string label, UIElement input)
    {
        return new StackPanel
        {
            Margin = new Thickness(0, 0, 10, 10),
            Children =
            {
                DialogLabel(label),
                input
            }
        };
    }

    private static TextBlock DialogHint(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Solid("#5B6B7A"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
    }

    private static Button DialogButton(string text, string color)
    {
        var isNeutral = string.Equals(color, "#5B6B7A", StringComparison.OrdinalIgnoreCase)
            || string.Equals(color, "#667684", StringComparison.OrdinalIgnoreCase);
        var button = new Button
        {
            Content = text,
            Height = 38,
            MinWidth = 128,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = isNeutral ? Solid("#EEF4F8") : Solid(color),
            Foreground = isNeutral ? Solid("#071A2C") : Brushes.White,
            BorderBrush = isNeutral ? Solid("#CAD6E2") : Solid(color),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            Template = RoundedButtonTemplate()
        };
        return button;
    }

    private static void ApplyDialogResources(Window dialog)
    {
        dialog.Resources.Add(typeof(TextBox), DialogTextBoxStyle());
        dialog.Resources.Add(typeof(PasswordBox), DialogPasswordBoxStyle());
        dialog.Resources.Add(typeof(ComboBox), DialogComboBoxStyle());
        dialog.Resources.Add(typeof(ListBox), DialogListBoxStyle());
        dialog.Resources.Add(typeof(CheckBox), DialogCheckBoxStyle());
        dialog.Resources.Add(typeof(System.Windows.Controls.TabControl), DialogTabControlStyle());
        dialog.Resources.Add(typeof(System.Windows.Controls.TabItem), DialogTabItemStyle());
    }

    private static Style DialogTextBoxStyle()
    {
        var style = new Style(typeof(TextBox));
        style.Setters.Add(new Setter(Control.HeightProperty, 38d));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 14d));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#071A2C")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Solid("#BFD1E2")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(Control.TemplateProperty, RoundedTextInputTemplate(typeof(TextBox))));
        return style;
    }

    private static Style DialogPasswordBoxStyle()
    {
        var style = new Style(typeof(PasswordBox));
        style.Setters.Add(new Setter(Control.HeightProperty, 38d));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 14d));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#071A2C")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Solid("#BFD1E2")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.TemplateProperty, RoundedTextInputTemplate(typeof(PasswordBox))));
        return style;
    }

    private static Style DialogComboBoxStyle()
    {
        var style = new Style(typeof(ComboBox));
        style.Setters.Add(new Setter(Control.HeightProperty, 38d));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 14d));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#071A2C")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Solid("#BFD1E2")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 0, 8, 0)));
        return style;
    }

    private static Style DialogListBoxStyle()
    {
        var style = new Style(typeof(ListBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Solid("#CAD6E2")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto));
        return style;
    }

    private static Style DialogCheckBoxStyle()
    {
        var style = new Style(typeof(CheckBox));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#071A2C")));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 5, 0, 5)));
        return style;
    }

    private static Style DialogTabControlStyle()
    {
        var style = new Style(typeof(System.Windows.Controls.TabControl));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Solid("#F8FBFD")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 12, 16, 16)));
        return style;
    }

    private static Style DialogTabItemStyle()
    {
        var style = new Style(typeof(System.Windows.Controls.TabItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#5B6B7A")));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 0, 6, 8)));
        style.Setters.Add(new Setter(Control.TemplateProperty, DialogTabItemTemplate()));
        return style;
    }

    private static ControlTemplate DialogTabItemTemplate()
    {
        var template = new ControlTemplate(typeof(System.Windows.Controls.TabItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "TabChrome";
        border.SetValue(Border.BackgroundProperty, Solid("#EEF4F8"));
        border.SetValue(Border.BorderBrushProperty, Solid("#CAD6E2"));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
        border.SetValue(Border.PaddingProperty, new Thickness(14, 8, 14, 8));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, Solid("#E6FBF8"), "TabChrome"));
        hover.Setters.Add(new Setter(Border.BorderBrushProperty, Solid("#73E7DE"), "TabChrome"));
        template.Triggers.Add(hover);

        var selected = new Trigger { Property = System.Windows.Controls.TabItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(Border.BackgroundProperty, Solid("#071A2C"), "TabChrome"));
        selected.Setters.Add(new Setter(Border.BorderBrushProperty, Solid("#071A2C"), "TabChrome"));
        selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        template.Triggers.Add(selected);
        return template;
    }

    private static ControlTemplate RoundedButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ButtonChrome";
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
        presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.92));
        template.Triggers.Add(hover);

        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
        template.Triggers.Add(disabled);
        return template;
    }

    private static ControlTemplate RoundedTextInputTemplate(Type targetType)
    {
        var template = new ControlTemplate(targetType);
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "InputChrome";
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var host = new FrameworkElementFactory(typeof(ScrollViewer));
        host.Name = "PART_ContentHost";
        host.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 10, 0));
        host.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(host);
        template.VisualTree = border;

        var focused = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        focused.Setters.Add(new Setter(Border.BorderBrushProperty, Solid("#0B3A52"), "InputChrome"));
        focused.Setters.Add(new Setter(Border.BackgroundProperty, Solid("#FBFDFF"), "InputChrome"));
        template.Triggers.Add(focused);
        return template;
    }

    private sealed class ModernDialogWindow : Window
    {
        private bool _isApplyingShell;

        public ModernDialogWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            FocusVisualStyle = null;
            SnapsToDevicePixels = true;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            if (_isApplyingShell)
            {
                return;
            }

            _isApplyingShell = true;
            Content = BuildShell(newContent);
            _isApplyingShell = false;
        }

        private FrameworkElement BuildShell(object body)
        {
            var rootBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#0B3A52"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                SnapsToDevicePixels = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 26,
                    ShadowDepth = 5,
                    Opacity = 0.18,
                    Color = Colors.Black
                }
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new Border
            {
                Background = Solid("#03151F"),
                BorderBrush = Solid("#0E3A4A"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };
            header.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            var headerGrid = new Grid { Margin = new Thickness(16, 0, 10, 0) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

            var badge = new Border
            {
                Width = 28,
                Height = 28,
                Background = Solid("#0B3A52"),
                BorderBrush = Solid("#255665"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "BL",
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var titleText = new TextBlock
            {
                Text = Title,
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(titleText, 1);

            var closeButton = new Button
            {
                Content = "X",
                Width = 30,
                Height = 30,
                Background = Solid("#0B3A52"),
                BorderBrush = Solid("#255665"),
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Template = RoundedButtonTemplate()
            };
            closeButton.Click += (_, _) => Close();
            Grid.SetColumn(closeButton, 2);

            headerGrid.Children.Add(badge);
            headerGrid.Children.Add(titleText);
            headerGrid.Children.Add(closeButton);
            header.Child = headerGrid;

            var bodyHost = new Border
            {
                Background = Solid("#EEF4F8"),
                CornerRadius = new CornerRadius(0, 0, 10, 10),
                Child = new ContentControl
                {
                    Content = body,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                }
            };
            Grid.SetRow(bodyHost, 1);

            root.Children.Add(header);
            root.Children.Add(bodyHost);
            rootBorder.Child = root;
            return rootBorder;
        }
    }

    private string BuildMenuHtml()
    {
        var grouped = Products
            .Where(product => product.Active)
            .GroupBy(product => string.IsNullOrWhiteSpace(product.Category) ? "Cardapio" : product.Category.Trim())
            .OrderBy(group => group.Key)
            .ToList();
        var restaurant = EscapeHtml(ResolveWaiterRestaurantName());
        var description = EscapeHtml("Veja os produtos, escolha o que deseja e chame a equipe para pedir.");
        var meta = string.Join("  |  ", new[] { _profile.Phone, _profile.Address, _profile.City, _profile.State }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()));
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"pt-br\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{restaurant} - Cardapio</title>");
        sb.AppendLine("<style>:root{--accent:#0f766e;--ink:#17212b;--muted:#5d7080;--line:#d8e2ec;--wash:#f4f8fb}*{box-sizing:border-box}body{margin:0;background:var(--wash);color:var(--ink);font-family:Inter,Segoe UI,Arial,sans-serif}.wrap{width:min(960px,100%);margin:0 auto;padding:16px}.hero{min-height:230px;border-radius:10px;background:linear-gradient(135deg,rgba(15,118,110,.95),rgba(36,91,145,.9));color:white;padding:26px;display:flex;flex-direction:column;justify-content:flex-end}.eyebrow{text-transform:uppercase;font-weight:850;font-size:12px;letter-spacing:.08em;opacity:.85;margin:0 0 8px}h1{font-size:clamp(32px,7vw,62px);line-height:1;margin:0}.hero p{max-width:680px;color:rgba(255,255,255,.88);font-size:16px;line-height:1.45}.meta{border:1px solid var(--line);background:#fff;border-radius:8px;padding:13px 14px;margin:14px 0;color:var(--muted);font-weight:650}.chips{display:flex;gap:8px;overflow:auto;padding:2px 0 12px}.chips a{flex:0 0 auto;text-decoration:none;color:var(--ink);background:#fff;border:1px solid var(--line);border-radius:999px;padding:10px 14px;font-weight:800}.cat{margin:12px 0 10px;font-size:24px}.item{background:#fff;border:1px solid var(--line);border-radius:8px;padding:14px;margin:10px 0;display:grid;grid-template-columns:1fr auto;gap:8px 14px}.item h3{margin:0;font-size:17px}.code{color:var(--muted);font-size:12px;font-weight:750}.price{color:var(--accent);font-size:17px;font-weight:900;white-space:nowrap}.footer{margin:20px 0;padding:16px;background:#fff;border:1px solid var(--line);border-radius:8px;color:var(--muted)}@media(max-width:640px){.wrap{padding:10px}.hero{min-height:200px;padding:18px}.item{grid-template-columns:1fr}.price{justify-self:start}}</style></head><body>");
        sb.AppendLine("<main class=\"wrap\">");
        sb.AppendLine($"<header class=\"hero\"><p class=\"eyebrow\">Cardapio digital</p><h1>{restaurant}</h1><p>{description}</p></header>");
        if (!string.IsNullOrWhiteSpace(meta))
        {
            sb.AppendLine($"<section class=\"meta\">{EscapeHtml(meta)}</section>");
        }

        sb.AppendLine("<nav class=\"chips\">");
        foreach (var group in grouped)
        {
            var id = ToMenuAnchor(group.Key);
            sb.AppendLine($"<a href=\"#{id}\">{EscapeHtml(group.Key)}</a>");
        }

        sb.AppendLine("</nav>");
        foreach (var group in grouped)
        {
            var id = ToMenuAnchor(group.Key);
            sb.AppendLine($"<section id=\"{id}\"><h2 class=\"cat\">{EscapeHtml(group.Key)}</h2>");
            foreach (var product in group.OrderBy(product => product.Name))
            {
                sb.AppendLine("<article class=\"item\">");
                sb.AppendLine($"<div><h3>{EscapeHtml(product.Name)}</h3><span class=\"code\">{EscapeHtml(product.Code)}</span></div>");
                sb.AppendLine($"<strong class=\"price\">{Money(product.Price)}</strong>");
                sb.AppendLine("</article>");
            }

            sb.AppendLine("</section>");
        }

        sb.AppendLine("<footer class=\"footer\"><strong>Como usar:</strong> escolha no celular e informe o pedido para a equipe do restaurante.</footer>");
        sb.AppendLine("</main></body></html>");
        return sb.ToString();
    }

    private static string ToMenuAnchor(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder("cat-");
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
            else if (sb.Length == 0 || sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        return sb.ToString().TrimEnd('-');
    }

    private string BuildOperationalReport(string period = "Total")
    {
        var openOrders = GetOpenReportOrders();
        var topProducts = GetTopReportProducts(period, 20);
        var lowStock = Products
            .Where(product => product.MinimumStock > 0 && product.StockQuantity <= product.MinimumStock)
            .OrderBy(product => product.Name)
            .ToList();
        var cashMovements = GetReportCashMovements(period)
            .OrderByDescending(item => item.When)
            .Take(50)
            .ToList();
        var profitSummary = GetProfitSummaryForReportPeriod(period);

        var sb = new StringBuilder();
        sb.AppendLine($"{AppReceiptName} - RELATORIO OPERACIONAL");
        sb.AppendLine($"PERIODO: {period.ToUpperInvariant()}");
        sb.AppendLine(DateTime.Now.ToString("G", Brazil));
        sb.AppendLine();
        sb.AppendLine("COMANDAS ABERTAS");
        foreach (var table in openOrders)
        {
            sb.AppendLine($"{table.Kind} {table.Number} {table.Status} {table.CustomerName} {Money(table.Total)}");
        }

        sb.AppendLine();
        sb.AppendLine("LUCRO E MARGEM");
        sb.AppendLine($"Vendas calculadas: {Money(profitSummary.Revenue)}");
        sb.AppendLine($"Custo estimado: {Money(profitSummary.Cost)}");
        sb.AppendLine($"Lucro bruto: {Money(profitSummary.Profit)}");
        sb.AppendLine($"Margem: {profitSummary.Margin:N2}%");

        if (IsIFoodReportEnabled())
        {
            var ifoodSummary = GetIFoodReportSummary(period);
            sb.AppendLine();
            sb.AppendLine("IFOOD - TAXAS E LUCRO ESTIMADOS");
            sb.AppendLine("Taxas padrao estimadas: entrega propria/merchant 12%; entrega iFood 23%.");
            sb.AppendLine($"Pedidos iFood: {ifoodSummary.TotalOrders:N0}  validos {ifoodSummary.ValidOrders:N0}  entregues {ifoodSummary.DeliveredOrders:N0}  cancelados {ifoodSummary.CancelledOrders:N0}");
            sb.AppendLine($"Venda bruta iFood: {Money(ifoodSummary.Revenue)}");
            sb.AppendLine($"Entrega propria/merchant ({ifoodSummary.MerchantOrders:N0}): venda {Money(ifoodSummary.MerchantRevenue)}  taxa -{Money(ifoodSummary.EstimatedMerchantFee)}");
            sb.AppendLine($"Entrega iFood ({ifoodSummary.IFoodShipmentOrders:N0}): venda {Money(ifoodSummary.IFoodShipmentRevenue)}  taxa -{Money(ifoodSummary.EstimatedIFoodShipmentFee)}");
            sb.AppendLine($"Custo produto estimado: {Money(ifoodSummary.ProductCost)}");
            sb.AppendLine($"Taxa iFood estimada total: -{Money(ifoodSummary.EstimatedFee)}");
            sb.AppendLine($"Lucro liquido iFood estimado: {Money(ifoodSummary.EstimatedNetProfit)}");
            sb.AppendLine($"Margem liquida iFood estimada: {ifoodSummary.EstimatedMargin:N2}%");
        }

        sb.AppendLine();
        sb.AppendLine("PRODUTOS MAIS VENDIDOS");
        foreach (var product in topProducts)
        {
            var totalProfit = product.ProfitAmount * product.SoldQuantity;
            sb.AppendLine($"{product.Code} {product.Name} qtd {product.SoldQuantity:N0} venda {product.PriceText} compra {product.CostPriceText} lucro {Money(totalProfit)} margem {product.ProfitMargin:N2}% estoque {product.StockQuantity:N0}");
        }

        sb.AppendLine();
        sb.AppendLine("ESTOQUE BAIXO");
        foreach (var product in lowStock)
        {
            sb.AppendLine($"{product.Code} {product.Name} estoque {product.StockQuantity:N0} minimo {product.MinimumStock:N0}");
        }

        sb.AppendLine();
        sb.AppendLine("CAIXA");
        sb.AppendLine($"Total atual: {Money(_cashTotal)}");
        foreach (var movement in cashMovements)
        {
            sb.AppendLine($"{movement.When:G} {movement.Type} {Money(movement.Amount)} {movement.User} {movement.Reason}");
        }

        return sb.ToString();
    }

    private string BuildCashReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Total atual: {Money(_cashTotal)}");
        foreach (var movement in CashMovements.OrderByDescending(item => item.When).Take(50))
        {
            sb.AppendLine($"{movement.When:G} {movement.Type} {Money(movement.Amount)} {movement.User} {movement.Reason}");
        }

        return sb.ToString();
    }

    private string BuildCashClosingReport()
    {
        var today = DateTime.Today;
        var closedBoards = Tables
            .Concat(DeliveryTiles)
            .Where(board => board.LastClosedAt.HasValue && board.LastClosedAt.Value.Date == today)
            .ToList();
        var closedLines = closedBoards.SelectMany(board => board.ClosedLines).ToList();
        var closedPayments = closedBoards.SelectMany(board => board.ClosedPayments).ToList();
        var salesTotal = closedLines.Sum(line => line.Total);
        if (salesTotal <= 0 && closedPayments.Count > 0)
        {
            salesTotal = closedPayments.Sum(payment => payment.Amount);
        }

        var profitSummary = GetProfitSummaryForLines(closedLines);
        var sb = new StringBuilder();
        var displayName = string.IsNullOrWhiteSpace(_profile.BusinessName)
            ? AppReceiptName
            : _profile.BusinessName.Trim().ToUpperInvariant();
        sb.AppendLine(displayName);
        if (!string.IsNullOrWhiteSpace(_profile.Cnpj))
        {
            sb.AppendLine($"CNPJ: {_profile.Cnpj}");
        }

        sb.AppendLine("FECHAMENTO DE CAIXA");
        sb.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", Brazil));
        sb.AppendLine($"OPERADOR: {_currentUser}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"TOTAL EM CAIXA: {Money(_cashTotal)}");
        sb.AppendLine($"VENDAS DO DIA: {Money(salesTotal)}");
        sb.AppendLine($"CUSTO ESTIMADO: {Money(profitSummary.Cost)}");
        sb.AppendLine($"LUCRO BRUTO: {Money(profitSummary.Profit)}");
        sb.AppendLine($"MARGEM: {profitSummary.Margin:N2}%");
        sb.AppendLine($"CONTAS FECHADAS: {closedBoards.Count}");
        sb.AppendLine();
        sb.AppendLine("FORMAS DE PAGAMENTO");
        if (closedPayments.Count == 0)
        {
            sb.AppendLine("Sem pagamentos fechados hoje.");
        }
        else
        {
            foreach (var group in closedPayments.GroupBy(payment => payment.Method).OrderBy(group => group.Key))
            {
                sb.AppendLine($"{group.Key}: {Money(group.Sum(payment => payment.Amount))}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("PRODUTOS VENDIDOS");
        if (closedLines.Count == 0)
        {
            sb.AppendLine("Sem produtos fechados hoje.");
        }
        else
        {
            foreach (var group in closedLines.GroupBy(line => new { line.Code, line.Name }).OrderByDescending(group => group.Sum(line => line.Total)).Take(20))
            {
                var quantity = group.Sum(line => line.Quantity);
                var revenue = group.Sum(line => line.Total);
                var product = Products.FirstOrDefault(item => item.Code == group.Key.Code);
                var cost = (product?.CostPrice ?? 0m) * quantity;
                var itemProfit = revenue - cost;
                var itemMargin = revenue > 0 ? itemProfit / revenue * 100m : 0m;
                sb.AppendLine($"{group.Key.Code} {group.Key.Name} qtd {quantity} total {Money(revenue)} lucro {Money(itemProfit)} margem {itemMargin:N2}%");
            }
        }

        sb.AppendLine();
        sb.AppendLine("MOVIMENTOS DO CAIXA");
        foreach (var movement in CashMovements.Where(item => item.When.Date == today).OrderBy(item => item.When))
        {
            sb.AppendLine($"{movement.When:HH:mm} {CashMovementLabel(movement.Type)} {Money(movement.Amount)} {movement.Reason}");
        }

        var pending = GetPendingCashBoards();
        sb.AppendLine();
        sb.AppendLine("PENDENCIAS");
        if (pending.Count == 0)
        {
            sb.AppendLine("Sem pendencias.");
        }
        else
        {
            foreach (var board in pending)
            {
                sb.AppendLine(PendingCashBoardDisplay(board));
            }
        }

        sb.AppendLine("--------------------------------");
        sb.AppendLine("CAIXA FECHADO");
        return sb.ToString();
    }

    private string WriteReportFile(string prefix, string content)
    {
        var path = Path.Combine(ExportDir, $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private bool ShouldSendLineToProduction(TicketLine line)
    {
        if (IsTableCharge(line))
        {
            return false;
        }

        var destination = NormalizeProductDestination(line.Sector, "CAIXA");
        if (!IsProductionDestinationName(destination))
        {
            return false;
        }

        return (_appSettings.SectorPrinters ?? []).Any(setting =>
            string.Equals(NormalizeProductDestination(setting.Sector, ""), destination, StringComparison.OrdinalIgnoreCase));
    }

    private void ScheduleKitchenPrint(TableTile board, IEnumerable<TicketLine> lines)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ScheduleKitchenPrint(board, lines.ToList()));
            return;
        }

        var productionLines = lines
            .Where(ShouldSendLineToProduction)
            .Where(line => line.Quantity > line.KitchenPrintedQuantity)
            .ToList();
        if (productionLines.Count == 0)
        {
            return;
        }

        if (!_pendingKitchenPrintLines.TryGetValue(board, out var pending))
        {
            pending = [];
            _pendingKitchenPrintLines[board] = pending;
        }

        foreach (var line in productionLines)
        {
            pending.Add(line);
        }

        _kitchenPrintBatchTimer.Stop();
        _kitchenPrintBatchTimer.Start();
    }

    private void FlushPendingKitchenPrints()
    {
        _kitchenPrintBatchTimer.Stop();
        if (_pendingKitchenPrintLines.Count == 0)
        {
            return;
        }

        var batches = _pendingKitchenPrintLines
            .Select(entry => new { Board = entry.Key, Lines = entry.Value.ToList() })
            .ToList();
        _pendingKitchenPrintLines.Clear();

        foreach (var batch in batches)
        {
            PrintKitchenBatch(batch.Board, batch.Lines);
        }

        SaveStore();
    }

    private void PrintKitchenBatch(TableTile board, IEnumerable<TicketLine> sourceLines)
    {
        var linesToPrint = new List<TicketLine>();
        foreach (var source in sourceLines.Where(ShouldSendLineToProduction))
        {
            var quantityToPrint = source.Quantity - source.KitchenPrintedQuantity;
            if (quantityToPrint <= 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(source.KitchenStatus) || string.Equals(source.KitchenStatus, "ENTREGUE", StringComparison.OrdinalIgnoreCase))
            {
                source.KitchenStatus = "RECEBIDO";
                source.KitchenStartedAt = null;
                source.KitchenReadyAt = null;
            }

            var printLine = CloneLine(source);
            printLine.Quantity = quantityToPrint;
            printLine.Sector = NormalizeProductDestination(source.Sector, "CAIXA");
            printLine.KitchenPrintedQuantity = quantityToPrint;
            linesToPrint.Add(printLine);
            source.KitchenPrintedQuantity += quantityToPrint;
        }

        if (linesToPrint.Count == 0)
        {
            return;
        }

        KitchenTiles.Add(new TableTile
        {
            Number = $"{KitchenTiles.Count + 1:000000}",
            Kind = "KDS",
            Status = AggregateKitchenStatus(linesToPrint),
            Waiter = board.Waiter,
            Lines = linesToPrint.Select(CloneLine).ToList(),
            Total = linesToPrint.Sum(line => line.Total),
            Detail = $"{board.Kind} {board.Number}  {linesToPrint.Sum(line => line.Quantity):N0} item(ns)"
        });

        if (!_appSettings.AutoPrintKitchen)
        {
            SetStatus($"Producao enviada ao monitor: {board.Kind} {board.Number}.");
            return;
        }

        var printedDestinations = new List<string>();
        var failedDestinations = new List<string>();
        foreach (var group in linesToPrint
                     .GroupBy(line => NormalizeProductDestination(line.Sector, "CAIXA"))
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var destination = group.Key;
            var printed = TryPrintTextToDefaultPrinter(
                BuildKitchenSectorPrintText(board, destination, group),
                $"Producao {destination} {board.Number}",
                compact: _appSettings.PrintLayout == "PEQUENO",
                printerName: GetPrinterNameForSector(destination));
            (printed ? printedDestinations : failedDestinations).Add(destination);
        }

        SetStatus(failedDestinations.Count == 0
            ? $"Producao impressa: {string.Join(", ", printedDestinations)}."
            : $"Producao gerada, mas falhou impressao em: {string.Join(", ", failedDestinations)}.");
    }

    private void PrintKitchen()
    {
        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        var path = Path.Combine(ExportDir, $"cozinha-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"{board.Kind} {board.Number}");
        var staffLine = BuildStaffReceiptLine(board);
        if (!string.IsNullOrWhiteSpace(staffLine))
        {
            sb.AppendLine(staffLine);
        }

        foreach (var line in TicketLines.Where(ShouldSendLineToProduction))
        {
            if (string.IsNullOrWhiteSpace(line.KitchenStatus) || line.KitchenStatus == "ENTREGUE")
            {
                line.KitchenStatus = "RECEBIDO";
                line.KitchenStartedAt = null;
                line.KitchenReadyAt = null;
            }
        }

        var kitchenGroups = TicketLines
            .Where(ShouldSendLineToProduction)
            .GroupBy(line => NormalizeProductDestination(line.Sector, "CAIXA"))
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Sector = NormalizeProductDestination(group.Key, "CAIXA"),
                Lines = group.ToList()
            })
            .ToList();

        if (kitchenGroups.Count == 0)
        {
            SetStatus("Nenhum item com destino de producao nesta comanda.");
            return;
        }

        foreach (var group in kitchenGroups)
        {
            sb.AppendLine();
            sb.AppendLine($"SETOR {group.Sector}");
            foreach (var line in group.Lines)
            {
                sb.AppendLine($"{line.Quantity}x {line.Name} [{line.KitchenStatus}] {line.Note}".Trim());
                line.KitchenPrintedQuantity = Math.Max(line.KitchenPrintedQuantity, line.Quantity);
                if (!string.IsNullOrWhiteSpace(line.ModifierSummary))
                {
                    sb.AppendLine($"  {line.ModifierSummary}");
                }
            }
        }

        var printText = sb.ToString();
        File.WriteAllText(path, printText, Encoding.UTF8);
        KitchenTiles.Add(new TableTile
        {
            Number = $"{KitchenTiles.Count + 1:000000}",
            Kind = "KDS",
            Status = AggregateKitchenStatus(TicketLines),
            Waiter = board.Waiter,
            Lines = TicketLines.Select(CloneLine).ToList(),
            Total = TicketLines.Sum(line => line.Total),
            Detail = $"{board.Kind} {board.Number}  {TicketLines.Count(ShouldSendLineToProduction):N0} item(ns)"
        });
        SaveStore();
        if (_appSettings.AutoPrintKitchen)
        {
            var printedSectors = new List<string>();
            var failedSectors = new List<string>();
            foreach (var group in kitchenGroups)
            {
                var sectorText = BuildKitchenSectorPrintText(board, group.Sector, group.Lines);
                var printed = TryPrintTextToDefaultPrinter(
                    sectorText,
                    $"Pedido {group.Sector} {board.Number}",
                    compact: _appSettings.PrintLayout == "PEQUENO",
                    printerName: GetPrinterNameForSector(group.Sector));
                (printed ? printedSectors : failedSectors).Add(group.Sector);
            }

            SetStatus(failedSectors.Count == 0
                ? $"Pedido impresso por setor: {string.Join(", ", printedSectors)}."
                : $"Pedido gerado: {path}. Falhou impressao em: {string.Join(", ", failedSectors)}.");
        }
        else
        {
            SetStatus($"Pedido de cozinha gerado: {path}");
        }
    }

    private string BuildKitchenSectorPrintText(TableTile board, string sector, IEnumerable<TicketLine> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{board.Kind} {board.Number}");
        sb.AppendLine($"SETOR {NormalizeSectorName(sector, "SEM SETOR")}");
        var staffLine = BuildStaffReceiptLine(board);
        if (!string.IsNullOrWhiteSpace(staffLine))
        {
            sb.AppendLine(staffLine);
        }

        foreach (var noteLine in BuildBoardNoteLines(board.Notes))
        {
            sb.AppendLine(noteLine);
        }

        sb.AppendLine(DateTime.Now.ToString("g", Brazil));
        sb.AppendLine("--------------------------------");
        foreach (var line in lines)
        {
            sb.AppendLine($"{line.Quantity}x {line.Name} [{line.KitchenStatus}]".Trim());
            if (!string.IsNullOrWhiteSpace(line.ModifierSummary))
            {
                sb.AppendLine($"  {line.ModifierSummary}");
            }

            if (!string.IsNullOrWhiteSpace(line.Note))
            {
                sb.AppendLine($"OBS: {line.Note}");
            }
        }

        return sb.ToString();
    }

    private string BuildDeliveryPrintText(TableTile order, string district, string printSize)
    {
        var compact = printSize == "PEQUENO";
        var divider = compact ? "--------------------------------" : "------------------------------------------------";
        var districtText = string.IsNullOrWhiteSpace(district) ? order.District : district;
        var sb = new StringBuilder();
        sb.AppendLine("PEDIDO DELIVERY / RETIRADA");
        sb.AppendLine(divider);
        sb.AppendLine($"Pedido: {order.Number}");
        sb.AppendLine($"Data: {DateTime.Now:G}");
        sb.AppendLine($"Tipo: {order.Detail}");
        if (IsIFoodDeliveryBoard(order))
        {
            sb.AppendLine($"OrderId iFood: {order.ExternalOrderId}");
            var orderType = BuildIFoodOrderTypeText(order);
            if (!string.IsNullOrWhiteSpace(orderType)) sb.AppendLine(orderType);
            var schedule = BuildIFoodScheduleText(order);
            if (!string.IsNullOrWhiteSpace(schedule)) sb.AppendLine(schedule);
            sb.AppendLine($"Entrega: {BuildIFoodShipmentText(order)}");
            var payment = BuildIFoodPaymentText(order);
            if (!string.IsNullOrWhiteSpace(payment)) sb.AppendLine(payment);
            var voucher = BuildIFoodVoucherText(order);
            if (!string.IsNullOrWhiteSpace(voucher)) sb.AppendLine(voucher);
            var cancellation = BuildIFoodCancellationText(order);
            if (!string.IsNullOrWhiteSpace(cancellation)) sb.AppendLine(cancellation);
        }
        else if (IsPublicMenuDeliveryBoard(order))
        {
            sb.AppendLine($"Pedido cardapio: {ShortPublicMenuOrderId(order.ExternalOrderId)}");
            if (!string.IsNullOrWhiteSpace(order.ExternalPaymentSummary)) sb.AppendLine(order.ExternalPaymentSummary);
        }

        sb.AppendLine($"Cliente: {order.CustomerName}");
        if (!string.IsNullOrWhiteSpace(order.CustomerCpf)) sb.AppendLine($"CPF/CNPJ: {order.CustomerCpf}");
        if (!string.IsNullOrWhiteSpace(order.Phone)) sb.AppendLine($"Telefone: {order.Phone}");
        if (!string.IsNullOrWhiteSpace(order.Address)) sb.AppendLine($"Endereco: {order.Address}");
        if (!string.IsNullOrWhiteSpace(districtText)) sb.AppendLine($"Bairro/ref: {districtText}");
        if (!string.IsNullOrWhiteSpace(order.Driver)) sb.AppendLine($"Entregador: {order.Driver}");
        if (!string.IsNullOrWhiteSpace(order.Notes))
        {
            sb.AppendLine(divider);
            sb.AppendLine("OBSERVACAO");
            sb.AppendLine(order.Notes);
        }

        sb.AppendLine(divider);
        sb.AppendLine("ITENS");
        if (order.Lines.Count == 0)
        {
            sb.AppendLine("Pedido aberto sem itens.");
        }
        else
        {
            foreach (var line in order.Lines)
            {
                sb.AppendLine($"{line.Quantity}x {line.Name} {Money(line.Total)}");
            }
        }

        sb.AppendLine(divider);
        sb.AppendLine($"TOTAL INICIAL: {Money(order.Total)}");
        sb.AppendLine();
        sb.AppendLine("Assinatura/Conferencia: __________________");
        return sb.ToString();
    }

    private bool TryPrintTextToDefaultPrinter(string content, string jobName, bool compact, string qrPayload = "", string qrCaption = "", string printerName = "")
    {
        try
        {
            var queue = GetConfiguredPrintQueue(printerName);
            if (queue is null)
            {
                return false;
            }

            qrPayload = (qrPayload ?? "").Trim();
            if (LooksLikeEscPosPrinter(queue))
            {
                var resolvedPrinterName = string.IsNullOrWhiteSpace(queue.FullName) ? queue.Name : queue.FullName;
                if (TryPrintEscPosReceipt(resolvedPrinterName, content, jobName, compact, qrPayload, qrCaption))
                {
                    return true;
                }
            }

            var dialog = new PrintDialog { PrintQueue = queue };
            var fontSize = compact ? 9.5 : 11.5;
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = fontSize,
                PagePadding = new Thickness(8, 2, 8, 2),
                ColumnWidth = double.PositiveInfinity
            };

            foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                document.Blocks.Add(new Paragraph(new Run(line))
                {
                    Margin = new Thickness(0, 0, 0, compact ? 0 : 1),
                    LineHeight = compact ? 12 : 16
                });
            }

            if (!string.IsNullOrWhiteSpace(qrPayload))
            {
                var qr = TryCreateQrBitmap(qrPayload, compact ? 8 : 10);
                if (qr is not null)
                {
                    document.Blocks.Add(new Paragraph(new Run(string.IsNullOrWhiteSpace(qrCaption) ? "QR CODE" : qrCaption.Trim().ToUpperInvariant()))
                    {
                        TextAlignment = TextAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        FontSize = fontSize,
                        Margin = new Thickness(0, compact ? 4 : 8, 0, 2)
                    });

                    var imageSize = compact ? 190 : 230;
                    document.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image
                    {
                        Source = qr,
                        Width = imageSize,
                        Height = imageSize,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center
                    })
                    {
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, compact ? 2 : 8)
                    });
                }
            }

            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, jobName);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Print failed: {ex}");
            return false;
        }
    }

    private bool TryPrintQrOnlyToDefaultPrinter(string qrPayload, string jobName, string printerName = "")
    {
        try
        {
            qrPayload = (qrPayload ?? "").Trim();
            if (string.IsNullOrWhiteSpace(qrPayload))
            {
                return false;
            }

            var queue = GetConfiguredPrintQueue(printerName);
            if (queue is null)
            {
                return false;
            }

            if (LooksLikeEscPosPrinter(queue))
            {
                var resolvedPrinterName = string.IsNullOrWhiteSpace(queue.FullName) ? queue.Name : queue.FullName;
                if (TryPrintEscPosQrOnly(resolvedPrinterName, qrPayload, jobName))
                {
                    return true;
                }
            }

            var qr = TryCreateQrBitmap(qrPayload, 12);
            if (qr is null)
            {
                return false;
            }

            var dialog = new PrintDialog { PrintQueue = queue };
            var document = new FlowDocument
            {
                PagePadding = new Thickness(2),
                ColumnWidth = double.PositiveInfinity
            };
            document.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image
            {
                Source = qr,
                Width = 320,
                Height = 320,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            });

            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, jobName);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"QR only print failed: {ex}");
            return false;
        }
    }

    private static BitmapSource? TryCreateQrBitmap(string payload, int pixelsPerModule)
    {
        try
        {
            var bytes = TryCreateQrPngBytes(payload, pixelsPerModule);
            if (bytes is null)
            {
                return null;
            }

            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"QR generation failed: {ex.Message}");
            return null;
        }
    }

    private static BitmapSource? TryCreateBitmapFromBase64(string base64)
    {
        try
        {
            var value = (base64 ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var comma = value.IndexOf(',', StringComparison.Ordinal);
            if (comma >= 0)
            {
                value = value[(comma + 1)..];
            }

            var bytes = Convert.FromBase64String(value);
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException or IOException)
        {
            Debug.WriteLine($"Base64 image generation failed: {ex.Message}");
            return null;
        }
    }

    private static byte[]? TryCreateQrPngBytes(string payload, int pixelsPerModule)
    {
        try
        {
            payload = (payload ?? "").Trim();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qr = new PngByteQRCode(data);
            return qr.GetGraphic(Math.Clamp(pixelsPerModule, 1, 20));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"QR PNG generation failed: {ex.Message}");
            return null;
        }
    }

    private static bool LooksLikeEscPosPrinter(PrintQueue queue)
    {
        var name = $"{queue.FullName} {queue.Name}".ToUpperInvariant();
        return name.Contains("POS", StringComparison.Ordinal)
               || name.Contains("58", StringComparison.Ordinal)
               || name.Contains("80", StringComparison.Ordinal)
               || name.Contains("TERMICA", StringComparison.Ordinal)
               || name.Contains("THERMAL", StringComparison.Ordinal)
               || name.Contains("RECEIPT", StringComparison.Ordinal)
               || name.Contains("ELGIN", StringComparison.Ordinal)
               || name.Contains("BEMATECH", StringComparison.Ordinal)
               || name.Contains("DARUMA", StringComparison.Ordinal)
               || name.Contains("EPSON", StringComparison.Ordinal)
               || name.Contains("TM-", StringComparison.Ordinal);
    }

    private static bool TryPrintEscPosReceipt(string printerName, string content, string jobName, bool compact, string qrPayload, string qrCaption)
    {
        try
        {
            var bytes = BuildEscPosReceiptBytes(content, compact, qrPayload, qrCaption);
            return SendRawToPrinter(printerName, bytes, jobName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ESC/POS print failed: {ex}");
            return false;
        }
    }

    private static byte[] BuildEscPosReceiptBytes(string content, bool compact, string qrPayload, string qrCaption)
    {
        using var ms = new MemoryStream();
        var textEncoding = GetReceiptPrinterEncoding();

        void Command(params byte[] bytes) => ms.Write(bytes, 0, bytes.Length);
        void Text(string text)
        {
            var bytes = textEncoding.GetBytes(text);
            ms.Write(bytes, 0, bytes.Length);
        }

        Command(0x1B, 0x40); // init
        Command(0x1B, 0x74, 0x02); // CP850 on most ESC/POS printers
        Command(0x1B, 0x61, 0x01);
        Command(0x1D, 0x21, compact ? (byte)0x00 : (byte)0x01);
        Text("\n");
        Command(0x1B, 0x61, 0x00);
        Command(0x1D, 0x21, 0x00);

        var textWidth = GetEscPosReceiptTextWidth(compact);
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            Text(FormatEscPosReceiptLine(line, textWidth));
            Text("\n");
        }

        qrPayload = (qrPayload ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(qrPayload))
        {
            Text("\n");
            Command(0x1B, 0x61, 0x01);
            Command(0x1D, 0x21, 0x00);
            Text(string.IsNullOrWhiteSpace(qrCaption) ? "QR CODE\n" : $"{qrCaption.Trim().ToUpperInvariant()}\n");

            if (!TryAppendEscPosQrRaster(ms, qrPayload, desiredPrintWidth: compact ? 272 : 296, maxPrintWidth: 384))
            {
                Command(0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00); // model 2
                Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, compact ? (byte)0x05 : (byte)0x06); // module size
                Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30); // error correction L

                var qrBytes = Encoding.UTF8.GetBytes(qrPayload);
                var storeLength = qrBytes.Length + 3;
                var pL = (byte)(storeLength % 256);
                var pH = (byte)(storeLength / 256);
                Command(0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30);
                ms.Write(qrBytes, 0, qrBytes.Length);
                Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30); // print QR
            }

            Text("\n");
            Command(0x1B, 0x61, 0x00);
        }

        Text("\n\n\n");
        return ms.ToArray();
    }

    private static bool TryPrintEscPosQrOnly(string printerName, string qrPayload, string jobName)
    {
        try
        {
            var bytes = BuildEscPosQrOnlyBytes(qrPayload);
            return SendRawToPrinter(printerName, bytes, jobName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ESC/POS QR only print failed: {ex}");
            return false;
        }
    }

    private static byte[] BuildEscPosQrOnlyBytes(string qrPayload)
    {
        using var ms = new MemoryStream();

        void Command(params byte[] bytes) => ms.Write(bytes, 0, bytes.Length);
        void Text(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            ms.Write(bytes, 0, bytes.Length);
        }

        Command(0x1B, 0x40); // init
        Command(0x1B, 0x61, 0x01); // center
        Text("\n");

        if (!TryAppendEscPosQrRaster(ms, qrPayload, desiredPrintWidth: 352, maxPrintWidth: 384))
        {
            Command(0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00); // model 2
            Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x08); // large module
            Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30); // error correction L

            var qrBytes = Encoding.UTF8.GetBytes((qrPayload ?? "").Trim());
            var storeLength = qrBytes.Length + 3;
            var pL = (byte)(storeLength % 256);
            var pH = (byte)(storeLength / 256);
            Command(0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30);
            ms.Write(qrBytes, 0, qrBytes.Length);
            Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30); // print QR
        }

        Text("\n\n\n");
        return ms.ToArray();
    }

    private static int GetEscPosReceiptTextWidth(bool compact)
    {
        return compact ? 30 : 28;
    }

    private static string FormatEscPosReceiptLine(string value, int width)
    {
        value = (value ?? string.Empty).TrimEnd('\r');
        if (value.Length > width)
        {
            value = value[..width];
        }

        return string.IsNullOrWhiteSpace(value) ? string.Empty : " " + value;
    }

    private static bool TryAppendEscPosQrRaster(Stream output, string payload, int desiredPrintWidth, int maxPrintWidth)
    {
        var probe = TryCreateQrBitmap(payload, 1);
        if (probe is null)
        {
            return false;
        }

        try
        {
            var moduleCount = Math.Max(1, probe.PixelWidth);
            var pixelsPerModule = Math.Clamp(desiredPrintWidth / moduleCount, 3, 10);
            var source = TryCreateQrBitmap(payload, pixelsPerModule);
            if (source is null)
            {
                return false;
            }

            BitmapSource bitmap = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            if (width > maxPrintWidth)
            {
                pixelsPerModule = Math.Max(1, maxPrintWidth / moduleCount);
                source = TryCreateQrBitmap(payload, pixelsPerModule);
                if (source is null)
                {
                    return false;
                }

                bitmap = source.Format == PixelFormats.Bgra32
                    ? source
                    : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                width = bitmap.PixelWidth;
                height = bitmap.PixelHeight;
            }

            if (width <= 0 || height <= 0)
            {
                return false;
            }

            var stride = width * 4;
            var pixels = new byte[stride * height];
            bitmap.CopyPixels(pixels, stride, 0);

            var printWidth = Math.Max(width, maxPrintWidth);
            var leftPadding = Math.Max(0, (printWidth - width) / 2);
            var widthBytes = (printWidth + 7) / 8;
            if (widthBytes > 255 || height > 65535)
            {
                return false;
            }

            var raster = new byte[widthBytes * height];
            for (var y = 0; y < height; y++)
            {
                var sourceRow = y * stride;
                var targetRow = y * widthBytes;
                for (var x = 0; x < width; x++)
                {
                    var pixel = sourceRow + x * 4;
                    var blue = pixels[pixel];
                    var green = pixels[pixel + 1];
                    var red = pixels[pixel + 2];
                    var alpha = pixels[pixel + 3];
                    var brightness = (red + green + blue) / 3;
                    if (alpha > 30 && brightness < 190)
                    {
                        var targetX = x + leftPadding;
                        raster[targetRow + targetX / 8] |= (byte)(0x80 >> (targetX % 8));
                    }
                }
            }

            var command = new[]
            {
                (byte)0x1D,
                (byte)0x76,
                (byte)0x30,
                (byte)0x00,
                (byte)(widthBytes % 256),
                (byte)(widthBytes / 256),
                (byte)(height % 256),
                (byte)(height / 256)
            };
            output.Write(command, 0, command.Length);
            output.Write(raster, 0, raster.Length);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ESC/POS raster QR failed: {ex.Message}");
            return false;
        }
    }

    private static Encoding GetReceiptPrinterEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(850);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.ASCII;
        }
    }

    private static bool SendRawToPrinter(string printerName, byte[] bytes, string jobName)
    {
        if (string.IsNullOrWhiteSpace(printerName) || bytes.Length == 0)
        {
            return false;
        }

        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
        {
            return false;
        }

        try
        {
            var document = new RawPrinterDocument
            {
                pDocName = string.IsNullOrWhiteSpace(jobName) ? "Balcao Livre PDV" : jobName,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(printer, 1, document))
            {
                return false;
            }

            try
            {
                if (!StartPagePrinter(printer))
                {
                    return false;
                }

                try
                {
                    return WritePrinter(printer, bytes, bytes.Length, out var written) && written == bytes.Length;
                }
                finally
                {
                    EndPagePrinter(printer);
                }
            }
            finally
            {
                EndDocPrinter(printer);
            }
        }
        finally
        {
            ClosePrinter(printer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class RawPrinterDocument
    {
        public string pDocName = "";
        public string? pOutputFile;
        public string pDataType = "RAW";
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] RawPrinterDocument document);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] data, int count, out int written);

    private void CloseTicket()
    {
        if (TicketLines.Count == 0)
        {
            SetStatus("Comanda vazia.");
            return;
        }

        if (CurrentBoard is { } board)
        {
            if (!TryApplyStaffFromInput(board))
            {
                return;
            }

            board.Status = board.Kind == "DELIVERY" ? "AGUARDANDO" : "CONTA";
        }

        var printText = BuildReceipt("CONFERENCIA DA CONTA", NextReceiptNumber());
        var qrAmount = GetTicketBalanceOrTotal();
        var printed = TryPrintTextToDefaultPrinter(
            printText,
            "Conferencia de conta",
            compact: IsCompactReceiptLayout(),
            qrPayload: GetReceiptQrPayload(qrAmount),
            qrCaption: GetReceiptQrCaption(qrAmount));
        RefreshTotals();
        SaveStore();
        SetStatus(printed
            ? "Conta fechada e impressa para conferencia."
            : "Conta fechada. Impressora padrao indisponivel.");
    }

    private void ReceiveTicket()
    {
        var board = CurrentBoard;
        if (board is not null && IsIFoodOrder(board))
        {
            ShowIFoodOrderActionDialog(board, isNewOrder: false);
            return;
        }

        if (!IsCashOpen())
        {
            SetStatus("Caixa fechado. Pressione F10 para abrir antes de receber pagamento.");
            return;
        }

        var finishedCounterFicha = board?.Kind == "BALCAO";
        var finishedCounterNumber = board?.Number ?? "";
        var total = TicketLines.Sum(line => line.Total);
        var paidTotal = Payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0, total - paidTotal);
        if (total <= 0)
        {
            SetStatus("Nada para receber.");
            return;
        }

        if (board is not null && !TryApplyStaffFromInput(board))
        {
            return;
        }

        var receive = ShowReceiveDialog(balance, total, paidTotal);
        if (receive is null)
        {
            SetStatus("Recebimento cancelado.");
            return;
        }

        if (receive.Value.Payment is { } payment)
        {
            Payments.Add(payment);
            _cashTotal += payment.Amount;
            paidTotal += payment.Amount;
            balance = Math.Max(0, total - paidTotal);
        }

        if (!receive.Value.Finalize && balance > 0)
        {
            SaveActiveTicketToCurrentBoard();
            RefreshTotals();
            SaveStore();
            SetStatus($"Pagamento parcial recebido. Saldo: {Money(balance)}");
            return;
        }

        var receiptText = BuildReceipt("COMPROVANTE NAO FISCAL", NextReceiptNumber());
        var path = Path.Combine(ExportDir, $"recibo-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path, receiptText, Encoding.UTF8);
        var printedReceipt = TryPrintTextToDefaultPrinter(
            receiptText,
            "Comprovante nao fiscal",
            compact: IsCompactReceiptLayout(),
            qrPayload: GetReceiptQrPayload(total),
            qrCaption: GetReceiptQrCaption(total));

        var closedLines = TicketLines.Select(CloneLine).ToList();
        var closedPayments = Payments.Select(ClonePayment).ToList();
        var whatsAppContext = board is null
            ? null
            : CreateWhatsAppSaleContext(board, closedLines, closedPayments, total, path);

        TicketLines.Clear();
        Payments.Clear();
        if (board is not null)
        {
            board.ClosedLines = closedLines;
            board.ClosedPayments = closedPayments;
            board.LastClosedAt = DateTime.Now;
            board.LastReceiptPath = path;
            board.Status = board.Kind switch
            {
                "DELIVERY" => "ENTREGUE",
                "KDS" => "ENTREGUE",
                "BALCAO" => "FINALIZADO",
                _ => "LIVRE"
            };
            board.Total = 0;
            board.Lines.Clear();
            board.Payments.Clear();
            QueuePublicMenuOrderStatusSync(board);
            ResetBoardAfterReceivedPayment(board);
            if (ReferenceEquals(board, CurrentBoard))
            {
                TableBox.Text = board.Number;
                LoadChargeInputsFromBoard(board);
                OpenInfoText.Text = BuildBoardInfo(board, 0);
            }
        }

        RefreshTotals();
        SaveStore();
        QueueWhatsAppReceipt(whatsAppContext);
        if (finishedCounterFicha)
        {
            var nextIndex = CreateNextCounterFicha();
            SelectTable(nextIndex);
            CodeBox.Focus();
            CodeBox.SelectAll();
            SetStatus(printedReceipt
                ? $"Ficha {finishedCounterNumber} recebida e impressa. Nova ficha {BoardTiles[nextIndex].Number} pronta."
                : $"Ficha {finishedCounterNumber} recebida. Impressora padrao indisponivel. Nova ficha {BoardTiles[nextIndex].Number} pronta.");
            return;
        }

        SetStatus(printedReceipt
            ? $"Conta recebida e comprovante impresso. Recibo: {path}"
            : $"Conta recebida. Recibo: {path}. Impressora padrao indisponivel.");
    }

    private void ResetBoardAfterReceivedPayment(TableTile board)
    {
        board.CustomerName = "";
        board.CustomerCpf = "";
        board.Phone = "";
        board.Address = "";
        board.District = "";
        board.Driver = "";
        board.Notes = "";
        board.People = 1;
        board.ChargesEnabled = false;
        board.CouvertAmount = 0m;
        board.ServicePercent = 10m;
    }

    private (PaymentLine? Payment, bool Finalize)? ShowReceiveDialog(decimal balance, decimal total, decimal paidTotal)
    {
        (PaymentLine? Payment, bool Finalize)? result = null;
        var dialog = CreateDialog("Receber pagamento", 560, 640);
        dialog.ResizeMode = ResizeMode.NoResize;

        var hasOpenBalance = balance > 0;
        var board = CurrentBoard;
        var selectedMethod = "DINHEIRO";
        var finalizePayment = true;
        var payerBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(board?.CustomerName) ? "Cliente" : board.CustomerName,
            Margin = new Thickness(0, 4, 0, 10)
        };
        var amountBox = new TextBox
        {
            Text = hasOpenBalance ? balance.ToString("N2", Brazil) : "0,00",
            IsReadOnly = !hasOpenBalance,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 0, 10)
        };
        AttachMoneyMask(amountBox);
        var changeText = new TextBlock
        {
            Text = "Sem troco.",
            Foreground = Solid("#5B6B7A"),
            FontWeight = FontWeights.SemiBold
        };
        var changeHint = new TextBlock
        {
            Text = "Digite o valor entregue pelo cliente.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0)
        };
        var changeCard = new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 10),
            Child = new StackPanel { Children = { changeText, changeHint } }
        };
        var message = new TextBlock
        {
            Text = hasOpenBalance
                ? "Escolha a forma e confirme o recebimento."
                : "Saldo ja pago. Confirme para finalizar e imprimir.",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var confirm = DialogButton("Finalizar venda", "#03151F");
        confirm.HorizontalAlignment = HorizontalAlignment.Stretch;
        confirm.Width = double.NaN;

        var methodButtons = new List<Button>();
        var methodGrid = new UniformGrid { Columns = 3, Rows = 2, Margin = new Thickness(0, 6, 0, 10) };

        void RefreshMethodButtons()
        {
            foreach (var button in methodButtons)
            {
                var isSelected = string.Equals(button.Tag?.ToString(), selectedMethod, StringComparison.Ordinal);
                button.Background = isSelected ? Solid("#F3F7FA") : Brushes.White;
                button.BorderBrush = isSelected ? Solid("#0B3A52") : Solid("#CAD6E2");
                button.Foreground = isSelected ? Solid("#0B3A52") : Solid("#071A2C");
                button.FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold;
            }
        }

        foreach (var method in new[] { "DINHEIRO", "PIX", "CREDITO", "DEBITO", "VALE", "FIADO" })
        {
            var shortcut = method switch
            {
                "DINHEIRO" => "D",
                "PIX" => "P",
                "CREDITO" => "C",
                "DEBITO" => "B",
                "VALE" => "V",
                "FIADO" => "F",
                _ => ""
            };
            var button = new Button
            {
                Content = $"{shortcut}  {method}",
                Tag = method,
                Height = 42,
                Margin = new Thickness(0, 0, 8, 8),
                Background = Brushes.White,
                BorderBrush = Solid("#CAD6E2"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Template = RoundedButtonTemplate()
            };
            button.Click += (_, _) =>
            {
                selectedMethod = method;
                RefreshMethodButtons();
                RefreshTenderedPreview();
            };
            methodButtons.Add(button);
            methodGrid.Children.Add(button);
        }

        var finalizeCard = new Border
        {
            Background = Solid("#F3F7FA"),
            BorderBrush = Solid("#0B3A52"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Cursor = Cursors.Hand
        };
        var finalizeText = new TextBlock
        {
            Text = "Finalizar conta apos este pagamento",
            Foreground = Solid("#0B3A52"),
            FontWeight = FontWeights.Bold
        };
        var finalizeHint = new TextBlock
        {
            Text = "Conta fica fechada e o recebimento entra no caixa.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0)
        };

        void RefreshFinalizeCard()
        {
            finalizeCard.Background = finalizePayment ? Solid("#F3F7FA") : Brushes.White;
            finalizeCard.BorderBrush = finalizePayment ? Solid("#0B3A52") : Solid("#CAD6E2");
            finalizeText.Foreground = finalizePayment ? Solid("#0B3A52") : Solid("#071A2C");
            if (!hasOpenBalance)
            {
                finalizeText.Text = "Finalizar e imprimir comprovante";
                finalizeHint.Text = "A conta ja possui pagamento registrado.";
                return;
            }

            finalizeText.Text = finalizePayment
                ? "Finalizar conta se quitar o saldo"
                : "Receber parcial e manter conta aberta";
            finalizeHint.Text = finalizePayment
                ? "A conta fecha somente quando o saldo chegar a zero."
                : "Pagamento parcial registrado, saldo continua aberto.";
        }

        void RefreshTenderedPreview()
        {
            if (!hasOpenBalance)
            {
                changeText.Text = "Troco: R$ 0,00";
                changeHint.Text = "Saldo ja quitado. Confirme para imprimir/finalizar.";
                changeText.Foreground = Solid("#5B6B7A");
                return;
            }

            var tendered = ParseMoney(amountBox.Text, 0);
            if (tendered <= 0)
            {
                changeText.Text = "Troco: R$ 0,00";
                changeHint.Text = $"Saldo atual: {Money(balance)}";
                changeText.Foreground = Solid("#5B6B7A");
                return;
            }

            var change = Math.Max(0, tendered - balance);
            var remaining = Math.Max(0, balance - tendered);
            if (change > 0)
            {
                changeText.Text = $"Troco para devolver: {Money(change)}";
                changeHint.Text = selectedMethod == "DINHEIRO"
                    ? $"Cliente entregou {Money(tendered)} para uma conta de {Money(balance)}."
                    : "Troco acima do saldo e permitido somente em dinheiro.";
                changeText.Foreground = selectedMethod == "DINHEIRO" ? Solid("#0B3A52") : RedText;
                return;
            }

            if (remaining > 0)
            {
                changeText.Text = $"Saldo restante: {Money(remaining)}";
                changeHint.Text = "Para fechar a venda, receba o saldo completo.";
                changeText.Foreground = Solid("#99620D");
                return;
            }

            changeText.Text = "Troco: R$ 0,00";
            changeHint.Text = "Valor exato para finalizar a venda.";
            changeText.Foreground = Solid("#5B6B7A");
        }

        finalizeCard.MouseLeftButtonDown += (_, _) =>
        {
            finalizePayment = !finalizePayment;
            RefreshFinalizeCard();
        };
        finalizeCard.Child = new StackPanel
        {
            Children =
            {
                finalizeText,
                finalizeHint
            }
        };

        async void Confirm()
        {
            if (!hasOpenBalance)
            {
                result = (null, true);
                dialog.Close();
                return;
            }

            var amount = ParseMoney(amountBox.Text, 0);
            if (amount <= 0)
            {
                message.Text = "Valor recebido invalido.";
                message.Foreground = RedText;
                amountBox.Focus();
                amountBox.SelectAll();
                return;
            }

            if (amount > balance && selectedMethod != "DINHEIRO")
            {
                message.Text = "Troco acima do saldo somente em DINHEIRO.";
                message.Foreground = RedText;
                amountBox.Focus();
                amountBox.SelectAll();
                return;
            }

            var appliedAmount = Math.Min(amount, balance);
            var changeAmount = Math.Max(0, amount - balance);
            var payer = string.IsNullOrWhiteSpace(payerBox.Text) ? "Cliente" : payerBox.Text.Trim();

            if (selectedMethod is "CREDITO" or "DEBITO" or "PIX"
                && (_appSettings.MercadoPago.Enabled || _appSettings.PagBank.Enabled))
            {
                confirm.IsEnabled = false;
                message.Foreground = Solid("#5B6B7A");
                message.Text = "Preparando cobranca integrada...";
                var integratedPayment = await ProcessIntegratedPaymentAsync(selectedMethod, appliedAmount, payer, dialog);
                confirm.IsEnabled = true;
                if (integratedPayment is null)
                {
                    message.Foreground = RedText;
                    message.Text = "Pagamento integrado nao confirmado. A venda continua aberta.";
                    return;
                }

                result = (integratedPayment, finalizePayment && appliedAmount >= balance);
                dialog.Close();
                return;
            }

            result = (new PaymentLine
            {
                Payer = payer,
                Method = selectedMethod,
                Amount = appliedAmount,
                TenderedAmount = amount,
                ChangeAmount = changeAmount,
                When = DateTime.Now
            }, finalizePayment && appliedAmount >= balance);
            dialog.Close();
        }

        confirm.Click += (_, _) => Confirm();
        bool SelectPaymentMethodByShortcut(KeyEventArgs e)
        {
            if (Keyboard.FocusedElement == payerBox)
            {
                return false;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != ModifierKeys.None)
            {
                return false;
            }

            var method = key switch
            {
                Key.D => "DINHEIRO",
                Key.P => "PIX",
                Key.C => "CREDITO",
                Key.B => "DEBITO",
                Key.V => "VALE",
                Key.F => "FIADO",
                Key.F1 => "DINHEIRO",
                Key.F2 => "PIX",
                Key.F3 => "CREDITO",
                Key.F4 => "DEBITO",
                Key.F5 => "VALE",
                Key.F6 => "FIADO",
                _ => ""
            };
            if (string.IsNullOrWhiteSpace(method))
            {
                return false;
            }

            selectedMethod = method;
            RefreshMethodButtons();
            RefreshTenderedPreview();
            amountBox.Focus();
            amountBox.CaretIndex = amountBox.Text.Length;
            return true;
        }

        dialog.PreviewKeyDown += (_, e) =>
        {
            if (SelectPaymentMethodByShortcut(e))
            {
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                Confirm();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
                e.Handled = true;
            }
        };

        amountBox.TextChanged += (_, _) => RefreshTenderedPreview();

        var panel = new StackPanel { Margin = new Thickness(18, 18, 18, 8) };
        panel.Children.Add(new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14, 11, 14, 11),
            Margin = new Thickness(0, 0, 0, 14),
            Child = new Grid
            {
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "Total / pago / saldo", Foreground = Solid("#5B6B7A"), FontSize = 12, FontWeight = FontWeights.SemiBold },
                            new TextBlock { Text = $"{Money(total)}  |  Pago {Money(paidTotal)}", Foreground = Solid("#071A2C"), FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) },
                            new TextBlock { Text = Money(balance), Foreground = hasOpenBalance ? Solid("#0B3A52") : Solid("#5B6B7A"), FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 0) }
                        }
                    }
                }
            }
        });
        panel.Children.Add(DialogLabel("Pagante"));
        panel.Children.Add(payerBox);
        panel.Children.Add(DialogLabel("Forma de pagamento"));
        panel.Children.Add(methodGrid);
        panel.Children.Add(DialogLabel("Valor recebido"));
        panel.Children.Add(amountBox);
        panel.Children.Add(new TextBlock
        {
            Text = "Digite no teclado numerico: 20000 vira R$ 200,00.",
            Foreground = Solid("#5B6B7A"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, -4, 0, 8)
        });
        panel.Children.Add(changeCard);
        panel.Children.Add(finalizeCard);
        panel.Children.Add(message);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false
        });

        var footer = new Border
        {
            Background = Solid("#FFFFFF"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(18, 10, 18, 10),
            Child = confirm
        };
        Grid.SetRow(footer, 1);
        root.Children.Add(footer);

        dialog.Content = root;
        RefreshMethodButtons();
        RefreshFinalizeCard();
        RefreshTenderedPreview();
        amountBox.Focus();
        amountBox.SelectAll();
        dialog.ShowDialog();
        return result;
    }

    private int NextReceiptNumber()
    {
        _appSettings.ReceiptSequence = Math.Max(0, _appSettings.ReceiptSequence) + 1;
        SaveAppSettings();
        return _appSettings.ReceiptSequence;
    }

    private string BuildStaffReceiptLine(TableTile board)
    {
        if (board.Waiter <= 0)
        {
            return "";
        }

        var role = board.Kind == "BALCAO" ? "CAIXA" : "GARCOM";
        var label = board.Kind == "BALCAO" ? "OPERADOR" : "GARCOM";
        var staff = FindStaffByNumber(board.Waiter, role);
        return staff is null
            ? $"{label}: {board.Waiter}"
            : $"{label}: {board.Waiter} - {staff.Name}";
    }

    private UserAccount? FindStaffByNumber(int number, string preferredRole)
    {
        if (number <= 0)
        {
            return null;
        }

        var key = number.ToString(Brazil);
        return Users.FirstOrDefault(user =>
                   string.Equals(user.Role, preferredRole, StringComparison.OrdinalIgnoreCase)
                   && StaffNumber(user) == key)
               ?? Users.FirstOrDefault(user => StaffNumber(user) == key);
    }

    private bool IsCompactReceiptLayout()
    {
        return string.Equals(_appSettings.PrintLayout, "PEQUENO", StringComparison.OrdinalIgnoreCase);
    }

    private int GetReceiptTextWidth()
    {
        return IsCompactReceiptLayout() ? 30 : 28;
    }

    private decimal GetTicketBalanceOrTotal()
    {
        var total = TicketLines.Sum(line => line.Total);
        var paid = Payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0, total - paid);
        return balance > 0 ? balance : total;
    }

    private string GetReceiptQrPayload(decimal amount)
    {
        if (!_appSettings.ReceiptQrEnabled)
        {
            return "";
        }

        var content = (_appSettings.ReceiptQrContent ?? "").Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return BuildReceiptQrFallbackPayload(amount);
        }

        return NormalizeReceiptQrKind(_appSettings.ReceiptQrKind) switch
        {
            "PIX" => BuildPixQrPayload(content, amount),
            "INSTAGRAM" => NormalizeInstagramQrContent(content),
            "GOOGLE MAPS" => NormalizeUrlQrContent(content),
            "LINK" => NormalizeUrlQrContent(content),
            _ => content
        };
    }

    private string GetReceiptQrCaption(decimal amount)
    {
        var kind = NormalizeReceiptQrKind(_appSettings.ReceiptQrKind);
        if (string.IsNullOrWhiteSpace(_appSettings.ReceiptQrContent))
        {
            return "DADOS DA LOJA";
        }

        if (kind == "PIX" && amount > 0)
        {
            return $"PIX {Money(amount)}";
        }

        return kind switch
        {
            "INSTAGRAM" => "INSTAGRAM",
            "GOOGLE MAPS" => "GOOGLE MAPS",
            "LINK" => "QR CODE",
            _ => "PIX"
        };
    }

    private string BuildReceiptQrFallbackPayload(decimal amount)
    {
        var lines = new List<string>
        {
            string.IsNullOrWhiteSpace(_profile.BusinessName) ? AppReceiptName : _profile.BusinessName.Trim()
        };

        if (amount > 0)
        {
            lines.Add($"TOTAL: {Money(amount)}");
        }

        if (!string.IsNullOrWhiteSpace(_profile.Cnpj))
        {
            lines.Add($"CNPJ: {_profile.Cnpj.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(_profile.Phone))
        {
            lines.Add($"TEL: {_profile.Phone.Trim()}");
        }

        var location = string.Join(" - ", new[] { _profile.Address, _profile.City, _profile.State }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim()));
        if (!string.IsNullOrWhiteSpace(location))
        {
            lines.Add(location);
        }

        return string.Join("\n", lines);
    }

    private static string NormalizeInstagramQrContent(string content)
    {
        content = content.Trim();
        if (content.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            content.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        content = content.TrimStart('@').Trim();
        if (content.StartsWith("instagram.com", StringComparison.OrdinalIgnoreCase) ||
            content.StartsWith("www.instagram.com", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{content}";
        }

        return $"https://instagram.com/{content}";
    }

    private static string NormalizeUrlQrContent(string content)
    {
        content = content.Trim();
        if (content.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            content.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            content.StartsWith("geo:", StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        return $"https://{content}";
    }

    private string BuildPixQrPayload(string content, decimal amount)
    {
        content = content.Trim();
        if (content.StartsWith("000201", StringComparison.OrdinalIgnoreCase))
        {
            return BuildPixPayloadWithAmount(content, amount);
        }

        var pixKey = NormalizePixKey(content);
        var merchantName = NormalizePixText(_profile.BusinessName, 25, AppReceiptName);
        var city = NormalizePixText(_profile.City, 15, "BRASIL");
        var merchantAccount = Emv("00", "BR.GOV.BCB.PIX") + Emv("01", pixKey);
        var additionalData = Emv("05", "***");
        var payloadWithoutCrc =
            Emv("00", "01") +
            Emv("01", "11") +
            Emv("26", merchantAccount) +
            Emv("52", "0000") +
            Emv("53", "986") +
            BuildPixAmountField(amount) +
            Emv("58", "BR") +
            Emv("59", merchantName) +
            Emv("60", city) +
            Emv("62", additionalData) +
            "6304";

        return payloadWithoutCrc + PixCrc16(payloadWithoutCrc);
    }

    private static string BuildPixAmountField(decimal amount)
    {
        if (amount <= 0)
        {
            return "";
        }

        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        return Emv("54", rounded.ToString("0.00", CultureInfo.InvariantCulture));
    }

    private static string BuildPixPayloadWithAmount(string payload, decimal amount)
    {
        var clean = new string(payload.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (amount <= 0)
        {
            return clean;
        }

        var body = StripPixCrc(clean);
        var amountField = BuildPixAmountField(amount);
        var fields = new List<(string Id, string Raw)>();
        var index = 0;
        while (index + 4 <= body.Length)
        {
            var id = body.Substring(index, 2);
            if (!int.TryParse(body.Substring(index + 2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var length)
                || length < 0
                || index + 4 + length > body.Length)
            {
                return clean;
            }

            if (id == "63")
            {
                break;
            }

            var raw = body.Substring(index, 4 + length);
            if (id != "54")
            {
                fields.Add((id, raw));
            }

            index += 4 + length;
        }

        if (index != body.Length)
        {
            return clean;
        }

        var rebuilt = new StringBuilder();
        var inserted = false;
        foreach (var field in fields)
        {
            if (!inserted && string.CompareOrdinal(field.Id, "54") > 0)
            {
                rebuilt.Append(amountField);
                inserted = true;
            }

            rebuilt.Append(field.Raw);
        }

        if (!inserted)
        {
            rebuilt.Append(amountField);
        }

        var withoutCrc = rebuilt + "6304";
        return withoutCrc + PixCrc16(withoutCrc);
    }

    private static string StripPixCrc(string payload)
    {
        var index = 0;
        while (index + 4 <= payload.Length)
        {
            var id = payload.Substring(index, 2);
            if (!int.TryParse(payload.Substring(index + 2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var length)
                || length < 0
                || index + 4 + length > payload.Length)
            {
                return payload;
            }

            if (id == "63" && length == 4)
            {
                return payload[..index];
            }

            index += 4 + length;
        }

        return payload;
    }

    private static string NormalizePixKey(string content)
    {
        var value = content.Trim();
        if (value.Contains('@'))
        {
            return value.ToLowerInvariant();
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (value.StartsWith('+') && digits.Length >= 10)
        {
            return $"+{digits}";
        }

        if (digits.Length is 11 or 14)
        {
            return digits;
        }

        if (digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            return $"+{digits}";
        }

        return value.Replace(" ", "", StringComparison.Ordinal);
    }

    private static string NormalizePixText(string value, int maxLength, string fallback)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c) || c is ' ' or '.' or '-' or '&')
            {
                sb.Append(char.ToUpperInvariant(c));
            }
        }

        var text = string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(text))
        {
            text = fallback;
        }

        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string NormalizeProductLookupText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToUpperInvariant(c));
            }
        }

        return sb.ToString();
    }

    private static string Emv(string id, string value)
    {
        value ??= "";
        return $"{id}{Encoding.ASCII.GetByteCount(value):00}{value}";
    }

    private static string PixCrc16(string payload)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ 0x1021)
                    : (ushort)(crc << 1);
            }
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    private string BuildReceipt(string documentTitle = "COMPROVANTE NAO FISCAL", int documentNumber = 0)
    {
        var board = CurrentBoard;
        var sb = new StringBuilder();
        var width = GetReceiptTextWidth();
        var divider = new string('-', width);
        var displayName = string.IsNullOrWhiteSpace(_profile.BusinessName)
            ? AppReceiptName
            : _profile.BusinessName.Trim().ToUpperInvariant();
        sb.AppendLine(CenterReceipt(displayName, width));
        if (!string.IsNullOrWhiteSpace(_profile.LegalName))
        {
            foreach (var line in WrapReceipt(_profile.LegalName.ToUpperInvariant(), width))
            {
                sb.AppendLine(CenterReceipt(line, width));
            }
        }

        if (!string.IsNullOrWhiteSpace(_profile.Cnpj))
        {
            sb.AppendLine(CenterReceipt($"CNPJ: {_profile.Cnpj.Trim()}", width));
        }

        if (!string.IsNullOrWhiteSpace(_profile.Phone))
        {
            sb.AppendLine(CenterReceipt($"TEL: {_profile.Phone.Trim()}", width));
        }

        var location = string.Join(" - ", new[] { _profile.Address, _profile.City, _profile.State }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim()));
        if (!string.IsNullOrWhiteSpace(location))
        {
            foreach (var line in WrapReceipt(location.ToUpperInvariant(), width))
            {
                sb.AppendLine(CenterReceipt(line, width));
            }
        }

        sb.AppendLine();
        sb.AppendLine(CenterReceipt($"COO {documentNumber:000000}", width));
        foreach (var line in WrapReceipt(documentTitle.ToUpperInvariant(), width))
        {
            sb.AppendLine(CenterReceipt(line, width));
        }

        sb.AppendLine(CenterReceipt("NAO E DOCUMENTO FISCAL", width));
        sb.AppendLine(divider);
        if (board is null)
        {
            sb.AppendLine("COMANDA");
        }
        else
        {
            sb.AppendLine(ClipReceipt($"{BoardKindLabel(board)} {board.Number}", width));
            var staffLine = BuildStaffReceiptLine(board);
            if (!string.IsNullOrWhiteSpace(staffLine))
            {
                foreach (var line in WrapReceipt(staffLine.ToUpperInvariant(), width))
                {
                    sb.AppendLine(line);
                }
            }

            if (!string.IsNullOrWhiteSpace(board.CustomerName))
            {
                foreach (var customerLine in WrapReceipt($"CLIENTE: {board.CustomerName.ToUpperInvariant()}", width))
                {
                    sb.AppendLine(customerLine);
                }
            }

            if (!string.IsNullOrWhiteSpace(board.CustomerCpf))
            {
                foreach (var cpfLine in WrapReceipt($"CPF/CNPJ: {board.CustomerCpf}", width))
                {
                    sb.AppendLine(cpfLine);
                }
            }

            if (!string.IsNullOrWhiteSpace(board.Address))
            {
                foreach (var addressLine in WrapReceipt($"END: {board.Address.ToUpperInvariant()}", width))
                {
                    sb.AppendLine(addressLine);
                }
            }

            if (!string.IsNullOrWhiteSpace(board.District))
            {
                foreach (var districtLine in WrapReceipt($"BAIRRO/REF: {board.District.ToUpperInvariant()}", width))
                {
                    sb.AppendLine(districtLine);
                }
            }
        }

        sb.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", Brazil));
        sb.AppendLine(divider);
        sb.AppendLine("DESCRICAO DO PRODUTO");

        foreach (var line in TicketLines)
        {
            foreach (var productLine in WrapReceipt(line.Name.ToUpperInvariant(), width))
            {
                sb.AppendLine(productLine);
            }

            var quantity = line.Quantity.ToString("N3", Brazil);
            var left = $"{quantity} x {MoneyReceipt(line.UnitPrice)}";
            sb.AppendLine(ReceiptColumns(left, MoneyReceipt(line.Total), width));
        }

        sb.AppendLine(divider);

        var total = TicketLines.Sum(line => line.Total);
        var paid = Payments.Sum(payment => payment.Amount);
        var tenderedTotal = Payments.Sum(payment => payment.TenderedAmount > 0 ? payment.TenderedAmount : payment.Amount);
        var explicitChange = Payments.Sum(payment => payment.ChangeAmount);
        sb.AppendLine(ReceiptColumns("TOTAL", MoneyReceipt(total), width));
        if (Payments.Count == 0)
        {
            sb.AppendLine(ReceiptColumns("SALDO", MoneyReceipt(total), width));
        }
        else
        {
            foreach (var payment in Payments.GroupBy(item => item.Method).OrderBy(item => item.Key))
            {
                sb.AppendLine(ReceiptColumns(payment.Key.ToUpperInvariant(), MoneyReceipt(payment.Sum(item => item.Amount)), width));
            }

            if (tenderedTotal > paid)
            {
                sb.AppendLine(ReceiptColumns("RECEBIDO", MoneyReceipt(tenderedTotal), width));
            }

            sb.AppendLine(ReceiptColumns("TROCO", MoneyReceipt(Math.Max(explicitChange, paid - total)), width));
            var balance = Math.Max(0, total - paid);
            if (balance > 0)
            {
                sb.AppendLine(ReceiptColumns("SALDO", MoneyReceipt(balance), width));
            }
        }

        sb.AppendLine(divider);
        sb.AppendLine(CenterReceipt($"CONTROLE {documentNumber:000000}", width));
        sb.AppendLine(CenterReceipt(BuildReceiptBars(documentNumber), width));
        sb.AppendLine();
        sb.AppendLine(CenterReceipt("OBRIGADO PELA PREFERENCIA", width));
        return sb.ToString();
    }

    private static IEnumerable<string> WrapReceipt(string value, int width)
    {
        value = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        while (value.Length > width)
        {
            var splitAt = value.LastIndexOf(' ', width);
            if (splitAt <= 0)
            {
                splitAt = width;
            }

            yield return value[..splitAt].Trim();
            value = value[splitAt..].Trim();
        }

        yield return value;
    }

    private static string CenterReceipt(string value, int width)
    {
        value = ClipReceipt(value, width);
        if (value.Length >= width)
        {
            return value;
        }

        var left = (width - value.Length) / 2;
        return new string(' ', left) + value;
    }

    private static string ClipReceipt(string value, int width)
    {
        value = (value ?? string.Empty).Trim();
        return value.Length <= width ? value : value[..width];
    }

    private static string ReceiptColumns(string left, string right, int width)
    {
        left = (left ?? string.Empty).Trim();
        right = (right ?? string.Empty).Trim();
        if (right.Length >= width)
        {
            return ClipReceipt(right, width);
        }

        var availableLeft = Math.Max(0, width - right.Length - 1);
        left = ClipReceipt(left, availableLeft);
        var spaces = Math.Max(1, width - left.Length - right.Length);
        return left + new string(' ', spaces) + right;
    }

    private static string MoneyReceipt(decimal value)
    {
        return value.ToString("C", Brazil).Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string BuildReceiptBars(int documentNumber)
    {
        var digits = Math.Abs(documentNumber).ToString("000000", CultureInfo.InvariantCulture);
        var bars = new StringBuilder();
        foreach (var digit in digits)
        {
            var size = ((digit - '0') % 4) + 1;
            bars.Append(new string('|', size));
            bars.Append(' ');
        }

        return bars.ToString().Trim();
    }

    private void RefreshTotals()
    {
        var ticketTotal = TicketLines.Sum(line => line.Total);
        var paidTotal = Payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0, ticketTotal - paidTotal);
        TicketTotalText.Text = Money(balance);
        CashTotalText.Text = Money(_cashTotal);
        CashStatusText.Text = IsCashOpen() ? "Caixa aberto" : "Caixa fechado";
        CashStatusText.Foreground = IsCashOpen() ? Solid("#5B6B7A") : RedText;
        TodaySalesText.Text = $"Vendas hoje: {Money(GetTodaySalesTotal())}";
        if (CurrentBoard is { } board)
        {
            board.Total = ticketTotal;
            OpenInfoText.Text = BuildBoardInfo(board, paidTotal);
        }

        TablesList.Items.Refresh();
        ProductsList.Items.Refresh();
        CategoriesList.Items.Refresh();
        PaymentsList.Items.Refresh();
        RefreshChargeToggleButton();
    }

    private void RefreshIFoodDeliveryLockUi(TableTile? board)
    {
        var locked = IsIFoodDeliveryBoard(board);
        var canEditProducts = !locked;

        CodeBox.IsEnabled = canEditProducts;
        QuantityBox.IsEnabled = canEditProducts;
        SearchBox.IsEnabled = canEditProducts;
        ProductsList.IsEnabled = canEditProducts;
        PriceBox.IsEnabled = canEditProducts;
        NoteBox.IsEnabled = canEditProducts;
        ChargeToggleButton.IsEnabled = canEditProducts;

        SearchButton.Content = locked ? "F9 Acoes iFood" : "F3 Catalogo";
        SearchButton.ToolTip = locked
            ? "Abrir acoes do pedido iFood: confirmar, preparar, pronto, despachar ou cancelar."
            : "Pesquisar produto";

        KeyboardText.Text = locked
            ? "Pedido iFood: F9 abre acoes/despacho  |  Itens bloqueados para edicao manual  |  F10 abrir/fechar caixa"
            : "Tab troca area: Comanda > Mesas/Fichas > Venda rapida  |  Enter inclui  |  F3 catalogo  |  Excluir na linha do item  |  F10 abrir/fechar caixa";

        foreach (var line in TicketLines)
        {
            line.CanDelete = canEditProducts;
        }

        TicketList.Items.Refresh();

        if (locked)
        {
            if (!string.IsNullOrEmpty(CodeBox.Text))
            {
                CodeBox.Text = "";
            }

            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = "";
            }

            SelectedProductText.Text = "Pedido iFood bloqueado para edicao. Use F9 ou Acoes iFood para confirmar/despachar.";
            SelectArea(KeyboardArea.Ticket);
        }
    }

    private decimal GetTodaySalesTotal()
    {
        var today = DateTime.Today;
        var boards = Tables.Concat(DeliveryTiles).ToList();
        var paymentsTotal = boards
            .SelectMany(board => board.ClosedPayments.Concat(board.Payments))
            .Where(payment => payment.When.Date == today)
            .Sum(payment => payment.Amount);
        if (paymentsTotal > 0)
        {
            return paymentsTotal;
        }

        return boards
            .Where(board => board.LastClosedAt.HasValue && board.LastClosedAt.Value.Date == today)
            .SelectMany(board => board.ClosedLines)
            .Sum(line => line.Total);
    }

    private void RefreshChargeToggleButton()
    {
        if (ChargeToggleButton is null)
        {
            return;
        }

        var active = HasAppliedTableCharges();
        ChargeToggleButton.Content = active ? "Desativar" : "Ativar";
        ChargeToggleButton.Background = active ? RedText : Solid("#08A99B");
        ChargeToggleButton.BorderBrush = ChargeToggleButton.Background;
    }

    private static string BoardKindLabel(TableTile board)
    {
        return board.Kind switch
        {
            "BALCAO" => "FICHA",
            "DELIVERY" => "PEDIDO",
            "KDS" => "KDS",
            _ => "MESA"
        };
    }

    private string BuildBoardInfo(TableTile board, decimal paidTotal)
    {
        var customer = string.IsNullOrWhiteSpace(board.CustomerName) ? "" : $"  |  Cliente {board.CustomerName}";
        var cash = IsCashOpen() ? "Caixa aberto" : "Caixa fechado";
        return $"{cash}  |  {BoardKindLabel(board)} {board.Number}  |  {board.Status}{customer}  |  Pago {Money(paidTotal)}";
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
        if (TryGetToastTitle(text, out var title, out var code, out var color, out var softColor))
        {
            ShowToast(title, text, code, color, softColor);
        }
    }

    private void ShowToast(string title, string message, string code, string color, string softColor)
    {
        var suppressSound = _suppressNextToastSound;
        _suppressNextToastSound = false;
        if (!_appSettings.WindowsNotificationsEnabled)
        {
            if (!suppressSound) PlayNotificationSound();
            VibrateInApp();
            return;
        }

        ToastTitleText.Text = title;
        ToastMessageText.Text = message;
        ToastCodeText.Text = code;
        ToastAccent.Background = Solid(color);
        ToastCodeText.Foreground = Solid(color);

        if (ToastCodeText.Parent is Border badge)
        {
            badge.Background = Solid(softColor);
        }

        ToastHost.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
        if (!suppressSound) PlayNotificationSound();
        VibrateInApp();
    }

    private void HideToast()
    {
        _toastTimer.Stop();
        ToastHost.Visibility = Visibility.Collapsed;
    }

    private void PlayNotificationSound()
    {
        if (!_appSettings.NotificationSoundEnabled || _appSettings.NotificationSound == "NENHUM")
        {
            return;
        }

        try
        {
            switch (_appSettings.NotificationSound)
            {
                case "AVISO":
                    SystemSounds.Asterisk.Play();
                    break;
                case "ERRO":
                    SystemSounds.Exclamation.Play();
                    break;
                default:
                    SystemSounds.Beep.Play();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Notification sound failed: {ex.Message}");
        }
    }

    private void VibrateInApp()
    {
        if (!_appSettings.InAppVibrationEnabled)
        {
            return;
        }

        try
        {
            if (ToastHost.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                ToastHost.RenderTransform = transform;
            }

            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(210)
            };
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(-7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(35))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(105))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(210))));
            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"In-app vibration failed: {ex.Message}");
        }
    }

    private static bool TryGetToastTitle(string text, out string title, out string code, out string color, out string softColor)
    {
        var lower = text.ToLowerInvariant();
        title = "";
        code = "OK";
        color = "#08A99B";
        softColor = "#E6FBF8";

        if (lower.Contains("bloquead") || lower.Contains("pendente"))
        {
            title = "Bloqueado";
            code = "BL";
            color = "#A11D1D";
            softColor = "#FFE2DF";
            return true;
        }

        if (lower.Contains("exclu") || lower.Contains("cancelad") || lower.Contains("removid"))
        {
            title = "Excluido";
            code = "EX";
            color = "#A11D1D";
            softColor = "#FFE2DF";
            return true;
        }

        if (lower.Contains("atualiz"))
        {
            title = "Atualizado";
            code = "AT";
            return true;
        }

        if (lower.Contains("salv"))
        {
            title = "Salvo";
            code = "SV";
            return true;
        }

        if (lower.Contains("cadastrad") || lower.Contains("criad") || lower.Contains("incluid"))
        {
            title = "Criado";
            code = "CR";
            return true;
        }

        if (lower.Contains("gerad") || lower.Contains("exportad"))
        {
            title = "Arquivo gerado";
            code = "AR";
            color = "#0B3A52";
            softColor = "#EAF8FA";
            return true;
        }

        if (lower.Contains("aplicad") || lower.Contains("vinculad") || lower.Contains("recebid") || lower.Contains("transferid") || lower.Contains("reabert"))
        {
            title = "Concluido";
            return true;
        }

        return false;
    }

    private void SeedStaticUi()
    {
        SeedDefaultRibbonActions();

        Modes.Add("Comandas");
        Modes.Add("Balcao");
        Modes.Add("Delivery");
    }

    private void SeedDefaultRibbonActions()
    {
        RibbonActions.Clear();
        RibbonActions.Add(new("SearchProducts", "PS", "Pesquisa", "Produtos"));
        RibbonActions.Add(new("TransferProducts", "F6", "Transferir", "Comanda"));
        RibbonActions.Add(new("Discount", "DS", "Desconto", "Permissao"));
        RibbonActions.Add(new("ChangeClient", "CL", "Cadastro", "Clientes"));
        RibbonActions.Add(new("ReopenCommand", "RC", "Reabrir", "Gerente"));
        RibbonActions.Add(new("PeopleCount", "EQ", "Equipe", "Garcom/Caixa"));
        RibbonActions.Add(new("ProductCatalog", "CP", "Cadastro", "Produtos"));
        RibbonActions.Add(new("Cash", "CX", "Caixa", "Movimentos"));
        RibbonActions.Add(new("CloseCash", "F10", "Abrir/Fechar", "Caixa"));
        RibbonActions.Add(new("DeliveryNew", "DL", "Novo", "Delivery"));
        RibbonActions.Add(new("DeliveryZones", "TZ", "Taxas", "Delivery"));
        RibbonActions.Add(new("IFood", "IF", "iFood", "Pedidos"));
        RibbonActions.Add(new("WhatsApp", "WA", "Ativar", "WhatsApp"));
        RibbonActions.Add(new("WaiterWeb", "GW", "Garcom", "Web"));
        RibbonActions.Add(new("Inventory", "ES", "Estoque", "Receitas"));
        RibbonActions.Add(new("Cardapio", "QR", "Cardapio", "Digital"));
        RibbonActions.Add(new("Reports", "BI", "Relatorios", "Vendas"));
        RibbonActions.Add(new("Backup", "BK", "Backup", "Dados"));
    }

    private void EnsureOnlineRibbonActions()
    {
        NormalizeWhatsAppRibbonAction();
        if (RibbonActions.Any(action => action.Id == "IFood")
            && RibbonActions.Any(action => action.Id == "WaiterWeb")
            && RibbonActions.Any(action => action.Id == "DeliveryZones")
            && RibbonActions.Any(action => action.Id == "WhatsApp"))
        {
            return;
        }

        var insertAt = RibbonActions
            .Select((action, index) => new { action.Id, index })
            .FirstOrDefault(item => item.Id == "Inventory")?.index ?? RibbonActions.Count;
        if (RibbonActions.All(action => action.Id != "DeliveryZones"))
        {
            RibbonActions.Insert(insertAt, new RibbonAction("DeliveryZones", "TZ", "Taxas", "Delivery"));
            insertAt++;
        }

        if (RibbonActions.All(action => action.Id != "IFood"))
        {
            RibbonActions.Insert(insertAt, new RibbonAction("IFood", "IF", "iFood", "Pedidos"));
            insertAt++;
        }

        if (RibbonActions.All(action => action.Id != "WhatsApp"))
        {
            RibbonActions.Insert(insertAt, new RibbonAction("WhatsApp", "WA", "Ativar", "WhatsApp"));
            insertAt++;
        }
        else
        {
            NormalizeWhatsAppRibbonAction();
        }

        if (RibbonActions.All(action => action.Id != "WaiterWeb"))
        {
            RibbonActions.Insert(insertAt, new RibbonAction("WaiterWeb", "GW", "Garcom", "Web"));
        }
    }

    private void NormalizeWhatsAppRibbonAction()
    {
        for (var i = 0; i < RibbonActions.Count; i++)
        {
            if (RibbonActions[i].Id == "WhatsApp")
            {
                RibbonActions[i] = RibbonActions[i] with { KeyText = "WA", Title = "Ativar", Subtitle = "WhatsApp" };
                return;
            }
        }
    }

    private void SeedStore()
    {
        Tables.Clear();

        DeliveryTiles.Clear();

        KitchenTiles.Clear();

        Categories.Clear();
        Categories.Add(new("BEBIDAS"));
        Categories.Add(new("REFEICOES"));
        Categories.Add(new("PIZZAS"));
        Categories.Add(new("COMPOSICOES"));
        Categories.Add(new("DELIVERY"));
        Categories.Add(new("SOBREMESAS"));

        Products.Clear();

        TicketLines.Clear();
        Payments.Clear();
        Users.Clear();
        Users.Add(new UserAccount
        {
            Name = "MASTER",
            Pin = "1234",
            EmployeeNumber = "0",
            Role = "MASTER",
            IsMaster = true,
            CanTransfer = true,
            CanCancel = true,
            CanDiscount = true,
            CanManageProducts = true,
            CanReports = true,
            CanCash = true
        });
        Users.Add(new UserAccount { Name = "CAIXA", Pin = "1111", EmployeeNumber = "1", Role = "CAIXA", CanCash = true, CanCancel = true, CanDiscount = true });
        Users.Add(new UserAccount { Name = "GARCOM", Pin = "2222", EmployeeNumber = "1", Role = "GARCOM", CanTransfer = true });
        foreach (var user in Users)
        {
            NormalizeLoadedUser(user);
        }

        Drivers.Clear();

        Customers.Clear();
        WhatsAppHistory.Clear();
        WhatsAppPendingOrders.Clear();
        CashMovements.Clear();
        _settings = new LocalHubSettings();
        _profile = new RestaurantIdentityProfile();
        _appSettings = new AppSettings();
        _cashTotal = 0m;
    }

    private void LoadStore()
    {
        if (TryLoadStoreFromPath(StoreFile, out var store) && store is not null && ApplyStore(store))
        {
            WriteStoreFile(CreateStoreSnapshot());
            return;
        }

        if (TryRecoverStoreFromBackups(out var recoveredPath))
        {
            WriteStoreFile(CreateStoreSnapshot());
            SetStatus($"Dados recuperados do backup local: {recoveredPath}");
            return;
        }

        SeedStore();
        CreateLocalStoreBackup("primeira-carga", force: true);
    }

    private void SaveStore()
    {
        SaveActiveTicketToCurrentBoard();
        var store = CreateStoreSnapshot();
        WriteStoreFile(store);
        QueueCentralSync("store.saved", store);
        QueueCloudBackup(store);
        QueuePublicMenuPublish();
    }

    private AppStore CreateStoreSnapshot()
    {
        return new AppStore
        {
            Profile = _profile,
            AppSettings = _appSettings,
            RibbonActions = RibbonActions.ToList(),
            Tables = Tables.ToList(),
            DeliveryTiles = DeliveryTiles.ToList(),
            KitchenTiles = KitchenTiles.ToList(),
            Categories = Categories.ToList(),
            Products = Products.ToList(),
            Users = Users.ToList(),
            Drivers = Drivers.ToList(),
            Customers = Customers.ToList(),
            CashMovements = CashMovements.ToList(),
            WhatsAppHistory = WhatsAppHistory.Take(500).ToList(),
            WhatsAppPendingOrders = WhatsAppPendingOrders.Take(100).ToList(),
            Settings = _settings,
            CashTotal = _cashTotal
        };
    }

    private bool ApplyStore(AppStore store)
    {
        if (store.RibbonActions.Count > 0)
        {
            RibbonActions.Clear();
            foreach (var item in store.RibbonActions.Where(action => action.Id is not "Users" and not "DeleteCommand" and not "FiscalTef"))
            {
                RibbonActions.Add(item.Id switch
                {
                    "ChangeClient" => item with { Title = "Cadastro", Subtitle = "Clientes" },
                    "TransferProducts" => item with { KeyText = "F6", Title = "Transferir", Subtitle = "Comanda" },
                    "ReopenCommand" => item with { KeyText = "RC", Title = "Reabrir", Subtitle = "Gerente" },
                    "PeopleCount" => item with { KeyText = "EQ", Title = "Equipe", Subtitle = "Garcom/Caixa" },
                    "WhatsApp" => item with { KeyText = "WA", Title = "Ativar", Subtitle = "WhatsApp" },
                    _ => item
                });
            }

            EnsureOnlineRibbonActions();
        }
        else if (RibbonActions.Count == 0)
        {
            SeedDefaultRibbonActions();
        }

        if (store.Profile is not null)
        {
            _profile = store.Profile;
            SaveRestaurantProfile();
            ApplyRestaurantIdentity();
        }

        if (store.AppSettings is not null)
        {
            var loadedAppSettings = _appSettings;
            _appSettings = store.AppSettings;
            MergeWhatsAppSendPulseSettings(loadedAppSettings.WhatsApp, _appSettings.WhatsApp ??= new WhatsAppSettings());
            NormalizeWhatsAppSendPulseOnlySettings();
            NormalizeProductionDestinations();
            SaveAppSettings();
        }

        Tables.Clear();
        foreach (var item in store.Tables)
        {
            if (string.IsNullOrWhiteSpace(item.Kind)) item.Kind = "MESA";
            NormalizeLoadedBoard(item);
            Tables.Add(item);
        }

        DeliveryTiles.Clear();
        foreach (var item in store.DeliveryTiles)
        {
            item.Kind = "DELIVERY";
            NormalizeLoadedBoard(item);
            DeliveryTiles.Add(item);
        }

        KitchenTiles.Clear();
        foreach (var item in store.KitchenTiles)
        {
            item.Kind = "KDS";
            NormalizeLoadedBoard(item);
            KitchenTiles.Add(item);
        }

        Categories.Clear();
        foreach (var item in store.Categories) Categories.Add(item);
        Products.Clear();
        foreach (var item in store.Products)
        {
            var destination = NormalizeProductDestination(item.Sector, "CAIXA");
            if (string.Equals(destination, "CAIXA", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "IFOOD", StringComparison.OrdinalIgnoreCase))
            {
                destination = "COZINHA";
            }

            item.Sector = destination;
            Products.Add(item);
        }
        ApplyPendingIFoodStockReconciliation();
        TicketLines.Clear();
        Payments.Clear();
        Users.Clear();
        foreach (var item in store.Users)
        {
            NormalizeLoadedUser(item);
            Users.Add(item);
        }
        RemoveDefaultSeedUsersIfRealUsersExist();
        Drivers.Clear();
        foreach (var item in store.Drivers) Drivers.Add(item);
        Customers.Clear();
        foreach (var item in store.Customers) Customers.Add(item);
        WhatsAppHistory.Clear();
        foreach (var item in store.WhatsAppHistory.OrderByDescending(item => item.When).Take(500))
        {
            WhatsAppHistory.Add(item);
        }
        WhatsAppPendingOrders.Clear();
        foreach (var item in store.WhatsAppPendingOrders.OrderByDescending(item => item.CreatedAt).Take(100))
        {
            WhatsAppPendingOrders.Add(item);
        }
        CashMovements.Clear();
        foreach (var item in store.CashMovements) CashMovements.Add(item);
        _settings = store.Settings ?? new LocalHubSettings();
        _cashTotal = store.CashTotal;

        if (Tables.Count > 0 && store.TicketLines.Count > 0 && Tables[0].Lines.Count == 0)
        {
            Tables[0].Lines = store.TicketLines.Select(CloneLine).ToList();
            Tables[0].Payments = store.Payments.ToList();
            Tables[0].Total = Tables[0].Lines.Sum(line => line.Total);
        }

        return Users.Count > 0;
    }

    private void ApplyPendingIFoodStockReconciliation()
    {
        var changed = false;
        foreach (var order in DeliveryTiles.Where(IsIFoodOrder).Where(order => !order.ExternalStockApplied))
        {
            var display = string.IsNullOrWhiteSpace(order.ExternalDisplayId) ? order.Number : order.ExternalDisplayId;
            var stockWarnings = new List<string>();
            foreach (var line in order.Lines.Where(line =>
                         !IsTableCharge(line)
                         && !string.Equals(line.Code, "IFOOD-TOTAL", StringComparison.OrdinalIgnoreCase)))
            {
                var item = new IFoodImportedItem
                {
                    Code = line.Code,
                    Name = line.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Notes = line.Note
                };
                var product = ResolveOrCreateIFoodProduct(item, display, stockWarnings, announce: false);
                ApplyIFoodStockMovement(product, line.Quantity, display);
            }

            if (stockWarnings.Count > 0)
            {
                order.Notes = $"{order.Notes}\n{string.Join("\n", stockWarnings)}".Trim();
            }

            order.ExternalStockApplied = true;
            changed = true;
        }

        if (changed)
        {
            Debug.WriteLine("iFood stock reconciliation applied to pending imported orders.");
        }
    }

    private void WriteStoreFile(AppStore store)
    {
        var json = JsonSerializer.Serialize(store, JsonOptions);
        WriteTextAtomic(StoreFile, json);
        WriteLocalDurabilityMirrors(json, "salvamento");
    }

    private bool TryLoadStoreFromPath(string path, out AppStore? store)
    {
        store = null;
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            store = JsonSerializer.Deserialize<AppStore>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
            return store is not null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Store load failed from {path}: {ex.Message}");
            return false;
        }
    }

    private bool TryRecoverStoreFromBackups(out string recoveredPath)
    {
        recoveredPath = "";
        foreach (var path in StoreRecoveryCandidates())
        {
            if (!TryLoadStoreFromPath(path, out var backupStore) || backupStore is null)
            {
                continue;
            }

            if (!ApplyStore(backupStore))
            {
                continue;
            }

            recoveredPath = path;
            return true;
        }

        return false;
    }

    private IEnumerable<string> StoreRecoveryCandidates()
    {
        var paths = new List<string>
        {
            $"{StoreFile}.bak",
            LatestLocalBackupFile
        };

        AddNewestFiles(paths, AutomaticBackupDir, "*.json");
        AddNewestFiles(paths, BackupDir, "*.json");

        return paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !string.Equals(path, StoreFile, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddNewestFiles(List<string> paths, string directory, string pattern)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            paths.AddRange(Directory
                .EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Unable to list backups from {directory}: {ex.Message}");
        }
    }

    private string CreateLocalStoreBackup(string reason, bool force)
    {
        var now = DateTime.Now;
        if (!force
            && _appSettings.LastLocalBackupAt.HasValue
            && now - _appSettings.LastLocalBackupAt.Value < TimeSpan.FromMinutes(30)
            && File.Exists(LatestLocalBackupFile))
        {
            return "";
        }

        try
        {
            SaveActiveTicketToCurrentBoard();
            _appSettings.LastLocalBackupAt = now;
            var store = CreateStoreSnapshot();
            var json = JsonSerializer.Serialize(store, JsonOptions);

            WriteLocalDurabilityMirrors(json, reason);
            Directory.CreateDirectory(AutomaticBackupDir);
            Directory.CreateDirectory(SqlBackupDir);

            var suffix = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var jsonPath = Path.Combine(AutomaticBackupDir, $"store-{suffix}.json");
            var sqlPath = Path.Combine(SqlBackupDir, $"store-{suffix}.sql");
            WriteTextAtomic(jsonPath, json);
            WriteTextAtomic(sqlPath, BuildStoreSqlBackup(json, reason, now));
            SaveAppSettings();
            PruneAutomaticBackups();
            return jsonPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Debug.WriteLine($"Local backup failed: {ex.Message}");
            return "";
        }
    }

    private void WriteLocalDurabilityMirrors(string json, string reason)
    {
        try
        {
            Directory.CreateDirectory(BackupDir);
            WriteTextAtomic(LatestLocalBackupFile, json);
            WriteTextAtomic(LatestSqlBackupFile, BuildStoreSqlBackup(json, reason, DateTime.Now));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Debug.WriteLine($"Durability mirror failed: {ex.Message}");
        }
    }

    private void PruneAutomaticBackups()
    {
        PruneBackupDirectory(AutomaticBackupDir, "*.json", MaxAutomaticLocalBackupFiles);
        PruneBackupDirectory(SqlBackupDir, "*.sql", MaxAutomaticLocalBackupFiles);
    }

    private static void PruneBackupDirectory(string directory, string pattern, int keep)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var path in Directory
                         .EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly)
                         .OrderByDescending(File.GetLastWriteTimeUtc)
                         .Skip(keep))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Backup prune failed from {directory}: {ex.Message}");
        }
    }

    private static string BuildStoreSqlBackup(string json, string reason, DateTime createdAt)
    {
        var escapedJson = json.Replace("'", "''");
        var escapedReason = (reason ?? "").Replace("'", "''");
        var created = createdAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return $"""
-- Balcao Livre PDV local recovery mirror
-- Generated at {created}
CREATE TABLE IF NOT EXISTS balcao_store_backup (
    id BIGSERIAL PRIMARY KEY,
    backup_kind TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    reason TEXT NOT NULL,
    payload_json TEXT NOT NULL
);

DELETE FROM balcao_store_backup WHERE backup_kind = 'latest';
INSERT INTO balcao_store_backup (backup_kind, created_at, reason, payload_json)
VALUES ('latest', '{created}', '{escapedReason}', '{escapedJson}');
""";
    }

    private static void WriteTextAtomic(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = $"{path}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temp, content, Encoding.UTF8);
        try
        {
            if (File.Exists(path))
            {
                File.Copy(path, $"{path}.bak", overwrite: true);
            }

            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            TryDeleteFile(temp);
            throw;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Unable to delete temp file {path}: {ex.Message}");
        }
    }

    private static void NormalizeLoadedUser(UserAccount user)
    {
        if (string.IsNullOrWhiteSpace(user.EmployeeNumber)
            && user.Role is "GARCOM" or "CAIXA"
            && !string.IsNullOrWhiteSpace(user.Pin))
        {
            user.EmployeeNumber = NormalizeStaffNumber(user.Pin);
        }

        if (string.IsNullOrWhiteSpace(user.PinHash) && !string.IsNullOrWhiteSpace(user.Pin))
        {
            SetUserPassword(user, user.Pin);
        }

        if (string.Equals(user.Role, "CAIXA", StringComparison.OrdinalIgnoreCase) && !user.IsMaster)
        {
            user.CanReports = false;
            user.CanManageProducts = false;
        }

        if (string.Equals(user.Role, "GERENTE", StringComparison.OrdinalIgnoreCase))
        {
            user.CanCash = true;
            user.CanCancel = true;
            user.CanDiscount = true;
            user.CanManageProducts = true;
            user.CanReports = true;
            user.CanTransfer = true;
        }

        NormalizeRolePermissions(user);
    }

    private static void NormalizeLoadedBoard(TableTile board)
    {
        foreach (var line in board.Lines)
        {
            line.Sector = NormalizeProductDestination(line.Sector, "CAIXA");
            line.KitchenPrintedQuantity = Math.Clamp(line.KitchenPrintedQuantity, 0, line.Quantity);
        }

        if (board.Status == "LIVRE")
        {
            board.Lines.Clear();
            board.Payments.Clear();
            board.Total = 0m;
            board.ChargesEnabled = false;
        }
        else
        {
            board.Total = board.Lines.Sum(line => line.Total);
        }
    }

    private static string NextDeliveryStatus(string status)
    {
        return status switch
        {
            "NOVO" => "PREPARO",
            "PREPARO" => "ROTA",
            "ROTA" => "ENTREGUE",
            "ENTREGUE" => "NOVO",
            _ => "PREPARO"
        };
    }

    private static int DeliveryBoardSortGroup(TableTile tile)
    {
        var status = NormalizeIFoodBoardStatus(tile.Status);
        return status switch
        {
            "NOVO" or "PLACED" or "CREATED" => 0,
            "AGUARDANDO" or "CONFIRMADO" or "ACEITO" or "PREPARO" or "PREPARANDO" or "PRONTO" => 1,
            "ROTA" or "DESPACHADO" => 2,
            "ENTREGUE" or "FINALIZADO" => 3,
            "CANCELAMENTO" or "CANCELADO" => 4,
            _ => 1
        };
    }

    private static DateTime DeliveryBoardTimeSort(TableTile tile)
    {
        var status = NormalizeIFoodBoardStatus(tile.Status);
        return status switch
        {
            "NOVO" or "PLACED" or "CREATED" => FirstKnownTime(
                tile.ExternalCreatedAt,
                GetIFoodConfirmationDeadline(tile),
                tile.CreatedAt),

            "AGUARDANDO" or "CONFIRMADO" or "ACEITO" or "PREPARO" or "PREPARANDO" or "PRONTO" => FirstKnownTime(
                GetIFoodPreparationStart(tile),
                tile.ExternalCreatedAt,
                tile.CreatedAt),

            "ROTA" or "DESPACHADO" => FirstKnownTime(
                tile.ExternalDeliveryExpectedAt,
                tile.ExternalCreatedAt,
                tile.CreatedAt),

            "ENTREGUE" or "FINALIZADO" => FirstKnownTime(
                tile.ExternalDeliveredAt,
                tile.ExternalDeliveryExpectedAt,
                tile.CreatedAt),

            "CANCELAMENTO" or "CANCELADO" => FirstKnownTime(
                tile.LastClosedAt,
                tile.ExternalDeliveredAt,
                tile.ExternalCreatedAt,
                tile.CreatedAt),

            _ => FirstKnownTime(tile.ExternalDeliveryExpectedAt, tile.ExternalCreatedAt, tile.CreatedAt)
        };
    }

    private static DateTime FirstKnownTime(params DateTime?[] values)
    {
        foreach (var value in values)
        {
            if (value.HasValue)
            {
                return value.Value;
            }
        }

        return DateTime.MaxValue;
    }

    private static int DeliveryBoardNumberSort(TableTile tile)
    {
        var digits = new string((tile.Number ?? "").Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, Brazil, out var value) ? value : int.MaxValue;
    }

    private static string NextKitchenStatus(string status)
    {
        return status switch
        {
            "RECEBIDO" => "PREPARANDO",
            "PREPARANDO" => "PRONTO",
            "PRONTO" => "ENTREGUE",
            "ENTREGUE" => "RECEBIDO",
            _ => "RECEBIDO"
        };
    }

    private static TicketLine CloneLine(TicketLine line)
    {
        return new TicketLine
        {
            Code = line.Code,
            Name = line.Name,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            Note = line.Note,
            Sector = line.Sector,
            KitchenPrintedQuantity = line.KitchenPrintedQuantity,
            KitchenStatus = line.KitchenStatus,
            KitchenStartedAt = line.KitchenStartedAt,
            KitchenReadyAt = line.KitchenReadyAt,
            ModifierSummary = line.ModifierSummary,
            CanDelete = line.CanDelete
        };
    }

    private static string AggregateKitchenStatus(IEnumerable<TicketLine> lines)
    {
        var items = lines.Where(line => !IsTableCharge(line)).ToList();
        if (items.Count == 0)
        {
            return "RECEBIDO";
        }

        if (items.All(line => string.Equals(line.KitchenStatus, "ENTREGUE", StringComparison.OrdinalIgnoreCase)))
        {
            return "ENTREGUE";
        }

        if (items.Any(line => string.Equals(line.KitchenStatus, "PRONTO", StringComparison.OrdinalIgnoreCase)))
        {
            return "PRONTO";
        }

        if (items.Any(line => string.Equals(line.KitchenStatus, "PREPARANDO", StringComparison.OrdinalIgnoreCase)))
        {
            return "PREPARANDO";
        }

        return "RECEBIDO";
    }

    private static PaymentLine ClonePayment(PaymentLine payment)
    {
        return new PaymentLine
        {
            Payer = payment.Payer,
            Method = payment.Method,
            Amount = payment.Amount,
            TenderedAmount = payment.TenderedAmount,
            ChangeAmount = payment.ChangeAmount,
            When = payment.When
        };
    }

    private static int Wrap(int value, int count)
    {
        if (count <= 0) return 0;
        return (value % count + count) % count;
    }

    private static bool TryDigit(Key key, out int digit)
    {
        digit = key switch
        {
            >= Key.D0 and <= Key.D9 => key - Key.D0,
            >= Key.NumPad0 and <= Key.NumPad9 => key - Key.NumPad0,
            _ => -1
        };
        return digit >= 0;
    }

    private static bool TryDigit(KeyEventArgs e, out int digit)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (TryDigit(key, out digit))
        {
            return true;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey is >= 0x30 and <= 0x39)
        {
            digit = virtualKey - 0x30;
            return true;
        }

        if (virtualKey is >= 0x60 and <= 0x69)
        {
            digit = virtualKey - 0x60;
            return true;
        }

        digit = -1;
        return false;
    }

    private static bool TryKeypadDigit(KeyEventArgs e, out int digit)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            digit = key - Key.NumPad0;
            return true;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey is >= 0x60 and <= 0x69)
        {
            digit = virtualKey - 0x60;
            return true;
        }

        digit = -1;
        return false;
    }

    private static void InsertTextAtCaret(TextBox textBox, string text)
    {
        var start = textBox.SelectionStart;
        var length = textBox.SelectionLength;
        var current = textBox.Text ?? "";
        textBox.Text = current.Remove(start, length).Insert(start, text);
        textBox.CaretIndex = start + text.Length;
        textBox.SelectionLength = 0;
    }

    private static void AttachCnpjMask(TextBox textBox)
    {
        textBox.ToolTip = "CNPJ: 00.000.000/0001-00";
        var updating = false;

        void Format()
        {
            if (updating)
            {
                return;
            }

            updating = true;
            textBox.Text = FormatCnpj(textBox.Text);
            textBox.CaretIndex = textBox.Text.Length;
            textBox.SelectionLength = 0;
            updating = false;
        }

        textBox.PreviewTextInput += (_, e) =>
        {
            e.Handled = !e.Text.All(char.IsDigit);
        };
        System.Windows.DataObject.AddPastingHandler(textBox, (_, e) =>
        {
            var pasted = e.DataObject.GetDataPresent(System.Windows.DataFormats.Text)
                ? e.DataObject.GetData(System.Windows.DataFormats.Text) as string
                : "";
            if (string.IsNullOrWhiteSpace(pasted) || !pasted.Any(char.IsDigit))
            {
                e.CancelCommand();
            }
        });
        textBox.TextChanged += (_, _) => Format();
        Format();
    }

    private static string FormatCnpj(string value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length > 14)
        {
            digits = digits[..14];
        }

        var sb = new StringBuilder();
        for (var index = 0; index < digits.Length; index++)
        {
            if (index == 2 || index == 5)
            {
                sb.Append('.');
            }
            else if (index == 8)
            {
                sb.Append('/');
            }
            else if (index == 12)
            {
                sb.Append('-');
            }

            sb.Append(digits[index]);
        }

        return sb.ToString();
    }

    private static void AttachMoneyMask(TextBox textBox)
    {
        textBox.ToolTip = "Digite centavos no teclado numerico. Exemplo: 20000 = R$ 200,00.";
        var updating = false;

        void Format()
        {
            if (updating)
            {
                return;
            }

            updating = true;
            var digits = DigitsOnly(textBox.Text);
            if (digits.Length > 11)
            {
                digits = digits[^11..];
            }

            var cents = string.IsNullOrWhiteSpace(digits)
                ? 0m
                : decimal.Parse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture) / 100m;
            textBox.Text = Money(cents);
            textBox.CaretIndex = textBox.Text.Length;
            textBox.SelectionLength = 0;
            updating = false;
        }

        textBox.PreviewTextInput += (_, e) =>
        {
            e.Handled = !e.Text.All(char.IsDigit);
        };
        System.Windows.DataObject.AddPastingHandler(textBox, (_, e) =>
        {
            var pasted = e.DataObject.GetDataPresent(System.Windows.DataFormats.Text)
                ? e.DataObject.GetData(System.Windows.DataFormats.Text) as string
                : "";
            if (string.IsNullOrWhiteSpace(pasted) || !pasted.Any(char.IsDigit))
            {
                e.CancelCommand();
            }
        });
        textBox.TextChanged += (_, _) => Format();
        Format();
    }

    private static string DigitsOnly(string value)
    {
        return new string((value ?? "").Where(char.IsDigit).ToArray());
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, Brazil, out var parsed) ? parsed : fallback;
    }

    private static decimal ParseMoney(string value, decimal fallback)
    {
        value = value.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(value, NumberStyles.Any, Brazil, out var parsed)
            || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : fallback;
    }

    private static string MercadoPagoAmountValue(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string Money(decimal value)
    {
        return value.ToString("C", Brazil);
    }

    private static Brush Solid(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    private enum KeyboardArea
    {
        Tables,
        Categories,
        Products,
        Ticket
    }

    public sealed class AppStore
    {
        public RestaurantIdentityProfile? Profile { get; set; }
        public AppSettings? AppSettings { get; set; }
        public List<RibbonAction> RibbonActions { get; set; } = [];
        public List<TableTile> Tables { get; set; } = [];
        public List<TableTile> DeliveryTiles { get; set; } = [];
        public List<TableTile> KitchenTiles { get; set; } = [];
        public List<CategoryTile> Categories { get; set; } = [];
        public List<ProductTile> Products { get; set; } = [];
        public List<TicketLine> TicketLines { get; set; } = [];
        public List<PaymentLine> Payments { get; set; } = [];
        public List<UserAccount> Users { get; set; } = [];
        public List<CustomerRecord> Customers { get; set; } = [];
        public List<DeliveryDriver> Drivers { get; set; } = [];
        public List<CashMovement> CashMovements { get; set; } = [];
        public List<WhatsAppMessageLog> WhatsAppHistory { get; set; } = [];
        public List<WhatsAppPendingOrder> WhatsAppPendingOrders { get; set; } = [];
        public LocalHubSettings? Settings { get; set; }
        public decimal CashTotal { get; set; }
    }

    public sealed record RibbonAction(string Id, string KeyText, string Title, string Subtitle);

    public sealed class TableTile : NotifyBase
    {
        private bool _isSelected;
        private string _customerName = "";
        private string _status = "LIVRE";
        private decimal _total;

        public string Number { get; set; } = "";
        public string Kind { get; set; } = "MESA";
        public string Detail { get; set; } = "";
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (_customerName == value)
                {
                    return;
                }

                _customerName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(DisplaySubtitle));
            }
        }
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string CustomerCpf { get; set; } = "";
        public string District { get; set; } = "";
        public string Driver { get; set; } = "";
        public string Notes { get; set; } = "";
        public string ExternalSource { get; set; } = "";
        public string ExternalOrderId { get; set; } = "";
        public string ExternalDisplayId { get; set; } = "";
        public string ExternalDeliveredBy { get; set; } = "";
        public string ExternalPickupCode { get; set; } = "";
        public string ExternalDeliveryLocalizer { get; set; } = "";
        public string ExternalShipmentInfo { get; set; } = "";
        public string ExternalOrderTiming { get; set; } = "";
        public string ExternalOrderType { get; set; } = "";
        public string ExternalPaymentMethod { get; set; } = "";
        public string ExternalPaymentSummary { get; set; } = "";
        public decimal ExternalChangeFor { get; set; }
        public string ExternalVoucherSummary { get; set; } = "";
        public string ExternalCancellationInfo { get; set; } = "";
        public DateTime? ExternalCreatedAt { get; set; }
        public DateTime? ExternalPreparationStartAt { get; set; }
        public DateTime? ExternalConfirmationDeadlineAt { get; set; }
        public DateTime? ExternalDeliveryExpectedAt { get; set; }
        public DateTime? ExternalDeliveredAt { get; set; }
        public DateTime? ExternalCollectedAt { get; set; }
        public bool ExternalStockApplied { get; set; }
        public int People { get; set; } = 1;
        public int Waiter { get; set; }
        public bool ChargesEnabled { get; set; }
        public decimal CouvertAmount { get; set; }
        public decimal ServicePercent { get; set; } = 10m;
        public List<TicketLine> Lines { get; set; } = [];
        public List<PaymentLine> Payments { get; set; } = [];
        public List<TicketLine> ClosedLines { get; set; } = [];
        public List<PaymentLine> ClosedPayments { get; set; } = [];
        public DateTime? LastClosedAt { get; set; }
        public string LastReceiptPath { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetField(ref _total, value);
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        [JsonIgnore]
        public Brush TileBrush => Status switch
        {
            "LIVRE" => FreeTile,
            "ABERTO" => FreeTile,
            "ABERTA" => FreeTile,
            "NOVO" => NewTile,
            "OCUPADA" => OccupiedTile,
            "OCUPADO" => OccupiedTile,
            "PRONTO" => ReadyTile,
            "DESPACHADO" => DispatchedTile,
            "ENTREGUE" => DeliveredTile,
            "FINALIZADO" => DeliveredTile,
            "CONTA" => AccountTile,
            "AGUARDANDO" => WaitingTile,
            "CONFIRMADO" => ConfirmedTile,
            "ACEITO" => AcceptedTile,
            "PREPARO" => PrepTile,
            "PREPARANDO" => PreparingTile,
            "ROTA" => RouteTile,
            "CANCELAMENTO" => CancelledTile,
            "CANCELADO" => CancelledTile,
            _ => NewTile
        };

        [JsonIgnore]
        public Brush StatusForegroundBrush => NormalizeIFoodBoardStatus(Status) is "NOVO" or "PLACED" or "CREATED" or "CANCELAMENTO" or "CANCELADO"
            ? Brushes.White
            : Solid("#17351F");

        [JsonIgnore]
        public string DisplayStatus => NormalizeIFoodBoardStatus(Status) == "CANCELAMENTO" ? "CANCELADO" : Status;

        [JsonIgnore] public string TransferDisplay => $"{Kind} {Number}  {Status}  {Money(Total)}";
        [JsonIgnore] public string TransferTotalText => Money(Total);
        [JsonIgnore] public string TransferSubtitle => $"{BoardKindLabel(this)} {Number}  |  {DisplaySubtitle}";
        [JsonIgnore] public string TimerText => BuildBoardTileTimerText(this);
        [JsonIgnore] public Brush TimerBrush => BuildBoardTileTimerBrush(this);
        [JsonIgnore] public string TransferHint => Lines.Count > 0 || Payments.Count > 0
            ? "Tem itens: juntar comandas"
            : "Livre: mover comanda";

        [JsonIgnore]
        public string DisplayTitle
        {
            get
            {
                if (Kind == "MESA" && !string.IsNullOrWhiteSpace(CustomerName))
                {
                    return CustomerName;
                }

                return Number;
            }
        }

        [JsonIgnore]
        public string DisplaySubtitle
        {
            get
            {
                if (Kind == "MESA" && !string.IsNullOrWhiteSpace(CustomerName))
                {
                    return $"MESA {Number}";
                }

                if (Kind == "BALCAO")
                {
                    return string.IsNullOrWhiteSpace(CustomerName) ? "FICHA" : CustomerName;
                }

                if (Kind == "DELIVERY")
                {
                    return CompactDeliveryTileSubtitle(this);
                }

                return Detail;
            }
        }

        public void RefreshVisualState()
        {
            OnPropertyChanged(nameof(DisplaySubtitle));
            OnPropertyChanged(nameof(DisplayStatus));
            OnPropertyChanged(nameof(TileBrush));
            OnPropertyChanged(nameof(TimerText));
            OnPropertyChanged(nameof(TimerBrush));
            OnPropertyChanged(nameof(TransferSubtitle));
        }
    }

    public sealed class CategoryTile : NotifyBase
    {
        private bool _isSelected;

        public CategoryTile()
        {
        }

        public CategoryTile(string name)
        {
            Name = name;
        }

        public string Name { get; set; } = "";

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }
    }

    private readonly record struct ProfitSummary(decimal Revenue, decimal Cost, decimal Profit, decimal Margin)
    {
        public static ProfitSummary From(decimal revenue, decimal cost)
        {
            var profit = revenue - cost;
            var margin = revenue > 0 ? profit / revenue * 100m : 0m;
            return new ProfitSummary(revenue, cost, profit, margin);
        }
    }

    private readonly record struct IFoodReportSummary(
        int TotalOrders,
        int ValidOrders,
        int MerchantOrders,
        int IFoodShipmentOrders,
        int CancelledOrders,
        int DeliveredOrders,
        decimal MerchantRevenue,
        decimal IFoodShipmentRevenue,
        decimal Revenue,
        decimal ProductCost,
        decimal EstimatedMerchantFee,
        decimal EstimatedIFoodShipmentFee,
        decimal EstimatedFee,
        decimal EstimatedNetProfit,
        decimal EstimatedMargin);

    public sealed class ProductTile : NotifyBase
    {
        private bool _isSelected;

        public ProductTile()
        {
        }

        public ProductTile(string code, string name, string category, decimal price)
        {
            Code = code;
            Name = name;
            Category = category;
            Price = price;
        }

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public string Sector { get; set; } = "CAIXA";
        public bool Active { get; set; } = true;
        public bool IsPizza { get; set; }
        public string WhatsAppCode { get; set; } = "";
        public string WhatsAppAliases { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string IFoodProductId { get; set; } = "";
        public string IFoodExternalCode { get; set; } = "";
        public bool IFoodCompositionEnabled { get; set; }
        public List<ProductModifier> Modifiers { get; set; } = [];
        public List<ProductRecipeItem> RecipeItems { get; set; } = [];
        public decimal StockQuantity { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal SoldQuantity { get; set; }
        public List<StockMovement> StockHistory { get; set; } = [];

        [JsonIgnore] public string ShortName => Name.Length <= 18 ? Name : Name[..18];
        [JsonIgnore] public string CostPriceText => Money(CostPrice);
        [JsonIgnore] public string PriceText => Money(Price);
        [JsonIgnore] public decimal ProfitAmount => Price - CostPrice;
        [JsonIgnore] public decimal ProfitMargin => Price > 0 ? ProfitAmount / Price * 100m : 0m;
        [JsonIgnore] public string ProfitMarginText => $"Margem {ProfitMargin:N2}%";
        [JsonIgnore] public string ProfitSummary => $"Compra {CostPriceText}  Venda {PriceText}  Lucro {Money(ProfitAmount)}";
        [JsonIgnore] public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
        [JsonIgnore] public string ImageStatusText => HasImage ? "Com foto" : "Sem foto";
        [JsonIgnore] public string IFoodCompositionText => IFoodCompositionEnabled
            ? $"iFood: {Modifiers.Count:N0} adicional(is), ficha {RecipeItems.Count:N0} item(ns)"
            : "iFood: sem composicao";
        [JsonIgnore] public string SearchDisplay => $"{Code}  {Name}  {Category}  {PriceText}";
        [JsonIgnore] public string StockDisplay => StockQuantity <= MinimumStock && MinimumStock > 0
            ? $"ALERTA  {Code} {Name}  estoque {StockQuantity:N0}  minimo {MinimumStock:N0}"
            : $"{Code} {Name}  estoque {StockQuantity:N0}  minimo {MinimumStock:N0}";
        [JsonIgnore] public bool IsLowStock => StockQuantity < 0 || (MinimumStock > 0 && StockQuantity <= MinimumStock);
        [JsonIgnore] public string StockStatusText => StockQuantity < 0 ? "NEGATIVO" : IsLowStock ? "CRITICO" : "OK";

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }
    }

    public sealed class TicketLine
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Note { get; set; } = "";
        public string Sector { get; set; } = "";
        public int KitchenPrintedQuantity { get; set; }
        public string KitchenStatus { get; set; } = "RECEBIDO";
        public DateTime? KitchenStartedAt { get; set; }
        public DateTime? KitchenReadyAt { get; set; }
        public string ModifierSummary { get; set; } = "";
        [JsonIgnore] public bool CanDelete { get; set; } = true;
        [JsonIgnore] public decimal Total => Quantity * UnitPrice;
        [JsonIgnore] public string TotalText => Money(Total);
        [JsonIgnore] public string TransferDisplay => $"{Quantity}x {Name}  {TotalText}  {Note}";
    }

    public sealed class PaymentLine
    {
        public string Payer { get; set; } = "";
        public string Method { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal TenderedAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public DateTime When { get; set; } = DateTime.Now;
        [JsonIgnore]
        public string Display
        {
            get
            {
                var tendered = TenderedAmount > 0 && TenderedAmount != Amount
                    ? $"  Recebido {Money(TenderedAmount)}"
                    : "";
                var change = ChangeAmount > 0 ? $"  Troco {Money(ChangeAmount)}" : "";
                return $"{Method}  {Money(Amount)}{tendered}{change}  {Payer}";
            }
        }
    }

    public sealed class UserAccount
    {
        public string Name { get; set; } = "";
        public string Pin { get; set; } = "";
        public string PinHash { get; set; } = "";
        public string EmployeeNumber { get; set; } = "";
        public string Role { get; set; } = "CAIXA";
        public bool IsMaster { get; set; }
        public bool CanTransfer { get; set; }
        public bool CanCancel { get; set; }
        public bool CanDiscount { get; set; }
        public bool CanManageProducts { get; set; }
        public bool CanReports { get; set; }
        public bool CanCash { get; set; }
        public bool CanDelivery { get; set; }
        public bool CanInventory { get; set; }
        public bool CanKitchen { get; set; }
        public bool CanIFood { get; set; }
        public bool CanSettings { get; set; }
        public bool CanBackup { get; set; }
        public bool CanFiscal { get; set; }
        public bool CanDeliveryZones { get; set; }
        public bool CanCentralSync { get; set; }
        [JsonIgnore] public string Display
        {
            get
            {
                var number = StaffNumber(this);
                return string.IsNullOrWhiteSpace(number)
                    ? $"{Name}  {Role}"
                    : $"{number} - {Name}  {Role}";
            }
        }
    }

    public sealed class CustomerRecord
    {
        public string Cpf { get; set; } = "";
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string District { get; set; } = "";
        public string Notes { get; set; } = "";
        [JsonIgnore] public string Display => $"{Name}  {Phone}  {Cpf}";
    }

    public sealed class DeliveryDriver
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public bool Active { get; set; } = true;
    }

    public sealed class DeliveryZoneFee
    {
        public string Zone { get; set; } = "";
        public string DistrictMatch { get; set; } = "";
        public double RadiusKm { get; set; }
        public decimal Fee { get; set; }
        public decimal MinimumOrder { get; set; }
        public bool Active { get; set; } = true;
        [JsonIgnore] public string Display => RadiusKm > 0
            ? $"Ate {RadiusKm:N1} km  {Money(Fee)}"
            : $"{Zone}  {Money(Fee)}";
    }

    public sealed class ProductModifier
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public bool Required { get; set; }
        [JsonIgnore] public string Display => $"{Name}  {Money(Price)}{(Required ? "  obrigatorio" : "")}";
    }

    public sealed class ProductRecipeItem
    {
        public string ProductCode { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "UN";
        [JsonIgnore] public string Display => $"{Name}  {Quantity:N2} {Unit}";
    }

    public sealed class FiscalTefSettings
    {
        public bool Enabled { get; set; }
        public string FiscalProvider { get; set; } = "NAO CONFIGURADO";
        public string TefProvider { get; set; } = "NAO CONFIGURADO";
        public string MerchantCode { get; set; } = "";
        public string CscId { get; set; } = "";
        public string Environment { get; set; } = "HOMOLOGACAO";
        public bool RequireFiscalBeforeReceipt { get; set; }
    }

    public sealed class CashClosingSnapshot
    {
        public decimal ExpectedCash { get; set; }
        public decimal CountedCash { get; set; }
        public decimal Difference { get; set; }
        public decimal PixTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public decimal DebitTotal { get; set; }
        public decimal OtherTotal { get; set; }
        public string Operator { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime When { get; set; } = DateTime.Now;
    }

    public sealed class CashMovement
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public string Reason { get; set; } = "";
        public string User { get; set; } = "";
        public DateTime When { get; set; } = DateTime.Now;
        public CashClosingSnapshot? Closing { get; set; }
        [JsonIgnore] public string Display => $"{When:t}  {CashMovementLabel(Type)}  {Money(Amount)}  {Reason}";
    }

    public sealed class WhatsAppSettings
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "META";
        public string SendPulseApiKey { get; set; } = "";
        public string SendPulseBotId { get; set; } = "";
        public string SendPulseStorePhone { get; set; } = "";
        public bool SendPulseActivationPending { get; set; }
        public DateTime? SendPulseLastActivationAt { get; set; }
        public string SendPulseSaleClosedScript { get; set; } = "";
        public string SendPulseOrderConfirmedScript { get; set; } = "";
        public string SendPulseOrderReadyScript { get; set; } = "";
        public string SendPulseOrderDispatchedScript { get; set; } = "";
        public bool AutoPressEnter { get; set; } = true;
        public int SendDelaySeconds { get; set; } = 8;
        public string DefaultCountryCode { get; set; } = "55";
        public bool ExtensionInstalledConfirmed { get; set; }
        public bool LocalConnectorEnabled { get; set; }
        public int LocalConnectorPort { get; set; } = 8787;
        public bool AutoReplyConnector { get; set; } = true;
        public bool AutoCreateConfirmedOrders { get; set; } = true;
        public int ManagedBrowserProcessId { get; set; }
    }

    public sealed class WhatsAppMessageLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BoardKind { get; set; } = "";
        public string BoardNumber { get; set; } = "";
        public decimal Total { get; set; }
        public string Message { get; set; } = "";
        public string Status { get; set; } = "CRIADO";
        public string Error { get; set; } = "";
        public DateTime When { get; set; } = DateTime.Now;
        public DateTime? OpenedAt { get; set; }
        public DateTime? SentAt { get; set; }
        [JsonIgnore] public string Display => $"{When:dd/MM HH:mm}  {Status}  {CustomerName}  {Phone}  {BoardKind} {BoardNumber}  {Money(Total)}";
    }

    public sealed class WhatsAppPendingOrder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ConversationKey { get; set; } = "";
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string SourceMessage { get; set; } = "";
        public string Status { get; set; } = "AGUARDANDO_CONFIRMACAO";
        public decimal Total { get; set; }
        public List<WhatsAppPendingOrderItem> Items { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ConfirmedAt { get; set; }
        [JsonIgnore] public string Display => $"{CreatedAt:dd/MM HH:mm}  {Status}  {CustomerName}  {Phone}  {Items.Count:N0} item(ns)  {Money(Total)}";
    }

    public sealed class WhatsAppPendingOrderItem
    {
        public string ProductCode { get; set; } = "";
        public string WhatsAppCode { get; set; } = "";
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Sector { get; set; } = "";
        [JsonIgnore] public decimal Total => Quantity * UnitPrice;
        [JsonIgnore] public string Display => $"{Quantity}x {Name}  {Money(Total)}";
    }

    public sealed class StockMovement
    {
        public string ProductCode { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = "";
        public DateTime When { get; set; } = DateTime.Now;

        [JsonIgnore]
        public string Display
        {
            get
            {
                var type = string.IsNullOrWhiteSpace(Type)
                    ? Quantity >= 0 ? "ENTRADA" : "SAIDA"
                    : Type;
                var sign = Quantity > 0 ? "+" : Quantity < 0 ? "-" : "";
                return $"{When:dd/MM HH:mm}  {type}  {sign}{Math.Abs(Quantity):N0}  {Reason}";
            }
        }
    }

    public sealed class LocalHubSettings
    {
        public string ServerIp { get; set; } = "192.168.0.10";
        public int Port { get; set; } = 8080;
        public string KitchenPrinter { get; set; } = "COZINHA";
        public string BarPrinter { get; set; } = "BAR";
    }

    public sealed class AppSettings
    {
        public bool WindowsNotificationsEnabled { get; set; } = true;
        public bool NotificationSoundEnabled { get; set; } = true;
        public bool InAppVibrationEnabled { get; set; } = true;
        public string NotificationSound { get; set; } = "PADRAO";
        public string IFoodAlertSoundPath { get; set; } = "";
        public bool AutoPrintDelivery { get; set; } = true;
        public bool AutoPrintKitchen { get; set; } = true;
        public string PrintLayout { get; set; } = "GRANDE";
        public bool LargeReceiptDefaultApplied { get; set; }
        public string PreferredPrinterName { get; set; } = "";
        public List<SectorPrinterSetting> SectorPrinters { get; set; } = [];
        public bool ReceiptQrEnabled { get; set; }
        public string ReceiptQrKind { get; set; } = "PIX";
        public string ReceiptQrContent { get; set; } = "";
        public int ReceiptSequence { get; set; }
        public List<DeliveryZoneFee> DeliveryZones { get; set; } = [];
        public FiscalTefSettings FiscalTef { get; set; } = new();
        public bool CloudBackupEnabled { get; set; } = true;
        public DateTime? LastCloudBackupAt { get; set; }
        public DateTime? LastLocalBackupAt { get; set; }
        public bool CentralSyncEnabled { get; set; } = true;
        public DateTime? LastCentralSyncAt { get; set; }
        public WhatsAppSettings WhatsApp { get; set; } = new();
        public bool ActivationCompleted { get; set; }
        public string ActivationKey { get; set; } = "";
        public string ActivationPlan { get; set; } = "";
        public DateTime? ActivationActivatedAt { get; set; }
        public DateTime? ActivationExpiresAt { get; set; }
        public string ActivationMachineHash { get; set; } = "";
        public string ActivationLastWarningKey { get; set; } = "";
        public bool AutoCheckUpdates { get; set; } = true;
        public string UpdateManifestUrl { get; set; } = DefaultUpdateManifestUrl;
        public DateTime? LastUpdateCheckAt { get; set; }
        public bool AdminSyncEnabled { get; set; } = true;
        public string AdminApiUrl { get; set; } = DefaultAdminApiUrl;
        public DateTime? LastAdminSyncAt { get; set; }
        public string WhatsAppFunctionUrl { get; set; } = DefaultWhatsAppFunctionUrl;
        public bool PublicMenuAutoPublish { get; set; } = true;
        public string PublicMenuBaseUrl { get; set; } = DefaultPublicMenuBaseUrl;
        public DateTime? LastPublicMenuPublishAt { get; set; }
        public bool PublicMenuStoreOpen { get; set; } = true;
        public int PublicMenuWaitMinMinutes { get; set; } = 30;
        public int PublicMenuWaitMaxMinutes { get; set; } = 60;
        public bool PublicMenuDiscountConfigured { get; set; }
        public bool PublicMenuDiscountEnabled { get; set; }
        public string PublicMenuDiscountCode { get; set; } = "EXCLUSIVO4";
        public decimal PublicMenuDiscountAmount { get; set; } = 4m;
        public string PublicMenuDiscountDescription { get; set; } = "Apresente este cupom no atendimento para receber o desconto.";
        public bool PublicMenuLoyaltyConfigured { get; set; }
        public bool PublicMenuLoyaltyEnabled { get; set; }
        public int PublicMenuLoyaltyGoal { get; set; } = 20;
        public decimal PublicMenuLoyaltyMinimumOrder { get; set; } = 20m;
        public IFoodIntegrationSettings IFood { get; set; } = new();
        public MercadoPagoPaymentSettings MercadoPago { get; set; } = new();
        public PagBankPaymentSettings PagBank { get; set; } = new();
    }

    public sealed class MercadoPagoPaymentSettings
    {
        public bool Enabled { get; set; }
        public bool Connected { get; set; }
        public string SellerUserId { get; set; } = "";
        public string DefaultTerminalId { get; set; } = "";
        public string DefaultTerminalLabel { get; set; } = "";
        public DateTime? LastSyncAt { get; set; }
        public string LastError { get; set; } = "";
    }

    public sealed class PagBankPaymentSettings
    {
        public bool Enabled { get; set; }
        public bool Connected { get; set; }
        public bool PlugPagEnabled { get; set; }
        public string AccountId { get; set; } = "";
        public string DefaultTerminalId { get; set; } = "";
        public string DefaultTerminalLabel { get; set; } = "";
        public string PlugPagComPort { get; set; } = "";
        public DateTime? LastSyncAt { get; set; }
        public string LastError { get; set; } = "";
    }

    public sealed class SectorPrinterSetting
    {
        public string Sector { get; set; } = "";
        public string PrinterName { get; set; } = "";
    }

    public sealed class ActivationLedger
    {
        public List<ActivationUse> Activations { get; set; } = [];
    }

    public sealed class ActivationUse
    {
        public string Key { get; set; } = "";
        public string MachineHash { get; set; } = "";
        public string MachineCode { get; set; } = "";
        public string Plan { get; set; } = "";
        public DateTime ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string AppVersion { get; set; } = "";
    }

    public class AdminClientPayload
    {
        public string EventName { get; set; } = "";
        public string LicenseKey { get; set; } = "";
        public string MachineHash { get; set; } = "";
        public string MachineCode { get; set; } = "";
        public string AppVersion { get; set; } = "";
        public DateTime? LocalExpiresAt { get; set; }
        public string LocalPlan { get; set; } = "";
        public AdminProfileSnapshot Profile { get; set; } = new();
        public AdminSettingsSnapshot Settings { get; set; } = new();
        public AdminMetricsSnapshot Metrics { get; set; } = new();
    }

    public sealed class AdminActivationResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public string Plan { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }

        public static AdminActivationResult Allow(string plan, DateTime? expiresAt, string message) => new()
        {
            Ok = true,
            Plan = plan,
            ExpiresAt = expiresAt,
            Message = message
        };

        public static AdminActivationResult Deny(string message) => new()
        {
            Ok = false,
            Message = message
        };
    }

    public class AdminMercadoPagoResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
    }

    public sealed class AdminMercadoPagoConnectResult : AdminMercadoPagoResult
    {
        public string AuthUrl { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
    }

    public sealed class AdminMercadoPagoConnectionStatusResult : AdminMercadoPagoResult
    {
        public bool Connected { get; set; }
        public string Status { get; set; } = "";
        public string SellerUserId { get; set; } = "";
        public string SelectedTerminalId { get; set; } = "";
        public string SelectedTerminalLabel { get; set; } = "";
        public string LastSyncAt { get; set; } = "";
        public string LastError { get; set; } = "";
    }

    public sealed class AdminMercadoPagoTerminalsResult : AdminMercadoPagoResult
    {
        public List<AdminMercadoPagoTerminalDto> Terminals { get; set; } = [];
    }

    public sealed class AdminMercadoPagoTerminalDto
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string PosId { get; set; } = "";
        public string StoreId { get; set; } = "";
        public string ExternalPosId { get; set; } = "";
        public string OperatingMode { get; set; } = "";

        [JsonIgnore]
        public string Display => string.IsNullOrWhiteSpace(Label) ? Id : Label;
    }

    public sealed class AdminMercadoPagoTerminalPayload : AdminClientPayload
    {
        public string TerminalId { get; set; } = "";
        public string TerminalLabel { get; set; } = "";
    }

    public class AdminMercadoPagoChargePayload : AdminClientPayload
    {
        public string Amount { get; set; } = "";
        public string Method { get; set; } = "";
        public string LocalReference { get; set; } = "";
        public string Description { get; set; } = "";
        public string TerminalId { get; set; } = "";
        public List<AdminMercadoPagoItemPayload> Items { get; set; } = [];
    }

    public sealed class AdminMercadoPagoWebChargePayload : AdminMercadoPagoChargePayload
    {
        public string PayerName { get; set; } = "";
        public string PayerEmail { get; set; } = "";
    }

    public sealed class AdminMercadoPagoItemPayload
    {
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public int Quantity { get; set; }
        public string UnitPrice { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public sealed class AdminMercadoPagoPointStatusPayload : AdminClientPayload
    {
        public string AttemptId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string LocalReference { get; set; } = "";
    }

    public class AdminMercadoPagoChargeResult : AdminMercadoPagoResult
    {
        public string AttemptId { get; set; } = "";
        public string LocalReference { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
    }

    public sealed class AdminMercadoPagoWebChargeResult : AdminMercadoPagoChargeResult
    {
        public string QrCode { get; set; } = "";
        public string QrCodeBase64 { get; set; } = "";
        public string TicketUrl { get; set; } = "";
        public string PaymentUrl { get; set; } = "";
        public string ExpiresAt { get; set; } = "";
    }

    public sealed class AdminMercadoPagoPointStatusResult : AdminMercadoPagoResult
    {
        public string AttemptId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
        public bool Paid { get; set; }
    }

    public sealed class AdminPagBankConnectResult : AdminMercadoPagoResult
    {
        public string AuthUrl { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
    }

    public sealed class AdminPagBankConnectionStatusResult : AdminMercadoPagoResult
    {
        public bool Connected { get; set; }
        public string Status { get; set; } = "";
        public string AccountId { get; set; } = "";
        public string SelectedTerminalId { get; set; } = "";
        public string SelectedTerminalLabel { get; set; } = "";
        public string ComPort { get; set; } = "";
        public string LastSyncAt { get; set; } = "";
        public string LastError { get; set; } = "";
    }

    public sealed class AdminPagBankTerminalPayload : AdminClientPayload
    {
        public string TerminalId { get; set; } = "";
        public string TerminalLabel { get; set; } = "";
        public string ComPort { get; set; } = "";
    }

    public sealed class AdminPagBankWebChargePayload : AdminMercadoPagoChargePayload
    {
        public string PayerName { get; set; } = "";
        public string PayerEmail { get; set; } = "";
        public string PayerTaxId { get; set; } = "";
        public string PayerPhone { get; set; } = "";
    }

    public sealed class AdminPagBankWebChargeResult : AdminMercadoPagoChargeResult
    {
        public string QrCode { get; set; } = "";
        public string QrCodeBase64 { get; set; } = "";
        public string TicketUrl { get; set; } = "";
        public string PaymentUrl { get; set; } = "";
        public string ExpiresAt { get; set; } = "";
    }

    public sealed class PlugPagChargeResult
    {
        public bool Approved { get; set; }
        public string Message { get; set; } = "";
        public string TransactionCode { get; set; } = "";
        public string HostNsu { get; set; } = "";

        public static PlugPagChargeResult Ok(PlugPagTransactionResult result) => new()
        {
            Approved = true,
            Message = CompactSingleLine(result.Message),
            TransactionCode = CompactSingleLine(result.TransactionCode),
            HostNsu = CompactSingleLine(result.HostNsu)
        };

        public static PlugPagChargeResult Fail(string message) => new()
        {
            Approved = false,
            Message = string.IsNullOrWhiteSpace(message) ? "Pagamento PagBank nao aprovado." : message
        };
    }

    public sealed class AdminSupportPayload : AdminClientPayload
    {
        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
    }

    public sealed class AdminPublicMenuPublishPayload : AdminClientPayload
    {
        public string Slug { get; set; } = "";
        public string PublicUrl { get; set; } = "";
        public string Description { get; set; } = "";
        public string ThemeColor { get; set; } = "#0f766e";
        public string LogoUrl { get; set; } = "";
        public string LogoFileName { get; set; } = "";
        public string LogoContentType { get; set; } = "";
        public string LogoBase64 { get; set; } = "";
        public string CoverImageUrl { get; set; } = "";
        public string CoverImageFileName { get; set; } = "";
        public string CoverImageContentType { get; set; } = "";
        public string CoverImageBase64 { get; set; } = "";
        public bool StoreOpen { get; set; } = true;
        public int WaitMinMinutes { get; set; } = 30;
        public int WaitMaxMinutes { get; set; } = 60;
        public bool DiscountEnabled { get; set; }
        public string DiscountCode { get; set; } = "EXCLUSIVO4";
        public decimal DiscountAmount { get; set; } = 4m;
        public string DiscountDescription { get; set; } = "Apresente este cupom no atendimento para receber o desconto.";
        public bool LoyaltyEnabled { get; set; }
        public int LoyaltyGoal { get; set; } = 20;
        public decimal LoyaltyMinimumOrder { get; set; } = 20m;
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
        public List<AdminPublicMenuProductSnapshot> Items { get; set; } = [];
    }

    public sealed class AdminPublicMenuProductSnapshot
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

    public sealed class AdminPublicMenuPublishResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public string Slug { get; set; } = "";
        public string PublicUrl { get; set; } = "";
        public int ItemsPublished { get; set; }

        public static AdminPublicMenuPublishResult Fail(string message) => new()
        {
            Ok = false,
            Message = message
        };
    }

    public sealed class AdminPublicMenuOrdersPollPayload : AdminClientPayload
    {
        public int Limit { get; set; } = 25;
    }

    public sealed class AdminPublicMenuOrderAckPayload : AdminClientPayload
    {
        public string OrderId { get; set; } = "";
        public string PdvOrderId { get; set; } = "";
        public string Status { get; set; } = "IMPORTADO";
    }

    public sealed class AdminPublicMenuOrdersResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public List<AdminPublicMenuOrderSnapshot> Orders { get; set; } = [];
    }

    public sealed class AdminPublicMenuOrderSnapshot
    {
        public string Id { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Source { get; set; } = "";
        public string Status { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerDocument { get; set; } = "";
        public string OrderType { get; set; } = "";
        public string TableLabel { get; set; } = "";
        public string Address { get; set; } = "";
        public string District { get; set; } = "";
        public string Reference { get; set; } = "";
        public string DesiredTime { get; set; } = "";
        public string Notes { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<AdminPublicMenuOrderItemSnapshot> Items { get; set; } = [];
    }

    public sealed class AdminPublicMenuOrderItemSnapshot
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Note { get; set; } = "";
    }

    public sealed class AdminWhatsAppActivationPayload : AdminClientPayload
    {
        public string StorePhone { get; set; } = "";
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
    }

    public sealed class AdminWhatsAppSendPayload : AdminClientPayload
    {
        public string StorePhone { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string Message { get; set; } = "";
        public string BoardKind { get; set; } = "";
        public string BoardNumber { get; set; } = "";
        public decimal Total { get; set; }
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
    }

    public sealed class AdminWhatsAppResult
    {
        public bool Ok { get; set; }
        public bool Pending { get; set; }
        public string Message { get; set; } = "";
        public string StorePhone { get; set; } = "";
        public string OnboardingUrl { get; set; } = "";
        public string PhoneNumberId { get; set; } = "";
        public string WabaId { get; set; } = "";

        public static AdminWhatsAppResult Fail(string message) => new()
        {
            Ok = false,
            Message = message
        };
    }

    public sealed class AdminSupportMessagePayload : AdminClientPayload
    {
        public string Message { get; set; } = "";
        public DateTimeOffset LocalWhen { get; set; } = DateTimeOffset.Now;
    }

    public sealed class AdminSupportListResult
    {
        public bool Ok { get; set; } = true;
        public string Message { get; set; } = "";
        public List<AdminSupportTicketSnapshot> Tickets { get; set; } = [];
    }

    public sealed class AdminSupportTicketSnapshot
    {
        public string Id { get; set; } = "";
        public string ShortId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<AdminSupportMessageSnapshot> Messages { get; set; } = [];
    }

    public sealed class AdminSupportMessageSnapshot
    {
        public string Id { get; set; } = "";
        public string Sender { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTimeOffset When { get; set; }
    }

    public sealed class AdminSupportResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public string TicketId { get; set; } = "";

        public static AdminSupportResult OkResult(string ticketId, string message) => new()
        {
            Ok = true,
            TicketId = ticketId,
            Message = message
        };

        public static AdminSupportResult Fail(string message) => new()
        {
            Ok = false,
            Message = message
        };
    }

    public sealed class AdminProfileSnapshot
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

    public sealed class AdminSettingsSnapshot
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
        public string ReceiptQrContentPreview { get; set; } = "";
        public bool AutoCheckUpdates { get; set; }
        public bool AdminSyncEnabled { get; set; }
        public bool SupabaseAuthEnabled { get; set; }
        public bool SupabaseUrlConfigured { get; set; }
        public string SupabaseUserEmail { get; set; } = "";
    }

    public sealed class AdminMetricsSnapshot
    {
        public int TablesCount { get; set; }
        public int OpenBoardsCount { get; set; }
        public int DeliveryCount { get; set; }
        public int ProductsCount { get; set; }
        public int UsersCount { get; set; }
        public int CustomersCount { get; set; }
        public int LowStockCount { get; set; }
    }

    public sealed class UpdateManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("installerUrl")]
        public string InstallerUrl { get; set; } = "";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
            if (propertyName is nameof(TableTile.Status))
            {
                OnPropertyChanged(nameof(TableTile.TileBrush));
                OnPropertyChanged(nameof(TableTile.StatusForegroundBrush));
                OnPropertyChanged(nameof(TableTile.DisplayStatus));
                OnPropertyChanged(nameof(TableTile.TimerText));
                OnPropertyChanged(nameof(TableTile.TimerBrush));
            }
        }
    }
}
