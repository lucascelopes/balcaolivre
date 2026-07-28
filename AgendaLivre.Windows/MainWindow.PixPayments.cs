using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    /// <summary>
    /// Creates a Mercado Pago Pix charge for an appointment and waits for the
    /// provider confirmation. This method never mutates the appointment.
    /// </summary>
    private Task<AgendaMercadoPagoPaymentOutcome?> ProcessMercadoPagoPixPaymentAsync(
        Appointment appointment,
        Window owner)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        ArgumentNullException.ThrowIfNull(owner);

        var service = FirstFilled(appointment.ServiceName, "Atendimento");
        var description = ClipMercadoPagoDescription(
            $"{BusinessDisplayName()} | {service} | {appointment.Start:dd/MM HH:mm}");

        return ProcessMercadoPagoPixPaymentAsync(
            appointment.Price,
            FirstFilled(appointment.CustomerName, "Cliente"),
            description,
            owner);
    }

    private Task<AgendaMercadoPagoPixChargeResult> CreateMercadoPagoPixChargeAsync(
        decimal amount,
        string payer,
        string description)
    {
        var cleanDescription = ClipMercadoPagoDescription(description);
        var payload = FillMercadoPagoPayload(new AgendaMercadoPagoPixChargePayload
        {
            EventName = "agendalivre.mercadopago.web.charge",
            Amount = amount.ToString("0.00", CultureInfo.InvariantCulture),
            Method = "PIX",
            LocalReference = BuildMercadoPagoLocalReference(),
            Description = cleanDescription,
            PayerName = FirstFilled(payer, "Cliente"),
            Items =
            [
                new AgendaMercadoPagoItemPayload
                {
                    Code = "AGENDALIVRE",
                    Title = "Atendimento Agenda Livre",
                    Quantity = 1,
                    UnitPrice = amount.ToString("0.00", CultureInfo.InvariantCulture),
                    Description = cleanDescription
                }
            ]
        });

        return PostMercadoPagoOperationAsync<AgendaMercadoPagoPixChargeResult>(
            "/mercadopago/web/charge",
            payload,
            TimeSpan.FromSeconds(18));
    }

    private Task<AgendaMercadoPagoPointStatusResult> FetchMercadoPagoPixStatusAsync(
        string attemptId,
        string orderId,
        string localReference)
    {
        var payload = FillMercadoPagoPayload(new AgendaMercadoPagoPointStatusPayload
        {
            EventName = "agendalivre.mercadopago.web.status",
            AttemptId = attemptId,
            OrderId = orderId,
            LocalReference = localReference
        });

        return PostMercadoPagoOperationAsync<AgendaMercadoPagoPointStatusResult>(
            "/mercadopago/web/status",
            payload,
            TimeSpan.FromSeconds(10));
    }

    private async Task<AgendaMercadoPagoPaymentOutcome?> ProcessMercadoPagoPixPaymentAsync(
        decimal amount,
        string payer,
        string description,
        Window owner)
    {
        if (!_data.Settings.MercadoPagoEnabled)
        {
            MessageBox.Show(
                owner,
                "Ative o Mercado Pago em Configurações antes de gerar um Pix.",
                "Pix Mercado Pago",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        if (!_data.Settings.MercadoPagoConnected)
        {
            MessageBox.Show(
                owner,
                "Conecte a conta Mercado Pago em Configurações antes de gerar um Pix.",
                "Pix Mercado Pago",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        if (amount <= 0)
        {
            MessageBox.Show(
                owner,
                "O valor do atendimento precisa ser maior que zero para gerar o Pix.",
                "Pix Mercado Pago",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        var charge = await CreateMercadoPagoPixChargeAsync(amount, payer, description);
        if (!charge.Ok)
        {
            MessageBox.Show(
                owner,
                FirstFilled(charge.Message, "Mercado Pago recusou a criação do Pix."),
                "Pix Mercado Pago",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        var copyAndPaste = FirstFilled(charge.QrCode, charge.PaymentUrl, charge.TicketUrl);
        if (string.IsNullOrWhiteSpace(copyAndPaste))
        {
            MessageBox.Show(
                owner,
                "O Mercado Pago criou a cobrança, mas não devolveu o QR nem o código Pix para exibir.",
                "Pix Mercado Pago",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        var qrSource = TryCreateMercadoPagoPixBitmap(charge.QrCodeBase64);
        var cancelledLocally = false;
        var completed = false;
        var lastStatus = FirstFilled(charge.Status, "pending");
        using var waitCancellation = new CancellationTokenSource();

        var shell = CreateFinanceEditorDialog(
            "Pix Mercado Pago",
            "Mostre o QR ao cliente ou copie o código Pix. O recebimento só será confirmado após a aprovação.",
            "Copiar código Pix",
            PackIconKind.Qrcode,
            useBodyCard: false);
        var waitDialog = shell.Dialog;
        waitDialog.Owner = owner;
        waitDialog.Width = 620;
        waitDialog.MaxHeight = 780;
        shell.CancelButton.Content = "Parar espera";
        shell.CancelButton.MinWidth = 128;
        AutomationProperties.SetName(shell.CancelButton, "Parar espera do Pix");

        AddFinanceDialogSection(
            shell.Body,
            PackIconKind.Qrcode,
            "Escaneie para pagar",
            "O QR e o copia-e-cola representam a mesma cobrança Mercado Pago.");

        var amountText = new TextBlock
        {
            Text = amount.ToString("C", Brazil),
            Foreground = InkBrush,
            FontSize = 30,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        var payerText = new TextBlock
        {
            Text = FirstFilled(payer, "Cliente"),
            Foreground = MutedBrush,
            FontSize = 12.5,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 14)
        };
        shell.Body.Children.Add(amountText);
        shell.Body.Children.Add(payerText);

        if (qrSource is not null)
        {
            shell.Body.Children.Add(new Border
            {
                Width = 318,
                Height = 318,
                Background = Brushes.White,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(AppSurfaceRadiusValue),
                Padding = new Thickness(12),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16),
                Child = new Image
                {
                    Source = qrSource,
                    Stretch = Stretch.Uniform,
                    SnapsToDevicePixels = true
                }
            });
        }
        else
        {
            shell.Body.Children.Add(new Border
            {
                Background = WarmSoftBrush,
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(AppActionRadiusValue),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 16),
                Child = new TextBlock
                {
                    Text = "O Mercado Pago não enviou a imagem do QR. Use o código Pix copia-e-cola abaixo.",
                    Foreground = InkBrush,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                }
            });
        }

        shell.Body.Children.Add(new TextBlock
        {
            Text = "Pix copia-e-cola",
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        var codeBox = new TextBox
        {
            Text = copyAndPaste,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 78,
            Padding = new Thickness(11, 9, 11, 9),
            Foreground = InkBrush,
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 12)
        };
        AutomationProperties.SetName(codeBox, "Código Pix copia-e-cola");
        shell.Body.Children.Add(codeBox);

        var statusText = new TextBlock
        {
            Text = BuildMercadoPagoPixWaitingText(amount, charge.ExpiresAt),
            Foreground = AccentTextBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        AutomationProperties.SetName(statusText, "Status do pagamento Pix");
        AutomationProperties.SetLiveSetting(statusText, AutomationLiveSetting.Polite);
        shell.Body.Children.Add(statusText);

        shell.Body.Children.Add(new TextBlock
        {
            Text = "Parar a espera fecha somente esta tela; o Pix já criado continua válido até expirar.",
            Foreground = MutedBrush,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var paymentUrl = FirstFilled(charge.PaymentUrl, charge.TicketUrl);
        if (!string.IsNullOrWhiteSpace(paymentUrl))
        {
            var openButton = new Button
            {
                Content = "Abrir página do Pix",
                Style = (Style)FindResource("GhostButton"),
                HorizontalAlignment = HorizontalAlignment.Left,
                MinWidth = 150,
                Height = 38,
                Margin = new Thickness(0, 0, 0, 2)
            };
            AutomationProperties.SetName(openButton, "Abrir página do Pix no navegador");
            openButton.Click += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(paymentUrl) { UseShellExecute = true });
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    statusText.Text = "Não foi possível abrir a página. Use o QR ou o código copia-e-cola.";
                }
            };
            shell.Body.Children.Add(openButton);
        }

        void CopyPixCode()
        {
            try
            {
                Clipboard.SetText(copyAndPaste);
                statusText.Text = "Código Pix copia-e-cola copiado. Aguardando a confirmação do pagamento...";
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
            {
                statusText.Text = "Não foi possível copiar agora. Selecione o código no campo acima.";
                codeBox.Focus();
                codeBox.SelectAll();
            }
        }

        void CancelLocalWait()
        {
            if (completed || cancelledLocally)
            {
                return;
            }

            cancelledLocally = true;
            waitCancellation.Cancel();
            if (waitDialog.IsVisible)
            {
                waitDialog.Close();
            }
        }

        shell.PrimaryButton.Click += (_, _) => CopyPixCode();
        shell.CancelButton.Click += (_, _) => CancelLocalWait();
        waitDialog.Closed += (_, _) =>
        {
            if (!completed && !cancelledLocally)
            {
                cancelledLocally = true;
                waitCancellation.Cancel();
            }

            owner.IsEnabled = true;
            owner.Activate();
        };

        var ownerWasEnabled = owner.IsEnabled;
        owner.IsEnabled = false;
        try
        {
            waitDialog.Show();
        }
        catch
        {
            owner.IsEnabled = ownerWasEnabled;
            throw;
        }

        for (var attempt = 0; attempt < 72 && !cancelledLocally; attempt++)
        {
            try
            {
                await Task.Delay(attempt == 0 ? 1500 : 2500, waitCancellation.Token);
            }
            catch (OperationCanceledException) when (waitCancellation.IsCancellationRequested)
            {
                break;
            }

            if (cancelledLocally)
            {
                break;
            }

            var status = await FetchMercadoPagoPixStatusAsync(
                charge.AttemptId,
                charge.OrderId,
                charge.LocalReference);
            if (cancelledLocally)
            {
                break;
            }

            if (!status.Ok)
            {
                statusText.Text = FirstFilled(
                    status.Message,
                    $"Aguardando retorno. Último status: {lastStatus}");
                continue;
            }

            lastStatus = FirstFilled(status.Status, lastStatus);
            statusText.Text = $"Status: {MercadoPagoPixStatusLabel(lastStatus)} · verificação {attempt + 1}/72";
            if (status.Paid)
            {
                completed = true;
                waitCancellation.Cancel();
                if (waitDialog.IsVisible)
                {
                    waitDialog.Close();
                }

                return new AgendaMercadoPagoPaymentOutcome(
                    FirstFilled(
                        status.PaymentId,
                        charge.PaymentId,
                        charge.OrderId,
                        charge.LocalReference),
                    FirstFilled(status.Status, "approved"));
            }

            if (IsMercadoPagoFinalFailure(lastStatus) ||
                lastStatus.Contains("refund", StringComparison.OrdinalIgnoreCase) ||
                lastStatus.Contains("estorn", StringComparison.OrdinalIgnoreCase))
            {
                completed = true;
                waitCancellation.Cancel();
                if (waitDialog.IsVisible)
                {
                    waitDialog.Close();
                }

                MessageBox.Show(
                    owner,
                    $"Pix não aprovado: {MercadoPagoPixStatusLabel(lastStatus)}.",
                    "Pix Mercado Pago",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }
        }

        if (!cancelledLocally)
        {
            completed = true;
            if (waitDialog.IsVisible)
            {
                waitDialog.Close();
            }

            MessageBox.Show(
                owner,
                "Ainda não houve confirmação do Mercado Pago. A cobrança continua válida até expirar; confira a conta antes de gerar outra.",
                "Pix Mercado Pago",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        return null;
    }

    private static BitmapSource? TryCreateMercadoPagoPixBitmap(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        try
        {
            var clean = base64.Trim();
            var marker = clean.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (marker >= 0)
            {
                clean = clean[(marker + "base64,".Length)..];
            }

            var bytes = Convert.FromBase64String(clean);
            using var stream = new MemoryStream(bytes, writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException or IOException)
        {
            return null;
        }
    }

    private static string BuildMercadoPagoPixWaitingText(decimal amount, string expiresAt)
    {
        var expiration = "";
        if (DateTimeOffset.TryParse(
                expiresAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsedExpiration))
        {
            expiration = $" · expira às {parsedExpiration.ToLocalTime():HH:mm}";
        }

        return $"Aguardando Pix de {amount.ToString("C", Brazil)}{expiration}...";
    }

    private static string MercadoPagoPixStatusLabel(string status)
    {
        var clean = (status ?? "").Trim().ToLowerInvariant();
        return clean switch
        {
            "approved" or "paid" or "processed" => "aprovado",
            "pending" or "created" or "in_process" or "action_required" => "aguardando pagamento",
            "rejected" or "refused" => "recusado",
            "cancelled" or "canceled" => "cancelado",
            "expired" => "expirado",
            "refunded" => "estornado",
            _ => string.IsNullOrWhiteSpace(clean) ? "aguardando pagamento" : clean
        };
    }

    private sealed class AgendaMercadoPagoPixChargePayload : AgendaMercadoPagoClientPayload
    {
        public string Amount { get; set; } = "";
        public string Method { get; set; } = "PIX";
        public string LocalReference { get; set; } = "";
        public string Description { get; set; } = "";
        public string PayerName { get; set; } = "";
        public string PayerEmail { get; set; } = "";
        public List<AgendaMercadoPagoItemPayload> Items { get; set; } = [];
    }

    private sealed class AgendaMercadoPagoPixChargeResult : AgendaMercadoPagoResult
    {
        public string AttemptId { get; set; } = "";
        public string LocalReference { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string PaymentId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
        public string QrCode { get; set; } = "";
        public string QrCodeBase64 { get; set; } = "";
        public string TicketUrl { get; set; } = "";
        public string PaymentUrl { get; set; } = "";
        public string ExpiresAt { get; set; } = "";
    }
}
