using System.Security.Claims;
using Core.Storage;
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Project.Features.Storage.R2Sync;

namespace Project.Pages.Admin.Ferramentas;

[Authorize(Roles = "Administrador,AdminConcessionaria,Desenvolvedor")]
public sealed class SincronizarImagensR2Model(
    R2VehicleImageSyncJobManager jobManager,
    ApplicationDbContext db,
    IOptions<StorageOptions> storageOptions) : PageModel
{
    public R2VehicleImageSyncSnapshot Snapshot { get; private set; } = null!;
    public int TotalVehiclesPreview { get; private set; }
    [BindProperty]
    public int? VehicleId { get; set; }
    public bool R2Configured => storageOptions.Value.R2.IsConfigured;
    public string BucketName => storageOptions.Value.R2.BucketName ?? "(nao configurado)";
    public string Prefix => StoragePath.LegacyImportedVehiclePrefix;

    public async Task OnGet(CancellationToken ct)
    {
        ViewData["Title"] = "Sincronizar Imagens R2";
        Snapshot = jobManager.GetSnapshot(null);
        TotalVehiclesPreview = Snapshot.TotalVehicles > 0
            ? Snapshot.TotalVehicles
            : await db.Veiculos.AsNoTracking().CountAsync(ct);
    }

    public async Task<IActionResult> OnPostStart(CancellationToken ct)
    {
        if (!R2Configured)
        {
            TempData["ErrorMessage"] = "Cloudflare R2 nao esta configurado. Verifique Storage:R2.";
            return RedirectToPage();
        }

        var started = await jobManager.StartAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name,
            ct);

        TempData[started ? "SuccessMessage" : "WarningMessage"] = started
            ? "Sincronizacao iniciada."
            : "Ja existe uma sincronizacao em andamento.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartVehicle(CancellationToken ct)
    {
        if (!R2Configured)
        {
            TempData["ErrorMessage"] = "Cloudflare R2 nao esta configurado. Verifique Storage:R2.";
            return RedirectToPage();
        }

        if (!VehicleId.HasValue || VehicleId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Informe um Id de veiculo valido.";
            return RedirectToPage();
        }

        var exists = await db.Veiculos.AsNoTracking().AnyAsync(x => x.Id == VehicleId.Value, ct);
        if (!exists)
        {
            TempData["ErrorMessage"] = $"Veiculo {VehicleId.Value} nao encontrado.";
            return RedirectToPage();
        }

        var started = await jobManager.StartAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name,
            VehicleId.Value,
            ct);

        TempData[started ? "SuccessMessage" : "WarningMessage"] = started
            ? $"Sincronizacao do veiculo {VehicleId.Value} iniciada."
            : "Ja existe uma sincronizacao em andamento.";

        return RedirectToPage();
    }

    public IActionResult OnPostCancel()
    {
        var cancelled = jobManager.Cancel();
        TempData[cancelled ? "WarningMessage" : "ErrorMessage"] = cancelled
            ? "Cancelamento solicitado."
            : "Nao ha sincronizacao ativa para cancelar.";

        return RedirectToPage();
    }

    public IActionResult OnGetStatus(int? afterLogIndex)
        => new JsonResult(jobManager.GetSnapshot(afterLogIndex));
}
