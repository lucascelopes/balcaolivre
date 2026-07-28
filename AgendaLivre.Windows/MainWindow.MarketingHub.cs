using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private readonly List<MarketingPublicationRow> _marketingHubAllPublications = [];
    private readonly ObservableCollection<MarketingPublicationRow> _marketingHubVisiblePublications = [];
    private bool _marketingHubPublicationsLoading;
    private string _marketingHubCurrentFilter = "all";
    private string _marketingHubLastLoadMessage = "";
    private DateTime _marketingHubLastRefreshAt = DateTime.MinValue;

    private void ShowMarketingHub()
    {
        if (MarketingHubView is null || MarketingStudioView is null || MarketingStudioHeader is null)
        {
            return;
        }

        MarketingHubView.Visibility = Visibility.Visible;
        MarketingStudioView.Visibility = Visibility.Collapsed;
        MarketingStudioHeader.Visibility = Visibility.Collapsed;
        MarketingSiteEditorView.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewView.Visibility = Visibility.Collapsed;
        MarketingPostOverviewView.Visibility = Visibility.Collapsed;
        MarketingSitePromotionView.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();

        if (MarketingHubAllFilter is not null)
        {
            MarketingHubAllFilter.IsChecked = true;
        }

        MarketingHubPublicationsItemsControl.ItemsSource = _marketingHubVisiblePublications;
        ApplyMarketingHubFilter("all");
        if (!_marketingHubPublicationsLoading &&
            DateTime.Now - _marketingHubLastRefreshAt > TimeSpan.FromSeconds(30))
        {
            _ = RefreshMarketingHubPublicationsAsync();
        }
        MarketingView.ScrollToTop();
    }

    private async Task RefreshMarketingHubPublicationsAsync()
    {
        if (_marketingHubPublicationsLoading ||
            MarketingHubPublicationsItemsControl is null)
        {
            return;
        }

        _marketingHubPublicationsLoading = true;
        MarketingHubLoadingRow.Visibility = Visibility.Visible;
        MarketingHubPublicationsScroll.Visibility = Visibility.Collapsed;
        MarketingHubEmptyTextRow.Visibility = Visibility.Collapsed;

        try
        {
            var publications = await FetchInstagramPublicationsAsync();
            _marketingHubAllPublications.Clear();

            var settings = _data.Settings;
            var publishedCatalog = settings.PublishedMarketingCatalog;
            var catalogPublishedAt = publishedCatalog?.PublishedAt ??
                                     settings.MarketingSitePublishedAt;
            if (catalogPublishedAt is { } sitePublishedAt)
            {
                var siteBookings = _data.Appointments.Count(appointment =>
                    appointment.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow &&
                    IsMarketingSiteBookingSource(appointment.ExternalSource));
                var heroPath = FirstFilled(
                    publishedCatalog?.HeroImagePath ?? "",
                    settings.MarketingSiteHeroImagePath,
                    "Assets/marketing-site-overview-makeup.png");
                _marketingHubAllPublications.Add(new MarketingPublicationRow(
                    "site-active",
                    FirstFilled(
                        publishedCatalog?.Header?.BusinessName ?? "",
                        settings.MarketingSiteHeader?.BusinessName ?? "",
                        BusinessDisplayName()),
                    "Catálogo publicado e disponível para agendamentos.",
                    "Catálogo",
                    "site",
                    "Publicado",
                    sitePublishedAt.ToLocalTime(),
                    siteBookings == 1
                        ? "1 agendamento"
                        : $"{siteBookings:N0} agendamentos",
                    PackIconKind.Web,
                    InkBrush,
                    LoadMarketingSiteBitmap(heroPath),
                    "",
                    MarketingSiteOverviewCatalogUrl(),
                    "",
                    0,
                    0,
                    true));
            }

            var clickAudit = string.Equals(
                Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE"),
                "marketing-hub-click-audit",
                StringComparison.OrdinalIgnoreCase);
            if (clickAudit)
            {
                _marketingHubAllPublications.Add(new MarketingPublicationRow(
                    "__audit_real_publication",
                    "Resultado real de coloração",
                    "Publicação sincronizada da conta profissional conectada.",
                    "Post",
                    "image",
                    "Publicado",
                    DateTime.Now.AddDays(-2),
                    "Curtidas 86\nComentários 12",
                    PackIconKind.Instagram,
                    Solid("#E1306C"),
                    LoadMarketingSiteBitmap("Assets/marketing-campaign-hair.png"),
                    "Resultado real de coloração\n\nPublicação sincronizada da conta profissional conectada.",
                    "https://www.instagram.com/",
                    "",
                    86,
                    12,
                    false));
            }

            foreach (var publication in clickAudit
                         ? []
                         : publications.Publications)
            {
                var (title, summary) = MarketingPublicationCopy(publication.Caption);
                var mediaType = publication.MediaType.Trim().ToUpperInvariant();
                var channel = mediaType == "VIDEO" || mediaType == "REELS"
                    ? "Reel"
                    : mediaType == "STORIES" || mediaType == "STORY"
                        ? "Story"
                        : "Post";
                var icon = channel == "Reel"
                    ? PackIconKind.VideoOutline
                    : PackIconKind.Instagram;
                var results =
                    $"Curtidas {publication.LikeCount:N0}\nComentários {publication.CommentsCount:N0}";
                _marketingHubAllPublications.Add(new MarketingPublicationRow(
                    publication.Id,
                    title,
                    summary,
                    channel,
                    "image",
                    "Publicado",
                    publication.PublishedAt,
                    results,
                    icon,
                    Solid("#E1306C"),
                    MarketingPublicationBitmap(publication.ThumbnailUrl),
                    publication.Caption,
                    publication.Permalink,
                    publication.MediaUrl,
                    publication.LikeCount,
                    publication.CommentsCount,
                    false));
            }

            _marketingHubAllPublications.Sort((left, right) =>
                right.PublishedAt.CompareTo(left.PublishedAt));
            _marketingHubLastLoadMessage = clickAudit || publications.Ok
                ? ""
                : publications.Connected
                    ? MarketingHubPublicationLoadMessage(publications.Message)
                    : "Conecte o Instagram profissional para ver suas publicações reais.";
            _marketingHubLastRefreshAt = DateTime.Now;
        }
        finally
        {
            _marketingHubPublicationsLoading = false;
            MarketingHubLoadingRow.Visibility = Visibility.Collapsed;
            ApplyMarketingHubFilter(_marketingHubCurrentFilter);
        }
    }

    private static (string Title, string Summary) MarketingPublicationCopy(string? caption)
    {
        var lines = (caption ?? "")
            .Replace("\r", "", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var title = lines.FirstOrDefault() ?? "Publicação do Instagram";
        var summary = lines.Length > 1
            ? string.Join(" ", lines.Skip(1))
            : title;
        return (TrimMarketingPublicationText(title, 54), TrimMarketingPublicationText(summary, 112));
    }

    private static string MarketingHubPublicationLoadMessage(string? message)
    {
        if ((message ?? "").Contains("Rota Instagram", StringComparison.OrdinalIgnoreCase))
        {
            return "A sincronização de publicações precisa ser ativada no servidor.";
        }

        return FirstFilled(message ?? "", "Não foi possível sincronizar as publicações agora.");
    }

    private static string TrimMarketingPublicationText(string value, int maximum)
    {
        var clean = string.Join(" ", (value ?? "").Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length <= maximum)
        {
            return clean;
        }

        return $"{clean[..Math.Max(1, maximum - 1)].TrimEnd()}…";
    }

    private static ImageSource? MarketingPublicationBitmap(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http"))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnDemand;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            return bitmap;
        }
        catch (Exception exception) when (
            exception is UriFormatException or
            InvalidOperationException or
            NotSupportedException)
        {
            return null;
        }
    }

    private void MarketingHubPublicationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MarketingPublicationRow publication })
        {
            return;
        }

        if (publication.IsSite)
        {
            ShowMarketingSiteOverview();
            return;
        }

        ShowMarketingPostOverview(publication);
    }

    private void ShowMarketingStudio(RadioButton? channel = null, string? title = null, string? copy = null)
    {
        if (MarketingHubView is null || MarketingStudioView is null || MarketingStudioHeader is null)
        {
            return;
        }

        MarketingHubView.Visibility = Visibility.Collapsed;
        MarketingStudioView.Visibility = Visibility.Visible;
        MarketingStudioHeader.Visibility = Visibility.Visible;
        MarketingSiteEditorView.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewView.Visibility = Visibility.Collapsed;
        MarketingPostOverviewView.Visibility = Visibility.Collapsed;
        MarketingSitePromotionView.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();

        if (channel is not null)
        {
            channel.IsChecked = true;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            MarketingStudioTitleTextBox.Text = title;
        }

        if (!string.IsNullOrWhiteSpace(copy))
        {
            MarketingStudioCopyTextBox.Text = copy;
        }

        RefreshMarketingStudio();
        if (_marketingPhotosLoadedForDate != DateTime.Today || _marketingPhotoSuggestions.Count == 0)
        {
            _ = RefreshMarketingPhotosAsync(MarketingPhotoSearchTextBox.Text);
        }
        MarketingView.ScrollToTop();
    }

    private void MarketingHubBackButton_Click(object sender, RoutedEventArgs e) => ShowMarketingHub();

    private void MarketingHubStoryButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingStudio(MarketingStudioStoryTab);

    private void MarketingHubPostButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingStudio(MarketingStudioPostTab);

    private void MarketingHubWhatsAppButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingStudio(MarketingStudioWhatsAppTab);

    private void MarketingHubNewCustomersButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingStudio(
            MarketingStudioWhatsAppTab,
            "Boas-vindas à nossa agenda",
            "Que bom ter você por aqui! Preparamos um atendimento especial para sua primeira visita. Escolha o melhor horário e fale com a gente pelo WhatsApp.");

    private void MarketingHubDiscountButton_Click(object sender, RoutedEventArgs e) =>
        ShowMarketingSitePromotion();

    private void MarketingHubEditSiteButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMarketingSiteEditor();
    }

    private readonly string[] _marketingSiteHeroImages =
    [
        "Assets/marketing-site-hero-hair.png",
        "Assets/marketing-campaign-nails.png",
        "Assets/marketing-campaign-spa.png"
    ];

    private int _marketingSiteHeroImageIndex;

    private void ShowMarketingSiteEditor()
    {
        if (MarketingSiteEditorView is null)
        {
            return;
        }

        MarketingHubView.Visibility = Visibility.Collapsed;
        MarketingStudioView.Visibility = Visibility.Collapsed;
        MarketingStudioHeader.Visibility = Visibility.Collapsed;
        MarketingSiteEditorView.Visibility = Visibility.Visible;
        MarketingSiteOverviewView.Visibility = Visibility.Collapsed;
        MarketingPostOverviewView.Visibility = Visibility.Collapsed;
        MarketingSitePromotionView.Visibility = Visibility.Collapsed;
        RefreshWhatsAppLauncherVisibility();

        MarketingSitePreviewBusinessNameText.Text = BusinessDisplayName();
        MarketingSitePreviewUrlText.Text = MarketingSiteDisplayUrl();
        LoadMarketingSiteEditorSettings();
        UpdateMarketingSitePreview();
        MarketingView.ScrollToTop();
    }

    private string MarketingSiteDisplayUrl()
    {
        if (Uri.TryCreate(_data.Settings.PublicBookingUrl, UriKind.Absolute, out var publicUri))
        {
            return publicUri.Host;
        }

        var slug = string.IsNullOrWhiteSpace(_data.Settings.PublicBookingSlug)
            ? "belabeautfull"
            : _data.Settings.PublicBookingSlug.Trim();
        return $"{slug}.minhaagendalivre.com.br";
    }

    private void MarketingSiteBackButton_Click(object sender, RoutedEventArgs e) => ShowMarketingHub();

    private void MarketingSiteField_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMarketingSitePreview();
        ScheduleMarketingSiteSave();
    }

    private void UpdateMarketingSitePreview()
    {
        if (MarketingSitePreviewTitleText is null || MarketingSiteTitleTextBox is null ||
            MarketingSitePreviewSupportText is null || MarketingSiteSupportTextBox is null ||
            MarketingSitePreviewButtonText is null || MarketingSiteButtonTextBox is null ||
            MarketingSitePreviewButtonBorder is null || MarketingSiteShowButtonToggle is null)
        {
            return;
        }

        MarketingSitePreviewTitleText.Text = string.IsNullOrWhiteSpace(MarketingSiteTitleTextBox.Text)
            ? "Sua beleza, do seu jeito"
            : MarketingSiteTitleTextBox.Text.Trim();
        MarketingSitePreviewSupportText.Text = string.IsNullOrWhiteSpace(MarketingSiteSupportTextBox.Text)
            ? "Realce sua essência com cuidados personalizados para você."
            : MarketingSiteSupportTextBox.Text.Trim();
        MarketingSitePreviewButtonText.Text = string.IsNullOrWhiteSpace(MarketingSiteButtonTextBox.Text)
            ? "Agendar agora"
            : MarketingSiteButtonTextBox.Text.Trim();
        if (MarketingSiteStyleButtonTextBox is not null &&
            !string.Equals(MarketingSiteStyleButtonTextBox.Text, MarketingSiteButtonTextBox.Text, StringComparison.Ordinal))
        {
            MarketingSiteStyleButtonTextBox.Text = MarketingSiteButtonTextBox.Text;
        }
        if (MarketingSiteStyleBookingButtonPreviewText is not null)
        {
            MarketingSiteStyleBookingButtonPreviewText.Text = MarketingSitePreviewButtonText.Text;
        }
        if (MarketingSiteStyleShowButtonToggle is not null &&
            MarketingSiteStyleShowButtonToggle.IsChecked != MarketingSiteShowButtonToggle.IsChecked)
        {
            MarketingSiteStyleShowButtonToggle.IsChecked = MarketingSiteShowButtonToggle.IsChecked;
        }
        if (MarketingSiteStyleBookingButtonPreviewBorder is not null)
        {
            MarketingSiteStyleBookingButtonPreviewBorder.Opacity =
                MarketingSiteShowButtonToggle.IsChecked == true ? 1 : 0.42;
        }
        MarketingSitePreviewButtonBorder.Visibility = MarketingSiteShowButtonToggle.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;

    }

    private void MarketingSiteDevice_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string device } || MarketingSiteBrowserFrame is null)
        {
            return;
        }

        MarketingSiteBrowserFrame.HorizontalAlignment = HorizontalAlignment.Center;
        switch (device)
        {
            case "tablet":
                MarketingSiteBrowserFrame.Width = 610;
                MarketingSitePreviewNavigation.Visibility = Visibility.Visible;
                MarketingSitePreviewServicesPanel.Visibility = Visibility.Collapsed;
                MarketingSitePreviewSectionsScroll.Visibility = Visibility.Visible;
                MarketingSiteHeroCopyPanel.Width = 330;
                MarketingSiteHeroCopyPanel.Opacity = 0.96;
                MarketingSitePreviewTitleText.FontSize = 29;
                break;
            case "mobile":
                MarketingSiteBrowserFrame.Width = 350;
                MarketingSitePreviewNavigation.Visibility = Visibility.Collapsed;
                MarketingSitePreviewServicesPanel.Visibility = Visibility.Collapsed;
                MarketingSitePreviewSectionsScroll.Visibility = Visibility.Visible;
                MarketingSiteHeroCopyPanel.Width = 350;
                MarketingSiteHeroCopyPanel.Opacity = 0.78;
                MarketingSitePreviewTitleText.FontSize = 26;
                break;
            default:
                MarketingSiteBrowserFrame.Width = double.NaN;
                MarketingSiteBrowserFrame.HorizontalAlignment = HorizontalAlignment.Stretch;
                MarketingSitePreviewNavigation.Visibility = Visibility.Visible;
                MarketingSitePreviewServicesPanel.Visibility = Visibility.Collapsed;
                MarketingSitePreviewSectionsScroll.Visibility = Visibility.Visible;
                MarketingSiteHeroCopyPanel.Width = 365;
                MarketingSiteHeroCopyPanel.Opacity = 0.96;
                MarketingSitePreviewTitleText.FontSize = 32;
                break;
        }
    }

    private RadioButton? FindMarketingSiteDeviceTab(string tag) =>
        FindVisualChildren<RadioButton>(MarketingSiteEditorView)
            .FirstOrDefault(item => string.Equals(item.Tag as string, tag, StringComparison.Ordinal));

    private RadioButton? FindMarketingSiteInspectorTab(string tag) =>
        FindVisualChildren<RadioButton>(MarketingSiteEditorView)
            .FirstOrDefault(item => item.GroupName == "MarketingSiteInspector" &&
                                    string.Equals(item.Tag as string, tag, StringComparison.Ordinal));

    private void MarketingSiteInspectorTab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string tab } || MarketingSiteContentInspectorScroll is null)
        {
            return;
        }

        MarketingSiteContentInspectorScroll.Visibility = tab == "content" ? Visibility.Visible : Visibility.Collapsed;
        MarketingSiteStyleInspectorScroll.Visibility = tab == "style" ? Visibility.Visible : Visibility.Collapsed;
        MarketingSiteStructureInspectorScroll.Visibility = tab == "sections" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MarketingSiteAlignment_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string alignment } || MarketingSitePreviewTitleText is null)
        {
            return;
        }

        var textAlignment = alignment switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            _ => TextAlignment.Left
        };
        MarketingSitePreviewTitleText.TextAlignment = textAlignment;
        MarketingSitePreviewSupportText.TextAlignment = textAlignment;
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteShowButtonToggle_Changed(object sender, RoutedEventArgs e)
    {
        UpdateMarketingSitePreview();
        ScheduleMarketingSiteSave();
    }

    private void MarketingSiteChangeImageButton_Click(object sender, RoutedEventArgs e)
    {
        var previousPath = _data.Settings.MarketingSiteHeroImagePath;
        _marketingSiteHeroImageIndex = (_marketingSiteHeroImageIndex + 1) % _marketingSiteHeroImages.Length;
        var builtInPath = _marketingSiteHeroImages[_marketingSiteHeroImageIndex];
        _data.Settings.MarketingSiteHeroImagePath = builtInPath;
        _onlineBookingCachedCatalogHeroFingerprint = "";
        _onlineBookingLastSyncedCatalogHeroFingerprint = "";
        ApplyMarketingSiteHeroImage(builtInPath);
        ScheduleMarketingSiteSave();
        DeleteManagedMarketingSiteImageIfUnused(previousPath);
        MarketingSiteSavedStatusText.Text = "Foto pronta aplicada";
    }

    private void MarketingSiteFocusTitleButton_Click(object sender, RoutedEventArgs e)
    {
        MarketingSiteContentTab.IsChecked = true;
        MarketingSiteTitleTextBox.Focus();
        MarketingSiteTitleTextBox.SelectAll();
    }

    private void MarketingSitePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        ShowMarketingSitePreviewWindow();
    }

    private void MarketingSitePublishButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValidateMarketingSiteAddressForPublish())
        {
            return;
        }
        SaveMarketingSiteSettings(markAsPublished: true);
        UpdateMarketingSiteCustomDomainStatus();
        ScheduleOnlineBookingSync();
        ShowStatus("Publicação enviada. Sincronizando o catálogo na nuvem.");
    }

    private void MarketingHubFilter_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string filter })
        {
            ApplyMarketingHubFilter(filter);
        }
    }

    private void ApplyMarketingHubFilter(string filter)
    {
        if (MarketingHubPublicationsItemsControl is null ||
            MarketingHubEmptyTextRow is null)
        {
            return;
        }

        _marketingHubCurrentFilter = filter;
        _marketingHubVisiblePublications.Clear();
        foreach (var publication in _marketingHubAllPublications.Where(publication =>
                     filter == "all" ||
                     string.Equals(publication.FilterKey, filter, StringComparison.OrdinalIgnoreCase)))
        {
            _marketingHubVisiblePublications.Add(publication);
        }

        var hasRows = _marketingHubVisiblePublications.Count > 0;
        MarketingHubPublicationsScroll.Visibility =
            hasRows && !_marketingHubPublicationsLoading
                ? Visibility.Visible
                : Visibility.Collapsed;
        MarketingHubEmptyTextRow.Visibility =
            !hasRows && !_marketingHubPublicationsLoading
                ? Visibility.Visible
                : Visibility.Collapsed;

        if (!hasRows && !_marketingHubPublicationsLoading)
        {
            MarketingHubEmptyTitleText.Text = filter switch
            {
                "image" => "Nenhuma publicação real do Instagram.",
                "text" => "Nenhuma campanha de texto salva.",
                "site" => "O catálogo ainda não foi publicado.",
                _ => "Nenhuma publicação real encontrada."
            };
            MarketingHubEmptyDescriptionText.Text = !string.IsNullOrWhiteSpace(_marketingHubLastLoadMessage)
                ? _marketingHubLastLoadMessage
                : filter switch
                {
                    "text" => "Crie e envie uma campanha para ela aparecer aqui.",
                    "site" => "Publique o catálogo para acompanhar o site ativo.",
                    _ => "As publicações da conta conectada aparecerão aqui automaticamente."
                };
        }
    }

    public sealed record MarketingPublicationRow(
        string Id,
        string Title,
        string Summary,
        string Channel,
        string FilterKey,
        string Status,
        DateTime PublishedAt,
        string Results,
        PackIconKind Icon,
        Brush IconBrush,
        ImageSource? Thumbnail,
        string Caption,
        string Permalink,
        string MediaUrl,
        int LikeCount,
        int CommentsCount,
        bool IsSite)
    {
        public string PublishedText => PublishedAt.ToString("dd/MM/yyyy\nHH:mm");
        public string AutomationName => IsSite
            ? $"Abrir detalhes do catálogo {Title}"
            : $"Abrir desempenho da publicação {Title}";
    }
}
