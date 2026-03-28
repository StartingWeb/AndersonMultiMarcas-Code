using Core.Interfaces;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Project.Pages;

public class VeiculoModel : PageModel
{
    private readonly IVeiculoService _veiculoService;

    public VeiculoModel(IVeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    public VehicleDetailViewModel Vehicle { get; private set; } = default!;
    public IReadOnlyList<CatalogoModel.CatalogVehicleItem> RelatedVehicles { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ViewData["ShowHero"] = false;

        if (id <= 0)
        {
            return NotFound();
        }

        var currentResponse = await _veiculoService.ObterPorIdAsync(id);
        if (currentResponse.Data == null || !currentResponse.Data.Ativo || currentResponse.Data.Vendido)
        {
            return NotFound();
        }

        Vehicle = VehicleDetailViewModel.From(currentResponse.Data);
        ViewData["Title"] = Vehicle.Titulo;

        var relatedResponse = await _veiculoService.ListarAtivosAsync();
        if (relatedResponse.Data != null)
        {
            RelatedVehicles = relatedResponse.Data
                .Where(veiculo => veiculo.Id != id && !veiculo.Vendido)
                .OrderByDescending(veiculo => MesmaMarca(veiculo, currentResponse.Data))
                .ThenByDescending(veiculo => Math.Abs((ObterPreco(veiculo) ?? 0) - (ObterPreco(currentResponse.Data) ?? 0)) <= 25000)
                .ThenByDescending(veiculo => veiculo.Destaque)
                .ThenByDescending(veiculo => veiculo.DataCadastro)
                .Take(3)
                .Select(CatalogoModel.CatalogVehicleItem.From)
                .ToList();
        }

        return Page();
    }

    private static bool MesmaMarca(Veiculo candidato, Veiculo atual)
    {
        return string.Equals(candidato.Marca?.Nome, atual.Marca?.Nome, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal? ObterPreco(Veiculo veiculo)
    {
        return veiculo.PrecoPromocional ?? veiculo.PrecoVenda ?? veiculo.PrecoFipe;
    }

    public sealed class VehicleDetailViewModel
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Marca { get; init; } = string.Empty;
        public string Modelo { get; init; } = string.Empty;
        public string Versao { get; init; } = string.Empty;
        public string? Vendedor { get; init; }
        public string? Cor { get; init; }
        public string? Combustivel { get; init; }
        public string? Cambio { get; init; }
        public int? Quilometragem { get; init; }
        public int? AnoFabricacao { get; init; }
        public int? AnoModelo { get; init; }
        public string? Placa { get; init; }
        public decimal? Preco { get; init; }
        public string? Descricao { get; init; }
        public string Highlight { get; init; } = string.Empty;
        public string WhatsappUrl { get; init; } = string.Empty;
        public IReadOnlyList<VehiclePhotoItem> Photos { get; init; } = [];
        public IReadOnlyList<string> Features { get; init; } = [];

        public static VehicleDetailViewModel From(Veiculo veiculo)
        {
            var titulo = string.IsNullOrWhiteSpace(veiculo.Titulo)
                ? string.Join(" ", new[] { veiculo.Marca?.Nome, veiculo.Modelo, veiculo.Versao }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                : veiculo.Titulo;

            return new VehicleDetailViewModel
            {
                Id = veiculo.Id,
                Titulo = string.IsNullOrWhiteSpace(titulo) ? $"Veículo #{veiculo.Id}" : titulo,
                Marca = veiculo.Marca?.Nome ?? "Sem marca",
                Modelo = veiculo.Modelo ?? string.Empty,
                Versao = veiculo.Versao ?? string.Empty,
                Vendedor = veiculo.Vendedor?.Nome,
                Cor = veiculo.Cor,
                Combustivel = veiculo.Combustivel,
                Cambio = veiculo.Cambio,
                Quilometragem = veiculo.Quilometragem,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
                Placa = NormalizarPlaca(veiculo.Placa),
                Preco = ObterPreco(veiculo),
                Descricao = veiculo.Descricao,
                Highlight = string.Join(" • ", new[]
                {
                    veiculo.Cor,
                    veiculo.AnoModelo?.ToString() ?? veiculo.AnoFabricacao?.ToString()
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                WhatsappUrl = $"https://wa.me/551632523490?text={Uri.EscapeDataString($"Olá, quero saber mais sobre o veículo {titulo}.")}",
                Photos = MontarFotos(veiculo),
                Features = MontarFeatures(veiculo.Caracteristica)
            };
        }

        private static IReadOnlyList<VehiclePhotoItem> MontarFotos(Veiculo veiculo)
        {
            var fotos = veiculo.Midias
                .Where(midia => midia.Ativo && !string.IsNullOrWhiteSpace(midia.Url))
                .OrderByDescending(midia => midia.Capa)
                .ThenBy(midia => midia.Ordem)
                .Select((midia, index) => new VehiclePhotoItem(
                    Alt: $"{(string.IsNullOrWhiteSpace(veiculo.Titulo) ? veiculo.Modelo : veiculo.Titulo)} - foto {index + 1}",
                    Url: Uri.TryCreate(midia.Url, UriKind.Absolute, out _) ? midia.Url : (midia.Url.StartsWith('/') ? midia.Url : $"/{midia.Url.TrimStart('/')}")))
                .ToList();

            if (fotos.Any())
            {
                return fotos;
            }

            return new[]
            {
                new VehiclePhotoItem("Imagem padrão do veículo", "/img/carroDefault.png")
            };
        }

        private static string? NormalizarPlaca(string? placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
            {
                return null;
            }

            var placaLimpa = placa.Trim();
            var somenteAlfanumericos = new string(placaLimpa
                .Where(char.IsLetterOrDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(somenteAlfanumericos))
            {
                return null;
            }

            return somenteAlfanumericos.All(caractere => caractere == '0')
                ? null
                : placaLimpa;
        }

        private static IReadOnlyList<string> MontarFeatures(VeiculoCaracteristica? caracteristica)
        {
            if (caracteristica == null)
            {
                return [];
            }

            var mapa = new Dictionary<string, bool>
            {
                ["Ar-condicionado"] = caracteristica.ArCondicionado,
                ["Ar quente"] = caracteristica.ArQuente,
                ["Direção hidráulica"] = caracteristica.DirecaoHidraulica,
                ["Direção elétrica"] = caracteristica.DirecaoEletrica,
                ["Vidro elétrico"] = caracteristica.VidroEletrico,
                ["Trava elétrica"] = caracteristica.TravaEletrica,
                ["Retrovisor elétrico"] = caracteristica.RetrovisorEletrico,
                ["Banco de couro"] = caracteristica.BancoDeCouro,
                ["Volante multifuncional"] = caracteristica.VolanteMultifuncional,
                ["Piloto automático"] = caracteristica.PilotoAutomatico,
                ["Computador de bordo"] = caracteristica.ComputadorBordo,
                ["Chave presencial"] = caracteristica.ChavePresencial,
                ["Partida por botão"] = caracteristica.PartidaBotao,
                ["Airbag motorista"] = caracteristica.AirbagMotorista,
                ["Airbag passageiro"] = caracteristica.AirbagPassageiro,
                ["Airbag lateral"] = caracteristica.AirbagLateral,
                ["Airbag cortina"] = caracteristica.AirbagCortina,
                ["Freios ABS"] = caracteristica.FreiosAbs,
                ["Controle de tração"] = caracteristica.ControleTracao,
                ["Controle de estabilidade"] = caracteristica.ControleEstabilidade,
                ["Assistente de rampa"] = caracteristica.AssistentePartidaRampa,
                ["Isofix"] = caracteristica.Isofix,
                ["Alarme"] = caracteristica.Alarme,
                ["Câmera de ré"] = caracteristica.CameraDeRe,
                ["Sensor dianteiro"] = caracteristica.SensorEstacionamentoDianteiro,
                ["Sensor traseiro"] = caracteristica.SensorEstacionamentoTraseiro,
                ["Farol de neblina"] = caracteristica.FarolNeblina,
                ["Farol LED"] = caracteristica.FarolLed,
                ["Central multimídia"] = caracteristica.CentralMultimidia,
                ["Som"] = caracteristica.Som,
                ["Bluetooth"] = caracteristica.Bluetooth,
                ["USB"] = caracteristica.Usb,
                ["Rádio"] = caracteristica.Radio,
                ["GPS"] = caracteristica.GPS,
                ["Apple CarPlay"] = caracteristica.AppleCarPlay,
                ["Android Auto"] = caracteristica.AndroidAuto,
                ["Roda de liga leve"] = caracteristica.RodaLigaLeve,
                ["Engate"] = caracteristica.Engate,
                ["Capota marítima"] = caracteristica.CapotaMaritima,
                ["Estribo"] = caracteristica.Estribo,
                ["Santo Antônio"] = caracteristica.SantoAntonio,
                ["Protetor de caçamba"] = caracteristica.ProtetorCacamba,
                ["Porta-malas elétrico"] = caracteristica.PortaMalasEletrico,
                ["Terceira fileira"] = caracteristica.TerceiraFileira,
                ["Câmbio automático"] = caracteristica.CambioAutomatico,
                ["Câmbio manual"] = caracteristica.CambioManual,
                ["Câmbio CVT"] = caracteristica.CambioCvt,
                ["Tração dianteira"] = caracteristica.TracaoDianteira,
                ["Tração traseira"] = caracteristica.TracaoTraseira,
                ["Tração integral"] = caracteristica.TracaoIntegral,
                ["Start/Stop"] = caracteristica.StartStop,
                ["Turbo"] = caracteristica.Turbo,
                ["Híbrido"] = caracteristica.Hibrido,
                ["Elétrico"] = caracteristica.Eletrico
            };

            return mapa
                .Where(item => item.Value)
                .Select(item => item.Key)
                .ToList();
        }
    }

    public sealed record VehiclePhotoItem(string Alt, string Url);
}
