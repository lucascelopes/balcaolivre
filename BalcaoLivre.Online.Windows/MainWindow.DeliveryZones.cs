using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Windows.Devices.Geolocation;
using CheckBox = System.Windows.Controls.CheckBox;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private DeliveryZoneFee? ResolveDeliveryZoneFee(string district)
    {
        return _appSettings.DeliveryZones
            .Where(zone => zone.Active)
            .OrderBy(zone => zone.RadiusKm <= 0 ? double.MaxValue : zone.RadiusKm)
            .FirstOrDefault();
    }

    private void ApplyDeliveryZoneFee(string district, System.Windows.Controls.TextBox feeBox, TextBlock zoneText)
    {
        var zone = ResolveDeliveryZoneFee(district);
        if (zone is null)
        {
            zoneText.Text = "Sem circulo cadastrado. Informe a taxa manual ou cadastre raios no mapa.";
            zoneText.Foreground = AmberText;
            return;
        }

        feeBox.Text = zone.Fee.ToString("N2", Brazil);
        var radius = zone.RadiusKm > 0 ? $"ate {zone.RadiusKm:N1} km" : "raio nao informado";
        zoneText.Text = $"Taxa sugerida pelo mapa: {radius} ({Money(zone.Fee)}). Ajuste se o cliente estiver em outro raio.";
        zoneText.Foreground = GreenText;
    }

    private void ShowDeliveryZonesDialog()
    {
        if (!RequirePermission(CanManageDeliveryZones, "Taxas por raio no mapa"))
        {
            return;
        }

        var dialog = CreateDialog("Taxas de entrega", 1140, 720);
        var zonesList = new ListBox
        {
            DisplayMemberPath = nameof(DeliveryZoneFee.Display),
            Height = 185,
            ItemsSource = _appSettings.DeliveryZones
        };
        var radiusBox = new TextBox();
        var feeBox = new TextBox();
        var minimumBox = new TextBox();
        var activeBox = new CheckBox { Content = "Ativa", IsChecked = true, Margin = new Thickness(0, 4, 0, 4) };
        var radiusPreviewCanvas = new Canvas { Width = 250, Height = 150, Margin = new Thickness(0, 10, 0, 6) };
        var radiusPreviewText = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };
        var radiusPreviewCard = new Border
        {
            Background = Solid("#F7FAFD"),
            BorderBrush = Solid("#CAD6E2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 12, 0, 12)
        };
        var status = new TextBlock { Foreground = GreenText, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        var mapStatus = new TextBlock
        {
            Foreground = Solid("#5B6B7A"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        var hasSavedLocation = IsValidMapCoordinate(_profile.Latitude, _profile.Longitude);
        var currentLatitude = hasSavedLocation ? _profile.Latitude : -23.55052;
        var currentLongitude = hasSavedLocation ? _profile.Longitude : -46.633308;
        var currentLabel = hasSavedLocation ? "Local salvo" : "Centro temporario";
        mapStatus.Text = hasSavedLocation
            ? $"Centro da loja salvo: {currentLatitude:N5}, {currentLongitude:N5}"
            : "Centro ainda nao salvo. Use o local do Windows para posicionar a loja.";

        var mapView = new WebView2
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        double DisplayRadiusFor(DeliveryZoneFee zone)
        {
            if (zone.RadiusKm > 0)
            {
                return zone.RadiusKm;
            }

            var index = Math.Max(0, _appSettings.DeliveryZones.IndexOf(zone));
            return index + 1;
        }

        double NextRadius()
        {
            var max = _appSettings.DeliveryZones
                .Select(DisplayRadiusFor)
                .DefaultIfEmpty(0)
                .Max();
            return Math.Max(1, Math.Ceiling(max + 1));
        }

        void RefreshZonesList(DeliveryZoneFee? selected = null)
        {
            _appSettings.DeliveryZones = _appSettings.DeliveryZones
                .OrderBy(zone => zone.RadiusKm <= 0 ? double.MaxValue : zone.RadiusKm)
                .ThenBy(zone => zone.Fee)
                .ToList();
            zonesList.ItemsSource = null;
            zonesList.ItemsSource = _appSettings.DeliveryZones;
            zonesList.SelectedItem = selected is not null && _appSettings.DeliveryZones.Contains(selected)
                ? selected
                : _appSettings.DeliveryZones.FirstOrDefault();
        }

        void LoadZone(DeliveryZoneFee zone)
        {
            radiusBox.Text = DisplayRadiusFor(zone).ToString("N1", Brazil);
            feeBox.Text = zone.Fee.ToString("N2", Brazil);
            minimumBox.Text = zone.MinimumOrder.ToString("N2", Brazil);
            activeBox.IsChecked = zone.Active;
            RefreshRadiusPreview();
        }

        void RefreshRadiusPreview()
        {
            radiusPreviewCanvas.Children.Clear();
            var radius = ParseDouble(radiusBox.Text, 0);
            var fee = ParseMoney(feeBox.Text, 0);
            var minimum = ParseMoney(minimumBox.Text, 0);
            var centerX = radiusPreviewCanvas.Width / 2;
            var centerY = radiusPreviewCanvas.Height / 2;

            void AddRing(double size, System.Windows.Media.Brush stroke, double thickness, double opacity = 1)
            {
                var ring = new System.Windows.Shapes.Ellipse
                {
                    Width = size,
                    Height = size,
                    Stroke = stroke,
                    StrokeThickness = thickness,
                    Fill = Solid("#E6FBF8"),
                    Opacity = opacity
                };
                Canvas.SetLeft(ring, centerX - size / 2);
                Canvas.SetTop(ring, centerY - size / 2);
                radiusPreviewCanvas.Children.Add(ring);
            }

            AddRing(132, Solid("#CAD6E2"), 1, 0.52);
            AddRing(92, Solid("#CAD6E2"), 1, 0.68);
            AddRing(52, Solid("#CAD6E2"), 1, 0.82);

            var mainSize = radius > 0 ? Math.Clamp(42 + radius * 28, 42, 132) : 42;
            AddRing(mainSize, Solid("#08A99B"), 4, 0.96);

            var centerDot = new System.Windows.Shapes.Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = Solid("#0B3A52"),
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(centerDot, centerX - 7);
            Canvas.SetTop(centerDot, centerY - 7);
            radiusPreviewCanvas.Children.Add(centerDot);

            radiusPreviewText.Text = radius > 0
                ? $"Previa: ate {radius:N1} km  |  taxa {Money(fee)}" + (minimum > 0 ? $"  |  minimo {Money(minimum)}" : "")
                : "Digite o raio para ver o circulo.";
        }

        async Task RefreshMapAsync()
        {
            try
            {
                await mapView.EnsureCoreWebView2Async();
                var previewRadius = ParseDouble(radiusBox.Text, 0);
                var previewFee = ParseMoney(feeBox.Text, 0);
                var previewMinimum = ParseMoney(minimumBox.Text, 0);
                mapView.NavigateToString(BuildDeliveryZonesMapHtml(
                    currentLatitude,
                    currentLongitude,
                    currentLabel,
                    previewRadius,
                    previewFee,
                    previewMinimum,
                    activeBox.IsChecked == true));
            }
            catch (Exception ex)
            {
                mapStatus.Text = $"Mapa indisponivel: {ex.Message}";
            }
        }

        async Task UseWindowsLocationAsync()
        {
            mapStatus.Text = "Buscando localizacao do Windows...";
            var location = await TryGetCurrentWindowsLocationAsync();
            if (!location.Ok)
            {
                mapStatus.Text = location.Message;
                return;
            }

            currentLatitude = location.Latitude;
            currentLongitude = location.Longitude;
            currentLabel = "Local atual";
            _profile.Latitude = currentLatitude;
            _profile.Longitude = currentLongitude;
            SaveRestaurantProfile();
            mapStatus.Text = location.Message;
            await RefreshMapAsync();
        }

        zonesList.SelectionChanged += (_, _) =>
        {
            if (zonesList.SelectedItem is DeliveryZoneFee zone)
            {
                LoadZone(zone);
            }
        };

        var newButton = DialogButton("Novo", "#0B3A52");
        newButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        newButton.Click += (_, _) =>
        {
            zonesList.SelectedIndex = -1;
            radiusBox.Text = NextRadius().ToString("N1", Brazil);
            feeBox.Text = "0,00";
            minimumBox.Text = "0,00";
            activeBox.IsChecked = true;
            RefreshRadiusPreview();
            radiusBox.Focus();
        };

        var saveButton = DialogButton("Salvar raio", "#08A99B");
        saveButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        async Task SaveRadiusAsync()
        {
            var fee = ParseMoney(feeBox.Text, -1);
            if (fee < 0)
            {
                status.Foreground = RedText;
                status.Text = "Informe uma taxa valida.";
                feeBox.Focus();
                return;
            }

            var radius = ParseDouble(radiusBox.Text, 0);
            if (radius <= 0)
            {
                status.Foreground = RedText;
                status.Text = "Informe um raio em km maior que zero.";
                radiusBox.Focus();
                return;
            }

            var zoneName = $"ATE {radius:N1} KM".ToUpperInvariant();
            var zone = zonesList.SelectedItem as DeliveryZoneFee
                ?? _appSettings.DeliveryZones.FirstOrDefault(item => Math.Abs(item.RadiusKm - radius) < 0.01);
            if (zone is null)
            {
                zone = new DeliveryZoneFee();
                _appSettings.DeliveryZones.Add(zone);
            }

            zone.Zone = zoneName;
            zone.DistrictMatch = "";
            zone.RadiusKm = radius;
            zone.Fee = fee;
            zone.MinimumOrder = Math.Max(0, ParseMoney(minimumBox.Text, 0));
            zone.Active = activeBox.IsChecked == true;
            RefreshZonesList(zone);
            SaveAppSettings();
            SaveStore();
            status.Foreground = GreenText;
            status.Text = $"Salvo: {zone.Display}";
            SetStatus(status.Text);
            await RefreshMapAsync();
        }

        saveButton.Click += async (_, _) => await SaveRadiusAsync();

        var deleteButton = DialogButton("Excluir selecionado", "#A11D1D");
        deleteButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        deleteButton.Click += async (_, _) =>
        {
            if (zonesList.SelectedItem is not DeliveryZoneFee zone)
            {
                status.Foreground = AmberText;
                status.Text = "Selecione um raio salvo para excluir.";
                return;
            }

            _appSettings.DeliveryZones.Remove(zone);
            RefreshZonesList();
            SaveAppSettings();
            SaveStore();
            status.Foreground = GreenText;
            status.Text = "Raio removido.";
            SetStatus(status.Text);
            await RefreshMapAsync();
        };

        void FocusText(TextBox box)
        {
            box.Focus();
            box.SelectAll();
        }

        void OnEnter(UIElement element, Func<Task> action)
        {
            element.PreviewKeyDown += async (_, e) =>
            {
                if ((e.Key != System.Windows.Input.Key.Enter && e.Key != System.Windows.Input.Key.Return) ||
                    System.Windows.Input.Keyboard.Modifiers != System.Windows.Input.ModifierKeys.None)
                {
                    return;
                }

                e.Handled = true;
                await action();
            };
        }

        OnEnter(radiusBox, () =>
        {
            FocusText(feeBox);
            return Task.CompletedTask;
        });
        OnEnter(feeBox, () =>
        {
            FocusText(minimumBox);
            return Task.CompletedTask;
        });
        OnEnter(minimumBox, SaveRadiusAsync);
        OnEnter(activeBox, SaveRadiusAsync);
        OnEnter(saveButton, SaveRadiusAsync);
        OnEnter(newButton, () =>
        {
            newButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            return Task.CompletedTask;
        });

        async void RefreshPreviewFromInput()
        {
            RefreshRadiusPreview();
            if (mapView.CoreWebView2 is not null)
            {
                await RefreshMapAsync();
            }
        }

        radiusBox.TextChanged += (_, _) => RefreshPreviewFromInput();
        feeBox.TextChanged += (_, _) => RefreshPreviewFromInput();
        minimumBox.TextChanged += (_, _) => RefreshPreviewFromInput();
        activeBox.Checked += (_, _) => RefreshPreviewFromInput();
        activeBox.Unchecked += (_, _) => RefreshPreviewFromInput();

        var locateButton = DialogButton("Usar local atual", "#99620D");
        locateButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        locateButton.Click += async (_, _) =>
        {
            locateButton.IsEnabled = false;
            await UseWindowsLocationAsync();
            locateButton.IsEnabled = true;
            SetStatus("Centro do mapa atualizado pela localizacao do Windows.");
        };

        var root = new Grid { Margin = new Thickness(18) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(410) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border Separator()
        {
            return new Border { Height = 1, Background = Solid("#E3EBF2"), Margin = new Thickness(0, 14, 0, 14) };
        }

        TextBlock MutedText(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Solid("#5B6B7A"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, -4, 0, 10)
            };
        }

        var actionGrid = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 4, 0, 0) };
        saveButton.Margin = new Thickness(0, 0, 8, 0);
        newButton.Margin = new Thickness(0, 0, 0, 0);
        actionGrid.Children.Add(saveButton);
        actionGrid.Children.Add(newButton);

        var sideCard = BorderCard();
        sideCard.Padding = new Thickness(18);
        sideCard.Margin = new Thickness(0, 0, 14, 0);
        var side = new StackPanel();
        side.Children.Add(SectionTitle("Taxas de entrega"));
        side.Children.Add(MutedText("Cadastre faixas por distancia. Na venda delivery, o PDV usa a menor faixa ativa que atende o pedido e sugere a taxa."));

        side.Children.Add(SectionTitle("Centro da loja"));
        side.Children.Add(mapStatus);
        side.Children.Add(MutedText("Use a localizacao do Windows uma vez e confira o pino no mapa."));
        side.Children.Add(locateButton);

        side.Children.Add(Separator());

        side.Children.Add(SectionTitle("Cadastrar faixa"));
        side.Children.Add(MutedText("Exemplo: 1 km = R$ 5,00; 3 km = R$ 8,00; 5 km = R$ 12,00."));
        side.Children.Add(DialogField("Raio (km)", radiusBox));
        side.Children.Add(DialogField("Taxa", feeBox));
        side.Children.Add(DialogField("Pedido minimo", minimumBox));
        side.Children.Add(activeBox);
        radiusPreviewCard.Padding = new Thickness(10, 8, 10, 8);
        radiusPreviewCard.Margin = new Thickness(0, 2, 0, 10);
        radiusPreviewCard.Child = radiusPreviewText;
        side.Children.Add(radiusPreviewCard);
        side.Children.Add(actionGrid);
        side.Children.Add(status);

        side.Children.Add(Separator());

        side.Children.Add(SectionTitle("Raios salvos"));
        side.Children.Add(MutedText("Clique em uma faixa para editar. Mantenha os raios em ordem crescente."));
        side.Children.Add(zonesList);
        side.Children.Add(deleteButton);
        sideCard.Child = new ScrollViewer
        {
            Content = side,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = System.Windows.Controls.PanningMode.VerticalOnly
        };
        root.Children.Add(sideCard);

        var mapCard = BorderCard();
        mapCard.Padding = new Thickness(0);
        mapCard.ClipToBounds = true;
        mapCard.Margin = new Thickness(0);
        mapCard.Child = mapView;
        Grid.SetColumn(mapCard, 1);
        root.Children.Add(mapCard);

        dialog.Content = root;
        RefreshZonesList();
        if (_appSettings.DeliveryZones.Count > 0)
        {
            zonesList.SelectedIndex = 0;
        }
        else
        {
            radiusBox.Text = "1,0";
            feeBox.Text = "0,00";
            minimumBox.Text = "0,00";
        }
        RefreshRadiusPreview();

        dialog.Loaded += async (_, _) =>
        {
            await RefreshMapAsync();
            if (!hasSavedLocation)
            {
                await UseWindowsLocationAsync();
            }
        };
        dialog.ShowDialog();
    }

    private async Task<(bool Ok, double Latitude, double Longitude, string Message)> TryGetCurrentWindowsLocationAsync()
    {
        try
        {
            var access = await Geolocator.RequestAccessAsync();
            if (access != GeolocationAccessStatus.Allowed)
            {
                return (false, 0, 0, "Ative a permissao de localizacao do Windows para usar o ponto atual.");
            }

            var locator = new Geolocator
            {
                DesiredAccuracyInMeters = 50
            };
            var position = await locator.GetGeopositionAsync(
                maximumAge: TimeSpan.FromSeconds(10),
                timeout: TimeSpan.FromSeconds(18)).AsTask();
            var point = position.Coordinate.Point.Position;
            if (!IsValidMapCoordinate(point.Latitude, point.Longitude))
            {
                return (false, 0, 0, "O Windows retornou uma coordenada invalida.");
            }

            var accuracy = position.Coordinate.Accuracy;
            return (true, point.Latitude, point.Longitude, $"Centro atual: {point.Latitude:N5}, {point.Longitude:N5}  |  precisao {accuracy:N0}m");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, 0, 0, "Permissao de localizacao negada no Windows.");
        }
        catch (Exception ex) when (ex is TimeoutException or InvalidOperationException or TaskCanceledException)
        {
            return (false, 0, 0, "Nao consegui pegar a localizacao do Windows agora.");
        }
    }

    private static bool IsValidMapCoordinate(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90
               && longitude is >= -180 and <= 180
               && Math.Abs(latitude) > 0.0001
               && Math.Abs(longitude) > 0.0001;
    }

    private string BuildDeliveryZonesMapHtml(
        double latitude,
        double longitude,
        string label,
        double previewRadiusKm = 0,
        decimal previewFee = 0,
        decimal previewMinimum = 0,
        bool previewActive = true)
    {
        var zones = _appSettings.DeliveryZones
            .Where(zone => zone.Active)
            .OrderBy(zone => zone.RadiusKm <= 0 ? double.MaxValue : zone.RadiusKm)
            .Select((zone, index) => new
            {
                zone = string.IsNullOrWhiteSpace(zone.Zone) ? $"ZONA {index + 1}" : zone.Zone,
                fee = Money(zone.Fee),
                minimum = zone.MinimumOrder > 0 ? Money(zone.MinimumOrder) : "",
                radiusKm = zone.RadiusKm > 0 ? zone.RadiusKm : index + 1,
                color = DeliveryZoneColor(index)
            })
            .ToList();
        object? preview = previewRadiusKm > 0
            ? new
            {
                zone = previewActive ? "RAIO EM EDICAO" : "RAIO INATIVO",
                fee = Money(previewFee),
                minimum = previewMinimum > 0 ? Money(previewMinimum) : "",
                radiusKm = previewRadiusKm,
                color = previewActive ? "#08A99B" : "#A11D1D"
            }
            : null;

        var centerJson = JsonSerializer.Serialize(new { lat = latitude, lng = longitude, label });
        var zonesJson = JsonSerializer.Serialize(zones);
        var previewJson = JsonSerializer.Serialize(preview);
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>");
        html.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'>");
        html.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
        html.AppendLine("<style>html,body,#map{height:100%;margin:0}body{font-family:Segoe UI,Arial,sans-serif;background:#eef3f7}.leaflet-container{background:#eef3f7}.legend{position:absolute;left:16px;bottom:16px;z-index:500;background:#fff;border:1px solid #ccd7e2;border-radius:8px;padding:12px 14px;box-shadow:0 10px 26px rgba(24,34,43,.14);max-width:300px}.legend h3{margin:0 0 7px;font-size:13px;color:#18222b}.row{display:flex;gap:8px;align-items:flex-start;margin:7px 0;font-size:12px;color:#465869}.row.preview{border-top:1px solid #e3ebf2;padding-top:8px;color:#18222b}.dot{width:11px;height:11px;border-radius:999px;margin-top:2px;flex:0 0 auto}.empty{font-size:12px;color:#5B6B7A}.leaflet-popup-content{font-size:13px}.center-chip{position:absolute;left:16px;top:16px;z-index:500;background:#fff;border:1px solid #ccd7e2;border-radius:999px;padding:8px 11px;color:#245b91;font-size:12px;font-weight:800;box-shadow:0 8px 22px rgba(24,34,43,.12)}</style>");
        html.AppendLine("</head><body><div id='map'></div><div class='center-chip'>Pino da loja</div><div class='legend'><h3>Mapa de entrega</h3><div id='legendRows'></div></div>");
        html.AppendLine("<script>");
        html.Append("const center=").Append(centerJson).AppendLine(";");
        html.Append("const zones=").Append(zonesJson).AppendLine(";");
        html.Append("const preview=").Append(previewJson).AppendLine(";");
        html.AppendLine("const map=L.map('map',{zoomControl:true,attributionControl:true}).setView([center.lat,center.lng],13);");
        html.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19,attribution:'&copy; OpenStreetMap'}).addTo(map);");
        html.AppendLine("const marker=L.marker([center.lat,center.lng]).addTo(map).bindPopup(center.label || 'Local atual');");
        html.AppendLine("const group=L.featureGroup([marker]);");
        html.AppendLine("const rows=document.getElementById('legendRows');");
        html.AppendLine("const circles=[];");
        html.AppendLine("if(!zones.length && !preview){rows.innerHTML='<div class=\"empty\">Nenhum raio salvo.</div>'}");
        html.AppendLine("zones.forEach(z=>{const circle=L.circle([center.lat,center.lng],{radius:z.radiusKm*1000,color:z.color,fillColor:z.color,fillOpacity:.12,weight:3}).addTo(map).bindPopup(`<b>${z.zone}</b><br>${z.radiusKm.toLocaleString('pt-BR')} km<br>Taxa ${z.fee}${z.minimum?'<br>Min. '+z.minimum:''}`);circles.push(circle);group.addLayer(circle);const row=document.createElement('div');row.className='row';row.innerHTML=`<span class=\"dot\" style=\"background:${z.color}\"></span><span><b>${z.zone}</b><br>${z.radiusKm.toLocaleString('pt-BR')} km | ${z.fee}</span>`;rows.appendChild(row);});");
        html.AppendLine("if(preview){const circle=L.circle([center.lat,center.lng],{radius:preview.radiusKm*1000,color:preview.color,fillColor:preview.color,fillOpacity:.10,weight:3,dashArray:'8 6'}).addTo(map).bindPopup(`<b>${preview.zone}</b><br>${preview.radiusKm.toLocaleString('pt-BR')} km<br>Taxa ${preview.fee}${preview.minimum?'<br>Min. '+preview.minimum:''}`);group.addLayer(circle);const row=document.createElement('div');row.className='row preview';row.innerHTML=`<span class=\"dot\" style=\"background:${preview.color}\"></span><span><b>${preview.zone}</b><br>${preview.radiusKm.toLocaleString('pt-BR')} km | ${preview.fee}</span>`;rows.appendChild(row);}");
        html.AppendLine("if(zones.length || preview){map.fitBounds(group.getBounds().pad(.22));}");
        html.AppendLine("</script></body></html>");
        return html.ToString();
    }

    private static string DeliveryZoneColor(int index)
    {
        var colors = new[] { "#08A99B", "#0B3A52", "#99620D", "#A11D1D", "#6D28D9", "#047857", "#B45309" };
        return colors[index % colors.Length];
    }

    private static double ParseDouble(string value, double fallback)
    {
        return double.TryParse(value, NumberStyles.Any, Brazil, out var parsed)
            || double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : fallback;
    }
}
