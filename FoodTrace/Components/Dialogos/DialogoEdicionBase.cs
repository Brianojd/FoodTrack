using FoodTrace.Services.Api;
using FoodTrace.Services.Interfaces;
using FoodTrace.Models.Validacion;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FoodTrace.Components.Dialogos;

// Shared lifecycle, not a CRUD engine: each typed dialog owns its operation and dirty comparison.
public abstract class DialogoEdicionBase : ComponentBase, IDisposable
{
    [Inject] protected ISesionFoodTrace Sesion { get; set; } = null!;
    [CascadingParameter] protected IMudDialogInstance Dialogo { get; set; } = null!;
    public string FormId { get; } = "edicion-" + Guid.NewGuid().ToString("N");
    public bool Guardando { get; private set; }
    public bool ConfirmarDescarte { get; private set; }
    public string ConfirmacionGuardado { get; private set; } = "";
    public string Error { get; private set; } = "";
    public string? TraceId { get; private set; }
    public EstadoValidacionServidor Validacion { get; } = new();
    public bool Bloqueado => Guardando || ConfirmarDescarte || ConfirmacionGuardado.Length != 0;
    protected abstract bool TieneCambios { get; }
    protected virtual string RolRequerido => "admin";
    protected void MostrarError(string mensaje) => Error = mensaje;
    protected CancellationToken Cancelacion => vida.Token;
    protected bool Vigente => !eliminado && !vida.IsCancellationRequested && version == Sesion.VersionSesion && Sesion.Identidad is not null;
    private readonly CancellationTokenSource vida = new();
    private long version;
    private bool eliminado;
    private Func<Task>? guardadoConfirmado;

    protected override void OnInitialized()
    {
        version = Sesion.VersionSesion;
        Sesion.SesionCambiada += CambiarSesion;
    }

    public Task PedirCancelarAsync()
    {
        if (Guardando || !Vigente) return Task.CompletedTask;
        if (TieneCambios) ConfirmarDescarte = true;
        else CerrarSinGuardar();
        return InvokeAsync(StateHasChanged);
    }

    public Task TeclaAsync(KeyboardEventArgs args) => args.Key == "Escape" ? PedirCancelarAsync() : Task.CompletedTask;
    public void SeguirEditando() { if (!Guardando) { ConfirmarDescarte = false; ConfirmacionGuardado = ""; guardadoConfirmado = null; } }
    public void Descartar() { if (!Guardando) CerrarSinGuardar(); }
    private void CerrarSinGuardar() { Validacion.Limpiar(); LimpiarDatosPrivados(); Dialogo.Cancel(); }

    public async Task ConfirmarGuardadoAsync()
    {
        if (Guardando || guardadoConfirmado is null || !Vigente) return;
        var accion = guardadoConfirmado;
        guardadoConfirmado = null;
        ConfirmacionGuardado = "";
        await accion();
    }

    protected Task RevisarGuardadoAsync<T>(Func<CancellationToken, Task<T>> accion, string consecuencia)
    {
        if (Bloqueado || !Vigente) return Task.CompletedTask;
        ConfirmacionGuardado = consecuencia;
        guardadoConfirmado = () => GuardarEnServidorAsync(accion);
        return InvokeAsync(StateHasChanged);
    }

    protected async Task GuardarEnServidorAsync<T>(Func<CancellationToken, Task<T>> accion)
    {
        if (Bloqueado || !Vigente || !Sesion.TieneRol(RolRequerido)) return;
        Guardando = true;
        Error = "";
        TraceId = null;
        Validacion.IniciarSolicitud();
        var confirmado = false;
        try
        {
            await Dialogo.SetOptionsAsync(Dialogo.Options with { BackdropClick = false, CloseOnEscapeKey = false, CloseButton = false });
            if (!Vigente) return;
            await InvokeAsync(StateHasChanged);
            var respuesta = await accion(vida.Token);
            if (!Vigente) return;
            confirmado = true;
            // Success exists only after a server acknowledgement, never after a swallowing page wrapper.
            LimpiarDatosPrivados();
            Dialogo.Close(DialogResult.Ok(respuesta));
        }
        catch (OperationCanceledException)
        {
            if (Vigente) Error = "No se pudo confirmar la operación. Consulte la lista antes de volver a enviarla. Sus valores se conservan.";
        }
        catch (ExcepcionApi ex)
        {
            if (Vigente)
            {
                Error = ex.Message;
                TraceId = ex.TraceId;
                Validacion.Publicar(ex);
                await AlErrorApiAsync(ex);
            }
        }
        catch (Exception)
        {
            if (Vigente) Error = "No se pudo confirmar el guardado. Sus valores se conservan. Compruebe la conexión y consulte la lista antes de repetir la operación.";
        }
        finally
        {
            Guardando = false;
            if (!confirmado && Vigente) await Dialogo.SetOptionsAsync(Dialogo.Options with { BackdropClick = true, CloseOnEscapeKey = false, CloseButton = false });
        }
    }

    public async Task CambiarBreakpointAsync(Breakpoint breakpoint)
    {
        if (Vigente) await Dialogo.SetOptionsAsync(Dialogo.Options with
        {
            FullScreen = breakpoint is Breakpoint.Xs or Breakpoint.Sm,
            BackdropClick = !Guardando, CloseOnEscapeKey = false, CloseButton = false
        });
    }

    private void CambiarSesion()
    {
        if (version == Sesion.VersionSesion) return;
        vida.Cancel();
        Validacion.Limpiar();
        LimpiarDatosPrivados();
        _ = InvokeAsync(() => Dialogo.Cancel());
    }

    protected virtual void LimpiarDatosPrivados() { }
    protected virtual Task AlErrorApiAsync(ExcepcionApi ex) => Task.CompletedTask;
    public virtual void Dispose()
    {
        eliminado = true;
        Validacion.Limpiar();
        Sesion.SesionCambiada -= CambiarSesion;
        vida.Cancel();
        LimpiarDatosPrivados();
        guardadoConfirmado = null;
        vida.Dispose();
    }
}
