using FoodTrace.Mobile.Models.Recepcion;

namespace FoodTrace.Mobile.Models.Salidas;

public sealed class LoteInventarioDto
{
    public Guid Id { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public Guid ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal SaldoDisponible { get; set; }
    public string UnidadBaseId { get; set; } = "kg";
    public DateTimeOffset FechaIngreso { get; set; }
    public bool Cerrado { get; set; }

    public string SaldoFormateado =>
        $"{FormatoCantidad.Formatear(SaldoDisponible)} {UnidadBaseId}";
}

public sealed class ValidacionLoteFifoResultado
{
    public bool EsValido { get; init; }
    public LoteInventarioDto? Lote { get; init; }
    public string MensajeError { get; init; } = string.Empty;
}

public sealed class SalidaCocinaResultadoDto
{
    public string ReferenciaSalida { get; init; } = string.Empty;
    public string CodigoLote { get; init; } = string.Empty;
    public string NombreProducto { get; init; } = string.Empty;
    public decimal CantidadDescontada { get; init; }
    public decimal SaldoRestante { get; init; }
    public string UnidadBaseId { get; init; } = "kg";
    public bool CajaCerrada { get; init; }
    public DateTimeOffset FechaSalida { get; init; }

    public string CantidadDescontadaTexto =>
        $"{FormatoCantidad.Formatear(CantidadDescontada)} {UnidadBaseId}";
}
