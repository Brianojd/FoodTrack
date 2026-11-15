using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTrace.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptOrderCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreadoPorId",
                table: "OrdenesRecepcion",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesRecepcion_EmpresaId_CreadoPorId",
                table: "OrdenesRecepcion",
                columns: new[] { "EmpresaId", "CreadoPorId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesRecepcion_AspNetUsers_EmpresaId_CreadoPorId",
                table: "OrdenesRecepcion",
                columns: new[] { "EmpresaId", "CreadoPorId" },
                principalTable: "AspNetUsers",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesRecepcion_AspNetUsers_EmpresaId_CreadoPorId",
                table: "OrdenesRecepcion");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesRecepcion_EmpresaId_CreadoPorId",
                table: "OrdenesRecepcion");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "OrdenesRecepcion");
        }
    }
}
