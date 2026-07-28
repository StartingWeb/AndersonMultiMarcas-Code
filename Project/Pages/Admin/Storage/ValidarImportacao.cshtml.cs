using System.ComponentModel.DataAnnotations;
using Core.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Project.Features.Storage.Validation;

namespace Project.Pages.Admin.Storage;

[Authorize]
public sealed class ValidarImportacaoModel(
    StorageImportValidationService validationService,
    IOptions<StorageOptions> storageOptions) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ValidationInput Input { get; set; } = new();

    public StorageImportValidationReport? Report { get; private set; }
    public bool R2Configured => storageOptions.Value.R2.IsConfigured;

    public async Task OnGet(CancellationToken ct)
    {
        if (Input.Run)
        {
            Report = await validationService.ValidateAsync(ToRequest(Input), ct);
        }
    }

    public async Task<IActionResult> OnGetExport(string format, CancellationToken ct)
    {
        var report = await validationService.ValidateAsync(ToRequest(Input), ct);
        var bytes = validationService.Export(report, format);
        var normalized = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        var contentType = normalized switch
        {
            "json" => "application/json; charset=utf-8",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv; charset=utf-8"
        };

        return File(bytes, contentType, $"validacao-importacao-{DateTime.UtcNow:yyyyMMddHHmmss}.{normalized}");
    }

    private static StorageImportValidationRequest ToRequest(ValidationInput input)
        => new()
        {
            VehicleId = input.VehicleId is > 0 ? input.VehicleId : null,
            Scope = string.IsNullOrWhiteSpace(input.Scope) ? StorageImportValidationScopes.All : input.Scope,
            MaxRecords = input.MaxRecords is > 0 ? input.MaxRecords : null
        };

    public sealed class ValidationInput
    {
        public bool Run { get; set; }

        [Display(Name = "ID do veiculo")]
        [Range(1, int.MaxValue)]
        public int? VehicleId { get; set; }

        [Display(Name = "Escopo")]
        public string Scope { get; set; } = StorageImportValidationScopes.All;

        [Display(Name = "Limite de registros")]
        [Range(1, int.MaxValue)]
        public int? MaxRecords { get; set; }
    }
}
