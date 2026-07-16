using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow : Window
{
    private const string AllSegments = "Todos";
    private const string PreviewAppointmentPrefix = "__preview_agenda_";
    private const string ReportChartAppointments = "Agendamentos por dia";
    private const string ReportChartRevenue = "Receita por dia";
    private const string ReportChartStatus = "Status dos atendimentos";
    private const double ScheduleTimeColumnWidth = 68;
    private const double ScheduleProfessionalColumnWidth = 200;
    private const double ScheduleHeaderHeight = 52;
    private const double ScheduleSlotHeight = 38;
    private const string WhatsAppEvolutionDefaultBaseUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp";
    private const string WhatsAppEvolutionLicenseSecret = "BalcaoLivrePDV-local-license-v1";
    private const string WhatsAppEvolutionLicenseExpires = "203512312359";
    private const string WhatsAppEvolutionLicenseScope = "AGENDALIVRE";
    private const string DefaultMercadoPagoPaymentsApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments";
    private const string MercadoPagoCreditMethod = "Mercado Pago - crédito na maquininha";
    private const string MercadoPagoDebitMethod = "Mercado Pago - débito na maquininha";
    private const double AppModalRadiusValue = 18;
    private const double AppSurfaceRadiusValue = 16;
    private const double AppActionRadiusValue = 14;
    private const double AppBadgeRadiusValue = 12;
    private const string ThemeDefaultWarm = "";
    private const string ThemeSalonClassicGold = "salon-classic-gold";
    private const string ThemeSalonLilacGlow = "salon-lilac-glow";
    private const string ThemeSalonRoseLuxe = "salon-rose-luxe";
    private const string ThemeBarberMidnight = "barber-midnight";
    private const string ThemeBarberEmerald = "barber-emerald";
    private const string ThemeBarberNavy = "barber-navy";
    private const string ThemeMedicalTeal = "medical-teal";
    private const string ThemeMedicalGreen = "medical-green";
    private const string ThemeMedicalBlue = "medical-blue";
    private const string ThemePetCoral = "pet-coral";
    private const string ThemePetLilac = "pet-lilac";
    private const string ThemePetTeal = "pet-teal";
    private const string ThemeWorkshopGold = "workshop-gold";
    private const string ThemeWorkshopOlive = "workshop-olive";
    private const string ThemeWorkshopGraphite = "workshop-graphite";
    private const string ThemeAestheticLavender = "aesthetic-lavender";
    private const string ThemeAestheticSage = "aesthetic-sage";
    private const string ThemeAestheticCoral = "aesthetic-coral";
    private const string ThemePodologyTerracotta = "podology-terracotta";
    private const string ThemePodologyMint = "podology-mint";
    private const string ThemePodologyBlue = "podology-blue";
    private const string ThemeSpaAqua = "spa-aqua";
    private const string ThemeSpaSand = "spa-sand";
    private const string ThemeSpaForest = "spa-forest";
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly HttpClient CepClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };
    private static Brush AccentBrush = Solid("#ED6823");
    private static Brush AccentDarkBrush = Solid("#C95016");
    private static Brush AccentTextBrush = Solid("#B74716");
    private static Brush AccentSoftBrush = Solid("#FFF1E9");
    private static Brush WarmSoftBrush = Solid("#FFF8F3");
    private static Brush RedSoftBrush = Solid("#FCE5E2");
    private static Brush BlueSoftBrush = Solid("#FCE4D8");
    private static Brush YellowSoftBrush = Solid("#FFF0D8");
    private static Brush GraySoftBrush = Solid("#F5F3F0");
    private static Brush PanelBrush = Solid("#FFFFFF");
    private static Brush InkBrush = Solid("#1C1B1A");
    private static Brush MutedBrush = Solid("#716B66");
    private static Brush LineBrush = Solid("#E8E3DE");
    private static Brush SidebarTextBrush = Solid("#48423D");
    private static Brush SidebarActiveTextBrush = AccentBrush;
    private static Brush SidebarActiveBackgroundBrush = Solid("#FFF1E9");
    private static string ActiveThemeId = ThemeDefaultWarm;
    private static Brush[] ReportPalette =
    [
        AccentBrush,
        Solid("#14B8A6"),
        Solid("#0F172A"),
        Solid("#8B5CF6"),
        Solid("#F59E0B"),
        Solid("#10B981"),
        Solid("#EF4444")
    ];
    private static Brush[] ReportSoftPalette =
    [
        AccentSoftBrush,
        Solid("#ECFDF5"),
        Solid("#F4EEE8"),
        Solid("#F3E8FF"),
        Solid("#FEF3C7"),
        Solid("#DCFCE7"),
        RedSoftBrush
    ];

    private readonly AgendaDataStore _store = new();
    private readonly ObservableCollection<AppointmentRow> _dayRows = [];
    private readonly ObservableCollection<AppointmentRow> _weekRows = [];
    private readonly ObservableCollection<WeekSummaryRow> _weekSummaryRows = [];
    private readonly ObservableCollection<MetricRow> _metrics = [];
    private readonly ObservableCollection<HomeMetricRow> _homeMetrics = [];
    private readonly ObservableCollection<HomeAgendaSummaryRow> _homeAgendaRows = [];
    private readonly ObservableCollection<HomeServiceRow> _homeTopServices = [];
    private readonly ObservableCollection<HomeCustomerSummaryRow> _homeRecentCustomers = [];
    private readonly ObservableCollection<HomeAlertRow> _homeAlerts = [];
    private readonly ObservableCollection<HomeFinanceBarRow> _homeFinanceBars = [];
    private readonly ObservableCollection<EstablishmentMetricRow> _establishmentMetrics = [];
    private readonly ObservableCollection<EstablishmentSectionRow> _establishmentSections = [];
    private readonly ObservableCollection<EstablishmentListRow> _establishmentClients = [];
    private readonly ObservableCollection<EstablishmentListRow> _establishmentProfessionals = [];
    private readonly ObservableCollection<EstablishmentListRow> _establishmentServices = [];
    private readonly ObservableCollection<EstablishmentListRow> _establishmentProducts = [];
    private readonly ObservableCollection<EstablishmentListRow> _establishmentSales = [];
    private Popup? _appointmentInfoPopup;
    private MouseButtonEventHandler? _appointmentInfoOutsideClickHandler;
    private Popup? _appointmentQuickEditPopup;
    private MouseButtonEventHandler? _appointmentQuickEditOutsideClickHandler;
    private Popup? _customerInfoPopup;
    private MouseButtonEventHandler? _customerInfoOutsideClickHandler;
    private Popup? _professionalInfoPopup;
    private MouseButtonEventHandler? _professionalInfoOutsideClickHandler;
    private Popup? _serviceInfoPopup;
    private MouseButtonEventHandler? _serviceInfoOutsideClickHandler;
    private Popup? _financeChartInfoPopup;
    private readonly ObservableCollection<HomeFinanceBarRow> _financeEntries = [];
    private readonly ObservableCollection<EstablishmentListRow> _financePendingPayments = [];
    private readonly ObservableCollection<EstablishmentListRow> _financeExpenses = [];
    private readonly ObservableCollection<HomeFinanceBarRow> _financeChartRows = [];
    private readonly ObservableCollection<EstablishmentMetricRow> _reportsMetrics = [];
    private readonly ObservableCollection<string> _reportChartOptions = [];
    private readonly ObservableCollection<ReportChartRow> _reportsColumnChartRows = [];
    private readonly ObservableCollection<ReportChartRow> _reportsLineChartRows = [];
    private readonly ObservableCollection<ReportChartRow> _reportsDonutChartRows = [];
    private readonly ObservableCollection<ReportChartRow> _reportsRankingChartRows = [];
    private IReadOnlyList<ReportChartRow> _activeReportChartRows = [];
    private readonly ObservableCollection<EstablishmentListRow> _reportsInsights = [];
    private readonly ObservableCollection<EstablishmentListRow> _reportsServices = [];
    private readonly ObservableCollection<EstablishmentListRow> _reportsProfessionals = [];
    private readonly ObservableCollection<MarketingContactRow> _marketingContacts = [];
    private readonly ObservableCollection<EstablishmentListRow> _marketingMessages = [];
    private readonly ObservableCollection<EstablishmentListRow> _marketingCampaigns = [];
    private string _lastAppliedPromotionName = "";
    private string _lastAppliedPromotionOffer = "";
    private string _lastAppliedPromotionMessage = "";
    private readonly ObservableCollection<WhatsAppConversationRow> _whatsAppConversations = [];
    private readonly ObservableCollection<WhatsAppMessageRow> _whatsAppMessages = [];
    private readonly DispatcherTimer _whatsAppPollTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _statusToastTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly DispatcherTimer _searchRefreshTimer = new() { Interval = TimeSpan.FromMilliseconds(280) };
    private readonly DispatcherTimer _snapshotExportTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private bool _whatsAppPollRunning;
    private string _selectedWhatsAppReplyPhone = "";
    private string _selectedWhatsAppReplyName = "";
    private bool _whatsAppConversationOpen;
    private FrameworkElement? _whatsAppReturnFocusElement;
    private readonly ObservableCollection<ProfessionalDayRow> _professionalRows = [];
    private readonly ObservableCollection<RecentCustomerRow> _recentCustomers = [];
    private readonly ObservableCollection<ServiceItem> _filteredServices = [];
    private readonly ObservableCollection<Professional> _filteredProfessionals = [];
    private readonly ObservableCollection<string> _resourceOptions = [];
    private readonly IReadOnlyList<OnboardingTemplate> _onboardingTemplates = OnboardingTemplate.CreateDefaults();
    private static readonly string[] OnboardingStepTitles =
    [
        "Dados iniciais",
        "Segmento do negócio",
        "Tamanho da equipe",
        "Objetivo principal",
        "Endereço",
        "Revisão"
    ];
    private static readonly string[] OnboardingStepCaptions =
    [
        "Identifique o responsável e o nome que aparecerá no sistema.",
        "Escolha o setor para carregar serviços e recursos mais próximos da sua rotina.",
        "Informe quantas pessoas atendem para preparar a agenda do tamanho certo.",
        "Marque a prioridade inicial para a configuração nascer alinhada ao seu uso.",
        "Cadastre onde o negócio funciona para consultas e relatórios.",
        "Confira os dados principais e conclua a configuração inicial."
    ];

    private readonly string[] _segments =
    [
        "Clínica médica",
        "Petshop",
        "Mecânica",
        "Unha e beleza",
        "Cabelo e barbearia"
    ];

    private readonly int[] _durationOptions = [15, 20, 25, 30, 35, 40, 45, 60, 75, 90, 120, 150, 180, 240];
    private readonly List<string> _timeOptions = [];

    private AgendaData _data = new();
    private Appointment? _selectedAppointment;
    private OnboardingTemplate? _selectedOnboardingTemplate;
    private DateTime _selectedDate = DateTime.Today;
    private DateTime _datePopoverStart = DateTime.Today.AddDays(-2);
    private bool _suppressDatePopoverCalendarSelection;
    private string _selectedSegmentFilter = AllSegments;
    private string _selectedProfessionalCount = "";
    private string _selectedObjective = "";
    private string _selectedOnboardingThemeId = "";
    private int _onboardingStep;
    private bool _showingThemeSelection;
    private bool _loadingEditor;
    private bool _formattingCustomerPhone;
    private bool _formattingDialogText;
    private bool _formattingOnboardingCep;
    private bool _mainWindowInitialized;
    private bool _syncingSelection;
    private int _appointmentEditorStep;
    private IInputElement? _appointmentEditorPreviousFocus;
    private int _appDialogBackdropDepth;
    private bool _sidebarCollapsed;
    private bool _configuringReportChart;
    private int _agendaModeIndex;
    private MainPage _currentPage = MainPage.Home;
    private Appointment? _homeNextAppointment;
    private string _lastOnboardingCepLookup = "";
    private CancellationTokenSource? _cepLookupCancellation;

    private enum MainPage
    {
        Home,
        Establishment,
        Finance,
        Reports,
        Marketing,
        Settings,
        Agenda
    }

    private enum ReportChartStyle
    {
        Columns,
        Line,
        Donut,
        Ranking
    }

    private sealed record VisualTheme(
        string Id,
        string Name,
        string FontFamily,
        string AppBackground,
        string Panel,
        string Accent,
        string AccentDark,
        string AccentSoft,
        string BlueSoft,
        string WarmSoft,
        string Line,
        string Ink,
        string Muted,
        string SidebarBackground,
        string SidebarActive,
        string SidebarBorder,
        string SidebarText,
        string RedSoft,
        string YellowSoft,
        string GraySoft);

    private sealed record OnboardingThemeChoice(
        string Id,
        string Title,
        string Description,
        string ImagePath,
        string PreviewBackground,
        string PreviewBorder);

    private sealed record DatePopoverDay(
        DateTime Date,
        string DayLabel,
        string DayNumber,
        string TodayLabel,
        bool IsSelected,
        string AutomationName,
        string SelectionStatus);

    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(ComboBox),
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(FormComboBox_PreviewKeyDown),
            handledEventsToo: true);
        EventManager.RegisterClassHandler(
            typeof(DatePicker),
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(FormDatePicker_PreviewKeyDown),
            handledEventsToo: true);
    }

    public MainWindow()
    {
        InitializeComponent();
        _statusToastTimer.Tick += (_, _) => HideStatusToast();
        _searchRefreshTimer.Tick += (_, _) =>
        {
            _searchRefreshTimer.Stop();
            RefreshAll();
        };
        _snapshotExportTimer.Tick += (_, _) =>
        {
            _snapshotExportTimer.Stop();
            ExportWhatsAppAgendaSnapshot();
        };
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_WhatsAppRealtimeClosed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mainWindowInitialized)
        {
            return;
        }

        _mainWindowInitialized = true;
        _whatsAppPollTimer.Tick += async (_, _) => await PollWhatsAppEvolutionMessagesAsync();

        _data = _store.LoadOrCreate();
        ApplyVisualTheme(ThemeById(_data.Settings.ThemeId), refreshVisibleData: false);
        var dataChanged = false;
        dataChanged |= PruneProfessionalsForSelectedCount();
        dataChanged |= RemoveBlockedAppointments();
        if (dataChanged)
        {
            _store.Save(_data);
        }

        ConfigureOnboardingInputs();
        ConfigureInputs();
        ConfigureSidebarHover();
        ClearEditor();
        RefreshAll();
        UpdateWhatsAppPollingState();
        StartWhatsAppRealtime();
        ApplyBusinessLabels();
        ShowMainPage(MainPage.Home);

        DataPathText.Text = _store.DataPath;
        ShowStatus("Agenda pronta para usar.");

        if (NeedsOnboarding())
        {
            ShowOnboarding();
        }

        CaptureAuditScreenshotIfRequested();
    }

    private void CaptureAuditScreenshotIfRequested()
    {
        var path = Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_SCREENSHOT_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var auditViewport = Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_VIEWPORT");
        var viewportParts = auditViewport?.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (viewportParts is { Length: 2 } &&
            double.TryParse(viewportParts[0], out var auditWidth) &&
            double.TryParse(viewportParts[1], out var auditHeight) &&
            auditWidth >= MinWidth &&
            auditHeight >= MinHeight)
        {
            WindowState = WindowState.Normal;
            Width = auditWidth;
            Height = auditHeight;
        }

        var auditState = Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE");
        switch (auditState?.Trim().ToLowerInvariant())
        {
            case "home":
                ShowMainPage(MainPage.Home);
                break;
            case "agenda":
                ShowMainPage(MainPage.Agenda);
                break;
            case "finance":
            case "financeiro":
                ShowMainPage(MainPage.Finance);
                break;
            case "whatsapp-panel":
                ShowMainPage(MainPage.Home);
                OpenWhatsAppPanelButton_Click(this, new RoutedEventArgs());
                break;
            case "onboarding-0":
            case "onboarding-1":
            case "onboarding-2":
            case "onboarding-3":
            case "onboarding-4":
            case "onboarding-5":
            {
                ShowOnboarding();
                var stepText = auditState[^1].ToString();
                ShowOnboardingStep(int.Parse(stepText, CultureInfo.InvariantCulture));
                break;
            }
            case "onboarding-theme":
                ShowOnboarding();
                ShowThemeSelectionStep();
                break;
        }

        var auditAppointment = string.Equals(auditState, "appointment", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(auditState, "appointment-client", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(auditState, "appointment-confirm", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(auditState, "appointment-top", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(auditState, "appointment-top-date", StringComparison.OrdinalIgnoreCase);
        if (auditAppointment)
        {
            ClearEditor();
            _agendaModeIndex = 0;
            UpdateAgendaModeButtons();
            OpenAppointmentEditorModal();

            if (string.Equals(auditState, "appointment-client", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(auditState, "appointment-confirm", StringComparison.OrdinalIgnoreCase))
            {
                CustomerNameTextBox.Text = "Mariana Costa";
                PhoneTextBox.Text = "(11) 99876-5432";
                CustomerProfileTextBox.Text = "Prefere atendimento no fim da tarde";
                NotesTextBox.Text = "Primeira visita — confirmar pelo WhatsApp.";
                SetAppointmentEditorStep(
                    string.Equals(auditState, "appointment-confirm", StringComparison.OrdinalIgnoreCase) ? 2 : 1);
            }
        }

        if (!string.IsNullOrWhiteSpace(auditState))
        {
            StatusToastBorder.BeginAnimation(OpacityProperty, null);
            StatusToastBorder.Opacity = 0;
            StatusToastBorder.Visibility = Visibility.Collapsed;
        }

        if (IsDialogAuditState(auditState))
        {
            Dispatcher.BeginInvoke(
                () => CaptureDialogAuditState(auditState!, path),
                DispatcherPriority.ApplicationIdle);
            return;
        }

        Dispatcher.BeginInvoke(() =>
            {
                UpdateLayout();
                if (string.Equals(auditState, "appointment-top-date", StringComparison.OrdinalIgnoreCase))
                {
                    AppointmentDatePicker.Focus();
                    UpdateLayout();
                }

                CaptureAuditScreenshot(path);
            },
            DispatcherPriority.ApplicationIdle);
    }

    private static bool IsDialogAuditState(string? state) => state?.ToLowerInvariant() is
        "dialog-payment" or
        "dialog-expense" or
        "dialog-product-sale" or
        "dialog-mercado-pago" or
        "manager-clients" or
        "manager-professionals" or
        "manager-services" or
        "dialog-customer" or
        "dialog-service" or
        "dialog-professional" or
        "dialog-business-hours" or
        "dialog-registration";

    private void CaptureDialogAuditState(string state, string path)
    {
        var auditThemeId = Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_THEME_ID");
        if (!string.IsNullOrWhiteSpace(auditThemeId))
        {
            ApplyVisualTheme(ThemeById(auditThemeId), refreshVisibleData: false);
        }

        var captured = false;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(420) };
        timer.Tick += (_, _) =>
        {
            var dialog = OwnedWindows.Cast<Window>().LastOrDefault(window => window.IsVisible);
            if (dialog is null || !dialog.IsLoaded || dialog.ActualWidth <= 0 || dialog.ActualHeight <= 0)
            {
                return;
            }

            dialog.UpdateLayout();
            CaptureAuditScreenshot(dialog, path);
            captured = true;
            timer.Stop();
            dialog.Close();
        };
        timer.Start();

        switch (state.ToLowerInvariant())
        {
            case "dialog-payment":
                ShowPaymentEditorDialog();
                break;
            case "dialog-expense":
                ShowExpenseEditorDialog();
                break;
            case "dialog-product-sale":
            {
                ProductItem? auditProduct = null;
                if (_data.Products.Count == 0)
                {
                    auditProduct = new ProductItem
                    {
                        Id = "__audit_product__",
                        Name = "Kit de cuidados",
                        Category = "Beleza",
                        Price = 89.90m,
                        StockQuantity = 12
                    };
                    _data.Products.Add(auditProduct);
                }

                try
                {
                    ShowProductSaleEditorDialog();
                }
                finally
                {
                    if (auditProduct is not null)
                    {
                        _data.Products.Remove(auditProduct);
                    }
                }

                break;
            }
            case "dialog-mercado-pago":
                OpenMercadoPagoSettingsButton_Click(this, new RoutedEventArgs());
                break;
            case "manager-clients":
                ShowEstablishmentManagerDialog("Clientes");
                break;
            case "manager-professionals":
                ShowEstablishmentManagerDialog("Profissionais");
                break;
            case "manager-services":
                ShowEstablishmentManagerDialog("Serviços");
                break;
            case "dialog-customer":
                ShowCustomerEditorDialog(FirstFilled(_data.Settings.BusinessSegment, "Salão de Beleza"));
                break;
            case "dialog-service":
                ShowServiceEditorDialog("Barbearia");
                break;
            case "dialog-professional":
                ShowProfessionalEditorDialog("Barbearia");
                break;
            case "dialog-business-hours":
                OpenBusinessHoursButton_Click(this, new RoutedEventArgs());
                break;
            case "dialog-registration":
                ShowRegistrationEditorDialog();
                break;
        }

        timer.Stop();
        if (captured)
        {
            Dispatcher.BeginInvoke(Close, DispatcherPriority.ApplicationIdle);
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DateFilterPopup.IsOpen)
        {
            DateFilterPopup.IsOpen = false;
            DateFilterButton.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && WhatsAppFloatingPanel.Visibility == Visibility.Visible)
        {
            if (_whatsAppConversationOpen)
            {
                _whatsAppConversationOpen = false;
                RefreshWhatsAppSurface();
                FocusWhatsAppPanel();
            }
            else
            {
                CloseWhatsAppPanel();
            }

            e.Handled = true;
            return;
        }

        if (e.Key != Key.F12)
        {
            HandleFormKeyboardNavigation(e);
            return;
        }

        var path = Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_SCREENSHOT_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        CaptureAuditScreenshot(path);
        e.Handled = true;
    }

    private void CaptureAuditScreenshot(string path)
    {
        CaptureAuditScreenshot(this, path);
    }

    private static void CaptureAuditScreenshot(Window source, string path)
    {
        try
        {
            source.UpdateLayout();
            var dpi = VisualTreeHelper.GetDpi(source);
            var width = Math.Max(1, (int)Math.Ceiling(source.ActualWidth * dpi.DpiScaleX));
            var height = Math.Max(1, (int)Math.Ceiling(source.ActualHeight * dpi.DpiScaleY));
            var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bitmap.Render(source);

            var fullPath = System.IO.Path.GetFullPath(path);
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = System.IO.File.Create(fullPath);
            encoder.Save(stream);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Debug.WriteLine($"Audit screenshot skipped: {ex.Message}");
        }
    }

    private static VisualTheme ThemeById(string? themeId)
    {
        return NormalizeTemplateLookup(themeId ?? "") switch
        {
            "SALONCLASSICGOLD" => new(
                ThemeSalonClassicGold,
                "Clássico dourado",
                "Georgia",
                "#FFF9EC",
                "#FFFFFF",
                "#C99A2E",
                "#9F7422",
                "#FFF4D5",
                "#F7E7B4",
                "#FFFBF2",
                "#EBDAB5",
                "#2A201A",
                "#756A58",
                "#FFFBF2",
                "#F4DF9F",
                "#EBDAB5",
                "#5A431E",
                "#FCE8E8",
                "#FFF7D6",
                "#F4EFE3"),
            "SALONLILACGLOW" => new(
                ThemeSalonLilacGlow,
                "Lilás glow",
                "Georgia",
                "#FCF9FF",
                "#FFFFFF",
                "#8757D9",
                "#6F42C1",
                "#F3EAFE",
                "#EDE0FF",
                "#FBF7FF",
                "#E4D8F5",
                "#241A34",
                "#6F6380",
                "#9D79D7",
                "#FFFFFF",
                "#8E68CB",
                "#FFFFFF",
                "#FDE7EE",
                "#FFF2D7",
                "#F4EFFA"),
            "SALONROSELUXE" => new(
                ThemeSalonRoseLuxe,
                "Rose luxe",
                "Georgia",
                "#FFF7FA",
                "#FFFFFF",
                "#C23A6A",
                "#9E244E",
                "#FFE8F1",
                "#FADCE9",
                "#FFF5F8",
                "#F0CBD8",
                "#2E1823",
                "#7C6470",
                "#B85A74",
                "#FFE7EF",
                "#A94C66",
                "#FFFFFF",
                "#FFE3EA",
                "#FFF2D7",
                "#F7EEF2"),
            "BARBERMIDNIGHT" => new(
                ThemeBarberMidnight,
                "Preto clássico",
                "Segoe UI",
                "#F6F7F8",
                "#FFFFFF",
                "#202830",
                "#141A20",
                "#EEF0F2",
                "#E7EAED",
                "#F5F6F7",
                "#DDE3E8",
                "#111827",
                "#5E6874",
                "#20272D",
                "#38434D",
                "#3D474F",
                "#F8FAFC",
                "#FDE8E8",
                "#FFF4D6",
                "#EFF2F5"),
            "BARBEREMERALD" => new(
                ThemeBarberEmerald,
                "Verde barbearia",
                "Segoe UI",
                "#F6FAF9",
                "#FFFFFF",
                "#00796B",
                "#005A50",
                "#DFF4F0",
                "#E8F7F4",
                "#F6FCFA",
                "#D7E8E4",
                "#10201E",
                "#5E716D",
                "#003D38",
                "#E8F5F2",
                "#0B5B53",
                "#F8FFFD",
                "#FDE8E8",
                "#FFF4D6",
                "#EDF5F3"),
            "BARBERNAVY" => new(
                ThemeBarberNavy,
                "Azul naval",
                "Segoe UI",
                "#F7FAFE",
                "#FFFFFF",
                "#062F63",
                "#031D3C",
                "#E7EEF9",
                "#EAF1FA",
                "#F8FBFF",
                "#D9E5F2",
                "#0D1B2F",
                "#5C6878",
                "#062B55",
                "#F7F2EC",
                "#22466E",
                "#F8FAFC",
                "#FDE8E8",
                "#FFF4D6",
                "#EEF3FA"),
            "MEDICALTEAL" => new(
                ThemeMedicalTeal,
                "Teal clínico",
                "Segoe UI",
                "#F7FCFC",
                "#FFFFFF",
                "#07989A",
                "#05777B",
                "#DDF6F5",
                "#EAF7F8",
                "#FAFDFD",
                "#CFE7E8",
                "#10233D",
                "#64778A",
                "#088E91",
                "#E1F7F5",
                "#2AA0A2",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF4D6",
                "#EFF7F7"),
            "MEDICALGREEN" => new(
                ThemeMedicalGreen,
                "Verde acolhe",
                "Segoe UI",
                "#F8FCFA",
                "#FFFFFF",
                "#2F9B76",
                "#1F7E5F",
                "#E2F4ED",
                "#EAF6F1",
                "#FBFEFC",
                "#D5E7DF",
                "#10231D",
                "#61736A",
                "#118762",
                "#E4F5EE",
                "#47A882",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF4D6",
                "#EFF7F3"),
            "MEDICALBLUE" => new(
                ThemeMedicalBlue,
                "Azul saúde",
                "Segoe UI",
                "#F8FBFF",
                "#FFFFFF",
                "#2478E6",
                "#145EC2",
                "#E6F0FF",
                "#EAF2FF",
                "#FBFDFF",
                "#D3E2F8",
                "#10213A",
                "#607188",
                "#0D6DDE",
                "#E4EFFF",
                "#2C82E6",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF4D6",
                "#EEF4FB"),
            "PETCORAL" => new(
                ThemePetCoral,
                "Coral pet",
                "Segoe UI",
                "#FFF8F5",
                "#FFFFFF",
                "#EB6F62",
                "#C8574B",
                "#FDE8E2",
                "#FCEDE9",
                "#FFFBF8",
                "#F0D2CB",
                "#241B1A",
                "#756562",
                "#D95E52",
                "#FFE9E3",
                "#F28274",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF1D6",
                "#F7EEEA"),
            "PETLILAC" => new(
                ThemePetLilac,
                "Lilás pet",
                "Segoe UI",
                "#FCF9FF",
                "#FFFFFF",
                "#8354D8",
                "#6A3DBD",
                "#F0E7FF",
                "#EDE2FF",
                "#FBF7FF",
                "#E3D6F5",
                "#211932",
                "#6D6380",
                "#8152CF",
                "#F3EAFF",
                "#A17BE2",
                "#FFFFFF",
                "#FDE7EE",
                "#FFF3D8",
                "#F4EFFA"),
            "PETTEAL" => new(
                ThemePetTeal,
                "Verde pet",
                "Segoe UI",
                "#F6FCFB",
                "#FFFFFF",
                "#0A9B94",
                "#077A74",
                "#DDF6F2",
                "#E7F7F5",
                "#F8FDFC",
                "#CFE7E4",
                "#0F2424",
                "#5E7472",
                "#078F89",
                "#E0F6F3",
                "#29AEA6",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF4D6",
                "#ECF7F5"),
            "WORKSHOPGOLD" => new(
                ThemeWorkshopGold,
                "Mecânica ouro",
                "Segoe UI",
                "#FAF8F2",
                "#FFFFFF",
                "#C99B2B",
                "#9B741D",
                "#FFF2D6",
                "#F8F0D7",
                "#FFFCF6",
                "#E7D8B8",
                "#171815",
                "#6E695C",
                "#333425",
                "#6B6030",
                "#5A5132",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF2D5",
                "#F3EFE6"),
            "WORKSHOPOLIVE" => new(
                ThemeWorkshopOlive,
                "Verde automotivo",
                "Segoe UI",
                "#F8FAF3",
                "#FFFFFF",
                "#6F8220",
                "#4F6112",
                "#EDF3D8",
                "#EEF4DC",
                "#FCFDF8",
                "#D9E2BD",
                "#151A10",
                "#646F54",
                "#2F3E16",
                "#6A772C",
                "#536329",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF4D6",
                "#EEF2E6"),
            "WORKSHOPGRAPHITE" => new(
                ThemeWorkshopGraphite,
                "Grafite oficina",
                "Segoe UI",
                "#F7F8F8",
                "#FFFFFF",
                "#5D6B6B",
                "#3D484A",
                "#E8ECEC",
                "#EDF1F1",
                "#FBFCFC",
                "#D9DFDF",
                "#101820",
                "#66757A",
                "#111A20",
                "#37434A",
                "#2C363D",
                "#FFFFFF",
                "#FCE8E8",
                "#FFF4D6",
                "#EEF1F2"),
            "AESTHETICLAVENDER" => new(
                ThemeAestheticLavender,
                "Lavanda wellness",
                "Segoe UI",
                "#FBF9FF",
                "#FFFFFF",
                "#7B62B8",
                "#654FA1",
                "#EFE9FB",
                "#EAE4F7",
                "#FCFAFF",
                "#E4DAF2",
                "#1F1D2E",
                "#6D6682",
                "#FCFAFF",
                "#ECE6F8",
                "#E3D9F1",
                "#66548F",
                "#FCE8E8",
                "#FFF4D6",
                "#F3EFF8"),
            "AESTHETICSAGE" => new(
                ThemeAestheticSage,
                "Sálvia natural",
                "Georgia",
                "#FBFBF6",
                "#FFFFFF",
                "#748463",
                "#5E6E4F",
                "#EEF3E7",
                "#E9EFE1",
                "#FCFBF6",
                "#DFE6D5",
                "#20251D",
                "#68715F",
                "#FCFBF6",
                "#EEF1E6",
                "#E2E7D9",
                "#5C674F",
                "#FCE8E8",
                "#F8EFCF",
                "#F1F2EA"),
            "AESTHETICCORAL" => new(
                ThemeAestheticCoral,
                "Coral glow",
                "Segoe UI",
                "#FFF9F7",
                "#FFFFFF",
                "#D87364",
                "#B85A4D",
                "#FBE5E0",
                "#F8DDD8",
                "#FFFBFA",
                "#F1D5CF",
                "#2F1D1A",
                "#7B6762",
                "#FFFBFA",
                "#F5D6CF",
                "#EED5CF",
                "#7C514B",
                "#FCE8E8",
                "#FFF1D6",
                "#F7EEEB"),
            "PODOLOGYTERRACOTTA" => new(
                ThemePodologyTerracotta,
                "Essencial terracota",
                "Segoe UI",
                "#FFFBF8",
                "#FFFFFF",
                "#C85632",
                "#A94527",
                "#FBE3DB",
                "#F8E7E0",
                "#FFF8F4",
                "#EED6CD",
                "#2D211D",
                "#75665F",
                "#FFFBF8",
                "#F8D7C9",
                "#EDD5CC",
                "#7A4A3A",
                "#FCE8E8",
                "#FFF1D6",
                "#F5EEE9"),
            "PODOLOGYMINT" => new(
                ThemePodologyMint,
                "Bem-estar verde",
                "Segoe UI",
                "#F8FEFC",
                "#FFFFFF",
                "#43A67E",
                "#2F8A68",
                "#E3F4ED",
                "#DEF0E9",
                "#FBFFFD",
                "#D6E8E0",
                "#17231F",
                "#60736B",
                "#FBFFFD",
                "#E1F2EA",
                "#D4E7DF",
                "#3F705E",
                "#FCE8E8",
                "#FFF4D6",
                "#ECF5F1"),
            "PODOLOGYBLUE" => new(
                ThemePodologyBlue,
                "Azul podologia",
                "Segoe UI",
                "#F8FBFF",
                "#FFFFFF",
                "#0F74D3",
                "#0B5EAD",
                "#E7F1FE",
                "#E4EFFD",
                "#FBFDFF",
                "#D8E6F7",
                "#10213A",
                "#5E6E82",
                "#FBFDFF",
                "#E4F0FD",
                "#D6E5F7",
                "#315B87",
                "#FCE8E8",
                "#FFF4D6",
                "#EEF4FB"),
            "SPAAQUA" => new(
                ThemeSpaAqua,
                "Água calma",
                "Segoe UI",
                "#F7FBFF",
                "#FFFFFF",
                "#6EA7D3",
                "#4E7FA8",
                "#E4F3FC",
                "#DDEFFB",
                "#F9FCFF",
                "#D7E7F4",
                "#102B4B",
                "#607E99",
                "#2F6288",
                "#DCEEFF",
                "#497FA5",
                "#FFFFFF",
                "#FCE8E8",
                "#F3EDFF",
                "#EEF7FD"),
            "SPASAND" => new(
                ThemeSpaSand,
                "Areia serena",
                "Georgia",
                "#FFF9FB",
                "#FFFFFF",
                "#B46D82",
                "#8E5265",
                "#F8E7EC",
                "#F9EEF1",
                "#FFFBFC",
                "#E9D2DA",
                "#2A1C22",
                "#77636B",
                "#7E5260",
                "#F5DFE6",
                "#9A6171",
                "#FFFFFF",
                "#FCE7EC",
                "#F8EFD5",
                "#F6EEF1"),
            "SPAFOREST" => new(
                ThemeSpaForest,
                "Floresta zen",
                "Georgia",
                "#FCFBF6",
                "#FFFFFF",
                "#536B3E",
                "#40532F",
                "#E8EEDC",
                "#E3EAD7",
                "#FFFDF7",
                "#DDE4D0",
                "#20281B",
                "#6A7460",
                "#3F532F",
                "#E7EBD9",
                "#63724F",
                "#FFFFFF",
                "#FCE8E8",
                "#F6EDCF",
                "#EFF2E8"),
            _ => new(
                ThemeDefaultWarm,
                "Agenda Livre",
                "Segoe UI Variable Text",
                "#FAF9F7",
                "#FFFFFF",
                "#ED6823",
                "#C95016",
                "#FFF1E9",
                "#FCE4D8",
                "#FFF8F3",
                "#E8E3DE",
                "#1C1B1A",
                "#716B66",
                "#FFFFFF",
                "#FFF1E9",
                "#ECE7E2",
                "#48423D",
                "#FCE5E2",
                "#FFF0D8",
                "#F5F3F0")
        };
    }

    private void ApplyVisualTheme(VisualTheme theme, bool refreshVisibleData)
    {
        ActiveThemeId = theme.Id;
        AccentBrush = Solid(theme.Accent);
        AccentDarkBrush = Solid(theme.AccentDark);
        AccentTextBrush = AccentDarkBrush;
        AccentSoftBrush = Solid(theme.AccentSoft);
        WarmSoftBrush = Solid(theme.WarmSoft);
        RedSoftBrush = Solid(theme.RedSoft);
        BlueSoftBrush = Solid(theme.BlueSoft);
        YellowSoftBrush = Solid(theme.YellowSoft);
        GraySoftBrush = Solid(theme.GraySoft);
        PanelBrush = Solid(theme.Panel);
        InkBrush = Solid(theme.Ink);
        MutedBrush = Solid(theme.Muted);
        LineBrush = Solid(theme.Line);
        ApplyMaterialPalette(theme);
        ApplySystemSelectionPalette();
        var usesDarkSidebar = ThemeUsesDarkSidebar(theme);
        SidebarTextBrush = usesDarkSidebar ? Solid("#F1EEE9") : Solid(theme.Ink);
        SidebarActiveTextBrush = usesDarkSidebar ? Brushes.White : Solid(theme.AccentDark);
        SidebarActiveBackgroundBrush = SidebarActiveBackground(theme);
        var sidebarBorderBrush = SidebarBorderSurface(theme);
        var sidebarHoverBrush = SidebarHoverSurface(theme);
        ReportPalette =
        [
            AccentBrush,
            Solid("#14B8A6"),
            InkBrush,
            Solid("#8B5CF6"),
            Solid("#F59E0B"),
            Solid("#10B981"),
            Solid("#EF4444")
        ];
        ReportSoftPalette =
        [
            AccentSoftBrush,
            Solid("#ECFDF5"),
            GraySoftBrush,
            Solid("#F3E8FF"),
            Solid("#FEF3C7"),
            Solid("#DCFCE7"),
            RedSoftBrush
        ];

        SetResourceBrush("Accent", theme.Accent);
        SetResourceBrush("AccentDark", theme.AccentDark);
        SetResourceBrush("AccentText", theme.AccentDark);
        SetResourceBrush("AccentSoft", theme.AccentSoft);
        SetResourceBrush("BlueSoft", theme.BlueSoft);
        SetResourceBrush("WarmSoft", theme.WarmSoft);
        SetResourceBrush("Ink", theme.Ink);
        SetResourceBrush("Muted", theme.Muted);
        SetResourceBrush("Line", theme.Line);
        SetResourceBrush("Panel", theme.Panel);
        Resources["SidebarBorder"] = sidebarBorderBrush;
        Resources["SidebarActive"] = SidebarActiveBackgroundBrush;
        Resources["SidebarHover"] = sidebarHoverBrush;

        ApplyThemeTypography();
        Background = Solid(theme.AppBackground);

        TopBarBorder.Background = Solid(theme.Panel);
        TopBarBorder.BorderBrush = LineBrush;
        TopBrandPanel.Background = SidebarHeaderGradient(theme);
        TopBrandPanel.BorderBrush = LineBrush;
        TopLogoText.Foreground = Solid(theme.AccentDark);
        AppTitleText.Foreground = InkBrush;
        AppSubtitleText.Foreground = MutedBrush;
        var sidebarProfileSurface = SidebarProfileSurface(theme);
        SidebarProfileButton.Background = sidebarProfileSurface;
        SidebarProfileButton.BorderBrush = sidebarBorderBrush;
        SidebarProfileButton.BorderThickness = new Thickness(1);
        SidebarLogoText.Foreground = Solid(theme.AccentDark);
        SidebarProfileLogoHost.Background = Brushes.Transparent;
        SidebarDarkThemeLogo.Visibility = usesDarkSidebar ? Visibility.Visible : Visibility.Collapsed;
        SidebarLightThemeAvatar.Visibility = usesDarkSidebar ? Visibility.Collapsed : Visibility.Visible;
        SidebarCollapsedDarkThemeLogo.Visibility = usesDarkSidebar ? Visibility.Visible : Visibility.Collapsed;
        SidebarCollapsedLightThemeLogo.Visibility = usesDarkSidebar ? Visibility.Collapsed : Visibility.Visible;
        SidebarLightThemeAvatar.Background = AccentSoftBrush;
        SidebarLightThemeAvatar.BorderBrush = LineBrush;
        SidebarUserNameText.Foreground = usesDarkSidebar ? Brushes.White : InkBrush;
        SidebarUserRoleText.Foreground = usesDarkSidebar ? Solid("#A9A39D") : MutedBrush;
        var sidebarToggleBrush = Solid(theme.AccentDark);
        SidebarProfileChevron.Foreground = sidebarToggleBrush;
        SidebarCollapsedToggleIcon.Foreground = sidebarToggleBrush;
        SidebarCollapsedToggleButton.Background = sidebarProfileSurface;
        SidebarCollapsedToggleButton.BorderBrush = sidebarBorderBrush;
        SidebarCollapsedToggleButton.BorderThickness = new Thickness(1);
        AppShellBodyGrid.Background = Solid(theme.AppBackground);
        SidebarExpandedPanel.Background = SidebarGradient(theme);
        SidebarExpandedPanel.BorderBrush = sidebarBorderBrush;
        SidebarCollapsedPanel.Background = SidebarGradient(theme);
        SidebarCollapsedPanel.BorderBrush = sidebarBorderBrush;
        OnboardingOverlay.Background = Solid(theme.AppBackground);
        OnboardingSidebarPanel.Background = Solid(theme.WarmSoft);
        OnboardingSidebarPanel.BorderBrush = LineBrush;
        OnboardingBrandIcon.Foreground = AccentBrush;
        OnboardingBrandIconShell.BorderBrush = LineBrush;
        OnboardingQuickTipCard.Background = Solid(theme.WarmSoft);
        OnboardingQuickTipCard.BorderBrush = LineBrush;
        OnboardingQuickTipIconShell.Background = AccentSoftBrush;
        OnboardingQuickTipIcon.Foreground = AccentBrush;

        if (refreshVisibleData)
        {
            RefreshAll();
            ApplyBusinessLabels();
        }

        UpdateOnboardingStepDots();
        UpdateWizardChoiceStates();
    }

    private void ApplyThemeTypography()
    {
        var bodyFont = new FontFamily("Segoe UI Variable Text, Segoe UI");

        FontFamily = bodyFont;

        foreach (var title in new[]
                 {
                     AppTitleText,
                     SidebarUserNameText,
                     HomeGreetingText,
                     HomeSummaryTitleText,
                     HomeScheduleTitleText,
                     HomeNextTimesTitleText,
                     HomeAttentionTitleText,
                     EstablishmentTitleText,
                     FinanceTitleText,
                     ReportsTitleText,
                     ReportsChartTitleText,
                     MarketingTitleText,
                     AgendaTitleText,
                     SettingsTitleText,
                     EditorTitleText
                 })
        {
            title.FontFamily = bodyFont;
        }
    }

    private static void ApplyMaterialPalette(VisualTheme visualTheme)
    {
        var paletteHelper = new PaletteHelper();
        var materialTheme = paletteHelper.GetTheme();
        materialTheme.SetPrimaryColor((Color)ColorConverter.ConvertFromString(visualTheme.Accent));
        materialTheme.SetSecondaryColor((Color)ColorConverter.ConvertFromString(visualTheme.AccentDark));
        paletteHelper.SetTheme(materialTheme);
    }

    private static void ApplySystemSelectionPalette()
    {
        Application.Current.Resources[SystemColors.HighlightBrushKey] = AccentSoftBrush;
        Application.Current.Resources[SystemColors.HighlightTextBrushKey] = InkBrush;
        Application.Current.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = AccentSoftBrush;
        Application.Current.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = InkBrush;
    }

    private void SetResourceBrush(string key, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        if (Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = color;
        }
        else
        {
            Resources[key] = new SolidColorBrush(color);
        }
    }

    private void ConfigureInputs()
    {
        BuildTimeOptions();
        var availableSegments = GetAvailableSegments();

        MetricsItemsControl.ItemsSource = _metrics;
        HomeMetricsItemsControl.ItemsSource = _homeMetrics;
        HomeAgendaItemsControl.ItemsSource = _homeAgendaRows;
        HomeTopServicesItemsControl.ItemsSource = _homeTopServices;
        HomeRecentCustomersItemsControl.ItemsSource = _homeRecentCustomers;
        HomeAlertsItemsControl.ItemsSource = _homeAlerts;
        HomeFinanceBarsItemsControl.ItemsSource = _homeFinanceBars;
        EstablishmentMetricsItemsControl.ItemsSource = _establishmentMetrics;
        EstablishmentSectionsItemsControl.ItemsSource = _establishmentSections;
        EstablishmentClientsItemsControl.ItemsSource = _establishmentClients;
        EstablishmentProfessionalsItemsControl.ItemsSource = _establishmentProfessionals;
        EstablishmentServicesItemsControl.ItemsSource = _establishmentServices;
        EstablishmentProductsItemsControl.ItemsSource = _establishmentProducts;
        EstablishmentSalesItemsControl.ItemsSource = _establishmentSales;
        FinanceEntriesItemsControl.ItemsSource = _financeEntries;
        FinancePendingItemsControl.ItemsSource = _financePendingPayments;
        FinanceExpensesItemsControl.ItemsSource = _financeExpenses;
        FinanceChartItemsControl.ItemsSource = _financeChartRows;
        ReportsMetricsItemsControl.ItemsSource = _reportsMetrics;
        ReportsColumnChartItemsControl.ItemsSource = _reportsColumnChartRows;
        ReportsLineLegendItemsControl.ItemsSource = _reportsLineChartRows;
        ReportsDonutLegendItemsControl.ItemsSource = _reportsDonutChartRows;
        ReportsRankingChartItemsControl.ItemsSource = _reportsRankingChartRows;
        ReportsInsightsItemsControl.ItemsSource = _reportsInsights;
        ReportsServicesItemsControl.ItemsSource = _reportsServices;
        ReportsProfessionalsItemsControl.ItemsSource = _reportsProfessionals;
        MarketingContactsItemsControl.ItemsSource = _marketingContacts;
        MarketingMessagesItemsControl.ItemsSource = _marketingMessages;
        MarketingCampaignsItemsControl.ItemsSource = _marketingCampaigns;
        WhatsAppConversationCardsItemsControl.ItemsSource = _whatsAppConversations;
        WhatsAppFloatingMessagesItemsControl.ItemsSource = _whatsAppMessages;
        ConfigureReportChartOptions();
        ProfessionalListBox.ItemsSource = _professionalRows;
        RecentCustomerListBox.ItemsSource = _recentCustomers;
        DayAgendaList.ItemsSource = _dayRows;
        WeekAgendaList.ItemsSource = _weekRows;
        WeekSummaryItemsControl.ItemsSource = _weekSummaryRows;
        UpdateAgendaModeButtons();

        _selectedSegmentFilter = AllSegments;
        UpdateSegmentFilterButton();

        AppointmentSegmentCombo.ItemsSource = availableSegments;
        AppointmentSegmentCombo.SelectedItem = availableSegments[0];

        ServiceCombo.ItemsSource = _filteredServices;
        ProfessionalCombo.ItemsSource = _filteredProfessionals;
        ResourceCombo.ItemsSource = _resourceOptions;

        TimeCombo.ItemsSource = _timeOptions;
        DurationCombo.ItemsSource = _durationOptions;
        DurationCombo.SelectedItem = 30;

        UpdateDateFilterButton();
        AppointmentDatePicker.SelectedDate = _selectedDate;
    }

    private void ConfigureReportChartOptions()
    {
        _configuringReportChart = true;

        var selected = ReportsChartTypeCombo.SelectedItem as string ?? ReportChartAppointments;
        _reportChartOptions.Clear();
        _reportChartOptions.Add(ReportChartAppointments);
        _reportChartOptions.Add(ReportChartStatus);

        ReportsChartTypeCombo.ItemsSource = _reportChartOptions;
        ReportsChartTypeCombo.SelectedItem = _reportChartOptions.Contains(selected)
            ? selected
            : ReportChartAppointments;

        _configuringReportChart = false;
        UpdateReportChartModeButtons();
    }

    private List<string> GetAvailableSegments()
    {
        if (!string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment))
        {
            return [_data.Settings.BusinessSegment];
        }

        return [.. _segments];
    }

    private void ConfigureOnboardingInputs()
    {
        _selectedOnboardingTemplate = ResolveBusinessTemplate(_data.Settings.BusinessSegment);
        _selectedProfessionalCount = _data.Settings.ProfessionalCountRange;
        _selectedObjective = _data.Settings.MainObjective;
        _selectedOnboardingThemeId = _data.Settings.ThemeId;
        _showingThemeSelection = false;

        InitialFullNameTextBox.Text = _data.Settings.AccountFullName;
        InitialPhoneTextBox.Text = string.IsNullOrWhiteSpace(_data.Settings.AccountPhone)
            ? _data.Settings.BusinessPhone
            : _data.Settings.AccountPhone;
        InitialEmailTextBox.Text = _data.Settings.AccountEmail;
        InitialBusinessNameTextBox.Text = IsDefaultBusinessName(_data.Settings.BusinessName)
            ? ""
            : _data.Settings.BusinessName;
        _formattingOnboardingCep = true;
        OnboardingCepTextBox.Text = FormatCepInput(_data.Settings.PostalCode);
        _formattingOnboardingCep = false;
        OnboardingNeighborhoodTextBox.Text = _data.Settings.Neighborhood;
        OnboardingStreetTextBox.Text = _data.Settings.Street;
        OnboardingAddressNumberTextBox.Text = _data.Settings.AddressNumber;
        OnboardingAddressComplementTextBox.Text = _data.Settings.AddressComplement;
        ShowOnboardingStep(0);
    }

    private bool NeedsOnboarding() =>
        !_data.Settings.OnboardingCompleted ||
        string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment);

    private static bool IsDefaultBusinessName(string? businessName) =>
        string.IsNullOrWhiteSpace(businessName) ||
        businessName.Equals("Agenda Livre", StringComparison.OrdinalIgnoreCase) ||
        businessName.Equals("Balcão Livre", StringComparison.OrdinalIgnoreCase) ||
        businessName.Equals("Balcão Livre", StringComparison.OrdinalIgnoreCase);

    private string BusinessDisplayName() =>
        IsDefaultBusinessName(_data.Settings.BusinessName)
            ? "Balcão Livre"
            : _data.Settings.BusinessName;

    private void ShowOnboarding()
    {
        WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
        _whatsAppReturnFocusElement = null;
        TopBarBorder.IsEnabled = false;
        AppShellBodyGrid.IsEnabled = false;
        OnboardingOverlay.Visibility = Visibility.Visible;
        FontFamily = new FontFamily("Segoe UI");
        RefreshWhatsAppLauncherVisibility();
        ShowOnboardingStep(0);
        ShowStatus("Informe os dados iniciais para criar sua agenda.");
    }

    private void ShowSegmentSelectionStep()
    {
        _showingThemeSelection = false;
        ShowOnboardingStep(1);
    }

    private void ShowThemeSelectionStep()
    {
        _showingThemeSelection = true;
        var themeChoices = ThemeChoicesForTemplate(_selectedOnboardingTemplate);
        if (!themeChoices.Any(choice => NormalizeTemplateLookup(choice.Id) == NormalizeTemplateLookup(_selectedOnboardingThemeId)))
        {
            _selectedOnboardingThemeId = "";
        }

        BuildThemeChoiceCards();
        ShowOnboardingStep(1);
    }

    private static bool TemplateSupportsThemeChoices(OnboardingTemplate? template) =>
        template is not null &&
        (NormalizeTemplateLookup(template.Title) == "SALAODEBELEZA" ||
         NormalizeTemplateLookup(template.Segment) == "SALAODEBELEZA" ||
         NormalizeTemplateLookup(template.Title) == "CENTRODEESTETICA" ||
         NormalizeTemplateLookup(template.Segment) == "CENTRODEESTETICA" ||
         NormalizeTemplateLookup(template.Title) == "CLINICAMEDICA" ||
         NormalizeTemplateLookup(template.Segment) == "CLINICAMEDICA" ||
         NormalizeTemplateLookup(template.Title) == "PETSHOP" ||
         NormalizeTemplateLookup(template.Segment) == "PETSHOP" ||
         NormalizeTemplateLookup(template.Title) == "OFICINA" ||
         NormalizeTemplateLookup(template.Segment) == "OFICINA" ||
         NormalizeTemplateLookup(template.Segment) == "MECANICA" ||
         NormalizeTemplateLookup(template.Title) == "PODOLOGIA" ||
         NormalizeTemplateLookup(template.Segment) == "PODOLOGIA" ||
         NormalizeTemplateLookup(template.Title) == "SPA" ||
         NormalizeTemplateLookup(template.Segment) == "SPA" ||
         NormalizeTemplateLookup(template.Title) == "BARBEARIA" ||
         NormalizeTemplateLookup(template.Segment) == "BARBEARIA" ||
         NormalizeTemplateLookup(template.Segment) == "CABELOEBARBEARIA");

    private static IReadOnlyList<OnboardingThemeChoice> ThemeChoicesForTemplate(OnboardingTemplate? template)
    {
        if (template is null)
        {
            return [];
        }

        var title = NormalizeTemplateLookup(template.Title);
        var segment = NormalizeTemplateLookup(template.Segment);

        if (title == "BARBEARIA" || segment == "BARBEARIA" || segment == "CABELOEBARBEARIA")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemeBarberMidnight,
                    "Preto clássico",
                    "Forte e direto.",
                    "/Assets/Themes/barber-midnight.png",
                    "#F3F5F7",
                    "#DDE3E8"),
                new(
                    ThemeBarberEmerald,
                    "Verde barbearia",
                    "Elegante e profissional.",
                    "/Assets/Themes/barber-emerald.png",
                    "#EEF8F5",
                    "#CDE6E1"),
                new(
                    ThemeBarberNavy,
                    "Azul naval",
                    "Limpo e sofisticado.",
                    "/Assets/Themes/barber-navy.png",
                    "#EEF4FC",
                    "#D3E1F1")
            ];
        }

        if (title == "CLINICAMEDICA" || segment == "CLINICAMEDICA")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemeMedicalTeal,
                    "Teal clínico",
                    "Cuidado limpo e moderno.",
                    "",
                    "#EEF9F9",
                    "#CFE7E8"),
                new(
                    ThemeMedicalGreen,
                    "Verde acolhe",
                    "Saúde leve e acolhedora.",
                    "",
                    "#EEF8F3",
                    "#D5E7DF"),
                new(
                    ThemeMedicalBlue,
                    "Azul saúde",
                    "Clínico e tecnológico.",
                    "",
                    "#EEF6FF",
                    "#D3E2F8")
            ];
        }

        if (title == "PETSHOP" || segment == "PETSHOP")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemePetCoral,
                    "Coral pet",
                    "Quente e divertido.",
                    "",
                    "#FFF0EA",
                    "#F0D2CB"),
                new(
                    ThemePetLilac,
                    "Lilás pet",
                    "Fofo e delicado.",
                    "",
                    "#F6EFFF",
                    "#E3D6F5"),
                new(
                    ThemePetTeal,
                    "Verde pet",
                    "Vivo e organizado.",
                    "",
                    "#ECFAF8",
                    "#CFE7E4")
            ];
        }

        if (title == "OFICINA" || segment == "OFICINA" || segment == "MECANICA")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemeWorkshopGold,
                    "Mecânica ouro",
                    "Forte e premium.",
                    "",
                    "#FFF7E8",
                    "#E7D8B8"),
                new(
                    ThemeWorkshopOlive,
                    "Verde automotivo",
                    "Robusto e organizado.",
                    "",
                    "#F2F7E6",
                    "#D9E2BD"),
                new(
                    ThemeWorkshopGraphite,
                    "Grafite oficina",
                    "Direto e técnico.",
                    "",
                    "#F1F3F4",
                    "#D9DFDF")
            ];
        }

        if (title == "CENTRODEESTETICA" || segment == "CENTRODEESTETICA")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemeAestheticLavender,
                    "Lavanda wellness",
                    "Suave e relaxante.",
                    "/Assets/Themes/aesthetic-lavender.png",
                    "#F7F1FF",
                    "#E4DAF2"),
                new(
                    ThemeAestheticSage,
                    "Sálvia natural",
                    "Natural e acolhedor.",
                    "/Assets/Themes/aesthetic-sage.png",
                    "#F3F5EC",
                    "#DFE6D5"),
                new(
                    ThemeAestheticCoral,
                    "Coral glow",
                    "Leve e sofisticado.",
                    "/Assets/Themes/aesthetic-coral.png",
                    "#FFF0ED",
                    "#F1D5CF")
            ];
        }

        if (title == "PODOLOGIA" || segment == "PODOLOGIA")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemePodologyTerracotta,
                    "Essencial terracota",
                    "Acolhedor e profissional.",
                    "/Assets/Themes/podology-terracotta.png",
                    "#FFF0EA",
                    "#EED6CD"),
                new(
                    ThemePodologyMint,
                    "Bem-estar verde",
                    "Leve e natural.",
                    "/Assets/Themes/podology-mint.png",
                    "#EEF9F4",
                    "#D4E7DF"),
                new(
                    ThemePodologyBlue,
                    "Azul podologia",
                    "Limpo e clínico.",
                    "/Assets/Themes/podology-blue.png",
                    "#EEF6FF",
                    "#D6E5F7")
            ];
        }

        if (title == "SPA" || segment == "SPA")
        {
            return
            [
                new(
                    ThemeDefaultWarm,
                    "Modo padrão",
                    "Laranjinha original.",
                    "",
                    "#FFF4EE",
                    "#ED6823"),
                new(
                    ThemeSpaAqua,
                    "Água calma",
                    "Azul leve e relaxante.",
                    "/Assets/Themes/spa-aqua.png",
                    "#F1FAFF",
                    "#D7E7F4"),
                new(
                    ThemeSpaSand,
                    "Areia serena",
                    "Natural e acolhedor.",
                    "/Assets/Themes/spa-sand.png",
                    "#FFF5EC",
                    "#EAD8CA"),
                new(
                    ThemeSpaForest,
                    "Floresta zen",
                    "Verde orgânico e calmo.",
                    "/Assets/Themes/spa-forest.png",
                    "#F4F7EC",
                    "#DDE4D0")
            ];
        }

        return
        [
            new(
                ThemeDefaultWarm,
                "Modo padrão",
                "Laranjinha original.",
                "",
                "#FFF4EE",
                "#ED6823"),
            new(
                ThemeSalonClassicGold,
                "Clássico dourado",
                "Claro e elegante.",
                "/Assets/Themes/salon-classic-gold.png",
                "#FFF7F0",
                "#EAD8CB"),
            new(
                ThemeSalonLilacGlow,
                "Lilás glow",
                "Delicado e moderno.",
                "/Assets/Themes/salon-lilac-glow.png",
                "#F7F0FF",
                "#E7DAFF"),
            new(
                ThemeSalonRoseLuxe,
                "Rose luxe",
                "Rosé sofisticado.",
                "/Assets/Themes/salon-rose-luxe.png",
                "#FFF0F5",
                "#F6CDD9")
        ];
    }

    private void BuildThemeChoiceCards()
    {
        ThemeChoiceCardsPanel.Children.Clear();

        var themeChoices = ThemeChoicesForTemplate(_selectedOnboardingTemplate);
        ThemeChoiceCardsPanel.Columns = Math.Min(4, Math.Max(1, themeChoices.Count));
        foreach (var choice in themeChoices)
        {
            var button = new Button
            {
                Tag = choice.Id,
                Style = (Style)ThemeChoiceCardsPanel.FindResource("WizardThemeButton"),
                Margin = new Thickness(8, 0, 8, 0)
            };
            AutomationProperties.SetName(button, $"Tema {choice.Title}. {choice.Description}");
            button.Click += ThemeChoiceButton_Click;

            var content = new StackPanel();
            var preview = CreateThemeChoicePreview(choice);

            var title = new TextBlock
            {
                Text = choice.Title,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2, 8, 2, 0)
            };
            BindingOperations.SetBinding(
                title,
                TextBlock.ForegroundProperty,
                new Binding("Foreground")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
                });

            var description = new TextBlock
            {
                Text = choice.Description,
                Foreground = MutedBrush,
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 3, 2, 0)
            };

            content.Children.Add(preview);
            content.Children.Add(title);
            content.Children.Add(description);
            button.Content = content;
            ThemeChoiceCardsPanel.Children.Add(button);
        }
    }

    private static Border CreateThemeChoicePreview(OnboardingThemeChoice choice)
    {
        return CreateThemeChoiceMiniPreview(choice, ThemeById(choice.Id));
    }

    private static Border CreateThemeChoiceMiniPreview(OnboardingThemeChoice choice, VisualTheme theme)
    {
        var radius = 13.0;
        var isDefaultTheme = string.IsNullOrWhiteSpace(choice.Id);
        var sidebarColor = isDefaultTheme ? theme.Accent : theme.SidebarBackground;
        var activeColor = isDefaultTheme ? theme.AccentSoft : theme.SidebarActive;
        var panelColor = isDefaultTheme ? "#FFFFFF" : theme.Panel;
        var contentSoftColor = isDefaultTheme ? theme.AccentSoft : theme.BlueSoft;
        var contentStrongColor = isDefaultTheme ? theme.BlueSoft : theme.AccentSoft;

        var shell = new Grid
        {
            Background = Solid(theme.AppBackground),
            ClipToBounds = true
        };

        shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = new Border
        {
            Background = Solid(sidebarColor),
            CornerRadius = new CornerRadius(radius, 0, 0, radius)
        };
        shell.Children.Add(sidebar);

        var sidebarMarks = new StackPanel
        {
            Margin = new Thickness(9, 10, 7, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        sidebarMarks.Children.Add(new Border
        {
            Height = 7,
            Background = Solid(activeColor),
            CornerRadius = new CornerRadius(4)
        });
        sidebarMarks.Children.Add(new Border
        {
            Height = 5,
            Width = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 9, 0, 0),
            Background = Solid(isDefaultTheme ? "#FFFFFF" : theme.SidebarText),
            Opacity = 0.65,
            CornerRadius = new CornerRadius(3)
        });
        shell.Children.Add(sidebarMarks);

        var content = new StackPanel
        {
            Margin = new Thickness(9, 9, 9, 7)
        };
        Grid.SetColumn(content, 1);

        content.Children.Add(new Border
        {
            Height = 9,
            Width = 86,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Solid(panelColor),
            BorderBrush = Solid(theme.Line),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5)
        });
        content.Children.Add(new Border
        {
            Height = 13,
            Margin = new Thickness(0, 9, 0, 5),
            Background = Solid(contentSoftColor),
            CornerRadius = new CornerRadius(6)
        });
        content.Children.Add(new Border
        {
            Height = 13,
            Width = 72,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Solid(contentStrongColor),
            CornerRadius = new CornerRadius(6)
        });

        shell.Children.Add(content);

        return new Border
        {
            Height = 66,
            Background = Solid(choice.PreviewBackground),
            BorderBrush = Solid(choice.PreviewBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            ClipToBounds = true,
            Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 2,
                Opacity = 0.08,
                Color = Colors.Black
            },
            Child = shell
        };
    }

    private void ContinueInitialDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCaptureInitialData())
        {
            return;
        }

        ShowSegmentSelectionStep();
        SegmentSalonButton.Focus();
    }

    private void SkipInitialDataButton_Click(object sender, RoutedEventArgs e)
    {
        SkipCurrentOnboardingStep();
    }

    private void SkipOnboardingStepButton_Click(object sender, RoutedEventArgs e)
    {
        SkipCurrentOnboardingStep();
    }

    private void SkipCurrentOnboardingStep()
    {
        switch (_onboardingStep)
        {
            case 0:
                EnsureInitialDataDefaults();
                ShowSegmentSelectionStep();
                SegmentSalonButton.Focus();
                ShowStatus("Dados iniciais pulados. Confira ou escolha o segmento.");
                break;
            case 1:
                if (_showingThemeSelection)
                {
                    SkipThemeChoiceButton_Click(this, new RoutedEventArgs());
                    break;
                }

                EnsureSegmentDefaults();
                ShowOnboardingStep(2);
                ShowStatus("Segmento pulado. Usando Salão de Beleza como padrão.");
                break;
            case 2:
                EnsureProfessionalCountDefault();
                ShowOnboardingStep(3);
                ShowStatus("Quantidade de profissionais pulada. Usando 1 profissional.");
                break;
            case 3:
                EnsureObjectiveDefault();
                ShowOnboardingStep(4);
                ShowStatus("Objetivo pulado. Usando Organizar agenda.");
                break;
            case 4:
                ShowOnboardingStep(5);
                ShowStatus("Endereço pulado. Confira os dados antes de concluir.");
                break;
            case 5:
                EnsureInitialDataDefaults();
                EnsureSegmentDefaults();
                EnsureProfessionalCountDefault();
                EnsureObjectiveDefault();
                FinishCreateAccountButton_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private void EnsureInitialDataDefaults()
    {
        var template = CreateBusinessTemplate("Salão de Beleza");
        var ownerName = FirstFilled(ToNameCase(InitialFullNameTextBox.Text), _data.Settings.AccountFullName, "Nina");
        var phone = FirstFilled(InitialPhoneTextBox.Text.Trim(), _data.Settings.AccountPhone, _data.Settings.BusinessPhone, "(33) 99800-7978");
        var typedEmail = InitialEmailTextBox.Text.Trim();
        var email = LooksLikeEmail(typedEmail)
            ? typedEmail
            : FirstFilled(_data.Settings.AccountEmail, "teste@agendalivre.local");
        var businessName = FirstFilled(ToNameCase(InitialBusinessNameTextBox.Text), _data.Settings.BusinessName, template.DefaultBusinessName);
        if (IsDefaultBusinessName(businessName))
        {
            businessName = template.DefaultBusinessName;
        }

        InitialFullNameTextBox.Text = ownerName;
        InitialPhoneTextBox.Text = phone;
        InitialEmailTextBox.Text = email;
        InitialBusinessNameTextBox.Text = businessName;
        _data.Settings.AccountFullName = ownerName;
        _data.Settings.AccountPhone = phone;
        _data.Settings.AccountEmail = email;
        _data.Settings.BusinessName = businessName;
        _data.Settings.BusinessPhone = phone;
    }

    private void EnsureSegmentDefaults()
    {
        _selectedOnboardingTemplate ??= CreateBusinessTemplate("Salão de Beleza");
        if (string.IsNullOrWhiteSpace(_selectedOnboardingThemeId))
        {
            _selectedOnboardingThemeId = ThemeDefaultWarm;
            _data.Settings.ThemeId = ThemeDefaultWarm;
        }

        UpdateWizardChoiceStates();
    }

    private void EnsureProfessionalCountDefault()
    {
        if (string.IsNullOrWhiteSpace(_selectedProfessionalCount))
        {
            _selectedProfessionalCount = "1 profissional";
        }

        UpdateWizardChoiceStates();
    }

    private void EnsureObjectiveDefault()
    {
        if (string.IsNullOrWhiteSpace(_selectedObjective))
        {
            _selectedObjective = "Organizar agenda";
        }

        UpdateWizardChoiceStates();
    }

    private void InitialNameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Text = ToNameCase(textBox.Text);
        }
    }

    private void InitialPhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox &&
            TryFormatBusinessPhone(textBox.Text, out var formattedPhone, out _))
        {
            textBox.Text = formattedPhone;
        }
    }

    private bool TryCaptureInitialData()
    {
        var fullName = ToNameCase(InitialFullNameTextBox.Text);
        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowStatus("Informe o nome completo antes de continuar.");
            InitialFullNameTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(InitialPhoneTextBox.Text))
        {
            ShowStatus("Informe o celular antes de continuar.");
            InitialPhoneTextBox.Focus();
            return false;
        }

        if (!TryFormatBusinessPhone(InitialPhoneTextBox.Text, out var phone, out var phoneError))
        {
            ShowStatus(phoneError);
            InitialPhoneTextBox.Focus();
            return false;
        }

        var email = InitialEmailTextBox.Text.Trim();
        if (!LooksLikeEmail(email))
        {
            ShowStatus("Informe um e-mail válido antes de continuar.");
            InitialEmailTextBox.Focus();
            return false;
        }

        var businessName = ToNameCase(InitialBusinessNameTextBox.Text);
        if (string.IsNullOrWhiteSpace(businessName))
        {
            ShowStatus("Informe o nome do negócio antes de continuar.");
            InitialBusinessNameTextBox.Focus();
            return false;
        }

        InitialFullNameTextBox.Text = fullName;
        InitialPhoneTextBox.Text = phone;
        InitialBusinessNameTextBox.Text = businessName;
        _data.Settings.AccountFullName = fullName;
        _data.Settings.AccountPhone = phone;
        _data.Settings.AccountEmail = email;
        _data.Settings.BusinessName = businessName;
        _data.Settings.BusinessPhone = phone;
        return true;
    }

    private void BusinessTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string businessType)
        {
            return;
        }

        _selectedOnboardingTemplate = CreateBusinessTemplate(businessType);
        UpdateWizardChoiceStates();
        if (TemplateSupportsThemeChoices(_selectedOnboardingTemplate))
        {
            ShowThemeSelectionStep();
            ShowStatus($"Selecionado: {_selectedOnboardingTemplate.Title}. Escolha um tema ou pule esta etapa.");
            return;
        }

        _selectedOnboardingThemeId = ThemeDefaultWarm;
        _data.Settings.ThemeId = ThemeDefaultWarm;
        ApplyVisualTheme(ThemeById(ThemeDefaultWarm), refreshVisibleData: true);
        ShowStatus($"Selecionado: {_selectedOnboardingTemplate.Title}. Clique em Continuar para confirmar.");
    }

    private void ThemeChoiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string themeId)
        {
            return;
        }

        var theme = ThemeById(themeId);
        _selectedOnboardingThemeId = theme.Id;
        _data.Settings.ThemeId = theme.Id;
        ApplyVisualTheme(theme, refreshVisibleData: true);
        UpdateWizardChoiceStates();
        ShowStatus($"Tema {theme.Name} aplicado. Clique em Continuar para seguir.");
    }

    private void SkipThemeChoiceButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedOnboardingThemeId = ThemeDefaultWarm;
        _data.Settings.ThemeId = ThemeDefaultWarm;
        ApplyVisualTheme(ThemeById(ThemeDefaultWarm), refreshVisibleData: true);
        ShowOnboardingStep(2);
        ShowStatus("Personalização pulada. Você poderá trocar o tema depois.");
    }

    private void ContinueThemeChoiceButton_Click(object sender, RoutedEventArgs e)
    {
        ShowOnboardingStep(2);
    }

    private void ProfessionalCountButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string professionalCount)
        {
            return;
        }

        _selectedProfessionalCount = professionalCount;
        UpdateWizardChoiceStates();
        ShowStatus($"Selecionado: {professionalCount}. Clique em Continuar para confirmar.");
    }

    private void ObjectiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string objective)
        {
            return;
        }

        _selectedObjective = objective;
        UpdateWizardChoiceStates();
        ShowStatus($"Selecionado: {objective}. Clique em Continuar para confirmar.");
    }

    private void ContinueBusinessTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOnboardingTemplate is null)
        {
            ShowStatus("Escolha o segmento do negócio antes de continuar.");
            return;
        }

        if (TemplateSupportsThemeChoices(_selectedOnboardingTemplate))
        {
            ShowThemeSelectionStep();
            return;
        }

        ShowOnboardingStep(2);
    }

    private void ContinueProfessionalCountButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedProfessionalCount))
        {
            ShowStatus("Escolha a quantidade de profissionais antes de continuar.");
            return;
        }

        ShowOnboardingStep(3);
    }

    private void ContinueObjectiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedObjective))
        {
            ShowStatus("Escolha o objetivo principal antes de continuar.");
            return;
        }

        ShowOnboardingStep(4);
    }

    private void ContinueAddressButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OnboardingCepTextBox.Text) &&
            string.IsNullOrWhiteSpace(OnboardingStreetTextBox.Text))
        {
            ShowStatus("Informe pelo menos o CEP ou o logradouro do negócio.");
            OnboardingCepTextBox.Focus();
            return;
        }

        ShowOnboardingStep(5);
    }

    private void FinishCreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var template = _selectedOnboardingTemplate ?? CreateBusinessTemplate("Salão de Beleza");
        var professionalCount = string.IsNullOrWhiteSpace(_selectedProfessionalCount)
            ? "1 profissional"
            : _selectedProfessionalCount;
        var objective = string.IsNullOrWhiteSpace(_selectedObjective)
            ? "Organizar agenda"
            : _selectedObjective;
        var businessName = string.IsNullOrWhiteSpace(_data.Settings.BusinessName) ||
                           IsDefaultBusinessName(_data.Settings.BusinessName)
            ? template.DefaultBusinessName
            : _data.Settings.BusinessName.Trim();
        var businessPhone = _data.Settings.AccountPhone;

        ApplyOnboardingTemplate(
            template,
            businessName,
            "",
            businessPhone,
            BuildOnboardingAddress(),
            template.StartHour,
            template.EndHour,
            ProfessionalTemplateLimit(professionalCount));

        _data.Settings.AccountFullName = InitialFullNameTextBox.Text.Trim();
        _data.Settings.AccountPhone = businessPhone;
        _data.Settings.AccountEmail = InitialEmailTextBox.Text.Trim();
        _data.Settings.ThemeId = TemplateSupportsThemeChoices(template) ? _selectedOnboardingThemeId : ThemeDefaultWarm;
        _data.Settings.ProfessionalCountRange = professionalCount;
        _data.Settings.MainObjective = objective;
        _data.Settings.PostalCode = OnboardingCepTextBox.Text.Trim();
        _data.Settings.Neighborhood = OnboardingNeighborhoodTextBox.Text.Trim();
        _data.Settings.Street = OnboardingStreetTextBox.Text.Trim();
        _data.Settings.AddressNumber = OnboardingAddressNumberTextBox.Text.Trim();
        _data.Settings.AddressComplement = OnboardingAddressComplementTextBox.Text.Trim();
        _data.Settings.AccountPasswordHash = "";
        _data.Settings.AccountCreatedAt = DateTime.Now;
        _store.Save(_data);

        OnboardingOverlay.Visibility = Visibility.Collapsed;
        TopBarBorder.IsEnabled = true;
        AppShellBodyGrid.IsEnabled = true;
        ApplyVisualTheme(ThemeById(_data.Settings.ThemeId), refreshVisibleData: false);
        RefreshWhatsAppLauncherVisibility();
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
        ApplyBusinessLabels();
        ShowMainPage(MainPage.Home);
        Dispatcher.BeginInvoke(HomeSidebarButton.Focus, DispatcherPriority.Input);
        ShowStatus($"Conta criada para {template.Title}. A agenda está pronta para uso.");
    }

    private void OnboardingBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_onboardingStep == 1 && _showingThemeSelection)
        {
            ShowSegmentSelectionStep();
            return;
        }

        ShowOnboardingStep(Math.Max(0, _onboardingStep - 1));
    }

    private void ShowOnboardingStep(int step)
    {
        _onboardingStep = Math.Clamp(step, 0, 5);
        if (_onboardingStep != 1)
        {
            _showingThemeSelection = false;
        }

        InitialDataStepPanel.Visibility = _onboardingStep == 0 ? Visibility.Visible : Visibility.Collapsed;
        BusinessSegmentStepPanel.Visibility = _onboardingStep == 1 && !_showingThemeSelection ? Visibility.Visible : Visibility.Collapsed;
        ThemeSelectionStepPanel.Visibility = _onboardingStep == 1 && _showingThemeSelection ? Visibility.Visible : Visibility.Collapsed;
        ProfessionalCountStepPanel.Visibility = _onboardingStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        ObjectiveStepPanel.Visibility = _onboardingStep == 3 ? Visibility.Visible : Visibility.Collapsed;
        AddressStepPanel.Visibility = _onboardingStep == 4 ? Visibility.Visible : Visibility.Collapsed;
        ReviewStepPanel.Visibility = _onboardingStep == 5 ? Visibility.Visible : Visibility.Collapsed;
        OnboardingHeaderRow.Height = _onboardingStep == 0 ? new GridLength(0) : new GridLength(64);
        OnboardingTopBar.Visibility = _onboardingStep == 0 ? Visibility.Collapsed : Visibility.Visible;
        OnboardingBackButton.Visibility = _onboardingStep == 0 ? Visibility.Hidden : Visibility.Visible;
        OnboardingSkipStepButton.Visibility = _onboardingStep is > 0 and < 5 && !_showingThemeSelection ? Visibility.Visible : Visibility.Collapsed;
        OnboardingProgressText.Text = $"{_onboardingStep + 1}/6";
        OnboardingSidebarProgressText.Text = $"Etapa {_onboardingStep + 1} de 6";
        OnboardingSidebarTitleText.Text = _onboardingStep == 1 && _showingThemeSelection
            ? "Tema do sistema"
            : OnboardingStepTitles[_onboardingStep];
        OnboardingSidebarCaptionText.Text = _onboardingStep == 1 && _showingThemeSelection
            ? "Veja como o app pode ficar e aplique uma identidade visual ao seu negócio."
            : OnboardingStepCaptions[_onboardingStep];
        UpdateOnboardingStepDots();

        UpdateWizardChoiceStates();
        if (_onboardingStep == 5)
        {
            RefreshOnboardingReview();
        }

        Dispatcher.BeginInvoke(() =>
        {
            FrameworkElement? focusTarget = _onboardingStep switch
            {
                0 => InitialFullNameTextBox,
                1 when _showingThemeSelection => ThemeChoiceCardsPanel.Children.OfType<Button>().FirstOrDefault(),
                1 => SegmentSalonButton,
                2 => FindVisualChildren<Button>(ProfessionalCountStepPanel).FirstOrDefault(),
                3 => FindVisualChildren<Button>(ObjectiveStepPanel).FirstOrDefault(),
                4 => OnboardingCepTextBox,
                5 => FindVisualChildren<Button>(ReviewStepPanel).FirstOrDefault(),
                _ => OnboardingBackButton
            };
            focusTarget?.Focus();
        }, DispatcherPriority.Input);
    }

    private void RefreshOnboardingReview()
    {
        var template = _selectedOnboardingTemplate ?? CreateBusinessTemplate("Salão de Beleza");
        var businessName = FirstFilled(_data.Settings.BusinessName, InitialBusinessNameTextBox.Text, template.DefaultBusinessName);
        var professionalCount = FirstFilled(_selectedProfessionalCount, "1 profissional");
        var objective = FirstFilled(_selectedObjective, "Organizar agenda");
        var address = BuildOnboardingAddress();

        OnboardingReviewBusinessText.Text = businessName;
        OnboardingReviewSegmentText.Text = template.Title;
        OnboardingReviewOperationText.Text = $"{professionalCount} | {objective}";
        OnboardingReviewAddressText.Text = string.IsNullOrWhiteSpace(address) ? "Não informado" : address;
    }

    private void UpdateOnboardingStepDots()
    {
        Border[] dots =
        [
            OnboardingStepDot1,
            OnboardingStepDot2,
            OnboardingStepDot3,
            OnboardingStepDot4,
            OnboardingStepDot5,
            OnboardingStepDot6
        ];

        Border[] lines =
        [
            OnboardingStepLine1,
            OnboardingStepLine2,
            OnboardingStepLine3,
            OnboardingStepLine4,
            OnboardingStepLine5
        ];

        for (var index = 0; index < dots.Length; index++)
        {
            var isActive = index <= _onboardingStep;
            dots[index].Background = isActive ? AccentDarkBrush : PanelBrush;
            dots[index].BorderBrush = isActive ? AccentDarkBrush : LineBrush;

            if (dots[index].Child is TextBlock label)
            {
                label.Foreground = isActive ? Brushes.White : MutedBrush;
            }
        }

        for (var index = 0; index < lines.Length; index++)
        {
            lines[index].Background = index < _onboardingStep ? AccentDarkBrush : LineBrush;
        }
    }

    private void UpdateWizardChoiceStates()
    {
        UpdateWizardChoiceState(BusinessSegmentStepPanel, _selectedOnboardingTemplate?.Title);
        UpdateWizardChoiceState(ThemeChoiceCardsPanel, _selectedOnboardingThemeId);
        UpdateWizardChoiceState(ProfessionalCountStepPanel, _selectedProfessionalCount);
        UpdateWizardChoiceState(ObjectiveStepPanel, _selectedObjective);

        SetWizardContinueState(ContinueInitialDataButton, true);
        SetWizardContinueState(ContinueBusinessTypeButton, _selectedOnboardingTemplate is not null);
        SetWizardContinueState(ContinueThemeChoiceButton, true);
        SetWizardContinueState(ContinueProfessionalCountButton, !string.IsNullOrWhiteSpace(_selectedProfessionalCount));
        SetWizardContinueState(ContinueObjectiveButton, !string.IsNullOrWhiteSpace(_selectedObjective));
    }

    private static void SetWizardContinueState(Button button, bool enabled)
    {
        button.IsEnabled = enabled;
        button.Background = enabled ? AccentDarkBrush : AccentSoftBrush;
        button.BorderBrush = enabled ? AccentDarkBrush : LineBrush;
        button.Foreground = enabled ? Solid("#FFFFFF") : MutedBrush;
        TextElement.SetForeground(button, enabled ? Solid("#FFFFFF") : MutedBrush);
        button.Cursor = enabled ? Cursors.Hand : Cursors.Arrow;
    }

    private static void UpdateWizardChoiceState(DependencyObject root, string? selectedTag)
    {
        var selectedLookup = NormalizeTemplateLookup(selectedTag ?? "");
        var isThemeChoiceRoot = root is FrameworkElement { Name: "ThemeChoiceCardsPanel" };
        foreach (var button in FindVisualChildren<Button>(root))
        {
            var isSelected = button.Tag is string tag &&
                             (isThemeChoiceRoot || !string.IsNullOrWhiteSpace(selectedLookup)) &&
                             NormalizeTemplateLookup(tag) == selectedLookup;
            button.Background = isSelected ? AccentSoftBrush : PanelBrush;
            button.BorderBrush = isSelected ? AccentTextBrush : LineBrush;
            button.Foreground = isSelected && isThemeChoiceRoot ? AccentTextBrush : InkBrush;
            TextElement.SetForeground(button, isSelected && isThemeChoiceRoot ? AccentTextBrush : InkBrush);
            button.BorderThickness = new Thickness(isSelected ? (isThemeChoiceRoot ? 2 : 3) : 1);
            button.Padding = isThemeChoiceRoot ? new Thickness(7) : isSelected ? new Thickness(10) : new Thickness(12);
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var nestedChild in FindVisualChildren<T>(child))
            {
                yield return nestedChild;
            }
        }
    }

    private OnboardingTemplate ResolveBusinessTemplate(string? segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return CreateBusinessTemplate("Salão de Beleza");
        }

        return CreateBusinessTemplate(segment);
    }

    private OnboardingTemplate CreateBusinessTemplate(string businessType)
    {
        var trimmed = businessType.Trim();
        return trimmed switch
        {
            "Salão de Beleza" or "Unha e beleza + salão" => RenameTemplate(
                OnboardingTemplate.CreateIntegratedBeauty(),
                "Salão de Beleza",
                "Meu salão de beleza",
                "Agenda para salão com cabelo, unha, estética, lavatório, cadeiras e profissionais.",
                "Cliente: Camila | Escova + manicure | Cadeira 1 / Mesa 1"),
            "Barbearia" or "Cabelo e barbearia" => RenameTemplate(
                TemplateByTitle("Barbearia"),
                "Barbearia",
                "Minha barbearia",
                "Agenda para cortes, barba, combos, preferências do cliente e cadeiras de atendimento.",
                "Cliente: André | Corte + barba | Cadeira 1"),
            "Esmalteria" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Esmalteria",
                "Minha esmalteria",
                "Agenda para manicure, pedicure, alongamento, design e mesas de atendimento.",
                "Cliente: Camila | Alongamento almond | Mesa 2"),
            "Centro de Estética" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Centro de Estética",
                "Meu centro de estética",
                "Agenda para procedimentos, avaliação, retorno, preferências e salas de atendimento.",
                "Cliente: Larissa | Limpeza de pele | Sala estética 1"),
            "Podologia" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Podologia",
                "Minha clínica de podologia",
                "Agenda para avaliação, retorno, procedimento, observações e sala de atendimento.",
                "Cliente: Renata | Avaliação podológica | Sala 1"),
            "Spa" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Spa",
                "Meu spa",
                "Agenda para terapias, massagens, salas, pacotes e preferências do cliente.",
                "Cliente: Marina | Massagem relaxante | Sala 1"),
            "Clínica médica" => TemplateBySegment("Clínica médica"),
            "Petshop" => TemplateBySegment("Petshop"),
            "Mecânica" or "Oficina" => RenameTemplate(
                TemplateBySegment("Mecânica"),
                "Oficina",
                "Minha oficina",
                "Agenda para diagnósticos, revisões, veículos, box e acompanhamento de entrega.",
                "Cliente: Lucas | Onix ABC1D23 | Diagnóstico | Box 1"),
            "Outro segmento" => CreateGenericTemplate(),
            _ => _onboardingTemplates.FirstOrDefault(template =>
                     template.Title.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                     template.Segment.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                 ?? CreateGenericTemplate()
        };
    }

    private OnboardingTemplate TemplateByTitle(string title) =>
        FindTemplate(title, template => template.Title)
        ?? FindTemplate(title, template => template.Segment)
        ?? CreateGenericTemplate();

    private OnboardingTemplate TemplateBySegment(string segment) =>
        FindTemplate(segment, template => template.Segment)
        ?? FindTemplate(segment, template => template.Title)
        ?? CreateGenericTemplate();

    private OnboardingTemplate? FindTemplate(string value, Func<OnboardingTemplate, string> selector)
    {
        var lookup = NormalizeTemplateLookup(value);
        if (string.IsNullOrWhiteSpace(lookup))
        {
            return null;
        }

        return _onboardingTemplates.FirstOrDefault(template =>
            NormalizeTemplateLookup(selector(template)) == lookup);
    }

    private static string NormalizeTemplateLookup(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var clean = RemoveDiacritics(RepairMojibakeForLookup(value.Trim()));
        var builder = new StringBuilder(clean.Length);
        foreach (var ch in clean)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
        }

        return builder.ToString();
    }

    private static string RepairMojibakeForLookup(string value)
    {
        if (!value.Contains('Ã', StringComparison.Ordinal) &&
            !value.Contains('Â', StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            return Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
        }
        catch
        {
            return value;
        }
    }

    private static OnboardingTemplate RenameTemplate(
        OnboardingTemplate template,
        string title,
        string defaultBusinessName,
        string description,
        string example) =>
        template with
        {
            Title = title,
            Segment = title,
            DefaultBusinessName = defaultBusinessName,
            Description = description,
            Example = example
        };

    private static OnboardingTemplate CreateGenericTemplate() =>
        new(
            "Outro segmento",
            "Outro segmento",
            "Meu negócio",
            "Agenda simples para organizar clientes, profissionais, serviços e locais de atendimento.",
            "Cliente: Ana | Atendimento | Profissional 1 | Sala 1",
            "Cliente",
            "Observação / preferência / motivo",
            "Sala ou local",
            8,
            18,
            ["Sala 1", "Sala 2", "Atendimento 1"],
            [
                new("Atendimento", 30, 0, "Sala 1"),
                new("Retorno", 30, 0, "Sala 1"),
                new("Encaixe", 20, 0, "Atendimento 1")
            ],
            [
                new("Profissional 1", "Atendimento"),
                new("Profissional 2", "Atendimento")
            ]);

    private string BuildOnboardingAddress()
    {
        var street = OnboardingStreetTextBox.Text.Trim();
        var number = OnboardingAddressNumberTextBox.Text.Trim();
        var neighborhood = OnboardingNeighborhoodTextBox.Text.Trim();
        var complement = OnboardingAddressComplementTextBox.Text.Trim();
        var postalCode = OnboardingCepTextBox.Text.Trim();

        var addressParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street))
        {
            addressParts.Add(string.IsNullOrWhiteSpace(number) ? street : $"{street}, {number}");
        }

        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            addressParts.Add(neighborhood);
        }

        if (!string.IsNullOrWhiteSpace(complement))
        {
            addressParts.Add(complement);
        }

        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            addressParts.Add($"CEP {postalCode}");
        }

        return string.Join(" | ", addressParts);
    }

    private static int ParseHour(string? text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        var hourText = text.Split(':')[0];
        return int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour)
            ? Math.Clamp(hour, 5, 24)
            : fallback;
    }

    private static bool TryFormatBusinessDocument(string? text, out string formatted, out string error)
    {
        formatted = "";
        error = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length == 11)
        {
            formatted = $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}";
            return true;
        }

        if (digits.Length == 14)
        {
            formatted = $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits[12..]}";
            return true;
        }

        error = "Informe CPF com 11 dígitos ou CNPJ com 14 dígitos.";
        return false;
    }

    private static bool TryFormatBusinessPhone(string? text, out string formatted, out string error)
    {
        formatted = "";
        error = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            formatted = $"({digits[..2]}) {digits.Substring(2, 4)}-{digits[6..]}";
            return true;
        }

        if (digits.Length == 11)
        {
            formatted = $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}";
            return true;
        }

        error = "Informe telefone com DDD e 10 ou 11 dígitos.";
        return false;
    }

    private static bool LooksLikeEmail(string text)
    {
        var email = text.Trim();
        var atIndex = email.IndexOf('@');
        var dotIndex = email.LastIndexOf('.');
        return atIndex > 0 &&
               dotIndex > atIndex + 1 &&
               dotIndex < email.Length - 1 &&
               !email.Contains(' ');
    }

    private static string ToNameCase(string text)
    {
        var normalized = string.Join(' ', text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        var lowerWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "da",
            "das",
            "de",
            "do",
            "dos",
            "e"
        };
        var words = normalized.Split(' ');

        for (var index = 0; index < words.Length; index++)
        {
            var word = words[index].ToLower(Brazil);
            words[index] = index > 0 && lowerWords.Contains(word)
                ? word
                : CapitalizeWord(word);
        }

        return string.Join(' ', words);
    }

    private static string CapitalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return "";
        }

        return string.Create(word.Length, word, static (span, source) =>
        {
            span[0] = char.ToUpper(source[0], Brazil);
            for (var index = 1; index < source.Length; index++)
            {
                span[index] = source[index];
            }
        });
    }

    private void ApplyOnboardingTemplate(OnboardingTemplate template, string businessName, string businessDocument, string businessPhone, string businessAddress, int startHour, int endHour, int professionalLimit)
    {
        _data.Settings.BusinessName = string.IsNullOrWhiteSpace(businessName) ? template.DefaultBusinessName : businessName;
        _data.Settings.BusinessDocument = businessDocument;
        _data.Settings.BusinessPhone = businessPhone;
        _data.Settings.BusinessAddress = businessAddress;
        _data.Settings.BusinessSegment = template.Segment;
        _data.Settings.ClientLabel = template.ClientLabel;
        _data.Settings.ClientDetailLabel = template.ClientDetailLabel;
        _data.Settings.ResourceLabel = template.ResourceLabel;
        _data.Settings.WorkdayStartHour = startHour;
        _data.Settings.WorkdayEndHour = endHour;
        _data.Settings.Workdays = [1, 2, 3, 4, 5, 6];
        _data.Settings.WorkdayBreakEnabled = true;
        _data.Settings.WorkdayBreakStartHour = 12;
        _data.Settings.WorkdayBreakEndHour = 13;
        _data.Settings.Resources = [.. template.Resources];
        _data.Settings.OnboardingCompleted = true;

        _data.Services.Clear();
        foreach (var service in template.Services)
        {
            _data.Services.Add(new ServiceItem
            {
                Segment = template.Segment,
                Name = service.Name,
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                DefaultResource = service.DefaultResource
            });
        }

        _data.Professionals.Clear();
        foreach (var professional in template.Professionals.Take(Math.Max(1, professionalLimit)))
        {
            _data.Professionals.Add(new Professional
            {
                Name = professional.Name,
                Role = professional.Role,
                Segments = [template.Segment]
            });
        }

        _data.Customers.Clear();
        _data.Appointments.Clear();
        _selectedAppointment = null;
        _selectedDate = DateTime.Today;
    }

    private bool PruneProfessionalsForSelectedCount()
    {
        if (ProfessionalTemplateLimit(_data.Settings.ProfessionalCountRange) > 1)
        {
            return false;
        }

        var appointmentProfessionalIds = _data.Appointments
            .Select(item => item.ProfessionalId)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var appointmentProfessionalNames = _data.Appointments
            .Select(item => item.ProfessionalName)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removed = _data.Professionals.RemoveAll(professional =>
        {
            var name = professional.Name.Trim();
            return IsAutoSecondProfessionalName(name) &&
                   !appointmentProfessionalIds.Contains(professional.Id) &&
                   !appointmentProfessionalNames.Contains(name);
        });

        return removed > 0;
    }

    private bool RemoveBlockedAppointments()
    {
        var removed = _data.Appointments.RemoveAll(IsBlockedAppointment);
        if (_selectedAppointment is not null && IsBlockedAppointment(_selectedAppointment))
        {
            _selectedAppointment = null;
        }

        return removed > 0;
    }

    private static bool IsBlockedAppointment(Appointment appointment) =>
        appointment.Status == AppointmentStatus.Blocked ||
        appointment.ServiceName.Equals("Bloqueio interno", StringComparison.OrdinalIgnoreCase);

    private static int ProfessionalTemplateLimit(string professionalCount) =>
        professionalCount.Trim().StartsWith("2", StringComparison.OrdinalIgnoreCase) ? 2 : 1;

    private static bool IsAutoSecondProfessionalName(string name) =>
        name.Equals("Barbeiro 2", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Profissional 2", StringComparison.OrdinalIgnoreCase);

    private void ApplyBusinessLabels()
    {
        var displayName = BusinessDisplayName();
        var accountName = string.IsNullOrWhiteSpace(_data.Settings.AccountFullName)
            ? displayName
            : _data.Settings.AccountFullName;

        TopLogoText.Text = InitialsFor(accountName);
        SidebarLogoText.Text = TopLogoText.Text;
        SidebarUserNameText.Text = accountName;
        SidebarUserRoleText.Text = string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment)
            ? "Proprietária"
            : _data.Settings.BusinessSegment;

        AppTitleText.Text = "Agenda Livre";
        AppTitleText.Foreground = InkBrush;
        AppSubtitleText.Foreground = MutedBrush;
        var usesDarkSidebar = ThemeUsesDarkSidebar(ActiveThemeId);
        SidebarUserNameText.Foreground = usesDarkSidebar ? Brushes.White : InkBrush;
        SidebarUserRoleText.Foreground = usesDarkSidebar ? Solid("#A9A39D") : MutedBrush;

        if (string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment))
        {
            AppSubtitleText.Text = displayName;
            AppSubtitleText.ToolTip = null;
        }
        else
        {
            var documentPart = string.IsNullOrWhiteSpace(_data.Settings.BusinessDocument)
                ? ""
                : $" | {_data.Settings.BusinessDocument}";
            AppSubtitleText.Text = $"{displayName} · {_data.Settings.BusinessSegment}{documentPart}";
            var businessDetails = new[]
                {
                    _data.Settings.BusinessPhone,
                    _data.Settings.BusinessAddress
                }
                .Where(detail => !string.IsNullOrWhiteSpace(detail));
            AppSubtitleText.ToolTip = string.Join(" | ", businessDetails);
        }

        ClientSectionTitle.Text = _data.Settings.ClientLabel;
        ProfileLabelText.Text = _data.Settings.ClientDetailLabel;
        ResourceLabelText.Text = _data.Settings.ResourceLabel;
    }

    private void BuildTimeOptions()
    {
        _timeOptions.Clear();
        for (var hour = 6; hour <= 23; hour++)
        {
            for (var minute = 0; minute < 60; minute += 15)
            {
                _timeOptions.Add($"{hour:00}:{minute:00}");
            }
        }
    }

    private void RefreshAll(string? selectedId = null)
    {
        selectedId ??= _selectedAppointment?.Id;

        RefreshDayRows();
        RefreshWeekRows();
        RefreshMetrics();
        RefreshProfessionals();
        RefreshRecentCustomers();
        RefreshTitles();
        RefreshHomeDashboard();
        RefreshEstablishmentPage();
        RefreshFinancePage();
        RefreshReportsPage();
        RefreshMarketingPage();
        RefreshWhatsAppSurface();
        RefreshInstagramSurface();
        ScheduleWhatsAppAgendaSnapshotExport();

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            ReselectAppointment(selectedId);
        }
    }

    private void RefreshDayRows()
    {
        _dayRows.Clear();

        foreach (var appointment in AgendaDisplayAppointments())
        {
            _dayRows.Add(new AppointmentRow(appointment));
        }

        BuildScheduleBoard();
        UpdateAgendaEmptyState();
    }

    private void RefreshWeekRows()
    {
        _weekRows.Clear();
        var startOfWeek = StartOfWeek(_selectedDate);
        var endOfWeek = startOfWeek.AddDays(7);
        var weekAppointments = WeekDisplayAppointments(startOfWeek, endOfWeek);

        foreach (var appointment in weekAppointments)
        {
            _weekRows.Add(new AppointmentRow(appointment));
        }

        RefreshWeekSummary(startOfWeek, weekAppointments);
    }

    private List<Appointment> WeekDisplayAppointments(DateTime startOfWeek, DateTime endOfWeek)
    {
        var realAppointments = ApplyFilters(_data.Appointments.Where(item => item.Start >= startOfWeek && item.Start < endOfWeek))
            .Where(IsVisibleAgendaAppointment)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.CustomerName)
            .ToList();

        return realAppointments;
    }

    private void RefreshWeekSummary(DateTime startOfWeek, IReadOnlyCollection<Appointment> weekAppointments)
    {
        _weekSummaryRows.Clear();

        var professionalCount = Math.Max(1, GetProfessionalsForCurrentFilter().Count());
        var workdayMinutes = Math.Max(60, (_data.Settings.WorkdayEndHour - _data.Settings.WorkdayStartHour) * 60);
        var dailyCapacity = Math.Max(60, workdayMinutes * professionalCount);

        for (var index = 0; index < 7; index++)
        {
            var day = startOfWeek.AddDays(index);
            var appointments = weekAppointments
                .Where(item => item.Start.Date == day.Date)
                .OrderBy(item => item.Start)
                .ToList();

            var activeAppointments = appointments.Where(IsOperationalStatus).ToList();
            var count = appointments.Count;
            var minutes = activeAppointments.Sum(item => Math.Max(15, item.DurationMinutes));
            var percent = Math.Min(100d, minutes * 100d / dailyCapacity);
            var revenue = appointments
                .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
                .Sum(item => item.Price);
            var isSelected = day.Date == _selectedDate.Date;
            var accent = count > 0 ? AccentBrush : MutedBrush;

            _weekSummaryRows.Add(new WeekSummaryRow(
                day.ToString("ddd", Brazil).TrimEnd('.'),
                day.ToString("dd", Brazil),
                day.ToString("dd/MM", Brazil),
                count == 0 ? "0" : count.ToString(Brazil),
                count == 0 ? "livre" : "ag.",
                count == 0 ? "sem horários" : revenue.ToString("C0", Brazil),
                percent,
                accent,
                count > 0 || isSelected ? Solid("#FFFCF8") : Solid("#FFF9F4"),
                isSelected ? AccentBrush : LineBrush,
                isSelected));
        }

        WeekSummaryRangeText.Text = $"{startOfWeek:dd/MM} - {startOfWeek.AddDays(6):dd/MM}";
        var total = weekAppointments.Count;
        var totalRevenue = weekAppointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Sum(item => item.Price);
        WeekSummaryTotalText.Text = $"{total} atendimento{(total == 1 ? "" : "s")} | {totalRevenue.ToString("C0", Brazil)} previsto";
    }

    private List<Appointment> AgendaDisplayAppointments()
    {
        var realAppointments = ApplyFilters(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date))
            .Where(IsVisibleAgendaAppointment)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.CustomerName)
            .ToList();

        return realAppointments;
    }

    private static bool IsVisibleAgendaAppointment(Appointment appointment) =>
        !IsBlockedAppointment(appointment);

    private void RefreshMetrics()
    {
        _metrics.Clear();

        var dayAppointments = AgendaDisplayAppointments();
        var active = dayAppointments.Where(IsOperationalStatus).ToList();
        var confirmed = dayAppointments.Count(item => item.Status is AppointmentStatus.Confirmed or AppointmentStatus.Waiting or AppointmentStatus.InService);
        var done = dayAppointments.Count(item => item.Status == AppointmentStatus.Done);
        var late = dayAppointments.Count(item =>
            item.Start < DateTime.Now &&
            item.Start.Date == DateTime.Today &&
            item.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed);

        var professionals = GetProfessionalsForCurrentFilter().ToList();
        var professionalCount = Math.Max(1, professionals.Count);
        var workdayMinutes = Math.Max(60, (_data.Settings.WorkdayEndHour - _data.Settings.WorkdayStartHour) * 60);
        var busyMinutes = active.Sum(item => Math.Max(15, item.DurationMinutes));
        var freeSlots = Math.Max(0, ((workdayMinutes * professionalCount) - busyMinutes) / 30);
        var forecast = dayAppointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Sum(item => item.Price);

        _metrics.Add(new MetricRow("Atendimentos", active.Count.ToString(Brazil), "em aberto no dia", AccentSoftBrush, PackIconKind.AccountGroup, AccentBrush));
        _metrics.Add(new MetricRow("Confirmados", confirmed.ToString(Brazil), $"{PercentText(confirmed, Math.Max(1, dayAppointments.Count))} do total", Solid("#EAFBF2"), PackIconKind.CheckCircleOutline, Solid("#16A34A")));
        _metrics.Add(new MetricRow("Horários livres", freeSlots.ToString(Brazil), "janelas de 30 min", WarmSoftBrush, PackIconKind.ClockOutline, AccentBrush));
        _metrics.Add(new MetricRow("Caixa previsto", forecast.ToString("C0", Brazil), $"{done} finalizado(s) | {late} atraso(s)", AccentSoftBrush, PackIconKind.WalletOutline, AccentBrush));
    }

    private void RefreshProfessionals()
    {
        _professionalRows.Clear();

        var dayAppointments = AgendaDisplayAppointments();
        var workdayMinutes = Math.Max(60, (_data.Settings.WorkdayEndHour - _data.Settings.WorkdayStartHour) * 60);
        var expectedAppointmentCount = Math.Max(1, workdayMinutes / 60);

        foreach (var professional in GetProfessionalsForCurrentFilter().OrderBy(item => item.Name))
        {
            var professionalAppointments = dayAppointments
                .Where(item => item.ProfessionalId == professional.Id && IsOperationalStatus(item))
                .ToList();

            var minutes = professionalAppointments.Sum(item => item.DurationMinutes);
            var percent = Math.Min(100d, minutes * 100d / workdayMinutes);
            var accent = percent >= 85 ? Solid("#DC2626") : percent >= 60 ? AccentBrush : Solid("#16A34A");
            var brush = percent >= 85 ? RedSoftBrush : percent >= 60 ? AccentSoftBrush : Solid("#ECFDF5");

            _professionalRows.Add(new ProfessionalDayRow(
                professional.Name,
                professional.SegmentLine,
                $"{percent:0}%",
                $"{professionalAppointments.Count}/{expectedAppointmentCount} atendimentos",
                percent,
                brush,
                accent,
                InitialsFor(professional.Name),
                professionalAppointments.Count > 0));
        }

        if (_professionalRows.Count == 0)
        {
            _professionalRows.Add(new ProfessionalDayRow(
                "Nenhum profissional",
                "Cadastre a equipe para montar a agenda",
                "0%",
                "0 atendimentos",
                0,
                GraySoftBrush,
                MutedBrush,
                "--",
                false));
        }
    }

    private void RefreshRecentCustomers()
    {
        _recentCustomers.Clear();

        var segment = CurrentSegmentFilter();
        var customers = _data.Customers.AsEnumerable();
        if (segment != AllSegments)
        {
            customers = customers.Where(item => item.Segment == segment);
        }

        foreach (var customer in customers
                     .OrderByDescending(item => item.LastSeenAt)
                     .Take(8))
        {
            var lastAppointment = _data.Appointments
                .Where(item => CustomerMatches(item, customer))
                .OrderByDescending(item => item.Start)
                .FirstOrDefault();
            var detail = lastAppointment?.ServiceName;
            if (string.IsNullOrWhiteSpace(detail))
            {
                var detailParts = new[] { customer.Segment, customer.Profile }
                    .Where(part => !string.IsNullOrWhiteSpace(part));
                detail = string.Join(" | ", detailParts);
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "Cliente cadastrado";
            }

            var date = (lastAppointment?.Start ?? customer.LastSeenAt).ToString("dd/MM", Brazil);
            _recentCustomers.Add(new RecentCustomerRow(customer.Name, detail, date, InitialsFor(customer.Name)));
        }

        if (_recentCustomers.Count == 0)
        {
            _recentCustomers.Add(new RecentCustomerRow(
                "Nenhum cliente recente",
                "Os próximos atendimentos aparecerão aqui.",
                "",
                "--"));
        }
    }

    private static bool CustomerMatches(Appointment appointment, Customer customer) =>
        appointment.CustomerName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrWhiteSpace(customer.Phone) &&
         !string.IsNullOrWhiteSpace(appointment.CustomerPhone) &&
         appointment.CustomerPhone.Equals(customer.Phone, StringComparison.OrdinalIgnoreCase));

    private void RefreshTitles()
    {
        var segment = CurrentSegmentFilter();
        var dateText = _selectedDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", Brazil);
        SelectedDateTitleText.Text = dateText;
        var title = _selectedDate.Date == DateTime.Today ? "Agenda de hoje" : $"Agenda de {_selectedDate:dd/MM}";
        AgendaTitleText.Text = segment == AllSegments ? title : $"{title} - {segment}";
        AgendaSubtitleText.Text = dateText;
        AgendaBoardCountText.Text = $"{_dayRows.Count} atendimento{(_dayRows.Count == 1 ? "" : "s")}";
        AgendaBoardRangeText.Text = $"{_data.Settings.WorkdayStartHour:00}:00-{_data.Settings.WorkdayEndHour:00}:00";
    }

    private void RefreshHomeDashboard()
    {
        var today = DateTime.Today;
        var now = DateTime.Now;
        var targetDate = _selectedDate.Date;
        var dayAppointments = HomeDisplayAppointments(targetDate);
        var confirmed = dayAppointments.Count(item => item.Status is AppointmentStatus.Confirmed or AppointmentStatus.Waiting or AppointmentStatus.InService);
        var activeNow = dayAppointments.Count(item => item.Status is AppointmentStatus.Waiting or AppointmentStatus.InService);
        var pending = dayAppointments.Count(item => item.Status == AppointmentStatus.Scheduled);
        var done = dayAppointments.Count(item => item.Status == AppointmentStatus.Done);
        var forecast = dayAppointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Sum(item => item.Price);
        var realizedToday = dayAppointments
            .Where(item => item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);

        HomeGreetingText.Text = $"{GreetingFor(now)}, {FirstName(_data.Settings.AccountFullName)}";
        HomeDateText.Text = targetDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", Brazil);
        HomeBusinessText.Text = string.IsNullOrWhiteSpace(_data.Settings.BusinessName)
            ? "Balcão Livre"
            : _data.Settings.BusinessName;

        var dateSuffix = targetDate == today
            ? "hoje"
            : targetDate == today.AddDays(1) ? "amanhã" : $"em {targetDate:dd/MM}";
        HomeSummaryTitleText.Text = targetDate == today
            ? "Resumo do dia"
            : targetDate == today.AddDays(1) ? "Resumo de amanhã" : $"Resumo de {targetDate:dd/MM}";
        HomeCashCaptionText.Text = targetDate == today
            ? "Caixa de hoje"
            : targetDate == today.AddDays(1) ? "Caixa de amanhã" : $"Caixa de {targetDate:dd/MM}";
        var cashMetricTitle = targetDate == today ? "Caixa do dia" : "Caixa previsto";
        var cashMetricValue = targetDate == today ? realizedToday : forecast;
        _homeMetrics.Clear();
        _homeMetrics.Add(new HomeMetricRow($"Agendamentos {dateSuffix}", dayAppointments.Count.ToString(Brazil), $"{confirmed} confirmado(s)", AccentSoftBrush, PackIconKind.CalendarMonth, Brushes.White));
        _homeMetrics.Add(new HomeMetricRow(
            "Confirmados",
            confirmed.ToString(Brazil),
            $"{PercentText(confirmed, Math.Max(1, dayAppointments.Count))} do total",
            IsActiveBarberMidnight() ? AccentSoftBrush : Solid("#EAFBF2"),
            PackIconKind.CheckCircleOutline,
            Brushes.White));
        _homeMetrics.Add(new HomeMetricRow("A confirmar", pending.ToString(Brazil), "precisa de WhatsApp", WarmSoftBrush, PackIconKind.ClockOutline, Brushes.White));
        _homeMetrics.Add(new HomeMetricRow(cashMetricTitle, cashMetricValue.ToString("C0", Brazil), $"{done} finalizado(s)", AccentSoftBrush, PackIconKind.WalletOutline, Brushes.White));

        var appointmentReference = targetDate == today ? now : targetDate;
        _homeNextAppointment = dayAppointments.FirstOrDefault(item =>
            item.Status is AppointmentStatus.Waiting or AppointmentStatus.InService)
            ?? dayAppointments.FirstOrDefault(item =>
            item.Start >= appointmentReference &&
            item.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.Waiting or AppointmentStatus.InService);
        RefreshHomeNextAppointment();
        RefreshHomeAgendaRows(dayAppointments, now);
        RefreshHomeFinance(targetDate, forecast, realizedToday);
        RefreshHomeGoals(realizedToday);
        RefreshHomeAlerts(dayAppointments, pending);
        RefreshHomeTopServices();
        RefreshHomeCustomers();
    }

    private List<Appointment> HomeDisplayAppointments(DateTime date)
    {
        var realAppointments = ApplySegmentFilter(_data.Appointments.Where(item => item.Start.Date == date.Date))
            .OrderBy(item => item.Start)
            .ThenBy(item => item.CustomerName)
            .ToList();

        return realAppointments;
    }

    private void RefreshHomeNextAppointment()
    {
        if (_homeNextAppointment is null)
        {
            HomeNextTimeText.Text = "--:--";
            HomeNextCustomerText.Text = "Nenhum atendimento pendente";
            HomeNextServiceText.Text = "A agenda está livre no momento";
            HomeNextProfessionalText.Text = "Sem profissional vinculado";
            return;
        }

        HomeNextTimeText.Text = _homeNextAppointment.Start.ToString("HH:mm", Brazil);
        HomeNextCustomerText.Text = _homeNextAppointment.CustomerName;
        HomeNextServiceText.Text = _homeNextAppointment.ServiceName;
        HomeNextProfessionalText.Text = _homeNextAppointment.ProfessionalName;
    }

    private void RefreshHomeAgendaRows(IReadOnlyList<Appointment> dayAppointments, DateTime now)
    {
        _homeAgendaRows.Clear();
        BuildHomeScheduleBoard(dayAppointments);

        var nextRows = dayAppointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .Take(6)
            .ToList();

        HomeEmptyScheduleBoard.Visibility = Visibility.Visible;
        HomeAgendaItemsControl.Visibility = Visibility.Collapsed;

        if (nextRows.Count == 0)
        {
            return;
        }

        foreach (var appointment in nextRows)
        {
            _homeAgendaRows.Add(new HomeAgendaSummaryRow(
                appointment.Start.ToString("HH:mm", Brazil),
                $"{appointment.DurationMinutes} min",
                appointment.CustomerName,
                appointment.ServiceName,
                appointment.ProfessionalName,
                StatusLabel(appointment.Status),
                StatusBackground(appointment.Status),
                StatusForeground(appointment.Status)));
        }
    }

    private void BuildHomeScheduleBoard(IReadOnlyList<Appointment> dayAppointments)
    {
        if (HomeScheduleBoardGrid is null)
        {
            return;
        }

        const double timeColumnWidth = 72;
        const double headerHeight = 44;
        const double rowHeight = 48;
        const int visibleHourWindow = 9;
        const string unassignedId = "__sem_profissional__";

        var visibleAppointments = dayAppointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.CustomerName)
            .ToList();

        var startHour = Math.Clamp(_data.Settings.WorkdayStartHour, 0, 23);
        if (visibleAppointments.Count > 0)
        {
            startHour = Math.Min(startHour, visibleAppointments.Min(item => item.Start.Hour));
        }

        var configuredEndHour = Math.Clamp(_data.Settings.WorkdayEndHour, startHour + 1, 24);
        var endHour = Math.Min(configuredEndHour, startHour + visibleHourWindow);
        if (visibleAppointments.Count > 0)
        {
            var latestEndHour = visibleAppointments
                .Select(item => Math.Min(24, (int)Math.Ceiling((item.End - item.End.Date).TotalHours)))
                .DefaultIfEmpty(endHour)
                .Max();
            endHour = Math.Max(endHour, latestEndHour);
        }
        endHour = Math.Clamp(endHour, startHour + 1, 24);
        var slotCount = Math.Max(1, endHour - startHour);

        var columns = BuildHomeScheduleColumns(visibleAppointments, unassignedId);
        var board = HomeScheduleBoardGrid;
        board.Children.Clear();
        board.ColumnDefinitions.Clear();
        board.RowDefinitions.Clear();
        board.MinHeight = headerHeight + rowHeight * slotCount;

        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(timeColumnWidth) });
        foreach (var _ in columns)
        {
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(headerHeight) });
        for (var row = 0; row < slotCount; row++)
        {
            board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });
        }

        AddHomeScheduleCorner(board);
        AddHomeScheduleHeaders(board, columns);
        var cells = AddHomeScheduleCells(board, columns, startHour, slotCount);

        foreach (var appointment in visibleAppointments)
        {
            var row = Math.Clamp(appointment.Start.Hour - startHour, 0, slotCount - 1);
            var column = columns.FindIndex(item =>
                !string.IsNullOrWhiteSpace(appointment.ProfessionalId) &&
                item.Id.Equals(appointment.ProfessionalId, StringComparison.OrdinalIgnoreCase));

            if (column < 0 && string.IsNullOrWhiteSpace(appointment.ProfessionalId))
            {
                column = columns.FindIndex(item => item.Id == unassignedId);
            }

            if (column < 0)
            {
                column = 0;
            }

            if (cells.TryGetValue((row, column), out var stack))
            {
                stack.Children.Add(CreateHomeScheduleAppointmentCard(appointment));
            }
        }

        if (visibleAppointments.Count == 0)
        {
            AddHomeScheduleEmptyState(board, slotCount, columns.Count);
        }
    }

    private void AddHomeScheduleEmptyState(Grid board, int slotCount, int columnCount)
    {
        var action = new Button
        {
            Content = "+ Agendar atendimento",
            Height = 40,
            MinWidth = 154,
            Padding = new Thickness(14, 0, 14, 0),
            Style = (Style)FindResource("CommandButton"),
            Margin = new Thickness(0, 7, 0, 0)
        };
        action.Click += NewButton_Click;

        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new PackIcon
                {
                    Kind = PackIconKind.CalendarPlus,
                    Foreground = AccentBrush,
                    Width = 20,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = "Nenhum atendimento hoje",
                    Foreground = InkBrush,
                    FontSize = 14.5,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                },
                new TextBlock
                {
                    Text = "A agenda está livre. Crie o primeiro horário ou clique diretamente na grade.",
                    Foreground = MutedBrush,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 360,
                    Margin = new Thickness(0, 2, 0, 0)
                },
                action
            }
        };

        var overlay = new Border
        {
            Background = Solid("#F5FFFFFF"),
            Padding = new Thickness(14, 8, 14, 8),
            Child = content
        };
        Grid.SetRow(overlay, 1);
        Grid.SetRowSpan(overlay, Math.Min(3, Math.Max(1, slotCount)));
        Grid.SetColumn(overlay, 1);
        Grid.SetColumnSpan(overlay, Math.Max(1, columnCount));
        Panel.SetZIndex(overlay, 12);
        board.Children.Add(overlay);
    }

    private List<HomeScheduleColumn> BuildHomeScheduleColumns(IReadOnlyList<Appointment> appointments, string unassignedId)
    {
        const int maxColumns = 3;
        var columns = new List<HomeScheduleColumn>();
        var appointmentProfessionalIds = appointments
            .Select(item => item.ProfessionalId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxColumns)
            .ToList();

        foreach (var professionalId in appointmentProfessionalIds)
        {
            var professional = _data.Professionals.FirstOrDefault(item => item.Id.Equals(professionalId, StringComparison.OrdinalIgnoreCase));
            if (professional is not null)
            {
                columns.Add(new HomeScheduleColumn(professional.Id, professional.Name, professional.SegmentLine, InitialsFor(professional.Name)));
                continue;
            }

            var appointment = appointments.First(item => item.ProfessionalId.Equals(professionalId, StringComparison.OrdinalIgnoreCase));
            var name = string.IsNullOrWhiteSpace(appointment.ProfessionalName) ? "Profissional" : appointment.ProfessionalName;
            columns.Add(new HomeScheduleColumn(professionalId, name, appointment.Segment, InitialsFor(name)));
        }

        if (appointments.Any(item => string.IsNullOrWhiteSpace(item.ProfessionalId)) && columns.Count < maxColumns)
        {
            columns.Add(new HomeScheduleColumn(unassignedId, "Sem profissional", "Agenda livre", "--"));
        }

        if (columns.Count == 0)
        {
            columns.AddRange(_data.Professionals
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .Take(2)
                .Select(item => new HomeScheduleColumn(item.Id, item.Name, item.SegmentLine, InitialsFor(item.Name))));
        }

        if (columns.Count == 0)
        {
            columns.Add(new HomeScheduleColumn(unassignedId, "Agenda livre", "Horários disponíveis", "--"));
        }

        return columns;
    }

    private void AddHomeScheduleCorner(Grid board)
    {
        var corner = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Child = new TextBlock
            {
                Text = "Horário",
                Foreground = InkBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        board.Children.Add(corner);
    }

    private void AddHomeScheduleHeaders(Grid board, IReadOnlyList<HomeScheduleColumn> columns)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var header = new Border
            {
                Background = WarmSoftBrush,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(10, 6, 10, 6),
                Child = CreateHomeScheduleHeaderContent(column)
            };

            Grid.SetRow(header, 0);
            Grid.SetColumn(header, index + 1);
            board.Children.Add(header);
        }
    }

    private Grid CreateHomeScheduleHeaderContent(HomeScheduleColumn column)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = AccentSoftBrush,
            Child = new TextBlock
            {
                Text = column.Initials,
                Foreground = AccentBrush,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock
        {
            Text = column.Name,
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        info.Children.Add(new TextBlock
        {
            Text = column.Detail,
            Foreground = MutedBrush,
            FontSize = 10,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        return grid;
    }

    private Dictionary<(int Row, int Column), StackPanel> AddHomeScheduleCells(
        Grid board,
        IReadOnlyList<HomeScheduleColumn> columns,
        int startHour,
        int slotCount)
    {
        var cells = new Dictionary<(int Row, int Column), StackPanel>();
        for (var row = 0; row < slotCount; row++)
        {
            var hour = startHour + row;
            var timeCell = new Border
            {
                Background = WarmSoftBrush,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = $"{hour:00}:00",
                    Foreground = MutedBrush,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetRow(timeCell, row + 1);
            Grid.SetColumn(timeCell, 0);
            board.Children.Add(timeCell);

            for (var column = 0; column < columns.Count; column++)
            {
                var stack = new StackPanel
                {
                    Margin = new Thickness(5, 4, 5, 2),
                    VerticalAlignment = VerticalAlignment.Top
                };
                var cell = new Border
                {
                    Background = Solid("#FFFFFF"),
                    BorderBrush = LineBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Child = stack
                };
                Grid.SetRow(cell, row + 1);
                Grid.SetColumn(cell, column + 1);
                board.Children.Add(cell);
                cells[(row, column)] = stack;
            }
        }

        return cells;
    }

    private Border CreateHomeScheduleAppointmentCard(Appointment appointment)
    {
        var accent = ScheduleAccentFor(appointment.Status);
        var titleBrush = ScheduleTextFor(appointment.Status);
        var detailBrush = ScheduleSubtextFor(appointment.Status);
        var card = new Border
        {
            Background = ScheduleCardBackground(appointment.Status),
            BorderBrush = IsActiveBarberMidnight() ? Solid("#FFFFFF") : LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 0, 0, 4),
            MinHeight = 38,
            Cursor = Cursors.Hand,
            Tag = appointment,
            ToolTip = $"{appointment.Start:HH:mm}-{appointment.End:HH:mm} | {appointment.CustomerName} | {appointment.ServiceName}"
        };
        card.PreviewMouseLeftButtonDown += ScheduleAppointment_MouseLeftButtonDown;

        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.Children.Add(new Border
        {
            Background = accent,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(-8, -5, 7, -5)
        });

        var text = new StackPanel();
        Grid.SetColumn(text, 1);
        text.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:HH:mm}  {appointment.ServiceName}",
            Foreground = titleBrush,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        text.Children.Add(new TextBlock
        {
            Text = $"{appointment.CustomerName} | {ScheduleStatusLabel(appointment.Status)}",
            Foreground = detailBrush,
            FontSize = 10.2,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 0)
        });
        layout.Children.Add(text);
        card.Child = layout;
        return card;
    }

    private void RefreshHomeFinance(DateTime targetDate, decimal forecast, decimal realizedToday)
    {
        var weekStart = StartOfWeek(targetDate);
        var weekEnd = weekStart.AddDays(7);
        var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var realizedWeek = SumRealizedRevenue(weekStart, weekEnd);
        var realizedMonth = SumRealizedRevenue(monthStart, monthEnd);
        HomeRevenueDayText.Text = realizedToday.ToString("C0", Brazil);
        HomeRevenueWeekText.Text = realizedWeek.ToString("C0", Brazil);
        HomeRevenueMonthText.Text = realizedMonth.ToString("C0", Brazil);
        HomeFinancialSubtitleText.Text = targetDate == DateTime.Today
            ? $"Previsto hoje: {forecast.ToString("C0", Brazil)}"
            : $"Previsto em {targetDate:dd/MM}: {forecast.ToString("C0", Brazil)}";

        var max = Math.Max(1m, Math.Max(realizedToday, Math.Max(realizedWeek, realizedMonth)));
        _homeFinanceBars.Clear();
        _homeFinanceBars.Add(new HomeFinanceBarRow("Faturamento do dia", realizedToday.ToString("C0", Brazil), Percent(realizedToday, max)));
        _homeFinanceBars.Add(new HomeFinanceBarRow("Faturamento da semana", realizedWeek.ToString("C0", Brazil), Percent(realizedWeek, max)));
        _homeFinanceBars.Add(new HomeFinanceBarRow("Faturamento do mês", realizedMonth.ToString("C0", Brazil), Percent(realizedMonth, max)));
        HomeCashProgress.Value = Percent(realizedToday, Math.Max(1m, forecast));
    }

    private void RefreshHomeGoals(decimal realizedToday)
    {
        var today = DateTime.Today;
        var confirmations = _data.Appointments.Count(item =>
            item.Start.Date >= today &&
            item.Status == AppointmentStatus.Scheduled);
        var staleCustomers = _data.Customers.Count(item => item.LastSeenAt.Date <= today.AddDays(-30));

        HomeDailyGoalText.Text = confirmations.ToString(Brazil);
        HomeMonthlyGoalText.Text = staleCustomers.ToString(Brazil);
        HomeDailyGoalProgress.Value = 0;
        HomeMonthlyGoalProgress.Value = 0;
        HomeGoalSubtitleText.Text = _data.Settings.WhatsAppLinked
            ? $"WhatsApp linkado em {FormatPhone(_data.Settings.WhatsAppStorePhone)}"
            : "WhatsApp nao linkado";
    }

    private void RefreshHomeAlerts(IReadOnlyList<Appointment> dayAppointments, int pending)
    {
        var paymentPending = dayAppointments.Count(item =>
            item.Price > 0 &&
            item.Status is AppointmentStatus.Confirmed or AppointmentStatus.Waiting or AppointmentStatus.InService);
        var staleCustomers = _data.Customers.Count(item => item.LastSeenAt.Date <= DateTime.Today.AddDays(-30));

        _homeAlerts.Clear();
        _homeAlerts.Add(new HomeAlertRow(
            "Confirmações pendentes",
            $"{pending} agendamento(s) aguardando confirmação.",
            PackIconKind.ClockOutline,
            Solid("#B45309"),
            Solid("#FFF7ED"),
            Solid("#FED7AA")));
        _homeAlerts.Add(new HomeAlertRow(
            "Pagamentos pendentes",
            $"{paymentPending} atendimento(s) com valor ainda não finalizado.",
            PackIconKind.CashClock,
            AccentBrush,
            AccentSoftBrush,
            Solid("#F3D7C7")));
        _homeAlerts.Add(new HomeAlertRow(
            "Clientes sem retorno",
            $"{staleCustomers} cliente(s) sem atendimento há mais de 30 dias.",
            PackIconKind.AccountClock,
            AccentBrush,
            WarmSoftBrush,
            LineBrush));
    }

    private void RefreshHomeTopServices()
    {
        _homeTopServices.Clear();
        var since = DateTime.Today.AddDays(-30);
        var rows = _data.Appointments
            .Where(item => item.Start >= since && item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .GroupBy(item => item.ServiceName)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(6)
            .Select(group => new HomeServiceRow(group.Key, $"{group.Count()} ag."))
            .ToList();

        if (rows.Count == 0)
        {
            _homeTopServices.Add(new HomeServiceRow("Sem serviços agendados", "0 ag."));
            return;
        }

        foreach (var row in rows)
        {
            _homeTopServices.Add(row);
        }
    }

    private void RefreshHomeCustomers()
    {
        _homeRecentCustomers.Clear();
        foreach (var customer in _data.Customers.OrderByDescending(item => item.LastSeenAt).Take(5))
        {
            var lastAppointment = _data.Appointments
                .Where(item => item.CustomerName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Start)
                .FirstOrDefault();
            var detail = lastAppointment is null
                ? $"Último atendimento: {customer.LastSeenAt:dd/MM}"
                : $"{lastAppointment.Start:dd/MM} | {lastAppointment.ServiceName}";
            _homeRecentCustomers.Add(new HomeCustomerSummaryRow(customer.Name, detail, InitialsFor(customer.Name)));
        }

    }

    private static string PercentText(int value, int total) =>
        $"{Math.Round(value * 100d / Math.Max(1, total)):0}%";

    private static string InitialsFor(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "--";
        }

        var first = parts[0][0].ToString().ToUpper(Brazil);
        var second = parts.Length > 1 ? parts[^1][0].ToString().ToUpper(Brazil) : "";
        return first + second;
    }

    private void RefreshEstablishmentPage()
    {
        CloseCustomerInfoPopup();
        CloseProfessionalInfoPopup();
        CloseServiceInfoPopup();
        EstablishmentBusinessText.Text = BusinessDisplayName();

        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var productSalesThisMonth = _data.ProductSales
            .Where(item => item.SoldAt >= monthStart && item.SoldAt < nextMonth)
            .ToList();
        var productRevenueThisMonth = productSalesThisMonth.Sum(item => item.Total);
        var appointmentsThisMonth = _data.Appointments
            .Where(item => item.Start >= monthStart && item.Start < nextMonth && item.Status != AppointmentStatus.Blocked)
            .ToList();
        var appointmentRevenueThisMonth = appointmentsThisMonth
            .Where(item => item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);
        var manualRevenueThisMonth = _data.ManualPayments
            .Where(item => item.PaidAt >= monthStart && item.PaidAt < nextMonth)
            .Sum(item => item.Value);
        var totalRevenueThisMonth = appointmentRevenueThisMonth + productRevenueThisMonth + manualRevenueThisMonth;
        var averageRevenue = appointmentsThisMonth.Count == 0
            ? 0
            : totalRevenueThisMonth / Math.Max(1, appointmentsThisMonth.Count);

        _establishmentMetrics.Clear();
        _establishmentMetrics.Add(new EstablishmentMetricRow("Clientes", _data.Customers.Count.ToString(Brazil), "cadastrados", AccentSoftBrush, PackIconKind.AccountGroup, AccentBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Profissionais", _data.Professionals.Count(item => item.IsActive).ToString(Brazil), "ativos", AccentSoftBrush, PackIconKind.AccountOutline, AccentBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Serviços", _data.Services.Count(item => item.IsActive).ToString(Brazil), "no catálogo", Solid("#ECFDF5"), PackIconKind.ClipboardText, Solid("#10B981")));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Receita do mês", totalRevenueThisMonth.ToString("C0", Brazil), "faturamento", WarmSoftBrush, PackIconKind.WalletOutline, AccentBrush));

        _establishmentSections.Clear();
        _establishmentSections.Add(new EstablishmentSectionRow("Clientes", $"{_data.Customers.Count} cadastrado(s)", "Acesse cadastros e histórico de clientes.", "Gerenciar", PackIconKind.AccountGroup, AccentBrush, AccentSoftBrush));
        _establishmentSections.Add(new EstablishmentSectionRow("Profissionais", $"{_data.Professionals.Count(item => item.IsActive)} ativo(s)", "Gerencie sua equipe de profissionais.", "Gerenciar", PackIconKind.AccountOutline, AccentBrush, AccentSoftBrush));
        _establishmentSections.Add(new EstablishmentSectionRow("Serviços", $"{_data.Services.Count(item => item.IsActive)} no catálogo", "Cadastre e organize seus serviços.", "Gerenciar", PackIconKind.ClipboardText, Solid("#10B981"), Solid("#ECFDF5")));

        EstablishmentMonthAppointmentsText.Text = appointmentsThisMonth.Count.ToString(Brazil);
        EstablishmentAverageRevenueText.Text = averageRevenue.ToString("C", Brazil);
        EstablishmentMonthlyRevenueText.Text = totalRevenueThisMonth.ToString("C0", Brazil);
        EstablishmentClientsTotalText.Text = $"+{_data.Customers.Count.ToString(Brazil)} clientes no total";
        EstablishmentProfessionalsTotalText.Text = $"{_data.Professionals.Count.ToString(Brazil)} profissionais no total";
        EstablishmentServicesTotalText.Text = $"{_data.Services.Count.ToString(Brazil)} serviços no catálogo";

        RefreshEstablishmentClients();
        RefreshEstablishmentProfessionals();
        RefreshEstablishmentServices();
        RefreshEstablishmentProducts();
        RefreshEstablishmentSales();
    }

    private void RefreshEstablishmentClients()
    {
        _establishmentClients.Clear();
        var hasCustomers = _data.Customers.Count > 0;
        EstablishmentClientsItemsControl.Visibility = hasCustomers ? Visibility.Visible : Visibility.Collapsed;
        EstablishmentClientsEmptyPanel.Visibility = hasCustomers ? Visibility.Collapsed : Visibility.Visible;
        EstablishmentClientsTotalText.Visibility = hasCustomers ? Visibility.Visible : Visibility.Collapsed;

        foreach (var customer in _data.Customers.OrderByDescending(item => item.LastSeenAt).ThenBy(item => item.Name).Take(4))
        {
            var phoneLine = string.IsNullOrWhiteSpace(customer.Phone)
                ? "Sem telefone cadastrado"
                : FormatPhone(customer.Phone);
            var contextLine = string.Join(" | ", new[] { customer.Profile, customer.Tags, customer.Segment }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Take(2));
            var detail = string.Join(Environment.NewLine, new[] { phoneLine, contextLine }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
            _establishmentClients.Add(new EstablishmentListRow(
                customer.Name,
                FirstFilled(detail, "Sem detalhes cadastrados"),
                customer.AcceptsWhatsApp ? "Ativa" : "Inativa",
                customer.AcceptsWhatsApp ? Solid("#DCFCE7") : GraySoftBrush,
                customer.AcceptsWhatsApp ? Solid("#16A34A") : MutedBrush,
                customer.Id));
        }

    }

    private void EstablishmentClientsItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var row = FindDataContext<EstablishmentListRow>(e.OriginalSource as DependencyObject);
        if (row is null || string.IsNullOrWhiteSpace(row.Id))
        {
            return;
        }

        var customer = _data.Customers.FirstOrDefault(item => item.Id.Equals(row.Id, StringComparison.OrdinalIgnoreCase));
        if (customer is null)
        {
            ShowStatus("Cliente não encontrado.");
            return;
        }

        ShowCustomerInfoPopup(customer);
        e.Handled = true;
    }

    private void ShowCustomerInfoPopup(Customer customer)
    {
        CloseCustomerInfoPopup();
        CloseProfessionalInfoPopup();
        CloseServiceInfoPopup();

        var popup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = 12,
            VerticalOffset = 10,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true
        };

        popup.Child = CreateCustomerInfoPopupContent(customer);
        _customerInfoPopup = popup;
        popup.Closed += (_, _) => DetachCustomerInfoOutsideClickHandler();
        popup.IsOpen = true;
        AttachCustomerInfoOutsideClickHandler();
    }

    private void AttachCustomerInfoOutsideClickHandler()
    {
        DetachCustomerInfoOutsideClickHandler();

        _customerInfoOutsideClickHandler = (_, args) =>
        {
            if (_customerInfoPopup?.Child is DependencyObject popupChild &&
                IsDescendantOf(args.OriginalSource as DependencyObject, popupChild))
            {
                return;
            }

            CloseCustomerInfoPopup();
        };

        Dispatcher.BeginInvoke(() =>
        {
            if (_customerInfoPopup?.IsOpen == true && _customerInfoOutsideClickHandler is not null)
            {
                PreviewMouseDown += _customerInfoOutsideClickHandler;
            }
        }, DispatcherPriority.Background);
    }

    private void DetachCustomerInfoOutsideClickHandler()
    {
        if (_customerInfoOutsideClickHandler is null)
        {
            return;
        }

        PreviewMouseDown -= _customerInfoOutsideClickHandler;
        _customerInfoOutsideClickHandler = null;
    }

    private void CloseCustomerInfoPopup()
    {
        DetachCustomerInfoOutsideClickHandler();

        if (_customerInfoPopup is null)
        {
            return;
        }

        var popup = _customerInfoPopup;
        _customerInfoPopup = null;
        popup.IsOpen = false;
    }

    private Border CreateCustomerInfoPopupContent(Customer customer)
    {
        var card = new Border
        {
            Width = 334,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.16
            }
        };

        var body = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            Background = customer.AcceptsWhatsApp ? Solid("#DCFCE7") : GraySoftBrush,
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new TextBlock
            {
                Text = InitialsFor(customer.Name),
                Foreground = customer.AcceptsWhatsApp ? Solid("#16A34A") : MutedBrush,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = FirstFilled(customer.Name, "Cliente"),
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = customer.AcceptsWhatsApp ? "WhatsApp ativo" : "WhatsApp inativo",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(titleStack, 1);
        header.Children.Add(titleStack);

        var closeButton = IconOnlyButton(PackIconKind.Close, 28);
        closeButton.Click += (_, _) => CloseCustomerInfoPopup();
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);
        body.Children.Add(header);

        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.AccountOutline, "Telefone", FirstFilled(FormatPhone(customer.Phone), "Sem telefone cadastrado")));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.StorefrontOutline, "Segmento", FirstFilled(customer.Segment, _data.Settings.BusinessSegment, "Sem segmento")));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.AccountClock, "Perfil", FirstFilled(customer.Profile, customer.Tags, "Sem perfil ou tags")));

        if (!string.IsNullOrWhiteSpace(customer.Document))
        {
            body.Children.Add(CreateAppointmentInfoRow(PackIconKind.ClipboardText, "Documento", customer.Document));
        }

        if (!string.IsNullOrWhiteSpace(customer.Notes))
        {
            body.Children.Add(CreateAppointmentInfoRow(PackIconKind.Pencil, "Observações", customer.Notes));
        }

        var appointments = _data.Appointments
            .Where(item => CustomerMatches(item, customer))
            .OrderByDescending(item => item.Start)
            .Take(3)
            .ToList();

        body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 4, 0, 10)
        });

        body.Children.Add(new TextBlock
        {
            Text = "Histórico recente",
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        if (appointments.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = "Nenhum atendimento registrado ainda.",
                Foreground = MutedBrush,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var appointment in appointments)
            {
                body.Children.Add(CreateCustomerHistoryRow(appointment));
            }
        }

        body.Children.Add(CreateCustomerPopupActions(customer));

        card.Child = body;
        return card;
    }

    private Grid CreateCustomerPopupActions(Customer customer)
    {
        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var editIcon = new PackIcon
        {
            Kind = PackIconKind.Pencil,
            Width = 15,
            Height = 15,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        BindForegroundToButton(editIcon);

        var editText = new TextBlock
        {
            Text = "Editar cliente",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindForegroundToButton(editText);

        var editButton = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Height = 34,
            Padding = new Thickness(10, 0, 10, 0),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    editIcon,
                    editText
                }
            }
        };
        editButton.Click += (_, _) =>
        {
            CloseCustomerInfoPopup();
            EditCustomer(customer.Id);
        };
        actions.Children.Add(editButton);

        var whatsAppButton = new Button
        {
            Style = (Style)FindResource("CommandButton"),
            Height = 34,
            Padding = new Thickness(10, 0, 10, 0),
            Background = Solid("#16A34A"),
            BorderBrush = Solid("#16A34A"),
            Foreground = Brushes.White,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new PackIcon
                    {
                        Kind = PackIconKind.Whatsapp,
                        Width = 15,
                        Height = 15,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    },
                    new TextBlock
                    {
                        Text = "WhatsApp",
                        Foreground = Brushes.White,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        whatsAppButton.Click += async (_, _) => await SendCustomerWhatsAppAsync(customer);
        Grid.SetColumn(whatsAppButton, 2);
        actions.Children.Add(whatsAppButton);

        return actions;
    }

    private async Task SendCustomerWhatsAppAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Phone))
        {
            ShowStatus($"Telefone não cadastrado para {customer.Name}.");
            return;
        }

        var phone = NormalizeBrazilPhone(customer.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus($"Telefone inválido para {customer.Name}.");
            return;
        }

        CloseCustomerInfoPopup();
        var sent = await SendOrOpenWhatsAppAsync(customer.Name, phone, BuildCustomerWhatsAppMessage(customer), "Cliente");
        ShowStatus(sent
            ? $"WhatsApp enviado para {customer.Name}."
            : $"WhatsApp aberto para {customer.Name}.");
    }

    private string BuildCustomerWhatsAppMessage(Customer customer)
    {
        var firstName = FirstName(customer.Name);
        return $"Oi {firstName}, aqui é da {BusinessDisplayName()}. Tudo bem? Posso te ajudar com um horário ou alguma informação?";
    }

    private Border CreateCustomerHistoryRow(Appointment appointment)
    {
        var row = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel();
        textStack.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:dd/MM HH:mm} - {appointment.ServiceName}",
            Foreground = InkBrush,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = FirstFilled(appointment.ProfessionalName, appointment.ResourceName, "Sem profissional"),
            Foreground = MutedBrush,
            FontSize = 10.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        grid.Children.Add(textStack);

        var status = new Border
        {
            Background = ScheduleCardBackground(appointment.Status),
            BorderBrush = ScheduleAccentFor(appointment.Status),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = ScheduleStatusLabel(appointment.Status),
                Foreground = ScheduleAccentFor(appointment.Status),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(status, 1);
        grid.Children.Add(status);

        row.Child = grid;
        return row;
    }

    private void RefreshEstablishmentProfessionals()
    {
        _establishmentProfessionals.Clear();
        foreach (var professional in _data.Professionals.Where(item => item.IsActive).OrderBy(item => item.Name).Take(4))
        {
            _establishmentProfessionals.Add(new EstablishmentListRow(
                professional.Name,
                professional.SegmentLine,
                string.IsNullOrWhiteSpace(professional.Role) ? "Equipe" : professional.Role,
                AccentSoftBrush,
                AccentBrush,
                professional.Id,
                PackIconKind.AccountTie));
        }

        if (_establishmentProfessionals.Count == 0)
        {
            _establishmentProfessionals.Add(EmptyEstablishmentRow("Nenhum profissional cadastrado", "Cadastre a equipe para montar a agenda.", "0"));
        }
    }

    private void EstablishmentProfessionalsItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var row = FindDataContext<EstablishmentListRow>(e.OriginalSource as DependencyObject);
        if (row is null || string.IsNullOrWhiteSpace(row.Id))
        {
            return;
        }

        var professional = _data.Professionals.FirstOrDefault(item => item.Id.Equals(row.Id, StringComparison.OrdinalIgnoreCase));
        if (professional is null)
        {
            ShowStatus("Profissional não encontrado.");
            return;
        }

        ShowProfessionalInfoPopup(professional);
        e.Handled = true;
    }

    private void ShowProfessionalInfoPopup(Professional professional)
    {
        CloseProfessionalInfoPopup();
        CloseCustomerInfoPopup();
        CloseServiceInfoPopup();

        var popup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = 12,
            VerticalOffset = 10,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true
        };

        popup.Child = CreateProfessionalInfoPopupContent(professional);
        _professionalInfoPopup = popup;
        popup.Closed += (_, _) => DetachProfessionalInfoOutsideClickHandler();
        popup.IsOpen = true;
        AttachProfessionalInfoOutsideClickHandler();
    }

    private void AttachProfessionalInfoOutsideClickHandler()
    {
        DetachProfessionalInfoOutsideClickHandler();

        _professionalInfoOutsideClickHandler = (_, args) =>
        {
            if (_professionalInfoPopup?.Child is DependencyObject popupChild &&
                IsDescendantOf(args.OriginalSource as DependencyObject, popupChild))
            {
                return;
            }

            CloseProfessionalInfoPopup();
        };

        Dispatcher.BeginInvoke(() =>
        {
            if (_professionalInfoPopup?.IsOpen == true && _professionalInfoOutsideClickHandler is not null)
            {
                PreviewMouseDown += _professionalInfoOutsideClickHandler;
            }
        }, DispatcherPriority.Background);
    }

    private void DetachProfessionalInfoOutsideClickHandler()
    {
        if (_professionalInfoOutsideClickHandler is null)
        {
            return;
        }

        PreviewMouseDown -= _professionalInfoOutsideClickHandler;
        _professionalInfoOutsideClickHandler = null;
    }

    private void CloseProfessionalInfoPopup()
    {
        DetachProfessionalInfoOutsideClickHandler();

        if (_professionalInfoPopup is null)
        {
            return;
        }

        var popup = _professionalInfoPopup;
        _professionalInfoPopup = null;
        popup.IsOpen = false;
    }

    private Border CreateProfessionalInfoPopupContent(Professional professional)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var appointments = _data.Appointments.Where(item => ProfessionalMatches(item, professional)).ToList();
        var todayAppointments = appointments.Count(item => item.Start >= today && item.Start < tomorrow && item.Status != AppointmentStatus.Blocked);
        var monthAppointments = appointments.Count(item => item.Start >= monthStart && item.Start < nextMonth && item.Status != AppointmentStatus.Blocked);
        var monthRevenue = appointments
            .Where(item => item.Start >= monthStart && item.Start < nextMonth && item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);

        var card = new Border
        {
            Width = 352,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.16
            }
        };

        var body = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new Border
        {
            Width = 40,
            Height = 40,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(13),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new TextBlock
            {
                Text = InitialsFor(professional.Name),
                Foreground = AccentBrush,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = FirstFilled(professional.Name, "Profissional"),
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"{FirstFilled(professional.Role, "Equipe")} | {FirstFilled(string.Join(", ", professional.Segments), _data.Settings.BusinessSegment, "Sem segmento")}",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(titleStack, 1);
        header.Children.Add(titleStack);

        var closeButton = IconOnlyButton(PackIconKind.Close, 28);
        closeButton.Click += (_, _) => CloseProfessionalInfoPopup();
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);
        body.Children.Add(header);

        var stats = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        stats.Children.Add(CreateProfessionalStatCard("Hoje", todayAppointments.ToString(Brazil), PackIconKind.CalendarClock));
        var monthCard = CreateProfessionalStatCard("Mês", monthAppointments.ToString(Brazil), PackIconKind.AccountClock);
        Grid.SetColumn(monthCard, 2);
        stats.Children.Add(monthCard);
        var revenueCard = CreateProfessionalStatCard("Receita", monthRevenue.ToString("C0", Brazil), PackIconKind.Cash);
        Grid.SetColumn(revenueCard, 4);
        stats.Children.Add(revenueCard);
        body.Children.Add(stats);

        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.AccountOutline, "Telefone", FirstFilled(FormatPhone(professional.Phone), "Sem telefone cadastrado")));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.ClipboardText, "E-mail", FirstFilled(professional.Email, "Sem e-mail cadastrado")));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.CashMultiple, "Comissão", $"{professional.CommissionPercent:N2}%"));

        if (!string.IsNullOrWhiteSpace(professional.Document))
        {
            body.Children.Add(CreateAppointmentInfoRow(PackIconKind.ClipboardText, "Documento", professional.Document));
        }

        if (!string.IsNullOrWhiteSpace(professional.Notes))
        {
            body.Children.Add(CreateAppointmentInfoRow(PackIconKind.Pencil, "Observações", professional.Notes));
        }

        body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 4, 0, 10)
        });

        body.Children.Add(new TextBlock
        {
            Text = "Próximos horários",
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var upcoming = appointments
            .Where(item => item.Start >= DateTime.Now && item.Status != AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .Take(3)
            .ToList();

        if (upcoming.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = "Nenhum horário futuro para este profissional.",
                Foreground = MutedBrush,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var appointment in upcoming)
            {
                body.Children.Add(CreateProfessionalAppointmentRow(appointment));
            }
        }

        body.Children.Add(CreateProfessionalPopupActions(professional));
        card.Child = body;
        return card;
    }

    private Border CreateProfessionalStatCard(string label, string value, PackIconKind icon)
    {
        var card = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(9, 8, 9, 8)
        };

        var stack = new StackPanel();
        stack.Children.Add(new PackIcon
        {
            Kind = icon,
            Width = 15,
            Height = 15,
            Foreground = AccentBrush,
            Margin = new Thickness(0, 0, 0, 5)
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 10,
            Margin = new Thickness(0, 1, 0, 0)
        });
        card.Child = stack;
        return card;
    }

    private Border CreateProfessionalAppointmentRow(Appointment appointment)
    {
        var row = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel();
        textStack.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:dd/MM HH:mm} - {appointment.CustomerName}",
            Foreground = InkBrush,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = FirstFilled(appointment.ServiceName, appointment.ResourceName, "Atendimento"),
            Foreground = MutedBrush,
            FontSize = 10.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        grid.Children.Add(textStack);

        var status = new Border
        {
            Background = ScheduleCardBackground(appointment.Status),
            BorderBrush = ScheduleAccentFor(appointment.Status),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = ScheduleStatusLabel(appointment.Status),
                Foreground = ScheduleAccentFor(appointment.Status),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(status, 1);
        grid.Children.Add(status);

        row.Child = grid;
        return row;
    }

    private Grid CreateProfessionalPopupActions(Professional professional)
    {
        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var editIcon = new PackIcon
        {
            Kind = PackIconKind.Pencil,
            Width = 15,
            Height = 15,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        BindForegroundToButton(editIcon);

        var editText = new TextBlock
        {
            Text = "Editar",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindForegroundToButton(editText);

        var editButton = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Height = 34,
            Padding = new Thickness(10, 0, 10, 0),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    editIcon,
                    editText
                }
            }
        };
        editButton.Click += (_, _) =>
        {
            CloseProfessionalInfoPopup();
            EditProfessional(professional.Id);
        };
        actions.Children.Add(editButton);

        var whatsAppButton = new Button
        {
            Style = (Style)FindResource("CommandButton"),
            Height = 34,
            Padding = new Thickness(10, 0, 10, 0),
            Background = Solid("#16A34A"),
            BorderBrush = Solid("#16A34A"),
            Foreground = Brushes.White,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new PackIcon
                    {
                        Kind = PackIconKind.Whatsapp,
                        Width = 15,
                        Height = 15,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    },
                    new TextBlock
                    {
                        Text = "WhatsApp",
                        Foreground = Brushes.White,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        whatsAppButton.Click += async (_, _) => await SendProfessionalWhatsAppAsync(professional);
        Grid.SetColumn(whatsAppButton, 2);
        actions.Children.Add(whatsAppButton);

        return actions;
    }

    private async Task SendProfessionalWhatsAppAsync(Professional professional)
    {
        if (string.IsNullOrWhiteSpace(professional.Phone))
        {
            ShowStatus($"Telefone não cadastrado para {professional.Name}.");
            return;
        }

        var phone = NormalizeBrazilPhone(professional.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus($"Telefone inválido para {professional.Name}.");
            return;
        }

        CloseProfessionalInfoPopup();
        var sent = await SendOrOpenWhatsAppAsync(professional.Name, phone, BuildProfessionalWhatsAppMessage(professional), "Equipe");
        ShowStatus(sent
            ? $"WhatsApp enviado para {professional.Name}."
            : $"WhatsApp aberto para {professional.Name}.");
    }

    private string BuildProfessionalWhatsAppMessage(Professional professional)
    {
        var firstName = FirstName(professional.Name);
        return $"Oi {firstName}, aqui é da {BusinessDisplayName()}. Tudo bem? Preciso falar com você sobre a agenda.";
    }

    private static bool ProfessionalMatches(Appointment appointment, Professional professional) =>
        (!string.IsNullOrWhiteSpace(professional.Id) &&
         !string.IsNullOrWhiteSpace(appointment.ProfessionalId) &&
         appointment.ProfessionalId.Equals(professional.Id, StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(professional.Name) &&
         appointment.ProfessionalName.Equals(professional.Name, StringComparison.OrdinalIgnoreCase));

    private static void BindForegroundToButton(PackIcon icon) =>
        icon.SetBinding(Control.ForegroundProperty, new Binding(nameof(Control.Foreground))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });

    private static void BindForegroundToButton(TextBlock textBlock) =>
        textBlock.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Control.Foreground))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });

    private void RefreshEstablishmentServices()
    {
        _establishmentServices.Clear();
        foreach (var service in _data.Services
            .Select((item, index) => new { item, index })
            .Where(row => row.item.IsActive)
            .OrderByDescending(row => row.index)
            .Take(4)
            .Select(row => row.item))
        {
            _establishmentServices.Add(new EstablishmentListRow(
                service.Name,
                $"{service.DurationMinutes} min",
                service.Price.ToString("C", Brazil),
                AccentSoftBrush,
                AccentBrush,
                service.Id,
                Icon: PackIconKind.ClipboardText));
        }

        if (_establishmentServices.Count == 0)
        {
            _establishmentServices.Add(EmptyEstablishmentRow("Nenhum serviço cadastrado", "Crie serviços para montar os agendamentos.", "0"));
        }
    }

    private void EstablishmentServicesItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var row = FindDataContext<EstablishmentListRow>(e.OriginalSource as DependencyObject);
        if (row is null || string.IsNullOrWhiteSpace(row.Id))
        {
            return;
        }

        var service = _data.Services.FirstOrDefault(item => item.Id.Equals(row.Id, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            ShowStatus("Serviço não encontrado.");
            return;
        }

        ShowServiceInfoPopup(service);
        e.Handled = true;
    }

    private void ShowServiceInfoPopup(ServiceItem service)
    {
        CloseServiceInfoPopup();
        CloseCustomerInfoPopup();
        CloseProfessionalInfoPopup();

        var popup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = 12,
            VerticalOffset = 10,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true
        };

        popup.Child = CreateServiceInfoPopupContent(service);
        _serviceInfoPopup = popup;
        popup.Closed += (_, _) => DetachServiceInfoOutsideClickHandler();
        popup.IsOpen = true;
        AttachServiceInfoOutsideClickHandler();
    }

    private void AttachServiceInfoOutsideClickHandler()
    {
        DetachServiceInfoOutsideClickHandler();

        _serviceInfoOutsideClickHandler = (_, args) =>
        {
            if (_serviceInfoPopup?.Child is DependencyObject popupChild &&
                IsDescendantOf(args.OriginalSource as DependencyObject, popupChild))
            {
                return;
            }

            CloseServiceInfoPopup();
        };

        Dispatcher.BeginInvoke(() =>
        {
            if (_serviceInfoPopup?.IsOpen == true && _serviceInfoOutsideClickHandler is not null)
            {
                PreviewMouseDown += _serviceInfoOutsideClickHandler;
            }
        }, DispatcherPriority.Background);
    }

    private void DetachServiceInfoOutsideClickHandler()
    {
        if (_serviceInfoOutsideClickHandler is null)
        {
            return;
        }

        PreviewMouseDown -= _serviceInfoOutsideClickHandler;
        _serviceInfoOutsideClickHandler = null;
    }

    private void CloseServiceInfoPopup()
    {
        DetachServiceInfoOutsideClickHandler();

        if (_serviceInfoPopup is null)
        {
            return;
        }

        var popup = _serviceInfoPopup;
        _serviceInfoPopup = null;
        popup.IsOpen = false;
    }

    private Border CreateServiceInfoPopupContent(ServiceItem service)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var appointments = _data.Appointments.Where(item => ServiceMatches(item, service)).ToList();
        var monthAppointments = appointments.Count(item => item.Start >= monthStart && item.Start < nextMonth && item.Status != AppointmentStatus.Blocked);
        var doneRevenue = appointments
            .Where(item => item.Start >= monthStart && item.Start < nextMonth && item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);

        var card = new Border
        {
            Width = 352,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.16
            }
        };

        var body = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new Border
        {
            Width = 40,
            Height = 40,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(13),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.ClipboardText,
                Width = 20,
                Height = 20,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = FirstFilled(service.Name, "Serviço"),
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"{FirstFilled(service.Category, "Atendimento")} | {FirstFilled(service.Segment, _data.Settings.BusinessSegment, "Sem segmento")}",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(titleStack, 1);
        header.Children.Add(titleStack);

        var closeButton = IconOnlyButton(PackIconKind.Close, 28);
        closeButton.Click += (_, _) => CloseServiceInfoPopup();
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);
        body.Children.Add(header);

        var stats = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        stats.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        stats.Children.Add(CreateProfessionalStatCard("Duração", $"{service.DurationMinutes} min", PackIconKind.ClockOutline));
        var priceCard = CreateProfessionalStatCard("Preço", service.Price.ToString("C0", Brazil), PackIconKind.Cash);
        Grid.SetColumn(priceCard, 2);
        stats.Children.Add(priceCard);
        var monthCard = CreateProfessionalStatCard("Mês", monthAppointments.ToString(Brazil), PackIconKind.CalendarClock);
        Grid.SetColumn(monthCard, 4);
        stats.Children.Add(monthCard);
        body.Children.Add(stats);

        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.CashMultiple, "Receita finalizada no mês", doneRevenue.ToString("C", Brazil)));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.Seat, "Local padrão", FirstFilled(service.DefaultResource, "Sem local padrão")));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.ClockOutline, "Preparação e intervalo", $"{service.PreparationMinutes} min antes | {service.BufferMinutes} min após"));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.CashCheck, "Comissão padrão", $"{service.CommissionPercent:N2}%"));

        if (!string.IsNullOrWhiteSpace(service.Description))
        {
            body.Children.Add(CreateAppointmentInfoRow(PackIconKind.Pencil, "Descrição", service.Description));
        }

        body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 4, 0, 10)
        });

        body.Children.Add(new TextBlock
        {
            Text = "Próximos atendimentos",
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var upcoming = appointments
            .Where(item => item.Start >= DateTime.Now && item.Status != AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .Take(3)
            .ToList();

        if (upcoming.Count == 0)
        {
            body.Children.Add(new TextBlock
            {
                Text = "Nenhum atendimento futuro com este serviço.",
                Foreground = MutedBrush,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var appointment in upcoming)
            {
                body.Children.Add(CreateServiceAppointmentRow(appointment));
            }
        }

        body.Children.Add(CreateServicePopupActions(service));
        card.Child = body;
        return card;
    }

    private Border CreateServiceAppointmentRow(Appointment appointment)
    {
        var row = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel();
        textStack.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:dd/MM HH:mm} - {appointment.CustomerName}",
            Foreground = InkBrush,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = FirstFilled(appointment.ProfessionalName, appointment.ResourceName, "Sem profissional"),
            Foreground = MutedBrush,
            FontSize = 10.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        grid.Children.Add(textStack);

        var status = new Border
        {
            Background = ScheduleCardBackground(appointment.Status),
            BorderBrush = ScheduleAccentFor(appointment.Status),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = ScheduleStatusLabel(appointment.Status),
                Foreground = ScheduleAccentFor(appointment.Status),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(status, 1);
        grid.Children.Add(status);

        row.Child = grid;
        return row;
    }

    private Grid CreateServicePopupActions(ServiceItem service)
    {
        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var editIcon = new PackIcon
        {
            Kind = PackIconKind.Pencil,
            Width = 15,
            Height = 15,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        BindForegroundToButton(editIcon);

        var editText = new TextBlock
        {
            Text = "Editar serviço",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindForegroundToButton(editText);

        var editButton = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Height = 34,
            Padding = new Thickness(10, 0, 10, 0),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    editIcon,
                    editText
                }
            }
        };
        editButton.Click += (_, _) =>
        {
            CloseServiceInfoPopup();
            EditService(service.Id);
        };
        actions.Children.Add(editButton);
        return actions;
    }

    private static bool ServiceMatches(Appointment appointment, ServiceItem service) =>
        (!string.IsNullOrWhiteSpace(service.Id) &&
         !string.IsNullOrWhiteSpace(appointment.ServiceId) &&
         appointment.ServiceId.Equals(service.Id, StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrWhiteSpace(service.Name) &&
         appointment.ServiceName.Equals(service.Name, StringComparison.OrdinalIgnoreCase));

    private void RefreshEstablishmentProducts()
    {
        _establishmentProducts.Clear();
        foreach (var product in _data.Products.OrderBy(item => item.Name))
        {
            var detail = string.IsNullOrWhiteSpace(product.Category)
                ? $"{product.StockQuantity} em estoque"
                : $"{product.Category} | {product.StockQuantity} em estoque";
            if (!string.IsNullOrWhiteSpace(product.Sku))
            {
                detail += $" | SKU {product.Sku}";
            }

            if (product.MinimumStock > 0)
            {
                detail += $" | mín. {product.MinimumStock}";
            }
            _establishmentProducts.Add(new EstablishmentListRow(
                product.Name,
                detail,
                product.Price.ToString("C0", Brazil),
                AccentSoftBrush,
                AccentBrush));
        }

        if (_establishmentProducts.Count == 0)
        {
            _establishmentProducts.Add(EmptyEstablishmentRow("Nenhum produto cadastrado", "Os produtos de venda aparecerão aqui.", "0"));
        }
    }

    private void RefreshEstablishmentSales()
    {
        _establishmentSales.Clear();
        foreach (var sale in _data.ProductSales.OrderByDescending(item => item.SoldAt).Take(8))
        {
            var customer = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Venda avulsa" : sale.CustomerName;
            _establishmentSales.Add(new EstablishmentListRow(
                sale.ProductName,
                $"{sale.SoldAt:dd/MM HH:mm} | {customer} | {sale.Quantity} un.",
                sale.Total.ToString("C0", Brazil),
                WarmSoftBrush,
                InkBrush));
        }

        if (_establishmentSales.Count == 0)
        {
            _establishmentSales.Add(EmptyEstablishmentRow("Nenhuma venda registrada", "As vendas de produtos aparecerão nesta lista.", "R$ 0"));
        }
    }

    private static EstablishmentListRow EmptyEstablishmentRow(string name, string detail, string badge) =>
        new(name, detail, badge, GraySoftBrush, MutedBrush);

    private void RefreshFinancePage()
    {
        FinanceBusinessText.Text = BusinessDisplayName();

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var serviceToday = SumServiceRevenue(today, tomorrow);
        var productToday = SumProductRevenue(today, tomorrow);
        var manualToday = SumManualPayments(today, tomorrow);
        var serviceMonth = SumServiceRevenue(monthStart, nextMonth);
        var productMonth = SumProductRevenue(monthStart, nextMonth);
        var manualMonth = SumManualPayments(monthStart, nextMonth);
        var receivedToday = serviceToday + productToday + manualToday;
        var receivedMonth = serviceMonth + productMonth + manualMonth;
        var pending = PendingPaymentAppointments().ToList();
        var pendingValue = pending.Sum(item => item.Price);
        var monthExpenses = _data.Expenses
            .Where(item => item.Date >= monthStart && item.Date < nextMonth)
            .Sum(item => item.Value);
        var monthBalance = receivedMonth - monthExpenses;
        var pendingLabel = pending.Count == 1 ? "1 atendimento em aberto" : $"{pending.Count} atendimentos em aberto";

        var balancePositive = monthBalance > 0;
        var balanceNegative = monthBalance < 0;
        var balanceForeground = balancePositive
            ? Solid("#166534")
            : balanceNegative ? Solid("#991B1B") : AccentBrush;

        FinanceBalanceText.Text = monthBalance.ToString("C", Brazil);
        FinanceBalanceText.Foreground = Brushes.White;
        FinanceBalanceHintText.Text = balancePositive
            ? "Acima das despesas"
            : balanceNegative ? "Despesas acima das entradas" : "Sem movimentação no período";
        FinanceBalanceBadgeText.Text = balancePositive ? "Positivo" : balanceNegative ? "Negativo" : "Neutro";
        FinanceBalanceBadgeText.Foreground = balanceForeground;
        FinanceBalanceBadgeBorder.Background = balancePositive
            ? Solid("#DCFCE7")
            : balanceNegative ? Solid("#FEE2E2") : AccentSoftBrush;
        FinanceBalanceAccentBorder.Background = balancePositive
            ? Solid("#16A34A")
            : balanceNegative ? Solid("#DC2626") : AccentBrush;
        FinanceReceivedMonthText.Text = receivedMonth.ToString("C", Brazil);
        FinanceExpensesMonthText.Text = monthExpenses.ToString("C", Brazil);
        FinancePendingTotalText.Text = pendingValue.ToString("C", Brazil);
        FinancePendingHintText.Text = pendingLabel;
        FinanceMercadoPagoText.Text = IsMercadoPagoPointReady()
            ? MercadoPagoTerminalLabel()
            : _data.Settings.MercadoPagoEnabled ? "Falta conectar Point" : "Desativado";
        FinanceMercadoPagoText.Foreground = IsMercadoPagoPointReady()
            ? Solid("#166534")
            : _data.Settings.MercadoPagoEnabled ? Solid("#B45309") : MutedBrush;
        FinanceMercadoPagoHintText.Text = IsMercadoPagoPointReady()
            ? "Crédito e débito podem ir direto para a maquininha."
            : "Ative em Configurações para liberar cartão na maquininha.";

        _financeEntries.Clear();
        var maxEntry = Math.Max(1m, Math.Max(serviceMonth, Math.Max(productMonth, manualMonth)));
        _financeEntries.Add(new HomeFinanceBarRow("Serviços finalizados", serviceMonth.ToString("C", Brazil), Percent(serviceMonth, maxEntry)));
        _financeEntries.Add(new HomeFinanceBarRow("Produtos vendidos", productMonth.ToString("C", Brazil), Percent(productMonth, maxEntry)));
        _financeEntries.Add(new HomeFinanceBarRow("Recebimentos avulsos", manualMonth.ToString("C", Brazil), Percent(manualMonth, maxEntry)));

        RefreshFinancePendingPayments(pending);
        RefreshFinanceExpenses();
        RefreshFinanceChart(today);
    }

    private void RefreshFinancePendingPayments(IReadOnlyList<Appointment> pending)
    {
        _financePendingPayments.Clear();
        foreach (var appointment in pending.OrderBy(item => item.Start).Take(8))
        {
            _financePendingPayments.Add(new EstablishmentListRow(
                appointment.CustomerName,
                $"{appointment.Start:dd/MM HH:mm} | {appointment.ServiceName}",
                appointment.Price.ToString("C", Brazil),
                YellowSoftBrush,
                InkBrush,
                Icon: PackIconKind.AccountOutline));
        }

        if (_financePendingPayments.Count == 0)
        {
            _financePendingPayments.Add(new EstablishmentListRow(
                "Sem cobrança pendente",
                "Tudo certo no caixa.",
                0m.ToString("C", Brazil),
                AccentSoftBrush,
                AccentBrush,
                Icon: PackIconKind.AccountOutline));
        }
    }

    private void RefreshFinanceExpenses()
    {
        _financeExpenses.Clear();
        foreach (var expense in _data.Expenses.OrderByDescending(item => item.Date).Take(8))
        {
            var category = string.IsNullOrWhiteSpace(expense.Category) ? "Despesa" : expense.Category;
            var status = expense.IsPaid ? "pago" : "pendente";
            _financeExpenses.Add(new EstablishmentListRow(
                expense.Description,
                $"{expense.Date:dd/MM} | {category} | {status}",
                expense.Value.ToString("C", Brazil),
                RedSoftBrush,
                InkBrush,
                Icon: PackIconKind.ReceiptText));
        }

        if (_financeExpenses.Count == 0)
        {
            _financeExpenses.Add(new EstablishmentListRow(
                "Sem gastos lançados",
                "Despesas aparecerão aqui.",
                0m.ToString("C", Brazil),
                RedSoftBrush,
                Solid("#DC2626"),
                Icon: PackIconKind.ReceiptText));
        }
    }

    private void RefreshFinanceChart(DateTime today)
    {
        _financeChartRows.Clear();
        var days = Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(offset - 6))
            .Select(day => new
            {
                Day = day,
                Value = SumReceivedRevenue(day, day.AddDays(1)),
                Payments = ReceivedPaymentRows(day, day.AddDays(1))
            })
            .ToList();
        var max = Math.Max(1m, days.Max(item => item.Value));
        var total = days.Sum(item => item.Value);
        var average = days.Count > 0 ? total / days.Count : 0m;
        var best = days.Count > 0 ? days.Max(item => item.Value) : 0m;

        foreach (var day in days)
        {
            _financeChartRows.Add(new HomeFinanceBarRow(
                day.Day.ToString("ddd, dd/MM", Brazil),
                day.Value.ToString("C0", Brazil),
                Percent(day.Value, max)));
        }

        const double chartWidth = 600d;
        const double chartBaseY = 52d;
        const double chartUsableHeight = 42d;
        var points = new PointCollection();
        CloseFinanceChartInfoPopup();
        FinanceChartPointLayer.Children.Clear();
        for (var index = 0; index < days.Count; index++)
        {
            var x = days.Count <= 1 ? 0d : index * chartWidth / (days.Count - 1);
            var y = chartBaseY - Math.Min(100d, Percent(days[index].Value, max)) / 100d * chartUsableHeight;
            points.Add(new Point(x, y));
            AddFinanceChartCustomerLabel(x, y, days[index].Value, days[index].Payments);
        }

        FinanceChartLine.Points = points;
        FinanceChartTotalText.Text = total.ToString("C", Brazil);
        FinanceChartAverageText.Text = average.ToString("C", Brazil);
        FinanceChartBestText.Text = best.ToString("C", Brazil);
    }

    private IReadOnlyList<FinanceChartPaymentRow> ReceivedPaymentRows(DateTime start, DateTime end)
    {
        var payments = _data.Appointments
            .Where(item => item.Start >= start && item.Start < end && item.Status == AppointmentStatus.Done && item.Price > 0)
            .Select(item => new FinanceChartPaymentRow(
                ToNameCase(FirstFilled(item.CustomerName, "Cliente")),
                FirstFilled(item.ServiceName, "Atendimento finalizado"),
                item.Price))
            .Concat(_data.ProductSales
                .Where(item => item.SoldAt >= start && item.SoldAt < end && item.Total > 0)
                .Select(item => new FinanceChartPaymentRow(
                    ToNameCase(FirstFilled(item.CustomerName, "Cliente")),
                    FirstFilled(item.ProductName, "Produto vendido"),
                    item.Total)))
            .Concat(_data.ManualPayments
                .Where(item => item.PaidAt >= start && item.PaidAt < end && item.Value > 0)
                .Select(item => new FinanceChartPaymentRow(
                    ToNameCase(FirstFilled(item.CustomerName, "Recebimento avulso")),
                    FirstFilled(item.Description, item.Category, "Recebimento avulso"),
                    item.Value)))
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return payments;
    }

    private void AddFinanceChartCustomerLabel(double x, double y, decimal value, IReadOnlyList<FinanceChartPaymentRow> payments)
    {
        if (value <= 0 || payments.Count == 0)
        {
            return;
        }

        var hoverTarget = new Ellipse
        {
            Width = 28,
            Height = 28,
            Fill = Brushes.Transparent,
            Cursor = Cursors.Hand
        };
        hoverTarget.MouseEnter += (_, _) => ShowFinanceChartInfoPopup(hoverTarget, value, payments);
        hoverTarget.MouseLeave += (_, _) => CloseFinanceChartInfoPopup();
        hoverTarget.Unloaded += (_, _) => CloseFinanceChartInfoPopup();
        Canvas.SetLeft(hoverTarget, x - 14);
        Canvas.SetTop(hoverTarget, y - 14);
        Panel.SetZIndex(hoverTarget, 3);
        FinanceChartPointLayer.Children.Add(hoverTarget);

        var marker = new Ellipse
        {
            Width = 9,
            Height = 9,
            Fill = AccentBrush,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(marker, x - 4.5);
        Canvas.SetTop(marker, y - 4.5);
        Panel.SetZIndex(marker, 4);
        FinanceChartPointLayer.Children.Add(marker);
    }

    private void ShowFinanceChartInfoPopup(
        FrameworkElement placementTarget,
        decimal total,
        IReadOnlyList<FinanceChartPaymentRow> payments)
    {
        CloseFinanceChartInfoPopup();

        var popup = new Popup
        {
            PlacementTarget = placementTarget,
            Placement = PlacementMode.Right,
            HorizontalOffset = 10,
            VerticalOffset = -10,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true,
            IsHitTestVisible = false
        };

        popup.Child = CreateFinanceChartInfoPopupContent(total, payments);
        _financeChartInfoPopup = popup;
        popup.IsOpen = true;
    }

    private Border CreateFinanceChartInfoPopupContent(decimal total, IReadOnlyList<FinanceChartPaymentRow> payments)
    {
        var card = new Border
        {
            Width = 268,
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Padding = new Thickness(14),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 24,
                ShadowDepth = 4,
                Opacity = 0.14
            }
        };

        var body = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new PackIcon
        {
            Kind = PackIconKind.CashCheck,
            Width = 15,
            Height = 15,
            Foreground = AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Children.Add(new Border
        {
            Width = 30,
            Height = 30,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 8, 0),
            Child = icon
        });

        var titleText = new TextBlock
        {
            Text = "Pagamentos do dia",
            Foreground = InkBrush,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 1);
        header.Children.Add(titleText);

        var totalText = new TextBlock
        {
            Text = total.ToString("C", Brazil),
            Foreground = AccentBrush,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(totalText, 2);
        header.Children.Add(totalText);
        body.Children.Add(header);

        foreach (var payment in payments.Take(5))
        {
            body.Children.Add(CreateFinanceChartPaymentPopupRow(payment));
        }

        if (payments.Count > 5)
        {
            body.Children.Add(new TextBlock
            {
                Text = $"+ {payments.Count - 5} pagamento(s)",
                Foreground = MutedBrush,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        card.Child = body;
        return card;
    }

    private Grid CreateFinanceChartPaymentPopupRow(FinanceChartPaymentRow payment)
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        row.Children.Add(new Border
        {
            Width = 18,
            Height = 18,
            Background = Solid("#DCFCE7"),
            CornerRadius = new CornerRadius(9),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new PackIcon
            {
                Kind = PackIconKind.CashCheck,
                Width = 11,
                Height = 11,
                Foreground = Solid("#16A34A"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var textStack = new StackPanel();
        textStack.Children.Add(new TextBlock
        {
            Text = payment.Name,
            Foreground = InkBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = payment.Detail,
            Foreground = MutedBrush,
            FontSize = 10,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(textStack, 1);
        row.Children.Add(textStack);

        var valueText = new TextBlock
        {
            Text = payment.Value.ToString("C", Brazil),
            Foreground = InkBrush,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(valueText, 2);
        row.Children.Add(valueText);

        return row;
    }

    private void CloseFinanceChartInfoPopup()
    {
        if (_financeChartInfoPopup is null)
        {
            return;
        }

        var popup = _financeChartInfoPopup;
        _financeChartInfoPopup = null;
        popup.IsOpen = false;
    }

    private IEnumerable<Appointment> PendingPaymentAppointments() =>
        _data.Appointments.Where(item =>
            item.Price > 0 &&
            item.Status is not AppointmentStatus.Done
                and not AppointmentStatus.Cancelled
                and not AppointmentStatus.NoShow
                and not AppointmentStatus.Blocked);

    private decimal SumReceivedRevenue(DateTime start, DateTime end) =>
        SumServiceRevenue(start, end) + SumProductRevenue(start, end) + SumManualPayments(start, end);

    private decimal SumServiceRevenue(DateTime start, DateTime end) =>
        _data.Appointments
            .Where(item => item.Start >= start && item.Start < end && item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);

    private decimal SumProductRevenue(DateTime start, DateTime end) =>
        _data.ProductSales
            .Where(item => item.SoldAt >= start && item.SoldAt < end)
            .Sum(item => item.Total);

    private decimal SumManualPayments(DateTime start, DateTime end) =>
        _data.ManualPayments
            .Where(item => item.PaidAt >= start && item.PaidAt < end)
            .Sum(item => item.Value);

    private void RefreshReportsPage()
    {
        var reportDate = _selectedDate.Date;
        var periodStart = reportDate.AddDays(-6);
        var periodEnd = reportDate.AddDays(1);
        ReportsPeriodText.Text = $"{periodStart:dd/MM} a {reportDate:dd/MM}";

        var appointments = ReportPeriodAppointments(periodStart, periodEnd);
        var finalizados = appointments.Count(item => item.Status == AppointmentStatus.Done);
        var canceladosFaltas = appointments.Count(item => item.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow);
        var receitaServicos = SumServiceRevenue(periodStart, periodEnd);
        var receita = SumReceivedRevenue(periodStart, periodEnd);
        var ticketMedio = finalizados > 0 ? receitaServicos / finalizados : 0m;
        var taxaConclusao = appointments.Count > 0 ? finalizados * 100m / appointments.Count : 0m;

        _reportsMetrics.Clear();
        _reportsMetrics.Add(new EstablishmentMetricRow("Agendamentos", appointments.Count.ToString(Brazil), "total do período", AccentSoftBrush, PackIconKind.CalendarMonth, AccentBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Finalizados", finalizados.ToString(Brazil), "concluídos", BlueSoftBrush, PackIconKind.CheckCircleOutline, Solid("#16A34A")));
        _reportsMetrics.Add(new EstablishmentMetricRow("Cancelados/faltas", canceladosFaltas.ToString(Brazil), "perdas", canceladosFaltas > 0 ? RedSoftBrush : GraySoftBrush, PackIconKind.AlertCircleOutline, Solid("#DC2626")));
        _reportsMetrics.Add(new EstablishmentMetricRow("Receita", receita.ToString("C0", Brazil), "entradas", WarmSoftBrush, PackIconKind.WalletOutline, AccentBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Ticket médio", ticketMedio.ToString("C0", Brazil), "por finalizado", BlueSoftBrush, PackIconKind.CashMultiple, AccentBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Conclusão", $"{taxaConclusao:N0}%", "sobre o total", YellowSoftBrush, PackIconKind.ChartDonut, Solid("#F59E0B")));

        RefreshReportsInsights(periodStart, periodEnd, appointments, ticketMedio, taxaConclusao);
        RefreshReportsChart(periodStart, reportDate, appointments);
        RefreshReportsServices(appointments);
        RefreshReportsProfessionals(appointments);
    }

    private List<Appointment> ReportPeriodAppointments(DateTime periodStart, DateTime periodEnd)
    {
        var realAppointments = _data.Appointments
            .Where(item => item.Start >= periodStart && item.Start < periodEnd)
            .Where(item => item.Status != AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.CustomerName)
            .ToList();

        return realAppointments;
    }

    private void RefreshReportsChart(DateTime periodStart, DateTime today, IReadOnlyList<Appointment> appointments)
    {
        var option = CurrentReportChartOption();
        ReportsChartTitleText.Text = option;

        switch (option)
        {
            case ReportChartRevenue:
                ReportsChartSubtitleText.Text = "Entradas por dia, incluindo serviços, produtos e pagamentos.";
                SetReportsChartRows(
                    ReportChartStyle.Line,
                    Enumerable.Range(0, 7)
                        .Select(offset => periodStart.AddDays(offset))
                        .Select(day =>
                        {
                            var value = SumReceivedRevenue(day, day.AddDays(1));
                            return (Label: day.ToString("ddd, dd/MM", Brazil), Value: value, ValueText: value.ToString("C0", Brazil));
                        }),
                    keepZeroRows: true,
                    emptyLabel: "Sem receita no período");
                break;
            case ReportChartStatus:
                ReportsChartSubtitleText.Text = "Distribuição dos atendimentos por situação.";
                SetReportsChartRows(
                    ReportChartStyle.Donut,
                    appointments
                        .GroupBy(item => StatusLabel(item.Status))
                        .Select(group => (Label: group.Key, Value: (decimal)group.Count(), ValueText: $"{group.Count()} ag."))
                        .OrderByDescending(row => row.Value),
                    emptyLabel: "Sem agendamentos no período");
                break;
            default:
                ReportsChartSubtitleText.Text = "Volume dos últimos 7 dias.";
                SetReportsChartRows(
                    ReportChartStyle.Columns,
                    Enumerable.Range(0, 7)
                        .Select(offset => periodStart.AddDays(offset))
                        .Select(day =>
                        {
                            var count = appointments.Count(item => item.Start.Date == day.Date);
                            return (Label: day.ToString("ddd, dd/MM", Brazil), Value: (decimal)count, ValueText: $"{count} ag.");
                        }),
                    keepZeroRows: true,
                    emptyLabel: "Sem agendamentos no período");
                break;
        }
    }

    private void RefreshReportsInsights(
        DateTime start,
        DateTime end,
        IReadOnlyList<Appointment> appointments,
        decimal ticketMedio,
        decimal taxaConclusao)
    {
        _reportsInsights.Clear();

        var activeAppointments = appointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow)
            .ToList();
        var busyMinutes = activeAppointments.Sum(item => item.DurationMinutes);
        var professionalCount = Math.Max(1, _data.Professionals.Count);
        var workMinutesPerDay = Math.Max(1, _data.Settings.WorkdayEndHour - _data.Settings.WorkdayStartHour) * 60;
        var capacityMinutes = Math.Max(1, professionalCount * workMinutesPerDay * 7);
        var occupancy = Math.Min(100m, busyMinutes * 100m / capacityMinutes);
        var productSales = _data.ProductSales.Where(item => item.SoldAt >= start && item.SoldAt < end).ToList();
        var manualPayments = _data.ManualPayments.Where(item => item.PaidAt >= start && item.PaidAt < end).ToList();
        var customersServed = appointments
            .Where(item => item.Status == AppointmentStatus.Done && !string.IsNullOrWhiteSpace(item.CustomerName))
            .Select(item => item.CustomerName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var bestDay = appointments
            .GroupBy(item => item.Start.Date)
            .Select(group => new
            {
                Day = group.Key,
                Count = group.Count(),
                Revenue = SumReceivedRevenue(group.Key, group.Key.AddDays(1))
            })
            .OrderByDescending(item => item.Count)
            .ThenByDescending(item => item.Revenue)
            .FirstOrDefault();

        _reportsInsights.Add(new EstablishmentListRow(
            "Melhor dia",
            bestDay is null ? "Sem movimento no período" : $"{bestDay.Count} agendamento(s) | {bestDay.Revenue.ToString("C0", Brazil)}",
            bestDay is null ? "-" : bestDay.Day.ToString("ddd", Brazil),
            AccentSoftBrush,
            AccentBrush,
            Icon: PackIconKind.CalendarMonth));
        _reportsInsights.Add(new EstablishmentListRow(
            "Ocupação estimada",
            $"{busyMinutes / 60:N0}h ocupada(s) em {professionalCount} profissional(is)",
            $"{occupancy:N0}%",
            BlueSoftBrush,
            AccentBrush,
            Icon: PackIconKind.AccountTie));
        _reportsInsights.Add(new EstablishmentListRow(
            "Clientes atendidos",
            "Clientes únicos com atendimento finalizado",
            customersServed.ToString(Brazil),
            GraySoftBrush,
            InkBrush,
            Icon: PackIconKind.AccountGroup));
        _reportsInsights.Add(new EstablishmentListRow(
            "Produtos vendidos",
            $"{productSales.Count} venda(s) no período",
            productSales.Sum(item => item.Total).ToString("C0", Brazil),
            WarmSoftBrush,
            AccentBrush,
            Icon: PackIconKind.PackageVariant));
        _reportsInsights.Add(new EstablishmentListRow(
            "Pagamentos avulsos",
            $"{manualPayments.Count} recebimento(s) manual(is)",
            manualPayments.Sum(item => item.Value).ToString("C0", Brazil),
            AccentSoftBrush,
            AccentBrush,
            Icon: PackIconKind.WalletOutline));
        _reportsInsights.Add(new EstablishmentListRow(
            "Saúde da operação",
            $"Ticket médio {ticketMedio.ToString("C0", Brazil)} | conclusão {taxaConclusao:N0}%",
            taxaConclusao >= 70 ? "Boa" : "Atenção",
            taxaConclusao >= 70 ? BlueSoftBrush : YellowSoftBrush,
            taxaConclusao >= 70 ? AccentBrush : InkBrush,
            Icon: PackIconKind.CheckCircleOutline));
    }

    private string CurrentReportChartOption() =>
        ReportsChartTypeCombo.SelectedItem as string ?? ReportChartAppointments;

    private void SetReportsChartRows(
        ReportChartStyle style,
        IEnumerable<(string Label, decimal Value, string ValueText)> rows,
        bool keepZeroRows = false,
        string emptyLabel = "Sem dados")
    {
        var rawRows = rows
            .Where(row => keepZeroRows || row.Value > 0)
            .ToList();
        var max = rawRows.Count == 0 ? 1m : Math.Max(1m, rawRows.Max(row => row.Value));
        var chartRows = rawRows
            .Select((row, index) =>
            {
                var percent = row.Value <= 0 ? 0 : Math.Max(6, Percent(row.Value, max));
                var accent = style == ReportChartStyle.Columns ? AccentBrush : ReportPalette[index % ReportPalette.Length];
                var background = style == ReportChartStyle.Columns ? AccentSoftBrush : ReportSoftPalette[index % ReportSoftPalette.Length];
                return new ReportChartRow(
                    row.Label,
                    row.Value,
                    row.ValueText,
                    percent,
                    accent,
                    background);
            })
            .ToList();

        if (chartRows.Count == 0)
        {
            chartRows.Add(new ReportChartRow(emptyLabel, 0, "0", 0, AccentBrush, AccentSoftBrush));
        }

        _activeReportChartRows = chartRows;
        UpdateReportsChartSummary(style, chartRows);
        ApplyReportChartStyle(style, chartRows);
        UpdateReportChartModeButtons();
    }

    private void UpdateReportsChartSummary(ReportChartStyle style, IReadOnlyList<ReportChartRow> rows)
    {
        var total = rows.Sum(item => item.Value);
        var divisor = Math.Max(1, rows.Count);

        switch (style)
        {
            case ReportChartStyle.Line:
                ReportsChartTotalText.Text = $"Receita no período: {total.ToString("C0", Brazil)}";
                ReportsChartAverageText.Text = $"Média diária: {(total / divisor).ToString("C0", Brazil)}";
                break;
            case ReportChartStyle.Donut:
                var top = rows
                    .Where(item => item.Value > 0)
                    .OrderByDescending(item => item.Value)
                    .FirstOrDefault();
                ReportsChartTotalText.Text = $"Total por status: {total:N0}";
                ReportsChartAverageText.Text = top is null ? "Maior grupo: -" : $"Maior grupo: {top.Label}";
                break;
            default:
                ReportsChartTotalText.Text = $"Total de agendamentos no período: {total:N0}";
                var average = total / divisor;
                var averageText = average is > 0 and < 1
                    ? average.ToString("N1", Brazil)
                    : average.ToString("N0", Brazil);
                ReportsChartAverageText.Text = $"Média diária: {averageText}";
                break;
        }
    }

    private void ApplyReportChartStyle(ReportChartStyle style, IReadOnlyList<ReportChartRow> rows)
    {
        ReportsColumnChartItemsControl.Visibility = style == ReportChartStyle.Columns ? Visibility.Visible : Visibility.Collapsed;
        ReportsLineChartView.Visibility = style == ReportChartStyle.Line ? Visibility.Visible : Visibility.Collapsed;
        ReportsDonutChartView.Visibility = style == ReportChartStyle.Donut ? Visibility.Visible : Visibility.Collapsed;
        ReportsRankingChartView.Visibility = style == ReportChartStyle.Ranking ? Visibility.Visible : Visibility.Collapsed;

        _reportsColumnChartRows.Clear();
        _reportsLineChartRows.Clear();
        _reportsDonutChartRows.Clear();
        _reportsRankingChartRows.Clear();

        var target = style switch
        {
            ReportChartStyle.Line => _reportsLineChartRows,
            ReportChartStyle.Donut => _reportsDonutChartRows,
            ReportChartStyle.Ranking => _reportsRankingChartRows,
            _ => _reportsColumnChartRows
        };

        foreach (var row in rows)
        {
            target.Add(row);
        }

        DrawReportsLineChart();
        DrawReportsDonutChart();
    }

    private void UpdateReportChartModeButtons()
    {
        var current = CurrentReportChartOption();
        ReportAppointmentsChartButton.Style = (Style)FindResource(current == ReportChartAppointments ? "CommandButton" : "GhostButton");
        ReportRevenueChartButton.Style = (Style)FindResource(current == ReportChartRevenue ? "CommandButton" : "GhostButton");
        ReportStatusChartButton.Style = (Style)FindResource(current == ReportChartStatus ? "CommandButton" : "GhostButton");
    }

    private void ReportsLineChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawReportsLineChart();

    private void ReportsDonutCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawReportsDonutChart();

    private void DrawReportsLineChart()
    {
        ReportsLineChartCanvas.Children.Clear();
        if (ReportsLineChartView.Visibility != Visibility.Visible)
        {
            return;
        }

        var rows = _reportsLineChartRows.ToList();
        var width = Math.Max(420, ReportsLineChartCanvas.ActualWidth);
        var height = Math.Max(180, ReportsLineChartCanvas.ActualHeight);
        const double left = 38;
        const double top = 16;
        const double right = 18;
        const double bottom = 34;
        var chartWidth = Math.Max(1, width - left - right);
        var chartHeight = Math.Max(1, height - top - bottom);

        for (var i = 0; i <= 4; i++)
        {
            var y = top + chartHeight * i / 4;
            ReportsLineChartCanvas.Children.Add(new Line
            {
                X1 = left,
                X2 = left + chartWidth,
                Y1 = y,
                Y2 = y,
                Stroke = LineBrush,
                StrokeThickness = 1
            });
        }

        var realRows = rows.Where(row => row.Value > 0).ToList();
        var max = Math.Max(1m, rows.Max(row => row.Value));
        var points = new PointCollection();

        for (var i = 0; i < rows.Count; i++)
        {
            var x = rows.Count == 1
                ? left + chartWidth / 2
                : left + chartWidth * i / (rows.Count - 1);
            var y = top + chartHeight - (double)(rows[i].Value / max) * chartHeight;
            points.Add(new Point(x, y));
        }

        if (points.Count > 1)
        {
            var areaPoints = new PointCollection(points)
            {
                new(left + chartWidth, top + chartHeight),
                new(left, top + chartHeight)
            };
            ReportsLineChartCanvas.Children.Add(new Polygon
            {
                Points = areaPoints,
                Fill = AccentSoftBrush,
                Opacity = realRows.Count == 0 ? 0.1 : 0.55
            });

            ReportsLineChartCanvas.Children.Add(new Polyline
            {
                Points = points,
                Stroke = AccentBrush,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            });
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var point = points[i];
            var dot = new Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = rows[i].AccentBrush,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, point.X - 5.5);
            Canvas.SetTop(dot, point.Y - 5.5);
            ReportsLineChartCanvas.Children.Add(dot);

            var value = new TextBlock
            {
                Text = rows[i].ValueText,
                Foreground = InkBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(value, Math.Max(0, Math.Min(width - 70, point.X - 28)));
            Canvas.SetTop(value, Math.Max(0, point.Y - 25));
            ReportsLineChartCanvas.Children.Add(value);

            var label = new TextBlock
            {
                Text = rows[i].Label,
                Foreground = MutedBrush,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            Canvas.SetLeft(label, Math.Max(0, Math.Min(width - 72, point.X - 30)));
            Canvas.SetTop(label, top + chartHeight + 9);
            ReportsLineChartCanvas.Children.Add(label);
        }
    }

    private void DrawReportsDonutChart()
    {
        ReportsDonutCanvas.Children.Clear();
        if (ReportsDonutChartView.Visibility != Visibility.Visible)
        {
            return;
        }

        var rows = _reportsDonutChartRows.Where(row => row.Value > 0).ToList();
        var total = rows.Sum(row => row.Value);
        var width = ReportsDonutCanvas.ActualWidth > 0
            ? ReportsDonutCanvas.ActualWidth
            : ReportsDonutCanvas.Width;
        var height = ReportsDonutCanvas.ActualHeight > 0
            ? ReportsDonutCanvas.ActualHeight
            : ReportsDonutCanvas.Height;
        var size = Math.Max(1, Math.Min(width, height));
        var centerX = width / 2;
        var centerY = height / 2;
        var outer = Math.Max(12, size / 2 - 8);
        var inner = Math.Max(7, outer * 0.58);

        if (total <= 0)
        {
            var ringThickness = Math.Max(8, outer - inner);
            var ringDiameter = outer * 2;
            ReportsDonutCanvas.Children.Add(new Ellipse
            {
                Width = ringDiameter,
                Height = ringDiameter,
                Stroke = LineBrush,
                StrokeThickness = ringThickness,
                Fill = Brushes.Transparent
            });
            Canvas.SetLeft(ReportsDonutCanvas.Children[^1], centerX - outer);
            Canvas.SetTop(ReportsDonutCanvas.Children[^1], centerY - outer);
        }
        else
        {
            var startAngle = 0d;
            foreach (var row in rows)
            {
                var sweep = Math.Max(1, Math.Min(359.8, (double)(row.Value / total) * 360));
                ReportsDonutCanvas.Children.Add(CreateDonutSlice(centerX, centerY, outer, inner, startAngle, sweep, row.AccentBrush));
                startAngle += sweep;
            }
        }

        var centerDiameter = Math.Max(1, inner * 2 - 4);
        var centerCircle = new Ellipse
        {
            Width = centerDiameter,
            Height = centerDiameter,
            Fill = PanelBrush
        };
        Canvas.SetLeft(centerCircle, centerX - centerDiameter / 2);
        Canvas.SetTop(centerCircle, centerY - centerDiameter / 2);
        ReportsDonutCanvas.Children.Add(centerCircle);

        var textWidth = Math.Max(70, size * 0.7);
        var totalText = new TextBlock
        {
            Text = total.ToString("N0", Brazil),
            Foreground = InkBrush,
            FontSize = Math.Clamp(size * 0.18, 18, 24),
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Width = textWidth
        };
        Canvas.SetLeft(totalText, centerX - textWidth / 2);
        Canvas.SetTop(totalText, centerY - 23);
        ReportsDonutCanvas.Children.Add(totalText);

        var labelText = new TextBlock
        {
            Text = "total",
            Foreground = MutedBrush,
            FontSize = Math.Clamp(size * 0.09, 10, 12),
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Width = textWidth
        };
        Canvas.SetLeft(labelText, centerX - textWidth / 2);
        Canvas.SetTop(labelText, centerY + 5);
        ReportsDonutCanvas.Children.Add(labelText);
    }

    private static Path CreateDonutSlice(
        double centerX,
        double centerY,
        double outerRadius,
        double innerRadius,
        double startAngle,
        double sweepAngle,
        Brush fill)
    {
        var endAngle = startAngle + sweepAngle;
        var outerStart = PointOnCircle(centerX, centerY, outerRadius, startAngle);
        var outerEnd = PointOnCircle(centerX, centerY, outerRadius, endAngle);
        var innerEnd = PointOnCircle(centerX, centerY, innerRadius, endAngle);
        var innerStart = PointOnCircle(centerX, centerY, innerRadius, startAngle);
        var largeArc = sweepAngle > 180;

        var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };
        figure.Segments.Add(new ArcSegment(outerEnd, new Size(outerRadius, outerRadius), 0, largeArc, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(innerEnd, true));
        figure.Segments.Add(new ArcSegment(innerStart, new Size(innerRadius, innerRadius), 0, largeArc, SweepDirection.Counterclockwise, true));
        figure.Segments.Add(new LineSegment(outerStart, true));

        return new Path
        {
            Data = new PathGeometry([figure]),
            Fill = fill,
            Stroke = Brushes.White,
            StrokeThickness = 2
        };
    }

    private static Point PointOnCircle(double centerX, double centerY, double radius, double angleDegrees)
    {
        var angle = (angleDegrees - 90) * Math.PI / 180;
        return new Point(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle));
    }

    private void RefreshReportsServices(IReadOnlyCollection<Appointment> appointments)
    {
        _reportsServices.Clear();
        var rows = appointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Where(item => !string.IsNullOrWhiteSpace(item.ServiceName))
            .GroupBy(item => item.ServiceName)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Sum(item => item.Price))
            .Take(6)
            .ToList();

        var navigationVisibility = rows.Count > 3 ? Visibility.Visible : Visibility.Collapsed;
        ReportsServicesScrollLeftButton.Visibility = navigationVisibility;
        ReportsServicesScrollRightButton.Visibility = navigationVisibility;

        foreach (var group in rows)
        {
            var revenue = group.Where(item => item.Status == AppointmentStatus.Done).Sum(item => item.Price);
            _reportsServices.Add(new EstablishmentListRow(
                group.Key,
                $"{group.Count()} atendimento(s) no período",
                revenue.ToString("C0", Brazil),
                AccentSoftBrush,
                AccentBrush,
                Icon: PackIconKind.ClipboardText));
        }

        if (_reportsServices.Count == 0)
        {
            _reportsServices.Add(new EstablishmentListRow(
                "Nenhum serviço",
                "Sem atendimentos no período.",
                "",
                GraySoftBrush,
                MutedBrush,
                Icon: PackIconKind.ClipboardText));
        }
    }

    private void RefreshReportsProfessionals(IReadOnlyCollection<Appointment> appointments)
    {
        _reportsProfessionals.Clear();
        var rows = appointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Where(item => !string.IsNullOrWhiteSpace(item.ProfessionalName))
            .GroupBy(item => item.ProfessionalName)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Where(item => item.Status == AppointmentStatus.Done).Sum(item => item.Price))
            .Take(8)
            .ToList();

        foreach (var group in rows)
        {
            var done = group.Count(item => item.Status == AppointmentStatus.Done);
            var revenue = group.Where(item => item.Status == AppointmentStatus.Done).Sum(item => item.Price);
            _reportsProfessionals.Add(new EstablishmentListRow(
                group.Key,
                $"{done} finalizado(s) | {group.Count()} atendimento(s)",
                revenue.ToString("C0", Brazil),
                AccentSoftBrush,
                AccentBrush,
                Icon: PackIconKind.AccountTie));
        }

        if (_reportsProfessionals.Count == 0)
        {
            _reportsProfessionals.Add(new EstablishmentListRow(
                "Nenhum profissional no período",
                "Os atendimentos da equipe aparecerão aqui quando houver movimentações.",
                "",
                GraySoftBrush,
                MutedBrush,
                Icon: PackIconKind.AccountOutline));
        }
    }

    private void RefreshMarketingPage()
    {
        MarketingBusinessText.Text = BusinessDisplayName();
        EnsureDefaultPromotionMessage();
        RefreshMarketingPreview();

        var today = DateTime.Today;
        var staleCustomers = _data.Customers
            .Where(item => item.LastSeenAt.Date <= today.AddDays(-30))
            .ToList();
        var noShows = _data.Appointments
            .Where(item => item.Status == AppointmentStatus.NoShow && item.Start.Date >= today.AddDays(-60))
            .ToList();
        var pendingConfirmations = _data.Appointments
            .Where(item => item.Status == AppointmentStatus.Scheduled && item.Start.Date >= today)
            .ToList();
        RefreshMarketingContacts();
        RefreshMarketingMessages();
        RefreshMarketingCampaigns(staleCustomers.Count, noShows.Count, pendingConfirmations.Count);
    }

    private void EnsureDefaultPromotionMessage()
    {
        if (!string.IsNullOrWhiteSpace(PromotionMessageTextBox.Text))
        {
            return;
        }

        PromotionMessageTextBox.Text = "Oi, {nome}! Tudo bem? Aqui é da {empresa}. A {promocao} está ativa: {oferta}. Quer que eu veja um horário para você?";
    }

    private void RefreshMarketingContacts()
    {
        _marketingContacts.Clear();
        foreach (var contact in BuildMarketingContacts().Take(12))
        {
            _marketingContacts.Add(contact);
        }

        if (_marketingContacts.Count == 0)
        {
            _marketingContacts.Add(new MarketingContactRow(
                "Nenhum cliente prioritário",
                "Clientes sem retorno, faltas ou confirmações pendentes aparecerão aqui.",
                "",
                "Sem ação",
                "Cadastre clientes com telefone para abrir WhatsApp.",
                GraySoftBrush,
                MutedBrush));
        }
    }

    private List<MarketingContactRow> BuildMarketingContacts()
    {
        var rows = new List<MarketingContactRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var today = DateTime.Today;

        foreach (var appointment in _data.Appointments
                     .Where(item => item.Status == AppointmentStatus.Scheduled && item.Start.Date >= today)
                     .OrderBy(item => item.Start))
        {
            AddMarketingContact(rows, seen, appointment.CustomerName, appointment.CustomerPhone, $"Confirmar horário de {appointment.Start:dd/MM HH:mm}", appointment.ServiceName, "Confirmação");
        }

        foreach (var appointment in _data.Appointments
                     .Where(item => item.Status == AppointmentStatus.NoShow && item.Start.Date >= today.AddDays(-60))
                     .OrderByDescending(item => item.Start))
        {
            AddMarketingContact(rows, seen, appointment.CustomerName, appointment.CustomerPhone, $"Faltou em {appointment.Start:dd/MM}", appointment.ServiceName, "Retorno");
        }

        foreach (var customer in _data.Customers
                     .Where(item => item.LastSeenAt.Date <= today.AddDays(-30))
                     .OrderBy(item => item.LastSeenAt))
        {
            AddMarketingContact(rows, seen, customer.Name, customer.Phone, $"Último atendimento em {customer.LastSeenAt:dd/MM}", customer.Profile, "Sem retorno");
        }

        return rows;
    }

    private void AddMarketingContact(
        ICollection<MarketingContactRow> rows,
        ISet<string> seen,
        string name,
        string phone,
        string reason,
        string context,
        string badge)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var key = string.IsNullOrWhiteSpace(phone) ? name.Trim() : $"{name.Trim()}|{OnlyDigits(phone)}";
        if (!seen.Add(key))
        {
            return;
        }

        rows.Add(new MarketingContactRow(
            name.Trim(),
            string.Join(" | ", new[] { reason, phone, context }.Where(item => !string.IsNullOrWhiteSpace(item))),
            phone,
            badge,
            BuildMarketingMessage(name.Trim()),
            string.IsNullOrWhiteSpace(phone) ? YellowSoftBrush : AccentSoftBrush,
            string.IsNullOrWhiteSpace(phone) ? InkBrush : AccentBrush));
    }

    private void RefreshMarketingMessages()
    {
        _marketingMessages.Clear();
        _marketingMessages.Add(new EstablishmentListRow("Promoção", "Texto para divulgar oferta, desconto ou pacote especial.", "oferta", WarmSoftBrush, AccentBrush, Icon: PackIconKind.Bullhorn));
        _marketingMessages.Add(new EstablishmentListRow("Confirmação", "Mensagem curta para confirmar presença antes do horário.", "agenda", AccentSoftBrush, AccentBrush, Icon: PackIconKind.CalendarMonth));
        _marketingMessages.Add(new EstablishmentListRow("Pós-atendimento", "Agradeça o cliente e incentive retorno ou avaliação.", "retorno", Solid("#DCFCE7"), Solid("#16A34A"), Icon: PackIconKind.CheckCircleOutline));
        _marketingMessages.Add(new EstablishmentListRow("Cliente sumido", "Convide clientes sem atendimento recente para voltar.", "30 dias", YellowSoftBrush, InkBrush, Icon: PackIconKind.AccountOutline));
    }

    private void RefreshMarketingCampaigns(int staleCustomers, int noShows, int pendingConfirmations)
    {
        var promotionName = string.IsNullOrWhiteSpace(PromotionNameTextBox.Text)
            ? "Oferta da semana"
            : PromotionNameTextBox.Text.Trim();
        var promotionOffer = string.IsNullOrWhiteSpace(PromotionOfferTextBox.Text)
            ? "Configure a oferta da campanha ativa."
            : PromotionOfferTextBox.Text.Trim();

        _marketingCampaigns.Clear();
        _marketingCampaigns.Add(new EstablishmentListRow("Volta para agenda", $"{staleCustomers} cliente(s) sem retorno para chamar.", "WhatsApp", Solid("#DCFCE7"), Solid("#16A34A"), Icon: PackIconKind.AccountOutline));
        _marketingCampaigns.Add(new EstablishmentListRow("Confirmar horários", $"{pendingConfirmations} agendamento(s) aguardando confirmação.", "Hoje", AccentSoftBrush, AccentBrush, Icon: PackIconKind.CalendarMonth));
        _marketingCampaigns.Add(new EstablishmentListRow("Recuperar faltas", $"{noShows} falta(s) recente(s) para remarcar.", "Retorno", RedSoftBrush, Solid("#DC2626"), Icon: PackIconKind.AlertCircleOutline));
        _marketingCampaigns.Add(new EstablishmentListRow(promotionName, promotionOffer, "Promoção", YellowSoftBrush, InkBrush, Icon: PackIconKind.Bullhorn));
    }

    private void RefreshMarketingPreview()
    {
        if (MarketingPreviewText is null ||
            PromotionNameTextBox is null ||
            PromotionOfferTextBox is null ||
            PromotionMessageTextBox is null)
        {
            return;
        }

        MarketingPreviewText.Text = BuildMarketingMessage(_data.Customers.FirstOrDefault()?.Name ?? "Cliente");
    }

    private string BuildMarketingMessage(string customerName)
    {
        var offer = string.IsNullOrWhiteSpace(PromotionOfferTextBox.Text)
            ? "uma condição especial"
            : PromotionOfferTextBox.Text.Trim();
        var promotionName = string.IsNullOrWhiteSpace(PromotionNameTextBox.Text)
            ? "promoção"
            : PromotionNameTextBox.Text.Trim();
        var template = string.IsNullOrWhiteSpace(PromotionMessageTextBox.Text)
            ? "Oi, {nome}! Aqui é da {empresa}. Temos {oferta}. Quer reservar um horário?"
            : PromotionMessageTextBox.Text.Trim();

        return template
            .Replace("{nome}", customerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{empresa}", BusinessDisplayName(), StringComparison.OrdinalIgnoreCase)
            .Replace("{oferta}", offer, StringComparison.OrdinalIgnoreCase)
            .Replace("{promocao}", promotionName, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyPromotionToMessageEditor()
    {
        EnsureDefaultPromotionMessage();

        var promotionName = string.IsNullOrWhiteSpace(PromotionNameTextBox.Text)
            ? "promoção"
            : PromotionNameTextBox.Text.Trim();
        var promotionOffer = string.IsNullOrWhiteSpace(PromotionOfferTextBox.Text)
            ? "uma condição especial"
            : PromotionOfferTextBox.Text.Trim();
        var message = PromotionMessageTextBox.Text.Trim();
        var containsPromotionToken = message.Contains("{promocao}", StringComparison.OrdinalIgnoreCase);
        var containsOfferToken = message.Contains("{oferta}", StringComparison.OrdinalIgnoreCase);

        if (containsPromotionToken || containsOfferToken)
        {
            message = message
                .Replace("{promocao}", promotionName, StringComparison.OrdinalIgnoreCase)
                .Replace("{oferta}", promotionOffer, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(_lastAppliedPromotionName) &&
                message.Contains(_lastAppliedPromotionName, StringComparison.OrdinalIgnoreCase))
            {
                message = message.Replace(
                    _lastAppliedPromotionName,
                    promotionName,
                    StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(_lastAppliedPromotionOffer) &&
                message.Contains(_lastAppliedPromotionOffer, StringComparison.OrdinalIgnoreCase))
            {
                message = message.Replace(
                    _lastAppliedPromotionOffer,
                    promotionOffer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        PromotionMessageTextBox.Text = message;
        PromotionMessageTextBox.CaretIndex = PromotionMessageTextBox.Text.Length;
        _lastAppliedPromotionName = promotionName;
        _lastAppliedPromotionOffer = promotionOffer;
        _lastAppliedPromotionMessage = message;
    }

    private static string OnlyDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private static string NormalizeBrazilPhone(string phone)
    {
        var digits = OnlyDigits(phone);
        if (digits.Length is 10 or 11)
        {
            return $"55{digits}";
        }

        return digits;
    }

    private async Task OpenMarketingWhatsAppAsync(MarketingContactRow row)
    {
        var phone = NormalizeBrazilPhone(row.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus($"Telefone não cadastrado para {row.Name}.");
            return;
        }

        var message = BuildMarketingMessage(row.Name);
        var sent = await SendOrOpenWhatsAppAsync(row.Name, phone, message, "Marketing");
        ShowStatus(sent
            ? $"WhatsApp enviado para {row.Name} pelo canal linkado."
            : $"WhatsApp aberto para {row.Name}. A conversa ficou no painel.");
    }

    private void RefreshWhatsAppSurface()
    {
        var settings = _data.Settings;
        var linked = settings.WhatsAppEnabled && settings.WhatsAppLinked;
        if (!linked &&
            !string.IsNullOrWhiteSpace(settings.WhatsAppEvolutionQrBase64) &&
            settings.WhatsAppEvolutionLastCheckedAt.HasValue &&
            settings.WhatsAppEvolutionLastCheckedAt.Value < DateTime.Now.AddMinutes(-2))
        {
            settings.WhatsAppEvolutionQrBase64 = "";
            settings.WhatsAppEvolutionState = "";
            _store.Save(_data);
        }

        var storePhone = NormalizeBrazilPhone(settings.WhatsAppStorePhone);
        var displayPhone = string.IsNullOrWhiteSpace(storePhone) ? "sem número" : FormatPhone(storePhone);
        if (linked)
        {
            WhatsAppQrPanel.Visibility = Visibility.Collapsed;
            WhatsAppQrImage.Source = null;
        }

        var hasQr = !linked && TryShowWhatsAppQr(settings.WhatsAppEvolutionQrBase64);
        var stateText = string.IsNullOrWhiteSpace(settings.WhatsAppEvolutionState)
            ? ""
            : $" Estado: {settings.WhatsAppEvolutionState}.";

        SettingsWhatsAppStatusText.Text = linked
            ? $"Linkado: {displayPhone}"
            : hasQr ? "Aguardando leitura do QR" : "Não linkado";
        SettingsWhatsAppStatusText.Foreground = linked ? Solid("#16A34A") : MutedBrush;
        SettingsWhatsAppDetailText.Text = linked
            ? "Confirmações e mensagens ativas."
            : hasQr
                ? "QR pronto no painel."
                : "Confirme horários e chame clientes pelo app.";
        SettingsWhatsAppConnectButton.Content = linked ? "Deslinkar" : hasQr ? "Novo QR" : "Linkar WhatsApp";

        WhatsAppFloatingStatusText.Text = linked
            ? $"Linkado em {displayPhone}"
            : hasQr ? "QR pronto para escanear" : "Linke o WhatsApp da loja";
        WhatsAppFloatingDetailText.Text = linked
            ? $"Ativo na agenda.{stateText}"
            : hasQr
                ? "Escaneie o QR e aguarde conectar."
                : "Linke o WhatsApp da loja.";
        WhatsAppFloatingConnectButton.Content = linked ? "Deslinkar" : hasQr ? "Novo QR" : "Linkar";
        WhatsAppFloatingEmptyConnectButton.Content = hasQr ? "Gerar novo QR" : "Linkar WhatsApp";
        if (!WhatsAppStorePhoneTextBox.IsKeyboardFocusWithin)
        {
            WhatsAppStorePhoneTextBox.Text = string.IsNullOrWhiteSpace(settings.WhatsAppStorePhone)
                ? FirstFilled(settings.BusinessPhone, settings.AccountPhone)
                : settings.WhatsAppStorePhone;
        }

        var messagesWithPhone = _data.WhatsAppMessages
            .Where(item => !IsLegacyWhatsAppConnectionNotice(item))
            .Where(item => !string.IsNullOrWhiteSpace(NormalizeBrazilPhone(item.Phone)))
            .OrderByDescending(item => item.CreatedAt)
            .ToList();
        var messageIndexes = _data.WhatsAppMessages
            .Select((message, index) => new { message.Id, index })
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);

        RefreshWhatsAppConversationRows(messagesWithPhone);

        var selectedConversationExists =
            !string.IsNullOrWhiteSpace(_selectedWhatsAppReplyPhone) &&
            messagesWithPhone.Any(item => string.Equals(NormalizeBrazilPhone(item.Phone), _selectedWhatsAppReplyPhone, StringComparison.OrdinalIgnoreCase));
        if (!selectedConversationExists)
        {
            _selectedWhatsAppReplyPhone = "";
            _selectedWhatsAppReplyName = "";
            _whatsAppConversationOpen = false;
        }

        var showingConversation = _whatsAppConversationOpen && selectedConversationExists;
        var visibleMessages = showingConversation
            ? DeduplicateWhatsAppTimeline(messagesWithPhone
                .Where(item => string.Equals(NormalizeBrazilPhone(item.Phone), _selectedWhatsAppReplyPhone, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.CreatedAt)
                .ThenByDescending(item => WhatsAppStorageIndex(item, messageIndexes)))
                .TakeLast(120)
                .ToList()
            : [];

        _whatsAppMessages.Clear();
        foreach (var message in visibleMessages)
        {
            var incoming = IsWhatsAppIncoming(message);
            _whatsAppMessages.Add(new WhatsAppMessageRow(
                WhatsAppMessageDisplayName(message),
                NormalizeBrazilPhone(message.Phone),
                message.Message,
                $"{message.CreatedAt:dd/MM HH:mm}",
                message.Category,
                incoming ? "Recebida" : StatusLabelForWhatsApp(message.Status),
                incoming ? BlueSoftBrush : AccentSoftBrush,
                incoming ? AccentBrush : Solid("#16A34A"),
                incoming ? Solid("#FFF9F4") : Solid("#F0FDF4"),
                incoming ? Solid("#EADFD6") : Solid("#BBF7D0")));
        }

        WhatsAppConversationListHeader.Visibility = showingConversation ? Visibility.Collapsed : Visibility.Visible;
        WhatsAppActiveConversationHeader.Visibility = showingConversation ? Visibility.Visible : Visibility.Collapsed;
        WhatsAppConversationListCountText.Text = _whatsAppConversations.Count == 0
            ? "Nenhuma conversa ainda"
            : $"{_whatsAppConversations.Count} conversa(s)";
        WhatsAppActiveConversationTitleText.Text = FirstFilled(_selectedWhatsAppReplyName, FormatPhone(_selectedWhatsAppReplyPhone));
        WhatsAppActiveConversationPhoneText.Text = FormatPhone(_selectedWhatsAppReplyPhone);
        WhatsAppReplyPanel.Visibility = showingConversation ? Visibility.Visible : Visibility.Collapsed;
        WhatsAppConversationCardsItemsControl.Visibility =
            !showingConversation && _whatsAppConversations.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        WhatsAppFloatingMessagesItemsControl.Visibility =
            showingConversation && _whatsAppMessages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        WhatsAppFloatingEmptyText.Text = showingConversation
            ? "Nenhuma mensagem nessa conversa ainda."
            : "Nenhuma conversa ainda. Quando um cliente chamar no WhatsApp, aparece aqui.";
        WhatsAppFloatingEmptyText.Visibility =
            (showingConversation && _whatsAppMessages.Count == 0) ||
            (!showingConversation && _whatsAppConversations.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        WhatsAppFloatingEmptyConnectButton.Visibility =
            !linked && !showingConversation && _whatsAppConversations.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        RefreshWhatsAppReplyComposer();
        RefreshWhatsAppLeadCard(showingConversation);
        ScrollWhatsAppConversationToEnd();

        var unreadCount = _data.WhatsAppMessages.Count(item =>
            IsWhatsAppIncoming(item) &&
            item.ReadAt is null &&
            !IsLegacyWhatsAppConnectionNotice(item));
        WhatsAppFloatingBadgeText.Text = unreadCount > 99 ? "99+" : unreadCount.ToString(CultureInfo.InvariantCulture);
        WhatsAppFloatingBadge.Visibility =
            unreadCount > 0 && WhatsAppFloatingPanel.Visibility != Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;
        WhatsAppFloatingDetailCard.Visibility = linked || _whatsAppConversations.Count > 0 || showingConversation
            ? Visibility.Collapsed
            : Visibility.Visible;
        RefreshWhatsAppLauncherVisibility();
        UpdateWhatsAppPollingState();
    }

    private void RefreshWhatsAppLauncherVisibility()
    {
        var modalOpen =
            AppointmentEditorOverlay.Visibility == Visibility.Visible ||
            AppDialogBackdrop.Visibility == Visibility.Visible ||
            OnboardingOverlay.Visibility == Visibility.Visible;
        WhatsAppFloatingButton.Visibility =
            modalOpen || WhatsAppFloatingPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    private void RefreshWhatsAppConversationRows(IReadOnlyCollection<WhatsAppMessage> messages)
    {
        _whatsAppConversations.Clear();
        foreach (var group in messages
                     .GroupBy(item => NormalizeBrazilPhone(item.Phone), StringComparer.OrdinalIgnoreCase)
                     .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                     .Select(group =>
                     {
                         var ordered = group
                             .OrderByDescending(item => item.CreatedAt)
                             .ThenBy(item => _data.WhatsAppMessages.IndexOf(item))
                             .ToList();
                         var latest = ordered[0];
                         var incomingCount = ordered.Count(item =>
                             IsWhatsAppIncoming(item) && item.ReadAt is null);
                         var latestIncoming = ordered.FirstOrDefault(item =>
                             IsWhatsAppIncoming(item) &&
                             !string.IsNullOrWhiteSpace(item.CustomerName));
                         var title = FirstFilled(
                             latestIncoming?.CustomerName ?? "",
                             ordered.FirstOrDefault(item => IsWhatsAppIncoming(item) && !string.IsNullOrWhiteSpace(item.CustomerName))?.CustomerName ?? "",
                             FormatPhone(group.Key));
                         return new WhatsAppConversationRow(
                             title,
                            group.Key,
                            ShortPreview(latest.Message, 72),
                            $"{latest.CreatedAt:dd/MM HH:mm}",
                            latest.CreatedAt,
                            incomingCount);
                     })
                     .OrderByDescending(item => item.LastAt)
                     .Take(30))
        {
            _whatsAppConversations.Add(group);
        }
    }

    private int WhatsAppStorageIndex(WhatsAppMessage message, IReadOnlyDictionary<string, int> indexes) =>
        !string.IsNullOrWhiteSpace(message.Id) && indexes.TryGetValue(message.Id, out var index)
            ? index
            : _data.WhatsAppMessages.IndexOf(message);

    private static List<WhatsAppMessage> DeduplicateWhatsAppTimeline(IEnumerable<WhatsAppMessage> messages)
    {
        var output = new List<WhatsAppMessage>();
        foreach (var message in messages)
        {
            var duplicateIndex = output.FindIndex(existing => IsWhatsAppEchoDuplicate(existing, message));
            if (duplicateIndex >= 0)
            {
                if (IsWhatsAppOutgoing(message) && !IsWhatsAppOutgoing(output[duplicateIndex]))
                {
                    output[duplicateIndex] = message;
                }

                continue;
            }

            output.Add(message);
        }

        return output;
    }

    private static bool IsWhatsAppEchoDuplicate(WhatsAppMessage a, WhatsAppMessage b) =>
        !string.Equals(a.Direction, b.Direction, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(NormalizeBrazilPhone(a.Phone), NormalizeBrazilPhone(b.Phone), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Message, b.Message, StringComparison.Ordinal) &&
        Math.Abs((a.CreatedAt - b.CreatedAt).TotalSeconds) <= 1;

    private static bool IsWhatsAppIncoming(WhatsAppMessage message) =>
        string.Equals(message.Direction, "entrada", StringComparison.OrdinalIgnoreCase);

    private static bool IsWhatsAppOutgoing(WhatsAppMessage message) =>
        string.Equals(message.Direction, "saida", StringComparison.OrdinalIgnoreCase);

    private static bool IsLegacyWhatsAppConnectionNotice(WhatsAppMessage message) =>
        string.Equals(message.Category, "Conexão", StringComparison.OrdinalIgnoreCase) &&
        message.Message.StartsWith(
            "WhatsApp linkado. Confirmações, retornos e mensagens dos clientes aparecem neste painel.",
            StringComparison.OrdinalIgnoreCase);

    private string WhatsAppMessageDisplayName(WhatsAppMessage message)
    {
        if (IsWhatsAppOutgoing(message))
        {
            return FirstFilled(_data.Settings.AccountFullName, BusinessDisplayName(), "Você");
        }

        return FirstFilled(message.CustomerName, FormatPhone(message.Phone), "Cliente");
    }

    private static string ShortPreview(string value, int maxLength)
    {
        var text = string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..Math.Max(1, maxLength - 1)] + "...";
    }

    private void SelectWhatsAppConversation(WhatsAppMessage? message)
    {
        if (message is null)
        {
            _selectedWhatsAppReplyPhone = "";
            _selectedWhatsAppReplyName = "";
            return;
        }

        _selectedWhatsAppReplyPhone = NormalizeBrazilPhone(message.Phone);
        _selectedWhatsAppReplyName = string.IsNullOrWhiteSpace(message.CustomerName)
            ? FormatPhone(_selectedWhatsAppReplyPhone)
            : message.CustomerName.Trim();
    }

    private void ScrollWhatsAppConversationToEnd()
    {
        if (WhatsAppConversationScrollViewer is null ||
            WhatsAppFloatingPanel.Visibility != Visibility.Visible ||
            !_whatsAppConversationOpen)
        {
            return;
        }

        Dispatcher.BeginInvoke(() => WhatsAppConversationScrollViewer.ScrollToEnd(), DispatcherPriority.Background);
    }

    private void RefreshWhatsAppReplyComposer()
    {
        if (WhatsAppReplyTargetText is null || WhatsAppReplyTextBox is null || WhatsAppReplySendButton is null)
        {
            return;
        }

        var hasTarget = !string.IsNullOrWhiteSpace(_selectedWhatsAppReplyPhone);
        WhatsAppReplyTargetText.Text = hasTarget
            ? $"Responder para {FirstFilled(_selectedWhatsAppReplyName, FormatPhone(_selectedWhatsAppReplyPhone))}"
            : "Selecione uma conversa para responder";
        WhatsAppReplyTextBox.IsEnabled = hasTarget && _data.Settings.WhatsAppLinked;
        WhatsAppReplySendButton.IsEnabled = hasTarget && _data.Settings.WhatsAppLinked;
    }

    private string WhatsAppAgendaSnapshotPath() =>
        System.IO.Path.Combine(_store.DataRoot, "whatsapp-agenda-snapshot.json");

    private void ScheduleWhatsAppAgendaSnapshotExport()
    {
        _snapshotExportTimer.Stop();
        _snapshotExportTimer.Start();
    }

    private void ExportWhatsAppAgendaSnapshot()
    {
        try
        {
            var today = DateTime.Today;
            var service = _data.Services
                .OrderBy(item => item.Name)
                .FirstOrDefault();
            var defaultDuration = Math.Clamp(service?.DurationMinutes ?? 30, 15, 240);
            var days = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = today.AddDays(offset);
                    var activeAppointments = _data.Appointments
                        .Where(item => item.Start.Date == date && IsOperationalStatus(item))
                        .OrderBy(item => item.Start)
                        .ToList();
                    return new
                    {
                        date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        label = DateShortcutLabel(date),
                        workday = $"{_data.Settings.WorkdayStartHour:00}:00-{_data.Settings.WorkdayEndHour:00}:00",
                        busyCount = activeAppointments.Count,
                        availableSlots = BuildWhatsAppAvailableSlots(date, defaultDuration, 6)
                    };
                })
                .ToList();

            var payload = new
            {
                storeName = BusinessDisplayName(),
                segment = _data.Settings.BusinessSegment,
                generatedAt = DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                workdayStartHour = _data.Settings.WorkdayStartHour,
                workdayEndHour = _data.Settings.WorkdayEndHour,
                services = _data.Services
                    .Where(item => item.IsActive)
                    .OrderBy(item => item.Name)
                    .Take(12)
                    .Select(item => new
                    {
                        id = item.Id,
                        name = item.Name,
                        durationMinutes = item.DurationMinutes,
                        price = item.Price
                    })
                    .ToList(),
                professionals = _data.Professionals
                    .Where(item => item.IsActive)
                    .OrderBy(item => item.Name)
                    .Take(12)
                    .Select(item => item.Name)
                    .ToList(),
                bookingServices = BuildWhatsAppBookingServicesSnapshot(today),
                days
            };

            System.IO.Directory.CreateDirectory(_store.DataRoot);
            var options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(WhatsAppAgendaSnapshotPath(), JsonSerializer.Serialize(payload, options), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Debug.WriteLine($"WhatsApp agenda snapshot skipped: {ex.Message}");
        }
    }

    private List<string> BuildWhatsAppAvailableSlots(DateTime date, int durationMinutes, int maxSlots)
    {
        var slots = new List<string>();
        if (!IsConfiguredWorkday(date))
        {
            return slots;
        }

        var professionals = _data.Professionals
            .OrderBy(item => item.Name)
            .ToList();
        if (professionals.Count == 0)
        {
            return slots;
        }

        var service = _data.Services.OrderBy(item => item.Name).FirstOrDefault();
        var start = date.Date.AddHours(_data.Settings.WorkdayStartHour);
        var end = date.Date.AddHours(_data.Settings.WorkdayEndHour);
        if (date.Date == DateTime.Today)
        {
            var next = DateTime.Now.AddMinutes(30);
            var roundedMinute = next.Minute <= 30 ? 30 : 60;
            start = roundedMinute == 60
                ? new DateTime(next.Year, next.Month, next.Day, next.Hour, 0, 0).AddHours(1)
                : new DateTime(next.Year, next.Month, next.Day, next.Hour, 30, 0);
            if (start < date.Date.AddHours(_data.Settings.WorkdayStartHour))
            {
                start = date.Date.AddHours(_data.Settings.WorkdayStartHour);
            }
        }

        for (var cursor = start; cursor.AddMinutes(durationMinutes) <= end && slots.Count < maxSlots; cursor = cursor.AddMinutes(30))
        {
            if (OverlapsConfiguredBreak(cursor, cursor.AddMinutes(durationMinutes)))
            {
                continue;
            }

            foreach (var professional in professionals)
            {
                var draft = new AppointmentDraft(
                    _data.Settings.BusinessSegment,
                    "Cliente WhatsApp",
                    "",
                    "",
                    service?.Id ?? "",
                    service?.Name ?? "Atendimento",
                    professional.Id,
                    professional.Name,
                    service?.DefaultResource ?? "",
                    cursor,
                    durationMinutes,
                    service?.Price ?? 0,
                    "");
                if (!FindConflicts(draft, null).Any())
                {
                    slots.Add($"{cursor:HH:mm} com {professional.Name}");
                    break;
                }
            }
        }

        return slots;
    }

    private string LastWhatsAppEventText()
    {
        var last = _data.Settings.WhatsAppLastMessageAt ??
                   _data.WhatsAppMessages.OrderByDescending(item => item.CreatedAt).FirstOrDefault()?.CreatedAt ??
                   _data.Settings.WhatsAppLinkedAt;
        return last.HasValue ? last.Value.ToString("dd/MM HH:mm", Brazil) : "sem mensagens";
    }

    private static string StatusLabelForWhatsApp(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "enviado" => "Enviada",
            "aberto" => "Aberta",
            "recebido" => "Recebida",
            "erro" => "Erro",
            _ => "Criada"
        };

    private static string FirstFilled(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";

    private static string FormatPhone(string phone)
    {
        var digits = OnlyDigits(phone);
        if (digits.Length == 13 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            return $"+55 ({digits[2..4]}) {digits[4..9]}-{digits[9..]}";
        }

        if (digits.Length == 12 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            return $"+55 ({digits[2..4]}) {digits[4..8]}-{digits[8..]}";
        }

        if (digits.Length == 11)
        {
            return $"({digits[..2]}) {digits[2..7]}-{digits[7..]}";
        }

        if (digits.Length == 10)
        {
            return $"({digits[..2]}) {digits[2..6]}-{digits[6..]}";
        }

        return string.IsNullOrWhiteSpace(phone) ? "sem telefone" : phone;
    }

    private bool TryShowWhatsAppQr(string qrBase64)
    {
        WhatsAppQrPanel.Visibility = Visibility.Collapsed;
        WhatsAppQrImage.Source = null;
        WhatsAppQrHintText.Text = "Depois de escanear, clique em checar conexão.";
        if (string.IsNullOrWhiteSpace(qrBase64))
        {
            return false;
        }

        try
        {
            var clean = qrBase64.Trim();
            var comma = clean.IndexOf(',', StringComparison.Ordinal);
            if (comma >= 0)
            {
                clean = clean[(comma + 1)..];
            }

            var bytes = Convert.FromBase64String(clean);
            using var stream = new System.IO.MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            WhatsAppQrImage.Source = image;
            WhatsAppQrPanel.Visibility = Visibility.Visible;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException or System.IO.IOException)
        {
            WhatsAppQrHintText.Text = "QR recebido, mas não consegui renderizar. Gere novamente.";
            WhatsAppQrPanel.Visibility = Visibility.Visible;
            return true;
        }
    }

    private void AddWhatsAppMessage(
        string customerName,
        string phone,
        string message,
        string category,
        string status,
        string direction = "saida",
        string id = "",
        DateTime? createdAt = null)
    {
        var normalizedPhone = NormalizeBrazilPhone(phone ?? "");
        var cleanMessage = message.Trim();
        var when = createdAt ?? DateTime.Now;
        if (_data.WhatsAppMessages.Any(item =>
                (!string.IsNullOrWhiteSpace(id) && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)) ||
                ((string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(item.ProviderMessageId)) &&
                 string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Phone, normalizedPhone, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Message, cleanMessage, StringComparison.Ordinal) &&
                 Math.Abs((item.CreatedAt - when).TotalMinutes) < 3) ||
                ((string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(item.ProviderMessageId)) &&
                 !string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Phone, normalizedPhone, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Message, cleanMessage, StringComparison.Ordinal) &&
                 Math.Abs((item.CreatedAt - when).TotalSeconds) <= 1)))
        {
            return;
        }

        _data.WhatsAppMessages.Insert(0, new WhatsAppMessage
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
            ProviderMessageId = id,
            Instance = WhatsAppRealtimeInstanceName(),
            CustomerName = customerName.Trim(),
            Phone = normalizedPhone,
            Message = cleanMessage,
            Direction = direction,
            Status = status,
            Category = category,
            CreatedAt = when,
            SentAt = string.Equals(direction, "saida", StringComparison.OrdinalIgnoreCase) ? when : null
        });

        if (_data.WhatsAppMessages.Count > 250)
        {
            _data.WhatsAppMessages.RemoveRange(250, _data.WhatsAppMessages.Count - 250);
        }

        _data.Settings.WhatsAppLastMessageAt = DateTime.Now;
        _store.Save(_data);
        RefreshWhatsAppSurface();
    }

    private void ToggleWhatsAppPanelButton_Click(object sender, RoutedEventArgs e)
    {
        var opening = WhatsAppFloatingPanel.Visibility != Visibility.Visible;
        if (!opening)
        {
            CloseWhatsAppPanel();
            return;
        }

        if (opening)
        {
            _whatsAppConversationOpen = false;
            _whatsAppReturnFocusElement = Keyboard.FocusedElement as FrameworkElement;
        }

        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        RefreshWhatsAppSurface();
        FocusWhatsAppPanel();
        _ = PollWhatsAppEvolutionMessagesAsync();
    }

    private void OpenWhatsAppPanelButton_Click(object sender, RoutedEventArgs e)
    {
        _whatsAppConversationOpen = false;
        _whatsAppReturnFocusElement = Keyboard.FocusedElement as FrameworkElement;
        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        RefreshWhatsAppSurface();
        FocusWhatsAppPanel();
        _ = PollWhatsAppEvolutionMessagesAsync();
    }

    private void CloseWhatsAppPanelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseWhatsAppPanel();
    }

    private void CloseWhatsAppPanel()
    {
        WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();
        UpdateWhatsAppPollingState();
        var returnFocus = _whatsAppReturnFocusElement ?? WhatsAppFloatingButton;
        _whatsAppReturnFocusElement = null;
        Dispatcher.BeginInvoke(returnFocus.Focus, DispatcherPriority.Input);
    }

    private void FocusWhatsAppPanel()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (WhatsAppFloatingEmptyConnectButton.Visibility == Visibility.Visible)
            {
                WhatsAppFloatingEmptyConnectButton.Focus();
                return;
            }

            WhatsAppFloatingConnectButton.Focus();
        }, DispatcherPriority.Input);
    }

    private void SelectWhatsAppConversationFromMessageButton_Click(object sender, RoutedEventArgs e)
    {
        var phone = "";
        var name = "";
        if (sender is FrameworkElement { DataContext: WhatsAppMessageRow row })
        {
            phone = row.Phone;
            name = row.CustomerName;
        }
        else if (sender is FrameworkElement element)
        {
            phone = element.Tag?.ToString() ?? "";
        }

        phone = NormalizeBrazilPhone(phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus("Essa mensagem não tem telefone para responder.");
            return;
        }

        OpenWhatsAppConversation(phone, name);
        e.Handled = true;
    }

    private void OpenWhatsAppConversationCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: WhatsAppConversationRow row })
        {
            return;
        }

        OpenWhatsAppConversation(row.Phone, row.Title);
        e.Handled = true;
    }

    private void OpenWhatsAppConversation(string phone, string name)
    {
        phone = NormalizeBrazilPhone(phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus("Essa conversa não tem telefone para abrir.");
            return;
        }

        _selectedWhatsAppReplyPhone = phone;
        _selectedWhatsAppReplyName = string.IsNullOrWhiteSpace(name) ? FormatPhone(phone) : name.Trim();
        _whatsAppConversationOpen = true;
        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        MarkWhatsAppConversationRead(phone);
        RefreshWhatsAppSurface();
        RefreshWhatsAppReplyComposer();
        WhatsAppReplyTextBox.Focus();
    }

    private void BackToWhatsAppConversationsButton_Click(object sender, RoutedEventArgs e)
    {
        _whatsAppConversationOpen = false;
        RefreshWhatsAppSurface();
    }

    private async void SendWhatsAppInlineReplyButton_Click(object sender, RoutedEventArgs e)
    {
        await SendWhatsAppInlineReplyAsync();
    }

    private async void WhatsAppReplyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            e.Handled = true;
            await SendWhatsAppInlineReplyAsync();
        }
    }

    private async Task SendWhatsAppInlineReplyAsync()
    {
        var phone = NormalizeBrazilPhone(_selectedWhatsAppReplyPhone);
        var text = WhatsAppReplyTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus("Selecione uma conversa para responder.");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            ShowStatus("Digite a resposta antes de enviar.");
            WhatsAppReplyTextBox.Focus();
            return;
        }

        if (!_data.Settings.WhatsAppLinked)
        {
            ShowStatus("WhatsApp não está linkado. Gere o QR antes de responder pelo painel.");
            return;
        }

        WhatsAppReplySendButton.IsEnabled = false;
        try
        {
            var result = await SendWhatsAppEvolutionTextAsync(phone, text);
            if (!result.Ok)
            {
                ShowStatus($"WhatsApp recusou a resposta: {result.Message}");
                return;
            }

            AddWhatsAppMessage(FirstFilled(_data.Settings.AccountFullName, BusinessDisplayName(), "Você"), phone, text, "Atendimento", "enviado");
            WhatsAppReplyTextBox.Clear();
            ShowStatus($"Resposta enviada para {FirstFilled(_selectedWhatsAppReplyName, FormatPhone(phone))}.");
        }
        finally
        {
            RefreshWhatsAppReplyComposer();
            WhatsAppReplyTextBox.Focus();
        }
    }

    private async void CheckWhatsAppConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckWhatsAppConnectionAsync(showPanel: true);
    }

    private async void LinkWhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data.Settings.WhatsAppLinked)
        {
            SetWhatsAppButtonsEnabled(false);
            await ResetWhatsAppEvolutionInstanceAsync();
            _data.Settings.WhatsAppLinked = false;
            _data.Settings.WhatsAppConnectedName = "";
            _data.Settings.WhatsAppEvolutionState = "";
            _data.Settings.WhatsAppEvolutionQrBase64 = "";
            _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
            _store.Save(_data);
            SetWhatsAppButtonsEnabled(true);
            RefreshWhatsAppSurface();
            ShowStatus("WhatsApp deslinkado. O histórico continua salvo.");
            return;
        }

        var phone = FirstFilled(WhatsAppStorePhoneTextBox.Text, _data.Settings.BusinessPhone, _data.Settings.AccountPhone);
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? "" : NormalizeBrazilPhone(phone);
        if (!string.IsNullOrWhiteSpace(phone) && normalizedPhone.Length < 10)
        {
            normalizedPhone = "";
        }

        SetWhatsAppButtonsEnabled(false);
        _data.Settings.WhatsAppEnabled = true;
            if (ActiveWhatsAppLead() is { } activeLead)
            {
                activeLead.Stage = "handoff";
                activeLead.UpdatedAt = DateTime.Now;
                _store.Save(_data);
                _ = PatchWhatsAppLeadStageAsync(activeLead.Id, activeLead.Stage);
            }
        _data.Settings.WhatsAppStorePhone = normalizedPhone;
        _data.Settings.WhatsAppEvolutionQrBase64 = "";
        _data.Settings.WhatsAppEvolutionState = "";
        if (!IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true);
        }
        _data.Settings.WhatsAppEvolutionBaseUrl = NormalizeWhatsAppEvolutionBaseUrl(_data.Settings.WhatsAppEvolutionBaseUrl);
        _data.Settings.WhatsAppEvolutionInstanceName = NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName);
        _store.Save(_data);
        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        RefreshWhatsAppSurface();
        ShowStatus("Gerando QR do WhatsApp para linkar a loja...");

        if (IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            await LinkWhatsAppGatewayAsync(normalizedPhone);
            return;
        }

        var state = await FetchWhatsAppEvolutionStateAsync();
        if (state.Ok && IsWhatsAppEvolutionConnected(state.State))
        {
            await ConfigureWhatsAppEvolutionWebhookAsync();
            ApplyWhatsAppConnectedState(state, normalizedPhone);
            SetWhatsAppButtonsEnabled(true);
            ShowStatus($"WhatsApp linkado: {FormatPhone(normalizedPhone)}.");
            return;
        }

        await ResetWhatsAppEvolutionInstanceAsync();
        var create = await CreateWhatsAppEvolutionInstanceAsync();
        if (!create.Ok)
        {
            _data.Settings.WhatsAppEvolutionState = "erro";
            _data.Settings.WhatsAppEvolutionQrBase64 = "";
            _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
            _store.Save(_data);
            SetWhatsAppButtonsEnabled(true);
            RefreshWhatsAppSurface();
            ShowStatus($"WhatsApp não linkou: {create.Message}");
            return;
        }

        await ConfigureWhatsAppEvolutionWebhookAsync();
        var connect = await ConnectWhatsAppEvolutionInstanceAsync();
        if (connect.Ok && IsWhatsAppEvolutionConnected(connect.State))
        {
            await ConfigureWhatsAppEvolutionWebhookAsync();
            ApplyWhatsAppConnectedState(connect, normalizedPhone);
            SetWhatsAppButtonsEnabled(true);
            ShowStatus($"WhatsApp linkado: {FormatPhone(normalizedPhone)}.");
            return;
        }

        _data.Settings.WhatsAppEvolutionState = string.IsNullOrWhiteSpace(connect.State) ? "qrcode" : connect.State;
        _data.Settings.WhatsAppEvolutionQrBase64 = connect.QrBase64;
        _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
        _store.Save(_data);
        SetWhatsAppButtonsEnabled(true);
        RefreshWhatsAppSurface();
        ShowStatus(connect.Ok && !string.IsNullOrWhiteSpace(connect.QrBase64)
            ? "QR do WhatsApp gerado. Escaneie pelo celular da loja."
            : $"WhatsApp ainda não gerou QR: {connect.Message}");
    }

    private void SetWhatsAppButtonsEnabled(bool enabled)
    {
        SettingsWhatsAppConnectButton.IsEnabled = enabled;
        WhatsAppFloatingConnectButton.IsEnabled = enabled;
        WhatsAppFloatingEmptyConnectButton.IsEnabled = enabled;
    }

    private void UpdateWhatsAppPollingState()
    {
        var shouldRun =
            _data.Settings.WhatsAppLinked ||
            !string.IsNullOrWhiteSpace(_data.Settings.WhatsAppEvolutionQrBase64) ||
            WhatsAppFloatingPanel.Visibility == Visibility.Visible;
        if (shouldRun)
        {
            if (!_whatsAppPollTimer.IsEnabled)
            {
                _whatsAppPollTimer.Start();
            }

            return;
        }

        if (_whatsAppPollTimer.IsEnabled)
        {
            _whatsAppPollTimer.Stop();
        }
    }

    private async Task CheckWhatsAppConnectionAsync(bool showPanel)
    {
        if (showPanel)
        {
            WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        }

        SetWhatsAppButtonsEnabled(false);
        ShowStatus("Checando conexão do WhatsApp...");
        try
        {
            var state = await FetchWhatsAppEvolutionStateAsync();
            _data.Settings.WhatsAppEvolutionState = state.State;
            _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
            if (state.Ok && IsWhatsAppEvolutionConnected(state.State))
            {
                var phone = NormalizeBrazilPhone(FirstFilled(
                    state.ConnectedPhone,
                    _data.Settings.WhatsAppStorePhone,
                    WhatsAppStorePhoneTextBox.Text,
                    _data.Settings.BusinessPhone,
                    _data.Settings.AccountPhone));
                ApplyWhatsAppConnectedState(state, phone);
                ShowStatus($"WhatsApp linkado: {FormatPhone(phone)}.");
                return;
            }

            _store.Save(_data);
            RefreshWhatsAppSurface();
            ShowStatus(state.Ok
                ? "WhatsApp ainda aguardando leitura do QR."
                : $"Não consegui confirmar o WhatsApp: {state.Message}");
        }
        finally
        {
            SetWhatsAppButtonsEnabled(true);
        }
    }

    private void ApplyWhatsAppConnectedState(WhatsAppEvolutionResult state, string phone)
    {
        var normalizedPhone = NormalizeBrazilPhone(phone);
        _data.Settings.WhatsAppEnabled = true;
        _data.Settings.WhatsAppLinked = true;
        _data.Settings.WhatsAppStorePhone = string.IsNullOrWhiteSpace(normalizedPhone)
            ? NormalizeBrazilPhone(_data.Settings.WhatsAppStorePhone)
            : normalizedPhone;
        _data.Settings.WhatsAppConnectedName = FirstFilled(state.ConnectedName, BusinessDisplayName());
        _data.Settings.WhatsAppLinkedAt ??= DateTime.Now;
        _data.Settings.WhatsAppLastMessageAt = DateTime.Now;
        _data.Settings.WhatsAppEvolutionState = string.IsNullOrWhiteSpace(state.State) ? "open" : state.State;
        _data.Settings.WhatsAppEvolutionQrBase64 = "";
        _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
        _store.Save(_data);
        RefreshWhatsAppSurface();
    }

    private async Task<bool> SendOrOpenWhatsAppAsync(string customerName, string phone, string message, string category)
    {
        var normalizedPhone = NormalizeBrazilPhone(phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            ShowStatus($"Telefone inválido para {customerName}.");
            return false;
        }

        if (_data.Settings.WhatsAppLinked)
        {
            var result = await SendWhatsAppEvolutionTextAsync(normalizedPhone, message);
            if (result.Ok)
            {
                AddWhatsAppMessage(customerName, normalizedPhone, message, category, "enviado");
                WhatsAppFloatingPanel.Visibility = Visibility.Visible;
                return true;
            }

            ShowStatus($"Evolution recusou o envio. Abrindo WhatsApp normal: {result.Message}");
        }

        OpenWhatsAppWeb(normalizedPhone, message);
        AddWhatsAppMessage(customerName, normalizedPhone, message, category, "aberto");
        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        return false;
    }

    private static void OpenWhatsAppWeb(string phone, string message)
    {
        var url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private async Task<WhatsAppEvolutionResult> CreateWhatsAppEvolutionInstanceAsync()
    {
        var result = await CreateWhatsAppEvolutionInstanceCoreAsync();
        if (!result.Ok && IsCloudflareOriginDown(result.Message) && TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true))
        {
            return await CreateWhatsAppEvolutionInstanceCoreAsync();
        }

        return result;
    }

    private async Task<WhatsAppEvolutionResult> CreateWhatsAppEvolutionInstanceCoreAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var payload = new
            {
                instanceName = NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName),
                qrcode = true,
                integration = "WHATSAPP-BAILEYS",
                rejectCall = true
            };
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Post, "/instance/create", payload);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode || LooksLikeEvolutionInstanceAlreadyExists(body))
            {
                return WhatsAppEvolutionResult.Success("Instância WhatsApp preparada.");
            }

            var message = ReadEvolutionMessage(body);
            return WhatsAppEvolutionResult.Fail(string.IsNullOrWhiteSpace(message)
                ? $"Servidor respondeu HTTP {(int)response.StatusCode}."
                : message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao preparar WhatsApp: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> ConnectWhatsAppEvolutionInstanceAsync()
    {
        var result = await ConnectWhatsAppEvolutionInstanceCoreAsync();
        if (!result.Ok && IsCloudflareOriginDown(result.Message) && TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true))
        {
            return await ConnectWhatsAppEvolutionInstanceCoreAsync();
        }

        return result;
    }

    private async Task<WhatsAppEvolutionResult> ConnectWhatsAppEvolutionInstanceCoreAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var path = $"/instance/connect/{Uri.EscapeDataString(NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName))}";
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Get, path);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return WhatsAppEvolutionResult.Fail(ReadEvolutionMessage(body));
            }

            return new WhatsAppEvolutionResult
            {
                Ok = true,
                Message = "QR gerado para vincular o WhatsApp.",
                QrBase64 = ReadEvolutionString(body, "base64", "qrcode", "qrCode", "code"),
                State = ReadEvolutionString(body, "state", "connectionStatus", "status")
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao gerar QR: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> FetchWhatsAppEvolutionStateAsync()
    {
        var result = await FetchWhatsAppEvolutionStateCoreAsync();
        if (!result.Ok && IsCloudflareOriginDown(result.Message) && TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true))
        {
            return await FetchWhatsAppEvolutionStateCoreAsync();
        }

        return result;
    }

    private async Task<WhatsAppEvolutionResult> FetchWhatsAppEvolutionStateCoreAsync()
    {
        if (IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            return await FetchWhatsAppGatewayStatusAsync();
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var path = $"/instance/connectionState/{Uri.EscapeDataString(NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName))}";
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Get, path);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return WhatsAppEvolutionResult.Fail(ReadEvolutionMessage(body));
            }

            var state = ReadEvolutionString(body, "state", "connectionStatus", "status");
            var result = new WhatsAppEvolutionResult
            {
                Ok = true,
                State = string.IsNullOrWhiteSpace(state) ? "desconhecido" : state,
                Message = "Estado consultado."
            };

            if (IsWhatsAppEvolutionConnected(result.State))
            {
                await FillWhatsAppEvolutionProfileAsync(client, result);
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao consultar WhatsApp: {ex.Message}");
        }
    }

    private async Task FillWhatsAppEvolutionProfileAsync(HttpClient client, WhatsAppEvolutionResult result)
    {
        try
        {
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Get, "/instance/fetchInstances");
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            result.ConnectedName = ReadEvolutionString(body, "profileName", "pushName", "displayName", "name");
            result.ConnectedPhone = NormalizeEvolutionJidPhone(ReadEvolutionString(body, "ownerJid", "jid", "number"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"WhatsApp profile skipped: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> ResetWhatsAppEvolutionInstanceAsync()
    {
        var result = await ResetWhatsAppEvolutionInstanceCoreAsync();
        if (!result.Ok && IsCloudflareOriginDown(result.Message) && TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true))
        {
            return await ResetWhatsAppEvolutionInstanceCoreAsync();
        }

        return result;
    }

    private async Task<WhatsAppEvolutionResult> ResetWhatsAppEvolutionInstanceCoreAsync()
    {
        if (IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            return await DisconnectWhatsAppGatewayAsync();
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            var path = $"/instance/delete/{Uri.EscapeDataString(NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName))}";
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Delete, path);
            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return WhatsAppEvolutionResult.Success("Instância resetada.");
            }

            var body = await response.Content.ReadAsStringAsync();
            return WhatsAppEvolutionResult.Fail(ReadEvolutionMessage(body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao resetar WhatsApp: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> ConfigureWhatsAppEvolutionWebhookAsync()
    {
        var result = await ConfigureWhatsAppEvolutionWebhookCoreAsync();
        if (!result.Ok && IsCloudflareOriginDown(result.Message) && TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true))
        {
            return await ConfigureWhatsAppEvolutionWebhookCoreAsync();
        }

        return result;
    }

    private async Task<WhatsAppEvolutionResult> ConfigureWhatsAppEvolutionWebhookCoreAsync()
    {
        if (IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            return WhatsAppEvolutionResult.Success("Webhook gerenciado pelo servidor seguro.");
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            var path = $"/webhook/set/{Uri.EscapeDataString(NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName))}";
            var payload = new
            {
                enabled = true,
                url = "http://host.docker.internal:8090/webhook/evolution",
                webhookByEvents = false,
                webhookBase64 = false,
                events = new[]
                {
                    "QRCODE_UPDATED",
                    "CONNECTION_UPDATE",
                    "MESSAGES_UPSERT",
                    "MESSAGES_UPDATE",
                    "SEND_MESSAGE"
                }
            };
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Post, path, payload);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode
                ? WhatsAppEvolutionResult.Success("Webhook do WhatsApp configurado.")
                : WhatsAppEvolutionResult.Fail(ReadEvolutionMessage(body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao configurar webhook: {ex.Message}");
        }
    }

    private async Task<WhatsAppEvolutionResult> SendWhatsAppEvolutionTextAsync(string phone, string text)
    {
        var result = await SendWhatsAppEvolutionTextCoreAsync(phone, text);
        if (!result.Ok && IsCloudflareOriginDown(result.Message) && TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true))
        {
            return await SendWhatsAppEvolutionTextCoreAsync(phone, text);
        }

        return result;
    }

    private async Task<WhatsAppEvolutionResult> SendWhatsAppEvolutionTextCoreAsync(string phone, string text)
    {
        if (IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            return await SendWhatsAppGatewayTextAsync(phone, text);
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(18) };
            var path = $"/message/sendText/{Uri.EscapeDataString(NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName))}";
            var payload = new
            {
                number = NormalizeBrazilPhone(phone),
                text,
                delay = 800,
                linkPreview = false
            };
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Post, path, payload);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return WhatsAppEvolutionResult.Success("Mensagem enviada.");
            }

            return WhatsAppEvolutionResult.Fail(ReadEvolutionMessage(body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return WhatsAppEvolutionResult.Fail($"Falha ao enviar mensagem: {ex.Message}");
        }
    }

    private async Task PollWhatsAppEvolutionMessagesAsync()
    {
        if (_whatsAppPollRunning)
        {
            return;
        }

        var canPollState =
            _data.Settings.WhatsAppLinked ||
            !string.IsNullOrWhiteSpace(_data.Settings.WhatsAppEvolutionQrBase64) ||
            WhatsAppFloatingPanel.Visibility == Visibility.Visible;
        if (!canPollState)
        {
            return;
        }

        _whatsAppPollRunning = true;
        try
        {
            var state = await FetchWhatsAppEvolutionStateAsync();
            if (state.Ok)
            {
                _data.Settings.WhatsAppEvolutionState = state.State;
                _data.Settings.WhatsAppEvolutionLastCheckedAt = DateTime.Now;
                if (IsWhatsAppEvolutionConnected(state.State))
                {
                    _data.Settings.WhatsAppConnectedName = FirstFilled(state.ConnectedName, _data.Settings.WhatsAppConnectedName, BusinessDisplayName());
                    _data.Settings.WhatsAppStorePhone = FirstFilled(state.ConnectedPhone, _data.Settings.WhatsAppStorePhone);
                    if (!_data.Settings.WhatsAppLinked)
                    {
                        var phone = NormalizeBrazilPhone(FirstFilled(
                            state.ConnectedPhone,
                            _data.Settings.WhatsAppStorePhone,
                            WhatsAppStorePhoneTextBox.Text,
                            _data.Settings.BusinessPhone,
                            _data.Settings.AccountPhone));
                        await ConfigureWhatsAppEvolutionWebhookAsync();
                        ApplyWhatsAppConnectedState(state, phone);
                    }
                }

                _store.Save(_data);
            }

            if (!state.Ok || !IsWhatsAppEvolutionConnected(state.State))
            {
                RefreshWhatsAppSurface();
                return;
            }

            // The authenticated local stream is authoritative for messages and leads.
            // Keep polling only for connection state so gateway/Supabase echoes cannot
            // create a second conversation with a malformed phone number.
            if (System.IO.File.Exists(WhatsAppLocalApiTokenPath()))
            {
                RefreshWhatsAppSurface();
                return;
            }

            var messages = await FetchWhatsAppEvolutionMessagesAsync();
            var added = 0;
            var incoming = 0;
            foreach (var message in messages
                         .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                         .OrderBy(item => item.When))
            {
                var before = _data.WhatsAppMessages.Count;
                AddWhatsAppMessage(
                    FirstFilled(message.CustomerName, FormatPhone(message.Phone), "Cliente"),
                    message.Phone,
                    message.Text,
                    message.FromMe ? "Bot" : "Atendimento",
                    message.FromMe ? "enviado" : "recebido",
                    message.FromMe ? "saida" : "entrada",
                    message.Id,
                    message.When);
                if (_data.WhatsAppMessages.Count > before)
                {
                    added++;
                    if (!message.FromMe)
                    {
                        incoming++;
                    }
                }
            }

            if (added > 0)
            {
                if (incoming > 0)
                {
                    WhatsAppFloatingPanel.Visibility = Visibility.Visible;
                }

                ShowStatus(incoming > 0
                    ? $"{incoming} mensagem(ns) nova(s) do WhatsApp."
                    : "Conversa do WhatsApp atualizada.");
            }

            RefreshWhatsAppSurface();
        }
        finally
        {
            _whatsAppPollRunning = false;
        }
    }

    private async Task<List<WhatsAppEvolutionIncomingMessage>> FetchWhatsAppEvolutionMessagesAsync()
    {
        if (IsWhatsAppGatewayEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            return await FetchWhatsAppGatewayMessagesAsync();
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            var path = $"/chat/findMessages/{Uri.EscapeDataString(NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName))}";
            var payload = new
            {
                where = new { },
                page = 1,
                limit = 50
            };
            using var request = CreateWhatsAppEvolutionRequest(HttpMethod.Post, path, payload);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
            {
                Debug.WriteLine($"WhatsApp messages failed: {(int)response.StatusCode} {ReadEvolutionMessage(body)}");
                return [];
            }

            return ParseWhatsAppEvolutionMessages(body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            Debug.WriteLine($"WhatsApp messages failed: {ex.Message}");
            return [];
        }
    }

    private HttpRequestMessage CreateWhatsAppEvolutionRequest(HttpMethod method, string path, object? payload = null)
    {
        var uri = BuildWhatsAppEvolutionUri(path)
                  ?? throw new InvalidOperationException("Endereço do WhatsApp online inválido.");
        var request = new HttpRequestMessage(method, uri);
        if (IsWhatsAppCentralProxyEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl))
        {
            request.Headers.TryAddWithoutValidation("x-balcao-license", BuildAgendaLivreWhatsAppLicense());
            request.Headers.TryAddWithoutValidation("x-balcao-machine", GetAgendaMachineFingerprint());
            request.Headers.TryAddWithoutValidation("x-balcao-machine-code", GetAgendaMachineCode());
            request.Headers.TryAddWithoutValidation("x-balcao-app-version", GetAppVersion());
            request.Headers.TryAddWithoutValidation("x-balcao-plan", "agenda-livre-whatsapp");
            request.Headers.TryAddWithoutValidation("x-balcao-expires-at", new DateTime(2035, 12, 31, 23, 59, 0, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture));
            request.Headers.TryAddWithoutValidation("x-balcao-store-name", BusinessDisplayName());
            request.Headers.TryAddWithoutValidation("x-balcao-store-phone", NormalizeBrazilPhone(FirstFilled(_data.Settings.WhatsAppStorePhone, _data.Settings.BusinessPhone, _data.Settings.AccountPhone)));
            request.Headers.TryAddWithoutValidation("x-balcao-store-document", OnlyDigits(_data.Settings.BusinessDocument));
        }
        else if (!string.IsNullOrWhiteSpace(_data.Settings.WhatsAppEvolutionApiKey))
        {
            request.Headers.TryAddWithoutValidation("apikey", _data.Settings.WhatsAppEvolutionApiKey.Trim());
        }

        if (payload is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private Uri? BuildWhatsAppEvolutionUri(string path)
    {
        var baseUrl = EnsureTrailingSlash(NormalizeWhatsAppEvolutionBaseUrl(_data.Settings.WhatsAppEvolutionBaseUrl));
        return Uri.TryCreate(new Uri(baseUrl), path.TrimStart('/'), out var uri) ? uri : null;
    }

    private bool TryApplyWhatsAppEvolutionLocalEnv(bool preferLocal)
    {
        foreach (var envFile in FindWhatsAppEvolutionEnvFiles())
        {
            if (!System.IO.File.Exists(envFile))
            {
                continue;
            }

            try
            {
                var values = ReadSimpleEnvFile(envFile);
                if (values.TryGetValue("AUTHENTICATION_API_KEY", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
                {
                    _data.Settings.WhatsAppEvolutionApiKey = apiKey.Trim();
                }

                var localUrl = FirstFilled(
                    values.TryGetValue("EVOLUTION_API_SITE", out var apiSite) ? apiSite : "",
                    values.TryGetValue("SERVER_URL", out var serverUrl) ? serverUrl : "");
                var publicUrl = FirstFilled(
                    values.TryGetValue("EVOLUTION_PUBLIC_URL", out var publicEvolutionUrl) ? publicEvolutionUrl : "",
                    values.TryGetValue("CLOUDFLARE_PUBLIC_URL", out var cloudflarePublicUrl) ? cloudflarePublicUrl : "",
                    values.TryGetValue("CLOUDFLARE_TUNNEL_URL", out var cloudflareTunnelUrl) ? cloudflareTunnelUrl : "");

                var chosen = preferLocal ? localUrl : FirstFilled(publicUrl, localUrl);
                if (!string.IsNullOrWhiteSpace(chosen))
                {
                    _data.Settings.WhatsAppEvolutionBaseUrl = NormalizeWhatsAppEvolutionBaseUrl(chosen);
                }

                _data.Settings.WhatsAppEvolutionInstanceName =
                    NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName);
                SyncWhatsAppAgendaProfileEnv(envFile);
                _store.Save(_data);
                return !string.IsNullOrWhiteSpace(_data.Settings.WhatsAppEvolutionApiKey)
                       && !IsWhatsAppCentralProxyEndpoint(_data.Settings.WhatsAppEvolutionBaseUrl);
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.WriteLine($"WhatsApp Evolution .env skipped: {ex.Message}");
            }
        }

        return false;
    }

    private static IEnumerable<string> FindWhatsAppEvolutionEnvFiles()
    {
        foreach (var root in FindWhatsAppEvolutionSearchRoots())
        {
            yield return System.IO.Path.Combine(root, "deploy", "evolution-local-windows", ".env");
            yield return System.IO.Path.Combine(root, "evolution-local-windows", ".env");
        }
    }

    private static IEnumerable<string> FindWhatsAppEvolutionSearchRoots()
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            var current = new System.IO.DirectoryInfo(root);
            for (var i = 0; current is not null && i < 8; i++, current = current.Parent)
            {
                if (seen.Add(current.FullName))
                {
                    yield return current.FullName;
                }
            }
        }
    }

    private static Dictionary<string, string> ReadSimpleEnvFile(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in System.IO.File.ReadLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            values[line[..separator].Trim()] = line[(separator + 1)..].Trim().Trim('"');
        }

        return values;
    }

    private void SyncWhatsAppAgendaProfileEnv(string envFile)
    {
        try
        {
            var instance = NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName);
            var segment = NormalizeAgendaProfileSegment(_data.Settings.BusinessSegment);
            var storeName = SanitizeAgendaProfileValue(BusinessDisplayName());
            if (string.IsNullOrWhiteSpace(instance))
            {
                return;
            }

            var content = System.IO.File.ReadAllText(envFile, Encoding.UTF8);
            var profiles = ParseSimpleEnvMap(ReadSimpleEnvValue(content, "BOT_AGENDA_INSTANCE_PROFILES"));
            profiles[instance] = $"{segment}|{storeName}";
            content = UpsertSimpleEnvLine(content, "BOT_AGENDA_INSTANCE_PROFILES", string.Join(",", profiles.Select(item => $"{item.Key}={item.Value}")));
            content = UpsertSimpleEnvLine(content, "BOT_AGENDA_LIVRE_INSTANCES", instance);
            content = UpsertSimpleEnvLine(content, "BOT_REPLY_INSTANCES", instance);
            content = UpsertSimpleEnvLine(content, "BOT_REPLY_ONLY_NEW_SECONDS", "45");
            content = UpsertSimpleEnvLine(content, "BOT_SEND_INTERACTIVE_BUTTONS", "buttons");
            content = UpsertSimpleEnvLine(content, "BOT_AGENDA_SNAPSHOT_PATH", WhatsAppAgendaSnapshotPath());
            content = UpsertSimpleEnvLine(content, "WEBHOOK_GLOBAL_ENABLED", "true");
            content = UpsertSimpleEnvLine(content, "WEBHOOK_GLOBAL_URL", "http://host.docker.internal:8090/webhook/evolution");
            content = UpsertSimpleEnvLine(content, "WEBHOOK_GLOBAL_WEBHOOK_BY_EVENTS", "false");
            System.IO.File.WriteAllText(envFile, content, new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or ArgumentException)
        {
            Debug.WriteLine($"WhatsApp agenda profile env skipped: {ex.Message}");
        }
    }

    private static Dictionary<string, string> ParseSimpleEnvMap(string value)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            var itemValue = part[(separator + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                map[key] = itemValue;
            }
        }

        return map;
    }

    private static string ReadSimpleEnvValue(string content, string key)
    {
        var prefix = $"{key}=";
        return content
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..]
            .Trim()
            .Trim('"') ?? "";
    }

    private static string UpsertSimpleEnvLine(string content, string key, string value)
    {
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        var prefix = $"{key}=";
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"{key}={value}";
                return string.Join(Environment.NewLine, lines);
            }
        }

        if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.Add("");
        }

        lines.Add($"{key}={value}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string SanitizeAgendaProfileValue(string value) =>
        value.Replace(',', ' ').Replace('|', ' ').ReplaceLineEndings(" ").Trim();

    private static string NormalizeAgendaProfileSegment(string value)
    {
        var clean = RemoveDiacritics(value).ToUpperInvariant();
        if (clean.Contains("BARBA", StringComparison.Ordinal) || clean.Contains("CABELO", StringComparison.Ordinal))
        {
            return "barbearia";
        }

        if (clean.Contains("MECAN", StringComparison.Ordinal) || clean.Contains("VEIC", StringComparison.Ordinal))
        {
            return "mecanica";
        }

        if (clean.Contains("CLIN", StringComparison.Ordinal) || clean.Contains("MEDIC", StringComparison.Ordinal))
        {
            return "clinica";
        }

        if (clean.Contains("PET", StringComparison.Ordinal) || clean.Contains("VETER", StringComparison.Ordinal))
        {
            return "petshop";
        }

        if (clean.Contains("BELEZA", StringComparison.Ordinal) || clean.Contains("UNHA", StringComparison.Ordinal))
        {
            return "beleza";
        }

        return "agenda";
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string BuildAgendaLivreWhatsAppLicense()
    {
        var machineScope = $"{WhatsAppEvolutionLicenseScope}{GetAgendaMachineCode()}";
        var message = $"BLV|{WhatsAppEvolutionLicenseExpires}|{machineScope}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WhatsAppEvolutionLicenseSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)))[..10];
        return $"BLV-{WhatsAppEvolutionLicenseExpires}-{machineScope}-{signature}";
    }

    private static string GetAgendaMachineFingerprint()
    {
        var seed = $"{Environment.MachineName}|{Environment.UserName}|AgendaLivre.Windows";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed))).ToLowerInvariant();
    }

    private static string GetAgendaMachineCode() => GetAgendaMachineFingerprint()[..8].ToUpperInvariant();

    private static string GetAppVersion() =>
        typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    private static string NormalizeWhatsAppEvolutionBaseUrl(string value)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? WhatsAppEvolutionDefaultBaseUrl : value.Trim();
        return Uri.TryCreate(EnsureTrailingSlash(clean), UriKind.Absolute, out _)
            ? clean.TrimEnd('/')
            : WhatsAppEvolutionDefaultBaseUrl;
    }

    private static bool IsWhatsAppCentralProxyEndpoint(string value)
    {
        return Uri.TryCreate(EnsureTrailingSlash(NormalizeWhatsAppEvolutionBaseUrl(value)), UriKind.Absolute, out var uri)
               && uri.Host.Contains("supabase.co", StringComparison.OrdinalIgnoreCase)
               && uri.AbsolutePath.Contains("evolution-proxy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWhatsAppGatewayEndpoint(string value)
    {
        return Uri.TryCreate(EnsureTrailingSlash(NormalizeWhatsAppEvolutionBaseUrl(value)), UriKind.Absolute, out var uri)
               && uri.Host.Contains("supabase.co", StringComparison.OrdinalIgnoreCase)
               && uri.AbsolutePath.TrimEnd('/').EndsWith("/whatsapp", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeWhatsAppEvolutionInstanceName(string value)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? "agenda-livre" : value.Trim().ToLowerInvariant();
        var builder = new StringBuilder();
        foreach (var ch in clean)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }

        clean = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(clean) ? "agenda-livre" : clean[..Math.Min(clean.Length, 48)];
    }

    private static string EnsureTrailingSlash(string value) => value.EndsWith('/') ? value : $"{value}/";

    private static bool IsWhatsAppEvolutionConnected(string state)
    {
        var clean = state.Trim().ToLowerInvariant();
        return clean is "open" or "connected" or "online" or "ready";
    }

    private static string ReadEvolutionMessage(string body)
    {
        if (IsCloudflareOriginDown(body))
        {
            return "Servidor online do WhatsApp fora do ar. Vou usar o serviço local deste computador.";
        }

        var message = ReadEvolutionString(body, "message", "error", "detail", "details", "response");
        return string.IsNullOrWhiteSpace(message) ? "Resposta sem detalhes." : message;
    }

    private static bool IsCloudflareOriginDown(string value)
    {
        return value.Contains("502 Bad Gateway", StringComparison.OrdinalIgnoreCase)
               || value.Contains("Unable to reach the origin service", StringComparison.OrdinalIgnoreCase)
               || value.Contains("cloudflared", StringComparison.OrdinalIgnoreCase)
               || value.Contains("origin service", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeEvolutionInstanceAlreadyExists(string value)
    {
        return value.Contains("already exists", StringComparison.OrdinalIgnoreCase)
               || value.Contains("ja existe", StringComparison.OrdinalIgnoreCase)
               || value.Contains("já existe", StringComparison.OrdinalIgnoreCase)
               || value.Contains("already in use", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadEvolutionString(string body, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return ReadEvolutionString(document.RootElement, names);
        }
        catch (JsonException)
        {
            return body.Trim();
        }
    }

    private static string ReadEvolutionString(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in names)
            {
                if (TryGetJsonProperty(element, name, out var property))
                {
                    var scalar = JsonScalarToString(property);
                    if (!string.IsNullOrWhiteSpace(scalar))
                    {
                        return scalar;
                    }
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var nested = ReadEvolutionString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = ReadEvolutionString(item, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return "";
    }

    private static string JsonScalarToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Number => element.ToString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => ""
    };

    private static bool TryGetJsonProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static List<WhatsAppEvolutionIncomingMessage> ParseWhatsAppEvolutionMessages(string body)
    {
        using var document = JsonDocument.Parse(body);
        var output = new List<WhatsAppEvolutionIncomingMessage>();
        foreach (var item in EnumerateJsonObjects(document.RootElement))
        {
            if (TryParseWhatsAppEvolutionMessage(item, out var message)
                && !string.IsNullOrWhiteSpace(message.Phone)
                && !string.IsNullOrWhiteSpace(message.Text)
                && output.All(existing => !string.Equals(existing.Id, message.Id, StringComparison.OrdinalIgnoreCase)))
            {
                output.Add(message);
            }
        }

        return output;
    }

    private static IEnumerable<JsonElement> EnumerateJsonObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateJsonObjects(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateJsonObjects(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool TryParseWhatsAppEvolutionMessage(JsonElement element, out WhatsAppEvolutionIncomingMessage message)
    {
        message = new WhatsAppEvolutionIncomingMessage("", "", "", "", "", DateTime.Now, false);
        var text = ReadEvolutionString(element, "conversation", "text", "body", "messageText", "caption");
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var key = TryGetJsonProperty(element, "key", out var keyElement) ? keyElement : default;
        var remoteJid = key.ValueKind == JsonValueKind.Object
            ? ReadEvolutionString(key, "remoteJid", "participant")
            : ReadEvolutionString(element, "remoteJid", "jid", "from");
        var phone = NormalizeEvolutionJidPhone(remoteJid);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var id = FirstFilled(
            key.ValueKind == JsonValueKind.Object ? ReadEvolutionString(key, "id") : "",
            ReadEvolutionString(element, "id", "messageId"));
        var fromMe = key.ValueKind == JsonValueKind.Object && ReadEvolutionBool(key, "fromMe");
        if (!fromMe)
        {
            fromMe = ReadEvolutionBool(element, "fromMe");
        }

        message = new WhatsAppEvolutionIncomingMessage(
            string.IsNullOrWhiteSpace(id) ? $"{phone}:{text.GetHashCode(StringComparison.Ordinal)}" : id,
            FirstFilled(ReadEvolutionString(element, "pushName", "senderName", "name"), FormatPhone(phone)),
            phone,
            text,
            remoteJid,
            ReadEvolutionDate(element),
            fromMe);
        return true;
    }

    private static bool ReadEvolutionBool(JsonElement element, string name)
    {
        if (!TryGetJsonProperty(element, name, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static DateTime ReadEvolutionDate(JsonElement element)
    {
        var raw = ReadEvolutionString(element, "messageTimestamp", "timestamp", "createdAt", "dateTime", "datetime");
        if (long.TryParse(OnlyDigits(raw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > 0)
        {
            var offset = number > 9_999_999_999
                ? DateTimeOffset.FromUnixTimeMilliseconds(number)
                : DateTimeOffset.FromUnixTimeSeconds(number);
            return offset.LocalDateTime;
        }

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : DateTime.Now;
    }

    private static string NormalizeEvolutionJidPhone(string jid)
    {
        var clean = jid.Trim();
        var at = clean.IndexOf('@', StringComparison.Ordinal);
        if (at >= 0)
        {
            clean = clean[..at];
        }

        var colon = clean.IndexOf(':', StringComparison.Ordinal);
        if (colon >= 0)
        {
            clean = clean[..colon];
        }

        return NormalizeBrazilPhone(clean);
    }

    private decimal SumRealizedRevenue(DateTime start, DateTime end) =>
        _data.Appointments
            .Where(item => item.Start >= start && item.Start < end && item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);

    private static double Percent(decimal current, decimal total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return (double)Math.Clamp(current / total * 100m, 0m, 100m);
    }

    private static string GreetingFor(DateTime dateTime)
    {
        if (dateTime.Hour < 12)
        {
            return "Bom dia";
        }

        return dateTime.Hour < 18 ? "Boa tarde" : "Boa noite";
    }

    private static string FirstName(string name)
    {
        var cleanName = name.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            return "tudo certo?";
        }

        return cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private void UpdateSegmentFilterButton()
    {
        SegmentFilterButton.Content = _selectedSegmentFilter == AllSegments
            ? "Todos segmentos"
            : _selectedSegmentFilter;
    }

    private void UpdateDateFilterButton()
    {
        DateFilterButton.Content = DateShortcutLabel(_selectedDate);
    }

    private static string DateShortcutLabel(DateTime date)
    {
        var today = DateTime.Today;
        if (date.Date == today)
        {
            return $"Hoje, {date:dd/MM}";
        }

        if (date.Date == today.AddDays(1))
        {
            return $"Amanhã, {date:dd/MM}";
        }

        if (date.Date == today.AddDays(-1))
        {
            return $"Ontem, {date:dd/MM}";
        }

        return date.ToString("ddd, dd/MM", Brazil);
    }

    private void SelectDate(DateTime date)
    {
        _selectedDate = date.Date;

        if (_selectedAppointment is null)
        {
            AppointmentDatePicker.SelectedDate = _selectedDate;
        }

        UpdateDateFilterButton();
        RefreshAll();
    }

    private void BuildScheduleBoard()
    {
        if (ScheduleBoardGrid is null)
        {
            return;
        }

        ScheduleBoardGrid.Children.Clear();
        ScheduleBoardGrid.ColumnDefinitions.Clear();
        ScheduleBoardGrid.RowDefinitions.Clear();

        var dayAppointments = AgendaDisplayAppointments();

        var professionals = GetBoardProfessionals(dayAppointments).ToList();
        if (professionals.Count == 0)
        {
            ScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(280) });
            ScheduleBoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var empty = new TextBlock
            {
                Text = "Cadastre profissionais para montar o quadro da agenda.",
                Foreground = MutedBrush,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            ScheduleBoardGrid.Children.Add(empty);
            return;
        }

        var dayStart = _selectedDate.Date.AddHours(_data.Settings.WorkdayStartHour);
        var dayEnd = _selectedDate.Date.AddHours(_data.Settings.WorkdayEndHour);
        var slotCount = Math.Max(1, (int)Math.Ceiling((dayEnd - dayStart).TotalMinutes / 30));

        ScheduleBoardGrid.MinWidth = Math.Max(620, ScheduleTimeColumnWidth + professionals.Count * ScheduleProfessionalColumnWidth);
        ScheduleBoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ScheduleTimeColumnWidth) });
        var stretchColumns = professionals.Count <= 2;
        foreach (var _ in professionals)
        {
            ScheduleBoardGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = stretchColumns
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(ScheduleProfessionalColumnWidth)
            });
        }

        ScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ScheduleHeaderHeight) });
        for (var index = 0; index < slotCount; index++)
        {
            ScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ScheduleSlotHeight) });
        }

        AddScheduleCorner(dayStart);
        AddScheduleHeaders(professionals);
        AddScheduleCells(dayStart, slotCount, professionals);
        AddScheduleAppointments(dayStart, slotCount, professionals, dayAppointments);

        if (!dayAppointments.Any(IsOperationalStatus))
        {
            AddScheduleEmptyState(slotCount, professionals.Count);
        }
    }

    private IEnumerable<Professional> GetBoardProfessionals(IReadOnlyCollection<Appointment> dayAppointments)
    {
        var eligible = GetProfessionalsForCurrentFilter().ToList();
        if (eligible.Count == 0)
        {
            return [];
        }

        var appointmentProfessionalIds = dayAppointments
            .Where(item => !string.IsNullOrWhiteSpace(item.ProfessionalId))
            .Select(item => item.ProfessionalId)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return eligible
            .OrderByDescending(item => appointmentProfessionalIds.Contains(item.Id))
            .ThenBy(item => item.Name);
    }

    private void AddScheduleCorner(DateTime dayStart)
    {
        var corner = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(10, 0, 10, 0),
            Child = new TextBlock
            {
                Text = "Horário",
                Foreground = InkBrush,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        ScheduleBoardGrid.Children.Add(corner);
    }

    private void AddScheduleHeaders(IReadOnlyList<Professional> professionals)
    {
        for (var index = 0; index < professionals.Count; index++)
        {
            var professional = professionals[index];
            var header = new Border
            {
                Background = WarmSoftBrush,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(12, 6, 12, 6),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new Border
                        {
                            Width = 30,
                            Height = 30,
                            Background = AccentSoftBrush,
                            CornerRadius = new CornerRadius(15),
                            Margin = new Thickness(0, 0, 9, 0),
                            Child = new TextBlock
                            {
                                Text = InitialsFor(professional.Name),
                                Foreground = AccentBrush,
                                FontSize = 10.5,
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        },
                        new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = professional.Name,
                                    Foreground = InkBrush,
                                    FontSize = 12.5,
                                    FontWeight = FontWeights.Bold,
                                    TextTrimming = TextTrimming.CharacterEllipsis
                                },
                                new TextBlock
                                {
                                    Text = professional.SegmentLine,
                                    Foreground = MutedBrush,
                                    FontSize = 10,
                                    TextTrimming = TextTrimming.CharacterEllipsis,
                                    Margin = new Thickness(0, 2, 0, 0)
                                }
                            }
                        }
                    }
                }
            };

            Grid.SetRow(header, 0);
            Grid.SetColumn(header, index + 1);
            ScheduleBoardGrid.Children.Add(header);
        }
    }

    private void AddScheduleCells(DateTime dayStart, int slotCount, IReadOnlyList<Professional> professionals)
    {
        for (var row = 0; row < slotCount; row++)
        {
            var slotStart = dayStart.AddMinutes(row * 30);
            var timeCell = new Border
            {
                Background = WarmSoftBrush,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(7, 7, 10, 0),
                Child = new TextBlock
                {
                    Text = slotStart.ToString("HH:mm", Brazil),
                    Foreground = MutedBrush,
                    FontSize = 11.5,
                    FontWeight = FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Right
                }
            };

            Grid.SetRow(timeCell, row + 1);
            Grid.SetColumn(timeCell, 0);
            ScheduleBoardGrid.Children.Add(timeCell);

            for (var column = 0; column < professionals.Count; column++)
            {
                var professional = professionals[column];
                var cell = new Border
                {
                    Background = PanelBrush,
                    BorderBrush = LineBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Cursor = Cursors.Hand,
                    Tag = new ScheduleSlot(professional, slotStart)
                };
                cell.MouseLeftButtonDown += ScheduleEmptySlot_MouseLeftButtonDown;

                Grid.SetRow(cell, row + 1);
                Grid.SetColumn(cell, column + 1);
                ScheduleBoardGrid.Children.Add(cell);
            }
        }
    }

    private void AddScheduleAppointments(
        DateTime dayStart,
        int slotCount,
        IReadOnlyList<Professional> professionals,
        IReadOnlyCollection<Appointment> appointments)
    {
        var professionalColumns = professionals
            .Select((professional, index) => new { professional.Id, Column = index + 1 })
            .ToDictionary(item => item.Id, item => item.Column, StringComparer.OrdinalIgnoreCase);

        foreach (var appointment in appointments)
        {
            if (!professionalColumns.TryGetValue(appointment.ProfessionalId, out var column))
            {
                continue;
            }

            var minutesFromStart = (appointment.Start - dayStart).TotalMinutes;
            var row = (int)Math.Floor(minutesFromStart / 30) + 1;
            if (row < 1 || row > slotCount)
            {
                continue;
            }

            var rowSpan = Math.Max(1, (int)Math.Ceiling(appointment.DurationMinutes / 30d));
            rowSpan = Math.Min(rowSpan, slotCount - row + 1);

            var card = CreateScheduleAppointmentCard(appointment, rowSpan);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, column);
            Grid.SetRowSpan(card, rowSpan);
            Panel.SetZIndex(card, 5);
            ScheduleBoardGrid.Children.Add(card);
        }
    }

    private void AddScheduleEmptyState(int slotCount, int professionalCount)
    {
        var action = new Button
        {
            Content = "+ Agendar horário",
            Height = 34,
            MinWidth = 128,
            Padding = new Thickness(12, 0, 12, 0),
            Style = (Style)FindResource("CommandButton"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        action.Click += NewButton_Click;

        var empty = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18),
            MaxWidth = 420,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 20, 0, 0),
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new Border
                    {
                        Width = 42,
                        Height = 42,
                        Background = AccentSoftBrush,
                        CornerRadius = new CornerRadius(12),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child = new PackIcon
                        {
                            Kind = PackIconKind.CalendarPlus,
                            Foreground = AccentBrush,
                            Width = 22,
                            Height = 22,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock
                    {
                        Text = "Agenda livre nesta data",
                        Foreground = InkBrush,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 12, 0, 0)
                    },
                    new TextBlock
                    {
                        Text = "Clique em um horário no quadro ou use o botão abaixo para criar o primeiro atendimento.",
                        Foreground = MutedBrush,
                        FontSize = 12.5,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 330,
                        Margin = new Thickness(0, 5, 0, 0)
                    },
                    action
                }
            }
        };

        const int startRow = 1;
        Grid.SetRow(empty, startRow);
        Grid.SetRowSpan(empty, Math.Min(6, Math.Max(1, slotCount - startRow + 1)));
        Grid.SetColumn(empty, 1);
        Grid.SetColumnSpan(empty, Math.Max(1, professionalCount));
        Panel.SetZIndex(empty, 10);
        ScheduleBoardGrid.Children.Add(empty);
    }

    private Border CreateScheduleAppointmentCard(Appointment appointment, int rowSpan)
    {
        var statusBrush = ScheduleCardBackground(appointment.Status);
        var accentBrush = ScheduleAccentFor(appointment.Status);
        var compact = rowSpan <= 1;
        var visualHeight = compact
            ? Math.Max(32, ScheduleSlotHeight - 4)
            : Math.Min(rowSpan * ScheduleSlotHeight - 6, Math.Max(50, rowSpan * 26));
        var card = new Border
        {
            Margin = compact ? new Thickness(7, 2, 7, 2) : new Thickness(7, 3, 7, 3),
            Height = visualHeight,
            Padding = new Thickness(0),
            Background = statusBrush,
            BorderBrush = Solid("#FFFFFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new ScaleTransform(1, 1),
            RenderTransformOrigin = new Point(0.5, 0.5),
            Tag = appointment,
            ToolTip = $"{appointment.Start:HH:mm}-{appointment.End:HH:mm} | {appointment.CustomerName} | {appointment.ServiceName}",
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(23, 20, 17),
                BlurRadius = 8,
                ShadowDepth = 1,
                Opacity = 0.04
            }
        };
        card.PreviewMouseLeftButtonDown += ScheduleAppointment_MouseLeftButtonDown;
        card.MouseEnter += (_, _) => AnimateScheduleCard(card, 1.012);
        card.MouseLeave += (_, _) => AnimateScheduleCard(card, 1.0);

        var grid = new Grid
        {
            ClipToBounds = true
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var stripe = new Border
        {
            Background = accentBrush,
            CornerRadius = new CornerRadius(7, 0, 0, 7)
        };
        Grid.SetColumn(stripe, 0);
        grid.Children.Add(stripe);

        var stack = new StackPanel
        {
            ClipToBounds = true,
            Margin = compact ? new Thickness(8, 1, 7, 1) : new Thickness(9, 4, 8, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(stack, 1);

        var header = new Grid { Margin = compact ? new Thickness(0) : new Thickness(0, 0, 0, 2) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new TextBlock
        {
            Text = appointment.ServiceName,
            Foreground = InkBrush,
            FontSize = compact ? 10 : 11.2,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        });

        var badge = new Border
        {
            Background = Solid("#FFFFFF"),
            CornerRadius = new CornerRadius(9),
            Padding = compact ? new Thickness(4, 1, 4, 1) : new Thickness(5, 1, 5, 1),
            MaxWidth = compact ? 66 : 78,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
            Child = new TextBlock
            {
                Text = ScheduleStatusLabel(appointment.Status),
                Foreground = accentBrush,
                FontSize = compact ? 7.3 : 8.2,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
        Grid.SetColumn(badge, 1);
        header.Children.Add(badge);
        stack.Children.Add(header);

        stack.Children.Add(new TextBlock
        {
            Text = appointment.CustomerName,
            Foreground = MutedBrush,
            FontSize = compact ? 8.5 : 9.4,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 0)
        });

        if (rowSpan > 1 && !string.IsNullOrWhiteSpace(appointment.ResourceName))
        {
            stack.Children.Add(new TextBlock
            {
                Text = appointment.ResourceName,
                Foreground = accentBrush,
                FontSize = 8.8,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 3, 0, 0)
            });
        }

        grid.Children.Add(stack);
        card.Child = grid;
        return card;
    }

    private static void AnimateScheduleCard(Border card, double scale)
    {
        if (card.RenderTransform is not ScaleTransform transform)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            To = scale,
            Duration = TimeSpan.FromMilliseconds(110),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static Brush ScheduleCardBackground(AppointmentStatus status)
    {
        if (IsActiveBarberMidnight())
        {
            return status switch
            {
                AppointmentStatus.Confirmed => Solid("#303941"),
                AppointmentStatus.Waiting => Solid("#E9ECEF"),
                AppointmentStatus.Scheduled => Solid("#F1F2F4"),
                AppointmentStatus.InService => Solid("#EDF1F0"),
                AppointmentStatus.Done => Solid("#EDF1F0"),
                AppointmentStatus.Cancelled or AppointmentStatus.NoShow => Solid("#F8EDED"),
                AppointmentStatus.Blocked => Solid("#ECEFF2"),
                _ => Solid("#F1F2F4")
            };
        }

        return status switch
        {
            AppointmentStatus.Scheduled => WarmSoftBrush,
            AppointmentStatus.Confirmed => AccentSoftBrush,
            AppointmentStatus.Waiting => BlueSoftBrush,
            AppointmentStatus.InService => Solid("#ECFDF5"),
            AppointmentStatus.Done => Solid("#ECFDF5"),
            AppointmentStatus.Cancelled or AppointmentStatus.NoShow => RedSoftBrush,
            AppointmentStatus.Blocked => GraySoftBrush,
            _ => AccentSoftBrush
        };
    }

    private static Brush ScheduleAccentFor(AppointmentStatus status)
    {
        if (IsActiveBarberMidnight())
        {
            return status switch
            {
                AppointmentStatus.Confirmed => Solid("#111820"),
                AppointmentStatus.Waiting => Solid("#87919B"),
                AppointmentStatus.Scheduled => Solid("#202830"),
                AppointmentStatus.InService => Solid("#47525C"),
                AppointmentStatus.Done => Solid("#47525C"),
                AppointmentStatus.Cancelled or AppointmentStatus.NoShow => Solid("#991B1B"),
                AppointmentStatus.Blocked => Solid("#64748B"),
                _ => AccentBrush
            };
        }

        return status switch
        {
            AppointmentStatus.Scheduled => AccentBrush,
            AppointmentStatus.Confirmed => AccentBrush,
            AppointmentStatus.Waiting => AccentBrush,
            AppointmentStatus.InService => Solid("#10B981"),
            AppointmentStatus.Done => Solid("#16A34A"),
            AppointmentStatus.Cancelled or AppointmentStatus.NoShow => Solid("#DC2626"),
            AppointmentStatus.Blocked => Solid("#64748B"),
            _ => AccentBrush
        };
    }

    private static Brush ScheduleTextFor(AppointmentStatus status) =>
        IsActiveBarberMidnight() && status == AppointmentStatus.Confirmed
            ? Solid("#FFFFFF")
            : InkBrush;

    private static Brush ScheduleSubtextFor(AppointmentStatus status) =>
        IsActiveBarberMidnight() && status == AppointmentStatus.Confirmed
            ? Solid("#DDE3E8")
            : MutedBrush;

    private static string ScheduleStatusLabel(AppointmentStatus status) =>
        status == AppointmentStatus.Scheduled ? "Pendente" : StatusLabel(status);

    private IEnumerable<Appointment> ApplyFilters(IEnumerable<Appointment> source)
    {
        var filtered = ApplySegmentFilter(source);
        var search = SearchTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            return filtered;
        }

        return filtered.Where(item => Contains(item.CustomerName, search) ||
                                      Contains(item.CustomerPhone, search) ||
                                      Contains(item.CustomerProfile, search) ||
                                      Contains(item.ServiceName, search) ||
                                      Contains(item.ProfessionalName, search) ||
                                      Contains(item.ResourceName, search) ||
                                      Contains(item.Notes, search));
    }

    private IEnumerable<Appointment> ApplySegmentFilter(IEnumerable<Appointment> source)
    {
        var segment = CurrentSegmentFilter();
        return segment == AllSegments ? source : source.Where(item => item.Segment == segment);
    }

    private IEnumerable<Professional> GetProfessionalsForCurrentFilter()
    {
        var segment = CurrentSegmentFilter();
        return segment == AllSegments
            ? _data.Professionals.Where(item => item.IsActive)
            : _data.Professionals.Where(item => item.IsActive && item.Segments.Contains(segment));
    }

    private static bool IsPreviewAppointment(Appointment appointment) =>
        appointment.Id.StartsWith(PreviewAppointmentPrefix, StringComparison.Ordinal);

    private string CurrentSegmentFilter() => _selectedSegmentFilter;

    private void UpdateAppointmentOptions(string? segment)
    {
        segment = string.IsNullOrWhiteSpace(segment) ? GetAvailableSegments()[0] : segment;
        var selectedService = ServiceCombo.SelectedItem as ServiceItem;

        _filteredServices.Clear();
        foreach (var serviceItem in ServicesForEditor(segment))
        {
            _filteredServices.Add(serviceItem);
        }

        _filteredProfessionals.Clear();
        foreach (var professional in _data.Professionals.Where(item => item.IsActive && item.Segments.Contains(segment)).OrderBy(item => item.Name))
        {
            _filteredProfessionals.Add(professional);
        }

        _resourceOptions.Clear();
        var resources = _data.Settings.Resources
            .Concat(_data.Services.Where(item => item.Segment == segment).Select(item => item.DefaultResource))
            .Concat(_data.Appointments.Where(item => item.Segment == segment).Select(item => item.ResourceName))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item);

        foreach (var resource in resources)
        {
            _resourceOptions.Add(resource);
        }

        if (segment == _data.Settings.BusinessSegment && !string.IsNullOrWhiteSpace(_data.Settings.ClientDetailLabel))
        {
            ClientSectionTitle.Text = _data.Settings.ClientLabel;
            ProfileLabelText.Text = _data.Settings.ClientDetailLabel;
            ResourceLabelText.Text = _data.Settings.ResourceLabel;
        }
        else
        {
            ProfileLabelText.Text = segment switch
            {
                "Clínica médica" => "Paciente / prontuário",
                "Petshop" => "Tutor / pet / raça",
                "Mecânica" => "Cliente / veículo / placa",
                "Unha e beleza" => "Cliente / preferência",
                "Cabelo e barbearia" => "Cliente / estilo",
                _ => "Cliente / detalhe"
            };
        }

        if (_loadingEditor)
        {
            return;
        }

        if (selectedService is not null)
        {
            ServiceCombo.SelectedItem =
                _filteredServices.FirstOrDefault(item => !string.IsNullOrWhiteSpace(selectedService.Id) &&
                                                         item.Id.Equals(selectedService.Id, StringComparison.OrdinalIgnoreCase)) ??
                _filteredServices.FirstOrDefault(item => item.Name.Equals(selectedService.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (ServiceCombo.SelectedItem is not ServiceItem)
        {
            ServiceCombo.SelectedItem = _filteredServices.FirstOrDefault();
        }

        if (ProfessionalCombo.SelectedItem is not Professional)
        {
            ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault();
        }

        if (ServiceCombo.SelectedItem is ServiceItem service)
        {
            ApplyServiceDefaults(service);
        }
    }

    private IEnumerable<ServiceItem> ServicesForEditor(string segment)
    {
        var singleSegmentAccount = GetAvailableSegments().Count <= 1;
        var rows = new Dictionary<string, ServiceItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in _data.Services
                     .Where(item => item.IsActive)
                     .Where(item => singleSegmentAccount || item.Segment.Equals(segment, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(item => item.Name))
        {
            if (!string.IsNullOrWhiteSpace(service.Name))
            {
                rows.TryAdd(service.Name.Trim(), service);
            }
        }

        foreach (var appointment in _data.Appointments
                     .Where(item => !IsPreviewAppointment(item))
                     .Where(item => item.Status != AppointmentStatus.Blocked)
                     .Where(item => singleSegmentAccount || item.Segment.Equals(segment, StringComparison.OrdinalIgnoreCase))
                     .Where(item => !string.IsNullOrWhiteSpace(item.ServiceName))
                     .OrderByDescending(item => item.UpdatedAt))
        {
            var serviceName = appointment.ServiceName.Trim();
            if (serviceName.Equals("Bloqueio interno", StringComparison.OrdinalIgnoreCase) || rows.ContainsKey(serviceName))
            {
                continue;
            }

            rows[serviceName] = new ServiceItem
            {
                Id = string.IsNullOrWhiteSpace(appointment.ServiceId)
                    ? $"saved_{StableHash(serviceName)}"
                    : appointment.ServiceId,
                Segment = string.IsNullOrWhiteSpace(appointment.Segment) ? segment : appointment.Segment,
                Name = serviceName,
                Category = "Atendimento",
                DurationMinutes = Math.Clamp(appointment.DurationMinutes, 5, 480),
                Price = appointment.Price,
                DefaultResource = appointment.ResourceName,
                IsActive = true
            };
        }

        return rows.Values
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.DurationMinutes);
    }

    private static string StableHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()));
        return Convert.ToHexString(bytes, 0, 6).ToLowerInvariant();
    }

    private void ClearEditor()
    {
        _loadingEditor = true;
        _selectedAppointment = null;
        _syncingSelection = true;
        DayAgendaList.SelectedItem = null;
        WeekAgendaList.SelectedItem = null;
        _syncingSelection = false;

        var availableSegments = GetAvailableSegments();
        var segment = CurrentSegmentFilter() == AllSegments ? availableSegments[0] : CurrentSegmentFilter();
        AppointmentSegmentCombo.SelectedItem = segment;
        UpdateAppointmentOptions(segment);

        CustomerNameTextBox.Text = "";
        CustomerProfileTextBox.Text = "";
        PhoneTextBox.Text = "";
        NotesTextBox.Text = "";
        PriceTextBox.Text = "";

        ServiceCombo.SelectedItem = _filteredServices.FirstOrDefault();
        ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault();
        var suggestedDuration = 30;
        if (ServiceCombo.SelectedItem is ServiceItem service)
        {
            ApplyServiceDefaults(service);
            suggestedDuration = service.DurationMinutes;
        }
        else
        {
            DurationCombo.SelectedItem = 30;
        }

        var suggestedStart = SuggestedStartFor(_selectedDate, suggestedDuration);
        AppointmentDatePicker.SelectedDate = suggestedStart.Date;
        TimeCombo.Text = suggestedStart.ToString("HH:mm", Brazil);

        EditorTitleText.Text = "Novo agendamento";
        EditorStatusText.Text = "Preencha o horário, o serviço e os dados do cliente.";
        ClearAppointmentEditorAlert();
        SelectedAppointmentCard.Visibility = Visibility.Collapsed;
        ExistingAppointmentActionsPanel.Visibility = Visibility.Collapsed;
        _loadingEditor = false;
        RefreshAppointmentEditorSummary();
    }

    private void LoadEditor(Appointment appointment)
    {
        _loadingEditor = true;
        _selectedAppointment = appointment;

        AppointmentSegmentCombo.SelectedItem = appointment.Segment;
        UpdateAppointmentOptions(appointment.Segment);

        AppointmentDatePicker.SelectedDate = appointment.Start.Date;
        TimeCombo.Text = appointment.Start.ToString("HH:mm", Brazil);

        if (!_durationOptions.Contains(appointment.DurationMinutes))
        {
            DurationCombo.Text = appointment.DurationMinutes.ToString(Brazil);
        }
        else
        {
            DurationCombo.SelectedItem = appointment.DurationMinutes;
        }

        ServiceCombo.SelectedItem = _filteredServices.FirstOrDefault(item => item.Id == appointment.ServiceId)
                                    ?? _filteredServices.FirstOrDefault(item => item.Name == appointment.ServiceName);
        ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault(item => item.Id == appointment.ProfessionalId)
                                         ?? _filteredProfessionals.FirstOrDefault(item => item.Name == appointment.ProfessionalName);
        SelectResource(appointment.ResourceName);
        CustomerNameTextBox.Text = appointment.Status == AppointmentStatus.Blocked ? "" : appointment.CustomerName;
        CustomerProfileTextBox.Text = appointment.CustomerProfile;
        PhoneTextBox.Text = appointment.CustomerPhone;
        PriceTextBox.Text = appointment.Price.ToString("N2", Brazil);
        NotesTextBox.Text = appointment.Notes;

        EditorTitleText.Text = appointment.Status == AppointmentStatus.Blocked ? "Bloqueio de horário" : "Editar agendamento";
        EditorStatusText.Text = $"{StatusLabel(appointment.Status)} | criado em {appointment.CreatedAt:dd/MM HH:mm}";
        ClearAppointmentEditorAlert();
        ExistingAppointmentActionsPanel.Visibility = Visibility.Visible;
        ShowSelectedAppointment(appointment);
        _loadingEditor = false;
        RefreshAppointmentEditorSummary();
    }

    private void OpenAppointmentEditorModal()
    {
        if (AppointmentEditorOverlay.Visibility != Visibility.Visible)
        {
            _appointmentEditorPreviousFocus = Keyboard.FocusedElement;
        }

        WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
        AppointmentEditorOverlay.Visibility = Visibility.Visible;
        SetAppointmentEditorStep(0, focusFirst: true);
        RefreshWhatsAppLauncherVisibility();
    }

    private void CloseAppointmentEditorModal()
    {
        AppointmentEditorOverlay.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();

        var previousFocus = _appointmentEditorPreviousFocus;
        _appointmentEditorPreviousFocus = null;
        if (previousFocus is not null)
        {
            Dispatcher.BeginInvoke(
                () => Keyboard.Focus(previousFocus),
                DispatcherPriority.Input);
        }
    }

    private void CloseAppointmentModalButton_Click(object sender, RoutedEventArgs e)
    {
        CloseAppointmentEditorModal();
    }

    private bool FailAppointmentEditor(string message, Control? focusTarget = null)
    {
        if (focusTarget is not null && AppointmentEditorOverlay.Visibility == Visibility.Visible)
        {
            var targetStep = focusTarget == CustomerNameTextBox ||
                             focusTarget == PhoneTextBox ||
                             focusTarget == CustomerProfileTextBox ||
                             focusTarget == NotesTextBox
                ? 1
                : 0;
            SetAppointmentEditorStep(targetStep);
        }

        ShowAppointmentEditorAlert(message, error: true);
        ShowStatus(message);
        if (focusTarget is not null)
        {
            Dispatcher.BeginInvoke(
                () => focusTarget.Focus(),
                DispatcherPriority.Background);
        }

        return false;
    }

    private void SetAppointmentEditorStep(int step, bool focusFirst = false)
    {
        step = Math.Clamp(step, 0, 2);
        _appointmentEditorStep = step;

        AppointmentScheduleStep.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        AppointmentClientStep.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        AppointmentConfirmStep.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;

        AppointmentClearStepButton.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        AppointmentBackStepButton.Visibility = step > 0 ? Visibility.Visible : Visibility.Collapsed;
        AppointmentContinueButton.Visibility = step < 2 ? Visibility.Visible : Visibility.Collapsed;
        AppointmentSaveStepButton.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;

        UpdateAppointmentStepIndicators();
        RefreshAppointmentEditorSummary();
        AppointmentEditorScrollViewer.ScrollToTop();

        if (!focusFirst)
        {
            return;
        }

        Control firstControl = step switch
        {
            0 => AppointmentDatePicker,
            1 => CustomerNameTextBox,
            _ => AppointmentSaveStepButton
        };
        Dispatcher.BeginInvoke(
            () => firstControl.Focus(),
            DispatcherPriority.Background);
    }

    private void UpdateAppointmentStepIndicators()
    {
        var circles = new[]
        {
            AppointmentStepOneCircle,
            AppointmentStepTwoCircle,
            AppointmentStepThreeCircle
        };
        var numbers = new[]
        {
            AppointmentStepOneNumber,
            AppointmentStepTwoNumber,
            AppointmentStepThreeNumber
        };
        var labels = new[]
        {
            AppointmentStepOneLabel,
            AppointmentStepTwoLabel,
            AppointmentStepThreeLabel
        };

        for (var index = 0; index < circles.Length; index++)
        {
            var reached = index <= _appointmentEditorStep;
            circles[index].SetResourceReference(Border.BackgroundProperty, reached ? "AccentDark" : "Panel");
            circles[index].SetResourceReference(Border.BorderBrushProperty, reached ? "AccentDark" : "Line");
            if (reached)
            {
                numbers[index].Foreground = Brushes.White;
            }
            else
            {
                numbers[index].SetResourceReference(TextBlock.ForegroundProperty, "Muted");
            }

            labels[index].SetResourceReference(
                TextBlock.ForegroundProperty,
                index == _appointmentEditorStep ? "Ink" : "Muted");
            labels[index].FontWeight = index == _appointmentEditorStep ? FontWeights.SemiBold : FontWeights.Normal;
        }

        AppointmentStepConnectorOne.SetResourceReference(
            Border.BackgroundProperty,
            _appointmentEditorStep >= 1 ? "AccentDark" : "Line");
        AppointmentStepConnectorTwo.SetResourceReference(
            Border.BackgroundProperty,
            _appointmentEditorStep >= 2 ? "AccentDark" : "Line");
    }

    private void AppointmentStepButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            int.TryParse(button.Tag?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var step))
        {
            SetAppointmentEditorStep(step, focusFirst: true);
        }
    }

    private void AppointmentBackStepButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppointmentEditorStep(_appointmentEditorStep - 1, focusFirst: true);
    }

    private void AppointmentContinueButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppointmentEditorStep(_appointmentEditorStep + 1, focusFirst: true);
    }

    private void AppointmentSummaryInput_Changed(object sender, RoutedEventArgs e)
    {
        RefreshAppointmentEditorSummary();
    }

    private void RefreshAppointmentEditorSummary()
    {
        if (AppointmentSummaryDateTimeText is null ||
            AppointmentDatePicker is null ||
            TimeCombo is null ||
            DurationCombo is null)
        {
            return;
        }

        var dateText = AppointmentDatePicker.SelectedDate is DateTime date
            ? date.ToString("dd/MM/yyyy", Brazil)
            : "Data não definida";
        var timeText = FirstFilled(TimeCombo.Text, "--:--");
        var durationText = DurationCombo.SelectedItem is int duration
            ? $"{duration} min"
            : string.IsNullOrWhiteSpace(DurationCombo.Text)
                ? "duração não definida"
                : $"{DurationCombo.Text.Trim()} min";
        var dateTimeText = $"{dateText} • {timeText} • {durationText}";

        var service = ServiceCombo.SelectedItem as ServiceItem;
        var serviceText = service?.DisplayName ?? FirstFilled(ServiceCombo.Text, "Serviço ainda não selecionado");
        var professionalText = ProfessionalCombo.SelectedItem is Professional professional
            ? professional.Name
            : FirstFilled(ProfessionalCombo.Text, "Profissional não definido");
        var resourceText = FirstFilled(CurrentResourceText(), "Recurso não definido");
        var professionalAndResourceText = $"{professionalText} • {resourceText}";

        var price = service?.Price ?? 0m;
        if (TryParseMoney(PriceTextBox.Text, out var typedPrice))
        {
            price = typedPrice;
        }

        var priceText = price.ToString("C", Brazil);
        var customerText = FirstFilled(CustomerNameTextBox.Text, "Cliente não informado");
        var phoneText = FirstFilled(PhoneTextBox.Text, "Telefone não informado");

        AppointmentSummaryDateTimeText.Text = dateTimeText;
        AppointmentSummaryServiceText.Text = serviceText;
        AppointmentSummaryResourceText.Text = professionalAndResourceText;
        AppointmentSummaryPriceText.Text = priceText;

        AppointmentClientContextTitle.Text = serviceText;
        AppointmentClientContextDetail.Text = $"{dateTimeText} • {professionalAndResourceText}";
        AppointmentClientContextPrice.Text = priceText;

        AppointmentConfirmDateTimeText.Text = dateTimeText;
        AppointmentConfirmServiceText.Text = serviceText;
        AppointmentConfirmProfessionalText.Text = professionalAndResourceText;
        AppointmentConfirmCustomerText.Text = $"{customerText} • {phoneText}";
        AppointmentConfirmPriceText.Text = priceText;
    }

    private void ShowAppointmentEditorAlert(string message, bool error)
    {
        AppointmentRuleAlert.Visibility = Visibility.Visible;
        if (error)
        {
            AppointmentRuleAlert.Background = Solid("#FEF2F2");
            AppointmentRuleAlert.BorderBrush = Solid("#FCA5A5");
        }
        else
        {
            AppointmentRuleAlert.SetResourceReference(Border.BackgroundProperty, "AccentSoft");
            AppointmentRuleAlert.SetResourceReference(Border.BorderBrushProperty, "Line");
        }

        AppointmentRuleText.Text = message;
    }

    private void ClearAppointmentEditorAlert()
    {
        AppointmentRuleAlert.Visibility = Visibility.Collapsed;
        AppointmentRuleText.Text = "";
    }

    private void AppointmentEditorForm_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        HandleFormKeyboardNavigation(e);
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Escape && AppointmentEditorOverlay.Visibility == Visibility.Visible)
        {
            CloseAppointmentEditorModal();
            e.Handled = true;
        }
    }

    private static void HandleFormKeyboardNavigation(KeyEventArgs e)
    {
        if (e.Handled ||
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ||
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) ||
            Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject ?? Keyboard.FocusedElement as DependencyObject;
        if (source is null)
        {
            return;
        }

        var comboBox = FindVisualParent<ComboBox>(source) ?? FindOpenComboBox();
        if (comboBox is not null)
        {
            HandleComboBoxKeyboardNavigation(comboBox, e);
            return;
        }

        var datePicker = FindVisualParent<DatePicker>(source) ?? FindOpenDatePicker();
        if (datePicker is not null)
        {
            HandleDatePickerKeyboardNavigation(datePicker, e);
            return;
        }

        var textBox = FindVisualParent<TextBox>(source);
        if (textBox is not null)
        {
            HandleTextBoxKeyboardNavigation(textBox, e);
            return;
        }

        var passwordBox = FindVisualParent<PasswordBox>(source);
        if (passwordBox is not null && IsEnterOrVerticalArrow(e.Key))
        {
            e.Handled = true;
            MoveFormFocus(passwordBox, e.Key == Key.Up
                ? FocusNavigationDirection.Previous
                : FocusNavigationDirection.Next);
            return;
        }

        var toggleButton = FindVisualParent<ToggleButton>(source);
        if (toggleButton is not null)
        {
            if (e.Key is Key.Enter or Key.Return)
            {
                toggleButton.IsChecked = toggleButton is RadioButton ? true : !(toggleButton.IsChecked ?? false);
                e.Handled = true;
                MoveFormFocus(toggleButton, FocusNavigationDirection.Next);
                return;
            }

            if (TryDirectionFromArrow(e.Key, out var toggleDirection))
            {
                e.Handled = true;
                MoveFormFocus(toggleButton, toggleDirection);
            }

            return;
        }

        var button = FindVisualParent<Button>(source);
        if (button is not null && TryDirectionFromArrow(e.Key, out var buttonDirection))
        {
            e.Handled = true;
            MoveFormFocus(button, buttonDirection);
        }
    }

    private static void HandleTextBoxKeyboardNavigation(TextBox textBox, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
        {
            if (textBox.AcceptsReturn && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                return;
            }

            e.Handled = true;
            MoveFormFocus(
                textBox,
                Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                    ? FocusNavigationDirection.Previous
                    : FocusNavigationDirection.Next);
            return;
        }

        if (e.Key is not (Key.Up or Key.Down))
        {
            return;
        }

        if (textBox.AcceptsReturn && textBox.LineCount > 1)
        {
            var currentLine = textBox.GetLineIndexFromCharacterIndex(textBox.CaretIndex);
            if ((e.Key == Key.Up && currentLine > 0) ||
                (e.Key == Key.Down && currentLine < textBox.LineCount - 1))
            {
                return;
            }
        }

        e.Handled = true;
        MoveFormFocus(
            textBox,
            e.Key == Key.Up ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next);
    }

    private static void FormComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!e.Handled && sender is ComboBox comboBox)
        {
            HandleComboBoxKeyboardNavigation(comboBox, e);
        }
    }

    private static void HandleComboBoxKeyboardNavigation(ComboBox comboBox, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && comboBox.IsDropDownOpen)
        {
            comboBox.IsDropDownOpen = false;
            comboBox.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Enter or Key.Return)
        {
            if (!comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
                e.Handled = true;
                return;
            }

            comboBox.IsDropDownOpen = false;
            e.Handled = true;
            comboBox.Dispatcher.BeginInvoke(
                () =>
                {
                    comboBox.Focus();
                    MoveFormFocusFromCurrent(comboBox, FocusNavigationDirection.Next);
                },
                DispatcherPriority.Input);
            return;
        }

        if (!comboBox.IsEditable && !comboBox.IsDropDownOpen && e.Key is Key.Left or Key.Right)
        {
            e.Handled = true;
            MoveFormFocus(
                Keyboard.FocusedElement as UIElement ?? comboBox,
                e.Key == Key.Left ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next);
        }
    }

    private static void FormDatePicker_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!e.Handled && sender is DatePicker datePicker)
        {
            HandleDatePickerKeyboardNavigation(datePicker, e);
        }
    }

    private static void HandleDatePickerKeyboardNavigation(DatePicker datePicker, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && datePicker.IsDropDownOpen)
        {
            datePicker.IsDropDownOpen = false;
            datePicker.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Enter or Key.Return)
        {
            if (!datePicker.IsDropDownOpen)
            {
                datePicker.IsDropDownOpen = true;
                e.Handled = true;
                return;
            }

            datePicker.IsDropDownOpen = false;
            e.Handled = true;
            datePicker.Dispatcher.BeginInvoke(
                () =>
                {
                    datePicker.Focus();
                    MoveFormFocusFromCurrent(datePicker, FocusNavigationDirection.Next);
                },
                DispatcherPriority.Input);
            return;
        }

    }

    private static bool IsEnterOrVerticalArrow(Key key) =>
        key is Key.Enter or Key.Return or Key.Up or Key.Down;

    private static bool TryDirectionFromArrow(Key key, out FocusNavigationDirection direction)
    {
        direction = key switch
        {
            Key.Left or Key.Up => FocusNavigationDirection.Previous,
            Key.Right or Key.Down => FocusNavigationDirection.Next,
            _ => FocusNavigationDirection.Next
        };
        return key is Key.Left or Key.Right or Key.Up or Key.Down;
    }

    private static void MoveFormFocus(UIElement source, FocusNavigationDirection direction)
    {
        source.MoveFocus(new TraversalRequest(direction));
    }

    private static void MoveFormFocusFromCurrent(UIElement fallback, FocusNavigationDirection direction)
    {
        MoveFormFocus(Keyboard.FocusedElement as UIElement ?? fallback, direction);
    }

    private static ComboBox? FindOpenComboBox()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .Where(window => window.IsActive)
            .SelectMany(FindVisualChildren<ComboBox>)
            .FirstOrDefault(comboBox => comboBox.IsDropDownOpen);
    }

    private static DatePicker? FindOpenDatePicker()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .Where(window => window.IsActive)
            .SelectMany(FindVisualChildren<DatePicker>)
            .FirstOrDefault(datePicker => datePicker.IsDropDownOpen);
    }

    private static T? FindVisualParent<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Settings);
        ShowStatus("Configurações abertas.");
    }

    private void HomeSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Home);
    }

    private void EstablishmentSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Establishment);
    }

    private void FinanceSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Finance);
    }

    private void ReportsSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Reports);
    }

    private void MarketingSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Marketing);
    }

    private void AgendaSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Agenda);
    }

    private void OpenNextAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_homeNextAppointment is null)
        {
            ShowStatus("Não há próximo atendimento para abrir.");
            return;
        }

        ShowMainPage(MainPage.Agenda);
        _selectedAppointment = _homeNextAppointment;
        LoadEditor(_homeNextAppointment);
        OpenAppointmentEditorModal();
        ShowStatus($"{_homeNextAppointment.CustomerName} aberto pelo painel Home.");
    }

    private void NewCustomerQuickButton_Click(object sender, RoutedEventArgs e)
    {
        var segment = CurrentEditorSegment();
        var form = ShowCustomerEditorDialog(segment, CustomerNameTextBox.Text, PhoneTextBox.Text, CustomerProfileTextBox.Text);
        if (form is null)
        {
            return;
        }

        var customer = _data.Customers.FirstOrDefault(item =>
            (!string.IsNullOrWhiteSpace(form.Phone) && item.Phone.Equals(form.Phone, StringComparison.OrdinalIgnoreCase)) ||
            item.Name.Equals(form.Name, StringComparison.OrdinalIgnoreCase));
        if (customer is null)
        {
            customer = new Customer();
            _data.Customers.Add(customer);
        }

        customer.Name = form.Name;
        customer.Phone = form.Phone;
        customer.Document = form.Document;
        customer.Segment = form.Segment;
        customer.Profile = form.Profile;
        customer.Tags = form.Tags;
        customer.Notes = form.Notes;
        customer.AcceptsWhatsApp = form.AcceptsWhatsApp;
        customer.LastSeenAt = DateTime.Now;

        AppointmentSegmentCombo.SelectedItem = form.Segment;
        CustomerNameTextBox.Text = form.Name;
        PhoneTextBox.Text = form.Phone;
        CustomerProfileTextBox.Text = form.Profile;
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Cliente salvo: {customer.Name}.");
    }

    private void RegisterPaymentQuickButton_Click(object sender, RoutedEventArgs e)
    {
        RegisterPaymentButton_Click(sender, e);
    }

    private void RegisterPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        var form = ShowPaymentEditorDialog();
        if (form is null)
        {
            return;
        }

        _data.ManualPayments.Add(new ManualPayment
        {
            Description = form.Description,
            CustomerName = form.CustomerName,
            Category = form.Category,
            PaymentMethod = form.PaymentMethod,
            PaymentProvider = form.PaymentProvider,
            PaymentReference = form.PaymentReference,
            PaymentStatus = form.PaymentStatus,
            Notes = form.Notes,
            Value = form.Value,
            PaidAt = DateTime.Now
        });

        _store.Save(_data);
        RefreshAll();
        ShowMainPage(MainPage.Finance);
        ShowStatus($"Pagamento registrado: {form.Value.ToString("C", Brazil)}.");
    }

    private void NewExpenseButton_Click(object sender, RoutedEventArgs e)
    {
        var form = ShowExpenseEditorDialog();
        if (form is null)
        {
            return;
        }

        _data.Expenses.Add(new ExpenseItem
        {
            Description = form.Description,
            Category = form.Category,
            Supplier = form.Supplier,
            PaymentMethod = form.PaymentMethod,
            Notes = form.Notes,
            Value = form.Value,
            Date = DateTime.Now,
            IsPaid = true
        });

        _store.Save(_data);
        RefreshAll();
        ShowMainPage(MainPage.Finance);
        ShowStatus($"Despesa cadastrada: {form.Value.ToString("C", Brazil)}.");
    }

    private void ReportsQuickButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Reports);
        ShowStatus("Página de relatórios aberta.");
    }

    private void ReportsChartTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_configuringReportChart)
        {
            return;
        }

        RefreshReportsPage();
        ShowStatus($"Gráfico alterado para {CurrentReportChartOption()}.");
    }

    private void ReportChartModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string option })
        {
            return;
        }

        _configuringReportChart = true;
        ReportsChartTypeCombo.SelectedItem = option;
        _configuringReportChart = false;
        RefreshReportsPage();
        ShowStatus($"Gráfico alterado para {option}.");
    }

    private void MarketingPromotionTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshMarketingPreview();
        if (MarketingCampaignsItemsControl is not null)
        {
            RefreshMarketingCampaigns(
                _data.Customers.Count(item => item.LastSeenAt.Date <= DateTime.Today.AddDays(-30)),
                _data.Appointments.Count(item => item.Status == AppointmentStatus.NoShow && item.Start.Date >= DateTime.Today.AddDays(-60)),
                _data.Appointments.Count(item => item.Status == AppointmentStatus.Scheduled && item.Start.Date >= DateTime.Today));
        }
    }

    private void MarketingInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void MarketingInput_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox { IsKeyboardFocusWithin: false } textBox)
        {
            e.Handled = true;
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private void CreatePromotionButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPromotionToMessageEditor();
        RefreshMarketingPage();
        var promotionName = string.IsNullOrWhiteSpace(PromotionNameTextBox.Text)
            ? "Promoção"
            : PromotionNameTextBox.Text.Trim();
        ShowStatus($"Promoção atualizada: {promotionName}.");
    }

    private void CopyMarketingMessageButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureDefaultPromotionMessage();
        Clipboard.SetText(BuildMarketingMessage(_data.Customers.FirstOrDefault()?.Name ?? "Cliente"));
        ShowStatus("Mensagem de marketing copiada para a área de transferência.");
    }

    private async void OpenFirstMarketingWhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        var row = _marketingContacts.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Phone));
        if (row is null)
        {
            ShowStatus("Nenhum cliente com telefone disponível para WhatsApp.");
            return;
        }

        await OpenMarketingWhatsAppAsync(row);
    }

    private async void OpenMarketingWhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MarketingContactRow row })
        {
            await OpenMarketingWhatsAppAsync(row);
        }
    }

    private async void OpenMarketingCampaign_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { DataContext: EstablishmentListRow campaign })
        {
            return;
        }

        if (campaign.BadgeText == "Promoção")
        {
            EnsureDefaultPromotionMessage();
            Clipboard.SetText(BuildMarketingMessage(_data.Customers.FirstOrDefault()?.Name ?? "Cliente"));
            ShowStatus($"{campaign.Name} copiada para a área de transferência.");
            return;
        }

        var badge = campaign.Name switch
        {
            "Volta para agenda" => "Sem retorno",
            "Confirmar horários" => "Confirmação",
            "Recuperar faltas" => "Retorno",
            _ => ""
        };
        var contact = _marketingContacts.FirstOrDefault(item =>
            item.BadgeText == badge && !string.IsNullOrWhiteSpace(item.Phone));
        if (contact is null)
        {
            ShowStatus($"Nenhum contato disponível para a campanha {campaign.Name}.");
            return;
        }

        await OpenMarketingWhatsAppAsync(contact);
    }

    private async void SendSelectedWhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAppointment is null)
        {
            ShowStatus("Selecione um agendamento para chamar no WhatsApp.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedAppointment.CustomerPhone))
        {
            ShowStatus($"Telefone não cadastrado para {_selectedAppointment.CustomerName}.");
            return;
        }

        var phone = NormalizeBrazilPhone(_selectedAppointment.CustomerPhone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus($"Telefone inválido para {_selectedAppointment.CustomerName}.");
            return;
        }

        var message = BuildAppointmentWhatsAppMessage(_selectedAppointment);
        var sent = await SendOrOpenWhatsAppAsync(_selectedAppointment.CustomerName, phone, message, "Confirmação");
        ShowStatus(sent
            ? $"WhatsApp de confirmação enviado para {_selectedAppointment.CustomerName}."
            : $"WhatsApp de confirmação aberto para {_selectedAppointment.CustomerName}.");
    }

    private string BuildAppointmentWhatsAppMessage(Appointment appointment)
    {
        var firstName = FirstName(appointment.CustomerName);
        var day = appointment.Start.ToString("dd/MM", Brazil);
        var time = appointment.Start.ToString("HH:mm", Brazil);
        return $"Oi {firstName}, aqui é da {BusinessDisplayName()}. Seu horário de {appointment.ServiceName} está marcado para {day} às {time}. Pode confirmar por aqui?";
    }

    private void RefreshMarketingButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshMarketingPage();
        ShowStatus("Marketing atualizado com os dados mais recentes.");
    }

    private void OpenHomeCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: HomeCustomerSummaryRow row })
        {
            ShowStatus($"Cadastro de {row.Name} selecionado.");
        }
    }

    private void NewServiceFromEstablishmentButton_Click(object sender, RoutedEventArgs e)
    {
        CreateServiceButton_Click(sender, e);
        ShowMainPage(MainPage.Establishment);
    }

    private void NewProfessionalFromEstablishmentButton_Click(object sender, RoutedEventArgs e)
    {
        CreateProfessionalButton_Click(sender, e);
        ShowMainPage(MainPage.Establishment);
    }

    private void NewProductButton_Click(object sender, RoutedEventArgs e)
    {
        var form = ShowProductEditorDialog();
        if (form is null)
        {
            return;
        }

        var product = new ProductItem
        {
            Name = form.Name,
            Category = form.Category,
            Sku = form.Sku,
            Supplier = form.Supplier,
            CostPrice = form.CostPrice,
            Price = form.Price,
            StockQuantity = form.StockQuantity,
            MinimumStock = form.MinimumStock,
            Notes = form.Notes,
            IsActive = form.IsActive
        };

        _data.Products.Add(product);
        _store.Save(_data);
        RefreshAll();
        ShowMainPage(MainPage.Establishment);
        ShowStatus($"Produto criado: {product.Name}.");
    }

    private void RegisterProductSaleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_data.Products.Count == 0)
        {
            ShowStatus("Cadastre um produto antes de registrar uma venda.");
            return;
        }

        var form = ShowProductSaleEditorDialog();
        if (form is null)
        {
            return;
        }

        var product = form.Product;
        var quantity = form.Quantity;

        _data.ProductSales.Add(new ProductSale
        {
            ProductId = product.Id,
            ProductName = product.Name,
            CustomerName = form.CustomerName,
            Quantity = quantity,
            UnitPrice = product.Price,
            Discount = form.Discount,
            PaymentMethod = form.PaymentMethod,
            PaymentProvider = form.PaymentProvider,
            PaymentReference = form.PaymentReference,
            PaymentStatus = form.PaymentStatus,
            Notes = form.Notes,
            SoldAt = DateTime.Now
        });

        product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Venda registrada: {quantity}x {product.Name}.");
    }

    private void OpenEstablishmentSectionButton_Click(object sender, RoutedEventArgs e)
    {
        var title = sender is Button button
            ? button.Tag?.ToString() ?? (button.DataContext as EstablishmentSectionRow)?.Title ?? ""
            : "";
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        ShowEstablishmentManagerDialog(title);
    }

    private void ShowEstablishmentManagerDialog(string section)
    {
        var (title, subtitle, primaryText, emptyTitle, emptyDetail) = section switch
        {
            "Clientes" => ("Gerenciar clientes", "Base completa de clientes, telefone, perfil e último atendimento.", "Novo cliente", "Nenhum cliente cadastrado", "Clique em Novo cliente para cadastrar a primeira pessoa."),
            "Profissionais" => ("Gerenciar profissionais", "Equipe, função e vínculo com o segmento da agenda.", "Novo profissional", "Nenhum profissional cadastrado", "Cadastre quem atende para liberar horários na agenda."),
            "Serviços" => ("Gerenciar serviços", "Catálogo com duração, preço e recurso padrão de atendimento.", "Novo serviço", "Nenhum serviço cadastrado", "Cadastre serviços para aparecerem no novo agendamento."),
            "Produtos" => ("Gerenciar produtos", "Estoque, categoria e preço de venda no balcão.", "Novo produto", "Nenhum produto cadastrado", "Cadastre produtos para registrar vendas."),
            "Venda de produtos" => ("Gerenciar venda de produtos", "Histórico de vendas, quantidade, cliente e receita.", _data.Products.Count == 0 ? "Cadastrar produto" : "Registrar venda", "Nenhuma venda registrada", _data.Products.Count == 0 ? "Cadastre produtos antes de registrar vendas." : "As vendas de produtos aparecerão aqui."),
            _ => ("Gerenciar cadastro", "Revise os registros desta área.", "Novo", "Nada cadastrado", "Nenhum registro encontrado.")
        };

        if (section == "Clientes" && _data.Customers.Count == 0)
        {
            var emptyShell = CreateEmptyClientManagerDialog(title, subtitle);
            emptyShell.PrimaryButton.Click += (_, _) => emptyShell.Dialog.DialogResult = true;
            emptyShell.ImportButton.Click += (_, _) =>
                ShowStatus("Importação de contatos estará disponível em breve.");

            if (ShowAppDialog(emptyShell.Dialog) == true)
            {
                NewCustomerQuickButton_Click(this, new RoutedEventArgs());
            }

            return;
        }

        if (section == "Clientes")
        {
            var clientShell = CreateManagerDialog(section, title, subtitle, primaryText);
            clientShell.Dialog.Width = 900;
            clientShell.Dialog.MaxHeight = 610;
            string? clientEditId = null;
            AddClientMasterDetail(clientShell.Body, id =>
            {
                clientEditId = id;
                clientShell.Dialog.DialogResult = false;
            });
            clientShell.PrimaryButton.Click += (_, _) => clientShell.Dialog.DialogResult = true;

            var clientResult = ShowAppDialog(clientShell.Dialog);
            if (!string.IsNullOrWhiteSpace(clientEditId))
            {
                OpenManagerItemEditor(section, clientEditId);
                return;
            }

            if (clientResult == true)
            {
                NewCustomerQuickButton_Click(this, new RoutedEventArgs());
            }

            return;
        }

        if (section == "Profissionais")
        {
            var professionalShell = CreateManagerDialog(section, title, subtitle, primaryText);
            professionalShell.Dialog.Width = 860;
            professionalShell.Dialog.MaxHeight = 580;
            string? professionalEditId = null;
            AddProfessionalManagerTable(professionalShell.Body, id =>
            {
                professionalEditId = id;
                professionalShell.Dialog.DialogResult = false;
            });
            professionalShell.PrimaryButton.Click += (_, _) => professionalShell.Dialog.DialogResult = true;

            var professionalResult = ShowAppDialog(professionalShell.Dialog);
            if (!string.IsNullOrWhiteSpace(professionalEditId))
            {
                OpenManagerItemEditor(section, professionalEditId);
                return;
            }

            if (professionalResult == true)
            {
                CreateProfessionalButton_Click(this, new RoutedEventArgs());
            }

            return;
        }

        if (section == "Serviços" && _data.Services.Count > 0)
        {
            var serviceShell = CreateManagerDialog(section, title, subtitle, primaryText);
            serviceShell.Dialog.Width = 980;
            serviceShell.Dialog.MaxHeight = 650;
            string? serviceEditId = null;
            AddServiceMasterDetail(serviceShell.Body, id =>
            {
                serviceEditId = id;
                serviceShell.Dialog.DialogResult = false;
            });
            serviceShell.PrimaryButton.Click += (_, _) => serviceShell.Dialog.DialogResult = true;

            var serviceResult = ShowAppDialog(serviceShell.Dialog);
            if (!string.IsNullOrWhiteSpace(serviceEditId))
            {
                OpenManagerItemEditor(section, serviceEditId);
                return;
            }

            if (serviceResult == true)
            {
                CreateServiceButton_Click(this, new RoutedEventArgs());
            }

            return;
        }

        var shell = CreateManagerDialog(section, title, subtitle, primaryText);
        string? editId = null;
        AddManagerRows(shell.Body, section, emptyTitle, emptyDetail, id =>
        {
            editId = id;
            shell.Dialog.DialogResult = false;
        });
        shell.PrimaryButton.Click += (_, _) => shell.Dialog.DialogResult = true;

        var dialogResult = ShowAppDialog(shell.Dialog);
        if (!string.IsNullOrWhiteSpace(editId))
        {
            OpenManagerItemEditor(section, editId);
            return;
        }

        if (dialogResult != true)
        {
            return;
        }

        switch (section)
        {
            case "Clientes":
                NewCustomerQuickButton_Click(this, new RoutedEventArgs());
                break;
            case "Profissionais":
                CreateProfessionalButton_Click(this, new RoutedEventArgs());
                break;
            case "Serviços":
                CreateServiceButton_Click(this, new RoutedEventArgs());
                break;
            case "Produtos":
                NewProductButton_Click(this, new RoutedEventArgs());
                break;
            case "Venda de produtos":
                if (_data.Products.Count == 0)
                {
                    NewProductButton_Click(this, new RoutedEventArgs());
                    break;
                }

                RegisterProductSaleButton_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private void CopyDialogThemeResources(Window dialog)
    {
        foreach (var key in new[]
                 {
                     "Accent",
                     "AccentDark",
                     "AccentSoft",
                     "BlueSoft",
                     "WarmSoft",
                     "Ink",
                     "Muted",
                     "Line",
                     "Panel",
                     "SidebarHover"
                 })
        {
            if (TryFindResource(key) is { } resource)
            {
                dialog.Resources[key] = resource;
            }
        }
    }

    private void ConfigureRoundedDialogWindow(Window dialog)
    {
        dialog.WindowStyle = WindowStyle.None;
        dialog.AllowsTransparency = true;
        dialog.Background = Brushes.Transparent;
        dialog.ShowInTaskbar = false;
    }

    private Button CreateDialogCloseButton(Window dialog)
    {
        var closeButton = new Button
        {
            Style = (Style)FindResource("SubtleButton"),
            Width = 40,
            MinWidth = 40,
            Height = 40,
            Padding = new Thickness(0),
            IsCancel = true,
            ToolTip = "Fechar",
            Content = new PackIcon
            {
                Kind = PackIconKind.Close,
                Width = 18,
                Height = 18,
                Foreground = InkBrush
            }
        };
        AutomationProperties.SetName(closeButton, $"Fechar {dialog.Title}".Trim());
        closeButton.Click += (_, _) => dialog.Close();
        return closeButton;
    }

    private static void EnableDialogDrag(Border header, Window dialog)
    {
        header.MouseLeftButtonDown += (_, args) =>
        {
            if (args.ChangedButton == MouseButton.Left)
            {
                dialog.DragMove();
            }
        };
    }

    private static void ApplyRoundedClip(Border frame, double radius)
    {
        void UpdateClip()
        {
            if (frame.ActualWidth <= 0 || frame.ActualHeight <= 0)
            {
                return;
            }

            frame.Clip = new RectangleGeometry(
                new Rect(0, 0, frame.ActualWidth, frame.ActualHeight),
                radius,
                radius);
        }

        frame.Loaded += (_, _) => UpdateClip();
        frame.SizeChanged += (_, _) => UpdateClip();
    }

    private static Grid WrapRoundedDialogContent(UIElement content, Brush background, double margin = 10)
    {
        var frame = new Border
        {
            Background = background,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppModalRadiusValue),
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 4,
                Opacity = 0.18,
                Color = Color.FromRgb(15, 23, 42)
            },
            Child = content
        };
        ApplyRoundedClip(frame, AppModalRadiusValue);

        var host = new Grid
        {
            Background = Brushes.Transparent,
            Margin = new Thickness(margin)
        };
        host.Children.Add(frame);
        return host;
    }

    private (Window Dialog, StackPanel Body, Button PrimaryButton) CreateManagerDialog(
        string section,
        string title,
        string subtitle,
        string primaryText)
    {
        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Width = 740,
            MaxHeight = 720,
            SizeToContent = SizeToContent.Height
        };
        CopyDialogThemeResources(dialog);

        var body = new StackPanel
        {
            Margin = new Thickness(22, 16, 22, 10)
        };

        var primaryButton = new Button
        {
            Style = (Style)FindResource("CommandButton"),
            Height = 40,
            MinWidth = 150,
            IsDefault = true,
            Background = AccentDarkBrush,
            BorderBrush = AccentDarkBrush,
            Foreground = Brushes.White,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PackIcon
                    {
                        Kind = PackIconKind.Plus,
                        Width = 17,
                        Height = 17,
                        Margin = new Thickness(0, 0, 7, 0),
                        Foreground = Brushes.White
                    },
                    new TextBlock
                    {
                        Text = primaryText,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        TextElement.SetForeground(primaryButton, Brushes.White);
        AutomationProperties.SetName(primaryButton, primaryText);

        var closeButton = CreateDialogCloseButton(dialog);

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconShell = new Border
        {
            Width = 44,
            Height = 40,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Margin = new Thickness(0, 0, 12, 0),
            Child = new PackIcon
            {
                Kind = ManagerIcon(section),
                Width = 21,
                Height = 21,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var headerText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    Foreground = InkBrush,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold
                },
                new TextBlock
                {
                    Text = subtitle,
                    Foreground = MutedBrush,
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 18, 0)
                }
            }
        };
        Grid.SetColumn(headerText, 1);
        Grid.SetColumn(closeButton, 2);
        headerGrid.Children.Add(iconShell);
        headerGrid.Children.Add(headerText);
        headerGrid.Children.Add(closeButton);

        var header = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(AppModalRadiusValue, AppModalRadiusValue, 0, 0),
            Padding = new Thickness(22, 18, 22, 16),
            Child = headerGrid
        };
        EnableDialogDrag(header, dialog);

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("GhostButton"),
            Height = 40,
            MinWidth = 108,
            IsCancel = true,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var footer = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, AppModalRadiusValue, AppModalRadiusValue),
            Padding = new Thickness(22, 14, 22, 16),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children = { cancelButton, primaryButton }
            }
        };

        var scroll = new ScrollViewer
        {
            MaxHeight = 430,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.VerticalOnly,
            Content = body
        };
        ApplyDialogScrollTheme(scroll);

        var content = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(header);
        content.Children.Add(footer);
        content.Children.Add(scroll);

        var frame = new Border
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppModalRadiusValue),
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 4,
                Opacity = 0.18,
                Color = Color.FromRgb(23, 20, 17)
            },
            Child = content
        };
        ApplyRoundedClip(frame, AppModalRadiusValue);

        var windowContent = new Grid { Margin = new Thickness(16) };
        windowContent.Children.Add(frame);
        dialog.Content = windowContent;
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;
        return (dialog, body, primaryButton);
    }

    private (Window Dialog, Button PrimaryButton, Button ImportButton) CreateEmptyClientManagerDialog(
        string title,
        string subtitle)
    {
        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Width = 740,
            Height = 542
        };
        CopyDialogThemeResources(dialog);

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        headerGrid.Children.Add(new Border
        {
            Width = 50,
            Height = 50,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Margin = new Thickness(0, 0, 19, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.AccountGroup,
                Width = 24,
                Height = 24,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var headerText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    Foreground = InkBrush,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold
                },
                new TextBlock
                {
                    Text = subtitle,
                    Foreground = MutedBrush,
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 18, 0)
                }
            }
        };
        Grid.SetColumn(headerText, 1);
        headerGrid.Children.Add(headerText);

        var closeButton = CreateDialogCloseButton(dialog);
        Grid.SetColumn(closeButton, 2);
        headerGrid.Children.Add(closeButton);

        var header = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(AppModalRadiusValue, AppModalRadiusValue, 0, 0),
            Padding = new Thickness(22, 18, 22, 13),
            Child = headerGrid
        };
        EnableDialogDrag(header, dialog);

        var statusGrid = new Grid();
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusGrid.Children.Add(new Border
        {
            Width = 30,
            Height = 30,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.AccountGroup,
                Width = 16,
                Height = 16,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        var countText = new TextBlock
        {
            Text = "0 clientes cadastrados",
            Foreground = InkBrush,
            FontSize = 13.5,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(countText, 1);
        statusGrid.Children.Add(countText);

        var primaryButton = new Button
        {
            Style = (Style)FindResource("CommandButton"),
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsDefault = true,
            Background = AccentDarkBrush,
            BorderBrush = AccentDarkBrush,
            Foreground = Brushes.White,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PackIcon
                    {
                        Kind = PackIconKind.Plus,
                        Width = 17,
                        Height = 17,
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 0, 7, 0)
                    },
                    new TextBlock
                    {
                        Text = "Cadastrar primeiro cliente",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        TextElement.SetForeground(primaryButton, Brushes.White);
        AutomationProperties.SetName(primaryButton, "Cadastrar primeiro cliente");

        var importButton = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 10, 0, 0),
            Foreground = AccentBrush,
            BorderBrush = AccentBrush,
            ToolTip = "Importação de contatos em breve",
            Content = new TextBlock
            {
                Text = "Importar contatos",
                Foreground = AccentBrush,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        TextElement.SetForeground(importButton, AccentBrush);
        AutomationProperties.SetName(importButton, "Importar contatos");

        var heroCopy = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0),
            Children =
            {
                new TextBlock
                {
                    Text = "Organize cada cliente desde\no primeiro contato",
                    Width = 263,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = InkBrush,
                    FontSize = 19,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.NoWrap,
                    LineHeight = 24
                },
                new TextBlock
                {
                    Text = "Salve WhatsApp, preferências e histórico de atendimento em um só lugar.",
                    Foreground = MutedBrush,
                    FontSize = 12.8,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 19,
                    Margin = new Thickness(0, 8, 0, 13)
                },
                CreateClientManagerBenefit("Contato sempre à mão"),
                CreateClientManagerBenefit("Atendimento mais personalizado"),
                new StackPanel
                {
                    Margin = new Thickness(0, 26, 0, 0),
                    Children = { primaryButton, importButton }
                }
            }
        };

        var hero = new Grid { Height = 318, Margin = new Thickness(0, 16, 0, 0) };
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(322) });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(263) });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hero.Children.Add(CreateClientManagerIllustration());
        Grid.SetColumn(heroCopy, 2);
        hero.Children.Add(heroCopy);

        var body = new Grid { Margin = new Thickness(22, 16, 22, 18) };
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(new Border
        {
            Height = 44,
            Background = GraySoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Padding = new Thickness(10, 7, 12, 7),
            Child = statusGrid
        });
        Grid.SetRow(hero, 1);
        body.Children.Add(hero);

        var content = new DockPanel { LastChildFill = true, Background = PanelBrush };
        DockPanel.SetDock(header, Dock.Top);
        content.Children.Add(header);
        content.Children.Add(body);

        var frame = new Border
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppModalRadiusValue),
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 4,
                Opacity = 0.2,
                Color = Color.FromRgb(23, 20, 17)
            },
            Child = content
        };
        ApplyRoundedClip(frame, AppModalRadiusValue);

        var windowContent = new Grid { Margin = new Thickness(16) };
        windowContent.Children.Add(frame);
        dialog.Content = windowContent;
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;
        return (dialog, primaryButton, importButton);
    }

    private static UIElement CreateClientManagerIllustration()
    {
        var illustration = new Grid
        {
            Width = 250,
            Height = 240,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 20, 0, 0),
            RenderTransform = new TranslateTransform(-24, 0)
        };

        illustration.Children.Add(new Border
        {
            Width = 220,
            Height = 220,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(110),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PackIcon
            {
                Kind = PackIconKind.AccountGroup,
                Width = 120,
                Height = 120,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var cardLines = new StackPanel
        {
            Width = 45,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Border
                {
                    Height = 7,
                    Background = AccentBrush,
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 7)
                },
                new Border
                {
                    Width = 32,
                    Height = 6,
                    Background = GraySoftBrush,
                    CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Left
                }
            }
        };
        var contactCardGrid = new Grid { Margin = new Thickness(12, 9, 12, 9) };
        contactCardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        contactCardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contactCardGrid.Children.Add(new Border
        {
            Width = 31,
            Height = 31,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(16),
            Child = new PackIcon
            {
                Kind = PackIconKind.AccountOutline,
                Width = 18,
                Height = 18,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        Grid.SetColumn(cardLines, 1);
        contactCardGrid.Children.Add(cardLines);

        illustration.Children.Add(new Border
        {
            Width = 118,
            Height = 76,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(15),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 2, 34),
            Effect = new DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 3,
                Opacity = 0.18,
                Color = Color.FromRgb(89, 55, 35)
            },
            Child = contactCardGrid
        });

        illustration.Children.Add(new PackIcon
        {
            Kind = PackIconKind.Plus,
            Width = 19,
            Height = 19,
            Foreground = AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 14, 15, 0)
        });
        illustration.Children.Add(new PackIcon
        {
            Kind = PackIconKind.Plus,
            Width = 13,
            Height = 13,
            Foreground = AccentBrush,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 0, 27)
        });
        return illustration;
    }

    private static UIElement CreateClientManagerBenefit(string text)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 7),
            Children =
            {
                new PackIcon
                {
                    Kind = PackIconKind.CheckCircleOutline,
                    Width = 17,
                    Height = 17,
                    Foreground = AccentBrush,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = text,
                    Foreground = InkBrush,
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }

    private void AddProfessionalManagerTable(StackPanel body, Action<string> editRequested)
    {
        var professionals = _data.Professionals.OrderBy(item => item.Name).ToList();
        var searchBox = new TextBox
        {
            Style = (Style)FindResource("AppointmentInputBox"),
            Height = 42,
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            CaretBrush = AccentBrush,
            Margin = new Thickness(0, 0, 14, 0)
        };
        HintAssist.SetHint(searchBox, "Buscar por nome, função ou segmento...");
        AutomationProperties.SetName(searchBox, "Buscar profissionais");

        var countText = new TextBlock
        {
            Foreground = MutedBrush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var toolbar = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.Children.Add(searchBox);
        var countBadge = new Border
        {
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppBadgeRadiusValue),
            Padding = new Thickness(12, 6, 12, 6),
            VerticalAlignment = VerticalAlignment.Center,
            Child = countText
        };
        Grid.SetColumn(countBadge, 1);
        toolbar.Children.Add(countBadge);
        body.Children.Add(toolbar);

        static void ConfigureColumns(Grid grid)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        }

        static TextBlock HeaderText(string text) => new()
        {
            Text = text,
            Foreground = MutedBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var header = new Grid { Height = 34 };
        ConfigureColumns(header);
        var headers = new[] { "", "PROFISSIONAL", "FUNÇÃO", "SEGMENTO", "STATUS", "AÇÕES" };
        for (var index = 0; index < headers.Length; index++)
        {
            var text = HeaderText(headers[index]);
            Grid.SetColumn(text, index);
            header.Children.Add(text);
        }

        var rowsPanel = new StackPanel();
        var table = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            ClipToBounds = true,
            Child = new StackPanel
            {
                Children =
                {
                    new Border
                    {
                        Background = GraySoftBrush,
                        BorderBrush = LineBrush,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(14, 0, 14, 0),
                        Child = header
                    },
                    rowsPanel
                }
            }
        };
        body.Children.Add(table);

        void RenderRows()
        {
            var query = searchBox.Text.Trim();
            var filtered = professionals
                .Where(item => string.IsNullOrWhiteSpace(query)
                    || item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || item.Role.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || item.Segments.Any(segment => segment.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();

            rowsPanel.Children.Clear();
            countText.Text = $"{filtered.Count} de {professionals.Count} profissional{(professionals.Count == 1 ? "" : "is")}";

            if (filtered.Count == 0)
            {
                rowsPanel.Children.Add(new TextBlock
                {
                    Text = professionals.Count == 0 ? "Nenhum profissional cadastrado." : "Nenhum profissional encontrado.",
                    Foreground = MutedBrush,
                    FontSize = 12.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20, 28, 20, 28)
                });
                return;
            }

            foreach (var professional in filtered)
            {
                var row = new Grid { MinHeight = 64 };
                ConfigureColumns(row);
                row.Children.Add(new Border
                {
                    Width = 36,
                    Height = 36,
                    Background = AccentSoftBrush,
                    CornerRadius = new CornerRadius(18),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = string.Concat(professional.Name
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(2)
                            .Select(part => char.ToUpperInvariant(part[0]))),
                        Foreground = AccentBrush,
                        FontSize = 11.5,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });

                var nameBlock = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                nameBlock.Children.Add(new TextBlock
                {
                    Text = professional.Name,
                    Foreground = InkBrush,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                nameBlock.Children.Add(new TextBlock
                {
                    Text = FirstFilled(professional.Phone, professional.Email, "Sem contato informado"),
                    Foreground = MutedBrush,
                    FontSize = 10.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 2, 8, 0)
                });
                Grid.SetColumn(nameBlock, 1);
                row.Children.Add(nameBlock);

                var role = new TextBlock
                {
                    Text = FirstFilled(professional.Role, "Equipe"),
                    Foreground = InkBrush,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(role, 2);
                row.Children.Add(role);

                var segment = new TextBlock
                {
                    Text = professional.Segments.Count == 0
                        ? FirstFilled(_data.Settings.BusinessSegment, "Agenda")
                        : string.Join(", ", professional.Segments),
                    Foreground = MutedBrush,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(segment, 3);
                row.Children.Add(segment);

                var status = new Border
                {
                    Background = professional.IsActive ? AccentSoftBrush : GraySoftBrush,
                    CornerRadius = new CornerRadius(AppBadgeRadiusValue),
                    Padding = new Thickness(9, 4, 9, 4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = professional.IsActive ? "Ativo" : "Inativo",
                        Foreground = professional.IsActive ? AccentDarkBrush : MutedBrush,
                        FontSize = 10.5,
                        FontWeight = FontWeights.SemiBold
                    }
                };
                Grid.SetColumn(status, 4);
                row.Children.Add(status);

                var editButton = new Button
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new PackIcon
                            {
                                Kind = PackIconKind.PencilOutline,
                                Width = 14,
                                Height = 14,
                                Margin = new Thickness(0, 0, 6, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = "Editar",
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    },
                    Style = (Style)FindResource("GhostButton"),
                    Height = 34,
                    MinWidth = 74,
                    Padding = new Thickness(10, 0, 10, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                AutomationProperties.SetName(editButton, $"Editar {professional.Name}");
                editButton.Click += (_, _) => editRequested(professional.Id);
                Grid.SetColumn(editButton, 5);
                row.Children.Add(editButton);

                rowsPanel.Children.Add(new Border
                {
                    BorderBrush = LineBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(14, 0, 14, 0),
                    Child = row
                });
            }
        }

        searchBox.TextChanged += (_, _) => RenderRows();
        RenderRows();
    }

    private void AddManagerRows(StackPanel body, string section, string emptyTitle, string emptyDetail, Action<string>? editRequested = null)
    {
        var rows = section switch
        {
            "Clientes" => _data.Customers
                .OrderBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join("  •  ", new[] { item.Phone, item.Tags, item.Profile, item.Segment }.Where(part => !string.IsNullOrWhiteSpace(part)).DefaultIfEmpty("Sem detalhes cadastrados")),
                    item.LastSeenAt == DateTime.MinValue ? "novo" : item.LastSeenAt.ToString("dd/MM", Brazil),
                    AccentSoftBrush,
                    AccentBrush,
                    item.Id,
                    PackIconKind.AccountOutline))
                .ToList(),
            "Profissionais" => _data.Professionals
                .OrderBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join("  •  ", new[] { item.SegmentLine, item.Phone, item.Email, item.CommissionPercent > 0 ? $"{item.CommissionPercent:N0}% comissão" : "" }.Where(part => !string.IsNullOrWhiteSpace(part)).DefaultIfEmpty("Sem detalhes cadastrados")),
                    item.IsActive ? "ativo" : "inativo",
                    item.IsActive ? AccentSoftBrush : GraySoftBrush,
                    item.IsActive ? AccentBrush : MutedBrush,
                    item.Id,
                    PackIconKind.AccountTie))
                .ToList(),
            "Serviços" => _data.Services
                .OrderBy(item => item.Segment)
                .ThenBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join("  •  ", new[]
                    {
                        item.Segment,
                        item.Category,
                        $"{item.DurationMinutes} min",
                        item.Price.ToString("C", Brazil),
                        item.DefaultResource,
                        item.BufferMinutes > 0 ? $"{item.BufferMinutes} min intervalo" : ""
                    }.Where(part => !string.IsNullOrWhiteSpace(part))),
                    item.IsActive ? "ativo" : "inativo",
                    item.IsActive ? AccentSoftBrush : GraySoftBrush,
                    item.IsActive ? AccentBrush : MutedBrush,
                    item.Id,
                    PackIconKind.ClipboardText))
                .ToList(),
            "Produtos" => _data.Products
                .OrderBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join("  •  ", new[]
                    {
                        string.IsNullOrWhiteSpace(item.Category) ? "Sem categoria" : item.Category,
                        string.IsNullOrWhiteSpace(item.Sku) ? "" : $"SKU {item.Sku}",
                        string.IsNullOrWhiteSpace(item.Supplier) ? "" : item.Supplier,
                        $"Venda {item.Price.ToString("C", Brazil)}",
                        item.CostPrice > 0 ? $"Custo {item.CostPrice.ToString("C", Brazil)}" : "",
                        $"Estoque {item.StockQuantity}"
                    }.Where(part => !string.IsNullOrWhiteSpace(part))),
                    item.StockQuantity <= item.MinimumStock && item.MinimumStock > 0 ? "baixo" : item.StockQuantity.ToString(Brazil),
                    AccentSoftBrush,
                    AccentBrush,
                    item.Id,
                    PackIconKind.PackageVariant))
                .ToList(),
            "Venda de produtos" => _data.ProductSales
                .OrderByDescending(item => item.SoldAt)
                .Select(item => new EstablishmentListRow(
                    item.ProductName,
                    $"{item.Quantity} un.  •  {item.Total.ToString("C", Brazil)}" +
                    (string.IsNullOrWhiteSpace(item.CustomerName) ? "" : $"  •  {item.CustomerName}") +
                    (string.IsNullOrWhiteSpace(item.PaymentMethod) ? "" : $"  •  {item.PaymentMethod}"),
                    item.SoldAt.ToString("dd/MM", Brazil),
                    WarmSoftBrush,
                    AccentBrush,
                    item.Id,
                    PackIconKind.Cart))
                .ToList(),
            _ => []
        };

        var summaryGrid = new Grid();
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        summaryGrid.Children.Add(new Border
        {
            Width = 34,
            Height = 34,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = ManagerIcon(section),
                Width = 17,
                Height = 17,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var countText = new TextBlock
        {
            Text = ManagerCountLabel(section, rows.Count),
            Foreground = InkBrush,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(countText, 1);
        summaryGrid.Children.Add(countText);

        var summaryHint = new TextBlock
        {
            Text = rows.Count == 0 ? "Cadastre o primeiro item" : "Selecione Editar para ver os detalhes",
            Foreground = MutedBrush,
            FontSize = 11.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(summaryHint, 2);
        summaryGrid.Children.Add(summaryHint);

        body.Children.Add(new Border
        {
            Background = GraySoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(10, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 10),
            Child = summaryGrid
        });

        if (rows.Count == 0)
        {
            AddManagerEmptyState(body, section, emptyTitle, emptyDetail);
            return;
        }

        foreach (var row in rows)
        {
            Action? clickAction = !string.IsNullOrWhiteSpace(row.Id) && editRequested is not null
                ? () => editRequested(row.Id)
                : null;
            body.Children.Add(CreateManagerRow(row, clickAction));
        }
    }

    private void AddClientMasterDetail(StackPanel body, Action<string> editRequested)
    {
        var clients = _data.Customers.OrderBy(item => item.Name).ToList();
        Customer selectedClient = clients[0];

        var root = new Grid { Height = 300 };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var searchBox = new TextBox
        {
            Style = (Style)FindResource("AppointmentInputBox"),
            Height = 46,
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            CaretBrush = AccentBrush,
            Margin = new Thickness(0, 0, 0, 14)
        };
        HintAssist.SetHint(searchBox, "Buscar clientes...");
        AutomationProperties.SetName(searchBox, "Buscar clientes");

        var countText = new TextBlock
        {
            Text = ManagerCountLabel("Clientes", clients.Count),
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var listPanel = new StackPanel();
        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = listPanel
        };
        ApplyDialogScrollTheme(listScroll);
        var leftPanel = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0, 0, 18, 0)
        };
        DockPanel.SetDock(searchBox, Dock.Top);
        DockPanel.SetDock(countText, Dock.Top);
        leftPanel.Children.Add(searchBox);
        leftPanel.Children.Add(countText);
        leftPanel.Children.Add(listScroll);

        var detailPanel = new StackPanel { Margin = new Thickness(22, 0, 0, 0) };
        var detailSurface = new Border
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Background = Brushes.White,
            Child = detailPanel
        };
        Grid.SetColumn(detailSurface, 1);
        root.Children.Add(leftPanel);
        root.Children.Add(detailSurface);
        body.Children.Add(root);

        static TextBlock DetailValue(string text, Brush? foreground = null) => new()
        {
            Text = text,
            Foreground = foreground ?? InkBrush,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        };

        static Border DetailRow(PackIconKind iconKind, string label, string value, Brush? valueBrush = null)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(new Border
            {
                Width = 34,
                Height = 34,
                Background = WarmSoftBrush,
                CornerRadius = new CornerRadius(17),
                Child = new PackIcon
                {
                    Kind = iconKind,
                    Width = 18,
                    Height = 18,
                    Foreground = MutedBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var text = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = label, Foreground = MutedBrush, FontSize = 12 },
                    DetailValue(value, valueBrush)
                }
            };
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            return new Border
            {
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 8, 0, 8),
                Child = row
            };
        }

        void RefreshDetails()
        {
            detailPanel.Children.Clear();
            var editButton = new Button
            {
                Style = (Style)FindResource("MercadoPagoOutlineButton"),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new PackIcon { Kind = PackIconKind.Pencil, Width = 15, Height = 15, Foreground = AccentBrush, Margin = new Thickness(0, 0, 7, 0) },
                        new TextBlock { Text = "Editar", Foreground = AccentBrush, FontWeight = FontWeights.SemiBold }
                    }
                },
                Height = 40,
                MinWidth = 108,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            editButton.Click += (_, _) => editRequested(selectedClient.Id);

            var heading = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heading.Children.Add(new Border
            {
                Width = 44,
                Height = 44,
                Background = AccentSoftBrush,
                CornerRadius = new CornerRadius(22),
                Child = new PackIcon { Kind = PackIconKind.AccountOutline, Width = 22, Height = 22, Foreground = AccentBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            });
            var headingText = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0),
                Children =
                {
                    new TextBlock { Text = selectedClient.Name, Foreground = InkBrush, FontSize = 18, FontWeight = FontWeights.Bold },
                    new TextBlock { Text = FirstFilled(selectedClient.Tags, "Cliente ativo"), Foreground = MutedBrush, FontSize = 12, Margin = new Thickness(0, 3, 0, 0) }
                }
            };
            Grid.SetColumn(headingText, 1);
            Grid.SetColumn(editButton, 2);
            heading.Children.Add(headingText);
            heading.Children.Add(editButton);
            detailPanel.Children.Add(heading);
            detailPanel.Children.Add(DetailRow(PackIconKind.PhoneOutline, "Contato", FirstFilled(selectedClient.Phone, "Não informado")));
            detailPanel.Children.Add(DetailRow(PackIconKind.ClockOutline, "Preferência de horário", FirstFilled(selectedClient.Profile, "Não informada")));
            detailPanel.Children.Add(DetailRow(PackIconKind.CalendarOutline, "Último atendimento", selectedClient.LastSeenAt == DateTime.MinValue ? "Sem atendimento" : selectedClient.LastSeenAt.ToString("dd/MM/yyyy", Brazil)));
            detailPanel.Children.Add(DetailRow(PackIconKind.StoreOutline, "Estabelecimento", FirstFilled(selectedClient.Segment, _data.Settings.BusinessSegment, "Não informado"), AccentBrush));
        }

        void RefreshList()
        {
            listPanel.Children.Clear();
            var query = searchBox.Text.Trim();
            var visible = clients.Where(item => string.IsNullOrWhiteSpace(query) || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Phone.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            countText.Text = ManagerCountLabel("Clientes", visible.Count);
            foreach (var client in visible)
            {
                var isSelected = client.Id == selectedClient.Id;
                var button = new Button
                {
                    Style = (Style)FindResource("MercadoPagoOutlineButton"),
                    Height = 56,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(14, 0, 14, 0),
                    Background = isSelected ? AccentSoftBrush : Brushes.White,
                    BorderBrush = isSelected ? AccentSoftBrush : LineBrush,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new Grid
                    {
                        Width = 250,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new GridLength(42) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        }
                    }
                };
                var row = (Grid)button.Content;
                row.Children.Add(new Border
                {
                    Width = 34,
                    Height = 34,
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(17),
                    Child = new PackIcon { Kind = PackIconKind.AccountGroup, Width = 17, Height = 17, Foreground = AccentBrush, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                });
                var nameAndArrow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = client.Name, Foreground = InkBrush, FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center },
                        new PackIcon { Kind = PackIconKind.ChevronRight, Width = 18, Height = 18, Foreground = MutedBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 0, 0, 0) }
                    }
                };
                Grid.SetColumn(nameAndArrow, 1);
                row.Children.Add(nameAndArrow);
                button.Click += (_, _) =>
                {
                    selectedClient = client;
                    RefreshList();
                    RefreshDetails();
                };
                listPanel.Children.Add(button);
            }
        }

        searchBox.TextChanged += (_, _) => RefreshList();
        RefreshList();
        RefreshDetails();
    }

    private void AddServiceMasterDetail(StackPanel body, Action<string> editRequested)
    {
        var services = _data.Services
            .OrderBy(item => item.Name)
            .ToList();
        var selectedService = services.FirstOrDefault(item =>
                                  item.Name.Equals("Corte feminino", StringComparison.OrdinalIgnoreCase))
                              ?? services[0];
        var activeOnly = false;

        var root = new Grid { Height = 400 };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(440) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var searchBox = new TextBox
        {
            Style = (Style)FindResource("AppointmentInputBox"),
            Height = 44,
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            CaretBrush = AccentBrush,
            Margin = new Thickness(0, 0, 8, 0)
        };
        HintAssist.SetHint(searchBox, "Buscar serviços...");
        AutomationProperties.SetName(searchBox, "Buscar serviços");

        var filterButton = new Button
        {
            Style = (Style)FindResource("MercadoPagoOutlineButton"),
            Width = 46,
            MinWidth = 46,
            Height = 44,
            Padding = new Thickness(0),
            ToolTip = "Exibir somente serviços ativos",
            Content = new PackIcon
            {
                Kind = PackIconKind.FilterVariant,
                Width = 19,
                Height = 19,
                Foreground = InkBrush
            }
        };
        AutomationProperties.SetName(filterButton, "Filtrar serviços ativos");

        var searchRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(filterButton, 1);
        searchRow.Children.Add(searchBox);
        searchRow.Children.Add(filterButton);

        var countText = new TextBlock
        {
            Text = ManagerCountLabel("Serviços", services.Count),
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var listPanel = new StackPanel();
        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = listPanel
        };
        ApplyDialogScrollTheme(listScroll);

        var leftPanel = new Grid { Margin = new Thickness(0, 0, 22, 0) };
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(countText, 1);
        Grid.SetRow(listScroll, 2);
        leftPanel.Children.Add(searchRow);
        leftPanel.Children.Add(countText);
        leftPanel.Children.Add(listScroll);

        var detailPanel = new StackPanel { Margin = new Thickness(28, 0, 0, 0) };
        var detailSurface = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = detailPanel
        };
        Grid.SetColumn(detailSurface, 1);
        root.Children.Add(leftPanel);
        root.Children.Add(detailSurface);
        body.Children.Add(root);

        static Border StatusBadge(bool isActive)
        {
            return new Border
            {
                Background = isActive ? Solid("#EAF7EE") : GraySoftBrush,
                CornerRadius = new CornerRadius(AppBadgeRadiusValue),
                Padding = new Thickness(9, 3, 9, 3),
                Child = new TextBlock
                {
                    Text = isActive ? "ativo" : "inativo",
                    Foreground = isActive ? Solid("#208541") : MutedBrush,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                }
            };
        }

        static Border DetailRow(PackIconKind iconKind, string label, string value)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new Border
            {
                Width = 34,
                Height = 34,
                Background = WarmSoftBrush,
                CornerRadius = new CornerRadius(17),
                Child = new PackIcon
                {
                    Kind = iconKind,
                    Width = 19,
                    Height = 19,
                    Foreground = MutedBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var labelText = new TextBlock
            {
                Text = label,
                Foreground = MutedBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 1);
            row.Children.Add(labelText);
            var valueText = new TextBlock
            {
                Text = value,
                Foreground = InkBrush,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 190
            };
            Grid.SetColumn(valueText, 2);
            row.Children.Add(valueText);
            return new Border
            {
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 13, 0, 13),
                Child = row
            };
        }

        void RefreshDetails()
        {
            detailPanel.Children.Clear();

            var editButton = new Button
            {
                Style = (Style)FindResource("MercadoPagoOutlineButton"),
                Height = 42,
                MinWidth = 112,
                BorderBrush = AccentBrush,
                Foreground = AccentBrush,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new PackIcon
                        {
                            Kind = PackIconKind.Pencil,
                            Width = 16,
                            Height = 16,
                            Foreground = AccentBrush,
                            Margin = new Thickness(0, 0, 7, 0)
                        },
                        new TextBlock
                        {
                            Text = "Editar",
                            Foreground = AccentBrush,
                            FontWeight = FontWeights.SemiBold
                        }
                    }
                }
            };
            AutomationProperties.SetName(editButton, $"Editar serviço {selectedService.Name}");
            editButton.Click += (_, _) => editRequested(selectedService.Id);

            var heading = new Grid { Margin = new Thickness(0, 8, 0, 20) };
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heading.Children.Add(new Border
            {
                Width = 68,
                Height = 68,
                Background = AccentDarkBrush,
                CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
                Child = new PackIcon
                {
                    Kind = PackIconKind.ContentCut,
                    Width = 32,
                    Height = 32,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            var titleAndStatus = new StackPanel
            {
                Margin = new Thickness(18, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleAndStatus.Children.Add(new TextBlock
            {
                Text = selectedService.Name,
                Foreground = InkBrush,
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var statusRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 0)
            };
            statusRow.Children.Add(StatusBadge(selectedService.IsActive));
            titleAndStatus.Children.Add(statusRow);
            titleAndStatus.Children.Add(new TextBlock
            {
                Text = selectedService.IsActive
                    ? "Serviço ativo e disponível para agendamento."
                    : "Serviço inativo e indisponível para agendamento.",
                Foreground = MutedBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 7, 0, 0)
            });
            Grid.SetColumn(titleAndStatus, 1);
            Grid.SetColumn(editButton, 2);
            heading.Children.Add(titleAndStatus);
            heading.Children.Add(editButton);
            detailPanel.Children.Add(heading);
            detailPanel.Children.Add(new Border
            {
                Height = 1,
                Background = LineBrush,
                Margin = new Thickness(0, 0, 0, 2)
            });
            detailPanel.Children.Add(DetailRow(PackIconKind.ClockOutline, "Duração", $"{selectedService.DurationMinutes} min"));
            detailPanel.Children.Add(DetailRow(PackIconKind.TagOutline, "Preço", selectedService.Price.ToString("C", Brazil)));
            detailPanel.Children.Add(DetailRow(PackIconKind.Seat, "Recurso padrão", FirstFilled(selectedService.DefaultResource, "Não informado")));
            detailPanel.Children.Add(DetailRow(PackIconKind.StoreOutline, "Categoria", FirstFilled(selectedService.Category, selectedService.Segment, "Não informada")));
        }

        void RefreshList()
        {
            listPanel.Children.Clear();
            var query = searchBox.Text.Trim();
            var visible = services
                .Where(item => !activeOnly || item.IsActive)
                .Where(item => string.IsNullOrWhiteSpace(query) ||
                               item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               item.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               item.Segment.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               item.DefaultResource.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            countText.Text = ManagerCountLabel("Serviços", visible.Count);

            foreach (var service in visible)
            {
                var isSelected = service.Id == selectedService.Id;
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Children.Add(new Border
                {
                    Width = 4,
                    Background = isSelected ? AccentBrush : Brushes.Transparent,
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(0, 5, 0, 5)
                });
                var icon = new Border
                {
                    Width = 32,
                    Height = 32,
                    Background = AccentSoftBrush,
                    CornerRadius = new CornerRadius(16),
                    Margin = new Thickness(6, 0, 4, 0),
                    Child = new PackIcon
                    {
                        Kind = PackIconKind.ClipboardText,
                        Width = 17,
                        Height = 17,
                        Foreground = AccentBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(icon, 1);
                row.Children.Add(icon);
                var textPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 8, 0),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = service.Name,
                            Foreground = InkBrush,
                            FontSize = 13.5,
                            FontWeight = FontWeights.Bold,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = string.Join("  •  ", new[]
                            {
                                $"{service.DurationMinutes} min",
                                service.Price.ToString("C", Brazil),
                                service.DefaultResource
                            }.Where(part => !string.IsNullOrWhiteSpace(part))),
                            Foreground = MutedBrush,
                            FontSize = 11.5,
                            Margin = new Thickness(0, 3, 0, 0),
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };
                Grid.SetColumn(textPanel, 2);
                row.Children.Add(textPanel);
                var badge = StatusBadge(service.IsActive);
                badge.Margin = new Thickness(4, 0, 8, 0);
                badge.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(badge, 3);
                row.Children.Add(badge);
                var arrow = new PackIcon
                {
                    Kind = PackIconKind.ChevronRight,
                    Width = 18,
                    Height = 18,
                    Foreground = MutedBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = isSelected ? Visibility.Visible : Visibility.Hidden,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(arrow, 4);
                row.Children.Add(arrow);

                var button = new Button
                {
                    Style = (Style)FindResource("SidebarMenuButton"),
                    Height = 56,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    Background = isSelected ? AccentSoftBrush : Brushes.Transparent,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Content = row
                };
                AutomationProperties.SetName(button, $"Selecionar serviço {service.Name}");
                button.Click += (_, _) =>
                {
                    selectedService = service;
                    RefreshList();
                    RefreshDetails();
                };
                listPanel.Children.Add(button);
                listPanel.Children.Add(new Border { Height = 1, Background = LineBrush });
            }
        }

        filterButton.Click += (_, _) =>
        {
            activeOnly = !activeOnly;
            filterButton.Background = activeOnly ? AccentSoftBrush : Brushes.White;
            filterButton.BorderBrush = activeOnly ? AccentBrush : LineBrush;
            RefreshList();
        };
        searchBox.TextChanged += (_, _) => RefreshList();
        RefreshList();
        RefreshDetails();
    }

    private static string ManagerCountLabel(string section, int count)
    {
        var noun = section switch
        {
            "Clientes" => count == 1 ? "cliente" : "clientes",
            "Profissionais" => count == 1 ? "profissional" : "profissionais",
            "Serviços" => count == 1 ? "serviço" : "serviços",
            "Produtos" => count == 1 ? "produto" : "produtos",
            "Venda de produtos" => count == 1 ? "venda" : "vendas",
            _ => count == 1 ? "registro" : "registros"
        };

        return $"{count} {noun}";
    }

    private void AddManagerEmptyState(StackPanel body, string section, string title, string detail)
    {
        var suggestions = ManagerSuggestions(section).ToList();
        var fields = ManagerRecommendedFields(section).ToList();

        var panel = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 2, 0, 12),
            Child = new StackPanel()
        };

        var stack = (StackPanel)panel.Child;
        stack.Children.Add(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(52) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            Children =
            {
                new Border
                {
                    Width = 42,
                    Height = 42,
                    Background = AccentSoftBrush,
                    CornerRadius = new CornerRadius(AppActionRadiusValue),
                    Child = new PackIcon
                    {
                        Kind = ManagerIcon(section),
                        Foreground = AccentBrush,
                        Width = 23,
                        Height = 23,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                new StackPanel
                {
                    Margin = new Thickness(8, 0, 0, 0),
                    Children =
                    {
                        new TextBlock { Text = title, Foreground = InkBrush, FontSize = 18, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap },
                        new TextBlock { Text = detail, Foreground = MutedBrush, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) }
                    }
                }
            }
        });
        Grid.SetColumn(((Grid)stack.Children[0]).Children[1], 1);

        stack.Children.Add(new TextBlock
        {
            Text = "Comece com estes dados",
            Foreground = InkBrush,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 18, 0, 8)
        });

        foreach (var field in fields)
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"• {field}",
                Foreground = MutedBrush,
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
        }

        if (suggestions.Count > 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Sugestões para cadastrar",
                Foreground = InkBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 16, 0, 8)
            });

            var wrap = new WrapPanel();
            foreach (var suggestion in suggestions)
            {
                wrap.Children.Add(new Border
                {
                    Background = Brushes.White,
                    BorderBrush = LineBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(AppActionRadiusValue),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 8, 8),
                    Child = new TextBlock
                    {
                        Text = suggestion,
                        Foreground = InkBrush,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold
                    }
                });
            }

            stack.Children.Add(wrap);
        }

        body.Children.Add(panel);
    }

    private IEnumerable<string> ManagerRecommendedFields(string section) => section switch
    {
        "Clientes" => ["Nome", "WhatsApp", "Documento", _data.Settings.ClientDetailLabel, "Tags e observações"],
        "Profissionais" => ["Nome", "Função", "Telefone", "E-mail", "Documento", "Comissão", "Segmento atendido"],
        "Serviços" => ["Nome", "Categoria", "Descrição", "Duração", "Preço", "Comissão", "Preparação e intervalo", "Recurso padrão"],
        "Produtos" => ["Nome", "Categoria", "SKU/código", "Fornecedor", "Custo", "Preço", "Estoque mínimo"],
        "Venda de produtos" => _data.Products.Count == 0
            ? ["Cadastre ao menos um produto", "Depois registre quantidade, cliente e data da venda"]
            : ["Produto vendido", "Quantidade", "Cliente opcional", "Pagamento", "Desconto", "Baixa automática do estoque"],
        _ => ["Nome", "Detalhes", "Categoria"]
    };

    private IEnumerable<string> ManagerSuggestions(string section)
    {
        var segment = _data.Settings.BusinessSegment;
        return section switch
        {
            "Produtos" when segment.Contains("Barbearia", StringComparison.OrdinalIgnoreCase) =>
                ["Pomada modeladora", "Óleo de barba", "Shampoo", "Lâmina descartável"],
            "Produtos" when segment.Contains("Clínica", StringComparison.OrdinalIgnoreCase) =>
                ["Creme pós-procedimento", "Protetor solar", "Kit de cuidados", "Produto de manutenção"],
            "Produtos" when segment.Contains("Oficina", StringComparison.OrdinalIgnoreCase) =>
                ["Óleo", "Filtro", "Aditivo", "Palheta"],
            "Produtos" => ["Produto principal", "Kit promocional", "Item de reposição", "Produto de pós-venda"],
            "Serviços" when segment.Contains("Barbearia", StringComparison.OrdinalIgnoreCase) =>
                ["Corte masculino", "Barba", "Corte + barba", "Sobrancelha"],
            "Serviços" when segment.Contains("Clínica", StringComparison.OrdinalIgnoreCase) =>
                ["Consulta", "Retorno", "Avaliação", "Procedimento"],
            "Serviços" when segment.Contains("Oficina", StringComparison.OrdinalIgnoreCase) =>
                ["Diagnóstico", "Troca de óleo", "Revisão", "Alinhamento"],
            "Serviços" => ["Atendimento", "Avaliação", "Retorno", "Serviço completo"],
            "Profissionais" => [DefaultRoleForSegment(segment), "Auxiliar", "Especialista", "Atendente"],
            "Clientes" => ["Cliente novo", "Preferência de horário", "Observação de atendimento", "WhatsApp confirmado"],
            _ => []
        };
    }

    private static PackIconKind ManagerIcon(string section) => section switch
    {
        "Clientes" => PackIconKind.AccountGroup,
        "Profissionais" => PackIconKind.AccountTie,
        "Serviços" => PackIconKind.ClipboardText,
        "Produtos" => PackIconKind.PackageVariant,
        "Venda de produtos" => PackIconKind.Cart,
        _ => PackIconKind.Folder
    };

    private Border CreateManagerRow(EstablishmentListRow row, Action? clickAction = null)
    {
        var rowGrid = new Grid();
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        rowGrid.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PackIcon
            {
                Kind = row.Icon,
                Width = 18,
                Height = 18,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var textPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 16, 0),
            Children =
            {
                new TextBlock
                {
                    Text = row.Name,
                    Foreground = InkBrush,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 420
                },
                new TextBlock
                {
                    Text = row.Detail,
                    Foreground = MutedBrush,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 470,
                    Margin = new Thickness(0, 3, 0, 0)
                }
            }
        };
        Grid.SetColumn(textPanel, 1);
        rowGrid.Children.Add(textPanel);

        var statusBadge = Badge(row.BadgeText, row.BadgeBackground, row.BadgeForeground);
        statusBadge.VerticalAlignment = VerticalAlignment.Center;
        statusBadge.Margin = new Thickness(0, 0, clickAction is null ? 0 : 10, 0);
        Grid.SetColumn(statusBadge, 2);
        rowGrid.Children.Add(statusBadge);

        if (clickAction is not null)
        {
            var editButton = new Button
            {
                Style = (Style)FindResource("SubtleButton"),
                Height = 34,
                MinWidth = 82,
                Padding = new Thickness(10, 0, 10, 0),
                Background = Brushes.White,
                BorderBrush = LineBrush,
                Foreground = AccentBrush,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new PackIcon
                        {
                            Kind = PackIconKind.Pencil,
                            Width = 14,
                            Height = 14,
                            Foreground = AccentBrush,
                            Margin = new Thickness(0, 0, 6, 0)
                        },
                        new TextBlock
                        {
                            Text = "Editar",
                            Foreground = AccentBrush,
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };
            TextElement.SetForeground(editButton, AccentBrush);
            editButton.Click += (_, _) => clickAction();
            Grid.SetColumn(editButton, 3);
            rowGrid.Children.Add(editButton);
        }

        var rowCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(10, 9, 10, 9),
            Margin = new Thickness(0, 0, 0, 7),
            Child = rowGrid
        };

        rowCard.MouseEnter += (_, _) => rowCard.Background = AccentSoftBrush;
        rowCard.MouseLeave += (_, _) => rowCard.Background = Brushes.White;

        return rowCard;
    }

    private static Border Badge(string text, Brush background, Brush foreground)
    {
        var badge = new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(AppBadgeRadiusValue),
            Padding = new Thickness(10, 4, 10, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, Foreground = foreground, FontSize = 11, FontWeight = FontWeights.Bold }
        };
        Grid.SetColumn(badge, 1);
        return badge;
    }

    private void OpenManagerItemEditor(string section, string id)
    {
        switch (section)
        {
            case "Clientes":
                EditCustomer(id);
                break;
            case "Profissionais":
                EditProfessional(id);
                break;
            case "Serviços":
                EditService(id);
                break;
            case "Produtos":
                EditProduct(id);
                break;
            case "Venda de produtos":
                EditProductSale(id);
                break;
        }
    }

    private void EditCustomer(string id)
    {
        var customer = _data.Customers.FirstOrDefault(item => item.Id == id);
        if (customer is null)
        {
            ShowStatus("Cliente não encontrado para edição.");
            return;
        }

        var form = ShowCustomerEditorDialog(customer.Segment, customer.Name, customer.Phone, customer.Profile, customer);
        if (form is null)
        {
            return;
        }

        customer.Name = form.Name;
        customer.Phone = form.Phone;
        customer.Document = form.Document;
        customer.Segment = form.Segment;
        customer.Profile = form.Profile;
        customer.Tags = form.Tags;
        customer.Notes = form.Notes;
        customer.AcceptsWhatsApp = form.AcceptsWhatsApp;
        customer.LastSeenAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Cliente atualizado: {customer.Name}.");
    }

    private void EditProfessional(string id)
    {
        var professional = _data.Professionals.FirstOrDefault(item => item.Id == id);
        if (professional is null)
        {
            ShowStatus("Profissional não encontrado para edição.");
            return;
        }

        var segment = professional.Segments.FirstOrDefault() ?? CurrentEditorSegment();
        var form = ShowProfessionalEditorDialog(segment, professional);
        if (form is null)
        {
            return;
        }

        professional.Name = form.Name;
        professional.Role = form.Role;
        professional.Phone = form.Phone;
        professional.Email = form.Email;
        professional.Document = form.Document;
        professional.CommissionPercent = form.CommissionPercent;
        professional.Segments = [form.Segment];
        professional.Notes = form.Notes;
        professional.IsActive = form.IsActive;
        _store.Save(_data);
        UpdateAppointmentOptions(CurrentEditorSegment());
        RefreshAll();
        ShowStatus($"Profissional atualizado: {professional.Name}.");
    }

    private void EditService(string id)
    {
        var service = _data.Services.FirstOrDefault(item => item.Id == id);
        if (service is null)
        {
            ShowStatus("Serviço não encontrado para edição.");
            return;
        }

        var form = ShowServiceEditorDialog(service.Segment, service);
        if (form is null)
        {
            return;
        }

        service.Segment = form.Segment;
        service.Name = form.Name;
        service.Category = form.Category;
        service.Description = form.Description;
        service.DurationMinutes = form.DurationMinutes;
        service.PreparationMinutes = form.PreparationMinutes;
        service.BufferMinutes = form.BufferMinutes;
        service.Price = form.Price;
        service.CommissionPercent = form.CommissionPercent;
        service.DefaultResource = form.DefaultResource;
        service.IsActive = form.IsActive;
        _store.Save(_data);
        UpdateAppointmentOptions(CurrentEditorSegment());
        RefreshAll();
        ShowStatus($"Serviço atualizado: {service.Name}.");
    }

    private void EditProduct(string id)
    {
        var product = _data.Products.FirstOrDefault(item => item.Id == id);
        if (product is null)
        {
            ShowStatus("Produto não encontrado para edição.");
            return;
        }

        var form = ShowProductEditorDialog(product);
        if (form is null)
        {
            return;
        }

        product.Name = form.Name;
        product.Category = form.Category;
        product.Sku = form.Sku;
        product.Supplier = form.Supplier;
        product.CostPrice = form.CostPrice;
        product.Price = form.Price;
        product.StockQuantity = form.StockQuantity;
        product.MinimumStock = form.MinimumStock;
        product.Notes = form.Notes;
        product.IsActive = form.IsActive;
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Produto atualizado: {product.Name}.");
    }

    private void EditProductSale(string id)
    {
        var sale = _data.ProductSales.FirstOrDefault(item => item.Id == id);
        if (sale is null)
        {
            ShowStatus("Venda não encontrada para edição.");
            return;
        }

        var originalProduct = _data.Products.FirstOrDefault(item => item.Id == sale.ProductId);
        var originalQuantity = sale.Quantity;
        var form = ShowProductSaleEditorDialog(sale);
        if (form is null)
        {
            return;
        }

        if (originalProduct is not null)
        {
            originalProduct.StockQuantity += originalQuantity;
        }

        sale.ProductId = form.Product.Id;
        sale.ProductName = form.Product.Name;
        sale.CustomerName = form.CustomerName;
        sale.Quantity = form.Quantity;
        sale.UnitPrice = form.Product.Price;
        sale.Discount = form.Discount;
        sale.PaymentMethod = form.PaymentMethod;
        sale.PaymentProvider = form.PaymentProvider;
        sale.PaymentReference = form.PaymentReference;
        sale.PaymentStatus = form.PaymentStatus;
        sale.Notes = form.Notes;

        form.Product.StockQuantity = Math.Max(0, form.Product.StockQuantity - form.Quantity);
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Venda atualizada: {sale.ProductName}.");
    }

    private void ConfigureSidebarHover()
    {
        AttachSidebarHover(HomeSidebarButton, HomeSidebarIcon, HomeSidebarText, MainPage.Home);
        AttachSidebarHover(AgendaSidebarButton, AgendaSidebarIcon, AgendaSidebarText, MainPage.Agenda);
        AttachSidebarHover(FinanceSidebarButton, FinanceSidebarIcon, FinanceSidebarText, MainPage.Finance);
        AttachSidebarHover(ReportsSidebarButton, ReportsSidebarIcon, ReportsSidebarText, MainPage.Reports);
        AttachSidebarHover(EstablishmentSidebarButton, EstablishmentSidebarIcon, EstablishmentSidebarText, MainPage.Establishment);
        AttachSidebarHover(MarketingSidebarButton, MarketingSidebarIcon, MarketingSidebarText, MainPage.Marketing);
        AttachSidebarHover(SettingsSidebarButton, SettingsSidebarIcon, SettingsSidebarText, MainPage.Settings);
    }

    private void AttachSidebarHover(Button button, PackIcon icon, TextBlock label, MainPage page)
    {
        button.MouseEnter += (_, _) =>
        {
            if (_currentPage == page)
            {
                return;
            }

            icon.Foreground = SidebarActiveTextBrush;
            label.Foreground = SidebarActiveTextBrush;
        };

        button.MouseLeave += (_, _) =>
        {
            if (_currentPage == page)
            {
                return;
            }

            icon.Foreground = SidebarTextBrush;
            label.Foreground = SidebarTextBrush;
        };
    }

    private void ShowMainPage(MainPage page)
    {
        _currentPage = page;
        var showHome = page == MainPage.Home;
        var showEstablishment = page == MainPage.Establishment;
        var showFinance = page == MainPage.Finance;
        var showReports = page == MainPage.Reports;
        var showMarketing = page == MainPage.Marketing;
        var showSettings = page == MainPage.Settings;
        var showAgenda = page == MainPage.Agenda;
        HomeDashboardView.Visibility = showHome ? Visibility.Visible : Visibility.Collapsed;
        EstablishmentView.Visibility = showEstablishment ? Visibility.Visible : Visibility.Collapsed;
        FinanceView.Visibility = showFinance ? Visibility.Visible : Visibility.Collapsed;
        ReportsView.Visibility = showReports ? Visibility.Visible : Visibility.Collapsed;
        MarketingView.Visibility = showMarketing ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
        AgendaWorkspaceView.Visibility = showAgenda ? Visibility.Visible : Visibility.Collapsed;
        if (showAgenda)
        {
            ResetAgendaWorkspaceScroll();
        }

        HomeSidebarButton.Style = (Style)FindResource(showHome ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        EstablishmentSidebarButton.Style = (Style)FindResource(showEstablishment ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        FinanceSidebarButton.Style = (Style)FindResource(showFinance ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        ReportsSidebarButton.Style = (Style)FindResource(showReports ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        MarketingSidebarButton.Style = (Style)FindResource(showMarketing ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        SettingsSidebarButton.Style = (Style)FindResource(showSettings ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        AgendaSidebarButton.Style = (Style)FindResource(showAgenda ? "SidebarMenuButtonActive" : "SidebarMenuButton");
        HomeCollapsedButton.Style = (Style)FindResource(showHome ? "SidebarIconButtonActive" : "SidebarIconButton");
        EstablishmentCollapsedButton.Style = (Style)FindResource(showEstablishment ? "SidebarIconButtonActive" : "SidebarIconButton");
        FinanceCollapsedButton.Style = (Style)FindResource(showFinance ? "SidebarIconButtonActive" : "SidebarIconButton");
        ReportsCollapsedButton.Style = (Style)FindResource(showReports ? "SidebarIconButtonActive" : "SidebarIconButton");
        MarketingCollapsedButton.Style = (Style)FindResource(showMarketing ? "SidebarIconButtonActive" : "SidebarIconButton");
        SettingsCollapsedButton.Style = (Style)FindResource(showSettings ? "SidebarIconButtonActive" : "SidebarIconButton");
        AgendaCollapsedButton.Style = (Style)FindResource(showAgenda ? "SidebarIconButtonActive" : "SidebarIconButton");

        HomeSidebarButton.Background = showHome ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        EstablishmentSidebarButton.Background = showEstablishment ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        FinanceSidebarButton.Background = showFinance ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        ReportsSidebarButton.Background = showReports ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        MarketingSidebarButton.Background = showMarketing ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        SettingsSidebarButton.Background = showSettings ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        AgendaSidebarButton.Background = showAgenda ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        HomeCollapsedButton.Background = showHome ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        EstablishmentCollapsedButton.Background = showEstablishment ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        FinanceCollapsedButton.Background = showFinance ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        ReportsCollapsedButton.Background = showReports ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        MarketingCollapsedButton.Background = showMarketing ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        SettingsCollapsedButton.Background = showSettings ? SidebarActiveBackgroundBrush : Brushes.Transparent;
        AgendaCollapsedButton.Background = showAgenda ? SidebarActiveBackgroundBrush : Brushes.Transparent;

        HomeSidebarIcon.Foreground = showHome ? SidebarActiveTextBrush : SidebarTextBrush;
        HomeSidebarText.Foreground = showHome ? SidebarActiveTextBrush : SidebarTextBrush;
        HomeSidebarText.FontWeight = showHome ? FontWeights.Bold : FontWeights.SemiBold;
        EstablishmentSidebarIcon.Foreground = showEstablishment ? SidebarActiveTextBrush : SidebarTextBrush;
        EstablishmentSidebarText.Foreground = showEstablishment ? SidebarActiveTextBrush : SidebarTextBrush;
        EstablishmentSidebarText.FontWeight = showEstablishment ? FontWeights.Bold : FontWeights.SemiBold;
        FinanceSidebarIcon.Foreground = showFinance ? SidebarActiveTextBrush : SidebarTextBrush;
        FinanceSidebarText.Foreground = showFinance ? SidebarActiveTextBrush : SidebarTextBrush;
        FinanceSidebarText.FontWeight = showFinance ? FontWeights.Bold : FontWeights.SemiBold;
        ReportsSidebarIcon.Foreground = showReports ? SidebarActiveTextBrush : SidebarTextBrush;
        ReportsSidebarText.Foreground = showReports ? SidebarActiveTextBrush : SidebarTextBrush;
        ReportsSidebarText.FontWeight = showReports ? FontWeights.Bold : FontWeights.SemiBold;
        MarketingSidebarIcon.Foreground = showMarketing ? SidebarActiveTextBrush : SidebarTextBrush;
        MarketingSidebarText.Foreground = showMarketing ? SidebarActiveTextBrush : SidebarTextBrush;
        MarketingSidebarText.FontWeight = showMarketing ? FontWeights.Bold : FontWeights.SemiBold;
        SettingsSidebarIcon.Foreground = showSettings ? SidebarActiveTextBrush : SidebarTextBrush;
        SettingsSidebarText.Foreground = showSettings ? SidebarActiveTextBrush : SidebarTextBrush;
        SettingsSidebarText.FontWeight = showSettings ? FontWeights.Bold : FontWeights.SemiBold;
        AgendaSidebarIcon.Foreground = showAgenda ? SidebarActiveTextBrush : SidebarTextBrush;
        AgendaSidebarText.Foreground = showAgenda ? SidebarActiveTextBrush : SidebarTextBrush;
        AgendaSidebarText.FontWeight = showAgenda ? FontWeights.Bold : FontWeights.SemiBold;
        HomeCollapsedIcon.Foreground = showHome ? SidebarActiveTextBrush : SidebarTextBrush;
        EstablishmentCollapsedIcon.Foreground = showEstablishment ? SidebarActiveTextBrush : SidebarTextBrush;
        FinanceCollapsedIcon.Foreground = showFinance ? SidebarActiveTextBrush : SidebarTextBrush;
        ReportsCollapsedIcon.Foreground = showReports ? SidebarActiveTextBrush : SidebarTextBrush;
        MarketingCollapsedIcon.Foreground = showMarketing ? SidebarActiveTextBrush : SidebarTextBrush;
        SettingsCollapsedIcon.Foreground = showSettings ? SidebarActiveTextBrush : SidebarTextBrush;
        AgendaCollapsedIcon.Foreground = showAgenda ? SidebarActiveTextBrush : SidebarTextBrush;

        if (showHome)
        {
            RefreshHomeDashboard();
        }

        if (showEstablishment)
        {
            RefreshEstablishmentPage();
        }

        if (showFinance)
        {
            RefreshFinancePage();
            Dispatcher.BeginInvoke(() => FinanceView.ScrollToTop(), DispatcherPriority.Background);
        }

        if (showReports)
        {
            RefreshReportsPage();
        }

        if (showMarketing)
        {
            RefreshMarketingPage();
        }

        if (showSettings)
        {
            RefreshSettingsSummary();
        }

        TopBarBorder.InvalidateVisual();
        TopBrandPanel.InvalidateVisual();
        SearchTextBox.InvalidateVisual();
        DateFilterButton.InvalidateVisual();
        AppShellBodyGrid.InvalidateVisual();
        InvalidateVisual();
        Dispatcher.BeginInvoke(
            () =>
            {
                InvalidateVisual();
                UpdateLayout();
            },
            DispatcherPriority.Render);
    }

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        SidebarColumn.Width = new GridLength(_sidebarCollapsed ? 72 : 260);
        SidebarExpandedPanel.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarCollapsedPanel.Visibility = _sidebarCollapsed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void EditRegistrationButton_Click(object sender, RoutedEventArgs e)
    {
        var form = ShowRegistrationEditorDialog();
        if (form is null)
        {
            return;
        }

        ApplyRegistrationEditorForm(form);
        ShowStatus("Cadastro atualizado.");
    }

    private RegistrationEditorForm? ShowRegistrationEditorDialog()
    {
        var initialName = FirstFilled(_data.Settings.AccountFullName, BusinessDisplayName());
        var initialPhone = FirstFilled(_data.Settings.AccountPhone, _data.Settings.BusinessPhone);
        var initialSegment = FirstFilled(_data.Settings.BusinessSegment, "Barbearia");

        var dialog = new Window
        {
            Title = "Editar cadastro",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Width = 700,
            Height = 610
        };
        ConfigureRoundedDialogWindow(dialog);
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;

        var content = new DockPanel { LastChildFill = true, Background = Brushes.White };
        var header = CreateRegistrationDialogHeader(dialog);
        DockPanel.SetDock(header, Dock.Top);
        content.Children.Add(header);

        var footerErrorText = new TextBlock
        {
            Foreground = Solid("#DC2626"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };
        AutomationProperties.SetName(footerErrorText, "Erro no cadastro");
        AutomationProperties.SetLiveSetting(footerErrorText, AutomationLiveSetting.Assertive);
        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("GhostButton"),
            Height = 40,
            MinWidth = 112,
            IsCancel = true,
            Margin = new Thickness(0, 0, 10, 0)
        };
        var saveButton = new Button
        {
            Content = new TextBlock
            {
                Text = "Salvar cadastro",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.SemiBold
            },
            Style = (Style)FindResource("CommandButton"),
            Height = 40,
            MinWidth = 150,
            IsDefault = true
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var footerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, saveButton }
        };
        Grid.SetColumn(footerActions, 1);
        footerGrid.Children.Add(footerErrorText);
        footerGrid.Children.Add(footerActions);
        var footer = new Border
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, AppModalRadiusValue, AppModalRadiusValue),
            Padding = new Thickness(20, 11, 20, 13),
            Child = footerGrid
        };
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);

        var body = new StackPanel { Margin = new Thickness(24, 16, 24, 12) };
        var cardsGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var ownerBody = new StackPanel();
        var ownerNameBox = AddRegistrationTextField(ownerBody, "Nome completo", initialName, "Ex: Isabella Gomes");
        var ownerPhoneBox = AddRegistrationTextField(ownerBody, "Celular / WhatsApp", FormatCustomerPhoneInput(initialPhone), "Ex: (33) 99800-7983");
        var emailBox = AddRegistrationTextField(ownerBody, "E-mail", _data.Settings.AccountEmail, "Ex: contato@empresa.com");
        var ownerSection = CreateRegistrationFlatSection("Responsável", "Quem administra a agenda.", PackIconKind.AccountCircleOutline, ownerBody);
        ownerSection.Margin = new Thickness(0, 0, 22, 0);
        cardsGrid.Children.Add(ownerSection);

        var columnDivider = new Border { Background = LineBrush };
        Grid.SetColumn(columnDivider, 1);
        cardsGrid.Children.Add(columnDivider);

        var businessBody = new StackPanel();
        var businessNameBox = AddRegistrationTextField(businessBody, "Nome do negócio", BusinessDisplayName(), "Ex: Marquinho Barbearia");
        var segmentBox = AddRegistrationComboField(businessBody, "Segmento", BusinessRegistrationSegmentOptions(), initialSegment, editable: false);
        var documentBox = AddRegistrationTextField(businessBody, "CPF / CNPJ", FormatDocumentInput(_data.Settings.BusinessDocument), "Ex: 123.456.789-00");
        var businessSection = CreateRegistrationFlatSection("Estabelecimento", "Dados exibidos no sistema.", PackIconKind.StorefrontOutline, businessBody);
        businessSection.Margin = new Thickness(22, 0, 0, 0);
        Grid.SetColumn(businessSection, 2);
        cardsGrid.Children.Add(businessSection);
        body.Children.Add(cardsGrid);

        var addressBody = new StackPanel();
        var addressBox = AddRegistrationTextField(addressBody, "Endereço do negócio", _data.Settings.BusinessAddress, "Rua, número, bairro e cidade", multiline: true);
        var locationSection = CreateRegistrationFlatSection("Localização", "Endereço de referência do negócio.", PackIconKind.MapMarkerOutline, addressBody);
        locationSection.BorderThickness = new Thickness(0, 1, 0, 0);
        locationSection.Padding = new Thickness(0, 12, 0, 0);
        body.Children.Add(locationSection);

        var registrationScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = body
        };
        ApplyDialogScrollTheme(registrationScroll);
        content.Children.Add(registrationScroll);
        dialog.Content = WrapRoundedDialogContent(content, Brushes.White);

        foreach (var control in new Control[] { ownerNameBox, ownerPhoneBox, emailBox, businessNameBox, segmentBox, documentBox, addressBox })
        {
            control.GotKeyboardFocus += (_, _) => footerErrorText.Visibility = Visibility.Collapsed;
        }

        ownerNameBox.LostFocus += (_, _) => ownerNameBox.Text = ToNameCase(ownerNameBox.Text);
        businessNameBox.LostFocus += (_, _) => businessNameBox.Text = ToNameCase(businessNameBox.Text);
        ownerPhoneBox.TextChanged += DialogPhoneTextBox_TextChanged;
        ownerPhoneBox.LostFocus += DialogPhoneTextBox_LostFocus;
        documentBox.TextChanged += DialogDocumentTextBox_TextChanged;
        documentBox.LostFocus += DialogDocumentTextBox_LostFocus;

        RegistrationEditorForm? result = null;
        saveButton.Click += (_, _) =>
        {
            var ownerName = ToNameCase(ownerNameBox.Text);
            if (string.IsNullOrWhiteSpace(ownerName))
            {
                SetDialogError(footerErrorText, "Informe o nome completo do responsável.");
                ownerNameBox.Focus();
                return;
            }

            var businessName = ToNameCase(businessNameBox.Text);
            if (string.IsNullOrWhiteSpace(businessName))
            {
                SetDialogError(footerErrorText, "Informe o nome do negócio.");
                businessNameBox.Focus();
                return;
            }

            if (!TryNormalizeCustomerPhone(ownerPhoneBox.Text, out var phone, out var phoneError))
            {
                SetDialogError(footerErrorText, phoneError);
                ownerPhoneBox.Focus();
                return;
            }

            var email = emailBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(email) && !LooksLikeEmail(email))
            {
                SetDialogError(footerErrorText, "Informe um e-mail válido ou deixe em branco.");
                emailBox.Focus();
                return;
            }

            if (!TryFormatBusinessDocument(documentBox.Text, out var document, out var documentError))
            {
                SetDialogError(footerErrorText, documentError);
                documentBox.Focus();
                return;
            }

            var segment = DialogComboText(segmentBox, initialSegment);
            if (string.IsNullOrWhiteSpace(segment))
            {
                SetDialogError(footerErrorText, "Selecione o segmento do negócio.");
                segmentBox.Focus();
                return;
            }

            result = new RegistrationEditorForm(
                ownerName,
                phone,
                email,
                businessName,
                segment,
                document,
                addressBox.Text.Trim());
            dialog.DialogResult = true;
        };

        ownerNameBox.Focus();
        return ShowAppDialog(dialog) == true ? result : null;
    }

    private Border CreateRegistrationDialogHeader(Window dialog)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 48,
            Height = 48,
            Background = AccentBrush,
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.AccountEditOutline,
                Foreground = Brushes.White,
                Width = 25,
                Height = 25,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Editar cadastro",
            Foreground = InkBrush,
            FontSize = 23,
            FontWeight = FontWeights.Bold
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Atualize os dados salvos sem voltar para as páginas iniciais.",
            Foreground = MutedBrush,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(titleStack, 1);
        grid.Children.Add(titleStack);

        var badge = new Border
        {
            Background = AccentSoftBrush,
            BorderBrush = Solid("#F3D7C7"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "Sem reiniciar",
                Foreground = AccentBrush,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(badge, 2);
        grid.Children.Add(badge);

        var closeButton = CreateDialogCloseButton(dialog);
        Grid.SetColumn(closeButton, 3);
        grid.Children.Add(closeButton);

        var header = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(AppModalRadiusValue, AppModalRadiusValue, 0, 0),
            Padding = new Thickness(22, 18, 22, 18),
            Child = grid
        };
        EnableDialogDrag(header, dialog);
        return header;
    }

    private Border CreateRegistrationCard(string title, string subtitle, PackIconKind icon, Brush iconBackground, Brush iconForeground, UIElement content)
    {
        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Solid("#EADFD6"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(16),
            Effect = new DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 5,
                Opacity = 0.06,
                Color = Color.FromRgb(15, 23, 42)
            }
        };

        var stack = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.Children.Add(new Border
        {
            Width = 38,
            Height = 38,
            Background = iconBackground,
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = icon,
                Foreground = iconForeground,
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var headerText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        headerText.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        headerText.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(headerText, 1);
        header.Children.Add(headerText);
        stack.Children.Add(header);
        stack.Children.Add(content);
        card.Child = stack;
        return card;
    }

    private Border CreateRegistrationFlatSection(string title, string subtitle, PackIconKind icon, UIElement content)
    {
        var section = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = LineBrush
        };

        var stack = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.Children.Add(new PackIcon
        {
            Kind = icon,
            Foreground = AccentBrush,
            Width = 20,
            Height = 20,
            Margin = new Thickness(0, 2, 10, 0),
            VerticalAlignment = VerticalAlignment.Top
        });

        var headerText = new StackPanel();
        headerText.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        headerText.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(headerText, 1);
        header.Children.Add(headerText);
        stack.Children.Add(header);
        stack.Children.Add(content);
        section.Child = stack;
        return section;
    }

    private TextBox AddRegistrationTextField(StackPanel body, string label, string value, string hint, bool multiline = false)
    {
        body.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 5)
        });

        var input = new TextBox
        {
            Text = value,
            Tag = hint,
            Style = (Style)FindResource(multiline ? "AppointmentMessageBox" : "AppointmentInputBox"),
            Height = multiline ? 70 : 39,
            MinWidth = 220,
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            CaretBrush = AccentBrush,
            SelectionBrush = AccentSoftBrush,
            SelectionTextBrush = InkBrush,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            AcceptsReturn = multiline,
            Margin = new Thickness(0, 0, 0, 11)
        };
        AutomationProperties.SetName(input, label);
        AutomationProperties.SetHelpText(input, hint);
        body.Children.Add(input);
        return input;
    }

    private ComboBox AddRegistrationComboField<T>(StackPanel body, string label, IEnumerable<T> items, object? selected, bool editable)
    {
        body.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 5)
        });

        var combo = new ComboBox
        {
            Style = (Style)FindResource("AppointmentComboBox"),
            ItemsSource = items.ToList(),
            SelectedItem = selected,
            IsEditable = editable,
            Height = 39,
            MinWidth = 220,
            Padding = new Thickness(12, 0, 12, 0),
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            Margin = new Thickness(0, 0, 0, 11)
        };
        if (selected is string selectedText && editable)
        {
            combo.Text = selectedText;
        }

        AutomationProperties.SetName(combo, label);
        AutomationProperties.SetHelpText(
            combo,
            editable ? $"Digite ou selecione {label.ToLowerInvariant()}." : $"Selecione {label.ToLowerInvariant()}.");
        ApplyEditableComboTheme(combo);

        body.Children.Add(combo);
        return combo;
    }

    private void ApplyRegistrationEditorForm(RegistrationEditorForm form)
    {
        var previousSegment = _data.Settings.BusinessSegment;
        var template = ResolveBusinessTemplate(form.BusinessSegment);
        var nextSegment = template.Segment;

        _data.Settings.AccountFullName = form.OwnerName;
        _data.Settings.AccountPhone = form.Phone;
        _data.Settings.AccountEmail = form.Email;
        _data.Settings.BusinessName = form.BusinessName;
        _data.Settings.BusinessPhone = form.Phone;
        _data.Settings.BusinessDocument = form.Document;
        _data.Settings.BusinessAddress = form.Address;
        _data.Settings.BusinessSegment = nextSegment;
        _data.Settings.ClientLabel = template.ClientLabel;
        _data.Settings.ClientDetailLabel = template.ClientDetailLabel;
        _data.Settings.ResourceLabel = template.ResourceLabel;
        _data.Settings.OnboardingCompleted = true;

        if (_data.Settings.Resources.Count == 0)
        {
            _data.Settings.Resources = [.. template.Resources];
        }

        if (!string.IsNullOrWhiteSpace(previousSegment) &&
            !previousSegment.Equals(nextSegment, StringComparison.OrdinalIgnoreCase))
        {
            RenameSegmentForExistingData(previousSegment, nextSegment);
            _selectedSegmentFilter = AllSegments;
        }

        _store.Save(_data);
        ConfigureOnboardingInputs();
        ConfigureInputs();
        ApplyBusinessLabels();
        RefreshAll();
        ShowMainPage(MainPage.Settings);
    }

    private IEnumerable<string> BusinessRegistrationSegmentOptions()
    {
        var options = new List<string>
        {
            "Barbearia",
            "Salão de Beleza",
            "Centro de Estética",
            "Esmalteria",
            "Podologia",
            "Spa",
            "Clínica médica",
            "Petshop",
            "Oficina",
            "Outro segmento"
        };

        if (!string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment) &&
            !options.Any(item => item.Equals(_data.Settings.BusinessSegment, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, _data.Settings.BusinessSegment);
        }

        return options;
    }

    private void RenameSegmentForExistingData(string previousSegment, string nextSegment)
    {
        foreach (var service in _data.Services.Where(item => item.Segment.Equals(previousSegment, StringComparison.OrdinalIgnoreCase)))
        {
            service.Segment = nextSegment;
        }

        foreach (var professional in _data.Professionals)
        {
            for (var index = 0; index < professional.Segments.Count; index++)
            {
                if (professional.Segments[index].Equals(previousSegment, StringComparison.OrdinalIgnoreCase))
                {
                    professional.Segments[index] = nextSegment;
                }
            }
        }

        foreach (var customer in _data.Customers.Where(item => item.Segment.Equals(previousSegment, StringComparison.OrdinalIgnoreCase)))
        {
            customer.Segment = nextSegment;
        }

        foreach (var appointment in _data.Appointments.Where(item => item.Segment.Equals(previousSegment, StringComparison.OrdinalIgnoreCase)))
        {
            appointment.Segment = nextSegment;
        }
    }

    private void ExitCurrentSystemButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ShowExitSystemConfirmation())
        {
            return;
        }

        _selectedAppointment = null;
        _selectedSegmentFilter = AllSegments;

        _data.Settings.BusinessName = "Balcão Livre";
        _data.Settings.BusinessDocument = "";
        _data.Settings.BusinessPhone = "";
        _data.Settings.BusinessAddress = "";
        _data.Settings.AccountFullName = "";
        _data.Settings.AccountPhone = "";
        _data.Settings.AccountEmail = "";
        _data.Settings.BusinessSegment = "";
        _data.Settings.ClientLabel = "Cliente";
        _data.Settings.ClientDetailLabel = "Paciente / pet / veículo / preferência";
        _data.Settings.ResourceLabel = "Sala, box ou cadeira";
        _data.Settings.WorkdayStartHour = 8;
        _data.Settings.WorkdayEndHour = 20;
        _data.Settings.Workdays = [1, 2, 3, 4, 5, 6];
        _data.Settings.WorkdayBreakEnabled = true;
        _data.Settings.WorkdayBreakStartHour = 12;
        _data.Settings.WorkdayBreakEndHour = 13;
        _data.Settings.Resources = [];
        _data.Settings.ProfessionalCountRange = "";
        _data.Settings.MainObjective = "";
        _data.Settings.PostalCode = "";
        _data.Settings.Neighborhood = "";
        _data.Settings.Street = "";
        _data.Settings.AddressNumber = "";
        _data.Settings.AddressComplement = "";
        _data.Settings.AccountPasswordHash = "";
        _data.Settings.AccountCreatedAt = DateTime.MinValue;
        _data.Settings.MercadoPagoEnabled = false;
        _data.Settings.MercadoPagoConnected = false;
        _data.Settings.MercadoPagoLicenseKey = "";
        _data.Settings.MercadoPagoPaymentsApiUrl = DefaultMercadoPagoPaymentsApiUrl;
        _data.Settings.MercadoPagoSellerUserId = "";
        _data.Settings.MercadoPagoDefaultTerminalId = "";
        _data.Settings.MercadoPagoDefaultTerminalLabel = "";
        _data.Settings.MercadoPagoLastError = "";
        _data.Settings.MercadoPagoLastSyncAt = null;
        _data.Settings.OnboardingCompleted = false;

        _store.Save(_data);
        ApplyBusinessLabels();
        ConfigureInputs();
        ConfigureOnboardingInputs();
        ClearEditor();
        RefreshAll();
        ShowOnboarding();
        ShowStatus("Você saiu do sistema atual. Escolha um setor para iniciar outra agenda.");
    }

    private bool ShowExitSystemConfirmation()
    {
        var dialog = new Window
        {
            Owner = this,
            Title = "Sair do sistema",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            SizeToContent = SizeToContent.WidthAndHeight,
            Background = Brushes.Transparent
        };

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("GhostButton"),
            Height = 40,
            MinWidth = 128,
            IsCancel = true,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var confirmButton = new Button
        {
            Content = "Sair agora",
            Style = (Style)FindResource("ExitButton"),
            Height = 40,
            MinWidth = 128,
            IsDefault = true
        };

        cancelButton.Click += (_, _) => dialog.DialogResult = false;
        confirmButton.Click += (_, _) => dialog.DialogResult = true;

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0),
            Children = { cancelButton, confirmButton }
        };

        var content = new Border
        {
            Width = 500,
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(22),
            Effect = new DropShadowEffect
            {
                BlurRadius = 30,
                ShadowDepth = 8,
                Opacity = 0.18,
                Color = Color.FromRgb(15, 23, 42)
            },
            Child = new StackPanel
            {
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new GridLength(56) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                        Children =
                        {
                            new Border
                            {
                                Width = 44,
                                Height = 44,
                                Background = Solid("#FEE2E2"),
                                CornerRadius = new CornerRadius(12),
                                VerticalAlignment = VerticalAlignment.Top,
                                Child = new PackIcon
                                {
                                    Kind = PackIconKind.Logout,
                                    Width = 24,
                                    Height = 24,
                                    Foreground = Solid("#DC2626"),
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            },
                            new StackPanel
                            {
                                Margin = new Thickness(4, 0, 0, 0),
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "Sair do sistema?",
                                        Foreground = InkBrush,
                                        FontSize = 22,
                                        FontWeight = FontWeights.Bold
                                    },
                                    new TextBlock
                                    {
                                        Text = "Você vai voltar para a configuração inicial.",
                                        Foreground = MutedBrush,
                                        FontSize = 13,
                                        TextWrapping = TextWrapping.Wrap,
                                        Margin = new Thickness(0, 4, 0, 0)
                                    }
                                }
                            }
                        }
                    },
                    new Border
                    {
                        Background = Solid("#FFF9F4"),
                        BorderBrush = LineBrush,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(14, 12, 14, 12),
                        Margin = new Thickness(0, 18, 0, 0),
                        Child = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "Seus dados continuam salvos neste computador.",
                                    Foreground = InkBrush,
                                    FontSize = 14,
                                    FontWeight = FontWeights.Bold,
                                    TextWrapping = TextWrapping.Wrap
                                },
                                new TextBlock
                                {
                                    Text = "Eles só serão substituídos se você criar outra agenda.",
                                    Foreground = MutedBrush,
                                    FontSize = 12.5,
                                    TextWrapping = TextWrapping.Wrap,
                                    Margin = new Thickness(0, 4, 0, 0)
                                }
                            }
                        }
                    },
                    actions
                }
            }
        };

        Grid.SetColumn((FrameworkElement)((Grid)((StackPanel)content.Child).Children[0]).Children[1], 1);
        dialog.Content = content;
        return ShowAppDialog(dialog) == true;
    }

    private void RefreshSettingsSummary()
    {
        var businessParts = new[]
        {
            IsDefaultBusinessName(_data.Settings.BusinessName) ? "Balcão Livre" : _data.Settings.BusinessName,
            _data.Settings.BusinessSegment,
            _data.Settings.BusinessDocument,
            _data.Settings.BusinessPhone
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        SettingsBusinessText.Text = string.Join(" | ", businessParts);
        ServicesCountText.Text = $"{_data.Services.Count} serviço(s) cadastrados";
        ProfessionalsCountText.Text = $"{_data.Professionals.Count} profissional(is) cadastrados";
        ResourcesCountText.Text = $"{ConfiguredWorkdaysSummary()} · {_data.Settings.WorkdayStartHour:00}:00 às {_data.Settings.WorkdayEndHour:00}:00";
        RefreshMercadoPagoSettingsSummary();
    }

    private void RefreshMercadoPagoSettingsSummary()
    {
        var terminal = MercadoPagoTerminalLabel();
        if (!_data.Settings.MercadoPagoEnabled)
        {
            SettingsMercadoPagoStatusText.Text = "Desativado";
            SettingsMercadoPagoStatusText.Foreground = MutedBrush;
            SettingsMercadoPagoDetailText.Text = "Ative cartão na maquininha.";
            return;
        }

        if (!_data.Settings.MercadoPagoConnected)
        {
            SettingsMercadoPagoStatusText.Text = "Ativado, falta conectar";
            SettingsMercadoPagoStatusText.Foreground = Solid("#D97706");
            SettingsMercadoPagoDetailText.Text = "Conecte a conta da loja.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoDefaultTerminalId))
        {
            SettingsMercadoPagoStatusText.Text = "Conectado, sem Point";
            SettingsMercadoPagoStatusText.Foreground = Solid("#D97706");
            SettingsMercadoPagoDetailText.Text = "Escolha a Point da loja.";
            return;
        }

        SettingsMercadoPagoStatusText.Text = "Maquininha pronta";
        SettingsMercadoPagoStatusText.Foreground = Solid("#16A34A");
        SettingsMercadoPagoDetailText.Text = $"Point: {terminal}.";
    }

    private void OpenMercadoPagoSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var shell = CreateFinanceEditorDialog(
            "Mercado Pago",
            "Ative a conta e escolha a Point usada nos pagamentos da agenda.",
            "Salvar configuração",
            PackIconKind.CreditCardOutline,
            useBodyCard: false);
        shell.Dialog.Width = 920;
        shell.Dialog.MaxHeight = 680;

        var enabledToggle = new ToggleButton
        {
            Style = (Style)FindResource("MaterialDesignSwitchToggleButton"),
            IsChecked = _data.Settings.MercadoPagoEnabled,
            Width = 54,
            Height = 32,
            MinWidth = 54,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Ativar Mercado Pago nos pagamentos"
        };
        enabledToggle.SetResourceReference(Control.ForegroundProperty, "Accent");
        AutomationProperties.SetName(enabledToggle, "Usar Mercado Pago nos pagamentos");

        var statusBadgeText = new TextBlock
        {
            Text = "Desativado",
            FontSize = 11.8,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        var statusBadge = new Border
        {
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppBadgeRadiusValue),
            Padding = new Thickness(11, 4, 11, 4),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = statusBadgeText
        };

        var activationTitleLine = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new TextBlock
                {
                    Text = "Mercado Pago na agenda",
                    Foreground = InkBrush,
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                },
                statusBadge
            }
        };
        var activationText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 18, 0),
            Children =
            {
                activationTitleLine,
                new TextBlock
                {
                    Text = "Ative para cobrar cartão na Point e registrar o pagamento só depois da aprovação.",
                    Foreground = MutedBrush,
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                }
            }
        };
        var activationIcon = new PackIcon
        {
            Kind = PackIconKind.Store,
            Width = 22,
            Height = 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        activationIcon.SetResourceReference(Control.ForegroundProperty, "Accent");
        var activationIconBadge = new Border
        {
            Width = 46,
            Height = 46,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Child = activationIcon
        };
        var activationControl = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "Usar Mercado Pago nos pagamentos",
                    Foreground = InkBrush,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0)
                },
                enabledToggle
            }
        };
        var activationGrid = new Grid { Margin = new Thickness(0, 0, 0, 24) };
        activationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        activationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        activationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        activationGrid.Children.Add(activationIconBadge);
        Grid.SetColumn(activationText, 1);
        activationGrid.Children.Add(activationText);
        Grid.SetColumn(activationControl, 2);
        activationGrid.Children.Add(activationControl);
        shell.Body.Children.Add(activationGrid);

        var connectButton = new Button
        {
            Content = "Conectar",
            Style = (Style)FindResource("CommandButton"),
            Width = 184,
            Height = 42,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var statusButton = new Button
        {
            Content = "Checar conta",
            Style = (Style)FindResource("MercadoPagoOutlineButton"),
            Width = 184,
            Height = 42,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var terminalsButton = new Button
        {
            Content = "Buscar Points",
            Style = (Style)FindResource("MercadoPagoOutlineButton"),
            Width = 184,
            Height = 42,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        StackPanel CreateSetupStage(PackIconKind iconKind, string title, string description, Button actionButton)
        {
            var stageIcon = new PackIcon
            {
                Kind = iconKind,
                Width = 25,
                Height = 25,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stageIcon.SetResourceReference(Control.ForegroundProperty, "Accent");
            var iconBadge = new Border
            {
                Width = 54,
                Height = 54,
                Background = AccentSoftBrush,
                CornerRadius = new CornerRadius(27),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = stageIcon
            };
            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Children =
                {
                    iconBadge,
                    new TextBlock
                    {
                        Text = title,
                        Foreground = InkBrush,
                        FontSize = 15.5,
                        FontWeight = FontWeights.SemiBold,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 11, 0, 0)
                    },
                    new TextBlock
                    {
                        Text = description,
                        Foreground = MutedBrush,
                        FontSize = 12.5,
                        LineHeight = 17,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Width = 190,
                        MinHeight = 38,
                        Margin = new Thickness(12, 5, 12, 11)
                    },
                    actionButton
                }
            };
        }

        var stages = new Grid { Margin = new Thickness(30, 0, 30, 24) };
        stages.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stages.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stages.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var firstStage = CreateSetupStage(PackIconKind.Link, "Conectar conta", "Conecte sua conta do Mercado Pago à agenda.", connectButton);
        var secondStage = CreateSetupStage(PackIconKind.CheckCircleOutline, "Verificar conta", "Verificaremos o acesso à sua conta.", statusButton);
        var thirdStage = CreateSetupStage(PackIconKind.CreditCardOutline, "Encontrar maquininha", "Encontre e selecione a Point da sua loja.", terminalsButton);
        var firstConnector = new Border
        {
            Height = 1,
            Background = LineBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(170, 27, 170, 0)
        };
        var secondConnector = new Border
        {
            Height = 1,
            Background = LineBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(170, 27, 170, 0)
        };
        stages.Children.Add(firstConnector);
        Grid.SetColumnSpan(firstConnector, 2);
        Grid.SetColumn(secondConnector, 1);
        Grid.SetColumnSpan(secondConnector, 2);
        stages.Children.Add(secondConnector);
        stages.Children.Add(firstStage);
        Grid.SetColumn(secondStage, 1);
        stages.Children.Add(secondStage);
        Grid.SetColumn(thirdStage, 2);
        stages.Children.Add(thirdStage);
        shell.Body.Children.Add(stages);

        var terminalOptions = CurrentMercadoPagoTerminalOptions();
        var terminalBox = AddFinanceDialogComboField(
            shell.Body,
            "Point da loja",
            terminalOptions,
            terminalOptions.FirstOrDefault(item => item.Id == _data.Settings.MercadoPagoDefaultTerminalId),
            editable: false);
        terminalBox.DisplayMemberPath = nameof(AgendaMercadoPagoTerminalDto.Display);
        terminalBox.Height = 58;
        terminalBox.Margin = new Thickness(0, 0, 0, 5);
        AutomationProperties.SetHelpText(terminalBox, "Conecte e verifique a conta antes de escolher uma Point.");
        var terminalHelpText = new TextBlock
        {
            Text = "Conecte e verifique a conta para escolher uma Point.",
            Foreground = MutedBrush,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 0, 0, 18)
        };
        shell.Body.Children.Add(terminalHelpText);

        var infoIcon = new PackIcon
        {
            Kind = PackIconKind.InformationOutline,
            Width = 22,
            Height = 22,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        infoIcon.SetResourceReference(Control.ForegroundProperty, "Accent");
        var infoText = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(11, 0, 0, 0),
            Children =
            {
                new TextBlock
                {
                    Text = "Como funciona",
                    Foreground = InkBrush,
                    FontSize = 13.5,
                    FontWeight = FontWeights.Bold
                },
                new TextBlock
                {
                    Text = "Conecte a conta, verifique o acesso e selecione a Point da loja.",
                    Foreground = MutedBrush,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                }
            }
        };
        var infoGrid = new Grid();
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        infoGrid.Children.Add(infoIcon);
        Grid.SetColumn(infoText, 1);
        infoGrid.Children.Add(infoText);
        shell.Body.Children.Add(new Border
        {
            MinHeight = 62,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Padding = new Thickness(14, 10, 14, 10),
            Child = infoGrid
        });

        void CopyDialogFieldsToSettings()
        {
            _data.Settings.MercadoPagoEnabled = enabledToggle.IsChecked == true;
            EnsureMercadoPagoInternalSettings();
            if (terminalBox.SelectedItem is AgendaMercadoPagoTerminalDto terminal)
            {
                _data.Settings.MercadoPagoDefaultTerminalId = terminal.Id.Trim();
                _data.Settings.MercadoPagoDefaultTerminalLabel = terminal.Display.Trim();
            }
        }

        void RefreshDialogStatus()
        {
            enabledToggle.IsChecked = _data.Settings.MercadoPagoEnabled;
            RefreshDialogVisualState();
        }

        void RefreshDialogVisualState()
        {
            var enabled = enabledToggle.IsChecked == true;
            var connected = _data.Settings.MercadoPagoConnected;
            var hasPoint = !string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoDefaultTerminalId);
            terminalBox.IsEnabled = connected;

            if (!enabled)
            {
                statusBadgeText.Text = "Desativado";
                statusBadgeText.Foreground = AccentDarkBrush;
                statusBadge.Background = AccentSoftBrush;
                terminalHelpText.Text = "Conecte e verifique a conta para escolher uma Point.";
                terminalHelpText.Foreground = MutedBrush;
                return;
            }

            if (!connected)
            {
                statusBadgeText.Text = "Falta conectar";
                statusBadgeText.Foreground = Solid("#B45309");
                statusBadge.Background = Solid("#FFF7ED");
                terminalHelpText.Text = FirstFilled(_data.Settings.MercadoPagoLastError, "Conecte a conta e depois clique em Checar conta.");
                terminalHelpText.Foreground = Solid("#B45309");
                return;
            }

            if (!hasPoint)
            {
                statusBadgeText.Text = "Sem Point";
                statusBadgeText.Foreground = Solid("#B45309");
                statusBadge.Background = Solid("#FFF7ED");
                terminalHelpText.Text = FirstFilled(_data.Settings.MercadoPagoLastError, "Conta conectada. Busque e escolha uma Point.");
                terminalHelpText.Foreground = Solid("#B45309");
                return;
            }

            statusBadgeText.Text = "Pronto";
            statusBadgeText.Foreground = Solid("#166534");
            statusBadge.Background = Solid("#DCFCE7");
            terminalHelpText.Text = $"Point selecionada: {MercadoPagoTerminalLabel()}.";
            terminalHelpText.Foreground = Solid("#166534");
        }

        async Task RefreshTerminalsAsync()
        {
            CopyDialogFieldsToSettings();
            var terminals = await FetchMercadoPagoTerminalsAsync();
            if (!terminals.Ok)
            {
                _data.Settings.MercadoPagoLastError = FirstFilled(terminals.Message, "Não consegui buscar as maquininhas Mercado Pago.");
                RefreshDialogStatus();
                return;
            }

            ApplyMercadoPagoTerminals(terminals);
            var list = CurrentMercadoPagoTerminalOptions();
            terminalBox.ItemsSource = list;
            terminalBox.SelectedItem = list.FirstOrDefault(item => item.Id == _data.Settings.MercadoPagoDefaultTerminalId);
            _store.Save(_data);
            RefreshDialogStatus();
        }

        connectButton.Click += async (_, _) =>
        {
            CopyDialogFieldsToSettings();
            _data.Settings.MercadoPagoEnabled = true;
            _store.Save(_data);
            var result = await StartMercadoPagoConnectAsync();
            if (!result.Ok || string.IsNullOrWhiteSpace(result.AuthUrl))
            {
                _data.Settings.MercadoPagoLastError = FirstFilled(result.Message, "Não consegui iniciar a conexão com o Mercado Pago.");
                RefreshDialogStatus();
                return;
            }

            Process.Start(new ProcessStartInfo(result.AuthUrl) { UseShellExecute = true });
            _data.Settings.MercadoPagoLastError = "Autorize no navegador e depois clique em Checar conta.";
            _store.Save(_data);
            RefreshDialogStatus();
        };

        statusButton.Click += async (_, _) =>
        {
            CopyDialogFieldsToSettings();
            var status = await FetchMercadoPagoConnectionStatusAsync();
            ApplyMercadoPagoStatus(status);
            _store.Save(_data);
            RefreshDialogStatus();
            if (status.Ok && status.Connected)
            {
                await RefreshTerminalsAsync();
            }
        };

        terminalsButton.Click += async (_, _) => await RefreshTerminalsAsync();
        enabledToggle.Checked += (_, _) => RefreshDialogVisualState();
        enabledToggle.Unchecked += (_, _) => RefreshDialogVisualState();

        shell.PrimaryButton.Click += (_, _) =>
        {
            CopyDialogFieldsToSettings();
            _store.Save(_data);
            RefreshSettingsSummary();
            shell.Dialog.DialogResult = true;
        };

        RefreshDialogStatus();
        ShowAppDialog(shell.Dialog);
        RefreshSettingsSummary();
    }

    private string MercadoPagoSettingsDetailText()
    {
        if (!_data.Settings.MercadoPagoEnabled)
        {
            return "Desativado. Ative para aparecer no pagamento como crédito/débito pela maquininha.";
        }

        if (!_data.Settings.MercadoPagoConnected)
        {
            return FirstFilled(_data.Settings.MercadoPagoLastError, "Conta ainda não conectada. Clique em Conectar e autorize no navegador.");
        }

        if (string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoDefaultTerminalId))
        {
            return FirstFilled(_data.Settings.MercadoPagoLastError, "Conta conectada. Busque e escolha uma maquininha Point.");
        }

        return $"Pronto: {MercadoPagoTerminalLabel()} selecionada. Cartão na agenda vai direto para a maquininha.";
    }

    private string MercadoPagoPaymentHintText()
    {
        if (IsMercadoPagoPointReady())
        {
            return $"Pronto para cobrar em {MercadoPagoTerminalLabel()}. O sistema só registra o pagamento depois da aprovação.";
        }

        if (_data.Settings.MercadoPagoEnabled)
        {
            return "Mercado Pago ativado, mas falta conectar a conta ou escolher a Point em Configurações.";
        }

        return "Mercado Pago está desativado. Ative em Configurações para liberar crédito/débito pela maquininha.";
    }

    private List<AgendaMercadoPagoTerminalDto> CurrentMercadoPagoTerminalOptions()
    {
        if (string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoDefaultTerminalId))
        {
            return [];
        }

        return
        [
            new AgendaMercadoPagoTerminalDto
            {
                Id = _data.Settings.MercadoPagoDefaultTerminalId,
                Label = MercadoPagoTerminalLabel()
            }
        ];
    }

    private void ApplyMercadoPagoStatus(AgendaMercadoPagoConnectionStatusResult result)
    {
        if (!result.Ok)
        {
            _data.Settings.MercadoPagoLastError = FirstFilled(result.Message, "Não consegui consultar o Mercado Pago.");
            return;
        }

        _data.Settings.MercadoPagoConnected = result.Connected;
        _data.Settings.MercadoPagoSellerUserId = result.SellerUserId ?? "";
        _data.Settings.MercadoPagoLastError = result.LastError ?? "";
        _data.Settings.MercadoPagoLastSyncAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(result.SelectedTerminalId))
        {
            _data.Settings.MercadoPagoDefaultTerminalId = result.SelectedTerminalId.Trim();
            _data.Settings.MercadoPagoDefaultTerminalLabel = FirstFilled(result.SelectedTerminalLabel, result.SelectedTerminalId).Trim();
        }
    }

    private void ApplyMercadoPagoTerminals(AgendaMercadoPagoTerminalsResult result)
    {
        if (!result.Ok)
        {
            _data.Settings.MercadoPagoLastError = FirstFilled(result.Message, "Não consegui listar as Points.");
            return;
        }

        var selected = result.Terminals
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .FirstOrDefault(item => item.Id.Equals(result.SelectedTerminalId, StringComparison.OrdinalIgnoreCase))
            ?? result.Terminals.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Id));
        if (selected is null)
        {
            _data.Settings.MercadoPagoDefaultTerminalId = "";
            _data.Settings.MercadoPagoDefaultTerminalLabel = "";
            _data.Settings.MercadoPagoLastError = "Conta conectada, mas nenhuma Point apareceu para esta loja.";
            return;
        }

        _data.Settings.MercadoPagoDefaultTerminalId = selected.Id.Trim();
        _data.Settings.MercadoPagoDefaultTerminalLabel = selected.Display.Trim();
        _data.Settings.MercadoPagoLastError = "";
        _data.Settings.MercadoPagoLastSyncAt = DateTime.Now;
    }

    private Task<AgendaMercadoPagoConnectResult> StartMercadoPagoConnectAsync() =>
        PostMercadoPagoOperationAsync<AgendaMercadoPagoConnectResult>(
            "/mercadopago/connect/start",
            FillMercadoPagoPayload(new AgendaMercadoPagoClientPayload { EventName = "agendalivre.mercadopago.connect" }),
            TimeSpan.FromSeconds(12));

    private Task<AgendaMercadoPagoConnectionStatusResult> FetchMercadoPagoConnectionStatusAsync() =>
        PostMercadoPagoOperationAsync<AgendaMercadoPagoConnectionStatusResult>(
            "/mercadopago/status",
            FillMercadoPagoPayload(new AgendaMercadoPagoClientPayload { EventName = "agendalivre.mercadopago.status" }),
            TimeSpan.FromSeconds(12));

    private Task<AgendaMercadoPagoTerminalsResult> FetchMercadoPagoTerminalsAsync() =>
        PostMercadoPagoOperationAsync<AgendaMercadoPagoTerminalsResult>(
            "/mercadopago/terminals",
            FillMercadoPagoPayload(new AgendaMercadoPagoClientPayload { EventName = "agendalivre.mercadopago.terminals" }),
            TimeSpan.FromSeconds(16));

    private Task<AgendaMercadoPagoChargeResult> CreateMercadoPagoPointChargeAsync(decimal amount, string method, string description)
    {
        var payload = FillMercadoPagoPayload(new AgendaMercadoPagoChargePayload
        {
            EventName = "agendalivre.mercadopago.point.charge",
            Amount = amount.ToString("0.00", CultureInfo.InvariantCulture),
            Method = MercadoPagoPointMethodCode(method),
            LocalReference = BuildMercadoPagoLocalReference(),
            Description = ClipMercadoPagoDescription(description),
            TerminalId = _data.Settings.MercadoPagoDefaultTerminalId,
            Items =
            [
                new AgendaMercadoPagoItemPayload
                {
                    Code = "AGENDALIVRE",
                    Title = "Agenda Livre",
                    Quantity = 1,
                    UnitPrice = amount.ToString("0.00", CultureInfo.InvariantCulture),
                    Description = ClipMercadoPagoDescription(description)
                }
            ]
        });
        return PostMercadoPagoOperationAsync<AgendaMercadoPagoChargeResult>(
            "/mercadopago/point/charge",
            payload,
            TimeSpan.FromSeconds(16));
    }

    private Task<AgendaMercadoPagoPointStatusResult> FetchMercadoPagoPointStatusAsync(string attemptId, string orderId, string localReference)
    {
        var payload = FillMercadoPagoPayload(new AgendaMercadoPagoPointStatusPayload
        {
            EventName = "agendalivre.mercadopago.point.status",
            AttemptId = attemptId,
            OrderId = orderId,
            LocalReference = localReference
        });
        return PostMercadoPagoOperationAsync<AgendaMercadoPagoPointStatusResult>(
            "/mercadopago/point/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private async Task<AgendaMercadoPagoPaymentOutcome?> ProcessMercadoPagoPointPaymentAsync(string method, decimal amount, string payer, string description, Window owner)
    {
        if (!IsMercadoPagoPointReady())
        {
            MessageBox.Show(owner, "Ative o Mercado Pago em Configurações, conecte a conta e escolha a maquininha Point.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var charge = await CreateMercadoPagoPointChargeAsync(amount, method, description);
        if (!charge.Ok)
        {
            MessageBox.Show(owner, FirstFilled(charge.Message, "Mercado Pago recusou a cobrança."), "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var cancelled = false;
        var paid = false;
        var lastStatus = FirstFilled(charge.Status, "criado");
        var waitShell = CreateFinanceEditorDialog(
            "Mercado Pago Point",
            "Acompanhe a confirmação da cobrança enviada para a maquininha.",
            "Parar espera",
            PackIconKind.CreditCardOutline);
        var waitDialog = waitShell.Dialog;
        waitDialog.Owner = owner;
        waitDialog.Width = 560;
        waitDialog.MaxHeight = 520;

        var statusText = new TextBlock
        {
            Text = $"Cobrança enviada para {MercadoPagoTerminalLabel()}.",
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        var detailText = new TextBlock
        {
            Text = $"{amount.ToString("C", Brazil)} | {method}",
            Foreground = MutedBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };
        AddFinanceDialogSection(
            waitShell.Body,
            PackIconKind.CreditCard,
            "Passe o cartão na maquininha",
            "O pagamento só será salvo depois que o Mercado Pago confirmar a aprovação.");
        waitShell.Body.Children.Add(new Border
        {
            Background = AccentSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Children = { statusText, detailText }
            }
        });
        void CancelWait()
        {
            if (paid)
            {
                return;
            }

            cancelled = true;
            if (waitDialog.IsVisible)
            {
                waitDialog.Close();
            }
        }

        waitShell.PrimaryButton.Click += (_, _) => CancelWait();
        waitShell.CancelButton.Click += (_, _) => CancelWait();
        waitDialog.PreviewKeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape)
            {
                return;
            }

            CancelWait();
            args.Handled = true;
        };
        waitDialog.Closed += (_, _) =>
        {
            cancelled = !paid;
            owner.IsEnabled = true;
            owner.Activate();
        };
        owner.IsEnabled = false;
        try
        {
            waitDialog.Show();
        }
        catch
        {
            owner.IsEnabled = true;
            throw;
        }

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
                detailText.Text = FirstFilled(status.Message, $"Aguardando retorno. Último status: {lastStatus}");
                continue;
            }

            lastStatus = FirstFilled(status.Status, lastStatus);
            detailText.Text = $"Status: {lastStatus} | tentativa {attempt + 1}/45";
            if (status.Paid)
            {
                paid = true;
                waitDialog.Close();
                return new AgendaMercadoPagoPaymentOutcome(
                    FirstFilled(status.PaymentId, charge.PaymentId, charge.OrderId, charge.LocalReference),
                    FirstFilled(status.Status, "approved"));
            }

            if (IsMercadoPagoFinalFailure(lastStatus))
            {
                waitDialog.Close();
                MessageBox.Show(owner, $"Pagamento não aprovado: {lastStatus}", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        if (!paid && !cancelled)
        {
            waitDialog.Close();
            MessageBox.Show(owner, "Ainda não houve confirmação do Mercado Pago. Confira a maquininha antes de registrar de novo.", "Mercado Pago", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        return null;
    }

    private async Task<T> PostMercadoPagoOperationAsync<T>(string path, AgendaMercadoPagoClientPayload payload, TimeSpan timeout)
        where T : AgendaMercadoPagoResult, new()
    {
        var endpoint = BuildMercadoPagoApiUri(path);
        if (endpoint is null)
        {
            return new T { Ok = false, Message = "Configuração interna de pagamentos inválida." };
        }

        try
        {
            using var client = new HttpClient { Timeout = timeout };
            using var content = new StringContent(JsonSerializer.Serialize(payload, payload.GetType(), WebJsonOptions), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(body, WebJsonOptions) ?? new T { Ok = response.IsSuccessStatusCode };
            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message))
            {
                result.Message = $"Pagamentos online retornou {(int)response.StatusCode}: {CompactMercadoPagoResponse(body)}";
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new T { Ok = false, Message = $"Falha ao chamar Mercado Pago: {ex.Message}" };
        }
    }

    private AgendaMercadoPagoClientPayload FillMercadoPagoPayload(AgendaMercadoPagoClientPayload payload)
    {
        EnsureMercadoPagoInternalSettings();
        payload.LicenseKey = _data.Settings.MercadoPagoLicenseKey.Trim();
        payload.MachineHash = BuildMercadoPagoMachineHash();
        payload.MachineCode = Environment.MachineName;
        payload.AppVersion = "AgendaLivre.Windows";
        payload.Profile = new Dictionary<string, object?>
        {
            ["businessName"] = BusinessDisplayName(),
            ["businessDocument"] = OnlyDigits(_data.Settings.BusinessDocument),
            ["businessPhone"] = OnlyDigits(FirstFilled(_data.Settings.BusinessPhone, _data.Settings.AccountPhone)),
            ["segment"] = _data.Settings.BusinessSegment
        };
        return payload;
    }

    private Uri? BuildMercadoPagoApiUri(string path)
    {
        var baseUrl = NormalizeMercadoPagoPaymentsApiUrl(_data.Settings.MercadoPagoPaymentsApiUrl);
        if (!Uri.TryCreate(EnsureTrailingSlash(baseUrl), UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private static string NormalizeMercadoPagoPaymentsApiUrl(string value)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? DefaultMercadoPagoPaymentsApiUrl : value.Trim();
        return clean.TrimEnd('/');
    }

    private void EnsureMercadoPagoInternalSettings()
    {
        _data.Settings.MercadoPagoPaymentsApiUrl = DefaultMercadoPagoPaymentsApiUrl;
        if (!string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoLicenseKey))
        {
            return;
        }

        var seed = FirstFilled(
            OnlyDigits(_data.Settings.BusinessDocument),
            OnlyDigits(_data.Settings.BusinessPhone),
            OnlyDigits(_data.Settings.AccountPhone),
            _data.Settings.AccountEmail,
            BusinessDisplayName(),
            BuildMercadoPagoMachineHash());
        var clean = new string(seed.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(clean))
        {
            clean = BuildMercadoPagoMachineHash()[..12].ToUpperInvariant();
        }

        if (clean.Length > 32)
        {
            clean = clean[..32];
        }

        _data.Settings.MercadoPagoLicenseKey = $"AGENDALIVRE-{clean}";
    }

    private string BuildMercadoPagoMachineHash()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|{OnlyDigits(_data.Settings.BusinessDocument)}|AGENDALIVRE";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildMercadoPagoLocalReference() =>
        $"AGL-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();

    private static string ClipMercadoPagoDescription(string value)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? "Agenda Livre" : value.Trim();
        return clean.Length <= 180 ? clean : clean[..180];
    }

    private static bool IsMercadoPagoFinalFailure(string status)
    {
        var text = (status ?? "").Trim().ToLowerInvariant();
        return text.Contains("cancel", StringComparison.Ordinal)
               || text.Contains("reject", StringComparison.Ordinal)
               || text.Contains("refus", StringComparison.Ordinal)
               || text.Contains("fail", StringComparison.Ordinal)
               || text.Contains("expired", StringComparison.Ordinal)
               || text.Contains("erro", StringComparison.Ordinal);
    }

    private static string CompactMercadoPagoResponse(string value)
    {
        var clean = (value ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
        while (clean.Contains("  ", StringComparison.Ordinal))
        {
            clean = clean.Replace("  ", " ");
        }

        return clean.Length > 260 ? clean[..260] + "..." : clean;
    }

    private void ShowSelectedAppointment(Appointment appointment)
    {
        SelectedAppointmentCard.Visibility = Visibility.Visible;
        SelectedAppointmentCard.Background = StatusBackground(appointment.Status);
        SelectedAppointmentCard.BorderBrush = AccentFor(appointment.Status);
        SelectedDetailStatusBadge.Background = AccentFor(appointment.Status);

        SelectedDetailTitle.Text = $"{appointment.Start:HH:mm} - {appointment.End:HH:mm}  |  {appointment.CustomerName}";
        SelectedDetailSubtitle.Text =
            $"{appointment.Segment} | {appointment.ServiceName} | {appointment.ProfessionalName} | {appointment.ResourceName}";

        var detailParts = new[]
        {
            string.IsNullOrWhiteSpace(appointment.CustomerPhone) ? "" : $"Telefone: {appointment.CustomerPhone}",
            string.IsNullOrWhiteSpace(appointment.CustomerProfile) ? "" : appointment.CustomerProfile,
            string.IsNullOrWhiteSpace(appointment.Notes) ? "" : appointment.Notes
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        SelectedDetailNotes.Text = string.Join(" | ", detailParts);
        SelectedDetailStatus.Text = StatusLabel(appointment.Status);
        SelectedDetailStatus.Foreground = Solid("#07110E");
        SelectedDetailPrice.Text = appointment.Status == AppointmentStatus.Blocked
            ? "Bloqueio"
            : appointment.Price.ToString("C", Brazil);
    }

    private void ApplyServiceDefaults(ServiceItem service)
    {
        DurationCombo.SelectedItem = _durationOptions.Contains(service.DurationMinutes)
            ? service.DurationMinutes
            : null;

        if (!_durationOptions.Contains(service.DurationMinutes))
        {
            DurationCombo.Text = service.DurationMinutes.ToString(Brazil);
        }

        PriceTextBox.Text = service.Price.ToString("N2", Brazil);

        if (!string.IsNullOrWhiteSpace(service.DefaultResource))
        {
            SelectResource(service.DefaultResource);
        }
    }

    private void CreateServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var segment = CurrentEditorSegment();
        var form = ShowServiceEditorDialog(segment);
        if (form is null)
        {
            return;
        }

        var existing = _data.Services.FirstOrDefault(item =>
            item.Segment == form.Segment &&
            item.Name.Equals(form.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            ServiceCombo.SelectedItem = existing;
            RefreshSettingsSummary();
            ShowStatus($"Serviço já existia e foi selecionado: {existing.Name}.");
            return;
        }

        var service = new ServiceItem
        {
            Segment = form.Segment,
            Name = form.Name,
            Category = form.Category,
            Description = form.Description,
            DurationMinutes = form.DurationMinutes,
            PreparationMinutes = form.PreparationMinutes,
            BufferMinutes = form.BufferMinutes,
            Price = form.Price,
            CommissionPercent = form.CommissionPercent,
            IsActive = form.IsActive,
            DefaultResource = form.DefaultResource
        };

        _data.Services.Add(service);
        _store.Save(_data);
        AppointmentSegmentCombo.SelectedItem = form.Segment;
        UpdateAppointmentOptions(form.Segment);
        ServiceCombo.SelectedItem = _filteredServices.FirstOrDefault(item => item.Id == service.Id);
        ApplyServiceDefaults(service);
        RefreshSettingsSummary();
        RefreshAll();
        ShowStatus($"Serviço criado: {service.Name}.");
    }

    private void CreateProfessionalButton_Click(object sender, RoutedEventArgs e)
    {
        var segment = CurrentEditorSegment();
        var form = ShowProfessionalEditorDialog(segment);
        if (form is null)
        {
            return;
        }

        var existing = _data.Professionals.FirstOrDefault(item =>
            item.Name.Equals(form.Name, StringComparison.OrdinalIgnoreCase) &&
            item.Segments.Contains(form.Segment));
        if (existing is not null)
        {
            ProfessionalCombo.SelectedItem = existing;
            RefreshSettingsSummary();
            ShowStatus($"Profissional já existia e foi selecionado: {existing.Name}.");
            return;
        }

        var professional = new Professional
        {
            Name = form.Name,
            Role = form.Role,
            Segments = [form.Segment],
            Phone = form.Phone,
            Email = form.Email,
            Document = form.Document,
            CommissionPercent = form.CommissionPercent,
            Notes = form.Notes,
            IsActive = form.IsActive
        };

        _data.Professionals.Add(professional);
        _store.Save(_data);
        AppointmentSegmentCombo.SelectedItem = form.Segment;
        UpdateAppointmentOptions(form.Segment);
        ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault(item => item.Id == professional.Id);
        RefreshSettingsSummary();
        RefreshAll();
        ShowStatus($"Profissional criado: {professional.Name}.");
    }

    private void CreateResourceButton_Click(object sender, RoutedEventArgs e)
    {
        var form = ShowResourceEditorDialog(CurrentResourceText());
        if (form is null)
        {
            return;
        }

        var resourceName = form.Name;
        if (!_data.Settings.Resources.Any(item => item.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            _data.Settings.Resources.Add(resourceName);
            _store.Save(_data);
        }

        var segment = CurrentEditorSegment();
        UpdateAppointmentOptions(segment);
        SelectResource(resourceName);
        RefreshSettingsSummary();
        ShowStatus($"Recurso criado: {resourceName}.");
    }

    private void OpenBusinessHoursButton_Click(object sender, RoutedEventArgs e)
    {
        var shell = CreateEditorDialog("Horários de atendimento", "Configure o horário padrão usado para montar a agenda.", "Salvar horários");
        shell.Dialog.Width = 680;
        shell.Dialog.MaxHeight = 720;

        var hourOptions = Enumerable.Range(6, 19)
            .Select(hour => $"{hour:00}:00")
            .ToList();
        var dayOptions = new (int Value, string Label, string FullLabel)[]
        {
            (1, "Seg", "Segunda-feira"),
            (2, "Ter", "Terça-feira"),
            (3, "Qua", "Quarta-feira"),
            (4, "Qui", "Quinta-feira"),
            (5, "Sex", "Sexta-feira"),
            (6, "Sáb", "Sábado"),
            (0, "Dom", "Domingo")
        };
        var selectedDays = new HashSet<int>(_data.Settings.Workdays ?? [1, 2, 3, 4, 5, 6]);
        var dayButtons = new Dictionary<int, Button>();

        shell.Body.Children.Add(new TextBlock
        {
            Text = "Dias de atendimento",
            Foreground = InkBrush,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var daysGrid = new Grid { Margin = new Thickness(-3, 0, -3, 16) };
        foreach (var _ in dayOptions)
        {
            daysGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < dayOptions.Length; index++)
        {
            var day = dayOptions[index];
            var button = new Button
            {
                Style = (Style)FindResource("MercadoPagoOutlineButton"),
                Height = 40,
                MinWidth = 0,
                Padding = new Thickness(7, 0, 7, 0),
                Margin = new Thickness(3, 0, 3, 0),
                Tag = day.Value
            };
            Grid.SetColumn(button, index);
            daysGrid.Children.Add(button);
            dayButtons[day.Value] = button;
        }
        shell.Body.Children.Add(daysGrid);

        shell.Body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 0, 0, 18)
        });

        var timeColumns = AddDialogColumns(shell.Body);
        var startBox = AddDialogComboField(timeColumns.Left, "Abre", hourOptions, $"{_data.Settings.WorkdayStartHour:00}:00", editable: false);
        var endBox = AddDialogComboField(timeColumns.Right, "Fecha", hourOptions, $"{_data.Settings.WorkdayEndHour:00}:00", editable: false);

        shell.Body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 4, 0, 18)
        });

        var breakToggle = new ToggleButton
        {
            Style = (Style)FindResource("BusinessHoursSwitch"),
            IsChecked = _data.Settings.WorkdayBreakEnabled,
            Width = 48,
            Height = 28,
            MinWidth = 48,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Adicionar intervalo no expediente"
        };
        breakToggle.SetResourceReference(Control.ForegroundProperty, "Accent");
        AutomationProperties.SetName(breakToggle, "Adicionar intervalo");

        var breakHeader = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        breakHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        breakHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        breakHeader.Children.Add(new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Inlines =
            {
                new Run("Adicionar intervalo") { FontWeight = FontWeights.Bold, Foreground = InkBrush },
                new Run(" (opcional)") { Foreground = MutedBrush }
            }
        });
        Grid.SetColumn(breakToggle, 1);
        breakHeader.Children.Add(breakToggle);
        shell.Body.Children.Add(breakHeader);

        var breakColumns = AddDialogColumns(shell.Body);
        var breakStartBox = AddDialogComboField(breakColumns.Left, "De", hourOptions, $"{_data.Settings.WorkdayBreakStartHour:00}:00", editable: false);
        var breakEndBox = AddDialogComboField(breakColumns.Right, "Até", hourOptions, $"{_data.Settings.WorkdayBreakEndHour:00}:00", editable: false);

        shell.Body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 4, 0, 14)
        });

        var summaryText = new TextBlock
        {
            Foreground = InkBrush,
            FontSize = 12.5,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        var summary = new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(0, 2, 0, 2),
            Margin = new Thickness(0, 0, 0, 2),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(42) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    new Border
                    {
                        Width = 36,
                        Height = 36,
                        Background = WarmSoftBrush,
                        CornerRadius = new CornerRadius(18),
                        Child = new PackIcon
                        {
                            Kind = PackIconKind.ClockOutline,
                            Width = 20,
                            Height = 20,
                            Foreground = InkBrush,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    summaryText
                }
            }
        };
        Grid.SetColumn(summaryText, 1);
        shell.Body.Children.Add(summary);

        void UpdateDayChip(int value)
        {
            var button = dayButtons[value];
            var option = dayOptions.First(item => item.Value == value);
            var isSelected = selectedDays.Contains(value);
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(new TextBlock
            {
                Text = option.Label,
                Foreground = isSelected ? AccentDarkBrush : InkBrush,
                FontSize = 12.5,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center
            });
            if (isSelected)
            {
                content.Children.Add(new Border
                {
                    Width = 20,
                    Height = 20,
                    Background = AccentDarkBrush,
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(7, 0, 0, 0),
                    Child = new PackIcon
                    {
                        Kind = PackIconKind.Check,
                        Width = 13,
                        Height = 13,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
            }

            button.Content = content;
            button.Background = isSelected ? WarmSoftBrush : Brushes.White;
            button.BorderBrush = isSelected ? AccentBrush : LineBrush;
            AutomationProperties.SetName(button, $"{option.FullLabel}: {(isSelected ? "selecionado" : "não selecionado")}");
        }

        string SelectedDaysSummary()
        {
            if (selectedDays.SetEquals([1, 2, 3, 4, 5, 6]))
            {
                return "Seg a sáb";
            }

            if (selectedDays.SetEquals([1, 2, 3, 4, 5]))
            {
                return "Seg a sex";
            }

            if (selectedDays.SetEquals([0, 1, 2, 3, 4, 5, 6]))
            {
                return "Todos os dias";
            }

            var labels = dayOptions
                .Where(item => selectedDays.Contains(item.Value))
                .Select(item => item.Label)
                .ToList();
            return labels.Count == 0 ? "Nenhum dia" : string.Join(", ", labels);
        }

        void UpdateSummary()
        {
            var startText = startBox.SelectedItem?.ToString() ?? startBox.Text;
            var endText = endBox.SelectedItem?.ToString() ?? endBox.Text;
            var text = $"{SelectedDaysSummary()}  •  {startText} às {endText}";
            if (breakToggle.IsChecked == true)
            {
                var breakStartText = breakStartBox.SelectedItem?.ToString() ?? breakStartBox.Text;
                var breakEndText = breakEndBox.SelectedItem?.ToString() ?? breakEndBox.Text;
                text += $"  •  intervalo {breakStartText}–{breakEndText}";
            }

            summaryText.Text = text;
            AutomationProperties.SetName(summary, $"Resumo: {text}");
        }

        void UpdateBreakState()
        {
            var enabled = breakToggle.IsChecked == true;
            breakStartBox.IsEnabled = enabled;
            breakEndBox.IsEnabled = enabled;
            breakColumns.Left.Opacity = enabled ? 1 : 0.48;
            breakColumns.Right.Opacity = enabled ? 1 : 0.48;
            UpdateSummary();
        }

        foreach (var day in dayOptions)
        {
            var dayValue = day.Value;
            dayButtons[dayValue].Click += (_, _) =>
            {
                if (!selectedDays.Add(dayValue))
                {
                    selectedDays.Remove(dayValue);
                }

                UpdateDayChip(dayValue);
                UpdateSummary();
            };
            UpdateDayChip(day.Value);
        }
        startBox.SelectionChanged += (_, _) => UpdateSummary();
        endBox.SelectionChanged += (_, _) => UpdateSummary();
        breakStartBox.SelectionChanged += (_, _) => UpdateSummary();
        breakEndBox.SelectionChanged += (_, _) => UpdateSummary();
        breakToggle.Checked += (_, _) => UpdateBreakState();
        breakToggle.Unchecked += (_, _) => UpdateBreakState();
        UpdateBreakState();
        UpdateSummary();

        shell.PrimaryButton.Click += (_, _) =>
        {
            var start = ParseHourOption(startBox.SelectedItem?.ToString() ?? startBox.Text);
            var end = ParseHourOption(endBox.SelectedItem?.ToString() ?? endBox.Text);
            if (selectedDays.Count == 0)
            {
                SetDialogError(shell.ErrorText, "Selecione pelo menos um dia de atendimento.");
                return;
            }

            if (start < 0 || end < 0 || end <= start)
            {
                SetDialogError(shell.ErrorText, "O horário de fechamento precisa ser depois da abertura.");
                return;
            }

            var breakEnabled = breakToggle.IsChecked == true;
            var breakStart = ParseHourOption(breakStartBox.SelectedItem?.ToString() ?? breakStartBox.Text);
            var breakEnd = ParseHourOption(breakEndBox.SelectedItem?.ToString() ?? breakEndBox.Text);
            if (breakEnabled &&
                (breakStart <= start || breakEnd <= breakStart || breakEnd >= end))
            {
                SetDialogError(shell.ErrorText, "O intervalo precisa ficar dentro do expediente e terminar depois do início.");
                return;
            }

            _data.Settings.WorkdayStartHour = start;
            _data.Settings.WorkdayEndHour = end;
            _data.Settings.Workdays = dayOptions
                .Where(item => selectedDays.Contains(item.Value))
                .Select(item => item.Value)
                .ToList();
            _data.Settings.WorkdayBreakEnabled = breakEnabled;
            _data.Settings.WorkdayBreakStartHour = breakStart;
            _data.Settings.WorkdayBreakEndHour = breakEnd;
            _store.Save(_data);
            RefreshAll();
            shell.Dialog.DialogResult = true;
            ShowStatus($"Horários atualizados: {SelectedDaysSummary()}, {start:00}:00 às {end:00}:00.");
        };

        ShowAppDialog(shell.Dialog);
    }

    private static int ParseHourOption(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return -1;
        }

        var hourPart = value.Trim().Split(':')[0];
        return int.TryParse(hourPart, NumberStyles.Integer, Brazil, out var hour) ? hour : -1;
    }

    private bool IsConfiguredWorkday(DateTime date) =>
        (_data.Settings.Workdays ?? [1, 2, 3, 4, 5, 6]).Contains((int)date.DayOfWeek);

    private string ConfiguredWorkdaysSummary()
    {
        var days = new HashSet<int>(_data.Settings.Workdays ?? [1, 2, 3, 4, 5, 6]);
        if (days.SetEquals([1, 2, 3, 4, 5, 6]))
        {
            return "Seg a sáb";
        }

        if (days.SetEquals([1, 2, 3, 4, 5]))
        {
            return "Seg a sex";
        }

        if (days.SetEquals([0, 1, 2, 3, 4, 5, 6]))
        {
            return "Todos os dias";
        }

        var labels = new[] { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };
        return string.Join(", ", Enumerable.Range(0, 7).Where(days.Contains).Select(day => labels[day]));
    }

    private bool OverlapsConfiguredBreak(DateTime start, DateTime end)
    {
        if (!_data.Settings.WorkdayBreakEnabled)
        {
            return false;
        }

        var breakStart = start.Date.AddHours(_data.Settings.WorkdayBreakStartHour);
        var breakEnd = start.Date.AddHours(_data.Settings.WorkdayBreakEndHour);
        return start < breakEnd && end > breakStart;
    }

    private bool TryValidateConfiguredBusinessWindow(DateTime start, DateTime end, out string message)
    {
        if (!IsConfiguredWorkday(start))
        {
            message = "O estabelecimento não atende no dia selecionado.";
            return false;
        }

        var workdayStart = start.Date.AddHours(_data.Settings.WorkdayStartHour);
        var workdayEnd = start.Date.AddHours(_data.Settings.WorkdayEndHour);
        if (start < workdayStart || end > workdayEnd)
        {
            message = $"O atendimento precisa ficar dentro do expediente: {workdayStart:HH:mm} até {workdayEnd:HH:mm}.";
            return false;
        }

        if (OverlapsConfiguredBreak(start, end))
        {
            message = $"Esse horário coincide com o intervalo: {_data.Settings.WorkdayBreakStartHour:00}:00 às {_data.Settings.WorkdayBreakEndHour:00}:00.";
            return false;
        }

        message = "";
        return true;
    }

    private DateTime NextConfiguredWorkday(DateTime date)
    {
        var candidate = date.Date;
        for (var offset = 0; offset < 8; offset++)
        {
            var current = candidate.AddDays(offset);
            if (IsConfiguredWorkday(current))
            {
                return current;
            }
        }

        return candidate;
    }

    private string CurrentEditorSegment()
    {
        return AppointmentSegmentCombo.SelectedItem?.ToString()
               ?? GetAvailableSegments().FirstOrDefault()
               ?? "";
    }

    private string CurrentResourceText()
    {
        return ResourceCombo.SelectedItem?.ToString() ?? ResourceCombo.Text.Trim();
    }

    private void SelectResource(string? resourceName)
    {
        var normalized = (resourceName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            ResourceCombo.SelectedItem = null;
            return;
        }

        var option = _resourceOptions.FirstOrDefault(item =>
            item.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            _resourceOptions.Add(normalized);
            option = normalized;
        }

        ResourceCombo.SelectedItem = option;
    }

    private int ReadCurrentDurationOrDefault()
    {
        return TryReadDuration(out var duration) ? duration : 30;
    }

    private static string DefaultRoleForSegment(string segment) => segment switch
    {
        "Barbearia" => "Barbeiro",
        "Clínica médica" => "Profissional de saúde",
        "Petshop" => "Atendimento pet",
        "Oficina" => "Mecânico",
        "Mecânica" => "Mecânico",
        "Unha e beleza" => "Profissional de beleza",
        "Unha e beleza + salão" => "Profissional de beleza",
        "Cabelo e barbearia" => "Cabeleireiro",
        _ => "Profissional"
    };

    private string CurrentBusinessSegmentForSuggestions()
    {
        var segment = CurrentEditorSegment();
        if (!string.IsNullOrWhiteSpace(segment))
        {
            return segment;
        }

        return string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment)
            ? "Serviço"
            : _data.Settings.BusinessSegment;
    }

    private IEnumerable<string> ServiceCategoryOptions(string segment) => segment switch
    {
        "Barbearia" or "Cabelo e barbearia" => ["Corte", "Barba", "Combo", "Química", "Acabamento", "Sobrancelha"],
        "Clínica médica" => ["Consulta", "Retorno", "Exame", "Procedimento", "Avaliação", "Teleatendimento"],
        "Petshop" => ["Banho", "Tosa", "Veterinário", "Vacina", "Estética pet", "Taxi dog"],
        "Oficina" or "Mecânica" => ["Diagnóstico", "Revisão", "Troca", "Alinhamento", "Elétrica", "Freio"],
        "Unha e beleza" or "Unha e beleza + salão" => ["Mão", "Pé", "Alongamento", "Esmaltação", "Design", "Tratamento"],
        _ => ["Atendimento", "Avaliação", "Retorno", "Procedimento", "Manutenção", "Combo"]
    };

    private IEnumerable<string> ProductCategoryOptions()
    {
        var segment = CurrentBusinessSegmentForSuggestions();
        return segment switch
        {
            "Barbearia" or "Cabelo e barbearia" => ["Finalizadores", "Barba", "Cabelo", "Higiene", "Ferramentas", "Kits"],
            "Clínica médica" => ["Cuidados", "Dermocosmético", "Pós-procedimento", "Kit", "Material", "Outros"],
            "Petshop" => ["Ração", "Higiene", "Acessórios", "Medicamento", "Brinquedos", "Kits"],
            "Oficina" or "Mecânica" => ["Óleo", "Filtro", "Aditivo", "Peça", "Acessório", "Limpeza"],
            "Unha e beleza" or "Unha e beleza + salão" => ["Esmalte", "Tratamento", "Higiene", "Acessório", "Kits", "Revenda"],
            _ => ["Revenda", "Kit", "Reposição", "Pós-venda", "Material", "Outros"]
        };
    }

    private static IEnumerable<string> PaymentMethodOptions() =>
        ["Pix", "Dinheiro", "Cartão de débito", "Cartão de crédito", MercadoPagoDebitMethod, MercadoPagoCreditMethod, "Cortesia", "Fiado"];

    private static bool IsMercadoPagoPaymentMethod(string method) =>
        method.Equals(MercadoPagoCreditMethod, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(MercadoPagoDebitMethod, StringComparison.OrdinalIgnoreCase);

    private IEnumerable<string> CustomerTagOptions()
    {
        var savedTags = _data.Customers
            .SelectMany(customer => (customer.Tags ?? "")
                .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tag => !string.IsNullOrWhiteSpace(tag));

        return savedTags
            .Concat(["VIP", "Recorrente", "Primeira visita", "Retorno", "Pós-venda", "Preferencial", "Aniversariante", "Fiado", "Atrasado", "Não chamar"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag);
    }

    private static string MercadoPagoPointMethodCode(string method) =>
        method.Equals(MercadoPagoDebitMethod, StringComparison.OrdinalIgnoreCase) ? "DEBITO" : "CREDITO";

    private string MercadoPagoTerminalLabel() =>
        FirstFilled(_data.Settings.MercadoPagoDefaultTerminalLabel, _data.Settings.MercadoPagoDefaultTerminalId, "Point Mercado Pago");

    private bool IsMercadoPagoPointReady() =>
        _data.Settings.MercadoPagoEnabled &&
        _data.Settings.MercadoPagoConnected &&
        !string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoDefaultTerminalId);

    private IEnumerable<string> SupplierOptions() =>
        _data.Products
            .Select(item => item.Supplier)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .Concat(["Fornecedor local", "Marca própria", "Distribuidor"]);

    private CustomerEditorForm? ShowCustomerEditorDialog(string segment, string name = "", string phone = "", string profile = "", Customer? existing = null)
    {
        var initialSegment = string.IsNullOrWhiteSpace(existing?.Segment) ? segment : existing.Segment;
        var initialName = existing?.Name ?? name;
        var initialPhone = existing?.Phone ?? phone;
        var initialProfile = existing?.Profile ?? profile;
        var lockedSegment = FirstFilled(initialSegment, _data.Settings.BusinessSegment, "Salão de Beleza");
        var initialPreferredTime = ReadCustomerPreferredTime(initialProfile);
        var initialObservations = RemoveCustomerPreferredTime(initialProfile);
        var shell = CreateCustomerEditorDialog(
            existing is null ? "Criar cliente" : "Editar cliente",
            "Cadastre os dados essenciais para agendar e manter o histórico.",
            existing is null ? "Salvar cliente" : "Salvar alterações");

        shell.Body.Children.Add(new TextBlock
        {
            Text = "Dados do cliente",
            Foreground = InkBrush,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var identityRow = AddDialogColumns(shell.Body);
        var nameBox = AddDialogTextField(identityRow.Left, "Nome do cliente", initialName, "Digite o nome do cliente");
        var phoneBox = AddDialogTextField(identityRow.Right, "WhatsApp principal", FormatCustomerPhoneInput(initialPhone), "(11) 9 9999-9999");

        var classificationRow = AddDialogColumns(shell.Body);
        var tagsBox = AddDialogComboField(classificationRow.Left, "Tags", CustomerTagOptions(), existing?.Tags ?? "", editable: true);
        AddLockedDialogField(classificationRow.Right, "Segmento", lockedSegment);

        var getPreferredTime = AddCustomerPreferredTimePicker(shell.Body, initialPreferredTime);
        var profileBox = AddDialogTextField(
            shell.Body,
            "Preferências, alergias e observações",
            initialObservations,
            "Ex: cor preferida, alergias, produtos preferidos, observações importantes...",
            multiline: true);

        foreach (var (control, hint) in new (Control Control, string Hint)[]
                 {
                     (nameBox, "Digite o nome do cliente"),
                     (phoneBox, "(11) 9 9999-9999"),
                     (tagsBox, "Selecione ou crie tags"),
                     (profileBox, "Ex: cor preferida, alergias, produtos preferidos, observações importantes...")
                 })
        {
            HintAssist.SetHint(control, hint);
            HintAssist.SetIsFloating(control, false);
        }

        foreach (var control in new Control[] { nameBox, phoneBox, tagsBox })
        {
            control.Height = 42;
            control.FontSize = 13;
            control.Margin = new Thickness(0, 5, 0, 14);
        }

        profileBox.Height = 64;
        profileBox.FontSize = 13;
        profileBox.Padding = new Thickness(12, 9, 12, 9);
        profileBox.Margin = new Thickness(0, 5, 0, 0);

        phoneBox.TextChanged += DialogPhoneTextBox_TextChanged;
        phoneBox.LostFocus += DialogPhoneTextBox_LostFocus;

        CustomerEditorForm? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            var customerName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(customerName))
            {
                SetDialogError(shell.ErrorText, "Informe o nome do cliente.");
                nameBox.Focus();
                return;
            }

            if (!TryNormalizeCustomerPhone(phoneBox.Text, out var formattedPhone, out var phoneError))
            {
                SetDialogError(shell.ErrorText, phoneError);
                phoneBox.Focus();
                return;
            }

            result = new CustomerEditorForm(
                customerName,
                formattedPhone,
                existing?.Document ?? "",
                lockedSegment,
                BuildCustomerProfile(getPreferredTime(), profileBox.Text),
                DialogComboText(tagsBox, ""),
                existing?.Notes ?? "",
                existing?.AcceptsWhatsApp ?? !string.IsNullOrWhiteSpace(formattedPhone));
            shell.Dialog.DialogResult = true;
        };

        nameBox.SelectAll();
        nameBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private ServiceEditorForm? ShowServiceEditorDialog(string segment, ServiceItem? existing = null)
    {
        var initialSegment = string.IsNullOrWhiteSpace(existing?.Segment) ? segment : existing.Segment;
        var categoryOptions = ServiceCategoryOptions(initialSegment).ToList();
        var initialCategory = string.IsNullOrWhiteSpace(existing?.Category)
            ? categoryOptions.FirstOrDefault() ?? ""
            : existing.Category;
        var initialName = existing?.Name ?? "";
        var initialDuration = existing?.DurationMinutes ?? ReadCurrentDurationOrDefault();
        var initialPrice = (existing?.Price ?? 0) > 0 ? existing!.Price.ToString("N2", Brazil) : PriceTextBox.Text.Trim();
        var initialResource = existing?.DefaultResource ?? CurrentResourceText();
        var initialIsActive = existing?.IsActive ?? true;
        var shell = CreateEditorDialog(
            existing is null ? "Criar serviço" : "Editar serviço",
            "Defina como o serviço aparece na agenda e no atendimento.",
            existing is null ? "Salvar serviço" : "Salvar alterações");
        shell.Dialog.Width = 860;
        shell.Dialog.MaxHeight = 650;
        shell.Body.MinWidth = 0;
        shell.Body.Width = 780;
        shell.Body.Margin = new Thickness(18, 0, 18, 0);
        shell.PrimaryButton.Height = 38;
        shell.PrimaryButton.MinWidth = 140;

        var contentGrid = new Grid();
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35, GridUnitType.Star) });
        var formPanel = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };

        static void AddCompactServiceSection(StackPanel panel, PackIconKind iconKind, string title)
        {
            panel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 3, 0, 6),
                Children =
                {
                    new Border
                    {
                        Width = 28,
                        Height = 28,
                        Background = AccentSoftBrush,
                        CornerRadius = new CornerRadius(14),
                        Margin = new Thickness(0, 0, 9, 0),
                        Child = new PackIcon
                        {
                            Kind = iconKind,
                            Width = 14,
                            Height = 14,
                            Foreground = AccentBrush,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock
                    {
                        Text = title,
                        Foreground = InkBrush,
                        FontSize = 13.5,
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            });
        }

        AddCompactServiceSection(formPanel, PackIconKind.ClipboardText, "Catálogo");
        var catalogRow = AddDialogColumns(formPanel);
        var segmentCombo = AddDialogComboField(catalogRow.Left, "Tipo de atendimento", GetAvailableSegments(), initialSegment, editable: false);
        var categoryCombo = AddDialogComboField(catalogRow.Right, "Categoria", categoryOptions, initialCategory, editable: true);
        var nameBox = AddDialogTextField(formPanel, "Nome do serviço", initialName, "Ex: Corte masculino, consulta, revisão");
        var descriptionBox = AddDialogTextField(formPanel, "Descrição para a equipe", existing?.Description ?? "", "Ex: inclui lavagem, avaliação inicial ou checklist", multiline: true);

        AddCompactServiceSection(formPanel, PackIconKind.ClockOutline, "Tempo e agenda");
        var timeRow = AddDialogColumns(formPanel);
        var durationBox = AddDialogTextField(timeRow.Left, "Duração em minutos", initialDuration.ToString(Brazil), "Ex: 30");
        var preparationBox = AddDialogTextField(timeRow.Right, "Preparação antes (min)", (existing?.PreparationMinutes ?? 0).ToString(Brazil), "Ex: 5");
        var flowRow = AddDialogColumns(formPanel);
        var bufferBox = AddDialogTextField(flowRow.Left, "Intervalo após (min)", (existing?.BufferMinutes ?? 0).ToString(Brazil), "Ex: 10");
        var resourceCombo = AddDialogComboField(flowRow.Right, "Sala, cadeira ou recurso padrão", _data.Settings.Resources, initialResource, editable: true);

        AddCompactServiceSection(formPanel, PackIconKind.CashMultiple, "Preço e equipe");
        var moneyRow = AddDialogColumns(formPanel);
        var priceBox = AddDialogTextField(moneyRow.Left, "Valor de venda", initialPrice, "Ex: 45,00");
        var commissionBox = AddDialogTextField(moneyRow.Right, "Comissão (%)", (existing?.CommissionPercent ?? 0).ToString("N2", Brazil), "Ex: 40");
        var activeCheck = AddDialogCheckBox(formPanel, "Serviço ativo para novos agendamentos", initialIsActive);

        foreach (var control in new Control[]
        {
            segmentCombo,
            categoryCombo,
            nameBox,
            durationBox,
            preparationBox,
            bufferBox,
            resourceCombo,
            priceBox,
            commissionBox
        })
        {
            control.Height = 32;
            control.FontSize = 12;
            control.Margin = new Thickness(0, 1, 0, 4);
        }

        descriptionBox.Height = 42;
        descriptionBox.FontSize = 12;
        descriptionBox.Padding = new Thickness(9, 5, 9, 5);
        descriptionBox.Margin = new Thickness(0, 1, 0, 4);
        activeCheck.FontSize = 12;
        activeCheck.Margin = new Thickness(0);

        static TextBlock PreviewValue(string text, Brush? foreground = null) => new()
        {
            Text = text,
            Foreground = foreground ?? InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0)
        };

        static Border PreviewRow(PackIconKind iconKind, string label, TextBlock value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(new Border
            {
                Width = 26,
                Height = 26,
                Background = AccentSoftBrush,
                CornerRadius = new CornerRadius(13),
                Child = new PackIcon
                {
                    Kind = iconKind,
                    Width = 14,
                    Height = 14,
                    Foreground = AccentBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var text = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = label, Foreground = MutedBrush, FontSize = 10.5 },
                    value
                }
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);
            return new Border
            {
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6, 0, 6),
                Child = grid
            };
        }

        var previewName = new TextBlock
        {
            Text = FirstFilled(initialName, "Novo serviço"),
            Foreground = InkBrush,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 2)
        };
        var previewStatus = new TextBlock
        {
            Text = initialIsActive ? "Ativo" : "Inativo",
            Foreground = initialIsActive ? Solid("#15803D") : MutedBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };
        var previewCategory = PreviewValue($"{FirstFilled(initialCategory, "Sem categoria")} • {FirstFilled(initialSegment, "Agenda")}");
        var previewDuration = PreviewValue($"{initialDuration} min");
        var previewPrice = PreviewValue((decimal.TryParse(initialPrice, NumberStyles.Number, Brazil, out var parsedInitialPrice)
            ? parsedInitialPrice.ToString("C", Brazil)
            : "R$ 0,00") + $" • {existing?.CommissionPercent ?? 0:N0}% comissão", AccentDarkBrush);
        var previewResource = PreviewValue(FirstFilled(initialResource, "Não definido"));
        var agendaPreviewText = new TextBlock
        {
            Foreground = InkBrush,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 17
        };

        var previewPanel = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "Prévia do serviço", Foreground = MutedBrush, FontSize = 11, FontWeight = FontWeights.SemiBold },
                previewName,
                previewStatus,
                new Border { Height = 1, Background = LineBrush, Margin = new Thickness(0, 12, 0, 2) },
                PreviewRow(PackIconKind.TagOutline, "Categoria e atendimento", previewCategory),
                PreviewRow(PackIconKind.ClockOutline, "Duração", previewDuration),
                PreviewRow(PackIconKind.Cash, "Valor e comissão", previewPrice),
                PreviewRow(PackIconKind.SeatOutline, "Recurso padrão", previewResource),
                new Border
                {
                    Background = WarmSoftBrush,
                    BorderBrush = AccentSoftBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(AppActionRadiusValue),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 12, 0, 0),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "Como aparece na agenda", Foreground = InkBrush, FontSize = 11.5, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) },
                            agendaPreviewText
                        }
                    }
                }
            }
        };
        var previewSurface = new Border
        {
            Background = Solid("#FFFCFA"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Padding = new Thickness(18, 2, 2, 2),
            Child = previewPanel
        };
        Grid.SetColumn(previewSurface, 1);
        contentGrid.Children.Add(formPanel);
        contentGrid.Children.Add(previewSurface);
        shell.Body.Children.Add(contentGrid);

        void RefreshPreview()
        {
            var displayName = string.IsNullOrWhiteSpace(nameBox.Text) ? "Novo serviço" : nameBox.Text.Trim();
            var displayCategory = DialogComboText(categoryCombo, "Sem categoria");
            var displaySegment = DialogComboText(segmentCombo, initialSegment);
            var displayResource = string.IsNullOrWhiteSpace(resourceCombo.Text) ? "Não definido" : resourceCombo.Text.Trim();
            var duration = int.TryParse(durationBox.Text, NumberStyles.Integer, Brazil, out var parsedDuration)
                ? Math.Max(0, parsedDuration)
                : 0;
            var price = decimal.TryParse(priceBox.Text, NumberStyles.Number, Brazil, out var parsedPrice)
                ? Math.Max(0, parsedPrice)
                : 0;
            var commission = decimal.TryParse(commissionBox.Text, NumberStyles.Number, Brazil, out var parsedCommission)
                ? Math.Max(0, parsedCommission)
                : 0;

            previewName.Text = displayName;
            previewStatus.Text = activeCheck.IsChecked == true ? "Ativo" : "Inativo";
            previewStatus.Foreground = activeCheck.IsChecked == true ? Solid("#15803D") : MutedBrush;
            previewCategory.Text = $"{displayCategory} • {displaySegment}";
            previewDuration.Text = $"{duration} min";
            previewPrice.Text = $"{price.ToString("C", Brazil)} • {commission:N0}% comissão";
            previewResource.Text = displayResource;
            agendaPreviewText.Text = $"{displayName}  •  {duration} min  •  {price.ToString("C", Brazil)}";
        }

        nameBox.TextChanged += (_, _) => RefreshPreview();
        durationBox.TextChanged += (_, _) => RefreshPreview();
        priceBox.TextChanged += (_, _) => RefreshPreview();
        commissionBox.TextChanged += (_, _) => RefreshPreview();
        categoryCombo.SelectionChanged += (_, _) => RefreshPreview();
        categoryCombo.LostKeyboardFocus += (_, _) => RefreshPreview();
        segmentCombo.SelectionChanged += (_, _) => RefreshPreview();
        resourceCombo.SelectionChanged += (_, _) => RefreshPreview();
        resourceCombo.LostKeyboardFocus += (_, _) => RefreshPreview();
        activeCheck.Checked += (_, _) => RefreshPreview();
        activeCheck.Unchecked += (_, _) => RefreshPreview();
        RefreshPreview();

        ServiceEditorForm? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            var serviceName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                SetDialogError(shell.ErrorText, "Informe o nome do serviço.");
                nameBox.Focus();
                return;
            }

            if (!TryReadDialogInt(durationBox, 5, 480, out var duration))
            {
                SetDialogError(shell.ErrorText, "Informe uma duração entre 5 e 480 minutos.");
                durationBox.Focus();
                return;
            }

            if (!TryReadDialogInt(preparationBox, 0, 240, out var preparation))
            {
                SetDialogError(shell.ErrorText, "Informe uma preparação entre 0 e 240 minutos.");
                preparationBox.Focus();
                return;
            }

            if (!TryReadDialogInt(bufferBox, 0, 240, out var buffer))
            {
                SetDialogError(shell.ErrorText, "Informe um intervalo entre 0 e 240 minutos.");
                bufferBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(priceBox, allowZero: true, out var price))
            {
                SetDialogError(shell.ErrorText, "Informe um valor válido.");
                priceBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(commissionBox, allowZero: true, out var commission) || commission > 100)
            {
                SetDialogError(shell.ErrorText, "Informe uma comissão entre 0 e 100%.");
                commissionBox.Focus();
                return;
            }

            result = new ServiceEditorForm(
                DialogComboText(segmentCombo, initialSegment),
                serviceName,
                DialogComboText(categoryCombo, ""),
                descriptionBox.Text.Trim(),
                duration,
                preparation,
                buffer,
                price,
                commission,
                resourceCombo.Text.Trim(),
                activeCheck.IsChecked == true);
            shell.Dialog.DialogResult = true;
        };

        nameBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private ProfessionalEditorForm? ShowProfessionalEditorDialog(string segment, Professional? existing = null)
    {
        var initialSegment = existing?.Segments.FirstOrDefault() ?? segment;
        var initialName = existing?.Name ?? "";
        var initialRole = string.IsNullOrWhiteSpace(existing?.Role) ? DefaultRoleForSegment(initialSegment) : existing.Role;
        var initialPhone = string.IsNullOrWhiteSpace(existing?.Phone) ? "" : FormatCustomerPhoneInput(existing.Phone);
        var initialDocument = string.IsNullOrWhiteSpace(existing?.Document) ? "" : FormatDocumentInput(existing.Document);
        var initialIsActive = existing?.IsActive ?? true;
        var shell = CreateEditorDialog(
            existing is null ? "Criar profissional" : "Editar profissional",
            "Cadastre quem atende e em qual agenda ele aparece.",
            existing is null ? "Salvar profissional" : "Salvar alterações");
        shell.Dialog.Width = 880;
        shell.Dialog.MaxHeight = 650;
        shell.Body.MinWidth = 0;
        shell.Body.Width = 800;
        shell.Body.Margin = new Thickness(18, 0, 18, 0);
        shell.PrimaryButton.Height = 38;
        shell.PrimaryButton.MinWidth = 168;

        var contentGrid = new Grid();
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Star) });
        var formPanel = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };

        AddDialogInlineSection(formPanel, PackIconKind.AccountTie, "Identificação", "Dados usados na agenda e no cadastro da equipe.");
        var identityRow = AddDialogColumns(formPanel);
        var nameBox = AddDialogTextField(identityRow.Left, "Nome do profissional", initialName, "Ex: Lucas");
        var roleBox = AddDialogTextField(identityRow.Right, "Função", initialRole, "Ex: Barbeiro, mecânico, dentista");

        var contactRow = AddDialogColumns(formPanel);
        var phoneBox = AddDialogTextField(contactRow.Left, "Telefone / WhatsApp", initialPhone, "Ex: (27) 99999-0000");
        var emailBox = AddDialogTextField(contactRow.Right, "E-mail", existing?.Email ?? "", "Ex: profissional@email.com");

        AddDialogInlineSection(formPanel, PackIconKind.CashMultiple, "Agenda e financeiro", "Segmento atendido, documento e comissão padrão.");
        var agendaRow = AddDialogColumns(formPanel);
        var segmentBox = AddDialogTextField(agendaRow.Left, "Segmento atendido", initialSegment, "");
        segmentBox.IsReadOnly = true;
        segmentBox.IsTabStop = false;
        var documentBox = AddDialogTextField(agendaRow.Right, "CPF / documento", initialDocument, "Ex: 123.456.789-00");

        var financeRow = AddDialogColumns(formPanel);
        var commissionBox = AddDialogTextField(financeRow.Left, "Comissão padrão (%)", (existing?.CommissionPercent ?? 0).ToString("N2", Brazil), "Ex: 40");
        var activeCheck = AddDialogCheckBox(financeRow.Right, "Profissional ativo na agenda", initialIsActive);
        var notesBox = AddDialogTextField(formPanel, "Observações internas", existing?.Notes ?? "", "Ex: folgas, especialidades, restrições de horário", multiline: true);

        foreach (var control in new Control[] { nameBox, roleBox, phoneBox, emailBox, segmentBox, documentBox, commissionBox })
        {
            control.Height = 32;
            control.FontSize = 12;
            control.Margin = new Thickness(0, 1, 0, 5);
        }

        notesBox.Height = 44;
        notesBox.FontSize = 12;
        notesBox.Padding = new Thickness(9, 5, 9, 5);
        notesBox.Margin = new Thickness(0, 1, 0, 5);
        activeCheck.FontSize = 12;
        activeCheck.Margin = new Thickness(0, 23, 0, 5);

        static TextBlock ProfileValue(string text, Brush? foreground = null) => new()
        {
            Text = text,
            Foreground = foreground ?? InkBrush,
            FontSize = 11.8,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0)
        };

        static Border ProfileRow(PackIconKind iconKind, string label, TextBlock value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(new Border
            {
                Width = 26,
                Height = 26,
                Background = AccentSoftBrush,
                CornerRadius = new CornerRadius(13),
                Child = new PackIcon
                {
                    Kind = iconKind,
                    Width = 14,
                    Height = 14,
                    Foreground = AccentBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var stack = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = label, Foreground = MutedBrush, FontSize = 10.5 },
                    value
                }
            };
            Grid.SetColumn(stack, 1);
            grid.Children.Add(stack);
            return new Border
            {
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6, 0, 6),
                Child = grid
            };
        }

        var initialsText = new TextBlock
        {
            Text = "NP",
            Foreground = AccentBrush,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var profileName = new TextBlock
        {
            Text = FirstFilled(initialName, "Novo profissional"),
            Foreground = InkBrush,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        var profileRole = new TextBlock { Text = initialRole, Foreground = MutedBrush, FontSize = 11.5, Margin = new Thickness(0, 2, 0, 0) };
        var profileContact = ProfileValue(FirstFilled(initialPhone, "WhatsApp não informado"));
        var profileSegment = ProfileValue(FirstFilled(initialSegment, "Agenda"));
        var profileCommission = ProfileValue($"{existing?.CommissionPercent ?? 0:N0}%");
        var profileStatus = ProfileValue(initialIsActive ? "Ativo" : "Inativo", initialIsActive ? Solid("#15803D") : MutedBrush);
        var agendaProfileText = new TextBlock { Foreground = InkBrush, FontSize = 11.5, TextWrapping = TextWrapping.Wrap, LineHeight = 17 };

        var profileHeader = new Grid { Margin = new Thickness(0, 8, 0, 12) };
        profileHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        profileHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        profileHeader.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(22),
            Child = initialsText
        });
        var profileIdentity = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { profileName, profileRole } };
        Grid.SetColumn(profileIdentity, 1);
        profileHeader.Children.Add(profileIdentity);

        var previewPanel = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "Perfil do profissional", Foreground = MutedBrush, FontSize = 11, FontWeight = FontWeights.SemiBold },
                profileHeader,
                ProfileRow(PackIconKind.Whatsapp, "Contato", profileContact),
                ProfileRow(PackIconKind.CalendarAccountOutline, "Agenda", profileSegment),
                ProfileRow(PackIconKind.PercentOutline, "Comissão", profileCommission),
                ProfileRow(PackIconKind.CheckCircleOutline, "Status", profileStatus),
                new Border
                {
                    Background = WarmSoftBrush,
                    BorderBrush = AccentSoftBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(AppActionRadiusValue),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 12, 0, 0),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "Como aparece na agenda", Foreground = InkBrush, FontSize = 11.5, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) },
                            agendaProfileText
                        }
                    }
                }
            }
        };
        var previewSurface = new Border
        {
            Background = Solid("#FFFCFA"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Padding = new Thickness(18, 2, 2, 2),
            Child = previewPanel
        };
        Grid.SetColumn(previewSurface, 1);
        contentGrid.Children.Add(formPanel);
        contentGrid.Children.Add(previewSurface);
        shell.Body.Children.Add(contentGrid);

        void RefreshProfessionalPreview()
        {
            var displayName = string.IsNullOrWhiteSpace(nameBox.Text) ? "Novo profissional" : nameBox.Text.Trim();
            var displayRole = string.IsNullOrWhiteSpace(roleBox.Text) ? "Profissional" : roleBox.Text.Trim();
            var displaySegment = FirstFilled(segmentBox.Text, initialSegment);
            var contact = string.IsNullOrWhiteSpace(phoneBox.Text) ? "WhatsApp não informado" : phoneBox.Text.Trim();
            var commission = decimal.TryParse(commissionBox.Text, NumberStyles.Number, Brazil, out var parsedCommission)
                ? Math.Max(0, parsedCommission)
                : 0;
            var initials = string.Join("", displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => char.ToUpperInvariant(part[0])));

            initialsText.Text = string.IsNullOrWhiteSpace(initials) ? "NP" : initials;
            profileName.Text = displayName;
            profileRole.Text = displayRole;
            profileContact.Text = contact;
            profileSegment.Text = displaySegment;
            profileCommission.Text = $"{commission:N0}%";
            profileStatus.Text = activeCheck.IsChecked == true ? "Ativo" : "Inativo";
            profileStatus.Foreground = activeCheck.IsChecked == true ? Solid("#15803D") : MutedBrush;
            agendaProfileText.Text = $"{displayName}  •  {displayRole}  •  {displaySegment}";
        }

        nameBox.TextChanged += (_, _) => RefreshProfessionalPreview();
        roleBox.TextChanged += (_, _) => RefreshProfessionalPreview();
        phoneBox.TextChanged += (_, _) => RefreshProfessionalPreview();
        commissionBox.TextChanged += (_, _) => RefreshProfessionalPreview();
        activeCheck.Checked += (_, _) => RefreshProfessionalPreview();
        activeCheck.Unchecked += (_, _) => RefreshProfessionalPreview();
        RefreshProfessionalPreview();

        phoneBox.TextChanged += DialogPhoneTextBox_TextChanged;
        phoneBox.LostFocus += DialogPhoneTextBox_LostFocus;
        documentBox.TextChanged += DialogDocumentTextBox_TextChanged;
        documentBox.LostFocus += DialogDocumentTextBox_LostFocus;
        commissionBox.LostFocus += DialogPercentTextBox_LostFocus;

        ProfessionalEditorForm? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            var professionalName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(professionalName))
            {
                SetDialogError(shell.ErrorText, "Informe o nome do profissional.");
                nameBox.Focus();
                return;
            }

            if (!TryNormalizeCustomerPhone(phoneBox.Text, out var phone, out var phoneError))
            {
                SetDialogError(shell.ErrorText, phoneError);
                phoneBox.Focus();
                return;
            }

            if (!TryFormatBusinessDocument(documentBox.Text, out var document, out var documentError))
            {
                SetDialogError(shell.ErrorText, documentError);
                documentBox.Focus();
                return;
            }

            if (!TryReadDialogPercent(commissionBox, out var commission))
            {
                SetDialogError(shell.ErrorText, "Informe uma comissão entre 0 e 100%.");
                commissionBox.Focus();
                return;
            }

            result = new ProfessionalEditorForm(
                professionalName,
                string.IsNullOrWhiteSpace(roleBox.Text) ? DefaultRoleForSegment(initialSegment) : roleBox.Text.Trim(),
                phone,
                emailBox.Text.Trim(),
                document,
                commission,
                FirstFilled(segmentBox.Text, initialSegment),
                notesBox.Text.Trim(),
                activeCheck.IsChecked == true);
            shell.Dialog.DialogResult = true;
        };

        nameBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private ResourceEditorForm? ShowResourceEditorDialog(string initialName)
    {
        var shell = CreateEditorDialog("Criar sala ou recurso", "Cadastre cadeira, sala, box, mesa, equipamento ou baia.", "Salvar recurso");
        var typeCombo = AddDialogComboField(shell.Body, "Tipo", new[] { "Cadeira", "Sala", "Box", "Mesa", "Equipamento", "Baia", "Outro" }, "Cadeira", editable: false);
        var nameBox = AddDialogTextField(shell.Body, "Nome visível na agenda", initialName, "Ex: Cadeira 1");
        var noteBox = AddDialogTextField(shell.Body, "Observação interna", "", "Ex: perto da entrada, sala com maca", multiline: true);

        ResourceEditorForm? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            var type = DialogComboText(typeCombo, "Recurso");
            var resourceName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                var next = _data.Settings.Resources.Count(item => item.StartsWith(type, StringComparison.OrdinalIgnoreCase)) + 1;
                resourceName = $"{type} {next}";
            }

            result = new ResourceEditorForm(resourceName, type, noteBox.Text.Trim());
            shell.Dialog.DialogResult = true;
        };

        nameBox.SelectAll();
        nameBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private ProductEditorForm? ShowProductEditorDialog(ProductItem? existing = null)
    {
        var categoryOptions = ProductCategoryOptions().ToList();
        var initialCategory = string.IsNullOrWhiteSpace(existing?.Category)
            ? categoryOptions.FirstOrDefault() ?? ""
            : existing.Category;
        var shell = CreateEditorDialog(
            existing is null ? "Criar produto" : "Editar produto",
            "Produto vendido no balcão, no atendimento ou no pós-venda.",
            existing is null ? "Salvar produto" : "Salvar alterações");
        shell.Dialog.Width = 840;
        AddDialogSection(shell.Body, "Produto", "Identificação para estoque, balcão e pós-venda.");
        var productRow = AddDialogColumns(shell.Body);
        var nameBox = AddDialogTextField(productRow.Left, "Nome do produto", existing?.Name ?? "", "Ex: Pomada modeladora");
        var categoryBox = AddDialogComboField(productRow.Right, "Categoria", categoryOptions, initialCategory, editable: true);

        var codeRow = AddDialogColumns(shell.Body);
        var skuBox = AddDialogTextField(codeRow.Left, "SKU / código interno", existing?.Sku ?? "", "Ex: POM-001");
        var supplierBox = AddDialogComboField(codeRow.Right, "Fornecedor / marca", SupplierOptions(), existing?.Supplier ?? "", editable: true);

        AddDialogSection(shell.Body, "Preço e margem", "Controle de custo para vender sem perder margem.");
        var priceRow = AddDialogColumns(shell.Body);
        var costBox = AddDialogTextField(priceRow.Left, "Preço de custo", (existing?.CostPrice ?? 0).ToString("N2", Brazil), "Ex: 18,00");
        var priceBox = AddDialogTextField(priceRow.Right, "Preço de venda", (existing?.Price ?? 0).ToString("N2", Brazil), "Ex: 39,90");

        AddDialogSection(shell.Body, "Estoque", "Quantidade inicial e ponto de reposição.");
        var stockRow = AddDialogColumns(shell.Body);
        var stockBox = AddDialogTextField(stockRow.Left, "Estoque atual", (existing?.StockQuantity ?? 0).ToString(Brazil), "Ex: 10");
        var minimumStockBox = AddDialogTextField(stockRow.Right, "Estoque mínimo", (existing?.MinimumStock ?? 0).ToString(Brazil), "Ex: 3");
        var notesBox = AddDialogTextField(shell.Body, "Observações de compra ou venda", existing?.Notes ?? "", "Ex: validade, variação, melhor oferta, comissão", multiline: true);
        var activeCheck = AddDialogCheckBox(shell.Body, "Produto ativo para venda", existing?.IsActive ?? true);

        ProductEditorForm? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            var productName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                SetDialogError(shell.ErrorText, "Informe o nome do produto.");
                nameBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(costBox, allowZero: true, out var cost))
            {
                SetDialogError(shell.ErrorText, "Informe um preço de custo válido.");
                costBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(priceBox, allowZero: true, out var price))
            {
                SetDialogError(shell.ErrorText, "Informe um preço válido.");
                priceBox.Focus();
                return;
            }

            if (!TryReadDialogInt(stockBox, 0, 100000, out var stock))
            {
                SetDialogError(shell.ErrorText, "Informe uma quantidade de estoque válida.");
                stockBox.Focus();
                return;
            }

            if (!TryReadDialogInt(minimumStockBox, 0, 100000, out var minimumStock))
            {
                SetDialogError(shell.ErrorText, "Informe um estoque mínimo válido.");
                minimumStockBox.Focus();
                return;
            }

            result = new ProductEditorForm(
                productName,
                DialogComboText(categoryBox, ""),
                skuBox.Text.Trim(),
                supplierBox.Text.Trim(),
                cost,
                price,
                stock,
                minimumStock,
                notesBox.Text.Trim(),
                activeCheck.IsChecked == true);
            shell.Dialog.DialogResult = true;
        };

        nameBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private PaymentEditorForm? ShowPaymentEditorDialog()
    {
        var shell = CreateFinanceEditorDialog(
            "Registrar pagamento",
            "Lance um recebimento avulso no financeiro.",
            "Registrar pagamento",
            PackIconKind.WalletOutline,
            useBodyCard: false);
        shell.Dialog.Width = 900;
        shell.Dialog.MaxHeight = 600;

        var contentGrid = new Grid { Background = Brushes.White };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Star) });

        var formPanel = new StackPanel { Margin = new Thickness(4, 0, 22, 0) };
        AddFinanceDialogSection(formPanel, PackIconKind.WalletOutline, "Recebimento", "Identifique de onde veio o valor.");
        var mainRow = AddFinanceDialogColumns(formPanel);
        var descriptionBox = AddFinanceDialogTextField(mainRow.Left, "Descrição *", "Pagamento avulso", "Ex: Sinal de agendamento");
        var customerBox = AddFinanceDialogComboField(mainRow.Right, "Cliente", _data.Customers.Select(item => item.Name).Distinct().OrderBy(item => item), "", editable: true);

        var paymentRow = AddFinanceDialogColumns(formPanel);
        var categoryBox = AddFinanceDialogComboField(paymentRow.Left, "Categoria *", new[] { "Agendamento", "Produto", "Sinal", "Mensalidade", "Ajuste", "Outro" }, "Agendamento", editable: true);
        var methodBox = AddFinanceDialogComboField(paymentRow.Right, "Forma de pagamento *", PaymentMethodOptions(), "Pix", editable: true);

        var valueBox = AddFinanceDialogTextField(formPanel, "Valor recebido *", "0,00", "Ex: 80,00");
        valueBox.Height = 40;
        valueBox.FontSize = 15;
        valueBox.FontWeight = FontWeights.SemiBold;
        var notesBox = AddFinanceDialogTextField(formPanel, "Observações (opcional)", "", "Ex: pago antecipado, comprovante enviado, ajuste manual", multiline: true);
        notesBox.Height = 64;

        var summaryTitle = new TextBlock
        {
            Text = "Resumo do recebimento",
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold
        };
        var summaryValue = new TextBlock
        {
            Text = "R$ 0,00",
            Foreground = AccentDarkBrush,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 8, 0, 12)
        };

        static Border SummaryDivider() => new()
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 0, 0, 10)
        };

        static TextBlock SummaryValueText(string text) => new()
        {
            Text = text,
            Foreground = InkBrush,
            FontSize = 11.8,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 220
        };

        static Grid SummaryRow(PackIconKind iconKind, string label, TextBlock valueText)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 9) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var iconBadge = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                Child = new PackIcon
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = AccentBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(9, 0, 0, 0),
                Children =
                {
                    new TextBlock { Text = label, Foreground = MutedBrush, FontSize = 10.8 },
                    valueText
                }
            };
            row.Children.Add(iconBadge);
            Grid.SetColumn(textStack, 1);
            row.Children.Add(textStack);
            return row;
        }

        var descriptionSummary = SummaryValueText("Pagamento avulso");
        var categorySummary = SummaryValueText("Agendamento");
        var methodSummary = SummaryValueText("Pix");
        var customerSummary = SummaryValueText("Não informado");

        var summaryPanel = new StackPanel
        {
            Children =
            {
                summaryTitle,
                summaryValue,
                SummaryDivider(),
                SummaryRow(PackIconKind.ReceiptTextOutline, "Descrição", descriptionSummary),
                SummaryRow(PackIconKind.TagOutline, "Categoria", categorySummary),
                SummaryRow(PackIconKind.CreditCardOutline, "Forma de pagamento", methodSummary),
                SummaryRow(PackIconKind.AccountOutline, "Cliente", customerSummary)
            }
        };
        AddFinanceDialogInfoCard(
            summaryPanel,
            IsMercadoPagoPointReady() ? "Maquininha pronta" : "Maquininha desativada",
            MercadoPagoPaymentHintText(),
            IsMercadoPagoPointReady() ? Solid("#DCFCE7") : Solid("#FFF7ED"),
            IsMercadoPagoPointReady() ? Solid("#16A34A") : Solid("#D97706"));

        if (shell.PrimaryButton.Parent is Panel footerActions)
        {
            footerActions.Children.Remove(shell.CancelButton);
            footerActions.Children.Remove(shell.PrimaryButton);
        }
        shell.CancelButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        shell.CancelButton.Height = 36;
        shell.CancelButton.Margin = new Thickness(0, 8, 0, 6);
        shell.PrimaryButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        shell.PrimaryButton.Height = 38;
        shell.PrimaryButton.Margin = new Thickness(0);
        summaryPanel.Children.Add(shell.CancelButton);
        summaryPanel.Children.Add(shell.PrimaryButton);
        var summarySurface = new Border
        {
            Background = Solid("#FFFCFA"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Padding = new Thickness(22, 0, 4, 0),
            Child = summaryPanel
        };

        contentGrid.Children.Add(formPanel);
        Grid.SetColumn(summarySurface, 1);
        contentGrid.Children.Add(summarySurface);
        shell.Body.Children.Add(contentGrid);

        void RefreshSummary()
        {
            descriptionSummary.Text = string.IsNullOrWhiteSpace(descriptionBox.Text) ? "Não informada" : descriptionBox.Text.Trim();
            customerSummary.Text = string.IsNullOrWhiteSpace(customerBox.Text) ? "Não informado" : customerBox.Text.Trim();
            categorySummary.Text = DialogComboText(categoryBox, "Agendamento");
            methodSummary.Text = DialogComboText(methodBox, "Pix");
            var displayValue = double.TryParse(valueBox.Text, NumberStyles.Number, Brazil, out var parsedValue)
                ? $"R$ {Math.Max(0, parsedValue).ToString("N2", Brazil)}"
                : "R$ 0,00";
            summaryValue.Text = displayValue;
        }

        descriptionBox.TextChanged += (_, _) => RefreshSummary();
        customerBox.SelectionChanged += (_, _) => RefreshSummary();
        customerBox.LostKeyboardFocus += (_, _) => RefreshSummary();
        valueBox.TextChanged += (_, _) => RefreshSummary();
        categoryBox.SelectionChanged += (_, _) => RefreshSummary();
        categoryBox.LostKeyboardFocus += (_, _) => RefreshSummary();
        methodBox.SelectionChanged += (_, _) => RefreshSummary();
        methodBox.LostKeyboardFocus += (_, _) => RefreshSummary();
        RefreshSummary();

        PaymentEditorForm? result = null;
        shell.PrimaryButton.Click += async (_, _) =>
        {
            var description = descriptionBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                SetDialogError(shell.ErrorText, "Informe a descrição do pagamento.");
                descriptionBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(valueBox, allowZero: false, out var value))
            {
                SetDialogError(shell.ErrorText, "Informe um valor maior que zero.");
                valueBox.Focus();
                return;
            }

            var selectedMethod = DialogComboText(methodBox, "Pix");
            var provider = "";
            var reference = "";
            var status = "";
            if (IsMercadoPagoPaymentMethod(selectedMethod))
            {
                if (!IsMercadoPagoPointReady())
                {
                    SetDialogError(shell.ErrorText, "Ative o Mercado Pago em Configurações, conecte a conta e escolha uma maquininha Point antes de registrar este pagamento.");
                    methodBox.Focus();
                    return;
                }

                shell.PrimaryButton.IsEnabled = false;
                var outcome = await ProcessMercadoPagoPointPaymentAsync(
                    selectedMethod,
                    value,
                    customerBox.Text.Trim(),
                    description,
                    shell.Dialog);
                shell.PrimaryButton.IsEnabled = true;
                if (outcome is null)
                {
                    return;
                }

                provider = "Mercado Pago";
                reference = outcome.Reference;
                status = outcome.Status;
            }

            result = new PaymentEditorForm(
                description,
                customerBox.Text.Trim(),
                DialogComboText(categoryBox, "Agendamento"),
                selectedMethod,
                notesBox.Text.Trim(),
                value,
                provider,
                reference,
                status);
            shell.Dialog.DialogResult = true;
        };

        descriptionBox.SelectAll();
        descriptionBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private ExpenseEditorForm? ShowExpenseEditorDialog()
    {
        var shell = CreateFinanceEditorDialog(
            "Nova despesa",
            "Registre custos do dia, fornecedores ou operação.",
            "Salvar despesa",
            PackIconKind.ReceiptText,
            useBodyCard: false);
        shell.Dialog.Width = 1040;
        shell.Dialog.MaxHeight = 680;

        var contentGrid = new Grid { Background = Brushes.White };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

        var formPanel = new StackPanel
        {
            Margin = new Thickness(6, 0, 30, 0)
        };

        var descriptionBox = AddFinanceDialogTextField(formPanel, "Descrição *", "", "Ex: Produtos para coloração");
        var supplierBox = AddFinanceDialogTextField(formPanel, "Fornecedor / responsável", "", "Digite o nome do fornecedor ou responsável");

        var detailRow = AddFinanceDialogColumns(formPanel);
        var categoryBox = AddFinanceDialogComboField(
            detailRow.Left,
            "Categoria *",
            new[] { "Operacional", "Fornecedor", "Equipe", "Marketing", "Aluguel", "Impostos", "Estoque" },
            "Operacional",
            editable: true);
        var methodBox = AddFinanceDialogComboField(detailRow.Right, "Forma de pagamento *", PaymentMethodOptions(), "Pix", editable: true);

        var valueBox = AddFinanceDialogTextField(formPanel, "Valor *", "0,00", "0,00");
        valueBox.FontSize = 16;
        valueBox.FontWeight = FontWeights.SemiBold;
        var notesBox = AddFinanceDialogTextField(
            formPanel,
            "Observações (opcional)",
            "",
            "Adicione detalhes, referência ou observações sobre esta despesa",
            multiline: true);

        var summaryTitle = new TextBlock
        {
            Text = "Resumo da despesa",
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        };
        var summaryValue = new TextBlock
        {
            Text = "R$ 0,00",
            Foreground = AccentDarkBrush,
            FontSize = 34,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 12, 0, 18)
        };

        static Border SummaryDivider() => new()
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 0, 0, 14)
        };

        static Grid SummaryRow(PackIconKind iconKind, string label, TextBlock valueText)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var iconBadge = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                Child = new PackIcon
                {
                    Kind = iconKind,
                    Width = 18,
                    Height = 18,
                    Foreground = AccentBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Children =
                {
                    new TextBlock { Text = label, Foreground = MutedBrush, FontSize = 11.5 },
                    valueText
                }
            };
            row.Children.Add(iconBadge);
            Grid.SetColumn(textStack, 1);
            row.Children.Add(textStack);
            return row;
        }

        static TextBlock SummaryValueText(string text) => new()
        {
            Text = text,
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 220
        };

        var descriptionSummary = SummaryValueText("Não informada");
        var categorySummary = SummaryValueText("Operacional");
        var methodSummary = SummaryValueText("Pix");
        var supplierSummary = SummaryValueText("Não informado");
        var footerValue = SummaryValueText("R$ 0,00");

        var amountStrip = new Border
        {
            Background = Solid("#FFF5EF"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 2, 0, 0),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    new PackIcon { Kind = PackIconKind.TagOutline, Width = 18, Height = 18, Foreground = AccentBrush },
                    new TextBlock { Text = "Valor", Foreground = MutedBrush, FontSize = 12, Margin = new Thickness(9, 0, 0, 0) },
                    footerValue
                }
            }
        };
        Grid.SetColumn(((Grid)amountStrip.Child).Children[1], 1);
        Grid.SetColumn(footerValue, 2);

        var summaryPanel = new StackPanel
        {
            Children =
            {
                summaryTitle,
                summaryValue,
                SummaryDivider(),
                SummaryRow(PackIconKind.ReceiptTextOutline, "Descrição", descriptionSummary),
                SummaryRow(PackIconKind.TagOutline, "Categoria", categorySummary),
                SummaryRow(PackIconKind.CreditCardOutline, "Forma de pagamento", methodSummary),
                SummaryRow(PackIconKind.AccountOutline, "Fornecedor / responsável", supplierSummary),
                amountStrip
            }
        };
        var summarySurface = new Border
        {
            Background = Solid("#FFFCFA"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Padding = new Thickness(28, 0, 6, 0),
            Child = summaryPanel
        };

        contentGrid.Children.Add(formPanel);
        Grid.SetColumn(summarySurface, 1);
        contentGrid.Children.Add(summarySurface);
        shell.Body.Children.Add(contentGrid);

        void RefreshSummary()
        {
            descriptionSummary.Text = string.IsNullOrWhiteSpace(descriptionBox.Text) ? "Não informada" : descriptionBox.Text.Trim();
            supplierSummary.Text = string.IsNullOrWhiteSpace(supplierBox.Text) ? "Não informado" : supplierBox.Text.Trim();
            categorySummary.Text = DialogComboText(categoryBox, "Operacional");
            methodSummary.Text = DialogComboText(methodBox, "Pix");
            var displayValue = double.TryParse(valueBox.Text, NumberStyles.Number, Brazil, out var parsedValue)
                ? $"R$ {Math.Max(0, parsedValue).ToString("N2", Brazil)}"
                : "R$ 0,00";
            summaryValue.Text = displayValue;
            footerValue.Text = displayValue;
        }

        descriptionBox.TextChanged += (_, _) => RefreshSummary();
        supplierBox.TextChanged += (_, _) => RefreshSummary();
        valueBox.TextChanged += (_, _) => RefreshSummary();
        categoryBox.SelectionChanged += (_, _) => RefreshSummary();
        categoryBox.LostKeyboardFocus += (_, _) => RefreshSummary();
        methodBox.SelectionChanged += (_, _) => RefreshSummary();
        methodBox.LostKeyboardFocus += (_, _) => RefreshSummary();
        RefreshSummary();

        ExpenseEditorForm? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            var description = descriptionBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                SetDialogError(shell.ErrorText, "Informe a descrição da despesa.");
                descriptionBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(valueBox, allowZero: false, out var value))
            {
                SetDialogError(shell.ErrorText, "Informe um valor maior que zero.");
                valueBox.Focus();
                return;
            }

            result = new ExpenseEditorForm(
                description,
                categoryBox.Text.Trim(),
                supplierBox.Text.Trim(),
                DialogComboText(methodBox, "Pix"),
                notesBox.Text.Trim(),
                value);
            shell.Dialog.DialogResult = true;
        };

        descriptionBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private ProductSaleEditorForm? ShowProductSaleEditorDialog(ProductSale? existing = null)
    {
        var selectedProduct = existing is null
            ? _data.Products.OrderBy(item => item.Name).First()
            : _data.Products.FirstOrDefault(item => item.Id == existing.ProductId)
              ?? _data.Products.FirstOrDefault(item => item.Name.Equals(existing.ProductName, StringComparison.OrdinalIgnoreCase))
              ?? _data.Products.OrderBy(item => item.Name).First();
        var shell = CreateFinanceEditorDialog(
            existing is null ? "Registrar venda" : "Editar venda",
            "Baixe estoque e registre o valor vendido.",
            existing is null ? "Registrar venda" : "Salvar alterações",
            PackIconKind.ShoppingOutline);
        AddFinanceDialogSection(shell.Body, PackIconKind.ShoppingOutline, "Produto vendido", "Venda com baixa automática de estoque.");
        var productCombo = AddFinanceDialogComboField(shell.Body, "Produto *", _data.Products.OrderBy(item => item.Name), selectedProduct, editable: false);
        productCombo.DisplayMemberPath = nameof(ProductItem.Name);
        var saleRow = AddFinanceDialogColumns(shell.Body);
        var quantityBox = AddFinanceDialogTextField(saleRow.Left, "Quantidade *", (existing?.Quantity ?? 1).ToString(Brazil), "Ex: 2");
        var discountBox = AddFinanceDialogTextField(saleRow.Right, "Desconto", (existing?.Discount ?? 0).ToString("N2", Brazil), "Ex: 5,00");
        var detailRow = AddFinanceDialogColumns(shell.Body);
        var customerBox = AddFinanceDialogComboField(detailRow.Left, "Cliente", _data.Customers.Select(item => item.Name).Distinct().OrderBy(item => item), existing?.CustomerName ?? "", editable: true);
        var methodBox = AddFinanceDialogComboField(detailRow.Right, "Forma de pagamento *", PaymentMethodOptions(), string.IsNullOrWhiteSpace(existing?.PaymentMethod) ? "Pix" : existing.PaymentMethod, editable: true);
        AddFinanceDialogInfoCard(
            shell.Body,
            "Mercado Pago na maquininha",
            MercadoPagoPaymentHintText(),
            IsMercadoPagoPointReady() ? Solid("#DCFCE7") : Solid("#FFF7ED"),
            IsMercadoPagoPointReady() ? Solid("#16A34A") : Solid("#D97706"));
        var notesBox = AddFinanceDialogTextField(shell.Body, "Observações", existing?.Notes ?? "", "Ex: retirada no balcão, venda junto ao atendimento", multiline: true);

        ProductSaleEditorForm? result = null;
        shell.PrimaryButton.Click += async (_, _) =>
        {
            if (productCombo.SelectedItem is not ProductItem product)
            {
                SetDialogError(shell.ErrorText, "Selecione o produto vendido.");
                productCombo.Focus();
                return;
            }

            if (!TryReadDialogInt(quantityBox, 1, 100000, out var quantity))
            {
                SetDialogError(shell.ErrorText, "Informe uma quantidade válida.");
                quantityBox.Focus();
                return;
            }

            if (!TryReadDialogMoney(discountBox, allowZero: true, out var discount))
            {
                SetDialogError(shell.ErrorText, "Informe um desconto válido.");
                discountBox.Focus();
                return;
            }

            if (discount > product.Price * quantity)
            {
                SetDialogError(shell.ErrorText, "O desconto não pode ser maior que o total da venda.");
                discountBox.Focus();
                return;
            }

            var selectedMethod = DialogComboText(methodBox, "Pix");
            var provider = "";
            var reference = "";
            var status = "";
            if (IsMercadoPagoPaymentMethod(selectedMethod))
            {
                var amount = Math.Max(0, (product.Price * quantity) - discount);
                if (amount <= 0)
                {
                    SetDialogError(shell.ErrorText, "O valor para cobrar no Mercado Pago precisa ser maior que zero.");
                    discountBox.Focus();
                    return;
                }

                if (!IsMercadoPagoPointReady())
                {
                    SetDialogError(shell.ErrorText, "Ative o Mercado Pago em Configurações, conecte a conta e escolha uma maquininha Point antes de registrar esta venda.");
                    methodBox.Focus();
                    return;
                }

                shell.PrimaryButton.IsEnabled = false;
                var outcome = await ProcessMercadoPagoPointPaymentAsync(
                    selectedMethod,
                    amount,
                    customerBox.Text.Trim(),
                    $"{quantity}x {product.Name}",
                    shell.Dialog);
                shell.PrimaryButton.IsEnabled = true;
                if (outcome is null)
                {
                    return;
                }

                provider = "Mercado Pago";
                reference = outcome.Reference;
                status = outcome.Status;
            }

            result = new ProductSaleEditorForm(
                product,
                quantity,
                customerBox.Text.Trim(),
                selectedMethod,
                discount,
                notesBox.Text.Trim(),
                provider,
                reference,
                status);
            shell.Dialog.DialogResult = true;
        };

        productCombo.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private (Window Dialog, StackPanel Body, TextBlock ErrorText, Button PrimaryButton, Button CancelButton) CreateFinanceEditorDialog(
        string title,
        string subtitle,
        string primaryText,
        PackIconKind headerIcon,
        bool useBodyCard = true)
    {
        var body = new StackPanel();
        var errorText = new TextBlock
        {
            Foreground = Solid("#DC2626"),
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };
        AutomationProperties.SetName(errorText, "Erro no formulário financeiro");
        AutomationProperties.SetLiveSetting(errorText, AutomationLiveSetting.Assertive);
        var primaryButton = new Button
        {
            Content = primaryText,
            Style = (Style)FindResource("CommandButton"),
            Height = 40,
            MinWidth = 150,
            IsDefault = true,
            Background = AccentDarkBrush,
            BorderBrush = AccentDarkBrush,
            Foreground = Brushes.White
        };
        TextElement.SetForeground(primaryButton, Brushes.White);
        AutomationProperties.SetName(primaryButton, primaryText);

        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Width = 920,
            MaxHeight = 660,
            SizeToContent = SizeToContent.Height
        };
        ConfigureRoundedDialogWindow(dialog);
        CopyDialogThemeResources(dialog);
        dialog.Resources["MaterialDesign.Brush.Primary"] = AccentBrush;
        dialog.Resources["MaterialDesign.Brush.Primary.Foreground"] = Brushes.White;
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;

        var icon = new PackIcon
        {
            Kind = headerIcon,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.SetResourceReference(Control.ForegroundProperty, "Accent");
        var iconBadge = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Child = icon
        };
        iconBadge.SetResourceReference(Border.BackgroundProperty, "AccentSoft");

        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    Foreground = InkBrush,
                    FontSize = 21,
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = 27
                },
                new TextBlock
                {
                    Text = subtitle,
                    Foreground = MutedBrush,
                    FontSize = 12,
                    LineHeight = 17,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 18, 0)
                }
            }
        };

        var closeButton = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Width = 48,
            MinWidth = 48,
            Height = 48,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            IsCancel = true,
            ToolTip = "Fechar",
            Content = new PackIcon
            {
                Kind = PackIconKind.Close,
                Width = 20,
                Height = 20,
                Foreground = InkBrush
            }
        };
        AutomationProperties.SetName(closeButton, $"Fechar {title.ToLowerInvariant()}");
        closeButton.Click += (_, _) => dialog.Close();

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.Children.Add(iconBadge);
        Grid.SetColumn(titleStack, 1);
        headerGrid.Children.Add(titleStack);
        Grid.SetColumn(closeButton, 2);
        headerGrid.Children.Add(closeButton);

        var header = new Border
        {
            Height = 84,
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(AppModalRadiusValue, AppModalRadiusValue, 0, 0),
            Padding = new Thickness(22, 0, 14, 0),
            Child = headerGrid
        };
        EnableDialogDrag(header, dialog);

        var bodyStack = new StackPanel
        {
            Children = { body }
        };
        var bodyCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = useBodyCard ? LineBrush : Brushes.Transparent,
            BorderThickness = useBodyCard ? new Thickness(1) : new Thickness(0),
            CornerRadius = useBodyCard ? new CornerRadius(AppSurfaceRadiusValue) : new CornerRadius(0),
            Padding = useBodyCard ? new Thickness(15) : new Thickness(24, 18, 24, 16),
            Margin = useBodyCard ? new Thickness(20) : new Thickness(0),
            Child = bodyStack
        };
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = bodyCard
        };
        ApplyDialogScrollTheme(scroll);

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("GhostButton"),
            Height = 40,
            MinWidth = 110,
            IsCancel = true,
            Margin = new Thickness(0, 0, 10, 0)
        };
        var footerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, primaryButton }
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footerGrid.Children.Add(errorText);
        Grid.SetColumn(footerActions, 1);
        footerGrid.Children.Add(footerActions);
        var footer = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, AppModalRadiusValue, AppModalRadiusValue),
            Padding = new Thickness(22, 16, 22, 18),
            Child = footerGrid
        };

        var content = new DockPanel { LastChildFill = true, Background = PanelBrush };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(header);
        content.Children.Add(footer);
        content.Children.Add(scroll);

        dialog.Content = WrapRoundedDialogContent(content, Brushes.White);
        return (dialog, body, errorText, primaryButton, cancelButton);
    }

    private bool? ShowAppDialog(Window dialog)
    {
        Dispatcher.VerifyAccess();
        dialog.PreviewKeyDown -= AppointmentEditorForm_PreviewKeyDown;
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;
        KeyboardNavigation.SetTabNavigation(dialog, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(dialog, KeyboardNavigationMode.Cycle);
        _appDialogBackdropDepth++;

        try
        {
            WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
            AppDialogBackdrop.Visibility = Visibility.Visible;
            RefreshWhatsAppLauncherVisibility();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            return dialog.ShowDialog();
        }
        finally
        {
            _appDialogBackdropDepth = Math.Max(0, _appDialogBackdropDepth - 1);
            if (_appDialogBackdropDepth == 0)
            {
                AppDialogBackdrop.Visibility = Visibility.Collapsed;
                RefreshWhatsAppLauncherVisibility();
                Activate();
            }
        }
    }

    private static void AddFinanceDialogSection(StackPanel body, PackIconKind iconKind, string title, string subtitle)
    {
        var icon = new PackIcon
        {
            Kind = iconKind,
            Width = 19,
            Height = 19,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.SetResourceReference(Control.ForegroundProperty, "Accent");
        var badge = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(AppBadgeRadiusValue),
            Margin = new Thickness(0, 0, 10, 0),
            Child = icon
        };
        badge.SetResourceReference(Border.BackgroundProperty, "AccentSoft");

        var text = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    Foreground = InkBrush,
                    FontSize = 17,
                    FontWeight = FontWeights.SemiBold
                },
                new TextBlock
                {
                    Text = subtitle,
                    Foreground = MutedBrush,
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                }
            }
        };

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(badge);
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        body.Children.Add(grid);
    }

    private TextBox AddFinanceDialogTextField(
        StackPanel body,
        string label,
        string value,
        string helpText,
        bool multiline = false)
    {
        var input = new TextBox
        {
            Text = value,
            Style = (Style)FindResource(multiline ? "AppointmentMessageBox" : "AppointmentInputBox"),
            Height = multiline ? 78 : 48,
            MinWidth = 240,
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            CaretBrush = AccentBrush,
            SelectionBrush = AccentSoftBrush,
            SelectionTextBrush = InkBrush,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            AcceptsReturn = multiline,
            Margin = new Thickness(0, 0, 0, 12)
        };
        HintAssist.SetHint(input, label);
        HintAssist.SetIsFloating(input, true);
        AutomationProperties.SetName(input, label.Replace(" *", "", StringComparison.Ordinal));
        AutomationProperties.SetHelpText(input, helpText);
        body.Children.Add(input);
        return input;
    }

    private ComboBox AddFinanceDialogComboField<T>(
        StackPanel body,
        string label,
        IEnumerable<T> items,
        object? selected,
        bool editable)
    {
        var combo = new ComboBox
        {
            Style = (Style)FindResource("AppointmentComboBox"),
            ItemsSource = items.ToList(),
            SelectedItem = selected,
            IsEditable = editable,
            IsTextSearchEnabled = true,
            Height = 48,
            MinWidth = 240,
            Padding = new Thickness(14, 0, 14, 0),
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            Margin = new Thickness(0, 0, 0, 12)
        };
        HintAssist.SetHint(combo, label);
        HintAssist.SetIsFloating(combo, true);
        AutomationProperties.SetName(combo, label.Replace(" *", "", StringComparison.Ordinal));
        if (selected is string selectedText && editable)
        {
            combo.Text = selectedText;
        }

        ApplyEditableComboTheme(combo);

        body.Children.Add(combo);
        return combo;
    }

    private static (StackPanel Left, StackPanel Right) AddFinanceDialogColumns(StackPanel body)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        var right = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        body.Children.Add(grid);
        return (left, right);
    }

    private static void AddFinanceDialogInfoCard(
        StackPanel body,
        string title,
        string text,
        Brush background,
        Brush accent)
    {
        body.Children.Add(new Border
        {
            MinHeight = 78,
            Background = background,
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        Foreground = InkBrush,
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = text,
                        Foreground = MutedBrush,
                        FontSize = 11.5,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 3, 0, 0)
                    }
                }
            }
        });
    }

    private (Window Dialog, StackPanel Body, TextBlock ErrorText, Button PrimaryButton) CreateCustomerEditorDialog(
        string title,
        string subtitle,
        string primaryText)
    {
        var body = new StackPanel { Margin = new Thickness(26, 18, 26, 10) };
        var errorText = new TextBlock
        {
            Foreground = Solid("#DC2626"),
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 18, 0)
        };
        AutomationProperties.SetName(errorText, "Erro no formulário");
        AutomationProperties.SetLiveSetting(errorText, AutomationLiveSetting.Assertive);

        var primaryButton = new Button
        {
            Content = primaryText,
            Style = (Style)FindResource("CommandButton"),
            Height = 44,
            MinWidth = 154,
            IsDefault = true,
            Background = AccentDarkBrush,
            BorderBrush = AccentDarkBrush,
            Foreground = Brushes.White
        };
        TextElement.SetForeground(primaryButton, Brushes.White);
        AutomationProperties.SetName(primaryButton, primaryText);

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("GhostButton"),
            Height = 44,
            MinWidth = 130,
            IsCancel = true,
            Margin = new Thickness(0, 0, 12, 0)
        };
        AutomationProperties.SetName(cancelButton, "Cancelar cadastro do cliente");

        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Width = 640,
            MaxHeight = 620,
            SizeToContent = SizeToContent.Height
        };
        ConfigureRoundedDialogWindow(dialog);
        CopyDialogThemeResources(dialog);
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;

        var closeButton = CreateDialogCloseButton(dialog);
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconTile = new Border
        {
            Width = 48,
            Height = 48,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 14, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.AccountOutline,
                Width = 23,
                Height = 23,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        headerGrid.Children.Add(iconTile);

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 18, 0)
        });
        Grid.SetColumn(titleStack, 1);
        headerGrid.Children.Add(titleStack);
        Grid.SetColumn(closeButton, 2);
        headerGrid.Children.Add(closeButton);

        var header = new Border
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(AppModalRadiusValue, AppModalRadiusValue, 0, 0),
            Padding = new Thickness(26, 18, 22, 18),
            Child = headerGrid
        };
        EnableDialogDrag(header, dialog);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, primaryButton }
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footerGrid.Children.Add(errorText);
        Grid.SetColumn(actions, 1);
        footerGrid.Children.Add(actions);

        var footer = new Border
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, AppModalRadiusValue, AppModalRadiusValue),
            Padding = new Thickness(26, 12, 26, 14),
            Child = footerGrid
        };

        var content = new DockPanel { LastChildFill = true, Background = PanelBrush };
        KeyboardNavigation.SetTabNavigation(content, KeyboardNavigationMode.Cycle);
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(header);
        content.Children.Add(footer);
        content.Children.Add(body);

        dialog.Content = WrapRoundedDialogContent(content, PanelBrush);
        return (dialog, body, errorText, primaryButton);
    }

    private (Window Dialog, StackPanel Body, TextBlock ErrorText, Button PrimaryButton) CreateEditorDialog(string title, string subtitle, string primaryText)
    {
        var body = new StackPanel { Margin = new Thickness(24, 0, 24, 0) };
        var errorText = new TextBlock
        {
            Foreground = Solid("#DC2626"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(22, 12, 22, 0)
        };
        AutomationProperties.SetName(errorText, "Erro no formulário");
        AutomationProperties.SetLiveSetting(errorText, AutomationLiveSetting.Assertive);
        var primaryButton = new Button
        {
            Content = primaryText,
            Style = (Style)FindResource("CommandButton"),
            Height = 40,
            MinWidth = 150,
            IsDefault = true,
            Background = AccentDarkBrush,
            BorderBrush = AccentDarkBrush,
            Foreground = Brushes.White
        };
        TextElement.SetForeground(primaryButton, Brushes.White);
        AutomationProperties.SetName(primaryButton, primaryText);

        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Width = 780,
            MaxHeight = 840,
            SizeToContent = SizeToContent.Height
        };
        ConfigureRoundedDialogWindow(dialog);
        CopyDialogThemeResources(dialog);
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;

        var closeButton = CreateDialogCloseButton(dialog);
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.Children.Add(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = title, Foreground = InkBrush, FontSize = 22, FontWeight = FontWeights.Bold },
                new TextBlock { Text = subtitle, Foreground = MutedBrush, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 18, 0) }
            }
        });
        Grid.SetColumn(closeButton, 1);
        headerGrid.Children.Add(closeButton);

        var content = new DockPanel { LastChildFill = true, Background = Brushes.White };
        var header = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(AppModalRadiusValue, AppModalRadiusValue, 0, 0),
            Padding = new Thickness(22, 18, 22, 18),
            Child = headerGrid
        };
        EnableDialogDrag(header, dialog);
        DockPanel.SetDock(header, Dock.Top);
        content.Children.Add(header);

        var footer = new Border
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, AppModalRadiusValue, AppModalRadiusValue),
            Padding = new Thickness(22, 16, 22, 18),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children =
                {
                    new Button
                    {
                        Content = "Cancelar",
                        Style = (Style)FindResource("GhostButton"),
                        Height = 40,
                        MinWidth = 110,
                        IsCancel = true,
                        Margin = new Thickness(0, 0, 10, 0)
                    },
                    primaryButton
                }
            }
        };
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new StackPanel
            {
                Margin = new Thickness(0, 18, 0, 26),
                Children = { body, errorText }
            }
        };
        ApplyDialogScrollTheme(scroll);
        content.Children.Add(scroll);
        dialog.Content = WrapRoundedDialogContent(content, PanelBrush);
        return (dialog, body, errorText, primaryButton);
    }

    private TextBox AddDialogTextField(StackPanel body, string label, string value, string hint, bool multiline = false)
    {
        body.Children.Add(DialogLabel(label));
        var input = new TextBox
        {
            Text = value,
            Tag = hint,
            Style = (Style)FindResource(multiline ? "AppointmentMessageBox" : "AppointmentInputBox"),
            Height = multiline ? 76 : 40,
            MinWidth = 240,
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            CaretBrush = AccentBrush,
            SelectionBrush = AccentSoftBrush,
            SelectionTextBrush = InkBrush,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            AcceptsReturn = multiline,
            Margin = new Thickness(0, 5, 0, 12)
        };
        AutomationProperties.SetName(input, label);
        AutomationProperties.SetHelpText(input, hint);
        body.Children.Add(input);
        return input;
    }

    private ComboBox AddDialogComboField<T>(StackPanel body, string label, IEnumerable<T> items, object? selected, bool editable)
    {
        body.Children.Add(DialogLabel(label));
        var combo = new ComboBox
        {
            Style = (Style)FindResource("AppointmentComboBox"),
            ItemsSource = items.ToList(),
            SelectedItem = selected,
            IsEditable = editable,
            Height = 42,
            MinWidth = 240,
            Padding = new Thickness(12, 0, 12, 0),
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            Margin = new Thickness(0, 4, 0, 12)
        };
        if (selected is string selectedText && editable)
        {
            combo.Text = selectedText;
        }

        AutomationProperties.SetName(combo, label);
        AutomationProperties.SetHelpText(
            combo,
            editable ? $"Digite ou selecione {label.ToLowerInvariant()}." : $"Selecione {label.ToLowerInvariant()}.");
        ApplyEditableComboTheme(combo);

        body.Children.Add(combo);
        return combo;
    }

    private static void ApplyEditableComboTheme(ComboBox combo)
    {
        if (!combo.IsEditable)
        {
            return;
        }

        void ApplyToEditor()
        {
            combo.ApplyTemplate();
            foreach (var editor in FindVisualChildren<TextBox>(combo))
            {
                editor.CaretBrush = AccentBrush;
                editor.SelectionBrush = AccentSoftBrush;
                editor.SelectionTextBrush = InkBrush;
            }
        }

        combo.Loaded += (_, _) => ApplyToEditor();
        combo.GotKeyboardFocus += (_, _) => ApplyToEditor();
    }

    private void ApplyDialogScrollTheme(ScrollViewer scroll)
    {
        if (TryFindResource("AppSlimScrollBar") is Style scrollBarStyle)
        {
            scroll.Resources[typeof(ScrollBar)] = scrollBarStyle;
        }
    }

    private (StackPanel Left, StackPanel Right) AddDialogColumns(StackPanel body)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        var right = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        body.Children.Add(grid);
        return (left, right);
    }

    private static Border AddLockedDialogField(StackPanel body, string label, string value)
    {
        body.Children.Add(DialogLabel(label));

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new PackIcon
                {
                    Kind = PackIconKind.LockOutline,
                    Width = 17,
                    Height = 17,
                    Foreground = MutedBrush,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = value,
                    Foreground = InkBrush,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            }
        };

        var field = new Border
        {
            Height = 42,
            Background = GraySoftBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppActionRadiusValue),
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0, 5, 0, 14),
            Child = content
        };
        AutomationProperties.SetName(field, $"{label}: {value}");
        AutomationProperties.SetHelpText(field, "Este campo segue o segmento configurado para o estabelecimento e não pode ser alterado aqui.");
        body.Children.Add(field);
        return field;
    }

    private Func<string> AddCustomerPreferredTimePicker(StackPanel body, string initialValue)
    {
        body.Children.Add(DialogLabel("Preferência de horário"));

        var selectedValue = NormalizeCustomerPreferredTime(initialValue);
        var options = new (string Label, PackIconKind Icon)[]
        {
            ("Manhã", PackIconKind.WhiteBalanceSunny),
            ("Tarde", PackIconKind.WeatherSunset),
            ("Noite", PackIconKind.WeatherNight)
        };
        var buttons = new List<(Button Button, PackIcon Icon, TextBlock Label, string Value)>();
        var grid = new Grid { Margin = new Thickness(0, 5, 0, 14) };

        for (var index = 0; index < options.Length; index++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (index < options.Length - 1)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            }

            var option = options[index];
            var icon = new PackIcon
            {
                Kind = option.Icon,
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var label = new TextBlock
            {
                Text = option.Label,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            var button = new Button
            {
                Style = (Style)FindResource("GhostButton"),
                Height = 40,
                MinWidth = 0,
                Padding = new Thickness(12, 0, 12, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { icon, label }
                }
            };
            AutomationProperties.SetName(button, $"Preferência de horário: {option.Label}");
            AutomationProperties.SetHelpText(button, "Clique novamente para limpar a preferência selecionada.");
            button.Click += (_, _) =>
            {
                selectedValue = selectedValue.Equals(option.Label, StringComparison.OrdinalIgnoreCase)
                    ? ""
                    : option.Label;
                RefreshCustomerPreferredTimeButtons();
            };

            Grid.SetColumn(button, index * 2);
            grid.Children.Add(button);
            buttons.Add((button, icon, label, option.Label));
        }

        void RefreshCustomerPreferredTimeButtons()
        {
            foreach (var item in buttons)
            {
                var isSelected = selectedValue.Equals(item.Value, StringComparison.OrdinalIgnoreCase);
                var foreground = isSelected ? AccentTextBrush : InkBrush;
                item.Button.Background = isSelected ? AccentSoftBrush : PanelBrush;
                item.Button.BorderBrush = isSelected ? AccentBrush : LineBrush;
                item.Button.BorderThickness = new Thickness(isSelected ? 2 : 1);
                item.Button.Foreground = foreground;
                item.Icon.Foreground = foreground;
                item.Label.Foreground = foreground;
            }
        }

        RefreshCustomerPreferredTimeButtons();
        body.Children.Add(grid);
        return () => selectedValue;
    }

    private static string NormalizeCustomerPreferredTime(string value)
    {
        var normalized = (value ?? "").Trim().TrimEnd('.');
        if (normalized.StartsWith("manh", StringComparison.OrdinalIgnoreCase))
        {
            return "Manhã";
        }

        if (normalized.StartsWith("tarde", StringComparison.OrdinalIgnoreCase))
        {
            return "Tarde";
        }

        return normalized.StartsWith("noite", StringComparison.OrdinalIgnoreCase) ? "Noite" : "";
    }

    private static string ReadCustomerPreferredTime(string profile)
    {
        const string prefix = "Preferência de horário:";
        var line = (profile ?? "")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(item => item.Trim())
            .FirstOrDefault(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return line is null ? "" : NormalizeCustomerPreferredTime(line[prefix.Length..]);
    }

    private static string RemoveCustomerPreferredTime(string profile)
    {
        const string prefix = "Preferência de horário:";
        return string.Join(
                Environment.NewLine,
                (profile ?? "")
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Split('\n')
                    .Where(item => !item.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Trim();
    }

    private static string BuildCustomerProfile(string preferredTime, string observations)
    {
        var parts = new List<string>();
        var normalizedTime = NormalizeCustomerPreferredTime(preferredTime);
        if (!string.IsNullOrWhiteSpace(normalizedTime))
        {
            parts.Add($"Preferência de horário: {normalizedTime}.");
        }

        var normalizedObservations = (observations ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedObservations))
        {
            parts.Add(normalizedObservations);
        }

        return string.Join(Environment.NewLine, parts);
    }

    private Action<string, string, string, bool> AddCustomerEditorSummary(StackPanel body, string name, string phone, string segment, bool acceptsWhatsApp)
    {
        var card = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = AccentSoftBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var initialsText = new TextBlock
        {
            Foreground = AccentBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 12, 0),
            Child = initialsText
        });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameText = new TextBlock
        {
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        textStack.Children.Add(nameText);
        var detailText = new TextBlock
        {
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 3, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        textStack.Children.Add(detailText);
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        var statusText = new TextBlock
        {
            FontSize = 10.5,
            FontWeight = FontWeights.Bold
        };
        var status = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(9, 4, 9, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = statusText
        };
        Grid.SetColumn(status, 2);
        grid.Children.Add(status);

        card.Child = grid;
        body.Children.Add(card);

        void UpdateSummary(string currentName, string currentPhone, string currentSegment, bool currentAcceptsWhatsApp)
        {
            var displayName = FirstFilled(currentName, "Novo cliente");
            var formattedPhone = FormatCustomerPhoneInput(currentPhone);
            var hasPhone = !string.IsNullOrWhiteSpace(formattedPhone);
            var displayPhone = FirstFilled(formattedPhone, "WhatsApp não informado");
            var displaySegment = FirstFilled(currentSegment, _data.Settings.BusinessSegment, "Cliente");
            var whatsAppReady = currentAcceptsWhatsApp && hasPhone;

            initialsText.Text = InitialsFor(displayName);
            nameText.Text = displayName;
            detailText.Text = $"{displayPhone}  |  {displaySegment}";
            status.Background = whatsAppReady ? Solid("#DCFCE7") : GraySoftBrush;
            statusText.Text = whatsAppReady ? "WhatsApp ativo" : currentAcceptsWhatsApp ? "Informe o WhatsApp" : "Sem retorno";
            statusText.Foreground = whatsAppReady ? Solid("#16A34A") : MutedBrush;
        }

        UpdateSummary(name, phone, segment, acceptsWhatsApp);
        return UpdateSummary;
    }

    private void AddProfessionalEditorSummary(StackPanel body, string name, string role, string phone, string segment, bool isActive)
    {
        var displayName = FirstFilled(name, "Novo profissional");
        var displayRole = FirstFilled(role, DefaultRoleForSegment(segment), "Equipe");
        var displayPhone = FirstFilled(FormatCustomerPhoneInput(phone), "WhatsApp não informado");
        var displaySegment = FirstFilled(segment, _data.Settings.BusinessSegment, "Agenda");

        var card = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = AccentSoftBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 12, 0),
            Child = new TextBlock
            {
                Text = InitialsFor(displayName),
                Foreground = AccentBrush,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = displayName,
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = $"{displayRole}  |  {displaySegment}  |  {displayPhone}",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 3, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        var status = new Border
        {
            Background = isActive ? Solid("#DCFCE7") : GraySoftBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(9, 4, 9, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = isActive ? "Ativo" : "Inativo",
                Foreground = isActive ? Solid("#16A34A") : MutedBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(status, 2);
        grid.Children.Add(status);

        card.Child = grid;
        body.Children.Add(card);
    }

    private void AddServiceEditorSummary(
        StackPanel body,
        string name,
        string category,
        string segment,
        int durationMinutes,
        string price,
        bool isActive)
    {
        var displayName = FirstFilled(name, "Novo serviço");
        var displayCategory = FirstFilled(category, "Atendimento");
        var displaySegment = FirstFilled(segment, _data.Settings.BusinessSegment, "Agenda");
        var displayPrice = FirstFilled(price, "0,00");

        var card = new Border
        {
            Background = WarmSoftBrush,
            BorderBrush = AccentSoftBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 12, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.ClipboardText,
                Width = 20,
                Height = 20,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = displayName,
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textStack.Children.Add(new TextBlock
        {
            Text = $"{displayCategory}  |  {displaySegment}  |  {durationMinutes} min  |  R$ {displayPrice}",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 3, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        var status = new Border
        {
            Background = isActive ? Solid("#DCFCE7") : GraySoftBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(9, 4, 9, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = isActive ? "Ativo" : "Inativo",
                Foreground = isActive ? Solid("#16A34A") : MutedBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            }
        };
        Grid.SetColumn(status, 2);
        grid.Children.Add(status);

        card.Child = grid;
        body.Children.Add(card);
    }

    private static void AddDialogInlineSection(StackPanel body, PackIconKind icon, string title, string subtitle)
    {
        var grid = new Grid { Margin = new Thickness(0, 6, 0, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new Border
        {
            Width = 30,
            Height = 30,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = icon,
                Width = 16,
                Height = 16,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 14,
            FontWeight = FontWeights.Bold
        });
        textStack.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        body.Children.Add(grid);
    }

    private static void AddDialogSection(StackPanel body, string title, string subtitle)
    {
        body.Children.Add(new Border
        {
            Background = Solid("#FFF9F4"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 4, 0, 14),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        Foreground = InkBrush,
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = subtitle,
                        Foreground = MutedBrush,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 3, 0, 0)
                    }
                }
            }
        });
    }

    private static void AddDialogInfoCard(StackPanel body, string title, string text, string background, string accent)
    {
        body.Children.Add(new Border
        {
            Background = Solid(background),
            BorderBrush = Solid(accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(AppBadgeRadiusValue),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        Foreground = InkBrush,
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = text,
                        Foreground = MutedBrush,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
            }
        });
    }

    private CheckBox AddDialogCheckBox(StackPanel body, string label, bool isChecked)
    {
        var check = new CheckBox
        {
            Content = label,
            IsChecked = isChecked,
            Foreground = InkBrush,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 14),
            Padding = new Thickness(2)
        };
        body.Children.Add(check);
        return check;
    }

    private static TextBlock DialogLabel(string text) => new()
    {
        Text = text,
        Foreground = MutedBrush,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 2, 0, 0)
    };

    private static string DialogComboText(ComboBox combo, string fallback)
    {
        var selected = combo.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(selected))
        {
            return selected.Trim();
        }

        var typed = combo.Text.Trim();
        return string.IsNullOrWhiteSpace(typed) ? fallback : typed;
    }

    private static bool TryReadDialogMoney(TextBox textBox, bool allowZero, out decimal value) =>
        TryParseMoney(textBox.Text, out value) && (allowZero ? value >= 0 : value > 0);

    private static bool TryReadDialogPercent(TextBox textBox, out decimal value)
    {
        value = 0;
        var text = (textBox.Text ?? "").Trim().Replace("%", "", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var parsed =
            decimal.TryParse(text, NumberStyles.Number, Brazil, out value) ||
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        if (!parsed || value < 0 || value > 100)
        {
            value = 0;
            return false;
        }

        value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryReadDialogInt(TextBox textBox, int min, int max, out int value) =>
        int.TryParse(textBox.Text.Trim(), NumberStyles.Integer, Brazil, out value) && value >= min && value <= max;

    private static void SetDialogError(TextBlock errorText, string message)
    {
        errorText.Text = message;
        errorText.Visibility = Visibility.Visible;
    }

    private string? PromptText(string title, string label, string initialValue)
    {
        var input = new TextBox
        {
            Text = initialValue,
            MinWidth = 340,
            Height = 38,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(10, 6, 10, 6),
            BorderBrush = LineBrush,
            Foreground = InkBrush
        };

        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            Background = Brushes.White,
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        Foreground = InkBrush,
                        FontSize = 18,
                        FontWeight = FontWeights.Bold
                    },
                    new TextBlock
                    {
                        Text = label,
                        Foreground = MutedBrush,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 12, 0, 0)
                    },
                    input,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 18, 0, 0),
                        Children =
                        {
                            new Button
                            {
                                Content = "Cancelar",
                                IsCancel = true,
                                MinWidth = 86,
                                Height = 34,
                                Margin = new Thickness(0, 0, 8, 0)
                            },
                            new Button
                            {
                                Content = "Criar",
                                IsDefault = true,
                                MinWidth = 86,
                                Height = 34
                            }
                        }
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel &&
            panel.Children[^1] is StackPanel buttons &&
            buttons.Children[^1] is Button createButton)
        {
            createButton.Click += (_, _) => dialog.DialogResult = true;
        }

        input.SelectAll();
        input.Focus();
        return ShowAppDialog(dialog) == true ? input.Text.Trim() : null;
    }

    private void SaveAppointmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadEditor(block: false, out var draft))
        {
            return;
        }

        var target = _selectedAppointment;
        var conflicts = FindConflicts(draft, target?.Id).ToList();
        if (conflicts.Count > 0)
        {
            ShowConflict(conflicts);
            return;
        }

        if (target is null)
        {
            target = new Appointment
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.Now,
                Status = AppointmentStatus.Scheduled
            };
            _data.Appointments.Add(target);
        }

        ApplyDraft(target, draft, target.Status == AppointmentStatus.Blocked ? AppointmentStatus.Scheduled : target.Status);
        UpsertCustomer(target);
        SaveAndRefresh(target.Id, $"Agendamento salvo para {target.CustomerName} às {target.Start:HH:mm}.");
    }

    private void BlockTimeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadEditor(block: true, out var draft))
        {
            return;
        }

        var conflicts = FindConflicts(draft, _selectedAppointment?.Id).ToList();
        if (conflicts.Count > 0)
        {
            ShowConflict(conflicts);
            return;
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid().ToString("N"),
            Segment = draft.Segment,
            CustomerName = "Horário bloqueado",
            CustomerProfile = string.IsNullOrWhiteSpace(draft.Profile) ? "Bloqueio interno" : draft.Profile,
            CustomerPhone = "",
            ServiceId = "",
            ServiceName = "Bloqueio interno",
            ProfessionalId = draft.ProfessionalId,
            ProfessionalName = draft.ProfessionalName,
            ResourceName = draft.ResourceName,
            Start = draft.Start,
            DurationMinutes = draft.DurationMinutes,
            Price = 0,
            Status = AppointmentStatus.Blocked,
            Notes = string.IsNullOrWhiteSpace(draft.Notes) ? "Horário indisponível para novos agendamentos." : draft.Notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _data.Appointments.Add(appointment);
        SaveAndRefresh(appointment.Id, $"Horário bloqueado em {appointment.Start:dd/MM HH:mm}.");
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => SetSelectedStatus(AppointmentStatus.Confirmed);

    private void CheckInButton_Click(object sender, RoutedEventArgs e) => SetSelectedStatus(AppointmentStatus.Waiting);

    private void StartServiceButton_Click(object sender, RoutedEventArgs e) => SetSelectedStatus(AppointmentStatus.InService);

    private void FinishButton_Click(object sender, RoutedEventArgs e) => SetSelectedStatus(AppointmentStatus.Done);

    private void CancelButton_Click(object sender, RoutedEventArgs e) => SetSelectedStatus(AppointmentStatus.Cancelled);

    private void NoShowButton_Click(object sender, RoutedEventArgs e) => SetSelectedStatus(AppointmentStatus.NoShow);

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAppointment is null)
        {
            ShowStatus("Selecione um agendamento para duplicar.");
            return;
        }

        var copy = new Appointment
        {
            Id = Guid.NewGuid().ToString("N"),
            Segment = _selectedAppointment.Segment,
            CustomerName = _selectedAppointment.CustomerName,
            CustomerPhone = _selectedAppointment.CustomerPhone,
            CustomerProfile = _selectedAppointment.CustomerProfile,
            ServiceId = _selectedAppointment.ServiceId,
            ServiceName = _selectedAppointment.ServiceName,
            ProfessionalId = _selectedAppointment.ProfessionalId,
            ProfessionalName = _selectedAppointment.ProfessionalName,
            ResourceName = _selectedAppointment.ResourceName,
            Start = _selectedAppointment.Start.AddDays(7),
            DurationMinutes = _selectedAppointment.DurationMinutes,
            Price = _selectedAppointment.Price,
            Status = AppointmentStatus.Scheduled,
            Notes = _selectedAppointment.Notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var draft = AppointmentDraft.From(copy);
        var conflicts = FindConflicts(draft, null).ToList();
        if (conflicts.Count > 0)
        {
            AppointmentDatePicker.SelectedDate = copy.Start.Date;
            TimeCombo.Text = copy.Start.ToString("HH:mm", Brazil);
            ShowConflict(conflicts);
            return;
        }

        _data.Appointments.Add(copy);
        UpsertCustomer(copy);
        SaveAndRefresh(copy.Id, $"Agendamento duplicado para {copy.Start:dd/MM HH:mm}.");
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAppointment is null)
        {
            ShowStatus("Selecione um agendamento para excluir.");
            return;
        }

        if (!ConfirmDestructiveAction(
                "Excluir agendamento",
                $"Excluir o agendamento de {_selectedAppointment.CustomerName} em {_selectedAppointment.Start:dd/MM HH:mm}?",
                "Excluir agendamento"))
        {
            return;
        }

        var removedName = _selectedAppointment.CustomerName;
        _data.Appointments.Remove(_selectedAppointment);
        _selectedAppointment = null;
        _store.Save(_data);
        ClearEditor();
        CloseAppointmentEditorModal();
        RefreshAll();
        ShowStatus($"Agendamento de {removedName} excluído.");
    }

    private bool ConfirmDestructiveAction(string title, string message, string primaryText)
    {
        var shell = CreateEditorDialog(title, "Revise a ação antes de continuar.", primaryText);
        shell.Dialog.Width = 540;
        shell.Dialog.MaxHeight = 420;
        shell.Body.Width = 460;
        AddDialogInfoCard(shell.Body, "Atenção", message, "#FEF2F2", "#FCA5A5");
        shell.PrimaryButton.Background = Solid("#DC2626");
        shell.PrimaryButton.BorderBrush = Solid("#DC2626");
        TextElement.SetForeground(shell.PrimaryButton, Brushes.White);

        var confirmed = false;
        shell.PrimaryButton.Click += (_, _) =>
        {
            confirmed = true;
            shell.Dialog.DialogResult = true;
        };
        return ShowAppDialog(shell.Dialog) == true && confirmed;
    }

    private void SetSelectedStatus(AppointmentStatus status)
    {
        if (_selectedAppointment is null)
        {
            ShowStatus("Selecione um agendamento para alterar o status.");
            return;
        }

        if (_selectedAppointment.Status == AppointmentStatus.Blocked && status != AppointmentStatus.Cancelled)
        {
            ShowStatus("Bloqueios podem ser cancelados ou excluídos.");
            return;
        }

        if (!CanApplyStatus(_selectedAppointment, status, out var statusError))
        {
            ShowAppointmentEditorAlert(statusError, error: true);
            ShowStatus(statusError);
            return;
        }

        ClearAppointmentEditorAlert();
        _selectedAppointment.Status = status;
        _selectedAppointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(_selectedAppointment.Id);
        LoadEditor(_selectedAppointment);
        ShowStatus($"{_selectedAppointment.CustomerName}: status alterado para {StatusLabel(status)}.");
    }

    private static bool CanApplyStatus(Appointment appointment, AppointmentStatus target, out string error)
    {
        error = "";
        if (appointment.Status == target)
        {
            return true;
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Done)
        {
            error = "Esse atendimento já foi encerrado. Edite o agendamento se precisar corrigir.";
            return false;
        }

        if (target == AppointmentStatus.InService &&
            appointment.Status is not AppointmentStatus.Confirmed and not AppointmentStatus.Waiting)
        {
            error = "Confirme ou marque que o cliente chegou antes de iniciar o atendimento.";
            return false;
        }

        if (target == AppointmentStatus.Done &&
            appointment.Status is not AppointmentStatus.Waiting and not AppointmentStatus.InService)
        {
            error = "Marque chegada ou inicie o atendimento antes de finalizar.";
            return false;
        }

        if (target == AppointmentStatus.Confirmed &&
            appointment.Status is not AppointmentStatus.Scheduled)
        {
            error = "A confirmação só entra antes da chegada do cliente.";
            return false;
        }

        return true;
    }

    private void ApplyDraft(Appointment appointment, AppointmentDraft draft, AppointmentStatus status)
    {
        appointment.Segment = draft.Segment;
        appointment.CustomerName = draft.CustomerName;
        appointment.CustomerPhone = draft.Phone;
        appointment.CustomerProfile = draft.Profile;
        appointment.ServiceId = draft.ServiceId;
        appointment.ServiceName = draft.ServiceName;
        appointment.ProfessionalId = draft.ProfessionalId;
        appointment.ProfessionalName = draft.ProfessionalName;
        appointment.ResourceName = draft.ResourceName;
        appointment.Start = draft.Start;
        appointment.DurationMinutes = draft.DurationMinutes;
        appointment.Price = draft.Price;
        appointment.Status = status;
        appointment.Notes = draft.Notes;
        appointment.UpdatedAt = DateTime.Now;
    }

    private void SaveAndRefresh(string appointmentId, string message)
    {
        _store.Save(_data);
        var appointment = _data.Appointments.First(item => item.Id == appointmentId);
        _selectedDate = appointment.Start.Date;
        UpdateDateFilterButton();
        RefreshAll(appointmentId);
        _selectedAppointment = appointment;
        LoadEditor(appointment);
        CloseAppointmentEditorModal();
        ShowStatus(message);
    }

    private bool TryReadEditor(bool block, out AppointmentDraft draft)
    {
        draft = default!;
        ClearAppointmentEditorAlert();

        if (AppointmentDatePicker.SelectedDate is not DateTime date)
        {
            return FailAppointmentEditor("Informe a data do agendamento.", AppointmentDatePicker);
        }

        if (!TryParseTime(TimeCombo.Text, out var time))
        {
            return FailAppointmentEditor("Informe a hora no formato 08:30.", TimeCombo);
        }

        if (!TryReadDuration(out var duration))
        {
            return FailAppointmentEditor("Informe uma duração válida entre 5 e 480 minutos.", DurationCombo);
        }

        var segment = AppointmentSegmentCombo.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(segment))
        {
            return FailAppointmentEditor("Escolha o tipo de atendimento.", AppointmentSegmentCombo);
        }

        var service = ServiceCombo.SelectedItem as ServiceItem;
        if (!block && service is null)
        {
            return FailAppointmentEditor("Escolha um serviço ativo.", ServiceCombo);
        }

        if (!block && service is { IsActive: false })
        {
            return FailAppointmentEditor("Esse serviço está desativado. Escolha outro serviço ativo.", ServiceCombo);
        }

        var professional = ProfessionalCombo.SelectedItem as Professional;
        if (professional is null)
        {
            return FailAppointmentEditor("Escolha um profissional ativo.", ProfessionalCombo);
        }

        if (!professional.IsActive)
        {
            return FailAppointmentEditor("Esse profissional está desativado. Escolha outro profissional ativo.", ProfessionalCombo);
        }

        var customerName = block ? "Horário bloqueado" : CustomerNameTextBox.Text.Trim();
        if (!block && string.IsNullOrWhiteSpace(customerName))
        {
            return FailAppointmentEditor("Informe o cliente, paciente, tutor ou veículo.", CustomerNameTextBox);
        }

        var price = 0m;
        if (!block)
        {
            if (string.IsNullOrWhiteSpace(PriceTextBox.Text))
            {
                price = service?.Price ?? 0;
            }
            else if (!TryParseMoney(PriceTextBox.Text, out price))
            {
                return FailAppointmentEditor("Informe um valor válido.", PriceTextBox);
            }

            if (price < 0)
            {
                return FailAppointmentEditor("O valor do atendimento não pode ser negativo.", PriceTextBox);
            }
        }

        var start = date.Date.Add(time);
        var end = start.AddMinutes(duration);
        if (!TryValidateConfiguredBusinessWindow(start, end, out var businessWindowError))
        {
            return FailAppointmentEditor(
                businessWindowError,
                TimeCombo);
        }

        var resourceName = CurrentResourceText();
        if (string.IsNullOrWhiteSpace(resourceName) && !string.IsNullOrWhiteSpace(service?.DefaultResource))
        {
            resourceName = service.DefaultResource.Trim();
            SelectResource(resourceName);
        }

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return FailAppointmentEditor("Informe a sala, cadeira, box ou recurso usado nesse horário.", ResourceCombo);
        }

        if (!TryNormalizeCustomerPhone(PhoneTextBox.Text, out var phone, out var phoneError))
        {
            return FailAppointmentEditor(phoneError, PhoneTextBox);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            PhoneTextBox.Text = phone;
        }

        if (start < DateTime.Now.AddMinutes(-5) && !block && _selectedAppointment is null)
        {
            return FailAppointmentEditor("Esse horário já passou. Escolha um horário atual ou futuro.", TimeCombo);
        }

        draft = new AppointmentDraft(
            segment,
            customerName,
            phone,
            CustomerProfileTextBox.Text.Trim(),
            service?.Id ?? "",
            block ? "Bloqueio interno" : service?.Name ?? "Atendimento",
            professional.Id,
            professional.Name,
            resourceName,
            start,
            duration,
            price,
            NotesTextBox.Text.Trim());

        return true;
    }

    private static bool TryNormalizeCustomerPhone(string? text, out string formatted, out string error)
    {
        formatted = "";
        error = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var digits = OnlyDigits(text);
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            digits = digits[2..];
        }

        if (digits.Length == 10)
        {
            formatted = $"({digits[..2]}) {digits.Substring(2, 4)}-{digits[6..]}";
            return true;
        }

        if (digits.Length == 11)
        {
            formatted = $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}";
            return true;
        }

        error = "Informe telefone com DDD e 10 ou 11 dígitos, ou deixe em branco.";
        return false;
    }

    private void OnboardingCepTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formattingOnboardingCep || sender is not TextBox textBox)
        {
            return;
        }

        var original = textBox.Text ?? "";
        var digitCaret = OnlyDigits(original[..Math.Min(textBox.CaretIndex, original.Length)]).Length;
        var formatted = FormatCepInput(original);
        if (formatted != original)
        {
            _formattingOnboardingCep = true;
            textBox.Text = formatted;
            textBox.CaretIndex = CaretIndexAfterDigits(formatted, digitCaret);
            _formattingOnboardingCep = false;
        }

        var cepDigits = OnlyDigits(textBox.Text ?? "");
        if (cepDigits.Length == 8)
        {
            _ = LookupOnboardingCepAsync(cepDigits);
            return;
        }

        _lastOnboardingCepLookup = "";
        _cepLookupCancellation?.Cancel();
    }

    private void OnboardingCepTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var formatted = FormatCepInput(textBox.Text);
        if (formatted != textBox.Text)
        {
            _formattingOnboardingCep = true;
            textBox.Text = formatted;
            textBox.CaretIndex = textBox.Text.Length;
            _formattingOnboardingCep = false;
        }

        var cepDigits = OnlyDigits(formatted);
        if (cepDigits.Length == 8)
        {
            _ = LookupOnboardingCepAsync(cepDigits);
        }
    }

    private async Task LookupOnboardingCepAsync(string cepDigits)
    {
        if (cepDigits.Length != 8 || cepDigits == _lastOnboardingCepLookup)
        {
            return;
        }

        _lastOnboardingCepLookup = cepDigits;
        _cepLookupCancellation?.Cancel();
        _cepLookupCancellation = new CancellationTokenSource();
        var token = _cepLookupCancellation.Token;

        try
        {
            using var response = await CepClient.GetAsync($"https://viacep.com.br/ws/{cepDigits}/json/", token);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(token);
            var result = JsonSerializer.Deserialize<ViaCepAddress>(body, WebJsonOptions);
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (result is null || result.Erro)
            {
                ShowStatus("CEP não encontrado. Confira o número ou preencha o endereço manualmente.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Bairro))
            {
                OnboardingNeighborhoodTextBox.Text = result.Bairro.Trim();
            }

            if (!string.IsNullOrWhiteSpace(result.Logradouro))
            {
                OnboardingStreetTextBox.Text = result.Logradouro.Trim();
            }

            ShowStatus("Endereço preenchido pelo CEP.");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            ShowStatus("Não foi possível consultar o CEP agora. Você pode preencher manualmente.");
        }
    }

    private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formattingCustomerPhone || sender is not TextBox textBox)
        {
            return;
        }

        RefreshAppointmentEditorSummary();

        var original = textBox.Text ?? "";
        var digitCaret = OnlyDigits(original[..Math.Min(textBox.CaretIndex, original.Length)]).Length;
        var formatted = FormatCustomerPhoneInput(original);
        if (formatted == original)
        {
            return;
        }

        _formattingCustomerPhone = true;
        textBox.Text = formatted;
        textBox.CaretIndex = CaretIndexAfterDigits(formatted, digitCaret);
        _formattingCustomerPhone = false;
        RefreshAppointmentEditorSummary();
    }

    private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !TryNormalizeCustomerPhone(textBox.Text, out var formatted, out _) ||
            formatted == textBox.Text)
        {
            return;
        }

        _formattingCustomerPhone = true;
        textBox.Text = formatted;
        textBox.CaretIndex = textBox.Text.Length;
        _formattingCustomerPhone = false;
    }

    private void DialogPhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formattingDialogText || sender is not TextBox textBox)
        {
            return;
        }

        var original = textBox.Text ?? "";
        var digitCaret = OnlyDigits(original[..Math.Min(textBox.CaretIndex, original.Length)]).Length;
        var formatted = FormatCustomerPhoneInput(original);
        if (formatted == original)
        {
            return;
        }

        _formattingDialogText = true;
        textBox.Text = formatted;
        textBox.CaretIndex = CaretIndexAfterDigits(formatted, digitCaret);
        _formattingDialogText = false;
    }

    private void DialogPhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !TryNormalizeCustomerPhone(textBox.Text, out var formatted, out _) ||
            formatted == textBox.Text)
        {
            return;
        }

        _formattingDialogText = true;
        textBox.Text = formatted;
        textBox.CaretIndex = textBox.Text.Length;
        _formattingDialogText = false;
    }

    private void DialogDocumentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formattingDialogText || sender is not TextBox textBox)
        {
            return;
        }

        var original = textBox.Text ?? "";
        var digitCaret = OnlyDigits(original[..Math.Min(textBox.CaretIndex, original.Length)]).Length;
        var formatted = FormatDocumentInput(original);
        if (formatted == original)
        {
            return;
        }

        _formattingDialogText = true;
        textBox.Text = formatted;
        textBox.CaretIndex = CaretIndexAfterDigits(formatted, digitCaret);
        _formattingDialogText = false;
    }

    private void DialogDocumentTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !TryFormatBusinessDocument(textBox.Text, out var formatted, out _) ||
            formatted == textBox.Text)
        {
            return;
        }

        _formattingDialogText = true;
        textBox.Text = formatted;
        textBox.CaretIndex = textBox.Text.Length;
        _formattingDialogText = false;
    }

    private void DialogPercentTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && TryReadDialogPercent(textBox, out var value))
        {
            textBox.Text = value.ToString("N2", Brazil);
            textBox.CaretIndex = textBox.Text.Length;
        }
    }

    private static string FormatCustomerPhoneInput(string text)
    {
        var digits = OnlyDigits(text ?? "");
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length > 11)
        {
            digits = digits[2..];
        }

        if (digits.Length > 11)
        {
            digits = digits[..11];
        }

        return digits.Length switch
        {
            0 => "",
            <= 2 => digits,
            <= 6 => $"({digits[..2]}) {digits[2..]}",
            <= 10 => $"({digits[..2]}) {digits.Substring(2, 4)}-{digits[6..]}",
            _ => $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}"
        };
    }

    private static string FormatCepInput(string? text)
    {
        var digits = OnlyDigits(text ?? "");
        if (digits.Length > 8)
        {
            digits = digits[..8];
        }

        return digits.Length switch
        {
            0 => "",
            <= 5 => digits,
            _ => $"{digits[..5]}-{digits[5..]}"
        };
    }

    private static string FormatDocumentInput(string text)
    {
        var digits = OnlyDigits(text ?? "");
        if (digits.Length > 14)
        {
            digits = digits[..14];
        }

        return digits.Length switch
        {
            0 => "",
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}",
            <= 11 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}",
            <= 12 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits[8..]}",
            <= 14 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits[12..]}",
            _ => digits
        };
    }

    private static int CaretIndexAfterDigits(string text, int digitCount)
    {
        if (digitCount <= 0)
        {
            return 0;
        }

        var seen = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (!char.IsDigit(text[index]))
            {
                continue;
            }

            seen++;
            if (seen >= digitCount)
            {
                return index + 1;
            }
        }

        return text.Length;
    }

    private bool TryReadDuration(out int duration)
    {
        duration = 0;
        if (DurationCombo.SelectedItem is int selected)
        {
            duration = selected;
            return duration is >= 5 and <= 480;
        }

        return int.TryParse(DurationCombo.Text, NumberStyles.Integer, Brazil, out duration) &&
               duration is >= 5 and <= 480;
    }

    private static bool TryParseTime(string text, out TimeSpan time)
    {
        text = (text ?? "").Trim();
        return TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out time) ||
               TimeSpan.TryParseExact(text, @"h\:mm", CultureInfo.InvariantCulture, out time) ||
               TimeSpan.TryParse(text, Brazil, out time) && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
    }

    private static bool TryParseMoney(string text, out decimal value)
    {
        text = text.Trim().Replace("R$", "", StringComparison.OrdinalIgnoreCase);
        return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, Brazil, out value) ||
               decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out value);
    }

    private IEnumerable<Appointment> FindConflicts(AppointmentDraft draft, string? ignoreId)
    {
        var end = draft.Start.AddMinutes(draft.DurationMinutes);
        return _data.Appointments.Where(item =>
            item.Id != ignoreId &&
            IsOperationalStatus(item) &&
            draft.Start < item.End &&
            end > item.Start &&
            (item.ProfessionalId == draft.ProfessionalId ||
             (!string.IsNullOrWhiteSpace(item.ResourceName) &&
              item.ResourceName.Equals(draft.ResourceName, StringComparison.OrdinalIgnoreCase))));
    }

    private static bool IsOperationalStatus(Appointment appointment) =>
        appointment.Status is AppointmentStatus.Scheduled
            or AppointmentStatus.Confirmed
            or AppointmentStatus.Waiting
            or AppointmentStatus.InService;

    private void ShowConflict(IReadOnlyCollection<Appointment> conflicts)
    {
        SetAppointmentEditorStep(0);
        var conflict = conflicts.OrderBy(item => item.Start).First();
        var message =
            $"Horário ocupado: {conflict.Start:dd/MM HH:mm} - {conflict.End:HH:mm}. " +
            $"{conflict.CustomerName} com {conflict.ProfessionalName}" +
            (string.IsNullOrWhiteSpace(conflict.ResourceName) ? "." : $" em {conflict.ResourceName}.");
        ShowAppointmentEditorAlert(message, error: true);
    }

    private void UpsertCustomer(Appointment appointment)
    {
        if (appointment.Status == AppointmentStatus.Blocked ||
            string.IsNullOrWhiteSpace(appointment.CustomerName) ||
            appointment.CustomerName.Equals("Horário bloqueado", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var customer = _data.Customers.FirstOrDefault(item =>
            item.Name.Equals(appointment.CustomerName, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(appointment.CustomerPhone) ||
             item.Phone.Equals(appointment.CustomerPhone, StringComparison.OrdinalIgnoreCase)));

        if (customer is null)
        {
            _data.Customers.Add(new Customer
            {
                Name = appointment.CustomerName,
                Phone = appointment.CustomerPhone,
                Segment = appointment.Segment,
                Profile = appointment.CustomerProfile,
                LastSeenAt = appointment.Start
            });
            return;
        }

        customer.Phone = appointment.CustomerPhone;
        customer.Segment = appointment.Segment;
        customer.Profile = appointment.CustomerProfile;
        customer.LastSeenAt = appointment.Start;
    }

    private void ReselectAppointment(string appointmentId)
    {
        _syncingSelection = true;
        var dayRow = _dayRows.FirstOrDefault(item => item.Appointment.Id == appointmentId);
        var weekRow = _weekRows.FirstOrDefault(item => item.Appointment.Id == appointmentId);
        var selectedRow = dayRow ?? weekRow;

        DayAgendaList.SelectedItem = dayRow;
        WeekAgendaList.SelectedItem = dayRow is null ? weekRow : null;
        _syncingSelection = false;

        if (selectedRow is null && _selectedAppointment?.Id == appointmentId)
        {
            _selectedAppointment = null;
            SelectedAppointmentCard.Visibility = Visibility.Collapsed;
        }
    }

    private void DateFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (DateFilterPopup.IsOpen)
        {
            DateFilterPopup.IsOpen = false;
            return;
        }

        _datePopoverStart = _selectedDate.Date.AddDays(-2);
        DatePopoverMainView.Visibility = Visibility.Visible;
        DatePopoverCalendarView.Visibility = Visibility.Collapsed;
        RefreshDatePopover();
        DateFilterPopup.IsOpen = true;

        Dispatcher.BeginInvoke(() =>
        {
            var selectedButton = FindVisualChildren<Button>(DatePopoverDaysItems)
                .FirstOrDefault(button => button.Tag is DateTime date && date.Date == _selectedDate.Date);
            selectedButton?.Focus();
        }, DispatcherPriority.Input);
    }

    private void RefreshDatePopover()
    {
        var start = _datePopoverStart.Date;
        var end = start.AddDays(6);
        DatePopoverRangeText.Text = DatePopoverRangeLabel(start, end);

        DatePopoverDaysItems.ItemsSource = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = start.AddDays(offset);
                var isSelected = date.Date == _selectedDate.Date;
                var dayLabel = date.ToString("ddd", Brazil).TrimEnd('.');
                var automationName = date.ToString("dddd, dd 'de' MMMM 'de' yyyy", Brazil);
                return new DatePopoverDay(
                    date,
                    dayLabel,
                    date.Day.ToString("00", Brazil),
                    date.Date == DateTime.Today ? "Hoje" : "",
                    isSelected,
                    automationName,
                    isSelected ? "Selecionado" : "Não selecionado");
            })
            .ToList();
    }

    private static string DatePopoverRangeLabel(DateTime start, DateTime end)
    {
        if (start.Year == end.Year && start.Month == end.Month)
        {
            return $"{start.Day} – {end.Day} de {end.ToString("MMMM", Brazil)}";
        }

        if (start.Year == end.Year)
        {
            return $"{start.Day} de {start.ToString("MMM", Brazil)} – {end.Day} de {end.ToString("MMM", Brazil)}";
        }

        return $"{start:dd/MM/yyyy} – {end:dd/MM/yyyy}";
    }

    private void DatePopoverPrevious_Click(object sender, RoutedEventArgs e)
    {
        _datePopoverStart = _datePopoverStart.AddDays(-7);
        RefreshDatePopover();
    }

    private void DatePopoverNext_Click(object sender, RoutedEventArgs e)
    {
        _datePopoverStart = _datePopoverStart.AddDays(7);
        RefreshDatePopover();
    }

    private void DatePopoverDay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime date })
        {
            return;
        }

        DateFilterPopup.IsOpen = false;
        SelectDate(date);
    }

    private void DateQuickYesterday_Click(object sender, RoutedEventArgs e) =>
        SelectDateFromPopover(DateTime.Today.AddDays(-1));

    private void DateQuickToday_Click(object sender, RoutedEventArgs e) =>
        SelectDateFromPopover(DateTime.Today);

    private void DateQuickTomorrow_Click(object sender, RoutedEventArgs e) =>
        SelectDateFromPopover(DateTime.Today.AddDays(1));

    private void SelectDateFromPopover(DateTime date)
    {
        DateFilterPopup.IsOpen = false;
        SelectDate(date);
    }

    private void DatePopoverChooseOther_Click(object sender, RoutedEventArgs e)
    {
        _suppressDatePopoverCalendarSelection = true;
        DatePopoverCalendar.SelectedDate = _selectedDate.Date;
        DatePopoverCalendar.DisplayDate = _selectedDate.Date;
        _suppressDatePopoverCalendarSelection = false;
        DatePopoverMainView.Visibility = Visibility.Collapsed;
        DatePopoverCalendarView.Visibility = Visibility.Visible;
        DatePopoverCalendar.Focus();
    }

    private void DatePopoverCalendarBack_Click(object sender, RoutedEventArgs e)
    {
        DatePopoverCalendarView.Visibility = Visibility.Collapsed;
        DatePopoverMainView.Visibility = Visibility.Visible;
        RefreshDatePopover();

        Dispatcher.BeginInvoke(() =>
        {
            var selectedButton = FindVisualChildren<Button>(DatePopoverDaysItems)
                .FirstOrDefault(button => button.Tag is DateTime date && date.Date == _selectedDate.Date);
            selectedButton?.Focus();
        }, DispatcherPriority.Input);
    }

    private void DatePopoverCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressDatePopoverCalendarSelection || DatePopoverCalendar.SelectedDate is not DateTime date)
        {
            return;
        }

        DateFilterPopup.IsOpen = false;
        SelectDate(date);
    }

    private void DateFilterPopup_Closed(object? sender, EventArgs e)
    {
        DatePopoverCalendarView.Visibility = Visibility.Collapsed;
        DatePopoverMainView.Visibility = Visibility.Visible;
    }

    private void SegmentFilterButton_Click(object sender, RoutedEventArgs e)
    {
        var menu = CreateThemedContextMenu(190);
        AddSegmentMenuItem(menu, AllSegments, "Todos segmentos");
        foreach (var segment in GetAvailableSegments())
        {
            AddSegmentMenuItem(menu, segment, segment);
        }

        menu.PlacementTarget = SegmentFilterButton;
        menu.IsOpen = true;
    }

    private void AddSegmentMenuItem(ContextMenu menu, string segment, string header)
    {
        var item = new MenuItem
        {
            Header = header,
            FontWeight = segment == _selectedSegmentFilter ? FontWeights.Bold : FontWeights.Normal,
            FontSize = 13,
            MinHeight = 40,
            Padding = new Thickness(12, 8, 12, 8),
            IsCheckable = true,
            IsChecked = segment == _selectedSegmentFilter
        };
        item.Click += (_, _) => SelectSegmentFilter(segment);
        menu.Items.Add(item);
    }

    private static ContextMenu CreateThemedContextMenu(double minWidth)
    {
        var menu = new ContextMenu
        {
            MinWidth = minWidth,
            Padding = new Thickness(4),
            Background = PanelBrush,
            Foreground = InkBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1)
        };

        var menuItemStyle = new Style(typeof(MenuItem));
        menuItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, PanelBrush));
        menuItemStyle.Setters.Add(new Setter(Control.ForegroundProperty, InkBrush));
        menuItemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        var hoverTrigger = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, AccentSoftBrush));
        hoverTrigger.Setters.Add(new Setter(Control.ForegroundProperty, AccentTextBrush));
        menuItemStyle.Triggers.Add(hoverTrigger);
        menu.Resources.Add(typeof(MenuItem), menuItemStyle);
        return menu;
    }

    private void SelectSegmentFilter(string segment)
    {
        _selectedSegmentFilter = segment;
        UpdateSegmentFilterButton();
        RefreshAll();

        if (_selectedAppointment is null)
        {
            var availableSegments = GetAvailableSegments();
            var editorSegment = CurrentSegmentFilter() == AllSegments ? availableSegments[0] : CurrentSegmentFilter();
            AppointmentSegmentCombo.SelectedItem = editorSegment;
            UpdateAppointmentOptions(editorSegment);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_mainWindowInitialized)
        {
            return;
        }

        _searchRefreshTimer.Stop();
        _searchRefreshTimer.Start();
    }

    private void AppointmentSegmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppointmentSegmentCombo.SelectedItem is string segment)
        {
            UpdateAppointmentOptions(segment);
        }

        RefreshAppointmentEditorSummary();
    }

    private void ServiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loadingEditor && ServiceCombo.SelectedItem is ServiceItem service)
        {
            ApplyServiceDefaults(service);
        }

        RefreshAppointmentEditorSummary();
    }

    private void ScheduleAppointment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement target || target.Tag is not Appointment appointment)
        {
            return;
        }

        e.Handled = true;
        if (IsPreviewAppointment(appointment))
        {
            ShowAppointmentInfoPopup(target, appointment);
            ShowStatus("Agendamento de exemplo aberto para visualização.");
            return;
        }

        _selectedAppointment = appointment;
        SelectedAppointmentCard.Visibility = Visibility.Visible;
        ShowSelectedAppointment(appointment);

        _syncingSelection = true;
        DayAgendaList.SelectedItem = _dayRows.FirstOrDefault(item => item.Appointment.Id == appointment.Id);
        WeekAgendaList.SelectedItem = null;
        _syncingSelection = false;

        ShowAppointmentInfoPopup(target, appointment);
        ShowStatus($"{appointment.CustomerName} aberto no quadro.");
    }

    private void ShowAppointmentInfoPopup(FrameworkElement placementTarget, Appointment appointment)
    {
        CloseAppointmentInfoPopup();

        var popup = new Popup
        {
            PlacementTarget = placementTarget,
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = 12,
            VerticalOffset = 10,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true
        };

        popup.Child = CreateAppointmentInfoPopupContent(appointment, popup);
        _appointmentInfoPopup = popup;
        popup.Closed += (_, _) => DetachAppointmentInfoOutsideClickHandler();
        popup.IsOpen = true;
        AttachAppointmentInfoOutsideClickHandler();
    }

    private void AttachAppointmentInfoOutsideClickHandler()
    {
        DetachAppointmentInfoOutsideClickHandler();

        _appointmentInfoOutsideClickHandler = (_, args) =>
        {
            if (_appointmentInfoPopup?.Child is DependencyObject popupChild &&
                IsDescendantOf(args.OriginalSource as DependencyObject, popupChild))
            {
                return;
            }

            CloseAppointmentInfoPopup();
        };

        Dispatcher.BeginInvoke(() =>
        {
            if (_appointmentInfoPopup?.IsOpen == true && _appointmentInfoOutsideClickHandler is not null)
            {
                PreviewMouseDown += _appointmentInfoOutsideClickHandler;
            }
        }, DispatcherPriority.Background);
    }

    private void DetachAppointmentInfoOutsideClickHandler()
    {
        if (_appointmentInfoOutsideClickHandler is null)
        {
            return;
        }

        PreviewMouseDown -= _appointmentInfoOutsideClickHandler;
        _appointmentInfoOutsideClickHandler = null;
    }

    private void CloseAppointmentInfoPopup()
    {
        DetachAppointmentInfoOutsideClickHandler();

        if (_appointmentInfoPopup is null)
        {
            return;
        }

        var popup = _appointmentInfoPopup;
        _appointmentInfoPopup = null;
        popup.IsOpen = false;
    }

    private Border CreateAppointmentInfoPopupContent(Appointment appointment, Popup popup)
    {
        var isPreview = IsPreviewAppointment(appointment);
        var statusAccent = ScheduleAccentFor(appointment.Status);
        var card = new Border
        {
            Width = 306,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.16
            }
        };

        var body = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new Border
        {
            Width = 38,
            Height = 38,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.CalendarClock,
                Width = 19,
                Height = 19,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = appointment.CustomerName,
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = isPreview ? "Agendamento de exemplo" : "Informações do atendimento",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(titleStack, 1);
        header.Children.Add(titleStack);

        var closeButton = new Button
        {
            Style = (Style)FindResource("SubtleButton"),
            Width = 40,
            MinWidth = 40,
            Height = 40,
            Padding = new Thickness(0),
            Background = WarmSoftBrush,
            Cursor = Cursors.Hand,
            ToolTip = "Fechar detalhes do atendimento",
            Content = new PackIcon
            {
                Kind = PackIconKind.Close,
                Width = 15,
                Height = 15,
                Foreground = MutedBrush
            }
        };
        closeButton.Click += (_, _) => CloseAppointmentInfoPopup();
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);
        body.Children.Add(header);

        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.AccountOutline, "Cliente", ClientPopupText(appointment)));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.ContentCut, "Serviço", appointment.ServiceName));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.ClockOutline, "Horário", $"{appointment.Start:HH:mm} - {appointment.End:HH:mm} | {appointment.DurationMinutes} min"));
        body.Children.Add(CreateAppointmentInfoRow(PackIconKind.Cash, "Valor", appointment.Price.ToString("C", Brazil)));

        if (!string.IsNullOrWhiteSpace(appointment.ResourceName))
        {
            body.Children.Add(CreateAppointmentInfoRow(PackIconKind.Seat, "Local", appointment.ResourceName));
        }

        var footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        footer.Children.Add(new Border
        {
            Background = ScheduleCardBackground(appointment.Status),
            BorderBrush = statusAccent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = ScheduleStatusLabel(appointment.Status),
                Foreground = statusAccent,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            }
        });

        var editPill = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Background = AccentSoftBrush,
            BorderBrush = AccentTextBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 5, 10, 5),
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Right,
            Cursor = Cursors.Hand,
            Content = new TextBlock
            {
                Text = "Editar",
                Foreground = AccentTextBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            }
        };
        AutomationProperties.SetName(closeButton, "Fechar detalhes do atendimento");
        AutomationProperties.SetName(editPill, "Editar agendamento");
        editPill.Click += (_, _) =>
        {
            OpenAppointmentEditorFromInfoPopup(appointment);
        };
        Grid.SetColumn(editPill, 1);
        footer.Children.Add(editPill);

        body.Children.Add(footer);
        card.Child = body;
        return card;
    }

    private void OpenAppointmentEditorFromInfoPopup(Appointment appointment)
    {
        CloseAppointmentInfoPopup();
        OpenAppointmentQuickEditPopup(appointment);
    }

    private void OpenAppointmentQuickEditPopup(Appointment appointment)
    {
        CloseAppointmentQuickEditPopup();

        var segment = string.IsNullOrWhiteSpace(appointment.Segment)
            ? GetAvailableSegments()[0]
            : appointment.Segment;
        var services = ServicesForEditor(segment).ToList();
        var selectedService = services.FirstOrDefault(item => !string.IsNullOrWhiteSpace(appointment.ServiceId) &&
                                                             item.Id.Equals(appointment.ServiceId, StringComparison.OrdinalIgnoreCase))
                              ?? services.FirstOrDefault(item => item.Name.Equals(appointment.ServiceName, StringComparison.OrdinalIgnoreCase));
        if (selectedService is null && !string.IsNullOrWhiteSpace(appointment.ServiceName))
        {
            selectedService = new ServiceItem
            {
                Id = $"quick_{StableHash(appointment.ServiceName)}",
                Segment = segment,
                Name = appointment.ServiceName,
                Category = "Atendimento",
                DurationMinutes = Math.Clamp(appointment.DurationMinutes, 5, 480),
                Price = appointment.Price,
                DefaultResource = appointment.ResourceName,
                IsActive = true
            };
            services.Add(selectedService);
        }

        var popup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = 12,
            VerticalOffset = 8,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true
        };

        popup.Child = CreateAppointmentQuickEditContent(appointment, popup, services, selectedService);
        _appointmentQuickEditPopup = popup;
        popup.Closed += (_, _) => DetachAppointmentQuickEditOutsideClickHandler();
        popup.IsOpen = true;
        AttachAppointmentQuickEditOutsideClickHandler();
    }

    private Border CreateAppointmentQuickEditContent(
        Appointment appointment,
        Popup popup,
        IReadOnlyList<ServiceItem> services,
        ServiceItem? selectedService)
    {
        var isPreview = IsPreviewAppointment(appointment);
        var card = new Border
        {
            Width = 430,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.16
            }
        };

        var body = new StackPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        header.Children.Add(new Border
        {
            Width = 34,
            Height = 34,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 10, 0),
            Child = new PackIcon
            {
                Kind = PackIconKind.Pencil,
                Width = 18,
                Height = 18,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Editar atendimento",
            Foreground = InkBrush,
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"{appointment.CustomerName} | {appointment.Start:HH:mm}",
            Foreground = MutedBrush,
            FontSize = 11.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(titleStack, 1);
        header.Children.Add(titleStack);

        var closeButton = IconOnlyButton(PackIconKind.Close, 28);
        closeButton.Click += (_, _) => CloseAppointmentQuickEditPopup();
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);
        body.Children.Add(header);

        var serviceBox = new ComboBox
        {
            ItemsSource = services,
            SelectedItem = selectedService,
            DisplayMemberPath = nameof(ServiceItem.DisplayName),
            Style = (Style)FindResource("OnboardingComboBox"),
            Height = 36,
            FontSize = 12.5,
            Margin = new Thickness(0, 3, 0, 10)
        };
        body.Children.Add(FieldBlock("Serviço", serviceBox));

        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });

        var datePicker = new DatePicker
        {
            SelectedDate = appointment.Start.Date,
            Height = 36,
            FontSize = 12.5,
            Margin = new Thickness(0, 3, 0, 0)
        };
        row.Children.Add(FieldBlock("Data", datePicker));

        var timeBox = new ComboBox
        {
            ItemsSource = _timeOptions,
            Text = appointment.Start.ToString("HH:mm", Brazil),
            IsEditable = true,
            Style = (Style)FindResource("OnboardingComboBox"),
            Height = 36,
            FontSize = 12.5,
            Margin = new Thickness(0, 3, 0, 0)
        };
        Grid.SetColumn(timeBox, 2);
        row.Children.Add(FieldBlock("Hora", timeBox, 2));

        var durationBox = new ComboBox
        {
            ItemsSource = _durationOptions,
            SelectedItem = _durationOptions.Contains(appointment.DurationMinutes) ? appointment.DurationMinutes : null,
            Text = appointment.DurationMinutes.ToString(Brazil),
            IsEditable = true,
            Style = (Style)FindResource("OnboardingComboBox"),
            Height = 36,
            FontSize = 12.5,
            Margin = new Thickness(0, 3, 0, 0)
        };
        Grid.SetColumn(durationBox, 4);
        row.Children.Add(FieldBlock("Duração", durationBox, 4));
        body.Children.Add(row);

        var priceBox = new TextBox
        {
            Text = appointment.Price.ToString("N2", Brazil),
            Style = (Style)FindResource("AppointmentInputBox"),
            Height = 36,
            FontSize = 12.5,
            Margin = new Thickness(0, 3, 0, 0)
        };
        body.Children.Add(FieldBlock("Valor", priceBox));

        var errorText = new TextBlock
        {
            Foreground = Solid("#DC2626"),
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };
        body.Children.Add(errorText);

        serviceBox.SelectionChanged += (_, _) =>
        {
            if (serviceBox.SelectedItem is not ServiceItem service)
            {
                return;
            }

            if (_durationOptions.Contains(service.DurationMinutes))
            {
                durationBox.SelectedItem = service.DurationMinutes;
            }
            else
            {
                durationBox.Text = service.DurationMinutes.ToString(Brazil);
            }

            priceBox.Text = service.Price.ToString("N2", Brazil);
        };

        var actions = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("GhostButton"),
            Height = 34,
            MinWidth = 84,
            Padding = new Thickness(10, 0, 10, 0)
        };
        cancelButton.Click += (_, _) => CloseAppointmentQuickEditPopup();
        Grid.SetColumn(cancelButton, 1);
        actions.Children.Add(cancelButton);

        var saveButton = new Button
        {
            Content = isPreview ? "Criar" : "Salvar",
            Style = (Style)FindResource("CommandButton"),
            Height = 34,
            MinWidth = 92,
            Padding = new Thickness(12, 0, 12, 0)
        };
        saveButton.Click += (_, _) => SaveAppointmentQuickEdit(
            appointment,
            isPreview,
            serviceBox,
            datePicker,
            timeBox,
            durationBox,
            priceBox,
            errorText);
        Grid.SetColumn(saveButton, 3);
        actions.Children.Add(saveButton);
        body.Children.Add(actions);

        card.Child = body;
        return card;
    }

    private StackPanel FieldBlock(string label, Control field, int gridColumn = 0)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold
        });
        stack.Children.Add(field);
        Grid.SetColumn(stack, gridColumn);
        return stack;
    }

    private Button IconOnlyButton(PackIconKind icon, double size)
    {
        return new Button
        {
            Width = size,
            Height = size,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = WarmSoftBrush,
            Cursor = Cursors.Hand,
            Content = new PackIcon
            {
                Kind = icon,
                Width = 15,
                Height = 15,
                Foreground = MutedBrush
            }
        };
    }

    private void SaveAppointmentQuickEdit(
        Appointment source,
        bool isPreview,
        ComboBox serviceBox,
        DatePicker datePicker,
        ComboBox timeBox,
        ComboBox durationBox,
        TextBox priceBox,
        TextBlock errorText)
    {
        errorText.Visibility = Visibility.Collapsed;

        if (serviceBox.SelectedItem is not ServiceItem service)
        {
            ShowQuickEditError(errorText, "Escolha um serviço.");
            return;
        }

        if (datePicker.SelectedDate is not DateTime date)
        {
            ShowQuickEditError(errorText, "Informe a data.");
            return;
        }

        if (!TryParseTime(timeBox.Text, out var time))
        {
            ShowQuickEditError(errorText, "Informe a hora no formato 08:30.");
            return;
        }

        if (!TryReadQuickDuration(durationBox, out var duration))
        {
            ShowQuickEditError(errorText, "Informe duração válida.");
            return;
        }

        if (!TryParseMoney(priceBox.Text, out var price) || price < 0)
        {
            ShowQuickEditError(errorText, "Informe um valor válido.");
            return;
        }

        var start = date.Date.Add(time);
        var end = start.AddMinutes(duration);
        if (!TryValidateConfiguredBusinessWindow(start, end, out var businessWindowError))
        {
            ShowQuickEditError(errorText, businessWindowError);
            return;
        }

        var resourceName = string.IsNullOrWhiteSpace(source.ResourceName)
            ? service.DefaultResource
            : source.ResourceName;
        var draft = new AppointmentDraft(
            string.IsNullOrWhiteSpace(source.Segment) ? service.Segment : source.Segment,
            source.CustomerName,
            source.CustomerPhone,
            source.CustomerProfile,
            service.Id,
            service.Name,
            source.ProfessionalId,
            source.ProfessionalName,
            resourceName,
            start,
            duration,
            price,
            source.Notes);

        var target = isPreview ? null : source;
        var conflicts = FindConflicts(draft, target?.Id).ToList();
        if (conflicts.Count > 0)
        {
            ShowQuickEditError(errorText, "Já existe atendimento nesse horário.");
            return;
        }

        if (target is null)
        {
            target = new Appointment
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.Now,
                Status = source.Status == AppointmentStatus.Blocked ? AppointmentStatus.Scheduled : source.Status
            };
            _data.Appointments.Add(target);
        }

        ApplyDraft(target, draft, target.Status == AppointmentStatus.Blocked ? AppointmentStatus.Scheduled : target.Status);
        UpsertCustomer(target);
        CloseAppointmentQuickEditPopup();
        SaveAndRefresh(target.Id, $"{target.CustomerName} atualizado para {target.Start:HH:mm}.");
    }

    private static bool TryReadQuickDuration(ComboBox durationBox, out int duration)
    {
        duration = 0;
        if (durationBox.SelectedItem is int selected)
        {
            duration = selected;
            return duration is >= 5 and <= 480;
        }

        return int.TryParse(durationBox.Text, NumberStyles.Integer, Brazil, out duration) &&
               duration is >= 5 and <= 480;
    }

    private static void ShowQuickEditError(TextBlock errorText, string message)
    {
        errorText.Text = message;
        errorText.Visibility = Visibility.Visible;
    }

    private void AttachAppointmentQuickEditOutsideClickHandler()
    {
        DetachAppointmentQuickEditOutsideClickHandler();

        _appointmentQuickEditOutsideClickHandler = (_, args) =>
        {
            if (_appointmentQuickEditPopup?.Child is DependencyObject popupChild &&
                IsDescendantOf(args.OriginalSource as DependencyObject, popupChild))
            {
                return;
            }

            CloseAppointmentQuickEditPopup();
        };

        Dispatcher.BeginInvoke(() =>
        {
            if (_appointmentQuickEditPopup?.IsOpen == true && _appointmentQuickEditOutsideClickHandler is not null)
            {
                PreviewMouseDown += _appointmentQuickEditOutsideClickHandler;
            }
        }, DispatcherPriority.Background);
    }

    private void DetachAppointmentQuickEditOutsideClickHandler()
    {
        if (_appointmentQuickEditOutsideClickHandler is null)
        {
            return;
        }

        PreviewMouseDown -= _appointmentQuickEditOutsideClickHandler;
        _appointmentQuickEditOutsideClickHandler = null;
    }

    private void CloseAppointmentQuickEditPopup()
    {
        DetachAppointmentQuickEditOutsideClickHandler();

        if (_appointmentQuickEditPopup is null)
        {
            return;
        }

        var popup = _appointmentQuickEditPopup;
        _appointmentQuickEditPopup = null;
        popup.IsOpen = false;
    }

    private Grid CreateAppointmentInfoRow(PackIconKind icon, string label, string value)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        row.Children.Add(new Border
        {
            Width = 28,
            Height = 28,
            Background = AccentSoftBrush,
            CornerRadius = new CornerRadius(9),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = new PackIcon
            {
                Kind = icon,
                Width = 15,
                Height = 15,
                Foreground = AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(stack, 1);
        row.Children.Add(stack);

        return row;
    }

    private static string ClientPopupText(Appointment appointment)
    {
        var parts = new List<string> { appointment.CustomerName };
        if (!string.IsNullOrWhiteSpace(appointment.CustomerPhone))
        {
            parts.Add(appointment.CustomerPhone);
        }
        if (!string.IsNullOrWhiteSpace(appointment.CustomerProfile))
        {
            parts.Add(appointment.CustomerProfile);
        }

        return string.Join(" | ", parts);
    }

    private void ScheduleEmptySlot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ScheduleSlot slot })
        {
            return;
        }

        e.Handled = true;
        ClearEditor();

        var segment = slot.Professional.Segments.FirstOrDefault() ?? GetAvailableSegments()[0];
        AppointmentSegmentCombo.SelectedItem = segment;
        UpdateAppointmentOptions(segment);
        AppointmentDatePicker.SelectedDate = slot.Start.Date;
        TimeCombo.Text = slot.Start.ToString("HH:mm", Brazil);
        ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault(item => item.Id == slot.Professional.Id)
                                         ?? _filteredProfessionals.FirstOrDefault();

        if (ServiceCombo.SelectedItem is ServiceItem service)
        {
            SelectResource(service.DefaultResource);
        }

        OpenAppointmentEditorModal();
        ShowStatus($"Novo horário preparado para {slot.Professional.Name} às {slot.Start:HH:mm}.");
    }

    private void AgendaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection || sender is not ListBox list || list.SelectedItem is not AppointmentRow row)
        {
            return;
        }

        _syncingSelection = true;
        if (ReferenceEquals(list, DayAgendaList))
        {
            WeekAgendaList.SelectedItem = null;
        }
        else
        {
            DayAgendaList.SelectedItem = null;
        }
        _syncingSelection = false;

        if (IsPreviewAppointment(row.Appointment))
        {
            list.SelectedItem = null;
            ShowStatus("Este é um agendamento de exemplo para visualizar o layout.");
            return;
        }

        LoadEditor(row.Appointment);
        OpenAppointmentEditorModal();
        ShowStatus($"{row.CustomerName} selecionado.");
    }

    private void DayAgendaList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_selectedAppointment is null)
        {
            return;
        }

        if (_selectedAppointment.Status == AppointmentStatus.Scheduled)
        {
            SetSelectedStatus(AppointmentStatus.Confirmed);
        }
        else if (_selectedAppointment.Status == AppointmentStatus.Confirmed)
        {
            SetSelectedStatus(AppointmentStatus.Waiting);
        }
        else if (_selectedAppointment.Status == AppointmentStatus.Waiting)
        {
            SetSelectedStatus(AppointmentStatus.InService);
        }
    }

    private void AgendaModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string rawTag } || !int.TryParse(rawTag, out var selectedIndex))
        {
            return;
        }

        _agendaModeIndex = Math.Clamp(selectedIndex, 0, 2);
        UpdateAgendaModeButtons();
    }

    private void UpdateAgendaModeButtons()
    {
        if (AgendaBoardModeButton is null ||
            AgendaListModeButton is null ||
            AgendaWeekModeButton is null ||
            ScheduleBoardPane is null ||
            DayAgendaList is null ||
            WeekAgendaPane is null ||
            WeekAgendaList is null)
        {
            return;
        }

        AgendaBoardModeButton.Style = (Style)FindResource(_agendaModeIndex == 0 ? "ScheduleModeButtonActive" : "ScheduleModeButton");
        AgendaListModeButton.Style = (Style)FindResource(_agendaModeIndex == 1 ? "ScheduleModeButtonActive" : "ScheduleModeButton");
        AgendaWeekModeButton.Style = (Style)FindResource(_agendaModeIndex == 2 ? "ScheduleModeButtonActive" : "ScheduleModeButton");

        SetAgendaPaneVisibility(ScheduleBoardPane, _agendaModeIndex == 0);
        SetAgendaPaneVisibility(DayAgendaList, _agendaModeIndex == 1);
        SetAgendaPaneVisibility(WeekAgendaPane, _agendaModeIndex == 2);
        UpdateAgendaEmptyState();
        ResetAgendaWorkspaceScroll();
    }

    private void UpdateAgendaEmptyState()
    {
        if (AgendaListEmptyState is null ||
            AgendaListEmptyTitleText is null ||
            AgendaListEmptyDescriptionText is null)
        {
            return;
        }

        AgendaListEmptyState.Visibility = _agendaModeIndex == 1 && _dayRows.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        var isToday = _selectedDate.Date == DateTime.Today;
        AgendaListEmptyTitleText.Text = isToday
            ? "Nenhum atendimento hoje"
            : $"Nenhum atendimento em {_selectedDate:dd/MM}";
        AgendaListEmptyDescriptionText.Text = isToday
            ? "Sua agenda está livre. Crie o primeiro atendimento do dia."
            : "Não há atendimentos nesta data. Você pode criar um novo horário agora.";
    }

    private void ResetAgendaWorkspaceScroll()
    {
        Dispatcher.BeginInvoke(() => AgendaWorkspaceView.ScrollToTop(), DispatcherPriority.Background);
    }

    private void AgendaWorkspaceView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_agendaModeIndex == 0 && IsDescendantOf(e.OriginalSource as DependencyObject, ScheduleBoardPane))
        {
            return;
        }

        var targetOffset = Math.Clamp(
            AgendaWorkspaceView.VerticalOffset - e.Delta * 0.85,
            0,
            AgendaWorkspaceView.ScrollableHeight);
        AgendaWorkspaceView.ScrollToVerticalOffset(targetOffset);
        e.Handled = true;
    }

    private void ScheduleBoardScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer)
        {
            return;
        }

        var targetOffset = Math.Clamp(
            viewer.VerticalOffset - e.Delta * 0.72,
            0,
            viewer.ScrollableHeight);
        viewer.ScrollToVerticalOffset(targetOffset);
        e.Handled = true;
    }

    private void AgendaWorkspaceView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }

    private static bool IsDescendantOf(DependencyObject? source, DependencyObject? ancestor)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, ancestor))
            {
                return true;
            }

            source = ParentOf(source);
        }

        return false;
    }

    private static T? FindDataContext<T>(DependencyObject? source)
        where T : class
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: T dataContext })
            {
                return dataContext;
            }

            source = ParentOf(source);
        }

        return null;
    }

    private static DependencyObject? ParentOf(DependencyObject source)
    {
        if (source is FrameworkElement { Parent: DependencyObject frameworkParent })
        {
            return frameworkParent;
        }

        if (source is FrameworkContentElement { Parent: DependencyObject contentParent })
        {
            return contentParent;
        }

        try
        {
            return VisualTreeHelper.GetParent(source);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static void SetAgendaPaneVisibility(UIElement element, bool isVisible)
    {
        element.BeginAnimation(OpacityProperty, null);

        if (!isVisible)
        {
            element.Opacity = 0;
            element.Visibility = Visibility.Collapsed;
            return;
        }

        element.Visibility = Visibility.Visible;
        element.Opacity = 0;
        element.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ClearEditor();
        _agendaModeIndex = 0;
        UpdateAgendaModeButtons();
        OpenAppointmentEditorModal();
        ShowStatus("Novo agendamento iniciado.");
    }

    private void ClearEditorButton_Click(object sender, RoutedEventArgs e)
    {
        ClearEditor();
        SetAppointmentEditorStep(0, focusFirst: true);
        ShowStatus("Formulário limpo.");
    }

    private void CopySummaryButton_Click(object sender, RoutedEventArgs e)
    {
        var summary = BuildSummaryText();
        Clipboard.SetText(summary);
        ShowStatus("Resumo do dia copiado para a área de transferência.");
    }

    private void CopyReportButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshReportsPage();
        Clipboard.SetText(BuildReportSummaryText());
        ShowStatus("Relatório copiado para a área de transferência.");
    }

    private void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshReportsPage();
        Clipboard.SetText(BuildReportSummaryText());
        ShowStatus("Relatório exportado como texto para a área de transferência.");
    }

    private void PrintReportButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshReportsPage();

        var document = new FlowDocument
        {
            PagePadding = new Thickness(40),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        document.Blocks.Add(new Paragraph(new Run($"{BusinessDisplayName()} - Relatórios"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        document.Blocks.Add(new Paragraph(new Run($"Período: {ReportsPeriodText.Text}"))
        {
            Foreground = MutedBrush,
            Margin = new Thickness(0, 0, 0, 16)
        });

        AddReportSection(document, "Resumo do período", _reportsMetrics.Select(item => $"{item.Label}: {item.Value} - {item.Hint}"));
        AddReportSection(document, "Leituras rápidas", _reportsInsights.Select(ReportListLine));
        AddReportSection(document, $"Gráfico - {CurrentReportChartOption()}", _activeReportChartRows.Select(item => $"{item.Label}: {item.ValueText}"));
        AddReportSection(document, "Serviços mais realizados", _reportsServices.Select(ReportListLine));
        AddReportSection(document, "Profissionais", _reportsProfessionals.Select(ReportListLine));

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            document.PageHeight = printDialog.PrintableAreaHeight;
            document.PageWidth = printDialog.PrintableAreaWidth;
            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Relatórios - Balcão Livre");
            ShowStatus("Relatório enviado para impressão.");
        }
    }

    private void ReportsServicesScrollLeftButton_Click(object sender, RoutedEventArgs e)
    {
        ScrollReportsServices(-430);
    }

    private void ReportsServicesScrollRightButton_Click(object sender, RoutedEventArgs e)
    {
        ScrollReportsServices(430);
    }

    private void ScrollReportsServices(double delta)
    {
        var nextOffset = Math.Clamp(
            ReportsServicesScrollViewer.HorizontalOffset + delta,
            0,
            ReportsServicesScrollViewer.ScrollableWidth);

        ReportsServicesScrollViewer.ScrollToHorizontalOffset(nextOffset);
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var rows = ApplyFilters(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date))
            .OrderBy(item => item.Start)
            .ToList();

        var document = new FlowDocument
        {
            PagePadding = new Thickness(40),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        document.Blocks.Add(new Paragraph(new Run($"Balcão Livre - {_selectedDate:dddd, dd/MM/yyyy}"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        document.Blocks.Add(new Paragraph(new Run($"{rows.Count} agendamento(s) | filtro: {CurrentSegmentFilter()}"))
        {
            Foreground = MutedBrush,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var table = new Table();
        table.Columns.Add(new TableColumn { Width = new GridLength(90) });
        table.Columns.Add(new TableColumn { Width = new GridLength(180) });
        table.Columns.Add(new TableColumn { Width = new GridLength(190) });
        table.Columns.Add(new TableColumn { Width = new GridLength(150) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });

        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        var header = new TableRow { FontWeight = FontWeights.Bold, Background = AccentSoftBrush };
        AddCell(header, "Hora");
        AddCell(header, "Cliente");
        AddCell(header, "Serviço");
        AddCell(header, "Profissional");
        AddCell(header, "Status");
        group.Rows.Add(header);

        foreach (var appointment in rows)
        {
            var row = new TableRow();
            AddCell(row, $"{appointment.Start:HH:mm}-{appointment.End:HH:mm}");
            AddCell(row, appointment.CustomerName);
            AddCell(row, appointment.ServiceName);
            AddCell(row, appointment.ProfessionalName);
            AddCell(row, StatusLabel(appointment.Status));
            group.Rows.Add(row);
        }

        document.Blocks.Add(table);

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Balcão Livre {_selectedDate:yyyy-MM-dd}");
            ShowStatus("Agenda enviada para impressão.");
        }
    }

    private string BuildSummaryText()
    {
        var builder = new StringBuilder();
        var rows = ApplyFilters(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date))
            .OrderBy(item => item.Start)
            .ToList();

        builder.AppendLine($"Balcão Livre - {_selectedDate:dddd, dd/MM/yyyy}");
        builder.AppendLine($"Filtro: {CurrentSegmentFilter()}");
        builder.AppendLine();

        foreach (var item in rows)
        {
            builder.AppendLine($"{item.Start:HH:mm}-{item.End:HH:mm} | {StatusLabel(item.Status)} | {item.CustomerName} | {item.ServiceName} | {item.ProfessionalName} | {item.ResourceName}");
        }

        if (rows.Count == 0)
        {
            builder.AppendLine("Nenhum agendamento encontrado.");
        }

        return builder.ToString();
    }

    private string BuildReportSummaryText()
    {
        RefreshReportsPage();

        var builder = new StringBuilder();
        builder.AppendLine($"{BusinessDisplayName()} - Relatórios");
        builder.AppendLine($"Período: {ReportsPeriodText.Text}");
        builder.AppendLine();

        builder.AppendLine("Resumo do período");
        foreach (var metric in _reportsMetrics)
        {
            builder.AppendLine($"{metric.Label}: {metric.Value} - {metric.Hint}");
        }

        builder.AppendLine();
        builder.AppendLine("Leituras rápidas");
        foreach (var insight in _reportsInsights)
        {
            builder.AppendLine(ReportListLine(insight));
        }

        builder.AppendLine();
        builder.AppendLine($"Gráfico - {CurrentReportChartOption()}");
        foreach (var row in _activeReportChartRows)
        {
            builder.AppendLine($"{row.Label}: {row.ValueText}");
        }

        builder.AppendLine();
        builder.AppendLine("Serviços mais realizados");
        foreach (var service in _reportsServices)
        {
            builder.AppendLine(ReportListLine(service));
        }

        builder.AppendLine();
        builder.AppendLine("Profissionais");
        foreach (var professional in _reportsProfessionals)
        {
            builder.AppendLine(ReportListLine(professional));
        }

        return builder.ToString();
    }

    private static string ReportListLine(EstablishmentListRow row) =>
        $"{row.Name}: {row.Detail} ({row.BadgeText})";

    private static void AddReportSection(FlowDocument document, string title, IEnumerable<string> lines)
    {
        document.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 6)
        });

        var list = new List
        {
            MarkerStyle = TextMarkerStyle.Disc,
            Margin = new Thickness(16, 0, 0, 8)
        };

        foreach (var line in lines)
        {
            list.ListItems.Add(new ListItem(new Paragraph(new Run(line)))
            {
                Margin = new Thickness(0, 0, 0, 2)
            });
        }

        document.Blocks.Add(list);
    }

    private static void AddCell(TableRow row, string text)
    {
        row.Cells.Add(new TableCell(new Paragraph(new Run(text)))
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(6)
        });
    }

    private DateTime SuggestedStartFor(DateTime requestedDate, int durationMinutes)
    {
        var date = requestedDate.Date < DateTime.Today ? DateTime.Today : requestedDate.Date;
        date = NextConfiguredWorkday(date);
        var workdayStart = date.AddHours(_data.Settings.WorkdayStartHour);
        var workdayEnd = date.AddHours(_data.Settings.WorkdayEndHour);
        var duration = TimeSpan.FromMinutes(Math.Clamp(durationMinutes, 5, 480));
        var candidate = workdayStart;
        if (date == DateTime.Today)
        {
            var now = DateTime.Now;
            candidate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Kind);
            var minutesToAdd = (15 - candidate.Minute % 15) % 15;
            if (minutesToAdd == 0 && (now.Second > 0 || now.Millisecond > 0))
            {
                minutesToAdd = 15;
            }

            candidate = candidate.AddMinutes(minutesToAdd);
        }

        if (candidate < workdayStart)
        {
            candidate = workdayStart;
        }

        if (OverlapsConfiguredBreak(candidate, candidate + duration))
        {
            candidate = date.AddHours(_data.Settings.WorkdayBreakEndHour);
        }

        if (candidate + duration > workdayEnd)
        {
            candidate = NextConfiguredWorkday(date.AddDays(1)).AddHours(_data.Settings.WorkdayStartHour);
        }

        return candidate;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    private static bool Contains(string source, string search) =>
        !string.IsNullOrWhiteSpace(source) &&
        source.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static string StatusLabel(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Agendado",
        AppointmentStatus.Confirmed => "Confirmado",
        AppointmentStatus.Waiting => "Chegou",
        AppointmentStatus.InService => "Em atendimento",
        AppointmentStatus.Done => "Finalizado",
        AppointmentStatus.Cancelled => "Cancelado",
        AppointmentStatus.NoShow => "Faltou",
        AppointmentStatus.Blocked => "Bloqueado",
        _ => status.ToString()
    };

    private static Brush StatusBackground(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => BlueSoftBrush,
        AppointmentStatus.Confirmed => AccentSoftBrush,
        AppointmentStatus.Waiting => YellowSoftBrush,
        AppointmentStatus.InService => WarmSoftBrush,
        AppointmentStatus.Done => Solid("#E6F6E1"),
        AppointmentStatus.Cancelled => RedSoftBrush,
        AppointmentStatus.NoShow => RedSoftBrush,
        AppointmentStatus.Blocked => GraySoftBrush,
        _ => GraySoftBrush
    };

    private static Brush StatusForeground(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Cancelled or AppointmentStatus.NoShow => Solid("#8B1D18"),
        AppointmentStatus.Blocked => MutedBrush,
        _ => InkBrush
    };

    private static Brush AccentFor(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => AccentBrush,
        AppointmentStatus.Confirmed => AccentBrush,
        AppointmentStatus.Waiting => Solid("#B08A1A"),
        AppointmentStatus.InService => Solid("#B96F3A"),
        AppointmentStatus.Done => Solid("#4B8B36"),
        AppointmentStatus.Cancelled => Solid("#9C2C26"),
        AppointmentStatus.NoShow => Solid("#9C2C26"),
        AppointmentStatus.Blocked => Solid("#68746E"),
        _ => AccentBrush
    };

    private static Brush SidebarGradient(VisualTheme theme)
    {
        return Solid(SidebarBackgroundColor(theme));
    }

    private static string SidebarBackgroundColor(VisualTheme theme) =>
        string.IsNullOrWhiteSpace(theme.Id) ? "#171614" : "#FFFFFF";

    private static Brush SidebarHeaderGradient(VisualTheme theme)
    {
        return Solid("#FFFFFF");
    }

    private static Brush SidebarProfileSurface(VisualTheme theme)
    {
        return ThemeUsesDarkSidebar(theme)
            ? Solid("#24211F")
            : Solid("#FFFFFF");
    }

    private static Brush SidebarHoverSurface(VisualTheme theme)
    {
        if (ThemeUsesDarkSidebar(theme))
        {
            return Solid("#282522");
        }

        var accent = (Color)ColorConverter.ConvertFromString(theme.Accent);
        return new SolidColorBrush(Blend(accent, Colors.White, 0.94));
    }

    private static Brush SidebarBorderSurface(VisualTheme theme)
    {
        return ThemeUsesDarkSidebar(theme)
            ? Solid("#302D2A")
            : Solid(theme.Line);
    }

    private static bool IsBarberTheme(string? themeId)
    {
        var lookup = NormalizeTemplateLookup(themeId ?? "");
        return lookup is "BARBERMIDNIGHT" or "BARBEREMERALD" or "BARBERNAVY";
    }

    private static bool ThemeUsesDarkSidebar(VisualTheme theme)
    {
        var background = (Color)ColorConverter.ConvertFromString(SidebarBackgroundColor(theme));
        var luminance = RelativeLuminance(background);
        var whiteContrast = 1.05 / (luminance + 0.05);
        return whiteContrast >= 3.0;
    }

    private static bool ThemeUsesDarkSidebar(string? themeId) =>
        ThemeUsesDarkSidebar(ThemeById(themeId));

    private static double RelativeLuminance(Color color)
    {
        static double Linearize(byte channel)
        {
            var value = channel / 255d;
            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Linearize(color.R)) +
               (0.7152 * Linearize(color.G)) +
               (0.0722 * Linearize(color.B));
    }

    private static bool IsActiveBarberTheme() => IsBarberTheme(ActiveThemeId);

    private static bool IsActiveDarkSidebarTheme() => ThemeUsesDarkSidebar(ActiveThemeId);

    private static bool IsActiveBarberMidnight() =>
        NormalizeTemplateLookup(ActiveThemeId) == "BARBERMIDNIGHT";

    private static Brush SidebarActiveBackground(VisualTheme theme)
    {
        return ThemeUsesDarkSidebar(theme)
            ? Solid(theme.Accent)
            : Solid(theme.AccentSoft);
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            255,
            (byte)Math.Round(from.R + ((to.R - from.R) * amount)),
            (byte)Math.Round(from.G + ((to.G - from.G) * amount)),
            (byte)Math.Round(from.B + ((to.B - from.B) * amount)));
    }

    private static Brush Solid(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }

    private void ShowStatus(string message)
    {
        if (StatusToastBorder is null || StatusToastText is null || StatusToastIcon is null)
        {
            return;
        }

        _statusToastTimer.Stop();

        var normalized = message.ToUpperInvariant();
        var isError = normalized.Contains("NÃO ", StringComparison.Ordinal) ||
                      normalized.Contains("INVÁLID", StringComparison.Ordinal) ||
                      normalized.Contains("ERRO", StringComparison.Ordinal) ||
                      normalized.Contains("CONFLITO", StringComparison.Ordinal) ||
                      normalized.Contains("RECUSOU", StringComparison.Ordinal);
        var isSuccess = normalized.Contains("SALV", StringComparison.Ordinal) ||
                        normalized.Contains("CRIAD", StringComparison.Ordinal) ||
                        normalized.Contains("ATUALIZAD", StringComparison.Ordinal) ||
                        normalized.Contains("REGISTRAD", StringComparison.Ordinal) ||
                        normalized.Contains("ENVIAD", StringComparison.Ordinal) ||
                        normalized.Contains("PREENCHIDO", StringComparison.Ordinal);

        if (isError)
        {
            StatusToastBorder.Background = Solid("#B91C1C");
            StatusToastBorder.BorderBrush = Solid("#40FFFFFF");
            StatusToastIcon.Foreground = Solid("#FFFFFF");
            StatusToastText.Foreground = Solid("#FFFFFF");
        }
        else if (isSuccess)
        {
            var theme = ThemeById(ActiveThemeId);
            var accent = (Color)ColorConverter.ConvertFromString(theme.Accent);
            var tint = Blend(accent, Colors.White, 0.94);

            StatusToastBorder.Background = new SolidColorBrush(
                Color.FromArgb(232, tint.R, tint.G, tint.B));
            StatusToastBorder.BorderBrush = new SolidColorBrush(
                Color.FromArgb(130, accent.R, accent.G, accent.B));
            StatusToastIcon.Foreground = AccentBrush;
            StatusToastText.Foreground = Solid(theme.Ink);
        }
        else
        {
            StatusToastBorder.Background = AccentDarkBrush;
            StatusToastBorder.BorderBrush = Solid("#40FFFFFF");
            StatusToastIcon.Foreground = Solid("#FFFFFF");
            StatusToastText.Foreground = Solid("#FFFFFF");
        }
        StatusToastIcon.Kind = isError
            ? PackIconKind.AlertCircleOutline
            : isSuccess
                ? PackIconKind.CheckCircleOutline
                : PackIconKind.InformationOutline;
        StatusToastText.Text = message;
        StatusToastBorder.Visibility = Visibility.Visible;
        StatusToastBorder.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        _statusToastTimer.Start();
    }

    private void HideStatusToast()
    {
        _statusToastTimer.Stop();
        if (StatusToastBorder is null || StatusToastBorder.Visibility != Visibility.Visible)
        {
            return;
        }

        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(170));
        animation.Completed += (_, _) => StatusToastBorder.Visibility = Visibility.Collapsed;
        StatusToastBorder.BeginAnimation(OpacityProperty, animation);
    }

    public sealed record WhatsAppConversationRow(
        string Title,
        string Phone,
        string Preview,
        string Detail,
        DateTime LastAt,
        int UnreadCount)
    {
        public string UnreadText => UnreadCount > 99 ? "99+" : UnreadCount.ToString(CultureInfo.InvariantCulture);
        public Visibility UnreadVisibility => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public sealed record WhatsAppMessageRow(
        string CustomerName,
        string Phone,
        string Message,
        string Detail,
        string Category,
        string BadgeText,
        Brush BadgeBackground,
        Brush BadgeForeground,
        Brush BubbleBackground,
        Brush BorderBrush);

    private sealed record RegistrationEditorForm(
        string OwnerName,
        string Phone,
        string Email,
        string BusinessName,
        string BusinessSegment,
        string Document,
        string Address);

    private sealed record CustomerEditorForm(
        string Name,
        string Phone,
        string Document,
        string Segment,
        string Profile,
        string Tags,
        string Notes,
        bool AcceptsWhatsApp);

    private sealed record ServiceEditorForm(
        string Segment,
        string Name,
        string Category,
        string Description,
        int DurationMinutes,
        int PreparationMinutes,
        int BufferMinutes,
        decimal Price,
        decimal CommissionPercent,
        string DefaultResource,
        bool IsActive);

    private sealed record ProfessionalEditorForm(
        string Name,
        string Role,
        string Phone,
        string Email,
        string Document,
        decimal CommissionPercent,
        string Segment,
        string Notes,
        bool IsActive);

    private sealed record ResourceEditorForm(
        string Name,
        string Type,
        string Notes);

    private sealed record ProductEditorForm(
        string Name,
        string Category,
        string Sku,
        string Supplier,
        decimal CostPrice,
        decimal Price,
        int StockQuantity,
        int MinimumStock,
        string Notes,
        bool IsActive);

    private sealed record ProductSaleEditorForm(
        ProductItem Product,
        int Quantity,
        string CustomerName,
        string PaymentMethod,
        decimal Discount,
        string Notes,
        string PaymentProvider,
        string PaymentReference,
        string PaymentStatus);

    private sealed record PaymentEditorForm(
        string Description,
        string CustomerName,
        string Category,
        string PaymentMethod,
        string Notes,
        decimal Value,
        string PaymentProvider,
        string PaymentReference,
        string PaymentStatus);

    private sealed record FinanceChartPaymentRow(string Name, string Detail, decimal Value);

    private sealed record ExpenseEditorForm(
        string Description,
        string Category,
        string Supplier,
        string PaymentMethod,
        string Notes,
        decimal Value);

    private sealed record AgendaMercadoPagoPaymentOutcome(
        string Reference,
        string Status);

    public class AgendaMercadoPagoResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
    }

    public class AgendaMercadoPagoClientPayload
    {
        public string EventName { get; set; } = "";
        public string LicenseKey { get; set; } = "";
        public string MachineHash { get; set; } = "";
        public string MachineCode { get; set; } = "";
        public string AppVersion { get; set; } = "";
        public Dictionary<string, object?> Profile { get; set; } = [];
    }

    public sealed class AgendaMercadoPagoConnectResult : AgendaMercadoPagoResult
    {
        public string AuthUrl { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
    }

    public sealed class AgendaMercadoPagoConnectionStatusResult : AgendaMercadoPagoResult
    {
        public bool Connected { get; set; }
        public string Status { get; set; } = "";
        public string SellerUserId { get; set; } = "";
        public string SelectedTerminalId { get; set; } = "";
        public string SelectedTerminalLabel { get; set; } = "";
        public string LastSyncAt { get; set; } = "";
        public string LastError { get; set; } = "";
    }

    public sealed class AgendaMercadoPagoTerminalsResult : AgendaMercadoPagoResult
    {
        public List<AgendaMercadoPagoTerminalDto> Terminals { get; set; } = [];
        public string SelectedTerminalId { get; set; } = "";
        public string SelectedTerminalLabel { get; set; } = "";
    }

    public sealed class AgendaMercadoPagoTerminalDto
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string PosId { get; set; } = "";
        public string StoreId { get; set; } = "";
        public string OperatingMode { get; set; } = "";

        [JsonIgnore]
        public string Display => string.IsNullOrWhiteSpace(Label) ? Id : Label;
    }

    public sealed class AgendaMercadoPagoItemPayload
    {
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public int Quantity { get; set; }
        public string UnitPrice { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public sealed class AgendaMercadoPagoChargePayload : AgendaMercadoPagoClientPayload
    {
        public string Amount { get; set; } = "";
        public string Method { get; set; } = "";
        public string LocalReference { get; set; } = "";
        public string Description { get; set; } = "";
        public string TerminalId { get; set; } = "";
        public List<AgendaMercadoPagoItemPayload> Items { get; set; } = [];
    }

    public sealed class AgendaMercadoPagoChargeResult : AgendaMercadoPagoResult
    {
        public string AttemptId { get; set; } = "";
        public string LocalReference { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
    }

    public sealed class AgendaMercadoPagoPointStatusPayload : AgendaMercadoPagoClientPayload
    {
        public string AttemptId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string LocalReference { get; set; } = "";
    }

    public sealed class AgendaMercadoPagoPointStatusResult : AgendaMercadoPagoResult
    {
        public string AttemptId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
        public bool Paid { get; set; }
    }

    public sealed class WhatsAppEvolutionResult
    {
        public bool Ok { get; init; }
        public bool Pending { get; init; }
        public string Message { get; init; } = "";
        public string State { get; init; } = "";
        public string QrBase64 { get; init; } = "";
        public string OnboardingUrl { get; init; } = "";
        public string ConnectedName { get; set; } = "";
        public string ConnectedPhone { get; set; } = "";

        public static WhatsAppEvolutionResult Success(string message) => new() { Ok = true, Message = message };
        public static WhatsAppEvolutionResult Fail(string message) => new() { Ok = false, Message = message };
    }

    public sealed record WhatsAppEvolutionIncomingMessage(
        string Id,
        string CustomerName,
        string Phone,
        string Text,
        string Jid,
        DateTime When,
        bool FromMe);

    public sealed class AppointmentRow
    {
        public AppointmentRow(Appointment appointment)
        {
            Appointment = appointment;
        }

        public Appointment Appointment { get; }
        public string TimeRange => $"{Appointment.Start:HH:mm} - {Appointment.End:HH:mm}";
        public string DateText => Appointment.Start.ToString("ddd dd/MM", Brazil);
        public string CustomerName => Appointment.CustomerName;
        public string ServiceLine => $"{Appointment.Segment} | {Appointment.ServiceName} | {Appointment.DurationMinutes} min";
        public string ContextLine
        {
            get
            {
                var parts = new[] { Appointment.CustomerPhone, Appointment.CustomerProfile, Appointment.Notes }
                    .Where(part => !string.IsNullOrWhiteSpace(part));
                return string.Join(" | ", parts);
            }
        }

        public string ProfessionalName => Appointment.ProfessionalName;
        public string ResourceLine => string.IsNullOrWhiteSpace(Appointment.ResourceName) ? "Sem sala/box/cadeira" : Appointment.ResourceName;
        public string StatusText => StatusLabel(Appointment.Status);
        public string PriceText => Appointment.Status == AppointmentStatus.Blocked ? "Bloqueio" : Appointment.Price.ToString("C", Brazil);
        public Brush StatusBackground => MainWindow.StatusBackground(Appointment.Status);
        public Brush StatusForeground => MainWindow.StatusForeground(Appointment.Status);
        public Brush AccentBrush => MainWindow.AccentFor(Appointment.Status);
        public Brush OutlineBrush => LineBrush;
    }

    public sealed record WeekSummaryRow(
        string WeekDayName,
        string DayNumber,
        string DateText,
        string CountText,
        string RevenueText,
        string BusyText,
        double Percent,
        Brush AccentBrush,
        Brush BackgroundBrush,
        Brush BorderBrush,
        bool IsSelected);

    public sealed record MetricRow(string Label, string Value, string Hint, Brush Background, PackIconKind Icon, Brush AccentBrush);

    public sealed record HomeMetricRow(string Label, string Value, string Hint, Brush Background, PackIconKind Icon, Brush AccentBrush);

    public sealed record HomeAgendaSummaryRow(string Time, string DurationText, string CustomerName, string ServiceName, string ProfessionalName, string StatusText, Brush StatusBackground, Brush StatusForeground);

    private sealed record HomeScheduleColumn(string Id, string Name, string Detail, string Initials);

    public sealed record HomeServiceRow(string Name, string CountText);

    public sealed record HomeCustomerSummaryRow(string Name, string Detail, string Initials);

    public sealed record HomeAlertRow(string Title, string Detail, PackIconKind Icon, Brush AccentBrush, Brush Background, Brush BorderBrush);

    public sealed record HomeFinanceBarRow(string Label, string ValueText, double Percent);

    public sealed record ReportChartRow(
        string Label,
        decimal Value,
        string ValueText,
        double Percent,
        Brush AccentBrush,
        Brush BackgroundBrush);

    public sealed record EstablishmentMetricRow(
        string Label,
        string Value,
        string Hint,
        Brush Background,
        PackIconKind Icon,
        Brush AccentBrush);

    public sealed record EstablishmentSectionRow(
        string Title,
        string CountText,
        string Description,
        string ActionText,
        PackIconKind Icon,
        Brush AccentBrush,
        Brush IconBackground);

    public sealed record EstablishmentListRow(
        string Name,
        string Detail,
        string BadgeText,
        Brush BadgeBackground,
        Brush BadgeForeground,
        string Id = "",
        PackIconKind Icon = PackIconKind.AccountCircleOutline);

    public sealed record MarketingContactRow(
        string Name,
        string Detail,
        string Phone,
        string BadgeText,
        string MessagePreview,
        Brush BadgeBackground,
        Brush BadgeForeground);

    public sealed record ProfessionalDayRow(
        string Name,
        string SegmentLine,
        string LoadText,
        string CountText,
        double Percent,
        Brush LoadBrush,
        Brush AccentBrush,
        string Initials,
        bool HasLoad);

    public sealed record RecentCustomerRow(string Name, string Detail, string DateText, string Initials);

    private sealed record ScheduleSlot(Professional Professional, DateTime Start);

    private sealed record ServiceTemplate(string Name, int DurationMinutes, decimal Price, string DefaultResource);

    private sealed record ProfessionalTemplate(string Name, string Role);

    private sealed record OnboardingTemplate(
        string Title,
        string Segment,
        string DefaultBusinessName,
        string Description,
        string Example,
        string ClientLabel,
        string ClientDetailLabel,
        string ResourceLabel,
        int StartHour,
        int EndHour,
        IReadOnlyList<string> Resources,
        IReadOnlyList<ServiceTemplate> Services,
        IReadOnlyList<ProfessionalTemplate> Professionals)
    {
        public override string ToString() => Title;

        public const string NailsSegment = "Unha e beleza";
        public const string IntegratedBeautySegment = "Unha e beleza + salão";
        public const string SalonTitle = "Cabelo / salão";

        public static OnboardingTemplate CreateIntegratedBeauty() =>
            new(
                "Unha / beleza + salão",
                IntegratedBeautySegment,
                "Meu studio integrado",
                "Cria uma agenda única para unha, design, cabelo, escova, coloração, lavatório, cadeiras e profissionais do salão.",
                "Cliente: Camila | Alongamento + escova | Mesa 2 / Cadeira 1",
                "Cliente",
                "Preferência / química / alergia / estilo",
                "Mesa, cadeira ou lavatório",
                9,
                20,
                ["Mesa 1", "Mesa 2", "Cadeira 1", "Cadeira 2", "Lavatório", "Coloração"],
                [
                    new("Manicure", 45, 55, "Mesa 1"),
                    new("Pedicure", 45, 60, "Mesa 1"),
                    new("Alongamento de unha", 120, 180, "Mesa 2"),
                    new("Sobrancelha", 30, 45, "Mesa 2"),
                    new("Escova", 45, 70, "Cadeira 1"),
                    new("Corte feminino", 50, 90, "Cadeira 1"),
                    new("Coloração", 120, 240, "Coloração"),
                    new("Hidratação", 60, 120, "Lavatório")
                ],
                [
                    new("Manicure 1", "Manicure"),
                    new("Designer 1", "Designer"),
                    new("Cabeleireiro 1", "Cabeleireiro"),
                    new("Colorista 1", "Colorista")
                ]);

        public static IReadOnlyList<OnboardingTemplate> CreateDefaults() =>
        [
            new(
                "Clínica médica",
                "Clínica médica",
                "Minha clínica",
                "Controla paciente, prontuário, profissional, sala, consulta, retorno, encaixe e chegada.",
                "Paciente: Maria Souza | Prontuário 0321 | Consulta médica | Consultório 1",
                "Paciente",
                "Prontuário / convênio / motivo",
                "Sala ou consultório",
                8,
                18,
                ["Consultório 1", "Consultório 2", "Sala de exames"],
                [
                    new("Consulta médica", 45, 180, "Consultório 1"),
                    new("Retorno", 30, 90, "Consultório 1"),
                    new("Exame simples", 30, 120, "Sala de exames"),
                    new("Encaixe", 20, 80, "Consultório 2")
                ],
                [
                    new("Profissional 1", "Médico"),
                    new("Profissional 2", "Médico")
                ]),
            new(
                "Petshop",
                "Petshop",
                "Meu petshop",
                "Controla tutor, pet, raça, porte, banho, tosa, vacinação, veterinário e baia de espera.",
                "Tutor: João | Pet: Nina, Spitz | Banho e tosa | Tosa 1",
                "Tutor / pet",
                "Raça / porte / observação do pet",
                "Sala, baia ou mesa",
                8,
                19,
                ["Banho 1", "Tosa 1", "Sala veterinária", "Baia de espera"],
                [
                    new("Banho", 60, 70, "Banho 1"),
                    new("Banho e tosa", 90, 110, "Tosa 1"),
                    new("Consulta veterinária", 40, 160, "Sala veterinária"),
                    new("Vacinação", 25, 85, "Sala veterinária")
                ],
                [
                    new("Tosador 1", "Banho e tosa"),
                    new("Veterinário 1", "Veterinário")
                ]),
            new(
                "Mecânica",
                "Mecânica",
                "Minha oficina",
                "Controla cliente, veículo, placa, problema relatado, box, diagnóstico, revisão e entrega.",
                "Cliente: Lucas | Veículo: Onix ABC1D23 | Troca de óleo | Box 1",
                "Cliente / veículo",
                "Placa / modelo / problema",
                "Box ou elevador",
                8,
                18,
                ["Box 1", "Box 2", "Elevador 1", "Diagnóstico"],
                [
                    new("Diagnóstico", 60, 120, "Diagnóstico"),
                    new("Troca de óleo", 45, 90, "Box 1"),
                    new("Revisão completa", 150, 420, "Box 2"),
                    new("Alinhamento", 50, 130, "Elevador 1")
                ],
                [
                    new("Mecânico 1", "Mecânico"),
                    new("Consultor técnico", "Recepção técnica")
                ]),
            new(
                "Barbearia",
                "Cabelo e barbearia",
                "Minha barbearia",
                "Controla cliente, preferência de corte, barbeiro, cadeira, barba, cabelo e combos.",
                "Cliente: André | Degradê baixo | Corte + barba | Cadeira 1",
                "Cliente",
                "Estilo / preferência / observação",
                "Cadeira",
                9,
                20,
                ["Cadeira 1", "Cadeira 2", "Lavatorio"],
                [
                    new("Corte masculino", 35, 45, "Cadeira 1"),
                    new("Barba", 25, 35, "Cadeira 1"),
                    new("Corte + barba", 60, 80, "Cadeira 1"),
                    new("Sobrancelha", 15, 20, "Cadeira 2")
                ],
                [
                    new("Barbeiro 1", "Barbeiro"),
                    new("Barbeiro 2", "Barbeiro")
                ]),
            new(
                SalonTitle,
                "Cabelo e barbearia",
                "Meu salão",
                "Controla cliente, histórico, química, cadeira, lavatório, escova, coloração e tratamentos.",
                "Cliente: Patrícia | Coloração sem amônia | Colorista 1 | Cadeira 2",
                "Cliente",
                "Preferência / química / histórico",
                "Cadeira ou lavatório",
                9,
                20,
                ["Cadeira 1", "Cadeira 2", "Lavatório", "Coloração"],
                [
                    new("Escova", 45, 70, "Cadeira 1"),
                    new("Corte feminino", 50, 90, "Cadeira 1"),
                    new("Coloração", 120, 240, "Coloração"),
                    new("Hidratação", 60, 120, "Lavatório")
                ],
                [
                    new("Cabeleireiro 1", "Cabeleireiro"),
                    new("Colorista 1", "Colorista")
                ]),
            new(
                "Unha / beleza",
                "Unha e beleza",
                "Meu studio de beleza",
                "Controla cliente, preferência, alergias, mesa, manicure, pedicure, alongamento e design.",
                "Cliente: Camila | Alongamento almond | Mesa 2",
                "Cliente",
                "Preferência / alergia / estilo",
                "Mesa ou cadeira",
                9,
                20,
                ["Mesa 1", "Mesa 2", "Cadeira beleza"],
                [
                    new("Manicure", 45, 55, "Mesa 1"),
                    new("Pedicure", 45, 60, "Mesa 1"),
                    new("Alongamento de unha", 120, 180, "Mesa 2"),
                    new("Sobrancelha", 30, 45, "Cadeira beleza")
                ],
                [
                    new("Manicure 1", "Manicure"),
                    new("Designer 1", "Designer")
                ])
        ];
    }

    private sealed record ViaCepAddress(
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("localidade")] string? Localidade,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] bool Erro);

    private sealed record AppointmentDraft(
        string Segment,
        string CustomerName,
        string Phone,
        string Profile,
        string ServiceId,
        string ServiceName,
        string ProfessionalId,
        string ProfessionalName,
        string ResourceName,
        DateTime Start,
        int DurationMinutes,
        decimal Price,
        string Notes)
    {
        public static AppointmentDraft From(Appointment appointment) =>
            new(
                appointment.Segment,
                appointment.CustomerName,
                appointment.CustomerPhone,
                appointment.CustomerProfile,
                appointment.ServiceId,
                appointment.ServiceName,
                appointment.ProfessionalId,
                appointment.ProfessionalName,
                appointment.ResourceName,
                appointment.Start,
                appointment.DurationMinutes,
                appointment.Price,
                appointment.Notes);
    }
}
