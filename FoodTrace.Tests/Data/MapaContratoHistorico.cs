using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FoodTrace.API.Application.Services;
using FoodTrace.API.Domain.Entities;

// Reconstructed from preserved M6 DLL; see docs/recuperacion-pruebas.md.
namespace FoodTrace.Tests.Data;

internal static class MapaContratoHistorico
{
	public static readonly Type[] TiposEntidad = new Type[13]
	{
		typeof(Empresa),
		typeof(UnidadMedida),
		typeof(Establecimiento),
		typeof(Producto),
		typeof(Proveedor),
		typeof(PresentacionProveedor),
		typeof(VersionReceta),
		typeof(IngredienteReceta),
		typeof(OrdenProduccion),
		typeof(NecesidadIngrediente),
		typeof(PedidoCompra),
		typeof(DetallePedidoCompra),
		typeof(AsignacionCompra)
	};

	private static readonly Dictionary<Type, string> NombresFixture = new Dictionary<Type, string>
	{
		[typeof(Empresa)] = "Company",
		[typeof(UnidadMedida)] = "Unit",
		[typeof(Establecimiento)] = "Establishment",
		[typeof(Producto)] = "Product",
		[typeof(Proveedor)] = "Supplier",
		[typeof(PresentacionProveedor)] = "SupplierPack",
		[typeof(VersionReceta)] = "RecipeVersion",
		[typeof(IngredienteReceta)] = "Ingredient",
		[typeof(OrdenProduccion)] = "ProductionOrder",
		[typeof(NecesidadIngrediente)] = "Requirement",
		[typeof(PedidoCompra)] = "PurchaseOrder",
		[typeof(DetallePedidoCompra)] = "PurchaseLine",
		[typeof(AsignacionCompra)] = "Allocation"
	};

	private static readonly Dictionary<string, string> PropiedadesJson = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["Id"] = "id",
		["Revision"] = "revision",
		["Dimension"] = "dimension",
		["EmpresaId"] = "companyId",
		["Nombre"] = "name",
		["UnidadBaseId"] = "baseUnitId",
		["FactorConversion"] = "factor",
		["ProveedorId"] = "supplierId",
		["ProductoId"] = "productId",
		["UnidadMedidaId"] = "unitId",
		["Cantidad"] = "quantity",
		["CantidadBase"] = "baseAmount",
		["FamiliaRecetaId"] = "familyId",
		["NumeroVersion"] = "number",
		["ProductoResultadoId"] = "outputProductId",
        ["Rendimiento"] = "yield",
        ["CantidadRendimiento"] = "yieldQuantity",
        ["UnidadRendimientoId"] = "yieldUnitId",
        ["PorcionesRendimiento"] = "yieldPortions",
		["Instrucciones"] = "instructions",
		["Publicada"] = "published",
		["Ingredientes"] = "ingredients",
		["EstablecimientoId"] = "establishmentId",
		["VersionRecetaId"] = "recipeVersionId",
		["CantidadPlanificada"] = "quantity",
		["FechaPlanificada"] = "date",
		["CreadoPorId"] = "createdBy",
		["FechaCreacion"] = "createdAt",
		["Necesidades"] = "requirements",
		["OrdenProduccionId"] = "productionOrderId",
		["CantidadNecesaria"] = "amount",
		["Estado"] = "status",
		["EnviadoPorId"] = "submittedBy",
		["FechaEnvio"] = "submittedAt",
		["DecididoPorId"] = "decidedBy",
		["FechaDecision"] = "decidedAt",
		["MotivoDecision"] = "decisionReason",
		["Detalles"] = "lines",
		["PedidoCompraId"] = "purchaseOrderId",
		["PresentacionProveedorId"] = "supplierPackId",
		["NombrePresentacion"] = "packName",
		["CantidadBasePresentacion"] = "packBaseAmount",
		["CantidadPresentaciones"] = "packCount",
		["CantidadPedida"] = "orderedAmount",
		["NecesidadBruta"] = "grossNeed",
		["Asignaciones"] = "allocations",
		["DetallePedidoCompraId"] = "purchaseLineId",
		["NecesidadIngredienteId"] = "requirementId",
		["Liberada"] = "released"
	};

	public static string NombreFixture(Type tipo)
	{
		return NombresFixture[tipo];
	}

	public static string NombreJson(Type tipo, string propiedad)
	{
		string nombre;
		if (propiedad == "CantidadAsignada")
		{
			nombre = ((tipo == typeof(NecesidadIngrediente)) ? "allocated" : "amount");
		}
		else
		{
			nombre = PropiedadesJson[propiedad];
		}
		Type tipoDto = typeof(MapeadorDtos).GetMethods().Single((MethodInfo x) => x.Name == "ADto" && x.GetParameters()[0].ParameterType == tipo).ReturnType;
		JsonPropertyNameAttribute? atributo = (from x in tipoDto.GetProperties()
			select x.GetCustomAttribute<JsonPropertyNameAttribute>()).Single((JsonPropertyNameAttribute? x) => x?.Name == nombre);
		// Single selecciona por un Name no nulo: un atributo ausente no puede satisfacerlo.
		return atributo!.Name;
	}

	public static string NombreValorSintetico(Type tipo, string propiedad)
	{
		string nombre = NombreJson(tipo, propiedad);
		return char.ToUpperInvariant(nombre[0]) + nombre.Substring(1);
	}

	public static JsonObject SerializarEntidad(object entidad, Type tipo)
	{
		JsonObject resultado = new JsonObject();
		PropertyInfo[] properties = tipo.GetProperties();
		foreach (PropertyInfo propiedad in properties)
		{
			object? valor = propiedad.GetValue(entidad);
			JsonNode? json;
			if (valor is IList lista)
			{
				JsonArray elementos = new JsonArray();
				foreach (object elemento in lista)
				{
					elementos.Add(SerializarEntidad(elemento, elemento.GetType()));
				}
				json = elementos;
			}
			else
			{
				json = JsonSerializer.SerializeToNode(valor, propiedad.PropertyType, new JsonSerializerOptions(JsonSerializerDefaults.Web));
			}
			resultado.Add(NombreJson(tipo, propiedad.Name), json);
		}
		return resultado;
	}
}
