using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.Services.Api;

public sealed class ExcepcionApi(HttpStatusCode estado, string mensaje, IReadOnlyDictionary<string, string>? erroresCampos = null,
    ProblemDetails? problema = null, IReadOnlyDictionary<string, string[]>? erroresValidacion = null, string? traceId = null) : Exception(mensaje)
{
    public HttpStatusCode Estado { get; } = estado;
    public IReadOnlyDictionary<string, string> ErroresCampos { get; } = erroresCampos ?? new Dictionary<string, string>();
    public ProblemDetails? Problema { get; } = problema;
    public IReadOnlyDictionary<string, string[]> ErroresValidacion { get; } = erroresValidacion ?? new Dictionary<string, string[]>();
    public string? TraceId { get; } = traceId;
}
