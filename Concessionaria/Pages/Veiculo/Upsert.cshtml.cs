using Core;
using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Concessionaria.Pages.Veiculo;

public class UpsertModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IVeiculoService _veiculoService;
    private readonly IVeiculoCaracteristicaService _veiculoCaracteristicaService;
    private readonly IVeiculoMidiaService _veiculoMidiaService;
    private readonly IWebHostEnvironment _environment;

    public UpsertModel(
        ApplicationDbContext context,
        IVeiculoService veiculoService,
        IVeiculoCaracteristicaService veiculoCaracteristicaService,
        IVeiculoMidiaService veiculoMidiaService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _veiculoService = veiculoService;
        _veiculoCaracteristicaService = veiculoCaracteristicaService;
        _veiculoMidiaService = veiculoMidiaService;
        _environment = environment;
    }

    [BindProperty]
    public Domain.Veiculo Veiculo { get; set; } = new();

    [BindProperty]
    public VeiculoCaracteristica Caracteristica { get; set; } = new();

    [BindProperty]
    public List<IFormFile> NovasMidias { get; set; } = new();

    [BindProperty]
    public string? MidiasOrdenadasJson { get; set; }

    [BindProperty]
    public string? CapaItemKey { get; set; }

    [BindProperty]
    public List<int> MidiasRemoverIds { get; set; } = new();

    public List<SelectListItem> Lojas { get; set; } = new();
    public List<SelectListItem> Marcas { get; set; } = new();
    public List<SelectListItem> Vendedores { get; set; } = new();

    public List<VeiculoMidia> MidiasExistentes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        await CarregarCombosAsync();

        if (!id.HasValue || id.Value <= 0)
        {
            Veiculo.Ativo = true;
            return Page();
        }

        var veiculoResult = await _veiculoService.ObterPorIdAsync(id.Value);
        if (veiculoResult.Status != PackageStatus.Success || veiculoResult.Data == null)
        {
            TempData["ErrorMessage"] = veiculoResult.UserMessage ?? "Veículo não encontrado.";
            return RedirectToPage("./Index");
        }

        Veiculo = veiculoResult.Data;

        var caracteristicaResult = await _veiculoCaracteristicaService.ObterPorVeiculoAsync(id.Value);
        if (caracteristicaResult.Status == PackageStatus.Success && caracteristicaResult.Data != null)
        {
            Caracteristica = caracteristicaResult.Data;
        }
        else
        {
            Caracteristica = new VeiculoCaracteristica
            {
                VeiculoId = id.Value
            };
        }

        var midiasResult = await _veiculoMidiaService.ListarPorVeiculoAsync(id.Value);
        if (midiasResult.Status == PackageStatus.Success && midiasResult.Data != null)
        {
            MidiasExistentes = midiasResult.Data
                .OrderByDescending(x => x.Capa)
                .ThenBy(x => x.Ordem)
                .ThenByDescending(x => x.DataCadastro)
                .ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CarregarCombosAsync();

        Veiculo.Titulo = string.IsNullOrWhiteSpace(Veiculo.Modelo)
            ? (Veiculo.Titulo ?? string.Empty).Trim()
            : Veiculo.Modelo.Trim();

        ModelState.Remove($"{nameof(Veiculo)}.{nameof(Veiculo.Titulo)}");
        TryValidateModel(Veiculo, nameof(Veiculo));

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Veiculo.Vendido &&
            Veiculo.VendidoPorUsuarioId == null &&
            Guid.TryParse(usuarioId, out var usuarioGuid))
        {
            Veiculo.VendidoPorUsuarioId = usuarioGuid;
        }

        if (!ModelState.IsValid)
        {
            await RecarregarMidiasExistentesAsync(Veiculo.Id);
            return Page();
        }

        int veiculoId;

        if (Veiculo.Id > 0)
        {
            var editarResult = await _veiculoService.EditarAsync(Veiculo);
            if (editarResult.Status != PackageStatus.Success)
            {
                ModelState.AddModelError(string.Empty, editarResult.UserMessage ?? "Não foi possível salvar o veículo.");
                await RecarregarMidiasExistentesAsync(Veiculo.Id);
                return Page();
            }

            veiculoId = Veiculo.Id;
        }
        else
        {
            var criarResult = await _veiculoService.CriarAsync(Veiculo);
            if (criarResult.Status != PackageStatus.Success)
            {
                ModelState.AddModelError(string.Empty, criarResult.UserMessage ?? "Não foi possível cadastrar o veículo.");
                await RecarregarMidiasExistentesAsync(0);
                return Page();
            }

            veiculoId = criarResult.Data;
            Veiculo.Id = veiculoId;
        }

        Caracteristica.VeiculoId = veiculoId;

        var caracteristicaSave = await _veiculoCaracteristicaService.CriarOuAtualizarAsync(Caracteristica);
        if (caracteristicaSave.Status != PackageStatus.Success)
        {
            ModelState.AddModelError(string.Empty, caracteristicaSave.UserMessage ?? "Não foi possível salvar as características.");
            await RecarregarMidiasExistentesAsync(veiculoId);
            return Page();
        }

        var processMidias = await ProcessarMidiasAsync(veiculoId);
        if (!processMidias.Status)
        {
            ModelState.AddModelError(string.Empty, processMidias.Message);
            await RecarregarMidiasExistentesAsync(veiculoId);
            return Page();
        }

        TempData["SuccessMessage"] = Veiculo.Id > 0
            ? "Veículo salvo com sucesso."
            : "Veículo cadastrado com sucesso.";

        return RedirectToPage("./Index");
    }

    private async Task CarregarCombosAsync()
    {
        Lojas = await _context.Lojas
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            })
            .ToListAsync();

        Marcas = await _context.Marcas
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            })
            .ToListAsync();

        Vendedores = await _context.Vendedores
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            })
            .ToListAsync();
    }

    private async Task RecarregarMidiasExistentesAsync(int veiculoId)
    {
        if (veiculoId <= 0)
        {
            MidiasExistentes = new List<VeiculoMidia>();
            return;
        }

        var result = await _veiculoMidiaService.ListarPorVeiculoAsync(veiculoId);
        MidiasExistentes = result.Status == PackageStatus.Success && result.Data != null
            ? result.Data.OrderByDescending(x => x.Capa).ThenBy(x => x.Ordem).ToList()
            : new List<VeiculoMidia>();
    }

    private async Task<(bool Status, string Message)> ProcessarMidiasAsync(int veiculoId)
    {
        try
        {
            var ordemItens = ParseOrdemItens();

            if (MidiasRemoverIds != null && MidiasRemoverIds.Any())
            {
                foreach (var id in MidiasRemoverIds.Distinct())
                {
                    var midiaBanco = await _veiculoMidiaService.ObterPorIdAsync(id);
                    if (midiaBanco.Status == PackageStatus.Success && midiaBanco.Data != null)
                    {
                        await ExcluirArquivoFisicoSeExistirAsync(midiaBanco.Data.Url);
                        await _veiculoMidiaService.ExcluirAsync(id);
                    }
                }
            }

            var midiasRestantesResult = await _veiculoMidiaService.ListarPorVeiculoAsync(veiculoId);
            var midiasExistentes = midiasRestantesResult.Status == PackageStatus.Success && midiasRestantesResult.Data != null
                ? midiasRestantesResult.Data.ToList()
                : new List<VeiculoMidia>();

            var existingOrderMap = ordemItens
                .Where(x => x.Kind == "existing" && x.Id.HasValue)
                .ToDictionary(x => x.Id!.Value, x => x.Ordem);

            foreach (var midia in midiasExistentes)
            {
                if (existingOrderMap.TryGetValue(midia.Id, out var ordem))
                {
                    midia.Ordem = ordem;
                }

                midia.Capa = string.Equals(CapaItemKey, $"existing-{midia.Id}", StringComparison.OrdinalIgnoreCase);

                await _veiculoMidiaService.EditarAsync(midia);
            }

            if (NovasMidias != null && NovasMidias.Any())
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "veiculos", veiculoId.ToString());
                Directory.CreateDirectory(uploadsPath);

                var newItemsOrdered = ordemItens
                    .Where(x => x.Kind == "new" && x.NewIndex.HasValue)
                    .OrderBy(x => x.Ordem)
                    .ToList();

                if (!newItemsOrdered.Any())
                {
                    for (int i = 0; i < NovasMidias.Count; i++)
                    {
                        newItemsOrdered.Add(new MidiaOrderItem
                        {
                            Kind = "new",
                            NewIndex = i,
                            Ordem = midiasExistentes.Count + i
                        });
                    }
                }

                foreach (var item in newItemsOrdered)
                {
                    if (!item.NewIndex.HasValue || item.NewIndex.Value < 0 || item.NewIndex.Value >= NovasMidias.Count)
                        continue;

                    var file = NovasMidias[item.NewIndex.Value];
                    if (file == null || file.Length <= 0)
                        continue;

                    var extensao = Path.GetExtension(file.FileName);
                    var nomeFisico = $"{Guid.NewGuid():N}{extensao}";
                    var caminhoFisico = Path.Combine(uploadsPath, nomeFisico);

                    using (var stream = new FileStream(caminhoFisico, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var urlRelativa = $"/uploads/veiculos/{veiculoId}/{nomeFisico}";

                    var novaMidia = new VeiculoMidia
                    {
                        VeiculoId = veiculoId,
                        NomeArquivo = file.FileName,
                        Url = urlRelativa,
                        BlobName = nomeFisico,
                        Container = $"uploads/veiculos/{veiculoId}",
                        Tipo = "imagem",
                        ContentType = file.ContentType,
                        TamanhoBytes = file.Length,
                        Capa = string.Equals(CapaItemKey, $"new-{item.NewIndex.Value}", StringComparison.OrdinalIgnoreCase),
                        Ordem = item.Ordem,
                        Ativo = true
                    };

                    var createMidia = await _veiculoMidiaService.CriarAsync(novaMidia);
                    if (createMidia.Status != PackageStatus.Success)
                    {
                        return (false, createMidia.UserMessage ?? "Erro ao salvar uma das mídias.");
                    }
                }
            }

            var reorganizar = await _veiculoMidiaService.ListarPorVeiculoAsync(veiculoId);
            if (reorganizar.Status == PackageStatus.Success && reorganizar.Data != null)
            {
                var listaFinal = reorganizar.Data.OrderBy(x => x.Ordem).ThenBy(x => x.Id).ToList();

                for (int i = 0; i < listaFinal.Count; i++)
                {
                    var item = listaFinal[i];
                    var deveSerCapa = string.Equals(CapaItemKey, $"existing-{item.Id}", StringComparison.OrdinalIgnoreCase);

                    if (!deveSerCapa && !string.IsNullOrWhiteSpace(CapaItemKey) && CapaItemKey.StartsWith("new-"))
                    {
                        // nesse caso a capa já foi aplicada na criação da nova mídia
                        deveSerCapa = item.Capa;
                    }

                    item.Ordem = i;
                    item.Capa = deveSerCapa;

                    await _veiculoMidiaService.EditarAsync(item);
                }

                if (listaFinal.Any() && string.IsNullOrWhiteSpace(CapaItemKey))
                {
                    var primeira = listaFinal.OrderBy(x => x.Ordem).First();
                    primeira.Capa = true;
                    await _veiculoMidiaService.EditarAsync(primeira);
                }
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao processar mídias: {ex.Message}");
        }
    }

    private List<MidiaOrderItem> ParseOrdemItens()
    {
        if (string.IsNullOrWhiteSpace(MidiasOrdenadasJson))
            return new List<MidiaOrderItem>();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<List<MidiaOrderItem>>(MidiasOrdenadasJson, options);
            return data ?? new List<MidiaOrderItem>();
        }
        catch
        {
            return new List<MidiaOrderItem>();
        }
    }

    private async Task ExcluirArquivoFisicoSeExistirAsync(string? urlRelativa)
    {
        if (string.IsNullOrWhiteSpace(urlRelativa))
            return;

        try
        {
            var relative = urlRelativa.TrimStart('/')
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            var path = Path.Combine(_environment.WebRootPath, relative);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
            await Task.CompletedTask;
        }
    }

    public IHtmlContent Check(string aspFor, string label)
    {
        return new HtmlString($@"
            <div class='feature-check'>
                <input type='checkbox' name='{aspFor}' id='{aspFor.Replace(".", "_")}' value='true' {(IsChecked(aspFor) ? "checked" : "")} />
                <input type='hidden' name='{aspFor}' value='false' />
                <label for='{aspFor.Replace(".", "_")}'>{label}</label>
            </div>");
    }

    public bool IsChecked(string aspFor)
    {
        var parts = aspFor.Split('.');
        if (parts.Length != 2) return false;

        var objName = parts[0];
        var propName = parts[1];

        object target = objName == "Caracteristica" ? Caracteristica : Veiculo;
        var prop = target.GetType().GetProperty(propName);
        if (prop == null || prop.PropertyType != typeof(bool)) return false;

        return (bool)(prop.GetValue(target) ?? false);

    }
    public class MidiaOrderItem
    {
        public string? Key { get; set; }
        public string? Kind { get; set; }
        public int? Id { get; set; }
        public int? NewIndex { get; set; }
        public int Ordem { get; set; }
    }
}
