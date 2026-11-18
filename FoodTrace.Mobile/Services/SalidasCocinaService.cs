using FoodTrace.Mobile.Models.Salidas;

namespace FoodTrace.Mobile.Services;

public sealed class SalidasCocinaService
{
    public static SalidasCocinaService Shared { get; } = new();

    private readonly object sync = new();
    private List<LoteInventarioDto> lotes = [];
    private int secuenciaReferencia = 8829;

    public SalidasCocinaService()
    {
        RestaurarLotesPrueba();
    }

    public List<LoteInventarioDto> ObtenerLotesActivos()
    {
        lock (sync)
        {
            return lotes
                .Where(x => !x.Cerrado && x.SaldoDisponible > 0)
                .OrderBy(x => x.NombreProducto)
                .ThenBy(x => x.FechaIngreso)
                .Select(Clonar)
                .ToList();
        }
    }

    public Task<ValidacionLoteFifoResultado> ValidarLoteManualAsync(string? codigoIngresado, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var codigo = codigoIngresado?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return Task.FromResult(new ValidacionLoteFifoResultado
            {
                EsValido = false,
                MensajeError = "Ingresá un número de lote para validar la rotación."
            });
        }

        lock (sync)
        {
            var lote = lotes.FirstOrDefault(x =>
                string.Equals(x.CodigoLote, codigo, StringComparison.OrdinalIgnoreCase));

            if (lote is null)
            {
                return Task.FromResult(new ValidacionLoteFifoResultado
                {
                    EsValido = false,
                    MensajeError = $"No se encontró la caja o lote «{codigo.ToUpperInvariant()}» en el inventario activo."
                });
            }

            if (lote.Cerrado || lote.SaldoDisponible <= 0)
            {
                return Task.FromResult(new ValidacionLoteFifoResultado
                {
                    EsValido = false,
                    MensajeError = $"El lote {lote.CodigoLote} ya fue consumido por completo y se encuentra cerrado."
                });
            }

            var loteMasAntiguo = lotes
                .Where(x =>
                    x.ProductoId == lote.ProductoId &&
                    x.Id != lote.Id &&
                    !x.Cerrado &&
                    x.SaldoDisponible > 0 &&
                    x.FechaIngreso < lote.FechaIngreso)
                .OrderBy(x => x.FechaIngreso)
                .FirstOrDefault();

            if (loteMasAntiguo is not null)
            {
                return Task.FromResult(new ValidacionLoteFifoResultado
                {
                    EsValido = false,
                    MensajeError = $"Hay lotes más antiguos que deben usarse primero. Busque el {loteMasAntiguo.CodigoLote}."
                });
            }

            return Task.FromResult(new ValidacionLoteFifoResultado
            {
                EsValido = true,
                Lote = Clonar(lote)
            });
        }
    }

    public async Task<SalidaCocinaResultadoDto> ConsumirTodoAsync(string codigoLote, CancellationToken cancellationToken = default)
    {
        var validacion = await ValidarLoteManualAsync(codigoLote, cancellationToken);
        if (!validacion.EsValido || validacion.Lote is null)
            throw new InvalidOperationException(validacion.MensajeError);

        lock (sync)
        {
            var lote = lotes.Single(x => x.Id == validacion.Lote.Id);
            var descontado = lote.SaldoDisponible;
            lote.SaldoDisponible = 0m;
            lote.Cerrado = true;

            return CrearResultado(lote, descontado);
        }
    }

    public async Task<SalidaCocinaResultadoDto> ConsumirParcialAsync(
        string codigoLote,
        decimal cantidad,
        CancellationToken cancellationToken = default)
    {
        var validacion = await ValidarLoteManualAsync(codigoLote, cancellationToken);
        if (!validacion.EsValido || validacion.Lote is null)
            throw new InvalidOperationException(validacion.MensajeError);

        lock (sync)
        {
            var lote = lotes.Single(x => x.Id == validacion.Lote.Id);
            if (cantidad <= 0)
                throw new InvalidOperationException("Ingresá una cantidad mayor que cero para descontar.");
            if (cantidad > lote.SaldoDisponible)
                throw new InvalidOperationException(
                    $"La cantidad a descontar no puede superar el saldo disponible ({lote.SaldoFormateado}).");

            lote.SaldoDisponible -= cantidad;
            if (lote.SaldoDisponible == 0)
                lote.Cerrado = true;

            return CrearResultado(lote, cantidad);
        }
    }

    public void RestaurarLotesPrueba()
    {
        lock (sync)
        {
            var productoPollo = Guid.Parse("01000000-0000-0000-0000-000000000003");
            var productoPan = Guid.Parse("01000000-0000-0000-0000-000000000001");

            lotes =
            [
                new LoteInventarioDto
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000445"),
                    CodigoLote = "LOTE-445",
                    ProductoId = productoPollo,
                    NombreProducto = "Pechuga de Pollo",
                    SaldoDisponible = 20m,
                    UnidadBaseId = "kg",
                    FechaIngreso = DateTimeOffset.UtcNow.AddHours(-48),
                    Cerrado = false
                },
                new LoteInventarioDto
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000099"),
                    CodigoLote = "LOTE-2024-X99",
                    ProductoId = productoPollo,
                    NombreProducto = "Pechuga de Pollo",
                    SaldoDisponible = 20m,
                    UnidadBaseId = "kg",
                    FechaIngreso = DateTimeOffset.UtcNow.AddHours(-12),
                    Cerrado = false
                },
                new LoteInventarioDto
                {
                    Id = Guid.Parse("70000000-0000-0000-0000-000000000101"),
                    CodigoLote = "LOTE-PAN-101",
                    ProductoId = productoPan,
                    NombreProducto = "Pan lactal",
                    SaldoDisponible = 10m,
                    UnidadBaseId = "kg",
                    FechaIngreso = DateTimeOffset.UtcNow.AddHours(-24),
                    Cerrado = false
                }
            ];
        }
    }

    private SalidaCocinaResultadoDto CrearResultado(LoteInventarioDto lote, decimal descontado)
    {
        var numero = Interlocked.Increment(ref secuenciaReferencia);
        return new SalidaCocinaResultadoDto
        {
            ReferenciaSalida = $"#TR-{numero}-X",
            CodigoLote = lote.CodigoLote,
            NombreProducto = lote.NombreProducto,
            CantidadDescontada = descontado,
            SaldoRestante = lote.SaldoDisponible,
            UnidadBaseId = lote.UnidadBaseId,
            CajaCerrada = lote.Cerrado,
            FechaSalida = DateTimeOffset.UtcNow
        };
    }

    private static LoteInventarioDto Clonar(LoteInventarioDto origen) => new()
    {
        Id = origen.Id,
        CodigoLote = origen.CodigoLote,
        ProductoId = origen.ProductoId,
        NombreProducto = origen.NombreProducto,
        SaldoDisponible = origen.SaldoDisponible,
        UnidadBaseId = origen.UnidadBaseId,
        FechaIngreso = origen.FechaIngreso,
        Cerrado = origen.Cerrado
    };
}
