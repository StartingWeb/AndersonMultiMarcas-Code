using Microsoft.AspNetCore.Mvc;
using Project.Navigation;

namespace Project.ViewComponents;

public sealed class MenuViewComponent : ViewComponent
{
    private readonly IAdminMenuService _adminMenuService;

    public MenuViewComponent(IAdminMenuService adminMenuService)
    {
        _adminMenuService = adminMenuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _adminMenuService.BuildAsync(
            HttpContext.User,
            HttpContext.Request.Path.Value,
            HttpContext.RequestAborted);

        return View(model);
    }
}
