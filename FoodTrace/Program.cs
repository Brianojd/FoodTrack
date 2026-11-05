using FoodTrace.Components;
using FoodTrace.Services.Api;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("FoodTrace", cliente => cliente.BaseAddress =
    new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7240/"));
builder.Services.AgregarClienteFoodTrace();
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
