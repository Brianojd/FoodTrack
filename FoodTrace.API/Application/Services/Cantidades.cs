namespace FoodTrace.API.Application.Services;

public sealed class ExcepcionRegla(string mensaje, IDictionary<string, string[]>? errores = null) : Exception(mensaje)
{
    public IDictionary<string, string[]>? Errores { get; } = errores;
}

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
    // Las recetas consolidan cantidades exactas antes de redondear una única necesidad por producto.
    public static decimal ConvertirExacto(decimal valor, string dimensionOrigen, decimal factorOrigen,
        string dimensionDestino, decimal factorDestino)
    {
        if (dimensionOrigen != dimensionDestino) throw new ExcepcionRegla("Las unidades deben pertenecer a la misma magnitud.");
        return Positiva(valor) * Positiva(factorOrigen) / Positiva(factorDestino);
    }
    public static decimal RedondearNecesidad(decimal valor) => Positiva(RedondearArriba(valor));
    public static decimal CantidadPresentaciones(decimal cantidad, decimal cantidadBase)
    {
        if (decimal.Truncate(cantidad) != cantidad) throw new ExcepcionRegla("Package count must be a whole number.");
        return Positiva(Positiva(cantidad) * Positiva(cantidadBase));
    }
    private static decimal RedondearArriba(decimal valor) => decimal.Ceiling(valor * 1_000_000m) / 1_000_000m;
}
