using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Project.Pages.Admin;

[Authorize(Roles = "Desenvolvedor,Administrador,AdminConcessionaria")]
public class VeiculosVendaModel : PageModel
{
    private readonly IVeiculoService _veiculoService;
    private readonly IVendedorService _vendedorService;

    public VeiculosVendaModel(
        IVeiculoService veiculoService,
        IVendedorService vendedorService)
    {
        _veiculoService = veiculoService;
        _vendedorService = vendedorService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EditId { get; set; }

    [BindProperty]
    public SaleInputModel Input { get; set; } = new();

    public IReadOnlyList<VeiculoListItem> Veiculos { get; private set; } = [];
    public IReadOnlyList<VendedorOptionItem> Vendedores { get; private set; } = [];
    public int FilteredVehicles => Veiculos.Count;
    public bool IsEditing => Input.VeiculoId > 0;
    public bool IsModalOpen => EditId.HasValue || IsEditing || !ModelState.IsValid;

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostSaveSaleAsync()
    {
        if (Input.VeiculoId <= 0)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.VeiculoId)}", "Selecione um veículo para registrar a venda.");
        }

        if (Input.VendedorId <= 0)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.VendedorId)}", "Selecione o vendedor responsável pela venda.");
        }

        var veiculoResponse = Input.VeiculoId > 0
            ? await _veiculoService.ObterPorIdAsync(Input.VeiculoId)
            : null;

        var veiculo = veiculoResponse?.Data;
        if (veiculo == null)
        {
            ModelState.AddModelError(string.Empty, "Veículo não encontrado.");
        }
        if (!ModelState.IsValid || veiculo == null)
        {
            await LoadPageAsync();
            return Page();
        }

        if (veiculo.Vendido)
        {
            TempData["Success"] = "Esse veículo já estava marcado como vendido.";
            return RedirectToPage("/Admin/VeiculosVenda", new { search = Search });
        }

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? usuarioVendaId = Guid.TryParse(usuarioId, out var usuarioGuid)
            ? usuarioGuid
            : null;

        veiculo.Vendido = true;
        veiculo.VendedorId = Input.VendedorId;
        veiculo.DataVenda = DateTime.Now;
        veiculo.VendidoPorUsuarioId = usuarioVendaId;

        var result = await _veiculoService.EditarAsync(veiculo);
        if (!result.Data)
        {
            ModelState.AddModelError(string.Empty, result.UserMessage ?? "Não foi possível registrar a venda.");
            await LoadPageAsync();
            return Page();
        }

        TempData["Success"] = "Venda registrada com sucesso.";
        return RedirectToPage("/Admin/VeiculosVenda", new { search = Search });
    }

    private async Task LoadPageAsync()
    {
        var veiculosResult = await _veiculoService.ListarAsync();
        var veiculos = veiculosResult.Data ?? [];

        var query = veiculos
            .OrderBy(veiculo => veiculo.Vendido)
            .ThenByDescending(veiculo => veiculo.DataCadastro)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchValue = Search.Trim();
            query = query.Where(veiculo =>
                Contains(veiculo.Titulo, searchValue) ||
                Contains(veiculo.Modelo, searchValue) ||
                Contains(veiculo.Versao, searchValue) ||
                Contains(veiculo.Placa, searchValue) ||
                Contains(veiculo.Chassi, searchValue) ||
                Contains(veiculo.Marca?.Nome, searchValue) ||
                Contains(veiculo.Loja?.Nome, searchValue));
        }

        Veiculos = query
            .Select(VeiculoListItem.From)
            .ToList();

        Domain.Veiculo? veiculoSelecionado = null;

        if (EditId.HasValue)
        {
            veiculoSelecionado = veiculos.FirstOrDefault(veiculo => veiculo.Id == EditId.Value);
            if (veiculoSelecionado != null)
            {
                Input = SaleInputModel.From(veiculoSelecionado);
            }
        }

        var lojaIdParaVendedores = veiculoSelecionado?.LojaId;
        if (lojaIdParaVendedores == null && Input.VeiculoId > 0)
        {
            lojaIdParaVendedores = veiculos
                .FirstOrDefault(veiculo => veiculo.Id == Input.VeiculoId)
                ?.LojaId;
        }

        Vendedores = await LoadVendedoresAsync(lojaIdParaVendedores);
    }

    private async Task<IReadOnlyList<VendedorOptionItem>> LoadVendedoresAsync(int? lojaId)
    {
        var vendedoresResult = lojaId.HasValue && lojaId.Value > 0
            ? await _vendedorService.ListarPorLojaAsync(lojaId.Value)
            : await _vendedorService.ListarAsync();

        var vendedores = vendedoresResult.Data ?? [];

        if (lojaId.HasValue && lojaId.Value > 0 && vendedores.Count == 0)
        {
            var fallbackResult = await _vendedorService.ListarAsync();
            vendedores = fallbackResult.Data ?? [];
        }

        var vendedoresDisponiveis = vendedores
            .Where(vendedor => vendedor.Ativo)
            .ToList();

        if (vendedoresDisponiveis.Count == 0)
        {
            vendedoresDisponiveis = vendedores.ToList();
        }

        return vendedoresDisponiveis
            .OrderByDescending(vendedor => vendedor.Ativo)
            .ThenBy(vendedor => vendedor.Nome)
            .Select(vendedor => new VendedorOptionItem
            {
                Id = vendedor.Id,
                Nome = vendedor.Ativo ? vendedor.Nome : $"{vendedor.Nome} (inativo)",
                Loja = vendedor.Loja?.Nome
            })
            .ToList();
    }

    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class SaleInputModel
    {
        public int VeiculoId { get; set; }
        public int VendedorId { get; set; }
        public string VeiculoTitulo { get; set; } = string.Empty;
        public string LojaNome { get; set; } = string.Empty;
        public string Placa { get; set; } = "-";

        public static SaleInputModel From(Veiculo veiculo)
        {
            return new SaleInputModel
            {
                VeiculoId = veiculo.Id,
                VendedorId = 0,
                VeiculoTitulo = string.IsNullOrWhiteSpace(veiculo.Titulo)
                    ? $"Veículo #{veiculo.Id}"
                    : veiculo.Titulo,
                LojaNome = veiculo.Loja?.Nome ?? "-",
                Placa = string.IsNullOrWhiteSpace(veiculo.Placa) ? "-" : veiculo.Placa
            };
        }
    }

    public sealed class VeiculoListItem
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Marca { get; init; } = "-";
        public string Loja { get; init; } = "-";
        public string Placa { get; init; } = "-";
        public string Status { get; init; } = "Disponível";
        public bool Vendido { get; init; }
        public string? Vendedor { get; init; }
        public string? VendidoPorUsuario { get; init; }
        public DateTime? DataVenda { get; init; }
        public DateTime DataCadastro { get; init; }

        public static VeiculoListItem From(Veiculo veiculo)
        {
            return new VeiculoListItem
            {
                Id = veiculo.Id,
                Titulo = string.IsNullOrWhiteSpace(veiculo.Titulo) ? $"Veículo #{veiculo.Id}" : veiculo.Titulo,
                Marca = veiculo.Marca?.Nome ?? "-",
                Loja = veiculo.Loja?.Nome ?? "-",
                Placa = string.IsNullOrWhiteSpace(veiculo.Placa) ? "-" : veiculo.Placa,
                Status = veiculo.Vendido ? "Vendido" : "Disponível",
                Vendido = veiculo.Vendido,
                Vendedor = veiculo.Vendedor?.Nome,
                VendidoPorUsuario = !string.IsNullOrWhiteSpace(veiculo.VendidoPorUsuario?.NomeCompleto)
                    ? veiculo.VendidoPorUsuario.NomeCompleto
                    : veiculo.VendidoPorUsuario?.UserName,
                DataVenda = veiculo.DataVenda,
                DataCadastro = veiculo.DataCadastro
            };
        }
    }

    public sealed class VendedorOptionItem
    {
        public int Id { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string? Loja { get; init; }
    }
}
