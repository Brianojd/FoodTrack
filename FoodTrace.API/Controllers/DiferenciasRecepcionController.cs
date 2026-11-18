using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Dtos.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.API.Controllers;

[ApiController, Route("api/receipt-differences"), Authorize(Roles = "admin")]
public sealed class DiferenciasRecepcionController(IServicioFlujo flujo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DiferenciasRecepcionPaginadasDto>> ObtenerAsync([FromQuery] DiferenciasRecepcionQuery consulta, CancellationToken cancelacion = default) =>
        Ok(await flujo.ObtenerDiferenciasRecepcionAsync(consulta, cancelacion));
}
