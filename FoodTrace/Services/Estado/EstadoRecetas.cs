using FoodTrace.Dtos.Comunes;
using FoodTrace.Dtos.Recetas;
using FoodTrace.Models.Recetas;

namespace FoodTrace.Services.Estado;

public sealed class EstadoRecetas
{
    public CatalogosDto? Catalogos { get; set; }
    public List<VersionRecetaDto> Versiones { get; set; } = [];
    public VersionRecetaDto? Seleccionada { get; private set; }
    public ModeloVersionReceta Formulario { get; private set; } = new();
    public bool TieneCambios => Seleccionada is null ? Formulario.TieneContenido : !Formulario.EquivaleA(Seleccionada);

    public void Seleccionar(VersionRecetaDto? version)
    {
        Seleccionada = version;
        Formulario = version is null ? new() : ModeloVersionReceta.Desde(version, Catalogos);
    }

    public void AceptarGuardada(VersionRecetaDto version)
    {
        Versiones = Versiones.Where(x => x.Id != version.Id).Append(version).ToList();
        Seleccionar(version);
    }
}
