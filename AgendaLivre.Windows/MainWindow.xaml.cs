using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow : Window
{
    private const string AllSegments = "Todos";
    private const string ReportChartAppointments = "Agendamentos por dia";
    private const string ReportChartRevenue = "Receita por dia";
    private const string ReportChartStatus = "Status dos atendimentos";
    private const double ScheduleTimeColumnWidth = 78;
    private const double ScheduleProfessionalColumnWidth = 240;
    private const string WhatsAppEvolutionDefaultBaseUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/evolution-proxy";
    private const string WhatsAppEvolutionLicenseSecret = "BalcaoLivrePDV-local-license-v1";
    private const string WhatsAppEvolutionLicenseExpires = "203512312359";
    private const string WhatsAppEvolutionLicenseScope = "AGENDALIVRE";
    private const string DefaultMercadoPagoPaymentsApiUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/payments";
    private const string MercadoPagoCreditMethod = "Mercado Pago - crédito na maquininha";
    private const string MercadoPagoDebitMethod = "Mercado Pago - débito na maquininha";
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly Brush AccentBrush = Solid("#2563EB");
    private static readonly Brush AccentSoftBrush = Solid("#EAF1FF");
    private static readonly Brush WarmSoftBrush = Solid("#DBEAFE");
    private static readonly Brush RedSoftBrush = Solid("#FEE2E2");
    private static readonly Brush BlueSoftBrush = Solid("#EAF1FF");
    private static readonly Brush YellowSoftBrush = Solid("#FFF6D8");
    private static readonly Brush GraySoftBrush = Solid("#EEF2F6");
    private static readonly Brush InkBrush = Solid("#172033");
    private static readonly Brush MutedBrush = Solid("#68758A");
    private static readonly Brush LineBrush = Solid("#E2E8F0");
    private static readonly Brush SidebarTextBrush = Solid("#EFF6FF");
    private static readonly Brush SidebarActiveTextBrush = Solid("#0F172A");
    private static readonly Brush[] ReportPalette =
    [
        AccentBrush,
        Solid("#38BDF8"),
        Solid("#0F172A"),
        Solid("#8B5CF6"),
        Solid("#F59E0B"),
        Solid("#10B981"),
        Solid("#EF4444")
    ];
    private static readonly Brush[] ReportSoftPalette =
    [
        AccentSoftBrush,
        Solid("#E0F2FE"),
        Solid("#EEF2F6"),
        Solid("#F3E8FF"),
        Solid("#FEF3C7"),
        Solid("#DCFCE7"),
        RedSoftBrush
    ];

    private readonly AgendaDataStore _store = new();
    private readonly ObservableCollection<AppointmentRow> _dayRows = [];
    private readonly ObservableCollection<AppointmentRow> _weekRows = [];
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
    private readonly ObservableCollection<EstablishmentMetricRow> _financeMetrics = [];
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
    private readonly ObservableCollection<WhatsAppConversationRow> _whatsAppConversations = [];
    private readonly ObservableCollection<WhatsAppMessageRow> _whatsAppMessages = [];
    private readonly DispatcherTimer _whatsAppPollTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _whatsAppPollRunning;
    private string _selectedWhatsAppReplyPhone = "";
    private string _selectedWhatsAppReplyName = "";
    private bool _whatsAppConversationOpen;
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
        "Senha de acesso"
    ];
    private static readonly string[] OnboardingStepCaptions =
    [
        "Identifique o responsável e o nome que aparecerá no sistema.",
        "Escolha o setor para carregar serviços e recursos mais próximos da sua rotina.",
        "Informe quantas pessoas atendem para preparar a agenda do tamanho certo.",
        "Marque a prioridade inicial para a configuração nascer alinhada ao seu uso.",
        "Cadastre onde o negócio funciona para consultas e relatórios.",
        "Defina a senha que libera o acesso ao sistema."
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
    private string _selectedSegmentFilter = AllSegments;
    private string _selectedProfessionalCount = "";
    private string _selectedObjective = "";
    private int _onboardingStep;
    private bool _loadingEditor;
    private bool _syncingSelection;
    private bool _sidebarCollapsed;
    private bool _configuringReportChart;
    private Appointment? _homeNextAppointment;

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

    public MainWindow()
    {
        InitializeComponent();

        _whatsAppPollTimer.Tick += async (_, _) => await PollWhatsAppEvolutionMessagesAsync();

        _data = _store.LoadOrCreate();
        ConfigureOnboardingInputs();
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
        UpdateWhatsAppPollingState();
        ApplyBusinessLabels();
        ShowMainPage(MainPage.Home);

        DataPathText.Text = _store.DataPath;
        ShowStatus($"Agenda pronta. Dados salvos localmente em {_store.DataPath}");

        if (NeedsOnboarding())
        {
            ShowOnboarding();
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
        FinanceMetricsItemsControl.ItemsSource = _financeMetrics;
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
        _reportChartOptions.Add(ReportChartRevenue);
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

        InitialFullNameTextBox.Text = _data.Settings.AccountFullName;
        InitialPhoneTextBox.Text = string.IsNullOrWhiteSpace(_data.Settings.AccountPhone)
            ? _data.Settings.BusinessPhone
            : _data.Settings.AccountPhone;
        InitialEmailTextBox.Text = _data.Settings.AccountEmail;
        InitialBusinessNameTextBox.Text = IsDefaultBusinessName(_data.Settings.BusinessName)
            ? ""
            : _data.Settings.BusinessName;
        OnboardingCepTextBox.Text = _data.Settings.PostalCode;
        OnboardingNeighborhoodTextBox.Text = _data.Settings.Neighborhood;
        OnboardingStreetTextBox.Text = _data.Settings.Street;
        OnboardingAddressNumberTextBox.Text = _data.Settings.AddressNumber;
        OnboardingAddressComplementTextBox.Text = _data.Settings.AddressComplement;
        OnboardingPasswordBox.Clear();

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
        OnboardingOverlay.Visibility = Visibility.Visible;
        RefreshWhatsAppLauncherVisibility();
        ShowOnboardingStep(0);
        InitialFullNameTextBox.Focus();
        ShowStatus("Informe os dados iniciais para criar sua agenda.");
    }

    private void ContinueInitialDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCaptureInitialData())
        {
            return;
        }

        ShowOnboardingStep(1);
        SegmentSalonButton.Focus();
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
        ShowStatus($"Selecionado: {_selectedOnboardingTemplate.Title}. Clique em Continuar para confirmar.");
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
        OnboardingPasswordBox.Focus();
    }

    private void FinishCreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var password = OnboardingPasswordBox.Password.Trim();
        if (password.Length < 4)
        {
            ShowStatus("Defina uma senha com pelo menos 4 caracteres.");
            OnboardingPasswordBox.Focus();
            return;
        }

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
            template.EndHour);

        _data.Settings.AccountFullName = InitialFullNameTextBox.Text.Trim();
        _data.Settings.AccountPhone = businessPhone;
        _data.Settings.AccountEmail = InitialEmailTextBox.Text.Trim();
        _data.Settings.ProfessionalCountRange = professionalCount;
        _data.Settings.MainObjective = objective;
        _data.Settings.PostalCode = OnboardingCepTextBox.Text.Trim();
        _data.Settings.Neighborhood = OnboardingNeighborhoodTextBox.Text.Trim();
        _data.Settings.Street = OnboardingStreetTextBox.Text.Trim();
        _data.Settings.AddressNumber = OnboardingAddressNumberTextBox.Text.Trim();
        _data.Settings.AddressComplement = OnboardingAddressComplementTextBox.Text.Trim();
        _data.Settings.AccountPasswordHash = HashPassword(password);
        _data.Settings.AccountCreatedAt = DateTime.Now;
        _store.Save(_data);

        OnboardingOverlay.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
        ApplyBusinessLabels();
        ShowMainPage(MainPage.Home);
        ShowStatus($"Conta criada para {template.Title}. A agenda está pronta para uso.");
    }

    private void OnboardingBackButton_Click(object sender, RoutedEventArgs e)
    {
        ShowOnboardingStep(Math.Max(0, _onboardingStep - 1));
    }

    private void ShowOnboardingStep(int step)
    {
        _onboardingStep = Math.Clamp(step, 0, 5);

        InitialDataStepPanel.Visibility = _onboardingStep == 0 ? Visibility.Visible : Visibility.Collapsed;
        BusinessSegmentStepPanel.Visibility = _onboardingStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        ProfessionalCountStepPanel.Visibility = _onboardingStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        ObjectiveStepPanel.Visibility = _onboardingStep == 3 ? Visibility.Visible : Visibility.Collapsed;
        AddressStepPanel.Visibility = _onboardingStep == 4 ? Visibility.Visible : Visibility.Collapsed;
        PasswordStepPanel.Visibility = _onboardingStep == 5 ? Visibility.Visible : Visibility.Collapsed;
        OnboardingHeaderRow.Height = _onboardingStep == 0 ? new GridLength(0) : new GridLength(64);
        OnboardingTopBar.Visibility = _onboardingStep == 0 ? Visibility.Collapsed : Visibility.Visible;
        OnboardingBackButton.Visibility = _onboardingStep == 0 ? Visibility.Hidden : Visibility.Visible;
        OnboardingProgressText.Text = $"{_onboardingStep + 1}/6";
        OnboardingSidebarProgressText.Text = $"Etapa {_onboardingStep + 1} de 6";
        OnboardingSidebarTitleText.Text = OnboardingStepTitles[_onboardingStep];
        OnboardingSidebarCaptionText.Text = OnboardingStepCaptions[_onboardingStep];

        UpdateWizardChoiceStates();
    }

    private void UpdateWizardChoiceStates()
    {
        UpdateWizardChoiceState(BusinessSegmentStepPanel, _selectedOnboardingTemplate?.Title);
        UpdateWizardChoiceState(ProfessionalCountStepPanel, _selectedProfessionalCount);
        UpdateWizardChoiceState(ObjectiveStepPanel, _selectedObjective);

        SetWizardContinueState(ContinueInitialDataButton, true);
        SetWizardContinueState(ContinueBusinessTypeButton, _selectedOnboardingTemplate is not null);
        SetWizardContinueState(ContinueProfessionalCountButton, !string.IsNullOrWhiteSpace(_selectedProfessionalCount));
        SetWizardContinueState(ContinueObjectiveButton, !string.IsNullOrWhiteSpace(_selectedObjective));
    }

    private static void SetWizardContinueState(Button button, bool enabled)
    {
        button.IsEnabled = enabled;
        button.Background = Solid(enabled ? "#2563EB" : "#F8FAFC");
        button.BorderBrush = Solid(enabled ? "#2563EB" : "#DCE4F0");
        button.Foreground = Solid(enabled ? "#FFFFFF" : "#94A3B8");
        TextElement.SetForeground(button, Solid(enabled ? "#FFFFFF" : "#94A3B8"));
        button.Cursor = enabled ? Cursors.Hand : Cursors.Arrow;
    }

    private static void UpdateWizardChoiceState(DependencyObject root, string? selectedTag)
    {
        foreach (var button in FindVisualChildren<Button>(root))
        {
            var isSelected = button.Tag is string tag &&
                             !string.IsNullOrWhiteSpace(selectedTag) &&
                             tag.Equals(selectedTag, StringComparison.OrdinalIgnoreCase);
            button.Background = Solid(isSelected ? "#EFF6FF" : "#FFFFFF");
            button.BorderBrush = Solid(isSelected ? "#2563EB" : "#DCE4F0");
            button.Foreground = Solid(isSelected ? "#1D4ED8" : "#626A73");
            button.BorderThickness = new Thickness(isSelected ? 2 : 1);
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
        _onboardingTemplates.First(template => template.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

    private OnboardingTemplate TemplateBySegment(string segment) =>
        _onboardingTemplates.First(template => template.Segment.Equals(segment, StringComparison.OrdinalIgnoreCase));

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

    private static string HashPassword(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

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

    private void ApplyOnboardingTemplate(OnboardingTemplate template, string businessName, string businessDocument, string businessPhone, string businessAddress, int startHour, int endHour)
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
        foreach (var professional in template.Professionals)
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

    private void ApplyBusinessLabels()
    {
        AppTitleText.Text = BusinessDisplayName();

        if (string.IsNullOrWhiteSpace(_data.Settings.BusinessSegment))
        {
            AppSubtitleText.Text = "Atendimento, agenda e gestão em um só lugar";
            AppSubtitleText.ToolTip = null;
        }
        else
        {
            var documentPart = string.IsNullOrWhiteSpace(_data.Settings.BusinessDocument)
                ? ""
                : $" | {_data.Settings.BusinessDocument}";
            AppSubtitleText.Text = $"{_data.Settings.BusinessSegment}{documentPart}";
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
        ExportWhatsAppAgendaSnapshot();

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            ReselectAppointment(selectedId);
        }
    }

    private void RefreshDayRows()
    {
        _dayRows.Clear();

        foreach (var appointment in ApplyFilters(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date))
                     .OrderBy(item => item.Start)
                     .ThenBy(item => item.CustomerName))
        {
            _dayRows.Add(new AppointmentRow(appointment));
        }

        BuildScheduleBoard();
    }

    private void RefreshWeekRows()
    {
        _weekRows.Clear();
        var startOfWeek = StartOfWeek(_selectedDate);
        var endOfWeek = startOfWeek.AddDays(7);

        foreach (var appointment in ApplyFilters(_data.Appointments.Where(item => item.Start >= startOfWeek && item.Start < endOfWeek))
                     .OrderBy(item => item.Start)
                     .ThenBy(item => item.CustomerName))
        {
            _weekRows.Add(new AppointmentRow(appointment));
        }
    }

    private void RefreshMetrics()
    {
        _metrics.Clear();

        var dayAppointments = ApplySegmentFilter(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date)).ToList();
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

        _metrics.Add(new MetricRow("Agendados", active.Count.ToString(Brazil), "em aberto no dia", AccentSoftBrush));
        _metrics.Add(new MetricRow("Confirmados", confirmed.ToString(Brazil), "inclui chegada e atendimento", BlueSoftBrush));
        _metrics.Add(new MetricRow("Livres", freeSlots.ToString(Brazil), "janelas estimadas de 30 min", GraySoftBrush));
        _metrics.Add(new MetricRow("Receita", forecast.ToString("C0", Brazil), $"{done} finalizado(s) | {late} atraso(s)", WarmSoftBrush));
    }

    private void RefreshProfessionals()
    {
        _professionalRows.Clear();

        var dayAppointments = ApplySegmentFilter(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date)).ToList();

        foreach (var professional in GetProfessionalsForCurrentFilter().OrderBy(item => item.Name))
        {
            var professionalAppointments = dayAppointments
                .Where(item => item.ProfessionalId == professional.Id && IsOperationalStatus(item))
                .ToList();

            var minutes = professionalAppointments.Sum(item => item.DurationMinutes);
            var brush = minutes >= 300 ? RedSoftBrush : minutes >= 180 ? YellowSoftBrush : AccentSoftBrush;

            _professionalRows.Add(new ProfessionalDayRow(
                professional.Name,
                professional.SegmentLine,
                $"{professionalAppointments.Count} ag.",
                brush));
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
            var detailParts = new[] { customer.Segment, customer.Phone, customer.Profile }
                .Where(part => !string.IsNullOrWhiteSpace(part));
            _recentCustomers.Add(new RecentCustomerRow(customer.Name, string.Join(" | ", detailParts)));
        }
    }

    private void RefreshTitles()
    {
        var segment = CurrentSegmentFilter();
        var dateText = _selectedDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", Brazil);
        SelectedDateTitleText.Text = dateText;
        AgendaTitleText.Text = segment == AllSegments ? "Agenda geral" : $"Agenda - {segment}";
        AgendaSubtitleText.Text = $"{dateText} | {_dayRows.Count} item(ns) visíveis";
    }

    private void RefreshHomeDashboard()
    {
        var today = DateTime.Today;
        var now = DateTime.Now;
        var dayAppointments = _data.Appointments
            .Where(item => item.Start.Date == today)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.CustomerName)
            .ToList();
        var operational = dayAppointments.Where(IsOperationalStatus).ToList();
        var confirmed = dayAppointments.Count(item => item.Status is AppointmentStatus.Confirmed or AppointmentStatus.Waiting or AppointmentStatus.InService);
        var pending = dayAppointments.Count(item => item.Status == AppointmentStatus.Scheduled);
        var noShows = dayAppointments.Count(item => item.Status == AppointmentStatus.NoShow);
        var done = dayAppointments.Count(item => item.Status == AppointmentStatus.Done);
        var forecast = dayAppointments
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Sum(item => item.Price);
        var realizedToday = dayAppointments
            .Where(item => item.Status == AppointmentStatus.Done)
            .Sum(item => item.Price);

        var professionalCount = Math.Max(1, _data.Professionals.Count);
        var workdayMinutes = Math.Max(60, (_data.Settings.WorkdayEndHour - _data.Settings.WorkdayStartHour) * 60);
        var busyMinutes = operational.Sum(item => Math.Max(15, item.DurationMinutes));
        var freeSlots = Math.Max(0, ((workdayMinutes * professionalCount) - busyMinutes) / 30);

        HomeGreetingText.Text = $"{GreetingFor(now)}, {FirstName(_data.Settings.AccountFullName)}";
        HomeDateText.Text = today.ToString("dddd, dd 'de' MMMM 'de' yyyy", Brazil);
        HomeBusinessText.Text = string.IsNullOrWhiteSpace(_data.Settings.BusinessName)
            ? "Balcão Livre"
            : _data.Settings.BusinessName;

        _homeMetrics.Clear();
        _homeMetrics.Add(new HomeMetricRow("Agendamentos de hoje", dayAppointments.Count.ToString(Brazil), "total na operação", AccentSoftBrush));
        _homeMetrics.Add(new HomeMetricRow("Confirmados", confirmed.ToString(Brazil), "inclui chegada e atendimento", BlueSoftBrush));
        _homeMetrics.Add(new HomeMetricRow("Pendentes", pending.ToString(Brazil), "aguardando confirmação", GraySoftBrush));
        _homeMetrics.Add(new HomeMetricRow("Faltas", noShows.ToString(Brazil), "clientes ausentes", RedSoftBrush));
        _homeMetrics.Add(new HomeMetricRow("Horários livres", freeSlots.ToString(Brazil), "janelas estimadas de 30 min", GraySoftBrush));
        _homeMetrics.Add(new HomeMetricRow("Receita prevista", forecast.ToString("C0", Brazil), "valor esperado no dia", WarmSoftBrush));
        _homeMetrics.Add(new HomeMetricRow("Receita realizada", realizedToday.ToString("C0", Brazil), $"{done} finalizado(s)", AccentSoftBrush));

        _homeNextAppointment = dayAppointments.FirstOrDefault(item =>
            item.Start >= now &&
            item.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.Waiting or AppointmentStatus.InService);
        RefreshHomeNextAppointment();
        RefreshHomeAgendaRows(dayAppointments, now);
        RefreshHomeFinance(today, forecast, realizedToday);
        RefreshHomeGoals(realizedToday);
        RefreshHomeAlerts(dayAppointments, pending);
        RefreshHomeTopServices();
        RefreshHomeCustomers();
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
        var nextRows = dayAppointments
            .Where(item => item.Start >= now || item.Status is AppointmentStatus.Waiting or AppointmentStatus.InService)
            .Where(item => item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .Take(6)
            .ToList();

        if (nextRows.Count == 0)
        {
            _homeAgendaRows.Add(new HomeAgendaSummaryRow("--:--", "Sem próximos atendimentos", "Agenda livre", "Livre", GraySoftBrush, MutedBrush));
            return;
        }

        foreach (var appointment in nextRows)
        {
            _homeAgendaRows.Add(new HomeAgendaSummaryRow(
                appointment.Start.ToString("HH:mm", Brazil),
                appointment.CustomerName,
                appointment.ServiceName,
                StatusLabel(appointment.Status),
                StatusBackground(appointment.Status),
                StatusForeground(appointment.Status)));
        }
    }

    private void RefreshHomeFinance(DateTime today, decimal forecast, decimal realizedToday)
    {
        var weekStart = StartOfWeek(today);
        var weekEnd = weekStart.AddDays(7);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var realizedWeek = SumRealizedRevenue(weekStart, weekEnd);
        var realizedMonth = SumRealizedRevenue(monthStart, monthEnd);
        HomeRevenueDayText.Text = realizedToday.ToString("C0", Brazil);
        HomeRevenueWeekText.Text = realizedWeek.ToString("C0", Brazil);
        HomeRevenueMonthText.Text = realizedMonth.ToString("C0", Brazil);
        HomeFinancialSubtitleText.Text = $"Previsto hoje: {forecast.ToString("C0", Brazil)}";

        var max = Math.Max(1m, Math.Max(realizedToday, Math.Max(realizedWeek, realizedMonth)));
        _homeFinanceBars.Clear();
        _homeFinanceBars.Add(new HomeFinanceBarRow("Faturamento do dia", realizedToday.ToString("C0", Brazil), Percent(realizedToday, max)));
        _homeFinanceBars.Add(new HomeFinanceBarRow("Faturamento da semana", realizedWeek.ToString("C0", Brazil), Percent(realizedWeek, max)));
        _homeFinanceBars.Add(new HomeFinanceBarRow("Faturamento do mês", realizedMonth.ToString("C0", Brazil), Percent(realizedMonth, max)));
    }

    private void RefreshHomeGoals(decimal realizedToday)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var realizedMonth = SumRealizedRevenue(monthStart, monthEnd);
        var dailyGoal = Math.Max(500m, realizedToday == 0 ? 500m : Math.Ceiling(realizedToday * 1.25m / 50m) * 50m);
        var monthlyGoal = dailyGoal * 22m;

        HomeDailyGoalText.Text = $"{realizedToday.ToString("C0", Brazil)} / {dailyGoal.ToString("C0", Brazil)}";
        HomeMonthlyGoalText.Text = $"{realizedMonth.ToString("C0", Brazil)} / {monthlyGoal.ToString("C0", Brazil)}";
        HomeDailyGoalProgress.Value = Percent(realizedToday, dailyGoal);
        HomeMonthlyGoalProgress.Value = Percent(realizedMonth, monthlyGoal);
        HomeGoalSubtitleText.Text = $"Realizado no mês: {realizedMonth.ToString("C0", Brazil)}";
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
            Solid("#1D4ED8"),
            Solid("#EFF6FF"),
            Solid("#BFDBFE")));
        _homeAlerts.Add(new HomeAlertRow(
            "Clientes sem retorno",
            $"{staleCustomers} cliente(s) sem atendimento há mais de 30 dias.",
            PackIconKind.AccountClock,
            Solid("#7C3AED"),
            Solid("#F5F3FF"),
            Solid("#DDD6FE")));
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
            _homeRecentCustomers.Add(new HomeCustomerSummaryRow(customer.Name, detail));
        }

        if (_homeRecentCustomers.Count == 0)
        {
            _homeRecentCustomers.Add(new HomeCustomerSummaryRow("Nenhum cliente recente", "Os próximos atendimentos aparecerão aqui."));
        }
    }

    private void RefreshEstablishmentPage()
    {
        EstablishmentBusinessText.Text = BusinessDisplayName();

        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var productSalesThisMonth = _data.ProductSales
            .Where(item => item.SoldAt >= monthStart && item.SoldAt < nextMonth)
            .ToList();
        var productRevenueThisMonth = productSalesThisMonth.Sum(item => item.Total);
        var productsInStock = _data.Products.Sum(item => Math.Max(0, item.StockQuantity));

        _establishmentMetrics.Clear();
        _establishmentMetrics.Add(new EstablishmentMetricRow("Clientes", _data.Customers.Count.ToString(Brazil), "cadastros ativos", AccentSoftBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Profissionais", _data.Professionals.Count.ToString(Brazil), "equipe cadastrada", BlueSoftBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Serviços", _data.Services.Count.ToString(Brazil), "itens no catálogo", GraySoftBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Produtos", _data.Products.Count.ToString(Brazil), $"{productsInStock} em estoque", AccentSoftBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Vendas do mês", productSalesThisMonth.Count.ToString(Brazil), "produtos vendidos", WarmSoftBrush));
        _establishmentMetrics.Add(new EstablishmentMetricRow("Receita produtos", productRevenueThisMonth.ToString("C0", Brazil), "faturamento no mês", BlueSoftBrush));

        _establishmentSections.Clear();
        _establishmentSections.Add(new EstablishmentSectionRow("Clientes", $"{_data.Customers.Count} cadastrado(s)", "Acesse a base completa de clientes e acompanhe o último atendimento.", "Gerenciar", PackIconKind.AccountGroup, AccentBrush, AccentSoftBrush));
        _establishmentSections.Add(new EstablishmentSectionRow("Profissionais", $"{_data.Professionals.Count} cadastrado(s)", "Controle equipe, funções e vínculo com os segmentos do negócio.", "Gerenciar", PackIconKind.AccountTie, AccentBrush, AccentSoftBrush));
        _establishmentSections.Add(new EstablishmentSectionRow("Serviços", $"{_data.Services.Count} cadastrado(s)", "Organize serviços, duração, preço e recurso padrão.", "Gerenciar", PackIconKind.ClipboardText, AccentBrush, AccentSoftBrush));
        _establishmentSections.Add(new EstablishmentSectionRow("Produtos", $"{_data.Products.Count} cadastrado(s)", "Cadastre itens de estoque para venda no balcão.", "Gerenciar", PackIconKind.PackageVariant, AccentBrush, AccentSoftBrush));
        _establishmentSections.Add(new EstablishmentSectionRow("Venda de produtos", $"{productSalesThisMonth.Count} no mês", "Acompanhe vendas, quantidade e receita de produtos.", "Registrar", PackIconKind.Cart, AccentBrush, AccentSoftBrush));

        RefreshEstablishmentClients();
        RefreshEstablishmentProfessionals();
        RefreshEstablishmentServices();
        RefreshEstablishmentProducts();
        RefreshEstablishmentSales();
    }

    private void RefreshEstablishmentClients()
    {
        _establishmentClients.Clear();
        foreach (var customer in _data.Customers.OrderBy(item => item.Name))
        {
            var detailParts = new[] { customer.Phone, customer.Email, customer.Tags, customer.Profile, customer.Segment }
                .Where(part => !string.IsNullOrWhiteSpace(part));
            _establishmentClients.Add(new EstablishmentListRow(
                customer.Name,
                string.Join(" | ", detailParts.DefaultIfEmpty("Sem detalhes cadastrados")),
                customer.LastSeenAt == DateTime.MinValue ? "novo" : customer.LastSeenAt.ToString("dd/MM", Brazil),
                AccentSoftBrush,
                AccentBrush));
        }

        if (_establishmentClients.Count == 0)
        {
            _establishmentClients.Add(EmptyEstablishmentRow("Nenhum cliente cadastrado", "Os clientes criados nos agendamentos aparecerão aqui.", "0"));
        }
    }

    private void RefreshEstablishmentProfessionals()
    {
        _establishmentProfessionals.Clear();
        foreach (var professional in _data.Professionals.OrderBy(item => item.Name))
        {
            _establishmentProfessionals.Add(new EstablishmentListRow(
                professional.Name,
                professional.SegmentLine,
                string.IsNullOrWhiteSpace(professional.Role) ? "equipe" : professional.Role,
                AccentSoftBrush,
                AccentBrush));
        }

        if (_establishmentProfessionals.Count == 0)
        {
            _establishmentProfessionals.Add(EmptyEstablishmentRow("Nenhum profissional cadastrado", "Cadastre a equipe para montar a agenda.", "0"));
        }
    }

    private void RefreshEstablishmentServices()
    {
        _establishmentServices.Clear();
        foreach (var service in _data.Services.OrderBy(item => item.Name))
        {
            var detailParts = new[]
            {
                service.Segment,
                service.Category,
                $"{service.DurationMinutes} min",
                service.Price > 0 ? service.Price.ToString("C", Brazil) : "",
                service.DefaultResource
            }.Where(part => !string.IsNullOrWhiteSpace(part));

            _establishmentServices.Add(new EstablishmentListRow(
                service.Name,
                string.Join(" | ", detailParts),
                service.Price.ToString("C0", Brazil),
                AccentSoftBrush,
                AccentBrush));
        }

        if (_establishmentServices.Count == 0)
        {
            _establishmentServices.Add(EmptyEstablishmentRow("Nenhum serviço cadastrado", "Crie serviços para montar os agendamentos.", "0"));
        }
    }

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
        var monthExpenseCount = _data.Expenses.Count(item => item.Date >= monthStart && item.Date < nextMonth);
        var monthBalance = receivedMonth - monthExpenses;
        var pendingLabel = pending.Count == 1 ? "1 atendimento em aberto" : $"{pending.Count} atendimentos em aberto";

        FinanceBalanceText.Text = monthBalance.ToString("C0", Brazil);
        FinanceBalanceHintText.Text = monthBalance >= 0
            ? "Mes fechando acima das despesas"
            : "Despesas acima do dinheiro recebido";
        FinanceBalanceBadgeText.Text = monthBalance >= 0 ? "positivo" : "negativo";
        FinanceBalanceBadgeText.Foreground = monthBalance >= 0 ? Solid("#166534") : Solid("#991B1B");
        FinanceBalanceBadgeBorder.Background = monthBalance >= 0 ? Solid("#DCFCE7") : Solid("#FEE2E2");
        FinanceReceivedMonthText.Text = receivedMonth.ToString("C0", Brazil);
        FinanceExpensesMonthText.Text = monthExpenses.ToString("C0", Brazil);
        FinancePendingTotalText.Text = pendingValue.ToString("C0", Brazil);
        FinancePendingHintText.Text = pendingLabel;
        FinanceMercadoPagoText.Text = IsMercadoPagoPointReady()
            ? MercadoPagoTerminalLabel()
            : _data.Settings.MercadoPagoEnabled ? "Falta conectar Point" : "Desativado";
        FinanceMercadoPagoText.Foreground = IsMercadoPagoPointReady()
            ? Solid("#166534")
            : _data.Settings.MercadoPagoEnabled ? Solid("#B45309") : MutedBrush;
        FinanceMercadoPagoHintText.Text = IsMercadoPagoPointReady()
            ? "Credito e debito podem ir direto para a maquininha."
            : "Ative em Configuracoes para liberar cartao na maquininha.";

        _financeMetrics.Clear();
        _financeMetrics.Add(new EstablishmentMetricRow("Recebido hoje", receivedToday.ToString("C0", Brazil), "servicos, produtos e avulsos", AccentSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("Servicos do mes", serviceMonth.ToString("C0", Brazil), "atendimentos finalizados", BlueSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("Produtos do mes", productMonth.ToString("C0", Brazil), $"{_data.ProductSales.Count(item => item.SoldAt >= monthStart && item.SoldAt < nextMonth)} venda(s)", WarmSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("Gastos lancados", monthExpenses.ToString("C0", Brazil), $"{monthExpenseCount} despesa(s)", RedSoftBrush));

        _financeEntries.Clear();
        var maxEntry = Math.Max(1m, Math.Max(serviceMonth, Math.Max(productMonth, manualMonth)));
        _financeEntries.Add(new HomeFinanceBarRow("Servicos finalizados", serviceMonth.ToString("C0", Brazil), Percent(serviceMonth, maxEntry)));
        _financeEntries.Add(new HomeFinanceBarRow("Produtos vendidos", productMonth.ToString("C0", Brazil), Percent(productMonth, maxEntry)));
        _financeEntries.Add(new HomeFinanceBarRow("Recebimentos avulsos", manualMonth.ToString("C0", Brazil), Percent(manualMonth, maxEntry)));

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
                appointment.Price.ToString("C0", Brazil),
                YellowSoftBrush,
                InkBrush));
        }

        if (_financePendingPayments.Count == 0)
        {
            _financePendingPayments.Add(EmptyEstablishmentRow("Nada para cobrar agora", "Quando um atendimento ficar aberto, ele aparece aqui.", "R$ 0"));
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
                expense.Value.ToString("C0", Brazil),
                RedSoftBrush,
                InkBrush));
        }

        if (_financeExpenses.Count == 0)
        {
            _financeExpenses.Add(EmptyEstablishmentRow("Nenhum gasto lancado", "Aluguel, insumos e marketing aparecem aqui.", "R$ 0"));
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
                Value = SumReceivedRevenue(day, day.AddDays(1))
            })
            .ToList();
        var max = Math.Max(1m, days.Max(item => item.Value));

        foreach (var day in days)
        {
            _financeChartRows.Add(new HomeFinanceBarRow(
                day.Day.ToString("ddd, dd/MM", Brazil),
                day.Value.ToString("C0", Brazil),
                Percent(day.Value, max)));
        }
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
        var today = DateTime.Today;
        var periodStart = today.AddDays(-6);
        var periodEnd = today.AddDays(1);
        ReportsPeriodText.Text = $"{periodStart:dd/MM} a {today:dd/MM}";

        var appointments = _data.Appointments
            .Where(item => item.Start >= periodStart && item.Start < periodEnd)
            .Where(item => item.Status != AppointmentStatus.Blocked)
            .ToList();
        var finalizados = appointments.Count(item => item.Status == AppointmentStatus.Done);
        var canceladosFaltas = appointments.Count(item => item.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow);
        var receitaServicos = SumServiceRevenue(periodStart, periodEnd);
        var receita = SumReceivedRevenue(periodStart, periodEnd);
        var ticketMedio = finalizados > 0 ? receitaServicos / finalizados : 0m;
        var taxaConclusao = appointments.Count > 0 ? finalizados * 100m / appointments.Count : 0m;

        _reportsMetrics.Clear();
        _reportsMetrics.Add(new EstablishmentMetricRow("Agendamentos", appointments.Count.ToString(Brazil), "total do período", AccentSoftBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Finalizados", finalizados.ToString(Brazil), "atendimentos concluídos", BlueSoftBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Cancelados/faltas", canceladosFaltas.ToString(Brazil), "perdas no período", canceladosFaltas > 0 ? RedSoftBrush : GraySoftBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Receita", receita.ToString("C0", Brazil), "serviços, produtos e pagamentos", WarmSoftBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Ticket médio", ticketMedio.ToString("C0", Brazil), "por atendimento finalizado", AccentSoftBrush));
        _reportsMetrics.Add(new EstablishmentMetricRow("Conclusão", $"{taxaConclusao:N0}%", "finalizados sobre o total", taxaConclusao >= 70 ? BlueSoftBrush : YellowSoftBrush));

        RefreshReportsInsights(periodStart, periodEnd, appointments, ticketMedio, taxaConclusao);
        RefreshReportsChart(periodStart, today, appointments);
        RefreshReportsServices(periodStart, periodEnd);
        RefreshReportsProfessionals(periodStart, periodEnd);
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
            AccentBrush));
        _reportsInsights.Add(new EstablishmentListRow(
            "Ocupação estimada",
            $"{busyMinutes / 60:N0}h ocupada(s) em {professionalCount} profissional(is)",
            $"{occupancy:N0}%",
            BlueSoftBrush,
            AccentBrush));
        _reportsInsights.Add(new EstablishmentListRow(
            "Clientes atendidos",
            "Clientes únicos com atendimento finalizado",
            customersServed.ToString(Brazil),
            GraySoftBrush,
            InkBrush));
        _reportsInsights.Add(new EstablishmentListRow(
            "Produtos vendidos",
            $"{productSales.Count} venda(s) no período",
            productSales.Sum(item => item.Total).ToString("C0", Brazil),
            WarmSoftBrush,
            AccentBrush));
        _reportsInsights.Add(new EstablishmentListRow(
            "Pagamentos avulsos",
            $"{manualPayments.Count} recebimento(s) manual(is)",
            manualPayments.Sum(item => item.Value).ToString("C0", Brazil),
            AccentSoftBrush,
            AccentBrush));
        _reportsInsights.Add(new EstablishmentListRow(
            "Saúde da operação",
            $"Ticket médio {ticketMedio.ToString("C0", Brazil)} | conclusão {taxaConclusao:N0}%",
            taxaConclusao >= 70 ? "Boa" : "Atenção",
            taxaConclusao >= 70 ? BlueSoftBrush : YellowSoftBrush,
            taxaConclusao >= 70 ? AccentBrush : InkBrush));
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
        ApplyReportChartStyle(style, chartRows);
        UpdateReportChartModeButtons();
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
        const double size = 220;
        const double center = size / 2;
        const double outer = 102;
        const double inner = 62;

        if (total <= 0)
        {
            ReportsDonutCanvas.Children.Add(new Ellipse
            {
                Width = outer * 2,
                Height = outer * 2,
                Stroke = LineBrush,
                StrokeThickness = 24,
                Fill = Brushes.Transparent
            });
            Canvas.SetLeft(ReportsDonutCanvas.Children[^1], center - outer);
            Canvas.SetTop(ReportsDonutCanvas.Children[^1], center - outer);
        }
        else
        {
            var startAngle = 0d;
            foreach (var row in rows)
            {
                var sweep = Math.Max(1, Math.Min(359.8, (double)(row.Value / total) * 360));
                ReportsDonutCanvas.Children.Add(CreateDonutSlice(center, center, outer, inner, startAngle, sweep, row.AccentBrush));
                startAngle += sweep;
            }
        }

        var centerCircle = new Ellipse
        {
            Width = inner * 2 - 8,
            Height = inner * 2 - 8,
            Fill = Brushes.White
        };
        Canvas.SetLeft(centerCircle, center - inner + 4);
        Canvas.SetTop(centerCircle, center - inner + 4);
        ReportsDonutCanvas.Children.Add(centerCircle);

        var totalText = new TextBlock
        {
            Text = total.ToString("N0", Brazil),
            Foreground = InkBrush,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Width = 110
        };
        Canvas.SetLeft(totalText, center - 55);
        Canvas.SetTop(totalText, center - 28);
        ReportsDonutCanvas.Children.Add(totalText);

        var labelText = new TextBlock
        {
            Text = "total",
            Foreground = MutedBrush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Width = 110
        };
        Canvas.SetLeft(labelText, center - 55);
        Canvas.SetTop(labelText, center + 6);
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

    private void RefreshReportsServices(DateTime start, DateTime end)
    {
        _reportsServices.Clear();
        var rows = _data.Appointments
            .Where(item => item.Start >= start && item.Start < end &&
                           item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .Where(item => !string.IsNullOrWhiteSpace(item.ServiceName))
            .GroupBy(item => item.ServiceName)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Sum(item => item.Price))
            .Take(6)
            .ToList();

        foreach (var group in rows)
        {
            var revenue = group.Where(item => item.Status == AppointmentStatus.Done).Sum(item => item.Price);
            _reportsServices.Add(new EstablishmentListRow(
                group.Key,
                $"{group.Count()} atendimento(s) no período",
                revenue.ToString("C0", Brazil),
                AccentSoftBrush,
                AccentBrush));
        }

        if (_reportsServices.Count == 0)
        {
            _reportsServices.Add(EmptyEstablishmentRow("Nenhum serviço no período", "Os serviços realizados aparecerão aqui.", "0"));
        }
    }

    private void RefreshReportsProfessionals(DateTime start, DateTime end)
    {
        _reportsProfessionals.Clear();
        var rows = _data.Appointments
            .Where(item => item.Start >= start && item.Start < end &&
                           item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
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
                AccentBrush));
        }

        if (_reportsProfessionals.Count == 0)
        {
            _reportsProfessionals.Add(EmptyEstablishmentRow("Nenhum profissional no período", "Os atendimentos da equipe aparecerão aqui.", "0"));
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

        PromotionMessageTextBox.Text = "Oi, {nome}! Tudo bem? Aqui é da {empresa}. Temos uma promoção especial: {oferta}. Quer que eu veja um horário para você?";
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
        _marketingMessages.Add(new EstablishmentListRow("Promoção", "Texto para divulgar oferta, desconto ou pacote especial.", "oferta", AccentSoftBrush, AccentBrush));
        _marketingMessages.Add(new EstablishmentListRow("Confirmação", "Mensagem curta para confirmar presença antes do horário.", "agenda", BlueSoftBrush, AccentBrush));
        _marketingMessages.Add(new EstablishmentListRow("Pós-atendimento", "Agradeça o cliente e incentive retorno ou avaliação.", "retorno", GraySoftBrush, InkBrush));
        _marketingMessages.Add(new EstablishmentListRow("Cliente sumido", "Convide clientes sem atendimento recente para voltar.", "30 dias", YellowSoftBrush, InkBrush));
    }

    private void RefreshMarketingCampaigns(int staleCustomers, int noShows, int pendingConfirmations)
    {
        _marketingCampaigns.Clear();
        _marketingCampaigns.Add(new EstablishmentListRow("Volta para agenda", $"{staleCustomers} cliente(s) sem retorno para chamar.", "WhatsApp", AccentSoftBrush, AccentBrush));
        _marketingCampaigns.Add(new EstablishmentListRow("Confirmar horários", $"{pendingConfirmations} agendamento(s) aguardando confirmação.", "Hoje", BlueSoftBrush, AccentBrush));
        _marketingCampaigns.Add(new EstablishmentListRow("Recuperar faltas", $"{noShows} falta(s) recente(s) para remarcar.", "Retorno", RedSoftBrush, InkBrush));
        _marketingCampaigns.Add(new EstablishmentListRow("Oferta da semana", PromotionOfferTextBox.Text.Trim(), "Promo", YellowSoftBrush, InkBrush));
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
            ? $"Pronto para confirmar horários, chamar clientes e acompanhar conversas. Último evento: {LastWhatsAppEventText()}."
            : hasQr
                ? "Escaneie o QR no painel flutuante para ativar confirmações e mensagens no app."
                : "Linke o WhatsApp da loja para confirmar horários, chamar clientes e ver mensagens no app.";
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
        if (!WhatsAppStorePhoneTextBox.IsKeyboardFocusWithin)
        {
            WhatsAppStorePhoneTextBox.Text = string.IsNullOrWhiteSpace(settings.WhatsAppStorePhone)
                ? FirstFilled(settings.BusinessPhone, settings.AccountPhone)
                : settings.WhatsAppStorePhone;
        }

        var messagesWithPhone = _data.WhatsAppMessages
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
                incoming ? Solid("#F8FAFC") : Solid("#F0FDF4"),
                incoming ? Solid("#E2E8F0") : Solid("#BBF7D0")));
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
        RefreshWhatsAppReplyComposer();
        ScrollWhatsAppConversationToEnd();

        var hasFreshIncoming = _data.WhatsAppMessages.Any(item =>
            string.Equals(item.Direction, "entrada", StringComparison.OrdinalIgnoreCase) &&
            item.CreatedAt >= DateTime.Now.AddMinutes(-10));
        WhatsAppFloatingBadge.Visibility =
            hasFreshIncoming && WhatsAppFloatingPanel.Visibility != Visibility.Visible
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
                             IsWhatsAppIncoming(item) &&
                             item.CreatedAt >= DateTime.Now.AddDays(-7));
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
                    .OrderBy(item => item.Name)
                    .Take(12)
                    .Select(item => new
                    {
                        name = item.Name,
                        durationMinutes = item.DurationMinutes,
                        price = item.Price
                    })
                    .ToList(),
                professionals = _data.Professionals
                    .OrderBy(item => item.Name)
                    .Take(12)
                    .Select(item => item.Name)
                    .ToList(),
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
                (string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Phone, normalizedPhone, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Message, cleanMessage, StringComparison.Ordinal) &&
                 Math.Abs((item.CreatedAt - when).TotalMinutes) < 3) ||
                (!string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Phone, normalizedPhone, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(item.Message, cleanMessage, StringComparison.Ordinal) &&
                 Math.Abs((item.CreatedAt - when).TotalSeconds) <= 1)))
        {
            return;
        }

        _data.WhatsAppMessages.Insert(0, new WhatsAppMessage
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
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
        if (opening)
        {
            _whatsAppConversationOpen = false;
        }

        WhatsAppFloatingPanel.Visibility = opening ? Visibility.Visible : Visibility.Collapsed;
        RefreshWhatsAppSurface();
        _ = PollWhatsAppEvolutionMessagesAsync();
    }

    private void OpenWhatsAppPanelButton_Click(object sender, RoutedEventArgs e)
    {
        _whatsAppConversationOpen = false;
        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        RefreshWhatsAppSurface();
        _ = PollWhatsAppEvolutionMessagesAsync();
    }

    private void CloseWhatsAppPanelButton_Click(object sender, RoutedEventArgs e)
    {
        WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();
        UpdateWhatsAppPollingState();
    }

    private void SelectWhatsAppConversationFromMessageButton_Click(object sender, MouseButtonEventArgs e)
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

    private void OpenWhatsAppConversationCard_Click(object sender, MouseButtonEventArgs e)
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
        _data.Settings.WhatsAppStorePhone = normalizedPhone;
        _data.Settings.WhatsAppEvolutionQrBase64 = "";
        _data.Settings.WhatsAppEvolutionState = "";
        TryApplyWhatsAppEvolutionLocalEnv(preferLocal: true);
        _data.Settings.WhatsAppEvolutionBaseUrl = NormalizeWhatsAppEvolutionBaseUrl(_data.Settings.WhatsAppEvolutionBaseUrl);
        _data.Settings.WhatsAppEvolutionInstanceName = NormalizeWhatsAppEvolutionInstanceName(_data.Settings.WhatsAppEvolutionInstanceName);
        _store.Save(_data);
        WhatsAppFloatingPanel.Visibility = Visibility.Visible;
        RefreshWhatsAppSurface();
        ShowStatus("Gerando QR do WhatsApp para linkar a loja...");

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
        var wasLinked = _data.Settings.WhatsAppLinked;
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
        if (!wasLinked)
        {
            AddWhatsAppMessage(
                BusinessDisplayName(),
                _data.Settings.WhatsAppStorePhone,
                "WhatsApp linkado. Confirmações, retornos e mensagens dos clientes aparecem neste painel.",
                "Conexão",
                "recebido",
                "entrada");
        }
        else
        {
            _store.Save(_data);
            RefreshWhatsAppSurface();
        }
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
        var message = $"BLV|{WhatsAppEvolutionLicenseExpires}|{WhatsAppEvolutionLicenseScope}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WhatsAppEvolutionLicenseSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)))[..10];
        return $"BLV-{WhatsAppEvolutionLicenseExpires}-{WhatsAppEvolutionLicenseScope}-{signature}";
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

        var dayAppointments = ApplyFilters(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date))
            .OrderBy(item => item.Start)
            .ToList();

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

        ScheduleBoardGrid.MinWidth = ScheduleTimeColumnWidth + professionals.Count * ScheduleProfessionalColumnWidth;
        ScheduleBoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ScheduleTimeColumnWidth) });
        foreach (var _ in professionals)
        {
            ScheduleBoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ScheduleProfessionalColumnWidth) });
        }

        ScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
        for (var index = 0; index < slotCount; index++)
        {
            ScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
        }

        AddScheduleCorner(dayStart);
        AddScheduleHeaders(professionals);
        AddScheduleCells(dayStart, slotCount, professionals);
        AddScheduleAppointments(dayStart, slotCount, professionals, dayAppointments);
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
            Background = Solid("#F6F9FC"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = dayStart.ToString("ddd", Brazil).ToUpper(Brazil),
                        Foreground = MutedBrush,
                        FontSize = 11,
                        FontWeight = FontWeights.Bold
                    },
                    new TextBlock
                    {
                        Text = dayStart.ToString("dd/MM", Brazil),
                        Foreground = InkBrush,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold
                    }
                }
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
                Background = Solid("#FFFFFF"),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(12, 9, 12, 8),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = professional.Name,
                            Foreground = InkBrush,
                            FontSize = 14,
                            FontWeight = FontWeights.Bold,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        },
                        new TextBlock
                        {
                            Text = professional.SegmentLine,
                            Foreground = MutedBrush,
                            FontSize = 11,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            Margin = new Thickness(0, 2, 0, 0)
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
                Background = row % 2 == 0 ? Solid("#F7FAFC") : Solid("#EEF3F7"),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(8, 7, 8, 0),
                Child = new TextBlock
                {
                    Text = slotStart.ToString("HH:mm", Brazil),
                    Foreground = MutedBrush,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
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
                    Background = row % 2 == 0 ? Solid("#FFFFFF") : Solid("#FAFCFD"),
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

            var card = CreateScheduleAppointmentCard(appointment);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, column);
            Grid.SetRowSpan(card, rowSpan);
            Panel.SetZIndex(card, 5);
            ScheduleBoardGrid.Children.Add(card);
        }
    }

    private Border CreateScheduleAppointmentCard(Appointment appointment)
    {
        var statusBrush = StatusBackground(appointment.Status);
        var accentBrush = AccentFor(appointment.Status);
        var card = new Border
        {
            Margin = new Thickness(5),
            Padding = new Thickness(9),
            Background = statusBrush,
            BorderBrush = accentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Cursor = Cursors.Hand,
            Tag = appointment,
            ToolTip = $"{appointment.Start:HH:mm}-{appointment.End:HH:mm} | {appointment.CustomerName} | {appointment.ServiceName}"
        };
        card.PreviewMouseLeftButtonDown += ScheduleAppointment_MouseLeftButtonDown;

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:HH:mm}  {StatusLabel(appointment.Status)}",
            Foreground = StatusForeground(appointment.Status),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = appointment.CustomerName,
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0)
        });
        stack.Children.Add(new TextBlock
        {
            Text = appointment.ServiceName,
            Foreground = MutedBrush,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 0)
        });

        if (!string.IsNullOrWhiteSpace(appointment.ResourceName))
        {
            stack.Children.Add(new TextBlock
            {
                Text = appointment.ResourceName,
                Foreground = AccentBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        card.Child = stack;
        return card;
    }

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
            ? _data.Professionals
            : _data.Professionals.Where(item => item.Segments.Contains(segment));
    }

    private string CurrentSegmentFilter() => _selectedSegmentFilter;

    private void UpdateAppointmentOptions(string? segment)
    {
        segment = string.IsNullOrWhiteSpace(segment) ? GetAvailableSegments()[0] : segment;

        _filteredServices.Clear();
        foreach (var serviceItem in _data.Services.Where(item => item.Segment == segment).OrderBy(item => item.Name))
        {
            _filteredServices.Add(serviceItem);
        }

        _filteredProfessionals.Clear();
        foreach (var professional in _data.Professionals.Where(item => item.Segments.Contains(segment)).OrderBy(item => item.Name))
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

        AppointmentDatePicker.SelectedDate = _selectedDate;
        TimeCombo.Text = SuggestedTimeFor(_selectedDate);
        DurationCombo.SelectedItem = 30;
        CustomerNameTextBox.Text = "";
        CustomerProfileTextBox.Text = "";
        PhoneTextBox.Text = "";
        NotesTextBox.Text = "";
        PriceTextBox.Text = "";

        ServiceCombo.SelectedItem = _filteredServices.FirstOrDefault();
        ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault();
        if (ServiceCombo.SelectedItem is ServiceItem service)
        {
            ApplyServiceDefaults(service);
        }

        EditorTitleText.Text = "Novo agendamento";
        EditorStatusText.Text = "Pronto para agendar";
        SelectedAppointmentCard.Visibility = Visibility.Collapsed;
        ExistingAppointmentActionsPanel.Visibility = Visibility.Collapsed;
        _loadingEditor = false;
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
        ResourceCombo.Text = appointment.ResourceName;
        CustomerNameTextBox.Text = appointment.Status == AppointmentStatus.Blocked ? "" : appointment.CustomerName;
        CustomerProfileTextBox.Text = appointment.CustomerProfile;
        PhoneTextBox.Text = appointment.CustomerPhone;
        PriceTextBox.Text = appointment.Price.ToString("N2", Brazil);
        NotesTextBox.Text = appointment.Notes;

        EditorTitleText.Text = appointment.Status == AppointmentStatus.Blocked ? "Bloqueio de horário" : "Editar agendamento";
        EditorStatusText.Text = $"{StatusLabel(appointment.Status)} | criado em {appointment.CreatedAt:dd/MM HH:mm}";
        ExistingAppointmentActionsPanel.Visibility = Visibility.Visible;
        ShowSelectedAppointment(appointment);
        _loadingEditor = false;
    }

    private void OpenAppointmentEditorModal()
    {
        WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
        AppointmentEditorOverlay.Visibility = Visibility.Visible;
        RefreshWhatsAppLauncherVisibility();
    }

    private void CloseAppointmentEditorModal()
    {
        AppointmentEditorOverlay.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();
    }

    private void CloseAppointmentModalButton_Click(object sender, RoutedEventArgs e)
    {
        CloseAppointmentEditorModal();
    }

    private void AppointmentEditorForm_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Return)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var textBox = FindVisualParent<TextBox>(source);
        var comboBox = FindVisualParent<ComboBox>(source);
        var datePicker = FindVisualParent<DatePicker>(source);

        if (comboBox is { IsDropDownOpen: true })
        {
            return;
        }

        if (textBox is { AcceptsReturn: true } &&
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        var target = comboBox ?? datePicker ?? (Control?)textBox;
        if (target is null)
        {
            return;
        }

        e.Handled = true;
        target.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
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
        customer.Email = form.Email;
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

    private void CreatePromotionButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureDefaultPromotionMessage();
        RefreshMarketingPage();
        ShowStatus($"Promoção pronta: {PromotionNameTextBox.Text.Trim()}.");
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

        var shell = CreateEditorDialog(title, subtitle, primaryText);
        shell.Dialog.Width = 760;
        shell.Dialog.MinHeight = 560;
        string? editId = null;
        AddManagerRows(shell.Body, section, emptyTitle, emptyDetail, id =>
        {
            editId = id;
            shell.Dialog.DialogResult = false;
        });
        shell.PrimaryButton.Click += (_, _) => shell.Dialog.DialogResult = true;

        var dialogResult = shell.Dialog.ShowDialog();
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

    private void AddManagerRows(StackPanel body, string section, string emptyTitle, string emptyDetail, Action<string>? editRequested = null)
    {
        var rows = section switch
        {
            "Clientes" => _data.Customers
                .OrderBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join(" | ", new[] { item.Phone, item.Email, item.Tags, item.Profile, item.Segment }.Where(part => !string.IsNullOrWhiteSpace(part)).DefaultIfEmpty("Sem detalhes cadastrados")),
                    item.LastSeenAt == DateTime.MinValue ? "novo" : item.LastSeenAt.ToString("dd/MM", Brazil),
                    AccentSoftBrush,
                    AccentBrush,
                    item.Id))
                .ToList(),
            "Profissionais" => _data.Professionals
                .OrderBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join(" | ", new[] { item.SegmentLine, item.Phone, item.Email, item.CommissionPercent > 0 ? $"{item.CommissionPercent:N0}% comissão" : "" }.Where(part => !string.IsNullOrWhiteSpace(part)).DefaultIfEmpty("Sem detalhes cadastrados")),
                    item.IsActive ? "ativo" : "inativo",
                    BlueSoftBrush,
                    AccentBrush,
                    item.Id))
                .ToList(),
            "Serviços" => _data.Services
                .OrderBy(item => item.Segment)
                .ThenBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join(" | ", new[]
                    {
                        item.Segment,
                        item.Category,
                        $"{item.DurationMinutes} min",
                        item.Price.ToString("C", Brazil),
                        item.DefaultResource,
                        item.BufferMinutes > 0 ? $"{item.BufferMinutes} min intervalo" : ""
                    }.Where(part => !string.IsNullOrWhiteSpace(part))),
                    item.IsActive ? "ativo" : "inativo",
                    GraySoftBrush,
                    AccentBrush,
                    item.Id))
                .ToList(),
            "Produtos" => _data.Products
                .OrderBy(item => item.Name)
                .Select(item => new EstablishmentListRow(
                    item.Name,
                    string.Join(" | ", new[]
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
                    item.Id))
                .ToList(),
            "Venda de produtos" => _data.ProductSales
                .OrderByDescending(item => item.SoldAt)
                .Select(item => new EstablishmentListRow(
                    item.ProductName,
                    $"{item.Quantity} un. | {item.Total.ToString("C", Brazil)}" +
                    (string.IsNullOrWhiteSpace(item.CustomerName) ? "" : $" | {item.CustomerName}") +
                    (string.IsNullOrWhiteSpace(item.PaymentMethod) ? "" : $" | {item.PaymentMethod}"),
                    item.SoldAt.ToString("dd/MM", Brazil),
                    WarmSoftBrush,
                    AccentBrush,
                    item.Id))
                .ToList(),
            _ => []
        };

        body.Children.Add(new TextBlock
        {
            Text = $"{rows.Count} registro(s)",
            Foreground = AccentBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
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

    private void AddManagerEmptyState(StackPanel body, string section, string title, string detail)
    {
        var suggestions = ManagerSuggestions(section).ToList();
        var fields = ManagerRecommendedFields(section).ToList();

        var panel = new Border
        {
            Background = Solid("#F8FAFC"),
            BorderBrush = Solid("#BFDBFE"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
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
                    CornerRadius = new CornerRadius(13),
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
                    CornerRadius = new CornerRadius(12),
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
        "Clientes" => ["Nome", "WhatsApp", "E-mail", "Documento", _data.Settings.ClientDetailLabel, "Tags e observações"],
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
        var rightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Badge(row.BadgeText, row.BadgeBackground, row.BadgeForeground)
            }
        };

        if (clickAction is not null)
        {
            rightPanel.Children.Add(new TextBlock
            {
                Text = "Editar",
                Foreground = AccentBrush,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        Grid.SetColumn(rightPanel, 1);

        var rowCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = clickAction is null ? Cursors.Arrow : Cursors.Hand,
            ToolTip = clickAction is null ? null : "Clique para ver e editar",
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = row.Name, Foreground = InkBrush, FontSize = 15, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap },
                            new TextBlock { Text = row.Detail, Foreground = MutedBrush, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 12, 0) }
                        }
                    },
                    rightPanel
                }
            }
        };

        if (clickAction is not null)
        {
            rowCard.MouseLeftButtonUp += (_, _) => clickAction();
        }

        return rowCard;
    }

    private static Border Badge(string text, Brush background, Brush foreground)
    {
        var badge = new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(14),
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
        customer.Email = form.Email;
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

    private void ShowMainPage(MainPage page)
    {
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
    }

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        SidebarColumn.Width = new GridLength(_sidebarCollapsed ? 72 : 230);
        SidebarExpandedPanel.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarCollapsedPanel.Visibility = _sidebarCollapsed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenOnboardingFromSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowOnboarding();
    }

    private void ExitCurrentSystemButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Você vai sair da agenda atual e voltar para a configuração inicial.\n\nOs dados atuais continuam salvos neste computador. Eles só serão substituídos se você criar outra agenda.",
            "Sair do sistema",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
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
        ResourcesCountText.Text = $"{_data.Settings.Resources.Count} sala(s) ou recurso(s)";
        RefreshMercadoPagoSettingsSummary();
    }

    private void RefreshMercadoPagoSettingsSummary()
    {
        var terminal = MercadoPagoTerminalLabel();
        if (!_data.Settings.MercadoPagoEnabled)
        {
            SettingsMercadoPagoStatusText.Text = "Desativado";
            SettingsMercadoPagoStatusText.Foreground = MutedBrush;
            SettingsMercadoPagoDetailText.Text = "Ative em Configurações para liberar crédito/débito pela maquininha no registro de pagamento.";
            return;
        }

        if (!_data.Settings.MercadoPagoConnected)
        {
            SettingsMercadoPagoStatusText.Text = "Ativado, falta conectar";
            SettingsMercadoPagoStatusText.Foreground = Solid("#D97706");
            SettingsMercadoPagoDetailText.Text = "Mercado Pago ativado, mas a conta da loja ainda precisa ser conectada para cobrar na Point.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_data.Settings.MercadoPagoDefaultTerminalId))
        {
            SettingsMercadoPagoStatusText.Text = "Conectado, sem Point";
            SettingsMercadoPagoStatusText.Foreground = Solid("#D97706");
            SettingsMercadoPagoDetailText.Text = "Conta conectada. Escolha uma maquininha Point para liberar cartão no financeiro.";
            return;
        }

        SettingsMercadoPagoStatusText.Text = "Maquininha pronta";
        SettingsMercadoPagoStatusText.Foreground = Solid("#16A34A");
        SettingsMercadoPagoDetailText.Text = $"Cartão será enviado para {terminal}. O pagamento só registra quando o Mercado Pago aprovar.";
    }

    private void OpenMercadoPagoSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var shell = CreateEditorDialog("Mercado Pago", "Ative a conta e escolha a Point usada nos pagamentos da agenda.", "Salvar configuração");
        shell.Dialog.Width = 820;

        AddDialogSection(shell.Body, "Mercado Pago na agenda", "Ative para cobrar cartão na Point e registrar o pagamento só depois da aprovação.");
        var enabledCheck = AddDialogCheckBox(shell.Body, "Usar Mercado Pago nos pagamentos", _data.Settings.MercadoPagoEnabled);
        AddDialogInfoCard(
            shell.Body,
            "Como funciona",
            "Conecte a conta Mercado Pago da loja, escolha a Point e use crédito/débito pela maquininha no financeiro.",
            "#F8FAFC",
            "#CBD5E1");

        AddDialogSection(shell.Body, "Conta e maquininha", "Conecte a conta Mercado Pago e selecione a Point da loja.");
        var statusText = new TextBlock
        {
            Text = MercadoPagoSettingsDetailText(),
            Foreground = MutedBrush,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        shell.Body.Children.Add(statusText);

        var terminalOptions = CurrentMercadoPagoTerminalOptions();
        var terminalBox = AddDialogComboField(shell.Body, "Point da loja", terminalOptions, terminalOptions.FirstOrDefault(item => item.Id == _data.Settings.MercadoPagoDefaultTerminalId), editable: false);
        terminalBox.DisplayMemberPath = nameof(AgendaMercadoPagoTerminalDto.Display);

        var actions = new Grid { Margin = new Thickness(0, 2, 0, 14) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var connectButton = new Button { Content = "Conectar", Style = (Style)FindResource("CommandButton"), Height = 40 };
        var statusButton = new Button { Content = "Checar conta", Style = (Style)FindResource("GhostButton"), Height = 40 };
        var terminalsButton = new Button { Content = "Buscar Points", Style = (Style)FindResource("GhostButton"), Height = 40 };
        Grid.SetColumn(statusButton, 2);
        Grid.SetColumn(terminalsButton, 4);
        actions.Children.Add(connectButton);
        actions.Children.Add(statusButton);
        actions.Children.Add(terminalsButton);
        shell.Body.Children.Add(actions);

        void CopyDialogFieldsToSettings()
        {
            _data.Settings.MercadoPagoEnabled = enabledCheck.IsChecked == true;
            EnsureMercadoPagoInternalSettings();
            if (terminalBox.SelectedItem is AgendaMercadoPagoTerminalDto terminal)
            {
                _data.Settings.MercadoPagoDefaultTerminalId = terminal.Id.Trim();
                _data.Settings.MercadoPagoDefaultTerminalLabel = terminal.Display.Trim();
            }
        }

        void RefreshDialogStatus()
        {
            statusText.Text = MercadoPagoSettingsDetailText();
            statusText.Foreground = IsMercadoPagoPointReady()
                ? Solid("#16A34A")
                : _data.Settings.MercadoPagoEnabled ? Solid("#D97706") : MutedBrush;
            enabledCheck.IsChecked = _data.Settings.MercadoPagoEnabled;
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

        shell.PrimaryButton.Click += (_, _) =>
        {
            CopyDialogFieldsToSettings();
            _store.Save(_data);
            RefreshSettingsSummary();
            shell.Dialog.DialogResult = true;
        };

        RefreshDialogStatus();
        shell.Dialog.ShowDialog();
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
        var waitDialog = new Window
        {
            Title = "Mercado Pago Point",
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Width = 480,
            SizeToContent = SizeToContent.Height,
            Background = Brushes.White
        };

        var statusText = new TextBlock
        {
            Text = $"Cobrança enviada para {MercadoPagoTerminalLabel()}.",
            Foreground = InkBrush,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        };
        var detailText = new TextBlock
        {
            Text = $"{amount.ToString("C", Brazil)} | {method}",
            Foreground = MutedBrush,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 14)
        };
        var cancelButton = new Button
        {
            Content = "Parar espera",
            Style = (Style)FindResource("GhostButton"),
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        cancelButton.Click += (_, _) =>
        {
            cancelled = true;
            waitDialog.Close();
        };
        waitDialog.Closed += (_, _) => cancelled = !paid;
        waitDialog.Content = new StackPanel
        {
            Margin = new Thickness(18),
            Children =
            {
                new TextBlock { Text = "Passe o cartão na maquininha", Foreground = AccentBrush, FontWeight = FontWeights.Bold, FontSize = 13 },
                statusText,
                detailText,
                new TextBlock
                {
                    Text = "O pagamento só será salvo quando o Mercado Pago confirmar aprovação.",
                    Foreground = MutedBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 14)
                },
                cancelButton
            }
        };
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
            ResourceCombo.Text = service.DefaultResource;
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
        ResourceCombo.SelectedItem = _resourceOptions.FirstOrDefault(item => item.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        RefreshSettingsSummary();
        ShowStatus($"Recurso criado: {resourceName}.");
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
        var shell = CreateEditorDialog(
            existing is null ? "Criar cliente" : "Editar cliente",
            "Cadastro completo para agenda, WhatsApp e histórico.",
            existing is null ? "Salvar cliente" : "Salvar alterações");
        shell.Dialog.Width = 820;
        AddDialogSection(shell.Body, "Dados do cliente", "Informações usadas na agenda, histórico e WhatsApp.");
        var identityRow = AddDialogColumns(shell.Body);
        var nameBox = AddDialogTextField(identityRow.Left, "Nome do cliente", initialName, "Ex: Maria Silva");
        var phoneBox = AddDialogTextField(identityRow.Right, "WhatsApp principal", initialPhone, "Ex: (27) 99999-0000");

        var contactRow = AddDialogColumns(shell.Body);
        var emailBox = AddDialogTextField(contactRow.Left, "E-mail", existing?.Email ?? "", "Ex: cliente@email.com");
        var documentBox = AddDialogTextField(contactRow.Right, "CPF / documento", existing?.Document ?? "", "Ex: 123.456.789-00");

        AddDialogSection(shell.Body, "Atendimento", "Preferências e observações para a equipe.");
        var segmentRow = AddDialogColumns(shell.Body);
        var segmentCombo = AddDialogComboField(segmentRow.Left, "Segmento", GetAvailableSegments(), initialSegment, editable: false);
        var tagsBox = AddDialogTextField(segmentRow.Right, "Tags", existing?.Tags ?? "", "Ex: VIP, recorrente, pós-venda");
        var profileBox = AddDialogTextField(shell.Body, _data.Settings.ClientDetailLabel, initialProfile, "Preferência, observação, paciente, pet ou veículo", multiline: true);
        var notesBox = AddDialogTextField(shell.Body, "Observações internas", existing?.Notes ?? "", "Ex: horário preferido, restrições, combinado financeiro", multiline: true);
        var whatsAppCheck = AddDialogCheckBox(shell.Body, "Pode receber confirmação e retorno pelo WhatsApp", existing?.AcceptsWhatsApp ?? true);

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

            result = new CustomerEditorForm(
                customerName,
                phoneBox.Text.Trim(),
                emailBox.Text.Trim(),
                documentBox.Text.Trim(),
                DialogComboText(segmentCombo, initialSegment),
                profileBox.Text.Trim(),
                tagsBox.Text.Trim(),
                notesBox.Text.Trim(),
                whatsAppCheck.IsChecked == true);
            shell.Dialog.DialogResult = true;
        };

        nameBox.SelectAll();
        nameBox.Focus();
        return shell.Dialog.ShowDialog() == true ? result : null;
    }

    private ServiceEditorForm? ShowServiceEditorDialog(string segment, ServiceItem? existing = null)
    {
        var initialSegment = string.IsNullOrWhiteSpace(existing?.Segment) ? segment : existing.Segment;
        var categoryOptions = ServiceCategoryOptions(initialSegment).ToList();
        var initialCategory = string.IsNullOrWhiteSpace(existing?.Category)
            ? categoryOptions.FirstOrDefault() ?? ""
            : existing.Category;
        var shell = CreateEditorDialog(
            existing is null ? "Criar serviço" : "Editar serviço",
            "Defina como o serviço aparece na agenda e no atendimento.",
            existing is null ? "Salvar serviço" : "Salvar alterações");
        shell.Dialog.Width = 840;
        AddDialogSection(shell.Body, "Catálogo", "Como o serviço aparece na criação de agendamentos.");
        var catalogRow = AddDialogColumns(shell.Body);
        var segmentCombo = AddDialogComboField(catalogRow.Left, "Tipo de atendimento", GetAvailableSegments(), initialSegment, editable: false);
        var categoryCombo = AddDialogComboField(catalogRow.Right, "Categoria", categoryOptions, initialCategory, editable: true);
        var nameBox = AddDialogTextField(shell.Body, "Nome do serviço", existing?.Name ?? "", "Ex: Corte masculino, consulta, revisão");
        var descriptionBox = AddDialogTextField(shell.Body, "Descrição para a equipe", existing?.Description ?? "", "Ex: inclui lavagem, avaliação inicial ou checklist", multiline: true);

        AddDialogSection(shell.Body, "Tempo e agenda", "Duração real do atendimento e bloqueios automáticos.");
        var timeRow = AddDialogColumns(shell.Body);
        var durationBox = AddDialogTextField(timeRow.Left, "Duração em minutos", (existing?.DurationMinutes ?? ReadCurrentDurationOrDefault()).ToString(Brazil), "Ex: 30");
        var preparationBox = AddDialogTextField(timeRow.Right, "Preparação antes (min)", (existing?.PreparationMinutes ?? 0).ToString(Brazil), "Ex: 5");
        var flowRow = AddDialogColumns(shell.Body);
        var bufferBox = AddDialogTextField(flowRow.Left, "Intervalo após (min)", (existing?.BufferMinutes ?? 0).ToString(Brazil), "Ex: 10");
        var resourceCombo = AddDialogComboField(flowRow.Right, "Sala, cadeira ou recurso padrão", _data.Settings.Resources, existing?.DefaultResource ?? CurrentResourceText(), editable: true);

        AddDialogSection(shell.Body, "Preço e equipe", "Valor cobrado e comissão padrão deste serviço.");
        var moneyRow = AddDialogColumns(shell.Body);
        var priceBox = AddDialogTextField(moneyRow.Left, "Valor de venda", (existing?.Price ?? 0) > 0 ? existing!.Price.ToString("N2", Brazil) : PriceTextBox.Text.Trim(), "Ex: 45,00");
        var commissionBox = AddDialogTextField(moneyRow.Right, "Comissão (%)", (existing?.CommissionPercent ?? 0).ToString("N2", Brazil), "Ex: 40");
        var activeCheck = AddDialogCheckBox(shell.Body, "Serviço ativo para novos agendamentos", existing?.IsActive ?? true);

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
        return shell.Dialog.ShowDialog() == true ? result : null;
    }

    private ProfessionalEditorForm? ShowProfessionalEditorDialog(string segment, Professional? existing = null)
    {
        var initialSegment = existing?.Segments.FirstOrDefault() ?? segment;
        var shell = CreateEditorDialog(
            existing is null ? "Criar profissional" : "Editar profissional",
            "Cadastre quem atende e em qual agenda ele aparece.",
            existing is null ? "Salvar profissional" : "Salvar alterações");
        shell.Dialog.Width = 820;
        AddDialogSection(shell.Body, "Identificação", "Dados usados na agenda e no cadastro da equipe.");
        var identityRow = AddDialogColumns(shell.Body);
        var nameBox = AddDialogTextField(identityRow.Left, "Nome do profissional", existing?.Name ?? "", "Ex: Lucas");
        var roleBox = AddDialogTextField(identityRow.Right, "Função", string.IsNullOrWhiteSpace(existing?.Role) ? DefaultRoleForSegment(initialSegment) : existing.Role, "Ex: Barbeiro, mecânico, dentista");

        var contactRow = AddDialogColumns(shell.Body);
        var phoneBox = AddDialogTextField(contactRow.Left, "Telefone / WhatsApp", existing?.Phone ?? "", "Ex: (27) 99999-0000");
        var emailBox = AddDialogTextField(contactRow.Right, "E-mail", existing?.Email ?? "", "Ex: profissional@email.com");

        AddDialogSection(shell.Body, "Agenda e financeiro", "Segmento atendido, documento e comissão padrão.");
        var agendaRow = AddDialogColumns(shell.Body);
        var segmentCombo = AddDialogComboField(agendaRow.Left, "Segmento atendido", GetAvailableSegments(), initialSegment, editable: false);
        var documentBox = AddDialogTextField(agendaRow.Right, "CPF / documento", existing?.Document ?? "", "Ex: 123.456.789-00");
        var commissionBox = AddDialogTextField(shell.Body, "Comissão padrão (%)", (existing?.CommissionPercent ?? 0).ToString("N2", Brazil), "Ex: 40");
        var notesBox = AddDialogTextField(shell.Body, "Observações internas", existing?.Notes ?? "", "Ex: folgas, especialidades, restrições de horário", multiline: true);
        var activeCheck = AddDialogCheckBox(shell.Body, "Profissional ativo na agenda", existing?.IsActive ?? true);

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

            if (!TryReadDialogMoney(commissionBox, allowZero: true, out var commission) || commission > 100)
            {
                SetDialogError(shell.ErrorText, "Informe uma comissão entre 0 e 100%.");
                commissionBox.Focus();
                return;
            }

            result = new ProfessionalEditorForm(
                professionalName,
                string.IsNullOrWhiteSpace(roleBox.Text) ? DefaultRoleForSegment(initialSegment) : roleBox.Text.Trim(),
                phoneBox.Text.Trim(),
                emailBox.Text.Trim(),
                documentBox.Text.Trim(),
                commission,
                DialogComboText(segmentCombo, initialSegment),
                notesBox.Text.Trim(),
                activeCheck.IsChecked == true);
            shell.Dialog.DialogResult = true;
        };

        nameBox.Focus();
        return shell.Dialog.ShowDialog() == true ? result : null;
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
        return shell.Dialog.ShowDialog() == true ? result : null;
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
        return shell.Dialog.ShowDialog() == true ? result : null;
    }

    private PaymentEditorForm? ShowPaymentEditorDialog()
    {
        var shell = CreateEditorDialog("Registrar pagamento", "Lance um recebimento avulso no financeiro.", "Registrar pagamento");
        shell.Dialog.Width = 760;
        AddDialogSection(shell.Body, "Recebimento", "Identifique de onde veio o valor.");
        var mainRow = AddDialogColumns(shell.Body);
        var descriptionBox = AddDialogTextField(mainRow.Left, "Descrição", "Pagamento avulso", "Ex: Sinal de agendamento");
        var customerBox = AddDialogComboField(mainRow.Right, "Cliente", _data.Customers.Select(item => item.Name).Distinct().OrderBy(item => item), "", editable: true);

        var paymentRow = AddDialogColumns(shell.Body);
        var categoryBox = AddDialogComboField(paymentRow.Left, "Categoria", new[] { "Agendamento", "Produto", "Sinal", "Mensalidade", "Ajuste", "Outro" }, "Agendamento", editable: true);
        var methodBox = AddDialogComboField(paymentRow.Right, "Forma de pagamento", PaymentMethodOptions(), "Pix", editable: true);
        AddDialogInfoCard(shell.Body, "Mercado Pago na maquininha", MercadoPagoPaymentHintText(), IsMercadoPagoPointReady() ? "#DCFCE7" : "#FFF7ED", IsMercadoPagoPointReady() ? "#16A34A" : "#D97706");
        var valueBox = AddDialogTextField(shell.Body, "Valor recebido", "0,00", "Ex: 80,00");
        var notesBox = AddDialogTextField(shell.Body, "Observações", "", "Ex: pago antecipado, comprovante enviado, ajuste manual", multiline: true);

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
        return shell.Dialog.ShowDialog() == true ? result : null;
    }

    private ExpenseEditorForm? ShowExpenseEditorDialog()
    {
        var shell = CreateEditorDialog("Nova despesa", "Registre custos do dia, fornecedores ou operação.", "Salvar despesa");
        shell.Dialog.Width = 760;
        AddDialogSection(shell.Body, "Despesa", "Controle custos fixos, fornecedores e operação.");
        var mainRow = AddDialogColumns(shell.Body);
        var descriptionBox = AddDialogTextField(mainRow.Left, "Descrição", "", "Ex: Aluguel, comissão, material");
        var categoryBox = AddDialogComboField(mainRow.Right, "Categoria", new[] { "Operacional", "Fornecedor", "Equipe", "Marketing", "Aluguel", "Impostos", "Estoque" }, "Operacional", editable: true);

        var moneyRow = AddDialogColumns(shell.Body);
        var supplierBox = AddDialogTextField(moneyRow.Left, "Fornecedor / responsável", "", "Ex: distribuidora, proprietário, equipe");
        var methodBox = AddDialogComboField(moneyRow.Right, "Forma de pagamento", PaymentMethodOptions(), "Pix", editable: true);
        var valueBox = AddDialogTextField(shell.Body, "Valor", "0,00", "Ex: 120,00");
        var notesBox = AddDialogTextField(shell.Body, "Observações", "", "Ex: vencimento, nota, parcela, recorrência", multiline: true);

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
        return shell.Dialog.ShowDialog() == true ? result : null;
    }

    private ProductSaleEditorForm? ShowProductSaleEditorDialog(ProductSale? existing = null)
    {
        var selectedProduct = existing is null
            ? _data.Products.OrderBy(item => item.Name).First()
            : _data.Products.FirstOrDefault(item => item.Id == existing.ProductId)
              ?? _data.Products.FirstOrDefault(item => item.Name.Equals(existing.ProductName, StringComparison.OrdinalIgnoreCase))
              ?? _data.Products.OrderBy(item => item.Name).First();
        var shell = CreateEditorDialog(
            existing is null ? "Registrar venda" : "Editar venda",
            "Baixe estoque e registre o valor vendido.",
            existing is null ? "Registrar venda" : "Salvar alterações");
        shell.Dialog.Width = 780;
        AddDialogSection(shell.Body, "Produto vendido", "Venda com baixa automática de estoque.");
        var productCombo = AddDialogComboField(shell.Body, "Produto", _data.Products.OrderBy(item => item.Name), selectedProduct, editable: false);
        productCombo.DisplayMemberPath = nameof(ProductItem.Name);
        var saleRow = AddDialogColumns(shell.Body);
        var quantityBox = AddDialogTextField(saleRow.Left, "Quantidade", (existing?.Quantity ?? 1).ToString(Brazil), "Ex: 2");
        var discountBox = AddDialogTextField(saleRow.Right, "Desconto", (existing?.Discount ?? 0).ToString("N2", Brazil), "Ex: 5,00");
        var customerBox = AddDialogComboField(shell.Body, "Cliente", _data.Customers.Select(item => item.Name).Distinct().OrderBy(item => item), existing?.CustomerName ?? "", editable: true);
        var methodBox = AddDialogComboField(shell.Body, "Forma de pagamento", PaymentMethodOptions(), string.IsNullOrWhiteSpace(existing?.PaymentMethod) ? "Pix" : existing.PaymentMethod, editable: true);
        AddDialogInfoCard(shell.Body, "Mercado Pago na maquininha", MercadoPagoPaymentHintText(), IsMercadoPagoPointReady() ? "#DCFCE7" : "#FFF7ED", IsMercadoPagoPointReady() ? "#16A34A" : "#D97706");
        var notesBox = AddDialogTextField(shell.Body, "Observações", existing?.Notes ?? "", "Ex: retirada no balcão, venda junto ao atendimento", multiline: true);

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
        return shell.Dialog.ShowDialog() == true ? result : null;
    }

    private (Window Dialog, StackPanel Body, TextBlock ErrorText, Button PrimaryButton) CreateEditorDialog(string title, string subtitle, string primaryText)
    {
        var body = new StackPanel { Margin = new Thickness(24, 0, 24, 0), MinWidth = 700 };
        var errorText = new TextBlock
        {
            Foreground = Solid("#DC2626"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(22, 12, 22, 0)
        };
        var primaryButton = new Button
        {
            Content = primaryText,
            Style = (Style)FindResource("CommandButton"),
            Height = 40,
            MinWidth = 150,
            IsDefault = true
        };

        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Width = 780,
            MaxHeight = 840,
            SizeToContent = SizeToContent.Height,
            Background = Brushes.White
        };
        dialog.PreviewKeyDown += AppointmentEditorForm_PreviewKeyDown;

        var content = new DockPanel { LastChildFill = true };
        var header = new Border
        {
            Background = Solid("#F8FAFC"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(22, 18, 22, 18),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, Foreground = InkBrush, FontSize = 22, FontWeight = FontWeights.Bold },
                    new TextBlock { Text = subtitle, Foreground = MutedBrush, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) }
                }
            }
        };
        DockPanel.SetDock(header, Dock.Top);
        content.Children.Add(header);

        var footer = new Border
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
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
                Margin = new Thickness(0, 18, 0, 18),
                Children = { body, errorText }
            }
        };
        content.Children.Add(scroll);
        dialog.Content = content;
        return (dialog, body, errorText, primaryButton);
    }

    private TextBox AddDialogTextField(StackPanel body, string label, string value, string hint, bool multiline = false)
    {
        body.Children.Add(DialogLabel(label));
        var input = new TextBox
        {
            Text = value,
            Tag = hint,
            Height = multiline ? 88 : 42,
            MinWidth = 240,
            Padding = new Thickness(12, multiline ? 9 : 7, 12, 7),
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            AcceptsReturn = multiline,
            VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 12)
        };
        body.Children.Add(input);
        return input;
    }

    private ComboBox AddDialogComboField<T>(StackPanel body, string label, IEnumerable<T> items, object? selected, bool editable)
    {
        body.Children.Add(DialogLabel(label));
        var combo = new ComboBox
        {
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

        body.Children.Add(combo);
        return combo;
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

    private static void AddDialogSection(StackPanel body, string title, string subtitle)
    {
        body.Children.Add(new Border
        {
            Background = Solid("#F8FAFC"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
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
            CornerRadius = new CornerRadius(10),
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
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
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

        var result = MessageBox.Show(
            $"Excluir o agendamento de {_selectedAppointment.CustomerName} em {_selectedAppointment.Start:dd/MM HH:mm}?",
            "Excluir agendamento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
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

        _selectedAppointment.Status = status;
        _selectedAppointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(_selectedAppointment.Id);
        LoadEditor(_selectedAppointment);
        ShowStatus($"{_selectedAppointment.CustomerName}: status alterado para {StatusLabel(status)}.");
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

        if (AppointmentDatePicker.SelectedDate is not DateTime date)
        {
            ShowStatus("Informe a data do agendamento.");
            return false;
        }

        if (!TryParseTime(TimeCombo.Text, out var time))
        {
            ShowStatus("Informe a hora no formato 08:30.");
            return false;
        }

        if (!TryReadDuration(out var duration))
        {
            ShowStatus("Informe uma duração válida entre 5 e 480 minutos.");
            return false;
        }

        var segment = AppointmentSegmentCombo.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(segment))
        {
            ShowStatus("Escolha o segmento do atendimento.");
            return false;
        }

        var service = ServiceCombo.SelectedItem as ServiceItem;
        if (!block && service is null)
        {
            ShowStatus("Escolha um serviço.");
            return false;
        }

        var professional = ProfessionalCombo.SelectedItem as Professional;
        if (professional is null)
        {
            ShowStatus("Escolha um profissional.");
            return false;
        }

        var customerName = block ? "Horário bloqueado" : CustomerNameTextBox.Text.Trim();
        if (!block && string.IsNullOrWhiteSpace(customerName))
        {
            ShowStatus("Informe o cliente, paciente, tutor ou veículo.");
            return false;
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
                ShowStatus("Informe um valor válido.");
                return false;
            }
        }

        var start = date.Date.Add(time);
        draft = new AppointmentDraft(
            segment,
            customerName,
            PhoneTextBox.Text.Trim(),
            CustomerProfileTextBox.Text.Trim(),
            service?.Id ?? "",
            block ? "Bloqueio interno" : service?.Name ?? "Atendimento",
            professional.Id,
            professional.Name,
            ResourceCombo.Text.Trim(),
            start,
            duration,
            price,
            NotesTextBox.Text.Trim());

        return true;
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
            or AppointmentStatus.InService
            or AppointmentStatus.Blocked;

    private void ShowConflict(IReadOnlyCollection<Appointment> conflicts)
    {
        var conflict = conflicts.OrderBy(item => item.Start).First();
        MessageBox.Show(
            $"Horário ocupado: {conflict.Start:dd/MM HH:mm} - {conflict.End:HH:mm}\n{conflict.CustomerName}\n{conflict.ProfessionalName} / {conflict.ResourceName}",
            "Conflito de agenda",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        ShowStatus("Conflito encontrado. Escolha outro horário, profissional ou recurso.");
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

        DayAgendaList.SelectedItem = dayRow;
        WeekAgendaList.SelectedItem = dayRow is null ? weekRow : null;
        _syncingSelection = false;
    }

    private void DateFilterButton_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        var today = DateTime.Today;
        for (var offset = -2; offset <= 21; offset++)
        {
            var date = today.AddDays(offset);
            var item = new MenuItem
            {
                Header = DateShortcutLabel(date),
                FontWeight = date.Date == _selectedDate.Date ? FontWeights.Bold : FontWeights.Normal
            };
            item.Click += (_, _) => SelectDate(date);
            menu.Items.Add(item);
        }

        menu.PlacementTarget = DateFilterButton;
        menu.IsOpen = true;
    }

    private void SegmentFilterButton_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
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
            FontWeight = segment == _selectedSegmentFilter ? FontWeights.Bold : FontWeights.Normal
        };
        item.Click += (_, _) => SelectSegmentFilter(segment);
        menu.Items.Add(item);
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

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshAll();

    private void AppointmentSegmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppointmentSegmentCombo.SelectedItem is string segment)
        {
            UpdateAppointmentOptions(segment);
        }
    }

    private void ServiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loadingEditor && ServiceCombo.SelectedItem is ServiceItem service)
        {
            ApplyServiceDefaults(service);
        }
    }

    private void ScheduleAppointment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Appointment appointment })
        {
            return;
        }

        e.Handled = true;
        _selectedAppointment = appointment;

        _syncingSelection = true;
        DayAgendaList.SelectedItem = _dayRows.FirstOrDefault(item => item.Appointment.Id == appointment.Id);
        WeekAgendaList.SelectedItem = null;
        _syncingSelection = false;

        LoadEditor(appointment);
        OpenAppointmentEditorModal();
        ShowStatus($"{appointment.CustomerName} selecionado no quadro.");
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
            ResourceCombo.Text = service.DefaultResource;
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

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ClearEditor();
        AgendaTabs.SelectedIndex = 0;
        OpenAppointmentEditorModal();
        ShowStatus("Novo agendamento iniciado.");
    }

    private void ClearEditorButton_Click(object sender, RoutedEventArgs e)
    {
        ClearEditor();
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

    private string SuggestedTimeFor(DateTime date)
    {
        var baseTime = date.Date == DateTime.Today
            ? DateTime.Now.AddMinutes(15 - DateTime.Now.Minute % 15)
            : date.Date.AddHours(_data.Settings.WorkdayStartHour);

        if (baseTime.Hour < _data.Settings.WorkdayStartHour || baseTime.Hour >= _data.Settings.WorkdayEndHour)
        {
            baseTime = date.Date.AddHours(_data.Settings.WorkdayStartHour);
        }

        return baseTime.ToString("HH:mm", Brazil);
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
        AppointmentStatus.Scheduled => Solid("#3764A6"),
        AppointmentStatus.Confirmed => AccentBrush,
        AppointmentStatus.Waiting => Solid("#B08A1A"),
        AppointmentStatus.InService => Solid("#B96F3A"),
        AppointmentStatus.Done => Solid("#4B8B36"),
        AppointmentStatus.Cancelled => Solid("#9C2C26"),
        AppointmentStatus.NoShow => Solid("#9C2C26"),
        AppointmentStatus.Blocked => Solid("#68746E"),
        _ => AccentBrush
    };

    private static Brush Solid(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }

    private void ShowStatus(string message)
    {
        // Mensagens de fluxo continuam centralizadas aqui, mas o rodape visual foi removido.
    }

    public sealed record WhatsAppConversationRow(
        string Title,
        string Phone,
        string Preview,
        string Detail,
        DateTime LastAt,
        int UnreadCount);

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

    private sealed record CustomerEditorForm(
        string Name,
        string Phone,
        string Email,
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
        public string Message { get; init; } = "";
        public string State { get; init; } = "";
        public string QrBase64 { get; init; } = "";
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

    public sealed record MetricRow(string Label, string Value, string Hint, Brush Background);

    public sealed record HomeMetricRow(string Label, string Value, string Hint, Brush Background);

    public sealed record HomeAgendaSummaryRow(string Time, string CustomerName, string ServiceName, string StatusText, Brush StatusBackground, Brush StatusForeground);

    public sealed record HomeServiceRow(string Name, string CountText);

    public sealed record HomeCustomerSummaryRow(string Name, string Detail);

    public sealed record HomeAlertRow(string Title, string Detail, PackIconKind Icon, Brush AccentBrush, Brush Background, Brush BorderBrush);

    public sealed record HomeFinanceBarRow(string Label, string ValueText, double Percent);

    public sealed record ReportChartRow(
        string Label,
        decimal Value,
        string ValueText,
        double Percent,
        Brush AccentBrush,
        Brush BackgroundBrush);

    public sealed record EstablishmentMetricRow(string Label, string Value, string Hint, Brush Background);

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
        string Id = "");

    public sealed record MarketingContactRow(
        string Name,
        string Detail,
        string Phone,
        string BadgeText,
        string MessagePreview,
        Brush BadgeBackground,
        Brush BadgeForeground);

    public sealed record ProfessionalDayRow(string Name, string SegmentLine, string LoadText, Brush LoadBrush);

    public sealed record RecentCustomerRow(string Name, string Detail);

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
                ["Cadeira 1", "Cadeira 2", "Lavatorio", "Coloração"],
                [
                    new("Escova", 45, 70, "Cadeira 1"),
                    new("Corte feminino", 50, 90, "Cadeira 1"),
                    new("Coloração", 120, 240, "Coloração"),
                    new("Hidratação", 60, 120, "Lavatorio")
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
