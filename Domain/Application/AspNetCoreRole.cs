using Microsoft.AspNetCore.Identity;

namespace Domain.Application;

public class AspNetCoreRole : IdentityRole<Guid>
{
    public string? Descricao { get; set; }
}
