namespace Domain.Application;

public class AspNetMenuRole
{
    public Guid MenuId { get; set; }

    public Guid RoleId { get; set; }

    public AspNetMenu? Menu { get; set; }

    public AspNetCoreRole? Role { get; set; }
}