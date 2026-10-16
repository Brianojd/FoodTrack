
using FoodTrace.API.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodTrace.API.Infrastructure.Middleware;

public sealed class ErroresMiddleware(
    RequestDelegate siguiente,
    IHostEnvironment entorno)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        if (!entorno.IsDevelopment() && !contexto.Request.IsHttps)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        try
        {
            await siguiente(contexto);
        }
        catch (Exception error) when (
            error is ExcepcionRegla
                or ExcepcionConflicto
                or KeyNotFoundException
                or DbUpdateException
                or OverflowException)
        {
            if (contexto.Response.HasStarted)
                throw;

            var estado = error switch
            {
                KeyNotFoundException => 404,
                ExcepcionConflicto
                    or DbUpdateConcurrencyException
                    or DbUpdateException => 409,
                _ => 400
            };

            var mensaje = error switch
            {
                DbUpdateException =>
                    "The record changed or conflicts with saved data. Refresh and try again.",

                OverflowException =>
                    "Quantity calculation exceeds the supported range. Use smaller quantities.",

                KeyNotFoundException =>
                    "Record not found in your company.",

                _ => error.Message
            };

            var problema = new ProblemDetails
            {
                Status = estado,
                Title = estado switch
                {
                    404 => "Not Found",
                    409 => "Conflict",
                    _ => "Bad Request"
                },
                Detail = mensaje,
                Instance = contexto.Request.Path
            };

            contexto.Response.StatusCode = estado;
            contexto.Response.ContentType = "application/problem+json";

            await contexto.Response.WriteAsJsonAsync(problema);
        }
    }
}
