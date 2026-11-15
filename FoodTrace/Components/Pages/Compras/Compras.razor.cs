using FoodTrace.Components.Dialogos;
using FoodTrace.Components.Dialogos.Compras;
using FoodTrace.Components.Layout;
using FoodTrace.Dtos.Compras;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Models.Compras;
using FoodTrace.Services.Estado;
using FoodTrace.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FoodTrace.Components.Pages.Compras;

public partial class Compras
{
    [Inject] protected IServicioCompras ComprasApi { get; set; } = null!;
    [Inject] protected IServicioProduccion ProduccionApi { get; set; } = null!;
    protected EstadoCompras Estado => Formularios.Compras;
    private static readonly string[] RolesCompra = ["admin"];
    protected bool PuedeComprar => Sesion.TieneRol("admin");
    private readonly ControlCargaModulo carga = new();
    private PedidoCompraDto? revisionServidor;
    private bool HayDetalleSinGuardar => Estado.Detalle.TieneContenido || Estado.EdicionCantidad is { } edicion && edicion.Cantidad != edicion.CantidadOriginal;
    private MudButton? botonActualizar, botonCrear, botonAgregar, botonEnviar;
    private ModeloPropuestaCompra? propuesta;
    private Func<MudButton?>? destinoFoco;
    protected override Task ActualizarAsync() => EjecutarAsync(ct => CargarAsync(ct, false), esConsulta: true);

    private bool ModuloHabilitado => Sesion.TieneRol("admin");

    private async Task CargarAsync(CancellationToken ct, bool recargar)
    {
        if (!ModuloHabilitado) return;
        var id = Estado.Seleccionado?.Id;
        var turno = carga.Iniciar(ct);
        var catalogos = Catalogos.ObtenerCatalogosAsync(turno.Token);
        var pedidos = ComprasApi.ObtenerPedidosCompraAsync(turno.Token);
        var necesidades = ProduccionApi.ObtenerNecesidadesAsync(turno.Token);
        var produccion = ProduccionApi.ObtenerProduccionAsync(turno.Token);
        await Task.WhenAll(catalogos, pedidos, necesidades, produccion);
        if (!Vigente || !carga.EsActual(turno.Generacion)) return;
        var actual = (await pedidos).FirstOrDefault(x => x.Id == id);
        if (recargar && actual is null) { MostrarError("El pedido ya no está disponible. Sus formularios se conservan."); return; }
        Estado.Catalogos = await catalogos; Estado.Pedidos = await pedidos;
        Estado.Necesidades = await necesidades; Estado.Produccion = await produccion;
        Estado.NecesidadesDesactualizadas = false;
        if (propuesta is not null) propuesta = ModeloPropuestaCompra.Crear(Estado.Necesidades, Estado.Catalogos);
        if (recargar) { Estado.Seleccionar(actual!); revisionServidor = null; }
        AjustarPaginaPedidos();
    }

    protected async Task SeleccionarAsync(PedidoCompraDto pedido)
    {
        if (Ocupado || Estado.Seleccionado?.Id == pedido.Id) return;
        if (Estado.TieneCambios && !await ConfirmarAsync("Cambiar pedido", "Se descartarán los formularios sin guardar del pedido actual. ¿Desea continuar?")) return;
        carga.Cancelar(); Estado.Seleccionar(pedido); revisionServidor = null;
    }
    private async Task RecargarAsync()
    {
        if (Ocupado || Estado.Seleccionado is null || !await ConfirmarAsync("Descartar y recargar", "Se descartarán los formularios y revisiones originales solo si todas las consultas finalizan correctamente. No se repetirá ninguna escritura.")) return;
        await EjecutarAsync(ct => CargarAsync(ct, true), esConsulta: true);
    }
    private Task RevisarAsync()
    {
        var id = Estado.Seleccionado?.Id;
        return EjecutarAsync(async ct =>
        {
            var turno = carga.Iniciar(ct);
            var pedidos = await ComprasApi.ObtenerPedidosCompraAsync(turno.Token);
            if (!Vigente || !carga.EsActual(turno.Generacion)) return;
            revisionServidor = pedidos.FirstOrDefault(x => x.Id == id);
            if (revisionServidor is null) MostrarError("El pedido no está disponible. Su formulario se conserva.");
        }, esConsulta: true);
    }
    private async Task CrearAsync()
    {
        if (Ocupado || !PuedeComprar || Estado.Catalogos is null) return;
        if (HayDetalleSinGuardar && !await ConfirmarAsync("Crear otro pedido", "Al crear el nuevo pedido se descartarán los otros formularios sin guardar. ¿Desea continuar?")) return;
        var parametros = new DialogParameters<DialogoCrearPedido>
        {
            { x => x.Catalogos, Estado.Catalogos }, { x => x.ProveedorInicialId, Estado.Nuevo.ProveedorId },
            { x => x.EstablecimientoInicialId, Estado.Nuevo.EstablecimientoId }
        };
        await AbrirOperacionAsync("Crear pedido de compra", parametros, "buyer", pedido => Estado.Seleccionar(pedido), foco: () => botonCrear, rolesCualquiera: RolesCompra);
    }
    protected Task AgregarAsync()
    {
        if (!PuedeEditar() || Estado.Catalogos is null || Estado.NecesidadesDesactualizadas) return Task.CompletedTask;
        var parametros = new DialogParameters<DialogoAgregarDetallePedido>
        {
            { x => x.Pedido, Estado.Seleccionado! }, { x => x.Catalogos, Estado.Catalogos },
            { x => x.Necesidades, Estado.Necesidades }, { x => x.Produccion, Estado.Produccion },
            { x => x.ModeloInicial, Estado.Detalle.Copiar() }, { x => x.NecesidadesDesactualizadas, Estado.NecesidadesDesactualizadas },
            { x => x.NecesidadesObsoletas, EventCallback.Factory.Create(this, () => { if (Vigente) Estado.NecesidadesDesactualizadas = true; }) }
        };
        return AbrirOperacionAsync("Agregar detalle al pedido", parametros, "buyer", _ => Estado.Detalle = new(),
            modificaNecesidades: true, ancho: MaxWidth.Large, rolesCualquiera: RolesCompra, foco: () => PuedeEditar() && !Estado.NecesidadesDesactualizadas ? botonAgregar : botonActualizar);
    }
    private bool PuedeEditar() => PuedeComprar && Estado.Seleccionado?.Estado == 0;
    protected Task EditarCantidadAsync(DetallePedidoCompraDto detalle)
    {
        if (Ocupado || !PuedeEditar() || Estado.Catalogos is null) return Task.CompletedTask;
        var pedido = Estado.Seleccionado!;
        if (InstantaneaPedido.DetalleActivo(pedido, detalle.Id) is null) { MostrarError("El detalle no pertenece al borrador seleccionado o está retirado."); return Task.CompletedTask; }
        var parametros = new DialogParameters<DialogoCantidadPresentaciones>
        {
            { x => x.Pedido, pedido }, { x => x.DetalleId, detalle.Id }, { x => x.Catalogos, Estado.Catalogos }
        };
        return AbrirOperacionAsync("Cambiar cantidad de presentaciones", parametros, "buyer", _ => Estado.EdicionCantidad = null, rolesCualquiera: RolesCompra);
    }
    private async Task DescartarDetalleAsync()
    {
        if (!Ocupado && await ConfirmarAsync("Descartar borradores locales", "Se descartarán los borradores anteriores de detalle, cantidad y motivo, sin enviar escrituras."))
        { Estado.Detalle = new(); Estado.EdicionCantidad = null; }
    }
    protected Task RetirarAsync(DetallePedidoCompraDto detalle)
    {
        if (Ocupado || !PuedeEditar() || Estado.Catalogos is null) return Task.CompletedTask;
        var pedido = Estado.Seleccionado!;
        if (InstantaneaPedido.DetalleActivo(pedido, detalle.Id) is null) { MostrarError("El detalle no pertenece al borrador seleccionado o está retirado."); return Task.CompletedTask; }
        var parametros = new DialogParameters<DialogoRetirarDetallePedido>
        {
            { x => x.Pedido, pedido }, { x => x.DetalleId, detalle.Id }, { x => x.Catalogos, Estado.Catalogos }
        };
        return AbrirOperacionAsync("Retirar detalle conservando historial", parametros, "buyer", _ =>
        {
            if (Estado.EdicionCantidad?.DetalleId == detalle.Id) Estado.EdicionCantidad = null;
        }, modificaNecesidades: true, rolesCualquiera: RolesCompra);
    }
    private Task EnviarAsync()
    {
        if (Ocupado || !PuedeEditar() || Estado.Catalogos is null || HayDetalleSinGuardar || Estado.DestinoSinGuardar || Estado.Seleccionado!.EstablecimientoId is null || !Estado.Seleccionado.Detalles.Any()) return Task.CompletedTask;
        var parametros = new DialogParameters<DialogoEnviarPedido> { { x => x.Pedido, Estado.Seleccionado }, { x => x.Catalogos, Estado.Catalogos } };
        return AbrirOperacionAsync("Enviar pedido para decisión", parametros, "buyer", foco: () => PuedeEditar() ? botonEnviar : botonActualizar, rolesCualquiera: RolesCompra);
    }
    private Task GuardarDestinoAsync()
    {
        var pedido = Estado.Seleccionado;
        if (Ocupado || !PuedeComprar || pedido is null || pedido.OrdenRecepcionId is not null || pedido.Estado is not (0 or 1) || Estado.Catalogos is null ||
            Estado.DestinoEdicionId == Guid.Empty || !Estado.Catalogos.Establecimientos.Any(x => x.Id == Estado.DestinoEdicionId)) return Task.CompletedTask;
        return EjecutarAsync(async cancelacion =>
        {
            var entrada = new CrearPedidoCompraDto { Revision = pedido.Revision, EstablecimientoId = Estado.DestinoEdicionId };
            var actualizado = await ComprasApi.ActualizarDestinoPedidoCompraAsync(pedido.Id, entrada, cancelacion);
            if (Vigente) Estado.AceptarRespuesta(actualizado);
        }, "El destino se guardó en la compra.");
    }

    private void PrepararPropuesta()
    {
        if (Estado.Catalogos is null || Estado.NecesidadesDesactualizadas) return;
        propuesta = ModeloPropuestaCompra.Crear(Estado.Necesidades, Estado.Catalogos);
        if (propuesta.Lineas.Count == 0) MostrarAviso("No hay faltantes pendientes para proponer.");
    }

    private async Task CrearBorradoresPropuestaAsync()
    {
        if (Ocupado || propuesta is null || Estado.Catalogos is null || propuesta.EstablecimientoId == Guid.Empty) return;
        var lineas = propuesta.Lineas.Where(x => x.Incluida && x.Presentacion is not null && x.CantidadPresentaciones > 0m).ToList();
        if (lineas.Count == 0) return;
        if (lineas.Any(x => x.CantidadBase <= 0m || x.CantidadCubierta < x.Faltante))
        {
            MostrarError("Cada línea incluida debe cubrir su faltante con una presentación válida.");
            return;
        }
        if (!await ConfirmarAsync("Crear borradores de compra", "Se creará un borrador por proveedor y se reservarán sus necesidades. No se enviará ningún pedido. ¿Desea continuar?")) return;

        await EjecutarAsync(async ct =>
        {
            foreach (var grupo in lineas.GroupBy(x => x.Presentacion!.ProveedorId))
            {
                var pedido = await ComprasApi.CrearPedidoCompraAsync(new CrearPedidoCompraDto
                {
                    ProveedorId = grupo.Key,
                    EstablecimientoId = propuesta.EstablecimientoId
                }, ct);
                foreach (var linea in grupo)
                {
                    pedido = await ComprasApi.AgregarDetalleCompraAsync(pedido.Id, new AgregarDetalleCompraDto
                    {
                        Revision = pedido.Revision,
                        PresentacionProveedorId = linea.PresentacionId,
                        CantidadPresentaciones = linea.CantidadPresentaciones,
                        Asignaciones = linea.Necesidades.Select(x => new AsignacionNecesidadEntradaDto
                        {
                            NecesidadIngredienteId = x.NecesidadId,
                            CantidadAsignada = x.Cantidad,
                            Revision = x.Revision
                        }).ToArray()
                    }, ct);
                }
            }
            propuesta = null;
            await CargarAsync(ct, false);
        }, "Se crearon los borradores por proveedor. Revise cada uno antes de enviarlo.");
    }
    private async Task GenerarOrdenRecepcionAsync()
    {
        var pedido = Estado.Seleccionado;
        if (Ocupado || !PuedeComprar || pedido is null || pedido.Estado != 1 || pedido.OrdenRecepcionId is not null || pedido.EstablecimientoId is null || Estado.DestinoSinGuardar) return;
        if (!await ConfirmarAsync("Generar orden de recepción", "Se creará una única orden con los datos esperados de esta compra enviada. No se registrará stock ni recepción física. ¿Desea continuar?")) return;
        await EjecutarAsync(async cancelacion =>
        {
            var actualizado = await ComprasApi.GenerarOrdenRecepcionAsync(pedido.Id, new RevisionDto { Revision = pedido.Revision }, cancelacion);
            if (Vigente) Estado.AceptarRespuesta(actualizado);
        }, "La orden de recepción se generó correctamente. Ya puede consultarla.");
    }
    private async Task AbrirOperacionAsync<TDialogo>(string titulo, DialogParameters<TDialogo> parametros, string rol,
        Action<PedidoCompraDto>? despues = null, bool modificaNecesidades = false, MaxWidth ancho = MaxWidth.Medium, Func<MudButton?>? foco = null, string[]? rolesCualquiera = null)
        where TDialogo : DialogoEdicionBase
    {
        var confirmada = false;
        await AbrirEditorAsync<TDialogo, PedidoCompraDto>(titulo, parametros, respuesta =>
        {
            confirmada = true;
            Estado.AceptarRespuesta(respuesta);
            revisionServidor = null;
            if (modificaNecesidades) Estado.NecesidadesDesactualizadas = true;
            despues?.Invoke(respuesta);
            AjustarPaginaPedidos();
        }, ancho, rol, rolesCualquiera);
        if (confirmada && modificaNecesidades && Vigente) await ActualizarNecesidadesGuardadasAsync(despuesDeGuardado: true);
        if (Vigente && foco is not null) destinoFoco = foco;
    }
    private async Task ActualizarNecesidadesGuardadasAsync(bool despuesDeGuardado = false)
    {
        await EjecutarAsync(async ct =>
        {
            var turno = carga.Iniciar(ct);
            var necesidades = await ProduccionApi.ObtenerNecesidadesAsync(turno.Token);
            if (!Vigente || !carga.EsActual(turno.Generacion)) return;
            Estado.Necesidades = necesidades;
            Estado.NecesidadesDesactualizadas = false;
        }, esConsulta: true);
        if (Vigente && Estado.NecesidadesDesactualizadas)
            MostrarError(despuesDeGuardado
                ? "La operación confirmada se guardó. La actualización de necesidades está pendiente; revíselas y actualice antes de agregar asignaciones. No repita la escritura guardada."
                : "La actualización de necesidades sigue pendiente. Revise y actualice los datos antes de agregar asignaciones; no repita escrituras que ya tengan confirmación.");
        else if (Vigente && despuesDeGuardado)
            MostrarAviso("La operación se guardó y las necesidades se consultaron nuevamente. Se conserva la respuesta confirmada del pedido.");
    }
    private void AjustarPaginaPedidos() => paginaPedidos = Math.Clamp(paginaPedidos, 0, Math.Max(0, (PedidosCoincidentes.Count() - 1) / tamanoPaginaPedidos));
    private void SolicitarFocoAlternativo() { destinoFoco = () => botonActualizar; StateHasChanged(); }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (destinoFoco is null || Ocupado) return;
        var destino = destinoFoco;
        destinoFoco = null;
        if (!Vigente || indiceArea != 0) return;
        if ((destino() ?? botonActualizar) is { } boton) await boton.FocusAsync();
    }
    public override void Dispose() { carga.Dispose(); base.Dispose(); }
}
