using FoodTrace.Mobile.Pages;
using FoodTrace.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FoodTrace.Mobile
{
    public partial class App : Application
    {
        private readonly SessionService sessionService;
        private readonly ILogger<App> logger;
        private readonly ILogger<LoginPage> loginPageLogger;

        public App(SessionService sessionService, ILogger<App> logger, ILogger<LoginPage> loginPageLogger)
        {
            this.sessionService = sessionService;
            this.logger = logger;
            this.loginPageLogger = loginPageLogger;
            try
            {
                InitializeComponent();
                logger.LogDebug("App.InitializeComponent completado; recursos XAML de aplicación cargados.");
            }
            catch (Exception exception)
            {
                logger.LogCritical("Fallo al ejecutar App.InitializeComponent y cargar recursos XAML: {Exception}", exception.ToString());
                throw;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                logger.LogDebug("Creando LoginPage como pantalla inicial.");
                var loginPage = new LoginPage(sessionService, loginPageLogger);
                NavigationPage.SetHasNavigationBar(loginPage, false);
                var navigation = new NavigationPage(loginPage);
                return new(navigation);
            }
            catch (Exception exception)
            {
                logger.LogCritical("Fallo al crear LoginPage durante el arranque: {Exception}", exception.ToString());
                throw;
            }
        }
    }
}
