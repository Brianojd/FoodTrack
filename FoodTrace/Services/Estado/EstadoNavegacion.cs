using FoodTrace.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace FoodTrace.Services.Estado;

// Circuit-scoped navigation preferences; query strings remain the shareable source of truth.
public sealed class EstadoNavegacion : IDisposable
{
    private readonly ISesionFoodTrace sesion;
    private long version;
    private readonly Dictionary<string, string> areas = new(StringComparer.Ordinal);

    public EstadoNavegacion(ISesionFoodTrace sesion)
    {
        this.sesion = sesion;
        version = sesion.VersionSesion;
        sesion.SesionCambiada += CambiarSesion;
    }

    public static string[] Areas(string modulo) => modulo switch
    {
        "produccion" => ["planificacion", "recetas"],
        "compras" => ["pedidos", "proveedores"],
        "catalogos" => ["productos", "unidades", "establecimientos"],
        _ => throw new ArgumentOutOfRangeException(nameof(modulo))
    };

    public string ResolverArea(string modulo, string? area, bool ordenExplicita = false)
    {
        var disponibles = Areas(modulo);
        var seleccionada = ordenExplicita && modulo == "produccion" ? disponibles[0]
            : area is null && areas.TryGetValue(modulo, out var anterior) ? anterior
            : disponibles.Contains(area, StringComparer.Ordinal) ? area! : disponibles[0];
        areas[modulo] = seleccionada;
        return seleccionada;
    }

    public static string RutaArea(string uri, string modulo, string area)
    {
        var actual = new Uri(uri);
        var query = QueryHelpers.ParseQuery(actual.Query);
        query["area"] = area;
        // A deep order link selects planning; leaving that tab makes the URL unambiguous.
        if (modulo == "produccion" && area != "planificacion") query.Remove("orden");
        return QueryHelpers.AddQueryString("/" + modulo, query) + actual.Fragment;
    }

    public static string? DestinoLegado(string uri)
    {
        var actual = new Uri(uri);
        var destino = actual.AbsolutePath.TrimEnd('/').ToLowerInvariant() switch
        {
            "/recetas" => ("produccion", "recetas"),
            "/proveedores" => ("compras", "proveedores"),
            "/productos" => ("catalogos", "productos"),
            "/establecimientos" => ("catalogos", "establecimientos"),
            _ => default
        };
        if (destino == default) return null;
        // Legacy redirects preserve even repeated/unknown query keys and fragments.
        var query = QueryHelpers.ParseQuery(actual.Query);
        query["area"] = destino.Item1 == "produccion" && query.ContainsKey("orden") ? "planificacion" : destino.Item2;
        return QueryHelpers.AddQueryString("/" + destino.Item1, query) + actual.Fragment;
    }

    public static bool CerrarDrawerAlNavegar(bool escritorio) => !escritorio;

    private void CambiarSesion()
    {
        if (version == sesion.VersionSesion) return;
        version = sesion.VersionSesion;
        areas.Clear();
    }

    public void Dispose() { sesion.SesionCambiada -= CambiarSesion; areas.Clear(); }
}
