using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CheckBox = System.Windows.Controls.CheckBox;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private DeliveryZoneFee? ResolveDeliveryZoneFee(string district)
    {
        var clean = NormalizeLookup(district);
        if (string.IsNullOrWhiteSpace(clean))
        {
            return _appSettings.DeliveryZones
                .Where(zone => zone.Active && string.IsNullOrWhiteSpace(zone.DistrictMatch))
                .OrderBy(zone => zone.Fee)
                .FirstOrDefault();
        }

        return _appSettings.DeliveryZones
            .Where(zone => zone.Active)
            .Select(zone => new
            {
                Zone = zone,
                Match = NormalizeLookup(string.IsNullOrWhiteSpace(zone.DistrictMatch) ? zone.Zone : zone.DistrictMatch)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Match)
                && (clean.Contains(item.Match, StringComparison.OrdinalIgnoreCase)
                    || item.Match.Contains(clean, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item => item.Match.Length)
            .Select(item => item.Zone)
            .FirstOrDefault();
    }

    private void ApplyDeliveryZoneFee(string district, System.Windows.Controls.TextBox feeBox, TextBlock zoneText)
    {
        var zone = ResolveDeliveryZoneFee(district);
        if (zone is null)
        {
            zoneText.Text = "Sem regra para este bairro. Use a taxa manual.";
            zoneText.Foreground = AmberText;
            return;
        }

        feeBox.Text = zone.Fee.ToString("N2", Brazil);
        zoneText.Text = $"Zona aplicada: {zone.Zone} ({Money(zone.Fee)})";
        zoneText.Foreground = GreenText;
    }

    private void ShowDeliveryZonesDialog()
    {
        if (!RequirePermission(CanManageDeliveryZones, "Taxas por bairro/zona"))
        {
            return;
        }

        var dialog = CreateDialog("Taxas por bairro/zona", 760, 560);
        var zonesList = new ListBox
        {
            DisplayMemberPath = nameof(DeliveryZoneFee.Display),
            Width = 300,
            ItemsSource = _appSettings.DeliveryZones
        };
        var zoneBox = new TextBox();
        var matchBox = new TextBox();
        var feeBox = new TextBox();
        var minimumBox = new TextBox();
        var activeBox = new CheckBox { Content = "Ativa", IsChecked = true, Margin = new Thickness(0, 6, 0, 8) };
        var status = new TextBlock { Foreground = GreenText, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };

        void LoadZone(DeliveryZoneFee zone)
        {
            zoneBox.Text = zone.Zone;
            matchBox.Text = zone.DistrictMatch;
            feeBox.Text = zone.Fee.ToString("N2", Brazil);
            minimumBox.Text = zone.MinimumOrder.ToString("N2", Brazil);
            activeBox.IsChecked = zone.Active;
        }

        zonesList.SelectionChanged += (_, _) =>
        {
            if (zonesList.SelectedItem is DeliveryZoneFee zone)
            {
                LoadZone(zone);
            }
        };

        var newButton = DialogButton("Nova zona", "#2F6FAE");
        newButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        newButton.Click += (_, _) =>
        {
            zonesList.SelectedIndex = -1;
            zoneBox.Text = "";
            matchBox.Text = "";
            feeBox.Text = "0,00";
            minimumBox.Text = "0,00";
            activeBox.IsChecked = true;
            zoneBox.Focus();
        };

        var saveButton = DialogButton("Salvar taxa", "#0F766E");
        saveButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        saveButton.Click += (_, _) =>
        {
            var zoneName = zoneBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(zoneName))
            {
                status.Foreground = RedText;
                status.Text = "Informe o nome da zona ou bairro.";
                zoneBox.Focus();
                return;
            }

            var fee = ParseMoney(feeBox.Text, -1);
            if (fee < 0)
            {
                status.Foreground = RedText;
                status.Text = "Informe uma taxa valida.";
                feeBox.Focus();
                return;
            }

            var zone = zonesList.SelectedItem as DeliveryZoneFee
                ?? _appSettings.DeliveryZones.FirstOrDefault(item => string.Equals(item.Zone, zoneName, StringComparison.OrdinalIgnoreCase));
            if (zone is null)
            {
                zone = new DeliveryZoneFee();
                _appSettings.DeliveryZones.Add(zone);
            }

            zone.Zone = zoneName;
            zone.DistrictMatch = matchBox.Text.Trim().ToUpperInvariant();
            zone.Fee = fee;
            zone.MinimumOrder = Math.Max(0, ParseMoney(minimumBox.Text, 0));
            zone.Active = activeBox.IsChecked == true;
            zonesList.Items.Refresh();
            SaveAppSettings();
            SaveStore();
            status.Foreground = GreenText;
            status.Text = $"Taxa salva: {zone.Display}";
            SetStatus(status.Text);
        };

        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel();
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
        form.Children.Add(DialogHint("Use o campo de correspondencia para nomes como CENTRO, JARDIM ou ZONA NORTE. O PDV aplica a regra mais especifica que aparecer no bairro/referencia."));
        form.Children.Add(DialogField("Zona/bairro", zoneBox));
        form.Children.Add(DialogField("Corresponder quando bairro contem", matchBox));
        form.Children.Add(DialogField("Taxa de entrega", feeBox));
        form.Children.Add(DialogField("Pedido minimo opcional", minimumBox));
        form.Children.Add(activeBox);
        form.Children.Add(saveButton);
        form.Children.Add(status);
        Grid.SetColumn(form, 1);
        grid.Children.Add(form);

        dialog.Content = grid;
        if (_appSettings.DeliveryZones.Count > 0)
        {
            zonesList.SelectedIndex = 0;
        }
        dialog.ShowDialog();
    }
}
