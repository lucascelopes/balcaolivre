using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace AgendaLivre.Windows;

internal sealed class SyncConflictResolutionDialog : Window
{
    private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(237, 104, 35));
    private bool _allowClose;

    public SyncConflictResolutionDialog(AgendaSyncConflictEventArgs conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        Title = "Resolver sincronização";
        Width = 620;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;
        Background = Brushes.Transparent;
        AllowsTransparency = true;
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        FontWeight = FontWeights.Medium;

        var localButton = CreateButton(
            "Usar dados deste computador",
            Accent,
            Brushes.White,
            AgendaSyncConflictResolution.UseThisComputer);
        var cloudButton = CreateButton(
            "Usar dados da nuvem",
            new SolidColorBrush(Color.FromRgb(245, 243, 240)),
            new SolidColorBrush(Color.FromRgb(48, 43, 39)),
            AgendaSyncConflictResolution.UseCloud);

        var buttons = new Grid { Margin = new Thickness(0, 24, 0, 0) };
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(localButton, 0);
        Grid.SetColumn(cloudButton, 2);
        buttons.Children.Add(localButton);
        buttons.Children.Add(cloudButton);

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = "Sua agenda foi alterada em dois lugares",
            FontSize = 23,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(28, 27, 26))
        });
        content.Children.Add(new TextBlock
        {
            Text = "Escolha qual versão deve continuar como principal. A opção deste computador será enviada para a nuvem; a opção da nuvem substituirá a agenda aberta.",
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = 14,
            LineHeight = 21,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(92, 85, 79))
        });
        content.Children.Add(new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(14, 12, 14, 12),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromRgb(255, 248, 243)),
            Child = new TextBlock
            {
                Text = $"Nenhum dado foi apagado. Cópias de segurança foram salvas em:\n{Path.GetDirectoryName(conflict.LocalCopyPath)}",
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(113, 77, 54))
            }
        });
        content.Children.Add(buttons);

        Content = new Border
        {
            Padding = new Thickness(30),
            CornerRadius = new CornerRadius(18),
            BorderThickness = new Thickness(1.25),
            BorderBrush = new SolidColorBrush(Color.FromRgb(211, 204, 197)),
            Background = Brushes.White,
            Child = content
        };

        AutomationProperties.SetName(this, "Escolher dados deste computador ou dados da nuvem");
        Closing += PreventClosingWithoutChoice;
    }

    public AgendaSyncConflictResolution? Resolution { get; private set; }

    private Button CreateButton(
        string label,
        Brush background,
        Brush foreground,
        AgendaSyncConflictResolution resolution)
    {
        var button = new Button
        {
            Content = label,
            MinHeight = 48,
            Padding = new Thickness(16, 10, 16, 10),
            Background = background,
            Foreground = foreground,
            BorderThickness = new Thickness(0),
            FontSize = 13.5,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => Choose(resolution);
        return button;
    }

    private void Choose(AgendaSyncConflictResolution resolution)
    {
        Resolution = resolution;
        _allowClose = true;
        DialogResult = true;
    }

    private void PreventClosingWithoutChoice(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
    }
}
