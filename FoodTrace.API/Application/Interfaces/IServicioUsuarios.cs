using FoodTrace.API.Dtos.Usuarios;

namespace FoodTrace.API.Application.Interfaces;

public interface IServicioUsuarios
{
    Task<List<UsuarioDto>> ObtenerUsuariosAsync(CancellationToken cancelacion = default);
    Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto entrada, CancellationToken cancelacion = default);
    Task<UsuarioDto> ActualizarMembresiaAsync(string id, ActualizarMembresiaDto entrada, CancellationToken cancelacion = default);
}
