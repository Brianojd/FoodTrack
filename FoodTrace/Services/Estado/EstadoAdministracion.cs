using FoodTrace.Models.Administracion;
using FoodTrace.Services.Interfaces;

namespace FoodTrace.Services.Estado;

// Conserva borradores entre módulos del mismo circuito, nunca entre sesiones.
public sealed class EstadoAdministracion : IDisposable
{
    private readonly ISesionFoodTrace sesion;
    private long version;
    public FormularioNombre Establecimiento { get; set; } = new();
    public FormularioNombre Empresa { get; set; } = new();
    public FormularioProducto Producto { get; set; } = new();
    public FormularioNombre Proveedor { get; set; } = new();
    public FormularioPresentacion Presentacion { get; set; } = new();
    public FormularioUsuario UsuarioNuevo { get; set; } = new();
    public FormularioMembresia? Membresia { get; set; }
    public EstadoRecetas Recetas { get; private set; } = new();
    public EstadoProduccion Produccion { get; private set; } = new();
    public EstadoCompras Compras { get; private set; } = new();

    public EstadoAdministracion(ISesionFoodTrace sesion)
    {
        this.sesion = sesion;
        version = sesion.VersionSesion;
        sesion.SesionCambiada += CambiarSesion;
    }

    private void CambiarSesion()
    {
        if (version == sesion.VersionSesion) return;
        version = sesion.VersionSesion;
        Limpiar();
    }

    private void Limpiar()
    {
        UsuarioNuevo.Contrasena = "";
        Establecimiento = new(); Empresa = new(); Producto = new(); Proveedor = new();
        Presentacion = new(); UsuarioNuevo = new(); Membresia = null;
        Recetas = new(); Produccion = new();
        Compras = new();
    }

    public void Dispose() { sesion.SesionCambiada -= CambiarSesion; Limpiar(); }
}
