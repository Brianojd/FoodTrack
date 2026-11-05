using FoodTrace.API.Application.Interfaces;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Dtos.Autenticacion;
using FoodTrace.API.Dtos.Empresas;
using FoodTrace.API.Dtos.Establecimientos;
using FoodTrace.API.Dtos.Productos;
using FoodTrace.API.Dtos.Proveedores;
using FoodTrace.API.Dtos.Comunes;
using FoodTrace.API.Dtos.Usuarios;
using FoodTrace.API.Dtos.Recetas;
using FoodTrace.API.Dtos.Produccion;
using FoodTrace.API.Dtos.Compras;
using FoodTrace.API.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.API.Controllers;

[ApiController, Route("api"), Authorize]
public sealed class FlujoController(SesionEmpresa sesion, IServicioFlujo flujo,
    IServicioCatalogos catalogos, IServicioUsuarios usuarios) : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UsuarioActualDto> UsuarioActual() => Ok(new UsuarioActualDto
    { Id = sesion.UsuarioId, EmpresaId = sesion.EmpresaId, Correo = User.Identity!.Name, Roles = Roles.Todos.Where(User.IsInRole).ToArray() });

    [HttpGet("catalogs")]
    public async Task<ActionResult<CatalogosDto>> ObtenerCatalogosAsync(CancellationToken cancelacion = default) => Ok(await catalogos.ObtenerCatalogosAsync(cancelacion));

    [HttpPut("company"), Authorize(Roles = "admin")]
    public async Task<ActionResult<EmpresaDto>> ActualizarEmpresaAsync(ActualizarEmpresaDto entrada, CancellationToken cancelacion = default) => Ok(await catalogos.ActualizarEmpresaAsync(entrada, cancelacion));

    [HttpPost("establishments"), Authorize(Roles = "admin")]
    public async Task<ActionResult<EntidadConNombreDto>> CrearEstablecimientoAsync(CrearEstablecimientoDto entrada, CancellationToken cancelacion = default) => Ok(await catalogos.CrearEstablecimientoAsync(entrada, cancelacion));

    [HttpPost("products"), Authorize(Roles = "admin")]
    public async Task<ActionResult<ProductoDto>> CrearProductoAsync(CrearProductoDto entrada, CancellationToken cancelacion = default) => Ok(await catalogos.CrearProductoAsync(entrada, cancelacion));

    [HttpPost("suppliers"), Authorize(Roles = "admin")]
    public async Task<ActionResult<EntidadConNombreDto>> CrearProveedorAsync(CrearProveedorDto entrada, CancellationToken cancelacion = default) => Ok(await catalogos.CrearProveedorAsync(entrada, cancelacion));

    [HttpPost("packs"), Authorize(Roles = "admin")]
    public async Task<ActionResult<PresentacionProveedorDto>> CrearPresentacionAsync(CrearPresentacionProveedorDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.CrearPresentacionAsync(entrada, cancelacion));

    [HttpGet("users"), Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UsuarioDto>>> ObtenerUsuariosAsync(CancellationToken cancelacion = default) => Ok(await usuarios.ObtenerUsuariosAsync(cancelacion));

    [HttpPost("users"), Authorize(Roles = "admin")]
    public async Task<ActionResult<UsuarioDto>> CrearUsuarioAsync(CrearUsuarioDto entrada, CancellationToken cancelacion = default) => Ok(await usuarios.CrearUsuarioAsync(entrada, cancelacion));

    [HttpPut("users/{id}"), Authorize(Roles = "admin")]
    public async Task<ActionResult<UsuarioDto>> ActualizarMembresiaAsync(string id, ActualizarMembresiaDto entrada, CancellationToken cancelacion = default) => Ok(await usuarios.ActualizarMembresiaAsync(id, entrada, cancelacion));

    [HttpGet("recipes")]
    public async Task<ActionResult<List<VersionRecetaDto>>> ObtenerRecetasAsync(CancellationToken cancelacion = default) => Ok(await flujo.ObtenerRecetasAsync(cancelacion));
    [HttpPost("recipes"), Authorize(Roles = "planner")]
    public async Task<ActionResult<VersionRecetaDto>> CrearRecetaAsync(CrearRecetaDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.CrearRecetaAsync(entrada, cancelacion));
    [HttpPut("recipes/{id:guid}"), Authorize(Roles = "planner")]
    public async Task<ActionResult<VersionRecetaDto>> ActualizarRecetaAsync(Guid id, ActualizarRecetaDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.ActualizarRecetaAsync(id, entrada, cancelacion));
    [HttpPost("recipes/{id:guid}/publish"), Authorize(Roles = "planner")]
    public async Task<ActionResult<VersionRecetaDto>> PublicarRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.PublicarRecetaAsync(id, entrada, cancelacion));
    [HttpPost("recipes/{id:guid}/versions"), Authorize(Roles = "planner")]
    public async Task<ActionResult<VersionRecetaDto>> CrearVersionRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.CrearVersionRecetaAsync(id, entrada, cancelacion));

    [HttpGet("production")]
    public async Task<ActionResult<List<OrdenProduccionDto>>> ObtenerProduccionAsync(CancellationToken cancelacion = default)
    {
        var (alcance, propietarioId) = ResolverAlcance();
        return Ok(await flujo.ObtenerProduccionAsync(alcance, propietarioId, cancelacion));
    }
    [HttpGet("needs")]
    public async Task<ActionResult<List<NecesidadIngredienteDto>>> ObtenerNecesidadesAsync(CancellationToken cancelacion = default)
    {
        var (alcance, propietarioId) = ResolverAlcance();
        return Ok(await flujo.ObtenerNecesidadesAsync(alcance, propietarioId, cancelacion));
    }
    [HttpPost("production"), Authorize(Roles = "planner")]
    public async Task<ActionResult<OrdenProduccionDto>> PlanificarProduccionAsync(PlanificarProduccionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.PlanificarProduccionAsync(entrada, cancelacion));

    [HttpGet("purchases")]
    public async Task<ActionResult<List<PedidoCompraDto>>> ObtenerPedidosCompraAsync(CancellationToken cancelacion = default)
    {
        var (alcance, propietarioId) = ResolverAlcance();
        return Ok(await flujo.ObtenerPedidosCompraAsync(alcance, propietarioId, cancelacion));
    }
    [HttpPost("purchases"), Authorize(Roles = "buyer,admin")]
    public async Task<ActionResult<PedidoCompraDto>> CrearPedidoCompraAsync(CrearPedidoCompraDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.CrearPedidoCompraAsync(entrada, cancelacion));
    [HttpPost("purchases/{id:guid}/lines"), Authorize]
    public async Task<ActionResult<PedidoCompraDto>> AgregarDetalleCompraAsync(Guid id, AgregarDetalleCompraDto entrada, CancellationToken cancelacion = default)
    {
        var (alcance, propietarioId) = ResolverAlcance();
        if (!await flujo.PedidoEsVisibleAsync(id, alcance, propietarioId, cancelacion))
            return NotFound();
        if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.Comprador))
            return Forbid();
        return Ok(await flujo.AgregarDetalleCompraAsync(id, entrada, cancelacion));
    }
    [HttpPut("purchases/{id:guid}/lines/{lineId:guid}"), Authorize(Roles = "buyer,admin")]
    public async Task<ActionResult<PedidoCompraDto>> ActualizarPresentacionesAsync(Guid id, [FromRoute(Name = "lineId")] Guid detalleId, ActualizarCantidadPresentacionesDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.ActualizarPresentacionesAsync(id, detalleId, entrada, cancelacion));
    [HttpPost("purchases/{id:guid}/lines/{lineId:guid}/remove"), Authorize(Roles = "buyer,admin")]
    public async Task<ActionResult<PedidoCompraDto>> RetirarDetalleCompraAsync(Guid id, [FromRoute(Name = "lineId")] Guid detalleId, RevisionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.RetirarDetalleCompraAsync(id, detalleId, entrada, cancelacion));
    [HttpPost("purchases/{id:guid}/submit"), Authorize(Roles = "buyer,admin")]
    public async Task<ActionResult<PedidoCompraDto>> EnviarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.EnviarPedidoCompraAsync(id, entrada, cancelacion));
    [HttpPost("purchases/{id:guid}/approve"), Authorize(Roles = "admin")]
    public async Task<ActionResult<PedidoCompraDto>> AprobarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.AprobarPedidoCompraAsync(id, entrada, cancelacion));
    [HttpPost("purchases/{id:guid}/reject"), Authorize(Roles = "admin")]
    public async Task<ActionResult<PedidoCompraDto>> RechazarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => Ok(await flujo.RechazarPedidoCompraAsync(id, entrada, cancelacion));

    private (AlcanceListado Alcance, string PropietarioId) ResolverAlcance()
    {
        if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Comprador))
            return (AlcanceListado.Global, sesion.UsuarioId);
        return (AlcanceListado.Propietario, sesion.UsuarioId);
    }
}
