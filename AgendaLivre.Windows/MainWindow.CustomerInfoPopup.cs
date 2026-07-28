using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private static readonly Brush CustomerPopupBlack = Solid("#111111");
    private static readonly Brush CustomerPopupOrange = Solid("#F5651D");
    private static readonly Brush CustomerPopupGreen = Solid("#16A34A");
    private static readonly Brush CustomerPopupMint = Solid("#DCFCE7");
    private static readonly Brush CustomerPopupWarm = Solid("#FFF7F1");
    private static readonly Brush CustomerPopupWarmStrong = Solid("#FFF1E8");
    private static readonly Brush CustomerPopupLine = Solid("#E8E1DC");

    private Border CreateCustomerInfoPopupRedesign(Customer customer)
    {
        var appointments = _data.Appointments
            .Where(item => CustomerMatches(item, customer))
            .OrderByDescending(item => item.Start)
            .ToList();
        var featuredAppointment = SelectCustomerFeaturedAppointment(appointments);

        var card = new Border
        {
            Width = 520,
            MaxHeight = 620,
            Background = PanelBrush,
            BorderBrush = CustomerPopupLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(15, 23, 42),
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.18
            }
        };
        AutomationProperties.SetName(card, $"Detalhes da cliente {customer.Name}");

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateCustomerPopupHeader(customer);
        layout.Children.Add(header);

        var body = new StackPanel { Margin = new Thickness(16, 14, 16, 0) };
        body.Children.Add(CreateCustomerPopupSummary(customer, featuredAppointment));

        var contentHost = new Border
        {
            MinHeight = 214,
            Margin = new Thickness(0, 8, 0, 0)
        };
        body.Children.Add(CreateCustomerPopupTabs(customer, appointments, contentHost));
        body.Children.Add(contentHost);

        Grid.SetRow(body, 1);
        layout.Children.Add(body);

        var footer = CreateCustomerPopupFooter(customer);
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        card.Child = layout;
        return card;
    }

    private Border CreateCustomerPopupHeader(Customer customer)
    {
        var header = new Border
        {
            Height = 72,
            Background = CustomerPopupBlack,
            BorderBrush = CustomerPopupOrange,
            BorderThickness = new Thickness(0, 0, 0, 2),
            CornerRadius = new CornerRadius(16, 16, 0, 0),
            Padding = new Thickness(16, 0, 14, 0)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 44,
            Height = 44,
            Background = CustomerPopupMint,
            CornerRadius = new CornerRadius(22),
            Margin = new Thickness(0, 0, 12, 0),
            Child = new TextBlock
            {
                Text = InitialsFor(customer.Name),
                Foreground = Solid("#087A38"),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var identity = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        identity.Children.Add(new TextBlock
        {
            Text = FirstFilled(customer.Name, "Cliente"),
            Foreground = Brushes.White,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var whatsAppState = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 3, 0, 0)
        };
        whatsAppState.Children.Add(new PackIcon
        {
            Kind = PackIconKind.Whatsapp,
            Width = 14,
            Height = 14,
            Foreground = customer.AcceptsWhatsApp ? Solid("#22C55E") : Solid("#94A3B8"),
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        whatsAppState.Children.Add(new TextBlock
        {
            Text = customer.AcceptsWhatsApp ? "WhatsApp ativo" : "WhatsApp inativo",
            Foreground = customer.AcceptsWhatsApp ? Solid("#22C55E") : Solid("#CBD5E1"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        identity.Children.Add(whatsAppState);

        Grid.SetColumn(identity, 1);
        grid.Children.Add(identity);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var closeButton = CreateCustomerHeaderActionButton(PackIconKind.Close, "Fechar detalhes da cliente");
        closeButton.Click += (_, _) => CloseCustomerInfoPopup();
        actions.Children.Add(closeButton);

        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);

        header.Child = grid;
        return header;
    }

    private Button CreateCustomerHeaderActionButton(PackIconKind icon, string accessibleName)
    {
        var button = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Width = 34,
            MinWidth = 34,
            Height = 34,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            Background = Solid("#171717"),
            BorderBrush = Solid("#4B4B4B"),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            Content = new PackIcon
            {
                Kind = icon,
                Width = 17,
                Height = 17,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        AutomationProperties.SetName(button, accessibleName);
        ToolTipService.SetToolTip(button, accessibleName);
        return button;
    }

    private Grid CreateCustomerPopupSummary(Customer customer, Appointment? featuredAppointment)
    {
        var summary = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.05, GridUnitType.Star) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var appointmentCard = CreateCustomerNextAppointmentCard(featuredAppointment);
        summary.Children.Add(appointmentCard);

        var accountCard = CreateCustomerAccountCard(customer);
        Grid.SetColumn(accountCard, 2);
        summary.Children.Add(accountCard);
        return summary;
    }

    private Border CreateCustomerNextAppointmentCard(Appointment? appointment)
    {
        var card = new Border
        {
            MinHeight = 80,
            Background = CustomerPopupWarmStrong,
            BorderBrush = Solid("#FFD7C0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(CreateCustomerPopupIcon(PackIconKind.CalendarOutline, 40));

        var text = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 6, 0)
        };
        text.Children.Add(new TextBlock
        {
            Text = appointment is not null && appointment.Start < DateTime.Now ? "Último atendimento" : "Próximo atendimento",
            Foreground = MutedBrush,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold
        });
        text.Children.Add(new TextBlock
        {
            Text = appointment is null ? "Nenhum agendamento" : $"{appointment.Start:dd/MM HH:mm}",
            Foreground = InkBrush,
            FontSize = appointment is null ? 13 : 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        if (appointment is not null)
        {
            var status = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = ScheduleAccentFor(appointment.Status),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = ScheduleStatusLabel(appointment.Status),
                    Foreground = ScheduleAccentFor(appointment.Status),
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold
                }
            };
            Grid.SetColumn(status, 2);
            grid.Children.Add(status);
        }

        card.Child = grid;
        return card;
    }

    private Border CreateCustomerAccountCard(Customer customer)
    {
        var openItems = OpenCustomerReceivables(customer);
        var balance = openItems.Sum(item => item.RemainingValue);

        var card = new Border
        {
            MinHeight = 80,
            Background = PanelBrush,
            BorderBrush = CustomerPopupLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var body = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        body.Children.Add(new TextBlock
        {
            Text = "Conta do cliente",
            Foreground = MutedBrush,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold
        });
        body.Children.Add(new TextBlock
        {
            Text = balance.ToString("C", Brazil),
            Foreground = InkBrush,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 2, 0, 0)
        });

        var accountHint = new TextBlock
        {
            Text = openItems.Count == 0 ? "Sem saldo em aberto" : openItems.Count == 1 ? "1 saldo em aberto" : $"{openItems.Count} saldos em aberto",
            Foreground = balance > 0 ? CustomerPopupOrange : MutedBrush,
            FontSize = 9.5,
            Margin = new Thickness(0, 3, 0, 0)
        };
        body.Children.Add(accountHint);
        card.Child = body;

        if (openItems.Count > 0)
        {
            card.Cursor = Cursors.Hand;
            card.ToolTip = "Clique para receber o saldo";
            card.MouseLeftButtonUp += async (_, _) =>
            {
                CloseCustomerInfoPopup();
                await ReceiveCustomerAccountAsync(customer);
            };
        }

        return card;
    }

    private Grid CreateCustomerPopupTabs(Customer customer, IReadOnlyList<Appointment> appointments, Border contentHost)
    {
        var tabs = new Grid
        {
            Height = 40
        };
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var profileButton = CreateCustomerPopupTabButton("Perfil", true);
        var historyButton = CreateCustomerPopupTabButton("Histórico", false);
        var notesButton = CreateCustomerPopupTabButton("Notas", false);

        Grid.SetColumn(historyButton, 1);
        Grid.SetColumn(notesButton, 2);
        tabs.Children.Add(profileButton);
        tabs.Children.Add(historyButton);
        tabs.Children.Add(notesButton);

        var divider = new Border
        {
            Height = 1,
            Background = CustomerPopupLine,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Grid.SetColumnSpan(divider, 4);
        tabs.Children.Insert(0, divider);

        void SelectTab(Button active, UIElement content)
        {
            foreach (var button in new[] { profileButton, historyButton, notesButton })
            {
                var selected = ReferenceEquals(button, active);
                button.Background = selected ? CustomerPopupWarmStrong : Brushes.Transparent;
                button.Foreground = selected ? CustomerPopupOrange : InkBrush;
                button.BorderBrush = selected ? CustomerPopupOrange : Brushes.Transparent;
                button.BorderThickness = selected ? new Thickness(0, 0, 0, 2) : new Thickness(0);
                button.FontWeight = selected ? FontWeights.Bold : FontWeights.SemiBold;
            }

            contentHost.Child = content;
        }

        profileButton.Click += (_, _) => SelectTab(profileButton, CreateCustomerPopupProfilePanel(customer, appointments));
        historyButton.Click += (_, _) => SelectTab(historyButton, CreateCustomerPopupHistoryPanel(appointments));
        notesButton.Click += (_, _) => SelectTab(notesButton, CreateCustomerPopupNotesPanel(customer));

        SelectTab(profileButton, CreateCustomerPopupProfilePanel(customer, appointments));
        return tabs;
    }

    private Button CreateCustomerPopupTabButton(string text, bool selected)
    {
        var button = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Content = text,
            Height = 38,
            MinWidth = 76,
            Padding = new Thickness(14, 0, 14, 0),
            Margin = new Thickness(0),
            Background = selected ? CustomerPopupWarmStrong : Brushes.Transparent,
            Foreground = selected ? CustomerPopupOrange : Solid("#303030"),
            BorderBrush = selected ? CustomerPopupOrange : Brushes.Transparent,
            BorderThickness = selected ? new Thickness(0, 0, 0, 2) : new Thickness(0),
            FontSize = 11.5,
            FontWeight = selected ? FontWeights.Bold : FontWeights.SemiBold,
            Cursor = Cursors.Hand
        };
        AutomationProperties.SetName(button, $"Aba {text}");
        return button;
    }

    private StackPanel CreateCustomerPopupProfilePanel(Customer customer, IReadOnlyList<Appointment> appointments)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };

        var firstRow = new Grid();
        firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        firstRow.Children.Add(CreateCustomerInfoTile(
            PackIconKind.PhoneOutline,
            "Telefone",
            FirstFilled(FormatPhone(customer.Phone), "Sem telefone cadastrado")));

        var segment = CreateCustomerInfoTile(
            PackIconKind.StorefrontOutline,
            "Segmento",
            FirstFilled(customer.Segment, _data.Settings.BusinessSegment, "Sem segmento"));
        Grid.SetColumn(segment, 2);
        firstRow.Children.Add(segment);
        panel.Children.Add(firstRow);

        panel.Children.Add(CreateCustomerInfoTile(
            PackIconKind.AccountOutline,
            "Perfil do cliente",
            FirstFilled(customer.Profile, customer.Tags, "Sem perfil ou tags"),
            new Thickness(0, 10, 0, 0)));

        panel.Children.Add(CreateCustomerHistoryPreview(appointments));
        return panel;
    }

    private Border CreateCustomerInfoTile(PackIconKind icon, string label, string value, Thickness? margin = null)
    {
        var tile = new Border
        {
            MinHeight = 58,
            Background = PanelBrush,
            BorderBrush = CustomerPopupLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = margin ?? new Thickness(0)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(CreateCustomerPopupIcon(icon, 34));

        var text = new StackPanel
        {
            Margin = new Thickness(9, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        text.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = MutedBrush,
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold
        });
        text.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = InkBrush,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        tile.Child = grid;
        return tile;
    }

    private Border CreateCustomerHistoryPreview(IReadOnlyList<Appointment> appointments)
    {
        var preview = new Border
        {
            Background = PanelBrush,
            BorderBrush = CustomerPopupLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 10, 0, 0)
        };

        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = "Histórico recente",
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var latest = appointments.FirstOrDefault();
        if (latest is null)
        {
            body.Children.Add(new TextBlock
            {
                Text = "Nenhum atendimento registrado ainda.",
                Foreground = MutedBrush,
                FontSize = 10.5
            });
        }
        else
        {
            body.Children.Add(CreateCustomerHistoryPreviewRow(latest));
        }

        preview.Child = body;
        return preview;
    }

    private Border CreateCustomerHistoryPreviewRow(Appointment appointment, bool showStatus = false)
    {
        var row = new Border
        {
            Background = CustomerPopupWarm,
            BorderBrush = Solid("#F2E2D8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 7, 8, 7),
            Margin = new Thickness(0, 0, 0, showStatus ? 8 : 0),
            Cursor = Cursors.Hand
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(CreateCustomerPopupIcon(PackIconKind.CalendarOutline, 30));

        var text = new StackPanel
        {
            Margin = new Thickness(8, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        text.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:dd/MM HH:mm} - {FirstFilled(appointment.ServiceName, "Atendimento")}",
            Foreground = InkBrush,
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        text.Children.Add(new TextBlock
        {
            Text = FirstFilled(appointment.ProfessionalName, appointment.ResourceName, "Sem profissional"),
            Foreground = MutedBrush,
            FontSize = 9.5,
            Margin = new Thickness(0, 1, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        UIElement trailing = showStatus
            ? new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = ScheduleAccentFor(appointment.Status),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = ScheduleStatusLabel(appointment.Status),
                    Foreground = ScheduleAccentFor(appointment.Status),
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold
                }
            }
            : new PackIcon
            {
                Kind = PackIconKind.ChevronRight,
                Width = 18,
                Height = 18,
                Foreground = InkBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
        Grid.SetColumn(trailing, 2);
        grid.Children.Add(trailing);
        row.Child = grid;
        row.MouseLeftButtonUp += (_, _) =>
        {
            CloseCustomerInfoPopup();
            ShowAppointmentInfoPopup(row, appointment);
        };
        return row;
    }

    private UIElement CreateCustomerPopupHistoryPanel(IReadOnlyList<Appointment> appointments)
    {
        var list = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        list.Children.Add(new TextBlock
        {
            Text = "Histórico de atendimentos",
            Foreground = InkBrush,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        if (appointments.Count == 0)
        {
            list.Children.Add(CreateCustomerEmptyState(
                PackIconKind.CalendarOutline,
                "Nenhum atendimento registrado",
                "Os próximos atendimentos desta cliente aparecerão aqui."));
        }
        else
        {
            foreach (var appointment in appointments.Take(4))
            {
                list.Children.Add(CreateCustomerHistoryPreviewRow(appointment, true));
            }
        }

        return new ScrollViewer
        {
            MaxHeight = 230,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = list
        };
    }

    private UIElement CreateCustomerPopupNotesPanel(Customer customer)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        panel.Children.Add(CreateCustomerInfoTile(
            PackIconKind.TagOutline,
            "Perfil e tags",
            FirstFilled(customer.Profile, customer.Tags, "Sem perfil ou tags")));
        panel.Children.Add(CreateCustomerInfoTile(
            PackIconKind.PencilOutline,
            "Observações",
            FirstFilled(customer.Notes, "Nenhuma observação cadastrada"),
            new Thickness(0, 10, 0, 0)));

        if (!string.IsNullOrWhiteSpace(customer.Document))
        {
            panel.Children.Add(CreateCustomerInfoTile(
                PackIconKind.ClipboardTextOutline,
                "Documento",
                customer.Document,
                new Thickness(0, 10, 0, 0)));
        }

        return panel;
    }

    private Border CreateCustomerEmptyState(PackIconKind icon, string title, string description)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(20)
        };
        panel.Children.Add(new PackIcon
        {
            Kind = icon,
            Width = 28,
            Height = 28,
            Foreground = CustomerPopupOrange,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = InkBrush,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = MutedBrush,
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });

        return new Border
        {
            Background = CustomerPopupWarm,
            BorderBrush = CustomerPopupLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Child = panel
        };
    }

    private Border CreateCustomerPopupFooter(Customer customer)
    {
        var footer = new Border
        {
            Background = PanelBrush,
            BorderBrush = CustomerPopupLine,
            BorderThickness = new Thickness(0, 1, 0, 0),
            CornerRadius = new CornerRadius(0, 0, 16, 16),
            Padding = new Thickness(16, 10, 16, 12),
            Margin = new Thickness(0, 10, 0, 0)
        };

        var actions = new Grid();
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var editButton = CreateCustomerFooterButton(PackIconKind.Pencil, "Editar cliente", false);
        editButton.Click += (_, _) =>
        {
            CloseCustomerInfoPopup();
            EditCustomer(customer.Id);
        };
        actions.Children.Add(editButton);

        var whatsAppButton = CreateCustomerFooterButton(PackIconKind.Whatsapp, "WhatsApp", true);
        whatsAppButton.Click += async (_, _) => await SendCustomerWhatsAppAsync(customer);
        Grid.SetColumn(whatsAppButton, 2);
        actions.Children.Add(whatsAppButton);

        footer.Child = actions;
        return footer;
    }

    private Button CreateCustomerFooterButton(PackIconKind icon, string label, bool primary)
    {
        var foreground = primary ? Brushes.White : CustomerPopupBlack;
        var button = new Button
        {
            Style = (Style)FindResource(primary ? "CommandButton" : "GhostButton"),
            Height = 40,
            Padding = new Thickness(12, 0, 12, 0),
            Background = primary ? CustomerPopupGreen : Brushes.White,
            BorderBrush = primary ? CustomerPopupGreen : Solid("#737373"),
            BorderThickness = primary ? new Thickness(0) : new Thickness(1),
            Foreground = foreground,
            Cursor = Cursors.Hand,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new PackIcon
                    {
                        Kind = icon,
                        Width = 16,
                        Height = 16,
                        Foreground = foreground,
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = label,
                        Foreground = foreground,
                        FontSize = 11.5,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        AutomationProperties.SetName(button, label);
        return button;
    }

    private static Border CreateCustomerPopupIcon(PackIconKind icon, double size)
    {
        return new Border
        {
            Width = size,
            Height = size,
            Background = Solid("#FFF8F3"),
            BorderBrush = Solid("#F1DED1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(size / 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new PackIcon
            {
                Kind = icon,
                Width = size * 0.42,
                Height = size * 0.42,
                Foreground = CustomerPopupBlack,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static Appointment? SelectCustomerFeaturedAppointment(IReadOnlyList<Appointment> appointments)
    {
        var active = appointments
            .Where(item =>
                item.Start >= DateTime.Now &&
                item.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.NoShow and not AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .FirstOrDefault();

        return active ?? appointments.FirstOrDefault(item => item.Status != AppointmentStatus.Blocked);
    }

    private void OpenCustomerPhone(Customer customer)
    {
        var phone = NormalizeBrazilPhone(customer.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            ShowStatus($"Telefone não cadastrado para {customer.Name}.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"tel:+{phone}",
                UseShellExecute = true
            });
        }
        catch
        {
            Clipboard.SetText(FormatPhone(customer.Phone));
            ShowStatus("Telefone copiado para a área de transferência.");
        }
    }
}
