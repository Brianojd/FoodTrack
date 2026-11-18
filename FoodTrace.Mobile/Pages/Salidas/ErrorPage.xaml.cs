namespace FoodTrace.Mobile.Pages.Salidas;

public partial class ErrorPage : ContentPage
{
    private bool navigating;

    public string MensajeError { get; }

    public ErrorPage(string mensajeError)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        MensajeError = string.IsNullOrWhiteSpace(mensajeError)
            ? "Hay lotes más antiguos que deben usarse primero. Busque el LOTE-445."
            : mensajeError;
        BindingContext = this;
    }

    private async void OnReintentarClicked(object? sender, EventArgs e)
    {
        if (navigating) return;
        navigating = true;
        await Navigation.PopAsync();
    }
}
