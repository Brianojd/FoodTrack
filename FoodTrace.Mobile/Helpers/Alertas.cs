namespace FoodTrace.Mobile.Helpers;

public static class Alertas
{
    public static Task ErrorAsync(Page page, string mensaje) => page.DisplayAlertAsync("Error", mensaje, "Aceptar");

    public static Task<bool> ConfirmarAsync(Page page, string pregunta) => page.DisplayAlertAsync("Confirmar", pregunta, "Confirmar", "Cancelar");

    public static Task ExitoAsync(Page page, string mensaje) => page.DisplayAlertAsync("Éxito", mensaje, "Aceptar");

    public static Task InformacionAsync(Page page, string mensaje) => page.DisplayAlertAsync("Información", mensaje, "Aceptar");
}
