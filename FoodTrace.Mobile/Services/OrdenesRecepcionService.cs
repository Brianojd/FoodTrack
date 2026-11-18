using FoodTrace.Mobile.Models.Recepcion;

namespace FoodTrace.Mobile.Services;

public sealed class OrdenesRecepcionService(ApiClient apiClient, SessionService sessionService)
{
    public async Task<List<OrdenRecepcionDto>> ObtenerOrdenesPendientesAsync(CancellationToken cancellationToken = default)
    {
        var ordenes = await apiClient.GetAsync<List<OrdenRecepcionDto>>("api/receipt-orders", cancellationToken);
        return ordenes
            .Where(x => x.Estado == (int)EstadoOrdenRecepcion.Pendiente)
            .OrderByDescending(x => x.FechaCreacion)
            .ToList();
    }

    public Task<OrdenRecepcionDto> ObtenerOrdenAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<OrdenRecepcionDto>($"api/receipt-orders/{id}", cancellationToken);

    public Task<RecepcionDto> ObtenerRecepcionAsync(Guid ordenId, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<RecepcionDto>($"api/receipt-orders/{ordenId}/receipt", cancellationToken);

    public async Task<RecepcionDto> ConfirmarRecepcionAsync(
        Guid ordenId,
        OrdenRecepcionDto orden,
        ConfirmarRecepcionDto entrada,
        CancellationToken cancellationToken = default)
    {
        ValidarEntrada(orden, entrada);
        return await apiClient.PostAsync<ConfirmarRecepcionDto, RecepcionDto>(
            $"api/receipt-orders/{ordenId}/receipt",
            entrada,
            cancellationToken);
    }

    public RecepcionDto ConstruirVistaPrevia(OrdenRecepcionDto orden, ConfirmarRecepcionDto entrada)
    {
        ValidarEntrada(orden, entrada);
        var porLinea = entrada.Detalles.ToDictionary(x => x.DetalleOrdenRecepcionId);
        var observacion = string.IsNullOrWhiteSpace(entrada.Observacion) ? null : entrada.Observacion.Trim();
        var receptor = sessionService.CurrentUser?.Email
            ?? sessionService.CurrentUser?.Id
            ?? "Cuenta autenticada";

        var recepcion = new RecepcionDto
        {
            Id = Guid.Empty,
            EmpresaId = orden.EmpresaId,
            Revision = orden.Revision,
            OrdenRecepcionId = orden.Id,
            UsuarioReceptorId = receptor,
            FechaRecepcion = DateTimeOffset.UtcNow,
            Observacion = observacion
        };

        foreach (var linea in orden.Detalles)
        {
            var recibidas = porLinea[linea.Id].CantidadPresentacionesRecibidas;
            var faltantes = linea.CantidadPresentaciones - recibidas;
            var cantidadRecibida = recibidas * linea.CantidadBasePresentacion;
            recepcion.Detalles.Add(new DetalleRecepcionDto
            {
                Id = Guid.Empty,
                EmpresaId = orden.EmpresaId,
                Revision = linea.Revision,
                DetalleOrdenRecepcionId = linea.Id,
                ProductoId = linea.ProductoId,
                NombreProducto = linea.NombreProducto,
                PresentacionProveedorId = linea.PresentacionProveedorId,
                NombrePresentacion = linea.NombrePresentacion,
                CantidadPresentacionesEsperadas = linea.CantidadPresentaciones,
                CantidadPresentacionesRecibidas = recibidas,
                CantidadPresentacionesFaltantes = faltantes,
                CantidadBasePresentacion = linea.CantidadBasePresentacion,
                CantidadEsperada = linea.CantidadEsperada,
                CantidadRecibida = cantidadRecibida,
                CantidadFaltante = linea.CantidadEsperada - cantidadRecibida,
                UnidadBaseId = linea.UnidadBaseId
            });
        }

        return recepcion;
    }

    private static void ValidarEntrada(OrdenRecepcionDto orden, ConfirmarRecepcionDto entrada)
    {
        if (entrada.Detalles is null || entrada.Detalles.Count != orden.Detalles.Count || entrada.Detalles.Count == 0)
            throw new InvalidOperationException("Debés informar explícitamente la cantidad recibida de todas las líneas de la orden.");

        if (entrada.Detalles.Any(x => x.DetalleOrdenRecepcionId == Guid.Empty) ||
            entrada.Detalles.Select(x => x.DetalleOrdenRecepcionId).Distinct().Count() != entrada.Detalles.Count)
            throw new InvalidOperationException("No repitas líneas ni envíes identificadores vacíos.");

        var porLinea = entrada.Detalles.ToDictionary(x => x.DetalleOrdenRecepcionId);
        foreach (var linea in orden.Detalles)
        {
            if (!porLinea.TryGetValue(linea.Id, out var detalleEntrada))
                throw new InvalidOperationException("Todas las líneas de la orden deben estar presentes en la confirmación.");

            var recibida = detalleEntrada.CantidadPresentacionesRecibidas;
            if (recibida < 0 || recibida > linea.CantidadPresentaciones || recibida != decimal.Truncate(recibida))
                throw new InvalidOperationException(
                    $"La cantidad recibida de «{linea.NombreProducto}» debe ser un entero entre 0 y {FormatoCantidad.Formatear(linea.CantidadPresentaciones)}.");
        }

        if (!string.IsNullOrWhiteSpace(entrada.Observacion) && entrada.Observacion.Trim().Length > 2000)
            throw new InvalidOperationException("La observación no puede superar los 2000 caracteres.");
    }
}
