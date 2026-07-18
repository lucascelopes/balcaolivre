using System.ComponentModel;
using System.Net.Http;
using System.Windows;

namespace AgendaLivre.Windows;

public partial class App : Application
{
    private AgendaAuthSessionManager? _auth;
    private AgendaSyncCoordinator? _syncCoordinator;
    private MainWindow? _agendaWindow;
    private bool _changingAccountWindow;
    private bool _closeAfterFlush;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            if (IsAuditStartup())
            {
                var auditWindow = new MainWindow(new AgendaDataStore(), syncCoordinator: null);
                MainWindow = auditWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                auditWindow.Show();
                return;
            }

            _auth = await AgendaAuthSessionManager.CreateAsync();
            await _auth.RestoreAsync();
            if (!await EnsureAuthenticatedAsync())
            {
                Shutdown();
                return;
            }

            await OpenAgendaWindowAsync();
        }
        catch (AgendaAuthException exception)
        {
            ShowStartupFailure(exception.Message);
            Shutdown();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ShowStartupFailure("Não foi possível conectar ao serviço de conta. Verifique a internet e tente novamente.");
            Shutdown();
        }
        catch (Exception exception)
        {
            ShowStartupFailure($"Não foi possível abrir o Agenda Livre: {exception.Message}");
            Shutdown();
        }
    }

    private Task<bool> EnsureAuthenticatedAsync()
    {
        if (_auth?.CurrentSession is not null)
        {
            return Task.FromResult(true);
        }

        var login = new LoginWindow(_auth ?? throw new InvalidOperationException("Serviço de conta indisponível."));
        MainWindow = login;
        var accepted = login.ShowDialog() == true && login.Session is not null;
        MainWindow = null;
        return Task.FromResult(accepted);
    }

    private async Task OpenAgendaWindowAsync()
    {
        var auth = _auth ?? throw new InvalidOperationException("Serviço de conta indisponível.");
        var session = auth.CurrentSession ?? throw new AgendaAuthException("Entre para abrir sua agenda.");
        var pendingInitialOnboarding = auth.RequiresInitialOnboarding;
        var store = new AgendaDataStore(session.UserId);
        var coordinator = await AgendaSyncCoordinator.CreateAndReconcileAsync(
            store,
            auth,
            allowLegacyMigration: !pendingInitialOnboarding);
        var forceFullOnboarding = pendingInitialOnboarding && !HasCompletedOnboarding(coordinator.InitialData);
        if (pendingInitialOnboarding && !forceFullOnboarding)
        {
            auth.CompleteInitialOnboarding();
        }

        var window = new MainWindow(store, coordinator, forceFullOnboarding);
        window.LogoutRequested += AgendaWindow_LogoutRequested;
        window.InitialOnboardingCompleted += AgendaWindow_InitialOnboardingCompleted;
        window.Closing += AgendaWindow_Closing;
        window.Closed += AgendaWindow_Closed;
        _syncCoordinator = coordinator;
        _agendaWindow = window;
        MainWindow = window;
        window.Show();
    }

    private static bool HasCompletedOnboarding(AgendaData data) =>
        data.Settings.OnboardingCompleted &&
        !string.IsNullOrWhiteSpace(data.Settings.BusinessSegment);

    private async void AgendaWindow_LogoutRequested(object? sender, EventArgs e)
    {
        if (_changingAccountWindow || _auth is null)
        {
            return;
        }

        _changingAccountWindow = true;
        try
        {
            await FlushCurrentSyncAsync();
            _syncCoordinator?.Dispose();
            _syncCoordinator = null;
            await _auth.SignOutAsync();

            var previous = _agendaWindow;
            _agendaWindow = null;
            if (previous is not null)
            {
                previous.LogoutRequested -= AgendaWindow_LogoutRequested;
                previous.InitialOnboardingCompleted -= AgendaWindow_InitialOnboardingCompleted;
                previous.Closing -= AgendaWindow_Closing;
                previous.Closed -= AgendaWindow_Closed;
                previous.Close();
            }

            MainWindow = null;
            if (!await EnsureAuthenticatedAsync())
            {
                Shutdown();
                return;
            }

            await OpenAgendaWindowAsync();
        }
        catch (Exception exception) when (exception is AgendaAuthException or HttpRequestException or TaskCanceledException)
        {
            MessageBox.Show(
                $"A sessão local foi encerrada, mas a nova conta não pôde ser aberta: {exception.Message}",
                "Agenda Livre",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
        }
        finally
        {
            _changingAccountWindow = false;
        }
    }

    private async void AgendaWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_changingAccountWindow || _closeAfterFlush || sender is not MainWindow window)
        {
            return;
        }

        e.Cancel = true;
        _closeAfterFlush = true;
        try
        {
            await FlushCurrentSyncAsync();
        }
        finally
        {
            _syncCoordinator?.Dispose();
            _syncCoordinator = null;
            window.Close();
        }
    }

    private void AgendaWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is MainWindow window)
        {
            window.InitialOnboardingCompleted -= AgendaWindow_InitialOnboardingCompleted;
        }

        if (_changingAccountWindow)
        {
            return;
        }

        _agendaWindow = null;
        Shutdown();
    }

    private void AgendaWindow_InitialOnboardingCompleted(object? sender, EventArgs e) =>
        _auth?.CompleteInitialOnboarding();

    private async Task FlushCurrentSyncAsync()
    {
        if (_syncCoordinator is null)
        {
            return;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            await _syncCoordinator.FlushAsync(timeout.Token);
        }
        catch (Exception exception) when (exception is OperationCanceledException or HttpRequestException or AgendaAuthException)
        {
            // The local atomic save already succeeded; the next session can retry.
        }
    }

    private static void ShowStartupFailure(string message) =>
        MessageBox.Show(
            message,
            "Agenda Livre",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

    private static bool IsAuditStartup() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_STATE")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGENDA_LIVRE_AUDIT_SCREENSHOT_PATH"));

    protected override void OnExit(ExitEventArgs e)
    {
        _syncCoordinator?.Dispose();
        _auth?.Dispose();
        base.OnExit(e);
    }
}
