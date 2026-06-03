using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace AgendaLivre.Windows;

public partial class MainWindow : Window
{
    private const string AllSegments = "Todos";
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

    private readonly AgendaDataStore _store = new();
    private readonly ObservableCollection<AppointmentRow> _dayRows = [];
    private readonly ObservableCollection<AppointmentRow> _weekRows = [];
    private readonly ObservableCollection<MetricRow> _metrics = [];
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

    public MainWindow()
    {
        InitializeComponent();

        _data = _store.LoadOrCreate();
        ConfigureOnboardingInputs();
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
        ApplyBusinessLabels();

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
        businessName.Equals("Balcão Livre", StringComparison.OrdinalIgnoreCase);

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
        ConfigureInputs();
        ClearEditor();
        RefreshAll();
        ApplyBusinessLabels();
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
        AppTitleText.Text = IsDefaultBusinessName(_data.Settings.BusinessName)
            ? "Balcão Livre"
            : _data.Settings.BusinessName;

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

    private void OpenSettingsModal()
    {
        RefreshSettingsSummary();
        SettingsOverlay.Visibility = Visibility.Visible;
    }

    private void CloseSettingsModal()
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsModal();
    }

    private void CloseSettingsModalButton_Click(object sender, RoutedEventArgs e)
    {
        CloseSettingsModal();
    }

    private void OpenOnboardingFromSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        CloseSettingsModal();
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

        CloseSettingsModal();
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
        var name = PromptText("Criar serviço", "Nome do serviço", "");
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
            ShowStatus($"Serviço já existia e foi selecionado: {existing.Name}.");
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
        ShowStatus($"Serviço criado: {service.Name}.");
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
            ShowStatus($"Profissional já existia e foi selecionado: {existing.Name}.");
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
        "Clínica médica" => "Profissional de saúde",
        "Petshop" => "Atendimento pet",
        "Mecânica" => "Mecânico",
        "Unha e beleza" => "Profissional de beleza",
        "Unha e beleza + salão" => "Profissional de beleza",
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
