using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private CashClosingSnapshot? ShowProfessionalCashClosingDialog()
    {
        var totals = GetTodayPaymentTotals();
        var expectedCash = _cashTotal;
        CashClosingSnapshot? snapshot = null;

        var dialog = CreateDialog("Fechamento profissional de caixa", 620, 620);
        dialog.ResizeMode = ResizeMode.NoResize;
        var countedBox = new TextBox { Text = expectedCash.ToString("N2", Brazil) };
        var notesBox = new TextBox { Height = 82, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        var differenceText = new TextBlock { FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap };
        var error = new TextBlock { Foreground = RedText, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };

        void RefreshDifference()
        {
            var counted = ParseMoney(countedBox.Text, expectedCash);
            var difference = counted - expectedCash;
            differenceText.Text = $"Diferenca: {Money(difference)}";
            differenceText.Foreground = difference == 0 ? GreenText : RedText;
        }

        var confirm = DialogButton("Confirmar fechamento", "#08A99B");
        confirm.HorizontalAlignment = HorizontalAlignment.Stretch;
        confirm.Click += (_, _) =>
        {
            var counted = ParseMoney(countedBox.Text, -1);
            if (counted < 0)
            {
                error.Text = "Informe o dinheiro contado.";
                countedBox.Focus();
                return;
            }

            var difference = counted - expectedCash;
            var notes = notesBox.Text.Trim();
            if (Math.Abs(difference) >= 0.01m && string.IsNullOrWhiteSpace(notes))
            {
                error.Text = "Explique a diferenca antes de fechar o caixa.";
                notesBox.Focus();
                return;
            }

            snapshot = new CashClosingSnapshot
            {
                ExpectedCash = expectedCash,
                CountedCash = counted,
                Difference = difference,
                PixTotal = totals.Pix,
                CreditTotal = totals.Credit,
                DebitTotal = totals.Debit,
                OtherTotal = totals.Other,
                Operator = _currentUser,
                Notes = notes,
                When = DateTime.Now
            };
            dialog.Close();
        };

        countedBox.TextChanged += (_, _) => RefreshDifference();

        var panel = DialogPanel();
        panel.Children.Add(CreateMetricCard("Dinheiro esperado", Money(expectedCash), "saldo vivo registrado no caixa", "#0B3A52"));
        panel.Children.Add(CreateMetricCard("Pix", Money(totals.Pix), "pagamentos Pix finalizados hoje", "#08A99B"));
        panel.Children.Add(CreateMetricCard("Credito", Money(totals.Credit), "cartoes de credito finalizados hoje", "#99620D"));
        panel.Children.Add(CreateMetricCard("Debito", Money(totals.Debit), "cartoes de debito finalizados hoje", "#99620D"));
        panel.Children.Add(DialogField("Dinheiro contado na gaveta", countedBox));
        panel.Children.Add(differenceText);
        panel.Children.Add(DialogField("Justificativa de diferenca / observacao", notesBox));
        panel.Children.Add(error);
        panel.Children.Add(confirm);
        dialog.Content = panel;
        RefreshDifference();
        dialog.Loaded += (_, _) =>
        {
            countedBox.Focus();
            countedBox.SelectAll();
        };
        dialog.ShowDialog();
        return snapshot;
    }

    private (decimal Pix, decimal Credit, decimal Debit, decimal Other) GetTodayPaymentTotals()
    {
        var today = DateTime.Today;
        var payments = Tables
            .Concat(DeliveryTiles)
            .SelectMany(board => board.ClosedPayments.Concat(board.Payments))
            .Where(payment => payment.When.Date == today)
            .ToList();

        decimal SumBy(params string[] tokens)
        {
            return payments
                .Where(payment => tokens.Any(token => payment.Method.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .Sum(payment => payment.Amount);
        }

        var pix = SumBy("PIX");
        var credit = SumBy("CREDITO", "CREDITO", "CARTAO CREDITO", "CRÉDITO");
        var debit = SumBy("DEBITO", "DÉBITO", "CARTAO DEBITO");
        var known = pix + credit + debit;
        var other = Math.Max(0, payments.Sum(payment => payment.Amount) - known);
        return (pix, credit, debit, other);
    }
}
