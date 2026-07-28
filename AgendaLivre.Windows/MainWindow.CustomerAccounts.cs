using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private bool HasCustomerReceivableForAppointment(string appointmentId) =>
        !string.IsNullOrWhiteSpace(appointmentId) &&
        _data.CustomerReceivables.Any(item =>
            item.AppointmentId.Equals(appointmentId, StringComparison.OrdinalIgnoreCase) &&
            item.Status is not "cancelled");

    private CustomerReceivable? OpenCustomerReceivableForAppointment(string appointmentId) =>
        _data.CustomerReceivables.FirstOrDefault(item =>
            item.AppointmentId.Equals(appointmentId, StringComparison.OrdinalIgnoreCase) &&
            item.Status == "open" &&
            item.RemainingValue > 0);

    private IReadOnlyList<CustomerReceivable> OpenCustomerReceivables(Customer customer) =>
        _data.CustomerReceivables
            .Where(item =>
                item.Status == "open" &&
                item.RemainingValue > 0 &&
                ((!string.IsNullOrWhiteSpace(customer.Id) &&
                  item.CustomerId.Equals(customer.Id, StringComparison.OrdinalIgnoreCase)) ||
                 (string.IsNullOrWhiteSpace(item.CustomerId) &&
                  item.CustomerName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(item => item.OpenedAt)
            .ToList();

    private void AppendCustomerAccountSummary(StackPanel body, Customer customer)
    {
        var openItems = OpenCustomerReceivables(customer);
        var balance = openItems.Sum(item => item.RemainingValue);

        body.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 5, 0, 10)
        });

        var heading = new Grid();
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        heading.Children.Add(new TextBlock
        {
            Text = "Conta do cliente",
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var balanceText = new TextBlock
        {
            Text = balance.ToString("C", Brazil),
            Foreground = balance > 0 ? AccentTextBrush : MutedBrush,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(balanceText, 1);
        heading.Children.Add(balanceText);

        var accountBody = new StackPanel();
        accountBody.Children.Add(heading);
        accountBody.Children.Add(new TextBlock
        {
            Text = openItems.Count == 0
                ? "Sem saldo em aberto."
                : openItems.Count == 1
                    ? "1 atendimento aguardando pagamento."
                    : $"{openItems.Count} atendimentos aguardando pagamento.",
            Foreground = MutedBrush,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });

        if (openItems.Count > 0)
        {
            var receiveButton = new Button
            {
                Style = (Style)FindResource("GhostButton"),
                Content = "Receber saldo",
                Foreground = AccentTextBrush,
                BorderBrush = AccentBrush,
                BorderThickness = new Thickness(1),
                Height = 36,
                MinWidth = 118,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 0),
                Cursor = Cursors.Hand
            };
            AutomationProperties.SetName(receiveButton, $"Receber saldo de {customer.Name}");
            receiveButton.Click += async (_, _) =>
            {
                CloseCustomerInfoPopup();
                await ReceiveCustomerAccountAsync(customer);
            };
            accountBody.Children.Add(receiveButton);
        }

        body.Children.Add(new Border
        {
            Background = balance > 0 ? AccentSoftBrush : GraySoftBrush,
            BorderBrush = balance > 0 ? AccentBrush : LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 5),
            Child = accountBody
        });
    }

    private async Task ExecuteAppointmentChargeAsync(
        Appointment appointment,
        AppointmentChargeKind kind,
        Button actionButton)
    {
        if (IsPreviewAppointment(appointment))
        {
            ShowStatus("O agendamento de exemplo não pode receber pagamentos.");
            return;
        }

        var current = _data.Appointments.FirstOrDefault(item => item.Id == appointment.Id);
        if (current is null)
        {
            CloseAppointmentInfoPopup();
            RefreshAll();
            ShowStatus("O atendimento não está mais disponível.");
            return;
        }

        if (current.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Blocked)
        {
            ShowStatus("Esse atendimento foi encerrado sem cobrança.");
            return;
        }

        if (current.PaymentConfirmedAt is not null || OpenCustomerReceivableForAppointment(current.Id) is not null)
        {
            ShowStatus("Esse atendimento já possui um pagamento ou saldo registrado.");
            return;
        }

        actionButton.IsEnabled = false;
        CloseAppointmentInfoPopup();

        try
        {
            if (kind == AppointmentChargeKind.CustomerAccount)
            {
                AddAppointmentToCustomerAccount(current);
                return;
            }

            if (current.Price <= 0)
            {
                ConfirmAppointmentPayment(current, "Sem cobrança", paymentStatus: "not_required");
                return;
            }

            if (kind == AppointmentChargeKind.Cash)
            {
                ConfirmAppointmentPayment(current, "Dinheiro");
                return;
            }

            if (kind == AppointmentChargeKind.PixKey)
            {
                if (ShowPixKeyPaymentConfirmationDialog(current))
                {
                    ConfirmAppointmentPayment(current, "Pix por chave", paymentProvider: "Chave Pix");
                }
                return;
            }

            AgendaMercadoPagoPaymentOutcome? outcome;
            string paymentMethod;
            if (kind == AppointmentChargeKind.PixMercadoPago)
            {
                paymentMethod = "Pix";
                outcome = await ProcessMercadoPagoPixPaymentAsync(current, this);
            }
            else
            {
                var pointMethod = kind == AppointmentChargeKind.Debit
                    ? MercadoPagoDebitMethod
                    : MercadoPagoCreditMethod;
                paymentMethod = kind == AppointmentChargeKind.Debit
                    ? "Débito na Point"
                    : "Crédito na Point";
                outcome = await ProcessMercadoPagoPointPaymentAsync(
                    pointMethod,
                    current.Price,
                    current.CustomerName,
                    $"{current.ServiceName} | {current.Start:dd/MM HH:mm}",
                    this);
            }

            if (outcome is null)
            {
                return;
            }

            ConfirmAppointmentPayment(
                current,
                paymentMethod,
                "Mercado Pago",
                outcome.Reference,
                outcome.Status);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Não foi possível concluir a cobrança.\n\n{ex.Message}",
                "Receber pagamento",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private bool ShowPixKeyPaymentConfirmationDialog(Appointment appointment) =>
        ShowPixKeyPaymentConfirmationDialog(appointment.Price);

    private bool ShowPixKeyPaymentConfirmationDialog(decimal amount)
    {
        var key = _data.Settings.PixKey.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            ShowStatus("Cadastre uma chave Pix nas configurações de pagamento.");
            return false;
        }

        var result = MessageBox.Show(
            this,
            $"Chave Pix do estabelecimento:\n\n{key}\n\nValor: {amount.ToString("C", Brazil)}\n\nConfirme somente depois que o valor aparecer na conta.",
            "Receber por Pix",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information,
            MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }

    private void AddAppointmentToCustomerAccount(Appointment appointment)
    {
        var customer = ResolveAppointmentCustomer(appointment);
        if (customer is null)
        {
            MessageBox.Show(
                this,
                "Cadastre ou selecione um cliente com nome e telefone únicos antes de adicionar o valor à conta.",
                "Conta do cliente",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var existing = _data.CustomerReceivables.FirstOrDefault(item =>
            item.AppointmentId.Equals(appointment.Id, StringComparison.OrdinalIgnoreCase) &&
            item.Status is not "cancelled");
        if (existing is not null)
        {
            ShowStatus("Esse atendimento já está vinculado à conta do cliente.");
            return;
        }

        var now = DateTime.Now;
        var receivable = new CustomerReceivable
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            AppointmentId = appointment.Id,
            Description = FirstFilled(appointment.ServiceName, "Atendimento"),
            OriginalValue = Math.Max(0, appointment.Price),
            RemainingValue = Math.Max(0, appointment.Price),
            Status = "open",
            OpenedAt = now,
            UpdatedAt = now,
            PaymentProvider = "customer_account",
            PaymentStatus = "pending"
        };

        appointment.CustomerId = customer.Id;
        appointment.Status = AppointmentStatus.Done;
        appointment.PaymentConfirmedAt = null;
        appointment.PaymentMethod = "Conta do cliente";
        appointment.PaymentProvider = "customer_account";
        appointment.PaymentReference = receivable.Id;
        appointment.PaymentStatus = "pending";
        appointment.UpdatedAt = now;
        _data.CustomerReceivables.Add(receivable);
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"{appointment.Price.ToString("C", Brazil)} adicionado à conta de {customer.Name}. O valor continua a receber.");
    }

    private Customer? ResolveAppointmentCustomer(Appointment appointment)
    {
        if (!string.IsNullOrWhiteSpace(appointment.CustomerId))
        {
            var linked = _data.Customers.FirstOrDefault(item =>
                item.Id.Equals(appointment.CustomerId, StringComparison.OrdinalIgnoreCase));
            if (linked is not null)
            {
                return linked;
            }
        }

        var normalizedPhone = NormalizeBrazilPhone(appointment.CustomerPhone);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var phoneMatches = _data.Customers
                .Where(item => NormalizeBrazilPhone(item.Phone).Equals(normalizedPhone, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (phoneMatches.Count == 1)
            {
                return phoneMatches[0];
            }
        }

        var nameMatches = _data.Customers
            .Where(item =>
                !string.IsNullOrWhiteSpace(appointment.CustomerName) &&
                item.Name.Equals(appointment.CustomerName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return nameMatches.Count == 1 ? nameMatches[0] : null;
    }

    private string ResolveCustomerIdForAppointment(string currentId, string customerName, string customerPhone)
    {
        if (!string.IsNullOrWhiteSpace(currentId) &&
            _data.Customers.Any(item => item.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase)))
        {
            return currentId;
        }

        var normalizedPhone = NormalizeBrazilPhone(customerPhone);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var phoneMatches = _data.Customers
                .Where(item => NormalizeBrazilPhone(item.Phone).Equals(normalizedPhone, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (phoneMatches.Count == 1)
            {
                return phoneMatches[0].Id;
            }
        }

        var nameMatches = _data.Customers
            .Where(item =>
                !string.IsNullOrWhiteSpace(customerName) &&
                item.Name.Equals(customerName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return nameMatches.Count == 1 ? nameMatches[0].Id : "";
    }

    private async Task ReceiveCustomerAccountAsync(Customer customer)
    {
        var openItems = OpenCustomerReceivables(customer);
        if (openItems.Count == 0)
        {
            ShowStatus($"{customer.Name} não possui saldo em aberto.");
            return;
        }

        var total = openItems.Sum(item => item.RemainingValue);
        var method = SelectCustomerAccountPaymentMethod(customer, total);
        if (string.IsNullOrWhiteSpace(method))
        {
            return;
        }

        AgendaMercadoPagoPaymentOutcome? outcome = null;
        var provider = "Manual";
        var reference = $"manual_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var status = "approved";

        try
        {
            switch (method)
            {
                case "Pix":
                    if (_data.Settings.MercadoPagoEnabled && _data.Settings.MercadoPagoConnected)
                    {
                        provider = "Mercado Pago";
                        outcome = await ProcessMercadoPagoPixPaymentAsync(
                            total,
                            customer.Name,
                            $"Conta do cliente | {customer.Name}",
                            this);
                    }
                    else if (ShowPixKeyPaymentConfirmationDialog(total))
                    {
                        provider = "Chave Pix";
                        outcome = new AgendaMercadoPagoPaymentOutcome(
                            $"pix_key_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                            "approved");
                    }
                    break;
                case "Débito na Point":
                    provider = "Mercado Pago";
                    outcome = await ProcessMercadoPagoPointPaymentAsync(
                        MercadoPagoDebitMethod,
                        total,
                        customer.Name,
                        $"Conta do cliente | {customer.Name}",
                        this);
                    break;
                case "Crédito na Point":
                    provider = "Mercado Pago";
                    outcome = await ProcessMercadoPagoPointPaymentAsync(
                        MercadoPagoCreditMethod,
                        total,
                        customer.Name,
                        $"Conta do cliente | {customer.Name}",
                        this);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Não foi possível receber a conta.\n\n{ex.Message}",
                "Conta do cliente",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (method is not "Dinheiro")
        {
            if (outcome is null)
            {
                return;
            }

            reference = outcome.Reference;
            status = outcome.Status;
        }

        MarkCustomerReceivablesPaid(openItems, method, provider, reference, status);
    }

    private string? SelectCustomerAccountPaymentMethod(Customer customer, decimal total)
    {
        var shell = CreateFinanceEditorDialog(
            "Receber conta",
            $"Quite o saldo em aberto de {customer.Name}.",
            "Continuar",
            PackIconKind.WalletOutline);
        shell.Dialog.Width = 620;
        AddFinanceDialogSection(
            shell.Body,
            PackIconKind.AccountCashOutline,
            total.ToString("C", Brazil),
            "O saldo só será baixado após a confirmação do pagamento.");

        var methodBox = AddFinanceDialogComboField(
            shell.Body,
            "Forma de pagamento",
            new[] { "Pix", "Dinheiro", "Débito na Point", "Crédito na Point" },
            "Pix",
            editable: false);

        string? result = null;
        shell.PrimaryButton.Click += (_, _) =>
        {
            result = methodBox.SelectedItem as string ?? "Pix";
            shell.Dialog.DialogResult = true;
        };
        methodBox.Focus();
        return ShowAppDialog(shell.Dialog) == true ? result : null;
    }

    private void MarkCustomerReceivablesPaid(
        IReadOnlyCollection<CustomerReceivable> requestedItems,
        string paymentMethod,
        string paymentProvider,
        string paymentReference,
        string paymentStatus)
    {
        var requestedIds = requestedItems.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentItems = _data.CustomerReceivables
            .Where(item => requestedIds.Contains(item.Id) && item.Status == "open" && item.RemainingValue > 0)
            .ToList();
        if (currentItems.Count == 0)
        {
            RefreshAll();
            ShowStatus("Esse saldo já foi quitado ou alterado.");
            return;
        }

        var now = DateTime.Now;
        foreach (var item in currentItems)
        {
            item.RemainingValue = 0;
            item.Status = "paid";
            item.PaidAt = now;
            item.UpdatedAt = now;
            item.PaymentMethod = paymentMethod;
            item.PaymentProvider = paymentProvider;
            item.PaymentReference = paymentReference;
            item.PaymentStatus = paymentStatus;

            var appointment = _data.Appointments.FirstOrDefault(candidate =>
                candidate.Id.Equals(item.AppointmentId, StringComparison.OrdinalIgnoreCase));
            if (appointment is null)
            {
                continue;
            }

            appointment.PaymentConfirmedAt = now;
            appointment.PaymentMethod = paymentMethod;
            appointment.PaymentProvider = paymentProvider;
            appointment.PaymentReference = paymentReference;
            appointment.PaymentStatus = paymentStatus;
            appointment.UpdatedAt = now;
        }

        var received = currentItems.Sum(item => item.OriginalValue);
        _store.Save(_data);
        RefreshAll();
        ShowStatus($"Conta recebida: {received.ToString("C", Brazil)} em {paymentMethod}.");
    }
}
