using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private readonly ObservableCollection<MarketingPromotionServiceRow> _marketingPromotionRows = [];
    private bool _marketingPromotionViewBuilt;
    private TextBox? _marketingPromotionSearchTextBox;
    private ComboBox? _marketingPromotionCategoryComboBox;
    private TextBox? _marketingPromotionNameTextBox;
    private DatePicker? _marketingPromotionStartDatePicker;
    private DatePicker? _marketingPromotionEndDatePicker;
    private TextBox? _marketingPromotionLimitTextBox;
    private CheckBox? _marketingPromotionHighlightCheckBox;
    private TextBlock? _marketingPromotionSummaryText;
    private TextBlock? _marketingPromotionPreviewTitleText;
    private TextBlock? _marketingPromotionPreviewDetailText;
    private TextBlock? _marketingPromotionStatusText;
    private TextBlock? _marketingPromotionSelectedCountText;
    private DataGrid? _marketingPromotionServicesGrid;

    private void ShowMarketingSitePromotion()
    {
        if (MarketingSitePromotionView is null)
        {
            return;
        }

        BuildMarketingSitePromotionView();
        MarketingHubView.Visibility = Visibility.Collapsed;
        MarketingStudioView.Visibility = Visibility.Collapsed;
        MarketingStudioHeader.Visibility = Visibility.Collapsed;
        MarketingSiteEditorView.Visibility = Visibility.Collapsed;
        MarketingSiteOverviewView.Visibility = Visibility.Collapsed;
        MarketingPostOverviewView.Visibility = Visibility.Collapsed;
        MarketingSitePromotionView.Visibility = Visibility.Visible;
        RefreshWhatsAppLauncherVisibility();
        LoadMarketingSitePromotion();
        MarketingView.ScrollToTop();
    }

    private void BuildMarketingSitePromotionView()
    {
        if (_marketingPromotionViewBuilt)
        {
            return;
        }

        _marketingPromotionViewBuilt = true;
        var ink = ResourceBrush("Ink", "#221F1D");
        var muted = ResourceBrush("Muted", "#746E69");
        var line = ResourceBrush("Line", "#E7E1DC");
        var accent = ResourceBrush("Accent", "#F56A1C");
        var accentText = ResourceBrush("AccentText", "#C9470A");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var backButton = new Button
        {
            Width = 38,
            Height = 38,
            Padding = new Thickness(0),
            ToolTip = "Voltar para Marketing",
            Style = TryFindResource("GhostButton") as Style
        };
        backButton.Content = new PackIcon { Kind = PackIconKind.ArrowLeft, Width = 18, Height = 18 };
        backButton.Click += (_, _) => ShowMarketingHub();
        header.Children.Add(backButton);

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Criar promoção no site",
            Foreground = ink,
            FontSize = 25,
            FontWeight = FontWeights.Bold
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Escolha os serviços, defina os novos preços e publique no catálogo online.",
            Foreground = muted,
            FontSize = 11.5,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(titleStack, 2);
        header.Children.Add(titleStack);

        var channelPill = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(255, 244, 237)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(251, 185, 145)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(12, 7, 12, 7),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new PackIcon { Kind = PackIconKind.Web, Width = 15, Height = 15, Foreground = accentText, Margin = new Thickness(0, 0, 6, 0) },
                    new TextBlock { Text = "Exclusivo para o site", Foreground = accentText, FontSize = 10.5, FontWeight = FontWeights.SemiBold }
                }
            }
        };
        Grid.SetColumn(channelPill, 3);
        header.Children.Add(channelPill);
        root.Children.Add(header);

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star), MinWidth = 294 });
        Grid.SetRow(content, 2);
        root.Children.Add(content);

        var catalogPanel = PromotionCard();
        Grid.SetColumn(catalogPanel, 0);
        content.Children.Add(catalogPanel);
        var catalogGrid = new Grid();
        catalogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        catalogGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        catalogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        catalogGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        catalogGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(430) });
        catalogPanel.Child = catalogGrid;

        var catalogHeading = new Grid();
        catalogHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        catalogHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var catalogHeadingText = new StackPanel();
        catalogHeadingText.Children.Add(new TextBlock { Text = "Serviços do catálogo", Foreground = ink, FontSize = 16, FontWeight = FontWeights.Bold });
        catalogHeadingText.Children.Add(new TextBlock { Text = "O preço do PDV não será alterado.", Foreground = muted, FontSize = 10, Margin = new Thickness(0, 3, 0, 0) });
        catalogHeading.Children.Add(catalogHeadingText);
        _marketingPromotionSelectedCountText = new TextBlock
        {
            Text = "Selecione ao menos 1 serviço",
            Foreground = muted,
            FontSize = 9.5
        };
        var selectedPill = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(246, 246, 244)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 6, 10, 6),
            Child = _marketingPromotionSelectedCountText
        };
        Grid.SetColumn(selectedPill, 1);
        catalogHeading.Children.Add(selectedPill);
        catalogGrid.Children.Add(catalogHeading);

        var filterGrid = new Grid();
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        _marketingPromotionSearchTextBox = new TextBox
        {
            Height = 38,
            Padding = new Thickness(12, 8, 12, 8),
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = "Buscar serviço"
        };
        HintAssist.SetHint(_marketingPromotionSearchTextBox, "Buscar serviço");
        _marketingPromotionSearchTextBox.TextChanged += (_, _) => ApplyMarketingPromotionFilters();
        filterGrid.Children.Add(_marketingPromotionSearchTextBox);
        _marketingPromotionCategoryComboBox = new ComboBox
        {
            Height = 38,
            Padding = new Thickness(10, 4, 8, 4),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 11
        };
        HintAssist.SetHint(_marketingPromotionCategoryComboBox, "Categoria");
        _marketingPromotionCategoryComboBox.SelectionChanged += (_, _) => ApplyMarketingPromotionFilters();
        Grid.SetColumn(_marketingPromotionCategoryComboBox, 2);
        filterGrid.Children.Add(_marketingPromotionCategoryComboBox);
        Grid.SetRow(filterGrid, 2);
        catalogGrid.Children.Add(filterGrid);

        _marketingPromotionServicesGrid = new DataGrid
        {
            ItemsSource = _marketingPromotionRows,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            CanUserResizeRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = line,
            VerticalGridLinesBrush = Brushes.Transparent,
            BorderBrush = line,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            RowBackground = Brushes.White,
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(253, 252, 251)),
            RowHeight = 58,
            ColumnHeaderHeight = 34,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow
        };
        _marketingPromotionServicesGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = "USAR",
            Width = 66,
            CellTemplate = PromotionSelectionTemplate()
        });
        _marketingPromotionServicesGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = "SERVIÇO",
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = PromotionServiceTemplate()
        });
        _marketingPromotionServicesGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
        {
            Header = "DURAÇÃO",
            Width = 76,
            IsReadOnly = true,
            Binding = new Binding(nameof(MarketingPromotionServiceRow.DurationText))
        });
        _marketingPromotionServicesGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
        {
            Header = "PREÇO ATUAL",
            Width = 92,
            IsReadOnly = true,
            Binding = new Binding(nameof(MarketingPromotionServiceRow.OriginalPriceText))
        });
        _marketingPromotionServicesGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
        {
            Header = "PREÇO PROMO",
            Width = 105,
            Binding = new Binding(nameof(MarketingPromotionServiceRow.PromotionalPrice))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                StringFormat = "N2",
                ConverterCulture = CultureInfo.GetCultureInfo("pt-BR")
            }
        });
        _marketingPromotionServicesGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
        {
            Header = "DESCONTO",
            Width = 78,
            IsReadOnly = true,
            Binding = new Binding(nameof(MarketingPromotionServiceRow.DiscountText))
        });
        Grid.SetRow(_marketingPromotionServicesGrid, 4);
        catalogGrid.Children.Add(_marketingPromotionServicesGrid);

        var detailsPanel = PromotionCard();
        Grid.SetColumn(detailsPanel, 2);
        content.Children.Add(detailsPanel);
        var detailsScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        detailsPanel.Child = detailsScroll;
        var details = new StackPanel();
        detailsScroll.Content = details;
        details.Children.Add(new TextBlock { Text = "Configurações da promoção", Foreground = ink, FontSize = 16, FontWeight = FontWeights.Bold });
        details.Children.Add(new TextBlock { Text = "Esses dados aparecem apenas no catálogo online.", Foreground = muted, FontSize = 10, Margin = new Thickness(0, 3, 0, 14) });

        details.Children.Add(PromotionLabel("Nome da promoção", muted));
        _marketingPromotionNameTextBox = PromotionTextBox();
        HintAssist.SetHint(_marketingPromotionNameTextBox, "Ex.: Semana do autocuidado");
        _marketingPromotionNameTextBox.TextChanged += (_, _) => UpdateMarketingPromotionSummary();
        details.Children.Add(_marketingPromotionNameTextBox);

        details.Children.Add(PromotionLabel("Período", muted, new Thickness(0, 12, 0, 5)));
        var dates = new Grid();
        dates.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dates.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        dates.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _marketingPromotionStartDatePicker = new DatePicker { Height = 36, FontSize = 10.5, SelectedDateFormat = DatePickerFormat.Short };
        _marketingPromotionEndDatePicker = new DatePicker { Height = 36, FontSize = 10.5, SelectedDateFormat = DatePickerFormat.Short };
        _marketingPromotionStartDatePicker.SelectedDateChanged += (_, _) => UpdateMarketingPromotionSummary();
        _marketingPromotionEndDatePicker.SelectedDateChanged += (_, _) => UpdateMarketingPromotionSummary();
        dates.Children.Add(_marketingPromotionStartDatePicker);
        Grid.SetColumn(_marketingPromotionEndDatePicker, 2);
        dates.Children.Add(_marketingPromotionEndDatePicker);
        details.Children.Add(dates);

        details.Children.Add(PromotionLabel("Limite por cliente", muted, new Thickness(0, 12, 0, 5)));
        _marketingPromotionLimitTextBox = PromotionTextBox();
        HintAssist.SetHint(_marketingPromotionLimitTextBox, "Quantidade");
        _marketingPromotionLimitTextBox.TextChanged += (_, _) => UpdateMarketingPromotionSummary();
        details.Children.Add(_marketingPromotionLimitTextBox);

        _marketingPromotionHighlightCheckBox = new CheckBox
        {
            Content = "Destacar no topo do catálogo",
            Foreground = ink,
            FontSize = 10.5,
            Margin = new Thickness(0, 13, 0, 0)
        };
        _marketingPromotionHighlightCheckBox.Checked += (_, _) => UpdateMarketingPromotionSummary();
        _marketingPromotionHighlightCheckBox.Unchecked += (_, _) => UpdateMarketingPromotionSummary();
        details.Children.Add(_marketingPromotionHighlightCheckBox);

        details.Children.Add(new Border { Height = 1, Background = line, Margin = new Thickness(0, 14, 0, 14) });
        details.Children.Add(new TextBlock { Text = "Prévia no site", Foreground = muted, FontSize = 9.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 7) });
        var preview = new Border
        {
            Height = 88,
            Background = new LinearGradientBrush(
                Color.FromRgb(255, 244, 237),
                Color.FromRgb(255, 226, 209),
                0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(250, 188, 151)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            ClipToBounds = true
        };
        var previewGrid = new Grid();
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var previewImage = new Image
        {
            Source = LoadMarketingSiteBitmap("Assets/marketing-site-overview-makeup.png"),
            Stretch = Stretch.UniformToFill,
            Opacity = 0.22,
            Margin = new Thickness(-14)
        };
        Grid.SetColumnSpan(previewImage, 2);
        previewGrid.Children.Add(previewImage);
        var previewCopy = new StackPanel { Width = 190 };
        _marketingPromotionPreviewTitleText = new TextBlock { Text = "Semana do autocuidado", Foreground = ink, FontSize = 14, FontWeight = FontWeights.Bold };
        _marketingPromotionPreviewDetailText = new TextBlock { Text = "Preços especiais por tempo limitado", Foreground = muted, FontSize = 9.5, Margin = new Thickness(0, 4, 0, 0) };
        previewCopy.Children.Add(_marketingPromotionPreviewTitleText);
        previewCopy.Children.Add(_marketingPromotionPreviewDetailText);
        previewGrid.Children.Add(previewCopy);
        var badge = new Border
        {
            Background = accent,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(9, 5, 9, 5),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = "OFF", Foreground = Brushes.White, FontSize = 8.5, FontWeight = FontWeights.Bold }
        };
        Grid.SetColumn(badge, 1);
        previewGrid.Children.Add(badge);
        preview.Child = previewGrid;
        details.Children.Add(preview);

        var summaryBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(248, 247, 245)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 10, 0, 0)
        };
        _marketingPromotionSummaryText = new TextBlock { Foreground = muted, FontSize = 9.5, TextWrapping = TextWrapping.Wrap };
        summaryBorder.Child = _marketingPromotionSummaryText;
        details.Children.Add(summaryBorder);

        var publishButton = new Button
        {
            Height = 42,
            Margin = new Thickness(0, 10, 0, 7),
            Style = TryFindResource("CommandButton") as Style,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new PackIcon { Kind = PackIconKind.WebCheck, Width = 17, Height = 17, Margin = new Thickness(0, 0, 8, 0) },
                    new TextBlock { Text = "Publicar no site", FontSize = 11.5, FontWeight = FontWeights.SemiBold }
                }
            }
        };
        publishButton.Click += MarketingSitePromotionPublishButton_Click;
        details.Children.Add(publishButton);

        var saveButton = new Button
        {
            Height = 38,
            Style = TryFindResource("GhostButton") as Style,
            Content = "Salvar rascunho"
        };
        saveButton.Click += MarketingSitePromotionSaveButton_Click;
        details.Children.Add(saveButton);
        _marketingPromotionStatusText = new TextBlock
        {
            Foreground = muted,
            FontSize = 9.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 9, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        details.Children.Add(_marketingPromotionStatusText);

        MarketingSitePromotionView.Children.Add(root);
    }

    private Border PromotionCard() =>
        new()
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(232, 226, 221)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 2,
                Opacity = 0.06,
                Color = Colors.Black
            }
        };

    private TextBox PromotionTextBox() =>
        new()
        {
            Height = 36,
            Padding = new Thickness(0),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 10.5
        };

    private static TextBlock PromotionLabel(string text, Brush brush, Thickness? margin = null) =>
        new()
        {
            Text = text,
            Foreground = brush,
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Margin = margin ?? new Thickness(0, 0, 0, 5)
        };

    private DataTemplate PromotionServiceTemplate()
    {
        var template = new DataTemplate(typeof(MarketingPromotionServiceRow));
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 3, 4, 3));
        panel.AppendChild(PromotionServiceImageFactory());
        var copy = new FrameworkElementFactory(typeof(StackPanel));
        copy.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        var name = new FrameworkElementFactory(typeof(TextBlock));
        name.SetBinding(TextBlock.TextProperty, new Binding(nameof(MarketingPromotionServiceRow.Name)));
        name.SetValue(TextBlock.FontSizeProperty, 11.0);
        name.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        name.SetValue(TextBlock.ForegroundProperty, ResourceBrush("Ink", "#221F1D"));
        copy.AppendChild(name);
        var category = new FrameworkElementFactory(typeof(TextBlock));
        category.SetBinding(TextBlock.TextProperty, new Binding(nameof(MarketingPromotionServiceRow.Category)));
        category.SetValue(TextBlock.FontSizeProperty, 9.0);
        category.SetValue(TextBlock.ForegroundProperty, ResourceBrush("Muted", "#746E69"));
        category.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 3, 0, 0));
        copy.AppendChild(category);
        panel.AppendChild(copy);
        template.VisualTree = panel;
        return template;
    }

    private DataTemplate PromotionSelectionTemplate()
    {
        var template = new DataTemplate(typeof(MarketingPromotionServiceRow));
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        panel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        panel.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
        panel.SetBinding(FrameworkElement.TagProperty, new Binding());
        panel.AddHandler(
            UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(MarketingPromotionSelectionButton_Click));
        var icon = new FrameworkElementFactory(typeof(PackIcon));
        icon.SetValue(FrameworkElement.WidthProperty, 18.0);
        icon.SetValue(FrameworkElement.HeightProperty, 18.0);
        icon.SetValue(Control.ForegroundProperty, ResourceBrush("AccentText", "#C9470A"));
        icon.SetBinding(PackIcon.KindProperty, new Binding(nameof(MarketingPromotionServiceRow.SelectionIcon)));
        panel.AppendChild(icon);
        template.VisualTree = panel;
        return template;
    }

    private void MarketingPromotionSelectionButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MarketingPromotionServiceRow row })
        {
            row.IsSelected = !row.IsSelected;
            e.Handled = true;
        }
    }

    private FrameworkElementFactory PromotionServiceImageFactory()
    {
        var imageBorder = new FrameworkElementFactory(typeof(Border));
        imageBorder.SetValue(FrameworkElement.WidthProperty, 34.0);
        imageBorder.SetValue(FrameworkElement.HeightProperty, 34.0);
        imageBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        imageBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 241, 233)));
        imageBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        var image = new FrameworkElementFactory(typeof(Image));
        image.SetBinding(Image.SourceProperty, new Binding(nameof(MarketingPromotionServiceRow.Thumbnail)));
        image.SetValue(Image.StretchProperty, Stretch.UniformToFill);
        imageBorder.AppendChild(image);
        return imageBorder;
    }

    private Brush ResourceBrush(string key, string fallback) =>
        TryFindResource(key) as Brush ??
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));

    private void LoadMarketingSitePromotion()
    {
        var settings = _data.Settings;
        var draft = settings.MarketingSitePromotion ?? new MarketingSitePromotion();
        var existingById = draft.Items
            .GroupBy(item => item.ServiceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        _marketingPromotionRows.Clear();
        foreach (var service in _data.Services.Where(service => service.IsActive).OrderBy(service => service.Name))
        {
            existingById.TryGetValue(service.Id, out var saved);
            var row = new MarketingPromotionServiceRow
            {
                ServiceId = service.Id,
                Name = service.Name,
                Category = PromotionServiceCategory(service),
                DurationMinutes = Math.Clamp(service.DurationMinutes, 15, 480),
                OriginalPrice = service.Price,
                PromotionalPrice = saved is { PromotionalPrice: >= 0 } ? saved.PromotionalPrice : SuggestedPromotionPrice(service.Price),
                IsSelected = saved is not null,
                Thumbnail = LoadMarketingSiteBitmap(PromotionThumbnailPath(service))
            };
            row.PropertyChanged += (_, _) => UpdateMarketingPromotionSummary();
            _marketingPromotionRows.Add(row);
        }

        if ((Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE") ?? "")
                .StartsWith("marketing-promotion", StringComparison.OrdinalIgnoreCase) &&
            _marketingPromotionRows.All(row => !row.IsSelected))
        {
            foreach (var row in _marketingPromotionRows.Take(3))
            {
                row.IsSelected = true;
            }
        }

        if (_marketingPromotionCategoryComboBox is not null)
        {
            _marketingPromotionCategoryComboBox.Items.Clear();
            _marketingPromotionCategoryComboBox.Items.Add("Todas as categorias");
            foreach (var category in _marketingPromotionRows
                         .Select(row => row.Category)
                         .Distinct(StringComparer.CurrentCultureIgnoreCase)
                         .OrderBy(value => value))
            {
                _marketingPromotionCategoryComboBox.Items.Add(category);
            }
            _marketingPromotionCategoryComboBox.SelectedIndex = 0;
        }

        if (_marketingPromotionNameTextBox is not null) _marketingPromotionNameTextBox.Text = draft.Name;
        if (_marketingPromotionStartDatePicker is not null) _marketingPromotionStartDatePicker.SelectedDate = draft.StartDate.Date;
        if (_marketingPromotionEndDatePicker is not null) _marketingPromotionEndDatePicker.SelectedDate = draft.EndDate.Date;
        if (_marketingPromotionLimitTextBox is not null) _marketingPromotionLimitTextBox.Text = Math.Clamp(draft.LimitPerCustomer, 1, 99).ToString(CultureInfo.CurrentCulture);
        if (_marketingPromotionHighlightCheckBox is not null) _marketingPromotionHighlightCheckBox.IsChecked = draft.HighlightInCatalog;
        if (_marketingPromotionStatusText is not null)
        {
            _marketingPromotionStatusText.Text = draft.IsPublished && draft.PublishedAt is { } publishedAt
                ? $"Publicada em {publishedAt:dd/MM/yyyy 'às' HH:mm}."
                : "Rascunho ainda não publicado.";
        }
        ApplyMarketingPromotionFilters();
        UpdateMarketingPromotionSummary();
    }

    private static decimal SuggestedPromotionPrice(decimal originalPrice)
    {
        if (originalPrice <= 0)
        {
            return 0;
        }
        return Math.Round(originalPrice * 0.85m, 2, MidpointRounding.AwayFromZero);
    }

    private static string PromotionServiceCategory(ServiceItem service)
    {
        if (!string.IsNullOrWhiteSpace(service.Category))
        {
            return service.Category;
        }
        var search = $"{service.Segment} {service.Name}".ToLowerInvariant();
        if (search.Contains("unha") || search.Contains("manicure") || search.Contains("pedicure")) return "Unhas";
        if (search.Contains("sobrancel") || search.Contains("pele") || search.Contains("facial") || search.Contains("limpeza")) return "Estética";
        if (search.Contains("spa") || search.Contains("massag") || search.Contains("relax")) return "Spa";
        if (search.Contains("make") || search.Contains("maqui")) return "Maquiagem";
        if (search.Contains("cabelo") || search.Contains("corte") || search.Contains("escova") || search.Contains("coloração")) return "Cabelo";
        return "Outros";
    }

    private static string PromotionThumbnailPath(ServiceItem service)
    {
        var search = $"{service.Segment} {service.Category} {service.Name}".ToLowerInvariant();
        if (search.Contains("unha") || search.Contains("manicure") || search.Contains("pedicure"))
        {
            return "Assets/marketing-editorial-nails-nude.png";
        }
        if (search.Contains("spa") || search.Contains("massag") || search.Contains("relax"))
        {
            return "Assets/marketing-campaign-spa.png";
        }
        if (search.Contains("estét") || search.Contains("estet") || search.Contains("pele") || search.Contains("facial") || search.Contains("make"))
        {
            return "Assets/marketing-site-overview-makeup.png";
        }
        return "Assets/marketing-campaign-hair.png";
    }

    private void ApplyMarketingPromotionFilters()
    {
        var search = _marketingPromotionSearchTextBox?.Text.Trim() ?? "";
        var category = _marketingPromotionCategoryComboBox?.SelectedItem as string ?? "Todas as categorias";
        var view = CollectionViewSource.GetDefaultView(_marketingPromotionRows);
        view.Filter = item =>
        {
            if (item is not MarketingPromotionServiceRow row)
            {
                return false;
            }
            var matchesSearch = string.IsNullOrWhiteSpace(search) ||
                row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                row.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase);
            var matchesCategory = category == "Todas as categorias" ||
                string.Equals(row.Category, category, StringComparison.CurrentCultureIgnoreCase);
            return matchesSearch && matchesCategory;
        };
    }

    private void UpdateMarketingPromotionSummary()
    {
        if (_marketingPromotionSummaryText is null)
        {
            return;
        }

        var selected = _marketingPromotionRows.Where(row => row.IsSelected).ToList();
        var averageDiscount = selected.Count == 0
            ? 0
            : (int)Math.Round(selected.Average(row => row.DiscountPercent));
        var name = string.IsNullOrWhiteSpace(_marketingPromotionNameTextBox?.Text)
            ? "Promoção sem nome"
            : _marketingPromotionNameTextBox.Text.Trim();
        var start = _marketingPromotionStartDatePicker?.SelectedDate ?? DateTime.Today;
        var end = _marketingPromotionEndDatePicker?.SelectedDate ?? start;
        _marketingPromotionSummaryText.Text =
            $"{selected.Count} serviço(s) selecionado(s) • desconto médio de {averageDiscount}% • {start:dd/MM} a {end:dd/MM}";
        if (_marketingPromotionSelectedCountText is not null)
        {
            _marketingPromotionSelectedCountText.Text = selected.Count == 0
                ? "Selecione ao menos 1 serviço"
                : $"{selected.Count} serviço(s) selecionado(s)";
        }
        if (_marketingPromotionPreviewTitleText is not null)
        {
            _marketingPromotionPreviewTitleText.Text = name;
        }
        if (_marketingPromotionPreviewDetailText is not null)
        {
            _marketingPromotionPreviewDetailText.Text = selected.Count == 0
                ? "Selecione serviços para montar a oferta"
                : $"Até {selected.Max(row => row.DiscountPercent)}% OFF • válido até {end:dd/MM}";
        }
    }

    private bool TrySaveMarketingSitePromotion(bool publish, out MarketingSitePromotion promotion)
    {
        promotion = new MarketingSitePromotion();
        _marketingPromotionServicesGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
        _marketingPromotionServicesGrid?.CommitEdit(DataGridEditingUnit.Row, true);
        var selected = _marketingPromotionRows.Where(row => row.IsSelected).ToList();
        var name = _marketingPromotionNameTextBox?.Text.Trim() ?? "";
        var start = _marketingPromotionStartDatePicker?.SelectedDate?.Date ?? DateTime.Today;
        var end = _marketingPromotionEndDatePicker?.SelectedDate?.Date ?? start;
        if (!int.TryParse(_marketingPromotionLimitTextBox?.Text, out var limit))
        {
            limit = 1;
        }

        if (publish && string.IsNullOrWhiteSpace(name))
        {
            ShowMarketingPromotionValidation(
                "Dê um nome à promoção",
                "Preencha o nome que será exibido no catálogo antes de publicar.",
                _marketingPromotionNameTextBox);
            return false;
        }
        if (publish && selected.Count == 0)
        {
            ShowMarketingPromotionValidation(
                "Escolha ao menos um serviço",
                "Selecione um ou mais serviços da lista para montar e publicar a promoção.",
                _marketingPromotionServicesGrid);
            return false;
        }
        if (end < start)
        {
            ShowMarketingPromotionValidation(
                "Revise o período da promoção",
                "A data final deve ser igual ou posterior à data inicial.",
                _marketingPromotionEndDatePicker);
            return false;
        }
        var invalidPrice = selected.FirstOrDefault(row =>
            row.PromotionalPrice < 0 ||
            row.PromotionalPrice >= row.OriginalPrice);
        if (publish && invalidPrice is not null)
        {
            ShowMarketingPromotionValidation(
                "Revise o preço promocional",
                $"O preço promocional de “{invalidPrice.Name}” deve ser menor que o preço atual.",
                _marketingPromotionServicesGrid);
            return false;
        }

        promotion = new MarketingSitePromotion
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Semana do autocuidado" : name,
            StartDate = start,
            EndDate = end,
            LimitPerCustomer = Math.Clamp(limit, 1, 99),
            HighlightInCatalog = _marketingPromotionHighlightCheckBox?.IsChecked == true,
            IsPublished = publish,
            PublishedAt = publish ? DateTime.Now : null,
            Items = selected.Select(row => new MarketingSitePromotionItem
            {
                ServiceId = row.ServiceId,
                ServiceName = row.Name,
                OriginalPrice = row.OriginalPrice,
                PromotionalPrice = row.PromotionalPrice
            }).ToList()
        };
        _data.Settings.MarketingSitePromotion = promotion;

        if (publish)
        {
            EnsurePublishedCatalogForPromotion(promotion);
        }
        _store.Save(_data);
        return true;
    }

    private void ShowMarketingPromotionValidation(
        string title,
        string message,
        FrameworkElement? focusTarget)
    {
        var shell = CreateFinanceEditorDialog(
            title,
            "Ajuste esta informação para continuar.",
            "Entendi",
            PackIconKind.AlertCircleOutline,
            useBodyCard: false);

        shell.Dialog.Width = 560;
        shell.Dialog.MaxHeight = 420;
        shell.Body.Width = 490;
        shell.CancelButton.Visibility = Visibility.Collapsed;
        shell.PrimaryButton.MinWidth = 122;

        AddDialogInfoCard(
            shell.Body,
            "O que precisa ser corrigido",
            message,
            "#FFF7ED",
            "#FDBA74");

        shell.PrimaryButton.Click += (_, _) => shell.Dialog.DialogResult = true;
        ShowAppDialog(shell.Dialog);

        if (focusTarget is not null)
        {
            focusTarget.BringIntoView();
            focusTarget.Focus();
            Keyboard.Focus(focusTarget);
        }
    }

    private void EnsurePublishedCatalogForPromotion(MarketingSitePromotion promotion)
    {
        var settings = _data.Settings;
        var now = DateTime.Now;
        if (settings.PublishedMarketingCatalog is null)
        {
            var slug = string.IsNullOrWhiteSpace(settings.MarketingSiteDraftSlug)
                ? SlugifyPublicBookingStore(BusinessDisplayName())
                : SlugifyPublicBookingStore(settings.MarketingSiteDraftSlug);
            settings.PublishedMarketingCatalog = new MarketingCatalogPublication
            {
                AddressSnapshotVersion = 1,
                Slug = slug,
                CustomDomain = NormalizeMarketingSiteCustomDomain(settings.MarketingSiteDraftCustomDomain),
                Title = settings.MarketingSiteTitle,
                SupportText = settings.MarketingSiteSupportText,
                ButtonText = settings.MarketingSiteButtonText,
                HeroImagePath = settings.MarketingSiteHeroImagePath,
                AccentColor = settings.MarketingSiteAccentColor,
                Alignment = settings.MarketingSiteAlignment,
                Spacing = settings.MarketingSiteSpacing,
                TitleFont = settings.MarketingSiteTitleFont,
                ImageContrast = settings.MarketingSiteImageContrast,
                ShowButton = settings.MarketingSiteShowButton,
                Header = CloneMarketingCatalogHeader(settings.MarketingSiteHeader),
                Footer = CloneMarketingCatalogFooter(settings.MarketingSiteFooter),
                Design = CloneMarketingCatalogDesign(settings.MarketingSiteDesign),
                Sections = CloneMarketingCatalogSections(settings.MarketingSiteSections),
                SeoTitle = settings.MarketingSiteSeoTitle,
                SeoDescription = settings.MarketingSiteSeoDescription,
                PublishedAt = now
            };
        }
        settings.PublishedMarketingCatalog.Promotion = CloneMarketingSitePromotion(promotion);
        settings.PublishedMarketingCatalog.PublishedAt = now;
        settings.MarketingSitePublishedAt = now;
    }

    private void MarketingSitePromotionSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySaveMarketingSitePromotion(false, out _))
        {
            return;
        }
        if (_marketingPromotionStatusText is not null)
        {
            _marketingPromotionStatusText.Text = "Rascunho salvo. Os preços do PDV continuam inalterados.";
        }
        ShowStatus("Rascunho da promoção salvo.");
    }

    private void MarketingSitePromotionPublishButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySaveMarketingSitePromotion(true, out var promotion))
        {
            return;
        }
        ScheduleOnlineBookingSync();
        if (_marketingPromotionStatusText is not null)
        {
            _marketingPromotionStatusText.Text =
                $"Publicação enviada ao site • {promotion.Items.Count} serviço(s) • PDV preservado.";
        }
        ShowStatus("Promoção publicada no catálogo online.");
    }

    private sealed class MarketingPromotionServiceRow : INotifyPropertyChanged
    {
        private bool _isSelected;
        private decimal _promotionalPrice;

        public string ServiceId { get; init; } = "";
        public string Name { get; init; } = "";
        public string Category { get; init; } = "";
        public int DurationMinutes { get; init; }
        public decimal OriginalPrice { get; init; }
        public BitmapSource? Thumbnail { get; init; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionIcon)));
            }
        }

        public decimal PromotionalPrice
        {
            get => _promotionalPrice;
            set
            {
                if (_promotionalPrice == value) return;
                _promotionalPrice = Math.Max(0, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PromotionalPrice)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiscountText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiscountPercent)));
            }
        }

        public int DiscountPercent => OriginalPrice <= 0 || PromotionalPrice >= OriginalPrice
            ? 0
            : Math.Clamp((int)Math.Round((OriginalPrice - PromotionalPrice) / OriginalPrice * 100), 0, 100);
        public string DiscountText => DiscountPercent > 0 ? $"-{DiscountPercent}%" : "—";
        public string SelectionText => IsSelected ? "Remover" : "Marcar";
        public PackIconKind SelectionIcon => IsSelected
            ? PackIconKind.CheckboxMarkedOutline
            : PackIconKind.CheckboxBlankOutline;
        public string DurationText => $"{DurationMinutes} min";
        public string OriginalPriceText => OriginalPrice.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
