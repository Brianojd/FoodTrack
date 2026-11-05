using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Empresas;
using FoodTrace.Dtos.Establecimientos;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Proveedores;
using FoodTrace.Dtos.Usuarios;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.Services.Api;

// El registro scoped mantiene token e identidad independientes por circuito.
public sealed class ClienteFoodTraceApi(HttpClient cliente, ILogger<ClienteFoodTraceApi>? registro = null) : ISesionFoodTrace, ICatalogosFoodTrace, IUsuariosFoodTrace, IServicioRecetas, IServicioProduccion, IServicioCompras, IDisposable
{
    private string? tokenAcceso;
    private bool dispuesto;
    public long VersionSesion { get; private set; }
    public string AvisoSesion { get; private set; } = "";
    public event Action? SesionCambiada;
    public UsuarioActualDto? Identidad { get; private set; }
    public bool TieneRol(string rol) => Identidad?.Roles.Contains(rol) == true;

    public async Task<UsuarioActualDto> IniciarSesionAsync(IniciarSesionDto entrada, CancellationToken cancelacion = default)
    {
        CerrarSesion();
        var version = VersionSesion;
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "auth/login") { Content = JsonContent.Create(entrada) };
        using var respuesta = await EnviarPeticionAsync(peticion, cancelacion);
        ComprobarVigencia(version, cancelacion);
        await ComprobarRespuestaAsync(respuesta, version, cancelacion);
        var resultado = await respuesta.Content.ReadFromJsonAsync<RespuestaInicioSesionDto>(cancelacion)
            ?? throw new InvalidOperationException("La API devolvió una respuesta de inicio de sesión vacía.");
        ComprobarVigencia(version, cancelacion);
        tokenAcceso = resultado.TokenAcceso;
        UsuarioActualDto identidad;
        try { identidad = await ObtenerUsuarioActualAsync(cancelacion); }
        catch { if (VersionSesion == version) CerrarSesion(); throw; }
        ComprobarVigencia(version, cancelacion);
        Identidad = identidad;
        SesionCambiada?.Invoke();
        return Identidad;
    }

    public void CerrarSesion() => FinalizarSesion("");

    private void FinalizarSesion(string aviso)
    {
        VersionSesion++;
        tokenAcceso = null;
        Identidad = null;
        AvisoSesion = aviso;
        SesionCambiada?.Invoke();
    }
    public Task<UsuarioActualDto> ObtenerUsuarioActualAsync(CancellationToken cancelacion = default) => ObtenerAsync<UsuarioActualDto>("api/me", cancelacion);
    public Task<CatalogosDto> ObtenerCatalogosAsync(CancellationToken cancelacion = default) => ObtenerAsync<CatalogosDto>("api/catalogs", cancelacion);
    public Task<NombreDto> ActualizarEmpresaAsync(ActualizarEmpresaDto entrada, CancellationToken cancelacion = default) => ActualizarAsync<NombreDto>("api/company", entrada, cancelacion);
    public Task<NombreDto> CrearEstablecimientoAsync(CrearEstablecimientoDto entrada, CancellationToken cancelacion = default) => CrearAsync<NombreDto>("api/establishments", entrada, cancelacion);
    public Task<ProductoDto> CrearProductoAsync(CrearProductoDto entrada, CancellationToken cancelacion = default) => CrearAsync<ProductoDto>("api/products", entrada, cancelacion);
    public Task<NombreDto> CrearProveedorAsync(CrearProveedorDto entrada, CancellationToken cancelacion = default) => CrearAsync<NombreDto>("api/suppliers", entrada, cancelacion);
    public Task<PresentacionProveedorDto> CrearPresentacionAsync(CrearPresentacionProveedorDto entrada, CancellationToken cancelacion = default) => CrearAsync<PresentacionProveedorDto>("api/packs", entrada, cancelacion);
    public Task<List<UsuarioDto>> ObtenerUsuariosAsync(CancellationToken cancelacion = default) => ObtenerAsync<List<UsuarioDto>>("api/users", cancelacion);
    public Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto entrada, CancellationToken cancelacion = default) => CrearAsync<UsuarioDto>("api/users", entrada, cancelacion);
    public Task<UsuarioDto> ActualizarMembresiaAsync(string id, ActualizarMembresiaDto entrada, CancellationToken cancelacion = default) => ActualizarAsync<UsuarioDto>($"api/users/{id}", entrada, cancelacion);
    public Task<List<VersionRecetaDto>> ObtenerRecetasAsync(CancellationToken cancelacion = default) => ObtenerAsync<List<VersionRecetaDto>>("api/recipes", cancelacion);
    public Task<VersionRecetaDto> CrearRecetaAsync(CrearRecetaDto entrada, CancellationToken cancelacion = default) => CrearAsync<VersionRecetaDto>("api/recipes", entrada, cancelacion);
    public Task<VersionRecetaDto> ActualizarRecetaAsync(Guid id, ActualizarRecetaDto entrada, CancellationToken cancelacion = default) => ActualizarAsync<VersionRecetaDto>($"api/recipes/{id}", entrada, cancelacion);
    public Task<VersionRecetaDto> PublicarRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => CrearAsync<VersionRecetaDto>($"api/recipes/{id}/publish", entrada, cancelacion);
    public Task<VersionRecetaDto> CrearVersionRecetaAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => CrearAsync<VersionRecetaDto>($"api/recipes/{id}/versions", entrada, cancelacion);
    public Task<List<OrdenProduccionDto>> ObtenerProduccionAsync(CancellationToken cancelacion = default) => ObtenerAsync<List<OrdenProduccionDto>>("api/production", cancelacion);
    public Task<List<NecesidadIngredienteDto>> ObtenerNecesidadesAsync(CancellationToken cancelacion = default) => ObtenerAsync<List<NecesidadIngredienteDto>>("api/needs", cancelacion);
    public Task<OrdenProduccionDto> PlanificarProduccionAsync(PlanificarProduccionDto entrada, CancellationToken cancelacion = default) => CrearAsync<OrdenProduccionDto>("api/production", entrada, cancelacion);
    public Task<List<PedidoCompraDto>> ObtenerPedidosCompraAsync(CancellationToken cancelacion = default) => ObtenerAsync<List<PedidoCompraDto>>("api/purchases", cancelacion);
    public Task<PedidoCompraDto> CrearPedidoCompraAsync(CrearPedidoCompraDto entrada, CancellationToken cancelacion = default) => CrearAsync<PedidoCompraDto>("api/purchases", entrada, cancelacion);
    public Task<PedidoCompraDto> AgregarDetalleCompraAsync(Guid id, AgregarDetalleCompraDto entrada, CancellationToken cancelacion = default) => CrearAsync<PedidoCompraDto>($"api/purchases/{id}/lines", entrada, cancelacion);
    public Task<PedidoCompraDto> ActualizarPresentacionesAsync(Guid id, Guid detalleId, ActualizarCantidadPresentacionesDto entrada, CancellationToken cancelacion = default) => ActualizarAsync<PedidoCompraDto>($"api/purchases/{id}/lines/{detalleId}", entrada, cancelacion);
    public Task<PedidoCompraDto> RetirarDetalleCompraAsync(Guid id, Guid detalleId, RevisionDto entrada, CancellationToken cancelacion = default) => CrearAsync<PedidoCompraDto>($"api/purchases/{id}/lines/{detalleId}/remove", entrada, cancelacion);
    public Task<PedidoCompraDto> EnviarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => CrearAsync<PedidoCompraDto>($"api/purchases/{id}/submit", entrada, cancelacion);
    public Task<PedidoCompraDto> AprobarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => CrearAsync<PedidoCompraDto>($"api/purchases/{id}/approve", entrada, cancelacion);
    public Task<PedidoCompraDto> RechazarPedidoCompraAsync(Guid id, RevisionDto entrada, CancellationToken cancelacion = default) => CrearAsync<PedidoCompraDto>($"api/purchases/{id}/reject", entrada, cancelacion);

    private Task<T> ObtenerAsync<T>(string ruta, CancellationToken cancelacion) => EnviarAsync<T>(HttpMethod.Get, ruta, null, cancelacion);
    private Task<T> CrearAsync<T>(string ruta, object cuerpo, CancellationToken cancelacion) => EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo, cancelacion);
    private Task<T> ActualizarAsync<T>(string ruta, object cuerpo, CancellationToken cancelacion) => EnviarAsync<T>(HttpMethod.Put, ruta, cuerpo, cancelacion);

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken cancelacion)
    {
        var version = VersionSesion;
        using var peticion = new HttpRequestMessage(metodo, ruta);
        if (tokenAcceso is not null) peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAcceso);
        if (cuerpo is not null) peticion.Content = JsonContent.Create(cuerpo);
        using var respuesta = await EnviarPeticionAsync(peticion, cancelacion);
        ComprobarVigencia(version, cancelacion);
        await ComprobarRespuestaAsync(respuesta, version, cancelacion);
        var resultado = await respuesta.Content.ReadFromJsonAsync<T>(cancelacion) ?? throw new InvalidOperationException("La API devolvió una respuesta vacía.");
        ComprobarVigencia(version, cancelacion);
        return resultado;
    }

    private void ComprobarVigencia(long version, CancellationToken cancelacion)
    {
        cancelacion.ThrowIfCancellationRequested();
        if (VersionSesion != version) throw new OperationCanceledException("La sesión cambió.");
    }

    private async Task<HttpResponseMessage> EnviarPeticionAsync(HttpRequestMessage peticion, CancellationToken cancelacion)
    {
        try { return await cliente.SendAsync(peticion, cancelacion); }
        catch (HttpRequestException)
        {
            registro?.LogWarning("No se pudo establecer la conexión con la API.");
            throw new ExcepcionApi(HttpStatusCode.ServiceUnavailable, "No se pudo conectar con el servicio. Compruebe la conexión e inténtelo nuevamente.");
        }
        catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            registro?.LogWarning("Se agotó el tiempo de espera de la API.");
            throw new ExcepcionApi(HttpStatusCode.GatewayTimeout, "El servicio tardó demasiado en responder. Sus datos se conservan; vuelva a intentarlo.");
        }
    }

    private async Task ComprobarRespuestaAsync(HttpResponseMessage respuesta, long version, CancellationToken cancelacion)
    {
        if (respuesta.IsSuccessStatusCode) return;
        registro?.LogWarning("Solicitud rechazada por la API. Estado HTTP: {Estado}.", (int)respuesta.StatusCode);
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && VersionSesion == version)
            FinalizarSesion(tokenAcceso is not null ? "Su sesión venció o dejó de ser válida. Vuelva a ingresar." : "");
        var versionEsperada = respuesta.StatusCode == HttpStatusCode.Unauthorized ? VersionSesion : version;
        var mensaje = respuesta.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "El inicio de sesión falló o la sesión venció. Vuelva a ingresar.",
            HttpStatusCode.Forbidden => "Su cuenta no tiene permiso para realizar esta acción.",
            HttpStatusCode.Conflict => "El registro cambió. Su formulario se conserva. Revise los datos y decida si desea recargar antes de reintentar.",
            HttpStatusCode.BadRequest => "Revise los campos obligatorios, las cantidades, las unidades y los permisos seleccionados. La API no aceptó los datos.",
            HttpStatusCode.NotFound => "El registro ya no está disponible. Puede actualizar la lista sin descartar su formulario.",
            _ => "El servicio no pudo completar la solicitud. Inténtelo nuevamente más tarde."
        };
        var problema = await LeerProblemaAsync(respuesta, cancelacion);
        var errores = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var campos = new Dictionary<string, string>();
        if ((int)respuesta.StatusCode < 500 && problema is ValidationProblemDetails validacion)
            foreach (var campo in validacion.Errors)
            {
                // Preserve MVC identity and every message, including indexed ingredient paths.
                errores[campo.Key] = campo.Value.ToArray();
                campos[EtiquetaCampo(campo.Key)] = string.Join(" ", errores[campo.Key]);
            }
        if (respuesta.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
        {
            var detalle = problema?.Detail;
            if (!string.IsNullOrWhiteSpace(detalle)) mensaje = detalle;
        }
        var traceId = ObtenerTraceId(problema);
        ComprobarVigencia(versionEsperada, cancelacion);
        throw new ExcepcionApi(respuesta.StatusCode, mensaje, campos, problema, errores, traceId);
    }

    private static async Task<ProblemDetails?> LeerProblemaAsync(HttpResponseMessage respuesta, CancellationToken cancelacion)
    {
        var tipo = respuesta.Content.Headers.ContentType?.MediaType;
        if (tipo is not ("application/problem+json" or "application/json")) return null;
        try
        {
            using var documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(cancelacion), cancellationToken: cancelacion);
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object) return null;
            // A media type is not proof of a problem envelope or of safe display text.
            // Require the complete API envelope for both accepted JSON media types.
            if (!raiz.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.Number || !status.TryGetInt32(out var estado) || estado != (int)respuesta.StatusCode ||
                !raiz.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String ||
                type.GetString() is not { Length: > 0 and <= 512 } tipoProblema || !Uri.TryCreate(tipoProblema, UriKind.Absolute, out _) ||
                !raiz.TryGetProperty("title", out var title) || title.ValueKind != JsonValueKind.String ||
                title.GetString() is not { Length: > 0 and <= 256 } titulo || string.IsNullOrWhiteSpace(titulo) ||
                !raiz.TryGetProperty("instance", out var instance) || instance.ValueKind != JsonValueKind.String ||
                instance.GetString() is not { Length: > 0 and <= 2048 } instancia || !instancia.StartsWith('/') || instancia.StartsWith("//") || instancia.Any(char.IsControl) ||
                !raiz.TryGetProperty("traceId", out var trace)) return null;

            var tieneErrores = raiz.TryGetProperty("errors", out var errors);
            if (tieneErrores && errors.ValueKind != JsonValueKind.Object) return null;
            var errores = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (tieneErrores)
                foreach (var campo in errors.EnumerateObject())
                {
                    // Keep valid MVC identities; never echo diagnostic text as a display label.
                    if (campo.Name.Length > 512 || !campo.Name.All(c => char.IsLetterOrDigit(c) || c is '_' or '.' or '[' or ']' or '$')) continue;
                    var mensajes = campo.Value.ValueKind == JsonValueKind.Array
                        ? campo.Value.EnumerateArray().Select(m => TraducirValidacion(m.ValueKind == JsonValueKind.String ? m.GetString() : null)).ToArray()
                        : [];
                    errores[campo.Name] = mensajes.Length == 0 ? ["Revise el valor ingresado."] : mensajes;
                }
            ProblemDetails problema = tieneErrores ? new ValidationProblemDetails(errores) : new ProblemDetails();
            problema.Type = tipoProblema;
            problema.Title = estado switch
            {
                400 => "Datos no válidos", 401 => "Autenticación requerida", 403 => "Acceso no permitido",
                404 => "Recurso no disponible", 409 => "Conflicto con los datos guardados",
                _ => estado >= 500 ? "Error del servicio" : "Solicitud no completada"
            };
            // HTTP alone controls authentication. Body status is only an envelope consistency check.
            problema.Status = estado;
            problema.Instance = instancia.Split('?', '#')[0];
            problema.Extensions["traceId"] = trace.Clone();
            if (ObtenerTraceId(problema) is null) return null;
            if (raiz.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                problema.Detail = DetalleSeguro(estado, detail.GetString());
            return problema;
        }
        catch (JsonException) { return null; }
        catch (NotSupportedException) { return null; }
        catch (IOException) { return null; }
    }

    private static string? ObtenerTraceId(ProblemDetails? problema)
    {
        if (problema?.Extensions.TryGetValue("traceId", out var valor) != true) return null;
        var texto = valor switch
        {
            string cadena => cadena,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.Number } json => json.GetRawText(),
            _ => null
        };
        return texto is { Length: > 0 and <= 128 } && texto.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':') ? texto : null;
    }

    // Only the current API's explicitly safe business explanations cross the display
    // boundary verbatim. Unknown prose (even in a valid envelope) uses local fallback;
    // keyword/blocklist scanning cannot establish that diagnostics are safe.
    private static string? DetalleSeguro(int estado, string? detalle) => (estado, detalle) switch
    {
        (400, "El cálculo supera el rango admitido. Utilice cantidades menores.") => detalle,
        (400, "Revise los datos, las cantidades, las unidades y el estado requerido para esta operación.") => detalle,
        (409, "Los datos cambiaron o entran en conflicto con un registro guardado. Revise la información antes de reintentar.") => detalle,
        _ => null
    };

    private static string TraducirValidacion(string? mensaje) => mensaje switch
    {
        null or "" => "Revise el valor ingresado.",
        "Seleccione un producto disponible." or "Seleccione una unidad válida." or "Seleccione una unidad de rendimiento válida."
            or "Seleccione una unidad de rendimiento válida o no envíe el campo." or "La unidad debe ser compatible con el producto resultado."
            or "La unidad debe ser compatible con el ingrediente." or "Incluya entre uno y 200 ingredientes completos."
            or "Complete el ingrediente." or "Indique una cantidad positiva de hasta 1.000.000.000 y seis decimales."
            or "El rendimiento convertido debe ser positivo, hasta 1.000.000.000 y representable con seis decimales sin redondeo."
            or "La cantidad convertida supera el rango admitido." or "No indique una unidad al planificar por porciones."
            or "La receta no define una equivalencia de porciones."
            or "La cantidad convertida debe ser positiva y representable con seis decimales dentro del rango admitido."
            or "Seleccione una unidad válida para planificar por cantidad." => mensaje,
        string m when m.Contains("required", StringComparison.OrdinalIgnoreCase) => "Este campo es obligatorio.",
        string m when m.Contains("email", StringComparison.OrdinalIgnoreCase) => "Ingrese un correo electrónico válido.",
        string m when m.Contains("range", StringComparison.OrdinalIgnoreCase) || m.Contains("between", StringComparison.OrdinalIgnoreCase) => "Ingrese un valor dentro del rango permitido.",
        string m when m.Contains("length", StringComparison.OrdinalIgnoreCase) || m.Contains("maximum", StringComparison.OrdinalIgnoreCase) => "Revise la longitud permitida para este campo.",
        _ => "Revise el valor ingresado."
    };

    private static string EtiquetaCampo(string clave) => clave.ToLowerInvariant() switch
    {
        "name" => "Nombre", "email" => "Correo electrónico", "password" => "Contraseña",
        "quantity" => "Contenido", "unitid" => "Unidad", "baseunitid" => "Unidad base",
        "supplierid" => "Proveedor", "productid" => "Producto", "roles" => "Roles",
        "enabled" => "Acceso habilitado", "outputproductid" => "Producto resultado",
        "yield" => "Rendimiento", "instructions" => "Instrucciones", "ingredients" => "Ingredientes",
        "yieldunitid" => "Unidad del rendimiento", "yieldportions" => "Porciones del rendimiento", "byportions" => "Planificación por porciones",
        "establishmentid" => "Establecimiento", "recipeversionid" => "Versión de receta",
        "date" => "Fecha de producción", "revision" => "Revisión guardada", _ => "Campo"
    };

    public void Dispose()
    {
        if (dispuesto) return;
        dispuesto = true;
        CerrarSesion();
        cliente.Dispose();
    }
}
