using System.Diagnostics;
using FoodTrace.API.Application.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Middleware;

public sealed class ManejadorExcepciones(IProblemDetailsService problemas, ILogger<ManejadorExcepciones> registro) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception error, CancellationToken cancelacion)
    {
        // The framework owns already-started responses and disconnected requests.
        if (contexto.Response.HasStarted || contexto.RequestAborted.IsCancellationRequested) return false;
        var estado = error switch
        {
            ExcepcionRegla or OverflowException => 400,
            KeyNotFoundException => 404,
            ExcepcionConflicto or DbUpdateConcurrencyException => 409,
            DbUpdateException { InnerException: SqlException sql } when sql.Number is 2601 or 2627 => 409,
            BadHttpRequestException peticion => peticion.StatusCode,
            _ => 500
        };
        if (estado >= 500)
            registro.LogError(error, "Error inesperado de API. TraceId: {TraceId}.", Activity.Current?.Id ?? contexto.TraceIdentifier);
        var detalle = error switch
        {
            OverflowException => "El cálculo supera el rango admitido. Utilice cantidades menores.",
            ExcepcionRegla => "Revise los datos, las cantidades, las unidades y el estado requerido para esta operación.",
            _ when estado == 404 => "El recurso solicitado no está disponible.",
            _ when estado == 409 => "Los datos cambiaron o entran en conflicto con un registro guardado. Revise la información antes de reintentar.",
            _ => null
        };
        contexto.Response.StatusCode = estado;
        await problemas.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = error is ExcepcionRegla { Errores: { } errores }
                ? new ValidationProblemDetails(errores) { Status = estado, Detail = detalle }
                : new ProblemDetails { Status = estado, Detail = detalle }
        });
        return true;
    }
}
