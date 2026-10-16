namespace FoodTrace.API.Application.Services;

public sealed class ExcepcionRegla(string mensaje) : Exception(mensaje);

public static class Cantidades
{
    public const decimal Maximo = 1_000_000_000m;
    public static decimal Positiva(decimal valor)
    {
        if (valor <= 0 || valor > Maximo || decimal.Round(valor, 6) != valor)
            throw new ExcepcionRegla("Quantity must be positive, at most 1,000,000,000, with at most six decimal places.");
        return valor;
    }
    public static decimal Convertir(decimal valor, string dimensionOrigen, decimal factorOrigen,
        string dimensionDestino, decimal factorDestino)
    {
        if (dimensionOrigen != dimensionDestino) throw new ExcepcionRegla("Unit dimensions must match.");
        return Positiva(RedondearArriba(Positiva(valor) * Positiva(factorOrigen) / Positiva(factorDestino)));
    }
    public static decimal Necesidad(decimal ingrediente, decimal factor, decimal factorBase,
        decimal rendimiento, decimal cantidadPlanificada) => Positiva(RedondearArriba(Positiva(ingrediente) * Positiva(factor)
            / Positiva(factorBase) * Positiva(cantidadPlanificada) / Positiva(rendimiento)));
    public static decimal CantidadPresentaciones(decimal cantidad, decimal cantidadBase)
    {
        if (decimal.Truncate(cantidad) != cantidad) throw new ExcepcionRegla("Package count must be a whole number.");
        return Positiva(Positiva(cantidad) * Positiva(cantidadBase));
    }
    private static decimal RedondearArriba(decimal valor) => decimal.Ceiling(valor * 1_000_000m) / 1_000_000m;
}
