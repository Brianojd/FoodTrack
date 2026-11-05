using FoodTrace.Services.Api;

namespace FoodTrace.Models.Validacion;

// UI lifetime only; the HTTP exception remains the transport contract.
public sealed class EstadoValidacionServidor
{
    public long Version { get; private set; }
    public IReadOnlyDictionary<string, string[]> Errores { get; private set; } = new Dictionary<string, string[]>();
    public IReadOnlyList<string> Generales { get; private set; } = [];
    public event Action? Cambio;
    public event Action? Capturar;
    private bool envioCapturado;

    public void CapturarEnvio() { Capturar?.Invoke(); envioCapturado = true; }
    public void IniciarSolicitud()
    {
        if (!envioCapturado) Capturar?.Invoke();
        envioCapturado = false;
        Limpiar();
    }
    public void Publicar(ExcepcionApi respuesta)
    {
        Errores = respuesta.ErroresValidacion.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.Ordinal);
        Generales = Errores.Values.SelectMany(x => x).ToArray();
        Version++;
        Cambio?.Invoke();
    }
    public void InformarVinculadas(ISet<string> claves) => Generales = Errores.Where(x => !claves.Contains(x.Key)).SelectMany(x => x.Value).ToArray();
    public void Limpiar()
    {
        Errores = new Dictionary<string, string[]>();
        Generales = [];
        Version++;
        Cambio?.Invoke();
    }
}
