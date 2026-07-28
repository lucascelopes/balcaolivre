using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const double AppointmentPaymentPopupWidth = 510;
    private const double AppointmentPaymentPopupHeight = 440;
    private const double AppointmentPaymentPopupLeftWidth = 166;
    private const double AppointmentPaymentWheelWidth = 308;
    private const double AppointmentPaymentWheelRowHeight = 30;

    private enum AppointmentChargeKind
    {
        PixKey,
        PixMercadoPago,
        Cash,
        Debit,
        Credit,
        CustomerAccount
    }

    private sealed record AppointmentChargeOption(
        AppointmentChargeKind Kind,
        string Label,
        string? SupportingText,
        string ActionText);

    private Border CreateAppointmentPaymentPopupContent(Appointment appointment)
    {
        var pixKind = _data.Settings.MercadoPagoEnabled && _data.Settings.MercadoPagoConnected
            ? AppointmentChargeKind.PixMercadoPago
            : AppointmentChargeKind.PixKey;
        var options = new List<AppointmentChargeOption>
        {
            new(pixKind, "Pix", null, pixKind == AppointmentChargeKind.PixMercadoPago ? "Gerar Pix" : "Mostrar chave Pix"),
            new(AppointmentChargeKind.Debit, "Débito", null, "Enviar débito para a maquininha"),
            new(AppointmentChargeKind.Credit, "Crédito", null, "Enviar crédito para a maquininha"),
            new(AppointmentChargeKind.CustomerAccount, "Conta do cliente", "Pagar depois", "Adicionar à conta do cliente")
        };

        var selectedIndex = options.FindIndex(item => item.Kind == AppointmentChargeKind.Debit);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        var isAnimating = false;
        var queuedStep = 0;
        var isExecuting = false;

        var card = new Border
        {
            Width = AppointmentPaymentPopupWidth,
            Height = AppointmentPaymentPopupHeight,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13),
            ClipToBounds = true,
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(28, 22, 18),
                BlurRadius = 24,
                ShadowDepth = 5,
                Opacity = 0.13
            }
        };
        KeyboardNavigation.SetTabNavigation(card, KeyboardNavigationMode.Cycle);

        var layout = new Grid
        {
            Width = AppointmentPaymentPopupWidth,
            Height = AppointmentPaymentPopupHeight,
            Background = PanelBrush
        };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(AppointmentPaymentPopupLeftWidth) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftPanel = CreateAppointmentPaymentLeftPanel(appointment);
        layout.Children.Add(leftPanel);

        var rightPanel = new Grid
        {
            Margin = new Thickness(18, 16, 18, 16)
        };
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(196) });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(104) });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
        Grid.SetColumn(rightPanel, 1);
        layout.Children.Add(rightPanel);

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };

        var editButton = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Height = 22,
            MinWidth = 0,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            Content = new TextBlock
            {
                Text = "Editar atendimento",
                Foreground = AccentTextBrush,
                FontSize = 11.5,
                TextDecorations = TextDecorations.Underline
            }
        };
        AutomationProperties.SetName(editButton, "Editar atendimento");
        editButton.Click += (_, _) => OpenAppointmentEditorFromInfoPopup(appointment);
        headerActions.Children.Add(editButton);

        var closeButton = IconOnlyButton(PackIconKind.Close, 24);
        closeButton.Background = Brushes.Transparent;
        closeButton.ToolTip = "Fechar cobrança";
        AutomationProperties.SetName(closeButton, "Fechar cobrança do atendimento");
        closeButton.Click += (_, _) => CloseAppointmentInfoPopup();
        headerActions.Children.Add(closeButton);

        rightPanel.Children.Add(headerActions);

        var details = new StackPanel();
        details.Children.Add(CreateAppointmentPaymentDetailRow(
            "01",
            "Serviço",
            FirstFilled(appointment.ServiceName, "Atendimento"),
            44));
        details.Children.Add(CreateAppointmentPaymentDetailRow(
            "02",
            "Cliente",
            FirstFilled(appointment.CustomerName, "Cliente"),
            44));
        details.Children.Add(CreateAppointmentPaymentDetailRow(
            "03",
            "Local",
            FirstFilled(appointment.ResourceName, "Não informado"),
            44));
        details.Children.Add(CreateAppointmentPaymentDetailRow(
            "04",
            "A receber",
            appointment.Price.ToString("C", Brazil),
            64,
            emphasizeValue: true));
        Grid.SetRow(details, 1);
        rightPanel.Children.Add(details);

        var paymentTitle = new TextBlock
        {
            Text = "Como cobrar?",
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(paymentTitle, 2);
        rightPanel.Children.Add(paymentTitle);

        var wheelTransform = new TranslateTransform();
        var wheelStack = new StackPanel
        {
            Width = AppointmentPaymentWheelWidth,
            Margin = new Thickness(0, -AppointmentPaymentWheelRowHeight, 0, 0),
            RenderTransform = wheelTransform
        };
        var wheelViewport = new Grid
        {
            Height = 104,
            ClipToBounds = true,
            Focusable = true,
            Cursor = Cursors.Hand,
            OpacityMask = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop(Colors.Transparent, 0),
                    new GradientStop(Colors.Black, 0.10),
                    new GradientStop(Colors.Black, 0.88),
                    new GradientStop(Colors.Transparent, 1)
                }
            }
        };
        wheelViewport.Children.Add(wheelStack);
        AutomationProperties.SetName(wheelViewport, "Forma de cobrança");
        AutomationProperties.SetHelpText(
            wheelViewport,
            "Role para cima ou para baixo, clique em uma opção ou use as setas do teclado.");
        Grid.SetRow(wheelViewport, 3);
        rightPanel.Children.Add(wheelViewport);

        var positionText = new TextBlock
        {
            Foreground = MutedBrush,
            FontSize = 9.5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetLiveSetting(positionText, AutomationLiveSetting.Polite);
        Grid.SetRow(positionText, 4);
        rightPanel.Children.Add(positionText);

        var actionButton = new Button
        {
            Style = (Style)FindResource("CommandButton"),
            Height = 40,
            MinWidth = 0,
            Background = AccentBrush,
            BorderBrush = AccentBrush,
            Foreground = Brushes.White,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRow(actionButton, 5);
        rightPanel.Children.Add(actionButton);

        static int WrapIndex(int value, int count)
        {
            var result = value % count;
            return result < 0 ? result + count : result;
        }

        void UpdateSelectionText()
        {
            var selected = options[selectedIndex];
            positionText.Text = $"{selectedIndex + 1} de {options.Count}  •  Role para cima ou para baixo";
            actionButton.Content = selected.ActionText;
            AutomationProperties.SetName(actionButton, selected.ActionText);
            AutomationProperties.SetName(
                wheelViewport,
                $"Forma de cobrança: {selected.Label}. {selectedIndex + 1} de {options.Count}.");
        }

        void RebuildWheel()
        {
            wheelStack.Children.Clear();
            for (var offset = -2; offset <= 3; offset++)
            {
                var option = options[WrapIndex(selectedIndex + offset, options.Count)];
                var isSelected = offset == 0;
                var distance = Math.Abs(offset);
                var labelStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                labelStack.Children.Add(new TextBlock
                {
                    Text = option.Label,
                    Foreground = isSelected ? InkBrush : MutedBrush,
                    FontSize = isSelected ? 16 : distance >= 2 ? 10.5 : 12,
                    FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                });
                if (!string.IsNullOrWhiteSpace(option.SupportingText))
                {
                    labelStack.Children.Add(new TextBlock
                    {
                        Text = option.SupportingText,
                        Foreground = MutedBrush,
                        FontSize = 8.5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, -1, 0, 0)
                    });
                }

                var selectionChrome = new Border
                {
                    Width = AppointmentPaymentWheelWidth,
                    Height = AppointmentPaymentWheelRowHeight,
                    Background = Brushes.Transparent,
                    BorderBrush = isSelected ? AccentBrush : LineBrush,
                    BorderThickness = isSelected
                        ? new Thickness(0, 0, 0, 2)
                        : new Thickness(0, 0, 0, 1),
                    CornerRadius = new CornerRadius(0),
                    Child = labelStack
                };

                var rowOffset = offset;
                var rowButton = new Button
                {
                    Style = (Style)FindResource("GhostButton"),
                    Width = AppointmentPaymentWheelWidth,
                    Height = AppointmentPaymentWheelRowHeight,
                    MinWidth = 0,
                    Padding = new Thickness(0),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Focusable = false,
                    Cursor = isSelected ? Cursors.Arrow : Cursors.Hand,
                    Opacity = isSelected
                        ? 1
                        : distance == 1
                            ? 0.68
                            : distance == 2
                                ? 0.40
                                : 0.24,
                    Content = selectionChrome
                };
                AutomationProperties.SetName(
                    rowButton,
                    isSelected ? $"{option.Label}, selecionado" : $"Selecionar {option.Label}");
                rowButton.Click += (_, _) =>
                {
                    if (rowOffset != 0)
                    {
                        AnimateWheel(rowOffset > 0 ? 1 : -1);
                    }
                };
                wheelStack.Children.Add(rowButton);
            }
        }

        void AnimateWheel(int step)
        {
            step = Math.Sign(step);
            if (step == 0)
            {
                return;
            }

            if (isAnimating)
            {
                queuedStep = step;
                return;
            }

            isAnimating = true;
            wheelTransform.BeginAnimation(TranslateTransform.YProperty, null);
            wheelTransform.Y = 0;
            var animation = new DoubleAnimation
            {
                From = 0,
                To = step > 0 ? -AppointmentPaymentWheelRowHeight : AppointmentPaymentWheelRowHeight,
                Duration = TimeSpan.FromMilliseconds(190),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };
            animation.Completed += (_, _) =>
            {
                wheelTransform.BeginAnimation(TranslateTransform.YProperty, null);
                wheelTransform.Y = 0;
                selectedIndex = WrapIndex(selectedIndex + step, options.Count);
                RebuildWheel();
                UpdateSelectionText();
                isAnimating = false;

                if (queuedStep == 0 || AppointmentPaymentOverlay.Visibility != Visibility.Visible)
                {
                    queuedStep = 0;
                    return;
                }

                var nextStep = queuedStep;
                queuedStep = 0;
                Dispatcher.BeginInvoke(
                    () => AnimateWheel(nextStep),
                    System.Windows.Threading.DispatcherPriority.Input);
            };
            wheelTransform.BeginAnimation(
                TranslateTransform.YProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        wheelViewport.PreviewMouseWheel += (_, args) =>
        {
            if (args.Delta != 0)
            {
                AnimateWheel(args.Delta > 0 ? -1 : 1);
            }

            args.Handled = true;
        };
        wheelViewport.PreviewKeyDown += (_, args) =>
        {
            switch (args.Key)
            {
                case Key.Up:
                case Key.Left:
                    AnimateWheel(-1);
                    args.Handled = true;
                    break;
                case Key.Down:
                case Key.Right:
                    AnimateWheel(1);
                    args.Handled = true;
                    break;
                case Key.Home:
                    if (selectedIndex != 0)
                    {
                        selectedIndex = 0;
                        RebuildWheel();
                        UpdateSelectionText();
                    }
                    args.Handled = true;
                    break;
                case Key.End:
                    if (selectedIndex != options.Count - 1)
                    {
                        selectedIndex = options.Count - 1;
                        RebuildWheel();
                        UpdateSelectionText();
                    }
                    args.Handled = true;
                    break;
                case Key.Enter:
                case Key.Space:
                    actionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    args.Handled = true;
                    break;
            }
        };

        actionButton.Click += async (_, _) =>
        {
            if (isExecuting || isAnimating)
            {
                return;
            }

            isExecuting = true;
            actionButton.IsEnabled = false;
            try
            {
                await ExecuteAppointmentChargeAsync(
                    appointment,
                    options[selectedIndex].Kind,
                    actionButton);
            }
            finally
            {
                isExecuting = false;
                if (AppointmentPaymentOverlay.Visibility == Visibility.Visible)
                {
                    actionButton.IsEnabled = true;
                    UpdateSelectionText();
                }
            }
        };

        card.PreviewKeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape)
            {
                return;
            }

            CloseAppointmentInfoPopup();
            args.Handled = true;
        };
        card.Loaded += (_, _) =>
        {
            RebuildWheel();
            UpdateSelectionText();
            Keyboard.Focus(wheelViewport);
        };

        RebuildWheel();
        UpdateSelectionText();
        card.Child = layout;
        return card;
    }

    private Grid CreateAppointmentPaymentLeftPanel(Appointment appointment)
    {
        var leftPanel = new Grid
        {
            Width = AppointmentPaymentPopupLeftWidth,
            Height = AppointmentPaymentPopupHeight,
            ClipToBounds = true
        };

        var warmShape = new Image
        {
            Width = AppointmentPaymentPopupLeftWidth,
            Height = AppointmentPaymentPopupHeight,
            Source = new BitmapImage(new Uri(
                "pack://application:,,,/Assets/appointment-payment-left-panel.png",
                UriKind.Absolute)),
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        leftPanel.Children.Add(warmShape);

        var summary = new StackPanel
        {
            Width = 132,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(22, 0, 0, 48)
        };
        summary.Children.Add(new TextBlock
        {
            Text = FirstFilled(appointment.CustomerName, "Cliente"),
            Foreground = InkBrush,
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        summary.Children.Add(CreateAppointmentPaymentAccentRule(new Thickness(0, 12, 0, 16)));
        summary.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:HH:mm} — {appointment.End:HH:mm}",
            Foreground = InkBrush,
            FontSize = 17.5,
            FontWeight = FontWeights.Bold
        });
        summary.Children.Add(new TextBlock
        {
            Text = $"{appointment.DurationMinutes} min",
            Foreground = InkBrush,
            FontSize = 13.5,
            Margin = new Thickness(0, 5, 0, 0)
        });
        summary.Children.Add(CreateAppointmentPaymentAccentRule(new Thickness(0, 16, 0, 16)));
        summary.Children.Add(new TextBlock
        {
            Text = ScheduleStatusLabel(appointment.Status),
            Foreground = ScheduleAccentFor(appointment.Status),
            FontSize = 12.5,
            FontWeight = FontWeights.Medium
        });
        leftPanel.Children.Add(summary);
        return leftPanel;
    }

    private static Border CreateAppointmentPaymentAccentRule(Thickness margin) => new()
    {
        Width = 20,
        Height = 1,
        Background = AccentBrush,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = margin
    };

    private Border CreateAppointmentPaymentDetailRow(
        string number,
        string label,
        string value,
        double height,
        bool emphasizeValue = false)
    {
        var row = new Grid { Height = height };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        row.Children.Add(new TextBlock
        {
            Text = number,
            Foreground = AccentTextBrush,
            FontSize = 11,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = emphasizeValue ? VerticalAlignment.Top : VerticalAlignment.Center,
            Margin = emphasizeValue ? new Thickness(0, 13, 0, 0) : new Thickness(0)
        });

        var divider = new Border
        {
            Width = 1,
            Height = emphasizeValue ? 42 : 28,
            Background = LineBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(divider, 1);
        row.Children.Add(divider);

        if (emphasizeValue)
        {
            var valueStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            valueStack.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = MutedBrush,
                FontSize = 10.5,
                Margin = new Thickness(0, 0, 0, 1)
            });
            valueStack.Children.Add(new TextBlock
            {
                Text = value,
                Foreground = InkBrush,
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(valueStack, 3);
            Grid.SetColumnSpan(valueStack, 2);
            row.Children.Add(valueStack);
        }
        else
        {
            var labelText = new TextBlock
            {
                Text = label,
                Foreground = MutedBrush,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 3);
            row.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value,
                Foreground = InkBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valueText, 4);
            row.Children.Add(valueText);
        }

        row.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            VerticalAlignment = VerticalAlignment.Bottom
        });
        Grid.SetColumnSpan(row.Children[^1], 5);
        return new Border
        {
            Height = height,
            Child = row
        };
    }
}
