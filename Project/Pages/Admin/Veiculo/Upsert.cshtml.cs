using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Data;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Project.Features.Veiculos.Commands;
using Project.Features.Veiculos.DTOs;
using Project.Features.Veiculos.Services;
using Project.Shared;

namespace Project.Pages.Admin.Veiculo;

[Authorize]
public sealed class UpsertModel(
    ApplicationDbContext db,
    ISender sender,
    IVeiculoMediaService mediaService,
    IWebHostEnvironment environment) : PageModel
{
    private static readonly CultureInfo BrCulture = new("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public VehicleInputModel Veiculo { get; set; } = new();

    [BindProperty]
    public CaracteristicaInputModel Caracteristica { get; set; } = new();

    [BindProperty]
    public List<IFormFile> NovasMidias { get; set; } = [];

    [BindProperty]
    public string MidiasOrdenadasJson { get; set; } = "[]";

    [BindProperty]
    public string? CapaItemKey { get; set; }

    [BindProperty]
    public List<int> RemoverMidiaIds { get; set; } = [];

    public IReadOnlyList<OptionItem> Lojas { get; private set; } = [];
    public IReadOnlyList<OptionItem> Marcas { get; private set; } = [];
    public IReadOnlyList<ExistingMediaItem> MidiasExistentes { get; private set; } = [];
    public string PageTitle { get; private set; } = "Novo Veículo";
    public string PageSubtitle { get; private set; } = "Preencha os dados para cadastrar um novo veículo";
    public string SubmitLabel { get; private set; } = "Cadastrar veículo";
    public bool IsEdit => Veiculo.Id > 0;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadSelectsAsync(ct);

        if (!Id.HasValue)
        {
            Veiculo.Ativo = true;
            ViewData["Title"] = PageTitle;
            return Page();
        }

        var veiculo = await db.Veiculos
            .AsNoTracking()
            .Include(x => x.Caracteristicas)
            .Include(x => x.Midias)
            .FirstOrDefaultAsync(x => x.Id == Id.Value, ct);

        if (veiculo is null)
        {
            TempData["ErrorMessage"] = "Veículo não encontrado.";
            return RedirectToPage("/Admin/Veiculo/Index");
        }

        Veiculo = new VehicleInputModel
        {
            Id = veiculo.Id,
            LojaId = veiculo.LojaId,
            MarcaId = veiculo.MarcaId,
            Titulo = veiculo.Titulo,
            Modelo = veiculo.Modelo,
            Versao = veiculo.Versao,
            AnoFabricacao = veiculo.AnoFabricacao,
            AnoModelo = veiculo.AnoModelo,
            Cor = veiculo.Cor,
            Quilometragem = veiculo.Quilometragem?.ToString("N0", BrCulture),
            Combustivel = veiculo.Combustivel,
            Cambio = veiculo.Cambio,
            Placa = veiculo.Placa,
            PrecoVenda = veiculo.PrecoVenda.Valor.ToString("N2", BrCulture),
            Descricao = veiculo.Descricao,
            Ativo = veiculo.Ativo,
            Vendido = veiculo.Vendido,
            Destaque = veiculo.Destaque,
            Seminovo = veiculo.Seminovo,
            MotoEletrica = veiculo.MotoEletrica,
            AceitaTroca = veiculo.AceitaTroca,
            Financiavel = veiculo.Financiavel
        };

        Caracteristica = CaracteristicaInputModel.FromEntity(veiculo.Caracteristicas);
        MidiasExistentes = veiculo.Midias
            .Where(x => x.Ativo && x.Tipo == TipoMidia.Imagem)
            .OrderBy(x => x.Ordem)
            .Select(ToExistingMediaItem)
            .Where(x => x is not null)
            .Cast<ExistingMediaItem>()
            .ToList();

        ApplyEditTitle(BuildNomeCompleto(veiculo.Titulo, veiculo.Modelo, veiculo.Versao));
        ViewData["Title"] = PageTitle;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadSelectsAsync(ct);

        var nomeVeiculo = BuildNomeCompleto(Veiculo.Titulo, Veiculo.Modelo, Veiculo.Versao);
        if (Veiculo.Id > 0)
        {
            ApplyEditTitle(string.IsNullOrWhiteSpace(nomeVeiculo) ? $"#{Veiculo.Id}" : nomeVeiculo);
        }

        NormalizeInput();
        ValidateInput();

        if (!ModelState.IsValid)
        {
            if (Veiculo.Id > 0)
            {
                MidiasExistentes = await LoadExistingMediaAsync(Veiculo.Id, ct);
            }

            return Page();
        }

        var lojaId = Veiculo.LojaId.GetValueOrDefault() > 0 ? Veiculo.LojaId.GetValueOrDefault() : Lojas.FirstOrDefault()?.Id ?? 0;
        var marcaId = Veiculo.MarcaId.GetValueOrDefault() > 0 ? Veiculo.MarcaId.GetValueOrDefault() : Marcas.FirstOrDefault()?.Id ?? 0;
        if (lojaId <= 0) ModelState.AddModelError("Veiculo.LojaId", "Cadastre uma loja antes de salvar o veiculo.");
        if (marcaId <= 0) ModelState.AddModelError("Veiculo.MarcaId", "Cadastre uma marca antes de salvar o veiculo.");

        if (!ModelState.IsValid)
        {
            if (Veiculo.Id > 0)
            {
                MidiasExistentes = await LoadExistingMediaAsync(Veiculo.Id, ct);
            }

            return Page();
        }

        var marcaNome = Marcas.FirstOrDefault(x => x.Id == marcaId)?.Nome ?? Veiculo.Modelo;
        var titulo = string.IsNullOrWhiteSpace(Veiculo.Titulo) ? marcaNome : Veiculo.Titulo.Trim();
        var opcionais = Caracteristica.ToOpcionais();
        var preco = ParseDecimal(Veiculo.PrecoVenda);
        var quilometragem = ParseNullableInt(Veiculo.Quilometragem);

        if (Veiculo.Id <= 0)
        {
            var result = await sender.Send(new CriarVeiculoCommand(new VeiculoCreateDto
            {
                LojaId = lojaId,
                MarcaId = marcaId,
                Titulo = titulo,
                Modelo = Veiculo.Modelo.Trim(),
                Versao = NullIfWhiteSpace(Veiculo.Versao),
                AnoFabricacao = Veiculo.AnoFabricacao,
                AnoModelo = Veiculo.AnoModelo ?? 0,
                Cor = NullIfWhiteSpace(Veiculo.Cor),
                Combustivel = Veiculo.Combustivel ?? Combustivel.NaoInformado,
                Cambio = Veiculo.Cambio ?? Cambio.NaoInformado,
                PrecoVenda = preco,
                Quilometragem = quilometragem,
                Placa = NullIfWhiteSpace(Veiculo.Placa),
                Descricao = NullIfWhiteSpace(Veiculo.Descricao),
                Destaque = Veiculo.Destaque,
                Seminovo = Veiculo.Seminovo,
                Financiavel = Veiculo.Financiavel,
                AceitaTroca = Veiculo.AceitaTroca,
                Opcionais = opcionais
            }), ct);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível cadastrar o veículo.");
                return Page();
            }

            await ApplyStatusAndMediaAsync(result.Value, ct);
            TempData["SuccessMessage"] = "Veículo cadastrado com sucesso.";
            return RedirectToPage("/Admin/Veiculo/Index");
        }

        var updateResult = await sender.Send(new AtualizarVeiculoCommand(new VeiculoUpdateDto
        {
            Id = Veiculo.Id,
            LojaId = lojaId,
            MarcaId = marcaId,
            Titulo = titulo,
            Modelo = Veiculo.Modelo.Trim(),
            Versao = NullIfWhiteSpace(Veiculo.Versao),
            AnoFabricacao = Veiculo.AnoFabricacao,
            AnoModelo = Veiculo.AnoModelo ?? 0,
            Cor = NullIfWhiteSpace(Veiculo.Cor),
            Combustivel = Veiculo.Combustivel ?? Combustivel.NaoInformado,
            Cambio = Veiculo.Cambio ?? Cambio.NaoInformado,
            PrecoVenda = preco,
            Quilometragem = quilometragem,
            Placa = NullIfWhiteSpace(Veiculo.Placa),
            Descricao = NullIfWhiteSpace(Veiculo.Descricao),
            Destaque = Veiculo.Destaque,
            Seminovo = Veiculo.Seminovo,
            Financiavel = Veiculo.Financiavel,
            AceitaTroca = Veiculo.AceitaTroca,
            Opcionais = opcionais
        }), ct);

        if (!updateResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, updateResult.Error ?? "Não foi possível salvar o veículo.");
            MidiasExistentes = await LoadExistingMediaAsync(Veiculo.Id, ct);
            return Page();
        }

        await ApplyStatusAndMediaAsync(Veiculo.Id, ct);
        TempData["SuccessMessage"] = "Veículo salvo com sucesso.";
        return RedirectToPage("/Admin/Veiculo/Index");
    }

    private async Task LoadSelectsAsync(CancellationToken ct)
    {
        Lojas = await db.Lojas
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new OptionItem(x.Id, x.Nome))
            .ToListAsync(ct);

        Marcas = await db.Marcas
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new OptionItem(x.Id, x.Nome))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<ExistingMediaItem>> LoadExistingMediaAsync(int veiculoId, CancellationToken ct)
    {
        var midias = await db.VeiculoMidias
            .AsNoTracking()
            .Where(x => x.VeiculoId == veiculoId && x.Ativo && x.Tipo == TipoMidia.Imagem)
            .OrderBy(x => x.Ordem)
            .ToListAsync(ct);

        return midias
            .Select(ToExistingMediaItem)
            .Where(x => x is not null)
            .Cast<ExistingMediaItem>()
            .ToList();
    }

    private ExistingMediaItem? ToExistingMediaItem(VeiculoMidia midia)
    {
        var imagens = VehicleImageHelper.NormalizeGallery([midia.Url], includeDefault: false, environment.WebRootPath);
        var url = imagens.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return new ExistingMediaItem(midia.Id, url, midia.NomeArquivo, midia.TamanhoBytes, midia.Capa);
    }

    private void ApplyEditTitle(string nome)
    {
        PageTitle = $"Editando veículo {nome}";
        PageSubtitle = "Atualize os dados do veículo selecionado";
        SubmitLabel = "Salvar veículo";
    }

    private void NormalizeInput()
    {
        Veiculo.Modelo = Veiculo.Modelo?.Trim() ?? string.Empty;
        Veiculo.Versao = Veiculo.Versao?.Trim();
        Veiculo.Cor = Veiculo.Cor?.Trim();
        Veiculo.Placa = Veiculo.Placa?.Trim().ToUpperInvariant();
        Veiculo.Descricao = Veiculo.Descricao?.Trim();
    }

    private void ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Veiculo.Modelo)) ModelState.AddModelError("Veiculo.Modelo", "Informe o modelo.");
        if (ParseDecimal(Veiculo.PrecoVenda) < 0) ModelState.AddModelError("Veiculo.PrecoVenda", "Informe um preço válido.");
    }

    private async Task ApplyStatusAndMediaAsync(int veiculoId, CancellationToken ct)
    {
        var veiculo = await db.Veiculos.FirstAsync(x => x.Id == veiculoId, ct);

        db.Entry(veiculo).Property(nameof(Domain.Entities.Veiculo.MotoEletrica)).CurrentValue = Veiculo.MotoEletrica;
        db.Entry(veiculo).Property(nameof(Domain.Entities.Veiculo.Vendido)).CurrentValue = Veiculo.Vendido;
        db.Entry(veiculo).Property(nameof(Domain.Entities.Veiculo.DataVenda)).CurrentValue = Veiculo.Vendido ? DateTime.UtcNow : null;

        if (Veiculo.Vendido || !Veiculo.Ativo)
        {
            veiculo.Desativar();
        }
        else
        {
            veiculo.Ativar();
        }

        await RemoveSelectedMediaAsync(veiculoId, ct);
        await UploadNewMediaAsync(veiculoId, ct);
        await ApplyMediaOrderAsync(veiculoId, ct);

        await db.SaveChangesAsync(ct);
    }

    private async Task RemoveSelectedMediaAsync(int veiculoId, CancellationToken ct)
    {
        if (RemoverMidiaIds.Count == 0) return;

        var midias = await db.VeiculoMidias
            .Where(x => x.VeiculoId == veiculoId && RemoverMidiaIds.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var midia in midias)
        {
            await mediaService.RemoverArquivoAsync(midia.Url, ct);
            db.VeiculoMidias.Remove(midia);
        }
    }

    private async Task UploadNewMediaAsync(int veiculoId, CancellationToken ct)
    {
        if (NovasMidias.Count == 0) return;

        var processed = await mediaService.ProcessarUploadAsync(veiculoId, NovasMidias, ct);
        var nextOrder = await db.VeiculoMidias
            .Where(x => x.VeiculoId == veiculoId)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(ct) ?? -1;

        foreach (var item in processed)
        {
            nextOrder++;
            var midia = new VeiculoMidia(veiculoId, item.NomeArquivo, item.Url, TipoMidia.Imagem, nextOrder);
            midia.UpdateStorage(null, null, "image/webp", item.TamanhoBytes);
            db.VeiculoMidias.Add(midia);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyMediaOrderAsync(int veiculoId, CancellationToken ct)
    {
        var midias = await db.VeiculoMidias
            .Where(x => x.VeiculoId == veiculoId)
            .OrderBy(x => x.Ordem)
            .ToListAsync(ct);

        var orderItems = ParseMediaOrder();
        var newMidias = midias
            .Where(x => !orderItems.Any(item => item.Kind == "existing" && item.Id == x.Id))
            .OrderBy(x => x.Ordem)
            .ToList();

        var final = new List<VeiculoMidia>();
        foreach (var item in orderItems.OrderBy(x => x.Ordem))
        {
            if (item.Kind == "existing" && item.Id.HasValue)
            {
                var existing = midias.FirstOrDefault(x => x.Id == item.Id.Value);
                if (existing is not null)
                {
                    final.Add(existing);
                }

                continue;
            }

            if (item.Kind == "new" && item.NewIndex.HasValue && item.NewIndex.Value >= 0 && item.NewIndex.Value < newMidias.Count)
            {
                final.Add(newMidias[item.NewIndex.Value]);
            }
        }

        foreach (var midia in midias)
        {
            if (!final.Contains(midia))
            {
                final.Add(midia);
            }
        }

        var selectedCover = CapaItemKey;
        var coverMedia = ResolveCoverMedia(final, newMidias, selectedCover) ?? final.FirstOrDefault();

        for (var index = 0; index < final.Count; index++)
        {
            var midia = final[index];
            db.Entry(midia).Property(nameof(VeiculoMidia.Ordem)).CurrentValue = index;
            db.Entry(midia).Property(nameof(VeiculoMidia.Capa)).CurrentValue = coverMedia is not null && midia.Id == coverMedia.Id;
        }
    }

    private static VeiculoMidia? ResolveCoverMedia(IReadOnlyList<VeiculoMidia> all, IReadOnlyList<VeiculoMidia> newMidias, string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (key.StartsWith("existing-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(key["existing-".Length..], out var existingId))
        {
            return all.FirstOrDefault(x => x.Id == existingId);
        }

        if (key.StartsWith("new-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(key["new-".Length..], out var newIndex) &&
            newIndex >= 0 &&
            newIndex < newMidias.Count)
        {
            return newMidias[newIndex];
        }

        return null;
    }

    private IReadOnlyList<MediaOrderItem> ParseMediaOrder()
    {
        if (string.IsNullOrWhiteSpace(MidiasOrdenadasJson)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<MediaOrderItem>>(MidiasOrdenadasJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;

        var normalized = value.Trim().Replace(".", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Number, BrCulture, out var result)
            ? result
            : 0m;
    }

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildNomeCompleto(string? titulo, string? modelo, string? versao)
    {
        var tituloLimpo = (titulo ?? string.Empty).Trim();
        var modeloLimpo = (modelo ?? string.Empty).Trim();
        var versaoLimpa = (versao ?? string.Empty).Trim();

        var incluirModelo = !string.IsNullOrWhiteSpace(modeloLimpo)
            && !string.Equals(tituloLimpo, modeloLimpo, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(versaoLimpa))
        {
            return incluirModelo ? $"{tituloLimpo} {modeloLimpo}" : tituloLimpo;
        }

        return incluirModelo
            ? $"{tituloLimpo} {modeloLimpo} {versaoLimpa}"
            : $"{tituloLimpo} {versaoLimpa}";
    }

    public sealed record OptionItem(int Id, string Nome);

    public sealed record ExistingMediaItem(int Id, string Url, string NomeArquivo, long? TamanhoBytes, bool Capa);

    private sealed record MediaOrderItem(string? Key, string? Kind, int? Id, int? NewIndex, int Ordem);

    public sealed class VehicleInputModel
    {
        public int Id { get; set; }
        public int? LojaId { get; set; }
        public int? MarcaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string? Versao { get; set; }
        public int? AnoFabricacao { get; set; }
        public int? AnoModelo { get; set; }
        public string? Cor { get; set; }
        public string? Quilometragem { get; set; }
        public Combustivel? Combustivel { get; set; }
        public Cambio? Cambio { get; set; }
        public string? Placa { get; set; }
        public string? PrecoVenda { get; set; }
        [MaxLength(4000)]
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
        public bool Vendido { get; set; }
        public bool Destaque { get; set; }
        public bool Seminovo { get; set; }
        public bool MotoEletrica { get; set; }
        public bool AceitaTroca { get; set; }
        public bool Financiavel { get; set; }
    }

    public sealed class CaracteristicaInputModel
    {
        public bool ArCondicionado { get; set; }
        public bool DirecaoHidraulica { get; set; }
        public bool DirecaoEletrica { get; set; }
        public bool VidroEletrico { get; set; }
        public bool TravaEletrica { get; set; }
        public bool BancoDeCouro { get; set; }
        public bool VolanteMultifuncional { get; set; }
        public bool ComputadorBordo { get; set; }
        public bool SensorChuva { get; set; }
        public bool TetoSolar { get; set; }
        public bool AirbagMotorista { get; set; }
        public bool AirbagPassageiro { get; set; }
        public bool AirbagLateral { get; set; }
        public bool AirbagCortina { get; set; }
        public bool FreiosAbs { get; set; }
        public bool Alarme { get; set; }
        public bool CameraDeRe { get; set; }
        public bool FarolLed { get; set; }
        public bool CentralMultimidia { get; set; }
        public bool Som { get; set; }
        public bool Bluetooth { get; set; }
        public bool Usb { get; set; }
        public bool Radio { get; set; }
        public bool CarregadorInducao { get; set; }
        public bool RodaLigaLeve { get; set; }
        public bool Engate { get; set; }
        public bool CapotaMaritima { get; set; }
        public bool SantoAntonio { get; set; }
        public bool ProtetorCacamba { get; set; }
        public bool CambioAutomatico { get; set; }
        public bool CambioManual { get; set; }
        public bool Turbo { get; set; }
        public bool Hibrido { get; set; }
        public bool Eletrico { get; set; }
        public bool TracaoDianteira { get; set; }

        public static CaracteristicaInputModel FromEntity(VeiculoCaracteristica? entity)
        {
            if (entity is null) return new CaracteristicaInputModel();

            return new CaracteristicaInputModel
            {
                ArCondicionado = entity.ArCondicionado,
                DirecaoHidraulica = entity.DirecaoHidraulica,
                DirecaoEletrica = entity.DirecaoEletrica,
                VidroEletrico = entity.VidroEletrico,
                TravaEletrica = entity.TravaEletrica,
                BancoDeCouro = entity.BancoDeCouro,
                VolanteMultifuncional = entity.VolanteMultifuncional,
                ComputadorBordo = entity.ComputadorBordo,
                SensorChuva = entity.SensorChuva,
                TetoSolar = entity.TetoSolar,
                AirbagMotorista = entity.AirbagMotorista,
                AirbagPassageiro = entity.AirbagPassageiro,
                AirbagLateral = entity.AirbagLateral,
                AirbagCortina = entity.AirbagCortina,
                FreiosAbs = entity.FreiosAbs,
                Alarme = entity.Alarme,
                CameraDeRe = entity.CameraDeRe,
                FarolLed = entity.FarolLed,
                CentralMultimidia = entity.CentralMultimidia,
                Som = entity.Som,
                Bluetooth = entity.Bluetooth,
                Usb = entity.Usb,
                Radio = entity.Radio,
                CarregadorInducao = entity.CarregadorInducao,
                RodaLigaLeve = entity.RodaLigaLeve,
                Engate = entity.Engate,
                CapotaMaritima = entity.CapotaMaritima,
                SantoAntonio = entity.SantoAntonio,
                ProtetorCacamba = entity.ProtetorCacamba,
                CambioAutomatico = entity.CambioAutomatico,
                CambioManual = entity.CambioManual,
                Turbo = entity.Turbo,
                Hibrido = entity.Hibrido,
                Eletrico = entity.Eletrico,
                TracaoDianteira = entity.TracaoDianteira
            };
        }

        public IReadOnlyCollection<TipoVeiculoOpcional> ToOpcionais()
        {
            var opcionais = new List<TipoVeiculoOpcional>();

            void AddIf(bool value, TipoVeiculoOpcional opcional)
            {
                if (value) opcionais.Add(opcional);
            }

            AddIf(ArCondicionado, TipoVeiculoOpcional.ArCondicionado);
            AddIf(DirecaoHidraulica, TipoVeiculoOpcional.DirecaoHidraulica);
            AddIf(DirecaoEletrica, TipoVeiculoOpcional.DirecaoEletrica);
            AddIf(VidroEletrico, TipoVeiculoOpcional.VidroEletrico);
            AddIf(TravaEletrica, TipoVeiculoOpcional.TravaEletrica);
            AddIf(BancoDeCouro, TipoVeiculoOpcional.BancoDeCouro);
            AddIf(VolanteMultifuncional, TipoVeiculoOpcional.VolanteMultifuncional);
            AddIf(ComputadorBordo, TipoVeiculoOpcional.ComputadorBordo);
            AddIf(SensorChuva, TipoVeiculoOpcional.SensorChuva);
            AddIf(TetoSolar, TipoVeiculoOpcional.TetoSolar);
            AddIf(AirbagMotorista, TipoVeiculoOpcional.AirbagMotorista);
            AddIf(AirbagPassageiro, TipoVeiculoOpcional.AirbagPassageiro);
            AddIf(AirbagLateral, TipoVeiculoOpcional.AirbagLateral);
            AddIf(AirbagCortina, TipoVeiculoOpcional.AirbagCortina);
            AddIf(FreiosAbs, TipoVeiculoOpcional.FreiosAbs);
            AddIf(Alarme, TipoVeiculoOpcional.Alarme);
            AddIf(CameraDeRe, TipoVeiculoOpcional.CameraDeRe);
            AddIf(FarolLed, TipoVeiculoOpcional.FarolLed);
            AddIf(CentralMultimidia, TipoVeiculoOpcional.CentralMultimidia);
            AddIf(Som, TipoVeiculoOpcional.Som);
            AddIf(Bluetooth, TipoVeiculoOpcional.Bluetooth);
            AddIf(Usb, TipoVeiculoOpcional.Usb);
            AddIf(Radio, TipoVeiculoOpcional.Radio);
            AddIf(CarregadorInducao, TipoVeiculoOpcional.CarregadorInducao);
            AddIf(RodaLigaLeve, TipoVeiculoOpcional.RodaLigaLeve);
            AddIf(Engate, TipoVeiculoOpcional.Engate);
            AddIf(CapotaMaritima, TipoVeiculoOpcional.CapotaMaritima);
            AddIf(SantoAntonio, TipoVeiculoOpcional.SantoAntonio);
            AddIf(ProtetorCacamba, TipoVeiculoOpcional.ProtetorCacamba);
            AddIf(CambioAutomatico, TipoVeiculoOpcional.CambioAutomatico);
            AddIf(CambioManual, TipoVeiculoOpcional.CambioManual);
            AddIf(Turbo, TipoVeiculoOpcional.Turbo);
            AddIf(Hibrido, TipoVeiculoOpcional.Hibrido);
            AddIf(Eletrico, TipoVeiculoOpcional.Eletrico);
            AddIf(TracaoDianteira, TipoVeiculoOpcional.TracaoDianteira);

            return opcionais;
        }
    }
}
