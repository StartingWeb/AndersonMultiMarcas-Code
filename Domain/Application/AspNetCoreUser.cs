using Microsoft.AspNetCore.Identity;

namespace Domain.Application;

public class AspNetCoreUser : IdentityUser<Guid>
{
    public string? NomeCompleto { get; set; }
}
