using System.ComponentModel.DataAnnotations;
using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Models.Administracion;
using Microsoft.AspNetCore.Components.Forms;

namespace FoodTrace.Models.Recetas;

public sealed class ModeloVersionReceta : IValidatableObject
{
    [Required(ErrorMessage = "Ingrese el nombre de la receta.")]
    [StringLength(500, ErrorMessage = "El nombre admite hasta 500 caracteres.")]
    public string Nombre { get; set; } = "";
    public Guid ProductoResultadoId { get; set; }
    [CantidadPositiva]
    public decimal Rendimiento { get; set; } = 1;
    public string UnidadRendimientoId { get; set; } = "";
    public decimal? PorcionesRendimiento { get; set; }
    [Required(ErrorMessage = "Ingrese las instrucciones.")]
    [StringLength(8000, ErrorMessage = "Las instrucciones admiten hasta 8.000 caracteres.")]
    public string Instrucciones { get; set; } = "";
    public List<ModeloIngredienteReceta> Ingredientes { get; set; } = [];

    public bool TieneContenido => !string.IsNullOrEmpty(Nombre) || ProductoResultadoId != Guid.Empty || Rendimiento != 1 || UnidadRendimientoId.Length != 0 || PorcionesRendimiento is not null || !string.IsNullOrEmpty(Instrucciones) || Ingredientes.Count != 0;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductoResultadoId == Guid.Empty) yield return new("Seleccione el producto resultado.", [nameof(ProductoResultadoId)]);
        if (Ingredientes.Count is < 1 or > 200) yield return new("Agregue entre uno y 200 ingredientes.", [nameof(Ingredientes)]);
        for (var i = 0; i < Ingredientes.Count; i++)
        {
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(Ingredientes[i], new ValidationContext(Ingredientes[i]), errores, true);
            foreach (var error in errores) yield return new($"Ingrediente {i + 1}: {error.ErrorMessage}", [nameof(Ingredientes)]);
        }
    }

    public Dictionary<FieldIdentifier, string> ValidarCampos(CatalogosDto catalogos)
    {
        var campos = new Dictionary<FieldIdentifier, string>();
        void Error(object modelo, string campo, string mensaje) => campos[new(modelo, campo)] = mensaje;
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), errores, true);
        foreach (var error in errores)
            foreach (var campo in error.MemberNames) Error(this, campo, error.ErrorMessage ?? "Revise el formulario.");
        var producto = catalogos.Productos.FirstOrDefault(x => x.Id == ProductoResultadoId);
        if (producto is null) Error(this, nameof(ProductoResultadoId), "Seleccione un producto elaborado disponible.");
        var unidad = CompatibilidadUnidades.ParaProducto(catalogos, ProductoResultadoId).FirstOrDefault(x => x.Id == UnidadRendimientoId);
        if (unidad is null) Error(this, nameof(UnidadRendimientoId), "Seleccione una unidad compatible con el producto elaborado.");
        else if (producto is not null && Rendimiento > 0)
        {
            var unidadBase = catalogos.UnidadesMedida.First(x => x.Id == producto.UnidadBaseId);
            try
            {
                var cantidadBase = Rendimiento * unidad.FactorConversion / unidadBase.FactorConversion;
                if (!CantidadValida(cantidadBase)) Error(this, nameof(Rendimiento), "El rendimiento convertido debe ser positivo y representable con seis decimales, hasta 1.000.000.000.");
            }
            catch (Exception ex) when (ex is OverflowException or DivideByZeroException)
            { Error(this, nameof(Rendimiento), "El rendimiento convertido supera el rango admitido."); }
        }
        if (PorcionesRendimiento is { } porciones && !CantidadValida(porciones))
            Error(this, nameof(PorcionesRendimiento), "Indique porciones positivas de hasta 1.000.000.000 y seis decimales, o deje el campo vacío.");
        for (var i = 0; i < Ingredientes.Count; i++)
        {
            var fila = Ingredientes[i];
            if (!catalogos.Productos.Any(x => x.Id == fila.ProductoId)) Error(fila, nameof(fila.ProductoId), "Seleccione un ingrediente disponible.");
            if (!CantidadValida(fila.Cantidad)) Error(fila, nameof(fila.Cantidad), "Indique una cantidad positiva de hasta 1.000.000.000 y seis decimales.");
            if (!CompatibilidadUnidades.ParaProducto(catalogos, fila.ProductoId).Any(x => x.Id == fila.UnidadMedidaId))
                Error(fila, nameof(fila.UnidadMedidaId), "Seleccione una unidad compatible con el ingrediente.");
            else if (CantidadValida(fila.Cantidad))
            {
                var unidadFila = catalogos.UnidadesMedida.First(x => x.Id == fila.UnidadMedidaId);
                var productoFila = catalogos.Productos.First(x => x.Id == fila.ProductoId);
                var baseFila = catalogos.UnidadesMedida.First(x => x.Id == productoFila.UnidadBaseId);
                try
                {
                    if (fila.Cantidad * unidadFila.FactorConversion / baseFila.FactorConversion > 1_000_000_000m)
                        Error(fila, nameof(fila.Cantidad), "La cantidad convertida supera el rango admitido.");
                }
                catch (Exception ex) when (ex is OverflowException or DivideByZeroException)
                { Error(fila, nameof(fila.Cantidad), "La cantidad convertida supera el rango admitido."); }
            }
        }
        return campos;
    }
    public static bool CantidadValida(decimal valor) => valor > 0 && valor <= 1_000_000_000m && decimal.Round(valor, 6) == valor;
    public List<string> Validar(CatalogosDto catalogos) => ValidarCampos(catalogos).Values.Distinct().ToList();

    public static ModeloVersionReceta Desde(VersionRecetaDto version, CatalogosDto? catalogos = null) => new()
    {
        Nombre = version.Nombre, ProductoResultadoId = version.ProductoResultadoId, Rendimiento = version.CantidadRendimiento ?? version.Rendimiento, Instrucciones = version.Instrucciones,
        UnidadRendimientoId = version.UnidadRendimientoId ?? catalogos?.Productos.FirstOrDefault(x => x.Id == version.ProductoResultadoId)?.UnidadBaseId ?? "",
        PorcionesRendimiento = version.PorcionesRendimiento,
        Ingredientes = version.Ingredientes.Select(x => new ModeloIngredienteReceta { ProductoId = x.ProductoId, UnidadMedidaId = x.UnidadMedidaId, Cantidad = x.Cantidad }).ToList()
    };

    public bool EquivaleA(VersionRecetaDto version) => Nombre == version.Nombre && ProductoResultadoId == version.ProductoResultadoId && Rendimiento == (version.CantidadRendimiento ?? version.Rendimiento) && Instrucciones == version.Instrucciones
        && (version.UnidadRendimientoId is null || UnidadRendimientoId == version.UnidadRendimientoId) && PorcionesRendimiento == version.PorcionesRendimiento
        && Ingredientes.Select(x => (x.ProductoId, x.UnidadMedidaId, x.Cantidad)).SequenceEqual(version.Ingredientes.Select(x => (x.ProductoId, x.UnidadMedidaId, x.Cantidad)));

    public ModeloVersionReceta Copiar() => new()
    {
        Nombre = Nombre, ProductoResultadoId = ProductoResultadoId, Rendimiento = Rendimiento, Instrucciones = Instrucciones,
        UnidadRendimientoId = UnidadRendimientoId, PorcionesRendimiento = PorcionesRendimiento,
        Ingredientes = Ingredientes.Select(x => new ModeloIngredienteReceta
        {
            Clave = x.Clave, ProductoId = x.ProductoId, UnidadMedidaId = x.UnidadMedidaId, Cantidad = x.Cantidad
        }).ToList()
    };

    public bool EquivaleA(ModeloVersionReceta modelo) => Nombre == modelo.Nombre && ProductoResultadoId == modelo.ProductoResultadoId && Rendimiento == modelo.Rendimiento && Instrucciones == modelo.Instrucciones
        && UnidadRendimientoId == modelo.UnidadRendimientoId && PorcionesRendimiento == modelo.PorcionesRendimiento
        && Ingredientes.Select(x => (x.ProductoId, x.UnidadMedidaId, x.Cantidad)).SequenceEqual(modelo.Ingredientes.Select(x => (x.ProductoId, x.UnidadMedidaId, x.Cantidad)));

    public IngredienteRecetaEntradaDto[] ObtenerIngredientes() => Ingredientes.Select(x => new IngredienteRecetaEntradaDto { ProductoId = x.ProductoId, UnidadMedidaId = x.UnidadMedidaId, Cantidad = x.Cantidad }).ToArray();
    public CrearRecetaDto ParaCrear() => new() { Nombre = Nombre.Trim(), ProductoResultadoId = ProductoResultadoId, Rendimiento = Rendimiento, UnidadRendimientoId = UnidadRendimientoId, PorcionesRendimiento = PorcionesRendimiento, Instrucciones = Instrucciones.Trim(), Ingredientes = ObtenerIngredientes() };
    public ActualizarRecetaDto ParaActualizar(Guid revision) => new() { Revision = revision, Nombre = Nombre.Trim(), ProductoResultadoId = ProductoResultadoId, Rendimiento = Rendimiento, UnidadRendimientoId = UnidadRendimientoId, PorcionesRendimiento = PorcionesRendimiento, Instrucciones = Instrucciones.Trim(), Ingredientes = ObtenerIngredientes() };
}
