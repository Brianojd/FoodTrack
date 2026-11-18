using FoodTrace.API.Infrastructure.Identity;
using FoodTrace.API.Infrastructure.DependencyInjection;
using FoodTrace.API.Infrastructure.Middleware;
using FoodTrace.API.Infrastructure.OpenApi;

var builder = WebApplication.CreateBuilder(args.Where(x => x != "--bootstrap").ToArray());
builder.Services.AgregarFoodTrace(builder.Configuration);
if (builder.Environment.IsDevelopment())
    builder.Services.AgregarSwaggerFoodTrace();

var app = builder.Build();
if (args.Contains("--bootstrap"))
{
    // El arranque inicial solo acepta secretos de usuario y variables de entorno, no JSON.
    var local = new ConfigurationBuilder().AddUserSecrets<Program>(optional: true).AddEnvironmentVariables().Build();
    await InicializadorIdentidad.EjecutarAsync(app.Services, local);
    return;
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodTrace API v1"));
}
app.UsarErroresFoodTrace();
app.UseAuthentication();
app.UseMiddleware<MembresiaMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
