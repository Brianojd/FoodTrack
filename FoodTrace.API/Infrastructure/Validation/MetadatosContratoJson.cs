using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace FoodTrace.API.Infrastructure.Validation;

public sealed class MetadatosContratoJson : IDisplayMetadataProvider, IValidationMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext contexto)
    {
        var nombre = NombreContrato(contexto.Attributes);
        if (nombre is not null) contexto.DisplayMetadata.DisplayName = () => nombre;
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext contexto)
    {
        var nombre = NombreContrato(contexto.Attributes);
        if (nombre is not null) contexto.ValidationMetadata.ValidationModelName = nombre;
    }

    private static string? NombreContrato(IReadOnlyList<object> atributos)
    {
        var nombreExplicito = atributos.OfType<DisplayAttribute>().FirstOrDefault()?.GetName();
        if (nombreExplicito is not null) return nombreExplicito;
        var nombreJson = atributos.OfType<JsonPropertyNameAttribute>().FirstOrDefault()?.Name;
        if (string.IsNullOrEmpty(nombreJson)) return null;
        // El contrato histórico de ModelState usa PascalCase, no camelCase JSON.
        return char.ToUpperInvariant(nombreJson[0]) + nombreJson[1..];
    }
}
