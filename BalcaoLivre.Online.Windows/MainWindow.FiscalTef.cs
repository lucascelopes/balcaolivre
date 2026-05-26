using System.Windows;
using System.Windows.Controls;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private void ShowFiscalTefDialog()
    {
        if (!RequirePermission(CanManageFiscal, "Modulo Fiscal/TEF"))
        {
            return;
        }

        var settings = _appSettings.FiscalTef ??= new FiscalTefSettings();
        var dialog = CreateDialog("Modulo Fiscal/TEF", 660, 560);
        dialog.ResizeMode = ResizeMode.NoResize;

        var enabledBox = new CheckBox { Content = "Ativar modulo fiscal/TEF separado", IsChecked = settings.Enabled };
        var fiscalProviderBox = new ComboBox
        {
            ItemsSource = new[] { "NAO CONFIGURADO", "NFC-E", "SAT", "MFE", "OUTRO" },
            IsEditable = true,
            Text = settings.FiscalProvider,
            MinHeight = 34
        };
        var tefProviderBox = new ComboBox
        {
            ItemsSource = new[] { "NAO CONFIGURADO", "STONE", "CIELO", "REDE", "PAGSEGURO", "TEF DISCADO", "OUTRO" },
            IsEditable = true,
            Text = settings.TefProvider,
            MinHeight = 34
        };
        var merchantCodeBox = new TextBox { Text = settings.MerchantCode };
        var cscIdBox = new TextBox { Text = settings.CscId };
        var environmentBox = new ComboBox
        {
            ItemsSource = new[] { "HOMOLOGACAO", "PRODUCAO" },
            SelectedItem = string.IsNullOrWhiteSpace(settings.Environment) ? "HOMOLOGACAO" : settings.Environment,
            MinHeight = 34
        };
        var requireFiscalBox = new CheckBox
        {
            Content = "Exigir fiscal antes de imprimir comprovante de venda",
            IsChecked = settings.RequireFiscalBeforeReceipt
        };
        var statusText = new TextBlock
        {
            Foreground = GreenText,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };

        var saveButton = DialogButton("Salvar modulo fiscal/TEF", "#0F766E");
        saveButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        saveButton.Width = double.NaN;
        saveButton.Click += (_, _) =>
        {
            settings.Enabled = enabledBox.IsChecked == true;
            settings.FiscalProvider = (fiscalProviderBox.Text ?? "").Trim().ToUpperInvariant();
            settings.TefProvider = (tefProviderBox.Text ?? "").Trim().ToUpperInvariant();
            settings.MerchantCode = merchantCodeBox.Text.Trim();
            settings.CscId = cscIdBox.Text.Trim();
            settings.Environment = environmentBox.SelectedItem?.ToString() ?? "HOMOLOGACAO";
            settings.RequireFiscalBeforeReceipt = requireFiscalBox.IsChecked == true;

            SaveAppSettings();
            SaveStore();
            statusText.Text = "Modulo fiscal/TEF salvo.";
            SetStatus(settings.Enabled
                ? "Modulo Fiscal/TEF ativado como integracao separada."
                : "Modulo Fiscal/TEF salvo, mas ainda desativado.");
        };

        var panel = DialogPanel();
        panel.Children.Add(enabledBox);
        panel.Children.Add(DialogHint("Este modulo guarda configuracao e permissao separadas para NFC-e, SAT e maquininha. A emissao real entra por provedor fiscal/TEF dedicado."));
        panel.Children.Add(DialogField("Fiscal", fiscalProviderBox));
        panel.Children.Add(DialogField("TEF / maquininha", tefProviderBox));
        panel.Children.Add(DialogField("Codigo do estabelecimento / afiliacao", merchantCodeBox));
        panel.Children.Add(DialogField("CSC/Token fiscal ou referencia tecnica", cscIdBox));
        panel.Children.Add(DialogField("Ambiente", environmentBox));
        panel.Children.Add(requireFiscalBox);
        panel.Children.Add(saveButton);
        panel.Children.Add(statusText);
        dialog.Content = panel;
        dialog.ShowDialog();
    }
}
