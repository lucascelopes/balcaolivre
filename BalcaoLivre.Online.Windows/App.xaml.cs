using System.Diagnostics;
using System.Windows;
using Application = System.Windows.Application;

namespace BalcaoLivre.Online.Windows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        DispatcherUnhandledException += (_, args) =>
        {
            Debug.WriteLine($"UI exception: {args.Exception}");
            System.Windows.MessageBox.Show(
                $"O PDV encontrou um erro, mas continuou aberto.\n\n{args.Exception.Message}",
                "Balcao Livre PDV Online",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
