using System.Globalization;
using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Models.Recepcion;
using FoodTrace.Mobile.Services;

namespace FoodTrace.Mobile.Pages.Recepcion;

public partial class DetalleOrdenPage : ContentPage
{
    private readonly Guid ordenId;
    private readonly OrdenesRecepcionService ordenesService;
    private readonly List<LineaCapturaControles> controlesLinea = [];
    private OrdenRecepcionDto? ordenActual;
    private bool loaded;
    private bool busy;

    public DetalleOrdenPage(Guid ordenId, OrdenesRecepcionService ordenesService)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.ordenId = ordenId;
        this.ordenesService = ordenesService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (loaded) return;
        await CargarDetalleAsync();
    }

    private async Task CargarDetalleAsync()
    {
        if (busy) return;
        SetBusy(true);
        try
        {
            ordenActual = await ordenesService.ObtenerOrdenAsync(ordenId);
            loaded = true;
            SupplierLabel.Text = ordenActual.NombreProveedor;
            OrderCodeLabel.Text = $"{ordenActual.CodigoCorto} · Estado: {ordenActual.EstadoTexto}";
            DestinationLabel.Text = ordenActual.DestinoResumen;
            MetaLabel.Text = $"{ordenActual.FechaResumen} · {ordenActual.LineasResumen}";
            ConstruirControlesLineas(ordenActual);
            ActualizarResumenEnVivo();
        }
        catch (ApiClientException error)
        {
            await Alertas.ErrorAsync(this, error.Message);
        }
        catch (Exception)
        {
            await Alertas.ErrorAsync(this, "No se pudo consultar el detalle de la orden. Intentá nuevamente.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ConstruirControlesLineas(OrdenRecepcionDto orden)
    {
        LinesContainer.Children.Clear();
        controlesLinea.Clear();

        foreach (var linea in orden.Detalles)
        {
            var card = new Border();
            card.SetDynamicResource(VisualElement.StyleProperty, "PanelStyle");

            var stack = new VerticalStackLayout { Spacing = 8 };

            var productoLabel = new Label { Text = linea.NombreProducto };
            productoLabel.SetDynamicResource(VisualElement.StyleProperty, "SectionLabelStyle");

            var presentacionLabel = new Label { Text = linea.PresentacionResumen };
            presentacionLabel.SetDynamicResource(VisualElement.StyleProperty, "BodyTextStyle");

            var esperadoLabel = new Label { Text = linea.EsperadoResumen };
            esperadoLabel.SetDynamicResource(VisualElement.StyleProperty, "FieldLabelStyle");

            var inputTitleLabel = new Label
            {
                Text = $"Cantidad recibida (entero de 0 a {FormatoCantidad.Formatear(linea.CantidadPresentaciones)} presentaciones):"
            };
            inputTitleLabel.SetDynamicResource(VisualElement.StyleProperty, "FieldLabelStyle");

            var entry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = FormatoCantidad.Formatear(linea.CantidadPresentaciones),
                HorizontalTextAlignment = TextAlignment.Center
            };
            entry.SetDynamicResource(VisualElement.StyleProperty, "EntryTextStyle");

            var entryBorder = new Border { Padding = new Thickness(8, 0) };
            entryBorder.SetDynamicResource(VisualElement.StyleProperty, "FieldContainerStyle");
            entryBorder.Content = entry;

            var menosButton = new Button { Text = "−", WidthRequest = 48, HeightRequest = 48 };
            menosButton.SetDynamicResource(VisualElement.StyleProperty, "SecondaryButtonStyle");

            var masButton = new Button { Text = "+", WidthRequest = 48, HeightRequest = 48 };
            masButton.SetDynamicResource(VisualElement.StyleProperty, "SecondaryButtonStyle");

            var ceroButton = new Button { Text = "Recibir 0", HeightRequest = 42, Padding = new Thickness(12, 0) };
            ceroButton.SetDynamicResource(VisualElement.StyleProperty, "SecondaryButtonStyle");

            var completoButton = new Button
            {
                Text = $"Recibir todo ({FormatoCantidad.Formatear(linea.CantidadPresentaciones)})",
                HeightRequest = 42,
                Padding = new Thickness(12, 0)
            };
            completoButton.SetDynamicResource(VisualElement.StyleProperty, "SecondaryButtonStyle");

            var rowControles = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                ],
                ColumnSpacing = 10
            };
            rowControles.Add(menosButton, 0, 0);
            rowControles.Add(entryBorder, 1, 0);
            rowControles.Add(masButton, 2, 0);

            var rowAtajos = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                ],
                ColumnSpacing = 10
            };
            rowAtajos.Add(ceroButton, 0, 0);
            rowAtajos.Add(completoButton, 1, 0);

            var calculoLabel = new Label();
            calculoLabel.SetDynamicResource(VisualElement.StyleProperty, "BodyTextStyle");

            var inventarioLabel = new Label();
            inventarioLabel.SetDynamicResource(VisualElement.StyleProperty, "ConnectivityStyle");

            var control = new LineaCapturaControles(linea, entry, calculoLabel, inventarioLabel);
            controlesLinea.Add(control);

            entry.TextChanged += (_, _) =>
            {
                ActualizarEstadoLinea(control);
                ActualizarResumenEnVivo();
            };
            menosButton.Clicked += (_, _) => AjustarCantidad(control, -1);
            masButton.Clicked += (_, _) => AjustarCantidad(control, 1);
            ceroButton.Clicked += (_, _) => EstablecerCantidad(control, 0);
            completoButton.Clicked += (_, _) => EstablecerCantidad(control, (int)linea.CantidadPresentaciones);

            ActualizarEstadoLinea(control);

            stack.Children.Add(productoLabel);
            stack.Children.Add(presentacionLabel);
            stack.Children.Add(esperadoLabel);
            stack.Children.Add(inputTitleLabel);
            stack.Children.Add(rowControles);
            stack.Children.Add(rowAtajos);
            stack.Children.Add(calculoLabel);
            stack.Children.Add(inventarioLabel);

            card.Content = stack;
            LinesContainer.Children.Add(card);
        }
    }

    private void AjustarCantidad(LineaCapturaControles control, int delta)
    {
        var maximo = (int)control.Linea.CantidadPresentaciones;
        var actual = IntentarLeerEntero(control.Entry.Text, out var valor) ? valor : maximo;
        var siguiente = Math.Clamp(actual + delta, 0, maximo);
        EstablecerCantidad(control, siguiente);
    }

    private static void EstablecerCantidad(LineaCapturaControles control, int cantidad) =>
        control.Entry.Text = cantidad.ToString(CultureInfo.InvariantCulture);

    private static void ActualizarEstadoLinea(LineaCapturaControles control)
    {
        var linea = control.Linea;
        var maximo = (int)linea.CantidadPresentaciones;
        if (!IntentarLeerEntero(control.Entry.Text, out var recibidas) || recibidas < 0 || recibidas > maximo)
        {
            control.CalculoLabel.Text = $"Valor inválido: ingresá un número entero entre 0 y {maximo}.";
            control.CalculoLabel.SetDynamicResource(Label.TextColorProperty, "ErrorColor");
            control.InventarioLabel.Text = "Todas las líneas deben tener una cantidad válida antes de continuar.";
            return;
        }

        var faltantes = maximo - recibidas;
        var cantidadRecibida = recibidas * linea.CantidadBasePresentacion;
        var cantidadFaltante = linea.CantidadEsperada - cantidadRecibida;

        control.CalculoLabel.Text =
            $"Recibido: {recibidas} pres. ({FormatoCantidad.Formatear(cantidadRecibida)} {linea.UnidadBaseId}) · " +
            $"Faltante: {faltantes} pres. ({FormatoCantidad.Formatear(cantidadFaltante)} {linea.UnidadBaseId})";
        control.CalculoLabel.SetDynamicResource(Label.TextColorProperty, "MenuTextColor");

        control.InventarioLabel.Text = cantidadRecibida > 0
            ? $"Ingresará a inventario: {FormatoCantidad.Formatear(cantidadRecibida)} {linea.UnidadBaseId}"
            : "Recibido en 0: se registrará el faltante total y no generará ingreso de inventario.";
    }

    private void ActualizarResumenEnVivo()
    {
        var completas = 0;
        var conFaltante = 0;
        var enCero = 0;
        var invalidas = 0;

        foreach (var control in controlesLinea)
        {
            var maximo = (int)control.Linea.CantidadPresentaciones;
            if (!IntentarLeerEntero(control.Entry.Text, out var recibidas) || recibidas < 0 || recibidas > maximo)
            {
                invalidas++;
                continue;
            }

            if (recibidas == maximo) completas++;
            else if (recibidas == 0) enCero++;
            else conFaltante++;
        }

        LiveSummaryLabel.Text = invalidas > 0
            ? $"Hay {invalidas} línea(s) con cantidad inválida. Corregilas para continuar."
            : $"Completas: {completas} · Parciales con faltante: {conFaltante} · En cero (faltante total): {enCero}";
    }

    private async void RevisarResumenClicked(object? sender, EventArgs e)
    {
        if (busy || ordenActual is null) return;
        if (!IntentarConstruirEntrada(ordenActual, out var entrada, out var mensajeError))
        {
            await Alertas.ErrorAsync(this, mensajeError);
            return;
        }

        SetBusy(true);
        try
        {
            var vistaPrevia = ordenesService.ConstruirVistaPrevia(ordenActual, entrada);
            await Navigation.PushAsync(new ResumenRecepcionPage(ordenActual, entrada, vistaPrevia, ordenesService));
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

    private async void VolverClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        await Navigation.PopAsync();
    }

    private bool IntentarConstruirEntrada(
        OrdenRecepcionDto orden,
        out ConfirmarRecepcionDto entrada,
        out string mensajeError)
    {
        entrada = new ConfirmarRecepcionDto
        {
            Revision = orden.Revision,
            Observacion = string.IsNullOrWhiteSpace(ObservationEntry.Text) ? null : ObservationEntry.Text.Trim()
        };

        foreach (var control in controlesLinea)
        {
            var maximo = (int)control.Linea.CantidadPresentaciones;
            if (!IntentarLeerEntero(control.Entry.Text, out var recibidas))
            {
                mensajeError = $"Ingresá una cantidad entera válida para «{control.Linea.NombreProducto}».";
                return false;
            }

            if (recibidas < 0 || recibidas > maximo)
            {
                mensajeError = $"La cantidad recibida de «{control.Linea.NombreProducto}» debe estar entre 0 y {maximo}.";
                return false;
            }

            entrada.Detalles.Add(new DetalleRecepcionEntradaDto
            {
                DetalleOrdenRecepcionId = control.Linea.Id,
                CantidadPresentacionesRecibidas = recibidas
            });
        }

        mensajeError = string.Empty;
        return true;
    }

    private static bool IntentarLeerEntero(string? texto, out int valor) =>
        int.TryParse(texto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out valor);

    private void SetBusy(bool value)
    {
        busy = value;
        ReviewSummaryButton.IsEnabled = !value;
        BackButton.IsEnabled = !value;
        ObservationEntry.IsEnabled = !value;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
    }

    private sealed record LineaCapturaControles(
        DetalleOrdenRecepcionDto Linea,
        Entry Entry,
        Label CalculoLabel,
        Label InventarioLabel);
}
