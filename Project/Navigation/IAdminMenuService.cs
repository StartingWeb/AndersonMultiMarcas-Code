using System.Security.Claims;

namespace Project.Navigation;

public interface IAdminMenuService
{
    Task<AdminMenuViewModel> BuildAsync(ClaimsPrincipal user, string? currentPath, CancellationToken cancellationToken = default);
}
