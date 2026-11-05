using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FoodTrace.API.Infrastructure.Middleware;

public static class PoliticaProblemDetails
{
    public static IServiceCollection AgregarErroresFoodTrace(this IServiceCollection servicios)
    {
        // Register before MVC and the default writer: API errors always use problem JSON,
        // including when a browser or proxy requests HTML.
        servicios.AddSingleton<IProblemDetailsWriter, EscritorProblemDetails>();
        servicios.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = contexto =>
            Completar(contexto.HttpContext, contexto.ProblemDetails));
        servicios.AddExceptionHandler<ManejadorExcepciones>();
        servicios.Configure<ApiBehaviorOptions>(opciones =>
        {
            opciones.InvalidModelStateResponseFactory = contexto =>
            {
                var problema = new ValidationProblemDetails(contexto.ModelState) { Status = 400 };
                Completar(contexto.HttpContext, problema);
                return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
            };
        });
        return servicios;
    }

    public static IApplicationBuilder UsarErroresFoodTrace(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(new ExceptionHandlerOptions { AllowStatusCode404Response = true });
        app.UseStatusCodePages(async contexto =>
        {
            var http = contexto.HttpContext;
            if (http.Response.HasStarted || http.RequestAborted.IsCancellationRequested) return;
            await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
                new ProblemDetailsContext { HttpContext = http, ProblemDetails = new ProblemDetails { Status = http.Response.StatusCode } });
        });
        // Preserve the previous non-development HTTPS guard after redirection.
        app.Use(async (contexto, siguiente) =>
        {
            if (!contexto.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment() && !contexto.Request.IsHttps)
            {
                contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            await siguiente(contexto);
        });
        return app;
    }

    public static void Completar(HttpContext http, ProblemDetails problema)
    {
        var estado = problema.Status ?? http.Response.StatusCode;
        problema.Status = estado;
        problema.Type = $"https://httpstatuses.io/{estado}";
        problema.Title = estado switch
        {
            400 => "Datos no válidos", 401 => "Autenticación requerida", 403 => "Acceso no permitido",
            404 => "Recurso no disponible", 405 => "Método no permitido", 409 => "Conflicto con los datos guardados",
            415 => "Formato no admitido", 429 => "Demasiadas solicitudes", _ => estado >= 500 ? "Error del servicio" : "Solicitud no completada"
        };
        problema.Instance = http.Request.Path;
        problema.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;
        if (estado >= 500) problema.Detail = "No se pudo completar la solicitud. Inténtelo nuevamente más tarde.";
    }
}

internal sealed class EscritorProblemDetails : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext contexto) => true;

    public async ValueTask WriteAsync(ProblemDetailsContext contexto)
    {
        var http = contexto.HttpContext;
        if (http.Response.HasStarted || http.RequestAborted.IsCancellationRequested) return;
        PoliticaProblemDetails.Completar(http, contexto.ProblemDetails);
        http.Response.StatusCode = contexto.ProblemDetails.Status!.Value;
        http.Response.ContentType = "application/problem+json";
        if (HttpMethods.IsHead(http.Request.Method)) return;
        // Keep the runtime type so a ValidationProblemDetails never loses its errors.
        await http.Response.WriteAsJsonAsync(contexto.ProblemDetails, contexto.ProblemDetails.GetType(),
            options: null, contentType: "application/problem+json", cancellationToken: http.RequestAborted);
    }
}
