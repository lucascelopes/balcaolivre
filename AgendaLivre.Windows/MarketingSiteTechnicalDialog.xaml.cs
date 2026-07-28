using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MarketingSiteTechnicalDialog : Window
{
    private readonly MarketingSiteTechnicalDialogModel _model;
    private bool _detailsExpanded;

    public MarketingSiteTechnicalDialog(
        MarketingSiteTechnicalDialogModel model,
        bool showDetails = false,
        bool selectCustomDomain = false)
    {
        InitializeComponent();
        _model = model;
        PublishedUrlText.Text = model.PublishedUrl
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
        OnlineStatusText.Text = model.IsOnline ? "Online" : "Requer atenção";
        OnlineStatusText.Foreground = model.IsOnline
            ? FindBrush("DialogSuccess")
            : System.Windows.Media.Brushes.Firebrick;
        OnlineDot.Fill = OnlineStatusText.Foreground;
        CustomDomainTextBox.Text = model.CustomDomain;
        CustomDomainCnameTargetText.Text = model.CnameTarget;
        WildcardRecordText.Text =
            $"*.minhaagendalivre.com.br  →  minhaagendalivre.com.br";
        FallbackOriginText.Text = $"Fallback origin: {model.CnameTarget}";

        if (selectCustomDomain || !string.IsNullOrWhiteSpace(model.CustomDomain))
        {
            CustomDomainTab.IsChecked = true;
        }

        SetDetailsExpanded(showDetails);
        UpdateDomainMode();
    }

    public bool CustomDomainRequested { get; private set; }

    public string CustomDomain => NormalizeDomain(CustomDomainTextBox.Text);

    private System.Windows.Media.Brush FindBrush(string key) =>
        (System.Windows.Media.Brush)FindResource(key);

    private void DomainTab_Checked(object sender, RoutedEventArgs e)
    {
        if (AgendaLivreDomainPanel is null)
        {
            return;
        }

        UpdateDomainMode();
    }

    private void UpdateDomainMode()
    {
        var customMode = CustomDomainTab.IsChecked == true;
        AgendaLivreDomainPanel.Visibility = customMode
            ? Visibility.Collapsed
            : Visibility.Visible;
        CustomDomainPanel.Visibility = customMode
            ? Visibility.Visible
            : Visibility.Collapsed;
        PrimaryActionButton.Content = customMode
            ? "Conectar domínio"
            : "Conectar domínio próprio";
        if (customMode)
        {
            Dispatcher.BeginInvoke(() =>
            {
                CustomDomainTextBox.Focus();
                CustomDomainTextBox.CaretIndex = CustomDomainTextBox.Text.Length;
            });
        }
    }

    private void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (CustomDomainTab.IsChecked != true)
        {
            CustomDomainTab.IsChecked = true;
            return;
        }

        var domain = CustomDomain;
        if (!IsValidDomain(domain))
        {
            CustomDomainErrorText.Text =
                "Informe um domínio válido, como www.seusalao.com.br.";
            CustomDomainErrorText.Visibility = Visibility.Visible;
            CustomDomainTextBox.Focus();
            CustomDomainTextBox.SelectAll();
            return;
        }

        CustomDomainErrorText.Visibility = Visibility.Collapsed;
        CustomDomainRequested = true;
        DialogResult = true;
    }

    private void OpenSiteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_model.PublishedUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                $"Não foi possível abrir o site.\n\n{exception.Message}",
                "Site publicado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e) =>
        SetDetailsExpanded(!_detailsExpanded);

    private void SetDetailsExpanded(bool expanded)
    {
        _detailsExpanded = expanded;
        TechnicalDetailsPanel.Visibility = expanded
            ? Visibility.Visible
            : Visibility.Collapsed;
        DetailsChevron.Kind = expanded
            ? PackIconKind.ChevronUp
            : PackIconKind.ChevronDown;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private static string NormalizeDomain(string? value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            normalized = uri.Host;
        }

        return normalized
            .Trim()
            .Trim('/')
            .TrimEnd('.');
    }

    private static bool IsValidDomain(string value) =>
        value.Contains('.', StringComparison.Ordinal) &&
        Uri.CheckHostName(value) == UriHostNameType.Dns;
}

public sealed record MarketingSiteTechnicalDialogModel(
    string PublishedUrl,
    bool IsOnline,
    string CustomDomain,
    string DnsStatus,
    string SslStatus,
    string CnameTarget);
