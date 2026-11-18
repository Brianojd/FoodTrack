using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodTrace.API.Infrastructure.OpenApi;

public static class SwaggerFoodTrace
{
    public static IServiceCollection AgregarSwaggerFoodTrace(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xml = Path.Combine(AppContext.BaseDirectory, "FoodTrace.API.xml");
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FoodTrace API",
                Version = "v1",
                Description = "API de trazabilidad alimentaria. Para autenticar operaciones protegidas: 1) ejecutar `POST /auth/login` con JSON `{ \"email\": \"...\", \"password\": \"...\" }`; 2) copiar el token Bearer de la respuesta; 3) pulsar **Authorize**; 4) pegar solamente el token, sin escribir `Bearer`; 5) pulsar **Authorize** y cerrar. Swagger no contiene credenciales ni tokens reales."
            });
            if (File.Exists(xml)) options.IncludeXmlComments(xml, includeControllerXmlComments: true);
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "Bearer token",
                Description = "Pegá únicamente el token devuelto por POST /auth/login. Swagger agregará automáticamente el encabezado Authorization: Bearer <token>."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
            options.TagActionsBy(api => [ObtenerTag(api)]);
            options.DocumentFilter<TagsDocumentFilter>();
            options.OperationFilter<OperacionFoodTraceFilter>();
            options.SchemaFilter<EsquemaRecepcionFilter>();
        });
        return services;
    }

    private static string ObtenerTag(ApiDescription api)
    {
        var path = api.RelativePath?.Split('?')[0].Trim('/').ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("auth/", StringComparison.Ordinal)) return "Compartido · Autenticación";
        if (path == "api/receipt-differences") return "Web · Supervisión de diferencias";
        if (path.StartsWith("api/receipt-orders/", StringComparison.Ordinal) && path.EndsWith("/receipt", StringComparison.Ordinal) && api.HttpMethod == "POST")
            return "Mobile · Recepción de mercadería";
        if (path.StartsWith("api/receipt-orders", StringComparison.Ordinal)) return "Compartido · Órdenes de recepción";
        if (path.StartsWith("api/purchases", StringComparison.Ordinal)) return "Web · Compras";
        if (path.StartsWith("api/recipes", StringComparison.Ordinal) || path.StartsWith("api/production", StringComparison.Ordinal) || path.StartsWith("api/needs", StringComparison.Ordinal))
            return "Web · Recetas y producción";
        if (path.StartsWith("api/users", StringComparison.Ordinal) || path is "api/company" or "api/establishments" or "api/products" or "api/suppliers" or "api/packs")
            return "Web · Catálogos y usuarios";
        return "Compartido · Diagnóstico";
    }
}

internal sealed class TagsDocumentFilter : IDocumentFilter
{
    private static readonly (string Name, string Description)[] Tags =
    [
        ("Compartido · Autenticación", "Login y sesión de identidad. El login es público; las demás operaciones requieren Bearer."),
        ("Compartido · Órdenes de recepción", "Listado y detalle de órdenes pendientes disponibles para Web y Mobile."),
        ("Mobile · Recepción de mercadería", "Confirmación física de recepción por parte del operario."),
        ("Web · Compras", "Preparación y envío de compras desde la aplicación Web."),
        ("Web · Supervisión de diferencias", "Consulta administrativa de faltantes registrados en recepciones."),
        ("Web · Recetas y producción", "Administración de recetas, producción y necesidades."),
        ("Web · Catálogos y usuarios", "Administración de empresa, catálogos y usuarios."),
        ("Compartido · Diagnóstico", "Endpoints técnicos restantes del API.")
    ];

    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Tags = Tags.Select(x => new OpenApiTag { Name = x.Name, Description = x.Description }).ToHashSet();
    }
}

internal sealed class OperacionFoodTraceFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var protectedOperation = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
        if (!protectedOperation) operation.Security = [];
        AddResponse(operation, context.SchemaGenerator, context.SchemaRepository, "400", "ValidationProblemDetails: la solicitud no cumple el contrato o las reglas de negocio; incluye el mapa de errores de validación cuando corresponde.", typeof(ValidationProblemDetails));
        if (protectedOperation)
        {
            AddResponse(operation, context.SchemaGenerator, context.SchemaRepository, "401", "ProblemDetails: falta autenticación Bearer válida.", typeof(ProblemDetails));
            AddResponse(operation, context.SchemaGenerator, context.SchemaRepository, "403", "ProblemDetails: el usuario autenticado no tiene el rol requerido.", typeof(ProblemDetails));
        }
        if (context.ApiDescription.RelativePath?.Contains("receipt", StringComparison.OrdinalIgnoreCase) == true)
        {
            operation.Summary ??= "Recepción física de mercadería";
            operation.Description ??= "La confirmación recibe entre cero y la cantidad esperada de cada línea. Deben enviarse cantidades explícitas para todas las líneas de la orden. La operación cierra definitivamente la orden, registra faltantes y crea inventario únicamente por la cantidad recibida. Un reintento idéntico devuelve la recepción existente; una revisión obsoleta o datos distintos generan conflicto.";
            AddResponse(operation, context.SchemaGenerator, context.SchemaRepository, "404", "ProblemDetails: la orden o recepción no existe en la empresa actual.", typeof(ProblemDetails));
            AddResponse(operation, context.SchemaGenerator, context.SchemaRepository, "409", "ProblemDetails: conflicto de revisión o reintento con datos diferentes.", typeof(ProblemDetails));
        }
        if (context.ApiDescription.RelativePath?.Equals("api/receipt-differences", StringComparison.OrdinalIgnoreCase) == true)
            operation.Description ??= "Devuelve únicamente faltantes de recepciones persistidas, paginados y aislados por empresa. Requiere rol admin.";
        if (context.ApiDescription.RelativePath?.Equals("auth/login", StringComparison.OrdinalIgnoreCase) == true)
        {
            operation.Summary ??= "Obtener token Bearer";
            operation.Description ??= "Enviar email y password en el cuerpo JSON. Si las credenciales son válidas, la respuesta contiene el token Bearer emitido por Identity; usar ese token en Authorize para probar las operaciones protegidas. No se documentan credenciales reales.";
            if (operation.Responses is not null && !operation.Responses.ContainsKey("401"))
                operation.Responses["401"] = new OpenApiResponse { Description = "Credenciales inválidas o usuario deshabilitado." };
        }
    }

    private static void AddResponse(OpenApiOperation operation, ISchemaGenerator generator, SchemaRepository repository, string code, string description, Type bodyType)
    {
        if (operation.Responses is not null && !operation.Responses.ContainsKey(code))
        {
            operation.Responses[code] = new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/problem+json"] = new OpenApiMediaType
                    {
                        Schema = generator.GenerateSchema(bodyType, repository)
                    }
                }
            };
        }
    }
}

internal sealed class EsquemaRecepcionFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema mutableSchema) return;
        var required = mutableSchema.Required ??= new HashSet<string>();
        if (context.Type.Name == "ConfirmarRecepcionDto")
        {
            required.Add("revision");
            required.Add("details");
        }
        if (context.Type.Name == "DetalleRecepcionEntradaDto")
        {
            required.Add("receiptOrderLineId");
            required.Add("receivedPackCount");
        }
    }
}
