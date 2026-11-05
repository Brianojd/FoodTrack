using FoodTrace.Dtos.Autenticacion;

namespace FoodTrace.Services.Interfaces;

public interface ISesionFoodTrace
{
    UsuarioActualDto? Identidad { get; }
    long VersionSesion { get; }
    string AvisoSesion { get; }
    event Action? SesionCambiada;
    bool TieneRol(string rol);
    Task<UsuarioActualDto> IniciarSesionAsync(IniciarSesionDto entrada, CancellationToken cancelacion = default);
    void CerrarSesion();
}
