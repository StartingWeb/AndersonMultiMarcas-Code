using Core.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IVeiculoService _veiculoService;
    private readonly ILojaService _lojaService;
    private readonly ApplicationDbContext _context;

    public IndexModel(
        ILogger<IndexModel> logger,
        IVeiculoService veiculoService,
        ILojaService lojaService,
        ApplicationDbContext context)
    {
        _logger = logger;
        _veiculoService = veiculoService;
        _lojaService = lojaService;
        _context = context;
    }

    public IReadOnlyList<CatalogoModel.CatalogVehicleItem> FeaturedVehicles { get; private set; } = [];
    public IReadOnlyList<HomeStoreItem> Stores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var veiculosTask = _veiculoService.ListarAtivosAsync();
        var lojasTask = _lojaService.ListarAsync();

        await Task.WhenAll(veiculosTask, lojasTask);

        if (veiculosTask.Result.Data != null)
        {
            FeaturedVehicles = veiculosTask.Result.Data
                .Where(veiculo => !veiculo.Vendido)
                .OrderByDescending(veiculo => veiculo.Destaque)
                .ThenByDescending(veiculo => veiculo.DataCadastro)
                .Take(4)
                .Select(CatalogoModel.CatalogVehicleItem.From)
                .ToList();
        }

        if (lojasTask.Result.Data != null)
        {
            var lojasAtivas = lojasTask.Result.Data
                .Where(loja => loja.Ativo)
                .ToList();

            var lojasParaExibir = lojasAtivas.Any()
                ? lojasAtivas
                : lojasTask.Result.Data;

            Stores = lojasParaExibir
                .OrderBy(loja => loja.Nome)
                .Select(HomeStoreItem.From)
                .ToList();
        }

        if (!Stores.Any())
        {
            var lojasFallback = await _context.Lojas
                .AsNoTracking()
                .Where(loja => loja.Ativo)
                .OrderBy(loja => loja.Nome)
                .ToListAsync();

            Stores = lojasFallback
                .Select(loja =>
                {
                    var enderecoBusca = string.Join(", ", new[]
                    {
                        loja.Endereco,
                        loja.Numero,
                        loja.Bairro,
                        loja.Cidade,
                        loja.Uf,
                        loja.Cep,
                        loja.Nome
                    }.Where(value => !string.IsNullOrWhiteSpace(value)));

                    return new HomeStoreItem
                    {
                        Nome = loja.Nome,
                        EnderecoCompleto = string.Join(", ", new[]
                        {
                            string.Join(", ", new[] { loja.Endereco, loja.Numero }.Where(value => !string.IsNullOrWhiteSpace(value))),
                            loja.Bairro,
                            string.Join(" - ", new[] { loja.Cidade, loja.Uf }.Where(value => !string.IsNullOrWhiteSpace(value))),
                            loja.Cep
                        }.Where(value => !string.IsNullOrWhiteSpace(value))),
                        MapsEmbedUrl = $"https://www.google.com/maps?q={Uri.EscapeDataString(enderecoBusca)}&output=embed",
                        MapsLinkUrl = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(enderecoBusca)}"
                    };
                })
                .ToList();
        }
    }

    public async Task<IActionResult> OnGetSearchSuggestionsAsync(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return new JsonResult(Array.Empty<SearchSuggestionItem>());
        }

        var termo = term.Trim();
        var termoComparacao = termo.ToLowerInvariant();
        var response = await _veiculoService.ListarAtivosAsync();

        if (response.Data == null)
        {
            return new JsonResult(Array.Empty<SearchSuggestionItem>());
        }

        var veiculos = response.Data
            .Where(veiculo => !veiculo.Vendido)
            .ToList();

        var sugestoes = new List<SearchSuggestionItem>();

        var nomes = veiculos
            .Select(veiculo => new
            {
                Veiculo = veiculo,
                Nome = MontarNomePesquisa(veiculo)
            })
            .Where(item =>
                item.Nome.Contains(termoComparacao, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(item.Veiculo.Modelo) && item.Veiculo.Modelo.Contains(termoComparacao, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(item.Veiculo.Versao) && item.Veiculo.Versao.Contains(termoComparacao, StringComparison.OrdinalIgnoreCase)))
            .GroupBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .Select(item => new SearchSuggestionItem
            {
                Group = "Nome",
                Label = item.Nome,
                Meta = string.Join(" • ", new[]
                {
                    item.Veiculo.Marca?.Nome,
                    item.Veiculo.AnoModelo?.ToString() ?? item.Veiculo.AnoFabricacao?.ToString()
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                Query = item.Nome,
                Url = $"/Catalogo?busca={Uri.EscapeDataString(item.Nome)}"
            });

        sugestoes.AddRange(nomes);

        var marcas = veiculos
            .Select(veiculo => veiculo.Marca?.Nome)
            .Where(marca => !string.IsNullOrWhiteSpace(marca) &&
                            marca.Contains(termoComparacao, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(marca => new SearchSuggestionItem
            {
                Group = "Marca",
                Label = marca!,
                Meta = "Filtrar veículos por marca",
                Query = marca!,
                Url = $"/Catalogo?marca={Uri.EscapeDataString(marca!)}"
            });

        sugestoes.AddRange(marcas);

        var categorias = veiculos
            .SelectMany(veiculo => new[]
            {
                CriarSugestaoCategoria("Combustível", veiculo.Combustivel, termoComparacao, "combustivel"),
                CriarSugestaoCategoria("Câmbio", veiculo.Cambio, termoComparacao, "cambio")
            })
            .Where(item => item != null)
            .Cast<SearchSuggestionItem>()
            .GroupBy(item => $"{item.Meta}|{item.Label}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6);

        sugestoes.AddRange(categorias);

        var ordenadas = sugestoes
            .OrderBy(item => item.Group == "Categoria" ? 0 : item.Group == "Marca" ? 1 : 2)
            .ThenBy(item => item.Label)
            .Take(12)
            .ToList();

        return new JsonResult(ordenadas);
    }

    private static SearchSuggestionItem? CriarSugestaoCategoria(
        string meta,
        string? valor,
        string termoComparacao,
        string parametro)
    {
        if (string.IsNullOrWhiteSpace(valor) ||
            !valor.Contains(termoComparacao, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SearchSuggestionItem
        {
            Group = "Categoria",
            Label = valor,
            Meta = meta,
            Query = valor,
            Url = $"/Catalogo?{parametro}={Uri.EscapeDataString(valor)}"
        };
    }

    private static string MontarNomePesquisa(Veiculo veiculo)
    {
        if (!string.IsNullOrWhiteSpace(veiculo.Titulo))
        {
            return veiculo.Titulo.Trim();
        }

        var nome = string.Join(" ", new[]
        {
            veiculo.Marca?.Nome,
            veiculo.Modelo,
            veiculo.Versao
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(nome) ? $"Veículo {veiculo.Id}" : nome;
    }

    public sealed class HomeVehicleItem
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string MarcaLinha { get; init; } = string.Empty;
        public string Resumo { get; init; } = string.Empty;
        public string Combustivel { get; init; } = "-";
        public string Cambio { get; init; } = "-";
        public string Quilometragem { get; init; } = "Não informado";
        public decimal? Preco { get; init; }
        public string Tag { get; init; } = "Disponível";
        public string ImagemUrl { get; init; } = string.Empty;
        public string WhatsappUrl { get; init; } = string.Empty;

        public static HomeVehicleItem From(Veiculo veiculo)
        {
            var titulo = string.IsNullOrWhiteSpace(veiculo.Titulo)
                ? string.Join(" ", new[] { veiculo.Marca?.Nome, veiculo.Modelo, veiculo.Versao }
                    .Where(item => !string.IsNullOrWhiteSpace(item)))
                : veiculo.Titulo;

            var marcaLinha = string.Join(" • ", new[]
            {
                veiculo.Marca?.Nome,
                veiculo.AnoModelo?.ToString() ?? veiculo.AnoFabricacao?.ToString()
            }.Where(item => !string.IsNullOrWhiteSpace(item)));

            var resumo = string.Join(" • ", new[]
            {
                veiculo.Modelo,
                veiculo.Versao
            }.Where(item => !string.IsNullOrWhiteSpace(item)));

            return new HomeVehicleItem
            {
                Id = veiculo.Id,
                Titulo = string.IsNullOrWhiteSpace(titulo) ? $"Veículo #{veiculo.Id}" : titulo,
                MarcaLinha = marcaLinha,
                Resumo = resumo,
                Combustivel = string.IsNullOrWhiteSpace(veiculo.Combustivel) ? "-" : veiculo.Combustivel!,
                Cambio = string.IsNullOrWhiteSpace(veiculo.Cambio) ? "-" : veiculo.Cambio!,
                Quilometragem = veiculo.Quilometragem.HasValue ? $"{veiculo.Quilometragem.Value:N0} km" : "Não informado",
                Preco = veiculo.PrecoPromocional ?? veiculo.PrecoVenda ?? veiculo.PrecoFipe,
                Tag = veiculo.Destaque ? "Destaque" : "Disponível",
                ImagemUrl = ObterImagem(veiculo),
                WhatsappUrl = MontarWhatsappUrl(titulo)
            };
        }

        private static string ObterImagem(Veiculo veiculo)
        {
            var midia = veiculo.Midias
                .Where(item => item.Ativo && !string.IsNullOrWhiteSpace(item.Url))
                .OrderByDescending(item => item.Capa)
                .ThenBy(item => item.Ordem)
                .FirstOrDefault();

            if (midia == null)
            {
                return string.Empty;
            }

            if (Uri.TryCreate(midia.Url, UriKind.Absolute, out _))
            {
                return midia.Url;
            }

            return midia.Url.StartsWith('/') ? midia.Url : $"/{midia.Url.TrimStart('/')}";
        }

        private static string MontarWhatsappUrl(string titulo)
        {
            var texto = $"Olá, quero saber mais sobre o veículo {titulo}.";
            return $"https://wa.me/551632523490?text={Uri.EscapeDataString(texto)}";
        }
    }

    public sealed class HomeStoreItem
    {
        public string Nome { get; init; } = string.Empty;
        public string EnderecoCompleto { get; init; } = string.Empty;
        public string MapsEmbedUrl { get; init; } = string.Empty;
        public string MapsLinkUrl { get; init; } = string.Empty;

        public static HomeStoreItem From(Core.Dtos.LojaDto loja)
        {
            var endereco = string.Join(", ", new[]
            {
                MontarLogradouro(loja),
                loja.Bairro,
                MontarCidadeUf(loja),
                loja.Cep
            }.Where(item => !string.IsNullOrWhiteSpace(item)));

            var query = Uri.EscapeDataString(string.IsNullOrWhiteSpace(endereco) ? loja.Nome : endereco);

            return new HomeStoreItem
            {
                Nome = loja.Nome,
                EnderecoCompleto = string.IsNullOrWhiteSpace(endereco) ? "Endereço não informado." : endereco,
                MapsEmbedUrl = $"https://www.google.com/maps?q={query}&output=embed",
                MapsLinkUrl = $"https://www.google.com/maps/search/?api=1&query={query}"
            };
        }

        private static string? MontarLogradouro(Core.Dtos.LojaDto loja)
        {
            return string.Join(", ", new[] { loja.Endereco, loja.Numero }
                .Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        private static string? MontarCidadeUf(Core.Dtos.LojaDto loja)
        {
            return string.Join(" - ", new[] { loja.Cidade, loja.Uf }
                .Where(item => !string.IsNullOrWhiteSpace(item)));
        }
    }

    public sealed class SearchSuggestionItem
    {
        public string Group { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Meta { get; init; } = string.Empty;
        public string Query { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
    }
}
