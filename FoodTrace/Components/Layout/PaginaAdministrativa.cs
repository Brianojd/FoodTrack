using FoodTrace.Dtos.Comunes;
using FoodTrace.Services.Api;
using FoodTrace.Services.Interfaces;
using FoodTrace.Services.Estado;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FoodTrace.Components.Layout;

// Coordina el ciclo de vida de una página; los componentes visuales no acceden a HTTP.
public abstract class PaginaAdministrativa : ComponentBase, IDisposable
{
    [Inject] protected ISesionFoodTrace Sesion { get; set; } = null!;
    [Inject] protected ICatalogosFoodTrace Catalogos { get; set; } = null!;
    [Inject] protected IDialogService Dialogos { get; set; } = null!;
    [Inject] protected ILogger<PaginaAdministrativa> Registro { get; set; } = null!;
    [Inject] protected EstadoAdministracion Formularios { get; set; } = null!;
    protected CatalogosDto? Datos { get; set; }
    protected bool Ocupado => ejecutando || confirmando;
    protected string Error { get; private set; } = "";
    protected string Aviso { get; private set; } = "";
    protected void MostrarError(string mensaje) => Error = mensaje;
    protected void MostrarAviso(string mensaje) => Aviso = mensaje;
    private readonly CancellationTokenSource vida = new();
    private long version;
    private bool ejecutando, confirmando;
    private IDialogReference? confirmacionActiva;
    private IDialogReference? editorActivo;
    private bool guardadoConfirmadoPendienteDeConsulta;
    protected bool Vigente => !vida.IsCancellationRequested && version == Sesion.VersionSesion;

    protected override async Task OnInitializedAsync()
    {
        version = Sesion.VersionSesion;
        if (Sesion.Identidad is not null) await ActualizarAsync();
    }

    protected virtual Task ActualizarAsync() => EjecutarAsync(async ct => await CargarCatalogosAsync(ct), esConsulta: true);

    protected async Task CargarCatalogosAsync(CancellationToken ct)
    {
        var nuevos = await Catalogos.ObtenerCatalogosAsync(ct);
        if (Vigente) Datos = nuevos;
    }

    protected async Task EjecutarAsync(Func<CancellationToken, Task> accion, string aviso = "", bool esConsulta = false)
    {
        if (Ocupado || !Vigente) return;
        ejecutando = true; Error = ""; Aviso = "";
        try
        {
            await accion(vida.Token);
            if (Vigente)
            {
                Aviso = aviso;
                if (esConsulta) guardadoConfirmadoPendienteDeConsulta = false;
            }
        }
        catch (OperationCanceledException)
        {
            if (Vigente && esConsulta) Error = "La consulta no se completó. Se conservan los últimos datos confirmados. Puede volver a actualizar la lista.";
        }
        catch (ExcepcionApi ex)
        {
            if (Vigente) Error = ex.Message + (ex.ErroresValidacion.Count == 0 ? "" : " " + string.Join(" ", ex.ErroresValidacion.Values.SelectMany(x => x)))
                + (ex.TraceId is null ? "" : $" Referencia: {ex.TraceId}");
        }
        catch (Exception ex)
        {
            // Solo se registra el tipo, nunca cuerpos, contraseñas, tokens ni mensajes del servidor.
            Registro.LogWarning("No se completó la operación administrativa ({Tipo}).", ex.GetType().Name);
            if (Vigente) Error = "No se pudo completar la operación. Compruebe la conexión e inténtelo nuevamente. Sus datos se conservan.";
        }
        finally
        {
            if (Vigente && esConsulta && guardadoConfirmadoPendienteDeConsulta && Error.Length != 0)
                Error = "La operación se guardó, pero no se pudo actualizar la lista. Se conservan los datos confirmados. " + Error;
            ejecutando = false;
        }
    }

    protected virtual async Task<bool> ConfirmarAsync(string titulo, string mensaje)
    {
        if (Ocupado || !Vigente) return false;
        confirmando = true;
        try
        {
            var parametros = new DialogParameters<Shared.DialogoConfirmacion> { { x => x.Mensaje, mensaje } };
            var dialogo = await Dialogos.ShowAsync<Shared.DialogoConfirmacion>(titulo, parametros);
            confirmacionActiva = dialogo;
            if (!Vigente) dialogo.Close(DialogResult.Cancel());
            var resultado = await dialogo.Result;
            return Vigente && resultado is { Canceled: false };
        }
        finally { confirmacionActiva = null; confirmando = false; }
    }

    protected async Task AbrirEditorAsync<TDialogo, TResultado>(string titulo, DialogParameters<TDialogo> parametros, Action<TResultado> aplicar, MaxWidth ancho = MaxWidth.Small, string rolRequerido = "admin")
        where TDialogo : FoodTrace.Components.Dialogos.DialogoEdicionBase
    {
        if (Ocupado || !Vigente || !Sesion.TieneRol(rolRequerido)) return;
        confirmando = true;
        var respuestaConfirmada = false;
        try
        {
            var opciones = new DialogOptions { FullWidth = true, MaxWidth = ancho, BackdropClick = true, CloseOnEscapeKey = false, CloseButton = false, CloseOnNavigation = true };
            editorActivo = await Dialogos.ShowAsync<TDialogo>(titulo, parametros, opciones);
            if (!Vigente) editorActivo.Close(DialogResult.Cancel());
            var resultado = await editorActivo.Result;
            if (!Vigente || resultado is not { Canceled: false, Data: TResultado respuesta }) return;
            respuestaConfirmada = true;
            guardadoConfirmadoPendienteDeConsulta = true;
            aplicar(respuesta);
            Error = "";
            Aviso = "Se guardó la operación. La lista incorpora la respuesta confirmada de la API; puede actualizarla para consultar otros cambios.";
        }
        catch (Exception ex)
        {
            Registro.LogWarning("No se completó la coordinación de edición ({Tipo}).", ex.GetType().Name);
            if (Vigente) Error = respuestaConfirmada
                ? "La operación se guardó, pero no se pudo actualizar la lista local. Actualice la lista; no repita el guardado."
                : "No se pudo completar la apertura de edición. No se confirmó ningún guardado. Consulte la lista antes de volver a intentarlo.";
        }
        finally { editorActivo = null; confirmando = false; }
    }

    public virtual void Dispose()
    {
        vida.Cancel();
        confirmacionActiva?.Close(DialogResult.Cancel());
        editorActivo?.Close(DialogResult.Cancel());
        vida.Dispose();
    }
}
