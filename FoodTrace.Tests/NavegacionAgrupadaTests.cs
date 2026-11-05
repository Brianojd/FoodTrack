using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FoodTrace.Components.Layout;
using FoodTrace.Dtos.Autenticacion;
using FoodTrace.Services.Interfaces;
using FoodTrace.Services.Estado;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Xunit;

namespace FoodTrace.Tests;

public sealed class NavegacionAgrupadaTests
{
    private sealed class Sesion(string rol) : ISesionFoodTrace
    {
        public UsuarioActualDto? Identidad { get; private set; } = new() { Correo = "fixture@example.invalid", Roles = [rol] };
        public long VersionSesion { get; private set; }
        public string AvisoSesion => "";
        public event Action? SesionCambiada;
        public bool TieneRol(string valor) => Identidad?.Roles.Contains(valor) == true;
        public Task<UsuarioActualDto> IniciarSesionAsync(IniciarSesionDto entrada, CancellationToken cancelacion = default) => throw new NotSupportedException();
        public void CerrarSesion() { Identidad = null; VersionSesion++; SesionCambiada?.Invoke(); }
    }

    private sealed class Navegacion(string ruta) : NavigationManager
    {
        public Navegacion() : this("/") { }
        public void Inicializar() => Initialize("https://fixture.invalid/", "https://fixture.invalid" + ruta);
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class Js : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => ValueTask.FromResult(default(T)!);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(T)!);
    }

    [Theory]
    [InlineData("admin", "/", 5, "Inicio")]
    [InlineData("admin", "/?origen=inicio", 5, "Inicio")]
    [InlineData("admin", "/usuarios", 5, "Usuarios")]
    [InlineData("planner", "/produccion?orden=00000000-0000-0000-0000-000000000001", 4, "Producción")]
    [InlineData("planner", "/produccion?area=recetas", 4, "Producción")]
    [InlineData("buyer", "/compras?area=proveedores", 4, "Compras")]
    [InlineData("approver", "/catalogos?area=establecimientos", 4, "Catálogos")]
    public async Task Menu_expone_destinos_planos_y_un_solo_activo(string rol, string ruta, int enlaces, string activo)
    {
        var nav = new Navegacion(ruta);
        nav.Inicializar();
        var servicios = new ServiceCollection().AddLogging();
        servicios.AddSingleton<ISesionFoodTrace>(new Sesion(rol));
        servicios.AddSingleton<NavigationManager>(nav);
        servicios.AddSingleton<IJSRuntime, Js>();
        servicios.AddMudServices();
        await using var provider = servicios.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var html = WebUtility.HtmlDecode((await renderer.RenderComponentAsync<NavMenu>()).ToHtmlString());
            Assert.Equal(enlaces, Regex.Matches(html, "<a\\b").Count);
            Assert.DoesNotContain("mud-nav-group", html);
            Assert.Contains("General", html);
            Assert.Contains("Operación", html);
            Assert.Contains("Gestión", html);
            Assert.Equal(rol == "admin", html.Contains("Administración", StringComparison.Ordinal));
            var seleccionados = Regex.Matches(html, "<a[^>]*class=\"[^\"]*\\bactive\\b[^\"]*\"[^>]*>(.*?)</a>", RegexOptions.Singleline);
            Assert.Single(seleccionados.Cast<Match>());
            Assert.Contains(activo, seleccionados[0].Value);
        });
    }

    [Theory]
    [InlineData("/recetas", "/produccion", "recetas")]
    [InlineData("/productos", "/catalogos", "productos")]
    [InlineData("/establecimientos", "/catalogos", "establecimientos")]
    [InlineData("/proveedores", "/compras", "proveedores")]
    public void Legados_conservan_query_repetida_fragmento_y_seleccion(string legado, string destino, string area)
    {
        var ruta = EstadoNavegacion.DestinoLegado("https://fixture.invalid" + legado + "?filtro=a%26b&filtro=c&seleccion=42&area=desconocida#detalle");
        var uri = new Uri("https://fixture.invalid" + ruta);
        Assert.Equal(destino, uri.AbsolutePath);
        var query = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal(area, query["area"]);
        Assert.Equal(new[] { "a&b", "c" }, query["filtro"].ToArray());
        Assert.Equal("42", query["seleccion"]);
        Assert.Equal("#detalle", uri.Fragment);
    }

    [Fact]
    public void Areas_recuerdan_solo_su_circuito_honran_query_y_se_reinician_con_sesion()
    {
        var sesion = new Sesion("planner");
        using var estado = new EstadoNavegacion(sesion);
        using var otraSesion = new EstadoNavegacion(new Sesion("planner"));
        Assert.Equal("planificacion", estado.ResolverArea("produccion", null));
        Assert.Equal("recetas", estado.ResolverArea("produccion", "recetas"));
        Assert.Equal("recetas", estado.ResolverArea("produccion", null));
        Assert.Equal("planificacion", otraSesion.ResolverArea("produccion", null));
        Assert.Equal("proveedores", estado.ResolverArea("compras", "proveedores"));
        Assert.Equal("establecimientos", estado.ResolverArea("catalogos", "establecimientos"));
        Assert.Equal("planificacion", estado.ResolverArea("produccion", "recetas", ordenExplicita: true));
        Assert.Equal("productos", estado.ResolverArea("catalogos", "area-invalida"));
        sesion.CerrarSesion();
        Assert.Equal("pedidos", estado.ResolverArea("compras", null));
        Assert.Equal("productos", estado.ResolverArea("catalogos", null));
        Assert.Equal("planificacion", estado.ResolverArea("produccion", null));
    }

    [Fact]
    public void Orden_explicita_tiene_prioridad_y_salir_de_planificacion_no_pierde_otros_parametros()
    {
        const string original = "https://fixture.invalid/recetas?orden=00000000-0000-0000-0000-000000000001&filtro=vigente#detalle";
        var legado = EstadoNavegacion.DestinoLegado(original)!;
        var uri = new Uri("https://fixture.invalid" + legado);
        Assert.Equal("planificacion", QueryHelpers.ParseQuery(uri.Query)["area"]);
        Assert.True(QueryHelpers.ParseQuery(uri.Query).ContainsKey("orden"));
        var recetas = new Uri("https://fixture.invalid" + EstadoNavegacion.RutaArea(uri.ToString(), "produccion", "recetas"));
        var query = QueryHelpers.ParseQuery(recetas.Query);
        Assert.False(query.ContainsKey("orden"));
        Assert.Equal("recetas", query["area"]);
        Assert.Equal("vigente", query["filtro"]);
        Assert.Equal("#detalle", recetas.Fragment);
        Assert.Null(EstadoNavegacion.DestinoLegado("https://fixture.invalid/usuarios"));
        Assert.True(EstadoNavegacion.CerrarDrawerAlNavegar(escritorio: false));
        Assert.False(EstadoNavegacion.CerrarDrawerAlNavegar(escritorio: true));
    }
}
