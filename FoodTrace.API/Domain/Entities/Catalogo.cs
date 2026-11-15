namespace FoodTrace.API.Domain.Entities;

public sealed class UnidadMedida
{
    public string Id { get; set; } = "";
    public string Dimension { get; set; } = "";
    public decimal FactorConversion { get; set; }
}

public sealed class Producto : EntidadEmpresa
{
    public string Nombre { get; set; } = "";
    public string UnidadBaseId { get; set; } = "";
}

public sealed class Proveedor : EntidadEmpresa
{
    public string Nombre { get; set; } = "";
}

public sealed class PresentacionProveedor : EntidadEmpresa
{
    public Guid ProveedorId { get; set; }
    public Guid ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public string UnidadMedidaId { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal CantidadBase { get; set; }
}
