using Microsoft.AspNetCore.Identity;

namespace FoodTrace.API.Domain.Entities;

public sealed class Empresa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = "";
}

public abstract class EntidadEmpresa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}

public sealed class Establecimiento : EntidadEmpresa
{
    public string Nombre { get; set; } = "";
}

public sealed class AppUser : IdentityUser
{
    public Guid EmpresaId { get; set; }
    public bool Habilitado { get; set; } = true;
}
