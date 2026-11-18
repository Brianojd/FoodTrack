using FoodTrace.Mobile.Helpers;
using FoodTrace.Mobile.Models.Recepcion;
using FoodTrace.Mobile.Services;

namespace FoodTrace.Mobile.Pages.Recepcion;

public partial class OrdenesRecepcionPage : ContentPage
{
    private readonly OrdenesRecepcionService ordenesService;
    private bool busy;

    public OrdenesRecepcionPage(OrdenesRecepcionService ordenesService)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        this.ordenesService = ordenesService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarOrdenesAsync();
    }

    private async void ActualizarClicked(object? sender, EventArgs e) =>
        await CargarOrdenesAsync();

    private async void VolverClicked(object? sender, EventArgs e)
    {
        if (busy) return;
        await Navigation.PopAsync();
    }

    private async Task CargarOrdenesAsync()
    {
        if (busy) return;
        SetBusy(true);
        try
        {
            var ordenes = await ordenesService.ObtenerOrdenesPendientesAsync();
            ErrorStateBorder.IsVisible = false;
            RenderizarOrdenes(ordenes);
        }
        catch (ApiClientException error)
        {
            OrdersContainer.Children.Clear();
            EmptyStateBorder.IsVisible = false;
            ErrorStateLabel.Text = error.Message;
            ErrorStateBorder.IsVisible = true;
            await Alertas.ErrorAsync(this, error.Message);
        }
        catch (Exception)
        {
            const string mensaje = "No se pudieron cargar las órdenes de recepción. Intentá nuevamente.";
            OrdersContainer.Children.Clear();
            EmptyStateBorder.IsVisible = false;
            ErrorStateLabel.Text = mensaje;
            ErrorStateBorder.IsVisible = true;
            await Alertas.ErrorAsync(this, mensaje);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RenderizarOrdenes(List<OrdenRecepcionDto> ordenes)
    {
        OrdersContainer.Children.Clear();
        EmptyStateBorder.IsVisible = ordenes.Count == 0;

        foreach (var orden in ordenes)
        {
            var card = new Border();
            card.SetDynamicResource(VisualElement.StyleProperty, "PanelStyle");

            var layout = new VerticalStackLayout { Spacing = 8 };

            var headerGrid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                ],
                ColumnSpacing = 8
            };

            var proveedorLabel = new Label { Text = orden.NombreProveedor };
            proveedorLabel.SetDynamicResource(VisualElement.StyleProperty, "SectionLabelStyle");

            var estadoLabel = new Label
            {
                Text = orden.EstadoTexto,
                VerticalOptions = LayoutOptions.Center
            };
            estadoLabel.SetDynamicResource(VisualElement.StyleProperty, "FieldLabelStyle");
            estadoLabel.SetDynamicResource(Label.TextColorProperty, "LoginPrimaryColor");

            headerGrid.Add(proveedorLabel, 0, 0);
            headerGrid.Add(estadoLabel, 1, 0);

            var codigoLabel = new Label { Text = orden.CodigoCorto };
            codigoLabel.SetDynamicResource(VisualElement.StyleProperty, "FieldLabelStyle");

            var destinoLabel = new Label { Text = orden.DestinoResumen };
            destinoLabel.SetDynamicResource(VisualElement.StyleProperty, "BodyTextStyle");

            var fechaLabel = new Label { Text = $"{orden.FechaResumen} · {orden.LineasResumen}" };
            fechaLabel.SetDynamicResource(VisualElement.StyleProperty, "ConnectivityStyle");

            var abrirButton = new Button
            {
                Text = "Abrir orden y cargar recepción",
                Margin = new Thickness(0, 6, 0, 0),
                CommandParameter = orden.Id
            };
            abrirButton.SetDynamicResource(VisualElement.StyleProperty, "LoginPrimaryButtonStyle");
            abrirButton.Clicked += AbrirOrdenClicked;

            layout.Children.Add(headerGrid);
            layout.Children.Add(codigoLabel);
            layout.Children.Add(destinoLabel);
            layout.Children.Add(fechaLabel);
            layout.Children.Add(abrirButton);

            card.Content = layout;
            OrdersContainer.Children.Add(card);
        }
    }

    private async void AbrirOrdenClicked(object? sender, EventArgs e)
    {
        if (busy || sender is not Button { CommandParameter: Guid ordenId }) return;
        SetBusy(true);
        try
        {
            await Navigation.PushAsync(new DetalleOrdenPage(ordenId, ordenesService));
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        busy = value;
        RefreshButton.IsEnabled = !value;
        RetryButton.IsEnabled = !value;
        BackButton.IsEnabled = !value;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
    }
}
