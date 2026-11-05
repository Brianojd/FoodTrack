using FoodTrace.Models.Inicio;
using FoodTrace.Services.Interfaces;

namespace FoodTrace.Services.Estado;

// Retains the last good dashboard only within the current session/circuit.
public sealed class EstadoInicio : IDisposable
{
    private readonly ISesionFoodTrace sesion;
    private long version;
    public ResumenInicio? Documento { get; set; }
    public EstadoInicio(ISesionFoodTrace sesion)
    {
        this.sesion = sesion;
        version = sesion.VersionSesion;
        sesion.SesionCambiada += CambiarSesion;
    }
    private void CambiarSesion()
    {
        if (version == sesion.VersionSesion) return;
        version = sesion.VersionSesion;
        Documento = null;
    }
    public void Dispose() { sesion.SesionCambiada -= CambiarSesion; Documento = null; }
}
