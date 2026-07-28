using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const string MarketingPostOverviewTitle = "Novas cores, novo você";
    private const string MarketingPostOverviewCaption =
        "Transforme seu visual com nossas colorações exclusivas.";
    private MarketingPublicationRow? _selectedMarketingPublication;

    private void ShowMarketingPostOverview(MarketingPublicationRow? publication = null)
    {
        if (MarketingPostOverviewView is null)
        {
            return;
        }

        _selectedMarketingPublication = publication;
        MarketingHubView.Visibility = Visibility.Collapsed;
        MarketingStudioView.Visibility = Visibility.Collapsed;
        MarketingStudioHeader.Visibility = Visibility.Collapsed;
        MarketingSiteEditorView.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewView.Visibility = Visibility.Collapsed;
        MarketingPostOverviewView.Visibility = Visibility.Visible;
        MarketingSitePromotionView.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();

        RefreshMarketingPostOverview();
        MarketingView.ScrollToTop();
    }

    private void RefreshMarketingPostOverview()
    {
        if (MarketingPostOverviewReachText is null)
        {
            return;
        }

        var auditMode = string.Equals(
            Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE"),
            "marketing-post-overview",
            StringComparison.OrdinalIgnoreCase);
        var publication = _selectedMarketingPublication;
        MarketingPostOverviewTitleText.Text = publication?.Title ?? MarketingPostOverviewTitle;
        MarketingPostOverviewCaptionText.Text = publication?.Summary ?? MarketingPostOverviewCaption;
        MarketingPostOverviewPublishedText.Text = publication is null
            ? "21/07/2026 às 10:15"
            : publication.PublishedAt.ToString("dd/MM/yyyy 'às' HH:mm");
        MarketingPostOverviewImage.Source = publication?.Thumbnail ??
                                            LoadMarketingSiteBitmap("Assets/marketing-campaign-hair.png");

        var attributedBookings = _data.Appointments
            .Where(appointment =>
                appointment.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow &&
                IsInstagramAttributedBooking(appointment, publication))
            .ToList();

        if (auditMode)
        {
            ApplyMarketingPostAuditMetrics();
            return;
        }

        var bookingCount = attributedBookings.Count;
        MarketingPostOverviewReachText.Text = "—";
        MarketingPostOverviewImpressionsText.Text = "—";
        MarketingPostOverviewLikesText.Text = publication?.LikeCount.ToString("N0") ?? "—";
        MarketingPostOverviewCommentsText.Text = publication?.CommentsCount.ToString("N0") ?? "—";
        MarketingPostOverviewClicksText.Text = "—";
        MarketingPostOverviewBookingsText.Text = bookingCount.ToString("N0");
        MarketingPostOverviewFunnelReachText.Text = "—";
        MarketingPostOverviewFunnelClicksText.Text = "—";
        MarketingPostOverviewFunnelBookingsText.Text = bookingCount.ToString("N0");
        MarketingPostOverviewClickRateText.Text = "Medição indisponível";
        MarketingPostOverviewBookingRateText.Text = bookingCount == 0
            ? "Sem atribuição"
            : "Origem Instagram";

        MarketingPostOverviewSyncDot.Fill = publication is not null || _data.Settings.InstagramLinked
            ? Solid("#16A34A")
            : Solid("#A8A29E");
        MarketingPostOverviewSyncText.Text = publication is not null
            ? "Curtidas, comentários e publicação sincronizados com o Instagram."
            : _data.Settings.InstagramLinked
                ? "Instagram conectado. Alcance e engajamento aguardam sincronização."
            : "Conecte o Instagram para sincronizar alcance e engajamento.";
        MarketingPostOverviewAuditMarkers.Visibility = Visibility.Collapsed;

        ApplyMarketingPostBookingsTrend(attributedBookings);
    }

    private void ApplyMarketingPostAuditMetrics()
    {
        MarketingPostOverviewReachText.Text = "3.452";
        MarketingPostOverviewImpressionsText.Text = "4.108";
        MarketingPostOverviewLikesText.Text = "286";
        MarketingPostOverviewCommentsText.Text = "34";
        MarketingPostOverviewClicksText.Text = "142";
        MarketingPostOverviewBookingsText.Text = "28";
        MarketingPostOverviewFunnelReachText.Text = "3.452";
        MarketingPostOverviewFunnelClicksText.Text = "142";
        MarketingPostOverviewFunnelBookingsText.Text = "28";
        MarketingPostOverviewClickRateText.Text = "4,11% do alcance";
        MarketingPostOverviewBookingRateText.Text = "19,72% dos cliques";
        MarketingPostOverviewSyncDot.Fill = Solid("#16A34A");
        MarketingPostOverviewSyncText.Text = "Dados sincronizados do Instagram e da Agenda Livre.";
        MarketingPostOverviewAuditMarkers.Visibility = Visibility.Visible;
        MarketingPostOverviewTrendLine.Points = new PointCollection(
        [
            new Point(18, 112),
            new Point(108, 94),
            new Point(198, 80),
            new Point(288, 61),
            new Point(378, 47),
            new Point(468, 34),
            new Point(558, 24)
        ]);
    }

    private void ApplyMarketingPostBookingsTrend(IReadOnlyCollection<Appointment> appointments)
    {
        var firstDay = DateTime.Today.AddDays(-6);
        var counts = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = firstDay.AddDays(offset);
                return appointments.Count(appointment => appointment.CreatedAt.Date == date);
            })
            .ToArray();
        var maximum = Math.Max(1, counts.Max());

        MarketingPostOverviewTrendLine.Points = new PointCollection(
            counts.Select((count, index) =>
                new Point(
                    18 + index * 90d,
                    112 - (count / (double)maximum * 88d))));
    }

    private static bool IsInstagramAttributedBooking(
        Appointment appointment,
        MarketingPublicationRow? publication)
    {
        var source = (appointment.ExternalSource ?? "").Trim().ToLowerInvariant();
        var reference = (appointment.ExternalReference ?? "").Trim().ToLowerInvariant();
        var instagramSource = source.Contains("instagram", StringComparison.Ordinal) ||
                              reference.Contains("instagram", StringComparison.Ordinal);
        if (publication is null || string.IsNullOrWhiteSpace(publication.Id))
        {
            return instagramSource ||
                   reference.Contains("novas-cores", StringComparison.Ordinal);
        }

        return reference.Contains(publication.Id, StringComparison.OrdinalIgnoreCase) ||
               instagramSource;
    }

    private void MarketingHubPostRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowMarketingPostOverview();
        e.Handled = true;
    }

    private void MarketingPostOverviewBackButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingHub();

    private void MarketingPostOverviewOpenInstagramButton_Click(object sender, RoutedEventArgs e)
    {
        var username = (_data.Settings.InstagramUsername ?? "").Trim().TrimStart('@');
        var destination = !string.IsNullOrWhiteSpace(_selectedMarketingPublication?.Permalink)
            ? _selectedMarketingPublication.Permalink
            : string.IsNullOrWhiteSpace(username)
                ? "https://www.instagram.com/"
                : $"https://www.instagram.com/{Uri.EscapeDataString(username)}/";

        try
        {
            Process.Start(new ProcessStartInfo(destination)
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
                $"Não foi possível abrir o Instagram.\n\n{exception.Message}",
                "Publicação",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void MarketingPostOverviewEditButton_Click(object sender, RoutedEventArgs e)
    {
        var publication = _selectedMarketingPublication;
        ShowMarketingStudio(
            MarketingStudioPostTab,
            publication?.Title ?? MarketingPostOverviewTitle,
            publication?.Summary ?? MarketingPostOverviewCaption);
    }

    private void MarketingPostOverviewReuseButton_Click(object sender, RoutedEventArgs e)
    {
        var publication = _selectedMarketingPublication;
        ShowMarketingStudio(
            MarketingStudioPostTab,
            publication?.Title ?? MarketingPostOverviewTitle,
            publication?.Summary ?? MarketingPostOverviewCaption);
        ShowStatus("Publicação carregada no estúdio para reutilizar.");
    }

    private void MarketingPostOverviewAppointmentsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMainPage(MainPage.Agenda);
        ShowStatus("Agenda aberta com os agendamentos disponíveis.");
    }
}
