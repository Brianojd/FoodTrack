using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTrace.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecipeYieldUnitsAndPortions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CantidadRendimiento",
                table: "VersionesReceta",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcionesRendimiento",
                table: "VersionesReceta",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnidadRendimientoId",
                table: "VersionesReceta",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionesReceta_UnidadRendimientoId",
                table: "VersionesReceta",
                column: "UnidadRendimientoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VersionesReceta_CantidadRendimiento",
                table: "VersionesReceta",
                sql: "[CantidadRendimiento] IS NULL OR (CAST([CantidadRendimiento] AS decimal(18,6)) > 0 AND CAST([CantidadRendimiento] AS decimal(18,6)) <= 1000000000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VersionesReceta_PorcionesRendimiento",
                table: "VersionesReceta",
                sql: "[PorcionesRendimiento] IS NULL OR (CAST([PorcionesRendimiento] AS decimal(18,6)) > 0 AND CAST([PorcionesRendimiento] AS decimal(18,6)) <= 1000000000)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VersionesReceta_UnidadCantidadRendimiento",
                table: "VersionesReceta",
                sql: "([CantidadRendimiento] IS NULL AND [UnidadRendimientoId] IS NULL) OR ([CantidadRendimiento] IS NOT NULL AND [UnidadRendimientoId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_VersionesReceta_UnidadesMedida_UnidadRendimientoId",
                table: "VersionesReceta",
                column: "UnidadRendimientoId",
                principalTable: "UnidadesMedida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VersionesReceta_UnidadesMedida_UnidadRendimientoId",
                table: "VersionesReceta");

            migrationBuilder.DropIndex(
                name: "IX_VersionesReceta_UnidadRendimientoId",
                table: "VersionesReceta");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VersionesReceta_CantidadRendimiento",
                table: "VersionesReceta");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VersionesReceta_PorcionesRendimiento",
                table: "VersionesReceta");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VersionesReceta_UnidadCantidadRendimiento",
                table: "VersionesReceta");

            migrationBuilder.DropColumn(
                name: "CantidadRendimiento",
                table: "VersionesReceta");

            migrationBuilder.DropColumn(
                name: "PorcionesRendimiento",
                table: "VersionesReceta");

            migrationBuilder.DropColumn(
                name: "UnidadRendimientoId",
                table: "VersionesReceta");
        }
    }
}
