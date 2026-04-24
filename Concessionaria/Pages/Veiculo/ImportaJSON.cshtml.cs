using Core.Enums;
using Core.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Concessionaria.Pages.Veiculo;

public class ImportaJSONModel : PageModel
{
    private const long MaxJsonFileSize = 15 * 1024 * 1024;
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly ApplicationDbContext _context;
    private readonly IVeiculoService _veiculoService;
    private readonly IVeiculoCaracteristicaService _veiculoCaracteristicaService;
    private readonly IVeiculoMidiaService _veiculoMidiaService;
    private readonly IWebHostEnvironment _environment;

    public ImportaJSONModel(
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
    public InputModel Input { get; set; } = new();

    public PreviewModel? Preview { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        var json = await ReadJsonAsync(Input.JsonFile);
        if (json == null)
        {
            return Page();
        }

        Input.RawJson = json;
        Preview = await BuildPreviewAsync(json);
        return Page();
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.RawJson))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.JsonFile)}", "Envie o arquivo JSON para importar.");
            return Page();
        }

        Preview = await BuildPreviewAsync(Input.RawJson);
        if (Preview.Items.Count == 0 || Preview.Items.All(item => !item.CanImport))
        {
            foreach (var error in Preview.Errors.DefaultIfEmpty("Nenhum registro valido foi encontrado para importacao."))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }

        var createdFiles = new List<string>();
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var importedVehicles = 0;
        var importedPhotos = 0;

        try
        {
            foreach (var item in Preview.Items.Where(item => item.CanImport))
            {
                var veiculoResult = await _veiculoService.CriarAsync(item.ToVeiculo());
                if (veiculoResult.Status != PackageStatus.Success)
                {
                    await transaction.RollbackAsync();
                    CleanupFiles(createdFiles);
                    ModelState.AddModelError(string.Empty, veiculoResult.UserMessage ?? $"Falha ao importar o veiculo legado {item.IdLegado}.");
                    return Page();
                }

                var veiculoId = veiculoResult.Data;
                var caracteristicaResult = await _veiculoCaracteristicaService.CriarOuAtualizarAsync(item.ToCaracteristica(veiculoId));
                if (caracteristicaResult.Status != PackageStatus.Success)
                {
                    await transaction.RollbackAsync();
                    CleanupFiles(createdFiles);
                    ModelState.AddModelError(string.Empty, caracteristicaResult.UserMessage ?? $"Falha ao salvar os opcionais do veiculo legado {item.IdLegado}.");
                    return Page();
                }

                foreach (var foto in item.Fotos.OrderBy(foto => foto.Ordem ?? int.MaxValue))
                {
                    Directory.CreateDirectory(item.DestinoPastaFisica);
                    var destinationPath = Path.Combine(item.DestinoPastaFisica, foto.NomeArquivo!);
                    var projectFileExists = System.IO.File.Exists(destinationPath);
                    var sourceFileExists = !projectFileExists && !string.IsNullOrWhiteSpace(foto.CaminhoCompletoOriginal) && System.IO.File.Exists(foto.CaminhoCompletoOriginal);

                    if (sourceFileExists)
                    {
                        System.IO.File.Copy(foto.CaminhoCompletoOriginal!, destinationPath, overwrite: false);
                        createdFiles.Add(destinationPath);
                    }

                    var useManagedFile = projectFileExists || sourceFileExists;
                    var mediaResult = await _veiculoMidiaService.CriarAsync(new VeiculoMidia
                    {
                        VeiculoId = veiculoId,
                        NomeArquivo = foto.NomeArquivo!,
                        Url = useManagedFile ? foto.UrlDestino! : foto.UrlOrigem!,
                        BlobName = useManagedFile ? foto.NomeArquivo : null,
                        Container = useManagedFile ? $"uploads/veiculos/{item.IdLegado}" : null,
                        Tipo = "imagem",
                        ContentType = string.IsNullOrWhiteSpace(foto.ContentType) ? "image/jpeg" : foto.ContentType,
                        TamanhoBytes = foto.TamanhoBytes,
                        Capa = foto.Capa,
                        Ordem = foto.Ordem ?? 0,
                        Ativo = true
                    });

                    if (mediaResult.Status != PackageStatus.Success)
                    {
                        await transaction.RollbackAsync();
                        CleanupFiles(createdFiles);
                        ModelState.AddModelError(string.Empty, mediaResult.UserMessage ?? $"Falha ao salvar a midia {foto.NomeArquivo} do veiculo legado {item.IdLegado}.");
                        return Page();
                    }

                    importedPhotos++;
                }

                var savedVehicle = await _context.Veiculos.FirstOrDefaultAsync(veiculo => veiculo.Id == veiculoId);
                if (savedVehicle != null)
                {
                    savedVehicle.ImportadoMidia = item.Fotos.Count > 0;
                    await _context.SaveChangesAsync();
                }

                importedVehicles++;
            }

            await transaction.CommitAsync();
            var skipped = Preview.Items.Count - importedVehicles;
            TempData["SuccessMessage"] = skipped > 0
                ? $"Importacao concluida: {importedVehicles} veiculo(s) e {importedPhotos} foto(s) importadas. {skipped} registro(s) foram ignorados."
                : $"Importacao concluida: {importedVehicles} veiculo(s) e {importedPhotos} foto(s) importadas.";

            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            CleanupFiles(createdFiles);
            ModelState.AddModelError(string.Empty, $"Nao foi possivel concluir a importacao: {ex.Message}");
            return Page();
        }
    }

    private async Task<string?> ReadJsonAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.JsonFile)}", "Selecione um arquivo JSON.");
            return null;
        }

        if (file.Length > MaxJsonFileSize)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.JsonFile)}", "O arquivo JSON excede o limite de 15 MB.");
            return null;
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.JsonFile)}", "O arquivo precisa ter extensao .json.");
            return null;
        }

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var json = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.JsonFile)}", "O arquivo JSON esta vazio.");
            return null;
        }

        return json;
    }

    private async Task<PreviewModel> BuildPreviewAsync(string rawJson)
    {
        var preview = new PreviewModel { RawJson = rawJson };
        List<LegacyItemDto> registros;

        try
        {
            registros = Deserialize(rawJson);
        }
        catch (Exception ex)
        {
            preview.Errors.Add($"JSON invalido: {ex.Message}");
            return preview;
        }

        if (registros.Count == 0)
        {
            preview.Errors.Add("Nenhum veiculo foi encontrado no JSON.");
            return preview;
        }

        var duplicateLegacyIds = registros.Where(item => item.IdLegado > 0)
            .GroupBy(item => item.IdLegado)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        var duplicatePlates = registros.Select(item => NormalizePlate(item.DadosAdmin?.Placa))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var lojas = await _context.Lojas.AsNoTracking().OrderBy(loja => loja.Id).ToListAsync();
        var marcas = await _context.Marcas.AsNoTracking().OrderBy(marca => marca.Nome).ToListAsync();
        var existingIds = registros.Where(item => item.IdLegado > 0).Select(item => item.IdLegado).Distinct().ToList();
        var veiculosComIdLegado = existingIds.Count == 0
            ? new Dictionary<int, Domain.Veiculo>()
            : await _context.Veiculos.AsNoTracking()
                .Where(veiculo => veiculo.IdLegado.HasValue && existingIds.Contains(veiculo.IdLegado.Value))
                .ToDictionaryAsync(veiculo => veiculo.IdLegado!.Value);

        var existingPlates = registros.Select(item => NormalizePlate(item.DadosAdmin?.Placa))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var veiculosComPlaca = existingPlates.Count == 0
            ? new Dictionary<string, Domain.Veiculo>(StringComparer.OrdinalIgnoreCase)
            : (await _context.Veiculos.AsNoTracking().Where(veiculo => veiculo.Placa != null).ToListAsync())
                .Select(veiculo => new { Veiculo = veiculo, Placa = NormalizePlate(veiculo.Placa) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Placa) && existingPlates.Contains(item.Placa, StringComparer.OrdinalIgnoreCase))
                .GroupBy(item => item.Placa!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Veiculo, StringComparer.OrdinalIgnoreCase);

        foreach (var registro in registros)
        {
            var item = await BuildItemAsync(registro, lojas, marcas, veiculosComIdLegado, veiculosComPlaca);

            if (duplicateLegacyIds.Contains(registro.IdLegado))
            {
                item.Errors.Add($"O idLegado {registro.IdLegado} esta duplicado dentro do proprio arquivo.");
            }

            var plate = NormalizePlate(registro.DadosAdmin?.Placa);
            if (!string.IsNullOrWhiteSpace(plate) && duplicatePlates.Contains(plate))
            {
                item.Errors.Add($"A placa {plate} esta duplicada dentro do proprio arquivo.");
            }

            item.CanImport = item.Errors.Count == 0;
            preview.Items.Add(item);
        }

        foreach (var item in preview.Items.Where(item => !item.CanImport))
        {
            preview.Errors.AddRange(item.Errors.Select(error => $"Id legado {item.IdLegado}: {error}"));
        }

        preview.CanImport = preview.Items.Any(item => item.CanImport);
        return preview;
    }

    private async Task<PreviewItem> BuildItemAsync(
        LegacyItemDto registro,
        List<Domain.Loja> lojas,
        List<Domain.Marca> marcas,
        IReadOnlyDictionary<int, Domain.Veiculo> veiculosComIdLegado,
        IReadOnlyDictionary<string, Domain.Veiculo> veiculosComPlaca)
    {
        var admin = registro.DadosAdmin ?? new LegacyAdminDto();
        var item = new PreviewItem
        {
            IdLegado = registro.IdLegado,
            TipoCadastro = Clean(admin.TipoCadastro),
            Status = Clean(admin.Status),
            LojaNome = Clean(admin.Loja),
            MarcaNome = Clean(admin.Marca),
            Modelo = Clean(admin.Modelo),
            Titulo = Clean(registro.Titulo) ?? BuildTitulo(admin.Marca, admin.Modelo),
            KmTexto = Clean(admin.Km),
            Cor = Clean(admin.Cor),
            Combustivel = Clean(admin.Combustivel),
            Cambio = Clean(admin.Cambio),
            Opcionais = Clean(admin.OpcionaisTexto),
            ValorTexto = Clean(admin.Valor ?? registro.Valor),
            AnoTexto = Clean(admin.Ano),
            AnoExibicaoTexto = Clean(admin.AnoExibicao),
            Placa = NormalizePlate(admin.Placa),
            DataVendaTexto = Clean(admin.DataVenda),
            Descricao = NormalizeDescription(admin.DescricaoHtml),
            Publico = registro.Origem?.Publico ?? true,
            Admin = registro.Origem?.Admin ?? true,
            DestinoPastaFisica = Path.Combine(GetProjectPublicWebRootPath(), "uploads", "veiculos", registro.IdLegado.ToString())
        };

        if (item.IdLegado <= 0) item.Errors.Add("idLegado e obrigatorio e precisa ser maior que zero.");
        if (string.IsNullOrWhiteSpace(item.TipoCadastro)) item.Errors.Add("tipoCadastro e obrigatorio.");
        else if (!Normalize(item.TipoCadastro).Contains("veiculo") && !Normalize(item.TipoCadastro).Contains("moto")) item.Errors.Add("tipoCadastro precisa ser 'Veiculos' ou 'Motos'.");

        item.Loja = ResolveLoja(admin, lojas);
        if (item.Loja == null) item.Errors.Add($"Loja nao encontrada para '{item.LojaNome ?? admin.LojaValue ?? "(vazia)"}'.");

        item.Marca = await ResolveOrCreateMarcaAsync(item.MarcaNome, marcas, item.Errors);
        if (item.Marca == null) item.Errors.Add("Marca nao pode ser resolvida.");
        if (string.IsNullOrWhiteSpace(item.Modelo)) item.Errors.Add("modelo e obrigatorio.");

        item.Quilometragem = ParseNullableInt(item.KmTexto);
        if (!string.IsNullOrWhiteSpace(item.KmTexto) && item.Quilometragem == null) item.Errors.Add("km esta em formato invalido.");

        item.PrecoVenda = ParseLegacyPrice(item.ValorTexto);

        item.AnoFabricacao = ParseNullableYear(item.AnoTexto);
        if (HasRelevantYearValue(item.AnoTexto) && item.AnoFabricacao == null) item.Errors.Add("ano esta em formato invalido.");

        item.AnoModelo = ParseNullableYear(item.AnoExibicaoTexto) ?? item.AnoFabricacao;
        item.DataVenda = ParseNullableDate(item.DataVendaTexto);
        if (!string.IsNullOrWhiteSpace(item.DataVendaTexto) && item.DataVenda == null) item.Errors.Add("dataVenda esta em formato invalido.");
        if (!string.IsNullOrWhiteSpace(item.Descricao) && item.Descricao.Length > 1000) item.Errors.Add("descricaoHtml gerou uma descricao com mais de 1000 caracteres.");
        if (!string.IsNullOrWhiteSpace(item.Placa) && item.Placa.Length > 20) item.Errors.Add("placa excede o limite de 20 caracteres.");

        item.Seminuovo = ContainsNormalized(item.Status, "seminovo");
        item.MotoEletrica = ContainsNormalized(item.Status, "moto eletrica")
            || ContainsNormalized(item.Status, "moto elétrica");
        item.Vendido = item.DataVenda.HasValue || ContainsNormalized(item.Status, "vendido");
        item.ObservacoesInternas = BuildObservacoes(admin, item, registro.Midias?.Pasta);
        item.Caracteristica = ParseOpcionais(item.Opcionais, item.Cambio, item.Combustivel);

        if (item.IdLegado > 0 && veiculosComIdLegado.ContainsKey(item.IdLegado)) item.Errors.Add($"Ja existe um veiculo cadastrado com o idLegado {item.IdLegado}.");
        if (!string.IsNullOrWhiteSpace(item.Placa) && veiculosComPlaca.TryGetValue(item.Placa, out var veiculoComPlaca)) item.Errors.Add($"A placa {item.Placa} ja esta vinculada ao veiculo #{veiculoComPlaca.Id}.");

        BuildPhotoPreview(item, registro.Midias);
        return item;
    }

    private void BuildPhotoPreview(PreviewItem item, LegacyMidiasDto? midias)
    {
        var fotos = midias?.Fotos ?? [];
        item.TotalFotosInformado = midias?.TotalFotos ?? 0;

        if (fotos.Count == 0)
        {
            item.Errors.Add("O registro nao possui fotos na secao midias.");
            return;
        }

        if (midias?.TotalFotos > 0 && midias.TotalFotos != fotos.Count)
        {
            item.Errors.Add($"totalFotos informado ({midias.TotalFotos}) difere da quantidade recebida ({fotos.Count}).");
        }

        var duplicateOrders = fotos.Where(foto => foto.Ordem.HasValue).GroupBy(foto => foto.Ordem!.Value).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        var duplicateNames = fotos.Where(foto => !string.IsNullOrWhiteSpace(foto.NomeArquivo)).GroupBy(foto => foto.NomeArquivo!, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var coverCount = fotos.Count(foto => foto.Capa);
        if (coverCount == 0) item.Errors.Add("E necessario definir uma foto de capa.");
        if (coverCount > 1) item.Errors.Add("Ha mais de uma foto marcada como capa no JSON.");

        foreach (var foto in fotos.OrderBy(foto => foto.Ordem ?? int.MaxValue))
        {
            var preview = new PreviewPhoto
            {
                Ordem = foto.Ordem,
                Capa = foto.Capa,
                NomeArquivo = Clean(foto.NomeArquivo),
                CaminhoCompletoOriginal = Clean(foto.CaminhoCompleto),
                CaminhoRelativo = Clean(foto.CaminhoRelativo),
                UrlOrigem = Clean(foto.UrlOrigem),
                ContentType = Clean(foto.ContentType),
                TamanhoBytes = foto.TamanhoBytes
            };

            if (!preview.Ordem.HasValue || preview.Ordem.Value < 0) preview.Errors.Add("ordem e obrigatoria e precisa ser maior ou igual a zero.");
            else if (duplicateOrders.Contains(preview.Ordem.Value)) preview.Errors.Add($"A ordem {preview.Ordem.Value} esta duplicada dentro do proprio arquivo.");

            if (string.IsNullOrWhiteSpace(preview.NomeArquivo)) preview.Errors.Add("nomeArquivo e obrigatorio.");
            else if (duplicateNames.Contains(preview.NomeArquivo)) preview.Errors.Add($"O arquivo {preview.NomeArquivo} esta duplicado dentro do proprio JSON.");

            if (!string.IsNullOrWhiteSpace(preview.NomeArquivo))
            {
                preview.DestinoFisico = Path.Combine(item.DestinoPastaFisica, preview.NomeArquivo);
                preview.UrlDestino = BuildUploadUrl(item.IdLegado, preview.CaminhoRelativo, preview.NomeArquivo);
            }

            if (!string.IsNullOrWhiteSpace(preview.ContentType) && !preview.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) preview.Errors.Add("A midia precisa ter contentType de imagem.");

            var fileAlreadyInProject = !string.IsNullOrWhiteSpace(preview.DestinoFisico) && System.IO.File.Exists(preview.DestinoFisico);
            var sourceExists = !fileAlreadyInProject && !string.IsNullOrWhiteSpace(preview.CaminhoCompletoOriginal) && System.IO.File.Exists(preview.CaminhoCompletoOriginal);
            if (!fileAlreadyInProject && !sourceExists && string.IsNullOrWhiteSpace(preview.UrlOrigem)) preview.Errors.Add("Informe caminhoCompleto existente, arquivo ja presente em uploads ou urlOrigem para a foto.");

            item.Fotos.Add(preview);
            item.Errors.AddRange(preview.Errors);
        }
    }

    private Domain.Loja? ResolveLoja(LegacyAdminDto admin, List<Domain.Loja> lojas)
    {
        if (!string.IsNullOrWhiteSpace(admin.LojaValue) && int.TryParse(admin.LojaValue, out var lojaId))
        {
            var byId = lojas.FirstOrDefault(loja => loja.Id == lojaId);
            if (byId != null) return byId;
        }

        if (string.IsNullOrWhiteSpace(admin.Loja)) return null;
        var normalized = Normalize(admin.Loja);
        return lojas.FirstOrDefault(loja => Normalize(loja.Nome) == normalized)
            ?? lojas.FirstOrDefault(loja => Normalize(loja.Nome).Contains(normalized, StringComparison.OrdinalIgnoreCase) || normalized.Contains(Normalize(loja.Nome), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Domain.Marca?> ResolveOrCreateMarcaAsync(string? marcaNome, List<Domain.Marca> marcas, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(marcaNome)) return null;

        var normalized = Normalize(marcaNome);
        var existing = marcas.FirstOrDefault(marca => Normalize(marca.Nome) == normalized);
        if (existing != null) return existing;

        try
        {
            var novaMarca = new Domain.Marca { Nome = marcaNome.Trim(), Ativo = true, DataCadastro = DateTime.Now, LogoUrl = string.Empty };
            _context.Marcas.Add(novaMarca);
            await _context.SaveChangesAsync();
            marcas.Add(novaMarca);
            return novaMarca;
        }
        catch (Exception ex)
        {
            errors.Add($"Nao foi possivel criar a marca '{marcaNome}': {ex.Message}");
            return null;
        }
    }

    private static List<LegacyItemDto> Deserialize(string rawJson)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        using var document = JsonDocument.Parse(rawJson);

        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Object => [DeserializeItem(document.RootElement, options)],
            JsonValueKind.Array => document.RootElement.EnumerateArray().Select(item => DeserializeItem(item, options)).ToList(),
            _ => throw new InvalidOperationException("O arquivo deve conter um objeto JSON ou um array de objetos.")
        };
    }

    private static LegacyItemDto DeserializeItem(JsonElement element, JsonSerializerOptions options)
    {
        return element.Deserialize<LegacyItemDto>(options)
            ?? throw new InvalidOperationException("Um dos registros do JSON nao pode ser lido.");
    }

    private static string BuildTitulo(string? marca, string? modelo)
    {
        return string.Join(" ", new[] { marca, modelo }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
    }

    private static string BuildObservacoes(LegacyAdminDto admin, PreviewItem item, string? pastaMidia)
    {
        var parts = new List<string> { $"Id legado: {item.IdLegado}" };
        if (!string.IsNullOrWhiteSpace(admin.DescricaoInterna)) parts.Add($"Descricao interna legado: {admin.DescricaoInterna.Trim()}");
        if (!string.IsNullOrWhiteSpace(admin.Portas)) parts.Add($"Portas: {admin.Portas.Trim()}");
        if (!string.IsNullOrWhiteSpace(admin.Cilindradas)) parts.Add($"Cilindradas: {admin.Cilindradas.Trim()}");
        if (!string.IsNullOrWhiteSpace(admin.TipoMoto)) parts.Add($"Tipo de moto: {admin.TipoMoto.Trim()}");
        if (!string.IsNullOrWhiteSpace(pastaMidia)) parts.Add($"Pasta de midia legado: {pastaMidia.Trim()}");
        return string.Join(" | ", parts);
    }

    private static VeiculoCaracteristica ParseOpcionais(string? opcionais, string? cambio, string? combustivel)
    {
        var result = new VeiculoCaracteristica();
        var normalized = Normalize(opcionais ?? string.Empty);
        var normalizedCambio = Normalize(cambio ?? string.Empty);
        var normalizedCombustivel = Normalize(combustivel ?? string.Empty);

        result.ArCondicionado = HasOption(normalized, "ar condicionado");
        result.DirecaoHidraulica = HasOption(normalized, "direcao hidraulica");
        result.DirecaoEletrica = HasOption(normalized, "direcao eletrica");
        result.VidroEletrico = HasOption(normalized, "vidros eletricos", "vidro eletrico");
        result.TravaEletrica = HasOption(normalized, "travas eletricas", "trava eletrica");
        result.RetrovisorEletrico = HasOption(normalized, "retrovisor eletrico");
        result.BancoDeCouro = HasOption(normalized, "banco de couro");
        result.VolanteMultifuncional = HasOption(normalized, "volante multifuncional");
        result.PilotoAutomatico = HasOption(normalized, "piloto automatico", "controle de cruzeiro");
        result.ComputadorBordo = HasOption(normalized, "computador de bordo");
        result.ChavePresencial = HasOption(normalized, "chave presencial");
        result.PartidaBotao = HasOption(normalized, "partida por botao");
        result.AirbagMotorista = HasOption(normalized, "airbag", "air bag", "airbag motorista");
        result.AirbagPassageiro = HasOption(normalized, "airbag", "air bag", "airbag passageiro");
        result.Alarme = HasOption(normalized, "alarme");
        result.FreiosAbs = HasOption(normalized, "abs", "freios abs");
        result.CameraDeRe = HasOption(normalized, "camera de re");
        result.SensorEstacionamentoTraseiro = HasOption(normalized, "sensor de estacionamento", "sensor traseiro");
        result.SensorEstacionamentoDianteiro = HasOption(normalized, "sensor dianteiro");
        result.CentralMultimidia = HasOption(normalized, "multimidia", "mp3player");
        result.Som = HasOption(normalized, "som", "mp3player");
        result.Bluetooth = HasOption(normalized, "bluetooth");
        result.Usb = HasOption(normalized, "usb");
        result.Radio = HasOption(normalized, "radio");
        result.GPS = HasOption(normalized, "gps");
        result.AppleCarPlay = HasOption(normalized, "apple carplay", "carplay");
        result.AndroidAuto = HasOption(normalized, "android auto");
        result.RodaLigaLeve = HasOption(normalized, "roda de liga", "rodas de liga");
        result.CapotaMaritima = HasOption(normalized, "capota maritima");
        result.Estribo = HasOption(normalized, "estribo");
        result.SantoAntonio = HasOption(normalized, "santo antonio");
        result.ProtetorCacamba = HasOption(normalized, "protetor de cacamba");
        result.CambioManual = normalizedCambio.Contains("manual", StringComparison.Ordinal);
        result.CambioAutomatico = normalizedCambio.Contains("automatic", StringComparison.Ordinal);
        result.CambioCvt = normalizedCambio.Contains("cvt", StringComparison.Ordinal);
        result.Turbo = HasOption(normalized, "turbo");
        result.TracaoIntegral = HasOption(normalized, "4x4", "tracao integral");
        result.TracaoDianteira = HasOption(normalized, "tracao dianteira");
        result.TracaoTraseira = HasOption(normalized, "tracao traseira");
        result.Hibrido = normalizedCombustivel.Contains("hibrido", StringComparison.Ordinal);
        result.Eletrico = normalizedCombustivel.Contains("eletric", StringComparison.Ordinal);
        return result;
    }

    private static bool HasOption(string source, params string[] values) => values.Any(value => source.Contains(Normalize(value), StringComparison.Ordinal));
    private static bool ContainsNormalized(string? source, string expected) => Normalize(source ?? string.Empty).Contains(Normalize(expected), StringComparison.Ordinal);

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : null;
    }

    private static int? ParseNullableYear(string? value)
    {
        if (IsEmptyPlaceholder(value)) return null;
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits) || digits.All(ch => ch == '0')) return null;
        var year = int.TryParse(digits, out var parsedYear) ? (int?)parsedYear : null;
        if (!year.HasValue || year.Value == 0) return null;
        var limit = DateTime.Now.Year + 1;
        return year.Value is >= 1900 and <= int.MaxValue && year.Value <= limit ? year.Value : null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value.Trim(), NumberStyles.Number, PtBr, out var parsed)) return decimal.Round(parsed, 2, MidpointRounding.AwayFromZero);
        var sanitized = new string(value.Where(ch => char.IsDigit(ch) || ch == ',' || ch == '.').ToArray());
        return decimal.TryParse(sanitized, NumberStyles.Number, PtBr, out parsed) ? decimal.Round(parsed, 2, MidpointRounding.AwayFromZero) : null;
    }

    private static decimal ParseLegacyPrice(string? value)
    {
        var parsed = ParseNullableDecimal(value);
        return parsed.HasValue && parsed.Value > 0m ? parsed.Value : 0m;
    }

    private static DateTime? ParseNullableDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yyyy HH:mm", "d/M/yyyy H:m", "yyyy-MM-dd" };
        return DateTime.TryParseExact(value.Trim(), formats, PtBr, DateTimeStyles.None, out var parsed) ? parsed : null;
    }

    private static string? NormalizeDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalizedInput = FixLegacyEncoding(value);
        var withBreaks = Regex.Replace(normalizedInput, @"<\s*br\s*/?\s*>|<\s*/p\s*>", Environment.NewLine, RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withBreaks, "<.*?>", " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var lines = decoded.Replace("\r\n", "\n").Split('\n').Select(CollapseWhitespace).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static string CollapseWhitespace(string value) => Regex.Replace(value.Trim(), @"\s+", " ");

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        if (IsEmptyPlaceholder(trimmed)) return null;

        return FixLegacyEncoding(trimmed);
    }

    private static bool IsEmptyPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;

        var trimmed = value.Trim();
        if (trimmed is "-" or "--" or "---" or "0" or "00" or "0000") return true;

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrWhiteSpace(digits) && digits.All(ch => ch == '0'))
        {
            var letters = new string(trimmed.Where(char.IsLetter).ToArray());
            if (string.IsNullOrWhiteSpace(letters) || letters.Equals("km", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizePlate(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return null;
        var normalized = new string(plate.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(normalized) || normalized.All(ch => ch == '0') ? null : normalized;
    }

    private static string Normalize(string value)
    {
        var formD = FixLegacyEncoding(value).Trim().Normalize(NormalizationForm.FormD);
        var chars = formD.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).ToLowerInvariant();
    }

    private static bool HasRelevantYearValue(string? value)
    {
        if (IsEmptyPlaceholder(value)) return false;
        return !string.IsNullOrWhiteSpace(new string((value ?? string.Empty).Where(char.IsDigit).ToArray()));
    }

    private static string FixLegacyEncoding(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !LooksLikeMojibake(value)) return value;

        try
        {
            var latin1 = Encoding.GetEncoding("ISO-8859-1");
            var bytes = latin1.GetBytes(value);
            var repaired = Encoding.UTF8.GetString(bytes);
            return LooksLikeMojibake(repaired) ? value : repaired;
        }
        catch
        {
            return value;
        }
    }

    private static bool LooksLikeMojibake(string value)
    {
        return value.Contains('Ã')
            || value.Contains('Â')
            || value.Contains('�')
            || value.Contains("â€", StringComparison.Ordinal);
    }

    private static string BuildUploadUrl(int idLegado, string? caminhoRelativo, string? nomeArquivo)
    {
        var normalizedRelative = string.IsNullOrWhiteSpace(caminhoRelativo)
            ? null
            : caminhoRelativo.Trim().Replace('\\', '/').TrimStart('/');

        if (!string.IsNullOrWhiteSpace(normalizedRelative))
        {
            return $"/uploads/veiculos/{normalizedRelative}";
        }

        return $"/uploads/veiculos/{idLegado}/{nomeArquivo}";
    }

    private string GetProjectPublicWebRootPath()
    {
        var solutionRoot = Directory.GetParent(_environment.ContentRootPath)?.FullName ?? _environment.ContentRootPath;
        return Path.Combine(solutionRoot, "Project", "wwwroot");
    }

    private static void CleanupFiles(IEnumerable<string> files)
    {
        foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try { if (System.IO.File.Exists(file)) System.IO.File.Delete(file); } catch { }
        }
    }

    public sealed class InputModel
    {
        public IFormFile? JsonFile { get; set; }
        public string RawJson { get; set; } = string.Empty;
    }

    public sealed class PreviewModel
    {
        public string RawJson { get; set; } = string.Empty;
        public List<string> Errors { get; } = [];
        public List<PreviewItem> Items { get; } = [];
        public bool CanImport { get; set; }
    }

    public sealed class PreviewItem
    {
        public int IdLegado { get; set; }
        public string? TipoCadastro { get; set; }
        public string? Status { get; set; }
        public string? LojaNome { get; set; }
        public Domain.Loja? Loja { get; set; }
        public string? MarcaNome { get; set; }
        public Domain.Marca? Marca { get; set; }
        public string? Modelo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? KmTexto { get; set; }
        public int? Quilometragem { get; set; }
        public string? Cor { get; set; }
        public string? Combustivel { get; set; }
        public string? Cambio { get; set; }
        public string? Opcionais { get; set; }
        public string? ValorTexto { get; set; }
        public decimal? PrecoVenda { get; set; }
        public string? AnoTexto { get; set; }
        public int? AnoFabricacao { get; set; }
        public string? AnoExibicaoTexto { get; set; }
        public int? AnoModelo { get; set; }
        public string? Placa { get; set; }
        public string? DataVendaTexto { get; set; }
        public DateTime? DataVenda { get; set; }
        public string? Descricao { get; set; }
        public bool Seminuovo { get; set; }
        public bool MotoEletrica { get; set; }
        public bool Vendido { get; set; }
        public bool Publico { get; set; }
        public bool Admin { get; set; }
        public string? ObservacoesInternas { get; set; }
        public int TotalFotosInformado { get; set; }
        public string DestinoPastaFisica { get; set; } = string.Empty;
        public VeiculoCaracteristica Caracteristica { get; set; } = new();
        public List<PreviewPhoto> Fotos { get; } = [];
        public List<string> Errors { get; } = [];
        public bool CanImport { get; set; }

        public Domain.Veiculo ToVeiculo() => new()
        {
            IdLegado = IdLegado,
            LojaId = Loja!.Id,
            MarcaId = Marca!.Id,
            Titulo = string.IsNullOrWhiteSpace(Titulo) ? $"{Marca?.Nome} {Modelo}".Trim() : Titulo,
            Modelo = Modelo,
            Quilometragem = Quilometragem,
            Cor = Cor,
            Combustivel = Combustivel,
            Cambio = Cambio,
            Descricao = Descricao,
            PrecoVenda = PrecoVenda,
            AnoFabricacao = AnoFabricacao,
            AnoModelo = AnoModelo,
            Placa = Placa,
            ObservacoesInternas = ObservacoesInternas,
            Seminovo = Seminuovo,
            MotoEletrica = MotoEletrica,
            Vendido = Vendido,
            DataVenda = DataVenda,
            Ativo = Publico && !Vendido,
            AceitaTroca = ContainsNormalized(Descricao, "aceitamos troca"),
            Financiavel = ContainsNormalized(Descricao, "financiamento")
        };

        public VeiculoCaracteristica ToCaracteristica(int veiculoId)
        {
            Caracteristica.VeiculoId = veiculoId;
            return Caracteristica;
        }
    }

    public sealed class PreviewPhoto
    {
        public int? Ordem { get; set; }
        public bool Capa { get; set; }
        public string? NomeArquivo { get; set; }
        public string? CaminhoCompletoOriginal { get; set; }
        public string? CaminhoRelativo { get; set; }
        public string? UrlOrigem { get; set; }
        public string? ContentType { get; set; }
        public long? TamanhoBytes { get; set; }
        public string? DestinoFisico { get; set; }
        public string? UrlDestino { get; set; }
        public List<string> Errors { get; } = [];
    }

    public sealed class LegacyItemDto
    {
        public int IdLegado { get; set; }
        public LegacyOrigemDto? Origem { get; set; }
        public string? Titulo { get; set; }
        public string? Valor { get; set; }
        public LegacyAdminDto? DadosAdmin { get; set; }
        public LegacyMidiasDto? Midias { get; set; }
    }

    public sealed class LegacyOrigemDto
    {
        public bool Admin { get; set; }
        public bool Publico { get; set; }
    }

    public sealed class LegacyAdminDto
    {
        public int IdLegado { get; set; }
        public string? TipoCadastro { get; set; }
        public string? TipoCadastroValue { get; set; }
        public string? Status { get; set; }
        public string? StatusValue { get; set; }
        public string? Loja { get; set; }
        public string? LojaValue { get; set; }
        public string? Marca { get; set; }
        public string? MarcaValue { get; set; }
        public string? Modelo { get; set; }
        public string? Km { get; set; }
        public string? Portas { get; set; }
        public string? Cor { get; set; }
        public string? Combustivel { get; set; }
        public string? CombustivelValue { get; set; }
        public string? Cambio { get; set; }
        public string? CambioValue { get; set; }
        public string? DescricaoHtml { get; set; }
        public string? OpcionaisTexto { get; set; }
        public string? Valor { get; set; }
        public string? Ano { get; set; }
        public string? AnoExibicao { get; set; }
        public string? Placa { get; set; }
        public string? Cilindradas { get; set; }
        public string? TipoMoto { get; set; }
        public string? DescricaoInterna { get; set; }
        public string? DataVenda { get; set; }
    }

    public sealed class LegacyMidiasDto
    {
        public string? Pasta { get; set; }
        public int TotalFotos { get; set; }
        public List<LegacyFotoDto>? Fotos { get; set; }
    }

    public sealed class LegacyFotoDto
    {
        public int? Ordem { get; set; }
        public bool Capa { get; set; }
        public string? NomeArquivo { get; set; }
        public string? CaminhoCompleto { get; set; }
        public string? CaminhoRelativo { get; set; }
        public string? UrlOrigem { get; set; }
        public string? ContentType { get; set; }
        public long? TamanhoBytes { get; set; }
    }
}
