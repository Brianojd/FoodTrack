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
    private readonly ControlCargaModulo carga = new();
    private PedidoCompraDto? revisionServidor;
    private bool HayDetalleSinGuardar => Estado.Detalle.TieneContenido || Estado.EdicionCantidad is { } edicion && edicion.Cantidad != edicion.CantidadOriginal;
    private MudButton? botonActualizar, botonCrear, botonAgregar, botonEnviar, botonAprobar, botonRechazar;
    private Func<MudButton?>? destinoFoco;
    protected override Task ActualizarAsync() => EjecutarAsync(ct => CargarAsync(ct, false), esConsulta: true);

    private async Task CargarAsync(CancellationToken ct, bool recargar)
    {
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
        if (Ocupado || !Sesion.TieneRol("buyer") || Estado.Catalogos is null) return;
        if ((HayDetalleSinGuardar || !string.IsNullOrEmpty(Estado.Rechazo.Motivo)) && !await ConfirmarAsync("Crear otro pedido", "Al crear el nuevo pedido se descartarán los otros formularios sin guardar. ¿Desea continuar?")) return;
        var parametros = new DialogParameters<DialogoCrearPedido>
        {
            { x => x.Catalogos, Estado.Catalogos }, { x => x.ProveedorInicialId, Estado.Nuevo.ProveedorId }
        };
        await AbrirOperacionAsync("Crear pedido de compra", parametros, "buyer", pedido => Estado.Seleccionar(pedido), foco: () => botonCrear);
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
            modificaNecesidades: true, ancho: MaxWidth.Large, foco: () => PuedeEditar() && !Estado.NecesidadesDesactualizadas ? botonAgregar : botonActualizar);
    }
    private bool PuedeEditar() => Sesion.TieneRol("buyer") && Estado.Seleccionado?.Estado == 0;
    protected Task EditarCantidadAsync(DetallePedidoCompraDto detalle)
    {
        if (Ocupado || !PuedeEditar() || Estado.Catalogos is null) return Task.CompletedTask;
        var pedido = Estado.Seleccionado!;
        if (InstantaneaPedido.DetalleActivo(pedido, detalle.Id) is null) { MostrarError("El detalle no pertenece al borrador seleccionado o está retirado."); return Task.CompletedTask; }
        var parametros = new DialogParameters<DialogoCantidadPresentaciones>
        {
            { x => x.Pedido, pedido }, { x => x.DetalleId, detalle.Id }, { x => x.Catalogos, Estado.Catalogos }
        };
        return AbrirOperacionAsync("Cambiar cantidad de presentaciones", parametros, "buyer", _ => Estado.EdicionCantidad = null);
    }
    private async Task DescartarDetalleAsync()
    {
        if (!Ocupado && await ConfirmarAsync("Descartar borradores locales", "Se descartarán los borradores anteriores de detalle, cantidad y motivo, sin enviar escrituras."))
        { Estado.Detalle = new(); Estado.EdicionCantidad = null; Estado.Rechazo = new(); }
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
        }, modificaNecesidades: true);
    }
    private Task EnviarAsync()
    {
        if (Ocupado || !PuedeEditar() || Estado.Catalogos is null || HayDetalleSinGuardar || !Estado.Seleccionado!.Detalles.Any()) return Task.CompletedTask;
        var parametros = new DialogParameters<DialogoEnviarPedido> { { x => x.Pedido, Estado.Seleccionado }, { x => x.Catalogos, Estado.Catalogos } };
        return AbrirOperacionAsync("Enviar pedido para decisión", parametros, "buyer", foco: () => PuedeEditar() ? botonEnviar : botonActualizar);
    }
    private Task AprobarAsync()
    {
        if (Ocupado || !Sesion.TieneRol("admin") || Estado.Seleccionado is not { Estado: 1 } pedido || Estado.Catalogos is null) return Task.CompletedTask;
        var parametros = new DialogParameters<DialogoAprobarPedido> { { x => x.Pedido, pedido }, { x => x.Catalogos, Estado.Catalogos } };
        return AbrirOperacionAsync("Aprobar pedido enviado", parametros, "admin", _ => Estado.Rechazo = new(), foco: () => Estado.Seleccionado?.Estado == 1 ? botonAprobar : botonActualizar);
    }
    private Task RechazarAsync()
    {
        if (Ocupado || !Sesion.TieneRol("admin") || Estado.Seleccionado is not { Estado: 1 } pedido || Estado.Catalogos is null) return Task.CompletedTask;
        var parametros = new DialogParameters<DialogoRechazarPedido>
        {
            { x => x.Pedido, pedido }, { x => x.Catalogos, Estado.Catalogos }, { x => x.MotivoInicial, Estado.Rechazo.Motivo }
        };
        return AbrirOperacionAsync("Rechazar pedido enviado", parametros, "admin", _ => Estado.Rechazo = new(), modificaNecesidades: true,
            foco: () => Estado.Seleccionado?.Estado == 1 ? botonRechazar : botonActualizar);
    }
    private async Task AbrirOperacionAsync<TDialogo>(string titulo, DialogParameters<TDialogo> parametros, string rol,
        Action<PedidoCompraDto>? despues = null, bool modificaNecesidades = false, MaxWidth ancho = MaxWidth.Medium, Func<MudButton?>? foco = null)
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
        }, ancho, rol);
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
