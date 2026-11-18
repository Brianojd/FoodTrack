using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTrace.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptReceptionInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recepciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenRecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioReceptorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FechaRecepcion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recepciones", x => x.Id);
                    table.UniqueConstraint("AK_Recepciones_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Recepciones_AspNetUsers_EmpresaId_UsuarioReceptorId",
                        columns: x => new { x.EmpresaId, x.UsuarioReceptorId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recepciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recepciones_OrdenesRecepcion_EmpresaId_OrdenRecepcionId",
                        columns: x => new { x.EmpresaId, x.OrdenRecepcionId },
                        principalTable: "OrdenesRecepcion",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesRecepcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetalleOrdenRecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenRecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreProducto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PresentacionProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombrePresentacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CantidadPresentacionesEsperadas = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadPresentacionesRecibidas = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadPresentacionesFaltantes = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadBasePresentacion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadEsperada = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadFaltante = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnidadBaseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesRecepcion", x => x.Id);
                    table.UniqueConstraint("AK_DetallesRecepcion_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_DetallesRecepcion_FaltanteExacto", "CAST([CantidadFaltante] AS decimal(18,6)) = CAST([CantidadEsperada] AS decimal(18,6)) - CAST([CantidadRecibida] AS decimal(18,6))");
                    table.CheckConstraint("CK_DetallesRecepcion_FaltantesExactos", "CAST([CantidadPresentacionesFaltantes] AS decimal(18,6)) = CAST([CantidadPresentacionesEsperadas] AS decimal(18,6)) - CAST([CantidadPresentacionesRecibidas] AS decimal(18,6))");
                    table.CheckConstraint("CK_DetallesRecepcion_RecibidasEnteras", "CAST([CantidadPresentacionesRecibidas] AS decimal(18,6)) = CAST([CantidadPresentacionesRecibidas] AS bigint)");
                    table.CheckConstraint("CK_DetallesRecepcion_RecibidasNoExcedenEsperado", "CAST([CantidadPresentacionesRecibidas] AS decimal(18,6)) <= CAST([CantidadPresentacionesEsperadas] AS decimal(18,6))");
                    table.CheckConstraint("CK_DetallesRecepcion_RecibidasNoNegativas", "CAST([CantidadPresentacionesRecibidas] AS decimal(18,6)) >= 0");
                    table.CheckConstraint("CK_DetallesRecepcion_RecibidoExacto", "CAST([CantidadRecibida] AS decimal(18,6)) = CAST([CantidadPresentacionesRecibidas] AS decimal(18,6)) * CAST([CantidadBasePresentacion] AS decimal(18,6))");
                    table.ForeignKey(
                        name: "FK_DetallesRecepcion_DetallesOrdenRecepcion_EmpresaId_DetalleOrdenRecepcionId",
                        columns: x => new { x.EmpresaId, x.DetalleOrdenRecepcionId },
                        principalTable: "DetallesOrdenRecepcion",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesRecepcion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesRecepcion_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesRecepcion_Recepciones_EmpresaId_RecepcionId",
                        columns: x => new { x.EmpresaId, x.RecepcionId },
                        principalTable: "Recepciones",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesRecepcion_UnidadesMedida_UnidadBaseId",
                        column: x => x.UnidadBaseId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngresosInventario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetalleRecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenRecepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnidadBaseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FechaIngreso = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngresosInventario", x => x.Id);
                    table.UniqueConstraint("AK_IngresosInventario_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_IngresosInventario_CantidadPositiva", "CAST([Cantidad] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_IngresosInventario_DetallesRecepcion_EmpresaId_DetalleRecepcionId",
                        columns: x => new { x.EmpresaId, x.DetalleRecepcionId },
                        principalTable: "DetallesRecepcion",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngresosInventario_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngresosInventario_Establecimientos_EmpresaId_EstablecimientoId",
                        columns: x => new { x.EmpresaId, x.EstablecimientoId },
                        principalTable: "Establecimientos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngresosInventario_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngresosInventario_Recepciones_EmpresaId_RecepcionId",
                        columns: x => new { x.EmpresaId, x.RecepcionId },
                        principalTable: "Recepciones",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngresosInventario_UnidadesMedida_UnidadBaseId",
                        column: x => x.UnidadBaseId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRecepcion_EmpresaId_DetalleOrdenRecepcionId",
                table: "DetallesRecepcion",
                columns: new[] { "EmpresaId", "DetalleOrdenRecepcionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRecepcion_EmpresaId_ProductoId",
                table: "DetallesRecepcion",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRecepcion_EmpresaId_RecepcionId_DetalleOrdenRecepcionId",
                table: "DetallesRecepcion",
                columns: new[] { "EmpresaId", "RecepcionId", "DetalleOrdenRecepcionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRecepcion_UnidadBaseId",
                table: "DetallesRecepcion",
                column: "UnidadBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_IngresosInventario_EmpresaId_DetalleRecepcionId",
                table: "IngresosInventario",
                columns: new[] { "EmpresaId", "DetalleRecepcionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngresosInventario_EmpresaId_EstablecimientoId",
                table: "IngresosInventario",
                columns: new[] { "EmpresaId", "EstablecimientoId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngresosInventario_EmpresaId_ProductoId",
                table: "IngresosInventario",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngresosInventario_EmpresaId_RecepcionId",
                table: "IngresosInventario",
                columns: new[] { "EmpresaId", "RecepcionId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngresosInventario_UnidadBaseId",
                table: "IngresosInventario",
                column: "UnidadBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_EmpresaId_OrdenRecepcionId",
                table: "Recepciones",
                columns: new[] { "EmpresaId", "OrdenRecepcionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_EmpresaId_UsuarioReceptorId",
                table: "Recepciones",
                columns: new[] { "EmpresaId", "UsuarioReceptorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngresosInventario");

            migrationBuilder.DropTable(
                name: "DetallesRecepcion");

            migrationBuilder.DropTable(
                name: "Recepciones");
        }
    }
}
