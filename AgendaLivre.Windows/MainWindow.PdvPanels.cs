using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private enum PdvPanelKind
    {
        Details,
        Edit,
        Timer,
        Products,
        Receive
    }

    private PdvPanelKind _pdvPanelKind = PdvPanelKind.Details;
    private TextBox? _pdvEditCustomerBox;
    private ComboBox? _pdvEditProfessionalBox;
    private DatePicker? _pdvEditDatePicker;
    private TextBox? _pdvEditTimeBox;
    private ComboBox? _pdvEditDurationBox;
    private TextBox? _pdvEditNotesBox;
    private TextBlock? _pdvTimerPanelElapsedText;
    private TextBlock? _pdvTimerPanelStatusText;
    private TextBlock? _pdvTimerPanelNowText;
    private ProgressBar? _pdvTimerPanelProgress;
    private ComboBox? _pdvServicePicker;
    private ComboBox? _pdvProductPicker;
    private string _pdvReceiveMethod = "Cartão";
    private string _pdvCardType = "Crédito";
    private bool _pdvSendToMachine = true;
    private bool _pdvPaymentRunning;

    private void ShowPdvPanel(PdvPanelKind kind)
    {
        if (!TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        _pdvPanelKind = kind;
        if (kind == PdvPanelKind.Receive && !IsMercadoPagoPointReady())
        {
            _pdvSendToMachine = false;
        }

        PdvInspectorCard.Visibility = Visibility.Visible;
        PdvPanelHost.Content = BuildPdvPanel(kind, appointment);
        UpdatePdvRailSelection();
    }

    private void RefreshPdvPanelForSelection()
    {
        if (!_isPdvMode || _selectedAppointment is null || !IsPdvAppointmentVisible(_selectedAppointment))
        {
            PdvPanelHost.Content = null;
            return;
        }

        if (PdvInspectorCard.Visibility == Visibility.Visible)
        {
            PdvPanelHost.Content = BuildPdvPanel(_pdvPanelKind, _selectedAppointment);
        }

        UpdatePdvRailSelection();
    }

    private UIElement BuildPdvPanel(PdvPanelKind kind, Appointment appointment) => kind switch
    {
        PdvPanelKind.Edit => CreatePdvEditPanel(appointment),
        PdvPanelKind.Timer => CreatePdvTimerPanel(appointment),
        PdvPanelKind.Products => CreatePdvProductsPanel(appointment),
        PdvPanelKind.Receive => CreatePdvReceivePanel(appointment),
        _ => CreatePdvDetailsPanel(appointment)
    };

    private ScrollViewer CreatePdvPanelShell(
        string title,
        PackIconKind iconKind,
        Appointment appointment,
        out StackPanel panel)
    {
        panel = new StackPanel { Margin = new Thickness(1) };

        var titleRow = new Grid();
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleRow.Children.Add(new TextBlock
        {
            Text = title.ToUpper(Brazil),
            Foreground = Solid("#D34C12"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var iconBox = new Border
        {
            Width = 34,
            Height = 34,
            Background = AccentBrush,
            CornerRadius = new CornerRadius(9),
            Margin = new Thickness(8, 0, 7, 0),
            Child = new PackIcon
            {
                Kind = iconKind,
                Width = 18,
                Height = 18,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(iconBox, 1);
        titleRow.Children.Add(iconBox);
        var close = CreatePdvPanelIconButton(PackIconKind.Close, "Fechar painel");
        close.Click += PdvCloseInspectorButton_Click;
        Grid.SetColumn(close, 2);
        titleRow.Children.Add(close);
        panel.Children.Add(titleRow);

        var identity = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        identity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        identity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        identity.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            Background = Solid("#FFE4D5"),
            CornerRadius = new CornerRadius(18),
            Child = new TextBlock
            {
                Text = InitialsFor(appointment.CustomerName),
                Foreground = InkBrush,
            FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        var identityText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        identityText.Children.Add(new TextBlock
        {
            Text = appointment.CustomerName,
            Foreground = InkBrush,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        identityText.Children.Add(new TextBlock
        {
            Text = appointment.ServiceName,
            Foreground = InkBrush,
            FontSize = 10.8,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        identityText.Children.Add(new TextBlock
        {
            Text = appointment.ProfessionalName,
            Foreground = MutedBrush,
            FontSize = 9.5,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(identityText, 1);
        identity.Children.Add(identityText);
        panel.Children.Add(identity);
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = LineBrush,
            Margin = new Thickness(0, 6, 0, 6)
        });

        return new ScrollViewer
        {
            Background = Brushes.White,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 448,
            Content = panel
        };
    }

    private Button CreatePdvPanelIconButton(PackIconKind iconKind, string tooltip)
    {
        var button = new Button
        {
            Width = 34,
            Height = 34,
            MinWidth = 0,
            MinHeight = 0,
            Padding = new Thickness(0),
            Style = (Style)FindResource("GhostButton"),
            ToolTip = tooltip,
            Content = new PackIcon { Kind = iconKind, Width = 17, Height = 17 }
        };
        AutomationProperties.SetName(button, tooltip);
        return button;
    }

    private Border CreatePdvInfoRow(PackIconKind icon, string label, string value, bool accent = false)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(new PackIcon
        {
            Kind = icon,
            Width = 15,
            Height = 15,
            Foreground = InkBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        var labelText = new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 10.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(labelText, 1);
        grid.Children.Add(labelText);
        var valueText = new TextBlock
        {
            Text = value,
            Foreground = accent ? Solid("#D34C12") : InkBrush,
            FontSize = accent ? 12.2 : 10.8,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(valueText);
        return new Border
        {
            Background = Solid("#F7F4F1"),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(9, 7, 9, 7),
            Margin = new Thickness(0, 0, 0, 5),
            Child = grid
        };
    }

    private Button CreatePdvActionButton(string text, bool primary, RoutedEventHandler handler)
    {
        var button = new Button
        {
            Content = text,
            Height = primary ? 38 : 34,
            Style = (Style)FindResource(primary ? "CommandButton" : "GhostButton"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontSize = 11.2,
            FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal
        };
        button.Click += handler;
        return button;
    }

    private UIElement CreatePdvDetailsPanel(Appointment appointment)
    {
        EnsurePdvServiceLines(appointment);
        appointment.ProductLines ??= [];
        var shell = CreatePdvPanelShell("Detalhes", PackIconKind.AccountDetails, appointment, out var panel);

        panel.Children.Add(CreatePdvSectionLabel("RESUMO DO ATENDIMENTO"));
        panel.Children.Add(CreatePdvInfoRow(PackIconKind.CalendarMonth, "Data", appointment.Start.ToString("dd/MM/yyyy", Brazil)));
        panel.Children.Add(CreatePdvInfoRow(PackIconKind.ClockOutline, "Horário", $"{appointment.Start:HH:mm} às {appointment.End:HH:mm}"));
        panel.Children.Add(CreatePdvInfoRow(PackIconKind.TimerOutline, "Duração prevista", $"{appointment.DurationMinutes} min"));
        panel.Children.Add(CreatePdvInfoRow(PackIconKind.AccountTieOutline, "Profissional", appointment.ProfessionalName));
        panel.Children.Add(CreatePdvInfoRow(PackIconKind.InformationOutline, "Status", StatusLabel(appointment.Status)));

        var contractedServiceCount = appointment.ServiceLines.Where(item => item.Quantity > 0).Sum(item => item.Quantity);
        panel.Children.Add(CreatePdvSectionLabel($"SERVIÇOS CONTRATADOS · {contractedServiceCount}"));
        if (appointment.ServiceLines.Count == 0)
        {
            panel.Children.Add(CreatePdvEmptyDetail("Nenhum serviço contratado."));
        }
        else
        {
            foreach (var line in appointment.ServiceLines.Where(item => item.Quantity > 0))
            {
                panel.Children.Add(CreatePdvContractedItemRow(
                    PackIconKind.ContentCut,
                    line.ServiceName,
                    $"{line.Quantity}x · {line.TotalDurationMinutes} min",
                    line.Total.ToString("C", Brazil)));
            }

            panel.Children.Add(CreatePdvInfoRow(
                PackIconKind.CurrencyUsd,
                "Subtotal dos serviços",
                PdvServiceTotal(appointment).ToString("C", Brazil),
                accent: true));
        }

        var contractedProductCount = appointment.ProductLines.Where(item => item.Quantity > 0).Sum(item => item.Quantity);
        panel.Children.Add(CreatePdvSectionLabel($"PRODUTOS ADICIONADOS · {contractedProductCount}"));
        if (appointment.ProductLines.Count == 0)
        {
            panel.Children.Add(CreatePdvEmptyDetail("Nenhum produto adicionado."));
        }
        else
        {
            foreach (var line in appointment.ProductLines.Where(item => item.Quantity > 0))
            {
                panel.Children.Add(CreatePdvContractedItemRow(
                    PackIconKind.PackageVariantClosed,
                    line.ProductName,
                    $"{line.Quantity}x · {line.UnitPrice.ToString("C", Brazil)} cada",
                    line.Total.ToString("C", Brazil)));
            }
        }

        panel.Children.Add(CreatePdvSectionLabel("RESUMO FINANCEIRO"));
        panel.Children.Add(CreatePdvPaymentSummary(
            PdvServiceTotal(appointment),
            PdvProductTotal(appointment),
            PdvAppointmentTotal(appointment)));
        panel.Children.Add(CreatePdvInfoRow(
            PackIconKind.CashRegister,
            "Pagamento",
            appointment.PaymentConfirmedAt is null
                ? "Pendente"
                : $"{appointment.PaymentMethod} · {appointment.PaymentConfirmedAt:dd/MM HH:mm}"));

        if (!string.IsNullOrWhiteSpace(appointment.Notes))
        {
            panel.Children.Add(CreatePdvSectionLabel("OBSERVAÇÕES"));
            panel.Children.Add(new Border
            {
                Background = Solid("#F7F4F1"),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(10, 8, 10, 8),
                Child = new TextBlock
                {
                    Text = appointment.Notes,
                    Foreground = InkBrush,
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        var actions = new UniformGrid { Columns = 3, Margin = new Thickness(0, 6, 0, 0) };
        var edit = CreatePdvActionButton("Editar", false, PdvEditButton_Click);
        edit.Margin = new Thickness(0, 0, 4, 0);
        actions.Children.Add(edit);
        var timer = CreatePdvActionButton(PdvTimerActionLabel(appointment), false, PdvToggleTimerButton_Click);
        timer.Margin = new Thickness(4, 0, 4, 0);
        actions.Children.Add(timer);
        var finish = CreatePdvActionButton("Finalizar", false, PdvFinishButton_Click);
        finish.Margin = new Thickness(4, 0, 0, 0);
        actions.Children.Add(finish);
        panel.Children.Add(actions);

        var receive = CreatePdvActionButton("Receber agora", true, PdvReceiveButton_Click);
        receive.Margin = new Thickness(0, 10, 0, 0);
        panel.Children.Add(receive);
        var customer = CreatePdvActionButton("Ver ficha completa", false, PdvViewCustomerButton_Click);
        customer.Margin = new Thickness(0, 7, 0, 0);
        panel.Children.Add(customer);
        return shell;
    }

    private Border CreatePdvContractedItemRow(PackIconKind icon, string title, string subtitle, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(new PackIcon
        {
            Kind = icon,
            Width = 15,
            Height = 15,
            Foreground = Solid("#D34C12"),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0)
        });
        var text = new StackPanel();
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        text.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 8.8,
            Margin = new Thickness(0, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        var valueText = new TextBlock
        {
            Text = value,
            Foreground = Solid("#D34C12"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(valueText);
        var row = new Border
        {
            Background = Brushes.White,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(4, 7, 4, 7),
            ToolTip = $"{title}\n{subtitle}\n{value}",
            Child = grid
        };
        ToolTipService.SetInitialShowDelay(row, 250);
        return row;
    }

    private Border CreatePdvEmptyDetail(string text) => new()
    {
        Background = Solid("#F7F4F1"),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10, 8, 10, 8),
        Child = new TextBlock
        {
            Text = text,
            Foreground = MutedBrush,
            FontSize = 9.5,
            TextAlignment = TextAlignment.Center
        }
    };

    private UIElement CreatePdvEditPanel(Appointment appointment)
    {
        var shell = CreatePdvPanelShell("Editar", PackIconKind.PencilOutline, appointment, out var panel);

        _pdvEditCustomerBox = CreatePdvTextBox(appointment.CustomerName);
        panel.Children.Add(CreatePdvFieldRow(PackIconKind.AccountOutline, "Cliente", _pdvEditCustomerBox));

        _pdvEditProfessionalBox = CreatePdvComboBox(_data.Professionals.Where(item => item.IsActive).OrderBy(item => item.Name).ToList(), nameof(Professional.Name));
        _pdvEditProfessionalBox.SelectedItem = _data.Professionals.FirstOrDefault(item => item.Id == appointment.ProfessionalId)
                                               ?? _data.Professionals.FirstOrDefault(item => item.Name.Equals(appointment.ProfessionalName, StringComparison.OrdinalIgnoreCase));
        panel.Children.Add(CreatePdvFieldRow(PackIconKind.AccountTieOutline, "Profissional", _pdvEditProfessionalBox));

        var dateTimeGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
        dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
        dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) });
        _pdvEditDatePicker = new DatePicker
        {
            SelectedDate = appointment.Start.Date,
            Height = 32,
            BorderBrush = LineBrush,
            Background = Solid("#F7F4F1")
        };
        dateTimeGrid.Children.Add(_pdvEditDatePicker);
        _pdvEditTimeBox = CreatePdvTextBox(appointment.Start.ToString("HH:mm", Brazil));
        _pdvEditTimeBox.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(_pdvEditTimeBox, 1);
        dateTimeGrid.Children.Add(_pdvEditTimeBox);
        _pdvEditDurationBox = CreatePdvComboBox(new[] { 30, 45, 60, 90, 120, 150, 180 }, null);
        _pdvEditDurationBox.SelectedItem = appointment.DurationMinutes;
        if (_pdvEditDurationBox.SelectedItem is null)
        {
            _pdvEditDurationBox.Text = appointment.DurationMinutes.ToString(Brazil);
        }
        _pdvEditDurationBox.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(_pdvEditDurationBox, 2);
        dateTimeGrid.Children.Add(_pdvEditDurationBox);
        panel.Children.Add(dateTimeGrid);

        _pdvEditNotesBox = CreatePdvTextBox(appointment.Notes);
        _pdvEditNotesBox.Height = 45;
        _pdvEditNotesBox.AcceptsReturn = true;
        _pdvEditNotesBox.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(CreatePdvFieldRow(PackIconKind.NoteTextOutline, "Observações", _pdvEditNotesBox));

        var save = CreatePdvActionButton("Salvar alterações", true, PdvSaveEditButton_Click);
        save.Margin = new Thickness(0, 7, 0, 0);
        panel.Children.Add(save);
        var cancel = CreatePdvActionButton("Cancelar", false, PdvEditCancelButton_Click);
        cancel.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(cancel);
        return shell;
    }

    private TextBox CreatePdvTextBox(string text) => new()
    {
        Text = text,
        Height = 32,
        Padding = new Thickness(8, 3, 8, 3),
        Background = Solid("#F7F4F1"),
        BorderBrush = LineBrush,
        BorderThickness = new Thickness(1),
        Foreground = InkBrush,
        FontSize = 10.5,
        VerticalContentAlignment = VerticalAlignment.Center
    };

    private ComboBox CreatePdvComboBox(System.Collections.IEnumerable items, string? displayMemberPath)
    {
        var combo = new ComboBox
        {
            ItemsSource = items,
            Height = 32,
            Padding = new Thickness(6, 0, 4, 0),
            Background = Solid("#F7F4F1"),
            BorderBrush = LineBrush,
            Foreground = InkBrush,
            FontSize = 10.2,
            IsEditable = false
        };
        if (!string.IsNullOrWhiteSpace(displayMemberPath))
        {
            combo.DisplayMemberPath = displayMemberPath;
        }
        return combo;
    }

    private Border CreatePdvFieldRow(PackIconKind icon, string label, Control control)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new PackIcon
        {
            Kind = icon,
            Width = 15,
            Height = 15,
            Foreground = InkBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        var labelText = new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(labelText, 1);
        grid.Children.Add(labelText);
        Grid.SetColumn(control, 2);
        grid.Children.Add(control);
        return new Border
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 3, 0, 3),
            Child = grid
        };
    }

    private UIElement CreatePdvTimerPanel(Appointment appointment)
    {
        var shell = CreatePdvPanelShell("Tempo", PackIconKind.TimerOutline, appointment, out var panel);
        _pdvTimerPanelElapsedText = new TextBlock
        {
            Foreground = InkBrush,
            FontSize = 35,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };
        panel.Children.Add(_pdvTimerPanelElapsedText);
        _pdvTimerPanelStatusText = new TextBlock
        {
            Foreground = Solid("#D34C12"),
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 10)
        };
        panel.Children.Add(_pdvTimerPanelStatusText);

        _pdvTimerPanelProgress = new ProgressBar
        {
            Height = 5,
            Minimum = 0,
            Maximum = Math.Max(1, appointment.DurationMinutes * 60),
            Foreground = AccentBrush,
            Background = Solid("#E7E2DE"),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(8, 0, 8, 0)
        };
        panel.Children.Add(_pdvTimerPanelProgress);
        var times = new Grid { Margin = new Thickness(7, 4, 7, 10) };
        times.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        times.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        times.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        times.Children.Add(new TextBlock { Text = appointment.Start.ToString("HH:mm", Brazil), Foreground = InkBrush, FontSize = 9.5, FontWeight = FontWeights.Bold });
        _pdvTimerPanelNowText = new TextBlock { Foreground = Solid("#D34C12"), FontSize = 9.5, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(_pdvTimerPanelNowText, 1);
        times.Children.Add(_pdvTimerPanelNowText);
        var end = new TextBlock { Text = appointment.End.ToString("HH:mm", Brazil), Foreground = InkBrush, FontSize = 9.5, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(end, 2);
        times.Children.Add(end);
        panel.Children.Add(times);

        var actions = new UniformGrid { Columns = 2 };
        var toggle = CreatePdvActionButton(PdvTimerActionLabel(appointment), false, PdvToggleTimerButton_Click);
        toggle.Margin = new Thickness(0, 0, 4, 0);
        actions.Children.Add(toggle);
        var finish = CreatePdvActionButton("Finalizar", false, PdvFinishButton_Click);
        finish.Margin = new Thickness(4, 0, 0, 0);
        actions.Children.Add(finish);
        panel.Children.Add(actions);
        panel.Children.Add(new Border
        {
            Background = Solid("#F7F4F1"),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 10, 0, 0),
            Child = new TextBlock
            {
                Text = "Tempo registrado na ficha do atendimento",
                Foreground = MutedBrush,
                FontSize = 9.5
            }
        });
        RefreshPdvTimerPanelVisuals();
        return shell;
    }

    private UIElement CreatePdvProductsPanel(Appointment appointment)
    {
        EnsurePdvServiceLines(appointment);
        appointment.ProductLines ??= [];
        var shell = CreatePdvPanelShell("Produtos e serviços", PackIconKind.PackageVariantClosed, appointment, out var panel);

        panel.Children.Add(CreatePdvSectionLabel("SERVIÇOS CONTRATADOS"));
        var servicePickerRow = new Grid();
        servicePickerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        servicePickerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _pdvServicePicker = CreatePdvComboBox(_data.Services.Where(item => item.IsActive).OrderBy(item => item.Name).ToList(), nameof(ServiceItem.DisplayName));
        _pdvServicePicker.SelectedIndex = _pdvServicePicker.Items.Count > 0 ? 0 : -1;
        servicePickerRow.Children.Add(_pdvServicePicker);
        var addService = CreatePdvPanelIconButton(PackIconKind.Plus, "Adicionar serviço selecionado");
        addService.Margin = new Thickness(6, 0, 0, 0);
        addService.Click += PdvAddServiceButton_Click;
        Grid.SetColumn(addService, 1);
        servicePickerRow.Children.Add(addService);
        panel.Children.Add(servicePickerRow);

        foreach (var line in appointment.ServiceLines)
        {
            panel.Children.Add(CreatePdvServiceLineRow(line));
        }

        panel.Children.Add(CreatePdvInfoRow(
            PackIconKind.ContentCut,
            "Subtotal serviços",
            PdvServiceTotal(appointment).ToString("C", Brazil),
            accent: true));

        panel.Children.Add(CreatePdvSectionLabel("PRODUTOS UTILIZADOS"));

        var pickerRow = new Grid();
        pickerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pickerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _pdvProductPicker = CreatePdvComboBox(_data.Products.Where(item => item.IsActive).OrderBy(item => item.Name).ToList(), nameof(ProductItem.Name));
        _pdvProductPicker.SelectedIndex = _pdvProductPicker.Items.Count > 0 ? 0 : -1;
        pickerRow.Children.Add(_pdvProductPicker);
        var add = CreatePdvPanelIconButton(PackIconKind.Plus, "Adicionar produto selecionado");
        add.Margin = new Thickness(6, 0, 0, 0);
        add.Click += PdvAddProductButton_Click;
        Grid.SetColumn(add, 1);
        pickerRow.Children.Add(add);
        panel.Children.Add(pickerRow);

        if (appointment.ProductLines.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Nenhum produto adicionado.",
                Foreground = MutedBrush,
                FontSize = 10.5,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 18, 0, 18)
            });
        }
        else
        {
            foreach (var line in appointment.ProductLines)
            {
                panel.Children.Add(CreatePdvProductLineRow(appointment, line));
            }
        }

        panel.Children.Add(CreatePdvInfoRow(
            PackIconKind.CurrencyUsd,
            "Subtotal produtos",
            PdvProductTotal(appointment).ToString("C", Brazil),
            accent: true));
        panel.Children.Add(CreatePdvInfoRow(
            PackIconKind.CashRegister,
            "Total do atendimento",
            PdvAppointmentTotal(appointment).ToString("C", Brazil),
            accent: true));
        var addPrimary = CreatePdvActionButton("Adicionar produto", true, PdvAddProductButton_Click);
        addPrimary.Margin = new Thickness(0, 5, 0, 0);
        panel.Children.Add(addPrimary);
        var save = CreatePdvActionButton("Salvar itens", false, PdvSaveProductsButton_Click);
        save.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(save);
        return shell;
    }

    private Border CreatePdvServiceLineRow(AppointmentServiceLine line)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        var identity = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        identity.Children.Add(new TextBlock
        {
            Text = line.ServiceName,
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        identity.Children.Add(new TextBlock
        {
            Text = $"{line.Quantity}x · {line.DurationMinutes} min · {line.UnitPrice.ToString("C", Brazil)}",
            Foreground = MutedBrush,
            FontSize = 8.8
        });
        row.Children.Add(identity);
        var quantity = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var minus = CreatePdvPanelIconButton(PackIconKind.Minus, $"Remover um serviço {line.ServiceName}");
        minus.Width = minus.Height = 27;
        minus.Tag = line.ServiceId;
        minus.Click += PdvDecreaseServiceButton_Click;
        quantity.Children.Add(minus);
        quantity.Children.Add(new TextBlock
        {
            Text = line.Quantity.ToString(Brazil),
            Width = 24,
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        var plus = CreatePdvPanelIconButton(PackIconKind.Plus, $"Adicionar mais um serviço {line.ServiceName}");
        plus.Width = plus.Height = 27;
        plus.Tag = line.ServiceId;
        plus.Click += PdvIncreaseServiceButton_Click;
        quantity.Children.Add(plus);
        Grid.SetColumn(quantity, 1);
        row.Children.Add(quantity);
        var total = new TextBlock
        {
            Text = line.Total.ToString("C", Brazil),
            Foreground = Solid("#D34C12"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(total, 2);
        row.Children.Add(total);
        return new Border
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 7, 0, 7),
            Child = row
        };
    }

    private Border CreatePdvProductLineRow(Appointment appointment, AppointmentProductLine line)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        var identity = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        identity.Children.Add(new TextBlock
        {
            Text = line.ProductName,
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        identity.Children.Add(new TextBlock
        {
            Text = $"{line.Quantity}x · {line.UnitPrice.ToString("C", Brazil)}",
            Foreground = MutedBrush,
            FontSize = 8.8
        });
        row.Children.Add(identity);
        var quantity = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var minus = CreatePdvPanelIconButton(PackIconKind.Minus, $"Remover uma unidade de {line.ProductName}");
        minus.Width = minus.Height = 27;
        minus.Tag = line.ProductId;
        minus.Click += PdvDecreaseProductButton_Click;
        quantity.Children.Add(minus);
        quantity.Children.Add(new TextBlock
        {
            Text = line.Quantity.ToString(Brazil),
            Width = 24,
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        var plus = CreatePdvPanelIconButton(PackIconKind.Plus, $"Adicionar uma unidade de {line.ProductName}");
        plus.Width = plus.Height = 27;
        plus.Tag = line.ProductId;
        plus.Click += PdvIncreaseProductButton_Click;
        quantity.Children.Add(plus);
        Grid.SetColumn(quantity, 1);
        row.Children.Add(quantity);
        var total = new TextBlock
        {
            Text = line.Total.ToString("C", Brazil),
            Foreground = Solid("#D34C12"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(total, 2);
        row.Children.Add(total);
        return new Border
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 7, 0, 7),
            Child = row
        };
    }

    private UIElement CreatePdvReceivePanel(Appointment appointment)
    {
        var shell = CreatePdvPanelShell("Receber", PackIconKind.CashRegister, appointment, out var panel);
        var serviceTotal = PdvServiceTotal(appointment);
        var productTotal = PdvProductTotal(appointment);
        var total = PdvAppointmentTotal(appointment);
        panel.Children.Add(CreatePdvPaymentSummary(serviceTotal, productTotal, total));

        var methods = new UniformGrid { Columns = 3, Margin = new Thickness(0, 2, 0, 0) };
        methods.Children.Add(CreatePdvChoiceButton("Pix", PackIconKind.Qrcode, _pdvReceiveMethod == "Pix", (_, _) => SelectPdvReceiveMethod("Pix")));
        methods.Children.Add(CreatePdvChoiceButton("Cartão", PackIconKind.CreditCardOutline, _pdvReceiveMethod == "Cartão", (_, _) => SelectPdvReceiveMethod("Cartão")));
        methods.Children.Add(CreatePdvChoiceButton("Dinheiro", PackIconKind.Cash, _pdvReceiveMethod == "Dinheiro", (_, _) => SelectPdvReceiveMethod("Dinheiro")));
        panel.Children.Add(methods);

        if (_pdvReceiveMethod == "Cartão")
        {
            panel.Children.Add(CreatePdvSectionLabel("TIPO DO CARTÃO"));
            var types = new UniformGrid { Columns = 2 };
            types.Children.Add(CreatePdvChoiceButton("Crédito", PackIconKind.CreditCardOutline, _pdvCardType == "Crédito", (_, _) => SelectPdvCardType("Crédito")));
            types.Children.Add(CreatePdvChoiceButton("Débito", PackIconKind.CreditCardOutline, _pdvCardType == "Débito", (_, _) => SelectPdvCardType("Débito")));
            panel.Children.Add(types);

            panel.Children.Add(CreatePdvSectionLabel("COMO RECEBER"));
            var modes = new Grid();
            modes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var machine = CreatePdvReceiveModeButton(
                "Enviar para a maquininha",
                IsMercadoPagoPointReady() ? $"{MercadoPagoTerminalLabel()} conectada" : "Conecte em Configurações",
                PackIconKind.CreditCardOutline,
                _pdvSendToMachine,
                (_, _) => SelectPdvReceiveMode(true));
            machine.Margin = new Thickness(0, 0, 4, 0);
            modes.Children.Add(machine);
            var app = CreatePdvReceiveModeButton(
                "Somente registrar no app",
                "Sem enviar para a maquininha",
                PackIconKind.ContentSaveOutline,
                !_pdvSendToMachine,
                (_, _) => SelectPdvReceiveMode(false));
            app.Margin = new Thickness(4, 0, 0, 0);
            Grid.SetColumn(app, 1);
            modes.Children.Add(app);
            panel.Children.Add(modes);
        }

        var primaryText = PdvReceivePrimaryText(total);
        var primary = CreatePdvActionButton(primaryText, true, PdvConfirmReceiptButton_Click);
        primary.IsEnabled = !_pdvPaymentRunning && appointment.PaymentConfirmedAt is null;
        primary.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(primary);
        var split = CreatePdvActionButton("Dividir pagamento", false, PdvSplitPaymentButton_Click);
        split.Margin = new Thickness(0, 3, 0, 0);
        panel.Children.Add(split);
        return shell;
    }

    private Border CreatePdvPaymentSummary(decimal service, decimal products, decimal total)
    {
        var stack = new StackPanel();
        stack.Children.Add(CreatePdvSummaryLine("Serviços", service.ToString("C", Brazil)));
        stack.Children.Add(CreatePdvSummaryLine("Produtos", products.ToString("C", Brazil)));
        stack.Children.Add(CreatePdvSummaryLine("Desconto", 0m.ToString("C", Brazil)));
        var totalLine = CreatePdvSummaryLine("Total", total.ToString("C", Brazil), true);
        totalLine.Margin = new Thickness(0, 4, 0, 0);
        stack.Children.Add(totalLine);
        return new Border
        {
            Background = Solid("#F7F4F1"),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(9, 5, 9, 5),
            Margin = new Thickness(0, 0, 0, 5),
            Child = stack
        };
    }

    private Grid CreatePdvSummaryLine(string label, string value, bool total = false)
    {
        var row = new Grid { Height = total ? 24 : 18 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = total ? InkBrush : MutedBrush,
            FontSize = total ? 13 : 9.5,
            FontWeight = total ? FontWeights.Bold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center
        });
        var valueText = new TextBlock
        {
            Text = value,
            Foreground = total ? Solid("#D34C12") : Solid("#D34C12"),
            FontSize = total ? 16 : 10,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(valueText, 1);
        row.Children.Add(valueText);
        return row;
    }

    private Button CreatePdvChoiceButton(string text, PackIconKind icon, bool selected, RoutedEventHandler click)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        content.Children.Add(new PackIcon { Kind = icon, Width = 14, Height = 14, Margin = new Thickness(0, 0, 5, 0) });
        content.Children.Add(new TextBlock { Text = text, FontSize = 9.8, FontWeight = selected ? FontWeights.Bold : FontWeights.Normal });
        var button = new Button
        {
            Content = content,
            Height = 34,
            Style = (Style)FindResource("GhostButton"),
            Background = selected ? Solid("#FFF1E9") : Brushes.White,
            BorderBrush = selected ? AccentBrush : LineBrush,
            Margin = new Thickness(2, 0, 2, 0),
            Padding = new Thickness(5, 0, 5, 0)
        };
        button.Click += click;
        return button;
    }

    private Button CreatePdvReceiveModeButton(
        string title,
        string subtitle,
        PackIconKind icon,
        bool selected,
        RoutedEventHandler click)
    {
        var layout = new Grid { Margin = new Thickness(6, 4, 6, 4) };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(23) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.Children.Add(new PackIcon
        {
            Kind = icon,
            Width = 17,
            Height = 17,
            Foreground = selected ? Solid("#D34C12") : InkBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0)
        });
        var text = new StackPanel();
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 9.2,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        text.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 7.9,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(text, 1);
        layout.Children.Add(text);
        var button = new Button
        {
            Content = layout,
            MinHeight = 50,
            Style = (Style)FindResource("GhostButton"),
            Background = selected ? Solid("#FFF1E9") : Brushes.White,
            BorderBrush = selected ? AccentBrush : LineBrush,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        button.Click += click;
        return button;
    }

    private TextBlock CreatePdvSectionLabel(string text) => new()
    {
        Text = text,
        Foreground = InkBrush,
        FontSize = 8.4,
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 7, 0, 4)
    };

    private static decimal PdvProductTotal(Appointment appointment) =>
        (appointment.ProductLines ?? []).Sum(line => line.Total);

    private static decimal PdvServiceTotal(Appointment appointment) =>
        appointment.ServiceLines is { Count: > 0 }
            ? appointment.ServiceLines.Sum(line => line.Total)
            : Math.Max(0, appointment.Price);

    private static decimal PdvAppointmentTotal(Appointment appointment) =>
        PdvServiceTotal(appointment) + PdvProductTotal(appointment);

    private static string PdvTimerActionLabel(Appointment appointment)
    {
        var elapsed = PdvElapsed(appointment);
        return appointment.ServiceStartedAt.HasValue && !appointment.ServiceTimerPaused
            ? "Pausar"
            : elapsed.TotalSeconds > 0 ? "Retomar" : "Iniciar";
    }

    private string PdvReceivePrimaryText(decimal total)
    {
        if (_pdvReceiveMethod == "Cartão")
        {
            if (_pdvSendToMachine)
            {
                return IsMercadoPagoPointReady()
                    ? $"Enviar {total.ToString("C", Brazil)} à maquininha"
                    : "Configurar maquininha";
            }

            return $"Registrar {total.ToString("C", Brazil)} no app";
        }

        return _pdvReceiveMethod == "Pix"
            ? $"Receber {total.ToString("C", Brazil)} em Pix"
            : $"Receber {total.ToString("C", Brazil)} em dinheiro";
    }

    private void UpdatePdvRailSelection()
    {
        if (PdvDetailsRailButton is null)
        {
            return;
        }

        SetPdvRailState(PdvDetailsRailButton, _pdvPanelKind == PdvPanelKind.Details);
        SetPdvRailState(PdvEditRailButton, _pdvPanelKind == PdvPanelKind.Edit);
        SetPdvRailState(PdvTimerRailButton, _pdvPanelKind == PdvPanelKind.Timer);
        SetPdvRailState(PdvProductsRailButton, _pdvPanelKind == PdvPanelKind.Products);
        SetPdvRailState(PdvReceiveRailButton, _pdvPanelKind == PdvPanelKind.Receive);
    }

    private static void SetPdvRailState(Button button, bool selected)
    {
        button.Background = selected ? Solid("#FFF1E9") : Brushes.Transparent;
        button.BorderBrush = selected ? AccentBrush : Brushes.Transparent;
        button.Foreground = selected ? Solid("#D34C12") : InkBrush;
    }

    private void SelectPdvReceiveMethod(string method)
    {
        _pdvReceiveMethod = method;
        RefreshPdvPanelForSelection();
    }

    private void SelectPdvCardType(string type)
    {
        _pdvCardType = type;
        RefreshPdvPanelForSelection();
    }

    private void SelectPdvReceiveMode(bool sendToMachine)
    {
        _pdvSendToMachine = sendToMachine;
        RefreshPdvPanelForSelection();
    }

    private void PdvOpenTimerPanelButton_Click(object sender, RoutedEventArgs e) =>
        ShowPdvPanel(PdvPanelKind.Timer);

    private void PdvEditCancelButton_Click(object sender, RoutedEventArgs e) =>
        ShowPdvPanel(PdvPanelKind.Details);

    private void PdvSaveEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment) ||
            _pdvEditCustomerBox is null ||
            _pdvEditDatePicker?.SelectedDate is not DateTime date ||
            _pdvEditTimeBox is null ||
            _pdvEditDurationBox is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_pdvEditCustomerBox.Text))
        {
            ShowStatus("Informe o nome do cliente.");
            _pdvEditCustomerBox.Focus();
            return;
        }

        if (!DateTime.TryParseExact(_pdvEditTimeBox.Text.Trim(), "HH:mm", Brazil, DateTimeStyles.None, out var time))
        {
            ShowStatus("Informe o horário no formato HH:mm.");
            _pdvEditTimeBox.Focus();
            return;
        }

        var durationText = _pdvEditDurationBox.SelectedItem?.ToString() ?? _pdvEditDurationBox.Text;
        if (!int.TryParse(durationText, NumberStyles.Integer, Brazil, out var duration) || duration <= 0)
        {
            ShowStatus("Informe uma duração válida.");
            return;
        }

        var professional = _pdvEditProfessionalBox?.SelectedItem as Professional;
        if (professional is null)
        {
            ShowStatus("Selecione o profissional.");
            return;
        }

        var start = date.Date.Add(time.TimeOfDay);
        var end = start.AddMinutes(duration);
        var conflict = _data.Appointments.FirstOrDefault(item =>
            item.Id != appointment.Id &&
            item.ProfessionalId.Equals(professional.Id, StringComparison.OrdinalIgnoreCase) &&
            IsOperationalStatus(item) &&
            start < item.End && end > item.Start);
        if (conflict is not null)
        {
            ShowStatus($"Horário ocupado por {conflict.CustomerName}, {conflict.Start:HH:mm}–{conflict.End:HH:mm}.");
            return;
        }

        appointment.CustomerName = _pdvEditCustomerBox.Text.Trim();
        appointment.ProfessionalId = professional.Id;
        appointment.ProfessionalName = professional.Name;
        appointment.Start = start;
        appointment.DurationMinutes = duration;
        appointment.Notes = _pdvEditNotesBox?.Text.Trim() ?? "";
        appointment.UpdatedAt = DateTime.Now;
        _selectedDate = appointment.Start.Date;
        _store.Save(_data);
        RefreshAll(appointment.Id);
        ShowPdvPanel(PdvPanelKind.Details);
        ShowStatus("Atendimento atualizado no PDV.");
    }

    private void PdvAddServiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment) || _pdvServicePicker?.SelectedItem is not ServiceItem service)
        {
            ShowStatus("Selecione um serviço para adicionar.");
            return;
        }

        if (appointment.PaymentConfirmedAt is not null)
        {
            ShowStatus("Os itens desse atendimento já foram recebidos.");
            return;
        }

        EnsurePdvServiceLines(appointment);
        var line = appointment.ServiceLines.FirstOrDefault(item => item.ServiceId.Equals(service.Id, StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            appointment.ServiceLines.Add(new AppointmentServiceLine
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                Segment = service.Segment,
                Quantity = 1,
                DurationMinutes = Math.Max(1, service.DurationMinutes),
                UnitPrice = Math.Max(0, service.Price)
            });
        }
        else
        {
            line.Quantity++;
        }

        SyncPdvServiceSummary(appointment);
        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(appointment.Id);
        ShowPdvPanel(PdvPanelKind.Products);
        ShowStatus($"{service.Name} adicionado ao atendimento.");
    }

    private void PdvIncreaseServiceButton_Click(object sender, RoutedEventArgs e) =>
        AdjustPdvServiceQuantity(sender, 1);

    private void PdvDecreaseServiceButton_Click(object sender, RoutedEventArgs e) =>
        AdjustPdvServiceQuantity(sender, -1);

    private void AdjustPdvServiceQuantity(object sender, int delta)
    {
        if (!TryGetPdvAppointment(out var appointment) || sender is not Button { Tag: string serviceId })
        {
            return;
        }

        if (appointment.PaymentConfirmedAt is not null)
        {
            ShowStatus("Os itens desse atendimento já foram recebidos.");
            return;
        }

        EnsurePdvServiceLines(appointment);
        var line = appointment.ServiceLines.FirstOrDefault(item => item.ServiceId.Equals(serviceId, StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            return;
        }

        line.Quantity += delta;
        if (line.Quantity <= 0)
        {
            appointment.ServiceLines.Remove(line);
        }

        SyncPdvServiceSummary(appointment);
        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(appointment.Id);
        ShowPdvPanel(PdvPanelKind.Products);
    }

    private static void EnsurePdvServiceLines(Appointment appointment)
    {
        appointment.ServiceLines ??= [];
        if (appointment.ServiceLines.Count == 0 && !string.IsNullOrWhiteSpace(appointment.ServiceName))
        {
            appointment.ServiceLines.Add(new AppointmentServiceLine
            {
                ServiceId = appointment.ServiceId,
                ServiceName = appointment.ServiceName,
                Segment = appointment.Segment,
                Quantity = 1,
                DurationMinutes = Math.Max(1, appointment.DurationMinutes),
                UnitPrice = Math.Max(0, appointment.Price)
            });
        }
    }

    private static void SyncPdvServiceSummary(Appointment appointment)
    {
        appointment.ServiceLines ??= [];
        var lines = appointment.ServiceLines.Where(item => item.Quantity > 0).ToList();
        appointment.ServiceId = lines.FirstOrDefault()?.ServiceId ?? "";
        appointment.ServiceName = lines.Count == 0
            ? "Sem serviço"
            : string.Join(" + ", lines.Select(item => item.Quantity > 1 ? $"{item.ServiceName} ({item.Quantity}x)" : item.ServiceName));
        appointment.Segment = lines.FirstOrDefault()?.Segment ?? appointment.Segment;
        appointment.Price = lines.Sum(item => item.Total);
        appointment.DurationMinutes = Math.Max(1, lines.Sum(item => item.TotalDurationMinutes));
    }

    private void PdvAddProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment) || _pdvProductPicker?.SelectedItem is not ProductItem product)
        {
            ShowStatus("Selecione um produto para adicionar.");
            return;
        }

        if (appointment.ProductSalesRecordedAt is not null)
        {
            ShowStatus("Os produtos desse atendimento já foram lançados no caixa.");
            return;
        }

        appointment.ProductLines ??= [];
        var line = appointment.ProductLines.FirstOrDefault(item => item.ProductId.Equals(product.Id, StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            appointment.ProductLines.Add(new AppointmentProductLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = product.Price
            });
        }
        else
        {
            line.Quantity++;
        }

        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshPdvPanelForSelection();
        ShowStatus($"{product.Name} adicionado ao atendimento.");
    }

    private void PdvIncreaseProductButton_Click(object sender, RoutedEventArgs e) =>
        AdjustPdvProductQuantity(sender, 1);

    private void PdvDecreaseProductButton_Click(object sender, RoutedEventArgs e) =>
        AdjustPdvProductQuantity(sender, -1);

    private void AdjustPdvProductQuantity(object sender, int delta)
    {
        if (!TryGetPdvAppointment(out var appointment) || sender is not Button { Tag: string productId })
        {
            return;
        }

        if (appointment.ProductSalesRecordedAt is not null)
        {
            ShowStatus("Os produtos desse atendimento já foram lançados no caixa.");
            return;
        }

        appointment.ProductLines ??= [];
        var line = appointment.ProductLines.FirstOrDefault(item => item.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            return;
        }

        line.Quantity += delta;
        if (line.Quantity <= 0)
        {
            appointment.ProductLines.Remove(line);
        }

        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshPdvPanelForSelection();
    }

    private void PdvSaveProductsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        SyncPdvServiceSummary(appointment);
        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(appointment.Id);
        ShowStatus("Produtos e serviços salvos no atendimento e sincronizados.");
    }

    private async void PdvConfirmReceiptButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pdvPaymentRunning || !TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        var current = _data.Appointments.FirstOrDefault(item => item.Id == appointment.Id);
        if (current is null || current.PaymentConfirmedAt is not null)
        {
            ShowStatus("Esse atendimento já possui um pagamento registrado.");
            return;
        }

        var total = PdvAppointmentTotal(current);
        if (total <= 0)
        {
            RecordPdvPayment(current, "Sem cobrança", "Agenda Livre", "", "not_required", 0);
            return;
        }

        _pdvPaymentRunning = true;
        RefreshPdvPanelForSelection();
        try
        {
            if (_pdvReceiveMethod == "Cartão")
            {
                if (_pdvSendToMachine)
                {
                    if (!IsMercadoPagoPointReady())
                    {
                        OpenMercadoPagoSettingsButton_Click(this, new RoutedEventArgs());
                        return;
                    }

                    var pointMethod = _pdvCardType == "Débito" ? MercadoPagoDebitMethod : MercadoPagoCreditMethod;
                    var outcome = await ProcessMercadoPagoPointPaymentAsync(
                        pointMethod,
                        total,
                        current.CustomerName,
                        $"{current.ServiceName} + produtos | {current.Start:dd/MM HH:mm}",
                        this);
                    if (outcome is null)
                    {
                        return;
                    }

                    RecordPdvPayment(
                        current,
                        $"Cartão de {_pdvCardType.ToLower(Brazil)} na Point",
                        "Mercado Pago",
                        outcome.Reference,
                        outcome.Status,
                        total);
                    return;
                }

                RecordPdvPayment(
                    current,
                    $"Cartão de {_pdvCardType.ToLower(Brazil)} (registrado no app)",
                    "Agenda Livre",
                    $"manual-{Guid.NewGuid():N}",
                    "approved",
                    total);
                return;
            }

            if (_pdvReceiveMethod == "Pix")
            {
                if (_data.Settings.MercadoPagoEnabled && _data.Settings.MercadoPagoConnected)
                {
                    var outcome = await ProcessMercadoPagoPixPaymentAsync(
                        total,
                        current.CustomerName,
                        $"{current.ServiceName} + produtos | {current.Start:dd/MM HH:mm}",
                        this);
                    if (outcome is null)
                    {
                        return;
                    }

                    RecordPdvPayment(current, "Pix", "Mercado Pago", outcome.Reference, outcome.Status, total);
                }
                else if (ShowPixKeyPaymentConfirmationDialog(total))
                {
                    RecordPdvPayment(current, "Pix por chave", "Chave Pix", $"manual-{Guid.NewGuid():N}", "approved", total);
                }

                return;
            }

            RecordPdvPayment(current, "Dinheiro", "Agenda Livre", $"manual-{Guid.NewGuid():N}", "approved", total);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível concluir o recebimento.\n\n{ex.Message}", "Modo PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _pdvPaymentRunning = false;
            RefreshPdvPanelForSelection();
        }
    }

    private void RecordPdvPayment(
        Appointment appointment,
        string method,
        string provider,
        string reference,
        string status,
        decimal total)
    {
        var now = DateTime.Now;
        appointment.Status = AppointmentStatus.Done;
        appointment.PaymentConfirmedAt = now;
        appointment.PaymentMethod = method;
        appointment.PaymentProvider = provider;
        appointment.PaymentReference = reference;
        appointment.PaymentStatus = status;
        appointment.UpdatedAt = now;

        appointment.ProductLines ??= [];
        if (appointment.ProductSalesRecordedAt is null)
        {
            foreach (var line in appointment.ProductLines.Where(item => item.Quantity > 0))
            {
                _data.ProductSales.Add(new ProductSale
                {
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    CustomerName = appointment.CustomerName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Discount = 0,
                    PaymentMethod = method,
                    PaymentProvider = provider,
                    PaymentReference = reference,
                    PaymentStatus = status,
                    Notes = $"Atendimento {appointment.Id}",
                    SoldAt = now
                });
                var product = _data.Products.FirstOrDefault(item => item.Id.Equals(line.ProductId, StringComparison.OrdinalIgnoreCase));
                if (product is not null)
                {
                    product.StockQuantity = Math.Max(0, product.StockQuantity - line.Quantity);
                }
            }

            appointment.ProductSalesRecordedAt = now;
        }

        _store.Save(_data);
        RefreshAll(appointment.Id);
        _pdvPanelKind = PdvPanelKind.Details;
        PdvInspectorCard.Visibility = Visibility.Visible;
        RefreshPdvPanelForSelection();
        ShowStatus($"Pagamento de {total.ToString("C", Brazil)} registrado em {method}.");
    }

    private void PdvSplitPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        ShowAppointmentInfoPopup(PdvInspectorCard, appointment);
        ShowStatus("Use as formas de cobrança para registrar as partes do pagamento.");
    }

    private void RefreshPdvTimerPanelVisuals()
    {
        if (_pdvPanelKind != PdvPanelKind.Timer || _selectedAppointment is null)
        {
            return;
        }

        var elapsed = PdvElapsed(_selectedAppointment);
        if (_pdvTimerPanelElapsedText is not null)
        {
            _pdvTimerPanelElapsedText.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }
        if (_pdvTimerPanelStatusText is not null)
        {
            _pdvTimerPanelStatusText.Text = StatusLabel(_selectedAppointment.Status);
        }
        if (_pdvTimerPanelNowText is not null)
        {
            _pdvTimerPanelNowText.Text = $"Agora {DateTime.Now:HH:mm}";
        }
        if (_pdvTimerPanelProgress is not null)
        {
            _pdvTimerPanelProgress.Maximum = Math.Max(1, _selectedAppointment.DurationMinutes * 60);
            _pdvTimerPanelProgress.Value = Math.Min(_pdvTimerPanelProgress.Maximum, elapsed.TotalSeconds);
        }
    }
}
