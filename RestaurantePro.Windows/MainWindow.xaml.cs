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

namespace RestaurantePro.Windows;

public partial class MainWindow : Window
{
    private const string AppDisplayName = "Balcão Livre PDV";
    private const string AppReceiptName = "BALCAO LIVRE PDV";
    private const string DefaultUpdateManifestUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows/version.json";
    private const string DefaultAdminApiUrl = "https://balcaolivrepdv.onrender.com";
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly Brush GreenTile = Solid("#9CE083");
    private static readonly Brush AmberTile = Solid("#F7D87A");
    private static readonly Brush RedTile = Solid("#F5A09A");
    private static readonly Brush BlueSoft = Solid("#E8F1FA");
    private static readonly Brush GreenSoft = Solid("#EAF8EF");
    private static readonly Brush GreenText = Solid("#176B36");
    private static readonly Brush AmberSoft = Solid("#FFF2CB");
    private static readonly Brush AmberText = Solid("#99620D");
    private static readonly Brush RedSoft = Solid("#FFE2DF");
    private static readonly Brush RedText = Solid("#A11D1D");
    private const string CouvertCode = "900001";
    private const string ServiceCode = "900002";
    private const double RibbonScrollStep = 260;
    private static readonly int[] ActivationWarningDays = [1, 3, 7, 15, 30];
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.8) };
    private readonly DispatcherTimer _licenseTimer = new() { Interval = TimeSpan.FromSeconds(5) };

    private readonly string _dataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RestaurantePro.Windows");

    private KeyboardArea _area = KeyboardArea.Products;
    private int _selectedTableIndex;
    private int _selectedProductIndex;
    private int _selectedCategoryIndex;
    private int _selectedTicketIndex;
    private bool _updatingTableSelection;
    private decimal _cashTotal;
    private string _currentUser = "";
    private LocalHubSettings _settings = new();
    private RestaurantIdentityProfile _profile = new();
    private AppSettings _appSettings = new();
    private Forms.NotifyIcon? _trayIcon;
    private bool _exitRequested;
    private bool _activationPromptOpen;

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

    private string StoreFile => Path.Combine(_dataRoot, "commandas-store.json");
    private string SettingsFile => Path.Combine(_dataRoot, "app-settings.json");
    private string ProfileFile => Path.Combine(_dataRoot, "restaurant-profile.json");
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
        Directory.CreateDirectory(_dataRoot);
        LoadAppSettings();
        LoadRestaurantProfile();
        ApplyRestaurantIdentity();
        SeedStaticUi();
        LoadStore();
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
        Loaded += (_, _) =>
        {
            if (!RequireStartupLogin())
            {
                return;
            }

            TableBox.Focus();
            TableBox.SelectAll();
            SelectArea(KeyboardArea.Ticket);
            SetStatus("Digite a mesa e pressione Enter. Modo offline 100% ativo.");
            _licenseTimer.Start();
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
        _appSettings.ActivationCompleted = false;
        SaveAppSettings();
        ShowToast("Ativacao expirada", "A licenca venceu. Informe uma nova chave para continuar.", "BL", "#A11D1D", "#FFE2DF");
        SetStatus("Ativacao expirada. Informe uma nova chave para continuar.");

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

    private async Task<AdminActivationResult> TryValidateAdminActivationAsync(string normalizedKey, DateTime? expiresAt, string plan)
    {
        if (!_appSettings.AdminSyncEnabled)
        {
            return AdminActivationResult.Allow(plan, expiresAt, "Sincronizacao admin desligada.");
        }

        var endpoint = BuildAdminApiUri("/api/app/activate");
        if (endpoint is null)
        {
            return AdminActivationResult.Allow(plan, expiresAt, "URL do admin invalida. Ativacao local liberada.");
        }

        var payload = CreateAdminClientPayload("activation", normalizedKey, expiresAt, plan);
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AdminActivationResult>(json, JsonOptions);
            if (result is not null)
            {
                return result;
            }

            if (!response.IsSuccessStatusCode)
            {
                return AdminActivationResult.Deny("Admin recusou a ativacao, mas nao retornou detalhes.");
            }
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"Admin activation sync unavailable: {ex.Message}");
        }

        return AdminActivationResult.Allow(plan, expiresAt, "Admin offline. Ativacao local liberada.");
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

    private Uri? BuildAdminApiUri(string path)
    {
        var baseUrl = (_appSettings.AdminApiUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultAdminApiUrl;
            _appSettings.AdminApiUrl = baseUrl;
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
        var today = DateTime.Today;
        var allPayments = boards.SelectMany(board => board.Payments.Concat(board.ClosedPayments)).ToList();
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
                AdminSyncEnabled = _appSettings.AdminSyncEnabled
            },
            Metrics = new AdminMetricsSnapshot
            {
                TablesCount = Tables.Count,
                OpenBoardsCount = openBoards,
                DeliveryCount = DeliveryTiles.Count,
                ProductsCount = Products.Count,
                UsersCount = Users.Count,
                CustomersCount = Customers.Count,
                CashTotal = _cashTotal,
                SalesToday = allPayments.Where(payment => payment.When.Date == today).Sum(payment => payment.Amount),
                SoldItemsTotal = Products.Sum(product => (int)product.SoldQuantity),
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
        meta.Add("Offline 100%");
        BrandMetaText.Text = string.Join("  |  ", meta);
        UpdateTrayTitle();

        ApplyBrandLogo(profile.LocalLogoPath);
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
        _trayIcon?.Dispose();
        _trayIcon = null;
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        base.OnClosed(e);
    }

    private void ApplyBrandLogo(string logoPath)
    {
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(logoPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                BrandLogoImage.Source = image;
                BrandLogoImage.Visibility = Visibility.Visible;
                BrandLogoFallback.Visibility = Visibility.Collapsed;
                return;
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException)
            {
                Debug.WriteLine($"Logo load failed: {ex.Message}");
            }
        }

        BrandLogoImage.Source = null;
        BrandLogoImage.Visibility = Visibility.Collapsed;
        BrandLogoFallback.Visibility = Visibility.Visible;
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

        if (HandleFocusedTextField(e))
        {
            return;
        }

        if (TryDigit(e, out var digit))
        {
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
                IncludeSelectedProduct(requireCode: true);
                e.Handled = true;
                break;
            case Key.F3:
                ShowProductSearchDialog();
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
                ShowTransferDialog();
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
                if (CodeBox.Text.Length > 0)
                {
                    CodeBox.Text = CodeBox.Text[..^1];
                    FilterProducts();
                }
                e.Handled = true;
                break;
            case Key.Add:
            case Key.OemPlus:
                ChangeQuantity(1);
                e.Handled = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
                ChangeQuantity(-1);
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

    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox || string.IsNullOrWhiteSpace(e.Text))
        {
            return;
        }

        if (e.Text.Length == 1 && char.IsDigit(e.Text[0]))
        {
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
                IncludeSelectedProduct(requireCode: true);
                e.Handled = true;
            }

            return true;
        }

        if (textBox == PriceBox || textBox == NoteBox)
        {
            if (e.Key == Key.Enter)
            {
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
                ReopenCurrentCommand();
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

    private void ShowSettingsDialog()
    {
        if (!RequirePermission(IsManagerUser, "Configuracoes do sistema"))
        {
            return;
        }

        var profile = new RestaurantIdentityProfile
        {
            OwnerName = _profile.OwnerName,
            BusinessName = _profile.BusinessName,
            LegalName = _profile.LegalName,
            Cnpj = _profile.Cnpj,
            Phone = _profile.Phone,
            Address = _profile.Address,
            City = _profile.City,
            State = _profile.State,
            LocalLogoPath = _profile.LocalLogoPath
        };
        var dialog = CreateDialog("Configuracoes do sistema", 940, 700);
        var ownerBox = new TextBox { Text = profile.OwnerName };
        var businessBox = new TextBox { Text = profile.BusinessName };
        var legalBox = new TextBox { Text = profile.LegalName };
        var cnpjBox = new TextBox { Text = profile.Cnpj };
        var phoneBox = new TextBox { Text = profile.Phone };
        var addressBox = new TextBox { Text = profile.Address };
        var cityBox = new TextBox { Text = profile.City };
        var stateBox = new TextBox { Text = profile.State };
        var logoPath = profile.LocalLogoPath;
        var logoText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(logoPath) ? "Nenhuma imagem selecionada" : Path.GetFileName(logoPath),
            Foreground = Solid("#667684"),
            TextWrapping = TextWrapping.Wrap
        };
        const string defaultPrinterOption = "Usar padrao do Windows";
        var installedPrinters = new List<string> { defaultPrinterOption };
        installedPrinters.AddRange(GetInstalledPrinterNames());
        if (!string.IsNullOrWhiteSpace(_appSettings.PreferredPrinterName)
            && installedPrinters.All(item => !string.Equals(item, _appSettings.PreferredPrinterName, StringComparison.OrdinalIgnoreCase)))
        {
            installedPrinters.Add(_appSettings.PreferredPrinterName);
        }

        var printerBox = new ComboBox
        {
            ItemsSource = installedPrinters,
            SelectedItem = string.IsNullOrWhiteSpace(_appSettings.PreferredPrinterName)
                ? defaultPrinterOption
                : _appSettings.PreferredPrinterName,
            MinHeight = 38,
            Margin = new Thickness(0, 4, 0, 0),
            IsEditable = false
        };
        var defaultPrinterText = new TextBlock
        {
            Text = $"Padrao do Windows: {GetDefaultPrinterName()}",
            Foreground = Solid("#667684"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var versionText = new TextBlock
        {
            Text = $"Versao do app: {GetAppVersion()}",
            Foreground = Solid("#18222B"),
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
        var qrTypeBox = new ComboBox
        {
            ItemsSource = new[] { "PIX", "INSTAGRAM", "GOOGLE MAPS", "LINK" },
            SelectedItem = NormalizeReceiptQrKind(_appSettings.ReceiptQrKind),
            MinHeight = 38,
            Margin = new Thickness(0, 4, 0, 8),
            IsEditable = false
        };
        var qrContentBox = new TextBox
        {
            Text = _appSettings.ReceiptQrContent,
            MinHeight = 38,
            Margin = new Thickness(0, 4, 0, 2)
        };
        var qrHint = new TextBlock
        {
            Text = "Exemplos: chave Pix, @instagram, link do Google Maps ou qualquer URL.",
            Foreground = Solid("#667684"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var soundButtons = new List<Button>();
        var printSizeButtons = new List<Button>();

        Border ToggleCard(string title, string subtitle, Func<bool> get, Action<bool> set)
        {
            var titleText = new TextBlock { FontWeight = FontWeights.Bold };
            var subtitleText = new TextBlock { Foreground = Solid("#667684"), FontSize = 11, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap };
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
                card.Background = enabled ? Solid("#E8F7F4") : Brushes.White;
                card.BorderBrush = enabled ? Solid("#0F766E") : Solid("#D8E2EC");
                titleText.Foreground = enabled ? Solid("#0F766E") : Solid("#18222B");
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
                BorderBrush = Solid("#D8E2EC"),
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
                button.Background = active ? Solid("#E8F1FA") : Brushes.White;
                button.BorderBrush = active ? Solid(selectedColor) : Solid("#D8E2EC");
                button.Foreground = active ? Solid(selectedColor) : Solid("#18222B");
                button.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
            }
        }

        var soundGrid = new UniformGrid { Columns = 4, Rows = 1, Margin = new Thickness(0, 6, 0, 4) };
        foreach (var item in new[] { ("Padrao", "PADRAO"), ("Aviso", "AVISO"), ("Erro", "ERRO"), ("Nenhum", "NENHUM") })
        {
            var button = SegmentButton(item.Item1, item.Item2);
            button.Click += (_, _) =>
            {
                _appSettings.NotificationSound = item.Item2;
                RefreshSegments(soundButtons, _appSettings.NotificationSound, "#245B91");
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
                RefreshSegments(printSizeButtons, _appSettings.PrintLayout, "#245B91");
            };
            printSizeButtons.Add(button);
            sizeGrid.Children.Add(button);
        }

        var chooseLogo = DialogButton("Trocar foto/logo", "#2F6FAE");
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

        var testNotification = DialogButton("Testar notificacao", "#2F6FAE");
        testNotification.HorizontalAlignment = HorizontalAlignment.Stretch;
        testNotification.Click += (_, _) =>
        {
            ShowToast("Teste de notificacao", "Som, Windows e vibracao visual foram testados conforme a configuracao.", "NT", "#2F6FAE", "#E8F1FA");
            status.Text = "Notificacao de teste enviada.";
        };
        var checkUpdate = DialogButton("Verificar atualizacao agora", "#2F6FAE");
        checkUpdate.HorizontalAlignment = HorizontalAlignment.Stretch;
        checkUpdate.Click += async (_, _) =>
        {
            _appSettings.UpdateManifestUrl = DefaultUpdateManifestUrl;
            SaveAppSettings();
            await CheckForUpdatesAsync(showIfCurrent: true);
        };

        var save = DialogButton("Salvar configuracoes", "#0F766E");
        save.HorizontalAlignment = HorizontalAlignment.Stretch;
        save.Click += (_, _) =>
        {
            profile.OwnerName = ownerBox.Text.Trim();
            profile.BusinessName = businessBox.Text.Trim();
            profile.LegalName = legalBox.Text.Trim();
            profile.Cnpj = cnpjBox.Text.Trim();
            profile.Phone = phoneBox.Text.Trim();
            profile.Address = addressBox.Text.Trim();
            profile.City = cityBox.Text.Trim();
            profile.State = stateBox.Text.Trim().ToUpperInvariant();
            profile.LocalLogoPath = logoPath;

            _profile = profile;
            var selectedPrinter = printerBox.SelectedItem?.ToString() ?? "";
            _appSettings.PreferredPrinterName = string.Equals(selectedPrinter, defaultPrinterOption, StringComparison.Ordinal)
                ? ""
                : selectedPrinter.Trim();
            _appSettings.UpdateManifestUrl = DefaultUpdateManifestUrl;
            _appSettings.ReceiptQrKind = NormalizeReceiptQrKind(qrTypeBox.SelectedItem?.ToString() ?? _appSettings.ReceiptQrKind);
            _appSettings.ReceiptQrContent = qrContentBox.Text.Trim();
            SaveRestaurantProfile();
            SaveAppSettings();
            ApplyRestaurantIdentity();
            SaveStore();
            status.Text = "Configuracoes salvas.";
            SetStatus("Configuracoes atualizadas.");
            dialog.Close();
        };

        var root = new Grid { Margin = new Thickness(18) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

        var company = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
        company.Children.Add(SectionTitle("Empresa"));
        company.Children.Add(TwoColumnFields(("Responsavel", ownerBox), ("Nome fantasia", businessBox)));
        company.Children.Add(TwoColumnFields(("Razao social", legalBox), ("CNPJ", cnpjBox)));
        company.Children.Add(TwoColumnFields(("Telefone", phoneBox), ("Cidade", cityBox)));
        company.Children.Add(TwoColumnFields(("UF", stateBox), ("Endereco", addressBox)));
        company.Children.Add(new Border { Background = Solid("#F8FBFD"), BorderBrush = Solid("#D8E2EC"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(12), Margin = new Thickness(0, 4, 0, 10), Child = new StackPanel { Children = { new TextBlock { Text = "Foto/logo", Foreground = Solid("#667684"), FontWeight = FontWeights.SemiBold }, logoText, chooseLogo } } });
        company.Children.Add(SectionTitle("Comprovantes"));
        company.Children.Add(new TextBlock { Text = "Dados usados somente nos comprovantes, recibos e impressoes locais.", Foreground = Solid("#667684"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 8) });
        company.Children.Add(new TextBlock { Text = "Sistema offline 100%: sem login e sem sincronizacao externa.", Foreground = Solid("#0F766E"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        company.Children.Add(versionText);

        var system = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };
        system.Children.Add(SectionTitle("Notificacoes"));
        system.Children.Add(ToggleCard("Toast visual no app", "Mostra aviso dentro do PDV quando uma acao importante acontecer.", () => _appSettings.WindowsNotificationsEnabled, value => _appSettings.WindowsNotificationsEnabled = value));
        system.Children.Add(ToggleCard("Som de notificacao", "Toca som quando o app confirmar uma acao.", () => _appSettings.NotificationSoundEnabled, value => _appSettings.NotificationSoundEnabled = value));
        system.Children.Add(ToggleCard("Vibracao no app", "Faz uma vibracao visual curta na janela/toast.", () => _appSettings.InAppVibrationEnabled, value => _appSettings.InAppVibrationEnabled = value));
        system.Children.Add(DialogLabel("Som"));
        system.Children.Add(soundGrid);
        system.Children.Add(testNotification);
        system.Children.Add(SectionTitle("Impressao"));
        system.Children.Add(ToggleCard("Imprimir delivery automaticamente", "Pedidos novos saem na impressora configurada.", () => _appSettings.AutoPrintDelivery, value => _appSettings.AutoPrintDelivery = value));
        system.Children.Add(ToggleCard("Imprimir cozinha automaticamente", "F4 envia a ordem para a impressora configurada.", () => _appSettings.AutoPrintKitchen, value => _appSettings.AutoPrintKitchen = value));
        system.Children.Add(DialogLabel("Modelo padrao"));
        system.Children.Add(sizeGrid);
        system.Children.Add(DialogLabel("Impressora preferida"));
        system.Children.Add(printerBox);
        system.Children.Add(defaultPrinterText);
        system.Children.Add(ToggleCard("QR no comprovante", "Quando ligado, imprime um QR Code no final do comprovante.", () => _appSettings.ReceiptQrEnabled, value => _appSettings.ReceiptQrEnabled = value));
        system.Children.Add(DialogLabel("Tipo do QR"));
        system.Children.Add(qrTypeBox);
        system.Children.Add(DialogLabel("Conteudo do QR"));
        system.Children.Add(qrContentBox);
        system.Children.Add(qrHint);
        system.Children.Add(SectionTitle("Atualizacoes"));
        system.Children.Add(ToggleCard("Atualizar automaticamente ao abrir", "Consulta o servidor ao entrar no PDV. Se houver versao nova, baixa, instala e reabre o sistema.", () => _appSettings.AutoCheckUpdates, value => _appSettings.AutoCheckUpdates = value));
        system.Children.Add(checkUpdate);

        root.Children.Add(company);
        Grid.SetColumn(system, 1);
        root.Children.Add(system);

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
            BorderBrush = Solid("#D8E2EC"),
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
        RefreshSegments(soundButtons, _appSettings.NotificationSound, "#245B91");
        RefreshSegments(printSizeButtons, _appSettings.PrintLayout, "#245B91");
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
            Foreground = Solid("#18222B"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Informe o primeiro numero e quantas mesas o estabelecimento usa.",
            Foreground = Solid("#667684"),
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
            Foreground = Solid("#0F766E"),
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
            Background = Solid("#E8F7F4"),
            BorderBrush = Solid("#0F766E"),
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

        var create = DialogButton("Criar mesas", "#0F766E");
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
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(22, 12, 22, 12)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footerGrid.Children.Add(new TextBlock
        {
            Text = "Enter confirma. Esc fecha.",
            Foreground = Solid("#667684"),
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
        ShowProductSearchDialog();
        SelectArea(KeyboardArea.Products);
    }

    private void DeleteTicketLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TicketLine line })
        {
            RemoveTicketLine(line);
        }

        e.Handled = true;
        SelectArea(KeyboardArea.Ticket);
    }

    private void ConfirmInclude_Click(object sender, RoutedEventArgs e)
    {
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
            "Delivery" => DeliveryTiles,
            _ => Tables.Where(table => table.Kind == "MESA")
        };

        foreach (var tile in source)
        {
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
        TicketLines.Clear();
        foreach (var line in board.Lines)
        {
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

        var line = new TicketLine
        {
            Code = product.Code,
            Name = product.Name,
            Quantity = qty,
            UnitPrice = product.Price,
            Note = note,
            Sector = product.Sector
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
        if (product.StockQuantity > 0)
        {
            product.StockQuantity = Math.Max(0, product.StockQuantity - qty);
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
        if (CurrentBoard is { } board)
        {
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
            board.Status = NextDeliveryStatus(board.Status);
        }
        else if (board.Kind == "KDS")
        {
            board.Status = NextKitchenStatus(board.Status);
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
        RefreshTotals();
        SaveStore();
    }

    private void RemoveSelectedLine()
    {
        if (TicketLines.Count == 0)
        {
            return;
        }

        var line = TicketLines[Math.Clamp(_selectedTicketIndex, 0, TicketLines.Count - 1)];
        RemoveTicketLine(line);
    }

    private void RemoveTicketLine(TicketLine line)
    {
        if (!TicketLines.Contains(line))
        {
            return;
        }

        if (IsTableCharge(line))
        {
            RemoveTableCharges();
            return;
        }

        var chargesWereActive = HasAppliedTableCharges();
        TicketLines.Remove(line);
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
    }

    private void TransferSelectedLine()
    {
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

        var confirm = DialogButton("Enter confirma", "#0F766E");

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
        panel.Children.Add(new TextBlock { Text = "Nome", Foreground = Solid("#667684"), FontWeight = FontWeights.SemiBold });
        panel.Children.Add(payerBox);
        panel.Children.Add(new TextBlock { Text = "Valor", Foreground = Solid("#667684"), FontWeight = FontWeights.SemiBold });
        panel.Children.Add(amountBox);
        panel.Children.Add(new TextBlock { Text = "Forma", Foreground = Solid("#667684"), FontWeight = FontWeights.SemiBold });
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
        var qty = Math.Max(1, ParseInt(QuantityBox.Text, 1) + delta);
        QuantityBox.Text = qty.ToString(Brazil);
    }

    private void ShowProductSearchDialog()
    {
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
  <Border Background="#F8FBFD" BorderBrush="#D8E2EC" BorderThickness="1" CornerRadius="8" Padding="10" Margin="0,0,0,7">
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
      <TextBlock Text="{Binding Name}" Foreground="#18222B" FontWeight="SemiBold" FontSize="13" TextTrimming="CharacterEllipsis"/>
      <TextBlock Grid.Column="1" Text="{Binding PriceText}" Foreground="#0F766E" FontWeight="Bold" Margin="12,0,0,0"/>
      <TextBlock Grid.Row="1" Text="{Binding Code}" Foreground="#667684" FontSize="11" Margin="0,3,0,0"/>
      <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding Category}" Foreground="#667684" FontSize="11" HorizontalAlignment="Right" Margin="12,3,0,0"/>
      <TextBlock Grid.Row="2" Grid.ColumnSpan="2" Text="{Binding ProfitMarginText}" Foreground="#667684" FontSize="11" Margin="0,3,0,0"/>
    </Grid>
  </Border>
</DataTemplate>
""");

        var queryBox = new TextBox { ToolTip = "Buscar produto" };
        var countText = new TextBlock
        {
            Foreground = Solid("#667684"),
            FontSize = 12,
            Margin = new Thickness(0, 6, 0, 10)
        };
        var formTitle = new TextBlock
        {
            Text = "Novo produto",
            Foreground = Solid("#18222B"),
            FontSize = 20,
            FontWeight = FontWeights.Bold
        };
        var formSubtitle = new TextBlock
        {
            Text = "Preencha nome, categoria e preco.",
            Foreground = Solid("#667684"),
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
        var groupBox = new ComboBox
        {
            ItemsSource = Categories.Select(category => category.Name).ToList(),
            IsEditable = true,
            MinHeight = 34
        };
        var sectorBox = new ComboBox { ItemsSource = new[] { "COZINHA", "BAR", "PIZZA", "SOBREMESA", "BALCAO" }, SelectedIndex = 0, MinHeight = 34 };
        var pizzaBox = new CheckBox { Content = "Pizza / produto com sabores", Margin = new Thickness(0, 8, 0, 4) };
        var activeBox = new CheckBox { Content = "Mostrar na venda", IsChecked = true, Margin = new Thickness(0, 2, 0, 0) };
        var marginText = new TextBlock
        {
            Foreground = Solid("#0F766E"),
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 8)
        };

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
                Foreground = Solid("#18222B"),
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
                BorderBrush = Solid("#D8E2EC"),
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
            marginText.Foreground = profit < 0 ? RedText : profit == 0 ? Solid("#667684") : GreenText;
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
            sectorBox.SelectedIndex = 0;
            pizzaBox.IsChecked = false;
            activeBox.IsChecked = true;
            formTitle.Text = "Novo produto";
            formSubtitle.Text = "Informe compra, venda e estoque. A margem calcula sozinha.";
            statusText.Text = "";
            RefreshMarginPreview();
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
            sectorBox.SelectedItem = product.Sector;
            pizzaBox.IsChecked = product.IsPizza;
            activeBox.IsChecked = product.Active;
            formTitle.Text = product.Name;
            formSubtitle.Text = $"{product.Code}  |  {product.Category}  |  venda {product.PriceText}  |  {product.ProfitMarginText}";
            statusText.Text = "";
            RefreshMarginPreview();
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

            var product = Products.FirstOrDefault(item => item.Code == code);
            if (product is null)
            {
                product = new ProductTile();
                Products.Add(product);
            }

            product.Code = code;
            product.Name = name;
            product.Category = category;
            product.CostPrice = cost;
            product.Price = price;
            product.StockQuantity = ParseMoney(stockBox.Text, 0);
            product.MinimumStock = ParseMoney(minBox.Text, 0);
            product.Sector = sectorBox.SelectedItem?.ToString() ?? "COZINHA";
            product.IsPizza = pizzaBox.IsChecked == true;
            product.Active = activeBox.IsChecked == true;
            ProductsList.Items.Refresh();
            RefreshProductList(product);
            FilterProducts();
            SaveStore();
            statusText.Foreground = GreenText;
            statusText.Text = $"Produto salvo: {product.Name}";
            formTitle.Text = product.Name;
            formSubtitle.Text = $"{product.Code}  |  {product.Category}  |  venda {product.PriceText}  |  {product.ProfitMarginText}";
            RefreshMarginPreview();
            SetStatus($"Produto salvo: {product.Code} {product.Name}");
            return true;
        }

        var addProductButton = DialogButton("Novo produto", "#2F6FAE");
        addProductButton.Click += (_, _) => StartNewProduct();

        var saveButton = DialogButton("Salvar produto", "#0F766E");
        saveButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        saveButton.Width = double.NaN;
        saveButton.Click += (_, _) => SaveProduct();

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
            Foreground = Solid("#18222B"),
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
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 16, 0),
            Child = leftGrid
        };
        root.Children.Add(leftCard);

        var formHeader = new Border
        {
            Background = Solid("#E8F7F4"),
            BorderBrush = Solid("#0F766E"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel { Children = { formTitle, formSubtitle } }
        };

        var basicSection = FormSection("Produto",
            TwoColumns(DialogField("Codigo", codeBox), DialogField("Nome do produto", nameBox)),
            DialogField("Categoria", groupBox));

        var saleSection = FormSection("Compra e venda",
            TwoColumns(DialogField("Preco de compra", costBox), DialogField("Preco de venda", priceBox)),
            marginText,
            DialogField("Setor", sectorBox),
            activeBox,
            pizzaBox);

        var stockSection = FormSection("Estoque",
            TwoColumns(DialogField("Quantidade atual", stockBox), DialogField("Estoque minimo", minBox)));

        var form = new StackPanel();
        form.Children.Add(formHeader);
        form.Children.Add(basicSection);
        form.Children.Add(saleSection);
        form.Children.Add(stockSection);
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

        var dialog = CreateDialog("Usuarios e permissoes", 760, 540);
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

        void LoadUser(UserAccount user)
        {
            nameBox.Text = user.Name;
            pinBox.Text = user.Pin;
            roleBox.SelectedItem = user.Role;
            masterBox.IsChecked = user.IsMaster;
            transferBox.IsChecked = user.CanTransfer;
            cancelBox.IsChecked = user.CanCancel;
            discountBox.IsChecked = user.CanDiscount;
            productsBox.IsChecked = user.CanManageProducts;
            reportsBox.IsChecked = user.CanReports;
            cashBox.IsChecked = user.CanCash;
        }

        usersList.SelectionChanged += (_, _) =>
        {
            if (usersList.SelectedItem is UserAccount user) LoadUser(user);
        };

        var saveButton = DialogButton("Salvar usuario", "#0F766E");
        saveButton.Click += (_, _) =>
        {
            var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "OPERADOR" : nameBox.Text.Trim().ToUpperInvariant();
            var user = Users.FirstOrDefault(item => item.Name == name);
            if (user is null)
            {
                user = new UserAccount();
                Users.Add(user);
            }

            user.Name = name;
            user.Pin = string.IsNullOrWhiteSpace(pinBox.Text) ? "0000" : pinBox.Text.Trim();
            user.Role = roleBox.SelectedItem?.ToString() ?? "CAIXA";
            user.IsMaster = masterBox.IsChecked == true;
            user.CanTransfer = transferBox.IsChecked == true || user.IsMaster;
            user.CanCancel = cancelBox.IsChecked == true || user.IsMaster;
            user.CanDiscount = discountBox.IsChecked == true || user.IsMaster;
            user.CanManageProducts = productsBox.IsChecked == true || user.IsMaster;
            user.CanReports = reportsBox.IsChecked == true || user.IsMaster;
            user.CanCash = cashBox.IsChecked == true || user.IsMaster;
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
        form.Children.Add(saveButton);
        Grid.SetColumn(form, 1);
        grid.Children.Add(form);
        dialog.Content = grid;
        if (Users.Count > 0) usersList.SelectedIndex = 0;
        dialog.ShowDialog();
    }

    private void ShowDeliveryOrderDialog()
    {
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
        var driverBox = new ComboBox { ItemsSource = Drivers.Select(driver => driver.Name).ToList(), MinHeight = 34 };
        var notesBox = new TextBox { Height = 62, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        var typeButtons = new List<Button>();
        var sizeButtons = new List<Button>();
        var typeGrid = new UniformGrid { Columns = 3, Rows = 1, Margin = new Thickness(0, 6, 0, 10) };
        var sizeGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 6, 0, 10) };
        var printCard = new Border
        {
            Background = Solid("#E8F7F4"),
            BorderBrush = Solid("#0F766E"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 10)
        };
        var printTitle = new TextBlock { FontWeight = FontWeights.Bold };
        var printHint = new TextBlock { Foreground = Solid("#667684"), FontSize = 11, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap };

        void RefreshButtons(IEnumerable<Button> buttons, string selected)
        {
            foreach (var button in buttons)
            {
                var active = string.Equals(button.Tag?.ToString(), selected, StringComparison.Ordinal);
                button.Background = active ? Solid("#E8F1FA") : Brushes.White;
                button.BorderBrush = active ? Solid("#245B91") : Solid("#D8E2EC");
                button.Foreground = active ? Solid("#245B91") : Solid("#18222B");
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
                BorderBrush = Solid("#D8E2EC"),
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
                feeBox.Text = orderType == "ENTREGA" ? feeBox.Text : "0,00";
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
            printCard.Background = autoPrint ? Solid("#E8F7F4") : Brushes.White;
            printCard.BorderBrush = autoPrint ? Solid("#0F766E") : Solid("#D8E2EC");
            printTitle.Foreground = autoPrint ? Solid("#0F766E") : Solid("#18222B");
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
            if (!string.IsNullOrWhiteSpace(customer.Notes))
            {
                notesBox.Text = customer.Notes;
            }
        }

        var includeCustomerButton = DialogButton("Incluir cliente cadastrado", "#2F6FAE");
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

        var createButton = DialogButton("Criar pedido", "#0F766E");
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
                tile.Lines.Add(new TicketLine { Code = "000020", Name = "TAXA ENTREGA", Quantity = 1, UnitPrice = fee, Sector = "BALCAO" });
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
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14, 10, 14, 10),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Cadastro rapido de pedido", Foreground = Solid("#18222B"), FontWeight = FontWeights.Bold, FontSize = 15 },
                    new TextBlock { Text = "Cria o pedido e pode imprimir automaticamente na impressora padrao do computador.", Foreground = Solid("#667684"), FontSize = 12, Margin = new Thickness(0, 3, 0, 0) }
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
        side.Children.Add(DialogLabel("Tipo"));
        side.Children.Add(typeGrid);
        side.Children.Add(DialogLabel("Entregador"));
        side.Children.Add(driverBox);
        side.Children.Add(DialogLabel("Taxa"));
        side.Children.Add(feeBox);
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
        phoneBox.Focus();
        dialog.ShowDialog();
    }

    private void ShowInventoryDialog()
    {
        if (!RequirePermission(IsManagerUser, "Estoque operacional"))
        {
            return;
        }

        var dialog = CreateDialog("Estoque operacional", 1020, 680);
        var sortedProducts = Products.OrderBy(product => product.Category).ThenBy(product => product.Name).ToList();
        var qtyBox = new TextBox();
        var minBox = new TextBox();
        var movementBox = new TextBox { Text = "1" };
        var reasonBox = new TextBox { Text = "Entrada manual" };
        var movementSummary = new TextBlock
        {
            Text = "Sem movimento registrado.",
            Foreground = Solid("#667684"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var selectedName = new TextBlock
        {
            Text = "Selecione um produto",
            Foreground = Solid("#18222B"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        var selectedMeta = new TextBlock
        {
            Foreground = Solid("#667684"),
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        var selectedStatus = new TextBlock
        {
            Foreground = Solid("#0F766E"),
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
            selectedMeta.Text = $"{product.Code}  |  {product.Category}  |  {product.PriceText}";
            selectedStatus.Text = product.StockStatusText;
            selectedStatus.Foreground = Solid(product.IsLowStock ? "#A11D1D" : "#0F766E");
            var lastMovement = product.StockHistory
                .OrderByDescending(item => item.When)
                .FirstOrDefault();
            movementSummary.Text = lastMovement is null
                ? "Sem movimento registrado."
                : $"Ultimo: {lastMovement.Display}";
        }

        Border CreateInventoryMetric(string title, TextBlock valueText, string detail, string color)
        {
            valueText.Foreground = Solid("#18222B");
            valueText.FontSize = 21;
            valueText.FontWeight = FontWeights.Bold;
            valueText.Margin = new Thickness(0, 3, 0, 0);

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#D8E2EC"),
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
                Foreground = Solid("#667684"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            });
            stack.Children.Add(valueText);
            stack.Children.Add(new TextBlock
            {
                Text = detail,
                Foreground = Solid("#667684"),
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
            inventoryValue.Text = Money(sortedProducts.Sum(product => product.StockQuantity * product.Price));
        }

        var grid = new Grid { Margin = new Thickness(18) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(94) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var metrics = new UniformGrid { Columns = 4, Rows = 1 };
        metrics.Children.Add(CreateInventoryMetric("Produtos", totalProductsValue, "itens cadastrados", "#245B91"));
        metrics.Children.Add(CreateInventoryMetric("Criticos", lowStockValue, "abaixo do estoque minimo", "#A11D1D"));
        metrics.Children.Add(CreateInventoryMetric("Unidades", totalUnitsValue, "saldo fisico total", "#99620D"));
        metrics.Children.Add(CreateInventoryMetric("Valor estoque", inventoryValue, "preco de venda estimado", "#0F766E"));
        grid.Children.Add(metrics);

        var body = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        Grid.SetRow(body, 1);

        var tableCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Margin = new Thickness(0, 0, 12, 0)
        };
        var tablePanel = new Grid { Margin = new Thickness(14, 12, 14, 14) };
        tablePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        tablePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var tableHeader = new StackPanel();
        tableHeader.Children.Add(new TextBlock
        {
            Text = "Produtos em estoque",
            Foreground = Solid("#18222B"),
            FontSize = 15,
            FontWeight = FontWeights.Bold
        });
        tableHeader.Children.Add(new TextBlock
        {
            Text = "Use as setas para selecionar e Enter nos campos para editar.",
            Foreground = Solid("#667684"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
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
            table.Items.Refresh();
            LoadStock(product);
            UpdateMetrics();
            SaveStore();
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
        }

        var setButton = DialogButton("Salvar saldo/minimo", "#2F6FAE");
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
            }
        };
        var inButton = DialogButton("Entrada", "#0F766E");
        inButton.Click += (_, _) => ChangeStock(1, "ENTRADA");
        var outButton = DialogButton("Saida", "#A11D1D");
        outButton.Click += (_, _) => ChangeStock(-1, "SAIDA");

        movementBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ChangeStock(1, "ENTRADA");
                e.Handled = true;
            }
        };
        reasonBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ChangeStock(1, "ENTRADA");
                e.Handled = true;
            }
        };

        foreach (var button in new[] { setButton, inButton, outButton })
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Width = double.NaN;
        }
        inButton.Margin = new Thickness(0, 8, 6, 0);
        outButton.Margin = new Thickness(6, 8, 0, 0);

        var movementButtons = new UniformGrid
        {
            Columns = 2,
            Rows = 1,
            Margin = new Thickness(0, 0, 0, 0)
        };
        movementButtons.Children.Add(inButton);
        movementButtons.Children.Add(outButton);

        var sideCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9)
        };
        var panel = new StackPanel { Margin = new Thickness(18, 16, 18, 16) };
        panel.Children.Add(selectedName);
        panel.Children.Add(selectedMeta);
        panel.Children.Add(selectedStatus);
        panel.Children.Add(new Border { Height = 1, Background = Solid("#E3EBF2"), Margin = new Thickness(0, 14, 0, 10) });
        panel.Children.Add(DialogLabel("Quantidade atual"));
        panel.Children.Add(qtyBox);
        panel.Children.Add(DialogLabel("Estoque minimo"));
        panel.Children.Add(minBox);
        panel.Children.Add(DialogLabel("Quantidade movimento"));
        panel.Children.Add(movementBox);
        panel.Children.Add(DialogLabel("Motivo"));
        panel.Children.Add(reasonBox);
        panel.Children.Add(setButton);
        panel.Children.Add(movementButtons);
        panel.Children.Add(movementSummary);
        sideCard.Child = panel;
        Grid.SetColumn(sideCard, 1);
        body.Children.Add(sideCard);
        grid.Children.Add(body);
        dialog.Content = grid;
        UpdateMetrics();
        if (sortedProducts.Count > 0) table.SelectedIndex = 0;
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

        var supply = DialogButton("Entrada de caixa", "#0F766E");
        supply.Click += (_, _) => AddMovement(1, "ENTRADA");
        var withdrawal = DialogButton("Retirada de caixa", "#A11D1D");
        withdrawal.Click += (_, _) => AddMovement(-1, "RETIRADA");
        var close = DialogButton(IsCashOpen() ? "F10 Fechar caixa" : "F10 Abrir caixa", "#2F6FAE");
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
            Foreground = Solid("#245B91"),
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
            Foreground = Solid("#245B91"),
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
        var open = DialogButton("Abrir caixa", "#0F766E");
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

        CashMovements.Add(new CashMovement
        {
            Type = "FECHAMENTO",
            Amount = 0,
            Reason = "Fechamento do caixa",
            User = _currentUser,
            When = DateTime.Now
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
        var ok = DialogButton("Entendi", "#2F6FAE");
        ok.HorizontalAlignment = HorizontalAlignment.Stretch;
        ok.Click += (_, _) => dialog.Close();

        var panel = DialogPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "Nao da para fechar o caixa enquanto existir mesa, ficha ou pedido com movimento pendente.",
            Foreground = Solid("#18222B"),
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
        if (!RequirePermission(user => user.IsMaster || user.CanTransfer, "Transferencia de comanda"))
        {
            return;
        }

        SaveActiveTicketToCurrentBoard();
        var current = CurrentBoard;
        var transferBoards = GetTransferBoards();
        var sources = transferBoards
            .Where(board => !HasReceivedPayment(board) && (board.Lines.Count > 0 || board.Payments.Count > 0))
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

        var dialog = CreateDialog("Transferencia de comanda completa", 1060, 650);
        var transferBoardTemplate = (DataTemplate)FindResource("TransferBoardTemplate");
        var sourceList = new ListBox
        {
            ItemsSource = sources,
            ItemTemplate = transferBoardTemplate,
            ItemContainerStyle = TransferListBoxItemStyle(),
            Height = 286,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 10, 0, 0)
        };
        var destinationList = new ListBox
        {
            ItemTemplate = transferBoardTemplate,
            ItemContainerStyle = TransferListBoxItemStyle(),
            Height = 286,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 10, 0, 12)
        };
        var selectedDestination = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#245B91"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var sourceDetails = new StackPanel();

        Border Card(string title, string subtitle, UIElement content, string color)
        {
            var stack = new StackPanel { Margin = new Thickness(16, 14, 16, 16) };
            stack.Children.Add(new TextBlock { Text = title, Foreground = Solid("#18222B"), FontSize = 17, FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = subtitle, Foreground = Solid("#667684"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 12) });
            stack.Children.Add(content);
            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Solid("#D8E2EC"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 12),
                Child = new Grid
                {
                    Children =
                    {
                        new Border { Width = 4, Background = Solid(color), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(10, 0, 0, 10) },
                        stack
                    }
                }
            };
        }

        Border MiniStat(string label, string value, string color)
        {
            var stack = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
            stack.Children.Add(new TextBlock { Text = label, Foreground = Solid("#667684"), FontSize = 11, FontWeight = FontWeights.SemiBold });
            stack.Children.Add(new TextBlock { Text = value, Foreground = Solid(color), FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 0) });
            return new Border
            {
                Background = Solid("#F8FBFE"),
                BorderBrush = Solid("#D8E2EC"),
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
                Foreground = Solid("#0F766E"),
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
                    new TextBlock { Text = line.Name, Foreground = Solid("#18222B"), FontWeight = FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis },
                    new TextBlock { Text = $"{line.Quantity:N0} x {Money(line.UnitPrice)}", Foreground = Solid("#667684"), FontSize = 11 }
                }
            });
            return new Border
            {
                Background = Solid("#F8FBFE"),
                BorderBrush = Solid("#D8E2EC"),
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
                sourceDetails.Children.Add(new TextBlock { Text = "Selecione a comanda de origem.", Foreground = Solid("#667684") });
                return;
            }

            sourceDetails.Children.Add(new TextBlock { Text = $"{BoardKindLabel(source)} {source.Number}", Foreground = Solid("#18222B"), FontSize = 24, FontWeight = FontWeights.Bold });
            sourceDetails.Children.Add(new TextBlock { Text = $"{source.Status}  |  origem selecionada", Foreground = Solid("#0F766E"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 0) });
            if (!string.IsNullOrWhiteSpace(source.CustomerName))
            {
                sourceDetails.Children.Add(new TextBlock { Text = $"Cliente: {source.CustomerName}", Foreground = Solid("#667684"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            }

            var stats = new UniformGrid { Columns = 3, Margin = new Thickness(0, 12, -8, 0) };
            stats.Children.Add(MiniStat("Itens", source.Lines.Count.ToString("N0", Brazil), "#245B91"));
            stats.Children.Add(MiniStat("Pagamentos", Money(source.Payments.Sum(payment => payment.Amount)), "#99620D"));
            stats.Children.Add(MiniStat("Total", Money(source.Lines.Sum(line => line.Total)), "#0F766E"));
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
                return;
            }

            var mode = destination.Lines.Count > 0 || destination.Payments.Count > 0
                ? "Destino ocupado: as comandas serao juntadas."
                : "Destino livre: a comanda sera movida inteira.";
            selectedDestination.Text = $"{BoardKindLabel(destination)} {destination.Number} - {destination.Status} - {Money(destination.Total)}\n{mode}";
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

        var transferButton = DialogButton("Transferir comanda completa", "#0F766E");
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
        sourcePanel.Children.Add(new TextBlock { Text = "Escolha qual comanda vai sair do lugar atual.", Foreground = Solid("#667684"), TextWrapping = TextWrapping.Wrap });
        sourcePanel.Children.Add(sourceList);
        sourcePanel.Children.Add(new Border { Height = 1, Background = Solid("#D8E2EC"), Margin = new Thickness(0, 10, 0, 10) });
        sourcePanel.Children.Add(sourceDetails);
        var destinationPanel = new StackPanel();
        destinationPanel.Children.Add(selectedDestination);
        destinationPanel.Children.Add(destinationList);
        destinationPanel.Children.Add(transferButton);

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        var left = new StackPanel();
        left.Children.Add(Card("Origem", "Selecione a comanda completa que sera transferida.", sourcePanel, "#245B91"));
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);
        var right = new StackPanel();
        right.Children.Add(Card("Destino", "Escolha para onde mover a comanda completa.", destinationPanel, "#0F766E"));
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
            Foreground = Solid("#18222B"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Abra uma mesa/ficha, inclua pelo menos um produto e depois use F6 Transferir Comanda.",
            Foreground = Solid("#667684"),
            Margin = new Thickness(0, 10, 0, 18),
            TextWrapping = TextWrapping.Wrap
        });
        var ok = DialogButton("Entendi", "#245B91");
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
        var sourceHadContent = source.Lines.Count > 0 || source.Payments.Count > 0;
        var destinationHadContent = destination.Lines.Count > 0 || destination.Payments.Count > 0;
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
            SetStatus($"Desconto aplicado: {Money(amount)}");
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

        var newButton = DialogButton("Novo cliente", "#2F6FAE");
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

        var save = DialogButton("Salvar cliente", "#0F766E");
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

        var include = DialogButton("Incluir no delivery", "#0F766E");
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
        var save = DialogButton("Salvar", "#0F766E");
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
            passwordBox.Password = user.Pin;
            roleBox.SelectedItem = user.Role is "CAIXA" or "GERENTE" ? user.Role : "GARCOM";
        }

        staffList.SelectionChanged += (_, _) =>
        {
            if (staffList.SelectedItem is UserAccount user)
            {
                LoadStaff(user);
            }
        };

        var newButton = DialogButton("Novo cadastro", "#2F6FAE");
        newButton.Click += (_, _) => ClearForm();

        var saveButton = DialogButton("Salvar equipe", "#0F766E");
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

            if (string.IsNullOrWhiteSpace(password))
            {
                status.Foreground = RedText;
                status.Text = "Informe a senha.";
                passwordBox.Focus();
                return;
            }

            var duplicatedNumber = Users.FirstOrDefault(item =>
                !ReferenceEquals(item, staffList.SelectedItem)
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
            user.Pin = password;
            user.Role = role;
            user.IsMaster = false;
            user.CanTransfer = role is "GARCOM" or "GERENTE";
            user.CanCash = role is "CAIXA" or "GERENTE";
            user.CanCancel = role is "CAIXA" or "GERENTE";
            user.CanDiscount = role is "CAIXA" or "GERENTE";
            user.CanReports = role == "GERENTE";
            user.CanManageProducts = role == "GERENTE";
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
            Foreground = Solid("#18222B"),
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
        var dialog = CreateDialog("Cardapio digital local", 620, 460);
        var info = new TextBlock
        {
            Text = "Gera um cardapio HTML local com os produtos ativos, separado por categorias, para usar na rede interna do restaurante.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Solid("#18222B"),
            Margin = new Thickness(0, 0, 0, 14)
        };
        var pathText = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = GreenText, FontWeight = FontWeights.SemiBold };
        var generate = DialogButton("Gerar cardapio", "#0F766E");
        generate.Click += (_, _) =>
        {
            var path = Path.Combine(ExportDir, "cardapio-digital.html");
            File.WriteAllText(path, BuildMenuHtml(), Encoding.UTF8);
            pathText.Text = path;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            SetStatus($"Cardapio digital gerado: {path}");
        };

        var panel = DialogPanel();
        panel.Children.Add(info);
        panel.Children.Add(generate);
        panel.Children.Add(pathText);
        panel.Children.Add(DialogHint("Sem modulo web no app: isso apenas exporta o cardapio local que o Windows pode abrir no navegador."));
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void ShowReportsDialog()
    {
        if (!RequirePermission(IsManagerUser, "Relatorios"))
        {
            return;
        }

        var dialog = CreateDialog("Relatorios e BI operacional", 1040, 690);
        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(96) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });

        var periodBox = new ComboBox
        {
            ItemsSource = new[] { "Hoje", "Total" },
            SelectedIndex = 0,
            MinHeight = 34,
            Width = 170,
            Margin = new Thickness(8, 0, 0, 0)
        };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        controls.Children.Add(new TextBlock
        {
            Text = "Periodo do relatorio",
            Foreground = Solid("#667684"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        controls.Children.Add(periodBox);
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

            var metrics = new UniformGrid { Columns = 5, Rows = 1 };
            metrics.Children.Add(CreateMetricCard("Em aberto", Money(openTotal), $"{openOrders.Count} comandas/pedidos", "#245B91"));
            metrics.Children.Add(CreateMetricCard("Caixa atual", Money(_cashTotal), IsCashOpen() ? "caixa aberto" : "caixa fechado", IsCashOpen() ? "#0F766E" : "#A11D1D"));
            metrics.Children.Add(CreateMetricCard("Itens vendidos", soldItems.ToString("N0", Brazil), $"{topProducts.Count} produtos no ranking - {period}", "#99620D"));
            metrics.Children.Add(CreateMetricCard("Lucro bruto", Money(profitSummary.Profit), $"Margem {profitSummary.Margin:N2}% - {period}", profitSummary.Profit >= 0 ? "#0F766E" : "#A11D1D"));
            metrics.Children.Add(CreateMetricCard("Estoque baixo", lowStock.Count.ToString("N0", Brazil), "itens abaixo do minimo", lowStock.Count == 0 ? "#0F766E" : "#A11D1D"));
            metricsHost.Content = metrics;

            var body = new Grid { Margin = new Thickness(0, 14, 0, 0) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star) });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var left = new Grid { Margin = new Thickness(0, 0, 10, 0) };
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.Children.Add(CreateReportSection("Comandas abertas", "Mesas, balcao e delivery com movimento", CreateOrderReportList(openOrders)));

            var topProductsSection = CreateReportSection("Produtos mais vendidos", $"Ranking por quantidade vendida - {period}", CreateProductRanking(topProducts));
            Grid.SetRow(topProductsSection, 1);
            left.Children.Add(topProductsSection);
            body.Children.Add(left);

            var right = new Grid { Margin = new Thickness(10, 0, 0, 0) };
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            right.Children.Add(CreateReportSection("Estoque critico", "Produtos no minimo ou abaixo", CreateLowStockList(lowStock)));

            var cashSection = CreateReportSection("Caixa", $"Total atual {Money(_cashTotal)} - {period}", CreateCashMovementList(cashMovements));
            Grid.SetRow(cashSection, 1);
            right.Children.Add(cashSection);
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
        var openFolder = DialogButton("Abrir pasta", "#2F6FAE");
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
        var export = DialogButton("Exportar TXT", "#0F766E");
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
        dialog.Content = root;
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
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Margin = new Thickness(0, 0, 10, 0),
            ClipToBounds = true
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new Border { Background = Solid(accentColor) });

        var text = new StackPanel { Margin = new Thickness(14, 12, 14, 10) };
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Solid("#667684"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold
        });
        text.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = Solid("#18222B"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 3, 0, 0)
        });
        text.Children.Add(new TextBlock
        {
            Text = detail,
            Foreground = Solid("#667684"),
            FontSize = 11,
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
            BorderBrush = Solid("#D8E2EC"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var grid = new Grid { Margin = new Thickness(14, 12, 14, 12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel();
        header.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Solid("#18222B"),
            FontSize = 15,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Solid("#667684"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0)
        });
        grid.Children.Add(header);

        var scroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 8, 0, 0)
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
                totalProfit >= 0 ? "#0F766E" : "#A11D1D"));
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
                movement.Amount < 0 ? "#A11D1D" : "#0F766E"));
        }

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
            Foreground = Solid("#18222B"),
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        text.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Solid("#667684"),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        var right = new TextBlock
        {
            Text = trailing,
            Foreground = Solid("#18222B"),
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
            Background = Solid("#E8F1FA"),
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
                Foreground = Solid("#667684"),
                FontWeight = FontWeights.SemiBold
            }
        };
    }

    private static string StatusColor(string status)
    {
        return status switch
        {
            "LIVRE" or "PRONTO" or "ENTREGUE" or "FINALIZADO" => "#0F766E",
            "CONTA" or "AGUARDANDO" or "PREPARO" or "PREPARANDO" or "ROTA" => "#99620D",
            _ => "#A11D1D"
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
        SaveStore();
        var dialog = CreateDialog("Backup e exportacao", 560, 360);
        var result = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = GreenText, FontWeight = FontWeights.SemiBold };
        var backup = DialogButton("Gerar backup agora", "#0F766E");
        backup.Click += (_, _) =>
        {
            var backupDir = Path.Combine(_dataRoot, "backups");
            Directory.CreateDirectory(backupDir);
            var path = Path.Combine(backupDir, $"backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(StoreFile, path, overwrite: true);
            result.Text = path;
            SetStatus($"Backup gerado: {path}");
        };
        var open = DialogButton("Abrir pasta de dados", "#2F6FAE");
        open.Click += (_, _) => Process.Start(new ProcessStartInfo(_dataRoot) { UseShellExecute = true });
        var export = DialogButton("Exportar resumo CSV", "#2F6FAE");
        export.Click += (_, _) =>
        {
            var path = Path.Combine(ExportDir, $"produtos-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            var lines = Products.Select(product => $"{product.Code};{product.Name};{product.Category};{product.CostPrice.ToString("N2", Brazil)};{product.Price.ToString("N2", Brazil)};{product.ProfitMargin.ToString("N2", Brazil)};{product.StockQuantity.ToString("N0", Brazil)}");
            File.WriteAllLines(path, new[] { "codigo;nome;grupo;preco_compra;preco_venda;margem_percentual;estoque" }.Concat(lines), Encoding.UTF8);
            result.Text = path;
            SetStatus($"CSV gerado: {path}");
        };

        var panel = DialogPanel();
        panel.Children.Add(DialogLabel("Dados locais"));
        panel.Children.Add(DialogHint(StoreFile));
        panel.Children.Add(backup);
        panel.Children.Add(export);
        panel.Children.Add(open);
        panel.Children.Add(result);
        dialog.Content = panel;
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
        SetStatus($"{board.Kind} {board.Number} excluida/cancelada.");
    }

    private void ReopenCurrentCommand()
    {
        var board = CurrentBoard;
        if (board is null)
        {
            return;
        }

        var restoredClosedCommand = false;
        var hasActiveCommand = board.Lines.Count > 0 || TicketLines.Count > 0 || board.Payments.Count > 0 || Payments.Count > 0;
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
        SetStatus(restoredClosedCommand
            ? $"{board.Kind} {board.Number} reaberta com a ultima conta fechada."
            : $"{board.Kind} {board.Number} reaberta.");
    }

    private static bool HasReceivedPayment(TableTile board)
    {
        return board.LastClosedAt.HasValue
               || !string.IsNullOrWhiteSpace(board.LastReceiptPath)
               || board.ClosedPayments.Count > 0;
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
        var ok = DialogButton("Confirmar pizza", "#0F766E");
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
            Foreground = Solid("#245B91"),
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
        var button = DialogButton(buttonText, "#0F766E");
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.Width = double.NaN;
        var firstAccessButton = DialogButton("Criar primeiro acesso", "#2F6FAE");
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
        var dialog = CreateDialog("Ativacao do Balcao Livre PDV", 620, needsAdmin ? 700 : 430);
        dialog.ResizeMode = ResizeMode.CanResize;

        var keyBox = new TextBox
        {
            Text = _appSettings.ActivationKey,
            Margin = new Thickness(0, 4, 0, 8)
        };
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
        var activate = DialogButton(needsAdmin ? "Ativar e criar administrador" : "Ativar sistema", "#0F766E");
        activate.HorizontalAlignment = HorizontalAlignment.Stretch;
        activate.Width = double.NaN;

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
                    BorderBrush = Solid("#D8E2EC"),
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
                BorderBrush = Solid("#D8E2EC"),
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

                if (needsAdmin)
                {
                    var name = adminNameBox.Text.Trim().ToUpperInvariant();
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
                        Pin = password,
                        Role = "GERENTE",
                        IsMaster = false,
                        CanTransfer = true,
                        CanCash = true,
                        CanCancel = true,
                        CanDiscount = true,
                        CanManageProducts = true,
                        CanReports = true
                    };
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
        keyBox.KeyDown += async (_, e) =>
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
            Foreground = Solid("#245B91"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(DialogLabel("Chave de ativacao"));
        panel.Children.Add(keyBox);
        panel.Children.Add(DialogHint($"Codigo deste computador: {GetMachineCode()}. A chave fica vinculada a este PC."));

        if (needsAdmin)
        {
            panel.Children.Add(new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#D8E2EC"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 14, 0, 4),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Administrador", Foreground = Solid("#18222B"), FontWeight = FontWeights.Bold, FontSize = 15, Margin = new Thickness(0, 0, 0, 8) },
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
        var create = DialogButton("Criar gerente e entrar", "#0F766E");
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
            user.Pin = password;
            user.Role = "GERENTE";
            user.IsMaster = false;
            user.CanTransfer = true;
            user.CanCash = true;
            user.CanCancel = true;
            user.CanDiscount = true;
            user.CanManageProducts = true;
            user.CanReports = true;

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
            Foreground = Solid("#245B91"),
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
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(ledger, JsonOptions), Encoding.UTF8);
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
                && user.Pin == "1234"
                && string.Equals(user.Role, "MASTER", StringComparison.OrdinalIgnoreCase))
            || (string.Equals(user.Name, "CAIXA", StringComparison.OrdinalIgnoreCase)
                && user.Pin == "1111"
                && string.Equals(user.Role, "CAIXA", StringComparison.OrdinalIgnoreCase))
            || (string.Equals(user.Name, "GARCOM", StringComparison.OrdinalIgnoreCase)
                && user.Pin == "2222"
                && string.Equals(user.Role, "GARCOM", StringComparison.OrdinalIgnoreCase));
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
        return string.Equals(user.Pin, password, StringComparison.Ordinal)
            || (string.IsNullOrWhiteSpace(user.Pin)
                && string.Equals(StaffNumber(user), NormalizeStaffNumber(password), StringComparison.Ordinal));
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
            string.Equals(_appSettings.AdminApiUrl.Trim().TrimEnd('/'), "http://localhost:5188", StringComparison.OrdinalIgnoreCase))
        {
            _appSettings.AdminApiUrl = DefaultAdminApiUrl;
            shouldSaveSettings = true;
        }

        if (shouldSaveSettings)
        {
            SaveAppSettings();
        }
    }

    private void SaveAppSettings()
    {
        Directory.CreateDirectory(_dataRoot);
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(_appSettings, JsonOptions), Encoding.UTF8);
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
        Directory.CreateDirectory(_dataRoot);
        File.WriteAllText(ProfileFile, JsonSerializer.Serialize(_profile, JsonOptions), Encoding.UTF8);
    }

    private static TextBlock SectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Solid("#18222B"),
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

    private PrintQueue? GetConfiguredPrintQueue()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_appSettings.PreferredPrinterName))
            {
                var server = new LocalPrintServer();
                var preferred = _appSettings.PreferredPrinterName.Trim();
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
            ?? "1.2.2026";
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
                    ShowToast("Sistema atualizado", $"Versao instalada: {GetAppVersion()}.", "AT", "#0F766E", "#E8F7F4");
                    SetStatus("Nenhuma atualizacao disponivel.");
                }

                return false;
            }

            if (autoInstall)
            {
                ShowToast("Atualizacao encontrada", $"Baixando versao {manifest.Version}.", "AT", "#0F766E", "#E8F7F4");
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
            Foreground = Solid("#667684"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var version = new TextBlock
        {
            Text = $"Nova versao: {manifest.Version}",
            Foreground = Solid("#0F766E"),
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
        var download = DialogButton("Baixar instalador", "#0F766E");
        var later = DialogButton("Depois", "#667684");
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
            Foreground = Solid("#18222B"),
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
            fileName = $"BalcaoLivrePDV-Setup-{SafeFileName(manifest.Version)}.exe";
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
            : "O instalador sera aberto agora.", "AT", "#0F766E", "#E8F7F4");
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

        return builder.Length == 0 ? "1.2.2026" : builder.ToString();
    }

    private static string CopyLogoToAppIdentityFolder(string sourcePath)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RestaurantePro.Windows",
            "identity");
        Directory.CreateDirectory(root);

        var extension = Path.GetExtension(sourcePath);
        var destination = Path.Combine(root, $"restaurant-logo{extension}");
        File.Copy(sourcePath, destination, overwrite: true);
        return destination;
    }

    private static StackPanel DialogPanel()
    {
        return new StackPanel { Margin = new Thickness(18) };
    }

    private static TextBlock DialogLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Solid("#667684"),
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
            Foreground = Solid("#667684"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
    }

    private static Button DialogButton(string text, string color)
    {
        var button = new Button
        {
            Content = text,
            Height = 40,
            MinWidth = 136,
            Padding = new Thickness(18, 0, 18, 0),
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Solid(color),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.SemiBold,
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
    }

    private static Style DialogTextBoxStyle()
    {
        var style = new Style(typeof(TextBox));
        style.Setters.Add(new Setter(Control.HeightProperty, 38d));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 14d));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#18222B")));
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
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#18222B")));
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
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#18222B")));
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
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Solid("#D5DEE7")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto));
        return style;
    }

    private static Style DialogCheckBoxStyle()
    {
        var style = new Style(typeof(CheckBox));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Solid("#18222B")));
        style.Setters.Add(new Setter(Control.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 5, 0, 5)));
        return style;
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
        focused.Setters.Add(new Setter(Border.BorderBrushProperty, Solid("#2F6FAE"), "InputChrome"));
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
                BorderBrush = Solid("#BFD1E2"),
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
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new Border
            {
                Background = Solid("#F8FBFD"),
                BorderBrush = Solid("#D5DEE7"),
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
                Background = Solid("#E8F1FA"),
                CornerRadius = new CornerRadius(7),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "PDV",
                    Foreground = Solid("#245B91"),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var titleText = new TextBlock
            {
                Text = Title,
                Foreground = Solid("#18222B"),
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
                Background = Solid("#EEF4F8"),
                BorderBrush = Solid("#D5DEE7"),
                BorderThickness = new Thickness(1),
                Foreground = Solid("#425466"),
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
                Background = Brushes.White,
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
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"pt-br\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>Balcao Livre PDV</title><style>body{font-family:Segoe UI,Arial;margin:0;background:#f4f7f9;color:#18222b}header{background:#245b91;color:white;padding:22px}.wrap{max-width:920px;margin:auto;padding:18px}.item{background:white;border:1px solid #d5dee7;border-radius:8px;padding:14px;margin:8px 0;display:flex;justify-content:space-between}.price{color:#0f766e;font-weight:700}.cat{margin-top:24px;color:#245b91}</style></head><body>");
        sb.AppendLine("<header><div class=\"wrap\"><h1>Cardapio Digital</h1><p>Pedido local por mesa, retirada ou delivery.</p></div></header><main class=\"wrap\">");
        foreach (var group in Products.Where(product => product.Active).GroupBy(product => product.Category).OrderBy(group => group.Key))
        {
            sb.AppendLine($"<h2 class=\"cat\">{EscapeHtml(group.Key)}</h2>");
            foreach (var product in group.OrderBy(product => product.Name))
            {
                sb.AppendLine($"<div class=\"item\"><span>{EscapeHtml(product.Name)}</span><span class=\"price\">{Money(product.Price)}</span></div>");
            }
        }

        sb.AppendLine("</main></body></html>");
        return sb.ToString();
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

        foreach (var line in TicketLines)
        {
            sb.AppendLine($"{line.Quantity}x {line.Name} {line.Note}");
        }

        var printText = sb.ToString();
        File.WriteAllText(path, printText, Encoding.UTF8);
        KitchenTiles.Add(new TableTile
        {
            Number = $"{KitchenTiles.Count + 1:000000}",
            Kind = "KDS",
            Status = "RECEBIDO",
            Waiter = board.Waiter,
            Lines = TicketLines.Select(CloneLine).ToList(),
            Total = TicketLines.Sum(line => line.Total),
            Detail = $"{board.Kind} {board.Number}"
        });
        SaveStore();
        if (_appSettings.AutoPrintKitchen)
        {
            var printed = TryPrintTextToDefaultPrinter(printText, $"Pedido cozinha {board.Number}", compact: _appSettings.PrintLayout == "PEQUENO");
            SetStatus(printed
                ? $"Pedido de cozinha impresso: {path}"
                : $"Pedido de cozinha gerado: {path}. Impressora padrao indisponivel.");
        }
        else
        {
            SetStatus($"Pedido de cozinha gerado: {path}");
        }
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

    private bool TryPrintTextToDefaultPrinter(string content, string jobName, bool compact, string qrPayload = "", string qrCaption = "")
    {
        try
        {
            var queue = GetConfiguredPrintQueue();
            if (queue is null)
            {
                return false;
            }

            qrPayload = (qrPayload ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(qrPayload) && LooksLikeEscPosPrinter(queue))
            {
                var printerName = string.IsNullOrWhiteSpace(queue.FullName) ? queue.Name : queue.FullName;
                if (TryPrintEscPosReceipt(printerName, content, jobName, compact, qrPayload, qrCaption))
                {
                    return true;
                }
            }

            var dialog = new PrintDialog { PrintQueue = queue };
            var fontSize = compact ? 10.5 : 13.5;
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = fontSize,
                PagePadding = new Thickness(4, 2, 4, 2),
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

    private static BitmapSource? TryCreateQrBitmap(string payload, int pixelsPerModule)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qr = new PngByteQRCode(data);
            var bytes = qr.GetGraphic(pixelsPerModule);
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

        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            Text(line);
            Text("\n");
        }

        qrPayload = (qrPayload ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(qrPayload))
        {
            Text("\n");
            Command(0x1B, 0x61, 0x01);
            Command(0x1D, 0x21, 0x00);
            Text(string.IsNullOrWhiteSpace(qrCaption) ? "QR CODE\n" : $"{qrCaption.Trim().ToUpperInvariant()}\n");

            if (!TryAppendEscPosQrRaster(ms, qrPayload, desiredPrintWidth: 320, maxPrintWidth: 384))
            {
                Command(0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00); // model 2
                Command(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, compact ? (byte)0x07 : (byte)0x08); // module size
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
        if (!IsCashOpen())
        {
            SetStatus("Caixa fechado. Pressione F10 para abrir antes de receber pagamento.");
            return;
        }

        var board = CurrentBoard;
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
        var changeText = new TextBlock
        {
            Text = "Sem troco.",
            Foreground = Solid("#667684"),
            FontWeight = FontWeights.SemiBold
        };
        var changeHint = new TextBlock
        {
            Text = "Digite o valor entregue pelo cliente.",
            Foreground = Solid("#667684"),
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0)
        };
        var changeCard = new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#D8E2EC"),
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
            Foreground = GreenText,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var confirm = DialogButton("Finalizar venda", "#0F766E");
        confirm.HorizontalAlignment = HorizontalAlignment.Stretch;
        confirm.Width = double.NaN;

        var methodButtons = new List<Button>();
        var methodGrid = new UniformGrid { Columns = 3, Rows = 2, Margin = new Thickness(0, 6, 0, 10) };

        void RefreshMethodButtons()
        {
            foreach (var button in methodButtons)
            {
                var isSelected = string.Equals(button.Tag?.ToString(), selectedMethod, StringComparison.Ordinal);
                button.Background = isSelected ? Solid("#E8F7F4") : Brushes.White;
                button.BorderBrush = isSelected ? Solid("#0F766E") : Solid("#D8E2EC");
                button.Foreground = isSelected ? Solid("#0F766E") : Solid("#18222B");
                button.FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold;
            }
        }

        foreach (var method in new[] { "DINHEIRO", "PIX", "CREDITO", "DEBITO", "VALE", "FIADO" })
        {
            var button = new Button
            {
                Content = method,
                Tag = method,
                Height = 42,
                Margin = new Thickness(0, 0, 8, 8),
                Background = Brushes.White,
                BorderBrush = Solid("#D8E2EC"),
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
            Background = Solid("#E8F7F4"),
            BorderBrush = Solid("#0F766E"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Cursor = Cursors.Hand
        };
        var finalizeText = new TextBlock
        {
            Text = "Finalizar conta apos este pagamento",
            Foreground = Solid("#0F766E"),
            FontWeight = FontWeights.Bold
        };
        var finalizeHint = new TextBlock
        {
            Text = "Conta fica fechada e o recebimento entra no caixa.",
            Foreground = Solid("#667684"),
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0)
        };

        void RefreshFinalizeCard()
        {
            finalizeCard.Background = finalizePayment ? Solid("#E8F7F4") : Brushes.White;
            finalizeCard.BorderBrush = finalizePayment ? Solid("#0F766E") : Solid("#D8E2EC");
            finalizeText.Foreground = finalizePayment ? Solid("#0F766E") : Solid("#18222B");
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
                changeText.Foreground = Solid("#667684");
                return;
            }

            var tendered = ParseMoney(amountBox.Text, 0);
            if (tendered <= 0)
            {
                changeText.Text = "Troco: R$ 0,00";
                changeHint.Text = $"Saldo atual: {Money(balance)}";
                changeText.Foreground = Solid("#667684");
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
                changeText.Foreground = selectedMethod == "DINHEIRO" ? Solid("#0F766E") : RedText;
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
            changeText.Foreground = Solid("#667684");
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

        void Confirm()
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

            result = (new PaymentLine
            {
                Payer = string.IsNullOrWhiteSpace(payerBox.Text) ? "Cliente" : payerBox.Text.Trim(),
                Method = selectedMethod,
                Amount = appliedAmount,
                TenderedAmount = amount,
                ChangeAmount = changeAmount,
                When = DateTime.Now
            }, finalizePayment && appliedAmount >= balance);
            dialog.Close();
        }

        confirm.Click += (_, _) => Confirm();
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

        amountBox.TextChanged += (_, _) => RefreshTenderedPreview();

        var panel = new StackPanel { Margin = new Thickness(18, 18, 18, 8) };
        panel.Children.Add(new Border
        {
            Background = Solid("#F8FBFD"),
            BorderBrush = Solid("#D8E2EC"),
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
                            new TextBlock { Text = "Total / pago / saldo", Foreground = Solid("#667684"), FontSize = 12, FontWeight = FontWeights.SemiBold },
                            new TextBlock { Text = $"{Money(total)}  |  Pago {Money(paidTotal)}", Foreground = Solid("#18222B"), FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) },
                            new TextBlock { Text = Money(balance), Foreground = hasOpenBalance ? Solid("#0F766E") : Solid("#667684"), FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 0) }
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
            BorderBrush = Solid("#D8E2EC"),
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
        return IsCompactReceiptLayout() ? 32 : 28;
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
            return "";
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
        CashStatusText.Foreground = IsCashOpen() ? Solid("#667684") : RedText;
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
        ChargeToggleButton.Background = active ? RedText : Solid("#0F766E");
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
        if (!_appSettings.WindowsNotificationsEnabled)
        {
            PlayNotificationSound();
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
        PlayNotificationSound();
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
        color = "#0F766E";
        softColor = "#E8F7F4";

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
            color = "#2F6FAE";
            softColor = "#E8F1FA";
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
        RibbonActions.Add(new("ReopenCommand", "RC", "Reabrir", "Comanda"));
        RibbonActions.Add(new("PeopleCount", "EQ", "Equipe", "Garcom/Caixa"));
        RibbonActions.Add(new("ProductCatalog", "CP", "Cadastro", "Produtos"));
        RibbonActions.Add(new("Cash", "CX", "Caixa", "Movimentos"));
        RibbonActions.Add(new("CloseCash", "F10", "Abrir/Fechar", "Caixa"));
        RibbonActions.Add(new("DeliveryNew", "DL", "Novo", "Delivery"));
        RibbonActions.Add(new("Inventory", "ES", "Estoque", "Receitas"));
        RibbonActions.Add(new("Cardapio", "QR", "Cardapio", "Digital"));
        RibbonActions.Add(new("Reports", "BI", "Relatorios", "Vendas"));
        RibbonActions.Add(new("Backup", "BK", "Backup", "Dados"));
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

        Drivers.Clear();

        Customers.Clear();
        CashMovements.Clear();
        _settings = new LocalHubSettings();
        _profile = new RestaurantIdentityProfile();
        _appSettings = new AppSettings();
        _cashTotal = 0m;
    }

    private void LoadStore()
    {
        try
        {
            if (!File.Exists(StoreFile))
            {
                SeedStore();
                return;
            }

            var store = JsonSerializer.Deserialize<AppStore>(File.ReadAllText(StoreFile), JsonOptions);
            if (store is null || !ApplyStore(store))
            {
                SeedStore();
                return;
            }

            WriteStoreFile(CreateStoreSnapshot());
        }
        catch
        {
            SeedStore();
        }
    }

    private void SaveStore()
    {
        SaveActiveTicketToCurrentBoard();
        var store = CreateStoreSnapshot();
        WriteStoreFile(store);
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
            Settings = _settings,
            CashTotal = _cashTotal
        };
    }

    private bool ApplyStore(AppStore store)
    {
        if (store.RibbonActions.Count > 0)
        {
            RibbonActions.Clear();
            foreach (var item in store.RibbonActions.Where(action => action.Id is not "Users" and not "DeleteCommand"))
            {
                RibbonActions.Add(item.Id switch
                {
                    "ChangeClient" => item with { Title = "Cadastro", Subtitle = "Clientes" },
                    "TransferProducts" => item with { KeyText = "F6", Title = "Transferir", Subtitle = "Comanda" },
                    "PeopleCount" => item with { KeyText = "EQ", Title = "Equipe", Subtitle = "Garcom/Caixa" },
                    _ => item
                });
            }
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
            _appSettings = store.AppSettings;
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
        foreach (var item in store.Products) Products.Add(item);
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

    private void WriteStoreFile(AppStore store)
    {
        Directory.CreateDirectory(_dataRoot);
        File.WriteAllText(StoreFile, JsonSerializer.Serialize(store, JsonOptions), Encoding.UTF8);
    }

    private static void NormalizeLoadedUser(UserAccount user)
    {
        if (string.IsNullOrWhiteSpace(user.EmployeeNumber)
            && user.Role is "GARCOM" or "CAIXA"
            && !string.IsNullOrWhiteSpace(user.Pin))
        {
            user.EmployeeNumber = NormalizeStaffNumber(user.Pin);
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
    }

    private static void NormalizeLoadedBoard(TableTile board)
    {
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
            Sector = line.Sector
        };
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
            "LIVRE" => GreenTile,
            "PRONTO" => GreenTile,
            "ENTREGUE" => GreenTile,
            "FINALIZADO" => GreenTile,
            "CONTA" => AmberTile,
            "AGUARDANDO" => AmberTile,
            "PREPARO" => AmberTile,
            "PREPARANDO" => AmberTile,
            "ROTA" => AmberTile,
            _ => RedTile
        };

        [JsonIgnore] public string TransferDisplay => $"{Kind} {Number}  {Status}  {Money(Total)}";
        [JsonIgnore] public string TransferTotalText => Money(Total);
        [JsonIgnore] public string TransferSubtitle => $"{BoardKindLabel(this)} {Number}  |  {DisplaySubtitle}";
        [JsonIgnore] public string TransferHint => Lines.Count > 0 || Payments.Count > 0
            ? "Ocupada: vai juntar as comandas"
            : "Livre: vai mover a comanda inteira";

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

                return Detail;
            }
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
        public string Sector { get; set; } = "COZINHA";
        public bool Active { get; set; } = true;
        public bool IsPizza { get; set; }
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
        [JsonIgnore] public string SearchDisplay => $"{Code}  {Name}  {Category}  {PriceText}";
        [JsonIgnore] public string StockDisplay => StockQuantity <= MinimumStock && MinimumStock > 0
            ? $"ALERTA  {Code} {Name}  estoque {StockQuantity:N0}  minimo {MinimumStock:N0}"
            : $"{Code} {Name}  estoque {StockQuantity:N0}  minimo {MinimumStock:N0}";
        [JsonIgnore] public bool IsLowStock => MinimumStock > 0 && StockQuantity <= MinimumStock;
        [JsonIgnore] public string StockStatusText => IsLowStock ? "CRITICO" : "OK";

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
        public string Sector { get; set; } = "COZINHA";
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
        public string EmployeeNumber { get; set; } = "";
        public string Role { get; set; } = "CAIXA";
        public bool IsMaster { get; set; }
        public bool CanTransfer { get; set; }
        public bool CanCancel { get; set; }
        public bool CanDiscount { get; set; }
        public bool CanManageProducts { get; set; }
        public bool CanReports { get; set; }
        public bool CanCash { get; set; }
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

    public sealed class CashMovement
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public string Reason { get; set; } = "";
        public string User { get; set; } = "";
        public DateTime When { get; set; } = DateTime.Now;
        [JsonIgnore] public string Display => $"{When:t}  {CashMovementLabel(Type)}  {Money(Amount)}  {Reason}";
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
        public bool AutoPrintDelivery { get; set; } = true;
        public bool AutoPrintKitchen { get; set; } = true;
        public string PrintLayout { get; set; } = "GRANDE";
        public bool LargeReceiptDefaultApplied { get; set; }
        public string PreferredPrinterName { get; set; } = "";
        public bool ReceiptQrEnabled { get; set; }
        public string ReceiptQrKind { get; set; } = "PIX";
        public string ReceiptQrContent { get; set; } = "";
        public int ReceiptSequence { get; set; }
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

    public sealed class AdminClientPayload
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

    public sealed class AdminProfileSnapshot
    {
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
    }

    public sealed class AdminMetricsSnapshot
    {
        public int TablesCount { get; set; }
        public int OpenBoardsCount { get; set; }
        public int DeliveryCount { get; set; }
        public int ProductsCount { get; set; }
        public int UsersCount { get; set; }
        public int CustomersCount { get; set; }
        public decimal CashTotal { get; set; }
        public decimal SalesToday { get; set; }
        public int SoldItemsTotal { get; set; }
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
            }
        }
    }
}
