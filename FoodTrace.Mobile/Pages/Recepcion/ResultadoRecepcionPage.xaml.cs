using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Models.Recepcion;
using FoodTrace.Mobile.Services;

namespace FoodTrace.Mobile.Pages.Recepcion;

public partial class ResultadoRecepcionPage : ContentPage
{
    private readonly OrdenRecepcionDto orden;
    private readonly OrdenesRecepcionService ordenesService;
    private RecepcionDto recepcion;
    private bool busy;
    private bool navigating;

    public ResultadoRecepcionPage(
        OrdenRecepcionDto orden,
        RecepcionDto recepcion,
        OrdenesRecepcionService ordenesService)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.orden = orden;
        this.recepcion = recepcion;
        this.ordenesService = ordenesService;
        RenderizarResultado();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = VolverAListadoLimpioAsync();
        return true;
    }

    private void RenderizarResultado()
    {
        SupplierLabel.Text = $"{orden.NombreProveedor} · {orden.DestinoResumen}";
        OrderStatusLabel.Text = $"{orden.CodigoCorto} · Estado final: Cerrada";
        ReceiverMetaLabel.Text =
            $"Receptor: {recepcion.UsuarioReceptorId} · Fecha: {recepcion.FechaRecepcion.ToLocalTime():dd/MM/yyyy HH:mm}";
        ObservationLabel.Text = string.IsNullOrWhiteSpace(recepcion.Observacion)
            ? "Observación: Sin observación"
            : $"Observación: {recepcion.Observacion}";

        var conIngreso = recepcion.Detalles.Count(x => x.CantidadRecibida > 0);
        var conFaltante = recepcion.Detalles.Count(x => x.CantidadFaltante > 0);
        ImpactSummaryLabel.Text =
            $"Líneas con ingreso de inventario: {conIngreso} · Líneas con faltante registrado: {conFaltante}";

        ResultLinesContainer.Children.Clear();
        foreach (var detalle in recepcion.Detalles)
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

            var faltanteLabel = new Label { Text = $"Faltante guardado: {detalle.FaltanteTexto}" };
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
            ResultLinesContainer.Children.Add(card);
        }
    }

    private async void RecargarRecepcionClicked(object? sender, EventArgs e)
    {
        if (busy || navigating) return;
        SetBusy(true);
        try
        {
            recepcion = await ordenesService.ObtenerRecepcionAsync(orden.Id);
            RenderizarResultado();
        }
        catch (ApiClientException error)
        {
            await Alertas.ErrorAsync(this, error.Message);
        }
        catch (Exception)
        {
            await Alertas.ErrorAsync(this, "No se pudo consultar la recepción persistida. Intentá nuevamente.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void VolverAOrdenesClicked(object? sender, EventArgs e) =>
        await VolverAListadoLimpioAsync();

    private async void VolverAlMenuClicked(object? sender, EventArgs e)
    {
        if (busy || navigating) return;
        navigating = true;
        ReloadReceiptButton.IsEnabled = false;
        BackToOrdersButton.IsEnabled = false;
        BackToMenuButton.IsEnabled = false;
        await Navigation.PopToRootAsync();
    }

    private async Task VolverAListadoLimpioAsync()
    {
        if (busy || navigating) return;
        navigating = true;
        ReloadReceiptButton.IsEnabled = false;
        BackToOrdersButton.IsEnabled = false;
        BackToMenuButton.IsEnabled = false;

        var paginasIntermedias = Navigation.NavigationStack
            .Where(p => p is DetalleOrdenPage or ResumenRecepcionPage)
            .ToList();
        foreach (var pagina in paginasIntermedias)
            Navigation.RemovePage(pagina);

        await Navigation.PopAsync();
    }

    private void SetBusy(bool value)
    {
        busy = value;
        ReloadReceiptButton.IsEnabled = !value;
        BackToOrdersButton.IsEnabled = !value;
        BackToMenuButton.IsEnabled = !value;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
    }
}
