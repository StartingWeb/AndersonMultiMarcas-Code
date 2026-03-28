using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace Domain.Application;

public class AspNetCoreUser : IdentityUser<Guid>
{
    public string? NomeCompleto { get; set; }
}
