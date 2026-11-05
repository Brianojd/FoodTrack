using Microsoft.AspNetCore.Components.Forms;

namespace FoodTrace.Models.Validacion;

public sealed class MapaCamposServidor(object modelo)
{
    private readonly Dictionary<string, FieldIdentifier> campos = new(StringComparer.OrdinalIgnoreCase);
    public MapaCamposServidor Campo(object propietario, string propiedad, params string[] claves)
    {
        var campo = new FieldIdentifier(propietario, propiedad);
        foreach (var clave in claves) campos.Add(clave, campo);
        return this;
    }
    public bool Resolver(string clave, out FieldIdentifier campo) => campos.TryGetValue(Normalizar(clave), out campo);
    public bool Contiene(FieldIdentifier campo) => campo.Equals(new FieldIdentifier(modelo, "")) || campos.Values.Contains(campo);
    public FieldIdentifier General => new(modelo, "");

    private static string Normalizar(string clave)
    {
        if (clave.StartsWith("$.")) clave = clave[2..];
        // Only known body-parameter prefixes; never strip arbitrary ancestors/last segments.
        while (clave.IndexOf('.') is var punto && punto > 0)
        {
            var prefijo = clave[..punto];
            if (!new[] { "entrada", "input", "body", "request", "model", "dto" }.Contains(prefijo, StringComparer.OrdinalIgnoreCase)) break;
            clave = clave[(punto + 1)..];
        }
        return clave;
    }
}
