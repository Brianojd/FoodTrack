using FoodTrace.Mobile.Models.Salidas;

namespace FoodTrace.Mobile.Pages.Salidas;

public partial class ExitoPage : ContentPage
{
    private bool navigating;

    public ExitoPage(SalidaCocinaResultadoDto resultado)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = resultado;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = VolverInicioAsync();
        return true;
    }

    private async void OnVolverInicioClicked(object? sender, EventArgs e) =>
        await VolverInicioAsync();

    private async Task VolverInicioAsync()
    {
        if (navigating) return;
        navigating = true;
        await Navigation.PopToRootAsync();
    }
}
