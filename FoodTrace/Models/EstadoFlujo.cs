using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Usuarios;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Dtos.Produccion;
using FoodTrace.Dtos.Compras;
using FoodTrace.Models.Catalogos;
using FoodTrace.Models.Recetas;
using FoodTrace.Models.Produccion;
using FoodTrace.Models.Compras;

namespace FoodTrace.Models;

// Inicio conserva esta instancia para mantener los borradores al desmontar un módulo.
public sealed class EstadoFlujo
{
    public string Correo { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public string Error { get; set; } = "";
    public string Aviso { get; set; } = "";
    public string Seccion { get; set; } = "Catalogos";
    public bool Ocupado { get; set; }
    public CatalogosDto? Catalogos { get; set; }
    public List<VersionRecetaDto> Recetas { get; set; } = [];
    public List<OrdenProduccionDto> Produccion { get; set; } = [];
    public List<PedidoCompraDto> PedidosCompra { get; set; } = [];
    public List<NecesidadIngredienteDto> Necesidades { get; set; } = [];
    public List<UsuarioDto> Usuarios { get; set; } = [];
    public FormularioCatalogos FormularioCatalogos { get; } = new();
    public FormularioReceta FormularioReceta { get; } = new();
    public FormularioProduccion FormularioProduccion { get; } = new();
    public FormularioCompras FormularioCompras { get; } = new();

    public void LimpiarVistas()
    {
        Catalogos = null; Recetas.Clear(); Produccion.Clear(); PedidosCompra.Clear(); Necesidades.Clear(); Usuarios.Clear();
        FormularioReceta.Restablecer(); FormularioCatalogos.Limpiar(); FormularioCompras.Limpiar();
        FormularioProduccion.EstablecimientoId = FormularioProduccion.VersionRecetaId = Guid.Empty;
    }

    public void RestablecerSeleccionesConflicto()
    {
        FormularioReceta.RecetaEdicion = null;
        FormularioCompras.RestablecerSeleccion();
    }

    public string NombreProducto(Guid id) => Catalogos?.Productos.FirstOrDefault(x => x.Id == id)?.Nombre ?? id.ToString();
    public string UnidadProducto(Guid id) => Catalogos?.Productos.FirstOrDefault(x => x.Id == id)?.UnidadBaseId ?? "";
    public string NombreProveedor(Guid id) => Catalogos?.Proveedores.FirstOrDefault(x => x.Id == id)?.Nombre ?? id.ToString();
    public string NombreReceta(Guid id)
    {
        var receta = Recetas.FirstOrDefault(x => x.Id == id);
        return receta is null ? id.ToString() : $"{receta.Nombre} v{receta.NumeroVersion}";
    }
    public string NombreActor(string? id) => id is null ? "—" : Usuarios.FirstOrDefault(x => x.Id == id)?.Correo ?? id;
    public string ProduccionNecesidad(Guid id) => Necesidades.FirstOrDefault(x => x.Id == id)?.OrdenProduccionId.ToString() ?? id.ToString();
}
