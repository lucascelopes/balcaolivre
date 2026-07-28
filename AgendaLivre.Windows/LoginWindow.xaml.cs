using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using MaterialDesignThemes.Wpf;

namespace AgendaLivre.Windows;

public partial class LoginWindow : Window
{
    private static readonly Brush ActiveBackground = new SolidColorBrush(Color.FromRgb(255, 244, 237));
    private static readonly Brush InactiveBackground = Brushes.Transparent;
    private static readonly Brush ActiveBorder = new SolidColorBrush(Color.FromRgb(244, 90, 22));
    private static readonly Brush InactiveBorder = Brushes.Transparent;
    private static readonly Brush ActiveText = new SolidColorBrush(Color.FromRgb(217, 77, 11));
    private static readonly Brush InactiveText = new SolidColorBrush(Color.FromRgb(113, 107, 102));
    private readonly AgendaAuthSessionManager _auth;
    private bool _signUpMode;
    private bool _busy;
    private bool _authenticationSucceeded;

    public LoginWindow(AgendaAuthSessionManager auth)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        InitializeComponent();
        StateChanged += (_, _) => UpdateCustomWindowChromeState();
        UpdateCustomWindowChromeState();
        Loaded += (_, _) => EmailTextBox.Focus();
    }

    private void CustomTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleCustomWindowState();
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The pointer may be released before the native move operation starts.
        }
    }

    private void CustomMinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CustomMaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleCustomWindowState();
    }

    private void CustomCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleCustomWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateCustomWindowChromeState()
    {
        if (CustomMaximizeIcon is null || CustomMaximizeButton is null)
        {
            return;
        }

        var isMaximized = WindowState == WindowState.Maximized;
        CustomMaximizeIcon.Kind = isMaximized ? PackIconKind.WindowRestore : PackIconKind.WindowMaximize;
        AutomationProperties.SetName(
            CustomMaximizeButton,
            isMaximized ? "Restaurar janela" : "Maximizar janela");
    }

    public AgendaAuthSession? Session => _auth.CurrentSession;

    public event EventHandler? AuthenticationSucceeded;

    private void LoginModeButton_Click(object sender, RoutedEventArgs e) => SetMode(signUp: false);

    private void SignUpModeButton_Click(object sender, RoutedEventArgs e) => SetMode(signUp: true);

    private void PrivacyLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void SetMode(bool signUp)
    {
        if (_busy)
        {
            return;
        }

        _signUpMode = signUp;
        SetPasswordVisibility(visible: false);
        SignUpFieldsPanel.Visibility = signUp ? Visibility.Visible : Visibility.Collapsed;
        LoginBenefitsPanel.Visibility = signUp ? Visibility.Collapsed : Visibility.Visible;
        AuthFormOffset.Y = signUp ? 0 : 30;
        SignUpPrivacyText.Visibility = signUp ? Visibility.Visible : Visibility.Collapsed;
        PrivacyText.Visibility = signUp ? Visibility.Collapsed : Visibility.Visible;
        TitleText.Text = signUp ? "Crie sua conta" : "Bem-vindo de volta";
        SubtitleText.Text = signUp
            ? "Cadastre-se para usar a mesma agenda no Windows e na Web."
            : "Entre para abrir sua agenda sincronizada.";
        PrimaryButton.Content = signUp ? "Criar minha conta" : "Entrar";
        PasswordHelpText.Text = signUp ? "Crie uma senha com pelo menos 6 caracteres." : "Use a senha da sua conta.";
        LoginModeButton.Background = signUp ? InactiveBackground : ActiveBackground;
        LoginModeButton.Foreground = signUp ? InactiveText : ActiveText;
        LoginModeButton.BorderBrush = signUp ? InactiveBorder : ActiveBorder;
        SignUpModeButton.Background = signUp ? ActiveBackground : InactiveBackground;
        SignUpModeButton.Foreground = signUp ? ActiveText : InactiveText;
        SignUpModeButton.BorderBrush = signUp ? ActiveBorder : InactiveBorder;
        HideFeedback();
        (signUp ? FullNameTextBox : EmailTextBox).Focus();
    }

    private async void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || !ValidateForm())
        {
            return;
        }

        SetBusy(true);
        try
        {
            if (_signUpMode)
            {
                var result = await _auth.SignUpAsync(
                    EmailTextBox.Text,
                    PasswordInput.Password,
                    FullNameTextBox.Text,
                    BusinessNameTextBox.Text);
                if (result.Session is not null)
                {
                    CompleteAuthentication();
                    return;
                }

                SetBusy(false);
                SetMode(signUp: false);
                PasswordInput.Password = "";
                ShowFeedback(result.Message, success: true);
                PasswordInput.Focus();
                return;
            }

            await _auth.SignInAsync(EmailTextBox.Text, PasswordInput.Password);
            CompleteAuthentication();
        }
        catch (AgendaAuthException exception)
        {
            ShowFeedback(exception.Message, success: false);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ShowFeedback("Não foi possível conectar agora. Verifique a internet e tente novamente.", success: false);
        }
        finally
        {
            if (!_authenticationSucceeded)
            {
                SetBusy(false);
            }
        }
    }

    private void CompleteAuthentication()
    {
        _authenticationSucceeded = true;
        PrimaryButton.Content = _signUpMode
            ? "Abrindo seu cadastro..."
            : "Abrindo sua agenda...";
        AuthenticationSucceeded?.Invoke(this, EventArgs.Empty);
    }

    private bool ValidateForm()
    {
        SyncPasswordFromReveal();
        HideFeedback();
        if (_signUpMode && string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            ShowFeedback("Informe seu nome completo.", success: false);
            FullNameTextBox.Focus();
            return false;
        }

        if (_signUpMode && string.IsNullOrWhiteSpace(BusinessNameTextBox.Text))
        {
            ShowFeedback("Informe o nome do seu negócio.", success: false);
            BusinessNameTextBox.Focus();
            return false;
        }

        var email = EmailTextBox.Text.Trim();
        if (email.Length < 5 || !email.Contains('@') || !email.Contains('.'))
        {
            ShowFeedback("Informe um e-mail válido.", success: false);
            EmailTextBox.Focus();
            return false;
        }

        if (PasswordInput.Password.Length < 6)
        {
            ShowFeedback("A senha precisa ter pelo menos 6 caracteres.", success: false);
            PasswordInput.Focus();
            return false;
        }

        return true;
    }

    private void PasswordRevealButton_Click(object sender, RoutedEventArgs e) =>
        SetPasswordVisibility(PasswordRevealButton.IsChecked == true);

    private void SetPasswordVisibility(bool visible)
    {
        if (visible)
        {
            PasswordRevealTextBox.Text = PasswordInput.Password;
            PasswordInput.Visibility = Visibility.Collapsed;
            PasswordRevealTextBox.Visibility = Visibility.Visible;
            PasswordRevealIcon.Kind = PackIconKind.EyeOffOutline;
            PasswordRevealTextBox.Focus();
            PasswordRevealTextBox.CaretIndex = PasswordRevealTextBox.Text.Length;
        }
        else
        {
            SyncPasswordFromReveal();
            PasswordRevealTextBox.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            PasswordRevealIcon.Kind = PackIconKind.EyeOutline;
        }

        PasswordRevealButton.IsChecked = visible;
    }

    private void SyncPasswordFromReveal()
    {
        if (PasswordRevealTextBox.Visibility == Visibility.Visible)
        {
            PasswordInput.Password = PasswordRevealTextBox.Text;
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        PrimaryButton.IsEnabled = !busy;
        LoginModeButton.IsEnabled = !busy;
        SignUpModeButton.IsEnabled = !busy;
        EmailTextBox.IsEnabled = !busy;
        PasswordInput.IsEnabled = !busy;
        PasswordRevealTextBox.IsEnabled = !busy;
        PasswordRevealButton.IsEnabled = !busy;
        FullNameTextBox.IsEnabled = !busy;
        BusinessNameTextBox.IsEnabled = !busy;
        PrimaryButton.Content = busy
            ? "Aguarde..."
            : _signUpMode ? "Criar minha conta" : "Entrar";
    }

    private void ShowFeedback(string message, bool success)
    {
        FeedbackText.Text = message;
        FeedbackBorder.Background = new SolidColorBrush(success
            ? Color.FromRgb(236, 253, 245)
            : Color.FromRgb(255, 241, 233));
        FeedbackBorder.BorderBrush = new SolidColorBrush(success
            ? Color.FromRgb(167, 243, 208)
            : Color.FromRgb(247, 199, 170));
        FeedbackText.Foreground = new SolidColorBrush(success
            ? Color.FromRgb(4, 120, 87)
            : Color.FromRgb(143, 59, 19));
        FeedbackBorder.Visibility = Visibility.Visible;
    }

    private void HideFeedback()
    {
        FeedbackText.Text = "";
        FeedbackBorder.Visibility = Visibility.Collapsed;
    }
}
