using FoodTrace.Mobile.Configuration;
using FoodTrace.Mobile.Controls;
using FoodTrace.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FoodTrace.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            var settings = ApiSettings.Current;
            var httpClient = new HttpClient
            {
                BaseAddress = settings.BaseAddress,
                Timeout = settings.TimeoutSeconds > 0 ? TimeSpan.FromSeconds(settings.TimeoutSeconds) : TimeSpan.FromSeconds(30)
            };
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(settings);
            builder.Services.AddSingleton(httpClient);
            builder.Services.AddSingleton<ApiClient>();
            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<OrdenesRecepcionService>();
            builder.Services.AddSingleton<SalidasCocinaService>();
#if ANDROID
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("FoodTraceLoginEntry", (handler, view) =>
            {
                if (view is LoginEntry && handler.PlatformView is Android.Widget.EditText editText)
                    editText.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            });
#endif
#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
