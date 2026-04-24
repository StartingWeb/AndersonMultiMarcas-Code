using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Project.Pages;

public class VeiculoModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VeiculoModel> _logger;

    public VeiculoModel(ApplicationDbContext context, ILogger<VeiculoModel> logger)
    {
        _context = context;
        _logger = logger;
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

        var vehicleData = await _context.Veiculos
            .AsNoTracking()
            .Where(veiculo => veiculo.Id == id && veiculo.Ativo && !veiculo.Vendido)
            .Select(veiculo => new VehicleDetailQueryItem
            {
                Id = veiculo.Id,
                Titulo = veiculo.Titulo,
                Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                Modelo = veiculo.Modelo,
                Versao = veiculo.Versao,
                Vendedor = veiculo.Vendedor != null ? veiculo.Vendedor.Nome : null,
                VendedorTelefone = veiculo.Vendedor != null ? veiculo.Vendedor.Telefone : null,
                VendedorWhatsapp = veiculo.Vendedor != null ? veiculo.Vendedor.Whatsapp : null,
                Cor = veiculo.Cor,
                Combustivel = veiculo.Combustivel,
                Cambio = veiculo.Cambio,
                Quilometragem = veiculo.Quilometragem,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
                PrecoVenda = veiculo.PrecoVenda,
                Descricao = veiculo.Descricao,
                ArCondicionado = veiculo.Caracteristica != null && veiculo.Caracteristica.ArCondicionado,
                ArQuente = veiculo.Caracteristica != null && veiculo.Caracteristica.ArQuente,
                DirecaoHidraulica = veiculo.Caracteristica != null && veiculo.Caracteristica.DirecaoHidraulica,
                DirecaoEletrica = veiculo.Caracteristica != null && veiculo.Caracteristica.DirecaoEletrica,
                VidroEletrico = veiculo.Caracteristica != null && veiculo.Caracteristica.VidroEletrico,
                TravaEletrica = veiculo.Caracteristica != null && veiculo.Caracteristica.TravaEletrica,
                RetrovisorEletrico = veiculo.Caracteristica != null && veiculo.Caracteristica.RetrovisorEletrico,
                BancoDeCouro = veiculo.Caracteristica != null && veiculo.Caracteristica.BancoDeCouro,
                VolanteMultifuncional = veiculo.Caracteristica != null && veiculo.Caracteristica.VolanteMultifuncional,
                PilotoAutomatico = veiculo.Caracteristica != null && veiculo.Caracteristica.PilotoAutomatico,
                ComputadorBordo = veiculo.Caracteristica != null && veiculo.Caracteristica.ComputadorBordo,
                ChavePresencial = veiculo.Caracteristica != null && veiculo.Caracteristica.ChavePresencial,
                PartidaBotao = veiculo.Caracteristica != null && veiculo.Caracteristica.PartidaBotao,
                AirbagMotorista = veiculo.Caracteristica != null && veiculo.Caracteristica.AirbagMotorista,
                AirbagPassageiro = veiculo.Caracteristica != null && veiculo.Caracteristica.AirbagPassageiro,
                AirbagLateral = veiculo.Caracteristica != null && veiculo.Caracteristica.AirbagLateral,
                AirbagCortina = veiculo.Caracteristica != null && veiculo.Caracteristica.AirbagCortina,
                FreiosAbs = veiculo.Caracteristica != null && veiculo.Caracteristica.FreiosAbs,
                ControleTracao = veiculo.Caracteristica != null && veiculo.Caracteristica.ControleTracao,
                ControleEstabilidade = veiculo.Caracteristica != null && veiculo.Caracteristica.ControleEstabilidade,
                AssistentePartidaRampa = veiculo.Caracteristica != null && veiculo.Caracteristica.AssistentePartidaRampa,
                Isofix = veiculo.Caracteristica != null && veiculo.Caracteristica.Isofix,
                Alarme = veiculo.Caracteristica != null && veiculo.Caracteristica.Alarme,
                CameraDeRe = veiculo.Caracteristica != null && veiculo.Caracteristica.CameraDeRe,
                SensorEstacionamentoDianteiro = veiculo.Caracteristica != null && veiculo.Caracteristica.SensorEstacionamentoDianteiro,
                SensorEstacionamentoTraseiro = veiculo.Caracteristica != null && veiculo.Caracteristica.SensorEstacionamentoTraseiro,
                FarolNeblina = veiculo.Caracteristica != null && veiculo.Caracteristica.FarolNeblina,
                FarolLed = veiculo.Caracteristica != null && veiculo.Caracteristica.FarolLed,
                CentralMultimidia = veiculo.Caracteristica != null && veiculo.Caracteristica.CentralMultimidia,
                Som = veiculo.Caracteristica != null && veiculo.Caracteristica.Som,
                Bluetooth = veiculo.Caracteristica != null && veiculo.Caracteristica.Bluetooth,
                Usb = veiculo.Caracteristica != null && veiculo.Caracteristica.Usb,
                Radio = veiculo.Caracteristica != null && veiculo.Caracteristica.Radio,
                GPS = veiculo.Caracteristica != null && veiculo.Caracteristica.GPS,
                AppleCarPlay = veiculo.Caracteristica != null && veiculo.Caracteristica.AppleCarPlay,
                AndroidAuto = veiculo.Caracteristica != null && veiculo.Caracteristica.AndroidAuto,
                RodaLigaLeve = veiculo.Caracteristica != null && veiculo.Caracteristica.RodaLigaLeve,
                Engate = veiculo.Caracteristica != null && veiculo.Caracteristica.Engate,
                CapotaMaritima = veiculo.Caracteristica != null && veiculo.Caracteristica.CapotaMaritima,
                Estribo = veiculo.Caracteristica != null && veiculo.Caracteristica.Estribo,
                SantoAntonio = veiculo.Caracteristica != null && veiculo.Caracteristica.SantoAntonio,
                ProtetorCacamba = veiculo.Caracteristica != null && veiculo.Caracteristica.ProtetorCacamba,
                PortaMalasEletrico = veiculo.Caracteristica != null && veiculo.Caracteristica.PortaMalasEletrico,
                TerceiraFileira = veiculo.Caracteristica != null && veiculo.Caracteristica.TerceiraFileira,
                CambioAutomatico = veiculo.Caracteristica != null && veiculo.Caracteristica.CambioAutomatico,
                CambioManual = veiculo.Caracteristica != null && veiculo.Caracteristica.CambioManual,
                CambioCvt = veiculo.Caracteristica != null && veiculo.Caracteristica.CambioCvt,
                TracaoDianteira = veiculo.Caracteristica != null && veiculo.Caracteristica.TracaoDianteira,
                TracaoTraseira = veiculo.Caracteristica != null && veiculo.Caracteristica.TracaoTraseira,
                TracaoIntegral = veiculo.Caracteristica != null && veiculo.Caracteristica.TracaoIntegral,
                StartStop = veiculo.Caracteristica != null && veiculo.Caracteristica.StartStop,
                Turbo = veiculo.Caracteristica != null && veiculo.Caracteristica.Turbo,
                Hibrido = veiculo.Caracteristica != null && veiculo.Caracteristica.Hibrido,
                Eletrico = veiculo.Caracteristica != null && veiculo.Caracteristica.Eletrico
            })
            .FirstOrDefaultAsync();

        if (vehicleData == null)
        {
            return NotFound();
        }

        var photosTask = _context.VeiculoMidias
            .AsNoTracking()
            .Where(midia => midia.VeiculoId == id && midia.Ativo && midia.Url != null && midia.Url != string.Empty)
            .OrderByDescending(midia => midia.Capa)
            .ThenBy(midia => midia.Ordem)
            .Select(midia => midia.Url)
            .ToListAsync();

        var relatedTask = LoadRelatedVehiclesAsync(id, vehicleData);

        await Task.WhenAll(photosTask, relatedTask);

        Vehicle = VehicleDetailViewModel.From(vehicleData, photosTask.Result);
        ViewData["Title"] = Vehicle.Titulo;

        RelatedVehicles = relatedTask.Result
            .Select(MapToCatalogVehicleItem)
            .ToList();

        return Page();
    }

    private static CatalogoModel.CatalogVehicleItem MapToCatalogVehicleItem(RelatedVehicleQueryItem item)
    {
        var titulo = !string.IsNullOrWhiteSpace(item.Marca) || !string.IsNullOrWhiteSpace(item.Modelo)
            ? string.Join(" ", new[] { item.Marca, item.Modelo }.Where(value => !string.IsNullOrWhiteSpace(value)))
            : string.IsNullOrWhiteSpace(item.Titulo)
                ? string.Join(" ", new[] { item.Marca, item.Versao }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                : item.Titulo!;

        if (string.IsNullOrWhiteSpace(titulo))
        {
            titulo = $"Veiculo #{item.Id}";
        }

        var precoPrincipal = ObterPrecoPrincipal(item.PrecoVenda);
        var precoDe = null as decimal?;

        return new CatalogoModel.CatalogVehicleItem
        {
            Id = item.Id,
            Titulo = titulo,
            Marca = item.Marca ?? "Sem marca",
            Modelo = item.Modelo ?? string.Empty,
            Versao = item.Versao ?? string.Empty,
            Cambio = string.IsNullOrWhiteSpace(item.Cambio) ? "-" : item.Cambio,
            Combustivel = string.IsNullOrWhiteSpace(item.Combustivel) ? "-" : NormalizarCombustivel(item.Combustivel),
            Cor = item.Cor ?? string.Empty,
            Ano = item.Ano,
            Seminovo = item.Seminovo,
            Quilometragem = item.Quilometragem,
            Preco = precoPrincipal,
            PrecoDe = precoDe,
            Tag = item.Destaque ? "Destaque" : precoDe.HasValue ? "Promocao" : "Disponivel",
            Highlight = string.Join(" - ", new[]
            {
                item.Cor,
                item.Ano?.ToString()
            }.Where(value => !string.IsNullOrWhiteSpace(value))),
            ImageUrl = NormalizarImagem(item.ImageUrl),
            WhatsappUrl = $"https://wa.me/5516996219214?text={Uri.EscapeDataString($"Ola, quero mais detalhes do {titulo}.")}"
        };
    }

    private static string NormalizarImagem(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return "/img/carroDefault.png";
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return imageUrl;
        }

        return imageUrl.StartsWith('/') ? imageUrl : $"/{imageUrl.TrimStart('/')}";
    }

    private static decimal? ObterPrecoPrincipal(decimal? precoVenda)
    {
        if (precoVenda.HasValue && precoVenda.Value > 0m)
        {
            return precoVenda.Value;
        }

        return null;
    }

    private static string NormalizarCombustivel(string? combustivel)
    {
        if (string.IsNullOrWhiteSpace(combustivel))
        {
            return "-";
        }

        return combustivel.Trim().Equals("Alcool", StringComparison.OrdinalIgnoreCase)
            ? "\u00C1lcool"
            : combustivel.Trim();
    }

    private async Task<IReadOnlyList<RelatedVehicleQueryItem>> LoadRelatedVehiclesAsync(int currentVehicleId, VehicleDetailQueryItem vehicleData)
    {
        try
        {
            var baseItems = await _context.Veiculos
                .AsNoTracking()
                .Where(veiculo => veiculo.Ativo && !veiculo.Vendido && veiculo.Id != currentVehicleId)
                .OrderByDescending(veiculo => veiculo.Destaque)
                .ThenByDescending(veiculo => veiculo.DataCadastro)
                .Take(24)
                .Select(veiculo => new RelatedVehicleQueryItem
                {
                    Id = veiculo.Id,
                    Titulo = veiculo.Titulo,
                    Marca = veiculo.Marca != null ? veiculo.Marca.Nome : null,
                    Modelo = veiculo.Modelo,
                    Versao = veiculo.Versao,
                    Cambio = veiculo.Cambio,
                    Combustivel = veiculo.Combustivel,
                    Cor = veiculo.Cor,
                    Ano = veiculo.AnoModelo ?? veiculo.AnoFabricacao,
                    Seminovo = veiculo.Seminovo,
                    Quilometragem = veiculo.Quilometragem,
                    PrecoVenda = veiculo.PrecoVenda,
                    Destaque = veiculo.Destaque,
                    ImageUrl = veiculo.Midias
                        .Where(item => item.Ativo && item.Url != null && item.Url != string.Empty)
                        .OrderByDescending(item => item.Capa)
                        .ThenBy(item => item.Ordem)
                        .Select(item => item.Url)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var referencePrice = ObterPrecoPrincipal(vehicleData.PrecoVenda) ?? 0m;

            return baseItems
                .OrderByDescending(item => string.Equals(item.Marca, vehicleData.Marca, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item =>
                {
                    var itemPrice = ObterPrecoPrincipal(item.PrecoVenda) ?? 0m;
                    return Math.Abs(itemPrice - referencePrice) <= 25000m;
                })
                .ThenByDescending(item => item.Destaque)
                .Take(3)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar veiculos relacionados para o veiculo {VehicleId}.", currentVehicleId);
            return [];
        }
    }

    internal sealed class VehicleDetailQueryItem
    {
        public int Id { get; init; }
        public string? Titulo { get; init; }
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public string? Versao { get; init; }
        public string? Vendedor { get; init; }
        public string? VendedorTelefone { get; init; }
        public string? VendedorWhatsapp { get; init; }
        public string? Cor { get; init; }
        public string? Combustivel { get; init; }
        public string? Cambio { get; init; }
        public int? Quilometragem { get; init; }
        public int? AnoFabricacao { get; init; }
        public int? AnoModelo { get; init; }
        public decimal? PrecoVenda { get; init; }
        public string? Descricao { get; init; }
        public bool ArCondicionado { get; init; }
        public bool ArQuente { get; init; }
        public bool DirecaoHidraulica { get; init; }
        public bool DirecaoEletrica { get; init; }
        public bool VidroEletrico { get; init; }
        public bool TravaEletrica { get; init; }
        public bool RetrovisorEletrico { get; init; }
        public bool BancoDeCouro { get; init; }
        public bool VolanteMultifuncional { get; init; }
        public bool PilotoAutomatico { get; init; }
        public bool ComputadorBordo { get; init; }
        public bool ChavePresencial { get; init; }
        public bool PartidaBotao { get; init; }
        public bool AirbagMotorista { get; init; }
        public bool AirbagPassageiro { get; init; }
        public bool AirbagLateral { get; init; }
        public bool AirbagCortina { get; init; }
        public bool FreiosAbs { get; init; }
        public bool ControleTracao { get; init; }
        public bool ControleEstabilidade { get; init; }
        public bool AssistentePartidaRampa { get; init; }
        public bool Isofix { get; init; }
        public bool Alarme { get; init; }
        public bool CameraDeRe { get; init; }
        public bool SensorEstacionamentoDianteiro { get; init; }
        public bool SensorEstacionamentoTraseiro { get; init; }
        public bool FarolNeblina { get; init; }
        public bool FarolLed { get; init; }
        public bool CentralMultimidia { get; init; }
        public bool Som { get; init; }
        public bool Bluetooth { get; init; }
        public bool Usb { get; init; }
        public bool Radio { get; init; }
        public bool GPS { get; init; }
        public bool AppleCarPlay { get; init; }
        public bool AndroidAuto { get; init; }
        public bool RodaLigaLeve { get; init; }
        public bool Engate { get; init; }
        public bool CapotaMaritima { get; init; }
        public bool Estribo { get; init; }
        public bool SantoAntonio { get; init; }
        public bool ProtetorCacamba { get; init; }
        public bool PortaMalasEletrico { get; init; }
        public bool TerceiraFileira { get; init; }
        public bool CambioAutomatico { get; init; }
        public bool CambioManual { get; init; }
        public bool CambioCvt { get; init; }
        public bool TracaoDianteira { get; init; }
        public bool TracaoTraseira { get; init; }
        public bool TracaoIntegral { get; init; }
        public bool StartStop { get; init; }
        public bool Turbo { get; init; }
        public bool Hibrido { get; init; }
        public bool Eletrico { get; init; }
    }

    private sealed class RelatedVehicleQueryItem
    {
        public int Id { get; init; }
        public string? Titulo { get; init; }
        public string? Marca { get; init; }
        public string? Modelo { get; init; }
        public string? Versao { get; init; }
        public string? Cambio { get; init; }
        public string? Combustivel { get; init; }
        public string? Cor { get; init; }
        public int? Ano { get; init; }
        public bool Seminovo { get; init; }
        public int? Quilometragem { get; init; }
        public decimal? PrecoVenda { get; init; }
        public bool Destaque { get; init; }
        public string? ImageUrl { get; init; }
    }

    public sealed class VehicleDetailViewModel
    {
        public int Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Marca { get; init; } = string.Empty;
        public string Modelo { get; init; } = string.Empty;
        public string Versao { get; init; } = string.Empty;
        public string? Vendedor { get; init; }
        public string? VendedorTelefone { get; init; }
        public string? VendedorWhatsapp { get; init; }
        public string? Cor { get; init; }
        public string? Combustivel { get; init; }
        public string? Cambio { get; init; }
        public int? Quilometragem { get; init; }
        public int? AnoFabricacao { get; init; }
        public int? AnoModelo { get; init; }
        public decimal? Preco { get; init; }
        public string? Descricao { get; init; }
        public string Highlight { get; init; } = string.Empty;
        public string WhatsappUrl { get; init; } = string.Empty;
        public IReadOnlyList<VehiclePhotoItem> Photos { get; init; } = [];
        public IReadOnlyList<string> Features { get; init; } = [];

        internal static VehicleDetailViewModel From(VehicleDetailQueryItem veiculo, IReadOnlyList<string> photos)
        {
            var titulo = !string.IsNullOrWhiteSpace(veiculo.Marca) || !string.IsNullOrWhiteSpace(veiculo.Modelo)
                ? string.Join(" ", new[] { veiculo.Marca, veiculo.Modelo }.Where(value => !string.IsNullOrWhiteSpace(value)))
                : string.IsNullOrWhiteSpace(veiculo.Titulo)
                    ? string.Join(" ", new[] { veiculo.Marca, veiculo.Versao }
                        .Where(value => !string.IsNullOrWhiteSpace(value)))
                    : veiculo.Titulo!;

            if (string.IsNullOrWhiteSpace(titulo))
            {
                titulo = $"Ve\u00EDculo #{veiculo.Id}";
            }

            var contatoNumero = ObterNumeroContato(veiculo.VendedorWhatsapp, veiculo.VendedorTelefone);
            var whatsappUrl = string.IsNullOrWhiteSpace(contatoNumero)
                ? $"https://wa.me/5516996219214?text={Uri.EscapeDataString($"Ol\u00E1, quero saber mais sobre o ve\u00EDculo {titulo}.")}"
                : $"https://wa.me/{contatoNumero}?text={Uri.EscapeDataString($"Ol\u00E1, quero saber mais sobre o ve\u00EDculo {titulo}.")}";

            return new VehicleDetailViewModel
            {
                Id = veiculo.Id,
                Titulo = titulo,
                Marca = veiculo.Marca ?? "Sem marca",
                Modelo = veiculo.Modelo ?? string.Empty,
                Versao = veiculo.Versao ?? string.Empty,
                Vendedor = veiculo.Vendedor,
                VendedorTelefone = veiculo.VendedorTelefone,
                VendedorWhatsapp = veiculo.VendedorWhatsapp,
                Cor = veiculo.Cor,
                Combustivel = NormalizarCombustivel(veiculo.Combustivel),
                Cambio = veiculo.Cambio,
                Quilometragem = veiculo.Quilometragem,
                AnoFabricacao = veiculo.AnoFabricacao,
                AnoModelo = veiculo.AnoModelo,
                Preco = ObterPrecoPrincipal(veiculo.PrecoVenda),
                Descricao = veiculo.Descricao,
                Highlight = string.Join(" - ", new[]
                {
                    veiculo.Cor,
                    (veiculo.AnoModelo ?? veiculo.AnoFabricacao)?.ToString()
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                WhatsappUrl = whatsappUrl,
                Photos = MontarFotos(titulo, photos),
                Features = MontarFeatures(veiculo)
            };
        }

        private static string? ObterNumeroContato(string? whatsapp, string? telefone)
        {
            var baseNumero = string.IsNullOrWhiteSpace(whatsapp) ? telefone : whatsapp;
            if (string.IsNullOrWhiteSpace(baseNumero))
            {
                return null;
            }

            var digits = new string(baseNumero.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            if (digits.StartsWith("55", StringComparison.Ordinal))
            {
                return digits;
            }

            return $"55{digits}";
        }
        private static IReadOnlyList<VehiclePhotoItem> MontarFotos(string titulo, IReadOnlyList<string> photos)
        {
            var fotos = photos
                .Select((url, index) => new VehiclePhotoItem(
                    Alt: $"{titulo} - foto {index + 1}",
                    Url: NormalizarImagem(url)))
                .ToList();

            if (fotos.Any())
            {
                return fotos;
            }

            return new[]
            {
                new VehiclePhotoItem("Imagem padr\u00E3o do ve\u00EDculo", "/img/carroDefault.png")
            };
        }

        private static IReadOnlyList<string> MontarFeatures(VehicleDetailQueryItem caracteristica)
        {
            var mapa = new Dictionary<string, bool>
            {
                ["Ar-condicionado"] = caracteristica.ArCondicionado,
                ["Ar quente"] = caracteristica.ArQuente,
                ["Dire\u00E7\u00E3o hidr\u00E1ulica"] = caracteristica.DirecaoHidraulica,
                ["Dire\u00E7\u00E3o el\u00E9trica"] = caracteristica.DirecaoEletrica,
                ["Vidro el\u00E9trico"] = caracteristica.VidroEletrico,
                ["Trava el\u00E9trica"] = caracteristica.TravaEletrica,
                ["Retrovisor el\u00E9trico"] = caracteristica.RetrovisorEletrico,
                ["Banco de couro"] = caracteristica.BancoDeCouro,
                ["Volante multifuncional"] = caracteristica.VolanteMultifuncional,
                ["Piloto autom\u00E1tico"] = caracteristica.PilotoAutomatico,
                ["Computador de bordo"] = caracteristica.ComputadorBordo,
                ["Chave presencial"] = caracteristica.ChavePresencial,
                ["Partida por bot\u00E3o"] = caracteristica.PartidaBotao,
                ["Airbag motorista"] = caracteristica.AirbagMotorista,
                ["Airbag passageiro"] = caracteristica.AirbagPassageiro,
                ["Airbag lateral"] = caracteristica.AirbagLateral,
                ["Airbag cortina"] = caracteristica.AirbagCortina,
                ["Freios ABS"] = caracteristica.FreiosAbs,
                ["Controle de tra\u00E7\u00E3o"] = caracteristica.ControleTracao,
                ["Controle de estabilidade"] = caracteristica.ControleEstabilidade,
                ["Assistente de rampa"] = caracteristica.AssistentePartidaRampa,
                ["Isofix"] = caracteristica.Isofix,
                ["Alarme"] = caracteristica.Alarme,
                ["C\u00E2mera de r\u00E9"] = caracteristica.CameraDeRe,
                ["Sensor dianteiro"] = caracteristica.SensorEstacionamentoDianteiro,
                ["Sensor traseiro"] = caracteristica.SensorEstacionamentoTraseiro,
                ["Farol de neblina"] = caracteristica.FarolNeblina,
                ["Farol LED"] = caracteristica.FarolLed,
                ["Central multim\u00EDdia"] = caracteristica.CentralMultimidia,
                ["Som"] = caracteristica.Som,
                ["Bluetooth"] = caracteristica.Bluetooth,
                ["USB"] = caracteristica.Usb,
                ["R\u00E1dio"] = caracteristica.Radio,
                ["GPS"] = caracteristica.GPS,
                ["Apple CarPlay"] = caracteristica.AppleCarPlay,
                ["Android Auto"] = caracteristica.AndroidAuto,
                ["Roda de liga leve"] = caracteristica.RodaLigaLeve,
                ["Engate"] = caracteristica.Engate,
                ["Capota mar\u00EDtima"] = caracteristica.CapotaMaritima,
                ["Estribo"] = caracteristica.Estribo,
                ["Santo Ant\u00F4nio"] = caracteristica.SantoAntonio,
                ["Protetor de ca\u00E7amba"] = caracteristica.ProtetorCacamba,
                ["Porta-malas el\u00E9trico"] = caracteristica.PortaMalasEletrico,
                ["Terceira fileira"] = caracteristica.TerceiraFileira,
                ["C\u00E2mbio autom\u00E1tico"] = caracteristica.CambioAutomatico,
                ["C\u00E2mbio manual"] = caracteristica.CambioManual,
                ["C\u00E2mbio CVT"] = caracteristica.CambioCvt,
                ["Tra\u00E7\u00E3o dianteira"] = caracteristica.TracaoDianteira,
                ["Tra\u00E7\u00E3o traseira"] = caracteristica.TracaoTraseira,
                ["Tra\u00E7\u00E3o integral"] = caracteristica.TracaoIntegral,
                ["Start/Stop"] = caracteristica.StartStop,
                ["Turbo"] = caracteristica.Turbo,
                ["H\u00EDbrido"] = caracteristica.Hibrido,
                ["El\u00E9trico"] = caracteristica.Eletrico
            };

            return mapa
                .Where(item => item.Value)
                .Select(item => item.Key)
                .ToList();
        }
    }

    public sealed record VehiclePhotoItem(string Alt, string Url);
}

