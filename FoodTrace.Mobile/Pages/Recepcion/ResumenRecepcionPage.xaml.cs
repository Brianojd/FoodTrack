using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Models.Recepcion;
using FoodTrace.Mobile.Services;

namespace FoodTrace.Mobile.Pages.Recepcion;

public partial class ResumenRecepcionPage : ContentPage
{
    private readonly OrdenRecepcionDto orden;
    private readonly ConfirmarRecepcionDto entrada;
    private readonly RecepcionDto vistaPrevia;
    private readonly OrdenesRecepcionService ordenesService;
    private bool busy;

    public ResumenRecepcionPage(
        OrdenRecepcionDto orden,
        ConfirmarRecepcionDto entrada,
        RecepcionDto vistaPrevia,
        OrdenesRecepcionService ordenesService)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.orden = orden;
        this.entrada = entrada;
        this.vistaPrevia = vistaPrevia;
        this.ordenesService = ordenesService;
        RenderizarResumen();
    }

    private void RenderizarResumen()
    {
        SupplierLabel.Text = orden.NombreProveedor;
        OrderCodeLabel.Text = orden.CodigoCorto;
        DestinationLabel.Text = orden.DestinoResumen;
        ObservationLabel.Text = string.IsNullOrWhiteSpace(vistaPrevia.Observacion)
            ? "Observación: Sin observación"
            : $"Observación: {vistaPrevia.Observacion}";

        var completas = vistaPrevia.Detalles.Count(x => x.CantidadPresentacionesFaltantes == 0);
        var conFaltante = vistaPrevia.Detalles.Count(x => x.CantidadPresentacionesFaltantes > 0 && x.CantidadPresentacionesRecibidas > 0);
        var enCero = vistaPrevia.Detalles.Count(x => x.CantidadPresentacionesRecibidas == 0);
        var ingresos = vistaPrevia.Detalles.Count(x => x.CantidadRecibida > 0);

        TotalsLabel.Text =
            $"Líneas completas: {completas} · Parciales: {conFaltante} · En cero: {enCero} · " +
            $"Ingresos de inventario a generar: {ingresos}";

        SummaryLinesContainer.Children.Clear();
        foreach (var detalle in vistaPrevia.Detalles)
        {
            var card = new Border();
            card.SetDynamicResource(VisualElement.StyleProperty, "PanelStyle");

            var stack = new VerticalStackLayout { Spacing = 6 };

            var productoLabel = new Label { Text = $"{detalle.NombreProducto} · {detalle.NombrePresentacion}" };
            productoLabel.SetDynamicResource(VisualElement.StyleProperty, "SectionLabelStyle");

            var esperadoLabel = new Label { Text = $"Esperado: {detalle.EsperadoTexto}" };
            esperadoLabel.SetDynamicResource(VisualElement.StyleProperty, "BodyTextStyle");

            var recibidoLabel = new Label { Text = $"Recibido: {detalle.RecibidoTexto}" };
            recibidoLabel.SetDynamicResource(VisualElement.StyleProperty, "BodyTextStyle");

            var faltanteLabel = new Label { Text = $"Faltante: {detalle.FaltanteTexto}" };
            faltanteLabel.SetDynamicResource(VisualElement.StyleProperty, "FieldLabelStyle");
            faltanteLabel.SetDynamicResource(
                Label.TextColorProperty,
                detalle.CantidadFaltante > 0 ? "ErrorColor" : "SuccessColor");

            var inventarioLabel = new Label { Text = detalle.IngresoInventarioTexto };
            inventarioLabel.SetDynamicResource(VisualElement.StyleProperty, "ConnectivityStyle");

            stack.Children.Add(productoLabel);
            stack.Children.Add(esperadoLabel);
            stack.Children.Add(recibidoLabel);
            stack.Children.Add(faltanteLabel);
            stack.Children.Add(inventarioLabel);

            card.Content = stack;
            SummaryLinesContainer.Children.Add(card);
        }
    }

    private async void ConfirmarClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        var conFaltantes = vistaPrevia.Detalles.Count(x => x.CantidadFaltante > 0);
        var avisoFaltantes = conFaltantes > 0
            ? $"Se registrarán faltantes en {conFaltantes} línea(s). "
            : "No se registran faltantes. ";

        var confirmado = await Alertas.ConfirmarAsync(
            this,
            $"¿Confirmar la recepción de {orden.CodigoCorto}? {avisoFaltantes}La orden quedará cerrada definitivamente y solo ingresará al inventario la cantidad recibida.");
        if (!confirmado) return;

        SetBusy(true);
        try
        {
            var resultado = await ordenesService.ConfirmarRecepcionAsync(orden.Id, orden, entrada);
            await Navigation.PushAsync(new ResultadoRecepcionPage(orden, resultado, ordenesService));
        }
        catch (ApiClientException error)
        {
            await Alertas.ErrorAsync(this, error.Message);
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

    private async void EditarClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        await Navigation.PopAsync();
    }

    private void SetBusy(bool value)
    {
        busy = value;
        ConfirmButton.IsEnabled = !value;
        EditButton.IsEnabled = !value;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
    }
}
