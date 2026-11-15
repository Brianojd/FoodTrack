using FoodTrace.Components.Layout;
using FoodTrace.Dtos.Compras;
using FoodTrace.Models.Compras;
using Microsoft.AspNetCore.Components;

namespace FoodTrace.Components.Pages.OrdenesRecepcion;

public partial class OrdenesRecepcion
{
    [Parameter] public Guid? Id { get; set; }
    private readonly ModeloOrdenRecepcion modelo = new();
    private bool cargada;

    protected override Task ActualizarAsync() => !Sesion.TieneRol("admin") ? Task.CompletedTask : EjecutarAsync(CargarAsync, esConsulta: true);

    private async Task CargarAsync(CancellationToken cancelacion)
    {
        var ordenes = await OrdenesApi.ObtenerOrdenesRecepcionAsync(cancelacion);
        if (!Vigente) return;
        modelo.Reemplazar(ordenes);
        cargada = true;
        if (Id is not { } id) return;
        var detalle = await OrdenesApi.ObtenerOrdenRecepcionAsync(id, cancelacion);
        if (Vigente) modelo.Seleccionar(detalle);
    }

    private Task ConsultarAsync(OrdenRecepcionDto orden) => EjecutarAsync(async cancelacion =>
    {
        var detalle = await OrdenesApi.ObtenerOrdenRecepcionAsync(orden.Id, cancelacion);
        if (Vigente) modelo.Seleccionar(detalle);
    }, esConsulta: true);
}
