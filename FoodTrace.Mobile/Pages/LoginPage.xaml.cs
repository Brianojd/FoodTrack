using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace FoodTrace.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly SessionService sessionService;
    private readonly ILogger<LoginPage> logger;
    private bool restoring;
    private bool busy;

    public LoginPage(SessionService sessionService, ILogger<LoginPage> logger)
    {
        this.sessionService = sessionService;
        this.logger = logger;
        try
        {
            InitializeComponent();
            logger.LogDebug("LoginPage.InitializeComponent completado; recursos XAML de LoginPage cargados.");
        }
        catch (Exception exception)
        {
            logger.LogCritical("Fallo al cargar los recursos XAML de LoginPage: {Exception}", exception.ToString());
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (restoring || sessionService.IsAuthenticated) return;
        restoring = true;
        SetBusy(true);
        try
        {
            if (await sessionService.RestoreAsync())
                OpenHome();
        }
        catch (Exception exception)
        {
            logger.LogError("Falló la restauración de sesión al abrir LoginPage: {Exception}", exception.ToString());
            await Alertas.ErrorAsync(this, "No se pudo restaurar la sesión. Ingresá nuevamente.");
        }
        finally { restoring = false; }
        if (!sessionService.IsAuthenticated) SetBusy(false);
    }

    private void TogglePasswordClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordVisibilityButton.Source = PasswordEntry.IsPassword ? "eye_outline.svg" : "eye_off_outline.svg";
    }

    private async void LoginClicked(object? sender, EventArgs e)
    {
        if (busy || !LoginButton.IsEnabled) return;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            await Alertas.ErrorAsync(this, "Ingresá tu correo electrónico.");
            return;
        }
        if (!MailAddress.TryCreate(email, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            await Alertas.ErrorAsync(this, "Ingresá un correo electrónico válido.");
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            await Alertas.ErrorAsync(this, "Ingresá tu contraseña.");
            return;
        }

        SetBusy(true);
        try
        {
            await sessionService.LoginAsync(email, password);
            PasswordEntry.Text = string.Empty;
            OpenHome();
        }
        catch (ApiClientException error)
        {
            await Alertas.ErrorAsync(this, error.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OpenHome()
    {
        var homePage = new InicioPage(sessionService, logger);
        NavigationPage.SetHasNavigationBar(homePage, false);
        Application.Current!.Windows[0].Page = new NavigationPage(homePage);
    }

    private void EmailCompleted(object? sender, EventArgs e) => PasswordEntry.Focus();

    private void PasswordCompleted(object? sender, EventArgs e) => LoginClicked(sender, e);

    private void FieldFocused(object? sender, FocusEventArgs e)
    {
        var border = sender == EmailEntry ? EmailFieldBorder : PasswordFieldBorder;
        border.Stroke = GetResourceBrush("FocusColor");
    }

    private void FieldUnfocused(object? sender, FocusEventArgs e)
    {
        var border = sender == EmailEntry ? EmailFieldBorder : PasswordFieldBorder;
        border.Stroke = GetResourceBrush("FieldBorderColor");
    }

    private Brush GetResourceBrush(string key) =>
        new SolidColorBrush((Color)Application.Current!.Resources[key]);

    private void SetBusy(bool value)
    {
        busy = value;
        LoginButton.IsEnabled = !value;
        EmailEntry.IsEnabled = !value;
        PasswordEntry.IsEnabled = !value;
        PasswordVisibilityButton.IsEnabled = !value;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
    }
}
