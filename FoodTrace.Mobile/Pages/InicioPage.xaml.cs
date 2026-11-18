using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Pages.Recepcion;
using FoodTrace.Mobile.Pages.Salidas;
using FoodTrace.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;

namespace FoodTrace.Mobile.Pages;

public partial class InicioPage : ContentPage
{
    private readonly SessionService sessionService;
    private readonly ILogger<LoginPage> logger;
    private readonly OrdenesRecepcionService ordenesService;
    private readonly SalidasCocinaService salidasService;
    private bool loggingOut;
    private bool navigating;

    public InicioPage(
        SessionService sessionService,
        ILogger<LoginPage> logger,
        OrdenesRecepcionService? ordenesService = null,
        SalidasCocinaService? salidasService = null)
    {
        InitializeComponent();
        this.sessionService = sessionService;
        this.logger = logger;
        this.ordenesService = ordenesService ?? new OrdenesRecepcionService(sessionService.ApiClient, sessionService);
        this.salidasService = salidasService ?? SalidasCocinaService.Shared;
        UserIdentityLabel.Text = sessionService.CurrentUser?.Email ?? "Cuenta";
        UpdateConnectivityLabel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Connectivity.ConnectivityChanged += ConnectivityChanged;
        UpdateConnectivityLabel();
    }

    protected override void OnDisappearing()
    {
        Connectivity.ConnectivityChanged -= ConnectivityChanged;
        base.OnDisappearing();
    }

    private void UserSelectorTapped(object? sender, TappedEventArgs e) =>
        UserMenuPopup.IsVisible = !UserMenuPopup.IsVisible;

    private async void IngresarMercaderiaClicked(object? sender, EventArgs e)
    {
        if (loggingOut || navigating) return;
        navigating = true;
        UserMenuPopup.IsVisible = false;
        try
        {
            await Navigation.PushAsync(new OrdenesRecepcionPage(ordenesService));
        }
        finally
        {
            navigating = false;
        }
    }

    private async void VerificarLoteClicked(object? sender, EventArgs e)
    {
        if (loggingOut || navigating) return;
        navigating = true;
        UserMenuPopup.IsVisible = false;
        try
        {
            await Navigation.PushAsync(new CapturaLotePage(salidasService));
        }
        finally
        {
            navigating = false;
        }
    }

    private Task ComingSoonClicked(object? sender, EventArgs e) =>
        Alertas.InformacionAsync(this, "Función disponible próximamente.");

    private async void LogoutClicked(object? sender, EventArgs e)
    {
        if (loggingOut || sender is not Button button) return;
        loggingOut = true;
        button.IsEnabled = false;
        UserMenuPopup.IsVisible = false;
        try
        {
            await sessionService.LogoutAsync();
            var loginPage = new LoginPage(sessionService, logger);
            NavigationPage.SetHasNavigationBar(loginPage, false);
            Application.Current!.Windows[0].Page = new NavigationPage(loginPage);
        }
        catch (Exception exception)
        {
            logger.LogError("Falló el cierre de sesión desde InicioPage: {Exception}", exception.ToString());
            await Alertas.ErrorAsync(this, "No se pudo cerrar la sesión. Intentá nuevamente.");
            button.IsEnabled = true;
            loggingOut = false;
        }
    }

    private void ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(UpdateConnectivityLabel);

    private void UpdateConnectivityLabel() =>
        ConnectivityLabel.Text = Connectivity.NetworkAccess == NetworkAccess.None
            ? "Sin conexión"
            : "Con conexión de red";
}
