using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
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

        var dialog = CreateDialog("Taxas por raio no mapa", 1160, 720);
        var zonesList = new ListBox
        {
            DisplayMemberPath = nameof(DeliveryZoneFee.Display),
            MinHeight = 455,
            ItemsSource = _appSettings.DeliveryZones
        };
        var radiusBox = new TextBox();
        var feeBox = new TextBox();
        var minimumBox = new TextBox();
        var activeBox = new CheckBox { Content = "Ativa", IsChecked = true, Margin = new Thickness(0, 6, 0, 8) };
        var status = new TextBlock { Foreground = GreenText, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        var mapStatus = new TextBlock
        {
            Text = "Local atual: aguardando permissao de localizacao...",
            Foreground = Solid("#667684"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var currentLatitude = IsValidMapCoordinate(_profile.Latitude, _profile.Longitude) ? _profile.Latitude : -23.55052;
        var currentLongitude = IsValidMapCoordinate(_profile.Latitude, _profile.Longitude) ? _profile.Longitude : -46.633308;
        var currentLabel = "Local atual do usuario";
        var mapView = new WebView2
        {
            MinHeight = 510,
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

        void LoadZone(DeliveryZoneFee zone)
        {
            radiusBox.Text = DisplayRadiusFor(zone).ToString("N1", Brazil);
            feeBox.Text = zone.Fee.ToString("N2", Brazil);
            minimumBox.Text = zone.MinimumOrder.ToString("N2", Brazil);
            activeBox.IsChecked = zone.Active;
        }

        async Task RefreshMapAsync(bool requestLiveLocation)
        {
            try
            {
                await mapView.EnsureCoreWebView2Async();
                mapView.NavigateToString(BuildDeliveryZonesMapHtml(currentLatitude, currentLongitude, currentLabel, requestLiveLocation));
            }
            catch (Exception ex)
            {
                mapStatus.Text = $"Mapa indisponivel: {ex.Message}";
            }
        }

        void HandleMapMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var document = JsonDocument.Parse(e.WebMessageAsJson);
                var root = document.RootElement;
                var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "";
                if (type == "location")
                {
                    currentLatitude = root.GetProperty("lat").GetDouble();
                    currentLongitude = root.GetProperty("lng").GetDouble();
                    _profile.Latitude = currentLatitude;
                    _profile.Longitude = currentLongitude;
                    SaveRestaurantProfile();
                    mapStatus.Text = $"Local atual em tempo real: {currentLatitude:N5}, {currentLongitude:N5}";
                    currentLabel = "Local atual do usuario";
                    return;
                }

                if (type == "location-error")
                {
                    mapStatus.Text = "Nao consegui pegar localizacao em tempo real. Libere permissao de localizacao no Windows/navegador.";
                }
            }
            catch
            {
                mapStatus.Text = "Nao consegui ler a localizacao enviada pelo mapa.";
            }
        }

        zonesList.SelectionChanged += (_, _) =>
        {
            if (zonesList.SelectedItem is DeliveryZoneFee zone)
            {
                LoadZone(zone);
            }
        };

        var newButton = DialogButton("Novo circulo", "#2F6FAE");
        newButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        newButton.Click += (_, _) =>
        {
            zonesList.SelectedIndex = -1;
            radiusBox.Text = NextRadius().ToString("N1", Brazil);
            feeBox.Text = "0,00";
            minimumBox.Text = "0,00";
            activeBox.IsChecked = true;
            radiusBox.Focus();
        };

        var saveButton = DialogButton("Salvar taxa", "#0F766E");
        saveButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        saveButton.Click += async (_, _) =>
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
            zonesList.Items.Refresh();
            SaveAppSettings();
            SaveStore();
            status.Foreground = GreenText;
            status.Text = $"Taxa salva: {zone.Display}";
            SetStatus(status.Text);
            await RefreshMapAsync(requestLiveLocation: false);
        };

        var locateButton = DialogButton("Usar local atual", "#99620D");
        locateButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        locateButton.Click += async (_, _) =>
        {
            mapStatus.Text = "Pedindo localizacao atual...";
            await RefreshMapAsync(requestLiveLocation: true);
            SetStatus("Solicitei a localizacao atual para redesenhar os circulos.");
        };

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        left.Children.Add(new TextBlock
        {
            Text = "Regras cadastradas",
            Foreground = Solid("#18222B"),
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8)
        });
        left.Children.Add(zonesList);
        left.Children.Add(newButton);
        grid.Children.Add(left);

        var form = DialogPanel();
        form.Margin = new Thickness(0, 0, 12, 0);
        form.Children.Add(DialogHint("Cadastre apenas circulos por km. O centro e a localizacao atual do usuario; cada circulo tem seu proprio valor de entrega."));
        form.Children.Add(DialogField("Raio do circulo (km)", radiusBox));
        form.Children.Add(DialogField("Taxa de entrega", feeBox));
        form.Children.Add(DialogField("Pedido minimo opcional", minimumBox));
        form.Children.Add(activeBox);
        form.Children.Add(saveButton);
        form.Children.Add(locateButton);
        form.Children.Add(status);
        Grid.SetColumn(form, 1);
        grid.Children.Add(form);

        var mapCard = BorderCard();
        mapCard.Padding = new Thickness(12);
        var mapPanel = new Grid();
        mapPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mapPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var mapHeader = new StackPanel();
        mapHeader.Children.Add(new TextBlock
        {
            Text = "Mapa de circulos por km",
            Foreground = Solid("#18222B"),
            FontWeight = FontWeights.Bold,
            FontSize = 16
        });
        mapHeader.Children.Add(mapStatus);
        mapPanel.Children.Add(mapHeader);
        Grid.SetRow(mapView, 1);
        mapPanel.Children.Add(mapView);
        mapCard.Child = mapPanel;
        Grid.SetColumn(mapCard, 2);
        grid.Children.Add(mapCard);

        dialog.Content = grid;
        if (_appSettings.DeliveryZones.Count > 0)
        {
            zonesList.SelectedIndex = 0;
        }
        else
        {
            radiusBox.Text = "1,0";
        }

        dialog.Loaded += async (_, _) =>
        {
            await mapView.EnsureCoreWebView2Async();
            mapView.CoreWebView2.PermissionRequested += HandleMapPermission;
            mapView.CoreWebView2.WebMessageReceived += HandleMapMessage;
            await RefreshMapAsync(requestLiveLocation: true);
        };
        dialog.Closed += (_, _) =>
        {
            if (mapView.CoreWebView2 is not null)
            {
                mapView.CoreWebView2.PermissionRequested -= HandleMapPermission;
                mapView.CoreWebView2.WebMessageReceived -= HandleMapMessage;
            }
        };
        dialog.ShowDialog();

        void HandleMapPermission(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            if (e.PermissionKind == CoreWebView2PermissionKind.Geolocation)
            {
                e.State = CoreWebView2PermissionState.Allow;
                e.Handled = true;
            }
        }
    }

    private static bool IsValidMapCoordinate(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90
               && longitude is >= -180 and <= 180
               && Math.Abs(latitude) > 0.0001
               && Math.Abs(longitude) > 0.0001;
    }

    private string BuildDeliveryZonesMapHtml(double latitude, double longitude, string label, bool requestLiveLocation)
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

        var centerJson = JsonSerializer.Serialize(new { lat = latitude, lng = longitude, label });
        var zonesJson = JsonSerializer.Serialize(zones);
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>");
        html.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'>");
        html.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
        html.AppendLine("<style>html,body,#map{height:100%;margin:0}body{font-family:Arial,sans-serif}.legend{position:absolute;right:12px;top:12px;z-index:500;background:#fff;border:1px solid #ccd7e2;border-radius:8px;padding:10px 12px;box-shadow:0 8px 24px rgba(24,34,43,.14);max-width:230px}.legend h3{margin:0 0 8px;font-size:14px;color:#18222b}.row{display:flex;gap:7px;align-items:flex-start;margin:6px 0;font-size:12px;color:#465869}.dot{width:11px;height:11px;border-radius:999px;margin-top:2px;flex:0 0 auto}.empty{font-size:12px;color:#667684}.leaflet-popup-content{font-size:13px}</style>");
        html.AppendLine("</head><body><div id='map'></div><div class='legend'><h3>Circulos de entrega</h3><div id='legendRows'></div></div>");
        html.AppendLine("<script>");
        html.Append("const center=").Append(centerJson).AppendLine(";");
        html.Append("const zones=").Append(zonesJson).AppendLine(";");
        html.Append("const requestLiveLocation=").Append(requestLiveLocation ? "true" : "false").AppendLine(";");
        html.AppendLine("const map=L.map('map',{zoomControl:true}).setView([center.lat,center.lng],12);");
        html.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19,attribution:'&copy; OpenStreetMap'}).addTo(map);");
        html.AppendLine("const marker=L.marker([center.lat,center.lng]).addTo(map).bindPopup(center.label || 'Local atual');");
        html.AppendLine("const group=L.featureGroup([marker]);");
        html.AppendLine("const rows=document.getElementById('legendRows');");
        html.AppendLine("const circles=[];");
        html.AppendLine("if(!zones.length){rows.innerHTML='<div class=\"empty\">Cadastre um circulo para desenhar o primeiro raio.</div>'}");
        html.AppendLine("zones.forEach(z=>{const circle=L.circle([center.lat,center.lng],{radius:z.radiusKm*1000,color:z.color,fillColor:z.color,fillOpacity:.12,weight:3}).addTo(map).bindPopup(`<b>${z.zone}</b><br>${z.radiusKm.toLocaleString('pt-BR')} km<br>Taxa ${z.fee}${z.minimum?'<br>Min. '+z.minimum:''}`);circles.push(circle);group.addLayer(circle);const row=document.createElement('div');row.className='row';row.innerHTML=`<span class=\"dot\" style=\"background:${z.color}\"></span><span><b>${z.zone}</b><br>${z.radiusKm.toLocaleString('pt-BR')} km | ${z.fee}</span>`;rows.appendChild(row);});");
        html.AppendLine("if(zones.length){map.fitBounds(group.getBounds().pad(.12));}");
        html.AppendLine("function updateCenter(lat,lng){const point=[lat,lng];marker.setLatLng(point);circles.forEach(c=>c.setLatLng(point));map.setView(point,map.getZoom()<13?13:map.getZoom());}");
        html.AppendLine("function post(message){if(window.chrome&&window.chrome.webview){window.chrome.webview.postMessage(message);}}");
        html.AppendLine("if(requestLiveLocation&&navigator.geolocation){navigator.geolocation.watchPosition(pos=>{const lat=pos.coords.latitude;const lng=pos.coords.longitude;updateCenter(lat,lng);post({type:'location',lat,lng,accuracy:pos.coords.accuracy||0});},err=>post({type:'location-error',message:err.message||'sem permissao'}),{enableHighAccuracy:true,maximumAge:2000,timeout:12000});}");
        html.AppendLine("if(requestLiveLocation&&!navigator.geolocation){post({type:'location-error',message:'geolocation indisponivel'});}");
        html.AppendLine("</script></body></html>");
        return html.ToString();
    }

    private static string DeliveryZoneColor(int index)
    {
        var colors = new[] { "#0F766E", "#245B91", "#99620D", "#A11D1D", "#6D28D9", "#047857", "#B45309" };
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
