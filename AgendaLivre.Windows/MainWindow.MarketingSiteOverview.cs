using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Web.WebView2.Core;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private CancellationTokenSource? _marketingSiteOverviewProbeCancellation;
    private bool _marketingSiteOverviewWebViewConfigured;

    private void ShowMarketingSiteOverview()
    {
        if (MarketingSiteOverviewView is null)
        {
            return;
        }

        EnsureMarketingCatalogAddressState();
        MarketingHubView.Visibility = Visibility.Collapsed;
        MarketingStudioView.Visibility = Visibility.Collapsed;
        MarketingStudioHeader.Visibility = Visibility.Collapsed;
        MarketingSiteEditorView.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewView.Visibility = Visibility.Visible;
        MarketingPostOverviewView.Visibility = Visibility.Collapsed;
        MarketingSitePromotionView.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();

        if (MarketingSiteOverviewDesktopTab is not null)
        {
            MarketingSiteOverviewDesktopTab.IsChecked = true;
        }

        RefreshMarketingSiteOverview();
        MarketingView.ScrollToTop();
    }

    private void RefreshMarketingSiteOverview()
    {
        if (MarketingSiteOverviewTopDomainText is null)
        {
            return;
        }

        var settings = _data.Settings;
        var platformHost = MarketingSiteDisplayUrl();
        var catalogUrl = MarketingSiteOverviewCatalogUrl();
        var publishedAt = settings.PublishedMarketingCatalog?.PublishedAt ??
                          settings.MarketingSitePublishedAt;
        var customDomain = NormalizeMarketingSiteCustomDomain(
            settings.PublishedMarketingCatalog?.CustomDomain ??
            settings.MarketingSiteDraftCustomDomain ??
            settings.PublicBookingCustomDomain);

        MarketingSiteOverviewTopDomainText.Text =
            Uri.TryCreate(catalogUrl, UriKind.Absolute, out var catalogUri)
                ? catalogUri.Host
                : platformHost;
        MarketingSiteOverviewPreviewBusinessText.Text = FirstFilled(
            settings.PublishedMarketingCatalog?.Header?.BusinessName ?? "",
            settings.MarketingSiteHeader?.BusinessName ?? "",
            BusinessDisplayName());
        MarketingSiteOverviewPublishedText.Text = publishedAt is { } date
            ? date.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm")
            : "Ainda não publicado";
        MarketingSiteOverviewCustomDomainTextBox.Text = customDomain;
        MarketingSiteOverviewCatalogLinkText.Text = catalogUrl;
        MarketingSiteOverviewBookingLinkText.Text = $"{catalogUrl.TrimEnd('/')}/#agendar";

        var customDomainStatus = settings.PublicBookingCustomDomainStatus?.Trim().ToLowerInvariant();
        MarketingSiteOverviewDnsStatusText.Text = string.IsNullOrWhiteSpace(customDomain)
            ? "Use o endereço Agenda Livre"
            : customDomainStatus == "active"
                ? "Tudo certo"
                : !string.IsNullOrWhiteSpace(settings.PublicBookingCustomDomainLastError)
                    ? "Requer atenção"
                    : "Aguardando propagação";

        var heroPath = settings.PublishedMarketingCatalog?.HeroImagePath;
        if (string.IsNullOrWhiteSpace(heroPath))
        {
            heroPath = settings.MarketingSiteHeroImagePath;
        }
        MarketingSiteOverviewHeroImage.Source =
            LoadMarketingSiteBitmap(heroPath) ??
            LoadMarketingSiteBitmap("Assets/marketing-site-overview-makeup.png");

        var startDate = DateTime.Today.AddDays(-29);
        var onlineBookings = _data.Appointments
            .Where(appointment =>
                appointment.CreatedAt.Date >= startDate &&
                appointment.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow &&
                IsMarketingSiteBookingSource(appointment.ExternalSource))
            .ToList();

        MarketingSiteOverviewBookingsText.Text = onlineBookings.Count.ToString("N0");
        MarketingSiteOverviewVisitorsText.Text = "—";
        MarketingSiteOverviewConversionText.Text = "—";
        MarketingSiteOverviewAnalyticsStatusText.Text =
            "Os agendamentos exibidos são reais. Visitantes e conversão aparecerão quando a medição do site estiver conectada.";

        var bookingsByDay = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var date = startDate.AddDays(offset);
                return onlineBookings.Count(appointment => appointment.CreatedAt.Date == date);
            })
            .ToArray();
        var maximum = Math.Max(1, bookingsByDay.Max());
        var points = bookingsByDay
            .Select((count, index) =>
                new Point(
                    index * (360d / 29d),
                    48d - (count / (double)maximum * 38d)))
            .ToArray();
        MarketingSiteOverviewBookingsTrendLine.Points = new PointCollection(points);

        SetMarketingSiteOverviewPendingState(customDomain);
        _ = RefreshMarketingSitePublishedStateAsync(catalogUrl, customDomain);
        _ = LoadMarketingSitePublishedPreviewAsync(catalogUrl);
    }

    private void SetMarketingSiteOverviewPendingState(string customDomain)
    {
        var pendingBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        MarketingSiteOverviewHeaderOnlineText.Text = "Verificando site...";
        MarketingSiteOverviewOnlineDot.Fill = pendingBrush;
        MarketingSiteOverviewOnlineStatusText.Text = "Verificando...";
        MarketingSiteOverviewAvailabilityTitleText.Text = "Verificando publicação";
        MarketingSiteOverviewAvailabilityDescriptionText.Text = "Consultando o endereço publicado.";
        MarketingSiteOverviewDnsIcon.Kind = PackIconKind.ClockOutline;
        MarketingSiteOverviewDnsIcon.Foreground = pendingBrush;
        MarketingSiteOverviewDnsLabelText.Text = string.IsNullOrWhiteSpace(customDomain)
            ? "DNS Cloudflare"
            : "DNS do domínio";
        MarketingSiteOverviewDnsStatusText.Text = "Verificando...";
        MarketingSiteOverviewSslIcon.Kind = PackIconKind.ClockOutline;
        MarketingSiteOverviewSslIcon.Foreground = pendingBrush;
        MarketingSiteOverviewSslStatusText.Text = "Verificando...";
    }

    private async Task RefreshMarketingSitePublishedStateAsync(
        string catalogUrl,
        string customDomain)
    {
        _marketingSiteOverviewProbeCancellation?.Cancel();
        _marketingSiteOverviewProbeCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        _marketingSiteOverviewProbeCancellation = cancellation;

        MarketingSitePublishedProbe probe;
        try
        {
            if (!Uri.TryCreate(catalogUrl, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                probe = new(false, false, false, "Endereço publicado inválido.");
            }
            else
            {
                var addresses = await Dns.GetHostAddressesAsync(
                    uri.DnsSafeHost,
                    cancellation.Token);
                var dnsResolved = addresses.Length > 0;
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd("AgendaLivre-Windows/1.0");
                using var response = await _onlineBookingClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellation.Token);
                probe = new(
                    dnsResolved,
                    response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode
                        ? ""
                        : $"O site respondeu HTTP {(int)response.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            System.Net.Sockets.SocketException or
            InvalidOperationException)
        {
            probe = new(false, false, false, exception.Message);
        }

        if (cancellation.IsCancellationRequested ||
            !ReferenceEquals(_marketingSiteOverviewProbeCancellation, cancellation))
        {
            return;
        }

        ApplyMarketingSitePublishedProbe(probe, customDomain);
    }

    private void ApplyMarketingSitePublishedProbe(
        MarketingSitePublishedProbe probe,
        string customDomain)
    {
        var settings = _data.Settings;
        var successBrush = new SolidColorBrush(Color.FromRgb(22, 163, 74));
        var warningBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        var failureBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        var customStatus = settings.PublicBookingCustomDomainStatus
            .Trim()
            .ToLowerInvariant();
        var customSslStatus = settings.PublicBookingCustomDomainSslStatus
            .Trim()
            .ToLowerInvariant();
        var hasCustomDomain = !string.IsNullOrWhiteSpace(customDomain);
        var customDnsActive = !hasCustomDomain || customStatus == "active";
        var customSslActive = !hasCustomDomain ||
                              customSslStatus is "active" or "valid" or "deployed";
        var siteOnline = probe.HttpReachable && probe.HttpsValid;
        var dnsReady = probe.DnsResolved && customDnsActive;
        var sslReady = probe.HttpsValid && customSslActive;

        MarketingSiteOverviewHeaderOnlineText.Text = siteOnline
            ? "Online agora"
            : "Site indisponível";
        MarketingSiteOverviewOnlineDot.Fill = siteOnline ? successBrush : failureBrush;
        MarketingSiteOverviewOnlineStatusText.Text = siteOnline
            ? "Validado agora"
            : "Falha na verificação";
        MarketingSiteOverviewAvailabilityTitleText.Text = siteOnline
            ? "Site publicado e online"
            : "Site requer atenção";
        MarketingSiteOverviewAvailabilityDescriptionText.Text = siteOnline
            ? "O catálogo real está acessível para clientes."
            : FirstFilled(probe.Error, "Não foi possível acessar o endereço publicado.");

        MarketingSiteOverviewDnsIcon.Kind = dnsReady
            ? PackIconKind.CheckCircle
            : hasCustomDomain && customStatus is "pending" or "initializing"
                ? PackIconKind.ClockOutline
                : PackIconKind.AlertCircleOutline;
        MarketingSiteOverviewDnsIcon.Foreground = dnsReady
            ? successBrush
            : hasCustomDomain && customStatus is "pending" or "initializing"
                ? warningBrush
                : failureBrush;
        MarketingSiteOverviewDnsLabelText.Text = hasCustomDomain
            ? "DNS do domínio"
            : "DNS Cloudflare";
        MarketingSiteOverviewDnsStatusText.Text = dnsReady
            ? hasCustomDomain ? "Conectado" : "Wildcard ativo"
            : hasCustomDomain && customStatus is "pending" or "initializing"
                ? "Propagando"
                : "Não resolvido";

        MarketingSiteOverviewSslIcon.Kind = sslReady
            ? PackIconKind.Lock
            : hasCustomDomain && customSslStatus is "pending" or "initializing"
                ? PackIconKind.ClockOutline
                : PackIconKind.LockAlert;
        MarketingSiteOverviewSslIcon.Foreground = sslReady
            ? successBrush
            : hasCustomDomain && customSslStatus is "pending" or "initializing"
                ? warningBrush
                : failureBrush;
        MarketingSiteOverviewSslStatusText.Text = sslReady
            ? "Certificado válido"
            : hasCustomDomain && customSslStatus is "pending" or "initializing"
                ? "Emitindo certificado"
                : "SSL indisponível";
    }

    private async Task LoadMarketingSitePublishedPreviewAsync(string catalogUrl)
    {
        if (MarketingSiteOverviewWebView is null ||
            !Uri.TryCreate(catalogUrl, UriKind.Absolute, out var uri))
        {
            ShowMarketingSitePreviewError("O endereço publicado é inválido.");
            return;
        }

        MarketingSiteOverviewPreviewStatusPanel.Visibility = Visibility.Visible;
        MarketingSiteOverviewPreviewStatusIcon.Kind = PackIconKind.WebSync;
        MarketingSiteOverviewPreviewStatusTitle.Text = "Carregando o site publicado";
        MarketingSiteOverviewPreviewStatusDescription.Text =
            "Conectando ao endereço real do catálogo...";
        MarketingSiteOverviewPreviewRetryButton.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewWebView.Visibility = Visibility.Collapsed;

        try
        {
            await MarketingSiteOverviewWebView.EnsureCoreWebView2Async();
            if (!_marketingSiteOverviewWebViewConfigured)
            {
                _marketingSiteOverviewWebViewConfigured = true;
                MarketingSiteOverviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                MarketingSiteOverviewWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                MarketingSiteOverviewWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                MarketingSiteOverviewWebView.CoreWebView2.NewWindowRequested +=
                    MarketingSiteOverviewWebView_NewWindowRequested;
            }

            MarketingSiteOverviewPreviewStatusPanel.Visibility = Visibility.Collapsed;
            MarketingSiteOverviewWebView.Visibility = Visibility.Visible;
            MarketingSiteOverviewWebView.Source = uri;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            System.ComponentModel.Win32Exception or
            NotSupportedException)
        {
            ShowMarketingSitePreviewError(exception.Message);
        }
    }

    private void MarketingSiteOverviewWebView_NavigationCompleted(
        object sender,
        CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            MarketingSiteOverviewPreviewStatusPanel.Visibility = Visibility.Collapsed;
            MarketingSiteOverviewWebView.Visibility = Visibility.Visible;
            return;
        }

        ShowMarketingSitePreviewError(
            $"Não foi possível carregar o site ({e.WebErrorStatus}).");
    }

    private void MarketingSiteOverviewWebView_NewWindowRequested(
        object? sender,
        CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            System.ComponentModel.Win32Exception)
        {
            ShowStatus($"Não foi possível abrir o link: {exception.Message}");
        }
    }

    private void ShowMarketingSitePreviewError(string message)
    {
        MarketingSiteOverviewWebView.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewPreviewStatusPanel.Visibility = Visibility.Visible;
        MarketingSiteOverviewPreviewStatusIcon.Kind = PackIconKind.AlertCircleOutline;
        MarketingSiteOverviewPreviewStatusTitle.Text = "Site temporariamente indisponível";
        MarketingSiteOverviewPreviewStatusDescription.Text =
            FirstFilled(message, "Não foi possível carregar o endereço publicado.");
        MarketingSiteOverviewPreviewRetryButton.Visibility = Visibility.Visible;
    }

    private void MarketingSiteOverviewPreviewRetryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RefreshMarketingSiteOverview();
    }

    private sealed record MarketingSitePublishedProbe(
        bool DnsResolved,
        bool HttpReachable,
        bool HttpsValid,
        string Error);

    private static bool IsMarketingSiteBookingSource(string? value)
    {
        var source = (value ?? "").Trim().ToLowerInvariant().Replace('_', '-');
        return source is "agenda-online" or "online" or "web" or "website" or "public-booking";
    }

    private string MarketingSiteOverviewCatalogUrl()
    {
        if (Uri.TryCreate(_data.Settings.PublicBookingUrl, UriKind.Absolute, out var publicUri))
        {
            return publicUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        return $"https://{MarketingSiteDisplayUrl()}";
    }

    private void MarketingHubSiteRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowMarketingSiteOverview();
        e.Handled = true;
    }

    private void MarketingSiteOverviewBackButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingHub();

    private void MarketingSiteOverviewEditButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingSiteEditor();

    private void MarketingSiteOverviewOpenButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(MarketingSiteOverviewCatalogUrl())
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

    private void MarketingSiteOverviewDevice_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string device } ||
            MarketingSiteOverviewPreviewFrame is null)
        {
            return;
        }

        MarketingSiteOverviewPreviewFrame.HorizontalAlignment = HorizontalAlignment.Center;
        switch (device)
        {
            case "tablet":
                MarketingSiteOverviewPreviewFrame.Width = 470;
                MarketingSiteOverviewPreviewNavigation.Visibility = Visibility.Collapsed;
                MarketingSiteOverviewHeroCopy.Width = 270;
                MarketingSiteOverviewHeroCopy.Margin = new Thickness(22, 0, 0, 0);
                if (MarketingSiteOverviewWebView is not null)
                {
                    MarketingSiteOverviewWebView.ZoomFactor = 0.82;
                }
                break;
            case "mobile":
                MarketingSiteOverviewPreviewFrame.Width = 285;
                MarketingSiteOverviewPreviewNavigation.Visibility = Visibility.Collapsed;
                MarketingSiteOverviewHeroCopy.Width = 230;
                MarketingSiteOverviewHeroCopy.Margin = new Thickness(18, 0, 0, 0);
                if (MarketingSiteOverviewWebView is not null)
                {
                    MarketingSiteOverviewWebView.ZoomFactor = 1;
                }
                break;
            default:
                MarketingSiteOverviewPreviewFrame.Width = double.NaN;
                MarketingSiteOverviewPreviewFrame.HorizontalAlignment = HorizontalAlignment.Stretch;
                MarketingSiteOverviewPreviewNavigation.Visibility = Visibility.Visible;
                MarketingSiteOverviewHeroCopy.Width = 300;
                MarketingSiteOverviewHeroCopy.Margin = new Thickness(28, 0, 0, 0);
                if (MarketingSiteOverviewWebView is not null)
                {
                    MarketingSiteOverviewWebView.ZoomFactor = 0.68;
                }
                break;
        }
    }

    private void MarketingSiteOverviewSaveDomainButton_Click(object sender, RoutedEventArgs e)
    {
        var domain = NormalizeMarketingSiteCustomDomain(
            MarketingSiteOverviewCustomDomainTextBox.Text);
        if (!IsValidMarketingSiteCustomDomain(domain))
        {
            MessageBox.Show(
                this,
                "Informe um domínio válido, como www.seusalao.com.br, ou deixe o campo vazio.",
                "Domínio personalizado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            MarketingSiteOverviewCustomDomainTextBox.Focus();
            MarketingSiteOverviewCustomDomainTextBox.SelectAll();
            return;
        }

        var settings = _data.Settings;
        settings.MarketingSiteDraftCustomDomain = domain;
        if (settings.PublishedMarketingCatalog is { } publication)
        {
            publication.CustomDomain = domain;
            publication.PublishedAt = DateTime.Now;
            settings.MarketingSitePublishedAt = publication.PublishedAt;
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            settings.PublicBookingCustomDomain = "";
            settings.PublicBookingCustomDomainStatus = "";
            settings.PublicBookingCustomDomainLastError = "";
        }
        else
        {
            settings.PublicBookingCustomDomainStatus = "pending";
            settings.PublicBookingCustomDomainLastError = "";
        }

        _store.Save(_data);
        ScheduleOnlineBookingSync();
        RefreshMarketingSiteOverview();
        ShowStatus(string.IsNullOrWhiteSpace(domain)
            ? "Domínio personalizado removido. O endereço Agenda Livre continua ativo."
            : "Domínio salvo. A conexão segura com o DNS está sendo atualizada.");
    }

    private void MarketingSiteOverviewFocusDomainButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMarketingSiteTechnicalDialog(
            showDetails: false,
            selectCustomDomain: true);
    }

    private void MarketingSiteOverviewTechnicalButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMarketingSiteTechnicalDialog(showDetails: true);
    }

    private void MarketingSiteOverviewDnsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMarketingSiteTechnicalDialog(showDetails: true);
    }

    private void ShowMarketingSiteTechnicalDialog(
        bool showDetails,
        bool selectCustomDomain = false,
        bool modal = true)
    {
        var settings = _data.Settings;
        var customDomain = NormalizeMarketingSiteCustomDomain(
            settings.PublishedMarketingCatalog?.CustomDomain ??
            settings.MarketingSiteDraftCustomDomain ??
            settings.PublicBookingCustomDomain);
        var target = FirstFilled(
            settings.PublicBookingCustomDomainCnameTarget,
            $"customers.{PublicBookingRootDomain}");
        var dialog = new MarketingSiteTechnicalDialog(
            new MarketingSiteTechnicalDialogModel(
                MarketingSiteOverviewCatalogUrl(),
                string.Equals(
                    MarketingSiteOverviewHeaderOnlineText.Text,
                    "Online agora",
                    StringComparison.OrdinalIgnoreCase),
                customDomain,
                settings.PublicBookingCustomDomainStatus,
                settings.PublicBookingCustomDomainSslStatus,
                target),
            showDetails,
            selectCustomDomain)
        {
            Owner = this
        };
        var previousOpacity = Opacity;
        Opacity = 0.58;
        dialog.Closed += (_, _) => Opacity = previousOpacity;

        if (!modal)
        {
            dialog.Show();
            return;
        }

        if (dialog.ShowDialog() != true || !dialog.CustomDomainRequested)
        {
            return;
        }

        MarketingSiteOverviewCustomDomainTextBox.Text = dialog.CustomDomain;
        MarketingSiteOverviewSaveDomainButton_Click(
            dialog,
            new RoutedEventArgs());
    }

    private void MarketingSiteOverviewCopyCatalogLinkButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(MarketingSiteOverviewCatalogLinkText.Text);
        ShowStatus("Link do catálogo copiado.");
    }

    private void MarketingSiteOverviewCopyBookingLinkButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(MarketingSiteOverviewBookingLinkText.Text);
        ShowStatus("Link direto de agendamento copiado.");
    }
}
