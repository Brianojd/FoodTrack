namespace FoodTrace.Services.Estado;

// Cada selección/carga invalida su generación anterior, incluso si el transporte ignora cancelar.
public sealed class ControlCargaModulo : IDisposable
{
    private CancellationTokenSource? fuente;
    private long generacion;
    public (long Generacion, CancellationToken Token) Iniciar(CancellationToken vida)
    {
        Cancelar();
        fuente = CancellationTokenSource.CreateLinkedTokenSource(vida);
        return (generacion, fuente.Token);
    }
    public bool EsActual(long valor) => valor == generacion && fuente is { IsCancellationRequested: false };
    public void Cancelar() { generacion++; fuente?.Cancel(); fuente?.Dispose(); fuente = null; }
    public void Dispose() => Cancelar();
}
