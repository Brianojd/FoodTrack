using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Dtos.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.API.Controllers;

[ApiController, Route("api/receipt-orders"), Authorize(Roles = "admin,operator")]
public sealed class OrdenesRecepcionController(IServicioFlujo flujo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OrdenRecepcionDto>>> ObtenerOrdenesAsync(CancellationToken cancelacion = default) =>
        Ok(await flujo.ObtenerOrdenesRecepcionAsync(cancelacion));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrdenRecepcionDto>> ObtenerOrdenAsync(Guid id, CancellationToken cancelacion = default) =>
        Ok(await flujo.ObtenerOrdenRecepcionAsync(id, cancelacion));

    [HttpPost("{id:guid}/receipt")]
    public async Task<ActionResult<RecepcionDto>> ConfirmarRecepcionAsync(Guid id, ConfirmarRecepcionDto entrada, CancellationToken cancelacion = default) =>
        Ok(await flujo.ConfirmarRecepcionAsync(id, entrada, cancelacion));

    [HttpGet("{id:guid}/receipt")]
    public async Task<ActionResult<RecepcionDto>> ObtenerRecepcionAsync(Guid id, CancellationToken cancelacion = default) =>
        Ok(await flujo.ObtenerRecepcionAsync(id, cancelacion));
}
