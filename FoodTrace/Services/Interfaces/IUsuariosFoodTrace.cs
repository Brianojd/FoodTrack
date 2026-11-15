using FoodTrace.Dtos.Usuarios;

namespace FoodTrace.Services.Interfaces;

public interface IUsuariosFoodTrace
{
    Task<List<UsuarioDto>> ObtenerUsuariosAsync(CancellationToken cancelacion = default);
    Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto entrada, CancellationToken cancelacion = default);
    Task<UsuarioDto> ActualizarMembresiaAsync(string id, ActualizarMembresiaDto entrada, CancellationToken cancelacion = default);
}
