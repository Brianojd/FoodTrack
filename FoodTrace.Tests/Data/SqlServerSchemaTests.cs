using System;
using System.Collections.Generic;
using System.Linq;
using FoodTrace.API.Domain.Entities;
using FoodTrace.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests.Data;

public class SqlServerSchemaTests
{
	[Theory]
	[InlineData(new object[]
	{
		typeof(IdentityUserRole<string>),
		512
	})]
	[InlineData(new object[]
	{
		typeof(IdentityUserLogin<string>),
		512
	})]
	[InlineData(new object[]
	{
		typeof(IdentityUserToken<string>),
		768
	})]
	public void Composite_identity_primary_keys_fit_sql_server_byte_limit(Type type, int expectedBytes)
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			// Los tres tipos Identity de la teoría están registrados y tienen clave primaria.
			IEntityType entity = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IDesignTimeModel>().Model.FindEntityType(type)!;
			int width = ((IEnumerable<IProperty>)entity.FindPrimaryKey()!.Properties).Sum((Func<IProperty, int>)MaximumBytes);
			Assert.True(width <= 900, $"{entity.GetTableName()} declares a {width}-byte primary key; SQL Server allows 900 bytes.");
			Assert.Equal(expectedBytes, width);
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

	[Fact]
	public void Keys_indexes_and_matching_foreign_keys_have_bounded_compatible_widths()
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			IModel model = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IDesignTimeModel>().Model;
			foreach (IEntityType entity in model.GetEntityTypes())
			{
				foreach (IKey key in entity.GetKeys())
				{
					int maximum = ((key.IsPrimaryKey() || key.IsClustered() == true) ? 900 : 1700);
					Assert.InRange(((IEnumerable<IProperty>)key.Properties).Sum((Func<IProperty, int>)MaximumBytes), 1, maximum);
				}
				foreach (IIndex index in entity.GetIndexes())
				{
					Assert.InRange(((IEnumerable<IProperty>)index.Properties).Sum((Func<IProperty, int>)MaximumBytes), 1, (index.IsClustered() == true) ? 900 : 1700);
				}
				foreach (IForeignKey foreignKey in entity.GetForeignKeys())
				{
					Assert.InRange(((IEnumerable<IProperty>)foreignKey.Properties).Sum((Func<IProperty, int>)MaximumBytes), 1, 900);
					foreach (var (dependent, principal) in foreignKey.Properties.Zip(foreignKey.PrincipalKey.Properties))
					{
						Assert.Equal(principal.GetMaxLength(), dependent.GetMaxLength());
						Assert.Equal(principal.GetColumnType(), dependent.GetColumnType());
					}
				}
			}
			// FoodTraceDbContext registra estos tipos, sus tablas y las propiedades indicadas.
			Assert.Equal(32, model.FindEntityType(typeof(UnidadMedida))!.FindProperty("Id")!.GetMaxLength());
			IEntityType user = model.FindEntityType(typeof(AppUser))!;
			StoreObjectIdentifier userTable = StoreObjectIdentifier.Table(user.GetTableName()!, user.GetSchema());
			Assert.Equal("CompanyId", user.FindProperty("EmpresaId")!.GetColumnName(in userTable));
			Assert.Equal("Enabled", user.FindProperty("Habilitado")!.GetColumnName(in userTable));
			Assert.Equal(128, user.FindProperty("Id")!.GetMaxLength());
			Assert.Equal(272, ((IEnumerable<IProperty>)user.GetKeys().Single((IKey x) => !x.IsPrimaryKey()).Properties).Sum((Func<IProperty, int>)MaximumBytes));
			string[] array = new string[4] { "UserName", "NormalizedUserName", "Email", "NormalizedEmail" };
			foreach (string property in array)
			{
				Assert.Equal(256, user.FindProperty(property)!.GetMaxLength());
			}
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

	[Fact]
	public void Initial_migration_creates_final_widths_without_constrained_column_narrowing()
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			IMigrationsAssembly assembly = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IMigrationsAssembly>();
			// La fábrica configura el proveedor SQL Server para inspeccionar sus metadatos.
            Migration migration = assembly.CreateMigration(assembly.Migrations["20261015141456_Inicial"], ((DbContext)(object)db).Database.ProviderName!);
			Assert.DoesNotContain((IEnumerable<MigrationOperation>)migration.UpOperations, (Predicate<MigrationOperation>)((MigrationOperation x) => (x is AlterColumnOperation || x is DropTableOperation || x is DropPrimaryKeyOperation || x is DropForeignKeyOperation) ? true : false));
			Dictionary<string, CreateTableOperation> tables = migration.UpOperations.OfType<CreateTableOperation>().ToDictionary((CreateTableOperation x) => x.Name);
			Assert.Equal(128, tables["AspNetUsers"].Columns.Single((AddColumnOperation x) => x.Name == "Id").MaxLength);
			Assert.Equal(32, tables["UnidadesMedida"].Columns.Single((AddColumnOperation x) => x.Name == "Id").MaxLength);
			Assert.All(tables["AspNetUserTokens"].Columns.Where((AddColumnOperation x) =>
			{
				switch (x.Name)
				{
				case "UserId":
				case "LoginProvider":
				case "Name":
					return true;
				default:
					return false;
				}
			}), (AddColumnOperation x) =>
			{
				Assert.Equal(128, x.MaxLength);
			});
			HashSet<string> createdTables = new HashSet<string>();
			foreach (CreateTableOperation table in migration.UpOperations.OfType<CreateTableOperation>())
			{
				foreach (AddForeignKeyOperation foreignKey in table.ForeignKeys)
				{
					Assert.True(createdTables.Contains(foreignKey.PrincipalTable), table.Name + " references " + foreignKey.PrincipalTable + " before its creation.");
					CreateTableOperation principal = tables[foreignKey.PrincipalTable];
					// La migración declara PK y columnas principales explícitas para estas FK.
					Assert.True(principal.PrimaryKey!.Columns.SequenceEqual(foreignKey.PrincipalColumns!) || principal.UniqueConstraints.Any((AddUniqueConstraintOperation x) => x.Columns.SequenceEqual(foreignKey.PrincipalColumns!)), table.Name + " foreign key has no matching principal PK/AK.");
					foreach (var item in foreignKey.Columns.Zip(foreignKey.PrincipalColumns!))
					{
						string dependentName = item.First;
						string principalName = item.Second;
						AddColumnOperation dependentColumn = table.Columns.Single((AddColumnOperation x) => x.Name == dependentName);
						AddColumnOperation principalColumn = principal.Columns.Single((AddColumnOperation x) => x.Name == principalName);
						Assert.Equal(principalColumn.ColumnType, dependentColumn.ColumnType);
						Assert.Equal(principalColumn.MaxLength, dependentColumn.MaxLength);
					}
				}
				createdTables.Add(table.Name);
			}
			string script = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IMigrator>().GenerateScript(null, null, MigrationsSqlGenerationOptions.Idempotent);
			Assert.Contains("CREATE TABLE [AspNetUsers]", script);
			Assert.Contains("CREATE TABLE [UnidadesMedida]", script);
			Assert.Contains("[Id] nvarchar(32) NOT NULL", script);
			Assert.Contains("[LoginProvider] nvarchar(128) NOT NULL", script);
			Assert.Contains("[ProviderKey] nvarchar(128) NOT NULL", script);
			Assert.Contains("[Name] nvarchar(128) NOT NULL", script);
			Assert.Contains("[UserId] nvarchar(128) NOT NULL", script);
			Assert.Contains("[RoleId] nvarchar(128) NOT NULL", script);
			Assert.Contains("[CreadoPorId] nvarchar(128) NOT NULL", script);
			Assert.Contains("[EnviadoPorId] nvarchar(128) NULL", script);
			Assert.Contains("[DecididoPorId] nvarchar(128) NULL", script);
			Assert.Contains("[UnidadBaseId] nvarchar(32) NOT NULL", script);
			Assert.Contains("CONSTRAINT [AK_AspNetUsers_CompanyId_Id] UNIQUE NONCLUSTERED ([CompanyId], [Id])", script);
			Assert.DoesNotContain("ALTER COLUMN", script);
			Assert.DoesNotContain("DROP CONSTRAINT", script);
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

	[Fact]
	public void Sql_server_delete_graph_has_no_cycles_or_multiple_cascade_paths()
	{
		FoodTraceDbContext db = new FoodTraceDbContextFactory().CreateDbContext(Array.Empty<string>());
		try
		{
			List<IEntityType> entities = ((IInfrastructure<IServiceProvider>)(object)db).GetService<IDesignTimeModel>().Model.GetEntityTypes().ToList();
			List<IForeignKey> cascading = entities.SelectMany((IEntityType x) => x.GetForeignKeys()).Where((IForeignKey x) =>
			{
				DeleteBehavior deleteBehavior = x.DeleteBehavior;
				return (uint)(deleteBehavior - 2) <= 1u;
			}).ToList();
			foreach (IEntityType item in entities)
			{
				IEntityType root = item;
				HashSet<IEntityType> reached = new HashSet<IEntityType> { root };
				Visit(root);
				void Visit(IEntityType parent)
				{
					foreach (IForeignKey edge in cascading.Where((IForeignKey x) => x.PrincipalEntityType == parent))
					{
						Assert.True(reached.Add(edge.DeclaringEntityType), $"Repeated cascade path from {root.GetTableName()} to {edge.DeclaringEntityType.GetTableName()}.");
						Visit(edge.DeclaringEntityType);
					}
				}
			}
		}
		finally
		{
			((IDisposable)(object)db)?.Dispose();
		}
	}

    [Fact]
    public void La_migracion_de_rendimiento_solo_agrega_metadatos_anulables_y_restricciones()
    {
        using var db = new FoodTraceDbContext(new DbContextOptionsBuilder<FoodTraceDbContext>()
            .UseSqlServer("Server=(local);Database=FoodTrace_QA_Recetas_OfflineOnly;Integrated Security=True").Options,
            new FoodTrace.API.Infrastructure.Identity.SesionEmpresa(new Microsoft.AspNetCore.Http.HttpContextAccessor()));
        var assembly = db.GetService<IMigrationsAssembly>();
        Assert.Equal(new[] { "20261015141456_Inicial", "20261105030317_RecipeYieldUnitsAndPortions" }, assembly.Migrations.Keys);
        var migration = assembly.CreateMigration(assembly.Migrations["20261105030317_RecipeYieldUnitsAndPortions"], db.Database.ProviderName!);
        Assert.Equal(8, migration.UpOperations.Count);
        Assert.All(migration.UpOperations, operacion => Assert.True(operacion is AddColumnOperation or CreateIndexOperation or AddCheckConstraintOperation or AddForeignKeyOperation));
        var columnas = migration.UpOperations.OfType<AddColumnOperation>().ToArray();
        Assert.Equal(new[] { "CantidadRendimiento", "PorcionesRendimiento", "UnidadRendimientoId" }, columnas.Select(x => x.Name));
        Assert.All(columnas, columna =>
        {
            Assert.Equal("VersionesReceta", columna.Table);
            Assert.True(columna.IsNullable);
            Assert.Null(columna.DefaultValue);
        });
        Assert.All(columnas.Where(x => x.ClrType == typeof(decimal)), columna =>
        { Assert.Equal(18, columna.Precision); Assert.Equal(6, columna.Scale); });
        Assert.Equal(32, columnas.Single(x => x.Name == "UnidadRendimientoId").MaxLength);
        Assert.Equal(3, migration.UpOperations.OfType<AddCheckConstraintOperation>().Count());
        var fk = Assert.Single(migration.UpOperations.OfType<AddForeignKeyOperation>());
        Assert.Equal("UnidadesMedida", fk.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, fk.OnDelete);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static int MaximumBytes(IProperty property)
	{
		if (property.ClrType == typeof(string))
		{
			int? maximumLength = property.GetMaxLength();
			Assert.NotNull(maximumLength);
			return maximumLength.Value * ((property.IsUnicode() == false) ? 1 : 2);
		}
		if (property.ClrType == typeof(Guid))
		{
			return 16;
		}
		if (property.ClrType == typeof(int))
		{
			return 4;
		}
		throw new InvalidOperationException("Add an explicit byte-width calculation for key property " + property.Name + ".");
	}
}
