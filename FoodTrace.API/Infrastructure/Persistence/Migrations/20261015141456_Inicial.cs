using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodTrace.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Dimension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FactorConversion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id);
                    table.CheckConstraint("CK_UnidadesMedida_FactorPositivo", "CAST([FactorConversion] AS decimal(18,6)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.UniqueConstraint("AK_AspNetUsers_CompanyId_Id", x => new { x.CompanyId, x.Id })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Empresas_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Establecimientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Establecimientos", x => x.Id);
                    table.UniqueConstraint("AK_Establecimientos_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Establecimientos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                    table.UniqueConstraint("AK_Proveedores_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Proveedores_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnidadBaseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.UniqueConstraint("AK_Productos_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Productos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_UnidadesMedida_UnidadBaseId",
                        column: x => x.UnidadBaseId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidosCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CreadoPorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EnviadoPorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FechaEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecididoPorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FechaDecision = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoDecision = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosCompra", x => x.Id);
                    table.UniqueConstraint("AK_PedidosCompra_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.ForeignKey(
                        name: "FK_PedidosCompra_AspNetUsers_EmpresaId_CreadoPorId",
                        columns: x => new { x.EmpresaId, x.CreadoPorId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_AspNetUsers_EmpresaId_DecididoPorId",
                        columns: x => new { x.EmpresaId, x.DecididoPorId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_AspNetUsers_EmpresaId_EnviadoPorId",
                        columns: x => new { x.EmpresaId, x.EnviadoPorId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_Proveedores_EmpresaId_ProveedorId",
                        columns: x => new { x.EmpresaId, x.ProveedorId },
                        principalTable: "Proveedores",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresentacionesProveedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnidadMedidaId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresentacionesProveedor", x => x.Id);
                    table.UniqueConstraint("AK_PresentacionesProveedor_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_PresentacionesProveedor_CantidadBasePositiva", "CAST([CantidadBase] AS decimal(18,6)) > 0");
                    table.CheckConstraint("CK_PresentacionesProveedor_CantidadPositiva", "CAST([Cantidad] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_PresentacionesProveedor_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentacionesProveedor_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentacionesProveedor_Proveedores_EmpresaId_ProveedorId",
                        columns: x => new { x.EmpresaId, x.ProveedorId },
                        principalTable: "Proveedores",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresentacionesProveedor_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VersionesReceta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamiliaRecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroVersion = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductoResultadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rendimiento = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Instrucciones = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Publicada = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionesReceta", x => x.Id);
                    table.UniqueConstraint("AK_VersionesReceta_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_VersionesReceta_RendimientoPositivo", "CAST([Rendimiento] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_VersionesReceta_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VersionesReceta_Productos_EmpresaId_ProductoResultadoId",
                        columns: x => new { x.EmpresaId, x.ProductoResultadoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPedidoCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresentacionProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombrePresentacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnidadBaseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CantidadBasePresentacion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadPresentaciones = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadPedida = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NecesidadBruta = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPedidoCompra", x => x.Id);
                    table.UniqueConstraint("AK_DetallesPedidoCompra_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_DetallesPedidoCompra_CantidadBasePositiva", "CAST([CantidadBasePresentacion] AS decimal(18,6)) > 0");
                    table.CheckConstraint("CK_DetallesPedidoCompra_CantidadPedidaPositiva", "CAST([CantidadPedida] AS decimal(18,6)) > 0");
                    table.CheckConstraint("CK_DetallesPedidoCompra_NecesidadBrutaNoNegativa", "CAST([NecesidadBruta] AS decimal(18,6)) >= 0");
                    table.CheckConstraint("CK_DetallesPedidoCompra_PresentacionesEnteras", "CAST([CantidadPresentaciones] AS decimal(18,6)) = CAST([CantidadPresentaciones] AS bigint)");
                    table.CheckConstraint("CK_DetallesPedidoCompra_PresentacionesPositivas", "CAST([CantidadPresentaciones] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_DetallesPedidoCompra_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesPedidoCompra_PedidosCompra_EmpresaId_PedidoCompraId",
                        columns: x => new { x.EmpresaId, x.PedidoCompraId },
                        principalTable: "PedidosCompra",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesPedidoCompra_PresentacionesProveedor_EmpresaId_PresentacionProveedorId",
                        columns: x => new { x.EmpresaId, x.PresentacionProveedorId },
                        principalTable: "PresentacionesProveedor",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesPedidoCompra_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngredientesReceta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionRecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadMedidaId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientesReceta", x => x.Id);
                    table.UniqueConstraint("AK_IngredientesReceta_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_IngredientesReceta_CantidadPositiva", "CAST([Cantidad] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_IngredientesReceta_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientesReceta_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientesReceta_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngredientesReceta_VersionesReceta_EmpresaId_VersionRecetaId",
                        columns: x => new { x.EmpresaId, x.VersionRecetaId },
                        principalTable: "VersionesReceta",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesProduccion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstablecimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionRecetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantidadPlanificada = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FechaPlanificada = table.Column<DateOnly>(type: "date", nullable: false),
                    CreadoPorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesProduccion", x => x.Id);
                    table.UniqueConstraint("AK_OrdenesProduccion_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_OrdenesProduccion_CantidadPositiva", "CAST([CantidadPlanificada] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_OrdenesProduccion_AspNetUsers_EmpresaId_CreadoPorId",
                        columns: x => new { x.EmpresaId, x.CreadoPorId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesProduccion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesProduccion_Establecimientos_EmpresaId_EstablecimientoId",
                        columns: x => new { x.EmpresaId, x.EstablecimientoId },
                        principalTable: "Establecimientos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesProduccion_VersionesReceta_EmpresaId_VersionRecetaId",
                        columns: x => new { x.EmpresaId, x.VersionRecetaId },
                        principalTable: "VersionesReceta",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NecesidadesIngredientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenProduccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadBaseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CantidadNecesaria = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadAsignada = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NecesidadesIngredientes", x => x.Id);
                    table.UniqueConstraint("AK_NecesidadesIngredientes_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_NecesidadesIngredientes_AsignadaNoExcedeNecesidad", "CAST([CantidadAsignada] AS decimal(18,6)) <= CAST([CantidadNecesaria] AS decimal(18,6))");
                    table.CheckConstraint("CK_NecesidadesIngredientes_AsignadaNoNegativa", "CAST([CantidadAsignada] AS decimal(18,6)) >= 0");
                    table.CheckConstraint("CK_NecesidadesIngredientes_CantidadPositiva", "CAST([CantidadNecesaria] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_NecesidadesIngredientes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NecesidadesIngredientes_OrdenesProduccion_EmpresaId_OrdenProduccionId",
                        columns: x => new { x.EmpresaId, x.OrdenProduccionId },
                        principalTable: "OrdenesProduccion",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NecesidadesIngredientes_Productos_EmpresaId_ProductoId",
                        columns: x => new { x.EmpresaId, x.ProductoId },
                        principalTable: "Productos",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NecesidadesIngredientes_UnidadesMedida_UnidadBaseId",
                        column: x => x.UnidadBaseId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetallePedidoCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NecesidadIngredienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantidadAsignada = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Liberada = table.Column<bool>(type: "bit", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesCompra", x => x.Id);
                    table.UniqueConstraint("AK_AsignacionesCompra_EmpresaId_Id", x => new { x.EmpresaId, x.Id });
                    table.CheckConstraint("CK_AsignacionesCompra_CantidadPositiva", "CAST([CantidadAsignada] AS decimal(18,6)) > 0");
                    table.ForeignKey(
                        name: "FK_AsignacionesCompra_DetallesPedidoCompra_EmpresaId_DetallePedidoCompraId",
                        columns: x => new { x.EmpresaId, x.DetallePedidoCompraId },
                        principalTable: "DetallesPedidoCompra",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesCompra_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesCompra_NecesidadesIngredientes_EmpresaId_NecesidadIngredienteId",
                        columns: x => new { x.EmpresaId, x.NecesidadIngredienteId },
                        principalTable: "NecesidadesIngredientes",
                        principalColumns: new[] { "EmpresaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "UnidadesMedida",
                columns: new[] { "Id", "Dimension", "FactorConversion" },
                values: new object[,]
                {
                    { "g", "mass", 0.001m },
                    { "kg", "mass", 1m },
                    { "L", "volume", 1m },
                    { "ml", "volume", 0.001m },
                    { "unit", "count", 1m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesCompra_EmpresaId_DetallePedidoCompraId",
                table: "AsignacionesCompra",
                columns: new[] { "EmpresaId", "DetallePedidoCompraId" });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesCompra_EmpresaId_NecesidadIngredienteId",
                table: "AsignacionesCompra",
                columns: new[] { "EmpresaId", "NecesidadIngredienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedidoCompra_EmpresaId_PedidoCompraId",
                table: "DetallesPedidoCompra",
                columns: new[] { "EmpresaId", "PedidoCompraId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedidoCompra_EmpresaId_PresentacionProveedorId",
                table: "DetallesPedidoCompra",
                columns: new[] { "EmpresaId", "PresentacionProveedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedidoCompra_EmpresaId_ProductoId",
                table: "DetallesPedidoCompra",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientesReceta_EmpresaId_ProductoId",
                table: "IngredientesReceta",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientesReceta_EmpresaId_VersionRecetaId",
                table: "IngredientesReceta",
                columns: new[] { "EmpresaId", "VersionRecetaId" });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientesReceta_UnidadMedidaId",
                table: "IngredientesReceta",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_NecesidadesIngredientes_EmpresaId_OrdenProduccionId",
                table: "NecesidadesIngredientes",
                columns: new[] { "EmpresaId", "OrdenProduccionId" });

            migrationBuilder.CreateIndex(
                name: "IX_NecesidadesIngredientes_EmpresaId_ProductoId",
                table: "NecesidadesIngredientes",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_NecesidadesIngredientes_UnidadBaseId",
                table: "NecesidadesIngredientes",
                column: "UnidadBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesProduccion_EmpresaId_CreadoPorId",
                table: "OrdenesProduccion",
                columns: new[] { "EmpresaId", "CreadoPorId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesProduccion_EmpresaId_EstablecimientoId",
                table: "OrdenesProduccion",
                columns: new[] { "EmpresaId", "EstablecimientoId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesProduccion_EmpresaId_VersionRecetaId",
                table: "OrdenesProduccion",
                columns: new[] { "EmpresaId", "VersionRecetaId" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_EmpresaId_CreadoPorId",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "CreadoPorId" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_EmpresaId_DecididoPorId",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "DecididoPorId" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_EmpresaId_EnviadoPorId",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "EnviadoPorId" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_EmpresaId_ProveedorId",
                table: "PedidosCompra",
                columns: new[] { "EmpresaId", "ProveedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesProveedor_EmpresaId_ProductoId",
                table: "PresentacionesProveedor",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesProveedor_EmpresaId_ProveedorId",
                table: "PresentacionesProveedor",
                columns: new[] { "EmpresaId", "ProveedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_PresentacionesProveedor_UnidadMedidaId",
                table: "PresentacionesProveedor",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_UnidadBaseId",
                table: "Productos",
                column: "UnidadBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionesReceta_EmpresaId_FamiliaRecetaId_NumeroVersion",
                table: "VersionesReceta",
                columns: new[] { "EmpresaId", "FamiliaRecetaId", "NumeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionesReceta_EmpresaId_ProductoResultadoId",
                table: "VersionesReceta",
                columns: new[] { "EmpresaId", "ProductoResultadoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesCompra");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "IngredientesReceta");

            migrationBuilder.DropTable(
                name: "DetallesPedidoCompra");

            migrationBuilder.DropTable(
                name: "NecesidadesIngredientes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "PedidosCompra");

            migrationBuilder.DropTable(
                name: "PresentacionesProveedor");

            migrationBuilder.DropTable(
                name: "OrdenesProduccion");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Establecimientos");

            migrationBuilder.DropTable(
                name: "VersionesReceta");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "UnidadesMedida");
        }
    }
}
