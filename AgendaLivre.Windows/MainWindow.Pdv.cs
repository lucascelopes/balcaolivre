using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class MainWindow
{
    private const double PdvTimeColumnWidth = 72;
    private const double PdvProfessionalColumnWidth = 222;
    private const double PdvHeaderHeight = 58;
    private const double PdvSlotHeight = 36;
    private const int PdvProfessionalGridUnits = 60;
    private const string PdvAppointmentDragFormat = "AgendaLivre.PdvAppointmentId";

    private readonly DispatcherTimer _pdvTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _pdvTimerAttached;
    private bool _isPdvMode;
    private bool _pdvSidebarWasCollapsed;
    private bool _pdvWeekView;
    private Point _pdvPanStartPoint;
    private double _pdvPanStartHorizontalOffset;
    private double _pdvPanStartVerticalOffset;
    private bool _pdvPanCandidate;
    private bool _pdvPanActive;
    private Point _pdvAppointmentDragStartPoint;
    private string? _pdvAppointmentDragId;
    private bool _pdvAppointmentDragCandidate;
    private bool _pdvSuppressAppointmentClick;

    private void EnterPdvModeButton_Click(object sender, RoutedEventArgs e) => EnterPdvMode();

    private void EnterPdvMode()
    {
        if (!_isPdvMode)
        {
            _pdvSidebarWasCollapsed = _sidebarCollapsed;
            ShowMainPage(MainPage.Agenda);
        }

        _isPdvMode = true;
        if (!_pdvTimerAttached)
        {
            _pdvTimer.Tick += PdvTimer_Tick;
            _pdvTimerAttached = true;
        }

        AppointmentPaymentOverlay.Visibility = Visibility.Collapsed;
        AgendaWorkspaceView.Visibility = Visibility.Collapsed;
        PdvWorkspaceView.Visibility = Visibility.Visible;
        SidebarExpandedPanel.Visibility = Visibility.Collapsed;
        SidebarCollapsedPanel.Visibility = Visibility.Collapsed;
        PdvCompactSidebar.Visibility = Visibility.Visible;
        SidebarColumn.Width = new GridLength(64);
        DefaultHeaderActionsPanel.Visibility = Visibility.Collapsed;
        PdvHeaderActionsPanel.Visibility = Visibility.Visible;
        AppTitleText.Text = "Agenda Livre · PDV";
        AppSubtitleText.Text = "Operação em tempo real";
        WindowCaptionTitleText.Text = "Agenda Livre · Modo PDV";
        WhatsAppFloatingPanel.Visibility = Visibility.Collapsed;
        _pdvTimer.Start();

        RefreshPdvWorkspace();
        Dispatcher.BeginInvoke(ScrollPdvToRelevantTime, DispatcherPriority.Loaded);
    }

    private void ExitPdvModeButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ShowMainPage(MainPage.Agenda);
    }

    private void ExitPdvMode()
    {
        if (!_isPdvMode)
        {
            return;
        }

        _isPdvMode = false;
        _pdvTimer.Stop();
        PdvInspectorCard.Visibility = Visibility.Collapsed;
        PdvWorkspaceView.Visibility = Visibility.Collapsed;
        PdvCompactSidebar.Visibility = Visibility.Collapsed;
        DefaultHeaderActionsPanel.Visibility = Visibility.Visible;
        PdvHeaderActionsPanel.Visibility = Visibility.Collapsed;
        SidebarColumn.Width = new GridLength(_pdvSidebarWasCollapsed ? 72 : 260);
        SidebarExpandedPanel.Visibility = _pdvSidebarWasCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarCollapsedPanel.Visibility = _pdvSidebarWasCollapsed ? Visibility.Visible : Visibility.Collapsed;
        AppTitleText.Text = "Agenda Livre";
        WindowCaptionTitleText.Text = "Agenda Livre";
        AgendaWorkspaceView.Visibility = Visibility.Visible;
        RefreshWhatsAppLauncherVisibility();
    }

    private void PdvQuickAddButton_Click(object sender, RoutedEventArgs e) => NewButton_Click(sender, e);

    private void PdvSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ConfigButton_Click(sender, e);
    }

    private void PdvAgendaRailButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ShowMainPage(MainPage.Agenda);
    }

    private void PdvFinanceRailButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ShowMainPage(MainPage.Finance);
    }

    private void PdvReportsRailButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ShowMainPage(MainPage.Reports);
    }

    private void PdvEstablishmentRailButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ShowMainPage(MainPage.Establishment);
    }

    private void PdvMarketingRailButton_Click(object sender, RoutedEventArgs e)
    {
        ExitPdvMode();
        ShowMainPage(MainPage.Marketing);
    }

    private void PdvPreviousDayButton_Click(object sender, RoutedEventArgs e) =>
        SelectDate(_selectedDate.AddDays(_pdvWeekView ? -7 : -1));

    private void PdvTodayButton_Click(object sender, RoutedEventArgs e) => SelectDate(DateTime.Today);

    private void PdvNextDayButton_Click(object sender, RoutedEventArgs e) =>
        SelectDate(_selectedDate.AddDays(_pdvWeekView ? 7 : 1));

    private void PdvDayButton_Click(object sender, RoutedEventArgs e)
    {
        _pdvWeekView = false;
        RefreshPdvWorkspace();
        Dispatcher.BeginInvoke(ScrollPdvToRelevantTime, DispatcherPriority.Loaded);
    }

    private void PdvWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _pdvWeekView = true;
        PdvInspectorCard.Visibility = Visibility.Collapsed;
        PdvPanelHost.Content = null;
        RefreshPdvWorkspace();
        Dispatcher.BeginInvoke(ScrollPdvToRelevantTime, DispatcherPriority.Loaded);
    }

    private void PdvTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPdvMode)
        {
            return;
        }

        RefreshPdvTimerVisuals();
    }

    private void RefreshPdvWorkspace()
    {
        if (!_isPdvMode || PdvWorkspaceView is null)
        {
            return;
        }

        var visibleAppointments = _pdvWeekView
            ? PdvAppointmentsForSelectedWeek()
            : PdvAppointmentsForSelectedDay();
        if (_selectedAppointment is null || !IsPdvAppointmentVisible(_selectedAppointment))
        {
            _selectedAppointment = visibleAppointments
                .Where(item => item.Status == AppointmentStatus.InService)
                .OrderBy(item => item.Start)
                .FirstOrDefault()
                ?? visibleAppointments
                    .Where(IsOperationalStatus)
                    .OrderBy(item => Math.Abs((item.Start - DateTime.Now).TotalMinutes))
                    .FirstOrDefault();
        }

        if (_pdvWeekView)
        {
            var weekStart = PdvWeekStart(_selectedDate);
            var weekEnd = weekStart.AddDays(6);
            PdvDateTitleText.Text = weekStart.Month == weekEnd.Month
                ? $"{weekStart:dd} a {weekEnd:dd} de {weekStart:MMMM}"
                : $"{weekStart:dd MMM} a {weekEnd:dd MMM}";
            var activeCount = visibleAppointments.Count(IsOperationalStatus);
            var runningCount = visibleAppointments.Count(item => item.Status == AppointmentStatus.InService);
            PdvDateSummaryText.Text = $"{visibleAppointments.Count} atendimento{(visibleAppointments.Count == 1 ? "" : "s")} na semana · {runningCount} em andamento · {activeCount} em aberto";
            BuildPdvWeekScheduleBoard(weekStart, visibleAppointments);
        }
        else
        {
            var pdvDateTitle = _selectedDate.ToString("dddd, dd 'de' MMMM", Brazil);
            PdvDateTitleText.Text = char.ToUpper(pdvDateTitle[0], Brazil) + pdvDateTitle[1..];
            var activeCount = visibleAppointments.Count(IsOperationalStatus);
            var runningCount = visibleAppointments.Count(item => item.Status == AppointmentStatus.InService);
            PdvDateSummaryText.Text = $"{visibleAppointments.Count} atendimento{(visibleAppointments.Count == 1 ? "" : "s")} · {runningCount} em andamento · {activeCount} em aberto";
            BuildPdvScheduleBoard(visibleAppointments);
        }

        UpdatePdvViewButtons();
        RefreshPdvSelectionVisuals();
    }

    private List<Appointment> PdvAppointmentsForSelectedDay() =>
        ApplyFilters(_data.Appointments)
            .Where(item => item.Start.Date == _selectedDate.Date && item.Status != AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.ProfessionalName)
            .ToList();

    private List<Appointment> PdvAppointmentsForSelectedWeek()
    {
        var weekStart = PdvWeekStart(_selectedDate);
        var weekEnd = weekStart.AddDays(7);
        return ApplyFilters(_data.Appointments)
            .Where(item => item.Start >= weekStart && item.Start < weekEnd && item.Status != AppointmentStatus.Blocked)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.ProfessionalName)
            .ToList();
    }

    private static DateTime PdvWeekStart(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }

    private bool IsPdvAppointmentVisible(Appointment appointment)
    {
        if (!_pdvWeekView)
        {
            return appointment.Start.Date == _selectedDate.Date;
        }

        var weekStart = PdvWeekStart(_selectedDate);
        return appointment.Start >= weekStart && appointment.Start < weekStart.AddDays(7);
    }

    private void UpdatePdvViewButtons()
    {
        PdvDayButton.Style = (Style)FindResource(_pdvWeekView ? "GhostButton" : "CommandButton");
        PdvWeekButton.Style = (Style)FindResource(_pdvWeekView ? "CommandButton" : "GhostButton");
    }

    private void BuildPdvWeekScheduleBoard(DateTime weekStart, IReadOnlyCollection<Appointment> appointments)
    {
        PdvScheduleBoardGrid.Children.Clear();
        PdvScheduleBoardGrid.ColumnDefinitions.Clear();
        PdvScheduleBoardGrid.RowDefinitions.Clear();
        PdvScheduleStickyHeaderGrid.Children.Clear();
        PdvScheduleStickyHeaderGrid.ColumnDefinitions.Clear();
        PdvScheduleStickyHeaderGrid.RowDefinitions.Clear();
        PdvScheduleStickyHeaderGrid.RenderTransform = null;

        var startHour = Math.Clamp(_data.Settings.WorkdayStartHour, 0, 23);
        var endHour = Math.Clamp(_data.Settings.WorkdayEndHour, startHour + 1, 24);
        var slotCount = Math.Max(1, (endHour - startHour) * 2);
        var availableWidth = PdvAvailableBoardWidth();
        var dayWidth = Math.Max(142, (availableWidth - PdvTimeColumnWidth) / 7d);
        PdvScheduleBoardGrid.MinWidth = PdvTimeColumnWidth + dayWidth * 7;
        PdvScheduleStickyHeaderGrid.MinWidth = PdvScheduleBoardGrid.MinWidth;
        PdvScheduleStickyHeaderGrid.Visibility = Visibility.Visible;

        foreach (var board in new[] { PdvScheduleBoardGrid, PdvScheduleStickyHeaderGrid })
        {
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PdvTimeColumnWidth) });
            for (var day = 0; day < 7; day++)
            {
                board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(dayWidth) });
            }
        }

        PdvScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(PdvHeaderHeight) });
        PdvScheduleStickyHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(PdvHeaderHeight) });
        for (var row = 0; row < slotCount; row++)
        {
            var maxAppointmentsAtSlot = appointments
                .Where(item =>
                {
                    var itemDayStart = item.Start.Date.AddHours(startHour);
                    return (int)Math.Floor((item.Start - itemDayStart).TotalMinutes / 30d) == row;
                })
                .GroupBy(item => item.Start.Date)
                .Select(group => group.Count())
                .DefaultIfEmpty(0)
                .Max();
            var rowHeight = Math.Max(PdvSlotHeight, maxAppointmentsAtSlot * 58 + (maxAppointmentsAtSlot > 0 ? 6 : 0));
            PdvScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });
        }

        AddPdvWeekHeaders(PdvScheduleBoardGrid, weekStart, appointments);
        AddPdvWeekHeaders(PdvScheduleStickyHeaderGrid, weekStart, appointments);
        AddPdvWeekScheduleCells(weekStart, startHour, slotCount);
        AddPdvWeekAppointmentCards(weekStart, startHour, slotCount, appointments);
        AddPdvWeekCurrentTimeMarker(weekStart, startHour, slotCount);
    }

    private void AddPdvWeekHeaders(Grid board, DateTime weekStart, IReadOnlyCollection<Appointment> appointments)
    {
        var corner = new Border
        {
            Background = Solid("#FBF7F4"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Child = new TextBlock
            {
                Text = "Horário",
                Foreground = MutedBrush,
                FontSize = 10.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetRow(corner, 0);
        Grid.SetColumn(corner, 0);
        board.Children.Add(corner);

        for (var day = 0; day < 7; day++)
        {
            var date = weekStart.AddDays(day);
            var count = appointments.Count(item => item.Start.Date == date.Date);
            var isToday = date.Date == DateTime.Today;
            var header = new Border
            {
                Background = isToday ? Solid("#FFF1E9") : Solid("#FBF7F4"),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = date.ToString("ddd", Brazil).TrimEnd('.').ToUpper(Brazil),
                            Foreground = isToday ? Solid("#D34C12") : InkBrush,
                            FontSize = 9.5,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = $"{date:dd/MM} · {count}",
                            Foreground = isToday ? Solid("#D34C12") : MutedBrush,
                            FontSize = 10.5,
                            FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 3, 0, 0)
                        }
                    }
                }
            };
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, day + 1);
            board.Children.Add(header);
        }
    }

    private void AddPdvWeekScheduleCells(DateTime weekStart, int startHour, int slotCount)
    {
        var defaultProfessional = _data.Professionals
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .FirstOrDefault();
        for (var row = 0; row < slotCount; row++)
        {
            var time = weekStart.AddHours(startHour).AddMinutes(row * 30);
            var wholeHour = time.Minute == 0;
            var timeCell = new Border
            {
                Background = Solid("#FBF7F4"),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, wholeHour ? 1 : 0.5),
                Child = new TextBlock
                {
                    Text = wholeHour ? time.ToString("HH:mm", Brazil) : "",
                    Foreground = MutedBrush,
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 7, 0, 0)
                }
            };
            Grid.SetRow(timeCell, row + 1);
            Grid.SetColumn(timeCell, 0);
            PdvScheduleBoardGrid.Children.Add(timeCell);

            for (var day = 0; day < 7; day++)
            {
                var slotStart = weekStart.AddDays(day).AddHours(startHour).AddMinutes(row * 30);
                var cell = new Border
                {
                    Background = slotStart.Date == DateTime.Today ? Solid("#FFFCFA") : Brushes.White,
                    BorderBrush = LineBrush,
                    BorderThickness = new Thickness(0, 0, 1, wholeHour ? 1 : 0.5),
                    Cursor = defaultProfessional is null ? Cursors.Arrow : Cursors.Hand,
                    Tag = defaultProfessional is null ? null : new ScheduleSlot(defaultProfessional, slotStart),
                    ToolTip = defaultProfessional is null
                        ? "Cadastre um profissional para criar horários."
                        : $"Criar horário em {slotStart:ddd, dd/MM} às {slotStart:HH:mm}"
                };
                var normalBackground = cell.Background;
                cell.MouseEnter += (_, _) => cell.Background = Solid("#FFF8F3");
                cell.MouseLeave += (_, _) => cell.Background = normalBackground;
                if (defaultProfessional is not null)
                {
                    cell.MouseLeftButtonUp += ScheduleEmptySlot_MouseLeftButtonDown;
                    EnablePdvAppointmentDrop(cell, preserveProfessional: true);
                }
                Grid.SetRow(cell, row + 1);
                Grid.SetColumn(cell, day + 1);
                PdvScheduleBoardGrid.Children.Add(cell);
            }
        }
    }

    private void AddPdvWeekAppointmentCards(
        DateTime weekStart,
        int startHour,
        int slotCount,
        IReadOnlyCollection<Appointment> appointments)
    {
        var groupedAppointments = appointments
            .Select(appointment =>
            {
                var day = (appointment.Start.Date - weekStart.Date).Days;
                var dayStart = appointment.Start.Date.AddHours(startHour);
                var row = (int)Math.Floor((appointment.Start - dayStart).TotalMinutes / 30) + 1;
                return new { appointment, day, row };
            })
            .Where(item => item.day is >= 0 and <= 6 && item.row >= 1 && item.row <= slotCount)
            .GroupBy(item => new { item.day, item.row });

        foreach (var group in groupedAppointments)
        {
            var stack = new StackPanel
            {
                Margin = new Thickness(3, 3, 3, 3),
                VerticalAlignment = VerticalAlignment.Top
            };
            foreach (var item in group.OrderBy(value => value.appointment.ProfessionalName).ThenBy(value => value.appointment.CustomerName))
            {
                stack.Children.Add(CreatePdvWeekAppointmentCard(item.appointment));
            }

            Grid.SetRow(stack, group.Key.row);
            Grid.SetColumn(stack, group.Key.day + 1);
            Panel.SetZIndex(stack, 10);
            PdvScheduleBoardGrid.Children.Add(stack);
        }
    }

    private Border CreatePdvWeekAppointmentCard(Appointment appointment)
    {
        var selected = _selectedAppointment?.Id == appointment.Id;
        var background = selected
            ? AccentBrush
            : appointment.Status switch
            {
                AppointmentStatus.InService => Solid("#FFE1D1"),
                AppointmentStatus.Confirmed => Solid("#FFF0E7"),
                AppointmentStatus.Waiting => Solid("#FFEBDD"),
                AppointmentStatus.Done => Solid("#F0EFED"),
                AppointmentStatus.Cancelled or AppointmentStatus.NoShow => Solid("#FCE8E6"),
                _ => Solid("#FFF7F2")
            };
        var foreground = selected ? Brushes.White : InkBrush;
        var secondary = selected ? Solid("#FFF3EC") : MutedBrush;
        var content = new StackPanel { Margin = new Thickness(8, 5, 7, 5) };
        content.Children.Add(new TextBlock
        {
            Text = appointment.CustomerName,
            Foreground = foreground,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        content.Children.Add(new TextBlock
        {
            Text = $"{appointment.Start:HH:mm}–{appointment.End:HH:mm} · {appointment.ServiceName}",
            Foreground = secondary,
            FontSize = 8.3,
            Margin = new Thickness(0, 1, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        content.Children.Add(new TextBlock
        {
            Text = appointment.ProfessionalName,
            Foreground = secondary,
            FontSize = 8.3,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var card = new Border
        {
            MinHeight = 52,
            Margin = new Thickness(0, 0, 0, 4),
            Background = background,
            BorderBrush = selected ? Solid("#C84912") : Solid("#F3C5AA"),
            BorderThickness = new Thickness(selected ? 2 : 1),
            CornerRadius = new CornerRadius(9),
            Cursor = Cursors.Hand,
            Tag = appointment,
            ToolTip = $"{appointment.CustomerName}\n{appointment.ServiceName}\nProfissional: {appointment.ProfessionalName}\n{appointment.Start:dd/MM HH:mm}–{appointment.End:HH:mm}\n{ScheduleStatusLabel(appointment.Status)}",
            Child = content
        };
        if (CanMovePdvAppointment(appointment))
        {
            card.ToolTip += "\nArraste para outro dia ou horário.";
        }
        ToolTipService.SetInitialShowDelay(card, 220);
        card.MouseLeftButtonUp += PdvAppointmentCard_MouseLeftButtonDown;
        card.MouseEnter += (_, _) =>
        {
            card.BorderBrush = AccentBrush;
            card.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(72, 40, 22),
                BlurRadius = 12,
                ShadowDepth = 2,
                Opacity = 0.17
            };
        };
        card.MouseLeave += (_, _) =>
        {
            card.BorderBrush = selected ? Solid("#C84912") : Solid("#F3C5AA");
            card.Effect = null;
        };
        return card;
    }

    private void AddPdvWeekCurrentTimeMarker(DateTime weekStart, int startHour, int slotCount)
    {
        var day = (DateTime.Today - weekStart.Date).Days;
        if (day is < 0 or > 6)
        {
            return;
        }

        var dayStart = DateTime.Today.AddHours(startHour);
        var elapsedMinutes = (DateTime.Now - dayStart).TotalMinutes;
        if (elapsedMinutes < 0 || elapsedMinutes > slotCount * 30)
        {
            return;
        }

        var rawSlot = elapsedMinutes / 30d;
        var slotIndex = Math.Min(slotCount - 1, (int)Math.Floor(rawSlot));
        var rowHeight = PdvScheduleBoardGrid.RowDefinitions.ElementAtOrDefault(slotIndex + 1)?.Height.Value ?? PdvSlotHeight;
        var fractionalOffset = (rawSlot - slotIndex) * rowHeight;
        var line = new Border
        {
            Height = 2,
            Background = AccentBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, fractionalOffset, 0, 0),
            IsHitTestVisible = false
        };
        Grid.SetRow(line, slotIndex + 1);
        Grid.SetColumn(line, day + 1);
        Panel.SetZIndex(line, 30);
        PdvScheduleBoardGrid.Children.Add(line);
    }

    private void BuildPdvScheduleBoard(IReadOnlyCollection<Appointment> appointments)
    {
        PdvScheduleBoardGrid.Children.Clear();
        PdvScheduleBoardGrid.ColumnDefinitions.Clear();
        PdvScheduleBoardGrid.RowDefinitions.Clear();
        PdvScheduleStickyHeaderGrid.Children.Clear();
        PdvScheduleStickyHeaderGrid.ColumnDefinitions.Clear();
        PdvScheduleStickyHeaderGrid.RowDefinitions.Clear();
        PdvScheduleStickyHeaderGrid.RenderTransform = null;

        var professionals = GetBoardProfessionals(appointments).ToList();
        if (professionals.Count == 0)
        {
            PdvScheduleStickyHeaderGrid.Visibility = Visibility.Collapsed;
            PdvScheduleBoardGrid.MinWidth = 760;
            PdvScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(420) });
            PdvScheduleBoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var emptyPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PackIcon
                    {
                        Kind = PackIconKind.AccountPlusOutline,
                        Width = 34,
                        Height = 34,
                        Foreground = AccentBrush,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Cadastre um profissional para montar o calendário do PDV.",
                        Foreground = InkBrush,
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 12, 0, 0)
                    }
                }
            };
            PdvScheduleBoardGrid.Children.Add(emptyPanel);
            return;
        }

        var startHour = Math.Clamp(_data.Settings.WorkdayStartHour, 0, 23);
        var endHour = Math.Clamp(_data.Settings.WorkdayEndHour, startHour + 1, 24);
        var dayStart = _selectedDate.Date.AddHours(startHour);
        var slotCount = Math.Max(1, (endHour - startHour) * 2);
        var availableWidth = PdvAvailableBoardWidth();
        var professionalGroups = professionals
            .Select((professional, index) => new { professional, index })
            .GroupBy(item => item.index / 6)
            .Select(group => (IReadOnlyList<Professional>)group.Select(item => item.professional).ToList())
            .ToList();
        var boardWidth = Math.Max(availableWidth, PdvTimeColumnWidth + (6 * 170));
        var professionalGridUnitWidth = (boardWidth - PdvTimeColumnWidth) / PdvProfessionalGridUnits;

        PdvScheduleBoardGrid.MinWidth = boardWidth;
        PdvScheduleStickyHeaderGrid.MinWidth = PdvScheduleBoardGrid.MinWidth;
        PdvScheduleStickyHeaderGrid.Visibility = professionalGroups.Count == 1
            ? Visibility.Visible
            : Visibility.Collapsed;
        foreach (var board in new[] { PdvScheduleBoardGrid, PdvScheduleStickyHeaderGrid })
        {
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PdvTimeColumnWidth) });
            for (var column = 0; column < PdvProfessionalGridUnits; column++)
            {
                board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(professionalGridUnitWidth) });
            }
        }

        PdvScheduleStickyHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(PdvHeaderHeight) });
        AddPdvHeaders(PdvScheduleStickyHeaderGrid, professionalGroups[0], 0, 0, professionalGroups.Count);

        for (var groupIndex = 0; groupIndex < professionalGroups.Count; groupIndex++)
        {
            if (groupIndex > 0)
            {
                PdvScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            }

            var headerRow = PdvScheduleBoardGrid.RowDefinitions.Count;
            PdvScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(PdvHeaderHeight) });
            for (var row = 0; row < slotCount; row++)
            {
                PdvScheduleBoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(PdvSlotHeight) });
            }

            var group = professionalGroups[groupIndex];
            AddPdvHeaders(PdvScheduleBoardGrid, group, headerRow, groupIndex, professionalGroups.Count);
            AddPdvScheduleCells(dayStart, slotCount, group, headerRow);
            AddPdvAppointmentCards(dayStart, slotCount, group, appointments, headerRow);
            AddPdvCurrentTimeMarker(dayStart, slotCount, group.Count, headerRow);
        }
    }

    private void AddPdvHeaders(
        Grid board,
        IReadOnlyList<Professional> professionals,
        int headerRow = 0,
        int groupIndex = 0,
        int groupCount = 1)
    {
        var corner = new Border
        {
            Background = Solid("#FBF7F4"),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Child = new TextBlock
            {
                Text = groupCount > 1 ? $"Equipe {groupIndex + 1}\nHorário" : "Horário",
                Foreground = MutedBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetRow(corner, headerRow);
        Grid.SetColumn(corner, 0);
        board.Children.Add(corner);

        for (var index = 0; index < professionals.Count; index++)
        {
            var professional = professionals[index];
            var columnSpan = PdvProfessionalGridUnits / professionals.Count;
            var headerGrid = new Grid { Margin = new Thickness(12, 0, 12, 0) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.Children.Add(new Border
            {
                Width = 30,
                Height = 30,
                Background = Solid("#FFE6D8"),
                CornerRadius = new CornerRadius(15),
                Child = new TextBlock
                {
                    Text = InitialsFor(professional.Name),
                    Foreground = Solid("#B34812"),
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock
            {
                Text = professional.Name,
                Foreground = InkBrush,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            text.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(professional.Role) ? "Profissional" : professional.Role,
                Foreground = MutedBrush,
                FontSize = 9.5,
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(text, 1);
            headerGrid.Children.Add(text);

            var header = new Border
            {
                Background = Solid("#FBF7F4"),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = headerGrid,
                ToolTip = $"{professional.Name}\n{(string.IsNullOrWhiteSpace(professional.Role) ? "Profissional" : professional.Role)}"
            };
            header.MouseEnter += (_, _) => header.Background = Solid("#FFF1E9");
            header.MouseLeave += (_, _) => header.Background = Solid("#FBF7F4");
            ToolTipService.SetInitialShowDelay(header, 220);
            Grid.SetRow(header, headerRow);
            Grid.SetColumn(header, 1 + (index * columnSpan));
            Grid.SetColumnSpan(header, columnSpan);
            board.Children.Add(header);
        }
    }

    private void PdvScheduleScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        PdvScheduleStickyHeaderGrid.RenderTransform = new TranslateTransform(-e.HorizontalOffset, 0);
    }

    private double PdvAvailableBoardWidth()
    {
        if (PdvScheduleScrollViewer.ActualWidth > 500)
        {
            return Math.Max(980, PdvScheduleScrollViewer.ActualWidth - 2);
        }

        return Math.Max(980, ActualWidth - 64 - 82 - 30);
    }

    private void PdvScheduleScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindPdvAppointmentCard(e.OriginalSource as DependencyObject) is { Tag: Appointment appointment } card)
        {
            _pdvPanCandidate = false;
            _pdvPanActive = false;
            _pdvAppointmentDragCandidate = CanMovePdvAppointment(appointment);
            _pdvAppointmentDragId = _pdvAppointmentDragCandidate ? appointment.Id : null;
            _pdvAppointmentDragStartPoint = e.GetPosition(PdvScheduleScrollViewer);
            card.Cursor = _pdvAppointmentDragCandidate ? Cursors.SizeAll : Cursors.Hand;
            return;
        }

        ResetPdvAppointmentDrag();
        _pdvPanCandidate = true;
        _pdvPanActive = false;
        _pdvPanStartPoint = e.GetPosition(PdvScheduleScrollViewer);
        _pdvPanStartHorizontalOffset = PdvScheduleScrollViewer.HorizontalOffset;
        _pdvPanStartVerticalOffset = PdvScheduleScrollViewer.VerticalOffset;
    }

    private void PdvScheduleScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_pdvAppointmentDragCandidate &&
            !string.IsNullOrWhiteSpace(_pdvAppointmentDragId) &&
            e.LeftButton == MouseButtonState.Pressed)
        {
            var dragCurrent = e.GetPosition(PdvScheduleScrollViewer);
            var dragDelta = dragCurrent - _pdvAppointmentDragStartPoint;
            if (Math.Abs(dragDelta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(dragDelta.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var appointment = _data.Appointments.FirstOrDefault(item => item.Id == _pdvAppointmentDragId);
            var card = FindPdvAppointmentCard(e.OriginalSource as DependencyObject);
            if (appointment is null || card is null)
            {
                ResetPdvAppointmentDrag();
                return;
            }

            _pdvAppointmentDragCandidate = false;
            _pdvSuppressAppointmentClick = true;
            card.Opacity = 0.58;
            card.Cursor = Cursors.SizeAll;
            var dragData = new DataObject(PdvAppointmentDragFormat, appointment.Id);
            try
            {
                DragDrop.DoDragDrop(card, dragData, DragDropEffects.Move);
            }
            finally
            {
                card.Opacity = 1;
                card.Cursor = Cursors.Hand;
                _pdvAppointmentDragId = null;
                _pdvSuppressAppointmentClick = false;
            }

            e.Handled = true;
            return;
        }

        if (!_pdvPanCandidate || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(PdvScheduleScrollViewer);
        var delta = current - _pdvPanStartPoint;
        if (!_pdvPanActive && Math.Abs(delta.X) + Math.Abs(delta.Y) < 7)
        {
            return;
        }

        if (!_pdvPanActive)
        {
            _pdvPanActive = true;
            PdvScheduleScrollViewer.CaptureMouse();
            PdvScheduleScrollViewer.Cursor = Cursors.SizeAll;
        }

        PdvScheduleScrollViewer.ScrollToHorizontalOffset(_pdvPanStartHorizontalOffset - delta.X);
        PdvScheduleScrollViewer.ScrollToVerticalOffset(_pdvPanStartVerticalOffset - delta.Y);
        e.Handled = true;
    }

    private void PdvScheduleScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_pdvPanActive)
        {
            e.Handled = true;
        }

        EndPdvGridPan();
        ResetPdvAppointmentDrag();
    }

    private void PdvScheduleScrollViewer_LostMouseCapture(object sender, MouseEventArgs e) =>
        EndPdvGridPan();

    private void EndPdvGridPan()
    {
        _pdvPanCandidate = false;
        _pdvPanActive = false;
        PdvScheduleScrollViewer.Cursor = Cursors.Arrow;
        if (Mouse.Captured == PdvScheduleScrollViewer)
        {
            Mouse.Capture(null);
        }
    }

    private static Border? FindPdvAppointmentCard(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Border { Tag: Appointment } card)
            {
                return card;
            }

            current = current is Visual
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    private static bool CanMovePdvAppointment(Appointment appointment) =>
        IsOperationalStatus(appointment);

    private void ResetPdvAppointmentDrag()
    {
        _pdvAppointmentDragCandidate = false;
        _pdvAppointmentDragId = null;
    }

    private void EnablePdvAppointmentDrop(Border cell, bool preserveProfessional)
    {
        cell.AllowDrop = true;
        cell.DragEnter += (_, e) => PreviewPdvAppointmentDrop(cell, e, preserveProfessional);
        cell.DragOver += (_, e) => PreviewPdvAppointmentDrop(cell, e, preserveProfessional);
        cell.DragLeave += (_, _) => RestorePdvScheduleCellBackground(cell);
        cell.Drop += (_, e) => DropPdvAppointment(cell, e, preserveProfessional);
    }

    private void PreviewPdvAppointmentDrop(Border cell, DragEventArgs e, bool preserveProfessional)
    {
        AutoScrollPdvDuringDrag(e.GetPosition(PdvScheduleScrollViewer));

        if (!TryGetPdvDropMove(cell, e.Data, preserveProfessional, out _, out _, out _))
        {
            e.Effects = DragDropEffects.None;
            cell.Background = Solid("#FFF0EE");
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        cell.Background = Solid("#FFE4D3");
        e.Handled = true;
    }

    private void DropPdvAppointment(Border cell, DragEventArgs e, bool preserveProfessional)
    {
        RestorePdvScheduleCellBackground(cell);
        e.Handled = true;

        if (!TryGetPdvDropMove(
                cell,
                e.Data,
                preserveProfessional,
                out var appointment,
                out var draft,
                out var error))
        {
            e.Effects = DragDropEffects.None;
            if (!string.IsNullOrWhiteSpace(error))
            {
                ShowStatus(error);
            }

            return;
        }

        if (appointment.Start == draft.Start &&
            appointment.ProfessionalId.Equals(draft.ProfessionalId, StringComparison.OrdinalIgnoreCase))
        {
            e.Effects = DragDropEffects.None;
            ShowStatus("O atendimento já está nesse horário.");
            return;
        }

        var previousStart = appointment.Start;
        appointment.Start = draft.Start;
        appointment.ProfessionalId = draft.ProfessionalId;
        appointment.ProfessionalName = draft.ProfessionalName;
        appointment.UpdatedAt = DateTime.Now;
        _selectedAppointment = appointment;
        _selectedDate = appointment.Start.Date;
        _store.Save(_data);
        RefreshAll(appointment.Id);
        ShowStatus(
            $"{appointment.CustomerName} reagendado de {previousStart:ddd, dd/MM HH:mm} " +
            $"para {appointment.Start:ddd, dd/MM HH:mm}.");
        e.Effects = DragDropEffects.Move;
    }

    private bool TryGetPdvDropMove(
        Border cell,
        IDataObject data,
        bool preserveProfessional,
        out Appointment appointment,
        out AppointmentDraft draft,
        out string error)
    {
        appointment = null!;
        draft = null!;
        error = "";

        if (!data.GetDataPresent(PdvAppointmentDragFormat) ||
            data.GetData(PdvAppointmentDragFormat) is not string appointmentId ||
            cell.Tag is not ScheduleSlot slot)
        {
            return false;
        }

        appointment = _data.Appointments.FirstOrDefault(item => item.Id == appointmentId)!;
        if (appointment is null)
        {
            error = "O atendimento não foi encontrado.";
            return false;
        }

        if (!CanMovePdvAppointment(appointment))
        {
            error = "Atendimentos finalizados, cancelados ou bloqueados não podem ser reagendados.";
            return false;
        }

        var appointmentProfessionalId = appointment.ProfessionalId;
        var professional = preserveProfessional
            ? _data.Professionals.FirstOrDefault(item =>
                item.Id.Equals(appointmentProfessionalId, StringComparison.OrdinalIgnoreCase))
            : slot.Professional;
        if (professional is null)
        {
            error = "O profissional desse atendimento não está disponível.";
            return false;
        }

        draft = AppointmentDraft.From(appointment) with
        {
            Start = slot.Start,
            ProfessionalId = professional.Id,
            ProfessionalName = professional.Name
        };

        var end = draft.Start.AddMinutes(draft.DurationMinutes);
        if (!TryValidateConfiguredBusinessWindow(draft.Start, end, out error))
        {
            return false;
        }

        var conflict = FindConflicts(draft, appointment.Id)
            .OrderBy(item => item.Start)
            .FirstOrDefault();
        if (conflict is not null)
        {
            error =
                $"Não foi possível mover: {conflict.CustomerName} já ocupa " +
                $"{conflict.Start:dd/MM HH:mm}–{conflict.End:HH:mm}.";
            return false;
        }

        return true;
    }

    private void RestorePdvScheduleCellBackground(Border cell)
    {
        if (cell.Tag is ScheduleSlot slot && _pdvWeekView && slot.Start.Date == DateTime.Today)
        {
            cell.Background = Solid("#FFFCFA");
            return;
        }

        cell.Background = Brushes.White;
    }

    private void AutoScrollPdvDuringDrag(Point position)
    {
        const double edge = 42;
        const double step = 28;

        if (position.Y < edge)
        {
            PdvScheduleScrollViewer.ScrollToVerticalOffset(
                Math.Max(0, PdvScheduleScrollViewer.VerticalOffset - step));
        }
        else if (position.Y > PdvScheduleScrollViewer.ViewportHeight - edge)
        {
            PdvScheduleScrollViewer.ScrollToVerticalOffset(
                Math.Min(
                    PdvScheduleScrollViewer.ScrollableHeight,
                    PdvScheduleScrollViewer.VerticalOffset + step));
        }

        if (position.X < edge)
        {
            PdvScheduleScrollViewer.ScrollToHorizontalOffset(
                Math.Max(0, PdvScheduleScrollViewer.HorizontalOffset - step));
        }
        else if (position.X > PdvScheduleScrollViewer.ViewportWidth - edge)
        {
            PdvScheduleScrollViewer.ScrollToHorizontalOffset(
                Math.Min(
                    PdvScheduleScrollViewer.ScrollableWidth,
                    PdvScheduleScrollViewer.HorizontalOffset + step));
        }
    }

    private void AddPdvScheduleCells(
        DateTime dayStart,
        int slotCount,
        IReadOnlyList<Professional> professionals,
        int headerRow = 0)
    {
        for (var row = 0; row < slotCount; row++)
        {
            var slotStart = dayStart.AddMinutes(row * 30);
            var wholeHour = slotStart.Minute == 0;
            var timeCell = new Border
            {
                Background = Solid("#FBF7F4"),
                BorderBrush = LineBrush,
                BorderThickness = new Thickness(0, 0, 1, wholeHour ? 1 : 0.5),
                Child = new TextBlock
                {
                    Text = wholeHour ? slotStart.ToString("HH:mm", Brazil) : "",
                    Foreground = MutedBrush,
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 7, 0, 0)
                }
            };
            Grid.SetRow(timeCell, headerRow + row + 1);
            Grid.SetColumn(timeCell, 0);
            PdvScheduleBoardGrid.Children.Add(timeCell);

            for (var column = 0; column < professionals.Count; column++)
            {
                var columnSpan = PdvProfessionalGridUnits / professionals.Count;
                var cell = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = LineBrush,
                    BorderThickness = new Thickness(0, 0, 1, wholeHour ? 1 : 0.5),
                    Cursor = Cursors.Hand,
                    Tag = new ScheduleSlot(professionals[column], slotStart),
                    ToolTip = $"Criar horário com {professionals[column].Name} às {slotStart:HH:mm}"
                };
                cell.MouseEnter += (_, _) => cell.Background = Solid("#FFF8F3");
                cell.MouseLeave += (_, _) => cell.Background = Brushes.White;
                cell.MouseLeftButtonUp += ScheduleEmptySlot_MouseLeftButtonDown;
                EnablePdvAppointmentDrop(cell, preserveProfessional: false);
                Grid.SetRow(cell, headerRow + row + 1);
                Grid.SetColumn(cell, 1 + (column * columnSpan));
                Grid.SetColumnSpan(cell, columnSpan);
                PdvScheduleBoardGrid.Children.Add(cell);
            }
        }
    }

    private void AddPdvAppointmentCards(
        DateTime dayStart,
        int slotCount,
        IReadOnlyList<Professional> professionals,
        IReadOnlyCollection<Appointment> appointments,
        int headerRow = 0)
    {
        var columnSpan = PdvProfessionalGridUnits / professionals.Count;
        var columns = professionals
            .Select((professional, index) => new { professional.Id, Column = 1 + (index * columnSpan) })
            .ToDictionary(item => item.Id, item => item.Column, StringComparer.OrdinalIgnoreCase);

        foreach (var appointment in appointments)
        {
            if (!columns.TryGetValue(appointment.ProfessionalId, out var column))
            {
                continue;
            }

            var row = (int)Math.Floor((appointment.Start - dayStart).TotalMinutes / 30) + 1;
            if (row < 1 || row > slotCount)
            {
                continue;
            }

            var rowSpan = Math.Clamp(
                (int)Math.Ceiling(appointment.DurationMinutes / 30d),
                1,
                slotCount - row + 1);
            var card = CreatePdvAppointmentCard(appointment, rowSpan);
            Grid.SetRow(card, headerRow + row);
            Grid.SetColumn(card, column);
            Grid.SetColumnSpan(card, columnSpan);
            Grid.SetRowSpan(card, rowSpan);
            Panel.SetZIndex(card, 10);
            PdvScheduleBoardGrid.Children.Add(card);
        }
    }

    private Border CreatePdvAppointmentCard(Appointment appointment, int rowSpan)
    {
        var selected = _selectedAppointment?.Id == appointment.Id;
        var background = selected
            ? AccentBrush
            : appointment.Status switch
            {
                AppointmentStatus.InService => Solid("#FFE1D1"),
                AppointmentStatus.Confirmed => Solid("#FFF0E7"),
                AppointmentStatus.Waiting => Solid("#FFEBDD"),
                AppointmentStatus.Done => Solid("#F0EFED"),
                AppointmentStatus.Cancelled or AppointmentStatus.NoShow => Solid("#FCE8E6"),
                _ => Solid("#FFF7F2")
            };
        var foreground = selected ? Brushes.White : InkBrush;
        var secondary = selected ? Solid("#FFF3EC") : MutedBrush;

        var stack = new StackPanel
        {
            Margin = new Thickness(11, rowSpan == 1 ? 4 : 7, 9, 4),
            VerticalAlignment = VerticalAlignment.Top
        };
        stack.Children.Add(new TextBlock
        {
            Text = appointment.CustomerName,
            Foreground = foreground,
            FontSize = rowSpan == 1 ? 10.5 : 12,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = rowSpan == 1
                ? $"{appointment.Start:HH:mm} · {appointment.ServiceName}"
                : $"{appointment.Start:HH:mm}–{appointment.End:HH:mm} · {appointment.ServiceName}",
            Foreground = secondary,
            FontSize = rowSpan == 1 ? 8.5 : 9.5,
            Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        if (rowSpan > 1)
        {
            stack.Children.Add(new TextBlock
            {
                Text = ScheduleStatusLabel(appointment.Status),
                Foreground = secondary,
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 3, 0, 0)
            });
        }

        var card = new Border
        {
            Margin = new Thickness(6, 3, 6, 3),
            Background = background,
            BorderBrush = selected ? Solid("#C84912") : Solid("#F3C5AA"),
            BorderThickness = new Thickness(selected ? 2 : 1),
            CornerRadius = new CornerRadius(10),
            Cursor = Cursors.Hand,
            ClipToBounds = true,
            Tag = appointment,
            Child = stack,
            ToolTip = $"{appointment.CustomerName}\n{appointment.ServiceName} com {appointment.ProfessionalName}\n{appointment.Start:HH:mm}–{appointment.End:HH:mm} · {ScheduleStatusLabel(appointment.Status)}\n{appointment.Price.ToString("C", Brazil)}\nClique para selecionar · F2 detalhes · F3 editar",
            Effect = selected
                ? new DropShadowEffect { Color = Color.FromRgb(185, 70, 18), BlurRadius = 12, ShadowDepth = 2, Opacity = 0.18 }
                : null
        };
        if (CanMovePdvAppointment(appointment))
        {
            card.ToolTip += "\nArraste para outro profissional ou horário.";
        }
        ToolTipService.SetInitialShowDelay(card, 220);
        card.MouseLeftButtonUp += PdvAppointmentCard_MouseLeftButtonDown;
        card.MouseEnter += (_, _) =>
        {
            card.BorderBrush = AccentBrush;
            card.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(72, 40, 22),
                BlurRadius = 13,
                ShadowDepth = 3,
                Opacity = 0.15
            };
        };
        card.MouseLeave += (_, _) =>
        {
            card.BorderBrush = selected ? Solid("#C84912") : Solid("#F3C5AA");
            card.Effect = selected
                ? new DropShadowEffect { Color = Color.FromRgb(185, 70, 18), BlurRadius = 12, ShadowDepth = 2, Opacity = 0.18 }
                : null;
        };
        return card;
    }

    private void AddPdvCurrentTimeMarker(
        DateTime dayStart,
        int slotCount,
        int professionalCount,
        int headerRow = 0)
    {
        if (_selectedDate.Date != DateTime.Today)
        {
            return;
        }

        var elapsedMinutes = (DateTime.Now - dayStart).TotalMinutes;
        if (elapsedMinutes < 0 || elapsedMinutes > slotCount * 30)
        {
            return;
        }

        var rawSlot = elapsedMinutes / 30d;
        var slotIndex = Math.Min(slotCount - 1, (int)Math.Floor(rawSlot));
        var fractionalOffset = (rawSlot - slotIndex) * PdvSlotHeight;
        var line = new Border
        {
            Height = 2,
            Background = AccentBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, fractionalOffset, 0, 0),
            IsHitTestVisible = false
        };
        Grid.SetRow(line, headerRow + slotIndex + 1);
        Grid.SetColumn(line, 0);
        Grid.SetColumnSpan(line, PdvProfessionalGridUnits + 1);
        Panel.SetZIndex(line, 30);
        PdvScheduleBoardGrid.Children.Add(line);

        var label = new Border
        {
            Background = AccentBrush,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 2, 6, 2),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, Math.Max(0, fractionalOffset - 9), 0, 0),
            Child = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm", Brazil),
                Foreground = Brushes.White,
                FontSize = 9,
                FontWeight = FontWeights.Bold
            },
            IsHitTestVisible = false
        };
        Grid.SetRow(label, headerRow + slotIndex + 1);
        Grid.SetColumn(label, 0);
        Panel.SetZIndex(label, 31);
        PdvScheduleBoardGrid.Children.Add(label);
    }

    private void PdvAppointmentCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_pdvSuppressAppointmentClick)
        {
            _pdvSuppressAppointmentClick = false;
            e.Handled = true;
            return;
        }

        if (sender is not Border { Tag: Appointment appointment })
        {
            return;
        }

        e.Handled = true;
        _selectedAppointment = appointment;
        if (_pdvWeekView)
        {
            _selectedDate = appointment.Start.Date;
        }
        _pdvPanelKind = PdvPanelKind.Details;
        PdvInspectorCard.Visibility = Visibility.Visible;
        RefreshPdvWorkspace();
        ShowStatus($"{appointment.CustomerName} selecionado no PDV.");
    }

    private void RefreshPdvSelectionVisuals()
    {
        var appointment = _selectedAppointment;
        var valid = appointment is not null && IsPdvAppointmentVisible(appointment);
        PdvPauseRibbonButton.IsEnabled = valid && appointment!.Status is not AppointmentStatus.Done
            and not AppointmentStatus.Cancelled
            and not AppointmentStatus.NoShow
            and not AppointmentStatus.Blocked;
        PdvFinishRibbonButton.IsEnabled = PdvPauseRibbonButton.IsEnabled;

        if (!valid)
        {
            PdvActiveCustomerText.Text = "Selecione um atendimento no calendário";
            PdvActiveServiceText.Text = "O serviço ativo e seus controles aparecem aqui.";
            PdvActiveStatusText.Text = "Aguardando";
            PdvActiveTimerText.Text = "00:00:00";
            PdvInspectorCard.Visibility = Visibility.Collapsed;
            PdvPanelHost.Content = null;
            return;
        }

        PdvActiveCustomerText.Text = appointment!.CustomerName;
        PdvActiveServiceText.Text = $"{appointment.ServiceName} · {appointment.ProfessionalName} · {appointment.Start:HH:mm}–{appointment.End:HH:mm}";
        PdvActiveStatusText.Text = StatusLabel(appointment.Status);

        PdvInspectorCustomerText.Text = appointment.CustomerName;
        PdvInspectorServiceText.Text = appointment.ServiceName;
        PdvInspectorProfessionalText.Text = appointment.ProfessionalName;
        PdvInspectorTimeText.Text = $"{appointment.Start:HH:mm} às {appointment.End:HH:mm}";
        PdvInspectorPriceText.Text = appointment.Price.ToString("C", Brazil);
        RefreshPdvTimerVisuals();
        RefreshPdvPanelForSelection();
    }

    private void RefreshPdvTimerVisuals()
    {
        var appointment = _selectedAppointment;
        if (appointment is null || !IsPdvAppointmentVisible(appointment))
        {
            PdvActiveTimerText.Text = "00:00:00";
            return;
        }

        var elapsed = PdvElapsed(appointment);
        PdvActiveTimerText.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        var isRunning = appointment.ServiceStartedAt.HasValue && !appointment.ServiceTimerPaused;
        var actionLabel = isRunning ? "Pausar" : elapsed.TotalSeconds > 0 ? "Retomar" : "Iniciar";
        PdvPauseRibbonText.Text = actionLabel;
        PdvPauseRibbonIcon.Kind = isRunning ? PackIconKind.Pause : PackIconKind.Play;
        PdvInspectorTimerButton.Content = actionLabel;
        PdvActiveStatusText.Text = StatusLabel(appointment.Status);
        RefreshPdvTimerPanelVisuals();
    }

    private static TimeSpan PdvElapsed(Appointment appointment)
    {
        var seconds = Math.Max(0, appointment.ServiceElapsedSeconds);
        if (appointment.ServiceStartedAt is DateTime startedAt && !appointment.ServiceTimerPaused)
        {
            seconds += Math.Max(0, (int)(DateTime.Now - startedAt).TotalSeconds);
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private bool TryGetPdvAppointment(out Appointment appointment)
    {
        appointment = _selectedAppointment!;
        if (appointment is not null && IsPdvAppointmentVisible(appointment))
        {
            return true;
        }

        ShowStatus("Selecione um atendimento no calendário do PDV.");
        return false;
    }

    private void PdvToggleTimerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        if (appointment.Status is AppointmentStatus.Done or AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Blocked)
        {
            ShowStatus("Esse atendimento não pode ter o tempo alterado.");
            return;
        }

        if (appointment.ServiceStartedAt is DateTime startedAt && !appointment.ServiceTimerPaused)
        {
            appointment.ServiceElapsedSeconds += Math.Max(0, (int)(DateTime.Now - startedAt).TotalSeconds);
            appointment.ServiceStartedAt = null;
            appointment.ServiceTimerPaused = true;
            ShowStatus($"Tempo de {appointment.CustomerName} pausado.");
        }
        else
        {
            appointment.ServiceStartedAt = DateTime.Now;
            appointment.ServiceTimerPaused = false;
            appointment.Status = AppointmentStatus.InService;
            ShowStatus($"Atendimento de {appointment.CustomerName} iniciado.");
        }

        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(appointment.Id);
    }

    private void PdvFinishButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        if (appointment.ServiceStartedAt is DateTime startedAt && !appointment.ServiceTimerPaused)
        {
            appointment.ServiceElapsedSeconds += Math.Max(0, (int)(DateTime.Now - startedAt).TotalSeconds);
        }

        appointment.ServiceStartedAt = null;
        appointment.ServiceTimerPaused = false;
        appointment.Status = AppointmentStatus.Done;
        appointment.UpdatedAt = DateTime.Now;
        _store.Save(_data);
        RefreshAll(appointment.Id);
        PdvInspectorCard.Visibility = Visibility.Visible;
        ShowStatus($"Atendimento de {appointment.CustomerName} finalizado.");
    }

    private void PdvDetailsButton_Click(object sender, RoutedEventArgs e)
        => ShowPdvPanel(PdvPanelKind.Details);

    private void PdvCloseInspectorButton_Click(object sender, RoutedEventArgs e) =>
        PdvInspectorCard.Visibility = Visibility.Collapsed;

    private void PdvEditButton_Click(object sender, RoutedEventArgs e)
        => ShowPdvPanel(PdvPanelKind.Edit);

    private void PdvProductsButton_Click(object sender, RoutedEventArgs e)
        => ShowPdvPanel(PdvPanelKind.Products);

    private void PdvReceiveButton_Click(object sender, RoutedEventArgs e)
        => ShowPdvPanel(PdvPanelKind.Receive);

    private void PdvViewCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPdvAppointment(out var appointment))
        {
            return;
        }

        var customer = _data.Customers.FirstOrDefault(item =>
            (!string.IsNullOrWhiteSpace(appointment.CustomerId) && item.Id.Equals(appointment.CustomerId, StringComparison.OrdinalIgnoreCase)) ||
            item.Name.Equals(appointment.CustomerName, StringComparison.OrdinalIgnoreCase));
        if (customer is null)
        {
            ShowStatus("A ficha desse cliente ainda não foi cadastrada.");
            return;
        }

        ShowCustomerInfoPopup(customer);
    }

    private void ScrollPdvToRelevantTime()
    {
        if (!_isPdvMode || PdvScheduleScrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var startHour = Math.Clamp(_data.Settings.WorkdayStartHour, 0, 23);
        var reference = _selectedAppointment is not null && IsPdvAppointmentVisible(_selectedAppointment)
            ? _selectedAppointment.Start
            : _selectedDate.Date == DateTime.Today
                ? DateTime.Now
                : _selectedDate.Date.AddHours(startHour);
        if (_pdvWeekView)
        {
            var referenceDayStart = reference.Date.AddHours(startHour);
            var slotIndex = Math.Clamp(
                (int)Math.Floor((reference - referenceDayStart).TotalMinutes / 30d),
                0,
                Math.Max(0, PdvScheduleBoardGrid.RowDefinitions.Count - 2));
            var rowTop = PdvScheduleBoardGrid.RowDefinitions
                .Take(slotIndex + 1)
                .Sum(row => row.ActualHeight > 0 ? row.ActualHeight : row.Height.Value);
            PdvScheduleScrollViewer.ScrollToVerticalOffset(
                Math.Max(0, rowTop - PdvScheduleScrollViewer.ViewportHeight * 0.22));
            return;
        }

        var minutes = Math.Max(0, (reference - _selectedDate.Date.AddHours(startHour)).TotalMinutes);
        var offset = Math.Max(0, PdvHeaderHeight + minutes / 30d * PdvSlotHeight - PdvScheduleScrollViewer.ViewportHeight * 0.32);
        PdvScheduleScrollViewer.ScrollToVerticalOffset(offset);
    }

    private void PreparePdvAuditState(
        PdvPanelKind panelKind = PdvPanelKind.Details,
        bool weekView = false,
        bool manyProfessionals = false,
        bool sixProfessionals = false)
    {
        _selectedDate = DateTime.Today;
        _pdvWeekView = weekView;
        _data.Professionals.Clear();
        _data.Appointments.Clear();
        _data.Products.Clear();
        _data.Services.RemoveAll(item => item.Id.StartsWith("__audit_pdv_service_", StringComparison.Ordinal));
        var auditIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "__audit_pdv_prof_1", "__audit_pdv_prof_2", "__audit_pdv_prof_3", "__audit_pdv_prof_4"
        };
        _data.Professionals.RemoveAll(item => auditIds.Contains(item.Id));
        _data.Appointments.RemoveAll(item => item.Id.StartsWith("__audit_pdv_", StringComparison.Ordinal));

        var auditProfessionals = new List<Professional>
        {
            new Professional { Id = "__audit_pdv_prof_1", Name = "Camila Rocha", Role = "Cabeleireira", Segments = ["Salão de Beleza"], IsActive = true },
            new Professional { Id = "__audit_pdv_prof_2", Name = "Júlia Martins", Role = "Manicure", Segments = ["Salão de Beleza"], IsActive = true },
            new Professional { Id = "__audit_pdv_prof_3", Name = "Mariana Costa", Role = "Esteticista", Segments = ["Salão de Beleza"], IsActive = true },
            new Professional { Id = "__audit_pdv_prof_4", Name = "Renata Alves", Role = "Designer", Segments = ["Salão de Beleza"], IsActive = true }
        };
        if (manyProfessionals || sixProfessionals)
        {
            auditProfessionals.Add(new Professional { Id = "__audit_pdv_prof_5", Name = "Bruna Oliveira", Role = "Colorista", Segments = ["Salão de Beleza"], IsActive = true });
            auditProfessionals.Add(new Professional { Id = "__audit_pdv_prof_6", Name = "Fernanda Lima", Role = "Massoterapeuta", Segments = ["Salão de Beleza"], IsActive = true });
        }
        if (manyProfessionals)
        {
            auditProfessionals.Add(new Professional { Id = "__audit_pdv_prof_7", Name = "Gabriela Nunes", Role = "Manicure", Segments = ["Salão de Beleza"], IsActive = true });
            auditProfessionals.Add(new Professional { Id = "__audit_pdv_prof_8", Name = "Helena Souza", Role = "Designer", Segments = ["Salão de Beleza"], IsActive = true });
        }
        _data.Professionals.AddRange(auditProfessionals);
        _data.Services.AddRange(
        [
            new ServiceItem { Id = "__audit_pdv_service_1", Segment = "Salão de Beleza", Name = "Coloração", DurationMinutes = 90, Price = 220m, IsActive = true },
            new ServiceItem { Id = "__audit_pdv_service_2", Segment = "Salão de Beleza", Name = "Tratamento capilar", DurationMinutes = 30, Price = 60m, IsActive = true },
            new ServiceItem { Id = "__audit_pdv_service_3", Segment = "Salão de Beleza", Name = "Escova", DurationMinutes = 45, Price = 85m, IsActive = true }
        ]);

        Appointment AuditAppointment(string id, int professionalIndex, string customer, string service, int hour, int minute, int duration, decimal price, AppointmentStatus status) =>
            new()
            {
                Id = id,
                Segment = "Salão de Beleza",
                CustomerId = id + "_customer",
                CustomerName = customer,
                ServiceId = id + "_service",
                ServiceName = service,
                ProfessionalId = auditProfessionals[professionalIndex].Id,
                ProfessionalName = auditProfessionals[professionalIndex].Name,
                Start = DateTime.Today.AddHours(hour).AddMinutes(minute),
                DurationMinutes = duration,
                Price = price,
                Status = status,
                ServiceStartedAt = status == AppointmentStatus.InService ? DateTime.Now.AddMinutes(-32) : null,
                CreatedAt = DateTime.Now.AddDays(-2),
                UpdatedAt = DateTime.Now
            };

        _data.Appointments.AddRange(
        [
            AuditAppointment("__audit_pdv_appointment_1", 0, "Ana Souza", "Corte + escova", 8, 30, 60, 160m, AppointmentStatus.Confirmed),
            AuditAppointment("__audit_pdv_appointment_2", 1, "Beatriz Lima", "Manicure completa", 9, 0, 60, 75m, AppointmentStatus.Scheduled),
            AuditAppointment("__audit_pdv_appointment_3", 2, "Paula Nunes", "Limpeza de pele", 10, 0, 90, 190m, AppointmentStatus.Confirmed),
            AuditAppointment("__audit_pdv_appointment_4", 3, "Carla Melo", "Design de sobrancelha", 11, 0, 30, 55m, AppointmentStatus.Done),
            AuditAppointment("__audit_pdv_appointment_5", 0, "Isabela Ferreira", "Coloração + tratamento", 13, 30, 120, 280m, AppointmentStatus.InService),
            AuditAppointment("__audit_pdv_appointment_6", 1, "Marina Dias", "Pedicure", 14, 0, 60, 85m, AppointmentStatus.Waiting),
            AuditAppointment("__audit_pdv_appointment_7", 3, "Luana Prado", "Henna", 15, 0, 60, 70m, AppointmentStatus.Scheduled),
            AuditAppointment("__audit_pdv_appointment_8", 2, "Sofia Reis", "Massagem facial", 16, 0, 60, 130m, AppointmentStatus.Confirmed)
        ]);
        if (manyProfessionals || sixProfessionals)
        {
            _data.Appointments.Add(AuditAppointment("__audit_pdv_appointment_9", 4, "Letícia Alves", "Coloração", 10, 30, 90, 220m, AppointmentStatus.Confirmed));
            _data.Appointments.Add(AuditAppointment("__audit_pdv_appointment_10", 5, "Patrícia Melo", "Massagem relaxante", 14, 30, 60, 150m, AppointmentStatus.Scheduled));
        }
        if (manyProfessionals)
        {
            _data.Appointments.Add(AuditAppointment("__audit_pdv_appointment_11", 6, "Débora Silva", "Manicure", 9, 30, 60, 75m, AppointmentStatus.Confirmed));
            _data.Appointments.Add(AuditAppointment("__audit_pdv_appointment_12", 7, "Rafaela Dias", "Design de sobrancelha", 15, 30, 45, 65m, AppointmentStatus.Scheduled));
        }
        if (weekView)
        {
            var weekStart = PdvWeekStart(DateTime.Today);
            for (var index = 0; index < _data.Appointments.Count; index++)
            {
                var appointment = _data.Appointments[index];
                appointment.Start = weekStart
                    .AddDays(index % 7)
                    .AddHours(appointment.Start.Hour)
                    .AddMinutes(appointment.Start.Minute);
            }

            var stackedStart = weekStart.AddDays(4).AddHours(13).AddMinutes(30);
            _data.Appointments[0].Start = stackedStart;
            _data.Appointments[1].Start = stackedStart;
            _data.Appointments[4].Start = stackedStart;
        }
        _selectedAppointment = _data.Appointments.First(item => item.Id == "__audit_pdv_appointment_5");
        _selectedAppointment.ServiceLines =
        [
            new AppointmentServiceLine { ServiceId = "__audit_pdv_service_1", ServiceName = "Coloração", Segment = "Salão de Beleza", Quantity = 1, DurationMinutes = 90, UnitPrice = 220m },
            new AppointmentServiceLine { ServiceId = "__audit_pdv_service_2", ServiceName = "Tratamento capilar", Segment = "Salão de Beleza", Quantity = 1, DurationMinutes = 30, UnitPrice = 60m }
        ];
        SyncPdvServiceSummary(_selectedAppointment);
        _data.Products.AddRange(
        [
            new ProductItem { Id = "__audit_pdv_product_1", Name = "Óleo de cutícula", Price = 25m, StockQuantity = 12, IsActive = true },
            new ProductItem { Id = "__audit_pdv_product_2", Name = "Esmalte premium", Price = 18m, StockQuantity = 8, IsActive = true }
        ]);
        _selectedAppointment.ProductLines =
        [
            new AppointmentProductLine { ProductId = "__audit_pdv_product_1", ProductName = "Óleo de cutícula", Quantity = 1, UnitPrice = 25m },
            new AppointmentProductLine { ProductId = "__audit_pdv_product_2", ProductName = "Esmalte premium", Quantity = 1, UnitPrice = 18m }
        ];
        _data.Settings.MercadoPagoEnabled = true;
        _data.Settings.MercadoPagoConnected = true;
        _data.Settings.MercadoPagoDefaultTerminalId = "NEWLAND_N950__N950TESTE";
        _data.Settings.MercadoPagoDefaultTerminalLabel = "Point Smart 2";
        EnterPdvMode();
        if (weekView)
        {
            PdvInspectorCard.Visibility = Visibility.Collapsed;
            PdvPanelHost.Content = null;
        }
        else
        {
            ShowPdvPanel(panelKind);
        }

        if (manyProfessionals)
        {
            PdvInspectorCard.Visibility = Visibility.Collapsed;
            PdvPanelHost.Content = null;
            var startHour = Math.Clamp(_data.Settings.WorkdayStartHour, 0, 23);
            var endHour = Math.Clamp(_data.Settings.WorkdayEndHour, startHour + 1, 24);
            var firstTeamHeight = PdvHeaderHeight + Math.Max(1, (endHour - startHour) * 2) * PdvSlotHeight + 14;
            Dispatcher.BeginInvoke(
                () => PdvScheduleScrollViewer.ScrollToVerticalOffset(firstTeamHeight),
                DispatcherPriority.ContextIdle);
        }
    }
}
