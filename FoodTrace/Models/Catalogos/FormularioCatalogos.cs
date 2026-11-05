using FoodTrace.Dtos.Usuarios;

namespace FoodTrace.Models.Catalogos;

public sealed class FormularioCatalogos
{
    public string NombreEmpresa { get; set; } = "";
    public string NombreEstablecimiento { get; set; } = "";
    public string NombreProducto { get; set; } = "";
    public string UnidadBaseId { get; set; } = "kg";
    public string NombreProveedor { get; set; } = "";
    public Guid ProveedorPresentacionId { get; set; }
    public Guid ProductoPresentacionId { get; set; }
    public string NombrePresentacion { get; set; } = "";
    public string UnidadPresentacionId { get; set; } = "kg";
    public decimal CantidadPresentacion { get; set; } = 1;
    public string CorreoNuevo { get; set; } = "";
    public string ContrasenaNueva { get; set; } = "";
    public HashSet<string> RolesNuevos { get; set; } = ["planner"];
    public HashSet<string> RolesEdicion { get; set; } = [];
    public UsuarioDto? UsuarioEdicion { get; set; }
    public bool UsuarioHabilitado { get; set; }

    public void Limpiar()
    {
        UsuarioEdicion = null; ContrasenaNueva = ""; CorreoNuevo = "";
        NombreEmpresa = ""; NombreEstablecimiento = ""; NombreProducto = ""; NombreProveedor = ""; NombrePresentacion = "";
        ProveedorPresentacionId = ProductoPresentacionId = Guid.Empty;
    }
}
