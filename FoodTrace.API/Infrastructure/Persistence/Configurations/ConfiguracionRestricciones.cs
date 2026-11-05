using FoodTrace.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Persistence.Configurations;

internal static class ConfiguracionRestricciones
{
    public static void Configurar(ModelBuilder modelo)
    {
        // El CAST evita comparar texto decimal en SQLite; SQL Server conserva decimal(18,6).
        modelo.Entity<UnidadMedida>().ToTable(tabla => tabla.HasCheckConstraint(
            "CK_UnidadesMedida_FactorPositivo", Positivo("FactorConversion")));
        modelo.Entity<PresentacionProveedor>().ToTable(tabla =>
        {
            tabla.HasCheckConstraint("CK_PresentacionesProveedor_CantidadPositiva", Positivo("Cantidad"));
            tabla.HasCheckConstraint("CK_PresentacionesProveedor_CantidadBasePositiva", Positivo("CantidadBase"));
        });
        modelo.Entity<VersionReceta>().ToTable(tabla =>
        {
            tabla.HasCheckConstraint("CK_VersionesReceta_RendimientoPositivo", Positivo("Rendimiento"));
            tabla.HasCheckConstraint("CK_VersionesReceta_UnidadCantidadRendimiento",
                "([CantidadRendimiento] IS NULL AND [UnidadRendimientoId] IS NULL) OR ([CantidadRendimiento] IS NOT NULL AND [UnidadRendimientoId] IS NOT NULL)");
            tabla.HasCheckConstraint("CK_VersionesReceta_CantidadRendimiento",
                "[CantidadRendimiento] IS NULL OR (" + Positivo("CantidadRendimiento") + " AND " + Decimal("CantidadRendimiento") + " <= 1000000000)");
            tabla.HasCheckConstraint("CK_VersionesReceta_PorcionesRendimiento",
                "[PorcionesRendimiento] IS NULL OR (" + Positivo("PorcionesRendimiento") + " AND " + Decimal("PorcionesRendimiento") + " <= 1000000000)");
        });
        modelo.Entity<IngredienteReceta>().ToTable(tabla => tabla.HasCheckConstraint(
            "CK_IngredientesReceta_CantidadPositiva", Positivo("Cantidad")));
        modelo.Entity<OrdenProduccion>().ToTable(tabla => tabla.HasCheckConstraint(
            "CK_OrdenesProduccion_CantidadPositiva", Positivo("CantidadPlanificada")));
        modelo.Entity<NecesidadIngrediente>().ToTable(tabla =>
        {
            tabla.HasCheckConstraint("CK_NecesidadesIngredientes_CantidadPositiva", Positivo("CantidadNecesaria"));
            tabla.HasCheckConstraint("CK_NecesidadesIngredientes_AsignadaNoNegativa", NoNegativo("CantidadAsignada"));
            tabla.HasCheckConstraint("CK_NecesidadesIngredientes_AsignadaNoExcedeNecesidad",
                $"{Decimal("CantidadAsignada")} <= {Decimal("CantidadNecesaria")}");
        });
        modelo.Entity<AsignacionCompra>().ToTable(tabla => tabla.HasCheckConstraint(
            "CK_AsignacionesCompra_CantidadPositiva", Positivo("CantidadAsignada")));
        modelo.Entity<DetallePedidoCompra>().ToTable(tabla =>
        {
            tabla.HasCheckConstraint("CK_DetallesPedidoCompra_PresentacionesPositivas", Positivo("CantidadPresentaciones"));
            tabla.HasCheckConstraint("CK_DetallesPedidoCompra_PresentacionesEnteras",
                $"{Decimal("CantidadPresentaciones")} = CAST([CantidadPresentaciones] AS bigint)");
            tabla.HasCheckConstraint("CK_DetallesPedidoCompra_CantidadBasePositiva", Positivo("CantidadBasePresentacion"));
            tabla.HasCheckConstraint("CK_DetallesPedidoCompra_CantidadPedidaPositiva", Positivo("CantidadPedida"));
            tabla.HasCheckConstraint("CK_DetallesPedidoCompra_NecesidadBrutaNoNegativa", NoNegativo("NecesidadBruta"));
        });
    }

    private static string Decimal(string columna) => $"CAST([{columna}] AS decimal(18,6))";
    private static string Positivo(string columna) => $"{Decimal(columna)} > 0";
    private static string NoNegativo(string columna) => $"{Decimal(columna)} >= 0";
}
