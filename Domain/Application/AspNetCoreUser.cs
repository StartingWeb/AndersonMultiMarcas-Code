using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace Domain.Application;

public class AspNetCoreUser : IdentityUser
{
    public string? NomeCompleto { get; set; }
}
