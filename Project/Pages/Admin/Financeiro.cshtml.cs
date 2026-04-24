using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages.Admin;

[Authorize(Roles = "Desenvolvedor,Administrador")]
public class FinanceiroModel : PageModel
{
    private readonly IVeiculoService _veiculoService;

    public FinanceiroModel(IVeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Ano { get; set; }

    public IReadOnlyList<MesOptionItem> Meses { get; private set; } = [];
    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = [];
    public IReadOnlyList<VeiculoVendidoItem> VeiculosVendidos { get; private set; } = [];
    public FinanceiroResumo Resumo { get; private set; } = FinanceiroResumo.Empty;

    public async Task OnGetAsync()
    {
        var hoje = DateTime.Today;
        var mesSelecionado = Mes is >= 1 and <= 12 ? Mes.Value : hoje.Month;
        var anoSelecionado = Ano is >= 2000 and <= 3000 ? Ano.Value : hoje.Year;

        Mes = mesSelecionado;
        Ano = anoSelecionado;

        Meses = BuildMeses(mesSelecionado);

        var veiculosResult = await _veiculoService.ListarAsync();
        var veiculos = veiculosResult.Data ?? [];

        var vendidos = veiculos
            .Where(veiculo => veiculo.Vendido && veiculo.DataVenda.HasValue)
            .ToList();

        AnosDisponiveis = BuildAnosDisponiveis(vendidos, anoSelecionado);

        var filtrados = vendidos
            .Where(veiculo => veiculo.DataVenda!.Value.Month == mesSelecionado)
            .Where(veiculo => veiculo.DataVenda!.Value.Year == anoSelecionado)
            .OrderByDescending(veiculo => veiculo.DataVenda)
            .ThenByDescending(veiculo => veiculo.Id)
            .ToList();

        VeiculosVendidos = filtrados
            .Select(VeiculoVendidoItem.From)
            .ToList();

        var faturamento = filtrados.Sum(ObterPrecoPrincipal);
        Resumo = new FinanceiroResumo
        {
            TotalVeiculosVendidos = filtrados.Count,
            FaturamentoBruto = faturamento,
            TicketMedio = filtrados.Count > 0 ? faturamento / filtrados.Count : 0m,
            Periodo = new DateTime(anoSelecionado, mesSelecionado, 1)
        };
    }

    private static IReadOnlyList<MesOptionItem> BuildMeses(int mesSelecionado)
    {
        return Enumerable.Range(1, 12)
            .Select(mes => new MesOptionItem
            {
                Valor = mes,
                Nome = new DateTime(2000, mes, 1).ToString("MMMM"),
                Selecionado = mes == mesSelecionado
            })
            .ToList();
    }

    private static IReadOnlyList<int> BuildAnosDisponiveis(IEnumerable<Veiculo> vendidos, int anoSelecionado)
    {
        var anos = vendidos
            .Where(veiculo => veiculo.DataVenda.HasValue)
            .Select(veiculo => veiculo.DataVenda!.Value.Year)
            .Append(DateTime.Today.Year)
            .Append(anoSelecionado)
            .Distinct()
            .OrderByDescending(ano => ano)
            .ToList();

        if (anos.Count == 0)
        {
            anos.Add(DateTime.Today.Year);
        }

        return anos;
    }

    private static decimal ObterPrecoPrincipal(Veiculo veiculo)
    {
        return veiculo.PrecoVenda ?? 0m;
    }

    public sealed class MesOptionItem
    {
        public int Valor { get; init; }
        public string Nome { get; init; } = string.Empty;
        public bool Selecionado { get; init; }
    }

    public sealed class FinanceiroResumo
    {
        public static FinanceiroResumo Empty { get; } = new()
        {
            Periodo = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        public int TotalVeiculosVendidos { get; init; }
        public decimal FaturamentoBruto { get; init; }
        public decimal TicketMedio { get; init; }
        public DateTime Periodo { get; init; }
    }

    public sealed class VeiculoVendidoItem
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Loja { get; init; } = "-";
        public string Marca { get; init; } = "-";
        public string Vendedor { get; init; } = "-";
        public string Placa { get; init; } = "-";
        public DateTime DataVenda { get; init; }
        public decimal ValorVenda { get; init; }

        public static VeiculoVendidoItem From(Veiculo veiculo)
        {
            var titulo = !string.IsNullOrWhiteSpace(veiculo.Marca?.Nome) || !string.IsNullOrWhiteSpace(veiculo.Modelo)
                ? string.Join(" ", new[] { veiculo.Marca?.Nome, veiculo.Modelo }.Where(parte => !string.IsNullOrWhiteSpace(parte)))
                : string.IsNullOrWhiteSpace(veiculo.Titulo)
                    ? $"Veículo #{veiculo.Id}"
                    : veiculo.Titulo;

            return new VeiculoVendidoItem
            {
                Id = veiculo.Id,
                Titulo = titulo,
                Loja = veiculo.Loja?.Nome ?? "-",
                Marca = veiculo.Marca?.Nome ?? "-",
                Vendedor = veiculo.Vendedor?.Nome ?? "-",
                Placa = string.IsNullOrWhiteSpace(veiculo.Placa) ? "-" : veiculo.Placa,
                DataVenda = veiculo.DataVenda ?? veiculo.DataCadastro,
                ValorVenda = ObterPrecoPrincipal(veiculo)
            };
        }
    }
}
