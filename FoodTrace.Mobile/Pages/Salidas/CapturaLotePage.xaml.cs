using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Services;

namespace FoodTrace.Mobile.Pages.Salidas;

public partial class CapturaLotePage : ContentPage
{
    private readonly SalidasCocinaService salidasService;
    private bool busy;

    public CapturaLotePage(SalidasCocinaService? salidasService = null)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.salidasService = salidasService ?? SalidasCocinaService.Shared;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActualizarResumenLotes();
    }

    private async void OnEscanearClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        await Alertas.InformacionAsync(
            this,
            "El escaneo por cámara o lector láser estará disponible próximamente. Ingresá el código de lote manualmente.");
    }

    private async void OnValidarManualClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        var codigo = LoteManualEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            await Alertas.ErrorAsync(this, "Ingresá un número de lote para validar la rotación.");
            return;
        }

        SetBusy(true);
        try
        {
            var resultado = await salidasService.ValidarLoteManualAsync(codigo);
            if (!resultado.EsValido || resultado.Lote is null)
            {
                await Navigation.PushAsync(new ErrorPage(resultado.MensajeError));
                return;
            }

            await Navigation.PushAsync(new ConsumoPage(resultado.Lote, salidasService));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnVolverMenuClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        await Navigation.PopAsync();
    }

    private void ActualizarResumenLotes()
    {
        var activos = salidasService.ObtenerLotesActivos();
        if (activos.Count == 0)
        {
            salidasService.RestaurarLotesPrueba();
            activos = salidasService.ObtenerLotesActivos();
        }

        LotesDisponiblesLabel.Text = string.Join(
            Environment.NewLine,
            activos.Select(x => $"• {x.CodigoLote} — {x.NombreProducto} ({x.SaldoFormateado})"));
    }

    private void SetBusy(bool value)
    {
        busy = value;
        EscanearButton.IsEnabled = !value;
        ValidarManualButton.IsEnabled = !value;
        VolverInicioButton.IsEnabled = !value;
        LoteManualEntry.IsEnabled = !value;
    }
}
