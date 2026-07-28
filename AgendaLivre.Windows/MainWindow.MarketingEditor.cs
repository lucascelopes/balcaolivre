using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private sealed class MarketingEditorLayerSnapshot
    {
        public string Key { get; init; } = "";
        public string Text { get; init; } = "";
        public string FontFamily { get; init; } = "Segoe UI";
        public double FontSize { get; init; }
        public Brush? Foreground { get; init; }
        public TextAlignment Alignment { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
        public double Opacity { get; init; }
        public Visibility Visibility { get; init; }
        public int ZIndex { get; init; }
    }

    private sealed class MarketingEditorSnapshot
    {
        public List<MarketingEditorLayerSnapshot> Layers { get; init; } = new();
        public ImageSource? Photo { get; init; }
        public bool PhotoVisible { get; init; }
        public double PhotoZoom { get; init; }
        public double PhotoX { get; init; }
        public double PhotoY { get; init; }
    }

    private readonly Stack<MarketingEditorSnapshot> _marketingEditorUndo = new();
    private readonly Stack<MarketingEditorSnapshot> _marketingEditorRedo = new();
    private readonly List<MarketingEditorSnapshot> _marketingEditorCopies = new();
    private bool _marketingEditorInitialized;
    private bool _marketingEditorApplying;
    private string _marketingEditorSelectedKey = "title";
    private ImageBrush? _marketingEditorPhotoBrush;
    private Border? _marketingEditorDraggingLayer;
    private Point _marketingEditorDragOrigin;
    private double _marketingEditorLayerOriginX;
    private double _marketingEditorLayerOriginY;
    private bool _marketingEditorDraggingPhoto;
    private Point _marketingEditorPhotoDragOrigin;
    private double _marketingEditorPhotoOriginX;
    private double _marketingEditorPhotoOriginY;

    private IEnumerable<string> MarketingEditorTextLayerKeys()
    {
        yield return "business";
        yield return "title";
        yield return "copy";
        yield return "slots";
        yield return "cta";
        yield return "phone";
        yield return "extra";
    }

    private Border? MarketingEditorLayer(string key) => key switch
    {
        "business" => MarketingEditorBusinessLayer,
        "title" => MarketingEditorTitleLayer,
        "copy" => MarketingEditorCopyLayer,
        "slots" => MarketingEditorSlotsLayer,
        "cta" => MarketingEditorCtaLayer,
        "phone" => MarketingEditorPhoneLayer,
        "extra" => MarketingEditorExtraLayer,
        _ => null
    };

    private TextBlock? MarketingEditorLayerText(string key) => key switch
    {
        "business" => MarketingEditorBusinessText,
        "title" => MarketingEditorTitleText,
        "copy" => MarketingEditorCopyText,
        "slots" => MarketingEditorSlotsText,
        "cta" => MarketingEditorCtaText,
        "phone" => MarketingEditorPhoneText,
        "extra" => MarketingEditorExtraText,
        _ => null
    };

    private CheckBox? MarketingEditorLayerVisibilityCheck(string key) => key switch
    {
        "business" => MarketingEditorBusinessVisibleCheck,
        "title" => MarketingEditorTitleVisibleCheck,
        "copy" => MarketingEditorCopyVisibleCheck,
        "slots" => MarketingEditorSlotsVisibleCheck,
        "cta" => MarketingEditorCtaVisibleCheck,
        "phone" => MarketingEditorPhoneVisibleCheck,
        "photo" => MarketingEditorPhotoVisibleCheck,
        "extra" => MarketingEditorExtraVisibleCheck,
        _ => null
    };

    private Border? MarketingEditorLayerRow(string key) => key switch
    {
        "business" => MarketingEditorBusinessLayerRow,
        "title" => MarketingEditorTitleLayerRow,
        "copy" => MarketingEditorCopyLayerRow,
        "slots" => MarketingEditorSlotsLayerRow,
        "cta" => MarketingEditorCtaLayerRow,
        "phone" => MarketingEditorPhoneLayerRow,
        "photo" => MarketingEditorPhotoLayerRow,
        "extra" => MarketingEditorExtraLayerRow,
        _ => null
    };

    private static string MarketingEditorLayerLabel(string key) => key switch
    {
        "business" => "Nome da empresa",
        "title" => "Título",
        "copy" => "Descrição",
        "slots" => "Horários",
        "cta" => "Botão",
        "phone" => "Telefone",
        "photo" => "Foto de fundo",
        "extra" => "Texto extra",
        _ => "Elemento"
    };

    private void UpdateMarketingEditorSelectionHandle(Border? layer)
    {
        if (MarketingEditorSelectionHandle == null || layer == null || layer.Visibility != Visibility.Visible)
        {
            if (MarketingEditorSelectionHandle != null)
            {
                MarketingEditorSelectionHandle.Visibility = Visibility.Collapsed;
            }
            return;
        }

        layer.UpdateLayout();
        double left = Canvas.GetLeft(layer);
        double top = Canvas.GetTop(layer);
        double width = !double.IsNaN(layer.Width) ? layer.Width : layer.ActualWidth;
        if (double.IsNaN(left))
        {
            left = 0;
        }
        if (double.IsNaN(top))
        {
            top = 0;
        }

        Canvas.SetLeft(MarketingEditorSelectionHandle, left + Math.Max(width, layer.ActualWidth) - 4);
        Canvas.SetTop(MarketingEditorSelectionHandle, top - 4);
        MarketingEditorSelectionHandle.Visibility = Visibility.Visible;
    }

    private void InitializeMarketingEditor()
    {
        if (_marketingEditorInitialized || MarketingEditorPreviewCard == null)
        {
            return;
        }

        _marketingEditorApplying = true;
        try
        {
            _marketingEditorInitialized = true;
            MarketingEditorCampaignTitleTextBox.Text = MarketingStudioTitleValue();
            MarketingEditorCampaignCopyTextBox.Text = MarketingStudioCopyValue();
            MarketingEditorBusinessText.Text = BusinessDisplayName();
            MarketingEditorPhoneText.Text = FormatPhone(string.IsNullOrWhiteSpace(_data.Settings.BusinessPhone)
                ? _data.Settings.AccountPhone
                : _data.Settings.BusinessPhone);

            ImageSource? initialPhoto = (MarketingStudioPreviewCard.Background as ImageBrush)?.ImageSource;
            SetMarketingEditorPhoto(initialPhoto);
            MarketingEditorPreviewZoomHost.LayoutTransform = new ScaleTransform(0.9, 0.9);
            MarketingEditorSyncSlotsFromLegacy();
            SelectMarketingEditorLayer("title");
            MarketingEditorUpdateSummary();
        }
        finally
        {
            _marketingEditorApplying = false;
        }
    }

    private void MarketingEditorSyncSlotsFromLegacy()
    {
        if (!_marketingEditorInitialized || MarketingStudioSlot1Check == null)
        {
            return;
        }

        _marketingEditorApplying = true;
        try
        {
            CheckBox[] legacy =
            {
                MarketingStudioSlot1Check, MarketingStudioSlot2Check, MarketingStudioSlot3Check,
                MarketingStudioSlot4Check, MarketingStudioSlot5Check
            };
            CheckBox[] editor =
            {
                MarketingEditorSlot1Check, MarketingEditorSlot2Check, MarketingEditorSlot3Check,
                MarketingEditorSlot4Check, MarketingEditorSlot5Check
            };
            for (int i = 0; i < editor.Length; i++)
            {
                editor[i].Content = legacy[i].Content;
                editor[i].Visibility = legacy[i].Visibility;
                editor[i].IsChecked = legacy[i].IsChecked;
            }
            MarketingEditorUpdateSlots();
        }
        finally
        {
            _marketingEditorApplying = false;
        }
    }

    private List<string> SelectedMarketingEditorSlots()
    {
        if (!_marketingEditorInitialized)
        {
            return new List<string>();
        }

        return new[]
            {
                MarketingEditorSlot1Check, MarketingEditorSlot2Check, MarketingEditorSlot3Check,
                MarketingEditorSlot4Check, MarketingEditorSlot5Check
            }
            .Where(item => item.Visibility == Visibility.Visible && item.IsChecked == true)
            .Select(item => item.Content?.ToString() ?? "")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private void MarketingEditorUpdateSlots()
    {
        if (!_marketingEditorInitialized)
        {
            return;
        }

        List<string> slots = SelectedMarketingEditorSlots();
        MarketingEditorSlotsText.Text = slots.Count == 0
            ? "Selecione os horários"
            : string.Join(Environment.NewLine, slots.Chunk(2).Select(chunk => string.Join("  •  ", chunk)));
        MarketingEditorSelectedSlotsText.Text = slots.Count switch
        {
            0 => "Nenhum selecionado",
            1 => "1 selecionado",
            _ => $"{slots.Count} selecionados"
        };
        MarketingEditorUpdateSummary();
    }

    private void MarketingEditorUpdateSummary()
    {
        if (!_marketingEditorInitialized)
        {
            return;
        }

        int visibleElements = MarketingEditorTextLayerKeys()
            .Select(MarketingEditorLayer)
            .Count(layer => layer?.Visibility == Visibility.Visible);
        if (MarketingEditorPhotoVisibleCheck.IsChecked == true)
        {
            visibleElements++;
        }

        List<string> slots = SelectedMarketingEditorSlots();
        string format = _marketingStudioChannel == "post" ? "Post (1080×1080)" : "Story (1080×1920)";
        MarketingEditorPublishChannelText.Text = "WhatsApp";
        MarketingEditorSummaryText.Text =
            $"{format} • {visibleElements} elementos\n{slots.Count} horário{(slots.Count == 1 ? "" : "s")} selecionado{(slots.Count == 1 ? "" : "s")}";
    }

    private MarketingEditorSnapshot CaptureMarketingEditorSnapshot()
    {
        var snapshot = new MarketingEditorSnapshot
        {
            Photo = _marketingEditorPhotoBrush?.ImageSource,
            PhotoVisible = MarketingEditorPhotoVisibleCheck.IsChecked == true,
            PhotoZoom = MarketingEditorPhotoZoomSlider.Value,
            PhotoX = MarketingEditorPhotoXSlider.Value,
            PhotoY = MarketingEditorPhotoYSlider.Value
        };

        foreach (string key in MarketingEditorTextLayerKeys())
        {
            Border? layer = MarketingEditorLayer(key);
            TextBlock? text = MarketingEditorLayerText(key);
            if (layer == null || text == null)
            {
                continue;
            }

            snapshot.Layers.Add(new MarketingEditorLayerSnapshot
            {
                Key = key,
                Text = text.Text,
                FontFamily = text.FontFamily.Source,
                FontSize = text.FontSize,
                Foreground = text.Foreground?.CloneCurrentValue(),
                Alignment = text.TextAlignment,
                X = Canvas.GetLeft(layer),
                Y = Canvas.GetTop(layer),
                Opacity = layer.Opacity,
                Visibility = layer.Visibility,
                ZIndex = Panel.GetZIndex(layer)
            });
        }

        return snapshot;
    }

    private void ApplyMarketingEditorSnapshot(MarketingEditorSnapshot snapshot)
    {
        _marketingEditorApplying = true;
        try
        {
            foreach (MarketingEditorLayerSnapshot item in snapshot.Layers)
            {
                Border? layer = MarketingEditorLayer(item.Key);
                TextBlock? text = MarketingEditorLayerText(item.Key);
                if (layer == null || text == null)
                {
                    continue;
                }

                text.Text = item.Text;
                text.FontFamily = new FontFamily(item.FontFamily);
                text.FontSize = item.FontSize;
                text.Foreground = item.Foreground?.CloneCurrentValue() ?? Brushes.Black;
                text.TextAlignment = item.Alignment;
                Canvas.SetLeft(layer, item.X);
                Canvas.SetTop(layer, item.Y);
                layer.Opacity = item.Opacity;
                layer.Visibility = item.Visibility;
                Panel.SetZIndex(layer, item.ZIndex);
                CheckBox? check = MarketingEditorLayerVisibilityCheck(item.Key);
                if (check != null)
                {
                    check.IsChecked = item.Visibility == Visibility.Visible;
                }
            }

            MarketingEditorExtraLayerRow.Visibility = MarketingEditorExtraLayer.Visibility;
            MarketingEditorPhotoZoomSlider.Value = snapshot.PhotoZoom;
            MarketingEditorPhotoXSlider.Value = snapshot.PhotoX;
            MarketingEditorPhotoYSlider.Value = snapshot.PhotoY;
            MarketingEditorPhotoVisibleCheck.IsChecked = snapshot.PhotoVisible;
            SetMarketingEditorPhoto(snapshot.Photo);
            ApplyMarketingEditorPhotoCrop();
            MarketingEditorCampaignTitleTextBox.Text = MarketingEditorTitleText.Text;
            MarketingEditorCampaignCopyTextBox.Text = MarketingEditorCopyText.Text;
            MarketingStudioTitleTextBox.Text = MarketingEditorCampaignTitleTextBox.Text;
            MarketingStudioCopyTextBox.Text = MarketingEditorCampaignCopyTextBox.Text;
            SelectMarketingEditorLayer(_marketingEditorSelectedKey);
            MarketingEditorUpdateSummary();
        }
        finally
        {
            _marketingEditorApplying = false;
        }
    }

    private void PushMarketingEditorUndo()
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }

        _marketingEditorUndo.Push(CaptureMarketingEditorSnapshot());
        _marketingEditorRedo.Clear();
        if (_marketingEditorUndo.Count > 80)
        {
            MarketingEditorSnapshot[] keep = _marketingEditorUndo.Take(60).Reverse().ToArray();
            _marketingEditorUndo.Clear();
            foreach (MarketingEditorSnapshot item in keep)
            {
                _marketingEditorUndo.Push(item);
            }
        }
    }

    private void SetMarketingEditorPhoto(ImageSource? source)
    {
        if (source == null)
        {
            _marketingEditorPhotoBrush = null;
            MarketingEditorPreviewCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E6DA"));
            return;
        }

        _marketingEditorPhotoBrush = new ImageBrush(source)
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
            ViewboxUnits = BrushMappingMode.RelativeToBoundingBox
        };
        ApplyMarketingEditorPhotoCrop();
    }

    private void ApplyMarketingEditorPhotoCrop()
    {
        if (!_marketingEditorInitialized || _marketingEditorPhotoBrush == null)
        {
            return;
        }

        double zoom = Math.Max(1, MarketingEditorPhotoZoomSlider.Value);
        double size = 1d / zoom;
        double available = 1d - size;
        double x = available * ((MarketingEditorPhotoXSlider.Value + 100d) / 200d);
        double y = available * ((MarketingEditorPhotoYSlider.Value + 100d) / 200d);
        _marketingEditorPhotoBrush.Viewbox = new Rect(x, y, size, size);
        MarketingEditorPreviewCard.Background = MarketingEditorPhotoVisibleCheck.IsChecked == true
            ? _marketingEditorPhotoBrush
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E6DA"));
    }

    private void SelectMarketingEditorLayer(string key)
    {
        if (!_marketingEditorInitialized)
        {
            return;
        }

        _marketingEditorSelectedKey = key;
        foreach (string layerKey in MarketingEditorTextLayerKeys())
        {
            Border? layer = MarketingEditorLayer(layerKey);
            if (layer != null)
            {
                layer.BorderBrush = layerKey == key
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"))
                    : Brushes.Transparent;
                layer.BorderThickness = new Thickness(1);
            }

            Border? row = MarketingEditorLayerRow(layerKey);
            if (row != null)
            {
                row.Background = layerKey == key
                    ? FindResource("AccentSoft") as Brush
                    : Brushes.Transparent;
                row.BorderBrush = layerKey == key
                    ? FindResource("Accent") as Brush
                    : FindResource("Line") as Brush;
            }
        }

        Border? photoRow = MarketingEditorLayerRow("photo");
        if (photoRow != null)
        {
            photoRow.Background = key == "photo" ? FindResource("AccentSoft") as Brush : Brushes.Transparent;
            photoRow.BorderBrush = key == "photo" ? FindResource("Accent") as Brush : FindResource("Line") as Brush;
        }

        MarketingEditorSelectedLayerText.Text = MarketingEditorLayerLabel(key) + " selecionado";
        if (key == "photo")
        {
            MarketingEditorDragHintText.Text = "Arraste a foto para ajustar o recorte";
            MarketingEditorSelectionHandle.Visibility = Visibility.Collapsed;
            MarketingEditorImageInspectorTab.IsChecked = true;
            ShowMarketingEditorInspector("image");
            return;
        }

        MarketingEditorDragHintText.Text = "Arraste o elemento na arte";

        TextBlock? selectedText = MarketingEditorLayerText(key);
        Border? selectedLayer = MarketingEditorLayer(key);
        if (selectedText == null || selectedLayer == null)
        {
            MarketingEditorSelectionHandle.Visibility = Visibility.Collapsed;
            return;
        }

        UpdateMarketingEditorSelectionHandle(selectedLayer);

        _marketingEditorApplying = true;
        try
        {
            MarketingEditorElementTextBox.Text = selectedText.Text;
            MarketingEditorFontSizeSlider.Value = Math.Clamp(selectedText.FontSize, 6, 36);
            MarketingEditorFontSizeText.Text = $"{selectedText.FontSize:0.#} px";
            MarketingEditorColorTextBox.Text = selectedText.Foreground is SolidColorBrush color
                ? color.Color.ToString()
                : "#000000";
            MarketingEditorPositionXTextBox.Text = Canvas.GetLeft(selectedLayer).ToString("0", CultureInfo.InvariantCulture);
            MarketingEditorPositionYTextBox.Text = Canvas.GetTop(selectedLayer).ToString("0", CultureInfo.InvariantCulture);
            MarketingEditorOpacitySlider.Value = selectedLayer.Opacity * 100d;
            MarketingEditorOpacityText.Text = $"{selectedLayer.Opacity * 100d:0}%";
            MarketingEditorInspectorVisibleCheck.IsChecked = selectedLayer.Visibility == Visibility.Visible;

            string selectedFamily = selectedText.FontFamily.Source;
            foreach (ComboBoxItem item in MarketingEditorFontCombo.Items)
            {
                if (string.Equals(item.Tag?.ToString(), selectedFamily, StringComparison.OrdinalIgnoreCase))
                {
                    MarketingEditorFontCombo.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _marketingEditorApplying = false;
        }
    }

    private void ShowMarketingEditorInspector(string tab)
    {
        MarketingEditorTextInspectorPanel.Visibility = tab == "text" ? Visibility.Visible : Visibility.Collapsed;
        MarketingEditorImageInspectorPanel.Visibility = tab == "image" ? Visibility.Visible : Visibility.Collapsed;
        MarketingEditorStyleInspectorPanel.Visibility = tab == "style" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MarketingEditorChannel_Checked(object sender, RoutedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying || sender is not RadioButton { Tag: string channel })
        {
            return;
        }

        _marketingStudioChannel = channel;
        _marketingEditorApplying = true;
        try
        {
            if (channel == "story")
            {
                MarketingStudioStoryTab.IsChecked = true;
            }
            else if (channel == "post")
            {
                MarketingStudioPostTab.IsChecked = true;
            }
            else
            {
                MarketingStudioWhatsAppTab.IsChecked = true;
            }
        }
        finally
        {
            _marketingEditorApplying = false;
        }
        MarketingEditorUpdateSummary();
    }

    private void MarketingEditorCampaign_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }

        PushMarketingEditorUndo();
        _marketingEditorApplying = true;
        try
        {
            MarketingEditorTitleText.Text = MarketingEditorCampaignTitleTextBox.Text.Trim().ToUpperInvariant();
            MarketingEditorCopyText.Text = MarketingEditorCampaignCopyTextBox.Text;
            MarketingEditorCopyCountText.Text = $"{MarketingEditorCampaignCopyTextBox.Text.Length}/500";
            MarketingStudioTitleTextBox.Text = MarketingEditorCampaignTitleTextBox.Text;
            MarketingStudioCopyTextBox.Text = MarketingEditorCampaignCopyTextBox.Text;
        }
        finally
        {
            _marketingEditorApplying = false;
        }

        if (_marketingEditorSelectedKey is "title" or "copy")
        {
            SelectMarketingEditorLayer(_marketingEditorSelectedKey);
        }
    }

    private void MarketingEditorSlot_Changed(object sender, RoutedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }

        PushMarketingEditorUndo();
        CheckBox[] editor =
        {
            MarketingEditorSlot1Check, MarketingEditorSlot2Check, MarketingEditorSlot3Check,
            MarketingEditorSlot4Check, MarketingEditorSlot5Check
        };
        CheckBox[] legacy =
        {
            MarketingStudioSlot1Check, MarketingStudioSlot2Check, MarketingStudioSlot3Check,
            MarketingStudioSlot4Check, MarketingStudioSlot5Check
        };
        _marketingEditorApplying = true;
        try
        {
            for (int i = 0; i < editor.Length; i++)
            {
                legacy[i].IsChecked = editor[i].IsChecked;
            }
        }
        finally
        {
            _marketingEditorApplying = false;
        }
        MarketingEditorUpdateSlots();
    }

    private void MarketingEditorLayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string key })
        {
            SelectMarketingEditorLayer(key);
        }
    }

    private void MarketingEditorLayerVisibility_Changed(object sender, RoutedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying || sender is not CheckBox { Tag: string key } check)
        {
            return;
        }

        PushMarketingEditorUndo();
        if (key == "photo")
        {
            ApplyMarketingEditorPhotoCrop();
        }
        else
        {
            Border? layer = MarketingEditorLayer(key);
            if (layer != null)
            {
                layer.Visibility = check.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        MarketingEditorUpdateSummary();
    }

    private void MarketingEditorInspectorVisibility_Changed(object sender, RoutedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying || _marketingEditorSelectedKey == "photo")
        {
            return;
        }

        PushMarketingEditorUndo();
        Border? layer = MarketingEditorLayer(_marketingEditorSelectedKey);
        CheckBox? listCheck = MarketingEditorLayerVisibilityCheck(_marketingEditorSelectedKey);
        bool visible = MarketingEditorInspectorVisibleCheck.IsChecked == true;
        if (layer != null)
        {
            layer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
        _marketingEditorApplying = true;
        try
        {
            if (listCheck != null)
            {
                listCheck.IsChecked = visible;
            }
        }
        finally
        {
            _marketingEditorApplying = false;
        }
        MarketingEditorUpdateSummary();
    }

    private void MarketingEditorInspectorTab_Checked(object sender, RoutedEventArgs e)
    {
        if (!_marketingEditorInitialized || sender is not RadioButton { Tag: string tab })
        {
            return;
        }
        ShowMarketingEditorInspector(tab);
    }

    private void MarketingEditorInspectorText_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }

        TextBlock? text = MarketingEditorLayerText(_marketingEditorSelectedKey);
        if (text == null)
        {
            return;
        }

        PushMarketingEditorUndo();
        text.Text = MarketingEditorElementTextBox.Text;
        UpdateMarketingEditorSelectionHandle(MarketingEditorLayer(_marketingEditorSelectedKey));
        _marketingEditorApplying = true;
        try
        {
            if (_marketingEditorSelectedKey == "title")
            {
                MarketingEditorCampaignTitleTextBox.Text = text.Text;
                MarketingStudioTitleTextBox.Text = text.Text;
            }
            else if (_marketingEditorSelectedKey == "copy")
            {
                MarketingEditorCampaignCopyTextBox.Text = text.Text;
                MarketingStudioCopyTextBox.Text = text.Text;
            }
        }
        finally
        {
            _marketingEditorApplying = false;
        }
    }

    private void MarketingEditorFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying ||
            MarketingEditorFontCombo.SelectedItem is not ComboBoxItem selected)
        {
            return;
        }

        TextBlock? text = MarketingEditorLayerText(_marketingEditorSelectedKey);
        if (text == null)
        {
            return;
        }
        PushMarketingEditorUndo();
        text.FontFamily = new FontFamily(selected.Tag?.ToString() ?? "Segoe UI");
    }

    private void MarketingEditorFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }

        TextBlock? text = MarketingEditorLayerText(_marketingEditorSelectedKey);
        if (text == null)
        {
            return;
        }
        PushMarketingEditorUndo();
        text.FontSize = MarketingEditorFontSizeSlider.Value;
        MarketingEditorFontSizeText.Text = $"{text.FontSize:0.#} px";
        UpdateMarketingEditorSelectionHandle(MarketingEditorLayer(_marketingEditorSelectedKey));
    }

    private void MarketingEditorColor_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }

        TextBlock? text = MarketingEditorLayerText(_marketingEditorSelectedKey);
        if (text == null)
        {
            return;
        }

        try
        {
            Color color = (Color)ColorConverter.ConvertFromString(MarketingEditorColorTextBox.Text);
            PushMarketingEditorUndo();
            text.Foreground = new SolidColorBrush(color);
            MarketingEditorColorTextBox.BorderBrush = FindResource("Line") as Brush;
        }
        catch (FormatException)
        {
            MarketingEditorColorTextBox.BorderBrush = Brushes.IndianRed;
        }
    }

    private void MarketingEditorAlign_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string alignment })
        {
            return;
        }
        TextBlock? text = MarketingEditorLayerText(_marketingEditorSelectedKey);
        if (text == null)
        {
            return;
        }
        PushMarketingEditorUndo();
        text.TextAlignment = alignment switch
        {
            "left" => TextAlignment.Left,
            "right" => TextAlignment.Right,
            _ => TextAlignment.Center
        };
    }

    private void MarketingEditorPosition_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }
        Border? layer = MarketingEditorLayer(_marketingEditorSelectedKey);
        if (layer == null ||
            !double.TryParse(MarketingEditorPositionXTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
            !double.TryParse(MarketingEditorPositionYTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
        {
            return;
        }
        PushMarketingEditorUndo();
        Canvas.SetLeft(layer, Math.Clamp(x, -layer.ActualWidth + 12, MarketingEditorCanvas.Width - 12));
        Canvas.SetTop(layer, Math.Clamp(y, -layer.ActualHeight + 12, MarketingEditorCanvas.Height - 12));
        UpdateMarketingEditorSelectionHandle(layer);
    }

    private void MarketingEditorOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }
        Border? layer = MarketingEditorLayer(_marketingEditorSelectedKey);
        if (layer == null)
        {
            return;
        }
        PushMarketingEditorUndo();
        layer.Opacity = MarketingEditorOpacitySlider.Value / 100d;
        MarketingEditorOpacityText.Text = $"{MarketingEditorOpacitySlider.Value:0}%";
    }

    private void MarketingEditorLayerOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string direction })
        {
            return;
        }
        Border? layer = MarketingEditorLayer(_marketingEditorSelectedKey);
        if (layer == null)
        {
            return;
        }
        PushMarketingEditorUndo();
        int current = Panel.GetZIndex(layer);
        Panel.SetZIndex(layer, Math.Clamp(current + (direction == "up" ? 1 : -1), -10, 20));
        ShowStatus(direction == "up" ? "Elemento trazido para a frente." : "Elemento enviado para trás.");
    }

    private void MarketingEditorLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: string key } layer)
        {
            return;
        }
        SelectMarketingEditorLayer(key);
        PushMarketingEditorUndo();
        _marketingEditorDraggingLayer = layer;
        _marketingEditorDragOrigin = e.GetPosition(MarketingEditorCanvas);
        _marketingEditorLayerOriginX = Canvas.GetLeft(layer);
        _marketingEditorLayerOriginY = Canvas.GetTop(layer);
        layer.CaptureMouse();
        e.Handled = true;
    }

    private void MarketingEditorLayer_MouseMove(object sender, MouseEventArgs e)
    {
        if (_marketingEditorDraggingLayer == null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }
        Point current = e.GetPosition(MarketingEditorCanvas);
        double x = _marketingEditorLayerOriginX + current.X - _marketingEditorDragOrigin.X;
        double y = _marketingEditorLayerOriginY + current.Y - _marketingEditorDragOrigin.Y;
        x = Math.Clamp(x, -_marketingEditorDraggingLayer.ActualWidth + 12, MarketingEditorCanvas.Width - 12);
        y = Math.Clamp(y, -_marketingEditorDraggingLayer.ActualHeight + 12, MarketingEditorCanvas.Height - 12);
        Canvas.SetLeft(_marketingEditorDraggingLayer, x);
        Canvas.SetTop(_marketingEditorDraggingLayer, y);
        UpdateMarketingEditorSelectionHandle(_marketingEditorDraggingLayer);
        _marketingEditorApplying = true;
        try
        {
            MarketingEditorPositionXTextBox.Text = x.ToString("0", CultureInfo.InvariantCulture);
            MarketingEditorPositionYTextBox.Text = y.ToString("0", CultureInfo.InvariantCulture);
        }
        finally
        {
            _marketingEditorApplying = false;
        }
    }

    private void MarketingEditorLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_marketingEditorDraggingLayer == null)
        {
            return;
        }
        _marketingEditorDraggingLayer.ReleaseMouseCapture();
        _marketingEditorDraggingLayer = null;
        e.Handled = true;
    }

    private void MarketingEditorBackground_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectMarketingEditorLayer("photo");
        if (_marketingEditorPhotoBrush == null)
        {
            return;
        }

        PushMarketingEditorUndo();
        if (MarketingEditorPhotoZoomSlider.Value <= 1.01)
        {
            _marketingEditorApplying = true;
            try
            {
                MarketingEditorPhotoZoomSlider.Value = 1.15;
            }
            finally
            {
                _marketingEditorApplying = false;
            }
            ApplyMarketingEditorPhotoCrop();
        }
        _marketingEditorDraggingPhoto = true;
        _marketingEditorPhotoDragOrigin = e.GetPosition(MarketingEditorPreviewCard);
        _marketingEditorPhotoOriginX = MarketingEditorPhotoXSlider.Value;
        _marketingEditorPhotoOriginY = MarketingEditorPhotoYSlider.Value;
        MarketingEditorPreviewCard.CaptureMouse();
        e.Handled = true;
    }

    private void MarketingEditorBackground_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_marketingEditorDraggingPhoto || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current = e.GetPosition(MarketingEditorPreviewCard);
        double deltaX = current.X - _marketingEditorPhotoDragOrigin.X;
        double deltaY = current.Y - _marketingEditorPhotoDragOrigin.Y;
        MarketingEditorPhotoXSlider.Value = Math.Clamp(
            _marketingEditorPhotoOriginX - (deltaX / MarketingEditorPreviewCard.Width * 200d),
            MarketingEditorPhotoXSlider.Minimum,
            MarketingEditorPhotoXSlider.Maximum);
        MarketingEditorPhotoYSlider.Value = Math.Clamp(
            _marketingEditorPhotoOriginY - (deltaY / MarketingEditorPreviewCard.Height * 200d),
            MarketingEditorPhotoYSlider.Minimum,
            MarketingEditorPhotoYSlider.Maximum);
        e.Handled = true;
    }

    private void MarketingEditorBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_marketingEditorDraggingPhoto)
        {
            return;
        }

        _marketingEditorDraggingPhoto = false;
        MarketingEditorPreviewCard.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void MarketingEditorChoosePhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Escolher foto de fundo",
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos os arquivos|*.*"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            PushMarketingEditorUndo();
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            SetMarketingEditorPhoto(bitmap);
            MarketingEditorPhotoVisibleCheck.IsChecked = true;
            MarketingStudioSelectedImageCard.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
            MarketingPhotoCreditPanel.Visibility = Visibility.Collapsed;
            ShowStatus("Foto carregada. Ajuste o zoom e a posição no painel Imagem.");
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Não foi possível abrir esta imagem.\n\n" + ex.Message,
                "Marketing", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }

    private void MarketingEditorPhotoAdjustment_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_marketingEditorInitialized || _marketingEditorApplying)
        {
            return;
        }
        PushMarketingEditorUndo();
        ApplyMarketingEditorPhotoCrop();
    }

    private void MarketingEditorResetPhotoCrop_Click(object sender, RoutedEventArgs e)
    {
        PushMarketingEditorUndo();
        _marketingEditorApplying = true;
        try
        {
            MarketingEditorPhotoZoomSlider.Value = 1;
            MarketingEditorPhotoXSlider.Value = 0;
            MarketingEditorPhotoYSlider.Value = 0;
        }
        finally
        {
            _marketingEditorApplying = false;
        }
        ApplyMarketingEditorPhotoCrop();
    }

    private void MarketingEditorUndo_Click(object sender, RoutedEventArgs e)
    {
        if (_marketingEditorUndo.Count == 0)
        {
            ShowStatus("Ainda não há alterações para desfazer.");
            return;
        }
        _marketingEditorRedo.Push(CaptureMarketingEditorSnapshot());
        ApplyMarketingEditorSnapshot(_marketingEditorUndo.Pop());
        ShowStatus("Última alteração desfeita.");
    }

    private void MarketingEditorRedo_Click(object sender, RoutedEventArgs e)
    {
        if (_marketingEditorRedo.Count == 0)
        {
            ShowStatus("Ainda não há alterações para refazer.");
            return;
        }
        _marketingEditorUndo.Push(CaptureMarketingEditorSnapshot());
        ApplyMarketingEditorSnapshot(_marketingEditorRedo.Pop());
        ShowStatus("Alteração refeita.");
    }

    private void MarketingEditorZoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_marketingEditorInitialized || MarketingEditorZoomCombo.SelectedItem is not ComboBoxItem item ||
            !double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double zoom))
        {
            return;
        }
        MarketingEditorPreviewZoomHost.LayoutTransform = new ScaleTransform(zoom, zoom);
    }

    private void MarketingEditorFit_Click(object sender, RoutedEventArgs e)
    {
        MarketingEditorZoomCombo.SelectedIndex = 1;
        ShowStatus("Arte ajustada à área de edição.");
    }

    private void MarketingEditorAddText_Click(object sender, RoutedEventArgs e)
    {
        PushMarketingEditorUndo();
        MarketingEditorExtraLayer.Visibility = Visibility.Visible;
        MarketingEditorExtraLayerRow.Visibility = Visibility.Visible;
        MarketingEditorExtraVisibleCheck.IsChecked = true;
        SelectMarketingEditorLayer("extra");
        MarketingEditorTextInspectorTab.IsChecked = true;
        MarketingEditorElementTextBox.Focus();
        MarketingEditorElementTextBox.SelectAll();
        MarketingEditorUpdateSummary();
    }

    private void MarketingEditorDuplicate_Click(object sender, RoutedEventArgs e)
    {
        _marketingEditorCopies.Add(CaptureMarketingEditorSnapshot());
        ShowStatus($"Cópia {_marketingEditorCopies.Count + 1} criada. A arte atual continua pronta para edição.");
    }

    private void SetMarketingEditorSelectionChrome(bool visible)
    {
        if (!_marketingEditorInitialized)
        {
            return;
        }
        foreach (string key in MarketingEditorTextLayerKeys())
        {
            Border? layer = MarketingEditorLayer(key);
            if (layer != null)
            {
                layer.BorderBrush = visible && key == _marketingEditorSelectedKey
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"))
                    : Brushes.Transparent;
            }
        }
    }
}
