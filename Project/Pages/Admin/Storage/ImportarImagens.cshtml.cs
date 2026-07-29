using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Core.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Project.Features.Storage.Legacy;

namespace Project.Pages.Admin.Storage;

[Authorize]
public sealed class ImportarImagensModel(
    LegacyImageImportJobManager jobManager,
    LegacyImageImportReportService reportService,
    IOptions<StorageOptions> storageOptions) : PageModel
{
    [BindProperty]
    public ImportInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? JobId { get; set; }

    [BindProperty(SupportsGet = true)]
    public FilterInput Filters { get; set; } = new();

    public LegacyImageImportSnapshot? CurrentJob { get; private set; }
    public LegacyImageImportDashboardSnapshot? Dashboard { get; private set; }
    public LegacyImageImportJobDetails? CurrentDetails { get; private set; }
    public IReadOnlyList<LegacyImageImportJobListItem> Jobs { get; private set; } = [];
    public bool R2WriteEnabled => storageOptions.Value.UseR2ForWrites && storageOptions.Value.R2.IsConfigured;

    public async Task OnGet(CancellationToken ct)
    {
        Input.BaseUrl = string.IsNullOrWhiteSpace(Input.BaseUrl)
            ? "https://andersonmultimarcas.com.br"
            : Input.BaseUrl;

        var filters = ToFilters();
        Dashboard = await reportService.GetDashboardAsync(filters, ct);
        Jobs = await jobManager.ListJobsAsync(filters, ct);
        if (JobId.HasValue)
        {
            CurrentDetails = await reportService.GetJobDetailsAsync(JobId.Value, filters, null, ct);
            CurrentJob = CurrentDetails?.Summary ?? await jobManager.GetSnapshotAsync(JobId.Value, null, ct);
        }
    }

    public async Task<IActionResult> OnPostStart(CancellationToken ct)
    {
        if (Input.OverwriteExisting)
        {
            Input.OnlyWithoutBlobName = false;
        }

        if (!Uri.TryCreate(Input.BaseUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            ModelState.AddModelError(nameof(Input.BaseUrl), "Informe uma URL HTTPS valida.");
        }

        if (!Input.DryRun && !R2WriteEnabled)
        {
            ModelState.AddModelError(string.Empty, "Para executar a importacao real, configure Storage:Provider=R2 e Storage:R2.");
        }

        if (!ModelState.IsValid)
        {
            var filters = ToFilters();
            Dashboard = await reportService.GetDashboardAsync(filters, ct);
            Jobs = await jobManager.ListJobsAsync(filters, ct);
            return Page();
        }

        try
        {
            var job = await jobManager.StartAsync(new LegacyImageImportRequest
            {
                BaseUrl = baseUri!.ToString().TrimEnd('/'),
                OnlyWithoutBlobName = Input.OnlyWithoutBlobName,
                OverwriteExisting = Input.OverwriteExisting,
                DryRun = Input.DryRun,
                MaxVehicles = Input.MaxVehicles,
                StartId = Input.StartId
            }, User.FindFirstValue(ClaimTypes.NameIdentifier), User.Identity?.Name, ct);

            TempData["SuccessMessage"] = "Importacao enfileirada.";
            return RedirectToPage(new { jobId = job.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var filters = ToFilters();
            Dashboard = await reportService.GetDashboardAsync(filters, ct);
            Jobs = await jobManager.ListJobsAsync(filters, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCancel(int jobId, CancellationToken ct)
    {
        if (await jobManager.CancelAsync(jobId, User.FindFirstValue(ClaimTypes.NameIdentifier), User.Identity?.Name, ct))
        {
            TempData["WarningMessage"] = "Cancelamento solicitado.";
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnPostResume(int jobId, CancellationToken ct)
    {
        try
        {
            if (await jobManager.ResumeAsync(jobId, User.FindFirstValue(ClaimTypes.NameIdentifier), User.Identity?.Name, ct))
            {
                TempData["SuccessMessage"] = "Job reenfileirado.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnPostRetryFailures(int jobId, CancellationToken ct)
    {
        try
        {
            if (await jobManager.RetryFailuresAsync(jobId, User.FindFirstValue(ClaimTypes.NameIdentifier), User.Identity?.Name, ct))
            {
                TempData["SuccessMessage"] = "Falhas reenfileiradas.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnGetStatus(int jobId, int? afterLogIndex, CancellationToken ct)
    {
        var snapshot = await jobManager.GetSnapshotAsync(jobId, afterLogIndex, ct);
        return snapshot is null ? NotFound() : new JsonResult(snapshot);
    }

    public async Task<IActionResult> OnGetDashboard(CancellationToken ct)
        => new JsonResult(await reportService.GetDashboardAsync(ToFilters(), ct));

    public async Task<IActionResult> OnGetDetails(int jobId, int? afterLogIndex, CancellationToken ct)
    {
        var details = await reportService.GetJobDetailsAsync(jobId, ToFilters(), afterLogIndex, ct);
        return details is null ? NotFound() : new JsonResult(details);
    }

    public async Task<IActionResult> OnGetLogCsv(int jobId, CancellationToken ct)
    {
        var bytes = await jobManager.CsvBytesAsync(jobId, ct);
        if (bytes is null)
        {
            return NotFound();
        }

        return File(bytes, "text/csv; charset=utf-8", $"importacao-imagens-{jobId}.csv");
    }

    public async Task<IActionResult> OnGetJobExport(int jobId, string format, CancellationToken ct)
    {
        var bytes = await reportService.ExportJobAsync(jobId, format, ct);
        if (bytes is null)
        {
            return NotFound();
        }

        var normalized = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        var contentType = normalized switch
        {
            "json" => "application/json; charset=utf-8",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv; charset=utf-8"
        };

        return File(bytes, contentType, $"importacao-imagens-job-{jobId}.{normalized}");
    }

    public async Task<IActionResult> OnGetReportExport(int jobId, string format, CancellationToken ct)
    {
        var bytes = await reportService.ExportReportAsync(jobId, format, ct);
        if (bytes is null)
        {
            return NotFound();
        }

        var normalized = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        var contentType = normalized switch
        {
            "json" => "application/json; charset=utf-8",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv; charset=utf-8"
        };

        return File(bytes, contentType, $"relatorio-importacao-job-{jobId}.{normalized}");
    }

    private LegacyImageImportFilters ToFilters()
        => new()
        {
            Status = string.IsNullOrWhiteSpace(Filters.Status) ? null : Filters.Status,
            Search = string.IsNullOrWhiteSpace(Filters.Search) ? null : Filters.Search,
            VehicleId = Filters.VehicleId is > 0 ? Filters.VehicleId : null,
            Marca = string.IsNullOrWhiteSpace(Filters.Marca) ? null : Filters.Marca,
            Modelo = string.IsNullOrWhiteSpace(Filters.Modelo) ? null : Filters.Modelo,
            PeriodStart = Filters.PeriodStart,
            PeriodEnd = Filters.PeriodEnd,
            User = string.IsNullOrWhiteSpace(Filters.User) ? null : Filters.User,
            OnlyErrors = Filters.OnlyErrors,
            OnlyPending = Filters.OnlyPending,
            OnlyCompleted = Filters.OnlyCompleted
        };

    public sealed class ImportInput
    {
        [Required]
        [Url]
        [Display(Name = "URL Base do Site Legado")]
        public string BaseUrl { get; set; } = "https://andersonmultimarcas.com.br";

        [Display(Name = "Importar apenas veiculos sem BlobName valido no R2")]
        public bool OnlyWithoutBlobName { get; set; } = true;

        [Display(Name = "Sobrescrever imagens existentes")]
        public bool OverwriteExisting { get; set; }

        [Display(Name = "Executar em modo Dry Run")]
        public bool DryRun { get; set; } = true;

        [Range(1, int.MaxValue)]
        [Display(Name = "Quantidade maxima de veiculos")]
        public int? MaxVehicles { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Iniciar pelo ID")]
        public int? StartId { get; set; }
    }

    public sealed class FilterInput
    {
        public string? Status { get; set; }
        public string? Search { get; set; }
        public int? VehicleId { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public string? User { get; set; }
        public bool OnlyErrors { get; set; }
        public bool OnlyPending { get; set; }
        public bool OnlyCompleted { get; set; }
    }
}
