using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Dialogos;
using FoodTrace.Components.Dialogos.Produccion;
using FoodTrace.Components.Dialogos.Recetas;
using FoodTrace.Components.Dialogos.Catalogos;
using FoodTrace.Components.Productos;
using FoodTrace.Components.Pages.Produccion;
using FoodTrace.Components.Pages.Recetas;
using FoodTrace.Components.Produccion;
using FoodTrace.Components.Recetas;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Productos;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Dtos.UnidadesMedida;
using FoodTrace.Models.Produccion;
using FoodTrace.Models.Recetas;
using FoodTrace.Services.Api;
using FoodTrace.Services.Estado;
using FoodTrace.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Xunit;

namespace FoodTrace.Tests;

public sealed class ProduccionInterfazTests
{
    [Fact]
    public void La_planificacion_por_cantidad_exige_conversion_exacta_representable()
    {
        var transporte = new Transporte("planner"); var catalogos = transporte.Catalogos();
        catalogos.UnidadesMedida.Add(new() { Id = "g", Dimension = "mass", FactorConversion = .001m });
        var version = new VersionRecetaDto { Id = Guid.NewGuid(), Publicada = true, ProductoResultadoId = transporte.Producto, Rendimiento = 1 };
        var modelo = new ModeloPlanificacion { EstablecimientoId = catalogos.Establecimientos[0].Id, VersionRecetaId = version.Id, Fecha = new DateTime(2026, 11, 5), UnidadMedidaId = "g", Cantidad = .000001m };
        Assert.Contains("La cantidad convertida debe ser positiva y representable con seis decimales dentro del rango admitido.", modelo.Validar(catalogos, [version]));
        modelo.Cantidad = .001m; Assert.Empty(modelo.Validar(catalogos, [version]));
        modelo.UnidadMedidaId = "kg"; modelo.Cantidad = 1.5m; Assert.Empty(modelo.Validar(catalogos, [version]));
    }

    [Fact]
    public void La_validacion_local_identifica_los_objetos_de_cada_fila_y_todos_los_campos()
    {
        var transporte = new Transporte("planner"); var catalogos = transporte.Catalogos();
        var fila = new ModeloIngredienteReceta { ProductoId = transporte.Producto, UnidadMedidaId = "kg", Cantidad = 1.5m };
        var modelo = new ModeloVersionReceta { Nombre = "QA", ProductoResultadoId = transporte.Producto, Rendimiento = 1, UnidadRendimientoId = "kg", Instrucciones = "Mezclar", Ingredientes = [fila] };
        Assert.Empty(modelo.ValidarCampos(catalogos));
        foreach (var cantidad in new[] { 0m, -1m, .0000001m, 1000000001m })
        {
            fila.Cantidad = cantidad;
            Assert.Contains(new FieldIdentifier(fila, nameof(fila.Cantidad)), modelo.ValidarCampos(catalogos).Keys);
        }
        fila.Cantidad = 1.5m;
        modelo.PorcionesRendimiento = 0;
        Assert.Contains(new FieldIdentifier(modelo, nameof(modelo.PorcionesRendimiento)), modelo.ValidarCampos(catalogos).Keys);
        modelo.PorcionesRendimiento = 8;
        modelo.Ingredientes.Add(new() { ProductoId = Guid.Empty, UnidadMedidaId = "L" });
        Assert.Contains(new FieldIdentifier(modelo.Ingredientes[1], nameof(fila.ProductoId)), modelo.ValidarCampos(catalogos).Keys);
        Assert.Equal(2, modelo.ObtenerIngredientes().Length); // No descartar silenciosamente filas inválidas.
        modelo.Ingredientes.RemoveAt(1);
        Assert.Empty(modelo.ValidarCampos(catalogos));
    }

    [Fact]
    public async Task La_unidad_incompatible_se_limpia_sin_perder_otras_filas()
    {
        var transporte = new Transporte("planner"); var catalogos = transporte.Catalogos();
        var liquido = new ProductoDto { Id = Guid.NewGuid(), Nombre = "Leche", UnidadBaseId = "L" };
        catalogos.Productos.Add(liquido); catalogos.UnidadesMedida.Add(new() { Id = "L", Dimension = "volume", FactorConversion = 1 });
        var primera = new ModeloIngredienteReceta { ProductoId = transporte.Producto, Cantidad = 1.5m, UnidadMedidaId = "kg" };
        var segunda = new ModeloIngredienteReceta { ProductoId = transporte.Producto, Cantidad = 2, UnidadMedidaId = "kg" };
        var editor = new EditorIngredientesReceta { Catalogos = catalogos, Ingredientes = [primera, segunda] };
        await (Task)typeof(EditorIngredientesReceta).GetMethod("CambiarProductoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [primera, liquido.Id])!;
        Assert.Equal("", primera.UnidadMedidaId); Assert.Equal(1.5m, primera.Cantidad);
        Assert.Equal("kg", segunda.UnidadMedidaId); Assert.Equal(2m, segunda.Cantidad);
        Assert.NotEqual(primera.Clave, segunda.Clave);
    }

    [Fact]
    public void La_cultura_decimal_del_control_acepta_coma_y_el_json_conserva_decimal()
    {
        var modelo = new ModeloVersionReceta { Rendimiento = 1.5m };
        var form = new FormularioVersionReceta { Modelo = modelo, PuedeEditar = true, Catalogos = new Transporte("planner").Catalogos() };
        var cantidad = FindComponent(Frames(form), typeof(MudNumericField<decimal>));
        var cultura = Assert.IsType<System.Globalization.CultureInfo>(cantidad["Culture"]);
        Assert.Equal("es-AR", cultura.Name);
        Assert.Equal(1.5m, decimal.Parse("1,5", cultura));
        Assert.Contains("\"yield\":1.5", System.Text.Json.JsonSerializer.Serialize(modelo.ParaCrear()));
    }

    [Fact]
    public async Task Dos_callbacks_simultaneos_solo_envian_un_guardado_valido()
    {
        var transporte = new Transporte("planner"); var fin = new TaskCompletionSource(); var guardados = 0;
        var modelo = new ModeloVersionReceta { Nombre = "QA", ProductoResultadoId = transporte.Producto, Rendimiento = 1.5m, UnidadRendimientoId = "kg", Instrucciones = "Mezclar", Ingredientes = [new() { ProductoId = transporte.Producto, UnidadMedidaId = "kg", Cantidad = 1 }] };
        var form = new FormularioVersionReceta { Modelo = modelo, Catalogos = transporte.Catalogos(), PuedeEditar = true,
            Guardar = EventCallback.Factory.Create(this, async () => { guardados++; await fin.Task; }) };
        var guardar = typeof(FormularioVersionReceta).GetMethod("GuardarValidoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var primero = (Task)guardar.Invoke(form, null)!;
        await (Task)guardar.Invoke(form, null)!;
        Assert.Equal(1, guardados); Assert.Equal(1.5m, modelo.Rendimiento);
        fin.SetResult(); await primero;
    }

    [Fact]
    public void Planificar_por_porciones_no_inventa_unidad_y_rechaza_mezclas_o_equivalencia_ausente()
    {
        var transporte = new Transporte("planner"); var catalogos = transporte.Catalogos();
        var version = new VersionRecetaDto { Id = Guid.NewGuid(), ProductoResultadoId = transporte.Producto, Publicada = true, Rendimiento = 5, PorcionesRendimiento = 10 };
        var modelo = new ModeloPlanificacion { EstablecimientoId = catalogos.Establecimientos[0].Id, VersionRecetaId = version.Id, PorPorciones = true, Cantidad = 20, Fecha = new DateTime(2026, 11, 5) };
        Assert.Empty(modelo.Validar(catalogos, [version]));
        Assert.True(modelo.ParaEnviar().PorPorciones); Assert.Equal("", modelo.ParaEnviar().UnidadMedidaId);
        Assert.True(modelo.Copiar().EquivaleA(modelo));
        modelo.UnidadMedidaId = "kg"; Assert.NotEmpty(modelo.Validar(catalogos, [version]));
        modelo.UnidadMedidaId = ""; version.PorcionesRendimiento = null; Assert.NotEmpty(modelo.Validar(catalogos, [version]));
    }

    public class Proxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Accion = (_, _) => null;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Accion(method!, args);
    }
    private static T Doblar<T>(Func<MethodInfo, object?[]?, object?> accion) where T : class
    { var proxy = DispatchProxy.Create<T, Proxy>(); ((Proxy)(object)proxy).Accion = accion; return proxy; }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Producto_anidado_cancelado_o_confirmado_con_GET_fallido_conserva_la_receta(bool confirmar)
    {
        var transporte = new Transporte("admin", "planner");
        await WithRenderer(transporte, async (renderer, _) =>
        {
            DialogoVersionReceta dialogo = null!;
            await renderer.RenderComponentAsync<HostDialogo>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Catalogos"] = transporte.Catalogos(), ["Capturar"] = (Action<DialogoVersionReceta>)(x => dialogo = x) }));
            var modelo = (ModeloVersionReceta)typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
            modelo.Nombre = "Texto conservado"; modelo.Rendimiento = 1.5m; modelo.Instrucciones = "Instrucciones conservadas";
            var fila = new ModeloIngredienteReceta { Cantidad = 2.5m }; modelo.Ingredientes.Add(fila);
            var revision = Guid.NewGuid(); typeof(DialogoVersionReceta).GetField("revisionOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dialogo, revision);
            var producto = new ProductoDto { Id = Guid.NewGuid(), Nombre = "QA-Recetas creado", UnidadBaseId = "kg" };
            var aperturas = 0;
            var referencia = Doblar<IDialogReference>((m, _) => m.Name == "get_Result" ? Task.FromResult<DialogResult?>(confirmar ? DialogResult.Ok(producto) : DialogResult.Cancel()) : null);
            var servicio = Doblar<IDialogService>((m, _) => { if (m.Name == "ShowAsync") { aperturas++; return Task.FromResult(referencia); } return null; });
            typeof(DialogoVersionReceta).GetProperty("Dialogos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(dialogo, servicio);
            transporte.FallaCatalogo = true;
            await (Task)typeof(DialogoVersionReceta).GetMethod("CrearProductoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dialogo, [fila.Clave])!;
            Assert.Equal(1, aperturas); Assert.Equal("Texto conservado", modelo.Nombre); Assert.Equal(1.5m, modelo.Rendimiento);
            Assert.Equal("Instrucciones conservadas", modelo.Instrucciones); Assert.Equal(2.5m, fila.Cantidad);
            Assert.Equal(revision, typeof(DialogoVersionReceta).GetField("revisionOriginal", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo));
            Assert.Equal(confirmar ? producto.Id : Guid.Empty, fila.ProductoId);
            if (confirmar)
            {
                var aviso = (string)typeof(DialogoVersionReceta).GetField("avisoCatalogo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
                Assert.Contains("ya se creó", aviso);
                await (Task)typeof(DialogoVersionReceta).GetMethod("ActualizarCatalogoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dialogo, null)!;
                Assert.Equal(1, aperturas); Assert.Equal(producto.Id, fila.ProductoId);
            }
            Assert.Single(dialogo.Catalogos.Productos); // La instancia del catálogo ajeno no se muta.
        });
    }

    [Fact]
    public void El_host_de_producto_conecta_su_formulario_real()
    {
        var dialogo = new DialogoCrearProducto();
        var marco = Component(Frames(dialogo), typeof(MarcoDialogoEdicion));
        Assert.Equal(dialogo.FormId, FindComponent(Frames((RenderFragment)marco["ChildContent"]), typeof(EditorProducto))["FormId"]);
    }

    [Fact]
    public void EditContext_mantiene_errores_por_fila_y_retirar_una_no_borra_otra()
    {
        var transporte = new Transporte("planner");
        var uno = new ModeloIngredienteReceta { ProductoId = transporte.Producto, UnidadMedidaId = "kg", Cantidad = 0 };
        var dos = new ModeloIngredienteReceta { ProductoId = transporte.Producto, UnidadMedidaId = "kg", Cantidad = -1 };
        var modelo = new ModeloVersionReceta { Ingredientes = [uno, dos] };
        using var form = new FormularioVersionReceta { Modelo = modelo, Catalogos = transporte.Catalogos() };
        typeof(FormularioVersionReceta).GetMethod("OnParametersSet", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        var context = (EditContext)typeof(FormularioVersionReceta).GetField("contexto", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        Assert.Empty(context.GetValidationMessages());
        context.NotifyFieldChanged(new(uno, nameof(uno.Cantidad))); context.NotifyFieldChanged(new(dos, nameof(dos.Cantidad)));
        Assert.NotEmpty(context.GetValidationMessages(new FieldIdentifier(uno, nameof(uno.Cantidad))));
        Assert.NotEmpty(context.GetValidationMessages(new FieldIdentifier(dos, nameof(dos.Cantidad))));
        modelo.Ingredientes.Remove(uno); context.NotifyFieldChanged(new(modelo, nameof(modelo.Ingredientes)));
        Assert.Empty(context.GetValidationMessages(new FieldIdentifier(uno, nameof(uno.Cantidad))));
        Assert.NotEmpty(context.GetValidationMessages(new FieldIdentifier(dos, nameof(dos.Cantidad))));
        dos.Cantidad = 1.5m; context.NotifyFieldChanged(new(dos, nameof(dos.Cantidad)));
        Assert.Empty(context.GetValidationMessages(new FieldIdentifier(dos, nameof(dos.Cantidad))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task El_modal_solo_confirma_tras_ACK_y_conserva_valores_ante_error(bool exito)
    {
        var transporte = new Transporte("planner");
        var respuesta = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        transporte.Guardado = () => respuesta.Task;
        await WithRenderer(transporte, async (renderer, _) =>
        {
            DialogoVersionReceta dialogo = null!;
            await renderer.RenderComponentAsync<HostDialogo>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Catalogos"] = transporte.Catalogos(), ["Capturar"] = (Action<DialogoVersionReceta>)(x => dialogo = x) }));
            var modelo = (ModeloVersionReceta)typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
            modelo.Nombre = "QA texto privado"; modelo.ProductoResultadoId = transporte.Producto;
            modelo.Rendimiento = 1.5m; modelo.UnidadRendimientoId = "kg"; modelo.Instrucciones = "Conservar instrucciones";
            modelo.Ingredientes.Add(new() { ProductoId = transporte.Producto, Cantidad = 2.5m, UnidadMedidaId = "kg" });
            var clave = modelo.Ingredientes[0].Clave; var cierres = 0;
            var instancia = Doblar<IMudDialogInstance>((m, _) =>
            {
                if (m.Name == "get_Options") return new DialogOptions();
                if (m.Name == "SetOptionsAsync") return Task.CompletedTask;
                if (m.Name == "Close") cierres++;
                return null;
            });
            typeof(DialogoEdicionBase).GetProperty("Dialogo", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dialogo, instancia);
            var guardar = typeof(DialogoVersionReceta).GetMethod("GuardarAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pendiente = (Task)guardar.Invoke(dialogo, null)!;
            await (Task)guardar.Invoke(dialogo, null)!;
            Assert.True(dialogo.Guardando); Assert.Equal(0, cierres); Assert.Equal(1, transporte.Escrituras);
            if (exito) respuesta.SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new VersionRecetaDto { Id = Guid.NewGuid(), Nombre = modelo.Nombre, Revision = Guid.NewGuid(), CantidadRendimiento = 1.5m, UnidadRendimientoId = "kg" }) });
            else respuesta.SetResult(new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(new
            { type = "https://httpstatuses.io/409", title = "Conflicto", status = 409, instance = "/api/recipes", traceId = "qa-safe-reference", detail = "Los datos cambiaron o entran en conflicto con un registro guardado. Revise la información antes de reintentar." }) });
            await pendiente;
            Assert.Equal(exito ? 1 : 0, cierres); Assert.False(dialogo.Guardando);
            if (!exito)
            {
                Assert.Same(modelo, typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo));
                Assert.Equal("QA texto privado", modelo.Nombre); Assert.Equal(1.5m, modelo.Rendimiento);
                Assert.Equal("Conservar instrucciones", modelo.Instrucciones); Assert.Equal(clave, modelo.Ingredientes[0].Clave);
                Assert.Equal(2.5m, modelo.Ingredientes[0].Cantidad);
                Assert.Equal("Los datos cambiaron o entran en conflicto con un registro guardado. Revise la información antes de reintentar.", dialogo.Error);
            }
        }, expectedWrites: 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Los_errores_nuevos_conservan_solo_mensajes_curados_y_claves_indexadas(bool confiable)
    {
        var transporte = new Transporte("planner");
        transporte.Guardado = () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new
            {
                type = "https://httpstatuses.io/400", title = "Datos no válidos", status = 400, instance = "/api/recipes", traceId = "qa-safe-reference",
                errors = new Dictionary<string, string[]> { ["Ingredients[0].UnitId"] = [confiable ? "La unidad debe ser compatible con el ingrediente." : "SELECT password FROM diagnostic_stack"], ["YieldUnitId"] = ["Seleccione una unidad de rendimiento válida."] }
            })
        });
        using var cliente = new ClienteFoodTraceApi(new HttpClient(transporte) { BaseAddress = new Uri("https://example.invalid") });
        await cliente.IniciarSesionAsync(new IniciarSesionDto());
        var error = await Assert.ThrowsAsync<ExcepcionApi>(() => cliente.CrearRecetaAsync(new CrearRecetaDto()));
        Assert.Equal(confiable ? "La unidad debe ser compatible con el ingrediente." : "Revise el valor ingresado.", Assert.Single(error.ErroresValidacion["Ingredients[0].UnitId"]));
        Assert.Contains("Unidad del rendimiento", error.ErroresCampos.Keys);
    }

    [Fact]
    public async Task Un_resultado_anidado_tardio_no_puebla_otra_sesion_ni_abre_dos_dialogos()
    {
        var transporte = new Transporte("admin", "planner");
        await WithRenderer(transporte, async (renderer, _) =>
        {
            DialogoVersionReceta dialogo = null!;
            await renderer.RenderComponentAsync<HostDialogo>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Catalogos"] = transporte.Catalogos(), ["Capturar"] = (Action<DialogoVersionReceta>)(x => dialogo = x) }));
            var modelo = (ModeloVersionReceta)typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
            var fila = new ModeloIngredienteReceta(); modelo.Ingredientes.Add(fila); modelo.Nombre = "Privado";
            var fin = new TaskCompletionSource<DialogResult?>(); var aperturas = 0;
            var referencia = Doblar<IDialogReference>((m, _) => m.Name == "get_Result" ? fin.Task : null);
            var servicio = Doblar<IDialogService>((m, _) => { if (m.Name == "ShowAsync") { aperturas++; return Task.FromResult(referencia); } return null; });
            typeof(DialogoVersionReceta).GetProperty("Dialogos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(dialogo, servicio);
            typeof(DialogoEdicionBase).GetProperty("Dialogo", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dialogo, Doblar<IMudDialogInstance>((_, _) => null));
            var crear = typeof(DialogoVersionReceta).GetMethod("CrearProductoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pendiente = (Task)crear.Invoke(dialogo, [fila.Clave])!;
            await (Task)crear.Invoke(dialogo, [fila.Clave])!;
            var sesion = (ClienteFoodTraceApi)typeof(DialogoEdicionBase).GetProperty("Sesion", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
            sesion.CerrarSesion();
            fin.SetResult(DialogResult.Ok(new ProductoDto { Id = Guid.NewGuid(), Nombre = "QA respuesta antigua", UnidadBaseId = "kg" }));
            await pendiente;
            Assert.Equal(1, aperturas); Assert.Equal(Guid.Empty, fila.ProductoId);
            var actual = (ModeloVersionReceta)typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
            Assert.Empty(actual.Nombre); Assert.Empty(actual.Ingredientes);
        });
    }

    [Fact]
    public void Reabrir_usa_la_cantidad_y_unidad_elegidas_no_el_rendimiento_normalizado()
    {
        var version = new VersionRecetaDto { Rendimiento = 5, CantidadRendimiento = 5000, UnidadRendimientoId = "ml", PorcionesRendimiento = 10 };
        var modelo = ModeloVersionReceta.Desde(version);
        Assert.Equal(5000m, modelo.Rendimiento);
        Assert.Equal("ml", modelo.ParaCrear().UnidadRendimientoId);
        Assert.Equal(10m, modelo.ParaCrear().PorcionesRendimiento);
        Assert.True(modelo.Copiar().EquivaleA(version));
    }

    [Fact]
    public void Una_fila_nueva_tiene_busqueda_y_placeholder_sin_guid_vacio()
    {
        var editor = new EditorIngredientesReceta { Ingredientes = [new()], Catalogos = new Transporte("planner").Catalogos() };
        var parametros = FindComponent(Frames(editor), typeof(MudAutocomplete<Guid?>));
        Assert.Equal("Seleccioná un ingrediente", parametros["Placeholder"]);
        Assert.Null(parametros["Value"]);
        var texto = (Func<Guid?, string>)parametros["ToStringFunc"];
        Assert.Equal("", texto(Guid.Empty));
    }

    [Fact]
    public void Cambiar_ingrediente_preserva_la_unidad_si_sigue_siendo_compatible()
    {
        var transporte = new Transporte("planner");
        var catalogos = transporte.Catalogos();
        var otro = new ProductoDto { Id = Guid.NewGuid(), Nombre = "Harina", UnidadBaseId = "kg" };
        catalogos.Productos.Add(otro);
        var fila = new ModeloIngredienteReceta { ProductoId = transporte.Producto, UnidadMedidaId = "kg", Cantidad = 1.5m };
        var editor = new EditorIngredientesReceta { Ingredientes = [fila], Catalogos = catalogos };
        typeof(EditorIngredientesReceta).GetMethod("CambiarProductoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [fila, otro.Id]);
        Assert.Equal("kg", fila.UnidadMedidaId);
        Assert.Equal(1.5m, fila.Cantidad);
    }

    [Fact]
    public void Guardar_no_se_habilita_solo_por_existir_una_fila()
    {
        var dialogo = new DialogoVersionReceta { Catalogos = new Transporte("planner").Catalogos() };
        var modelo = (ModeloVersionReceta)typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialogo)!;
        modelo.Ingredientes.Add(new());
        Assert.False((bool)Component(Frames(dialogo), typeof(MarcoDialogoEdicion))["GuardarDisponible"]);
    }

    private sealed class Transporte(params string[] roles) : HttpMessageHandler
    {
        public bool Establecimiento = true, Publicada = true;
        public bool FallaCatalogo;
        public Func<Task<HttpResponseMessage>>? Guardado;
        public int Escrituras;
        public readonly Guid Producto = Guid.NewGuid(), Version = Guid.NewGuid();
        public List<FoodTrace.Dtos.Produccion.OrdenProduccionDto> Ordenes = [];
        public List<FoodTrace.Dtos.Produccion.NecesidadIngredienteDto> Necesidades = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (FallaCatalogo && path == "/api/catalogs") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = JsonContent.Create(new { message = "diagnóstico no confiable" }) });
            if (request.Method != HttpMethod.Get && path != "/auth/login") { Escrituras++; if (Guardado is not null) return Guardado(); }
            object body = path switch
            {
                "/auth/login" => new { accessToken = "local-test-only" },
                "/api/me" => new UsuarioActualDto { Id = "local-test", Roles = roles },
                "/api/recipes" => Publicada ? new List<VersionRecetaDto> { new() { Id = Version, Nombre = "Sopa", Publicada = true, NumeroVersion = 4, ProductoResultadoId = Producto } } : new List<VersionRecetaDto>(),
                "/api/production" => Ordenes,
                "/api/needs" => Necesidades,
                _ => Catalogos()
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
        public CatalogosDto Catalogos() => new()
        {
            Productos = [new() { Id = Producto, Nombre = "Verduras", UnidadBaseId = "kg" }],
            Establecimientos = Establecimiento ? [new() { Id = Guid.NewGuid(), Nombre = "Cocina" }] : [],
            UnidadesMedida = [new() { Id = "kg", Dimension = "mass", FactorConversion = 1 }]
        };
    }

    private static async Task WithRenderer(Transporte transport, Func<HtmlRenderer, EstadoAdministracion, Task> test, int expectedWrites = 0)
    {
        using var client = new ClienteFoodTraceApi(new HttpClient(transport) { BaseAddress = new Uri("https://example.invalid") });
        await client.IniciarSesionAsync(new IniciarSesionDto());
        using var state = new EstadoAdministracion(client);
        await using var services = ServiciosRender.Crear(client, state);
        await using var scope = services.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(() => test(renderer, state));
        Assert.Equal(expectedWrites, transport.Escrituras);
    }

    [Theory]
    [InlineData(false, true, "Falta un establecimiento.")]
    [InlineData(true, false, "Falta una versión de receta publicada.")]
    [InlineData(false, false, "Faltan un establecimiento y una versión de receta publicada.")]
    public async Task Los_requisitos_identifican_solo_lo_que_falta(bool establishment, bool published, string expected)
    {
        var transport = new Transporte("planner") { Establecimiento = establishment, Publicada = published };
        await WithRenderer(transport, async (renderer, _) =>
        {
            var html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
            Assert.Contains(expected, html);
            Assert.DoesNotContain("href=\"/catalogos?area=establecimientos\"", html);
            if (!establishment) Assert.Contains("Solicite a un administrador", html);
            Assert.DoesNotContain("Todas las necesidades brutas guardadas", html);
        });
    }

    [Theory]
    [InlineData("planner", true)]
    [InlineData("admin", false)]
    [InlineData("buyer", false)]
    [InlineData("approver", false)]
    [InlineData("operator", false)]
    [InlineData("admin,planner", true)]
    public async Task Las_acciones_no_convierten_admin_en_planificador(string roles, bool write)
    {
        await WithRenderer(new Transporte(roles.Split(',')) { Publicada = false }, async (renderer, _) =>
        {
            var recipes = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Recetas>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["EnContenedor"] = true }))).ToHtmlString());
            Assert.Equal(write, recipes.Contains(">Crear receta</span>", StringComparison.Ordinal));
            Assert.Contains("Cree una receta, publique una versión y luego planifique la producción.", recipes);
            Assert.Contains(">Actualizar</span>", recipes);
            var planning = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
            Assert.Equal(write, planning.Contains(">Planificar producción</span>", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Solo_admin_recibe_el_enlace_para_gestionar_el_establecimiento_faltante()
    {
        await WithRenderer(new Transporte("admin", "planner") { Establecimiento = false }, async (renderer, _) =>
        {
            var html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
            Assert.Contains("href=\"/catalogos?area=establecimientos\"", html);
            Assert.DoesNotContain("Solicite a un administrador", html);
        });
    }

    [Fact]
    public async Task Actualizar_recetas_conserva_la_revision_y_el_borrador_original()
    {
        await WithRenderer(new Transporte("planner"), async (renderer, state) =>
        {
            var original = new VersionRecetaDto { Id = Guid.NewGuid(), Revision = Guid.NewGuid(), Nombre = "Original" };
            state.Recetas.Seleccionar(original);
            state.Recetas.Formulario.Nombre = "Sin guardar";
            await renderer.RenderComponentAsync<Recetas>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["EnContenedor"] = true }));
            Assert.Same(original, state.Recetas.Seleccionada);
            Assert.Equal("Sin guardar", state.Recetas.Formulario.Nombre);
            Assert.True(state.Recetas.TieneCambios);
        });
    }

    [Theory]
    [InlineData("planner", false)]
    [InlineData("planner", true)]
    [InlineData("admin", false)]
    [InlineData("admin", true)]
    [InlineData("admin,planner", false)]
    [InlineData("admin,planner", true)]
    public async Task El_contexto_de_version_conserva_familia_y_acciones_segun_estado_y_rol(string roles, bool published)
    {
        await WithRenderer(new Transporte(roles.Split(',')), async (renderer, state) =>
        {
            var family = Guid.NewGuid();
            state.Recetas.Seleccionar(new VersionRecetaDto { Id = Guid.NewGuid(), FamiliaRecetaId = family, Nombre = "Versión seleccionada", NumeroVersion = 12, Publicada = published, Instrucciones = "Preparar sin modificar" });
            var html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Recetas>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["EnContenedor"] = true }))).ToHtmlString());
            var planner = roles.Split(',').Contains("planner");
            Assert.Contains(family.ToString(), html); Assert.Contains("versión 12", html);
            Assert.Equal(planner && !published, html.Contains(">Editar borrador</span>", StringComparison.Ordinal));
            Assert.Equal(planner && !published, html.Contains(">Publicar versión guardada</span>", StringComparison.Ordinal));
            Assert.Equal(planner && published, html.Contains(">Crear nueva versión</span>", StringComparison.Ordinal));
            Assert.DoesNotContain("<textarea", html);
        });
    }

    [Fact]
    public async Task El_modal_agrupa_campos_y_no_envia_un_modelo_invalido()
    {
        var transport = new Transporte("planner");
        await WithRenderer(transport, async (renderer, _) =>
        {
            var html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<FormularioVersionReceta>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Modelo"] = new ModeloVersionReceta(), ["Catalogos"] = transport.Catalogos(), ["PuedeEditar"] = true
            }))).ToHtmlString());
            Assert.Contains("Identidad y rendimiento", html);
            Assert.Contains("Ingredientes", html);
            Assert.Contains("Preparación", html);
            var called = 0;
            var form = new FormularioVersionReceta { Modelo = new(), Catalogos = transport.Catalogos(), PuedeEditar = true, Guardar = EventCallback.Factory.Create(this, () => called++) };
            await (Task)typeof(FormularioVersionReceta).GetMethod("GuardarValidoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null)!;
            Assert.Equal(0, called);
            using var services = new ServiceCollection().BuildServiceProvider();
            var context = new EditContext(form.Modelo);
            using var validation = context.EnableDataAnnotationsValidation(services);
            Assert.False(context.Validate());
            Assert.NotEmpty(context.GetValidationMessages(context.Field(nameof(ModeloVersionReceta.Nombre))));
        });
    }

    [Fact]
    public async Task Las_filas_usan_callbacks_reales_y_conservan_claves_y_unidades()
    {
        var transport = new Transporte("planner");
        var model = new ModeloVersionReceta();
        var context = new EditContext(model);
        var changes = 0;
        var editor = new EditorIngredientesReceta
        {
            Ingredientes = model.Ingredientes, Catalogos = transport.Catalogos(), Contexto = context,
            Cambiado = EventCallback.Factory.Create(this, () => changes++)
        };
        async Task Invoke(string name, params object[] args) => await (Task)typeof(EditorIngredientesReceta).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, args)!;
        await Invoke("AgregarAsync");
        await Invoke("AgregarAsync");
        Assert.Equal(2, model.Ingredientes.Count);
        Assert.True(context.IsModified(context.Field(nameof(model.Ingredientes))));
        var first = model.Ingredientes[0]; var secondKey = model.Ingredientes[1].Clave;
        first.UnidadMedidaId = "kg";
        await Invoke("CambiarProductoAsync", first, transport.Producto);
        Assert.Equal("kg", first.UnidadMedidaId);
        await Invoke("QuitarAsync", first);
        Assert.Equal(secondKey, Assert.Single(model.Ingredientes).Clave);
        editor.Ocupado = true;
        await Invoke("AgregarAsync");
        await Invoke("QuitarAsync", model.Ingredientes[0]);
        Assert.Single(model.Ingredientes);
        Assert.Equal(4, changes);
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    public async Task Solo_el_submit_valido_editable_invoca_guardar(bool busy, bool published, int expected)
    {
        var transport = new Transporte("planner");
        var saves = 0;
        var form = new FormularioVersionReceta
        {
            Modelo = new() { Nombre = "Sopa", ProductoResultadoId = transport.Producto, UnidadRendimientoId = "kg", Instrucciones = "Preparar", Ingredientes = [new() { ProductoId = transport.Producto, UnidadMedidaId = "kg", Cantidad = 2 }] },
            Catalogos = transport.Catalogos(), PuedeEditar = true, Ocupado = busy,
            Version = published ? new() { Publicada = true } : null,
            Guardar = EventCallback.Factory.Create(this, () => saves++)
        };
        await (Task)typeof(FormularioVersionReceta).GetMethod("GuardarValidoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null)!;
        Assert.Equal(expected, saves);
    }

    [Fact]
    public async Task Un_detalle_muestra_solo_sus_necesidades_y_conserva_el_enlace_de_orden()
    {
        var transport = new Transporte("planner");
        var selected = Guid.NewGuid(); var other = Guid.NewGuid();
        transport.Ordenes = [new() { Id = selected, VersionRecetaId = transport.Version, CantidadPlanificada = 2, FechaPlanificada = new DateOnly(2026, 11, 4) }];
        transport.Necesidades = [
            new() { OrdenProduccionId = selected, ProductoId = transport.Producto, CantidadNecesaria = 150, CantidadAsignada = 7, UnidadBaseId = "kg" },
            new() { OrdenProduccionId = other, ProductoId = transport.Producto, CantidadNecesaria = 999, CantidadAsignada = 0, UnidadBaseId = "kg" }
        ];
        await WithRenderer(transport, async (renderer, state) =>
        {
            state.Produccion.OrdenSeleccionadaId = selected;
            var html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
            Assert.Contains("150 kg", html); Assert.Contains("7 kg", html);
            Assert.Contains($"/produccion?orden={selected}", html);
            Assert.Contains("Verduras", html); Assert.DoesNotContain("999 kg", html);
            Assert.Contains("Sopa · v4", html); Assert.Contains("04/11/2026", html);
            Assert.Equal(1, html.Split("150 kg").Length - 1);
            state.Produccion.OrdenSeleccionadaId = null;
            var all = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<Produccion>()).ToHtmlString());
            Assert.Contains("150 kg", all); Assert.Contains("999 kg", all);
        });
    }

    [Fact]
    public async Task El_formulario_de_planificacion_rechaza_version_no_publicada_sin_callback()
    {
        var transport = new Transporte("planner"); var saves = 0;
        var form = new FormularioPlanificacion
        {
            Catalogos = transport.Catalogos(), Versiones = [new() { Id = transport.Version, Publicada = false }],
            Modelo = new ModeloPlanificacion { VersionRecetaId = transport.Version, UnidadMedidaId = "kg", Fecha = new DateTime(2026, 11, 4) },
            Guardar = EventCallback.Factory.Create(this, () => saves++)
        };
        await (Task)typeof(FormularioPlanificacion).GetMethod("PlanificarValidoAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null)!;
        Assert.Equal(0, saves);
    }

    public sealed class HostDialogo : ComponentBase
    {
        [Parameter] public CatalogosDto Catalogos { get; set; } = new();
        [Parameter] public Action<DialogoVersionReceta> Capturar { get; set; } = null!;
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DialogoVersionReceta>(0);
            builder.AddAttribute(1, "Catalogos", Catalogos);
            builder.AddComponentReferenceCapture(2, instance => Capturar((DialogoVersionReceta)instance));
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task Cancelar_un_modal_modificado_pide_descarte_y_permite_seguir_editando()
    {
        var transport = new Transporte("planner");
        await WithRenderer(transport, async (renderer, _) =>
        {
            DialogoVersionReceta dialog = null!;
            await renderer.RenderComponentAsync<HostDialogo>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Catalogos"] = transport.Catalogos(), ["Capturar"] = (Action<DialogoVersionReceta>)(instance => dialog = instance)
            }));
            var model = (ModeloVersionReceta)typeof(DialogoVersionReceta).GetField("modelo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
            model.Nombre = "Borrador sin enviar";
            await dialog.PedirCancelarAsync();
            Assert.True(dialog.ConfirmarDescarte); Assert.True(dialog.Bloqueado);
            dialog.SeguirEditando();
            Assert.False(dialog.ConfirmarDescarte); Assert.False(dialog.Bloqueado);
            Assert.Equal("Borrador sin enviar", model.Nombre);
        });
    }

    [Fact]
    public async Task Cerrar_sesion_limpia_los_borradores_y_las_selecciones_del_circuito()
    {
        using var client = new ClienteFoodTraceApi(new HttpClient(new Transporte("planner")) { BaseAddress = new Uri("https://example.invalid") });
        await client.IniciarSesionAsync(new IniciarSesionDto());
        using var state = new EstadoAdministracion(client);
        state.Recetas.Formulario.Nombre = "Privado";
        state.Produccion.OrdenSeleccionadaId = Guid.NewGuid();
        state.Produccion.Formulario.Cantidad = 123;
        client.CerrarSesion();
        Assert.False(state.Recetas.TieneCambios);
        Assert.Null(state.Produccion.OrdenSeleccionadaId);
        Assert.Equal(1, state.Produccion.Formulario.Cantidad);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Los_dos_hosts_conectan_el_submit_externo_con_su_formulario_real(bool recipe)
    {
        var transport = new Transporte("planner");
        DialogoEdicionBase dialog = recipe ? new DialogoVersionReceta { Catalogos = transport.Catalogos() }
            : new DialogoPlanificarProduccion { Catalogos = transport.Catalogos(), Versiones = [new() { Publicada = true }] };
        using var client = new ClienteFoodTraceApi(new HttpClient(transport) { BaseAddress = new Uri("https://example.invalid") });
        typeof(DialogoEdicionBase).GetProperty("Sesion", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dialog, client);
        var frame = Component(Frames(dialog), typeof(MarcoDialogoEdicion));
        var editorType = recipe ? typeof(FormularioVersionReceta) : typeof(FormularioPlanificacion);
        var editorParameters = Component(Frames((RenderFragment)frame["ChildContent"]), editorType);
        Assert.Equal(dialog.FormId, editorParameters["FormId"]);
        object editor = recipe ? new FormularioVersionReceta { PuedeEditar = true, Catalogos = transport.Catalogos(), FormId = (string)editorParameters["FormId"] }
            : new FormularioPlanificacion { Catalogos = transport.Catalogos(), Versiones = [new() { Publicada = true }], FormId = (string)editorParameters["FormId"] };
        var actualForm = FindComponent(Frames(editor), typeof(EditForm));
        Assert.Equal(dialog.FormId, actualForm["id"]);
        Assert.True(((EventCallback<EditContext>)actualForm["OnValidSubmit"]).HasDelegate);
        var shell = Component(Frames(new MarcoDialogoEdicion { Control = dialog }), typeof(MudDialog));
        var footer = Frames((RenderFragment)shell["DialogActions"]);
        Assert.Contains(footer, x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeName == "form" && Equals(x.AttributeValue, dialog.FormId));
    }

    private static RenderTreeFrame[] Frames(object component)
    {
        var builder = new RenderTreeBuilder();
        component.GetType().GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, [builder]);
        var range = builder.GetFrames();
        return range.Array.Take(range.Count).ToArray();
    }
    private static RenderTreeFrame[] Frames(RenderFragment content)
    {
        var builder = new RenderTreeBuilder(); content(builder);
        var range = builder.GetFrames(); return range.Array.Take(range.Count).ToArray();
    }
    private static Dictionary<string, object> Component(RenderTreeFrame[] frames, Type type)
    {
        var index = Array.FindIndex(frames, x => x.FrameType == RenderTreeFrameType.Component && x.ComponentType == type);
        Assert.True(index >= 0, $"Missing component {type.Name}");
        return frames.Skip(index + 1).TakeWhile(x => x.FrameType == RenderTreeFrameType.Attribute).ToDictionary(x => x.AttributeName, x => x.AttributeValue);
    }
    private static Dictionary<string, object> FindComponent(RenderTreeFrame[] frames, Type type)
    {
        if (frames.Any(x => x.FrameType == RenderTreeFrameType.Component && x.ComponentType == type)) return Component(frames, type);
        foreach (var frame in frames.Where(x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeValue is RenderFragment<EditContext>))
        {
            try { return FindComponent(Frames(((RenderFragment<EditContext>)frame.AttributeValue)(new EditContext(new object()))), type); } catch (InvalidOperationException) { }
        }
        foreach (var frame in frames.Where(x => x.FrameType == RenderTreeFrameType.Attribute && x.AttributeValue is RenderFragment))
        {
            try { return FindComponent(Frames((RenderFragment)frame.AttributeValue), type); } catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing nested component {type.Name}");
    }
}
