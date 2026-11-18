using System.Globalization;
using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Models.Salidas;
using FoodTrace.Mobile.Services;

namespace FoodTrace.Mobile.Pages.Salidas;

public partial class ConsumoPage : ContentPage
{
    private readonly LoteInventarioDto lote;
    private readonly SalidasCocinaService salidasService;
    private bool busy;

    public ConsumoPage(LoteInventarioDto lote, SalidasCocinaService? salidasService = null)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.lote = lote;
        this.salidasService = salidasService ?? SalidasCocinaService.Shared;
        BindingContext = this.lote;
    }

    private async void OnConsumirTodoClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        var confirmado = await Alertas.ConfirmarAsync(
            this,
            $"¿Consumir todo el saldo disponible de {lote.CodigoLote} ({lote.SaldoFormateado}) y cerrar la caja?");
        if (!confirmado) return;

        SetBusy(true);
        try
        {
            var resultado = await salidasService.ConsumirTodoAsync(lote.CodigoLote);
            await Navigation.PushAsync(new ExitoPage(resultado));
        }
        catch (Exception ex)
        {
            await Alertas.ErrorAsync(this, ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnDescontarParcialClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        var texto = ConsumoParcialEntry.Text?.Trim().Replace(',', '.') ?? string.Empty;
        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0)
        {
            await Alertas.ErrorAsync(this, "Ingresá una cantidad mayor que cero para descontar.");
            return;
        }

        if (cantidad > lote.SaldoDisponible)
        {
            await Alertas.ErrorAsync(
                this,
                $"La cantidad ingresada supera el saldo disponible del lote ({lote.SaldoFormateado}).");
            return;
        }

        var confirmado = await Alertas.ConfirmarAsync(
            this,
            $"¿Descontar {cantidad:0.######} {lote.UnidadBaseId} del lote {lote.CodigoLote}?");
        if (!confirmado) return;

        SetBusy(true);
        try
        {
            var resultado = await salidasService.ConsumirParcialAsync(lote.CodigoLote, cantidad);
            await Navigation.PushAsync(new ExitoPage(resultado));
        }
        catch (Exception ex)
        {
            await Alertas.ErrorAsync(this, ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnVolverClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        await Navigation.PopAsync();
    }

    private void SetBusy(bool value)
    {
        busy = value;
        ConsumirTodoButton.IsEnabled = !value;
        DescontarButton.IsEnabled = !value;
        CancelarButton.IsEnabled = !value;
        ConsumoParcialEntry.IsEnabled = !value;
    }
}
