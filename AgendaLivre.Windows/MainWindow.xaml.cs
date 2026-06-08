using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");
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
    private readonly ObservableCollection<ProfessionalDayRow> _professionalRows = [];
    private readonly ObservableCollection<RecentCustomerRow> _recentCustomers = [];
    private readonly ObservableCollection<ServiceItem> _filteredServices = [];
    private readonly ObservableCollection<Professional> _filteredProfessionals = [];
    private readonly ObservableCollection<string> _resourceOptions = [];
    private readonly IReadOnlyList<OnboardingTemplate> _onboardingTemplates = OnboardingTemplate.CreateDefaults();
    private static readonly string[] OnboardingStepTitles =
    [
        "Dados iniciais",
        "Segmento do negÃ³cio",
        "Tamanho da equipe",
        "Objetivo principal",
        "EndereÃ§o",
        "Senha de acesso"
    ];
    private static readonly string[] OnboardingStepCaptions =
    [
        "Identifique o responsÃ¡vel e o nome que aparecerÃ¡ no sistema.",
        "Escolha o setor para carregar serviÃ§os e recursos mais prÃ³ximos da sua rotina.",
        "Informe quantas pessoas atendem para preparar a agenda do tamanho certo.",
        "Marque a prioridade inicial para a configuraÃ§Ã£o nascer alinhada ao seu uso.",
        "Cadastre onde o negÃ³cio funciona para consultas e relatÃ³rios.",
        "Defina a senha que libera o acesso ao sistema."
    ];

    private readonly string[] _segments =
    [
        "ClÃ­nica mÃ©dica",
        "Petshop",
        "MecÃ¢nica",
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

        _data = _store.LoadOrCreate();
        ConfigureOnboardingInputs();
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
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
        businessName.Equals("BalcÃ£o Livre", StringComparison.OrdinalIgnoreCase);

    private string BusinessDisplayName() =>
        IsDefaultBusinessName(_data.Settings.BusinessName)
            ? "Balcão Livre"
            : _data.Settings.BusinessName;

    private void ShowOnboarding()
    {
        OnboardingOverlay.Visibility = Visibility.Visible;
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
            ShowStatus("Informe um e-mail vÃ¡lido antes de continuar.");
            InitialEmailTextBox.Focus();
            return false;
        }

        var businessName = ToNameCase(InitialBusinessNameTextBox.Text);
        if (string.IsNullOrWhiteSpace(businessName))
        {
            ShowStatus("Informe o nome do negÃ³cio antes de continuar.");
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
            ShowStatus("Escolha o segmento do negÃ³cio antes de continuar.");
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
            ShowStatus("Informe pelo menos o CEP ou o logradouro do negÃ³cio.");
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

        var template = _selectedOnboardingTemplate ?? CreateBusinessTemplate("SalÃ£o de Beleza");
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
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
        ApplyBusinessLabels();
        ShowMainPage(MainPage.Home);
        ShowStatus($"Conta criada para {template.Title}. A agenda estÃ¡ pronta para uso.");
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
            return CreateBusinessTemplate("SalÃ£o de Beleza");
        }

        return CreateBusinessTemplate(segment);
    }

    private OnboardingTemplate CreateBusinessTemplate(string businessType)
    {
        var trimmed = businessType.Trim();
        return trimmed switch
        {
            "SalÃ£o de Beleza" or "Unha e beleza + salÃ£o" => RenameTemplate(
                OnboardingTemplate.CreateIntegratedBeauty(),
                "SalÃ£o de Beleza",
                "Meu salÃ£o de beleza",
                "Agenda para salÃ£o com cabelo, unha, estÃ©tica, lavatÃ³rio, cadeiras e profissionais.",
                "Cliente: Camila | Escova + manicure | Cadeira 1 / Mesa 1"),
            "Barbearia" or "Cabelo e barbearia" => RenameTemplate(
                TemplateByTitle("Barbearia"),
                "Barbearia",
                "Minha barbearia",
                "Agenda para cortes, barba, combos, preferÃªncias do cliente e cadeiras de atendimento.",
                "Cliente: AndrÃ© | Corte + barba | Cadeira 1"),
            "Esmalteria" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Esmalteria",
                "Minha esmalteria",
                "Agenda para manicure, pedicure, alongamento, design e mesas de atendimento.",
                "Cliente: Camila | Alongamento almond | Mesa 2"),
            "Centro de EstÃ©tica" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Centro de EstÃ©tica",
                "Meu centro de estÃ©tica",
                "Agenda para procedimentos, avaliaÃ§Ã£o, retorno, preferÃªncias e salas de atendimento.",
                "Cliente: Larissa | Limpeza de pele | Sala estÃ©tica 1"),
            "Podologia" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Podologia",
                "Minha clÃ­nica de podologia",
                "Agenda para avaliaÃ§Ã£o, retorno, procedimento, observaÃ§Ãµes e sala de atendimento.",
                "Cliente: Renata | AvaliaÃ§Ã£o podolÃ³gica | Sala 1"),
            "Spa" => RenameTemplate(
                TemplateBySegment(OnboardingTemplate.NailsSegment),
                "Spa",
                "Meu spa",
                "Agenda para terapias, massagens, salas, pacotes e preferÃªncias do cliente.",
                "Cliente: Marina | Massagem relaxante | Sala 1"),
            "ClÃ­nica mÃ©dica" => TemplateBySegment("ClÃ­nica mÃ©dica"),
            "Petshop" => TemplateBySegment("Petshop"),
            "MecÃ¢nica" or "Oficina" => RenameTemplate(
                TemplateBySegment("MecÃ¢nica"),
                "Oficina",
                "Minha oficina",
                "Agenda para diagnÃ³sticos, revisÃµes, veÃ­culos, box e acompanhamento de entrega.",
                "Cliente: Lucas | Onix ABC1D23 | DiagnÃ³stico | Box 1"),
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
            "Meu negÃ³cio",
            "Agenda simples para organizar clientes, profissionais, serviÃ§os e locais de atendimento.",
            "Cliente: Ana | Atendimento | Profissional 1 | Sala 1",
            "Cliente",
            "ObservaÃ§Ã£o / preferÃªncia / motivo",
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

        error = "Informe CPF com 11 dÃ­gitos ou CNPJ com 14 dÃ­gitos.";
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

        error = "Informe telefone com DDD e 10 ou 11 dÃ­gitos.";
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
            AppSubtitleText.Text = "Atendimento, agenda e gestÃ£o em um sÃ³ lugar";
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
        AgendaSubtitleText.Text = $"{dateText} | {_dayRows.Count} item(ns) visÃ­veis";
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
            var detailParts = new[] { customer.Phone, customer.Profile, customer.Segment }
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
                $"{service.DurationMinutes} min",
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

        _financeMetrics.Clear();
        _financeMetrics.Add(new EstablishmentMetricRow("Receita de hoje", receivedToday.ToString("C0", Brazil), "serviços, produtos e pagamentos", AccentSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("Receita do mês", receivedMonth.ToString("C0", Brazil), "total recebido no mês", BlueSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("A receber", pendingValue.ToString("C0", Brazil), $"{pending.Count} pagamento(s) pendente(s)", YellowSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("Despesas do mês", monthExpenses.ToString("C0", Brazil), $"{_data.Expenses.Count(item => item.Date >= monthStart && item.Date < nextMonth)} lançamento(s)", RedSoftBrush));
        _financeMetrics.Add(new EstablishmentMetricRow("Saldo do mês", monthBalance.ToString("C0", Brazil), "receita menos despesas", monthBalance >= 0 ? AccentSoftBrush : RedSoftBrush));

        _financeEntries.Clear();
        var maxEntry = Math.Max(1m, Math.Max(serviceMonth, Math.Max(productMonth, manualMonth)));
        _financeEntries.Add(new HomeFinanceBarRow("Receita de serviços", serviceMonth.ToString("C0", Brazil), Percent(serviceMonth, maxEntry)));
        _financeEntries.Add(new HomeFinanceBarRow("Receita de produtos", productMonth.ToString("C0", Brazil), Percent(productMonth, maxEntry)));
        _financeEntries.Add(new HomeFinanceBarRow("Pagamentos manuais", manualMonth.ToString("C0", Brazil), Percent(manualMonth, maxEntry)));

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
            _financePendingPayments.Add(EmptyEstablishmentRow("Nenhum pagamento pendente", "Atendimentos em aberto aparecerão aqui.", "R$ 0"));
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
            _financeExpenses.Add(EmptyEstablishmentRow("Nenhuma despesa cadastrada", "Cadastre despesas fixas ou avulsas nesta página.", "R$ 0"));
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

    private void OpenMarketingWhatsApp(MarketingContactRow row)
    {
        var phone = NormalizeBrazilPhone(row.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus($"Telefone não cadastrado para {row.Name}.");
            return;
        }

        var message = BuildMarketingMessage(row.Name);
        var url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(message)}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        ShowStatus($"WhatsApp aberto para {row.Name}.");
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
            return $"AmanhÃ£, {date:dd/MM}";
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
                "ClÃ­nica mÃ©dica" => "Paciente / prontuÃ¡rio",
                "Petshop" => "Tutor / pet / raÃ§a",
                "MecÃ¢nica" => "Cliente / veÃ­culo / placa",
                "Unha e beleza" => "Cliente / preferÃªncia",
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

        EditorTitleText.Text = appointment.Status == AppointmentStatus.Blocked ? "Bloqueio de horÃ¡rio" : "Editar agendamento";
        EditorStatusText.Text = $"{StatusLabel(appointment.Status)} | criado em {appointment.CreatedAt:dd/MM HH:mm}";
        ShowSelectedAppointment(appointment);
        _loadingEditor = false;
    }

    private void OpenAppointmentEditorModal()
    {
        AppointmentEditorOverlay.Visibility = Visibility.Visible;
    }

    private void CloseAppointmentEditorModal()
    {
        AppointmentEditorOverlay.Visibility = Visibility.Collapsed;
    }

    private void CloseAppointmentModalButton_Click(object sender, RoutedEventArgs e)
    {
        CloseAppointmentEditorModal();
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
        ClearEditor();
        CustomerNameTextBox.Focus();
        OpenAppointmentEditorModal();
        ShowStatus("Novo cliente iniciado. Informe os dados no agendamento.");
    }

    private void RegisterPaymentQuickButton_Click(object sender, RoutedEventArgs e)
    {
        RegisterPaymentButton_Click(sender, e);
    }

    private void RegisterPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        var description = PromptText("Registrar pagamento", "Descrição do pagamento", "Pagamento avulso");
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var customer = PromptText("Registrar pagamento", "Cliente (opcional)", "");
        var valueText = PromptText("Registrar pagamento", "Valor recebido", "0,00");
        if (string.IsNullOrWhiteSpace(valueText) || !TryParseMoney(valueText, out var value) || value <= 0)
        {
            ShowStatus("Informe um valor válido para registrar o pagamento.");
            return;
        }

        _data.ManualPayments.Add(new ManualPayment
        {
            Description = description.Trim(),
            CustomerName = customer?.Trim() ?? "",
            Value = value,
            PaidAt = DateTime.Now
        });

        _store.Save(_data);
        RefreshAll();
        ShowMainPage(MainPage.Finance);
        ShowStatus($"Pagamento registrado: {value.ToString("C", Brazil)}.");
    }

    private void NewExpenseButton_Click(object sender, RoutedEventArgs e)
    {
        var description = PromptText("Nova despesa", "Descrição da despesa", "");
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var category = PromptText("Nova despesa", "Categoria", "Operacional");
        var valueText = PromptText("Nova despesa", "Valor", "0,00");
        if (string.IsNullOrWhiteSpace(valueText) || !TryParseMoney(valueText, out var value) || value <= 0)
        {
            ShowStatus("Informe um valor válido para cadastrar a despesa.");
            return;
        }

        _data.Expenses.Add(new ExpenseItem
        {
            Description = description.Trim(),
            Category = category?.Trim() ?? "",
            Value = value,
            Date = DateTime.Now,
            IsPaid = true
        });

        _store.Save(_data);
        RefreshAll();
        ShowMainPage(MainPage.Finance);
        ShowStatus($"Despesa cadastrada: {value.ToString("C", Brazil)}.");
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

    private void OpenFirstMarketingWhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        var row = _marketingContacts.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Phone));
        if (row is null)
        {
            ShowStatus("Nenhum cliente com telefone disponível para WhatsApp.");
            return;
        }

        OpenMarketingWhatsApp(row);
    }

    private void OpenMarketingWhatsAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MarketingContactRow row })
        {
            OpenMarketingWhatsApp(row);
        }
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
        var name = PromptText("Criar produto", "Nome do produto", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var category = PromptText("Criar produto", "Categoria do produto (opcional)", "");
        var priceText = PromptText("Criar produto", "Preço de venda", "0,00");
        var stockText = PromptText("Criar produto", "Quantidade em estoque", "0");

        var product = new ProductItem
        {
            Name = name.Trim(),
            Category = category?.Trim() ?? "",
            Price = !string.IsNullOrWhiteSpace(priceText) && TryParseMoney(priceText, out var price) ? price : 0m,
            StockQuantity = int.TryParse(stockText, NumberStyles.Integer, Brazil, out var stock) ? Math.Max(0, stock) : 0
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

        var defaultProduct = _data.Products.OrderBy(item => item.Name).First();
        var productName = PromptText("Registrar venda", "Produto vendido", defaultProduct.Name);
        if (string.IsNullOrWhiteSpace(productName))
        {
            return;
        }

        var product = _data.Products.FirstOrDefault(item => item.Name.Equals(productName.Trim(), StringComparison.OrdinalIgnoreCase))
                      ?? defaultProduct;
        var quantityText = PromptText("Registrar venda", "Quantidade", "1");
        var customerName = PromptText("Registrar venda", "Cliente (opcional)", "");
        var quantity = int.TryParse(quantityText, NumberStyles.Integer, Brazil, out var parsedQuantity)
            ? Math.Max(1, parsedQuantity)
            : 1;

        _data.ProductSales.Add(new ProductSale
        {
            ProductId = product.Id,
            ProductName = product.Name,
            CustomerName = customerName?.Trim() ?? "",
            Quantity = quantity,
            UnitPrice = product.Price,
            SoldAt = DateTime.Now
        });

        product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Venda registrada: {quantity}x {product.Name}.");
    }

    private void OpenEstablishmentSectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: EstablishmentSectionRow row })
        {
            ShowStatus($"{row.Title}: seção aberta em Meu estabelecimento.");
        }
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
            "VocÃª vai sair da agenda atual e voltar para a configuraÃ§Ã£o inicial.\n\nOs dados atuais continuam salvos neste computador. Eles sÃ³ serÃ£o substituÃ­dos se vocÃª criar outra agenda.",
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

        _data.Settings.BusinessName = "BalcÃ£o Livre";
        _data.Settings.BusinessDocument = "";
        _data.Settings.BusinessPhone = "";
        _data.Settings.BusinessAddress = "";
        _data.Settings.AccountFullName = "";
        _data.Settings.AccountPhone = "";
        _data.Settings.AccountEmail = "";
        _data.Settings.BusinessSegment = "";
        _data.Settings.ClientLabel = "Cliente";
        _data.Settings.ClientDetailLabel = "Paciente / pet / veÃ­culo / preferÃªncia";
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
        _data.Settings.OnboardingCompleted = false;

        _store.Save(_data);
        ApplyBusinessLabels();
        ConfigureInputs();
        ConfigureOnboardingInputs();
        ClearEditor();
        RefreshAll();
        ShowOnboarding();
        ShowStatus("VocÃª saiu do sistema atual. Escolha um setor para iniciar outra agenda.");
    }

    private void RefreshSettingsSummary()
    {
        var businessParts = new[]
        {
            IsDefaultBusinessName(_data.Settings.BusinessName) ? "BalcÃ£o Livre" : _data.Settings.BusinessName,
            _data.Settings.BusinessSegment,
            _data.Settings.BusinessDocument,
            _data.Settings.BusinessPhone
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        SettingsBusinessText.Text = string.Join(" | ", businessParts);
        ServicesCountText.Text = $"{_data.Services.Count} serviÃ§o(s) cadastrados";
        ProfessionalsCountText.Text = $"{_data.Professionals.Count} profissional(is) cadastrados";
        ResourcesCountText.Text = $"{_data.Settings.Resources.Count} sala(s) ou recurso(s)";
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
        var name = PromptText("Criar serviÃ§o", "Nome do serviÃ§o", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var existing = _data.Services.FirstOrDefault(item =>
            item.Segment == segment &&
            item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            ServiceCombo.SelectedItem = existing;
            RefreshSettingsSummary();
            ShowStatus($"ServiÃ§o jÃ¡ existia e foi selecionado: {existing.Name}.");
            return;
        }

        var duration = ReadCurrentDurationOrDefault();
        var price = TryParseMoney(PriceTextBox.Text, out var parsedPrice) ? parsedPrice : 0m;
        var resource = CurrentResourceText();
        var service = new ServiceItem
        {
            Segment = segment,
            Name = name.Trim(),
            DurationMinutes = duration,
            Price = price,
            DefaultResource = resource
        };

        _data.Services.Add(service);
        _store.Save(_data);
        UpdateAppointmentOptions(segment);
        ServiceCombo.SelectedItem = _filteredServices.FirstOrDefault(item => item.Id == service.Id);
        ApplyServiceDefaults(service);
        RefreshSettingsSummary();
        RefreshAll();
        ShowStatus($"ServiÃ§o criado: {service.Name}.");
    }

    private void CreateProfessionalButton_Click(object sender, RoutedEventArgs e)
    {
        var segment = CurrentEditorSegment();
        var name = PromptText("Criar profissional", "Nome do profissional", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var existing = _data.Professionals.FirstOrDefault(item =>
            item.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            item.Segments.Contains(segment));
        if (existing is not null)
        {
            ProfessionalCombo.SelectedItem = existing;
            RefreshSettingsSummary();
            ShowStatus($"Profissional jÃ¡ existia e foi selecionado: {existing.Name}.");
            return;
        }

        var professional = new Professional
        {
            Name = name.Trim(),
            Role = DefaultRoleForSegment(segment),
            Segments = [segment]
        };

        _data.Professionals.Add(professional);
        _store.Save(_data);
        UpdateAppointmentOptions(segment);
        ProfessionalCombo.SelectedItem = _filteredProfessionals.FirstOrDefault(item => item.Id == professional.Id);
        RefreshSettingsSummary();
        RefreshAll();
        ShowStatus($"Profissional criado: {professional.Name}.");
    }

    private void CreateResourceButton_Click(object sender, RoutedEventArgs e)
    {
        var initial = CurrentResourceText();
        var resourceName = PromptText("Criar sala ou recurso", "Nome da sala, box, cadeira ou mesa", initial);
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return;
        }

        resourceName = resourceName.Trim();
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
        "ClÃ­nica mÃ©dica" => "Profissional de saÃºde",
        "Petshop" => "Atendimento pet",
        "MecÃ¢nica" => "MecÃ¢nico",
        "Unha e beleza" => "Profissional de beleza",
        "Unha e beleza + salÃ£o" => "Profissional de beleza",
        "Cabelo e barbearia" => "Cabeleireiro",
        _ => "Profissional"
    };

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
        SaveAndRefresh(target.Id, $"Agendamento salvo para {target.CustomerName} Ã s {target.Start:HH:mm}.");
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
            CustomerName = "HorÃ¡rio bloqueado",
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
            Notes = string.IsNullOrWhiteSpace(draft.Notes) ? "HorÃ¡rio indisponÃ­vel para novos agendamentos." : draft.Notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _data.Appointments.Add(appointment);
        SaveAndRefresh(appointment.Id, $"HorÃ¡rio bloqueado em {appointment.Start:dd/MM HH:mm}.");
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
        ShowStatus($"Agendamento de {removedName} excluÃ­do.");
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
            ShowStatus("Bloqueios podem ser cancelados ou excluÃ­dos.");
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
            ShowStatus("Informe uma duraÃ§Ã£o vÃ¡lida entre 5 e 480 minutos.");
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
            ShowStatus("Escolha um serviÃ§o.");
            return false;
        }

        var professional = ProfessionalCombo.SelectedItem as Professional;
        if (professional is null)
        {
            ShowStatus("Escolha um profissional.");
            return false;
        }

        var customerName = block ? "HorÃ¡rio bloqueado" : CustomerNameTextBox.Text.Trim();
        if (!block && string.IsNullOrWhiteSpace(customerName))
        {
            ShowStatus("Informe o cliente, paciente, tutor ou veÃ­culo.");
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
                ShowStatus("Informe um valor vÃ¡lido.");
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
            $"HorÃ¡rio ocupado: {conflict.Start:dd/MM HH:mm} - {conflict.End:HH:mm}\n{conflict.CustomerName}\n{conflict.ProfessionalName} / {conflict.ResourceName}",
            "Conflito de agenda",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        ShowStatus("Conflito encontrado. Escolha outro horÃ¡rio, profissional ou recurso.");
    }

    private void UpsertCustomer(Appointment appointment)
    {
        if (appointment.Status == AppointmentStatus.Blocked ||
            string.IsNullOrWhiteSpace(appointment.CustomerName) ||
            appointment.CustomerName.Equals("HorÃ¡rio bloqueado", StringComparison.OrdinalIgnoreCase))
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
        ShowStatus($"Novo horÃ¡rio preparado para {slot.Professional.Name} Ã s {slot.Start:HH:mm}.");
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
        ShowStatus("FormulÃ¡rio limpo.");
    }

    private void CopySummaryButton_Click(object sender, RoutedEventArgs e)
    {
        var summary = BuildSummaryText();
        Clipboard.SetText(summary);
        ShowStatus("Resumo do dia copiado para a Ã¡rea de transferÃªncia.");
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

        document.Blocks.Add(new Paragraph(new Run($"BalcÃ£o Livre - {_selectedDate:dddd, dd/MM/yyyy}"))
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
        AddCell(header, "ServiÃ§o");
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
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"BalcÃ£o Livre {_selectedDate:yyyy-MM-dd}");
            ShowStatus("Agenda enviada para impressÃ£o.");
        }
    }

    private string BuildSummaryText()
    {
        var builder = new StringBuilder();
        var rows = ApplyFilters(_data.Appointments.Where(item => item.Start.Date == _selectedDate.Date))
            .OrderBy(item => item.Start)
            .ToList();

        builder.AppendLine($"BalcÃ£o Livre - {_selectedDate:dddd, dd/MM/yyyy}");
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

    private void ShowStatus(string message) => StatusTextBlock.Text = $"{DateTime.Now:HH:mm} | {message}";

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
        Brush BadgeForeground);

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
        public const string IntegratedBeautySegment = "Unha e beleza + salÃ£o";
        public const string SalonTitle = "Cabelo / salÃ£o";

        public static OnboardingTemplate CreateIntegratedBeauty() =>
            new(
                "Unha / beleza + salÃ£o",
                IntegratedBeautySegment,
                "Meu studio integrado",
                "Cria uma agenda Ãºnica para unha, design, cabelo, escova, coloraÃ§Ã£o, lavatÃ³rio, cadeiras e profissionais do salÃ£o.",
                "Cliente: Camila | Alongamento + escova | Mesa 2 / Cadeira 1",
                "Cliente",
                "PreferÃªncia / quÃ­mica / alergia / estilo",
                "Mesa, cadeira ou lavatÃ³rio",
                9,
                20,
                ["Mesa 1", "Mesa 2", "Cadeira 1", "Cadeira 2", "LavatÃ³rio", "ColoraÃ§Ã£o"],
                [
                    new("Manicure", 45, 55, "Mesa 1"),
                    new("Pedicure", 45, 60, "Mesa 1"),
                    new("Alongamento de unha", 120, 180, "Mesa 2"),
                    new("Sobrancelha", 30, 45, "Mesa 2"),
                    new("Escova", 45, 70, "Cadeira 1"),
                    new("Corte feminino", 50, 90, "Cadeira 1"),
                    new("ColoraÃ§Ã£o", 120, 240, "ColoraÃ§Ã£o"),
                    new("HidrataÃ§Ã£o", 60, 120, "LavatÃ³rio")
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
                "ClÃ­nica mÃ©dica",
                "ClÃ­nica mÃ©dica",
                "Minha clÃ­nica",
                "Controla paciente, prontuÃ¡rio, profissional, sala, consulta, retorno, encaixe e chegada.",
                "Paciente: Maria Souza | ProntuÃ¡rio 0321 | Consulta mÃ©dica | ConsultÃ³rio 1",
                "Paciente",
                "ProntuÃ¡rio / convÃªnio / motivo",
                "Sala ou consultÃ³rio",
                8,
                18,
                ["ConsultÃ³rio 1", "ConsultÃ³rio 2", "Sala de exames"],
                [
                    new("Consulta mÃ©dica", 45, 180, "ConsultÃ³rio 1"),
                    new("Retorno", 30, 90, "ConsultÃ³rio 1"),
                    new("Exame simples", 30, 120, "Sala de exames"),
                    new("Encaixe", 20, 80, "ConsultÃ³rio 2")
                ],
                [
                    new("Profissional 1", "MÃ©dico"),
                    new("Profissional 2", "MÃ©dico")
                ]),
            new(
                "Petshop",
                "Petshop",
                "Meu petshop",
                "Controla tutor, pet, raÃ§a, porte, banho, tosa, vacinaÃ§Ã£o, veterinÃ¡rio e baia de espera.",
                "Tutor: JoÃ£o | Pet: Nina, Spitz | Banho e tosa | Tosa 1",
                "Tutor / pet",
                "RaÃ§a / porte / observaÃ§Ã£o do pet",
                "Sala, baia ou mesa",
                8,
                19,
                ["Banho 1", "Tosa 1", "Sala veterinÃ¡ria", "Baia de espera"],
                [
                    new("Banho", 60, 70, "Banho 1"),
                    new("Banho e tosa", 90, 110, "Tosa 1"),
                    new("Consulta veterinÃ¡ria", 40, 160, "Sala veterinÃ¡ria"),
                    new("VacinaÃ§Ã£o", 25, 85, "Sala veterinÃ¡ria")
                ],
                [
                    new("Tosador 1", "Banho e tosa"),
                    new("VeterinÃ¡rio 1", "VeterinÃ¡rio")
                ]),
            new(
                "MecÃ¢nica",
                "MecÃ¢nica",
                "Minha oficina",
                "Controla cliente, veÃ­culo, placa, problema relatado, box, diagnÃ³stico, revisÃ£o e entrega.",
                "Cliente: Lucas | VeÃ­culo: Onix ABC1D23 | Troca de Ã³leo | Box 1",
                "Cliente / veÃ­culo",
                "Placa / modelo / problema",
                "Box ou elevador",
                8,
                18,
                ["Box 1", "Box 2", "Elevador 1", "DiagnÃ³stico"],
                [
                    new("DiagnÃ³stico", 60, 120, "DiagnÃ³stico"),
                    new("Troca de Ã³leo", 45, 90, "Box 1"),
                    new("RevisÃ£o completa", 150, 420, "Box 2"),
                    new("Alinhamento", 50, 130, "Elevador 1")
                ],
                [
                    new("MecÃ¢nico 1", "MecÃ¢nico"),
                    new("Consultor tÃ©cnico", "RecepÃ§Ã£o tÃ©cnica")
                ]),
            new(
                "Barbearia",
                "Cabelo e barbearia",
                "Minha barbearia",
                "Controla cliente, preferÃªncia de corte, barbeiro, cadeira, barba, cabelo e combos.",
                "Cliente: AndrÃ© | DegradÃª baixo | Corte + barba | Cadeira 1",
                "Cliente",
                "Estilo / preferÃªncia / observaÃ§Ã£o",
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
                "Meu salÃ£o",
                "Controla cliente, histÃ³rico, quÃ­mica, cadeira, lavatÃ³rio, escova, coloraÃ§Ã£o e tratamentos.",
                "Cliente: PatrÃ­cia | ColoraÃ§Ã£o sem amÃ´nia | Colorista 1 | Cadeira 2",
                "Cliente",
                "PreferÃªncia / quÃ­mica / histÃ³rico",
                "Cadeira ou lavatÃ³rio",
                9,
                20,
                ["Cadeira 1", "Cadeira 2", "Lavatorio", "ColoraÃ§Ã£o"],
                [
                    new("Escova", 45, 70, "Cadeira 1"),
                    new("Corte feminino", 50, 90, "Cadeira 1"),
                    new("ColoraÃ§Ã£o", 120, 240, "ColoraÃ§Ã£o"),
                    new("HidrataÃ§Ã£o", 60, 120, "Lavatorio")
                ],
                [
                    new("Cabeleireiro 1", "Cabeleireiro"),
                    new("Colorista 1", "Colorista")
                ]),
            new(
                "Unha / beleza",
                "Unha e beleza",
                "Meu studio de beleza",
                "Controla cliente, preferÃªncia, alergias, mesa, manicure, pedicure, alongamento e design.",
                "Cliente: Camila | Alongamento almond | Mesa 2",
                "Cliente",
                "PreferÃªncia / alergia / estilo",
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
