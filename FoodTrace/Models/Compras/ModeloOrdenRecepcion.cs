using FoodTrace.Dtos.Compras;

namespace FoodTrace.Models.Compras;

public sealed class ModeloOrdenRecepcion
{
    public List<OrdenRecepcionDto> Ordenes { get; private set; } = [];
    public OrdenRecepcionDto? Seleccionada { get; private set; }

    public void Reemplazar(IEnumerable<OrdenRecepcionDto> ordenes)
    {
        Ordenes = ordenes.ToList();
        Seleccionada = Seleccionada is null ? null : Ordenes.SingleOrDefault(x => x.Id == Seleccionada.Id);
    }

    public void Seleccionar(OrdenRecepcionDto orden) => Seleccionada = orden;

    public static string NombreEstado(int estado) => estado == 0 ? "Pendiente" : "Estado no reconocido";
}
