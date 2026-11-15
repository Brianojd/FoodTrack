using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTrace.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EstablecimientoId",
                table: "PedidosCompra",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrdenesRecepcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreProveedor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreEstablecimiento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesRecepcion", x => x.Id);
                    table.UniqueConstraint("AK_OrdenesRecepcion_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.ForeignKey(
                        name: "FK_OrdenesRecepcion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesRecepcion_Establecimientos_EmpresaId_EstablecimientoId",
                        columns: x => new { x.EmpresaId, x.EstablecimientoId },
                        principalTable: "Establecimientos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesRecepcion_PedidosCompra_EmpresaId_PedidoCompraId",
                        columns: x => new { x.EmpresaId, x.PedidoCompraId },
                        principalTable: "PedidosCompra",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesRecepcion_Proveedores_EmpresaId_ProveedorId",
                        columns: x => new { x.EmpresaId, x.ProveedorId },
                        principalTable: "Proveedores",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesOrdenRecepcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenRecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreProducto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PresentacionProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombrePresentacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CantidadPresentaciones = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadBasePresentacion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadEsperada = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnidadBaseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesOrdenRecepcion", x => x.Id);
                    table.UniqueConstraint("AK_DetallesOrdenRecepcion_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_DetallesOrdenRecepcion_CantidadBasePositiva", "CAST([CantidadBasePresentacion] AS decimal(18,6)) > 0");
                    table.CheckConstraint("CK_DetallesOrdenRecepcion_CantidadEsperadaExacta", "CAST([CantidadEsperada] AS decimal(18,6)) = CAST([CantidadPresentaciones] AS decimal(18,6)) * CAST([CantidadBasePresentacion] AS decimal(18,6))");
                    table.CheckConstraint("CK_DetallesOrdenRecepcion_CantidadEsperadaPositiva", "CAST([CantidadEsperada] AS decimal(18,6)) > 0");
                    table.CheckConstraint("CK_DetallesOrdenRecepcion_PresentacionesEnteras", "CAST([CantidadPresentaciones] AS decimal(18,6)) = CAST([CantidadPresentaciones] AS bigint)");
                    table.CheckConstraint("CK_DetallesOrdenRecepcion_PresentacionesPositivas", "CAST([CantidadPresentaciones] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_DetallesOrdenRecepcion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenRecepcion_OrdenesRecepcion_EmpresaId_OrdenRecepcionId",
                        columns: x => new { x.EmpresaId, x.OrdenRecepcionId },
                        principalTable: "OrdenesRecepcion",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenRecepcion_PresentacionesProveedor_EmpresaId_PresentacionProveedorId",
                        columns: x => new { x.EmpresaId, x.PresentacionProveedorId },
                        principalTable: "PresentacionesProveedor",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenRecepcion_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenRecepcion_UnidadesMedida_UnidadBaseId",
                        column: x => x.UnidadBaseId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_EmpresaId_EstablecimientoId",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "EstablecimientoId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenRecepcion_EmpresaId_OrdenRecepcionId",
                table: "DetallesOrdenRecepcion",
                columns: new[] { "EmpresaId", "OrdenRecepcionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenRecepcion_EmpresaId_PresentacionProveedorId",
                table: "DetallesOrdenRecepcion",
                columns: new[] { "EmpresaId", "PresentacionProveedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenRecepcion_EmpresaId_ProductoId",
                table: "DetallesOrdenRecepcion",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenRecepcion_UnidadBaseId",
                table: "DetallesOrdenRecepcion",
                column: "UnidadBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesRecepcion_EmpresaId_EstablecimientoId",
                table: "OrdenesRecepcion",
                columns: new[] { "EmpresaId", "EstablecimientoId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesRecepcion_EmpresaId_PedidoCompraId",
                table: "OrdenesRecepcion",
                columns: new[] { "EmpresaId", "PedidoCompraId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesRecepcion_EmpresaId_ProveedorId",
                table: "OrdenesRecepcion",
                columns: new[] { "EmpresaId", "ProveedorId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosCompra_Establecimientos_EmpresaId_EstablecimientoId",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "EstablecimientoId" },
                principalTable: "Establecimientos",
                principalColumns: new[] { "EmpresaId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosCompra_Establecimientos_EmpresaId_EstablecimientoId",
                table: "PedidosCompra");

            migrationBuilder.DropTable(
                name: "DetallesOrdenRecepcion");

            migrationBuilder.DropTable(
                name: "OrdenesRecepcion");

            migrationBuilder.DropIndex(
                name: "IX_PedidosCompra_EmpresaId_EstablecimientoId",
                table: "PedidosCompra");

            migrationBuilder.DropColumn(
                name: "EstablecimientoId",
                table: "PedidosCompra");
        }
    }
}
